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
        public RoamingNetworkCommit  Root                { get; }

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

        #endregion

        #region Constructor(s)

        private RoamingNetworkHistory(RoamingNetworkCommit  Root,
                                      RoamingNetwork        RoamingNetwork)
        {

            this.RoamingNetworkId  = RoamingNetwork.Id;
            this.Root              = Root;
            this.head              = new RoamingNetworkHead(Root, RoamingNetwork);
            this.RuntimeStatus     = new RoamingNetworkRuntimeStatus(this);

            RuntimeStatus.Attach(RoamingNetwork);

            Remember(Root);

        }

        #endregion


        #region (static) Start        (RoamingNetwork)

        /// <summary>
        /// Start the history of the given first version of a roaming network with its genesis commit.
        /// </summary>
        /// <param name="RoamingNetwork">The first version of a roaming network.</param>
        public static RoamingNetworkHistory Start(RoamingNetwork RoamingNetwork)

            => new (RoamingNetworkCommit.Genesis(RoamingNetwork),
                    RoamingNetwork);

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

            RoamingNetwork.Revision  = Commit.Revision;
            RoamingNetwork.CalcETags();

            History        = new RoamingNetworkHistory(Commit, RoamingNetwork);
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region ToSnapshotJSON()

        /// <summary>
        /// A snapshot of the head: its commit and the complete version of the roaming network,
        /// from which another server starts its history.
        /// </summary>
        public JObject ToSnapshotJSON()
        {

            var head = Head;

            return new JObject(
                       new JProperty("commit",          head.Commit.ToJSON()),
                       new JProperty("roamingNetwork",  head.RoamingNetwork.ToJSON())
                   );

        }

        #endregion


        #region ToSnapshotCBOR()

        /// <summary>
        /// A snapshot of the head in CBOR: its commit and the complete version of the roaming network,
        /// with the keys of the JSON snapshot. Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        public CBORValue ToSnapshotCBOR()
        {

            var head = Head;

            return CBORValue.FromMap([
                       new (CBORValue.FromText("commit"),          head.Commit.ToCBOR()),
                       new (CBORValue.FromText("roamingNetwork"),  head.RoamingNetwork.ToCBOR())
                   ]);

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
                                       out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

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

                    changeSet = rebasedChangeSet;
                    rebased   = true;

                }

                if (!head.RoamingNetwork.TryApplyChangeSet(changeSet, out var roamingNetwork, out var applyError, VerifySignature))
                    return new (RoamingNetworkPublishStatus.Refused, head, ErrorResponse: applyError);

                var commit  = RoamingNetworkCommit.Next(head.Commit, changeSet, roamingNetwork);
                var newHead = new RoamingNetworkHead(commit, roamingNetwork);

                Advance(newHead);

                return new (RoamingNetworkPublishStatus.Published, newHead, commit, rebased);

            }

        }

        #endregion

        #region TryAppend(Commit,  out ErrorResponse, VerifySignature = null)

        /// <summary>
        /// Append the given commit of the leading server to this history: on the other servers.
        /// It must follow the head, have the next revision, and its change set must lead
        /// to a version with its ETags. A commit this history holds already is accepted without a change.
        /// </summary>
        /// <param name="Commit">The commit.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        public Boolean TryAppend(RoamingNetworkCommit                Commit,
                                 [NotNullWhen(false)] out String?    ErrorResponse,
                                 VerifyChangeSetSignatureDelegate?   VerifySignature   = null)
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
                {
                    ErrorResponse = null;
                    return true;
                }

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

        #region TryAppend(Commits, out Appended, out ErrorResponse, VerifySignature = null)

        /// <summary>
        /// Append the given commits of the leading server in their order, until the first one that can not be appended.
        /// </summary>
        /// <param name="Commits">The commits.</param>
        /// <param name="Appended">The number of commits appended or held already.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifySignature">A verifier of signatures using trusted keys, required for signed change sets.</param>
        public Boolean TryAppend(IEnumerable<RoamingNetworkCommit>  Commits,
                                 out Int32                          Appended,
                                 [NotNullWhen(false)] out String?   ErrorResponse,
                                 VerifyChangeSetSignatureDelegate?  VerifySignature   = null)
        {

            Appended = 0;

            foreach (var commit in Commits)
            {

                if (!TryAppend(commit, out ErrorResponse, VerifySignature))
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
