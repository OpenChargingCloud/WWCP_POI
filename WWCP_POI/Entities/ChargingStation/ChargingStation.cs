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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

/// <summary>
    /// Extension methods for the common charging station interface.
    /// </summary>
    public static class IChargingStationExtensions
    {

        #region ToJSON(this ChargingStations, Skip = null, Take = null, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given enumeration of charging stations.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="Skip">The optional number of charging stations to skip.</param>
        /// <param name="Take">The optional number of charging stations to return.</param>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a charging pool.</param>
        public static JArray ToJSON(this IEnumerable<ChargingStation>                    ChargingStations,
                                    UInt64?                                              Skip                                = null,
                                    UInt64?                                              Take                                = null,
                                    Boolean                                              Embedded                            = false,
                                    Boolean?                                             IncludeRemoved                      = false,
                                    InfoStatus                                           ExpandRoamingNetworkId              = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandChargingStationOperatorId     = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandChargingPoolId                = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandEVSEIds                       = InfoStatus.Expanded,
                                    InfoStatus                                           ExpandBrandIds                      = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandDataLicenses                  = InfoStatus.ShowIdOnly,
                                    Boolean?                                             IncludeRemovedEVSEs                 = false,
                                    Boolean?                                             IncludeCustomData                   = null,
                                    CustomJObjectSerializerDelegate<ChargingStation>?    CustomChargingStationSerializer     = null,
                                    CustomJObjectSerializerDelegate<EVSE>?               CustomEVSESerializer                = null,
                                    CustomJObjectSerializerDelegate<ChargingConnector>?  CustomChargingConnectorSerializer   = null)


            => ChargingStations is not null && ChargingStations.Any()

                   ? new JArray(
                         ChargingStations.
                             Where          (chargingStation => chargingStation is not null).
                             Where          (chargingStation => IncludeRemoved == true || chargingStation.Status != ChargingStationStatusType.Removed).
                             OrderBy        (chargingStation => chargingStation.Id).
                             SkipTakeFilter (Skip, Take).
                             SafeSelect     (chargingStation => chargingStation.ToJSON(Embedded,
                                                                                       ExpandRoamingNetworkId,
                                                                                       ExpandChargingStationOperatorId,
                                                                                       ExpandChargingPoolId,
                                                                                       ExpandEVSEIds,
                                                                                       ExpandBrandIds,
                                                                                       ExpandDataLicenses,
                                                                                       IncludeRemovedEVSEs,
                                                                                       IncludeCustomData,
                                                                                       CustomChargingStationSerializer,
                                                                                       CustomEVSESerializer,
                                                                                       CustomChargingConnectorSerializer)).
                             Where          (chargingStation => chargingStation is not null)
                     )

                   : [];

        #endregion

    }


    /// <summary>
    /// A charging station to charge an electric vehicle.
    /// </summary>
    public sealed partial class ChargingStation : AImmutableEMobilityEntity<ChargingStation_Id,
                                                    ChargingStationAdminStatusType,
                                                    ChargingStationStatusType>,
                                   IEquatable <ChargingStation>,
                                   IComparable<ChargingStation>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const            String    JSONLDContext                                       = "https://open.charging.cloud/contexts/wwcp+json/chargingStation";


        private readonly        Decimal   EPSILON                                             = 0.01m;

        /// <summary>
        /// The default max size of the charging station admin status list.
        /// </summary>
        public const            UInt16    DefaultMaxChargingStationAdminStatusScheduleSize    = 15;

        /// <summary>
        /// The default max size of the charging station (aggregated EVSE) status list.
        /// </summary>
        public const            UInt16    DefaultMaxChargingStationStatusScheduleSize         = 15;

        /// <summary>
        /// The maximum time span for a reservation.
        /// </summary>
        public static readonly  TimeSpan  DefaultMaxChargingStationReservationDuration        = TimeSpan.FromMinutes(30);

        #endregion

        #region Properties

        /// <summary>
        /// The roaming network of this charging station.
        /// </summary>
        [InternalUseOnly]
        public RoamingNetwork?              RoamingNetwork
            => Operator?.RoamingNetwork;

        /// <summary>
        /// The charging station operator of this charging station.
        /// </summary>
        [InternalUseOnly]
        public ChargingStationOperator?     Operator
            => ChargingPool?.Operator;

        /// <summary>
        /// The charging station sub operator of this charging station.
        /// </summary>
        [Optional]
        public ChargingStationOperator?             SubOperator                 { get; }

        /// <summary>
        /// The charging pool.
        /// </summary>
        [InternalUseOnly]
        public ChargingPool?                        ChargingPool                { get; }


        public Boolean                               Published                   { get; }

        public Boolean                               Disabled                    { get; }


        /// <summary>
        /// All brands registered for this charging station.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<Brand> immutableBrands = [];
        [Optional, SlowData]
        public System.Collections.Immutable.ImmutableArray<Brand> Brands
            => ImmutablePOIValues.CopyItems(immutableBrands);

        /// <summary>
        /// All e-mobility related Root-CAs, e.g. ISO 15118-2/-20, available at this charging station.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<RootCAInfo> immutableMobilityRootCAs = [];
        [Optional, SlowData]
        public IEnumerable<RootCAInfo> MobilityRootCAs
        {
            get => ImmutablePOIValues.CopyItems(immutableMobilityRootCAs);
            private set => immutableMobilityRootCAs = ImmutablePOIValues.CopyItems(value);
        }

        /// <summary>
        /// An optional enumeration of EV roaming partners.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<EVRoamingPartnerInfo> immutableEVRoamingPartners = [];
        [Optional, SlowData]
        public IEnumerable<EVRoamingPartnerInfo> EVRoamingPartners
        {
            get => ImmutablePOIValues.CopyItems(immutableEVRoamingPartners);
            private set => immutableEVRoamingPartners = ImmutablePOIValues.CopyItems(value);
        }


        /// <summary>
        /// The optional URL where declarations of conformity, certificates and other documents can be found.
        /// </summary>
        public URL?                                  CertificationInfo           { get; }

        /// <summary>
        /// The optional URL where certificates, identifiers and public keys related to the calibration
        /// of the charging station can be found.
        /// </summary>
        public URL?                                  CalibrationInfo             { get; }

        /// <summary>
        /// The license of the charging station data.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<DataLicense> immutableDataLicenses = [];
        [Mandatory, SlowData]
        public IEnumerable<DataLicense> DataLicenses
        {
            get => ImmutablePOIValues.CopyItems(immutableDataLicenses);
            private set => immutableDataLicenses = ImmutablePOIValues.CopyItems(value);
        }


        #region Address

        private Address? address;

        /// <summary>
        /// The address of this charging station.
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

        #region OpenStreetMap NodeId

        private String? openStreetMapNodeId;

        /// <summary>
        /// OpenStreetMap Node Id.
        /// </summary>
        [Optional]
        public String? OpenStreetMapNodeId
        {

            get
            {
                return openStreetMapNodeId;
            }


        }

        #endregion

        #region GeoLocation

        private GeoCoordinate? geoLocation;

        /// <summary>
        /// The geographical location of this charging station.
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

        #region EntranceAddress

        private Address? entranceAddress;

        /// <summary>
        /// The address of the entrance to this charging station.
        /// (If different from 'Address').
        /// </summary>
        [Optional]
        public Address? EntranceAddress
        {

            get
            {
                return ImmutablePOIValues.Copy(entranceAddress);
            }


        }

        #endregion

        #region EntranceLocation

        internal GeoCoordinate? entranceLocation;

        /// <summary>
        /// The geographical location of the entrance to this charging station.
        /// (If different from 'GeoLocation').
        /// </summary>
        [Optional]
        public GeoCoordinate? EntranceLocation
        {

            get
            {
                return entranceLocation;
            }


        }

        #endregion

        #region ArrivalInstructions

        /// <summary>
        /// An optional (multi-language) description of how to find the charging station.
        /// </summary>
        private ImmutableI18NString immutableArrivalInstructions = ImmutableI18NString.Empty;
        [Optional]
        public ImmutableI18NString ArrivalInstructions
        {
            get => ImmutablePOIValues.Copy(immutableArrivalInstructions);
            private set => immutableArrivalInstructions = ImmutablePOIValues.Copy(value);
        }

        #endregion

        #region OpeningTimes

        private ImmutableOpeningTimes openingTimes;

        /// <summary>
        /// The opening times of this charging station (non recursive).
        /// </summary>
        public ImmutableOpeningTimes OpeningTimes
        {

            get
            {
                return ImmutablePOIValues.Copy(openingTimes);
            }


        }

        #endregion

        #region ParkingSpaces

        /// <summary>
        /// Parking spaces located at the charging station.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<ParkingSpace> immutableParkingSpaces = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<ParkingSpace> ParkingSpaces
            => ImmutablePOIValues.CopyItems(immutableParkingSpaces);

        #endregion

        #region UIFeatures

        /// <summary>
        /// User interface features of the charging station.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<UIFeatures> immutableUIFeatures = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<UIFeatures> UIFeatures
            => ImmutablePOIValues.CopyItems(immutableUIFeatures);

        #endregion

        #region AuthenticationModes

        /// <summary>
        /// The authentication options an EV driver can use.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<AuthenticationModes> immutableAuthenticationModes = [];
        [Mandatory]
        public System.Collections.Immutable.ImmutableArray<AuthenticationModes> AuthenticationModes
            => ImmutablePOIValues.CopyItems(immutableAuthenticationModes);

        #endregion

        #region PaymentOptions

        /// <summary>
        /// The payment options an EV driver can use.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<PaymentOptions> immutablePaymentOptions = [];
        [Mandatory]
        public System.Collections.Immutable.ImmutableArray<PaymentOptions> PaymentOptions
            => ImmutablePOIValues.CopyItems(immutablePaymentOptions);

        #endregion

        #region Accessibility

        private AccessibilityType? accessibility;

        /// <summary>
        /// The accessibility of the charging station.
        /// </summary>
        [Optional]
        public AccessibilityType? Accessibility
        {

            get
            {

                return accessibility is not null
                           ? accessibility
                           : ChargingPool?.Accessibility;

            }

            private set
            {

                if (value != accessibility && value != ChargingPool?.Accessibility)
                {

                    if (value is null)
                        DeleteProperty(ref accessibility);

                    else
                        SetProperty(ref accessibility, value);

                }

            }

        }

        #endregion

        #region Features

        /// <summary>
        /// Charging features of the charging station.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<ChargingStationFeature> immutableFeatures = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<ChargingStationFeature> Features
            => ImmutablePOIValues.CopyItems(immutableFeatures);

        #endregion

        #region PhotoURLs

        /// <summary>
        /// URIs of photos of this charging station.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<URL> immutablePhotoURLs = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<URL> PhotoURLs
            => ImmutablePOIValues.CopyItems(immutablePhotoURLs);

        #endregion

        #region PhysicalReference

        private String? physicalReference;

        /// <summary>
        /// An optional number/string printed on the outside of the charging station for visual identification.
        /// </summary>
        [Optional]
        public String? PhysicalReference
        {

            get
            {
                return physicalReference;
            }


        }

        #endregion

        #region LocationLanguage

        private Languages? locationLanguage;

        /// <summary>
        /// The location language.
        /// </summary>
        [Optional]
        public Languages? LocationLanguage
        {

            get
            {
                return locationLanguage;
            }


        }

        #endregion

        #region HotlinePhoneNumber

        private PhoneNumber? hotlinePhoneNumber;

        /// <summary>
        /// The telephone number of the charging station operator hotline.
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

        #region ChargingWhenClosed

        private Boolean? chargingWhenClosed;

        /// <summary>
        /// Whether chargingWhenClosed?
        /// </summary>
        [Optional]
        public Boolean? ChargingWhenClosed
        {

            get
            {
                return chargingWhenClosed;
            }


        }

        #endregion


        #region ExitAddress

        private Address? exitAddress;

        /// <summary>
        /// The address of the exit of this charging station.
        /// (If different from 'Address').
        /// </summary>
        [Optional]
        public Address ExitAddress
        {

            get
            {
                return ImmutablePOIValues.Copy(exitAddress ?? ChargingPool?.ExitAddress);
            }

            private set
            {

                if (value != exitAddress && value != ChargingPool?.ExitAddress)
                {

                    if (value is null)
                        DeleteProperty(ref exitAddress);

                    else
                        SetProperty(ref exitAddress, value);

                }

            }

        }

        #endregion

        #region ExitLocation

        private GeoCoordinate? exitLocation;

        /// <summary>
        /// The geographical location of the exit of this charging station.
        /// (If different from 'GeoLocation').
        /// </summary>
        [Optional]
        public GeoCoordinate? ExitLocation
        {

            get
            {

                return exitLocation.HasValue
                           ? exitLocation
                           : ChargingPool?.ExitLocation;

            }

            private set
            {

                if (value != exitLocation && value != ChargingPool?.ExitLocation)
                {

                    if (value is null)
                        DeleteProperty(ref exitLocation);

                    else
                        SetProperty(ref exitLocation, value);

                }

            }

        }

        #endregion


        private System.Collections.Immutable.ImmutableArray<Image> immutableImages = [];
        public IEnumerable<Image> Images
        {
            get => ImmutablePOIValues.CopyItems(immutableImages);
            private set => immutableImages = ImmutablePOIValues.CopyItems(value);
        }

        private System.Collections.Immutable.ImmutableArray<VehicleType> immutableVehicleTypes = [];
        public IEnumerable<VehicleType> VehicleTypes
        {
            get => ImmutablePOIValues.CopyItems(immutableVehicleTypes);
            private set => immutableVehicleTypes = ImmutablePOIValues.CopyItems(value);
        }


        #region GridConnection

        private GridConnectionTypes? gridConnection;

        /// <summary>
        /// The grid connection of the charging station.
        /// </summary>
        [Optional]
        public GridConnectionTypes? GridConnection
        {

            get
            {
                return gridConnection;
            }


        }

        #endregion

        /// <summary>
        /// The directly owned energy meters, for example at the grid or photovoltaic connection.
        /// Membership and POI data are immutable; each meter retains mutable runtime statuses.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<EnergyMeter> EnergyMeters { get; }


        #region MaxCurrent

        private Decimal? maxCurrent;

        /// <summary>
        /// The maximum current [Ampere].
        /// </summary>
        [Mandatory, SlowData]
        public Decimal? MaxCurrent
        {

            get
            {
                return maxCurrent;
            }


        }

        #endregion

        #region MaxCurrentRealTime

        private Timestamped<Decimal>? maxCurrentRealTime;

        /// <summary>
        /// The real-time maximum current [Ampere].
        /// </summary>
        [Optional, FastData]
        public Timestamped<Decimal>? MaxCurrentRealTime
        {

            get
            {
                return maxCurrentRealTime;
            }

            set
            {

                if (value is not null)
                    SetProperty(ref maxCurrentRealTime,
                                value);

                else
                    DeleteProperty(ref maxCurrentRealTime);

            }

        }

        #endregion

        /// <summary>
        /// Prognoses on future values of the maximum current [Ampere].
        /// </summary>
        [Optional, FastData]
        public ReactiveSet<Timestamped<Decimal>>        MaxCurrentPrognoses     { get; }


        #region MaxPower

        private Decimal? maxPower;

        /// <summary>
        /// The maximum power [kWatt].
        /// </summary>
        [Optional, SlowData]
        public Decimal? MaxPower
        {

            get
            {
                return maxPower;
            }


        }

        #endregion

        #region MaxPowerRealTime

        private Timestamped<Decimal>? maxPowerRealTime;

        /// <summary>
        /// The real-time maximum power [kWatt].
        /// </summary>
        [Optional, FastData]
        public Timestamped<Decimal>? MaxPowerRealTime
        {

            get
            {
                return maxPowerRealTime;
            }

            set
            {

                if (value is not null)
                    SetProperty(ref maxPowerRealTime,
                                value);

                else
                    DeleteProperty(ref maxPowerRealTime);

            }

        }

        #endregion

        /// <summary>
        /// Prognoses on future values of the maximum power [kWatt].
        /// </summary>
        [Optional, FastData]
        public ReactiveSet<Timestamped<Decimal>>        MaxPowerPrognoses       { get; }


        #region MaxCapacity

        private Decimal? maxCapacity;

        /// <summary>
        /// The maximum capacity [kWh].
        /// </summary>
        [Mandatory]
        public Decimal? MaxCapacity
        {

            get
            {
                return maxCapacity;
            }


        }

        #endregion

        #region MaxCapacityRealTime

        private Timestamped<Decimal>? maxCapacityRealTime;

        /// <summary>
        /// The real-time maximum capacity [kWh].
        /// </summary>
        [Mandatory]
        public Timestamped<Decimal>? MaxCapacityRealTime
        {

            get
            {
                return maxCapacityRealTime;
            }

            set
            {

                if (value is not null)
                    SetProperty(ref maxCapacityRealTime,
                                value);

                else
                    DeleteProperty(ref maxCapacityRealTime);

            }

        }

        #endregion

        /// <summary>
        /// Prognoses on future values of the maximum capacity [kWh].
        /// </summary>
        [Mandatory]
        public ReactiveSet<Timestamped<Decimal>>        MaxCapacityPrognoses    { get; }


        #region EnergyMix

        private EnergyMix? energyMix;

        /// <summary>
        /// The energy mix.
        /// </summary>
        [Optional, SlowData]
        public EnergyMix? EnergyMix
        {

            get
            {
                return ImmutablePOIValues.Copy(energyMix ?? ChargingPool?.EnergyMix);
            }

            private set
            {

                if (value != energyMix && value != ChargingPool?.EnergyMix)
                {

                    if (value is null)
                        DeleteProperty(ref energyMix);

                    else
                        SetProperty(ref energyMix, value);

                }

            }

        }

        #endregion

        #region EnergyMixRealTime

        private Timestamped<EnergyMix>? energyMixRealTime;

        /// <summary>
        /// The current energy mix.
        /// </summary>
        [Optional, FastData]
        public Timestamped<EnergyMix>? EnergyMixRealTime
        {

            get
            {
                return energyMixRealTime;
            }

            set
            {

                if (value is not null)
                    SetProperty(ref energyMixRealTime,
                                value);

                else
                    DeleteProperty(ref energyMixRealTime);

            }

        }

        #endregion

        #region EnergyMixPrognoses

        private EnergyMixPrognosis? energyMixPrognoses;

        /// <summary>
        /// Prognoses on future values of the energy mix.
        /// </summary>
        [Optional, FastData]
        public EnergyMixPrognosis? EnergyMixPrognoses
        {

            get
            {
                return energyMixPrognoses ?? ChargingPool?.EnergyMixPrognoses;
            }

            set
            {

                if (value != energyMixPrognoses && value != ChargingPool?.EnergyMixPrognoses)
                {

                    if (value is null)
                        DeleteProperty(ref energyMixPrognoses);

                    else
                        SetProperty(ref energyMixPrognoses, value);

                }

            }

        }

        #endregion


        #region MaxReservationDuration

        private TimeSpan maxReservationDuration;

        /// <summary>
        /// The maximum reservation time at this EVSE.
        /// </summary>
        [Optional, SlowData]
        public TimeSpan MaxReservationDuration
        {

            get
            {
                return maxReservationDuration;
            }


        }

        #endregion

        #region IsFreeOfCharge

        private Boolean isFreeOfCharge;

        /// <summary>
        /// Charging at this EVSE is ALWAYS free of charge.
        /// </summary>
        [Optional, SlowData]
        public Boolean IsFreeOfCharge
        {

            get
            {
                return isFreeOfCharge;
            }


        }

        #endregion

        #region IsHubjectCompatible

        private Boolean _IsHubjectCompatible;

        [Optional]
        public Boolean IsHubjectCompatible
        {

            get
            {
                return _IsHubjectCompatible;
            }


        }

        #endregion

        #region DynamicInfoAvailable

        private Boolean _DynamicInfoAvailable;

        [Optional]
        public Boolean DynamicInfoAvailable
        {

            get
            {
                return _DynamicInfoAvailable;
            }


        }

        #endregion


        #region ServiceIdentification

        private String? serviceIdentification;

        /// <summary>
        /// The internal service identification of the charging station maintained by the Charging Station Operator.
        /// </summary>
        [Optional]
        public String? ServiceIdentification
        {

            get
            {
                return serviceIdentification;
            }


        }

        #endregion

        #region ModelCode

        private String? modelCode;

        /// <summary>
        /// The internal model code of the charging station maintained by the Charging Station Operator.
        /// </summary>
        [Optional]
        public String? ModelCode
        {

            get
            {
                return modelCode;
            }


        }

        #endregion

        #region HubjectStationId

        private String? hubjectStationId;

        [Optional]
        public String? HubjectStationId
        {

            get
            {
                return hubjectStationId;
            }


        }

        #endregion


        #region StatusAggregationDelegate

        /// <summary>
        /// A delegate called to aggregate the dynamic status of all subordinated EVSEs.
        /// </summary>
        public Func<EVSEStatusReport, ChargingStationStatusType>? StatusAggregationDelegate { get; set; }

        #endregion

        #endregion

        #region Events

        #region OnData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data changed.
        /// </summary>
        public event OnChargingStationDataChangedDelegate?         OnDataChanged;

        /// <summary>
        /// An event fired whenever the dynamic status changed.
        /// </summary>
        public event OnChargingStationStatusChangedDelegate?       OnStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingStationAdminStatusChangedDelegate?  OnAdminStatusChanged;

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station having the given identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station pool.</param>
        /// <param name="ChargingPool">The parent charging pool.</param>
        /// <param name="EnergyMeters">Optional directly owned energy meters, with unique IDs within this station.</param>
        ///
        /// <param name="InitialAdminStatus">An optional initial admin status of the EVSE.</param>
        /// <param name="InitialStatus">An optional initial status of the EVSE.</param>
        /// <param name="MaxAdminStatusScheduleSize">An optional max length of the admin staus list.</param>
        /// <param name="MaxStatusScheduleSize">An optional max length of the staus list.</param>
        ///
        /// <param name="Configurator">A delegate to configure the newly created charging station.</param>
        /// <param name="RemoteChargingStationCreator">A delegate to attach a remote charging station.</param>
        public ChargingStation(ChargingStation_Id                             Id,
                               ChargingPool?                                 ChargingPool                   = null,

                               I18NString?                                    Name                           = null,
                               I18NString?                                    Description                    = null,

                               Address?                                       Address                        = null,
                               GeoCoordinate?                                 GeoLocation                    = null,
                               OpeningTimes?                                  OpeningTimes                   = null,
                               Boolean?                                       ChargingWhenClosed             = null,
                               AccessibilityType?                             Accessibility                  = null,
                               Languages?                                     LocationLanguage               = null,
                               String?                                        PhysicalReference              = null,
                               PhoneNumber?                                   HotlinePhoneNumber             = null,

                               IEnumerable<AuthenticationModes>?              AuthenticationModes            = null,
                               IEnumerable<PaymentOptions>?                   PaymentOptions                 = null,
                               IEnumerable<ChargingStationFeature>?           Features                       = null,
                               IEnumerable<VehicleType>?                      VehicleTypes                   = null,
                               IEnumerable<Image>?                            Images                         = null,

                               String?                                        ServiceIdentification          = null,
                               String?                                        ModelCode                      = null,

                               Boolean?                                       Published                      = null,
                               Boolean?                                       Disabled                       = null,

                               IEnumerable<Brand>?                            Brands                         = null,
                               IEnumerable<RootCAInfo>?                       MobilityRootCAs                = null,
                               IEnumerable<EVRoamingPartnerInfo>?             EVRoamingPartners              = null,
                               URL?                                           CertificationInfo              = null,
                               URL?                                           CalibrationInfo                = null,

                               Timestamped<ChargingStationAdminStatusType>?  InitialAdminStatus             = null,
                               Timestamped<ChargingStationStatusType>?       InitialStatus                  = null,
                               UInt16?                                        MaxAdminStatusScheduleSize     = null,
                               UInt16?                                        MaxStatusScheduleSize          = null,

                               String?                                        DataSource                     = null,
                               DateTimeOffset?                                Created                        = null,
                               DateTimeOffset?                                LastChange                     = null,

                               CustomDataNew?                                 CustomData                     = null,
                               UserDefinedDictionary?                         InternalData                   = null,
                               IEnumerable<DataLicense>?                      DataLicenses                   = null,
                               IEnumerable<EnergyMeter>?                      EnergyMeters                   = null)

            : base(Id,
                   Name,
                   Description,
                   InitialAdminStatus         ?? ChargingStationAdminStatusType.Operational,
                   InitialStatus              ?? ChargingStationStatusType.     Available,
                   MaxAdminStatusScheduleSize ?? DefaultMaxChargingStationAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxChargingStationStatusScheduleSize,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            #region Init data and properties

            this.ChargingPool                        = ChargingPool;

            this.EnergyMeters = ImmutablePOIValues.CopyEnergyMeters(EnergyMeters);

            this.address                             = ImmutablePOIValues.Copy(Address);
            this.geoLocation                         = GeoLocation;
            this.openingTimes                        = ImmutablePOIValues.Copy(OpeningTimes                  ?? OpeningTimes.Open24Hours);
            this.hotlinePhoneNumber                  = HotlinePhoneNumber;
            this.physicalReference                   = PhysicalReference;
            this.chargingWhenClosed                  = ChargingWhenClosed;
            this.accessibility                       = Accessibility;
            this.locationLanguage                    = LocationLanguage;
            this.serviceIdentification               = ServiceIdentification;
            this.modelCode                           = ModelCode;
            this.CertificationInfo                   = CertificationInfo;
            this.CalibrationInfo                     = CalibrationInfo;
            this.VehicleTypes                        = VehicleTypes?.     Distinct() ?? [];
            this.Images                              = Images?.           Distinct() ?? [];
            this.MobilityRootCAs                     = MobilityRootCAs?.  Distinct() ?? [];
            this.EVRoamingPartners                   = EVRoamingPartners?.Distinct() ?? [];
            this.DataLicenses                        = DataLicenses?.     Distinct() ?? [];

            this.Published                           = Published                     ?? true;
            this.Disabled                            = Disabled                      ?? false;

            this.immutableBrands                              = [];

            if (Brands is not null)
                foreach (var brand in Brands)
                    this.immutableBrands = this.immutableBrands.Add(ImmutablePOIValues.Copy(brand));


            this.immutableUIFeatures                          = [];


            this.immutableAuthenticationModes                 = [];

            if (AuthenticationModes is not null)
                foreach (var authenticationMode in AuthenticationModes)
                    this.immutableAuthenticationModes = this.immutableAuthenticationModes.Add(ImmutablePOIValues.Copy(authenticationMode));


            this.immutablePaymentOptions                      = [];

            if (PaymentOptions is not null)
                foreach (var paymentOption in PaymentOptions)
                    this.immutablePaymentOptions = this.immutablePaymentOptions.Add(ImmutablePOIValues.Copy(paymentOption));


            this.immutableFeatures                            = [];

            if (Features is not null)
                foreach (var feature in Features)
                    this.immutableFeatures = this.immutableFeatures.Add(ImmutablePOIValues.Copy(feature));


            this.immutableParkingSpaces                       = [];


            this.immutableParkingSpaces                       = [];


            this.immutablePhotoURLs                           = [];


            this.ArrivalInstructions = ArrivalInstructions ?? I18NString.Empty;


            this.MaxCurrentPrognoses                = [];
            this.MaxCurrentPrognoses.OnSetChanged  += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxCurrentPrognoses",
                                oldItems,
                                newItems);

            };

            this.MaxPowerPrognoses                  = [];
            this.MaxPowerPrognoses.OnSetChanged    += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxPowerPrognoses",
                                oldItems,
                                newItems);

            };

            this.MaxCapacityPrognoses               = [];
            this.MaxCapacityPrognoses.OnSetChanged += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxCapacityPrognoses",
                                oldItems,
                                newItems);

            };

            //this.evses                             = new EntityHashSet<ChargingStation, EVSE_Id, EVSE>(this);
            //this.evses.OnSetChanged               += (timestamp, reactiveSet, newItems, oldItems) =>
            //{

            //    PropertyChanged("EVSEs",
            //                    oldItems,
            //                    newItems);

            //};

            this.evses                             = new EntityHashSet<ChargingStation, EVSE_Id,  EVSE> (this);

            #endregion

            #region Link events

            this.OnPropertyChanged += UpdateData;

            this.adminStatusSchedule.OnStatusChanged += (timestamp, eventTrackingId, statusSchedule, newStatus, oldStatus, dataSource)
                                                          => UpdateAdminStatus(timestamp, eventTrackingId, newStatus, oldStatus, dataSource);

            this.statusSchedule.     OnStatusChanged += (timestamp, eventTrackingId, statusSchedule, newStatus, oldStatus, dataSource)
                                                          => UpdateStatus     (timestamp, eventTrackingId, newStatus, oldStatus, dataSource);

            #endregion

        }

        #endregion


        #region Data/(Admin-)Status

        #region (internal) UpdateData       (Timestamp, EventTrackingId, Sender, PropertyName, NewValue, OldValue = null, DataSource = null)

        /// <summary>
        /// Update the static data.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="Sender">The changed charging station.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        /// <param name="OldValue">The optional old value of the changed property.</param>
        /// <param name="DataSource">An optional data source or context for the charging station data update.</param>
        internal async Task UpdateData(DateTimeOffset    Timestamp,
                                       EventTracking_Id  EventTrackingId,
                                       Object            Sender,
                                       String            PropertyName,
                                       Object?           NewValue,
                                       Object?           OldValue     = null,
                                       Context?          DataSource   = null)
        {

            var onDataChanged = OnDataChanged;
            if (onDataChanged is not null)
                await onDataChanged(Timestamp,
                                    EventTrackingId,
                                    Sender as ChargingStation,
                                    PropertyName,
                                    NewValue,
                                    OldValue,
                                    DataSource);

        }

        #endregion

        #region (internal) UpdateAdminStatus(Timestamp, EventTrackingId, OldStatus, NewStatus, DataSource = null)

        /// <summary>
        /// Update the current admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="NewStatus">The new charging station admin status.</param>
        /// <param name="OldStatus">The optional old charging station admin status.</param>
        /// <param name="DataSource">An optional data source or context for the charging station admin status update.</param>
        internal async Task UpdateAdminStatus(DateTimeOffset                                 Timestamp,
                                              EventTracking_Id                               EventTrackingId,
                                              Timestamped<ChargingStationAdminStatusType>   NewStatus,
                                              Timestamped<ChargingStationAdminStatusType>?  OldStatus    = null,
                                              Context?                                       DataSource   = null)
        {

            var onAdminStatusChanged = OnAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp,
                                           EventTrackingId,
                                           this,
                                           NewStatus,
                                           OldStatus,
                                           DataSource);

        }

        #endregion

        #region (internal) UpdateStatus     (Timestamp, EventTrackingId, OldStatus, NewStatus, DataSource = null)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="NewStatus">The new charging station status.</param>
        /// <param name="OldStatus">The optional old charging station status.</param>
        /// <param name="DataSource">An optional data source or context for the charging station status update.</param>
        internal async Task UpdateStatus(DateTimeOffset                            Timestamp,
                                         EventTracking_Id                          EventTrackingId,
                                         Timestamped<ChargingStationStatusType>   NewStatus,
                                         Timestamped<ChargingStationStatusType>?  OldStatus    = null,
                                         Context?                                  DataSource   = null)
        {

            var onAggregatedStatusChanged = OnStatusChanged;
            if (onAggregatedStatusChanged is not null)
                await onAggregatedStatusChanged(Timestamp,
                                                EventTrackingId,
                                                this,
                                                NewStatus,
                                                OldStatus,
                                                DataSource);

        }

        #endregion

        #endregion

        #region EVSEs

        #region EVSEAddition

        /// <summary>
        /// Called whenever an EVSE will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStation, EVSE, Boolean> OnEVSEAddition

            => evses.OnAddition;

        #endregion

        #region EVSEUpdate

        /// <summary>
        /// Called whenever an EVSE will be or was updated.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStation, EVSE, EVSE, Boolean> OnEVSEUpdate

            => evses.OnUpdate;

        #endregion

        #region EVSERemoval

        /// <summary>
        /// Called whenever an EVSE will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingStation, EVSE, Boolean> OnEVSERemoval

            => evses.OnRemoval;

        #endregion


        #region EVSEs

        private readonly EntityHashSet<ChargingStation, EVSE_Id, EVSE> evses;

        /// <summary>
        /// All Electric Vehicle Supply Equipments (EVSE) present
        /// within this charging station.
        /// </summary>
        public IEnumerable<EVSE> EVSEs
        {
            get
            {
                lock (evses)
                {
                    return evses.ToArray();
                }
            }
        }

        #endregion

        #region EVSEIds                (IncludeEVSEs = null)

        /// <summary>
        /// The unique identifications of all Electric Vehicle Supply Equipment (EVSEs)
        /// present within this charging station.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSE_Id> EVSEIds(IncludeEVSEDelegate?  IncludeEVSEs   = null)
        {

            IncludeEVSEs ??= (evse => true);

            return evses.Where (evse => IncludeEVSEs(evse)).
                         Select(evse => evse.Id);

        }

        #endregion

        #region EVSEAdminStatus        (IncludeEVSEs = null)

        /// <summary>
        /// Return the admin status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSEAdminStatus> EVSEAdminStatus(IncludeEVSEDelegate? IncludeEVSEs = null)
        {

            IncludeEVSEs ??= (evse => true);

            return evses.Where (evse => IncludeEVSEs?.Invoke(evse) ?? true).
                         Select(evse => new EVSEAdminStatus(evse.Id,
                                                            evse.AdminStatus));

        }

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

        {

            IncludeEVSEs ??= (evse => true);

            return EVSEs.Where (evse => IncludeEVSEs(evse)).
                         Select(evse => new Tuple<EVSE_Id, IEnumerable<Timestamped<EVSEAdminStatusType>>>(
                                            evse.Id,
                                            evse.AdminStatusSchedule(TimestampFilter,
                                                                     StatusFilter,
                                                                     Skip,
                                                                     Take)));

        }

        #endregion

        #region EVSEStatus             (IncludeEVSEs = null)

        /// <summary>
        /// Return the admin status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSEStatus> EVSEStatus(IncludeEVSEDelegate? IncludeEVSEs = null)
        {

            IncludeEVSEs ??= (evse => true);

            return evses.Where (evse => IncludeEVSEs?.Invoke(evse) ?? true).
                         Select(evse => new EVSEStatus(evse.Id,
                                                       evse.Status));

        }

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

        {

            IncludeEVSEs ??= (evse => true);

            return EVSEs.Where (evse => IncludeEVSEs(evse)).
                         Select(evse => new Tuple<EVSE_Id, IEnumerable<Timestamped<EVSEStatusType>>>(
                                            evse.Id,
                                            evse.StatusSchedule(TimestampFilter,
                                                                StatusFilter,
                                                                Skip,
                                                                Take)));

        }

        #endregion


        #region (private) Connect(EVSE)

        private void Connect(EVSE EVSE)
        {

            EVSE.OnDataChanged           += UpdateEVSEData;
            EVSE.OnStatusChanged         += UpdateEVSEStatus;
            EVSE.OnAdminStatusChanged    += UpdateEVSEAdminStatus;

        }

        #endregion


        #region ContainsEVSE(EVSE)

        /// <summary>
        /// Check if the given EVSE is already present within the charging station.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        public Boolean ContainsEVSE(EVSE EVSE)

            => evses.Contains(EVSE);

        #endregion

        #region ContainsEVSE(EVSEId)

        /// <summary>
        /// Check if the given EVSE identification is already present within the charging station.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        public Boolean ContainsEVSE(EVSE_Id EVSEId)

            => evses.ContainsId(EVSEId);

        #endregion

        #region GetEVSEById(EVSEId)

        public EVSE? GetEVSEById(EVSE_Id EVSEId)

            => evses.GetById(EVSEId);

        #endregion

        #region TryGetEVSEById(EVSEId, out EVSE)

        public Boolean TryGetEVSEById(EVSE_Id EVSEId, out EVSE? EVSE)

            => evses.TryGet(EVSEId, out EVSE);

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


        #region ToJSON(this ChargingStation,                      Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation of the given charging station.
        /// </summary>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a charging pool.</param>
        public JObject ToJSON(Boolean                                              Embedded                            = false,
                              InfoStatus                                           ExpandRoamingNetworkId              = InfoStatus.ShowIdOnly,
                              InfoStatus                                           ExpandChargingStationOperatorId     = InfoStatus.ShowIdOnly,
                              InfoStatus                                           ExpandChargingPoolId                = InfoStatus.ShowIdOnly,
                              InfoStatus                                           ExpandEVSEIds                       = InfoStatus.Expanded,
                              InfoStatus                                           ExpandBrandIds                      = InfoStatus.ShowIdOnly,
                              InfoStatus                                           ExpandDataLicenses                  = InfoStatus.ShowIdOnly,
                              Boolean?                                             IncludeRemovedEVSEs                 = false,
                              Boolean?                                             IncludeCustomData                   = null,
                              CustomJObjectSerializerDelegate<ChargingStation>?   CustomChargingStationSerializer     = null,
                              CustomJObjectSerializerDelegate<EVSE>?              CustomEVSESerializer                = null,
                              CustomJObjectSerializerDelegate<ChargingConnector>?  CustomChargingConnectorSerializer   = null)
        {

            try
            {

                var json = JSONObject.Create(

                                     new JProperty("@id",           Id.         ToString()),

                               !Embedded
                                   ? new JProperty("@context",      JSONLDContext)
                                   : null,

                               Name.       IsNotNullOrEmpty()
                                   ? new JProperty("name",          Name.       ToJSON())
                                   : null,

                               Description.IsNotNullOrEmpty()
                                   ? new JProperty("description",   Description.ToJSON())
                                   : null,

                               !Embedded || DataSource   != ChargingPool?.DataSource
                                   ? new JProperty("dataSource",    DataSource)
                                   : null,

                               DataLicenses.Any()
                                   ? ExpandDataLicenses.Switch(
                                       () => new JProperty("dataLicenseIds",  new JArray(DataLicenses.SafeSelect(dataLicense => dataLicense.Id.ToString()))),
                                       () => new JProperty("dataLicenses",    DataLicenses.ToJSON()))
                                   : null,

                               ExpandRoamingNetworkId != InfoStatus.Hidden && RoamingNetwork is not null
                                   ? ExpandRoamingNetworkId.Switch(
                                         () => new JProperty("roamingNetworkId",           RoamingNetwork.Id. ToString()),
                                         () => new JProperty("roamingNetwork",             RoamingNetwork.    ToJSON(Embedded:                          true,
                                                                                                                     ExpandChargingStationOperatorIds:  InfoStatus.Hidden,
                                                                                                                     ExpandChargingPoolIds:             InfoStatus.Hidden,
                                                                                                                     ExpandChargingStationIds:          InfoStatus.Hidden,
                                                                                                                     ExpandEVSEIds:                     InfoStatus.Hidden,
                                                                                                                     ExpandBrandIds:                    ExpandBrandIds,
                                                                                                                     ExpandDataLicenses:                ExpandDataLicenses)))
                                   : null,

                               ExpandChargingStationOperatorId != InfoStatus.Hidden && Operator is not null
                                   ? ExpandChargingStationOperatorId.Switch(
                                         () => new JProperty("chargingStationOperatorId",  Operator.Id.       ToString()),
                                         () => new JProperty("chargingStationOperator",    Operator.          ToJSON(Embedded:                   true,
                                                                                                                     ExpandRoamingNetworkId:            InfoStatus.Hidden,
                                                                                                                     ExpandChargingPoolIds:             InfoStatus.Hidden,
                                                                                                                     ExpandChargingStationIds:          InfoStatus.Hidden,
                                                                                                                     ExpandEVSEIds:                     InfoStatus.Hidden,
                                                                                                                     ExpandBrandIds:                    ExpandBrandIds,
                                                                                                                     ExpandDataLicenses:                ExpandDataLicenses)))
                                   : null,

                               ExpandChargingPoolId != InfoStatus.Hidden && ChargingPool is not null
                                   ? ExpandChargingPoolId.Switch(
                                         () => new JProperty("chargingPoolId",             ChargingPool.Id.   ToString()),
                                         () => new JProperty("chargingPool",               ChargingPool.      ToJSON(Embedded:                          true,
                                                                                                                     ExpandRoamingNetworkId:            InfoStatus.Hidden,
                                                                                                                     ExpandChargingStationOperatorId:   InfoStatus.Hidden,
                                                                                                                     ExpandChargingStationIds:          InfoStatus.Hidden,
                                                                                                                     ExpandEVSEIds:                     InfoStatus.Hidden,
                                                                                                                     ExpandBrandIds:                    ExpandBrandIds,
                                                                                                                     ExpandDataLicenses:                ExpandDataLicenses)))
                                   : null,

                               GeoLocation.HasValue && (!Embedded || ChargingPool?.GeoLocation.HasValue != true ||
                                   !JToken.DeepEquals(InfrastructureJson.LocationJSON(GeoLocation.Value), InfrastructureJson.LocationJSON(ChargingPool.GeoLocation.Value)))
                                   ? new JProperty("geoLocation", InfrastructureJson.LocationJSON(GeoLocation.Value, true)) : null,
                               Address is not null && (!Embedded || Address != ChargingPool?.Address) ? new JProperty("address", Address.ToJSON(Embedded: true)) : null,
                               (!Embedded || AuthenticationModes != ChargingPool?.AuthenticationModes) ? new JProperty("authenticationModes",  AuthenticationModes.ToJSON())   : null,
                               HotlinePhoneNumber.HasValue && (!Embedded || HotlinePhoneNumber != ChargingPool?.HotlinePhoneNumber) ? new JProperty("hotlinePhoneNumber", HotlinePhoneNumber.ToString()) : null,
                               (!Embedded || OpeningTimes        != ChargingPool?.OpeningTimes)        ? new JProperty("openingTimes",         OpeningTimes.       ToJSON())   : null,

                               IsFreeOfCharge
                                   ? new JProperty("isFreeOfCharge", IsFreeOfCharge)
                                   : null,

                               !EnergyMeters.IsEmpty
                                   ? new JProperty("energyMeters", new JArray(
                                         EnergyMeters.OrderBy(meter => meter.Id.ToString(), StringComparer.Ordinal).
                                                      Select(meter => meter.ToJSON(Embedded: true))))
                                   : null,

                               ExpandEVSEIds != InfoStatus.Hidden && EVSEs.Any()
                                   ? ExpandEVSEIds.Switch(

                                         () => new JProperty("EVSEIds",
                                                             new JArray(evses.Select (evse   => evse.Id).
                                                                              OrderBy(evseId => evseId).
                                                                              Select (evseId => evseId.ToString()))),

                                         () => new JProperty("EVSEs",
                                                             new JArray(evses.Where  (evse => evse.Status.Value != EVSEStatusType.Removed).
                                                                              OrderBy(evse => evse.Id).
                                                                              Select (evse => evse.ToJSON (Embedded:                           true,
                                                                                                           ExpandRoamingNetworkId:             InfoStatus.Hidden,
                                                                                                           ExpandChargingStationOperatorId:    InfoStatus.Hidden,
                                                                                                           ExpandChargingPoolId:               InfoStatus.Hidden,
                                                                                                           ExpandChargingStationId:            InfoStatus.Hidden,
                                                                                                           ExpandBrandIds:                     ExpandBrandIds,
                                                                                                           ExpandDataLicenses:                 ExpandDataLicenses,
                                                                                                           IncludeCustomData:                  IncludeCustomData,
                                                                                                           CustomEVSESerializer:               CustomEVSESerializer,
                                                                                                           CustomChargingConnectorSerializer:  CustomChargingConnectorSerializer)))))

                                   : null,


                               ExpandBrandIds != InfoStatus.Hidden && Brands.Any()
                                   ? ExpandBrandIds.Switch(

                                         () => new JProperty("brandIds",
                                                             new JArray(Brands.Select (brand   => brand.Id).
                                                                               OrderBy(brandId => brandId).
                                                                               Select (brandId => brandId.ToString()))),

                                         () => new JProperty("brands",
                                                             new JArray(Brands.OrderBy(brand => brand.Id).
                                                                               ToJSON (Embedded:            true,
                                                                                       ExpandDataLicenses:  ExpandDataLicenses))))

                                   : null,

                               CustomData.HasValues && IncludeCustomData == true
                                   ? new JProperty("customData",            CustomData.ToJObject())
                                   : null

                         );

                return POIRepresentation.AddETags(this, CustomChargingStationSerializer is not null
                           ? CustomChargingStationSerializer(this, json)
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
                       nameof(ChargingStation),
                       Logger,
                       LogHandler,
                       EventName,
                       Command
                   );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingStation1, ChargingStation2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStation1">A charging station.</param>
        /// <param name="ChargingStation2">Another charging station.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingStation ChargingStation1,
                                           ChargingStation ChargingStation2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ChargingStation1, ChargingStation2))
                return true;

            // If one is null, but not both, return false.
            if (ChargingStation1 is null || ChargingStation2 is null)
                return false;

            return ChargingStation1.Equals(ChargingStation2);

        }

        #endregion

        #region Operator != (ChargingStation1, ChargingStation2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStation1">A charging station.</param>
        /// <param name="ChargingStation2">Another charging station.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingStation ChargingStation1,
                                           ChargingStation ChargingStation2)

            => !(ChargingStation1 == ChargingStation2);

        #endregion

        #region Operator <  (ChargingStation1, ChargingStation2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStation1">A charging station.</param>
        /// <param name="ChargingStation2">Another charging station.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingStation ChargingStation1,
                                          ChargingStation ChargingStation2)
        {

            if (ChargingStation1 is null)
                throw new ArgumentNullException(nameof(ChargingStation1), "The given ChargingStation1 must not be null!");

            return ChargingStation1.CompareTo(ChargingStation2) < 0;

        }

        #endregion

        #region Operator <= (ChargingStation1, ChargingStation2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStation1">A charging station.</param>
        /// <param name="ChargingStation2">Another charging station.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingStation ChargingStation1,
                                           ChargingStation ChargingStation2)

            => !(ChargingStation1 > ChargingStation2);

        #endregion

        #region Operator >  (ChargingStation1, ChargingStation2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStation1">A charging station.</param>
        /// <param name="ChargingStation2">Another charging station.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingStation ChargingStation1,
                                          ChargingStation ChargingStation2)
        {

            if (ChargingStation1 is null)
                throw new ArgumentNullException(nameof(ChargingStation1), "The given ChargingStation1 must not be null!");

            return ChargingStation1.CompareTo(ChargingStation2) > 0;

        }

        #endregion

        #region Operator >= (ChargingStation1, ChargingStation2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingStation1">A charging station.</param>
        /// <param name="ChargingStation2">Another charging station.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingStation ChargingStation1,
                                           ChargingStation ChargingStation2)

            => !(ChargingStation1 < ChargingStation2);

        #endregion

        #endregion

        #region IComparable<ChargingStation> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging stations.
        /// </summary>
        /// <param name="Object">A charging station to compare with.</param>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingStation chargingStation
                   ? CompareTo(chargingStation)
                   : throw new ArgumentException("The given object is not a charging station!", nameof(Object));

        #endregion

        #region CompareTo(ChargingStation)

        /// <summary>
        /// Compares two charging stations.
        /// </summary>
        /// <param name="ChargingStation">A charging station to compare with.</param>
        public Int32 CompareTo(ChargingStation? ChargingStation)

            => ChargingStation is not null
                   ? Id.CompareTo(ChargingStation.Id)
                   : throw new ArgumentException("The given object is not a ChargingStation!", nameof(ChargingStation));

        #endregion

        #endregion

        #region IEquatable<ChargingStation> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging stations for equality.
        /// </summary>
        /// <param name="Object">A charging station to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStation chargingStation &&
                   Equals(chargingStation);

        #endregion

        #region Equals(ChargingStation)

        /// <summary>
        /// Compares two charging stations for equality.
        /// </summary>
        /// <param name="ChargingStation">A charging station to compare with.</param>
        public Boolean Equals(ChargingStation? ChargingStation)

            => ChargingStation is not null &&

               Id.Equals(ChargingStation.Id);

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

            => Id.ToString();

        #endregion


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
                DebugX.LogException(e, $"ChargingStation '{Id}'.UpdateEVSEData of EVSE '{EVSE.Id}' property '{PropertyName}' from '{OldValue?.ToString() ?? "-"}' to '{NewValue?.ToString() ?? "-"}'");
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
                DebugX.LogException(e, $"ChargingStation '{Id}'.UpdateEVSEAdminStatus of EVSE '{EVSE.Id}' from '{OldAdminStatus?.ToString() ?? "-"}' to '{NewAdminStatus}'");
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
                DebugX.LogException(e, $"ChargingStation '{Id}'.UpdateEVSEStatus of EVSE '{EVSE.Id}' from '{OldStatus}' to '{NewStatus}'");
            }

        }
    }

}
