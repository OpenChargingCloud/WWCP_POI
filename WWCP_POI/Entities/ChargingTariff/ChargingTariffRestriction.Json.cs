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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public partial class ChargingTariffRestriction
    {

        #region Parse/TryParse

        /// <summary>
        /// Parse tariff restriction JSON text, retaining decimal and duration precision.
        /// </summary>
        public static ChargingTariffRestriction Parse(String JSON)
            => Parse(InfrastructureJson.ReadObject(JSON));

        /// <summary>
        /// Parse date, time, energy, power, duration and weekday restrictions.
        /// </summary>
        public static ChargingTariffRestriction Parse(JObject JSON)
        {

            if (TryParse(JSON, out var result, out var error))
                return result;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to parse at least one restriction with valid bounds and TimeSpan tick precision.
        /// </summary>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out ChargingTariffRestriction?  result,
                                       [NotNullWhen(false)] out String?                     error)
        {

            result = null;
            error  = null;

            try
            {
                InfrastructureJson.ValidateFields(JSON,
                                                  "startTime", "endTime", "startDate", "endDate",
                                                  "minkWh", "maxkWh", "minPower", "maxPower",
                                                  "minDuration", "maxDuration", "day_of_week");

                var startTime   = ParseRestrictionTime(JSON, "startTime");
                var endTime     = ParseRestrictionTime(JSON, "endTime");
                var startDate   = InfrastructureJson.Date(JSON, "startDate");
                var endDate     = InfrastructureJson.Date(JSON, "endDate");

                if (startDate is null && endDate is not null)
                    throw new ArgumentException("startDate: required when endDate is supplied.");

                var minDuration = ParseRestrictionSeconds(JSON, "minDuration");
                var maxDuration = ParseRestrictionSeconds(JSON, "maxDuration");
                var weekdays    = InfrastructureJson.Array(JSON, "day_of_week", ParseRestrictionWeekday);

                result = new ChargingTariffRestriction(
                             Time:      startTime is null && endTime is null
                                            ? null
                                            : new TimeRange(startTime, endTime),
                             Date:      startDate is null
                                            ? null
                                            : new StartEndDateTime(startDate.Value, endDate),
                             kWh:       ParseRestrictionRange(JSON, "minkWh", "maxkWh"),
                             Power:     ParseRestrictionRange(JSON, "minPower", "maxPower"),
                             Duration:  minDuration is null && maxDuration is null
                                            ? null
                                            : new TimeSpanMinMax(minDuration, maxDuration),
                             DayOfWeek: weekdays);

                return true;
            }
            catch (Exception exception)
            {
                error = $"ChargingTariffRestriction: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Parse restriction values

        private static Time? ParseRestrictionTime(JObject JSON, String Field)
        {

            var text = InfrastructureJson.Text(JSON, Field);

            if (text is null)
                return null;

            if (!org.GraphDefined.Vanaheimr.Illias.Time.TryParse(text, out var time, out var error))
                throw new ArgumentException($"{Field}: {error}");

            return time;

        }

        private static DecimalMinMax? ParseRestrictionRange(JObject JSON, String MinField, String MaxField)
        {

            var min = TariffJson.Decimal(JSON, MinField);
            var max = TariffJson.Decimal(JSON, MaxField);

            return min is null && max is null
                       ? null
                       : new DecimalMinMax(min, max);

        }

        private static TimeSpan? ParseRestrictionSeconds(JObject JSON, String Field)
        {

            if (TariffJson.Decimal(JSON, Field) is not { } seconds)
                return null;

            var ticks = checked(seconds * TimeSpan.TicksPerSecond);

            if (ticks != Decimal.Truncate(ticks))
                throw new ArgumentException($"{Field}: exceeds TimeSpan precision.");

            return TimeSpan.FromTicks(checked((Int64) ticks));

        }

        private static DayOfWeek ParseRestrictionWeekday(JToken Token)
        {

            if (Token.Type != JTokenType.String ||
                !TariffJson.EnumValue(Token.Value<String>()!, out DayOfWeek day))
            {
                throw new ArgumentException("Invalid weekday.");
            }

            return day;

        }

        #endregion

    }

}
