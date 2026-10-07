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
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Extension methods for the common charging pool interface.
    /// </summary>
    public static partial class ChargingPoolExtensions
    {

        #region ToJSON(this ChargingPools, Skip = null, Take = null, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given enumeration of charging pools.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="Skip">The optional number of charging pools to skip.</param>
        /// <param name="Take">The optional number of charging pools to return.</param>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a charging station operator.</param>
        public static JArray ToJSON(this IEnumerable<ChargingPool>                       ChargingPools,
                                    UInt64?                                              Skip                                = null,
                                    UInt64?                                              Take                                = null,
                                    Boolean                                              Embedded                            = false,
                                    Boolean?                                             IncludeRemoved                      = false,
                                    InfoStatus                                           ExpandRoamingNetworkId              = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandChargingStationOperatorId     = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandChargingStationIds            = InfoStatus.Expanded,
                                    InfoStatus                                           ExpandEVSEIds                       = InfoStatus.Hidden,
                                    InfoStatus                                           ExpandBrandIds                      = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandDataLicenses                  = InfoStatus.ShowIdOnly,
                                    IncludeChargingStationDelegate?                      IncludeChargingStations             = null,
                                    Boolean?                                             IncludeRemovedChargingStations      = false,
                                    Boolean?                                             IncludeCustomData                   = null,
                                    CustomJObjectSerializerDelegate<ChargingPool>?       CustomChargingPoolSerializer        = null,
                                    CustomJObjectSerializerDelegate<ChargingStation>?    CustomChargingStationSerializer     = null,
                                    CustomJObjectSerializerDelegate<EVSE>?               CustomEVSESerializer                = null,
                                    CustomJObjectSerializerDelegate<ChargingConnector>?  CustomChargingConnectorSerializer   = null)


            => ChargingPools is not null && ChargingPools.Any()

                   ? new JArray(
                         ChargingPools.
                             Where          (chargingPool => chargingPool is not null).
                             Where          (chargingPool => IncludeRemoved == true || chargingPool.Status != ChargingPoolStatusType.Removed).
                             OrderBy        (chargingPool => chargingPool.Id).
                             SkipTakeFilter (Skip, Take).
                             SafeSelect     (chargingPool => chargingPool.ToJSON(Embedded,
                                                                                 ExpandRoamingNetworkId,
                                                                                 ExpandChargingStationOperatorId,
                                                                                 ExpandChargingStationIds,
                                                                                 ExpandEVSEIds,
                                                                                 ExpandBrandIds,
                                                                                 ExpandDataLicenses,
                                                                                 IncludeChargingStations,
                                                                                 IncludeRemovedChargingStations,
                                                                                 IncludeCustomData,
                                                                                 CustomChargingPoolSerializer,
                                                                                 CustomChargingStationSerializer,
                                                                                 CustomEVSESerializer,
                                                                                 CustomChargingConnectorSerializer)).
                             Where          (chargingPool => chargingPool is not null)
                     )

                   : [];


        #endregion

    }


    /// <summary>
    /// A pool of electric vehicle charging stations.
    /// The geo locations of these charging stations will be close together and the charging pool
    /// might provide a shared network access to aggregate and optimize communication
    /// with the EVSE Operator backend.
    /// </summary>
    public sealed partial class ChargingPool : AImmutableEMobilityEntity<ChargingPool_Id,
                                                 ChargingPoolAdminStatusType,
                                                 ChargingPoolStatusType>,
                                IEquatable<ChargingPool>,
                                IComparable<ChargingPool>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const            String    JSONLDContext                                    = "https://open.charging.cloud/contexts/wwcp+json/chargingPool";


        private readonly        Decimal   EPSILON                                          = 0.01m;

        /// <summary>
        /// The default max size of the charging pool admin status list.
        /// </summary>
        public const            UInt16    DefaultMaxChargingPoolAdminStatusScheduleSize    = 15;

        /// <summary>
        /// The default max size of the charging pool (aggregated charging station) status list.
        /// </summary>
        public const            UInt16    DefaultMaxChargingPoolStatusScheduleSize         = 15;

        /// <summary>
        /// The maximum time span for a reservation.
        /// </summary>
        public static readonly  TimeSpan  DefaultMaxChargingPoolReservationDuration        = TimeSpan.FromMinutes(30);

        #endregion

        #region Properties

        /// <summary>
        /// The roaming network of this charging pool.
        /// </summary>
        [InternalUseOnly]
        public RoamingNetwork?                      RoamingNetwork
            => Operator?.RoamingNetwork;

        /// <summary>
        /// The charging station operator of this charging pool.
        /// </summary>
        [Optional]
        public ChargingStationOperator?             Operator               { get; }

        /// <summary>
        /// The charging pool sub operator of this charging pool.
        /// (Better use brands!)
        /// </summary>
        [Optional]
        public ChargingStationOperator?             SubOperator            { get; }

        /// <summary>
        /// The charging owner sub operator of this charging pool.
        /// (Better use brands!)
        /// </summary>
        [Optional]
        public ChargingStationOperator?             Owner                  { get; }

        /// <summary>
        /// All brands registered for this charging pool.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<Brand> immutableBrands = [];
        [Optional, SlowData]
        public System.Collections.Immutable.ImmutableArray<Brand> Brands
            => ImmutablePOIValues.CopyItems(immutableBrands);

        /// <summary>
        /// All e-mobility related Root-CAs, e.g. ISO 15118-2/-20 available in this charging pool.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<RootCAInfo> immutableMobilityRootCAs = [];
        [Optional, SlowData]
        public IEnumerable<RootCAInfo> MobilityRootCAs
        {
            get => ImmutablePOIValues.CopyItems(immutableMobilityRootCAs);
            private set => immutableMobilityRootCAs = ImmutablePOIValues.CopyItems(value);
        }

        /// <summary>
        /// An optional enumeration of EV roaming partners available in this charging pool.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<EVRoamingPartnerInfo> immutableEVRoamingPartners = [];
        [Optional, SlowData]
        public IEnumerable<EVRoamingPartnerInfo> EVRoamingPartners
        {
            get => ImmutablePOIValues.CopyItems(immutableEVRoamingPartners);
            private set => immutableEVRoamingPartners = ImmutablePOIValues.CopyItems(value);
        }


        #region Address related

        public IEnumerable<Languages>        LocationLanguages      { get; private set; }

        #region Address

        private Address? address;

        /// <summary>
        /// The address of this charging pool.
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
        /// The geographical location of this charging pool.
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

        #region ParkingType

        private ParkingType? parkingType;

        /// <summary>
        /// The parking type.
        /// </summary>
        [Optional]
        public ParkingType? ParkingType
        {

            get
            {
                return parkingType;
            }


        }

        #endregion

        #region TimeZone

        private Time_Zone? timeZone;

        /// <summary>
        /// The time zone of this charging pool.
        /// </summary>
        [Mandatory]
        public Time_Zone? TimeZone
        {

            get
            {
                return timeZone;
            }


        }

        #endregion

        #region OpeningTimes

        private ImmutableOpeningTimes openingTimes;

        /// <summary>
        /// The opening times of this charging pool.
        /// </summary>
        [Mandatory]
        public ImmutableOpeningTimes OpeningTimes
        {

            get
            {
                return ImmutablePOIValues.Copy(openingTimes);
            }


        }

        #endregion

        #region ChargingWhenClosed

        /// <summary>
        /// Indicates if the charging stations are still charging outside the opening hours of the charging pool.
        /// </summary>
        public Boolean?  ChargingWhenClosed    { get; private set; }

        #endregion

        #region ArrivalInstructions

        /// <summary>
        /// An optional (multi-language) description of how to find the charging pool.
        /// </summary>
        private ImmutableI18NString immutableArrivalInstructions = ImmutableI18NString.Empty;
        [Optional]
        public ImmutableI18NString ArrivalInstructions
        {
            get => ImmutablePOIValues.Copy(immutableArrivalInstructions);
            private set => immutableArrivalInstructions = ImmutablePOIValues.Copy(value);
        }

        #endregion

        #region EntranceAddress

        private Address? entranceAddress;

        /// <summary>
        /// The address of the entrance to this charging pool.
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

        private GeoCoordinate? entranceLocation;

        /// <summary>
        /// The geographical location of the entrance to this charging pool.
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

        #region ExitAddress

        private Address? exitAddress;

        /// <summary>
        /// The address of the exit of this charging pool.
        /// (If different from 'Address').
        /// </summary>
        [Optional]
        public Address? ExitAddress
        {

            get
            {
                return ImmutablePOIValues.Copy(exitAddress);
            }


        }

        #endregion

        #region ExitLocation

        private GeoCoordinate? exitLocation;

        /// <summary>
        /// The geographical location of the exit of this charging pool.
        /// (If different from 'GeoLocation').
        /// </summary>
        [Optional]
        public GeoCoordinate? ExitLocation
        {

            get
            {
                return exitLocation;
            }


        }

        #endregion

        #endregion


        /// <summary>
        /// The optional enumeration of services that are offered around the location
        /// of the charging pool by the CPO or their affiliated partners.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<LocationService> immutableServices = [];
        [Optional]
        public IEnumerable<LocationService> Services
        {
            get => ImmutablePOIValues.CopyItems(immutableServices);
            private set => immutableServices = ImmutablePOIValues.CopyItems(value);
        }

        /// <summary>
        /// The optional enumeration of additional geographical locations of related
        /// geo coordinates that might be relevant to the EV driver.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<AdditionalGeoLocation> immutableRelatedLocations = [];
        [Optional]
        public IEnumerable<AdditionalGeoLocation> RelatedLocations
        {
            get => ImmutablePOIValues.CopyItems(immutableRelatedLocations);
            private set => immutableRelatedLocations = ImmutablePOIValues.CopyItems(value);
        }

        /// <summary>
        /// The optional enumeration of restrictions that apply to parking within the charging pool.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<ParkingRestrictionGroup> immutableParkingRestrictions = [];
        [Optional]
        public IEnumerable<ParkingRestrictionGroup> ParkingRestrictions
        {
            get => ImmutablePOIValues.CopyItems(immutableParkingRestrictions);
            private set => immutableParkingRestrictions = ImmutablePOIValues.CopyItems(value);
        }


        #region Accessibility

        private AccessibilityType? accessibility;

        /// <summary>
        /// The accessibility of the charging pool.
        /// </summary>
        [Optional]
        public AccessibilityType? Accessibility
        {

            get
            {
                return accessibility;
            }


        }

        #endregion

        #region Features

        /// <summary>
        /// Charging features of the charging pool, when those features
        /// are not features of the charging stations, e.g. hasARoof.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<ChargingPoolFeature> immutableFeatures = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<ChargingPoolFeature> Features
            => ImmutablePOIValues.CopyItems(immutableFeatures);

        #endregion

        #region UIFeatures

        /// <summary>
        /// User interface features of the charging pool, when those features
        /// are not features of the charging stations, e.g. an external payment terminal.
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
        [Optional]
        public System.Collections.Immutable.ImmutableArray<AuthenticationModes> AuthenticationModes
            => ImmutablePOIValues.CopyItems(immutableAuthenticationModes);

        #endregion

        #region PaymentOptions

        /// <summary>
        /// The payment options an EV driver can use.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<PaymentOptions> immutablePaymentOptions = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<PaymentOptions> PaymentOptions
            => ImmutablePOIValues.CopyItems(immutablePaymentOptions);

        #endregion

        #region Facilities

        /// <summary>
        /// Charging facilities of the charging pool, e.g. a supermarket.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<Facility> immutableFacilities = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<Facility> Facilities
            => ImmutablePOIValues.CopyItems(immutableFacilities);

        #endregion

        #region PhotoURLs

        /// <summary>
        /// URIs of photos of this charging pool.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<URL> immutablePhotoURLs = [];
        [Optional]
        public System.Collections.Immutable.ImmutableArray<URL> PhotoURLs
            => ImmutablePOIValues.CopyItems(immutablePhotoURLs);

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


        #region Energy related

        #region GridConnection

        private GridConnectionTypes? gridConnection;

        /// <summary>
        /// The grid connection of the charging pool.
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
                return ImmutablePOIValues.Copy(energyMix);
            }


        }

        #endregion

        #region EnergyMixRealTime

        private Timestamped<EnergyMix>? energyMixRealTime;

        /// <summary>
        /// The current energy mix.
        /// </summary>
        [Mandatory, FastData]
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
                return energyMixPrognoses;
            }

            set
            {

                if (value != energyMixPrognoses)
                {

                    if (value is null)
                        DeleteProperty(ref energyMixPrognoses);

                    else
                        SetProperty(ref energyMixPrognoses, value);

                }

            }

        }

        #endregion

        #endregion


        /// <summary>
        /// The data license(s) of the charging pool data.
        /// </summary>
        private System.Collections.Immutable.ImmutableArray<DataLicense> immutableDataLicenses = [];
        [Mandatory, SlowData]
        public IEnumerable<DataLicense> DataLicenses
        {
            get => ImmutablePOIValues.CopyItems(immutableDataLicenses);
            private set => immutableDataLicenses = ImmutablePOIValues.CopyItems(value);
        }


        #region StatusAggregationDelegate

        /// <summary>
        /// A delegate called to aggregate the dynamic status of all subordinated charging stations.
        /// </summary>
        public Func<ChargingStationStatusReport, ChargingPoolStatusType>? StatusAggregationDelegate { get; set; }

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new group/pool of charging stations having the given identification.
        /// </summary>
        /// <param name="Id">The unique identification of the charging pool.</param>
        /// <param name="Operator">The parent charging station operator.</param>
        /// <param name="EnergyMeters">Optional directly owned energy meters with unique IDs within this pool.</param>
        /// <param name="GridConnectionPoint">The optional connection to the electricity grid.</param>
        ///
        /// <param name="InitialAdminStatus">An optional initial admin status of the EVSE.</param>
        /// <param name="InitialStatus">An optional initial status of the EVSE.</param>
        /// <param name="MaxPoolStatusScheduleSize">The default size of the charging pool (aggregated charging station) status list.</param>
        /// <param name="MaxPoolAdminStatusScheduleSize">The default size of the charging pool admin status list.</param>
        ///
        /// <param name="Configurator">A delegate to configure the newly created charging station.</param>
        /// <param name="RemoteChargingPoolCreator">A delegate to attach a remote charging pool.</param>
        public ChargingPool(ChargingPool_Id                            Id,
                            ChargingStationOperator                   Operator,
                            I18NString?                                Name                             = null,
                            I18NString?                                Description                      = null,

                            Address?                                   Address                          = null,
                            GeoCoordinate?                             GeoLocation                      = null,
                            Time_Zone?                                 TimeZone                         = null,
                            OpeningTimes?                              OpeningTimes                     = null,
                            Boolean?                                   ChargingWhenClosed               = null,
                            ParkingType?                               ParkingType                      = null,
                            AccessibilityType?                         Accessibility                    = null,
                            IEnumerable<Languages>?                    LocationLanguages                = null,
                            PhoneNumber?                               HotlinePhoneNumber               = null,

                            IEnumerable<Facility>?                     Facilities                       = null,
                            IEnumerable<LocationService>?              Services                         = null,
                            IEnumerable<AdditionalGeoLocation>?        RelatedLocations                 = null,

                            IEnumerable<Brand>?                        Brands                           = null,
                            IEnumerable<RootCAInfo>?                   MobilityRootCAs                  = null,
                            IEnumerable<EVRoamingPartnerInfo>?         EVRoamingPartners                = null,

                            IEnumerable<ChargingStation>?              ChargingStations                 = null,
                            IEnumerable<EnergyMeter>?                  EnergyMeters                     = null,

                            Timestamped<ChargingPoolAdminStatusType>?  InitialAdminStatus               = null,
                            Timestamped<ChargingPoolStatusType>?       InitialStatus                    = null,
                            UInt16?                                    MaxPoolAdminStatusScheduleSize   = null,
                            UInt16?                                    MaxPoolStatusScheduleSize        = null,

                            String?                                    DataSource                       = null,
                            DateTimeOffset?                            Created                          = null,
                            DateTimeOffset?                            LastChange                       = null,

                            CustomDataNew?                             CustomData                       = null,
                            UserDefinedDictionary?                     InternalData                     = null,
                            IEnumerable<DataLicense>?                  DataLicenses                     = null,
                            GridConnectionPoint?                       GridConnectionPoint              = null)

            : base(Id,
                   Name,
                   Description,
                   InitialAdminStatus             ?? ChargingPoolAdminStatusType.Operational,
                   InitialStatus                  ?? ChargingPoolStatusType.Available,
                   MaxPoolAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxPoolStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            #region Init data and properties

            this.address                           = ImmutablePOIValues.Copy(Address);
            this.geoLocation                       = GeoLocation;
            this.timeZone                          = TimeZone;
            this.openingTimes                      = ImmutablePOIValues.Copy(OpeningTimes ?? OpeningTimes.Open24Hours);
            this.ChargingWhenClosed                = ChargingWhenClosed;
            this.parkingType                       = ParkingType;
            this.accessibility                     = Accessibility;
            this.LocationLanguages                 = System.Collections.Immutable.ImmutableArray.CreateRange(LocationLanguages?.Distinct() ?? []);
            this.hotlinePhoneNumber                = HotlinePhoneNumber;

            this.Operator                          = Operator;

            this.Services                          = Services?.         Distinct() ?? [];
            this.RelatedLocations                  = RelatedLocations?. Distinct() ?? [];
            this.MobilityRootCAs                   = MobilityRootCAs?.  Distinct() ?? [];
            this.EVRoamingPartners                 = EVRoamingPartners?.Distinct() ?? [];
            this.DataLicenses                      = DataLicenses?.     Distinct() ?? [];

            this.immutableBrands                            = [];

            if (Brands is not null)
                foreach (var brand in Brands)
                    this.immutableBrands = this.immutableBrands.Add(ImmutablePOIValues.Copy(brand));


            this.immutableUIFeatures                         = [];


            this.immutableAuthenticationModes                = [];


            this.immutablePaymentOptions                     = [];


            this.immutableFeatures                           = [];


            this.immutableFacilities                         = ImmutablePOIValues.CopyItems(Facilities);


            this.immutablePhotoURLs                          = [];


            this.MaxCurrentPrognoses                = new ReactiveSet<Timestamped<Decimal>>();
            this.MaxCurrentPrognoses.OnSetChanged  += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxCurrentPrognoses",
                                oldItems,
                                newItems);

            };

            this.MaxPowerPrognoses                  = new ReactiveSet<Timestamped<Decimal>>();
            this.MaxPowerPrognoses.OnSetChanged    += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxPowerPrognoses",
                                oldItems,
                                newItems);

            };

            this.MaxCapacityPrognoses               = new ReactiveSet<Timestamped<Decimal>>();
            this.MaxCapacityPrognoses.OnSetChanged += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxCapacityPrognoses",
                                oldItems,
                                newItems);

            };

            this.hotlinePhoneNumber  = HotlinePhoneNumber;

            this.ArrivalInstructions = ArrivalInstructions ?? I18NString.Empty;


            this.chargingStations            = new EntityHashSet<ChargingPool, ChargingStation_Id, ChargingStation>(this);

            foreach (var chargingStation in ChargingStations ?? [])
            {
                if (chargingStation.ChargingPool is not null && chargingStation.ChargingPool.Id != Id)
                    throw new ArgumentException("A supplied station belongs to a different pool.", nameof(ChargingStations));
                if (this.chargingStations.TryAdd(chargingStation.CloneToParent(this), Connect).Result != CommandResult.Success)
                    throw new ArgumentException("Duplicate charging station identifier.", nameof(ChargingStations));
            }

            //this.evses.OnSetChanged               += (timestamp, reactiveSet, newItems, oldItems) =>
            //{

            //    PropertyChanged("ChargingStations",
            //                    oldItems,
            //                    newItems);

            //};

            this.EnergyMeters = ImmutablePOIValues.CopyEnergyMeters(EnergyMeters);
            if (GridConnectionPoint is not null && RoamingNetwork is { } network &&
                GridConnectionPoint.GridOperator.RoamingNetwork.Id != network.Id)
                throw new ArgumentException("gridConnectionPoint: grid operator belongs to a different roaming network.", nameof(GridConnectionPoint));
            this.GridConnectionPoint = GridConnectionPoint?.Clone(RoamingNetwork);

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


        #region Data/(Admin-)Status management

        #region OnData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data changed.
        /// </summary>
        public event OnChargingPoolDataChangedDelegate?         OnDataChanged;

        /// <summary>
        /// An event fired whenever the aggregated dynamic status changed.
        /// </summary>
        public event OnChargingPoolStatusChangedDelegate?       OnStatusChanged;

        /// <summary>
        /// An event fired whenever the aggregated dynamic status changed.
        /// </summary>
        public event OnChargingPoolAdminStatusChangedDelegate?  OnAdminStatusChanged;

        #endregion


        #region (internal) UpdateData        (Timestamp, EventTrackingId, Sender, PropertyName, OldValue, NewValue)

        /// <summary>
        /// Update the static data.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="Sender">The changed charging pool.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        /// <param name="OldValue">The optional old value of the changed property.</param>
        /// <param name="DataSource">An optional data source or context for the charging pool data update.</param>
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
                                    Sender as ChargingPool,
                                    PropertyName,
                                    NewValue,
                                    OldValue,
                                    DataSource);

        }

        #endregion

        #region (internal) UpdateAdminStatus (Timestamp, EventTrackingId, OldStatus, NewStatus)

        /// <summary>
        /// Update the current admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="NewStatus">The new charging station admin status.</param>
        /// <param name="OldStatus">The optional old charging station admin status.</param>
        /// <param name="DataSource">An optional data source or context for the charging pool admin status update.</param>
        internal async Task UpdateAdminStatus(DateTimeOffset                             Timestamp,
                                              EventTracking_Id                           EventTrackingId,
                                              Timestamped<ChargingPoolAdminStatusType>   NewStatus,
                                              Timestamped<ChargingPoolAdminStatusType>?  OldStatus    = null,
                                              Context?                                   DataSource   = null)
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

        #region (internal) UpdateStatus      (Timestamp, EventTrackingId, OldStatus, NewStatus)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        /// <param name="OldStatus">The optional old EVSE status.</param>
        /// <param name="DataSource">An optional data source or context for the charging pool admin status update.</param>
        internal async Task UpdateStatus(DateTimeOffset                        Timestamp,
                                         EventTracking_Id                      EventTrackingId,
                                         Timestamped<ChargingPoolStatusType>   NewStatus,
                                         Timestamped<ChargingPoolStatusType>?  OldStatus    = null,
                                         Context?                              DataSource   = null)
        {

            var onStatusChanged = OnStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp,
                                      EventTrackingId,
                                      this,
                                      NewStatus,
                                      OldStatus,
                                      DataSource);

        }

        #endregion

        #endregion

        #region Charging stations

        #region ChargingStations

        private readonly EntityHashSet<ChargingPool, ChargingStation_Id, ChargingStation> chargingStations;

        /// <summary>
        /// Return all charging stations registered within this charging pool.
        /// </summary>
        public IEnumerable<ChargingStation> ChargingStations
        {
            get
            {
                lock (chargingStations)
                {
                    return chargingStations.ToArray();
                }
            }
        }

        #endregion

        #region ChargingStationIds        (IncludeStations = null)

        /// <summary>
        /// Return an enumeration of all charging station identifications.
        /// </summary>
        /// <param name="IncludeStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStation_Id> ChargingStationIds(IncludeChargingStationDelegate? IncludeStations = null)

            => IncludeStations is null

                   ? ChargingStations.
                         Select    (station => station.Id)

                   : ChargingStations.
                         Where     (station => IncludeStations(station)).
                         Select    (station => station.Id);

        #endregion

        #region ChargingStationAdminStatus(IncludeStations = null)

        /// <summary>
        /// Return an enumeration of all charging station admin status.
        /// </summary>
        /// <param name="IncludeStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStationAdminStatus> ChargingStationAdminStatus(IncludeChargingStationDelegate? IncludeStations = null)

            => IncludeStations is null

                   ? ChargingStations.
                         Select    (station => new ChargingStationAdminStatus(station.Id, station.AdminStatus))

                   : ChargingStations.
                         Where     (station => IncludeStations(station)).
                         Select    (station => new ChargingStationAdminStatus(station.Id, station.AdminStatus));

        #endregion

        #region ChargingStationStatus     (IncludeStations = null)

        /// <summary>
        /// Return an enumeration of all charging station status.
        /// </summary>
        /// <param name="IncludeStations">An optional delegate for filtering charging stations.</param>
        public IEnumerable<ChargingStationStatus> ChargingStationStatus(IncludeChargingStationDelegate? IncludeStations = null)

            => IncludeStations is null

                   ? ChargingStations.
                         Select    (station => new ChargingStationStatus(station.Id, station.Status))

                   : ChargingStations.
                         Where     (station => IncludeStations(station)).
                         Select    (station => new ChargingStationStatus(station.Id, station.Status));

        #endregion


        #region ChargingStationAddition

        /// <summary>
        /// Called whenever a charging station will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingPool, ChargingStation, Boolean> OnChargingStationAddition

            => chargingStations.OnAddition;

        #endregion

        #region ChargingStationUpdate

        /// <summary>
        /// Called whenever a charging station will be or was updated.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingPool, ChargingStation, ChargingStation, Boolean> OnChargingStationUpdate

            => chargingStations.OnUpdate;

        #endregion

        #region ChargingStationRemoval

        /// <summary>
        /// Called whenever a charging station will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, User_Id, ChargingPool, ChargingStation, Boolean> OnChargingStationRemoval

            => chargingStations.OnRemoval;

        #endregion


        #region (private) Connect(ChargingStation)

        private void Connect(ChargingStation ChargingStation)
        {

            ChargingStation.OnDataChanged                  += UpdateChargingStationData;
            ChargingStation.OnStatusChanged                += UpdateChargingStationStatus;
            ChargingStation.OnAdminStatusChanged           += UpdateChargingStationAdminStatus;

            ChargingStation.OnEVSEDataChanged              += UpdateEVSEData;
            ChargingStation.OnEVSEStatusChanged            += UpdateEVSEStatus;
            ChargingStation.OnEVSEAdminStatusChanged       += UpdateEVSEAdminStatus;

        }

        #endregion


        #region ContainsChargingStation  (ChargingStation)

        /// <summary>
        /// Check if the given ChargingStation is already present within the charging pool.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        public Boolean ContainsChargingStation(ChargingStation ChargingStation)

            => chargingStations.ContainsId(ChargingStation.Id);

        #endregion

        #region ContainsChargingStation  (ChargingStationId)

        /// <summary>
        /// Check if the given ChargingStation identification is already present within the charging pool.
        /// </summary>
        /// <param name="ChargingStationId">The unique identification of the charging station.</param>
        public Boolean ContainsChargingStation(ChargingStation_Id ChargingStationId)

            => chargingStations.ContainsId(ChargingStationId);

        #endregion

        #region GetChargingStationById   (ChargingStationId)

        public ChargingStation? GetChargingStationById(ChargingStation_Id ChargingStationId)

            => chargingStations.GetById(ChargingStationId);

        #endregion

        #region TryGetChargingStationById(ChargingStationId, out ChargingStation)

        public Boolean TryGetChargingStationById(ChargingStation_Id ChargingStationId, out ChargingStation? ChargingStation)

            => chargingStations.TryGet(ChargingStationId, out ChargingStation);

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


        #region TryGetChargingStationByEVSEId(EVSEId, out Station)

        public Boolean TryGetChargingStationByEVSEId(EVSE_Id EVSEId, out ChargingStation? Station)
        {

            foreach (var station in chargingStations)
            {

                if (station.TryGetEVSEById(EVSEId, out var evse))
                {
                    Station = station;
                    return true;
                }

            }

            Station = null;
            return false;

        }

        #endregion

        #endregion


        #region EVSEs

        #region EVSEs

        /// <summary>
        /// All Electric Vehicle Supply Equipments (EVSE) present
        /// within this charging pool.
        /// </summary>
        public IEnumerable<EVSE> EVSEs
        {
            get
            {
                lock (chargingStations)
                {

                    return chargingStations.SelectMany(station => station.EVSEs).
                                            ToArray();

                }
            }
        }

        #endregion

        #region EVSEIds                (IncludeEVSEs = null)

        /// <summary>
        /// The unique identifications of all Electric Vehicle Supply Equipment
        /// (EVSEs) present within this charging pool.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSE_Id> EVSEIds(IncludeEVSEDelegate? IncludeEVSEs = null)

            => IncludeEVSEs is null

                   ? chargingStations.
                         SelectMany(station => station.EVSEs).
                         Select    (evse    => evse.Id)

                   : chargingStations.
                         SelectMany(station => station.EVSEs).
                         Where     (evse    => IncludeEVSEs(evse)).
                         Select    (evse    => evse.Id);

        #endregion

        #region EVSEAdminStatus        (IncludeEVSEs = null)

        /// <summary>
        /// Return the admin status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSEAdminStatus> EVSEAdminStatus(IncludeEVSEDelegate IncludeEVSEs = null)

            => chargingStations.
                   SelectMany(station => station.EVSEAdminStatus(IncludeEVSEs));

        #endregion

        #region EVSEAdminStatusSchedule(IncludeEVSEs = null)

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

            => chargingStations.
                   SelectMany(station => station.EVSEAdminStatusSchedule(IncludeEVSEs,
                                                                         TimestampFilter,
                                                                         StatusFilter,
                                                                         Skip,
                                                                         Take));

        #endregion

        #region EVSEStatus             (IncludeEVSEs = null)

        /// <summary>
        /// Return the admin status of all EVSEs registered within this roaming network.
        /// </summary>
        /// <param name="IncludeEVSEs">An optional delegate for filtering EVSEs.</param>
        public IEnumerable<EVSEStatus> EVSEStatus(IncludeEVSEDelegate IncludeEVSEs = null)

            => chargingStations.
                   SelectMany(station => station.EVSEStatus(IncludeEVSEs));

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

            => chargingStations.
                   SelectMany(station => station.EVSEStatusSchedule(IncludeEVSEs,
                                                                    TimestampFilter,
                                                                    StatusFilter,
                                                                    Skip,
                                                                    Take));

        #endregion


        #region ContainsEVSE(EVSE)

        /// <summary>
        /// Check if the given EVSE is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        public Boolean ContainsEVSE(EVSE EVSE)

            => chargingStations.Any(ChargingStation => ChargingStation.EVSEIds().Contains(EVSE.Id));

        #endregion

        #region ContainsEVSE(EVSEId)

        /// <summary>
        /// Check if the given EVSE identification is already present within the Charging Station Operator.
        /// </summary>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        public Boolean ContainsEVSE(EVSE_Id EVSEId)

            => chargingStations.Any(ChargingStation => ChargingStation.EVSEIds().Contains(EVSEId));

        #endregion

        #region GetEVSEById(EVSEId)

        public EVSE GetEVSEById(EVSE_Id EVSEId)

            => chargingStations.
                   SelectMany    (station => station.EVSEs).
                   FirstOrDefault(EVSE    => EVSE.Id == EVSEId);

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

        //#region SocketOutletAddition

        //internal readonly IVotingNotificator<DateTime, EVSE, SocketOutlet, Boolean> SocketOutletAddition;

        ///// <summary>
        ///// Called whenever a socket outlet will be or was added.
        ///// </summary>
        //public IVotingSender<DateTimeOffset, EVSE, SocketOutlet, Boolean> OnSocketOutletAddition

        //    => SocketOutletAddition;

        //#endregion

        //#region SocketOutletRemoval

        //internal readonly IVotingNotificator<DateTime, EVSE, SocketOutlet, Boolean> SocketOutletRemoval;

        ///// <summary>
        ///// Called whenever a socket outlet will be or was removed.
        ///// </summary>
        //public IVotingSender<DateTimeOffset, EVSE, SocketOutlet, Boolean> OnSocketOutletRemoval

        //    => SocketOutletRemoval;

        //#endregion


        #region TryGetEVSEById(EVSEId, out EVSE)

        public Boolean TryGetEVSEById(EVSE_Id EVSEId, out EVSE? EVSE)
        {

            EVSE = ChargingStations.
                       SelectMany    (station => station.EVSEs).
                       FirstOrDefault(evse    => evse.Id == EVSEId);

            return EVSE is not null;

        }

        #endregion


        #endregion

        #region Energy Meters (outside charging stations and EVSEs!)

        #region EnergyMeters

        /// <summary>
        /// The directly owned energy meters, separate from station, EVSE and connection-point meters.
        /// Membership and POI data are immutable; each meter retains mutable runtime statuses.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<EnergyMeter> EnergyMeters { get; }

        /// <summary>
        /// The optional electricity-grid connection of this pool.
        /// </summary>
        public GridConnectionPoint? GridConnectionPoint { get; }

        #endregion


        #endregion


        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation of the given charging pool.
        /// </summary>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a charging station operator.</param>
        public JObject ToJSON(Boolean                                              Embedded                            = false,
                              InfoStatus                                           ExpandRoamingNetworkId              = InfoStatus.ShowIdOnly,
                              InfoStatus                                           ExpandChargingStationOperatorId     = InfoStatus.ShowIdOnly,
                              InfoStatus                                           ExpandChargingStationIds            = InfoStatus.Expanded,
                              InfoStatus                                           ExpandEVSEIds                       = InfoStatus.Hidden,
                              InfoStatus                                           ExpandBrandIds                      = InfoStatus.ShowIdOnly,
                              InfoStatus                                           ExpandDataLicenses                  = InfoStatus.ShowIdOnly,
                              IncludeChargingStationDelegate?                      IncludeChargingStations             = null,
                              Boolean?                                             IncludeRemovedChargingStations      = false,
                              Boolean?                                             IncludeCustomData                   = null,
                              CustomJObjectSerializerDelegate<ChargingPool>?      CustomChargingPoolSerializer        = null,
                              CustomJObjectSerializerDelegate<ChargingStation>?   CustomChargingStationSerializer     = null,
                              CustomJObjectSerializerDelegate<EVSE>?              CustomEVSESerializer                = null,
                              CustomJObjectSerializerDelegate<ChargingConnector>?  CustomChargingConnectorSerializer   = null)
        {

            IncludeChargingStations ??= station => true;

            try
            {

                var json = JSONObject.Create(

                                     new JProperty("@id",          Id.ToString()),

                               !Embedded
                                   ? new JProperty("@context",     JSONLDContext)
                                   : null,

                               Name.       IsNotNullOrEmpty()
                                   ? new JProperty("name",         Name.ToJSON())
                                   : null,

                               Description.IsNotNullOrEmpty()
                                   ? new JProperty("description",  Description.ToJSON())
                                   : null,

                               ((!Embedded || DataSource != Operator?.DataSource) && DataSource is not null)
                                   ? new JProperty("dataSource",   DataSource)
                                   : null,

                               DataLicenses.Any()
                                   ? ExpandDataLicenses.Switch(
                                         () => new JProperty("dataLicenseIds",  new JArray(DataLicenses.SafeSelect(dataLicense => dataLicense.Id.ToString()))),
                                         () => new JProperty("dataLicenses",    DataLicenses.ToJSON())
                                     )
                                   : null,

                               ExpandRoamingNetworkId != InfoStatus.Hidden && RoamingNetwork is not null
                                   ? ExpandRoamingNetworkId.Switch(
                                         () => new JProperty("roamingNetworkId",           RoamingNetwork.Id.ToString()),
                                         () => new JProperty("roamingNetwork",             RoamingNetwork.   ToJSON(Embedded:                          true,
                                                                                                                    ExpandChargingStationOperatorIds:  InfoStatus.Hidden,
                                                                                                                    ExpandChargingPoolIds:             InfoStatus.Hidden,
                                                                                                                    ExpandChargingStationIds:          InfoStatus.Hidden,
                                                                                                                    ExpandEVSEIds:                     InfoStatus.Hidden,
                                                                                                                    ExpandBrandIds:                    ExpandBrandIds,
                                                                                                                    ExpandDataLicenses:                ExpandDataLicenses)))
                                   : null,

                               ExpandChargingStationOperatorId != InfoStatus.Hidden && Operator is not null
                                   ? ExpandChargingStationOperatorId.Switch(
                                         () => new JProperty("chargingStationOperatorId",  Operator.Id.      ToString()),
                                         () => new JProperty("chargingStationOperator",    Operator.         ToJSON(Embedded:                          true,
                                                                                                                    ExpandRoamingNetworkId:            InfoStatus.Hidden,
                                                                                                                    ExpandChargingPoolIds:             InfoStatus.Hidden,
                                                                                                                    ExpandChargingStationIds:          InfoStatus.Hidden,
                                                                                                                    ExpandEVSEIds:                     InfoStatus.Hidden,
                                                                                                                    ExpandBrandIds:                    ExpandBrandIds,
                                                                                                                    ExpandDataLicenses:                ExpandDataLicenses)))
                                   : null,

                               GeoLocation.HasValue
                                   ? new JProperty("geoLocation",          InfrastructureJson.LocationJSON(GeoLocation.Value))
                                   : null,

                               Address is not null
                                   ? new JProperty("address",              Address.            ToJSON(Embedded: true))
                                   : null,

                               !EnergyMeters.IsEmpty
                                   ? new JProperty("energyMeters", new JArray(
                                         EnergyMeters.OrderBy(meter => meter.Id.ToString(), StringComparer.Ordinal).
                                                      Select(meter => meter.ToJSON(Embedded: true))))
                                   : null,

                               GridConnectionPoint is not null
                                   ? new JProperty("gridConnectionPoint", GridConnectionPoint.ToJSON(Embedded: true))
                                   : null,

                               ParkingType.IsNotNullOrEmpty()
                                   ? new JProperty("locationType",         ParkingType.ToString())
                                   : null,

                               Accessibility.HasValue
                                   ? new JProperty("accessibility",        Accessibility.      ToString())
                                   : null,

                               AuthenticationModes.Any()
                                   ? new JProperty("authenticationModes",  AuthenticationModes.ToJSON())
                                   : null,

                               HotlinePhoneNumber.HasValue
                                   ? new JProperty("hotlinePhoneNumber",   HotlinePhoneNumber. ToString())
                                   : null,

                               OpeningTimes is not null
                                   ? new JProperty("openingTimes",         OpeningTimes.       ToJSON())
                                   : null,


                               ExpandChargingStationIds != InfoStatus.Hidden && ChargingStations.Any()
                                   ? ExpandChargingStationIds.Switch(

                                         () => new JProperty("chargingStationIds",  ChargingStations.
                                                                                                 Where  (chargingStation => IncludeChargingStations(chargingStation)).
                                                                                                 OrderBy(chargingStation => chargingStation).
                                                                                                 Select (chargingStation => chargingStation.Id.ToString())),

                                         () => new JProperty("chargingStations",    ChargingStations.
                                                                                                 Where  (chargingStation => IncludeChargingStations(chargingStation)).
                                                                                                 OrderBy(chargingStation => chargingStation.Id).
                                                                                                 ToJSON (Embedded:                           true,
                                                                                                         IncludeRemoved:                     IncludeRemovedChargingStations,
                                                                                                         ExpandRoamingNetworkId:             InfoStatus.Hidden,
                                                                                                         ExpandChargingStationOperatorId:    InfoStatus.Hidden,
                                                                                                         ExpandChargingPoolId:               InfoStatus.Hidden,
                                                                                                         ExpandEVSEIds:                      ExpandEVSEIds,
                                                                                                         ExpandBrandIds:                     ExpandBrandIds,
                                                                                                         ExpandDataLicenses:                 ExpandDataLicenses,
                                                                                                         IncludeRemovedEVSEs:                IncludeRemovedChargingStations, // just for equivalent behavior!
                                                                                                         IncludeCustomData:                  IncludeCustomData,
                                                                                                         CustomChargingStationSerializer:    CustomChargingStationSerializer,
                                                                                                         CustomEVSESerializer:               CustomEVSESerializer,
                                                                                                         CustomChargingConnectorSerializer:  CustomChargingConnectorSerializer)))

                                   : null,


                               ExpandChargingStationIds != InfoStatus.Expanded && ExpandEVSEIds != InfoStatus.Hidden && EVSEs.Any()
                                   ? ExpandEVSEIds.Switch(

                                         () => new JProperty("EVSEIds",
                                                             new JArray(EVSEIds().
                                                                                                 OrderBy(evseId => evseId).
                                                                                                 Select (evseId => evseId.ToString()))),

                                         () => new JProperty("EVSEs",
                                                             new JArray(EVSEs.
                                                                                                 OrderBy(evse   => evse).
                                                                                                 ToJSON (Embedded:                         true,
                                                                                                         IncludeRemoved:                   false,
                                                                                                         ExpandRoamingNetworkId:           InfoStatus.Hidden,
                                                                                                         ExpandChargingStationOperatorId:  InfoStatus.Hidden,
                                                                                                         ExpandChargingPoolId:             InfoStatus.Hidden,
                                                                                                         ExpandChargingStationId:          InfoStatus.Hidden,
                                                                                                         ExpandBrandIds:                   ExpandBrandIds,
                                                                                                         ExpandDataLicenses:               ExpandDataLicenses,
                                                                                                         IncludeCustomData:                IncludeCustomData,
                                                                                                         CustomEVSESerializer:             CustomEVSESerializer))))

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

                return POIRepresentation.AddETags(this, CustomChargingPoolSerializer is not null
                           ? CustomChargingPoolSerializer(this, json)
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

        #region Clone()

        /// <summary>
        /// Clone this charging pool.
        /// </summary>
        public ChargingPool Clone()

        {

            var clone = new ChargingPool(

                            Id.                 Clone(),
                            Operator,
                            Name.               Clone(),
                            Description.        Clone(),

                            Address?.           Clone(),
                            GeoLocation?.       Clone(),
                            TimeZone?.          Clone(),
                            OpeningTimes,
                            ChargingWhenClosed,
                            ParkingType?.       Clone(),
                            Accessibility?.     Clone(),
                            LocationLanguages,
                            HotlinePhoneNumber?.Clone(),

                            [.. Facilities],
                            Services.           Select(service          => service.         Clone()),
                            RelatedLocations.   Select(relatedLocation  => relatedLocation. Clone()),

                            Brands,
                            MobilityRootCAs.    Select(mobilityRootCA   => mobilityRootCA.  Clone()),
                            EVRoamingPartners.  Select(evRoamingPartner => evRoamingPartner.Clone()),

                            chargingStations,
                            EnergyMeters,

                            AdminStatus,
                            Status,
                            adminStatusSchedule.MaxStatusHistorySize,
                            statusSchedule.     MaxStatusHistorySize,

                            DataSource?.        CloneString(),
                            Created,
                            LastChangeDate,

                            CustomData,
                            InternalData,
                            GridConnectionPoint: GridConnectionPoint

                        );

            clone.entranceAddress = ImmutablePOIValues.Copy(entranceAddress);
            clone.entranceLocation = entranceLocation;
            clone.exitAddress = ImmutablePOIValues.Copy(exitAddress);
            clone.exitLocation = exitLocation;
            clone.immutableArrivalInstructions = immutableArrivalInstructions;
            clone.immutableParkingRestrictions = immutableParkingRestrictions;
            clone.immutableUIFeatures = immutableUIFeatures;
            clone.immutableAuthenticationModes = ImmutablePOIValues.CopyItems(immutableAuthenticationModes);
            clone.immutablePaymentOptions = immutablePaymentOptions;
            clone.immutableFeatures = immutableFeatures;
            clone.immutablePhotoURLs = immutablePhotoURLs;
            clone.immutableDataLicenses = ImmutablePOIValues.CopyItems(immutableDataLicenses);
            clone.gridConnection = gridConnection;
            clone.maxCurrent = maxCurrent;
            clone.maxPower = maxPower;
            clone.maxCapacity = maxCapacity;
            clone.energyMix = ImmutablePOIValues.Copy(energyMix);
            clone.StatusAggregationDelegate = StatusAggregationDelegate;
            clone.MaxCurrentRealTime = MaxCurrentRealTime;
            clone.MaxPowerRealTime = MaxPowerRealTime;
            clone.MaxCapacityRealTime = MaxCapacityRealTime;
            clone.EnergyMixRealTime = ImmutablePOIValues.Copy(EnergyMixRealTime);
            clone.EnergyMixPrognoses = ImmutablePOIValues.Copy(energyMixPrognoses);
            clone.MaxCurrentPrognoses.Replace(MaxCurrentPrognoses);
            clone.MaxPowerPrognoses.Replace(MaxPowerPrognoses);
            clone.MaxCapacityPrognoses.Replace(MaxCapacityPrognoses);
            clone.SetAdminStatus(AdminStatusSchedule());
            clone.SetStatus(StatusSchedule());

            foreach (var handler in OnDataChanged?.       GetInvocationList() ?? [])
            {

                if (handler.Target == Operator)
                    continue;

                clone.OnDataChanged        += (OnChargingPoolDataChangedDelegate)        handler;

            }

            foreach (var handler in OnStatusChanged?.     GetInvocationList() ?? [])
            {
                clone.OnStatusChanged      += (OnChargingPoolStatusChangedDelegate)      handler;
            }

            foreach (var handler in OnAdminStatusChanged?.GetInvocationList() ?? [])
            {
                clone.OnAdminStatusChanged += (OnChargingPoolAdminStatusChangedDelegate) handler;
            }

            return clone;

        }

        #endregion


        #region (private) LogEvent(Logger, LogHandler, ...)

        private Task LogEvent<TDelegate>(TDelegate?                                         Logger,
                                         Func<TDelegate, Task>                              LogHandler,
                                         [CallerArgumentExpression(nameof(Logger))] String  EventName   = "",
                                         [CallerMemberName()]                       String  Command     = "")

            where TDelegate : Delegate

                => LogEvent(
                       nameof(ChargingPool),
                       Logger,
                       LogHandler,
                       EventName,
                       Command
                   );

        #endregion


        #region Operator overloading

        #region Operator == (ChargingPool1, ChargingPool2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPool1">A charging pool.</param>
        /// <param name="ChargingPool2">Another charging pool.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingPool ChargingPool1,
                                           ChargingPool ChargingPool2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ChargingPool1, ChargingPool2))
                return true;

            // If one is null, but not both, return false.
            if (ChargingPool1 is null || ChargingPool2 is null)
                return false;

            return ChargingPool1.Equals(ChargingPool2);

        }

        #endregion

        #region Operator != (ChargingPool1, ChargingPool2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPool1">A charging pool.</param>
        /// <param name="ChargingPool2">Another charging pool.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingPool ChargingPool1,
                                           ChargingPool ChargingPool2)

            => !(ChargingPool1 == ChargingPool2);

        #endregion

        #region Operator <  (ChargingPool1, ChargingPool2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPool1">A charging pool.</param>
        /// <param name="ChargingPool2">Another charging pool.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingPool ChargingPool1,
                                          ChargingPool ChargingPool2)
        {

            if (ChargingPool1 is null)
                throw new ArgumentNullException(nameof(ChargingPool1), "The given ChargingPool1 must not be null!");

            return ChargingPool1.CompareTo(ChargingPool2) < 0;

        }

        #endregion

        #region Operator <= (ChargingPool1, ChargingPool2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPool1">A charging pool.</param>
        /// <param name="ChargingPool2">Another charging pool.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingPool ChargingPool1,
                                           ChargingPool ChargingPool2)

            => !(ChargingPool1 > ChargingPool2);

        #endregion

        #region Operator >  (ChargingPool1, ChargingPool2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPool1">A charging pool.</param>
        /// <param name="ChargingPool2">Another charging pool.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingPool ChargingPool1,
                                          ChargingPool ChargingPool2)
        {

            if (ChargingPool1 is null)
                throw new ArgumentNullException(nameof(ChargingPool1), "The given ChargingPool1 must not be null!");

            return ChargingPool1.CompareTo(ChargingPool2) > 0;

        }

        #endregion

        #region Operator >= (ChargingPool1, ChargingPool2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPool1">A charging pool.</param>
        /// <param name="ChargingPool2">Another charging pool.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingPool ChargingPool1,
                                           ChargingPool ChargingPool2)

            => !(ChargingPool1 < ChargingPool2);

        #endregion

        #endregion

        #region IComparable<ChargingPool> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingPool chargingPool
                   ? CompareTo(chargingPool)
                   : throw new ArgumentException("The given object is not a charging pool!", nameof(Object));

        #endregion

        #region CompareTo(ChargingPool)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingPool">An ChargingPool to compare with.</param>
        public Int32 CompareTo(ChargingPool? ChargingPool)

            => ChargingPool is not null
                   ? Id.CompareTo(ChargingPool.Id)
                   : throw new ArgumentException("The given object is not a ChargingPool!", nameof(ChargingPool));

        #endregion

        #endregion

        #region IEquatable<ChargingPool> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPool chargingPool &&
                   Equals(chargingPool);

        #endregion

        #region Equals(ChargingPool)

        /// <summary>
        /// Compares two charging pools for equality.
        /// </summary>
        /// <param name="ChargingPool">A charging pool to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(ChargingPool? ChargingPool)

            => ChargingPool is not null &&

               Id.Equals(ChargingPool.Id);

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

            if (StatusAggregationDelegate is not null)
                statusSchedule.Insert(StatusAggregationDelegate(new ChargingStationStatusReport(chargingStations)),
                                      Timestamp,
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
                DebugX.LogException(e, $"ChargingPool '{Id}'.UpdateEVSEData of EVSE '{EVSE.Id}' property '{PropertyName}' from '{OldValue?.ToString() ?? "-"}' to '{NewValue?.ToString() ?? "-"}'");
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
                DebugX.LogException(e, $"ChargingPool '{Id}'.UpdateEVSEAdminStatus of EVSE '{EVSE.Id}' from '{OldAdminStatus?.ToString() ?? "-"}' to '{NewAdminStatus}'");
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
                DebugX.LogException(e, $"ChargingPool '{Id}'.UpdateEVSEStatus of EVSE '{EVSE.Id}' from '{OldStatus}' to '{NewStatus}'");
            }

        }
    }

}
