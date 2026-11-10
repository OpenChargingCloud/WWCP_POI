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

using System.Net.Http.Headers;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A small client of the private API of an instance at a GNU Taler merchant backend (concepts/EVTaler.md, section 1):
    /// what the backend of an operator needs for the steps of EV Taler sessions, with an access token that has only the
    /// permissions for it (orders-write, orders-read, orders-refund). Making orders, waiting for their payment by long
    /// polling, and granting refunds; nothing else.
    /// </summary>
    public sealed class TalerMerchantClient : IDisposable
    {

        #region Data

        /// <summary>
        /// How long a request waits for its answer, beyond the time a long poll asked for, by default.
        /// </summary>
        public static readonly TimeSpan  DefaultRequestTimeout  = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The longest a single long poll waits for a payment; a longer wait polls again.
        /// </summary>
        public static readonly TimeSpan  MaxLongPollTimeout     = TimeSpan.FromMinutes(2);

        private readonly HttpClient  httpClient;
        private readonly String      accessToken;

        #endregion

        #region Properties

        /// <summary>
        /// The base URL of the instance: HTTPS, ending in '/', the merchant base URL of the operator's evTaler, e.g.
        /// https://taler.gef.example/instances/de-gef/.
        /// </summary>
        public URL       InstanceBaseURL    { get; }

        /// <summary>
        /// How long a request waits for its answer, beyond the time a long poll asked for.
        /// </summary>
        public TimeSpan  RequestTimeout     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a client of the private API of an instance at a GNU Taler merchant backend.
        /// </summary>
        /// <param name="InstanceBaseURL">The base URL of the instance, ending in '/'.</param>
        /// <param name="AccessToken">An access token of the instance, as GNU Taler writes it ("secret-token:…").</param>
        /// <param name="Handler">An optional HTTP message handler, e.g. one of a test; the client then leaves it open.</param>
        /// <param name="RequestTimeout">How long a request waits for its answer, beyond the time a long poll asked for.</param>
        public TalerMerchantClient(URL                  InstanceBaseURL,
                                   String               AccessToken,
                                   HttpMessageHandler?  Handler          = null,
                                   TimeSpan?            RequestTimeout   = null)
        {

            if (!InstanceBaseURL.ToString().EndsWith('/'))
                throw new ArgumentException($"The base URL of a merchant instance '{InstanceBaseURL}' must end with '/'!", nameof(InstanceBaseURL));

            if (String.IsNullOrWhiteSpace(AccessToken))
                throw new ArgumentException("A merchant instance is asked with an access token!", nameof(AccessToken));

            this.InstanceBaseURL  = InstanceBaseURL;
            this.RequestTimeout   = RequestTimeout ?? DefaultRequestTimeout;
            this.accessToken      = AccessToken.StartsWith("secret-token:", StringComparison.Ordinal)
                                        ? AccessToken
                                        : "secret-token:" + AccessToken;
            this.httpClient       = new HttpClient(Handler ?? new SocketsHttpHandler(), disposeHandler: Handler is null) {
                                        Timeout = Timeout.InfiniteTimeSpan
                                    };

        }

        #endregion


        #region PostOrder(Order, RefundDelay = null, CreateToken = true, CancellationToken = default)

        /// <summary>
        /// Make an order: POST /private/orders.
        /// </summary>
        /// <param name="Order">The order: summary, amount, products, deadlines, extra, … (GNU Taler's Order).</param>
        /// <param name="RefundDelay">How long after the payment deadline the order may be refunded.</param>
        /// <param name="CreateToken">Whether the order gets a claim token, which only who knows it may claim it with.</param>
        /// <param name="CancellationToken">A cancellation token.</param>
        public async Task<TalerMerchantResponse<TalerPostOrderResponse>> PostOrder(JObject            Order,
                                                                                   TimeSpan?          RefundDelay         = null,
                                                                                   Boolean            CreateToken         = true,
                                                                                   CancellationToken  CancellationToken   = default)
        {

            var request = new JObject(new JProperty("order", Order));

            if (RefundDelay.HasValue)
                request.Add("refund_delay", TalerTime.RelativeTime(RefundDelay.Value));

            request.Add("create_token", CreateToken);

            var (statusCode, json, error) = await Send(HttpMethod.Post, "private/orders", request, TimeSpan.Zero, CancellationToken);

            if (json is null || statusCode != 200)
                return Failed<TalerPostOrderResponse>(statusCode, json, error);

            return json["order_id"]?.Type == JTokenType.String
                       ? TalerMerchantResponse<TalerPostOrderResponse>.Success(statusCode,
                                                                               new TalerPostOrderResponse(json["order_id"]!.Value<String>()!,
                                                                                                          json["token"]?.Type == JTokenType.String ? json["token"]!.Value<String>() : null,
                                                                                                          json))
                       : TalerMerchantResponse<TalerPostOrderResponse>.Failed(statusCode, "The merchant backend made an order without naming it!");

        }

        #endregion

        #region GetOrder (OrderId, Timeout = null, CancellationToken = default)

        /// <summary>
        /// The status of an order: GET /private/orders/{order_id}. With a timeout, the backend waits up to that long for the
        /// order to be paid before it answers (long polling).
        /// </summary>
        /// <param name="OrderId">The order.</param>
        /// <param name="Timeout">How long to wait for its payment, at most MaxLongPollTimeout.</param>
        /// <param name="CancellationToken">A cancellation token.</param>
        public async Task<TalerMerchantResponse<TalerOrderStatus>> GetOrder(String             OrderId,
                                                                            TimeSpan?          Timeout             = null,
                                                                            CancellationToken  CancellationToken   = default)
        {

            var wait  = Timeout.HasValue
                            ? TimeSpan.FromMilliseconds(Math.Clamp(Timeout.Value.TotalMilliseconds, 0, MaxLongPollTimeout.TotalMilliseconds))
                            : TimeSpan.Zero;

            var path  = "private/orders/" + Uri.EscapeDataString(OrderId) +
                        (wait > TimeSpan.Zero ? $"?timeout_ms={(Int64) wait.TotalMilliseconds}" : "");

            var (statusCode, json, error) = await Send(HttpMethod.Get, path, null, wait, CancellationToken);

            if (json is null || statusCode != 200)
                return Failed<TalerOrderStatus>(statusCode, json, error);

            return TalerOrderStatus.TryParse(json, out var status, out var statusError)
                       ? TalerMerchantResponse<TalerOrderStatus>.Success(statusCode, status)
                       : TalerMerchantResponse<TalerOrderStatus>.Failed (statusCode, statusError);

        }

        #endregion

        #region Refund   (OrderId, Refund, Reason, CancellationToken = default)

        /// <summary>
        /// Grant a refund of a paid order: POST /private/orders/{order_id}/refund. Refunds of an order add up; together they
        /// never exceed what it took.
        /// </summary>
        /// <param name="OrderId">The order.</param>
        /// <param name="Refund">How much to refund in all, as GNU Taler writes amounts, e.g. "EUR:1.03".</param>
        /// <param name="Reason">Why, as the driver reads it.</param>
        /// <param name="CancellationToken">A cancellation token.</param>
        public async Task<TalerMerchantResponse<TalerRefundResponse>> Refund(String             OrderId,
                                                                             String             Refund,
                                                                             String             Reason,
                                                                             CancellationToken  CancellationToken   = default)
        {

            var (statusCode, json, error) = await Send(HttpMethod.Post,
                                                       "private/orders/" + Uri.EscapeDataString(OrderId) + "/refund",
                                                       new JObject(new JProperty("refund", Refund),
                                                                   new JProperty("reason", Reason)),
                                                       TimeSpan.Zero,
                                                       CancellationToken);

            if (json is null || statusCode != 200)
                return Failed<TalerRefundResponse>(statusCode, json, error);

            return json["taler_refund_uri"]?.Type == JTokenType.String
                       ? TalerMerchantResponse<TalerRefundResponse>.Success(statusCode,
                                                                            new TalerRefundResponse(json["taler_refund_uri"]!.Value<String>()!,
                                                                                                    json["h_contract"]?.Type == JTokenType.String ? json["h_contract"]!.Value<String>() : null,
                                                                                                    json))
                       : TalerMerchantResponse<TalerRefundResponse>.Failed(statusCode, "The merchant backend granted a refund without its taler://refund URI!");

        }

        #endregion


        #region (private) Send(Method, Path, Body, Wait, CancellationToken)

        private async Task<(Int32 StatusCode, JObject? JSON, String? Error)> Send(HttpMethod         Method,
                                                                                    String             Path,
                                                                                    JObject?           Body,
                                                                                    TimeSpan           Wait,
                                                                                    CancellationToken  CancellationToken)
        {

            using var timeout  = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
            timeout.CancelAfter(Wait + RequestTimeout);

            using var request  = new HttpRequestMessage(Method, new Uri(new Uri(InstanceBaseURL.ToString()), Path));

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (Body is not null)
                request.Content = new StringContent(Body.ToString(Formatting.None), Encoding.UTF8, "application/json");

            try
            {

                using var response  = await httpClient.SendAsync(request, timeout.Token);
                var       text      = await response.Content.ReadAsStringAsync(timeout.Token);

                JObject? json = null;

                if (text.Length > 0)
                {
                    try
                    {
                        json = JObject.Parse(text);
                    }
                    catch (JsonException)
                    {
                        return ((Int32) response.StatusCode, null, $"The merchant backend answered no JSON object: {text[..Math.Min(text.Length, 200)]}");
                    }
                }

                return ((Int32) response.StatusCode, json, json is null ? "The merchant backend answered nothing." : null);

            }
            catch (OperationCanceledException) when (!CancellationToken.IsCancellationRequested)
            {
                return (0, null, "The merchant backend did not answer in time.");
            }
            catch (HttpRequestException e)
            {
                return (0, null, $"The merchant backend is not reached: {e.Message}");
            }

        }

        #endregion

        #region (private static) Failed<T>(StatusCode, JSON, Error)

        private static TalerMerchantResponse<T> Failed<T>(Int32     StatusCode,
                                                          JObject?  JSON,
                                                          String?   Error)
            where T : class

            => TalerMerchantResponse<T>.Failed(StatusCode,
                                               JSON?["hint"]?.Type == JTokenType.String
                                                   ? JSON["hint"]!.Value<String>()!
                                                   : Error ?? $"The merchant backend answered {StatusCode}.",
                                               JSON?["code"]?.Type == JTokenType.Integer
                                                   ? JSON["code"]!.Value<Int32>()
                                                   : null);

        #endregion


        public void Dispose()
            => httpClient.Dispose();

    }

}
