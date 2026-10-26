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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The epoch of a history (SPECIFICATION.md, section 11): one history of a roaming network under one set of profiles,
    /// the hash of its identifications and the signature algorithms it accepts. A new epoch begins only when these must
    /// change: its final checkpoint closes an epoch and names the next one, which starts from the version of its commit,
    /// with a genesis of its own, and whose first checkpoint names that final checkpoint. Commits of different epochs are
    /// never merged; an epoch closed stays readable.
    /// </summary>
    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        private          RoamingNetworkEpoch           epoch           = RoamingNetworkEpoch.First;
        private          RoamingNetworkEpochLink?      previousEpoch;
        private          RoamingNetworkEpochProfiles?  previousProfiles;
        private volatile Boolean                       nextEpochStarted;

        #endregion

        #region Properties

        /// <summary>
        /// The epoch of this history and its profiles.
        /// </summary>
        public RoamingNetworkEpoch        Epoch
            => epoch;

        /// <summary>
        /// The final checkpoint of the epoch before, which this one starts from, if it is known: the first checkpoint of
        /// this epoch names it.
        /// </summary>
        public RoamingNetworkEpochLink?   PreviousEpoch
            => previousEpoch;

        /// <summary>
        /// The profiles of the epoch before, if they are known: the first checkpoint of this epoch is signed with the
        /// algorithms of both (SPECIFICATION.md, section 11.3). Those of the first epoch are the default ones; those of a
        /// later one this history knows from the epoch it was started from, or from the snapshot it started from.
        /// </summary>
        public RoamingNetworkEpochProfiles? PreviousEpochProfiles
            => epoch.Number switch {
                   1  => null,
                   2  => RoamingNetworkEpochProfiles.Default,
                   _  => previousProfiles
               };

        /// <summary>
        /// The final checkpoint of this epoch, once there is one: this history takes no commits any more.
        /// </summary>
        public RoamingNetworkCheckpoint?  FinalCheckpoint
            => LatestCheckpoint is RoamingNetworkCheckpoint latest && latest.IsFinal
                   ? latest
                   : null;

        /// <summary>
        /// Where this epoch is: taking commits, closed by its final checkpoint, or followed by the next epoch started here.
        /// </summary>
        public RoamingNetworkEpochStatus  EpochStatus
            => FinalCheckpoint is null
                   ? RoamingNetworkEpochStatus.Active
                   : nextEpochStarted
                         ? RoamingNetworkEpochStatus.Closed
                         : RoamingNetworkEpochStatus.Closing;

        #endregion


        #region TryCloseEpoch(NextProfiles, out FinalCheckpoint, out ErrorResponse, CreatedAt = null)

        /// <summary>
        /// Close this epoch with its final checkpoint, signed by the CheckpointSigners, naming the next epoch with the given
        /// profiles (SPECIFICATION.md, section 11): of the head, when it is the only tip, also when the latest checkpoint is
        /// of it already. The epoch takes no commit after it; TryStartNextEpoch(…) starts the next one from its version.
        /// </summary>
        /// <param name="NextProfiles">The profiles of the next epoch.</param>
        /// <param name="FinalCheckpoint">The final checkpoint.</param>
        /// <param name="ErrorResponse">Why it was not closed.</param>
        /// <param name="CreatedAt">When the final checkpoint is made, now otherwise.</param>
        public Boolean TryCloseEpoch(RoamingNetworkEpochProfiles                          NextProfiles,
                                     [NotNullWhen(true)]  out RoamingNetworkCheckpoint?  FinalCheckpoint,
                                     [NotNullWhen(false)] out String?                    ErrorResponse,
                                     DateTimeOffset?                                     CreatedAt   = null)
        {

            FinalCheckpoint = null;

            ArgumentNullException.ThrowIfNull(NextProfiles);

            lock (historyLock)
            {

                if (ClosedError() is String closed)
                {
                    ErrorResponse = closed;
                    return false;
                }

                if (tips.Count != 1)
                {
                    ErrorResponse = $"The epoch {epoch.Number} is closed only without open branches, but it has {tips.Count} tips: merge them first!";
                    return false;
                }

                var commit   = head.Commit;
                var latest   = checkpoints.Count > 0 ? checkpoints[^1] : null;

                // The commits since the latest checkpoint, or the commit of the latest one alone.
                IReadOnlyList<RoamingNetworkCommit>? commitsOfCheckpoint = latest?.CommitId == commit.Id
                                                                               ? [ commit ]
                                                                               : CommitsOf(commit.Id, latest?.CommitId);

                if (commitsOfCheckpoint is null)
                {
                    ErrorResponse = latest is null
                                        ? "The final checkpoint needs every commit since the genesis, which this history does not hold!"
                                        : "This history does not hold every commit since the latest checkpoint!";
                    return false;
                }

                var createdAt = DateTimeOffset.FromUnixTimeMilliseconds((CreatedAt ?? Timestamp.Now).ToUnixTimeMilliseconds());

                lock (priceStatementLock)
                {

                    var priceStatementsOfCheckpoint  = PriceStatementsForCheckpoint(createdAt);

                    var checkpoint                   = RoamingNetworkCheckpoint.Create(RoamingNetworkId,
                                                                                       epoch.Number,
                                                                                       latest is null ? 0 : latest.Sequence + 1,
                                                                                       latest?.Id,
                                                                                       commit,
                                                                                       commitsOfCheckpoint,
                                                                                       createdAt,
                                                                                       priceStatementsOfCheckpoint,
                                                                                       epoch.Next(NextProfiles),
                                                                                       latest is null ? previousEpoch : null);

                    var signers                      = CheckpointSigners;

                    foreach (var signer in signers.IsDefault ? [] : signers.AsEnumerable())
                        if (CheckpointSignatureAlgorithmError(latest is null && epoch.Number > 1, signer.Algorithm.Name, $"the checkpoint signer {signer}") is String signerError)
                        {
                            ErrorResponse = signerError;
                            return false;
                        }

                    if (!signers.IsDefaultOrEmpty)
                        checkpoint = checkpoint.WithSignatures(signers.Select(signer => signer.Sign(checkpoint)));

                    TakeCheckpoint(checkpoint, priceStatementsOfCheckpoint);

                    FinalCheckpoint = checkpoint;

                }

                ErrorResponse = null;
                return true;

            }

        }

        #endregion

        #region TryStartNextEpoch(out Next, out ErrorResponse, Signers = null, Store = null, Retention = null, Archive = null)

        /// <summary>
        /// Start the next epoch from the final checkpoint of this one (SPECIFICATION.md, section 11): nothing is applied, its
        /// genesis has the version of the commit of the final checkpoint, identified with the profiles of the next epoch, and
        /// leads to the ETags of the final checkpoint; the runtime status and the current price statements are taken along,
        /// and the profiles of this epoch, for the signatures of its first checkpoint. Every server starting it from the same
        /// final checkpoint makes the same genesis. This history stays readable.
        /// </summary>
        /// <param name="Next">The history of the next epoch.</param>
        /// <param name="ErrorResponse">Why it was not started.</param>
        /// <param name="Signers">The keys that sign its genesis and every commit made there, with algorithms of the next epoch.</param>
        /// <param name="Store">An optional empty store to keep it in.</param>
        /// <param name="Retention">What it keeps; everything, unless given.</param>
        /// <param name="Archive">Where it keeps the commits it drops when compacting.</param>
        public Boolean TryStartNextEpoch([NotNullWhen(true)]  out RoamingNetworkHistory?    Next,
                                         [NotNullWhen(false)] out String?                   ErrorResponse,
                                         IEnumerable<RoamingNetworkCommitSigner>?           Signers     = null,
                                         IRoamingNetworkHistoryStore?                       Store       = null,
                                         RoamingNetworkHistoryRetention?                    Retention   = null,
                                         IRoamingNetworkHistoryArchive?                     Archive     = null)
        {

            Next = null;

            var signers = Signers?.ToImmutableArray() ?? [];

            lock (historyLock)
            {

                if (FinalCheckpoint is not RoamingNetworkCheckpoint finalCheckpoint)
                {
                    ErrorResponse = $"The epoch {epoch.Number} has no final checkpoint yet!";
                    return false;
                }

                var nextEpoch = finalCheckpoint.Next!;

                foreach (var signer in signers)
                    if (!nextEpoch.Profiles.AllowsSignatureAlgorithm(signer.Algorithm.Name))
                    {
                        ErrorResponse = $"The signature algorithm '{signer.Algorithm.Name}' of the signer {signer} is not one of the epoch {nextEpoch.Number}!";
                        return false;
                    }

                if (!TryGetVersion(finalCheckpoint.CommitId, out var version, out var versionError))
                {
                    ErrorResponse = $"The version of the final checkpoint '{finalCheckpoint.Id}' is not held here: {versionError}";
                    return false;
                }

                var genesis = RoamingNetworkCommit.Genesis(version.WithRevision(0), nextEpoch);

                if (!genesis.ETags.SequenceEqual(finalCheckpoint.ETags))
                {
                    ErrorResponse = $"The version of the final checkpoint '{finalCheckpoint.Id}' does not lead to its ETags under the profiles of the epoch {nextEpoch.Number}!";
                    return false;
                }

                // As from a snapshot: a version of its own, with the runtime status of the head.
                var snapshot = new JObject(
                                   new JProperty(PropertyKeys.Commit,          SignedBy(signers, genesis).ToJSON(Embedded: true)),
                                   new JProperty(PropertyKeys.RoamingNetwork,  version.ToJSON())
                               );

                if (version.EffectiveFloorsJSON() is JArray effectiveLastChanges)
                    snapshot.Add(PropertyKeys.EffectiveLastChanges, effectiveLastChanges);

                snapshot.Add(PropertyKeys.RuntimeStatus, new JArray(RoamingNetworkRuntimeStatus.Of(head.RoamingNetwork).Select(status => status.ToJSON(Embedded: true))));
                snapshot.Add(EpochJSONProperty, EpochJSON(nextEpoch, RoamingNetworkEpochLink.To(finalCheckpoint), epoch.Profiles));

                if (!TryFromSnapshot(snapshot, out var nextHistory, out var snapshotError))
                {
                    ErrorResponse = $"The epoch {nextEpoch.Number} can not be started: {snapshotError}";
                    return false;
                }

                nextHistory.Signers    = signers;
                nextHistory.Retention  = Retention ?? RoamingNetworkHistoryRetention.KeepEverything;
                nextHistory.Archive    = Archive;

                // Their chains go on.
                lock (priceStatementLock)
                lock (nextHistory.priceStatementLock)
                {
                    foreach (var priceStatement in priceStatements.Values)
                        nextHistory.priceStatements[(priceStatement.EntityType, priceStatement.EntityId)] = priceStatement;
                }

                if (Store is not null)
                    nextHistory.AttachStore(Store);

                nextEpochStarted  = true;
                Next              = nextHistory;
                ErrorResponse     = null;
                return true;

            }

        }

        #endregion


        #region (internal) EpochJSONProperty / EpochJSON(Epoch, PreviousEpoch, PreviousProfiles)

        /// <summary>
        /// The JSON key of the epoch of a snapshot.
        /// </summary>
        internal const String EpochJSONProperty             = "epoch";

        /// <summary>
        /// The JSON key of the profiles of the epoch before, in the epoch of a snapshot.
        /// </summary>
        internal const String PreviousProfilesJSONProperty  = "previousProfiles";

        /// <summary>
        /// The epoch of a snapshot: { epoch, profiles, previous?, previousProfiles? }, the profiles of the epoch before from
        /// the third epoch on: those of the first one are the default ones.
        /// </summary>
        internal static JObject EpochJSON(RoamingNetworkEpoch           Epoch,
                                          RoamingNetworkEpochLink?      PreviousEpoch,
                                          RoamingNetworkEpochProfiles?  PreviousProfiles)
        {

            var json = Epoch.ToJSON(Embedded: true);

            if (PreviousEpoch is not null)
                json.Add(RoamingNetworkEpochLink.JSONProperty, PreviousEpoch.ToJSON(Embedded: true));

            if (PreviousProfiles is not null && Epoch.Number > 2)
                json.Add(PreviousProfilesJSONProperty, PreviousProfiles.ToJSON(Embedded: true));

            return json;

        }

        #endregion

        #region (private) ClosedError()

        /// <summary>
        /// Why this history takes no commits and no checkpoints any more, if it does not: its epoch is closed.
        /// </summary>
        private String? ClosedError()

            => FinalCheckpoint is RoamingNetworkCheckpoint finalCheckpoint
                   ? $"The epoch {epoch.Number} of the roaming network '{RoamingNetworkId}' is closed by its final checkpoint '{finalCheckpoint.Id}': what follows belongs to the epoch {epoch.Number + 1}!"
                   : null;

        #endregion

        #region (private) SignatureAlgorithmError(Algorithm, What)

        /// <summary>
        /// Why a signature of the given algorithm is not taken, if it is not: the epoch does not accept the algorithm.
        /// </summary>
        private String? SignatureAlgorithmError(String  Algorithm,
                                                String  What)

            => epoch.Profiles.AllowsSignatureAlgorithm(Algorithm)
                   ? null
                   : $"The signature algorithm '{Algorithm}' of {What} is not one of the epoch {epoch.Number}" +
                     (epoch.Profiles.SignatureAlgorithms.IsEmpty ? "!" : $", {String.Join(", ", epoch.Profiles.SignatureAlgorithms)}!");

        #endregion

        #region (private) CommitEpochError(Commit)

        /// <summary>
        /// Why the given commit of another server is not one of this epoch, if it is not.
        /// </summary>
        private String? CommitEpochError(RoamingNetworkCommit Commit)
        {

            if (Commit.Epoch != epoch.Number)
                return $"The commit '{Commit.Id}' is one of the epoch {Commit.Epoch}, but this history is of the epoch {epoch.Number}!";

            if (Commit.IdentificationHash != epoch.Profiles.IdentificationHash)
                return $"The commit '{Commit.Id}' is identified with {Commit.IdentificationHash.ToText()}, but the epoch {epoch.Number} with {epoch.Profiles.IdentificationHash.ToText()}!";

            foreach (var signature in Commit.Signatures)
                if (SignatureAlgorithmError(signature.Algorithm, $"a signature of the commit '{Commit.Id}'") is String commitSignatureError)
                    return commitSignatureError;

            foreach (var signature in Commit.ChangeSet?.Signatures ?? [])
                if (SignatureAlgorithmError(signature.Algorithm, $"a signature of the change set of the commit '{Commit.Id}'") is String changeSetSignatureError)
                    return changeSetSignatureError;

            return null;

        }

        #endregion

        #region (private) VersionEpochError(RoamingNetwork)

        /// <summary>
        /// Why the given new version does not fit this epoch, if it does not: a signature of its key statement, or of a
        /// certificate of a signing key, is made with an algorithm the epoch does not accept.
        /// </summary>
        private String? VersionEpochError(RoamingNetwork RoamingNetwork)

            => (RoamingNetwork.Operator?.KeyStatement?.Signatures ?? []).
                   Select(signature   => SignatureAlgorithmError(signature.Algorithm,   "the key statement of the roaming network operator")).
               Concat((RoamingNetwork.Operator?.SigningKeyCertificates ?? []).
                   Select(certificate => SignatureAlgorithmError(certificate.Algorithm, $"the certificate of the signing key '{certificate.SigningKey.Id}' of the roaming network operator"))).
                   FirstOrDefault(error => error is not null);

        #endregion

        #region (private) CheckpointSignatureAlgorithmError(Checkpoint, Signature)

        /// <summary>
        /// Why the given signature of the given checkpoint is not taken for its algorithm, if it is not.
        /// </summary>
        private String? CheckpointSignatureAlgorithmError(RoamingNetworkCheckpoint       Checkpoint,
                                                          RoamingNetworkCommitSignature  Signature)

            => CheckpointSignatureAlgorithmError(Checkpoint.PreviousEpoch is not null,
                                                 Signature.Algorithm,
                                                 $"a signature of the checkpoint '{Checkpoint.Id}'");

        #endregion

        #region (private) CheckpointSignatureAlgorithmError(FirstOfAnEpoch, Algorithm, What)

        /// <summary>
        /// Why a signature of a checkpoint of the given algorithm is not taken, if it is not (SPECIFICATION.md, section 11.3):
        /// the first checkpoint of an epoch after the first one is signed with the algorithms of both epochs, every later one
        /// with those of its own. Without the profiles of the epoch before, only those of its own are taken.
        /// </summary>
        private String? CheckpointSignatureAlgorithmError(Boolean  FirstOfAnEpoch,
                                                          String   Algorithm,
                                                          String   What)
        {

            if (!FirstOfAnEpoch || epoch.Profiles.AllowsSignatureAlgorithm(Algorithm))
                return SignatureAlgorithmError(Algorithm, What);

            var before = PreviousEpochProfiles;

            return before is null
                       ? $"The signature algorithm '{Algorithm}' of {What} is not one of the epoch {epoch.Number}, {String.Join(", ", epoch.Profiles.SignatureAlgorithms)}, and the profiles of the epoch {epoch.Number - 1} are not known here!"
                       : before.AllowsSignatureAlgorithm(Algorithm)
                             ? null
                             : $"The signature algorithm '{Algorithm}' of {What} is neither one of the epoch {epoch.Number}, {String.Join(", ", epoch.Profiles.SignatureAlgorithms)}, nor of the epoch {epoch.Number - 1}, {String.Join(", ", before.SignatureAlgorithms)}!";

        }

        #endregion

    }

}
