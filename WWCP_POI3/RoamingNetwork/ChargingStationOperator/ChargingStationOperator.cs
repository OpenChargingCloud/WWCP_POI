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

        /// <summary>
        /// The brands it is a white-label solution for: its own, embedded completely, in the order of their identifications.
        /// Not those of its charging pools, charging stations or EVSEs; every level has brands of its own.
        /// </summary>
        public POICollection<Brand_Id, Brand>  Brands  { get; } = POICollection<Brand_Id, Brand>.Empty;

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

        private readonly System.Collections.Immutable.ImmutableDictionary<ChargingStation_Id, ChargingStation>  chargingStationIndex;
        private readonly System.Collections.Immutable.ImmutableDictionary<EVSE_Id,            EVSE>             evseIndex;
        private readonly System.Collections.Immutable.ImmutableDictionary<EnergyMeter_Id,     EnergyMeter>      energyMeterIndex;

        /// <summary>
        /// The EVSE templates, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<EVSETemplate_Id, EVSETemplate>                        EVSETemplates             { get; } = POICollection<EVSETemplate_Id, EVSETemplate>.Empty;

        /// <summary>
        /// The charging station templates, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingStationTemplate_Id, ChargingStationTemplate>  ChargingStationTemplates  { get; } = POICollection<ChargingStationTemplate_Id, ChargingStationTemplate>.Empty;

        /// <summary>
        /// The charging pool templates, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingPoolTemplate_Id, ChargingPoolTemplate>        ChargingPoolTemplates     { get; } = POICollection<ChargingPoolTemplate_Id, ChargingPoolTemplate>.Empty;

        /// <summary>
        /// The charging tariff templates, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingTariffTemplate_Id, ChargingTariffTemplate>    ChargingTariffTemplates   { get; } = POICollection<ChargingTariffTemplate_Id, ChargingTariffTemplate>.Empty;

        /// <summary>
        /// The templates of the energy meters of its charging pools, charging stations and EVSEs, embedded completely,
        /// in the order of their identifications.
        /// </summary>
        public POICollection<EnergyMeterTemplate_Id, EnergyMeterTemplate>          EnergyMeterTemplates      { get; } = POICollection<EnergyMeterTemplate_Id, EnergyMeterTemplate>.Empty;

        /// <summary>
        /// Its charging tariff offers at its own EVSEs, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingTariffOffer_Id, ChargingTariffOffer>          ChargingTariffOffers      { get; } = POICollection<ChargingTariffOffer_Id, ChargingTariffOffer>.Empty;

        /// <summary>
        /// Try to return the charging tariff offer with the given identification.
        /// </summary>
        public Boolean TryGetChargingTariffOffer(ChargingTariffOffer_Id                                                    ChargingTariffOfferId,
                                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingTariffOffer?  ChargingTariffOffer)

            => ChargingTariffOffers.TryGet(ChargingTariffOfferId, out ChargingTariffOffer);

        /// <summary>
        /// Try to return the EVSE template with the given identification.
        /// </summary>
        public Boolean TryGetEVSETemplate(EVSETemplate_Id                                                    EVSETemplateId,
                                          [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EVSETemplate?  EVSETemplate)

            => EVSETemplates.TryGet(EVSETemplateId, out EVSETemplate);

        /// <summary>
        /// Try to return the charging station template with the given identification.
        /// </summary>
        public Boolean TryGetChargingStationTemplate(ChargingStationTemplate_Id                                                    ChargingStationTemplateId,
                                                     [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingStationTemplate?  ChargingStationTemplate)

            => ChargingStationTemplates.TryGet(ChargingStationTemplateId, out ChargingStationTemplate);

        /// <summary>
        /// Try to return the charging pool template with the given identification.
        /// </summary>
        public Boolean TryGetChargingPoolTemplate(ChargingPoolTemplate_Id                                                    ChargingPoolTemplateId,
                                                  [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingPoolTemplate?  ChargingPoolTemplate)

            => ChargingPoolTemplates.TryGet(ChargingPoolTemplateId, out ChargingPoolTemplate);

        /// <summary>
        /// Try to return the charging tariff template with the given identification.
        /// </summary>
        public Boolean TryGetChargingTariffTemplate(ChargingTariffTemplate_Id                                                    ChargingTariffTemplateId,
                                                    [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingTariffTemplate?  ChargingTariffTemplate)

            => ChargingTariffTemplates.TryGet(ChargingTariffTemplateId, out ChargingTariffTemplate);

        /// <summary>
        /// Try to return the energy meter template with the given identification.
        /// </summary>
        public Boolean TryGetEnergyMeterTemplate(EnergyMeterTemplate_Id                                                    EnergyMeterTemplateId,
                                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EnergyMeterTemplate?  EnergyMeterTemplate)

            => EnergyMeterTemplates.TryGet(EnergyMeterTemplateId, out EnergyMeterTemplate);

        /// <summary>
        /// The EVSE groups, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<EVSEGroup_Id, EVSEGroup>  EVSEGroups  { get; } = POICollection<EVSEGroup_Id, EVSEGroup>.Empty;

        /// <summary>
        /// Try to return the EVSEGroup with the given identification.
        /// </summary>
        public Boolean TryGetEVSEGroup(EVSEGroup_Id  EVSEGroupId,
                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EVSEGroup?  EVSEGroup)

            => EVSEGroups.TryGet(EVSEGroupId, out EVSEGroup);

        /// <summary>
        /// The charging station groups, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingStationGroup_Id, ChargingStationGroup>  ChargingStationGroups  { get; } = POICollection<ChargingStationGroup_Id, ChargingStationGroup>.Empty;

        /// <summary>
        /// Try to return the ChargingStationGroup with the given identification.
        /// </summary>
        public Boolean TryGetChargingStationGroup(ChargingStationGroup_Id  ChargingStationGroupId,
                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingStationGroup?  ChargingStationGroup)

            => ChargingStationGroups.TryGet(ChargingStationGroupId, out ChargingStationGroup);

        /// <summary>
        /// The charging pool groups, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingPoolGroup_Id, ChargingPoolGroup>  ChargingPoolGroups  { get; } = POICollection<ChargingPoolGroup_Id, ChargingPoolGroup>.Empty;

        /// <summary>
        /// Try to return the ChargingPoolGroup with the given identification.
        /// </summary>
        public Boolean TryGetChargingPoolGroup(ChargingPoolGroup_Id  ChargingPoolGroupId,
                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingPoolGroup?  ChargingPoolGroup)

            => ChargingPoolGroups.TryGet(ChargingPoolGroupId, out ChargingPoolGroup);

        /// <summary>
        /// The charging tariff groups, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingTariffGroup_Id, ChargingTariffGroup>  ChargingTariffGroups  { get; } = POICollection<ChargingTariffGroup_Id, ChargingTariffGroup>.Empty;

        /// <summary>
        /// Try to return the ChargingTariffGroup with the given identification.
        /// </summary>
        public Boolean TryGetChargingTariffGroup(ChargingTariffGroup_Id  ChargingTariffGroupId,
                                 [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingTariffGroup?  ChargingTariffGroup)

            => ChargingTariffGroups.TryGet(ChargingTariffGroupId, out ChargingTariffGroup);

        /// <summary>
        /// The groups of the energy meters of its charging pools, charging stations and EVSEs, embedded completely,
        /// in the order of their identifications.
        /// </summary>
        public POICollection<EnergyMeterGroup_Id, EnergyMeterGroup>  EnergyMeterGroups  { get; } = POICollection<EnergyMeterGroup_Id, EnergyMeterGroup>.Empty;

        /// <summary>
        /// Try to return the energy meter group with the given identification.
        /// </summary>
        public Boolean TryGetEnergyMeterGroup(EnergyMeterGroup_Id                                                    EnergyMeterGroupId,
                                              [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out EnergyMeterGroup?  EnergyMeterGroup)

            => EnergyMeterGroups.TryGet(EnergyMeterGroupId, out EnergyMeterGroup);

        /// <summary>
        /// The parking spaces it defines itself, where no parking operator does: embedded completely, in the order of
        /// their identifications. They are in no parking garage, and have no parking sensors or parking products.
        /// </summary>
        public POICollection<ParkingSpace_Id, ParkingSpace>  ParkingSpaces  { get; } = POICollection<ParkingSpace_Id, ParkingSpace>.Empty;

        /// <summary>
        /// Try to return the parking space it defines with the given identification.
        /// </summary>
        public Boolean TryGetParkingSpace(ParkingSpace_Id  ParkingSpaceId,
                                          [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ParkingSpace?  ParkingSpace)

            => ParkingSpaces.TryGet(ParkingSpaceId, out ParkingSpace);

        /// <summary>
        /// The charging tariffs, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingTariff_Id, ChargingTariff>  ChargingTariffs  { get; } = POICollection<ChargingTariff_Id, ChargingTariff>.Empty;

        /// <summary>
        /// Try to return the charging tariff with the given identification.
        /// </summary>
        public Boolean TryGetChargingTariff(ChargingTariff_Id                                                    ChargingTariffId,
                                            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ChargingTariff?  ChargingTariff)

            => ChargingTariffs.TryGet(ChargingTariffId, out ChargingTariff);

        /// <summary>
        /// The charging pools, embedded completely with their charging stations and EVSEs, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingPool_Id, ChargingPool>  ChargingPools    { get; } = POICollection<ChargingPool_Id, ChargingPool>.Empty;

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

            => ChargingPools.TryGet(ChargingPoolId, out ChargingPool);

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

            foreach (var energyMeterGroup in EnergyMeterGroups)
                if (Previous.TryGetEnergyMeterGroup(energyMeterGroup.Id, out var previousEnergyMeterGroup))
                    energyMeterGroup.ShareRuntimeOf(previousEnergyMeterGroup);

            foreach (var chargingTariffOffer in ChargingTariffOffers)
                if (Previous.TryGetChargingTariffOffer(chargingTariffOffer.Id, out var previousChargingTariffOffer))
                    chargingTariffOffer.ShareRuntimeOf(previousChargingTariffOffer);

            foreach (var parkingSpace in ParkingSpaces)
                if (Previous.TryGetParkingSpace(parkingSpace.Id, out var previousParkingSpace))
                    parkingSpace.ShareRuntimeOf(previousParkingSpace);

            StampChangedEffective(Previous);
            ShareRuntimeOfEffective();

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
                                       IEnumerable<ParkingSpace>?                            ParkingSpaces                           = null,
                                       IEnumerable<Brand>?                                   Brands                                  = null,
                                       IEnumerable<POIPublicKey>?                            SigningKeys                             = null,
                                       IEnumerable<POIPublicKey>?                            IdentityKeys                            = null,
                                       IEnumerable<EnergyMeterGroup>?                        EnergyMeterGroups                       = null,
                                       IEnumerable<EVSETemplate>?                            EVSETemplates                           = null,
                                       IEnumerable<ChargingStationTemplate>?                 ChargingStationTemplates                = null,
                                       IEnumerable<ChargingPoolTemplate>?                    ChargingPoolTemplates                   = null,
                                       IEnumerable<ChargingTariffTemplate>?                  ChargingTariffTemplates                 = null,
                                       IEnumerable<EnergyMeterTemplate>?                     EnergyMeterTemplates                    = null,
                                       IEnumerable<ChargingTariffOffer>?                     ChargingTariffOffers                    = null,

                                       Timestamped<ChargingStationOperatorAdminStatusType>?  InitialAdminStatus                     = null,
                                       Timestamped<ChargingStationOperatorStatusType>?       InitialStatus                          = null,
                                       UInt16?                                               MaxAdminStatusScheduleSize             = DefaultMaxAdminStatusScheduleSize,
                                       UInt16?                                               MaxStatusScheduleSize                  = DefaultMaxStatusScheduleSize,

                                       String?                                               DataSource                             = null,
                                       DateTimeOffset?                                       Created                                = null,
                                       DateTimeOffset?                                       LastChange                             = null,

                                       CustomDataNew?                                        CustomData                             = null,
                                       UserDefinedDictionary?                                InternalData                           = null)

            : this(true,
                   Id,
                   Name,
                   Description,
                   Logo,
                   Address,
                   GeoLocation,
                   Telephone,
                   EMailAddress,
                   Homepage,
                   HotlinePhoneNumber,
                   TermsAndConditionsURL,
                   DataLicenses,
                   ChargingPools,
                   ChargingTariffs,
                   EVSEGroups,
                   ChargingStationGroups,
                   ChargingPoolGroups,
                   ChargingTariffGroups,
                   ParkingSpaces,
                   Brands,
                   SigningKeys,
                   IdentityKeys,
                   EnergyMeterGroups,
                   EVSETemplates,
                   ChargingStationTemplates,
                   ChargingPoolTemplates,
                   ChargingTariffTemplates,
                   EnergyMeterTemplates,
                   ChargingTariffOffers,
                   InitialAdminStatus,
                   InitialStatus,
                   MaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        { }

        /// <summary>
        /// Create a new charging station operator, checking the members of its groups, the groups its charging tariff
        /// offers name and its offers not overlapping only when told so: within a change set they are checked after its
        /// last change (see InvalidGroupOrOffer()), as a charging station moved to another charging pool is no member of
        /// its operator between its removal and its addition.
        /// </summary>
        /// <param name="CheckGroupsAndOffers">Whether to check the members of its groups and its charging tariff offers.</param>
        private ChargingStationOperator(Boolean                                               CheckGroupsAndOffers,
                                        ChargingStationOperator_Id                            Id,
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
                                        IEnumerable<ParkingSpace>?                            ParkingSpaces                           = null,
                                        IEnumerable<Brand>?                                   Brands                                  = null,
                                        IEnumerable<POIPublicKey>?                            SigningKeys                             = null,
                                        IEnumerable<POIPublicKey>?                            IdentityKeys                            = null,
                                        IEnumerable<EnergyMeterGroup>?                        EnergyMeterGroups                       = null,
                                        IEnumerable<EVSETemplate>?                            EVSETemplates                           = null,
                                        IEnumerable<ChargingStationTemplate>?                 ChargingStationTemplates                = null,
                                        IEnumerable<ChargingPoolTemplate>?                    ChargingPoolTemplates                   = null,
                                        IEnumerable<ChargingTariffTemplate>?                  ChargingTariffTemplates                 = null,
                                        IEnumerable<EnergyMeterTemplate>?                     EnergyMeterTemplates                    = null,
                                        IEnumerable<ChargingTariffOffer>?                     ChargingTariffOffers                    = null,

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
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

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

            this.ChargingPools = new (chargingPools, chargingPool => chargingPool.Id, "charging pool", $"the operator '{Id}'");

            try
            {
                this.chargingStationIndex  = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.ChargingStations.Select(chargingStation => new KeyValuePair<ChargingStation_Id, ChargingStation>(chargingStation.Id, chargingStation)));
                this.evseIndex             = System.Collections.Immutable.ImmutableDictionary.CreateRange(this.EVSEs.           Select(evse            => new KeyValuePair<EVSE_Id,            EVSE>           (evse.Id,            evse)));
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"A charging station or EVSE of the operator '{Id}' must not be given twice!", nameof(ChargingPools));
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

            this.ChargingTariffs = new (chargingTariffs, chargingTariff => chargingTariff.Id, "charging tariff", $"the operator '{Id}'");

            foreach (var evse in EVSEs)
                foreach (var chargingTariffId in evse.ChargingTariffIds)
                    if (!this.ChargingTariffs.Contains(chargingTariffId))
                        throw new ArgumentException($"The charging tariff '{chargingTariffId}' of the EVSE '{evse.Id}' is unknown at the operator '{Id}'!", nameof(ChargingTariffs));

            #endregion

            #region Groups, of this operator (their members: see InvalidGroupOrOffer())

            {

                var evseGroups = (EVSEGroups ?? []).ToArray();

                if (evseGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(EVSEGroups));

                this.EVSEGroups = new (evseGroups, group => group.Id, "EVSE group", $"the operator '{Id}'");

                foreach (var evseGroup in this.EVSEGroups)
                    if (evseGroup.Id.OperatorId != Id)
                        throw new ArgumentException($"The group '{evseGroup.Id}' does not belong to the operator '{Id}'!", nameof(EVSEGroups));

            }

            {

                var chargingStationGroups = (ChargingStationGroups ?? []).ToArray();

                if (chargingStationGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(ChargingStationGroups));

                this.ChargingStationGroups = new (chargingStationGroups, group => group.Id, "charging station group", $"the operator '{Id}'");

                foreach (var chargingStationGroup in this.ChargingStationGroups)
                    if (chargingStationGroup.Id.OperatorId != Id)
                        throw new ArgumentException($"The group '{chargingStationGroup.Id}' does not belong to the operator '{Id}'!", nameof(ChargingStationGroups));

            }

            {

                var chargingPoolGroups = (ChargingPoolGroups ?? []).ToArray();

                if (chargingPoolGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(ChargingPoolGroups));

                this.ChargingPoolGroups = new (chargingPoolGroups, group => group.Id, "charging pool group", $"the operator '{Id}'");

                foreach (var chargingPoolGroup in this.ChargingPoolGroups)
                    if (chargingPoolGroup.Id.OperatorId != Id)
                        throw new ArgumentException($"The group '{chargingPoolGroup.Id}' does not belong to the operator '{Id}'!", nameof(ChargingPoolGroups));

            }

            {

                var chargingTariffGroups = (ChargingTariffGroups ?? []).ToArray();

                if (chargingTariffGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(ChargingTariffGroups));

                this.ChargingTariffGroups = new (chargingTariffGroups, group => group.Id, "charging tariff group", $"the operator '{Id}'");

                foreach (var chargingTariffGroup in this.ChargingTariffGroups)
                    if (chargingTariffGroup.Id.OperatorId != Id)
                        throw new ArgumentException($"The group '{chargingTariffGroup.Id}' does not belong to the operator '{Id}'!", nameof(ChargingTariffGroups));

            }

            {

                var energyMeterGroups = (EnergyMeterGroups ?? []).ToArray();

                if (energyMeterGroups.Any(group => group is null))
                    throw new ArgumentException("Groups must not contain null entries!", nameof(EnergyMeterGroups));

                this.EnergyMeterGroups = new (energyMeterGroups, group => group.Id, "energy meter group", $"the operator '{Id}'");

            }

            {

                var parkingSpaces = (ParkingSpaces ?? []).ToArray();

                if (parkingSpaces.Any(parkingSpace => parkingSpace is null))
                    throw new ArgumentException("Parking spaces must not contain null entries!", nameof(ParkingSpaces));

                this.ParkingSpaces = new (parkingSpaces, parkingSpace => parkingSpace.Id, "parking space", $"the operator '{Id}'");

                // Garages, sensors and products are those of parking operators.
                foreach (var parkingSpace in this.ParkingSpaces)
                    if (parkingSpace.ParkingGarageId.HasValue || parkingSpace.ParkingSensorIds.Length > 0 || parkingSpace.ParkingProductIds.Length > 0)
                        throw new ArgumentException($"The parking space '{parkingSpace.Id}' of the operator '{Id}' can be in no parking garage, and have no parking sensors or parking products: only those of a parking operator can!", nameof(ParkingSpaces));

            }

            this.Brands = new (Brands, brand => brand.Id, "brand", $"the operator '{Id}'");

            #endregion

            #region Templates, of this operator, and those its members name

            this.EVSETemplates             = new (EVSETemplates,            template => template.Id, "EVSE template",             $"the operator '{Id}'");
            this.ChargingStationTemplates  = new (ChargingStationTemplates, template => template.Id, "charging station template", $"the operator '{Id}'");
            this.ChargingPoolTemplates     = new (ChargingPoolTemplates,    template => template.Id, "charging pool template",    $"the operator '{Id}'");
            this.ChargingTariffTemplates   = new (ChargingTariffTemplates,  template => template.Id, "charging tariff template",  $"the operator '{Id}'");
            this.EnergyMeterTemplates      = new (EnergyMeterTemplates,     template => template.Id, "energy meter template",     $"the operator '{Id}'");

            foreach (var template in this.EVSETemplates)
            {

                if (template.Id.OperatorId != Id)
                    throw new ArgumentException($"The template '{template.Id}' does not belong to the operator '{Id}'!", nameof(EVSETemplates));

                if (template.ChargingTariffId.HasValue && !this.ChargingTariffs.Contains(template.ChargingTariffId.Value))
                    throw new ArgumentException($"The charging tariff '{template.ChargingTariffId}' of the template '{template.Id}' is unknown at the operator '{Id}'!", nameof(EVSETemplates));

            }

            foreach (var template in this.ChargingStationTemplates)
            {

                if (template.Id.OperatorId != Id)
                    throw new ArgumentException($"The template '{template.Id}' does not belong to the operator '{Id}'!", nameof(ChargingStationTemplates));

                if (template.ChargingTariffId.HasValue && !this.ChargingTariffs.Contains(template.ChargingTariffId.Value))
                    throw new ArgumentException($"The charging tariff '{template.ChargingTariffId}' of the template '{template.Id}' is unknown at the operator '{Id}'!", nameof(ChargingStationTemplates));

            }

            foreach (var template in this.ChargingPoolTemplates)
            {

                if (template.Id.OperatorId != Id)
                    throw new ArgumentException($"The template '{template.Id}' does not belong to the operator '{Id}'!", nameof(ChargingPoolTemplates));

                if (template.ChargingTariffId.HasValue && !this.ChargingTariffs.Contains(template.ChargingTariffId.Value))
                    throw new ArgumentException($"The charging tariff '{template.ChargingTariffId}' of the template '{template.Id}' is unknown at the operator '{Id}'!", nameof(ChargingPoolTemplates));

            }

            foreach (var template in this.ChargingTariffTemplates)
                if (template.Id.OperatorId != Id)
                    throw new ArgumentException($"The template '{template.Id}' does not belong to the operator '{Id}'!", nameof(ChargingTariffTemplates));

            // The templates its members name, of this operator.
            foreach (var evse in EVSEs)
                foreach (var templateId in evse.TemplateIds)
                    if (!this.EVSETemplates.Contains(templateId))
                        throw new ArgumentException($"The template '{templateId}' of the EVSE '{evse.Id}' is unknown at the operator '{Id}'!", nameof(EVSETemplates));

            foreach (var chargingStation in ChargingStations)
                foreach (var templateId in chargingStation.TemplateIds)
                    if (!this.ChargingStationTemplates.Contains(templateId))
                        throw new ArgumentException($"The template '{templateId}' of the charging station '{chargingStation.Id}' is unknown at the operator '{Id}'!", nameof(ChargingStationTemplates));

            foreach (var chargingPool in this.ChargingPools)
                foreach (var templateId in chargingPool.TemplateIds)
                    if (!this.ChargingPoolTemplates.Contains(templateId))
                        throw new ArgumentException($"The template '{templateId}' of the charging pool '{chargingPool.Id}' is unknown at the operator '{Id}'!", nameof(ChargingPoolTemplates));

            foreach (var chargingTariff in this.ChargingTariffs)
                foreach (var templateId in chargingTariff.TemplateIds)
                    if (!this.ChargingTariffTemplates.Contains(templateId))
                        throw new ArgumentException($"The template '{templateId}' of the charging tariff '{chargingTariff.Id}' is unknown at the operator '{Id}'!", nameof(ChargingTariffTemplates));

            foreach (var energyMeter in EnergyMeters)
                foreach (var templateId in energyMeter.TemplateIds)
                    if (!this.EnergyMeterTemplates.Contains(templateId))
                        throw new ArgumentException($"The template '{templateId}' of the energy meter '{energyMeter.Id}' is unknown at the operator '{Id}'!", nameof(EnergyMeterTemplates));

            #endregion

            #region Charging tariff offers: naming no operator but itself (their groups: see InvalidGroupOrOffer())

            this.ChargingTariffOffers = new (ChargingTariffOffers, offer => offer.Id, "charging tariff offer", $"the operator '{Id}'");

            foreach (var offer in this.ChargingTariffOffers)
                foreach (var operatorId in offer.ChargingStationOperatorIds)
                    if (operatorId != Id)
                        throw new ArgumentException($"The charging tariff offer '{offer.Id}' of the operator '{Id}' must not name the operator '{operatorId}'!", nameof(ChargingTariffOffers));

            #endregion

            #region Effective charging tariffs, charging pools, charging stations and EVSEs, with the properties their templates give them

            this.effectiveChargingTariffs = CalculateEffectiveChargingTariffs();
            (this.effectiveEVSEs, this.effectiveChargingStations, this.effectiveChargingPoolIndex) = CalculateEffective();
            this.effectiveChargingPools = new (CalculateEffectiveChargingPools);
            this.evseTimeZones          = new (CalculateEVSETimeZones);

            #endregion

            #region The members of its groups, the groups of its offers, and its offers not overlapping in the time zones of its EVSEs

            if (CheckGroupsAndOffers && InvalidGroupOrOffer() is String groupOrOfferError)
                throw new ArgumentException(groupOrOfferError, nameof(ChargingTariffOffers));

            #endregion

        }

        #endregion

        #region (internal) InvalidGroupOrOffer()

        /// <summary>
        /// The error, when a group of this operator names a member it does not have, a charging tariff offer of it names a
        /// group it does not have, or two of its offers overlap; null when there is none. A change set checks it after its
        /// last change, not after every change: a charging station moved to another charging pool is removed and added
        /// again, and is no member of this operator in between, though its groups still name it and its EVSEs.
        /// </summary>
        internal String? InvalidGroupOrOffer()
        {

            foreach (var evseGroup in EVSEGroups)
                foreach (var memberId in evseGroup.EVSEIds)
                    if (!evseIndex.ContainsKey(memberId))
                        return $"The member '{memberId}' of the group '{evseGroup.Id}' is unknown at the operator '{Id}'!";

            foreach (var chargingStationGroup in ChargingStationGroups)
                foreach (var memberId in chargingStationGroup.ChargingStationIds)
                    if (!chargingStationIndex.ContainsKey(memberId))
                        return $"The member '{memberId}' of the group '{chargingStationGroup.Id}' is unknown at the operator '{Id}'!";

            foreach (var chargingPoolGroup in ChargingPoolGroups)
                foreach (var memberId in chargingPoolGroup.ChargingPoolIds)
                    if (!ChargingPools.Contains(memberId))
                        return $"The member '{memberId}' of the group '{chargingPoolGroup.Id}' is unknown at the operator '{Id}'!";

            foreach (var chargingTariffGroup in ChargingTariffGroups)
                foreach (var memberId in chargingTariffGroup.ChargingTariffIds)
                    if (!ChargingTariffs.Contains(memberId))
                        return $"The member '{memberId}' of the group '{chargingTariffGroup.Id}' is unknown at the operator '{Id}'!";

            // The energy meters of its own charging pools, charging stations and EVSEs.
            foreach (var energyMeterGroup in EnergyMeterGroups)
                foreach (var memberId in energyMeterGroup.EnergyMeterIds)
                    if (!energyMeterIndex.ContainsKey(memberId))
                        return $"The member '{memberId}' of the group '{energyMeterGroup.Id}' is unknown at the operator '{Id}'!";

            foreach (var offer in ChargingTariffOffers)
            {

                foreach (var groupId in offer.EVSEGroupIds)
                    if (!EVSEGroups.Contains(groupId))
                        return $"The EVSE group '{groupId}' of the charging tariff offer '{offer.Id}' is unknown at the operator '{Id}'!";

                foreach (var groupId in offer.ChargingStationGroupIds)
                    if (!ChargingStationGroups.Contains(groupId))
                        return $"The charging station group '{groupId}' of the charging tariff offer '{offer.Id}' is unknown at the operator '{Id}'!";

                foreach (var groupId in offer.ChargingPoolGroupIds)
                    if (!ChargingPoolGroups.Contains(groupId))
                        return $"The charging pool group '{groupId}' of the charging tariff offer '{offer.Id}' is unknown at the operator '{Id}'!";

            }

            return OverlappingOffers();

        }

        #endregion


        #region  Data/(Admin-)Status management

        #region OnData/(Admin)StatusChanged

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

            => UpdateAdminStatus(Timestamp, EventTrackingId, OldStatus, NewStatus, DataSource);

        /// <inheritdoc/>
        protected override Task OnStatusScheduleChanged(DateTimeOffset                                  Timestamp,
                                                        EventTracking_Id                                EventTrackingId,
                                                        Timestamped<ChargingStationOperatorStatusType>  NewStatus,
                                                        Timestamped<ChargingStationOperatorStatusType>  OldStatus,
                                                        Context?                                        DataSource)

            => UpdateStatus(Timestamp, EventTrackingId, OldStatus, NewStatus, DataSource);

        #endregion


        #region (internal) UpdateAdminStatus(Timestamp, EventTrackingId, NewStatus, OldStatus, DataSource)

        /// <summary>
        /// Update the current admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="OldStatus">The old charging station admin status.</param>
        /// <param name="NewStatus">The new charging station admin status.</param>
        /// <param name="DataSource">The optional source of the status.</param>
        internal async Task UpdateAdminStatus(DateTimeOffset                                        Timestamp,
                                              EventTracking_Id                                      EventTrackingId,
                                              Timestamped<ChargingStationOperatorAdminStatusType>  OldStatus,
                                              Timestamped<ChargingStationOperatorAdminStatusType>  NewStatus,
                                              Context?                                              DataSource   = null)
        {

            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp,
                                           EventTrackingId,
                                           this,
                                           OldStatus,
                                           NewStatus,
                                           DataSource);

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
        /// <param name="DataSource">The optional source of the status.</param>
        internal async Task UpdateStatus(DateTimeOffset                                   Timestamp,
                                         EventTracking_Id                                 EventTrackingId,
                                         Timestamped<ChargingStationOperatorStatusType>  OldStatus,
                                         Timestamped<ChargingStationOperatorStatusType>  NewStatus,
                                         Context?                                         DataSource   = null)
        {

            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp,
                                      EventTrackingId,
                                      this,
                                      OldStatus,
                                      NewStatus,
                                      DataSource);

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
                              CustomJObjectSerializerDelegate<ChargingStationOperator>?  CustomChargingStationOperatorSerializer   = null)
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


                               // created, lastChange and customData: see below
                               null

                               );

                // Embedded completely, as content.
                if (Brands.Count > 0)
                    json.Add("brands", new JArray(Brands.Select(brand => brand.ToJSON())));

                if (ChargingTariffs.Count > 0)
                    json.Add("chargingTariffs", new JArray(ChargingTariffs.Select(chargingTariff => chargingTariff.ToJSON())));

                if (ChargingPools.Count > 0)
                    json.Add("chargingPools", new JArray(ChargingPools.Select(chargingPool => chargingPool.ToJSON())));

                if (EVSETemplates.Count > 0)
                    json.Add("EVSETemplates", new JArray(EVSETemplates.Select(template => template.ToJSON())));

                if (ChargingStationTemplates.Count > 0)
                    json.Add("chargingStationTemplates", new JArray(ChargingStationTemplates.Select(template => template.ToJSON())));

                if (ChargingPoolTemplates.Count > 0)
                    json.Add("chargingPoolTemplates", new JArray(ChargingPoolTemplates.Select(template => template.ToJSON())));

                if (ChargingTariffTemplates.Count > 0)
                    json.Add("chargingTariffTemplates", new JArray(ChargingTariffTemplates.Select(template => template.ToJSON())));

                if (EnergyMeterTemplates.Count > 0)
                    json.Add("energyMeterTemplates", new JArray(EnergyMeterTemplates.Select(template => template.ToJSON())));

                if (EVSEGroups.Count > 0)
                    json.Add("EVSEGroups", new JArray(EVSEGroups.Select(group => group.ToJSON())));

                if (ChargingStationGroups.Count > 0)
                    json.Add("chargingStationGroups", new JArray(ChargingStationGroups.Select(group => group.ToJSON())));

                if (ChargingPoolGroups.Count > 0)
                    json.Add("chargingPoolGroups", new JArray(ChargingPoolGroups.Select(group => group.ToJSON())));

                if (ChargingTariffGroups.Count > 0)
                    json.Add("chargingTariffGroups", new JArray(ChargingTariffGroups.Select(group => group.ToJSON())));

                if (EnergyMeterGroups.Count > 0)
                    json.Add("energyMeterGroups", new JArray(EnergyMeterGroups.Select(group => group.ToJSON())));

                if (ChargingTariffOffers.Count > 0)
                    json.Add("chargingTariffOffers", new JArray(ChargingTariffOffers.Select(offer => offer.ToJSON())));

                if (ParkingSpaces.Count > 0)
                    json.Add("parkingSpaces", new JArray(ParkingSpaces.Select(parkingSpace => parkingSpace.ToJSON())));

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
