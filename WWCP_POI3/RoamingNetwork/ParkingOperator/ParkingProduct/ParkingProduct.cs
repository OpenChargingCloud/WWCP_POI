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

    /// <summary>
    /// A parking product offered by a parking operator, embedded completely in it:
    /// its parking garages, parking spaces and parking space groups name it.
    /// Without runtime status and timestamps; it does not calculate prices.
    /// Its identification is unique within the roaming network and compared case-sensitively.
    /// </summary>
    /// <remarks>
    /// The durations are in seconds.
    /// </remarks>
    public sealed class ParkingProduct : IEquatable<ParkingProduct>,
                                         IComparable<ParkingProduct>
    {

        #region Properties

        /// <summary>
        /// The identification of the parking product.
        /// </summary>
        public ParkingProduct_Id     Id                      { get; }

        /// <summary>
        /// The multi-language name.
        /// </summary>
        public ImmutableI18NString?  Name                    { get; }

        /// <summary>
        /// The multi-language description.
        /// </summary>
        public ImmutableI18NString?  Description             { get; }

        /// <summary>
        /// The minimum parking duration billed.
        /// </summary>
        public TimeSpan?             MinDuration             { get; }

        /// <summary>
        /// The parking duration after which parking must stop.
        /// </summary>
        public TimeSpan?             StopParkingAfterTime    { get; }

        /// <summary>
        /// Custom data.
        /// </summary>
        public CustomDataNew?        CustomData              { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking product.
        /// </summary>
        public ParkingProduct(ParkingProduct_Id     Id,
                              ImmutableI18NString?  Name                   = null,
                              ImmutableI18NString?  Description            = null,
                              TimeSpan?             MinDuration            = null,
                              TimeSpan?             StopParkingAfterTime   = null,
                              CustomDataNew?        CustomData             = null)
        {

            if (MinDuration < TimeSpan.Zero || StopParkingAfterTime < TimeSpan.Zero)
                throw new ArgumentException($"The durations of the parking product '{Id}' must not be negative!");

            if (MinDuration > StopParkingAfterTime)
                throw new ArgumentException($"The minimum duration of the parking product '{Id}' must not exceed its stopParkingAfterTime!");

            this.Id                    = Id;
            this.Name                  = Name;
            this.Description           = Description;
            this.MinDuration           = MinDuration;
            this.StopParkingAfterTime  = StopParkingAfterTime;
            this.CustomData            = CustomData;

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this parking product, as its parking operator embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",                  Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",           Description.ToJSON());

            if (MinDuration.HasValue)
                json.Add("minDuration",           Seconds(MinDuration.Value));

            if (StopParkingAfterTime.HasValue)
                json.Add("stopParkingAfterTime",  Seconds(StopParkingAfterTime.Value));

            if (CustomData is not null && CustomData.HasValues)
                json.Add("customData",            CustomData.ToJObject());

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ParkingProduct, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a parking product.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ParkingProduct">The parsed parking product.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out ParkingProduct?  ParkingProduct,
                                       [NotNullWhen(false)] out String?          ErrorResponse)
        {

            ParkingProduct = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "parking product identification",
                                         ParkingProduct_Id.TryParse,
                                         out ParkingProduct_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N(JSON, "name",         "parking product name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N(JSON, "description",  "parking product description",  out var description,  out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional("minDuration",           "minimum duration",          out Decimal? minDuration,           out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("stopParkingAfterTime",  "stop parking after time",   out Decimal? stopParkingAfterTime,  out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out _,
                                                out _,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ParkingProduct  = new ParkingProduct(id,
                                                     name,
                                                     description,
                                                     minDuration.         HasValue ? FromSeconds(minDuration.Value)          : null,
                                                     stopParkingAfterTime.HasValue ? FromSeconds(stopParkingAfterTime.Value) : null,
                                                     customData);

                ErrorResponse   = null;
                return true;

            }
            catch (Exception e)
            {
                ParkingProduct  = null;
                ErrorResponse   = "The given JSON representation of a parking product is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region (private static) Seconds    (Duration)

        private static Decimal Seconds(TimeSpan Duration)

            => (Decimal) Duration.Ticks / TimeSpan.TicksPerSecond;

        #endregion

        #region (private static) FromSeconds(Seconds)

        private static TimeSpan FromSeconds(Decimal Seconds)

            => TimeSpan.FromTicks((Int64) (Seconds * TimeSpan.TicksPerSecond));

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two parking products for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ParkingProduct? ParkingProduct1, ParkingProduct? ParkingProduct2)

            => ReferenceEquals(ParkingProduct1, ParkingProduct2) ||
               (ParkingProduct1 is not null && ParkingProduct1.Equals(ParkingProduct2));

        /// <summary>
        /// Compares two parking products for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ParkingProduct? ParkingProduct1, ParkingProduct? ParkingProduct2)

            => !(ParkingProduct1 == ParkingProduct2);

        /// <inheritdoc/>
        public Int32 CompareTo(ParkingProduct? ParkingProduct)

            => ParkingProduct is null
                   ? throw new ArgumentNullException(nameof(ParkingProduct), "The given parking product must not be null!")
                   : Id.CompareTo(ParkingProduct.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ParkingProduct parkingProduct &&
               Equals(parkingProduct);

        /// <inheritdoc/>
        public Boolean Equals(ParkingProduct? ParkingProduct)

            => ParkingProduct is not null &&
               Id.Equals(ParkingProduct.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
