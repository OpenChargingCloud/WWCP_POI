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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The hash algorithms of the effective tree of a roaming network, treeHashAlgorithms (SPECIFICATION.md, section 8.3):
    /// a property of the roaming network, owned by its operator, part of its content. A version has one effectiveTree ETag
    /// per algorithm, in their order; naming two lets clients move from the one to the other.
    /// </summary>
    public static class POITreeHashAlgorithms
    {

        #region Data

        /// <summary>
        /// The name of the property of a roaming network.
        /// </summary>
        public const String JSONProperty = "treeHashAlgorithms";

        /// <summary>
        /// The hash algorithms of a roaming network naming none: SHA-256.
        /// </summary>
        public static readonly ImmutableArray<ETagHashAlgorithm> Default = [ ETagHashAlgorithm.SHA256 ];

        #endregion


        #region ToJSON(HashAlgorithms)

        /// <summary>
        /// The given hash algorithms as a JSON array of their names, in their order.
        /// </summary>
        public static JArray ToJSON(IEnumerable<ETagHashAlgorithm> HashAlgorithms)

            => new (HashAlgorithms.Select(hashAlgorithm => hashAlgorithm.ToText()));

        #endregion

        #region TryParse(JSON, out HashAlgorithms, out ErrorResponse)

        /// <summary>
        /// Try to parse the given tree hash algorithms: absent (null), or an array of at least one name of a hash algorithm,
        /// none twice.
        /// </summary>
        /// <param name="JSON">The JSON value, or null when the property is absent.</param>
        /// <param name="HashAlgorithms">The hash algorithms, or null when absent.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JToken?                           JSON,
                                       out ImmutableArray<ETagHashAlgorithm>?  HashAlgorithms,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            HashAlgorithms = null;

            if (JSON is null)
            {
                ErrorResponse = null;
                return true;
            }

            if (JSON is not JArray array)
            {
                ErrorResponse = $"The '{JSONProperty}' must be a JSON array of hash algorithms!";
                return false;
            }

            var hashAlgorithms = ImmutableArray.CreateBuilder<ETagHashAlgorithm>(array.Count);

            foreach (var item in array)
            {

                if (item.Type != JTokenType.String || !ETagHashAlgorithmExtensions.TryParse(item.Value<String>(), out var hashAlgorithm))
                {
                    ErrorResponse = $"Unknown tree hash algorithm '{item}': sha256, sha384 or sha512!";
                    return false;
                }

                hashAlgorithms.Add(hashAlgorithm);

            }

            if (Invalid(hashAlgorithms) is String error)
            {
                ErrorResponse = error;
                return false;
            }

            HashAlgorithms  = hashAlgorithms.ToImmutable();
            ErrorResponse   = null;
            return true;

        }

        #endregion

        #region (internal) Invalid(HashAlgorithms)

        /// <summary>
        /// Why the given tree hash algorithms are invalid, or null: at least one, none twice.
        /// </summary>
        internal static String? Invalid(IReadOnlyCollection<ETagHashAlgorithm> HashAlgorithms)

            => HashAlgorithms.Count == 0
                   ? $"The '{JSONProperty}' must name at least one hash algorithm!"
                   : HashAlgorithms.Distinct().Count() != HashAlgorithms.Count
                         ? $"The '{JSONProperty}' must not name a hash algorithm twice!"
                         : null;

        #endregion

    }

}
