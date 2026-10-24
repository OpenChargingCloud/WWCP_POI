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
    /// A single immutable operation against an entity in a roaming network.
    /// </summary>
    public sealed record RoamingNetworkChange
    {

        #region Data

        private readonly JToken? oldValue;
        private readonly JToken? newValue;

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/change/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context            = "@context";

            /// <summary>
            /// The property of its change kind.
            /// </summary>
            public const String  Kind               = "kind";

            /// <summary>
            /// The property of its entity type.
            /// </summary>
            public const String  EntityType         = "entityType";

            /// <summary>
            /// The property of its entity identification.
            /// </summary>
            public const String  EntityId           = "entityId";

            /// <summary>
            /// The property of its property name.
            /// </summary>
            public const String  PropertyName       = "propertyName";

            /// <summary>
            /// The property of its parent entity type.
            /// </summary>
            public const String  ParentEntityType   = "parentEntityType";

            /// <summary>
            /// The property of its parent entity identification.
            /// </summary>
            public const String  ParentEntityId     = "parentEntityId";

            /// <summary>
            /// The property of its element path.
            /// </summary>
            public const String  ElementPath        = "elementPath";

            /// <summary>
            /// The property of its expected previous value.
            /// </summary>
            public const String  OldValue           = "oldValue";

            /// <summary>
            /// The property of its new value.
            /// </summary>
            public const String  NewValue           = "newValue";

        }

        #endregion


        /// <summary>
        /// The operation to perform.
        /// </summary>
        public RoamingNetworkChangeKind               Kind                { get; }

        /// <summary>
        /// The infrastructure entity type targeted by graph operations or owning a nested element path.
        /// </summary>
        public String                                 EntityType          { get; }

        /// <summary>
        /// The identifier of the target graph entity or owner of a nested element path.
        /// </summary>
        public String                                 EntityId            { get; }

        /// <summary>
        /// The JSON property to update, for property operations.
        /// </summary>
        public String?                                PropertyName        { get; }

        /// <summary>
        /// The ordered ownership path from the addressed graph entity to one nested value.
        /// Empty for graph Add/Remove and top-level property operations.
        /// </summary>
        public ImmutableArray<POIElementPathSegment>  ElementPath         { get; }

        /// <summary>
        /// The explicit parent type, when required by the operation.
        /// </summary>
        public String?                                ParentEntityType    { get; }

        /// <summary>
        /// The explicit parent identifier; connectors require their EVSE scope.
        /// </summary>
        public String?                                ParentEntityId      { get; }

        /// <summary>
        /// An optional expected previous value for optimistic conflict detection.
        /// Null means: the previous value is not checked.
        /// A JSON null means: the value is expected to be absent.
        /// Every read returns an independent copy.
        /// </summary>
        public JToken?                                OldValue
            => oldValue?.DeepClone();

        /// <summary>
        /// The new entity document or property value.
        /// Every read returns an independent copy.
        /// </summary>
        public JToken?                                NewValue
            => newValue?.DeepClone();

        /// <summary>
        /// Whether this change addresses an element of a property of the entity by its element path.
        /// </summary>
        public Boolean                                IsElementChange
            => !ElementPath.IsDefaultOrEmpty;

        /// <summary>
        /// The property of the entity itself this change changes: its property name, or for an element change
        /// the first property of its element path; null for Add and Remove. Owners and changeable properties
        /// are those of this property.
        /// </summary>
        public String?                                EntityPropertyName
            => IsElementChange
                   ? ElementPath[0].PropertyName
                   : PropertyName;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a validated operation owning independent copies of its JSON values.
        /// JSON null is not a value here: a property is cleared by RemoveProperty.
        /// </summary>
        /// <param name="Kind">The operation to perform.</param>
        /// <param name="EntityType">A stable entity type, for example <c>ChargingStation</c>.</param>
        /// <param name="EntityId">The entity's canonical identifier.</param>
        /// <param name="PropertyName">The property being changed, for property updates.</param>
        /// <param name="OldValue">An optional expected previous value for conflict detection.</param>
        /// <param name="NewValue">The new property value or full entity document.</param>
        /// <param name="ParentEntityType">The optional graph parent type; connectors require their EVSE scope.</param>
        /// <param name="ParentEntityId">The optional graph parent identity.</param>
        /// <param name="ElementPath">The structured ownership path for nested element operations; empty for graph operations.</param>
        public RoamingNetworkChange(RoamingNetworkChangeKind               Kind,
                                    String                                 EntityType,
                                    String                                 EntityId,
                                    String?                                PropertyName,
                                    JToken?                                OldValue,
                                    JToken?                                NewValue,
                                    String?                                ParentEntityType   = null,
                                    String?                                ParentEntityId     = null,
                                    ImmutableArray<POIElementPathSegment>  ElementPath        = default)
        {

            if (!Enum.IsDefined(Kind))
                throw new ArgumentOutOfRangeException(nameof(Kind));

            this.EntityType  = Required(EntityType, nameof(EntityType));
            this.EntityId    = Required(EntityId,   nameof(EntityId));

            if ((ParentEntityType is null) != (ParentEntityId is null))
                throw new ArgumentException("Parent type and parent identifier must be supplied together.");

            this.ParentEntityType  = ParentEntityType is null ? null : Required(ParentEntityType, nameof(ParentEntityType));
            this.ParentEntityId    = ParentEntityId   is null ? null : Required(ParentEntityId,   nameof(ParentEntityId));

            if (OldValue?.Type is JTokenType.Undefined ||
                NewValue?.Type is JTokenType.Null or JTokenType.Undefined)
            {
                throw new ArgumentException("A new JSON value must not be null or undefined; a property is cleared by RemoveProperty.");
            }

            var elementOperation = Kind is RoamingNetworkChangeKind.AddElement            or
                                           RoamingNetworkChangeKind.RemoveElement         or
                                           RoamingNetworkChangeKind.ReplaceElement        or
                                           RoamingNetworkChangeKind.UpdateElementProperty or
                                           RoamingNetworkChangeKind.RemoveElementProperty;

            if (elementOperation && (ElementPath.IsDefaultOrEmpty || ElementPath.Any(segment => segment is null)))
                throw new ArgumentException("Element operations require a nonempty path without null segments.", nameof(ElementPath));

            if (!elementOperation && !ElementPath.IsDefaultOrEmpty)
                throw new ArgumentException("Only element operations may contain an element path.", nameof(ElementPath));

            switch (Kind)
            {

                case RoamingNetworkChangeKind.Add when NewValue is not JObject || OldValue is not null || PropertyName is not null:
                    throw new ArgumentException("Add requires an entity object and no old value or property name.");

                case RoamingNetworkChangeKind.Remove when NewValue is not null || PropertyName is not null:
                    throw new ArgumentException("Remove must not contain a new value or property name.");

                case RoamingNetworkChangeKind.UpdateProperty when NewValue is null || String.IsNullOrWhiteSpace(PropertyName):
                    throw new ArgumentException("UpdateProperty requires a property name and a new JSON value.");

                case RoamingNetworkChangeKind.AddElement when NewValue is null || OldValue is not null || PropertyName is not null:
                    throw new ArgumentException("AddElement requires a new value and no old value or property name.");

                case RoamingNetworkChangeKind.RemoveElement when NewValue is not null || PropertyName is not null:
                    throw new ArgumentException("RemoveElement must not contain a new value or property name.");

                case RoamingNetworkChangeKind.ReplaceElement when NewValue is null || PropertyName is not null:
                    throw new ArgumentException("ReplaceElement requires a new value and no property name.");

                case RoamingNetworkChangeKind.UpdateElementProperty when NewValue is null || String.IsNullOrWhiteSpace(PropertyName):
                    throw new ArgumentException("UpdateElementProperty requires a property name and a new value.");

                case RoamingNetworkChangeKind.RemoveProperty or RoamingNetworkChangeKind.RemoveElementProperty
                    when NewValue is not null || String.IsNullOrWhiteSpace(PropertyName):
                    throw new ArgumentException("Property removal requires a property name and no new value.");

            }

            this.Kind          = Kind;
            this.PropertyName  = PropertyName;
            this.oldValue      = OldValue?.DeepClone();
            this.newValue      = NewValue?.DeepClone();
            this.ElementPath   = elementOperation ? ElementPath : [];

        }

        #endregion


        #region Add                   (EntityType, EntityId, Entity, ...)

        /// <summary>
        /// Add an entity using its JSON representation.
        /// </summary>
        /// <param name="EntityType">The type of the new entity.</param>
        /// <param name="EntityId">The identification of the new entity.</param>
        /// <param name="Entity">The JSON representation of the new entity.</param>
        /// <param name="ParentEntityType">The optional graph parent type.</param>
        /// <param name="ParentEntityId">The optional graph parent identity.</param>
        public static RoamingNetworkChange Add(String   EntityType,
                                               String   EntityId,
                                               JObject  Entity,
                                               String?  ParentEntityType   = null,
                                               String?  ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.Add,
                    EntityType,
                    EntityId,
                    null,
                    null,
                    Entity,
                    ParentEntityType,
                    ParentEntityId);

        #endregion

        #region Add                   (ChargingStationOperator)

        /// <summary>
        /// Add the given charging station operator to its roaming network, without its charging pools,
        /// tariffs and groups: they are its own to add, see AddChildren(ChargingStationOperator).
        /// </summary>
        /// <param name="ChargingStationOperator">The new charging station operator.</param>
        public static RoamingNetworkChange Add(ChargingStationOperator ChargingStationOperator)

            => Add(nameof(POI.ChargingStationOperator),
                   ChargingStationOperator.Id.ToString(),
                   WithoutChildren(nameof(POI.ChargingStationOperator),
                                   ChargingStationOperator.ToEmbeddedJSON()));

        #endregion

        #region AddChildren           (ChargingStationOperator)

        /// <summary>
        /// Add the charging tariff templates, charging tariffs, the EVSE, charging station, charging pool and energy meter
        /// templates, charging pools and groups of the given charging station operator, in this order, to the operator
        /// once added: a change set the operator signs.
        /// </summary>
        /// <param name="ChargingStationOperator">The charging station operator with its children.</param>
        public static IReadOnlyList<RoamingNetworkChange> AddChildren(ChargingStationOperator ChargingStationOperator)

            => [
                   .. ChargingStationOperator.ChargingTariffTemplates. Select(chargingTariffTemplate  => Add(chargingTariffTemplate)),
                   .. ChargingStationOperator.ChargingTariffs.         Select(chargingTariff          => Add(chargingTariff)),
                   .. ChargingStationOperator.EVSETemplates.           Select(evseTemplate            => Add(evseTemplate)),
                   .. ChargingStationOperator.ChargingStationTemplates.Select(chargingStationTemplate => Add(chargingStationTemplate)),
                   .. ChargingStationOperator.ChargingPoolTemplates.   Select(chargingPoolTemplate    => Add(chargingPoolTemplate)),
                   .. ChargingStationOperator.EnergyMeterTemplates.    Select(energyMeterTemplate     => Add(energyMeterTemplate, nameof(POI.ChargingStationOperator), ChargingStationOperator.Id.ToString())),
                   .. ChargingStationOperator.ChargingPools.        Select(chargingPool         => Add(chargingPool)),
                   .. ChargingStationOperator.EVSEGroups.           Select(evseGroup            => Add(evseGroup)),
                   .. ChargingStationOperator.ChargingStationGroups.Select(chargingStationGroup => Add(chargingStationGroup)),
                   .. ChargingStationOperator.ChargingPoolGroups.   Select(chargingPoolGroup    => Add(chargingPoolGroup)),
                   .. ChargingStationOperator.ChargingTariffGroups. Select(chargingTariffGroup  => Add(chargingTariffGroup)),
                   .. ChargingStationOperator.EnergyMeterGroups.    Select(energyMeterGroup     => Add(energyMeterGroup, nameof(POI.ChargingStationOperator), ChargingStationOperator.Id.ToString())),
                   .. ChargingStationOperator.ChargingTariffOffers. Select(chargingTariffOffer  => Add(chargingTariffOffer, ChargingStationOperator.Id))
               ];

        #endregion

        #region (private static) WithoutChildren(EntityType, Party)

        /// <summary>
        /// The given JSON representation of a party without the embedded children
        /// it must not be added with.
        /// </summary>
        /// <param name="EntityType">The entity type of the party.</param>
        /// <param name="Party">The JSON representation of the party, changed in place.</param>
        private static JObject WithoutChildren(String   EntityType,
                                               JObject  Party)
        {

            if (RoamingNetwork.PartyChildren.TryGetValue(EntityType, out var children))
                foreach (var child in children)
                    Party.Remove(child);

            return Party;

        }

        #endregion

        #region Remove                (ChargingStationOperatorId, OldValue = null)

        /// <summary>
        /// Remove the given charging station operator from its roaming network,
        /// optionally checking its complete previous JSON representation.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The identification of the charging station operator.</param>
        /// <param name="OldValue">The optional expected previous JSON representation of the charging station operator.</param>
        public static RoamingNetworkChange Remove(ChargingStationOperator_Id  ChargingStationOperatorId,
                                                  JToken?                     OldValue   = null)

            => Remove(nameof(POI.ChargingStationOperator),
                      ChargingStationOperatorId.ToString(),
                      OldValue);

        #endregion

        #region Remove                (EntityType, EntityId, OldValue = null, ...)

        /// <summary>
        /// Remove an entity, optionally checking its complete previous document.
        /// </summary>
        /// <param name="EntityType">The type of the entity.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="OldValue">The optional expected previous JSON representation of the entity.</param>
        /// <param name="ParentEntityType">The optional graph parent type.</param>
        /// <param name="ParentEntityId">The optional graph parent identity.</param>
        public static RoamingNetworkChange Remove(String   EntityType,
                                                  String   EntityId,
                                                  JToken?  OldValue           = null,
                                                  String?  ParentEntityType   = null,
                                                  String?  ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.Remove,
                    EntityType,
                    EntityId,
                    null,
                    OldValue,
                    null,
                    ParentEntityType,
                    ParentEntityId);

        #endregion

        #region UpdateProperty        (EntityType, EntityId, PropertyName, OldValue, NewValue, ...)

        /// <summary>
        /// Update one entity property, optionally asserting its previous value.
        /// </summary>
        /// <param name="EntityType">The type of the entity.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="PropertyName">The JSON property to update.</param>
        /// <param name="OldValue">The optional expected previous value.</param>
        /// <param name="NewValue">The new value.</param>
        /// <param name="ParentEntityType">The optional graph parent type.</param>
        /// <param name="ParentEntityId">The optional graph parent identity.</param>
        public static RoamingNetworkChange UpdateProperty(String   EntityType,
                                                          String   EntityId,
                                                          String   PropertyName,
                                                          JToken?  OldValue,
                                                          JToken   NewValue,
                                                          String?  ParentEntityType   = null,
                                                          String?  ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.UpdateProperty,
                    EntityType,
                    EntityId,
                    PropertyName,
                    OldValue,
                    NewValue,
                    ParentEntityType,
                    ParentEntityId);

        #endregion

        #region UpdateProperty        (RoamingNetwork, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Update one property of the given roaming network, optionally asserting its previous value.
        /// </summary>
        /// <param name="RoamingNetwork">The roaming network.</param>
        /// <param name="PropertyName">The JSON property to update.</param>
        /// <param name="OldValue">The optional expected previous value.</param>
        /// <param name="NewValue">The new value.</param>
        public static RoamingNetworkChange UpdateProperty(RoamingNetwork  RoamingNetwork,
                                                          String          PropertyName,
                                                          JToken?         OldValue,
                                                          JToken          NewValue)

            => UpdateProperty(nameof(POI.RoamingNetwork),
                              RoamingNetwork.Id.ToString(),
                              PropertyName,
                              OldValue,
                              NewValue);

        #endregion

        #region UpdateProperty        (ChargingStationOperator, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Update one property of the given charging station operator, optionally asserting its previous value.
        /// </summary>
        /// <param name="ChargingStationOperator">The charging station operator.</param>
        /// <param name="PropertyName">The JSON property to update.</param>
        /// <param name="OldValue">The optional expected previous value.</param>
        /// <param name="NewValue">The new value.</param>
        public static RoamingNetworkChange UpdateProperty(ChargingStationOperator  ChargingStationOperator,
                                                          String                   PropertyName,
                                                          JToken?                  OldValue,
                                                          JToken                   NewValue)

            => UpdateProperty(nameof(POI.ChargingStationOperator),
                              ChargingStationOperator.Id.ToString(),
                              PropertyName,
                              OldValue,
                              NewValue);

        #endregion

        #region RemoveProperty        (EntityType, EntityId, PropertyName, OldValue = null, ...)

        /// <summary>
        /// Remove an existing optional entity property, optionally asserting its previous value.
        /// </summary>
        /// <param name="EntityType">The type of the entity.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="PropertyName">The JSON property to remove.</param>
        /// <param name="OldValue">The optional expected previous value.</param>
        /// <param name="ParentEntityType">The optional graph parent type.</param>
        /// <param name="ParentEntityId">The optional graph parent identity.</param>
        public static RoamingNetworkChange RemoveProperty(String   EntityType,
                                                          String   EntityId,
                                                          String   PropertyName,
                                                          JToken?  OldValue           = null,
                                                          String?  ParentEntityType   = null,
                                                          String?  ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.RemoveProperty,
                    EntityType,
                    EntityId,
                    PropertyName,
                    OldValue,
                    null,
                    ParentEntityType,
                    ParentEntityId);

        #endregion

        #region RemoveProperty        (RoamingNetwork, PropertyName, OldValue = null)

        /// <summary>
        /// Remove an existing optional property of the given roaming network, optionally asserting its previous value.
        /// </summary>
        /// <param name="RoamingNetwork">The roaming network.</param>
        /// <param name="PropertyName">The JSON property to remove.</param>
        /// <param name="OldValue">The optional expected previous value.</param>
        public static RoamingNetworkChange RemoveProperty(RoamingNetwork  RoamingNetwork,
                                                          String          PropertyName,
                                                          JToken?         OldValue   = null)

            => RemoveProperty(nameof(POI.RoamingNetwork),
                              RoamingNetwork.Id.ToString(),
                              PropertyName,
                              OldValue);

        #endregion

        #region RemoveProperty        (ChargingStationOperator, PropertyName, OldValue = null)

        /// <summary>
        /// Remove an existing optional property of the given charging station operator, optionally asserting its previous value.
        /// </summary>
        /// <param name="ChargingStationOperator">The charging station operator.</param>
        /// <param name="PropertyName">The JSON property to remove.</param>
        /// <param name="OldValue">The optional expected previous value.</param>
        public static RoamingNetworkChange RemoveProperty(ChargingStationOperator  ChargingStationOperator,
                                                          String                   PropertyName,
                                                          JToken?                  OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingStationOperator),
                              ChargingStationOperator.Id.ToString(),
                              PropertyName,
                              OldValue);

        #endregion

        #region Add                   (ChargingStationManufacturer)

        /// <summary>
        /// Add the given charging station manufacturer to its roaming network.
        /// </summary>
        /// <param name="ChargingStationManufacturer">The new charging station manufacturer.</param>
        public static RoamingNetworkChange Add(ChargingStationManufacturer ChargingStationManufacturer)

            => Add(nameof(POI.ChargingStationManufacturer),
                   ChargingStationManufacturer.Id.ToString(),
                   ChargingStationManufacturer.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given charging station manufacturer, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingStationManufacturer_Id  ChargingStationManufacturerId,
                                                  JToken?                         OldValue   = null)

            => Remove(nameof(POI.ChargingStationManufacturer),
                      ChargingStationManufacturerId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given charging station manufacturer, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingStationManufacturer  ChargingStationManufacturer,
                                                          String                       PropertyName,
                                                          JToken?                      OldValue,
                                                          JToken                       NewValue)

            => UpdateProperty(nameof(POI.ChargingStationManufacturer), ChargingStationManufacturer.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging station manufacturer, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingStationManufacturer  ChargingStationManufacturer,
                                                          String                       PropertyName,
                                                          JToken?                      OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingStationManufacturer), ChargingStationManufacturer.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (EMobilityProvider)

        /// <summary>
        /// Add the given e-mobility provider to its roaming network.
        /// </summary>
        /// <param name="EMobilityProvider">The new e-mobility provider.</param>
        public static RoamingNetworkChange Add(EMobilityProvider EMobilityProvider)

            => Add(nameof(POI.EMobilityProvider),
                   EMobilityProvider.Id.ToString(),
                   WithoutChildren(nameof(POI.EMobilityProvider),
                                   EMobilityProvider.ToJSON(Embedded: true)));

        /// <summary>
        /// Add the EVSE groups, charging station groups, charging pool groups and charging tariff offers of the given
        /// e-mobility provider, in this order, to the provider once added: a change set the provider signs.
        /// </summary>
        /// <param name="EMobilityProvider">The e-mobility provider with its groups.</param>
        public static IReadOnlyList<RoamingNetworkChange> AddChildren(EMobilityProvider EMobilityProvider)

            => [
                   .. EMobilityProvider.EVSEGroups.           Select(evseGroup            => Add(evseGroup,            EMobilityProvider.Id)),
                   .. EMobilityProvider.ChargingStationGroups.Select(chargingStationGroup => Add(chargingStationGroup, EMobilityProvider.Id)),
                   .. EMobilityProvider.ChargingPoolGroups.   Select(chargingPoolGroup    => Add(chargingPoolGroup,    EMobilityProvider.Id)),
                   .. EMobilityProvider.ChargingTariffOffers. Select(chargingTariffOffer  => Add(chargingTariffOffer,  EMobilityProvider.Id))
               ];

        /// <summary>
        /// Remove the given e-mobility provider, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(EMobilityProvider_Id  EMobilityProviderId,
                                                  JToken?               OldValue              = null)

            => Remove(nameof(POI.EMobilityProvider),
                      EMobilityProviderId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given e-mobility provider, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(EMobilityProvider  EMobilityProvider,
                                                          String             PropertyName,
                                                          JToken?            OldValue,
                                                          JToken             NewValue)

            => UpdateProperty(nameof(POI.EMobilityProvider), EMobilityProvider.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given e-mobility provider, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(EMobilityProvider  EMobilityProvider,
                                                          String             PropertyName,
                                                          JToken?            OldValue   = null)

            => RemoveProperty(nameof(POI.EMobilityProvider), EMobilityProvider.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region UpdateProperty        (RoamingNetworkOperator, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Update one property of the given roaming network operator, e.g. its keys, optionally asserting its previous value.
        /// A roaming network operator is neither added nor removed: it comes with the roaming network it operates.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(RoamingNetworkOperator  RoamingNetworkOperator,
                                                          String                  PropertyName,
                                                          JToken?                 OldValue,
                                                          JToken                  NewValue)

            => UpdateProperty(nameof(POI.RoamingNetworkOperator), RoamingNetworkOperator.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given roaming network operator, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(RoamingNetworkOperator  RoamingNetworkOperator,
                                                          String                  PropertyName,
                                                          JToken?                 OldValue   = null)

            => RemoveProperty(nameof(POI.RoamingNetworkOperator), RoamingNetworkOperator.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (GridOperator)

        /// <summary>
        /// Add the given grid operator to its roaming network, without its grid connection points:
        /// they are added on their own, see AddChildren(GridOperator).
        /// </summary>
        /// <param name="GridOperator">The new grid operator.</param>
        public static RoamingNetworkChange Add(GridOperator GridOperator)

            => Add(nameof(POI.GridOperator),
                   GridOperator.Id.ToString(),
                   WithoutChildren(nameof(POI.GridOperator),
                                   GridOperator.ToJSON(Embedded: true)));

        /// <summary>
        /// Add the energy meter templates of the given grid operator, its grid connection points and then its energy meter
        /// groups, to the operator once added: a change set their maintainers or the grid operator sign.
        /// </summary>
        /// <param name="GridOperator">The grid operator with its energy meter templates, grid connection points and energy meter groups.</param>
        public static IReadOnlyList<RoamingNetworkChange> AddChildren(GridOperator GridOperator)

            => [
                   .. GridOperator.EnergyMeterTemplates.Select(energyMeterTemplate => Add(energyMeterTemplate, nameof(POI.GridOperator), GridOperator.Id.ToString())),
                   .. GridOperator.GridConnectionPoints.Select(gridConnectionPoint => Add(gridConnectionPoint, GridOperator.Id)),
                   .. GridOperator.EnergyMeterGroups.   Select(energyMeterGroup    => Add(energyMeterGroup,    nameof(POI.GridOperator), GridOperator.Id.ToString()))
               ];

        /// <summary>
        /// Remove the given grid operator, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(GridOperator_Id  GridOperatorId,
                                                  JToken?          OldValue         = null)

            => Remove(nameof(POI.GridOperator),
                      GridOperatorId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given grid operator, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(GridOperator  GridOperator,
                                                          String        PropertyName,
                                                          JToken?       OldValue,
                                                          JToken        NewValue)

            => UpdateProperty(nameof(POI.GridOperator), GridOperator.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given grid operator, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(GridOperator  GridOperator,
                                                          String        PropertyName,
                                                          JToken?       OldValue   = null)

            => RemoveProperty(nameof(POI.GridOperator), GridOperator.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ParkingOperator)

        /// <summary>
        /// Add the given parking operator to its roaming network, without its parking data:
        /// it is its own to add, see AddChildren(ParkingOperator).
        /// </summary>
        /// <param name="ParkingOperator">The new parking operator.</param>
        public static RoamingNetworkChange Add(ParkingOperator ParkingOperator)

            => Add(nameof(POI.ParkingOperator),
                   ParkingOperator.Id.ToString(),
                   WithoutChildren(nameof(POI.ParkingOperator),
                                   ParkingOperator.ToJSON(Embedded: true)));

        /// <summary>
        /// Add the parking garages, space templates, spaces, sensors, space groups and products of the given parking operator,
        /// in this order, to the operator once added: a change set the operator signs.
        /// </summary>
        /// <param name="ParkingOperator">The parking operator with its parking data.</param>
        public static IReadOnlyList<RoamingNetworkChange> AddChildren(ParkingOperator ParkingOperator)

            => [
                   .. ParkingOperator.ParkingGarages.    Select(parkingGarage     => Add(parkingGarage,     ParkingOperator.Id)),
                   .. ParkingOperator.ParkingSpaceTemplates.Select(parkingSpaceTemplate => Add(parkingSpaceTemplate, ParkingOperator.Id)),
                   .. ParkingOperator.ParkingSpaces.     Select(parkingSpace      => Add(parkingSpace,      ParkingOperator.Id)),
                   .. ParkingOperator.ParkingSensors.    Select(parkingSensor     => Add(parkingSensor,     ParkingOperator.Id)),
                   .. ParkingOperator.ParkingSpaceGroups.Select(parkingSpaceGroup => Add(parkingSpaceGroup, ParkingOperator.Id)),
                   .. ParkingOperator.ParkingProducts.   Select(parkingProduct    => Add(parkingProduct,    ParkingOperator.Id))
               ];

        /// <summary>
        /// Remove the given parking operator, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ParkingOperator_Id  ParkingOperatorId,
                                                  JToken?             OldValue            = null)

            => Remove(nameof(POI.ParkingOperator),
                      ParkingOperatorId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given parking operator, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ParkingOperator  ParkingOperator,
                                                          String           PropertyName,
                                                          JToken?          OldValue,
                                                          JToken           NewValue)

            => UpdateProperty(nameof(POI.ParkingOperator), ParkingOperator.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given parking operator, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ParkingOperator  ParkingOperator,
                                                          String           PropertyName,
                                                          JToken?          OldValue   = null)

            => RemoveProperty(nameof(POI.ParkingOperator), ParkingOperator.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ParkingGarage | ParkingSpace | ParkingSensor, ParkingOperatorId)

        /// <summary>
        /// Add the given parking garage to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingGarage       ParkingGarage,
                                               ParkingOperator_Id  ParkingOperatorId)

            => Add(nameof(POI.ParkingGarage), ParkingGarage.Id.ToString(), ParkingGarage.ToJSON(Embedded: true), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

        /// <summary>
        /// Add the given parking space to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSpace        ParkingSpace,
                                               ParkingOperator_Id  ParkingOperatorId)

            => Add(nameof(POI.ParkingSpace),  ParkingSpace. Id.ToString(), ParkingSpace. ToJSON(Embedded: true), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

        /// <summary>
        /// Add the given parking space to the given charging station operator, which defines it where no parking operator does.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSpace                ParkingSpace,
                                               ChargingStationOperator_Id  ChargingStationOperatorId)

            => Add(nameof(POI.ParkingSpace),  ParkingSpace. Id.ToString(), ParkingSpace. ToJSON(Embedded: true), nameof(POI.ChargingStationOperator), ChargingStationOperatorId.ToString());

        /// <summary>
        /// Add the given parking sensor to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSensor       ParkingSensor,
                                               ParkingOperator_Id  ParkingOperatorId)

            => Add(nameof(POI.ParkingSensor), ParkingSensor.Id.ToString(), ParkingSensor.ToJSON(Embedded: true), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

        #endregion

        #region Remove                (ParkingGarageId | ParkingSpaceId | ParkingSensorId, OldValue = null)

        /// <summary>
        /// Remove the given parking garage, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ParkingGarage_Id  ParkingGarageId,
                                                  JToken?           OldValue   = null)

            => Remove(nameof(POI.ParkingGarage), ParkingGarageId.ToString(), OldValue);

        /// <summary>
        /// Remove the given parking space, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ParkingSpace_Id   ParkingSpaceId,
                                                  JToken?           OldValue   = null)

            => Remove(nameof(POI.ParkingSpace),  ParkingSpaceId. ToString(), OldValue);

        /// <summary>
        /// Remove the given parking sensor, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ParkingSensor_Id  ParkingSensorId,
                                                  JToken?           OldValue   = null)

            => Remove(nameof(POI.ParkingSensor), ParkingSensorId.ToString(), OldValue);

        #endregion

        #region UpdateProperty        (ParkingGarage | ParkingSpace | ParkingSensor, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Update one property of the given parking garage, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ParkingGarage  ParkingGarage,
                                                          String         PropertyName,
                                                          JToken?        OldValue,
                                                          JToken         NewValue)

            => UpdateProperty(nameof(POI.ParkingGarage), ParkingGarage.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Update one property of the given parking space, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ParkingSpace   ParkingSpace,
                                                          String         PropertyName,
                                                          JToken?        OldValue,
                                                          JToken         NewValue)

            => UpdateProperty(nameof(POI.ParkingSpace),  ParkingSpace. Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Update one property of the given parking sensor, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ParkingSensor  ParkingSensor,
                                                          String         PropertyName,
                                                          JToken?        OldValue,
                                                          JToken         NewValue)

            => UpdateProperty(nameof(POI.ParkingSensor), ParkingSensor.Id.ToString(), PropertyName, OldValue, NewValue);

        #endregion

        #region RemoveProperty        (ParkingGarage | ParkingSpace | ParkingSensor, PropertyName, OldValue = null)

        /// <summary>
        /// Remove one property of the given parking garage, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ParkingGarage  ParkingGarage,
                                                          String         PropertyName,
                                                          JToken?        OldValue   = null)

            => RemoveProperty(nameof(POI.ParkingGarage), ParkingGarage.Id.ToString(), PropertyName, OldValue);

        /// <summary>
        /// Remove one property of the given parking space, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ParkingSpace   ParkingSpace,
                                                          String         PropertyName,
                                                          JToken?        OldValue   = null)

            => RemoveProperty(nameof(POI.ParkingSpace),  ParkingSpace. Id.ToString(), PropertyName, OldValue);

        /// <summary>
        /// Remove one property of the given parking sensor, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ParkingSensor  ParkingSensor,
                                                          String         PropertyName,
                                                          JToken?        OldValue   = null)

            => RemoveProperty(nameof(POI.ParkingSensor), ParkingSensor.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (GridConnectionPoint, GridOperatorId)

        /// <summary>
        /// Add the given grid connection point to the given grid operator.
        /// </summary>
        public static RoamingNetworkChange Add(GridConnectionPoint  GridConnectionPoint,
                                               GridOperator_Id      GridOperatorId)

            => Add(nameof(POI.GridConnectionPoint), GridConnectionPoint.Id.ToString(), GridConnectionPoint.ToJSON(Embedded: true), nameof(POI.GridOperator), GridOperatorId.ToString());

        #endregion

        #region Remove                (GridConnectionPointId, OldValue = null)

        /// <summary>
        /// Remove the given grid connection point, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(GridConnectionPoint_Id  GridConnectionPointId,
                                                  JToken?                 OldValue   = null)

            => Remove(nameof(POI.GridConnectionPoint), GridConnectionPointId.ToString(), OldValue);

        #endregion

        #region UpdateProperty        (GridConnectionPoint, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Update one property of the given grid connection point, optionally asserting its previous value,
        /// e.g. maintainedBy to take it over or to hand it on.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(GridConnectionPoint  GridConnectionPoint,
                                                          String               PropertyName,
                                                          JToken?              OldValue,
                                                          JToken               NewValue)

            => UpdateProperty(nameof(POI.GridConnectionPoint), GridConnectionPoint.Id.ToString(), PropertyName, OldValue, NewValue);

        #endregion

        #region RemoveProperty        (GridConnectionPoint, PropertyName, OldValue = null)

        /// <summary>
        /// Remove one property of the given grid connection point, optionally asserting its previous value,
        /// e.g. maintainedBy to be maintained by its grid operator.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(GridConnectionPoint  GridConnectionPoint,
                                                          String               PropertyName,
                                                          JToken?              OldValue   = null)

            => RemoveProperty(nameof(POI.GridConnectionPoint), GridConnectionPoint.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (EnergyMeterTemplate, ChargingStationOperatorId)

        /// <summary>
        /// Add the given energy meter template to the given charging station operator, for energy meters of its
        /// charging pools, charging stations and EVSEs.
        /// </summary>
        public static RoamingNetworkChange Add(EnergyMeterTemplate         EnergyMeterTemplate,
                                               ChargingStationOperator_Id  ChargingStationOperatorId)

            => Add(EnergyMeterTemplate, nameof(POI.ChargingStationOperator), ChargingStationOperatorId.ToString());

        #endregion

        #region Add                   (EnergyMeterTemplate, GridOperatorId)

        /// <summary>
        /// Add the given energy meter template to the given grid operator, for energy meters of its grid connection points.
        /// </summary>
        public static RoamingNetworkChange Add(EnergyMeterTemplate  EnergyMeterTemplate,
                                               GridOperator_Id      GridOperatorId)

            => Add(EnergyMeterTemplate, nameof(POI.GridOperator), GridOperatorId.ToString());

        #endregion

        #region (private) Add         (EnergyMeterTemplate, OwnerType, OwnerId)

        private static RoamingNetworkChange Add(EnergyMeterTemplate  EnergyMeterTemplate,
                                                String               OwnerType,
                                                String               OwnerId)

            => Add(nameof(POI.EnergyMeterTemplate), EnergyMeterTemplate.Id.ToString(), EnergyMeterTemplate.ToJSON(Embedded: true), OwnerType, OwnerId);

        #endregion

        #region Remove, UpdateProperty, RemoveProperty (EnergyMeterTemplate)

        /// <summary>
        /// Remove the given energy meter template, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(EnergyMeterTemplate_Id  EnergyMeterTemplateId,
                                                  JToken?                 OldValue   = null)

            => Remove(nameof(POI.EnergyMeterTemplate), EnergyMeterTemplateId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given energy meter template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(EnergyMeterTemplate     EnergyMeterTemplate,
                                                          String                  PropertyName,
                                                          JToken?                 OldValue,
                                                          JToken                  NewValue)

            => UpdateProperty(nameof(POI.EnergyMeterTemplate), EnergyMeterTemplate.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given energy meter template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(EnergyMeterTemplate     EnergyMeterTemplate,
                                                          String                  PropertyName,
                                                          JToken?                 OldValue   = null)

            => RemoveProperty(nameof(POI.EnergyMeterTemplate), EnergyMeterTemplate.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (EnergyMeterGroup, ChargingStationOperatorId)

        /// <summary>
        /// Add the given energy meter group to the given charging station operator, grouping energy meters of its
        /// charging pools, charging stations and EVSEs.
        /// </summary>
        public static RoamingNetworkChange Add(EnergyMeterGroup            EnergyMeterGroup,
                                               ChargingStationOperator_Id  ChargingStationOperatorId)

            => Add(EnergyMeterGroup, nameof(POI.ChargingStationOperator), ChargingStationOperatorId.ToString());

        #endregion

        #region Add                   (EnergyMeterGroup, GridOperatorId)

        /// <summary>
        /// Add the given energy meter group to the given grid operator, grouping energy meters of its grid connection points.
        /// </summary>
        public static RoamingNetworkChange Add(EnergyMeterGroup  EnergyMeterGroup,
                                               GridOperator_Id   GridOperatorId)

            => Add(EnergyMeterGroup, nameof(POI.GridOperator), GridOperatorId.ToString());

        #endregion

        #region (private) Add         (EnergyMeterGroup, OwnerType, OwnerId)

        private static RoamingNetworkChange Add(EnergyMeterGroup  EnergyMeterGroup,
                                                String            OwnerType,
                                                String            OwnerId)

            => Add(nameof(POI.EnergyMeterGroup), EnergyMeterGroup.Id.ToString(), EnergyMeterGroup.ToJSON(Embedded: true), OwnerType, OwnerId);

        #endregion

        #region Remove                (EnergyMeterGroupId, OldValue = null)

        /// <summary>
        /// Remove the given energy meter group from its owner, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(EnergyMeterGroup_Id  EnergyMeterGroupId,
                                                  JToken?              OldValue   = null)

            => Remove(nameof(POI.EnergyMeterGroup), EnergyMeterGroupId.ToString(), OldValue);

        #endregion

        #region UpdateProperty        (EnergyMeterGroup, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Update one property of the given energy meter group, optionally asserting its previous value,
        /// e.g. energyMeterIds, or a property of an energy meter it gives its members.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(EnergyMeterGroup  EnergyMeterGroup,
                                                          String            PropertyName,
                                                          JToken?           OldValue,
                                                          JToken            NewValue)

            => UpdateProperty(nameof(POI.EnergyMeterGroup), EnergyMeterGroup.Id.ToString(), PropertyName, OldValue, NewValue);

        #endregion

        #region RemoveProperty        (EnergyMeterGroup, PropertyName, OldValue = null)

        /// <summary>
        /// Remove one property of the given energy meter group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(EnergyMeterGroup  EnergyMeterGroup,
                                                          String            PropertyName,
                                                          JToken?           OldValue   = null)

            => RemoveProperty(nameof(POI.EnergyMeterGroup), EnergyMeterGroup.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingTariff)

        /// <summary>
        /// Add the given charging tariff to its operator.
        /// </summary>
        /// <param name="ChargingTariff">The new charging tariff.</param>
        public static RoamingNetworkChange Add(ChargingTariff ChargingTariff)

            => Add(nameof(POI.ChargingTariff),
                   ChargingTariff.Id.ToString(),
                   ChargingTariff.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given charging tariff, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingTariff_Id  ChargingTariffId,
                                                  JToken?            OldValue           = null)

            => Remove(nameof(POI.ChargingTariff),
                      ChargingTariffId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given charging tariff, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingTariff  ChargingTariff,
                                                          String          PropertyName,
                                                          JToken?         OldValue,
                                                          JToken          NewValue)

            => UpdateProperty(nameof(POI.ChargingTariff), ChargingTariff.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging tariff, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingTariff  ChargingTariff,
                                                          String          PropertyName,
                                                          JToken?         OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingTariff), ChargingTariff.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (TransparencySoftwareManufacturer)

        /// <summary>
        /// Add the given transparency software manufacturer to its roaming network, with its keys and without its releases.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturer">The new transparency software manufacturer.</param>
        public static RoamingNetworkChange Add(TransparencySoftwareManufacturer TransparencySoftwareManufacturer)

            => Add(nameof(POI.TransparencySoftwareManufacturer),
                   TransparencySoftwareManufacturer.Id.ToString(),
                   TransparencySoftwareManufacturer.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given transparency software manufacturer, with its releases, optionally checking its complete
        /// previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(TransparencySoftwareManufacturer_Id  TransparencySoftwareManufacturerId,
                                                  JToken?                              OldValue                             = null)

            => Remove(nameof(POI.TransparencySoftwareManufacturer),
                      TransparencySoftwareManufacturerId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given transparency software manufacturer, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(TransparencySoftwareManufacturer  TransparencySoftwareManufacturer,
                                                          String                            PropertyName,
                                                          JToken?                           OldValue,
                                                          JToken                            NewValue)

            => UpdateProperty(nameof(POI.TransparencySoftwareManufacturer), TransparencySoftwareManufacturer.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given transparency software manufacturer, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(TransparencySoftwareManufacturer  TransparencySoftwareManufacturer,
                                                          String                            PropertyName,
                                                          JToken?                           OldValue   = null)

            => RemoveProperty(nameof(POI.TransparencySoftwareManufacturer), TransparencySoftwareManufacturer.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (TransparencySoftware, TransparencySoftwareManufacturerId)

        /// <summary>
        /// Add the given transparency software release to its manufacturer.
        /// </summary>
        /// <param name="TransparencySoftware">The new transparency software release.</param>
        /// <param name="TransparencySoftwareManufacturerId">The identification of its manufacturer.</param>
        public static RoamingNetworkChange Add(TransparencySoftware                 TransparencySoftware,
                                               TransparencySoftwareManufacturer_Id  TransparencySoftwareManufacturerId)

            => Add(nameof(POI.TransparencySoftware),
                   TransparencySoftware.Id.ToString(),
                   TransparencySoftware.ToJSON(Embedded: true),
                   nameof(POI.TransparencySoftwareManufacturer),
                   TransparencySoftwareManufacturerId.ToString());

        /// <summary>
        /// Remove the given transparency software release, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(TransparencySoftware_Id  TransparencySoftwareId,
                                                  JToken?                  OldValue                 = null)

            => Remove(nameof(POI.TransparencySoftware),
                      TransparencySoftwareId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given transparency software release, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(TransparencySoftware  TransparencySoftware,
                                                          String                PropertyName,
                                                          JToken?               OldValue,
                                                          JToken                NewValue)

            => UpdateProperty(nameof(POI.TransparencySoftware), TransparencySoftware.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given transparency software release, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(TransparencySoftware  TransparencySoftware,
                                                          String                PropertyName,
                                                          JToken?               OldValue   = null)

            => RemoveProperty(nameof(POI.TransparencySoftware), TransparencySoftware.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ConformityAssessmentBody)

        /// <summary>
        /// Add the given conformity assessment body to its roaming network, with its keys, if it takes part, and without its
        /// certificates.
        /// </summary>
        /// <param name="ConformityAssessmentBody">The new conformity assessment body.</param>
        public static RoamingNetworkChange Add(ConformityAssessmentBody ConformityAssessmentBody)

            => Add(nameof(POI.ConformityAssessmentBody),
                   ConformityAssessmentBody.Id.ToString(),
                   ConformityAssessmentBody.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given conformity assessment body, with its certificates, optionally checking its complete previous
        /// JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ConformityAssessmentBody_Id  ConformityAssessmentBodyId,
                                                  JToken?                      OldValue                     = null)

            => Remove(nameof(POI.ConformityAssessmentBody),
                      ConformityAssessmentBodyId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given conformity assessment body, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ConformityAssessmentBody  ConformityAssessmentBody,
                                                          String                    PropertyName,
                                                          JToken?                   OldValue,
                                                          JToken                    NewValue)

            => UpdateProperty(nameof(POI.ConformityAssessmentBody), ConformityAssessmentBody.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given conformity assessment body, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ConformityAssessmentBody  ConformityAssessmentBody,
                                                          String                    PropertyName,
                                                          JToken?                   OldValue   = null)

            => RemoveProperty(nameof(POI.ConformityAssessmentBody), ConformityAssessmentBody.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (TransparencySoftwareCertificate, ConformityAssessmentBodyId)

        /// <summary>
        /// Add the given transparency software certificate to its roaming network.
        /// </summary>
        /// <param name="TransparencySoftwareCertificate">The new transparency software certificate.</param>
        /// <param name="ConformityAssessmentBodyId">The identification of the conformity assessment body issuing it.</param>
        public static RoamingNetworkChange Add(TransparencySoftwareCertificate  TransparencySoftwareCertificate,
                                               ConformityAssessmentBody_Id      ConformityAssessmentBodyId)

            => Add(nameof(POI.TransparencySoftwareCertificate),
                   TransparencySoftwareCertificate.Id.ToString(),
                   TransparencySoftwareCertificate.ToJSON(Embedded: true),
                   nameof(POI.ConformityAssessmentBody),
                   ConformityAssessmentBodyId.ToString());

        /// <summary>
        /// Remove the given transparency software certificate, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(TransparencySoftwareCertificate_Id  TransparencySoftwareCertificateId,
                                                  JToken?                             OldValue                            = null)

            => Remove(nameof(POI.TransparencySoftwareCertificate),
                      TransparencySoftwareCertificateId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given transparency software certificate, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(TransparencySoftwareCertificate  TransparencySoftwareCertificate,
                                                          String                           PropertyName,
                                                          JToken?                          OldValue,
                                                          JToken                           NewValue)

            => UpdateProperty(nameof(POI.TransparencySoftwareCertificate), TransparencySoftwareCertificate.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given transparency software certificate, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(TransparencySoftwareCertificate  TransparencySoftwareCertificate,
                                                          String                           PropertyName,
                                                          JToken?                          OldValue   = null)

            => RemoveProperty(nameof(POI.TransparencySoftwareCertificate), TransparencySoftwareCertificate.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (EVSETemplate)

        /// <summary>
        /// Add the given EVSE template to its operator.
        /// </summary>
        /// <param name="EVSETemplate">The new EVSE template.</param>
        public static RoamingNetworkChange Add(EVSETemplate EVSETemplate)

            => Add(nameof(POI.EVSETemplate),
                   EVSETemplate.Id.ToString(),
                   EVSETemplate.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given EVSE template, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(EVSETemplate_Id  EVSETemplateId,
                                                  JToken?          OldValue   = null)

            => Remove(nameof(POI.EVSETemplate), EVSETemplateId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given EVSE template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(EVSETemplate     EVSETemplate,
                                                          String           PropertyName,
                                                          JToken?          OldValue,
                                                          JToken           NewValue)

            => UpdateProperty(nameof(POI.EVSETemplate), EVSETemplate.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given EVSE template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(EVSETemplate     EVSETemplate,
                                                          String           PropertyName,
                                                          JToken?          OldValue   = null)

            => RemoveProperty(nameof(POI.EVSETemplate), EVSETemplate.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingStationTemplate)

        /// <summary>
        /// Add the given charging station template to its operator.
        /// </summary>
        /// <param name="ChargingStationTemplate">The new charging station template.</param>
        public static RoamingNetworkChange Add(ChargingStationTemplate ChargingStationTemplate)

            => Add(nameof(POI.ChargingStationTemplate),
                   ChargingStationTemplate.Id.ToString(),
                   ChargingStationTemplate.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given charging station template, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingStationTemplate_Id  ChargingStationTemplateId,
                                                  JToken?                     OldValue   = null)

            => Remove(nameof(POI.ChargingStationTemplate), ChargingStationTemplateId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given charging station template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingStationTemplate     ChargingStationTemplate,
                                                          String                      PropertyName,
                                                          JToken?                     OldValue,
                                                          JToken                      NewValue)

            => UpdateProperty(nameof(POI.ChargingStationTemplate), ChargingStationTemplate.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging station template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingStationTemplate     ChargingStationTemplate,
                                                          String                      PropertyName,
                                                          JToken?                     OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingStationTemplate), ChargingStationTemplate.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingPoolTemplate)

        /// <summary>
        /// Add the given charging pool template to its operator.
        /// </summary>
        /// <param name="ChargingPoolTemplate">The new charging pool template.</param>
        public static RoamingNetworkChange Add(ChargingPoolTemplate ChargingPoolTemplate)

            => Add(nameof(POI.ChargingPoolTemplate),
                   ChargingPoolTemplate.Id.ToString(),
                   ChargingPoolTemplate.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given charging pool template, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingPoolTemplate_Id  ChargingPoolTemplateId,
                                                  JToken?                  OldValue   = null)

            => Remove(nameof(POI.ChargingPoolTemplate), ChargingPoolTemplateId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given charging pool template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingPoolTemplate     ChargingPoolTemplate,
                                                          String                   PropertyName,
                                                          JToken?                  OldValue,
                                                          JToken                   NewValue)

            => UpdateProperty(nameof(POI.ChargingPoolTemplate), ChargingPoolTemplate.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging pool template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingPoolTemplate     ChargingPoolTemplate,
                                                          String                   PropertyName,
                                                          JToken?                  OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingPoolTemplate), ChargingPoolTemplate.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingTariffTemplate)

        /// <summary>
        /// Add the given charging tariff template to its operator.
        /// </summary>
        /// <param name="ChargingTariffTemplate">The new charging tariff template.</param>
        public static RoamingNetworkChange Add(ChargingTariffTemplate ChargingTariffTemplate)

            => Add(nameof(POI.ChargingTariffTemplate),
                   ChargingTariffTemplate.Id.ToString(),
                   ChargingTariffTemplate.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given charging tariff template, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingTariffTemplate_Id  ChargingTariffTemplateId,
                                                  JToken?                    OldValue   = null)

            => Remove(nameof(POI.ChargingTariffTemplate), ChargingTariffTemplateId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given charging tariff template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingTariffTemplate     ChargingTariffTemplate,
                                                          String                     PropertyName,
                                                          JToken?                    OldValue,
                                                          JToken                     NewValue)

            => UpdateProperty(nameof(POI.ChargingTariffTemplate), ChargingTariffTemplate.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging tariff template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingTariffTemplate     ChargingTariffTemplate,
                                                          String                     PropertyName,
                                                          JToken?                    OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingTariffTemplate), ChargingTariffTemplate.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingTariffOffer, ChargingStationOperatorId / EMobilityProviderId)

        /// <summary>
        /// Add the given charging tariff offer to the given charging station operator, at its own EVSEs.
        /// </summary>
        public static RoamingNetworkChange Add(ChargingTariffOffer         ChargingTariffOffer,
                                               ChargingStationOperator_Id  ChargingStationOperatorId)

            => Add(nameof(POI.ChargingTariffOffer), ChargingTariffOffer.Id.ToString(), ChargingTariffOffer.ToJSON(Embedded: true), nameof(POI.ChargingStationOperator), ChargingStationOperatorId.ToString());

        /// <summary>
        /// Add the given charging tariff offer to the given e-mobility provider, for its customers at the EVSEs of any operator.
        /// </summary>
        public static RoamingNetworkChange Add(ChargingTariffOffer   ChargingTariffOffer,
                                               EMobilityProvider_Id  EMobilityProviderId)

            => Add(nameof(POI.ChargingTariffOffer), ChargingTariffOffer.Id.ToString(), ChargingTariffOffer.ToJSON(Embedded: true), nameof(POI.EMobilityProvider), EMobilityProviderId.ToString());

        /// <summary>
        /// Remove the given charging tariff offer from its owner, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingTariffOffer_Id  ChargingTariffOfferId,
                                                  JToken?                 OldValue   = null)

            => Remove(nameof(POI.ChargingTariffOffer), ChargingTariffOfferId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given charging tariff offer, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingTariffOffer  ChargingTariffOffer,
                                                          String               PropertyName,
                                                          JToken?              OldValue,
                                                          JToken               NewValue)

            => UpdateProperty(nameof(POI.ChargingTariffOffer), ChargingTariffOffer.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging tariff offer, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingTariffOffer  ChargingTariffOffer,
                                                          String               PropertyName,
                                                          JToken?              OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingTariffOffer), ChargingTariffOffer.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (EVSEGroup / ChargingStationGroup / ChargingPoolGroup, EMobilityProviderId)

        /// <summary>
        /// Add the given EVSE group to the given e-mobility provider, naming EVSEs of any operator.
        /// </summary>
        public static RoamingNetworkChange Add(EVSEGroup             EVSEGroup,
                                               EMobilityProvider_Id  EMobilityProviderId)

            => Add(nameof(POI.EVSEGroup), EVSEGroup.Id.ToString(), EVSEGroup.ToJSON(Embedded: true), nameof(POI.EMobilityProvider), EMobilityProviderId.ToString());

        /// <summary>
        /// Add the given charging station group to the given e-mobility provider, naming charging stations of any operator.
        /// </summary>
        public static RoamingNetworkChange Add(ChargingStationGroup  ChargingStationGroup,
                                               EMobilityProvider_Id  EMobilityProviderId)

            => Add(nameof(POI.ChargingStationGroup), ChargingStationGroup.Id.ToString(), ChargingStationGroup.ToJSON(Embedded: true), nameof(POI.EMobilityProvider), EMobilityProviderId.ToString());

        /// <summary>
        /// Add the given charging pool group to the given e-mobility provider, naming charging pools of any operator.
        /// </summary>
        public static RoamingNetworkChange Add(ChargingPoolGroup     ChargingPoolGroup,
                                               EMobilityProvider_Id  EMobilityProviderId)

            => Add(nameof(POI.ChargingPoolGroup), ChargingPoolGroup.Id.ToString(), ChargingPoolGroup.ToJSON(Embedded: true), nameof(POI.EMobilityProvider), EMobilityProviderId.ToString());

        #endregion

        #region Add                   (EVSEGroup)

        /// <summary>
        /// Add the given EVSE group to its operator.
        /// </summary>
        /// <param name="EVSEGroup">The new EVSE group.</param>
        public static RoamingNetworkChange Add(EVSEGroup EVSEGroup)

            => Add(nameof(POI.EVSEGroup),
                   EVSEGroup.Id.ToString(),
                   EVSEGroup.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given EVSE group, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(EVSEGroup_Id  EVSEGroupId,
                                                  JToken?       OldValue      = null)

            => Remove(nameof(POI.EVSEGroup),
                      EVSEGroupId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given EVSE group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(EVSEGroup  EVSEGroup,
                                                          String     PropertyName,
                                                          JToken?    OldValue,
                                                          JToken     NewValue)

            => UpdateProperty(nameof(POI.EVSEGroup), EVSEGroup.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given EVSE group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(EVSEGroup  EVSEGroup,
                                                          String     PropertyName,
                                                          JToken?    OldValue   = null)

            => RemoveProperty(nameof(POI.EVSEGroup), EVSEGroup.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingStationGroup)

        /// <summary>
        /// Add the given charging station group to its operator.
        /// </summary>
        /// <param name="ChargingStationGroup">The new charging station group.</param>
        public static RoamingNetworkChange Add(ChargingStationGroup ChargingStationGroup)

            => Add(nameof(POI.ChargingStationGroup),
                   ChargingStationGroup.Id.ToString(),
                   ChargingStationGroup.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given charging station group, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingStationGroup_Id  ChargingStationGroupId,
                                                  JToken?                  OldValue                 = null)

            => Remove(nameof(POI.ChargingStationGroup),
                      ChargingStationGroupId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given charging station group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingStationGroup  ChargingStationGroup,
                                                          String                PropertyName,
                                                          JToken?               OldValue,
                                                          JToken                NewValue)

            => UpdateProperty(nameof(POI.ChargingStationGroup), ChargingStationGroup.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging station group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingStationGroup  ChargingStationGroup,
                                                          String                PropertyName,
                                                          JToken?               OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingStationGroup), ChargingStationGroup.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingPoolGroup)

        /// <summary>
        /// Add the given charging pool group to its operator.
        /// </summary>
        /// <param name="ChargingPoolGroup">The new charging pool group.</param>
        public static RoamingNetworkChange Add(ChargingPoolGroup ChargingPoolGroup)

            => Add(nameof(POI.ChargingPoolGroup),
                   ChargingPoolGroup.Id.ToString(),
                   ChargingPoolGroup.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given charging pool group, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingPoolGroup_Id  ChargingPoolGroupId,
                                                  JToken?               OldValue              = null)

            => Remove(nameof(POI.ChargingPoolGroup),
                      ChargingPoolGroupId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given charging pool group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingPoolGroup  ChargingPoolGroup,
                                                          String             PropertyName,
                                                          JToken?            OldValue,
                                                          JToken             NewValue)

            => UpdateProperty(nameof(POI.ChargingPoolGroup), ChargingPoolGroup.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging pool group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingPoolGroup  ChargingPoolGroup,
                                                          String             PropertyName,
                                                          JToken?            OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingPoolGroup), ChargingPoolGroup.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingTariffGroup)

        /// <summary>
        /// Add the given charging tariff group to its operator.
        /// </summary>
        /// <param name="ChargingTariffGroup">The new charging tariff group.</param>
        public static RoamingNetworkChange Add(ChargingTariffGroup ChargingTariffGroup)

            => Add(nameof(POI.ChargingTariffGroup),
                   ChargingTariffGroup.Id.ToString(),
                   ChargingTariffGroup.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given charging tariff group, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingTariffGroup_Id  ChargingTariffGroupId,
                                                  JToken?                 OldValue                = null)

            => Remove(nameof(POI.ChargingTariffGroup),
                      ChargingTariffGroupId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given charging tariff group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingTariffGroup  ChargingTariffGroup,
                                                          String               PropertyName,
                                                          JToken?              OldValue,
                                                          JToken               NewValue)

            => UpdateProperty(nameof(POI.ChargingTariffGroup), ChargingTariffGroup.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given charging tariff group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingTariffGroup  ChargingTariffGroup,
                                                          String               PropertyName,
                                                          JToken?              OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingTariffGroup), ChargingTariffGroup.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ParkingSpaceTemplate, ParkingOperatorId)

        /// <summary>
        /// Add the given parking space template to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSpaceTemplate  ParkingSpaceTemplate,
                                               ParkingOperator_Id    ParkingOperatorId)

            => Add(nameof(POI.ParkingSpaceTemplate), ParkingSpaceTemplate.Id.ToString(), ParkingSpaceTemplate.ToJSON(Embedded: true), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

        /// <summary>
        /// Remove the given parking space template, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ParkingSpaceTemplate_Id  ParkingSpaceTemplateId,
                                                  JToken?                  OldValue   = null)

            => Remove(nameof(POI.ParkingSpaceTemplate), ParkingSpaceTemplateId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given parking space template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ParkingSpaceTemplate     ParkingSpaceTemplate,
                                                          String                   PropertyName,
                                                          JToken?                  OldValue,
                                                          JToken                   NewValue)

            => UpdateProperty(nameof(POI.ParkingSpaceTemplate), ParkingSpaceTemplate.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given parking space template, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ParkingSpaceTemplate     ParkingSpaceTemplate,
                                                          String                   PropertyName,
                                                          JToken?                  OldValue   = null)

            => RemoveProperty(nameof(POI.ParkingSpaceTemplate), ParkingSpaceTemplate.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ParkingSpaceGroup, ParkingOperatorId)

        /// <summary>
        /// Add the given parking space group to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSpaceGroup   ParkingSpaceGroup,
                                               ParkingOperator_Id  ParkingOperatorId)

            => Add(nameof(POI.ParkingSpaceGroup), ParkingSpaceGroup.Id.ToString(), ParkingSpaceGroup.ToJSON(Embedded: true), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

        /// <summary>
        /// Remove the given parking space group, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ParkingSpaceGroup_Id  ParkingSpaceGroupId,
                                                  JToken?               OldValue   = null)

            => Remove(nameof(POI.ParkingSpaceGroup), ParkingSpaceGroupId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given parking space group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ParkingSpaceGroup  ParkingSpaceGroup,
                                                          String             PropertyName,
                                                          JToken?            OldValue,
                                                          JToken             NewValue)

            => UpdateProperty(nameof(POI.ParkingSpaceGroup), ParkingSpaceGroup.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given parking space group, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ParkingSpaceGroup  ParkingSpaceGroup,
                                                          String             PropertyName,
                                                          JToken?            OldValue   = null)

            => RemoveProperty(nameof(POI.ParkingSpaceGroup), ParkingSpaceGroup.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (EnergyMeterManufacturer)

        /// <summary>
        /// Add the given energy meter manufacturer to its roaming network.
        /// </summary>
        /// <param name="EnergyMeterManufacturer">The new energy meter manufacturer.</param>
        public static RoamingNetworkChange Add(EnergyMeterManufacturer EnergyMeterManufacturer)

            => Add(nameof(POI.EnergyMeterManufacturer),
                   EnergyMeterManufacturer.Id.ToString(),
                   EnergyMeterManufacturer.ToJSON(Embedded: true));

        /// <summary>
        /// Remove the given energy meter manufacturer, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(EnergyMeterManufacturer_Id  EnergyMeterManufacturerId,
                                                  JToken?                     OldValue   = null)

            => Remove(nameof(POI.EnergyMeterManufacturer),
                      EnergyMeterManufacturerId.ToString(),
                      OldValue);

        /// <summary>
        /// Update one property of the given energy meter manufacturer, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(EnergyMeterManufacturer  EnergyMeterManufacturer,
                                                          String                   PropertyName,
                                                          JToken?                  OldValue,
                                                          JToken                   NewValue)

            => UpdateProperty(nameof(POI.EnergyMeterManufacturer), EnergyMeterManufacturer.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given energy meter manufacturer, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(EnergyMeterManufacturer  EnergyMeterManufacturer,
                                                          String                   PropertyName,
                                                          JToken?                  OldValue   = null)

            => RemoveProperty(nameof(POI.EnergyMeterManufacturer), EnergyMeterManufacturer.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ParkingProduct, ParkingOperatorId)

        /// <summary>
        /// Add the given parking product to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingProduct   ParkingProduct,
                                               ParkingOperator_Id  ParkingOperatorId)

            => Add(nameof(POI.ParkingProduct), ParkingProduct.Id.ToString(), ParkingProduct.ToJSON(Embedded: true), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

        /// <summary>
        /// Remove the given parking product, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ParkingProduct_Id  ParkingProductId,
                                                  JToken?            OldValue   = null)

            => Remove(nameof(POI.ParkingProduct), ParkingProductId.ToString(), OldValue);

        /// <summary>
        /// Update one property of the given parking product, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ParkingProduct  ParkingProduct,
                                                          String             PropertyName,
                                                          JToken?            OldValue,
                                                          JToken             NewValue)

            => UpdateProperty(nameof(POI.ParkingProduct), ParkingProduct.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Remove one property of the given parking product, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ParkingProduct  ParkingProduct,
                                                          String             PropertyName,
                                                          JToken?            OldValue   = null)

            => RemoveProperty(nameof(POI.ParkingProduct), ParkingProduct.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region Add                   (ChargingPool)

        /// <summary>
        /// Add the given charging pool, with its charging stations and EVSEs, to its charging station operator.
        /// </summary>
        /// <param name="ChargingPool">The new charging pool.</param>
        public static RoamingNetworkChange Add(ChargingPool ChargingPool)

            => Add(nameof(POI.ChargingPool),
                   ChargingPool.Id.ToString(),
                   ChargingPool.ToJSON(Embedded: true),
                   nameof(POI.ChargingStationOperator),
                   ChargingPool.Id.OperatorId.ToString());

        #endregion

        #region Add                   (ChargingStation, ChargingPoolId)

        /// <summary>
        /// Add the given charging station, with its EVSEs, to the given charging pool.
        /// </summary>
        /// <param name="ChargingStation">The new charging station.</param>
        /// <param name="ChargingPoolId">The identification of its charging pool.</param>
        public static RoamingNetworkChange Add(ChargingStation  ChargingStation,
                                               ChargingPool_Id  ChargingPoolId)

            => Add(nameof(POI.ChargingStation),
                   ChargingStation.Id.ToString(),
                   ChargingStation.ToJSON(Embedded: true),
                   nameof(POI.ChargingPool),
                   ChargingPoolId.ToString());

        #endregion

        #region Add                   (EVSE, ChargingStationId)

        /// <summary>
        /// Add the given EVSE to the given charging station.
        /// </summary>
        /// <param name="EVSE">The new EVSE.</param>
        /// <param name="ChargingStationId">The identification of its charging station.</param>
        public static RoamingNetworkChange Add(EVSE                EVSE,
                                               ChargingStation_Id  ChargingStationId)

            => Add(nameof(POI.EVSE),
                   EVSE.Id.ToString(),
                   EVSE.ToJSON(Embedded: true),
                   nameof(POI.ChargingStation),
                   ChargingStationId.ToString());

        #endregion

        #region Remove                (ChargingPoolId | ChargingStationId | EVSEId, OldValue = null)

        /// <summary>
        /// Remove the given charging pool, with its charging stations and EVSEs,
        /// optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingPool_Id  ChargingPoolId,
                                                  JToken?          OldValue   = null)

            => Remove(nameof(POI.ChargingPool),
                      ChargingPoolId.ToString(),
                      OldValue);

        /// <summary>
        /// Remove the given charging station, with its EVSEs,
        /// optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(ChargingStation_Id  ChargingStationId,
                                                  JToken?             OldValue   = null)

            => Remove(nameof(POI.ChargingStation),
                      ChargingStationId.ToString(),
                      OldValue);

        /// <summary>
        /// Remove the given EVSE, optionally checking its complete previous JSON representation.
        /// </summary>
        public static RoamingNetworkChange Remove(EVSE_Id  EVSEId,
                                                  JToken?  OldValue   = null)

            => Remove(nameof(POI.EVSE),
                      EVSEId.ToString(),
                      OldValue);

        #endregion

        #region UpdateProperty        (ChargingPool | ChargingStation | EVSE, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Update one property of the given charging pool, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingPool  ChargingPool,
                                                          String        PropertyName,
                                                          JToken?       OldValue,
                                                          JToken        NewValue)

            => UpdateProperty(nameof(POI.ChargingPool), ChargingPool.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Update one property of the given charging station, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(ChargingStation  ChargingStation,
                                                          String           PropertyName,
                                                          JToken?          OldValue,
                                                          JToken           NewValue)

            => UpdateProperty(nameof(POI.ChargingStation), ChargingStation.Id.ToString(), PropertyName, OldValue, NewValue);

        /// <summary>
        /// Update one property of the given EVSE, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange UpdateProperty(EVSE     EVSE,
                                                          String   PropertyName,
                                                          JToken?  OldValue,
                                                          JToken   NewValue)

            => UpdateProperty(nameof(POI.EVSE), EVSE.Id.ToString(), PropertyName, OldValue, NewValue);

        #endregion

        #region RemoveProperty        (ChargingPool | ChargingStation | EVSE, PropertyName, OldValue = null)

        /// <summary>
        /// Remove one property of the given charging pool, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingPool  ChargingPool,
                                                          String        PropertyName,
                                                          JToken?       OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingPool), ChargingPool.Id.ToString(), PropertyName, OldValue);

        /// <summary>
        /// Remove one property of the given charging station, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(ChargingStation  ChargingStation,
                                                          String           PropertyName,
                                                          JToken?          OldValue   = null)

            => RemoveProperty(nameof(POI.ChargingStation), ChargingStation.Id.ToString(), PropertyName, OldValue);

        /// <summary>
        /// Remove one property of the given EVSE, optionally asserting its previous value.
        /// </summary>
        public static RoamingNetworkChange RemoveProperty(EVSE     EVSE,
                                                          String   PropertyName,
                                                          JToken?  OldValue   = null)

            => RemoveProperty(nameof(POI.EVSE), EVSE.Id.ToString(), PropertyName, OldValue);

        #endregion

        #region AddElement            (EntityType, EntityId, ElementPath, Value, ...)

        /// <summary>
        /// Add one identified collection element or an absent optional singleton property.
        /// </summary>
        public static RoamingNetworkChange AddElement(String                                 EntityType,
                                                      String                                 EntityId,
                                                      ImmutableArray<POIElementPathSegment>  ElementPath,
                                                      JToken                                 Value,
                                                      String?                                ParentEntityType   = null,
                                                      String?                                ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.AddElement,
                    EntityType,
                    EntityId,
                    null,
                    null,
                    Value,
                    ParentEntityType,
                    ParentEntityId,
                    ElementPath);

        #endregion

        #region RemoveElement         (EntityType, EntityId, ElementPath, OldValue = null, ...)

        /// <summary>
        /// Remove one collection element or optional singleton, with an optional complete precondition.
        /// </summary>
        public static RoamingNetworkChange RemoveElement(String                                 EntityType,
                                                         String                                 EntityId,
                                                         ImmutableArray<POIElementPathSegment>  ElementPath,
                                                         JToken?                                OldValue           = null,
                                                         String?                                ParentEntityType   = null,
                                                         String?                                ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.RemoveElement,
                    EntityType,
                    EntityId,
                    null,
                    OldValue,
                    null,
                    ParentEntityType,
                    ParentEntityId,
                    ElementPath);

        #endregion

        #region ReplaceElement        (EntityType, EntityId, ElementPath, OldValue, NewValue, ...)

        /// <summary>
        /// Replace one existing collection element or singleton, with an optional complete precondition.
        /// </summary>
        public static RoamingNetworkChange ReplaceElement(String                                 EntityType,
                                                          String                                 EntityId,
                                                          ImmutableArray<POIElementPathSegment>  ElementPath,
                                                          JToken?                                OldValue,
                                                          JToken                                 NewValue,
                                                          String?                                ParentEntityType   = null,
                                                          String?                                ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.ReplaceElement,
                    EntityType,
                    EntityId,
                    null,
                    OldValue,
                    NewValue,
                    ParentEntityType,
                    ParentEntityId,
                    ElementPath);

        #endregion

        #region UpdateElementProperty (EntityType, EntityId, ElementPath, PropertyName, OldValue, NewValue, ...)

        /// <summary>
        /// Update one static property of an existing nested object while retaining its other properties.
        /// </summary>
        public static RoamingNetworkChange UpdateElementProperty(String                                 EntityType,
                                                                 String                                 EntityId,
                                                                 ImmutableArray<POIElementPathSegment>  ElementPath,
                                                                 String                                 PropertyName,
                                                                 JToken?                                OldValue,
                                                                 JToken                                 NewValue,
                                                                 String?                                ParentEntityType   = null,
                                                                 String?                                ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.UpdateElementProperty,
                    EntityType,
                    EntityId,
                    PropertyName,
                    OldValue,
                    NewValue,
                    ParentEntityType,
                    ParentEntityId,
                    ElementPath);

        #endregion

        #region RemoveElementProperty (EntityType, EntityId, ElementPath, PropertyName, OldValue = null, ...)

        /// <summary>
        /// Remove an existing optional element property without clearing the element's runtime lifetime.
        /// </summary>
        public static RoamingNetworkChange RemoveElementProperty(String                                 EntityType,
                                                                 String                                 EntityId,
                                                                 ImmutableArray<POIElementPathSegment>  ElementPath,
                                                                 String                                 PropertyName,
                                                                 JToken?                                OldValue           = null,
                                                                 String?                                ParentEntityType   = null,
                                                                 String?                                ParentEntityId     = null)

            => new (RoamingNetworkChangeKind.RemoveElementProperty,
                    EntityType,
                    EntityId,
                    PropertyName,
                    OldValue,
                    null,
                    ParentEntityType,
                    ParentEntityId,
                    ElementPath);

        #endregion


        #region Parse    (JSON)

        /// <summary>
        /// Parse the given JSON representation of a roaming network change.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        public static RoamingNetworkChange Parse(JObject JSON)
        {

            if (TryParse(JSON,
                         out var roamingNetworkChange,
                         out var errorResponse))
            {
                return roamingNetworkChange;
            }

            throw new ArgumentException("The given JSON representation of a roaming network change is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region TryParse (JSON, out RoamingNetworkChange, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a roaming network change.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="RoamingNetworkChange">The parsed roaming network change.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkChange?  RoamingNetworkChange,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            RoamingNetworkChange = null;

            try
            {

                if (JSON is null)
                {
                    ErrorResponse = "The given JSON object must not be null!";
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the roaming network change must be '{JSONLDContext}'!";
                    return false;
                }

                #region Parse Kind                [mandatory]

                if (!JSON.ParseMandatoryText(PropertyKeys.Kind,
                                             "change kind",
                                             out var kindText,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (!Enum.TryParse<RoamingNetworkChangeKind>(kindText, ignoreCase: false, out var kind) ||
                    !Enum.IsDefined(kind)                                                              ||
                    kind.ToString() != kindText)
                {
                    ErrorResponse = $"Unknown change kind '{kindText}'!";
                    return false;
                }

                #endregion

                #region Parse EntityType          [mandatory]

                if (!JSON.ParseMandatoryText(PropertyKeys.EntityType,
                                             "entity type",
                                             out var entityType,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse EntityId            [mandatory]

                if (!JSON.ParseMandatoryText(PropertyKeys.EntityId,
                                             "entity identification",
                                             out var entityId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse PropertyName        [optional]

                if (JSON.ParseOptional(PropertyKeys.PropertyName,
                                       "property name",
                                       out String? propertyName,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse ParentEntityType    [optional]

                if (JSON.ParseOptional(PropertyKeys.ParentEntityType,
                                       "parent entity type",
                                       out String? parentEntityType,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse ParentEntityId      [optional]

                if (JSON.ParseOptional(PropertyKeys.ParentEntityId,
                                       "parent entity identification",
                                       out String? parentEntityId,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse ElementPath         [optional]

                var elementPath = ImmutableArray<POIElementPathSegment>.Empty;

                if (JSON[PropertyKeys.ElementPath] is JToken elementPathJSON)
                {

                    if (elementPathJSON is not JArray elementPathArray)
                    {
                        ErrorResponse = "The element path must be a JSON array!";
                        return false;
                    }

                    foreach (var segmentJSON in elementPathArray)
                    {

                        if (segmentJSON is not JObject segmentObject)
                        {
                            ErrorResponse = "An element path segment must be a JSON object!";
                            return false;
                        }

                        if (!POIElementPathSegment.TryParse(segmentObject, out var segment, out var segmentError))
                        {
                            ErrorResponse = "Invalid element path segment: " + segmentError;
                            return false;
                        }

                        elementPath = elementPath.Add(segment);

                    }

                }

                #endregion

                #region Parse OldValue/NewValue   [optional]

                var oldValue = JSON[PropertyKeys.OldValue];
                var newValue = JSON[PropertyKeys.NewValue];

                #endregion


                RoamingNetworkChange = new RoamingNetworkChange(
                                           kind,
                                           entityType,
                                           entityId,
                                           propertyName,
                                           oldValue,
                                           newValue,
                                           parentEntityType,
                                           parentEntityId,
                                           elementPath
                                       );

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                RoamingNetworkChange  = null;
                ErrorResponse         = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomRoamingNetworkChangeSerializer = null)

        /// <summary>
        /// Return a JSON representation of this roaming network change.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomRoamingNetworkChangeSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                                 Embedded                               = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkChange>?  CustomRoamingNetworkChangeSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,            JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Kind,               Kind.ToString()),
                                 new JProperty(PropertyKeys.EntityType,         EntityType),
                                 new JProperty(PropertyKeys.EntityId,           EntityId),

                           PropertyName is not null
                               ? new JProperty(PropertyKeys.PropertyName,       PropertyName)
                               : null,

                           ParentEntityType is not null
                               ? new JProperty(PropertyKeys.ParentEntityType,   ParentEntityType)
                               : null,

                           ParentEntityId is not null
                               ? new JProperty(PropertyKeys.ParentEntityId,     ParentEntityId)
                               : null,

                           ElementPath.Length > 0
                               ? new JProperty(PropertyKeys.ElementPath,        new JArray(ElementPath.Select(segment => segment.ToJSON(Embedded: true))))
                               : null

                       );

            // Added directly: JSONObject.Create leaves out JSON null and empty values,
            // but an expected absent old value (null) or an empty new value ([], {}, "") is a value here.
            if (oldValue is not null)
                json.Add(PropertyKeys.OldValue, oldValue.DeepClone());

            if (newValue is not null)
                json.Add(PropertyKeys.NewValue, newValue.DeepClone());

            return CustomRoamingNetworkChangeSerializer is not null
                       ? CustomRoamingNetworkChangeSerializer(this, json)
                       : json;

        }

        #endregion


        #region Equals(RoamingNetworkChange)

        /// <summary>
        /// Compares two roaming network changes for equality, their JSON values by their content.
        /// </summary>
        /// <param name="RoamingNetworkChange">A roaming network change to compare with.</param>
        public Boolean Equals(RoamingNetworkChange? RoamingNetworkChange)

            => RoamingNetworkChange is not null &&

               Kind             == RoamingNetworkChange.Kind             &&
               EntityType       == RoamingNetworkChange.EntityType       &&
               EntityId         == RoamingNetworkChange.EntityId         &&
               PropertyName     == RoamingNetworkChange.PropertyName     &&
               ParentEntityType == RoamingNetworkChange.ParentEntityType &&
               ParentEntityId   == RoamingNetworkChange.ParentEntityId   &&

               ElementPath.SequenceEqual(RoamingNetworkChange.ElementPath) &&

               POIJSON.SameValue(oldValue, RoamingNetworkChange.oldValue) &&
               POIJSON.SameValue(newValue, RoamingNetworkChange.newValue);

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => HashCode.Combine(Kind,
                                EntityType,
                                EntityId,
                                PropertyName,
                                ParentEntityType,
                                ParentEntityId,
                                ElementPath.Length);

        #endregion

        #region ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Kind} {EntityType} '{EntityId}'{(PropertyName is not null ? $" .{PropertyName}" : "")}";

        #endregion


        #region (internal) WithoutOldValue()

        /// <summary>
        /// This change without its old value: applying it gives it the value it finds.
        /// </summary>
        internal RoamingNetworkChange WithoutOldValue()

            => new (Kind,
                    EntityType,
                    EntityId,
                    PropertyName,
                    null,
                    newValue,
                    ParentEntityType,
                    ParentEntityId,
                    ElementPath);

        #endregion


        #region (private static) Required(Value, ParameterName)

        private static String Required(String  Value,
                                       String  ParameterName)

            => !String.IsNullOrWhiteSpace(Value)
                   ? Value
                   : throw new ArgumentException("Value must not be empty.", ParameterName);

        #endregion

    }

}
