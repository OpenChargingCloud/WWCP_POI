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
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The roaming networks and what they embed over HTTP, as JSON or as CBOR,
    /// each with the ETag of its representation, readable from every origin.
    /// </summary>
    /// <remarks>
    /// GET ~/RNs                                                                  the roaming networks, in short
    /// GET ~/RNs/{RN_Id}                                                          the roaming network
    /// GET ~/RNs/{RN_Id}/CSOs                                                     its charging station operators, in short
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}                                            a charging station operator;
    ///     ?without=chargingPools leaves out its charging pools, which ~/RNs/{RN_Id}/CPs?operatorId=… lists;
    ///     ?without=chargingTariffs,chargingTariffGroups,chargingPoolGroups,chargingStationGroups,EVSEGroups,
    ///     or any of them, leaves them out, which ~/RNs/{RN_Id}/CSOs/{CSO_Id}/{CTs|CTGs|CPGs|CSGs|EVSEGs} list
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/Brands                                     the brands of the charging station operator, its own
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/Brands/{Brand_Id}                          a brand of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs                                        its charging tariffs, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs/{CT_Id}                                a charging tariff of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTGs                                       its charging tariff groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTGs/{CTG_Id}                              a charging tariff group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPGs                                       its charging pool groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPGs/{CPG_Id}                              a charging pool group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CSGs                                       its charging station groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CSGs/{CSG_Id}                              a charging station group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EVSEGs                                     its EVSE groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EVSEGs/{EVSEG_Id}                          an EVSE group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}                                a charging pool;
    ///     ?expand=gridConnectionPoint embeds its grid connection point instead of naming it
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/Brands                         the brands of the charging pool, its own
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/Brands/{Brand_Id}              a brand of the charging pool
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}                    a charging station
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/PSs                the parking spaces it names, of parking
    ///                                                                             operators or of its operator, as they embed them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/PSs/{PS_Id}        a parking space the charging station names
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/Brands             the brands of the charging station, its own
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/Brands/{Brand_Id}  a brand of the charging station
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}    an EVSE
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/Brands
    ///                                                                            the brands of the EVSE, its own
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/Brands/{Brand_Id}
    ///                                                                            a brand of the EVSE
    /// GET ~/RNs/{RN_Id}/GOs                                                      its grid operators, in short
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}                                              a grid operator;
    ///     ?without=gridConnectionPoints leaves out its grid connection points, which ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs lists
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs                                         its grid connection points, as it embeds them
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}                                a grid connection point of the grid operator
    /// GET ~/RNs/{RN_Id}/EMPs                                                     its e-mobility providers, in short
    /// GET ~/RNs/{RN_Id}/EMPs/{EMP_Id}                                            an e-mobility provider
    /// GET ~/RNs/{RN_Id}/CSMs                                                     its charging station manufacturers, in short
    /// GET ~/RNs/{RN_Id}/CSMs/{CSM_Id}                                            a charging station manufacturer
    /// GET ~/RNs/{RN_Id}/POs                                                      its parking operators, in short
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}                                              a parking operator;
    ///     ?without=parkingGarages,parkingSpaces,parkingSpaceGroups,parkingSensors,parkingProducts, or any of them,
    ///     leaves them out, which ~/RNs/{RN_Id}/POs/{PO_Id}/{PGs|PSs|PSGs|PSNs|PPs} list
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PGs                                          its parking garages, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PGs/{PG_Id}                                  a parking garage of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSs                                          its parking spaces, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSs/{PS_Id}                                  a parking space of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSGs                                         its parking space groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSGs/{PSG_Id}                                a parking space group of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSNs                                         its parking sensors, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSNs/{PSN_Id}                                a parking sensor of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PPs                                          its parking products, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PPs/{PP_Id}                                  a parking product of the parking operator
    ///
    /// Brands are embedded in what they are a brand of, and mean something of their own on every level: a charging
    /// station operator is a white-label solution for its brands, a charging pool, charging station or EVSE also
    /// appears under its brands. The levels are not mixed: the brands of a pool are not those of its operator.
    ///
    /// The roaming network is the head of its history, when served with one, else the one added.
    /// A list is an array of { "@id", "name", "description" } in the order of the identifications,
    /// with the ETag of the list.
    /// An entity is the representation its roaming network embeds, so its ETag is the one of that
    /// part of the roaming network: SHA-256 over its canonical JSON or canonical CBOR, which is also
    /// the content of the response. An expanded or a slimmer view is another representation with its own ETag. The roaming network carries its own ETags, calculated without them.
    /// The HTTP ETag is the POI ETag of the format of the response, e.g. "json:sha256:hex:…";
    /// an If-None-Match naming it is answered with 304 Not Modified.
    ///
    /// Versions are immutable, and their runtime status is no part of their representations:
    /// so the content and the ETag of every representation are calculated once per version and format,
    /// and kept as long as the version is. An entity unchanged by a commit is the same object in the
    /// new version, and keeps its representations, a slimmer view as well. A list is kept with its collection, a POICollection
    /// of the version; the list of the roaming networks while the same versions are served. Only an
    /// expanded view, which depends on more than one version, is calculated for every request.
    /// HEAD is answered by Hermod from GET; OPTIONS answers the CORS preflights of every origin.
    /// With an HTML view, a browser opening any of these URLs gets it instead, e.g. the page of a web frontend.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The methods of the roaming networks and of the entities they embed.
        /// </summary>
        private static readonly HTTPMethod[] EntityMethods = [ HTTPMethod.OPTIONS, HTTPMethod.GET, HTTPMethod.HEAD ];

        /// <summary>
        /// The request header fields a foreign origin may send: If-None-Match needs a preflight.
        /// </summary>
        private static readonly String[]     EntityRequestHeaders = [ "Accept", "If-None-Match" ];

        /// <summary>
        /// The view of a collection as a list of its children in short.
        /// </summary>
        private const           String       SummariesView        = "summaries";

        /// <summary>
        /// The view of a collection as a list of its children as their parent embeds them.
        /// </summary>
        private const           String       EmbeddedView         = "embedded";

        /// <summary>
        /// The representations of versions of roaming networks and of the entities they embed,
        /// as long as the versions are referenced elsewhere. Lists keep theirs in their collections.
        /// </summary>
        private readonly ConditionalWeakTable<Object, Representations> representationCache = new ();

        /// <summary>
        /// The roaming networks served, with the representations of their list, while the same versions are served.
        /// </summary>
        private volatile POICollection<RoamingNetwork_Id, RoamingNetwork>? roamingNetworksServed;

        #endregion

        #region (private) Representations

        /// <summary>
        /// The representations of one immutable version of an entity, by view and format, each calculated once.
        /// </summary>
        private sealed class Representations
        {

            private readonly ConcurrentDictionary<(String View, ETagFormat Format), Lazy<POIRepresentation>> representations = new ();

            /// <summary>
            /// The representation of the given view and format, calculated by the given delegate the first time only.
            /// </summary>
            public POIRepresentation GetOrAdd(String                   View,
                                              ETagFormat               Format,
                                              Func<POIRepresentation>  Create)

                => representations.GetOrAdd((View, Format), _ => new Lazy<POIRepresentation>(Create)).Value;

        }

        #endregion


        #region (private) RegisterEntityURLTemplates()

        private void RegisterEntityURLTemplates()
        {

            var roamingNetworkPath           = HTTPPath.Root + "RNs/{roamingNetworkId}";
            var chargingStationOperatorPath  = roamingNetworkPath          + "/CSOs/{chargingStationOperatorId}";
            var chargingPoolPath             = chargingStationOperatorPath + "/CPs/{chargingPoolId}";
            var chargingStationPath          = chargingPoolPath            + "/CSs/{chargingStationId}";
            var gridOperatorPath             = roamingNetworkPath          + "/GOs/{gridOperatorId}";
            var parkingOperatorPath          = roamingNetworkPath          + "/POs/{parkingOperatorId}";

            #region ~/RNs

            AddEntityHandlers(HTTPPath.Root + "RNs",
                              RoamingNetworksResponse);

            #endregion

            #region ~/RNs/{RN_Id}

            AddEntityHandlers(roamingNetworkPath,
                              request => TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse)
                                             ? RoamingNetworkResponse(request, roamingNetwork)
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs, GOs, EMPs, CSMs, POs

            AddListHandlers(roamingNetworkPath, "CSOs",  roamingNetwork => roamingNetwork.ChargingStationOperators,      EntitySummaries);
            AddListHandlers(roamingNetworkPath, "GOs",   roamingNetwork => roamingNetwork.GridOperators,                 EntitySummaries);
            AddListHandlers(roamingNetworkPath, "EMPs",  roamingNetwork => roamingNetwork.EMobilityProviders,            EntitySummaries);
            AddListHandlers(roamingNetworkPath, "CSMs",  roamingNetwork => roamingNetwork.ChargingStationManufacturers,  EntitySummaries);
            AddListHandlers(roamingNetworkPath, "POs",   roamingNetwork => roamingNetwork.ParkingOperators,              EntitySummaries);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}

            // Its charging pools are what makes an operator large: thousands of them, each with its
            // stations and EVSEs. Without them, the operator is what it says of itself.
            AddEntityHandlers(chargingStationOperatorPath,
                              request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                                             ? CachedEntityResponseWithout(request,
                                                                           chargingStationOperator,
                                                                           chargingStationOperator.ToEmbeddedJSON,
                                                                           [ "chargingPools", "chargingTariffs", "chargingTariffGroups", "chargingPoolGroups", "chargingStationGroups", "EVSEGroups" ],
                                                                           "a charging station operator")
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs, CTGs, CPGs, CSGs, EVSEGs, and each of them below

            // Its charging tariffs and its groups: a list each, and each of them on its own.
            AddEmbeddedListHandlers<ChargingTariff_Id, ChargingTariff>(
                chargingStationOperatorPath + "/CTs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffs, null)
                               : (null, errorResponse),
                chargingTariff => chargingTariff.ToJSON());

            AddEmbeddedEntityHandlers<ChargingTariff_Id, ChargingTariff>(
                chargingStationOperatorPath + "/CTs/{chargingTariffId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffs, null)
                               : (null, errorResponse),
                ChargingTariff_Id.TryParse,
                "charging tariff",
                chargingTariff => chargingTariff.ToJSON());

            AddEmbeddedListHandlers<ChargingTariffGroup_Id, ChargingTariffGroup>(
                chargingStationOperatorPath + "/CTGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffGroups, null)
                               : (null, errorResponse),
                chargingTariffGroup => chargingTariffGroup.ToJSON());

            AddEmbeddedEntityHandlers<ChargingTariffGroup_Id, ChargingTariffGroup>(
                chargingStationOperatorPath + "/CTGs/{chargingTariffGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffGroups, null)
                               : (null, errorResponse),
                ChargingTariffGroup_Id.TryParse,
                "charging tariff group",
                chargingTariffGroup => chargingTariffGroup.ToJSON());

            AddEmbeddedListHandlers<ChargingPoolGroup_Id, ChargingPoolGroup>(
                chargingStationOperatorPath + "/CPGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingPoolGroups, null)
                               : (null, errorResponse),
                chargingPoolGroup => chargingPoolGroup.ToJSON());

            AddEmbeddedEntityHandlers<ChargingPoolGroup_Id, ChargingPoolGroup>(
                chargingStationOperatorPath + "/CPGs/{chargingPoolGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingPoolGroups, null)
                               : (null, errorResponse),
                ChargingPoolGroup_Id.TryParse,
                "charging pool group",
                chargingPoolGroup => chargingPoolGroup.ToJSON());

            AddEmbeddedListHandlers<ChargingStationGroup_Id, ChargingStationGroup>(
                chargingStationOperatorPath + "/CSGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingStationGroups, null)
                               : (null, errorResponse),
                chargingStationGroup => chargingStationGroup.ToJSON());

            AddEmbeddedEntityHandlers<ChargingStationGroup_Id, ChargingStationGroup>(
                chargingStationOperatorPath + "/CSGs/{chargingStationGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingStationGroups, null)
                               : (null, errorResponse),
                ChargingStationGroup_Id.TryParse,
                "charging station group",
                chargingStationGroup => chargingStationGroup.ToJSON());

            AddEmbeddedListHandlers<EVSEGroup_Id, EVSEGroup>(
                chargingStationOperatorPath + "/EVSEGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EVSEGroups, null)
                               : (null, errorResponse),
                eVSEGroup => eVSEGroup.ToJSON());

            AddEmbeddedEntityHandlers<EVSEGroup_Id, EVSEGroup>(
                chargingStationOperatorPath + "/EVSEGs/{EVSEGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EVSEGroups, null)
                               : (null, errorResponse),
                EVSEGroup_Id.TryParse,
                "EVSE group",
                eVSEGroup => eVSEGroup.ToJSON());

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}

            AddEntityHandlers(chargingPoolPath,
                              request => {

                                  if (!TryGetRequestedChargingPool  (request, out var chargingPool,   out var errorResponse) ||
                                      !TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out errorResponse))
                                  {
                                      return errorResponse;
                                  }

                                  if (request.QueryString.GetString("expand") is not String expand)
                                      return CachedEntityResponse(request, chargingPool, chargingPool.ToJSON);

                                  // An expanded view depends on the version of the pool and on the one of its grid connection point.
                                  var json = chargingPool.ToJSON();

                                  foreach (var expansion in expand.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                      if (expansion != "gridConnectionPoint")
                                          return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Unknown expansion '{expansion}'; a charging pool expands only 'gridConnectionPoint'!");

                                  // Embedded with its grid operator, in the place of its identification.
                                  if (chargingPool.GridConnectionPointId is GridConnectionPoint_Id gridConnectionPointId &&
                                      roamingNetwork.TryGetGridConnectionPoint(gridConnectionPointId, out var gridOperator, out var gridConnectionPoint) &&
                                      json.Property("gridConnectionPointId") is JProperty idProperty)
                                  {

                                      var embedded = new JObject(
                                                         new JProperty("@id",             gridConnectionPoint.Id.ToString()),
                                                         new JProperty("gridOperatorId",  gridOperator.Id.ToString())
                                                     );

                                      foreach (var property in gridConnectionPoint.ToJSON().Properties().Where(property => property.Name != "@id"))
                                          embedded.Add(new JProperty(property.Name, property.Value.DeepClone()));

                                      idProperty.Replace(new JProperty("gridConnectionPoint", embedded));

                                  }

                                  return EntityResponse(request, json);

                              });

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}

            AddEntityHandlers(chargingStationPath,
                              request => TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse)
                                             ? CachedEntityResponse(request, chargingStation, chargingStation.ToJSON)
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/PSs, and each of them below

            // The parking spaces a charging station charges at: named by it, defined by parking operators
            // or by its charging station operator.
            AddEmbeddedListHandlers<ParkingSpace_Id, ParkingSpace>(
                chargingStationPath + "/PSs",
                RequestedParkingSpacesOfChargingStation,
                parkingSpace => parkingSpace.ToJSON());

            AddEmbeddedEntityHandlers<ParkingSpace_Id, ParkingSpace>(
                chargingStationPath + "/PSs/{parkingSpaceId}",
                RequestedParkingSpacesOfChargingStation,
                ParkingSpace_Id.TryParse,
                "parking space",
                parkingSpace => parkingSpace.ToJSON());

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}

            AddEntityHandlers(chargingStationPath + "/EVSEs/{evseId}",
                              request => TryGetRequestedEVSE(request, out var evse, out var errorResponse)
                                             ? CachedEntityResponse(request, evse, evse.ToJSON)
                                             : errorResponse);

            #endregion

            #region The brands of a charging station operator, pool, station and EVSE: ~/…/Brands, and each of them below

            // Every level its own brands: the white labels of an operator, the brands a pool, station or EVSE also appears under.
            AddEmbeddedListHandlers<Brand_Id, Brand>(
                chargingStationOperatorPath + "/Brands",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.Brands, null)
                               : (null, errorResponse),
                brand => brand.ToJSON());

            AddEmbeddedEntityHandlers<Brand_Id, Brand>(
                chargingStationOperatorPath + "/Brands/{brandId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.Brands, null)
                               : (null, errorResponse),
                Brand_Id.TryParse,
                "brand of the charging station operator",
                brand => brand.ToJSON());

            AddEmbeddedListHandlers<Brand_Id, Brand>(
                chargingPoolPath + "/Brands",
                request => TryGetRequestedChargingPool(request, out var chargingPool, out var errorResponse)
                               ? (chargingPool.Brands, null)
                               : (null, errorResponse),
                brand => brand.ToJSON());

            AddEmbeddedEntityHandlers<Brand_Id, Brand>(
                chargingPoolPath + "/Brands/{brandId}",
                request => TryGetRequestedChargingPool(request, out var chargingPool, out var errorResponse)
                               ? (chargingPool.Brands, null)
                               : (null, errorResponse),
                Brand_Id.TryParse,
                "brand of the charging pool",
                brand => brand.ToJSON());

            AddEmbeddedListHandlers<Brand_Id, Brand>(
                chargingStationPath + "/Brands",
                request => TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse)
                               ? (chargingStation.Brands, null)
                               : (null, errorResponse),
                brand => brand.ToJSON());

            AddEmbeddedEntityHandlers<Brand_Id, Brand>(
                chargingStationPath + "/Brands/{brandId}",
                request => TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse)
                               ? (chargingStation.Brands, null)
                               : (null, errorResponse),
                Brand_Id.TryParse,
                "brand of the charging station",
                brand => brand.ToJSON());

            AddEmbeddedListHandlers<Brand_Id, Brand>(
                chargingStationPath + "/EVSEs/{evseId}" + "/Brands",
                request => TryGetRequestedEVSE(request, out var evse, out var errorResponse)
                               ? (evse.Brands, null)
                               : (null, errorResponse),
                brand => brand.ToJSON());

            AddEmbeddedEntityHandlers<Brand_Id, Brand>(
                chargingStationPath + "/EVSEs/{evseId}" + "/Brands/{brandId}",
                request => TryGetRequestedEVSE(request, out var evse, out var errorResponse)
                               ? (evse.Brands, null)
                               : (null, errorResponse),
                Brand_Id.TryParse,
                "brand of the EVSE",
                brand => brand.ToJSON());

            #endregion

            #region ~/RNs/{RN_Id}/GOs/{GO_Id}

            AddEntityHandlers(gridOperatorPath,
                              request => TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse)
                                             ? CachedEntityResponseWithout(request,
                                                                           gridOperator,
                                                                           gridOperator.ToJSON,
                                                                           [ "gridConnectionPoints" ],
                                                                           "a grid operator")
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs

            AddEmbeddedListHandlers<GridConnectionPoint_Id, GridConnectionPoint>(
                gridOperatorPath + "/GCPs",
                request => TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse)
                               ? (gridOperator.GridConnectionPoints, null)
                               : (null, errorResponse),
                gridConnectionPoint => gridConnectionPoint.ToJSON());

            #endregion

            #region ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}

            AddEntityHandlers(gridOperatorPath + "/GCPs/{gridConnectionPointId}",
                              request => {

                                  if (!TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse))
                                      return errorResponse;

                                  if (!GridConnectionPoint_Id.TryParse(request.ParsedURLParameters[2], out var gridConnectionPointId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid grid connection point identification!");

                                  return gridOperator.TryGetGridConnectionPoint(gridConnectionPointId, out var gridConnectionPoint)
                                             ? CachedEntityResponse(request, gridConnectionPoint, gridConnectionPoint.ToJSON)
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The grid connection point '{gridConnectionPointId}' of the grid operator '{gridOperator.Id}' is unknown!");

                              });

            #endregion

            #region ~/RNs/{RN_Id}/EMPs/{EMP_Id}

            AddEntityHandlers(roamingNetworkPath + "/EMPs/{eMobilityProviderId}",
                              request => {

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  if (!EMobilityProvider_Id.TryParse(request.ParsedURLParameters[1], out var eMobilityProviderId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid e-mobility provider identification!");

                                  return roamingNetwork.TryGetEMobilityProviderById(eMobilityProviderId, out var eMobilityProvider)
                                             ? CachedEntityResponse(request, eMobilityProvider, eMobilityProvider.ToJSON)
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The e-mobility provider '{eMobilityProviderId}' is unknown in the roaming network '{roamingNetwork.Id}'!");

                              });

            #endregion

            #region ~/RNs/{RN_Id}/CSMs/{CSM_Id}

            AddEntityHandlers(roamingNetworkPath + "/CSMs/{chargingStationManufacturerId}",
                              request => {

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  if (!ChargingStationManufacturer_Id.TryParse(request.ParsedURLParameters[1], out var chargingStationManufacturerId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid charging station manufacturer identification!");

                                  return roamingNetwork.TryGetChargingStationManufacturerById(chargingStationManufacturerId, out var chargingStationManufacturer)
                                             ? CachedEntityResponse(request, chargingStationManufacturer, chargingStationManufacturer.ToJSON)
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The charging station manufacturer '{chargingStationManufacturerId}' is unknown in the roaming network '{roamingNetwork.Id}'!");

                              });

            #endregion

            #region ~/RNs/{RN_Id}/POs/{PO_Id}

            // Its parking spaces and their sensors are what makes a parking operator large: one each for every
            // space of every garage. Without them, or without anything else it embeds, a list of their own each.
            AddEntityHandlers(parkingOperatorPath,
                              request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                                             ? CachedEntityResponseWithout(request,
                                                                           parkingOperator,
                                                                           parkingOperator.ToJSON,
                                                                           [ "parkingGarages", "parkingSpaces", "parkingSpaceGroups", "parkingSensors", "parkingProducts" ],
                                                                           "a parking operator")
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/POs/{PO_Id}/PGs, PSs, PSGs, PSNs, PPs

            AddEmbeddedListHandlers<ParkingGarage_Id, ParkingGarage>(
                parkingOperatorPath + "/PGs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingGarages, null)
                               : (null, errorResponse),
                parkingGarage => parkingGarage.ToJSON());

            AddEmbeddedListHandlers<ParkingSpace_Id, ParkingSpace>(
                parkingOperatorPath + "/PSs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSpaces, null)
                               : (null, errorResponse),
                parkingSpace => parkingSpace.ToJSON());

            AddEmbeddedListHandlers<ParkingSpaceGroup_Id, ParkingSpaceGroup>(
                parkingOperatorPath + "/PSGs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSpaceGroups, null)
                               : (null, errorResponse),
                parkingSpaceGroup => parkingSpaceGroup.ToJSON());

            AddEmbeddedListHandlers<ParkingSensor_Id, ParkingSensor>(
                parkingOperatorPath + "/PSNs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSensors, null)
                               : (null, errorResponse),
                parkingSensor => parkingSensor.ToJSON());

            AddEmbeddedListHandlers<ParkingProduct_Id, ParkingProduct>(
                parkingOperatorPath + "/PPs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingProducts, null)
                               : (null, errorResponse),
                parkingProduct => parkingProduct.ToJSON());

            #endregion

            #region ~/RNs/{RN_Id}/POs/{PO_Id}/PGs/{PG_Id}, PSs/{PS_Id}, PSGs/{PSG_Id}, PSNs/{PSN_Id}, PPs/{PP_Id}

            AddEmbeddedEntityHandlers<ParkingGarage_Id, ParkingGarage>(
                parkingOperatorPath + "/PGs/{parkingGarageId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingGarages, null)
                               : (null, errorResponse),
                ParkingGarage_Id.TryParse,
                "parking garage",
                parkingGarage => parkingGarage.ToJSON());

            AddEmbeddedEntityHandlers<ParkingSpace_Id, ParkingSpace>(
                parkingOperatorPath + "/PSs/{parkingSpaceId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSpaces, null)
                               : (null, errorResponse),
                ParkingSpace_Id.TryParse,
                "parking space",
                parkingSpace => parkingSpace.ToJSON());

            AddEmbeddedEntityHandlers<ParkingSpaceGroup_Id, ParkingSpaceGroup>(
                parkingOperatorPath + "/PSGs/{parkingSpaceGroupId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSpaceGroups, null)
                               : (null, errorResponse),
                ParkingSpaceGroup_Id.TryParse,
                "parking space group",
                parkingSpaceGroup => parkingSpaceGroup.ToJSON());

            AddEmbeddedEntityHandlers<ParkingSensor_Id, ParkingSensor>(
                parkingOperatorPath + "/PSNs/{parkingSensorId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSensors, null)
                               : (null, errorResponse),
                ParkingSensor_Id.TryParse,
                "parking sensor",
                parkingSensor => parkingSensor.ToJSON());

            AddEmbeddedEntityHandlers<ParkingProduct_Id, ParkingProduct>(
                parkingOperatorPath + "/PPs/{parkingProductId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingProducts, null)
                               : (null, errorResponse),
                ParkingProduct_Id.TryParse,
                "parking product",
                parkingProduct => parkingProduct.ToJSON());

            #endregion

        }

        #endregion

        #region (private) AddEntityHandlers(Path, GET)

        /// <summary>
        /// Answer GET with the given delegate, HEAD by Hermod from it, and OPTIONS with the methods
        /// and the CORS permissions of every origin. With an HTML view, a request preferring text/html
        /// to JSON and CBOR, e.g. of a browser opening the URL, gets the HTML view instead: of a list
        /// as of a single entity.
        /// </summary>
        private void AddEntityHandlers(HTTPPath                         Path,
                                       Func<HTTPRequest, HTTPResponse>  GET)
        {

            AddHandler(Path,
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => HTMLView is not null && WantsHTML(request)
                                                     ? HTMLView(request)
                                                     : Task.FromResult(GET(request)));

            AddHandler(Path,
                       HTTPMethod:    HTTPMethod.OPTIONS,
                       HTTPDelegate:  request => Task.FromResult(
                                          new HTTPResponse.Builder(request) {
                                              HTTPStatusCode             = HTTPStatusCode.NoContent,
                                              Allow                      = EntityMethods,
                                              AccessControlAllowOrigin   = "*",
                                              AccessControlAllowMethods  = EntityMethods,
                                              AccessControlAllowHeaders  = EntityRequestHeaders,
                                              AccessControlMaxAge        = 86400
                                          }.AsImmutable
                                      ));

        }

        #endregion


        #region (private) AddListHandlers(RoamingNetworkPath, Name, Collection, Summaries)

        /// <summary>
        /// Answer GET of ~/RNs/{RN_Id}/{Name} with the list of the given collection of the roaming network named
        /// in the path, in short, searched and paged by the query of the request; the whole list kept with the
        /// collection. HEAD by Hermod from it, and OPTIONS.
        /// </summary>
        private void AddListHandlers<TId, TEntity>(HTTPPath                                               RoamingNetworkPath,
                                                   String                                                 Name,
                                                   Func<RoamingNetwork, POICollection<TId, TEntity>>      Collection,
                                                   Func<IEnumerable<TEntity>, JArray>                     Summaries)

            where TId     : notnull
            where TEntity : class

            => AddEntityHandlers(RoamingNetworkPath + "/" + Name,
                                 request => {

                                     if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse) ||
                                         !TryParseListQuery(request, [], out var query, out errorResponse))
                                     {
                                         return errorResponse;
                                     }

                                     var collection = Collection(roamingNetwork);

                                     return QueryResponse<JObject>(request,
                                                                   query,
                                                                   () => [.. Summaries(collection).Cast<JObject>()],
                                                                   SearchText,
                                                                   entries => new JArray(entries),
                                                                   format => collection.Representation(SummariesView, format, () => Summaries(collection)),
                                                                   collection.Count);

                                 });

        #endregion


        #region (private) AddEmbeddedListHandlers(Path, Collection, ToJSON)

        /// <summary>
        /// Answer GET of the given path with the children of an entity as it embeds them, searched and paged
        /// by the query of the request; the whole list kept with their collection. HEAD by Hermod from it,
        /// and OPTIONS.
        /// </summary>
        /// <param name="Path">The path of the list, below the one of the entity.</param>
        /// <param name="Collection">The children of the entity the path names, or why there are none.</param>
        /// <param name="ToJSON">A child, as the entity embeds it.</param>
        private void AddEmbeddedListHandlers<TId, TEntity>(HTTPPath                                                                          Path,
                                                           Func<HTTPRequest, (POICollection<TId, TEntity>? Collection, HTTPResponse? ErrorResponse)>  Collection,
                                                           Func<TEntity, JObject>                                                            ToJSON)

            where TId     : notnull
            where TEntity : class

            => AddEntityHandlers(Path,
                                 request => {

                                     var (collection, errorResponse) = Collection(request);

                                     if (collection is null)
                                         return errorResponse!;

                                     if (!TryParseListQuery(request, [], out var query, out errorResponse))
                                         return errorResponse;

                                     return QueryResponse<TEntity>(request,
                                                                   query,
                                                                   () => collection,
                                                                   child => SearchTextOf(child, () => ToJSON(child)),
                                                                   entries => new JArray(entries.Select(ToJSON)),
                                                                   format => collection.Representation(EmbeddedView, format, () => new JArray(collection.Select(ToJSON))),
                                                                   collection.Count);

                                 });

        #endregion


        #region (private) AddEmbeddedEntityHandlers(Path, Collection, TryParseId, Description, ToJSON)

        /// <summary>
        /// Parses the identification of an entity from the text of its path.
        /// </summary>
        private delegate Boolean TryParseIdDelegate<TId>(String Text, out TId Id);

        /// <summary>
        /// Answer GET of the given path with one child of an entity, the last parameter of the path, as the
        /// entity embeds it: kept with its version, as every entity is. HEAD by Hermod from it, and OPTIONS.
        /// </summary>
        /// <param name="Path">The path of the child, below the one of the list of the children.</param>
        /// <param name="Collection">The children of the entity the path names, or why there are none.</param>
        /// <param name="TryParseId">Parses the identification of a child.</param>
        /// <param name="Description">What a child is, e.g. "parking space", for a refusal.</param>
        /// <param name="ToJSON">A child, as the entity embeds it.</param>
        private void AddEmbeddedEntityHandlers<TId, TEntity>(HTTPPath                                                                          Path,
                                                             Func<HTTPRequest, (POICollection<TId, TEntity>? Collection, HTTPResponse? ErrorResponse)>  Collection,
                                                             TryParseIdDelegate<TId>                                                           TryParseId,
                                                             String                                                                            Description,
                                                             Func<TEntity, JObject>                                                            ToJSON)

            where TId     : notnull
            where TEntity : class

            => AddEntityHandlers(Path,
                                 request => {

                                     var (collection, errorResponse) = Collection(request);

                                     if (collection is null)
                                         return errorResponse!;

                                     var text = request.ParsedURLParameters[^1];

                                     if (!TryParseId(text, out var id))
                                         return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid {Description} identification '{text}'!");

                                     return collection.TryGet(id, out var child)
                                                ? CachedEntityResponse(request, child, () => ToJSON(child))
                                                : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The {Description} '{id}' is unknown here!");

                                 });

        #endregion


        #region RoamingNetworks

        /// <summary>
        /// The roaming networks served: the heads of the histories, and the ones added without a history.
        /// </summary>
        public IEnumerable<RoamingNetwork> RoamingNetworks
            => RoamingNetworksServed();

        #endregion

        #region (private) RoamingNetworksServed()

        /// <summary>
        /// The roaming networks served: the heads of the histories, and the ones added without a history.
        /// </summary>
        private IEnumerable<RoamingNetwork> RoamingNetworksServed()

            => roamingNetworkHistories.Values.
                   Select(history => history.Head.RoamingNetwork).
                   Concat(roamingNetworks.Values.Where(roamingNetwork => !roamingNetworkHistories.ContainsKey(roamingNetwork.Id)));

        #endregion

        #region (private) TryGetRequestedParkingOperator        (Request, out ParkingOperator,                      out ErrorResponse)

        private Boolean TryGetRequestedParkingOperator(HTTPRequest                                Request,
                                                       [NotNullWhen(true)]  out ParkingOperator?  ParkingOperator,
                                                       [NotNullWhen(false)] out HTTPResponse?     ErrorResponse)
        {

            ParkingOperator = null;

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out ErrorResponse))
                return false;

            if (!ParkingOperator_Id.TryParse(Request.ParsedURLParameters[1], out var parkingOperatorId))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid parking operator identification!");
                return false;
            }

            if (!roamingNetwork.TryGetParkingOperatorById(parkingOperatorId, out ParkingOperator))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The parking operator '{parkingOperatorId}' is unknown in the roaming network '{roamingNetwork.Id}'!");
                return false;
            }

            return true;

        }

        #endregion

        #region (private) TryGetRequestedRoamingNetwork         (Request, out RoamingNetwork,                       out ErrorResponse)

        /// <summary>
        /// The roaming network named in the path of the request: the head of its history, else the one added.
        /// </summary>
        private Boolean TryGetRequestedRoamingNetwork(HTTPRequest                               Request,
                                                      [NotNullWhen(true)]  out RoamingNetwork?  RoamingNetwork,
                                                      [NotNullWhen(false)] out HTTPResponse?    ErrorResponse)
        {

            RoamingNetwork = null;

            if (Request.ParsedURLParameters.Length < 1 ||
                !RoamingNetwork_Id.TryParse(Request.ParsedURLParameters[0], out var roamingNetworkId))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid roaming network identification!");
                return false;
            }

            RoamingNetwork = roamingNetworkHistories.TryGetValue(roamingNetworkId, out var history)
                                 ? history.Head.RoamingNetwork
                                 : GetRoamingNetwork(roamingNetworkId);

            if (RoamingNetwork is null)
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The roaming network '{roamingNetworkId}' is unknown!");
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private) TryGetRequestedChargingStationOperator(Request, out ChargingStationOperator,              out ErrorResponse)

        private Boolean TryGetRequestedChargingStationOperator(HTTPRequest                                        Request,
                                                               [NotNullWhen(true)]  out ChargingStationOperator?  ChargingStationOperator,
                                                               [NotNullWhen(false)] out HTTPResponse?             ErrorResponse)
        {

            ChargingStationOperator = null;

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out ErrorResponse))
                return false;

            if (!ChargingStationOperator_Id.TryParse(Request.ParsedURLParameters[1], out var chargingStationOperatorId))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid charging station operator identification!");
                return false;
            }

            if (!roamingNetwork.TryGetChargingStationOperatorById(chargingStationOperatorId, out ChargingStationOperator))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The charging station operator '{chargingStationOperatorId}' is unknown in the roaming network '{roamingNetwork.Id}'!");
                return false;
            }

            return true;

        }

        #endregion

        #region (private) TryGetRequestedChargingPool           (Request, out ChargingPool,                         out ErrorResponse)

        private Boolean TryGetRequestedChargingPool(HTTPRequest                             Request,
                                                    [NotNullWhen(true)]  out ChargingPool?  ChargingPool,
                                                    [NotNullWhen(false)] out HTTPResponse?  ErrorResponse)
        {

            ChargingPool = null;

            if (!TryGetRequestedChargingStationOperator(Request, out var chargingStationOperator, out ErrorResponse))
                return false;

            if (!ChargingPool_Id.TryParse(Request.ParsedURLParameters[2], out var chargingPoolId))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid charging pool identification!");
                return false;
            }

            if (!chargingStationOperator.TryGetChargingPool(chargingPoolId, out ChargingPool))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The charging pool '{chargingPoolId}' is unknown at the charging station operator '{chargingStationOperator.Id}'!");
                return false;
            }

            return true;

        }

        #endregion

        #region (private) TryGetRequestedEVSE                   (Request, out EVSE,                                 out ErrorResponse)

        private Boolean TryGetRequestedEVSE(HTTPRequest                             Request,
                                            [NotNullWhen(true)]  out EVSE?          EVSE,
                                            [NotNullWhen(false)] out HTTPResponse?  ErrorResponse)
        {

            EVSE = null;

            if (!TryGetRequestedChargingStation(Request, out var chargingStation, out ErrorResponse))
                return false;

            if (!EVSE_Id.TryParse(Request.ParsedURLParameters[4], out var evseId))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid EVSE identification!");
                return false;
            }

            if (!chargingStation.TryGetEVSE(evseId, out EVSE))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The EVSE '{evseId}' is unknown at the charging station '{chargingStation.Id}'!");
                return false;
            }

            return true;

        }

        #endregion

        #region (private) TryGetRequestedChargingStation        (Request, out ChargingStation,                      out ErrorResponse)

        private Boolean TryGetRequestedChargingStation(HTTPRequest                                Request,
                                                       [NotNullWhen(true)]  out ChargingStation?  ChargingStation,
                                                       [NotNullWhen(false)] out HTTPResponse?     ErrorResponse)
        {

            ChargingStation = null;

            if (!TryGetRequestedChargingPool(Request, out var chargingPool, out ErrorResponse))
                return false;

            if (!ChargingStation_Id.TryParse(Request.ParsedURLParameters[3], out var chargingStationId))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid charging station identification!");
                return false;
            }

            if (!chargingPool.TryGetChargingStation(chargingStationId, out ChargingStation))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The charging station '{chargingStationId}' is unknown at the charging pool '{chargingPool.Id}'!");
                return false;
            }

            return true;

        }

        #endregion

        #region (private) RequestedParkingSpacesOfChargingStation(Request)

        /// <summary>
        /// The parking spaces the requested charging station names, of parking operators or of its charging station operator,
        /// in the order of their identifications; or why there are none.
        /// </summary>
        private (POICollection<ParkingSpace_Id, ParkingSpace>? Collection, HTTPResponse? ErrorResponse) RequestedParkingSpacesOfChargingStation(HTTPRequest Request)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse) ||
                !TryGetRequestedChargingStation(Request, out var chargingStation, out errorResponse))
            {
                return (null, errorResponse);
            }

            return (new POICollection<ParkingSpace_Id, ParkingSpace>(
                        chargingStation.ParkingSpaceIds.
                                        Select(parkingSpaceId => roamingNetwork.TryGetParkingSpaceById(parkingSpaceId, out var parkingSpace) ? parkingSpace : null).
                                        OfType<ParkingSpace>(),
                        parkingSpace => parkingSpace.Id,
                        "parking space",
                        $"the charging station '{chargingStation.Id}'"
                    ),
                    null);

        }

        #endregion

        #region (private) TryGetRequestedGridOperator           (Request, out RoamingNetwork, out GridOperator,     out ErrorResponse)

        private Boolean TryGetRequestedGridOperator(HTTPRequest                               Request,
                                                    [NotNullWhen(true)]  out RoamingNetwork?  RoamingNetwork,
                                                    [NotNullWhen(true)]  out GridOperator?    GridOperator,
                                                    [NotNullWhen(false)] out HTTPResponse?    ErrorResponse)
        {

            GridOperator = null;

            if (!TryGetRequestedRoamingNetwork(Request, out RoamingNetwork, out ErrorResponse))
                return false;

            if (!GridOperator_Id.TryParse(Request.ParsedURLParameters[1], out var gridOperatorId))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid grid operator identification!");
                return false;
            }

            if (!RoamingNetwork.TryGetGridOperatorById(gridOperatorId, out GridOperator))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The grid operator '{gridOperatorId}' is unknown in the roaming network '{RoamingNetwork.Id}'!");
                return false;
            }

            return true;

        }

        #endregion


        #region (private) RoamingNetworksResponse(Request)

        /// <summary>
        /// The list of the roaming networks served, kept while the same versions are served.
        /// </summary>
        private HTTPResponse RoamingNetworksResponse(HTTPRequest Request)
        {

            if (!TryParseListQuery(Request, [], out var query, out var errorResponse))
                return errorResponse;

            var versions  = RoamingNetworksServed().
                                OrderBy(roamingNetwork => roamingNetwork.Id.ToString(), StringComparer.Ordinal).
                                ToArray();

            var served    = roamingNetworksServed;

            // Roaming networks equal by their identifications: the versions must be the very same.
            if (served is null || !Enumerable.SequenceEqual<RoamingNetwork>(served, versions, ReferenceEqualityComparer.Instance))
                roamingNetworksServed = served = new (versions, roamingNetwork => roamingNetwork.Id, "roaming network");

            return QueryResponse<JObject>(Request,
                                          query,
                                          () => [.. EntitySummaries(served).Cast<JObject>()],
                                          SearchText,
                                          entries => new JArray(entries),
                                          format => served.Representation(SummariesView, format, () => EntitySummaries(served)),
                                          served.Count);

        }

        #endregion

        #region (private) RoamingNetworkResponse(Request, RoamingNetwork)

        /// <summary>
        /// The roaming network with its ETags, in JSON or in CBOR as the request prefers.
        /// The HTTP ETag is its ETag of that format, calculated without ETags.
        /// Kept with its version, once its ETags are calculated: they are part of its representation.
        /// </summary>
        private HTTPResponse RoamingNetworkResponse(HTTPRequest     Request,
                                                    RoamingNetwork  RoamingNetwork)
        {

            var format      = FormatOf(Request);
            var cbor        = format == ETagFormat.CBOR;
            var eTags       = RoamingNetwork.ETags.IsDefault ? [] : RoamingNetwork.ETags;
            var stored      = eTags.Where(eTag => eTag.Format == format).ToArray();

            if (stored.Length > 0)
                return EntityResponse(Request,
                                      format,
                                      representationCache.GetValue(RoamingNetwork, _ => new Representations()).
                                                          GetOrAdd($"roamingNetwork:{eTags.Length}",
                                                                   format,
                                                                   () => new POIRepresentation(cbor
                                                                                                   ? RoamingNetwork.ToCBOR().ToByteArray(CBORWriterOptions.Canonical)
                                                                                                   : CanonicalJSON.ToUTF8Bytes(RoamingNetwork.ToJSON()),
                                                                                               stored[0])));

            // Without its ETags, e.g. one added without a history.
            return EntityResponse(Request,
                                  format,
                                  new POIRepresentation(cbor
                                                            ? RoamingNetwork.ToCBOR().ToByteArray(CBORWriterOptions.Canonical)
                                                            : CanonicalJSON.ToUTF8Bytes(RoamingNetwork.ToJSON()),
                                                        ETag.Compute(format,
                                                                     cbor
                                                                         ? RoamingNetwork.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical)
                                                                         : CanonicalJSON.ToUTF8Bytes(RoamingNetwork.ToJSON(IncludeETags: false)))));

        }

        #endregion

        #region (private static) EntitySummaries       (Entities)

        /// <summary>
        /// A list of the given entities: { "@id", "name", "description" } in the order of their identifications,
        /// without an empty name or description.
        /// </summary>
        private static JArray EntitySummaries<TId, TAdminStatus, TStatus>(IEnumerable<AImmutableEMobilityEntity<TId, TAdminStatus, TStatus>> Entities)

            where TId          : IId
            where TAdminStatus : IComparable
            where TStatus      : IComparable

            => new (Entities.
                        OrderBy(entity => entity.Id.ToString(), StringComparer.Ordinal).
                        Select (entity => {

                            var json = new JObject(
                                           new JProperty("@id", entity.Id.ToString())
                                       );

                            if (entity.Name.IsNotNullOrEmpty())
                                json.Add("name",         entity.Name.ToJSON());

                            if (entity.Description.IsNotNullOrEmpty())
                                json.Add("description",  entity.Description.ToJSON());

                            return json;

                        }));

        #endregion

        #region (private static) WantsHTML             (Request)

        /// <summary>
        /// Whether the request prefers text/html to JSON and CBOR by its Accept header field, e.g. a browser
        /// opening the URL. Not without an Accept header field, and not for */*: then JSON, as for every client.
        /// </summary>
        private static Boolean WantsHTML(HTTPRequest Request)

            => Request.Accept.BestMatchingContentType(HTTPContentType.Application.JSON_UTF8,
                                                      HTTPContentType.Application.CBOR,
                                                      HTTPContentType.Text.HTML_UTF8) == HTTPContentType.Text.HTML_UTF8;

        #endregion

        #region (private static) FormatOf              (Request)

        /// <summary>
        /// CBOR when the request prefers it to JSON by its Accept header field, else JSON.
        /// </summary>
        private static ETagFormat FormatOf(HTTPRequest Request)

            => WantsCBOR(Request)
                   ? ETagFormat.CBOR
                   : ETagFormat.JSON;

        #endregion

        #region (private)        CachedEntityResponse  (Request, Version, JSON, View = "")

        /// <summary>
        /// The given immutable version of an entity in JSON or in CBOR as the request prefers,
        /// calculated once per version, view and format.
        /// </summary>
        /// <param name="View">The view of the entity, e.g. without its charging pools; its whole representation by default.</param>
        private HTTPResponse CachedEntityResponse(HTTPRequest   Request,
                                                  Object        Version,
                                                  Func<JToken>  JSON,
                                                  String        View   = "")
        {

            var format = FormatOf(Request);

            return EntityResponse(Request,
                                  format,
                                  representationCache.GetValue(Version, _ => new Representations()).
                                                      GetOrAdd(View, format, () => POIRepresentation.Of(JSON(), format)));

        }

        #endregion

        #region (private)        CachedEntityResponseWithout(Request, Version, JSON, Omittables, Description)

        /// <summary>
        /// The given immutable version of an entity as CachedEntityResponse(…) answers it, or, with
        /// ?without=…, without the parts named, separated by commas or each in a parameter of its own:
        /// another view, with its own ETag, kept as well, whatever the order they are named in. Anything
        /// else to leave out is refused.
        /// </summary>
        /// <param name="Omittables">The properties the entity may leave out, e.g. its children with a list of their own.</param>
        /// <param name="Description">What the entity is, e.g. "a grid operator", for a refusal.</param>
        private HTTPResponse CachedEntityResponseWithout(HTTPRequest    Request,
                                                         Object         Version,
                                                         Func<JObject>  JSON,
                                                         String[]       Omittables,
                                                         String         Description)
        {

            // Hermod splits a value at its commas: GetString(…) would be the last part only.
            var omissions = Request.QueryString.GetStrings("without").
                                    SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).
                                    Distinct().
                                    Order(StringComparer.Ordinal).
                                    ToArray();

            foreach (var omission in omissions)
                if (!Omittables.Contains(omission))
                    return EntityErrorResponse(Request,
                                               HTTPStatusCode.BadRequest,
                                               $"Unknown omission '{omission}'; {Description} leaves out only {String.Join(", ", Omittables.Select(omittable => $"'{omittable}'"))}!");

            if (omissions.Length == 0)
                return CachedEntityResponse(Request, Version, JSON);

            return CachedEntityResponse(Request,
                                        Version,
                                        () => {
                                            var json = JSON();
                                            foreach (var omission in omissions)
                                                json.Remove(omission);
                                            return json;
                                        },
                                        View: "without=" + String.Join(",", omissions));

        }

        #endregion

        #region (private static) EntityResponse        (Request, JSON)

        /// <summary>
        /// The given entity in canonical JSON, or in canonical CBOR, calculated for this request.
        /// </summary>
        private static HTTPResponse EntityResponse(HTTPRequest  Request,
                                                   JToken       JSON)
        {

            var format = FormatOf(Request);

            return EntityResponse(Request,
                                  format,
                                  POIRepresentation.Of(JSON, format));

        }

        #endregion

        #region (private static) EntityResponse        (Request, Format, Representation)

        /// <summary>
        /// 200 OK with the given representation and its ETag, or 304 Not Modified when the If-None-Match
        /// of the request names the ETag. Caches revalidate it; every origin may read it and its ETag.
        /// </summary>
        private static HTTPResponse EntityResponse(HTTPRequest                    Request,
                                                   ETagFormat                     Format,
                                                   POIRepresentation              Representation,
                                                   Action<HTTPResponse.Builder>?  Headers   = null)
        {

            var eTag         = $"\"{Representation.ETag.ToString(ETagDigestEncoding.HEX)}\"";
            var notModified  = ContentNegotiation.IfNoneMatchMatches(Request.IfNoneMatch, eTag);

            var response     = new HTTPResponse.Builder(Request) {
                                   HTTPStatusCode            = notModified
                                                                   ? HTTPStatusCode.NotModified
                                                                   : HTTPStatusCode.OK,
                                   ETag                      = eTag,
                                   CacheControl              = "no-cache",
                                   Vary                      = "Accept",
                                   AccessControlAllowOrigin  = "*"
                               };

            // A 304 carries no content, and no metadata of it beyond the ETag, Cache-Control and Vary.
            if (!notModified)
            {
                response.ContentType  = Format == ETagFormat.CBOR
                                            ? HTTPContentType.Application.CBOR
                                            : HTTPContentType.Application.JSON_UTF8;
                response.Content      = Representation.Content;
            }

            Headers?.Invoke(response);

            // Not CORS-safelisted response headers: scripts of other origins see them only when exposed.
            response.Set("Access-Control-Expose-Headers", Headers is null
                                                              ? "ETag"
                                                              : "ETag, X-Total-Count, X-Filtered-Count, Link");

            return response.AsImmutable;

        }

        #endregion

        #region (private static) EntityErrorResponse   (Request, StatusCode, Description)

        /// <summary>
        /// An error in JSON, or in CBOR when the request prefers it: { "description": "…" },
        /// readable from every origin.
        /// </summary>
        private static HTTPResponse EntityErrorResponse(HTTPRequest     Request,
                                                        HTTPStatusCode  StatusCode,
                                                        String          Description)
        {

            var json  = new JObject(new JProperty("description", Description));
            var cbor  = WantsCBOR(Request);

            return new HTTPResponse.Builder(Request) {
                       HTTPStatusCode            = StatusCode,
                       ContentType               = cbor
                                                       ? HTTPContentType.Application.CBOR
                                                       : HTTPContentType.Application.JSON_UTF8,
                       Content                   = cbor
                                                       ? CBORJSON.ToCBOR(json).ToByteArray(CBORWriterOptions.Canonical)
                                                       : Encoding.UTF8.GetBytes(json.ToString(Formatting.None)),
                       CacheControl              = "no-store",
                       Vary                      = "Accept",
                       AccessControlAllowOrigin  = "*"
                   }.AsImmutable;

        }

        #endregion

    }

}
