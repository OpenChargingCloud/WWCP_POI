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
    /// An immutable peer envelope used by the batch signing profile.
    /// Profile selects the signed content; Value carries Base64 signature bytes.
    /// </summary>
    public sealed record RoamingNetworkChangeSetSignature
    {

        #region Properties

        /// <summary>
        /// The canonical Styx COSE algorithm name for the built-in signing profile.
        /// </summary>
        public String  Algorithm    { get; }

        /// <summary>
        /// The application identifier used to resolve a trusted public key.
        /// </summary>
        public String  KeyId        { get; }

        /// <summary>
        /// Signature bytes encoded as canonical standard Base64 in the built-in profile.
        /// </summary>
        public String  Value        { get; }

        /// <summary>
        /// The versioned signature-input profile.
        /// </summary>
        public String  Profile      { get; }

        /// <summary>
        /// The signature byte encoding, base64 for the built-in profile.
        /// </summary>
        public String  Encoding     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create an immutable signature envelope; cryptographic validation occurs during verification.
        /// </summary>
        /// <param name="Algorithm">The canonical Styx COSE algorithm name.</param>
        /// <param name="KeyId">The application identifier used to resolve a trusted public key.</param>
        /// <param name="Value">The encoded signature bytes.</param>
        /// <param name="Profile">The versioned signature-input profile.</param>
        /// <param name="Encoding">The signature byte encoding.</param>
        public RoamingNetworkChangeSetSignature(String  Algorithm,
                                                String  KeyId,
                                                String  Value,
                                                String  Profile    = RoamingNetworkChangeSet.SigningProfile,
                                                String  Encoding   = "base64")
        {

            this.Algorithm  = Required(Algorithm, nameof(Algorithm));
            this.KeyId      = Required(KeyId,     nameof(KeyId));
            this.Value      = Required(Value,     nameof(Value));
            this.Profile    = Required(Profile,   nameof(Profile));
            this.Encoding   = Required(Encoding,  nameof(Encoding));

        }

        #endregion


        #region TryParse (JSON, out Signature, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a change set signature.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Signature">The parsed signature.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                                     JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkChangeSetSignature?  Signature,
                                       [NotNullWhen(false)] out String?                            ErrorResponse)
        {

            Signature = null;

            try
            {

                if (!JSON.ParseMandatoryText("algorithm", "signature algorithm", out var algorithm, out ErrorResponse) ||
                    !JSON.ParseMandatoryText("keyId",     "key identification",  out var keyId,     out ErrorResponse) ||
                    !JSON.ParseMandatoryText("value",     "signature value",     out var value,     out ErrorResponse) ||
                    !JSON.ParseMandatoryText("profile",   "signature profile",   out var profile,   out ErrorResponse) ||
                    !JSON.ParseMandatoryText("encoding",  "signature encoding",  out var encoding,  out ErrorResponse))
                {
                    return false;
                }

                Signature = new RoamingNetworkChangeSetSignature(
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

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this change set signature.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty("algorithm",  Algorithm),
                   new JProperty("keyId",      KeyId),
                   new JProperty("value",      Value),
                   new JProperty("profile",    Profile),
                   new JProperty("encoding",   Encoding)
               );

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
