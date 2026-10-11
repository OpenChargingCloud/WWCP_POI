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
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The histories of roaming networks over HTTP, read only, as JSON:
    /// what another server needs to start from a snapshot and to catch up with the commits since.
    /// </summary>
    /// <remarks>
    /// GET ~/roamingNetworks/{roamingNetworkId}/history/head                                  the metadata of the head commit
    /// GET ~/roamingNetworks/{roamingNetworkId}/history/snapshot                              the head commit and the complete roaming network
    /// GET ~/roamingNetworks/{roamingNetworkId}/history/commitInfos?since={commitId}&amp;max={n}   the metadata of the commits since, without changes
    /// GET ~/roamingNetworks/{roamingNetworkId}/history/commits?since={commitId}&amp;max={n}       the complete commits since
    /// GET ~/roamingNetworks/{roamingNetworkId}/history/commits/{commitId}                    one complete commit
    /// Without "since", the commits since the root are returned. "max" is at most MaxCommitsPerRequest.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The maximum number of commits returned by one request.
        /// </summary>
        public const Int32 MaxCommitsPerRequest = RoamingNetworkHistory.DefaultMaxCommits;

        private readonly ConcurrentDictionary<RoamingNetwork_Id, RoamingNetworkHistory> roamingNetworkHistories = [];

        #endregion


        #region AddRoamingNetworkHistory   (History)

        /// <summary>
        /// Serve the given history of a roaming network.
        /// </summary>
        /// <param name="History">The history of a roaming network.</param>
        public Boolean AddRoamingNetworkHistory(RoamingNetworkHistory History)

            => roamingNetworkHistories.TryAdd(History.RoamingNetworkId, History);

        #endregion

        #region TryGetRoamingNetworkHistory(RoamingNetworkId, out History)

        /// <summary>
        /// Try to return the served history of the given roaming network.
        /// </summary>
        /// <param name="RoamingNetworkId">The identification of a roaming network.</param>
        /// <param name="History">Its history.</param>
        public Boolean TryGetRoamingNetworkHistory(RoamingNetwork_Id                               RoamingNetworkId,
                                                   [NotNullWhen(true)] out RoamingNetworkHistory?  History)

            => roamingNetworkHistories.TryGetValue(RoamingNetworkId, out History);

        #endregion


        #region (private) RegisterHistoryURLTemplates()

        private void RegisterHistoryURLTemplates()
        {

            var history = HTTPPath.Root + "roamingNetworks/{roamingNetworkId}/history";

            #region GET ~/roamingNetworks/{roamingNetworkId}/history/head

            AddHandler(history + "/head",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, new RoamingNetworkCommitInfo(roamingNetworkHistory.Head.Commit).ToJSON())
                               : errorResponse

                       ));

            #endregion

            #region GET ~/roamingNetworks/{roamingNetworkId}/history/snapshot

            AddHandler(history + "/snapshot",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, roamingNetworkHistory.ToSnapshotJSON())
                               : errorResponse

                       ));

            #endregion

            #region GET ~/roamingNetworks/{roamingNetworkId}/history/commitInfos?since={commitId}&max={n}

            AddHandler(history + "/commitInfos",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : !TryGetSince(request, roamingNetworkHistory, out var since, out var max, out errorResponse)
                                     ? errorResponse

                                     : roamingNetworkHistory.TryGetCommitInfosSince(since, out var commitInfos, out var error, max)
                                           ? JSONResponse(request, HTTPStatusCode.OK, new JArray(commitInfos.Select(commitInfo => commitInfo.ToJSON())))
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, error)

                       ));

            #endregion

            #region GET ~/roamingNetworks/{roamingNetworkId}/history/commits?since={commitId}&max={n}

            AddHandler(history + "/commits",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : !TryGetSince(request, roamingNetworkHistory, out var since, out var max, out errorResponse)
                                     ? errorResponse

                                     : roamingNetworkHistory.TryGetCommitsSince(since, out var commits, out var error, max)
                                           ? JSONResponse(request, HTTPStatusCode.OK, new JArray(commits.Select(commit => commit.ToJSON())))
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, error)

                       ));

            #endregion

            #region GET ~/roamingNetworks/{roamingNetworkId}/history/commits/{commitId}

            AddHandler(history + "/commits/{commitId}",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : request.ParsedURLParameters.Length < 2 ||
                                 !RoamingNetworkCommit_Id.TryParse(request.ParsedURLParameters[1], out var commitId)
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid commit identification!")

                                     : roamingNetworkHistory.TryGetCommit(commitId, out var commit)
                                           ? JSONResponse(request, HTTPStatusCode.OK, commit.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, $"The commit '{commitId}' is unknown here!")

                       ));

            #endregion

        }

        #endregion


        #region (private) TryGetHistory(Request, out History, out ErrorResponse)

        /// <summary>
        /// The history of the roaming network named in the path of the request, or an error response.
        /// </summary>
        private Boolean TryGetHistory(HTTPRequest                                      Request,
                                      [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                      [NotNullWhen(false)] out HTTPResponse?           ErrorResponse)
        {

            History = null;

            if (Request.ParsedURLParameters.Length < 1 ||
                !RoamingNetwork_Id.TryParse(Request.ParsedURLParameters[0], out var roamingNetworkId))
            {
                ErrorResponse = this.ErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid roaming network identification!");
                return false;
            }

            if (!roamingNetworkHistories.TryGetValue(roamingNetworkId, out History))
            {
                ErrorResponse = this.ErrorResponse(Request, HTTPStatusCode.NotFound, $"The roaming network '{roamingNetworkId}' has no history here!");
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private) TryGetSince  (Request, History, out Since, out Max, out ErrorResponse)

        /// <summary>
        /// The "since" commit, by default the root, and the "max" number of commits of the request, or an error response.
        /// </summary>
        private Boolean TryGetSince(HTTPRequest                            Request,
                                    RoamingNetworkHistory                  History,
                                    out RoamingNetworkCommit_Id            Since,
                                    out Int32                              Max,
                                    [NotNullWhen(false)] out HTTPResponse? ErrorResponse)
        {

            Since  = History.Root.Id;
            Max    = MaxCommitsPerRequest;

            var since = Request.QueryString.GetString("since");

            if (since is not null && !RoamingNetworkCommit_Id.TryParse(since, out Since))
            {
                ErrorResponse = this.ErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid commit identification '{since}'!");
                return false;
            }

            var max = Request.QueryString.GetString("max");

            if (max is not null)
            {

                if (!Int32.TryParse(max, out Max) || Max < 1)
                {
                    ErrorResponse = this.ErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid maximum number of commits '{max}'!");
                    return false;
                }

                Max = Math.Min(Max, MaxCommitsPerRequest);

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) JSONResponse (Request, StatusCode, JSON)

        /// <summary>
        /// An answer in JSON, never cached.
        /// </summary>
        private static HTTPResponse JSONResponse(HTTPRequest     Request,
                                                 HTTPStatusCode  StatusCode,
                                                 JToken          JSON)

            => new HTTPResponse.Builder(Request) {
                   HTTPStatusCode  = StatusCode,
                   ContentType     = HTTPContentType.Application.JSON_UTF8,
                   Content         = Encoding.UTF8.GetBytes(JSON.ToString(Formatting.None)),
                   CacheControl    = "no-store"
               }.AsImmutable;

        #endregion

        #region (private)        ErrorResponse(Request, StatusCode, Description)

        /// <summary>
        /// An error in JSON: { "description": "…" }.
        /// </summary>
        private HTTPResponse ErrorResponse(HTTPRequest     Request,
                                           HTTPStatusCode  StatusCode,
                                           String?         Description)

            => JSONResponse(Request,
                            StatusCode,
                            new JObject(new JProperty("description", Description ?? StatusCode.ToString())));

        #endregion

    }

}
