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
    /// An energy mix prognosis: the expected composition of an energy product
    /// over consecutive periods, for information only.
    /// </summary>
    public sealed class EnergyMixPrognosis : IEquatable<EnergyMixPrognosis>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/energyMixPrognosis";

        #endregion

        #region Properties

        /// <summary>
        /// When this prognosis was issued.
        /// </summary>
        public DateTimeOffset                         Timestamp            { get; }

        /// <summary>
        /// The name or brand of the energy supplier.
        /// </summary>
        public ImmutableI18NString                    SupplierName         { get; }

        /// <summary>
        /// The name or brand of the energy product.
        /// </summary>
        public ImmutableI18NString                    ProductName          { get; }

        /// <summary>
        /// The periods of this prognosis, ordered by their start and without overlaps.
        /// </summary>
        public IEnumerable<EnergyMixPrognosisPeriod>  Periods              { get; }

        /// <summary>
        /// Optional additional remarks.
        /// </summary>
        public ImmutableI18NString?                   AdditionalRemarks    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy mix prognosis.
        /// </summary>
        /// <param name="Timestamp">When this prognosis was issued.</param>
        /// <param name="SupplierName">The name or brand of the energy supplier.</param>
        /// <param name="ProductName">The name or brand of the energy product.</param>
        /// <param name="Periods">The periods of this prognosis, ordered by their start and without overlaps.</param>
        /// <param name="AdditionalRemarks">Optional additional remarks.</param>
        public EnergyMixPrognosis(DateTimeOffset                         Timestamp,
                                  ImmutableI18NString                    SupplierName,
                                  ImmutableI18NString                    ProductName,
                                  IEnumerable<EnergyMixPrognosisPeriod>  Periods,
                                  ImmutableI18NString?                   AdditionalRemarks   = null)
        {

            var periods = (Periods ?? []).ToImmutableArray();

            if (periods.Any(period => period is null))
                throw new ArgumentException("A period of an energy mix prognosis must not be null!", nameof(Periods));

            for (var index = 1; index < periods.Length; index++)
            {

                var previous = periods[index - 1];

                if (periods[index].Start <= previous.Start)
                    throw new ArgumentException($"periods[{index}]: the periods must be ordered by their start!", nameof(Periods));

                if (previous.End.HasValue && previous.End.Value > periods[index].Start)
                    throw new ArgumentException($"periods[{index - 1}]: the period must end before the next one starts!", nameof(Periods));

            }

            this.Timestamp          = Timestamp;
            this.SupplierName       = SupplierName ?? throw new ArgumentNullException(nameof(SupplierName));
            this.ProductName        = ProductName  ?? throw new ArgumentNullException(nameof(ProductName));
            this.Periods            = periods;
            this.AdditionalRemarks  = AdditionalRemarks;

            unchecked
            {

                hashCode = this.Timestamp.         GetHashCode()  * 11 ^
                           this.SupplierName.      GetHashCode()  *  7 ^
                           this.ProductName.       GetHashCode()  *  5 ^
                           this.Periods.           CalcHashCode() *  3 ^
                          (this.AdditionalRemarks?.GetHashCode() ?? 0);

            }

        }

        #endregion


        #region (static) Parse   (JSON, CustomEnergyMixPrognosisParser = null)

        /// <summary>
        /// Parse the given JSON representation of an energy mix prognosis.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CustomEnergyMixPrognosisParser">An optional delegate to parse custom energy mix prognosis JSON objects.</param>
        public static EnergyMixPrognosis Parse(JObject                                           JSON,
                                               CustomJObjectParserDelegate<EnergyMixPrognosis>?  CustomEnergyMixPrognosisParser   = null)
        {

            if (TryParse(JSON,
                         out var energyMixPrognosis,
                         out var errorResponse,
                         CustomEnergyMixPrognosisParser))
            {
                return energyMixPrognosis;
            }

            throw new ArgumentException("The given JSON representation of an energy mix prognosis is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out EnergyMixPrognosis, out ErrorResponse, CustomEnergyMixPrognosisParser = null)

        /// <summary>
        /// Try to parse the given JSON representation of an energy mix prognosis.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyMixPrognosis">The parsed energy mix prognosis.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEnergyMixPrognosisParser">An optional delegate to parse custom energy mix prognosis JSON objects.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out EnergyMixPrognosis?      EnergyMixPrognosis,
                                       [NotNullWhen(false)] out String?                  ErrorResponse,
                                       CustomJObjectParserDelegate<EnergyMixPrognosis>?  CustomEnergyMixPrognosisParser   = null)
        {

            EnergyMixPrognosis  = null;
            ErrorResponse       = null;

            try
            {

                if (JSON?.HasValues != true)
                {
                    ErrorResponse = "The given JSON object must not be null or empty!";
                    return false;
                }

                #region Parse Timestamp               [mandatory]

                if (!JSON.ParseMandatory("timestamp",
                                         "timestamp",
                                         out DateTimeOffset timestamp,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse SupplierName            [mandatory]

                if (!JSON.ParseMandatory("supplierName",
                                         "supplier name",
                                         out I18NString supplierName,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ProductName             [mandatory]

                if (!JSON.ParseMandatory("productName",
                                         "product name",
                                         out I18NString productName,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Periods                 [mandatory]

                if (JSON["periods"] is not JArray periodsJSON)
                {
                    ErrorResponse = "The 'periods' property must be an array!";
                    return false;
                }

                var periods = new List<EnergyMixPrognosisPeriod>();

                for (var index = 0; index < periodsJSON.Count; index++)
                {

                    if (periodsJSON[index] is not JObject periodJSON)
                    {
                        ErrorResponse = $"periods[{index}]: expected an object!";
                        return false;
                    }

                    if (!EnergyMixPrognosisPeriod.TryParse(periodJSON,
                                                           out var period,
                                                           out var periodError))
                    {
                        ErrorResponse = $"periods[{index}]: {periodError}";
                        return false;
                    }

                    if (periods.Count > 0)
                    {

                        var previous = periods[^1];

                        if (period.Start <= previous.Start)
                        {
                            ErrorResponse = $"periods[{index}].start: the periods must be ordered by their start!";
                            return false;
                        }

                        if (previous.End.HasValue && previous.End.Value > period.Start)
                        {
                            ErrorResponse = $"periods[{index - 1}].end: the period must end before the next one starts!";
                            return false;
                        }

                    }

                    periods.Add(period);

                }

                #endregion

                #region Parse AdditionalRemarks       [optional]

                if (JSON.ParseOptional("additionalRemarks",
                                       "additional remarks",
                                       I18NString.TryParse,
                                       out I18NString? additionalRemarks,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                EnergyMixPrognosis = new EnergyMixPrognosis(
                                         timestamp,
                                         supplierName,
                                         productName,
                                         periods,
                                         additionalRemarks
                                     );

                if (CustomEnergyMixPrognosisParser is not null)
                    EnergyMixPrognosis = CustomEnergyMixPrognosisParser(JSON,
                                                                        EnergyMixPrognosis);

                return true;

            }
            catch (Exception e)
            {
                EnergyMixPrognosis  = null;
                ErrorResponse       = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomEnergyMixPrognosisSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// The timestamp is written in UTC with millisecond precision.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEnergyMixPrognosisSerializer">A delegate to serialize custom energy mix prognosis JSON objects.</param>
        public JObject ToJSON(Boolean                                               Embedded                             = false,
                              CustomJObjectSerializerDelegate<EnergyMixPrognosis>?  CustomEnergyMixPrognosisSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty("@context",            JSONLDContext)
                               : null,

                                 new JProperty("timestamp",           Timestamp.   ToISO8601()),
                                 new JProperty("supplierName",        SupplierName.ToJSON()),
                                 new JProperty("productName",         ProductName. ToJSON()),
                                 new JProperty("periods",             new JArray(Periods.Select(period => period.ToJSON()))),

                           AdditionalRemarks is not null
                               ? new JProperty("additionalRemarks",   AdditionalRemarks.ToJSON())
                               : null

                       );

            return CustomEnergyMixPrognosisSerializer is not null
                       ? CustomEnergyMixPrognosisSerializer(this, json)
                       : json;

        }

        #endregion


        #region IEquatable<EnergyMixPrognosis> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy mix prognoses for equality.
        /// </summary>
        /// <param name="Object">An energy mix prognosis to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMixPrognosis energyMixPrognosis &&
                   Equals(energyMixPrognosis);

        #endregion

        #region Equals(EnergyMixPrognosis)

        /// <summary>
        /// Compares two energy mix prognoses for equality.
        /// </summary>
        /// <param name="EnergyMixPrognosis">An energy mix prognosis to compare with.</param>
        public Boolean Equals(EnergyMixPrognosis? EnergyMixPrognosis)

            => EnergyMixPrognosis is not null &&

               Timestamp.   Equals       (EnergyMixPrognosis.Timestamp)    &&
               SupplierName.Equals       (EnergyMixPrognosis.SupplierName) &&
               ProductName. Equals       (EnergyMixPrognosis.ProductName)  &&
               Periods.     SequenceEqual(EnergyMixPrognosis.Periods)      &&

               ((AdditionalRemarks is null     && EnergyMixPrognosis.AdditionalRemarks is null) ||
                (AdditionalRemarks is not null && EnergyMixPrognosis.AdditionalRemarks is not null && AdditionalRemarks.Equals(EnergyMixPrognosis.AdditionalRemarks)));

        #endregion

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

            => String.Concat("'", ProductName.FirstText(), "' from '", SupplierName.FirstText(), "', ",
                             Periods.Count(), " period(s), issued ", Timestamp.ToISO8601());

        #endregion

    }

}
