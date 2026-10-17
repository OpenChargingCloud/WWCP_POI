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

        #endregion

        #region Properties

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
        /// Add the charging tariffs, charging pools and groups of the given charging station operator,
        /// in this order, to the operator once added: a change set the operator signs.
        /// </summary>
        /// <param name="ChargingStationOperator">The charging station operator with its children.</param>
        public static IReadOnlyList<RoamingNetworkChange> AddChildren(ChargingStationOperator ChargingStationOperator)

            => [
                   .. ChargingStationOperator.ChargingTariffs.      Select(chargingTariff       => Add(chargingTariff)),
                   .. ChargingStationOperator.ChargingPools.        Select(chargingPool         => Add(chargingPool)),
                   .. ChargingStationOperator.EVSEGroups.           Select(evseGroup            => Add(evseGroup)),
                   .. ChargingStationOperator.ChargingStationGroups.Select(chargingStationGroup => Add(chargingStationGroup)),
                   .. ChargingStationOperator.ChargingPoolGroups.   Select(chargingPoolGroup    => Add(chargingPoolGroup)),
                   .. ChargingStationOperator.ChargingTariffGroups. Select(chargingTariffGroup  => Add(chargingTariffGroup))
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
                   ChargingStationManufacturer.ToJSON());

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
                   EMobilityProvider.ToJSON());

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
                                   GridOperator.ToJSON()));

        /// <summary>
        /// Add the grid connection points of the given grid operator to the operator once added:
        /// a change set their maintainers or the grid operator sign.
        /// </summary>
        /// <param name="GridOperator">The grid operator with its grid connection points.</param>
        public static IReadOnlyList<RoamingNetworkChange> AddChildren(GridOperator GridOperator)

            => [ .. GridOperator.GridConnectionPoints.Select(gridConnectionPoint => Add(gridConnectionPoint, GridOperator.Id)) ];

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
                                   ParkingOperator.ToJSON()));

        /// <summary>
        /// Add the parking garages, spaces, sensors, space groups and products of the given parking operator,
        /// in this order, to the operator once added: a change set the operator signs.
        /// </summary>
        /// <param name="ParkingOperator">The parking operator with its parking data.</param>
        public static IReadOnlyList<RoamingNetworkChange> AddChildren(ParkingOperator ParkingOperator)

            => [
                   .. ParkingOperator.ParkingGarages.    Select(parkingGarage     => Add(parkingGarage,     ParkingOperator.Id)),
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

            => Add(nameof(POI.ParkingGarage), ParkingGarage.Id.ToString(), ParkingGarage.ToJSON(), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

        /// <summary>
        /// Add the given parking space to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSpace        ParkingSpace,
                                               ParkingOperator_Id  ParkingOperatorId)

            => Add(nameof(POI.ParkingSpace),  ParkingSpace. Id.ToString(), ParkingSpace. ToJSON(), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

        /// <summary>
        /// Add the given parking space to the given charging station operator, which defines it where no parking operator does.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSpace                ParkingSpace,
                                               ChargingStationOperator_Id  ChargingStationOperatorId)

            => Add(nameof(POI.ParkingSpace),  ParkingSpace. Id.ToString(), ParkingSpace. ToJSON(), nameof(POI.ChargingStationOperator), ChargingStationOperatorId.ToString());

        /// <summary>
        /// Add the given parking sensor to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSensor       ParkingSensor,
                                               ParkingOperator_Id  ParkingOperatorId)

            => Add(nameof(POI.ParkingSensor), ParkingSensor.Id.ToString(), ParkingSensor.ToJSON(), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

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

            => Add(nameof(POI.GridConnectionPoint), GridConnectionPoint.Id.ToString(), GridConnectionPoint.ToJSON(), nameof(POI.GridOperator), GridOperatorId.ToString());

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

        #region Add                   (ChargingTariff)

        /// <summary>
        /// Add the given charging tariff to its operator.
        /// </summary>
        /// <param name="ChargingTariff">The new charging tariff.</param>
        public static RoamingNetworkChange Add(ChargingTariff ChargingTariff)

            => Add(nameof(POI.ChargingTariff),
                   ChargingTariff.Id.ToString(),
                   ChargingTariff.ToJSON());

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

        #region Add                   (TransparencySoftware)

        /// <summary>
        /// Add the given transparency software release to its roaming network.
        /// </summary>
        /// <param name="TransparencySoftware">The new transparency software release.</param>
        public static RoamingNetworkChange Add(TransparencySoftware TransparencySoftware)

            => Add(nameof(POI.TransparencySoftware),
                   TransparencySoftware.Id.ToString(),
                   TransparencySoftware.ToJSON());

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

        #region Add                   (TransparencySoftwareCertificate)

        /// <summary>
        /// Add the given transparency software certificate to its roaming network.
        /// </summary>
        /// <param name="TransparencySoftwareCertificate">The new transparency software certificate.</param>
        public static RoamingNetworkChange Add(TransparencySoftwareCertificate TransparencySoftwareCertificate)

            => Add(nameof(POI.TransparencySoftwareCertificate),
                   TransparencySoftwareCertificate.Id.ToString(),
                   TransparencySoftwareCertificate.ToJSON());

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

        #region Add                   (EVSEGroup)

        /// <summary>
        /// Add the given EVSE group to its operator.
        /// </summary>
        /// <param name="EVSEGroup">The new EVSE group.</param>
        public static RoamingNetworkChange Add(EVSEGroup EVSEGroup)

            => Add(nameof(POI.EVSEGroup),
                   EVSEGroup.Id.ToString(),
                   EVSEGroup.ToJSON());

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
                   ChargingStationGroup.ToJSON());

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
                   ChargingPoolGroup.ToJSON());

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
                   ChargingTariffGroup.ToJSON());

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

        #region Add                   (ParkingSpaceGroup, ParkingOperatorId)

        /// <summary>
        /// Add the given parking space group to the given parking operator.
        /// </summary>
        public static RoamingNetworkChange Add(ParkingSpaceGroup   ParkingSpaceGroup,
                                               ParkingOperator_Id  ParkingOperatorId)

            => Add(nameof(POI.ParkingSpaceGroup), ParkingSpaceGroup.Id.ToString(), ParkingSpaceGroup.ToJSON(), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

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
                   EnergyMeterManufacturer.ToJSON());

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

            => Add(nameof(POI.ParkingProduct), ParkingProduct.Id.ToString(), ParkingProduct.ToJSON(), nameof(POI.ParkingOperator), ParkingOperatorId.ToString());

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
                   ChargingPool.ToJSON(),
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
                   ChargingStation.ToJSON(),
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
                   EVSE.ToJSON(),
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

                #region Parse Kind                [mandatory]

                if (!JSON.ParseMandatoryText("kind",
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

                if (!JSON.ParseMandatoryText("entityType",
                                             "entity type",
                                             out var entityType,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse EntityId            [mandatory]

                if (!JSON.ParseMandatoryText("entityId",
                                             "entity identification",
                                             out var entityId,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse PropertyName        [optional]

                if (JSON.ParseOptional("propertyName",
                                       "property name",
                                       out String? propertyName,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse ParentEntityType    [optional]

                if (JSON.ParseOptional("parentEntityType",
                                       "parent entity type",
                                       out String? parentEntityType,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse ParentEntityId      [optional]

                if (JSON.ParseOptional("parentEntityId",
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

                if (JSON["elementPath"] is JToken elementPathJSON)
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

                var oldValue = JSON["oldValue"];
                var newValue = JSON["newValue"];

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

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this roaming network change.
        /// </summary>
        public JObject ToJSON()
        {

            var json = JSONObject.Create(

                                 new JProperty("kind",               Kind.ToString()),
                                 new JProperty("entityType",         EntityType),
                                 new JProperty("entityId",           EntityId),

                           PropertyName is not null
                               ? new JProperty("propertyName",       PropertyName)
                               : null,

                           ParentEntityType is not null
                               ? new JProperty("parentEntityType",   ParentEntityType)
                               : null,

                           ParentEntityId is not null
                               ? new JProperty("parentEntityId",     ParentEntityId)
                               : null,

                           ElementPath.Length > 0
                               ? new JProperty("elementPath",        new JArray(ElementPath.Select(segment => segment.ToJSON())))
                               : null

                       );

            // Added directly: JSONObject.Create leaves out JSON null and empty values,
            // but an expected absent old value (null) or an empty new value ([], {}, "") is a value here.
            if (oldValue is not null)
                json.Add("oldValue", oldValue.DeepClone());

            if (newValue is not null)
                json.Add("newValue", newValue.DeepClone());

            return json;

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
