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
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate verifying the signature of an attestation with a trusted key, e.g. by resolving the key of its key
    /// identification and calling VerifySignature. The key MUST be trusted independently of the network it attests.
    /// </summary>
    /// <param name="Attestation">The attestation.</param>
    public delegate Boolean VerifyAttestationDelegate(RoamingNetworkAttestation Attestation);

    /// <summary>
    /// A delegate called for every attestation a history takes.
    /// </summary>
    /// <param name="Attestation">The attestation.</param>
    public delegate void OnRoamingNetworkAttestationDelegate(RoamingNetworkAttestation Attestation);


    /// <summary>
    /// What an attestation attests instead of the root: the digest of one entity of the tree (SPECIFICATION.md, section 8.5),
    /// e.g. a charging station operator attesting its own node.
    /// </summary>
    /// <param name="EntityType">The type of the entity, e.g. ChargingStationOperator.</param>
    /// <param name="EntityId">The identification of the entity.</param>
    /// <param name="Digest">The digest of the entity, lowercase hex.</param>
    public sealed record RoamingNetworkAttestationSubject(String  EntityType,
                                                          String  EntityId,
                                                          String  Digest)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetworkAttestationSubject";

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context     = "@context";

            /// <summary>
            /// The property of the type of its entity.
            /// </summary>
            public const String  EntityType  = "entityType";

            /// <summary>
            /// The property of the identification of its entity.
            /// </summary>
            public const String  EntityId    = "entityId";

            /// <summary>
            /// The property of the digest of its entity.
            /// </summary>
            public const String  Digest      = "digest";

        }

        #endregion

        /// <summary>
        /// The JSON representation of this subject.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomAttestationSubjectSerializer">An optional custom serializer of subjects of attestations.</param>
        public JObject ToJSON(Boolean                                                             Embedded                             = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkAttestationSubject>?  CustomAttestationSubjectSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,     JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.EntityType,  EntityType),
                                 new JProperty(PropertyKeys.EntityId,    EntityId),
                                 new JProperty(PropertyKeys.Digest,      Digest)

                       );

            return CustomAttestationSubjectSerializer is not null
                       ? CustomAttestationSubjectSerializer(this, json)
                       : json;

        }

    }


    /// <summary>
    /// An attestation: a signed statement that a version is the one its signer has, at a point in time, with the
    /// effectiveTree ETag of one hash algorithm (SPECIFICATION.md, section 8.5). Unlike a commit signature, it does not
    /// depend on the size of the change set: a proof of a charging station (RoamingNetworkProof) is checked against it.
    /// </summary>
    public sealed class RoamingNetworkAttestation
    {

        #region Data

        /// <summary>
        /// The profile of the signature of an attestation.
        /// </summary>
        public const String SigningProfile  = "wwcp-poi-attestation-v1";

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext   = "https://open.charging.cloud/contexts/wwcp+json/roamingNetworkAttestation";

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
            public const String  Context           = "@context";

            /// <summary>
            /// The property of the identification of its roaming network.
            /// </summary>
            public const String  RoamingNetworkId  = "roamingNetworkId";

            /// <summary>
            /// The property of the identification of the commit of the attested version.
            /// </summary>
            public const String  CommitId          = "commitId";

            /// <summary>
            /// The property of the revision of the commit.
            /// </summary>
            public const String  Revision          = "revision";

            /// <summary>
            /// The property of the effectiveTree ETag of the attested version.
            /// </summary>
            public const String  ETag              = "eTag";

            /// <summary>
            /// The property of the role of its signer.
            /// </summary>
            public const String  Role              = "role";

            /// <summary>
            /// The property of the optional name of its signer.
            /// </summary>
            public const String  Signer            = "signer";

            /// <summary>
            /// The property of its signing timestamp.
            /// </summary>
            public const String  SignedAt          = "signedAt";

            /// <summary>
            /// The property of the end of its validity.
            /// </summary>
            public const String  ValidUntil        = "validUntil";

            /// <summary>
            /// The property of the one entity it attests instead of the root.
            /// </summary>
            public const String  Subject           = "subject";

            /// <summary>
            /// The property of its signature algorithm.
            /// </summary>
            public const String  Algorithm         = "algorithm";

            /// <summary>
            /// The property of the identification of its key.
            /// </summary>
            public const String  KeyId             = "keyId";

            /// <summary>
            /// The property of its signature value.
            /// </summary>
            public const String  Value             = "value";

            /// <summary>
            /// The property of the profile of its signature.
            /// </summary>
            public const String  Profile           = "profile";

            /// <summary>
            /// The property of the encoding of its signature value.
            /// </summary>
            public const String  Encoding          = "encoding";

        }

        #endregion


        /// <summary>
        /// The roaming network.
        /// </summary>
        public RoamingNetwork_Id                  RoamingNetworkId    { get; }

        /// <summary>
        /// The commit of the attested version.
        /// </summary>
        public RoamingNetworkCommit_Id            CommitId            { get; }

        /// <summary>
        /// The revision of the commit.
        /// </summary>
        public UInt64                             Revision            { get; }

        /// <summary>
        /// The effectiveTree ETag of the attested version, of one hash algorithm.
        /// </summary>
        public ETag                               ETag                { get; }

        /// <summary>
        /// The role of the signer, e.g. roamingNetworkOperator.
        /// </summary>
        public String                             Role                { get; }

        /// <summary>
        /// The optional name of the signer.
        /// </summary>
        public String?                            Signer              { get; }

        /// <summary>
        /// When it was signed, signed as text: yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        public DateTimeOffset                     SignedAt            { get; }

        /// <summary>
        /// Until when it is valid, if it says: a verifier SHOULD refuse it after it.
        /// </summary>
        public DateTimeOffset?                    ValidUntil          { get; }

        /// <summary>
        /// The digest of the one entity it attests instead of the root, if any.
        /// </summary>
        public RoamingNetworkAttestationSubject?  Subject             { get; }

        /// <summary>
        /// The COSE name of the signature algorithm, e.g. ES256.
        /// </summary>
        public String                             Algorithm           { get; }

        /// <summary>
        /// The identification of the key, the @id of a key of a party.
        /// </summary>
        public String                             KeyId               { get; }

        /// <summary>
        /// The signature value, canonical Base64.
        /// </summary>
        public String                             Value               { get; }

        /// <summary>
        /// The profile of the signature.
        /// </summary>
        public String                             Profile             { get; }

        /// <summary>
        /// The encoding of the signature value: base64.
        /// </summary>
        public String                             Encoding            { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create an attestation of its parts; Create(…) signs a new one.
        /// </summary>
        public RoamingNetworkAttestation(RoamingNetwork_Id                  RoamingNetworkId,
                                         RoamingNetworkCommit_Id            CommitId,
                                         UInt64                             Revision,
                                         ETag                               ETag,
                                         String                             Role,
                                         String?                            Signer,
                                         DateTimeOffset                     SignedAt,
                                         DateTimeOffset?                    ValidUntil,
                                         RoamingNetworkAttestationSubject?  Subject,
                                         String                             Algorithm,
                                         String                             KeyId,
                                         String                             Value,
                                         String                             Profile    = SigningProfile,
                                         String                             Encoding   = "base64")
        {

            if (ETag.Format != ETagFormat.EffectiveTree || !ETag.IsValid)
                throw new ArgumentException("An attestation attests an effectiveTree ETag.", nameof(ETag));

            if (String.IsNullOrWhiteSpace(Role))
                throw new ArgumentException("A role is required.", nameof(Role));

            if (String.IsNullOrWhiteSpace(Algorithm) || String.IsNullOrWhiteSpace(KeyId) || String.IsNullOrEmpty(Value))
                throw new ArgumentException("An attestation requires an algorithm, a key identification and a value.");

            if (ValidUntil.HasValue && ValidUntil.Value < SignedAt)
                throw new ArgumentException("An attestation is not valid until before it was signed.", nameof(ValidUntil));

            this.RoamingNetworkId  = RoamingNetworkId;
            this.CommitId          = CommitId;
            this.Revision          = Revision;
            this.ETag              = ETag;
            this.Role              = Role;
            this.Signer            = Signer;
            this.SignedAt          = SignedAt;
            this.ValidUntil        = ValidUntil;
            this.Subject           = Subject;
            this.Algorithm         = Algorithm;
            this.KeyId             = KeyId;
            this.Value             = Value;
            this.Profile           = Profile;
            this.Encoding          = Encoding;

        }

        #endregion


        #region (static) Create(PrivateKey, KeyId, Algorithm, Role, RoamingNetworkId, CommitId, Revision, ETag, Signer = null, SignedAt = null, ValidUntil = null, Subject = null, Deterministic = true)

        /// <summary>
        /// Sign a new attestation of the given version.
        /// </summary>
        /// <param name="PrivateKey">The private key.</param>
        /// <param name="KeyId">The identification of the key, the @id of a key of a party.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="Role">The role of the signer, e.g. roamingNetworkOperator.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="CommitId">The commit of the attested version.</param>
        /// <param name="Revision">The revision of the commit.</param>
        /// <param name="ETag">The effectiveTree ETag of the attested version.</param>
        /// <param name="Signer">The optional name of the signer.</param>
        /// <param name="SignedAt">When it is signed, now otherwise.</param>
        /// <param name="ValidUntil">Until when it is valid, if it says.</param>
        /// <param name="Subject">The digest of the one entity it attests instead of the root, if any.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkAttestation Create(AsymmetricKeyParameter             PrivateKey,
                                                       String                             KeyId,
                                                       COSEAlgorithm                      Algorithm,
                                                       String                             Role,
                                                       RoamingNetwork_Id                  RoamingNetworkId,
                                                       RoamingNetworkCommit_Id            CommitId,
                                                       UInt64                             Revision,
                                                       ETag                               ETag,
                                                       String?                            Signer          = null,
                                                       DateTimeOffset?                    SignedAt        = null,
                                                       DateTimeOffset?                    ValidUntil      = null,
                                                       RoamingNetworkAttestationSubject?  Subject         = null,
                                                       Boolean                            Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(PrivateKey);

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            // Without a value yet, only to get the signed fields exactly as they will be written.
            var unsigned = new RoamingNetworkAttestation(RoamingNetworkId,
                                                         CommitId,
                                                         Revision,
                                                         ETag,
                                                         Role,
                                                         Signer,
                                                         Truncated(SignedAt ?? Timestamp.Now),
                                                         ValidUntil.HasValue ? Truncated(ValidUntil.Value) : null,
                                                         Subject,
                                                         Algorithm.Name,
                                                         KeyId,
                                                         "-");

            var value = Algorithm.Sign(unsigned.GetSigningBytes(), PrivateKey, Deterministic: Deterministic);

            return new RoamingNetworkAttestation(unsigned.RoamingNetworkId,
                                                 unsigned.CommitId,
                                                 unsigned.Revision,
                                                 unsigned.ETag,
                                                 unsigned.Role,
                                                 unsigned.Signer,
                                                 unsigned.SignedAt,
                                                 unsigned.ValidUntil,
                                                 unsigned.Subject,
                                                 unsigned.Algorithm,
                                                 unsigned.KeyId,
                                                 Convert.ToBase64String(value));

        }

        #endregion

        #region IsValidAt(Timestamp)

        /// <summary>
        /// Whether it is valid at the given time: not after its validUntil, when it has one.
        /// </summary>
        public Boolean IsValidAt(DateTimeOffset Timestamp)

            => !ValidUntil.HasValue || Timestamp <= ValidUntil.Value;

        #endregion

        #region VerifySignature(PublicKey, ExpectedKeyId, out ErrorResponse)

        /// <summary>
        /// Verify the signature of this attestation with a trusted public key and its expected key identification.
        /// Unknown profiles, noncanonical Base64, mismatched keys and malformed inputs return false.
        /// </summary>
        /// <param name="PublicKey">The trusted public key.</param>
        /// <param name="ExpectedKeyId">The key identification of the trusted public key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(AsymmetricKeyParameter            PublicKey,
                                       String                            ExpectedKeyId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                ArgumentNullException.ThrowIfNull(PublicKey);

                if (PublicKey.IsPrivate)
                    throw new ArgumentException("Verification requires a public asymmetric key.", nameof(PublicKey));

                if (Profile != SigningProfile || Encoding != "base64")
                    throw new ArgumentException("Unsupported attestation profile or encoding.");

                if (KeyId != ExpectedKeyId)
                    throw new ArgumentException("The key ID of the attestation does not match the trusted key ID.");

                if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || algorithm.Name != Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");

                var value = Convert.FromBase64String(Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Value)
                    throw new ArgumentException("The value must contain nonempty canonical standard Base64.");

                return algorithm.Verify(GetSigningBytes(), value, PublicKey, out ErrorResponse);

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
        /// The signature input of this attestation, as Styx canonical JSON: its signed fields, without its value.
        /// </summary>
        public Byte[] GetSigningBytes()
        {

            if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || !algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.");

            var json = Content();

            json.AddFirst(new JProperty(PropertyKeys.Encoding,   "base64"));
            json.AddFirst(new JProperty(PropertyKeys.KeyId,      KeyId));
            json.AddFirst(new JProperty(PropertyKeys.Algorithm,  Algorithm));
            json.AddFirst(new JProperty(PropertyKeys.Profile,    SigningProfile));

            return CanonicalJSON.ToUTF8Bytes(json);

        }

        #endregion


        #region (static) TryParse(Text, out Attestation, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON text of an attestation. Dates stay text, so signed values keep their exact representation.
        /// </summary>
        public static Boolean TryParse(String                                               Text,
                                       [NotNullWhen(true)]  out RoamingNetworkAttestation?  Attestation,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            Attestation = null;

            try
            {

                if (POIJSON.Read(Text) is not JObject json)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryParse(json, out Attestation, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(JSON, out Attestation, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an attestation. Its signature is not verified here.
        /// </summary>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkAttestation?  Attestation,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            Attestation = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the attestation must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatory(PropertyKeys.RoamingNetworkId,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!RoamingNetworkCommit_Id.TryParse(Text(JSON, PropertyKeys.CommitId) ?? "", out var commitId))
                {
                    ErrorResponse = "Missing or invalid commit identification!";
                    return false;
                }

                if (JSON[PropertyKeys.Revision] is not JValue { Type: JTokenType.Integer } revisionJSON ||
                    revisionJSON.Value<Decimal>() < 0 || revisionJSON.Value<Decimal>() > UInt64.MaxValue)
                {
                    ErrorResponse = "Missing or invalid revision!";
                    return false;
                }

                if (JSON[PropertyKeys.ETag] is not JArray eTagJSON)
                {
                    ErrorResponse = "The ETag must be an effectiveTree ETag in hex!";
                    return false;
                }

                var eTag = ETag.ParseHex(eTagJSON);

                if (eTag.Format != ETagFormat.EffectiveTree)
                {
                    ErrorResponse = "The ETag must be an effectiveTree ETag in hex!";
                    return false;
                }

                if (Text(JSON, PropertyKeys.Role)      is not String role      || role.     Length == 0 ||
                    Text(JSON, PropertyKeys.Algorithm) is not String algorithm || algorithm.Length == 0 ||
                    Text(JSON, PropertyKeys.KeyId)     is not String keyId     || keyId.    Length == 0 ||
                    Text(JSON, PropertyKeys.Value)     is not String value     || value.    Length == 0 ||
                    Text(JSON, PropertyKeys.Profile)   is not String profile   ||
                    Text(JSON, PropertyKeys.Encoding)  is not String encoding)
                {
                    ErrorResponse = "An attestation requires a role, an algorithm, a key identification, a value, a profile and an encoding!";
                    return false;
                }

                String? signer = null;

                if (JSON[PropertyKeys.Signer] is JToken signerJSON)
                {

                    if (signerJSON.Type != JTokenType.String)
                    {
                        ErrorResponse = "The signer must be a text!";
                        return false;
                    }

                    signer = signerJSON.Value<String>();

                }

                if (!TryParseTimestamp(JSON, PropertyKeys.SignedAt, out var signedAt) || !signedAt.HasValue)
                {
                    ErrorResponse = "The signing timestamp must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                if (!TryParseTimestamp(JSON, PropertyKeys.ValidUntil, out var validUntil))
                {
                    ErrorResponse = "The end of the validity must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                RoamingNetworkAttestationSubject? subject = null;

                if (JSON[PropertyKeys.Subject] is JToken subjectJSON)
                {

                    // { entityType, entityId, digest }, and its JSON-LD context, if it names one.
                    if (subjectJSON is not JObject subjectObject ||
                        subjectObject.Count != (Text(subjectObject, RoamingNetworkAttestationSubject.PropertyKeys.Context) == RoamingNetworkAttestationSubject.JSONLDContext ? 4 : 3) ||
                        Text(subjectObject, RoamingNetworkAttestationSubject.PropertyKeys.EntityType) is not String entityType || entityType.Length == 0 ||
                        Text(subjectObject, RoamingNetworkAttestationSubject.PropertyKeys.EntityId)   is not String entityId   || entityId.  Length == 0 ||
                        Text(subjectObject, RoamingNetworkAttestationSubject.PropertyKeys.Digest)     is not String digest     ||
                        digest.Length != 2 * ETag.DigestLength(eTag.HashAlgorithm) ||
                        digest.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0)
                    {
                        ErrorResponse = "The subject must be { entityType, entityId, digest }, the digest in lowercase hex of the hash algorithm of the ETag!";
                        return false;
                    }

                    subject = new RoamingNetworkAttestationSubject(entityType, entityId, digest);

                }

                if (validUntil.HasValue && validUntil.Value < signedAt.Value)
                {
                    ErrorResponse = "An attestation is not valid until before it was signed!";
                    return false;
                }

                Attestation    = new RoamingNetworkAttestation(roamingNetworkId,
                                                               commitId,
                                                               revisionJSON.Value<UInt64>(),
                                                               eTag,
                                                               role,
                                                               signer,
                                                               signedAt.Value,
                                                               validUntil,
                                                               subject,
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
                Attestation    = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out Attestation, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of an attestation: the keys and values of its JSON representation.
        /// </summary>
        public static Boolean TryParse(CBORValue                                            CBOR,
                                       [NotNullWhen(true)]  out RoamingNetworkAttestation?  Attestation,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            Attestation = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map || CBORJSON.ToJSON(CBOR) is not JObject json)
                {
                    ErrorResponse = "The CBOR representation of an attestation must be a map with text keys!";
                    return false;
                }

                return TryParse(json, out Attestation, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomAttestationSerializer = null)

        /// <summary>
        /// Return the JSON representation of this attestation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomAttestationSerializer">An optional custom serializer of attestations.</param>
        public JObject ToJSON(Boolean                                                      Embedded                      = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkAttestation>?  CustomAttestationSerializer   = null)
        {

            var json = Content();

            if (!Embedded)
                json.AddFirst(new JProperty(PropertyKeys.Context, JSONLDContext));

            json.Add(PropertyKeys.Algorithm,  Algorithm);
            json.Add(PropertyKeys.KeyId,      KeyId);
            json.Add(PropertyKeys.Value,      Value);
            json.Add(PropertyKeys.Profile,    Profile);
            json.Add(PropertyKeys.Encoding,   Encoding);

            return CustomAttestationSerializer is not null
                       ? CustomAttestationSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomAttestationSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this attestation: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomAttestationSerializer">An optional custom serializer of attestations.</param>
        public CBORValue ToCBOR(Boolean                                                   Embedded                      = false,
                                CustomCBORSerializerDelegate<RoamingNetworkAttestation>?  CustomAttestationSerializer   = null)
        {

            var cbor = new CBORMap();

            if (!Embedded)
                cbor.Add(CBORValue.FromText(PropertyKeys.Context),           CBORValue.FromText(JSONLDContext));

            cbor.Add(CBORValue.FromText(PropertyKeys.RoamingNetworkId),      CBORValue.FromText(RoamingNetworkId.ToString()));
            cbor.Add(CBORValue.FromText(PropertyKeys.CommitId),              CBORValue.FromText(CommitId.ToString()));
            cbor.Add(CBORValue.FromText(PropertyKeys.Revision),              CBORValue.FromUInt64(Revision));

            // The ETag as in the JSON representation: an array of texts, its digest in hex.
            cbor.Add(CBORValue.FromText(PropertyKeys.ETag),                  CBORJSON.ToCBOR(ETag.ToJSON()));
            cbor.Add(CBORValue.FromText(PropertyKeys.Role),                  CBORValue.FromText(Role));

            if (Signer is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Signer),            CBORValue.FromText(Signer));

            cbor.Add(CBORValue.FromText(PropertyKeys.SignedAt),              CBORValue.FromText(SignedAt.ToISO8601()));

            if (ValidUntil.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.ValidUntil),        CBORValue.FromText(ValidUntil.Value.ToISO8601()));

            if (Subject is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Subject),           new CBORMap {
                                                                                 { CBORValue.FromText(RoamingNetworkAttestationSubject.PropertyKeys.EntityType),  CBORValue.FromText(Subject.EntityType) },
                                                                                 { CBORValue.FromText(RoamingNetworkAttestationSubject.PropertyKeys.EntityId),    CBORValue.FromText(Subject.EntityId) },
                                                                                 { CBORValue.FromText(RoamingNetworkAttestationSubject.PropertyKeys.Digest),      CBORValue.FromText(Subject.Digest) }
                                                                             });

            cbor.Add(CBORValue.FromText(PropertyKeys.Algorithm),             CBORValue.FromText(Algorithm));
            cbor.Add(CBORValue.FromText(PropertyKeys.KeyId),                 CBORValue.FromText(KeyId));
            cbor.Add(CBORValue.FromText(PropertyKeys.Value),                 CBORValue.FromText(Value));
            cbor.Add(CBORValue.FromText(PropertyKeys.Profile),               CBORValue.FromText(Profile));
            cbor.Add(CBORValue.FromText(PropertyKeys.Encoding),              CBORValue.FromText(Encoding));

            return CustomAttestationSerializer is not null
                       ? CustomAttestationSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region (private) Content()

        /// <summary>
        /// What this attestation states and who states it, without its signature.
        /// </summary>
        private JObject Content()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString()),
                           new JProperty(PropertyKeys.CommitId,          CommitId.ToString()),
                           new JProperty(PropertyKeys.Revision,          Revision),
                           new JProperty(PropertyKeys.ETag,              ETag.ToJSON()),
                           new JProperty(PropertyKeys.Role,              Role)
                       );

            if (Signer is not null)
                json.Add(PropertyKeys.Signer,      Signer);

            json.Add(PropertyKeys.SignedAt,        SignedAt.ToISO8601());

            if (ValidUntil.HasValue)
                json.Add(PropertyKeys.ValidUntil,  ValidUntil.Value.ToISO8601());

            if (Subject is not null)
                json.Add(PropertyKeys.Subject,     Subject.ToJSON(Embedded: true));

            return json;

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
        /// An optional timestamp, written exactly as yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        private static Boolean TryParseTimestamp(JObject              JSON,
                                                 String               PropertyName,
                                                 out DateTimeOffset?  Timestamp)
        {

            Timestamp = null;

            if (JSON[PropertyName] is null)
                return true;

            if (Text(JSON, PropertyName) is not String text ||
                !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp) ||
                timestamp.ToISO8601() != text)
            {
                return false;
            }

            Timestamp = timestamp;
            return true;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"Attestation of {ETag} of commit {CommitId} by {Role}{(Signer is not null ? $" {Signer}" : "")}{(ValidUntil.HasValue ? $", valid until {ValidUntil.Value.ToISO8601()}" : "")}";

        #endregion

    }

}
