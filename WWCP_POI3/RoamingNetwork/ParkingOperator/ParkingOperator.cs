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
    /// An operator of parking garages, parking spaces and parking sensors, embedded completely in its roaming network.
    /// It owns all of them directly; a parking space may name a parking garage and parking sensors of the same operator.
    /// </summary>
    public sealed class ParkingOperator : AImmutableEMobilityEntity<ParkingOperator_Id,
                                                                    ParkingOperatorAdminStatusType,
                                                                    ParkingOperatorStatusType>,
                                          IEquatable<ParkingOperator>,
                                          IComparable<ParkingOperator>
    {

        #region Data

        private readonly ImmutableDictionary<ParkingGarage_Id, ParkingGarage>  parkingGarageIndex;
        private readonly ImmutableDictionary<ParkingSpace_Id,  ParkingSpace>   parkingSpaceIndex;
        private readonly ImmutableDictionary<ParkingSensor_Id, ParkingSensor>  parkingSensorIndex;
        private readonly ImmutableDictionary<ParkingSpaceGroup_Id, ParkingSpaceGroup>  parkingSpaceGroupIndex;
        private readonly ImmutableDictionary<ParkingProduct_Id, ParkingProduct>  parkingProductIndex;

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

        /// <summary>
        /// The parking garages, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingGarage>  ParkingGarages        { get; }

        /// <summary>
        /// The parking spaces, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingSpace>   ParkingSpaces         { get; }

        /// <summary>
        /// The parking sensors, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingSensor>  ParkingSensors        { get; }

        /// <summary>
        /// The groups of its parking spaces, in the order of their identifications; they may overlap.
        /// </summary>
        public ImmutableArray<ParkingSpaceGroup>  ParkingSpaceGroups  { get; }

        /// <summary>
        /// The parking products, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingProduct>  ParkingProducts  { get; }

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

            this.ParkingGarages      = Ordered(ParkingGarages, garage => garage.Id.ToString(), "parking garage");
            this.ParkingSpaces       = Ordered(ParkingSpaces,  space  => space. Id.ToString(), "parking space");
            this.ParkingSensors      = Ordered(ParkingSensors, sensor => sensor.Id.ToString(), "parking sensor");

            this.parkingGarageIndex  = this.ParkingGarages.ToImmutableDictionary(garage => garage.Id);
            this.parkingSpaceIndex   = this.ParkingSpaces. ToImmutableDictionary(space  => space. Id);
            this.parkingSensorIndex  = this.ParkingSensors.ToImmutableDictionary(sensor => sensor.Id);

            this.ParkingSpaceGroups      = Ordered(ParkingSpaceGroups, group => group.Id.ToString(), "parking space group");
            this.parkingSpaceGroupIndex  = this.ParkingSpaceGroups.ToImmutableDictionary(group => group.Id);

            // Parking product identifications compare case-sensitively.
            this.ParkingProducts      = [.. (ParkingProducts ?? []).Select(product => product ?? throw new ArgumentException("Parking products must not contain null entries!")).
                                                                    OrderBy(product => product.Id.ToString(), StringComparer.Ordinal)];

            if (this.ParkingProducts.Select(product => product.Id).Distinct().Count() != this.ParkingProducts.Length)
                throw new ArgumentException($"A parking product of the parking operator '{Id}' must not be given twice!", nameof(ParkingProducts));

            this.parkingProductIndex  = this.ParkingProducts.ToImmutableDictionary(product => product.Id);

            foreach (var (kind, ownerId, productIds) in this.ParkingGarages.    Select(garage => ("parking garage",      garage.Id.ToString(), garage.ParkingProductIds)).
                                                   Concat(this.ParkingSpaces.     Select(space  => ("parking space",       space. Id.ToString(), space. ParkingProductIds))).
                                                   Concat(this.ParkingSpaceGroups.Select(group  => ("parking space group", group. Id.ToString(), group. ParkingProductIds))))
                foreach (var productId in productIds)
                    if (!parkingProductIndex.ContainsKey(productId))
                        throw new ArgumentException($"The parking product '{productId}' of the {kind} '{ownerId}' is unknown at the parking operator '{Id}'!", nameof(ParkingProducts));

            foreach (var parkingSpaceGroup in this.ParkingSpaceGroups)
                foreach (var parkingSpaceId in parkingSpaceGroup.ParkingSpaceIds)
                    if (!parkingSpaceIndex.ContainsKey(parkingSpaceId))
                        throw new ArgumentException($"The parking space '{parkingSpaceId}' of the parking space group '{parkingSpaceGroup.Id}' is unknown at the parking operator '{Id}'!", nameof(ParkingSpaceGroups));

            // A parking space names only garages and sensors of its own operator.
            foreach (var parkingSpace in this.ParkingSpaces)
            {

                if (parkingSpace.ParkingGarageId.HasValue &&
                    !parkingGarageIndex.ContainsKey(parkingSpace.ParkingGarageId.Value))
                {
                    throw new ArgumentException($"The parking garage '{parkingSpace.ParkingGarageId}' of the parking space '{parkingSpace.Id}' is unknown at the parking operator '{Id}'!", nameof(ParkingSpaces));
                }

                foreach (var parkingSensorId in parkingSpace.ParkingSensorIds)
                    if (!parkingSensorIndex.ContainsKey(parkingSensorId))
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

            => parkingGarageIndex.TryGetValue(ParkingGarageId, out ParkingGarage);

        #endregion

        #region TryGetParkingSpace (ParkingSpaceId,  out ParkingSpace)

        /// <summary>
        /// Try to return the parking space with the given identification.
        /// </summary>
        public Boolean TryGetParkingSpace(ParkingSpace_Id                         ParkingSpaceId,
                                          [NotNullWhen(true)] out ParkingSpace?   ParkingSpace)

            => parkingSpaceIndex.TryGetValue(ParkingSpaceId, out ParkingSpace);

        #endregion

        #region TryGetParkingSensor(ParkingSensorId, out ParkingSensor)

        /// <summary>
        /// Try to return the parking sensor with the given identification.
        /// </summary>
        public Boolean TryGetParkingSensor(ParkingSensor_Id                         ParkingSensorId,
                                           [NotNullWhen(true)] out ParkingSensor?   ParkingSensor)

            => parkingSensorIndex.TryGetValue(ParkingSensorId, out ParkingSensor);

        #endregion

        #region TryGetParkingSpaceGroup(ParkingSpaceGroupId, out ParkingSpaceGroup)

        /// <summary>
        /// Try to return the parking space group with the given identification.
        /// </summary>
        public Boolean TryGetParkingSpaceGroup(ParkingSpaceGroup_Id                        ParkingSpaceGroupId,
                                               [NotNullWhen(true)] out ParkingSpaceGroup?  ParkingSpaceGroup)

            => parkingSpaceGroupIndex.TryGetValue(ParkingSpaceGroupId, out ParkingSpaceGroup);

        #endregion

        #region TryGetParkingProduct(ParkingProductId, out ParkingProduct)

        /// <summary>
        /// Try to return the parking product with the given identification.
        /// </summary>
        public Boolean TryGetParkingProduct(ParkingProduct_Id                        ParkingProductId,
                                            [NotNullWhen(true)] out ParkingProduct?  ParkingProduct)

            => parkingProductIndex.TryGetValue(ParkingProductId, out ParkingProduct);

        #endregion

        #region (internal) ShareRuntimeOfAll(Previous)

        /// <summary>
        /// Take the runtime status and internal data of the given previous version of this parking operator,
        /// and of every parking garage, parking space and parking sensor it had, by their identifications.
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
        /// with its parking garages, parking spaces and parking sensors.
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

            if (ParkingGarages.Length > 0)
                json.Add("parkingGarages",  new JArray(ParkingGarages.Select(parkingGarage => parkingGarage.ToJSON())));

            if (ParkingSpaces.Length > 0)
                json.Add("parkingSpaces",   new JArray(ParkingSpaces. Select(parkingSpace  => parkingSpace. ToJSON())));

            if (ParkingSensors.Length > 0)
                json.Add("parkingSensors",  new JArray(ParkingSensors.Select(parkingSensor => parkingSensor.ToJSON())));

            if (ParkingSpaceGroups.Length > 0)
                json.Add("parkingSpaceGroups", new JArray(ParkingSpaceGroups.Select(parkingSpaceGroup => parkingSpaceGroup.ToJSON())));

            if (ParkingProducts.Length > 0)
                json.Add("parkingProducts", new JArray(ParkingProducts.Select(parkingProduct => parkingProduct.ToJSON())));

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
        /// with its parking garages, parking spaces and parking sensors.
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


        #region (private static) Ordered<T>(Children, Id, Description)

        private static ImmutableArray<T> Ordered<T>(IEnumerable<T>?    Children,
                                                    Func<T, String>    Id,
                                                    String             Description)
        {

            var children = (Children ?? []).ToArray();

            if (children.Any(child => child is null))
                throw new ArgumentException($"The {Description}s must not contain null entries!");

            if (children.Select(Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != children.Length)
                throw new ArgumentException($"A {Description} must not be given twice!");

            return [.. children.OrderBy(Id, StringComparer.Ordinal)];

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
