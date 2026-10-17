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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// One period of an energy mix prognosis: the expected composition
    /// from its start until its optional end, or else until the next period starts.
    /// </summary>
    public sealed class EnergyMixPrognosisPeriod : IEquatable<EnergyMixPrognosisPeriod>
    {

        #region Properties

        /// <summary>
        /// The start of this period.
        /// </summary>
        public DateTimeOffset                                   Start                   { get; }

        /// <summary>
        /// The optional end of this period; without one, it lasts until the next period starts.
        /// </summary>
        public DateTimeOffset?                                  End                     { get; }

        /// <summary>
        /// The expected energy sources; empty for an unknown composition.
        /// </summary>
        public IEnumerable<PercentageOf<EnergySourceCategory>>  EnergySources           { get; }

        /// <summary>
        /// The expected environmental impacts per kWh; empty for an unknown composition.
        /// </summary>
        public IEnumerable<EnvironmentalImpactAmount>           EnvironmentalImpacts    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new period of an energy mix prognosis.
        /// </summary>
        /// <param name="Start">The start of this period.</param>
        /// <param name="EnergySources">The expected energy sources; empty for an unknown composition.</param>
        /// <param name="EnvironmentalImpacts">The expected environmental impacts per kWh; empty for an unknown composition.</param>
        /// <param name="End">The optional end of this period.</param>
        public EnergyMixPrognosisPeriod(DateTimeOffset                                   Start,
                                        IEnumerable<PercentageOf<EnergySourceCategory>>  EnergySources,
                                        IEnumerable<EnvironmentalImpactAmount>           EnvironmentalImpacts,
                                        DateTimeOffset?                                  End   = null)
        {

            var sources  = (EnergySources        ?? []).ToImmutableArray();
            var impacts  = (EnvironmentalImpacts ?? []).ToImmutableArray();

            if (End.HasValue && End.Value <= Start)
                throw new ArgumentException("The end of an energy mix prognosis period must be after its start!", nameof(End));

            if (sources.Any(item => !Single.IsFinite(item.Percent) || item.Percent < 0 || item.Percent > 100))
                throw new ArgumentException("Each share of an energy mix prognosis period requires a percentage between 0 and 100!");

            // E.g. a default one: amounts are not negative by construction.
            if (impacts.Any(item => item.Category.IsNullOrEmpty))
                throw new ArgumentException("Each environmental impact of an energy mix prognosis period requires a category!");

            if (sources.Select(item => item.Value).Distinct().Count() != sources.Length ||
                impacts.Select(item => item.Category).Distinct().Count() != impacts.Length)
                throw new ArgumentException("A category must not be given twice within an energy mix prognosis period!");

            this.Start                 = Start;
            this.End                   = End;
            this.EnergySources         = sources;
            this.EnvironmentalImpacts  = impacts;

            unchecked
            {

                hashCode = this.Start.               GetHashCode()  * 7 ^
                          (this.End?.                GetHashCode() ?? 0) * 5 ^
                           this.EnergySources.       CalcHashCode() * 3 ^
                           this.EnvironmentalImpacts.CalcHashCode();

            }

        }

        #endregion


        #region (static) Parse   (JSON)

        /// <summary>
        /// Parse the given JSON representation of a period of an energy mix prognosis.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        public static EnergyMixPrognosisPeriod Parse(JObject JSON)
        {

            if (TryParse(JSON, out var period, out var errorResponse))
                return period;

            throw new ArgumentException("The given JSON representation of an energy mix prognosis period is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out Period, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a period of an energy mix prognosis.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Period">The parsed period.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out EnergyMixPrognosisPeriod?  Period,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)
        {

            Period         = null;
            ErrorResponse  = null;

            try
            {

                if (JSON is null)
                {
                    ErrorResponse = "The given JSON object must not be null!";
                    return false;
                }

                #region Parse Start                   [mandatory]

                if (!JSON.ParseMandatory("start",
                                         "start",
                                         out DateTimeOffset start,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse End                     [optional]

                if (JSON.ParseOptional("end",
                                       "end",
                                       out DateTimeOffset? end,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse EnergySources           [mandatory]

                if (!PercentageOfExtensions.TryParse<EnergySourceCategory>(JSON,
                                                                           "energySources",
                                                                           "energy source",
                                                                           EnergySourceCategory.TryParse,
                                                                           out var energySources,
                                                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse EnvironmentalImpacts    [mandatory]

                if (!EnvironmentalImpactAmountExtensions.TryParse(JSON,
                                                                  "environmentalImpacts",
                                                                  out var environmentalImpacts,
                                                                  out ErrorResponse))
                {
                    return false;
                }

                #endregion

                if (end.HasValue && end.Value <= start)
                {
                    ErrorResponse = "end: must be after the start!";
                    return false;
                }

                Period = new EnergyMixPrognosisPeriod(
                             start,
                             energySources,
                             environmentalImpacts,
                             end
                         );

                return true;

            }
            catch (Exception e)
            {
                Period         = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this object.
        /// Timestamps are written in UTC with millisecond precision.
        /// </summary>
        public JObject ToJSON()

            => JSONObject.Create(

                         new JProperty("start",                  Start.    ToISO8601()),

                   End.HasValue
                       ? new JProperty("end",                    End.Value.ToISO8601())
                       : null,

                         new JProperty("energySources",          EnergySources.       ToJSON()),
                         new JProperty("environmentalImpacts",   EnvironmentalImpacts.ToJSON())

               );

        #endregion


        #region IEquatable<EnergyMixPrognosisPeriod> Members

        /// <summary>
        /// Compares two energy mix prognosis periods for equality.
        /// </summary>
        /// <param name="Object">An energy mix prognosis period to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMixPrognosisPeriod period &&
                   Equals(period);

        /// <summary>
        /// Compares two energy mix prognosis periods for equality.
        /// </summary>
        /// <param name="Period">An energy mix prognosis period to compare with.</param>
        public Boolean Equals(EnergyMixPrognosisPeriod? Period)

            => Period is not null &&

               Start.               Equals       (Period.Start)                &&
               End.                 Equals       (Period.End)                  &&
               EnergySources.       SequenceEqual(Period.EnergySources)        &&
               EnvironmentalImpacts.SequenceEqual(Period.EnvironmentalImpacts);

        #endregion

        #region (override) GetHashCode()

        private readonly Int32 hashCode;

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => hashCode;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(
                   Start.ToISO8601(),
                   End.HasValue ? " → " + End.Value.ToISO8601() : " →",
                   ": ",
                   EnergySources.Select(share => share.ToString()).AggregateWith(", ")
               );

        #endregion

    }

}
