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
    /// A delegate deciding whether the sender of the given HTTP request may submit the given change set.
    /// </summary>
    /// <param name="Request">The HTTP request.</param>
    /// <param name="ChangeSet">The submitted change set.</param>
    public delegate Boolean AuthorizeChangeSetSubmissionDelegate(HTTPRequest              Request,
                                                                 RoamingNetworkChangeSet  ChangeSet);


    /// <summary>
    /// The histories of roaming networks over HTTP, as JSON:
    /// what another server needs to start from a snapshot, to catch up with the commits since,
    /// and to submit its change sets to the leading server.
    /// </summary>
    /// <remarks>
    /// GET  ~/roamingNetworks/{roamingNetworkId}/history/head                                  the metadata of the head commit
    /// GET  ~/roamingNetworks/{roamingNetworkId}/history/snapshot                              the head commit and the complete roaming network
    /// GET  ~/roamingNetworks/{roamingNetworkId}/history/commitInfos?since={commitId}&amp;max={n}   the metadata of the commits since, without changes
    /// GET  ~/roamingNetworks/{roamingNetworkId}/history/commits?since={commitId}&amp;max={n}       the complete commits since
    /// GET  ~/roamingNetworks/{roamingNetworkId}/history/commits/{commitId}                    one complete commit
    /// POST ~/roamingNetworks/{roamingNetworkId}/history/changeSets?expectedHead={commitId}&amp;rebase=true
    ///                                                                                         publish a change set as the new head commit
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

        #region Properties

        /// <summary>
        /// Decides who may submit which change sets. Without it, submitting change sets is refused:
        /// a server has to decide on purpose whom it lets change its roaming networks.
        /// </summary>
        public AuthorizeChangeSetSubmissionDelegate?  AuthorizeChangeSetSubmission    { get; set; }

        /// <summary>
        /// Verifies the signatures of submitted change sets with trusted keys; without it, signed change sets are refused.
        /// </summary>
        public VerifyChangeSetSignatureDelegate?      VerifyChangeSetSignature        { get; set; }

        /// <summary>
        /// The limits for reading submitted change sets.
        /// </summary>
        public RoamingNetworkChangeSetLimits          ChangeSetLimits                 { get; set; } = RoamingNetworkChangeSetLimits.Default;

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

            #region POST ~/roamingNetworks/{roamingNetworkId}/history/changeSets?expectedHead={commitId}&rebase=true

            AddHandler(history + "/changeSets",
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  request => Task.FromResult(SubmitChangeSet(request)));

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


        #region (private) SubmitChangeSet(Request)

        /// <summary>
        /// Publish the change set in the body of the request as the new head commit.
        /// 201 Created:          published now; the metadata of the commit, and whether it was rebased
        /// 200 OK:               published before, also in a rebased version; the metadata of its commit
        /// 409 Conflict:         the head moved, or the change set does not fit it; the metadata of the head
        /// 422 Unprocessable:    refused, e.g. as it conflicts or a signature is invalid
        /// 400 Bad Request:      no valid change set, or one for another roaming network
        /// 403 Forbidden:        submitting is not enabled, or not allowed for the sender
        /// </summary>
        private HTTPResponse SubmitChangeSet(HTTPRequest Request)
        {

            if (!TryGetHistory(Request, out var history, out var errorResponse))
                return errorResponse;

            if (AuthorizeChangeSetSubmission is null)
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "Submitting change sets is not enabled here!");

            #region Parse the change set and the options

            var text = Request.HTTPBodyAsUTF8String;

            if (String.IsNullOrWhiteSpace(text))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain a change set!");

            if (!RoamingNetworkChangeSet.TryParse(text, out var changeSet, out var changeSetError, ChangeSetLimits))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid change set: " + changeSetError);

            if (changeSet.RoamingNetworkId != history.RoamingNetworkId)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, $"The change set is for the roaming network '{changeSet.RoamingNetworkId}', not for '{history.RoamingNetworkId}'!");

            RoamingNetworkCommit_Id? expectedHead = null;

            if (Request.QueryString.GetString("expectedHead") is String expectedHeadText)
            {

                if (!RoamingNetworkCommit_Id.TryParse(expectedHeadText, out var parsedExpectedHead))
                    return ErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid commit identification '{expectedHeadText}'!");

                expectedHead = parsedExpectedHead;

            }

            var rebaseText = Request.QueryString.GetString("rebase");

            if (rebaseText is not null && rebaseText is not ("true" or "false"))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid rebase option '{rebaseText}'; use true or false!");

            #endregion

            if (!AuthorizeChangeSetSubmission(Request, changeSet))
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "The sender may not submit this change set!");

            var result = history.Publish(changeSet,
                                         expectedHead,
                                         rebaseText == "true",
                                         VerifyChangeSetSignature);

            return result.Status switch {

                       RoamingNetworkPublishStatus.Published         => JSONResponse(Request,
                                                                                     HTTPStatusCode.Created,
                                                                                     PublishedJSON(result)),

                       RoamingNetworkPublishStatus.AlreadyPublished  => JSONResponse(Request,
                                                                                     HTTPStatusCode.OK,
                                                                                     PublishedJSON(result)),

                       RoamingNetworkPublishStatus.HeadMoved         => JSONResponse(Request,
                                                                                     HTTPStatusCode.Conflict,
                                                                                     new JObject(
                                                                                         new JProperty("description",  result.ErrorResponse),
                                                                                         new JProperty("head",         new RoamingNetworkCommitInfo(result.Head.Commit).ToJSON())
                                                                                     )),

                       _                                             => ErrorResponse(Request,
                                                                                      HTTPStatusCode.UnprocessableEntity,
                                                                                      result.ErrorResponse)

                   };

        }

        #endregion

        #region (private static) PublishedJSON(Result)

        /// <summary>
        /// The metadata of the commit of a published change set, and whether it was rebased.
        /// </summary>
        private static JObject PublishedJSON(RoamingNetworkPublishResult Result)
        {

            var json = new RoamingNetworkCommitInfo(Result.Commit!).ToJSON();

            json.Add("status",   Result.Status.ToString());
            json.Add("rebased",  Result.Rebased);

            return json;

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
