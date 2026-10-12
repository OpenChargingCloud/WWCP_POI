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
    /// Changes of the parking garages, parking spaces and parking sensors of a roaming network.
    /// </summary>
    /// <remarks>
    /// They are embedded completely in their parking operator: a change edits the embedded JSON of the
    /// parking operator and reads it again, so that the changed parking operator is valid as a whole.
    /// Its admin status, status and internal data, and those of its garages, spaces and sensors, go along.
    /// Their identifications are unique within the network, so an existing one is found without its parent;
    /// adding one names its parking operator. A given parent of any other change must be the actual one.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The properties of a parking garage a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingGarageProperties = [
            "name",
            "description",
            "dataSource",
            "address",
            "geoLocation",
            "chargingStationIds",
            "customData"
        ];

        /// <summary>
        /// The properties of a parking space a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingSpaceProperties = [
            "name",
            "description",
            "dataSource",
            "geoLocation",
            "parkingGarageId",
            "parkingSensorIds",
            "chargingStationIds",
            "customData"
        ];

        /// <summary>
        /// The properties of a parking space group a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingSpaceGroupProperties = [
            "name",
            "description",
            "dataSource",
            "parkingSpaceIds",
            "customData"
        ];

        /// <summary>
        /// The properties of a parking sensor a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingSensorProperties = [
            "name",
            "description",
            "dataSource",
            "geoLocation",
            "chargingStationIds",
            "customData"
        ];

        #endregion


        #region (private static) IsParking(EntityType)

        /// <summary>
        /// Whether the given entity type is embedded in parking operators.
        /// </summary>
        private static Boolean IsParking(String EntityType)

            => EntityType is nameof(ParkingGarage)
                          or nameof(ParkingSpace)
                          or nameof(ParkingSensor)
                          or nameof(ParkingSpaceGroup);

        #endregion

        #region (private static) TryApplyParkingChange(Change, Index, CreatedAt, ParkingOperators, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a parking garage, parking space or parking sensor to the given parking operators.
        /// </summary>
        private static Boolean TryApplyParkingChange(RoamingNetworkChange                             Change,
                                                     Int32                                            Index,
                                                     DateTimeOffset                                   CreatedAt,
                                                     Dictionary<ParkingOperator_Id, ParkingOperator>  ParkingOperators,
                                                     out JToken?                                      CurrentValue,
                                                     [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            CurrentValue = null;

            var (description, arrayName, changeable) = Change.EntityType switch {
                nameof(ParkingGarage)  => ("parking garage", "parkingGarages", ChangeableParkingGarageProperties),
                nameof(ParkingSpace)   => ("parking space",  "parkingSpaces",  ChangeableParkingSpaceProperties),
                nameof(ParkingSpaceGroup) => ("parking space group", "parkingSpaceGroups", ChangeableParkingSpaceGroupProperties),
                _                      => ("parking sensor", "parkingSensors", ChangeableParkingSensorProperties)
            };

            var entityId = Change.EntityId;

            #region The parking operator: the one having it, or the parent of a new one

            Func<ParkingOperator, Boolean>? owns = Change.EntityType switch {
                nameof(ParkingGarage)  => ParkingGarage_Id.TryParse(entityId, out var garageId) ? parkingOperator => parkingOperator.TryGetParkingGarage(garageId, out _) : null,
                nameof(ParkingSpace)   => ParkingSpace_Id. TryParse(entityId, out var spaceId)  ? parkingOperator => parkingOperator.TryGetParkingSpace (spaceId,  out _) : null,
                nameof(ParkingSpaceGroup) => ParkingSpaceGroup_Id.TryParse(entityId, out var groupId) ? parkingOperator => parkingOperator.TryGetParkingSpaceGroup(groupId, out _) : null,
                _                      => ParkingSensor_Id.TryParse(entityId, out var sensorId) ? parkingOperator => parkingOperator.TryGetParkingSensor(sensorId, out _) : null
            };

            if (owns is null)
            {
                ErrorResponse = $"Change {Index}: invalid {description} identification '{entityId}'!";
                return false;
            }

            var owner = ParkingOperators.Values.FirstOrDefault(owns);

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {

                if (owner is not null)
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' already exists!";
                    return false;
                }

                if (Change.ParentEntityType != nameof(ParkingOperator) ||
                    !ParkingOperator_Id.TryParse(Change.ParentEntityId ?? "", out var parentId) ||
                    !ParkingOperators.TryGetValue(parentId, out owner))
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' to add needs its ParkingOperator, an existing one!";
                    return false;
                }

            }

            else
            {

                if (owner is null)
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' does not exist!";
                    return false;
                }

                if (Change.ParentEntityType is not null &&
                   (Change.ParentEntityType != nameof(ParkingOperator) || Change.ParentEntityId != owner.Id.ToString()))
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' belongs to ParkingOperator '{owner.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                    return false;
                }

            }

            var operatorJSON  = owner.ToJSON();
            var children      = operatorJSON[arrayName] as JArray;
            // Identifications compare without case sensitivity.
            var entityJSON    = Change.Kind == RoamingNetworkChangeKind.Add
                                    ? null
                                    : children?.OfType<JObject>().FirstOrDefault(child => String.Equals(child["@id"]?.Value<String>(), entityId, StringComparison.OrdinalIgnoreCase));

            #endregion

            var isProperty = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

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

                    if (Change.NewValue is not JObject newEntity ||
                        newEntity["@id"]?.Type != JTokenType.String ||
                        newEntity["@id"]!.Value<String>() != entityId)
                    {
                        ErrorResponse = $"Change {Index}: the {description} to add must be a JSON object with the identification '{entityId}'!";
                        return false;
                    }

                    if (children is null)
                    {
                        children = [];
                        operatorJSON[arrayName] = children;
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

                    children!.Remove(entityJSON!);

                    if (children.Count == 0)
                        operatorJSON.Remove(arrayName);

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a {description} is not supported yet!";
                    return false;

            }

            #region The changed parking operator must still be a valid one

            if (!ParkingOperator.TryParse(operatorJSON, out var changedOperator, out var changedOperatorError))
            {
                ErrorResponse = $"Change {Index}: {changedOperatorError}";
                return false;
            }

            // Admin status, status and internal data are not content: they go along, at every level.
            changedOperator.ShareRuntimeOfAll(owner);

            ParkingOperators[owner.Id] = changedOperator;

            #endregion

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
