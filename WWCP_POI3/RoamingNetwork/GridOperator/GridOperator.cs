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
    /// The admin status of a grid operator changed.
    /// </summary>
    public delegate Task OnGridOperatorAdminStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  GridOperator                               GridOperator,
                                                                  Timestamped<GridOperatorAdminStatusType>   OldStatus,
                                                                  Timestamped<GridOperatorAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of a grid operator changed.
    /// </summary>
    public delegate Task OnGridOperatorStatusChangedDelegate(DateTimeOffset                        Timestamp,
                                                             EventTracking_Id                      EventTrackingId,
                                                             GridOperator                          GridOperator,
                                                             Timestamped<GridOperatorStatusType>   OldStatus,
                                                             Timestamped<GridOperatorStatusType>   NewStatus);


    /// <summary>
    /// An operator of an electricity grid, embedded completely in its roaming network,
    /// with its grid connection points. A grid operator without keys does not take part:
    /// the roaming network operator maintains it, and charging station operators or parking
    /// operators may maintain its grid connection points.
    /// </summary>
    public sealed class GridOperator : AImmutableEMobilityEntity<GridOperator_Id,
                                                                 GridOperatorAdminStatusType,
                                                                 GridOperatorStatusType>,
                                       IEquatable<GridOperator>,
                                       IComparable<GridOperator>,
                                       IPOIParty
    {

        #region Properties

        /// <summary>
        /// The address of the headquarters.
        /// </summary>
        public Address?      Address               { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?          Homepage              { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?  HotlinePhoneNumber    { get; }

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>  SigningKeys     { get; }

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>  IdentityKeys    { get; }

        /// <summary>
        /// The grid connection points, in the order of their identifications.
        /// </summary>
        public ImmutableArray<GridConnectionPoint>  GridConnectionPoints    { get; }

        /// <summary>
        /// The energy meters of its grid connection points.
        /// </summary>
        public IEnumerable<EnergyMeter>  EnergyMeters
            => GridConnectionPoints.Select(gridConnectionPoint => gridConnectionPoint.EnergyMeter).OfType<EnergyMeter>();

        private readonly ImmutableDictionary<GridConnectionPoint_Id, GridConnectionPoint> gridConnectionPointIndex;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new grid operator.
        /// </summary>
        public GridOperator(GridOperator_Id                               Id,
                            I18NString?                                   Name                         = null,
                            I18NString?                                   Description                  = null,
                            Address?                                      Address                      = null,
                            URL?                                          Homepage                     = null,
                            PhoneNumber?                                  HotlinePhoneNumber           = null,
                            IEnumerable<GridConnectionPoint>?             GridConnectionPoints         = null,
                            IEnumerable<POIPublicKey>?                    SigningKeys                  = null,
                            IEnumerable<POIPublicKey>?                    IdentityKeys                 = null,

                            Timestamped<GridOperatorAdminStatusType>?     InitialAdminStatus           = null,
                            Timestamped<GridOperatorStatusType>?          InitialStatus                = null,
                            UInt16?                                       MaxAdminStatusScheduleSize   = null,
                            UInt16?                                       MaxStatusScheduleSize        = null,

                            String?                                       DataSource                   = null,
                            DateTimeOffset?                               Created                      = null,
                            DateTimeOffset?                               LastChange                   = null,

                            CustomDataNew?                                CustomData                   = null,
                            UserDefinedDictionary?                        InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<GridOperatorAdminStatusType>(Created ?? Timestamp.Now, GridOperatorAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<GridOperatorStatusType>     (Created ?? Timestamp.Now, GridOperatorStatusType.Available),
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

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the grid operator '{Id}'");

            var gridConnectionPoints = (GridConnectionPoints ?? []).ToArray();

            if (gridConnectionPoints.Any(gridConnectionPoint => gridConnectionPoint is null))
                throw new ArgumentException($"The grid connection points of the grid operator '{Id}' must not contain null entries!", nameof(GridConnectionPoints));

            // Grid connection point identifications compare without case.
            if (gridConnectionPoints.Select(gridConnectionPoint => gridConnectionPoint.Id).Distinct().Count() != gridConnectionPoints.Length)
                throw new ArgumentException($"A grid connection point of the grid operator '{Id}' must not be given twice!", nameof(GridConnectionPoints));

            this.GridConnectionPoints      = [.. gridConnectionPoints.OrderBy(gridConnectionPoint => gridConnectionPoint.Id.ToString(), StringComparer.Ordinal)];
            this.gridConnectionPointIndex  = this.GridConnectionPoints.ToImmutableDictionary(gridConnectionPoint => gridConnectionPoint.Id);

        }

        #endregion


        #region TryGetGridConnectionPoint(GridConnectionPointId, out GridConnectionPoint)

        /// <summary>
        /// Try to return the grid connection point with the given identification.
        /// </summary>
        public Boolean TryGetGridConnectionPoint(GridConnectionPoint_Id                         GridConnectionPointId,
                                                 [NotNullWhen(true)] out GridConnectionPoint?   GridConnectionPoint)

            => gridConnectionPointIndex.TryGetValue(GridConnectionPointId, out GridConnectionPoint);

        #endregion

        #region (internal) ShareRuntimeOfAll(Previous)

        /// <summary>
        /// Take the runtime status and internal data of the given previous version of this grid operator,
        /// and of the energy meters of its grid connection points, by their identifications.
        /// </summary>
        internal void ShareRuntimeOfAll(GridOperator Previous)
        {

            ShareRuntimeOf(Previous);

            var previousMeters = Previous.EnergyMeters.ToDictionary(energyMeter => energyMeter.Id);

            foreach (var energyMeter in EnergyMeters)
                if (previousMeters.TryGetValue(energyMeter.Id, out var previousMeter))
                    energyMeter.ShareRuntimeOf(previousMeter);

        }

        #endregion


        #region Status events

        private readonly Lock                                       statusEventsLock  = new ();
        private          OnGridOperatorAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnGridOperatorStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnGridOperatorAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnGridOperatorStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                            Timestamp,
                                                                   EventTracking_Id                          EventTrackingId,
                                                                   Timestamped<GridOperatorAdminStatusType>  NewStatus,
                                                                   Timestamped<GridOperatorAdminStatusType>  OldStatus,
                                                                   Context?                                  DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                       Timestamp,
                                                              EventTracking_Id                     EventTrackingId,
                                                              Timestamped<GridOperatorStatusType>  NewStatus,
                                                              Timestamped<GridOperatorStatusType>  OldStatus,
                                                              Context?                             DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this grid operator, as its roaming network embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",  Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",   DataSource);

            if (Address is not null)
                json.Add("address",      Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add("homepage",     Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",      HotlinePhoneNumber.Value.ToString());

            if (GridConnectionPoints.Length > 0)
                json.Add("gridConnectionPoints", new JArray(GridConnectionPoints.Select(gridConnectionPoint => gridConnectionPoint.ToJSON())));

            POIPartyKeys.AddTo(json, this);

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return the CBOR representation of this grid operator: the keys and values of its JSON representation.
        /// </summary>
        public CBORValue ToCBOR()

            => CBORJSON.ToCBOR(ToJSON());

        #endregion

        #region (static) TryParse(JSON, out GridOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a grid operator.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="GridOperator">The parsed grid operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                 JSON,
                                       [NotNullWhen(true)]  out GridOperator?  GridOperator,
                                       [NotNullWhen(false)] out String?        ErrorResponse)
        {

            GridOperator = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "grid operator identification",
                                         GridOperator_Id.TryParse,
                                         out GridOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, "name",         "grid operator name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, "description",  "grid operator description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, "dataSource",   "data source",                out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress(JSON,                                               out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "homepage",     "homepage",                   URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "hotline",      "hotline phone number",       PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<GridConnectionPoint>(JSON, "gridConnectionPoints", "gridConnectionPointIds", "grid connection points", GridConnectionPoint.TryParse, out var gridConnectionPoints, out ErrorResponse))
                    return false;

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

                GridOperator = new GridOperator(id,
                                                name,
                                                description,
                                                address,
                                                homepage,
                                                hotline,
                                                gridConnectionPoints,
                                                SigningKeys:   signingKeys,
                                                IdentityKeys:  identityKeys,
                                                DataSource:  dataSource,
                                                Created:     created,
                                                LastChange:  lastChange,
                                                CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                GridOperator   = null;
                ErrorResponse  = "The given JSON representation of a grid operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two grid operators for equality by their identifications.
        /// </summary>
        public static Boolean operator == (GridOperator? GridOperator1, GridOperator? GridOperator2)

            => ReferenceEquals(GridOperator1, GridOperator2) ||
               (GridOperator1 is not null && GridOperator1.Equals(GridOperator2));

        /// <summary>
        /// Compares two grid operators for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (GridOperator? GridOperator1, GridOperator? GridOperator2)

            => !(GridOperator1 == GridOperator2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is GridOperator gridOperator
                   ? CompareTo(gridOperator)
                   : throw new ArgumentException("The given object is not a grid operator!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(GridOperator? GridOperator)

            => GridOperator is null
                   ? throw new ArgumentNullException(nameof(GridOperator), "The given grid operator must not be null!")
                   : Id.CompareTo(GridOperator.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is GridOperator gridOperator &&
               Equals(gridOperator);

        /// <inheritdoc/>
        public Boolean Equals(GridOperator? GridOperator)

            => GridOperator is not null &&
               Id.Equals(GridOperator.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
