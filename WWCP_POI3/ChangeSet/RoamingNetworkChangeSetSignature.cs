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
    /// One signature of a change set: the algorithm, the key and the Base64 signature bytes. What it signs, and in which
    /// version, the JSON-LD context of the change set names in its signature input (RoamingNetworkChangeSet.JSONLDContext).
    /// </summary>
    public sealed record RoamingNetworkChangeSetSignature
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/changeSetSignature/v1";

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
            /// The property of its signature algorithm.
            /// </summary>
            public const String  Algorithm   = "algorithm";

            /// <summary>
            /// The property of its key identification.
            /// </summary>
            public const String  KeyId       = "keyId";

            /// <summary>
            /// The property of its signature value.
            /// </summary>
            public const String  Value       = "value";

        }

        #endregion


        /// <summary>
        /// The canonical Styx COSE algorithm name.
        /// </summary>
        public String  Algorithm    { get; }

        /// <summary>
        /// The application identifier used to resolve a trusted public key.
        /// </summary>
        public String  KeyId        { get; }

        /// <summary>
        /// The signature bytes, as canonical standard Base64.
        /// </summary>
        public String  Value        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create an immutable signature envelope; cryptographic validation occurs during verification.
        /// </summary>
        /// <param name="Algorithm">The canonical Styx COSE algorithm name.</param>
        /// <param name="KeyId">The application identifier used to resolve a trusted public key.</param>
        /// <param name="Value">The signature bytes, as canonical standard Base64.</param>
        public RoamingNetworkChangeSetSignature(String  Algorithm,
                                                String  KeyId,
                                                String  Value)
        {

            this.Algorithm  = Required(Algorithm, nameof(Algorithm));
            this.KeyId      = Required(KeyId,     nameof(KeyId));
            this.Value      = Required(Value,     nameof(Value));

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

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the change set signature must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatoryText(PropertyKeys.Algorithm, "signature algorithm", out var algorithm, out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.KeyId,     "key identification",  out var keyId,     out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.Value,     "signature value",     out var value,     out ErrorResponse))
                {
                    return false;
                }

                Signature = new RoamingNetworkChangeSetSignature(
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

        #region ToJSON(Embedded = false, CustomRoamingNetworkChangeSetSignatureSerializer = null)

        /// <summary>
        /// Return a JSON representation of this change set signature.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomRoamingNetworkChangeSetSignatureSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                                             Embedded                                           = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkChangeSetSignature>?  CustomRoamingNetworkChangeSetSignatureSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,    JSONLDContext);

            json.Add(PropertyKeys.Algorithm,      Algorithm);
            json.Add(PropertyKeys.KeyId,          KeyId);
            json.Add(PropertyKeys.Value,          Value);

            return CustomRoamingNetworkChangeSetSignatureSerializer is not null
                       ? CustomRoamingNetworkChangeSetSignatureSerializer(this, json)
                       : json;

        }

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
