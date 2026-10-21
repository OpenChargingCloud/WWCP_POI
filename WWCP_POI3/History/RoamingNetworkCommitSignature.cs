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
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetworkCommitSignature";

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
            /// The property of the profile of its signature input.
            /// </summary>
            public const String  Profile     = "profile";

            /// <summary>
            /// The property of the encoding of its signature value.
            /// </summary>
            public const String  Encoding    = "encoding";

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

        /// <summary>
        /// The versioned profile of the signature input.
        /// </summary>
        public String          Profile      { get; }

        /// <summary>
        /// The encoding of the signature bytes: base64.
        /// </summary>
        public String          Encoding     { get; }

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
        /// <param name="Profile">The versioned profile of the signature input.</param>
        /// <param name="Encoding">The encoding of the signature bytes.</param>
        public RoamingNetworkCommitSignature(String          Role,
                                             String?         Signer,
                                             DateTimeOffset  SignedAt,
                                             String          Algorithm,
                                             String          KeyId,
                                             String          Value,
                                             String          Profile    = RoamingNetworkCommit.SigningProfile,
                                             String          Encoding   = "base64")
        {

            this.Role       = Required(Role,      nameof(Role));
            this.Signer     = Signer is not null ? Required(Signer, nameof(Signer)) : null;
            this.SignedAt   = DateTimeOffset.FromUnixTimeMilliseconds(SignedAt.ToUnixTimeMilliseconds());
            this.Algorithm  = Required(Algorithm, nameof(Algorithm));
            this.KeyId      = Required(KeyId,     nameof(KeyId));
            this.Value      = Required(Value,     nameof(Value));
            this.Profile    = Required(Profile,   nameof(Profile));
            this.Encoding   = Required(Encoding,  nameof(Encoding));

        }

        #endregion


        #region TryParse (JSON, out Signature, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a commit signature.
        /// Its signedAt must be written as this library writes it, e.g. 2026-10-12T12:00:00.000Z,
        /// as it is part of the signed content.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Signature">The parsed signature.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkCommitSignature?  Signature,
                                       [NotNullWhen(false)] out String?                         ErrorResponse)
        {

            Signature = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the commit signature must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatoryText(PropertyKeys.Role,       "signer role",          out var role,       out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.SignedAt,   "signing timestamp",    out var signedAt,   out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.Algorithm,  "signature algorithm",  out var algorithm,  out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.KeyId,      "key identification",   out var keyId,      out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.Value,      "signature value",      out var value,      out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.Profile,    "signature profile",    out var profile,    out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.Encoding,   "signature encoding",   out var encoding,   out ErrorResponse))
                {
                    return false;
                }

                if (!DateTimeOffset.TryParse(signedAt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var signedAtTimestamp) ||
                    signedAtTimestamp.ToISO8601() != signedAt)
                {
                    ErrorResponse = $"The signing timestamp '{signedAt}' must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                String? signer = null;

                if (JSON.ParseOptional(PropertyKeys.Signer, "signer", out String? parsedSigner, out ErrorResponse))
                {

                    if (ErrorResponse is not null)
                        return false;

                    signer = parsedSigner;

                }

                Signature = new RoamingNetworkCommitSignature(
                                role,
                                signer,
                                signedAtTimestamp,
                                algorithm,
                                keyId,
                                value,
                                profile,
                                encoding
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
            json.Add(PropertyKeys.Profile,        Profile);
            json.Add(PropertyKeys.Encoding,       Encoding);

            return CustomCommitSignatureSerializer is not null
                       ? CustomCommitSignatureSerializer(this, json)
                       : json;

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
