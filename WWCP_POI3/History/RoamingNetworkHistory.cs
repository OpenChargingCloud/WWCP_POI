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
        Refused,

        /// <summary>
        /// The change set was made for another version than the head, and its commit would conflict with the head
        /// (SPECIFICATION.md, section 5.3): nothing changed. Its owner prepares it again for the head.
        /// </summary>
        Conflict

    }


    /// <summary>
    /// The result of committing a change set.
    /// </summary>
    /// <param name="Status">The outcome.</param>
    /// <param name="Head">The head afterwards.</param>
    /// <param name="Commit">The commit of the change set, when committed now or before.</param>
    /// <param name="ErrorResponse">Why it was not committed.</param>
    /// <param name="Conflicts">The conflicts with the head, when its commit would conflict with it.</param>
    public sealed record RoamingNetworkCommitResult(RoamingNetworkCommitStatus                  Status,
                                                    RoamingNetworkHead                          Head,
                                                    RoamingNetworkCommit?                       Commit          = null,
                                                    String?                                     ErrorResponse   = null,
                                                    IReadOnlyList<RoamingNetworkMergeConflict>? Conflicts       = null)
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
    /// A server takes no conflict from a change set: one made for an older version or another tip is committed only
    /// when its commit merges with the head. Conflicting tips come only from commits of different servers, and stay
    /// until an owner resolves the conflict with a change set on one of them.
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
        /// The default maximum number of commits a history names as held, and a server takes as held from another one
        /// (HaveIds(), TryGetMissingCommits(…)).
        /// </summary>
        public const Int32 DefaultMaxHaveIds = 1000;

        /// <summary>
        /// The prefix of the identification of the change set of a merge commit.
        /// </summary>
        public const String MergeChangeSetIdPrefix = "merge-";

        /// <summary>
        /// The default maximum number of revisions the version a change set is made for may be behind the head.
        /// </summary>
        public const UInt64 DefaultMaxRevisionsBehindHead = 1000;

        private readonly Lock                                                       historyLock          = new ();
        private readonly List<RoamingNetworkCommit>                                 commits              = [];
        private readonly Dictionary<RoamingNetworkCommit_Id, Int32>                 commitIndex          = [];
        private readonly Dictionary<RoamingNetworkCommit_Id, RoamingNetwork>        versions             = [];
        private readonly Dictionary<ChangeSet_Id, RoamingNetworkCommit_Id>          committedChangeSets  = [];
        private readonly List<RoamingNetworkCommit_Id>                              tips                 = [];
        private          RoamingNetworkHead                                         head;
        private          ImmutableArray<RoamingNetworkMergeConflict>                conflicts            = [];

        /// <summary>
        /// The conflicts of the pairs of tips that do not merge, by the pair, in their order: a merge depends on the two
        /// commits alone, so it is calculated once, not again with every commit, and forgotten once one of them is no tip.
        /// </summary>
        private readonly Dictionary<(RoamingNetworkCommit_Id Left, RoamingNetworkCommit_Id Right),
                                    IReadOnlyList<RoamingNetworkMergeConflict>>     conflictingTips      = [];

        private          Int64                                                      mergeCalculations;
        private          Int64                                                      commitsLookedAt;

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys of its snapshots, in JSON and CBOR.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the time a snapshot was made.
            /// </summary>
            public const String  Timestamp             = "timestamp";

            /// <summary>
            /// The property of the commit of a snapshot.
            /// </summary>
            public const String  Commit                = "commit";

            /// <summary>
            /// The property of the version of the roaming network of a snapshot.
            /// </summary>
            public const String  RoamingNetwork        = "roamingNetwork";

            /// <summary>
            /// The property of the effective last changes of a snapshot.
            /// </summary>
            public const String  EffectiveLastChanges  = "effectiveLastChanges";

            /// <summary>
            /// The property of the runtime status of a snapshot.
            /// </summary>
            public const String  RuntimeStatus         = "runtimeStatus";

            /// <summary>
            /// The property of the attestations of the head of a snapshot, valid when it was made.
            /// </summary>
            public const String  Attestations          = "attestations";

            /// <summary>
            /// The property of the checkpoint of a snapshot started from a checkpoint.
            /// </summary>
            public const String  Checkpoint            = "checkpoint";

            /// <summary>
            /// The property of the epoch of a snapshot.
            /// </summary>
            public const String  Epoch                 = EpochJSONProperty;

        }

        #endregion


        /// <summary>
        /// The roaming network of this history.
        /// </summary>
        public RoamingNetwork_Id     RoamingNetworkId    { get; }

        /// <summary>
        /// The first commit this history holds: a genesis commit, or the head of the snapshot it started from.
        /// </summary>
        public RoamingNetworkCommit  Root
            => View.Commits[0];

        /// <summary>
        /// The first tip and the version it leads to: the commit of the highest revision,
        /// of the lowest identification among equal ones. The runtime status follows its entities.
        /// </summary>
        public RoamingNetworkHead    Head
            => Volatile.Read(ref head);

        /// <summary>
        /// The tips: the commits no other commit follows, the head first.
        /// More than one, while they conflict. Read without the lock: the versions of the tips are always kept.
        /// </summary>
        public IReadOnlyList<RoamingNetworkHead>  Tips
        {
            get
            {

                var view = View;

                return [ .. view.Tips.Select(tip => new RoamingNetworkHead(view.Commits[view.CommitIndex[tip]],
                                                                           TryGetVersion(view, tip, out var version, out var errorResponse)
                                                                               ? version
                                                                               : throw new InvalidOperationException(errorResponse))) ];

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
        /// How many revisions the version a change set is made for may be behind the head; DefaultMaxRevisionsBehindHead
        /// by default. A change set made for an older version is refused: it is prepared again for the head.
        /// Commits appended from other servers are not bound by it.
        /// </summary>
        public UInt64                MaxRevisionsBehindHead    { get; set; } = DefaultMaxRevisionsBehindHead;

        /// <summary>
        /// The number of merges this history calculated: of two tips, also of those that conflict, and of the head with
        /// the commit of a change set made for another version. A pair of tips that conflicts is not merged again until
        /// one of them is no tip any longer.
        /// </summary>
        public Int64                 MergeCalculations
            => Interlocked.Read(ref mergeCalculations);

        /// <summary>
        /// The number of commits looked at to find the commits missing elsewhere, TryGetCommitsSince(…) and
        /// TryGetMissingCommits(…): about those returned, and not the whole history for every page.
        /// </summary>
        public Int64                 CommitsLookedAt
            => Interlocked.Read(ref commitsLookedAt);

        /// <summary>
        /// The number of commits this history holds, including its root.
        /// </summary>
        public Int32                 Count
            => View.Commits.Count;

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

        private RoamingNetworkHistory(RoamingNetworkCommit          Root,
                                      RoamingNetwork                RoamingNetwork,
                                      RoamingNetworkEpoch?          Epoch              = null,
                                      RoamingNetworkEpochLink?      PreviousEpoch      = null,
                                      RoamingNetworkEpochProfiles?  PreviousProfiles   = null)
        {

            this.RoamingNetworkId  = RoamingNetwork.Id;
            this.head              = new RoamingNetworkHead(Root, RoamingNetwork);
            this.attestedHead      = this.head;
            this.RuntimeStatus     = new RoamingNetworkRuntimeStatus(this);
            this.epoch             = Epoch ?? (Root.Profiles is not null ? new RoamingNetworkEpoch(Root.Epoch, Root.Profiles) : RoamingNetworkEpoch.First);
            this.previousEpoch     = PreviousEpoch;
            this.previousProfiles  = PreviousProfiles;

            if (Root.Epoch != epoch.Number || Root.IdentificationHash != epoch.Profiles.IdentificationHash)
                throw new ArgumentException($"The root '{Root.Id}' is no commit of the epoch {epoch.Number}.", nameof(Root));

            RuntimeStatus.Attach(RoamingNetwork);
            RuntimeStatus.OnStatusUpdate += AppendStatusUpdate;
            RuntimeStatus.OnStatusUpdate += PriceStatusChanged;

            Remember(Root, RoamingNetwork);
            tips.Add(Root.Id);
            PublishTips();

        }

        #endregion


        #region (static) Start        (RoamingNetwork, Signers = null, Store = null, Retention = null, Archive = null)

        /// <summary>
        /// Start the history of the given first version of a roaming network with its genesis commit.
        /// </summary>
        /// <param name="RoamingNetwork">The first version of a roaming network.</param>
        /// <param name="Signers">The optional keys that sign the genesis commit and every commit made here.</param>
        /// <param name="Store">An optional empty store to keep the history in.</param>
        /// <param name="Retention">What to keep; everything, unless given.</param>
        /// <param name="Archive">Where to keep the commits dropped when compacting.</param>
        public static RoamingNetworkHistory Start(RoamingNetwork                            RoamingNetwork,
                                                  IEnumerable<RoamingNetworkCommitSigner>?  Signers     = null,
                                                  IRoamingNetworkHistoryStore?              Store       = null,
                                                  RoamingNetworkHistoryRetention?           Retention   = null,
                                                  IRoamingNetworkHistoryArchive?            Archive     = null)
        {

            var signers = Signers?.ToImmutableArray() ?? [];

            var history = new RoamingNetworkHistory(SignedBy(signers, RoamingNetworkCommit.Genesis(RoamingNetwork)),
                                                    RoamingNetwork) {
                              Signers    = signers,
                              Retention  = Retention ?? RoamingNetworkHistoryRetention.KeepEverything,
                              Archive    = Archive
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

                if (Snapshot?[PropertyKeys.Commit] is not JObject commitJSON ||
                    !RoamingNetworkCommit.TryParse(commitJSON, out var commit, out var commitError, Limits))
                {
                    ErrorResponse = "Missing or invalid commit of the snapshot!";
                    return false;
                }

                if (Snapshot[PropertyKeys.RoamingNetwork] is not JObject roamingNetworkJSON)
                {
                    ErrorResponse = "Missing roaming network of the snapshot!";
                    return false;
                }

                if (!RoamingNetwork.TryParse(roamingNetworkJSON, out var roamingNetwork, out var roamingNetworkError))
                {
                    ErrorResponse = "Invalid roaming network of the snapshot: " + roamingNetworkError;
                    return false;
                }

                // The last changes of its effective members as its history left them: they never go back.
                if (!roamingNetwork.TryTakeEffectiveFloors(Snapshot[PropertyKeys.EffectiveLastChanges], out var effectiveLastChangesError))
                {
                    ErrorResponse = effectiveLastChangesError;
                    return false;
                }

                // The epoch of a history after the first one: { epoch, profiles, previous?, previousProfiles? }.
                RoamingNetworkEpoch?          epoch             = null;
                RoamingNetworkEpochLink?      previousEpoch     = null;
                RoamingNetworkEpochProfiles?  previousProfiles  = null;

                if (Snapshot[EpochJSONProperty] is JToken epochJSON)
                {

                    if (!RoamingNetworkEpoch.TryParse(epochJSON, out epoch, out var epochError))
                    {
                        ErrorResponse = "Invalid epoch of the snapshot: " + epochError;
                        return false;
                    }

                    if (epochJSON[RoamingNetworkEpochLink.JSONProperty] is JToken previousEpochJSON &&
                        !RoamingNetworkEpochLink.TryParse(previousEpochJSON, out previousEpoch, out var previousEpochError))
                    {
                        ErrorResponse = "Invalid epoch of the snapshot: " + previousEpochError;
                        return false;
                    }

                    if (epochJSON[PreviousProfilesJSONProperty] is JToken previousProfilesJSON &&
                        !RoamingNetworkEpochProfiles.TryParse(previousProfilesJSON, out previousProfiles, out var previousProfilesError))
                    {
                        ErrorResponse = "Invalid epoch of the snapshot: " + previousProfilesError;
                        return false;
                    }

                }

                return TryFromSnapshot(roamingNetwork,
                                       commit,
                                       out History,
                                       out ErrorResponse,
                                       epoch,
                                       previousEpoch,
                                       previousProfiles) &&

                       TryTakeRuntimeStatus(ref History,
                                            Snapshot[PropertyKeys.RuntimeStatus],
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

                if (POIJSON.Read(Text) is not JObject snapshot)
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

        #region (static) TryFromSnapshot(RoamingNetwork, Commit, out History, out ErrorResponse, Epoch = null, PreviousEpoch = null, PreviousProfiles = null)

        /// <summary>
        /// Try to start a history from the given version of a roaming network and the commit leading to it.
        /// The roaming network must have the ETags of the commit.
        /// </summary>
        /// <param name="RoamingNetwork">The version of the roaming network.</param>
        /// <param name="Commit">The commit leading to it.</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Epoch">The epoch of the history, unless the first one or the one its genesis names.</param>
        /// <param name="PreviousEpoch">The final checkpoint of the epoch before, if known.</param>
        /// <param name="PreviousProfiles">The profiles of the epoch before, if known, from the third epoch on: those of the first one are the default ones.</param>
        public static Boolean TryFromSnapshot(RoamingNetwork                                   RoamingNetwork,
                                              RoamingNetworkCommit                             Commit,
                                              [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                              [NotNullWhen(false)] out String?                 ErrorResponse,
                                              RoamingNetworkEpoch?                             Epoch              = null,
                                              RoamingNetworkEpochLink?                         PreviousEpoch      = null,
                                              RoamingNetworkEpochProfiles?                     PreviousProfiles   = null)
        {

            History = null;

            if (RoamingNetwork.Id != Commit.RoamingNetworkId)
            {
                ErrorResponse = $"The roaming network '{RoamingNetwork.Id}' is not the one of the commit, '{Commit.RoamingNetworkId}'!";
                return false;
            }

            if (!RoamingNetwork.MatchesCommitETags(Commit.ETags))
            {
                ErrorResponse = "The roaming network does not have the ETags of the commit!";
                return false;
            }

            var epoch = Epoch ?? (Commit.Profiles is not null ? new RoamingNetworkEpoch(Commit.Epoch, Commit.Profiles) : RoamingNetworkEpoch.First);

            if (Commit.Epoch != epoch.Number || Commit.IdentificationHash != epoch.Profiles.IdentificationHash ||
                (Commit.Profiles is not null && !Commit.Profiles.Equals(epoch.Profiles)))
            {
                ErrorResponse = $"The commit '{Commit.Id}' is no commit of the epoch {epoch.Number} of the snapshot, with its profiles!";
                return false;
            }

            if (PreviousEpoch is not null && PreviousEpoch.Epoch != epoch.Number - 1)
            {
                ErrorResponse = $"The epoch before the epoch {epoch.Number} is not the epoch {PreviousEpoch.Epoch}!";
                return false;
            }

            if (PreviousProfiles is not null && epoch.Number <= 2)
            {
                ErrorResponse = epoch.Number == 1
                                    ? "The first epoch has no epoch before, nor its profiles!"
                                    : "The epoch before the epoch 2 is the first one, of the default profiles: they are not given!";
                return false;
            }

            // A copy, when it needs another revision: the given version stays as it is.
            var roamingNetwork = RoamingNetwork.WithRevision(Commit.Revision);
            roamingNetwork.CalcETags();

            History        = new RoamingNetworkHistory(Commit, roamingNetwork, epoch, PreviousEpoch, PreviousProfiles);
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region ToSnapshotJSON(IncludeRuntimeStatus = true, IncludeAttestations = false)

        /// <summary>
        /// A snapshot of the head, now and complete: its commit, the complete version of the roaming network,
        /// and the current admin status and status of its entities, from which another server starts its history.
        /// The commit and the network can be verified by the ETags of the commit; the runtime status is not content.
        /// With its attestations, it is a snapshot of the head as far as it is attested here, with the attestations of
        /// it valid now, so that whoever starts from it checks it against the trust anchor of the roaming network
        /// (SPECIFICATION.md, section 10.5); the head a moment later may not be attested yet.
        /// </summary>
        /// <param name="IncludeRuntimeStatus">Whether to include the runtime status.</param>
        /// <param name="IncludeAttestations">Whether to include the attestations of the head, unless none.</param>
        public JObject ToSnapshotJSON(Boolean IncludeRuntimeStatus  = true,
                                      Boolean IncludeAttestations   = false)
        {

            var (head, attestations) = IncludeAttestations
                                           ? AttestedHead(null)
                                           : (Head, (IReadOnlyList<RoamingNetworkAttestation>) []);

            var json = new JObject(
                           new JProperty(PropertyKeys.Timestamp,       Timestamp.Now.ToISO8601()),
                           new JProperty(PropertyKeys.Commit,          head.Commit.ToJSON(Embedded: true)),
                           new JProperty(PropertyKeys.RoamingNetwork,  head.RoamingNetwork.ToJSON())
                       );

            // Not content, but part of the version as its history calculated it.
            if (head.RoamingNetwork.EffectiveFloorsJSON() is JArray effectiveLastChanges)
                json.Add(PropertyKeys.EffectiveLastChanges, effectiveLastChanges);

            if (epoch.Number > 1)
                json.Add(EpochJSONProperty, EpochJSON(epoch, previousEpoch, PreviousEpochProfiles));

            if (IncludeRuntimeStatus)
                json.Add(PropertyKeys.RuntimeStatus, new JArray(RoamingNetworkRuntimeStatus.Of(head.RoamingNetwork).Select(status => status.ToJSON(Embedded: true))));

            if (attestations.Count > 0)
                json.Add(PropertyKeys.Attestations,  new JArray(attestations.Select(attestation => attestation.ToJSON(Embedded: true))));

            // The roaming network and the runtime status write whole numbers as integers themselves (POIJSON.Number);
            // this holds for anything else in it, too, so that ToString() of it is read back with its ETags.
            return POIJSON.WholeNumbersAsIntegers(json);

        }

        #endregion


        #region ToSnapshotCBOR(IncludeRuntimeStatus = true, IncludeAttestations = false)

        /// <summary>
        /// A snapshot of the head in CBOR, see ToSnapshotJSON(), with the keys of the JSON snapshot.
        /// Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        /// <param name="IncludeRuntimeStatus">Whether to include the runtime status.</param>
        /// <param name="IncludeAttestations">Whether to include the attestations of the head, unless none.</param>
        public CBORValue ToSnapshotCBOR(Boolean IncludeRuntimeStatus  = true,
                                        Boolean IncludeAttestations   = false)
        {

            var (head, attestations) = IncludeAttestations
                                           ? AttestedHead(null)
                                           : (Head, (IReadOnlyList<RoamingNetworkAttestation>) []);

            var entries  = new List<KeyValuePair<CBORValue, CBORValue>> {
                               new (CBORValue.FromText(PropertyKeys.Timestamp),       CBORValue.FromText(Timestamp.Now.ToISO8601())),
                               new (CBORValue.FromText(PropertyKeys.Commit),          head.Commit.ToCBOR(Embedded: true)),
                               new (CBORValue.FromText(PropertyKeys.RoamingNetwork),  head.RoamingNetwork.ToCBOR())
                           };

            if (head.RoamingNetwork.EffectiveFloorsJSON() is JArray effectiveLastChanges)
                entries.Add(new (CBORValue.FromText(PropertyKeys.EffectiveLastChanges), CBORJSON.ToCBOR(effectiveLastChanges)));

            if (IncludeRuntimeStatus)
                entries.Add(new (CBORValue.FromText(PropertyKeys.RuntimeStatus),
                                 CBORJSON.ToCBOR(new JArray(RoamingNetworkRuntimeStatus.Of(head.RoamingNetwork).Select(status => status.ToJSON(Embedded: true))))));

            if (attestations.Count > 0)
                entries.Add(new (CBORValue.FromText(PropertyKeys.Attestations),
                                 CBORValue.FromArray(attestations.Select(attestation => attestation.ToCBOR(Embedded: true)))));

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
                    !Snapshot.TryGetValue(CBORValue.FromText(PropertyKeys.Commit), out var commitCBOR) ||
                    !RoamingNetworkCommit.TryParse(commitCBOR, out var commit, out var commitError, Limits))
                {
                    ErrorResponse = "Missing or invalid commit of the snapshot!";
                    return false;
                }

                if (!Snapshot.TryGetValue(CBORValue.FromText(PropertyKeys.RoamingNetwork), out var roamingNetworkCBOR))
                {
                    ErrorResponse = "Missing roaming network of the snapshot!";
                    return false;
                }

                if (!RoamingNetwork.TryParse(roamingNetworkCBOR, out var roamingNetwork, out var roamingNetworkError))
                {
                    ErrorResponse = "Invalid roaming network of the snapshot: " + roamingNetworkError;
                    return false;
                }

                // The last changes of its effective members as its history left them: they never go back.
                if (!roamingNetwork.TryTakeEffectiveFloors(Snapshot.TryGetValue(CBORValue.FromText(PropertyKeys.EffectiveLastChanges), out var effectiveLastChangesCBOR)
                                                               ? CBORJSON.ToJSON(effectiveLastChangesCBOR)
                                                               : null,
                                                           out var effectiveLastChangesError))
                {
                    ErrorResponse = effectiveLastChangesError;
                    return false;
                }

                return TryFromSnapshot(roamingNetwork,
                                       commit,
                                       out History,
                                       out ErrorResponse) &&

                       TryTakeRuntimeStatus(ref History,
                                            Snapshot.TryGetValue(CBORValue.FromText(PropertyKeys.RuntimeStatus), out var runtimeStatusCBOR)
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


        #region Commit   (ChangeSet, VerifySignature = null, At = null)

        /// <summary>
        /// How much later than the clock of this server the creation of a change set it takes may be.
        /// </summary>
        public static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Commit the given change set, signed by its owners, to the version it was made for:
        /// the head, another tip or an older commit, which then starts a branch.
        /// The keys it is signed by must be valid at its creation and when it is taken, by the clock of this server, and it
        /// must not be created later than then; made for an older version, it must not be signed by a key a tip it will be
        /// merged with removed, or which expired there (SPECIFICATION.md, sections 4.1 and 5.3).
        /// Made for another version than the head, that version must be at most MaxRevisionsBehindHead revisions behind
        /// the head and follow the commit of the latest checkpoint, and its commit must merge with the head without
        /// conflict (SPECIFICATION.md, section 5.3); otherwise it is refused, as a conflict with the conflicts: a server
        /// takes no conflict from a change set, its owner prepares it again for the head.
        /// The new commit is merged with the other tips where it does not conflict.
        /// It must be identified by its content and have changes; a change set committed before, also in a rebased
        /// version, is not committed again (SPECIFICATION.md, section 4).
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="VerifySignature">An optional verifier of its signatures, in addition to those of its owners.</param>
        /// <param name="At">When it is taken, now by default.</param>
        public RoamingNetworkCommitResult Commit(RoamingNetworkChangeSet            ChangeSet,
                                                 VerifyChangeSetSignatureDelegate?  VerifySignature   = null,
                                                 DateTimeOffset?                    At                = null)
        {

            lock (historyLock)
            {

                if (ChangeSet is null)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: "The given change set must not be null!");

                if (ChangeSet.RoamingNetworkId != RoamingNetworkId)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: $"The change set is for the roaming network '{ChangeSet.RoamingNetworkId}', not for '{RoamingNetworkId}'!");

                if (ClosedError() is String closed)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: closed);

                foreach (var signature in ChangeSet.Signatures)
                    if (SignatureAlgorithmError(signature.Algorithm, "a signature of the change set") is String changeSetSignatureError)
                        return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: changeSetSignatureError);

                if (ChangeSetSignatureError(ChangeSet) is String tooManySignatures)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: tooManySignatures);

                foreach (var signer in Signers.IsDefault ? [] : Signers.AsEnumerable())
                    if (SignatureAlgorithmError(signer.Algorithm.Name, $"the signer {signer}") is String signerError)
                        return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: signerError);

                // Identified by its content: committed before is then this change set, or a rebased one of it.
                if (ChangeSetError(ChangeSet) is String changeSetError)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: changeSetError);

                if (FindCommitted(ChangeSet) is RoamingNetworkCommit committed)
                    return new (RoamingNetworkCommitStatus.AlreadyCommitted, Head, committed);

                // The version it was made for: a tip, the head first, or else the latest commit having it, not too old.
                if (FindVersionFor(ChangeSet, out var staleError) is not (RoamingNetworkCommit parent, RoamingNetwork parentVersion))
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: staleError ?? "The change set fits no version of this history: prepare it again for the head!");

                // Taken now: its keys valid also now, by the clock of this server, so that a creation chosen earlier does
                // not let a key sign once it expired; and created no later than now.
                var at = At ?? Timestamp.Now;

                if (ChangeSet.CreatedAt > at + MaxClockSkew)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: $"The change set was created at {ChangeSet.CreatedAt.ToISO8601()}, later than now, {at.ToISO8601()}!");

                if (!parentVersion.TryCheckOwnership(ChangeSet, out var keyError, [ ChangeSet.CreatedAt, at ]))
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: keyError);

                if (!parentVersion.TryApplyChangeSet(ChangeSet, out var roamingNetwork, out var applyError, VerifySignature))
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: applyError);

                if (VersionEpochError(roamingNetwork) is String versionError)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: versionError);

                var unsigned = RoamingNetworkCommit.Next(parent, ChangeSet, roamingNetwork);

                MergedTips? mergedWithHead = null;

                // Made for another version than the head: its commit must merge with the head, so that no change set makes
                // a tip conflicting with the head, which might become the head itself (SPECIFICATION.md, section 5.3).
                if (parent.Id != Head.Commit.Id &&
                    ConflictsWithHead(unsigned, roamingNetwork, out mergedWithHead) is { Count: > 0 } headConflicts)
                {
                    return new (RoamingNetworkCommitStatus.Conflict,
                                Head,
                                ErrorResponse:  $"The change set, made for the version '{parent.Id}', conflicts with the head '{Head.Commit.Id}': " +
                                                $"{String.Join(" ", headConflicts.Select(conflict => conflict.Description))} Prepare it again for the head!",
                                Conflicts:      headConflicts);
                }

                // Made for an older version: its preconditions must also hold for the other tips it will be merged with,
                // and its keys must not be ones they removed, or which expired there.
                if (BrokenPreconditionOf(ChangeSet, parent.Id) is String preconditionError)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: preconditionError);

                if (RevokedKeyOfTips(ChangeSet, parent.Id) is String revokedKeyError)
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: revokedKeyError);

                var commit = SignedBy(Signers, unsigned);

                try
                {
                    Take(commit, roamingNetwork);
                }
                catch (Exception e)
                {
                    return new (RoamingNetworkCommitStatus.Refused, Head, ErrorResponse: "The commit can not be stored: " + e.Message);
                }

                // Its merge with the head is calculated already.
                MergeTips(mergedWithHead);
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
        /// Its revision is one more than the highest one of its parents. It must be signed in every required role,
        /// and its own signatures must all be valid in the version it leads to, the one calculated for it.
        /// Afterwards, the tips are merged where they do not conflict.
        /// A commit this history holds already is accepted, taking the signatures it has in addition.
        /// Unlike a change set, it is appended also when it conflicts with the head, and whatever version it follows:
        /// the other server took it by its own head, and refusing it here would part the histories for good.
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

                // Whether it is signed as required first: its signatures are verified in its version, once calculated.
                if (!Known && SignedAsRequiredError(Commit, VerifyCommitSignature, RequiredSignatureRoles) is String signedError)
                {
                    ErrorResponse = signedError;
                    return false;
                }

                if (!Known && (ClosedError() ?? CommitEpochError(Commit)) is String epochError)
                {
                    ErrorResponse = epochError;
                    return false;
                }

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

                    if (!JToken.DeepEquals(mergeChangeSet.ToJSON(Embedded: true, IncludeSignatures: false), Commit.ChangeSet!.ToJSON(Embedded: true, IncludeSignatures: false)))
                    {
                        ErrorResponse = $"The merge commit '{Commit.Id}' is not the merge of its parents!";
                        return false;
                    }

                }

                else if (ChangeSetError(Commit.ChangeSet!) is String changeSetError)
                {
                    ErrorResponse = changeSetError;
                    return false;
                }

                else if (!TryGetVersion(parents[0].Id, out var parentVersion, out ErrorResponse) ||
                         !parentVersion.TryApplyChangeSet(Commit.ChangeSet!, out roamingNetwork, out ErrorResponse, VerifySignature))
                {
                    return false;
                }

                if (!roamingNetwork.MatchesCommitETags(Commit.ETags))
                {
                    ErrorResponse = "The change set of the commit does not lead to the ETags of the commit!";
                    return false;
                }

                if (!Known && VersionEpochError(roamingNetwork) is String versionError)
                {
                    ErrorResponse = versionError;
                    return false;
                }

                // Its signatures by the keys of the version it leads to.
                if (!Known && !VerifySignatures(Commit, roamingNetwork, VerifyCommitSignature, out ErrorResponse))
                    return false;

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
        /// Every new signature must be valid in the version of the commit; signatures the commit has already are skipped.
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
                    if (SignatureAlgorithmError(signature.Algorithm, $"a signature of the commit '{CommitId}'") is String algorithmError)
                    {
                        ErrorResponse = algorithmError;
                        return false;
                    }

                // Its version is calculated only for a verifier to check them in.
                if (VerifyCommitSignature is null)
                {
                    ErrorResponse = $"The commit '{CommitId}' is signed, but there is no verifier of commit signatures!";
                    return false;
                }

                if (!TryGetVersion(CommitId, out var version, out ErrorResponse))
                    return false;

                foreach (var signature in newSignatures)
                    if (!CheckSignature(commit, version, signature, VerifyCommitSignature, out ErrorResponse))
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
                PublishReplacedCommit(index, commit);

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

        #region (static) CheckSignatures(Commit, Version, VerifyCommitSignature, RequiredSignatureRoles, out ErrorResponse)

        /// <summary>
        /// Check that the given commit is signed in every required role, and that every signature of it is valid
        /// in the given version, the one it leads to, e.g. the head commit of a snapshot in the version of the snapshot.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        /// <param name="Version">The version of the roaming network the commit leads to.</param>
        /// <param name="VerifyCommitSignature">A verifier of commit signatures using trusted keys, required for signed commits.</param>
        /// <param name="RequiredSignatureRoles">The roles the commit must be signed in, e.g. leader.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean CheckSignatures(RoamingNetworkCommit              Commit,
                                              RoamingNetwork                    Version,
                                              VerifyCommitSignatureDelegate?    VerifyCommitSignature,
                                              IReadOnlyCollection<String>?      RequiredSignatureRoles,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (SignedAsRequiredError(Commit, VerifyCommitSignature, RequiredSignatureRoles) is String signedError)
            {
                ErrorResponse = signedError;
                return false;
            }

            return VerifySignatures(Commit, Version, VerifyCommitSignature, out ErrorResponse);

        }

        #endregion

        #region (private static) SignedAsRequiredError(Commit, VerifyCommitSignature, RequiredSignatureRoles)

        /// <summary>
        /// Why the given commit is not signed as required, if it is not, without verifying any signature:
        /// signed without a verifier of commit signatures, or not signed in a required role.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        /// <param name="VerifyCommitSignature">A verifier of commit signatures using trusted keys, required for signed commits.</param>
        /// <param name="RequiredSignatureRoles">The roles the commit must be signed in, e.g. leader.</param>
        private static String? SignedAsRequiredError(RoamingNetworkCommit            Commit,
                                                     VerifyCommitSignatureDelegate?  VerifyCommitSignature,
                                                     IReadOnlyCollection<String>?    RequiredSignatureRoles)
        {

            if (Commit.Signatures.Length > 0 && VerifyCommitSignature is null)
                return $"The commit '{Commit.Id}' is signed, but there is no verifier of commit signatures!";

            foreach (var role in RequiredSignatureRoles ?? [])
                if (!Commit.Signatures.Any(signature => signature.Role == role))
                    return $"The commit '{Commit.Id}' is not signed in the role '{role}'!";

            return null;

        }

        #endregion

        #region (private static) VerifySignatures(Commit, Version, VerifyCommitSignature, out ErrorResponse)

        /// <summary>
        /// Verify every signature of the given commit in the given version, the one it leads to.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        /// <param name="Version">The version of the roaming network the commit leads to.</param>
        /// <param name="VerifyCommitSignature">A verifier of commit signatures using trusted keys, required for signed commits.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        private static Boolean VerifySignatures(RoamingNetworkCommit              Commit,
                                                RoamingNetwork                    Version,
                                                VerifyCommitSignatureDelegate?    VerifyCommitSignature,
                                                [NotNullWhen(false)] out String?  ErrorResponse)
        {

            foreach (var signature in Commit.Signatures)
                if (!CheckSignature(Commit, Version, signature, VerifyCommitSignature, out ErrorResponse))
                    return false;

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) CheckSignature(Commit, Version, Signature, VerifyCommitSignature, out ErrorResponse)

        private static Boolean CheckSignature(RoamingNetworkCommit              Commit,
                                              RoamingNetwork                    Version,
                                              RoamingNetworkCommitSignature     Signature,
                                              VerifyCommitSignatureDelegate?    VerifyCommitSignature,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (VerifyCommitSignature is null)
            {
                ErrorResponse = $"The commit '{Commit.Id}' is signed, but there is no verifier of commit signatures!";
                return false;
            }

            if (!VerifyCommitSignature(Commit, Version, Signature))
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

            // The view before the head: every commit a reader sees as the head is in it.
            PublishTips();

            var newHead = new RoamingNetworkHead(commits[commitIndex[tips[0]]], VersionOf(tips[0]));

            var headChanged = newHead.Commit.Id != Head.Commit.Id;

            if (headChanged)
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

            // Attested after it was announced, so that whoever learns of the attestation knows the commit; until then,
            // readers take the head attested before.
            if (headChanged)
            {
                AttestNewHead();
                Volatile.Write(ref attestedHead, newHead);
            }

        }

        #endregion

        #region (private) MergeTips(Merged = null)

        /// <summary>
        /// Merge the tips, while the history is locked: the first two that do not conflict, in their order,
        /// again and again, until none are left that do not conflict. Every server holding the same commits
        /// makes the same merge commits. The conflicts of the tips left are kept.
        /// A pair of tips that conflicts is not merged again while both are tips: its conflicts are remembered,
        /// so that conflicting tips do not make every later commit merge every pair of them anew.
        /// </summary>
        /// <param name="Merged">The merge of two of the tips calculated already, e.g. of the head and a new commit.</param>
        private void MergeTips(MergedTips? Merged = null)
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

                        if (conflictingTips.TryGetValue((tips[i], tips[j]), out var knownConflicts))
                        {
                            found.AddRange(knownConflicts);
                            continue;
                        }

                        var left   = commits[commitIndex[tips[i]]];
                        var right  = commits[commitIndex[tips[j]]];

                        Boolean                                     mergeable;
                        RoamingNetwork?                             roamingNetwork;
                        RoamingNetworkChangeSet?                    changeSet;
                        IReadOnlyList<RoamingNetworkMergeConflict>  mergeConflicts  = [];
                        String?                                     mergeError      = null;

                        // Calculated already: the same, as a merge depends on the two commits alone.
                        if (Merged is not null && Merged.Left == left.Id && Merged.Right == right.Id)
                        {
                            mergeable       = true;
                            roamingNetwork  = Merged.RoamingNetwork;
                            changeSet       = Merged.ChangeSet;
                            Merged          = null;
                        }

                        else
                        {
                            Interlocked.Increment(ref mergeCalculations);
                            mergeable = TryMerge(left, right, out roamingNetwork, out changeSet, out mergeConflicts, out mergeError);
                        }

                        if (mergeable && roamingNetwork is not null && changeSet is not null)
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
                        {

                            IReadOnlyList<RoamingNetworkMergeConflict> pairConflicts = mergeError is not null
                                                                                           ? [ InvalidResult(mergeError) ]
                                                                                           : mergeConflicts;

                            conflictingTips[(left.Id, right.Id)] = pairConflicts;
                            found.AddRange(pairConflicts);

                        }

                    }
                }

            }

            // Remembered only while both are tips.
            foreach (var pair in conflictingTips.Keys.ToArray())
                if (!tips.Contains(pair.Left) || !tips.Contains(pair.Right))
                    conflictingTips.Remove(pair);

            ImmutableInterlocked.InterlockedExchange(ref conflicts, tips.Count > 1 ? [ .. found ] : []);

        }

        #endregion

        #region (private) ConflictsWithHead(Commit, RoamingNetwork, out Merged)

        /// <summary>
        /// The merge of two tips calculated before they are both tips, by their identifications in their order.
        /// </summary>
        private sealed record MergedTips(RoamingNetworkCommit_Id   Left,
                                         RoamingNetworkCommit_Id   Right,
                                         RoamingNetwork            RoamingNetwork,
                                         RoamingNetworkChangeSet   ChangeSet);


        /// <summary>
        /// The conflicts of the given commit, not taken yet, with the head, as merging them would find them (TryMerge):
        /// none, when they merge, and their merge then, which MergeTips() takes once the commit is taken, as the first
        /// two tips that do not conflict are the head and the commit. The commit and its version are held only while the
        /// merge is calculated, and nobody sees them: the history is locked, and the view is not published.
        /// </summary>
        private IReadOnlyList<RoamingNetworkMergeConflict> ConflictsWithHead(RoamingNetworkCommit  Commit,
                                                                             RoamingNetwork        RoamingNetwork,
                                                                             out MergedTips?       Merged)
        {

            var head = Head.Commit;

            Merged = null;

            commitIndex[Commit.Id]  = commits.Count;
            versions   [Commit.Id]  = RoamingNetwork;
            commits.Add(Commit);

            try
            {

                Interlocked.Increment(ref mergeCalculations);

                var (left, right) = IsCanonicalOrder(head, Commit) ? (head, Commit) : (Commit, head);

                if (TryMerge(left, right, out var merge, out var changeSet, out var mergeConflicts, out var mergeError))
                {
                    Merged = new MergedTips(left.Id, right.Id, merge, changeSet);
                    return [];
                }

                return mergeError is not null
                           ? [ InvalidResult(mergeError) ]
                           : mergeConflicts;

            }
            finally
            {
                commits.RemoveAt(commits.Count - 1);
                commitIndex.Remove(Commit.Id);
                versions.   Remove(Commit.Id);
            }

        }

        #endregion

        #region (private) InvalidResult(Description)

        /// <summary>
        /// A merge that can not be calculated, or whose result is invalid, as a conflict of the roaming network.
        /// </summary>
        private RoamingNetworkMergeConflict InvalidResult(String Description)

            => new (RoamingNetworkMergeConflictKind.InvalidResult, nameof(RoamingNetwork), RoamingNetworkId.ToString(), null, null, null, null, Description);

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

            // One walk for the merge base and the commits of each branch.
            var ancestry = CommonAncestryOf(Left.Id, Right.Id);

            if (!TryGetMergeBase(Left.Id, Right.Id, ancestry.Best, 0, out var mergeBase, out ErrorResponse) ||
                !TryGetVersion  (Left.Id,  out var leftVersion,  out ErrorResponse) ||
                !TryGetVersion  (Right.Id, out var rightVersion, out ErrorResponse))
            {
                return false;
            }

            // A change set of one branch naming an entity as it must still be, which the other branch changed.
            var broken = BrokenPreconditions(ancestry, mergeBase, leftVersion, rightVersion);

            if (broken.Count > 0)
            {
                Conflicts = broken;
                return false;
            }

            // A change set of one branch signed by a key the other branch removed, or which expired there.
            if (RevokedKeys(ancestry, leftVersion, rightVersion, out ErrorResponse) is not List<RoamingNetworkMergeConflict> revoked)
                return false;

            if (revoked.Count > 0)
            {
                Conflicts = revoked;
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

        #region (private) BrokenPreconditions(Ancestry, MergeBase, LeftVersion, RightVersion)

        /// <summary>
        /// The preconditions of the change sets of each branch since the merge base the other branch broke, as conflicts:
        /// a change set applies only while the entities it names are as it read them, also when it is merged.
        /// </summary>
        private List<RoamingNetworkMergeConflict> BrokenPreconditions(CommonAncestry                  Ancestry,
                                                                      POI.RoamingNetwork.MergeBase    MergeBase,
                                                                      POI.RoamingNetwork              LeftVersion,
                                                                      POI.RoamingNetwork              RightVersion)
        {

            var conflicts = new List<RoamingNetworkMergeConflict>();

            void Check(IEnumerable<RoamingNetworkCommit_Id>  BranchOnly,
                       POI.RoamingNetwork                    Other,
                       String                                OtherName)
            {

                IReadOnlyDictionary<POI.RoamingNetwork.MergeKey, POI.RoamingNetwork.MergeEntity>? otherEntities = null;

                foreach (var commitId in BranchOnly.OrderBy(commitId => commitId.ToString(), StringComparer.Ordinal))
                {

                    if (commits[commitIndex[commitId]].ChangeSet is not RoamingNetworkChangeSet changeSet ||
                        changeSet.Preconditions.IsDefaultOrEmpty)
                    {
                        continue;
                    }

                    otherEntities ??= POI.RoamingNetwork.EntitiesOf(Other);

                    foreach (var (precondition, what) in POI.RoamingNetwork.BrokenPreconditions(changeSet.Preconditions, MergeBase, otherEntities))
                        conflicts.Add(new RoamingNetworkMergeConflict(RoamingNetworkMergeConflictKind.Precondition,
                                                                      precondition.EntityType,
                                                                      precondition.EntityId,
                                                                      null,
                                                                      null,
                                                                      null,
                                                                      null,
                                                                      $"The change set '{changeSet.Id}' requires the {precondition.EntityType} '{precondition.EntityId}' to have the ETag {precondition.ETag}, but the {OtherName} version {what} since their common ancestor!"));

                }

            }

            Check(Ancestry.RightOnly, LeftVersion,  "left");
            Check(Ancestry.LeftOnly,  RightVersion, "right");

            return conflicts;

        }

        #endregion

        #region (private) RevokedKeys(Ancestry, LeftVersion, RightVersion, out ErrorResponse)

        /// <summary>
        /// The change sets of each branch since their best common ancestors signed by a key the other branch removed, or
        /// which expired there, as conflicts (RevokedKeyOf, SPECIFICATION.md section 5.3); null when the versions of the
        /// common ancestors are not to be had.
        /// </summary>
        private List<RoamingNetworkMergeConflict>? RevokedKeys(CommonAncestry       Ancestry,
                                                               POI.RoamingNetwork   LeftVersion,
                                                               POI.RoamingNetwork   RightVersion,
                                                               out String?          ErrorResponse)
        {

            if (!TryGetVersions(Ancestry.Best, out var bases, out ErrorResponse))
                return null;

            var conflicts = new List<RoamingNetworkMergeConflict>();

            void Check(IEnumerable<RoamingNetworkCommit_Id>  BranchOnly,
                       POI.RoamingNetwork                    Other,
                       String                                OtherName)
            {
                foreach (var commitId in BranchOnly.OrderBy(commitId => commitId.ToString(), StringComparer.Ordinal))
                    if (commits[commitIndex[commitId]] is { ParentIds.Length: 1, ChangeSet: RoamingNetworkChangeSet changeSet } &&
                        RevokedKeyOf(changeSet, Other, OtherName, bases) is (POIPartyId party, String description))
                    {
                        conflicts.Add(new RoamingNetworkMergeConflict(RoamingNetworkMergeConflictKind.RevokedKey,
                                                                      party.EntityType,
                                                                      party.EntityId,
                                                                      null,
                                                                      null,
                                                                      null,
                                                                      null,
                                                                      description));
                    }
            }

            Check(Ancestry.RightOnly, LeftVersion,  "left version");
            Check(Ancestry.LeftOnly,  RightVersion, "right version");

            ErrorResponse = null;
            return conflicts;

        }

        #endregion

        #region (private static) RevokedKeyOf(ChangeSet, Other, OtherName, Bases)

        /// <summary>
        /// The party and why, when the given change set of one branch is signed by a key the given versions of the best
        /// common ancestors name for a party, which the other version no longer names for it, or names as not valid at the
        /// later of the creation of the change set and the last change of the other version (SPECIFICATION.md, section 5.3):
        /// a key removed or expired on one branch, e.g. a signing key whose certificate was removed, carries no change set of
        /// the other into the merge. Keys a branch added itself since the common ancestors are not compared.
        /// </summary>
        private static (POIPartyId Party, String Description)? RevokedKeyOf(RoamingNetworkChangeSet           ChangeSet,
                                                                            POI.RoamingNetwork                Other,
                                                                            String                            OtherName,
                                                                            IReadOnlyList<POI.RoamingNetwork>  Bases)
        {

            var at = ChangeSet.CreatedAt > Other.LastChangeDate ? ChangeSet.CreatedAt : Other.LastChangeDate;

            foreach (var signature in ChangeSet.Signatures)
                foreach (var @base in Bases)
                    foreach (var (partyId, party) in @base.Parties)
                        foreach (var (kind, keys) in new[] { (POIKeyKind.Signing, party.SigningKeys), (POIKeyKind.Identity, party.IdentityKeys) })
                        {

                            // A party the other removed conflicts as its removal does, with what the change set changes.
                            if (!keys.Any(key => key.Id == signature.KeyId) ||
                                !Other.TryGetParty(partyId, out var otherParty))
                            {
                                continue;
                            }

                            var otherKey = (kind == POIKeyKind.Signing ? otherParty.SigningKeys : otherParty.IdentityKeys).FirstOrDefault(key => key.Id == signature.KeyId);

                            if (otherKey is null)
                                return (partyId, $"The change set '{ChangeSet.Id}' is signed by the key '{signature.KeyId}' of {partyId}, which the {OtherName} no longer names!");

                            if (!otherKey.IsValidAt(at))
                                return (partyId, $"The change set '{ChangeSet.Id}' is signed by the key '{signature.KeyId}' of {partyId}, which is not valid in the {OtherName} at {at.ToISO8601()}!");

                        }

            return null;

        }

        #endregion

        #region (private) RevokedKeyOfTips(ChangeSet, ParentId)

        /// <summary>
        /// Why the given change set, made for the given commit, would conflict with a tip it will be merged with, as a merge
        /// would find it (RevokedKeyOf): signed by a key that tip removed, or which expired there; none when it would not.
        /// </summary>
        private String? RevokedKeyOfTips(RoamingNetworkChangeSet  ChangeSet,
                                         RoamingNetworkCommit_Id  ParentId)
        {

            foreach (var tip in tips.Where(tip => !IsOrFollows(ParentId, tip)))
            {

                if (!TryGetBestCommonAncestorVersions(ParentId, tip, out var bases, out var error) ||
                    !TryGetVersion(tip, out var tipVersion, out error))
                {
                    return error;
                }

                if (RevokedKeyOf(ChangeSet, tipVersion, $"version '{tip}'", bases) is (_, String revoked))
                    return $"{revoked} Prepare it again for the head!";

            }

            return null;

        }

        #endregion

        #region (private) TryGetBestCommonAncestorVersions(Left, Right, out Versions, out ErrorResponse)

        /// <summary>
        /// The versions of the best common ancestors of the given commits: those no other common ancestor follows.
        /// </summary>
        private Boolean TryGetBestCommonAncestorVersions(RoamingNetworkCommit_Id                              Left,
                                                         RoamingNetworkCommit_Id                              Right,
                                                         out IReadOnlyList<POI.RoamingNetwork>                Versions,
                                                         [NotNullWhen(false)] out String?                     ErrorResponse)

            => TryGetVersions(BestCommonAncestors(Left, Right), out Versions, out ErrorResponse);

        #endregion

        #region (private) TryGetVersions(Commits, out Versions, out ErrorResponse)

        /// <summary>
        /// The versions of the given commits, in their order.
        /// </summary>
        private Boolean TryGetVersions(IEnumerable<RoamingNetworkCommit>                    Commits,
                                       out IReadOnlyList<POI.RoamingNetwork>                Versions,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            var versions = new List<POI.RoamingNetwork>();
            Versions     = versions;

            foreach (var commit in Commits)
            {

                if (!TryGetVersion(commit.Id, out var version, out ErrorResponse))
                    return false;

                versions.Add(version);

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private) BestCommonAncestors(Left, Right)

        /// <summary>
        /// The best common ancestors of the given commits, in their order: the common ancestors no other common ancestor
        /// follows; none, when they have no common ancestor here.
        /// </summary>
        private List<RoamingNetworkCommit> BestCommonAncestors(RoamingNetworkCommit_Id  Left,
                                                               RoamingNetworkCommit_Id  Right)

            => CommonAncestryOf(Left, Right).Best;

        #endregion

        #region (private) CommonAncestryOf(Left, Right)

        /// <summary>
        /// The best common ancestors of two commits, and the commits only one of them is or follows.
        /// </summary>
        /// <param name="Best">The best common ancestors, in their order: the common ancestors no other common ancestor follows.</param>
        /// <param name="LeftOnly">The commits the left commit is or follows, and the right one not.</param>
        /// <param name="RightOnly">The commits the right commit is or follows, and the left one not.</param>
        private sealed record CommonAncestry(List<RoamingNetworkCommit>        Best,
                                             HashSet<RoamingNetworkCommit_Id>  LeftOnly,
                                             HashSet<RoamingNetworkCommit_Id>  RightOnly);


        /// <summary>
        /// The common ancestry of the given commits, as this history holds them, found by one walk from both, the higher
        /// revisions first, as git finds a merge base: a commit both reach is a common ancestor, and every commit it follows
        /// is no best one. As a parent has a lower revision than its children, every commit is complete once it is reached
        /// in the walk, and the walk ends once only commits following a common ancestor are left: it goes back no further
        /// than the branches of the two commits since their common ancestors, not to the root.
        /// </summary>
        private CommonAncestry CommonAncestryOf(RoamingNetworkCommit_Id  Left,
                                                RoamingNetworkCommit_Id  Right)
        {

            const Byte LeftFlag   = 1;
            const Byte RightFlag  = 2;
            const Byte Both       = LeftFlag | RightFlag;
            const Byte Stale      = 4;

            var best       = new List<RoamingNetworkCommit>();
            var leftOnly   = new HashSet<RoamingNetworkCommit_Id>();
            var rightOnly  = new HashSet<RoamingNetworkCommit_Id>();

            var flags      = new Dictionary<RoamingNetworkCommit_Id, Byte>();
            var queue      = new PriorityQueue<RoamingNetworkCommit_Id, UInt64>(Comparer<UInt64>.Create((revision1, revision2) => revision2.CompareTo(revision1)));
            var nonStale   = 0;

            // A commit is reached only from its children, of higher revisions: never again once it was taken from the queue.
            void Reach(RoamingNetworkCommit_Id  CommitId,
                       Byte                     Flags)
            {

                if (!commitIndex.TryGetValue(CommitId, out var index))
                    return;

                if (flags.TryGetValue(CommitId, out var before))
                {

                    if ((before & Stale) == 0 && (Flags & Stale) != 0)
                        nonStale--;

                    flags[CommitId] = (Byte) (before | Flags);

                }

                else
                {

                    flags[CommitId] = Flags;
                    queue.Enqueue(CommitId, commits[index].Revision);

                    if ((Flags & Stale) == 0)
                        nonStale++;

                }

            }

            Reach(Left,  LeftFlag);
            Reach(Right, RightFlag);

            while (nonStale > 0 && queue.TryDequeue(out var commitId, out _))
            {

                var commitFlags = flags[commitId];

                if ((commitFlags & Stale) == 0)
                {

                    nonStale--;

                    switch (commitFlags)
                    {

                        // A common ancestor no other one follows: those it follows are no best ones.
                        case Both:
                            best.Add(commits[commitIndex[commitId]]);
                            commitFlags |= Stale;
                            break;

                        case LeftFlag:
                            leftOnly. Add(commitId);
                            break;

                        default:
                            rightOnly.Add(commitId);
                            break;

                    }

                }

                foreach (var parentId in commits[commitIndex[commitId]].ParentIds)
                    Reach(parentId, commitFlags);

            }

            best.Sort((commit1, commit2) => IsCanonicalOrder(commit1, commit2) ? (commit1.Id == commit2.Id ? 0 : -1) : 1);

            return new (best, leftOnly, rightOnly);

        }

        #endregion

        #region (private) BrokenPreconditionOf(ChangeSet, ParentId)

        /// <summary>
        /// Why the preconditions of the given change set, made for the given commit, do not hold for a tip it will be
        /// merged with: one that changed or removed an entity it names since their common ancestor; none when they hold.
        /// </summary>
        private String? BrokenPreconditionOf(RoamingNetworkChangeSet  ChangeSet,
                                             RoamingNetworkCommit_Id  ParentId)
        {

            if (ChangeSet.Preconditions.IsDefaultOrEmpty)
                return null;

            foreach (var tip in tips.Where(tip => !IsOrFollows(ParentId, tip)))
            {

                if (!TryGetMergeBase(ParentId, tip, 0, out var mergeBase, out var mergeBaseError) ||
                    !TryGetVersion  (tip, out var tipVersion, out mergeBaseError))
                {
                    return mergeBaseError;
                }

                foreach (var (precondition, what) in POI.RoamingNetwork.BrokenPreconditions(ChangeSet.Preconditions, mergeBase, POI.RoamingNetwork.EntitiesOf(tipVersion)))
                    return $"The {precondition.EntityType} '{precondition.EntityId}' the change set requires to have the ETag {precondition.ETag} is no longer as it was: the version '{tip}' {what} since the one the change set was made for. Prepare it again for the head!";

            }

            return null;

        }

        #endregion

        #region (private) TryGetMergeBase(Left, Right, Depth, out MergeBase, out ErrorResponse)

        /// <summary>
        /// The version of the best common ancestor of the given commits: one no other common ancestor follows.
        /// Several of them, in their order, are merged into a virtual one, as git's recursive strategy does:
        /// where they conflict, it has conflict markers, so that the merge conflicts there unless both commits agree.
        /// </summary>
        private Boolean TryGetMergeBase(RoamingNetworkCommit_Id                                      Left,
                                        RoamingNetworkCommit_Id                                      Right,
                                        Int32                                                        Depth,
                                        [NotNullWhen(true)]  out POI.RoamingNetwork.MergeBase?       MergeBase,
                                        out String?                                                  ErrorResponse)

            => TryGetMergeBase(Left, Right, BestCommonAncestors(Left, Right), Depth, out MergeBase, out ErrorResponse);


        /// <param name="Left">The left commit.</param>
        /// <param name="Right">The right commit.</param>
        /// <param name="Best">Their best common ancestors, in their order.</param>
        /// <param name="Depth">How deeply the merge bases of best common ancestors are nested.</param>
        /// <param name="MergeBase">The merge base.</param>
        /// <param name="ErrorResponse">Why there is none.</param>
        private Boolean TryGetMergeBase(RoamingNetworkCommit_Id                                      Left,
                                        RoamingNetworkCommit_Id                                      Right,
                                        IReadOnlyList<RoamingNetworkCommit>                          Best,
                                        Int32                                                        Depth,
                                        [NotNullWhen(true)]  out POI.RoamingNetwork.MergeBase?       MergeBase,
                                        out String?                                                  ErrorResponse)
        {

            MergeBase      = null;
            ErrorResponse  = null;

            var best = Best;

            if (best.Count == 0)
            {
                ErrorResponse = $"The commits '{Left}' and '{Right}' have no common ancestor here!";
                return false;
            }

            if (!TryGetVersion(best[0].Id, out var firstVersion, out ErrorResponse))
                return false;

            var virtualBase = POI.RoamingNetwork.MergeBase.Of(firstVersion);

            if (best.Count > 1 && Depth > 16)
            {
                ErrorResponse = $"The merge base of the commits '{Left}' and '{Right}' is too deeply nested!";
                return false;
            }

            // Several best common ancestors: merged one after the other, with conflict markers where they conflict.
            foreach (var other in best.Skip(1))
            {

                if (!TryGetMergeBase(best[0].Id, other.Id, Depth + 1, out var innerBase,    out ErrorResponse) ||
                    !TryGetVersion  (other.Id,                       out var otherVersion, out ErrorResponse))
                {
                    return false;
                }

                virtualBase = POI.RoamingNetwork.MergeBase.Virtual(innerBase, virtualBase, POI.RoamingNetwork.MergeBase.Of(otherVersion));

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

        #region (private) IsOrFollows(CommitId, AncestorId)

        /// <summary>
        /// Whether the given commit is the given ancestor, or follows it, as this history holds them. Only the commits of
        /// higher revisions than the ancestor are walked: a parent has a lower revision than its children.
        /// </summary>
        private Boolean IsOrFollows(RoamingNetworkCommit_Id  CommitId,
                                    RoamingNetworkCommit_Id  AncestorId)

            => IsOrFollows(CommitId, AncestorId, null);


        /// <param name="CommitId">The commit.</param>
        /// <param name="AncestorId">The ancestor.</param>
        /// <param name="View">The view of a reader without the lock; none while the history is locked.</param>
        private Boolean IsOrFollows(RoamingNetworkCommit_Id  CommitId,
                                    RoamingNetworkCommit_Id  AncestorId,
                                    ReadView?                View)
        {

            IReadOnlyDictionary<RoamingNetworkCommit_Id, Int32>  index    = View is null ? commitIndex : View.CommitIndex;
            IReadOnlyList<RoamingNetworkCommit>                  held     = View is null ? commits     : View.Commits;

            if (CommitId == AncestorId)
                return index.ContainsKey(CommitId);

            if (!index.TryGetValue(AncestorId, out var ancestorIndex))
                return false;

            var revision  = held[ancestorIndex].Revision;
            var walked    = new HashSet<RoamingNetworkCommit_Id>();
            var pending   = new Stack<RoamingNetworkCommit_Id>([ CommitId ]);

            while (pending.TryPop(out var commitId))
            {

                if (commitId == AncestorId)
                    return true;

                if (index.TryGetValue(commitId, out var position) &&
                    held[position].Revision > revision              &&
                    walked.Add(commitId))
                {
                    foreach (var parentId in held[position].ParentIds)
                        pending.Push(parentId);
                }

            }

            return false;

        }

        #endregion

        #region (private) StaleVersionError(Commit, View = null)

        /// <summary>
        /// Why a change set may not be made for the version of the given commit, as it is too old: more than
        /// MaxRevisionsBehindHead revisions behind the head, or not following the commit of the latest checkpoint
        /// (SPECIFICATION.md, section 5.3); none for the head, or a commit recent enough.
        /// So no change set starts a branch where the history is long settled, which would keep its tips from being merged
        /// cheaply and the history from being compacted.
        /// </summary>
        /// <param name="Commit">The commit.</param>
        /// <param name="View">The view of a reader without the lock, whose first tip is its head; none while the history is locked.</param>
        private String? StaleVersionError(RoamingNetworkCommit  Commit,
                                          ReadView?             View   = null)
        {

            var head = View is null ? Head.Commit : View.Commits[View.CommitIndex[View.Tips[0]]];

            if (Commit.Id == head.Id)
                return null;

            if (head.Revision > Commit.Revision &&
                head.Revision - Commit.Revision > MaxRevisionsBehindHead)
            {
                return $"The change set was made for the version '{Commit.Id}' of the revision {Commit.Revision}, more than {MaxRevisionsBehindHead} revisions behind the head of the revision {head.Revision}: prepare it again for the head!";
            }

            IReadOnlyList<RoamingNetworkCheckpoint>              checkpointsHeld  = View is null ? checkpoints : View.Checkpoints;
            IReadOnlyDictionary<RoamingNetworkCommit_Id, Int32>  index            = View is null ? commitIndex : View.CommitIndex;

            if (checkpointsHeld.Count > 0                                                   &&
                checkpointsHeld[checkpointsHeld.Count - 1].CommitId is var checkpointCommitId  &&
                index.ContainsKey(checkpointCommitId)                                       &&
                !IsOrFollows(Commit.Id, checkpointCommitId, View))
            {
                return $"The change set was made for the version '{Commit.Id}', which does not follow the commit '{checkpointCommitId}' of the latest checkpoint: prepare it again for the head!";
            }

            return null;

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

            => View.CommitIndex.ContainsKey(CommitId);

        #endregion

        #region TryGetCommit       (CommitId, out Commit)

        /// <summary>
        /// Try to return the given commit completely.
        /// </summary>
        /// <param name="CommitId">A commit identification.</param>
        /// <param name="Commit">The commit.</param>
        public Boolean TryGetCommit(RoamingNetworkCommit_Id                         CommitId,
                                    [NotNullWhen(true)] out RoamingNetworkCommit?   Commit)

            => View.TryGetCommit(CommitId, out Commit);

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

            var missing = View.Missing([ CommitId ], MaxCount, out var lookedAt);

            Interlocked.Add(ref commitsLookedAt, lookedAt);

            if (missing is null)
            {
                Commits        = null;
                ErrorResponse  = $"The commit '{CommitId}' is unknown here; start from a new snapshot!";
                return false;
            }

            Commits        = missing;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region HaveIds(MaxCount = DefaultMaxHaveIds)

        /// <summary>
        /// The commits to tell another server this history holds, so that it can send the ones missing here:
        /// the tips, the latest commits, older ones at growing distances, and the root, at most the given number,
        /// the tips first. It may send some held already; appending them again changes nothing.
        /// </summary>
        /// <param name="MaxCount">The maximum number of commits to name, as a server takes at most DefaultMaxHaveIds.</param>
        public IReadOnlyList<RoamingNetworkCommit_Id> HaveIds(Int32 MaxCount = DefaultMaxHaveIds)
        {

            var view  = View;
            var have  = new List<RoamingNetworkCommit_Id>(view.Tips);
            var step  = 1;

            for (var index = view.Commits.Count - 1; index >= 0; index -= step)
            {

                have.Add(view.Commits[index].Id);

                if (have.Count > 16)
                    step *= 2;

            }

            have.Add(view.Commits[0].Id);

            return [ .. have.Distinct().Take(Math.Max(1, MaxCount)) ];

        }

        #endregion

        #region TryGetMissingCommits(Have, out Commits, out ErrorResponse, MaxCount = DefaultMaxCommits)

        /// <summary>
        /// Try to return the commits a server holding the given commits and their ancestors misses, completely,
        /// parents before their children, at most the given number; commits unknown here are skipped.
        /// None of them known here requires a new snapshot.
        /// It costs about the commits returned, not every commit for every given one nor the whole history for every page
        /// (ReadView.Missing(…)); a server limits how many it takes from another one, e.g. POI_API.MaxHaveIds.
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

            var missing = View.Missing(Have, MaxCount, out var lookedAt);

            Interlocked.Add(ref commitsLookedAt, lookedAt);

            if (missing is null)
            {
                Commits        = null;
                ErrorResponse  = "None of the given commits is known here; start from a new snapshot!";
                return false;
            }

            Commits        = missing;
            ErrorResponse  = null;
            return true;

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
            PublishAddedCommit(Commit, RoamingNetwork);

            if (Commit.ChangeSet is RoamingNetworkChangeSet changeSet && !Commit.IsMerge)
            {

                foreach (var changeSetId in ChangeSetIds(changeSet))
                    committedChangeSets.TryAdd(changeSetId, Commit.Id);

                ForgetPendingChangeSet(Commit, changeSet);

            }

        }

        #endregion

        #region (private static) ChangeSetError(ChangeSet)

        /// <summary>
        /// Why the given change set, not one of a merge, can not be taken: it is not identified by its content, names change
        /// sets it was rebased from that it was not rebased from, or has no changes (SPECIFICATION.md, section 4). Checked
        /// before it is looked for as committed, so that only the change set itself, or a rebased one of it, is.
        /// </summary>
        private static String? ChangeSetError(RoamingNetworkChangeSet ChangeSet)

            => ChangeSet.IdentificationError() ??
                   (ChangeSet.Changes.Length == 0
                        ? $"The change set '{ChangeSet.Id}' has no changes: it would change nothing!"
                        : null);

        #endregion

        #region (private static) ChangeSetSignatureError(ChangeSet)

        /// <summary>
        /// Why the signatures of the given change set can not be taken: more of them than a change set may have, or a key
        /// signing it more than once. Co-signing (SPECIFICATION.md, section 4.2) gathers signatures from several holders,
        /// and every submission is within the limit on its own; their union is checked here, so that neither a change set
        /// waiting for signatures nor the commit carrying them grows past what the store and the peers read back with the
        /// same limit, which would make them refuse it after a restart.
        /// </summary>
        private static String? ChangeSetSignatureError(RoamingNetworkChangeSet ChangeSet)
        {

            if (ChangeSet.Signatures.Length > RoamingNetworkChangeSetLimits.Default.MaxSignatures)
                return $"A change set has at most {RoamingNetworkChangeSetLimits.Default.MaxSignatures} signatures, not {ChangeSet.Signatures.Length}!";

            // A key signs a change set once: a holder renewing a key, or proving a new identity key, signs with that key,
            // not several times with it (SPECIFICATION.md, section 4.1 counts holders, not signatures).
            var keyIds = new HashSet<String>(StringComparer.Ordinal);

            foreach (var signature in ChangeSet.Signatures)
                if (!keyIds.Add(signature.KeyId))
                    return $"The key '{signature.KeyId}' signs the change set '{ChangeSet.Id}' more than once!";

            return null;

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
        /// The identification of the given change set and of those it was rebased from, which it names as they were, as
        /// checked when it was taken (RoamingNetworkChangeSet.IdentificationError).
        /// </summary>
        private static IEnumerable<ChangeSet_Id> ChangeSetIds(RoamingNetworkChangeSet ChangeSet)
        {

            yield return ChangeSet.Id;

            if (ChangeSet.Metadata[RoamingNetworkChangeSet.PropertyKeys.RebasedFrom] is JArray rebasedFrom)
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
