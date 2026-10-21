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
    /// Changes of the energy meter templates and groups of a roaming network.
    /// </summary>
    /// <remarks>
    /// They are embedded completely in their owner, a charging station operator or a grid operator: a change edits the
    /// embedded JSON of the owner and reads it again, so that the changed owner is valid as a whole. Its admin status,
    /// status and internal data, and those of everything it embeds, go along. Their identifications are unique within
    /// the network, so an existing one is found without its parent; adding one names its owner. A given parent of any
    /// other change must be the actual one.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The properties of an energy meter an energy meter template can give its members: all but its identification,
        /// name, description, data source, custom data, templates, and what belongs to the single device, its serial number
        /// and its public keys.
        /// </summary>
        internal static readonly ImmutableArray<String> EnergyMeterTemplateMeterProperties = [
            EnergyMeter.PropertyKeys.Role,
            EnergyMeter.PropertyKeys.ManufacturerId,
            EnergyMeter.PropertyKeys.ModelId,
            EnergyMeter.PropertyKeys.ModelURL,
            EnergyMeter.PropertyKeys.HardwareVersion,
            EnergyMeter.PropertyKeys.FirmwareVersion,
            EnergyMeter.PropertyKeys.TransparencySoftwareStatus,
            EnergyMeter.PropertyKeys.SignatureMethods
        ];

        /// <summary>
        /// The properties of an energy meter group a change set can update or remove: its members, as it gives them nothing.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableEnergyMeterGroupProperties = [
            EnergyMeterGroup.BasePropertyKeys.Name,
            EnergyMeterGroup.BasePropertyKeys.Description,
            EnergyMeterGroup.BasePropertyKeys.DataSource,
            EnergyMeterGroup.PropertyKeys.EnergyMeterIds,
            EnergyMeterGroup.BasePropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of an energy meter template a change set can update or remove: those of an energy meter it gives
        /// its members.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableEnergyMeterTemplateProperties = [
            EnergyMeterTemplate.PropertyKeys.Name,
            EnergyMeterTemplate.PropertyKeys.Description,
            EnergyMeterTemplate.PropertyKeys.DataSource,
            EnergyMeterTemplate.PropertyKeys.CustomData,
            .. EnergyMeterTemplateMeterProperties
        ];

        /// <summary>
        /// The JSON key of the energy meter groups of their owners.
        /// </summary>
        private const String EnergyMeterGroupsProperty     = ChargingStationOperator.PropertyKeys.EnergyMeterGroups;

        /// <summary>
        /// The JSON key of the energy meter templates of their owners.
        /// </summary>
        private const String EnergyMeterTemplatesProperty  = ChargingStationOperator.PropertyKeys.EnergyMeterTemplates;

        /// <summary>
        /// What a change of an energy meter template or group needs to know of its kind.
        /// </summary>
        private sealed record EnergyMeterChildKind(String                                     Description,
                                                   String                                     Article,
                                                   String                                     Property,
                                                   ImmutableHashSet<String>                   Changeable,
                                                   Func<String, Boolean>                      IsValidId,
                                                   Func<ChargingStationOperator, String, String?>  StoredInOperator,
                                                   Func<GridOperator,            String, String?>  StoredInGridOperator);

        private static readonly EnergyMeterChildKind EnergyMeterGroupKind = new (
            "energy meter group",
            "an",
            EnergyMeterGroupsProperty,
            ChangeableEnergyMeterGroupProperties,
            id => EnergyMeterGroup_Id.TryParse(id, out _),
            (chargingStationOperator, id) => EnergyMeterGroup_Id.TryParse(id, out var groupId) && chargingStationOperator.TryGetEnergyMeterGroup(groupId, out var group) ? group.Id.ToString() : null,
            (gridOperator,            id) => EnergyMeterGroup_Id.TryParse(id, out var groupId) && gridOperator.           TryGetEnergyMeterGroup(groupId, out var group) ? group.Id.ToString() : null);

        private static readonly EnergyMeterChildKind EnergyMeterTemplateKind = new (
            "energy meter template",
            "an",
            EnergyMeterTemplatesProperty,
            ChangeableEnergyMeterTemplateProperties,
            id => EnergyMeterTemplate_Id.TryParse(id, out _),
            (chargingStationOperator, id) => EnergyMeterTemplate_Id.TryParse(id, out var templateId) && chargingStationOperator.TryGetEnergyMeterTemplate(templateId, out var template) ? template.Id.ToString() : null,
            (gridOperator,            id) => EnergyMeterTemplate_Id.TryParse(id, out var templateId) && gridOperator.           TryGetEnergyMeterTemplate(templateId, out var template) ? template.Id.ToString() : null);

        #endregion


        #region (private static) EnergyMeterGroupOwners(EnergyMeterGroupId, ChargingStationOperators, GridOperators)

        /// <summary>
        /// The charging station operator or the grid operator having the energy meter group with the given identification,
        /// with the identification it keeps it with; nulls for one nobody has.
        /// </summary>
        private static (ChargingStationOperator? ChargingStationOperator, GridOperator? GridOperator, String? StoredId) EnergyMeterGroupOwners(EnergyMeterGroup_Id                   EnergyMeterGroupId,
                                                                                                                                             IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                                                                                                             IEnumerable<GridOperator>             GridOperators)

            => EnergyMeterChildOwners(EnergyMeterGroupKind, EnergyMeterGroupId.ToString(), ChargingStationOperators, GridOperators);

        /// <summary>
        /// The charging station operator or the grid operator having the energy meter template with the given
        /// identification, with the identification it keeps it with; nulls for one nobody has.
        /// </summary>
        private static (ChargingStationOperator? ChargingStationOperator, GridOperator? GridOperator, String? StoredId) EnergyMeterTemplateOwners(EnergyMeterTemplate_Id                EnergyMeterTemplateId,
                                                                                                                                                IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                                                                                                                IEnumerable<GridOperator>             GridOperators)

            => EnergyMeterChildOwners(EnergyMeterTemplateKind, EnergyMeterTemplateId.ToString(), ChargingStationOperators, GridOperators);

        private static (ChargingStationOperator? ChargingStationOperator, GridOperator? GridOperator, String? StoredId) EnergyMeterChildOwners(EnergyMeterChildKind                  Kind,
                                                                                                                                             String                                EntityId,
                                                                                                                                             IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                                                                                                             IEnumerable<GridOperator>             GridOperators)
        {

            foreach (var chargingStationOperator in ChargingStationOperators)
                if (Kind.StoredInOperator(chargingStationOperator, EntityId) is String storedId)
                    return (chargingStationOperator, null, storedId);

            foreach (var gridOperator in GridOperators)
                if (Kind.StoredInGridOperator(gridOperator, EntityId) is String storedId)
                    return (null, gridOperator, storedId);

            return (null, null, null);

        }

        #endregion

        #region (private static) TryApplyEnergyMeterGroupChange   (Change, Index, CreatedAt, ChargingStationOperators, GridOperators, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of an energy meter group to the given charging station operators or grid operators.
        /// </summary>
        private static Boolean TryApplyEnergyMeterGroupChange(RoamingNetworkChange                                             Change,
                                                              Int32                                                            Index,
                                                              DateTimeOffset                                                   CreatedAt,
                                                              Dictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                              Dictionary<GridOperator_Id, GridOperator>                        GridOperators,
                                                              out JToken?                                                      CurrentValue,
                                                              [NotNullWhen(false)] out String?                                 ErrorResponse)

            => TryApplyEnergyMeterChildChange(EnergyMeterGroupKind, Change, Index, CreatedAt, ChargingStationOperators, GridOperators, out CurrentValue, out ErrorResponse);

        #endregion

        #region (private static) TryApplyEnergyMeterTemplateChange(Change, Index, CreatedAt, ChargingStationOperators, GridOperators, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of an energy meter template to the given charging station operators or grid operators.
        /// </summary>
        private static Boolean TryApplyEnergyMeterTemplateChange(RoamingNetworkChange                                             Change,
                                                                 Int32                                                            Index,
                                                                 DateTimeOffset                                                   CreatedAt,
                                                                 Dictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                                 Dictionary<GridOperator_Id, GridOperator>                        GridOperators,
                                                                 out JToken?                                                      CurrentValue,
                                                                 [NotNullWhen(false)] out String?                                 ErrorResponse)

            => TryApplyEnergyMeterChildChange(EnergyMeterTemplateKind, Change, Index, CreatedAt, ChargingStationOperators, GridOperators, out CurrentValue, out ErrorResponse);

        #endregion

        #region (private static) TryApplyEnergyMeterChildChange   (Kind, Change, Index, CreatedAt, ChargingStationOperators, GridOperators, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of an energy meter template or group to the given charging station operators or grid operators.
        /// </summary>
        private static Boolean TryApplyEnergyMeterChildChange(EnergyMeterChildKind                                             Kind,
                                                              RoamingNetworkChange                                             Change,
                                                              Int32                                                            Index,
                                                              DateTimeOffset                                                   CreatedAt,
                                                              Dictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                              Dictionary<GridOperator_Id, GridOperator>                        GridOperators,
                                                              out JToken?                                                      CurrentValue,
                                                              [NotNullWhen(false)] out String?                                 ErrorResponse)
        {

            CurrentValue = null;

            var description = Kind.Description;
            var entityId    = Change.EntityId;

            #region Its owner: the one having it, or the parent of a new one

            if (!Kind.IsValidId(entityId))
            {
                ErrorResponse = $"Change {Index}: invalid {description} identification '{entityId}'!";
                return false;
            }

            var (chargingStationOperator, gridOperator, storedId) = EnergyMeterChildOwners(Kind, entityId, ChargingStationOperators.Values, GridOperators.Values);

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {

                if (storedId is not null)
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' already exists!";
                    return false;
                }

                if (Change.ParentEntityType == nameof(ChargingStationOperator))
                {
                    if (!ChargingStationOperator_Id.TryParse(Change.ParentEntityId ?? "", out var operatorId) ||
                        !ChargingStationOperators.TryGetValue(operatorId, out chargingStationOperator))
                    {
                        ErrorResponse = $"Change {Index}: the {description} '{entityId}' to add needs its ChargingStationOperator, an existing one!";
                        return false;
                    }
                }

                else if (Change.ParentEntityType != nameof(GridOperator) ||
                         !GridOperator_Id.TryParse(Change.ParentEntityId ?? "", out var gridOperatorId) ||
                         !GridOperators.TryGetValue(gridOperatorId, out gridOperator))
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' to add needs its ChargingStationOperator or GridOperator, an existing one!";
                    return false;
                }

            }

            else
            {

                if (storedId is null)
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' does not exist!";
                    return false;
                }

                if (Change.ParentEntityType is not null)
                {

                    var isActualParent = chargingStationOperator is not null
                                             ? Change.ParentEntityType == nameof(ChargingStationOperator) &&
                                               ChargingStationOperator_Id.TryParse(Change.ParentEntityId ?? "", out var namedOperatorId) &&
                                               namedOperatorId == chargingStationOperator.Id
                                             : Change.ParentEntityType == nameof(GridOperator) &&
                                               GridOperator_Id.TryParse(Change.ParentEntityId ?? "", out var namedGridOperatorId) &&
                                               namedGridOperatorId == gridOperator!.Id;

                    if (!isActualParent)
                    {
                        ErrorResponse = chargingStationOperator is not null
                                            ? $"Change {Index}: the {description} '{entityId}' belongs to ChargingStationOperator '{chargingStationOperator.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!"
                                            : $"Change {Index}: the {description} '{entityId}' belongs to GridOperator '{gridOperator!.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                        return false;
                    }

                }

            }

            // Of a charging station operator none of its charging pools are written and read again: they are kept as they
            // are (SPECIFICATION.md, section 8.7).
            var ownerJSON   = chargingStationOperator is not null
                                  ? chargingStationOperator.ToEmbeddedJSON([])
                                  : gridOperator!.          ToJSON(Embedded: true);
            var children    = ownerJSON[Kind.Property] as JArray;
            // Found under the identification it is kept with: the change may name it in another spelling.
            var entityJSON  = Change.Kind == RoamingNetworkChangeKind.Add
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

            // A property change compares the property, every other change the complete energy meter template or group.
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

                    if (!Kind.Changeable.Contains(propertyName))
                    {
                        ErrorResponse = $"Change {Index}: the property '{propertyName}' of {Kind.Article} {description} can not be changed!";
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
                        ownerJSON[Kind.Property] = children;
                    }

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

                    children!.Remove(entityJSON!);

                    if (children.Count == 0)
                        ownerJSON.Remove(Kind.Property);

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of {Kind.Article} {description} is not supported yet!";
                    return false;

            }

            // The owner embedding it changed with it, at the time of the change set.
            ownerJSON[BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();

            #region The changed owner must still be a valid one

            if (chargingStationOperator is not null)
            {

                if (!ChargingStationOperator.TryParseWithinChangeSet(ownerJSON, out var changedOperator, out var changedOperatorError) ||
                    !TryKeepChargingPools(changedOperator, chargingStationOperator, [], out changedOperator, out changedOperatorError))
                {
                    ErrorResponse = $"Change {Index}: {changedOperatorError}";
                    return false;
                }

                // Admin status, status and internal data are not content: they go along, at every level.
                changedOperator.ShareRuntimeOfAll(chargingStationOperator);

                ChargingStationOperators[chargingStationOperator.Id] = changedOperator;

            }

            else
            {

                if (!GridOperator.TryParse(ownerJSON, out var changedOperator, out var changedOperatorError))
                {
                    ErrorResponse = $"Change {Index}: {changedOperatorError}";
                    return false;
                }

                changedOperator.ShareRuntimeOfAll(gridOperator!);

                GridOperators[gridOperator!.Id] = changedOperator;

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
