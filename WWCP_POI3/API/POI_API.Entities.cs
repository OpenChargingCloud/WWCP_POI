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
    /// GET ~/RNs/{RN_Id}                                                          the roaming network
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}                                            a charging station operator
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}                                a charging pool
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}                    a charging station
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}    an EVSE
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}                                              a grid operator
    /// GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}                                a grid connection point of the grid operator
    /// GET ~/RNs/{RN_Id}/EMPs/{EMP_Id}                                            an e-mobility provider
    ///
    /// The roaming network is the head of its history, when served with one, else the one added.
    /// An entity is the representation its roaming network embeds, so its ETag is the one of that
    /// part of the roaming network: SHA-256 over its canonical JSON or canonical CBOR, which is also
    /// the content of the response. The roaming network carries its own ETags, calculated without them.
    /// The HTTP ETag is the POI ETag of the format of the response, e.g. "json:sha256:hex:…";
    /// an If-None-Match naming it is answered with 304 Not Modified.
    /// HEAD is answered by Hermod from GET; OPTIONS answers the CORS preflights of every origin.
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

        #endregion


        #region (private) RegisterEntityURLTemplates()

        private void RegisterEntityURLTemplates()
        {

            var roamingNetworkPath           = HTTPPath.Root + "RNs/{roamingNetworkId}";
            var chargingStationOperatorPath  = roamingNetworkPath          + "/CSOs/{chargingStationOperatorId}";
            var chargingPoolPath             = chargingStationOperatorPath + "/CPs/{chargingPoolId}";
            var chargingStationPath          = chargingPoolPath            + "/CSs/{chargingStationId}";
            var gridOperatorPath             = roamingNetworkPath          + "/GOs/{gridOperatorId}";

            #region ~/RNs/{RN_Id}

            AddEntityHandlers(roamingNetworkPath,
                              request => TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse)
                                             ? RoamingNetworkResponse(request, roamingNetwork)
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}

            AddEntityHandlers(chargingStationOperatorPath,
                              request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse)
                                             ? EntityResponse(request, chargingStationOperator.ToEmbeddedJSON())
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}

            AddEntityHandlers(chargingPoolPath,
                              request => TryGetRequestedChargingPool(request, out var chargingPool, out var errorResponse)
                                             ? EntityResponse(request, chargingPool.ToJSON())
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}

            AddEntityHandlers(chargingStationPath,
                              request => TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse)
                                             ? EntityResponse(request, chargingStation.ToJSON())
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}

            AddEntityHandlers(chargingStationPath + "/EVSEs/{evseId}",
                              request => {

                                  if (!TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse))
                                      return errorResponse;

                                  if (!EVSE_Id.TryParse(request.ParsedURLParameters[4], out var evseId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid EVSE identification!");

                                  return chargingStation.TryGetEVSE(evseId, out var evse)
                                             ? EntityResponse     (request, evse.ToJSON())
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The EVSE '{evseId}' is unknown at the charging station '{chargingStation.Id}'!");

                              });

            #endregion

            #region ~/RNs/{RN_Id}/GOs/{GO_Id}

            AddEntityHandlers(gridOperatorPath,
                              request => TryGetRequestedGridOperator(request, out _, out var gridOperator, out var errorResponse)
                                             ? EntityResponse(request, gridOperator.ToJSON())
                                             : errorResponse);

            #endregion

            #region ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}

            // The charging pools embed the grid connection points, naming their grid operators.
            AddEntityHandlers(gridOperatorPath + "/GCPs/{gridConnectionPointId}",
                              request => {

                                  if (!TryGetRequestedGridOperator(request, out var roamingNetwork, out var gridOperator, out var errorResponse))
                                      return errorResponse;

                                  if (!GridConnectionPoint_Id.TryParse(request.ParsedURLParameters[2], out var gridConnectionPointId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid grid connection point identification!");

                                  var gridConnectionPoint = roamingNetwork.ChargingStationOperators.
                                                                OrderBy   (chargingStationOperator => chargingStationOperator.Id.ToString(), StringComparer.Ordinal).
                                                                SelectMany(chargingStationOperator => chargingStationOperator.ChargingPools).
                                                                Select    (chargingPool            => chargingPool.GridConnectionPoint).
                                                                FirstOrDefault(point => point is not null &&
                                                                                        point.Id.            Equals(gridConnectionPointId) &&
                                                                                        point.GridOperatorId.Equals(gridOperator.Id));

                                  return gridConnectionPoint is not null
                                             ? EntityResponse     (request, gridConnectionPoint.ToJSON())
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
                                             ? EntityResponse     (request, eMobilityProvider.ToJSON())
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The e-mobility provider '{eMobilityProviderId}' is unknown in the roaming network '{roamingNetwork.Id}'!");

                              });

            #endregion

        }

        #endregion

        #region (private) AddEntityHandlers(Path, GET)

        /// <summary>
        /// Answer GET with the given delegate, HEAD by Hermod from it, and OPTIONS with the methods
        /// and the CORS permissions of every origin.
        /// </summary>
        private void AddEntityHandlers(HTTPPath                         Path,
                                       Func<HTTPRequest, HTTPResponse>  GET)
        {

            AddHandler(Path,
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(GET(request)));

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


        #region (private static) RoamingNetworkResponse(Request, RoamingNetwork)

        /// <summary>
        /// The roaming network with its ETags, in JSON or in CBOR as the request prefers.
        /// The HTTP ETag is its ETag of that format, calculated without ETags.
        /// </summary>
        private static HTTPResponse RoamingNetworkResponse(HTTPRequest     Request,
                                                           RoamingNetwork  RoamingNetwork)
        {

            var cbor    = WantsCBOR(Request);
            var format  = cbor ? ETagFormat.CBOR : ETagFormat.JSON;
            var stored  = (RoamingNetwork.ETags.IsDefault ? [] : RoamingNetwork.ETags).Where(eTag => eTag.Format == format).ToArray();

            var eTag    = stored.Length > 0
                              ? stored[0]
                              : ETag.Compute(format,
                                             cbor
                                                 ? RoamingNetwork.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical)
                                                 : CanonicalJSON.ToUTF8Bytes(RoamingNetwork.ToJSON(IncludeETags: false)));

            return EntityResponse(Request,
                                  cbor,
                                  cbor
                                      ? RoamingNetwork.ToCBOR().ToByteArray(CBORWriterOptions.Canonical)
                                      : CanonicalJSON.ToUTF8Bytes(RoamingNetwork.ToJSON()),
                                  eTag);

        }

        #endregion

        #region (private static) EntityResponse        (Request, JSON)

        /// <summary>
        /// The given entity in canonical JSON, or in canonical CBOR with the keys and values of the JSON
        /// when the request prefers it. The HTTP ETag is the SHA-256 of the content.
        /// </summary>
        private static HTTPResponse EntityResponse(HTTPRequest  Request,
                                                   JObject      JSON)
        {

            var cbor     = WantsCBOR(Request);

            var content  = cbor
                               ? CBORJSON.ToCBOR(JSON).ToByteArray(CBORWriterOptions.Canonical)
                               : CanonicalJSON.ToUTF8Bytes(JSON);

            return EntityResponse(Request,
                                  cbor,
                                  content,
                                  ETag.Compute(cbor ? ETagFormat.CBOR : ETagFormat.JSON, content));

        }

        #endregion

        #region (private static) EntityResponse        (Request, CBOR, Content, ETag)

        /// <summary>
        /// 200 OK with the given content and its ETag, or 304 Not Modified when the If-None-Match
        /// of the request names the ETag. Caches revalidate it; every origin may read it and its ETag.
        /// </summary>
        private static HTTPResponse EntityResponse(HTTPRequest  Request,
                                                   Boolean      CBOR,
                                                   Byte[]       Content,
                                                   ETag         ETag)
        {

            var eTag         = $"\"{ETag.ToString(ETagDigestEncoding.HEX)}\"";
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
                response.ContentType  = CBOR
                                            ? HTTPContentType.Application.CBOR
                                            : HTTPContentType.Application.JSON_UTF8;
                response.Content      = Content;
            }

            // Not a CORS-safelisted response header: scripts of other origins see it only when exposed.
            response.Set("Access-Control-Expose-Headers", "ETag");

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
