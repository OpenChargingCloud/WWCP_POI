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

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Styx;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Styx.Arrows;


#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate for filtering roaming networks.
    /// </summary>
    /// <param name="RoamingNetwork">A roaming network to include.</param>
    public delegate Boolean IncludeRoamingNetworkDelegate(RoamingNetwork RoamingNetwork);


    /// <summary>
    /// Extension methods for the roaming networks.
    /// </summary>
    public static class RoamingNetworkExtensions2
    {

        #region ToJSON(this RoamingNetworks, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given roaming networks collection.
        /// </summary>
        /// <param name="RoamingNetworks">An enumeration of roaming networks.</param>
        /// <param name="Embedded">Whether this roaming network is embedded into another data structure.</param>
        /// <param name="Skip">The optional number of roaming networks to skip.</param>
        /// <param name="Take">The optional number of roaming networks to return.</param>
        public static JArray ToJSON(this IEnumerable<RoamingNetwork>                           RoamingNetworks,
                                    Boolean                                                    Embedded                                  = false,
                                    UInt64?                                                    Skip                                      = null,
                                    UInt64?                                                    Take                                      = null,

                                    InfoStatus                                                 ExpandRoamingNetworkIds                   = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingStationOperatorIds          = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandDataLicenses                        = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandEMobilityProviderId                 = InfoStatus.ShowIdOnly,

                                    CustomJObjectSerializerDelegate<RoamingNetwork>?           CustomRoamingNetworkSerializer            = null,
                                    CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null,
                                    CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                                    CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                                    CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null)
        {

            #region Initial checks

            if (RoamingNetworks is null)
                return [];

            #endregion

            return new JArray(
                       RoamingNetworks.
                           SkipTakeFilter(Skip, Take).
                           Select        (roamingNetwork => roamingNetwork.ToJSON(

                                                                Embedded,

                                                                ExpandRoamingNetworkIds,
                                                                ExpandChargingStationOperatorIds,
                                                                ExpandChargingPoolIds,
                                                                ExpandChargingStationIds,
                                                                ExpandEVSEIds,
                                                                ExpandBrandIds,
                                                                ExpandDataLicenses,
                                                                ExpandEMobilityProviderId,

                                                                CustomRoamingNetworkSerializer,
                                                                CustomChargingStationOperatorSerializer,
                                                                CustomChargingPoolSerializer,
                                                                CustomChargingStationSerializer,
                                                                CustomEVSESerializer

                                                            ))
                   );

        }

        #endregion

    }


    /// <summary>
    /// A Electric Vehicle Roaming Network is a service abstraction to allow multiple
    /// independent roaming services to be delivered over the same infrastructure.
    /// This can e.g. be a differentation of service levels (premiun, basic,
    /// discount) or allow a simplified testing (production, qa, featureX, ...)
    /// </summary>
    public sealed partial class RoamingNetwork : AImmutableEMobilityEntity<RoamingNetwork_Id,
                                                   RoamingNetworkAdminStatusType,
                                                   RoamingNetworkStatusType>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const               String                                             JSONLDContext                          = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork";

        protected static readonly  SemaphoreSlim                                      eMobilityProvidersSemaphore            = new (1, 1);
        protected static readonly  SemaphoreSlim                                      chargingStationOperatorsSemaphore      = new (1, 1);

        protected static readonly  TimeSpan                                           SemaphoreSlimTimeout                   = TimeSpan.FromSeconds(5);

        protected static readonly  Byte                                               MinEMobilityProviderIdLength           = 5;
        protected static readonly  Byte                                               MinEMobilityProviderNameLength         = 5;

        protected static readonly  Byte                                               MinChargingStationOperatorIdLength     = 5;
        protected static readonly  Byte                                               MinChargingStationOperatorNameLength   = 5;

        #endregion

        #region Properties

        #region Data licenses

        private readonly ConcurrentDictionary<DataLicense_Id, DataLicense> dataLicenses = [];

        /// <summary>
        /// The license of the roaming network data.
        /// </summary>
        [Mandatory]
        public IEnumerable<DataLicense> DataLicenses
            => ImmutablePOIValues.CopyItems(dataLicenses.Values);

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network having the given unique roaming network identification.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network.</param>
        /// <param name="Name">The multi-language name of the roaming network.</param>
        /// <param name="Description">An optional multi-language description of the roaming network.</param>
        /// <param name="InitialAdminStatus">The initial admin status of the roaming network.</param>
        /// <param name="InitialStatus">The initial status of the roaming network.</param>
        /// <param name="MaxAdminStatusScheduleSize">The maximum number of entries in the admin status history.</param>
        /// <param name="MaxStatusScheduleSize">The maximum number of entries in the status history.</param>
        public RoamingNetwork(RoamingNetwork_Id               Id,
                              I18NString?                     Name                         = null,
                              I18NString?                     Description                  = null,
                              RoamingNetworkAdminStatusType?  InitialAdminStatus           = null,
                              RoamingNetworkStatusType?       InitialStatus                = null,
                              UInt16?                         MaxAdminStatusScheduleSize   = null,
                              UInt16?                         MaxStatusScheduleSize        = null,

                              String?                         DataSource                   = null,
                              DateTimeOffset?                 Created                      = null,
                              DateTimeOffset?                 LastChange                   = null,

                              CustomDataNew?                  CustomData                   = null,
                              UserDefinedDictionary?          InternalData                 = null)

            : base(Id,
                   Name,
                   Description,
                   InitialAdminStatus         ?? RoamingNetworkAdminStatusType.Operational,
                   InitialStatus              ?? RoamingNetworkStatusType.Available,
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {


        }

        #endregion


        #region Data/(Admin-)Status management

        /// <summary>
        /// An event fired whenever the dynamic status changed.
        /// </summary>
        public event OnRoamingNetworkStatusChangedDelegate?       OnStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnRoamingNetworkAdminStatusChangedDelegate?  OnAdminStatusChanged;

        #endregion


        #region E-Mobility Providers...

        #region EMobilityProviders

        private readonly ConcurrentDictionary<EMobilityProvider_Id, EMobilityProvider> projectedEMobilityProviders = [];

        private ConcurrentDictionary<EMobilityProvider_Id, EMobilityProvider> eMobilityProviders
        {
            get { EnsureSnapshotProjection(); return projectedEMobilityProviders; }
        }

        /// <summary>
        /// Return all e-mobility providers registered within this roaming network.
        /// </summary>
        public IEnumerable<EMobilityProvider> EMobilityProviders
            => ImmutablePOIValues.CopyItems(eMobilityProviders.Values);

        #endregion


        #region ContainsEMobilityProvider  (EMobilityProvider)

        /// <summary>
        /// Check if the given e-mobility provider is already present within the roaming network.
        /// </summary>
        /// <param name="EMobilityProvider">An e-mobility provider.</param>
        public Boolean ContainsEMobilityProvider(EMobilityProvider EMobilityProvider)

            => eMobilityProviders.ContainsKey(EMobilityProvider.Id);

        #endregion

        #region ContainsEMobilityProvider  (EMobilityProviderId)

        /// <summary>
        /// Check if the given e-mobility provider identification is already present within the roaming network.
        /// </summary>
        /// <param name="EMobilityProviderId">The unique identification of the e-mobility provider.</param>
        public Boolean ContainsEMobilityProvider(EMobilityProvider_Id EMobilityProviderId)

            => eMobilityProviders.ContainsKey(EMobilityProviderId);

        #endregion

        #region GetEMobilityProviderById   (EMobilityProviderId)

        public EMobilityProvider? GetEMobilityProviderById(EMobilityProvider_Id  EMobilityProviderId)
        {

            if (eMobilityProviders.TryGetValue(EMobilityProviderId, out var eMobilityProvider))
                return eMobilityProvider;

            return null;

        }

        public EMobilityProvider? GetEMobilityProviderById(EMobilityProvider_Id? EMobilityProviderId)
        {

            if (EMobilityProviderId.HasValue &&
                eMobilityProviders.TryGetValue(EMobilityProviderId.Value, out var eMobilityProvider))
                return eMobilityProvider;

            return null;

        }

        #endregion

        #region TryGetEMobilityProviderById(EMobilityProviderId, out EMobilityProvider)

        public Boolean TryGetEMobilityProviderById(EMobilityProvider_Id                        EMobilityProviderId,
                                                   [NotNullWhen(true)] out EMobilityProvider?  EMobilityProvider)

            => eMobilityProviders.TryGetValue(EMobilityProviderId, out EMobilityProvider);

        public Boolean TryGetEMobilityProviderById(EMobilityProvider_Id?                       EMobilityProviderId,
                                                   [NotNullWhen(true)] out EMobilityProvider?  EMobilityProvider)
        {

            if (!EMobilityProviderId.HasValue)
            {
                EMobilityProvider = null;
                return false;
            }

            return eMobilityProviders.TryGetValue(EMobilityProviderId.Value, out EMobilityProvider);

        }

        #endregion


        #endregion


        #region Charging station operators

        private readonly ConcurrentDictionary<ChargingStationOperator_Id, ChargingStationOperator> projectedChargingStationOperators = [];

        private ConcurrentDictionary<ChargingStationOperator_Id, ChargingStationOperator> chargingStationOperators
        {
            get { EnsureSnapshotProjection(); return projectedChargingStationOperators; }
        }

        /// <summary>
        /// The operators in this immutable network version.
        /// </summary>
        public IEnumerable<ChargingStationOperator> ChargingStationOperators
            => ImmutablePOIValues.CopyItems(chargingStationOperators.Values);

        #region ChargingStationOperatorExists    (ChargingStationOperator)

        /// <summary>
        /// Check if the given charging station operator identification is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of the charging station operator.</param>
        public Boolean ChargingStationOperatorExists(ChargingStationOperator  ChargingStationOperator)
            => chargingStationOperators.ContainsKey(ChargingStationOperator.Id);

        /// <summary>
        /// Determines whether the given user identification exists within this API.
        /// </summary>
        /// <param name="UserId">The unique identification of an user.</param>
        public Boolean ChargingStationOperatorExists(ChargingStationOperator_Id? ChargingStationOperatorId)

            => ChargingStationOperatorId.HasValue &&
               ChargingStationOperatorId.IsNotNullOrEmpty() &&
               chargingStationOperators.ContainsKey(ChargingStationOperatorId.Value);

        #endregion

        #region GetChargingStationOperatorById   (ChargingStationOperatorId)

        /// <summary>
        /// Get the charging station operator having the given unique identification.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of a charging station operator.</param>
        public ChargingStationOperator? GetChargingStationOperatorById(ChargingStationOperator_Id ChargingStationOperatorId)
        {

            if (chargingStationOperators.TryGetValue(ChargingStationOperatorId, out var chargingStationOperator))
                return chargingStationOperator;

            return null;

        }

        #endregion

        #region TryGetChargingStationOperatorById(ChargingStationOperatorId, out ChargingStationOperator)

        /// <summary>
        /// Try to get the charging station operator having the given unique identification.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of a charging station operator.</param>
        /// <param name="ChargingStationOperator">The charging station operator.</param>
        public Boolean TryGetChargingStationOperatorById(ChargingStationOperator_Id?                       ChargingStationOperatorId,
                                                         [NotNullWhen(true)] out ChargingStationOperator?  ChargingStationOperator)
        {

            if (ChargingStationOperatorId.HasValue &&
                chargingStationOperators.TryGetValue(ChargingStationOperatorId.Value, out ChargingStationOperator))
            {
                return true;
            }

            ChargingStationOperator = null;
            return false;

        }

        #endregion


        #region ChargingStationOperatorAdminStatus(IncludeChargingStationOperator = null)

        /// <summary>
        /// Return the admin status of all charging station operators registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingStationOperator">An optional delegate for filtering charging station operators.</param>
        public IEnumerable<ChargingStationOperatorAdminStatus> ChargingStationOperatorAdminStatus(IncludeChargingStationOperatorDelegate? IncludeChargingStationOperator = null)

            => IncludeChargingStationOperator is null

                   ? chargingStationOperators.Values.
                         Select(chargingStationOperator => new ChargingStationOperatorAdminStatus(
                                                               chargingStationOperator.Id,
                                                               chargingStationOperator.AdminStatus
                                                           ))

                   : chargingStationOperators.Values.
                         Where (chargingStationOperator => IncludeChargingStationOperator(chargingStationOperator)).
                         Select(chargingStationOperator => new ChargingStationOperatorAdminStatus(
                                                               chargingStationOperator.Id,
                                                               chargingStationOperator.AdminStatus
                                                           ));

        #endregion


        #endregion

        /// <summary>
        /// All charging tariffs registered with the operators of this roaming network.
        /// </summary>
        public IEnumerable<ChargingTariff> ChargingTariffs
            => ImmutablePOIValues.CopyItems(ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingTariffs));

        #region ChargingPools...

        #region ChargingPools

        /// <summary>
        /// Return all charging pools registered within this roaming network.
        /// </summary>
        public IEnumerable<ChargingPool> ChargingPools

            => ImmutablePOIValues.CopyItems(chargingStationOperators.Values.SelectMany(cso => cso.ChargingPools));

        #endregion


        #region ContainsChargingPool(ChargingPool)

        /// <summary>
        /// Check if the given charging pool is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        public Boolean ContainsChargingPool(ChargingPool ChargingPool)
        {

            if (ChargingPool.Operator is not null &&
                TryGetChargingStationOperatorById(ChargingPool.Operator.Id, out var chargingStationOperator))
            {
                return chargingStationOperator.ChargingPoolExists(ChargingPool.Id);
            }

            return false;

        }

        #endregion

        #region ContainsChargingPool(ChargingPoolId)

        /// <summary>
        /// Check if the given charging pool identification is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingPoolId">A charging pool identification.</param>
        public Boolean ContainsChargingPool(ChargingPool_Id ChargingPoolId)
        {

            if (TryGetChargingStationOperatorById(ChargingPoolId.OperatorId, out var chargingStationOperator))
            {
                return chargingStationOperator.ChargingPoolExists(ChargingPoolId);
            }

            return false;

        }

        #endregion

        #region GetChargingPoolbyId(ChargingPoolId)

        public ChargingPool? GetChargingPoolById(ChargingPool_Id ChargingPoolId)
        {

            if (TryGetChargingStationOperatorById(ChargingPoolId.OperatorId,   out var chargingStationOperator) &&
                chargingStationOperator is not null                                                             &&
                chargingStationOperator.TryGetChargingPoolById(ChargingPoolId, out var chargingPool))
            {
                return chargingPool;
            }

            return null;

        }

        public ChargingPool? GetChargingPoolById(ChargingPool_Id? ChargingPoolId)
        {

            if (ChargingPoolId.HasValue &&
                TryGetChargingStationOperatorById(ChargingPoolId.Value.OperatorId,   out var chargingStationOperator) &&
                chargingStationOperator is not null                                                                   &&
                chargingStationOperator.TryGetChargingPoolById(ChargingPoolId.Value, out var chargingPool))
            {
                return chargingPool;
            }

            return null;

        }

        #endregion

        #region TryGetChargingPoolbyId(ChargingPoolId, out ChargingPool)

        public Boolean TryGetChargingPoolById(ChargingPool_Id                         ChargingPoolId,
                                              [NotNullWhen(true)] out ChargingPool?  ChargingPool)
        {

            if (TryGetChargingStationOperatorById(ChargingPoolId.OperatorId, out var chargingStationOperator))
            {
                return chargingStationOperator.TryGetChargingPoolById(ChargingPoolId, out ChargingPool);
            }

            ChargingPool = null;
            return false;

        }

        public Boolean TryGetChargingPoolById(ChargingPool_Id?                        ChargingPoolId,
                                              [NotNullWhen(true)] out ChargingPool?  ChargingPool)
        {

            if (ChargingPoolId.HasValue &&
                TryGetChargingStationOperatorById(ChargingPoolId.Value.OperatorId, out var chargingStationOperator))
            {
                return chargingStationOperator.TryGetChargingPoolById(ChargingPoolId.Value, out ChargingPool);
            }

            ChargingPool = null;
            return false;

        }

        #endregion


        #region SetChargingPoolAdminStatus(ChargingPoolId, StatusList)

        public void SetChargingPoolAdminStatus(ChargingPool_Id                                         ChargingPoolId,
                                               IEnumerable<Timestamped<ChargingPoolAdminStatusType>>  StatusList)
        {

            if (TryGetChargingStationOperatorById(ChargingPoolId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {
                chargingStationOperator.SetChargingPoolAdminStatus(ChargingPoolId, StatusList);
            }

        }

        #endregion


        #region SendChargingPoolAdminStatusDiff(StatusDiff)

        internal void SendChargingPoolAdminStatusDiff(ChargingPoolAdminStatusDiff StatusDiff)
        {
            OnChargingPoolAdminDiff?.Invoke(StatusDiff);
        }

        #endregion


        #region OnChargingPoolAdminDiff

        public delegate void OnChargingPoolAdminDiffDelegate(ChargingPoolAdminStatusDiff StatusDiff);

        /// <summary>
        /// An event fired whenever a charging station admin status diff was received.
        /// </summary>
        public event OnChargingPoolAdminDiffDelegate? OnChargingPoolAdminDiff;

        #endregion


        #region ChargingPoolAdminStatus        (IncludeChargingPools = null)

        /// <summary>
        /// Return the admin status of all charging pools registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        public IEnumerable<ChargingPoolAdminStatus> ChargingPoolAdminStatus(IncludeChargingPoolDelegate? IncludeChargingPools = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingPoolAdminStatus(IncludeChargingPools));

        #endregion

        #region ChargingPoolAdminStatusSchedule(IncludeChargingPools = null, TimestampFilter  = null, StatusFilter = null, Skip = null, Take = null)

        /// <summary>
        /// Return the admin status of all charging pools registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="AdminStatusFilter">An optional admin status value filter.</param>
        /// <param name="HistorySize">The size of the history.</param>
        public IEnumerable<Tuple<ChargingPool_Id, IEnumerable<Timestamped<ChargingPoolAdminStatusType>>>>

            ChargingPoolAdminStatusSchedule(IncludeChargingPoolDelegate?                 IncludeChargingPools   = null,
                                            Func<DateTimeOffset,              Boolean>?  TimestampFilter        = null,
                                            Func<ChargingPoolAdminStatusType, Boolean>?  AdminStatusFilter      = null,
                                            UInt64?                                      Skip                   = null,
                                            UInt64?                                      Take                   = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingPoolAdminStatusSchedule(IncludeChargingPools,
                                                                         TimestampFilter,
                                                                         AdminStatusFilter,
                                                                         Skip,
                                                                         Take));

        #endregion


        #region ChargingPoolStatus             (IncludeChargingPools = null)

        /// <summary>
        /// Return the status of all charging pools registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        public IEnumerable<ChargingPoolStatus> ChargingPoolStatus(IncludeChargingPoolDelegate? IncludeChargingPools = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingPoolStatus(IncludeChargingPools));

        #endregion

        #region ChargingPoolStatusSchedule     (IncludeChargingPools = null, TimestampFilter  = null, StatusFilter = null, Skip = null, Take = null)

        /// <summary>
        /// Return the admin status of all charging pools registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingPools">An optional delegate for filtering charging pools.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="StatusFilter">An optional status value filter.</param>
        /// <param name="HistorySize">The size of the history.</param>
        public IEnumerable<Tuple<ChargingPool_Id, IEnumerable<Timestamped<ChargingPoolStatusType>>>>

            ChargingPoolStatusSchedule(IncludeChargingPoolDelegate?            IncludeChargingPools   = null,
                                       Func<DateTimeOffset,         Boolean>?  TimestampFilter        = null,
                                       Func<ChargingPoolStatusType, Boolean>?  StatusFilter           = null,
                                       UInt64?                                 Skip                   = null,
                                       UInt64?                                 Take                   = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingPoolStatusSchedule(IncludeChargingPools,
                                                                    TimestampFilter,
                                                                    StatusFilter,
                                                                    Skip,
                                                                    Take));

        #endregion

        #endregion

        #region ChargingStations...

        #region ChargingStations

        /// <summary>
        /// Return all charging stations registered within this roaming network.
        /// </summary>
        public IEnumerable<ChargingStation> ChargingStations

            => ImmutablePOIValues.CopyItems(chargingStationOperators.Values.SelectMany(cso => cso.ChargingStations));

        #endregion

        #region ChargingStationIds(IncludeStations = null)

        /// <summary>
        /// Return all charging station identifications registered within this roaming network.
        /// </summary>
        /// <param name="IncludeStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStation_Id> ChargingStationIds(IncludeChargingStationDelegate? IncludeStations = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingStationIds(IncludeStations));

        #endregion


        #region ContainsChargingStation       (ChargingStation)

        /// <summary>
        /// Check if the given charging station is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        public Boolean ContainsChargingStation(ChargingStation ChargingStation)
        {

            if (ChargingStation.Operator is not null                                                            &&
                TryGetChargingStationOperatorById(ChargingStation.Operator.Id, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {
                return chargingStationOperator.ContainsChargingStation(ChargingStation.Id);
            }

            return false;

        }

        #endregion

        #region ContainsChargingStation       (ChargingStationId)

        /// <summary>
        /// Check if the given charging station identification is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingStationId">A charging station identification.</param>
        public Boolean ContainsChargingStation(ChargingStation_Id ChargingStationId)
        {

            if (TryGetChargingStationOperatorById(ChargingStationId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {
                return chargingStationOperator.ContainsChargingStation(ChargingStationId);
            }

            return false;

        }

        #endregion

        #region GetChargingStationbyId        (ChargingStationId)

        public ChargingStation? GetChargingStationById(ChargingStation_Id ChargingStationId)
        {

            if (TryGetChargingStationOperatorById(ChargingStationId.OperatorId,      out var chargingStationOperator) &&
                chargingStationOperator is not null                                                                   &&
                chargingStationOperator.TryGetChargingStationById(ChargingStationId, out var chargingStation))
            {
                return chargingStation;
            }

            return null;

        }

        public ChargingStation? GetChargingStationById(ChargingStation_Id? ChargingStationId)
        {

            if (ChargingStationId.HasValue &&
                TryGetChargingStationOperatorById(ChargingStationId.Value.OperatorId,      out var chargingStationOperator) &&
                chargingStationOperator is not null                                                                         &&
                chargingStationOperator.TryGetChargingStationById(ChargingStationId.Value, out var chargingStation))
            {
                return chargingStation;
            }

            return null;

        }

        #endregion

        #region TryGetChargingStationbyId     (ChargingStationId, out ChargingStation)

        public Boolean TryGetChargingStationById(ChargingStation_Id                         ChargingStationId,
                                                 [NotNullWhen(true)] out ChargingStation?  ChargingStation)
        {

            if (TryGetChargingStationOperatorById(ChargingStationId.OperatorId, out var chargingStationOperator))
            {
                return chargingStationOperator.TryGetChargingStationById(ChargingStationId, out ChargingStation);
            }

            ChargingStation = null;
            return false;

        }

        public Boolean TryGetChargingStationById(ChargingStation_Id?                        ChargingStationId,
                                                 [NotNullWhen(true)] out ChargingStation?  ChargingStation)
        {

            if (ChargingStationId.HasValue &&
                TryGetChargingStationOperatorById(ChargingStationId.Value.OperatorId, out var chargingStationOperator))
            {
                return chargingStationOperator.TryGetChargingStationById(ChargingStationId.Value, out ChargingStation);
            }

            ChargingStation = null;
            return false;

        }

        #endregion


        #region SetChargingStationAdminStatus (ChargingStationId, CurrentAdminStatus)

        public async Task SetChargingStationAdminStatus(ChargingStation_Id                            ChargingStationId,
                                                        Timestamped<ChargingStationAdminStatusType>  CurrentAdminStatus)
        {

            if (TryGetChargingStationOperatorById(ChargingStationId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {

                await chargingStationOperator.SetChargingStationAdminStatus(
                          ChargingStationId,
                          CurrentAdminStatus
                      );

            }

        }

        #endregion

        #region SetChargingStationAdminStatus (ChargingStationId, CurrentAdminStatusList)

        public async Task SetChargingStationAdminStatus(ChargingStation_Id                                         ChargingStationId,
                                                        IEnumerable<Timestamped<ChargingStationAdminStatusType>>  CurrentAdminStatusList)
        {

            if (TryGetChargingStationOperatorById(ChargingStationId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {

                await chargingStationOperator.SetChargingStationAdminStatus(
                          ChargingStationId,
                          CurrentAdminStatusList
                      );

            }

        }

        #endregion

        #region SetChargingStationStatus      (ChargingStationId, CurrentStatus)

        public async Task SetChargingStationStatus(ChargingStation_Id                       ChargingStationId,
                                                   Timestamped<ChargingStationStatusType>  CurrentStatus)
        {

            if (TryGetChargingStationOperatorById(ChargingStationId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {

                await chargingStationOperator.SetChargingStationStatus(
                          ChargingStationId,
                          CurrentStatus
                      );

            }

        }

        #endregion

        #region SetChargingStationStatus      (ChargingStationId, CurrentStatusList)

        public async Task SetChargingStationStatus(ChargingStation_Id                                    ChargingStationId,
                                                   IEnumerable<Timestamped<ChargingStationStatusType>>  CurrentStatusList)
        {

            if (TryGetChargingStationOperatorById(ChargingStationId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {

                await chargingStationOperator.SetChargingStationStatus(
                          ChargingStationId,
                          CurrentStatusList
                      );

            }

        }

        #endregion


        #region ChargingStationAdminStatus        (IncludeStations = null)

        /// <summary>
        /// Return the admin status of all charging stations registered within this roaming network.
        /// </summary>
        /// <param name="IncludeStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStationAdminStatus> ChargingStationAdminStatus(IncludeChargingStationDelegate? IncludeStations = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingStationAdminStatus(IncludeStations));

        #endregion

        #region ChargingStationAdminStatusSchedule(IncludeChargingStations = null, TimestampFilter  = null, StatusFilter = null, Skip = null, Take = null)

        /// <summary>
        /// Return the admin status of all charging stations registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingStations">An optional delegate for filtering charging stations.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="AdminStatusFilter">An optional admin status value filter.</param>
        /// <param name="HistorySize">The size of the history.</param>
        public IEnumerable<Tuple<ChargingStation_Id, IEnumerable<Timestamped<ChargingStationAdminStatusType>>>>

            ChargingStationAdminStatusSchedule(IncludeChargingStationDelegate?                  IncludeChargingStations   = null,
                                               Func<DateTimeOffset,                  Boolean>?  TimestampFilter           = null,
                                               Func<ChargingStationAdminStatusType, Boolean>?  AdminStatusFilter         = null,
                                               UInt64?                                          Skip                      = null,
                                               UInt64?                                          Take                      = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingStationAdminStatusSchedule(IncludeChargingStations,
                                                                            TimestampFilter,
                                                                            AdminStatusFilter,
                                                                            Skip,
                                                                            Take));

        #endregion


        #region ChargingStationStatus             (IncludeStations = null)

        /// <summary>
        /// Return the status of all charging stations registered within this roaming network.
        /// </summary>
        /// <param name="IncludeStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStationStatus> ChargingStationStatus(IncludeChargingStationDelegate? IncludeStations = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingStationStatus(IncludeStations));

        #endregion

        #region ChargingStationStatusSchedule     (IncludeChargingStations = null, TimestampFilter  = null, StatusFilter = null, Skip = null, Take = null)

        /// <summary>
        /// Return the admin status of all charging stations registered within this roaming network.
        /// </summary>
        /// <param name="IncludeChargingStations">An optional delegate for filtering charging stations.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="StatusFilter">An optional status value filter.</param>
        /// <param name="HistorySize">The size of the history.</param>
        public IEnumerable<Tuple<ChargingStation_Id, IEnumerable<Timestamped<ChargingStationStatusType>>>>

            ChargingStationStatusSchedule(IncludeChargingStationDelegate?             IncludeChargingStations   = null,
                                          Func<DateTimeOffset,             Boolean>?  TimestampFilter           = null,
                                          Func<ChargingStationStatusType, Boolean>?  StatusFilter              = null,
                                          UInt64?                                     Skip                      = null,
                                          UInt64?                                     Take                      = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.ChargingStationStatusSchedule(IncludeChargingStations,
                                                                       TimestampFilter,
                                                                       StatusFilter,
                                                                       Skip,
                                                                       Take));

        #endregion

        #endregion

        #region EVSEs...

        #region EVSEs

        /// <summary>
        /// Return all EVSEs registered within this roaming network.
        /// </summary>
        public IEnumerable<EVSE> EVSEs

            => ImmutablePOIValues.CopyItems(chargingStationOperators.Values.SelectMany(cso => cso.EVSEs));

        #endregion

        #region EVSEIds(IncludeEVSEs = null)

        /// <summary>
        /// Return all EVSE identifications registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSE_Id> EVSEIds(IncludeEVSEDelegate? IncludeEVSEs = null)

            => chargingStationOperators.Values.
                   SelectMany(cso => cso.EVSEIds(IncludeEVSEs));

        #endregion


        #region ContainsEVSE(EVSE)

        /// <summary>
        /// Check if the given EVSE is already present within the roaming network.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        public Boolean ContainsEVSE(EVSE EVSE)
        {

            if (EVSE.Operator is not null                                                            &&
                TryGetChargingStationOperatorById(EVSE.Operator.Id, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {
                return chargingStationOperator.ContainsEVSE(EVSE.Id);
            }

            return false;

        }

        #endregion

        #region ContainsEVSE(EVSEId)

        /// <summary>
        /// Check if the given EVSE identification is already present within the roaming network.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        public Boolean ContainsEVSE(EVSE_Id EVSEId)
        {

            if (TryGetChargingStationOperatorById(EVSEId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null)
            {
                return chargingStationOperator.ContainsEVSE(EVSEId);
            }

            return false;

        }

        #endregion

        #region GetEVSEById(EVSEId)

        public EVSE? GetEVSEById(EVSE_Id EVSEId)
        {

            if (TryGetChargingStationOperatorById(EVSEId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null                                                   &&
                chargingStationOperator.TryGetEVSEById(EVSEId, out var evse))
            {
                return evse;
            }

            return null;

        }

        public EVSE? GetEVSEById(EVSE_Id? EVSEId)
        {

            if (EVSEId.HasValue &&
                TryGetChargingStationOperatorById(EVSEId.Value.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator is not null                                                         &&
                chargingStationOperator.TryGetEVSEById(EVSEId.Value, out var evse))
            {
                return evse;
            }

            return null;

        }

        #endregion

        #region TryGetEVSEById(EVSEId, out EVSE)

        public Boolean TryGetEVSEById(EVSE_Id EVSEId, [NotNullWhen(true)] out EVSE? EVSE)
        {

            if (TryGetChargingStationOperatorById(EVSEId.OperatorId, out var chargingStationOperator))
            {
                return chargingStationOperator.TryGetEVSEById(EVSEId, out EVSE);
            }

            EVSE = null;
            return false;

        }

        public Boolean TryGetEVSEById(EVSE_Id? EVSEId, [NotNullWhen(true)] out EVSE? EVSE)
        {

            if (EVSEId.HasValue &&
                TryGetChargingStationOperatorById(EVSEId.Value.OperatorId, out var chargingStationOperator))
            {
                return chargingStationOperator.TryGetEVSEById(EVSEId.Value, out EVSE);
            }

            EVSE = null;
            return false;

        }

        #endregion


        #region EVSEAdminStatus        (IncludeEVSEs = null)

        /// <summary>
        /// Return the admin status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSEAdminStatus> EVSEAdminStatus(IncludeEVSEDelegate? IncludeEVSEs = null)

            => chargingStationOperators.Values.
                   SelectMany(chargingStationOperator => chargingStationOperator.EVSEAdminStatus(IncludeEVSEs));

        #endregion

        #region EVSEAdminStatusSchedule(IncludeEVSEs = null, TimestampFilter  = null, StatusFilter = null, Skip = null, Take = null)

        /// <summary>
        /// Return the admin status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="StatusFilter">An optional status value filter.</param>
        /// <param name="HistorySize">The size of the history.</param>
        public IEnumerable<Tuple<EVSE_Id, IEnumerable<Timestamped<EVSEAdminStatusType>>>>

            EVSEAdminStatusSchedule(IncludeEVSEDelegate?                 IncludeEVSEs      = null,
                                    Func<DateTimeOffset,      Boolean>?  TimestampFilter   = null,
                                    Func<EVSEAdminStatusType, Boolean>?  StatusFilter      = null,
                                    UInt64?                              Skip              = null,
                                    UInt64?                              Take              = null)

                => chargingStationOperators.Values.
                       SelectMany(chargingStationOperator => chargingStationOperator.EVSEAdminStatusSchedule(
                                                                 IncludeEVSEs,
                                                                 TimestampFilter,
                                                                 StatusFilter,
                                                                 Skip,
                                                                 Take
                                                             ));

        #endregion


        #region EVSEStatus             (IncludeEVSEs = null)

        /// <summary>
        /// Return the status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSEStatus> EVSEStatus(IncludeEVSEDelegate? IncludeEVSEs = null)

            => chargingStationOperators.Values.
                   SelectMany(chargingStationOperator => chargingStationOperator.EVSEStatus(IncludeEVSEs));

        #endregion

        #region EVSEStatusSchedule     (IncludeEVSEs = null, TimestampFilter  = null, StatusFilter = null, Skip = null, Take = null)

        /// <summary>
        /// Return the status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        /// <param name="TimestampFilter">An optional status timestamp filter.</param>
        /// <param name="StatusFilter">An optional status value filter.</param>
        /// <param name="HistorySize">The size of the history.</param>
        public IEnumerable<Tuple<EVSE_Id, IEnumerable<Timestamped<EVSEStatusType>>>>

            EVSEStatusSchedule(IncludeEVSEDelegate?            IncludeEVSEs      = null,
                               Func<DateTimeOffset, Boolean>?  TimestampFilter   = null,
                               Func<EVSEStatusType, Boolean>?  StatusFilter      = null,
                               UInt64?                         Skip              = null,
                               UInt64?                         Take              = null)

                => chargingStationOperators.Values.
                       SelectMany(chargingStationOperator => chargingStationOperator.EVSEStatusSchedule(
                                                                 IncludeEVSEs,
                                                                 TimestampFilter,
                                                                 StatusFilter,
                                                                 Skip,
                                                                 Take
                                                             ));

        #endregion

        #endregion

        #region EnergyMeters...

        private readonly ConcurrentDictionary<EnergyMeter_Id, EnergyMeter> energyMeters = [];

        #region EnergyMeters

        /// <summary>
        /// Return all energy meters registered within this roaming network.
        /// </summary>
        public IEnumerable<EnergyMeter> EnergyMeters

            => ImmutablePOIValues.CopyItems(energyMeters.Values);

        #endregion

        #region EnergyMeterIds(IncludeEnergyMeters = null)

        /// <summary>
        /// Return all EnergyMeter identifications registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEnergyMeters">An optional delegate for filtering EnergyMeters.</param>
        public IEnumerable<EnergyMeter_Id> EnergyMeterIds(IncludeEnergyMeterDelegate? IncludeEnergyMeters = null)

            => IncludeEnergyMeters is null
                   ? energyMeters.Keys
                   : energyMeters.Where (energyMeterKVP => IncludeEnergyMeters(energyMeterKVP.Value)).
                                  Select(energyMeterKVP => energyMeterKVP.Key);

        #endregion


        #region ContainsEnergyMeter (EnergyMeter)

        /// <summary>
        /// Check if the given EnergyMeter is already present within the roaming network.
        /// </summary>
        /// <param name="EnergyMeter">An EnergyMeter.</param>
        public Boolean ContainsEnergyMeter(EnergyMeter EnergyMeter)

            => energyMeters.ContainsKey(EnergyMeter.Id);

        #endregion

        #region ContainsEnergyMeter (EnergyMeterId)

        /// <summary>
        /// Check if the given EnergyMeter identification is already present within the roaming network.
        /// </summary>
        /// <param name="EnergyMeterId">An EnergyMeter identification.</param>
        public Boolean ContainsEnergyMeter(EnergyMeter_Id EnergyMeterId)

            => energyMeters.ContainsKey(EnergyMeterId);

        #endregion

        #region GetEnergyMeterById  (EnergyMeterId)

        public EnergyMeter? GetEnergyMeterById(EnergyMeter_Id EnergyMeterId)
        {

            if (energyMeters.TryGetValue(EnergyMeterId, out var energyMeter))
                return energyMeter;

            return null;

        }

        public EnergyMeter? GetEnergyMeterById(EnergyMeter_Id? EnergyMeterId)
        {

            if (EnergyMeterId.HasValue &&
                energyMeters.TryGetValue(EnergyMeterId.Value, out var energyMeter))
            {
                return energyMeter;
            }

            return null;

        }

        #endregion

        #region TryGetEnergyMeterById (EnergyMeterId, out EnergyMeter)

        public Boolean TryGetEnergyMeterById(EnergyMeter_Id EnergyMeterId, [NotNullWhen(true)] out EnergyMeter? EnergyMeter)

            => energyMeters.TryGetValue(EnergyMeterId, out EnergyMeter);

        public Boolean TryGetEnergyMeterById(EnergyMeter_Id? EnergyMeterId, [NotNullWhen(true)] out EnergyMeter? EnergyMeter)
        {

            if (EnergyMeterId.HasValue &&
                energyMeters.TryGetValue(EnergyMeterId.Value, out EnergyMeter))
            {
                return true;
            }

            EnergyMeter = null;
            return false;

        }

        #endregion

        #endregion


        #region Grid Operators...

        #region GridOperators

        private readonly ConcurrentDictionary<GridOperator_Id, GridOperator> projectedGridOperators = [];

        private ConcurrentDictionary<GridOperator_Id, GridOperator> gridOperators
        {
            get { EnsureSnapshotProjection(); return projectedGridOperators; }
        }

        /// <summary>
        /// Return the standalone grid operators; the collection currently has no registration path.
        /// </summary>
        public IEnumerable<GridOperator> GridOperators
            => ImmutablePOIValues.CopyItems(gridOperators.Values);

        #endregion


        #region GridOperatorsAdminStatus

        /// <summary>
        /// Return the admin status of all smart cities registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<GridOperator_Id, IEnumerable<Timestamped<GridOperatorAdminStatusTypes>>>> GridOperatorsAdminStatus

            => ImmutablePOIValues.CopyItems(gridOperators.Values.
                   Select(emp => new KeyValuePair<GridOperator_Id, IEnumerable<Timestamped<GridOperatorAdminStatusTypes>>>(emp.Id, emp.AdminStatusSchedule())));

        #endregion

        #region GridOperatorsStatus

        /// <summary>
        /// Return the status of all smart cities registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<GridOperator_Id, IEnumerable<Timestamped<GridOperatorStatusTypes>>>> GridOperatorsStatus

            => ImmutablePOIValues.CopyItems(gridOperators.Values.
                   Select(emp => new KeyValuePair<GridOperator_Id, IEnumerable<Timestamped<GridOperatorStatusTypes>>>(emp.Id, emp.StatusSchedule())));

        #endregion


        #region ContainsGridOperator(GridOperator)

        /// <summary>
        /// Check if the given GridOperator is already present within the roaming network.
        /// </summary>
        /// <param name="GridOperator">A grid operator.</param>
        public Boolean ContainsGridOperator(GridOperator GridOperator)

            => gridOperators.ContainsKey(GridOperator.Id);

        #endregion

        #region ContainsGridOperator(GridOperatorId)

        /// <summary>
        /// Check if the given GridOperator identification is already present within the roaming network.
        /// </summary>
        /// <param name="GridOperatorId">The unique identification of the grid operator.</param>
        public Boolean ContainsGridOperator(GridOperator_Id GridOperatorId)

            => gridOperators.ContainsKey(GridOperatorId);

        #endregion

        #region GetGridOperator(GridOperatorId)

        public GridOperator? GetGridOperator(GridOperator_Id GridOperatorId)

            => gridOperators.GetValueOrDefault(GridOperatorId);

        #endregion

        #region TryGetGridOperatorById(GridOperatorId, out GridOperator)

        public Boolean TryGetGridOperatorById(GridOperator_Id                        GridOperatorId,
                                              [NotNullWhen(true)] out GridOperator?  GridOperator)

            => gridOperators.TryGetValue(GridOperatorId, out GridOperator);

        #endregion


        #endregion


        #region Parking Operators...

        #region ParkingOperators

        private readonly ConcurrentDictionary<ParkingOperator_Id, ParkingOperator> projectedParkingOperators = [];

        private ConcurrentDictionary<ParkingOperator_Id, ParkingOperator> parkingOperators
        {
            get { EnsureSnapshotProjection(); return projectedParkingOperators; }
        }

        /// <summary>
        /// Return all parking operators registered within this roaming network.
        /// </summary>
        public IEnumerable<ParkingOperator> ParkingOperators

            => ImmutablePOIValues.CopyItems(parkingOperators.Values);

        #endregion


        #region ParkingOperatorAdminStatus

        /// <summary>
        /// Return the admin status of all parking operators registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorAdminStatusTypes>>>> ParkingOperatorAdminStatus

            => ImmutablePOIValues.CopyItems(parkingOperators.Values.
                   Select(pop => new KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorAdminStatusTypes>>>(pop.Id,
                                                                                                                                 pop.AdminStatusSchedule())));

        #endregion

        #region ParkingOperatorStatus

        /// <summary>
        /// Return the status of all parking operators registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorStatusTypes>>>> ParkingOperatorStatus

            => ImmutablePOIValues.CopyItems(parkingOperators.Values.
                   Select(pop => new KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorStatusTypes>>>(pop.Id,
                                                                                                                            pop.StatusSchedule())));

        #endregion


        #region ContainsParkingOperator(ParkingOperator)

        /// <summary>
        /// Check if the given ParkingOperator is already present within the roaming network.
        /// </summary>
        /// <param name="ParkingOperator">An parking Operator.</param>
        public Boolean ContainsParkingOperator(ParkingOperator ParkingOperator)

            => parkingOperators.ContainsKey(ParkingOperator.Id);

        #endregion

        #region ContainsParkingOperator(ParkingOperatorId)

        /// <summary>
        /// Check if the given ParkingOperator identification is already present within the roaming network.
        /// </summary>
        /// <param name="ParkingOperatorId">The unique identification of the parking Operator.</param>
        public Boolean ContainsParkingOperator(ParkingOperator_Id ParkingOperatorId)

            => parkingOperators.ContainsKey(ParkingOperatorId);

        #endregion

        #region GetParkingOperatorById(ParkingOperatorId)

        public ParkingOperator? GetParkingOperatorById(ParkingOperator_Id ParkingOperatorId)

            => parkingOperators.GetValueOrDefault(ParkingOperatorId);

        #endregion

        #region TryGetParkingOperatorById(ParkingOperatorId, out ParkingOperator)

        public Boolean TryGetParkingOperatorById(ParkingOperator_Id                        ParkingOperatorId,
                                                 [NotNullWhen(true)] out ParkingOperator?  ParkingOperator)

            => parkingOperators.TryGetValue(ParkingOperatorId, out ParkingOperator);

        #endregion


        #endregion


        #region ToJSON(this RoamingNetwork, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation of the given roaming network.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        public JObject ToJSON(Boolean                                                     Embedded                                  = false,

                              InfoStatus                                                  ExpandRoamingNetworkIds                   = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandChargingStationOperatorIds          = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandDataLicenses                        = InfoStatus.ShowIdOnly,
                              InfoStatus                                                  ExpandEMobilityProviderId                 = InfoStatus.ShowIdOnly,

                              CustomJObjectSerializerDelegate<RoamingNetwork>?           CustomRoamingNetworkSerializer            = null,
                              CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null,
                              CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                              CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                              CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null)
        {

            var JSON = JSONObject.Create(

                               new JProperty("@id",             Id.         ToString()),

                         !Embedded
                             ? new JProperty("@context",        JSONLDContext)
                             : null,

                               new JProperty("name",            Name.       ToJSON()),

                         Description.IsNotNullOrEmpty()
                             ? new JProperty("description",     Description.ToJSON())
                             : null,

                         DataSource is not null && DataSource.IsNeitherNullNorEmpty()
                             ? new JProperty("dataSource",      DataSource)
                             : null,

                         DataLicenses.Any()
                             ? ExpandDataLicenses.Switch(
                                 () => new JProperty("dataLicenseIds",  new JArray(DataLicenses.SafeSelect(license => license.Id.ToString()))),
                                 () => new JProperty("dataLicenses",    DataLicenses.ToJSON()))
                             : null,

                         ChargingStationOperators.Any()
                             ? ExpandChargingStationOperatorIds.Switch(

                                   () => new JProperty("chargingStationOperatorIds",  new JArray(ChargingStationOperators.Select(op => op.Id).
                                                                                                                OrderBy(id => id).
                                                                                                                Select (id => id.ToString()))),

                                   () => new JProperty("chargingStationOperators",    new JArray(ChargingStationOperators.
                                                                                                                OrderBy(cso => cso).
                                                                                                                ToJSON (Embedded: true,
                                                                                                                        ExpandRoamingNetworkId:                   InfoStatus.Hidden,
                                                                                                                        ExpandChargingPoolIds:                    ExpandChargingPoolIds,
                                                                                                                        ExpandChargingStationIds:                 ExpandChargingStationIds,
                                                                                                                        ExpandEVSEIds:                            ExpandEVSEIds,
                                                                                                                        ExpandBrandIds:                           ExpandBrandIds,
                                                                                                                        ExpandDataLicenses:                       ExpandDataLicenses,
                                                                                                                        CustomChargingStationOperatorSerializer:  CustomChargingStationOperatorSerializer,
                                                                                                                        CustomChargingPoolSerializer:             CustomChargingPoolSerializer,
                                                                                                                        CustomChargingStationSerializer:          CustomChargingStationSerializer,
                                                                                                                        CustomEVSESerializer:                     CustomEVSESerializer))))
                             : null,

                         !ChargingStationOperators.Any() || ExpandChargingStationOperatorIds == InfoStatus.Expanded
                             ? null
                             : ExpandChargingPoolIds.Switch(
                                   () => new JProperty("chargingPoolIds",             new JArray(ChargingPools.Select(pool => pool.Id).
                                                                                                                OrderBy(id => id).
                                                                                                                Select (id => id.ToString()))),

                                   () => new JProperty("chargingPools",               new JArray(ChargingPools.
                                                                                                                OrderBy(pool => pool).
                                                                                                                ToJSON (Embedded: true,
                                                                                                                        ExpandRoamingNetworkId:           InfoStatus.Hidden,
                                                                                                                        ExpandChargingStationOperatorId:  InfoStatus.Hidden,
                                                                                                                        ExpandChargingStationIds:         InfoStatus.Hidden,
                                                                                                                        ExpandEVSEIds:                    InfoStatus.Hidden,
                                                                                                                        ExpandBrandIds:                   InfoStatus.Hidden,
                                                                                                                        ExpandDataLicenses:               InfoStatus.Hidden,
                                                                                                                        CustomChargingPoolSerializer:     CustomChargingPoolSerializer,
                                                                                                                        CustomChargingStationSerializer:  CustomChargingStationSerializer,
                                                                                                                        CustomEVSESerializer:             CustomEVSESerializer)))),

                         !ChargingStationOperators.Any() || (ExpandChargingPoolIds == InfoStatus.Expanded || ExpandChargingStationOperatorIds == InfoStatus.Expanded)
                             ? null
                             : ExpandChargingStationIds.Switch(
                                   () => new JProperty("chargingStationIds",          new JArray(ChargingStationIds().
                                                                                                                OrderBy(id => id).
                                                                                                                Select (id => id.ToString()))),

                                   () => new JProperty("chargingStations",            new JArray(ChargingStations.
                                                                                                                OrderBy(station => station).
                                                                                                                ToJSON (Embedded: true,
                                                                                                                        ExpandRoamingNetworkId:           InfoStatus.Hidden,
                                                                                                                        ExpandChargingStationOperatorId:  InfoStatus.Hidden,
                                                                                                                        ExpandChargingPoolId:             InfoStatus.Hidden,
                                                                                                                        ExpandEVSEIds:                    InfoStatus.Hidden,
                                                                                                                        ExpandBrandIds:                   InfoStatus.Hidden,
                                                                                                                        ExpandDataLicenses:               InfoStatus.Hidden,
                                                                                                                        CustomChargingStationSerializer:  CustomChargingStationSerializer,
                                                                                                                        CustomEVSESerializer:             CustomEVSESerializer)))),

                         !ChargingStationOperators.Any() || (ExpandChargingStationIds == InfoStatus.Expanded || ExpandChargingPoolIds == InfoStatus.Expanded || ExpandChargingStationOperatorIds == InfoStatus.Expanded)
                             ? null
                             : ExpandEVSEIds.Switch(
                                   () => new JProperty("EVSEIds",                     new JArray(EVSEIds().
                                                                                                                OrderBy(id => id).
                                                                                                                Select (id => id.ToString()))),

                                   () => new JProperty("EVSEs",                       new JArray(EVSEs.
                                                                                                                OrderBy(evse => evse).
                                                                                                                ToJSON (Embedded: true,
                                                                                                                        ExpandRoamingNetworkId:           InfoStatus.Hidden,
                                                                                                                        ExpandChargingStationOperatorId:  InfoStatus.Hidden,
                                                                                                                        ExpandChargingPoolId:             InfoStatus.Hidden,
                                                                                                                        ExpandChargingStationId:          InfoStatus.Hidden,
                                                                                                                        ExpandBrandIds:                   InfoStatus.Hidden,
                                                                                                                        ExpandDataLicenses:               InfoStatus.Hidden)))),


                         EMobilityProviders.Any()
                             ? ExpandEMobilityProviderId.Switch(
                                   () => new JProperty("eMobilityProviderIds",        new JArray(EMobilityProviders.Select(emp => emp.Id).
                                                                                                                OrderBy(id => id).
                                                                                                                Select (id => id.ToString()))),

                                   () => new JProperty("eMobilityProviders",          new JArray(EMobilityProviders.
                                                                                                                OrderBy(emp => emp).
                                                                                                                ToJSON (Embedded: true,
                                                                                                                        ExpandRoamingNetworkId:           InfoStatus.Hidden,
                                                                                                                        ExpandBrandIds:                   ExpandBrandIds,
                                                                                                                        ExpandDataLicenses:               ExpandDataLicenses))))
                             : null

                         );

            JSON["gridOperators"] = POIJSON.Children(GridOperators);
            JSON["parkingOperators"] = POIJSON.Children(ParkingOperators);
            JSON["chargingStationManufacturers"] = POIJSON.Children(ChargingStationManufacturers);

            return POIRepresentation.AddETags(this, CustomRoamingNetworkSerializer is not null
                       ? CustomRoamingNetworkSerializer(this, JSON)
                       : JSON);

        }

        #endregion


        #region (private) LogEvent(Logger, LogHandler, ...)

        private Task LogEvent<TDelegate>(TDelegate?                                         Logger,
                                         Func<TDelegate, Task>                              LogHandler,
                                         [CallerArgumentExpression(nameof(Logger))] String  EventName   = "",
                                         [CallerMemberName()]                       String  Command     = "")

            where TDelegate : Delegate

                => LogEvent(
                       nameof(RoamingNetwork),
                       Logger,
                       LogHandler,
                       EventName,
                       Command
                   );

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two roaming networks for equality.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetwork? RoamingNetwork1,
                                           RoamingNetwork? RoamingNetwork2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(RoamingNetwork1, RoamingNetwork2))
                return true;

            // If one is null, but not both, return false.
            if (RoamingNetwork1 is null || RoamingNetwork2 is null)
                return false;

            return RoamingNetwork1.Equals(RoamingNetwork2);

        }

        #endregion

        #region Operator != (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two roaming networks for inequality.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetwork? RoamingNetwork1,
                                           RoamingNetwork? RoamingNetwork2)

            => !(RoamingNetwork1 == RoamingNetwork2);

        #endregion

        #region Operator <  (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetwork? RoamingNetwork1,
                                          RoamingNetwork? RoamingNetwork2)
        {

            if (RoamingNetwork1 is null)
                throw new ArgumentNullException(nameof(RoamingNetwork1), "The given roaming network must not be null!");

            return RoamingNetwork1.CompareTo(RoamingNetwork2) < 0;

        }

        #endregion

        #region Operator <= (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetwork? RoamingNetwork1,
                                           RoamingNetwork? RoamingNetwork2)

            => !(RoamingNetwork1 > RoamingNetwork2);

        #endregion

        #region Operator >  (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetwork? RoamingNetwork1,
                                          RoamingNetwork? RoamingNetwork2)
        {

            if (RoamingNetwork1 is null)
                throw new ArgumentNullException(nameof(RoamingNetwork1), "The given roaming network must not be null!");

            return RoamingNetwork1.CompareTo(RoamingNetwork2) > 0;

        }

        #endregion

        #region Operator >= (RoamingNetwork1, RoamingNetwork2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork1">A roaming network.</param>
        /// <param name="RoamingNetwork2">Another roaming network.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetwork? RoamingNetwork1,
                                           RoamingNetwork? RoamingNetwork2)

            => !(RoamingNetwork1 < RoamingNetwork2);

        #endregion

        #endregion

        #region IComparable<RoamingNetwork> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two roaming networks.
        /// </summary>
        /// <param name="Object">A roaming network to compare with.</param>
        public override Int32 CompareTo(Object? Object)

            => Object is RoamingNetwork roamingNetwork
                   ? CompareTo(roamingNetwork)
                   : throw new ArgumentException("The given object is not a roaming network!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetwork)

        /// <summary>
        /// Compares two roaming networks.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network to compare with.</param>
        public Int32 CompareTo(RoamingNetwork? RoamingNetwork)
        {

            if (RoamingNetwork is null)
                throw new ArgumentNullException(nameof(RoamingNetwork),
                                                "The given roaming network must not be null!");

            return Id.CompareTo(RoamingNetwork.Id);

            //ToDo: Compare more properties!

        }

        #endregion

        #endregion

        #region IEquatable<RoamingNetwork> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two roaming networks for equality.
        /// </summary>
        /// <param name="Object">A roaming network to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetwork roamingNetwork &&
                   Equals(roamingNetwork);

        #endregion

        #region Equals(RoamingNetwork)

        /// <summary>
        /// Compares two roaming networks for equality.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network to compare with.</param>
        public Boolean Equals(RoamingNetwork? RoamingNetwork)

            => RoamingNetwork is not null &&
                   Id.Equals(RoamingNetwork.Id);

        //ToDo: Compare more properties!

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

            => $"'{Name.FirstText()}' ({Id})";

        #endregion


    }

}
