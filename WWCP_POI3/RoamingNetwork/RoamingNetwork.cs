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
                                                                           RoamingNetworkStatusType>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const               String                                             JSONLDContext                          = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/v1";

        protected static readonly  SemaphoreSlim                                      eMobilityProvidersSemaphore            = new (1, 1);
        protected static readonly  SemaphoreSlim                                      chargingStationOperatorsSemaphore      = new (1, 1);

        protected static readonly  TimeSpan                                           SemaphoreSlimTimeout                   = TimeSpan.FromSeconds(5);

        protected static readonly  Byte                                               MinEMobilityProviderIdLength           = 5;
        protected static readonly  Byte                                               MinEMobilityProviderNameLength         = 5;

        protected static readonly  Byte                                               MinChargingStationOperatorIdLength     = 5;
        protected static readonly  Byte                                               MinChargingStationOperatorNameLength   = 5;

        /// <summary>
        /// The ETags of this version, as CalcETags() calculated them.
        /// </summary>
        private ImmutableArray<ETag> eTags = [];

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the operator.
            /// </summary>
            public const String  Operator                            = "operator";

            /// <summary>
            /// The property of its charging station operators.
            /// </summary>
            public const String  ChargingStationOperators            = "chargingStationOperators";

            /// <summary>
            /// The property of the identifications of its charging station operators, written instead of them when they are not expanded.
            /// </summary>
            public const String  ChargingStationOperatorIds          = "chargingStationOperatorIds";

            /// <summary>
            /// The property of its charging station manufacturers.
            /// </summary>
            public const String  ChargingStationManufacturers        = "chargingStationManufacturers";

            /// <summary>
            /// The property of the identifications of its charging station manufacturers.
            /// </summary>
            public const String  ChargingStationManufacturerIds      = "chargingStationManufacturerIds";

            /// <summary>
            /// The property of its e-mobility providers.
            /// </summary>
            public const String  EMobilityProviders                  = "eMobilityProviders";

            /// <summary>
            /// The property of the identifications of its e-mobility providers.
            /// </summary>
            public const String  EMobilityProviderIds                = "eMobilityProviderIds";

            /// <summary>
            /// The property of its grid operators.
            /// </summary>
            public const String  GridOperators                       = "gridOperators";

            /// <summary>
            /// The property of the identifications of its grid operators.
            /// </summary>
            public const String  GridOperatorIds                     = "gridOperatorIds";

            /// <summary>
            /// The property of its parking operators.
            /// </summary>
            public const String  ParkingOperators                    = "parkingOperators";

            /// <summary>
            /// The property of the identifications of its parking operators.
            /// </summary>
            public const String  ParkingOperatorIds                  = "parkingOperatorIds";

            /// <summary>
            /// The property of its energy meter manufacturers.
            /// </summary>
            public const String  EnergyMeterManufacturers            = "energyMeterManufacturers";

            /// <summary>
            /// The property of the identifications of its energy meter manufacturers.
            /// </summary>
            public const String  EnergyMeterManufacturerIds          = "energyMeterManufacturerIds";

            /// <summary>
            /// The property of its transparency software manufacturers, with their releases.
            /// </summary>
            public const String  TransparencySoftwareManufacturers   = "transparencySoftwareManufacturers";

            /// <summary>
            /// The property of the identifications of its transparency software manufacturers.
            /// </summary>
            public const String  TransparencySoftwareManufacturerIds = "transparencySoftwareManufacturerIds";

            /// <summary>
            /// The property of its conformity assessment bodies, with the transparency software certificates they issued.
            /// </summary>
            public const String  ConformityAssessmentBodies          = "conformityAssessmentBodies";

            /// <summary>
            /// The property of the identifications of its conformity assessment bodies.
            /// </summary>
            public const String  ConformityAssessmentBodyIds         = "conformityAssessmentBodyIds";

            /// <summary>
            /// The property of the hash algorithms of its effective tree (SPECIFICATION.md, section 8.3).
            /// </summary>
            public const String  TreeHashAlgorithms                  = POITreeHashAlgorithms.JSONProperty;

        }

        #endregion


        #region Data licenses

        private readonly ConcurrentDictionary<DataLicense_Id, DataLicense> dataLicenses = [];

        /// <summary>
        /// The license of the roaming network data.
        /// </summary>
        [Mandatory]
        public IEnumerable<DataLicense> DataLicenses
            => dataLicenses.Values;

        #endregion

        #region Operator

        /// <summary>
        /// The operator of the roaming network, with its keys: it owns the roaming network itself and decides who
        /// is a member. A roaming network without one can not be changed, as nobody may sign a change of it.
        /// </summary>
        public RoamingNetworkOperator?  Operator    { get; }

        #endregion

        #region Tree hash algorithms

        /// <summary>
        /// The hash algorithms of its effective tree as it names them, treeHashAlgorithms (SPECIFICATION.md, section 8.3),
        /// or null when it names none.
        /// </summary>
        public ImmutableArray<ETagHashAlgorithm>?  GivenTreeHashAlgorithms    { get; }

        /// <summary>
        /// The hash algorithms of its effective tree: those it names, else SHA-256. A version has one effectiveTree ETag
        /// per algorithm, in their order.
        /// </summary>
        public ImmutableArray<ETagHashAlgorithm>   TreeHashAlgorithms
            => GivenTreeHashAlgorithms ?? POITreeHashAlgorithms.Default;

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
        public RoamingNetwork(RoamingNetwork_Id                              Id,
                              ImmutableI18NString?                           Name                               = null,
                              ImmutableI18NString?                           Description                        = null,
                              RoamingNetworkAdminStatusType?                 InitialAdminStatus                 = null,
                              RoamingNetworkStatusType?                      InitialStatus                      = null,
                              UInt16?                                        MaxAdminStatusScheduleSize         = null,
                              UInt16?                                        MaxStatusScheduleSize              = null,

                              String?                                        DataSource                         = null,
                              DateTimeOffset?                                Created                            = null,
                              DateTimeOffset?                                LastChange                         = null,

                              CustomDataNew?                                 CustomData                         = null,
                              UserDefinedDictionary?                         InternalData                       = null,

                              IEnumerable<DataLicense>?                      DataLicenses                       = null,
                              IEnumerable<ChargingStationOperator>?          ChargingStationOperators           = null,
                              IEnumerable<ChargingStationManufacturer>?      ChargingStationManufacturers       = null,
                              IEnumerable<EMobilityProvider>?                EMobilityProviders                 = null,
                              IEnumerable<GridOperator>?                     GridOperators                      = null,
                              IEnumerable<ParkingOperator>?                  ParkingOperators                   = null,
                              IEnumerable<TransparencySoftwareManufacturer>? TransparencySoftwareManufacturers  = null,
                              IEnumerable<ConformityAssessmentBody>?         ConformityAssessmentBodies         = null,
                              IEnumerable<EnergyMeterManufacturer>?          EnergyMeterManufacturers           = null,
                              RoamingNetworkOperator?                        Operator                           = null,
                              IEnumerable<ETagHashAlgorithm>?                TreeHashAlgorithms                 = null)

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

            this.Operator = Operator;

            // A key statement of its operator states this network and the operator's own keys, signed by one of them.
            if (Operator?.KeyStatementError(Id) is String keyStatementError)
                throw new ArgumentException(keyStatementError, nameof(Operator));

            if (TreeHashAlgorithms is not null)
            {

                ImmutableArray<ETagHashAlgorithm> treeHashAlgorithms = [ .. TreeHashAlgorithms ];

                if (POITreeHashAlgorithms.Invalid(treeHashAlgorithms) is String treeHashAlgorithmsError)
                    throw new ArgumentException(treeHashAlgorithmsError, nameof(TreeHashAlgorithms));

                this.GivenTreeHashAlgorithms = treeHashAlgorithms;

            }

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

            // Groups of e-mobility providers and charging station operators are unique within the network, and so are their
            // charging tariff offers.
            if ((DuplicateProviderGroup      (chargingStationOperators, eMobilityProviders) ??
                 DuplicateChargingTariffOffer(chargingStationOperators, eMobilityProviders)) is String providerGroupError)
            {
                throw new ArgumentException(providerGroupError, nameof(EMobilityProviders));
            }

            this.transparencySoftwareManufacturers = new (TransparencySoftwareManufacturers, entity => entity.Id, "transparency software manufacturer");

            // Releases are embedded in their manufacturers, and unique within the network: energy meters and certificates
            // name them by their identifications alone.
            if (DuplicateTransparencySoftware(transparencySoftwareManufacturers) is String duplicateRelease)
                throw new ArgumentException(duplicateRelease, nameof(TransparencySoftwareManufacturers));

            this.transparencySoftware = TransparencySoftwareOf(transparencySoftwareManufacturers);

            this.conformityAssessmentBodies = new (ConformityAssessmentBodies, entity => entity.Id, "conformity assessment body");

            // Certificates are embedded in the bodies issuing them, and unique within the network: energy meters name them
            // by their identifications alone.
            if (DuplicateTransparencySoftwareCertificate(conformityAssessmentBodies) is String duplicateCertificate)
                throw new ArgumentException(duplicateCertificate, nameof(ConformityAssessmentBodies));

            this.transparencySoftwareCertificates = TransparencySoftwareCertificatesOf(conformityAssessmentBodies);

            // Certificates name releases and manufacturers of this network; energy meters name releases and certificates.
            if (UnknownTransparencyReference(AllEnergyMeters(chargingStationOperators, gridOperators), transparencySoftware, transparencySoftwareCertificates, chargingStationManufacturers.Index) is String transparencyError)
                throw new ArgumentException(transparencyError, nameof(TransparencySoftwareManufacturers));

            // Energy meter templates and groups are unique within the network; templates name manufacturers, releases and
            // certificates of it.
            if ((DuplicateEnergyMeterGroup          (AllEnergyMeterGroups   (chargingStationOperators, gridOperators)) ??
                 DuplicateEnergyMeterTemplate       (AllEnergyMeterTemplates(chargingStationOperators, gridOperators)) ??
                 UnknownEnergyMeterTemplateReference(AllEnergyMeterTemplates(chargingStationOperators, gridOperators), energyMeterManufacturers.Index, transparencySoftware, transparencySoftwareCertificates)) is String energyMeterGroupError)
            {
                throw new ArgumentException(energyMeterGroupError, nameof(ChargingStationOperators));
            }

            foreach (var energyMeterGroup in AllEnergyMeterGroups(chargingStationOperators, gridOperators))
                energyMeterGroups.TryAdd(energyMeterGroup.Id, energyMeterGroup);

            foreach (var energyMeterTemplate in AllEnergyMeterTemplates(chargingStationOperators, gridOperators))
                energyMeterTemplates.TryAdd(energyMeterTemplate.Id, energyMeterTemplate);

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

                foreach (var parkingSpaceTemplate in parkingOperator.ParkingSpaceTemplates)
                    parkingSpaceTemplates.TryAdd(parkingSpaceTemplate.Id, parkingSpaceTemplate);

            }

            // The parking spaces charging station operators define where no parking operator does.
            foreach (var chargingStationOperator in chargingStationOperators)
                foreach (var parkingSpace in chargingStationOperator.ParkingSpaces)
                    parkingSpaces.TryAdd(parkingSpace.Id, parkingSpace);

            // A key belongs to one party only: no party may name another party's public key as its own, which would
            // otherwise let it block that party's signatures (its signatures are checked against every party naming the
            // key, by its validity there), the roaming network operator's included (SPECIFICATION.md, section 4.1).
            if (DuplicateKeyAcrossParties(Parties) is String duplicateKey)
                throw new ArgumentException(duplicateKey, nameof(ChargingStationOperators));

            #endregion

        }

        #endregion


        #region (Admin-)Status management

        private readonly Lock                                         statusEventsLock = new ();
        private          OnRoamingNetworkAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnRoamingNetworkStatusChangedDelegate?       onStatusChanged;


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



        #region (static) Parse    (JSON, ...)

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

        #region (static) TryParse (JSON, out RoamingNetwork, out ErrorResponse, ...)

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

                #region Parse Id                 [mandatory]

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Check JSON-LD context    [optional]

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the roaming network '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                #endregion

                #region Parse Name               [optional]

                if (JSON.ParseOptionalJSON(BasePropertyKeys.Name,
                                           "network name",
                                           ImmutableI18NString.TryParse,
                                           out ImmutableI18NString? name,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse Description        [optional]

                if (JSON.ParseOptionalJSON(BasePropertyKeys.Description,
                                           "network description",
                                           I18NString.TryParse,
                                           out I18NString? description,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse DataSource         [optional]

                if (JSON.ParseOptional(BasePropertyKeys.DataSource,
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

                if (!POIDataLicenses.TryParse(JSON[BasePropertyKeys.DataLicenses],
                                              JSON[BasePropertyKeys.DataLicenseIds],
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse TreeHashAlgorithms [optional]

                if (!POITreeHashAlgorithms.TryParse(JSON[PropertyKeys.TreeHashAlgorithms],
                                                    out var treeHashAlgorithms,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Operator       [optional]

                // The keys of a roaming network are the keys of its operator.
                if (JSON.ContainsKey(POIPartyKeys.SigningKeysProperty) || JSON.ContainsKey(RoamingNetworkOperator.PropertyKeys.IdentityKeys))
                {
                    ErrorResponse = $"A roaming network has no keys of its own: they are the keys of its operator, '{PropertyKeys.Operator}'!";
                    return false;
                }

                RoamingNetworkOperator? roamingNetworkOperator = null;

                if (JSON[PropertyKeys.Operator] is JToken operatorJSON)
                {

                    if (operatorJSON is not JObject operatorObject)
                    {
                        ErrorResponse = "The roaming network operator must be a JSON object!";
                        return false;
                    }

                    if (!RoamingNetworkOperator.TryParse(operatorObject, out roamingNetworkOperator, out ErrorResponse))
                        return false;

                }

                #endregion

                #region Parse ChargingStationManufacturers [optional]

                if (!POIJSON.TryParseChildren<ChargingStationManufacturer>(JSON,
                                                                           PropertyKeys.ChargingStationManufacturers,
                                                                           PropertyKeys.ChargingStationManufacturerIds,
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
                                                                 PropertyKeys.EMobilityProviders,
                                                                 PropertyKeys.EMobilityProviderIds,
                                                                 "e-mobility providers",
                                                                 EMobilityProvider.TryParse,
                                                                 out var eMobilityProviders,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<GridOperator>     (JSON,
                                                                 PropertyKeys.GridOperators,
                                                                 PropertyKeys.GridOperatorIds,
                                                                 "grid operators",
                                                                 GridOperator.TryParse,
                                                                 out var gridOperators,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<ParkingOperator>  (JSON,
                                                                 PropertyKeys.ParkingOperators,
                                                                 PropertyKeys.ParkingOperatorIds,
                                                                 "parking operators",
                                                                 ParkingOperator.TryParse,
                                                                 out var parkingOperators,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<TransparencySoftwareManufacturer>(JSON,
                                                                 PropertyKeys.TransparencySoftwareManufacturers,
                                                                 PropertyKeys.TransparencySoftwareManufacturerIds,
                                                                 "transparency software manufacturers",
                                                                 TransparencySoftwareManufacturer.TryParse,
                                                                 out var transparencySoftwareManufacturers,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<ConformityAssessmentBody>(JSON,
                                                                 PropertyKeys.ConformityAssessmentBodies,
                                                                 PropertyKeys.ConformityAssessmentBodyIds,
                                                                 "conformity assessment bodies",
                                                                 ConformityAssessmentBody.TryParse,
                                                                 out var conformityAssessmentBodies,
                                                                 out ErrorResponse) ||

                    !POIJSON.TryParseChildren<EnergyMeterManufacturer>(JSON,
                                                                 PropertyKeys.EnergyMeterManufacturers,
                                                                 PropertyKeys.EnergyMeterManufacturerIds,
                                                                 "energy meter manufacturers",
                                                                 EnergyMeterManufacturer.TryParse,
                                                                 out var energyMeterManufacturers,
                                                                 out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ChargingStationOperators [optional]

                if (JSON.ContainsKey(PropertyKeys.ChargingStationOperatorIds))
                {
                    ErrorResponse = $"Charging station operators given only by their identifications can not be read; they must be given completely as '{PropertyKeys.ChargingStationOperators}'!";
                    return false;
                }

                var chargingStationOperators = new List<ChargingStationOperator>();

                if (JSON[PropertyKeys.ChargingStationOperators] is JToken chargingStationOperatorsJSON)
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
                                     DataSource:                         dataSource,
                                     Created:                            created,
                                     LastChange:                         lastChange,
                                     CustomData:                         customData,
                                     DataLicenses:                       dataLicenses,
                                     ChargingStationOperators:           chargingStationOperators,
                                     ChargingStationManufacturers:       chargingStationManufacturers,
                                     EMobilityProviders:                 eMobilityProviders,
                                     GridOperators:                      gridOperators,
                                     ParkingOperators:                   parkingOperators,
                                     TransparencySoftwareManufacturers:  transparencySoftwareManufacturers,
                                     ConformityAssessmentBodies:         conformityAssessmentBodies,
                                     EnergyMeterManufacturers:           energyMeterManufacturers,
                                     Operator:                           roamingNetworkOperator,
                                     TreeHashAlgorithms:                 treeHashAlgorithms

                                 );

                #region Check ETags          [optional]

                if (JSON[BasePropertyKeys.ETags] is JToken eTagsJSON &&
                    !RoamingNetwork.TryCheckETags(eTagsJSON is JArray eTagsArray
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
                ErrorResponse   = "The given JSON representation of a roaming network is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON (Embedded = false, ...)

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

            => ToJSON(Embedded,
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
                      IncludeETags,
                      ETagDigestEncoding,
                      IncludeChildArrays: true);


        /// <summary>
        /// The JSON representation of this roaming network, with or without its child arrays: without them for the skeleton of its
        /// tree (SPECIFICATION.md, section 8.2), which names their roots in their place.
        /// </summary>
        /// <param name="IncludeChildArrays">Whether to include its child arrays.</param>
        internal JObject ToJSON(Boolean                                                    Embedded,
                                InfoStatus                                                 ExpandRoamingNetworkIds,
                                InfoStatus                                                 ExpandChargingStationOperatorIds,
                                InfoStatus                                                 ExpandChargingPoolIds,
                                InfoStatus                                                 ExpandChargingStationIds,
                                InfoStatus                                                 ExpandEVSEIds,
                                InfoStatus                                                 ExpandBrandIds,
                                InfoStatus                                                 ExpandDataLicenses,
                                InfoStatus                                                 ExpandEMobilityProviderId,
                                CustomJObjectSerializerDelegate<RoamingNetwork>?           CustomRoamingNetworkSerializer,
                                CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer,
                                Boolean                                                    IncludeETags,
                                ETagDigestEncoding?                                        ETagDigestEncoding,
                                Boolean                                                    IncludeChildArrays)
        {

            var json = JSONObject.Create(

                               new JProperty(BasePropertyKeys.Id,            Id.         ToString()),

                         !Embedded
                             ? new JProperty(BasePropertyKeys.Context,       JSONLDContext)
                             : null,

                         Name.IsNotNullOrEmpty()
                             ? new JProperty(BasePropertyKeys.Name,          Name.       ToJSON())
                             : null,

                         Description.IsNotNullOrEmpty()
                             ? new JProperty(BasePropertyKeys.Description,   Description.ToJSON())
                             : null,

                         DataSource is not null && DataSource.IsNeitherNullNorEmpty()
                             ? new JProperty(BasePropertyKeys.DataSource,    DataSource)
                             : null,

                         DataLicenses.Any() && ExpandDataLicenses != InfoStatus.Hidden
                             ? ExpandDataLicenses.Switch(
                                 () => new JProperty(
                                           BasePropertyKeys.DataLicenseIds,
                                           DataLicenses.ToPOIIdsJSON()
                                       ),
                                 () => new JProperty(
                                           BasePropertyKeys.DataLicenses,
                                           DataLicenses.ToPOIJSON()
                                       ))
                             : null,

                         Operator is not null
                             ? new JProperty(PropertyKeys.Operator,      Operator.ToJSON(Embedded: true))
                             : null,

                         IncludeChildArrays && ChargingStationOperators.Any() && ExpandChargingStationOperatorIds != InfoStatus.Hidden
                             ? ExpandChargingStationOperatorIds.Switch(

                                   () => new JProperty(
                                             PropertyKeys.ChargingStationOperatorIds,
                                             new JArray(
                                                 chargingStationOperators.
                                                     Select(chargingStationOperator => chargingStationOperator.Id.ToString())
                                             )
                                         ),

                                   () => new JProperty(
                                             PropertyKeys.ChargingStationOperators,
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
            if (IncludeChildArrays && !chargingStationManufacturers.IsEmpty)
                json.Add(PropertyKeys.ChargingStationManufacturers,      new JArray(chargingStationManufacturers.Select(chargingStationManufacturer => chargingStationManufacturer.ToJSON(Embedded: true))));

            if (IncludeChildArrays && !eMobilityProviders.IsEmpty)
                json.Add(PropertyKeys.EMobilityProviders,                new JArray(eMobilityProviders.Select(eMobilityProvider => eMobilityProvider.ToJSON(Embedded: true))));

            if (IncludeChildArrays && !gridOperators.IsEmpty)
                json.Add(PropertyKeys.GridOperators,                     new JArray(gridOperators.Select(gridOperator => gridOperator.ToJSON(Embedded: true))));

            if (IncludeChildArrays && !parkingOperators.IsEmpty)
                json.Add(PropertyKeys.ParkingOperators,                  new JArray(parkingOperators.Select(parkingOperator => parkingOperator.ToJSON(Embedded: true))));

            if (IncludeChildArrays && !energyMeterManufacturers.IsEmpty)
                json.Add(PropertyKeys.EnergyMeterManufacturers,          new JArray(energyMeterManufacturers.Select(energyMeterManufacturer => energyMeterManufacturer.ToJSON(Embedded: true))));

            if (IncludeChildArrays && !transparencySoftwareManufacturers.IsEmpty)
                json.Add(PropertyKeys.TransparencySoftwareManufacturers, new JArray(transparencySoftwareManufacturers.Select(manufacturer => manufacturer.ToJSON(Embedded: true))));

            if (IncludeChildArrays && !conformityAssessmentBodies.IsEmpty)
                json.Add(PropertyKeys.ConformityAssessmentBodies,        new JArray(conformityAssessmentBodies.Select(body => body.ToJSON(Embedded: true))));

            if (GivenTreeHashAlgorithms.HasValue)
                json.Add(PropertyKeys.TreeHashAlgorithms,                POITreeHashAlgorithms.ToJSON(GivenTreeHashAlgorithms.Value));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            // The ETags describe the content without them, irrespective of the view options.
            if (IncludeETags && ETags.Length > 0)
                json.Add(BasePropertyKeys.ETags,                             new JArray(ETags.Select(eTag => eTag.ToJSON(ETagDigestEncoding ?? DefaultETagDigestEncoding))));

            // Whole numbers are integers in it, as every writer of its entities writes them (POIJSON.Number): ToString()
            // of it is read back with its ETags.
            return CustomRoamingNetworkSerializer is not null
                       ? CustomRoamingNetworkSerializer(this, json)
                       : json;

        }

        #endregion


        #region (static) Parse    (CBOR, ...)

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

        #region (static) TryParse (CBOR, out RoamingNetwork, out ErrorResponse, ...)

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

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a roaming network must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Check CBOR-LD context [optional]

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the roaming network '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                #endregion

                #region Parse Name           [optional]

                if (CBOR.ParseOptional(BasePropertyKeys.Name,
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

                if (CBOR.ParseOptional(BasePropertyKeys.Description,
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

                if (CBOR.ParseOptionalText(BasePropertyKeys.DataSource,
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

                foreach (var key in new[] { BasePropertyKeys.Created, BasePropertyKeys.LastChange, BasePropertyKeys.CustomData })
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

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.DataLicenses),   out var dataLicensesCBOR))
                    dataLicensesJSON    = CBORJSON.ToJSON(dataLicensesCBOR);

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.DataLicenseIds), out var dataLicenseIdsCBOR))
                    dataLicenseIdsJSON  = CBORJSON.ToJSON(dataLicenseIdsCBOR);

                if (!POIDataLicenses.TryParse(dataLicensesJSON,
                                              dataLicenseIdsJSON,
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse TreeHashAlgorithms [optional]

                if (!POITreeHashAlgorithms.TryParse(CBOR.TryGetValue(CBORValue.FromText(PropertyKeys.TreeHashAlgorithms), out var treeHashAlgorithmsCBOR)
                                                        ? CBORJSON.ToJSON(treeHashAlgorithmsCBOR)
                                                        : null,
                                                    out var treeHashAlgorithms,
                                                    out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Operator       [optional]

                // The keys of a roaming network are the keys of its operator.
                foreach (var keysProperty in new[] { POIPartyKeys.SigningKeysProperty, RoamingNetworkOperator.PropertyKeys.IdentityKeys })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(keysProperty), out _))
                    {
                        ErrorResponse = $"A roaming network has no keys of its own: they are the keys of its operator, '{PropertyKeys.Operator}'!";
                        return false;
                    }
                }

                RoamingNetworkOperator? roamingNetworkOperator = null;

                // Read from its own CBOR representation: the operator refuses what is no map.
                if (CBOR.TryGetValue(CBORValue.FromText(PropertyKeys.Operator), out var operatorCBOR) &&
                    !RoamingNetworkOperator.TryParse(operatorCBOR, out roamingNetworkOperator, out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ChargingStationManufacturers, EMobilityProviders, GridOperators [optional]

                if (!TryParseCBORChildren<ChargingStationManufacturer>(CBOR,
                                                                       PropertyKeys.ChargingStationManufacturers,
                                                                       PropertyKeys.ChargingStationManufacturerIds,
                                                                       "charging station manufacturers",
                                                                       ChargingStationManufacturer.TryParse,
                                                                       out var chargingStationManufacturers,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<EMobilityProvider>          (CBOR,
                                                                       PropertyKeys.EMobilityProviders,
                                                                       PropertyKeys.EMobilityProviderIds,
                                                                       "e-mobility providers",
                                                                       EMobilityProvider.TryParse,
                                                                       out var eMobilityProviders,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<GridOperator>               (CBOR,
                                                                       PropertyKeys.GridOperators,
                                                                       PropertyKeys.GridOperatorIds,
                                                                       "grid operators",
                                                                       GridOperator.TryParse,
                                                                       out var gridOperators,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<ParkingOperator>            (CBOR,
                                                                       PropertyKeys.ParkingOperators,
                                                                       PropertyKeys.ParkingOperatorIds,
                                                                       "parking operators",
                                                                       ParkingOperator.TryParse,
                                                                       out var parkingOperators,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<TransparencySoftwareManufacturer>(CBOR,
                                                                       PropertyKeys.TransparencySoftwareManufacturers,
                                                                       PropertyKeys.TransparencySoftwareManufacturerIds,
                                                                       "transparency software manufacturers",
                                                                       TransparencySoftwareManufacturer.TryParse,
                                                                       out var transparencySoftwareManufacturers,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<ConformityAssessmentBody>   (CBOR,
                                                                       PropertyKeys.ConformityAssessmentBodies,
                                                                       PropertyKeys.ConformityAssessmentBodyIds,
                                                                       "conformity assessment bodies",
                                                                       ConformityAssessmentBody.TryParse,
                                                                       out var conformityAssessmentBodies,
                                                                       out ErrorResponse) ||

                    !TryParseCBORChildren<EnergyMeterManufacturer>    (CBOR,
                                                                       PropertyKeys.EnergyMeterManufacturers,
                                                                       PropertyKeys.EnergyMeterManufacturerIds,
                                                                       "energy meter manufacturers",
                                                                       EnergyMeterManufacturer.TryParse,
                                                                       out var energyMeterManufacturers,
                                                                       out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse ChargingStationOperators [optional]

                if (CBOR.TryGetValue(CBORValue.FromText(PropertyKeys.ChargingStationOperatorIds), out _))
                {
                    ErrorResponse = $"Charging station operators given only by their identifications can not be read; they must be given completely as '{PropertyKeys.ChargingStationOperators}'!";
                    return false;
                }

                var chargingStationOperators = new List<ChargingStationOperator>();

                if (CBOR.TryGetValue(CBORValue.FromText(PropertyKeys.ChargingStationOperators), out var chargingStationOperatorsCBOR))
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
                                     TransparencySoftwareManufacturers:  transparencySoftwareManufacturers,
                                     ConformityAssessmentBodies:  conformityAssessmentBodies,
                                     EnergyMeterManufacturers:  energyMeterManufacturers,
                                     Operator:                  roamingNetworkOperator,
                                     TreeHashAlgorithms:        treeHashAlgorithms
                                 );

                #region Check ETags          [optional]

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.ETags), out var eTagsCBOR) &&
                    !RoamingNetwork.TryCheckETags(eTagsCBOR.Kind == CBORValueKind.Array
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

        #region ToCBOR (Embedded = false, ...)

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

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),                  CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),             CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),                Name.ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),         Description.ToCBOR());

            if (DataSource is not null && DataSource.IsNeitherNullNorEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),          CBORValue.FromText(DataSource));

            if (DataLicenses.Any())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataLicenses),        DataLicenses.ToPOICBOR());

            if (Operator is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Operator),                Operator.ToCBOR(Embedded: true));

            if (ChargingStationOperators.Any())
                cbor.Add(CBORValue.FromText(PropertyKeys.ChargingStationOperators),
                         CBORValue.FromArray(chargingStationOperators.
                                                 Select(chargingStationOperator => chargingStationOperator.ToCBOR(Embedded: true))));

            if (!chargingStationManufacturers.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.ChargingStationManufacturers),
                         CBORValue.FromArray(chargingStationManufacturers.
                                                 Select(chargingStationManufacturer => chargingStationManufacturer.ToCBOR(Embedded: true))));

            if (!eMobilityProviders.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.EMobilityProviders),
                         CBORValue.FromArray(eMobilityProviders.
                                                 Select(eMobilityProvider => eMobilityProvider.ToCBOR(Embedded: true))));

            if (!gridOperators.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.GridOperators),
                         CBORValue.FromArray(gridOperators.
                                                 Select(gridOperator => gridOperator.ToCBOR(Embedded: true))));

            if (!parkingOperators.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.ParkingOperators),
                         CBORValue.FromArray(parkingOperators.
                                                 Select(parkingOperator => parkingOperator.ToCBOR(Embedded: true))));

            if (!energyMeterManufacturers.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.EnergyMeterManufacturers),
                         CBORValue.FromArray(energyMeterManufacturers.
                                                 Select(energyMeterManufacturer => energyMeterManufacturer.ToCBOR(Embedded: true))));

            if (!transparencySoftwareManufacturers.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.TransparencySoftwareManufacturers),
                         CBORValue.FromArray(transparencySoftwareManufacturers.
                                                 Select(manufacturer => manufacturer.ToCBOR(Embedded: true))));

            if (!conformityAssessmentBodies.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.ConformityAssessmentBodies),
                         CBORValue.FromArray(conformityAssessmentBodies.
                                                 Select(body => body.ToCBOR(Embedded: true))));

            if (GivenTreeHashAlgorithms.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.TreeHashAlgorithms),  POITreeHashAlgorithms.ToCBOR(GivenTreeHashAlgorithms.Value));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),             CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),          CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),      CBORJSON.ToCBOR(CustomData.ToJObject()));

            // The ETags describe the content without them, irrespective of the view options.
            if (IncludeETags && ETags.Length > 0)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.ETags),           CBORValue.FromArray(ETags.Select(eTag => eTag.ToCBOR())));

            return CustomRoamingNetworkSerializer is not null
                       ? CustomRoamingNetworkSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region TryCheckETags(ETags, out ErrorResponse)

        /// <summary>
        /// The ETags a representation declares must be the ETags of the roaming network read from it, also its effectiveTree
        /// ETags: stale or wrong ETags are refused, not ignored. At least one must be declared.
        /// </summary>
        /// <param name="ETags">The ETags the representation declares.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public override Boolean TryCheckETags(IEnumerable<ETag>                 ETags,
                                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (MatchETags([ .. ETags ], this, out var error))
            {
                ErrorResponse = null;
                return true;
            }

            ErrorResponse = $"The ETags of the roaming network '{Id}' do not match its content: {error}";
            return false;

        }

        #endregion

        #region ETags

        /// <summary>
        /// The ETags of this version, once CalcETags() calculated them: of its canonical JSON and CBOR representations and
        /// its effectiveTree ETags; none before.
        /// </summary>
        public override ImmutableArray<ETag> ETags
            => eTags;

        #endregion

        #region CalcETags()

        /// <summary>
        /// Calculate the ETags of this version and keep them: of its canonical JSON and CBOR representations, both without
        /// ETags, without view options and without custom serializers, and its effectiveTree ETags, one per tree hash
        /// algorithm, in their order (SPECIFICATION.md, section 8.3).
        /// </summary>
        public ImmutableArray<ETag> CalcETags()
        {

            eTags = [ .. CalcChangeSetETags(),
                      .. TreeHashAlgorithms.Select(EffectiveTreeETag) ];

            return eTags;

        }

        #endregion

        #region CalcChangeSetETags()

        /// <summary>
        /// Calculate the ETags change sets name of this version as their before and after ETags: of its canonical JSON and
        /// CBOR representations. They are not kept: the ETags of the version are those of CalcETags().
        /// </summary>
        public ImmutableArray<ETag> CalcChangeSetETags()

            => [ ContentETag(ETagFormat.JSON),
                 ContentETag(ETagFormat.CBOR) ];

        #endregion

        #region (protected override) ContentJSON() / ContentCBOR() / ContentOf(JSON) / ContentOf(CBOR)

        /// <summary>
        /// The content of this version its JSON and CBOR ETags describe (see ContentETag(…)): its canonical JSON and CBOR
        /// representations, without ETags, without view options and without custom serializers. A roaming network has no
        /// parent: they are its own representations, with its JSON-LD context. Applying a change set checks those of the
        /// version before it and calculates those after it, each a serialization of the whole roaming network.
        /// </summary>
        protected override JObject ContentJSON()
            => ToJSON(IncludeETags: false);

        /// <inheritdoc cref="ContentJSON"/>
        protected override CBORValue ContentCBOR()
            => ToCBOR(IncludeETags: false);

        /// <summary>
        /// The content of the given representation of this version: without its ETags, with its JSON-LD context.
        /// </summary>
        protected override JObject ContentOf(JObject JSON)
        {
            var content = (JObject) JSON.DeepClone();
            content.Remove(BasePropertyKeys.ETags);
            return content;
        }

        /// <inheritdoc cref="ContentOf(JObject)"/>
        protected override CBORValue ContentOf(CBORValue CBOR)
            => CBORValue.FromMap(CBOR.AsMap().Where(entry => !(entry.Key.TryGetText(out var key) && key == BasePropertyKeys.ETags)));

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
        /// The error, when a charging pool or a charging pool template names a grid connection point the given grid operators do not have.
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

            foreach (var chargingPoolTemplate in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingPoolTemplates))
                if (chargingPoolTemplate.GridConnectionPointId.HasValue &&
                    !known.Contains(chargingPoolTemplate.GridConnectionPointId.Value))
                {
                    return $"The grid connection point '{chargingPoolTemplate.GridConnectionPointId}' of the charging pool template '{chargingPoolTemplate.Id}' is unknown in the roaming network!";
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
        /// The parking spaces of all parking operators as they are at runtime, with the properties their parking space groups
        /// give them, and those charging station operators define where no parking operator does.
        /// </summary>
        public IEnumerable<ParkingSpace>    EffectiveParkingSpaces
            => parkingSpaces.Values.Select(EffectiveParkingSpace);

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
        /// Try to get the parking space having the given identification as it is at runtime, with the properties its
        /// parking space groups give it.
        /// </summary>
        public Boolean TryGetEffectiveParkingSpaceById(ParkingSpace_Id                        ParkingSpaceId,
                                                       [NotNullWhen(true)] out ParkingSpace?  ParkingSpace)
        {

            if (!parkingSpaces.TryGetValue(ParkingSpaceId, out ParkingSpace))
                return false;

            ParkingSpace = EffectiveParkingSpace(ParkingSpace);
            return true;

        }

        /// <summary>
        /// The given parking space as it is at runtime: with the properties the parking space groups of its parking operator
        /// give it; one of a charging station operator as it is.
        /// </summary>
        private ParkingSpace EffectiveParkingSpace(ParkingSpace ParkingSpace)
        {

            foreach (var parkingOperator in parkingOperators)
                if (parkingOperator.TryGetParkingSpace(ParkingSpace.Id, out var own) && ReferenceEquals(own, ParkingSpace))
                    return parkingOperator.EffectiveParkingSpace(ParkingSpace);

            return ParkingSpace;

        }

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
            var templates = new HashSet<ParkingSpaceTemplate_Id>();
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

                foreach (var parkingSpaceTemplate in parkingOperator.ParkingSpaceTemplates)
                    if (!templates.Add(parkingSpaceTemplate.Id))
                        return $"The parking space template '{parkingSpaceTemplate.Id}' must not be given twice in the roaming network!";

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
        /// The error, when a parking garage, space, space group or sensor names a charging station the given operators do not have,
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
                            Concat(parkingOperator.ParkingSpaceTemplates.SelectMany(parkingSpaceTemplate => parkingSpaceTemplate.ChargingStationIds.Select(id => (Kind: "parking space template", Id: parkingSpaceTemplate.Id.ToString(), ChargingStationId: id)))).
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
        /// The error, when a charging station, also by its templates, or a charging station template names a parking space
        /// neither the given parking operators nor the given charging station operators have.
        /// </summary>
        internal static String? UnknownParkingSpace(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                    IEnumerable<ParkingOperator>          ParkingOperators)
        {

            var parkingSpaceIds = ParkingOperators.SelectMany(parkingOperator => parkingOperator.ParkingSpaces).
                                                   Concat    (ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ParkingSpaces)).
                                                   Select    (parkingSpace    => parkingSpace.Id).
                                                   ToHashSet();

            foreach (var chargingStationTemplate in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingStationTemplates))
                foreach (var parkingSpaceId in chargingStationTemplate.ParkingSpaceIds)
                    if (!parkingSpaceIds.Contains(parkingSpaceId))
                        return $"The parking space '{parkingSpaceId}' of the charging station template '{chargingStationTemplate.Id}' is unknown in the roaming network!";

            foreach (var chargingStation in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingStations))
                foreach (var parkingSpaceId in chargingStation.ParkingSpaceIds)
                    if (!parkingSpaceIds.Contains(parkingSpaceId))
                        return $"The parking space '{parkingSpaceId}' of the charging station '{chargingStation.Id}' is unknown in the roaming network!";

            return null;

        }

        #endregion


        #region Transparency software manufacturers and their releases, conformity assessment bodies and their certificates

        private readonly POICollection<TransparencySoftwareManufacturer_Id, TransparencySoftwareManufacturer>  transparencySoftwareManufacturers;
        private readonly IReadOnlyDictionary<TransparencySoftware_Id, TransparencySoftware>                    transparencySoftware;
        private readonly POICollection<ConformityAssessmentBody_Id, ConformityAssessmentBody>                  conformityAssessmentBodies;
        private readonly IReadOnlyDictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>  transparencySoftwareCertificates;

        /// <summary>
        /// The conformity assessment bodies in this immutable network version, with the certificates they issued.
        /// </summary>
        public POICollection<ConformityAssessmentBody_Id, ConformityAssessmentBody>  ConformityAssessmentBodies
            => conformityAssessmentBodies;

        /// <summary>
        /// Try to get the conformity assessment body having the given identification.
        /// </summary>
        public Boolean TryGetConformityAssessmentBodyById(ConformityAssessmentBody_Id                        BodyId,
                                                          [NotNullWhen(true)] out ConformityAssessmentBody?  Body)

            => conformityAssessmentBodies.TryGet(BodyId, out Body);

        /// <summary>
        /// The conformity assessment body that issued the certificate having the given identification, if it is one of this network.
        /// </summary>
        public ConformityAssessmentBody? ConformityAssessmentBodyOf(TransparencySoftwareCertificate_Id CertificateId)

            => ConformityAssessmentBodyOf(conformityAssessmentBodies, CertificateId);

        /// <summary>
        /// The conformity assessment body of the given certificate among the given bodies.
        /// </summary>
        internal static ConformityAssessmentBody? ConformityAssessmentBodyOf(IEnumerable<ConformityAssessmentBody>  Bodies,
                                                                            TransparencySoftwareCertificate_Id     CertificateId)

            => Bodies.FirstOrDefault(body => body.TryGetTransparencySoftwareCertificate(CertificateId, out _));

        /// <summary>
        /// The certificates of the given conformity assessment bodies, by their identifications.
        /// </summary>
        internal static IReadOnlyDictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate> TransparencySoftwareCertificatesOf(IEnumerable<ConformityAssessmentBody> Bodies)
        {

            var certificates = new Dictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>();

            foreach (var body in Bodies)
                foreach (var certificate in body.TransparencySoftwareCertificates)
                    certificates.TryAdd(certificate.Id, certificate);

            return certificates;

        }

        /// <summary>
        /// The error, when two of the given conformity assessment bodies have a certificate of the same identification.
        /// </summary>
        internal static String? DuplicateTransparencySoftwareCertificate(IEnumerable<ConformityAssessmentBody> Bodies)
        {

            var seen = new Dictionary<TransparencySoftwareCertificate_Id, ConformityAssessmentBody_Id>();

            foreach (var body in Bodies)
                foreach (var certificate in body.TransparencySoftwareCertificates)
                    if (!seen.TryAdd(certificate.Id, body.Id))
                        return $"The transparency software certificate '{certificate.Id}' of the conformity assessment body '{body.Id}' is one of the conformity assessment body '{seen[certificate.Id]}' already!";

            return null;

        }

        /// <summary>
        /// The transparency software manufacturers in this immutable network version, with their releases.
        /// </summary>
        public POICollection<TransparencySoftwareManufacturer_Id, TransparencySoftwareManufacturer>  TransparencySoftwareManufacturers
            => transparencySoftwareManufacturers;

        /// <summary>
        /// The transparency software releases of all manufacturers in this immutable network version: one per version of a
        /// software, unique within the network.
        /// </summary>
        public IReadOnlyDictionary<TransparencySoftware_Id, TransparencySoftware>                    TransparencySoftware
            => transparencySoftware;

        /// <summary>
        /// Try to get the transparency software manufacturer having the given identification.
        /// </summary>
        public Boolean TryGetTransparencySoftwareManufacturerById(TransparencySoftwareManufacturer_Id                        ManufacturerId,
                                                                  [NotNullWhen(true)] out TransparencySoftwareManufacturer?  Manufacturer)

            => transparencySoftwareManufacturers.TryGet(ManufacturerId, out Manufacturer);

        /// <summary>
        /// The manufacturer of the transparency software release having the given identification, if it is one of this network.
        /// </summary>
        public TransparencySoftwareManufacturer? TransparencySoftwareManufacturerOf(TransparencySoftware_Id TransparencySoftwareId)

            => TransparencySoftwareManufacturerOf(transparencySoftwareManufacturers, TransparencySoftwareId);

        /// <summary>
        /// The manufacturer of the given release among the given manufacturers.
        /// </summary>
        internal static TransparencySoftwareManufacturer? TransparencySoftwareManufacturerOf(IEnumerable<TransparencySoftwareManufacturer>  Manufacturers,
                                                                                            TransparencySoftware_Id                        TransparencySoftwareId)

            => Manufacturers.FirstOrDefault(manufacturer => manufacturer.TryGetTransparencySoftware(TransparencySoftwareId, out _));

        /// <summary>
        /// The releases of the given manufacturers, by their identifications.
        /// </summary>
        internal static IReadOnlyDictionary<TransparencySoftware_Id, TransparencySoftware> TransparencySoftwareOf(IEnumerable<TransparencySoftwareManufacturer> Manufacturers)
        {

            var releases = new Dictionary<TransparencySoftware_Id, TransparencySoftware>();

            foreach (var manufacturer in Manufacturers)
                foreach (var release in manufacturer.TransparencySoftware)
                    releases.TryAdd(release.Id, release);

            return releases;

        }

        /// <summary>
        /// The error, when two of the given manufacturers have a release of the same identification.
        /// </summary>
        internal static String? DuplicateTransparencySoftware(IEnumerable<TransparencySoftwareManufacturer> Manufacturers)
        {

            var seen = new Dictionary<TransparencySoftware_Id, TransparencySoftwareManufacturer_Id>();

            foreach (var manufacturer in Manufacturers)
                foreach (var release in manufacturer.TransparencySoftware)
                    if (!seen.TryAdd(release.Id, manufacturer.Id))
                        return $"The transparency software release '{release.Id}' of the manufacturer '{manufacturer.Id}' is one of the manufacturer '{seen[release.Id]}' already!";

            return null;

        }

        /// <summary>
        /// The certificates of transparency software releases of all conformity assessment bodies in this immutable network
        /// version, unique within the network.
        /// </summary>
        public IReadOnlyDictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>  TransparencySoftwareCertificates
            => transparencySoftwareCertificates;

        /// <summary>
        /// Try to get the transparency software release having the given identification.
        /// </summary>
        public Boolean TryGetTransparencySoftwareById(TransparencySoftware_Id                        TransparencySoftwareId,
                                                      [NotNullWhen(true)] out TransparencySoftware?  TransparencySoftware)

            => transparencySoftware.TryGetValue(TransparencySoftwareId, out TransparencySoftware);

        /// <summary>
        /// Try to get the transparency software certificate having the given identification.
        /// </summary>
        public Boolean TryGetTransparencySoftwareCertificateById(TransparencySoftwareCertificate_Id                        CertificateId,
                                                                 [NotNullWhen(true)] out TransparencySoftwareCertificate?  Certificate)

            => transparencySoftwareCertificates.TryGetValue(CertificateId, out Certificate);

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
        /// The groups the given EVSE is a member of: of its operator, then of e-mobility providers, each in the order of
        /// their identifications.
        /// </summary>
        public IEnumerable<EVSEGroup> EVSEGroupsOf(EVSE_Id EVSEId)

            => (chargingStationOperators.TryGet(EVSEId.OperatorId, out var chargingStationOperator)
                    ? chargingStationOperator.EVSEGroupsOf(EVSEId)
                    : []).
               Concat(eMobilityProviders.SelectMany(eMobilityProvider => eMobilityProvider.EVSEGroups.Where(evseGroup => evseGroup.EVSEIds.Contains(EVSEId))));

        /// <summary>
        /// The groups the given charging station is a member of: of its operator, then of e-mobility providers, each in the
        /// order of their identifications.
        /// </summary>
        public IEnumerable<ChargingStationGroup> ChargingStationGroupsOf(ChargingStation_Id ChargingStationId)

            => (chargingStationOperators.TryGet(ChargingStationId.OperatorId, out var chargingStationOperator)
                    ? chargingStationOperator.ChargingStationGroupsOf(ChargingStationId)
                    : []).
               Concat(eMobilityProviders.SelectMany(eMobilityProvider => eMobilityProvider.ChargingStationGroups.Where(chargingStationGroup => chargingStationGroup.ChargingStationIds.Contains(ChargingStationId))));

        /// <summary>
        /// The groups the given charging pool is a member of: of its operator, then of e-mobility providers, each in the
        /// order of their identifications.
        /// </summary>
        public IEnumerable<ChargingPoolGroup> ChargingPoolGroupsOf(ChargingPool_Id ChargingPoolId)

            => (chargingStationOperators.TryGet(ChargingPoolId.OperatorId, out var chargingStationOperator)
                    ? chargingStationOperator.ChargingPoolGroupsOf(ChargingPoolId)
                    : []).
               Concat(eMobilityProviders.SelectMany(eMobilityProvider => eMobilityProvider.ChargingPoolGroups.Where(chargingPoolGroup => chargingPoolGroup.ChargingPoolIds.Contains(ChargingPoolId))));

        /// <summary>
        /// The e-mobility provider the given prefix of the identification of a group names, if any.
        /// </summary>
        private EMobilityProvider? ProviderOf(ChargingStationOperator_Id Prefix)

            => EMobilityProvider_Id.TryParse(Prefix.ToString(), out var providerId) &&
               eMobilityProviders.TryGet(providerId, out var eMobilityProvider)
                   ? eMobilityProvider
                   : null;

        /// <summary>
        /// The error, when a group of an e-mobility provider has the identification of a group of a charging station
        /// operator: the identifications of groups are unique in the roaming network, and a provider and an operator
        /// may have the same prefix.
        /// </summary>
        internal static String? DuplicateProviderGroup(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                       IEnumerable<EMobilityProvider>        EMobilityProviders)
        {

            var operators = ChargingStationOperators.ToArray();

            foreach (var eMobilityProvider in EMobilityProviders)
            {

                foreach (var evseGroup in eMobilityProvider.EVSEGroups)
                    if (operators.Any(chargingStationOperator => chargingStationOperator.TryGetEVSEGroup(evseGroup.Id, out _)))
                        return $"The EVSE group '{evseGroup.Id}' must not be given twice in the roaming network!";

                foreach (var chargingStationGroup in eMobilityProvider.ChargingStationGroups)
                    if (operators.Any(chargingStationOperator => chargingStationOperator.TryGetChargingStationGroup(chargingStationGroup.Id, out _)))
                        return $"The charging station group '{chargingStationGroup.Id}' must not be given twice in the roaming network!";

                foreach (var chargingPoolGroup in eMobilityProvider.ChargingPoolGroups)
                    if (operators.Any(chargingStationOperator => chargingStationOperator.TryGetChargingPoolGroup(chargingPoolGroup.Id, out _)))
                        return $"The charging pool group '{chargingPoolGroup.Id}' must not be given twice in the roaming network!";

            }

            return null;

        }

        /// <summary>
        /// The EVSEGroups of all charging station operators and e-mobility providers.
        /// </summary>
        public IEnumerable<EVSEGroup> EVSEGroups
            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.EVSEGroups).
                   Concat(eMobilityProviders.SelectMany(eMobilityProvider => eMobilityProvider.EVSEGroups));

        /// <summary>
        /// Try to get the EVSEGroup having the given identification, of the charging station operator or the e-mobility
        /// provider it names: unique in the roaming network, one of them has it.
        /// </summary>
        public Boolean TryGetEVSEGroupById(EVSEGroup_Id  EVSEGroupId,
                                     [NotNullWhen(true)] out EVSEGroup?  EVSEGroup)
        {

            if (chargingStationOperators.TryGet(EVSEGroupId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator.TryGetEVSEGroup(EVSEGroupId, out EVSEGroup))
            {
                return true;
            }

            if (ProviderOf(EVSEGroupId.OperatorId) is EMobilityProvider eMobilityProvider)
                return eMobilityProvider.TryGetEVSEGroup(EVSEGroupId, out EVSEGroup);

            EVSEGroup = null;
            return false;

        }

        /// <summary>
        /// The ChargingStationGroups of all charging station operators and e-mobility providers.
        /// </summary>
        public IEnumerable<ChargingStationGroup> ChargingStationGroups
            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingStationGroups).
                   Concat(eMobilityProviders.SelectMany(eMobilityProvider => eMobilityProvider.ChargingStationGroups));

        /// <summary>
        /// Try to get the ChargingStationGroup having the given identification, of the charging station operator or the e-mobility
        /// provider it names: unique in the roaming network, one of them has it.
        /// </summary>
        public Boolean TryGetChargingStationGroupById(ChargingStationGroup_Id  ChargingStationGroupId,
                                     [NotNullWhen(true)] out ChargingStationGroup?  ChargingStationGroup)
        {

            if (chargingStationOperators.TryGet(ChargingStationGroupId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator.TryGetChargingStationGroup(ChargingStationGroupId, out ChargingStationGroup))
            {
                return true;
            }

            if (ProviderOf(ChargingStationGroupId.OperatorId) is EMobilityProvider eMobilityProvider)
                return eMobilityProvider.TryGetChargingStationGroup(ChargingStationGroupId, out ChargingStationGroup);

            ChargingStationGroup = null;
            return false;

        }

        /// <summary>
        /// The ChargingPoolGroups of all charging station operators and e-mobility providers.
        /// </summary>
        public IEnumerable<ChargingPoolGroup> ChargingPoolGroups
            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingPoolGroups).
                   Concat(eMobilityProviders.SelectMany(eMobilityProvider => eMobilityProvider.ChargingPoolGroups));

        /// <summary>
        /// Try to get the ChargingPoolGroup having the given identification, of the charging station operator or the e-mobility
        /// provider it names: unique in the roaming network, one of them has it.
        /// </summary>
        public Boolean TryGetChargingPoolGroupById(ChargingPoolGroup_Id  ChargingPoolGroupId,
                                     [NotNullWhen(true)] out ChargingPoolGroup?  ChargingPoolGroup)
        {

            if (chargingStationOperators.TryGet(ChargingPoolGroupId.OperatorId, out var chargingStationOperator) &&
                chargingStationOperator.TryGetChargingPoolGroup(ChargingPoolGroupId, out ChargingPoolGroup))
            {
                return true;
            }

            if (ProviderOf(ChargingPoolGroupId.OperatorId) is EMobilityProvider eMobilityProvider)
                return eMobilityProvider.TryGetChargingPoolGroup(ChargingPoolGroupId, out ChargingPoolGroup);

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

        private readonly ConcurrentDictionary<ParkingSpaceTemplate_Id, ParkingSpaceTemplate> parkingSpaceTemplates = [];

        /// <summary>
        /// The parking space templates of all parking operators.
        /// </summary>
        public IEnumerable<ParkingSpaceTemplate> ParkingSpaceTemplates
            => parkingSpaceTemplates.Values;

        /// <summary>
        /// Try to get the parking space template having the given identification, of any parking operator.
        /// </summary>
        public Boolean TryGetParkingSpaceTemplateById(ParkingSpaceTemplate_Id                        ParkingSpaceTemplateId,
                                                      [NotNullWhen(true)] out ParkingSpaceTemplate?  ParkingSpaceTemplate)

            => parkingSpaceTemplates.TryGetValue(ParkingSpaceTemplateId, out ParkingSpaceTemplate);

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

        /// <summary>
        /// The error, when a public key (by its @id, the thumbprint of the key) belongs to more than one party: a party,
        /// the roaming network operator included, must name only its own keys. A party naming another one's key could
        /// otherwise keep it from signing, as a change set is refused where any party names the signing key with a
        /// validity that does not cover its creation (SPECIFICATION.md, section 4.1).
        /// </summary>
        internal static String? DuplicateKeyAcrossParties(IEnumerable<(POIPartyId Id, IPOIParty Party)> Parties)
        {

            var seen = new Dictionary<String, POIPartyId>(StringComparer.Ordinal);

            foreach (var (partyId, party) in Parties)
                foreach (var key in party.SigningKeys.Concat(party.IdentityKeys))
                {
                    if (seen.TryGetValue(key.Id, out var owner))
                        return $"The key '{key.Id}' of {partyId} is a key of {owner} as well: a key belongs to one party only!";
                    seen[key.Id] = partyId;
                }

            return null;

        }

        #endregion


        #region Energy meter groups

        private readonly ConcurrentDictionary<EnergyMeterGroup_Id, EnergyMeterGroup> energyMeterGroups = [];

        /// <summary>
        /// The energy meter groups of all charging station operators and grid operators.
        /// </summary>
        public IEnumerable<EnergyMeterGroup> EnergyMeterGroups
            => energyMeterGroups.Values;

        /// <summary>
        /// Try to get the energy meter group having the given identification, of any charging station operator or grid operator.
        /// </summary>
        /// <param name="EnergyMeterGroupId">The identification of an energy meter group.</param>
        /// <param name="EnergyMeterGroup">The energy meter group.</param>
        public Boolean TryGetEnergyMeterGroupById(EnergyMeterGroup_Id                        EnergyMeterGroupId,
                                                  [NotNullWhen(true)] out EnergyMeterGroup?  EnergyMeterGroup)

            => energyMeterGroups.TryGetValue(EnergyMeterGroupId, out EnergyMeterGroup);

        private readonly ConcurrentDictionary<EnergyMeterTemplate_Id, EnergyMeterTemplate> energyMeterTemplates = [];

        /// <summary>
        /// The energy meter templates of all charging station operators and grid operators.
        /// </summary>
        public IEnumerable<EnergyMeterTemplate> EnergyMeterTemplates
            => energyMeterTemplates.Values;

        /// <summary>
        /// Try to get the energy meter template having the given identification, of any charging station operator or grid operator.
        /// </summary>
        /// <param name="EnergyMeterTemplateId">The identification of an energy meter template.</param>
        /// <param name="EnergyMeterTemplate">The energy meter template.</param>
        public Boolean TryGetEnergyMeterTemplateById(EnergyMeterTemplate_Id                        EnergyMeterTemplateId,
                                                     [NotNullWhen(true)] out EnergyMeterTemplate?  EnergyMeterTemplate)

            => energyMeterTemplates.TryGetValue(EnergyMeterTemplateId, out EnergyMeterTemplate);

        /// <summary>
        /// The energy meter templates of the given charging station operators and grid operators.
        /// </summary>
        internal static IEnumerable<EnergyMeterTemplate> AllEnergyMeterTemplates(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                                                 IEnumerable<GridOperator>             GridOperators)

            => ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.EnergyMeterTemplates).
                   Concat(GridOperators.SelectMany(gridOperator => gridOperator.EnergyMeterTemplates));

        /// <summary>
        /// The error, when two of the given energy meter templates have the same identification: they are unique within
        /// the roaming network, not only within their owner.
        /// </summary>
        internal static String? DuplicateEnergyMeterTemplate(IEnumerable<EnergyMeterTemplate> EnergyMeterTemplates)
        {

            var seen = new HashSet<EnergyMeterTemplate_Id>();

            foreach (var energyMeterTemplate in EnergyMeterTemplates)
                if (!seen.Add(energyMeterTemplate.Id))
                    return $"The energy meter template '{energyMeterTemplate.Id}' must not be given twice in the roaming network!";

            return null;

        }

        /// <summary>
        /// The energy meter groups of the given charging station operators and grid operators.
        /// </summary>
        internal static IEnumerable<EnergyMeterGroup> AllEnergyMeterGroups(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                                           IEnumerable<GridOperator>             GridOperators)

            => ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.EnergyMeterGroups).
                   Concat(GridOperators.SelectMany(gridOperator => gridOperator.EnergyMeterGroups));

        /// <summary>
        /// The error, when two of the given energy meter groups have the same identification: they are unique within
        /// the roaming network, not only within their owner.
        /// </summary>
        internal static String? DuplicateEnergyMeterGroup(IEnumerable<EnergyMeterGroup> EnergyMeterGroups)
        {

            var seen = new HashSet<EnergyMeterGroup_Id>();

            foreach (var energyMeterGroup in EnergyMeterGroups)
                if (!seen.Add(energyMeterGroup.Id))
                    return $"The energy meter group '{energyMeterGroup.Id}' must not be given twice in the roaming network!";

            return null;

        }

        /// <summary>
        /// The error, when an energy meter template gives its members an unknown manufacturer, an unknown transparency
        /// software release or certificate, or a certificate not covering its release.
        /// </summary>
        internal static String? UnknownEnergyMeterTemplateReference(IEnumerable<EnergyMeterTemplate>                                                     EnergyMeterTemplates,
                                                                 IReadOnlyDictionary<EnergyMeterManufacturer_Id,         EnergyMeterManufacturer>          EnergyMeterManufacturers,
                                                                 IReadOnlyDictionary<TransparencySoftware_Id,            TransparencySoftware>             TransparencySoftware,
                                                                 IReadOnlyDictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>  Certificates)
        {

            foreach (var energyMeterTemplate in EnergyMeterTemplates)
            {

                if (energyMeterTemplate.ManufacturerId.HasValue &&
                    !EnergyMeterManufacturers.ContainsKey(energyMeterTemplate.ManufacturerId.Value))
                {
                    return $"The energy meter template '{energyMeterTemplate.Id}' names the unknown energy meter manufacturer '{energyMeterTemplate.ManufacturerId}'!";
                }

                foreach (var assignment in energyMeterTemplate.TransparencySoftwareStatus)
                {

                    if (!TransparencySoftware.ContainsKey(assignment.TransparencySoftwareId))
                        return $"The transparency software release '{assignment.TransparencySoftwareId}' of the energy meter template '{energyMeterTemplate.Id}' is unknown in the roaming network!";

                    if (assignment.CertificateId.HasValue)
                    {

                        if (!Certificates.TryGetValue(assignment.CertificateId.Value, out var certificate))
                            return $"The certificate '{assignment.CertificateId}' of the energy meter template '{energyMeterTemplate.Id}' is unknown in the roaming network!";

                        if (!certificate.Covers(assignment.TransparencySoftwareId))
                            return $"The certificate '{certificate.Id}' of the energy meter template '{energyMeterTemplate.Id}' does not cover the transparency software release '{assignment.TransparencySoftwareId}'!";

                    }

                }

            }

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
        /// The error, when a charging station or a charging station template names a manufacturer the given ones do not have.
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

            foreach (var chargingStationTemplate in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingStationTemplates))
                if (chargingStationTemplate.ManufacturerId.HasValue &&
                    !ChargingStationManufacturers.ContainsKey(chargingStationTemplate.ManufacturerId.Value))
                {
                    return $"The manufacturer '{chargingStationTemplate.ManufacturerId}' of the charging station template '{chargingStationTemplate.Id}' is unknown in the roaming network!";
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
