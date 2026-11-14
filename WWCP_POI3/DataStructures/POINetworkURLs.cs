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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The URLs of a roaming network, urls (SPECIFICATION.md, section 2): where its copies are served, ~/RNs/{RN_Id} at
    /// every server holding it, e.g. one of each holder of its operator. A property of the roaming network, owned by its
    /// operator, part of its content and so of its ETags and of what its commits sign; the preferred one first, their
    /// order being content. A server serving a copy is not trusted for it: every version is signed. They are for
    /// whoever reads the roaming network to follow, not for a server to follow by itself: https only, without user
    /// information, a query or a fragment, at most MaxCount of them, each at most MaxLength characters.
    /// </summary>
    public static class POINetworkURLs
    {

        #region Data

        /// <summary>
        /// The name of the property of a roaming network.
        /// </summary>
        public const String JSONProperty = "urls";

        /// <summary>
        /// The most URLs a roaming network names.
        /// </summary>
        public const Int32  MaxCount      = 16;

        /// <summary>
        /// The most characters of one of them.
        /// </summary>
        public const Int32  MaxLength     = 512;

        #endregion


        #region ToJSON(URLs)

        /// <summary>
        /// The given URLs as a JSON array of their texts, in their order.
        /// </summary>
        public static JArray ToJSON(IEnumerable<URL> URLs)

            => new (URLs.Select(url => url.ToString()));

        #endregion

        #region ToCBOR(URLs)

        /// <summary>
        /// The given URLs as a CBOR array of their texts, in their order.
        /// </summary>
        public static CBORValue ToCBOR(IEnumerable<URL> URLs)

            => CBORValue.FromArray(URLs.Select(url => CBORValue.FromText(url.ToString())));

        #endregion

        #region TryParse(JSON, RoamingNetworkId, out URLs, out ErrorResponse)

        /// <summary>
        /// Try to parse the given URLs of the given roaming network: absent (null), or an array of at least one and at
        /// most MaxCount absolute https URLs of this roaming network, ~/RNs/{RN_Id}, each at most MaxLength characters,
        /// without user information, a query, a fragment or a trailing slash, none twice.
        /// </summary>
        /// <param name="JSON">The JSON value, or null when the property is absent.</param>
        /// <param name="RoamingNetworkId">The roaming network naming them.</param>
        /// <param name="URLs">The URLs, none when absent.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JToken?                           JSON,
                                       RoamingNetwork_Id                 RoamingNetworkId,
                                       out ImmutableArray<URL>           URLs,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            URLs = [];

            if (JSON is null)
            {
                ErrorResponse = null;
                return true;
            }

            if (JSON is not JArray array)
            {
                ErrorResponse = $"The '{JSONProperty}' of the roaming network '{RoamingNetworkId}' must be a JSON array of URLs!";
                return false;
            }

            if (array.Count == 0)
            {
                ErrorResponse = $"The '{JSONProperty}' of the roaming network '{RoamingNetworkId}' must name at least one URL, or be left out!";
                return false;
            }

            if (array.Count > MaxCount)
            {
                ErrorResponse = $"The '{JSONProperty}' of the roaming network '{RoamingNetworkId}' must name at most {MaxCount} URLs, not {array.Count}!";
                return false;
            }

            var urls = ImmutableArray.CreateBuilder<URL>(array.Count);

            foreach (var item in array)
            {

                if (item.Type != JTokenType.String || !URL.TryParse(item.Value<String>()!, out var url))
                {
                    ErrorResponse = $"The '{JSONProperty}' of the roaming network '{RoamingNetworkId}' must name absolute URLs, not '{item}'!";
                    return false;
                }

                urls.Add(url);

            }

            if (Invalid(urls, RoamingNetworkId) is String error)
            {
                ErrorResponse = error;
                return false;
            }

            URLs           = urls.ToImmutable();
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (internal) Invalid(URLs, RoamingNetworkId)

        /// <summary>
        /// Why the given URLs of the given roaming network are invalid, or null: at most MaxCount, each an https URL of
        /// this roaming network, ~/RNs/{RN_Id}, at most MaxLength characters, without user information, a query, a
        /// fragment or a trailing slash, none twice.
        /// </summary>
        internal static String? Invalid(IReadOnlyCollection<URL>  URLs,
                                        RoamingNetwork_Id         RoamingNetworkId)
        {

            if (URLs.Count > MaxCount)
                return $"The '{JSONProperty}' of the roaming network '{RoamingNetworkId}' must name at most {MaxCount} URLs, not {URLs.Count}!";

            if (URLs.Select(url => url.ToString()).Distinct(StringComparer.Ordinal).Count() != URLs.Count)
                return $"The '{JSONProperty}' of the roaming network '{RoamingNetworkId}' must not name a URL twice!";

            foreach (var url in URLs)
            {

                var text      = url.ToString();

                if (text.Length > MaxLength)
                    return $"A URL of the roaming network '{RoamingNetworkId}' has {text.Length} characters, more than {MaxLength}!";

                // Followed by whoever reads the roaming network: over TLS only, as the documents read there are checked
                // by their signatures, but who asks for them, and for what, is nobody else's.
                if (url.Scheme != URIScheme.https)
                    return $"The URL '{url}' of the roaming network '{RoamingNetworkId}' is no https URL!";

                // No user information: what a version names, everybody reads.
                var authority = text[(text.IndexOf("://", StringComparison.Ordinal) + 3)..];
                authority     = authority[..(authority.IndexOf('/') is >= 0 and var slash ? slash : authority.Length)];

                if (authority.Contains('@'))
                    return $"The URL '{url}' of the roaming network '{RoamingNetworkId}' must not name user information!";

                var path      = url.Path.ToString();
                var segments  = path.Split('/');

                if (url.QueryString is not null || url.Fragment is not null || path.EndsWith('/') ||
                    segments.Length < 2 || segments[^2] != "RNs" ||
                    !RoamingNetwork_Id.TryParse(Uri.UnescapeDataString(segments[^1]), out var urlNetworkId))
                {
                    return $"The URL '{url}' of the roaming network '{RoamingNetworkId}' is not the one of a roaming network, ~/RNs/{{RN_Id}}, without a query, a fragment or a trailing slash!";
                }

                if (urlNetworkId != RoamingNetworkId)
                    return $"The URL '{url}' of the roaming network '{RoamingNetworkId}' is one of the roaming network '{urlNetworkId}'!";

            }

            return null;

        }

        #endregion

    }

}
