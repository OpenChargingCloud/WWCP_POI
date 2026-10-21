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

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/poiStatusUpdate";

        #endregion

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
            /// The property of its kind.
            /// </summary>
            public const String  Kind          = "kind";

            /// <summary>
            /// The property of the type of its entity.
            /// </summary>
            public const String  EntityType    = "entityType";

            /// <summary>
            /// The property of the identification of its entity.
            /// </summary>
            public const String  EntityId      = "entityId";

            /// <summary>
            /// The property of since when the entity has the status.
            /// </summary>
            public const String  Timestamp     = "timestamp";

            /// <summary>
            /// The property of its status.
            /// </summary>
            public const String  Value         = "value";

            /// <summary>
            /// The property of since when the entity had its previous status.
            /// </summary>
            public const String  OldTimestamp  = "oldTimestamp";

            /// <summary>
            /// The property of the previous status.
            /// </summary>
            public const String  OldValue      = "oldValue";

            /// <summary>
            /// The property of its data source.
            /// </summary>
            public const String  DataSource    = "dataSource";

        }

        #endregion

        #region ToJSON(Embedded = false, CustomStatusUpdateSerializer = null)

        /// <summary>
        /// Return a JSON representation of this status update.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomStatusUpdateSerializer">An optional custom serializer of status updates.</param>
        public JObject ToJSON(Boolean                                            Embedded                       = false,
                              CustomJObjectSerializerDelegate<POIStatusUpdate>?  CustomStatusUpdateSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,     JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Kind,        KindText(Kind)),
                                 new JProperty(PropertyKeys.EntityType,  EntityType),
                                 new JProperty(PropertyKeys.EntityId,    EntityId),
                                 new JProperty(PropertyKeys.Timestamp,   Timestamp.ToISO8601()),
                                 new JProperty(PropertyKeys.Value,       ValueToJSON(Kind, Value))

                       );

            if (OldTimestamp.HasValue)
                json.Add(PropertyKeys.OldTimestamp,  OldTimestamp.Value.ToISO8601());

            if (OldValue is not null)
                json.Add(PropertyKeys.OldValue,      ValueToJSON(Kind, OldValue));

            if (DataSource is not null)
                json.Add(PropertyKeys.DataSource,    DataSource);

            return CustomStatusUpdateSerializer is not null
                       ? CustomStatusUpdateSerializer(this, json)
                       : json;

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

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the status update must be '{JSONLDContext}'!";
                return false;
            }

            if (!TryParseKind(JSON[PropertyKeys.Kind]?.Type == JTokenType.String ? JSON[PropertyKeys.Kind]!.Value<String>() : null, out var kind))
            {
                ErrorResponse = "Missing or invalid status kind!";
                return false;
            }

            if (!JSON.ParseMandatoryText(PropertyKeys.EntityType, "entity type",           out String? entityType, out ErrorResponse) ||
                !JSON.ParseMandatoryText(PropertyKeys.EntityId,   "entity identification", out String? entityId,   out ErrorResponse) ||
                !TryParseValue(kind, JSON[PropertyKeys.Value], "status", out String? value, out ErrorResponse))
            {
                return false;
            }

            if (!JSON.ParseMandatory(PropertyKeys.Timestamp, "timestamp", out DateTimeOffset timestamp, out ErrorResponse))
                return false;

            if (JSON.ParseOptional(PropertyKeys.OldTimestamp, "old timestamp", out DateTimeOffset? oldTimestamp, out ErrorResponse))
            {
                if (ErrorResponse is not null)
                    return false;
            }

            String? oldValue = null;

            if (JSON[PropertyKeys.OldValue] is JToken oldValueJSON &&
                !TryParseValue(kind, oldValueJSON, "old status", out oldValue, out ErrorResponse))
            {
                return false;
            }

            if (JSON.ParseOptional(PropertyKeys.DataSource, "data source", out String? dataSource, out ErrorResponse))
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

            if (Kind == POIRuntimeStatusKind.PriceStatus && ChargingPrices.TryParseText(Value, out var priceStatus))
                return priceStatus.ToJSON(Embedded: true);

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

            if (Kind == POIRuntimeStatusKind.PriceStatus)
            {

                if (!ChargingPrices.TryParse(JSON, out var priceStatus, out ErrorResponse))
                    return false;

                Value = priceStatus.Text;
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
                   POIRuntimeStatusKind.PriceStatus   => "priceStatus",
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

                case "priceStatus":
                    Kind = POIRuntimeStatusKind.PriceStatus;
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
