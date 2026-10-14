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
                                                                     Timestamped<ParkingOperatorAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of a parking operator changed.
    /// </summary>
    public delegate Task OnParkingOperatorStatusChangedDelegate(DateTimeOffset                           Timestamp,
                                                                EventTracking_Id                         EventTrackingId,
                                                                ParkingOperator                          ParkingOperator,
                                                                Timestamped<ParkingOperatorStatusType>   OldStatus,
                                                                Timestamped<ParkingOperatorStatusType>   NewStatus);


    /// <summary>
    /// An operator of parking garages, parking spaces, parking sensors and parking space groups, offering parking products,
    /// embedded completely in its roaming network. It owns all of them directly; a parking space may name a parking garage
    /// and parking sensors of the same operator.
    /// </summary>
    public sealed class ParkingOperator : AImmutableEMobilityEntity<ParkingOperator_Id,
                                                                    ParkingOperatorAdminStatusType,
                                                                    ParkingOperatorStatusType>,
                                          IEquatable<ParkingOperator>,
                                          IComparable<ParkingOperator>,
                                          IPOIParty
    {

        #region Data


        #endregion

        #region Properties

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
            this.ParkingSpaceGroups  = new (ParkingSpaceGroups, group   => group.  Id, "parking space group", owner);
            this.ParkingProducts     = new (ParkingProducts,    product => product.Id, "parking product",     owner);

            foreach (var (kind, ownerId, productIds) in this.ParkingGarages.    Select(garage => ("parking garage",      garage.Id.ToString(), garage.ParkingProductIds)).
                                                   Concat(this.ParkingSpaces.     Select(space  => ("parking space",       space. Id.ToString(), space. ParkingProductIds))).
                                                   Concat(this.ParkingSpaceGroups.Select(group  => ("parking space group", group. Id.ToString(), group. ParkingProductIds))))
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
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
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
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this parking operator, as its roaming network embeds it:
        /// with its parking garages, parking spaces, parking sensors, parking space groups and parking products.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",            Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",     Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",      DataSource);

            if (Address is not null)
                json.Add("address",         Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add("homepage",        Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",         HotlinePhoneNumber.Value.ToString());

            if (ParkingGarages.Count > 0)
                json.Add("parkingGarages",  new JArray(ParkingGarages.Select(parkingGarage => parkingGarage.ToJSON())));

            if (ParkingSpaces.Count > 0)
                json.Add("parkingSpaces",   new JArray(ParkingSpaces. Select(parkingSpace  => parkingSpace. ToJSON())));

            if (ParkingSensors.Count > 0)
                json.Add("parkingSensors",  new JArray(ParkingSensors.Select(parkingSensor => parkingSensor.ToJSON())));

            if (ParkingSpaceGroups.Count > 0)
                json.Add("parkingSpaceGroups", new JArray(ParkingSpaceGroups.Select(parkingSpaceGroup => parkingSpaceGroup.ToJSON())));

            if (ParkingProducts.Count > 0)
                json.Add("parkingProducts", new JArray(ParkingProducts.Select(parkingProduct => parkingProduct.ToJSON())));

            POIPartyKeys.AddTo(json, this);

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return the CBOR representation of this parking operator: the keys and values of its JSON representation.
        /// </summary>
        public CBORValue ToCBOR()

            => CBORJSON.ToCBOR(ToJSON());

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

                if (!JSON.ParseMandatory("@id",
                                         "parking operator identification",
                                         ParkingOperator_Id.TryParse,
                                         out ParkingOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, "name",         "parking operator name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, "description",  "parking operator description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, "dataSource",   "data source",                   out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress(JSON,                                                  out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "homepage",     "homepage",                      URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "hotline",      "hotline phone number",          PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<ParkingGarage>(JSON, "parkingGarages", "parkingGarageIds", "parking garages", ParkingGarage.TryParse, out var parkingGarages, out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ParkingSpace> (JSON, "parkingSpaces",  "parkingSpaceIds",  "parking spaces",  ParkingSpace. TryParse, out var parkingSpaces,  out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ParkingSensor>(JSON, "parkingSensors", "parkingSensorIds", "parking sensors", ParkingSensor.TryParse, out var parkingSensors, out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ParkingSpaceGroup>(JSON, "parkingSpaceGroups", "parkingSpaceGroupIds", "parking space groups", ParkingSpaceGroup.TryParse, out var parkingSpaceGroups, out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ParkingProduct>(JSON, "parkingProducts", "parkingProductIds", "parking products", ParkingProduct.TryParse, out var parkingProducts, out ErrorResponse))
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

                ParkingOperator = new ParkingOperator(id,
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
                                                      DataSource:  dataSource,
                                                      Created:     created,
                                                      LastChange:  lastChange,
                                                      CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ParkingOperator  = null;
                ErrorResponse    = "The given JSON representation of a parking operator is invalid: " + e.Message;
                return false;
            }

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
