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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The answer of a GNU Taler merchant backend to a request of the TalerMerchantClient: its result, or the HTTP status
    /// code and GNU Taler's error code and hint of why there is none.
    /// </summary>
    /// <typeparam name="T">The type of its result.</typeparam>
    public sealed class TalerMerchantResponse<T>
        where T : class
    {

        #region Properties

        /// <summary>
        /// The HTTP status code of the answer; 0 when no answer came.
        /// </summary>
        public Int32    StatusCode    { get; }

        /// <summary>
        /// The result, when the request succeeded.
        /// </summary>
        public T?       Result        { get; }

        /// <summary>
        /// GNU Taler's error code ("code"), if the backend gave one.
        /// </summary>
        public Int32?   ErrorCode     { get; }

        /// <summary>
        /// Why there is no result: GNU Taler's hint, or what went wrong on the way.
        /// </summary>
        public String?  Error         { get; }

        /// <summary>
        /// Whether the request succeeded.
        /// </summary>
        [MemberNotNullWhen(true, nameof(Result))]
        public Boolean  IsSuccess
            => Result is not null;

        #endregion

        #region Constructor(s)

        private TalerMerchantResponse(Int32    StatusCode,
                                      T?       Result,
                                      Int32?   ErrorCode,
                                      String?  Error)
        {

            this.StatusCode  = StatusCode;
            this.Result      = Result;
            this.ErrorCode   = ErrorCode;
            this.Error       = Error;

        }

        #endregion


        /// <summary>
        /// A result.
        /// </summary>
        public static TalerMerchantResponse<T> Success(Int32  StatusCode,
                                                       T      Result)

            => new (StatusCode, Result, null, null);

        /// <summary>
        /// No result, and why.
        /// </summary>
        public static TalerMerchantResponse<T> Failed(Int32    StatusCode,
                                                      String   Error,
                                                      Int32?   ErrorCode   = null)

            => new (StatusCode, null, ErrorCode, Error);


        public override String ToString()

            => IsSuccess
                   ? $"{StatusCode}: {Result}"
                   : $"{StatusCode}{(ErrorCode.HasValue ? $" ({ErrorCode})" : "")}: {Error}";

    }


    /// <summary>
    /// What a merchant backend answers to POST /private/orders: the order and its claim token.
    /// </summary>
    /// <param name="OrderId">The order.</param>
    /// <param name="Token">Its claim token, unless none was asked for.</param>
    /// <param name="JSON">The answer, as given.</param>
    public sealed record TalerPostOrderResponse(String   OrderId,
                                                String?  Token,
                                                JObject  JSON);


    /// <summary>
    /// What a merchant backend answers to POST /private/orders/{order_id}/refund: where the wallet picks the refund up.
    /// </summary>
    /// <param name="TalerRefundURI">The taler://refund/… URI.</param>
    /// <param name="HContract">The hash of the contract, as GNU Taler writes it.</param>
    /// <param name="JSON">The answer, as given.</param>
    public sealed record TalerRefundResponse(String   TalerRefundURI,
                                             String?  HContract,
                                             JObject  JSON);


    /// <summary>
    /// The status of an order at a merchant backend, GET /private/orders/{order_id}: unpaid, with the taler://pay URI and
    /// the contract terms it will have once claimed (proto_contract_terms); claimed by a wallet; or paid, with what was
    /// refunded.
    /// </summary>
    public sealed class TalerOrderStatus
    {

        #region Data

        /// <summary>
        /// An order not paid yet.
        /// </summary>
        public const String Unpaid   = "unpaid";

        /// <summary>
        /// An order a wallet claimed, not paid yet.
        /// </summary>
        public const String Claimed  = "claimed";

        /// <summary>
        /// A paid order.
        /// </summary>
        public const String Paid     = "paid";

        #endregion

        #region Properties

        /// <summary>
        /// The status, as given.
        /// </summary>
        public JObject   JSON              { get; }

        /// <summary>
        /// "unpaid", "claimed" or "paid".
        /// </summary>
        public String    Status            { get; }

        /// <summary>
        /// The taler://pay URI of an unpaid order.
        /// </summary>
        public String?   TalerPayURI       { get; }

        /// <summary>
        /// The contract terms: of a claimed or paid order as claimed, of an unpaid one as they will be (without the
        /// wallet's nonce).
        /// </summary>
        public JObject?  ContractTerms     { get; }

        /// <summary>
        /// Whether a paid order was refunded, in part or in full.
        /// </summary>
        public Boolean   Refunded          { get; }

        /// <summary>
        /// What was refunded of a paid order, as GNU Taler writes amounts, e.g. "EUR:1.03".
        /// </summary>
        public String?   RefundAmount      { get; }

        /// <summary>
        /// Whether the order is paid.
        /// </summary>
        public Boolean   IsPaid
            => Status == Paid;

        #endregion

        #region Constructor(s)

        private TalerOrderStatus(JObject   JSON,
                                 String    Status,
                                 String?   TalerPayURI,
                                 JObject?  ContractTerms,
                                 Boolean   Refunded,
                                 String?   RefundAmount)
        {

            this.JSON           = JSON;
            this.Status         = Status;
            this.TalerPayURI    = TalerPayURI;
            this.ContractTerms  = ContractTerms;
            this.Refunded       = Refunded;
            this.RefundAmount   = RefundAmount;

        }

        #endregion


        #region (static) TryParse(JSON, out Status, out ErrorResponse)

        /// <summary>
        /// Try to read the status of an order as a merchant backend gives it.
        /// </summary>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out TalerOrderStatus?  Status,
                                       [NotNullWhen(false)] out String?            ErrorResponse)
        {

            Status = null;

            var status = JSON["order_status"]?.Type == JTokenType.String
                             ? JSON["order_status"]!.Value<String>()!
                             : null;

            switch (status)
            {

                case Unpaid:

                    if (JSON["taler_pay_uri"]?.Type != JTokenType.String)
                    {
                        ErrorResponse = "An unpaid order names its taler://pay URI!";
                        return false;
                    }

                    Status = new TalerOrderStatus(JSON,
                                                  Unpaid,
                                                  JSON["taler_pay_uri"]!.Value<String>(),
                                                  JSON["proto_contract_terms"] as JObject,
                                                  false,
                                                  null);
                    break;

                case Claimed:
                case Paid:

                    if (JSON["contract_terms"] is not JObject contractTerms)
                    {
                        ErrorResponse = $"A {status} order names its contract terms!";
                        return false;
                    }

                    Status = new TalerOrderStatus(JSON,
                                                  status,
                                                  null,
                                                  contractTerms,
                                                  JSON["refunded"]?.Type == JTokenType.Boolean && JSON["refunded"]!.Value<Boolean>(),
                                                  JSON["refund_amount"]?.Type == JTokenType.String ? JSON["refund_amount"]!.Value<String>() : null);
                    break;

                default:
                    ErrorResponse = $"Unknown status of an order: '{JSON["order_status"]}'!";
                    return false;

            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        public override String ToString()
            => Status;

    }


    /// <summary>
    /// The times of GNU Taler in JSON: a timestamp { "t_s": … }, a relative time { "d_us": … }.
    /// </summary>
    public static class TalerTime
    {

        /// <summary>
        /// A timestamp, in seconds since the epoch.
        /// </summary>
        public static JObject Timestamp(DateTimeOffset At)

            => new (new JProperty("t_s", At.ToUnixTimeSeconds()));

        /// <summary>
        /// A relative time, in microseconds.
        /// </summary>
        public static JObject RelativeTime(TimeSpan Duration)

            => new (new JProperty("d_us", Duration.Ticks / TimeSpan.TicksPerMicrosecond));

        /// <summary>
        /// Try to read a timestamp; "never" is none.
        /// </summary>
        public static Boolean TryParseTimestamp(JToken?             JSON,
                                                out DateTimeOffset  At)
        {

            At = default;

            if (JSON is JObject { } timestamp && timestamp["t_s"] is JValue { Type: JTokenType.Integer } seconds)
            {
                At = DateTimeOffset.FromUnixTimeSeconds(seconds.Value<Int64>());
                return true;
            }

            return false;

        }

    }

}
