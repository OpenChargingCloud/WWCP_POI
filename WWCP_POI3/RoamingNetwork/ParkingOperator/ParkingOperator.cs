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
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of a parking operator changed.
    /// </summary>
    public delegate Task OnParkingOperatorAdminStatusChangedDelegate(DateTimeOffset                                Timestamp,
                                                                     EventTracking_Id                              EventTrackingId,
                                                                     ParkingOperator                               ParkingOperator,
                                                                     Timestamped<ParkingOperatorAdminStatusType>   OldStatus,
                                                                     Timestamped<ParkingOperatorAdminStatusType>   NewStatus,
                                                                     Context?                                      DataSource);

    /// <summary>
    /// The status of a parking operator changed.
    /// </summary>
    public delegate Task OnParkingOperatorStatusChangedDelegate(DateTimeOffset                           Timestamp,
                                                                EventTracking_Id                         EventTrackingId,
                                                                ParkingOperator                          ParkingOperator,
                                                                Timestamped<ParkingOperatorStatusType>   OldStatus,
                                                                Timestamped<ParkingOperatorStatusType>   NewStatus,
                                                                Context?                                 DataSource);


    /// <summary>
    /// An operator of parking garages, parking spaces, parking sensors and parking space groups, offering parking products,
    /// embedded completely in its roaming network. It owns all of them directly; a parking space may name a parking garage
    /// and parking sensors of the same operator.
    /// </summary>
    public sealed partial class ParkingOperator : AImmutableEMobilityEntity<ParkingOperator_Id,
                                                                    ParkingOperatorAdminStatusType,
                                                                    ParkingOperatorStatusType>,
                                          IEquatable<ParkingOperator>,
                                          IComparable<ParkingOperator>,
                                          IPOIParty
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/parkingOperator";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address                   = "address";

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage                  = "homepage";

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber        = "hotline";

            /// <summary>
            /// The property of its parking garages.
            /// </summary>
            public const String  ParkingGarages            = "parkingGarages";

            /// <summary>
            /// The property of the roots of its parking garages, in the skeleton of its tree.
            /// </summary>
            public const String  ParkingGarageIds          = "parkingGarageIds";

            /// <summary>
            /// The property of its parking spaces.
            /// </summary>
            public const String  ParkingSpaces             = "parkingSpaces";

            /// <summary>
            /// The property of the roots of its parking spaces, in the skeleton of its tree.
            /// </summary>
            public const String  ParkingSpaceIds           = "parkingSpaceIds";

            /// <summary>
            /// The property of its parking sensors.
            /// </summary>
            public const String  ParkingSensors            = "parkingSensors";

            /// <summary>
            /// The property of the roots of its parking sensors, in the skeleton of its tree.
            /// </summary>
            public const String  ParkingSensorIds          = "parkingSensorIds";

            /// <summary>
            /// The property of its parking space templates.
            /// </summary>
            public const String  ParkingSpaceTemplates     = "parkingSpaceTemplates";

            /// <summary>
            /// The property of the roots of its parking space templates, in the skeleton of its tree.
            /// </summary>
            public const String  ParkingSpaceTemplateIds   = "parkingSpaceTemplateIds";

            /// <summary>
            /// The property of its parking space groups.
            /// </summary>
            public const String  ParkingSpaceGroups        = "parkingSpaceGroups";

            /// <summary>
            /// The property of the roots of its parking space groups, in the skeleton of its tree.
            /// </summary>
            public const String  ParkingSpaceGroupIds      = "parkingSpaceGroupIds";

            /// <summary>
            /// The property of its parking products.
            /// </summary>
            public const String  ParkingProducts           = "parkingProducts";

            /// <summary>
            /// The property of the roots of its parking products, in the skeleton of its tree.
            /// </summary>
            public const String  ParkingProductIds         = "parkingProductIds";

            /// <summary>
            /// The property of its signing keys, as of every party.
            /// </summary>
            public const String  SigningKeys               = POIPartyKeys.SigningKeysProperty;

            /// <summary>
            /// The property of its identity keys, as of every party.
            /// </summary>
            public const String  IdentityKeys              = POIPartyKeys.IdentityKeysProperty;

        }

        #endregion


        /// <summary>
        /// The address of the headquarters.
        /// </summary>
        public Address?                       Address               { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?                           Homepage              { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?                   HotlinePhoneNumber    { get; }

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>   SigningKeys           { get; }

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>   IdentityKeys          { get; }

        /// <summary>
        /// The parking garages, in the order of their identifications.
        /// </summary>
        public POICollection<ParkingGarage_Id, ParkingGarage>  ParkingGarages        { get; }

        /// <summary>
        /// The parking spaces, in the order of their identifications.
        /// </summary>
        public POICollection<ParkingSpace_Id, ParkingSpace>   ParkingSpaces         { get; }

        /// <summary>
        /// The parking sensors, in the order of their identifications.
        /// </summary>
        public POICollection<ParkingSensor_Id, ParkingSensor>  ParkingSensors        { get; }

        /// <summary>
        /// The templates of its parking spaces, in the order of their identifications.
        /// </summary>
        public POICollection<ParkingSpaceTemplate_Id, ParkingSpaceTemplate>  ParkingSpaceTemplates  { get; }

        /// <summary>
        /// The groups of its parking spaces, in the order of their identifications; they may overlap.
        /// </summary>
        public POICollection<ParkingSpaceGroup_Id, ParkingSpaceGroup>  ParkingSpaceGroups  { get; }

        /// <summary>
        /// The parking products, in the order of their identifications.
        /// </summary>
        public POICollection<ParkingProduct_Id, ParkingProduct>  ParkingProducts  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking operator.
        /// </summary>
        public ParkingOperator(ParkingOperator_Id                              Id,
                               I18NString?                                     Name                         = null,
                               I18NString?                                     Description                  = null,
                               Address?                                        Address                      = null,
                               URL?                                            Homepage                     = null,
                               PhoneNumber?                                    HotlinePhoneNumber           = null,
                               IEnumerable<ParkingGarage>?                     ParkingGarages               = null,
                               IEnumerable<ParkingSpace>?                      ParkingSpaces                = null,
                               IEnumerable<ParkingSensor>?                     ParkingSensors               = null,
                               IEnumerable<ParkingSpaceGroup>?                 ParkingSpaceGroups           = null,
                               IEnumerable<ParkingProduct>?                    ParkingProducts              = null,
                               IEnumerable<POIPublicKey>?                      SigningKeys                  = null,
                               IEnumerable<POIPublicKey>?                      IdentityKeys                 = null,
                               IEnumerable<ParkingSpaceTemplate>?              ParkingSpaceTemplates        = null,

                               Timestamped<ParkingOperatorAdminStatusType>?    InitialAdminStatus           = null,
                               Timestamped<ParkingOperatorStatusType>?         InitialStatus                = null,
                               UInt16?                                         MaxAdminStatusScheduleSize   = null,
                               UInt16?                                         MaxStatusScheduleSize        = null,

                               String?                                         DataSource                   = null,
                               DateTimeOffset?                                 Created                      = null,
                               DateTimeOffset?                                 LastChange                   = null,

                               CustomDataNew?                                  CustomData                   = null,
                               UserDefinedDictionary?                          InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ParkingOperatorAdminStatusType>(Created ?? Timestamp.Now, ParkingOperatorAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ParkingOperatorStatusType>     (Created ?? Timestamp.Now, ParkingOperatorStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.Address             = Address;
            this.Homepage            = Homepage;
            this.HotlinePhoneNumber  = HotlinePhoneNumber;

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the parking operator '{Id}'");

            var owner = $"the parking operator '{Id}'";

            // Parking garage, space, sensor and group identifications compare without case, parking product ones with case.
            this.ParkingGarages      = new (ParkingGarages,     garage  => garage. Id, "parking garage",      owner);
            this.ParkingSpaces       = new (ParkingSpaces,      space   => space.  Id, "parking space",       owner);
            this.ParkingSensors      = new (ParkingSensors,     sensor  => sensor. Id, "parking sensor",      owner);
            this.ParkingSpaceTemplates  = new (ParkingSpaceTemplates, template => template.Id, "parking space template", owner);
            this.ParkingSpaceGroups  = new (ParkingSpaceGroups, group   => group.  Id, "parking space group", owner);
            this.ParkingProducts     = new (ParkingProducts,    product => product.Id, "parking product",     owner);

            foreach (var (kind, ownerId, productIds) in this.ParkingGarages.    Select(garage => ("parking garage",      garage.Id.ToString(), garage.ParkingProductIds)).
                                                   Concat(this.ParkingSpaces.     Select(space  => ("parking space",       space. Id.ToString(), space. ParkingProductIds))).
                                                   Concat(this.ParkingSpaceTemplates.Select(template => ("parking space template", template.Id.ToString(), template.ParkingProductIds))))
                foreach (var productId in productIds)
                    if (!this.ParkingProducts.Contains(productId))
                        throw new ArgumentException($"The parking product '{productId}' of the {kind} '{ownerId}' is unknown at the parking operator '{Id}'!", nameof(ParkingProducts));

            foreach (var parkingSpaceGroup in this.ParkingSpaceGroups)
                foreach (var parkingSpaceId in parkingSpaceGroup.ParkingSpaceIds)
                    if (!this.ParkingSpaces.Contains(parkingSpaceId))
                        throw new ArgumentException($"The parking space '{parkingSpaceId}' of the parking space group '{parkingSpaceGroup.Id}' is unknown at the parking operator '{Id}'!", nameof(ParkingSpaceGroups));

            // A parking space names only garages and sensors of its own operator.
            foreach (var parkingSpace in this.ParkingSpaces)
            {

                if (parkingSpace.ParkingGarageId.HasValue &&
                    !this.ParkingGarages.Contains(parkingSpace.ParkingGarageId.Value))
                {
                    throw new ArgumentException($"The parking garage '{parkingSpace.ParkingGarageId}' of the parking space '{parkingSpace.Id}' is unknown at the parking operator '{Id}'!", nameof(ParkingSpaces));
                }

                foreach (var parkingSensorId in parkingSpace.ParkingSensorIds)
                    if (!this.ParkingSensors.Contains(parkingSensorId))
                        throw new ArgumentException($"The parking sensor '{parkingSensorId}' of the parking space '{parkingSpace.Id}' is unknown at the parking operator '{Id}'!", nameof(ParkingSpaces));

            }

            // So does a parking space template, for its members.
            foreach (var parkingSpaceTemplate in this.ParkingSpaceTemplates)
            {

                if (parkingSpaceTemplate.ParkingGarageId.HasValue &&
                    !this.ParkingGarages.Contains(parkingSpaceTemplate.ParkingGarageId.Value))
                {
                    throw new ArgumentException($"The parking garage '{parkingSpaceTemplate.ParkingGarageId}' of the parking space template '{parkingSpaceTemplate.Id}' is unknown at the parking operator '{Id}'!", nameof(ParkingSpaceTemplates));
                }

                foreach (var parkingSensorId in parkingSpaceTemplate.ParkingSensorIds)
                    if (!this.ParkingSensors.Contains(parkingSensorId))
                        throw new ArgumentException($"The parking sensor '{parkingSensorId}' of the parking space template '{parkingSpaceTemplate.Id}' is unknown at the parking operator '{Id}'!", nameof(ParkingSpaceTemplates));

            }

            // The templates its parking spaces name, its own.
            foreach (var parkingSpace in this.ParkingSpaces)
                foreach (var templateId in parkingSpace.TemplateIds)
                    if (!this.ParkingSpaceTemplates.Contains(templateId))
                        throw new ArgumentException($"The template '{templateId}' of the parking space '{parkingSpace.Id}' is unknown at the parking operator '{Id}'!", nameof(ParkingSpaceTemplates));

            // The effective parking spaces, with the properties their templates give them.
            this.effectiveParkingSpaceIndex  = CalculateEffective();
            this.effectiveParkingSpaces      = new (CalculateEffectiveParkingSpaces);

        }

        #endregion


        #region TryGetParkingGarage(ParkingGarageId, out ParkingGarage)

        /// <summary>
        /// Try to return the parking garage with the given identification.
        /// </summary>
        public Boolean TryGetParkingGarage(ParkingGarage_Id                         ParkingGarageId,
                                           [NotNullWhen(true)] out ParkingGarage?   ParkingGarage)

            => ParkingGarages.TryGet(ParkingGarageId, out ParkingGarage);

        #endregion

        #region TryGetParkingSpace (ParkingSpaceId,  out ParkingSpace)

        /// <summary>
        /// Try to return the parking space with the given identification.
        /// </summary>
        public Boolean TryGetParkingSpace(ParkingSpace_Id                         ParkingSpaceId,
                                          [NotNullWhen(true)] out ParkingSpace?   ParkingSpace)

            => ParkingSpaces.TryGet(ParkingSpaceId, out ParkingSpace);

        #endregion

        #region TryGetParkingSensor(ParkingSensorId, out ParkingSensor)

        /// <summary>
        /// Try to return the parking sensor with the given identification.
        /// </summary>
        public Boolean TryGetParkingSensor(ParkingSensor_Id                         ParkingSensorId,
                                           [NotNullWhen(true)] out ParkingSensor?   ParkingSensor)

            => ParkingSensors.TryGet(ParkingSensorId, out ParkingSensor);

        #endregion

        #region TryGetParkingSpaceTemplate(ParkingSpaceTemplateId, out ParkingSpaceTemplate)

        /// <summary>
        /// Try to return the parking space template with the given identification.
        /// </summary>
        public Boolean TryGetParkingSpaceTemplate(ParkingSpaceTemplate_Id                        ParkingSpaceTemplateId,
                                                  [NotNullWhen(true)] out ParkingSpaceTemplate?  ParkingSpaceTemplate)

            => ParkingSpaceTemplates.TryGet(ParkingSpaceTemplateId, out ParkingSpaceTemplate);

        #endregion

        #region TryGetParkingSpaceGroup(ParkingSpaceGroupId, out ParkingSpaceGroup)

        /// <summary>
        /// Try to return the parking space group with the given identification.
        /// </summary>
        public Boolean TryGetParkingSpaceGroup(ParkingSpaceGroup_Id                        ParkingSpaceGroupId,
                                               [NotNullWhen(true)] out ParkingSpaceGroup?  ParkingSpaceGroup)

            => ParkingSpaceGroups.TryGet(ParkingSpaceGroupId, out ParkingSpaceGroup);

        #endregion

        #region TryGetParkingProduct(ParkingProductId, out ParkingProduct)

        /// <summary>
        /// Try to return the parking product with the given identification.
        /// </summary>
        public Boolean TryGetParkingProduct(ParkingProduct_Id                        ParkingProductId,
                                            [NotNullWhen(true)] out ParkingProduct?  ParkingProduct)

            => ParkingProducts.TryGet(ParkingProductId, out ParkingProduct);

        #endregion

        #region (internal) ShareRuntimeOfAll(Previous)

        /// <summary>
        /// Take the runtime status and internal data of the given previous version of this parking operator,
        /// and of every parking garage, parking space, parking sensor and parking space group it had, by their identifications.
        /// </summary>
        internal void ShareRuntimeOfAll(ParkingOperator Previous)
        {

            ShareRuntimeOf(Previous);

            foreach (var parkingGarage in ParkingGarages)
                if (Previous.TryGetParkingGarage(parkingGarage.Id, out var previousParkingGarage))
                    parkingGarage.ShareRuntimeOf(previousParkingGarage);

            foreach (var parkingSpace in ParkingSpaces)
                if (Previous.TryGetParkingSpace(parkingSpace.Id, out var previousParkingSpace))
                    parkingSpace.ShareRuntimeOf(previousParkingSpace);

            foreach (var parkingSensor in ParkingSensors)
                if (Previous.TryGetParkingSensor(parkingSensor.Id, out var previousParkingSensor))
                    parkingSensor.ShareRuntimeOf(previousParkingSensor);

            foreach (var parkingSpaceGroup in ParkingSpaceGroups)
                if (Previous.TryGetParkingSpaceGroup(parkingSpaceGroup.Id, out var previousParkingSpaceGroup))
                    parkingSpaceGroup.ShareRuntimeOf(previousParkingSpaceGroup);

            StampChangedEffective(Previous);
            ShareRuntimeOfEffective();

        }

        #endregion


        #region Status events

        private readonly Lock                                          statusEventsLock  = new ();
        private          OnParkingOperatorAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnParkingOperatorStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnParkingOperatorAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnParkingOperatorStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                               Timestamp,
                                                                   EventTracking_Id                             EventTrackingId,
                                                                   Timestamped<ParkingOperatorAdminStatusType>  NewStatus,
                                                                   Timestamped<ParkingOperatorAdminStatusType>  OldStatus,
                                                                   Context?                                     DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                          Timestamp,
                                                              EventTracking_Id                        EventTrackingId,
                                                              Timestamped<ParkingOperatorStatusType>  NewStatus,
                                                              Timestamped<ParkingOperatorStatusType>  OldStatus,
                                                              Context?                                DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON(Embedded = false, CustomParkingOperatorSerializer = null)

        /// <summary>
        /// Return the JSON representation of this parking operator, as its roaming network embeds it:
        /// with its parking garages, parking spaces, parking sensors, parking space groups and parking products.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomParkingOperatorSerializer">A delegate to serialize custom parking operator JSON objects.</param>
        public JObject ToJSON(Boolean                                            Embedded                          = false,
                              CustomJObjectSerializerDelegate<ParkingOperator>?  CustomParkingOperatorSerializer   = null)

            => ToJSON(Embedded,
                      CustomParkingOperatorSerializer,
                      IncludeChildArrays: true);

        /// <summary>
        /// The JSON representation of this parking operator, as its roaming network embeds it, with or without its child arrays:
        /// without them for the skeleton of its tree (SPECIFICATION.md, section 8.2), which names their roots in their place.
        /// </summary>
        /// <param name="IncludeChildArrays">Whether to include its child arrays.</param>
        internal JObject ToJSON(Boolean IncludeChildArrays)

            => ToJSON(Embedded:                         true,
                      CustomParkingOperatorSerializer:  null,
                      IncludeChildArrays:               IncludeChildArrays);

        /// <summary>
        /// The JSON representation of this parking operator, with or without its child arrays.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomParkingOperatorSerializer">A delegate to serialize custom parking operator JSON objects.</param>
        /// <param name="IncludeChildArrays">Whether to include its child arrays.</param>
        private JObject ToJSON(Boolean                                            Embedded,
                               CustomJObjectSerializerDelegate<ParkingOperator>?  CustomParkingOperatorSerializer,
                               Boolean                                            IncludeChildArrays)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,            JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,               Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,        Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,         DataSource);

            if (Address is not null)
                json.Add(PropertyKeys.Address,                Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add(PropertyKeys.Homepage,               Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,     HotlinePhoneNumber.Value.ToString());

            if (IncludeChildArrays && ParkingGarages.Count > 0)
                json.Add(PropertyKeys.ParkingGarages,         new JArray(ParkingGarages.       Select(parkingGarage        => parkingGarage.       ToJSON(Embedded: true))));

            if (IncludeChildArrays && ParkingSpaces.Count > 0)
                json.Add(PropertyKeys.ParkingSpaces,          new JArray(ParkingSpaces.        Select(parkingSpace         => parkingSpace.        ToJSON(Embedded: true))));

            if (IncludeChildArrays && ParkingSensors.Count > 0)
                json.Add(PropertyKeys.ParkingSensors,         new JArray(ParkingSensors.       Select(parkingSensor        => parkingSensor.       ToJSON(Embedded: true))));

            if (IncludeChildArrays && ParkingSpaceTemplates.Count > 0)
                json.Add(PropertyKeys.ParkingSpaceTemplates,  new JArray(ParkingSpaceTemplates.Select(parkingSpaceTemplate => parkingSpaceTemplate.ToJSON(Embedded: true))));

            if (IncludeChildArrays && ParkingSpaceGroups.Count > 0)
                json.Add(PropertyKeys.ParkingSpaceGroups,     new JArray(ParkingSpaceGroups.   Select(parkingSpaceGroup    => parkingSpaceGroup.   ToJSON(Embedded: true))));

            if (IncludeChildArrays && ParkingProducts.Count > 0)
                json.Add(PropertyKeys.ParkingProducts,        new JArray(ParkingProducts.      Select(parkingProduct       => parkingProduct.      ToJSON(Embedded: true))));

            POIPartyKeys.AddTo(json, this);

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return CustomParkingOperatorSerializer is not null
                       ? CustomParkingOperatorSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomParkingOperatorSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this parking operator: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomParkingOperatorSerializer">A delegate to serialize custom parking operator CBOR values.</param>
        public CBORValue ToCBOR(Boolean                                          Embedded                          = false,
                                CustomCBORSerializerDelegate<ParkingOperator>?   CustomParkingOperatorSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),                 CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),            CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),               Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),        Description.ToCBOR());

            if (DataSource is not null)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),         CBORValue.FromText(DataSource));

            // The address, the parking garages, spaces, sensors, space templates, space groups and products and the keys have
            // no CBOR representation of their own: theirs are the keys and values of their JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),                CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),               CBORValue.FromText(Homepage.Value.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),     CBORValue.FromText(HotlinePhoneNumber.Value.ToString()));

            if (ParkingGarages.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParkingGarages),
                         CBORValue.FromArray(ParkingGarages.       Select(parkingGarage        => CBORJSON.ToCBOR(parkingGarage.       ToJSON(Embedded: true)))));

            if (ParkingSpaces.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParkingSpaces),
                         CBORValue.FromArray(ParkingSpaces.        Select(parkingSpace         => CBORJSON.ToCBOR(parkingSpace.        ToJSON(Embedded: true)))));

            if (ParkingSensors.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParkingSensors),
                         CBORValue.FromArray(ParkingSensors.       Select(parkingSensor        => CBORJSON.ToCBOR(parkingSensor.       ToJSON(Embedded: true)))));

            if (ParkingSpaceTemplates.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParkingSpaceTemplates),
                         CBORValue.FromArray(ParkingSpaceTemplates.Select(parkingSpaceTemplate => CBORJSON.ToCBOR(parkingSpaceTemplate.ToJSON(Embedded: true)))));

            if (ParkingSpaceGroups.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParkingSpaceGroups),
                         CBORValue.FromArray(ParkingSpaceGroups.   Select(parkingSpaceGroup    => CBORJSON.ToCBOR(parkingSpaceGroup.   ToJSON(Embedded: true)))));

            if (ParkingProducts.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParkingProducts),
                         CBORValue.FromArray(ParkingProducts.      Select(parkingProduct       => CBORJSON.ToCBOR(parkingProduct.      ToJSON(Embedded: true)))));

            foreach (var keys in POIPartyKeys.AddTo(new JObject(), this).Properties())
                cbor.Add(CBORValue.FromText(keys.Name),                           CBORJSON.ToCBOR(keys.Value));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),                CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),             CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),         CBORJSON.ToCBOR(CustomData.ToJObject()));

            return CustomParkingOperatorSerializer is not null
                       ? CustomParkingOperatorSerializer(this, cbor)
                       : cbor;

        }

        #endregion

        #region (static) TryParse(JSON, out ParkingOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a parking operator,
        /// with its parking garages, parking spaces, parking sensors, parking space groups and parking products.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ParkingOperator">The parsed parking operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out ParkingOperator?  ParkingOperator,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            ParkingOperator = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "parking operator identification",
                                         ParkingOperator_Id.TryParse,
                                         out ParkingOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the parking operator '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource, "data source", out var dataSource, out ErrorResponse))
                    return false;

                return TryParse(id,
                                dataSource,
                                JSON,
                                out ParkingOperator,
                                out ErrorResponse);

            }
            catch (Exception e)
            {
                ParkingOperator  = null;
                ErrorResponse    = "The given JSON representation of a parking operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out ParkingOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a parking operator: the keys and values of its JSON representation,
        /// see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="ParkingOperator">The parsed parking operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                  CBOR,
                                       [NotNullWhen(true)]  out ParkingOperator?  ParkingOperator,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            ParkingOperator = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a parking operator must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "parking operator identification",
                                         ParkingOperator_Id.TryParse,
                                         out ParkingOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var context) &&
                    !(context.TryGetText(out var contextText) && contextText == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the parking operator '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource, "data source", out var dataSource, out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                // What has no CBOR parser of its own is read as its JSON, with the parsers of the JSON representation:
                // the multilingual texts, the address, the homepage, the hotline, the children, the keys and the metadata.
                var json = new JObject(
                               new JProperty(BasePropertyKeys.Id, id.ToString())
                           );

                foreach (var key in new[] { BasePropertyKeys.Name,
                                            BasePropertyKeys.Description,
                                            PropertyKeys.    Address,
                                            PropertyKeys.    Homepage,
                                            PropertyKeys.    HotlinePhoneNumber,
                                            PropertyKeys.    ParkingGarages,
                                            PropertyKeys.    ParkingGarageIds,
                                            PropertyKeys.    ParkingSpaces,
                                            PropertyKeys.    ParkingSpaceIds,
                                            PropertyKeys.    ParkingSensors,
                                            PropertyKeys.    ParkingSensorIds,
                                            PropertyKeys.    ParkingSpaceTemplates,
                                            PropertyKeys.    ParkingSpaceTemplateIds,
                                            PropertyKeys.    ParkingSpaceGroups,
                                            PropertyKeys.    ParkingSpaceGroupIds,
                                            PropertyKeys.    ParkingProducts,
                                            PropertyKeys.    ParkingProductIds,
                                            PropertyKeys.    SigningKeys,
                                            PropertyKeys.    IdentityKeys,
                                            BasePropertyKeys.Created,
                                            BasePropertyKeys.LastChange,
                                            BasePropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                return TryParse(id,
                                dataSource,
                                json,
                                out ParkingOperator,
                                out ErrorResponse);

            }
            catch (Exception e)
            {
                ParkingOperator  = null;
                ErrorResponse    = "The given CBOR representation of a parking operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (private static) TryParse(Id, DataSource, JSON, out ParkingOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the rest of the given JSON representation of a parking operator, or of the JSON of the parts of its
        /// CBOR representation without a CBOR parser of their own.
        /// </summary>
        private static Boolean TryParse(ParkingOperator_Id                         Id,
                                        String?                                    DataSource,
                                        JObject                                    JSON,
                                        [NotNullWhen(true)]  out ParkingOperator?  ParkingOperator,
                                        [NotNullWhen(false)] out String?           ErrorResponse)
        {

            ParkingOperator = null;

            if (!POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Name,                "parking operator name",         out var name,         out ErrorResponse) ||
                !POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Description,         "parking operator description",  out var description,  out ErrorResponse) ||
                !POIJSON.TryParseAddress(JSON, PropertyKeys.    Address,             "address",                       out var address,      out ErrorResponse) ||
                !POIJSON.TryParseText   (JSON, PropertyKeys.    Homepage,            "homepage",                      URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                !POIJSON.TryParseText   (JSON, PropertyKeys.    HotlinePhoneNumber,  "hotline phone number",          PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
            {
                return false;
            }

            if (!POIJSON.TryParseChildren<ParkingGarage>       (JSON, PropertyKeys.ParkingGarages,         PropertyKeys.ParkingGarageIds,         "parking garages",          ParkingGarage.       TryParse, out var parkingGarages,         out ErrorResponse) ||
                !POIJSON.TryParseChildren<ParkingSpace>        (JSON, PropertyKeys.ParkingSpaces,          PropertyKeys.ParkingSpaceIds,          "parking spaces",           ParkingSpace.        TryParse, out var parkingSpaces,          out ErrorResponse) ||
                !POIJSON.TryParseChildren<ParkingSensor>       (JSON, PropertyKeys.ParkingSensors,         PropertyKeys.ParkingSensorIds,         "parking sensors",          ParkingSensor.       TryParse, out var parkingSensors,         out ErrorResponse) ||
                !POIJSON.TryParseChildren<ParkingSpaceTemplate>(JSON, PropertyKeys.ParkingSpaceTemplates,  PropertyKeys.ParkingSpaceTemplateIds,  "parking space templates",  ParkingSpaceTemplate.TryParse, out var parkingSpaceTemplates,  out ErrorResponse) ||
                !POIJSON.TryParseChildren<ParkingSpaceGroup>   (JSON, PropertyKeys.ParkingSpaceGroups,     PropertyKeys.ParkingSpaceGroupIds,     "parking space groups",     ParkingSpaceGroup.   TryParse, out var parkingSpaceGroups,     out ErrorResponse) ||
                !POIJSON.TryParseChildren<ParkingProduct>      (JSON, PropertyKeys.ParkingProducts,        PropertyKeys.ParkingProductIds,        "parking products",         ParkingProduct.      TryParse, out var parkingProducts,        out ErrorResponse))
            {
                return false;
            }

            if (!POIPartyKeys.TryParse(JSON, out var signingKeys, out var identityKeys, out ErrorResponse))
                return false;

            if (!POIEntityMetadata.TryParse(JSON,
                                            out var created,
                                            out var lastChange,
                                            out var customData,
                                            out ErrorResponse))
            {
                return false;
            }

            ParkingOperator = new ParkingOperator(Id,
                                                  name,
                                                  description,
                                                  address,
                                                  homepage,
                                                  hotline,
                                                  parkingGarages,
                                                  parkingSpaces,
                                                  parkingSensors,
                                                  parkingSpaceGroups,
                                                  parkingProducts,
                                                  signingKeys,
                                                  identityKeys,
                                                  parkingSpaceTemplates,
                                                  DataSource:  DataSource,
                                                  Created:     created,
                                                  LastChange:  lastChange,
                                                  CustomData:  customData);

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two parking operators for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ParkingOperator? ParkingOperator1, ParkingOperator? ParkingOperator2)

            => ReferenceEquals(ParkingOperator1, ParkingOperator2) ||
               (ParkingOperator1 is not null && ParkingOperator1.Equals(ParkingOperator2));

        /// <summary>
        /// Compares two parking operators for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ParkingOperator? ParkingOperator1, ParkingOperator? ParkingOperator2)

            => !(ParkingOperator1 == ParkingOperator2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ParkingOperator parkingOperator
                   ? CompareTo(parkingOperator)
                   : throw new ArgumentException("The given object is not a parking operator!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ParkingOperator? ParkingOperator)

            => ParkingOperator is null
                   ? throw new ArgumentNullException(nameof(ParkingOperator), "The given parking operator must not be null!")
                   : Id.CompareTo(ParkingOperator.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ParkingOperator parkingOperator &&
               Equals(parkingOperator);

        /// <inheritdoc/>
        public Boolean Equals(ParkingOperator? ParkingOperator)

            => ParkingOperator is not null &&
               Id.Equals(ParkingOperator.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
