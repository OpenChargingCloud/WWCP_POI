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

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The certificate of a signing key of a roaming network operator (SPECIFICATION.md, section 8.6): the signing key,
    /// with its validity, its holder and its usages, signed by an identity key of the same holder. Each holder of a quorum
    /// certifies its own signing keys, so that it renews them without the others: a signing key gives its holder one
    /// voice only, so no holder decides more alone than with its signing key (section 4.1). The roaming network operator
    /// embeds its certificates as signingKeyCertificates, the only place naming its signing keys. A certificate names
    /// the operator, not a roaming network: its signing keys are its own in every network it operates, and a network
    /// trusts it only as far as it trusts the identity key that signed it.
    /// </summary>
    public sealed class RoamingNetworkSigningKeyCertificate
    {

        #region Data

        /// <summary>
        /// The name of the property of the roaming network operator embedding them.
        /// </summary>
        public const String JSONProperty    = "signingKeyCertificates";

        /// <summary>
        /// The JSON-LD context of the object, which names what its signature signs and in which version. It is part of the
        /// signature input also where it is embedded, as in the roaming network operator.
        /// </summary>
        public const String JSONLDContext   = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/signingKeyCertificate/v2";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                   = "@context";

            /// <summary>
            /// The property of the identification of the roaming network operator.
            /// </summary>
            public const String  RoamingNetworkOperatorId  = "roamingNetworkOperatorId";

            /// <summary>
            /// The property of the signing key it certifies.
            /// </summary>
            public const String  SigningKey                = "signingKey";

            /// <summary>
            /// The property of its signing timestamp.
            /// </summary>
            public const String  SignedAt                  = "signedAt";

            /// <summary>
            /// The property of the algorithm of its signature.
            /// </summary>
            public const String  Algorithm                 = "algorithm";

            /// <summary>
            /// The property of the identification of the identity key that signed it.
            /// </summary>
            public const String  KeyId                     = "keyId";

            /// <summary>
            /// The property of the value of its signature.
            /// </summary>
            public const String  Value                     = "value";

        }

        #endregion


        /// <summary>
        /// The roaming network operator.
        /// </summary>
        public RoamingNetworkOperator_Id  RoamingNetworkOperatorId    { get; }

        /// <summary>
        /// The signing key it certifies, with its validity, its holder and its usages.
        /// </summary>
        public POIPublicKey               SigningKey                  { get; }

        /// <summary>
        /// When it was signed, signed as text: yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        public DateTimeOffset             SignedAt                    { get; }

        /// <summary>
        /// The COSE name of the algorithm of its signature, e.g. ES512.
        /// </summary>
        public String                     Algorithm                   { get; }

        /// <summary>
        /// The @id of the identity key that signed it: one of the same holder as the signing key.
        /// </summary>
        public String                     KeyId                       { get; }

        /// <summary>
        /// The value of its signature, canonical Base64.
        /// </summary>
        public String                     Value                       { get; }

        /// <summary>
        /// The holder of the signing key, and so of the identity key that signed it, as a quorum counts it.
        /// </summary>
        public String                     Holder
            => SigningKey.HolderOrUnnamed;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a certificate of its parts; Create(…) signs a new one.
        /// </summary>
        public RoamingNetworkSigningKeyCertificate(RoamingNetworkOperator_Id  RoamingNetworkOperatorId,
                                                   POIPublicKey               SigningKey,
                                                   DateTimeOffset             SignedAt,
                                                   String                     Algorithm,
                                                   String                     KeyId,
                                                   String                     Value)
        {

            ArgumentNullException.ThrowIfNull(SigningKey);

            if (String.IsNullOrWhiteSpace(Algorithm) || String.IsNullOrWhiteSpace(KeyId) || String.IsNullOrEmpty(Value))
                throw new ArgumentException("A signing key certificate requires the algorithm, the key identification and the value of its signature.");

            if (KeyId == SigningKey.Id)
                throw new ArgumentException("A signing key does not certify itself: an identity key certifies it.", nameof(KeyId));

            this.RoamingNetworkOperatorId  = RoamingNetworkOperatorId;
            this.SigningKey                = SigningKey;
            this.SignedAt                  = SignedAt;
            this.Algorithm                 = Algorithm;
            this.KeyId                     = KeyId;
            this.Value                     = Value;

        }

        #endregion


        #region (static) Create(IdentityKey,     Algorithm, RoamingNetworkOperatorId, SigningKey, SignedAt, Deterministic = true)

        /// <summary>
        /// Certify the given signing key with the given private identity key of the same holder.
        /// </summary>
        /// <param name="IdentityKey">The private identity key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
        /// <param name="SigningKey">The signing key it certifies, with its validity, its holder and its usages.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkSigningKeyCertificate Create(AsymmetricKeyParameter     IdentityKey,
                                                                 COSEAlgorithm              Algorithm,
                                                                 RoamingNetworkOperator_Id  RoamingNetworkOperatorId,
                                                                 POIPublicKey               SigningKey,
                                                                 DateTimeOffset             SignedAt,
                                                                 Boolean                    Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(IdentityKey);

            if (!IdentityKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(IdentityKey));

            var keyId        = POIPublicKey.From(IdentityKey).Id;
            var signedAt     = DateTimeOffset.Parse(SignedAt.ToISO8601(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

            // Signed over what it says, with a placeholder value that the signature input leaves out.
            var unsigned     = new RoamingNetworkSigningKeyCertificate(RoamingNetworkOperatorId, SigningKey, signedAt, Algorithm.Name, keyId, "-");

            return new RoamingNetworkSigningKeyCertificate(RoamingNetworkOperatorId,
                                                           SigningKey,
                                                           signedAt,
                                                           Algorithm.Name,
                                                           keyId,
                                                           Convert.ToBase64String(Algorithm.Sign(unsigned.GetSigningBytes(), IdentityKey, Deterministic: Deterministic)));

        }

        #endregion

        #region (static) Create(IdentityKeyPair,            RoamingNetworkOperatorId, SigningKey, SignedAt, Deterministic = true)

        /// <summary>
        /// Certify the given signing key with the given identity key pair of the same holder and its signature algorithm.
        /// </summary>
        /// <param name="IdentityKeyPair">The COSE pair of the private identity key and its signature algorithm.</param>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
        /// <param name="SigningKey">The signing key it certifies, with its validity, its holder and its usages.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkSigningKeyCertificate Create(COSEKeyPair                IdentityKeyPair,
                                                                 RoamingNetworkOperator_Id  RoamingNetworkOperatorId,
                                                                 POIPublicKey               SigningKey,
                                                                 DateTimeOffset             SignedAt,
                                                                 Boolean                    Deterministic   = true)

            => Create(IdentityKeyPair.Private,
                      IdentityKeyPair.Algorithm,
                      RoamingNetworkOperatorId,
                      SigningKey,
                      SignedAt,
                      Deterministic);

        #endregion


        #region TryVerify(IdentityKeys, out IdentityKey, out ErrorResponse, TrustedIdentityKeyIds = null)

        /// <summary>
        /// Verify this certificate against the given identity keys, e.g. those of the roaming network operator: it must be
        /// signed by one of them, of the same holder as the signing key, valid when it was signed, and, where given, one the
        /// verifier trusts by its @id.
        /// </summary>
        /// <param name="IdentityKeys">The identity keys among which the one that signed it is found.</param>
        /// <param name="IdentityKey">The identity key that signed it.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="TrustedIdentityKeyIds">The @ids of the identity keys the verifier trusts, unless all given.</param>
        public Boolean TryVerify(IEnumerable<POIPublicKey>                     IdentityKeys,
                                 [NotNullWhen(true)]  out POIPublicKey?        IdentityKey,
                                 [NotNullWhen(false)] out String?              ErrorResponse,
                                 IEnumerable<String>?                          TrustedIdentityKeyIds   = null)
        {

            IdentityKey = null;

            if (IdentityKeys.FirstOrDefault(key => key.Id == KeyId) is not POIPublicKey identityKey)
            {
                ErrorResponse = $"The identity key '{KeyId}' that certified the signing key '{SigningKey.Id}' is not given!";
                return false;
            }

            if (TrustedIdentityKeyIds is not null && !TrustedIdentityKeyIds.Contains(KeyId, StringComparer.Ordinal))
            {
                ErrorResponse = $"The signing key '{SigningKey.Id}' is certified by the identity key '{KeyId}', which is not trusted!";
                return false;
            }

            if (identityKey.HolderOrUnnamed != SigningKey.HolderOrUnnamed)
            {
                ErrorResponse = $"The signing key '{SigningKey.Id}' of {HolderText(SigningKey)} is certified by the identity key '{KeyId}' of {HolderText(identityKey)}: each holder certifies its own signing keys only!";
                return false;
            }

            if (!identityKey.IsValidAt(SignedAt))
            {
                ErrorResponse = $"The identity key '{KeyId}' was not valid when it certified the signing key '{SigningKey.Id}'!";
                return false;
            }

            if (!VerifySignature(identityKey, out ErrorResponse))
            {
                ErrorResponse = $"The certificate of the signing key '{SigningKey.Id}' is invalid: {ErrorResponse}";
                return false;
            }

            IdentityKey    = identityKey;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region VerifySignature(IdentityKey, out ErrorResponse)

        /// <summary>
        /// Verify the signature of this certificate by the given public identity key, which its keyId names. Noncanonical
        /// algorithm names, noncanonical Base64, other keys and malformed inputs return false.
        /// </summary>
        /// <param name="IdentityKey">The public identity key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(POIPublicKey                      IdentityKey,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                if (KeyId != IdentityKey.Id)
                    throw new ArgumentException("The certificate is signed by another key.");

                if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || algorithm.Name != Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");

                if (IdentityKey.Key.Algorithm is { } restricted && restricted.Name != Algorithm)
                    throw new ArgumentException("The signature algorithm differs from the key's algorithm restriction.");

                var value = Convert.FromBase64String(Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Value)
                    throw new ArgumentException("The value must contain nonempty canonical standard Base64.");

                return algorithm.Verify(GetSigningBytes(), value, IdentityKey.Key.ToPublicKey(), out ErrorResponse);

            }
            catch (Exception exception)
            {
                ErrorResponse = exception.Message;
                return false;
            }

        }

        #endregion

        #region GetSigningBytes()

        /// <summary>
        /// The signature input of this certificate, as Styx canonical JSON: its JSON-LD context, what it certifies, the
        /// algorithm and the keyId; no value.
        /// </summary>
        public Byte[] GetSigningBytes()
        {

            var json = Content();

            json.AddFirst(new JProperty(PropertyKeys.Context, JSONLDContext));

            return CanonicalJSON.ToUTF8Bytes(json);

        }

        #endregion


        #region (static) TryParse(JSON, out Certificate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a signing key certificate, read with strings as strings. Its
        /// signature is not verified here.
        /// </summary>
        public static Boolean TryParse(JObject                                                        JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkSigningKeyCertificate?  Certificate,
                                       [NotNullWhen(false)] out String?                               ErrorResponse)
        {

            Certificate = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of a signing key certificate must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatory(PropertyKeys.RoamingNetworkOperatorId,
                                         "roaming network operator identification",
                                         RoamingNetworkOperator_Id.TryParse,
                                         out RoamingNetworkOperator_Id roamingNetworkOperatorId,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.SigningKey] is not JObject signingKeyJSON ||
                    !POIPublicKey.TryParse(signingKeyJSON, out var signingKey, out var signingKeyError))
                {
                    ErrorResponse = $"A signing key certificate requires the {PropertyKeys.SigningKey} it certifies!";
                    return false;
                }

                // Signed as text: written exactly so, or read as a date by a reader parsing dates, to the millisecond.
                var signedAtText = JSON[PropertyKeys.SignedAt] is JValue { Type: JTokenType.Date } signedAtDate
                                       ? signedAtDate.Value is DateTimeOffset dateTimeOffset
                                             ? dateTimeOffset.ToISO8601()
                                             : new DateTimeOffset(DateTime.SpecifyKind(signedAtDate.Value<DateTime>(), DateTimeKind.Utc)).ToISO8601()
                                       : Text(JSON, PropertyKeys.SignedAt);

                if (signedAtText is null ||
                    !DateTimeOffset.TryParse(signedAtText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var signedAt) ||
                    signedAt.ToISO8601() != signedAtText)
                {
                    ErrorResponse = $"The {PropertyKeys.SignedAt} of a signing key certificate must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                if (Text(JSON, PropertyKeys.Algorithm) is not String algorithm || algorithm.Length == 0 ||
                    Text(JSON, PropertyKeys.KeyId)     is not String keyId     || keyId.    Length == 0 ||
                    Text(JSON, PropertyKeys.Value)     is not String value     || value.    Length == 0)
                {
                    ErrorResponse = "A signing key certificate requires the algorithm, the key identification and the value of its signature!";
                    return false;
                }

                Certificate    = new RoamingNetworkSigningKeyCertificate(roamingNetworkOperatorId,
                                                                         signingKey,
                                                                         signedAt,
                                                                         algorithm,
                                                                         keyId,
                                                                         value);

                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Certificate    = null;
                ErrorResponse  = "Invalid signing key certificate: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomCertificateSerializer = null)

        /// <summary>
        /// Return the JSON representation of this certificate: what it certifies and its signature.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomCertificateSerializer">An optional custom serializer of signing key certificates.</param>
        public JObject ToJSON(Boolean                                                                Embedded                      = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkSigningKeyCertificate>?  CustomCertificateSerializer   = null)
        {

            var json = Content();

            if (!Embedded)
                json.AddFirst(new JProperty(PropertyKeys.Context, JSONLDContext));

            json.Add(PropertyKeys.Value, Value);

            return CustomCertificateSerializer is not null
                       ? CustomCertificateSerializer(this, json)
                       : json;

        }

        #endregion


        #region (internal static) Ordered(Certificates, Description)

        /// <summary>
        /// The given certificates in the order of the signing keys they certify, then of the identity keys that signed them,
        /// without one given twice.
        /// </summary>
        internal static ImmutableArray<RoamingNetworkSigningKeyCertificate> Ordered(IEnumerable<RoamingNetworkSigningKeyCertificate>?  Certificates,
                                                                                    String                                             Description)

            => [ .. POIJSON.Distinct(Certificates, certificate => $"{certificate.SigningKey.Id}/{certificate.KeyId}", Description).
                            OrderBy (certificate => certificate.SigningKey.Id, StringComparer.Ordinal).
                            ThenBy  (certificate => certificate.KeyId,         StringComparer.Ordinal) ];

        #endregion

        #region (private) Content()

        /// <summary>
        /// What this certificate says, without its value: its operator, signing key, signing time, and the algorithm and
        /// identity key of its signature.
        /// </summary>
        private JObject Content()

            => new (new JProperty(PropertyKeys.RoamingNetworkOperatorId,  RoamingNetworkOperatorId.ToString()),
                    new JProperty(PropertyKeys.SigningKey,                SigningKey.              ToJSON(Embedded: true)),
                    new JProperty(PropertyKeys.SignedAt,                  SignedAt.                ToISO8601()),
                    new JProperty(PropertyKeys.Algorithm,                 Algorithm),
                    new JProperty(PropertyKeys.KeyId,                     KeyId));

        #endregion

        #region (private static) Text(JSON, PropertyName)

        private static String? Text(JObject JSON, String PropertyName)

            => JSON[PropertyName]?.Type == JTokenType.String
                   ? JSON[PropertyName]!.Value<String>()
                   : null;

        #endregion

        #region (private static) HolderText(Key)

        private static String HolderText(POIPublicKey Key)

            => Key.Holder is not null
                   ? $"the holder '{Key.Holder}'"
                   : "no named holder";

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"Certificate of the signing key '{SigningKey.Id}' of {RoamingNetworkOperatorId}, signed by '{KeyId}' at {SignedAt.ToISO8601()}";

        #endregion

    }

}
