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
    /// A change of the admin status or of the status of an entity of a roaming network:
    /// runtime data, not content, without ETags and without commits.
    /// </summary>
    /// <param name="Kind">Admin status or status.</param>
    /// <param name="EntityType">The type of the entity, e.g. ChargingStationOperator.</param>
    /// <param name="EntityId">The identification of the entity.</param>
    /// <param name="Timestamp">Since when the entity has this status, to the millisecond.</param>
    /// <param name="Value">The status.</param>
    /// <param name="OldTimestamp">Since when the entity had its previous status, if known.</param>
    /// <param name="OldValue">The previous status, if known.</param>
    /// <param name="DataSource">The optional source of the status.</param>
    public sealed record POIStatusUpdate(POIRuntimeStatusKind  Kind,
                                         String                EntityType,
                                         String                EntityId,
                                         DateTimeOffset        Timestamp,
                                         String                Value,
                                         DateTimeOffset?       OldTimestamp   = null,
                                         String?               OldValue       = null,
                                         String?               DataSource     = null)
    {

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this status update.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("kind",        KindText(Kind)),
                           new JProperty("entityType",  EntityType),
                           new JProperty("entityId",    EntityId),
                           new JProperty("timestamp",   Timestamp.ToISO8601()),
                           new JProperty("value",       ValueToJSON(Kind, Value))
                       );

            if (OldTimestamp.HasValue)
                json.Add("oldTimestamp",  OldTimestamp.Value.ToISO8601());

            if (OldValue is not null)
                json.Add("oldValue",      ValueToJSON(Kind, OldValue));

            if (DataSource is not null)
                json.Add("dataSource",    DataSource);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out StatusUpdate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a status update, e.g. one pushed by the leading server.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="StatusUpdate">The parsed status update.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out POIStatusUpdate?  StatusUpdate,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            StatusUpdate = null;

            if (!TryParseKind(JSON["kind"]?.Type == JTokenType.String ? JSON["kind"]!.Value<String>() : null, out var kind))
            {
                ErrorResponse = "Missing or invalid status kind!";
                return false;
            }

            if (!JSON.ParseMandatoryText("entityType", "entity type",           out String? entityType, out ErrorResponse) ||
                !JSON.ParseMandatoryText("entityId",   "entity identification", out String? entityId,   out ErrorResponse) ||
                !TryParseValue(kind, JSON["value"], "status", out String? value, out ErrorResponse))
            {
                return false;
            }

            if (!JSON.ParseMandatory("timestamp", "timestamp", out DateTimeOffset timestamp, out ErrorResponse))
                return false;

            if (JSON.ParseOptional("oldTimestamp", "old timestamp", out DateTimeOffset? oldTimestamp, out ErrorResponse))
            {
                if (ErrorResponse is not null)
                    return false;
            }

            String? oldValue = null;

            if (JSON["oldValue"] is JToken oldValueJSON &&
                !TryParseValue(kind, oldValueJSON, "old status", out oldValue, out ErrorResponse))
            {
                return false;
            }

            if (JSON.ParseOptional("dataSource", "data source", out String? dataSource, out ErrorResponse))
            {
                if (ErrorResponse is not null)
                    return false;
            }

            StatusUpdate   = new POIStatusUpdate(kind,
                                                 entityType,
                                                 entityId,
                                                 timestamp,
                                                 value,
                                                 oldTimestamp,
                                                 oldValue,
                                                 dataSource);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (static) ValueToJSON(Kind, Value)

        /// <summary>
        /// The JSON of the given status value: a text, or the object of an energy status.
        /// </summary>
        public static JToken ValueToJSON(POIRuntimeStatusKind  Kind,
                                         String                Value)
        {

            if (Kind == POIRuntimeStatusKind.EnergyStatus && POIEnergyStatus.TryParseText(Value, out var energyStatus))
                return POIEnergyStatus.ToJSON(energyStatus);

            return Value;

        }

        #endregion

        #region (static) TryParseValue(Kind, JSON, Description, out Value, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON of a status value: a text, or the object of an energy status,
        /// which is kept as its compact JSON text.
        /// </summary>
        public static Boolean TryParseValue(POIRuntimeStatusKind              Kind,
                                            JToken?                           JSON,
                                            String                            Description,
                                            [NotNullWhen(true)]  out String?  Value,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Value = null;

            if (Kind == POIRuntimeStatusKind.EnergyStatus)
            {

                if (!POIEnergyStatus.TryParse(JSON, out var energyStatus, out ErrorResponse))
                    return false;

                Value = POIEnergyStatus.ToText(energyStatus);
                return true;

            }

            if (JSON?.Type != JTokenType.String || String.IsNullOrWhiteSpace(JSON.Value<String>()))
            {
                ErrorResponse = $"Missing or invalid {Description}!";
                return false;
            }

            Value          = JSON.Value<String>()!;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (static) KindText(Kind)

        /// <summary>
        /// The JSON text of the given status kind: adminStatus or status.
        /// </summary>
        public static String KindText(POIRuntimeStatusKind Kind)

            => Kind switch {
                   POIRuntimeStatusKind.AdminStatus   => "adminStatus",
                   POIRuntimeStatusKind.EnergyStatus  => "energyStatus",
                   _                                  => "status"
               };

        #endregion

        #region (static) TryParseKind(Text, out Kind)

        /// <summary>
        /// Try to parse the JSON text of a status kind: adminStatus or status.
        /// </summary>
        public static Boolean TryParseKind(String?                   Text,
                                           out POIRuntimeStatusKind  Kind)
        {

            switch (Text)
            {

                case "adminStatus":
                    Kind = POIRuntimeStatusKind.AdminStatus;
                    return true;

                case "energyStatus":
                    Kind = POIRuntimeStatusKind.EnergyStatus;
                    return true;

                case "status":
                    Kind = POIRuntimeStatusKind.Status;
                    return true;

                default:
                    Kind = default;
                    return false;

            }

        }

        #endregion

    }

}
