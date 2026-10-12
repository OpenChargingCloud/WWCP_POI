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
    /// Changes of the charging pools, charging stations and EVSEs of a roaming network.
    /// </summary>
    /// <remarks>
    /// They are embedded completely in their operator: a change edits the embedded JSON of the operator
    /// and reads it again, so that the changed operator is valid as a whole. Its admin status, status and
    /// internal data, and those of its charging pools, charging stations and EVSEs, go along.
    /// An entity is found by its identification within the operator its identification names.
    /// Adding a charging pool needs no parent, adding a charging station names its charging pool,
    /// adding an EVSE its charging station. A given parent of any other change must be the actual one.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The properties of a charging pool a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableChargingPoolProperties = [
            "name",
            "description",
            "dataSource",
            "dataLicenses",
            "address",
            "geoLocation",
            "parkingType",
            "accessibility",
            "hotline",
            "customData"
        ];

        /// <summary>
        /// The properties of a charging station a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableChargingStationProperties = [
            "name",
            "description",
            "dataSource",
            "physicalReference",
            "address",
            "geoLocation",
            "hotline",
            "manufacturerId",
            "modelId",
            "customData"
        ];

        /// <summary>
        /// The properties of an EVSE a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableEVSEProperties = [
            "name",
            "description",
            "dataSource",
            "physicalReference",
            "currentTypes",
            "chargingModes",
            "maxVoltage",
            "maxCurrent",
            "maxPower",
            "chargingConnectors",
            "customData"
        ];

        #endregion


        #region (private static) IsInfrastructure(EntityType)

        /// <summary>
        /// Whether the given entity type is embedded in charging station operators.
        /// </summary>
        private static Boolean IsInfrastructure(String EntityType)

            => EntityType is nameof(ChargingPool)
                          or nameof(ChargingStation)
                          or nameof(EVSE);

        #endregion

        #region (private static) TryApplyInfrastructureChange(Change, Index, CreatedAt, Operators, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a charging pool, charging station or EVSE to the given operators.
        /// </summary>
        private static Boolean TryApplyInfrastructureChange(RoamingNetworkChange                                             Change,
                                                            Int32                                                            Index,
                                                            DateTimeOffset                                                   CreatedAt,
                                                            Dictionary<ChargingStationOperator_Id, ChargingStationOperator>  Operators,
                                                            out JToken?                                                      CurrentValue,
                                                            [NotNullWhen(false)] out String?                                 ErrorResponse)
        {

            CurrentValue = null;

            var description  = Describe(Change.EntityType);
            var entityId     = Change.EntityId;

            #region The operator, named by the identification of the entity

            ChargingStationOperator_Id? operatorId = Change.EntityType switch {

                nameof(ChargingPool)     => ChargingPool_Id.   TryParse(entityId, out var chargingPoolId)    ? chargingPoolId.   OperatorId : null,
                nameof(ChargingStation)  => ChargingStation_Id.TryParse(entityId, out var chargingStationId) ? chargingStationId.OperatorId : null,
                _                        => EVSE_Id.           TryParse(entityId, out var evseId)            ? evseId.           OperatorId : null

            };

            if (!operatorId.HasValue)
            {
                ErrorResponse = $"Change {Index}: invalid {description} identification '{entityId}'!";
                return false;
            }

            if (!Operators.TryGetValue(operatorId.Value, out var currentOperator))
            {
                ErrorResponse = $"Change {Index}: the charging station operator '{operatorId}' of the {description} '{entityId}' does not exist!";
                return false;
            }

            var operatorJSON = currentOperator.ToEmbeddedJSON();

            #endregion

            #region The entity, the array holding it, and its parent

            var (entityJSON, container, parentType, parentId) = Find(operatorJSON, Change.EntityType, entityId);

            // The actual parent, of an existing entity.
            if (entityJSON is not null &&
                Change.ParentEntityType is not null &&
               (Change.ParentEntityType != parentType || Change.ParentEntityId != parentId))
            {
                ErrorResponse = $"Change {Index}: the {description} '{entityId}' belongs to {parentType} '{parentId}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                return false;
            }

            #endregion

            var isProperty = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            if (Change.Kind != RoamingNetworkChangeKind.Add && entityJSON is null)
            {
                ErrorResponse = $"Change {Index}: the {description} '{entityId}' does not exist!";
                return false;
            }

            // A property change compares the property, every other change the complete entity.
            CurrentValue = isProperty
                               ? entityJSON![Change.PropertyName!]?.DeepClone()
                               : entityJSON?.DeepClone();

            if (Change.OldValue is JToken expected &&
                !OldValueMatches(expected, CurrentValue))
            {
                ErrorResponse = isProperty
                                    ? $"Change {Index}: the property '{Change.PropertyName}' of the {description} '{entityId}' is no longer the expected old value, but '{CurrentValue?.ToString(Newtonsoft.Json.Formatting.None) ?? "absent"}'!"
                                    : $"Change {Index}: the {description} '{entityId}' is no longer the expected old value{(CurrentValue is null ? ": it does not exist" : "")}!";
                return false;
            }

            switch (Change.Kind)
            {

                #region UpdateProperty, RemoveProperty

                case RoamingNetworkChangeKind.UpdateProperty:
                case RoamingNetworkChangeKind.RemoveProperty:

                    var propertyName = Change.PropertyName!;

                    var changeable   = Change.EntityType switch {
                                           nameof(ChargingPool)     => ChangeableChargingPoolProperties,
                                           nameof(ChargingStation)  => ChangeableChargingStationProperties,
                                           _                        => ChangeableEVSEProperties
                                       };

                    if (!changeable.Contains(propertyName))
                    {
                        ErrorResponse = $"Change {Index}: the property '{propertyName}' of a {description} can not be changed!";
                        return false;
                    }

                    if (Change.Kind == RoamingNetworkChangeKind.RemoveProperty)
                    {

                        if (CurrentValue is null)
                        {
                            ErrorResponse = $"Change {Index}: the property '{propertyName}' of the {description} '{entityId}' does not exist!";
                            return false;
                        }

                        entityJSON!.Remove(propertyName);

                    }
                    else
                        entityJSON![propertyName] = Change.NewValue;

                    // The change set's timestamp is the last change, so that its after ETags are deterministic.
                    entityJSON["lastChange"] = CreatedAt.ToISO8601();
                    break;

                #endregion

                #region Add

                case RoamingNetworkChangeKind.Add:

                    if (entityJSON is not null)
                    {
                        ErrorResponse = $"Change {Index}: the {description} '{entityId}' already exists!";
                        return false;
                    }

                    if (Change.NewValue is not JObject newEntity ||
                        newEntity["@id"]?.Type != JTokenType.String ||
                        newEntity["@id"]!.Value<String>() != entityId)
                    {
                        ErrorResponse = $"Change {Index}: the {description} to add must be a JSON object with the identification '{entityId}'!";
                        return false;
                    }

                    if (!TryFindParentArray(operatorJSON, Change, operatorId.Value, out var children, out ErrorResponse))
                    {
                        ErrorResponse = $"Change {Index}: {ErrorResponse}";
                        return false;
                    }

                    // The change set's timestamp is the last change and, unless given, the creation,
                    // so that its after ETags are deterministic.
                    var added = (JObject) newEntity.DeepClone();
                    added["created"]    ??= CreatedAt.ToISO8601();
                    added["lastChange"]   = CreatedAt.ToISO8601();

                    children.Add(added);
                    break;

                #endregion

                #region Remove

                case RoamingNetworkChangeKind.Remove:

                    container!.Remove(entityJSON!);

                    if (container.Count == 0)
                        container.Parent?.Remove();

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a {description} is not supported yet!";
                    return false;

            }

            #region The changed operator must still be a valid one

            if (!ChargingStationOperator.TryParse(operatorJSON, out var changedOperator, out var changedOperatorError))
            {
                ErrorResponse = $"Change {Index}: {changedOperatorError}";
                return false;
            }

            // Admin status, status and internal data are not content: they go along, at every level.
            changedOperator.ShareRuntimeOfAll(currentOperator);

            Operators[operatorId.Value] = changedOperator;

            #endregion

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) Find(OperatorJSON, EntityType, EntityId)

        /// <summary>
        /// The JSON of the entity within the embedded JSON of its operator, the array holding it, and its parent.
        /// </summary>
        private static (JObject? Entity, JArray? Container, String? ParentType, String? ParentId) Find(JObject  OperatorJSON,
                                                                                                    String   EntityType,
                                                                                                    String   EntityId)
        {

            var operatorId = OperatorJSON["@id"]?.Value<String>();

            if (OperatorJSON["chargingPools"] is not JArray chargingPools)
                return (null, null, null, null);

            foreach (var chargingPool in chargingPools.OfType<JObject>())
            {

                var chargingPoolId = chargingPool["@id"]?.Value<String>();

                if (EntityType == nameof(ChargingPool))
                {
                    if (chargingPoolId == EntityId)
                        return (chargingPool, chargingPools, nameof(ChargingStationOperator), operatorId);
                    continue;
                }

                if (chargingPool["chargingStations"] is not JArray chargingStations)
                    continue;

                foreach (var chargingStation in chargingStations.OfType<JObject>())
                {

                    var chargingStationId = chargingStation["@id"]?.Value<String>();

                    if (EntityType == nameof(ChargingStation))
                    {
                        if (chargingStationId == EntityId)
                            return (chargingStation, chargingStations, nameof(ChargingPool), chargingPoolId);
                        continue;
                    }

                    if (chargingStation["EVSEs"] is not JArray evses)
                        continue;

                    foreach (var evse in evses.OfType<JObject>())
                        if (evse["@id"]?.Value<String>() == EntityId)
                            return (evse, evses, nameof(ChargingStation), chargingStationId);

                }

            }

            return (null, null, null, null);

        }

        #endregion

        #region (private static) TryFindParentArray(OperatorJSON, Change, OperatorId, out Children, out ErrorResponse)

        /// <summary>
        /// The array a new entity is added to: of the operator for a charging pool,
        /// of the named charging pool for a charging station, of the named charging station for an EVSE.
        /// </summary>
        private static Boolean TryFindParentArray(JObject                                OperatorJSON,
                                                  RoamingNetworkChange                   Change,
                                                  ChargingStationOperator_Id             OperatorId,
                                                  [NotNullWhen(true)]  out JArray?       Children,
                                                  [NotNullWhen(false)] out String?       ErrorResponse)
        {

            Children = null;

            JObject  parent;
            String   childrenName;

            switch (Change.EntityType)
            {

                case nameof(ChargingPool):

                    if (Change.ParentEntityType is not null &&
                       (Change.ParentEntityType != nameof(ChargingStationOperator) || Change.ParentEntityId != OperatorId.ToString()))
                    {
                        ErrorResponse = $"a charging pool '{Change.EntityId}' belongs to ChargingStationOperator '{OperatorId}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                        return false;
                    }

                    parent        = OperatorJSON;
                    childrenName  = "chargingPools";
                    break;

                case nameof(ChargingStation):

                    if (Change.ParentEntityType != nameof(ChargingPool) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the charging station '{Change.EntityId}' needs its ChargingPool as its parent!";
                        return false;
                    }

                    if (Find(OperatorJSON, nameof(ChargingPool), Change.ParentEntityId).Entity is not JObject chargingPool)
                    {
                        ErrorResponse = $"the charging pool '{Change.ParentEntityId}' of the charging station '{Change.EntityId}' does not exist!";
                        return false;
                    }

                    parent        = chargingPool;
                    childrenName  = "chargingStations";
                    break;

                default:

                    if (Change.ParentEntityType != nameof(ChargingStation) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the EVSE '{Change.EntityId}' needs its ChargingStation as its parent!";
                        return false;
                    }

                    if (Find(OperatorJSON, nameof(ChargingStation), Change.ParentEntityId).Entity is not JObject chargingStation)
                    {
                        ErrorResponse = $"the charging station '{Change.ParentEntityId}' of the EVSE '{Change.EntityId}' does not exist!";
                        return false;
                    }

                    parent        = chargingStation;
                    childrenName  = "EVSEs";
                    break;

            }

            if (parent[childrenName] is not JArray children)
            {
                children = [];
                parent[childrenName] = children;
            }

            Children       = children;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (private static) Describe(EntityType)

        private static String Describe(String EntityType)

            => EntityType switch {
                   nameof(ChargingPool)     => "charging pool",
                   nameof(ChargingStation)  => "charging station",
                   _                        => "EVSE"
               };

        #endregion

    }

}
