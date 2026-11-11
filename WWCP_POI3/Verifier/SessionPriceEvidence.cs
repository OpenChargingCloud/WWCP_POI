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
using System.Globalization;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Unconditional gross rates of the simple profile, as their decimals are written.
    /// </summary>
    /// <param name="Currency">The currency of the tariff.</param>
    /// <param name="EnergyPerKWh">The price of a kWh, if any.</param>
    /// <param name="FlatFee">The flat fee, if any.</param>
    /// <param name="TimePerHour">The price of an hour, if any.</param>
    public sealed record GrossRates(String   Currency,
                                    String?  EnergyPerKWh,
                                    String?  FlatFee,
                                    String?  TimePerHour);


    /// <summary>
    /// The price selected for a session: the statement, its digest, the charging tariff, its currency, the signed elements
    /// and, of the simple profile, its gross rates.
    /// </summary>
    public sealed record SelectedSessionPrice(RoamingNetworkPriceStatement  Statement,
                                              String                        Digest,
                                              JObject                       Tariff,
                                              String                        Currency,
                                              ImmutableArray<JObject>       Elements,
                                              GrossRates?                   Rates);


    /// <summary>
    /// What a verification of session price evidence established (WWCP_POI.TS SessionPriceVerification), each apart:
    /// the signatures of the price statements, whether a price applies to the session and is bound by an assignment of the
    /// CPO, whether it is published in a checkpoint, and whether witnesses the verifier accepted retrieved it publicly before
    /// the signed start (PublicationBeforeStart: "witnessed-before-signed-start", "invalid" or "not-proven"). That the
    /// customer agreed and that it was the only or the latest offer are not proven.
    /// </summary>
    public sealed record SessionPriceVerification(VerifiedChargingStation        Station,
                                                  String                         Signatures,
                                                  String                         Applicability,
                                                  String                         Agreement,
                                                  String                         Publication,
                                                  Boolean                        ChainGaps,
                                                  DateTimeOffset                 CheckedAt,
                                                  ImmutableArray<String>         CheckpointIds,
                                                  ImmutableArray<String>         WitnessedBy,
                                                  ImmutableArray<String>         Diagnostics,
                                                  SelectedSessionPrice?          Selected,
                                                  String                         PublicationBeforeStart   = "not-proven",
                                                  PricePublicationVerification?  PublicationWitnesses     = null)
    {

        /// <summary>
        /// That the customer agreed: not proven.
        /// </summary>
        public String CustomerConsent         { get; } = "not-proven";

        /// <summary>
        /// That it was the only or the latest offer: not proven.
        /// </summary>
        public String LatestOrSoleOffer       { get; } = "not-proven";

    }


    /// <summary>
    /// The evidence of the price of a charging session (WWCP_POI.TS sessionPriceEvidence/v1, experimental): the proof of
    /// the station at the start, the price statements of the CPO, its assignment of one to the session, and where given the
    /// proofs of the statements in checkpoints and those checkpoints with their operators.
    /// </summary>
    public sealed partial class SessionPriceEvidence
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/sessionPriceEvidence/v1";

        [GeneratedRegex("^[0-9a-f]{64}$")]
        private static partial Regex SHA256Hex();

        #endregion

        #region Properties

        /// <summary>
        /// The EVSE of the session.
        /// </summary>
        public String                                     EVSEId                  { get; }

        /// <summary>
        /// The proof of its charging station.
        /// </summary>
        public JObject                                    ChargingStationProof    { get; }

        /// <summary>
        /// The price statements.
        /// </summary>
        public ImmutableArray<JToken>                     PriceStatements         { get; }

        /// <summary>
        /// The assignment of the CPO, if any.
        /// </summary>
        public JToken?                                    Agreement               { get; }

        /// <summary>
        /// The proofs of price statements in checkpoints.
        /// </summary>
        public ImmutableArray<JToken>                     PriceStatementProofs    { get; }

        /// <summary>
        /// The checkpoints, with their operators.
        /// </summary>
        public ImmutableArray<HistoricalCheckpointEntry>  Checkpoints             { get; }

        /// <summary>
        /// The receipts of witnesses that they retrieved the price publicly (PricePublicationReceipt), at most 64.
        /// </summary>
        public ImmutableArray<JToken>                     PublicationReceipts     { get; }

        /// <summary>
        /// Witnesses recommended, e.g. by the CPO, at most 32: recommendations only, which authorize nothing.
        /// </summary>
        public ImmutableArray<JToken>                     WitnessCandidates       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the evidence of the price of a charging session.
        /// </summary>
        public SessionPriceEvidence(String                                  EVSEId,
                                    JObject                                 ChargingStationProof,
                                    IEnumerable<JToken>                     PriceStatements,
                                    JToken?                                 Agreement              = null,
                                    IEnumerable<JToken>?                    PriceStatementProofs   = null,
                                    IEnumerable<HistoricalCheckpointEntry>? Checkpoints            = null,
                                    IEnumerable<JToken>?                    PublicationReceipts    = null,
                                    IEnumerable<JToken>?                    WitnessCandidates      = null)
        {

            this.EVSEId                = EVSEId;
            this.ChargingStationProof  = ChargingStationProof;
            this.PriceStatements       = [.. PriceStatements];
            this.Agreement             = Agreement;
            this.PriceStatementProofs  = [.. PriceStatementProofs ?? []];
            this.Checkpoints           = [.. Checkpoints          ?? []];
            this.PublicationReceipts   = [.. PublicationReceipts  ?? []];
            this.WitnessCandidates     = [.. WitnessCandidates    ?? []];

        }

        #endregion


        #region (static) TryParse(JSON, out Evidence, out ErrorResponse)

        /// <summary>
        /// Try to read the given session price evidence, naming nothing else.
        /// </summary>
        public static Boolean TryParse(JToken?                                         JSON,
                                       [NotNullWhen(true)]  out SessionPriceEvidence?  Evidence,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            Evidence = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "Session price evidence must be a JSON object!";
                return false;
            }

            if (!HistoricalMeterEvidence.Only(json, "Session price evidence", out ErrorResponse,
                                              "@context", "EVSEId", "chargingStationProof", "priceStatements", "agreement", "priceStatementProofs", "checkpoints",
                                              "publicationReceipts", "witnessCandidates"))
            {
                return false;
            }

            if (json["@context"]?.Type != JTokenType.String || json["@context"]!.Value<String>() != JSONLDContext)
            {
                ErrorResponse = "Unsupported session price evidence!";
                return false;
            }

            if (json["EVSEId"]?.Type != JTokenType.String || json["chargingStationProof"] is not JObject proof || json["priceStatements"] is not JArray statements)
            {
                ErrorResponse = "Session price evidence names its EVSEId, the proof of the priced station and the price statements!";
                return false;
            }

            if (json["priceStatementProofs"] is not null and not JArray)
            {
                ErrorResponse = "The price inclusion proofs must be a JSON array!";
                return false;
            }

            if (json["publicationReceipts"] is not null and not JArray ||
                json["witnessCandidates"]   is not null and not JArray)
            {
                ErrorResponse = "The publication receipts and the witness recommendations must be JSON arrays!";
                return false;
            }

            if ((json["publicationReceipts"] as JArray)?.Count > 64 || (json["witnessCandidates"] as JArray)?.Count > 32)
            {
                ErrorResponse = "Too many publication receipts or recommendations!";
                return false;
            }

            if (!HistoricalMeterEvidence.TryParseCheckpoints(json["checkpoints"], "Price checkpoints", out var checkpoints, out ErrorResponse))
                return false;

            Evidence = new SessionPriceEvidence(json["EVSEId"]!.Value<String>()!,
                                                proof,
                                                statements,
                                                json["agreement"],
                                                json["priceStatementProofs"] as JArray,
                                                checkpoints,
                                                json["publicationReceipts"]  as JArray,
                                                json["witnessCandidates"]    as JArray);

            return true;

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// The JSON of this evidence.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(new JProperty("@context",              JSONLDContext),
                                   new JProperty("EVSEId",                EVSEId),
                                   new JProperty("chargingStationProof",  ChargingStationProof),
                                   new JProperty("priceStatements",       new JArray(PriceStatements)));

            if (Agreement is not null)
                json.Add("agreement", Agreement);

            if (!PriceStatementProofs.IsEmpty)
                json.Add("priceStatementProofs", new JArray(PriceStatementProofs));

            if (!Checkpoints.IsEmpty)
                json.Add("checkpoints", new JArray(Checkpoints.Select(entry => new JObject(new JProperty("checkpoint", entry.Checkpoint),
                                                                                         new JProperty("operator",   entry.Operator)))));

            if (!WitnessCandidates.IsEmpty)
                json.Add("witnessCandidates", new JArray(WitnessCandidates));

            if (!PublicationReceipts.IsEmpty)
                json.Add("publicationReceipts", new JArray(PublicationReceipts));

            return json;

        }

        #endregion


        #region TryVerify(Current, EVSEId, MeterSerialNumber, ChargingStarted, StartDocumentSHA256, out Verification, out ErrorResponse, Policy = null, RelatedCheckpoints = null, PublicationPolicy = null)

        /// <summary>
        /// Verify this evidence for the given original session under the given local authorization (WWCP_POI.TS
        /// verifySessionPriceEvidence): the station attested at the start, with the operator of that time; the price
        /// statements of its CPO, their validity at the start and their chains; the assignment of the CPO to exactly this
        /// session; the one price selected; and apart from that its publication in checkpoints verified under the local
        /// authority, with the checkpoints an after link names and those related given, forks among them refused; and apart
        /// from all of that the receipts of witnesses of the given publication policy the verifier accepted, the keys and
        /// identities of the RNO and the CPO excluded (PricePublicationVerification). The decimals are kept as written.
        /// </summary>
        /// <param name="Current">The current local authorization.</param>
        /// <param name="EVSEId">The EVSE of the session.</param>
        /// <param name="MeterSerialNumber">The serial number of its meter.</param>
        /// <param name="ChargingStarted">When the session started, as its start document says.</param>
        /// <param name="StartDocumentSHA256">The SHA-256 of the exact start document, as lowercase hex.</param>
        /// <param name="Verification">What was established.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Policy">What the verifier requires.</param>
        /// <param name="RelatedCheckpoints">Other checkpoints known, e.g. of the station, to refuse forks.</param>
        /// <param name="PublicationPolicy">The witness publication policy the verifier accepted, if any.</param>
        public Boolean TryVerify(IRoamingNetworkTrustAuthority                       Current,
                                 String                                              EVSEId,
                                 String                                              MeterSerialNumber,
                                 DateTimeOffset                                      ChargingStarted,
                                 String                                              StartDocumentSHA256,
                                 [NotNullWhen(true)]  out SessionPriceVerification?  Verification,
                                 [NotNullWhen(false)] out String?                    ErrorResponse,
                                 HistoricalPolicy?                                   Policy               = null,
                                 IEnumerable<HistoricalCheckpointEntry>?             RelatedCheckpoints   = null,
                                 WitnessPublicationPolicy?                           PublicationPolicy    = null)
        {

            Verification = null;

            var policy = Policy ?? new HistoricalPolicy();

            if (ChargingStarted > Current.EvaluatedAt || !SHA256Hex().IsMatch(StartDocumentSHA256))
            {
                ErrorResponse = "Invalid original price context!";
                return false;
            }

            if (this.EVSEId != EVSEId)
            {
                ErrorResponse = "Price evidence belongs to another EVSE!";
                return false;
            }

            if (!RoamingNetworkProof.TryParse(ChargingStationProof, out var proof, out ErrorResponse) ||
                !HistoricalTrustContext.TryGetOperatorContext(Current, proof.RoamingNetwork[RoamingNetwork.PropertyKeys.Operator], ChargingStarted, out var context, out ErrorResponse) ||
                !VerifiedChargingStation.TryVerify(context, proof, out var station, out ErrorResponse, EVSEId: EVSEId))
            {
                return false;
            }

            var operatorId   = VerifiedChargingStation.IdOf(station.Operator);
            var diagnostics  = new List<String>();

            SessionPriceVerification Fail(String Signatures, String Applicability, String Message, String Agreement = "missing", Boolean ChainGaps = false)
                => new (station, Signatures, Applicability, Agreement, "missing", ChainGaps, Current.EvaluatedAt, [], [], [ Message ], null);

            ErrorResponse = null;

            if (PriceStatements.IsEmpty)
            {
                Verification = Fail("missing", "missing", "No signed price statement supplied");
                return true;
            }

            var statements = new List<RoamingNetworkPriceStatement>();
            var originals  = new Dictionary<String, JObject>(StringComparer.Ordinal);

            foreach (var statementJSON in PriceStatements)
            {

                if (statementJSON is not JObject json || !RoamingNetworkPriceStatement.TryParse(json, out var statement, out var error))
                {
                    Verification = Fail("invalid", "invalid", "A price statement is invalid");
                    return true;
                }

                if (statement.RoamingNetworkId  != station.RoamingNetworkId                             ||
                    statement.Signer.ToString() != operatorId                                           ||
                    !statement.EntityId.StartsWith(operatorId + "*", StringComparison.Ordinal)          ||
                    !statement.TryVerify(station.OperatorSigningKeys, out error))
                {
                    Verification = Fail("invalid", "invalid", error ?? $"The price statement of the {statement.EntityType} '{statement.EntityId}' is not signed by its operator '{operatorId}'!");
                    return true;
                }

                if (statement.SignedAt > Current.EvaluatedAt || policy.IsRevoked(statement.KeyId, statement.SignedAt))
                {
                    Verification = Fail("invalid", "invalid", "Future/revoked CPO price signature");
                    return true;
                }

                statements.Add(statement);
                originals.TryAdd(statement.Digest, json);

            }

            if (!station.TryGetCurrentPrices(statements, out var active, out var activeError, ChargingStarted))
            {
                Verification = Fail("valid", "invalid", activeError);
                return true;
            }

            var chainGaps = statements.Any(statement => statement.Sequence > 0 && !statements.Any(before => statement.Follows(before)));

            SessionPriceAgreement? agreement = null;

            if (Agreement is not null)
            {

                if (!SessionPriceAgreement.TryParse(Agreement, out agreement, out var agreementError) ||
                    !agreement.TryVerify(station, out agreementError))
                {
                    Verification = Fail("valid", "invalid", agreementError, "invalid", chainGaps);
                    return true;
                }

                if (agreement.EVSEId              != EVSEId              ||
                    agreement.MeterSerialNumber   != MeterSerialNumber   ||
                    agreement.ChargingStarted     != ChargingStarted     ||
                    agreement.StartDocumentSHA256 != StartDocumentSHA256 ||
                    agreement.SignedAt            >  Current.EvaluatedAt ||
                    policy.IsRevoked(agreement.KeyId, agreement.SignedAt))
                {
                    Verification = Fail("valid", "invalid", "Price assignment does not match this original session", "invalid", chainGaps);
                    return true;
                }

            }

            var tariffs     = station.Tariffs.TryGetValue(EVSEId, out var ofEVSE) ? ofEVSE : [];
            var candidates  = (from    price      in active.Values
                               from    tariffJSON in (price.Statement.PriceStatusJSON["prices"] as JArray ?? []).OfType<JObject>()
                               let     tariff     =  tariffs.FirstOrDefault(tariff => VerifiedChargingStation.IdOf(tariff) == tariffJSON["chargingTariffId"]?.Value<String>())
                               where   tariff is not null
                               select  (price.Statement, price.Digest, Tariff: tariff, Price: tariffJSON)).ToArray();

            var eligible    = agreement is null
                                  ? candidates
                                  : candidates.Where(candidate => candidate.Digest == agreement.PriceStatementDigest &&
                                                                  VerifiedChargingStation.IdOf(candidate.Tariff) == agreement.ChargingTariffId).ToArray();

            if (eligible.Length != 1)
            {
                Verification = Fail("valid",
                                    eligible.Length == 0 ? "missing" : "ambiguous",
                                    eligible.Length == 0 ? "No selected tariff/price is active at charging start" : "Competing price/tariff candidates need a signed selection",
                                    agreement is null ? "missing" : "invalid",
                                    chainGaps);
                return true;
            }

            var selected       = eligible[0];
            var currency       = selected.Tariff["currency"]?.Type == JTokenType.String ? selected.Tariff["currency"]!.Value<String>()! : "";
            var elements       = (selected.Price["elements"] as JArray ?? []).OfType<JObject>().ToImmutableArray();
            var rates          = GrossRatesOf(currency, elements);
            var applicability  = agreement is null ? "unbound" : "bound";

            // A session paid ad hoc of the simple profile, one through an e-mobility provider of the profile of contracts.
            if (agreement is not null &&
                (agreement.PricingProfile != SessionPriceAgreement.GrossPriceProfileOf(agreement.ChargingMode) ||
                 agreement.PriceBinding   != "fixed-at-start" ||
                 rates is null))
            {
                applicability = "unsupported";
                diagnostics.Add("EMP/contract terms, variable binding, unknown price profile or conditional elements are not established by this profile");
            }

            var publication    = "missing";
            var checkpointIds  = new List<String>();
            var witnessed      = new HashSet<String>(StringComparer.Ordinal);

            if (!PriceStatementProofs.IsEmpty || !Checkpoints.IsEmpty)
            {
                if (!TryVerifyPublication(Current, station, statements, selected.Digest, policy, RelatedCheckpoints, checkpointIds, witnessed, diagnostics, out publication, out var publicationError))
                {
                    publication = "invalid";
                    diagnostics.Add(publicationError);
                }
            }

            if (!TryVerifyWitnesses(Current, proof, station, originals[selected.Digest], selected.Tariff, ChargingStarted, publication, checkpointIds, PublicationPolicy, out var witnesses, out ErrorResponse))
                return false;

            if (policy.RequirePublication && publication != "valid")
                diagnostics.Add("Required price publication evidence is incomplete");

            Verification = new SessionPriceVerification(station,
                                                        "valid",
                                                        applicability,
                                                        agreement is null ? "missing" : "valid",
                                                        publication,
                                                        chainGaps,
                                                        Current.EvaluatedAt,
                                                        [.. checkpointIds],
                                                        [.. witnessed],
                                                        [.. diagnostics],
                                                        new SelectedSessionPrice(selected.Statement,
                                                                                 selected.Digest,
                                                                                 selected.Tariff,
                                                                                 currency,
                                                                                 elements,
                                                                                 agreement?.PricingProfile is SessionPriceAgreement.SimpleGrossPriceProfile or SessionPriceAgreement.ContractGrossPriceProfile ? rates : null),
                                                        witnesses.Status,
                                                        witnesses);

            return true;

        }

        #endregion


        #region (private) TryVerifyWitnesses(…)

        /// <summary>
        /// Verify the receipts of witnesses of the selected price (PricePublicationVerification): the checkpoints proven to
        /// name it, where its publication is valid, and the keys and identities of the CPO and of every RNO the evidence gives
        /// excluded from witnessing.
        /// </summary>
        private Boolean TryVerifyWitnesses(IRoamingNetworkTrustAuthority                           Current,
                                           RoamingNetworkProof                                     Proof,
                                           VerifiedChargingStation                                 Station,
                                           JObject                                                 PriceStatement,
                                           JObject                                                 Tariff,
                                           DateTimeOffset                                          ChargingStarted,
                                           String                                                  Publication,
                                           IReadOnlyCollection<String>                             CheckpointIds,
                                           WitnessPublicationPolicy?                               PublicationPolicy,
                                           [NotNullWhen(true)]  out PricePublicationVerification?  Verification,
                                           [NotNullWhen(false)] out String?                        ErrorResponse)
        {

            Verification = null;

            var authorities  = new List<JObject> { Proof.RoamingNetwork[RoamingNetwork.PropertyKeys.Operator] as JObject ?? [] };
            authorities.AddRange(Checkpoints.Select(entry => entry.Operator));

            if (!POIPartyKeys.TryParse(Station.Operator, out _, out var cpoIdentityKeys, out ErrorResponse))
                return false;

            var partyKeys    = new List<POIPublicKey>([ .. Station.OperatorSigningKeys, .. cpoIdentityKeys ]);

            foreach (var authority in authorities)
            {

                if (!POIPartyKeys.TryParse(authority, out _, out var identityKeys, out ErrorResponse))
                    return false;

                partyKeys.AddRange(identityKeys);

                foreach (var certificate in (authority[RoamingNetworkOperator.PropertyKeys.SigningKeyCertificates] as JArray ?? []).OfType<JObject>())
                    if (certificate["signingKey"] is JObject signingKeyJSON)
                    {
                        if (!POIPublicKey.TryParse(signingKeyJSON, out var signingKey, out ErrorResponse))
                            return false;
                        partyKeys.Add(signingKey);
                    }

            }

            var checkpoints  = new List<RoamingNetworkCheckpoint>();

            if (Publication == "valid")
                foreach (var entry in Checkpoints)
                    if (RoamingNetworkCheckpoint.TryParse(entry.Checkpoint, out var checkpoint, out _) && CheckpointIds.Contains(checkpoint.Id.ToString()))
                        checkpoints.Add(checkpoint);

            return PricePublicationVerification.TryVerify(PublicationReceipts,
                                                          PublicationPolicy,
                                                          Station.RoamingNetworkId,
                                                          EVSEId,
                                                          PriceStatement,
                                                          Tariff,
                                                          checkpoints,
                                                          ChargingStarted,
                                                          Current.EvaluatedAt,
                                                          out Verification,
                                                          out ErrorResponse,
                                                          partyKeys.Select(key => Convert.ToHexStringLower(key.Key.Thumbprint())),
                                                          [ VerifiedChargingStation.IdOf(Station.Operator), .. authorities.Select(authority => VerifiedChargingStation.IdOf(authority)) ]);

        }

        #endregion

        #region (private) TryVerifyPublication(…)

        private Boolean TryVerifyPublication(IRoamingNetworkTrustAuthority                 Current,
                                             VerifiedChargingStation                       Station,
                                             IReadOnlyList<RoamingNetworkPriceStatement>   Statements,
                                             String                                        SelectedDigest,
                                             HistoricalPolicy                              Policy,
                                             IEnumerable<HistoricalCheckpointEntry>?       RelatedCheckpoints,
                                             List<String>                                  CheckpointIds,
                                             HashSet<String>                               Witnessed,
                                             List<String>                                  Diagnostics,
                                             out String                                    Publication,
                                             [NotNullWhen(false)] out String?              ErrorResponse)
        {

            Publication = "missing";

            var entries = new List<AuthorizedCheckpoint>();

            foreach (var entry in Checkpoints)
            {
                if (!RoamingNetworkCheckpoint.TryParse(entry.Checkpoint, out var checkpoint, out ErrorResponse) ||
                    !AuthorizedCheckpoint.TryVerify(Current, checkpoint, entry.Operator, Policy, out var authorized, out ErrorResponse))
                {
                    return false;
                }
                entries.Add(authorized);
            }

            var related = new List<AuthorizedCheckpoint>();

            foreach (var entry in RelatedCheckpoints ?? [])
            {
                if (!RoamingNetworkCheckpoint.TryParse(entry.Checkpoint, out var checkpoint, out ErrorResponse) ||
                    !AuthorizedCheckpoint.TryVerify(Current, checkpoint, entry.Operator, Policy, out var authorized, out ErrorResponse))
                {
                    return false;
                }
                related.Add(authorized);
            }

            var heads = new Dictionary<String, RoamingNetworkCheckpoint_Id>(StringComparer.Ordinal);

            foreach (var checkpoint in entries.Concat(related).Select(authorized => authorized.Checkpoint))
            {

                var coordinate = $"{checkpoint.Epoch}:{checkpoint.Sequence}";

                if (heads.TryGetValue(coordinate, out var head) && head != checkpoint.Id)
                {
                    ErrorResponse = "Conflicting station/price checkpoint heads!";
                    return false;
                }

                heads[coordinate] = checkpoint.Id;

            }

            var byId          = entries.GroupBy(authorized => authorized.Checkpoint.Id).ToDictionary(group => group.Key, group => group.First());
            var selectedProof = false;
            var incomplete    = false;

            foreach (var proofJSON in PriceStatementProofs)
            {

                if (proofJSON is not JObject json)
                {
                    ErrorResponse = "A price inclusion proof must be a JSON object!";
                    return false;
                }

                if (!RoamingNetworkPriceStatementProof.TryParse(json, out var proof, out ErrorResponse))
                    return false;

                if (!byId.TryGetValue(proof.CheckpointId, out var checkedCheckpoint))
                {
                    ErrorResponse = "Missing price checkpoint!";
                    return false;
                }

                if (proof.PriceStatement.Signer.ToString() != VerifiedChargingStation.IdOf(Station.Operator) ||
                    !proof.PriceStatement.TryVerify(Station.OperatorSigningKeys, out ErrorResponse))
                {
                    ErrorResponse ??= "The price statement of a price proof is not signed by the operator of the station!";
                    return false;
                }

                if (!Statements.Any(statement => statement.Digest == proof.PriceStatement.Digest))
                {
                    ErrorResponse = "Price proof belongs to an unsupplied statement!";
                    return false;
                }

                if (!proof.Verify(checkedCheckpoint.Checkpoint, out ErrorResponse))
                    return false;

                if (proof.PriceStatement.Digest != SelectedDigest)
                    continue;

                selectedProof = true;
                CheckpointIds.Add(checkedCheckpoint.Checkpoint.Id.ToString());

                foreach (var witness in checkedCheckpoint.WitnessedBy)
                    Witnessed.Add(witness);

                if (proof.PriceStatement.After is RoamingNetworkCheckpoint_Id afterId)
                {

                    if (!byId.TryGetValue(afterId, out var after))
                    {
                        incomplete = true;
                        Diagnostics.Add("Missing checkpoint named by after-link");
                        continue;
                    }

                    if (after.Checkpoint.Epoch != checkedCheckpoint.Checkpoint.Epoch || after.Checkpoint.Sequence >= checkedCheckpoint.Checkpoint.Sequence)
                    {
                        ErrorResponse = "Invalid checkpoint temporal order!";
                        return false;
                    }

                    if (!TryVerifyCheckpointLine(entries.Select(authorized => authorized.Checkpoint).
                                                         Where(checkpoint => checkpoint.Epoch    == after.Checkpoint.Epoch     &&
                                                                             checkpoint.Sequence >= after.Checkpoint.Sequence  &&
                                                                             checkpoint.Sequence <= checkedCheckpoint.Checkpoint.Sequence).
                                                         OrderBy(checkpoint => checkpoint.Sequence).
                                                         ToArray(),
                                                 out ErrorResponse))
                    {
                        return false;
                    }

                }

            }

            Publication    = incomplete ? "incomplete" : selectedProof ? "valid" : "missing";
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (internal static) TryVerifyCheckpointLine(Checkpoints, out ErrorResponse)

        /// <summary>
        /// Whether the given checkpoints, in their order, follow one another (WWCP_POI.TS verifyCheckpointLine): of one
        /// roaming network and epoch, each one more in sequence, naming the one before, of a later revision.
        /// </summary>
        internal static Boolean TryVerifyCheckpointLine(IReadOnlyList<RoamingNetworkCheckpoint>  Checkpoints,
                                                        [NotNullWhen(false)] out String?          ErrorResponse)
        {

            for (var i = 1; i < Checkpoints.Count; i++)
            {

                var before     = Checkpoints[i - 1];
                var checkpoint = Checkpoints[i];

                if (checkpoint.RoamingNetworkId != before.RoamingNetworkId || checkpoint.Epoch != before.Epoch)
                {
                    ErrorResponse = $"The checkpoint '{checkpoint.Id}' is one of another roaming network or epoch!";
                    return false;
                }

                if (checkpoint.Sequence != before.Sequence + 1 || checkpoint.PreviousId != before.Id)
                {
                    ErrorResponse = $"The checkpoint '{checkpoint.Id}' does not follow the checkpoint '{before.Id}'!";
                    return false;
                }

                if (checkpoint.Revision <= before.Revision)
                {
                    ErrorResponse = $"The checkpoint '{checkpoint.Id}' is of an earlier revision than the one before!";
                    return false;
                }

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) GrossRatesOf(Currency, Elements)

        /// <summary>
        /// The gross rates of the simple profile: one element, without restrictions, of energy, flat or time components
        /// each once, with a price not negative, energy and time with a step size of at least one, a flat fee without any,
        /// as a FLAT price component has none; none otherwise.
        /// </summary>
        private static GrossRates? GrossRatesOf(String                    Currency,
                                                IReadOnlyList<JObject>    Elements)
        {

            if (Elements.Count != 1)
                return null;

            var element = Elements[0];

            if (element.Properties().Any(property => property.Name is not "priceComponents" and not "restrictions"))
                return null;

            if (element["restrictions"] is JToken restrictions && (restrictions is not JObject restrictionsJSON || restrictionsJSON.HasValues))
                return null;

            if (element["priceComponents"] is not JArray components || components.Count == 0)
                return null;

            String? energy = null, flat = null, time = null;

            foreach (var item in components)
            {

                if (item is not JObject component ||
                    component.Properties().Any(property => property.Name is not "type" and not "price" and not "stepSize"))
                {
                    return null;
                }

                if (component["price"] is not JValue { Type: JTokenType.Integer or JTokenType.Float } price ||
                    price.AsDecimal() < 0)
                {
                    return null;
                }

                var type = component["type"]?.Type == JTokenType.String ? component["type"]!.Value<String>() : null;

                // A flat fee has no step size, as a FLAT price component has none; energy and time one of at least one.
                if (type == "FLAT"
                        ? component.ContainsKey("stepSize")
                        : component["stepSize"] is not JValue { Type: JTokenType.Integer } stepSize || stepSize.Value<Int64>() < 1)
                {
                    return null;
                }

                var text = Convert.ToString(price.Value, CultureInfo.InvariantCulture);

                switch (type)
                {
                    case "ENERGY" when energy is null:  energy = text; break;
                    case "FLAT"   when flat   is null:  flat   = text; break;
                    case "TIME"   when time   is null:  time   = text; break;
                    default:                            return null;
                }

            }

            return new GrossRates(Currency, energy, flat, time);

        }

        #endregion

    }

}
