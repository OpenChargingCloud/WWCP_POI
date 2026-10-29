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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The epochs of the histories of roaming networks over HTTP and the WebSocket (SPECIFICATION.md, section 11): the
    /// history served is the one of the current epoch; an epoch closed stays readable, its checkpoints and commits proved.
    /// </summary>
    /// <remarks>
    /// GET ~/RNs/{RN_Id}/history/epoch                                               the current epoch: { epoch, profiles, status, previous?, finalCheckpointId? }
    /// GET ~/RNs/{RN_Id}/history/epochs                                              every epoch served here, in their order
    /// GET ~/RNs/{RN_Id}/history/epochs/{epoch}/checkpoints[/{checkpointId}]         the checkpoints of an epoch, or one of them
    /// GET ~/RNs/{RN_Id}/history/epochs/{epoch}/commits/{commitId}[/proof]           a commit of an epoch, or its proof in a checkpoint
    ///
    /// WebSocket action: epoch {} answered as GET …/epoch.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The histories of epochs closed, by their roaming networks and numbers.
        /// </summary>
        private readonly ConcurrentDictionary<(RoamingNetwork_Id RoamingNetworkId, UInt32 Epoch), RoamingNetworkHistory> closedEpochs = [];

        private readonly Lock epochLock = new ();

        #endregion


        #region TryServeNextEpoch(NextEpoch, out ErrorResponse)

        /// <summary>
        /// Serve the history of the next epoch of a roaming network in place of the one of its current epoch, which its final
        /// checkpoint closed (SPECIFICATION.md, section 11): over HTTP, and over a WebSocket of its own; the connections to
        /// the one before are closed, so that peers connect again and move to the next epoch. The epoch before stays readable.
        /// </summary>
        /// <param name="NextEpoch">The history of the next epoch, started from the final checkpoint of the current one.</param>
        /// <param name="ErrorResponse">Why it is not served.</param>
        public Boolean TryServeNextEpoch(RoamingNetworkHistory             NextEpoch,
                                         [NotNullWhen(false)] out String?  ErrorResponse)
        {

            var roamingNetworkId = NextEpoch.RoamingNetworkId;

            lock (epochLock)
            {

                if (!roamingNetworkHistories.TryGetValue(roamingNetworkId, out var current))
                {
                    ErrorResponse = $"The roaming network '{roamingNetworkId}' is not served here!";
                    return false;
                }

                if (NextEpoch.Epoch.Number != current.Epoch.Number + 1 ||
                    current.FinalCheckpoint is not RoamingNetworkCheckpoint finalCheckpoint ||
                    NextEpoch.PreviousEpoch?.CheckpointId != finalCheckpoint.Id)
                {
                    ErrorResponse = $"The history is no next epoch of the epoch {current.Epoch.Number} of the roaming network '{roamingNetworkId}' served here, started from its final checkpoint!";
                    return false;
                }

                var webSocketServer = CreateHistoryWebSocketServer(NextEpoch);

                if (historyDisconnects.TryRemove(roamingNetworkId, out var disconnect))
                    disconnect();

                historyWebSocketServers.TryRemove(roamingNetworkId, out var closedWebSocketServer);

                closedEpochs[(roamingNetworkId, current.Epoch.Number)]  = current;
                historyWebSocketServers[roamingNetworkId]               = webSocketServer;
                roamingNetworkHistories[roamingNetworkId]               = NextEpoch;

                ConnectHistoryWebSocketServer(NextEpoch, webSocketServer);

                // Its peers connect again, to the next epoch.
                closedWebSocketServer?.DisposeAsync().AsTask().GetAwaiter().GetResult();

                ErrorResponse = null;
                return true;

            }

        }

        #endregion

        #region EpochsOf(RoamingNetworkId)

        /// <summary>
        /// The histories of every epoch of the given roaming network served here, in their order, the current one last.
        /// </summary>
        /// <param name="RoamingNetworkId">The identification of a roaming network.</param>
        public IReadOnlyList<RoamingNetworkHistory> EpochsOf(RoamingNetwork_Id RoamingNetworkId)
        {

            var epochs = closedEpochs.Where (closed => closed.Key.RoamingNetworkId == RoamingNetworkId).
                                      OrderBy(closed => closed.Key.Epoch).
                                      Select (closed => closed.Value).
                                      ToList();

            if (roamingNetworkHistories.TryGetValue(RoamingNetworkId, out var current))
                epochs.Add(current);

            return epochs;

        }

        #endregion


        #region (private) RegisterEpochURLTemplates(History)

        private void RegisterEpochURLTemplates(HTTPPath History)
        {

            #region GET ~/RNs/{roamingNetworkId}/history/epoch

            AddHandler(History + "/epoch",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, EpochJSON(roamingNetworkHistory))
                               : errorResponse

                       ));

            AddReadPreflight(History + "/epoch");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/epochs

            AddHandler(History + "/epochs",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, new JArray(EpochsOf(roamingNetworkHistory.RoamingNetworkId).Select(EpochJSON)))
                               : errorResponse

                       ));

            AddReadPreflight(History + "/epochs");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/epochs/{epoch}/checkpoints

            AddHandler(History + "/epochs/{epoch}/checkpoints",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetEpoch(request, out var epochHistory, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, new JArray(epochHistory.Checkpoints.Select(checkpoint => checkpoint.ToJSON(Embedded: true))))
                               : errorResponse

                       ));

            AddReadPreflight(History + "/epochs/{epoch}/checkpoints");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/epochs/{epoch}/checkpoints/{checkpointId}

            AddHandler(History + "/epochs/{epoch}/checkpoints/{checkpointId}",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetEpoch(request, out var epochHistory, out var errorResponse)
                               ? errorResponse

                               : request.ParsedURLParameters.Length < 3 ||
                                 !RoamingNetworkCheckpoint_Id.TryParse(request.ParsedURLParameters[2], out var checkpointId)
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid checkpoint identification!")

                                     : epochHistory.TryGetCheckpoint(checkpointId, out var checkpoint)
                                           ? JSONResponse(request, HTTPStatusCode.OK, checkpoint.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, $"The checkpoint '{checkpointId}' is unknown in the epoch {epochHistory.Epoch.Number}!")

                       ));

            AddReadPreflight(History + "/epochs/{epoch}/checkpoints/{checkpointId}");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/epochs/{epoch}/commits/{commitId}

            AddHandler(History + "/epochs/{epoch}/commits/{commitId}",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetEpoch(request, out var epochHistory, out var errorResponse)
                               ? errorResponse

                               : request.ParsedURLParameters.Length < 3 ||
                                 !RoamingNetworkCommit_Id.TryParse(request.ParsedURLParameters[2], out var commitId)
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid commit identification!")

                                     : epochHistory.TryGetCommit(commitId, out var commit) || epochHistory.TryGetArchivedCommit(commitId, out commit)
                                           ? JSONResponse(request, HTTPStatusCode.OK, commit.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, $"The commit '{commitId}' is unknown in the epoch {epochHistory.Epoch.Number}!")

                       ));

            AddReadPreflight(History + "/epochs/{epoch}/commits/{commitId}");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/epochs/{epoch}/commits/{commitId}/proof

            AddHandler(History + "/epochs/{epoch}/commits/{commitId}/proof",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetEpoch(request, out var epochHistory, out var errorResponse)
                               ? errorResponse

                               : request.ParsedURLParameters.Length < 3 ||
                                 !RoamingNetworkCommit_Id.TryParse(request.ParsedURLParameters[2], out var commitId)
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid commit identification!")

                                     : epochHistory.TryGetCommitProof(commitId, out var proof, out var proofError)
                                           ? JSONResponse(request, HTTPStatusCode.OK, proof.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, proofError)

                       ));

            AddReadPreflight(History + "/epochs/{epoch}/commits/{commitId}/proof");

            #endregion

        }

        #endregion

        #region (private) AddEpochCommands(WebSocketServer, History)

        /// <summary>
        /// The epoch action of the WebSocket of the given history.
        /// </summary>
        private static void AddEpochCommands(POIWebSocketServer     WebSocketServer,
                                             RoamingNetworkHistory  History)
        {

            WebSocketServer.AddCommand("epoch", request =>
                Task.FromResult(request.Respond(EpochJSON(History))));

        }

        #endregion


        #region (private static) EpochJSON(History)

        /// <summary>
        /// The epoch of the given history: { epoch, profiles, status, previous?, finalCheckpointId? }.
        /// </summary>
        private static JObject EpochJSON(RoamingNetworkHistory History)
        {

            var json = History.Epoch.ToJSON(Embedded: true);

            json.Add(PropertyKeys.Status, History.EpochStatus switch {
                                   RoamingNetworkEpochStatus.Closing  => "closing",
                                   RoamingNetworkEpochStatus.Closed   => "closed",
                                   _                                  => "active"
                               });

            if (History.PreviousEpoch is RoamingNetworkEpochLink previousEpoch)
                json.Add(RoamingNetworkEpochLink.JSONProperty, previousEpoch.ToJSON(Embedded: true));

            if (History.FinalCheckpoint is RoamingNetworkCheckpoint finalCheckpoint)
                json.Add(PropertyKeys.FinalCheckpointId, finalCheckpoint.Id.ToString());

            return json;

        }

        #endregion

        #region (private) TryGetEpoch(Request, out History, out ErrorResponse)

        /// <summary>
        /// The history of the epoch named in the path of the request, the current one or one closed, or an error response.
        /// </summary>
        private Boolean TryGetEpoch(HTTPRequest                                      Request,
                                    [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                    [NotNullWhen(false)] out HTTPResponse?           ErrorResponse)
        {

            History = null;

            if (!TryGetHistory(Request, out var current, out ErrorResponse))
                return false;

            if (Request.ParsedURLParameters.Length < 2 ||
                !UInt32.TryParse(Request.ParsedURLParameters[1], out var number))
            {
                ErrorResponse = this.ErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid epoch!");
                return false;
            }

            if (number == current.Epoch.Number)
                History = current;

            else if (!closedEpochs.TryGetValue((current.RoamingNetworkId, number), out History))
            {
                ErrorResponse = this.ErrorResponse(Request, HTTPStatusCode.NotFound, $"The roaming network '{current.RoamingNetworkId}' has no epoch {number} here!");
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
