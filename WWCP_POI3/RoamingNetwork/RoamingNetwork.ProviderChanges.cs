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
    /// Changes of the groups of e-mobility providers.
    /// </summary>
    /// <remarks>
    /// An e-mobility provider embeds its groups of EVSEs, charging stations and charging pools completely: a change edits
    /// its embedded JSON and reads it again, so that the changed provider is valid as a whole; its admin status, status and
    /// internal data, and those of its groups, go along. The identifications of groups are unique within the network, so an
    /// existing group of a provider is found without its parent; adding one names its provider, as a group of a charging
    /// station operator may have the same prefix. A given parent of any other change must be the actual one.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region (private static) ProviderGroupKind(EntityType)

        /// <summary>
        /// The JSON key of the groups of the given entity type in an e-mobility provider, and what one is in words;
        /// null for any other entity type.
        /// </summary>
        private static (String Property, String Description)? ProviderGroupKind(String EntityType)

            => EntityType switch {
                   nameof(EVSEGroup)             => (EMobilityProvider.PropertyKeys.EVSEGroups,            "EVSE group"),
                   nameof(ChargingStationGroup)  => (EMobilityProvider.PropertyKeys.ChargingStationGroups, "charging station group"),
                   nameof(ChargingPoolGroup)     => (EMobilityProvider.PropertyKeys.ChargingPoolGroups,    "charging pool group"),
                   _                             => null
               };

        #endregion

        #region (private static) ProviderGroupStoredId(EMobilityProvider, EntityType, EntityId)

        /// <summary>
        /// The identification the given e-mobility provider keeps its group of the given entity type and identification
        /// with; null when it has no such group.
        /// </summary>
        private static String? ProviderGroupStoredId(EMobilityProvider  EMobilityProvider,
                                                     String             EntityType,
                                                     String             EntityId)

            => EntityType switch {
                   nameof(EVSEGroup)             => EVSEGroup_Id.           TryParse(EntityId, out var evseGroupId)    && EMobilityProvider.TryGetEVSEGroup           (evseGroupId,    out var evseGroup)    ? evseGroup.   Id.ToString() : null,
                   nameof(ChargingStationGroup)  => ChargingStationGroup_Id.TryParse(EntityId, out var stationGroupId) && EMobilityProvider.TryGetChargingStationGroup(stationGroupId, out var stationGroup) ? stationGroup.Id.ToString() : null,
                   nameof(ChargingPoolGroup)     => ChargingPoolGroup_Id.   TryParse(EntityId, out var poolGroupId)    && EMobilityProvider.TryGetChargingPoolGroup   (poolGroupId,    out var poolGroup)    ? poolGroup.   Id.ToString() : null,
                   _                             => null
               };

        #endregion

        #region (private static) ProviderOfGroup(EMobilityProviders, EntityType, EntityId)

        /// <summary>
        /// The e-mobility provider having the group of the given entity type and identification, with the identification it
        /// keeps it with; nulls when no provider has it.
        /// </summary>
        private static (EMobilityProvider? EMobilityProvider, String? StoredId) ProviderOfGroup(IEnumerable<EMobilityProvider>  EMobilityProviders,
                                                                                               String                          EntityType,
                                                                                               String                          EntityId)
        {

            foreach (var eMobilityProvider in EMobilityProviders)
                if (ProviderGroupStoredId(eMobilityProvider, EntityType, EntityId) is String storedId)
                    return (eMobilityProvider, storedId);

            return (null, null);

        }

        #endregion

        #region (private static) IsProviderGroupChange(Change, EMobilityProviders)

        /// <summary>
        /// Whether the given change is one of a group of an e-mobility provider: adding a group to a provider, or changing
        /// or removing a group a provider has.
        /// </summary>
        private static Boolean IsProviderGroupChange(RoamingNetworkChange            Change,
                                                     IEnumerable<EMobilityProvider>  EMobilityProviders)

            => ProviderGroupKind(Change.EntityType) is not null &&
               (Change.Kind == RoamingNetworkChangeKind.Add
                    ? Change.ParentEntityType == nameof(EMobilityProvider)
                    : ProviderOfGroup(EMobilityProviders, Change.EntityType, Change.EntityId).EMobilityProvider is not null);

        #endregion

        #region (private static) TryApplyProviderGroupChange(Change, Index, CreatedAt, EMobilityProviders, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a group of an e-mobility provider to the given e-mobility providers.
        /// </summary>
        private static Boolean TryApplyProviderGroupChange(RoamingNetworkChange                                 Change,
                                                           Int32                                                Index,
                                                           DateTimeOffset                                       CreatedAt,
                                                           Dictionary<EMobilityProvider_Id, EMobilityProvider>  EMobilityProviders,
                                                           out JToken?                                          CurrentValue,
                                                           [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            CurrentValue = null;

            var (property, description)  = ProviderGroupKind(Change.EntityType)!.Value;
            var entityId                 = Change.EntityId;

            #region Its provider: the one having it, or the parent of a new one

            var (eMobilityProvider, storedId) = ProviderOfGroup(EMobilityProviders.Values, Change.EntityType, entityId);

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {

                if (storedId is not null)
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' already exists!";
                    return false;
                }

                if (!EMobilityProvider_Id.TryParse(Change.ParentEntityId ?? "", out var providerId) ||
                    !EMobilityProviders.TryGetValue(providerId, out eMobilityProvider))
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' to add needs its EMobilityProvider, an existing one!";
                    return false;
                }

            }

            else if (Change.ParentEntityType is not null &&
                     !(Change.ParentEntityType == nameof(EMobilityProvider) &&
                       EMobilityProvider_Id.TryParse(Change.ParentEntityId ?? "", out var namedProviderId) &&
                       namedProviderId == eMobilityProvider!.Id))
            {
                ErrorResponse = $"Change {Index}: the {description} '{entityId}' belongs to EMobilityProvider '{eMobilityProvider!.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                return false;
            }

            var ownerJSON   = eMobilityProvider!.ToJSON(Embedded: true);
            var children    = ownerJSON[property] as JArray;
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

            // A property change compares the property, every other change the complete group.
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

                    if (!ChangeableGroupProperties[Change.EntityType].Contains(propertyName))
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
                        ownerJSON[property] = children;
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
                        ownerJSON.Remove(property);

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a {description} is not supported yet!";
                    return false;

            }

            // The provider embedding it changed with it, at the time of the change set.
            ownerJSON[EMobilityProvider.BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();

            #region The changed provider must still be a valid one

            if (!EMobilityProvider.TryParse(ownerJSON, out var changedProvider, out var changedProviderError))
            {
                ErrorResponse = $"Change {Index}: {changedProviderError}";
                return false;
            }

            // Admin status, status and internal data are not content: they go along, of its groups as well.
            changedProvider.ShareRuntimeOfAll(eMobilityProvider);

            EMobilityProviders[eMobilityProvider.Id] = changedProvider;

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
