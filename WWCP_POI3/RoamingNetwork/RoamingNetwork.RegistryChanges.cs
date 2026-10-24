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
    /// Changes of the registries of a roaming network: the entities it embeds directly next to its
    /// charging station operators, e.g. charging station manufacturers, e-mobility providers and grid operators.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region (private) Registry<TId, TEntity>

        /// <summary>
        /// What a change of one registry needs to know about its entities.
        /// </summary>
        /// <param name="Description">The entity in words, e.g. "grid operator".</param>
        /// <param name="Article">The indefinite article of the description, "a" or "an".</param>
        /// <param name="ChangeableProperties">The properties a change set can update or remove.</param>
        /// <param name="TryParseId">Parses an identification.</param>
        /// <param name="TryParse">Parses the JSON representation of an entity.</param>
        /// <param name="GetId">The identification of an entity.</param>
        /// <param name="ToJSON">The JSON representation of an entity.</param>
        /// <param name="ShareRuntimeOf">Lets a changed entity take the runtime status and internal data of its previous version.</param>
        /// <param name="Timestamps">Whether its entities have created and lastChange timestamps.</param>
        private sealed record Registry<TId, TEntity>(String                      Description,
                                                     String                      Article,
                                                     ImmutableHashSet<String>    ChangeableProperties,
                                                     POITextParser<TId>          TryParseId,
                                                     POIObjectParser<TEntity>    TryParse,
                                                     Func<TEntity, TId>          GetId,
                                                     Func<TEntity, JObject>      ToJSON,
                                                     Action<TEntity, TEntity>    ShareRuntimeOf,
                                                     Boolean                     Timestamps   = true)

            where TId     : notnull
            where TEntity : class;

        #endregion

        #region Data

        /// <summary>
        /// The properties of an entity of a registry that has a name, a description, an address, a homepage and a hotline.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableRegistryProperties = [
            RoamingNetworkOperator.BasePropertyKeys.Name,
            RoamingNetworkOperator.BasePropertyKeys.Description,
            RoamingNetworkOperator.BasePropertyKeys.DataSource,
            RoamingNetworkOperator.PropertyKeys.Address,
            RoamingNetworkOperator.PropertyKeys.Homepage,
            RoamingNetworkOperator.PropertyKeys.HotlinePhoneNumber,
            RoamingNetworkOperator.PropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of a party of a registry: those of an entity of a registry, and its keys.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeablePartyProperties = ChangeableRegistryProperties.Union([
            POIPartyKeys.SigningKeysProperty,
            POIPartyKeys.IdentityKeysProperty
        ]);

        /// <summary>
        /// The properties of the roaming network operator: those of a party but its signing keys, its e-mail addresses, its
        /// key statement and the certificates of its signing keys, the only place naming them.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableRoamingNetworkOperatorProperties = ChangeablePartyProperties.Remove(POIPartyKeys.SigningKeysProperty).Union([
            RoamingNetworkOperator.PropertyKeys.EMailAddress,
            RoamingNetworkOperator.PropertyKeys.HotlineEMailAddress,
            RoamingNetworkOperator.PropertyKeys.MinConsensus,
            RoamingNetworkOperator.PropertyKeys.KeyStatement,
            RoamingNetworkOperator.PropertyKeys.SigningKeyCertificates
        ]);

        private static readonly Registry<RoamingNetworkOperator_Id, RoamingNetworkOperator> RoamingNetworkOperatorRegistry = new (
            "roaming network operator",
            "a",
            ChangeableRoamingNetworkOperatorProperties,
            RoamingNetworkOperator_Id.TryParse,
            RoamingNetworkOperator.   TryParse,
            roamingNetworkOperator => roamingNetworkOperator.Id,
            roamingNetworkOperator => roamingNetworkOperator.ToJSON(Embedded: true),
            (changed, previous)    => changed.ShareRuntimeOf(previous)
        );

        /// <summary>
        /// The properties of a charging station or energy meter manufacturer: those of an entity of a registry, its contact
        /// data and data licenses, as of a charging station operator, and its keys. Its models are changed on their own.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableManufacturerProperties = ChangeableRegistryProperties.Union([
            ChargingStationManufacturer.BasePropertyKeys.DataLicenses,
            ChargingStationManufacturer.PropertyKeys.Logos,
            ChargingStationManufacturer.PropertyKeys.GeoLocation,
            ChargingStationManufacturer.PropertyKeys.Telephone,
            ChargingStationManufacturer.PropertyKeys.EMailAddress,
            ChargingStationManufacturer.PropertyKeys.TermsAndConditions,
            ChargingStationManufacturer.PropertyKeys.SigningKeys,
            ChargingStationManufacturer.PropertyKeys.IdentityKeys
        ]);

        private static readonly Registry<ChargingStationManufacturer_Id, ChargingStationManufacturer> ManufacturerRegistry = new (
            "charging station manufacturer",
            "a",
            ChangeableManufacturerProperties,
            ChargingStationManufacturer_Id.TryParse,
            ChargingStationManufacturer.TryParse,
            manufacturer        => manufacturer.Id,
            manufacturer        => manufacturer.ToJSON(Embedded: true),
            (changed, previous) => changed.ShareRuntimeOf(previous)
        );

        /// <summary>
        /// The properties of an e-mobility provider: those of a party, and its contact data and data licenses, as of a
        /// charging station operator.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableEMobilityProviderProperties = ChangeablePartyProperties.Union([
            EMobilityProvider.BasePropertyKeys.DataLicenses,
            EMobilityProvider.PropertyKeys.Logos,
            EMobilityProvider.PropertyKeys.GeoLocation,
            EMobilityProvider.PropertyKeys.Telephone,
            EMobilityProvider.PropertyKeys.EMailAddress,
            EMobilityProvider.PropertyKeys.TermsAndConditions
        ]);

        private static readonly Registry<EMobilityProvider_Id, EMobilityProvider> EMobilityProviderRegistry = new (
            "e-mobility provider",
            "an",
            ChangeableEMobilityProviderProperties,
            EMobilityProvider_Id.TryParse,
            EMobilityProvider.TryParse,
            eMobilityProvider => eMobilityProvider.Id,
            eMobilityProvider => eMobilityProvider.ToJSON(Embedded: true),
            // Its groups are embedded: theirs go along too.
            (changed, previous) => changed.ShareRuntimeOfAll(previous)
        );

        private static readonly Registry<GridOperator_Id, GridOperator> GridOperatorRegistry = new (
            "grid operator",
            "a",
            ChangeablePartyProperties,
            GridOperator_Id.TryParse,
            GridOperator.TryParse,
            gridOperator => gridOperator.Id,
            gridOperator => gridOperator.ToJSON(Embedded: true),
            // The meters of its grid connection points are embedded: theirs go along too.
            (changed, previous) => changed.ShareRuntimeOfAll(previous)
        );

        // In this file, after ChangeablePartyProperties: static fields are initialized in their textual order only within one file.
        private static readonly Registry<ParkingOperator_Id, ParkingOperator> ParkingOperatorRegistry = new (
            "parking operator",
            "a",
            ChangeablePartyProperties,
            ParkingOperator_Id.TryParse,
            ParkingOperator.TryParse,
            parkingOperator => parkingOperator.Id,
            parkingOperator => parkingOperator.ToJSON(Embedded: true),
            // Its garages, spaces and sensors are embedded: theirs go along too.
            (changed, previous) => changed.ShareRuntimeOfAll(previous)
        );

        private static readonly Registry<EnergyMeterManufacturer_Id, EnergyMeterManufacturer> EnergyMeterManufacturerRegistry = new (
            "energy meter manufacturer",
            "an",
            // The same as of a charging station manufacturer.
            ChangeableManufacturerProperties,
            EnergyMeterManufacturer_Id.TryParse,
            EnergyMeterManufacturer.TryParse,
            energyMeterManufacturer => energyMeterManufacturer.Id,
            energyMeterManufacturer => energyMeterManufacturer.ToJSON(Embedded: true),
            (changed, previous)     => changed.ShareRuntimeOf(previous)
        );

        /// <summary>
        /// The properties of a transparency software manufacturer: those of a charging station manufacturer, and what the
        /// Cyber Resilience Act asks of it. Its releases are changed on their own.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableTransparencySoftwareManufacturerProperties = ChangeableManufacturerProperties.Union([
            TransparencySoftwareManufacturer.PropertyKeys.CRARole,
            TransparencySoftwareManufacturer.PropertyKeys.VulnerabilityContact,
            TransparencySoftwareManufacturer.PropertyKeys.VulnerabilityReporting,
            TransparencySoftwareManufacturer.PropertyKeys.VulnerabilityDisclosurePolicy,
            TransparencySoftwareManufacturer.PropertyKeys.SecurityAdvisories
        ]);

        private static readonly Registry<TransparencySoftwareManufacturer_Id, TransparencySoftwareManufacturer> TransparencySoftwareManufacturerRegistry = new (
            "transparency software manufacturer",
            "a",
            ChangeableTransparencySoftwareManufacturerProperties,
            TransparencySoftwareManufacturer_Id.TryParse,
            TransparencySoftwareManufacturer.TryParse,
            manufacturer        => manufacturer.Id,
            manufacturer        => manufacturer.ToJSON(Embedded: true),
            // Its releases have no runtime status.
            (changed, previous) => changed.ShareRuntimeOf(previous)
        );

        /// <summary>
        /// The properties of a conformity assessment body: those of a charging station manufacturer, its number as a
        /// notified body and the legislations it is notified for. Its certificates are changed on their own.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableConformityAssessmentBodyProperties = ChangeableManufacturerProperties.Union([
            ConformityAssessmentBody.PropertyKeys.NotifiedBodyNumber,
            ConformityAssessmentBody.PropertyKeys.NotifiedFor
        ]);

        private static readonly Registry<ConformityAssessmentBody_Id, ConformityAssessmentBody> ConformityAssessmentBodyRegistry = new (
            "conformity assessment body",
            "a",
            ChangeableConformityAssessmentBodyProperties,
            ConformityAssessmentBody_Id.TryParse,
            ConformityAssessmentBody.TryParse,
            body                => body.Id,
            body                => body.ToJSON(Embedded: true),
            // Its certificates have no runtime status.
            (changed, previous) => changed.ShareRuntimeOf(previous)
        );

        #endregion


        #region (private) TryApplyRegistryChange<TId, TEntity>(Registry, Change, Index, CreatedAt, Entities, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of an entity of the given registry to the given entities.
        /// References to the entity are checked when the whole change set has been applied.
        /// </summary>
        private Boolean TryApplyRegistryChange<TId, TEntity>(Registry<TId, TEntity>            Registry,
                                                             RoamingNetworkChange              Change,
                                                             Int32                             Index,
                                                             DateTimeOffset                    CreatedAt,
                                                             Dictionary<TId, TEntity>          Entities,
                                                             out JToken?                       CurrentValue,
                                                             [NotNullWhen(false)] out String?  ErrorResponse)

            where TId     : notnull
            where TEntity : class

        {

            CurrentValue = null;

            var description = Registry.Description;

            if (Change.ParentEntityType is not null &&
               (Change.ParentEntityType != nameof(POI.RoamingNetwork) || !IsThisRoamingNetwork(Change.ParentEntityId)))
            {
                ErrorResponse = $"Change {Index}: {Registry.Article} {description} of this roaming network belongs to RoamingNetwork '{Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                return false;
            }

            if (!Registry.TryParseId(Change.EntityId, out var entityId))
            {
                ErrorResponse = $"Change {Index}: invalid {description} identification '{Change.EntityId}'!";
                return false;
            }

            Entities.TryGetValue(entityId, out var currentEntity);

            var currentJSON  = currentEntity is not null ? Registry.ToJSON(currentEntity) : null;

            // An element change is applied as the change of the property it amounts to.
            var isElementChange = Change.IsElementChange;

            if (!TryResolveElementChange(ref Change, currentJSON, description, out var elementValue, out ErrorResponse))
            {
                ErrorResponse = $"Change {Index}: {ErrorResponse}";
                return false;
            }

            var isProperty   = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            if (Change.Kind != RoamingNetworkChangeKind.Add && currentJSON is null)
            {
                ErrorResponse = $"Change {Index}: the {description} '{entityId}' does not exist!";
                return false;
            }

            // A property change compares the property, every other change the complete entity.
            CurrentValue = isProperty
                               ? currentJSON![Change.PropertyName!]?.DeepClone()
                               : currentJSON;

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

                case RoamingNetworkChangeKind.UpdateProperty:
                case RoamingNetworkChangeKind.RemoveProperty:

                    var propertyName = Change.PropertyName!;

                    if (!Registry.ChangeableProperties.Contains(propertyName))
                    {
                        ErrorResponse = $"Change {Index}: the property '{propertyName}' of {Registry.Article} {description} can not be changed!";
                        return false;
                    }

                    if (Change.Kind == RoamingNetworkChangeKind.RemoveProperty)
                    {

                        if (CurrentValue is null)
                        {
                            ErrorResponse = $"Change {Index}: the property '{propertyName}' of the {description} '{entityId}' does not exist!";
                            return false;
                        }

                        currentJSON!.Remove(propertyName);

                    }
                    else
                        currentJSON![propertyName] = Change.NewValue;

                    if (Registry.Timestamps)
                        currentJSON[BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();

                    if (!Registry.TryParse(currentJSON, out var changedEntity, out var changedError))
                    {
                        ErrorResponse = $"Change {Index}: {changedError}";
                        return false;
                    }

                    // Admin status, status and internal data are not content: they go along.
                    Registry.ShareRuntimeOf(changedEntity, currentEntity!);

                    Entities[entityId] = changedEntity;
                    break;

                case RoamingNetworkChangeKind.Add:

                    if (currentEntity is not null)
                    {
                        ErrorResponse = $"Change {Index}: the {description} '{entityId}' already exists!";
                        return false;
                    }

                    if (Change.NewValue is not JObject newJSON)
                    {
                        ErrorResponse = $"Change {Index}: the {description} to add must be a JSON object!";
                        return false;
                    }

                    var added = (JObject) newJSON.DeepClone();

                    if (Registry.Timestamps)
                    {
                        added[BasePropertyKeys.Created]    ??= CreatedAt.ToISO8601();
                        added[BasePropertyKeys.LastChange]   = CreatedAt.ToISO8601();
                    }

                    if (!Registry.TryParse(added, out var newEntity, out var newError))
                    {
                        ErrorResponse = $"Change {Index}: {newError}";
                        return false;
                    }

                    if (!Registry.GetId(newEntity).Equals(entityId))
                    {
                        ErrorResponse = $"Change {Index}: the {description} '{Registry.GetId(newEntity)}' is not the one to add, '{entityId}'!";
                        return false;
                    }

                    Entities.Add(entityId, newEntity);
                    break;

                case RoamingNetworkChangeKind.Remove:
                    Entities.Remove(entityId);
                    break;

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of {Registry.Article} {description} is not supported yet!";
                    return false;

            }

            // The old value of an element change is the value of what it addresses.
            if (isElementChange)
                CurrentValue = elementValue;

            ErrorResponse = null;
            return true;

        }

        #endregion


    }

}
