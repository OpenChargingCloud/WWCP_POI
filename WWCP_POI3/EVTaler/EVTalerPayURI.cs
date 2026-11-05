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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A taler://pay URI of an offer, as the merchant backend gives it:
    /// taler://pay/{merchant_host}{/merchant_prefix_path*}/{order_id}/{session_id}{?c=claim_token}{#ssid}.
    /// An app handing it to a separate wallet checks at least that it names the merchant of the operator
    /// (concepts/EVTaler.md, 7.3): the wallet then gets the contract terms from that merchant, signed by its instance.
    /// </summary>
    public sealed class EVTalerPayURI
    {

        #region Properties

        /// <summary>
        /// The URI as given.
        /// </summary>
        public String   Text               { get; }

        /// <summary>
        /// The base URL of the merchant instance it names: https://{merchant_host}/{merchant_prefix_path}/.
        /// </summary>
        public String   MerchantBaseURL    { get; }

        /// <summary>
        /// The order.
        /// </summary>
        public String   OrderId            { get; }

        /// <summary>
        /// GNU Taler's session of the order, if any; not the session of the offers server.
        /// </summary>
        public String?  SessionId          { get; }

        /// <summary>
        /// The claim token, if any.
        /// </summary>
        public String?  ClaimToken         { get; }

        #endregion

        #region (private) Constructor(s)

        private EVTalerPayURI(String   Text,
                              String   MerchantBaseURL,
                              String   OrderId,
                              String?  SessionId,
                              String?  ClaimToken)
        {

            this.Text             = Text;
            this.MerchantBaseURL  = MerchantBaseURL;
            this.OrderId          = OrderId;
            this.SessionId        = SessionId;
            this.ClaimToken       = ClaimToken;

        }

        #endregion


        #region (static) TryParse(Text, out URI, out ErrorResponse)

        /// <summary>
        /// Try to read the given taler://pay URI. Its scheme is read in any case, as QR codes write it in upper case;
        /// taler+http://, of a merchant without HTTPS, is refused. The order and GNU Taler's session are counted from the
        /// end of its path, as the merchant's path may have any number of segments.
        /// </summary>
        public static Boolean TryParse(String?                                  Text,
                                       [NotNullWhen(true)]  out EVTalerPayURI?  URI,
                                       [NotNullWhen(false)] out String?         ErrorResponse)
        {

            URI = null;

            const String scheme = "taler://pay/";

            if (Text is null || !Text.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                ErrorResponse = Text?.StartsWith("taler+http://", StringComparison.OrdinalIgnoreCase) == true
                                    ? "An EV Taler offer is paid at a merchant reached with HTTPS: taler+http:// is refused!"
                                    : $"'{Text}' is no taler://pay URI!";
                return false;
            }

            var rest      = Text[scheme.Length..];
            var fragment  = rest.IndexOf('#');

            if (fragment >= 0)
                rest = rest[..fragment];

            String? claimToken = null;
            var query  = rest.IndexOf('?');

            if (query >= 0)
            {

                foreach (var parameter in rest[(query + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
                    if (parameter.StartsWith("c=", StringComparison.Ordinal))
                    {

                        if (claimToken is not null || parameter.Length == 2)
                        {
                            ErrorResponse = "A taler://pay URI names at most one claim token, not empty!";
                            return false;
                        }

                        claimToken = parameter[2..];

                    }

                rest = rest[..query];

            }

            var segments = rest.Split('/');

            if (segments.Length < 3 || segments[0].Length == 0 || segments[^2].Length == 0 ||
                segments[..^2].Any(segment => segment.Length == 0))
            {
                ErrorResponse = "A taler://pay URI names the merchant's host, maybe a path, the order, and GNU Taler's session, maybe empty!";
                return false;
            }

            var host  = segments[0].ToLowerInvariant();
            var path  = String.Concat(segments[1..^2].Select(segment => segment + "/"));

            URI            = new EVTalerPayURI(Text,
                                               $"https://{host}/{path}",
                                               segments[^2],
                                               segments[^1].Length > 0 ? segments[^1] : null,
                                               claimToken);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryVerify(Merchant, out ErrorResponse)

        /// <summary>
        /// Whether this URI names the merchant instance of the given EV Taler merchant: its host in any case, its path
        /// as written.
        /// </summary>
        public Boolean TryVerify(EVTalerMerchant                   Merchant,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (!SameBaseURL(MerchantBaseURL, Merchant.MerchantBaseURL.ToString()))
            {
                ErrorResponse = $"The offer is one of the merchant '{MerchantBaseURL}', not of the operator's '{Merchant.MerchantBaseURL}'!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (internal static) SameBaseURL(A, B)

        /// <summary>
        /// Whether two HTTPS base URLs are the same: the host in any case, the path as written.
        /// </summary>
        internal static Boolean SameBaseURL(String  A,
                                            String  B)
        {

            static (String Host, String Path)? Split(String URL)
            {

                if (!URL.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return null;

                var rest   = URL[8..];
                var slash  = rest.IndexOf('/');

                return slash < 0
                           ? (rest.ToLowerInvariant(), "/")
                           : (rest[..slash].ToLowerInvariant(), rest[slash..]);

            }

            return Split(A) is var (hostA, pathA) &&
                   Split(B) is var (hostB, pathB) &&
                   hostA == hostB &&
                   pathA == pathB;

        }

        #endregion

        #region ToString()

        /// <inheritdoc/>
        public override String ToString()
            => Text;

        #endregion

    }

}
