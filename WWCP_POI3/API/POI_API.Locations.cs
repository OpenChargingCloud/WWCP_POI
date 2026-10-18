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

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Where a charging pool, a charging station, an EVSE or a charging tariff offer of a roaming network is, by its
    /// identification alone.
    /// </summary>
    /// <remarks>
    /// GET ~/RNs/{RN_Id}/CPs/{CP_Id}        to ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}
    /// GET ~/RNs/{RN_Id}/CSs/{CS_Id}        to ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}
    /// GET ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}    to ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}
    /// GET ~/RNs/{RN_Id}/CTOs/{CTO_Id}      to ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTOs/{CTO_Id} or ~/RNs/{RN_Id}/EMPs/{EMP_Id}/CTOs/{CTO_Id}
    ///
    /// The charging tariff offers covering an EVSE, ~/…/EVSEs/{EVSE_Id}/CTOs, are of its operator and of e-mobility
    /// providers, and their identifications, unique in the roaming network, name no owner: they are found by it.
    ///
    /// What names a charging station or an EVSE names it by its identification only: a group of its operator, a
    /// parking garage, space or sensor of a parking operator. These find it in the roaming network and answer with
    /// 307 Temporary Redirect to where it is, the query kept: temporary, as a station may move to another pool of
    /// its operator, an EVSE to another station. A browser opening one is redirected as well, so a web frontend
    /// links them so. The operator the identification names is looked at first, then every other one.
    /// An unknown one is 404 Not Found, for a browser the HTML view.
    /// </remarks>
    public partial class POI_API
    {

        #region (private) Locations

        /// <summary>
        /// Where the charging stations and EVSEs of one version of a charging station operator are: the pool of
        /// each station, the pool and the station of each EVSE.
        /// </summary>
        private sealed class Locations
        {

            public Dictionary<ChargingStation_Id, ChargingPool_Id>                              PoolsOfStations  { get; } = [];
            public Dictionary<EVSE_Id, (ChargingPool_Id Pool, ChargingStation_Id Station)>      PlacesOfEVSEs    { get; } = [];

        }

        /// <summary>
        /// The locations of the versions of charging station operators, kept as long as the versions are: an operator
        /// unchanged by a commit is the same object in the new version, and keeps them.
        /// </summary>
        private readonly ConditionalWeakTable<ChargingStationOperator, Locations> locations = new ();

        #endregion


        #region (private) RegisterLocationURLTemplates()

        private void RegisterLocationURLTemplates()
        {

            AddLocationHandlers<ChargingPool_Id>(
                "CPs",
                ChargingPool_Id.TryParse,
                chargingPoolId => chargingPoolId.OperatorId,
                (chargingStationOperator, chargingPoolId) => chargingStationOperator.TryGetChargingPool(chargingPoolId, out _)
                                                                 ? $"CPs/{chargingPoolId}"
                                                                 : null,
                "charging pool");

            AddLocationHandlers<ChargingStation_Id>(
                "CSs",
                ChargingStation_Id.TryParse,
                chargingStationId => chargingStationId.OperatorId,
                (chargingStationOperator, chargingStationId) => LocationsOf(chargingStationOperator).PoolsOfStations.TryGetValue(chargingStationId, out var chargingPoolId)
                                                                    ? $"CPs/{chargingPoolId}/CSs/{chargingStationId}"
                                                                    : null,
                "charging station");

            AddLocationHandlers<EVSE_Id>(
                "EVSEs",
                EVSE_Id.TryParse,
                evseId => evseId.OperatorId,
                (chargingStationOperator, evseId) => LocationsOf(chargingStationOperator).PlacesOfEVSEs.TryGetValue(evseId, out var place)
                                                         ? $"CPs/{place.Pool}/CSs/{place.Station}/EVSEs/{evseId}"
                                                         : null,
                "EVSE");

            AddLocationHandlers("CTOs", OfferLocationResponse);

        }

        #endregion

        #region (private) AddLocationHandlers(Segment, TryParseId, OperatorIdOf, PathBelowOperator, Description)

        /// <summary>
        /// Answer GET of ~/RNs/{RN_Id}/{Segment}/{Id} with a redirect to where the entity is, below its operator,
        /// for a browser as well; HEAD by Hermod from it, and OPTIONS.
        /// </summary>
        /// <param name="Segment">The segment of the kind of entity, e.g. "CSs".</param>
        /// <param name="TryParseId">Parses the identification of an entity.</param>
        /// <param name="OperatorIdOf">The charging station operator an identification names.</param>
        /// <param name="PathBelowOperator">The path of the entity below the given operator, or null when it has none.</param>
        /// <param name="Description">What an entity is, e.g. "charging station", for a refusal.</param>
        private void AddLocationHandlers<TId>(String                                             Segment,
                                              TryParseIdDelegate<TId>                            TryParseId,
                                              Func<TId, ChargingStationOperator_Id>              OperatorIdOf,
                                              Func<ChargingStationOperator, TId, String?>        PathBelowOperator,
                                              String                                             Description)

            => AddLocationHandlers(Segment,
                                   request => LocationResponse(request, TryParseId, OperatorIdOf, PathBelowOperator, Description));

        #endregion

        #region (private) AddLocationHandlers(Segment, LocationResponse)

        /// <summary>
        /// Answer GET of ~/RNs/{RN_Id}/{Segment}/{Id} with the given redirect to where the entity is, or why there is
        /// none, for a browser as well; HEAD by Hermod from it, and OPTIONS.
        /// </summary>
        /// <param name="Segment">The segment of the kind of entity, e.g. "CTOs".</param>
        /// <param name="LocationResponse">The redirect to where the requested entity is, or why there is none.</param>
        private void AddLocationHandlers(String                           Segment,
                                         Func<HTTPRequest, HTTPResponse>  LocationResponse)
        {

            var path = HTTPPath.Root + $"RNs/{{roamingNetworkId}}/{Segment}/{{id}}";

            AddHandler(path,
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => {

                           var response = LocationResponse(request);

                           // A browser gets the HTML view of what is not there, as of any URL of this API.
                           return response.HTTPStatusCode != HTTPStatusCode.TemporaryRedirect && HTMLView is not null && WantsHTML(request)
                                      ? HTMLView(request)
                                      : Task.FromResult(response);

                       });

            AddHandler(path,
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

        #region (private) LocationResponse(Request, TryParseId, OperatorIdOf, PathBelowOperator, Description)

        /// <summary>
        /// The redirect to where the requested entity is, or why there is none.
        /// </summary>
        private HTTPResponse LocationResponse<TId>(HTTPRequest                                  Request,
                                                   TryParseIdDelegate<TId>                      TryParseId,
                                                   Func<TId, ChargingStationOperator_Id>        OperatorIdOf,
                                                   Func<ChargingStationOperator, TId, String?>  PathBelowOperator,
                                                   String                                       Description)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse))
                return errorResponse;

            var text = Request.ParsedURLParameters[^1];

            if (!TryParseId(text, out var id))
                return EntityErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid {Description} identification '{text}'!");

            var namedOperatorId = OperatorIdOf(id);

            // The operator the identification names first: where it is but for a few.
            IEnumerable<ChargingStationOperator> chargingStationOperators =
                roamingNetwork.TryGetChargingStationOperatorById(namedOperatorId, out var namedOperator)
                    ? [ namedOperator, .. roamingNetwork.ChargingStationOperators.Where(chargingStationOperator => chargingStationOperator.Id != namedOperatorId) ]
                    : roamingNetwork.ChargingStationOperators;

            foreach (var chargingStationOperator in chargingStationOperators)
                if (PathBelowOperator(chargingStationOperator, id) is String below)
                    return RedirectTo(Request, roamingNetwork, $"CSOs/{chargingStationOperator.Id}/{below}");

            return EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The {Description} '{id}' is unknown in the roaming network '{roamingNetwork.Id}'!");

        }

        #endregion

        #region (private) OfferLocationResponse(Request)

        /// <summary>
        /// The redirect to the requested charging tariff offer below its owner, a charging station operator or an
        /// e-mobility provider, or why there is none: its identification is unique in the roaming network.
        /// </summary>
        private HTTPResponse OfferLocationResponse(HTTPRequest Request)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse))
                return errorResponse;

            var text = Request.ParsedURLParameters[^1];

            if (!ChargingTariffOffer_Id.TryParse(text, out var offerId))
                return EntityErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid charging tariff offer identification '{text}'!");

            foreach (var chargingStationOperator in roamingNetwork.ChargingStationOperators)
                if (chargingStationOperator.TryGetChargingTariffOffer(offerId, out var offer))
                    return RedirectTo(Request, roamingNetwork, $"CSOs/{chargingStationOperator.Id}/CTOs/{Uri.EscapeDataString(offer.Id.ToString())}");

            foreach (var eMobilityProvider in roamingNetwork.EMobilityProviders)
                if (eMobilityProvider.TryGetChargingTariffOffer(offerId, out var offer))
                    return RedirectTo(Request, roamingNetwork, $"EMPs/{eMobilityProvider.Id}/CTOs/{Uri.EscapeDataString(offer.Id.ToString())}");

            return EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The charging tariff offer '{offerId}' is unknown in the roaming network '{roamingNetwork.Id}'!");

        }

        #endregion

        #region (private) RedirectTo(Request, RoamingNetwork, PathBelowNetwork)

        /// <summary>
        /// 307 Temporary Redirect to the given path below the given roaming network, the query of the request kept.
        /// </summary>
        private HTTPResponse RedirectTo(HTTPRequest     Request,
                                        RoamingNetwork  RoamingNetwork,
                                        String          PathBelowNetwork)
        {

            var requestTarget  = Request.RequestTarget ?? "";
            var queryAt        = requestTarget.IndexOf('?');

            return new HTTPResponse.Builder(Request) {
                       HTTPStatusCode            = HTTPStatusCode.TemporaryRedirect,
                       Location                  = Location.Parse((URLPathPrefix + $"RNs/{RoamingNetwork.Id}/{PathBelowNetwork}").ToString() +
                                                                  (queryAt >= 0 ? requestTarget[queryAt..] : "")),
                       CacheControl              = "no-store",
                       AccessControlAllowOrigin  = "*"
                   }.AsImmutable;

        }

        #endregion

        #region (private) LocationsOf(ChargingStationOperator)

        /// <summary>
        /// Where the charging stations and EVSEs of the given version of a charging station operator are, calculated
        /// once per version. As its operator embeds them: a group gives a pool, station or EVSE properties, not a place.
        /// </summary>
        private Locations LocationsOf(ChargingStationOperator ChargingStationOperator)

            => locations.GetValue(ChargingStationOperator, chargingStationOperator => {

                   var locationsOfOperator = new Locations();

                   foreach (var chargingPool in chargingStationOperator.ChargingPools)
                       foreach (var chargingStation in chargingPool.ChargingStations)
                       {

                           locationsOfOperator.PoolsOfStations[chargingStation.Id] = chargingPool.Id;

                           foreach (var evse in chargingStation.EVSEs)
                               locationsOfOperator.PlacesOfEVSEs[evse.Id] = (chargingPool.Id, chargingStation.Id);

                       }

                   return locationsOfOperator;

               });

        #endregion

    }

}
