/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The head of a history: its latest commit and the version of the roaming network it leads to.
    /// </summary>
    /// <param name="Commit">The latest commit.</param>
    /// <param name="RoamingNetwork">The version of the roaming network it leads to.</param>
    public sealed record RoamingNetworkHead(RoamingNetworkCommit  Commit,
                                            RoamingNetwork        RoamingNetwork);


    /// <summary>
    /// The outcome of publishing a change set.
    /// </summary>
    public enum RoamingNetworkPublishStatus
    {

        /// <summary>
        /// The change set is the new head commit.
        /// </summary>
        Published,

        /// <summary>
        /// The change set, or a rebased version of it, was published before: nothing changed.
        /// </summary>
        AlreadyPublished,

        /// <summary>
        /// The head is no longer the expected one, or the change set no longer fits it: nothing changed.
        /// Rebase the change set and publish it again.
        /// </summary>
        HeadMoved,

        /// <summary>
        /// The change set was refused, e.g. as it is invalid or conflicts: nothing changed.
        /// </summary>
        Refused

    }


    /// <summary>
    /// The result of publishing a change set.
    /// </summary>
    /// <param name="Status">The outcome.</param>
    /// <param name="Head">The head after publishing.</param>
    /// <param name="Commit">The commit of the change set, when published now or before.</param>
    /// <param name="Rebased">Whether the change set was rebased to be published.</param>
    /// <param name="ErrorResponse">Why it was not published.</param>
    public sealed record RoamingNetworkPublishResult(RoamingNetworkPublishStatus  Status,
                                                     RoamingNetworkHead           Head,
                                                     RoamingNetworkCommit?        Commit          = null,
                                                     Boolean                      Rebased         = false,
                                                     String?                      ErrorResponse   = null)
    {

        /// <summary>
        /// Whether the change set is part of the history now.
        /// </summary>
        public Boolean IsPublished
            => Status is RoamingNetworkPublishStatus.Published or RoamingNetworkPublishStatus.AlreadyPublished;

    }


    /// <summary>
    /// Called for every commit a history takes, in their order, while the history is locked:
    /// return quickly and do not change the history from here.
    /// </summary>
    /// <param name="Announcement">A slim notice of the commit.</param>
    public delegate void OnRoamingNetworkCommitAnnouncementDelegate(RoamingNetworkCommitAnnouncement Announcement);

    /// <summary>
    /// Called for every commit a history takes new signatures of, while the history is locked:
    /// return quickly and do not change the history from here.
    /// </summary>
    /// <param name="Commit">The commit with all its signatures.</param>
    /// <param name="NewSignatures">The signatures taken now.</param>
    public delegate void OnRoamingNetworkCommitSignaturesDelegate(RoamingNetworkCommit                          Commit,
                                                                  IReadOnlyList<RoamingNetworkCommitSignature>  NewSignatures);


    /// <summary>
    /// The history of a roaming network as a chain of commits, held in memory.
    /// With one leading server (model a), the leader publishes change sets as new commits,
    /// and the other servers start from a snapshot of its head and append its commits.
    /// Every change of the history is atomic: it is visible completely or not at all.
    /// </summary>
    public sealed class RoamingNetworkHistory
    {

        #region Data

        /// <summary>
        /// The default maximum number of commits returned at once.
        /// </summary>
        public const Int32 DefaultMaxCommits = 1000;

        private readonly Lock                                                       historyLock          = new ();
        private readonly List<RoamingNetworkCommit>                                 commits              = [];
        private readonly Dictionary<RoamingNetworkCommit_Id, Int32>                 commitIndex          = [];
        private readonly Dictionary<ChangeSet_Id, RoamingNetworkCommit_Id>          publishedChangeSets  = [];
        private          RoamingNetworkHead                                         head;

        #endregion

        #region Properties

        /// <summary>
        /// The roaming network of this history.
        /// </summary>
        public RoamingNetwork_Id     RoamingNetworkId    { get; }

        /// <summary>
        /// The first commit this history holds: a genesis commit, or the head of the snapshot it started from.
        /// </summary>
        public RoamingNetworkCommit  Root
        {
            get
            {
                lock (historyLock)
                {
                    return commits[0];
                }
            }
        }

        /// <summary>
        /// The latest commit and the version it leads to.
        /// </summary>
        public RoamingNetworkHead    Head
            => Volatile.Read(ref head);

        /// <summary>
        /// The current admin status and status of the entities of the head version, kept across versions.
        /// </summary>
        public RoamingNetworkRuntimeStatus  RuntimeStatus    { get; }

        /// <summary>
        /// The keys that sign every commit published here, e.g. the one of the leading server.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommitSigner>  Signers  { get; set; } = [];

        /// <summary>
        /// The number of commits this history holds, including its root.
        /// </summary>
        public Int32                 Count
        {
            get
            {
                lock (historyLock)
                {
                    return commits.Count;
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Called for every commit this history takes, published here or appended from the leading server,
        /// in their order, while the history is locked: return quickly and do not change the history from there.
        /// </summary>
        public event OnRoamingNetworkCommitAnnouncementDelegate? OnCommitAnnouncement;

        /// <summary>
        /// Called for every commit this history takes new signatures of, e.g. of a roaming network operator,
        /// while the history is locked: return quickly and do not change the history from there.
        /// </summary>
        public event OnRoamingNetworkCommitSignaturesDelegate?   OnCommitSignatures;

        #endregion

        #region Constructor(s)

        private RoamingNetworkHistory(RoamingNetworkCommit  Root,
                                      RoamingNetwork        RoamingNetwork)
        {

            this.RoamingNetworkId  = RoamingNetwork.Id;
            this.head              = new RoamingNetworkHead(Root, RoamingNetwork);
            this.RuntimeStatus     = new RoamingNetworkRuntimeStatus(this);

            RuntimeStatus.Attach(RoamingNetwork);

            Remember(Root);

        }

        #endregion


        #region (static) Start        (RoamingNetwork, Signers = null)

        /// <summary>
        /// Start the history of the given first version of a roaming network with its genesis commit.
        /// </summary>
        /// <param name="RoamingNetwork">The first version of a roaming network.</param>
        /// <param name="Signers">The optional keys that sign the genesis commit and every commit published here.</param>
        public static RoamingNetworkHistory Start(RoamingNetwork                            RoamingNetwork,
                                                  IEnumerable<RoamingNetworkCommitSigner>?  Signers   = null)
        {

            var signers = Signers?.ToImmutableArray() ?? [];

            return new (SignedBy(signers, RoamingNetworkCommit.Genesis(RoamingNetwork)),
                        RoamingNetwork) {
                       Signers = signers
                   };

        }

        #endregion

        #region (static) TryFromSnapshot(Snapshot, out History, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to start a history from a snapshot of another history's head, e.g. of the leading server.
        /// The roaming network must have the ETags of the commit.
        /// </summary>
        /// <param name="Snapshot">A snapshot, see ToSnapshotJSON().</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits for the change set of the commit, the default ones otherwise.</param>
        public static Boolean TryFromSnapshot(JObject                                          Snapshot,
                                              [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                              [NotNullWhen(false)] out String?                 ErrorResponse,
                                              RoamingNetworkChangeSetLimits?                   Limits   = null)
        {

            History = null;

            try
            {

                if (Snapshot?["commit"] is not JObject commitJSON ||
                    !RoamingNetworkCommit.TryParse(commitJSON, out var commit, out var commitError, Limits))
                {
                    ErrorResponse = "Missing or invalid commit of the snapshot!";
                    return false;
                }

                if (Snapshot["roamingNetwork"] is not JObject roamingNetworkJSON ||
                    !RoamingNetwork.TryParse(roamingNetworkJSON, out var roamingNetwork, out var roamingNetworkError))
                {
                    ErrorResponse = "Missing or invalid roaming network of the snapshot!";
                    return false;
                }

                return TryFromSnapshot(roamingNetwork,
                                       commit,
                                       out History,
                                       out ErrorResponse) &&

                       TryTakeRuntimeStatus(ref History,
                                            Snapshot["runtimeStatus"],
                                            out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryFromSnapshot(Text, out History, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to start a history from the JSON text of a snapshot of another history's head.
        /// Dates stay text, so signed values keep their exact representation.
        /// </summary>
        /// <param name="Text">The JSON text of a snapshot, see ToSnapshotJSON().</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits for the change set of the commit, the default ones otherwise.</param>
        public static Boolean TryFromSnapshot(String                                           Text,
                                              [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                              [NotNullWhen(false)] out String?                 ErrorResponse,
                                              RoamingNetworkChangeSetLimits?                   Limits   = null)
        {

            History = null;

            try
            {

                using var reader = new JsonTextReader(new StringReader(Text)) {
                                       DateParseHandling   = DateParseHandling.None,
                                       FloatParseHandling  = FloatParseHandling.Decimal
                                   };

                if (JToken.ReadFrom(reader) is not JObject snapshot)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryFromSnapshot(snapshot, out History, out ErrorResponse, Limits);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryFromSnapshot(RoamingNetwork, Commit, out History, out ErrorResponse)

        /// <summary>
        /// Try to start a history from the given version of a roaming network and the commit leading to it.
        /// The roaming network must have the ETags of the commit.
        /// </summary>
        /// <param name="RoamingNetwork">The version of the roaming network.</param>
        /// <param name="Commit">The commit leading to it.</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryFromSnapshot(RoamingNetwork                                   RoamingNetwork,
                                              RoamingNetworkCommit                             Commit,
                                              [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                              [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            History = null;

            if (RoamingNetwork.Id != Commit.RoamingNetworkId)
            {
                ErrorResponse = $"The roaming network '{RoamingNetwork.Id}' is not the one of the commit, '{Commit.RoamingNetworkId}'!";
                return false;
            }

            if (!RoamingNetwork.MatchesETags(Commit.ETags))
            {
                ErrorResponse = "The roaming network does not have the ETags of the commit!";
                return false;
            }

            // A copy, when it needs another revision: the given version stays as it is.
            var roamingNetwork = RoamingNetwork.WithRevision(Commit.Revision);
            roamingNetwork.CalcETags();

            History        = new RoamingNetworkHistory(Commit, roamingNetwork);
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region ToSnapshotJSON(IncludeRuntimeStatus = true)

        /// <summary>
        /// A snapshot of the head, now and complete: its commit, the complete version of the roaming network,
        /// and the current admin status and status of its entities, from which another server starts its history.
        /// The commit and the network can be verified by the ETags of the commit; the runtime status is not content.
        /// </summary>
        /// <param name="IncludeRuntimeStatus">Whether to include the runtime status.</param>
        public JObject ToSnapshotJSON(Boolean IncludeRuntimeStatus = true)
        {

            var head = Head;

            var json = new JObject(
                           new JProperty("timestamp",       Timestamp.Now.ToISO8601()),
                           new JProperty("commit",          head.Commit.ToJSON()),
                           new JProperty("roamingNetwork",  head.RoamingNetwork.ToJSON())
                       );

            if (IncludeRuntimeStatus)
                json.Add("runtimeStatus", new JArray(RoamingNetworkRuntimeStatus.Of(head.RoamingNetwork).Select(status => status.ToJSON())));

            return json;

        }

        #endregion


        #region ToSnapshotCBOR(IncludeRuntimeStatus = true)

        /// <summary>
        /// A snapshot of the head in CBOR, see ToSnapshotJSON(), with the keys of the JSON snapshot.
        /// Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        /// <param name="IncludeRuntimeStatus">Whether to include the runtime status.</param>
        public CBORValue ToSnapshotCBOR(Boolean IncludeRuntimeStatus = true)
        {

            var head     = Head;

            var entries  = new List<KeyValuePair<CBORValue, CBORValue>> {
                               new (CBORValue.FromText("timestamp"),       CBORValue.FromText(Timestamp.Now.ToISO8601())),
                               new (CBORValue.FromText("commit"),          head.Commit.ToCBOR()),
                               new (CBORValue.FromText("roamingNetwork"),  head.RoamingNetwork.ToCBOR())
                           };

            if (IncludeRuntimeStatus)
                entries.Add(new (CBORValue.FromText("runtimeStatus"),
                                 CBORJSON.ToCBOR(new JArray(RoamingNetworkRuntimeStatus.Of(head.RoamingNetwork).Select(status => status.ToJSON())))));

            return CBORValue.FromMap(entries);

        }

        #endregion

        #region (static) TryFromSnapshot(CBOR,  out History, out ErrorResponse, Limits = null)

        /// <summary>
        /// Try to start a history from a CBOR snapshot of another history's head, e.g. of the leading server.
        /// The roaming network must have the ETags of the commit.
        /// </summary>
        /// <param name="Snapshot">A CBOR snapshot, see ToSnapshotCBOR().</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Limits">Optional limits for the change set of the commit, the default ones otherwise.</param>
        public static Boolean TryFromSnapshot(CBORValue                                        Snapshot,
                                              [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                              [NotNullWhen(false)] out String?                 ErrorResponse,
                                              RoamingNetworkChangeSetLimits?                   Limits   = null)
        {

            History = null;

            try
            {

                if (Snapshot.Kind != CBORValueKind.Map ||
                    !Snapshot.TryGetValue(CBORValue.FromText("commit"), out var commitCBOR) ||
                    !RoamingNetworkCommit.TryParse(commitCBOR, out var commit, out var commitError, Limits))
                {
                    ErrorResponse = "Missing or invalid commit of the snapshot!";
                    return false;
                }

                if (!Snapshot.TryGetValue(CBORValue.FromText("roamingNetwork"), out var roamingNetworkCBOR) ||
                    !RoamingNetwork.TryParse(roamingNetworkCBOR, out var roamingNetwork, out var roamingNetworkError))
                {
                    ErrorResponse = "Missing or invalid roaming network of the snapshot!";
                    return false;
                }

                return TryFromSnapshot(roamingNetwork,
                                       commit,
                                       out History,
                                       out ErrorResponse) &&

                       TryTakeRuntimeStatus(ref History,
                                            Snapshot.TryGetValue(CBORValue.FromText("runtimeStatus"), out var runtimeStatusCBOR)
                                                ? CBORJSON.ToJSON(runtimeStatusCBOR)
                                                : null,
                                            out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion


        #region (private static) TryTakeRuntimeStatus(ref History, RuntimeStatus, out ErrorResponse)

        /// <summary>
        /// Take the runtime status of a snapshot, if it has one, as it is: the leader's.
        /// An invalid one refuses the snapshot.
        /// </summary>
        private static Boolean TryTakeRuntimeStatus([NotNullWhen(true)] ref RoamingNetworkHistory?  History,
                                                    JToken?                                         RuntimeStatus,
                                                    [NotNullWhen(false)] out String?                ErrorResponse)
        {

            ErrorResponse = null;

            if (RuntimeStatus is null || RuntimeStatus.Type == JTokenType.Null)
                return History is not null;

            if (RuntimeStatus is not JArray runtimeStatusJSON)
            {
                History        = null;
                ErrorResponse  = "The runtime status of the snapshot must be an array!";
                return false;
            }

            var statusUpdates = new List<POIStatusUpdate>();

            foreach (var statusJSON in runtimeStatusJSON)
            {

                if (statusJSON is not JObject statusObject ||
                    !POIStatusUpdate.TryParse(statusObject, out var statusUpdate, out var statusError))
                {
                    History        = null;
                    ErrorResponse  = "Invalid runtime status of the snapshot!";
                    return false;
                }

                statusUpdates.Add(statusUpdate);

            }

            if (!History!.RuntimeStatus.TryTake(statusUpdates, out var takeError))
            {
                History        = null;
                ErrorResponse  = "Invalid runtime status of the snapshot: " + takeError;
                return false;
            }

            return true;

        }

        #endregion


        #region Publish  (ChangeSet, ExpectedHeadId = null, Rebase = false, VerifySignature = null)

        /// <summary>
        /// Publish the given change set as the new head commit: on the leading server.
        /// A change set published before, also in a rebased version, is not published again.
        /// With an expected head, nothing is published when the head has moved on.
        /// A change set that does not fit the head is rebased when allowed, otherwise refused as HeadMoved.
        /// A signed change set requires a verifier accepting every signature; a rebased one is unsigned.
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="ExpectedHeadId">The optional identification of the head the caller expects.</param>
        /// <param name="Rebase">Whether to rebase a change set that does not fit the head.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        public RoamingNetworkPublishResult Publish(RoamingNetworkChangeSet            ChangeSet,
                                                   RoamingNetworkCommit_Id?           ExpectedHeadId    = null,
                                                   Boolean                            Rebase            = false,
                                                   VerifyChangeSetSignatureDelegate?  VerifySignature   = null)
        {

            lock (historyLock)
            {

                var head = Head;

                if (ChangeSet is null)
                    return new (RoamingNetworkPublishStatus.Refused, head, ErrorResponse: "The given change set must not be null!");

                if (ChangeSet.RoamingNetworkId != RoamingNetworkId)
                    return new (RoamingNetworkPublishStatus.Refused, head, ErrorResponse: $"The change set is for the roaming network '{ChangeSet.RoamingNetworkId}', not for '{RoamingNetworkId}'!");

                if (FindPublished(ChangeSet) is RoamingNetworkCommit published)
                    return new (RoamingNetworkPublishStatus.AlreadyPublished, head, published);

                if (ExpectedHeadId.HasValue && ExpectedHeadId.Value != head.Commit.Id)
                    return new (RoamingNetworkPublishStatus.HeadMoved, head, ErrorResponse: $"The head is no longer '{ExpectedHeadId}', but '{head.Commit.Id}'!");

                var changeSet = ChangeSet;
                var rebased   = false;

                if (!head.RoamingNetwork.Fits(changeSet))
                {

                    if (!Rebase)
                        return new (RoamingNetworkPublishStatus.HeadMoved, head, ErrorResponse: "The change set does not fit the head; rebase it!");

                    if (!head.RoamingNetwork.TryRebase(changeSet, out var rebasedChangeSet, out var rebaseError, VerifySignature))
                        return new (RoamingNetworkPublishStatus.Refused, head, ErrorResponse: rebaseError);

                    // The signatures of the owners cover the before ETags: only they can sign a rebased change set.
                    if (rebasedChangeSet.Signatures.IsEmpty)
                        return new (RoamingNetworkPublishStatus.Refused, head, ErrorResponse: "The change set does not fit the head: it can be rebased, but its owners must sign it again!");

                    changeSet = rebasedChangeSet;
                    rebased   = true;

                }

                if (!head.RoamingNetwork.TryApplyChangeSet(changeSet, out var roamingNetwork, out var applyError, VerifySignature))
                    return new (RoamingNetworkPublishStatus.Refused, head, ErrorResponse: applyError);

                var commit  = SignedBy(Signers, RoamingNetworkCommit.Next(head.Commit, changeSet, roamingNetwork));
                var newHead = new RoamingNetworkHead(commit, roamingNetwork);

                Advance(newHead);

                return new (RoamingNetworkPublishStatus.Published, newHead, commit, rebased);

            }

        }

        #endregion

        #region TryAppend(Commit,  out ErrorResponse, VerifySignature = null, VerifyCommitSignature = null, RequiredSignatureRoles = null)

        /// <summary>
        /// Append the given commit of the leading server to this history: on the other servers.
        /// It must follow the head, have the next revision, and its change set must lead
        /// to a version with its ETags. Its signatures must all be valid, and it must be signed in every required role.
        /// A commit this history holds already is accepted, taking the signatures it has in addition.
        /// </summary>
        /// <param name="Commit">The commit.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        /// <param name="VerifyCommitSignature">A verifier of commit signatures using trusted keys, required for signed commits.</param>
        /// <param name="RequiredSignatureRoles">The roles a commit must be signed in, e.g. leader.</param>
        public Boolean TryAppend(RoamingNetworkCommit                Commit,
                                 [NotNullWhen(false)] out String?    ErrorResponse,
                                 VerifyChangeSetSignatureDelegate?   VerifySignature          = null,
                                 VerifyCommitSignatureDelegate?      VerifyCommitSignature    = null,
                                 IReadOnlyCollection<String>?        RequiredSignatureRoles   = null)
        {

            lock (historyLock)
            {

                var head = Head;

                if (Commit is null)
                {
                    ErrorResponse = "The given commit must not be null!";
                    return false;
                }

                if (commitIndex.ContainsKey(Commit.Id))
                    return TryAddSignatures(Commit.Id, Commit.Signatures, VerifyCommitSignature, out _, out _, out ErrorResponse);

                if (!CheckSignatures(Commit, VerifyCommitSignature, RequiredSignatureRoles, out ErrorResponse))
                    return false;

                if (Commit.RoamingNetworkId != RoamingNetworkId)
                {
                    ErrorResponse = $"The commit is for the roaming network '{Commit.RoamingNetworkId}', not for '{RoamingNetworkId}'!";
                    return false;
                }

                if (Commit.ChangeSet is null || Commit.ParentId != head.Commit.Id)
                {
                    ErrorResponse = $"The commit '{Commit.Id}' does not follow the head '{head.Commit.Id}'!";
                    return false;
                }

                if (Commit.Revision != head.Commit.Revision + 1)
                {
                    ErrorResponse = $"The commit has the revision {Commit.Revision}, not the next one, {head.Commit.Revision + 1}!";
                    return false;
                }

                if (!head.RoamingNetwork.TryApplyChangeSet(Commit.ChangeSet, out var roamingNetwork, out var applyError, VerifySignature))
                {
                    ErrorResponse = applyError;
                    return false;
                }

                if (!roamingNetwork.MatchesETags(Commit.ETags))
                {
                    ErrorResponse = "The change set of the commit does not lead to the ETags of the commit!";
                    return false;
                }

                Advance(new RoamingNetworkHead(Commit, roamingNetwork));

                ErrorResponse = null;
                return true;

            }

        }

        #endregion

        #region TryAppend(Commits, out Appended, out ErrorResponse, VerifySignature = null, VerifyCommitSignature = null, RequiredSignatureRoles = null)

        /// <summary>
        /// Append the given commits of the leading server in their order, until the first one that can not be appended.
        /// </summary>
        /// <param name="Commits">The commits.</param>
        /// <param name="Appended">The number of commits appended or held already.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        /// <param name="VerifyCommitSignature">A verifier of commit signatures using trusted keys, required for signed commits.</param>
        /// <param name="RequiredSignatureRoles">The roles a commit must be signed in, e.g. leader.</param>
        public Boolean TryAppend(IEnumerable<RoamingNetworkCommit>  Commits,
                                 out Int32                          Appended,
                                 [NotNullWhen(false)] out String?   ErrorResponse,
                                 VerifyChangeSetSignatureDelegate?  VerifySignature          = null,
                                 VerifyCommitSignatureDelegate?     VerifyCommitSignature    = null,
                                 IReadOnlyCollection<String>?       RequiredSignatureRoles   = null)
        {

            Appended = 0;

            foreach (var commit in Commits)
            {

                if (!TryAppend(commit, out ErrorResponse, VerifySignature, VerifyCommitSignature, RequiredSignatureRoles))
                {
                    ErrorResponse = $"Commit {Appended}: {ErrorResponse}";
                    return false;
                }

                Appended++;

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryAddSignatures(CommitId, Signatures, VerifyCommitSignature, out Commit, out NewSignatures, out ErrorResponse)

        /// <summary>
        /// Add the given signatures to a commit of this history, e.g. the one of a roaming network operator
        /// that received the data of a charging station operator and checked it, or ones the leading server took.
        /// Every new signature must be valid; signatures the commit has already are skipped.
        /// The identification of the commit stays the same.
        /// </summary>
        /// <param name="CommitId">The identification of the commit.</param>
        /// <param name="Signatures">Signatures of the commit.</param>
        /// <param name="VerifyCommitSignature">A verifier of commit signatures using trusted keys, required for new signatures.</param>
        /// <param name="Commit">The commit with all its signatures afterwards.</param>
        /// <param name="NewSignatures">The signatures added now.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryAddSignatures(RoamingNetworkCommit_Id                                   CommitId,
                                        IEnumerable<RoamingNetworkCommitSignature>                Signatures,
                                        VerifyCommitSignatureDelegate?                            VerifyCommitSignature,
                                        [NotNullWhen(true)]  out RoamingNetworkCommit?            Commit,
                                        out IReadOnlyList<RoamingNetworkCommitSignature>          NewSignatures,
                                        [NotNullWhen(false)] out String?                          ErrorResponse)
        {

            lock (historyLock)
            {

                Commit         = null;
                NewSignatures  = [];

                if (!commitIndex.TryGetValue(CommitId, out var index))
                {
                    ErrorResponse = $"The commit '{CommitId}' is unknown here!";
                    return false;
                }

                var commit         = commits[index];
                var newSignatures  = Signatures.Where(signature => !commit.Signatures.Contains(signature)).Distinct().ToArray();

                if (newSignatures.Length == 0)
                {
                    Commit         = commit;
                    ErrorResponse  = null;
                    return true;
                }

                foreach (var signature in newSignatures)
                    if (!CheckSignature(commit, signature, VerifyCommitSignature, out ErrorResponse))
                        return false;

                commit          = commit.WithSignatures(newSignatures);
                commits[index]  = commit;

                var head = Head;

                if (head.Commit.Id == CommitId)
                    Volatile.Write(ref this.head, head with { Commit = commit });

                // One failing subscriber must neither fail the signatures nor keep them from the others.
                foreach (var subscriber in OnCommitSignatures?.GetInvocationList().Cast<OnRoamingNetworkCommitSignaturesDelegate>() ?? [])
                {
                    try
                    {
                        subscriber(commit, newSignatures);
                    }
                    catch
                    { }
                }

                Commit         = commit;
                NewSignatures  = newSignatures;
                ErrorResponse  = null;
                return true;

            }

        }

        #endregion

        #region (static) CheckSignatures(Commit, VerifyCommitSignature, RequiredSignatureRoles, out ErrorResponse)

        /// <summary>
        /// Check that every signature of the given commit is valid, and that it is signed in every required role,
        /// e.g. a commit taken from a snapshot.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        /// <param name="VerifyCommitSignature">A verifier of commit signatures using trusted keys, required for signed commits.</param>
        /// <param name="RequiredSignatureRoles">The roles the commit must be signed in, e.g. leader.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean CheckSignatures(RoamingNetworkCommit              Commit,
                                              VerifyCommitSignatureDelegate?    VerifyCommitSignature,
                                              IReadOnlyCollection<String>?      RequiredSignatureRoles,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            foreach (var signature in Commit.Signatures)
                if (!CheckSignature(Commit, signature, VerifyCommitSignature, out ErrorResponse))
                    return false;

            foreach (var role in RequiredSignatureRoles ?? [])
                if (!Commit.Signatures.Any(signature => signature.Role == role))
                {
                    ErrorResponse = $"The commit '{Commit.Id}' is not signed in the role '{role}'!";
                    return false;
                }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) CheckSignature(Commit, Signature, VerifyCommitSignature, out ErrorResponse)

        private static Boolean CheckSignature(RoamingNetworkCommit              Commit,
                                              RoamingNetworkCommitSignature     Signature,
                                              VerifyCommitSignatureDelegate?    VerifyCommitSignature,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (VerifyCommitSignature is null)
            {
                ErrorResponse = $"The commit '{Commit.Id}' is signed, but there is no verifier of commit signatures!";
                return false;
            }

            if (!VerifyCommitSignature(Commit, Signature))
            {
                ErrorResponse = $"The signature of the commit '{Commit.Id}' by {Signature} is invalid, or its key is not trusted!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) SignedBy(Signers, Commit)

        /// <summary>
        /// The given commit signed by all the given signers.
        /// </summary>
        private static RoamingNetworkCommit SignedBy(ImmutableArray<RoamingNetworkCommitSigner>  Signers,
                                                     RoamingNetworkCommit                        Commit)

            => Signers.IsDefaultOrEmpty
                   ? Commit
                   : Commit.WithSignatures(Signers.Select(signer => signer.Sign(Commit)));

        #endregion


        #region (private) Advance(NewHead)

        /// <summary>
        /// Take the commit of the given new head, while the history is locked:
        /// remember it, move the head, follow the status of its entities, and announce it.
        /// </summary>
        private void Advance(RoamingNetworkHead NewHead)
        {

            Remember(NewHead.Commit);
            Volatile.Write(ref head, NewHead);

            RuntimeStatus.Attach(NewHead.RoamingNetwork);

            var announcement = new RoamingNetworkCommitAnnouncement(NewHead.Commit, Timestamp.Now);

            // One failing subscriber must neither fail the commit nor keep it from the others.
            foreach (var subscriber in OnCommitAnnouncement?.GetInvocationList().Cast<OnRoamingNetworkCommitAnnouncementDelegate>() ?? [])
            {
                try
                {
                    subscriber(announcement);
                }
                catch
                { }
            }

        }

        #endregion


        #region Contains           (CommitId)

        /// <summary>
        /// Whether this history holds the given commit.
        /// </summary>
        /// <param name="CommitId">A commit identification.</param>
        public Boolean Contains(RoamingNetworkCommit_Id CommitId)
        {
            lock (historyLock)
            {
                return commitIndex.ContainsKey(CommitId);
            }
        }

        #endregion

        #region TryGetCommit       (CommitId, out Commit)

        /// <summary>
        /// Try to return the given commit completely.
        /// </summary>
        /// <param name="CommitId">A commit identification.</param>
        /// <param name="Commit">The commit.</param>
        public Boolean TryGetCommit(RoamingNetworkCommit_Id                         CommitId,
                                    [NotNullWhen(true)] out RoamingNetworkCommit?   Commit)
        {

            lock (historyLock)
            {

                if (commitIndex.TryGetValue(CommitId, out var index))
                {
                    Commit = commits[index];
                    return true;
                }

                Commit = null;
                return false;

            }

        }

        #endregion

        #region TryGetCommitsSince (CommitId, out Commits,     out ErrorResponse, MaxCount = DefaultMaxCommits)

        /// <summary>
        /// Try to return the commits after the given one completely, oldest first, at most the given number:
        /// what a server on that commit needs to reach the head. Fetch more from the last one returned.
        /// A commit this history does not hold, e.g. older than its root, requires a new snapshot.
        /// </summary>
        /// <param name="CommitId">The identification of the commit the caller is on.</param>
        /// <param name="Commits">The commits after it.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="MaxCount">The maximum number of commits to return.</param>
        public Boolean TryGetCommitsSince(RoamingNetworkCommit_Id                                    CommitId,
                                          [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkCommit>?  Commits,
                                          [NotNullWhen(false)] out String?                           ErrorResponse,
                                          Int32                                                      MaxCount   = DefaultMaxCommits)
        {

            lock (historyLock)
            {

                if (!commitIndex.TryGetValue(CommitId, out var index))
                {
                    Commits        = null;
                    ErrorResponse  = $"The commit '{CommitId}' is unknown here; start from a new snapshot!";
                    return false;
                }

                Commits        = commits.Skip(index + 1).Take(Math.Max(0, MaxCount)).ToArray();
                ErrorResponse  = null;
                return true;

            }

        }

        #endregion

        #region TryGetCommitInfosSince(CommitId, out CommitInfos, out ErrorResponse, MaxCount = DefaultMaxCommits)

        /// <summary>
        /// Try to return the metadata of the commits after the given one, oldest first, at most the given number,
        /// without their changes: to list them, and to decide which commits to fetch completely.
        /// </summary>
        /// <param name="CommitId">The identification of the commit the caller is on.</param>
        /// <param name="CommitInfos">The metadata of the commits after it.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="MaxCount">The maximum number of commits to return.</param>
        public Boolean TryGetCommitInfosSince(RoamingNetworkCommit_Id                                         CommitId,
                                              [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkCommitInfo>?  CommitInfos,
                                              [NotNullWhen(false)] out String?                                ErrorResponse,
                                              Int32                                                           MaxCount   = DefaultMaxCommits)
        {

            if (TryGetCommitsSince(CommitId, out var commits, out ErrorResponse, MaxCount))
            {
                CommitInfos = commits.Select(commit => new RoamingNetworkCommitInfo(commit)).ToArray();
                return true;
            }

            CommitInfos = null;
            return false;

        }

        #endregion


        #region (private) Remember     (Commit)

        /// <summary>
        /// Hold the given commit and remember its change set as published:
        /// by its own identification and by those of the change sets it was rebased from.
        /// </summary>
        private void Remember(RoamingNetworkCommit Commit)
        {

            commitIndex[Commit.Id] = commits.Count;
            commits.Add(Commit);

            if (Commit.ChangeSet is RoamingNetworkChangeSet changeSet)
                foreach (var changeSetId in ChangeSetIds(changeSet))
                    publishedChangeSets.TryAdd(changeSetId, Commit.Id);

        }

        #endregion

        #region (private) FindPublished(ChangeSet)

        /// <summary>
        /// The commit of the given change set, or of another rebased version of it, if it was published before.
        /// </summary>
        private RoamingNetworkCommit? FindPublished(RoamingNetworkChangeSet ChangeSet)
        {

            foreach (var changeSetId in ChangeSetIds(ChangeSet))
                if (publishedChangeSets.TryGetValue(changeSetId, out var commitId))
                    return commits[commitIndex[commitId]];

            return null;

        }

        #endregion

        #region (private static) ChangeSetIds(ChangeSet)

        /// <summary>
        /// The identification of the given change set and of those it was rebased from.
        /// </summary>
        private static IEnumerable<ChangeSet_Id> ChangeSetIds(RoamingNetworkChangeSet ChangeSet)
        {

            yield return ChangeSet.Id;

            if (ChangeSet.Metadata["rebasedFrom"] is JArray rebasedFrom)
                foreach (var original in rebasedFrom)
                    if (original["@id"]?.Type == JTokenType.String &&
                        ChangeSet_Id.TryParse(original["@id"]!.Value<String>()!, out var originalId))
                    {
                        yield return originalId;
                    }

        }

        #endregion

    }

}
