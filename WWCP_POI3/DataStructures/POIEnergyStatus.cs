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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The energy status of EVSEs and charging stations: runtime data like the admin status and the status,
    /// as JSON { "used": W, "available": W, "storedEnergy"?: Wh, "currentVoltage"?: V, "remainingChargingTime"?: s }
    /// in their base units; only an EVSE has a current voltage and a remaining charging time.
    /// Its text is that JSON, compact and in this order, so that equal energy statuses have equal texts.
    /// </summary>
    public static class POIEnergyStatus
    {

        #region ToJSON(EnergyInfo)

        /// <summary>
        /// The JSON representation of the given energy status.
        /// </summary>
        public static JObject ToJSON(EnergyInfo EnergyInfo)
        {

            var json = new JObject(
                           new JProperty("used",       POIJSON.Number(EnergyInfo.Used.Value)),
                           new JProperty("available",  POIJSON.Number(EnergyInfo.Available.Value))
                       );

            if (EnergyInfo.StoredEnergy.HasValue)
                json.Add("storedEnergy",           POIJSON.Number(EnergyInfo.StoredEnergy.Value.Value));

            if (EnergyInfo.CurrentVoltage.HasValue)
                json.Add("currentVoltage",         POIJSON.Number(EnergyInfo.CurrentVoltage.Value.Value));

            if (EnergyInfo.RemainingChargingTime.HasValue)
                json.Add("remainingChargingTime",  POIJSON.Seconds(EnergyInfo.RemainingChargingTime.Value));

            return json;

        }

        #endregion

        #region ToText(EnergyInfo)

        /// <summary>
        /// The text of the given energy status: its compact JSON.
        /// </summary>
        public static String ToText(EnergyInfo EnergyInfo)

            => ToJSON(EnergyInfo).ToString(Formatting.None);

        #endregion

        #region TryParse(JSON, out EnergyInfo, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an energy status.
        /// </summary>
        public static Boolean TryParse(JToken?                           JSON,
                                       out EnergyInfo                    EnergyInfo,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            EnergyInfo = default;

            if (JSON is not JObject json)
            {
                ErrorResponse = "An energy status must be a JSON object { used, available, storedEnergy?, currentVoltage?, remainingChargingTime? }!";
                return false;
            }

            foreach (var property in json.Properties())
                if (property.Name is not ("used" or "available" or "storedEnergy" or "currentVoltage" or "remainingChargingTime"))
                {
                    ErrorResponse = $"Unknown property '{property.Name}' of an energy status!";
                    return false;
                }

            if (!TryParseValue(json, "used",                   Mandatory: true,  out var used,                   out ErrorResponse) ||
                !TryParseValue(json, "available",              Mandatory: true,  out var available,              out ErrorResponse) ||
                !TryParseValue(json, "storedEnergy",           Mandatory: false, out var storedEnergy,           out ErrorResponse) ||
                !TryParseValue(json, "currentVoltage",         Mandatory: false, out var currentVoltage,         out ErrorResponse) ||
                !TryParseValue(json, "remainingChargingTime",  Mandatory: false, out var remainingChargingTime,  out ErrorResponse))
            {
                return false;
            }

            EnergyInfo     = new EnergyInfo(Watt.FromW(used!.Value),
                                            Watt.FromW(available!.Value),
                                            storedEnergy.         HasValue ? WattHour.  FromWh     (storedEnergy.         Value) : null,
                                            currentVoltage.       HasValue ? Volt.      FromV      (currentVoltage.       Value) : null,
                                            remainingChargingTime.HasValue ? POIJSON.   FromSeconds(remainingChargingTime.Value) : null);

            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryParseText(Text, out EnergyInfo)

        /// <summary>
        /// Try to parse the given text of an energy status, its JSON.
        /// </summary>
        public static Boolean TryParseText(String          Text,
                                           out EnergyInfo  EnergyInfo)
        {

            EnergyInfo = default;

            try
            {
                return TryParse(JToken.Parse(Text), out EnergyInfo, out _);
            }
            catch
            {
                return false;
            }

        }

        #endregion


        #region (private static) TryParseValue(JSON, PropertyName, Mandatory, out Value, out ErrorResponse)

        private static Boolean TryParseValue(JObject                           JSON,
                                             String                            PropertyName,
                                             Boolean                           Mandatory,
                                             out Decimal?                      Value,
                                             [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = Mandatory ? $"An energy status needs '{PropertyName}'!" : null;
                return !Mandatory;
            }

            if (token.Type is not (JTokenType.Integer or JTokenType.Float) || token.Value<Decimal>() < 0)
            {
                ErrorResponse = $"The '{PropertyName}' of an energy status must be a number, not negative!";
                return false;
            }

            Value          = token.Value<Decimal>();
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
