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
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The assignment of a price to a charging session by its charging station operator (WWCP_POI.TS SessionPriceAgreement,
    /// experimental): the exact start document of the session, its meter and EVSE, the time it started, the charging tariff
    /// and the price statement selected, how the price is bound and its profile, signed by the CPO with a signing key that
    /// may sign price statements. An assertion of the CPO, not the consent of the customer; an unknown one authorizes
    /// nothing.
    /// </summary>
    public sealed partial class SessionPriceAgreement
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext          = "https://open.charging.cloud/contexts/wwcp/sessionPriceAgreement/v1";

        /// <summary>
        /// The experimental profile of unconditional gross rates fixed at the start of a session.
        /// </summary>
        public const String SimpleGrossPriceProfile = "chargy-gross-rates/v1";

        private static readonly String[] Properties = [ "@context", "roamingNetworkId", "EVSEId", "meterSerialNumber", "chargingStarted", "startDocumentSHA256",
                                                        "chargingMode", "chargingTariffId", "priceStatementDigest", "priceBinding", "pricingProfile",
                                                        "role", "signer", "signedAt", "algorithm", "keyId", "value" ];

        private static readonly String[] Texts      = [ "roamingNetworkId", "EVSEId", "meterSerialNumber", "chargingMode", "chargingTariffId", "priceBinding",
                                                        "pricingProfile", "signer", "algorithm", "keyId", "value" ];

        [GeneratedRegex("^[0-9a-f]{64}$")]
        private static partial Regex SHA256Hex();

        #endregion

        #region Properties

        /// <summary>
        /// The JSON as signed.
        /// </summary>
        public JObject         JSON                   { get; }

        /// <summary>
        /// The roaming network.
        /// </summary>
        public String          RoamingNetworkId       => Text("roamingNetworkId");

        /// <summary>
        /// The EVSE of the session.
        /// </summary>
        public String          EVSEId                 => Text("EVSEId");

        /// <summary>
        /// The serial number of the meter of the session.
        /// </summary>
        public String          MeterSerialNumber      => Text("meterSerialNumber");

        /// <summary>
        /// When the session started.
        /// </summary>
        public DateTimeOffset  ChargingStarted        { get; }

        /// <summary>
        /// The SHA-256 of the exact start document of the session, as lowercase hex.
        /// </summary>
        public String          StartDocumentSHA256    => Text("startDocumentSHA256");

        /// <summary>
        /// The charging mode, e.g. "adhoc".
        /// </summary>
        public String          ChargingMode           => Text("chargingMode");

        /// <summary>
        /// The charging tariff selected.
        /// </summary>
        public String          ChargingTariffId       => Text("chargingTariffId");

        /// <summary>
        /// The digest of the price statement selected.
        /// </summary>
        public String          PriceStatementDigest   => Text("priceStatementDigest");

        /// <summary>
        /// How the price is bound, e.g. "fixed-at-start".
        /// </summary>
        public String          PriceBinding           => Text("priceBinding");

        /// <summary>
        /// The pricing profile, e.g. SimpleGrossPriceProfile.
        /// </summary>
        public String          PricingProfile         => Text("pricingProfile");

        /// <summary>
        /// The charging station operator signing it.
        /// </summary>
        public String          Signer                 => Text("signer");

        /// <summary>
        /// When it was signed.
        /// </summary>
        public DateTimeOffset  SignedAt               { get; }

        /// <summary>
        /// The signature algorithm.
        /// </summary>
        public String          Algorithm              => Text("algorithm");

        /// <summary>
        /// The @id of the signing key.
        /// </summary>
        public String          KeyId                  => Text("keyId");

        /// <summary>
        /// The signature, Base64.
        /// </summary>
        public String          Value                  => Text("value");

        #endregion

        #region (private) Constructor(s)

        private SessionPriceAgreement(JObject         JSON,
                                      DateTimeOffset  ChargingStarted,
                                      DateTimeOffset  SignedAt)
        {

            this.JSON             = JSON;
            this.ChargingStarted  = ChargingStarted;
            this.SignedAt         = SignedAt;

        }

        #endregion


        #region (static) Create(PrivateKey, KeyId, Algorithm, RoamingNetworkId, EVSEId, MeterSerialNumber, ChargingStarted, StartDocumentSHA256, ChargingTariffId, PriceStatementDigest, Signer, …)

        /// <summary>
        /// Create and sign the assignment of a price to a charging session, as its charging station operator does with a
        /// signing key that may sign price statements.
        /// </summary>
        /// <param name="PrivateKey">The private signing key of the CPO.</param>
        /// <param name="KeyId">The @id of the signing key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="EVSEId">The EVSE of the session.</param>
        /// <param name="MeterSerialNumber">The serial number of its meter.</param>
        /// <param name="ChargingStarted">When the session started.</param>
        /// <param name="StartDocumentSHA256">The SHA-256 of the exact start document, as lowercase hex.</param>
        /// <param name="ChargingTariffId">The charging tariff selected.</param>
        /// <param name="PriceStatementDigest">The digest of the price statement selected.</param>
        /// <param name="Signer">The charging station operator.</param>
        /// <param name="ChargingMode">The charging mode, "adhoc" by default.</param>
        /// <param name="PriceBinding">How the price is bound, "fixed-at-start" by default.</param>
        /// <param name="PricingProfile">The pricing profile, the simple gross rates by default.</param>
        /// <param name="SignedAt">When it is signed, now by default.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static SessionPriceAgreement Create(AsymmetricKeyParameter      PrivateKey,
                                                   String                      KeyId,
                                                   COSEAlgorithm               Algorithm,
                                                   RoamingNetwork_Id           RoamingNetworkId,
                                                   String                      EVSEId,
                                                   String                      MeterSerialNumber,
                                                   DateTimeOffset              ChargingStarted,
                                                   String                      StartDocumentSHA256,
                                                   String                      ChargingTariffId,
                                                   String                      PriceStatementDigest,
                                                   ChargingStationOperator_Id  Signer,
                                                   String                      ChargingMode     = "adhoc",
                                                   String                      PriceBinding     = "fixed-at-start",
                                                   String                      PricingProfile   = SimpleGrossPriceProfile,
                                                   DateTimeOffset?             SignedAt         = null,
                                                   Boolean                     Deterministic    = true)
        {

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            var json = new JObject(new JProperty("@context",              JSONLDContext),
                                   new JProperty("roamingNetworkId",      RoamingNetworkId.ToString()),
                                   new JProperty("EVSEId",                EVSEId),
                                   new JProperty("meterSerialNumber",     MeterSerialNumber),
                                   new JProperty("chargingStarted",       ChargingStarted.ToISO8601()),
                                   new JProperty("startDocumentSHA256",   StartDocumentSHA256),
                                   new JProperty("chargingMode",          ChargingMode),
                                   new JProperty("chargingTariffId",      ChargingTariffId),
                                   new JProperty("priceStatementDigest",  PriceStatementDigest),
                                   new JProperty("priceBinding",          PriceBinding),
                                   new JProperty("pricingProfile",        PricingProfile),
                                   new JProperty("role",                  RoamingNetworkCommitSignatureRoles.ChargingStationOperator),
                                   new JProperty("signer",                Signer.ToString()),
                                   new JProperty("signedAt",              (SignedAt ?? Timestamp.Now).ToISO8601()),
                                   new JProperty("algorithm",             Algorithm.Name),
                                   new JProperty("keyId",                 KeyId));

            json.Add("value", Convert.ToBase64String(Algorithm.Sign(CanonicalJSON.ToUTF8Bytes(json), PrivateKey, Deterministic: Deterministic)));

            return TryParse(json, out var agreement, out var error)
                       ? agreement
                       : throw new ArgumentException(error);

        }

        #endregion

        #region (static) TryParse(JSON, out Agreement, out ErrorResponse)

        /// <summary>
        /// Try to read the given assignment of a price to a charging session, naming nothing else.
        /// </summary>
        /// <param name="JSON">The JSON.</param>
        /// <param name="Agreement">The assignment.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JToken?                                          JSON,
                                       [NotNullWhen(true)]  out SessionPriceAgreement?  Agreement,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            Agreement = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "A session price assignment must be a JSON object!";
                return false;
            }

            if (!HistoricalMeterEvidence.Only(json, "A session price assignment", out ErrorResponse, Properties))
                return false;

            if (json["@context"]?.Type != JTokenType.String || json["@context"]!.Value<String>() != JSONLDContext ||
                json["role"]?.    Type != JTokenType.String || json["role"]!.    Value<String>() != RoamingNetworkCommitSignatureRoles.ChargingStationOperator)
            {
                ErrorResponse = "Unsupported price assignment context/role!";
                return false;
            }

            if (Texts.FirstOrDefault(name => json[name]?.Type != JTokenType.String) is String missing)
            {
                ErrorResponse = $"A session price assignment names its '{missing}' as a text!";
                return false;
            }

            if (new[] { "startDocumentSHA256", "priceStatementDigest" }.Any(name => json[name]?.Type != JTokenType.String || !SHA256Hex().IsMatch(json[name]!.Value<String>()!)))
            {
                ErrorResponse = "Invalid price assignment digest!";
                return false;
            }

            if (!TryTimestamp(json, "chargingStarted", out var chargingStarted, out ErrorResponse) ||
                !TryTimestamp(json, "signedAt",        out var signedAt,        out ErrorResponse))
            {
                return false;
            }

            Agreement = new SessionPriceAgreement(json, chargingStarted, signedAt);
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

        #region TryVerify(Station, out ErrorResponse)

        /// <summary>
        /// Verify this assignment against the given verified charging station: of its roaming network, signed by its
        /// operator with a signing key of it valid when it was signed and when charging started, that may sign price
        /// statements.
        /// </summary>
        /// <param name="Station">The verified charging station.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryVerify(VerifiedChargingStation           Station,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (RoamingNetworkId != Station.RoamingNetworkId.ToString() || Signer != VerifiedChargingStation.IdOf(Station.Operator))
            {
                ErrorResponse = "Foreign price assignment authority!";
                return false;
            }

            if (Station.OperatorSigningKeys.FirstOrDefault(key => key.Id == KeyId) is not POIPublicKey key ||
                !key.IsValidAt(SignedAt) ||
                !key.IsValidAt(ChargingStarted) ||
                !key.MaySign(POIKeyUsages.PriceStatements))
            {
                ErrorResponse = "Unauthorized/expired price assignment key!";
                return false;
            }

            try
            {

                if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || algorithm.Name != Algorithm)
                {
                    ErrorResponse = "Unknown or noncanonical signature algorithm of the price assignment!";
                    return false;
                }

                var value = Convert.FromBase64String(Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Value)
                {
                    ErrorResponse = "The signature of the price assignment must be nonempty canonical Base64!";
                    return false;
                }

                if (!algorithm.Verify(GetSigningBytes(), value, key.Key.ToPublicKey(), out var signatureError))
                {
                    ErrorResponse = $"The signature of the price assignment is invalid: {signatureError}";
                    return false;
                }

            }
            catch (Exception e)
            {
                ErrorResponse = $"The signature of the price assignment is invalid: {e.Message}";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (private) Text(Name)

        private String Text(String Name)

            => JSON[Name]!.Value<String>()!;

        #endregion

        #region (internal static) TryTimestamp(JSON, Name, out Timestamp, out ErrorResponse)

        internal static Boolean TryTimestamp(JObject                           JSON,
                                             String                            Name,
                                             out DateTimeOffset                Timestamp,
                                             [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Timestamp = default;

            if (JSON[Name]?.Type != JTokenType.String ||
                !DateTimeOffset.TryParse(JSON[Name]!.Value<String>(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out Timestamp))
            {
                ErrorResponse = $"The '{Name}' must be an ISO 8601 timestamp!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
