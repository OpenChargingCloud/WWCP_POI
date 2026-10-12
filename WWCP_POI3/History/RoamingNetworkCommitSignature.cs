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

    }


    /// <summary>
    /// A signature of a commit: who signed it, in which role and when, and the signature value.
    /// It signs the commit with the signatures of its change set, but without the other signatures of the commit,
    /// so that every party signs on its own, in any order, and later signatures do not change the commit identification.
    /// </summary>
    public sealed record RoamingNetworkCommitSignature
    {

        #region Properties

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

                if (!JSON.ParseMandatoryText("role",       "signer role",          out var role,       out ErrorResponse) ||
                    !JSON.ParseMandatoryText("signedAt",   "signing timestamp",    out var signedAt,   out ErrorResponse) ||
                    !JSON.ParseMandatoryText("algorithm",  "signature algorithm",  out var algorithm,  out ErrorResponse) ||
                    !JSON.ParseMandatoryText("keyId",      "key identification",   out var keyId,      out ErrorResponse) ||
                    !JSON.ParseMandatoryText("value",      "signature value",      out var value,      out ErrorResponse) ||
                    !JSON.ParseMandatoryText("profile",    "signature profile",    out var profile,    out ErrorResponse) ||
                    !JSON.ParseMandatoryText("encoding",   "signature encoding",   out var encoding,   out ErrorResponse))
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

                if (JSON.ParseOptional("signer", "signer", out String? parsedSigner, out ErrorResponse))
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

            if (!JSON.ParseMandatory("commitId",
                                     "commit identification",
                                     RoamingNetworkCommit_Id.TryParse,
                                     out CommitId,
                                     out ErrorResponse))
            {
                return false;
            }

            if (JSON["signatures"] is not JArray signaturesJSON || signaturesJSON.Count == 0)
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

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this commit signature.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("role",       Role)
                       );

            if (Signer is not null)
                json.Add("signer",     Signer);

            json.Add("signedAt",       SignedAt.ToISO8601());
            json.Add("algorithm",      Algorithm);
            json.Add("keyId",          KeyId);
            json.Add("value",          Value);
            json.Add("profile",        Profile);
            json.Add("encoding",       Encoding);

            return json;

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
