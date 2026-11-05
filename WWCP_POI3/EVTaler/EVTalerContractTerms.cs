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
    /// How exactly a reader checked the amount of a step.
    /// </summary>
    public static class EVTalerAmountChecks
    {

        /// <summary>
        /// The amount is what the step covers at the price of the session, rounded up to the minor unit; in step 1 with
        /// the flat fee, and the reserve as the rest.
        /// </summary>
        public const String Exact        = "exact";

        /// <summary>
        /// The session is priced by energy and time: the amount is at least what the step covers at the price of its
        /// dimension, the rest an estimate of the other one (concepts/EVTaler.md, 5.1).
        /// </summary>
        public const String Estimate     = "estimate";

        /// <summary>
        /// The prices of the session are not of the simple profile (EVTalerRates): the amount is not recalculated.
        /// </summary>
        public const String Unsupported  = "unsupported";

    }


    /// <summary>
    /// A step of an EV Taler session as its contract terms and a station proof establish it (concepts/EVTaler.md, 7).
    /// </summary>
    /// <param name="Terms">The contract terms of the step.</param>
    /// <param name="Merchant">The operator's merchant, as the proof gives it.</param>
    /// <param name="Tariff">The EV Taler tariff of the EVSE, as the proof gives it.</param>
    /// <param name="Currency">Its currency.</param>
    /// <param name="PriceStatement">The price statement that prices the session, if any.</param>
    /// <param name="Elements">The elements that price the session: of the price statement, else of the tariff.</param>
    /// <param name="Rates">The rates of the simple profile, if the elements are of it.</param>
    /// <param name="SessionStarted">When step 1 was made: the time its price statement is current at.</param>
    /// <param name="StepPrice">What the step covers at the price of its dimension, if recalculated.</param>
    /// <param name="FlatFee">The flat fee, in step 1, if recalculated.</param>
    /// <param name="Reserve">The reserve, in step 1, if recalculated: the amount less the step and the flat fee.</param>
    /// <param name="AmountCheck">How exactly the amount was checked (EVTalerAmountChecks).</param>
    /// <param name="Start">From step 2 on: the signed meter value at the start, verified.</param>
    /// <param name="Diagnostics">What a reader should know.</param>
    public sealed record EVTalerVerifiedStep(EVTalerContractTerms        Terms,
                                             EVTalerMerchant             Merchant,
                                             JObject                     Tariff,
                                             String                      Currency,
                                             VerifiedPrice?              PriceStatement,
                                             ImmutableArray<JObject>     Elements,
                                             EVTalerRates?               Rates,
                                             DateTimeOffset              SessionStarted,
                                             Decimal?                    StepPrice,
                                             Decimal?                    FlatFee,
                                             Decimal?                    Reserve,
                                             String                      AmountCheck,
                                             EVTalerVerifiedMeterValue?  Start,
                                             ImmutableArray<String>      Diagnostics)
    {

        /// <summary>
        /// The step in WWCP terms.
        /// </summary>
        public EVTalerStep  Step
            => Terms.Step;

    }


    /// <summary>
    /// The contract terms of a step of an EV Taler session, as GNU Taler's merchant backend makes them and its instance
    /// signs them (contract terms of version 0): what WWCP_POI reads of them, the rest kept as given.
    /// </summary>
    public sealed class EVTalerContractTerms
    {

        #region Properties

        /// <summary>
        /// The contract terms, as given.
        /// </summary>
        public JObject                                                 JSON                    { get; }

        /// <summary>
        /// The order.
        /// </summary>
        public String                                                  OrderId                 { get; }

        /// <summary>
        /// The currency of its amount.
        /// </summary>
        public String                                                  Currency                { get; }

        /// <summary>
        /// Its amount.
        /// </summary>
        public Decimal                                                 Amount                  { get; }

        /// <summary>
        /// The base URL of the merchant instance.
        /// </summary>
        public String                                                  MerchantBaseURL         { get; }

        /// <summary>
        /// The public key of the merchant instance, which signed the contract terms.
        /// </summary>
        public String                                                  MerchantPublicKey       { get; }

        /// <summary>
        /// The exchanges the merchant accepts: base URL and master public key.
        /// </summary>
        public ImmutableArray<(String URL, String MasterPublicKey)>    Exchanges               { get; }

        /// <summary>
        /// When the contract terms were made.
        /// </summary>
        public DateTimeOffset                                          Timestamp               { get; }

        /// <summary>
        /// Until when the step can be paid.
        /// </summary>
        public DateTimeOffset                                          PayDeadline             { get; }

        /// <summary>
        /// Until when the step can be refunded.
        /// </summary>
        public DateTimeOffset                                          RefundDeadline          { get; }

        /// <summary>
        /// How long the wallet asks for a refund by itself, if at all.
        /// </summary>
        public TimeSpan?                                               AutoRefund              { get; }

        /// <summary>
        /// The step in WWCP terms: "extra": { "wwcp": … }.
        /// </summary>
        public EVTalerStep                                             Step                    { get; }

        #endregion

        #region (private) Constructor(s)

        private EVTalerContractTerms(JObject                                                JSON,
                                     String                                                 OrderId,
                                     String                                                 Currency,
                                     Decimal                                                Amount,
                                     String                                                 MerchantBaseURL,
                                     String                                                 MerchantPublicKey,
                                     IEnumerable<(String URL, String MasterPublicKey)>      Exchanges,
                                     DateTimeOffset                                         Timestamp,
                                     DateTimeOffset                                         PayDeadline,
                                     DateTimeOffset                                         RefundDeadline,
                                     TimeSpan?                                              AutoRefund,
                                     EVTalerStep                                            Step)
        {

            this.JSON               = JSON;
            this.OrderId            = OrderId;
            this.Currency           = Currency;
            this.Amount             = Amount;
            this.MerchantBaseURL    = MerchantBaseURL;
            this.MerchantPublicKey  = MerchantPublicKey;
            this.Exchanges          = [.. Exchanges];
            this.Timestamp          = Timestamp;
            this.PayDeadline        = PayDeadline;
            this.RefundDeadline     = RefundDeadline;
            this.AutoRefund         = AutoRefund;
            this.Step               = Step;

        }

        #endregion


        #region (static) TryParse(JSON, out Terms, out ErrorResponse)

        /// <summary>
        /// Try to read the given contract terms of a step: of version 0, with an order, an amount and a maximum fee in one
        /// currency, the merchant's base URL and public key, at least one exchange, the times GNU Taler writes as
        /// { "t_s": … }, and the step in "extra": { "wwcp": … }. What else GNU Taler writes is kept, not read.
        /// </summary>
        public static Boolean TryParse(JToken?                                         JSON,
                                       [NotNullWhen(true)]  out EVTalerContractTerms?  Terms,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            Terms = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "Contract terms must be a JSON object!";
                return false;
            }

            if (json["version"] is JToken version && !(version.Type == JTokenType.Integer && version.Value<Int64>() == 0))
            {
                ErrorResponse = "The contract terms of a step are of version 0: choices and token families are not taken (phase 4)!";
                return false;
            }

            if (json["order_id"]?.Type != JTokenType.String || json["order_id"]!.Value<String>()!.Length == 0)
            {
                ErrorResponse = "Contract terms name their order!";
                return false;
            }

            if (!EVTalerAmounts.TryParseTaler(json["amount"]?. Type == JTokenType.String ? json["amount"]!. Value<String>() : null, out var currency,    out var amount, out ErrorResponse) ||
                !EVTalerAmounts.TryParseTaler(json["max_fee"]?.Type == JTokenType.String ? json["max_fee"]!.Value<String>() : null, out var feeCurrency, out _,          out ErrorResponse))
            {
                ErrorResponse = $"The amount and the maximum fee of contract terms: {ErrorResponse}";
                return false;
            }

            if (feeCurrency != currency)
            {
                ErrorResponse = "The amount and the maximum fee of contract terms are of one currency!";
                return false;
            }

            if (json["merchant_base_url"]?.Type != JTokenType.String ||
                json["merchant_pub"]?.     Type != JTokenType.String ||
                !EVTalerMerchant.IsTalerPublicKey(json["merchant_pub"]!.Value<String>()))
            {
                ErrorResponse = "Contract terms name the merchant's base URL and its public key, as GNU Taler writes it!";
                return false;
            }

            var exchanges = new List<(String, String)>();

            foreach (var exchange in json["exchanges"] as JArray ?? [])
            {

                if (exchange is not JObject exchangeJSON ||
                    exchangeJSON["url"]?.       Type != JTokenType.String ||
                    exchangeJSON["master_pub"]?.Type != JTokenType.String)
                {
                    ErrorResponse = "An exchange of contract terms names its URL and its master public key!";
                    return false;
                }

                exchanges.Add((exchangeJSON["url"]!.Value<String>()!, exchangeJSON["master_pub"]!.Value<String>()!));

            }

            if (exchanges.Count == 0)
            {
                ErrorResponse = "Contract terms name at least one exchange!";
                return false;
            }

            if (!TryTime(json, "timestamp",       out var timestamp,      out ErrorResponse) ||
                !TryTime(json, "pay_deadline",    out var payDeadline,    out ErrorResponse) ||
                !TryTime(json, "refund_deadline", out var refundDeadline, out ErrorResponse))
            {
                return false;
            }

            TimeSpan? autoRefund = null;

            if (json["auto_refund"] is JToken autoRefundJSON)
            {

                if (autoRefundJSON is not JObject { Count: 1 } relative ||
                    relative["d_us"] is not JToken microseconds ||
                    !(microseconds.Type == JTokenType.Integer && microseconds.Value<Int64>() >= 0 ||
                      microseconds.Type == JTokenType.String  && microseconds.Value<String>() == "forever"))
                {
                    ErrorResponse = "The auto refund of contract terms is a relative time, { \"d_us\": … }!";
                    return false;
                }

                autoRefund = microseconds.Type == JTokenType.Integer
                                 ? TimeSpan.FromMicroseconds(microseconds.Value<Int64>())
                                 : TimeSpan.MaxValue;

            }

            if (json["extra"] is not JObject extra || extra["wwcp"] is not JObject)
            {
                ErrorResponse = "The contract terms of a step name it in \"extra\": { \"wwcp\": … }!";
                return false;
            }

            if (!EVTalerStep.TryParse(extra["wwcp"], out var step, out ErrorResponse))
                return false;

            Terms          = new EVTalerContractTerms(json,
                                                      json["order_id"]!.Value<String>()!,
                                                      currency,
                                                      amount,
                                                      json["merchant_base_url"]!.Value<String>()!,
                                                      json["merchant_pub"]!.Value<String>()!,
                                                      exchanges,
                                                      timestamp,
                                                      payDeadline,
                                                      refundDeadline,
                                                      autoRefund,
                                                      step);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryVerify(Station, PriceStatements, Previous, MeterValueVerifier, out Verified, out ErrorResponse, PayURI = null)

        /// <summary>
        /// Check these contract terms of a step against a verified charging station (concepts/EVTaler.md, 6 and 7): the
        /// merchant and its exchanges are those of the operator's evTaler in the currency of the EVSE's EV Taler tariff;
        /// the step names the roaming network, an EVSE of the station, that tariff and the price statement current when
        /// the session began; it follows the step before, or is step 1; from step 2 on, the signed start value is verified
        /// with a measurement key the proof gives; the amount is the step at the price of the session, where the prices are
        /// of the simple profile; the refund deadline is after the payment deadline, and the wallet picks a refund up by
        /// itself.
        /// </summary>
        /// <param name="Station">The verified charging station of the EVSE.</param>
        /// <param name="PriceStatements">The price statements the app holds of the station, its pool and its EVSEs.</param>
        /// <param name="Previous">The step before, verified; none for step 1.</param>
        /// <param name="MeterValueVerifier">A transparency software verifying signed meter values, from step 2 on.</param>
        /// <param name="Verified">The step, verified.</param>
        /// <param name="ErrorResponse">Why it is refused.</param>
        /// <param name="PayURI">The taler://pay URI of the offer, if the app has it as well.</param>
        public Boolean TryVerify(VerifiedChargingStation                        Station,
                                 IEnumerable<RoamingNetworkPriceStatement>      PriceStatements,
                                 EVTalerVerifiedStep?                           Previous,
                                 EVTalerMeterValueVerifier?                     MeterValueVerifier,
                                 [NotNullWhen(true)]  out EVTalerVerifiedStep?  Verified,
                                 [NotNullWhen(false)] out String?               ErrorResponse,
                                 EVTalerPayURI?                                 PayURI   = null)
        {

            Verified = null;

            var diagnostics = new List<String>();

            #region The merchant of the operator

            if (Station.Operator[ChargingStationOperator.PropertyKeys.EVTaler] is not JObject merchantJSON ||
                !EVTalerMerchant.TryParse(merchantJSON, out var merchant, out var merchantError))
            {
                ErrorResponse = $"The proof gives no EV Taler merchant of the operator '{VerifiedChargingStation.IdOf(Station.Operator)}'!";
                return false;
            }

            if (!EVTalerPayURI.SameBaseURL(MerchantBaseURL, merchant.MerchantBaseURL.ToString()) ||
                MerchantPublicKey != merchant.MerchantPublicKey)
            {
                ErrorResponse = $"The step is offered by the merchant '{MerchantBaseURL}' ({MerchantPublicKey}), not by the operator's '{merchant.MerchantBaseURL}' ({merchant.MerchantPublicKey})!";
                return false;
            }

            if (PayURI is not null &&
                (!EVTalerPayURI.SameBaseURL(PayURI.MerchantBaseURL, MerchantBaseURL) || PayURI.OrderId != OrderId))
            {
                ErrorResponse = "The taler://pay URI names another merchant or another order than the contract terms!";
                return false;
            }

            #endregion

            #region Where: the roaming network and the EVSE

            if (Step.RoamingNetworkId != Station.RoamingNetworkId)
            {
                ErrorResponse = $"The step is one of the roaming network '{Step.RoamingNetworkId}', not of '{Station.RoamingNetworkId}'!";
                return false;
            }

            if (!Station.EVSEs.Any(evse => VerifiedChargingStation.IdOf(evse) == Step.EVSEId))
            {
                ErrorResponse = $"The EVSE '{Step.EVSEId}' of the step is none of the charging station '{VerifiedChargingStation.IdOf(Station.ChargingStation)}'!";
                return false;
            }

            #endregion

            #region The step before

            if (Previous is null)
            {
                if (Step.Step != 1)
                {
                    ErrorResponse = $"Step {Step.Step} is checked after the step before it!";
                    return false;
                }
            }

            else
            {

                var before = Previous.Step;

                if (Step.Step               != before.Step + 1           ||
                    Step.PreviousOrderId    != Previous.Terms.OrderId    ||
                    Step.SessionId          != before.SessionId          ||
                    Step.EVSEId             != before.EVSEId             ||
                    Step.ChargingTariffId   != before.ChargingTariffId   ||
                    Step.PriceStatement     != before.PriceStatement     ||
                    Step.Covers.Unit        != before.Covers.Unit        ||
                    Step.Covers.From        != before.Covers.To)
                {
                    ErrorResponse = $"Step {Step.Step} does not follow step {before.Step}: one session, EVSE, tariff and price statement, the order before, what it covers following on!";
                    return false;
                }

                if (before.StartMeterValue is not null && !before.StartMeterValue.Equals(Step.StartMeterValue))
                {
                    ErrorResponse = $"Step {Step.Step} names another signed meter value at the start than step {before.Step}!";
                    return false;
                }

            }

            #endregion

            #region The EV Taler tariff and the prices of the session

            JObject?                tariff;
            VerifiedPrice?          price;
            ImmutableArray<JObject> elements;
            DateTimeOffset          sessionStarted;

            if (Previous is not null)
            {
                tariff          = Previous.Tariff;
                price           = Previous.PriceStatement;
                elements        = Previous.Elements;
                sessionStarted  = Previous.SessionStarted;
            }

            else
            {

                sessionStarted = Timestamp;

                if (!TryGetEVTalerTariff(Station, Step.EVSEId, out tariff, out ErrorResponse))
                    return false;

                if (VerifiedChargingStation.IdOf(tariff) != Step.ChargingTariffId)
                {
                    ErrorResponse = $"The step names the charging tariff '{Step.ChargingTariffId}', not the EV Taler tariff '{VerifiedChargingStation.IdOf(tariff)}' of the EVSE '{Step.EVSEId}'!";
                    return false;
                }

                if (!Station.TryGetCurrentPrices(PriceStatements, out var current, out ErrorResponse, sessionStarted))
                    return false;

                var pricing = current.Values.
                                  Where (candidate => candidate.Statement.PriceStatus.Prices.Any(prices => prices.ChargingTariffId.ToString() == Step.ChargingTariffId)).
                                  ToArray();

                price = Step.PriceStatement is not null
                            ? pricing.FirstOrDefault(candidate => candidate.Digest == Step.PriceStatement)
                            : null;

                if (Step.PriceStatement is not null && price is null)
                {
                    ErrorResponse = $"The step names the price statement '{Step.PriceStatement}', which is not one current at the start of the session pricing its tariff!";
                    return false;
                }

                if (Step.PriceStatement is null && pricing.Length > 0)
                {
                    ErrorResponse = $"The session is priced by the price statement '{pricing[0].Digest}', which the step does not name!";
                    return false;
                }

                elements = price is not null
                               ? [.. ((price.Statement.PriceStatusJSON["prices"] as JArray ?? []).
                                         OfType<JObject>().
                                         First(prices => prices["chargingTariffId"]?.Value<String>() == Step.ChargingTariffId)["elements"] as JArray ?? []).
                                         OfType<JObject>()]
                               : [.. (tariff[ChargingTariff.PropertyKeys.Elements] as JArray ?? []).OfType<JObject>()];

            }

            var currency = tariff[ChargingTariff.PropertyKeys.Currency]!.Value<String>()!;

            #endregion

            #region The exchanges, in the currency of the tariff

            if (Currency != currency)
            {
                ErrorResponse = $"The step is paid in '{Currency}', not in '{currency}', the currency of the EV Taler tariff!";
                return false;
            }

            var accepted = merchant.Exchanges.Where(exchange => exchange.Currency.ISOCode == currency).ToArray();

            if (Exchanges.FirstOrDefault(exchange => !accepted.Any(known => EVTalerPayURI.SameBaseURL(exchange.URL, known.BaseURL.ToString()) &&
                                                                             exchange.MasterPublicKey == known.MasterPublicKey)) is var (url, masterPublicKey) &&
                url is not null)
            {
                ErrorResponse = $"The exchange '{url}' ({masterPublicKey}) of the step is not one of the operator's evTaler in '{currency}'!";
                return false;
            }

            #endregion

            #region The times

            if (PayDeadline < Timestamp || RefundDeadline <= PayDeadline)
            {
                ErrorResponse = "A step is paid after it is made and refunded until after its payment deadline!";
                return false;
            }

            if (AutoRefund is null || AutoRefund.Value <= TimeSpan.Zero)
            {
                ErrorResponse = "The wallet must pick the refund of a step up by itself: contract terms of a step have an auto refund!";
                return false;
            }

            #endregion

            #region The signed meter value at the start

            EVTalerVerifiedMeterValue? start = Previous?.Start;

            if (Step.StartMeterValue is not null && start is null)
            {

                if (MeterValueVerifier is null)
                {
                    ErrorResponse = "From step 2 on, the signed meter value at the start is verified: a transparency software is needed!";
                    return false;
                }

                if (!EVTalerSignedMeterValue.TryVerify(Station, Step.EVSEId, Step.StartMeterValue, MeterValueVerifier, out start, out ErrorResponse))
                    return false;

            }

            #endregion

            #region The amount

            Decimal? stepPrice    = null;
            Decimal? flatFee      = null;
            Decimal? reserve      = null;
            var      amountCheck  = EVTalerAmountChecks.Unsupported;

            if (!EVTalerRates.TryOf(currency, tariff, elements, out var rates, out var reason))
                diagnostics.Add($"The amount is not recalculated: {reason}.");

            else
            {

                if (!rates.TryGetStepPrice(Step.Covers, out var covered, out ErrorResponse))
                    return false;

                var estimate  = Step.Covers.Unit == EVTalerCovers.KWh
                                    ? rates.TimePerHour.HasValue
                                    : rates.EnergyPerKWh.HasValue;

                var base_     = covered + (Step.Step == 1 ? rates.FlatFee ?? 0m : 0m);

                if (Amount < base_ || (Step.Step > 1 && !estimate && Amount != base_))
                {
                    ErrorResponse = $"The amount {EVTalerAmounts.Write(Amount, currency)} {currency} of step {Step.Step} is not what it covers, {Step.Covers}, at the price of the session: {EVTalerAmounts.Write(base_, currency)} {currency}{(Step.Step == 1 ? ", the flat fee and the reserve" : "")}!";
                    return false;
                }

                stepPrice    = covered;
                flatFee      = Step.Step == 1 ? rates.FlatFee ?? 0m : null;
                reserve      = Step.Step == 1 ? Amount - base_      : null;
                amountCheck  = estimate ? EVTalerAmountChecks.Estimate : EVTalerAmountChecks.Exact;

                if (estimate)
                    diagnostics.Add("The session is priced by energy and time: the amount of a step is an estimate, settled at the end.");

            }

            #endregion

            Verified       = new EVTalerVerifiedStep(this,
                                                     merchant,
                                                     tariff,
                                                     currency,
                                                     price,
                                                     elements,
                                                     rates,
                                                     sessionStarted,
                                                     stepPrice,
                                                     flatFee,
                                                     reserve,
                                                     amountCheck,
                                                     start,
                                                     [.. diagnostics]);
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region (static) TryGetEVTalerTariff(Station, EVSEId, out Tariff, out ErrorResponse)

        /// <summary>
        /// The EV Taler tariff of the given EVSE, as the proof gives its tariffs (concepts/EVTaler.md, 4.3): the first of
        /// the type AD_HOC_PAYMENT, else the first without a type, with a currency, naming no payment means or EV_TALER
        /// among them.
        /// </summary>
        public static Boolean TryGetEVTalerTariff(VerifiedChargingStation           Station,
                                                  String                            EVSEId,
                                                  [NotNullWhen(true)]  out JObject? Tariff,
                                                  [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Tariff = null;

            var tariffs  = Station.Tariffs.TryGetValue(EVSEId, out var ofEVSE) ? ofEVSE : [];

            static String? TypeOf(JObject Tariff)
                => Tariff[ChargingTariff.PropertyKeys.TariffType]?.Type == JTokenType.String
                       ? Tariff[ChargingTariff.PropertyKeys.TariffType]!.Value<String>()
                       : null;

            var tariff   = tariffs.FirstOrDefault(tariff => TypeOf(tariff) == ChargingTariffType.AD_HOC_PAYMENT.ToString()) ??
                           tariffs.FirstOrDefault(tariff => TypeOf(tariff) is null);

            var means    = (tariff?[ChargingTariff.PropertyKeys.PaymentMeans] as JArray ?? []).
                               Where (mean => mean.Type == JTokenType.String).
                               Select(mean => mean.Value<String>()!).
                               ToArray();

            if (tariff is null ||
                tariff[ChargingTariff.PropertyKeys.Currency]?.Type != JTokenType.String ||
                (means.Length > 0 && !means.Contains(PaymentMeans.EV_TALER.ToString(), StringComparer.OrdinalIgnoreCase)))
            {
                ErrorResponse = $"The EVSE '{EVSEId}' has no EV Taler tariff in the proof!";
                return false;
            }

            Tariff         = tariff;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (private static) TryTime(JSON, Name, out Time, out ErrorResponse)

        private static Boolean TryTime(JObject                           JSON,
                                       String                            Name,
                                       out DateTimeOffset                Time,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Time = default;

            if (JSON[Name] is not JObject { Count: 1 } timestamp ||
                timestamp["t_s"] is not JValue { Type: JTokenType.Integer } seconds ||
                seconds.Value<Int64>() < 0)
            {
                ErrorResponse = $"The '{Name}' of contract terms is a time GNU Taler writes as {{ \"t_s\": … }}, not never!";
                return false;
            }

            Time           = DateTimeOffset.FromUnixTimeSeconds(seconds.Value<Int64>());
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
