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
using System.Collections.Immutable;
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
    public sealed partial class ChargingStationOperator : AImmutableEMobilityEntity<ChargingStationOperator_Id,
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


        }

        #endregion

        #region Brands

        /// <summary>
        /// All brands registered for this charging station operator.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<Brand> immutableBrands = [];
        [Optional, SlowData]
        public System.Collections.Immutable.ImmutableArray<Brand> Brands
            => ImmutablePOIValues.CopyItems(immutableBrands);

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
                return ImmutablePOIValues.Copy(address);
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


        }

        #endregion

        #region DataLicenses

        /// <summary>
        /// The license(s) of the charging station operator data.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<DataLicense> immutableDataLicenses = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<DataLicense> DataLicenses
            => ImmutablePOIValues.CopyItems(immutableDataLicenses);


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
            this.chargingPools = new EntityHashSet<ChargingStationOperator, ChargingPool_Id, ChargingPool>(this);

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

            => ImmutablePOIValues.CopyItems(chargingPools);

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


        #endregion

        //ToDo: Charging Pool Groups

        #region Charging Stations

        #region Data

        private readonly ConcurrentDictionary<ChargingStation_Id, ChargingStation> chargingStationLookup = new();

        /// <summary>
        /// Return an enumeration of all charging stations.
        /// </summary>
        public IEnumerable<ChargingStation> ChargingStations

            => ImmutablePOIValues.CopyItems(chargingStationLookup.Values);

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


        #endregion

        #region Charging Station Groups

        #region Data

        private ImmutableDictionary<ChargingStationGroup_Id, ChargingStationGroup> chargingStationGroups = ImmutableDictionary<ChargingStationGroup_Id, ChargingStationGroup>.Empty;

        /// <summary>
        /// All charging station groups registered within this charging station operator.
        /// </summary>
        public IEnumerable<ChargingStationGroup> ChargingStationGroups

            => ImmutablePOIValues.CopyItems(chargingStationGroups.Values);

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


        #endregion

        #region EVSEs

        #region Data

        private readonly ConcurrentDictionary<EVSE_Id, EVSE> evseLookup = new();

        /// <summary>
        /// Return an enumeration of all EVSEs.
        /// </summary>
        public IEnumerable<EVSE> EVSEs

            => ImmutablePOIValues.CopyItems(evseLookup.Values);

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


        #endregion

        #region EVSE Groups

        #region Data

        private ImmutableDictionary<EVSEGroup_Id, EVSEGroup> evseGroups = ImmutableDictionary<EVSEGroup_Id, EVSEGroup>.Empty;

        /// <summary>
        /// All EVSE groups registered within this charging station operator.
        /// </summary>
        public IEnumerable<EVSEGroup> EVSEGroups

            => ImmutablePOIValues.CopyItems(evseGroups.Values);

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


        #region TryGetEVSEGroup(Id, out EVSEGroup)

        /// <summary>
        /// Try to return to EVSE group for the given EVSE group identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station group.</param>
        /// <param name="EVSEGroup">The charging station group.</param>
        public Boolean TryGetEVSEGroup(EVSEGroup_Id    Id,
                                       out EVSEGroup?  EVSEGroup)

            => evseGroups.TryGetValue(Id, out EVSEGroup);

        #endregion


        #endregion


        #region ChargingTariffs

        #region Data

        private readonly ConcurrentDictionary<ChargingTariff_Id, ChargingTariff> chargingTariffs = [];

        /// <summary>
        /// All charging tariffs registered within this charging station operator.
        /// </summary>
        public IEnumerable<ChargingTariff> ChargingTariffs
            => ImmutablePOIValues.CopyItems(chargingTariffs.Values);

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

            if (EMobilityProviderId is not null)
                throw new NotSupportedException("Provider-specific tariff agreements are not represented in the POI model.");
            if (ChargingConnectorId is not null && EVSEId is null)
                throw new ArgumentException("Connector tariff queries require an EVSE identifier because connector IDs are local.", nameof(EVSEId));
            if (ChargingPoolId is null && ChargingStationId is null && EVSEId is null)
                return ChargingTariffs;

            var ids = EVSEs.Where(evse =>
                    (ChargingPoolId is null || evse.ChargingPool?.Id == ChargingPoolId) &&
                    (ChargingStationId is null || evse.ChargingStation?.Id == ChargingStationId) &&
                    (EVSEId is null || evse.Id == EVSEId))
                .SelectMany(evse =>
                {
                    var connectors = evse.ChargingConnectors.Where(connector => ChargingConnectorId is null || connector.Id == ChargingConnectorId).ToArray();
                    return ChargingConnectorId is not null && connectors.Length == 0 ? [] :
                        evse.ChargingTariffIds.Concat(connectors.SelectMany(connector => connector.TariffIds));
                }).ToHashSet();
            return ChargingTariffs.Where(tariff => ids.Contains(tariff.Id)).ToArray();

        }

        public IEnumerable<ChargingTariff_Id> GetChargingTariffIds(ChargingPool_Id?       ChargingPoolId        = null,
                                                                   ChargingStation_Id?    ChargingStationId     = null,
                                                                   EVSE_Id?               EVSEId                = null,
                                                                   ChargingConnector_Id?  ChargingConnectorId   = null,
                                                                   EMobilityProvider_Id?  EMobilityProviderId   = null)
        {

            return GetChargingTariffs(ChargingPoolId, ChargingStationId, EVSEId, ChargingConnectorId, EMobilityProviderId).Select(tariff => tariff.Id);

        }

        #endregion

        #region ChargingTariffGroups

        #region ChargingTariffGroups

        private ImmutableDictionary<ChargingTariffGroup_Id, ChargingTariffGroup> chargingTariffGroups = ImmutableDictionary<ChargingTariffGroup_Id, ChargingTariffGroup>.Empty;

        /// <summary>
        /// All charging Tariff groups registered within this charging station operator.
        /// </summary>
        public IEnumerable<ChargingTariffGroup> ChargingTariffGroups
            => ImmutablePOIValues.CopyItems(chargingTariffGroups.Values);

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
                              CustomJObjectSerializerDelegate<ChargingConnector>?         CustomChargingConnectorSerializer         = null,
                              InfoStatus                                                  ExpandChargingTariffIds                    = InfoStatus.Expanded)
        {

            try
            {

                var json = JSONObject.Create(

                                     new JProperty("@id",                 Id.ToString()),

                               ExpandChargingTariffIds != InfoStatus.Hidden && ChargingTariffs.Any()
                                   ? ExpandChargingTariffIds.Switch(
                                         () => new JProperty("chargingTariffIds", new JArray(ChargingTariffs.OrderBy(tariff => tariff.Id.ToString(), StringComparer.Ordinal).Select(tariff => tariff.Id.ToString()))),
                                         () => new JProperty("chargingTariffs", new JArray(ChargingTariffs.OrderBy(tariff => tariff.Id.ToString(), StringComparer.Ordinal).Select(tariff => tariff.ToJSON(Embedded: true, ExpandBrandIds: ExpandBrandIds, ExpandDataLicenses: ExpandDataLicenses)))))
                                   : null,

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
                                                                                                        ExpandChargingStationIds:           ExpandChargingStationIds,
                                                                                                        ExpandEVSEIds:                      ExpandEVSEIds,
                                                                                                        ExpandBrandIds:                     ExpandBrandIds,
                                                                                                        ExpandDataLicenses:                 ExpandDataLicenses,
                                                                                                        CustomChargingPoolSerializer:       CustomChargingPoolSerializer,
                                                                                                        CustomChargingStationSerializer:    CustomChargingStationSerializer,
                                                                                                        CustomEVSESerializer:               CustomEVSESerializer,
                                                                                                        CustomChargingConnectorSerializer:  CustomChargingConnectorSerializer))))
                                   : null,


                               ExpandChargingPoolIds != InfoStatus.Expanded && ExpandChargingStationIds != InfoStatus.Hidden && ChargingStations.Any()
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


                               ExpandChargingPoolIds != InfoStatus.Expanded && ExpandChargingStationIds != InfoStatus.Expanded && ExpandEVSEIds != InfoStatus.Hidden && EVSEs.Any()
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

                json["EVSEGroups"] = POIJSON.Children(EVSEGroups);
                json["chargingStationGroups"] = POIJSON.Children(ChargingStationGroups);
                json["chargingPoolGroups"] = POIJSON.Children(ChargingPoolGroups);
                json["chargingTariffGroups"] = POIJSON.Children(ChargingTariffGroups);

                return POIRepresentation.AddETags(this, CustomChargingStationOperatorSerializer is not null
                           ? CustomChargingStationOperatorSerializer(this, json)
                           : json);

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
    }

}
