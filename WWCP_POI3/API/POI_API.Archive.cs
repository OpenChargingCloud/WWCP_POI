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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The archives of the histories of roaming networks over HTTP and the WebSocket (SPECIFICATION.md, section 10.6):
    /// the commits dropped when compacting, one segment per compaction. GET ~/RNs/{id}/history/commits/{commitId}
    /// serves archived commits as well, and the proofs of commits prove archived ones.
    /// </summary>
    /// <remarks>
    /// GET    ~/RNs/{roamingNetworkId}/history/archive             what the segments hold, without their commit identifications
    /// GET    ~/RNs/{roamingNetworkId}/history/archive/{number}    one segment completely: { info, base, commits }
    ///
    /// WebSocket actions: archive {}, archiveSegment { number }.
    /// </remarks>
    public partial class POI_API
    {

        #region (private) RegisterArchiveURLTemplates(History)

        private void RegisterArchiveURLTemplates(HTTPPath History)
        {

            #region GET ~/RNs/{roamingNetworkId}/history/archive

            AddHandler(History + "/archive",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse
                               : JSONResponse(request, HTTPStatusCode.OK, ArchiveJSON(roamingNetworkHistory))

                       ));

            AddReadPreflight(History + "/archive");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/archive/{number}

            AddHandler(History + "/archive/{number}",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  HeavyRead(request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : request.ParsedURLParameters.Length < 2 ||
                                 !UInt32.TryParse(request.ParsedURLParameters[1], out var number)
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid segment number!")

                                     : roamingNetworkHistory.TryGetArchiveSegment(number, out var segment)
                                           ? JSONResponse(request, HTTPStatusCode.OK, segment.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, $"The archive has no segment {number} here!")

                       )));

            AddReadPreflight(History + "/archive/{number}");

            #endregion

        }

        #endregion

        #region (private) AddArchiveCommands(WebSocketServer, History)

        private void AddArchiveCommands(POIWebSocketServer     WebSocketServer,
                                        RoamingNetworkHistory  History)
        {

            WebSocketServer.AddCommand("archive", request =>

                Task.FromResult(
                    request.Respond(ArchiveJSON(History))
                )

            );

            WebSocketServer.AddCommand("archiveSegment", HeavyCommand(request =>

                Task.FromResult(

                    request.Payload[PropertyKeys.Number] is not JValue { Type: JTokenType.Integer } number || number.Value<Int64>() is < 1 or > UInt32.MaxValue
                        ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid segment number!")

                        : History.TryGetArchiveSegment(number.Value<UInt32>(), out var segment)
                              ? request.Respond(segment.ToJSON())
                              : request.Error(POIWebSocketErrorCodes.NotFound, $"The archive has no segment {number} here!")

                )

            ));

        }

        #endregion

        #region (private static) ArchiveJSON(History)

        /// <summary>
        /// { segments: [ { number, rootId, endId, fromRevision, toRevision, size, archivedAt } ] }
        /// </summary>
        private static JObject ArchiveJSON(RoamingNetworkHistory History)

            => new (
                   new JProperty(PropertyKeys.Segments, new JArray(History.ArchiveSegments.Select(info => info.ToJSON(Embedded: true, IncludeCommitIds: false))))
               );

        #endregion

    }

}
