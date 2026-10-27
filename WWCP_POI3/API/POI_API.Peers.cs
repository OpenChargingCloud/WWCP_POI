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
    /// A delegate deciding whether the sender of the given HTTP request may push the given commits, e.g. another server.
    /// </summary>
    /// <param name="Request">The HTTP request, or the WebSocket upgrade request of the sender.</param>
    /// <param name="Commits">The pushed commits.</param>
    public delegate Boolean AuthorizeCommitPushDelegate(HTTPRequest                          Request,
                                                        IReadOnlyList<RoamingNetworkCommit>  Commits);


    /// <summary>
    /// What servers without a leader exchange: their tips, the commits they hold, and the commits the other one misses.
    /// </summary>
    public partial class POI_API
    {

        #region Properties

        /// <summary>
        /// Decides who may push commits, e.g. another server. Without it, pushing is refused.
        /// Pushed commits are checked as every appended commit: signed by their owners, or the merge of their parents.
        /// </summary>
        public AuthorizeCommitPushDelegate?   AuthorizeCommitPush             { get; set; }

        /// <summary>
        /// The roles every pushed commit must be signed in, if any.
        /// </summary>
        public IReadOnlyCollection<String>?   RequiredCommitSignatureRoles    { get; set; }

        /// <summary>
        /// The maximum number of commits a request may name as held, "have", over HTTP and the WebSocket;
        /// RoamingNetworkHistory.DefaultMaxHaveIds by default, as many as HaveIds() names at most. More are refused.
        /// </summary>
        public Int32                          MaxHaveIds                      { get; set; } = RoamingNetworkHistory.DefaultMaxHaveIds;

        #endregion


        #region (private) RegisterPeerURLTemplates()

        private void RegisterPeerURLTemplates()
        {

            var history = HTTPPath.Root + "RNs/{roamingNetworkId}/history";

            #region GET  ~/RNs/{roamingNetworkId}/history/tips

            AddHandler(history + "/tips",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, TipsJSON(roamingNetworkHistory))
                               : errorResponse

                       ));

            #endregion

            #region GET  ~/RNs/{roamingNetworkId}/history/haveIds

            AddHandler(history + "/haveIds",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, HaveIdsJSON(roamingNetworkHistory))
                               : errorResponse

                       ));

            #endregion

            #region POST ~/RNs/{roamingNetworkId}/history/commits

            AddHandler(history + "/commits",
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  request => Task.FromResult(SubmitCommits(request)));

            #endregion

        }

        #endregion

        #region (private) SubmitCommits(Request)

        /// <summary>
        /// Append the commits in the body of the request, { commits: [ … ] }, parents before their children,
        /// and merge the tips afterwards.
        /// 200 OK:               all appended or held already; how many, the head and the conflicts left
        /// 422 Unprocessable:    one could not be appended; the ones before it are, and why
        /// 400 Bad Request:      no valid commits
        /// 403 Forbidden:        pushing is not enabled, or not allowed for the sender
        /// </summary>
        private HTTPResponse SubmitCommits(HTTPRequest Request)
        {

            if (!TryGetHistory(Request, out var history, out var errorResponse))
                return errorResponse;

            JObject? json = null;

            try
            {
                json = Request.HTTPBody is { Length: > 0 } body
                           ? Request.ContentType == HTTPContentType.Application.CBOR
                                 ? CBORJSON.ToJSON(CBORValue.Parse(body)) as JObject
                                 : POIJSON.Read(Encoding.UTF8.GetString(body)) as JObject
                           : null;
            }
            catch
            { }

            if (json is null || !TryParseCommits(json, out var commits, out var commitsError))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain { commits: [ … ] }!");

            var result = PushCommits(Request, history, commits);

            return result.Forbidden
                       ? ErrorResponse(Request, HTTPStatusCode.Forbidden, result.ErrorResponse)
                       : JSONResponse (Request, result.ErrorResponse is null ? HTTPStatusCode.OK : HTTPStatusCode.UnprocessableEntity, result.JSON);

        }

        #endregion

        #region (private) PushCommits(Request, History, Commits)

        /// <summary>
        /// Append the given pushed commits, if the sender may push them.
        /// </summary>
        private (Boolean Forbidden, JObject JSON, String? ErrorResponse) PushCommits(HTTPRequest                          Request,
                                                                                     RoamingNetworkHistory                History,
                                                                                     IReadOnlyList<RoamingNetworkCommit>  Commits)
        {

            if (AuthorizeCommitPush is null)
                return (true, [], "Pushing commits is not enabled here!");

            if (!AuthorizeCommitPush(Request, Commits))
                return (true, [], "The sender may not push these commits!");

            History.TryAppend(Commits,
                              out var appended,
                              out var appendError,
                              VerifyChangeSetSignature,
                              VerifyCommitSignature,
                              RequiredCommitSignatureRoles);

            var json = TipsJSON(History);

            json.AddFirst(new JProperty(PropertyKeys.Appended, appended));

            if (appendError is not null)
                json.Add(PropertyKeys.Description, appendError);

            return (false, json, appendError);

        }

        #endregion

        #region (private) TryParseCommits(JSON, out Commits, out ErrorResponse)

        /// <summary>
        /// The commits of { commits: [ … ] }.
        /// </summary>
        private Boolean TryParseCommits(JObject                                                    JSON,
                                        [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkCommit>?  Commits,
                                        [NotNullWhen(false)] out String?                           ErrorResponse)
        {

            Commits = null;

            if (JSON[PropertyKeys.Commits] is not JArray commitsJSON)
            {
                ErrorResponse = "Missing commits!";
                return false;
            }

            var commits = new List<RoamingNetworkCommit>();

            foreach (var commitJSON in commitsJSON)
            {

                if (commitJSON is not JObject commitObject ||
                    !RoamingNetworkCommit.TryParse(commitObject, out var commit, out var commitError, ChangeSetLimits))
                {
                    ErrorResponse = $"Invalid commit {commits.Count}!";
                    return false;
                }

                commits.Add(commit);

            }

            Commits        = commits;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (internal static) TipsJSON(History)

        /// <summary>
        /// The metadata of the head and of all tips, and the conflicts keeping them apart.
        /// </summary>
        internal static JObject TipsJSON(RoamingNetworkHistory History)
        {

            var tips  = History.Tips;
            var json  = new JObject(
                            new JProperty(PropertyKeys.Head,  new RoamingNetworkCommitInfo(tips[0].Commit).ToJSON(Embedded: true)),
                            new JProperty(PropertyKeys.Tips,  new JArray(tips.Select(tip => new RoamingNetworkCommitInfo(tip.Commit).ToJSON(Embedded: true))))
                        );

            var conflicts = History.Conflicts;

            if (!conflicts.IsEmpty)
                json.Add(PropertyKeys.Conflicts, new JArray(conflicts.Select(conflict => conflict.ToJSON(Embedded: true))));

            return json;

        }

        #endregion

        #region (internal static) HaveIdsJSON(History)

        /// <summary>
        /// The commits this history holds, as HaveIds() names them, for another server to send the missing ones.
        /// </summary>
        internal static JObject HaveIdsJSON(RoamingNetworkHistory History)

            => new (new JProperty(PropertyKeys.HaveIds, new JArray(History.HaveIds().Select(commitId => commitId.ToString()))));

        #endregion

        #region (private) TryParseHave(Have, out HaveIds, out ErrorResponse)

        /// <summary>
        /// The commit identifications of "have", a JSON array or a comma separated text, each once:
        /// at most MaxHaveIds of them, more are refused.
        /// </summary>
        private Boolean TryParseHave(JToken                                                     Have,
                                     [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkCommit_Id>?  HaveIds,
                                     [NotNullWhen(false)] out String?                           ErrorResponse)
        {

            HaveIds = null;

            var texts = Have switch {
                            JArray array                          => array.Select(item => item.Type == JTokenType.String ? item.Value<String>() : null),
                            JValue { Type: JTokenType.String } t  => t.Value<String>()!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                            _                                     => null
                        };

            if (texts is null)
            {
                ErrorResponse = "The commits held must be an array of commit identifications!";
                return false;
            }

            var named = texts.Take(Math.Max(0, MaxHaveIds) + 1).ToArray();

            // An honest server names its tips and a sample of older commits (HaveIds()), never more.
            if (named.Length > MaxHaveIds)
            {
                ErrorResponse = $"At most {MaxHaveIds} commits held may be named: name the tips and a sample of older commits!";
                return false;
            }

            var haveIds = new HashSet<RoamingNetworkCommit_Id>();

            foreach (var text in named)
            {

                if (text is null || !RoamingNetworkCommit_Id.TryParse(text, out var commitId))
                {
                    ErrorResponse = $"Invalid commit identification '{text}'!";
                    return false;
                }

                haveIds.Add(commitId);

            }

            HaveIds        = [ .. haveIds ];
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
