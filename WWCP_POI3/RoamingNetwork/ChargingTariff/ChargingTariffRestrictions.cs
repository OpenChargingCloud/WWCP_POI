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
using System.Globalization;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Which part of a reservation a charging tariff element prices, as in OCPI.
    /// </summary>
    public enum ChargingReservationRestriction
    {

        /// <summary>
        /// The reservation, from its start until the charging starts; e.g. with TIME per hour reserved, or a FLAT fee.
        /// </summary>
        RESERVATION,

        /// <summary>
        /// A reservation that expired: no charging session followed it.
        /// </summary>
        RESERVATION_EXPIRES

    }


    /// <summary>
    /// When a charging tariff element applies, as in OCPI: an immutable value without an identification.
    /// All given restrictions must hold. Times are local times of the location ("HH:mm"), dates are "yyyy-MM-dd";
    /// energy is in Wh, current in A, power in W, durations in seconds.
    /// </summary>
    public sealed class ChargingTariffRestrictions
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/chargingTariffRestrictions/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context       = "@context";

            /// <summary>
            /// The property of its start time.
            /// </summary>
            public const String  StartTime     = "startTime";

            /// <summary>
            /// The property of its end time.
            /// </summary>
            public const String  EndTime       = "endTime";

            /// <summary>
            /// The property of its start date.
            /// </summary>
            public const String  StartDate     = "startDate";

            /// <summary>
            /// The property of its end date.
            /// </summary>
            public const String  EndDate       = "endDate";

            /// <summary>
            /// The property of its minimum energy.
            /// </summary>
            public const String  MinEnergy     = "minEnergy";

            /// <summary>
            /// The property of its maximum energy.
            /// </summary>
            public const String  MaxEnergy     = "maxEnergy";

            /// <summary>
            /// The property of its minimum current.
            /// </summary>
            public const String  MinCurrent    = "minCurrent";

            /// <summary>
            /// The property of its maximum current.
            /// </summary>
            public const String  MaxCurrent    = "maxCurrent";

            /// <summary>
            /// The property of its minimum power.
            /// </summary>
            public const String  MinPower      = "minPower";

            /// <summary>
            /// The property of its maximum power.
            /// </summary>
            public const String  MaxPower      = "maxPower";

            /// <summary>
            /// The property of its minimum duration.
            /// </summary>
            public const String  MinDuration   = "minDuration";

            /// <summary>
            /// The property of its maximum duration.
            /// </summary>
            public const String  MaxDuration   = "maxDuration";

            /// <summary>
            /// The property of its days of the week.
            /// </summary>
            public const String  DaysOfWeek    = "daysOfWeek";

            /// <summary>
            /// The property of its reservation restriction.
            /// </summary>
            public const String  Reservation   = "reservation";

        }

        #endregion


        /// <summary>
        /// From this time of day on, inclusive.
        /// </summary>
        public TimeOnly?                   StartTime      { get; }

        /// <summary>
        /// Until this time of day, exclusive; before the start time it means the next day.
        /// </summary>
        public TimeOnly?                   EndTime        { get; }

        /// <summary>
        /// From this date on, inclusive.
        /// </summary>
        public DateOnly?                   StartDate      { get; }

        /// <summary>
        /// Until this date, exclusive.
        /// </summary>
        public DateOnly?                   EndDate        { get; }

        /// <summary>
        /// The minimum energy charged so far.
        /// </summary>
        public Decimal?                    MinEnergy      { get; }

        /// <summary>
        /// The maximum energy charged so far.
        /// </summary>
        public Decimal?                    MaxEnergy      { get; }

        /// <summary>
        /// The minimum current.
        /// </summary>
        public Decimal?                    MinCurrent     { get; }

        /// <summary>
        /// The maximum current.
        /// </summary>
        public Decimal?                    MaxCurrent     { get; }

        /// <summary>
        /// The minimum power.
        /// </summary>
        public Decimal?                    MinPower       { get; }

        /// <summary>
        /// The maximum power.
        /// </summary>
        public Decimal?                    MaxPower       { get; }

        /// <summary>
        /// The minimum duration of the session so far.
        /// </summary>
        public Decimal?                    MinDuration    { get; }

        /// <summary>
        /// The maximum duration of the session so far.
        /// </summary>
        public Decimal?                    MaxDuration    { get; }

        /// <summary>
        /// The days of the week, from Monday to Sunday.
        /// </summary>
        public ImmutableArray<DayOfWeek>   DaysOfWeek     { get; }

        /// <summary>
        /// Whether the element prices a reservation rather than charging, and which part of it.
        /// </summary>
        public ChargingReservationRestriction?  Reservation  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create new restrictions of a charging tariff element.
        /// </summary>
        public ChargingTariffRestrictions(TimeOnly?                StartTime     = null,
                                          TimeOnly?                EndTime       = null,
                                          DateOnly?                StartDate     = null,
                                          DateOnly?                EndDate       = null,
                                          Decimal?                 MinEnergy     = null,
                                          Decimal?                 MaxEnergy     = null,
                                          Decimal?                 MinCurrent    = null,
                                          Decimal?                 MaxCurrent    = null,
                                          Decimal?                 MinPower      = null,
                                          Decimal?                 MaxPower      = null,
                                          Decimal?                 MinDuration   = null,
                                          Decimal?                 MaxDuration   = null,
                                          IEnumerable<DayOfWeek>?  DaysOfWeek    = null,
                                          ChargingReservationRestriction?  Reservation  = null)
        {

            CheckRange(MinEnergy,   MaxEnergy,   "energy");
            CheckRange(MinCurrent,  MaxCurrent,  "current");
            CheckRange(MinPower,    MaxPower,    "power");
            CheckRange(MinDuration, MaxDuration, "duration");

            if (StartTime.HasValue && StartTime.Value.Second + StartTime.Value.Millisecond != 0 ||
                EndTime.  HasValue && EndTime.  Value.Second + EndTime.  Value.Millisecond != 0)
            {
                throw new ArgumentException("The start and end times of a charging tariff restriction are given in minutes!");
            }

            // Equal times would be ambiguous: no time at all, or the whole day.
            if (StartTime.HasValue && StartTime == EndTime)
                throw new ArgumentException("The start and end times of a charging tariff restriction must differ!");

            // The end date is exclusive: a period is at least one day.
            if (StartDate >= EndDate)
                throw new ArgumentException("The start date of a charging tariff restriction must be before its end date!");

            var daysOfWeek = (DaysOfWeek ?? []).ToArray();

            if (daysOfWeek.Distinct().Count() != daysOfWeek.Length)
                throw new ArgumentException("A day of the week of a charging tariff restriction must not be given twice!");

            this.StartTime    = StartTime;
            this.EndTime      = EndTime;
            this.StartDate    = StartDate;
            this.EndDate      = EndDate;
            this.MinEnergy    = MinEnergy;
            this.MaxEnergy    = MaxEnergy;
            this.MinCurrent   = MinCurrent;
            this.MaxCurrent   = MaxCurrent;
            this.MinPower     = MinPower;
            this.MaxPower     = MaxPower;
            this.MinDuration  = MinDuration;
            this.MaxDuration  = MaxDuration;
            // From Monday to Sunday.
            this.DaysOfWeek   = [.. daysOfWeek.OrderBy(day => ((Int32) day + 6) % 7)];
            this.Reservation  = Reservation;

        }

        #endregion


        #region ToJSON(Embedded = false, CustomChargingTariffRestrictionsSerializer = null)

        /// <summary>
        /// Return the JSON representation of these restrictions.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingTariffRestrictionsSerializer">A delegate to serialize custom charging tariff restrictions.</param>
        public JObject ToJSON(Boolean                                                       Embedded                                     = false,
                              CustomJObjectSerializerDelegate<ChargingTariffRestrictions>?  CustomChargingTariffRestrictionsSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,      JSONLDContext);

            if (StartTime.HasValue)
                json.Add(PropertyKeys.StartTime,    StartTime.Value.ToString("HH:mm", CultureInfo.InvariantCulture));

            if (EndTime.HasValue)
                json.Add(PropertyKeys.EndTime,      EndTime.  Value.ToString("HH:mm", CultureInfo.InvariantCulture));

            if (StartDate.HasValue)
                json.Add(PropertyKeys.StartDate,    StartDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (EndDate.HasValue)
                json.Add(PropertyKeys.EndDate,      EndDate.  Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (MinEnergy.HasValue)
                json.Add(PropertyKeys.MinEnergy,    POIJSON.Number(MinEnergy.Value));

            if (MaxEnergy.HasValue)
                json.Add(PropertyKeys.MaxEnergy,    POIJSON.Number(MaxEnergy.Value));

            if (MinCurrent.HasValue)
                json.Add(PropertyKeys.MinCurrent,   POIJSON.Number(MinCurrent.Value));

            if (MaxCurrent.HasValue)
                json.Add(PropertyKeys.MaxCurrent,   POIJSON.Number(MaxCurrent.Value));

            if (MinPower.HasValue)
                json.Add(PropertyKeys.MinPower,     POIJSON.Number(MinPower.Value));

            if (MaxPower.HasValue)
                json.Add(PropertyKeys.MaxPower,     POIJSON.Number(MaxPower.Value));

            if (MinDuration.HasValue)
                json.Add(PropertyKeys.MinDuration,  POIJSON.Number(MinDuration.Value));

            if (MaxDuration.HasValue)
                json.Add(PropertyKeys.MaxDuration,  POIJSON.Number(MaxDuration.Value));

            if (DaysOfWeek.Length > 0)
                json.Add(PropertyKeys.DaysOfWeek,   new JArray(DaysOfWeek.Select(day => day.ToString().ToUpperInvariant())));

            if (Reservation.HasValue)
                json.Add(PropertyKeys.Reservation,  Reservation.Value.ToString());

            return CustomChargingTariffRestrictionsSerializer is not null
                       ? CustomChargingTariffRestrictionsSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingTariffRestrictions, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of restrictions.
        /// </summary>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out ChargingTariffRestrictions?  ChargingTariffRestrictions,
                                       [NotNullWhen(false)] out String?                      ErrorResponse)
        {

            ChargingTariffRestrictions = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging tariff restrictions must be '{JSONLDContext}'!";
                    return false;
                }

                if (!TryParseText(JSON, PropertyKeys.StartTime, "HH:mm",      TimeOnly.TryParseExact, out TimeOnly? startTime, out ErrorResponse) ||
                    !TryParseText(JSON, PropertyKeys.EndTime,   "HH:mm",      TimeOnly.TryParseExact, out TimeOnly? endTime,   out ErrorResponse) ||
                    !TryParseText(JSON, PropertyKeys.StartDate, "yyyy-MM-dd", DateOnly.TryParseExact, out DateOnly? startDate, out ErrorResponse) ||
                    !TryParseText(JSON, PropertyKeys.EndDate,   "yyyy-MM-dd", DateOnly.TryParseExact, out DateOnly? endDate,   out ErrorResponse))
                {
                    return false;
                }

                var values = new Dictionary<String, Decimal?>();

                foreach (var name in new[] { PropertyKeys.MinEnergy,    PropertyKeys.MaxEnergy,
                                             PropertyKeys.MinCurrent,   PropertyKeys.MaxCurrent,
                                             PropertyKeys.MinPower,     PropertyKeys.MaxPower,
                                             PropertyKeys.MinDuration,  PropertyKeys.MaxDuration })
                {

                    values[name] = null;

                    if (JSON.ParseOptional(name, name, out Decimal? value, out ErrorResponse))
                    {
                        if (ErrorResponse is not null)
                            return false;
                        values[name] = value;
                    }

                }

                var daysOfWeek = new List<DayOfWeek>();

                if (JSON[PropertyKeys.DaysOfWeek] is JToken daysOfWeekJSON)
                {

                    if (daysOfWeekJSON is not JArray daysOfWeekArray)
                    {
                        ErrorResponse = "The days of the week must be a JSON array!";
                        return false;
                    }

                    foreach (var day in daysOfWeekArray)
                    {

                        var text = day.Type == JTokenType.String ? day.Value<String>() : null;

                        if (text is null || text != text.ToUpperInvariant() ||
                            !Enum.TryParse<DayOfWeek>(text, ignoreCase: true, out var dayOfWeek) ||
                            !Enum.IsDefined(dayOfWeek) || Char.IsDigit(text[0]))
                        {
                            ErrorResponse = $"Invalid day of the week '{day}'!";
                            return false;
                        }

                        daysOfWeek.Add(dayOfWeek);

                    }

                }

                if (!POIJSON.TryParseEnum(JSON, PropertyKeys.Reservation, "reservation restriction", out ChargingReservationRestriction? reservation, out ErrorResponse))
                    return false;

                ChargingTariffRestrictions = new ChargingTariffRestrictions(startTime,
                                                                            endTime,
                                                                            startDate,
                                                                            endDate,
                                                                            values[PropertyKeys.MinEnergy],
                                                                            values[PropertyKeys.MaxEnergy],
                                                                            values[PropertyKeys.MinCurrent],
                                                                            values[PropertyKeys.MaxCurrent],
                                                                            values[PropertyKeys.MinPower],
                                                                            values[PropertyKeys.MaxPower],
                                                                            values[PropertyKeys.MinDuration],
                                                                            values[PropertyKeys.MaxDuration],
                                                                            daysOfWeek,
                                                                            reservation);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingTariffRestrictions  = null;
                ErrorResponse               = "Invalid charging tariff restrictions: " + e.Message;
                return false;
            }

        }

        #endregion


        #region (private) CheckRange(Min, Max, Description)

        private static void CheckRange(Decimal? Min, Decimal? Max, String Description)
        {

            if (Min < 0 || Max < 0)
                throw new ArgumentException($"The {Description} of a charging tariff restriction must not be negative!");

            if (Min > Max)
                throw new ArgumentException($"The minimum {Description} of a charging tariff restriction must not exceed its maximum!");

        }

        #endregion

        #region (private) TryParseText<T>(JSON, PropertyName, Format, Parser, out Value, out ErrorResponse)

        private delegate Boolean ExactParser<T>(String Text, String Format, IFormatProvider Provider, DateTimeStyles Styles, out T Value);

        private static Boolean TryParseText<T>(JObject                           JSON,
                                               String                            PropertyName,
                                               String                            Format,
                                               ExactParser<T>                    Parser,
                                               out T?                            Value,
                                               [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct

        {

            Value = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            // JSON text read with DateParseHandling.DateTime turns a date into a date token.
            var text = token.Type switch {
                           JTokenType.String                                                 => token.Value<String>(),
                           JTokenType.Date when ((JValue) token).Value is DateTime       date  => date.ToString(Format, CultureInfo.InvariantCulture),
                           JTokenType.Date when ((JValue) token).Value is DateTimeOffset date  => date.ToString(Format, CultureInfo.InvariantCulture),
                           _                                                                 => null
                       };

            if (text is null ||
                !Parser(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            {
                ErrorResponse = $"Invalid {PropertyName} '{token}', expected {Format}!";
                return false;
            }

            Value          = value;
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
