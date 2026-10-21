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
    /// Changes of the parking garages, parking spaces, parking sensors, parking space groups and parking products of a roaming network.
    /// </summary>
    /// <remarks>
    /// They are embedded completely in their parking operator: a change edits the embedded JSON of the
    /// parking operator and reads it again, so that the changed parking operator is valid as a whole.
    /// Its admin status, status and internal data, and those of its garages, spaces, sensors and space groups, go along.
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
            ParkingGarage.BasePropertyKeys.Name,
            ParkingGarage.BasePropertyKeys.Description,
            ParkingGarage.BasePropertyKeys.DataSource,
            ParkingGarage.PropertyKeys.Address,
            ParkingGarage.PropertyKeys.GeoLocation,
            ParkingGarage.PropertyKeys.ChargingStationIds,
            ParkingGarage.PropertyKeys.OpeningTimes,
            ParkingGarage.PropertyKeys.Images,
            ParkingGarage.PropertyKeys.ParkingProductIds,
            ParkingGarage.BasePropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of a parking space a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingSpaceProperties = [
            ParkingSpace.BasePropertyKeys.Name,
            ParkingSpace.BasePropertyKeys.Description,
            ParkingSpace.BasePropertyKeys.DataSource,
            ParkingSpace.PropertyKeys.GeoLocation,
            ParkingSpace.PropertyKeys.ParkingGarageId,
            ParkingSpace.PropertyKeys.ParkingSensorIds,
            ParkingSpace.PropertyKeys.ChargingStationIds,
            ParkingSpace.PropertyKeys.ParkingProductIds,
            ParkingSpace.PropertyKeys.ParkingRestrictions,
            ParkingSpace.PropertyKeys.EUVehicleCategories,
            ParkingSpace.PropertyKeys.PhysicalReference,
            ParkingSpace.PropertyKeys.VehicleTypes,
            ParkingSpace.PropertyKeys.RestrictedToVehicleTypes,
            ParkingSpace.PropertyKeys.MaxVehicleWeight,
            ParkingSpace.PropertyKeys.MaxVehicleHeight,
            ParkingSpace.PropertyKeys.MaxVehicleLength,
            ParkingSpace.PropertyKeys.MaxVehicleWidth,
            ParkingSpace.PropertyKeys.Length,
            ParkingSpace.PropertyKeys.Width,
            ParkingSpace.PropertyKeys.EVSEPosition,
            ParkingSpace.PropertyKeys.Direction,
            ParkingSpace.PropertyKeys.Surface,
            ParkingSpace.PropertyKeys.Slope,
            ParkingSpace.PropertyKeys.DriveThrough,
            ParkingSpace.PropertyKeys.ReservationRequired,
            ParkingSpace.PropertyKeys.TimeLimit,
            ParkingSpace.PropertyKeys.Roofed,
            ParkingSpace.PropertyKeys.Illuminated,
            ParkingSpace.PropertyKeys.RefrigerationOutlet,
            ParkingSpace.PropertyKeys.DangerousGoodsAllowed,
            ParkingSpace.PropertyKeys.ProtectedArea,
            ParkingSpace.PropertyKeys.APDSReference,
            ParkingSpace.PropertyKeys.Standards,
            ParkingSpace.PropertyKeys.Images,
            ParkingSpace.PropertyKeys.TemplateIds,
            ParkingSpace.BasePropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of a parking product a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingProductProperties = [
            ParkingProduct.PropertyKeys.Name,
            ParkingProduct.PropertyKeys.Description,
            ParkingProduct.PropertyKeys.MinDuration,
            ParkingProduct.PropertyKeys.StopParkingAfterTime,
            ParkingProduct.PropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of a parking space group a change set can update or remove: its members, as it gives them nothing.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingSpaceGroupProperties = [
            ParkingSpaceGroup.BasePropertyKeys.Name,
            ParkingSpaceGroup.BasePropertyKeys.Description,
            ParkingSpaceGroup.BasePropertyKeys.DataSource,
            ParkingSpaceGroup.PropertyKeys.ParkingSpaceIds,
            ParkingSpaceGroup.BasePropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of a parking space template a change set can update or remove: those of a parking space it gives
        /// its members, all but its identification, name, description, data source, custom data, templates and APDS reference.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingSpaceTemplateProperties = [
            ParkingSpaceTemplate.PropertyKeys.Name,
            ParkingSpaceTemplate.PropertyKeys.Description,
            ParkingSpaceTemplate.PropertyKeys.DataSource,
            ParkingSpaceTemplate.PropertyKeys.ParkingProductIds,
            ParkingSpaceTemplate.PropertyKeys.CustomData,
            ParkingSpaceTemplate.PropertyKeys.GeoLocation,
            ParkingSpaceTemplate.PropertyKeys.ParkingGarageId,
            ParkingSpaceTemplate.PropertyKeys.ParkingSensorIds,
            ParkingSpaceTemplate.PropertyKeys.ChargingStationIds,
            ParkingSpaceTemplate.PropertyKeys.ParkingRestrictions,
            ParkingSpaceTemplate.PropertyKeys.EUVehicleCategories,
            ParkingSpaceTemplate.PropertyKeys.PhysicalReference,
            ParkingSpaceTemplate.PropertyKeys.VehicleTypes,
            ParkingSpaceTemplate.PropertyKeys.MaxVehicleWeight,
            ParkingSpaceTemplate.PropertyKeys.MaxVehicleHeight,
            ParkingSpaceTemplate.PropertyKeys.MaxVehicleLength,
            ParkingSpaceTemplate.PropertyKeys.MaxVehicleWidth,
            ParkingSpaceTemplate.PropertyKeys.Length,
            ParkingSpaceTemplate.PropertyKeys.Width,
            ParkingSpaceTemplate.PropertyKeys.EVSEPosition,
            ParkingSpaceTemplate.PropertyKeys.Direction,
            ParkingSpaceTemplate.PropertyKeys.Surface,
            ParkingSpaceTemplate.PropertyKeys.Slope,
            ParkingSpaceTemplate.PropertyKeys.TimeLimit,
            ParkingSpaceTemplate.PropertyKeys.RestrictedToVehicleTypes,
            ParkingSpaceTemplate.PropertyKeys.DriveThrough,
            ParkingSpaceTemplate.PropertyKeys.ReservationRequired,
            ParkingSpaceTemplate.PropertyKeys.Roofed,
            ParkingSpaceTemplate.PropertyKeys.Illuminated,
            ParkingSpaceTemplate.PropertyKeys.RefrigerationOutlet,
            ParkingSpaceTemplate.PropertyKeys.DangerousGoodsAllowed,
            ParkingSpaceTemplate.PropertyKeys.ProtectedArea,
            ParkingSpaceTemplate.PropertyKeys.Standards,
            ParkingSpaceTemplate.PropertyKeys.Images
        ];

        /// <summary>
        /// The properties of a parking sensor a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableParkingSensorProperties = [
            ParkingSensor.BasePropertyKeys.Name,
            ParkingSensor.BasePropertyKeys.Description,
            ParkingSensor.BasePropertyKeys.DataSource,
            ParkingSensor.PropertyKeys.GeoLocation,
            ParkingSensor.PropertyKeys.ChargingStationIds,
            ParkingSensor.BasePropertyKeys.CustomData
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
                          or nameof(ParkingSpaceTemplate)
                          or nameof(ParkingSpaceGroup)
                          or nameof(ParkingProduct);

        #endregion

        #region (private static) ParkingId(EntityType, EntityId)

        /// <summary>
        /// The identification of the given parking entity type the given text names, compared as its type compares it;
        /// null for an invalid one.
        /// </summary>
        private static Object? ParkingId(String  EntityType,
                                         String  EntityId)

            => EntityType switch {
                   nameof(ParkingGarage)      => ParkingGarage_Id.    TryParse(EntityId, out var garageId)  ? garageId  : null,
                   nameof(ParkingSpace)       => ParkingSpace_Id.     TryParse(EntityId, out var spaceId)   ? spaceId   : null,
                   nameof(ParkingSensor)      => ParkingSensor_Id.    TryParse(EntityId, out var sensorId)  ? sensorId  : null,
                   nameof(ParkingSpaceTemplate)  => ParkingSpaceTemplate_Id.TryParse(EntityId, out var templateId)  ? templateId : null,
                   nameof(ParkingSpaceGroup)  => ParkingSpaceGroup_Id.TryParse(EntityId, out var groupId)   ? groupId   : null,
                   nameof(ParkingProduct)     => ParkingProduct_Id.   TryParse(EntityId, out var productId) ? productId : null,
                   _                          => null
               };

        #endregion

        #region (private static) ParkingStoredId(ParkingOperator, EntityType, EntityId)

        /// <summary>
        /// The identification the given parking operator keeps the named parking entity with, as its JSON writes it.
        /// Identifications are equal as their types compare them, not as texts.
        /// Null for an invalid identification or an entity the parking operator does not have.
        /// </summary>
        private static String? ParkingStoredId(ParkingOperator  ParkingOperator,
                                               String           EntityType,
                                               String           EntityId)

            => EntityType switch {
                   nameof(ParkingGarage)      => ParkingGarage_Id.    TryParse(EntityId, out var garageId)  && ParkingOperator.TryGetParkingGarage    (garageId,  out var garage)  ? garage. Id.ToString() : null,
                   nameof(ParkingSpace)       => ParkingSpace_Id.     TryParse(EntityId, out var spaceId)   && ParkingOperator.TryGetParkingSpace     (spaceId,   out var space)   ? space.  Id.ToString() : null,
                   nameof(ParkingSensor)      => ParkingSensor_Id.    TryParse(EntityId, out var sensorId)  && ParkingOperator.TryGetParkingSensor    (sensorId,  out var sensor)  ? sensor. Id.ToString() : null,
                   nameof(ParkingSpaceTemplate)  => ParkingSpaceTemplate_Id.TryParse(EntityId, out var templateId) && ParkingOperator.TryGetParkingSpaceTemplate(templateId, out var template) ? template.Id.ToString() : null,
                   nameof(ParkingSpaceGroup)  => ParkingSpaceGroup_Id.TryParse(EntityId, out var groupId)   && ParkingOperator.TryGetParkingSpaceGroup(groupId,   out var group)   ? group.  Id.ToString() : null,
                   nameof(ParkingProduct)     => ParkingProduct_Id.   TryParse(EntityId, out var productId) && ParkingOperator.TryGetParkingProduct   (productId, out var product) ? product.Id.ToString() : null,
                   _                          => null
               };

        #endregion

        #region (private static) OperatorParkingSpace(ChargingStationOperators, EntityType, EntityId)

        /// <summary>
        /// The charging station operator defining the named parking space itself, where no parking operator does;
        /// null for any other entity type, or one no charging station operator has.
        /// </summary>
        private static ChargingStationOperator? OperatorParkingSpace(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                                     String                                EntityType,
                                                                     String                                EntityId)

            => EntityType == nameof(ParkingSpace) && ParkingSpace_Id.TryParse(EntityId, out var spaceId)
                   ? ChargingStationOperators.FirstOrDefault(chargingStationOperator => chargingStationOperator.TryGetParkingSpace(spaceId, out _))
                   : null;

        #endregion

        #region (private static) TryApplyParkingChange(Change, Index, CreatedAt, ParkingOperators, ChargingStationOperators, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a parking garage, parking space, parking sensor, parking space template or group, or parking product
        /// to the given parking operators; of a parking space a charging station operator defines, to the given
        /// charging station operators.
        /// </summary>
        private static Boolean TryApplyParkingChange(RoamingNetworkChange                                             Change,
                                                     Int32                                                            Index,
                                                     DateTimeOffset                                                   CreatedAt,
                                                     Dictionary<ParkingOperator_Id, ParkingOperator>                  ParkingOperators,
                                                     Dictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                     out JToken?                                                      CurrentValue,
                                                     [NotNullWhen(false)] out String?                                 ErrorResponse)
        {

            CurrentValue = null;

            var (description, arrayName, changeable) = Change.EntityType switch {
                nameof(ParkingGarage)  => ("parking garage", ParkingOperator.PropertyKeys.ParkingGarages, ChangeableParkingGarageProperties),
                nameof(ParkingSpace)   => ("parking space",  ParkingOperator.PropertyKeys.ParkingSpaces,  ChangeableParkingSpaceProperties),
                nameof(ParkingSpaceTemplate) => ("parking space template", ParkingOperator.PropertyKeys.ParkingSpaceTemplates, ChangeableParkingSpaceTemplateProperties),
                nameof(ParkingSpaceGroup) => ("parking space group", ParkingOperator.PropertyKeys.ParkingSpaceGroups, ChangeableParkingSpaceGroupProperties),
                nameof(ParkingProduct)    => ("parking product",     ParkingOperator.PropertyKeys.ParkingProducts,    ChangeableParkingProductProperties),
                _                      => ("parking sensor", ParkingOperator.PropertyKeys.ParkingSensors, ChangeableParkingSensorProperties)
            };

            var entityId    = Change.EntityId;

            // Parking products have no timestamps.
            var timestamps  = Change.EntityType != nameof(ParkingProduct);

            #region The parking operator: the one having it, or the parent of a new one; or a charging station operator

            if (ParkingId(Change.EntityType, entityId) is null)
            {
                ErrorResponse = $"Change {Index}: invalid {description} identification '{entityId}'!";
                return false;
            }

            var owner          = ParkingOperators.Values.FirstOrDefault(parkingOperator => ParkingStoredId(parkingOperator, Change.EntityType, entityId) is not null);
            // A parking space defined where no parking operator does.
            var operatorOwner  = OperatorParkingSpace(ChargingStationOperators.Values, Change.EntityType, entityId);

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {

                if (owner is not null || operatorOwner is not null)
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' already exists!";
                    return false;
                }

                if (Change.EntityType == nameof(ParkingSpace) &&
                    Change.ParentEntityType == nameof(ChargingStationOperator))
                {
                    if (!ChargingStationOperator_Id.TryParse(Change.ParentEntityId ?? "", out var operatorId) ||
                        !ChargingStationOperators.TryGetValue(operatorId, out operatorOwner))
                    {
                        ErrorResponse = $"Change {Index}: the {description} '{entityId}' to add needs its ChargingStationOperator, an existing one!";
                        return false;
                    }
                }

                else if (Change.ParentEntityType != nameof(ParkingOperator) ||
                         !ParkingOperator_Id.TryParse(Change.ParentEntityId ?? "", out var parentId) ||
                         !ParkingOperators.TryGetValue(parentId, out owner))
                {
                    ErrorResponse = Change.EntityType == nameof(ParkingSpace)
                                        ? $"Change {Index}: the {description} '{entityId}' to add needs its ParkingOperator or ChargingStationOperator, an existing one!"
                                        : $"Change {Index}: the {description} '{entityId}' to add needs its ParkingOperator, an existing one!";
                    return false;
                }

            }

            else
            {

                if (owner is null && operatorOwner is null)
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' does not exist!";
                    return false;
                }

                if (owner is not null &&
                    Change.ParentEntityType is not null &&
                   (Change.ParentEntityType != nameof(ParkingOperator)                         ||
                    !ParkingOperator_Id.TryParse(Change.ParentEntityId ?? "", out var namedParentId) ||
                    namedParentId != owner.Id))
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' belongs to ParkingOperator '{owner.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                    return false;
                }

                if (operatorOwner is not null &&
                    Change.ParentEntityType is not null &&
                   (Change.ParentEntityType != nameof(ChargingStationOperator)                           ||
                    !ChargingStationOperator_Id.TryParse(Change.ParentEntityId ?? "", out var namedOperatorId) ||
                    namedOperatorId != operatorOwner.Id))
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' belongs to ChargingStationOperator '{operatorOwner.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                    return false;
                }

            }

            var operatorJSON  = owner is not null
                                    ? owner.        ToJSON(Embedded: true)
                                    : operatorOwner!.ToEmbeddedJSON();
            var children      = operatorJSON[arrayName] as JArray;
            // Found under the identification it is kept with: the change may name it in every spelling
            // its identification type considers equal, e.g. without case for garages, with case for products.
            var storedId      = owner is not null
                                    ? ParkingStoredId(owner, Change.EntityType, entityId)
                                    : ParkingSpace_Id.TryParse(entityId, out var operatorSpaceId) && operatorOwner!.TryGetParkingSpace(operatorSpaceId, out var operatorSpace)
                                          ? operatorSpace.Id.ToString()
                                          : null;
            var entityJSON    = Change.Kind == RoamingNetworkChangeKind.Add
                                    ? null
                                    : children?.OfType<JObject>().FirstOrDefault(child => child[BasePropertyKeys.Id]?.Value<String>() == storedId);

            #endregion

            // An element change is applied as the change of the property it amounts to.
            var isElementChange = Change.IsElementChange;

            if (!TryResolveElementChange(ref Change, entityJSON, description, out var elementValue, out ErrorResponse))
            {
                ErrorResponse = $"Change {Index}: {ErrorResponse}";
                return false;
            }

            var isProperty = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            // A property change compares the property, every other change the complete entity.
            CurrentValue = isProperty
                               ? entityJSON![Change.PropertyName!]?.DeepClone()
                               : entityJSON?.DeepClone();

            if (Change.OldValue is JToken expected &&
                !OldValueMatches(expected, CurrentValue))
            {
                ErrorResponse = isProperty
                                    ? $"Change {Index}: the property '{Change.PropertyName}' of the {description} '{entityId}' is no longer the expected old value, but '{POIJSON.ContentText(CurrentValue)}'!"
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
                    if (timestamps)
                        entityJSON[BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();

                    break;

                #endregion

                #region Add

                case RoamingNetworkChangeKind.Add:

                    if (Change.NewValue is not JObject newEntity ||
                        newEntity[BasePropertyKeys.Id]?.Type != JTokenType.String ||
                        newEntity[BasePropertyKeys.Id]!.Value<String>() != entityId)
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

                    if (timestamps)
                    {
                        added[BasePropertyKeys.Created]    ??= CreatedAt.ToISO8601();
                        added[BasePropertyKeys.LastChange]   = CreatedAt.ToISO8601();
                    }

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

            // The operator embedding it changed with it, at the time of the change set.
            operatorJSON[BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();

            #region The changed operator must still be a valid one

            if (owner is not null)
            {

                if (!ParkingOperator.TryParse(operatorJSON, out var changedOperator, out var changedOperatorError))
                {
                    ErrorResponse = $"Change {Index}: {changedOperatorError}";
                    return false;
                }

                // Admin status, status and internal data are not content: they go along, at every level.
                changedOperator.ShareRuntimeOfAll(owner);

                ParkingOperators[owner.Id] = changedOperator;

            }

            else
            {

                if (!ChargingStationOperator.TryParseWithinChangeSet(operatorJSON, out var changedOperator, out var changedOperatorError))
                {
                    ErrorResponse = $"Change {Index}: {changedOperatorError}";
                    return false;
                }

                changedOperator.ShareRuntimeOfAll(operatorOwner!);

                ChargingStationOperators[operatorOwner!.Id] = changedOperator;

            }

            #endregion

            // The old value of an element change is the value of what it addresses.
            if (isElementChange)
                CurrentValue = elementValue;

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
