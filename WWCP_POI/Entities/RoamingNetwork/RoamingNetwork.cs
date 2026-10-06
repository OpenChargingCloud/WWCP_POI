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
    public partial class RoamingNetwork : AEMobilityEntity<RoamingNetwork_Id,
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
            => dataLicenses.Values;

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
            => eMobilityProviders.Values;

        #endregion


        #region AddEMobilityProvider                      (EMobilityProvider, ..., OnAdded = null, ...)

        /// <summary>
        /// Add the given charging station operator.
        /// </summary>
        /// <param name="EMobilityProvider">A charging station operator.</param>
        /// <param name="EventTrackingId">An optional unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddEMobilityProviderResult>

            AddEMobilityProvider(EMobilityProvider  EMobilityProvider,
                                 EventTracking_Id?  EventTrackingId   = null,
                                 User_Id?           CurrentUserId     = null)

        {

            var eventTrackingId = EventTrackingId ?? EventTracking_Id.New;

            if (eMobilityProviders.ContainsKey(EMobilityProvider.Id))
                return AddEMobilityProviderResult.ArgumentError(
                           EMobilityProvider,
                           $"The given charging station operator identification '{EMobilityProvider.Id}' already exists!".ToI18NString(),
                           eventTrackingId,
                           Id,
                           this,
                           this
                       );

            if (EMobilityProvider.Id.Length < MinEMobilityProviderIdLength)
                return AddEMobilityProviderResult.ArgumentError(
                           EMobilityProvider,
                           $"The given charging station operator identification '{EMobilityProvider.Id}' is too short!".ToI18NString(),
                           eventTrackingId,
                           Id,
                           this,
                           this
                       );

            if (EMobilityProvider.Name.IsNullOrEmpty())
                return AddEMobilityProviderResult.ArgumentError(
                           EMobilityProvider,
                           "The given charging station operator name must not be null!".ToI18NString(),
                           eventTrackingId,
                           Id,
                           this,
                           this
                       );

            if (EMobilityProvider.Name.FirstText().Length < MinEMobilityProviderNameLength)
                return AddEMobilityProviderResult.ArgumentError(
                           EMobilityProvider,
                           $"The given charging station operator name '{EMobilityProvider.Name}' is too short!".ToI18NString(),
                           eventTrackingId,
                           Id,
                           this,
                           this
                       );



            if (eMobilityProviders.TryAdd(EMobilityProvider.Id, EMobilityProvider))
                return AddEMobilityProviderResult.Success(
                           EMobilityProvider,
                           eventTrackingId,
                           Id,
                           this,
                           this
                       );

            return AddEMobilityProviderResult.Error(
                       EMobilityProvider,
                       I18NString.Empty,
                       eventTrackingId,
                       Id,
                       this,
                       this
                   );

        }

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

        #region RemoveEMobilityProvider    (EMobilityProviderId)

        public EMobilityProvider? RemoveEMobilityProvider(EMobilityProvider_Id EMobilityProviderId)
        {

            if (eMobilityProviders.TryRemove(EMobilityProviderId, out var eMobilityProvider))
                return eMobilityProvider;

            return null;

        }

        public EMobilityProvider? RemoveEMobilityProvider(EMobilityProvider_Id? EMobilityProviderId)
        {

            if (EMobilityProviderId.HasValue &&
                eMobilityProviders.TryRemove(EMobilityProviderId.Value, out var eMobilityProvider))
            {
                return eMobilityProvider;
            }

            return null;

        }

        #endregion

        #region TryRemoveEMobilityProvider (EMobilityProviderId, out EMobilityProvider)

        public Boolean TryRemoveEMobilityProvider(EMobilityProvider_Id                        EMobilityProviderId,
                                                  [NotNullWhen(true)] out EMobilityProvider?  EMobilityProvider)

            => eMobilityProviders.TryRemove(EMobilityProviderId, out EMobilityProvider);

        public Boolean TryRemoveEMobilityProvider(EMobilityProvider_Id?                       EMobilityProviderId,
                                                  [NotNullWhen(true)] out EMobilityProvider?  EMobilityProvider)

        {

            if (!EMobilityProviderId.HasValue)
            {
                EMobilityProvider = null;
                return false;
            }

            return eMobilityProviders.TryRemove(EMobilityProviderId.Value, out EMobilityProvider);

        }

        #endregion

        #endregion


        #region Charging Station Operators...

        #region ChargingStationOperators

        private readonly ConcurrentDictionary<ChargingStationOperator_Id, ChargingStationOperator> projectedChargingStationOperators = [];

        private ConcurrentDictionary<ChargingStationOperator_Id, ChargingStationOperator> chargingStationOperators
        {
            get { EnsureSnapshotProjection(); return projectedChargingStationOperators; }
        }

        /// <summary>
        /// Return all charging station operators registered within this roaming network.
        /// </summary>
        public IEnumerable<ChargingStationOperator> ChargingStationOperators
            => chargingStationOperators.Values;

        #endregion


        #region AddChargingStationOperator           (ChargingStationOperator,      ..., OnAdded = null, ...)

        /// <summary>
        /// Add the given charging station operator.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// <param name="EventTrackingId">An optional unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddChargingStationOperatorResult>

            AddChargingStationOperator(ChargingStationOperator  ChargingStationOperator,
                                       EventTracking_Id?        EventTrackingId                               = null,
                                       User_Id?                 CurrentUserId                                 = null)

        {

            var eventTrackingId = EventTrackingId ?? EventTracking_Id.New;

            if (chargingStationOperators.ContainsKey(ChargingStationOperator.Id))
                return AddChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given charging station operator identification '{ChargingStationOperator.Id}' already exists!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );;

            if (ChargingStationOperator.Id.Length < MinChargingStationOperatorIdLength)
                return AddChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given charging station operator identification '{ChargingStationOperator.Id}' is too short!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            if (ChargingStationOperator.Name.IsNullOrEmpty())
                return AddChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given charging station operator name must not be null or empty!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            if (ChargingStationOperator.Name.FirstText().Length < MinChargingStationOperatorNameLength)
                return AddChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given charging station operator name '{ChargingStationOperator.Name}' is too short!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            var now = Timestamp.Now;

            if (chargingStationOperators.TryAdd(ChargingStationOperator.Id, ChargingStationOperator))
            {

                //ChargingStationOperator.OnDataChanged                              += UpdateChargingStationOperatorData;
                //ChargingStationOperator.OnStatusChanged                            += UpdateChargingStationOperatorStatus;
                //ChargingStationOperator.OnAdminStatusChanged                       += UpdateChargingStationOperatorAdminStatus;

                //ChargingStationOperator.OnChargingPoolAddition.   OnVoting         += (eventTrackingId, timestamp, userId, cso, pool, vote)      => ChargingPoolAddition.   SendVoting      (eventTrackingId, timestamp, userId, cso, pool, vote);
                //ChargingStationOperator.OnChargingPoolAddition.   OnNotification   += SendChargingPoolAdded;
                //ChargingStationOperator.OnChargingPoolDataChanged                  += UpdateChargingPoolData;
                //ChargingStationOperator.OnChargingPoolAdminStatusChanged           += UpdateChargingPoolAdminStatus;
                //ChargingStationOperator.OnChargingPoolStatusChanged                += UpdateChargingPoolStatus;
                //ChargingStationOperator.OnChargingPoolRemoval.    OnVoting         += (eventTrackingId, timestamp, userId, cso, pool, vote)      => ChargingPoolRemoval.    SendVoting      (eventTrackingId, timestamp, userId, cso, pool, vote);
                //ChargingStationOperator.OnChargingPoolRemoval.    OnNotification   += (eventTrackingId, timestamp, userId, cso, pool)            => ChargingPoolRemoval.    SendNotification(eventTrackingId, timestamp, userId, cso, pool);

                //ChargingStationOperator.OnChargingStationAddition.OnVoting         += (eventTrackingId, timestamp, userId, pool, station, vote)  => ChargingStationAddition.SendVoting      (eventTrackingId, timestamp, userId, pool, station, vote);
                //ChargingStationOperator.OnChargingStationAddition.OnNotification   += SendChargingStationAdded;
                //ChargingStationOperator.OnChargingStationDataChanged               += UpdateChargingStationData;
                //ChargingStationOperator.OnChargingStationAdminStatusChanged        += UpdateChargingStationAdminStatus;
                //ChargingStationOperator.OnChargingStationStatusChanged             += UpdateChargingStationStatus;
                //ChargingStationOperator.OnChargingStationRemoval. OnVoting         += (eventTrackingId, timestamp, userId, pool, station, vote)  => ChargingStationRemoval. SendVoting      (eventTrackingId, timestamp, userId, pool, station, vote);
                //ChargingStationOperator.OnChargingStationRemoval. OnNotification   += (eventTrackingId, timestamp, userId, pool, station)        => ChargingStationRemoval. SendNotification(eventTrackingId, timestamp, userId, pool, station);

                //ChargingStationOperator.OnEVSEAddition.           OnVoting         += (eventTrackingId, timestamp, userId, station, evse, vote)  => EVSEAddition.           SendVoting      (eventTrackingId, timestamp, userId, station, evse, vote);
                //ChargingStationOperator.OnEVSEAddition.           OnNotification   += SendEVSEAdded;
                //ChargingStationOperator.OnEVSEDataChanged                          += UpdateEVSEData;
                //ChargingStationOperator.OnEVSEAdminStatusChanged                   += UpdateEVSEAdminStatus;
                //ChargingStationOperator.OnEVSEStatusChanged                        += UpdateEVSEStatus;
                //ChargingStationOperator.OnEVSERemoval.            OnVoting         += (eventTrackingId, timestamp, userId, station, evse, vote)  => EVSERemoval.            SendVoting      (eventTrackingId, timestamp, userId, station, evse, vote);
                //ChargingStationOperator.OnEVSERemoval.            OnNotification   += (eventTrackingId, timestamp, userId, station, evse)        => EVSERemoval.            SendNotification(eventTrackingId, timestamp, userId, station, evse);

            }

            return AddChargingStationOperatorResult.Success(
                       ChargingStationOperator:  ChargingStationOperator,
                       EventTrackingId:          eventTrackingId,
                       SenderId:                 Id,
                       Sender:                   this,
                       RoamingNetwork:           this
                   );

        }

        #endregion

        #region AddChargingStationOperatorIfNotExists(ChargingStationOperator,      ..., OnAdded = null, ...)

        /// <summary>
        /// Add the given user.
        /// </summary>
        /// <param name="User">A new user.</param>
        /// <param name="SkipDefaultNotifications">Do not apply the default notifications settings for new users.</param>
        /// <param name="OnAdded">A delegate run whenever the user has been added successfully.</param>
        /// <param name="EventTrackingId">An optional unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddChargingStationOperatorResult>

            AddChargingStationOperatorIfNotExists(ChargingStationOperator  ChargingStationOperator,
                                                  Boolean                  SkipNewUserNotifications   = false,
                                                  EventTracking_Id?        EventTrackingId            = null,
                                                  User_Id?                 CurrentUserId              = null)

        {

            var eventTrackingId = EventTrackingId ?? EventTracking_Id.New;

            if (chargingStationOperators.TryGetValue(ChargingStationOperator.Id, out var existingChargingStationOperator))
                return AddChargingStationOperatorResult.Exists(
                           ChargingStationOperator:  ChargingStationOperator,
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            if (ChargingStationOperator.Id.Length < MinChargingStationOperatorIdLength)
                return AddChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given charging station operator identification '{ChargingStationOperator.Id}' is too short!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            if (ChargingStationOperator.Name.IsNullOrEmpty())
                return AddChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given charging station operator name must not be null or empty!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            if (ChargingStationOperator.Name.FirstText().Length < MinChargingStationOperatorNameLength)
                return AddChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given user name '{ChargingStationOperator.Name}' is too short!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );


            if (chargingStationOperators.TryAdd(ChargingStationOperator.Id, ChargingStationOperator))
                return AddChargingStationOperatorResult.Success(
                           ChargingStationOperator:  ChargingStationOperator,
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            return AddChargingStationOperatorResult.Error(
                       ChargingStationOperator:  ChargingStationOperator,
                       EventTrackingId:          eventTrackingId,
                       Description:              I18NString.Empty,
                       SenderId:                 Id,
                       Sender:                   this,
                       RoamingNetwork:           this
                   );

        }

        #endregion

        #region AddOrUpdateChargingStationOperator   (ChargingStationOperator,      ..., OnAdded = null, OnUpdated = null, ...)

        /// <summary>
        /// Add or update the given user to/within the API.
        /// </summary>
        /// <param name="ChargingStationOperator">A user.</param>
        /// <param name="SkipNewChargingStationOperatorNotifications">Do not send notifications for this user addition.</param>
        /// <param name="SkipChargingStationOperatorUpdatedNotifications">Do not send the updated user information e-mail to the new user.</param>
        /// <param name="OnAdded">A delegate run whenever the user has been added successfully.</param>
        /// <param name="OnUpdated">A delegate run whenever the user has been updated successfully.</param>
        /// <param name="EventTrackingId">An optional unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<AddOrUpdateChargingStationOperatorResult>

            AddOrUpdateChargingStationOperator(ChargingStationOperator  ChargingStationOperator,
                                               EventTracking_Id?        EventTrackingId   = null,
                                               User_Id?                 CurrentUserId     = null)

        {

            var eventTrackingId = EventTrackingId ?? EventTracking_Id.New;

            //if (ChargingStationOperator.API is not null && ChargingStationOperator.API != this)
            //    return AddOrUpdateChargingStationOperatorResult.ArgumentError(ChargingStationOperator,
            //                                               eventTrackingId,
            //                                               nameof(ChargingStationOperator.API),
            //                                               "The given user is already attached to another API!");

            if (ChargingStationOperator.Id.Length < MinChargingStationOperatorIdLength)
                return AddOrUpdateChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given charging station operator identification '{ChargingStationOperator.Id}' is too short!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            if (ChargingStationOperator.Name.IsNullOrEmpty())
                return AddOrUpdateChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           EventTrackingId:          eventTrackingId,
                           Description:              $"The given charging station operator name must not be null or empty!".ToI18NString(),
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            if (ChargingStationOperator.Name.FirstText().Length < MinChargingStationOperatorNameLength)
                return AddOrUpdateChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           EventTrackingId:          eventTrackingId,
                           Description:              $"The given charging station operator name '{ChargingStationOperator.Name}' is too short!".ToI18NString(),
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );


            if (chargingStationOperators.TryGetValue(ChargingStationOperator.Id, out var oldChargingStationOperator))
            {

                chargingStationOperators.TryRemove(oldChargingStationOperator.Id, out _);

                //ChargingStationOperator.CopyAllLinkedDataFrom(OldChargingStationOperator);

            }


            if (chargingStationOperators.TryAdd(ChargingStationOperator.Id, ChargingStationOperator))
            {

                if (oldChargingStationOperator is null)
                    return AddOrUpdateChargingStationOperatorResult.Added(
                               ChargingStationOperator:  ChargingStationOperator,
                               EventTrackingId:          eventTrackingId,
                               SenderId:                 Id,
                               Sender:                   this,
                               RoamingNetwork:           this
                           );

                return AddOrUpdateChargingStationOperatorResult.Updated(
                           ChargingStationOperator:  ChargingStationOperator,
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );

            }

            return AddOrUpdateChargingStationOperatorResult.Error(
                       ChargingStationOperator:  ChargingStationOperator,
                       EventTrackingId:          eventTrackingId,
                       Description:              I18NString.Empty,
                       SenderId:                 Id,
                       Sender:                   this,
                       RoamingNetwork:           this
                   );

        }

        #endregion

        #region UpdateChargingStationOperator        ((New)ChargingStationOperator, ...,                 OnUpdated = null, ...)

        /// <summary>
        /// Update the given charging station operator to/within the API.
        /// </summary>
        /// <param name="NewChargingStationOperator">A charging station operator.</param>
        /// <param name="SkipChargingStationOperatorUpdatedNotifications">Do not send the updated charging station operator notifications.</param>
        /// <param name="OnUpdated">A delegate run whenever the charging station operator has been updated successfully.</param>
        /// <param name="EventTrackingId">An optional unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<UpdateChargingStationOperatorResult>

            UpdateChargingStationOperator(ChargingStationOperator  NewChargingStationOperator,
                                          EventTracking_Id?        EventTrackingId   = null,
                                          User_Id?                 CurrentUserId     = null)

        {

            var eventTrackingId = EventTrackingId ?? EventTracking_Id.New;

            if (!chargingStationOperators.TryGetValue(NewChargingStationOperator.Id, out var oldChargingStationOperator))
                return UpdateChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  NewChargingStationOperator,
                           Description:              $"The given user '{NewChargingStationOperator.Id}' does not exists in this API!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );


            chargingStationOperators.TryRemove(oldChargingStationOperator.Id, out _);

            //ChargingStationOperator.CopyAllLinkedDataFrom(oldChargingStationOperator);

            var result  = chargingStationOperators.TryAdd(NewChargingStationOperator.Id, NewChargingStationOperator);
            var now     = Timestamp.Now;

            return UpdateChargingStationOperatorResult.Success(
                       ChargingStationOperator:  NewChargingStationOperator,
                       EventTrackingId:          eventTrackingId,
                       SenderId:                 Id,
                       Sender:                   this,
                       RoamingNetwork:           this
                   );

        }

        #endregion


        #region (protected internal virtual) _CanRemoveChargingStationOperator(ChargingStationOperator)

        /// <summary>
        /// Determines whether the charging station operator can safely be removed from the API.
        /// </summary>
        /// <param name="ChargingStationOperator">The charging station operator to be removed.</param>
        protected internal virtual I18NString? _CanRemoveChargingStationOperator(ChargingStationOperator ChargingStationOperator)
        {

            //if (ChargingStationOperator.ChargingStationOperator2Organization_OutEdges.Any())
            //    return new I18NString(Languages.en, "The user is still member of an organization!");

            return null;

        }

        #endregion

        #region RemoveChargingStationOperator        (ChargingStationOperator,      ...,                 OnRemoved = null, ...)

        /// <summary>
        /// Remove the given charging station operator.
        /// </summary>
        /// <param name="ChargingStationOperator">The charging station operator to be removed.</param>
        /// <param name="OnRemoved">A delegate run whenever the charging station operator has been removed successfully.</param>
        /// <param name="EventTrackingId">An optional unique event tracking identification for correlating this request with other events.</param>
        /// <param name="CurrentUserId">An optional user identification initiating this command/request.</param>
        public async Task<DeleteChargingStationOperatorResult>

            RemoveChargingStationOperator(ChargingStationOperator  ChargingStationOperator,
                                          EventTracking_Id?        EventTrackingId   = null,
                                          User_Id?                 CurrentUserId     = null)

        {

            var eventTrackingId = EventTrackingId ?? EventTracking_Id.New;

            //if (ChargingStationOperator.API != this)
            //    return RemoveChargingStationOperatorResult.ArgumentError(
            //               ChargingStationOperator,
            //               eventTrackingId,
            //               nameof(ChargingStationOperator),
            //               "The given user is not attached to this API!"
            //           );

            if (!chargingStationOperators.ContainsKey(ChargingStationOperator.Id))
                return DeleteChargingStationOperatorResult.ArgumentError(
                           ChargingStationOperator:  ChargingStationOperator,
                           Description:              $"The given user does not exists in this API!".ToI18NString(),
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this
                       );


            var canBeRemoved  = _CanRemoveChargingStationOperator(ChargingStationOperator);

            if (canBeRemoved is not null)
                return DeleteChargingStationOperatorResult.CanNotBeRemoved(
                           ChargingStationOperator:  ChargingStationOperator,
                           EventTrackingId:          eventTrackingId,
                           SenderId:                 Id,
                           Sender:                   this,
                           RoamingNetwork:           this,
                           Description:              canBeRemoved
                       );


            var result  = chargingStationOperators.TryRemove(ChargingStationOperator.Id, out _);
            var now     = Timestamp.Now;

            return DeleteChargingStationOperatorResult.Success(
                       ChargingStationOperator:  ChargingStationOperator,
                       EventTrackingId:          eventTrackingId,
                       SenderId:                 Id,
                       Sender:                   this,
                       RoamingNetwork:           this
                   );

        }

        #endregion


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
            => ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingTariffs);

        #region ChargingPools...

        #region ChargingPools

        /// <summary>
        /// Return all charging pools registered within this roaming network.
        /// </summary>
        public IEnumerable<ChargingPool> ChargingPools

            => chargingStationOperators.Values.SelectMany(cso => cso.ChargingPools);

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

            => chargingStationOperators.Values.SelectMany(cso => cso.ChargingStations);

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

            => chargingStationOperators.Values.SelectMany(cso => cso.EVSEs);

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

            => energyMeters.Values;

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

        private readonly ConcurrentDictionary<GridOperator_Id, GridOperator> gridOperators;

        /// <summary>
        /// Return all smart cities registered within this roaming network.
        /// </summary>
        public IEnumerable<GridOperator> GridOperators
            => gridOperators.Values;

        #endregion


        #region GridOperatorsAdminStatus

        /// <summary>
        /// Return the admin status of all smart cities registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<GridOperator_Id, IEnumerable<Timestamped<GridOperatorAdminStatusTypes>>>> GridOperatorsAdminStatus

            => gridOperators.Values.
                   Select(emp => new KeyValuePair<GridOperator_Id, IEnumerable<Timestamped<GridOperatorAdminStatusTypes>>>(emp.Id, emp.AdminStatusSchedule()));

        #endregion

        #region GridOperatorsStatus

        /// <summary>
        /// Return the status of all smart cities registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<GridOperator_Id, IEnumerable<Timestamped<GridOperatorStatusTypes>>>> GridOperatorsStatus

            => gridOperators.Values.
                   Select(emp => new KeyValuePair<GridOperator_Id, IEnumerable<Timestamped<GridOperatorStatusTypes>>>(emp.Id, emp.StatusSchedule()));

        #endregion


        #region CreateNewGridOperator(GridOperatorId, Configurator = null)

        /// <summary>
        /// Create and register a new e-mobility (service) provider having the given
        /// unique smart city identification.
        /// </summary>
        /// <param name="GridOperatorId">The unique identification of the new smart city.</param>
        /// <param name="Name">The official (multi-language) name of the smart city.</param>
        /// <param name="Description">An optional (multi-language) description of the smart city.</param>
        /// <param name="Configurator">An optional delegate to configure the new smart city before its successful creation.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new smart city after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the smart city failed.</param>
        public GridOperator? CreateNewGridOperator(GridOperator_Id                           GridOperatorId,
                                                   I18NString?                               Name                       = null,
                                                   I18NString?                               Description                = null,
                                                   GridOperatorPriority?                     Priority                   = null,
                                                   GridOperatorAdminStatusTypes              AdminStatus                = GridOperatorAdminStatusTypes.Available,
                                                   GridOperatorStatusTypes                   Status                     = GridOperatorStatusTypes.Available,
                                                   Action<GridOperator>?                     Configurator               = null,
                                                   Action<GridOperator>?                     OnSuccess                  = null,
                                                   Action<RoamingNetwork, GridOperator_Id>?  OnError                    = null)
        {

            var gridOperator = new GridOperator(
                                   GridOperatorId,
                                   this,
                                   Name,
                                   Description,
                                   Priority,
                                   AdminStatus,
                                   Status
                               );


            if (gridOperators.TryAdd(gridOperator.Id, gridOperator))
            {

                // Link events!

                return gridOperator;

            }

            return null;

        }

        #endregion

        #region ContainsGridOperator(GridOperator)

        /// <summary>
        /// Check if the given GridOperator is already present within the roaming network.
        /// </summary>
        /// <param name="GridOperator">An Charging Station Operator.</param>
        public Boolean ContainsGridOperator(GridOperator GridOperator)

            => gridOperators.ContainsKey(GridOperator.Id);

        #endregion

        #region ContainsGridOperator(GridOperatorId)

        /// <summary>
        /// Check if the given GridOperator identification is already present within the roaming network.
        /// </summary>
        /// <param name="GridOperatorId">The unique identification of the Charging Station Operator.</param>
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

        #region RemoveGridOperator(GridOperatorId)

        public GridOperator? RemoveGridOperator(GridOperator_Id GridOperatorId)
        {

            if (gridOperators.TryRemove(GridOperatorId, out var gridOperator))
                return gridOperator;

            return null;

        }

        #endregion

        #region TryRemoveGridOperator(GridOperatorId, out GridOperator)

        public Boolean TryRemoveGridOperator(GridOperator_Id                        GridOperatorId,
                                             [NotNullWhen(true)] out GridOperator?  GridOperator)

            => gridOperators.TryRemove(GridOperatorId, out GridOperator);

        #endregion

        #endregion


        #region Parking Operators...

        #region ParkingOperators

        private readonly ConcurrentDictionary<ParkingOperator_Id, ParkingOperator> parkingOperators;

        /// <summary>
        /// Return all parking operators registered within this roaming network.
        /// </summary>
        public IEnumerable<ParkingOperator> ParkingOperators

            => parkingOperators.Values;

        #endregion


        #region ParkingOperatorAdminStatus

        /// <summary>
        /// Return the admin status of all parking operators registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorAdminStatusTypes>>>> ParkingOperatorAdminStatus

            => parkingOperators.Values.
                   Select(pop => new KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorAdminStatusTypes>>>(pop.Id,
                                                                                                                                 pop.AdminStatusSchedule()));

        #endregion

        #region ParkingOperatorStatus

        /// <summary>
        /// Return the status of all parking operators registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorStatusTypes>>>> ParkingOperatorStatus

            => parkingOperators.Values.
                   Select(pop => new KeyValuePair<ParkingOperator_Id, IEnumerable<Timestamped<ParkingOperatorStatusTypes>>>(pop.Id,
                                                                                                                            pop.StatusSchedule()));

        #endregion


        #region CreateNewParkingOperator(Id, Name = null, Description = null, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new parking operator having the given
        /// unique parking operator identification.
        /// </summary>
        /// <param name="Id">The unique identification of the new parking operator.</param>
        /// <param name="Name">The official (multi-language) name of the parking operator.</param>
        /// <param name="Description">An optional (multi-language) description of the parking operator.</param>
        /// <param name="Configurator">An optional delegate to configure the new parking operator before its successful creation.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new parking operator after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the parking operator failed.</param>
        public ParkingOperator? CreateNewParkingOperator(ParkingOperator_Id                           Id,
                                                         I18NString?                                  Name                           = null,
                                                         I18NString?                                  Description                    = null,
                                                         ParkingOperatorAdminStatusTypes?             InititalAdminStatus            = ParkingOperatorAdminStatusTypes.Operational,
                                                         ParkingOperatorStatusTypes?                  InititalStatus                 = ParkingOperatorStatusTypes.Available,
                                                         Action<ParkingOperator>?                     OnSuccess                      = null,
                                                         Action<RoamingNetwork, ParkingOperator_Id>?  OnError                        = null)

        {

            var parkingOperator = new ParkingOperator(Id,
                                                      this,
                                                      Name,
                                                      Description,
                                                      InititalAdminStatus,
                                                      InititalStatus);


            if (parkingOperators.TryAdd(parkingOperator.Id, parkingOperator))
                return parkingOperator;

            return null;

        }

        #endregion

        #region CreateParkingSpace(ParkingSpaceId, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new parking space having the given
        /// unique parking space identification.
        /// </summary>
        /// <param name="ParkingSpaceId">The unique identification of the new charging pool.</param>
        /// <param name="Configurator">An optional delegate to configure the new charging pool before its successful creation.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new charging pool after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the charging pool failed.</param>
        public ParkingSpace CreateParkingSpace(ParkingSpace_Id                                    ParkingSpaceId,
                                               Action<ParkingSpace>?                              Configurator   = null,
                                               Action<ParkingSpace>?                              OnSuccess      = null,
                                               Action<ChargingStationOperator, ParkingSpace_Id>?  OnError        = null)
        {

            var parkingSpace = new ParkingSpace(ParkingSpaceId);

            Configurator?.Invoke(parkingSpace);

            return parkingSpace;

        }

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

        #region RemoveParkingOperator(ParkingOperatorId)

        public ParkingOperator? RemoveParkingOperator(ParkingOperator_Id ParkingOperatorId)
        {

            if (parkingOperators.TryRemove(ParkingOperatorId, out var parkingOperator))
                return parkingOperator;

            return null;

        }

        #endregion

        #region TryRemoveParkingOperator(ParkingOperatorId, out ParkingOperator)

        public Boolean TryRemoveParkingOperator(ParkingOperator_Id                        ParkingOperatorId,
                                                [NotNullWhen(true)] out ParkingOperator?  ParkingOperator)

            => parkingOperators.TryRemove(ParkingOperatorId, out ParkingOperator);

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

            return CustomRoamingNetworkSerializer is not null
                       ? CustomRoamingNetworkSerializer(this, JSON)
                       : JSON;

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
