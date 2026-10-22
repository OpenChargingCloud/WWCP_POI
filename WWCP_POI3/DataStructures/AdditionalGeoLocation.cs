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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A further geographical location related to a charging pool, e.g. a walkway or
    /// a point of interest nearby: an immutable value without an identification.
    /// </summary>
    /// <param name="GeoLocation">The geographical location.</param>
    /// <param name="Name">What is there, unless unknown.</param>
    public sealed record AdditionalGeoLocation(GeoCoordinate         GeoLocation,
                                               ImmutableI18NString?  Name   = null)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/additionalGeoLocation/v1";

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
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation   = "geoLocation";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name          = "name";

        }

        #endregion

        #endregion


        #region ToJSON(Embedded = false, CustomAdditionalGeoLocationSerializer = null)

        /// <summary>
        /// Return the JSON representation of this related location.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomAdditionalGeoLocationSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                                  Embedded                                = false,
                              CustomJObjectSerializerDelegate<AdditionalGeoLocation>?  CustomAdditionalGeoLocationSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,  JSONLDContext);

            json.Add(PropertyKeys.GeoLocation,  POIJSON.GeoCoordinateJSON(GeoLocation));

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,     Name.ToJSON());

            return CustomAdditionalGeoLocationSerializer is not null
                       ? CustomAdditionalGeoLocationSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out AdditionalGeoLocation, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a related location.
        /// </summary>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out AdditionalGeoLocation?  AdditionalGeoLocation,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            AdditionalGeoLocation = null;

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the related location must be '{JSONLDContext}'!";
                return false;
            }

            if (!POIJSON.TryParseGeoLocation(JSON, PropertyKeys.GeoLocation, "geo location",               out var geoLocation, out ErrorResponse) ||
                !POIJSON.TryParseI18N       (JSON, PropertyKeys.Name,        "name of a related location", out var name,        out ErrorResponse))
            {
                return false;
            }

            if (!geoLocation.HasValue)
            {
                ErrorResponse = "A related location needs its geo location!";
                return false;
            }

            AdditionalGeoLocation  = new AdditionalGeoLocation(geoLocation.Value, name);
            ErrorResponse          = null;
            return true;

        }

        #endregion

    }

}
