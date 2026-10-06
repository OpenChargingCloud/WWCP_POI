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

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Extension methods for charging station operators.
    /// </summary>
    public static class IChargingStationOperatorExtensions
    {

        #region ToJSON(this ChargingStationOperators, Skip = null, Take = null, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given enumeration of charging station operators.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// <param name="Skip">The optional number of charging station operators to skip.</param>
        /// <param name="Take">The optional number of charging station operators to return.</param>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a roaming network.</param>
        public static JArray ToJSON(this IEnumerable<ChargingStationOperator>                  ChargingStationOperators,
                                    UInt64?                                                    Skip                                      = null,
                                    UInt64?                                                    Take                                      = null,
                                    Boolean                                                    Embedded                                  = false,
                                    InfoStatus                                                 ExpandRoamingNetworkId                    = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandDataLicenses                        = InfoStatus.ShowIdOnly,
                                    CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null,
                                    CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                                    CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                                    CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null)


            => ChargingStationOperators?.Any() == true

                   ? new JArray(

                         ChargingStationOperators.
                             Where          (cso => cso is not null).
                             OrderBy        (cso => cso.Id).
                             SkipTakeFilter (Skip, Take).
                             SafeSelect     (cso => cso.ToJSON(Embedded,
                                                               ExpandRoamingNetworkId,
                                                               ExpandChargingPoolIds,
                                                               ExpandChargingStationIds,
                                                               ExpandEVSEIds,
                                                               ExpandBrandIds,
                                                               ExpandDataLicenses,
                                                               CustomChargingStationOperatorSerializer,
                                                               CustomChargingPoolSerializer,
                                                               CustomChargingStationSerializer,
                                                               CustomEVSESerializer)).
                             Where          (cso => cso is not null)

                     )

                   : [];

        #endregion


        #region ToJSON(this ChargingStationOperatorAdminStatus, Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<KeyValuePair<ChargingStationOperator_Id, IEnumerable<Timestamped<ChargingStationOperatorAdminStatusTypes>>>>  ChargingStationOperatorAdminStatus,
                                     UInt64?                                                                                                                        Skip         = null,
                                     UInt64?                                                                                                                        Take         = null,
                                     UInt64?                                                                                                                        HistorySize  = 1)

        {

            #region Initial checks

            if (ChargingStationOperatorAdminStatus is null || !ChargingStationOperatorAdminStatus.Any())
                return new JObject();

            var _ChargingStationOperatorAdminStatus = new Dictionary<ChargingStationOperator_Id, IEnumerable<Timestamped<ChargingStationOperatorAdminStatusTypes>>>();

            #endregion

            #region Maybe there are duplicate ChargingStationOperator identifications in the enumeration... take the newest one!

            foreach (var csostatus in Take.HasValue ? ChargingStationOperatorAdminStatus.Skip(Skip).Take(Take)
                                                    : ChargingStationOperatorAdminStatus.Skip(Skip))
            {

                if (!_ChargingStationOperatorAdminStatus.ContainsKey(csostatus.Key))
                    _ChargingStationOperatorAdminStatus.Add(csostatus.Key, csostatus.Value);

                else if (csostatus.Value.FirstOrDefault().Timestamp > _ChargingStationOperatorAdminStatus[csostatus.Key].FirstOrDefault().Timestamp)
                    _ChargingStationOperatorAdminStatus[csostatus.Key] = csostatus.Value;

            }

            #endregion

            return _ChargingStationOperatorAdminStatus.Count == 0

                   ? new JObject()

                   : new JObject(_ChargingStationOperatorAdminStatus.
                                     SafeSelect(statuslist => new JProperty(statuslist.Key.ToString(),
                                                                  new JObject(statuslist.Value.

                                                                              // Will filter multiple cso status having the exact same ISO 8601 timestamp!
                                                                              GroupBy          (tsv   => tsv.  Timestamp.ToISO8601()).
                                                                              Select           (group => group.First()).

                                                                              OrderByDescending(tsv   => tsv.Timestamp).
                                                                              Take             (HistorySize).
                                                                              Select           (tsv   => new JProperty(tsv.Timestamp.ToISO8601(),
                                                                                                                       tsv.Value.    ToString())))

                                                              )));

        }

        #endregion

        #region ToJSON(this ChargingStationOperatorStatus,      Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<KeyValuePair<ChargingStationOperator_Id, IEnumerable<Timestamped<ChargingStationOperatorStatusTypes>>>>  ChargingStationOperatorStatus,
                                     UInt64?                                                                                                                   Skip         = null,
                                     UInt64?                                                                                                                   Take         = null,
                                     UInt64?                                                                                                                   HistorySize  = 1)

        {

            #region Initial checks

            if (ChargingStationOperatorStatus is null || !ChargingStationOperatorStatus.Any())
                return new JObject();

            var _ChargingStationOperatorStatus = new Dictionary<ChargingStationOperator_Id, IEnumerable<Timestamped<ChargingStationOperatorStatusTypes>>>();

            #endregion

            #region Maybe there are duplicate ChargingStationOperator identifications in the enumeration... take the newest one!

            foreach (var csostatus in Take.HasValue ? ChargingStationOperatorStatus.Skip(Skip).Take(Take)
                                                    : ChargingStationOperatorStatus.Skip(Skip))
            {

                if (!_ChargingStationOperatorStatus.ContainsKey(csostatus.Key))
                    _ChargingStationOperatorStatus.Add(csostatus.Key, csostatus.Value);

                else if (csostatus.Value.FirstOrDefault().Timestamp > _ChargingStationOperatorStatus[csostatus.Key].FirstOrDefault().Timestamp)
                    _ChargingStationOperatorStatus[csostatus.Key] = csostatus.Value;

            }

            #endregion

            return _ChargingStationOperatorStatus.Count == 0

                   ? new JObject()

                   : new JObject(_ChargingStationOperatorStatus.
                                     SafeSelect(statuslist => new JProperty(statuslist.Key.ToString(),
                                                                  new JObject(statuslist.Value.

                                                                              // Will filter multiple cso status having the exact same ISO 8601 timestamp!
                                                                              GroupBy          (tsv   => tsv.  Timestamp.ToISO8601()).
                                                                              Select           (group => group.First()).

                                                                              OrderByDescending(tsv   => tsv.Timestamp).
                                                                              Take             (HistorySize).
                                                                              Select           (tsv   => new JProperty(tsv.Timestamp.ToISO8601(),
                                                                                                                       tsv.Value.    ToString())))

                                                              )));

        }

        #endregion

    }


    /// <summary>
    /// The Charging Station Operator (CSO) is responsible for operating charging pools,
    /// charging stations and EVSEs (power connectors), but is not neccessarily also the
    /// owner of all these devices.
    /// The Charging Station Operator delivers the locations, characteristics and real-time
    /// status information of its charging pools/-stations and EVSEs as Linked
    /// Open Data (LOD) to e-mobility service providers, navigation service
    /// providers and the public. For these delivered services (energy, parking, etc.) the
    /// operator will either be payed directly by the ev driver or by a contracted
    /// e-mobility service provider. The required pricing information can either be public
    /// information or part of B2B contracts.
    /// </summary>
    public class ChargingStationOperator : AEMobilityEntity<ChargingStationOperator_Id,
                                                            ChargingStationOperatorAdminStatusTypes,
                                                            ChargingStationOperatorStatusTypes>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String  JSONLDContext                                               = "https://open.charging.cloud/contexts/wwcp+json/chargingStationOperator";

        /// <summary>
        /// The default max size of the charging station operator admin status list.
        /// </summary>
        public const UInt16  DefaultMaxChargingStationOperatorAdminStatusScheduleSize    = 15;

        /// <summary>
        /// The default max size of the charging station operator (aggregated charging station) status list.
        /// </summary>
        public const UInt16  DefaultMaxChargingStationOperatorStatusScheduleSize         = 15;

        #endregion

        #region Properties

        public RoamingNetwork RoamingNetwork { get; }


        #region Logo

        private URL? logo;

        /// <summary>
        /// The logo of this evse operator.
        /// </summary>
        [Optional]
        public URL? Logo
        {

            get
            {
                return logo;
            }

            set
            {
                if (logo != value)
                    SetProperty(ref logo, value);
            }

        }

        #endregion

        #region Brands

        /// <summary>
        /// All brands registered for this charging station operator.
        /// </summary>
        [Optional, SlowData]
        public ReactiveSet<Brand>               Brands                           { get; } = new();

        #endregion

        #region Address

        private Address? address;

        /// <summary>
        /// The address of the operators headquarter.
        /// </summary>
        [Optional]
        public Address? Address
        {

            get
            {
                return address;
            }

            set
            {
                SetProperty(ref address, value);
            }

        }

        #endregion

        #region GeoLocation

        private GeoCoordinate? geoLocation;

        /// <summary>
        /// The geographical location of this operator.
        /// </summary>
        [Optional]
        public GeoCoordinate? GeoLocation
        {

            get
            {
                return geoLocation;
            }

            set
            {
                SetProperty(ref geoLocation, value);
            }

        }

        #endregion

        #region Telephone

        private PhoneNumber? telephone;

        /// <summary>
        /// The telephone number of the operator's (sales) office.
        /// </summary>
        [Optional]
        public PhoneNumber? Telephone
        {

            get
            {
                return telephone;
            }

            set
            {
                SetProperty(ref telephone, value);
            }

        }

        #endregion

        #region EMailAddress

        private SimpleEMailAddress? eMailAddress;

        /// <summary>
        /// The e-mail address of the operator's (sales) office.
        /// </summary>
        [Optional]
        public SimpleEMailAddress? EMailAddress
        {

            get
            {
                return eMailAddress;
            }

            set
            {
                SetProperty(ref eMailAddress, value);
            }

        }

        #endregion

        #region Homepage

        private URL? homepage;

        /// <summary>
        /// The homepage of this charging station operator.
        /// </summary>
        [Optional]
        public URL? Homepage
        {

            get
            {
                return homepage;
            }

            set
            {
                SetProperty(ref homepage, value);
            }

        }

        #endregion

        #region HotlinePhoneNumber

        private PhoneNumber? hotlinePhoneNumber;

        /// <summary>
        /// The telephone number of the Charging Station Operator hotline.
        /// </summary>
        [Optional]
        public PhoneNumber? HotlinePhoneNumber
        {

            get
            {
                return hotlinePhoneNumber;
            }

            set
            {
                SetProperty(ref hotlinePhoneNumber, value);
            }

        }

        #endregion

        #region TermsAndConditionsURL

        private URL? termsAndConditionsURL;

        /// <summary>
        /// The optional URL to terms and conditions for charging.
        /// </summary>
        [Optional]
        public URL? TermsAndConditionsURL
        {

            get
            {
                return termsAndConditionsURL;
            }

            set
            {
                SetProperty(ref termsAndConditionsURL, value);
            }

        }

        #endregion

        #region DataLicenses

        /// <summary>
        /// The license(s) of the charging station operator data.
        /// </summary>
        [Optional]
        public List<DataLicense> DataLicenses { get;} = [];


        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station operator (CSO) having the given
        /// charging station operator identification (CSO Id).
        /// </summary>
        /// <param name="Id">The unique identification of the Charging Station Operator.</param>
        /// <param name="RoamingNetwork">The associated roaming network.</param>
        /// <param name="Name">The official (multi-language) name of the EVSE Operator.</param>
        /// <param name="Description">An optional (multi-language) description of the EVSE Operator.</param>
        /// <param name="EVRoamingPartners">An enumeration of EV roaming partners.</param>
        /// 
        /// <param name="Configurator">A delegate to configure the new charging station operator after its creation.</param>
        public ChargingStationOperator(ChargingStationOperator_Id                             Id,
                                       RoamingNetwork                                         RoamingNetwork,
                                       I18NString?                                            Name                                   = null,
                                       I18NString?                                            Description                            = null,

                                       Timestamped<ChargingStationOperatorAdminStatusTypes>?  InitialAdminStatus                     = null,
                                       Timestamped<ChargingStationOperatorStatusTypes>?       InitialStatus                          = null,
                                       UInt16?                                                MaxAdminStatusScheduleSize             = DefaultMaxAdminStatusScheduleSize,
                                       UInt16?                                                MaxStatusScheduleSize                  = DefaultMaxStatusScheduleSize,

                                       String?                                                DataSource                             = null,
                                       DateTimeOffset?                                        Created                                = null,
                                       DateTimeOffset?                                        LastChange                             = null,

                                       CustomDataNew?                                         CustomData                             = null,
                                       UserDefinedDictionary?                                 InternalData                           = null)

            : base(Id,
                   Name ?? I18NString.Create("Charging Station Operator " + Id.ToString()),
                   Description,

                   InitialAdminStatus         ?? ChargingStationOperatorAdminStatusTypes.Operational,
                   InitialStatus              ?? ChargingStationOperatorStatusTypes.Available,
                   MaxAdminStatusScheduleSize ?? DefaultMaxChargingStationOperatorAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxChargingStationOperatorStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.RoamingNetwork = RoamingNetwork;

        }

        #endregion


        #region  Data/(Admin-)Status management

        #region OnData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data changed.
        /// </summary>
        public event OnChargingStationOperatorDataChangedDelegate?         OnDataChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingStationOperatorAdminStatusChangedDelegate?  OnAdminStatusChanged;

        /// <summary>
        /// An event fired whenever the dynamic status changed.
        /// </summary>
        public event OnChargingStationOperatorStatusChangedDelegate?       OnStatusChanged;

        #endregion


        #region (internal) UpdateData       (Timestamp, EventTrackingId, Sender, PropertyName, NewValue, OldValue, DataSource)

        /// <summary>
        /// Update the static data.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="Sender">The changed Charging Station Operator.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        internal async Task UpdateData(DateTimeOffset    Timestamp,
                                       EventTracking_Id  EventTrackingId,
                                       Object            Sender,
                                       String            PropertyName,
                                       Object?           OldValue,
                                       Object?           NewValue)
        {

            var onDataChanged = OnDataChanged;
            if (onDataChanged is not null)
                await onDataChanged(Timestamp,
                                    EventTrackingId,
                                    Sender as ChargingStationOperator,
                                    PropertyName,
                                    OldValue,
                                    NewValue);

        }

        #endregion

        #region (internal) UpdateAdminStatus(Timestamp, EventTrackingId, NewStatus, OldStatus, DataSource)

        /// <summary>
        /// Update the current admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="OldStatus">The old charging station admin status.</param>
        /// <param name="NewStatus">The new charging station admin status.</param>
        internal async Task UpdateAdminStatus(DateTimeOffset                                        Timestamp,
                                              EventTracking_Id                                      EventTrackingId,
                                              Timestamped<ChargingStationOperatorAdminStatusTypes>  OldStatus,
                                              Timestamped<ChargingStationOperatorAdminStatusTypes>  NewStatus)
        {

            var onAdminStatusChanged = OnAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp,
                                           EventTrackingId,
                                           this,
                                           OldStatus,
                                           NewStatus);

        }

        #endregion

        #region (internal) UpdateStatus     (Timestamp, EventTrackingId, NewStatus, OldStatus, DataSource)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal async Task UpdateStatus(DateTimeOffset                                   Timestamp,
                                         EventTracking_Id                                 EventTrackingId,
                                         Timestamped<ChargingStationOperatorStatusTypes>  OldStatus,
                                         Timestamped<ChargingStationOperatorStatusTypes>  NewStatus)
        {

            var onStatusChanged = OnStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp,
                                      EventTrackingId,
                                      this,
                                      OldStatus,
                                      NewStatus);

        }

        #endregion

        #endregion

        #region Charging Pools

        #region Data

        private readonly EntityHashSet<ChargingStationOperator, ChargingPool_Id, ChargingPool> chargingPools;

        /// <summary>
        /// Return an enumeration of all charging pools.
        /// </summary>
        public IEnumerable<ChargingPool> ChargingPools

            => chargingPools;

        #endregion

        #region OnChargingPoolData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data of any subordinated charging pool changed.
        /// </summary>
        public event OnChargingPoolDataChangedDelegate?         OnChargingPoolDataChanged;

        /// <summary>
        /// An event fired whenever the aggregated dynamic status of any subordinated charging pool changed.
        /// </summary>
        public event OnChargingPoolStatusChangedDelegate?       OnChargingPoolStatusChanged;

        /// <summary>
        /// An event fired whenever the aggregated dynamic status of any subordinated charging pool changed.
        /// </summary>
        public event OnChargingPoolAdminStatusChangedDelegate?  OnChargingPoolAdminStatusChanged;

        #endregion


        #region ChargingPoolAddition

        /// <summary>
        /// Called whenever an charging pool will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStationOperator, ChargingPool, Boolean> OnChargingPoolAddition

            => chargingPools.OnAddition;

        #endregion

        #region ChargingPoolUpdate

        /// <summary>
        /// Called whenever an charging pool will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStationOperator, ChargingPool, ChargingPool, Boolean> OnChargingPoolUpdate

            => chargingPools.OnUpdate;


        ///// <summary>
        ///// A delegate called whenever a charging pool was updated.
        ///// </summary>
        ///// <param name="Timestamp">The timestamp when the charging pool was updated.</param>
        ///// <param name="ChargingPool">The updated charging pool.</param>
        ///// <param name="OldChargingPool">The old charging pool.</param>
        ///// <param name="EventTrackingId">An optional unique event tracking identification for correlating this request with other events.</param>
        ///// <param name="CurrentChargingPoolId">An optional charging pool identification initiating this command/request.</param>
        //public delegate Task OnChargingPoolUpdatedDelegate(DateTime           Timestamp,
        //                                                   ChargingPool       ChargingPool,
        //                                                   ChargingPool       OldChargingPool,
        //                                                   EventTracking_Id?  EventTrackingId         = null,
        //                                                   ChargingPool_Id?   CurrentChargingPoolId   = null);

        ///// <summary>
        ///// An event fired whenever a charging pool was updated.
        ///// </summary>
        //public event OnChargingPoolUpdatedDelegate OnChargingPoolUpdated;

        #endregion

        #region ChargingPoolRemoval

        /// <summary>
        /// Called whenever an charging pool will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStationOperator, ChargingPool, Boolean> OnChargingPoolRemoval

            => chargingPools.OnRemoval;

        #endregion


        #region ChargingPoolIds                (IncludeChargingPools = null)

        /// <summary>
        /// Return an enumeration of all charging pool identifications.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        public IEnumerable<ChargingPool_Id> ChargingPoolIds(IncludeChargingPoolDelegate? IncludeChargingPools = null)
        {

            IncludeChargingPools ??= (chargingPool => true);

            return chargingPools.
                       Where (chargingPool => IncludeChargingPools(chargingPool)).
                       Select(chargingPool => chargingPool.Id);

        }

        #endregion

        #region ChargingPoolAdminStatus        (IncludeChargingPools = null)

        /// <summary>
        /// Return an enumeration of all charging pool admin status.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        public IEnumerable<ChargingPoolAdminStatus> ChargingPoolAdminStatus(IncludeChargingPoolDelegate? IncludeChargingPools = null)
        {

            IncludeChargingPools ??= (chargingPool => true);

            return chargingPools.
                       Where (chargingPool => IncludeChargingPools(chargingPool)).
                       Select(chargingPool => new ChargingPoolAdminStatus(chargingPool.Id,
                                                                          chargingPool.AdminStatus));
        }

        #endregion

        #region ChargingPoolAdminStatusSchedule(IncludeChargingPools = null, TimestampFilter  = null, StatusFilter = null, HistorySize = null)

        /// <summary>
        /// Return the admin status of all charging pools registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="AdminStatusFilter">An optional admin status value filter.</param>
        /// <param name="Skip">The number of admin status entries per pool to skip.</param>
        /// <param name="Take">The number of admin status entries per pool to return.</param>
        public IEnumerable<Tuple<ChargingPool_Id, IEnumerable<Timestamped<ChargingPoolAdminStatusType>>>>

            ChargingPoolAdminStatusSchedule(IncludeChargingPoolDelegate?                 IncludeChargingPools   = null,
                                            Func<DateTimeOffset,              Boolean>?  TimestampFilter        = null,
                                            Func<ChargingPoolAdminStatusType, Boolean>?  AdminStatusFilter      = null,
                                            UInt64?                                      Skip                   = null,
                                            UInt64?                                      Take                   = null)

        {

            IncludeChargingPools ??= (chargingPool => true);

            return chargingPools.
                         Where (chargingPool => IncludeChargingPools(chargingPool)).
                         Select(chargingPool => new Tuple<ChargingPool_Id, IEnumerable<Timestamped<ChargingPoolAdminStatusType>>>(
                                                    chargingPool.Id,
                                                    chargingPool.AdminStatusSchedule(TimestampFilter,
                                                                                     AdminStatusFilter,
                                                                                     Skip,
                                                                                     Take)));

        }

        #endregion

        #region ChargingPoolStatus             (IncludeChargingPools = null)

        /// <summary>
        /// Return an enumeration of all charging pool status.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        public IEnumerable<ChargingPoolStatus> ChargingPoolStatus(IncludeChargingPoolDelegate? IncludeChargingPools = null)
        {

            IncludeChargingPools ??= (chargingPool => true);

            return chargingPools.
                       Where (chargingPool => IncludeChargingPools  (chargingPool)).
                       Select(chargingPool => new ChargingPoolStatus(chargingPool.Id,
                                                                     chargingPool.Status));

        }

        #endregion

        #region ChargingPoolStatusSchedule     (IncludeChargingPools = null, TimestampFilter  = null, StatusFilter = null, HistorySize = null)

        /// <summary>
        /// Return the admin status of all charging pools registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="StatusFilter">An optional admin status value filter.</param>
        /// <param name="Skip">The number of status entries per pool to skip.</param>
        /// <param name="Take">The number of status entries per pool to return.</param>
        public IEnumerable<Tuple<ChargingPool_Id, IEnumerable<Timestamped<ChargingPoolStatusType>>>>

            ChargingPoolStatusSchedule(IncludeChargingPoolDelegate?            IncludeChargingPools   = null,
                                       Func<DateTimeOffset,         Boolean>?  TimestampFilter        = null,
                                       Func<ChargingPoolStatusType, Boolean>?  StatusFilter           = null,
                                       UInt64?                                 Skip                   = null,
                                       UInt64?                                 Take                   = null)

        {

            IncludeChargingPools ??= (chargingPool => true);

            return chargingPools.
                         Where (chargingPool => IncludeChargingPools(chargingPool)).
                         Select(chargingPool => new Tuple<ChargingPool_Id, IEnumerable<Timestamped<ChargingPoolStatusType>>>(
                                                    chargingPool.Id,
                                                    chargingPool.StatusSchedule(TimestampFilter,
                                                                                StatusFilter,
                                                                                Skip,
                                                                                Take)));

        }

        #endregion


        #region Connect(ChargingPool)

        private void Connect(ChargingPool ChargingPool)
        {

            ChargingPool.OnDataChanged                             += UpdateChargingPoolData;
            ChargingPool.OnAdminStatusChanged                      += UpdateChargingPoolAdminStatus;
            ChargingPool.OnStatusChanged                           += UpdateChargingPoolStatus;

            ChargingPool.OnChargingStationAddition.OnVoting        += (eventTrackingId, timestamp, userId, chargingPool, chargingStation, vote) => ChargingStationAddition.SendVoting(eventTrackingId, timestamp, userId, chargingPool, chargingStation, vote);
            ChargingPool.OnChargingStationAddition.OnNotification  += (eventTrackingId, timestamp, userId, chargingPool, chargingStation)       => {
                chargingStationLookup.TryAdd(chargingStation.Id, chargingStation);
                ChargingStationAddition.SendNotification(eventTrackingId, timestamp, userId, chargingPool, chargingStation);
            };
            ChargingPool.OnChargingStationDataChanged              += UpdateChargingStationData;
            ChargingPool.OnChargingStationAdminStatusChanged       += UpdateChargingStationAdminStatus;
            ChargingPool.OnChargingStationStatusChanged            += UpdateChargingStationStatus;
            ChargingPool.OnChargingStationRemoval. OnVoting        += (eventTrackingId, timestamp, userId, chargingPool, chargingStation, vote) => ChargingStationRemoval. SendVoting(eventTrackingId, timestamp, userId, chargingPool, chargingStation, vote);
            ChargingPool.OnChargingStationRemoval. OnNotification  += (eventTrackingId, timestamp, userId, chargingPool, chargingStation)       => {
                chargingStationLookup.TryRemove(chargingStation.Id, out _);
                ChargingStationRemoval.SendNotification(eventTrackingId, timestamp, userId, chargingPool, chargingStation);
            };

            ChargingPool.OnEVSEDataChanged                         += UpdateEVSEData;
            ChargingPool.OnEVSEAdminStatusChanged                  += UpdateEVSEAdminStatus;
            ChargingPool.OnEVSEStatusChanged                       += UpdateEVSEStatus;

        }

        #endregion

        #region AddChargingPool           (ChargingPool,                             OnSuccess       = null, OnError = null, ...)

        /// <summary>
        /// Add a new charging pool.
        /// </summary>
        /// <param name="ChargingPool">A new charging pool.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to be called after the successful addition of the charging pool.</param>
        /// <param name="OnError">An optional delegate to be called whenever the addition of the new charging pool failed.</param>
        /// 
        /// <param name="SkipAddedNotifications">Whether to skip sending the 'OnAdded' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddChargingPoolResult> AddChargingPool(ChargingPool                                                       ChargingPool,

                                                                 Action<ChargingPool,                           EventTracking_Id>?  OnSuccess                      = null,
                                                                 Action<ChargingStationOperator, ChargingPool, EventTracking_Id>?  OnError                        = null,

                                                                 Boolean                                                             SkipAddedNotifications         = false,
                                                                 Func<ChargingStationOperator_Id, ChargingPool_Id, Boolean>?         AllowInconsistentOperatorIds   = null,
                                                                 EventTracking_Id?                                                   EventTrackingId                = null,
                                                                 User_Id?                                                            CurrentUserId                  = null)
        {

            #region Initial checks

            EventTrackingId              ??= EventTracking_Id.New;
            AllowInconsistentOperatorIds ??= ((chargingStationOperatorId, chargingPoolId) => false);

            if (ChargingPool.Id.OperatorId != this.Id && !AllowInconsistentOperatorIds(this.Id, ChargingPool.Id))
                return AddChargingPoolResult.ArgumentError(
                           ChargingPool,
                           $"The operator identification of the given charging pool '{ChargingPool.Id.OperatorId}' is invalid!".ToI18NString(),
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            #endregion


            if (chargingPools.TryAdd(ChargingPool,
                                     Connect,
                                     EventTrackingId,
                                     CurrentUserId).Result == CommandResult.Success)
            {

                //ToDo: Persistency
                await Task.Delay(1);

                OnSuccess?.Invoke(ChargingPool,
                                  EventTrackingId);

                return AddChargingPoolResult.Success(
                           ChargingPool,
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            }

            OnError?.Invoke(this,
                            ChargingPool,
                            EventTrackingId);

            return AddChargingPoolResult.Error(
                       ChargingPool,
                       "Could not add the given charging pool!".ToI18NString(),
                       EventTrackingId,
                       Id,
                       this,
                       this
                   );

        }

        #endregion

        #region AddChargingPoolIfNotExists(ChargingPool,                             OnSuccess       = null,                 ...)

        /// <summary>
        /// Add a new charging pool, but do not fail when this charging pool already exists.
        /// </summary>
        /// <param name="ChargingPool">A new charging pool.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to be called after the successful addition of the charging pool.</param>
        /// 
        /// <param name="SkipAddedNotifications">Whether to skip sending the 'OnAdded' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddChargingPoolResult> AddChargingPoolIfNotExists(ChargingPool                                                ChargingPool,

                                                                            Action<ChargingPool, EventTracking_Id>?                     OnSuccess                      = null,

                                                                            Boolean                                                      SkipAddedNotifications         = false,
                                                                            Func<ChargingStationOperator_Id, ChargingPool_Id, Boolean>?  AllowInconsistentOperatorIds   = null,
                                                                            EventTracking_Id?                                            EventTrackingId                = null,
                                                                            User_Id?                                                     CurrentUserId                  = null)
        {

            #region Initial checks

            EventTrackingId              ??= EventTracking_Id.New;
            AllowInconsistentOperatorIds ??= ((chargingStationOperatorId, chargingPoolId) => false);

            if (ChargingPool.Id.OperatorId != Id && !AllowInconsistentOperatorIds(Id, ChargingPool.Id))
                return AddChargingPoolResult.ArgumentError(
                           ChargingPool,
                           $"The operator identification of the given charging pool '{ChargingPool.Id.OperatorId}' is invalid!".ToI18NString(),
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            #endregion

            if (chargingPools.TryAdd(ChargingPool,
                                     Connect,
                                     EventTrackingId,
                                     CurrentUserId).Result == CommandResult.Success)
            {

                //ToDo: Persistency
                await Task.Delay(1);

                OnSuccess?.Invoke(ChargingPool,
                                  EventTrackingId);

                return AddChargingPoolResult.Success(
                           ChargingPool,
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            }

            return AddChargingPoolResult.NoOperation(
                       ChargingPool,
                       EventTrackingId,
                       Id,
                       this,
                       this
                   );

        }

        #endregion

        #region AddOrUpdateChargingPool   (ChargingPool,   OnAdditionSuccess = null, OnUpdateSuccess = null, OnError = null, ...)

        /// <summary>
        /// Add a new or update an existing charging pool.
        /// </summary>
        /// <param name="ChargingPool">A new or updated charging pool.</param>
        /// 
        /// <param name="OnAdditionSuccess">An optional delegate to be called after the successful addition of the charging pool.</param>
        /// <param name="OnUpdateSuccess">An optional delegate to be called after the successful update of the charging pool.</param>
        /// <param name="OnError">An optional delegate to be called whenever the addition of the new charging pool failed.</param>
        /// 
        /// <param name="SkipAddOrUpdatedUpdatedNotifications">Whether to skip sending the 'OnAddedOrUpdated' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddOrUpdateChargingPoolResult> AddOrUpdateChargingPool(ChargingPool                                                       ChargingPool,

                                                                                 Action<ChargingPool,                           EventTracking_Id>?  OnAdditionSuccess                      = null,
                                                                                 Action<ChargingPool,            ChargingPool, EventTracking_Id>?  OnUpdateSuccess                        = null,
                                                                                 Action<ChargingStationOperator, ChargingPool, EventTracking_Id>?  OnError                                = null,

                                                                                 Boolean                                                             SkipAddOrUpdatedUpdatedNotifications   = false,
                                                                                 Func<ChargingStationOperator_Id, ChargingPool_Id, Boolean>?         AllowInconsistentOperatorIds           = null,
                                                                                 EventTracking_Id?                                                   EventTrackingId                        = null,
                                                                                 User_Id?                                                            CurrentUserId                          = null)
        {

            #region Initial checks

            EventTrackingId              ??= EventTracking_Id.New;
            AllowInconsistentOperatorIds ??= ((chargingStationOperatorId, chargingPoolId) => false);

            if (ChargingPool.Id.OperatorId != this.Id && !AllowInconsistentOperatorIds(this.Id, ChargingPool.Id))
                return AddOrUpdateChargingPoolResult.ArgumentError(
                           ChargingPool,
                           $"The operator identification of the given charging pool '{ChargingPool.Id.OperatorId}' is invalid!".ToI18NString(),
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            #endregion


            if (chargingPools.TryGet(ChargingPool.Id, out var existingChargingPool) &&
                existingChargingPool is not null)
            {

                //var xx1 = existingChargingPool.Equals(ChargingPool);

                var xx2 = existingChargingPool == ChargingPool; //FalseFriend!!!

                if (chargingPools.TryUpdate(ChargingPool.Id,
                                            ChargingPool,
                                            existingChargingPool,
                                            EventTrackingId,
                                            CurrentUserId))
                {

                    //ToDo: Persistency
                    await Task.Delay(1);

                    Connect(ChargingPool);

                    OnUpdateSuccess?.Invoke(ChargingPool,
                                            existingChargingPool,
                                            EventTrackingId);

                    return AddOrUpdateChargingPoolResult.Updated(
                               ChargingPool,
                               EventTrackingId,
                               Id,
                               this,
                               this
                           );

                }
                else
                {

                    OnError?.Invoke(this,
                                    ChargingPool,
                                    EventTrackingId);

                    return AddOrUpdateChargingPoolResult.Error(
                               ChargingPool,
                               "Error!".ToI18NString(),
                               EventTrackingId,
                               Id,
                               this,
                               this
                           );

                }

            }

            else
            {

                if (chargingPools.TryAdd(ChargingPool,
                                         Connect,
                                         EventTrackingId,
                                         CurrentUserId).Result == CommandResult.Success)
                {

                    //ToDo: Persistency
                    await Task.Delay(1);

                    OnAdditionSuccess?.Invoke(ChargingPool,
                                              EventTrackingId);

                    return AddOrUpdateChargingPoolResult.Added(
                               ChargingPool,
                               EventTrackingId,
                               Id,
                               this,
                               this
                           );

                }
                else
                {

                    OnError?.Invoke(this,
                                    ChargingPool,
                                    EventTrackingId);

                    return AddOrUpdateChargingPoolResult.Error(
                               ChargingPool,
                               "Error!".ToI18NString(),
                               EventTrackingId,
                               Id,
                               this,
                               this
                           );

                }

            }

        }

        #endregion

        #region UpdateChargingPool        (ChargingPool,                             OnUpdateSuccess = null, OnError = null, ...)

        /// <summary>
        /// Update the given charging pool.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// 
        /// <param name="OnUpdateSuccess">An optional delegate to be called after the successful update of the charging pool.</param>
        /// <param name="OnError">An optional delegate to be called whenever the update of the new charging pool failed.</param>
        /// 
        /// <param name="SkipUpdatedNotifications">Whether to skip sending the 'OnUpdated' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<UpdateChargingPoolResult> UpdateChargingPool(ChargingPool                                                       ChargingPool,

                                                                       Action<ChargingPool,            ChargingPool, EventTracking_Id>?  OnUpdateSuccess                = null,
                                                                       Action<ChargingStationOperator, ChargingPool, EventTracking_Id>?  OnError                        = null,

                                                                       Boolean                                                             SkipUpdatedNotifications       = false,
                                                                       Func<ChargingStationOperator_Id, ChargingPool_Id, Boolean>?         AllowInconsistentOperatorIds   = null,
                                                                       EventTracking_Id?                                                   EventTrackingId                = null,
                                                                       User_Id?                                                            CurrentUserId                  = null)
        {

            var start            = Timestamp.Now;
            var eventTrackingId  = EventTrackingId ?? EventTracking_Id.New;

            if (!TryGetChargingPoolById(ChargingPool.Id, out var OldChargingPool))
                return UpdateChargingPoolResult.ArgumentError(
                           ChargingPool:              ChargingPool,
                           Description:               $"The given charging pool '{ChargingPool.Id}' does not exists in this API!".ToI18NString(),
                           EventTrackingId:           eventTrackingId,
                           SenderId:                  Id,
                           Sender:                    this,
                           ChargingStationOperator:   this,
                           Warnings:                  null,
                           Runtime:                   Timestamp.Now - start
                       );

            //if (ChargingPool.API is not null && ChargingPool.API != this)
            //    return UpdateChargingPoolResult.ArgumentError(ChargingPool,
            //                                                  eventTrackingId,
            //                                                  nameof(ChargingPool.API),
            //                                                  "The given charging pool is not attached to this API!");

            //ChargingPool.API = this;


            //await WriteToDatabaseFile(updateChargingPool_MessageType,
            //                          ChargingPool.ToJSON(),
            //                          eventTrackingId,
            //                          CurrentChargingPoolId);

            //chargingPools.TryRemove(OldChargingPool.Id,
            //                        out _,
            //                        EventTrackingId,
            //                        CurrentUserId);

            ////ChargingPool.CopyAllLinkedDataFrom(OldChargingPool);
            //chargingPools.TryAdd(ChargingPool,
            //                     EventTrackingId,
            //                     CurrentUserId);

            if (chargingPools.TryUpdate(OldChargingPool.Id,
                                        ChargingPool,
                                        OldChargingPool,
                                        eventTrackingId,
                                        CurrentUserId))
            {

                //ToDo: Persistency

                await UpdateChargingPoolData(
                          Timestamp:         Timestamp.Now,
                          EventTrackingId:   eventTrackingId,
                          ChargingPool:      ChargingPool,
                          PropertyName:      null,
                          NewValue:          null,
                          OldValue:          null,
                          DataSource:        null
                      );

                OnUpdateSuccess?.Invoke(
                    ChargingPool,
                    ChargingPool,
                    eventTrackingId
                );

                return UpdateChargingPoolResult.Success(
                           ChargingPool:              ChargingPool,
                           EventTrackingId:           eventTrackingId,
                           SenderId:                  Id,
                           Sender:                    this,
                           ChargingStationOperator:   this,
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   Timestamp.Now - start
                       );

            }

            //var OnChargingPoolUpdatedLocal = OnChargingPoolUpdated;
            //if (OnChargingPoolUpdatedLocal is not null)
            //    await OnChargingPoolUpdatedLocal.Invoke(Timestamp.Now,
            //                                            ChargingPool,
            //                                            OldChargingPool,
            //                                            eventTrackingId, 
            //                                            CurrentChargingPoolId);

            //if (!SkipChargingPoolUpdatedNotifications)
            //    await SendNotifications(ChargingPool,
            //                            updateChargingPool_MessageType,
            //                            OldChargingPool,
            //                            eventTrackingId,
            //                            CurrentChargingPoolId);

            OnError?.Invoke(this,
                            ChargingPool,
                            eventTrackingId);

            return UpdateChargingPoolResult.Error(
                       ChargingPool:              ChargingPool,
                       Description:               I18NString.Create("Could not be updated!"),
                       EventTrackingId:           eventTrackingId,
                       SenderId:                  Id,
                       Sender:                    this,
                       ChargingStationOperator:   this,
                       Warnings:                  null,
                       Runtime:                   Timestamp.Now - start
                   );

        }

        #endregion

        #region UpdateChargingPool        (ChargingPoolId, UpdateDelegate,           OnUpdateSuccess = null, OnError = null, ...)

        /// <summary>
        /// Update the given charging pool.
        /// </summary>
        /// <param name="ChargingPoolId">A charging pool identification.</param>
        /// <param name="UpdateDelegate">A delegate for updating the given charging pool.</param>
        /// 
        /// <param name="OnUpdateSuccess">An optional delegate to be called after the successful update of the charging pool.</param>
        /// <param name="OnError">An optional delegate to be called whenever the update of the new charging pool failed.</param>
        /// 
        /// <param name="SkipUpdatedNotifications">Whether to skip sending the 'OnUpdated' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<UpdateChargingPoolResult> UpdateChargingPool(ChargingPool_Id                                                     ChargingPoolId,
                                                                       Action<ChargingPool>                                               UpdateDelegate,

                                                                       Action<ChargingPool,            ChargingPool, EventTracking_Id>?  OnUpdateSuccess                = null,
                                                                       Action<ChargingStationOperator, ChargingPool, EventTracking_Id>?  OnError                        = null,

                                                                       Boolean                                                             SkipUpdatedNotifications       = false,
                                                                       Func<ChargingStationOperator_Id, ChargingPool_Id, Boolean>?         AllowInconsistentOperatorIds   = null,
                                                                       EventTracking_Id?                                                   EventTrackingId                = null,
                                                                       User_Id?                                                            CurrentUserId                  = null)
        {

            EventTrackingId ??= EventTracking_Id.New;

            if (!chargingPools.TryRemove(ChargingPoolId,
                                         out var oldChargingPool,
                                         EventTrackingId,
                                         CurrentUserId) ||
                oldChargingPool is null)
            {

                return UpdateChargingPoolResult.ArgumentError(
                           ChargingPoolId,
                           $"The given charging pool '{ChargingPoolId}' does not exists!".ToI18NString(),
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            }

            //if (ChargingPool.API is not null && ChargingPool.API != this)
            //    return UpdateChargingPoolResult.ArgumentError(ChargingPool,
            //                                                  eventTrackingId,
            //                                                  nameof(ChargingPool.API),
            //                                                  "The given charging pool is not attached to this API!");

            //ChargingPool.API = this;


            //await WriteToDatabaseFile(updateChargingPool_MessageType,
            //                          ChargingPool.ToJSON(),
            //                          eventTrackingId,
            //                          CurrentChargingPoolId);

            var newChargingPool = oldChargingPool.Clone();
            Connect(newChargingPool);
            UpdateDelegate(newChargingPool);

            //ChargingPool.CopyAllLinkedDataFrom(OldChargingPool);
            chargingPools.TryAdd(newChargingPool,
                                 EventTrackingId,
                                 CurrentUserId);

            OnUpdateSuccess?.Invoke(newChargingPool,
                                    oldChargingPool,
                                    EventTrackingId);

            //var OnChargingPoolUpdatedLocal = OnChargingPoolUpdated;
            //if (OnChargingPoolUpdatedLocal is not null)
            //    await OnChargingPoolUpdatedLocal.Invoke(Timestamp.Now,
            //                                            ChargingPool,
            //                                            OldChargingPool,
            //                                            eventTrackingId, 
            //                                            CurrentChargingPoolId);

            //if (!SkipChargingPoolUpdatedNotifications)
            //    await SendNotifications(ChargingPool,
            //                            updateChargingPool_MessageType,
            //                            OldChargingPool,
            //                            eventTrackingId,
            //                            CurrentChargingPoolId);

            return UpdateChargingPoolResult.Success(newChargingPool,
                                                    EventTrackingId);

        }

        #endregion

        #region RemoveChargingPool        (ChargingPoolId,                           OnRemoveSuccess = null, OnError = null, ...)

        /// <summary>
        /// Remove the given charging pool.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of the charging pool.</param>
        /// 
        /// <param name="OnRemoveSuccess">An optional delegate to be called after the successful removal of the charging pool.</param>
        /// <param name="OnError">An optional delegate to be called whenever the removal of the new charging pool failed.</param>
        /// 
        /// <param name="SkipRemovedNotifications">Whether to skip sending the 'OnRemoved' event.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<DeleteChargingPoolResult> RemoveChargingPool(ChargingPool_Id                                      ChargingPoolId,

                                                                       Action<ChargingPool,            EventTracking_Id>?  OnRemoveSuccess                = null,
                                                                       Action<ChargingStationOperator, EventTracking_Id>?  OnError                        = null,

                                                                       Boolean                                              SkipRemovedNotifications       = false,
                                                                       EventTracking_Id?                                    EventTrackingId                = null,
                                                                       User_Id?                                             CurrentUserId                  = null)
        {

            EventTrackingId ??= EventTracking_Id.New;

            if (chargingPools.TryRemove(ChargingPoolId,
                                        out var chargingPool,
                                        EventTrackingId,
                                        null) &&
                chargingPool is not null)
            {

                return DeleteChargingPoolResult.Success(
                           chargingPool,
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            }

            return DeleteChargingPoolResult.ArgumentError(
                       ChargingPoolId,
                       "error".ToI18NString(),
                       EventTrackingId,
                       Id,
                       this,
                       this
                   );

        }

        #endregion


        #region ChargingPoolExists(ChargingPool)

        /// <summary>
        /// Check if the given ChargingPool is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        public Boolean ChargingPoolExists(ChargingPool ChargingPool)

            => ChargingPools.Contains(ChargingPool);

        #endregion

        #region ChargingPoolExists(ChargingPoolId)

        /// <summary>
        /// Determines whether the given user identification exists within this API.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of an user.</param>
        public Boolean ChargingPoolExists(ChargingPool_Id ChargingPoolId)

            => ChargingPoolId.IsNotNullOrEmpty &&
               chargingPools.ContainsId(ChargingPoolId);

        /// <summary>
        /// Determines whether the given user identification exists within this API.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of an user.</param>
        public Boolean ChargingPoolExists(ChargingPool_Id? ChargingPoolId)

            => ChargingPoolId.HasValue &&
               ChargingPoolId.Value.IsNotNullOrEmpty &&
               chargingPools.ContainsId(ChargingPoolId.Value);

        #endregion

        #region GetChargingPoolById(ChargingPoolId)

        public ChargingPool? GetChargingPoolById(ChargingPool_Id ChargingPoolId)

            => chargingPools.GetById(ChargingPoolId);

        #endregion

        #region TryGetChargingPoolById(ChargingPoolId, out ChargingPool)

        /// <summary>
        /// Try to get the charging pool having the given unique identification.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
        /// <param name="ChargingPool">The charging pool.</param>
        public Boolean TryGetChargingPoolById(ChargingPool_Id                         ChargingPoolId,
                                              [NotNullWhen(true)] out ChargingPool?  ChargingPool)
        {

            if (!ChargingPoolId.IsNullOrEmpty &&
                chargingPools.TryGet(ChargingPoolId, out var chargingPool))
            {
                ChargingPool = chargingPool;
                return true;
            }

            ChargingPool = null;
            return false;

        }

        /// <summary>
        /// Try to get the charging pool having the given unique identification.
        /// </summary>
        /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
        /// <param name="ChargingPool">The charging pool.</param>
        public Boolean TryGetChargingPoolById(ChargingPool_Id?                        ChargingPoolId,
                                              [NotNullWhen(true)] out ChargingPool?  ChargingPool)
        {

            if (ChargingPoolId.IsNotNullOrEmpty() &&
               chargingPools.TryGet(ChargingPoolId!.Value, out var chargingPool))
            {
                ChargingPool = chargingPool;
                return true;
            }

            ChargingPool = null;
            return false;

        }

        #endregion

        #region TryGetChargingPoolByStationId(ChargingStationId, out ChargingPool)

        public Boolean TryGetChargingPoolByStationId(ChargingStation_Id                      ChargingStationId,
                                                     [NotNullWhen(true)] out ChargingPool?  ChargingPool)
        {

            foreach (var chargingPool in chargingPools)
            {
                if (chargingPool.TryGetChargingStationById(ChargingStationId, out var chargingStation))
                {
                    ChargingPool = chargingPool;
                    return true;
                }
            }

            ChargingPool = null;
            return false;

        }

        #endregion


        #region SetChargingPoolAdminStatus(ChargingPoolId, NewStatus)

        public void SetChargingPoolAdminStatus(ChargingPool_Id                           ChargingPoolId,
                                               Timestamped<ChargingPoolAdminStatusType>  NewStatus,
                                               Boolean                                   SendUpstream = false)
        {

            if (TryGetChargingPoolById(ChargingPoolId, out var chargingPool))
            {
                chargingPool.AdminStatus = NewStatus;
            }

        }

        #endregion

        #region SetChargingPoolAdminStatus(ChargingPoolId, NewStatus, Timestamp)

        public void SetChargingPoolAdminStatus(ChargingPool_Id              ChargingPoolId,
                                               ChargingPoolAdminStatusType  NewStatus,
                                               DateTimeOffset               Timestamp)
        {

            if (TryGetChargingPoolById(ChargingPoolId, out var chargingPool))
            {
                chargingPool.AdminStatus = new Timestamped<ChargingPoolAdminStatusType>(
                                               Timestamp,
                                               NewStatus
                                           );
            }

        }

        #endregion

        #region SetChargingPoolAdminStatus(ChargingPoolId, StatusList, ChangeMethod = ChangeMethods.Replace)

        public void SetChargingPoolAdminStatus(ChargingPool_Id                                        ChargingPoolId,
                                               IEnumerable<Timestamped<ChargingPoolAdminStatusType>>  StatusList,
                                               ChangeMethods                                          ChangeMethod  = ChangeMethods.Replace)
        {

            if (TryGetChargingPoolById(ChargingPoolId, out var chargingPool))
            {
                chargingPool.SetAdminStatus(
                    StatusList,
                    ChangeMethod
                );
            }

        }

        #endregion


        #region (internal) UpdateChargingPoolData       (Timestamp, EventTrackingId, ChargingPool, PropertyName = null, NewValue = null, OldValue = null, DataSource = null)

        /// <summary>
        /// Update the data of an charging pool.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargingPool">The changed charging pool.</param>
        /// <param name="PropertyName">The name of the changed property, if any specific.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        /// <param name="OldValue">The optional old value of the changed property.</param>
        /// <param name="DataSource">An optional data source or context for the data change.</param>
        internal async Task UpdateChargingPoolData(DateTimeOffset    Timestamp,
                                                   EventTracking_Id  EventTrackingId,
                                                   ChargingPool     ChargingPool,
                                                   String?           PropertyName   = null,
                                                   Object?           NewValue       = null,
                                                   Object?           OldValue       = null,
                                                   Context?          DataSource     = null)
        {

            var onChargingPoolDataChanged = OnChargingPoolDataChanged;
            if (onChargingPoolDataChanged is not null)
                await onChargingPoolDataChanged(Timestamp,
                                                EventTrackingId,
                                                ChargingPool,
                                                PropertyName,
                                                NewValue,
                                                OldValue,
                                                DataSource);

        }

        #endregion

        #region (internal) UpdateChargingPoolAdminStatus(Timestamp, EventTrackingId, ChargingPool, NewStatus, OldStatus = null, DataSource = null)

        /// <summary>
        /// Update a charging pool admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargingPool">The updated charging pool.</param>
        /// <param name="NewStatus">The new charging pool status.</param>
        /// <param name="OldStatus">The old charging pool status.</param>
        /// <param name="DataSource">An optional data source or context for the admin status update.</param>
        internal async Task UpdateChargingPoolAdminStatus(DateTimeOffset                             Timestamp,
                                                          EventTracking_Id                           EventTrackingId,
                                                          ChargingPool                              ChargingPool,
                                                          Timestamped<ChargingPoolAdminStatusType>   NewStatus,
                                                          Timestamped<ChargingPoolAdminStatusType>?  OldStatus    = null,
                                                          Context?                                   DataSource   = null)
        {

            var onChargingPoolAdminStatusChanged = OnChargingPoolAdminStatusChanged;
            if (onChargingPoolAdminStatusChanged is not null)
                await onChargingPoolAdminStatusChanged(Timestamp,
                                                       EventTrackingId,
                                                       ChargingPool,
                                                       NewStatus,
                                                       OldStatus,
                                                       DataSource);

        }

        #endregion

        #region (internal) UpdateChargingPoolStatus     (Timestamp, EventTrackingId, ChargingPool, NewStatus, OldStatus = null, DataSource = null)

        /// <summary>
        /// Update a charging pool status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargingPool">The updated charging pool.</param>
        /// <param name="NewStatus">The new charging pool status.</param>
        /// <param name="OldStatus">The optional old charging pool status.</param>
        /// <param name="DataSource">An optional data source or context for the admin status update.</param>
        internal async Task UpdateChargingPoolStatus(DateTimeOffset                        Timestamp,
                                                     EventTracking_Id                      EventTrackingId,
                                                     ChargingPool                         ChargingPool,
                                                     Timestamped<ChargingPoolStatusType>   NewStatus,
                                                     Timestamped<ChargingPoolStatusType>?  OldStatus    = null,
                                                     Context?                              DataSource   = null)
        {

            var onChargingPoolStatusChanged = OnChargingPoolStatusChanged;
            if (onChargingPoolStatusChanged is not null)
                await onChargingPoolStatusChanged(Timestamp,
                                                  EventTrackingId,
                                                  ChargingPool,
                                                  NewStatus,
                                                  OldStatus,
                                                  DataSource);

        }

        #endregion

        #endregion

        //ToDo: Charging Pool Groups

        #region Charging Stations

        #region Data

        private readonly ConcurrentDictionary<ChargingStation_Id, ChargingStation> chargingStationLookup = new();

        /// <summary>
        /// Return an enumeration of all charging stations.
        /// </summary>
        public IEnumerable<ChargingStation> ChargingStations

            => chargingStationLookup.Values;

        #endregion

        #region ChargingStationAddition

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingPool, ChargingStation, Boolean> ChargingStationAddition;

        /// <summary>
        /// Called whenever a charging station will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingPool, ChargingStation, Boolean> OnChargingStationAddition

            => ChargingStationAddition;

        #endregion

        #region ChargingStationUpdate

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingPool, ChargingStation, ChargingStation, Boolean> ChargingStationUpdate;

        /// <summary>
        /// Called whenever a charging station will be or was updated.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingPool, ChargingStation, ChargingStation, Boolean> OnChargingStationUpdate

            => ChargingStationUpdate;

        #endregion

        #region ChargingStationRemoval

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingPool, ChargingStation, Boolean> ChargingStationRemoval;

        /// <summary>
        /// Called whenever a charging station will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingPool, ChargingStation, Boolean> OnChargingStationRemoval

            => ChargingStationRemoval;

        #endregion


        #region ChargingStationIds                 (IncludeChargingStations = null)

        /// <summary>
        /// Return an enumeration of all charging station identifications.
        /// </summary>
        /// <param name="IncludeChargingStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStation_Id> ChargingStationIds(IncludeChargingStationDelegate? IncludeChargingStations = null)
        {

            IncludeChargingStations ??= (chargingStation => true);

            return chargingStationLookup.Values.
                       Where (chargingStation => IncludeChargingStations(chargingStation)).
                       Select(chargingStation => chargingStation.Id);

        }

        #endregion

        #region ChargingStationAdminStatus         (IncludeChargingStations = null)

        /// <summary>
        /// Return an enumeration of all charging station admin status.
        /// </summary>
        /// <param name="IncludeChargingStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStationAdminStatus> ChargingStationAdminStatus(IncludeChargingStationDelegate? IncludeChargingStations = null)
        {

            IncludeChargingStations ??= (chargingStation => true);

            return chargingStationLookup.Values.
                       Where (chargingStation => IncludeChargingStations(chargingStation)).
                       Select(chargingStation => new ChargingStationAdminStatus(chargingStation.Id,
                                                                                chargingStation.AdminStatus));

        }

        #endregion

        #region ChargingStationAdminStatusSchedule (IncludeChargingStations = null, TimestampFilter  = null, StatusFilter = null, HistorySize = null)

        /// <summary>
        /// Return the admin status of all charging stations registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingStations">An optional delegate for filtering charging stations.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="AdminStatusFilter">An optional admin status value filter.</param>
        /// <param name="Skip">The number of admin status entries per station to skip.</param>
        /// <param name="Take">The number of admin status entries per station to return.</param>
        public IEnumerable<Tuple<ChargingStation_Id, IEnumerable<Timestamped<ChargingStationAdminStatusType>>>>

            ChargingStationAdminStatusSchedule(IncludeChargingStationDelegate?                  IncludeChargingStations   = null,
                                               Func<DateTimeOffset,                  Boolean>?  TimestampFilter           = null,
                                               Func<ChargingStationAdminStatusType, Boolean>?  AdminStatusFilter         = null,
                                               UInt64?                                          Skip                      = null,
                                               UInt64?                                          Take                      = null)

        {

            IncludeChargingStations ??= (chargingStation => true);

            return chargingStationLookup.Values.
                         Where (chargingStation => IncludeChargingStations(chargingStation)).
                         Select(chargingStation => new Tuple<ChargingStation_Id, IEnumerable<Timestamped<ChargingStationAdminStatusType>>>(
                                                       chargingStation.Id,
                                                       chargingStation.AdminStatusSchedule(TimestampFilter,
                                                                                           AdminStatusFilter,
                                                                                           Skip,
                                                                                           Take)));

        }

        #endregion

        #region ChargingStationStatus              (IncludeChargingStations = null)

        /// <summary>
        /// Return an enumeration of all charging station status.
        /// </summary>
        /// <param name="IncludeChargingStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStationStatus> ChargingStationStatus(IncludeChargingStationDelegate? IncludeChargingStations = null)
        {

            IncludeChargingStations ??= (chargingStation => true);

            return chargingStationLookup.Values.
                       Where (chargingStation => IncludeChargingStations  (chargingStation)).
                       Select(chargingStation => new ChargingStationStatus(chargingStation.Id,
                                                                           chargingStation.Status));

        }

        #endregion

        #region ChargingStationStatusSchedule      (IncludeChargingStations = null, TimestampFilter  = null, StatusFilter = null, HistorySize = null)

        /// <summary>
        /// Return the admin status of all charging stations registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingStations">An optional delegate for filtering charging stations.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="StatusFilter">An optional admin status value filter.</param>
        /// <param name="Skip">The number of status entries per station to skip.</param>
        /// <param name="Take">The number of status entries per station to return.</param>
        public IEnumerable<Tuple<ChargingStation_Id, IEnumerable<Timestamped<ChargingStationStatusType>>>>

            ChargingStationStatusSchedule(IncludeChargingStationDelegate?                  IncludeChargingStations   = null,
                                               Func<DateTimeOffset,             Boolean>?  TimestampFilter           = null,
                                               Func<ChargingStationStatusType, Boolean>?  StatusFilter              = null,
                                               UInt64?                                     Skip                      = null,
                                               UInt64?                                     Take                      = null)

        {

            IncludeChargingStations ??= (chargingStation => true);

            return chargingStationLookup.Values.
                         Where (chargingStation => IncludeChargingStations(chargingStation)).
                         Select(chargingStation => new Tuple<ChargingStation_Id, IEnumerable<Timestamped<ChargingStationStatusType>>>(
                                                       chargingStation.Id,
                                                       chargingStation.StatusSchedule(TimestampFilter,
                                                                                      StatusFilter,
                                                                                      Skip,
                                                                                      Take)));

        }

        #endregion


        #region ContainsChargingStation       (ChargingStation)

        /// <summary>
        /// Check if the given ChargingStation is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        public Boolean ContainsChargingStation(ChargingStation ChargingStation)

            => chargingStationLookup.ContainsKey(ChargingStation.Id);

        #endregion

        #region ContainsChargingStation       (ChargingStationId)

        /// <summary>
        /// Check if the given ChargingStation identification is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of the charging station.</param>
        public Boolean ContainsChargingStation(ChargingStation_Id ChargingStationId)

            => chargingStationLookup.ContainsKey(ChargingStationId);


        /// <summary>
        /// Check if the given ChargingStation identification is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of the charging station.</param>
        public Boolean ContainsChargingStation(ChargingStation_Id? ChargingStationId)

            => ChargingStationId.HasValue &&
                   chargingStationLookup.ContainsKey(ChargingStationId.Value);

        #endregion

        #region GetChargingStationById        (ChargingStationId)

        public ChargingStation? GetChargingStationById(ChargingStation_Id ChargingStationId)
        {

            if (chargingStationLookup.TryGetValue(ChargingStationId, out var chargingStation))
                return chargingStation;

            return null;

        }

        public ChargingStation? GetChargingStationById(ChargingStation_Id? ChargingStationId)
        {

            if (ChargingStationId.HasValue &&
                chargingStationLookup.TryGetValue(ChargingStationId.Value, out var chargingStation))
            {
                return chargingStation;
            }

            return null;

        }

        #endregion

        #region TryGetChargingStationById     (ChargingStationId, out ChargingStation)

        public Boolean TryGetChargingStationById(ChargingStation_Id                         ChargingStationId,
                                                 [NotNullWhen(true)] out ChargingStation?  ChargingStation)

            => chargingStationLookup.TryGetValue(
                   ChargingStationId,
                   out ChargingStation
               );


        public Boolean TryGetChargingStationById(ChargingStation_Id?                        ChargingStationId,
                                                 [NotNullWhen(true)] out ChargingStation?  ChargingStation)
        {

            if (!ChargingStationId.HasValue)
            {
                ChargingStation = null;
                return false;
            }

            return chargingStationLookup.TryGetValue(
                       ChargingStationId.Value,
                       out ChargingStation
                   );

        }

        #endregion



        #region SetChargingStationAdminStatus (ChargingStationId, NewAdminStatus)

        public async Task SetChargingStationAdminStatus(ChargingStation_Id              ChargingStationId,
                                                        ChargingStationAdminStatusType  NewAdminStatus)
        {

            if (TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                chargingStation.AdminStatus = NewAdminStatus;
            }

        }

        #endregion

        #region SetChargingStationAdminStatus (ChargingStationId, NewTimestampedAdminStatus)

        public async Task SetChargingStationAdminStatus(ChargingStation_Id                           ChargingStationId,
                                                        Timestamped<ChargingStationAdminStatusType>  NewTimestampedAdminStatus)
        {

            if (TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                chargingStation.AdminStatus = NewTimestampedAdminStatus;
            }

        }

        #endregion

        #region SetChargingStationAdminStatus (ChargingStationId, NewAdminStatus, Timestamp)

        public async Task SetChargingStationAdminStatus(ChargingStation_Id              ChargingStationId,
                                                        ChargingStationAdminStatusType  NewAdminStatus,
                                                        DateTimeOffset                  Timestamp)
        {

            if (TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                chargingStation.AdminStatus = new Timestamped<ChargingStationAdminStatusType>(
                                                  Timestamp,
                                                  NewAdminStatus
                                              );
            }

        }

        #endregion

        #region SetChargingStationAdminStatus (ChargingStationId, StatusList, ChangeMethod = ChangeMethods.Replace)

        public async Task SetChargingStationAdminStatus(ChargingStation_Id                                        ChargingStationId,
                                                        IEnumerable<Timestamped<ChargingStationAdminStatusType>>  AdminStatusList,
                                                        ChangeMethods                                             ChangeMethod  = ChangeMethods.Replace)
        {

            if (TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                chargingStation.SetAdminStatus(
                    AdminStatusList,
                    ChangeMethod
                );
            }

            //if (SendUpstream)
            //{
            //
            //    RoamingNetwork.
            //        SendChargingStationAdminStatusDiff(new ChargingStationAdminStatusDiff(Timestamp.Now,
            //                                               ChargingStationOperatorId:    Id,
            //                                               ChargingStationOperatorName:  Name,
            //                                               NewStatus:         new List<KeyValuePair<ChargingStation_Id, ChargingStationAdminStatusType>>(),
            //                                               ChangedStatus:     new List<KeyValuePair<ChargingStation_Id, ChargingStationAdminStatusType>>() {
            //                                                                          new KeyValuePair<ChargingStation_Id, ChargingStationAdminStatusType>(ChargingStationId, NewStatus.Value)
            //                                                                      },
            //                                               RemovedIds:        new List<ChargingStation_Id>()));
            //
            //}

        }

        #endregion


        #region SetChargingStationStatus      (ChargingStationId, NewStatus)

        public async Task SetChargingStationStatus(ChargingStation_Id         ChargingStationId,
                                                   ChargingStationStatusType  NewStatus)
        {

            if (TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                chargingStation.Status = NewStatus;
            }

        }

        #endregion

        #region SetChargingStationStatus      (ChargingStationId, NewTimestampedStatus)

        public async Task SetChargingStationStatus(ChargingStation_Id                      ChargingStationId,
                                                   Timestamped<ChargingStationStatusType>  NewTimestampedStatus)
        {

            if (TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                chargingStation.Status = NewTimestampedStatus;
            }

        }

        #endregion

        #region SetChargingStationStatus      (ChargingStationId, NewStatus, Timestamp)

        public async Task SetChargingStationStatus(ChargingStation_Id         ChargingStationId,
                                                   ChargingStationStatusType  NewStatus,
                                                   DateTimeOffset             Timestamp)
        {

            if (TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                chargingStation.Status = new Timestamped<ChargingStationStatusType>(
                                             Timestamp,
                                             NewStatus
                                         );
            }

        }

        #endregion

        #region SetChargingStationStatus      (ChargingStationId, StatusList, ChangeMethod = ChangeMethods.Replace)

        public async Task SetChargingStationStatus(ChargingStation_Id                                   ChargingStationId,
                                                   IEnumerable<Timestamped<ChargingStationStatusType>>  StatusList,
                                                   ChangeMethods                                        ChangeMethod  = ChangeMethods.Replace)
        {

            if (TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                chargingStation.SetStatus(
                    StatusList,
                    ChangeMethod
                );
            }

            //if (SendUpstream)
            //{
            //
            //    RoamingNetwork.
            //        SendChargingStationStatusDiff(new ChargingStationStatusDiff(Timestamp.Now,
            //                                               ChargingStationOperatorId:    Id,
            //                                               ChargingStationOperatorName:  Name,
            //                                               NewStatus:         new List<KeyValuePair<ChargingStation_Id, ChargingStationStatusType>>(),
            //                                               ChangedStatus:     new List<KeyValuePair<ChargingStation_Id, ChargingStationStatusType>>() {
            //                                                                          new KeyValuePair<ChargingStation_Id, ChargingStationStatusType>(ChargingStationId, NewStatus.Value)
            //                                                                      },
            //                                               RemovedIds:        new List<ChargingStation_Id>()));
            //
            //}

        }

        #endregion


        #region OnChargingStationData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data of any subordinated charging station changed.
        /// </summary>
        public event OnChargingStationDataChangedDelegate?         OnChargingStationDataChanged;

        /// <summary>
        /// An event fired whenever the aggregated dynamic status of any subordinated charging station changed.
        /// </summary>
        public event OnChargingStationStatusChangedDelegate?       OnChargingStationStatusChanged;

        /// <summary>
        /// An event fired whenever the aggregated admin status of any subordinated charging station changed.
        /// </summary>
        public event OnChargingStationAdminStatusChangedDelegate?  OnChargingStationAdminStatusChanged;

        #endregion


        #region (internal) UpdateChargingStationData       (Timestamp, EventTrackingId, ChargingStation, PropertyName, NewValue, OldValue = null, DataSource = null)

        /// <summary>
        /// Update the data of a charging station.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargingStation">The changed charging station.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        /// <param name="OldValue">The optional old value of the changed property.</param>
        /// <param name="DataSource">An optional data source or context for the charging station data update.</param>
        internal async Task UpdateChargingStationData(DateTimeOffset    Timestamp,
                                                      EventTracking_Id  EventTrackingId,
                                                      ChargingStation  ChargingStation,
                                                      String            PropertyName,
                                                      Object?           NewValue,
                                                      Object?           OldValue     = null,
                                                      Context?          DataSource   = null)
        {

            var onChargingStationDataChanged = OnChargingStationDataChanged;
            if (onChargingStationDataChanged is not null)
                await onChargingStationDataChanged(Timestamp,
                                                   EventTrackingId,
                                                   ChargingStation,
                                                   PropertyName,
                                                   NewValue,
                                                   OldValue,
                                                   DataSource);

        }

        #endregion

        #region (internal) UpdateChargingStationAdminStatus(Timestamp, EventTrackingId, ChargingStation, NewStatus, OldStatus = null, DataSource = null)

        /// <summary>
        /// Update the current charging station admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="OldStatus">The old aggreagted charging station status.</param>
        /// <param name="NewStatus">The new aggreagted charging station status.</param>
        /// <param name="DataSource">An optional data source or context for the charging station admin update.</param>
        internal async Task UpdateChargingStationAdminStatus(DateTimeOffset                                 Timestamp,
                                                             EventTracking_Id                               EventTrackingId,
                                                             ChargingStation                               ChargingStation,
                                                             Timestamped<ChargingStationAdminStatusType>   NewStatus,
                                                             Timestamped<ChargingStationAdminStatusType>?  OldStatus    = null,
                                                             Context?                                       DataSource   = null)
        {

            var onChargingStationAdminStatusChanged = OnChargingStationAdminStatusChanged;
            if (onChargingStationAdminStatusChanged is not null)
                await onChargingStationAdminStatusChanged(Timestamp,
                                                          EventTrackingId,
                                                          ChargingStation,
                                                          NewStatus,
                                                          OldStatus,
                                                          DataSource);

        }

        #endregion

        #region (internal) UpdateChargingStationStatus     (Timestamp, EventTrackingId, ChargingStation, NewStatus, OldStatus = null, DataSource = null)

        /// <summary>
        /// Update a charging pool admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="OldStatus">The old aggregated charging station status.</param>
        /// <param name="NewStatus">The new aggregated charging station status.</param>
        /// <param name="DataSource">An optional data source or context for the charging pool admin status update.</param>
        internal async Task UpdateChargingStationStatus(DateTimeOffset                            Timestamp,
                                                        EventTracking_Id                          EventTrackingId,
                                                        ChargingStation                          ChargingStation,
                                                        Timestamped<ChargingStationStatusType>   NewStatus,
                                                        Timestamped<ChargingStationStatusType>?  OldStatus    = null,
                                                        Context?                                  DataSource   = null)
        {

            var onChargingStationStatusChanged = OnChargingStationStatusChanged;
            if (onChargingStationStatusChanged is not null)
                await onChargingStationStatusChanged(Timestamp,
                                                     EventTrackingId,
                                                     ChargingStation,
                                                     NewStatus,
                                                     OldStatus,
                                                     DataSource);

        }

        #endregion

        #endregion

        #region Charging Station Groups

        #region Data

        private readonly ConcurrentDictionary<ChargingStationGroup_Id, ChargingStationGroup> chargingStationGroups;

        /// <summary>
        /// All charging station groups registered within this charging station operator.
        /// </summary>
        public IEnumerable<ChargingStationGroup> ChargingStationGroups

            => chargingStationGroups.Values;

        #endregion


        #region CreateChargingStationGroup     (Id,       Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new charging group having the given
        /// unique charging group identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station group.</param>
        /// <param name="Name">The official (multi-language) name of this charging station group.</param>
        /// <param name="Description">An optional (multi-language) description of this charging station group.</param>
        /// 
        /// <param name="Members">An enumeration of charging stations member building this charging station group.</param>
        /// <param name="MemberIds">An enumeration of charging station identifications which are building this charging station group.</param>
        /// <param name="AutoIncludeStations">A delegate deciding whether to include new charging stations automatically into this group.</param>
        /// 
        /// <param name="StatusAggregationDelegate">A delegate called to aggregate the dynamic status of all subordinated charging stations.</param>
        /// <param name="MaxGroupStatusListSize">The default size of the charging station group status list.</param>
        /// <param name="MaxGroupAdminStatusListSize">The default size of the charging station group admin status list.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging group failed.</param>
        public ChargingStationGroup CreateChargingStationGroup(ChargingStationGroup_Id                                             Id,
                                                               I18NString                                                          Name,
                                                               I18NString                                                          Description                   = null,

                                                               Brand                                                               Brand                         = null,
                                                               Priority?                                                           Priority                      = null,
                                                               ChargingTariff                                                      Tariff                        = null,
                                                               IEnumerable<DataLicense>                                            DataLicenses                  = null,

                                                               IEnumerable<ChargingStation>                                        Members                       = null,
                                                               IEnumerable<ChargingStation_Id>                                     MemberIds                     = null,
                                                               Func<ChargingStation, Boolean>                                      AutoIncludeStations           = null,

                                                               Func<ChargingStationStatusReport, ChargingStationGroupStatusTypes>  StatusAggregationDelegate     = null,
                                                               UInt16                                                              MaxGroupStatusListSize        = ChargingStationGroup.DefaultMaxGroupStatusListSize,
                                                               UInt16                                                              MaxGroupAdminStatusListSize   = ChargingStationGroup.DefaultMaxGroupAdminStatusListSize,

                                                               Action<ChargingStationGroup>                                        OnSuccess                     = null,
                                                               Action<ChargingStationOperator, ChargingStationGroup_Id>           OnError                       = null)

        {

            lock (chargingStationGroups)
            {

                #region Initial checks

                if (chargingStationGroups.TryGetValue(Id, out var ccc))
                {

                    if (OnError is not null)
                        OnError?.Invoke(this, Id);

                    return ccc;

                }

                if (Name.IsNullOrEmpty())
                    throw new ArgumentNullException(nameof(Name), "The name of the charging station group must not be null or empty!");

                #endregion

                var chargingStationGroup = new ChargingStationGroup(Id,
                                                                    this,
                                                                    Name,
                                                                    Description,

                                                                    Brand,
                                                                    Priority,
                                                                    Tariff,
                                                                    DataLicenses,

                                                                    Members,
                                                                    MemberIds,
                                                                    AutoIncludeStations,
                                                                    StatusAggregationDelegate,
                                                                    MaxGroupAdminStatusListSize,
                                                                    MaxGroupStatusListSize);


                if (chargingStationGroups.TryAdd(chargingStationGroup.Id, chargingStationGroup))
                {

                    chargingStationGroup.OnEVSEDataChanged                             += UpdateEVSEData;
                    chargingStationGroup.OnEVSEStatusChanged                           += UpdateEVSEStatus;
                    chargingStationGroup.OnEVSEAdminStatusChanged                      += UpdateEVSEAdminStatus;

                    chargingStationGroup.OnChargingStationDataChanged                  += UpdateChargingStationData;
                    chargingStationGroup.OnChargingStationStatusChanged                += UpdateChargingStationStatus;
                    chargingStationGroup.OnChargingStationAdminStatusChanged           += UpdateChargingStationAdminStatus;

                    //_ChargingStationGroup.OnDataChanged                                 += UpdateChargingStationGroupData;
                    //_ChargingStationGroup.OnAdminStatusChanged                          += UpdateChargingStationGroupAdminStatus;

                    OnSuccess?.Invoke(chargingStationGroup);

                    return chargingStationGroup;

                }

                return null;

            }

        }

        #endregion

        #region CreateChargingStationGroup     (IdSuffix, Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new charging group having the given
        /// unique charging group identification.
        /// </summary>
        /// <param name="IdSuffix">The suffix of the unique identification of the new charging group.</param>
        /// <param name="Name">The official (multi-language) name of this charging station group.</param>
        /// <param name="Description">An optional (multi-language) description of this charging station group.</param>
        /// 
        /// <param name="Members">An enumeration of charging stations member building this charging station group.</param>
        /// <param name="MemberIds">An enumeration of charging station identifications which are building this charging station group.</param>
        /// <param name="AutoIncludeStations">A delegate deciding whether to include new charging stations automatically into this group.</param>
        /// 
        /// <param name="StatusAggregationDelegate">A delegate called to aggregate the dynamic status of all subordinated charging stations.</param>
        /// <param name="MaxGroupStatusListSize">The default size of the charging station group status list.</param>
        /// <param name="MaxGroupAdminStatusListSize">The default size of the charging station group admin status list.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging group failed.</param>
        public ChargingStationGroup CreateChargingStationGroup(String                                                              IdSuffix,
                                                               I18NString                                                          Name,
                                                               I18NString                                                          Description                   = null,

                                                               IEnumerable<ChargingStation>                                       Members                       = null,
                                                               IEnumerable<ChargingStation_Id>                                     MemberIds                     = null,
                                                               Func<ChargingStation, Boolean>                                     AutoIncludeStations           = null,

                                                               Func<ChargingStationStatusReport, ChargingStationGroupStatusTypes>  StatusAggregationDelegate     = null,
                                                               UInt16                                                              MaxGroupStatusListSize        = ChargingStationGroup.DefaultMaxGroupStatusListSize,
                                                               UInt16                                                              MaxGroupAdminStatusListSize   = ChargingStationGroup.DefaultMaxGroupAdminStatusListSize,

                                                               Action<ChargingStationGroup>                                        OnSuccess                     = null,
                                                               Action<ChargingStationOperator, ChargingStationGroup_Id>           OnError                       = null)

        {

            #region Initial checks

            if (IdSuffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(IdSuffix), "The given suffix of the unique identification of the new charging group must not be null or empty!");

            #endregion

            return CreateChargingStationGroup(ChargingStationGroup_Id.Parse(Id,
                                                                            IdSuffix.Trim().ToUpper()),
                                              Name,
                                              Description,

                                              null,
                                              new Priority(0),
                                              null,
                                              null,

                                              Members,
                                              MemberIds,
                                              AutoIncludeStations,
                                              StatusAggregationDelegate,
                                              MaxGroupAdminStatusListSize,
                                              MaxGroupStatusListSize,
                                              OnSuccess,
                                              OnError);

        }

        #endregion

        #region GetOrCreateChargingStationGroup(Id,       Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Get or create and register a new charging group having the given
        /// unique charging group identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station group.</param>
        /// <param name="Name">The official (multi-language) name of this charging station group.</param>
        /// <param name="Description">An optional (multi-language) description of this charging station group.</param>
        /// 
        /// <param name="Members">An enumeration of charging stations member building this charging station group.</param>
        /// <param name="MemberIds">An enumeration of charging station identifications which are building this charging station group.</param>
        /// <param name="AutoIncludeStations">A delegate deciding whether to include new charging stations automatically into this group.</param>
        /// 
        /// <param name="StatusAggregationDelegate">A delegate called to aggregate the dynamic status of all subordinated charging stations.</param>
        /// <param name="MaxGroupStatusListSize">The default size of the charging station group status list.</param>
        /// <param name="MaxGroupAdminStatusListSize">The default size of the charging station group admin status list.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging group failed.</param>
        public ChargingStationGroup GetOrCreateChargingStationGroup(ChargingStationGroup_Id                                             Id,
                                                                    I18NString                                                          Name,
                                                                    I18NString                                                          Description                   = null,

                                                                    IEnumerable<ChargingStation>                                       Members                       = null,
                                                                    IEnumerable<ChargingStation_Id>                                     MemberIds                     = null,
                                                                    Func<ChargingStation, Boolean>                                     AutoIncludeStations           = null,

                                                                    Func<ChargingStationStatusReport, ChargingStationGroupStatusTypes>  StatusAggregationDelegate     = null,
                                                                    UInt16                                                              MaxGroupStatusListSize        = ChargingStationGroup.DefaultMaxGroupStatusListSize,
                                                                    UInt16                                                              MaxGroupAdminStatusListSize   = ChargingStationGroup.DefaultMaxGroupAdminStatusListSize,

                                                                    Action<ChargingStationGroup>                                        OnSuccess                     = null,
                                                                    Action<ChargingStationOperator, ChargingStationGroup_Id>           OnError                       = null)

        {

            lock (chargingStationGroups)
            {

                #region Initial checks

                if (Name.IsNullOrEmpty())
                    throw new ArgumentNullException(nameof(Name), "The name of the charging station group must not be null or empty!");

                #endregion

                if (chargingStationGroups.TryGetValue(Id, out var _ChargingStationGroup))
                    return _ChargingStationGroup;

                return CreateChargingStationGroup(Id,
                                                  Name,
                                                  Description,

                                                  null,
                                                  new Priority(0),
                                                  null,
                                                  null,

                                                  Members,
                                                  MemberIds,
                                                  AutoIncludeStations,
                                                  StatusAggregationDelegate,
                                                  MaxGroupAdminStatusListSize,
                                                  MaxGroupStatusListSize,
                                                  OnSuccess,
                                                  OnError);

            }

        }

        #endregion

        #region GetOrCreateChargingStationGroup(IdSuffix, Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Get or create and register a new charging group having the given
        /// unique charging group identification.
        /// </summary>
        /// <param name="IdSuffix">The suffix of the unique identification of the new charging group.</param>
        /// <param name="Name">The official (multi-language) name of this charging station group.</param>
        /// <param name="Description">An optional (multi-language) description of this charging station group.</param>
        /// 
        /// <param name="Members">An enumeration of charging stations member building this charging station group.</param>
        /// <param name="MemberIds">An enumeration of charging station identifications which are building this charging station group.</param>
        /// <param name="AutoIncludeStations">A delegate deciding whether to include new charging stations automatically into this group.</param>
        /// 
        /// <param name="StatusAggregationDelegate">A delegate called to aggregate the dynamic status of all subordinated charging stations.</param>
        /// <param name="MaxGroupStatusListSize">The default size of the charging station group status list.</param>
        /// <param name="MaxGroupAdminStatusListSize">The default size of the charging station group admin status list.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging group failed.</param>
        public ChargingStationGroup GetOrCreateChargingStationGroup(String                                                              IdSuffix,
                                                                    I18NString                                                          Name,
                                                                    I18NString                                                          Description                   = null,

                                                                    IEnumerable<ChargingStation>                                       Members                       = null,
                                                                    IEnumerable<ChargingStation_Id>                                     MemberIds                     = null,
                                                                    Func<ChargingStation, Boolean>                                     AutoIncludeStations           = null,

                                                                    Func<ChargingStationStatusReport, ChargingStationGroupStatusTypes>  StatusAggregationDelegate     = null,
                                                                    UInt16                                                              MaxGroupStatusListSize        = ChargingStationGroup.DefaultMaxGroupStatusListSize,
                                                                    UInt16                                                              MaxGroupAdminStatusListSize   = ChargingStationGroup.DefaultMaxGroupAdminStatusListSize,

                                                                    Action<ChargingStationGroup>                                        OnSuccess                     = null,
                                                                    Action<ChargingStationOperator, ChargingStationGroup_Id>           OnError                       = null)


        {

            #region Initial checks

            if (IdSuffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(IdSuffix), "The given suffix of the unique identification of the new charging group must not be null or empty!");

            #endregion

            return GetOrCreateChargingStationGroup(ChargingStationGroup_Id.Parse(Id,
                                                                                 IdSuffix.Trim().ToUpper()),
                                                   Name,
                                                   Description,
                                                   Members,
                                                   MemberIds,
                                                   AutoIncludeStations,
                                                   StatusAggregationDelegate,
                                                   MaxGroupAdminStatusListSize,
                                                   MaxGroupStatusListSize,
                                                   OnSuccess,
                                                   OnError);

        }

        #endregion


        #region TryGetChargingStationGroup(Id, out ChargingStationGroup)

        /// <summary>
        /// Try to return to charging station group for the given charging station group identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station group.</param>
        /// <param name="ChargingStationGroup">The charging station group.</param>
        public Boolean TryGetChargingStationGroup(ChargingStationGroup_Id   Id,
                                                  out ChargingStationGroup  ChargingStationGroup)

            => chargingStationGroups.TryGetValue(Id, out ChargingStationGroup);

        #endregion


        #region RemoveChargingStationGroup(ChargingStationGroupId, OnSuccess = null, OnError = null)

        /// <summary>
        /// All charging station groups registered within this charging station operator.
        /// </summary>
        /// <param name="ChargingStationGroupId">The unique identification of the charging station group to be removed.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new charging station group after its successful deletion.</param>
        /// <param name="OnError">An optional delegate to be called whenever the deletion of the charging station group failed.</param>
        public ChargingStationGroup RemoveChargingStationGroup(ChargingStationGroup_Id                                    ChargingStationGroupId,
                                                               Action<ChargingStationOperator, ChargingStationGroup>?     OnSuccess   = null,
                                                               Action<ChargingStationOperator, ChargingStationGroup_Id>?  OnError     = null)
        {

            lock (chargingStationGroups)
            {

                if (chargingStationGroups.TryRemove(
                        ChargingStationGroupId,
                        out var _ChargingStationGroup
                    )
                ) {

                    //OnSuccess?.Invoke(this, ChargingStationGroup);

                    //ChargingStationGroupRemoval.SendNotification(
                    //    EventTracking_Id.New,
                    //    Timestamp.Now,
                    //    this,
                    //    _ChargingStationGroup
                    //);

                    return _ChargingStationGroup;

                }

                OnError?.Invoke(this, ChargingStationGroupId);

                return null;

            }

        }

        #endregion

        #region RemoveChargingStationGroup(ChargingStationGroup,   OnSuccess = null, OnError = null)

        /// <summary>
        /// All charging station groups registered within this charging station operator.
        /// </summary>
        /// <param name="ChargingStationGroup">The charging station group to remove.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new charging station group after its successful deletion.</param>
        /// <param name="OnError">An optional delegate to be called whenever the deletion of the charging station group failed.</param>
        public ChargingStationGroup RemoveChargingStationGroup(ChargingStationGroup                                   ChargingStationGroup,
                                                               Action<ChargingStationOperator, ChargingStationGroup>  OnSuccess   = null,
                                                               Action<ChargingStationOperator, ChargingStationGroup>  OnError     = null)
        {

            lock (chargingStationGroups)
            {

                //if (ChargingStationGroupRemoval.SendVoting(
                //    EventTracking_Id.New,
                //    Timestamp.Now,
                //    this,
                //    ChargingStationGroup) &&
                //    chargingStationGroups.TryRemove(
                //        ChargingStationGroup.Id,
                //        out var _ChargingStationGroup,
                //        EventTracking_Id.New,
                //        null
                //    )
                //){

                //    OnSuccess?.Invoke(this, _ChargingStationGroup);

                //    ChargingStationGroupRemoval.SendNotification(
                //        EventTracking_Id.New,
                //        Timestamp.Now,
                //        this,
                //        _ChargingStationGroup
                //    );

                //    return _ChargingStationGroup;

                //}

                //OnError?.Invoke(this, ChargingStationGroup);

                return ChargingStationGroup;

            }

        }

        #endregion

        #endregion

        #region EVSEs

        #region Data

        private readonly ConcurrentDictionary<EVSE_Id, EVSE> evseLookup = new();

        /// <summary>
        /// Return an enumeration of all EVSEs.
        /// </summary>
        public IEnumerable<EVSE> EVSEs

            => evseLookup.Values;

        #endregion


        #region EVSEAddition

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingStation, EVSE, Boolean> evseAddition;

        public IVotingNotificator<DateTimeOffset, User_Id, ChargingStation, EVSE, Boolean> EVSEAddition
            => evseAddition;

        /// <summary>
        /// Called whenever an EVSE will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStation, EVSE, Boolean> OnEVSEAddition

            => evseAddition;

        #endregion

        #region EVSEUpdate

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingStation, EVSE, EVSE, Boolean> evseUpdate;

        public IVotingNotificator<DateTimeOffset, User_Id, ChargingStation, EVSE, EVSE, Boolean> EVSEUpdate
            => evseUpdate;

        /// <summary>
        /// Called whenever an EVSE will be or was update.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStation, EVSE, EVSE, Boolean> OnEVSEUpdate

            => evseUpdate;

        #endregion

        #region EVSERemoval

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingStation, EVSE, Boolean> evseRemoval;

        public IVotingNotificator<DateTimeOffset, User_Id, ChargingStation, EVSE, Boolean> EVSERemoval
            => evseRemoval;

        /// <summary>
        /// Called whenever an EVSE will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStation, EVSE, Boolean> OnEVSERemoval

            => evseRemoval;

        #endregion


        #region EVSEIds                 (IncludeEVSEs = null)

        /// <summary>
        /// Return an enumeration of all EVSE identifications.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSE_Id> EVSEIds(IncludeEVSEDelegate? IncludeEVSEs = null)
        {

            IncludeEVSEs ??= (evse => true);

            return evseLookup.Values.
                       Where (evse => IncludeEVSEs(evse)).
                       Select(evse => evse.Id);

        }

        #endregion

        #region EVSEAdminStatus         (IncludeEVSEs = null)

        /// <summary>
        /// Return an enumeration of all EVSE admin status.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSEAdminStatus> EVSEAdminStatus(IncludeEVSEDelegate? IncludeEVSEs = null)
        {

            IncludeEVSEs ??= (evse => true);

            return evseLookup.Values.
                       Where (evse => IncludeEVSEs(evse)).
                       Select(evse => new EVSEAdminStatus(evse.Id,
                                                          evse.AdminStatus));
        }

        #endregion

        #region EVSEAdminStatusSchedule (IncludeEVSEs = null, TimestampFilter  = null, StatusFilter = null, HistorySize = null)

        /// <summary>
        /// Return the admin status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="AdminStatusFilter">An optional admin status value filter.</param>
        /// <param name="Skip">The number of admin status entries per pool to skip.</param>
        /// <param name="Take">The number of admin status entries per pool to return.</param>
        public IEnumerable<Tuple<EVSE_Id, IEnumerable<Timestamped<EVSEAdminStatusType>>>>

            EVSEAdminStatusSchedule(IncludeEVSEDelegate?                 IncludeEVSEs        = null,
                                    Func<DateTimeOffset,      Boolean>?  TimestampFilter     = null,
                                    Func<EVSEAdminStatusType, Boolean>?  AdminStatusFilter   = null,
                                    UInt64?                              Skip                = null,
                                    UInt64?                              Take                = null)

        {

            IncludeEVSEs ??= (evse => true);

            return evseLookup.Values.
                       Where (evse => IncludeEVSEs(evse)).
                       Select(evse => new Tuple<EVSE_Id, IEnumerable<Timestamped<EVSEAdminStatusType>>>(
                                          evse.Id,
                                          evse.AdminStatusSchedule(TimestampFilter,
                                                                   AdminStatusFilter,
                                                                   Skip,
                                                                   Take)));

        }

        #endregion

        #region EVSEStatus              (IncludeEVSEs = null)

        /// <summary>
        /// Return an enumeration of all EVSE status.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSEStatus> EVSEStatus(IncludeEVSEDelegate? IncludeEVSEs = null)
        {

            IncludeEVSEs ??= (evse => true);

            return evseLookup.Values.
                       Where (evse => IncludeEVSEs  (evse)).
                       Select(evse => new EVSEStatus(evse.Id,
                                                     evse.Status));

        }

        #endregion

        #region EVSEStatusSchedule      (IncludeEVSEs = null, TimestampFilter  = null, StatusFilter = null, HistorySize = null)

        /// <summary>
        /// Return the admin status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="StatusFilter">An optional admin status value filter.</param>
        /// <param name="Skip">The number of status entries per pool to skip.</param>
        /// <param name="Take">The number of status entries per pool to return.</param>
        public IEnumerable<Tuple<EVSE_Id, IEnumerable<Timestamped<EVSEStatusType>>>>

            EVSEStatusSchedule(IncludeEVSEDelegate?            IncludeEVSEs      = null,
                               Func<DateTimeOffset, Boolean>?  TimestampFilter   = null,
                               Func<EVSEStatusType, Boolean>?  StatusFilter      = null,
                               UInt64?                         Skip              = null,
                               UInt64?                         Take              = null)

        {

            IncludeEVSEs ??= (evse => true);

            return evseLookup.Values.
                       Where (evse => IncludeEVSEs(evse)).
                       Select(evse => new Tuple<EVSE_Id, IEnumerable<Timestamped<EVSEStatusType>>>(
                                          evse.Id,
                                          evse.StatusSchedule(TimestampFilter,
                                                              StatusFilter,
                                                              Skip,
                                                              Take)));

        }

        #endregion


        #region ContainsEVSE                  (EVSE)

        /// <summary>
        /// Check if the given EVSE is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        public Boolean ContainsEVSE(EVSE EVSE)

            => evseLookup.ContainsKey(EVSE.Id);

        #endregion

        #region ContainsEVSE                  (EVSEId)

        /// <summary>
        /// Check if the given EVSE identification is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        public Boolean ContainsEVSE(EVSE_Id EVSEId)

            => evseLookup.ContainsKey(EVSEId);

        /// <summary>
        /// Check if the given EVSE identification is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        public Boolean ContainsEVSE(EVSE_Id? EVSEId)

            => EVSEId.HasValue &&
                   evseLookup.ContainsKey(EVSEId.Value);

        #endregion

        #region GetEVSEById                   (EVSEId)

        public EVSE? GetEVSEById(EVSE_Id EVSEId)
        {

            if (evseLookup.TryGetValue(EVSEId, out var evse))
                return evse;

            return null;

        }

        public EVSE? GetEVSEById(EVSE_Id? EVSEId)
        {

            if (EVSEId.HasValue &&
                evseLookup.TryGetValue(EVSEId.Value, out var evse))
            {
                return evse;
            }

            return null;

        }

        #endregion

        #region TryGetEVSEById                (EVSEId, out EVSE)

        public Boolean TryGetEVSEById(EVSE_Id                         EVSEId,
                                      [NotNullWhen(true)] out EVSE?  EVSE)

            => evseLookup.TryGetValue(
                   EVSEId,
                   out EVSE
               );


        public Boolean TryGetEVSEById(EVSE_Id?                        EVSEId,
                                      [NotNullWhen(true)] out EVSE?  EVSE)
        {

            if (!EVSEId.HasValue)
            {
                EVSE = null;
                return false;
            }

            return evseLookup.TryGetValue(
                       EVSEId.Value,
                       out EVSE
                   );

        }

        #endregion

        #region TryGetChargingStationByEVSEId (EVSEId, out ChargingStation)

        public Boolean TryGetChargingStationByEVSEId(EVSE_Id                                    EVSEId,
                                                     [NotNullWhen(true)] out ChargingStation?  ChargingStation)
        {

            if (evseLookup.TryGetValue(EVSEId, out var evse))
            {
                ChargingStation = evse.ChargingStation;
                return ChargingStation is not null;
            }

            ChargingStation = null;
            return false;

        }

        public Boolean TryGetChargingStationByEVSEId(EVSE_Id?                                   EVSEId,
                                                     [NotNullWhen(true)] out ChargingStation?  ChargingStation)
        {

            if (EVSEId.HasValue &&
                evseLookup.TryGetValue(EVSEId.Value, out var evse))
            {
                ChargingStation = evse.ChargingStation;
                return ChargingStation is not null;
            }

            ChargingStation = null;
            return false;

        }

        #endregion

        #region TryGetChargingPoolByEVSEId    (EVSEId, out ChargingPool)

        public Boolean TryGetChargingPoolByEVSEId(EVSE_Id                                 EVSEId,
                                                  [NotNullWhen(true)] out ChargingPool?  ChargingPool)
        {

            if (evseLookup.TryGetValue(EVSEId, out var evse))
            {
                ChargingPool = evse.ChargingStation?.ChargingPool;
                return ChargingPool is not null;
            }

            ChargingPool = null;
            return false;

        }

        public Boolean TryGetChargingPoolByEVSEId(EVSE_Id?                                EVSEId,
                                                  [NotNullWhen(true)] out ChargingPool?  ChargingPool)
        {

            if (EVSEId.HasValue &&
                evseLookup.TryGetValue(EVSEId.Value, out var evse))
            {
                ChargingPool = evse.ChargingStation?.ChargingPool;
                return ChargingPool is not null;
            }

            ChargingPool = null;
            return false;

        }

        #endregion


        #region SetEVSEAdminStatus (NewAdminStatus)

        public void SetEVSEAdminStatus(EVSEAdminStatus NewAdminStatus)
        {

            if (TryGetEVSEById(NewAdminStatus.Id, out var evse))
            {
                evse.AdminStatus = NewAdminStatus.AsTimestampedStatus();
            }

        }

        #endregion

        #region SetEVSEAdminStatus (EVSEId, NewAdminStatus)

        public void SetEVSEAdminStatus(EVSE_Id              EVSEId,
                                       EVSEAdminStatusType  NewAdminStatus)
        {

            if (TryGetEVSEById(EVSEId, out var evse))
            {
                evse.AdminStatus = NewAdminStatus;
            }

        }

        #endregion

        #region SetEVSEAdminStatus (EVSEId, NewTimestampedAdminStatus)

        public void SetEVSEAdminStatus(EVSE_Id                           EVSEId,
                                       Timestamped<EVSEAdminStatusType>  NewTimestampedAdminStatus)
        {

            if (TryGetEVSEById(EVSEId, out var evse))
            {
                evse.AdminStatus = NewTimestampedAdminStatus;
            }

        }

        #endregion

        #region SetEVSEAdminStatus (EVSEId, NewAdminStatus, Timestamp)

        public void SetEVSEAdminStatus(EVSE_Id              EVSEId,
                                       EVSEAdminStatusType  NewAdminStatus,
                                       DateTimeOffset       Timestamp)
        {

            if (TryGetEVSEById(EVSEId, out var evse))
            {
                evse.AdminStatus = new Timestamped<EVSEAdminStatusType>(
                                       Timestamp,
                                       NewAdminStatus
                                   );
            }

        }

        #endregion

        #region SetEVSEAdminStatus (EVSEId, AdminStatusList, ChangeMethod = ChangeMethods.Replace)

        public void SetEVSEAdminStatus(EVSE_Id                                        EVSEId,
                                       IEnumerable<Timestamped<EVSEAdminStatusType>>  AdminStatusList,
                                       ChangeMethods                                  ChangeMethod  = ChangeMethods.Replace)
        {

            if (TryGetEVSEById(EVSEId, out var evse))
            {
                evse.SetAdminStatus(
                         AdminStatusList,
                         ChangeMethod
                     );
            }

        }

        #endregion


        #region ApplyEVSEAdminStatusDiff (EVSEAdminStatusDiff)

        public EVSEAdminStatusDiff ApplyEVSEAdminStatusDiff(EVSEAdminStatusDiff EVSEAdminStatusDiff)
        {

            #region Initial checks

            if (EVSEAdminStatusDiff is null)
                throw new ArgumentNullException(nameof(EVSEAdminStatusDiff),  "The given EVSE admin status diff must not be null!");

            #endregion

            foreach (var status in EVSEAdminStatusDiff.NewStatus)
                SetEVSEAdminStatus(status.Key, status.Value);

            foreach (var status in EVSEAdminStatusDiff.ChangedStatus)
                SetEVSEAdminStatus(status.Key, status.Value);

            return EVSEAdminStatusDiff;

        }

        #endregion



        #region SetEVSEStatus (NewStatus)

        public void SetEVSEStatus(EVSEStatus  NewStatus)
        {

            if (TryGetEVSEById(NewStatus.Id, out var evse))
            {
                evse.SetStatus(NewStatus);
            }

        }

        #endregion

        #region SetEVSEStatus (EVSEId, NewStatus)

        public void SetEVSEStatus(EVSE_Id         EVSEId,
                                  EVSEStatusType  NewStatus)
        {

            if (TryGetEVSEById(EVSEId, out var evse))
            {
                evse.Status = NewStatus;
            }

        }

        #endregion

        #region SetEVSEStatus (EVSEId, NewTimestampedStatus)

        public void SetEVSEStatus(EVSE_Id                      EVSEId,
                                  Timestamped<EVSEStatusType>  NewTimestampedStatus)
        {

            if (TryGetEVSEById(EVSEId, out var evse))
            {
                evse.Status = NewTimestampedStatus;
            }

        }

        #endregion

        #region SetEVSEStatus (EVSEId, NewStatus, Timestamp)

        public void SetEVSEStatus(EVSE_Id         EVSEId,
                                  EVSEStatusType  NewStatus,
                                  DateTimeOffset  Timestamp)
        {

            if (TryGetEVSEById(EVSEId, out var evse))
            {

                evse.Status = new Timestamped<EVSEStatusType>(
                                  Timestamp,
                                  NewStatus
                              );

            }

        }

        #endregion

        #region SetEVSEStatus (EVSEId, StatusList, ChangeMethod = ChangeMethods.Replace)

        public void SetEVSEStatus(EVSE_Id                                   EVSEId,
                                  IEnumerable<Timestamped<EVSEStatusType>>  StatusList,
                                  ChangeMethods                             ChangeMethod  = ChangeMethods.Replace)
        {

            if (TryGetEVSEById(EVSEId, out var evse))
            {
                evse.SetStatus(StatusList, ChangeMethod);
            }

        }

        #endregion


        #region CalcEVSEStatusDiff  (EVSEStatus, IncludeEVSE = null)

        //public EVSEStatusDiff CalcEVSEStatusDiff(Dictionary<EVSE_Id, EVSEStatusType>  EVSEStatus,
        //                                         Func<EVSE, Boolean>                  IncludeEVSE  = null)
        //{

        //    if (EVSEStatus is null || EVSEStatus.Count == 0)
        //        return new EVSEStatusDiff(Timestamp.Now, Id, Name);

        //    #region Get data...

        //    var EVSEStatusDiff     = new EVSEStatusDiff(Timestamp.Now, Id, Name);

        //    // Only ValidEVSEIds!
        //    // Do nothing with manual EVSE Ids!
        //    var CurrentEVSEStates  = AllEVSEStatus(IncludeEVSE).
        //                                 //Where(KVP => ValidEVSEIds. Contains(KVP.Key) &&
        //                                 //            !ManualEVSEIds.Contains(KVP.Key)).
        //                                 ToDictionary(v => v.Key, v => v.Value);

        //    var OldEVSEIds         = new List<EVSE_Id>(CurrentEVSEStates.Keys);

        //    #endregion

        //    try
        //    {

        //        #region Find new and changed EVSE states

        //        // Only for ValidEVSEIds!
        //        // Do nothing with manual EVSE Ids!
        //        foreach (var NewEVSEStatus in EVSEStatus)
        //                                          //Where(KVP => ValidEVSEIds. Contains(KVP.Key) &&
        //                                          //            !ManualEVSEIds.Contains(KVP.Key)))
        //        {

        //            // Add to NewEVSEStates, if new EVSE was found!
        //            if (!CurrentEVSEStates.ContainsKey(NewEVSEStatus.Key))
        //                EVSEStatusDiff.AddNewStatus(NewEVSEStatus);

        //            else
        //            {

        //                // Add to CHANGED, if state of known EVSE changed!
        //                if (CurrentEVSEStates[NewEVSEStatus.Key] != NewEVSEStatus.Value)
        //                    EVSEStatusDiff.AddChangedStatus(NewEVSEStatus);

        //                // Remove EVSEId, as it was processed...
        //                OldEVSEIds.Remove(NewEVSEStatus.Key);

        //            }

        //        }

        //        #endregion

        //        #region Delete what is left in OldEVSEIds!

        //        EVSEStatusDiff.AddRemovedId(OldEVSEIds);

        //        #endregion

        //        return EVSEStatusDiff;

        //    }

        //    catch (Exception e)
        //    {

        //        while (e.InnerException is not null)
        //            e = e.InnerException;

        //        DebugX.Log("GetEVSEStatusDiff led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

        //    }

        //    // empty!
        //    return new EVSEStatusDiff(Timestamp.Now, Id, Name);

        //}

        #endregion

        #region ApplyEVSEStatusDiff (EVSEStatusDiff)

        public EVSEStatusDiff ApplyEVSEStatusDiff(EVSEStatusDiff EVSEStatusDiff)
        {

            #region Initial checks

            if (EVSEStatusDiff is null)
                throw new ArgumentNullException(nameof(EVSEStatusDiff),  "The given EVSE status diff must not be null!");

            #endregion

            foreach (var status in EVSEStatusDiff.NewStatus)
                SetEVSEStatus(status.Key, status.Value);

            foreach (var status in EVSEStatusDiff.ChangedStatus)
                SetEVSEStatus(status.Key, status.Value);

            return EVSEStatusDiff;

        }

        #endregion


        #region OnEVSEData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEDataChangedDelegate?         OnEVSEDataChanged;

        /// <summary>
        /// An event fired whenever the dynamic status of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEStatusChangedDelegate?       OnEVSEStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEAdminStatusChangedDelegate?  OnEVSEAdminStatusChanged;

        #endregion


        #region (internal) UpdateEVSEData        (Timestamp, EventTrackingId, EVSE, NewValue,       OldValue       = null, DataSource = null)

        /// <summary>
        /// Update the static data of an EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="EVSE">The changed EVSE.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        /// <param name="OldValue">The optional old value of the changed property.</param>
        /// <param name="DataSource">An optional data source or context for the EVSE data update.</param>
        internal async Task UpdateEVSEData(DateTimeOffset    Timestamp,
                                           EventTracking_Id  EventTrackingId,
                                           EVSE             EVSE,
                                           String            PropertyName,
                                           Object?           NewValue,
                                           Object?           OldValue     = null,
                                           Context?          DataSource   = null)
        {

            try
            {

                var onEVSEDataChanged = OnEVSEDataChanged;
                if (onEVSEDataChanged is not null)
                    await onEVSEDataChanged(Timestamp,
                                            EventTrackingId,
                                            EVSE,
                                            PropertyName,
                                            NewValue,
                                            OldValue,
                                            DataSource);

            }
            catch (Exception e)
            {
                DebugX.LogException(e, $"ChargingStationOperator '{Id}'.UpdateEVSEData of EVSE '{EVSE.Id}' property '{PropertyName}' from '{OldValue?.ToString() ?? "-"}' to '{NewValue?.ToString() ?? "-"}'");
            }

        }

        #endregion

        #region (internal) UpdateEVSEAdminStatus (Timestamp, EventTrackingId, EVSE, NewAdminStatus, OldAdminStatus = null, DataSource = null)

        /// <summary>
        /// Update the current admin status of an EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An event tracking identification for correlating this request with other events.</param>
        /// <param name="EVSE">The updated EVSE.</param>
        /// <param name="NewAdminStatus">The new EVSE admin status.</param>
        /// <param name="OldAdminStatus">The optional old EVSE admin status.</param>
        /// <param name="DataSource">An optional data source or context for the EVSE admin status update.</param>
        internal async Task UpdateEVSEAdminStatus(DateTimeOffset                     Timestamp,
                                                  EventTracking_Id                   EventTrackingId,
                                                  EVSE                              EVSE,
                                                  Timestamped<EVSEAdminStatusType>   NewAdminStatus,
                                                  Timestamped<EVSEAdminStatusType>?  OldAdminStatus   = null,
                                                  Context?                           DataSource       = null)
        {

            try
            {

                var onEVSEAdminStatusChanged = OnEVSEAdminStatusChanged;
                if (onEVSEAdminStatusChanged is not null)
                    await onEVSEAdminStatusChanged(Timestamp,
                                                   EventTrackingId,
                                                   EVSE,
                                                   NewAdminStatus,
                                                   OldAdminStatus,
                                                   DataSource);

            }
            catch (Exception e)
            {
                DebugX.LogException(e, $"ChargingStationOperator '{Id}'.UpdateEVSEAdminStatus of EVSE '{EVSE.Id}' from '{OldAdminStatus?.ToString() ?? "-"}' to '{NewAdminStatus}'");
            }

        }

        #endregion

        #region (internal) UpdateEVSEStatus      (Timestamp, EventTrackingId, EVSE, NewStatus,      OldStatus      = null, DataSource = null)

        /// <summary>
        /// Update the current status of an EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An event tracking identification for correlating this request with other events.</param>
        /// <param name="EVSE">The updated EVSE.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        /// <param name="OldStatus">The optional old EVSE status.</param>
        /// <param name="DataSource">An optional data source or context for the EVSE status update.</param>
        internal async Task UpdateEVSEStatus(DateTimeOffset                Timestamp,
                                             EventTracking_Id              EventTrackingId,
                                             EVSE                         EVSE,
                                             Timestamped<EVSEStatusType>   NewStatus,
                                             Timestamped<EVSEStatusType>?  OldStatus    = null,
                                             Context?                      DataSource   = null)
        {

            try
            {

                var onEVSEStatusChanged = OnEVSEStatusChanged;
                if (onEVSEStatusChanged is not null)
                    await onEVSEStatusChanged(Timestamp,
                                              EventTrackingId,
                                              EVSE,
                                              NewStatus,
                                              OldStatus,
                                              DataSource);

            }
            catch (Exception e)
            {
                DebugX.LogException(e, $"ChargingStationOperator '{Id}'.UpdateEVSEStatus of EVSE '{EVSE.Id}' from '{OldStatus}' to '{NewStatus}'");
            }

        }

        #endregion

        #endregion

        #region EVSE Groups

        #region Data

        private readonly EntityHashSet<ChargingStationOperator, EVSEGroup_Id, EVSEGroup> evseGroups;

        /// <summary>
        /// All EVSE groups registered within this charging station operator.
        /// </summary>
        public IEnumerable<EVSEGroup> EVSEGroups

            => evseGroups;

        #endregion

        #region EVSEGroupAddition

        internal readonly IVotingNotificator<DateTimeOffset, ChargingStationOperator, EVSEGroup, Boolean> evseGroupAddition;

        /// <summary>
        /// Called whenever an EVSE group will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, ChargingStationOperator, EVSEGroup, Boolean> OnEVSEGroupAddition

            => evseGroupAddition;

        #endregion

        #region EVSEGroupRemoval

        internal readonly IVotingNotificator<DateTimeOffset, ChargingStationOperator, EVSEGroup, Boolean> evseGroupRemoval;

        /// <summary>
        /// Called whenever an EVSE group will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, ChargingStationOperator, EVSEGroup, Boolean> OnEVSEGroupRemoval

            => evseGroupRemoval;

        #endregion


        #region CreateEVSEGroup     (Id,       Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new charging group having the given
        /// unique charging group identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station group.</param>
        /// <param name="Name">The official (multi-language) name of this EVSE group.</param>
        /// <param name="Description">An optional (multi-language) description of this EVSE group.</param>
        /// 
        /// <param name="Members">An enumeration of EVSEs member building this EVSE group.</param>
        /// <param name="MemberIds">An enumeration of EVSE identifications which are building this EVSE group.</param>
        /// <param name="AutoIncludeStations">A delegate deciding whether to include new EVSEs automatically into this group.</param>
        /// 
        /// <param name="StatusAggregationDelegate">A delegate called to aggregate the dynamic status of all subordinated EVSEs.</param>
        /// <param name="MaxGroupStatusListSize">The default size of the EVSE group status list.</param>
        /// <param name="MaxGroupAdminStatusListSize">The default size of the EVSE group admin status list.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging group failed.</param>
        public EVSEGroup CreateEVSEGroup(EVSEGroup_Id                                   Id,
                                         I18NString                                     Name,
                                         I18NString                                     Description                   = null,

                                         Brand                                          Brand                         = null,
                                         Priority?                                      Priority                      = null,
                                         ChargingTariff                                 Tariff                        = null,
                                         IEnumerable<DataLicense>                   DataLicenses                  = null,

                                         IEnumerable<EVSE>                              Members                       = null,
                                         IEnumerable<EVSE_Id>                           MemberIds                     = null,
                                         Func<EVSE_Id, Boolean>                         AutoIncludeEVSEIds            = null,
                                         Func<EVSE,   Boolean>                         AutoIncludeEVSEs              = null,

                                         Func<EVSEStatusReport, EVSEGroupStatusTypes>   StatusAggregationDelegate     = null,
                                         UInt16                                         MaxGroupStatusListSize        = EVSEGroup.DefaultMaxGroupStatusListSize,
                                         UInt16                                         MaxGroupAdminStatusListSize   = EVSEGroup.DefaultMaxGroupAdminStatusListSize,

                                         Action<EVSEGroup>                              OnSuccess                     = null,
                                         Action<ChargingStationOperator, EVSEGroup_Id>  OnError                       = null)

        {

            lock (evseGroups)
            {

                #region Initial checks

                if (evseGroups.ContainsId(Id))
                {

                    if (OnError is not null)
                        OnError?.Invoke(this, Id);

                    //throw new EVSEGroupAlreadyExists(this, Id);

                }

                if (Name.IsNullOrEmpty())
                    throw new ArgumentNullException(nameof(Name), "The name of the EVSE group must not be null or empty!");

                #endregion

                var evseGroup = new EVSEGroup(Id,
                                              this,
                                              Name,
                                              Description,

                                              Brand,
                                              Priority,
                                              Tariff,
                                              DataLicenses,

                                              Members,
                                              MemberIds,
                                              AutoIncludeEVSEIds,
                                              AutoIncludeEVSEs,
                                              StatusAggregationDelegate,
                                              MaxGroupAdminStatusListSize,
                                              MaxGroupStatusListSize);


                if (evseGroups.TryAdd(evseGroup,
                                      EventTracking_Id.New,
                                      null).Result == CommandResult.Success)
                {

                    evseGroup.OnEVSEDataChanged                  += UpdateEVSEData;
                    evseGroup.OnEVSEStatusChanged                += UpdateEVSEStatus;
                    evseGroup.OnEVSEAdminStatusChanged           += UpdateEVSEAdminStatus;

                    evseGroup.OnEVSEDataChanged                  += UpdateEVSEData;
                    evseGroup.OnEVSEStatusChanged                += UpdateEVSEStatus;
                    evseGroup.OnEVSEAdminStatusChanged           += UpdateEVSEAdminStatus;

                    //_EVSEGroup.OnDataChanged                                 += UpdateEVSEGroupData;
                    //_EVSEGroup.OnAdminStatusChanged                          += UpdateEVSEGroupAdminStatus;

                    OnSuccess?.Invoke(evseGroup);

                    return evseGroup;

                }

                return null;

            }

        }

        #endregion

        #region CreateEVSEGroup     (IdSuffix, Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new charging group having the given
        /// unique charging group identification.
        /// </summary>
        /// <param name="IdSuffix">The suffix of the unique identification of the new charging group.</param>
        /// <param name="Name">The official (multi-language) name of this EVSE group.</param>
        /// <param name="Description">An optional (multi-language) description of this EVSE group.</param>
        /// 
        /// <param name="Members">An enumeration of EVSEs member building this EVSE group.</param>
        /// <param name="MemberIds">An enumeration of EVSE identifications which are building this EVSE group.</param>
        /// <param name="AutoIncludeStations">A delegate deciding whether to include new EVSEs automatically into this group.</param>
        /// 
        /// <param name="StatusAggregationDelegate">A delegate called to aggregate the dynamic status of all subordinated EVSEs.</param>
        /// <param name="MaxGroupStatusListSize">The default size of the EVSE group status list.</param>
        /// <param name="MaxGroupAdminStatusListSize">The default size of the EVSE group admin status list.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging group failed.</param>
        public EVSEGroup CreateEVSEGroup(String                                         IdSuffix,
                                         I18NString                                     Name,
                                         I18NString                                     Description                   = null,

                                         Brand                                          Brand                         = null,
                                         Priority?                                      Priority                      = null,
                                         ChargingTariff                                 Tariff                        = null,
                                         IEnumerable<DataLicense>                       DataLicenses                  = null,

                                         IEnumerable<EVSE>                              Members                       = null,
                                         IEnumerable<EVSE_Id>                           MemberIds                     = null,
                                         Func<EVSE_Id, Boolean>                         AutoIncludeEVSEIds            = null,
                                         Func<EVSE,   Boolean>                         AutoIncludeEVSEs              = null,

                                         Func<EVSEStatusReport, EVSEGroupStatusTypes>   StatusAggregationDelegate     = null,
                                         UInt16                                         MaxGroupStatusListSize        = EVSEGroup.DefaultMaxGroupStatusListSize,
                                         UInt16                                         MaxGroupAdminStatusListSize   = EVSEGroup.DefaultMaxGroupAdminStatusListSize,

                                         Action<EVSEGroup>                              OnSuccess                     = null,
                                         Action<ChargingStationOperator, EVSEGroup_Id>  OnError                       = null)

        {

            #region Initial checks

            if (IdSuffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(IdSuffix), "The given suffix of the unique identification of the new charging group must not be null or empty!");

            #endregion

            return CreateEVSEGroup(EVSEGroup_Id.Parse(Id,
                                                      IdSuffix.Trim().ToUpper()),
                                   Name,
                                   Description,

                                   Brand,
                                   Priority,
                                   Tariff,
                                   DataLicenses,

                                   Members,
                                   MemberIds,
                                   AutoIncludeEVSEIds,
                                   AutoIncludeEVSEs,
                                   StatusAggregationDelegate,
                                   MaxGroupAdminStatusListSize,
                                   MaxGroupStatusListSize,
                                   OnSuccess,
                                   OnError);

        }

        #endregion

        #region GetOrCreateEVSEGroup(Id,       Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Get or create and register a new charging group having the given
        /// unique charging group identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station group.</param>
        /// <param name="Name">The official (multi-language) name of this EVSE group.</param>
        /// <param name="Description">An optional (multi-language) description of this EVSE group.</param>
        /// 
        /// <param name="Members">An enumeration of EVSEs member building this EVSE group.</param>
        /// <param name="MemberIds">An enumeration of EVSE identifications which are building this EVSE group.</param>
        /// <param name="AutoIncludeStations">A delegate deciding whether to include new EVSEs automatically into this group.</param>
        /// 
        /// <param name="StatusAggregationDelegate">A delegate called to aggregate the dynamic status of all subordinated EVSEs.</param>
        /// <param name="MaxGroupStatusListSize">The default size of the EVSE group status list.</param>
        /// <param name="MaxGroupAdminStatusListSize">The default size of the EVSE group admin status list.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging group failed.</param>
        public EVSEGroup GetOrCreateEVSEGroup(EVSEGroup_Id                                   Id,
                                              I18NString                                     Name,
                                              I18NString                                     Description                   = null,

                                              Brand                                          Brand                         = null,
                                              Priority?                                      Priority                      = null,
                                              ChargingTariff                                 Tariff                        = null,
                                              IEnumerable<DataLicense>                       DataLicenses                  = null,

                                              IEnumerable<EVSE>                              Members                       = null,
                                              IEnumerable<EVSE_Id>                           MemberIds                     = null,
                                              Func<EVSE_Id, Boolean>                         AutoIncludeEVSEIds            = null,
                                              Func<EVSE,   Boolean>                         AutoIncludeEVSEs              = null,

                                              Func<EVSEStatusReport, EVSEGroupStatusTypes>   StatusAggregationDelegate     = null,
                                              UInt16                                         MaxGroupStatusListSize        = EVSEGroup.DefaultMaxGroupStatusListSize,
                                              UInt16                                         MaxGroupAdminStatusListSize   = EVSEGroup.DefaultMaxGroupAdminStatusListSize,

                                              Action<EVSEGroup>                              OnSuccess                     = null,
                                              Action<ChargingStationOperator, EVSEGroup_Id>  OnError                       = null)

        {

            lock (evseGroups)
            {

                #region Initial checks

                if (Name.IsNullOrEmpty())
                    throw new ArgumentNullException(nameof(Name), "The name of the EVSE group must not be null or empty!");

                #endregion

                if (evseGroups.TryGet(Id, out EVSEGroup _EVSEGroup))
                    return _EVSEGroup;

                return CreateEVSEGroup(Id,
                                       Name,
                                       Description,

                                       Brand,
                                       Priority,
                                       Tariff,
                                       DataLicenses,

                                       Members,
                                       MemberIds,
                                       AutoIncludeEVSEIds,
                                       AutoIncludeEVSEs,
                                       StatusAggregationDelegate,
                                       MaxGroupAdminStatusListSize,
                                       MaxGroupStatusListSize,
                                       OnSuccess,
                                       OnError);

            }

        }

        #endregion

        #region GetOrCreateEVSEGroup(IdSuffix, Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Get or create and register a new charging group having the given
        /// unique charging group identification.
        /// </summary>
        /// <param name="IdSuffix">The suffix of the unique identification of the new charging group.</param>
        /// <param name="Name">The official (multi-language) name of this EVSE group.</param>
        /// <param name="Description">An optional (multi-language) description of this EVSE group.</param>
        /// 
        /// <param name="Members">An enumeration of EVSEs member building this EVSE group.</param>
        /// <param name="MemberIds">An enumeration of EVSE identifications which are building this EVSE group.</param>
        /// <param name="AutoIncludeStations">A delegate deciding whether to include new EVSEs automatically into this group.</param>
        /// 
        /// <param name="StatusAggregationDelegate">A delegate called to aggregate the dynamic status of all subordinated EVSEs.</param>
        /// <param name="MaxGroupStatusListSize">The default size of the EVSE group status list.</param>
        /// <param name="MaxGroupAdminStatusListSize">The default size of the EVSE group admin status list.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging group failed.</param>
        public EVSEGroup GetOrCreateEVSEGroup(String                                         IdSuffix,
                                              I18NString                                     Name,
                                              I18NString                                     Description                   = null,

                                              Brand                                          Brand                         = null,
                                              Priority?                                      Priority                      = null,
                                              ChargingTariff                                 Tariff                        = null,
                                              IEnumerable<DataLicense>                       DataLicenses                  = null,

                                              IEnumerable<EVSE>                              Members                       = null,
                                              IEnumerable<EVSE_Id>                           MemberIds                     = null,
                                              Func<EVSE_Id, Boolean>                         AutoIncludeEVSEIds            = null,
                                              Func<EVSE,   Boolean>                         AutoIncludeEVSEs              = null,

                                              Func<EVSEStatusReport, EVSEGroupStatusTypes>   StatusAggregationDelegate     = null,
                                              UInt16                                         MaxGroupStatusListSize        = EVSEGroup.DefaultMaxGroupStatusListSize,
                                              UInt16                                         MaxGroupAdminStatusListSize   = EVSEGroup.DefaultMaxGroupAdminStatusListSize,

                                              Action<EVSEGroup>                              OnSuccess                     = null,
                                              Action<ChargingStationOperator, EVSEGroup_Id>  OnError                       = null)


        {

            #region Initial checks

            if (IdSuffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(IdSuffix), "The given suffix of the unique identification of the new charging group must not be null or empty!");

            #endregion

            return GetOrCreateEVSEGroup(EVSEGroup_Id.Parse(Id, IdSuffix.Trim().ToUpper()),
                                        Name,
                                        Description,

                                        Brand,
                                        Priority,
                                        Tariff,
                                        DataLicenses,

                                        Members,
                                        MemberIds,
                                        AutoIncludeEVSEIds,
                                        AutoIncludeEVSEs,
                                        StatusAggregationDelegate,
                                        MaxGroupAdminStatusListSize,
                                        MaxGroupStatusListSize,
                                        OnSuccess,
                                        OnError);

        }

        #endregion


        #region TryGetEVSEGroup(Id, out EVSEGroup)

        /// <summary>
        /// Try to return to EVSE group for the given EVSE group identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station group.</param>
        /// <param name="EVSEGroup">The charging station group.</param>
        public Boolean TryGetEVSEGroup(EVSEGroup_Id    Id,
                                       out EVSEGroup?  EVSEGroup)

            => evseGroups.TryGet(Id, out EVSEGroup);

        #endregion


        #region RemoveEVSEGroup(EVSEGroupId, OnSuccess = null, OnError = null)

        /// <summary>
        /// All EVSE groups registered within this charging station operator.
        /// </summary>
        /// <param name="EVSEGroupId">The unique identification of the EVSE group to be removed.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new EVSE group after its successful deletion.</param>
        /// <param name="OnError">An optional delegate to be called whenever the deletion of the EVSE group failed.</param>
        public EVSEGroup RemoveEVSEGroup(EVSEGroup_Id                                    EVSEGroupId,
                                         Action<ChargingStationOperator, EVSEGroup>?     OnSuccess   = null,
                                         Action<ChargingStationOperator, EVSEGroup_Id>?  OnError     = null)
        {

            lock (evseGroups)
            {

                if (evseGroups.TryGet(EVSEGroupId, out var EVSEGroup) &&
                    evseGroupRemoval.SendVoting(
                        EventTracking_Id.New,
                        Timestamp.Now,
                                                this,
                                                EVSEGroup) &&
                    evseGroups.TryRemove(EVSEGroupId,
                                         out var _EVSEGroup,
                                         EventTracking_Id.New,
                                         null))
                {

                    OnSuccess?.Invoke(this, EVSEGroup);

                    evseGroupRemoval.SendNotification(
                        EventTracking_Id.New,
                        Timestamp.Now,
                                                      this,
                                                      _EVSEGroup);

                    return _EVSEGroup;

                }

                OnError?.Invoke(this, EVSEGroupId);

                return null;

            }

        }

        #endregion

        #region RemoveEVSEGroup(EVSEGroup,   OnSuccess = null, OnError = null)

        /// <summary>
        /// All EVSE groups registered within this charging station operator.
        /// </summary>
        /// <param name="EVSEGroup">The EVSE group to remove.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new EVSE group after its successful deletion.</param>
        /// <param name="OnError">An optional delegate to be called whenever the deletion of the EVSE group failed.</param>
        public EVSEGroup RemoveEVSEGroup(EVSEGroup                                   EVSEGroup,
                                         Action<ChargingStationOperator, EVSEGroup>  OnSuccess   = null,
                                         Action<ChargingStationOperator, EVSEGroup>  OnError     = null)
        {

            lock (evseGroups)
            {

                if (evseGroupRemoval.SendVoting(EventTracking_Id.New, Timestamp.Now,
                                                this,
                                                EVSEGroup) &&
                    evseGroups.TryRemove(EVSEGroup.Id,
                                         out var _EVSEGroup,
                                         EventTracking_Id.New,
                                         null))
                {

                    OnSuccess?.Invoke(this, _EVSEGroup);

                    evseGroupRemoval.SendNotification(EventTracking_Id.New, Timestamp.Now,
                                                      this,
                                                      _EVSEGroup);

                    return _EVSEGroup;

                }

                OnError?.Invoke(this, EVSEGroup);

                return EVSEGroup;

            }

        }

        #endregion

        #endregion


        #region ChargingTariffs

        #region Data

        private readonly ConcurrentDictionary<ChargingTariff_Id, ChargingTariff> chargingTariffs;

        /// <summary>
        /// All charging tariffs registered within this charging station operator.
        /// </summary>
        public IEnumerable<ChargingTariff> ChargingTariffs
            => chargingTariffs.Values;

        #endregion


        #region ChargingTariffAddition

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, Boolean> chargingTariffAddition;

        public IVotingNotificator<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, Boolean> ChargingTariffAddition
            => chargingTariffAddition;

        /// <summary>
        /// Called whenever a charging tariff will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, Boolean> OnChargingTariffAddition
            => chargingTariffAddition;

        #endregion

        #region ChargingTariffUpdate

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, ChargingTariff, Boolean> chargingTariffUpdate;

        public IVotingNotificator<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, ChargingTariff, Boolean> ChargingTariffUpdate
            => chargingTariffUpdate;

        /// <summary>
        /// Called whenever a charging Tariff will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, ChargingTariff, Boolean> OnChargingTariffUpdate
            => chargingTariffUpdate;

        #endregion

        #region ChargingTariffRemoval

        internal readonly IVotingNotificator<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, Boolean> chargingTariffRemoval;

        public IVotingNotificator<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, Boolean> ChargingTariffRemoval
            => chargingTariffRemoval;

        /// <summary>
        /// Called whenever a charging Tariff will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStationOperator, ChargingTariff, Boolean> OnChargingTariffRemoval
            => chargingTariffRemoval;

        #endregion


        #region GetOrCreateChargingTariff(Id,       Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Get or create and register a new charging Tariff having the given
        /// unique charging Tariff identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging Tariff.</param>
        /// <param name="Name">The official (multi-language) name of this charging Tariff.</param>
        /// <param name="Description">An optional (multi-language) description of this charging Tariff.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging Tariff after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging Tariff failed.</param>
        public Task<AddChargingTariffResult> GetOrCreateChargingTariff(ChargingTariff_Id                                                     Id,
                                                                       I18NString                                                            Name,
                                                                       I18NString                                                            Description,
                                                                       IEnumerable<ChargingTariffElement>                                    TariffElements,
                                                                       Currency                                                              Currency,
                                                                       Brand                                                                 Brand,
                                                                       URL                                                                   TariffURL,
                                                                       EnergyMix                                                             EnergyMix,

                                                                       String?                                                               DataSource                     = null,
                                                                       DateTimeOffset?                                                       LastChange                     = null,

                                                                       CustomDataNew?                                                        CustomData                     = null,
                                                                       UserDefinedDictionary?                                                InternalData                   = null,

                                                                       Action<ChargingTariff,                           EventTracking_Id>?  OnSuccess                      = null,
                                                                       Action<ChargingStationOperator, ChargingTariff, EventTracking_Id>?  OnError                        = null,

                                                                       Boolean                                                               SkipAddedNotifications         = false,
                                                                       Func<ChargingStationOperator_Id, ChargingTariff_Id, Boolean>?         AllowInconsistentOperatorIds   = null,
                                                                       EventTracking_Id?                                                     EventTrackingId                = null,
                                                                       User_Id?                                                              CurrentUserId                  = null)

        {

            #region Initial checks

            if (Name.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(Name), "The name of the charging Tariff must not be null or empty!");

            #endregion

            if (chargingTariffs.TryGetValue(Id, out var chargingTariff))
            {

                return Task.FromResult(
                           AddChargingTariffResult.Success(
                               chargingTariff,
                               EventTracking_Id.New,
                               Id,
                               this,
                               this
                           )
                       );
            }

            return GetOrCreateChargingTariff(Id,
                                             Name,
                                             Description,
                                             TariffElements,
                                             Currency,
                                             Brand,
                                             TariffURL,
                                             EnergyMix,

                                             DataSource,
                                             LastChange,

                                             CustomData,
                                             InternalData,

                                             OnSuccess,
                                             OnError,

                                             SkipAddedNotifications,
                                             AllowInconsistentOperatorIds,
                                             EventTrackingId,
                                             CurrentUserId);

        }

        #endregion

        #region GetOrCreateChargingTariff(IdSuffix, Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Get or create and register a new charging Tariff having the given
        /// unique charging Tariff identification.
        /// </summary>
        /// <param name="IdSuffix">The suffix of the unique identification of the new charging Tariff.</param>
        /// <param name="Name">The official (multi-language) name of this charging Tariff.</param>
        /// <param name="Description">An optional (multi-language) description of this charging Tariff.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to configure the new charging Tariff after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging Tariff failed.</param>
        public Task<AddChargingTariffResult> GetOrCreateChargingTariff(String                                                                IdSuffix,
                                                                       I18NString                                                            Name,
                                                                       I18NString                                                            Description,
                                                                       IEnumerable<ChargingTariffElement>                                    TariffElements,
                                                                       Currency                                                              Currency,
                                                                       Brand                                                                 Brand,
                                                                       URL                                                                   TariffURL,
                                                                       EnergyMix                                                             EnergyMix,

                                                                       String?                                                               DataSource                     = null,
                                                                       DateTimeOffset?                                                       LastChange                     = null,

                                                                       CustomDataNew?                                                        CustomData                     = null,
                                                                       UserDefinedDictionary?                                                InternalData                   = null,

                                                                       Action<ChargingTariff,                           EventTracking_Id>?  OnSuccess                      = null,
                                                                       Action<ChargingStationOperator, ChargingTariff, EventTracking_Id>?  OnError                        = null,

                                                                       Boolean                                                               SkipAddedNotifications         = false,
                                                                       Func<ChargingStationOperator_Id, ChargingTariff_Id, Boolean>?         AllowInconsistentOperatorIds   = null,
                                                                       EventTracking_Id?                                                     EventTrackingId                = null,
                                                                       User_Id?                                                              CurrentUserId                  = null)


        {

            #region Initial checks

            if (IdSuffix.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(IdSuffix), "The given suffix of the unique identification of the new charging Tariff must not be null or empty!");

            #endregion

            return GetOrCreateChargingTariff(ChargingTariff_Id.Parse(Id, IdSuffix.Trim()),
                                             Name,
                                             Description,
                                             TariffElements,
                                             Currency,
                                             Brand,
                                             TariffURL,
                                             EnergyMix,

                                             DataSource,
                                             LastChange,

                                             CustomData,
                                             InternalData,

                                             OnSuccess,
                                             OnError,

                                             SkipAddedNotifications,
                                             AllowInconsistentOperatorIds,
                                             EventTrackingId,
                                             CurrentUserId);

        }

        #endregion


        #region AddChargingTariff           (ChargingTariff,                             OnSuccess       = null, OnError = null, ...)

        /// <summary>
        /// Add a new charging tariff.
        /// </summary>
        /// <param name="ChargingTariff">A new charging tariff.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to be called after the successful addition of the charging tariff.</param>
        /// <param name="OnError">An optional delegate to be called whenever the addition of the new charging tariff failed.</param>
        /// 
        /// <param name="SkipAddedNotifications">Whether to skip sending the 'OnAdded' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddChargingTariffResult> AddChargingTariff(ChargingTariff                                                      ChargingTariff,

                                                                     Action<ChargingTariff,                          EventTracking_Id>?  OnSuccess                      = null,
                                                                     Action<ChargingStationOperator, ChargingTariff, EventTracking_Id>?  OnError                        = null,

                                                                     Boolean                                                             SkipAddedNotifications         = false,
                                                                     Func<ChargingStationOperator_Id, ChargingTariff_Id, Boolean>?       AllowInconsistentOperatorIds   = null,
                                                                     EventTracking_Id?                                                   EventTrackingId                = null,
                                                                     User_Id?                                                            CurrentUserId                  = null)
        {

            #region Initial checks

            EventTrackingId              ??= EventTracking_Id.New;
            AllowInconsistentOperatorIds ??= ((chargingStationOperatorId, chargingTariffId) => false);

            if (ChargingTariff.Id.OperatorId != this.Id && !AllowInconsistentOperatorIds(this.Id, ChargingTariff.Id))
                return AddChargingTariffResult.Error(
                           ChargingTariff,
                           $"The operator identification of the given charging tariff '{ChargingTariff.Id.OperatorId}' is invalid!".ToI18NString(),
                           EventTrackingId,
                           this.Id,
                           this
                       );

            #endregion


            if (chargingTariffs.TryAdd(ChargingTariff.Id, ChargingTariff))
            {

                //ToDo: Persistency
                await Task.Delay(1);

                OnSuccess?.Invoke(ChargingTariff,
                                  EventTrackingId);

                return AddChargingTariffResult.Success(
                           ChargingTariff,
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            }

            OnError?.Invoke(this,
                            ChargingTariff,
                            EventTrackingId);

            return AddChargingTariffResult.Error(
                       ChargingTariff,
                       "Could not add the given charging tariff!".ToI18NString(),
                       EventTrackingId,
                       Id,
                       this,
                       this
                   );

        }

        #endregion

        #region AddChargingTariffIfNotExists(ChargingTariff,                             OnSuccess       = null,                 ...)

        /// <summary>
        /// Add a new charging tariff, but do not fail when this charging tariff already exists.
        /// </summary>
        /// <param name="ChargingTariff">A new charging tariff.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to be called after the successful addition of the charging tariff.</param>
        /// 
        /// <param name="SkipAddedNotifications">Whether to skip sending the 'OnAdded' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddChargingTariffResult> AddChargingTariffIfNotExists(ChargingTariff                                                 ChargingTariff,

                                                                                Action<ChargingTariff, EventTracking_Id>?                      OnSuccess                      = null,

                                                                                Boolean                                                        SkipAddedNotifications         = false,
                                                                                Func<ChargingStationOperator_Id, ChargingTariff_Id, Boolean>?  AllowInconsistentOperatorIds   = null,
                                                                                EventTracking_Id?                                              EventTrackingId                = null,
                                                                                User_Id?                                                       CurrentUserId                  = null)
        {

            #region Initial checks

            EventTrackingId              ??= EventTracking_Id.New;
            AllowInconsistentOperatorIds ??= ((chargingStationOperatorId, chargingTariffId) => false);

            if (ChargingTariff.Id.OperatorId != Id && !AllowInconsistentOperatorIds(Id, ChargingTariff.Id))
                return AddChargingTariffResult.ArgumentError(
                           ChargingTariff,
                           $"The operator identification of the given charging tariff '{ChargingTariff.Id.OperatorId}' is invalid!".ToI18NString(),
                           EventTrackingId,
                           Id,
                           this
                       );

            #endregion

            if (chargingTariffs.TryAdd(ChargingTariff.Id, ChargingTariff))
            {

                //ToDo: Persistency
                await Task.Delay(1);

                OnSuccess?.Invoke(ChargingTariff,
                                  EventTrackingId);

                return AddChargingTariffResult.Success(
                           ChargingTariff,
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            }

            return AddChargingTariffResult.NoOperation(
                       ChargingTariff,
                       EventTrackingId,
                       Id,
                       this,
                       this
                   );

        }

        #endregion

        #region AddOrUpdateChargingTariff   (ChargingTariff,   OnAdditionSuccess = null, OnUpdateSuccess = null, OnError = null, ...)

        /// <summary>
        /// Add a new or update an existing charging tariff.
        /// </summary>
        /// <param name="ChargingTariff">A new or updated charging tariff.</param>
        /// 
        /// <param name="OnAdditionSuccess">An optional delegate to be called after the successful addition of the charging tariff.</param>
        /// <param name="OnUpdateSuccess">An optional delegate to be called after the successful update of the charging tariff.</param>
        /// <param name="OnError">An optional delegate to be called whenever the addition of the new charging tariff failed.</param>
        /// 
        /// <param name="SkipAddOrUpdatedUpdatedNotifications">Whether to skip sending the 'OnAddedOrUpdated' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddOrUpdateChargingTariffResult> AddOrUpdateChargingTariff(ChargingTariff                                                       ChargingTariff,

                                                                                     Action<ChargingTariff,                           EventTracking_Id>?  OnAdditionSuccess                      = null,
                                                                                     Action<ChargingTariff,          ChargingTariff, EventTracking_Id>?  OnUpdateSuccess                        = null,
                                                                                     Action<ChargingStationOperator, ChargingTariff, EventTracking_Id>?  OnError                                = null,

                                                                                     Boolean                                                               SkipAddOrUpdatedUpdatedNotifications   = false,
                                                                                     Func<ChargingStationOperator_Id, ChargingTariff_Id, Boolean>?         AllowInconsistentOperatorIds           = null,
                                                                                     EventTracking_Id?                                                     EventTrackingId                        = null,
                                                                                     User_Id?                                                              CurrentUserId                          = null)
        {

            #region Initial checks

            EventTrackingId              ??= EventTracking_Id.New;
            AllowInconsistentOperatorIds ??= ((chargingStationOperatorId, chargingTariffId) => false);

            if (ChargingTariff.Id.OperatorId != this.Id && !AllowInconsistentOperatorIds(this.Id, ChargingTariff.Id))
                return AddOrUpdateChargingTariffResult.ArgumentError(
                           ChargingTariff,
                           $"The operator identification of the given charging tariff '{ChargingTariff.Id.OperatorId}' is invalid!".ToI18NString(),
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            #endregion


            if (chargingTariffs.TryGetValue(ChargingTariff.Id, out var existingChargingTariff))
            {

                var xx1 = existingChargingTariff.Equals(ChargingTariff);

                var xx2 = existingChargingTariff == ChargingTariff; //FalseFriend!!!

                if (chargingTariffs.TryUpdate(ChargingTariff.Id,
                                              ChargingTariff,
                                              existingChargingTariff))
                {

                    //ToDo: Persistency
                    await Task.Delay(1);

                    OnUpdateSuccess?.Invoke(ChargingTariff,
                                            existingChargingTariff,
                                            EventTrackingId);

                    return AddOrUpdateChargingTariffResult.Updated(
                               ChargingTariff,
                               EventTrackingId,
                               Id,
                               this,
                               this
                           );

                }
                else
                {

                    OnError?.Invoke(this,
                                    ChargingTariff,
                                    EventTrackingId);

                    return AddOrUpdateChargingTariffResult.Error(
                               ChargingTariff,
                               "Error!".ToI18NString(),
                               EventTrackingId,
                               Id,
                               this,
                               this
                           );

                }

            }

            else
            {

                if (chargingTariffs.TryAdd(ChargingTariff.Id, ChargingTariff))
                {

                    //ToDo: Persistency
                    await Task.Delay(1);

                    OnAdditionSuccess?.Invoke(ChargingTariff,
                                              EventTrackingId);

                    return AddOrUpdateChargingTariffResult.Added(
                               ChargingTariff,
                               EventTrackingId,
                               Id,
                               this,
                               this
                           );

                }
                else
                {

                    OnError?.Invoke(this,
                                    ChargingTariff,
                                    EventTrackingId);

                    return AddOrUpdateChargingTariffResult.Error(
                               ChargingTariff,
                               "Error!".ToI18NString(),
                               EventTrackingId,
                               Id,
                               this,
                               this
                           );

                }

            }

        }

        #endregion

        #region UpdateChargingTariff        (ChargingTariff,                             OnUpdateSuccess = null, OnError = null, ...)

        /// <summary>
        /// Update the given charging tariff.
        /// </summary>
        /// <param name="ChargingTariff">A charging tariff.</param>
        /// 
        /// <param name="OnUpdateSuccess">An optional delegate to be called after the successful update of the charging tariff.</param>
        /// <param name="OnError">An optional delegate to be called whenever the update of the new charging tariff failed.</param>
        /// 
        /// <param name="SkipUpdatedNotifications">Whether to skip sending the 'OnUpdated' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<UpdateChargingTariffResult> UpdateChargingTariff(ChargingTariff                                                       ChargingTariff,

                                                                           Action<ChargingTariff,          ChargingTariff, EventTracking_Id>?  OnUpdateSuccess                = null,
                                                                           Action<ChargingStationOperator, ChargingTariff, EventTracking_Id>?  OnError                        = null,

                                                                           Boolean                                                               SkipUpdatedNotifications       = false,
                                                                           Func<ChargingStationOperator_Id, ChargingTariff_Id, Boolean>?         AllowInconsistentOperatorIds   = null,
                                                                           EventTracking_Id?                                                     EventTrackingId                = null,
                                                                           User_Id?                                                              CurrentUserId                  = null)
        {

            var eventTrackingId = EventTrackingId ?? EventTracking_Id.New;

            if (!TryGetChargingTariffById(ChargingTariff.Id, out var OldChargingTariff))
                return UpdateChargingTariffResult.ArgumentError(
                           ChargingTariff,
                           $"The given charging tariff '{ChargingTariff.Id}' does not exists in this API!".ToI18NString(),
                           eventTrackingId,
                           Id,
                           this,
                           this
                       );

            //if (ChargingTariff.API is not null && ChargingTariff.API != this)
            //    return UpdateChargingTariffResult.ArgumentError(ChargingTariff,
            //                                                  eventTrackingId,
            //                                                  nameof(ChargingTariff.API),
            //                                                  "The given charging tariff is not attached to this API!");

            //ChargingTariff.API = this;


            //await WriteToDatabaseFile(updateChargingTariff_MessageType,
            //                          ChargingTariff.ToJSON(),
            //                          eventTrackingId,
            //                          CurrentChargingTariffId);

            chargingTariffs.TryRemove(OldChargingTariff.Id, out _);

            //ChargingTariff.CopyAllLinkedDataFrom(OldChargingTariff);
            chargingTariffs.TryAdd(ChargingTariff.Id, ChargingTariff);

            OnUpdateSuccess?.Invoke(ChargingTariff,
                                    ChargingTariff,
                                    eventTrackingId);

            //var OnChargingTariffUpdatedLocal = OnChargingTariffUpdated;
            //if (OnChargingTariffUpdatedLocal is not null)
            //    await OnChargingTariffUpdatedLocal.Invoke(Timestamp.Now,
            //                                            ChargingTariff,
            //                                            OldChargingTariff,
            //                                            eventTrackingId, 
            //                                            CurrentChargingTariffId);

            //if (!SkipChargingTariffUpdatedNotifications)
            //    await SendNotifications(ChargingTariff,
            //                            updateChargingTariff_MessageType,
            //                            OldChargingTariff,
            //                            eventTrackingId,
            //                            CurrentChargingTariffId);

            return UpdateChargingTariffResult.Success(
                       ChargingTariff,
                       eventTrackingId,
                       Id,
                       this,
                       this
                   );

        }

        #endregion

        #region UpdateChargingTariff        (ChargingTariffId, UpdateDelegate,           OnUpdateSuccess = null, OnError = null, ...)

        /// <summary>
        /// Update the given charging tariff.
        /// </summary>
        /// <param name="ChargingTariffId">A charging tariff identification.</param>
        /// <param name="UpdateDelegate">A delegate for updating the given charging tariff.</param>
        /// 
        /// <param name="OnUpdateSuccess">An optional delegate to be called after the successful update of the charging tariff.</param>
        /// <param name="OnError">An optional delegate to be called whenever the update of the new charging tariff failed.</param>
        /// 
        /// <param name="SkipUpdatedNotifications">Whether to skip sending the 'OnUpdated' event.</param>
        /// <param name="AllowInconsistentOperatorIds">A delegate to decide whether to allow inconsistent charging station operator identifications.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<UpdateChargingTariffResult> UpdateChargingTariff(ChargingTariff_Id                                                     ChargingTariffId,
                                                                           Action<ChargingTariff>                                               UpdateDelegate,

                                                                           Action<ChargingTariff,          ChargingTariff, EventTracking_Id>?  OnUpdateSuccess                = null,
                                                                           Action<ChargingStationOperator, ChargingTariff, EventTracking_Id>?  OnError                        = null,

                                                                           Boolean                                                               SkipUpdatedNotifications       = false,
                                                                           Func<ChargingStationOperator_Id, ChargingTariff_Id, Boolean>?         AllowInconsistentOperatorIds   = null,
                                                                           EventTracking_Id?                                                     EventTrackingId                = null,
                                                                           User_Id?                                                              CurrentUserId                  = null)
        {

            EventTrackingId ??= EventTracking_Id.New;

            if (!chargingTariffs.TryRemove(ChargingTariffId, out var oldChargingTariff))
            {

                return UpdateChargingTariffResult.ArgumentError(
                           oldChargingTariff,
                           $"The given charging tariff '{ChargingTariffId}' does not exists!".ToI18NString(),
                           EventTrackingId,
                           Id,
                           this,
                           this
                       );

            }

            //if (ChargingTariff.API is not null && ChargingTariff.API != this)
            //    return UpdateChargingTariffResult.ArgumentError(ChargingTariff,
            //                                                  eventTrackingId,
            //                                                  nameof(ChargingTariff.API),
            //                                                  "The given charging tariff is not attached to this API!");

            //ChargingTariff.API = this;


            //await WriteToDatabaseFile(updateChargingTariff_MessageType,
            //                          ChargingTariff.ToJSON(),
            //                          eventTrackingId,
            //                          CurrentChargingTariffId);

            var newChargingTariff = oldChargingTariff.Clone();
            UpdateDelegate(oldChargingTariff);

            //ChargingTariff.CopyAllLinkedDataFrom(OldChargingTariff);
            chargingTariffs.TryAdd(newChargingTariff.Id, newChargingTariff);

            OnUpdateSuccess?.Invoke(newChargingTariff,
                                    oldChargingTariff,
                                    EventTrackingId);

            //var OnChargingTariffUpdatedLocal = OnChargingTariffUpdated;
            //if (OnChargingTariffUpdatedLocal is not null)
            //    await OnChargingTariffUpdatedLocal.Invoke(Timestamp.Now,
            //                                            ChargingTariff,
            //                                            OldChargingTariff,
            //                                            eventTrackingId, 
            //                                            CurrentChargingTariffId);

            //if (!SkipChargingTariffUpdatedNotifications)
            //    await SendNotifications(ChargingTariff,
            //                            updateChargingTariff_MessageType,
            //                            OldChargingTariff,
            //                            eventTrackingId,
            //                            CurrentChargingTariffId);

            return UpdateChargingTariffResult.Success(
                       newChargingTariff,
                       EventTrackingId,
                       Id,
                       this,
                       this
                   );

        }

        #endregion

        #region RemoveChargingTariff(ChargingTariffId, OnSuccess = null, OnError = null)

        /// <summary>
        /// Remove the given charging Tariff.
        /// </summary>
        /// <param name="Id">The unique identification of the charging Tariff.</param>
        /// 
        /// <param name="OnSuccess">An optional delegate to be called after the successful removal of the charging Tariff.</param>
        /// <param name="OnError">An optional delegate to be called whenever the removal of the new charging Tariff failed.</param>
        /// 
        /// <param name="SkipRemovedNotifications">Whether to skip sending the 'OnRemoved' event.</param>
        /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<DeleteChargingTariffResult>

            RemoveChargingTariff(ChargingTariff_Id                                    Id,

                                 Action<ChargingTariff,          EventTracking_Id>?  OnSuccess                  = null,
                                 Action<ChargingStationOperator, EventTracking_Id>?  OnError                    = null,

                                 Boolean                                              SkipRemovedNotifications   = false,
                                 EventTracking_Id?                                    EventTrackingId            = null,
                                 User_Id?                                             CurrentUserId              = null)

        {

            EventTrackingId ??= EventTracking_Id.New;

            if (chargingTariffs.TryRemove(Id, out var chargingTariff))
            {

                return DeleteChargingTariffResult.Success(
                           chargingTariff,
                           EventTrackingId,
                           this.Id,
                           this
                       );

            }

            return DeleteChargingTariffResult.ArgumentError(
                       Id,
                       "error".ToI18NString(),
                       EventTrackingId,
                       this.Id,
                       this
                   );

        }

        #endregion


        #region ChargingTariffExists(ChargingTariff)

        /// <summary>
        /// Check if the given ChargingTariff is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="ChargingTariff">A charging pool.</param>
        public Boolean ChargingTariffExists(ChargingTariff ChargingTariff)

            => ChargingTariffs.Contains(ChargingTariff);

        #endregion

        #region ChargingTariffExists(ChargingTariffId)

        /// <summary>
        /// Determines whether the given user identification exists within this API.
        /// </summary>
        /// <param name="ChargingTariffId">The unique identification of an user.</param>
        public Boolean ChargingTariffExists(ChargingTariff_Id ChargingTariffId)

            => ChargingTariffId.IsNotNullOrEmpty &&
               chargingTariffs.ContainsKey(ChargingTariffId);

        /// <summary>
        /// Determines whether the given user identification exists within this API.
        /// </summary>
        /// <param name="ChargingTariffId">The unique identification of an user.</param>
        public Boolean ChargingTariffExists(ChargingTariff_Id? ChargingTariffId)

            => ChargingTariffId.HasValue &&
               ChargingTariffId.Value.IsNotNullOrEmpty &&
               chargingTariffs.ContainsKey(ChargingTariffId.Value);

        #endregion

        #region GetChargingTariff   (Id)

        /// <summary>
        /// Return to charging Tariff for the given charging Tariff identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging Tariff.</param>
        public ChargingTariff? GetChargingTariff(ChargingTariff_Id Id)
        {

            if (chargingTariffs.TryGetValue(Id, out var chargingTariff))
                return chargingTariff;

            return null;

        }

        #endregion

        #region TryGetChargingTariffById(Id, out ChargingTariff)

        /// <summary>
        /// Try to return to charging Tariff for the given charging Tariff identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging Tariff.</param>
        /// <param name="ChargingTariff">The charging Tariff.</param>
        public Boolean TryGetChargingTariffById(ChargingTariff_Id     Id,
                                                out ChargingTariff?  ChargingTariff)

            => chargingTariffs.TryGetValue(Id, out ChargingTariff);

        #endregion

        public IEnumerable<ChargingTariff>   GetChargingTariffs  (ChargingPool_Id?       ChargingPoolId        = null,
                                                                   ChargingStation_Id?    ChargingStationId     = null,
                                                                   EVSE_Id?               EVSEId                = null,
                                                                   ChargingConnector_Id?  ChargingConnectorId   = null,
                                                                   EMobilityProvider_Id?  EMobilityProviderId   = null)
        {

            return Array.Empty<ChargingTariff>();

        }

        public IEnumerable<ChargingTariff_Id> GetChargingTariffIds(ChargingPool_Id?       ChargingPoolId        = null,
                                                                   ChargingStation_Id?    ChargingStationId     = null,
                                                                   EVSE_Id?               EVSEId                = null,
                                                                   ChargingConnector_Id?  ChargingConnectorId   = null,
                                                                   EMobilityProvider_Id?  EMobilityProviderId   = null)
        {

            return Array.Empty<ChargingTariff_Id>();

        }

        #endregion

        #region ChargingTariffGroups

        #region ChargingTariffGroups

        private readonly ConcurrentDictionary<ChargingTariffGroup_Id, ChargingTariffGroup> chargingTariffGroups;

        /// <summary>
        /// All charging Tariff groups registered within this charging station operator.
        /// </summary>
        public IEnumerable<ChargingTariffGroup> ChargingTariffGroups
            => chargingTariffGroups.Values;

        #endregion


        #region CreateChargingTariffGroup     (IdSuffix, Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new charging Tariff group having the given
        /// unique charging Tariff identification.
        /// </summary>
        /// <param name="IdSuffix">The suffix of the unique identification of the charging Tariff group.</param>
        /// <param name="Description">An optional (multi-language) description of this charging Tariff group.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new charging Tariff group after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging Tariff group failed.</param>
        public ChargingTariffGroup? CreateChargingTariffGroup(String                                                     IdSuffix,
                                                              I18NString                                                 Description,
                                                              Action<ChargingTariffGroup>?                               OnSuccess   = null,
                                                              Action<ChargingStationOperator, ChargingTariffGroup_Id>?  OnError     = null)

        {

            lock (chargingTariffGroups)
            {

                #region Initial checks

                var newGroupId = ChargingTariffGroup_Id.Parse(Id, IdSuffix);

                if (chargingTariffGroups.TryGetValue(newGroupId, out var existingGroup))
                {

                    if (OnError is not null)
                        OnError?.Invoke(this, newGroupId);

                    return existingGroup;

                }

                #endregion

                var chargingTariffGroup = new ChargingTariffGroup(newGroupId,
                                                                  this,
                                                                  Description);


                if (chargingTariffGroups.TryAdd(chargingTariffGroup.Id, chargingTariffGroup))
                {

                    //_ChargingTariffGroup.OnEVSEDataChanged                             += UpdateEVSEData;
                    //_ChargingTariffGroup.OnEVSEStatusChanged                           += UpdateEVSEStatus;
                    //_ChargingTariffGroup.OnEVSEAdminStatusChanged                      += UpdateEVSEAdminStatus;

                    //_ChargingTariffGroup.OnChargingStationDataChanged                  += UpdateChargingStationData;
                    //_ChargingTariffGroup.OnChargingStationStatusChanged                += UpdateChargingStationStatus;
                    //_ChargingTariffGroup.OnChargingStationAdminStatusChanged           += UpdateChargingStationAdminStatus;

                    ////_ChargingTariffGroup.OnDataChanged                                 += UpdateChargingTariffGroupData;
                    ////_ChargingTariffGroup.OnAdminStatusChanged                          += UpdateChargingTariffGroupAdminStatus;

                    OnSuccess?.Invoke(chargingTariffGroup);

                    return chargingTariffGroup;

                }

                return null;

            }

        }

        #endregion

        #region GetOrCreateChargingTariffGroup(Id,       Name, Description = null, ..., OnSuccess = null, OnError = null)

        /// <summary>
        /// Get or create and register a new charging Tariff having the given
        /// unique charging Tariff identification.
        /// </summary>
        /// <param name="IdSuffix">The suffix of the unique identification of the charging Tariff group.</param>
        /// <param name="Description">An optional (multi-language) description of this charging Tariff.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new charging Tariff after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging Tariff failed.</param>
        public ChargingTariffGroup? GetOrCreateChargingTariffGroup(String                                                    IdSuffix,
                                                                   I18NString                                                Description,
                                                                   Action<ChargingTariffGroup>?                              OnSuccess   = null,
                                                                   Action<ChargingStationOperator, ChargingTariffGroup_Id>?  OnError     = null)

        {

            lock (chargingTariffGroups)
            {

                if (chargingTariffGroups.TryGetValue(ChargingTariffGroup_Id.Parse(Id, IdSuffix), out var chargingTariffGroup))
                    return chargingTariffGroup;

                return CreateChargingTariffGroup(IdSuffix,
                                                 Description,
                                                 OnSuccess,
                                                 OnError);

            }

        }

        #endregion


        #region GetChargingTariffGroup(Id)

        /// <summary>
        /// Return to charging Tariff for the given charging Tariff identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging Tariff.</param>
        public ChargingTariffGroup? GetChargingTariffGroup(ChargingTariffGroup_Id Id)
        {

            if (chargingTariffGroups.TryGetValue(Id, out var chargingTariffGroup))
                return chargingTariffGroup;

            return null;

        }

        #endregion

        #region TryGetChargingTariffGroup(Id, out ChargingTariffGroup)

        /// <summary>
        /// Try to return to charging Tariff for the given charging Tariff identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging Tariff.</param>
        /// <param name="ChargingTariffGroup">The charging Tariff.</param>
        public Boolean TryGetChargingTariffGroup(ChargingTariffGroup_Id   Id,
                                                 out ChargingTariffGroup? ChargingTariffGroup)

            => chargingTariffGroups.TryGetValue(Id, out ChargingTariffGroup);

        #endregion


        #region ChargingTariffGroupRemoval

        internal readonly IVotingNotificator<DateTimeOffset, ChargingStationOperator, ChargingTariffGroup, Boolean> chargingTariffGroupRemoval;

        public IVotingNotificator<DateTimeOffset, ChargingStationOperator, ChargingTariffGroup, Boolean> ChargingTariffGroupRemoval
            => chargingTariffGroupRemoval;

        /// <summary>
        /// Called whenever a charging Tariff group will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, ChargingStationOperator, ChargingTariffGroup, Boolean> OnChargingTariffGroupRemoval

            => chargingTariffGroupRemoval;

        #endregion

        #region RemoveChargingTariffGroup(ChargingTariffGroupId, OnSuccess = null, OnError = null)

        /// <summary>
        /// All charging Tariffs registered within this charging station operator.
        /// </summary>
        /// <param name="ChargingTariffGroupId">The unique identification of the charging Tariff to be removed.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new charging Tariff after its successful deletion.</param>
        /// <param name="OnError">An optional delegate to be called whenever the deletion of the charging Tariff failed.</param>
        public ChargingTariffGroup? RemoveChargingTariffGroup(ChargingTariffGroup_Id                                     ChargingTariffGroupId,
                                                              Action<ChargingStationOperator, ChargingTariffGroup>?     OnSuccess   = null,
                                                              Action<ChargingStationOperator, ChargingTariffGroup_Id>?  OnError     = null)
        {

            lock (chargingTariffGroups)
            {

                if (chargingTariffGroups.Remove(ChargingTariffGroupId, out var chargingTariffGroup))
                {

                    OnSuccess?.Invoke(this, chargingTariffGroup);

                    chargingTariffGroupRemoval.SendNotification(EventTracking_Id.New, Timestamp.Now,
                                                                this,
                                                                chargingTariffGroup);

                    return chargingTariffGroup;

                }

                OnError?.Invoke(this, ChargingTariffGroupId);

                return null;

            }

        }

        #endregion

        #region RemoveChargingTariffGroup(ChargingTariffGroup,   OnSuccess = null, OnError = null)

        /// <summary>
        /// All charging Tariffs registered within this charging station operator.
        /// </summary>
        /// <param name="ChargingTariffGroup">The charging Tariff to remove.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new charging Tariff after its successful deletion.</param>
        /// <param name="OnError">An optional delegate to be called whenever the deletion of the charging Tariff failed.</param>
        public ChargingTariffGroup? RemoveChargingTariffGroup(ChargingTariffGroup                                     ChargingTariffGroup,
                                                              Action<ChargingStationOperator, ChargingTariffGroup>?  OnSuccess   = null,
                                                              Action<ChargingStationOperator, ChargingTariffGroup>?  OnError     = null)
        {

            lock (chargingTariffGroups)
            {

                if (chargingTariffGroups.Remove(ChargingTariffGroup.Id, out var chargingTariffGroup))
                {

                    OnSuccess?.Invoke(this, chargingTariffGroup);

                    chargingTariffGroupRemoval.SendNotification(EventTracking_Id.New, Timestamp.Now,
                                                                this,
                                                                chargingTariffGroup);

                    return chargingTariffGroup;

                }

                OnError?.Invoke(this, ChargingTariffGroup);

                return ChargingTariffGroup;

            }

        }

        #endregion

        #endregion


        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given charging station operator.
        /// </summary>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a roaming network.</param>
        public JObject ToJSON(Boolean                                                     Embedded                                  = false,
                              InfoStatus                                                  ExpandRoamingNetworkId                    = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandDataLicenses                        = InfoStatus.ShowIdOnly,
                              CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null,
                              CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                              CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                              CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null,
                              CustomJObjectSerializerDelegate<ChargingConnector>?         CustomChargingConnectorSerializer         = null)
        {

            try
            {

                var json = JSONObject.Create(

                                     new JProperty("@id",                 Id.ToString()),

                               !Embedded
                                   ? new JProperty("@context",            JSONLDContext)
                                   : null,

                                     new JProperty("name",                Name.       ToJSON()),

                               Description.IsNotNullOrEmpty()
                                   ? new JProperty("description",         Description.ToJSON())
                                   : null,

                               DataSource.IsNotNullOrEmpty()
                                   ? new JProperty("dataSource",          DataSource)
                                   : null,

                               ExpandDataLicenses != InfoStatus.Hidden
                                   ? ExpandDataLicenses.Switch(
                                         () => new JProperty("dataLicenseIds",  new JArray(DataLicenses.SafeSelect(license => license.Id.ToString()))),
                                         () => new JProperty("dataLicenses",    DataLicenses.ToJSON()))
                                   : null,

                               ExpandRoamingNetworkId != InfoStatus.Hidden
                                   ? ExpandRoamingNetworkId.Switch(
                                         () => new JProperty("roamingNetworkId",   RoamingNetwork.Id. ToString()),
                                         () => new JProperty("roamingNetwork",     RoamingNetwork.    ToJSON(Embedded:                          true,
                                                                                                             ExpandChargingStationOperatorIds:  InfoStatus.Hidden,
                                                                                                             ExpandChargingPoolIds:             InfoStatus.Hidden,
                                                                                                             ExpandChargingStationIds:          InfoStatus.Hidden,
                                                                                                             ExpandEVSEIds:                     InfoStatus.Hidden,
                                                                                                             ExpandBrandIds:                    InfoStatus.Hidden,
                                                                                                             ExpandDataLicenses:                InfoStatus.Hidden)))
                                   : null,

                               Address is not null
                                   ? new JProperty("address",             Address.ToJSON(Embedded: true))
                                   : null,

                               // API
                               // MainKeys
                               // RobotKeys
                               // Endpoints
                               // DNS SRV

                               Logo.IsNotNullOrEmpty()
                                   ? new JProperty("logos",               JSONArray.Create(
                                                                              JSONObject.Create(
                                                                                  new JProperty("uri",          Logo.            ToString())
                                                                                  //new JProperty("description",  I18NString.Empty.ToJSON())
                                                                              )
                                                                          ))
                                   : null,

                               Homepage.HasValue
                                   ? new JProperty("homepage",            Homepage.ToString())
                                   : null,

                               HotlinePhoneNumber.HasValue
                                   ? new JProperty("hotline",             HotlinePhoneNumber.ToString())
                                   : null,


                               ExpandChargingPoolIds != InfoStatus.Hidden && ChargingPools.Any()
                                   ? ExpandChargingPoolIds.Switch(

                                         () => new JProperty("chargingPoolIds",
                                                             new JArray(ChargingPoolIds().
                                                                                                OrderBy(poolId => poolId).
                                                                                                Select (poolId => poolId.ToString()))),

                                         () => new JProperty("chargingPools",
                                                             new JArray(ChargingPools.
                                                                                                OrderBy(poolId => poolId).
                                                                                                ToJSON (Embedded:                           true,
                                                                                                        ExpandRoamingNetworkId:             InfoStatus.Hidden,
                                                                                                        ExpandChargingStationOperatorId:    InfoStatus.Hidden,
                                                                                                        ExpandChargingStationIds:           InfoStatus.Expanded,
                                                                                                        ExpandEVSEIds:                      InfoStatus.Expanded,
                                                                                                        ExpandBrandIds:                     InfoStatus.ShowIdOnly,
                                                                                                        ExpandDataLicenses:                 InfoStatus.Hidden,
                                                                                                        CustomChargingPoolSerializer:       CustomChargingPoolSerializer,
                                                                                                        CustomChargingStationSerializer:    CustomChargingStationSerializer,
                                                                                                        CustomEVSESerializer:               CustomEVSESerializer,
                                                                                                        CustomChargingConnectorSerializer:  CustomChargingConnectorSerializer))))
                                   : null,


                               ExpandChargingStationIds != InfoStatus.Hidden && ChargingStations.Any()
                                   ? ExpandChargingStationIds.Switch(

                                         () => new JProperty("chargingStationIds",
                                                             new JArray(ChargingStationIds().
                                                                                                OrderBy(stationid => stationid).
                                                                                                Select (stationid => stationid.ToString()))),

                                         () => new JProperty("chargingStations",
                                                             new JArray(ChargingStations.
                                                                                                OrderBy(station   => station).
                                                                                                ToJSON (Embedded:                         true,
                                                                                                        ExpandRoamingNetworkId:           InfoStatus.Hidden,
                                                                                                        ExpandChargingStationOperatorId:  InfoStatus.Hidden,
                                                                                                        ExpandChargingPoolId:             InfoStatus.Hidden,
                                                                                                        ExpandEVSEIds:                    InfoStatus.Expanded,
                                                                                                        ExpandBrandIds:                   InfoStatus.ShowIdOnly,
                                                                                                        ExpandDataLicenses:               InfoStatus.Hidden,
                                                                                                        CustomChargingStationSerializer:  CustomChargingStationSerializer,
                                                                                                        CustomEVSESerializer:             CustomEVSESerializer))))
                                   : null,


                               ExpandEVSEIds != InfoStatus.Hidden && EVSEs.Any()
                                   ? ExpandEVSEIds.Switch(

                                         () => new JProperty("EVSEIds",
                                                             new JArray(EVSEIds().
                                                                                                OrderBy(evseId => evseId).
                                                                                                Select (evseId => evseId.ToString()))),

                                         () => new JProperty("EVSEs",
                                                             new JArray(EVSEs.
                                                                                                OrderBy(evse   => evse).
                                                                                                ToJSON (Embedded:                         true,
                                                                                                        ExpandRoamingNetworkId:           InfoStatus.Hidden,
                                                                                                        ExpandChargingStationOperatorId:  InfoStatus.Hidden,
                                                                                                        ExpandChargingPoolId:             InfoStatus.Hidden,
                                                                                                        ExpandChargingStationId:          InfoStatus.Hidden,
                                                                                                        ExpandBrandIds:                   InfoStatus.ShowIdOnly,
                                                                                                        ExpandDataLicenses:               InfoStatus.Hidden,
                                                                                                        CustomEVSESerializer:             CustomEVSESerializer))))
                                   : null,


                               ExpandBrandIds != InfoStatus.Hidden && Brands.Any()
                                   ? ExpandBrandIds.Switch(

                                         () => new JProperty("brandIds",
                                                             new JArray(Brands.                 Select (brand   => brand.Id).
                                                                                                OrderBy(brandId => brandId).
                                                                                                Select (brandId => brandId.ToString()))),

                                         () => new JProperty("brands",
                                                             new JArray(Brands.
                                                                                                OrderBy(brand => brand).
                                                                                                ToJSON (Embedded:                         true,
                                                                                                        ExpandDataLicenses:               InfoStatus.ShowIdOnly))))
                                   : null,

                               CustomData.HasValues
                                   ? new JProperty("customData",            CustomData.ToJObject())
                                   : null

                               );

                return CustomChargingStationOperatorSerializer is not null
                           ? CustomChargingStationOperatorSerializer(this, json)
                           : json;

            }
            catch (Exception e)
            {
                return new JObject(
                           new JProperty("@id",         Id.ToString()),
                           new JProperty("@context",    JSONLDContext),
                           new JProperty("exception",   e.Message),
                           new JProperty("stackTrace",  e.StackTrace)
                       );
            }

        }

        #endregion


        #region (private) LogEvent(Logger, LogHandler, ...)

        private Task LogEvent<TDelegate>(TDelegate?                                         Logger,
                                         Func<TDelegate, Task>                              LogHandler,
                                         [CallerArgumentExpression(nameof(Logger))] String  EventName   = "",
                                         [CallerMemberName()]                       String  Command     = "")

            where TDelegate : Delegate

                => LogEvent(
                       nameof(ChargingStationOperator),
                       Logger,
                       LogHandler,
                       EventName,
                       Command
                   );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingStationOperator1, ChargingStationOperator2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperator1">A charging station operator.</param>
        /// <param name="ChargingStationOperator2">Another charging station operator.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingStationOperator ChargingStationOperator1,
                                           ChargingStationOperator ChargingStationOperator2)
        {

            if (Object.ReferenceEquals(ChargingStationOperator1, ChargingStationOperator2))
                return true;

            if (ChargingStationOperator1 is null || ChargingStationOperator2 is null)
                return false;

            return ChargingStationOperator1.Equals(ChargingStationOperator2);

        }

        #endregion

        #region Operator != (ChargingStationOperator1, ChargingStationOperator2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperator1">A charging station operator.</param>
        /// <param name="ChargingStationOperator2">Another charging station operator.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingStationOperator ChargingStationOperator1,
                                           ChargingStationOperator ChargingStationOperator2)

            => !(ChargingStationOperator1 == ChargingStationOperator2);

        #endregion

        #region Operator <  (ChargingStationOperator1, ChargingStationOperator2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperator1">A charging station operator.</param>
        /// <param name="ChargingStationOperator2">Another charging station operator.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingStationOperator ChargingStationOperator1,
                                          ChargingStationOperator ChargingStationOperator2)

            => ChargingStationOperator1 is null
                   ? throw new ArgumentNullException(nameof(ChargingStationOperator1), "The given charging station operator must not be null!")
                   : ChargingStationOperator1.CompareTo(ChargingStationOperator2) < 0;

        #endregion

        #region Operator <= (ChargingStationOperator1, ChargingStationOperator2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperator1">A charging station operator.</param>
        /// <param name="ChargingStationOperator2">Another charging station operator.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingStationOperator ChargingStationOperator1,
                                           ChargingStationOperator ChargingStationOperator2)

            => !(ChargingStationOperator1 > ChargingStationOperator2);

        #endregion

        #region Operator >  (ChargingStationOperator1, ChargingStationOperator2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperator1">A charging station operator.</param>
        /// <param name="ChargingStationOperator2">Another charging station operator.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingStationOperator ChargingStationOperator1,
                                          ChargingStationOperator ChargingStationOperator2)

            => ChargingStationOperator1 is null
                   ? throw new ArgumentNullException(nameof(ChargingStationOperator1), "The given charging station operator must not be null!")
                   : ChargingStationOperator1.CompareTo(ChargingStationOperator2) > 0;

        #endregion

        #region Operator >= (ChargingStationOperator1, ChargingStationOperator2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperator1">A charging station operator.</param>
        /// <param name="ChargingStationOperator2">Another charging station operator.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingStationOperator ChargingStationOperator1,
                                           ChargingStationOperator ChargingStationOperator2)

            => !(ChargingStationOperator1 < ChargingStationOperator2);

        #endregion

        #endregion

        #region IComparable<ChargingStationOperator> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingStationOperator chargingStationOperator
                   ? CompareTo(chargingStationOperator)
                   : throw new ArgumentException("The given object is not a charging station operator!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingStationOperator)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator object to compare with.</param>
        public Int32 CompareTo(ChargingStationOperator? ChargingStationOperator)
        {

            if (ChargingStationOperator is null)
                throw new ArgumentNullException(nameof(ChargingStationOperator), "The given charging station operator must not be null!");

            return Id.CompareTo(ChargingStationOperator.Id);

        }

        #endregion

        #endregion

        #region IEquatable<ChargingStationOperator> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationOperator chargingStationOperator &&
                   Equals(chargingStationOperator);

        #endregion

        #region Equals(Operator)

        /// <summary>
        /// Compares two Charging Station Operators for equality.
        /// </summary>
        /// <param name="Operator">An Charging Station Operator to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(ChargingStationOperator? ChargingStationOperator)

            => ChargingStationOperator is not null &&

               Id.Equals(ChargingStationOperator.Id);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Get the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(
                   "'",
                   Name.FirstText(),
                   "' (",
                   Id.ToString(),
                   ") in ",
                   RoamingNetwork.Id.ToString()
               );

        #endregion

    }

}
