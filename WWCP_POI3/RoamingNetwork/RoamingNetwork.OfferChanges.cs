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
    /// Changes of the charging tariff offers of a roaming network.
    /// </summary>
    /// <remarks>
    /// They are embedded completely in their owner, a charging station operator or an e-mobility provider: a change edits
    /// the embedded JSON of the owner and reads it again, so that the changed owner is valid as a whole, e.g. its offers do
    /// not overlap. Its admin status, status and internal data, and those of everything it embeds, go along. Their
    /// identifications are unique within the network, so an existing one is found without its parent; adding one names
    /// its owner. A given parent of any other change must be the actual one.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The JSON key of the charging tariff offers in their owner.
        /// </summary>
        internal const String ChargingTariffOffersProperty = "chargingTariffOffers";

        /// <summary>
        /// The properties of a charging tariff offer a change set can update or remove: all but its identification.
        /// Switching between a price of its own and a discount replaces the offer, as it has exactly one of them.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableChargingTariffOfferProperties = [
            "name",
            "description",
            "dataSource",
            "chargingTariff",
            "discount",
            "EVSEGroupIds",
            "chargingStationGroupIds",
            "chargingPoolGroupIds",
            "chargingStationOperatorIds",
            "notBefore",
            "notAfter",
            "openingTimes",
            "sessions",
            "eMobilityProviderIds",
            "maxSessionsPerContract",
            "customData"
        ];

        #endregion


        #region (private static) ChargingTariffOfferOwners(EntityId, ChargingStationOperators, EMobilityProviders)

        /// <summary>
        /// The charging station operator or the e-mobility provider having the charging tariff offer with the given
        /// identification, with the identification it keeps it with; nulls for one nobody has.
        /// </summary>
        private static (ChargingStationOperator? ChargingStationOperator, EMobilityProvider? EMobilityProvider, String? StoredId) ChargingTariffOfferOwners(String                                EntityId,
                                                                                                                                                         IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                                                                                                                         IEnumerable<EMobilityProvider>        EMobilityProviders)
        {

            if (!ChargingTariffOffer_Id.TryParse(EntityId, out var offerId))
                return (null, null, null);

            foreach (var chargingStationOperator in ChargingStationOperators)
                if (chargingStationOperator.TryGetChargingTariffOffer(offerId, out var offer))
                    return (chargingStationOperator, null, offer.Id.ToString());

            foreach (var eMobilityProvider in EMobilityProviders)
                if (eMobilityProvider.TryGetChargingTariffOffer(offerId, out var offer))
                    return (null, eMobilityProvider, offer.Id.ToString());

            return (null, null, null);

        }

        #endregion

        #region (private static) TryApplyChargingTariffOfferChange(Change, Index, CreatedAt, ChargingStationOperators, EMobilityProviders, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a charging tariff offer to the given charging station operators or e-mobility providers.
        /// </summary>
        private static Boolean TryApplyChargingTariffOfferChange(RoamingNetworkChange                                             Change,
                                                                 Int32                                                            Index,
                                                                 DateTimeOffset                                                   CreatedAt,
                                                                 Dictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                                 Dictionary<EMobilityProvider_Id,       EMobilityProvider>        EMobilityProviders,
                                                                 out JToken?                                                      CurrentValue,
                                                                 [NotNullWhen(false)] out String?                                 ErrorResponse)
        {

            CurrentValue = null;

            const String description = "charging tariff offer";

            var entityId = Change.EntityId;

            #region Its owner: the one having it, or the parent of a new one

            if (!ChargingTariffOffer_Id.TryParse(entityId, out _))
            {
                ErrorResponse = $"Change {Index}: invalid {description} identification '{entityId}'!";
                return false;
            }

            var (chargingStationOperator, eMobilityProvider, storedId) = ChargingTariffOfferOwners(entityId, ChargingStationOperators.Values, EMobilityProviders.Values);

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

                else if (Change.ParentEntityType != nameof(EMobilityProvider) ||
                         !EMobilityProvider_Id.TryParse(Change.ParentEntityId ?? "", out var providerId) ||
                         !EMobilityProviders.TryGetValue(providerId, out eMobilityProvider))
                {
                    ErrorResponse = $"Change {Index}: the {description} '{entityId}' to add needs its ChargingStationOperator or EMobilityProvider, an existing one!";
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
                                             : Change.ParentEntityType == nameof(EMobilityProvider) &&
                                               EMobilityProvider_Id.TryParse(Change.ParentEntityId ?? "", out var namedProviderId) &&
                                               namedProviderId == eMobilityProvider!.Id;

                    if (!isActualParent)
                    {
                        ErrorResponse = chargingStationOperator is not null
                                            ? $"Change {Index}: the {description} '{entityId}' belongs to ChargingStationOperator '{chargingStationOperator.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!"
                                            : $"Change {Index}: the {description} '{entityId}' belongs to EMobilityProvider '{eMobilityProvider!.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                        return false;
                    }

                }

            }

            var ownerJSON   = chargingStationOperator is not null
                                  ? chargingStationOperator.ToEmbeddedJSON()
                                  : eMobilityProvider!.     ToJSON();
            var children    = ownerJSON[ChargingTariffOffersProperty] as JArray;
            // Found under the identification it is kept with: the change may name it in another spelling.
            var entityJSON  = Change.Kind == RoamingNetworkChangeKind.Add
                                  ? null
                                  : children?.OfType<JObject>().FirstOrDefault(child => child["@id"]?.Value<String>() == storedId);

            #endregion

            // An element change is applied as the change of the property it amounts to.
            var isElementChange = Change.IsElementChange;

            if (!TryResolveElementChange(ref Change, entityJSON, description, out var elementValue, out ErrorResponse))
            {
                ErrorResponse = $"Change {Index}: {ErrorResponse}";
                return false;
            }

            var isProperty = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            // A property change compares the property, every other change the complete offer.
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

                    if (!ChangeableChargingTariffOfferProperties.Contains(propertyName))
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
                        ownerJSON[ChargingTariffOffersProperty] = children;
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
                        ownerJSON.Remove(ChargingTariffOffersProperty);

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a {description} is not supported yet!";
                    return false;

            }

            // The owner embedding it changed with it, at the time of the change set.
            ownerJSON["lastChange"] = CreatedAt.ToISO8601();

            #region The changed owner must still be a valid one

            if (chargingStationOperator is not null)
            {

                if (!ChargingStationOperator.TryParseWithinChangeSet(ownerJSON, out var changedOperator, out var changedOperatorError))
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

                if (!EMobilityProvider.TryParse(ownerJSON, out var changedProvider, out var changedProviderError))
                {
                    ErrorResponse = $"Change {Index}: {changedProviderError}";
                    return false;
                }

                changedProvider.ShareRuntimeOfAll(eMobilityProvider!);

                EMobilityProviders[eMobilityProvider!.Id] = changedProvider;

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
