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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// This class defines a geo location. The geodetic system to be used is WGS 84.
    /// </summary>
    public readonly struct AdditionalGeoLocation : IEquatable<AdditionalGeoLocation>
    {

        #region Properties

        /// <summary>
        /// The geo location.
        /// </summary>
        [Mandatory]
        public GeoCoordinate  GeoLocation    { get; }

        /// <summary>
        /// An optional name for this geo location in the local language or as written at the location
        /// </summary>
        /// <example>The street name of a parking lot entrance or it's number.</example>
        [Optional]
        public I18NString?    Name           { get; }

        #endregion

        #region Constructor(s)

        #region AdditionalGeoLocation(GeoLocation,                          Name = null)

        /// <summary>
        /// Create a new geographical coordinate or position on a map.
        /// </summary>
        /// <param name="GeoLocation">The geo location.</param>
        /// <param name="Name">An optional name for this geo location in the local language or as written at the location.</param>
        public AdditionalGeoLocation(GeoCoordinate  GeoLocation,
                                     I18NString?    Name   = null)
        {

            this.GeoLocation  = GeoLocation;
            this.Name         = Name;

        }

        #endregion

        #region AdditionalGeoLocation(Latitude, Longitude, Altitude = null, Name = null)

        /// <summary>
        /// Create a new geographical coordinate or position on a map.
        /// </summary>
        /// <param name="Latitude">The Latitude (south to nord).</param>
        /// <param name="Longitude">The Longitude (parallel to equator).</param>
        /// <param name="Altitude">The (optional) Altitude.</param>
        /// <param name="Name">An optional name for this geo locationin the local language or as written at the location.</param>
        public AdditionalGeoLocation(Latitude     Latitude,
                                     Longitude    Longitude,
                                     Altitude?    Altitude   = null,
                                     I18NString?  Name       = null)

            : this(
                  new GeoCoordinate(
                      Latitude,
                      Longitude,
                      Altitude
                  ),
                  Name
              )

        { }

        #endregion

        #endregion


        #region (static) Parse   (JSON, CustomAdditionalGeoLocationParser = null)

        /// <summary>
        /// Parse the given JSON representation of an additional geo location.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CustomAdditionalGeoLocationParser">A delegate to parse custom additional geo location JSON objects.</param>
        public static AdditionalGeoLocation Parse(JObject                                              JSON,
                                                  CustomJObjectParserDelegate<AdditionalGeoLocation>?  CustomAdditionalGeoLocationParser   = null)
        {

            if (TryParse(JSON,
                         out var additionalGeoLocation,
                         out var errorResponse,
                         CustomAdditionalGeoLocationParser))
            {
                return additionalGeoLocation;
            }

            throw new ArgumentException("The given JSON representation of an additional geo location is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse(JSON, out AdditionalGeoLocation, out ErrorResponse, CustomAdditionalGeoLocationParser = null)

        // Note: The following is needed to satisfy pattern matching delegates! Do not refactor it!

        /// <summary>
        /// Try to parse the given JSON representation of an additional geo location.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="AdditionalGeoLocation">The parsed connector.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out AdditionalGeoLocation  AdditionalGeoLocation,
                                       [NotNullWhen(false)] out String?                ErrorResponse)

            => TryParse(JSON,
                        out AdditionalGeoLocation,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of an additional geo location.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="AdditionalGeoLocation">The parsed connector.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomAdditionalGeoLocationParser">A delegate to parse custom additional geo location JSON objects.</param>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out AdditionalGeoLocation       AdditionalGeoLocation,
                                       [NotNullWhen(false)] out String?                     ErrorResponse,
                                       CustomJObjectParserDelegate<AdditionalGeoLocation>?  CustomAdditionalGeoLocationParser)
        {

            try
            {

                AdditionalGeoLocation = default;

                if (JSON?.HasValues != true)
                {
                    ErrorResponse = "The given JSON object must not be null or empty!";
                    return false;
                }

                #region Parse Latitude     [mandatory]

                if (!JsonValueParsing.TryReadOptional(JSON, "latitude", TryParseLatitude,
                                                      out Latitude? latitude, out ErrorResponse) ||
                    !latitude.HasValue || !Double.IsFinite(latitude.Value.Value) ||
                    latitude.Value.Value < -90 || latitude.Value.Value > 90)
                {
                    ErrorResponse ??= "Invalid or missing 'latitude': expected a value between -90 and 90.";
                    return false;
                }

                #endregion

                #region Parse Longitude    [mandatory]

                if (!JsonValueParsing.TryReadOptional(JSON, "longitude", TryParseLongitude,
                                                      out Longitude? longitude, out ErrorResponse) ||
                    !longitude.HasValue || !Double.IsFinite(longitude.Value.Value) ||
                    longitude.Value.Value < -180 || longitude.Value.Value > 180)
                {
                    ErrorResponse ??= "Invalid or missing 'longitude': expected a value between -180 and 180.";
                    return false;
                }

                #endregion

                if (!JsonValueParsing.TryReadOptional(JSON, "altitude", TryParseAltitude,
                                                      out Altitude? altitude, out ErrorResponse) ||
                    (altitude.HasValue && !Double.IsFinite(altitude.Value.Value)))
                {
                    ErrorResponse ??= "Invalid 'altitude': expected a finite value in metres.";
                    return false;
                }

                #region Parse Name         [optional]

                if (JSON.ParseOptionalJSON("name",
                                           "name",
                                           I18NString.TryParse,
                                           out I18NString? Name,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                AdditionalGeoLocation = new AdditionalGeoLocation(
                                            new GeoCoordinate(
                                                latitude.Value,
                                                longitude.Value,
                                                altitude
                                            ),
                                            Name
                                        );


                if (CustomAdditionalGeoLocationParser is not null)
                    AdditionalGeoLocation = CustomAdditionalGeoLocationParser(JSON,
                                                                              AdditionalGeoLocation);

                return true;

            }
            catch (Exception e)
            {
                AdditionalGeoLocation  = default;
                ErrorResponse          = "The given JSON representation of an additional geo location is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        private static Boolean TryParseFiniteNumber(String text, out Double number)
            => Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && Double.IsFinite(number);

        private static Boolean TryParseLatitude(String text, out Latitude latitude)
        {
            latitude = default;
            return TryParseFiniteNumber(text, out var number) && Latitude.TryParse(number, out latitude);
        }

        private static Boolean TryParseLongitude(String text, out Longitude longitude)
        {
            longitude = default;
            return TryParseFiniteNumber(text, out var number) && Longitude.TryParse(number, out longitude);
        }

        private static Boolean TryParseAltitude(String text, out Altitude altitude)
        {
            altitude = default;
            return TryParseFiniteNumber(text, out var number) && Altitude.TryParse(number, out altitude);
        }

        #region ToJSON(CustomAdditionalGeoLocationSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object.
        /// </summary>
        /// <param name="CustomAdditionalGeoLocationSerializer">A delegate to serialize custom additional geo location JSON objects.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<AdditionalGeoLocation>? CustomAdditionalGeoLocationSerializer = null)
        {

            var JSON = JSONObject.Create(

                                 new JProperty("latitude",   GeoLocation.Latitude. Value.ToString("R", CultureInfo.InvariantCulture)),
                                 new JProperty("longitude",  GeoLocation.Longitude.Value.ToString("R", CultureInfo.InvariantCulture)),

                           GeoLocation.Altitude.HasValue
                               ? new JProperty("altitude",   GeoLocation.Altitude.Value.Value)
                               : null,

                           Name.IsNotNullOrEmpty()
                               ? new JProperty("name",       Name.                       ToJSON())
                               : null

                       );

            return CustomAdditionalGeoLocationSerializer is not null
                       ? CustomAdditionalGeoLocationSerializer(this, JSON)
                       : JSON;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this object.
        /// </summary>
        public AdditionalGeoLocation Clone()

            => new (
                   GeoLocation.Clone(),
                   Name?.      Clone()
               );

        #endregion


        #region Operator overloading

        #region Operator == (AdditionalGeoLocation1, AdditionalGeoLocation2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="AdditionalGeoLocation1">An additional geo location.</param>
        /// <param name="AdditionalGeoLocation2">Another additional geo location.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (AdditionalGeoLocation AdditionalGeoLocation1,
                                           AdditionalGeoLocation AdditionalGeoLocation2)

            => AdditionalGeoLocation1.Equals(AdditionalGeoLocation2);

        #endregion

        #region Operator != (AdditionalGeoLocation1, AdditionalGeoLocation2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="AdditionalGeoLocation1">An additional geo location.</param>
        /// <param name="AdditionalGeoLocation2">Another additional geo location.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (AdditionalGeoLocation AdditionalGeoLocation1,
                                           AdditionalGeoLocation AdditionalGeoLocation2)

            => !(AdditionalGeoLocation1 == AdditionalGeoLocation2);

        #endregion

        #endregion

        #region IEquatable<AdditionalGeoLocation> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two additional geo locations for equality.
        /// </summary>
        /// <param name="Object">An additional geo location to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is AdditionalGeoLocation AdditionalGeoLocation &&
                   Equals(AdditionalGeoLocation);

        #endregion

        #region Equals(AdditionalGeoLocation)

        /// <summary>
        /// Compares two additional geo locations for equality.
        /// </summary>
        /// <param name="AdditionalGeoLocation">An additional geo location to compare with.</param>
        public Boolean Equals(AdditionalGeoLocation AdditionalGeoLocation)

            => GeoLocation.Equals(AdditionalGeoLocation.GeoLocation) &&

             ((Name.IsNullOrEmpty()    && AdditionalGeoLocation.Name.IsNullOrEmpty()) ||
              (Name.IsNotNullOrEmpty() && AdditionalGeoLocation.Name.IsNotNullOrEmpty() && Name.Equals(AdditionalGeoLocation.Name)));

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return GeoLocation.GetHashCode() * 3 ^
                       Name?.      GetHashCode() ?? 0;

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(

                   GeoLocation,

                   Name.IsNotNullOrEmpty()
                       ? $", {Name}"
                       : ""

               );

        #endregion

    }

}
