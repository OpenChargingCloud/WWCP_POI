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
    /// Called for every checkpoint a history takes, made here or taken from another server,
    /// while the history is locked: return quickly and do not change the history from here.
    /// </summary>
    /// <param name="Checkpoint">The checkpoint.</param>
    public delegate void OnRoamingNetworkCheckpointDelegate(RoamingNetworkCheckpoint Checkpoint);

    /// <summary>
    /// Called for every checkpoint a history takes new signatures of, e.g. of a witness,
    /// while the history is locked: return quickly and do not change the history from here.
    /// </summary>
    /// <param name="Checkpoint">The checkpoint with all its signatures.</param>
    /// <param name="NewSignatures">The signatures taken now.</param>
    public delegate void OnRoamingNetworkCheckpointSignaturesDelegate(RoamingNetworkCheckpoint                      Checkpoint,
                                                                      IReadOnlyList<RoamingNetworkCommitSignature>  NewSignatures);


    /// <summary>
    /// The checkpoints of a history (SPECIFICATION.md, section 10): one line of signed statements that a commit,
    /// and every commit it follows, is part of the history, signed by the roaming network operator that makes them
    /// and by witnesses that checked them against their own history.
    /// </summary>
    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        private readonly List<RoamingNetworkCheckpoint>                             checkpoints          = [];
        private readonly Dictionary<RoamingNetworkCheckpoint_Id, Int32>             checkpointIndex      = [];

        #endregion

        #region Properties

        /// <summary>
        /// The keys that sign every checkpoint made here, e.g. the one of the roaming network operator.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommitSigner>  CheckpointSigners  { get; set; } = [];

        /// <summary>
        /// The checkpoints this history holds, in their order.
        /// </summary>
        public IReadOnlyList<RoamingNetworkCheckpoint>     Checkpoints
        {
            get
            {
                lock (historyLock)
                {
                    return [ .. checkpoints ];
                }
            }
        }

        /// <summary>
        /// The latest checkpoint this history holds, if any.
        /// </summary>
        public RoamingNetworkCheckpoint?                   LatestCheckpoint
        {
            get
            {
                lock (historyLock)
                {
                    return checkpoints.Count > 0 ? checkpoints[^1] : null;
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Called for every checkpoint this history takes, made here or taken from another server.
        /// </summary>
        public event OnRoamingNetworkCheckpointDelegate?            OnCheckpoint;

        /// <summary>
        /// Called for every checkpoint this history takes new signatures of, e.g. of a witness.
        /// </summary>
        public event OnRoamingNetworkCheckpointSignaturesDelegate?  OnCheckpointSignatures;

        #endregion


        #region TryMakeCheckpoint(out Checkpoint, out ErrorResponse, CommitId = null, CreatedAt = null)

        /// <summary>
        /// Make the next checkpoint, signed by the CheckpointSigners: of the given commit, or of the latest commit all tips follow.
        /// The commit must follow the commit of the latest checkpoint, and this history must hold all commits of the checkpoint:
        /// the first one of a history is made only where the history holds its genesis.
        /// It names the price statements taken since the latest checkpoint, signed before it (SPECIFICATION.md, section 12).
        /// </summary>
        /// <param name="Checkpoint">The new checkpoint.</param>
        /// <param name="ErrorResponse">Why none was made, e.g. as there is no new commit.</param>
        /// <param name="CommitId">The commit, the latest one all tips follow otherwise.</param>
        /// <param name="CreatedAt">When it is made, now otherwise.</param>
        public Boolean TryMakeCheckpoint([NotNullWhen(true)]  out RoamingNetworkCheckpoint?  Checkpoint,
                                         [NotNullWhen(false)] out String?                    ErrorResponse,
                                         RoamingNetworkCommit_Id?                            CommitId    = null,
                                         DateTimeOffset?                                     CreatedAt   = null)
        {

            Checkpoint = null;

            lock (historyLock)
            {

                var commitId = CommitId ?? LatestCommitAllTipsFollow();

                if (!commitIndex.TryGetValue(commitId, out var index))
                {
                    ErrorResponse = $"The commit '{commitId}' is unknown here!";
                    return false;
                }

                var commit  = commits[index];
                var latest  = checkpoints.Count > 0 ? checkpoints[^1] : null;

                if (latest is not null)
                {

                    if (latest.CommitId == commitId)
                    {
                        ErrorResponse = $"The latest checkpoint is of the commit '{commitId}' already!";
                        return false;
                    }

                    if (!Ancestors(commitId).Contains(latest.CommitId))
                    {
                        ErrorResponse = $"The commit '{commitId}' does not follow the commit '{latest.CommitId}' of the latest checkpoint!";
                        return false;
                    }

                }

                if (CommitsOf(commitId, latest?.CommitId) is not IReadOnlyList<RoamingNetworkCommit> commitsOfCheckpoint)
                {
                    ErrorResponse = latest is null
                                        ? "The first checkpoint needs every commit since the genesis, which this history does not hold!"
                                        : "This history does not hold every commit since the latest checkpoint!";
                    return false;
                }

                var createdAt                    = DateTimeOffset.FromUnixTimeMilliseconds((CreatedAt ?? Timestamp.Now).ToUnixTimeMilliseconds());

                // The price statements stay locked until the checkpoint is kept: it names those taken before it.
                lock (priceStatementLock)
                {

                    var priceStatementsOfCheckpoint  = PriceStatementsForCheckpoint(createdAt);

                    var checkpoint                   = RoamingNetworkCheckpoint.Create(RoamingNetworkId,
                                                                                       RoamingNetworkCheckpoint.DefaultEpoch,
                                                                                       latest is null ? 0 : latest.Sequence + 1,
                                                                                       latest?.Id,
                                                                                       commit,
                                                                                       commitsOfCheckpoint,
                                                                                       createdAt,
                                                                                       priceStatementsOfCheckpoint);

                    var signers                      = CheckpointSigners;

                    if (!signers.IsDefaultOrEmpty)
                        checkpoint = checkpoint.WithSignatures(signers.Select(signer => signer.Sign(checkpoint)));

                    TakeCheckpoint(checkpoint, priceStatementsOfCheckpoint);

                    Checkpoint                       = checkpoint;

                }

                ErrorResponse  = null;
                return true;

            }

        }

        #endregion

        #region TryAddCheckpoint (Checkpoint, out ErrorResponse, VerifyCheckpointSignature = null, RequiredSignatureRoles = null, PriceStatements = null)

        /// <summary>
        /// Take a checkpoint of another server, after checking it against this history (SPECIFICATION.md, section 10.1):
        /// its commit is held, has its revision and ETags, follows the commit of the latest checkpoint, its commits are the ones
        /// since, it names the latest checkpoint as the one before and every signature is valid. A checkpoint held already only
        /// brings its signatures in addition. A history without checkpoints takes any one of a commit it holds as its first,
        /// e.g. after it started from a snapshot, without checking its commits it does not hold.
        /// The price statements it names are checked when they are given, TryAddCheckpointPriceStatements(…) takes them later.
        /// </summary>
        /// <param name="Checkpoint">The checkpoint.</param>
        /// <param name="ErrorResponse">Why it was refused.</param>
        /// <param name="VerifyCheckpointSignature">A verifier of checkpoint signatures using trusted keys, required for signed checkpoints.</param>
        /// <param name="RequiredSignatureRoles">The roles a checkpoint must be signed in, e.g. roamingNetworkOperator.</param>
        /// <param name="PriceStatements">The price statements it names, if they are given.</param>
        public Boolean TryAddCheckpoint(RoamingNetworkCheckpoint                    Checkpoint,
                                        [NotNullWhen(false)] out String?            ErrorResponse,
                                        VerifyCheckpointSignatureDelegate?          VerifyCheckpointSignature   = null,
                                        IReadOnlyCollection<String>?                RequiredSignatureRoles      = null,
                                        IEnumerable<RoamingNetworkPriceStatement>?  PriceStatements             = null)
        {

            lock (historyLock)
            {

                if (Checkpoint is null)
                {
                    ErrorResponse = "The given checkpoint must not be null!";
                    return false;
                }

                if (checkpointIndex.ContainsKey(Checkpoint.Id))
                {

                    if (!TryAddCheckpointSignatures(Checkpoint.Id, Checkpoint.Signatures, VerifyCheckpointSignature, out _, out _, out ErrorResponse))
                        return false;

                    return PriceStatements is null || Checkpoint.PriceStatementsSize == 0 ||
                           TryAddCheckpointPriceStatements(Checkpoint.Id, PriceStatements, out ErrorResponse);

                }

                if (!CheckCheckpoint(Checkpoint, out ErrorResponse))
                    return false;

                foreach (var signature in Checkpoint.Signatures)
                    if (!CheckCheckpointSignature(Checkpoint, signature, VerifyCheckpointSignature, out ErrorResponse))
                        return false;

                foreach (var role in RequiredSignatureRoles ?? [])
                    if (!Checkpoint.Signatures.Any(signature => signature.Role == role))
                    {
                        ErrorResponse = $"The checkpoint '{Checkpoint.Id}' is not signed in the role '{role}'!";
                        return false;
                    }

                IReadOnlyList<RoamingNetworkPriceStatement>? priceStatementsOfCheckpoint = null;

                if (PriceStatements is not null && Checkpoint.PriceStatementsSize > 0 &&
                    !CheckCheckpointPriceStatements(Checkpoint, PriceStatements, out priceStatementsOfCheckpoint, out ErrorResponse))
                {
                    return false;
                }

                lock (priceStatementLock)
                {
                    TakeCheckpoint(Checkpoint, priceStatementsOfCheckpoint);
                }

                ErrorResponse = null;
                return true;

            }

        }

        #endregion

        #region TryAddCheckpointSignatures(CheckpointId, Signatures, VerifyCheckpointSignature, out Checkpoint, out NewSignatures, out ErrorResponse)

        /// <summary>
        /// Add the given signatures to a checkpoint of this history, e.g. of witnesses. Every new signature must be valid;
        /// signatures the checkpoint has already are skipped. The identification of the checkpoint stays the same.
        /// </summary>
        /// <param name="CheckpointId">The identification of the checkpoint.</param>
        /// <param name="Signatures">Signatures of the checkpoint.</param>
        /// <param name="VerifyCheckpointSignature">A verifier of checkpoint signatures using trusted keys, required for new signatures.</param>
        /// <param name="Checkpoint">The checkpoint with all its signatures afterwards.</param>
        /// <param name="NewSignatures">The signatures added now.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryAddCheckpointSignatures(RoamingNetworkCheckpoint_Id                             CheckpointId,
                                                  IEnumerable<RoamingNetworkCommitSignature>              Signatures,
                                                  VerifyCheckpointSignatureDelegate?                      VerifyCheckpointSignature,
                                                  [NotNullWhen(true)]  out RoamingNetworkCheckpoint?      Checkpoint,
                                                  out IReadOnlyList<RoamingNetworkCommitSignature>        NewSignatures,
                                                  [NotNullWhen(false)] out String?                        ErrorResponse)
        {

            lock (historyLock)
            {

                Checkpoint     = null;
                NewSignatures  = [];

                if (!checkpointIndex.TryGetValue(CheckpointId, out var index))
                {
                    ErrorResponse = $"The checkpoint '{CheckpointId}' is unknown here!";
                    return false;
                }

                var checkpoint     = checkpoints[index];
                var newSignatures  = Signatures.Where(signature => !checkpoint.Signatures.Contains(signature)).Distinct().ToArray();

                if (newSignatures.Length == 0)
                {
                    Checkpoint     = checkpoint;
                    ErrorResponse  = null;
                    return true;
                }

                foreach (var signature in newSignatures)
                    if (!CheckCheckpointSignature(checkpoint, signature, VerifyCheckpointSignature, out ErrorResponse))
                        return false;

                // Kept before it is shown: a store failing keeps it from the history.
                StoreAppend(RoamingNetworkHistoryLogEntry.ForCheckpointSignatures(CheckpointId, newSignatures));

                checkpoint          = checkpoint.WithSignatures(newSignatures);
                checkpoints[index]  = checkpoint;

                // One failing subscriber must neither fail the signatures nor keep them from the others.
                foreach (var subscriber in OnCheckpointSignatures?.GetInvocationList().Cast<OnRoamingNetworkCheckpointSignaturesDelegate>() ?? [])
                {
                    try
                    {
                        subscriber(checkpoint, newSignatures);
                    }
                    catch
                    { }
                }

                Checkpoint     = checkpoint;
                NewSignatures  = newSignatures;
                ErrorResponse  = null;
                return true;

            }

        }

        #endregion

        #region TryWitness(CheckpointId, Witness, out Checkpoint, out Signature, out ErrorResponse, SignedAt = null)

        /// <summary>
        /// Sign a checkpoint of this history as a witness, e.g. with the role witness, and add the signature to it.
        /// This history holds only checkpoints it checked, one per sequence, so a witness never signs two different ones
        /// of the same sequence. The signature is to be sent to the server that made the checkpoint.
        /// </summary>
        /// <param name="CheckpointId">The identification of the checkpoint.</param>
        /// <param name="Witness">The key of the witness.</param>
        /// <param name="Checkpoint">The checkpoint with the new signature.</param>
        /// <param name="Signature">The new signature.</param>
        /// <param name="ErrorResponse">Why it was not signed.</param>
        /// <param name="SignedAt">When it is signed, now otherwise.</param>
        public Boolean TryWitness(RoamingNetworkCheckpoint_Id                             CheckpointId,
                                  RoamingNetworkCommitSigner                              Witness,
                                  [NotNullWhen(true)]  out RoamingNetworkCheckpoint?      Checkpoint,
                                  [NotNullWhen(true)]  out RoamingNetworkCommitSignature? Signature,
                                  [NotNullWhen(false)] out String?                        ErrorResponse,
                                  DateTimeOffset?                                         SignedAt   = null)
        {

            lock (historyLock)
            {

                Checkpoint  = null;
                Signature   = null;

                if (!checkpointIndex.TryGetValue(CheckpointId, out var index))
                {
                    ErrorResponse = $"The checkpoint '{CheckpointId}' is unknown here; a witness signs only checkpoints it checked!";
                    return false;
                }

                if (checkpoints[index].PriceStatementsSize > 0 && PriceStatementsOf(CheckpointId) is null)
                {
                    ErrorResponse = $"The price statements of the checkpoint '{CheckpointId}' are not checked here; a witness signs only checkpoints it checked!";
                    return false;
                }

                var signature = Witness.Sign(checkpoints[index], SignedAt);

                // The witness trusts its own signature: it just made it.
                if (!TryAddCheckpointSignatures(CheckpointId, [ signature ], (checkpoint, sig) => sig == signature, out Checkpoint, out _, out ErrorResponse))
                    return false;

                Signature = signature;
                return true;

            }

        }

        #endregion

        #region TryGetCheckpoint(CheckpointId, out Checkpoint)

        /// <summary>
        /// Try to return the given checkpoint.
        /// </summary>
        public Boolean TryGetCheckpoint(RoamingNetworkCheckpoint_Id                         CheckpointId,
                                        [NotNullWhen(true)] out RoamingNetworkCheckpoint?   Checkpoint)
        {

            lock (historyLock)
            {

                if (checkpointIndex.TryGetValue(CheckpointId, out var index))
                {
                    Checkpoint = checkpoints[index];
                    return true;
                }

                Checkpoint = null;
                return false;

            }

        }

        #endregion

        #region GetCheckpointsSince(Sequence = null, MaxCount = DefaultMaxCommits)

        /// <summary>
        /// The checkpoints after the given sequence, all without one, in their order, at most the given number.
        /// </summary>
        /// <param name="Sequence">The sequence of the latest checkpoint the caller holds.</param>
        /// <param name="MaxCount">The maximum number of checkpoints to return.</param>
        public IReadOnlyList<RoamingNetworkCheckpoint> GetCheckpointsSince(UInt64?  Sequence   = null,
                                                                           Int32    MaxCount   = DefaultMaxCommits)
        {

            lock (historyLock)
            {
                return [ .. checkpoints.Where(checkpoint => !Sequence.HasValue || checkpoint.Sequence > Sequence.Value).
                                        Take (Math.Max(0, MaxCount)) ];
            }

        }

        #endregion

        #region TryGetCommitProof(CommitId, out Proof, out ErrorResponse, CheckpointId = null)

        /// <summary>
        /// The proof that the given commit is one of the commits of a checkpoint (SPECIFICATION.md, section 10.4):
        /// of the given checkpoint, or of the first one whose commits include it.
        /// </summary>
        /// <param name="CommitId">The commit.</param>
        /// <param name="Proof">The proof.</param>
        /// <param name="ErrorResponse">Why there is none, e.g. as no checkpoint includes the commit yet.</param>
        /// <param name="CheckpointId">The checkpoint, the first one including the commit otherwise.</param>
        public Boolean TryGetCommitProof(RoamingNetworkCommit_Id                                  CommitId,
                                         [NotNullWhen(true)]  out RoamingNetworkCheckpointProof?  Proof,
                                         [NotNullWhen(false)] out String?                         ErrorResponse,
                                         RoamingNetworkCheckpoint_Id?                             CheckpointId   = null)
        {

            Proof = null;

            lock (historyLock)
            {

                // A commit held is proved from the commits held, else, or e.g. when it is the root of the history and its
                // checkpoint covers commits dropped when compacting, from the segment of the archive holding it.
                var lookups = new List<Func<RoamingNetworkCommit_Id, RoamingNetworkCommit?>>();

                if (commitIndex.ContainsKey(CommitId))
                    lookups.Add(HeldLookup);

                if (TryLoadArchiveSegmentOf(CommitId, out var segment))
                    lookups.Add(ArchiveLookup(segment));

                if (lookups.Count == 0)
                {
                    ErrorResponse = $"The commit '{CommitId}' is unknown here!";
                    return false;
                }

                foreach (var lookup in lookups)
                for (var position = 0; position < checkpoints.Count; position++)
                {

                    var checkpoint = checkpoints[position];

                    if (CheckpointId.HasValue && checkpoint.Id != CheckpointId.Value)
                        continue;

                    var previous = position > 0 && checkpoints[position - 1].Sequence + 1 == checkpoint.Sequence
                                       ? checkpoints[position - 1].CommitId
                                       : (RoamingNetworkCommit_Id?) null;

                    if (checkpoint.Sequence > 0 && previous is null)
                        continue;

                    if (CommitsOf(lookup, checkpoint.CommitId, previous) is not IReadOnlyList<RoamingNetworkCommit> commitsOfCheckpoint)
                        continue;

                    var leafIndex = -1;

                    for (var i = 0; i < commitsOfCheckpoint.Count; i++)
                        if (commitsOfCheckpoint[i].Id == CommitId)
                        {
                            leafIndex = i;
                            break;
                        }

                    if (leafIndex < 0)
                        continue;

                    var commit = commitsOfCheckpoint[leafIndex];

                    Proof = new RoamingNetworkCheckpointProof(checkpoint.Id,
                                                              commit.Id,
                                                              commit.ETags,
                                                              commit.Revision,
                                                              (UInt64) leafIndex,
                                                              (UInt64) commitsOfCheckpoint.Count,
                                                              POIMerkleTree.AuditPath([ .. commitsOfCheckpoint.Select(RoamingNetworkCheckpoint.Leaf) ], leafIndex));

                    ErrorResponse = null;
                    return true;

                }

                ErrorResponse = CheckpointId.HasValue
                                    ? $"The commit '{CommitId}' is not one of the commits of the checkpoint '{CheckpointId}' here!"
                                    : $"No checkpoint here includes the commit '{CommitId}' yet!";

                return false;

            }

        }

        #endregion

        #region TryGetCheckpointSnapshotJSON(CheckpointId, out Snapshot, out ErrorResponse)

        /// <summary>
        /// A snapshot of the version of the commit of the given checkpoint, with the checkpoint, without runtime status:
        /// where a server or client joins from (SPECIFICATION.md, section 10.5).
        /// </summary>
        public Boolean TryGetCheckpointSnapshotJSON(RoamingNetworkCheckpoint_Id         CheckpointId,
                                                    [NotNullWhen(true)]  out JObject?   Snapshot,
                                                    [NotNullWhen(false)] out String?    ErrorResponse)
        {

            Snapshot = null;

            lock (historyLock)
            {

                if (!checkpointIndex.TryGetValue(CheckpointId, out var index))
                {
                    ErrorResponse = $"The checkpoint '{CheckpointId}' is unknown here!";
                    return false;
                }

                var checkpoint = checkpoints[index];

                if (!commitIndex.TryGetValue(checkpoint.CommitId, out var commitPosition) ||
                    !TryGetVersion(checkpoint.CommitId, out var version, out ErrorResponse))
                {
                    ErrorResponse = $"The commit of the checkpoint '{CheckpointId}' is no longer held here!";
                    return false;
                }

                Snapshot = SnapshotJSON(commits[commitPosition], version, Head.RoamingNetwork);
                Snapshot.Remove("runtimeStatus");
                Snapshot.Add("checkpoint", checkpoint.ToJSON());

                ErrorResponse = null;
                return true;

            }

        }

        #endregion


        #region (static) TryFromCheckpointSnapshot(Snapshot, out History, out ErrorResponse, VerifyCheckpointSignature = null, RequiredSignatureRoles = null, PriceStatements = null, Limits = null)

        /// <summary>
        /// Try to start a history from the snapshot of a checkpoint of another server, as TryGetCheckpointSnapshotJSON writes
        /// it (SPECIFICATION.md, section 10.5): the version of the commit of the checkpoint, which must lead to its ETags, and
        /// the checkpoint, whose signatures must be valid and of the required roles. The history begins its line of
        /// checkpoints with it, takes the price statements it names when they are given, and the commits since from there.
        /// A runtime status of the snapshot is not taken: the version is not the head.
        /// </summary>
        /// <param name="Snapshot">The snapshot of a checkpoint: { commit, roamingNetwork, effectiveLastChanges?, checkpoint }.</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifyCheckpointSignature">A verifier of checkpoint signatures using trusted keys, required for a signed checkpoint.</param>
        /// <param name="RequiredSignatureRoles">The roles the checkpoint must be signed in, e.g. roamingNetworkOperator.</param>
        /// <param name="PriceStatements">The price statements the checkpoint names, if they are given.</param>
        /// <param name="Limits">Optional limits for the change set of the commit, the default ones otherwise.</param>
        public static Boolean TryFromCheckpointSnapshot(JObject                                          Snapshot,
                                                        [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                                        [NotNullWhen(false)] out String?                 ErrorResponse,
                                                        VerifyCheckpointSignatureDelegate?               VerifyCheckpointSignature   = null,
                                                        IReadOnlyCollection<String>?                     RequiredSignatureRoles      = null,
                                                        IEnumerable<RoamingNetworkPriceStatement>?       PriceStatements             = null,
                                                        RoamingNetworkChangeSetLimits?                   Limits                      = null)
        {

            History = null;

            if (Snapshot?["checkpoint"] is not JObject checkpointJSON)
            {
                ErrorResponse = "The snapshot of a checkpoint needs its checkpoint!";
                return false;
            }

            if (!RoamingNetworkCheckpoint.TryParse(checkpointJSON, out var checkpoint, out var checkpointError))
            {
                ErrorResponse = "Invalid checkpoint of the snapshot: " + checkpointError;
                return false;
            }

            // The version and its commit only: a runtime status would be the one of another version.
            var version = new JObject(
                              new JProperty("commit",          Snapshot["commit"]),
                              new JProperty("roamingNetwork",  Snapshot["roamingNetwork"])
                          );

            if (Snapshot["effectiveLastChanges"] is JToken effectiveLastChanges)
                version.Add("effectiveLastChanges", effectiveLastChanges);

            if (!TryFromSnapshot(version, out var history, out var snapshotError, Limits))
            {
                ErrorResponse = "Invalid snapshot of the checkpoint: " + snapshotError;
                return false;
            }

            var commit = history.Head.Commit;

            if (commit.Id != checkpoint.CommitId || commit.Revision != checkpoint.Revision || !commit.ETags.SequenceEqual(checkpoint.ETags))
            {
                ErrorResponse = $"The snapshot is not the version of the commit '{checkpoint.CommitId}' of its checkpoint, with its revision and ETags!";
                return false;
            }

            if (!history.TryAddCheckpoint(checkpoint, out var addError, VerifyCheckpointSignature, RequiredSignatureRoles, PriceStatements))
            {
                ErrorResponse = "The checkpoint of the snapshot can not be taken: " + addError;
                return false;
            }

            History        = history;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (static) TryFromCheckpointSnapshot(Text,     out History, out ErrorResponse, VerifyCheckpointSignature = null, RequiredSignatureRoles = null, PriceStatements = null, Limits = null)

        /// <summary>
        /// Try to start a history from the JSON text of the snapshot of a checkpoint of another server, e.g. of
        /// GET ~/RNs/{RN_Id}/history/checkpoints/{checkpointId}/snapshot. Dates stay text, so signed values keep their exact
        /// representation.
        /// </summary>
        /// <param name="Text">The JSON text of the snapshot of a checkpoint.</param>
        /// <param name="History">The new history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="VerifyCheckpointSignature">A verifier of checkpoint signatures using trusted keys, required for a signed checkpoint.</param>
        /// <param name="RequiredSignatureRoles">The roles the checkpoint must be signed in, e.g. roamingNetworkOperator.</param>
        /// <param name="PriceStatements">The price statements the checkpoint names, if they are given.</param>
        /// <param name="Limits">Optional limits for the change set of the commit, the default ones otherwise.</param>
        public static Boolean TryFromCheckpointSnapshot(String                                           Text,
                                                        [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                                        [NotNullWhen(false)] out String?                 ErrorResponse,
                                                        VerifyCheckpointSignatureDelegate?               VerifyCheckpointSignature   = null,
                                                        IReadOnlyCollection<String>?                     RequiredSignatureRoles      = null,
                                                        IEnumerable<RoamingNetworkPriceStatement>?       PriceStatements             = null,
                                                        RoamingNetworkChangeSetLimits?                   Limits                      = null)
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

                return TryFromCheckpointSnapshot(snapshot,
                                                 out History,
                                                 out ErrorResponse,
                                                 VerifyCheckpointSignature,
                                                 RequiredSignatureRoles,
                                                 PriceStatements,
                                                 Limits);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion


        #region (private) CheckCheckpoint(Checkpoint, out ErrorResponse)

        /// <summary>
        /// Check a checkpoint of another server against this history, while it is locked (SPECIFICATION.md, section 10.1).
        /// </summary>
        private Boolean CheckCheckpoint(RoamingNetworkCheckpoint          Checkpoint,
                                        [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (Checkpoint.RoamingNetworkId != RoamingNetworkId)
            {
                ErrorResponse = $"The checkpoint is for the roaming network '{Checkpoint.RoamingNetworkId}', not for '{RoamingNetworkId}'!";
                return false;
            }

            if (Checkpoint.Epoch != RoamingNetworkCheckpoint.DefaultEpoch)
            {
                ErrorResponse = $"The checkpoint is of the epoch {Checkpoint.Epoch}, but this history is of epoch {RoamingNetworkCheckpoint.DefaultEpoch}!";
                return false;
            }

            var latest = checkpoints.Count > 0 ? checkpoints[^1] : null;

            if (latest is not null)
            {

                if (Checkpoint.Sequence <= latest.Sequence)
                {
                    ErrorResponse = $"This history has another checkpoint of the sequence {Checkpoint.Sequence}: two checkpoints of one sequence are a split view!";
                    return false;
                }

                if (Checkpoint.Sequence != latest.Sequence + 1 || Checkpoint.PreviousId != latest.Id)
                {
                    ErrorResponse = $"The checkpoint does not follow the latest checkpoint '{latest.Id}' of the sequence {latest.Sequence} here!";
                    return false;
                }

            }

            if (!commitIndex.TryGetValue(Checkpoint.CommitId, out var index))
            {
                ErrorResponse = $"The commit '{Checkpoint.CommitId}' of the checkpoint is unknown here!";
                return false;
            }

            var commit = commits[index];

            if (commit.Revision != Checkpoint.Revision || !commit.ETags.SequenceEqual(Checkpoint.ETags))
            {
                ErrorResponse = "The revision or the ETags of the checkpoint are not those of its commit!";
                return false;
            }

            if (latest is not null &&
               (latest.CommitId == Checkpoint.CommitId || !Ancestors(Checkpoint.CommitId).Contains(latest.CommitId)))
            {
                ErrorResponse = $"The commit of the checkpoint does not follow the commit '{latest.CommitId}' of the latest checkpoint!";
                return false;
            }

            if (CommitsOf(Checkpoint.CommitId, latest?.CommitId) is IReadOnlyList<RoamingNetworkCommit> commitsOfCheckpoint)
            {

                if ((UInt64) commitsOfCheckpoint.Count != Checkpoint.CommitsSize ||
                    RoamingNetworkCheckpoint.CommitsRootOf(commitsOfCheckpoint) != Checkpoint.CommitsRoot)
                {
                    ErrorResponse = "The commits of the checkpoint are not the commits since the latest checkpoint here!";
                    return false;
                }

            }

            // Only the first checkpoint of a history that started from a snapshot can not be checked completely.
            else if (latest is not null)
            {
                ErrorResponse = "This history does not hold every commit of the checkpoint!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) CheckCheckpointSignature(Checkpoint, Signature, VerifyCheckpointSignature, out ErrorResponse)

        private static Boolean CheckCheckpointSignature(RoamingNetworkCheckpoint             Checkpoint,
                                                        RoamingNetworkCommitSignature        Signature,
                                                        VerifyCheckpointSignatureDelegate?   VerifyCheckpointSignature,
                                                        [NotNullWhen(false)] out String?     ErrorResponse)
        {

            if (VerifyCheckpointSignature is null)
            {
                ErrorResponse = $"The checkpoint '{Checkpoint.Id}' is signed, but there is no verifier of checkpoint signatures!";
                return false;
            }

            if (!VerifyCheckpointSignature(Checkpoint, Signature))
            {
                ErrorResponse = $"The signature of the checkpoint '{Checkpoint.Id}' by {Signature} is invalid, or its key is not trusted!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private) TakeCheckpoint(Checkpoint, PriceStatements = null)

        /// <summary>
        /// Take the given checkpoint, while the history and its price statements are locked: keep it in the store, with the
        /// price statements it names when they are given, hold it, and announce it. The price statements signed before it
        /// are no longer those for the next checkpoint.
        /// </summary>
        private void TakeCheckpoint(RoamingNetworkCheckpoint                      Checkpoint,
                                    IReadOnlyList<RoamingNetworkPriceStatement>?  PriceStatements   = null)
        {

            // Kept before it is shown: a store failing keeps it from the history.
            StoreAppend(RoamingNetworkHistoryLogEntry.ForCheckpoint(Checkpoint));

            if (PriceStatements is not null && Checkpoint.PriceStatementsSize > 0)
                StoreAppend(RoamingNetworkHistoryLogEntry.ForCheckpointPriceStatements(Checkpoint.Id, PriceStatements));

            checkpointIndex[Checkpoint.Id] = checkpoints.Count;
            checkpoints.Add(Checkpoint);

            if (PriceStatements is not null && Checkpoint.PriceStatementsSize > 0)
                TakeCheckpointPriceStatements(Checkpoint.Id, PriceStatements);

            PrunePendingPriceStatements(Checkpoint);

            // One failing subscriber must neither fail the checkpoint nor keep it from the others.
            foreach (var subscriber in OnCheckpoint?.GetInvocationList().Cast<OnRoamingNetworkCheckpointDelegate>() ?? [])
            {
                try
                {
                    subscriber(Checkpoint);
                }
                catch
                { }
            }

        }

        #endregion

        #region (private) TakeStoredCheckpoint(Checkpoint)

        /// <summary>
        /// Take a checkpoint of the store while loading: it was checked before it was kept, also one of commits dropped since.
        /// </summary>
        private void TakeStoredCheckpoint(RoamingNetworkCheckpoint Checkpoint)
        {

            if (checkpointIndex.ContainsKey(Checkpoint.Id))
                return;

            checkpointIndex[Checkpoint.Id] = checkpoints.Count;
            checkpoints.Add(Checkpoint);

            lock (priceStatementLock)
            {
                PrunePendingPriceStatements(Checkpoint);
            }

        }

        #endregion

        #region (private) TakeStoredCheckpointSignatures(CheckpointId, Signatures)

        private void TakeStoredCheckpointSignatures(RoamingNetworkCheckpoint_Id                   CheckpointId,
                                                    IReadOnlyList<RoamingNetworkCommitSignature>  Signatures)
        {

            if (checkpointIndex.TryGetValue(CheckpointId, out var index))
                checkpoints[index] = checkpoints[index].WithSignatures(Signatures);

        }

        #endregion

        #region (private) CheckpointLogEntries()

        /// <summary>
        /// The log entries of all checkpoints, with their signatures and the price statements they name, and of the other
        /// price statements held, e.g. for a compacted log: those for the next checkpoint after the checkpoints.
        /// </summary>
        private IEnumerable<RoamingNetworkHistoryLogEntry> CheckpointLogEntries()
        {

            var (before, after)  = PriceStatementLogEntries();
            var entries          = new List<RoamingNetworkHistoryLogEntry>(before);

            foreach (var checkpoint in checkpoints)
            {

                entries.Add(RoamingNetworkHistoryLogEntry.ForCheckpoint(checkpoint));

                if (PriceStatementsOf(checkpoint.Id) is IReadOnlyList<RoamingNetworkPriceStatement> priceStatementsOfCheckpoint)
                    entries.Add(RoamingNetworkHistoryLogEntry.ForCheckpointPriceStatements(checkpoint.Id, priceStatementsOfCheckpoint));

            }

            entries.AddRange(after);

            return entries;

        }

        #endregion

        #region (private) CheckpointCommitIds()

        /// <summary>
        /// The commits of the checkpoints, the only ones a history with checkpoints compacts to.
        /// </summary>
        private HashSet<RoamingNetworkCommit_Id> CheckpointCommitIds()

            => [ .. checkpoints.Select(checkpoint => checkpoint.CommitId) ];

        #endregion

        #region (private) CommitsOf(CommitId, PreviousCommitId)

        /// <summary>
        /// The commits of a checkpoint of the given commit, in their order: the commit and every commit it follows,
        /// that the commit of the previous checkpoint neither is nor follows; null when this history does not hold all of them.
        /// </summary>
        private IReadOnlyList<RoamingNetworkCommit>? CommitsOf(RoamingNetworkCommit_Id   CommitId,
                                                               RoamingNetworkCommit_Id?  PreviousCommitId)

            => CommitsOf(HeldLookup, CommitId, PreviousCommitId);


        /// <summary>
        /// The commits of a checkpoint of the given commit, as the given lookup finds them, e.g. in a segment of the archive.
        /// </summary>
        private static IReadOnlyList<RoamingNetworkCommit>? CommitsOf(Func<RoamingNetworkCommit_Id, RoamingNetworkCommit?>  Lookup,
                                                                      RoamingNetworkCommit_Id                               CommitId,
                                                                      RoamingNetworkCommit_Id?                              PreviousCommitId)
        {

            if (Lookup(CommitId) is null)
                return null;

            var included  = AncestorsOf(Lookup, CommitId);
            var excluded  = PreviousCommitId.HasValue ? AncestorsOf(Lookup, PreviousCommitId.Value) : new HashSet<RoamingNetworkCommit_Id>();

            if (PreviousCommitId.HasValue && !excluded.Contains(PreviousCommitId.Value))
                return null;

            included.ExceptWith(excluded);

            var commitsOfCheckpoint = included.Select(commitId => Lookup(commitId)!).ToList();

            // Complete: every parent is one of them, or one the previous checkpoint covers; the first one starts at the genesis.
            foreach (var commit in commitsOfCheckpoint)
                foreach (var parentId in commit.ParentIds)
                    if (!included.Contains(parentId) && !excluded.Contains(parentId))
                        return null;

            if (!PreviousCommitId.HasValue && !commitsOfCheckpoint.Any(commit => commit.ParentIds.IsDefaultOrEmpty))
                return null;

            return RoamingNetworkCheckpoint.InOrder(commitsOfCheckpoint);

        }

        #endregion

        #region (private) HeldLookup(CommitId)

        /// <summary>
        /// The commit this history holds, if it does, while it is locked.
        /// </summary>
        private RoamingNetworkCommit? HeldLookup(RoamingNetworkCommit_Id CommitId)

            => commitIndex.TryGetValue(CommitId, out var index)
                   ? commits[index]
                   : null;

        #endregion

        #region (private static) AncestorsOf(Lookup, CommitId)

        /// <summary>
        /// The given commit and all its ancestors the given lookup finds.
        /// </summary>
        private static HashSet<RoamingNetworkCommit_Id> AncestorsOf(Func<RoamingNetworkCommit_Id, RoamingNetworkCommit?>  Lookup,
                                                                    RoamingNetworkCommit_Id                               CommitId)
        {

            var ancestors  = new HashSet<RoamingNetworkCommit_Id>();
            var pending    = new Stack<RoamingNetworkCommit_Id>([ CommitId ]);

            while (pending.TryPop(out var commitId))
                if (!ancestors.Contains(commitId) && Lookup(commitId) is RoamingNetworkCommit commit)
                {
                    ancestors.Add(commitId);
                    foreach (var parentId in commit.ParentIds)
                        pending.Push(parentId);
                }

            return ancestors;

        }

        #endregion

        #region (private) LatestCommitAllTipsFollow()

        /// <summary>
        /// The latest commit all tips follow: the head while there is one tip.
        /// </summary>
        private RoamingNetworkCommit_Id LatestCommitAllTipsFollow()
        {

            HashSet<RoamingNetworkCommit_Id>? common = null;

            foreach (var tip in tips)
            {
                var ancestors = Ancestors(tip);
                if (common is null) common = ancestors;
                else                common.IntersectWith(ancestors);
            }

            return (common ?? []).
                       Select (commitId => commits[commitIndex[commitId]]).
                       OrderByDescending(commit => commit.Revision).
                       ThenBy (commit => commit.Id.ToString(), StringComparer.Ordinal).
                       First().Id;

        }

        #endregion

    }

}
