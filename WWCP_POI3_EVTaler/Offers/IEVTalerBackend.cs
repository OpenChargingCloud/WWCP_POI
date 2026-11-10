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
    /// The backend of an operator that makes EV Taler offers (concepts/EVTaler.md, section 6): it talks to the operator's
    /// instance at a GNU Taler merchant backend and to its charging station management system. The application hosting a
    /// POI server provides it, as it provides AuthorizeStatusUpdate; the operator's charging station management system
    /// does (decision D7). The offers server relays what it tells, after checking every offer against the version it
    /// holds.
    /// </summary>
    public interface IEVTalerBackend
    {

        /// <summary>
        /// Whether it makes offers for the given operator in the given roaming network.
        /// </summary>
        Boolean Serves(RoamingNetwork_Id           RoamingNetworkId,
                       ChargingStationOperator_Id  OperatorId);

        /// <summary>
        /// Start a session: check that the EVSE takes one now and that the step and the exchange are accepted, make the
        /// order of step 1 and tell it as an offer (Session.Events.Offer) before answering. Everything after that is told
        /// to Session.Events as it happens: payments, signed meter values, the next offers, the end and the settlement.
        /// </summary>
        Task<EVTalerStartResult> StartSession(EVTalerSessionContext  Session,
                                              CancellationToken      CancellationToken);

        /// <summary>
        /// The driver stops the session: charging ends, and the session is settled as it would be otherwise.
        /// </summary>
        Task StopSession(String             SessionId,
                         CancellationToken  CancellationToken);

    }


    /// <summary>
    /// What the offers server tells a backend of a session it starts.
    /// </summary>
    /// <param name="SessionId">The session: 128 random bits, base64url; the sessionId of every step.</param>
    /// <param name="Request">What the driver asked for.</param>
    /// <param name="Station">The charging station of the EVSE, as the server's version proves it, verified.</param>
    /// <param name="PriceStatements">The current price statements of the EVSE, its station and its pool, as held.</param>
    /// <param name="StartedAt">When the session was asked for: the time its prices are current at.</param>
    /// <param name="Events">Where the backend tells what happens.</param>
    public sealed record EVTalerSessionContext(String                                       SessionId,
                                               EVTalerSessionRequest                        Request,
                                               VerifiedChargingStation                      Station,
                                               IReadOnlyList<RoamingNetworkPriceStatement>  PriceStatements,
                                               DateTimeOffset                               StartedAt,
                                               IEVTalerSessionEvents                        Events);


    /// <summary>
    /// What a backend tells of a session; the offers server relays it to the driver as events.
    /// </summary>
    public interface IEVTalerSessionEvents
    {

        /// <summary>
        /// The session.
        /// </summary>
        String SessionId { get; }

        /// <summary>
        /// An offer of the next step. The server checks it against the version it holds before it relays it; an offer
        /// failing a check is not relayed, the session ends (StopSession), and the answer is false with the reason.
        /// </summary>
        Task<EVTalerOfferResult> Offer(EVTalerOffer Offer);

        /// <summary>
        /// A step is paid: the credit is raised.
        /// </summary>
        Task Paid(UInt32  Step,
                  String  OrderId);

        /// <summary>
        /// A step was not paid by its deadline: charging ends when the credit is used.
        /// </summary>
        Task Unpaid(UInt32  Step,
                    String  OrderId);

        /// <summary>
        /// A signed meter value, as the meter gives it: the one at the start, before step 2 is offered, and the one at the
        /// end, before the settlement.
        /// </summary>
        Task MeterValue(EVTalerSignedMeterValue  Value,
                        String                   At);

        /// <summary>
        /// Charging ended (EVTalerStopReasons).
        /// </summary>
        Task Stopped(String   Reason,
                     String?  Description   = null);

        /// <summary>
        /// The session is settled: the settlement statement, signed with a key for payments, and the refunds. Without both
        /// signed meter values there is no statement, and every step is refunded in full (concepts/EVTaler.md, 5.4).
        /// </summary>
        Task Settlement(EVTalerSettlement?          Statement,
                        IEnumerable<EVTalerRefund>  Refunds);

    }


    /// <summary>
    /// Why charging of an EV Taler session ended.
    /// </summary>
    public static class EVTalerStopReasons
    {

        /// <summary>
        /// The driver stopped it.
        /// </summary>
        public const String Driver    = "driver";

        /// <summary>
        /// The vehicle stopped it, e.g. full.
        /// </summary>
        public const String Vehicle   = "vehicle";

        /// <summary>
        /// The cap of the driver was reached.
        /// </summary>
        public const String Cap       = "cap";

        /// <summary>
        /// A step was not paid, and the credit is used.
        /// </summary>
        public const String Unpaid    = "unpaid";

        /// <summary>
        /// The credit was used: the station ended at the limit of the transaction. A backend tells it as the cap or an unpaid
        /// step where it knows that one of them is why.
        /// </summary>
        public const String Limit     = "limit";

        /// <summary>
        /// The operator ended it.
        /// </summary>
        public const String Operator  = "operator";

        /// <summary>
        /// The offers server refused an offer of the backend: it failed a check against the version the server holds.
        /// </summary>
        public const String Refused   = "refused";

    }


    /// <summary>
    /// A meter value is the one at the start or at the end.
    /// </summary>
    public static class EVTalerMeterValueAt
    {

        /// <summary>
        /// The signed meter value at the start of the session.
        /// </summary>
        public const String Start  = "start";

        /// <summary>
        /// The signed meter value at the end of the session.
        /// </summary>
        public const String End    = "end";

    }


    /// <summary>
    /// The size of the steps a driver asks for: a quantity of the dimension the tariff is priced in, kWh or minutes.
    /// </summary>
    /// <param name="Quantity">The quantity, more than 0.</param>
    /// <param name="Unit">EVTalerCovers.KWh or EVTalerCovers.Minutes.</param>
    public sealed record EVTalerStepSize(Decimal  Quantity,
                                         String   Unit)
    {

        /// <summary>
        /// { "quantity": "5", "unit": "kWh" }
        /// </summary>
        public JObject ToJSON()

            => new (new JProperty("quantity", EVTalerAmounts.WriteDecimal(Quantity)),
                    new JProperty("unit",     Unit));

        /// <summary>
        /// Try to read a step size.
        /// </summary>
        public static Boolean TryParse(JToken?                                    JSON,
                                       [NotNullWhen(true)]  out EVTalerStepSize?  StepSize,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            StepSize = null;

            if (JSON is not JObject json ||
                !EVTalerAmounts.TryParseDecimal(json, "quantity", out var quantity, out _) ||
                quantity <= 0 ||
                json["unit"]?.Type != JTokenType.String ||
                json["unit"]!.Value<String>() is not (EVTalerCovers.KWh or EVTalerCovers.Minutes))
            {
                ErrorResponse = $"A step is a quantity of more than 0, written as a text, in '{EVTalerCovers.KWh}' or '{EVTalerCovers.Minutes}': {{ \"quantity\": \"5\", \"unit\": \"kWh\" }}!";
                return false;
            }

            StepSize       = new EVTalerStepSize(quantity, json["unit"]!.Value<String>()!);
            ErrorResponse  = null;
            return true;

        }

        public override String ToString()
            => $"{EVTalerAmounts.WriteDecimal(Quantity)} {Unit}";

    }


    /// <summary>
    /// What a driver asks for, POST ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/evTaler/sessions:
    /// { "exchange": "&lt;masterPublicKey&gt;", "step": { "quantity": "5", "unit": "kWh" }, "cap": "30.00" }.
    /// </summary>
    /// <param name="RoamingNetworkId">The roaming network.</param>
    /// <param name="EVSEId">The EVSE.</param>
    /// <param name="Exchange">The master public key of the exchange the driver's coins are of.</param>
    /// <param name="Step">The size of the steps.</param>
    /// <param name="Cap">The most the whole session may cost, in the currency of the tariff, if the driver sets one.</param>
    public sealed record EVTalerSessionRequest(RoamingNetwork_Id  RoamingNetworkId,
                                               EVSE_Id            EVSEId,
                                               String             Exchange,
                                               EVTalerStepSize    Step,
                                               Decimal?           Cap)
    {

        /// <summary>
        /// Try to read what a driver asks for, at the given EVSE.
        /// </summary>
        public static Boolean TryParse(JObject                                          JSON,
                                       RoamingNetwork_Id                                RoamingNetworkId,
                                       EVSE_Id                                          EVSEId,
                                       [NotNullWhen(true)]  out EVTalerSessionRequest?  Request,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            Request = null;

            if (JSON.Properties().FirstOrDefault(property => property.Name is not "exchange" and not "step" and not "cap") is JProperty unknown)
            {
                ErrorResponse = $"A session is asked for without '{unknown.Name}': exchange, step and cap are all it names!";
                return false;
            }

            if (JSON["exchange"]?.Type != JTokenType.String || !EVTalerMerchant.IsTalerPublicKey(JSON["exchange"]!.Value<String>()))
            {
                ErrorResponse = "A session names the exchange of the driver's coins by its master public key, as GNU Taler writes it!";
                return false;
            }

            if (!EVTalerStepSize.TryParse(JSON["step"], out var step, out ErrorResponse))
                return false;

            Decimal? cap = null;

            if (JSON["cap"] is not null)
            {

                if (!EVTalerAmounts.TryParseDecimal(JSON, "cap", out var value, out _) || value <= 0)
                {
                    ErrorResponse = "The cap of a session is an amount of more than 0, written as a text, e.g. \"30.00\"!";
                    return false;
                }

                cap = value;

            }

            Request        = new EVTalerSessionRequest(RoamingNetworkId, EVSEId, JSON["exchange"]!.Value<String>()!, step, cap);
            ErrorResponse  = null;
            return true;

        }

    }


    /// <summary>
    /// An offer of a step, as the backend made its order: what the driver's wallet pays.
    /// </summary>
    /// <param name="Step">The step: 1, 2, …</param>
    /// <param name="OrderId">The order at the operator's instance.</param>
    /// <param name="TalerPayURI">The taler://pay URI, as the merchant backend gave it.</param>
    /// <param name="ContractTerms">The contract terms of the order, as the merchant backend gave them before a wallet
    /// claimed it (proto_contract_terms): what the wallet will be asked to pay.</param>
    public sealed record EVTalerOffer(UInt32   Step,
                                      String   OrderId,
                                      String   TalerPayURI,
                                      JObject  ContractTerms);


    /// <summary>
    /// Whether the offers server relayed an offer, or why not.
    /// </summary>
    /// <param name="Relayed">Whether it was relayed.</param>
    /// <param name="ErrorResponse">Why it was refused.</param>
    public sealed record EVTalerOfferResult(Boolean  Relayed,
                                            String?  ErrorResponse);


    /// <summary>
    /// A refund of a step at the settlement.
    /// </summary>
    /// <param name="OrderId">The order of the step.</param>
    /// <param name="Amount">How much of it is refunded.</param>
    /// <param name="Currency">The currency.</param>
    /// <param name="TalerRefundURI">Where the wallet picks it up, as the merchant backend gave it, if it gave one.</param>
    public sealed record EVTalerRefund(String   OrderId,
                                       Decimal  Amount,
                                       String   Currency,
                                       String?  TalerRefundURI)
    {

        /// <summary>
        /// { "orderId", "amount": "EUR:1.03", "talerRefundURI" }
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(new JProperty("orderId", OrderId),
                                   new JProperty("amount",  $"{Currency}:{EVTalerAmounts.Write(Amount, Currency)}"));

            if (TalerRefundURI is not null)
                json.Add("talerRefundURI", TalerRefundURI);

            return json;

        }

        public override String ToString()
            => $"{OrderId}: {EVTalerAmounts.Write(Amount, Currency)} {Currency}";

    }


    /// <summary>
    /// How a backend answers the start of a session.
    /// </summary>
    public sealed class EVTalerStartResult
    {

        #region Properties

        /// <summary>
        /// Whether the session started: step 1 was offered.
        /// </summary>
        public Boolean                          Started           { get; }

        /// <summary>
        /// The HTTP status code the driver gets when it did not start: 409 when the EVSE takes no session now, 422 when the
        /// step or the exchange is not accepted, 502 when the merchant backend failed.
        /// </summary>
        public Int32                            StatusCode        { get; }

        /// <summary>
        /// Why it did not start.
        /// </summary>
        public String?                          Description       { get; }

        /// <summary>
        /// The step sizes that are accepted, when the one asked for is not.
        /// </summary>
        public IReadOnlyList<EVTalerStepSize>   AcceptedSteps     { get; }

        #endregion

        #region Constructor(s)

        private EVTalerStartResult(Boolean                        Started,
                                   Int32                          StatusCode,
                                   String?                        Description,
                                   IReadOnlyList<EVTalerStepSize> AcceptedSteps)
        {

            this.Started        = Started;
            this.StatusCode     = StatusCode;
            this.Description    = Description;
            this.AcceptedSteps  = AcceptedSteps;

        }

        #endregion


        /// <summary>
        /// The session started: step 1 was offered.
        /// </summary>
        public static EVTalerStartResult Success()
            => new (true, 201, null, []);

        /// <summary>
        /// The EVSE takes no session now: no vehicle plugged in, or one charging already.
        /// </summary>
        public static EVTalerStartResult Busy(String Description)
            => new (false, 409, Description, []);

        /// <summary>
        /// The step or the exchange is not accepted.
        /// </summary>
        public static EVTalerStartResult NotAccepted(String                         Description,
                                                     IEnumerable<EVTalerStepSize>?  AcceptedSteps = null)
            => new (false, 422, Description, [.. AcceptedSteps ?? []]);

        /// <summary>
        /// The merchant backend or the charging station management system failed, or the offer of step 1 was refused.
        /// </summary>
        public static EVTalerStartResult Failed(String Description)
            => new (false, 502, Description, []);


        public override String ToString()
            => Started ? "started" : $"{StatusCode}: {Description}";

    }

}
