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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What an app trusts of a roaming network (SPECIFICATION.md, section 8.6): where its proofs come from and the @ids of
    /// the identity keys of its operator, their SHA-256 thumbprints. Written as one URI for a QR code, e.g. on the website
    /// of the roaming network operator: https://hub.example/poi/RNs/rn1?identityKeys=a1…,c3…. Only the thumbprints are
    /// trusted; the keys themselves come with every proof, in the key statement and the operator it gives.
    /// </summary>
    public sealed class RoamingNetworkTrustAnchor
    {

        #region Data

        /// <summary>
        /// The query parameter naming the @ids of the identity keys, separated by commas.
        /// </summary>
        public const String IdentityKeysParameter = "identityKeys";

        #endregion

        #region Properties

        /// <summary>
        /// The URI of the roaming network, ~/RNs/{RN_Id}, below which its proofs are served.
        /// </summary>
        public Uri                     RoamingNetworkURI    { get; }

        /// <summary>
        /// The roaming network, the last segment of its URI.
        /// </summary>
        public RoamingNetwork_Id       RoamingNetworkId     { get; }

        /// <summary>
        /// The @ids of the identity keys of the roaming network operator: their SHA-256 thumbprints, lowercase hex.
        /// </summary>
        public ImmutableArray<String>  IdentityKeyIds       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a trust anchor of the given URI of a roaming network, ~/RNs/{RN_Id}, and the @ids of the identity keys of
        /// its operator.
        /// </summary>
        public RoamingNetworkTrustAnchor(Uri                  RoamingNetworkURI,
                                         IEnumerable<String>  IdentityKeyIds)
        {

            if (!TryCheck(RoamingNetworkURI, IdentityKeyIds, out var roamingNetworkId, out var identityKeyIds, out var error))
                throw new ArgumentException(error);

            this.RoamingNetworkURI  = new Uri(RoamingNetworkURI.GetLeftPart(UriPartial.Path));
            this.RoamingNetworkId   = roamingNetworkId;
            this.IdentityKeyIds     = identityKeyIds;

        }

        #endregion


        #region ToURI()

        /// <summary>
        /// The URI for a QR code: the URI of the roaming network with the @ids of the identity keys.
        /// </summary>
        public String ToURI()

            => $"{RoamingNetworkURI.GetLeftPart(UriPartial.Path)}?{IdentityKeysParameter}={String.Join(",", IdentityKeyIds)}";

        #endregion

        #region (static) TryParse(URI, out TrustAnchor, out ErrorResponse)

        /// <summary>
        /// Try to parse the given URI of a trust anchor, e.g. of a QR code.
        /// </summary>
        public static Boolean TryParse(String                                              URI,
                                       [NotNullWhen(true)]  out RoamingNetworkTrustAnchor?  TrustAnchor,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            TrustAnchor = null;

            if (!Uri.TryCreate(URI?.Trim(), UriKind.Absolute, out var uri))
            {
                ErrorResponse = "A trust anchor is an absolute URI!";
                return false;
            }

            var identityKeys = uri.Query.TrimStart('?').
                                   Split('&', StringSplitOptions.RemoveEmptyEntries).
                                   Select (parameter => parameter.Split('=', 2)).
                                   Where  (parameter => parameter.Length == 2 && parameter[0] == IdentityKeysParameter).
                                   ToArray();

            if (identityKeys.Length != 1)
            {
                ErrorResponse = $"A trust anchor names the identity keys once: ?{IdentityKeysParameter}=…!";
                return false;
            }

            if (!TryCheck(uri, Uri.UnescapeDataString(identityKeys[0][1]).Split(','), out _, out _, out ErrorResponse))
                return false;

            TrustAnchor = new RoamingNetworkTrustAnchor(uri, Uri.UnescapeDataString(identityKeys[0][1]).Split(','));
            return true;

        }

        #endregion


        #region (private static) TryCheck(URI, IdentityKeyIds, out RoamingNetworkId, out Ids, out ErrorResponse)

        private static Boolean TryCheck(Uri                               URI,
                                        IEnumerable<String>               IdentityKeyIds,
                                        out RoamingNetwork_Id             RoamingNetworkId,
                                        out ImmutableArray<String>        Ids,
                                        [NotNullWhen(false)] out String?  ErrorResponse)
        {

            RoamingNetworkId  = default;
            Ids               = [ .. IdentityKeyIds ];

            if (!URI.IsAbsoluteUri || (URI.Scheme != Uri.UriSchemeHttps && URI.Scheme != Uri.UriSchemeHttp))
            {
                ErrorResponse = "A trust anchor is an http or https URI!";
                return false;
            }

            var segments = URI.AbsolutePath.TrimEnd('/').Split('/');

            if (segments.Length < 2 || segments[^2] != "RNs" ||
                !RoamingNetwork_Id.TryParse(Uri.UnescapeDataString(segments[^1]), out RoamingNetworkId))
            {
                ErrorResponse = "The URI of a trust anchor is the one of a roaming network, ~/RNs/{RN_Id}!";
                return false;
            }

            if (Ids.IsEmpty ||
                Ids.Any(id => id.Length != 64 || id.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0) ||
                Ids.Distinct().Count() != Ids.Length)
            {
                ErrorResponse = "A trust anchor names at least one identity key by its @id, 64 lowercase hexadecimal digits, each once!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => ToURI();

        #endregion

    }

}
