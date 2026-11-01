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
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A witness of the publication of prices a verifier accepted (WWCP_POI.TS PublicationWitness): an organization approved
    /// on its own, the roaming networks it witnesses, its keys, its validity, the error of its clock the verifier accepts,
    /// and the endpoints of its receipts. Several keys or aliases of one organization never add votes.
    /// </summary>
    public sealed record PublicationWitness(String                        Id,
                                            String                        Organization,
                                            ImmutableArray<String>        Networks,
                                            ImmutableArray<POIPublicKey>  Keys,
                                            DateTimeOffset                NotBefore,
                                            DateTimeOffset                NotAfter,
                                            TimeSpan                      ClockError,
                                            ImmutableArray<String>        Endpoints,
                                            JObject                       JSON);


    /// <summary>
    /// The policy of the witnesses of the publication of prices a verifier accepted (WWCP_POI.TS WitnessPublicationPolicy,
    /// witnessPublicationPolicy/v1, experimental): how many organizations must witness a publication, and which. Reading a
    /// policy, e.g. one recommended by an evidence, authorizes nothing: only the verifier gives the one it accepted.
    /// </summary>
    public sealed partial class WitnessPublicationPolicy
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/witnessPublicationPolicy/v1";

        /// <summary>
        /// The role of a witness of the publication of prices and of their time.
        /// </summary>
        public const String WitnessRole   = "price-publication-and-time";

        [GeneratedRegex(@"^\d+(\.\d+){3}\z")]
        private static partial Regex IPv4();

        #endregion

        #region Properties

        /// <summary>
        /// How many organizations must witness a publication.
        /// </summary>
        public Byte                                RequiredOrganizations    { get; }

        /// <summary>
        /// The witnesses.
        /// </summary>
        public ImmutableArray<PublicationWitness>  Witnesses                { get; }

        /// <summary>
        /// The JSON as read.
        /// </summary>
        public JObject                             JSON                     { get; }

        #endregion

        #region (private) Constructor(s)

        private WitnessPublicationPolicy(Byte                             RequiredOrganizations,
                                         IEnumerable<PublicationWitness>  Witnesses,
                                         JObject                          JSON)
        {

            this.RequiredOrganizations  = RequiredOrganizations;
            this.Witnesses              = [.. Witnesses];
            this.JSON                   = JSON;

        }

        #endregion


        #region (static) TryParse(JSON, out Policy, out ErrorResponse)

        /// <summary>
        /// Try to read the given witness publication policy: from 1 to 16 organizations required, at most 32 witnesses and
        /// 1 MiB, each in the role of a witness of the publication of prices, of explicit networks, with 1 to 8 keys that may
        /// sign witness publications, a key of one organization only, a validity, an accepted clock error of at most five
        /// minutes and at most 4 public HTTPS endpoints.
        /// </summary>
        /// <param name="JSON">The JSON.</param>
        /// <param name="Policy">The policy.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JToken?                                             JSON,
                                       [NotNullWhen(true)]  out WitnessPublicationPolicy?  Policy,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)
        {

            Policy = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "A witness publication policy must be a JSON object!";
                return false;
            }

            if (!HistoricalMeterEvidence.Only(json, "A witness publication policy", out ErrorResponse, "@context", "requiredOrganizations", "witnesses"))
                return false;

            if (json["@context"]?.Type != JTokenType.String || json["@context"]!.Value<String>() != JSONLDContext)
            {
                ErrorResponse = "Unsupported witness publication policy!";
                return false;
            }

            if (json["requiredOrganizations"] is not JValue { Type: JTokenType.Integer } required || required.Value<Int64>() is < 1 or > 16)
            {
                ErrorResponse = "The required organizations of a witness publication policy are an integer from 1 to 16!";
                return false;
            }

            if (json["witnesses"] is not JArray entries || entries.Count > 32 || CanonicalJSON.ToUTF8Bytes(json).Length > 1024 * 1024)
            {
                ErrorResponse = "Witness policy exceeds its capacity!";
                return false;
            }

            var witnesses  = new List<PublicationWitness>();
            var ids        = new HashSet<String>(StringComparer.Ordinal);
            var material   = new Dictionary<String, String>(StringComparer.Ordinal);

            foreach (var entry in entries)
            {

                if (entry is not JObject item)
                {
                    ErrorResponse = "A publication witness must be a JSON object!";
                    return false;
                }

                if (!HistoricalMeterEvidence.Only(item, "A publication witness", out ErrorResponse,
                                                  "id", "organization", "role", "networks", "keys", "notBefore", "notAfter", "clockErrorMs", "endpoints"))
                {
                    return false;
                }

                if (item["role"]?.Type != JTokenType.String || item["role"]!.Value<String>() != WitnessRole)
                {
                    ErrorResponse = "A witness needs the explicit publication and time role!";
                    return false;
                }

                if (!TryGetIdentifier(item, "id",           out var id,           out ErrorResponse) ||
                    !TryGetIdentifier(item, "organization", out var organization, out ErrorResponse))
                {
                    return false;
                }

                if (!ids.Add(id))
                {
                    ErrorResponse = "Duplicate witness identity!";
                    return false;
                }

                if (item["networks"] is not JArray networks                                                           ||
                    networks.Count is 0 or > 32                                                                       ||
                    networks.Any(network => network.Type != JTokenType.String || network.Value<String>() is "" or "*") ||
                    networks.Select(network => network.Value<String>()).Distinct().Count() != networks.Count)
                {
                    ErrorResponse = "Explicit witness network scopes required!";
                    return false;
                }

                if (item["keys"] is not JArray keysJSON || keysJSON.Count is 0 or > 8)
                {
                    ErrorResponse = "Invalid witness key list!";
                    return false;
                }

                var keys = new List<POIPublicKey>();

                foreach (var keyJSON in keysJSON)
                {

                    if (keyJSON is not JObject keyObject || !POIPublicKey.TryParse(keyObject, out var key, out ErrorResponse))
                    {
                        ErrorResponse ??= "Invalid witness key list!";
                        return false;
                    }

                    if (!key.MaySign(POIKeyUsages.WitnessPublications))
                    {
                        ErrorResponse = "Witness key usage does not permit publication receipts!";
                        return false;
                    }

                    var fingerprint = Convert.ToHexStringLower(key.Key.Thumbprint());

                    if (material.TryGetValue(fingerprint, out var before) && before != organization)
                    {
                        ErrorResponse = "The same witness key cannot represent different organizations!";
                        return false;
                    }

                    material[fingerprint] = organization;
                    keys.Add(key);

                }

                if (keys.Select(key => key.Id).Distinct().Count() != keys.Count)
                {
                    ErrorResponse = "Invalid witness key list!";
                    return false;
                }

                if (!SessionPriceAgreement.TryTimestamp(item, "notBefore", out var notBefore, out ErrorResponse) ||
                    !SessionPriceAgreement.TryTimestamp(item, "notAfter",  out var notAfter,  out ErrorResponse))
                {
                    return false;
                }

                if (notAfter <= notBefore)
                {
                    ErrorResponse = "Invalid witness validity interval!";
                    return false;
                }

                if (item["clockErrorMs"] is not JValue { Type: JTokenType.Integer } clockError || clockError.Value<Int64>() is < 0 or > 300000)
                {
                    ErrorResponse = "The accepted clock error of a witness is an integer of milliseconds from 0 to 300000!";
                    return false;
                }

                if (item["endpoints"] is not JArray endpointsJSON || endpointsJSON.Count > 4 || endpointsJSON.Any(endpoint => endpoint.Type != JTokenType.String))
                {
                    ErrorResponse = "Invalid witness receipt endpoints!";
                    return false;
                }

                var endpoints = new List<String>();

                foreach (var endpoint in endpointsJSON)
                {
                    if (!TryGetPublicURL(endpoint.Value<String>()!, out var url, out ErrorResponse))
                        return false;
                    endpoints.Add(url);
                }

                witnesses.Add(new PublicationWitness(id,
                                                     organization,
                                                     [.. networks.Select(network => network.Value<String>()!)],
                                                     [.. keys],
                                                     notBefore,
                                                     notAfter,
                                                     TimeSpan.FromMilliseconds(clockError.Value<Int64>()),
                                                     [.. endpoints],
                                                     item));

            }

            Policy = new WitnessPublicationPolicy((Byte) required.Value<Int64>(), witnesses, json);
            return true;

        }

        #endregion


        #region (static) TryGetPublicURL(Text, out URL, out ErrorResponse)

        /// <summary>
        /// Whether the given text is a public HTTPS URL of a publication (WWCP_POI.TS publicPublicationURL): no credentials,
        /// no fragment, at most 2048 characters, a public DNS host name, no IP address, no localhost and no .local. Syntax
        /// only: a caller applies its own transport policy before fetching.
        /// </summary>
        /// <param name="Text">The URL.</param>
        /// <param name="URL">The URL, normalized.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryGetPublicURL(String                            Text,
                                              [NotNullWhen(true)]  out String?  URL,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            URL = null;

            if (!Uri.TryCreate(Text, UriKind.Absolute, out var uri) ||
                uri.Scheme   != Uri.UriSchemeHttps ||
                uri.UserInfo != ""                 ||
                uri.Fragment != ""                 ||
                Text.Contains('#')                 ||
                Text.Length  >  2048)
            {
                ErrorResponse = "Publication sources require public HTTPS without credentials or fragments!";
                return false;
            }

            var host = uri.Host.ToLowerInvariant().Trim('[', ']');

            if (host == "localhost" || host.EndsWith(".localhost", StringComparison.Ordinal) || host.EndsWith(".local", StringComparison.Ordinal) ||
                host == "::1" || host.Contains(':') || IPv4().IsMatch(host) || IPAddress.TryParse(host, out _) || !host.Contains('.'))
            {
                ErrorResponse = "Publication sources require a public DNS hostname!";
                return false;
            }

            URL            = uri.AbsoluteUri;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (internal static) TryGetIdentifier(JSON, Property, out Identifier, out ErrorResponse)

        /// <summary>
        /// An identifier of a publication: a text of 1 to 512 characters, without spaces at its edges or control characters.
        /// </summary>
        internal static Boolean TryGetIdentifier(JObject                           JSON,
                                                 String                            Property,
                                                 out String                        Identifier,
                                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Identifier = JSON[Property]?.Type == JTokenType.String ? JSON[Property]!.Value<String>()! : "";

            if (Identifier.Length is 0 or > 512 || Identifier.Trim() != Identifier || Identifier.Any(character => character < ' '))
            {
                ErrorResponse = "Invalid publication identity!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }


    /// <summary>
    /// The receipt of an independent witness that it retrieved a price publicly (WWCP_POI.TS PricePublicationReceipt,
    /// pricePublicationReceipt/v1, experimental): of the roaming network, the EVSE, the price statement, its original
    /// signature and the tariff selected, the checkpoint naming it, the public source and when it observed it, signed within
    /// a minute. No checkpoint witness format and no RFC 3161 token.
    /// </summary>
    public sealed partial class PricePublicationReceipt
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext  = "https://open.charging.cloud/contexts/wwcp/pricePublicationReceipt/v1";

        /// <summary>
        /// The assertion of a receipt: a public retrieval of the price.
        /// </summary>
        public const String Assertion      = "public-price-retrieval";

        private static readonly String[] Properties = [ "@context", "assertion", "roamingNetworkId", "EVSEId", "priceStatementDigest", "priceStatementSHA256", "tariffSHA256",
                                                        "checkpointId", "witnessId", "publicSourceURL", "observedAt", "signedAt", "algorithm", "keyId", "value" ];

        [GeneratedRegex("^[0-9a-f]{64}\\z")]
        private static partial Regex SHA256Hex();

        [GeneratedRegex("^(?:[0-9a-f]{64}|[0-9a-f]{96}|[0-9a-f]{128})\\z")]
        private static partial Regex CheckpointHex();

        #endregion

        #region Properties

        /// <summary>
        /// The JSON as signed.
        /// </summary>
        public JObject         JSON          { get; }

        /// <summary>
        /// When the witness observed the price.
        /// </summary>
        public DateTimeOffset  ObservedAt    { get; }

        /// <summary>
        /// When the witness signed the receipt.
        /// </summary>
        public DateTimeOffset  SignedAt      { get; }

        /// <summary>
        /// The witness.
        /// </summary>
        public String          WitnessId     => JSON["witnessId"]!.Value<String>()!;

        #endregion

        #region (private) Constructor(s)

        private PricePublicationReceipt(JObject         JSON,
                                        DateTimeOffset  ObservedAt,
                                        DateTimeOffset  SignedAt)
        {
            this.JSON        = JSON;
            this.ObservedAt  = ObservedAt;
            this.SignedAt    = SignedAt;
        }

        #endregion


        #region (static) OfferHashesOf(PriceStatement, Tariff)

        /// <summary>
        /// The hashes a receipt binds the offer with (WWCP_POI.TS pricePublicationOfferHashes): the SHA-256 of the canonical
        /// JSON of the complete original price statement, its signature included, and of the complete tariff selected, its
        /// currency included; as lowercase hex. The digest of a statement leaves its signature out: a draft has it as well.
        /// </summary>
        /// <param name="PriceStatement">The price statement as the CPO signed it, as JSON.</param>
        /// <param name="Tariff">The tariff selected, as the proof gives it.</param>
        public static (String PriceStatementSHA256, String TariffSHA256) OfferHashesOf(JObject  PriceStatement,
                                                                                         JObject  Tariff)

            => (Convert.ToHexStringLower(SHA256.HashData(CanonicalJSON.ToUTF8Bytes(PriceStatement))),
                Convert.ToHexStringLower(SHA256.HashData(CanonicalJSON.ToUTF8Bytes(Tariff))));

        #endregion

        #region (static) Create(PrivateKey, KeyId, Algorithm, RoamingNetworkId, EVSEId, PriceStatement, Tariff, CheckpointId, WitnessId, PublicSourceURL, ObservedAt, SignedAt = null)

        /// <summary>
        /// Create and sign the receipt of a witness that it retrieved the given price publicly, as a witness service does.
        /// </summary>
        /// <param name="PrivateKey">The private key of the witness, one that may sign witness publications.</param>
        /// <param name="KeyId">The @id of the key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="EVSEId">The EVSE.</param>
        /// <param name="PriceStatement">The price statement as the CPO signed it, as JSON.</param>
        /// <param name="Tariff">The tariff selected, as the proof gives it.</param>
        /// <param name="CheckpointId">The checkpoint naming the price statement.</param>
        /// <param name="WitnessId">The witness.</param>
        /// <param name="PublicSourceURL">The public source it retrieved the price from.</param>
        /// <param name="ObservedAt">When it observed the price.</param>
        /// <param name="SignedAt">When it signs, at most a minute later; when it observed by default.</param>
        public static PricePublicationReceipt Create(AsymmetricKeyParameter       PrivateKey,
                                                     String                       KeyId,
                                                     COSEAlgorithm                Algorithm,
                                                     RoamingNetwork_Id            RoamingNetworkId,
                                                     String                       EVSEId,
                                                     JObject                      PriceStatement,
                                                     JObject                      Tariff,
                                                     RoamingNetworkCheckpoint_Id  CheckpointId,
                                                     String                       WitnessId,
                                                     String                       PublicSourceURL,
                                                     DateTimeOffset               ObservedAt,
                                                     DateTimeOffset?              SignedAt   = null)
        {

            if (!RoamingNetworkPriceStatement.TryParse(PriceStatement, out var statement, out var error))
                throw new ArgumentException(error, nameof(PriceStatement));

            var (statementSHA256, tariffSHA256) = OfferHashesOf(PriceStatement, Tariff);

            var json = new JObject(new JProperty("@context",              JSONLDContext),
                                   new JProperty("assertion",             Assertion),
                                   new JProperty("roamingNetworkId",      RoamingNetworkId.ToString()),
                                   new JProperty("EVSEId",                EVSEId),
                                   new JProperty("priceStatementDigest",  statement.Digest),
                                   new JProperty("priceStatementSHA256",  statementSHA256),
                                   new JProperty("tariffSHA256",          tariffSHA256),
                                   new JProperty("checkpointId",          CheckpointId.ToString()),
                                   new JProperty("witnessId",             WitnessId),
                                   new JProperty("publicSourceURL",       PublicSourceURL),
                                   new JProperty("observedAt",            ObservedAt.ToISO8601()),
                                   new JProperty("signedAt",              (SignedAt ?? ObservedAt).ToISO8601()),
                                   new JProperty("algorithm",             Algorithm.Name),
                                   new JProperty("keyId",                 KeyId));

            json.Add("value", Convert.ToBase64String(Algorithm.Sign(CanonicalJSON.ToUTF8Bytes(json), PrivateKey)));

            return TryParse(json, out var receipt, out error)
                       ? receipt
                       : throw new ArgumentException(error);

        }

        #endregion

        #region (static) TryParse(JSON, out Receipt, out ErrorResponse)

        /// <summary>
        /// Try to read the given receipt, naming nothing else, signed within a minute after the observation.
        /// </summary>
        public static Boolean TryParse(JToken?                                            JSON,
                                       [NotNullWhen(true)]  out PricePublicationReceipt?  Receipt,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            Receipt = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "A price publication receipt must be a JSON object!";
                return false;
            }

            if (!HistoricalMeterEvidence.Only(json, "A price publication receipt", out ErrorResponse, Properties))
                return false;

            if (json["@context"]?.Type != JTokenType.String || json["@context"]!.Value<String>() != JSONLDContext ||
                json["assertion"]?.Type != JTokenType.String || json["assertion"]!.Value<String>() != Assertion)
            {
                ErrorResponse = "Unsupported publication receipt assertion!";
                return false;
            }

            foreach (var property in new[] { "roamingNetworkId", "EVSEId", "witnessId", "algorithm", "keyId" })
                if (!WitnessPublicationPolicy.TryGetIdentifier(json, property, out _, out ErrorResponse))
                    return false;

            if (json["value"]?.Type != JTokenType.String || json["value"]!.Value<String>()!.Length is 0 or > 131072)
            {
                ErrorResponse = "Invalid publication signature size!";
                return false;
            }

            if (json["priceStatementDigest"]?.Type != JTokenType.String || !SHA256Hex().    IsMatch(json["priceStatementDigest"]!.Value<String>()!) ||
                json["checkpointId"]?.        Type != JTokenType.String || !CheckpointHex().IsMatch(json["checkpointId"]!.        Value<String>()!))
            {
                ErrorResponse = "Invalid publication receipt digest!";
                return false;
            }

            foreach (var property in new[] { "priceStatementSHA256", "tariffSHA256" })
                if (json[property]?.Type != JTokenType.String || !SHA256Hex().IsMatch(json[property]!.Value<String>()!))
                {
                    ErrorResponse = "Invalid signed-price or tariff digest!";
                    return false;
                }

            if (json["publicSourceURL"]?.Type != JTokenType.String ||
                !WitnessPublicationPolicy.TryGetPublicURL(json["publicSourceURL"]!.Value<String>()!, out _, out ErrorResponse))
            {
                ErrorResponse ??= "Publication sources require public HTTPS without credentials or fragments!";
                return false;
            }

            if (!SessionPriceAgreement.TryTimestamp(json, "observedAt", out var observedAt, out ErrorResponse) ||
                !SessionPriceAgreement.TryTimestamp(json, "signedAt",   out var signedAt,   out ErrorResponse))
            {
                return false;
            }

            if (signedAt < observedAt || signedAt - observedAt > TimeSpan.FromMinutes(1))
            {
                ErrorResponse = "A publication receipt must be signed within one minute after observation!";
                return false;
            }

            Receipt = new PricePublicationReceipt(json, observedAt, signedAt);
            return true;

        }

        #endregion

        #region GetSigningBytes()

        /// <summary>
        /// The signature input: the canonical JSON of what it names, but its value.
        /// </summary>
        public Byte[] GetSigningBytes()
        {
            var signed = (JObject) JSON.DeepClone();
            signed.Remove("value");
            return CanonicalJSON.ToUTF8Bytes(signed);
        }

        #endregion

        #region Digest()

        /// <summary>
        /// The SHA-256 of the canonical JSON of the receipt, its signature included, as lowercase hex.
        /// </summary>
        public String Digest()

            => Convert.ToHexStringLower(SHA256.HashData(CanonicalJSON.ToUTF8Bytes(JSON)));

        #endregion

    }


    /// <summary>
    /// What a receipt counted for (WWCP_POI.TS PricePublicationVerification.receipts).
    /// </summary>
    /// <param name="WitnessId">The witness it names.</param>
    /// <param name="Status">"counted", "duplicate-organization", "after-start", "untrusted" or "invalid".</param>
    /// <param name="Detail">Why.</param>
    /// <param name="Organization">The organization of the witness, where trusted.</param>
    /// <param name="KeyId">The key, where trusted.</param>
    /// <param name="ReceiptDigest">The digest of the receipt, where verified.</param>
    /// <param name="LatestPublicationAt">The latest time of the observation and the signature, its clock error included.</param>
    /// <param name="SourceURL">The public source, where verified.</param>
    public sealed record PricePublicationReceiptResult(String           WitnessId,
                                                       String           Status,
                                                       String           Detail,
                                                       String?          Organization          = null,
                                                       String?          KeyId                 = null,
                                                       String?          ReceiptDigest         = null,
                                                       DateTimeOffset?  LatestPublicationAt   = null,
                                                       String?          SourceURL             = null);


    /// <summary>
    /// Whether independent witnesses retrieved the selected price publicly before the signed start of a session (WWCP_POI.TS
    /// PricePublicationVerification): "witnessed-before-signed-start" where enough organizations of the accepted policy did,
    /// "invalid" where a receipt is, "not-proven" otherwise. The actual start of the session is not proven independently.
    /// </summary>
    public sealed record PricePublicationVerification(String                                        Status,
                                                      Byte                                          RequiredOrganizations,
                                                      ImmutableArray<String>                        Organizations,
                                                      ImmutableArray<PricePublicationReceiptResult>  Receipts)
    {

        /// <summary>
        /// Before the start: by the clocks of trusted witnesses against the signed start.
        /// </summary>
        public String TimeBasis        { get; } = "trusted-witness-clocks-versus-signed-start";

        /// <summary>
        /// The actual start time: not proven independently.
        /// </summary>
        public String ActualStartTime  { get; } = "not-independently-proven";


        #region (static) TryVerify(Receipts, Policy, RoamingNetworkId, EVSEId, PriceStatement, Tariff, Checkpoints, ChargingStarted, Now, out Verification, out ErrorResponse, …)

        /// <summary>
        /// Verify the given receipts of the publication of the selected price under the given policy the verifier accepted
        /// (WWCP_POI.TS verifyPricePublication), the inclusion and the signature of the CPO checked before: each of a witness
        /// of the policy for the roaming network, by one of its keys, no key or identity of the RNO or the CPO, its observation
        /// and signature with the accepted clock error within the validity of the witness and the key, not later than now,
        /// binding the exact statement, its signature and the tariff, observed after the checkpoint and the statement; an
        /// organization counts once, and only where the latest of it is before the signed start.
        /// </summary>
        /// <param name="Receipts">The receipts, as an evidence gives them.</param>
        /// <param name="Policy">The witness publication policy the verifier accepted, if any.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="EVSEId">The EVSE of the session.</param>
        /// <param name="PriceStatement">The price statement selected, as the CPO signed it, as JSON.</param>
        /// <param name="Tariff">The tariff selected.</param>
        /// <param name="Checkpoints">The checkpoints proven to name the statement.</param>
        /// <param name="ChargingStarted">The signed start of the session.</param>
        /// <param name="Now">The time of the check.</param>
        /// <param name="Verification">The verification.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="ExcludedKeyMaterial">The thumbprints of the keys of the RNO and the CPO.</param>
        /// <param name="ExcludedIdentities">The identities of the RNO and the CPO.</param>
        public static Boolean TryVerify(IReadOnlyList<JToken>                                   Receipts,
                                        WitnessPublicationPolicy?                               Policy,
                                        RoamingNetwork_Id                                       RoamingNetworkId,
                                        String                                                  EVSEId,
                                        JObject                                                 PriceStatement,
                                        JObject                                                 Tariff,
                                        IEnumerable<RoamingNetworkCheckpoint>                   Checkpoints,
                                        DateTimeOffset                                          ChargingStarted,
                                        DateTimeOffset                                          Now,
                                        [NotNullWhen(true)]  out PricePublicationVerification?  Verification,
                                        [NotNullWhen(false)] out String?                        ErrorResponse,
                                        IEnumerable<String>?                                    ExcludedKeyMaterial   = null,
                                        IEnumerable<String>?                                    ExcludedIdentities    = null)
        {

            Verification = null;

            if (ChargingStarted > Now || Receipts.Count > 64 || !RoamingNetworkPriceStatement.TryParse(PriceStatement, out var statement, out _))
            {
                ErrorResponse = "Invalid publication evaluation context!";
                return false;
            }

            var excludedKeys        = (ExcludedKeyMaterial ?? []).ToHashSet(StringComparer.Ordinal);
            var excludedIdentities  = (ExcludedIdentities  ?? []).ToHashSet(StringComparer.Ordinal);
            var checkpoints         = Checkpoints.ToArray();
            var hashes              = PricePublicationReceipt.OfferHashesOf(PriceStatement, Tariff);
            var organizations       = new HashSet<String>(StringComparer.Ordinal);
            var rows                = new List<PricePublicationReceiptResult>();

            foreach (var raw in Receipts)
            {

                if (!PricePublicationReceipt.TryParse(raw, out var receipt, out var parseError))
                {
                    rows.Add(new PricePublicationReceiptResult("", "invalid", parseError));
                    continue;
                }

                var json     = receipt.JSON;
                var witness  = Policy?.Witnesses.FirstOrDefault(item => item.Id == receipt.WitnessId);

                if (witness is null || !witness.Networks.Contains(RoamingNetworkId.ToString()))
                {
                    rows.Add(new PricePublicationReceiptResult(receipt.WitnessId, "untrusted", "No locally authorized witness for this network"));
                    continue;
                }

                void Invalid(String Detail)
                    => rows.Add(new PricePublicationReceiptResult(receipt.WitnessId, "invalid", Detail));

                if (witness.Keys.FirstOrDefault(item => item.Id == json["keyId"]!.Value<String>()) is not POIPublicKey key)
                {
                    Invalid("The receipt key is not authorized for this witness");
                    continue;
                }

                if (excludedIdentities.Contains(witness.Id) || excludedIdentities.Contains(witness.Organization) ||
                    excludedKeys.Contains(Convert.ToHexStringLower(key.Key.Thumbprint())))
                {
                    Invalid("An RNO/CPO authority cannot count as an independent publication witness");
                    continue;
                }

                var lower = receipt.ObservedAt - witness.ClockError;
                var upper = receipt.SignedAt   + witness.ClockError;

                if (lower < witness.NotBefore || upper >= witness.NotAfter || !key.IsValidAt(lower) || !key.IsValidAt(upper))
                {
                    Invalid("Witness/key does not cover the observation and signing interval");
                    continue;
                }

                if (receipt.SignedAt > Now                                                        ||
                    json["roamingNetworkId"]!.    Value<String>() != RoamingNetworkId.ToString() ||
                    json["EVSEId"]!.              Value<String>() != EVSEId                      ||
                    json["priceStatementDigest"]!.Value<String>() != statement.Digest)
                {
                    Invalid("Foreign, substituted or future publication receipt");
                    continue;
                }

                if (json["priceStatementSHA256"]!.Value<String>() != hashes.PriceStatementSHA256 ||
                    json["tariffSHA256"]!.        Value<String>() != hashes.TariffSHA256)
                {
                    Invalid("Receipt does not bind the exact original CPO signature and selected tariff definition/currency");
                    continue;
                }

                if (checkpoints.FirstOrDefault(item => item.Id.ToString() == json["checkpointId"]!.Value<String>()) is not RoamingNetworkCheckpoint checkpoint ||
                    checkpoint.CreatedAt > receipt.ObservedAt + witness.ClockError ||
                    statement.SignedAt   > receipt.ObservedAt + witness.ClockError)
                {
                    Invalid("Receipt needs the exact authorized price checkpoint and a consistent observation time");
                    continue;
                }

                if (!VerifiedTrustTransition.TryVerifySignature(json["algorithm"]!.Value<String>()!, key, receipt.GetSigningBytes(), json["value"]!.Value<String>()!, out var signatureError))
                {
                    Invalid(signatureError);
                    continue;
                }

                PricePublicationReceiptResult Row(String Status, String Detail)
                    => new (witness.Id, Status, Detail, witness.Organization, key.Id, receipt.Digest(), upper, json["publicSourceURL"]!.Value<String>());

                if (upper >= ChargingStarted)
                    rows.Add(Row("after-start",            "The latest observation/signing time including the accepted clock error is not before the signed start"));
                else if (!organizations.Add(witness.Organization))
                    rows.Add(Row("duplicate-organization", "Only one vote per independently approved organization"));
                else
                    rows.Add(Row("counted",                "Public retrieval of this exact included offer witnessed before its signed charging start"));

            }

            var required = Policy?.RequiredOrganizations ?? 2;

            Verification   = new PricePublicationVerification(organizations.Count >= required
                                                                  ? "witnessed-before-signed-start"
                                                                  : rows.Any(row => row.Status == "invalid")
                                                                        ? "invalid"
                                                                        : "not-proven",
                                                              required,
                                                              [.. organizations.Order(StringComparer.Ordinal)],
                                                              [.. rows]);
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
