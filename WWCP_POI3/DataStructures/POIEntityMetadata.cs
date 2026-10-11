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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The creation and last change timestamps and the custom data of a POI entity:
    /// always part of its content and of its ETags, the timestamps to the millisecond.
    /// </summary>
    public static class POIEntityMetadata
    {

        #region Timestamp       (Timestamp)

        /// <summary>
        /// The given timestamp to the millisecond, as its JSON and CBOR representations keep it.
        /// </summary>
        /// <param name="Timestamp">A timestamp.</param>
        public static DateTimeOffset Timestamp(DateTimeOffset Timestamp)

            => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.ToUnixTimeMilliseconds());

        #endregion

        #region ToJSON          (JSON, Created, LastChange, CustomData)

        /// <summary>
        /// Add the creation and last change timestamps and the custom data to the given JSON object.
        /// </summary>
        public static JObject AddTo(JObject         JSON,
                                    DateTimeOffset  Created,
                                    DateTimeOffset  LastChange,
                                    CustomDataNew   CustomData)
        {

            JSON.Add("created",     Created.   ToISO8601());
            JSON.Add("lastChange",  LastChange.ToISO8601());

            if (CustomData.HasValues)
                JSON.Add("customData", CustomData.ToJObject());

            return JSON;

        }

        #endregion

        #region TryParse        (JSON, out Created, out LastChange, out CustomData, out ErrorResponse)

        /// <summary>
        /// Try to read the creation and last change timestamps and the custom data of a POI entity.
        /// Both timestamps are optional when reading, but always written.
        /// </summary>
        public static Boolean TryParse(JObject                           JSON,
                                       out DateTimeOffset?               Created,
                                       out DateTimeOffset?               LastChange,
                                       out CustomDataNew?                CustomData,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Created     = null;
            LastChange  = null;
            CustomData  = null;

            if (!TryParseTimestamp(JSON["created"],     "creation timestamp",     out Created,     out ErrorResponse) ||
                !TryParseTimestamp(JSON["lastChange"],  "last change timestamp",  out LastChange,  out ErrorResponse))
            {
                return false;
            }

            if (JSON["customData"] is JToken customDataJSON)
            {

                if (customDataJSON is not JObject customDataObject)
                {
                    ErrorResponse = "The custom data must be a JSON object!";
                    return false;
                }

                try
                {
                    CustomData = CustomDataNew.ParseJObject(customDataObject);
                }
                catch (Exception e)
                {
                    ErrorResponse = "Invalid custom data: " + e.Message;
                    return false;
                }

            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (private static) TryParseTimestamp(JSON, Description, out Timestamp, out ErrorResponse)

        private static Boolean TryParseTimestamp(JToken?                           JSON,
                                                 String                            Description,
                                                 out DateTimeOffset?               Timestamp,
                                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Timestamp = null;

            if (JSON is null)
            {
                ErrorResponse = null;
                return true;
            }

            DateTimeOffset timestamp;

            switch (JSON.Type)
            {

                // Read as a date, when the JSON text was parsed with DateParseHandling.DateTime.
                case JTokenType.Date:
                    timestamp = ((JValue) JSON).Value switch {
                                    DateTimeOffset dateTimeOffset                                    => dateTimeOffset,
                                    DateTime       dateTime when dateTime.Kind == DateTimeKind.Unspecified  => new DateTimeOffset(dateTime, TimeSpan.Zero),
                                    DateTime       dateTime                                          => new DateTimeOffset(dateTime),
                                    _                                                                => throw new FormatException($"Invalid {Description}!")
                                };
                    break;

                case JTokenType.String when DateTimeOffset.TryParse(JSON.Value<String>(),
                                                                    CultureInfo.InvariantCulture,
                                                                    DateTimeStyles.AssumeUniversal,
                                                                    out var parsed):
                    timestamp = parsed;
                    break;

                default:
                    ErrorResponse = $"Invalid {Description} '{JSON}'!";
                    return false;

            }

            Timestamp      = POIEntityMetadata.Timestamp(timestamp);
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
