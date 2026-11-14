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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The well-known roles of the signers of a commit. Others may be used as well.
    /// </summary>
    public static class RoamingNetworkCommitSignatureRoles
    {

        /// <summary>
        /// The leading server, which made the commit.
        /// </summary>
        public const String Leader                   = "leader";

        /// <summary>
        /// The operator of the roaming network, e.g. one that received the data of a charging station operator and checked it.
        /// </summary>
        public const String RoamingNetworkOperator   = "roamingNetworkOperator";

        /// <summary>
        /// A charging station operator, e.g. confirming that the commit holds its data as it sent it.
        /// </summary>
        public const String ChargingStationOperator  = "chargingStationOperator";

        /// <summary>
        /// An independent third party signing checkpoints it checked against its own history (SPECIFICATION.md, section 10.2).
        /// </summary>
        public const String Witness                  = "witness";


        /// <summary>
        /// Whether the key of the given signature may sign in its role, as far as the given version tells it: in the role
        /// of the roaming network operator a signing key certified for it, in the role of a charging station operator a
        /// signing key of the operator its signer names. Who leads or witnesses no version says: those roles are the
        /// verifier's to judge by the keys it trusts (POI3-ISSUES.md S24).
        /// </summary>
        /// <param name="Signature">A signature of a commit.</param>
        /// <param name="Version">The version of the roaming network the commit leads to.</param>
        public static Boolean KeyMaySignIn(RoamingNetworkCommitSignature  Signature,
                                           RoamingNetwork                 Version)

            => KeyMaySignIn(Signature.Role,
                            Signature.KeyId,
                            Signature.Signer,
                            Version);

        /// <summary>
        /// Whether the key of the given identification may sign in the given role, as far as the given version tells it:
        /// as KeyMaySignIn(Signature, Version), before anything is signed.
        /// </summary>
        /// <param name="Role">The role of the signer.</param>
        /// <param name="KeyId">The identification of its key.</param>
        /// <param name="Signer">The optional name of the signer.</param>
        /// <param name="Version">The version of the roaming network the commit leads to.</param>
        public static Boolean KeyMaySignIn(String          Role,
                                           String          KeyId,
                                           String?         Signer,
                                           RoamingNetwork  Version)

            => Role switch {

                   RoamingNetworkOperator   => Version.Operator?.SigningKeyCertificates.Any(certificate => certificate.SigningKey.Id == KeyId) == true,

                   ChargingStationOperator  => ChargingStationOperator_Id.TryParse(Signer ?? "", out var operatorId) &&
                                               Version.TryGetChargingStationOperatorById(operatorId, out var chargingStationOperator) &&
                                               chargingStationOperator.SigningKeys.Any(key => key.Id == KeyId),

                   _                        => true

               };

    }


    /// <summary>
    /// A signature of a commit: who signed it, in which role and when, and the signature value.
    /// It signs the commit with the signatures of its change set, but without the other signatures of the commit,
    /// so that every party signs on its own, in any order, and later signatures do not change the commit identification.
    /// </summary>
    public sealed record RoamingNetworkCommitSignature
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/commitSignature/v1";

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
            public const String  Context     = "@context";

            /// <summary>
            /// The property of the role of its signer.
            /// </summary>
            public const String  Role        = "role";

            /// <summary>
            /// The property of the optional name of its signer.
            /// </summary>
            public const String  Signer      = "signer";

            /// <summary>
            /// The property of its signing timestamp.
            /// </summary>
            public const String  SignedAt    = "signedAt";

            /// <summary>
            /// The property of its signature algorithm.
            /// </summary>
            public const String  Algorithm   = "algorithm";

            /// <summary>
            /// The property of the identification of its key.
            /// </summary>
            public const String  KeyId       = "keyId";

            /// <summary>
            /// The property of its signature value.
            /// </summary>
            public const String  Value       = "value";

            /// <summary>
            /// The property of the identification of the commit of the signatures of a commit, { commitId, signatures }.
            /// </summary>
            public const String  CommitId    = "commitId";

            /// <summary>
            /// The property of the signatures of the signatures of a commit, { commitId, signatures }.
            /// </summary>
            public const String  Signatures  = "signatures";

        }

        #endregion


        /// <summary>
        /// The role of the signer, e.g. leader or roamingNetworkOperator.
        /// </summary>
        public String          Role         { get; }

        /// <summary>
        /// The optional name of the signer, e.g. the identification of a roaming network operator.
        /// </summary>
        public String?         Signer       { get; }

        /// <summary>
        /// When the commit was signed, to the millisecond.
        /// </summary>
        public DateTimeOffset  SignedAt     { get; }

        /// <summary>
        /// The canonical Styx COSE algorithm name.
        /// </summary>
        public String          Algorithm    { get; }

        /// <summary>
        /// The application identifier used to resolve a trusted public key.
        /// </summary>
        public String          KeyId        { get; }

        /// <summary>
        /// The signature bytes, as canonical standard Base64.
        /// </summary>
        public String          Value        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a commit signature; it is checked cryptographically when verified.
        /// </summary>
        /// <param name="Role">The role of the signer, e.g. leader or roamingNetworkOperator.</param>
        /// <param name="Signer">The optional name of the signer.</param>
        /// <param name="SignedAt">When the commit was signed; kept to the millisecond.</param>
        /// <param name="Algorithm">The canonical Styx COSE algorithm name.</param>
        /// <param name="KeyId">The application identifier used to resolve a trusted public key.</param>
        /// <param name="Value">The signature bytes, as canonical standard Base64.</param>
        public RoamingNetworkCommitSignature(String          Role,
                                             String?         Signer,
                                             DateTimeOffset  SignedAt,
                                             String          Algorithm,
                                             String          KeyId,
                                             String          Value)
        {

            this.Role       = Required(Role,      nameof(Role));
            this.Signer     = Signer is not null ? Required(Signer, nameof(Signer)) : null;
            this.SignedAt   = DateTimeOffset.FromUnixTimeMilliseconds(SignedAt.ToUnixTimeMilliseconds());
            this.Algorithm  = Required(Algorithm, nameof(Algorithm));
            this.KeyId      = Required(KeyId,     nameof(KeyId));
            this.Value      = Required(Value,     nameof(Value));

        }

        #endregion


        #region TryParse (JSON, out Signature, out ErrorResponse, Embedded = false, What = "A commit signature")

        /// <summary>
        /// Try to parse the given JSON representation of a commit signature.
        /// Its signedAt must be written as this library writes it, e.g. 2026-10-12T12:00:00.000Z,
        /// as it is part of the signed content. It names its own properties only, as WWCP_POI.TS reads it: whatever else
        /// it names is signed by nobody, its signer is a text, and embedded in a commit or a checkpoint it names no JSON-LD
        /// context (SPECIFICATION.md, section 5.4). Its role, signedAt, algorithm, keyId and value are trimmed, its signer
        /// is taken as given, and none of them is empty. It is refused in the words of WWCP_POI.TS (parseRoleSignature),
        /// in its order: its properties, then each of them a text, the signedAt a point in time, then none of them empty.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Signature">The parsed signature.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Embedded">Whether it is embedded in a commit or a checkpoint, without its JSON-LD context.</param>
        /// <param name="What">What it is called in an error response, e.g. "A signature of a commit".</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkCommitSignature?  Signature,
                                       [NotNullWhen(false)] out String?                         ErrorResponse,
                                       Boolean                                                  Embedded   = false,
                                       String                                                   What       = "A commit signature")
        {

            Signature = null;

            // "The role of a signature of a commit", as WWCP_POI.TS names it.
            var of = What.Length > 0
                         ? Char.ToLowerInvariant(What[0]) + What[1..]
                         : What;

            try
            {

                foreach (var property in JSON.Properties())
                    if (!(property.Name is PropertyKeys.Role      or PropertyKeys.Signer or PropertyKeys.SignedAt or
                                           PropertyKeys.Algorithm or PropertyKeys.KeyId  or PropertyKeys.Value ||
                          property.Name == PropertyKeys.Context && !Embedded))
                    {
                        ErrorResponse = $"{What} must not name the property '{property.Name}'!";
                        return false;
                    }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of {of} must be '{JSONLDContext}'!";
                    return false;
                }

                if (!TryParseText(JSON, PropertyKeys.Role, of, out var role, out ErrorResponse))
                    return false;

                if (JSON[PropertyKeys.Signer] is JToken signerJSON && signerJSON.Type != JTokenType.String)
                {
                    ErrorResponse = $"The {PropertyKeys.Signer} of {of} must be a text!";
                    return false;
                }

                var signer = JSON[PropertyKeys.Signer]?.Value<String>();

                if (!TryParseText(JSON, PropertyKeys.SignedAt, of, out var signedAt, out ErrorResponse))
                    return false;

                if (!IsWrittenAsTimestamp(signedAt))
                {
                    ErrorResponse = $"The {PropertyKeys.SignedAt} of {of} must be written as yyyy-MM-ddTHH:mm:ss.fffZ, not '{signedAt}'!";
                    return false;
                }

                if (!DateTimeOffset.TryParse(signedAt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var signedAtTimestamp) ||
                    signedAtTimestamp.ToISO8601() != signedAt)
                {
                    ErrorResponse = $"The {PropertyKeys.SignedAt} of {of} '{signedAt}' is no point in time!";
                    return false;
                }

                if (!TryParseText(JSON, PropertyKeys.Algorithm, of, out var algorithm, out ErrorResponse) ||
                    !TryParseText(JSON, PropertyKeys.KeyId,     of, out var keyId,     out ErrorResponse) ||
                    !TryParseText(JSON, PropertyKeys.Value,     of, out var value,     out ErrorResponse))
                {
                    return false;
                }

                // None of them empty, nor whitespace alone: a role, a signer, a key or a value of nothing is refused when read,
                // before anything counts it.
                foreach (var (property, text) in new (String Property, String? Text)[] {
                                                     (PropertyKeys.Role,      role),
                                                     (PropertyKeys.Signer,    signer),
                                                     (PropertyKeys.Algorithm, algorithm),
                                                     (PropertyKeys.KeyId,     keyId),
                                                     (PropertyKeys.Value,     value)
                                                 })
                {
                    if (text is not null && String.IsNullOrWhiteSpace(text))
                    {
                        ErrorResponse = $"The {property} of {of} must not be empty!";
                        return false;
                    }
                }

                Signature = new RoamingNetworkCommitSignature(
                                role,
                                signer,
                                signedAtTimestamp,
                                algorithm,
                                keyId,
                                value
                            );

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                Signature      = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region (private static) TryParseText(JSON, Property, Of, out Text, out ErrorResponse)

        /// <summary>
        /// The given property of a commit signature, a text, trimmed.
        /// </summary>
        private static Boolean TryParseText(JObject                           JSON,
                                            String                            Property,
                                            String                            Of,
                                            [NotNullWhen(true)]  out String?  Text,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (JSON[Property] is JToken { Type: JTokenType.String } token &&
                token.Value<String>() is String text)
            {
                Text           = text.Trim();
                ErrorResponse  = null;
                return true;
            }

            Text           = null;
            ErrorResponse  = $"The {Property} of {Of} must be a text!";
            return false;

        }

        #endregion

        #region (private static) IsWrittenAsTimestamp(Text)

        /// <summary>
        /// Whether the given text is written as yyyy-MM-ddTHH:mm:ss.fffZ, with ASCII digits.
        /// </summary>
        private static Boolean IsWrittenAsTimestamp(String Text)
        {

            const String form = "dddd-dd-ddTdd:dd:dd.dddZ";

            if (Text.Length != form.Length)
                return false;

            for (var i = 0; i < form.Length; i++)
                if (form[i] == 'd' ? !Char.IsAsciiDigit(Text[i]) : Text[i] != form[i])
                    return false;

            return true;

        }

        #endregion

        #region TryParseCommitSignatures(JSON, out CommitId, out Signatures, out ErrorResponse)

        /// <summary>
        /// Try to parse the signatures of a commit, { commitId, signatures: [ … ] },
        /// as the command signCommit takes them and the topic commitSignatures pushes them.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CommitId">The identification of the commit.</param>
        /// <param name="Signatures">The signatures, at least one.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParseCommitSignatures(JObject                                                                 JSON,
                                                       out RoamingNetworkCommit_Id                                             CommitId,
                                                       [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkCommitSignature>?  Signatures,
                                                       [NotNullWhen(false)] out String?                                        ErrorResponse)
        {

            Signatures = null;

            if (!JSON.ParseMandatory(PropertyKeys.CommitId,
                                     "commit identification",
                                     RoamingNetworkCommit_Id.TryParse,
                                     out CommitId,
                                     out ErrorResponse))
            {
                return false;
            }

            if (JSON[PropertyKeys.Signatures] is not JArray signaturesJSON || signaturesJSON.Count == 0)
            {
                ErrorResponse = "The signatures must be a JSON array of at least one signature!";
                return false;
            }

            var signatures = new List<RoamingNetworkCommitSignature>();

            foreach (var signatureJSON in signaturesJSON)
            {

                if (signatureJSON is not JObject signatureObject)
                {
                    ErrorResponse = $"Invalid signature {signatures.Count}: it must be a JSON object!";
                    return false;
                }

                if (!TryParse(signatureObject, out var signature, out var signatureError))
                {
                    ErrorResponse = $"Invalid signature {signatures.Count}: " + signatureError;
                    return false;
                }

                signatures.Add(signature);

            }

            Signatures     = signatures;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region ToJSON(Embedded = false, CustomCommitSignatureSerializer = null)

        /// <summary>
        /// Return a JSON representation of this commit signature.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomCommitSignatureSerializer">An optional custom serializer of commit signatures.</param>
        public JObject ToJSON(Boolean                                                          Embedded                          = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkCommitSignature>?  CustomCommitSignatureSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,    JSONLDContext);

            json.Add(PropertyKeys.Role,           Role);

            if (Signer is not null)
                json.Add(PropertyKeys.Signer,     Signer);

            json.Add(PropertyKeys.SignedAt,       SignedAt.ToISO8601());
            json.Add(PropertyKeys.Algorithm,      Algorithm);
            json.Add(PropertyKeys.KeyId,          KeyId);
            json.Add(PropertyKeys.Value,          Value);

            return CustomCommitSignatureSerializer is not null
                       ? CustomCommitSignatureSerializer(this, json)
                       : json;

        }

        #endregion


        #region SignsTheSameAs(Other)

        /// <summary>
        /// Whether the given signature signs what this one signs, by the same key, whatever its value: an ECDSA signature
        /// has a twin with the other S anybody can make from it, and a randomized signature another value each time
        /// (POI3-ISSUES.md S11). A commit or checkpoint keeps such signatures once.
        /// </summary>
        public Boolean SignsTheSameAs(RoamingNetworkCommitSignature Other)

            => Role      == Other.Role      &&
               Signer    == Other.Signer    &&
               SignedAt  == Other.SignedAt  &&
               Algorithm == Other.Algorithm &&
               KeyId     == Other.KeyId;

        #endregion

        #region IsOfTheSameKeyAndRoleAs(Other)

        /// <summary>
        /// Whether the given signature is one of the same key in the same role: a server adding signatures to a commit or
        /// checkpoint it holds takes one of them, the first, whatever time the others name, so a trusted key adds no
        /// signatures without bound (POI3-ISSUES.md S24). The twin of an ECDSA signature is one of them as well
        /// (SignsTheSameAs). A commit or checkpoint read keeps the signatures it was given, none dropped unseen: they are
        /// checked where it is verified, a quorum counting each holder once.
        /// </summary>
        public Boolean IsOfTheSameKeyAndRoleAs(RoamingNetworkCommitSignature Other)

            => KeyId == Other.KeyId &&
               Role  == Other.Role;

        #endregion

        #region (static) NotYetIn(Held, Given)

        /// <summary>
        /// The given signatures of a key in a role none of the held ones has, each the first of its key and role
        /// (IsOfTheSameKeyAndRoleAs): what a server adds to a commit or checkpoint it holds, not what it reads.
        /// </summary>
        public static RoamingNetworkCommitSignature[] NotYetIn(IEnumerable<RoamingNetworkCommitSignature>  Held,
                                                               IEnumerable<RoamingNetworkCommitSignature>  Given)
        {

            var kept = new List<RoamingNetworkCommitSignature>(Held);
            var news = new List<RoamingNetworkCommitSignature>();

            foreach (var signature in Given)
                if (!kept.Any(signature.IsOfTheSameKeyAndRoleAs))
                {
                    kept.Add(signature);
                    news.Add(signature);
                }

            return [ .. news ];

        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Role}{(Signer is not null ? $" {Signer}" : "")} with '{KeyId}' at {SignedAt.ToISO8601()}";

        #endregion

        #region (private static) Required(Value, ParameterName)

        private static String Required(String  Value,
                                       String  ParameterName)

            => !String.IsNullOrWhiteSpace(Value)
                   ? Value
                   : throw new ArgumentException("Value must not be empty.", ParameterName);

        #endregion

    }

}
