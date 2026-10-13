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
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A tip of a history: a commit no other commit follows, and the version of the roaming network it leads to.
    /// The head is the first tip: the one of the highest revision, of the lowest identification among equal ones.
    /// </summary>
    /// <param name="Commit">The commit.</param>
    /// <param name="RoamingNetwork">The version of the roaming network it leads to.</param>
    public sealed record RoamingNetworkHead(RoamingNetworkCommit  Commit,
                                            RoamingNetwork        RoamingNetwork);


    /// <summary>
    /// The outcome of committing a change set.
    /// </summary>
    public enum RoamingNetworkCommitStatus
    {

        /// <summary>
        /// The change set is a new commit, merged with the other tips where it does not conflict.
        /// </summary>
        Committed,

        /// <summary>
        /// The change set, or a rebased version of it, was committed before: nothing changed.
        /// </summary>
        AlreadyCommitted,

        /// <summary>
        /// The change set was refused, e.g. as it is invalid, not signed by its owners, or fits no version: nothing changed.
        /// </summary>
        Refused

    }


    /// <summary>
    /// The result of committing a change set.
    /// </summary>
    /// <param name="Status">The outcome.</param>
    /// <param name="Head">The head afterwards.</param>
    /// <param name="Commit">The commit of the change set, when committed now or before.</param>
    /// <param name="ErrorResponse">Why it was not committed.</param>
    public sealed record RoamingNetworkCommitResult(RoamingNetworkCommitStatus  Status,
                                                    RoamingNetworkHead          Head,
                                                    RoamingNetworkCommit?       Commit          = null,
                                                    String?                     ErrorResponse   = null)
    {

        /// <summary>
        /// Whether the change set is part of the history now.
        /// </summary>
        public Boolean IsCommitted
            => Status is RoamingNetworkCommitStatus.Committed or RoamingNetworkCommitStatus.AlreadyCommitted;

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
    /// The history of a roaming network as a graph of commits, held in memory with the version of each.
    /// There is no leader: every server commits change sets signed by their owners, appends the commits of others,
    /// and merges the tips that do not conflict, deterministically, so that every server makes the same merge commits.
    /// Conflicting tips stay until an owner resolves the conflict with a change set on one of them.
    /// Every change of the history is atomic: it is visible completely or not at all.
    /// </summary>
    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        /// <summary>
        /// The default maximum number of commits returned at once.
        /// </summary>
        public const Int32 DefaultMaxCommits = 1000;

        /// <summary>
        /// The prefix of the identification of the change set of a merge commit.
        /// </summary>
        public const String MergeChangeSetIdPrefix = "merge-";

        private readonly Lock                                                       historyLock          = new ();
        private readonly List<RoamingNetworkCommit>                                 commits              = [];
        private readonly Dictionary<RoamingNetworkCommit_Id, Int32>                 commitIndex          = [];
        private readonly Dictionary<RoamingNetworkCommit_Id, RoamingNetwork>        versions             = [];
        private readonly Dictionary<ChangeSet_Id, RoamingNetworkCommit_Id>          committedChangeSets  = [];
        private readonly List<RoamingNetworkCommit_Id>                              tips                 = [];
        private          RoamingNetworkHead                                         head;
        private          ImmutableArray<RoamingNetworkMergeConflict>                conflicts            = [];

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
        /// The first tip and the version it leads to: the commit of the highest revision,
        /// of the lowest identification among equal ones. The runtime status follows its entities.
        /// </summary>
        public RoamingNetworkHead    Head
            => Volatile.Read(ref head);

        /// <summary>
        /// The tips: the commits no other commit follows, the head first.
        /// More than one, while they conflict.
        /// </summary>
        public IReadOnlyList<RoamingNetworkHead>  Tips
        {
            get
            {
                lock (historyLock)
                {
                    return [ .. tips.Select(tip => new RoamingNetworkHead(commits[commitIndex[tip]], VersionOf(tip))) ];
                }
            }
        }

        /// <summary>
        /// The conflicts keeping the tips from being merged.
        /// </summary>
        public ImmutableArray<RoamingNetworkMergeConflict>  Conflicts
            => conflicts;

        /// <summary>
        /// The current admin status and status of the entities of the head version, kept across versions.
        /// </summary>
        public RoamingNetworkRuntimeStatus  RuntimeStatus    { get; }

        /// <summary>
        /// The keys that sign every commit made here, e.g. the one of this server, also merge commits.
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
        /// Called for every commit this history takes, made here or appended from another server, also merge commits,
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
            RuntimeStatus.OnStatusUpdate += AppendStatusUpdate;

            Remember(Root, RoamingNetwork);
            tips.Add(Root.Id);

        }

        #endregion


        #region (static) Start        (RoamingNetwork, Signers = null, Store = null, Retention = null)

        /// <summary>
        /// Start the history of the given first version of a roaming network with its genesis commit.
        /// </summary>
        /// <param name="RoamingNetwork">The first version of a roaming network.</param>
        /// <param name="Signers">The optional keys that sign the genesis commit and every commit made here.</param>
        /// <param name="Store">An optional empty store to keep the history in.</param>
        /// <param name="Retention">What to keep; everything, unless given.</param>
        public static RoamingNetworkHistory Start(RoamingNetwork                            RoamingNetwork,
                                                  IEnumerable<RoamingNetworkCommitSigner>?  Signers     = null,
                                                  IRoamingNetworkHistoryStore?              Store       = null,
                                                  RoamingNetworkHistoryRetention?           Retention   = null)
        {

            var signers = Signers?.ToImmutableArray() ?? [];

            var history = new RoamingNetworkHistory(SignedBy(signers, RoamingNetworkCommit.Genesis(RoamingNetwork)),
                                                    RoamingNetwork) {
                              Signers    = signers,
                              Retention  = Retention ?? RoamingNetworkHistoryRetention.KeepEverything
                          };

            if (Store is not null)
                history.AttachStore(Store);

            return history;

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


        #region Commit   (ChangeSet, VerifySignature = null)

        /// <summary>
        /// Commit the given change set, signed by its owners, to the version it was made for:
        /// the head, another tip or an older commit, which then starts a branch.
        /// The new commit is merged with the other tips where it does not conflict.
        /// A change set committed before, also in a rebased version, is not committed again.
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="VerifySignature">An optional verifier of its signatures, in addition to those of its owners.</param>
        public RoamingNetworkCommitResult Commit(RoamingNetworkChangeSet            ChangeSet,
                                                 VerifyChangeSetSignatureDelegate?  VerifySignature   = null)
        {

            lock (historyLock)
            {

                if (ChangeSet is null)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: "The given change set must not be null!");

                if (ChangeSet.RoamingNetworkId != RoamingNetworkId)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: $"The change set is for the roaming network '{ChangeSet.RoamingNetworkId}', not for '{RoamingNetworkId}'!");

                if (FindCommitted(ChangeSet) is RoamingNetworkCommit committed)
                    return new (RoamingNetworkCommitStatus.AlreadyCommitted, Head, committed);

                // The version it was made for: a tip, the head first, or else the latest commit having it.
                if (FindVersionFor(ChangeSet) is not (RoamingNetworkCommit parent, RoamingNetwork parentVersion))
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: "The change set fits no version of this history: prepare it again for the head!");

                if (!parentVersion.TryApplyChangeSet(ChangeSet, out var roamingNetwork, out var applyError, VerifySignature))
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: applyError);

                var commit = SignedBy(Signers, RoamingNetworkCommit.Next(parent, ChangeSet, roamingNetwork));

                try
                {
                    Take(commit, roamingNetwork);
                }
                catch (Exception e)
                {
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: "The commit can not be stored: " + e.Message);
                }

                MergeTips();
                CompactIfDue();

                return new (RoamingNetworkCommitStatus.Committed, Head, commit);

            }

        }

        #endregion

        #region TryAppend(Commit,  out ErrorResponse, VerifySignature = null, VerifyCommitSignature = null, RequiredSignatureRoles = null)

        /// <summary>
        /// Append the given commit of another server to this history. Its parents must be known here.
        /// The change set of a commit with one parent must be signed by its owners, and lead from the version of its parent
        /// to a version with its ETags; a merge commit must be the merge of its parents, as this history calculates it.
        /// Its revision is one more than the highest one of its parents. Its own signatures must all be valid,
        /// and it must be signed in every required role. Afterwards, the tips are merged where they do not conflict.
        /// A commit this history holds already is accepted, taking the signatures it has in addition.
        /// </summary>
        /// <param name="Commit">The commit.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifySignature">An optional verifier of the signatures of change sets, in addition to those of their owners.</param>
        /// <param name="VerifyCommitSignature">A verifier of commit signatures using trusted keys, required for signed commits.</param>
        /// <param name="RequiredSignatureRoles">The roles a commit must be signed in.</param>
        public Boolean TryAppend(RoamingNetworkCommit                Commit,
                                 [NotNullWhen(false)] out String?    ErrorResponse,
                                 VerifyChangeSetSignatureDelegate?   VerifySignature          = null,
                                 VerifyCommitSignatureDelegate?      VerifyCommitSignature    = null,
                                 IReadOnlyCollection<String>?        RequiredSignatureRoles   = null)

            => TryAppend(Commit, true, false, out ErrorResponse, VerifySignature, VerifyCommitSignature, RequiredSignatureRoles);


        /// <param name="Commit">The commit.</param>
        /// <param name="MergeTips">Whether to merge the tips afterwards.</param>
        /// <param name="Known">Whether its signatures were checked before, e.g. when it comes from the store.</param>
        private Boolean TryAppend(RoamingNetworkCommit                Commit,
                                  Boolean                             MergeTips,
                                  Boolean                             Known,
                                  [NotNullWhen(false)] out String?    ErrorResponse,
                                  VerifyChangeSetSignatureDelegate?   VerifySignature,
                                  VerifyCommitSignatureDelegate?      VerifyCommitSignature,
                                  IReadOnlyCollection<String>?        RequiredSignatureRoles)
        {

            lock (historyLock)
            {

                if (Commit is null)
                {
                    ErrorResponse = "The given commit must not be null!";
                    return false;
                }

                if (commitIndex.ContainsKey(Commit.Id))
                {

                    if (!Known)
                        return TryAddSignatures(Commit.Id, Commit.Signatures, VerifyCommitSignature, out _, out _, out ErrorResponse);

                    TakeKnownSignatures(Commit.Id, Commit.Signatures);
                    ErrorResponse = null;
                    return true;

                }

                if (!Known && !CheckSignatures(Commit, VerifyCommitSignature, RequiredSignatureRoles, out ErrorResponse))
                    return false;

                if (Commit.RoamingNetworkId != RoamingNetworkId)
                {
                    ErrorResponse = $"The commit is for the roaming network '{Commit.RoamingNetworkId}', not for '{RoamingNetworkId}'!";
                    return false;
                }

                if (Commit.IsGenesis)
                {
                    ErrorResponse = $"The genesis commit '{Commit.Id}' starts another history!";
                    return false;
                }

                if (Commit.ParentIds.FirstOrDefault(parentId => !commitIndex.ContainsKey(parentId)) is RoamingNetworkCommit_Id unknownParentId &&
                    unknownParentId != default)
                {
                    ErrorResponse = $"The parent '{unknownParentId}' of the commit '{Commit.Id}' is unknown here!";
                    return false;
                }

                var parents   = Commit.ParentIds.Select(parentId => commits[commitIndex[parentId]]).ToArray();
                var revision  = parents.Max(parent => parent.Revision) + 1;

                if (Commit.Revision != revision)
                {
                    ErrorResponse = $"The commit has the revision {Commit.Revision}, not the one after its parents, {revision}!";
                    return false;
                }

                RoamingNetwork? roamingNetwork;

                if (Commit.IsMerge)
                {

                    if (!IsCanonicalOrder(parents[0], parents[1]))
                    {
                        ErrorResponse = $"The merge commit '{Commit.Id}' does not name its parents in their order: the higher revision first, the lower identification among equal ones!";
                        return false;
                    }

                    if (!TryMerge(parents[0], parents[1], out roamingNetwork, out var mergeChangeSet, out var mergeConflicts, out var mergeError))
                    {
                        ErrorResponse = $"The merge commit '{Commit.Id}' merges conflicting commits: {mergeError ?? String.Join(" ", mergeConflicts.Select(conflict => conflict.Description))}";
                        return false;
                    }

                    if (!JToken.DeepEquals(mergeChangeSet.ToJSON(IncludeSignatures: false), Commit.ChangeSet!.ToJSON(IncludeSignatures: false)))
                    {
                        ErrorResponse = $"The merge commit '{Commit.Id}' is not the merge of its parents!";
                        return false;
                    }

                }

                else if (!TryGetVersion(parents[0].Id, out var parentVersion, out ErrorResponse) ||
                         !parentVersion.TryApplyChangeSet(Commit.ChangeSet!, out roamingNetwork, out ErrorResponse, VerifySignature))
                {
                    return false;
                }

                if (!roamingNetwork.MatchesETags(Commit.ETags))
                {
                    ErrorResponse = "The change set of the commit does not lead to the ETags of the commit!";
                    return false;
                }

                try
                {
                    Take(Commit, roamingNetwork);
                }
                catch (Exception e)
                {
                    ErrorResponse = "The commit can not be stored: " + e.Message;
                    return false;
                }

                if (MergeTips)
                {
                    this.MergeTips();
                    CompactIfDue();
                }

                ErrorResponse = null;
                return true;

            }

        }

        #endregion

        #region TryAppend(Commits, out Appended, out ErrorResponse, VerifySignature = null, VerifyCommitSignature = null, RequiredSignatureRoles = null)

        /// <summary>
        /// Append the given commits of another server in their order, parents before their children,
        /// until the first one that can not be appended, and merge the tips afterwards:
        /// not in between, so that this server makes the merges the other one made.
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

            lock (historyLock)
            {

                try
                {

                    foreach (var commit in Commits)
                    {

                        if (!TryAppend(commit, false, false, out ErrorResponse, VerifySignature, VerifyCommitSignature, RequiredSignatureRoles))
                        {
                            ErrorResponse = $"Commit {Appended}: {ErrorResponse}";
                            return false;
                        }

                        Appended++;

                    }

                }
                finally
                {
                    if (Appended > 0)
                    {
                        MergeTips();
                        CompactIfDue();
                    }
                }

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

                try
                {
                    StoreAppend(RoamingNetworkHistoryLogEntry.ForSignatures(CommitId, newSignatures));
                }
                catch (Exception e)
                {
                    ErrorResponse = "The signatures can not be stored: " + e.Message;
                    return false;
                }

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


        #region (private) Take(Commit, RoamingNetwork)

        /// <summary>
        /// Take the given commit and the version it leads to, while the history is locked:
        /// remember both, update the tips and the head, follow the status of the entities of the head, and announce it.
        /// </summary>
        private void Take(RoamingNetworkCommit  Commit,
                          RoamingNetwork        RoamingNetwork)
        {

            // Kept before it is shown: a store failing keeps it from the history.
            StoreAppend(RoamingNetworkHistoryLogEntry.ForCommit(Commit));

            Remember(Commit, RoamingNetwork);

            foreach (var parentId in Commit.ParentIds)
                tips.Remove(parentId);

            tips.Add(Commit.Id);
            tips.Sort((tip1, tip2) => IsCanonicalOrder(commits[commitIndex[tip1]], commits[commitIndex[tip2]]) ? (tip1 == tip2 ? 0 : -1) : 1);

            var newHead = new RoamingNetworkHead(commits[commitIndex[tips[0]]], VersionOf(tips[0]));

            if (newHead.Commit.Id != Head.Commit.Id)
            {
                Volatile.Write(ref head, newHead);
                RuntimeStatus.Attach(newHead.RoamingNetwork);
            }

            RetainVersions();

            var announcement = new RoamingNetworkCommitAnnouncement(Commit, Timestamp.Now);

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

        #region (private) MergeTips()

        /// <summary>
        /// Merge the tips, while the history is locked: the first two that do not conflict, in their order,
        /// again and again, until none are left that do not conflict. Every server holding the same commits
        /// makes the same merge commits. The conflicts of the tips left are kept.
        /// </summary>
        private void MergeTips()
        {

            var found = new List<RoamingNetworkMergeConflict>();

            var merged = true;

            while (merged && tips.Count > 1)
            {

                merged = false;
                found.Clear();

                for (var i = 0; i < tips.Count && !merged; i++)
                {
                    for (var j = i + 1; j < tips.Count && !merged; j++)
                    {

                        var left   = commits[commitIndex[tips[i]]];
                        var right  = commits[commitIndex[tips[j]]];

                        if (TryMerge(left, right, out var roamingNetwork, out var changeSet, out var mergeConflicts, out var mergeError))
                        {

                            try
                            {
                                Take(SignedBy(Signers, RoamingNetworkCommit.Merge(left, right, changeSet, roamingNetwork)), roamingNetwork);
                            }
                            catch (Exception e)
                            {
                                // Made again when the history is loaded, or with the next commit.
                                RaiseStoreError("A merge commit can not be stored: " + e.Message, e);
                                return;
                            }

                            merged = true;

                        }

                        else
                            found.AddRange(mergeError is not null
                                               ? [ new RoamingNetworkMergeConflict(RoamingNetworkMergeConflictKind.InvalidResult, nameof(RoamingNetwork), RoamingNetworkId.ToString(), null, null, null, null, mergeError) ]
                                               : mergeConflicts);

                    }
                }

            }

            ImmutableInterlocked.InterlockedExchange(ref conflicts, tips.Count > 1 ? [ .. found ] : []);

        }

        #endregion

        #region (private) TryMerge(Left, Right, out RoamingNetwork, out ChangeSet, out Conflicts, out ErrorResponse)

        /// <summary>
        /// Merge the version of the right commit into the one of the left commit, with their merge base.
        /// </summary>
        private Boolean TryMerge(RoamingNetworkCommit                                     Left,
                                 RoamingNetworkCommit                                     Right,
                                 [NotNullWhen(true)]  out RoamingNetwork?                 RoamingNetwork,
                                 [NotNullWhen(true)]  out RoamingNetworkChangeSet?        ChangeSet,
                                 out IReadOnlyList<RoamingNetworkMergeConflict>           Conflicts,
                                 out String?                                              ErrorResponse)
        {

            RoamingNetwork  = null;
            ChangeSet       = null;
            Conflicts       = [];

            if (!TryGetMergeBase(Left.Id, Right.Id, 0, out var mergeBase, out ErrorResponse) ||
                !TryGetVersion  (Left.Id,  out var leftVersion,  out ErrorResponse) ||
                !TryGetVersion  (Right.Id, out var rightVersion, out ErrorResponse))
            {
                return false;
            }

            return POI.RoamingNetwork.TryMerge(mergeBase,
                                               leftVersion,
                                               rightVersion,
                                               MergeChangeSetId(Left.Id, Right.Id),
                                               out RoamingNetwork,
                                               out ChangeSet,
                                               out Conflicts);

        }

        #endregion

        #region (private) TryGetMergeBase(Left, Right, Depth, out MergeBase, out ErrorResponse)

        /// <summary>
        /// The version of the best common ancestor of the given commits: one no other common ancestor follows.
        /// Several of them are merged into a virtual one, as git's recursive strategy does.
        /// </summary>
        private Boolean TryGetMergeBase(RoamingNetworkCommit_Id                   Left,
                                        RoamingNetworkCommit_Id                   Right,
                                        Int32                                     Depth,
                                        [NotNullWhen(true)]  out RoamingNetwork?  MergeBase,
                                        out String?                               ErrorResponse)
        {

            MergeBase      = null;
            ErrorResponse  = null;

            var common     = Ancestors(Left);
            common.IntersectWith(Ancestors(Right));

            if (common.Count == 0)
            {
                ErrorResponse = $"The commits '{Left}' and '{Right}' have no common ancestor here!";
                return false;
            }

            var notBest    = new HashSet<RoamingNetworkCommit_Id>();

            foreach (var commitId in common)
                foreach (var parentId in commits[commitIndex[commitId]].ParentIds)
                    if (commitIndex.ContainsKey(parentId))
                        notBest.UnionWith(Ancestors(parentId));

            var best       = common.Where(commitId => !notBest.Contains(commitId)).
                                    Select(commitId => commits[commitIndex[commitId]]).
                                    ToList();

            best.Sort((commit1, commit2) => IsCanonicalOrder(commit1, commit2) ? (commit1.Id == commit2.Id ? 0 : -1) : 1);

            if (best.Count == 1)
                return TryGetVersion(best[0].Id, out MergeBase, out ErrorResponse);

            if (Depth > 16)
            {
                ErrorResponse = $"The merge base of the commits '{Left}' and '{Right}' is too deeply nested!";
                return false;
            }

            // Several best common ancestors: merged one after the other, as long as they do not conflict.
            if (!TryGetVersion(best[0].Id, out var virtualBase, out ErrorResponse))
                return false;

            foreach (var other in best.Skip(1))
            {

                if (!TryGetMergeBase(best[0].Id, other.Id, Depth + 1, out var innerBase, out ErrorResponse) ||
                    !TryGetVersion  (other.Id, out var otherVersion, out ErrorResponse))
                {
                    return false;
                }

                if (!POI.RoamingNetwork.TryMerge(innerBase, virtualBase, otherVersion, MergeChangeSetId(best[0].Id, other.Id), out var merged, out _, out var innerConflicts))
                {
                    ErrorResponse = $"The common ancestors of the commits '{Left}' and '{Right}' conflict: {String.Join(" ", innerConflicts.Select(conflict => conflict.Description))}";
                    return false;
                }

                virtualBase = merged;

            }

            MergeBase = virtualBase;
            return true;

        }

        #endregion

        #region (private) Ancestors(CommitId)

        /// <summary>
        /// The given commit and all its ancestors this history holds.
        /// </summary>
        private HashSet<RoamingNetworkCommit_Id> Ancestors(RoamingNetworkCommit_Id CommitId)
        {

            var ancestors  = new HashSet<RoamingNetworkCommit_Id>();
            var pending    = new Stack<RoamingNetworkCommit_Id>([ CommitId ]);

            while (pending.TryPop(out var commitId))
                if (commitIndex.TryGetValue(commitId, out var index) && ancestors.Add(commitId))
                    foreach (var parentId in commits[index].ParentIds)
                        pending.Push(parentId);

            return ancestors;

        }

        #endregion

        #region (static) IsCanonicalOrder(Commit1, Commit2)

        /// <summary>
        /// Whether the first commit comes before the second one, or is it: by the higher revision,
        /// and among equal ones by the lower identification. The head is the first tip, a merge commit
        /// names the first of its parents first.
        /// </summary>
        public static Boolean IsCanonicalOrder(RoamingNetworkCommit  Commit1,
                                               RoamingNetworkCommit  Commit2)

            => Commit1.Revision != Commit2.Revision
                   ? Commit1.Revision > Commit2.Revision
                   : String.CompareOrdinal(Commit1.Id.ToString(), Commit2.Id.ToString()) <= 0;

        #endregion

        #region (static) MergeChangeSetId(Left, Right)

        /// <summary>
        /// The identification of the change set of the merge of the given commits: the same for everyone.
        /// </summary>
        public static ChangeSet_Id MergeChangeSetId(RoamingNetworkCommit_Id  Left,
                                                    RoamingNetworkCommit_Id  Right)

            => ChangeSet_Id.Parse(MergeChangeSetIdPrefix +
                                  Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{Left}+{Right}")))[..32]);

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
        /// Try to return the commits a server holding the given one and its ancestors misses, completely,
        /// parents before their children, at most the given number. Fetch more from the last one returned.
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

                var held       = Ancestors(CommitId);

                Commits        = commits.Where(commit => !held.Contains(commit.Id)).Take(Math.Max(0, MaxCount)).ToArray();
                ErrorResponse  = null;
                return true;

            }

        }

        #endregion

        #region HaveIds()

        /// <summary>
        /// The commits to tell another server this history holds, so that it can send the ones missing here:
        /// the tips, the latest commits, older ones at growing distances, and the root.
        /// It may send some held already; appending them again changes nothing.
        /// </summary>
        public IReadOnlyList<RoamingNetworkCommit_Id> HaveIds()
        {

            lock (historyLock)
            {

                var have  = new List<RoamingNetworkCommit_Id>(tips);
                var step  = 1;

                for (var index = commits.Count - 1; index >= 0; index -= step)
                {

                    have.Add(commits[index].Id);

                    if (have.Count > 16)
                        step *= 2;

                }

                have.Add(commits[0].Id);

                return [ .. have.Distinct() ];

            }

        }

        #endregion

        #region TryGetMissingCommits(Have, out Commits, out ErrorResponse, MaxCount = DefaultMaxCommits)

        /// <summary>
        /// Try to return the commits a server holding the given commits and their ancestors misses, completely,
        /// parents before their children, at most the given number; commits unknown here are skipped.
        /// None of them known here requires a new snapshot.
        /// </summary>
        /// <param name="Have">The commits the caller holds, e.g. its tips.</param>
        /// <param name="Commits">The commits it misses.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="MaxCount">The maximum number of commits to return.</param>
        public Boolean TryGetMissingCommits(IEnumerable<RoamingNetworkCommit_Id>                         Have,
                                            [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkCommit>?  Commits,
                                            [NotNullWhen(false)] out String?                           ErrorResponse,
                                            Int32                                                      MaxCount   = DefaultMaxCommits)
        {

            lock (historyLock)
            {

                var known = Have.Where(commitIndex.ContainsKey).ToArray();

                if (known.Length == 0)
                {
                    Commits        = null;
                    ErrorResponse  = "None of the given commits is known here; start from a new snapshot!";
                    return false;
                }

                var held = new HashSet<RoamingNetworkCommit_Id>();

                foreach (var commitId in known)
                    held.UnionWith(Ancestors(commitId));

                Commits        = commits.Where(commit => !held.Contains(commit.Id)).Take(Math.Max(0, MaxCount)).ToArray();
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
        /// Hold the given commit and the version it leads to, and remember its change set as committed:
        /// by its own identification and by those of the change sets it was rebased from.
        /// </summary>
        private void Remember(RoamingNetworkCommit  Commit,
                              RoamingNetwork        RoamingNetwork)
        {

            commitIndex[Commit.Id]  = commits.Count;
            versions   [Commit.Id]  = RoamingNetwork;
            commits.Add(Commit);

            if (Commit.ChangeSet is RoamingNetworkChangeSet changeSet && !Commit.IsMerge)
                foreach (var changeSetId in ChangeSetIds(changeSet))
                    committedChangeSets.TryAdd(changeSetId, Commit.Id);

        }

        #endregion

        #region (private) FindCommitted(ChangeSet)

        /// <summary>
        /// The commit of the given change set, or of another rebased version of it, if it was committed before.
        /// </summary>
        private RoamingNetworkCommit? FindCommitted(RoamingNetworkChangeSet ChangeSet)
        {

            foreach (var changeSetId in ChangeSetIds(ChangeSet))
                if (committedChangeSets.TryGetValue(changeSetId, out var commitId))
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
