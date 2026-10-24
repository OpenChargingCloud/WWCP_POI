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
using System.Globalization;
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
    /// GET ~/RNs/{RN_Id}                                                          the roaming network;
    ///                                                                             ?without=chargingStationOperators,… without what
    ///                                                                             it holds, each of them with a list of its own
    /// GET ~/RNs/{RN_Id}/CSOs                                                     its charging station operators, in short
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}                                            a charging station operator;
    ///     ?without=chargingPools leaves out its charging pools, which ~/RNs/{RN_Id}/CPs?operatorId=… lists;
    ///     ?without=chargingTariffs,EVSETemplates,chargingStationTemplates,chargingPoolTemplates,chargingTariffTemplates,
    ///     energyMeterTemplates,chargingTariffGroups,chargingPoolGroups,chargingStationGroups,EVSEGroups,energyMeterGroups,
    ///     chargingTariffOffers, or any of them, leaves them out, which
    ///     ~/RNs/{RN_Id}/CSOs/{CSO_Id}/{CTs|EVSETs|CSTs|CPTs|CTTs|EMTs|CTGs|CPGs|CSGs|EVSEGs|EMGs|CTOs} list;
    ///     ?without=parkingSpaces leaves out its own parking spaces, which ~/…/CSs/{CS_Id}/PSs list as its stations name them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/Brands                                     the brands of the charging station operator, its own
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/Brands/{Brand_Id}                          a brand of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs                                        its charging tariffs, the effective ones;
    ///                                                                             ?view=stored as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs/{CT_Id}                                a charging tariff of the charging station operator,
    ///                                                                             the effective one; ?view=stored as it embeds it
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs/{CT_Id}/CTTs                           the templates the charging tariff names, in its order
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/{EVSETs|CSTs|CPTs|CTTs|EMTs}               its EVSE, charging station, charging pool, charging
    ///                                                                             tariff and energy meter templates, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/{EVSETs|CSTs|CPTs|CTTs|EMTs}/{Id}          a template of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTOs                                       its charging tariff offers, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTOs/{CTO_Id}                              a charging tariff offer of the charging station operator
    /// GET ~/…/CPs/{CP_Id}/CTOs, ~/…/CSs/{CS_Id}/CTOs, ~/…/EVSEs/{EVSE_Id}/CTOs     the charging tariff offers covering one of its EVSEs,
    ///                                                                             of its operator and of e-mobility providers;
    ///                                                                             ?status=active, ?eMobilityProviderId=… filter them;
    ///                                                                             ?applicable=true, also &at=…, those that apply
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTGs                                       its charging tariff groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTGs/{CTG_Id}                              a charging tariff group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPGs                                       its charging pool groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPGs/{CPG_Id}                              a charging pool group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CSGs                                       its charging station groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CSGs/{CSG_Id}                              a charging station group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EVSEGs                                     its EVSE groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EVSEGs/{EVSEG_Id}                          an EVSE group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EMGs                                       its energy meter groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EMGs/{EMG_Id}                              an energy meter group of the charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}                                a charging pool, the effective one;
    ///     ?expand=gridConnectionPoint embeds its grid connection point instead of naming it,
    ///     ?view=stored serves it, and everything below, as its operator embeds it
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CPTs                           the templates the charging pool names, in its order
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/Brands                         the brands of the charging pool, its own
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/Brands/{Brand_Id}              a brand of the charging pool
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}                    a charging station
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/PSs                the parking spaces it names, of parking
    ///                                                                             operators or of its operator, as they embed them
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/PSs/{PS_Id}        a parking space the charging station names
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/CSTs               the templates the charging station names, in its order
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/Brands             the brands of the charging station, its own
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/Brands/{Brand_Id}  a brand of the charging station
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}    an EVSE
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/EVSETs
    ///                                                                            the templates the EVSE names, in its order
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/Brands
    ///                                                                            the brands of the EVSE, its own
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/Brands/{Brand_Id}
    ///                                                                            a brand of the EVSE
    /// GET ~/RNs/{RN_Id}/GOs                                                      its grid operators, in short
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}                                              a grid operator;
    ///     ?without=gridConnectionPoints,energyMeterTemplates,energyMeterGroups, or any of them, leaves them out, which
    ///     ~/RNs/{RN_Id}/GOs/{GO_Id}/{GCPs|EMTs|EMGs} list
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs                                         its grid connection points, the effective ones;
    ///     ?view=stored as it embeds them
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}                                a grid connection point of the grid operator, the
    ///                                                                             effective one; ?view=stored as it embeds it
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/EMTs                                         its energy meter templates, as it embeds them
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/EMTs/{EMT_Id}                                an energy meter template of the grid operator
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/EMGs                                         its energy meter groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/EMGs/{EMG_Id}                                an energy meter group of the grid operator
    /// GET ~/RNs/{RN_Id}/EMPs                                                     its e-mobility providers, in short
    /// GET ~/RNs/{RN_Id}/EMPs/{EMP_Id}                                            an e-mobility provider;
    ///     ?without=EVSEGroups,chargingStationGroups,chargingPoolGroups,chargingTariffOffers, or any of them, leaves them
    ///     out, which ~/RNs/{RN_Id}/EMPs/{EMP_Id}/{EVSEGs|CSGs|CPGs|CTOs} list, each of them below it, e.g. …/CTOs/{CTO_Id}
    /// GET ~/RNs/{RN_Id}/CSMs                                                     its charging station manufacturers, in short
    /// GET ~/RNs/{RN_Id}/CSMs/{CSM_Id}                                            a charging station manufacturer
    /// GET ~/RNs/{RN_Id}/EMMs                                                     its energy meter manufacturers, in short
    /// GET ~/RNs/{RN_Id}/EMMs/{EMM_Id}                                            an energy meter manufacturer
    /// GET ~/RNs/{RN_Id}/TSMs                                                     its transparency software manufacturers, in short
    /// GET ~/RNs/{RN_Id}/TSMs/{TSM_Id}                                            a transparency software manufacturer, with its releases;
    ///     ?without=transparencySoftware leaves them out, which ~/RNs/{RN_Id}/TSMs/{TSM_Id}/TSs lists
    /// GET ~/RNs/{RN_Id}/TSMs/{TSM_Id}/TSs/{TS_Id}                                a transparency software release of the manufacturer
    /// GET ~/RNs/{RN_Id}/CABs                                                     its conformity assessment bodies, in short
    /// GET ~/RNs/{RN_Id}/CABs/{CAB_Id}                                            a conformity assessment body, with its certificates;
    ///     ?without=transparencySoftwareCertificates leaves them out, which ~/RNs/{RN_Id}/CABs/{CAB_Id}/TSCs lists
    /// GET ~/RNs/{RN_Id}/CABs/{CAB_Id}/TSCs/{TSC_Id}                              a transparency software certificate of the body
    /// GET ~/RNs/{RN_Id}/POs                                                      its parking operators, in short
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}                                              a parking operator;
    ///     ?without=parkingGarages,parkingSpaces,parkingSpaceTemplates,parkingSpaceGroups,parkingSensors,parkingProducts,
    ///     or any of them, leaves them out, which ~/RNs/{RN_Id}/POs/{PO_Id}/{PGs|PSs|PSTs|PSGs|PSNs|PPs} list
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PGs                                          its parking garages, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PGs/{PG_Id}                                  a parking garage of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSs                                          its parking spaces, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSs/{PS_Id}                                  a parking space of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSs/{PS_Id}/PSTs                             the templates the parking space names, in its order
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSTs                                         its parking space templates, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSTs/{PST_Id}                                a parking space template of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSGs                                         its parking space groups, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSGs/{PSG_Id}                                a parking space group of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSNs                                         its parking sensors, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSNs/{PSN_Id}                                a parking sensor of the parking operator
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PPs                                          its parking products, as it embeds them
    /// GET ~/RNs/{RN_Id}/POs/{PO_Id}/PPs/{PP_Id}                                  a parking product of the parking operator
    ///
    /// A charging pool, a charging station, an EVSE, a charging tariff and a parking space are served as they are at
    /// runtime, the effective ones of their operator: with the properties their templates give them, see
    /// ChargingStationOperator.EffectiveChargingPools, EffectiveChargingTariffs and ParkingOperator.EffectiveParkingSpaces;
    /// a charging pool also in ~/RNs/{RN_Id}/CPs, searched so. The energy meters they embed take the properties of their
    /// energy meter templates, and so does the one of a grid connection point, see GridOperator.EffectiveGridConnectionPoints.
    /// Their operators and the roaming network embed them as they are given, naming their templates. ?view=stored serves a
    /// charging pool, and what it embeds, a charging tariff, a parking space or a grid connection point as its operator
    /// embeds it, with the ETag a precondition of a change set names, also in the links
    /// to other pages of a list; in either view, X-Stored-ETag tells that ETag as well.
    ///
    /// Brands are embedded in what they are a brand of, and mean something of their own on every level: a charging
    /// station operator is a white-label solution for its brands, a charging pool, charging station or EVSE also
    /// appears under its brands. The levels are not mixed: the brands of a pool are not those of its operator.
    ///
    /// The roaming network is the head of its history, when served with one, else the one added. Without what it
    /// holds, it is what it says of itself, with its ETags, the ones of all of it.
    /// A list is an array of { "@id", "name", "description" } in the natural order of the identifications, P1, P2,
    /// P10, with the ETag of the list; so is a list of what an entity embeds, which embeds them in their ordinal order.
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
        /// What a roaming network may leave out: what it holds, each of them with a list of its own.
        /// </summary>
        private static readonly String[]     RoamingNetworkOmittables = [ "chargingStationManufacturers", "chargingStationOperators", "eMobilityProviders", "energyMeterManufacturers", "gridOperators", "parkingOperators" ];

        /// <summary>
        /// The view of a collection as a list of its children as their parent embeds them.
        /// </summary>
        private const           String       EmbeddedView         = "embedded";

        /// <summary>
        /// The view of a charging pool, and of what it embeds, as its operator embeds it: its charging stations
        /// without the properties their groups give them.
        /// </summary>
        private const           String       StoredView           = "stored";

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
            var eMobilityProviderPath        = roamingNetworkPath          + "/EMPs/{eMobilityProviderId}";

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

            #region ~/RNs/{RN_Id}/CSOs, GOs, EMPs, CSMs, EMMs, TSMs, CABs, POs

            AddListHandlers(roamingNetworkPath, "CSOs",  roamingNetwork => roamingNetwork.ChargingStationOperators,      EntitySummaries);
            AddListHandlers(roamingNetworkPath, "GOs",   roamingNetwork => roamingNetwork.GridOperators,                 EntitySummaries);
            AddListHandlers(roamingNetworkPath, "EMPs",  roamingNetwork => roamingNetwork.EMobilityProviders,            EntitySummaries);
            AddListHandlers(roamingNetworkPath, "CSMs",  roamingNetwork => roamingNetwork.ChargingStationManufacturers,  EntitySummaries);
            AddListHandlers(roamingNetworkPath, "EMMs",  roamingNetwork => roamingNetwork.EnergyMeterManufacturers,      EntitySummaries);
            AddListHandlers(roamingNetworkPath, "TSMs",  roamingNetwork => roamingNetwork.TransparencySoftwareManufacturers, EntitySummaries);
            AddListHandlers(roamingNetworkPath, "CABs",  roamingNetwork => roamingNetwork.ConformityAssessmentBodies,    EntitySummaries);
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
                                                                           [ "chargingPools", "chargingTariffs", "EVSETemplates", "chargingStationTemplates", "chargingPoolTemplates", "chargingTariffTemplates", "energyMeterTemplates",
                                                                             "chargingTariffGroups", "chargingPoolGroups", "chargingStationGroups", "EVSEGroups", "energyMeterGroups", "chargingTariffOffers", "parkingSpaces" ],
                                                                           "a charging station operator")
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs, EVSETs, CSTs, CPTs, CTTs, EMTs, CTGs, CPGs, CSGs, EVSEGs, EMGs, and each of them below

            // Its charging tariffs, its templates and its groups: a list each, and each of them on its own. The charging
            // tariffs as they are at runtime, with the properties their templates give them; with ?view=stored as it embeds them.
            AddEmbeddedListHandlers<ChargingTariff_Id, ChargingTariff>(
                chargingStationOperatorPath + "/CTs",
                RequestedChargingTariffsOfOperator,
                chargingTariff => chargingTariff.ToJSON(Embedded: true),
                Viewed: true);

            AddEmbeddedEntityHandlers<ChargingTariff_Id, ChargingTariff>(
                chargingStationOperatorPath + "/CTs/{chargingTariffId}",
                RequestedChargingTariffsOfOperator,
                ChargingTariff_Id.TryParse,
                "charging tariff",
                chargingTariff => chargingTariff.ToJSON(Embedded: true),
                (request, chargingTariff) => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out _) &&
                                             chargingStationOperator.TryGetChargingTariff(chargingTariff.Id, out var stored)
                                                 ? stored
                                                 : null);

            AddEmbeddedListHandlers<EVSETemplate_Id, EVSETemplate>(
                chargingStationOperatorPath + "/EVSETs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EVSETemplates, null)
                               : (null, errorResponse),
                evseTemplate => evseTemplate.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<EVSETemplate_Id, EVSETemplate>(
                chargingStationOperatorPath + "/EVSETs/{EVSETemplateId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EVSETemplates, null)
                               : (null, errorResponse),
                EVSETemplate_Id.TryParse,
                "EVSE template",
                evseTemplate => evseTemplate.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingStationTemplate_Id, ChargingStationTemplate>(
                chargingStationOperatorPath + "/CSTs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingStationTemplates, null)
                               : (null, errorResponse),
                chargingStationTemplate => chargingStationTemplate.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingStationTemplate_Id, ChargingStationTemplate>(
                chargingStationOperatorPath + "/CSTs/{chargingStationTemplateId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingStationTemplates, null)
                               : (null, errorResponse),
                ChargingStationTemplate_Id.TryParse,
                "charging station template",
                chargingStationTemplate => chargingStationTemplate.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingPoolTemplate_Id, ChargingPoolTemplate>(
                chargingStationOperatorPath + "/CPTs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingPoolTemplates, null)
                               : (null, errorResponse),
                chargingPoolTemplate => chargingPoolTemplate.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingPoolTemplate_Id, ChargingPoolTemplate>(
                chargingStationOperatorPath + "/CPTs/{chargingPoolTemplateId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingPoolTemplates, null)
                               : (null, errorResponse),
                ChargingPoolTemplate_Id.TryParse,
                "charging pool template",
                chargingPoolTemplate => chargingPoolTemplate.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingTariffTemplate_Id, ChargingTariffTemplate>(
                chargingStationOperatorPath + "/CTTs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffTemplates, null)
                               : (null, errorResponse),
                chargingTariffTemplate => chargingTariffTemplate.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingTariffTemplate_Id, ChargingTariffTemplate>(
                chargingStationOperatorPath + "/CTTs/{chargingTariffTemplateId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffTemplates, null)
                               : (null, errorResponse),
                ChargingTariffTemplate_Id.TryParse,
                "charging tariff template",
                chargingTariffTemplate => chargingTariffTemplate.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<EnergyMeterTemplate_Id, EnergyMeterTemplate>(
                chargingStationOperatorPath + "/EMTs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EnergyMeterTemplates, null)
                               : (null, errorResponse),
                energyMeterTemplate => energyMeterTemplate.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<EnergyMeterTemplate_Id, EnergyMeterTemplate>(
                chargingStationOperatorPath + "/EMTs/{energyMeterTemplateId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EnergyMeterTemplates, null)
                               : (null, errorResponse),
                EnergyMeterTemplate_Id.TryParse,
                "energy meter template",
                energyMeterTemplate => energyMeterTemplate.ToJSON(Embedded: true));


            AddEmbeddedListHandlers<ChargingTariffOffer_Id, ChargingTariffOffer>(
                chargingStationOperatorPath + "/CTOs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffOffers, null)
                               : (null, errorResponse),
                chargingTariffOffer => chargingTariffOffer.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingTariffOffer_Id, ChargingTariffOffer>(
                chargingStationOperatorPath + "/CTOs/{chargingTariffOfferId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffOffers, null)
                               : (null, errorResponse),
                ChargingTariffOffer_Id.TryParse,
                "charging tariff offer",
                chargingTariffOffer => chargingTariffOffer.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingTariffGroup_Id, ChargingTariffGroup>(
                chargingStationOperatorPath + "/CTGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffGroups, null)
                               : (null, errorResponse),
                chargingTariffGroup => chargingTariffGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingTariffGroup_Id, ChargingTariffGroup>(
                chargingStationOperatorPath + "/CTGs/{chargingTariffGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingTariffGroups, null)
                               : (null, errorResponse),
                ChargingTariffGroup_Id.TryParse,
                "charging tariff group",
                chargingTariffGroup => chargingTariffGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingPoolGroup_Id, ChargingPoolGroup>(
                chargingStationOperatorPath + "/CPGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingPoolGroups, null)
                               : (null, errorResponse),
                chargingPoolGroup => chargingPoolGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingPoolGroup_Id, ChargingPoolGroup>(
                chargingStationOperatorPath + "/CPGs/{chargingPoolGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingPoolGroups, null)
                               : (null, errorResponse),
                ChargingPoolGroup_Id.TryParse,
                "charging pool group",
                chargingPoolGroup => chargingPoolGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingStationGroup_Id, ChargingStationGroup>(
                chargingStationOperatorPath + "/CSGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingStationGroups, null)
                               : (null, errorResponse),
                chargingStationGroup => chargingStationGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingStationGroup_Id, ChargingStationGroup>(
                chargingStationOperatorPath + "/CSGs/{chargingStationGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.ChargingStationGroups, null)
                               : (null, errorResponse),
                ChargingStationGroup_Id.TryParse,
                "charging station group",
                chargingStationGroup => chargingStationGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<EVSEGroup_Id, EVSEGroup>(
                chargingStationOperatorPath + "/EVSEGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EVSEGroups, null)
                               : (null, errorResponse),
                eVSEGroup => eVSEGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<EVSEGroup_Id, EVSEGroup>(
                chargingStationOperatorPath + "/EVSEGs/{EVSEGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EVSEGroups, null)
                               : (null, errorResponse),
                EVSEGroup_Id.TryParse,
                "EVSE group",
                eVSEGroup => eVSEGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<EnergyMeterGroup_Id, EnergyMeterGroup>(
                chargingStationOperatorPath + "/EMGs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EnergyMeterGroups, null)
                               : (null, errorResponse),
                energyMeterGroup => energyMeterGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<EnergyMeterGroup_Id, EnergyMeterGroup>(
                chargingStationOperatorPath + "/EMGs/{energyMeterGroupId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.EnergyMeterGroups, null)
                               : (null, errorResponse),
                EnergyMeterGroup_Id.TryParse,
                "energy meter group",
                energyMeterGroup => energyMeterGroup.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}

            AddEntityHandlers(chargingPoolPath,
                              request => {

                                  if (!TryGetRequestedChargingPool  (request, out var chargingPool,   out var errorResponse) ||
                                      !TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out errorResponse))
                                  {
                                      return errorResponse;
                                  }

                                  // Every part of every parameter of its name: a list, separated by commas or given more than once.
                                  var expansions = request.QueryString.GetStrings("expand").
                                                       SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).
                                                       ToArray();

                                  // The ETag a precondition names: the one of the charging pool as its operator embeds it.
                                  var storedETag = roamingNetwork.TryGetChargingStationOperatorById(chargingPool.Id.OperatorId, out var chargingStationOperator) &&
                                                   chargingStationOperator.TryGetChargingPool(chargingPool.Id, out var storedChargingPool)
                                                       ? StoredETagOf(request, storedChargingPool, () => storedChargingPool.ToJSON(Embedded: true))
                                                       : null;

                                  if (expansions.Length == 0)
                                      return CachedEntityResponse(request, chargingPool, () => chargingPool.ToJSON(Embedded: true), StoredETag: storedETag);

                                  // An expanded view depends on the version of the pool and on the one of its grid connection point.
                                  var json = chargingPool.ToJSON(Embedded: true);

                                  foreach (var expansion in expansions)
                                      if (expansion != "gridConnectionPoint")
                                          return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Unknown expansion '{expansion}'; a charging pool expands only 'gridConnectionPoint'!");

                                  // Embedded with its grid operator, in the place of its identification: as it is at runtime,
                                  // as the charging pool is, unless the view is the stored one.
                                  if (chargingPool.GridConnectionPointId is GridConnectionPoint_Id gridConnectionPointId &&
                                      roamingNetwork.TryGetGridConnectionPoint(gridConnectionPointId, out var gridOperator, out var gridConnectionPoint) &&
                                      json.Property(ChargingPool.PropertyKeys.GridConnectionPointId) is JProperty idProperty)
                                  {

                                      if (!TryGetStoredView(request, out var stored, out errorResponse))
                                          return errorResponse;

                                      var shown    = stored
                                                         ? gridConnectionPoint
                                                         : gridOperator.EffectiveGridConnectionPoint(gridConnectionPoint);

                                      var embedded = new JObject(
                                                         new JProperty(POI.RoamingNetwork.BasePropertyKeys.Id,             gridConnectionPoint.Id.ToString()),
                                                         new JProperty(PropertyKeys.GridOperatorId,  gridOperator.Id.ToString())
                                                     );

                                      foreach (var property in shown.ToJSON(Embedded: true).Properties().Where(property => property.Name != POI.RoamingNetwork.BasePropertyKeys.Id))
                                          embedded.Add(new JProperty(property.Name, property.Value.DeepClone()));

                                      idProperty.Replace(new JProperty(PropertyKeys.GridConnectionPoint, embedded));

                                  }

                                  return EntityResponse(request, FormatOf(request), POIRepresentation.Of(json, FormatOf(request)), StoredETag: storedETag);

                              });

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}

            AddEntityHandlers(chargingStationPath,
                              request => TryGetRequestedChargingStation         (request, out var chargingStation,         out var errorResponse) &&
                                         TryGetRequestedChargingStationOperator (request, out var chargingStationOperator, out errorResponse)
                                             ? CachedEntityResponse(request,
                                                                    chargingStation,
                                                                    () => chargingStation.ToJSON(Embedded: true),
                                                                    StoredETag: chargingStationOperator.TryGetChargingStation(chargingStation.Id, out var stored)
                                                                                    ? StoredETagOf(request, stored, () => stored.ToJSON(Embedded: true))
                                                                                    : null)
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/PSs, and each of them below

            // The parking spaces a charging station charges at: named by it, defined by parking operators
            // or by its charging station operator.
            AddEmbeddedListHandlers<ParkingSpace_Id, ParkingSpace>(
                chargingStationPath + "/PSs",
                RequestedParkingSpacesOfChargingStation,
                parkingSpace => parkingSpace.ToJSON(Embedded: true),
                Viewed: true);

            AddEmbeddedEntityHandlers<ParkingSpace_Id, ParkingSpace>(
                chargingStationPath + "/PSs/{parkingSpaceId}",
                RequestedParkingSpacesOfChargingStation,
                ParkingSpace_Id.TryParse,
                "parking space",
                parkingSpace => parkingSpace.ToJSON(Embedded: true),
                (request, parkingSpace) => TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out _) &&
                                           roamingNetwork.TryGetParkingSpaceById(parkingSpace.Id, out var stored)
                                               ? stored
                                               : null);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}

            AddEntityHandlers(chargingStationPath + "/EVSEs/{evseId}",
                              request => TryGetRequestedEVSE                    (request, out var evse,                    out var errorResponse) &&
                                         TryGetRequestedChargingStationOperator (request, out var chargingStationOperator, out errorResponse)
                                             ? CachedEntityResponse(request,
                                                                    evse,
                                                                    () => evse.ToJSON(Embedded: true),
                                                                    StoredETag: chargingStationOperator.TryGetEVSE(evse.Id, out var stored)
                                                                                    ? StoredETagOf(request, stored, () => stored.ToJSON(Embedded: true))
                                                                                    : null)
                                             : errorResponse);

            #endregion

            #region The brands of a charging station operator, pool, station and EVSE: ~/…/Brands, and each of them below

            // Every level its own brands: the white labels of an operator, the brands a pool, station or EVSE also appears under.
            AddEmbeddedListHandlers<Brand_Id, Brand>(
                chargingStationOperatorPath + "/Brands",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.Brands, null)
                               : (null, errorResponse),
                brand => brand.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<Brand_Id, Brand>(
                chargingStationOperatorPath + "/Brands/{brandId}",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                               ? (chargingStationOperator.Brands, null)
                               : (null, errorResponse),
                Brand_Id.TryParse,
                "brand of the charging station operator",
                brand => brand.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<Brand_Id, Brand>(
                chargingPoolPath + "/Brands",
                request => TryGetRequestedChargingPool(request, out var chargingPool, out var errorResponse)
                               ? (chargingPool.Brands, null)
                               : (null, errorResponse),
                brand => brand.ToJSON(Embedded: true),
                Viewed: true);

            AddEmbeddedEntityHandlers<Brand_Id, Brand>(
                chargingPoolPath + "/Brands/{brandId}",
                request => TryGetRequestedChargingPool(request, out var chargingPool, out var errorResponse)
                               ? (chargingPool.Brands, null)
                               : (null, errorResponse),
                Brand_Id.TryParse,
                "brand of the charging pool",
                brand => brand.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<Brand_Id, Brand>(
                chargingStationPath + "/Brands",
                request => TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse)
                               ? (chargingStation.Brands, null)
                               : (null, errorResponse),
                brand => brand.ToJSON(Embedded: true),
                Viewed: true);

            AddEmbeddedEntityHandlers<Brand_Id, Brand>(
                chargingStationPath + "/Brands/{brandId}",
                request => TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse)
                               ? (chargingStation.Brands, null)
                               : (null, errorResponse),
                Brand_Id.TryParse,
                "brand of the charging station",
                brand => brand.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<Brand_Id, Brand>(
                chargingStationPath + "/EVSEs/{evseId}" + "/Brands",
                request => TryGetRequestedEVSE(request, out var evse, out var errorResponse)
                               ? (evse.Brands, null)
                               : (null, errorResponse),
                brand => brand.ToJSON(Embedded: true),
                Viewed: true);

            AddEmbeddedEntityHandlers<Brand_Id, Brand>(
                chargingStationPath + "/EVSEs/{evseId}" + "/Brands/{brandId}",
                request => TryGetRequestedEVSE(request, out var evse, out var errorResponse)
                               ? (evse.Brands, null)
                               : (null, errorResponse),
                Brand_Id.TryParse,
                "brand of the EVSE",
                brand => brand.ToJSON(Embedded: true));

            #endregion

            #region The templates of a charging pool, station, EVSE, charging tariff and parking space: ~/…/CPTs, CSTs, EVSETs, CTTs, PSTs below it

            // The templates a member names, in its order, which give it properties at runtime, as its owner embeds them,
            // each with its page below the owner: so a client tells which of the values of the member come from which template.
            AddOrderedListHandlers<ChargingPoolTemplate>(
                chargingPoolPath + "/CPTs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse) &&
                           TryGetRequestedChargingPool           (request, out var chargingPool,            out errorResponse)
                               ? ([ .. chargingStationOperator.TemplatesOf(chargingPool) ], null)
                               : (null, errorResponse),
                chargingPoolTemplate => chargingPoolTemplate.ToJSON(Embedded: true));

            AddOrderedListHandlers<ChargingStationTemplate>(
                chargingStationPath + "/CSTs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse) &&
                           TryGetRequestedChargingStation        (request, out var chargingStation,         out errorResponse)
                               ? ([ .. chargingStationOperator.TemplatesOf(chargingStation) ], null)
                               : (null, errorResponse),
                chargingStationTemplate => chargingStationTemplate.ToJSON(Embedded: true));

            AddOrderedListHandlers<EVSETemplate>(
                chargingStationPath + "/EVSEs/{evseId}" + "/EVSETs",
                request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse) &&
                           TryGetRequestedEVSE                   (request, out var evse,                    out errorResponse)
                               ? ([ .. chargingStationOperator.TemplatesOf(evse) ], null)
                               : (null, errorResponse),
                evseTemplate => evseTemplate.ToJSON(Embedded: true));

            AddOrderedListHandlers<ChargingTariffTemplate>(
                chargingStationOperatorPath + "/CTs/{chargingTariffId}" + "/CTTs",
                request => {

                    if (!TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse))
                        return (null, errorResponse);

                    var text = request.ParsedURLParameters[2];

                    if (!ChargingTariff_Id.TryParse(text, out var chargingTariffId))
                        return (null, EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid charging tariff identification '{text}'!"));

                    if (!chargingStationOperator.TryGetChargingTariff(chargingTariffId, out var chargingTariff))
                        return (null, EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The charging tariff '{chargingTariffId}' is unknown at the operator '{chargingStationOperator.Id}'!"));

                    return ([ .. chargingStationOperator.TemplatesOf(chargingTariff) ], null);

                },
                chargingTariffTemplate => chargingTariffTemplate.ToJSON(Embedded: true));

            AddOrderedListHandlers<ParkingSpaceTemplate>(
                parkingOperatorPath + "/PSs/{parkingSpaceId}" + "/PSTs",
                request => {

                    if (!TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse))
                        return (null, errorResponse);

                    var text = request.ParsedURLParameters[2];

                    if (!ParkingSpace_Id.TryParse(text, out var parkingSpaceId))
                        return (null, EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid parking space identification '{text}'!"));

                    if (!parkingOperator.TryGetParkingSpace(parkingSpaceId, out var parkingSpace))
                        return (null, EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The parking space '{parkingSpaceId}' is unknown at the parking operator '{parkingOperator.Id}'!"));

                    return ([ .. parkingOperator.TemplatesOf(parkingSpace) ], null);

                },
                parkingSpaceTemplate => parkingSpaceTemplate.ToJSON(Embedded: true));

            #endregion

            #region The charging tariff offers covering an EVSE, or one of the EVSEs of a charging station or pool: ~/…/CTOs below it

            // Of its operator and of e-mobility providers; ?status=active leaves out all others, ?eMobilityProviderId= all
            // but those of the operator and of that provider. The member itself does not change.
            AddCoveringOffersHandlers(
                chargingStationPath + "/EVSEs/{evseId}" + "/CTOs",
                request => TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse) &&
                           TryGetRequestedEVSE          (request, out var evse,           out errorResponse)
                               ? (roamingNetwork, [ evse.Id ], null)
                               : (null, null, errorResponse));

            AddCoveringOffersHandlers(
                chargingStationPath + "/CTOs",
                request => TryGetRequestedRoamingNetwork (request, out var roamingNetwork,  out var errorResponse) &&
                           TryGetRequestedChargingStation(request, out var chargingStation, out errorResponse)
                               ? (roamingNetwork, [ .. chargingStation.EVSEs.Select(evse => evse.Id) ], null)
                               : (null, null, errorResponse));

            AddCoveringOffersHandlers(
                chargingPoolPath + "/CTOs",
                request => TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse) &&
                           TryGetRequestedChargingPool  (request, out var chargingPool,   out errorResponse)
                               ? (roamingNetwork, [ .. chargingPool.ChargingStations.SelectMany(chargingStation => chargingStation.EVSEs).Select(evse => evse.Id) ], null)
                               : (null, null, errorResponse));

            #endregion

            #region The groups of a charging pool, station, EVSE and parking space: ~/…/CPGs, CSGs, EVSEGs, PSGs below it

            // The groups a member is in, which give it nothing, of its operator and of e-mobility providers, as their owners
            // embed them, each with its page below its owner.
            AddEmbeddedListHandlers<ChargingPoolGroup_Id, ChargingPoolGroup>(
                chargingPoolPath + "/CPGs",
                request => TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse) &&
                           TryGetRequestedChargingPool  (request, out var chargingPool,   out errorResponse)
                               ? (new POICollection<ChargingPoolGroup_Id, ChargingPoolGroup>(roamingNetwork.ChargingPoolGroupsOf(chargingPool.Id),
                                                                                             chargingPoolGroup => chargingPoolGroup.Id,
                                                                                             "charging pool group",
                                                                                             $"the charging pool '{chargingPool.Id}'"),
                                  null)
                               : (null, errorResponse),
                chargingPoolGroup => chargingPoolGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingStationGroup_Id, ChargingStationGroup>(
                chargingStationPath + "/CSGs",
                request => TryGetRequestedRoamingNetwork (request, out var roamingNetwork,  out var errorResponse) &&
                           TryGetRequestedChargingStation(request, out var chargingStation, out errorResponse)
                               ? (new POICollection<ChargingStationGroup_Id, ChargingStationGroup>(roamingNetwork.ChargingStationGroupsOf(chargingStation.Id),
                                                                                                   chargingStationGroup => chargingStationGroup.Id,
                                                                                                   "charging station group",
                                                                                                   $"the charging station '{chargingStation.Id}'"),
                                  null)
                               : (null, errorResponse),
                chargingStationGroup => chargingStationGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<EVSEGroup_Id, EVSEGroup>(
                chargingStationPath + "/EVSEs/{evseId}" + "/EVSEGs",
                request => TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse) &&
                           TryGetRequestedEVSE          (request, out var evse,           out errorResponse)
                               ? (new POICollection<EVSEGroup_Id, EVSEGroup>(roamingNetwork.EVSEGroupsOf(evse.Id),
                                                                             evseGroup => evseGroup.Id,
                                                                             "EVSE group",
                                                                             $"the EVSE '{evse.Id}'"),
                                  null)
                               : (null, errorResponse),
                evseGroup => evseGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ParkingSpaceGroup_Id, ParkingSpaceGroup>(
                parkingOperatorPath + "/PSs/{parkingSpaceId}" + "/PSGs",
                request => {

                    if (!TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse))
                        return (null, errorResponse);

                    var text = request.ParsedURLParameters[2];

                    if (!ParkingSpace_Id.TryParse(text, out var parkingSpaceId))
                        return (null, EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid parking space identification '{text}'!"));

                    if (!parkingOperator.TryGetParkingSpace(parkingSpaceId, out _))
                        return (null, EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The parking space '{parkingSpaceId}' is unknown at the parking operator '{parkingOperator.Id}'!"));

                    return (new POICollection<ParkingSpaceGroup_Id, ParkingSpaceGroup>(parkingOperator.ParkingSpaceGroupsOf(parkingSpaceId),
                                                                                       parkingSpaceGroup => parkingSpaceGroup.Id,
                                                                                       "parking space group",
                                                                                       $"the parking space '{parkingSpaceId}'"),
                            null);

                },
                parkingSpaceGroup => parkingSpaceGroup.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/GOs/{GO_Id}

            AddEntityHandlers(gridOperatorPath,
                              request => TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse)
                                             ? CachedEntityResponseWithout(request,
                                                                           gridOperator,
                                                                           () => gridOperator.ToJSON(Embedded: true),
                                                                           [ "gridConnectionPoints", "energyMeterTemplates", "energyMeterGroups" ],
                                                                           "a grid operator")
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs, EMTs, EMGs

            // As they are at runtime, with their energy meters taking the properties of their groups; with ?view=stored as they are given.
            AddEmbeddedListHandlers<GridConnectionPoint_Id, GridConnectionPoint>(
                gridOperatorPath + "/GCPs",
                RequestedGridConnectionPointsOfGridOperator,
                gridConnectionPoint => gridConnectionPoint.ToJSON(Embedded: true),
                Viewed: true);

            AddEmbeddedListHandlers<EnergyMeterTemplate_Id, EnergyMeterTemplate>(
                gridOperatorPath + "/EMTs",
                request => TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse)
                               ? (gridOperator.EnergyMeterTemplates, null)
                               : (null, errorResponse),
                energyMeterTemplate => energyMeterTemplate.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<EnergyMeterTemplate_Id, EnergyMeterTemplate>(
                gridOperatorPath + "/EMTs/{energyMeterTemplateId}",
                request => TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse)
                               ? (gridOperator.EnergyMeterTemplates, null)
                               : (null, errorResponse),
                EnergyMeterTemplate_Id.TryParse,
                "energy meter template",
                energyMeterTemplate => energyMeterTemplate.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<EnergyMeterGroup_Id, EnergyMeterGroup>(
                gridOperatorPath + "/EMGs",
                request => TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse)
                               ? (gridOperator.EnergyMeterGroups, null)
                               : (null, errorResponse),
                energyMeterGroup => energyMeterGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<EnergyMeterGroup_Id, EnergyMeterGroup>(
                gridOperatorPath + "/EMGs/{energyMeterGroupId}",
                request => TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse)
                               ? (gridOperator.EnergyMeterGroups, null)
                               : (null, errorResponse),
                EnergyMeterGroup_Id.TryParse,
                "energy meter group",
                energyMeterGroup => energyMeterGroup.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}

            AddEntityHandlers(gridOperatorPath + "/GCPs/{gridConnectionPointId}",
                              request => {

                                  if (!TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse) ||
                                      !TryGetStoredView           (request, out var stored,           out errorResponse))
                                  {
                                      return errorResponse;
                                  }

                                  if (!GridConnectionPoint_Id.TryParse(request.ParsedURLParameters[2], out var gridConnectionPointId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid grid connection point identification!");

                                  if (!gridOperator.TryGetGridConnectionPoint(gridConnectionPointId, out var gridConnectionPoint))
                                      return EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The grid connection point '{gridConnectionPointId}' of the grid operator '{gridOperator.Id}' is unknown!");

                                  // As it is at runtime, unless the view is the stored one; the ETag of its stored representation in either view.
                                  var shown = stored
                                                  ? gridConnectionPoint
                                                  : gridOperator.EffectiveGridConnectionPoint(gridConnectionPoint);

                                  return CachedEntityResponse(request,
                                                              shown,
                                                              () => shown.ToJSON(Embedded: true),
                                                              StoredETag: StoredETagOf(request, gridConnectionPoint, () => gridConnectionPoint.ToJSON(Embedded: true)));

                              });

            #endregion

            #region ~/RNs/{RN_Id}/EMPs/{EMP_Id}

            AddEntityHandlers(eMobilityProviderPath,
                              request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                                             ? CachedEntityResponseWithout(request,
                                                                           eMobilityProvider,
                                                                           () => eMobilityProvider.ToJSON(Embedded: true),
                                                                           [ "EVSEGroups", "chargingStationGroups", "chargingPoolGroups", "chargingTariffOffers" ],
                                                                           "an e-mobility provider")
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/EMPs/{EMP_Id}/EVSEGs, CSGs, CPGs, CTOs, and each of them below

            // Its groups, of members of any operator, and its charging tariff offers: a list each, and each of them on its own.
            AddEmbeddedListHandlers<ChargingTariffOffer_Id, ChargingTariffOffer>(
                eMobilityProviderPath + "/CTOs",
                request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                               ? (eMobilityProvider.ChargingTariffOffers, null)
                               : (null, errorResponse),
                chargingTariffOffer => chargingTariffOffer.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingTariffOffer_Id, ChargingTariffOffer>(
                eMobilityProviderPath + "/CTOs/{chargingTariffOfferId}",
                request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                               ? (eMobilityProvider.ChargingTariffOffers, null)
                               : (null, errorResponse),
                ChargingTariffOffer_Id.TryParse,
                "charging tariff offer",
                chargingTariffOffer => chargingTariffOffer.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<EVSEGroup_Id, EVSEGroup>(
                eMobilityProviderPath + "/EVSEGs",
                request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                               ? (eMobilityProvider.EVSEGroups, null)
                               : (null, errorResponse),
                evseGroup => evseGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<EVSEGroup_Id, EVSEGroup>(
                eMobilityProviderPath + "/EVSEGs/{EVSEGroupId}",
                request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                               ? (eMobilityProvider.EVSEGroups, null)
                               : (null, errorResponse),
                EVSEGroup_Id.TryParse,
                "EVSE group",
                evseGroup => evseGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingStationGroup_Id, ChargingStationGroup>(
                eMobilityProviderPath + "/CSGs",
                request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                               ? (eMobilityProvider.ChargingStationGroups, null)
                               : (null, errorResponse),
                chargingStationGroup => chargingStationGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingStationGroup_Id, ChargingStationGroup>(
                eMobilityProviderPath + "/CSGs/{chargingStationGroupId}",
                request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                               ? (eMobilityProvider.ChargingStationGroups, null)
                               : (null, errorResponse),
                ChargingStationGroup_Id.TryParse,
                "charging station group",
                chargingStationGroup => chargingStationGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ChargingPoolGroup_Id, ChargingPoolGroup>(
                eMobilityProviderPath + "/CPGs",
                request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                               ? (eMobilityProvider.ChargingPoolGroups, null)
                               : (null, errorResponse),
                chargingPoolGroup => chargingPoolGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingPoolGroup_Id, ChargingPoolGroup>(
                eMobilityProviderPath + "/CPGs/{chargingPoolGroupId}",
                request => TryGetRequestedEMobilityProvider(request, out var eMobilityProvider, out var errorResponse)
                               ? (eMobilityProvider.ChargingPoolGroups, null)
                               : (null, errorResponse),
                ChargingPoolGroup_Id.TryParse,
                "charging pool group",
                chargingPoolGroup => chargingPoolGroup.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/CSMs/{CSM_Id}

            AddEntityHandlers(roamingNetworkPath + "/CSMs/{chargingStationManufacturerId}",
                              request => {

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  if (!ChargingStationManufacturer_Id.TryParse(request.ParsedURLParameters[1], out var chargingStationManufacturerId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid charging station manufacturer identification!");

                                  return roamingNetwork.TryGetChargingStationManufacturerById(chargingStationManufacturerId, out var chargingStationManufacturer)
                                             ? CachedEntityResponseWithout(request,
                                                                           chargingStationManufacturer,
                                                                           () => chargingStationManufacturer.ToJSON(Embedded: true),
                                                                           [ ChargingStationManufacturer.PropertyKeys.Models ],
                                                                           "a charging station manufacturer")
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The charging station manufacturer '{chargingStationManufacturerId}' is unknown in the roaming network '{roamingNetwork.Id}'!");

                              });

            #endregion

            #region ~/RNs/{RN_Id}/CSMs/{CSM_Id}/models[/{Model_Id}]

            AddEmbeddedListHandlers<ChargingStationModel_Id, ChargingStationModel>(
                roamingNetworkPath + "/CSMs/{chargingStationManufacturerId}/models",
                RequestedModelsOfChargingStationManufacturer,
                model => model.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ChargingStationModel_Id, ChargingStationModel>(
                roamingNetworkPath + "/CSMs/{chargingStationManufacturerId}/models/{modelId}",
                RequestedModelsOfChargingStationManufacturer,
                ChargingStationModel_Id.TryParse,
                "charging station model",
                model => model.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/EMMs/{EMM_Id}

            AddEntityHandlers(roamingNetworkPath + "/EMMs/{energyMeterManufacturerId}",
                              request => {

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  if (!EnergyMeterManufacturer_Id.TryParse(request.ParsedURLParameters[1], out var energyMeterManufacturerId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid energy meter manufacturer identification!");

                                  return roamingNetwork.TryGetEnergyMeterManufacturerById(energyMeterManufacturerId, out var energyMeterManufacturer)
                                             ? CachedEntityResponseWithout(request,
                                                                           energyMeterManufacturer,
                                                                           () => energyMeterManufacturer.ToJSON(Embedded: true),
                                                                           [ EnergyMeterManufacturer.PropertyKeys.Models ],
                                                                           "an energy meter manufacturer")
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The energy meter manufacturer '{energyMeterManufacturerId}' is unknown in the roaming network '{roamingNetwork.Id}'!");

                              });

            #endregion

            #region ~/RNs/{RN_Id}/EMMs/{EMM_Id}/models[/{Model_Id}]

            AddEmbeddedListHandlers<EnergyMeterModel_Id, EnergyMeterModel>(
                roamingNetworkPath + "/EMMs/{energyMeterManufacturerId}/models",
                RequestedModelsOfEnergyMeterManufacturer,
                model => model.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<EnergyMeterModel_Id, EnergyMeterModel>(
                roamingNetworkPath + "/EMMs/{energyMeterManufacturerId}/models/{modelId}",
                RequestedModelsOfEnergyMeterManufacturer,
                EnergyMeterModel_Id.TryParse,
                "energy meter model",
                model => model.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/CABs/{CAB_Id}

            AddEntityHandlers(roamingNetworkPath + "/CABs/{conformityAssessmentBodyId}",
                              request => {

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  if (!ConformityAssessmentBody_Id.TryParse(request.ParsedURLParameters[1], out var conformityAssessmentBodyId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid conformity assessment body identification!");

                                  return roamingNetwork.TryGetConformityAssessmentBodyById(conformityAssessmentBodyId, out var conformityAssessmentBody)
                                             ? CachedEntityResponseWithout(request,
                                                                           conformityAssessmentBody,
                                                                           () => conformityAssessmentBody.ToJSON(Embedded: true),
                                                                           [ ConformityAssessmentBody.PropertyKeys.TransparencySoftwareCertificates ],
                                                                           "a conformity assessment body")
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The conformity assessment body '{conformityAssessmentBodyId}' is unknown in the roaming network '{roamingNetwork.Id}'!");

                              });

            #endregion

            #region ~/RNs/{RN_Id}/CABs/{CAB_Id}/TSCs[/{TSC_Id}]

            AddEmbeddedListHandlers<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>(
                roamingNetworkPath + "/CABs/{conformityAssessmentBodyId}/TSCs",
                RequestedCertificatesOfConformityAssessmentBody,
                certificate => certificate.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>(
                roamingNetworkPath + "/CABs/{conformityAssessmentBodyId}/TSCs/{certificateId}",
                RequestedCertificatesOfConformityAssessmentBody,
                TransparencySoftwareCertificate_Id.TryParse,
                "transparency software certificate",
                certificate => certificate.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/TSMs/{TSM_Id}

            AddEntityHandlers(roamingNetworkPath + "/TSMs/{transparencySoftwareManufacturerId}",
                              request => {

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  if (!TransparencySoftwareManufacturer_Id.TryParse(request.ParsedURLParameters[1], out var transparencySoftwareManufacturerId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid transparency software manufacturer identification!");

                                  return roamingNetwork.TryGetTransparencySoftwareManufacturerById(transparencySoftwareManufacturerId, out var transparencySoftwareManufacturer)
                                             ? CachedEntityResponseWithout(request,
                                                                           transparencySoftwareManufacturer,
                                                                           () => transparencySoftwareManufacturer.ToJSON(Embedded: true),
                                                                           [ TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware ],
                                                                           "a transparency software manufacturer")
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The transparency software manufacturer '{transparencySoftwareManufacturerId}' is unknown in the roaming network '{roamingNetwork.Id}'!");

                              });

            #endregion

            #region ~/RNs/{RN_Id}/TSMs/{TSM_Id}/TSs[/{TS_Id}]

            AddEmbeddedListHandlers<TransparencySoftware_Id, TransparencySoftware>(
                roamingNetworkPath + "/TSMs/{transparencySoftwareManufacturerId}/TSs",
                RequestedReleasesOfTransparencySoftwareManufacturer,
                release => release.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<TransparencySoftware_Id, TransparencySoftware>(
                roamingNetworkPath + "/TSMs/{transparencySoftwareManufacturerId}/TSs/{transparencySoftwareId}",
                RequestedReleasesOfTransparencySoftwareManufacturer,
                TransparencySoftware_Id.TryParse,
                "transparency software release",
                release => release.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/POs/{PO_Id}

            // Its parking spaces and their sensors are what makes a parking operator large: one each for every
            // space of every garage. Without them, or without anything else it embeds, a list of their own each.
            AddEntityHandlers(parkingOperatorPath,
                              request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                                             ? CachedEntityResponseWithout(request,
                                                                           parkingOperator,
                                                                           () => parkingOperator.ToJSON(Embedded: true),
                                                                           [ "parkingGarages", "parkingSpaces", "parkingSpaceTemplates", "parkingSpaceGroups", "parkingSensors", "parkingProducts" ],
                                                                           "a parking operator")
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/POs/{PO_Id}/PGs, PSs, PSTs, PSGs, PSNs, PPs

            AddEmbeddedListHandlers<ParkingGarage_Id, ParkingGarage>(
                parkingOperatorPath + "/PGs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingGarages, null)
                               : (null, errorResponse),
                parkingGarage => parkingGarage.ToJSON(Embedded: true));

            // As they are at runtime, with the properties their groups give them; with ?view=stored as they are given.
            AddEmbeddedListHandlers<ParkingSpace_Id, ParkingSpace>(
                parkingOperatorPath + "/PSs",
                RequestedParkingSpacesOfParkingOperator,
                parkingSpace => parkingSpace.ToJSON(Embedded: true),
                Viewed: true);

            AddEmbeddedListHandlers<ParkingSpaceTemplate_Id, ParkingSpaceTemplate>(
                parkingOperatorPath + "/PSTs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSpaceTemplates, null)
                               : (null, errorResponse),
                parkingSpaceTemplate => parkingSpaceTemplate.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ParkingSpaceTemplate_Id, ParkingSpaceTemplate>(
                parkingOperatorPath + "/PSTs/{parkingSpaceTemplateId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSpaceTemplates, null)
                               : (null, errorResponse),
                ParkingSpaceTemplate_Id.TryParse,
                "parking space template",
                parkingSpaceTemplate => parkingSpaceTemplate.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ParkingSpaceGroup_Id, ParkingSpaceGroup>(
                parkingOperatorPath + "/PSGs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSpaceGroups, null)
                               : (null, errorResponse),
                parkingSpaceGroup => parkingSpaceGroup.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ParkingSensor_Id, ParkingSensor>(
                parkingOperatorPath + "/PSNs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSensors, null)
                               : (null, errorResponse),
                parkingSensor => parkingSensor.ToJSON(Embedded: true));

            AddEmbeddedListHandlers<ParkingProduct_Id, ParkingProduct>(
                parkingOperatorPath + "/PPs",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingProducts, null)
                               : (null, errorResponse),
                parkingProduct => parkingProduct.ToJSON(Embedded: true));

            #endregion

            #region ~/RNs/{RN_Id}/POs/{PO_Id}/PGs/{PG_Id}, PSs/{PS_Id}, PSGs/{PSG_Id}, PSNs/{PSN_Id}, PPs/{PP_Id}

            AddEmbeddedEntityHandlers<ParkingGarage_Id, ParkingGarage>(
                parkingOperatorPath + "/PGs/{parkingGarageId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingGarages, null)
                               : (null, errorResponse),
                ParkingGarage_Id.TryParse,
                "parking garage",
                parkingGarage => parkingGarage.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ParkingSpace_Id, ParkingSpace>(
                parkingOperatorPath + "/PSs/{parkingSpaceId}",
                RequestedParkingSpacesOfParkingOperator,
                ParkingSpace_Id.TryParse,
                "parking space",
                parkingSpace => parkingSpace.ToJSON(Embedded: true),
                (request, parkingSpace) => TryGetRequestedParkingOperator(request, out var parkingOperator, out _) &&
                                           parkingOperator.TryGetParkingSpace(parkingSpace.Id, out var stored)
                                               ? stored
                                               : null);

            AddEmbeddedEntityHandlers<ParkingSpaceGroup_Id, ParkingSpaceGroup>(
                parkingOperatorPath + "/PSGs/{parkingSpaceGroupId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSpaceGroups, null)
                               : (null, errorResponse),
                ParkingSpaceGroup_Id.TryParse,
                "parking space group",
                parkingSpaceGroup => parkingSpaceGroup.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ParkingSensor_Id, ParkingSensor>(
                parkingOperatorPath + "/PSNs/{parkingSensorId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingSensors, null)
                               : (null, errorResponse),
                ParkingSensor_Id.TryParse,
                "parking sensor",
                parkingSensor => parkingSensor.ToJSON(Embedded: true));

            AddEmbeddedEntityHandlers<ParkingProduct_Id, ParkingProduct>(
                parkingOperatorPath + "/PPs/{parkingProductId}",
                request => TryGetRequestedParkingOperator(request, out var parkingOperator, out var errorResponse)
                               ? (parkingOperator.ParkingProducts, null)
                               : (null, errorResponse),
                ParkingProduct_Id.TryParse,
                "parking product",
                parkingProduct => parkingProduct.ToJSON(Embedded: true));

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


        #region (private) AddEmbeddedListHandlers(Path, Collection, ToJSON, Viewed = false)

        /// <summary>
        /// Answer GET of the given path with the children of an entity as it embeds them, searched and paged
        /// by the query of the request; the whole list kept with their collection. HEAD by Hermod from it,
        /// and OPTIONS.
        /// </summary>
        /// <param name="Path">The path of the list, below the one of the entity.</param>
        /// <param name="Collection">The children of the entity the path names, or why there are none.</param>
        /// <param name="ToJSON">A child, as the entity embeds it.</param>
        /// <param name="Viewed">Whether the list has the view ?view=stored: of charging pools, stations, EVSEs and parking
        /// spaces and what they embed, which they are at runtime otherwise. Any other list is as it is given, and a view
        /// asked for is no parameter of it.</param>
        private void AddEmbeddedListHandlers<TId, TEntity>(HTTPPath                                                                          Path,
                                                           Func<HTTPRequest, (POICollection<TId, TEntity>? Collection, HTTPResponse? ErrorResponse)>  Collection,
                                                           Func<TEntity, JObject>                                                            ToJSON,
                                                           Boolean                                                                           Viewed   = false)

            where TId     : notnull
            where TEntity : class

            => AddEntityHandlers(Path,
                                 request => {

                                     var (collection, errorResponse) = Collection(request);

                                     if (collection is null)
                                         return errorResponse!;

                                     // The view in the links to other pages as well.
                                     var stored = false;

                                     if (Viewed && !TryGetStoredView(request, out stored, out errorResponse) ||
                                         !TryParseListQuery(request, stored ? [ ("view", StoredView) ] : [], out var query, out errorResponse))
                                     {
                                         return errorResponse!;
                                     }

                                     // In the natural order of their identifications, not in the one their entity embeds them in.
                                     return QueryResponse<TEntity>(request,
                                                                   query,
                                                                   () => collection.InNaturalOrder,
                                                                   child => SearchTextOf(child, () => ToJSON(child)),
                                                                   entries => new JArray(entries.Select(ToJSON)),
                                                                   format => collection.Representation(EmbeddedView, format, () => new JArray(collection.InNaturalOrder.Select(ToJSON))),
                                                                   collection.Count);

                                 });

        #endregion


        #region (private) AddCoveringOffersHandlers(Path, EVSEs)

        /// <summary>
        /// Answer GET of the given path with the charging tariff offers covering one of the given EVSEs: of their operator,
        /// then of e-mobility providers, each in the order of their identifications, searched and paged by the query of the
        /// request. ?status= leaves out the offers of another status now, e.g. ?status=active; ?eMobilityProviderId= the
        /// offers of every other provider; ?applicable=true those that do not apply at one of the EVSEs now, or at the time
        /// &at= gives (RoamingNetwork.ApplicableOffers). HEAD by Hermod from it, and OPTIONS.
        /// </summary>
        /// <param name="Path">The path of the list, below the one of the member.</param>
        /// <param name="EVSEs">The roaming network and the EVSEs of the member the path names, or why there are none.</param>
        private void AddCoveringOffersHandlers(HTTPPath                                                                                            Path,
                                               Func<HTTPRequest, (RoamingNetwork? Network, IReadOnlyList<EVSE_Id>? EVSEIds, HTTPResponse? ErrorResponse)>  EVSEs)

            => AddEntityHandlers(Path,
                                 request => {

                                     var (roamingNetwork, evseIds, errorResponse) = EVSEs(request);

                                     if (roamingNetwork is null || evseIds is null)
                                         return errorResponse!;

                                     var further = new List<(String Name, String Value)>();

                                     ChargingTariffOfferStatusType? status = null;

                                     if (request.QueryString.GetString("status") is String statusText)
                                     {

                                         if (!ChargingTariffOfferStatusType.TryParse(statusText, out var parsedStatus) ||
                                             !new[] { ChargingTariffOfferStatusType.Planned, ChargingTariffOfferStatusType.Active, ChargingTariffOfferStatusType.Exhausted, ChargingTariffOfferStatusType.Expired }.Contains(parsedStatus))
                                         {
                                             return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid status of a charging tariff offer '{statusText}': planned, active, exhausted or expired!");
                                         }

                                         status = parsedStatus;
                                         further.Add(("status", statusText));

                                     }

                                     EMobilityProvider_Id? providerId = null;

                                     if (request.QueryString.GetString("eMobilityProviderId") is String providerText)
                                     {

                                         if (!EMobilityProvider_Id.TryParse(providerText, out var parsedProviderId))
                                             return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid e-mobility provider identification '{providerText}'!");

                                         providerId = parsedProviderId;
                                         further.Add(("eMobilityProviderId", providerText));

                                     }

                                     var now        = Timestamp.Now;

                                     // The offers that apply at the given time, now by default.
                                     DateTimeOffset? applicableAt = null;

                                     if (request.QueryString.GetString("applicable") is String applicableText)
                                     {

                                         if (applicableText != "true")
                                             return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid applicable '{applicableText}': only true!");

                                         applicableAt = now;
                                         further.Add(("applicable", applicableText));

                                     }

                                     if (request.QueryString.GetString("at") is String atText)
                                     {

                                         if (!applicableAt.HasValue)
                                             return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "A time 'at' is only given together with applicable=true!");

                                         if (!DateTimeOffset.TryParse(atText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at))
                                             return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid time '{atText}'!");

                                         applicableAt = at;
                                         further.Add(("at", atText));

                                     }

                                     if (!TryParseListQuery(request, further, out var query, out errorResponse))
                                         return errorResponse!;

                                     var operators  = roamingNetwork.ChargingStationOperators.ToDictionary(chargingStationOperator => chargingStationOperator.Id);
                                     var providers  = roamingNetwork.EMobilityProviders.ToArray();

                                     var applicable = applicableAt.HasValue
                                                          ? evseIds.SelectMany(evseId => roamingNetwork.ApplicableOffers(evseId, applicableAt.Value)).Select(offer => offer.Id).ToHashSet()
                                                          : null;

                                     var operatorOffers  = evseIds.SelectMany(evseId => operators.TryGetValue(evseId.OperatorId, out var chargingStationOperator)
                                                                                            ? chargingStationOperator.OffersCovering(evseId)
                                                                                            : []).
                                                                   Where     (offer             => applicable?.Contains(offer.Id) != false);

                                     var providerOffers  = providers.Where     (eMobilityProvider => !providerId.HasValue || eMobilityProvider.Id == providerId.Value).
                                                                     SelectMany(eMobilityProvider => eMobilityProvider.ChargingTariffOffers).
                                                                     Where     (offer             => applicable?.Contains(offer.Id) ?? RoamingNetwork.CoveredEVSEs(offer, operators, providers).Overlaps(evseIds));

                                     var offers          = operatorOffers.DistinctBy(offer => offer.Id).OrderBy(offer => offer.Id.ToString(), StringComparer.Ordinal).
                                                               Concat(providerOffers.DistinctBy(offer => offer.Id).OrderBy(offer => offer.Id.ToString(), StringComparer.Ordinal)).
                                                               Where (offer => !status.HasValue || offer.StatusAt(now) == status.Value).
                                                               ToArray();

                                     return QueryResponse<ChargingTariffOffer>(request,
                                                                               query,
                                                                               () => offers,
                                                                               offer   => SearchTextOf(offer, () => offer.ToJSON(Embedded: true)),
                                                                               entries => new JArray(entries.Select(offer => offer.ToJSON(Embedded: true))),
                                                                               format  => POIRepresentation.Of(new JArray(offers.Select(offer => offer.ToJSON(Embedded: true))), format),
                                                                               offers.Length);

                                 });

        #endregion

        #region (private) AddOrderedListHandlers(Path, List, ToJSON)

        /// <summary>
        /// Answer GET of the given path with the templates a member names, in its order, as their owner embeds them:
        /// the order is content, the first template giving a value wins. Searched and paged by the query of the request
        /// like any list. HEAD by Hermod from it, and OPTIONS.
        /// </summary>
        /// <param name="Path">The path of the list, below the one of the member.</param>
        /// <param name="List">The templates of the member the path names, in its order, or why there are none.</param>
        /// <param name="ToJSON">A template, as its owner embeds it.</param>
        private void AddOrderedListHandlers<TEntity>(HTTPPath                                                                  Path,
                                                     Func<HTTPRequest, (IReadOnlyList<TEntity>? List, HTTPResponse? ErrorResponse)>  List,
                                                     Func<TEntity, JObject>                                                    ToJSON)

            where TEntity : class

            => AddEntityHandlers(Path,
                                 request => {

                                     var (list, errorResponse) = List(request);

                                     if (list is null)
                                         return errorResponse!;

                                     if (!TryParseListQuery(request, [], out var query, out errorResponse))
                                         return errorResponse!;

                                     return QueryResponse<TEntity>(request,
                                                                   query,
                                                                   () => list,
                                                                   child => SearchTextOf(child, () => ToJSON(child)),
                                                                   entries => new JArray(entries.Select(ToJSON)),
                                                                   format => POIRepresentation.Of(new JArray(list.Select(ToJSON)), format),
                                                                   list.Count);

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
                                                             Func<TEntity, JObject>                                                            ToJSON,
                                                             Func<HTTPRequest, TEntity, TEntity?>?                                             StoredOf   = null)

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

                                     if (!collection.TryGet(id, out var child))
                                         return EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The {Description} '{id}' is unknown here!");

                                     // An entity served as it is at runtime tells the ETag of its stored representation as well.
                                     return CachedEntityResponse(request,
                                                                 child,
                                                                 () => ToJSON(child),
                                                                 StoredETag: StoredOf?.Invoke(request, child) is TEntity stored
                                                                                 ? StoredETagOf(request, stored, () => ToJSON(stored))
                                                                                 : null);

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

        #region (private) TryGetRequestedEMobilityProvider      (Request, out EMobilityProvider,                    out ErrorResponse)

        /// <summary>
        /// Try to get the e-mobility provider the given request names, in the roaming network it names.
        /// </summary>
        private Boolean TryGetRequestedEMobilityProvider(HTTPRequest                                  Request,
                                                         [NotNullWhen(true)]  out EMobilityProvider?  EMobilityProvider,
                                                         [NotNullWhen(false)] out HTTPResponse?       ErrorResponse)
        {

            EMobilityProvider = null;

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out ErrorResponse))
                return false;

            if (!EMobilityProvider_Id.TryParse(Request.ParsedURLParameters[1], out var eMobilityProviderId))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid e-mobility provider identification!");
                return false;
            }

            if (!roamingNetwork.TryGetEMobilityProviderById(eMobilityProviderId, out EMobilityProvider))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The e-mobility provider '{eMobilityProviderId}' is unknown in the roaming network '{roamingNetwork.Id}'!");
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

        #region (private static) TryGetStoredView               (Request, out Stored,                               out ErrorResponse)

        /// <summary>
        /// Whether the request asks for ?view=stored: charging pools, charging stations, EVSEs, parking spaces and grid
        /// connection points as they are given, not as they are at runtime with the properties their groups give them, or
        /// their energy meters. Any other view is refused.
        /// </summary>
        private static Boolean TryGetStoredView(HTTPRequest                             Request,
                                                out Boolean                             Stored,
                                                [NotNullWhen(false)] out HTTPResponse?  ErrorResponse)
        {

            // Every part of every parameter of its name: a list, separated by commas or given more than once.
            var views = Request.QueryString.GetStrings("view").
                                SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).
                                Distinct().
                                ToArray();

            foreach (var view in views)
                if (view != StoredView)
                {
                    Stored         = false;
                    ErrorResponse  = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, $"Unknown view '{view}'; a charging pool, charging station, EVSE, parking space or grid connection point has only the view '{StoredView}'!");
                    return false;
                }

            Stored         = views.Length > 0;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (private) TryGetRequestedChargingPool           (Request, out ChargingPool,                         out ErrorResponse)

        /// <summary>
        /// The requested charging pool, the effective one, with the properties its groups give it, with its effective
        /// charging stations and EVSEs; with ?view=stored as its operator embeds it, the representation whose ETag a
        /// precondition names.
        /// </summary>
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

            if (!TryGetStoredView(Request, out var stored, out ErrorResponse))
                return false;

            if (!(stored
                      ? chargingStationOperator.TryGetChargingPool         (chargingPoolId, out ChargingPool)
                      : chargingStationOperator.TryGetEffectiveChargingPool(chargingPoolId, out ChargingPool)))
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

            if (!TryGetRequestedRoamingNetwork (Request, out var roamingNetwork,  out var errorResponse) ||
                !TryGetRequestedChargingStation(Request, out var chargingStation, out errorResponse)     ||
                !TryGetStoredView              (Request, out var stored,          out errorResponse))
            {
                return (null, errorResponse);
            }

            // As they are at runtime, with the properties their groups give them; with ?view=stored as they are given.
            return (new POICollection<ParkingSpace_Id, ParkingSpace>(
                        chargingStation.ParkingSpaceIds.
                                        Select(parkingSpaceId => (stored
                                                                      ? roamingNetwork.TryGetParkingSpaceById         (parkingSpaceId, out var parkingSpace)
                                                                      : roamingNetwork.TryGetEffectiveParkingSpaceById(parkingSpaceId, out parkingSpace))
                                                                      ? parkingSpace
                                                                      : null).
                                        OfType<ParkingSpace>(),
                        parkingSpace => parkingSpace.Id,
                        "parking space",
                        $"the charging station '{chargingStation.Id}'"
                    ),
                    null);

        }

        #endregion

        #region (private) RequestedChargingTariffsOfOperator(Request)

        /// <summary>
        /// The charging tariffs of the charging station operator the request names, as they are at runtime, with the
        /// properties their templates give them; with ?view=stored as it embeds them; or why there are none.
        /// </summary>
        private (POICollection<ChargingTariff_Id, ChargingTariff>? Collection, HTTPResponse? ErrorResponse) RequestedChargingTariffsOfOperator(HTTPRequest Request)
        {

            if (!TryGetRequestedChargingStationOperator(Request, out var chargingStationOperator, out var errorResponse) ||
                !TryGetStoredView                      (Request, out var stored,                  out errorResponse))
            {
                return (null, errorResponse);
            }

            return (stored ? chargingStationOperator.ChargingTariffs : chargingStationOperator.EffectiveChargingTariffs, null);

        }

        #endregion

        #region (private) RequestedParkingSpacesOfParkingOperator(Request)

        /// <summary>
        /// The parking spaces of the requested parking operator as they are at runtime, with the properties their groups
        /// give them; with ?view=stored as it embeds them; or why there are none.
        /// </summary>
        private (POICollection<ParkingSpace_Id, ParkingSpace>? Collection, HTTPResponse? ErrorResponse) RequestedParkingSpacesOfParkingOperator(HTTPRequest Request)
        {

            if (!TryGetRequestedParkingOperator(Request, out var parkingOperator, out var errorResponse) ||
                !TryGetStoredView              (Request, out var stored,          out errorResponse))
            {
                return (null, errorResponse);
            }

            return (stored ? parkingOperator.ParkingSpaces : parkingOperator.EffectiveParkingSpaces, null);

        }

        #endregion

        #region (private) RequestedGridConnectionPointsOfGridOperator(Request)

        /// <summary>
        /// The grid connection points of the requested grid operator as they are at runtime, with their energy meters
        /// taking the properties of their groups; with ?view=stored as it embeds them; or why there are none.
        /// </summary>
        private (POICollection<GridConnectionPoint_Id, GridConnectionPoint>? Collection, HTTPResponse? ErrorResponse) RequestedGridConnectionPointsOfGridOperator(HTTPRequest Request)
        {

            if (!TryGetRequestedGridOperator(Request, out _, out var gridOperator, out var errorResponse) ||
                !TryGetStoredView           (Request, out var stored,           out errorResponse))
            {
                return (null, errorResponse);
            }

            return (stored ? gridOperator.GridConnectionPoints : gridOperator.EffectiveGridConnectionPoints, null);

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
                                OrderBy(roamingNetwork => roamingNetwork.Id.ToString(), POINaturalOrder.Instance).
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

            if (!TryParseOmissions(Request, RoamingNetworkOmittables, "a roaming network", out var omissions, out var errorResponse))
                return errorResponse;

            var format      = FormatOf(Request);
            var cbor        = format == ETagFormat.CBOR;
            var eTags       = RoamingNetwork.ETags.IsDefault ? [] : RoamingNetwork.ETags;
            var stored      = eTags.Where(eTag => eTag.Format == format).ToArray();

            // Without what it holds: what it says of itself, with the ETags of all of it, kept as every other view.
            // Its ETags may be calculated after it was served: a view of each number of them.
            if (omissions.Length > 0)
                return CachedEntityResponse(Request,
                                            RoamingNetwork,
                                            () => {
                                                var json = RoamingNetwork.ToJSON();
                                                foreach (var omission in omissions)
                                                    json.Remove(omission);
                                                return json;
                                            },
                                            View: $"without={String.Join(",", omissions)};eTags={eTags.Length}");

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
                                                        RoamingNetwork.ContentETag(format)));

        }

        #endregion

        #region (private static) EntitySummaries       (Entities)

        /// <summary>
        /// A list of the given entities: { "@id", "name", "description" } in the natural order of their
        /// identifications, without an empty name or description.
        /// </summary>
        private static JArray EntitySummaries<TId, TAdminStatus, TStatus>(IEnumerable<AImmutableEMobilityEntity<TId, TAdminStatus, TStatus>> Entities)

            where TId          : IId
            where TAdminStatus : IComparable
            where TStatus      : IComparable

            => new (Entities.
                        OrderBy(entity => entity.Id.ToString(), POINaturalOrder.Instance).
                        Select (entity => {

                            var json = new JObject(
                                           new JProperty(POI.RoamingNetwork.BasePropertyKeys.Id, entity.Id.ToString())
                                       );

                            if (entity.Name.IsNotNullOrEmpty())
                                json.Add(POI.RoamingNetwork.BasePropertyKeys.Name,         entity.Name.ToJSON());

                            if (entity.Description.IsNotNullOrEmpty())
                                json.Add(POI.RoamingNetwork.BasePropertyKeys.Description,  entity.Description.ToJSON());

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

        #region (private)        StoredETagOf          (Request, Stored, JSON)

        /// <summary>
        /// The HTTP ETag of the given entity as its parent embeds it, in the format the request prefers: the one a
        /// precondition of a change set names, also of an entity served as it is at runtime. Calculated once per version,
        /// by an entity having content ETags itself (IHasContentETags).
        /// </summary>
        private String StoredETagOf(HTTPRequest   Request,
                                    Object        Stored,
                                    Func<JToken>  JSON)
        {

            var format = FormatOf(Request);
            var eTag   = Stored is IHasContentETags entity
                             ? entity.ContentETag(format)
                             : representationCache.GetValue(Stored, _ => new Representations()).
                                                   GetOrAdd("", format, () => POIRepresentation.Of(JSON(), format)).
                                                   ETag;

            return $"\"{eTag.ToString(ETagDigestEncoding.HEX)}\"";

        }

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
        /// <param name="StoredETag">The ETag of the stored representation of the entity, e.g. of a charging pool served as it is at runtime: X-Stored-ETag.</param>
        private HTTPResponse CachedEntityResponse(HTTPRequest   Request,
                                                  Object        Version,
                                                  Func<JToken>  JSON,
                                                  String        View         = "",
                                                  String?       StoredETag   = null)
        {

            var format = FormatOf(Request);

            return EntityResponse(Request,
                                  format,
                                  representationCache.GetValue(Version, _ => new Representations()).
                                                      GetOrAdd(View, format, () => POIRepresentation.Of(JSON(), format)),
                                  StoredETag: StoredETag);

        }

        #endregion

        #region (private)        RequestedModelsOfChargingStationManufacturer(Request)

        /// <summary>
        /// The models of the charging station manufacturer of the given request, ~/RNs/{RN_Id}/CSMs/{CSM_Id}/...
        /// </summary>
        private (POICollection<ChargingStationModel_Id, ChargingStationModel>? Collection, HTTPResponse? ErrorResponse) RequestedModelsOfChargingStationManufacturer(HTTPRequest Request)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse))
                return (null, errorResponse);

            if (!ChargingStationManufacturer_Id.TryParse(Request.ParsedURLParameters[1], out var manufacturerId))
                return (null, EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid charging station manufacturer identification!"));

            return roamingNetwork.TryGetChargingStationManufacturerById(manufacturerId, out var manufacturer)
                       ? (manufacturer.Models, null)
                       : (null, EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The charging station manufacturer '{manufacturerId}' is unknown in the roaming network '{roamingNetwork.Id}'!"));

        }

        #endregion

        #region (private)        RequestedModelsOfEnergyMeterManufacturer(Request)

        /// <summary>
        /// The models of the energy meter manufacturer of the given request, ~/RNs/{RN_Id}/EMMs/{EMM_Id}/...
        /// </summary>
        private (POICollection<EnergyMeterModel_Id, EnergyMeterModel>? Collection, HTTPResponse? ErrorResponse) RequestedModelsOfEnergyMeterManufacturer(HTTPRequest Request)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse))
                return (null, errorResponse);

            if (!EnergyMeterManufacturer_Id.TryParse(Request.ParsedURLParameters[1], out var manufacturerId))
                return (null, EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid energy meter manufacturer identification!"));

            return roamingNetwork.TryGetEnergyMeterManufacturerById(manufacturerId, out var manufacturer)
                       ? (manufacturer.Models, null)
                       : (null, EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The energy meter manufacturer '{manufacturerId}' is unknown in the roaming network '{roamingNetwork.Id}'!"));

        }

        #endregion

        #region (private)        RequestedReleasesOfTransparencySoftwareManufacturer(Request)

        /// <summary>
        /// The releases of the transparency software manufacturer of the given request, ~/RNs/{RN_Id}/TSMs/{TSM_Id}/...
        /// </summary>
        private (POICollection<TransparencySoftware_Id, TransparencySoftware>? Collection, HTTPResponse? ErrorResponse) RequestedReleasesOfTransparencySoftwareManufacturer(HTTPRequest Request)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse))
                return (null, errorResponse);

            if (!TransparencySoftwareManufacturer_Id.TryParse(Request.ParsedURLParameters[1], out var manufacturerId))
                return (null, EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid transparency software manufacturer identification!"));

            return roamingNetwork.TryGetTransparencySoftwareManufacturerById(manufacturerId, out var manufacturer)
                       ? (manufacturer.TransparencySoftware, null)
                       : (null, EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The transparency software manufacturer '{manufacturerId}' is unknown in the roaming network '{roamingNetwork.Id}'!"));

        }

        #endregion

        #region (private)        RequestedCertificatesOfConformityAssessmentBody(Request)

        /// <summary>
        /// The certificates of the conformity assessment body of the given request, ~/RNs/{RN_Id}/CABs/{CAB_Id}/...
        /// </summary>
        private (POICollection<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>? Collection, HTTPResponse? ErrorResponse) RequestedCertificatesOfConformityAssessmentBody(HTTPRequest Request)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse))
                return (null, errorResponse);

            if (!ConformityAssessmentBody_Id.TryParse(Request.ParsedURLParameters[1], out var bodyId))
                return (null, EntityErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid conformity assessment body identification!"));

            return roamingNetwork.TryGetConformityAssessmentBodyById(bodyId, out var body)
                       ? (body.TransparencySoftwareCertificates, null)
                       : (null, EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The conformity assessment body '{bodyId}' is unknown in the roaming network '{roamingNetwork.Id}'!"));

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

            if (!TryParseOmissions(Request, Omittables, Description, out var omissions, out var errorResponse))
                return errorResponse;

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

        #region (private static) TryParseOmissions     (Request, Omittables, Description, out Omissions, out ErrorResponse)

        /// <summary>
        /// The parts ?without=… names, separated by commas or each in a parameter of its own, each once and in
        /// ordinal order, whatever the order they are named in; or the refusal of one the entity cannot leave out.
        /// </summary>
        /// <param name="Omittables">The properties the entity may leave out, e.g. its children with a list of their own.</param>
        /// <param name="Description">What the entity is, e.g. "a grid operator", for a refusal.</param>
        private static Boolean TryParseOmissions(HTTPRequest                             Request,
                                                 String[]                                Omittables,
                                                 String                                  Description,
                                                 out String[]                            Omissions,
                                                 [NotNullWhen(false)] out HTTPResponse?  ErrorResponse)
        {

            Omissions = [.. Request.QueryString.GetStrings("without").
                                    SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).
                                    Distinct().
                                    Order(StringComparer.Ordinal)];

            foreach (var omission in Omissions)
                if (!Omittables.Contains(omission))
                {
                    ErrorResponse = EntityErrorResponse(Request,
                                                        HTTPStatusCode.BadRequest,
                                                        $"Unknown omission '{omission}'; {Description} leaves out only {String.Join(", ", Omittables.Select(omittable => $"'{omittable}'"))}!");
                    return false;
                }

            ErrorResponse = null;
            return true;

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
                                                   Action<HTTPResponse.Builder>?  Headers      = null,
                                                   String?                        StoredETag   = null)
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

            if (StoredETag is not null)
                response.Set("X-Stored-ETag", StoredETag);

            // Not CORS-safelisted response headers: scripts of other origins see them only when exposed.
            response.Set("Access-Control-Expose-Headers", Headers is not null  ? "ETag, X-Total-Count, X-Filtered-Count, Link"
                                                        : StoredETag is not null ? "ETag, X-Stored-ETag"
                                                        : "ETag");

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

            var json  = new JObject(new JProperty(PropertyKeys.Description, Description));
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
