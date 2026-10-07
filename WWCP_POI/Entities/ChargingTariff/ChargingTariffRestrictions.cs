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

using Newtonsoft.Json.Linq;
using System.Collections.Immutable;
using System.Globalization;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A charging tariff restrictions class.
    /// </summary>
    public sealed partial class ChargingTariffRestriction
    {

        #region Properties

        /// <summary>
        /// Start/end time of day, for example "13:30 - 19:45", valid from this time of the day.
        /// </summary>
        public TimeRange?              Time         { get; }

        /// <summary>
        /// Start/end date, for example: 2015-12-24, valid from this day until that day (excluding that day).
        /// </summary>
        private readonly StartEndDateTime? date;
        public StartEndDateTime? Date => date is null ? null : new(date.StartTime, date.EndTime);

        /// <summary>
        /// Minimum/maximum used energy as typed watt-hour quantities.
        /// </summary>
        public Range<WattHour?>?       Energy       { get; }

        /// <summary>
        /// Minimum/maximum charging power as typed watt quantities.
        /// </summary>
        public Range<Watt?>?           Power        { get; }

        /// <summary>
        /// Minimum/Maximum duration in seconds, valid for a duration from x seconds.
        /// </summary>
        public TimeSpanMinMax?         Duration     { get; }

        /// <summary>
        /// Minimum/Maximum duration in seconds, valid for a duration from x seconds.
        /// </summary>
        public IEnumerable<DayOfWeek>  DayOfWeek    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new tariff restrictions class.
        /// </summary>
        /// <param name="Time">Start/end time of day, for example "13:30 - 19:45", valid from this time of the day.</param>
        /// <param name="Date">Start/end date, for example: 2015-12-24, valid from this day until that day (excluding that day).</param>
        /// <param name="Energy">Minimum/maximum used energy as typed watt-hour quantities.</param>
        /// <param name="Power">Minimum/maximum charging power as typed watt quantities.</param>
        /// <param name="Duration">Minimum/Maximum duration in seconds, valid for a duration from x seconds.</param>
        /// <param name="DayOfWeek">Minimum/Maximum duration in seconds, valid for a duration from x seconds.</param>
        public ChargingTariffRestriction(TimeRange?               Time        = null,
                                         StartEndDateTime?        Date        = null,
                                         Range<WattHour?>?        Energy      = null,
                                         Range<Watt?>?            Power       = null,
                                         TimeSpanMinMax?          Duration    = null,
                                         IEnumerable<DayOfWeek>?  DayOfWeek   = null)
        {

            #region Initial checks

            var days = DayOfWeek?.Distinct().ToImmutableArray() ?? [];
            if (days.Any(day => !Enum.IsDefined(day)))
                throw new ArgumentException("Invalid weekday.", nameof(DayOfWeek));
            if (!(Time?.StartTime.HasValue == true || Time?.EndTime.HasValue == true || Date is not null ||
                  Energy?.Min.HasValue == true || Energy?.Max.HasValue == true || Power?.Min.HasValue == true || Power?.Max.HasValue == true ||
                  Duration?.Min.HasValue == true || Duration?.Max.HasValue == true || !days.IsEmpty))
                throw new ArgumentException("At least one tariff restriction is required.");
            if (Energy?.Min < WattHour.AdditiveIdentity || Energy?.Max < WattHour.AdditiveIdentity || Energy?.Min > Energy?.Max ||
                Power?.Min < Watt.AdditiveIdentity || Power?.Max < Watt.AdditiveIdentity || Power?.Min > Power?.Max)
                throw new ArgumentException("Energy and power restrictions must be nonnegative and ordered.");
            if (Duration?.Min < TimeSpan.Zero || Duration?.Max < TimeSpan.Zero || Duration?.Min > Duration?.Max)
                throw new ArgumentException("Duration restrictions must be nonnegative and ordered.");

            #endregion

            this.Time       = Time;
            this.date       = Date is null ? null : new(Date.StartTime, Date.EndTime);
            this.Energy     = Energy;
            this.Power      = Power;
            this.Duration   = Duration;
            this.DayOfWeek  = days;

        }

        #endregion


        #region (static) MinEnergy(MinEnergy)

        /// <summary>
        /// Create a new MinEnergy tariff restriction.
        /// </summary>
        /// <param name="MinEnergy">The minimum energy quantity.</param>
        public static ChargingTariffRestriction MinEnergy(WattHour MinEnergy)
            => new (Energy: new Range<WattHour?>(MinEnergy, null));

        #endregion

        #region (static) MaxEnergy(MaxEnergy)

        /// <summary>
        /// Create a new MaxEnergy tariff restriction.
        /// </summary>
        /// <param name="MaxEnergy">The maximum energy quantity.</param>
        public static ChargingTariffRestriction MaxEnergy(WattHour MaxEnergy)
            => new (Energy: new Range<WattHour?>(null, MaxEnergy));

        #endregion

        #region (static) MinPower(MinPower)

        /// <summary>
        /// Create a new MinPower tariff restriction.
        /// </summary>
        /// <param name="MinPower">The minimum power value.</param>
        public static ChargingTariffRestriction MinPower(Watt MinPower)
            => new (Power: new Range<Watt?>(MinPower, null));

        #endregion

        #region (static) MaxPower(MaxPower)

        /// <summary>
        /// Create a new MaxPower tariff restriction.
        /// </summary>
        /// <param name="MaxPower">The maximum power value.</param>
        public static ChargingTariffRestriction MaxPower(Watt MaxPower)
            => new (Power: new Range<Watt?>(null, MaxPower));

        #endregion

        #region (static) MinDuration(MinDuration)

        /// <summary>
        /// Create a new MinDuration tariff restriction.
        /// </summary>
        /// <param name="MinDuration">The minimum Duration value.</param>
        public static ChargingTariffRestriction MinDuration(TimeSpan MinDuration)
            => new (Duration: TimeSpanMinMax.FromMin(MinDuration));

        #endregion

        #region (static) MinDuration(MaxDuration)

        /// <summary>
        /// Create a new MaxDuration tariff restriction.
        /// </summary>
        /// <param name="MaxDuration">The maximum Duration value.</param>
        public static ChargingTariffRestriction MaxDuration(TimeSpan MaxDuration)
            => new (Duration: TimeSpanMinMax.FromMax(MaxDuration));

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        public JObject ToJSON()

            => POIRepresentation.AddETags(this, JSONObject.Create(

                   //new JProperty("type",       _Type.    ToString()),
                   //new JProperty("type", _Type.ToString()),
                   //new JProperty("price",      _Price.   ToString()),

                   Time. HasValue && Time. Value.StartTime.HasValue ? new JProperty("startTime",  Time. Value.StartTime.Value.ToString())       : null,
                   Time. HasValue && Time. Value.EndTime.  HasValue ? new JProperty("endTime",    Time. Value.EndTime.  Value.ToString())       : null,

                   date is not null ? new JProperty("startDate", date.StartTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)) : null,
                   date?.EndTime is { } endDate ? new JProperty("endDate", endDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)) : null,
                   Duration?.Min is { } minDuration ? new JProperty("minDuration", MetrologyJson.DurationText(minDuration)) : null,
                   Duration?.Max is { } maxDuration ? new JProperty("maxDuration", MetrologyJson.DurationText(maxDuration)) : null,
                   Energy.  HasValue && Energy.  Value.Min.      HasValue ? new JProperty("minEnergy", MetrologyJson.Text(Energy.Value.Min.Value)) : null,
                   Energy.  HasValue && Energy.  Value.Max.      HasValue ? new JProperty("maxEnergy", MetrologyJson.Text(Energy.Value.Max.Value)) : null,

                   Power.HasValue && Power.Value.Min.      HasValue ? new JProperty("minPower", MetrologyJson.Text(Power.Value.Min.Value)) : null,
                   Power.HasValue && Power.Value.Max.      HasValue ? new JProperty("maxPower", MetrologyJson.Text(Power.Value.Max.Value)) : null,

                   DayOfWeek.Any()
                       ? new JProperty("daysOfWeek",
                                       new JArray(DayOfWeek.Select(day => day.ToString().ToUpperInvariant())))
                       : null

               ));

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this object.
        /// </summary>
        public ChargingTariffRestriction Clone()

            => new (Time,
                    Date,
                    Energy,
                    Power,
                    Duration,
                    DayOfWeek);

        #endregion


        #region (override) GetHashCode()

        /// <summary>
        /// Get the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return Time.     GetHashCode() * 41 ^
                       (Date?.GetHashCode() ?? 0) * 37 ^
                       Energy.      GetHashCode() * 31 ^
                       Power.    GetHashCode() * 23 ^
                       Duration. GetHashCode() * 17 ^
                       DayOfWeek.GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        ///// <summary>
        ///// Get a string representation of this object.
        ///// </summary>
        //public override String ToString()
        //{
        //    return String.Concat("type: ", _Type.ToString(), ", price: ", _Price.ToString(), ", step size:", _StepSize.ToString());
        //}

        #endregion

    }

}
