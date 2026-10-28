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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What became of a price statement given to a history.
    /// </summary>
    public enum POIPriceStatementResult
    {

        /// <summary>
        /// It was taken: the current statement of its entity now.
        /// </summary>
        Taken,

        /// <summary>
        /// It was taken before.
        /// </summary>
        Known,

        /// <summary>
        /// Its entity has a later statement already.
        /// </summary>
        Outdated,

        /// <summary>
        /// It was refused: invalid, of an unknown entity or key, or not following the statement before.
        /// </summary>
        Refused

    }


    /// <summary>
    /// The price statements of a history (SPECIFICATION.md, section 12): the current one of every charging pool, charging
    /// station and EVSE with a price status, signed here by the PriceStatementSigners whenever a price status of their
    /// charging station operators changes, or taken from another server; and those taken since the latest checkpoint,
    /// which the next checkpoint made here names by their tree.
    /// </summary>
    /// <remarks>
    /// Price statements are kept in the store as they are taken, and the statements a checkpoint names with it, so that a
    /// signer continues its chains after a restart, and in the next epoch, and a checkpoint still proves its statements. The
    /// price status itself stays runtime data: a statement taken sets it, as of its validFrom.
    /// </remarks>
    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        /// <summary>
        /// The most price statements of one charging station operator a history keeps for its next checkpoint by default.
        /// </summary>
        public const Int32  DefaultMaxPendingPriceStatementsPerOperator  = 100_000;

        /// <summary>
        /// The highest sequence of a price statement a history takes or makes: 2^53 - 1, the highest integer every JSON
        /// reader holds exactly - and one more never overflows.
        /// </summary>
        public const UInt64 MaxPriceStatementSequence                    = (1UL << 53) - 1;

        /// <summary>
        /// How far beyond the current statement of its entity the sequence of a price statement taken may lie: the
        /// statements in between did not reach this history, but not a million of them.
        /// </summary>
        public const UInt64 MaxPriceStatementSequenceGap                 = 1_000_000;

        private readonly Lock                                                                                priceStatementLock         = new ();
        private readonly Dictionary<(String EntityType, String EntityId), RoamingNetworkPriceStatement>     priceStatements            = [];
        private readonly List<RoamingNetworkPriceStatement>                                                  pendingPriceStatements     = [];
        private readonly Dictionary<ChargingStationOperator_Id, Int32>                                       pendingPerOperator         = [];
        private readonly Dictionary<RoamingNetworkCheckpoint_Id, ImmutableArray<RoamingNetworkPriceStatement>> checkpointPriceStatements = [];

        /// <summary>
        /// The last price statements of an epoch before, of entities without a current one here, as their algorithms are
        /// not those of this epoch: not taken along, but the next statement of their entities made here follows them in
        /// sequence. Nothing taken is checked against them (SPECIFICATION.md, section 11.4).
        /// </summary>
        private readonly Dictionary<(String EntityType, String EntityId), RoamingNetworkPriceStatement>     previousEpochPriceStatements = [];

        /// <summary>
        /// The latest checkpoint, for price statements to name without the lock of the history:
        /// they are made while the status schedule of an entity is locked.
        /// </summary>
        private          RoamingNetworkCheckpoint?                                                           latestCheckpointForPrices;

        #endregion

        #region Properties

        /// <summary>
        /// The keys that sign a price statement here whenever the price status of a charging pool, a charging station or
        /// an EVSE of their charging station operators changes: signing keys of the operators, the Signer of each naming
        /// its operator, e.g. DE*GEF.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommitSigner>  PriceStatementSigners    { get; set; } = [];

        /// <summary>
        /// How long a price statement made here is valid, unless until the next one of its entity: null by default.
        /// </summary>
        public TimeSpan?                                   PriceStatementValidity   { get; set; }

        /// <summary>
        /// How long before its end RefreshPriceStatements(…) states the price status of an entity anew, when price
        /// statements made here end: 5 minutes by default.
        /// </summary>
        public TimeSpan                                    PriceStatementRenewal    { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The most price statements of one charging station operator this history keeps for its next checkpoint; beyond,
        /// it drops the oldest of that operator, and never those of another one: DefaultMaxPendingPriceStatementsPerOperator
        /// unless set.
        /// </summary>
        public Int32                                       MaxPendingPriceStatementsPerOperator { get; set; } = DefaultMaxPendingPriceStatementsPerOperator;

        /// <summary>
        /// The current price statement of every entity, by their entity types and identifications in ordinal order.
        /// </summary>
        public IReadOnlyList<RoamingNetworkPriceStatement> PriceStatements
        {
            get
            {
                lock (priceStatementLock)
                {
                    return RoamingNetworkPriceStatement.InOrder(priceStatements.Values);
                }
            }
        }

        /// <summary>
        /// The price statements taken since the latest checkpoint, which the next checkpoint made here names, in the order
        /// they were taken.
        /// </summary>
        public IReadOnlyList<RoamingNetworkPriceStatement> PendingPriceStatements
        {
            get
            {
                lock (priceStatementLock)
                {
                    return [ .. pendingPriceStatements ];
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Called for every price statement this history takes, made here or taken from another server, while its price
        /// statements are locked: return quickly and do not give it price statements from there.
        /// </summary>
        public event OnRoamingNetworkPriceStatementDelegate?  OnPriceStatement;

        #endregion


        #region (static) ChainOf(EntityType, EntityId)

        /// <summary>
        /// The key of the chain of price statements of the given entity: its type and its identification as the
        /// identifications compare - without '*', whitespace around it, or the case of its letters - so that two
        /// spellings of one EVSE, "DE*GEF*E1*1" and "DE*GEF*E11", are one chain, and a split view between them is seen.
        /// </summary>
        /// <param name="EntityType">ChargingPool, ChargingStation or EVSE.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        public static (String EntityType, String EntityId) ChainOf(String  EntityType,
                                                                   String  EntityId)

            => (EntityType, EntityId.Trim().Replace("*", "").ToUpperInvariant());

        private static (String EntityType, String EntityId) ChainOf(RoamingNetworkPriceStatement PriceStatement)

            => ChainOf(PriceStatement.EntityType, PriceStatement.EntityId);

        #endregion

        #region TryGetPriceStatement(EntityType, EntityId, out PriceStatement)

        /// <summary>
        /// Try to return the current price statement of the given charging pool, charging station or EVSE.
        /// </summary>
        /// <param name="EntityType">ChargingPool, ChargingStation or EVSE.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="PriceStatement">Its current price statement.</param>
        public Boolean TryGetPriceStatement(String                                                  EntityType,
                                            String                                                  EntityId,
                                            [NotNullWhen(true)] out RoamingNetworkPriceStatement?   PriceStatement)
        {
            lock (priceStatementLock)
            {
                return priceStatements.TryGetValue(ChainOf(EntityType, EntityId), out PriceStatement);
            }
        }

        #endregion

        #region PriceStatementsOf(ChargingStationId)

        /// <summary>
        /// The current price statements a driver's app needs at the given charging station of the head, with its proof: of
        /// its charging pool, of itself and of its EVSEs, in this order.
        /// </summary>
        /// <param name="ChargingStationId">The identification of a charging station.</param>
        public IReadOnlyList<RoamingNetworkPriceStatement> PriceStatementsOf(ChargingStation_Id ChargingStationId)
        {

            var network = Head.RoamingNetwork;

            if (!network.TryGetChargingStationOperatorById(ChargingStationId.OperatorId, out var chargingStationOperator) ||
                !chargingStationOperator.TryGetChargingStation(ChargingStationId, out var chargingStation))
            {
                return [];
            }

            var chargingPool = chargingStationOperator.ChargingPools.FirstOrDefault(chargingPool => chargingPool.ChargingStations.Contains(ChargingStationId));
            var entities     = new List<(String, String)>();

            if (chargingPool is not null)
                entities.Add((nameof(ChargingPool), chargingPool.Id.ToString()));

            entities.Add((nameof(ChargingStation), ChargingStationId.ToString()));
            entities.AddRange(chargingStation.EVSEs.Select(evse => (nameof(EVSE), evse.Id.ToString())));

            return PriceStatementsOf(entities);

        }

        #endregion

        #region PriceStatementsOf(EVSEId)

        /// <summary>
        /// The current price statements a driver's app needs at the given EVSE of the head, with the proof of its charging
        /// station: of its charging pool, of its charging station and of itself, in this order.
        /// </summary>
        /// <param name="EVSEId">The identification of an EVSE.</param>
        public IReadOnlyList<RoamingNetworkPriceStatement> PriceStatementsOf(EVSE_Id EVSEId)
        {

            var network = Head.RoamingNetwork;

            if (!network.TryGetChargingStationOperatorById(EVSEId.OperatorId, out var chargingStationOperator))
                return [];

            var chargingStation  = chargingStationOperator.ChargingPools.SelectMany(chargingPool => chargingPool.ChargingStations).FirstOrDefault(chargingStation => chargingStation.EVSEs.Contains(EVSEId));

            if (chargingStation is null)
                return [];

            var chargingPool     = chargingStationOperator.ChargingPools.FirstOrDefault(chargingPool => chargingPool.ChargingStations.Contains(chargingStation.Id));
            var entities         = new List<(String, String)>();

            if (chargingPool is not null)
                entities.Add((nameof(ChargingPool), chargingPool.Id.ToString()));

            entities.Add((nameof(ChargingStation), chargingStation.Id.ToString()));
            entities.Add((nameof(EVSE),            EVSEId.ToString()));

            return PriceStatementsOf(entities);

        }

        #endregion

        #region TryAddPriceStatement(PriceStatement, out ErrorResponse, At = null)

        /// <summary>
        /// Take a price statement of another server, e.g. of one of its charging station operator, after checking it
        /// against the head (SPECIFICATION.md, section 12): its entity is one of the head, signed by a signing key of the
        /// operator of the entity valid when it was signed, naming charging tariffs of that operator, not over at the given
        /// time, now by default; and it follows the current statement of its entity: one more in sequence, naming its
        /// digest, or a later one when some in between did not reach this history. Taken, it sets the price status of the
        /// entity as of its validFrom.
        /// </summary>
        /// <param name="PriceStatement">The price statement.</param>
        /// <param name="ErrorResponse">Why it was refused or is out of date.</param>
        /// <param name="At">The time, now by default.</param>
        public POIPriceStatementResult TryAddPriceStatement(RoamingNetworkPriceStatement  PriceStatement,
                                                            out String?                   ErrorResponse,
                                                            DateTimeOffset?               At   = null)
        {

            if (PriceStatement is null)
            {
                ErrorResponse = "The given price statement must not be null!";
                return POIPriceStatementResult.Refused;
            }

            if (!CheckPriceStatement(PriceStatement, Head.RoamingNetwork, At ?? Timestamp.Now, out ErrorResponse))
                return POIPriceStatementResult.Refused;

            POIPriceStatementResult result;

            lock (priceStatementLock)
            {
                result = TakePriceStatement(PriceStatement, out ErrorResponse);
            }

            // Outside of the lock of the price statements: the status schedule of the entity is locked while it changes.
            if (result == POIPriceStatementResult.Taken)
                RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus,
                                        PriceStatement.EntityType,
                                        PriceStatement.EntityId,
                                        PriceStatement.PriceStatus.Text,
                                        PriceStatement.ValidFrom,
                                        out _,
                                        out _);

            return result;

        }

        #endregion

        #region TryStatePrices(EntityType, EntityId, out PriceStatement, out ErrorResponse, At = null)

        /// <summary>
        /// State the current price status of the given charging pool, charging station or EVSE anew, signed by the
        /// PriceStatementSigner of its charging station operator, e.g. after a restart.
        /// </summary>
        /// <param name="EntityType">ChargingPool, ChargingStation or EVSE.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="PriceStatement">The new price statement.</param>
        /// <param name="ErrorResponse">Why none was made.</param>
        /// <param name="At">When it is signed, now by default.</param>
        public Boolean TryStatePrices(String                                                  EntityType,
                                      String                                                  EntityId,
                                      [NotNullWhen(true)]  out RoamingNetworkPriceStatement?  PriceStatement,
                                      [NotNullWhen(false)] out String?                        ErrorResponse,
                                      DateTimeOffset?                                         At   = null)
        {

            PriceStatement = null;

            if (RoamingNetworkPriceStatement.OperatorOf(EntityType, EntityId) is not ChargingStationOperator_Id operatorId)
            {
                ErrorResponse = $"Invalid identification of a {EntityType}: only charging pools, charging stations and EVSEs have a price status!";
                return false;
            }

            if (PriceStatementSignerOf(operatorId) is not RoamingNetworkCommitSigner signer)
            {
                ErrorResponse = $"This history has no price statement signer of the charging station operator '{operatorId}'!";
                return false;
            }

            if (CurrentPriceStatusOf(EntityType, EntityId) is not ChargingPrices prices)
            {
                ErrorResponse = $"The {EntityType} '{EntityId}' has no price status!";
                return false;
            }

            return TryStateAnew(signer, EntityType, EntityId, prices, At ?? Timestamp.Now, OnlyWhenChanged: false, out PriceStatement, out ErrorResponse);

        }

        #endregion

        #region RefreshPriceStatements(At = null)

        /// <summary>
        /// State the price status of every entity anew whose current price statement was made here and ends within the
        /// PriceStatementRenewal: call it regularly, e.g. every minute, when the PriceStatementValidity is set, so that the
        /// price status stays stated without changing.
        /// </summary>
        /// <param name="At">The time, now by default.</param>
        /// <returns>The new price statements.</returns>
        public IReadOnlyList<RoamingNetworkPriceStatement> RefreshPriceStatements(DateTimeOffset? At = null)
        {

            var at       = At ?? Timestamp.Now;
            var signers  = PriceStatementSigners;

            if (signers.IsDefaultOrEmpty || !PriceStatementValidity.HasValue)
                return [];

            RoamingNetworkPriceStatement[] ending;

            lock (priceStatementLock)
            {
                ending = [ .. priceStatements.Values.Where(priceStatement => priceStatement.ValidUntil.HasValue &&
                                                                             priceStatement.ValidUntil.Value - at < PriceStatementRenewal &&
                                                                             signers.Any(signer => signer.Signer == priceStatement.Signer.ToString() &&
                                                                                                   signer.KeyId  == priceStatement.KeyId)) ];
            }

            var made = new List<RoamingNetworkPriceStatement>();

            foreach (var priceStatement in ending)
                if (PriceStatementSignerOf(priceStatement.Signer) is RoamingNetworkCommitSigner signer &&
                    CurrentPriceStatusOf(priceStatement.EntityType, priceStatement.EntityId) is ChargingPrices prices &&
                    TryStateAnew(signer, priceStatement.EntityType, priceStatement.EntityId, prices, at, OnlyWhenChanged: false, out var newStatement, out _))
                {
                    made.Add(newStatement);
                }

            return made;

        }

        #endregion


        #region PriceStatementsOf(CheckpointId)

        /// <summary>
        /// The price statements the given checkpoint names, in the order of its tree, when this history holds them:
        /// it made the checkpoint, or checked them when it took it. Null otherwise.
        /// </summary>
        /// <param name="CheckpointId">The identification of a checkpoint.</param>
        public IReadOnlyList<RoamingNetworkPriceStatement>? PriceStatementsOf(RoamingNetworkCheckpoint_Id CheckpointId)
        {
            lock (priceStatementLock)
            {
                return checkpointPriceStatements.TryGetValue(CheckpointId, out var ordered)
                           ? ordered
                           : null;
            }
        }

        #endregion

        #region TryGetPriceStatementProof(CheckpointId, Digest, out Proof, out ErrorResponse)

        /// <summary>
        /// Try to make the proof that the price statement of the given digest is one the given checkpoint names.
        /// </summary>
        /// <param name="CheckpointId">The identification of a checkpoint.</param>
        /// <param name="Digest">The digest of a price statement.</param>
        /// <param name="Proof">The proof.</param>
        /// <param name="ErrorResponse">Why there is none.</param>
        public Boolean TryGetPriceStatementProof(RoamingNetworkCheckpoint_Id                                  CheckpointId,
                                                 String                                                       Digest,
                                                 [NotNullWhen(true)]  out RoamingNetworkPriceStatementProof?  Proof,
                                                 [NotNullWhen(false)] out String?                             ErrorResponse)
        {

            Proof = null;

            if (!TryGetCheckpoint(CheckpointId, out var checkpoint))
            {
                ErrorResponse = $"The checkpoint '{CheckpointId}' is unknown here!";
                return false;
            }

            if (PriceStatementsOf(CheckpointId) is not IReadOnlyList<RoamingNetworkPriceStatement> ordered)
            {
                ErrorResponse = checkpoint.PriceStatementsSize == 0
                                    ? $"The checkpoint '{CheckpointId}' names no price statements!"
                                    : $"The price statements of the checkpoint '{CheckpointId}' are not held here!";
                return false;
            }

            var index = -1;

            for (var position = 0; position < ordered.Count; position++)
                if (ordered[position].Digest == Digest)
                {
                    index = position;
                    break;
                }

            if (index < 0)
            {
                ErrorResponse = $"The checkpoint '{CheckpointId}' names no price statement '{Digest}'!";
                return false;
            }

            Proof          = new RoamingNetworkPriceStatementProof(CheckpointId,
                                                                   ordered[index],
                                                                   (UInt64) index,
                                                                   (UInt64) ordered.Count,
                                                                   POIMerkleTree.AuditPathOfLeafHashes(checkpoint.IdentificationHash,
                                                                                                       [ .. ordered.Select(priceStatement => POIMerkleTree.LeafHash(checkpoint.IdentificationHash, priceStatement.Leaf)) ],
                                                                                                       index));

            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryAddCheckpointPriceStatements(CheckpointId, PriceStatements, out ErrorResponse)

        /// <summary>
        /// Take the price statements a checkpoint of this history names, e.g. those of a checkpoint taken from another server
        /// without them, after checking them (SPECIFICATION.md, section 12): their tree is the one of the checkpoint, every one
        /// was signed before it, by a signing key of its charging station operator in the version of the commit of the
        /// checkpoint or of the checkpoint before. A witness signs a checkpoint naming price statements only then.
        /// </summary>
        /// <param name="CheckpointId">The identification of the checkpoint.</param>
        /// <param name="PriceStatements">The price statements it names, in any order.</param>
        /// <param name="ErrorResponse">Why they were refused.</param>
        public Boolean TryAddCheckpointPriceStatements(RoamingNetworkCheckpoint_Id                CheckpointId,
                                                       IEnumerable<RoamingNetworkPriceStatement>  PriceStatements,
                                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            lock (historyLock)
            {

                if (!checkpointIndex.TryGetValue(CheckpointId, out var index))
                {
                    ErrorResponse = $"The checkpoint '{CheckpointId}' is unknown here!";
                    return false;
                }

                var checkpoint = checkpoints[index];

                if (!CheckCheckpointPriceStatements(checkpoint, PriceStatements, out var ordered, out ErrorResponse))
                    return false;

                lock (priceStatementLock)
                {

                    if (checkpointPriceStatements.ContainsKey(CheckpointId))
                        return true;

                    // Kept before it is shown: a store failing keeps them from the history.
                    StoreAppend(RoamingNetworkHistoryLogEntry.ForCheckpointPriceStatements(CheckpointId, ordered));

                    TakeCheckpointPriceStatements(CheckpointId, ordered);

                }

                return true;

            }

        }

        #endregion


        #region (private) CheckPriceStatement(PriceStatement, RoamingNetwork, At, out ErrorResponse)

        /// <summary>
        /// Check a price statement against the given version: its entity, its operator, the key it is signed with and the
        /// charging tariffs it names; and that it is not over at the given time.
        /// </summary>
        private Boolean CheckPriceStatement(RoamingNetworkPriceStatement      PriceStatement,
                                            RoamingNetwork                    RoamingNetwork,
                                            DateTimeOffset                    At,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (PriceStatement.RoamingNetworkId != RoamingNetworkId)
            {
                ErrorResponse = $"The price statement is one of the roaming network '{PriceStatement.RoamingNetworkId}', not of '{RoamingNetworkId}'!";
                return false;
            }

            if (SignatureAlgorithmError(PriceStatement.Algorithm, "the price statement") is String algorithmError)
            {
                ErrorResponse = algorithmError;
                return false;
            }

            if (PriceStatement.ValidUntil.HasValue && At > PriceStatement.ValidUntil.Value)
            {
                ErrorResponse = $"The price statement is no longer valid: it ended at {PriceStatement.ValidUntil.Value.ToISO8601()}!";
                return false;
            }

            if (!RoamingNetwork.TryGetChargingStationOperatorById(PriceStatement.Signer, out var chargingStationOperator))
            {
                ErrorResponse = $"The charging station operator '{PriceStatement.Signer}' of the price statement is unknown!";
                return false;
            }

            var spelled = PriceStatement.EntityType switch {
                              nameof(ChargingPool)     => chargingStationOperator.TryGetChargingPool   (ChargingPool_Id.   Parse(PriceStatement.EntityId), out var chargingPool)    ? chargingPool.   Id.ToString() : null,
                              nameof(ChargingStation)  => chargingStationOperator.TryGetChargingStation(ChargingStation_Id.Parse(PriceStatement.EntityId), out var chargingStation) ? chargingStation.Id.ToString() : null,
                              _                        => chargingStationOperator.TryGetEVSE           (EVSE_Id.           Parse(PriceStatement.EntityId), out var evse)            ? evse.           Id.ToString() : null
                          };

            if (spelled is null)
            {
                ErrorResponse = $"The {PriceStatement.EntityType} '{PriceStatement.EntityId}' of the price statement is unknown!";
                return false;
            }

            // One spelling per entity, the head's: another one would be a chain of its own to everybody comparing texts,
            // and the identifications that compare equal are as many as the ways of writing them.
            if (!String.Equals(spelled, PriceStatement.EntityId, StringComparison.Ordinal))
            {
                ErrorResponse = $"The price statement names the {PriceStatement.EntityType} '{spelled}' as '{PriceStatement.EntityId}': it has to name it as the roaming network does!";
                return false;
            }

            foreach (var price in PriceStatement.PriceStatus.Prices)
                if (!chargingStationOperator.ChargingTariffs.Any(chargingTariff => chargingTariff.Id == price.ChargingTariffId))
                {
                    ErrorResponse = $"The charging tariff '{price.ChargingTariffId}' of the price statement is no tariff of the charging station operator '{chargingStationOperator.Id}'!";
                    return false;
                }

            return PriceStatement.TryVerify(chargingStationOperator.SigningKeys, out ErrorResponse);

        }

        #endregion

        #region (private) TakePriceStatement(PriceStatement, out ErrorResponse)

        /// <summary>
        /// Take the given checked price statement, while the price statements are locked, unless it does not follow the
        /// current one of its entity: keep it in the store, as the current one of its entity and for the next checkpoint,
        /// and announce it.
        /// </summary>
        private POIPriceStatementResult TakePriceStatement(RoamingNetworkPriceStatement  PriceStatement,
                                                           out String?                   ErrorResponse)
        {

            var key = ChainOf(PriceStatement);

            if (PriceStatement.Sequence > MaxPriceStatementSequence)
            {
                ErrorResponse = $"The price statement {PriceStatement.Sequence} of the {PriceStatement.EntityType} '{PriceStatement.EntityId}' is beyond the highest sequence, {MaxPriceStatementSequence}!";
                return POIPriceStatementResult.Refused;
            }

            if (priceStatements.TryGetValue(key, out var current))
            {

                if (current.Digest == PriceStatement.Digest)
                {
                    ErrorResponse = null;
                    return POIPriceStatementResult.Known;
                }

                if (PriceStatement.Sequence < current.Sequence)
                {
                    ErrorResponse = $"The {PriceStatement.EntityType} '{PriceStatement.EntityId}' has the price statement {current.Sequence} already, a later one than {PriceStatement.Sequence}!";
                    return POIPriceStatementResult.Outdated;
                }

                if (PriceStatement.Sequence == current.Sequence)
                {
                    ErrorResponse = $"The {PriceStatement.EntityType} '{PriceStatement.EntityId}' has another price statement {current.Sequence}: two statements of one sequence are a split view!";
                    return POIPriceStatementResult.Refused;
                }

                if (PriceStatement.Sequence == current.Sequence + 1 && !PriceStatement.Follows(current))
                {
                    ErrorResponse = $"The price statement {PriceStatement.Sequence} of the {PriceStatement.EntityType} '{PriceStatement.EntityId}' does not name the digest of the statement {current.Sequence} before!";
                    return POIPriceStatementResult.Refused;
                }

                // Lower than the highest sequence, so neither overflows.
                if (PriceStatement.Sequence - current.Sequence > MaxPriceStatementSequenceGap)
                {
                    ErrorResponse = $"The price statement {PriceStatement.Sequence} of the {PriceStatement.EntityType} '{PriceStatement.EntityId}' lies more than {MaxPriceStatementSequenceGap} beyond the current one, {current.Sequence}!";
                    return POIPriceStatementResult.Refused;
                }

            }

            try
            {
                // Kept before it is shown: a store failing keeps it from the history.
                StoreAppend(RoamingNetworkHistoryLogEntry.ForPriceStatement(PriceStatement));
                CompactIfDue();
            }
            catch (Exception e)
            {
                RaiseStoreError("A price statement can not be stored: " + e.Message, e);
                ErrorResponse = "The price statement can not be stored: " + e.Message;
                return POIPriceStatementResult.Refused;
            }

            TakeStoredPriceStatement(PriceStatement);

            // One failing subscriber must neither fail the statement nor keep it from the others.
            foreach (var subscriber in OnPriceStatement?.GetInvocationList().Cast<OnRoamingNetworkPriceStatementDelegate>() ?? [])
            {
                try
                {
                    subscriber(PriceStatement);
                }
                catch
                { }
            }

            ErrorResponse = null;
            return POIPriceStatementResult.Taken;

        }

        #endregion

        #region (private) TakeStoredPriceStatement(PriceStatement)

        /// <summary>
        /// Hold a price statement, while the price statements are locked: as the current one of its entity, unless a later
        /// one is, and for the next checkpoint. Also while loading the store, where it was checked before it was kept.
        /// </summary>
        private void TakeStoredPriceStatement(RoamingNetworkPriceStatement PriceStatement)
        {

            var key = ChainOf(PriceStatement);

            if (!priceStatements.TryGetValue(key, out var current) || current.Sequence < PriceStatement.Sequence)
                priceStatements[key] = PriceStatement;

            previousEpochPriceStatements.Remove(key);

            pendingPriceStatements.Add(PriceStatement);

            // An operator stating more than its share drops its own oldest, never those of another one: they are the
            // evidence of a checkpoint (SPECIFICATION.md, section 12.4).
            var pending = pendingPerOperator[PriceStatement.Signer] = pendingPerOperator.GetValueOrDefault(PriceStatement.Signer) + 1;

            if (pending > MaxPendingPriceStatementsPerOperator)
            {
                pendingPriceStatements.RemoveAt(pendingPriceStatements.FindIndex(priceStatement => priceStatement.Signer == PriceStatement.Signer));
                pendingPerOperator[PriceStatement.Signer] = pending - 1;
            }

        }

        #endregion

        #region (private) TakePreviousEpochPriceStatement(PriceStatement)

        /// <summary>
        /// Hold the last price statement of an entity in an epoch before, not taken along, while the price statements are
        /// locked: the next statement of its entity made here follows it, unless the entity has a current one.
        /// </summary>
        private void TakePreviousEpochPriceStatement(RoamingNetworkPriceStatement PriceStatement)
        {

            var key = ChainOf(PriceStatement);

            if (!priceStatements.ContainsKey(key))
                previousEpochPriceStatements[key] = PriceStatement;

        }

        #endregion

        #region (private) TryStateAnew(Signer, EntityType, EntityId, Prices, At, OnlyWhenChanged, out PriceStatement, out ErrorResponse)

        /// <summary>
        /// Sign a price statement of the given prices of the given entity, following its current one, or the last one of an
        /// epoch before, and take it.
        /// </summary>
        private Boolean TryStateAnew(RoamingNetworkCommitSigner                              Signer,
                                     String                                                  EntityType,
                                     String                                                  EntityId,
                                     ChargingPrices                                          Prices,
                                     DateTimeOffset                                          At,
                                     Boolean                                                 OnlyWhenChanged,
                                     [NotNullWhen(true)]  out RoamingNetworkPriceStatement?  PriceStatement,
                                     [NotNullWhen(false)] out String?                        ErrorResponse)
        {

            PriceStatement = null;

            var network = Head.RoamingNetwork;

            lock (priceStatementLock)
            {

                var key = ChainOf(EntityType, EntityId);

                priceStatements.TryGetValue(key, out var current);

                if (OnlyWhenChanged && current is not null && current.PriceStatus.Text == Prices.Text)
                {
                    ErrorResponse = "The price status is stated already.";
                    return false;
                }

                // Its chain goes on across epochs, also from a statement not taken along.
                var previous  = current ?? previousEpochPriceStatements.GetValueOrDefault(key);

                if (previous is not null && previous.Sequence >= MaxPriceStatementSequence)
                {
                    ErrorResponse = $"The chain of price statements of the {EntityType} '{EntityId}' is at its end, {previous.Sequence}!";
                    return false;
                }

                var statement = Signer.StatePrices(RoamingNetworkId,
                                                   EntityType,
                                                   EntityId,
                                                   Prices,
                                                   previous is not null ? previous.Sequence + 1 : 0,
                                                   previous?.Digest,
                                                   At,
                                                   At,
                                                   PriceStatementValidity.HasValue ? At + PriceStatementValidity.Value : null,
                                                   Volatile.Read(ref latestCheckpointForPrices)?.Id);

                // A signer whose key is no signing key of its operator states nothing others would take.
                if (!CheckPriceStatement(statement, network, statement.SignedAt, out ErrorResponse))
                    return false;

                var result = TakePriceStatement(statement, out var takeError);

                if (result != POIPriceStatementResult.Taken)
                {
                    ErrorResponse = takeError ?? $"The new price statement is {result}!";
                    return false;
                }

                PriceStatement = statement;
                return true;

            }

        }

        #endregion

        #region (private) PriceStatusChanged(StatusUpdate)

        /// <summary>
        /// State a changed price status of an entity of a charging station operator this history holds a
        /// PriceStatementSigner of, unless it is the one stated already, e.g. one a price statement just set.
        /// Failing to state it never fails the status update.
        /// </summary>
        private void PriceStatusChanged(POIStatusUpdate StatusUpdate)
        {

            if (loading || StatusUpdate.Kind != POIRuntimeStatusKind.PriceStatus || PriceStatementSigners.IsDefaultOrEmpty)
                return;

            if (RoamingNetworkPriceStatement.OperatorOf(StatusUpdate.EntityType, StatusUpdate.EntityId) is not ChargingStationOperator_Id operatorId ||
                PriceStatementSignerOf(operatorId) is not RoamingNetworkCommitSigner signer ||
                !ChargingPrices.TryParseText(StatusUpdate.Value, out var prices))
            {
                return;
            }

            try
            {
                TryStateAnew(signer, StatusUpdate.EntityType, StatusUpdate.EntityId, prices, Timestamp.Now, OnlyWhenChanged: true, out _, out _);
            }
            catch
            { }

        }

        #endregion

        #region (private) PriceStatementSignerOf(OperatorId)

        private RoamingNetworkCommitSigner? PriceStatementSignerOf(ChargingStationOperator_Id OperatorId)

            => PriceStatementSigners.IsDefaultOrEmpty
                   ? null
                   : PriceStatementSigners.FirstOrDefault(signer => signer.Signer == OperatorId.ToString());

        #endregion

        #region (private) CurrentPriceStatusOf(EntityType, EntityId)

        /// <summary>
        /// The current price status of the given entity of the head, if it has one.
        /// </summary>
        private ChargingPrices? CurrentPriceStatusOf(String  EntityType,
                                                     String  EntityId)
        {

            if (RoamingNetworkPriceStatement.OperatorOf(EntityType, EntityId) is not ChargingStationOperator_Id operatorId ||
                !Head.RoamingNetwork.TryGetChargingStationOperatorById(operatorId, out var chargingStationOperator))
            {
                return null;
            }

            Timestamped<ChargingPrices>? priceStatus = EntityType switch {
                nameof(ChargingPool)     => chargingStationOperator.TryGetChargingPool   (ChargingPool_Id.   Parse(EntityId), out var chargingPool)    ? chargingPool.   PriceStatus : null,
                nameof(ChargingStation)  => chargingStationOperator.TryGetChargingStation(ChargingStation_Id.Parse(EntityId), out var chargingStation) ? chargingStation.PriceStatus : null,
                _                        => chargingStationOperator.TryGetEVSE           (EVSE_Id.           Parse(EntityId), out var evse)            ? evse.           PriceStatus : null
            };

            return priceStatus?.Value;

        }

        #endregion

        #region (private) PriceStatementsOf(Entities)

        private IReadOnlyList<RoamingNetworkPriceStatement> PriceStatementsOf(IEnumerable<(String EntityType, String EntityId)> Entities)
        {
            lock (priceStatementLock)
            {
                return [ .. Entities.Select(entity => priceStatements.TryGetValue(ChainOf(entity.EntityType, entity.EntityId), out var priceStatement) ? priceStatement : null).
                                     OfType<RoamingNetworkPriceStatement>() ];
            }
        }

        #endregion


        #region (private) CheckCheckpointPriceStatements(Checkpoint, PriceStatements, out Ordered, out ErrorResponse)

        /// <summary>
        /// Check the price statements a checkpoint of this history names, while the history is locked.
        /// </summary>
        private Boolean CheckCheckpointPriceStatements(RoamingNetworkCheckpoint                                                      Checkpoint,
                                                       IEnumerable<RoamingNetworkPriceStatement>                                     PriceStatements,
                                                       [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkPriceStatement>?        Ordered,
                                                       [NotNullWhen(false)] out String?                                              ErrorResponse)
        {

            Ordered = null;

            if (Checkpoint.PriceStatementsRoot is null)
            {
                ErrorResponse = $"The checkpoint '{Checkpoint.Id}' names no price statements!";
                return false;
            }

            var ordered = RoamingNetworkPriceStatement.InOrder(PriceStatements);

            if ((UInt64) ordered.Count != Checkpoint.PriceStatementsSize ||
                RoamingNetworkPriceStatement.RootOf(ordered, Checkpoint.IdentificationHash) != Checkpoint.PriceStatementsRoot)
            {
                ErrorResponse = $"The price statements are not those the checkpoint '{Checkpoint.Id}' names!";
                return false;
            }

            // The keys of the operators as of the checkpoint and as of the one before: a key replaced in between signed
            // statements of the checkpoint as well.
            var keyVersions = new List<RoamingNetwork>();

            if (TryGetVersion(Checkpoint.CommitId, out var version, out _))
                keyVersions.Add(version);

            if (Checkpoint.PreviousId.HasValue &&
                checkpointIndex.TryGetValue(Checkpoint.PreviousId.Value, out var previousIndex) &&
                TryGetVersion(checkpoints[previousIndex].CommitId, out var previousVersion, out _))
            {
                keyVersions.Add(previousVersion);
            }

            var ofChains = new Dictionary<((String, String) Chain, UInt64 Sequence), String>();

            foreach (var priceStatement in ordered)
            {

                if (priceStatement.RoamingNetworkId != RoamingNetworkId)
                {
                    ErrorResponse = $"The price statement {priceStatement.Digest} is one of the roaming network '{priceStatement.RoamingNetworkId}'!";
                    return false;
                }

                if (priceStatement.Sequence > MaxPriceStatementSequence)
                {
                    ErrorResponse = $"The price statement {priceStatement.Digest} of the checkpoint '{Checkpoint.Id}' is beyond the highest sequence, {MaxPriceStatementSequence}!";
                    return false;
                }

                // Two of one entity and sequence, in any spelling, are a split view a checkpoint does not name.
                if (ofChains.TryGetValue((ChainOf(priceStatement), priceStatement.Sequence), out var other) && other != priceStatement.Digest)
                {
                    ErrorResponse = $"The checkpoint '{Checkpoint.Id}' names two price statements {priceStatement.Sequence} of the {priceStatement.EntityType} '{priceStatement.EntityId}', {other} and {priceStatement.Digest}: a split view!";
                    return false;
                }

                ofChains[(ChainOf(priceStatement), priceStatement.Sequence)] = priceStatement.Digest;

                if (priceStatement.SignedAt > Checkpoint.CreatedAt)
                {
                    ErrorResponse = $"The price statement {priceStatement.Digest} was signed after the checkpoint '{Checkpoint.Id}' was made!";
                    return false;
                }

                var signingKeys = keyVersions.SelectMany(roamingNetwork => roamingNetwork.TryGetChargingStationOperatorById(priceStatement.Signer, out var chargingStationOperator)
                                                                               ? chargingStationOperator.SigningKeys
                                                                               : []).
                                                 DistinctBy(signingKey => signingKey.Id);

                if (!priceStatement.TryVerify(signingKeys, out var verifyError))
                {
                    ErrorResponse = $"The price statement {priceStatement.Digest} of the checkpoint '{Checkpoint.Id}' is invalid: {verifyError}";
                    return false;
                }

            }

            Ordered        = ordered;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (private) PriceStatementsForCheckpoint(CreatedAt)

        /// <summary>
        /// The price statements the next checkpoint made at the given time names, while the price statements are locked:
        /// those taken since the latest checkpoint, signed before it.
        /// </summary>
        private IReadOnlyList<RoamingNetworkPriceStatement> PriceStatementsForCheckpoint(DateTimeOffset CreatedAt)

            => RoamingNetworkPriceStatement.InOrder(pendingPriceStatements.Where(priceStatement => priceStatement.SignedAt <= CreatedAt));

        #endregion

        #region (private) TakeCheckpointPriceStatements(CheckpointId, Ordered)

        /// <summary>
        /// Hold the price statements of a checkpoint, while the price statements are locked: also the current one of its
        /// entity, unless a later one is.
        /// </summary>
        private void TakeCheckpointPriceStatements(RoamingNetworkCheckpoint_Id                  CheckpointId,
                                                   IReadOnlyList<RoamingNetworkPriceStatement>  Ordered)
        {

            checkpointPriceStatements[CheckpointId] = [ .. Ordered ];

            foreach (var priceStatement in Ordered)
            {
                var key = ChainOf(priceStatement);
                if (!priceStatements.TryGetValue(key, out var current) || current.Sequence < priceStatement.Sequence)
                    priceStatements[key] = priceStatement;
                previousEpochPriceStatements.Remove(key);
            }

            var digests = Ordered.Select(priceStatement => priceStatement.Digest).ToHashSet();

            RemovePendingPriceStatements(priceStatement => digests.Contains(priceStatement.Digest));

        }

        #endregion

        #region (private) PrunePendingPriceStatements(Checkpoint)

        /// <summary>
        /// Drop the price statements signed before the given new checkpoint from those for the next one, while the price
        /// statements are locked: it names them, or its server never took them.
        /// </summary>
        private void PrunePendingPriceStatements(RoamingNetworkCheckpoint Checkpoint)
        {

            RemovePendingPriceStatements(priceStatement => priceStatement.SignedAt <= Checkpoint.CreatedAt);

            Volatile.Write(ref latestCheckpointForPrices, Checkpoint);

        }

        #endregion

        #region (private) RemovePendingPriceStatements(Match)

        /// <summary>
        /// Drop the matching price statements from those for the next checkpoint, while the price statements are locked,
        /// and count those left per operator again.
        /// </summary>
        private void RemovePendingPriceStatements(Predicate<RoamingNetworkPriceStatement> Match)
        {

            if (pendingPriceStatements.RemoveAll(Match) == 0)
                return;

            pendingPerOperator.Clear();

            foreach (var priceStatement in pendingPriceStatements)
                pendingPerOperator[priceStatement.Signer] = pendingPerOperator.GetValueOrDefault(priceStatement.Signer) + 1;

        }

        #endregion

        #region (private) PriceStatementLogEntries()

        /// <summary>
        /// The log entries of the price statements that are not those of a checkpoint, the current ones and those for the
        /// next checkpoint, e.g. for a compacted log: the current ones first, so that the checkpoints after them drop those
        /// signed before them again, and before them the last ones of an epoch before, not taken along.
        /// </summary>
        private (IReadOnlyList<RoamingNetworkHistoryLogEntry> Before, IReadOnlyList<RoamingNetworkHistoryLogEntry> After) PriceStatementLogEntries()
        {

            lock (priceStatementLock)
            {

                var ofCheckpoints  = checkpointPriceStatements.Values.SelectMany(ordered => ordered).Select(priceStatement => priceStatement.Digest).ToHashSet();
                var pending        = pendingPriceStatements.Select(priceStatement => priceStatement.Digest).ToHashSet();

                var before         = RoamingNetworkPriceStatement.InOrder(previousEpochPriceStatements.Values).
                                                          Select(RoamingNetworkHistoryLogEntry.ForPreviousEpochPriceStatement).
                                                          Concat(RoamingNetworkPriceStatement.InOrder(priceStatements.Values.Where(priceStatement => !ofCheckpoints.Contains(priceStatement.Digest) &&
                                                                                                                                !pending.      Contains(priceStatement.Digest))).
                                                                 Select(RoamingNetworkHistoryLogEntry.ForPriceStatement)).
                                                          ToArray();

                var after          = pendingPriceStatements.Select(RoamingNetworkHistoryLogEntry.ForPriceStatement).ToArray();

                return (before, after);

            }

        }

        #endregion

    }

}
