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
    /// A key statement of a roaming network operator (SPECIFICATION.md, section 8.6): which signing keys it uses, signed
    /// with one of its identity keys. An app that pins the identity keys of the operator, e.g. by scanning the QR code of
    /// a RoamingNetworkTrustAnchor, trusts the signing keys it names, and so the attestations they sign, without trusting
    /// the network that names them. It is part of the content: the roaming network operator embeds it as keyStatement,
    /// so that every proof of a charging station gives it, peers take it and witnesses see it.
    /// </summary>
    public sealed class RoamingNetworkKeyStatement
    {

        #region Data

        /// <summary>
        /// The profile of the signature of a key statement.
        /// </summary>
        public const String SigningProfile  = "wwcp-poi-key-statement-v1";

        /// <summary>
        /// The name of the property of the roaming network operator embedding it.
        /// </summary>
        public const String JSONProperty    = "keyStatement";

        #endregion

        #region Properties

        /// <summary>
        /// The roaming network.
        /// </summary>
        public RoamingNetwork_Id              RoamingNetworkId            { get; }

        /// <summary>
        /// The roaming network operator stating it.
        /// </summary>
        public RoamingNetworkOperator_Id      RoamingNetworkOperatorId    { get; }

        /// <summary>
        /// The signing keys it states, e.g. those of attestations, each with its validity.
        /// </summary>
        public ImmutableArray<POIPublicKey>   SigningKeys                 { get; }

        /// <summary>
        /// The identity keys of the roaming network operator, which sign its next key statements: a verifier trusting this
        /// statement may trust them as well, e.g. when the operator replaces an identity key.
        /// </summary>
        public ImmutableArray<POIPublicKey>   IdentityKeys                { get; }

        /// <summary>
        /// When it was signed, signed as text: yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        public DateTimeOffset                 SignedAt                    { get; }

        /// <summary>
        /// Until when it is valid: a verifier refuses it afterwards.
        /// </summary>
        public DateTimeOffset                 ValidUntil                  { get; }

        /// <summary>
        /// The COSE name of the signature algorithm, e.g. ES512.
        /// </summary>
        public String                         Algorithm                   { get; }

        /// <summary>
        /// The @id of the identity key that signed it.
        /// </summary>
        public String                         KeyId                       { get; }

        /// <summary>
        /// The signature value, canonical Base64.
        /// </summary>
        public String                         Value                       { get; }

        /// <summary>
        /// The profile of the signature.
        /// </summary>
        public String                         Profile                     { get; }

        /// <summary>
        /// The encoding of the signature value: base64.
        /// </summary>
        public String                         Encoding                    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a key statement of its parts; Create(…) signs a new one.
        /// </summary>
        public RoamingNetworkKeyStatement(RoamingNetwork_Id           RoamingNetworkId,
                                          RoamingNetworkOperator_Id   RoamingNetworkOperatorId,
                                          IEnumerable<POIPublicKey>   SigningKeys,
                                          IEnumerable<POIPublicKey>?  IdentityKeys,
                                          DateTimeOffset              SignedAt,
                                          DateTimeOffset              ValidUntil,
                                          String                      Algorithm,
                                          String                      KeyId,
                                          String                      Value,
                                          String                      Profile    = SigningProfile,
                                          String                      Encoding   = "base64")
        {

            this.SigningKeys   = POIPublicKey.Ordered(SigningKeys,  "the signing keys of a key statement");
            this.IdentityKeys  = POIPublicKey.Ordered(IdentityKeys, "the identity keys of a key statement");

            if (this.SigningKeys.IsEmpty)
                throw new ArgumentException("A key statement names at least one signing key.", nameof(SigningKeys));

            if (this.SigningKeys.Any(signingKey => this.IdentityKeys.Any(identityKey => identityKey.Id == signingKey.Id)))
                throw new ArgumentException("A key is not both a signing key and an identity key.", nameof(SigningKeys));

            if (ValidUntil < SignedAt)
                throw new ArgumentException("A key statement is not valid until before it was signed.", nameof(ValidUntil));

            if (String.IsNullOrWhiteSpace(Algorithm) || String.IsNullOrWhiteSpace(KeyId) || String.IsNullOrEmpty(Value))
                throw new ArgumentException("A key statement requires an algorithm, a key identification and a value.");

            this.RoamingNetworkId          = RoamingNetworkId;
            this.RoamingNetworkOperatorId  = RoamingNetworkOperatorId;
            this.SignedAt                  = SignedAt;
            this.ValidUntil                = ValidUntil;
            this.Algorithm                 = Algorithm;
            this.KeyId                     = KeyId;
            this.Value                     = Value;
            this.Profile                   = Profile;
            this.Encoding                  = Encoding;

        }

        #endregion


        #region (static) Create(IdentityKey, Algorithm, RoamingNetworkId, RoamingNetworkOperatorId, SigningKeys, IdentityKeys, SignedAt, ValidUntil, Deterministic = true)

        /// <summary>
        /// Sign a new key statement with the given private identity key of the roaming network operator.
        /// </summary>
        /// <param name="IdentityKey">The private identity key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
        /// <param name="SigningKeys">The signing keys it states.</param>
        /// <param name="IdentityKeys">The identity keys of the operator, which sign its next key statements.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="ValidUntil">Until when it is valid.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkKeyStatement Create(AsymmetricKeyParameter      IdentityKey,
                                                        COSEAlgorithm               Algorithm,
                                                        RoamingNetwork_Id           RoamingNetworkId,
                                                        RoamingNetworkOperator_Id   RoamingNetworkOperatorId,
                                                        IEnumerable<POIPublicKey>   SigningKeys,
                                                        IEnumerable<POIPublicKey>?  IdentityKeys,
                                                        DateTimeOffset              SignedAt,
                                                        DateTimeOffset              ValidUntil,
                                                        Boolean                     Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(IdentityKey);

            if (!IdentityKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(IdentityKey));

            // Without a value yet, only to get the signed fields exactly as they will be written.
            var unsigned = new RoamingNetworkKeyStatement(RoamingNetworkId,
                                                          RoamingNetworkOperatorId,
                                                          SigningKeys,
                                                          IdentityKeys,
                                                          Truncated(SignedAt),
                                                          Truncated(ValidUntil),
                                                          Algorithm.Name,
                                                          POIPublicKey.From(IdentityKey).Id,
                                                          "-");

            return new RoamingNetworkKeyStatement(unsigned.RoamingNetworkId,
                                                  unsigned.RoamingNetworkOperatorId,
                                                  unsigned.SigningKeys,
                                                  unsigned.IdentityKeys,
                                                  unsigned.SignedAt,
                                                  unsigned.ValidUntil,
                                                  unsigned.Algorithm,
                                                  unsigned.KeyId,
                                                  Convert.ToBase64String(Algorithm.Sign(unsigned.GetSigningBytes(), IdentityKey, Deterministic: Deterministic)));

        }

        #endregion

        #region IsValidAt(Timestamp)

        /// <summary>
        /// Whether it is valid at the given time: not before it was signed, not after its validUntil.
        /// </summary>
        public Boolean IsValidAt(DateTimeOffset Timestamp)

            => SignedAt <= Timestamp && Timestamp <= ValidUntil;

        #endregion

        #region VerifySignature(IdentityKey, out ErrorResponse)

        /// <summary>
        /// Verify the signature of this key statement with the given public identity key, whose @id it names.
        /// Unknown profiles, noncanonical Base64, other keys and malformed inputs return false.
        /// </summary>
        /// <param name="IdentityKey">The public identity key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(POIPublicKey                      IdentityKey,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                if (Profile != SigningProfile || Encoding != "base64")
                    throw new ArgumentException("Unsupported key statement profile or encoding.");

                if (KeyId != IdentityKey.Id)
                    throw new ArgumentException("The key statement is signed by another key.");

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

        #region TryVerify(TrustedIdentityKeyIds, OtherIdentityKeys, out ErrorResponse)

        /// <summary>
        /// Verify this key statement against identity keys the verifier trusts by their @ids, e.g. those of a QR code it
        /// scanned: it must be signed by one of them, given among its own identity keys or among the given other ones,
        /// e.g. those of the roaming network operator a proof gives, each found by its @id, the SHA-256 thumbprint of the key.
        /// </summary>
        /// <param name="TrustedIdentityKeyIds">The @ids of the identity keys the verifier trusts.</param>
        /// <param name="OtherIdentityKeys">Other identity keys, whose @ids the verifier may trust.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryVerify(IEnumerable<String>                 TrustedIdentityKeyIds,
                                 IEnumerable<POIPublicKey>?          OtherIdentityKeys,
                                 [NotNullWhen(false)] out String?    ErrorResponse)
        {

            if (!TrustedIdentityKeyIds.Contains(KeyId))
            {
                ErrorResponse = $"The key statement is signed by the key '{KeyId}', which is not trusted!";
                return false;
            }

            var identityKey = IdentityKeys.Concat(OtherIdentityKeys ?? []).FirstOrDefault(key => key.Id == KeyId);

            if (identityKey is null)
            {
                ErrorResponse = $"The identity key '{KeyId}' that signed the key statement is not given!";
                return false;
            }

            if (!identityKey.IsValidAt(SignedAt))
            {
                ErrorResponse = $"The identity key '{KeyId}' was not valid when the key statement was signed!";
                return false;
            }

            return VerifySignature(identityKey, out ErrorResponse);

        }

        #endregion

        #region SigningKeyOf(KeyId, At)

        /// <summary>
        /// The signing key it states with the given @id, valid at the given time, or null.
        /// </summary>
        public POIPublicKey? SigningKeyOf(String          KeyId,
                                          DateTimeOffset  At)

            => SigningKeys.FirstOrDefault(signingKey => signingKey.Id == KeyId && signingKey.IsValidAt(At));

        #endregion

        #region GetSigningBytes()

        /// <summary>
        /// The signature input of this key statement, as Styx canonical JSON: its signed fields, without its value.
        /// </summary>
        public Byte[] GetSigningBytes()
        {

            var json = Content();

            json.Add("profile",    SigningProfile);
            json.Add("algorithm",  Algorithm);
            json.Add("keyId",      KeyId);
            json.Add("encoding",   "base64");

            return CanonicalJSON.ToUTF8Bytes(json);

        }

        #endregion


        #region (static) TryParse(JSON, out KeyStatement, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a key statement, read with strings as strings. Its signature is
        /// not verified here.
        /// </summary>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkKeyStatement?  KeyStatement,
                                       [NotNullWhen(false)] out String?                      ErrorResponse)
        {

            KeyStatement = null;

            try
            {

                if (!JSON.ParseMandatory("roamingNetworkId",
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse) ||
                    !JSON.ParseMandatory("roamingNetworkOperatorId",
                                         "roaming network operator identification",
                                         RoamingNetworkOperator_Id.TryParse,
                                         out RoamingNetworkOperator_Id roamingNetworkOperatorId,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!TryParseKeys(JSON, "signingKeys",  out var signingKeys,  out ErrorResponse) ||
                    !TryParseKeys(JSON, "identityKeys", out var identityKeys, out ErrorResponse))
                {
                    return false;
                }

                if (!TryParseTimestamp(JSON, "signedAt",   out var signedAt) ||
                    !TryParseTimestamp(JSON, "validUntil", out var validUntil))
                {
                    ErrorResponse = "The signedAt and validUntil of a key statement must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                if (Text(JSON, "algorithm") is not String algorithm || algorithm.Length == 0 ||
                    Text(JSON, "keyId")     is not String keyId     || keyId.    Length == 0 ||
                    Text(JSON, "value")     is not String value     || value.    Length == 0 ||
                    Text(JSON, "profile")   is not String profile   ||
                    Text(JSON, "encoding")  is not String encoding)
                {
                    ErrorResponse = "A key statement requires an algorithm, a key identification, a value, a profile and an encoding!";
                    return false;
                }

                KeyStatement   = new RoamingNetworkKeyStatement(roamingNetworkId,
                                                                roamingNetworkOperatorId,
                                                                signingKeys,
                                                                identityKeys,
                                                                signedAt,
                                                                validUntil,
                                                                algorithm,
                                                                keyId,
                                                                value,
                                                                profile,
                                                                encoding);

                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                KeyStatement   = null;
                ErrorResponse  = "Invalid key statement: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this key statement.
        /// </summary>
        public JObject ToJSON()
        {

            var json = Content();

            json.Add("algorithm",  Algorithm);
            json.Add("keyId",      KeyId);
            json.Add("value",      Value);
            json.Add("profile",    Profile);
            json.Add("encoding",   Encoding);

            return json;

        }

        #endregion


        #region (private) Content()

        /// <summary>
        /// What this key statement states, without its signature.
        /// </summary>
        private JObject Content()
        {

            var json = new JObject(
                           new JProperty("roamingNetworkId",          RoamingNetworkId.        ToString()),
                           new JProperty("roamingNetworkOperatorId",  RoamingNetworkOperatorId.ToString()),
                           new JProperty("signingKeys",               new JArray(SigningKeys.Select(signingKey => signingKey.ToJSON())))
                       );

            if (!IdentityKeys.IsEmpty)
                json.Add("identityKeys",  new JArray(IdentityKeys.Select(identityKey => identityKey.ToJSON())));

            json.Add("signedAt",          SignedAt.  ToISO8601());
            json.Add("validUntil",        ValidUntil.ToISO8601());

            return json;

        }

        #endregion

        #region (private static) TryParseKeys(JSON, PropertyName, out Keys, out ErrorResponse)

        private static Boolean TryParseKeys(JObject                           JSON,
                                            String                            PropertyName,
                                            out List<POIPublicKey>            Keys,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Keys = [];

            if (JSON[PropertyName] is null)
            {
                ErrorResponse = null;
                return true;
            }

            if (JSON[PropertyName] is not JArray keysJSON)
            {
                ErrorResponse = $"The {PropertyName} of a key statement must be a JSON array!";
                return false;
            }

            foreach (var keyJSON in keysJSON)
            {

                if (keyJSON is not JObject keyObject || !POIPublicKey.TryParse(keyObject, out var key, out var keyError))
                {
                    ErrorResponse = $"Invalid key in the {PropertyName} of a key statement!";
                    return false;
                }

                Keys.Add(key);

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) Truncated(Timestamp)

        /// <summary>
        /// The given timestamp to the millisecond, as it is signed.
        /// </summary>
        private static DateTimeOffset Truncated(DateTimeOffset Timestamp)

            => DateTimeOffset.Parse(Timestamp.ToISO8601(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        #endregion

        #region (private static) Text(JSON, PropertyName)

        private static String? Text(JObject JSON, String PropertyName)

            => JSON[PropertyName]?.Type == JTokenType.String
                   ? JSON[PropertyName]!.Value<String>()
                   : null;

        #endregion

        #region (private static) TryParseTimestamp(JSON, PropertyName, out Timestamp)

        /// <summary>
        /// A mandatory timestamp, written exactly as yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        private static Boolean TryParseTimestamp(JObject             JSON,
                                                 String              PropertyName,
                                                 out DateTimeOffset  Timestamp)
        {

            Timestamp = default;

            return Text(JSON, PropertyName) is String text &&
                   DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out Timestamp) &&
                   Timestamp.ToISO8601() == text;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"Key statement of {RoamingNetworkOperatorId} for {RoamingNetworkId}: {SigningKeys.Length} signing keys, valid until {ValidUntil.ToISO8601()}, signed by '{KeyId}'";

        #endregion

    }

}
