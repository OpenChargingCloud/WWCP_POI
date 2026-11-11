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
using System.Globalization;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Amounts of EV Taler: GNU Taler's amounts, "EUR:3.25", and the decimals WWCP writes as texts, "3.25", with the
    /// rounding of concepts/EVTaler.md (open point 3): a step up to the minor unit of its currency, the price of a session
    /// to the nearest minor unit, half away from zero.
    /// </summary>
    public static partial class EVTalerAmounts
    {

        #region Data

        /// <summary>
        /// The ISO 4217 codes whose minor unit is not the hundredth.
        /// </summary>
        private static readonly IReadOnlyDictionary<String, Int32> MinorUnits = new Dictionary<String, Int32>(StringComparer.Ordinal) {
                                                                                    ["BIF"] = 0, ["CLP"] = 0, ["DJF"] = 0, ["GNF"] = 0, ["ISK"] = 0, ["JPY"] = 0,
                                                                                    ["KMF"] = 0, ["KRW"] = 0, ["PYG"] = 0, ["RWF"] = 0, ["UGX"] = 0, ["UYI"] = 0,
                                                                                    ["VND"] = 0, ["VUV"] = 0, ["XAF"] = 0, ["XOF"] = 0, ["XPF"] = 0,
                                                                                    ["BHD"] = 3, ["IQD"] = 3, ["JOD"] = 3, ["KWD"] = 3, ["LYD"] = 3, ["OMR"] = 3,
                                                                                    ["TND"] = 3,
                                                                                    ["CLF"] = 4, ["UYW"] = 4
                                                                                };

        /// <summary>
        /// The largest integer part of a GNU Taler amount, 2^52.
        /// </summary>
        private const Decimal MaxTalerValue = 4503599627370496m;

        [GeneratedRegex("^([A-Za-z]{1,11}):((?:0|[1-9][0-9]*)(?:\\.[0-9]{1,8})?)$")]
        private static partial Regex TalerAmount();

        [GeneratedRegex("^(?:0|[1-9][0-9]*)(?:\\.[0-9]+)?$")]
        private static partial Regex DecimalText();

        #endregion


        #region MinorUnitDigits(Currency)

        /// <summary>
        /// The number of decimal digits of the minor unit of the given ISO 4217 currency: 2 unless it is known otherwise.
        /// </summary>
        /// <param name="Currency">An ISO 4217 code.</param>
        public static Int32 MinorUnitDigits(String Currency)

            => MinorUnits.TryGetValue(Currency, out var digits)
                   ? digits
                   : 2;

        #endregion

        #region RoundUp(Value, Currency)

        /// <summary>
        /// The given value rounded up to the minor unit of the given currency, as the amount of a step.
        /// </summary>
        public static Decimal RoundUp(Decimal  Value,
                                      String   Currency)

            => Math.Round(Value, MinorUnitDigits(Currency), MidpointRounding.ToPositiveInfinity);

        #endregion

        #region Round(Value, Currency)

        /// <summary>
        /// The given value rounded to the nearest minor unit of the given currency, half away from zero, as the price of a
        /// session.
        /// </summary>
        public static Decimal Round(Decimal  Value,
                                    String   Currency)

            => Math.Round(Value, MinorUnitDigits(Currency), MidpointRounding.AwayFromZero);

        #endregion

        #region Write(Value, Currency)

        /// <summary>
        /// The given amount as a text with the digits of the minor unit of the given currency, e.g. "3.20", or with all of
        /// its digits where it has more, as GNU Taler's amounts may.
        /// </summary>
        public static String Write(Decimal  Value,
                                   String   Currency)
        {

            var digits = MinorUnitDigits(Currency);

            return Math.Round(Value, digits) == Value
                       ? Value.ToString("F" + digits, CultureInfo.InvariantCulture)
                       : WriteDecimal(Value);

        }

        #endregion

        #region WriteDecimal(Value)

        /// <summary>
        /// The given value as a text without trailing zeros, e.g. "13.412".
        /// </summary>
        public static String WriteDecimal(Decimal Value)
        {

            var text = Value.ToString("0.############################", CultureInfo.InvariantCulture);

            return text == "-0" ? "0" : text;

        }

        #endregion

        #region TryParseTaler(Text, out Currency, out Value, out ErrorResponse)

        /// <summary>
        /// Try to read the given amount as GNU Taler writes it, e.g. "EUR:3.25": a currency of at most 11 letters, no sign,
        /// at most 8 fractional digits, an integer part of at most 2^52.
        /// </summary>
        public static Boolean TryParseTaler(String?                           Text,
                                            [NotNullWhen(true)]  out String?  Currency,
                                            out Decimal                       Value,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Currency  = null;
            Value     = 0;

            var match = Text is not null ? TalerAmount().Match(Text) : null;

            if (match is null || !match.Success ||
                !Decimal.TryParse(match.Groups[2].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out Value) ||
                Math.Truncate(Value) > MaxTalerValue)
            {
                ErrorResponse = $"'{Text}' is no amount as GNU Taler writes it, e.g. 'EUR:3.25'!";
                return false;
            }

            Currency       = match.Groups[1].Value;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseDecimal(JSON, Name, out Value, out ErrorResponse)

        /// <summary>
        /// Try to read the given property as a decimal written as a text, not negative, e.g. "13.412".
        /// </summary>
        public static Boolean TryParseDecimal(JObject                           JSON,
                                              String                            Name,
                                              out Decimal                       Value,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = 0;

            if (JSON[Name] is not JValue { Type: JTokenType.String } text ||
                !DecimalText().IsMatch(text.Value<String>()!) ||
                !Decimal.TryParse(text.Value<String>(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out Value))
            {
                ErrorResponse = $"The '{Name}' must be a decimal written as a text, not negative, e.g. \"3.25\"!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }


    /// <summary>
    /// The gross rates an EV Taler session is priced by, where its prices are those of the simple profile: one element,
    /// without restrictions, of ENERGY, TIME and FLAT components each at most once, with their VAT added; no minimum or
    /// maximum price. A session of other prices is paid in steps all the same, but its amounts and its price are not
    /// recalculated by a reader.
    /// </summary>
    /// <param name="Currency">The currency of the tariff.</param>
    /// <param name="EnergyPerKWh">The gross price of a kWh, if the tariff prices energy.</param>
    /// <param name="EnergyStepWh">The billing increment of energy, in Wh.</param>
    /// <param name="TimePerHour">The gross price of an hour of charging, if the tariff prices time.</param>
    /// <param name="TimeStepSeconds">The billing increment of time, in seconds.</param>
    /// <param name="FlatFee">The gross flat fee, if any.</param>
    public sealed record EVTalerRates(String    Currency,
                                      Decimal?  EnergyPerKWh,
                                      Decimal?  EnergyStepWh,
                                      Decimal?  TimePerHour,
                                      Decimal?  TimeStepSeconds,
                                      Decimal?  FlatFee)
    {

        #region (static) TryOf(Currency, Tariff, Elements, out Rates, out Reason)

        /// <summary>
        /// The rates of the simple profile of the given elements of the given charging tariff, or why there are none.
        /// </summary>
        /// <param name="Currency">The currency of the tariff.</param>
        /// <param name="Tariff">The charging tariff, as a proof gives it.</param>
        /// <param name="Elements">The elements that price it: of a price statement, or else its own.</param>
        /// <param name="Rates">The rates.</param>
        /// <param name="Reason">Why the elements are not of the simple profile.</param>
        public static Boolean TryOf(String                                  Currency,
                                    JObject                                 Tariff,
                                    IReadOnlyList<JObject>                  Elements,
                                    [NotNullWhen(true)]  out EVTalerRates?  Rates,
                                    [NotNullWhen(false)] out String?        Reason)
        {

            Rates = null;

            if (Tariff[ChargingTariff.PropertyKeys.MinPrice] is not null || Tariff[ChargingTariff.PropertyKeys.MaxPrice] is not null)
            {
                Reason = "the charging tariff has a minimum or maximum price";
                return false;
            }

            if (Elements.Count != 1)
            {
                Reason = "the session is priced by more than one element";
                return false;
            }

            var element = Elements[0];

            if (element.Properties().Any(property => property.Name is not "priceComponents" and not "restrictions") ||
                (element["restrictions"] is JToken restrictions && (restrictions is not JObject restrictionsJSON || restrictionsJSON.HasValues)) ||
                element["priceComponents"] is not JArray components || components.Count == 0)
            {
                Reason = "the element of the session has restrictions";
                return false;
            }

            Decimal? energy = null, energyStep = null, time = null, timeStep = null, flat = null;

            foreach (var item in components)
            {

                if (item is not JObject component ||
                    component.Properties().Any(property => property.Name is not "type" and not "price" and not "vat" and not "stepSize") ||
                    component["price"] is not JValue { Type: JTokenType.Integer or JTokenType.Float } priceJSON)
                {
                    Reason = "a price component is not one of the simple profile";
                    return false;
                }

                var price = priceJSON.AsDecimal();
                var vat   = component["vat"] is JValue { Type: JTokenType.Integer or JTokenType.Float } vatJSON
                                ? vatJSON.AsDecimal()
                                : 0m;
                var step  = component["stepSize"] is JValue { Type: JTokenType.Integer or JTokenType.Float } stepJSON
                                ? stepJSON.AsDecimal()
                                : (Decimal?) null;
                var gross = price * (100m + vat) / 100m;

                if (price < 0 || vat < 0 || vat > 100)
                {
                    Reason = "a price component has a negative price or a VAT outside 0..100";
                    return false;
                }

                switch (component["type"]?.Type == JTokenType.String ? component["type"]!.Value<String>() : null)
                {

                    case "ENERGY" when energy is null && step > 0:
                        energy      = gross;
                        energyStep  = step;
                        break;

                    case "TIME"   when time   is null && step > 0:
                        time        = gross;
                        timeStep    = step;
                        break;

                    case "FLAT"   when flat   is null && step is null:
                        flat        = gross;
                        break;

                    default:
                        Reason = "a price component is not ENERGY, TIME or FLAT once each, with its step size";
                        return false;

                }

            }

            Rates   = new EVTalerRates(Currency, energy, energyStep, time, timeStep, flat);
            Reason  = null;
            return true;

        }

        #endregion

        #region TryGetStepPrice(Covers, out Price, out ErrorResponse)

        /// <summary>
        /// The price of what a step covers, rounded up to the minor unit of the currency: its kWh at the price of energy, its
        /// minutes at the price of time.
        /// </summary>
        public Boolean TryGetStepPrice(EVTalerCovers                     Covers,
                                       out Decimal                       Price,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Price = 0;

            var quantity = Covers.To - Covers.From;

            // Multiplied before divided: a decimal holds no rate of a minute such as 0.40 / 60 exactly, and rounding up
            // would take its last digit for a cent.
            var price    = Covers.Unit switch {
                               EVTalerCovers.KWh      => quantity * EnergyPerKWh,
                               EVTalerCovers.Minutes  => quantity * TimePerHour / 60m,
                               _                      => null
                           };

            if (price is null)
            {
                ErrorResponse = $"The charging tariff does not price what a step covers, '{Covers.Unit}'!";
                return false;
            }

            Price          = EVTalerAmounts.RoundUp(price.Value, Currency);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region SessionPrice(Energy, Duration)

        /// <summary>
        /// The price of a session of the given energy and duration: the flat fee, the energy and the time each billed in
        /// its increments, rounded to the nearest minor unit of the currency.
        /// </summary>
        /// <param name="Energy">The energy, in kWh: the end value less the start value.</param>
        /// <param name="Duration">The time from the start value to the end value.</param>
        public Decimal SessionPrice(Decimal   Energy,
                                    TimeSpan  Duration)
        {

            var price = FlatFee ?? 0m;

            if (EnergyPerKWh.HasValue)
                price += EnergyPerKWh.Value * Billed(Energy * 1000m, EnergyStepWh!.Value) / 1000m;

            if (TimePerHour.HasValue)
                price += TimePerHour.Value * Billed(Duration.TotalSeconds.AsDecimal(), TimeStepSeconds!.Value) / 3600m;

            return EVTalerAmounts.Round(price, Currency);

        }

        #endregion


        #region (private static) Billed(Quantity, Step)

        private static Decimal Billed(Decimal  Quantity,
                                      Decimal  Step)

            => Math.Ceiling(Quantity / Step) * Step;

        #endregion

    }

}
