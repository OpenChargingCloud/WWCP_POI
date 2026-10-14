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
                                    InfoStatus                                                 ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                                    InfoStatus                                                 ExpandDataLicenses                        = InfoStatus.Expanded,
                                    CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null)
                                    //CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                                    //CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                                    //CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null)


            => ChargingStationOperators?.Any() == true

                   ? new JArray(

                         ChargingStationOperators.
                             Where          (cso => cso is not null).
                             OrderBy        (cso => cso.Id).
                             SkipTakeFilter (Skip, Take).
                             SafeSelect     (cso => cso.ToJSON(Embedded,
                                                               ExpandChargingPoolIds,
                                                               ExpandChargingStationIds,
                                                               ExpandEVSEIds,
                                                               ExpandBrandIds,
                                                               ExpandDataLicenses,
                                                               CustomChargingStationOperatorSerializer
                                                               //CustomChargingPoolSerializer,
                                                               //CustomChargingStationSerializer,
                                                               //CustomEVSESerializer
                                 )).
                             Where          (cso => cso is not null)

                     )

                   : [];

        #endregion


        #region ToJSON(this ChargingStationOperatorAdminStatus, Skip = null, Take = null, HistorySize = 1)

        public static JObject ToJSON(this IEnumerable<KeyValuePair<ChargingStationOperator_Id, IEnumerable<Timestamped<ChargingStationOperatorAdminStatusType>>>>  ChargingStationOperatorAdminStatus,
                                     UInt64?                                                                                                                        Skip         = null,
                                     UInt64?                                                                                                                        Take         = null,
                                     UInt64?                                                                                                                        HistorySize  = 1)

        {

            #region Initial checks

            if (ChargingStationOperatorAdminStatus is null || !ChargingStationOperatorAdminStatus.Any())
                return new JObject();

            var _ChargingStationOperatorAdminStatus = new Dictionary<ChargingStationOperator_Id, IEnumerable<Timestamped<ChargingStationOperatorAdminStatusType>>>();

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

        public static JObject ToJSON(this IEnumerable<KeyValuePair<ChargingStationOperator_Id, IEnumerable<Timestamped<ChargingStationOperatorStatusType>>>>  ChargingStationOperatorStatus,
                                     UInt64?                                                                                                                   Skip         = null,
                                     UInt64?                                                                                                                   Take         = null,
                                     UInt64?                                                                                                                   HistorySize  = 1)

        {

            #region Initial checks

            if (ChargingStationOperatorStatus is null || !ChargingStationOperatorStatus.Any())
                return new JObject();

            var _ChargingStationOperatorStatus = new Dictionary<ChargingStationOperator_Id, IEnumerable<Timestamped<ChargingStationOperatorStatusType>>>();

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
                                                                                    ChargingStationOperatorAdminStatusType,
                                                                                    ChargingStationOperatorStatusType>,
                                                         IPOIParty
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

        ///// <summary>
        ///// All brands registered for this charging station operator.
        ///// </summary>
        //private System.Collections.Immutable.ImmutableArray<Brand> immutableBrands = [];
        //[Optional, SlowData]
        //public System.Collections.Immutable.ImmutableArray<Brand> Brands
        //    => ImmutablePOIValues.CopyItems(immutableBrands);

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
                return address; //ImmutablePOIValues.Copy(address);
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

        #region SigningKeys, IdentityKeys

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>  SigningKeys     { get; }

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>  IdentityKeys    { get; }

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
            => immutableDataLicenses; //ImmutablePOIValues.CopyItems(immutableDataLicenses);


        #endregion

        #region ChargingPools, ChargingStations, EVSEs

        private readonly System.Collections.Immutable.ImmutableDictionary<ChargingPool_Id,    ChargingPool>     chargingPoolIndex;
        private readonly System.Collections.Immutable.ImmutableDictionary<ChargingStation_Id, ChargingStation>  chargingStationIndex;
        private readonly System.Collections.Immutable.ImmutableDictionary<EVSE_Id,            EVSE>             evseIndex;
        private readonly System.Collections.Immutable.ImmutableDictionary<EnergyMeter_Id,     EnergyMeter>      energyMeterIndex;
        private readonly System.Collections.Immutable.ImmutableDictionary<ChargingTariff_Id,  ChargingTariff>   chargingTariffIndex;

        private readonly System.Collections.Immutable.ImmutableDictionary<EVSEGroup_Id, EVSEGroup> evseGroupIndex;

        /// <summary>
        /// The EVSEGroup groups, embedded completely, in the order of their identifications.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<EVSEGroup>  EVSEGroups  { get; } = [];

        /// <summary>
        /// Try to return the EVSEGroup with the given identification.
        /// </summary>
        public Boolean TryGetEVSEGroup(EVSEGroup_Id  EVSEGroupId,
                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EVSEGroup?  EVSEGroup)

            => evseGroupIndex.TryGetValue(EVSEGroupId, out EVSEGroup);

        private readonly System.Collections.Immutable.ImmutableDictionary<ChargingStationGroup_Id, ChargingStationGroup> chargingStationGroupIndex;

        /// <summary>
        /// The ChargingStationGroup groups, embedded completely, in the order of their identifications.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<ChargingStationGroup>  ChargingStationGroups  { get; } = [];

        /// <summary>
        /// Try to return the ChargingStationGroup with the given identification.
        /// </summary>
        public Boolean TryGetChargingStationGroup(ChargingStationGroup_Id  ChargingStationGroupId,
                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingStationGroup?  ChargingStationGroup)

            => chargingStationGroupIndex.TryGetValue(ChargingStationGroupId, out ChargingStationGroup);

        private readonly System.Collections.Immutable.ImmutableDictionary<ChargingPoolGroup_Id, ChargingPoolGroup> chargingPoolGroupIndex;

        /// <summary>
        /// The ChargingPoolGroup groups, embedded completely, in the order of their identifications.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<ChargingPoolGroup>  ChargingPoolGroups  { get; } = [];

        /// <summary>
        /// Try to return the ChargingPoolGroup with the given identification.
        /// </summary>
        public Boolean TryGetChargingPoolGroup(ChargingPoolGroup_Id  ChargingPoolGroupId,
                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingPoolGroup?  ChargingPoolGroup)

            => chargingPoolGroupIndex.TryGetValue(ChargingPoolGroupId, out ChargingPoolGroup);

        private readonly System.Collections.Immutable.ImmutableDictionary<ChargingTariffGroup_Id, ChargingTariffGroup> chargingTariffGroupIndex;

        /// <summary>
        /// The ChargingTariffGroup groups, embedded completely, in the order of their identifications.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<ChargingTariffGroup>  ChargingTariffGroups  { get; } = [];

        /// <summary>
        /// Try to return the ChargingTariffGroup with the given identification.
        /// </summary>
        public Boolean TryGetChargingTariffGroup(ChargingTariffGroup_Id  ChargingTariffGroupId,
                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingTariffGroup?  ChargingTariffGroup)

            => chargingTariffGroupIndex.TryGetValue(ChargingTariffGroupId, out ChargingTariffGroup);

        /// <summary>
        /// The charging tariffs, embedded completely, in the order of their identifications.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<ChargingTariff>  ChargingTariffs  { get; } = [];

        /// <summary>
        /// Try to return the charging tariff with the given identification.
        /// </summary>
        public Boolean TryGetChargingTariff(ChargingTariff_Id                                                    ChargingTariffId,
                                            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingTariff?  ChargingTariff)

            => chargingTariffIndex.TryGetValue(ChargingTariffId, out ChargingTariff);

        /// <summary>
        /// The charging pools, embedded completely with their charging stations and EVSEs, in the order of their identifications.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<ChargingPool>  ChargingPools    { get; } = [];

        /// <summary>
        /// The charging stations of all its charging pools.
        /// </summary>
        public IEnumerable<ChargingStation>                               ChargingStations
            => ChargingPools.SelectMany(chargingPool => chargingPool.ChargingStations);

        /// <summary>
        /// The EVSEs of all its charging pools.
        /// </summary>
        public IEnumerable<EVSE>                                          EVSEs
            => ChargingPools.SelectMany(chargingPool => chargingPool.EVSEs);

        /// <summary>
        /// The energy meters of all its charging pools, their grid connection points, charging stations and EVSEs.
        /// </summary>
        public IEnumerable<EnergyMeter>                                   EnergyMeters
            => ChargingPools.SelectMany(chargingPool => chargingPool.AllEnergyMeters);

        /// <summary>
        /// Try to return the charging pool with the given identification.
        /// </summary>
        public Boolean TryGetChargingPool(ChargingPool_Id                                                 ChargingPoolId,
                                          [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingPool?  ChargingPool)

            => chargingPoolIndex.TryGetValue(ChargingPoolId, out ChargingPool);

        /// <summary>
        /// Try to return the charging station with the given identification, in any of its charging pools.
        /// </summary>
        public Boolean TryGetChargingStation(ChargingStation_Id                                                    ChargingStationId,
                                             [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingStation?  ChargingStation)

            => chargingStationIndex.TryGetValue(ChargingStationId, out ChargingStation);

        /// <summary>
        /// Try to return the EVSE with the given identification, in any of its charging stations.
        /// </summary>
        public Boolean TryGetEVSE(EVSE_Id                                                    EVSEId,
                                  [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EVSE?  EVSE)

            => evseIndex.TryGetValue(EVSEId, out EVSE);

        /// <summary>
        /// Try to return the energy meter with the given identification, anywhere in its charging pools.
        /// </summary>
        public Boolean TryGetEnergyMeter(EnergyMeter_Id                                                    EnergyMeterId,
                                         [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EnergyMeter?  EnergyMeter)

            => energyMeterIndex.TryGetValue(EnergyMeterId, out EnergyMeter);

        /// <summary>
        /// Take the runtime status and internal data of the given previous version of this operator,
        /// and of every charging pool, charging station, EVSE, energy meter, charging tariff and group it had, by their identifications.
        /// </summary>
        internal void ShareRuntimeOfAll(ChargingStationOperator Previous)
        {

            ShareRuntimeOf(Previous);

            foreach (var chargingPool in ChargingPools)
                if (Previous.TryGetChargingPool(chargingPool.Id, out var previousChargingPool))
                    chargingPool.ShareRuntimeOf(previousChargingPool);

            foreach (var chargingStation in ChargingStations)
                if (Previous.TryGetChargingStation(chargingStation.Id, out var previousChargingStation))
                    chargingStation.ShareRuntimeOf(previousChargingStation);

            foreach (var evse in EVSEs)
                if (Previous.TryGetEVSE(evse.Id, out var previousEVSE))
                    evse.ShareRuntimeOf(previousEVSE);

            foreach (var energyMeter in EnergyMeters)
                if (Previous.TryGetEnergyMeter(energyMeter.Id, out var previousEnergyMeter))
                    energyMeter.ShareRuntimeOf(previousEnergyMeter);

            foreach (var chargingTariff in ChargingTariffs)
                if (Previous.TryGetChargingTariff(chargingTariff.Id, out var previousChargingTariff))
                    chargingTariff.ShareRuntimeOf(previousChargingTariff);

            foreach (var evseGroup in EVSEGroups)
                if (Previous.TryGetEVSEGroup(evseGroup.Id, out var previousEVSEGroup))
                    evseGroup.ShareRuntimeOf(previousEVSEGroup);

            foreach (var chargingStationGroup in ChargingStationGroups)
                if (Previous.TryGetChargingStationGroup(chargingStationGroup.Id, out var previousChargingStationGroup))
                    chargingStationGroup.ShareRuntimeOf(previousChargingStationGroup);

            foreach (var chargingPoolGroup in ChargingPoolGroups)
                if (Previous.TryGetChargingPoolGroup(chargingPoolGroup.Id, out var previousChargingPoolGroup))
                    chargingPoolGroup.ShareRuntimeOf(previousChargingPoolGroup);

            foreach (var chargingTariffGroup in ChargingTariffGroups)
                if (Previous.TryGetChargingTariffGroup(chargingTariffGroup.Id, out var previousChargingTariffGroup))
                    chargingTariffGroup.ShareRuntimeOf(previousChargingTariffGroup);

        }

        #endregion


        //ToDo: Technical reachability via: DNS-SRV / DNS-HTTPS Records and/or EV-RoamingNetworks


        // EVRoamingPartners

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
        /// <param name="Logo">An optional logo.</param>
        /// <param name="Address">An optional address of the headquarter.</param>
        /// <param name="GeoLocation">An optional geographical location.</param>
        /// <param name="Telephone">An optional telephone number.</param>
        /// <param name="EMailAddress">An optional e-mail address.</param>
        /// <param name="Homepage">An optional homepage.</param>
        /// <param name="HotlinePhoneNumber">An optional hotline phone number.</param>
        /// <param name="TermsAndConditionsURL">An optional URL of the terms and conditions.</param>
        /// <param name="DataLicenses">Optional data licenses.</param>
        public ChargingStationOperator(ChargingStationOperator_Id                            Id,
                                       //RoamingNetwork                                        RoamingNetwork,
                                       I18NString?                                           Name                                   = null,
                                       I18NString?                                           Description                            = null,

                                       URL?                                                  Logo                                   = null,
                                       Address?                                              Address                                = null,
                                       GeoCoordinate?                                        GeoLocation                            = null,
                                       PhoneNumber?                                          Telephone                              = null,
                                       SimpleEMailAddress?                                   EMailAddress                           = null,
                                       URL?                                                  Homepage                               = null,
                                       PhoneNumber?                                          HotlinePhoneNumber                     = null,
                                       URL?                                                  TermsAndConditionsURL                  = null,
                                       IEnumerable<DataLicense>?                             DataLicenses                           = null,
                                       IEnumerable<ChargingPool>?                            ChargingPools                          = null,
                                       IEnumerable<ChargingTariff>?                          ChargingTariffs                        = null,
                                       IEnumerable<EVSEGroup>?                               EVSEGroups                              = null,
                                       IEnumerable<ChargingStationGroup>?                    ChargingStationGroups                   = null,
                                       IEnumerable<ChargingPoolGroup>?                       ChargingPoolGroups                      = null,
                                       IEnumerable<ChargingTariffGroup>?                     ChargingTariffGroups                    = null,
                                       IEnumerable<POIPublicKey>?                            SigningKeys                             = null,
                                       IEnumerable<POIPublicKey>?                            IdentityKeys                            = null,

                                       Timestamped<ChargingStationOperatorAdminStatusType>?  InitialAdminStatus                     = null,
                                       Timestamped<ChargingStationOperatorStatusType>?       InitialStatus                          = null,
                                       UInt16?                                               MaxAdminStatusScheduleSize             = DefaultMaxAdminStatusScheduleSize,
                                       UInt16?                                               MaxStatusScheduleSize                  = DefaultMaxStatusScheduleSize,

                                       String?                                               DataSource                             = null,
                                       DateTimeOffset?                                       Created                                = null,
                                       DateTimeOffset?                                       LastChange                             = null,

                                       CustomDataNew?                                        CustomData                             = null,
                                       UserDefinedDictionary?                                InternalData                           = null)

            : base(Id,
                   Name ?? I18NString.Create("Charging Station Operator " + Id.ToString()),
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingStationOperatorAdminStatusType>(Created ?? Timestamp.Now, ChargingStationOperatorAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingStationOperatorStatusType>     (Created ?? Timestamp.Now, ChargingStationOperatorStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxChargingStationOperatorAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxChargingStationOperatorStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            //this.chargingPools = new EntityHashSet<ChargingStationOperator, ChargingPool_Id, ChargingPool>(this);

            this.logo                   = Logo;
            this.address                = Address;
            this.geoLocation            = GeoLocation;
            this.telephone              = Telephone;
            this.eMailAddress           = EMailAddress;
            this.homepage               = Homepage;
            this.hotlinePhoneNumber     = HotlinePhoneNumber;
            this.termsAndConditionsURL  = TermsAndConditionsURL;

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the charging station operator '{Id}'");

            var dataLicenses = (DataLicenses ?? []).ToArray();

            if (dataLicenses.Any(dataLicense => dataLicense is null))
                throw new ArgumentException("Data licenses must not contain null entries.", nameof(DataLicenses));

            if (dataLicenses.Select(dataLicense => dataLicense.Id).Distinct().Count() != dataLicenses.Length)
                throw new ArgumentException("A data license must not be given twice.", nameof(DataLicenses));

            this.immutableDataLicenses  = [.. dataLicenses.Ordered()];

            #region Charging pools, unique by their identifications, as their charging stations and EVSEs

            var chargingPools = (ChargingPools ?? []).ToArray();

            if (chargingPools.Any(chargingPool => chargingPool is null))
                throw new ArgumentException("Charging pools must not contain null entries!", nameof(ChargingPools));

            foreach (var chargingPool in chargingPools)
                if (chargingPool.Id.OperatorId != Id)
                    throw new ArgumentException($"The charging pool '{chargingPool.Id}' does not belong to the operator '{Id}'!", nameof(ChargingPools));

            this.ChargingPools = [.. chargingPools.OrderBy(chargingPool => chargingPool.Id.ToString(), StringComparer.Ordinal)];

            try
            {
                this.chargingPoolIndex     = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.ChargingPools.   Select(chargingPool    => new KeyValuePair<ChargingPool_Id,    ChargingPool>   (chargingPool.Id,    chargingPool)));
                this.chargingStationIndex  = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.ChargingStations.Select(chargingStation => new KeyValuePair<ChargingStation_Id, ChargingStation>(chargingStation.Id, chargingStation)));
                this.evseIndex             = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.EVSEs.           Select(evse            => new KeyValuePair<EVSE_Id,            EVSE>           (evse.Id,            evse)));
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"A charging pool, charging station or EVSE of the operator '{Id}' must not be given twice!", nameof(ChargingPools));
            }

            try
            {
                this.energyMeterIndex      = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.EnergyMeters.    Select(energyMeter     => new KeyValuePair<EnergyMeter_Id,     EnergyMeter>    (energyMeter.Id,     energyMeter)));
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"An energy meter of the operator '{Id}' must not be given twice!", nameof(ChargingPools));
            }

            #endregion

            #region Charging tariffs, of this operator, and the ones its EVSEs name

            var chargingTariffs = (ChargingTariffs ?? []).ToArray();

            if (chargingTariffs.Any(chargingTariff => chargingTariff is null))
                throw new ArgumentException("Charging tariffs must not contain null entries!", nameof(ChargingTariffs));

            foreach (var chargingTariff in chargingTariffs)
                if (chargingTariff.Id.OperatorId != Id)
                    throw new ArgumentException($"The charging tariff '{chargingTariff.Id}' does not belong to the operator '{Id}'!", nameof(ChargingTariffs));

            this.ChargingTariffs = [.. chargingTariffs.OrderBy(chargingTariff => chargingTariff.Id.ToString(), StringComparer.Ordinal)];

            try
            {
                this.chargingTariffIndex   = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.ChargingTariffs. Select(chargingTariff  => new KeyValuePair<ChargingTariff_Id,  ChargingTariff> (chargingTariff.Id,  chargingTariff)));
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"A charging tariff of the operator '{Id}' must not be given twice!", nameof(ChargingTariffs));
            }

            foreach (var evse in EVSEs)
                foreach (var chargingTariffId in evse.ChargingTariffIds)
                    if (!chargingTariffIndex.ContainsKey(chargingTariffId))
                        throw new ArgumentException($"The charging tariff '{chargingTariffId}' of the EVSE '{evse.Id}' is unknown at the operator '{Id}'!", nameof(ChargingTariffs));

            #endregion

            #region Groups, of this operator, of its members


            {

                var evseGroups = (EVSEGroups ?? []).ToArray();

                if (evseGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(EVSEGroups));

                this.EVSEGroups = [.. evseGroups.OrderBy(group => group.Id.ToString(), StringComparer.Ordinal)];

                try
                {
                    this.evseGroupIndex = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.EVSEGroups.Select(group => new KeyValuePair<EVSEGroup_Id, EVSEGroup>(group.Id, group)));
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException($"A group of the operator '{Id}' must not be given twice!", nameof(EVSEGroups));
                }

                foreach (var evseGroup in this.EVSEGroups)
                {

                    if (evseGroup.Id.OperatorId != Id)
                        throw new ArgumentException($"The group '{evseGroup.Id}' does not belong to the operator '{Id}'!", nameof(EVSEGroups));

                    foreach (var memberId in evseGroup.EVSEIds)
                        if (!evseIndex.ContainsKey(memberId))
                            throw new ArgumentException($"The member '{memberId}' of the group '{evseGroup.Id}' is unknown at the operator '{Id}'!", nameof(EVSEGroups));

                if (evseGroup.ChargingTariffId.HasValue && !chargingTariffIndex.ContainsKey(evseGroup.ChargingTariffId.Value))
                    throw new ArgumentException($"The charging tariff '{evseGroup.ChargingTariffId}' of the group '{evseGroup.Id}' is unknown at the operator '{Id}'!", nameof(EVSEGroups));

                }

            }

            {

                var chargingStationGroups = (ChargingStationGroups ?? []).ToArray();

                if (chargingStationGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(ChargingStationGroups));

                this.ChargingStationGroups = [.. chargingStationGroups.OrderBy(group => group.Id.ToString(), StringComparer.Ordinal)];

                try
                {
                    this.chargingStationGroupIndex = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.ChargingStationGroups.Select(group => new KeyValuePair<ChargingStationGroup_Id, ChargingStationGroup>(group.Id, group)));
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException($"A group of the operator '{Id}' must not be given twice!", nameof(ChargingStationGroups));
                }

                foreach (var chargingStationGroup in this.ChargingStationGroups)
                {

                    if (chargingStationGroup.Id.OperatorId != Id)
                        throw new ArgumentException($"The group '{chargingStationGroup.Id}' does not belong to the operator '{Id}'!", nameof(ChargingStationGroups));

                    foreach (var memberId in chargingStationGroup.ChargingStationIds)
                        if (!chargingStationIndex.ContainsKey(memberId))
                            throw new ArgumentException($"The member '{memberId}' of the group '{chargingStationGroup.Id}' is unknown at the operator '{Id}'!", nameof(ChargingStationGroups));

                if (chargingStationGroup.ChargingTariffId.HasValue && !chargingTariffIndex.ContainsKey(chargingStationGroup.ChargingTariffId.Value))
                    throw new ArgumentException($"The charging tariff '{chargingStationGroup.ChargingTariffId}' of the group '{chargingStationGroup.Id}' is unknown at the operator '{Id}'!", nameof(ChargingStationGroups));

                }

            }

            {

                var chargingPoolGroups = (ChargingPoolGroups ?? []).ToArray();

                if (chargingPoolGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(ChargingPoolGroups));

                this.ChargingPoolGroups = [.. chargingPoolGroups.OrderBy(group => group.Id.ToString(), StringComparer.Ordinal)];

                try
                {
                    this.chargingPoolGroupIndex = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.ChargingPoolGroups.Select(group => new KeyValuePair<ChargingPoolGroup_Id, ChargingPoolGroup>(group.Id, group)));
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException($"A group of the operator '{Id}' must not be given twice!", nameof(ChargingPoolGroups));
                }

                foreach (var chargingPoolGroup in this.ChargingPoolGroups)
                {

                    if (chargingPoolGroup.Id.OperatorId != Id)
                        throw new ArgumentException($"The group '{chargingPoolGroup.Id}' does not belong to the operator '{Id}'!", nameof(ChargingPoolGroups));

                    foreach (var memberId in chargingPoolGroup.ChargingPoolIds)
                        if (!chargingPoolIndex.ContainsKey(memberId))
                            throw new ArgumentException($"The member '{memberId}' of the group '{chargingPoolGroup.Id}' is unknown at the operator '{Id}'!", nameof(ChargingPoolGroups));

                if (chargingPoolGroup.ChargingTariffId.HasValue && !chargingTariffIndex.ContainsKey(chargingPoolGroup.ChargingTariffId.Value))
                    throw new ArgumentException($"The charging tariff '{chargingPoolGroup.ChargingTariffId}' of the group '{chargingPoolGroup.Id}' is unknown at the operator '{Id}'!", nameof(ChargingPoolGroups));

                }

            }

            {

                var chargingTariffGroups = (ChargingTariffGroups ?? []).ToArray();

                if (chargingTariffGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(ChargingTariffGroups));

                this.ChargingTariffGroups = [.. chargingTariffGroups.OrderBy(group => group.Id.ToString(), StringComparer.Ordinal)];

                try
                {
                    this.chargingTariffGroupIndex = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.ChargingTariffGroups.Select(group => new KeyValuePair<ChargingTariffGroup_Id, ChargingTariffGroup>(group.Id, group)));
                }
                catch (ArgumentException)
                {
                    throw new ArgumentException($"A group of the operator '{Id}' must not be given twice!", nameof(ChargingTariffGroups));
                }

                foreach (var chargingTariffGroup in this.ChargingTariffGroups)
                {

                    if (chargingTariffGroup.Id.OperatorId != Id)
                        throw new ArgumentException($"The group '{chargingTariffGroup.Id}' does not belong to the operator '{Id}'!", nameof(ChargingTariffGroups));

                    foreach (var memberId in chargingTariffGroup.ChargingTariffIds)
                        if (!chargingTariffIndex.ContainsKey(memberId))
                            throw new ArgumentException($"The member '{memberId}' of the group '{chargingTariffGroup.Id}' is unknown at the operator '{Id}'!", nameof(ChargingTariffGroups));

                }

            }

            #endregion

        }

        #endregion


        #region  Data/(Admin-)Status management

        #region OnData/(Admin)StatusChanged

        /// <summary>
        /// An event fired whenever the static data changed.
        /// </summary>
        public event OnChargingStationOperatorDataChangedDelegate?         OnDataChanged;

        private readonly Lock                                                statusEventsLock  = new ();
        private          OnChargingStationOperatorAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingStationOperatorStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingStationOperatorAdminStatusChangedDelegate?  OnAdminStatusChanged
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
        public event OnChargingStationOperatorStatusChangedDelegate?       OnStatusChanged
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
        protected override Task OnAdminStatusScheduleChanged(DateTimeOffset                                       Timestamp,
                                                             EventTracking_Id                                     EventTrackingId,
                                                             Timestamped<ChargingStationOperatorAdminStatusType>  NewStatus,
                                                             Timestamped<ChargingStationOperatorAdminStatusType>  OldStatus,
                                                             Context?                                             DataSource)

            => UpdateAdminStatus(Timestamp, EventTrackingId, OldStatus, NewStatus);

        /// <inheritdoc/>
        protected override Task OnStatusScheduleChanged(DateTimeOffset                                  Timestamp,
                                                        EventTracking_Id                                EventTrackingId,
                                                        Timestamped<ChargingStationOperatorStatusType>  NewStatus,
                                                        Timestamped<ChargingStationOperatorStatusType>  OldStatus,
                                                        Context?                                        DataSource)

            => UpdateStatus(Timestamp, EventTrackingId, OldStatus, NewStatus);

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
                                              Timestamped<ChargingStationOperatorAdminStatusType>  OldStatus,
                                              Timestamped<ChargingStationOperatorAdminStatusType>  NewStatus)
        {

            var onAdminStatusChanged = this.onAdminStatusChanged;
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
                                         Timestamped<ChargingStationOperatorStatusType>  OldStatus,
                                         Timestamped<ChargingStationOperatorStatusType>  NewStatus)
        {

            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp,
                                      EventTrackingId,
                                      this,
                                      OldStatus,
                                      NewStatus);

        }

        #endregion

        #endregion


        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation for the given charging station operator.
        /// </summary>
        /// <param name="Embedded">Whether this data is embedded into another data structure, e.g. into a roaming network.</param>
        public JObject ToJSON(Boolean                                                    Embedded                                  = false,
                              InfoStatus                                                 ExpandChargingPoolIds                     = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandChargingStationIds                  = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandEVSEIds                             = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandBrandIds                            = InfoStatus.ShowIdOnly,
                              InfoStatus                                                 ExpandDataLicenses                        = InfoStatus.Expanded,
                              CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null,
                              //CustomJObjectSerializerDelegate<ChargingPool>?             CustomChargingPoolSerializer              = null,
                              //CustomJObjectSerializerDelegate<ChargingStation>?          CustomChargingStationSerializer           = null,
                              //CustomJObjectSerializerDelegate<EVSE>?                     CustomEVSESerializer                      = null,
                              //CustomJObjectSerializerDelegate<ChargingConnector>?        CustomChargingConnectorSerializer         = null,
                              InfoStatus                                                 ExpandChargingTariffIds                   = InfoStatus.Expanded)
        {

            try
            {

                var json = JSONObject.Create(

                                     new JProperty("@id",                 Id.ToString()),

                               //ExpandChargingTariffIds != InfoStatus.Hidden && ChargingTariffs.Any()
                               //    ? ExpandChargingTariffIds.Switch(
                               //          () => new JProperty("chargingTariffIds", new JArray(ChargingTariffs.OrderBy(tariff => tariff.Id.ToString(), StringComparer.Ordinal).Select(tariff => tariff.Id.ToString()))),
                               //          () => new JProperty("chargingTariffs", new JArray(ChargingTariffs.OrderBy(tariff => tariff.Id.ToString(), StringComparer.Ordinal).Select(tariff => tariff.ToJSON(Embedded: true, ExpandBrandIds: ExpandBrandIds, ExpandDataLicenses: ExpandDataLicenses)))))
                               //    : null,

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

                               // No data licenses, no property: as the roaming network and its charging pools write them.
                               ExpandDataLicenses != InfoStatus.Hidden && DataLicenses.Any()
                                   ? ExpandDataLicenses.Switch(
                                         () => new JProperty("dataLicenseIds",  DataLicenses.ToPOIIdsJSON()),
                                         () => new JProperty("dataLicenses",    DataLicenses.ToPOIJSON()))
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

                               GeoLocation.HasValue
                                   ? new JProperty("geoLocation",         GeoLocation.Value.ToJSON(Embedded: true))
                                   : null,

                               Telephone.HasValue
                                   ? new JProperty("telephone",           Telephone.ToString())
                                   : null,

                               EMailAddress.HasValue
                                   ? new JProperty("eMailAddress",        EMailAddress.ToString())
                                   : null,

                               Homepage.HasValue
                                   ? new JProperty("homepage",            Homepage.ToString())
                                   : null,

                               HotlinePhoneNumber.HasValue
                                   ? new JProperty("hotline",             HotlinePhoneNumber.ToString())
                                   : null,

                               TermsAndConditionsURL.HasValue
                                   ? new JProperty("termsAndConditions",  TermsAndConditionsURL.ToString())
                                   : null,


                               //ExpandChargingPoolIds != InfoStatus.Hidden && ChargingPools.Any()
                               //    ? ExpandChargingPoolIds.Switch(

                               //          () => new JProperty("chargingPoolIds",
                               //                              new JArray(ChargingPoolIds().
                               //                                                                 OrderBy(poolId => poolId).
                               //                                                                 Select (poolId => poolId.ToString()))),

                               //          () => new JProperty("chargingPools",
                               //                              new JArray(ChargingPools.
                               //                                                                 OrderBy(poolId => poolId).
                               //                                                                 ToJSON (Embedded:                           true,
                               //                                                                         ExpandRoamingNetworkId:             InfoStatus.Hidden,
                               //                                                                         ExpandChargingStationOperatorId:    InfoStatus.Hidden,
                               //                                                                         ExpandChargingStationIds:           ExpandChargingStationIds,
                               //                                                                         ExpandEVSEIds:                      ExpandEVSEIds,
                               //                                                                         ExpandBrandIds:                     ExpandBrandIds,
                               //                                                                         ExpandDataLicenses:                 ExpandDataLicenses,
                               //                                                                         CustomChargingPoolSerializer:       CustomChargingPoolSerializer,
                               //                                                                         CustomChargingStationSerializer:    CustomChargingStationSerializer,
                               //                                                                         CustomEVSESerializer:               CustomEVSESerializer,
                               //                                                                         CustomChargingConnectorSerializer:  CustomChargingConnectorSerializer))))
                               //    : null,


                               //ExpandChargingPoolIds != InfoStatus.Expanded && ExpandChargingStationIds != InfoStatus.Hidden && ChargingStations.Any()
                               //    ? ExpandChargingStationIds.Switch(

                               //          () => new JProperty("chargingStationIds",
                               //                              new JArray(ChargingStationIds().
                               //                                                                 OrderBy(stationid => stationid).
                               //                                                                 Select (stationid => stationid.ToString()))),

                               //          () => new JProperty("chargingStations",
                               //                              new JArray(ChargingStations.
                               //                                                                 OrderBy(station   => station).
                               //                                                                 ToJSON (Embedded:                         true,
                               //                                                                         ExpandRoamingNetworkId:           InfoStatus.Hidden,
                               //                                                                         ExpandChargingStationOperatorId:  InfoStatus.Hidden,
                               //                                                                         ExpandChargingPoolId:             InfoStatus.Hidden,
                               //                                                                         ExpandEVSEIds:                    InfoStatus.Expanded,
                               //                                                                         ExpandBrandIds:                   InfoStatus.ShowIdOnly,
                               //                                                                         ExpandDataLicenses:               InfoStatus.Hidden,
                               //                                                                         CustomChargingStationSerializer:  CustomChargingStationSerializer,
                               //                                                                         CustomEVSESerializer:             CustomEVSESerializer))))
                               //    : null,


                               //ExpandChargingPoolIds != InfoStatus.Expanded && ExpandChargingStationIds != InfoStatus.Expanded && ExpandEVSEIds != InfoStatus.Hidden && EVSEs.Any()
                               //    ? ExpandEVSEIds.Switch(

                               //          () => new JProperty("EVSEIds",
                               //                              new JArray(EVSEIds().
                               //                                                                 OrderBy(evseId => evseId).
                               //                                                                 Select (evseId => evseId.ToString()))),

                               //          () => new JProperty("EVSEs",
                               //                              new JArray(EVSEs.
                               //                                                                 OrderBy(evse   => evse).
                               //                                                                 ToJSON (Embedded:                         true,
                               //                                                                         ExpandRoamingNetworkId:           InfoStatus.Hidden,
                               //                                                                         ExpandChargingStationOperatorId:  InfoStatus.Hidden,
                               //                                                                         ExpandChargingPoolId:             InfoStatus.Hidden,
                               //                                                                         ExpandChargingStationId:          InfoStatus.Hidden,
                               //                                                                         ExpandBrandIds:                   InfoStatus.ShowIdOnly,
                               //                                                                         ExpandDataLicenses:               InfoStatus.Hidden,
                               //                                                                         CustomEVSESerializer:             CustomEVSESerializer))))
                               //    : null,


                               //ExpandBrandIds != InfoStatus.Hidden && Brands.Any()
                               //    ? ExpandBrandIds.Switch(

                               //          () => new JProperty("brandIds",
                               //                              new JArray(Brands.                 Select (brand   => brand.Id).
                               //                                                                 OrderBy(brandId => brandId).
                               //                                                                 Select (brandId => brandId.ToString()))),

                               //          () => new JProperty("brands",
                               //                              new JArray(Brands.
                               //                                                                 OrderBy(brand => brand).
                               //                                                                 ToJSON (Embedded:                         true,
                               //                                                                         ExpandDataLicenses:               InfoStatus.ShowIdOnly))))
                               //    : null,

                               // created, lastChange and customData: see below
                               null

                               );

                // Embedded completely, as content.
                if (ChargingTariffs.Length > 0)
                    json.Add("chargingTariffs", new JArray(ChargingTariffs.Select(chargingTariff => chargingTariff.ToJSON())));

                if (ChargingPools.Length > 0)
                    json.Add("chargingPools", new JArray(ChargingPools.Select(chargingPool => chargingPool.ToJSON())));

                if (EVSEGroups.Length > 0)
                    json.Add("EVSEGroups", new JArray(EVSEGroups.Select(group => group.ToJSON())));

                if (ChargingStationGroups.Length > 0)
                    json.Add("chargingStationGroups", new JArray(ChargingStationGroups.Select(group => group.ToJSON())));

                if (ChargingPoolGroups.Length > 0)
                    json.Add("chargingPoolGroups", new JArray(ChargingPoolGroups.Select(group => group.ToJSON())));

                if (ChargingTariffGroups.Length > 0)
                    json.Add("chargingTariffGroups", new JArray(ChargingTariffGroups.Select(group => group.ToJSON())));

                POIPartyKeys.AddTo(json, this);

                // Always part of the content.
                POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

                //json["EVSEGroups"] = POIJSON.Children(EVSEGroups);
                //json["chargingStationGroups"] = POIJSON.Children(ChargingStationGroups);
                //json["chargingPoolGroups"] = POIJSON.Children(ChargingPoolGroups);
                //json["chargingTariffGroups"] = POIJSON.Children(ChargingTariffGroups);

                //return POIRepresentation.AddETags(this, CustomChargingStationOperatorSerializer is not null
                //           ? CustomChargingStationOperatorSerializer(this, json)
                //           : json);


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
                   ")"
               );

        #endregion


    }

}
