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

using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using System.Collections.Concurrent;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Extension methods for the common Electric Vehicle Supply Equipments (EVSEs).
    /// </summary>
    public static class EVSEExtensions
    {

        #region ToJSON(this EVSEs, Skip = null, Take = null, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given enumeration of EVSEs.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="Skip">The optional number of EVSEs to skip.</param>
        /// <param name="Take">The optional number of EVSEs to return.</param>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a charging station.</param>
        public static JArray ToJSON(this IEnumerable<EVSE>                               EVSEs,
                                    UInt64?                                              Skip                                = null,
                                    UInt64?                                              Take                                = null,
                                    Boolean                                              Embedded                            = false,
                                    Boolean?                                             IncludeRemoved                      = false,
                                    InfoStatus                                           ExpandRoamingNetworkId              = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandChargingStationOperatorId     = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandChargingPoolId                = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandChargingStationId             = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandBrandIds                      = InfoStatus.ShowIdOnly,
                                    InfoStatus                                           ExpandDataLicenses                  = InfoStatus.ShowIdOnly,
                                    Boolean?                                             IncludeCustomData                   = null,
                                    CustomJObjectSerializerDelegate<EVSE>?               CustomEVSESerializer                = null,
                                    CustomJObjectSerializerDelegate<ChargingConnector>?  CustomChargingConnectorSerializer   = null)


            => EVSEs is not null && EVSEs.Any()

                   ? new JArray(
                         EVSEs.Where          (evse => evse is not null).
                               Where          (evse => IncludeRemoved == true || evse.Status != EVSEStatusType.Removed).
                               OrderBy        (evse => evse.Id).
                               SkipTakeFilter (Skip, Take).
                               SafeSelect     (evse => evse.ToJSON(Embedded,
                                                                   ExpandRoamingNetworkId,
                                                                   ExpandChargingStationOperatorId,
                                                                   ExpandChargingPoolId,
                                                                   ExpandChargingStationId,
                                                                   ExpandBrandIds,
                                                                   ExpandDataLicenses,
                                                                   IncludeCustomData,
                                                                   CustomEVSESerializer,
                                                                   CustomChargingConnectorSerializer)).
                               Where          (evse => evse is not null)
                     )

                   : [];

        #endregion

    }


    /// <summary>
    /// An Electric Vehicle Supply Equipment (EVSE) to charge an electric vehicle (EV).
    /// This is meant to be one electrical circuit which can charge a electric vehicle
    /// independently. Thus there could be multiple interdependent power sockets.
    /// </summary>
    public partial class EVSE : AEMobilityEntity<EVSE_Id,
                                         EVSEAdminStatusType,
                                         EVSEStatusType>,
                        IEquatable<EVSE>, IComparable<EVSE>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const            String    JSONLDContext                           = "https://open.charging.cloud/contexts/wwcp+json/EVSE";


        private readonly        Decimal   EPSILON                                 = 0.01m;

        /// <summary>
        /// The default max size of the EVSE admin status schedule/history.
        /// </summary>
        public const            UInt16    DefaultMaxEVSEAdminStatusScheduleSize   = 50;

        /// <summary>
        /// The default max size of the EVSE status schedule/history.
        /// </summary>
        public const            UInt16    DefaultMaxEVSEStatusScheduleSize        = 50;

        /// <summary>
        /// The maximum time span for a reservation.
        /// </summary>
        public static readonly  TimeSpan  DefaultMaxReservationDuration           = TimeSpan.FromMinutes(15);

        #endregion

        #region Properties

        /// <summary>
        /// The roaming network of this EVSE.
        /// </summary>
        [InternalUseOnly]
        public RoamingNetwork?                         RoamingNetwork
            => ChargingStation?.RoamingNetwork;

        /// <summary>
        /// The charging station operator of this EVSE.
        /// </summary>
        [InternalUseOnly]
        public ChargingStationOperator?                Operator
            => ChargingStation?.Operator;

        /// <summary>
        /// The charging pool of this EVSE.
        /// </summary>
        public ChargingPool?                           ChargingPool
            => ChargingStation?.ChargingPool;

        /// <summary>
        /// The charging station of this EVSE.
        /// </summary>
        public ChargingStation?                        ChargingStation             { get; }

        /// <summary>Tariffs assigned directly to this EVSE; connector assignments are separate.</summary>
        public System.Collections.Immutable.ImmutableArray<ChargingTariff_Id> ChargingTariffIds { get; private set; } = [];

        public IEnumerable<ChargingTariff> ChargingTariffs
            => Operator?.ChargingTariffs.Where(tariff => ChargingTariffIds.Any(id => id.Equals(tariff.Id))) ?? [];

        #region PhysicalReference

        private String? physicalReference;

        /// <summary>
        /// An optional number/string printed on the outside of the EVSE for visual identification.
        /// </summary>
        [Optional, SlowData]
        public String? PhysicalReference
        {

            get
            {
                return physicalReference;
            }

            set
            {

                if (physicalReference != value)
                    SetProperty(ref physicalReference,
                                value);

            }

        }

        #endregion

        /// <summary>
        /// The geographical location of this EVSE, e.g. when this EVSE is part of a satellite system.
        /// </summary>
        public GeoCoordinate?                           GeoLocation                 { get; }

        /// <summary>
        /// An optional enumeration of links to photos related to the EVSE.
        /// </summary>
        [Optional, SlowData]
        public ReactiveSet<URL>                         PhotoURLs                   { get; }

        /// <summary>
        /// An enumeration of all brands registered for this EVSE.
        /// </summary>
        [Optional, SlowData]
        public ReactiveSet<Brand>                       Brands                      { get; }

        /// <summary>
        /// All e-mobility related Root-CAs, e.g. ISO 15118-2/-20, available at this EVSE.
        /// </summary>
        [Optional, SlowData]
        public ReactiveSet<RootCAInfo>                  MobilityRootCAs             { get; }

        /// <summary>
        /// An enumeration of all data license(s) of this EVSE.
        /// </summary>
        [Optional, SlowData]
        public ReactiveSet<DataLicense>                 DataLicenses                { get; }

        /// <summary>
        /// The optional URL where certificates, identifiers and public keys related to the calibration
        /// of meters in this EVSE can be found.
        /// </summary>
        public URL?                                     CalibrationInfo             { get; }

        /// <summary>
        /// An enumeration of all supported charging modes of this EVSE.
        /// </summary>
        [Mandatory, SlowData]
        public ReactiveSet<ChargingModes>               ChargingModes               { get; }


        #region CurrentType

        private CurrentTypes currentType;

        /// <summary>
        /// The type of the current.
        /// </summary>
        [Mandatory, SlowData]
        public CurrentTypes CurrentType
        {

            get
            {
                return currentType;
            }

            set
            {

                if (currentType != value)
                    SetProperty(ref currentType,
                                value);

            }

        }

        #endregion


        #region MaxVoltage

        private Volt? averageVoltage;

        /// <summary>
        /// The maximum voltage.
        /// </summary>
        [Optional, SlowData]
        public Volt? MaxVoltage
        {

            get
            {
                return averageVoltage;
            }

            set
            {

                if (value is not null)
                {

                    if (!averageVoltage.HasValue)
                        averageVoltage = value;

                    else if (Math.Abs(averageVoltage.Value.Value - value.Value.Value) > EPSILON)
                        SetProperty(ref averageVoltage,
                                    value);

                }
                else
                    DeleteProperty(ref averageVoltage);

            }

        }

        #endregion

        #region MaxVoltageRealTime

        private Timestamped<Volt>? averageVoltageRealTime;

        /// <summary>
        /// The real-time maximum voltage.
        /// </summary>
        [Optional, FastData]
        public Timestamped<Volt>? MaxVoltageRealTime
        {

            get
            {
                return averageVoltageRealTime;
            }

            set
            {

                if (value is not null)
                {

                    if (!averageVoltageRealTime.HasValue || Math.Abs(averageVoltageRealTime.Value.Value.Value - value.Value.Value.Value) > EPSILON)
                        SetProperty(ref averageVoltageRealTime,
                                    value);

                }
                else
                    DeleteProperty(ref averageVoltage);

            }

        }

        #endregion

        /// <summary>
        /// Prognoses on future values of the maximum voltage.
        /// </summary>
        [Optional, FastData]
        public ReactiveSet<Timestamped<Volt>>           MaxVoltagePrognoses     { get; }


        #region MaxCurrent

        private Ampere? maxCurrent;

        /// <summary>
        /// The maximum current [Ampere].
        /// </summary>
        [Mandatory, SlowData]
        public Ampere? MaxCurrent
        {

            get
            {
                return maxCurrent;
            }

            set
            {

                if (value is not null)
                {

                    if (!maxCurrent.HasValue)
                        SetProperty(ref maxCurrent,
                                    value);

                    else if (Math.Abs(maxCurrent.Value.Value - value.Value.Value) > EPSILON)
                        SetProperty(ref maxCurrent,
                                    value);

                }
                else
                    DeleteProperty(ref maxCurrent);

            }

        }

        #endregion

        #region MaxCurrentRealTime

        private Timestamped<Ampere>? maxCurrentRealTime;

        /// <summary>
        /// The real-time maximum current [Ampere].
        /// </summary>
        [Optional, FastData]
        public Timestamped<Ampere>? MaxCurrentRealTime
        {

            get
            {
                return maxCurrentRealTime;
            }

            set
            {

                if (value is not null)
                {

                    if (!maxCurrentRealTime.HasValue || Math.Abs(maxCurrentRealTime.Value.Value.Value - value.Value.Value.Value) > EPSILON)
                        SetProperty(ref maxCurrentRealTime,
                                    value);

                }

                else
                    DeleteProperty(ref maxCurrentRealTime);

            }

        }

        #endregion

        /// <summary>
        /// Prognoses on future values of the maximum current].
        /// </summary>
        [Optional, FastData]
        public ReactiveSet<Timestamped<Ampere>>         MaxCurrentPrognoses         { get; }


        #region MaxPower

        private Watt? maxPower;

        /// <summary>
        /// The maximum power.
        /// </summary>
        [Optional, SlowData]
        public Watt? MaxPower
        {

            get
            {
                return maxPower;
            }

            set
            {

                if (value is not null)
                {

                    if (!maxPower.HasValue || Math.Abs(maxPower.Value.Value - value.Value.Value) > EPSILON)
                        SetProperty(ref maxPower,
                                    value);

                }
                else
                    DeleteProperty(ref maxPower);

            }

        }

        #endregion

        #region MaxPowerRealTime

        private Timestamped<Watt>? maxPowerRealTime;

        /// <summary>
        /// The real-time maximum power.
        /// </summary>
        [Optional, FastData]
        public Timestamped<Watt>? MaxPowerRealTime
        {

            get
            {
                return maxPowerRealTime;
            }

            set
            {

                if (value is not null)
                {

                    if (!maxPowerRealTime.HasValue || Math.Abs(maxPowerRealTime.Value.Value.Value - value.Value.Value.Value) > EPSILON)
                        SetProperty(ref maxPowerRealTime,
                                    value);

                }
                else
                    DeleteProperty(ref maxPowerRealTime);

            }

        }

        #endregion

        /// <summary>
        /// Prognoses on future values of the maximum power.
        /// </summary>
        [Optional, FastData]
        public ReactiveSet<Timestamped<Watt>>           MaxPowerPrognoses           { get; }


        #region MaxCapacity

        private WattHour? maxCapacity;

        /// <summary>
        /// The maximum capacity.
        /// </summary>
        [Mandatory]
        public WattHour? MaxCapacity
        {

            get
            {
                return maxCapacity;
            }

            set
            {

                if (value is not null)
                {

                    if (!maxCapacity.HasValue)
                        SetProperty(ref maxCapacity,
                                    value);

                    else if (Math.Abs(maxCapacity.Value.Value - value.Value.Value) > EPSILON)
                        SetProperty(ref maxCapacity,
                                    value);

                }
                else
                    DeleteProperty(ref maxCapacity);

            }

        }

        #endregion

        #region MaxCapacityRealTime

        private Timestamped<WattHour>? maxCapacityRealTime;

        /// <summary>
        /// The real-time maximum capacity.
        /// </summary>
        [Optional]
        public Timestamped<WattHour>? MaxCapacityRealTime
        {

            get
            {
                return maxCapacityRealTime;
            }

            set
            {

                if (value is not null)
                {

                    if (!maxCapacityRealTime.HasValue || Math.Abs(maxCapacityRealTime.Value.Value.Value - value.Value.Value.Value) > EPSILON)
                        SetProperty(ref maxCapacityRealTime,
                                    value);

                }
                else
                    DeleteProperty(ref maxCapacityRealTime);

            }

        }

        #endregion

        /// <summary>
        /// Prognoses on future values of the maximum capacity.
        /// </summary>
        [Optional]
        public ReactiveSet<Timestamped<WattHour>>       MaxCapacityPrognoses        { get; }


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
                return energyMix ?? ChargingStation?.EnergyMix;
            }

            set
            {

                if (value != energyMix && value != ChargingStation?.EnergyMix)
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
                return energyMixPrognoses ?? ChargingStation?.EnergyMixPrognoses;
            }

            set
            {

                if (value != energyMixPrognoses && value != ChargingStation?.EnergyMixPrognoses)
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

            set
            {
                if (maxReservationDuration.TotalSeconds != value.TotalSeconds)
                    SetProperty(ref maxReservationDuration,
                                value);
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

            set
            {
                if (isFreeOfCharge != value)
                    SetProperty(ref isFreeOfCharge,
                                value);
            }

        }

        #endregion


        #region EnergyMeter

        private EnergyMeter? energyMeter;

        /// <summary>
        /// The smart energy meter attached to this EVSE.
        /// </summary>
        [Optional, SlowData]
        public EnergyMeter? EnergyMeter
        {

            get
            {
                return energyMeter;
            }

            set
            {

                if (value is not null)
                    SetProperty(ref energyMeter, value);

                else
                    DeleteProperty(ref energyMeter);

            }

        }

        #endregion


        private readonly ConcurrentDictionary<ChargingConnector_Id, ChargingConnector> chargingConnectors = [];

        /// <summary>
        /// The power socket outlets.
        /// </summary>
        [Mandatory, SlowData]
        public IEnumerable<ChargingConnector>           ChargingConnectors
            => chargingConnectors.Values;

        /// <summary>
        /// The timestamp of the last status update.
        /// This might be different from the timestamp of the last status change,
        /// when the status was imported from a third party.
        /// </summary>
        [Optional]
        public DateTimeOffset?                          LastStatusUpdate            { get; set; }

        #endregion

        #region Events

        #region OnData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data changed.
        /// </summary>
        public event OnEVSEDataChangedDelegate?         OnDataChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnEVSEAdminStatusChangedDelegate?  OnAdminStatusChanged;

        /// <summary>
        /// An event fired whenever the dynamic status changed.
        /// </summary>
        public event OnEVSEStatusChangedDelegate?       OnStatusChanged;

        #endregion

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new Electric Vehicle Supply Equipment (EVSE) having the given EVSE identification.
        /// </summary>
        /// <param name="Id">The unique identification of the EVSE.</param>
        /// <param name="ChargingStation">The charging station hosting this EVSE.</param>
        /// 
        /// <param name="RemoteEVSECreator">A delegate to attach a remote EVSE.</param>
        /// <param name="InitialAdminStatus">An optional initial admin status of the EVSE.</param>
        /// <param name="InitialStatus">An optional initial status of the EVSE.</param>
        /// <param name="MaxAdminStatusScheduleSize">An optional max length of the admin staus schedule.</param>
        /// <param name="MaxStatusScheduleSize">An optional max length of the staus schedule.</param>
        /// 
        /// <param name="Name">An optional multi-language text e.g. printed on the outside of the EVSE for visual identification.</param>
        /// <param name="Description">An optional multi-language description of this EVSE.</param>
        /// 
        /// <param name="PhotoURLs">An optional enumeration of links to photos related to the EVSE.</param>
        /// <param name="Brands">An optional enumeration of brands registered for this EVSE.</param>
        /// <param name="DataLicenses">An optional enumeration of data license(s) of this EVSE.</param>
        /// <param name="ChargingModes">An optional enumeration of the supported charging modes of this EVSE.</param>
        /// 
        /// <param name="DataSource"></param>
        /// <param name="LastChange"></param>
        /// 
        /// <param name="Configurator">A delegate to configure the newly created EVSE.</param>
        /// 
        /// <param name="CustomData">Optional customer specific data, e.g. in combination with custom parsers and serializers.</param>
        /// <param name="InternalData">Optional internal data.</param>
        public EVSE(EVSE_Id                              Id,
                    ChargingStation                     ChargingStation,
                    I18NString?                          Name                         = null,
                    I18NString?                          Description                  = null,

                    String?                              PhysicalReference            = null,
                    GeoCoordinate?                       GeoLocation                  = null,
                    IEnumerable<URL>?                    PhotoURLs                    = null,
                    IEnumerable<Brand>?                  Brands                       = null,
                    IEnumerable<RootCAInfo>?             MobilityRootCAs              = null,
                    IEnumerable<DataLicense>?            DataLicenses                 = null,
                    IEnumerable<ChargingModes>?          ChargingModes                = null,
                    IEnumerable<ChargingTariff>?         ChargingTariffs              = null,
                    CurrentTypes?                        CurrentType                  = null,
                    Volt?                                MaxVoltage                   = null,
                    Timestamped<Volt>?                   MaxVoltageRealTime           = null,
                    IEnumerable<Timestamped<Volt>>?      MaxVoltagePrognoses          = null,
                    Ampere?                              MaxCurrent                   = null,
                    Timestamped<Ampere>?                 MaxCurrentRealTime           = null,
                    IEnumerable<Timestamped<Ampere>>?    MaxCurrentPrognoses          = null,
                    Watt?                                MaxPower                     = null,
                    Timestamped<Watt>?                   MaxPowerRealTime             = null,
                    IEnumerable<Timestamped<Watt>>?      MaxPowerPrognoses            = null,
                    WattHour?                            MaxCapacity                  = null,
                    Timestamped<WattHour>?               MaxCapacityRealTime          = null,
                    IEnumerable<Timestamped<WattHour>>?  MaxCapacityPrognoses         = null,
                    EnergyMix?                           EnergyMix                    = null,
                    Timestamped<EnergyMix>?              EnergyMixRealTime            = null,
                    EnergyMixPrognosis?                  EnergyMixPrognoses           = null,
                    EnergyMeter?                         EnergyMeter                  = null,
                    Boolean?                             IsFreeOfCharge               = null,
                    URL?                                 CalibrationInfo              = null,
                    IEnumerable<ChargingConnector>?     ChargingConnectors           = null,

                    Timestamped<EVSEAdminStatusType>?    InitialAdminStatus           = null,
                    Timestamped<EVSEStatusType>?         InitialStatus                = null,
                    UInt16?                              MaxAdminStatusScheduleSize   = null,
                    UInt16?                              MaxStatusScheduleSize        = null,
                    DateTimeOffset?                      LastStatusUpdate             = null,

                    String?                              DataSource                   = null,
                    DateTimeOffset?                      Created                      = null,
                    DateTimeOffset?                      LastChange                   = null,

                    CustomDataNew?                       CustomData                   = null,
                    UserDefinedDictionary?               InternalData                 = null)

            : base(Id,
                   Name,
                   Description,
                   InitialAdminStatus         ?? EVSEAdminStatusType.Operational,
                   InitialStatus              ?? EVSEStatusType.Available,
                   MaxAdminStatusScheduleSize ?? DefaultMaxEVSEAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxEVSEStatusScheduleSize,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            #region Init data and properties

            this.ChargingStation                    = ChargingStation;

            this.physicalReference                  = PhysicalReference;
            this.GeoLocation                        = GeoLocation;
            this.PhysicalReference                  = PhysicalReference;
            this.GeoLocation                        = GeoLocation;

            this.PhotoURLs                          = PhotoURLs is null
                                                          ? []
                                                          : [.. PhotoURLs];
            this.PhotoURLs.OnSetChanged            += (timestamp, sender, newItems, oldItems) => {

                PropertyChanged("PhotoURLs",
                                oldItems,
                                newItems);

            };

            this.Brands                             = Brands is null
                                                          ? []
                                                          : [.. Brands];
            this.Brands.OnSetChanged               += (timestamp, sender, newItems, oldItems) => {

                PropertyChanged("DataLicenses",
                                oldItems,
                                newItems);

            };


            this.MobilityRootCAs = new ReactiveSet<RootCAInfo>();

            if (MobilityRootCAs is not null)
                foreach (var mobilityRootCA in MobilityRootCAs)
                    this.MobilityRootCAs.Add(mobilityRootCA);

            this.MobilityRootCAs.OnSetChanged       += (timestamp, sender, newItems, oldItems) => {

                PropertyChanged("MobilityRootCAs",
                                oldItems,
                                newItems);

            };

            this.DataLicenses                   = DataLicenses is null
                                                      ? []
                                                      : [.. DataLicenses];
            this.DataLicenses.OnSetChanged     += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("DataLicenses",
                                oldItems,
                                newItems);

            };

            this.ChargingModes                      = ChargingModes is null
                                                          ? []
                                                          : [.. ChargingModes];
            this.ChargingModes.OnSetChanged        += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("ChargingModes",
                                oldItems,
                                newItems);

            };

            this.ChargingTariffIds = System.Collections.Immutable.ImmutableArray.CreateRange(
                ChargingTariffs?.Select(tariff => tariff.Id).Distinct() ?? []);

            this.CalibrationInfo                    = CalibrationInfo;

            this.currentType                        = CurrentType ?? CurrentTypes.AC_ThreePhases;

            this.averageVoltage                     = MaxVoltage;
            this.averageVoltageRealTime             = MaxVoltageRealTime;

            this.MaxVoltagePrognoses            = MaxVoltagePrognoses is null
                                                          ? []
                                                          : [.. MaxVoltagePrognoses];
            this.MaxVoltagePrognoses.OnSetChanged  += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxVoltagePrognoses",
                                oldItems,
                                newItems);

            };


            this.maxCurrent                         = MaxCurrent;
            this.maxCurrentRealTime                 = MaxCurrentRealTime;

            this.MaxCurrentPrognoses                = MaxCurrentPrognoses is null
                                                          ? []
                                                          : [.. MaxCurrentPrognoses];
            this.MaxCurrentPrognoses.OnSetChanged  += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxCurrentPrognoses",
                                oldItems,
                                newItems);

            };

            this.maxPower                           = MaxPower;
            this.maxPowerRealTime                   = MaxPowerRealTime;

            this.MaxPowerPrognoses                  = MaxPowerPrognoses is null
                                                          ? []
                                                          : [.. MaxPowerPrognoses];
            this.MaxPowerPrognoses.OnSetChanged    += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxPowerPrognoses",
                                oldItems,
                                newItems);

            };

            this.maxCapacity                        = MaxCapacity;
            this.maxCapacityRealTime                = MaxCapacityRealTime;

            this.MaxCapacityPrognoses               = MaxCapacityPrognoses is null
                                                          ? []
                                                          : [.. MaxCapacityPrognoses];
            this.MaxCapacityPrognoses.OnSetChanged += (timestamp, reactiveSet, newItems, oldItems) =>
            {

                PropertyChanged("MaxCapacityPrognoses",
                                oldItems,
                                newItems);

            };

            this.energyMix                          = EnergyMix;
            this.energyMixRealTime                  = EnergyMixRealTime;
            this.energyMixPrognoses                 = EnergyMixPrognoses;

            this.energyMeter                        = EnergyMeter;

            this.IsFreeOfCharge                     = IsFreeOfCharge ?? false;

            this.chargingConnectors                 = ChargingConnectors is null
                                                          ? new ConcurrentDictionary<ChargingConnector_Id, ChargingConnector>()
                                                          : new ConcurrentDictionary<ChargingConnector_Id, ChargingConnector>(ChargingConnectors.ToDictionary(cc => cc.Id, cc => cc));

            foreach (var chargingConnector in this.ChargingConnectors)
                chargingConnector.EVSE = this;

            this.LastStatusUpdate                   = LastStatusUpdate;

            #endregion

            #region Link events

            this.OnPropertyChanged                   += (timestamp, eventTrackingId, sender, propertyName, newValue, oldValue, dataSource)
                                                         => UpdateData       (timestamp, eventTrackingId, propertyName, newValue, oldValue, dataSource);

            this.adminStatusSchedule.OnStatusChanged += (timestamp, eventTrackingId, statusSchedule, newStatus, oldStatus, dataSource)
                                                         => UpdateAdminStatus(timestamp, eventTrackingId, newStatus, oldStatus, dataSource);

            this.statusSchedule.     OnStatusChanged += (timestamp, eventTrackingId, statusSchedule, newStatus, oldStatus, dataSource)
                                                         => UpdateStatus     (timestamp, eventTrackingId, newStatus, oldStatus, dataSource);

            #endregion

        }

        #endregion


        #region UpdateWith(OtherEVSE)

        /// <summary>
        /// Update this EVSE with the data of the other EVSE.
        /// </summary>
        /// <param name="OtherEVSE">Another EVSE.</param>
        public EVSE UpdateWith(EVSE OtherEVSE)
        {

            Name.                   Set    (OtherEVSE.Name);
            Description.            Set    (OtherEVSE.Description);

            Brands.                 Replace(OtherEVSE.Brands);
            ChargingModes.          Replace(OtherEVSE.ChargingModes);
            //ChargingConnectors.     Replace(OtherEVSE.ChargingConnectors);
            DataLicenses.           Replace(OtherEVSE.DataLicenses);
            MaxVoltagePrognoses.    Replace(OtherEVSE.MaxVoltagePrognoses);
            MaxCurrentPrognoses.    Replace(OtherEVSE.MaxCurrentPrognoses);
            MaxPowerPrognoses.      Replace(OtherEVSE.MaxPowerPrognoses);
            MaxCapacityPrognoses.   Replace(OtherEVSE.MaxCapacityPrognoses);

            CurrentType                = OtherEVSE.CurrentType;
            MaxVoltage                 = OtherEVSE.MaxVoltage;
            MaxVoltageRealTime         = OtherEVSE.MaxVoltageRealTime;
            MaxCurrent                 = OtherEVSE.MaxCurrent;
            MaxCurrentRealTime         = OtherEVSE.MaxCurrentRealTime;
            MaxPower                   = OtherEVSE.MaxPower;
            MaxPowerRealTime           = OtherEVSE.MaxPowerRealTime;
            MaxCapacity                = OtherEVSE.MaxCapacity;
            MaxCapacityRealTime        = OtherEVSE.MaxCapacityRealTime;
            EnergyMix                  = OtherEVSE.EnergyMix;               //ToDo: Implement Equality!
            EnergyMixRealTime          = OtherEVSE.EnergyMixRealTime;       //ToDo: Implement Equality!
            EnergyMixPrognoses         = OtherEVSE.EnergyMixPrognoses;      //ToDo: Implement Equality!
            EnergyMeter                = OtherEVSE.EnergyMeter;             //ToDo: Implement Equality!
            IsFreeOfCharge             = OtherEVSE.IsFreeOfCharge;
            MaxReservationDuration     = OtherEVSE.MaxReservationDuration;

            if (OtherEVSE.AdminStatus.Timestamp > AdminStatus.Timestamp)
                AdminStatus            = OtherEVSE.AdminStatus;

            if (OtherEVSE.Status.     Timestamp > Status.     Timestamp)
                Status                 = OtherEVSE.Status;

            return this;

        }

        #endregion


        #region Data/(Admin-)Status


        public void SetAdminStatus(EVSEAdminStatus EVSEAdminStatus)
        {

            //adminStatusSchedule.Insert(EVSEAdminStatus.Status,
            //                           EVSEAdminStatus.Timestamp,
            //                           EVSEAdminStatus.DataSource);

        }

        public void SetAdminStatus(EVSEAdminStatusUpdate EVSEAdminStatusUpdate)
        {

        }

        public void SetStatus(EVSEStatus EVSEStatus)
        {

            statusSchedule.Insert(
                EVSEStatus.Status,
                EVSEStatus.Timestamp,
                EVSEStatus.Context
            );

        }

        public void SetStatus(EVSEStatusUpdate EVSEStatusUpdate)
        {

            statusSchedule.Insert(
                EVSEStatusUpdate.NewStatus,
                EVSEStatusUpdate.Context
            );

        }

        public void SetEnergyStatus(EVSEEnergyStatus EVSEEnergyStatus)
        {

        }

        public void SetEnergyStatus(EVSEEnergyStatusUpdate EVSEEnergyStatusUpdate)
        {

        }


        #region (internal) UpdateData       (Timestamp, EventTrackingId, PropertyName, NewValue, OldValue = null, DataSource = null)

        /// <summary>
        /// Update the static data of the EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        /// <param name="OldValue">The optional old value of the changed property.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        internal async Task UpdateData(DateTimeOffset    Timestamp,
                                       EventTracking_Id  EventTrackingId,
                                       String            PropertyName,
                                       Object?           NewValue,
                                       Object?           OldValue     = null,
                                       Context?          DataSource   = null)
        {

            try
            {

                var onEVSEDataChanged = OnDataChanged;
                if (onEVSEDataChanged is not null)
                    await onEVSEDataChanged(Timestamp,
                                            EventTrackingId,
                                            this,
                                            PropertyName,
                                            NewValue,
                                            OldValue,
                                            DataSource);

            }
            catch (Exception e)
            {

                DebugX.LogException(e, $"EVSE '{Id}'.UpdateEVSEData of property '{PropertyName}' from '{OldValue?.ToString() ?? "-"}' to '{NewValue?.ToString() ?? "-"}'" +
                                       DataSource is not null ? $" ({DataSource})" : "");

            }

        }

        #endregion

        #region (internal) UpdateAdminStatus(Timestamp, EventTrackingId, NewAdminStatus, OldAdminStatus = null, DataSource = null)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An event tracking identification for correlating this request with other events.</param>
        /// <param name="NewAdminStatus">The new EVSE admin status.</param>
        /// <param name="OldAdminStatus">The optional old EVSE admin status.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        internal async Task UpdateAdminStatus(DateTimeOffset                     Timestamp,
                                              EventTracking_Id                   EventTrackingId,
                                              Timestamped<EVSEAdminStatusType>   NewAdminStatus,
                                              Timestamped<EVSEAdminStatusType>?  OldAdminStatus   = null,
                                              Context?                           DataSource       = null)
        {

            try
            {

                var onEVSEAdminStatusChanged = OnAdminStatusChanged;
                if (onEVSEAdminStatusChanged is not null)
                    await onEVSEAdminStatusChanged(Timestamp,
                                                   EventTrackingId,
                                                   this,
                                                   NewAdminStatus,
                                                   OldAdminStatus,
                                                   DataSource);

            }
            catch (Exception e)
            {

                DebugX.LogException(e, $"EVSE '{Id}'.UpdateEVSEAdminStatus from '{OldAdminStatus?.ToString() ?? "-"}' to '{NewAdminStatus}'" +
                                       DataSource is not null ? $" ({DataSource})" : "");

            }

        }

        #endregion

        #region (internal) UpdateStatus     (Timestamp, EventTrackingId, NewStatus, OldStatus = null, DataSource = null)

        /// <summary>
        /// Update the current status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An event tracking identification for correlating this request with other events.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        /// <param name="OldStatus">The optional old EVSE status.</param>
        /// <param name="DataSource">An optional data source or context for the status update.</param>
        internal async Task UpdateStatus(DateTimeOffset                Timestamp,
                                         EventTracking_Id              EventTrackingId,
                                         Timestamped<EVSEStatusType>   NewStatus,
                                         Timestamped<EVSEStatusType>?  OldStatus    = null,
                                         Context?                      DataSource   = null)
        {

            try
            {

                var onEVSEStatusChanged = OnStatusChanged;
                if (onEVSEStatusChanged is not null)
                    await onEVSEStatusChanged(Timestamp,
                                              EventTrackingId,
                                              this,
                                              NewStatus,
                                              OldStatus,
                                              DataSource);

            }
            catch (Exception e)
            {

                DebugX.LogException(e, $"EVSE '{Id}'.UpdateEVSEStatus from '{OldStatus}' to '{NewStatus}'" +
                                       DataSource is not null ? $" ({DataSource})" : "");

            }

        }

        #endregion

        #endregion


        #region ToJSON(this EVSE, Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation of the given EVSE.
        /// </summary>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a charging station.</param>
        public JObject? ToJSON(Boolean                                              Embedded                            = false,
                               InfoStatus                                           ExpandRoamingNetworkId              = InfoStatus.ShowIdOnly,
                               InfoStatus                                           ExpandChargingStationOperatorId     = InfoStatus.ShowIdOnly,
                               InfoStatus                                           ExpandChargingPoolId                = InfoStatus.ShowIdOnly,
                               InfoStatus                                           ExpandChargingStationId             = InfoStatus.ShowIdOnly,
                               InfoStatus                                           ExpandBrandIds                      = InfoStatus.ShowIdOnly,
                               InfoStatus                                           ExpandDataLicenses                  = InfoStatus.ShowIdOnly,
                               Boolean?                                             IncludeCustomData                   = null,
                               CustomJObjectSerializerDelegate<EVSE>?               CustomEVSESerializer                = null,
                               CustomJObjectSerializerDelegate<ChargingConnector>?  CustomChargingConnectorSerializer   = null)
        {

            try
            {

                var geoLocation = GeoLocation;

                // When embedded, do not include the geolocation if it is the same as the charging station or pool!
                if (Embedded)
                {

                    if (geoLocation.HasValue && ChargingStation?.GeoLocation.HasValue == true &&
                        JToken.DeepEquals(geoLocation.Value.ToJSON(), ChargingStation.GeoLocation.Value.ToJSON()))
                        geoLocation  = null;

                    if (geoLocation.HasValue && ChargingPool?.GeoLocation.HasValue == true &&
                        JToken.DeepEquals(geoLocation.Value.ToJSON(), ChargingPool.GeoLocation.Value.ToJSON()))
                        geoLocation  = null;

                }

                // Not embedded and no geolocation given, try to get it from the charging station or pool!
                else if (!geoLocation.HasValue)
                {
                    geoLocation ??= ChargingStation?.GeoLocation;
                    geoLocation ??= ChargingPool?.   GeoLocation;
                }

                var json         = JSONObject.Create(

                                             new JProperty("@id",                 Id.ToString()),

                                       !ChargingTariffIds.IsEmpty
                                           ? new JProperty("tariffIds", new JArray(ChargingTariffIds.Select(id => id.ToString())))
                                           : null,

                                       !Embedded
                                           ? new JProperty("@context",            JSONLDContext)
                                           : null,

                                       Name.IsNotNullOrEmpty()
                                           ? new JProperty("name",                Name.ToJSON())
                                           : null,

                                       PhysicalReference is not null
                                           ? new JProperty("physicalReference",   PhysicalReference)
                                           : null,

                                       Description.IsNotNullOrEmpty()
                                           ? new JProperty("description",         Description.ToJSON())
                                           : null,

                                       Brands.SafeAny()
                                           ? ExpandBrandIds.Switch(
                                                 () => new JProperty("brandId",   Brands.Select(brand => brand.Id.ToString())),
                                                 () => new JProperty("brand",     Brands.ToJSON()))
                                           : null,

                                       DataSource is not null
                                           ? new JProperty("dataSource", DataSource)
                                           : null,

                                       DataLicenses.Any()
                                           ? ExpandDataLicenses.Switch(
                                                 () => new JProperty("dataLicenseIds",             new JArray(DataLicenses.SafeSelect(dataLicense => dataLicense.Id.ToString()))),
                                                 () => new JProperty("dataLicenses",               DataLicenses.ToJSON()))
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

                                       ExpandChargingPoolId != InfoStatus.Hidden && ChargingPool is not null
                                           ? ExpandChargingPoolId.Switch(
                                                 () => new JProperty("chargingPoolId",            ChargingPool.Id.   ToString()),
                                                 () => new JProperty("chargingPool",              ChargingPool.      ToJSON(Embedded:                          true,
                                                                                                                            ExpandRoamingNetworkId:            InfoStatus.Hidden,
                                                                                                                            ExpandChargingStationOperatorId:   InfoStatus.Hidden,
                                                                                                                            ExpandChargingStationIds:          InfoStatus.Hidden,
                                                                                                                            ExpandEVSEIds:                     InfoStatus.Hidden,
                                                                                                                            ExpandBrandIds:                    ExpandBrandIds,
                                                                                                                            ExpandDataLicenses:                ExpandDataLicenses)))
                                           : null,

                                       ExpandChargingStationId != InfoStatus.Hidden && ChargingStation is not null
                                           ? ExpandChargingStationId.Switch(
                                                 () => new JProperty("chargingStationId",         ChargingStation.Id.ToString()),
                                                 () => new JProperty("chargingStation",           ChargingStation.   ToJSON(Embedded:                          true,
                                                                                                                            ExpandRoamingNetworkId:            InfoStatus.Hidden,
                                                                                                                            ExpandChargingStationOperatorId:   InfoStatus.Hidden,
                                                                                                                            ExpandEVSEIds:                     InfoStatus.Hidden,
                                                                                                                            ExpandBrandIds:                    ExpandBrandIds,
                                                                                                                            ExpandDataLicenses:                ExpandDataLicenses)))
                                           : null,

                                       geoLocation.HasValue
                                           ? new JProperty("geoLocation",           geoLocation.Value.ToJSON(Embedded: true))
                                           : null,

                                       !Embedded && ChargingStation is not null && ChargingPool is not null && (ChargingStation.Address is not null  || ChargingPool.Address is not null)
                                           ? new JProperty("address",              (ChargingStation.Address ?? ChargingPool.Address)?.ToJSON(Embedded: true))
                                           : null,

                                       !Embedded && ChargingStation is not null && ChargingStation.AuthenticationModes.Any()
                                           ? new JProperty("authenticationModes",   ChargingStation.AuthenticationModes.ToJSON())
                                           : null,

                                       IsFreeOfCharge
                                           ? new JProperty("isFreeOfCharge",        IsFreeOfCharge)
                                           : null,

                                       ChargingModes.SafeAny()
                                           ? new JProperty("chargingModes",         new JArray(ChargingModes.SelectMany(chargingMode => chargingMode.ToText()).Distinct()))
                                           : null,

                                             new JProperty("currentType",           CurrentType.ToText()),

                                       MaxVoltage.HasValue
                                           ? new JProperty("averageVoltage",        MaxVoltage.Value.Value)
                                           : null,

                                       MaxCurrent.    HasValue
                                           ? new JProperty("maxCurrent",            MaxCurrent.Value.Value)
                                           : null,

                                       MaxPower.      HasValue
                                           ? new JProperty("maxPower",              MaxPower.Value.Value)
                                           : null,

                                       MaxCapacity.   HasValue
                                           ? new JProperty("maxCapacity",           MaxCapacity.Value.Value)
                                           : null,

                                       ChargingConnectors.Any()
                                           ? new JProperty("socketOutlets",         new JArray(ChargingConnectors.Select(chargingConnector => chargingConnector.ToJSON(Embedded:                           true,
                                                                                                                                                                       CustomChargingConnectorSerializer:  CustomChargingConnectorSerializer))))
                                           : null,

                                       EnergyMeter is not null
                                           ? new JProperty("energyMeter",           EnergyMeter.ToJSON(Embedded: true))
                                           : null,

                                       !Embedded && ChargingStation?.OpeningTimes is not null
                                           ? new JProperty("openingTimes",          ChargingStation.OpeningTimes.ToJSON())
                                           : null,

                                       CustomData.HasValues && IncludeCustomData == true
                                           ? new JProperty("customData",            CustomData.ToJObject())
                                           : null

                                 );

                return CustomEVSESerializer is not null
                           ? CustomEVSESerializer(this, json)
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
                       nameof(EVSE),
                       Logger,
                       LogHandler,
                       EventName,
                       Command
                   );

        #endregion


        #region Operator overloading

        #region Operator == (EVSE1, EVSE2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSE1">An EVSE.</param>
        /// <param name="EVSE2">Another EVSE.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator ==(EVSE EVSE1, EVSE EVSE2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(EVSE1, EVSE2))
                return true;

            // If one is null, but not both, return false.
            if (EVSE1 is null || EVSE2 is null)
                return false;

            return EVSE1.Equals(EVSE2);

        }

        #endregion

        #region Operator != (EVSE1, EVSE2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSE1">An EVSE.</param>
        /// <param name="EVSE2">Another EVSE.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator !=(EVSE EVSE1, EVSE EVSE2)
            => !(EVSE1 == EVSE2);

        #endregion

        #region Operator <  (EVSE1, EVSE2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSE1">An EVSE.</param>
        /// <param name="EVSE2">Another EVSE.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <(EVSE EVSE1, EVSE EVSE2)
        {

            if (EVSE1 is null)
                throw new ArgumentNullException(nameof(EVSE1), "The given EVSE1 must not be null!");

            return EVSE1.CompareTo(EVSE2) < 0;

        }

        #endregion

        #region Operator <= (EVSE1, EVSE2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSE1">An EVSE.</param>
        /// <param name="EVSE2">Another EVSE.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <=(EVSE EVSE1, EVSE EVSE2)
            => !(EVSE1 > EVSE2);

        #endregion

        #region Operator >  (EVSE1, EVSE2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSE1">An EVSE.</param>
        /// <param name="EVSE2">Another EVSE.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >(EVSE EVSE1, EVSE EVSE2)
        {

            if (EVSE1 is null)
                throw new ArgumentNullException(nameof(EVSE1), "The given EVSE1 must not be null!");

            return EVSE1.CompareTo(EVSE2) > 0;

        }

        #endregion

        #region Operator >= (EVSE1, EVSE2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSE1">An EVSE.</param>
        /// <param name="EVSE2">Another EVSE.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >=(EVSE EVSE1, EVSE EVSE2)
            => !(EVSE1 < EVSE2);

        #endregion

        #endregion

        #region IComparable<EVSE> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EVSEs.
        /// </summary>
        /// <param name="Object">An EVSE to compare with.</param>
        public override Int32 CompareTo(Object? Object)

            => Object is EVSE evse
                   ? CompareTo(evse)
                   : throw new ArgumentException("The given object is not an EVSE!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EVSE)

        /// <summary>
        /// Compares two EVSEs.
        /// </summary>
        /// <param name="EVSE">An EVSE to compare with.</param>
        public Int32 CompareTo(EVSE? EVSE)
        {

            if (EVSE is null)
                throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!");

            return Id.CompareTo(EVSE.Id);

            //ToDo: Compare more properties!

        }

        #endregion

        #endregion

        #region IEquatable<EVSE> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVSEs for equality.
        /// </summary>
        /// <param name="Object">An EVSE to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVSE evse &&
                   Equals(evse);

        #endregion

        #region Equals(EVSE)

        /// <summary>
        /// Compares two EVSEs for equality.
        /// </summary>
        /// <param name="EVSE">An EVSE to compare with.</param>
        public Boolean Equals(EVSE? EVSE)

            => EVSE is not null &&
                   Id.Equals(EVSE.Id);

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

    }

}
