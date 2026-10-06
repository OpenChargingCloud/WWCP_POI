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

using System.Text.Json;
using System.Text.Json.Serialization;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A single immutable operation against an entity in a roaming network.
    /// </summary>
    public sealed record RoamingNetworkChange
    {
        /// <summary>
        /// Create a validated operation owning independent copies of its JSON values.
        /// </summary>
        /// <param name="kind">The operation to perform.</param>
        /// <param name="entityType">A stable entity type, for example <c>ChargingStation</c>.</param>
        /// <param name="entityId">The entity's canonical identifier.</param>
        /// <param name="propertyName">The property being changed, for property updates.</param>
        /// <param name="oldValue">An optional expected previous value for conflict detection.</param>
        /// <param name="newValue">The new property value or full entity document. A defined JSON null clears a property.</param>
        [JsonConstructor]
        public RoamingNetworkChange(RoamingNetworkChangeKind  kind,
                                    String                    entityType,
                                    String                    entityId,
                                    String?                   propertyName,
                                    JsonElement?              oldValue,
                                    JsonElement?              newValue,
                                    String?                   parentEntityType = null,
                                    String?                   parentEntityId   = null)
        {

            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            EntityType = Required(entityType, nameof(entityType));
            EntityId = Required(entityId, nameof(entityId));

            if ((parentEntityType is null) != (parentEntityId is null))
            {
                throw new ArgumentException("Parent type and parent identifier must be supplied together.");
            }

            ParentEntityType = parentEntityType is null ? null : Required(parentEntityType, nameof(parentEntityType));
            ParentEntityId = parentEntityId is null ? null : Required(parentEntityId, nameof(parentEntityId));

            if (oldValue?.ValueKind == JsonValueKind.Undefined || newValue?.ValueKind == JsonValueKind.Undefined)
            {
                throw new ArgumentException("JSON values must be defined.");
            }

            switch (kind)
            {
                case RoamingNetworkChangeKind.Add when newValue?.ValueKind != JsonValueKind.Object ||
                                                                                           oldValue.HasValue || propertyName is not null:
                    throw new ArgumentException("Add requires an entity object and no old value or property name.");
                case RoamingNetworkChangeKind.Remove when newValue.HasValue || propertyName is not null:
                    throw new ArgumentException("Remove must not contain a new value or property name.");
                case RoamingNetworkChangeKind.UpdateProperty when !newValue.HasValue || String.IsNullOrWhiteSpace(propertyName):
                    throw new ArgumentException("UpdateProperty requires a property name and a new JSON value.");
            }

            Kind = kind;
            PropertyName = propertyName;
            OldValue = oldValue?.Clone();
            NewValue = newValue?.Clone();

        }

        /// <summary>
        /// The operation to perform.
        /// </summary>
        [JsonInclude, JsonRequired]
        public RoamingNetworkChangeKind Kind { get; private init; }

        /// <summary>
        /// The infrastructure entity type targeted by this operation.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String EntityType { get; private init; }

        /// <summary>
        /// The identifier of the target entity.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String EntityId { get; private init; }

        /// <summary>
        /// The JSON property to update, for property operations.
        /// </summary>
        public String? PropertyName { get; }

        /// <summary>
        /// The explicit parent type, when required by the operation.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public String? ParentEntityType { get; }

        /// <summary>
        /// The explicit parent identifier; connectors require their EVSE scope.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public String? ParentEntityId { get; }

        /// <summary>
        /// An optional expected previous value for optimistic conflict detection.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(OptionalJsonElementConverter))]
        public JsonElement? OldValue { get; }

        /// <summary>
        /// The new document or property value; a defined JSON null clears a property.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonConverter(typeof(OptionalJsonElementConverter))]
        public JsonElement? NewValue { get; }

        /// <summary>
        /// Add an entity using its JSON representation.
        /// </summary>
        public static RoamingNetworkChange Add(String       entityType,
                                               String       entityId,
                                               JsonElement  entity)
            => new(RoamingNetworkChangeKind.Add, entityType, entityId, null, null, entity);

        /// <summary>
        /// Add an entity beneath an explicit parent.
        /// </summary>
        public static RoamingNetworkChange Add(String       entityType,
                                               String       entityId,
                                               JsonElement  entity,
                                               String       parentEntityType,
                                               String       parentEntityId)
            => new(RoamingNetworkChangeKind.Add, entityType, entityId, null, null, entity, parentEntityType, parentEntityId);

        /// <summary>
        /// Remove an entity.
        /// </summary>
        public static RoamingNetworkChange Remove(String  entityType,
                                                  String  entityId)
            => new(RoamingNetworkChangeKind.Remove, entityType, entityId, null, null, null);

        /// <summary>
        /// Remove an entity, optionally checking its complete previous document.
        /// </summary>
        public static RoamingNetworkChange Remove(String        entityType,
                                                  String        entityId,
                                                  JsonElement?  oldValue,
                                                  String?       parentEntityType = null,
                                                  String?       parentEntityId   = null)
            => new(RoamingNetworkChangeKind.Remove, entityType, entityId, null, oldValue, null, parentEntityType, parentEntityId);

        /// <summary>
        /// Update one entity property, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(String        entityType,
                                                          String        entityId,
                                                          String        propertyName,
                                                          JsonElement?  oldValue,
                                                          JsonElement   newValue,
                                                          String?       parentEntityType = null,
                                                          String?       parentEntityId   = null)
            => new(RoamingNetworkChangeKind.UpdateProperty, entityType, entityId, propertyName, oldValue, newValue, parentEntityType, parentEntityId);

        private static String Required(String  value,
                                       String  parameterName)
            => !String.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value must not be empty.", parameterName);
    }
}
