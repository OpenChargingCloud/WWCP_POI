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

using System.Text.Json.Serialization;

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An immutable signature envelope. The built-in profile binds its algorithm, key ID,
    /// profile and encoding together with the complete batch; Value carries Base64 signature bytes.
    /// </summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed record RoamingNetworkChangeSetSignature
    {

        /// <summary>
        /// Create an immutable signature envelope; cryptographic validation occurs during verification.
        /// </summary>
        [JsonConstructor]
        public RoamingNetworkChangeSetSignature(String algorithm,
                                               String keyId,
                                               String value,
                                               String profile = RoamingNetworkChangeSet.SigningProfile,
                                               String encoding = "base64")
        {
            Algorithm = Required(algorithm, nameof(algorithm));
            KeyId     = Required(keyId, nameof(keyId));
            Value     = Required(value, nameof(value));
            Profile   = Required(profile, nameof(profile));
            Encoding  = Required(encoding, nameof(encoding));
        }

        /// <summary>
        /// The canonical Styx COSE algorithm name for the built-in signing profile.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String Algorithm { get; private init; }

        /// <summary>
        /// The application identifier used to resolve a trusted public key.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String KeyId { get; private init; }

        /// <summary>
        /// Signature bytes encoded as canonical standard Base64 in the built-in profile.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String Value { get; private init; }

        /// <summary>
        /// The versioned signature-input profile.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String Profile { get; private init; }

        /// <summary>
        /// The signature byte encoding, base64 for the built-in profile.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String Encoding { get; private init; }

        private static String Required(String value, String parameterName)
            => !String.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value must not be empty.", parameterName);

    }
}
