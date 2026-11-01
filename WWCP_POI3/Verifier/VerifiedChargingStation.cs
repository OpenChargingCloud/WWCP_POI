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
    /// The current price of an entity, by a verified price statement.
    /// </summary>
    /// <param name="Statement">The price statement.</param>
    /// <param name="Digest">Its digest, which a checkpoint names and its proof proves (SPECIFICATION.md, section 12.3).</param>
    public sealed record VerifiedPrice(RoamingNetworkPriceStatement  Statement,
                                       String                        Digest);


    /// <summary>
    /// A charging station as a verified proof gives it, as WWCP_POI.TS verifyChargingStation gives it: everything here is
    /// attested by the roaming network operator. The manufacturers, models, certificates and transparency software the
    /// proof gives above the station are read from its RoamingNetwork, as before.
    /// </summary>
    public sealed class VerifiedChargingStation
    {

        #region Properties

        /// <summary>
        /// The roaming network of the proof.
        /// </summary>
        public RoamingNetwork_Id                                          RoamingNetworkId       { get; }

        /// <summary>
        /// The charging station operator, as its skeleton: its own properties and keys, its child arrays as far as given.
        /// </summary>
        public JObject                                                    Operator               { get; }

        /// <summary>
        /// The charging pool of the station, as its skeleton.
        /// </summary>
        public JObject                                                    ChargingPool           { get; }

        /// <summary>
        /// The charging station as it is at runtime (SPECIFICATION.md, section 9.2), with its EVSEs.
        /// </summary>
        public JObject                                                    ChargingStation        { get; }

        /// <summary>
        /// The EVSEs of the station.
        /// </summary>
        public ImmutableArray<JObject>                                    EVSEs                  { get; }

        /// <summary>
        /// The charging tariffs each EVSE takes, by the identification of the EVSE, as the proof gives them.
        /// </summary>
        public IReadOnlyDictionary<String, ImmutableArray<JObject>>       Tariffs                { get; }

        /// <summary>
        /// The signing keys of the operator, e.g. of its price statements and of its charge transparency records.
        /// </summary>
        public ImmutableArray<POIPublicKey>                               OperatorSigningKeys    { get; }

        /// <summary>
        /// The energy meters of the station and of its EVSEs, with their public keys.
        /// </summary>
        public ImmutableArray<JObject>                                    EnergyMeters           { get; }

        /// <summary>
        /// The commit of the version of the proof.
        /// </summary>
        public RoamingNetworkCommit_Id?                                   CommitId               { get; }

        /// <summary>
        /// The revision of the version of the proof.
        /// </summary>
        public UInt64?                                                    Revision               { get; }

        /// <summary>
        /// What the proof attests, and by whom, e.g. until when the observation holds at most.
        /// </summary>
        public RoamingNetworkProofVerification                            Verification           { get; }

        /// <summary>
        /// Where the trust anchor names the network otherwise than the proof, and other things worth telling.
        /// </summary>
        public ImmutableArray<String>                                     Warnings               { get; }

        #endregion

        #region (private) Constructor(s)

        private VerifiedChargingStation(RoamingNetwork_Id                                     RoamingNetworkId,
                                        JObject                                               Operator,
                                        JObject                                               ChargingPool,
                                        JObject                                               ChargingStation,
                                        IEnumerable<JObject>                                  EVSEs,
                                        IReadOnlyDictionary<String, ImmutableArray<JObject>>  Tariffs,
                                        IEnumerable<POIPublicKey>                             OperatorSigningKeys,
                                        IEnumerable<JObject>                                  EnergyMeters,
                                        RoamingNetworkCommit_Id?                              CommitId,
                                        UInt64?                                               Revision,
                                        RoamingNetworkProofVerification                       Verification,
                                        IEnumerable<String>                                   Warnings)
        {

            this.RoamingNetworkId     = RoamingNetworkId;
            this.Operator             = Operator;
            this.ChargingPool         = ChargingPool;
            this.ChargingStation      = ChargingStation;
            this.EVSEs                = [.. EVSEs];
            this.Tariffs              = Tariffs;
            this.OperatorSigningKeys  = [.. OperatorSigningKeys];
            this.EnergyMeters         = [.. EnergyMeters];
            this.CommitId             = CommitId;
            this.Revision             = Revision;
            this.Verification         = Verification;
            this.Warnings             = [.. Warnings];

        }

        #endregion


        #region (static) TryVerify(TrustContext, Proof, out Station, out ErrorResponse, ChargingStationId = null, EVSEId = null)

        /// <summary>
        /// Verify the given proof of a charging station against the given trust context (SPECIFICATION.md, sections 8.4 to
        /// 8.6) and give what it attests: the operator, the pool, the station with its EVSEs as they are at runtime, the
        /// charging tariffs they take and the signing keys of the operator. With a charging station or an EVSE given, the
        /// proof must give that station, or the station of that EVSE.
        /// </summary>
        /// <param name="TrustContext">The trust context, evaluated of the decisions of the app.</param>
        /// <param name="Proof">The proof of the charging station.</param>
        /// <param name="Station">The verified charging station.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="ChargingStationId">The charging station the proof must give, if any.</param>
        /// <param name="EVSEId">The EVSE whose station the proof must give, if any.</param>
        public static Boolean TryVerify(RoamingNetworkTrustContext                         TrustContext,
                                        RoamingNetworkProof                                Proof,
                                        [NotNullWhen(true)]  out VerifiedChargingStation?  Station,
                                        [NotNullWhen(false)] out String?                   ErrorResponse,
                                        String?                                            ChargingStationId   = null,
                                        String?                                            EVSEId              = null)
        {

            Station = null;

            if (!Proof.TryVerify(TrustContext, out var verification, out ErrorResponse))
                return false;

            return TryGive(Proof, verification, Proof.TrustAnchorTextMismatch(TrustContext.TrustAnchor), ChargingStationId, EVSEId, out Station, out ErrorResponse);

        }

        #endregion

        #region (static) TryVerify(TrustAnchor,  Proof, out Station, out ErrorResponse, ChargingStationId = null, EVSEId = null, At = null)

        /// <summary>
        /// Verify the given proof of a charging station against the given trust anchor, as TryVerify(TrustContext, …) does
        /// against a trust context, at the given time, now by default.
        /// </summary>
        /// <param name="TrustAnchor">The trust anchor.</param>
        /// <param name="Proof">The proof of the charging station.</param>
        /// <param name="Station">The verified charging station.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="ChargingStationId">The charging station the proof must give, if any.</param>
        /// <param name="EVSEId">The EVSE whose station the proof must give, if any.</param>
        /// <param name="At">When it must be valid, now by default.</param>
        public static Boolean TryVerify(RoamingNetworkTrustAnchor                          TrustAnchor,
                                        RoamingNetworkProof                                Proof,
                                        [NotNullWhen(true)]  out VerifiedChargingStation?  Station,
                                        [NotNullWhen(false)] out String?                   ErrorResponse,
                                        String?                                            ChargingStationId   = null,
                                        String?                                            EVSEId              = null,
                                        DateTimeOffset?                                    At                  = null)
        {

            Station = null;

            if (!Proof.TryVerify(TrustAnchor, out var verification, out ErrorResponse, At))
                return false;

            return TryGive(Proof, verification, Proof.TrustAnchorTextMismatch(TrustAnchor), ChargingStationId, EVSEId, out Station, out ErrorResponse);

        }

        #endregion

        #region TryGetCurrentPrices(PriceStatements, out Prices, out ErrorResponse, At = null)

        /// <summary>
        /// Verify the given price statements (SPECIFICATION.md, section 12.4) against this station, as WWCP_POI.TS
        /// verifyPriceStatements does: each one of its pool, the station or one of its EVSEs, named as the roaming network
        /// names it, signed by a signing key of the operator that may sign price statements and was valid when it signed,
        /// naming only charging tariffs of that entity, the statements of one entity a chain. Give the statement of each entity
        /// valid at the given time, now by default, where there is one, its key valid then as well, by "type:id" of the entity.
        /// </summary>
        /// <param name="PriceStatements">The price statements.</param>
        /// <param name="Prices">The current price of each entity having one.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When they must be valid, now by default.</param>
        public Boolean TryGetCurrentPrices(IEnumerable<RoamingNetworkPriceStatement>                         PriceStatements,
                                           [NotNullWhen(true)]  out IReadOnlyDictionary<String, VerifiedPrice>?  Prices,
                                           [NotNullWhen(false)] out String?                                  ErrorResponse,
                                           DateTimeOffset?                                                   At   = null)
        {

            Prices = null;

            var at          = At ?? Timestamp.Now;
            var operatorId  = IdOf(Operator);

            // By the chains of their price statements: an entity in any spelling (12.1).
            var entities    = new Dictionary<String, (String Type, String Id, HashSet<String>? Tariffs)>(StringComparer.Ordinal) {
                                  [ ChainOf(nameof(POI.ChargingPool),    IdOf(ChargingPool))    ] = (nameof(POI.ChargingPool),    IdOf(ChargingPool),    null),
                                  [ ChainOf(nameof(POI.ChargingStation), IdOf(ChargingStation)) ] = (nameof(POI.ChargingStation), IdOf(ChargingStation), null)
                              };

            foreach (var evse in EVSEs)
                entities[ChainOf(nameof(EVSE), IdOf(evse))] = (nameof(EVSE),
                                                               IdOf(evse),
                                                               Tariffs.TryGetValue(IdOf(evse), out var tariffs)
                                                                   ? tariffs.Select(IdOf).ToHashSet(StringComparer.Ordinal)
                                                                   : []);

            var byEntity    = new Dictionary<String, List<RoamingNetworkPriceStatement>>(StringComparer.Ordinal);
            var signedWith  = new Dictionary<String, POIPublicKey>(StringComparer.Ordinal);

            foreach (var statement in PriceStatements)
            {

                var chain = ChainOf(statement.EntityType, statement.EntityId);

                if (!entities.TryGetValue(chain, out var entity))
                {
                    ErrorResponse = $"The price statement of the {statement.EntityType} '{statement.EntityId}' is none of the station, its pool or its EVSEs!";
                    return false;
                }

                // One spelling per entity, the roaming network's: another one would be a chain of its own to everybody comparing texts.
                if (entity.Id != statement.EntityId)
                {
                    ErrorResponse = $"The price statement names the {statement.EntityType} '{entity.Id}' as '{statement.EntityId}': it has to name it as the roaming network does!";
                    return false;
                }

                if (statement.RoamingNetworkId != RoamingNetworkId)
                {
                    ErrorResponse = $"The price statement is one of the roaming network '{statement.RoamingNetworkId}', not of '{RoamingNetworkId}'!";
                    return false;
                }

                if (statement.Signer.ToString() != operatorId || !statement.EntityId.StartsWith(operatorId + "*", StringComparison.Ordinal))
                {
                    ErrorResponse = $"The price statement of the {statement.EntityType} '{statement.EntityId}' is not signed by its operator '{operatorId}'!";
                    return false;
                }

                if (!statement.TryVerify(OperatorSigningKeys, out ErrorResponse))
                    return false;

                signedWith[statement.Digest] = OperatorSigningKeys.First(key => key.Id == statement.KeyId);

                // The prices of an EVSE apply to the charging tariffs the proof gives, those of the EVSE (12.4).
                if (entity.Tariffs is not null &&
                    statement.PriceStatus.Prices.FirstOrDefault(price => !entity.Tariffs.Contains(price.ChargingTariffId.ToString())) is ChargingTariffPrices foreign)
                {
                    ErrorResponse = $"The price statement of the EVSE '{statement.EntityId}' names the charging tariff '{foreign.ChargingTariffId}', which is not one of its own!";
                    return false;
                }

                // A statement given again, also as its ECDSA twin, is that statement, not a second one of its sequence.
                if (!byEntity.TryGetValue(chain, out var known))
                    byEntity[chain] = known = [];

                if (!known.Any(other => other.Digest == statement.Digest))
                    known.Add(statement);

            }

            var current = new Dictionary<String, VerifiedPrice>(StringComparer.Ordinal);

            foreach (var (chain, statements) in byEntity)
            {

                var ordered = statements.OrderBy(statement => statement.Sequence).ToArray();

                if (ordered.Select(statement => statement.Sequence).Distinct().Count() != ordered.Length)
                {
                    ErrorResponse = $"Two price statements of one sequence of '{chain}': a split view!";
                    return false;
                }

                // Of what is given, each statement directly after another one names its digest; gaps did not reach the caller.
                for (var i = 1; i < ordered.Length; i++)
                    if (ordered[i].Sequence == ordered[i - 1].Sequence + 1 && ordered[i].Previous != ordered[i - 1].Digest)
                    {
                        ErrorResponse = $"The price statement {ordered[i].Sequence} of the {ordered[i].EntityType} '{ordered[i].EntityId}' does not follow the one before!";
                        return false;
                    }

                // The latest statement valid now: a later one replaces it from its validFrom on. Valid no longer than its key.
                if (ordered.Reverse().FirstOrDefault(statement => statement.ValidFrom <= at) is RoamingNetworkPriceStatement valid &&
                    valid.IsValidAt(at) &&
                    signedWith[valid.Digest].IsValidAt(at))
                {
                    current[$"{valid.EntityType}:{valid.EntityId}"] = new VerifiedPrice(valid, valid.Digest);
                }

            }

            Prices         = current;
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region (private static) TryGive(Proof, Verification, TextMismatch, ChargingStationId, EVSEId, out Station, out ErrorResponse)

        private static Boolean TryGive(RoamingNetworkProof                                Proof,
                                       RoamingNetworkProofVerification                    Verification,
                                       String?                                            TextMismatch,
                                       String?                                            ChargingStationId,
                                       String?                                            EVSEId,
                                       [NotNullWhen(true)]  out VerifiedChargingStation?  Station,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            Station = null;

            var warnings  = new List<String>();

            // The station the proof gives completely: below a given operator and pool.
            var found     = (from      @operator in Proof.Given(RoamingNetwork.PropertyKeys.ChargingStationOperators)
                             from      pool      in Proof.GivenBelow(@operator, ChargingStationOperator.PropertyKeys.ChargingPools)
                             from      station   in Proof.GivenBelow(pool,      POI.ChargingPool.PropertyKeys.ChargingStations)
                             where     station[POI.ChargingStation.PropertyKeys.EVSEs] is JArray
                             select    (Operator: @operator, Pool: pool, Station: station)).ToArray();

            var matches   = found.Where(item => (ChargingStationId is null || IdOf(item.Station) == ChargingStationId) &&
                                                (EVSEId            is null || EVSEsOf(item.Station).Any(evse => IdOf(evse) == EVSEId))).
                                  ToArray();

            if (matches.Length != 1)
            {
                ErrorResponse = matches.Length == 0
                                    ? $"The proof gives no charging station{(ChargingStationId is not null ? $" '{ChargingStationId}'" : "")}{(EVSEId is not null ? $" of the EVSE '{EVSEId}'" : "")} completely!"
                                    : "The proof gives more than one charging station: name the one to show!";
                return false;
            }

            var (operatorJSON, poolJSON, stationJSON) = matches[0];

            var evses     = EVSEsOf(stationJSON).ToArray();
            var given     = operatorJSON[ChargingStationOperator.PropertyKeys.ChargingTariffs] is JArray tariffsJSON
                                ? tariffsJSON.OfType<JObject>().ToArray()
                                : [];
            var tariffs   = new Dictionary<String, ImmutableArray<JObject>>(StringComparer.Ordinal);

            foreach (var evse in evses)
            {

                var taken = new List<JObject>();

                foreach (var id in (evse[EVSE.PropertyKeys.ChargingTariffIds] as JArray ?? []).Where(id => id.Type == JTokenType.String).Select(id => id.Value<String>()!))
                {
                    if (given.FirstOrDefault(tariff => IdOf(tariff) == id) is JObject tariff)
                        taken.Add(tariff);
                    else
                        warnings.Add($"the charging tariff '{id}' of the EVSE '{IdOf(evse)}' is not given by the proof");
                }

                tariffs[IdOf(evse)] = [.. taken];

            }

            var energyMeters = (stationJSON[POI.ChargingStation.PropertyKeys.EnergyMeters] as JArray ?? []).OfType<JObject>().
                                   Concat(evses.Select(evse => evse[EVSE.PropertyKeys.EnergyMeter]).OfType<JObject>()).
                                   ToArray();

            if (!POIPartyKeys.TryParse(operatorJSON, out var signingKeys, out _, out var keysError))
            {
                ErrorResponse = $"The keys of the charging station operator '{IdOf(operatorJSON)}' in the proof are invalid: {keysError}";
                return false;
            }

            if (TextMismatch is not null)
                warnings.Add(TextMismatch);

            Station        = new VerifiedChargingStation(RoamingNetwork_Id.Parse(IdOf(Proof.RoamingNetwork)),
                                                         operatorJSON,
                                                         poolJSON,
                                                         stationJSON,
                                                         evses,
                                                         tariffs,
                                                         signingKeys,
                                                         energyMeters,
                                                         Proof.CommitId,
                                                         Proof.Revision,
                                                         Verification,
                                                         warnings);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (internal static) IdOf(JSON)

        /// <summary>
        /// The @id of the given entity, or an empty text.
        /// </summary>
        internal static String IdOf(JToken? JSON)

            => JSON?[RoamingNetwork.BasePropertyKeys.Id]?.Type == JTokenType.String
                   ? JSON[RoamingNetwork.BasePropertyKeys.Id]!.Value<String>()!
                   : "";

        #endregion

        #region (private static) EVSEsOf(Station)

        private static IEnumerable<JObject> EVSEsOf(JObject Station)

            => (Station[POI.ChargingStation.PropertyKeys.EVSEs] as JArray ?? []).OfType<JObject>();

        #endregion

        #region (private static) ChainOf(EntityType, EntityId)

        /// <summary>
        /// The chain of the price statements of an entity, in any spelling of it (WWCP_POI.TS priceStatementChainOf).
        /// </summary>
        private static String ChainOf(String  EntityType,
                                      String  EntityId)

            => $"{EntityType}:{EntityId.Trim().Replace("*", "").ToUpperInvariant()}";

        #endregion

    }

}
