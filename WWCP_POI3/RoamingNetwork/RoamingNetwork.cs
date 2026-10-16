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
                                    InfoStatus                                                 ExpandDataLicenses                        = InfoStatus.Expanded,
                                    InfoStatus                                                 ExpandEMobilityProviderId                 = InfoStatus.ShowIdOnly,

                                    CustomJObjectSerializerDelegate<RoamingNetwork>?           CustomRoamingNetworkSerializer            = null)
                                    //CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null,
                                    //CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                                    //CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                                    //CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null)
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

                                                                CustomRoamingNetworkSerializer
                                                                //CustomChargingStationOperatorSerializer,
                                                                //CustomChargingPoolSerializer,
                                                                //CustomChargingStationSerializer,
                                                                //CustomEVSESerializer

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
                                                                           RoamingNetworkStatusType>,
                                                 IPOIParty
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

        #region SigningKeys, IdentityKeys

        /// <summary>
        /// The keys of the roaming network operator signing its change sets and commits, in the order of their identifications.
        /// </summary>
        public ImmutableArray<POIPublicKey>  SigningKeys     { get; }

        /// <summary>
        /// The identity keys of the roaming network operator, signing changes of its own keys
        /// and, for recovery, of the keys of other parties, in the order of their identifications.
        /// </summary>
        public ImmutableArray<POIPublicKey>  IdentityKeys    { get; }

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
                              ImmutableI18NString?            Name                         = null,
                              ImmutableI18NString?            Description                  = null,
                              RoamingNetworkAdminStatusType?  InitialAdminStatus           = null,
                              RoamingNetworkStatusType?       InitialStatus                = null,
                              UInt16?                         MaxAdminStatusScheduleSize   = null,
                              UInt16?                         MaxStatusScheduleSize        = null,

                              String?                         DataSource                   = null,
                              DateTimeOffset?                 Created                      = null,
                              DateTimeOffset?                 LastChange                   = null,

                              CustomDataNew?                  CustomData                   = null,
                              UserDefinedDictionary?          InternalData                 = null,

                              IEnumerable<DataLicense>?              DataLicenses               = null,
                              IEnumerable<ChargingStationOperator>?  ChargingStationOperators   = null,
                              IEnumerable<ChargingStationManufacturer>?  ChargingStationManufacturers   = null,
                              IEnumerable<EMobilityProvider>?            EMobilityProviders             = null,
                              IEnumerable<GridOperator>?                 GridOperators                  = null,
                              IEnumerable<ParkingOperator>?              ParkingOperators               = null,
                              IEnumerable<TransparencySoftware>?         TransparencySoftware           = null,
                              IEnumerable<TransparencySoftwareCertificate>?  TransparencySoftwareCertificates  = null,
                              IEnumerable<Brand>?                        Brands                         = null,
                              IEnumerable<EnergyMeterManufacturer>?      EnergyMeterManufacturers       = null,
                              IEnumerable<POIPublicKey>?                 SigningKeys                    = null,
                              IEnumerable<POIPublicKey>?                 IdentityKeys                   = null)

            : base(Id,
                   Name,
                   Description,
                   // Since its creation, unless told otherwise.
                   new Timestamped<RoamingNetworkAdminStatusType>(Created ?? Timestamp.Now, InitialAdminStatus ?? RoamingNetworkAdminStatusType.Operational),
                   new Timestamped<RoamingNetworkStatusType>     (Created ?? Timestamp.Now, InitialStatus      ?? RoamingNetworkStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the roaming network '{Id}'");

            #region Data licenses and charging station operators

            foreach (var dataLicense in DataLicenses ?? [])
            {

                ArgumentNullException.ThrowIfNull(dataLicense, nameof(DataLicenses));

                if (!dataLicenses.TryAdd(dataLicense.Id, dataLicense))
                    throw new ArgumentException($"The data license '{dataLicense.Id}' must not be given twice!", nameof(DataLicenses));

            }

            this.chargingStationOperators = new (ChargingStationOperators, entity => entity.Id, "charging station operator");

            this.chargingStationManufacturers = new (ChargingStationManufacturers, entity => entity.Id, "charging station manufacturer");

            // Every manufacturer a charging station names is one of this network.
            if (UnknownManufacturer(chargingStationOperators, chargingStationManufacturers.Index) is String unknownManufacturer)
                throw new ArgumentException(unknownManufacturer, nameof(ChargingStationManufacturers));

            this.eMobilityProviders = new (EMobilityProviders, entity => entity.Id, "e-mobility provider");

            this.gridOperators = new (GridOperators, entity => entity.Id, "grid operator");

            // Grid connection points are unique within the network, and every one a charging pool names is one of this network.
            if ((DuplicateGridConnectionPoint(gridOperators) ??
                 UnknownGridConnectionPoint  (chargingStationOperators, gridOperators)) is String gridError)
            {
                throw new ArgumentException(gridError, nameof(GridOperators));
            }

            // Energy meters are unique within the network, not only within their operator.
            if (DuplicateEnergyMeter(AllEnergyMeters(chargingStationOperators, gridOperators)) is String duplicateEnergyMeter)
                throw new ArgumentException(duplicateEnergyMeter, nameof(ChargingStationOperators));

            foreach (var energyMeter in AllEnergyMeters(chargingStationOperators, gridOperators))
                energyMeters.TryAdd(energyMeter.Id, energyMeter);

            this.energyMeterManufacturers = new (EnergyMeterManufacturers, entity => entity.Id, "energy meter manufacturer");

            // Every manufacturer an energy meter names is one of this network.
            if (UnknownEnergyMeterManufacturer(AllEnergyMeters(chargingStationOperators, gridOperators), energyMeterManufacturers.Index) is String unknownEnergyMeterManufacturer)
                throw new ArgumentException(unknownEnergyMeterManufacturer, nameof(EnergyMeterManufacturers));

            this.parkingOperators = new (ParkingOperators, entity => entity.Id, "parking operator");

            // Parking garages, spaces, sensors, space groups and products are unique within the network, the parking spaces
            // of charging station operators as well; the charging stations they name are ones of this network, and so are
            // the parking spaces charging stations name.
            if ((DuplicateParking      (parkingOperators, chargingStationOperators) ??
                 UnknownChargingStation(parkingOperators, chargingStationOperators.Index) ??
                 UnknownParkingSpace   (chargingStationOperators, parkingOperators)) is String parkingError)
            {
                throw new ArgumentException(parkingError, nameof(ParkingOperators));
            }

            // The maintainers of grid connection points are charging station operators and parking operators of this network.
            if (UnknownMaintainer(gridOperators, chargingStationOperators.Index, parkingOperators.Index) is String maintainerError)
                throw new ArgumentException(maintainerError, nameof(GridOperators));

            this.transparencySoftware = new (TransparencySoftware, entity => entity.Id, "transparency software release");

            this.transparencySoftwareCertificates = new (TransparencySoftwareCertificates, entity => entity.Id, "transparency software certificate");

            this.brands = new (Brands, entity => entity.Id, "brand");

            // Charging pools, stations and EVSEs name brands of this network.
            if (UnknownBrand(chargingStationOperators, brands.Index) is String brandError)
                throw new ArgumentException(brandError, nameof(Brands));

            // Certificates name releases and manufacturers of this network; energy meters name releases and certificates.
            if (UnknownTransparencyReference(AllEnergyMeters(chargingStationOperators, gridOperators), transparencySoftware.Index, transparencySoftwareCertificates.Index, chargingStationManufacturers.Index) is String transparencyError)
                throw new ArgumentException(transparencyError, nameof(TransparencySoftware));

            foreach (var parkingOperator in parkingOperators)
            {

                foreach (var parkingGarage in parkingOperator.ParkingGarages)
                    parkingGarages.TryAdd(parkingGarage.Id, parkingGarage);

                foreach (var parkingSpace in parkingOperator.ParkingSpaces)
                    parkingSpaces. TryAdd(parkingSpace. Id, parkingSpace);

                foreach (var parkingSensor in parkingOperator.ParkingSensors)
                    parkingSensors.TryAdd(parkingSensor.Id, parkingSensor);

                foreach (var parkingSpaceGroup in parkingOperator.ParkingSpaceGroups)
                    parkingSpaceGroups.TryAdd(parkingSpaceGroup.Id, parkingSpaceGroup);

            }

            // The parking spaces charging station operators define where no parking operator does.
            foreach (var chargingStationOperator in chargingStationOperators)
                foreach (var parkingSpace in chargingStationOperator.ParkingSpaces)
                    parkingSpaces.TryAdd(parkingSpace.Id, parkingSpace);

            #endregion

        }

        #endregion


        #region Data/(Admin-)Status management

        private readonly Lock                                     statusEventsLock  = new ();
        private          OnRoamingNetworkStatusChangedDelegate?       onStatusChanged;
        private          OnRoamingNetworkAdminStatusChangedDelegate?  onAdminStatusChanged;

        /// <summary>
        /// An event fired whenever the dynamic status changed.
        /// </summary>
        public event OnRoamingNetworkStatusChangedDelegate?       OnStatusChanged
        {

            add
            {
                lock (statusEventsLock)
                {
                    onStatusChanged += value;
                    ListenToStatus(onStatusChanged is not null);
                }
            }

            remove
            {
                lock (statusEventsLock)
                {
                    onStatusChanged -= value;
                    ListenToStatus(onStatusChanged is not null);
                }
            }

        }

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnRoamingNetworkAdminStatusChangedDelegate?  OnAdminStatusChanged
        {

            add
            {
                lock (statusEventsLock)
                {
                    onAdminStatusChanged += value;
                    ListenToAdminStatus(onAdminStatusChanged is not null);
                }
            }

            remove
            {
                lock (statusEventsLock)
                {
                    onAdminStatusChanged -= value;
                    ListenToAdminStatus(onAdminStatusChanged is not null);
                }
            }

        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                              Timestamp,
                                                                   EventTracking_Id                            EventTrackingId,
                                                                   Timestamped<RoamingNetworkAdminStatusType>  NewStatus,
                                                                   Timestamped<RoamingNetworkAdminStatusType>  OldStatus,
                                                                   Context?                                    DataSource)
        {

            var onAdminStatusChanged = this.onAdminStatusChanged;

            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, NewStatus, OldStatus, DataSource);

        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                         Timestamp,
                                                              EventTracking_Id                       EventTrackingId,
                                                              Timestamped<RoamingNetworkStatusType>  NewStatus,
                                                              Timestamped<RoamingNetworkStatusType>  OldStatus,
                                                              Context?                               DataSource)
        {

            var onStatusChanged = this.onStatusChanged;

            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, NewStatus, OldStatus, DataSource);

        }

        #endregion



        #region Parse    (JSON, ...)

        /// <summary>
        /// Parse the given JSON representation of a roaming network.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CustomRoamingNetworkParser">An optional delegate to parse custom roaming network JSON objects.</param>
        public static RoamingNetwork Parse(JObject                                       JSON,
                                           CustomJObjectParserDelegate<RoamingNetwork>?  CustomRoamingNetworkParser   = null)
        {

            if (TryParse(JSON,
                         out var roamingNetwork,
                         out var errorResponse,
                         CustomRoamingNetworkParser))
            {
                return roamingNetwork;
            }

            throw new ArgumentException("The given JSON representation of a roaming network is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region TryParse (JSON, out RoamingNetwork, out ErrorResponse, ...)

        /// <summary>
        /// Try to parse the given JSON representation of a roaming network.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="RoamingNetwork">The parsed roaming network.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomRoamingNetworkParser">An optional delegate to parse custom roaming network JSON objects.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out RoamingNetwork?      RoamingNetwork,
                                       [NotNullWhen(false)] out String?              ErrorResponse,
                                       CustomJObjectParserDelegate<RoamingNetwork>?  CustomRoamingNetworkParser   = null)
        {

            RoamingNetwork  = null;
            ErrorResponse   = null;

            try
            {

                if (JSON?.HasValues != true)
                {
                    ErrorResponse = "The given JSON object must not be null or empty!";
                    return false;
                }

                #region Parse Id             [mandatory]

                if (!JSON.ParseMandatory("@id",
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Check JSON-LD context [optional]

                if (JSON["@context"] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the roaming network '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                #endregion

                #region Parse Name           [optional]

                if (JSON.ParseOptionalJSON("name",
                                           "network name",
                                           ImmutableI18NString.TryParse,
                                           out ImmutableI18NString? name,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse Description    [optional]

                if (JSON.ParseOptionalJSON("description",
                                           "network description",
                                           I18NString.TryParse,
                                           out I18NString? description,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse DataSource     [optional]

                if (JSON.ParseOptional("dataSource",
                                       "data source",
                                       out String? dataSource,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse Created, LastChange, CustomData [optional]

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse DataLicenses   [optional]

                if (!POIDataLicenses.TryParse(JSON["dataLicenses"],
                                              JSON["dataLicenseIds"],
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse SigningKeys, IdentityKeys [optional]

                if (!POIPartyKeys.TryParse(JSON, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                #endregion

                #region Parse ChargingStationManufacturers [optional]

                if (!POIJSON.TryParseChildren<ChargingStationManufacturer>(JSON,
                                                                           "chargingStationManufacturers",
                                                                           "chargingStationManufacturerIds",
                                                                           "charging station manufacturers",
                                                                           ChargingStationManufacturer.TryParse,
                                                                           out var chargingStationManufacturers,
                                                                           out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse EMobilityProviders, GridOperators [optional]

                if (!POIJSON.TryParseChildren<EMobilityProvider>(JSON,
                                                                 "eMobilityProviders",
                                                                 "eMobilityProviderIds",
                                                                 "e-mobility providers",
                                                                 EMobilityProvider.TryParse,
                                                                 out var eMobilityProviders,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<GridOperator>     (JSON,
                                                                 "gridOperators",
                                                                 "gridOperatorIds",
                                                                 "grid operators",
                                                                 GridOperator.TryParse,
                                                                 out var gridOperators,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<ParkingOperator>  (JSON,
                                                                 "parkingOperators",
                                                                 "parkingOperatorIds",
                                                                 "parking operators",
                                                                 ParkingOperator.TryParse,
                                                                 out var parkingOperators,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<TransparencySoftware>(JSON,
                                                                 "transparencySoftware",
                                                                 "transparencySoftwareIds",
                                                                 "transparency software releases",
                                                                 POI.TransparencySoftware.TryParse,
                                                                 out var transparencySoftware,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<TransparencySoftwareCertificate>(JSON,
                                                                 "transparencySoftwareCertificates",
                                                                 "transparencySoftwareCertificateIds",
                                                                 "transparency software certificates",
                                                                 TransparencySoftwareCertificate.TryParse,
                                                                 out var transparencySoftwareCertificates,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<Brand>            (JSON,
                                                                 "brands",
                                                                 "brandIds",
                                                                 "brands",
                                                                 POI.Brand.TryParse,
                                                                 out var brands,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<EnergyMeterManufacturer>(JSON,
                                                                 "energyMeterManufacturers",
                                                                 "energyMeterManufacturerIds",
                                                                 "energy meter manufacturers",
                                                                 EnergyMeterManufacturer.TryParse,
                                                                 out var energyMeterManufacturers,
                                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ChargingStationOperators [optional]

                if (JSON.ContainsKey("chargingStationOperatorIds"))
                {
                    ErrorResponse = "Charging station operators given only by their identifications can not be read; they must be given completely as 'chargingStationOperators'!";
                    return false;
                }

                var chargingStationOperators = new List<ChargingStationOperator>();

                if (JSON["chargingStationOperators"] is JToken chargingStationOperatorsJSON)
                {

                    if (chargingStationOperatorsJSON is not JArray chargingStationOperatorsArray)
                    {
                        ErrorResponse = "The charging station operators must be a JSON array!";
                        return false;
                    }

                    foreach (var chargingStationOperatorJSON in chargingStationOperatorsArray)
                    {

                        if (chargingStationOperatorJSON is not JObject chargingStationOperatorObject)
                        {
                            ErrorResponse = "A charging station operator must be a JSON object!";
                            return false;
                        }

                        if (!ChargingStationOperator.TryParse(chargingStationOperatorObject,
                                                              out var chargingStationOperator,
                                                              out var chargingStationOperatorError))
                        {
                            ErrorResponse = chargingStationOperatorError;
                            return false;
                        }

                        chargingStationOperators.Add(chargingStationOperator);

                    }

                }

                #endregion


                RoamingNetwork = new RoamingNetwork(
                                     id,
                                     name,
                                     description,
                                     DataSource:                dataSource,
                                     Created:                   created,
                                     LastChange:                lastChange,
                                     CustomData:                customData,
                                     DataLicenses:              dataLicenses,
                                     ChargingStationOperators:  chargingStationOperators,
                                     ChargingStationManufacturers:  chargingStationManufacturers,
                                     EMobilityProviders:        eMobilityProviders,
                                     GridOperators:             gridOperators,
                                     ParkingOperators:          parkingOperators,
                                     TransparencySoftware:      transparencySoftware,
                                     TransparencySoftwareCertificates:  transparencySoftwareCertificates,
                                     Brands:                    brands,
                                     EnergyMeterManufacturers:  energyMeterManufacturers,
                                     SigningKeys:               signingKeys,
                                     IdentityKeys:              identityKeys
                                 );

                #region Check ETags          [optional]

                if (JSON["eTags"] is JToken eTagsJSON &&
                    !TryCheckETags(RoamingNetwork,
                                   eTagsJSON is JArray eTagsArray
                                       ? [ .. eTagsArray.Select(eTag => ETag.Parse(eTag)) ]
                                       : [],
                                   out ErrorResponse))
                {
                    RoamingNetwork = null;
                    return false;
                }

                #endregion

                if (CustomRoamingNetworkParser is not null)
                    RoamingNetwork = CustomRoamingNetworkParser(JSON,
                                                                RoamingNetwork);

                return true;

            }
            catch (Exception e)
            {
                RoamingNetwork  = null;
                ErrorResponse   = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON   (Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation of the given roaming network.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        public JObject ToJSON(Boolean                                                    Embedded                                  = false,

                              InfoStatus                                                 ExpandRoamingNetworkIds                   = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandChargingStationOperatorIds          = InfoStatus.Expanded,
                              InfoStatus                                                 ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandDataLicenses                        = InfoStatus.Expanded,
                              InfoStatus                                                 ExpandEMobilityProviderId                 = InfoStatus.ShowIdOnly,

                              CustomJObjectSerializerDelegate<RoamingNetwork>?           CustomRoamingNetworkSerializer            = null,
                              CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null,
                              //CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                              //CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                              //CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null,

                              Boolean                                                    IncludeETags                              = true,
                              ETagDigestEncoding?                                        ETagDigestEncoding                        = null)

        {

            var JSON = JSONObject.Create(

                               new JProperty("@id",             Id.         ToString()),

                         !Embedded
                             ? new JProperty("@context",        JSONLDContext)
                             : null,

                         Name.IsNotNullOrEmpty()
                             ? new JProperty("name",            Name.       ToJSON())
                             : null,

                         Description.IsNotNullOrEmpty()
                             ? new JProperty("description",     Description.ToJSON())
                             : null,

                         DataSource is not null && DataSource.IsNeitherNullNorEmpty()
                             ? new JProperty("dataSource",      DataSource)
                             : null,

                         DataLicenses.Any() && ExpandDataLicenses != InfoStatus.Hidden
                             ? ExpandDataLicenses.Switch(
                                 () => new JProperty(
                                           "dataLicenseIds",
                                           DataLicenses.ToPOIIdsJSON()
                                       ),
                                 () => new JProperty(
                                           "dataLicenses",
                                           DataLicenses.ToPOIJSON()
                                       ))
                             : null,

                         ChargingStationOperators.Any() && ExpandChargingStationOperatorIds != InfoStatus.Hidden
                             ? ExpandChargingStationOperatorIds.Switch(

                                   () => new JProperty(
                                             "chargingStationOperatorIds",
                                             new JArray(
                                                 chargingStationOperators.
                                                     Select(chargingStationOperator => chargingStationOperator.Id.ToString())
                                             )
                                         ),

                                   () => new JProperty(
                                             "chargingStationOperators",
                                             new JArray(
                                                 chargingStationOperators.
                                                     Select(chargingStationOperator => chargingStationOperator.ToJSON(
                                                                                           Embedded:                                 true,
                                                                                           ExpandChargingPoolIds:                    ExpandChargingPoolIds,
                                                                                           ExpandChargingStationIds:                 ExpandChargingStationIds,
                                                                                           ExpandEVSEIds:                            ExpandEVSEIds,
                                                                                           ExpandBrandIds:                           ExpandBrandIds,
                                                                                           ExpandDataLicenses:                       ExpandDataLicenses,
                                                                                           CustomChargingStationOperatorSerializer:  CustomChargingStationOperatorSerializer
                                                                                       ))
                                             )
                                         ))

                             : null

            //             !ChargingStationOperators.Any() || ExpandChargingStationOperatorIds == InfoStatus.Expanded
            //                 ? null
            //                 : ExpandChargingPoolIds.Switch(
            //                       () => new JProperty("chargingPoolIds",             new JArray(ChargingPools.Select(pool => pool.Id).
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("chargingPools",               new JArray(ChargingPools.
            //                                                                                                    OrderBy(pool => pool).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:           InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationOperatorId:  InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationIds:         InfoStatus.Hidden,
            //                                                                                                            ExpandEVSEIds:                    InfoStatus.Hidden,
            //                                                                                                            ExpandBrandIds:                   InfoStatus.Hidden,
            //                                                                                                            ExpandDataLicenses:               InfoStatus.Hidden,
            //                                                                                                            CustomChargingPoolSerializer:     CustomChargingPoolSerializer,
            //                                                                                                            CustomChargingStationSerializer:  CustomChargingStationSerializer,
            //                                                                                                            CustomEVSESerializer:             CustomEVSESerializer)))),

            //             !ChargingStationOperators.Any() || (ExpandChargingPoolIds == InfoStatus.Expanded || ExpandChargingStationOperatorIds == InfoStatus.Expanded)
            //                 ? null
            //                 : ExpandChargingStationIds.Switch(
            //                       () => new JProperty("chargingStationIds",          new JArray(ChargingStationIds().
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("chargingStations",            new JArray(ChargingStations.
            //                                                                                                    OrderBy(station => station).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:           InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationOperatorId:  InfoStatus.Hidden,
            //                                                                                                            ExpandChargingPoolId:             InfoStatus.Hidden,
            //                                                                                                            ExpandEVSEIds:                    InfoStatus.Hidden,
            //                                                                                                            ExpandBrandIds:                   InfoStatus.Hidden,
            //                                                                                                            ExpandDataLicenses:               InfoStatus.Hidden,
            //                                                                                                            CustomChargingStationSerializer:  CustomChargingStationSerializer,
            //                                                                                                            CustomEVSESerializer:             CustomEVSESerializer)))),

            //             !ChargingStationOperators.Any() || (ExpandChargingStationIds == InfoStatus.Expanded || ExpandChargingPoolIds == InfoStatus.Expanded || ExpandChargingStationOperatorIds == InfoStatus.Expanded)
            //                 ? null
            //                 : ExpandEVSEIds.Switch(
            //                       () => new JProperty("EVSEIds",                     new JArray(EVSEIds().
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("EVSEs",                       new JArray(EVSEs.
            //                                                                                                    OrderBy(evse => evse).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:           InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationOperatorId:  InfoStatus.Hidden,
            //                                                                                                            ExpandChargingPoolId:             InfoStatus.Hidden,
            //                                                                                                            ExpandChargingStationId:          InfoStatus.Hidden,
            //                                                                                                            ExpandBrandIds:                   InfoStatus.Hidden,
            //                                                                                                            ExpandDataLicenses:               InfoStatus.Hidden)))),


            //             EMobilityProviders.Any()
            //                 ? ExpandEMobilityProviderId.Switch(
            //                       () => new JProperty("eMobilityProviderIds",        new JArray(EMobilityProviders.Select(emp => emp.Id).
            //                                                                                                    OrderBy(id => id).
            //                                                                                                    Select (id => id.ToString()))),

            //                       () => new JProperty("eMobilityProviders",          new JArray(EMobilityProviders.
            //                                                                                                    OrderBy(emp => emp).
            //                                                                                                    ToJSON (Embedded: true,
            //                                                                                                            ExpandRoamingNetworkId:           InfoStatus.Hidden,
            //                                                                                                            ExpandBrandIds:                   ExpandBrandIds,
            //                                                                                                            ExpandDataLicenses:               ExpandDataLicenses))))
            //                 : null

            //             );

            //JSON["gridOperators"] = POIJSON.Children(GridOperators);
            //JSON["transparencySoftware"] = POIJSON.Values(TransparencySoftware, value => value.ToJSON());
            //JSON["transparencySoftwareCertificates"] = POIJSON.Values(TransparencySoftwareCertificates, value => value.ToJSON());
            //JSON["parkingOperators"] = POIJSON.Children(ParkingOperators);
            //JSON["chargingStationManufacturers"] = POIJSON.Children(ChargingStationManufacturers);

            //return POIRepresentation.AddETags(this, CustomRoamingNetworkSerializer is not null
            //           ? CustomRoamingNetworkSerializer(this, JSON)
            //           : JSON);

                             );

            // Always part of the content.
            if (!chargingStationManufacturers.IsEmpty)
                JSON.Add("chargingStationManufacturers", new JArray(chargingStationManufacturers.Select(chargingStationManufacturer => chargingStationManufacturer.ToJSON())));

            if (!eMobilityProviders.IsEmpty)
                JSON.Add("eMobilityProviders",           new JArray(eMobilityProviders.Select(eMobilityProvider => eMobilityProvider.ToJSON())));

            if (!gridOperators.IsEmpty)
                JSON.Add("gridOperators",                new JArray(gridOperators.Select(gridOperator => gridOperator.ToJSON())));

            if (!parkingOperators.IsEmpty)
                JSON.Add("parkingOperators",             new JArray(parkingOperators.Select(parkingOperator => parkingOperator.ToJSON())));

            if (!brands.IsEmpty)
                JSON.Add("brands",                       new JArray(brands.Select(brand => brand.ToJSON())));

            if (!energyMeterManufacturers.IsEmpty)
                JSON.Add("energyMeterManufacturers",     new JArray(energyMeterManufacturers.Select(energyMeterManufacturer => energyMeterManufacturer.ToJSON())));

            if (!transparencySoftware.IsEmpty)
                JSON.Add("transparencySoftware",         new JArray(transparencySoftware.Select(release => release.ToJSON())));

            if (!transparencySoftwareCertificates.IsEmpty)
                JSON.Add("transparencySoftwareCertificates", new JArray(transparencySoftwareCertificates.Select(certificate => certificate.ToJSON())));

            POIPartyKeys.AddTo(JSON, this);

            POIEntityMetadata.AddTo(JSON, Created, LastChangeDate, CustomData);

            // The ETags describe the content without them, irrespective of the view options.
            if (IncludeETags && ETags.Length > 0)
                JSON.Add("eTags",  new JArray(ETags.Select(eTag => eTag.ToJSON(ETagDigestEncoding ?? POI.ETagDigestEncoding.HEX))));

            return CustomRoamingNetworkSerializer is not null
                       ? CustomRoamingNetworkSerializer(this, JSON)
                       : JSON;

        }

        #endregion


        #region Parse    (CBOR, ...)

        /// <summary>
        /// Parse the given CBOR representation of a roaming network.
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="CustomRoamingNetworkParser">An optional delegate to parse custom roaming network CBOR maps.</param>
        public static RoamingNetwork Parse(CBORValue                                  CBOR,
                                           CustomCBORParserDelegate<RoamingNetwork>?  CustomRoamingNetworkParser   = null)
        {

            if (TryParse(CBOR,
                         out var roamingNetwork,
                         out var errorResponse,
                         CustomRoamingNetworkParser))
            {
                return roamingNetwork;
            }

            throw new ArgumentException("The given CBOR representation of a roaming network is invalid: " + errorResponse,
                                        nameof(CBOR));

        }

        #endregion

        #region TryParse (CBOR, out RoamingNetwork, out ErrorResponse, ...)

        /// <summary>
        /// Try to parse the given CBOR representation of a roaming network.
        /// It uses the same keys as the JSON representation.
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="RoamingNetwork">The parsed roaming network.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomRoamingNetworkParser">An optional delegate to parse custom roaming network CBOR maps.</param>
        public static Boolean TryParse(CBORValue                                  CBOR,
                                       [NotNullWhen(true)]  out RoamingNetwork?   RoamingNetwork,
                                       [NotNullWhen(false)] out String?           ErrorResponse,
                                       CustomCBORParserDelegate<RoamingNetwork>?  CustomRoamingNetworkParser   = null)
        {

            RoamingNetwork  = null;
            ErrorResponse   = null;

            try
            {

                #region Parse Id             [mandatory]

                if (!CBOR.ParseMandatory("@id",
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Check JSON-LD context [optional]

                if (CBOR.TryGetValue(CBORValue.FromText("@context"), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the roaming network '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                #endregion

                #region Parse Name           [optional]

                if (CBOR.ParseOptional("name",
                                       "network name",
                                       I18NString.TryParse,
                                       out I18NString? name,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse Description    [optional]

                if (CBOR.ParseOptional("description",
                                       "network description",
                                       I18NString.TryParse,
                                       out I18NString? description,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse DataSource     [optional]

                if (CBOR.ParseOptionalText("dataSource",
                                           "data source",
                                           out var dataSource,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse Created, LastChange, CustomData [optional]

                var metadataJSON = new JObject();

                foreach (var key in new[] { "created", "lastChange", "customData" })
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var metadataCBOR))
                        metadataJSON.Add(key, CBORJSON.ToJSON(metadataCBOR));

                if (!POIEntityMetadata.TryParse(metadataJSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse DataLicenses   [optional]

                JToken? dataLicensesJSON    = null;
                JToken? dataLicenseIdsJSON  = null;

                if (CBOR.TryGetValue(CBORValue.FromText("dataLicenses"),   out var dataLicensesCBOR))
                    dataLicensesJSON    = CBORJSON.ToJSON(dataLicensesCBOR);

                if (CBOR.TryGetValue(CBORValue.FromText("dataLicenseIds"), out var dataLicenseIdsCBOR))
                    dataLicenseIdsJSON  = CBORJSON.ToJSON(dataLicenseIdsCBOR);

                if (!POIDataLicenses.TryParse(dataLicensesJSON,
                                              dataLicenseIdsJSON,
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse SigningKeys, IdentityKeys [optional]

                var keysJSON = new JObject();

                foreach (var keysProperty in new[] { POIPartyKeys.SigningKeysProperty, POIPartyKeys.IdentityKeysProperty })
                    if (CBOR.TryGetValue(CBORValue.FromText(keysProperty), out var keysCBOR))
                        keysJSON.Add(keysProperty, CBORJSON.ToJSON(keysCBOR));

                if (!POIPartyKeys.TryParse(keysJSON, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                #endregion

                #region Parse ChargingStationManufacturers, EMobilityProviders, GridOperators [optional]

                if (!TryParseCBORChildren<ChargingStationManufacturer>(CBOR,
                                                                       "chargingStationManufacturers",
                                                                       "chargingStationManufacturerIds",
                                                                       "charging station manufacturers",
                                                                       ChargingStationManufacturer.TryParse,
                                                                       out var chargingStationManufacturers,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<EMobilityProvider>          (CBOR,
                                                                       "eMobilityProviders",
                                                                       "eMobilityProviderIds",
                                                                       "e-mobility providers",
                                                                       EMobilityProvider.TryParse,
                                                                       out var eMobilityProviders,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<GridOperator>               (CBOR,
                                                                       "gridOperators",
                                                                       "gridOperatorIds",
                                                                       "grid operators",
                                                                       GridOperator.TryParse,
                                                                       out var gridOperators,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<ParkingOperator>            (CBOR,
                                                                       "parkingOperators",
                                                                       "parkingOperatorIds",
                                                                       "parking operators",
                                                                       ParkingOperator.TryParse,
                                                                       out var parkingOperators,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<TransparencySoftware>       (CBOR,
                                                                       "transparencySoftware",
                                                                       "transparencySoftwareIds",
                                                                       "transparency software releases",
                                                                       POI.TransparencySoftware.TryParse,
                                                                       out var transparencySoftware,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<TransparencySoftwareCertificate>(CBOR,
                                                                       "transparencySoftwareCertificates",
                                                                       "transparencySoftwareCertificateIds",
                                                                       "transparency software certificates",
                                                                       TransparencySoftwareCertificate.TryParse,
                                                                       out var transparencySoftwareCertificates,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<Brand>                      (CBOR,
                                                                       "brands",
                                                                       "brandIds",
                                                                       "brands",
                                                                       POI.Brand.TryParse,
                                                                       out var brands,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<EnergyMeterManufacturer>    (CBOR,
                                                                       "energyMeterManufacturers",
                                                                       "energyMeterManufacturerIds",
                                                                       "energy meter manufacturers",
                                                                       EnergyMeterManufacturer.TryParse,
                                                                       out var energyMeterManufacturers,
                                                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ChargingStationOperators [optional]

                if (CBOR.TryGetValue(CBORValue.FromText("chargingStationOperatorIds"), out _))
                {
                    ErrorResponse = "Charging station operators given only by their identifications can not be read; they must be given completely as 'chargingStationOperators'!";
                    return false;
                }

                var chargingStationOperators = new List<ChargingStationOperator>();

                if (CBOR.TryGetValue(CBORValue.FromText("chargingStationOperators"), out var chargingStationOperatorsCBOR))
                {

                    if (chargingStationOperatorsCBOR.Kind != CBORValueKind.Array)
                    {
                        ErrorResponse = "The charging station operators must be a CBOR array!";
                        return false;
                    }

                    foreach (var chargingStationOperatorCBOR in chargingStationOperatorsCBOR.AsArray())
                    {

                        if (!ChargingStationOperator.TryParse(chargingStationOperatorCBOR,
                                                              out var chargingStationOperator,
                                                              out var chargingStationOperatorError))
                        {
                            ErrorResponse = chargingStationOperatorError;
                            return false;
                        }

                        chargingStationOperators.Add(chargingStationOperator);

                    }

                }

                #endregion


                RoamingNetwork = new RoamingNetwork(
                                     id,
                                     name,
                                     description,
                                     DataSource:                dataSource,
                                     Created:                   created,
                                     LastChange:                lastChange,
                                     CustomData:                customData,
                                     DataLicenses:              dataLicenses,
                                     ChargingStationOperators:  chargingStationOperators,
                                     ChargingStationManufacturers:  chargingStationManufacturers,
                                     EMobilityProviders:        eMobilityProviders,
                                     GridOperators:             gridOperators,
                                     ParkingOperators:          parkingOperators,
                                     TransparencySoftware:      transparencySoftware,
                                     TransparencySoftwareCertificates:  transparencySoftwareCertificates,
                                     Brands:                    brands,
                                     EnergyMeterManufacturers:  energyMeterManufacturers,
                                     SigningKeys:               signingKeys,
                                     IdentityKeys:              identityKeys
                                 );

                #region Check ETags          [optional]

                if (CBOR.TryGetValue(CBORValue.FromText("eTags"), out var eTagsCBOR) &&
                    !TryCheckETags(RoamingNetwork,
                                   eTagsCBOR.Kind == CBORValueKind.Array
                                       ? [ .. eTagsCBOR.AsArray().Select(eTag => ETag.Parse(eTag)) ]
                                       : [],
                                   out ErrorResponse))
                {
                    RoamingNetwork = null;
                    return false;
                }

                #endregion

                if (CustomRoamingNetworkParser is not null)
                    RoamingNetwork = CustomRoamingNetworkParser(CBOR,
                                                                RoamingNetwork);

                return true;

            }
            catch (Exception e)
            {
                RoamingNetwork  = null;
                ErrorResponse   = e.Message;
                return false;
            }

        }

        #endregion

        #region ToCBOR   (Embedded = false, ...)

        /// <summary>
        /// Return a CBOR representation of the given roaming network,
        /// using the same keys as its JSON representation.
        /// Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomRoamingNetworkSerializer">An optional delegate to customize the CBOR representation.</param>
        public CBORValue ToCBOR(Boolean                                        Embedded                         = false,
                                CustomCBORSerializerDelegate<RoamingNetwork>?  CustomRoamingNetworkSerializer   = null,
                                Boolean                                        IncludeETags                     = true)
        {

            var CBOR = new CBORMap {
                           { CBORValue.FromText("@id"),               CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                CBOR.Add(CBORValue.FromText("@context"),              CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                CBOR.Add(CBORValue.FromText("name"),                  Name.ToCBOR());

            if (Description.IsNotNullOrEmpty())
                CBOR.Add(CBORValue.FromText("description"),           Description.ToCBOR());

            if (DataSource is not null && DataSource.IsNeitherNullNorEmpty())
                CBOR.Add(CBORValue.FromText("dataSource"),            CBORValue.FromText(DataSource));

            if (DataLicenses.Any())
                CBOR.Add(CBORValue.FromText("dataLicenses"),          CBORJSON.ToCBOR(DataLicenses.ToPOIJSON()));

            if (ChargingStationOperators.Any())
                CBOR.Add(CBORValue.FromText("chargingStationOperators"),
                         CBORValue.FromArray(chargingStationOperators.
                                                 Select(chargingStationOperator => chargingStationOperator.ToCBOR(Embedded: true))));

            if (!chargingStationManufacturers.IsEmpty)
                CBOR.Add(CBORValue.FromText("chargingStationManufacturers"),
                         CBORValue.FromArray(chargingStationManufacturers.
                                                 Select(chargingStationManufacturer => chargingStationManufacturer.ToCBOR())));

            if (!eMobilityProviders.IsEmpty)
                CBOR.Add(CBORValue.FromText("eMobilityProviders"),
                         CBORValue.FromArray(eMobilityProviders.
                                                 Select(eMobilityProvider => eMobilityProvider.ToCBOR())));

            if (!gridOperators.IsEmpty)
                CBOR.Add(CBORValue.FromText("gridOperators"),
                         CBORValue.FromArray(gridOperators.
                                                 Select(gridOperator => gridOperator.ToCBOR())));

            if (!parkingOperators.IsEmpty)
                CBOR.Add(CBORValue.FromText("parkingOperators"),
                         CBORValue.FromArray(parkingOperators.
                                                 Select(parkingOperator => parkingOperator.ToCBOR())));

            if (!brands.IsEmpty)
                CBOR.Add(CBORValue.FromText("brands"),
                         CBORValue.FromArray(brands.
                                                 Select(brand => CBORJSON.ToCBOR(brand.ToJSON()))));

            if (!energyMeterManufacturers.IsEmpty)
                CBOR.Add(CBORValue.FromText("energyMeterManufacturers"),
                         CBORValue.FromArray(energyMeterManufacturers.
                                                 Select(energyMeterManufacturer => energyMeterManufacturer.ToCBOR())));

            if (!transparencySoftware.IsEmpty)
                CBOR.Add(CBORValue.FromText("transparencySoftware"),
                         CBORValue.FromArray(transparencySoftware.
                                                 Select(release => CBORJSON.ToCBOR(release.ToJSON()))));

            if (!transparencySoftwareCertificates.IsEmpty)
                CBOR.Add(CBORValue.FromText("transparencySoftwareCertificates"),
                         CBORValue.FromArray(transparencySoftwareCertificates.
                                                 Select(certificate => CBORJSON.ToCBOR(certificate.ToJSON()))));

            foreach (var keys in POIPartyKeys.AddTo([], this).Properties())
                CBOR.Add(CBORValue.FromText(keys.Name), CBORJSON.ToCBOR(keys.Value));

            // Always part of the content, with the keys and values of the JSON representation.
            foreach (var metadata in POIEntityMetadata.AddTo([], Created, LastChangeDate, CustomData).Properties())
                CBOR.Add(CBORValue.FromText(metadata.Name),           CBORJSON.ToCBOR(metadata.Value));

            // The ETags describe the content without them, irrespective of the view options.
            if (IncludeETags && ETags.Length > 0)
                CBOR.Add(CBORValue.FromText("eTags"),                 CBORValue.FromArray(ETags.Select(eTag => eTag.ToCBOR())));

            return CustomRoamingNetworkSerializer is not null
                       ? CustomRoamingNetworkSerializer(this, CBOR)
                       : CBOR;

        }

        #endregion


        #region (private static) TryCheckETags(RoamingNetwork, DeclaredETags, out ErrorResponse)

        /// <summary>
        /// The ETags a representation declares must be the ETags of the roaming network read from it:
        /// stale or wrong ETags are refused, not ignored.
        /// </summary>
        /// <param name="RoamingNetwork">The roaming network read from the representation.</param>
        /// <param name="DeclaredETags">The ETags the representation declares.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        private static Boolean TryCheckETags(RoamingNetwork                    RoamingNetwork,
                                             ImmutableArray<ETag>              DeclaredETags,
                                             [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (MatchETags(DeclaredETags, RoamingNetwork, out var error))
            {
                ErrorResponse = null;
                return true;
            }

            ErrorResponse = $"The ETags of the roaming network '{RoamingNetwork.Id}' do not match its content: {error}";
            return false;

        }

        #endregion

        #region CalcETags()

        /// <summary>
        /// Calculate the ETags of the canonical JSON and CBOR representations of this roaming network,
        /// both without ETags, without view options and without custom serializers.
        /// </summary>
        public ImmutableArray<ETag> CalcETags()

            => CalcETags(
                   ToJSON(IncludeETags: false),
                   ToCBOR(IncludeETags: false)
               );

        #endregion





        #region (private static) TryParseCBORChildren<T>(CBOR, PropertyName, IdsPropertyName, Description, Parser, out Children, out ErrorResponse)

        /// <summary>
        /// The optional embedded children of the given CBOR representation, read with the keys and values of the JSON representation.
        /// </summary>
        private static Boolean TryParseCBORChildren<T>(CBORValue                         CBOR,
                                                       String                            PropertyName,
                                                       String                            IdsPropertyName,
                                                       String                            Description,
                                                       POIObjectParser<T>                Parser,
                                                       out ImmutableArray<T>             Children,
                                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            var json = new JObject();

            if (CBOR.TryGetValue(CBORValue.FromText(PropertyName),    out var childrenCBOR))
                json.Add(PropertyName,    CBORJSON.ToJSON(childrenCBOR));

            if (CBOR.TryGetValue(CBORValue.FromText(IdsPropertyName), out var childIdsCBOR))
                json.Add(IdsPropertyName, CBORJSON.ToJSON(childIdsCBOR));

            return POIJSON.TryParseChildren(json,
                                            PropertyName,
                                            IdsPropertyName,
                                            Description,
                                            Parser,
                                            out Children,
                                            out ErrorResponse);

        }

        #endregion


        #region E-mobility providers

        private readonly POICollection<EMobilityProvider_Id, EMobilityProvider> eMobilityProviders;

        /// <summary>
        /// The e-mobility providers in this immutable network version.
        /// </summary>
        public POICollection<EMobilityProvider_Id, EMobilityProvider> EMobilityProviders
            => eMobilityProviders;

        /// <summary>
        /// Try to get the e-mobility provider having the given identification.
        /// </summary>
        /// <param name="EMobilityProviderId">The identification of an e-mobility provider.</param>
        /// <param name="EMobilityProvider">The e-mobility provider.</param>
        public Boolean TryGetEMobilityProviderById(EMobilityProvider_Id                        EMobilityProviderId,
                                                   [NotNullWhen(true)] out EMobilityProvider?  EMobilityProvider)

            => eMobilityProviders.TryGet(EMobilityProviderId, out EMobilityProvider);

        #endregion


        #region Grid operators

        private readonly POICollection<GridOperator_Id, GridOperator> gridOperators;

        /// <summary>
        /// The grid operators in this immutable network version.
        /// </summary>
        public POICollection<GridOperator_Id, GridOperator> GridOperators
            => gridOperators;

        /// <summary>
        /// Try to get the grid operator having the given identification.
        /// </summary>
        /// <param name="GridOperatorId">The identification of a grid operator.</param>
        /// <param name="GridOperator">The grid operator.</param>
        public Boolean TryGetGridOperatorById(GridOperator_Id                        GridOperatorId,
                                              [NotNullWhen(true)] out GridOperator?  GridOperator)

            => gridOperators.TryGet(GridOperatorId, out GridOperator);

        /// <summary>
        /// The grid connection points of all grid operators.
        /// </summary>
        public IEnumerable<GridConnectionPoint> GridConnectionPoints
            => gridOperators.SelectMany(gridOperator => gridOperator.GridConnectionPoints);

        /// <summary>
        /// Try to get the grid connection point having the given identification, and its grid operator.
        /// </summary>
        /// <param name="GridConnectionPointId">The identification of a grid connection point, unique within the network.</param>
        /// <param name="GridOperator">Its grid operator.</param>
        /// <param name="GridConnectionPoint">The grid connection point.</param>
        public Boolean TryGetGridConnectionPoint(GridConnectionPoint_Id                        GridConnectionPointId,
                                                 [NotNullWhen(true)] out GridOperator?         GridOperator,
                                                 [NotNullWhen(true)] out GridConnectionPoint?  GridConnectionPoint)
        {

            foreach (var gridOperator in gridOperators)
                if (gridOperator.TryGetGridConnectionPoint(GridConnectionPointId, out GridConnectionPoint))
                {
                    GridOperator = gridOperator;
                    return true;
                }

            GridOperator         = null;
            GridConnectionPoint  = null;
            return false;

        }

        /// <summary>
        /// The error, when two of the given grid operators have a grid connection point with the same identification.
        /// </summary>
        internal static String? DuplicateGridConnectionPoint(IEnumerable<GridOperator> GridOperators)
        {

            var seen = new Dictionary<GridConnectionPoint_Id, GridOperator_Id>();

            foreach (var gridOperator in GridOperators)
                foreach (var gridConnectionPoint in gridOperator.GridConnectionPoints)
                    if (!seen.TryAdd(gridConnectionPoint.Id, gridOperator.Id))
                        return $"The grid connection point '{gridConnectionPoint.Id}' of the grid operator '{gridOperator.Id}' must be unique within the roaming network, but the grid operator '{seen[gridConnectionPoint.Id]}' has it too!";

            return null;

        }

        /// <summary>
        /// The error, when a charging pool names a grid connection point the given grid operators do not have.
        /// </summary>
        internal static String? UnknownGridConnectionPoint(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                           IEnumerable<GridOperator>             GridOperators)
        {

            var known = GridOperators.SelectMany(gridOperator => gridOperator.GridConnectionPoints).Select(gridConnectionPoint => gridConnectionPoint.Id).ToHashSet();

            foreach (var chargingPool in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingPools))
                if (chargingPool.GridConnectionPointId.HasValue &&
                    !known.Contains(chargingPool.GridConnectionPointId.Value))
                {
                    return $"The grid connection point '{chargingPool.GridConnectionPointId}' of the charging pool '{chargingPool.Id}' is unknown in the roaming network!";
                }

            return null;

        }

        /// <summary>
        /// The error, when a grid connection point is maintained by a charging station operator or parking operator the given ones do not have.
        /// </summary>
        internal static String? UnknownMaintainer(IEnumerable<GridOperator>                                                   GridOperators,
                                                  IReadOnlyDictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                  IReadOnlyDictionary<ParkingOperator_Id,         ParkingOperator>          ParkingOperators)
        {

            foreach (var gridOperator in GridOperators)
                foreach (var gridConnectionPoint in gridOperator.GridConnectionPoints)
                    if (gridConnectionPoint.MaintainedBy is POIPartyId maintainer &&
                        !(maintainer.EntityType switch {
                              nameof(ChargingStationOperator)  => ChargingStationOperator_Id.TryParse(maintainer.EntityId, out var chargingStationOperatorId) &&
                                                                  ChargingStationOperators.ContainsKey(chargingStationOperatorId),
                              nameof(ParkingOperator)          => ParkingOperator_Id.TryParse(maintainer.EntityId, out var parkingOperatorId) &&
                                                                  ParkingOperators.ContainsKey(parkingOperatorId),
                              _                                => false
                          }))
                    {
                        return $"The maintainer of the grid connection point '{gridConnectionPoint.Id}', {maintainer}, is unknown in the roaming network!";
                    }

            return null;

        }

        #endregion


        #region Parking operators, parking garages, parking spaces, parking sensors

        private readonly POICollection<ParkingOperator_Id, ParkingOperator>  parkingOperators;
        private readonly ConcurrentDictionary<ParkingGarage_Id,   ParkingGarage>    parkingGarages    = [];
        private readonly ConcurrentDictionary<ParkingSpace_Id,    ParkingSpace>     parkingSpaces     = [];
        private readonly ConcurrentDictionary<ParkingSensor_Id,   ParkingSensor>    parkingSensors    = [];

        /// <summary>
        /// The parking operators in this immutable network version.
        /// </summary>
        public POICollection<ParkingOperator_Id, ParkingOperator> ParkingOperators
            => parkingOperators;

        /// <summary>
        /// The parking garages of all parking operators.
        /// </summary>
        public IEnumerable<ParkingGarage>   ParkingGarages
            => parkingGarages.Values;

        /// <summary>
        /// The parking spaces of all parking operators.
        /// </summary>
        public IEnumerable<ParkingSpace>    ParkingSpaces
            => parkingSpaces.Values;

        /// <summary>
        /// The parking sensors of all parking operators.
        /// </summary>
        public IEnumerable<ParkingSensor>   ParkingSensors
            => parkingSensors.Values;

        /// <summary>
        /// Try to get the parking operator having the given identification.
        /// </summary>
        public Boolean TryGetParkingOperatorById(ParkingOperator_Id                        ParkingOperatorId,
                                                 [NotNullWhen(true)] out ParkingOperator?  ParkingOperator)

            => parkingOperators.TryGet(ParkingOperatorId, out ParkingOperator);

        /// <summary>
        /// The parking products of all parking operators.
        /// </summary>
        public IEnumerable<ParkingProduct> ParkingProducts
            => parkingOperators.SelectMany(parkingOperator => parkingOperator.ParkingProducts);

        /// <summary>
        /// Try to get the parking product having the given identification, of any parking operator.
        /// </summary>
        public Boolean TryGetParkingProductById(ParkingProduct_Id                        ParkingProductId,
                                                [NotNullWhen(true)] out ParkingProduct?  ParkingProduct)
        {

            foreach (var parkingOperator in parkingOperators)
                if (parkingOperator.TryGetParkingProduct(ParkingProductId, out ParkingProduct))
                    return true;

            ParkingProduct = null;
            return false;

        }

        /// <summary>
        /// Try to get the parking garage having the given identification, of any parking operator.
        /// </summary>
        public Boolean TryGetParkingGarageById(ParkingGarage_Id                        ParkingGarageId,
                                               [NotNullWhen(true)] out ParkingGarage?  ParkingGarage)

            => parkingGarages.TryGetValue(ParkingGarageId, out ParkingGarage);

        /// <summary>
        /// Try to get the parking space having the given identification, of any parking operator.
        /// </summary>
        public Boolean TryGetParkingSpaceById(ParkingSpace_Id                        ParkingSpaceId,
                                              [NotNullWhen(true)] out ParkingSpace?  ParkingSpace)

            => parkingSpaces.TryGetValue(ParkingSpaceId, out ParkingSpace);

        /// <summary>
        /// Try to get the parking sensor having the given identification, of any parking operator.
        /// </summary>
        public Boolean TryGetParkingSensorById(ParkingSensor_Id                        ParkingSensorId,
                                               [NotNullWhen(true)] out ParkingSensor?  ParkingSensor)

            => parkingSensors.TryGetValue(ParkingSensorId, out ParkingSensor);

        /// <summary>
        /// The error, when two of the given parking operators have a parking garage, space, sensor, space group or product
        /// with the same identification, or a parking space of a parking operator or charging station operator has the
        /// identification of another.
        /// </summary>
        internal static String? DuplicateParking(IEnumerable<ParkingOperator>           ParkingOperators,
                                                 IEnumerable<ChargingStationOperator>   ChargingStationOperators)
        {

            var garages = new HashSet<ParkingGarage_Id>();
            var spaces  = new HashSet<ParkingSpace_Id>();
            var sensors = new HashSet<ParkingSensor_Id>();
            var groups  = new HashSet<ParkingSpaceGroup_Id>();
            var products = new HashSet<ParkingProduct_Id>();

            foreach (var parkingOperator in ParkingOperators)
            {

                foreach (var parkingGarage in parkingOperator.ParkingGarages)
                    if (!garages.Add(parkingGarage.Id))
                        return $"The parking garage '{parkingGarage.Id}' must not be given twice in the roaming network!";

                foreach (var parkingSpace in parkingOperator.ParkingSpaces)
                    if (!spaces.Add(parkingSpace.Id))
                        return $"The parking space '{parkingSpace.Id}' must not be given twice in the roaming network!";

                foreach (var parkingSensor in parkingOperator.ParkingSensors)
                    if (!sensors.Add(parkingSensor.Id))
                        return $"The parking sensor '{parkingSensor.Id}' must not be given twice in the roaming network!";

                foreach (var parkingSpaceGroup in parkingOperator.ParkingSpaceGroups)
                    if (!groups.Add(parkingSpaceGroup.Id))
                        return $"The parking space group '{parkingSpaceGroup.Id}' must not be given twice in the roaming network!";

                foreach (var parkingProduct in parkingOperator.ParkingProducts)
                    if (!products.Add(parkingProduct.Id))
                        return $"The parking product '{parkingProduct.Id}' must not be given twice in the roaming network!";

            }

            foreach (var parkingSpace in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ParkingSpaces))
                if (!spaces.Add(parkingSpace.Id))
                    return $"The parking space '{parkingSpace.Id}' must not be given twice in the roaming network!";

            return null;

        }

        /// <summary>
        /// The error, when a parking garage, space or sensor names a charging station the given operators do not have,
        /// or a parking space of a charging station operator does.
        /// </summary>
        internal static String? UnknownChargingStation(IEnumerable<ParkingOperator>                                              ParkingOperators,
                                                       IReadOnlyDictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators)
        {

            Boolean Exists(ChargingStation_Id ChargingStationId)

                => ChargingStationOperators.TryGetValue(ChargingStationId.OperatorId, out var chargingStationOperator) &&
                   chargingStationOperator.TryGetChargingStation(ChargingStationId, out _);

            foreach (var parkingOperator in ParkingOperators)
            {

                var named = parkingOperator.ParkingGarages.SelectMany(parkingGarage => parkingGarage.ChargingStationIds.Select(id => (Kind: "parking garage", Id: parkingGarage.Id.ToString(), ChargingStationId: id))).
                            Concat(parkingOperator.ParkingSpaces. SelectMany(parkingSpace  => parkingSpace. ChargingStationIds.Select(id => (Kind: "parking space",  Id: parkingSpace. Id.ToString(), ChargingStationId: id)))).
                            Concat(parkingOperator.ParkingSensors.SelectMany(parkingSensor => parkingSensor.ChargingStationIds.Select(id => (Kind: "parking sensor", Id: parkingSensor.Id.ToString(), ChargingStationId: id))));

                foreach (var (kind, id, chargingStationId) in named)
                    if (!Exists(chargingStationId))
                        return $"The charging station '{chargingStationId}' of the {kind} '{id}' is unknown in the roaming network!";

            }

            foreach (var parkingSpace in ChargingStationOperators.Values.SelectMany(chargingStationOperator => chargingStationOperator.ParkingSpaces))
                foreach (var chargingStationId in parkingSpace.ChargingStationIds)
                    if (!Exists(chargingStationId))
                        return $"The charging station '{chargingStationId}' of the parking space '{parkingSpace.Id}' is unknown in the roaming network!";

            return null;

        }

        /// <summary>
        /// The error, when a charging station names a parking space neither the given parking operators nor the given
        /// charging station operators have.
        /// </summary>
        internal static String? UnknownParkingSpace(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                    IEnumerable<ParkingOperator>          ParkingOperators)
        {

            var parkingSpaceIds = ParkingOperators.SelectMany(parkingOperator => parkingOperator.ParkingSpaces).
                                                   Concat    (ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ParkingSpaces)).
                                                   Select    (parkingSpace    => parkingSpace.Id).
                                                   ToHashSet();

            foreach (var chargingStation in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingStations))
                foreach (var parkingSpaceId in chargingStation.ParkingSpaceIds)
                    if (!parkingSpaceIds.Contains(parkingSpaceId))
                        return $"The parking space '{parkingSpaceId}' of the charging station '{chargingStation.Id}' is unknown in the roaming network!";

            return null;

        }

        #endregion


        #region Brands

        private readonly POICollection<Brand_Id, Brand> brands;

        /// <summary>
        /// The brands in this immutable network version.
        /// </summary>
        public POICollection<Brand_Id, Brand> Brands
            => brands;

        /// <summary>
        /// Try to get the brand having the given identification.
        /// </summary>
        public Boolean TryGetBrandById(Brand_Id                        BrandId,
                                       [NotNullWhen(true)] out Brand?  Brand)

            => brands.TryGet(BrandId, out Brand);

        /// <summary>
        /// The error, when a charging pool, charging station or EVSE names a brand the given ones do not have.
        /// </summary>
        internal static String? UnknownBrand(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                             IReadOnlyDictionary<Brand_Id, Brand>  Brands)
        {

            foreach (var chargingStationOperator in ChargingStationOperators)
            {

                var named = chargingStationOperator.ChargingPools.   SelectMany(chargingPool    => chargingPool.   BrandIds.Select(brandId => (Kind: "charging pool",    Id: chargingPool.   Id.ToString(), BrandId: brandId))).
                            Concat(chargingStationOperator.ChargingStations.SelectMany(chargingStation => chargingStation.BrandIds.Select(brandId => (Kind: "charging station", Id: chargingStation.Id.ToString(), BrandId: brandId)))).
                            Concat(chargingStationOperator.EVSEs.           SelectMany(evse            => evse.           BrandIds.Select(brandId => (Kind: "EVSE",             Id: evse.           Id.ToString(), BrandId: brandId)))).
                            Concat(chargingStationOperator.ChargingPools.   SelectMany(chargingPool    => chargingPool.   AuthenticationModes.SelectMany(authenticationMode => authenticationMode.BrandIds).
                                                                                                                                       Select(brandId => (Kind: "RFID authentication of the charging pool", Id: chargingPool.Id.ToString(), BrandId: brandId))));

                foreach (var (kind, id, brandId) in named)
                    if (!Brands.ContainsKey(brandId))
                        return $"The brand '{brandId}' of the {kind} '{id}' is unknown in the roaming network!";

            }

            return null;

        }

        #endregion


        #region Transparency software releases and certificates

        private readonly POICollection<TransparencySoftware_Id, TransparencySoftware>             transparencySoftware;
        private readonly POICollection<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>  transparencySoftwareCertificates;

        /// <summary>
        /// The transparency software releases in this immutable network version: one per version of a software.
        /// </summary>
        public POICollection<TransparencySoftware_Id, TransparencySoftware>             TransparencySoftware
            => transparencySoftware;

        /// <summary>
        /// The certificates of transparency software releases in this immutable network version.
        /// </summary>
        public POICollection<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>  TransparencySoftwareCertificates
            => transparencySoftwareCertificates;

        /// <summary>
        /// Try to get the transparency software release having the given identification.
        /// </summary>
        public Boolean TryGetTransparencySoftwareById(TransparencySoftware_Id                        TransparencySoftwareId,
                                                      [NotNullWhen(true)] out TransparencySoftware?  TransparencySoftware)

            => transparencySoftware.TryGet(TransparencySoftwareId, out TransparencySoftware);

        /// <summary>
        /// Try to get the transparency software certificate having the given identification.
        /// </summary>
        public Boolean TryGetTransparencySoftwareCertificateById(TransparencySoftwareCertificate_Id                        CertificateId,
                                                                 [NotNullWhen(true)] out TransparencySoftwareCertificate?  Certificate)

            => transparencySoftwareCertificates.TryGet(CertificateId, out Certificate);

        /// <summary>
        /// The error, when a certificate names an unknown release or manufacturer,
        /// or an energy meter an unknown release, an unknown certificate, or a certificate not covering its release.
        /// </summary>
        internal static String? UnknownTransparencyReference(IEnumerable<EnergyMeter>                                                                EnergyMeters,
                                                             IReadOnlyDictionary<TransparencySoftware_Id,            TransparencySoftware>             TransparencySoftware,
                                                             IReadOnlyDictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>  Certificates,
                                                             IReadOnlyDictionary<ChargingStationManufacturer_Id,     ChargingStationManufacturer>      ChargingStationManufacturers)
        {

            foreach (var certificate in Certificates.Values)
            {

                foreach (var releaseId in certificate.VerifiedTransparencySoftwareIds.Concat(certificate.CompatibleTransparencySoftwareIds))
                    if (!TransparencySoftware.ContainsKey(releaseId))
                        return $"The transparency software release '{releaseId}' of the certificate '{certificate.Id}' is unknown in the roaming network!";

                if (certificate.ChargingStationManufacturerId.HasValue &&
                    !ChargingStationManufacturers.ContainsKey(certificate.ChargingStationManufacturerId.Value))
                {
                    return $"The manufacturer '{certificate.ChargingStationManufacturerId}' of the certificate '{certificate.Id}' is unknown in the roaming network!";
                }

            }

            foreach (var energyMeter in EnergyMeters)
                foreach (var assignment in energyMeter.TransparencySoftwareStatus)
                {

                    if (!TransparencySoftware.ContainsKey(assignment.TransparencySoftwareId))
                        return $"The transparency software release '{assignment.TransparencySoftwareId}' of the energy meter '{energyMeter.Id}' is unknown in the roaming network!";

                    if (assignment.CertificateId.HasValue)
                    {

                        if (!Certificates.TryGetValue(assignment.CertificateId.Value, out var certificate))
                            return $"The certificate '{assignment.CertificateId}' of the energy meter '{energyMeter.Id}' is unknown in the roaming network!";

                        if (!certificate.Covers(assignment.TransparencySoftwareId))
                            return $"The certificate '{certificate.Id}' of the energy meter '{energyMeter.Id}' does not cover the transparency software release '{assignment.TransparencySoftwareId}'!";

                    }

                }

            return null;

        }

        #endregion


        #region Groups

        /// <summary>
        /// The EVSEGroups of all charging station operators.
        /// </summary>
        public IEnumerable<EVSEGroup> EVSEGroups
            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.EVSEGroups);

        /// <summary>
        /// Try to get the EVSEGroup having the given identification, of the operator it names.
        /// </summary>
        public Boolean TryGetEVSEGroupById(EVSEGroup_Id  EVSEGroupId,
                                     [NotNullWhen(true)] out EVSEGroup?  EVSEGroup)
        {

            if (chargingStationOperators.TryGet(EVSEGroupId.OperatorId, out var chargingStationOperator))
                return chargingStationOperator.TryGetEVSEGroup(EVSEGroupId, out EVSEGroup);

            EVSEGroup = null;
            return false;

        }

        /// <summary>
        /// The ChargingStationGroups of all charging station operators.
        /// </summary>
        public IEnumerable<ChargingStationGroup> ChargingStationGroups
            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingStationGroups);

        /// <summary>
        /// Try to get the ChargingStationGroup having the given identification, of the operator it names.
        /// </summary>
        public Boolean TryGetChargingStationGroupById(ChargingStationGroup_Id  ChargingStationGroupId,
                                     [NotNullWhen(true)] out ChargingStationGroup?  ChargingStationGroup)
        {

            if (chargingStationOperators.TryGet(ChargingStationGroupId.OperatorId, out var chargingStationOperator))
                return chargingStationOperator.TryGetChargingStationGroup(ChargingStationGroupId, out ChargingStationGroup);

            ChargingStationGroup = null;
            return false;

        }

        /// <summary>
        /// The ChargingPoolGroups of all charging station operators.
        /// </summary>
        public IEnumerable<ChargingPoolGroup> ChargingPoolGroups
            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingPoolGroups);

        /// <summary>
        /// Try to get the ChargingPoolGroup having the given identification, of the operator it names.
        /// </summary>
        public Boolean TryGetChargingPoolGroupById(ChargingPoolGroup_Id  ChargingPoolGroupId,
                                     [NotNullWhen(true)] out ChargingPoolGroup?  ChargingPoolGroup)
        {

            if (chargingStationOperators.TryGet(ChargingPoolGroupId.OperatorId, out var chargingStationOperator))
                return chargingStationOperator.TryGetChargingPoolGroup(ChargingPoolGroupId, out ChargingPoolGroup);

            ChargingPoolGroup = null;
            return false;

        }

        /// <summary>
        /// The ChargingTariffGroups of all charging station operators.
        /// </summary>
        public IEnumerable<ChargingTariffGroup> ChargingTariffGroups
            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingTariffGroups);

        /// <summary>
        /// Try to get the ChargingTariffGroup having the given identification, of the operator it names.
        /// </summary>
        public Boolean TryGetChargingTariffGroupById(ChargingTariffGroup_Id  ChargingTariffGroupId,
                                     [NotNullWhen(true)] out ChargingTariffGroup?  ChargingTariffGroup)
        {

            if (chargingStationOperators.TryGet(ChargingTariffGroupId.OperatorId, out var chargingStationOperator))
                return chargingStationOperator.TryGetChargingTariffGroup(ChargingTariffGroupId, out ChargingTariffGroup);

            ChargingTariffGroup = null;
            return false;

        }

        private readonly ConcurrentDictionary<ParkingSpaceGroup_Id, ParkingSpaceGroup> parkingSpaceGroups = [];

        /// <summary>
        /// The parking space groups of all parking operators.
        /// </summary>
        public IEnumerable<ParkingSpaceGroup> ParkingSpaceGroups
            => parkingSpaceGroups.Values;

        /// <summary>
        /// Try to get the parking space group having the given identification, of any parking operator.
        /// </summary>
        public Boolean TryGetParkingSpaceGroupById(ParkingSpaceGroup_Id                        ParkingSpaceGroupId,
                                                   [NotNullWhen(true)] out ParkingSpaceGroup?  ParkingSpaceGroup)

            => parkingSpaceGroups.TryGetValue(ParkingSpaceGroupId, out ParkingSpaceGroup);

        #endregion


        #region Charging tariffs

        /// <summary>
        /// The charging tariffs of all charging station operators.
        /// </summary>
        public IEnumerable<ChargingTariff> ChargingTariffs
            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingTariffs);

        /// <summary>
        /// Try to get the charging tariff having the given identification, of the operator it names.
        /// </summary>
        /// <param name="ChargingTariffId">The identification of a charging tariff.</param>
        /// <param name="ChargingTariff">The charging tariff.</param>
        public Boolean TryGetChargingTariffById(ChargingTariff_Id                        ChargingTariffId,
                                                [NotNullWhen(true)] out ChargingTariff?  ChargingTariff)
        {

            if (chargingStationOperators.TryGet(ChargingTariffId.OperatorId, out var chargingStationOperator))
                return chargingStationOperator.TryGetChargingTariff(ChargingTariffId, out ChargingTariff);

            ChargingTariff = null;
            return false;

        }

        #endregion


        #region Energy meter manufacturers

        private readonly POICollection<EnergyMeterManufacturer_Id, EnergyMeterManufacturer> energyMeterManufacturers;

        /// <summary>
        /// The energy meter manufacturers in this immutable network version.
        /// </summary>
        public POICollection<EnergyMeterManufacturer_Id, EnergyMeterManufacturer> EnergyMeterManufacturers
            => energyMeterManufacturers;

        /// <summary>
        /// Try to get the energy meter manufacturer having the given identification.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId">The identification of an energy meter manufacturer.</param>
        /// <param name="EnergyMeterManufacturer">The energy meter manufacturer.</param>
        public Boolean TryGetEnergyMeterManufacturerById(EnergyMeterManufacturer_Id                        EnergyMeterManufacturerId,
                                                         [NotNullWhen(true)] out EnergyMeterManufacturer?  EnergyMeterManufacturer)

            => energyMeterManufacturers.TryGet(EnergyMeterManufacturerId, out EnergyMeterManufacturer);

        /// <summary>
        /// The error, when an energy meter of the given operators names a manufacturer not among the given ones.
        /// </summary>
        internal static String? UnknownEnergyMeterManufacturer(IEnumerable<EnergyMeter>                                                  EnergyMeters,
                                                               IReadOnlyDictionary<EnergyMeterManufacturer_Id, EnergyMeterManufacturer>  EnergyMeterManufacturers)
        {

            foreach (var energyMeter in EnergyMeters)
                if (energyMeter.ManufacturerId.HasValue &&
                    !EnergyMeterManufacturers.ContainsKey(energyMeter.ManufacturerId.Value))
                {
                    return $"The energy meter '{energyMeter.Id}' names the unknown energy meter manufacturer '{energyMeter.ManufacturerId}'!";
                }

            return null;

        }

        #endregion


        #region Energy meters

        private readonly ConcurrentDictionary<EnergyMeter_Id, EnergyMeter> energyMeters = [];

        /// <summary>
        /// The energy meters of all charging pools, charging stations and EVSEs, and of all grid connection points.
        /// </summary>
        public IEnumerable<EnergyMeter> EnergyMeters
            => energyMeters.Values;

        /// <summary>
        /// The energy meters of the given charging station operators and grid operators.
        /// </summary>
        internal static IEnumerable<EnergyMeter> AllEnergyMeters(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                                 IEnumerable<GridOperator>             GridOperators)

            => ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.EnergyMeters).
                   Concat(GridOperators.SelectMany(gridOperator => gridOperator.EnergyMeters));

        /// <summary>
        /// Try to get the energy meter having the given identification, anywhere in the network.
        /// </summary>
        /// <param name="EnergyMeterId">The identification of an energy meter.</param>
        /// <param name="EnergyMeter">The energy meter.</param>
        public Boolean TryGetEnergyMeterById(EnergyMeter_Id                        EnergyMeterId,
                                             [NotNullWhen(true)] out EnergyMeter?  EnergyMeter)

            => energyMeters.TryGetValue(EnergyMeterId, out EnergyMeter);

        /// <summary>
        /// The error, when two of the given energy meters have the same identification.
        /// </summary>
        internal static String? DuplicateEnergyMeter(IEnumerable<EnergyMeter> EnergyMeters)
        {

            var seen = new HashSet<EnergyMeter_Id>();

            foreach (var energyMeter in EnergyMeters)
                if (!seen.Add(energyMeter.Id))
                    return $"The energy meter '{energyMeter.Id}' must not be given twice in the roaming network!";

            return null;

        }

        #endregion


        #region Charging station manufacturers

        private readonly POICollection<ChargingStationManufacturer_Id, ChargingStationManufacturer> chargingStationManufacturers;

        /// <summary>
        /// The manufacturers of charging stations in this immutable network version.
        /// </summary>
        public POICollection<ChargingStationManufacturer_Id, ChargingStationManufacturer> ChargingStationManufacturers
            => chargingStationManufacturers;

        /// <summary>
        /// Try to get the charging station manufacturer having the given identification.
        /// </summary>
        /// <param name="ChargingStationManufacturerId">The identification of a charging station manufacturer.</param>
        /// <param name="ChargingStationManufacturer">The charging station manufacturer.</param>
        public Boolean TryGetChargingStationManufacturerById(ChargingStationManufacturer_Id                        ChargingStationManufacturerId,
                                                             [NotNullWhen(true)] out ChargingStationManufacturer?  ChargingStationManufacturer)

            => chargingStationManufacturers.TryGet(ChargingStationManufacturerId, out ChargingStationManufacturer);

        /// <summary>
        /// The error, when a charging station names a manufacturer the given ones do not have.
        /// </summary>
        internal static String? UnknownManufacturer(IEnumerable<ChargingStationOperator>                                              ChargingStationOperators,
                                                    IReadOnlyDictionary<ChargingStationManufacturer_Id, ChargingStationManufacturer>  ChargingStationManufacturers)
        {

            foreach (var chargingStation in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingStations))
                if (chargingStation.ManufacturerId.HasValue &&
                    !ChargingStationManufacturers.ContainsKey(chargingStation.ManufacturerId.Value))
                {
                    return $"The manufacturer '{chargingStation.ManufacturerId}' of the charging station '{chargingStation.Id}' is unknown in the roaming network!";
                }

            return null;

        }

        #endregion


        #region Charging station operators

        private readonly POICollection<ChargingStationOperator_Id, ChargingStationOperator> chargingStationOperators;

        /// <summary>
        /// The operators in this immutable network version.
        /// </summary>
        public POICollection<ChargingStationOperator_Id, ChargingStationOperator> ChargingStationOperators
            => chargingStationOperators;


        #region ChargingStationOperatorExists    (ChargingStationOperator)

        /// <summary>
        /// Check if the given charging station operator identification is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of the charging station operator.</param>
        public Boolean ChargingStationOperatorExists(ChargingStationOperator  ChargingStationOperator)
            => chargingStationOperators.Contains(ChargingStationOperator.Id);

        /// <summary>
        /// Determines whether the given user identification exists within this API.
        /// </summary>
        /// <param name="UserId">The unique identification of an user.</param>
        public Boolean ChargingStationOperatorExists(ChargingStationOperator_Id? ChargingStationOperatorId)

            => ChargingStationOperatorId.HasValue &&
               ChargingStationOperatorId.IsNotNullOrEmpty() &&
               chargingStationOperators.Contains(ChargingStationOperatorId.Value);

        #endregion

        #region GetChargingStationOperatorById   (ChargingStationOperatorId)

        /// <summary>
        /// Get the charging station operator having the given unique identification.
        /// </summary>
        /// <param name="ChargingStationOperatorId">The unique identification of a charging station operator.</param>
        public ChargingStationOperator? GetChargingStationOperatorById(ChargingStationOperator_Id ChargingStationOperatorId)
        {

            if (chargingStationOperators.TryGet(ChargingStationOperatorId, out var chargingStationOperator))
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
                chargingStationOperators.TryGet(ChargingStationOperatorId.Value, out ChargingStationOperator))
            {
                return true;
            }

            ChargingStationOperator = null;
            return false;

        }

        #endregion

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
