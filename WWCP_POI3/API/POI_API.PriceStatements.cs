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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The price statements of the histories of roaming networks over HTTP and the WebSocket (SPECIFICATION.md, section 12):
    /// a driver's app gets the price statements of a charging station with its proof, and checks them offline; a server of a
    /// charging station operator gives its statements to others; a witness gets the statements a checkpoint names.
    /// </summary>
    /// <remarks>
    /// GET  ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/priceStatements                  the current price statements of the pool,
    ///                                                                                        the station and its EVSEs
    /// GET  ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/priceStatements  those of the pool, the station and the EVSE
    /// GET  ~/RNs/{RN_Id}/CSs/{CS_Id}/priceStatements, ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/priceStatements  the same, found by the identification alone
    /// GET  ~/RNs/{RN_Id}/history/priceStatements                                                   the current price statement of every entity
    /// POST ~/RNs/{RN_Id}/history/priceStatements                                                   take price statements: { priceStatements: [ … ] }
    /// GET  ~/RNs/{RN_Id}/history/checkpoints/{checkpointId}/priceStatements                        those a checkpoint names, in the order of its tree
    /// GET  ~/RNs/{RN_Id}/history/checkpoints/{checkpointId}/priceStatements/{digest}/proof         the proof of one of them
    ///
    /// WebSocket actions: priceStatements { chargingStationId | EVSEId }?, addPriceStatements { priceStatements } answered with { taken, known, outdated, refused, refusals },
    /// checkpointPriceStatements { checkpointId }, priceStatementProof { checkpointId, digest };
    /// topic: priceStatements (action priceStatement), every price statement the history takes.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The topic of new price statements.
        /// </summary>
        public const String PriceStatementsTopic = "priceStatements";

        #endregion


        #region (private) RegisterPriceStatementURLTemplates()

        private void RegisterPriceStatementURLTemplates()
        {

            var roamingNetworkPath   = HTTPPath.Root + "RNs/{roamingNetworkId}";
            var chargingStationPath  = roamingNetworkPath + "/CSOs/{chargingStationOperatorId}/CPs/{chargingPoolId}/CSs/{chargingStationId}";

            #region GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/priceStatements

            AddEntityHandlers(chargingStationPath + "/priceStatements",
                              request => TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse)
                                             ? PriceStatementsResponse(request, chargingStation.Id, null)
                                             : errorResponse);

            #endregion

            #region GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/priceStatements

            AddEntityHandlers(chargingStationPath + "/EVSEs/{evseId}/priceStatements",
                              request => TryGetRequestedEVSE(request, out var evse, out var errorResponse)
                                             ? PriceStatementsResponse(request, null, evse.Id)
                                             : errorResponse);

            #endregion

            #region GET ~/RNs/{RN_Id}/CSs/{CS_Id}/priceStatements

            AddEntityHandlers(roamingNetworkPath + "/CSs/{id}/priceStatements",
                              request => ChargingStation_Id.TryParse(request.ParsedURLParameters.ElementAtOrDefault(1) ?? "", out var chargingStationId)
                                             ? PriceStatementsResponse(request, chargingStationId, null)
                                             : EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid charging station identification!"));

            #endregion

            #region GET ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/priceStatements

            AddEntityHandlers(roamingNetworkPath + "/EVSEs/{id}/priceStatements",
                              request => EVSE_Id.TryParse(request.ParsedURLParameters.ElementAtOrDefault(1) ?? "", out var evseId)
                                             ? PriceStatementsResponse(request, null, evseId)
                                             : EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid EVSE identification!"));

            #endregion

        }

        #endregion

        #region (private) RegisterHistoryPriceStatementURLTemplates(History)

        private void RegisterHistoryPriceStatementURLTemplates(HTTPPath History)
        {

            #region GET ~/RNs/{roamingNetworkId}/history/priceStatements

            AddHandler(History + "/priceStatements",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse
                               : JSONResponse(request, HTTPStatusCode.OK, PriceStatementsJSON(roamingNetworkHistory.PriceStatements))

                       ));

            #endregion

            #region POST ~/RNs/{roamingNetworkId}/history/priceStatements

            AddHandler(History + "/priceStatements",
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  request => Task.FromResult(SubmitPriceStatements(request)));

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}/priceStatements

            AddHandler(History + "/checkpoints/{checkpointId}/priceStatements",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetCheckpoint(request, out var roamingNetworkHistory, out var checkpoint, out var errorResponse)
                               ? errorResponse

                               : roamingNetworkHistory.PriceStatementsOf(checkpoint.Id) is IReadOnlyList<RoamingNetworkPriceStatement> priceStatements
                                     ? JSONResponse(request, HTTPStatusCode.OK, CheckpointPriceStatementsJSON(checkpoint.Id, priceStatements))
                                     : ErrorResponse(request,
                                                     HTTPStatusCode.NotFound,
                                                     checkpoint.PriceStatementsSize == 0
                                                         ? $"The checkpoint '{checkpoint.Id}' names no price statements!"
                                                         : $"The price statements of the checkpoint '{checkpoint.Id}' are not held here!")

                       ));

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}/priceStatements/{digest}/proof

            AddHandler(History + "/checkpoints/{checkpointId}/priceStatements/{digest}/proof",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetCheckpoint(request, out var roamingNetworkHistory, out var checkpoint, out var errorResponse)
                               ? errorResponse

                               : request.ParsedURLParameters.Length < 3
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid digest of a price statement!")

                                     : roamingNetworkHistory.TryGetPriceStatementProof(checkpoint.Id, request.ParsedURLParameters[2], out var proof, out var proofError)
                                           ? JSONResponse(request, HTTPStatusCode.OK, proof.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, proofError)

                       ));

            #endregion

        }

        #endregion


        #region (private) PriceStatementsResponse(Request, ChargingStationId, EVSEId)

        /// <summary>
        /// The current price statements of the given charging station, or of the given EVSE, of the requested roaming network.
        /// </summary>
        private HTTPResponse PriceStatementsResponse(HTTPRequest          Request,
                                                     ChargingStation_Id?  ChargingStationId,
                                                     EVSE_Id?             EVSEId)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse))
                return errorResponse;

            if (!roamingNetworkHistories.TryGetValue(roamingNetwork.Id, out var history))
                return EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The roaming network '{roamingNetwork.Id}' has no price statements here!");

            return EntityResponse(Request,
                                  PriceStatementsJSON(ChargingStationId.HasValue
                                                          ? history.PriceStatementsOf(ChargingStationId.Value)
                                                          : history.PriceStatementsOf(EVSEId!.Value)));

        }

        #endregion

        #region (private) SubmitPriceStatements(Request)

        /// <summary>
        /// Take the price statements in the body of the request, { priceStatements: [ … ] }, in their order. They need no
        /// authorization: each is signed by a signing key of the charging station operator of its entity.
        /// 200 OK:             all taken, known or out of date; { taken, known, outdated, refused, refusals }
        /// 400 Bad Request:    no valid price statements
        /// 422 Unprocessable:  at least one was refused, e.g. as it is not signed by its operator or does not follow the one
        ///                     before; the others are taken nevertheless, the answer tells which
        /// </summary>
        private HTTPResponse SubmitPriceStatements(HTTPRequest Request)
        {

            if (!TryGetHistory(Request, out var history, out var errorResponse))
                return errorResponse;

            if (!TryReadBody(Request, out var json) || json["priceStatements"] is not JArray priceStatementsJSON)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain { priceStatements: [ … ] }!");

            if (!TryParsePriceStatements(priceStatementsJSON, out var priceStatements, out var parseError))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, parseError);

            var answer = AddPriceStatements(history, priceStatements);

            return JSONResponse(Request,
                                answer["refused"]?.Value<UInt32>() > 0 ? HTTPStatusCode.UnprocessableEntity : HTTPStatusCode.OK,
                                answer);

        }

        #endregion

        #region AddPriceStatements(History, PriceStatements)

        /// <summary>
        /// Take the given price statements of another server, in their order, each checked against the head of the given
        /// history and following the current one of its entity; one refused does not keep the others from being taken.
        /// </summary>
        /// <param name="History">The history.</param>
        /// <param name="PriceStatements">The price statements.</param>
        /// <returns>{ taken, known, outdated, refused, refusals: [ { index, description }, … ] }</returns>
        public JObject AddPriceStatements(RoamingNetworkHistory                      History,
                                          IEnumerable<RoamingNetworkPriceStatement>  PriceStatements)
        {

            UInt32 taken = 0, known = 0, outdated = 0, index = 0;

            var refusals = new JArray();

            foreach (var priceStatement in PriceStatements)
            {

                switch (History.TryAddPriceStatement(priceStatement, out var errorResponse, TimeProvider.GetUtcNow()))
                {

                    case POIPriceStatementResult.Taken:     taken++;     break;
                    case POIPriceStatementResult.Known:     known++;     break;
                    case POIPriceStatementResult.Outdated:  outdated++;  break;

                    default:
                        refusals.Add(new JObject(
                                         new JProperty("index",        index),
                                         new JProperty("description",  errorResponse)
                                     ));
                        break;

                }

                index++;

            }

            return new JObject(
                       new JProperty("taken",     taken),
                       new JProperty("known",     known),
                       new JProperty("outdated",  outdated),
                       new JProperty("refused",   refusals.Count),
                       new JProperty("refusals",  refusals)
                   );

        }

        #endregion


        #region (private) AddPriceStatementCommands(WebSocketServer, History)

        /// <summary>
        /// The actions and the topic of price statements of the WebSocket of the given history.
        /// </summary>
        private void AddPriceStatementCommands(POIWebSocketServer     WebSocketServer,
                                               RoamingNetworkHistory  History)
        {

            WebSocketServer.AddCommand("priceStatements", request => {

                if (request.Payload["chargingStationId"] is JToken chargingStationIdJSON)
                    return Task.FromResult(
                               chargingStationIdJSON.Type != JTokenType.String || !ChargingStation_Id.TryParse(chargingStationIdJSON.Value<String>() ?? "", out var chargingStationId)
                                   ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Invalid charging station identification!")
                                   : request.Respond(PriceStatementsJSON(History.PriceStatementsOf(chargingStationId)))
                           );

                if (request.Payload["EVSEId"] is JToken evseIdJSON)
                    return Task.FromResult(
                               evseIdJSON.Type != JTokenType.String || !EVSE_Id.TryParse(evseIdJSON.Value<String>() ?? "", out var evseId)
                                   ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Invalid EVSE identification!")
                                   : request.Respond(PriceStatementsJSON(History.PriceStatementsOf(evseId)))
                           );

                return Task.FromResult(request.Respond(PriceStatementsJSON(History.PriceStatements)));

            });

            WebSocketServer.AddCommand("addPriceStatements", request => {

                if (request.Payload["priceStatements"] is not JArray priceStatementsJSON ||
                    !TryParsePriceStatements(priceStatementsJSON, out var priceStatements, out var parseError))
                {
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "The request must contain { priceStatements: [ … ] }!"));
                }

                return Task.FromResult(request.Respond(AddPriceStatements(History, priceStatements)));

            });

            WebSocketServer.AddCommand("checkpointPriceStatements", request =>

                Task.FromResult(

                    !RoamingNetworkCheckpoint_Id.TryParse(request.Payload["checkpointId"]?.Value<String>() ?? "", out var checkpointId)
                        ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid checkpoint identification!")

                        : History.PriceStatementsOf(checkpointId) is IReadOnlyList<RoamingNetworkPriceStatement> priceStatements
                              ? request.Respond(CheckpointPriceStatementsJSON(checkpointId, priceStatements))
                              : request.Error(POIWebSocketErrorCodes.NotFound, $"The price statements of the checkpoint '{checkpointId}' are not held here!")

                )

            );

            WebSocketServer.AddCommand("priceStatementProof", request =>

                Task.FromResult(

                    !RoamingNetworkCheckpoint_Id.TryParse(request.Payload["checkpointId"]?.Value<String>() ?? "", out var checkpointId) ||
                    request.Payload["digest"]?.Type != JTokenType.String
                        ? request.Error(POIWebSocketErrorCodes.FormatViolation, "The request must name a checkpointId and the digest of a price statement!")

                        : History.TryGetPriceStatementProof(checkpointId, request.Payload["digest"]!.Value<String>()!, out var proof, out var proofError)
                              ? request.Respond(proof.ToJSON())
                              : request.Error(POIWebSocketErrorCodes.NotFound, proofError)

                )

            );

            WebSocketServer.AddTopic(new POIWebSocketTopic(PriceStatementsTopic,
                                                           "priceStatement",
                                                           "Every price statement the history takes: a price status signed by the charging station operator of its entity."));

        }

        #endregion

        #region (private static) ConnectPriceStatements(History, WebSocketServer)

        /// <summary>
        /// Push the price statements of the given history to the subscribers; returns how to stop it.
        /// </summary>
        private static Action ConnectPriceStatements(RoamingNetworkHistory  History,
                                                     POIWebSocketServer     WebSocketServer)
        {

            OnRoamingNetworkPriceStatementDelegate onPriceStatement = priceStatement =>
                WebSocketServer.Push(PriceStatementsTopic, priceStatement.ToJSON());

            History.OnPriceStatement += onPriceStatement;

            return () => History.OnPriceStatement -= onPriceStatement;

        }

        #endregion


        #region (private static) PriceStatementsJSON(PriceStatements)

        /// <summary>
        /// { priceStatements: [ … ] }
        /// </summary>
        private static JObject PriceStatementsJSON(IEnumerable<RoamingNetworkPriceStatement> PriceStatements)

            => new (new JProperty("priceStatements", new JArray(PriceStatements.Select(priceStatement => priceStatement.ToJSON()))));

        #endregion

        #region (private static) CheckpointPriceStatementsJSON(CheckpointId, PriceStatements)

        /// <summary>
        /// { checkpointId, priceStatements: [ … ] }
        /// </summary>
        private static JObject CheckpointPriceStatementsJSON(RoamingNetworkCheckpoint_Id                CheckpointId,
                                                             IEnumerable<RoamingNetworkPriceStatement>  PriceStatements)

            => new (new JProperty("checkpointId",     CheckpointId.ToString()),
                    new JProperty("priceStatements",  new JArray(PriceStatements.Select(priceStatement => priceStatement.ToJSON()))));

        #endregion

        #region (private static) TryParsePriceStatements(JSON, out PriceStatements, out ErrorResponse)

        private static Boolean TryParsePriceStatements(JArray                                             JSON,
                                                       out IReadOnlyList<RoamingNetworkPriceStatement>    PriceStatements,
                                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            var priceStatements = new List<RoamingNetworkPriceStatement>();
            PriceStatements = priceStatements;

            foreach (var priceStatementJSON in JSON)
            {

                if (priceStatementJSON is not JObject priceStatementObject)
                {
                    ErrorResponse = $"Invalid price statement {priceStatements.Count}: it must be a JSON object!";
                    return false;
                }

                if (!RoamingNetworkPriceStatement.TryParse(priceStatementObject, out var priceStatement, out var priceStatementError))
                {
                    ErrorResponse = $"Invalid price statement {priceStatements.Count}: {priceStatementError}";
                    return false;
                }

                priceStatements.Add(priceStatement);

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) TryParseCheckpointPriceStatements(JSON, out PriceStatements, out ErrorResponse)

        /// <summary>
        /// The price statements of checkpoints given along with them: [ { checkpointId, priceStatements: [ … ] }, … ].
        /// </summary>
        private static Boolean TryParseCheckpointPriceStatements(JToken?                                                                                  JSON,
                                                                 out IReadOnlyDictionary<RoamingNetworkCheckpoint_Id, IReadOnlyList<RoamingNetworkPriceStatement>>  PriceStatements,
                                                                 [NotNullWhen(false)] out String?                                                         ErrorResponse)
        {

            var priceStatements = new Dictionary<RoamingNetworkCheckpoint_Id, IReadOnlyList<RoamingNetworkPriceStatement>>();
            PriceStatements = priceStatements;

            if (JSON is null)
            {
                ErrorResponse = null;
                return true;
            }

            if (JSON is not JArray array)
            {
                ErrorResponse = "The price statements of checkpoints must be [ { checkpointId, priceStatements: [ … ] }, … ]!";
                return false;
            }

            foreach (var entry in array)
            {

                if (entry is not JObject entryObject ||
                    !RoamingNetworkCheckpoint_Id.TryParse(entryObject["checkpointId"]?.Value<String>() ?? "", out var checkpointId) ||
                    entryObject["priceStatements"] is not JArray priceStatementsJSON)
                {
                    ErrorResponse = "The price statements of checkpoints must be [ { checkpointId, priceStatements: [ … ] }, … ]!";
                    return false;
                }

                if (!TryParsePriceStatements(priceStatementsJSON, out var ofCheckpoint, out ErrorResponse))
                    return false;

                priceStatements[checkpointId] = ofCheckpoint;

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
