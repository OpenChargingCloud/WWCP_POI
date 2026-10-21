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
            ChargingPool.BasePropertyKeys.Name,
            ChargingPool.BasePropertyKeys.Description,
            ChargingPool.BasePropertyKeys.DataSource,
            ChargingPool.BasePropertyKeys.DataLicenses,
            ChargingPool.PropertyKeys.Address,
            ChargingPool.PropertyKeys.GeoLocation,
            ChargingPool.PropertyKeys.ParkingType,
            ChargingPool.PropertyKeys.Accessibility,
            ChargingPool.PropertyKeys.HotlinePhoneNumber,
            ChargingPool.PropertyKeys.GridConnectionPointId,
            ChargingPool.PropertyKeys.EnergyMeters,
            ChargingPool.PropertyKeys.Brands,
            ChargingPool.PropertyKeys.OpeningTimes,
            ChargingPool.PropertyKeys.EnergyMix,
            ChargingPool.PropertyKeys.Images,
            ChargingPool.PropertyKeys.TimeZone,
            ChargingPool.PropertyKeys.Facilities,
            ChargingPool.PropertyKeys.PaymentOptions,
            ChargingPool.PropertyKeys.AuthenticationModes,
            ChargingPool.PropertyKeys.UIFeatures,
            ChargingPool.PropertyKeys.Features,
            ChargingPool.PropertyKeys.LocationLanguages,
            ChargingPool.PropertyKeys.EntranceAddress,
            ChargingPool.PropertyKeys.EntranceLocation,
            ChargingPool.PropertyKeys.ExitAddress,
            ChargingPool.PropertyKeys.ExitLocation,
            ChargingPool.PropertyKeys.ArrivalInstructions,
            ChargingPool.PropertyKeys.ChargingWhenClosed,
            ChargingPool.PropertyKeys.ParkingRestrictions,
            ChargingPool.PropertyKeys.RelatedLocations,
            ChargingPool.PropertyKeys.Services,
            ChargingPool.PropertyKeys.AssistanceServiceDetails,
            ChargingPool.PropertyKeys.Standards,
            ChargingPool.PropertyKeys.NUTSCodes,
            ChargingPool.PropertyKeys.TemplateIds,
            ChargingPool.BasePropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of a charging station a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableChargingStationProperties = [
            ChargingStation.BasePropertyKeys.Name,
            ChargingStation.BasePropertyKeys.Description,
            ChargingStation.BasePropertyKeys.DataSource,
            ChargingStation.PropertyKeys.PhysicalReference,
            ChargingStation.PropertyKeys.Address,
            ChargingStation.PropertyKeys.GeoLocation,
            ChargingStation.PropertyKeys.HotlinePhoneNumber,
            ChargingStation.PropertyKeys.ManufacturerId,
            ChargingStation.PropertyKeys.ModelId,
            ChargingStation.PropertyKeys.EnergyMeters,
            ChargingStation.PropertyKeys.Brands,
            ChargingStation.PropertyKeys.OpeningTimes,
            ChargingStation.PropertyKeys.Images,
            ChargingStation.PropertyKeys.VehicleTypes,
            ChargingStation.PropertyKeys.EUVehicleCategories,
            ChargingStation.PropertyKeys.Accessibility,
            ChargingStation.PropertyKeys.Features,
            ChargingStation.PropertyKeys.CalibrationInfo,
            ChargingStation.PropertyKeys.CertificationInfo,
            ChargingStation.PropertyKeys.MaxPower,
            ChargingStation.PropertyKeys.ParkingSpaceIds,
            ChargingStation.PropertyKeys.TemplateIds,
            ChargingStation.BasePropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of an EVSE a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableEVSEProperties = [
            EVSE.BasePropertyKeys.Name,
            EVSE.BasePropertyKeys.Description,
            EVSE.BasePropertyKeys.DataSource,
            EVSE.PropertyKeys.PhysicalReference,
            EVSE.PropertyKeys.CurrentTypes,
            EVSE.PropertyKeys.ChargingModes,
            EVSE.PropertyKeys.MaxVoltage,
            EVSE.PropertyKeys.MaxCurrent,
            EVSE.PropertyKeys.MaxPower,
            EVSE.PropertyKeys.ChargingConnectors,
            EVSE.PropertyKeys.EnergyMeter,
            EVSE.PropertyKeys.ChargingTariffIds,
            EVSE.PropertyKeys.Brands,
            EVSE.PropertyKeys.Images,
            EVSE.PropertyKeys.GeoLocation,
            EVSE.PropertyKeys.MaxReservationDuration,
            EVSE.PropertyKeys.ReachDistance,
            EVSE.PropertyKeys.OperationTimeout,
            EVSE.PropertyKeys.ExtendedOperationTimeout,
            EVSE.PropertyKeys.Standards,
            EVSE.PropertyKeys.FloorLevel,
            EVSE.PropertyKeys.AcceptedEMobilityProviderIds,
            EVSE.PropertyKeys.TemplateIds,
            EVSE.BasePropertyKeys.CustomData
        ];

        /// <summary>
        /// The direct children of an operator besides its charging pools, by their JSON keys.
        /// </summary>
        private static readonly ImmutableDictionary<String, String> OperatorChildArrays = new Dictionary<String, String> {
            { nameof(ChargingTariff),           ChargingStationOperator.PropertyKeys.ChargingTariffs          },
            { nameof(EVSETemplate),             ChargingStationOperator.PropertyKeys.EVSETemplates            },
            { nameof(ChargingStationTemplate),  ChargingStationOperator.PropertyKeys.ChargingStationTemplates },
            { nameof(ChargingPoolTemplate),     ChargingStationOperator.PropertyKeys.ChargingPoolTemplates    },
            { nameof(ChargingTariffTemplate),   ChargingStationOperator.PropertyKeys.ChargingTariffTemplates  },
            { nameof(EVSEGroup),            ChargingStationOperator.PropertyKeys.EVSEGroups            },
            { nameof(ChargingStationGroup), ChargingStationOperator.PropertyKeys.ChargingStationGroups },
            { nameof(ChargingPoolGroup),    ChargingStationOperator.PropertyKeys.ChargingPoolGroups    },
            { nameof(ChargingTariffGroup),  ChargingStationOperator.PropertyKeys.ChargingTariffGroups  }
        }.ToImmutableDictionary();

        /// <summary>
        /// The properties of a charging station a charging station template can give its members:
        /// all but its identification, name, description, data source, custom data, templates, EVSEs and energy meters.
        /// </summary>
        internal static readonly ImmutableArray<String> ChargingStationTemplateStationProperties = [
            ChargingStation.PropertyKeys.PhysicalReference,
            ChargingStation.PropertyKeys.Address,
            ChargingStation.PropertyKeys.GeoLocation,
            ChargingStation.PropertyKeys.HotlinePhoneNumber,
            ChargingStation.PropertyKeys.ManufacturerId,
            ChargingStation.PropertyKeys.ModelId,
            ChargingStation.PropertyKeys.Brands,
            ChargingStation.PropertyKeys.OpeningTimes,
            ChargingStation.PropertyKeys.Images,
            ChargingStation.PropertyKeys.VehicleTypes,
            ChargingStation.PropertyKeys.EUVehicleCategories,
            ChargingStation.PropertyKeys.Accessibility,
            ChargingStation.PropertyKeys.Features,
            ChargingStation.PropertyKeys.CalibrationInfo,
            ChargingStation.PropertyKeys.CertificationInfo,
            ChargingStation.PropertyKeys.MaxPower,
            ChargingStation.PropertyKeys.ParkingSpaceIds
        ];

        /// <summary>
        /// The properties of an EVSE an EVSE template can give its members:
        /// all but its identification, name, description, data source, custom data, templates, energy meter and charging tariffs.
        /// </summary>
        internal static readonly ImmutableArray<String> EVSETemplateEVSEProperties = [
            EVSE.PropertyKeys.PhysicalReference,
            EVSE.PropertyKeys.CurrentTypes,
            EVSE.PropertyKeys.ChargingModes,
            EVSE.PropertyKeys.MaxVoltage,
            EVSE.PropertyKeys.MaxCurrent,
            EVSE.PropertyKeys.MaxPower,
            EVSE.PropertyKeys.ChargingConnectors,
            EVSE.PropertyKeys.Brands,
            EVSE.PropertyKeys.Images,
            EVSE.PropertyKeys.GeoLocation,
            EVSE.PropertyKeys.MaxReservationDuration,
            EVSE.PropertyKeys.ReachDistance,
            EVSE.PropertyKeys.OperationTimeout,
            EVSE.PropertyKeys.ExtendedOperationTimeout,
            EVSE.PropertyKeys.Standards,
            EVSE.PropertyKeys.FloorLevel,
            EVSE.PropertyKeys.AcceptedEMobilityProviderIds
        ];

        /// <summary>
        /// The properties of a charging pool a charging pool template can give its members:
        /// all but its identification, name, description, data source, custom data, templates, charging stations and energy meters.
        /// </summary>
        internal static readonly ImmutableArray<String> ChargingPoolTemplatePoolProperties = [
            ChargingPool.BasePropertyKeys.DataLicenses,
            ChargingPool.PropertyKeys.Address,
            ChargingPool.PropertyKeys.GeoLocation,
            ChargingPool.PropertyKeys.ParkingType,
            ChargingPool.PropertyKeys.Accessibility,
            ChargingPool.PropertyKeys.HotlinePhoneNumber,
            ChargingPool.PropertyKeys.GridConnectionPointId,
            ChargingPool.PropertyKeys.Brands,
            ChargingPool.PropertyKeys.OpeningTimes,
            ChargingPool.PropertyKeys.EnergyMix,
            ChargingPool.PropertyKeys.Images,
            ChargingPool.PropertyKeys.TimeZone,
            ChargingPool.PropertyKeys.Facilities,
            ChargingPool.PropertyKeys.PaymentOptions,
            ChargingPool.PropertyKeys.AuthenticationModes,
            ChargingPool.PropertyKeys.UIFeatures,
            ChargingPool.PropertyKeys.Features,
            ChargingPool.PropertyKeys.LocationLanguages,
            ChargingPool.PropertyKeys.EntranceAddress,
            ChargingPool.PropertyKeys.EntranceLocation,
            ChargingPool.PropertyKeys.ExitAddress,
            ChargingPool.PropertyKeys.ExitLocation,
            ChargingPool.PropertyKeys.ArrivalInstructions,
            ChargingPool.PropertyKeys.ChargingWhenClosed,
            ChargingPool.PropertyKeys.ParkingRestrictions,
            ChargingPool.PropertyKeys.RelatedLocations,
            ChargingPool.PropertyKeys.Services,
            ChargingPool.PropertyKeys.AssistanceServiceDetails,
            ChargingPool.PropertyKeys.Standards,
            ChargingPool.PropertyKeys.NUTSCodes
        ];

        /// <summary>
        /// The properties of a charging tariff a charging tariff template can give its members:
        /// all but its identification, name, description, data source, custom data and templates.
        /// </summary>
        internal static readonly ImmutableArray<String> ChargingTariffTemplateTariffProperties = [
            ChargingTariff.PropertyKeys.Currency,
            ChargingTariff.PropertyKeys.TariffType,
            ChargingTariff.PropertyKeys.TariffURL,
            ChargingTariff.PropertyKeys.Elements,
            ChargingTariff.PropertyKeys.MinPrice,
            ChargingTariff.PropertyKeys.MaxPrice,
            ChargingTariff.PropertyKeys.PaymentMeans,
            ChargingTariff.PropertyKeys.PaymentBrands,
            ChargingTariff.PropertyKeys.ValidFrom,
            ChargingTariff.PropertyKeys.ValidUntil,
            ChargingTariff.PropertyKeys.EnergyMix
        ];

        /// <summary>
        /// The properties of a group a change set can update or remove, by the group type: its members, as it gives them
        /// nothing.
        /// </summary>
        private static readonly ImmutableDictionary<String, ImmutableHashSet<String>> ChangeableGroupProperties = new Dictionary<String, ImmutableHashSet<String>> {
            { nameof(EVSEGroup),            [ EVSEGroup.           BasePropertyKeys.Name, EVSEGroup.           BasePropertyKeys.Description, EVSEGroup.           BasePropertyKeys.DataSource,
                                              EVSEGroup.           PropertyKeys.EVSEIds,            EVSEGroup.           BasePropertyKeys.CustomData ] },
            { nameof(ChargingStationGroup), [ ChargingStationGroup.BasePropertyKeys.Name, ChargingStationGroup.BasePropertyKeys.Description, ChargingStationGroup.BasePropertyKeys.DataSource,
                                              ChargingStationGroup.PropertyKeys.ChargingStationIds, ChargingStationGroup.BasePropertyKeys.CustomData ] },
            { nameof(ChargingPoolGroup),    [ ChargingPoolGroup.   BasePropertyKeys.Name, ChargingPoolGroup.   BasePropertyKeys.Description, ChargingPoolGroup.   BasePropertyKeys.DataSource,
                                              ChargingPoolGroup.   PropertyKeys.ChargingPoolIds,    ChargingPoolGroup.   BasePropertyKeys.CustomData ] },
            { nameof(ChargingTariffGroup),  [ ChargingTariffGroup. BasePropertyKeys.Name, ChargingTariffGroup. BasePropertyKeys.Description, ChargingTariffGroup. BasePropertyKeys.DataSource,
                                              ChargingTariffGroup. PropertyKeys.ChargingTariffIds,  ChargingTariffGroup. BasePropertyKeys.CustomData ] }
        }.ToImmutableDictionary();

        /// <summary>
        /// The properties of a template a change set can update or remove, by the template type: those of its members it
        /// gives them, and of an EVSE, charging station or charging pool template the charging tariff of their EVSEs.
        /// </summary>
        private static readonly ImmutableDictionary<String, ImmutableHashSet<String>> ChangeableTemplateProperties = new Dictionary<String, ImmutableHashSet<String>> {
            { nameof(EVSETemplate),             [ EVSETemplate.           PropertyKeys.Name, EVSETemplate.           PropertyKeys.Description, EVSETemplate.           PropertyKeys.DataSource,
                                                  EVSETemplate.           PropertyKeys.ChargingTariffId, EVSETemplate.           PropertyKeys.CustomData,
                                                  .. EVSETemplateEVSEProperties ] },
            { nameof(ChargingStationTemplate),  [ ChargingStationTemplate.PropertyKeys.Name, ChargingStationTemplate.PropertyKeys.Description, ChargingStationTemplate.PropertyKeys.DataSource,
                                                  ChargingStationTemplate.PropertyKeys.ChargingTariffId, ChargingStationTemplate.PropertyKeys.CustomData,
                                                  .. ChargingStationTemplateStationProperties ] },
            { nameof(ChargingPoolTemplate),     [ ChargingPoolTemplate.   PropertyKeys.Name, ChargingPoolTemplate.   PropertyKeys.Description, ChargingPoolTemplate.   PropertyKeys.DataSource,
                                                  ChargingPoolTemplate.   PropertyKeys.ChargingTariffId, ChargingPoolTemplate.   PropertyKeys.CustomData,
                                                  .. ChargingPoolTemplatePoolProperties ] },
            { nameof(ChargingTariffTemplate),   [ ChargingTariffTemplate. PropertyKeys.Name, ChargingTariffTemplate. PropertyKeys.Description, ChargingTariffTemplate. PropertyKeys.DataSource,
                                                  ChargingTariffTemplate. PropertyKeys.CustomData,
                                                  .. ChargingTariffTemplateTariffProperties ] }
        }.ToImmutableDictionary();

        /// <summary>
        /// The properties of a charging tariff a change set can update, and remove unless they are required.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableChargingTariffProperties = [
            ChargingTariff.BasePropertyKeys.Name,
            ChargingTariff.BasePropertyKeys.Description,
            ChargingTariff.BasePropertyKeys.DataSource,
            ChargingTariff.PropertyKeys.Currency,
            ChargingTariff.PropertyKeys.TariffType,
            ChargingTariff.PropertyKeys.TariffURL,
            ChargingTariff.PropertyKeys.Elements,
            ChargingTariff.PropertyKeys.MinPrice,
            ChargingTariff.PropertyKeys.MaxPrice,
            ChargingTariff.PropertyKeys.PaymentMeans,
            ChargingTariff.PropertyKeys.PaymentBrands,
            ChargingTariff.PropertyKeys.ValidFrom,
            ChargingTariff.PropertyKeys.ValidUntil,
            ChargingTariff.PropertyKeys.EnergyMix,
            ChargingTariff.PropertyKeys.TemplateIds,
            ChargingTariff.BasePropertyKeys.CustomData
        ];

        #endregion


        #region (private static) IsInfrastructure(EntityType)

        /// <summary>
        /// Whether the given entity type is embedded in charging station operators.
        /// </summary>
        private static Boolean IsInfrastructure(String EntityType)

            => EntityType is nameof(ChargingPool)
                          or nameof(ChargingStation)
                          or nameof(EVSE)
                          or nameof(ChargingTariff)
                          or nameof(EVSETemplate)
                          or nameof(ChargingStationTemplate)
                          or nameof(ChargingPoolTemplate)
                          or nameof(ChargingTariffTemplate)
                          or nameof(EVSEGroup)
                          or nameof(ChargingStationGroup)
                          or nameof(ChargingPoolGroup)
                          or nameof(ChargingTariffGroup);

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
                nameof(ChargingTariff)   => ChargingTariff_Id. TryParse(entityId, out var chargingTariffId)  ? chargingTariffId. OperatorId : null,
                nameof(EVSEGroup)            => EVSEGroup_Id.           TryParse(entityId, out var evseGroupId)            ? evseGroupId.           OperatorId : null,
                nameof(ChargingStationGroup) => ChargingStationGroup_Id.TryParse(entityId, out var chargingStationGroupId) ? chargingStationGroupId.OperatorId : null,
                nameof(ChargingPoolGroup)    => ChargingPoolGroup_Id.   TryParse(entityId, out var chargingPoolGroupId)    ? chargingPoolGroupId.   OperatorId : null,
                nameof(ChargingTariffGroup)  => ChargingTariffGroup_Id. TryParse(entityId, out var chargingTariffGroupId)  ? chargingTariffGroupId. OperatorId : null,
                nameof(EVSETemplate)            => EVSETemplate_Id.           TryParse(entityId, out var evseTemplateId)            ? evseTemplateId.           OperatorId : null,
                nameof(ChargingStationTemplate) => ChargingStationTemplate_Id.TryParse(entityId, out var chargingStationTemplateId) ? chargingStationTemplateId.OperatorId : null,
                nameof(ChargingPoolTemplate)    => ChargingPoolTemplate_Id.   TryParse(entityId, out var chargingPoolTemplateId)    ? chargingPoolTemplateId.   OperatorId : null,
                nameof(ChargingTariffTemplate)  => ChargingTariffTemplate_Id. TryParse(entityId, out var chargingTariffTemplateId)  ? chargingTariffTemplateId. OperatorId : null,
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

            // A change may name an entity in every spelling its identification type considers equal,
            // e.g. without the '*' of its operator: it is found under the identification it is kept with.
            var (entityJSON, container, parentType, parentId) = Find(operatorJSON,
                                                                     Change.EntityType,
                                                                     StoredId(currentOperator, Change.EntityType, entityId) ?? entityId);

            // The actual parent, of an existing entity.
            if (entityJSON is not null &&
                Change.ParentEntityType is not null &&
               (Change.ParentEntityType != parentType || StoredId(currentOperator, Change.ParentEntityType, Change.ParentEntityId) != parentId))
            {
                ErrorResponse = $"Change {Index}: the {description} '{entityId}' belongs to {parentType} '{parentId}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                return false;
            }

            #endregion

            // An element change is applied as the change of the property it amounts to.
            var isElementChange = Change.IsElementChange;

            if (!TryResolveElementChange(ref Change, entityJSON, description, out var elementValue, out ErrorResponse))
            {
                ErrorResponse = $"Change {Index}: {ErrorResponse}";
                return false;
            }

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

                    var changeable   = Change.EntityType switch {
                                           nameof(ChargingPool)     => ChangeableChargingPoolProperties,
                                           nameof(ChargingStation)  => ChangeableChargingStationProperties,
                                           nameof(ChargingTariff)   => ChangeableChargingTariffProperties,
                                           _ when ChangeableGroupProperties.TryGetValue(Change.EntityType, out var groupProperties)
                                                                    => groupProperties,
                                           _ when ChangeableTemplateProperties.TryGetValue(Change.EntityType, out var templateProperties)
                                                                    => templateProperties,
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
                    entityJSON[BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();
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
                        newEntity[BasePropertyKeys.Id]?.Type != JTokenType.String ||
                        newEntity[BasePropertyKeys.Id]!.Value<String>() != entityId)
                    {
                        ErrorResponse = $"Change {Index}: the {description} to add must be a JSON object with the identification '{entityId}'!";
                        return false;
                    }

                    if (!TryFindParentArray(operatorJSON, Change, currentOperator, out var children, out ErrorResponse))
                    {
                        ErrorResponse = $"Change {Index}: {ErrorResponse}";
                        return false;
                    }

                    // The parent it is added to: its operator, charging pool or charging station.
                    parentType  = Change.ParentEntityType ?? nameof(ChargingStationOperator);
                    parentId    = StoredId(currentOperator, parentType, Change.ParentEntityId ?? currentOperator.Id.ToString());

                    // The change set's timestamp is the last change and, unless given, the creation,
                    // so that its after ETags are deterministic.
                    var added = (JObject) newEntity.DeepClone();
                    added[BasePropertyKeys.Created]    ??= CreatedAt.ToISO8601();
                    added[BasePropertyKeys.LastChange]   = CreatedAt.ToISO8601();

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

            // What embeds it changed with it, at the time of the change set.
            TouchParents(operatorJSON, parentType, parentId, CreatedAt);

            #region The changed operator must still be a valid one

            if (!ChargingStationOperator.TryParseWithinChangeSet(operatorJSON, out var changedOperator, out var changedOperatorError))
            {
                ErrorResponse = $"Change {Index}: {changedOperatorError}";
                return false;
            }

            // Admin status, status and internal data are not content: they go along, at every level.
            changedOperator.ShareRuntimeOfAll(currentOperator);

            Operators[operatorId.Value] = changedOperator;

            #endregion

            // The old value of an element change is the value of what it addresses.
            if (isElementChange)
                CurrentValue = elementValue;

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) TouchParents(OperatorJSON, ParentType, ParentId, CreatedAt)

        /// <summary>
        /// Set the last change of what embeds a changed entity, up to its operator: the charging station of an EVSE,
        /// the charging pool of a charging station, and the operator of everything, as a change of an embedded entity
        /// is a change of the content of everything embedding it.
        /// </summary>
        private static void TouchParents(JObject         OperatorJSON,
                                         String?         ParentType,
                                         String?         ParentId,
                                         DateTimeOffset  CreatedAt)
        {

            var lastChange = CreatedAt.ToISO8601();

            while (ParentType is nameof(ChargingStation) or nameof(ChargingPool) && ParentId is not null)
            {

                var (parent, _, grandParentType, grandParentId) = Find(OperatorJSON, ParentType, ParentId);

                if (parent is null)
                    break;

                parent[BasePropertyKeys.LastChange]  = lastChange;
                ParentType            = grandParentType;
                ParentId              = grandParentId;

            }

            OperatorJSON[ChargingStationOperator.BasePropertyKeys.LastChange] = lastChange;

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

            var operatorId = OperatorJSON[ChargingStationOperator.BasePropertyKeys.Id]?.Value<String>();

            // Charging tariffs, templates and groups are children of the operator itself.
            if (OperatorChildArrays.TryGetValue(EntityType, out var childArray))
            {

                if (OperatorJSON[childArray] is JArray operatorChildren)
                    foreach (var operatorChild in operatorChildren.OfType<JObject>())
                        if (operatorChild[BasePropertyKeys.Id]?.Value<String>() == EntityId)
                            return (operatorChild, operatorChildren, nameof(ChargingStationOperator), operatorId);

                return (null, null, null, null);

            }

            if (OperatorJSON[ChargingStationOperator.PropertyKeys.ChargingPools] is not JArray chargingPools)
                return (null, null, null, null);

            foreach (var chargingPool in chargingPools.OfType<JObject>())
            {

                var chargingPoolId = chargingPool[ChargingPool.BasePropertyKeys.Id]?.Value<String>();

                if (EntityType == nameof(ChargingPool))
                {
                    if (chargingPoolId == EntityId)
                        return (chargingPool, chargingPools, nameof(ChargingStationOperator), operatorId);
                    continue;
                }

                if (chargingPool[ChargingPool.PropertyKeys.ChargingStations] is not JArray chargingStations)
                    continue;

                foreach (var chargingStation in chargingStations.OfType<JObject>())
                {

                    var chargingStationId = chargingStation[ChargingStation.BasePropertyKeys.Id]?.Value<String>();

                    if (EntityType == nameof(ChargingStation))
                    {
                        if (chargingStationId == EntityId)
                            return (chargingStation, chargingStations, nameof(ChargingPool), chargingPoolId);
                        continue;
                    }

                    if (chargingStation[ChargingStation.PropertyKeys.EVSEs] is not JArray evses)
                        continue;

                    foreach (var evse in evses.OfType<JObject>())
                        if (evse[EVSE.BasePropertyKeys.Id]?.Value<String>() == EntityId)
                            return (evse, evses, nameof(ChargingStation), chargingStationId);

                }

            }

            return (null, null, null, null);

        }

        #endregion

        #region (private static) TryFindParentArray(OperatorJSON, Change, Operator, out Children, out ErrorResponse)

        /// <summary>
        /// The array a new entity is added to: of the operator for a charging pool,
        /// of the named charging pool for a charging station, of the named charging station for an EVSE.
        /// The parent may be named in every spelling its identification type considers equal.
        /// </summary>
        private static Boolean TryFindParentArray(JObject                                OperatorJSON,
                                                  RoamingNetworkChange                   Change,
                                                  ChargingStationOperator                Operator,
                                                  [NotNullWhen(true)]  out JArray?       Children,
                                                  [NotNullWhen(false)] out String?       ErrorResponse)
        {

            Children = null;

            JObject  parent;
            String   childrenName;

            switch (Change.EntityType)
            {

                case nameof(ChargingPool):
                case nameof(ChargingTariff):
                case nameof(EVSEGroup):
                case nameof(ChargingStationGroup):
                case nameof(ChargingPoolGroup):
                case nameof(ChargingTariffGroup):
                case nameof(EVSETemplate):
                case nameof(ChargingStationTemplate):
                case nameof(ChargingPoolTemplate):
                case nameof(ChargingTariffTemplate):

                    if (Change.ParentEntityType is not null &&
                       (Change.ParentEntityType != nameof(ChargingStationOperator) || StoredId(Operator, Change.ParentEntityType, Change.ParentEntityId) is null))
                    {
                        ErrorResponse = $"a {Describe(Change.EntityType)} '{Change.EntityId}' belongs to ChargingStationOperator '{Operator.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                        return false;
                    }

                    parent        = OperatorJSON;
                    childrenName  = Change.EntityType == nameof(ChargingPool) ? ChargingStationOperator.PropertyKeys.ChargingPools : OperatorChildArrays[Change.EntityType];
                    break;

                case nameof(ChargingStation):

                    if (Change.ParentEntityType != nameof(ChargingPool) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the charging station '{Change.EntityId}' needs its ChargingPool as its parent!";
                        return false;
                    }

                    if (Find(OperatorJSON, nameof(ChargingPool), StoredId(Operator, nameof(ChargingPool), Change.ParentEntityId) ?? Change.ParentEntityId).Entity is not JObject chargingPool)
                    {
                        ErrorResponse = $"the charging pool '{Change.ParentEntityId}' of the charging station '{Change.EntityId}' does not exist!";
                        return false;
                    }

                    parent        = chargingPool;
                    childrenName  = ChargingPool.PropertyKeys.ChargingStations;
                    break;

                default:

                    if (Change.ParentEntityType != nameof(ChargingStation) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the EVSE '{Change.EntityId}' needs its ChargingStation as its parent!";
                        return false;
                    }

                    if (Find(OperatorJSON, nameof(ChargingStation), StoredId(Operator, nameof(ChargingStation), Change.ParentEntityId) ?? Change.ParentEntityId).Entity is not JObject chargingStation)
                    {
                        ErrorResponse = $"the charging station '{Change.ParentEntityId}' of the EVSE '{Change.EntityId}' does not exist!";
                        return false;
                    }

                    parent        = chargingStation;
                    childrenName  = ChargingStation.PropertyKeys.EVSEs;
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

        #region (private static) StoredId(Operator, EntityType, EntityId)

        /// <summary>
        /// The identification the given operator keeps the named entity with, as its JSON writes it:
        /// the operator itself, or one of its charging pools, charging stations, EVSEs, charging tariffs or groups.
        /// Identifications are equal as their types compare them, e.g. 'DEGEF*S1' and 'DE*GEF*S1',
        /// not as texts. Null for an invalid identification or an entity the operator does not have.
        /// </summary>
        private static String? StoredId(ChargingStationOperator  Operator,
                                        String?                  EntityType,
                                        String?                  EntityId)

            => EntityId is null
                   ? null
                   : EntityType switch {
                         nameof(ChargingStationOperator) => ChargingStationOperator_Id.TryParse(EntityId, out var operatorId)             && operatorId == Operator.Id                                                    ? Operator.Id.ToString()             : null,
                         nameof(ChargingPool)            => ChargingPool_Id.           TryParse(EntityId, out var chargingPoolId)         && Operator.TryGetChargingPool        (chargingPoolId,         out var chargingPool)         ? chargingPool.        Id.ToString() : null,
                         nameof(ChargingStation)         => ChargingStation_Id.        TryParse(EntityId, out var chargingStationId)      && Operator.TryGetChargingStation     (chargingStationId,      out var chargingStation)      ? chargingStation.     Id.ToString() : null,
                         nameof(EVSE)                    => EVSE_Id.                   TryParse(EntityId, out var evseId)                 && Operator.TryGetEVSE                (evseId,                 out var evse)                 ? evse.                Id.ToString() : null,
                         nameof(ChargingTariff)          => ChargingTariff_Id.         TryParse(EntityId, out var chargingTariffId)       && Operator.TryGetChargingTariff      (chargingTariffId,       out var chargingTariff)       ? chargingTariff.      Id.ToString() : null,
                         nameof(EVSEGroup)               => EVSEGroup_Id.              TryParse(EntityId, out var evseGroupId)            && Operator.TryGetEVSEGroup           (evseGroupId,            out var evseGroup)            ? evseGroup.           Id.ToString() : null,
                         nameof(ChargingStationGroup)    => ChargingStationGroup_Id.   TryParse(EntityId, out var chargingStationGroupId) && Operator.TryGetChargingStationGroup(chargingStationGroupId, out var chargingStationGroup) ? chargingStationGroup.Id.ToString() : null,
                         nameof(ChargingPoolGroup)       => ChargingPoolGroup_Id.      TryParse(EntityId, out var chargingPoolGroupId)    && Operator.TryGetChargingPoolGroup   (chargingPoolGroupId,    out var chargingPoolGroup)    ? chargingPoolGroup.   Id.ToString() : null,
                         nameof(ChargingTariffGroup)     => ChargingTariffGroup_Id.    TryParse(EntityId, out var chargingTariffGroupId)  && Operator.TryGetChargingTariffGroup (chargingTariffGroupId,  out var chargingTariffGroup)  ? chargingTariffGroup. Id.ToString() : null,
                         nameof(EVSETemplate)            => EVSETemplate_Id.           TryParse(EntityId, out var evseTemplateId)            && Operator.TryGetEVSETemplate           (evseTemplateId,            out var evseTemplate)            ? evseTemplate.           Id.ToString() : null,
                         nameof(ChargingStationTemplate) => ChargingStationTemplate_Id.TryParse(EntityId, out var chargingStationTemplateId) && Operator.TryGetChargingStationTemplate(chargingStationTemplateId, out var chargingStationTemplate) ? chargingStationTemplate.Id.ToString() : null,
                         nameof(ChargingPoolTemplate)    => ChargingPoolTemplate_Id.   TryParse(EntityId, out var chargingPoolTemplateId)    && Operator.TryGetChargingPoolTemplate   (chargingPoolTemplateId,    out var chargingPoolTemplate)    ? chargingPoolTemplate.   Id.ToString() : null,
                         nameof(ChargingTariffTemplate)  => ChargingTariffTemplate_Id. TryParse(EntityId, out var chargingTariffTemplateId)  && Operator.TryGetChargingTariffTemplate (chargingTariffTemplateId,  out var chargingTariffTemplate)  ? chargingTariffTemplate. Id.ToString() : null,
                         _                               => null
                     };

        #endregion

        #region (private static) Describe(EntityType)

        private static String Describe(String EntityType)

            => EntityType switch {
                   nameof(ChargingPool)     => "charging pool",
                   nameof(ChargingStation)  => "charging station",
                   nameof(ChargingTariff)   => "charging tariff",
                   nameof(EVSEGroup)            => "EVSE group",
                   nameof(ChargingStationGroup) => "charging station group",
                   nameof(ChargingPoolGroup)    => "charging pool group",
                   nameof(ChargingTariffGroup)  => "charging tariff group",
                   nameof(EVSETemplate)             => "EVSE template",
                   nameof(ChargingStationTemplate)  => "charging station template",
                   nameof(ChargingPoolTemplate)     => "charging pool template",
                   nameof(ChargingTariffTemplate)   => "charging tariff template",
                   _                        => "EVSE"
               };

        #endregion

    }

}
