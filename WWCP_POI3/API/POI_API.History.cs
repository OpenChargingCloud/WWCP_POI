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

using org.GraphDefined.Vanaheimr.Illias;
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
    /// A delegate deciding whether the sender of the given HTTP request may add the given signature to the given commit,
    /// e.g. whether its role is the one of the sender.
    /// </summary>
    /// <param name="Request">The HTTP request, or the WebSocket upgrade request of the sender.</param>
    /// <param name="Commit">The commit.</param>
    /// <param name="Signature">The signature to add.</param>
    public delegate Boolean AuthorizeCommitSignatureDelegate(HTTPRequest                    Request,
                                                             RoamingNetworkCommit           Commit,
                                                             RoamingNetworkCommitSignature  Signature);


    /// <summary>
    /// The outcome of adding signatures to a commit.
    /// </summary>
    public enum CommitSignaturesStatus
    {

        /// <summary>
        /// At least one signature was added.
        /// </summary>
        Added,

        /// <summary>
        /// The commit had all signatures already: nothing changed.
        /// </summary>
        Known,

        /// <summary>
        /// The commit is unknown here.
        /// </summary>
        NotFound,

        /// <summary>
        /// Adding signatures is not enabled, or not allowed for the sender.
        /// </summary>
        Forbidden,

        /// <summary>
        /// A signature is invalid, or its key is not trusted: nothing changed.
        /// </summary>
        Invalid

    }


    /// <summary>
    /// The result of adding signatures to a commit.
    /// </summary>
    /// <param name="Status">The outcome.</param>
    /// <param name="CommitId">The identification of the commit.</param>
    /// <param name="Commit">The commit with all its signatures afterwards, unless unknown.</param>
    /// <param name="Added">The number of signatures added.</param>
    /// <param name="ErrorResponse">Why nothing was added.</param>
    public sealed record CommitSignaturesResult(CommitSignaturesStatus    Status,
                                                RoamingNetworkCommit_Id   CommitId,
                                                RoamingNetworkCommit?     Commit          = null,
                                                Int32                     Added           = 0,
                                                String?                   ErrorResponse   = null)
    {

        /// <summary>
        /// { commitId, signatures, added }: all signatures of the commit, and how many were added.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty("commitId",    CommitId.ToString()),
                   new JProperty("signatures",  new JArray(Commit?.Signatures.Select(signature => signature.ToJSON()) ?? [])),
                   new JProperty("added",       Added)
               );

    }


    /// <summary>
    /// The histories of roaming networks over HTTP, as JSON:
    /// what another server needs to start from a snapshot, to catch up with the commits since,
    /// and to submit its change sets to the leading server.
    /// </summary>
    /// <remarks>
    /// GET    ~/RNs/{roamingNetworkId}/history/head                                  the metadata of the head commit
    /// GET    ~/RNs/{roamingNetworkId}/history/snapshot?runtimeStatus=false          the head commit, the complete roaming network and its runtime status
    /// GET    ~/RNs/{roamingNetworkId}/history/commitInfos?since={commitId}&amp;max={n}   the metadata of the commits since, without changes
    /// GET    ~/RNs/{roamingNetworkId}/history/commits?since={commitId}&amp;max={n}       the complete commits since
    /// GET    ~/RNs/{roamingNetworkId}/history/commits/{commitId}                    one complete commit
    /// COMMIT ~/RNs/{roamingNetworkId}/history/changeSets?expectedHead={commitId}&amp;rebase=true
    ///                                                                                         publish a change set as the new head commit
    /// POST   ~/RNs/{roamingNetworkId}/history/commits/{commitId}/signatures      add signatures to a commit: { signatures: [ … ] }
    /// Without "since", the commits since the root are returned. "max" is at most MaxCommitsPerRequest.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The maximum number of commits returned by one request.
        /// </summary>
        public const Int32 MaxCommitsPerRequest = RoamingNetworkHistory.DefaultMaxCommits;

        /// <summary>
        /// Commit a change set to a history: Hermod's COMMIT. Not safe, as it changes the history;
        /// idempotent, as committing the same change set again returns the same commit. A client
        /// of Hermod sends one by HTTPClient.COMMIT(path, content, contentType).
        /// </summary>
        public static readonly HTTPMethod COMMIT = HTTPMethod.COMMIT;

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
        /// Decides who may add which signatures to commits, e.g. a roaming network operator that checked the data.
        /// Without it, adding signatures is refused.
        /// </summary>
        public AuthorizeCommitSignatureDelegate?      AuthorizeCommitSignature        { get; set; }

        /// <summary>
        /// Verifies the signatures of commits with trusted keys; without it, signatures are refused.
        /// </summary>
        public VerifyCommitSignatureDelegate?         VerifyCommitSignature           { get; set; }

        /// <summary>
        /// The limits for reading submitted change sets.
        /// </summary>
        public RoamingNetworkChangeSetLimits          ChangeSetLimits                 { get; set; } = RoamingNetworkChangeSetLimits.Default;

        #endregion


        #region AddRoamingNetworkHistory   (History)

        /// <summary>
        /// Serve the given history of a roaming network, over HTTP and over its WebSocket.
        /// </summary>
        /// <param name="History">The history of a roaming network.</param>
        public Boolean AddRoamingNetworkHistory(RoamingNetworkHistory History)
        {

            var webSocketServer = CreateHistoryWebSocketServer(History);

            if (!historyWebSocketServers.TryAdd(History.RoamingNetworkId, webSocketServer))
            {
                webSocketServer.DisposeAsync().AsTask().GetAwaiter().GetResult();
                return false;
            }

            if (!roamingNetworkHistories.TryAdd(History.RoamingNetworkId, History))
            {
                historyWebSocketServers.TryRemove(History.RoamingNetworkId, out _);
                webSocketServer.DisposeAsync().AsTask().GetAwaiter().GetResult();
                return false;
            }

            ConnectHistoryWebSocketServer(History, webSocketServer);

            return true;

        }

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

            var history = HTTPPath.Root + "RNs/{roamingNetworkId}/history";

            #region GET ~/RNs/{roamingNetworkId}/history/head

            AddHandler(history + "/head",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, new RoamingNetworkCommitInfo(roamingNetworkHistory.Head.Commit).ToJSON())
                               : errorResponse

                       ));

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/snapshot

            AddHandler(history + "/snapshot",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : request.QueryString.GetString("runtimeStatus") is String runtimeStatusText &&
                                 runtimeStatusText is not ("true" or "false")
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid runtimeStatus option '{runtimeStatusText}'; use true or false!")

                                     : SnapshotResponse(request, roamingNetworkHistory, request.QueryString.GetString("runtimeStatus") != "false")

                       ));

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/commitInfos?since={commitId}&max={n}

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

            #region GET ~/RNs/{roamingNetworkId}/history/commits?since={commitId}&have={commitId,…}&max={n}

            AddHandler(history + "/commits",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => {

                           if (!TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse) ||
                               !TryGetSince  (request, roamingNetworkHistory, out var since, out var max, out errorResponse))
                           {
                               return Task.FromResult(errorResponse);
                           }

                           IReadOnlyList<RoamingNetworkCommit>? commits;
                           String?                              error;

                           // The commits missing at a server holding the given ones, e.g. its tips and a sample of older commits.
                           if (request.QueryString.GetString("have") is String haveText)
                           {

                               if (!TryParseHave(haveText, out var haveIds, out var haveError))
                                   return Task.FromResult(ErrorResponse(request, HTTPStatusCode.BadRequest, haveError));

                               roamingNetworkHistory.TryGetMissingCommits(haveIds, out commits, out error, max);

                           }
                           else
                               roamingNetworkHistory.TryGetCommitsSince(since, out commits, out error, max);

                           return Task.FromResult(
                                      commits is not null
                                          ? JSONResponse(request, HTTPStatusCode.OK, new JArray(commits.Select(commit => commit.ToJSON())))
                                          : ErrorResponse(request, HTTPStatusCode.NotFound, error)
                                  );

                       });

            #endregion

            #region COMMIT ~/RNs/{roamingNetworkId}/history/changeSets

            AddHandler(history + "/changeSets",
                       HTTPMethod:    COMMIT,
                       HTTPDelegate:  request => Task.FromResult(SubmitChangeSet(request)));

            #endregion

            #region POST ~/RNs/{roamingNetworkId}/history/commits/{commitId}/signatures

            AddHandler(history + "/commits/{commitId}/signatures",
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  request => Task.FromResult(SubmitCommitSignatures(request)));

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/commits/{commitId}

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

            RegisterCheckpointURLTemplates(history);

        }

        #endregion


        #region (private) SubmitCommitSignatures(Request)

        /// <summary>
        /// Add the signatures in the body of the request, { signatures: [ … ] }, to a commit.
        /// 201 Created:          at least one signature added; { commitId, signatures, added }
        /// 200 OK:               the commit had all of them already
        /// 400 Bad Request:      no valid signatures, or an invalid commit identification
        /// 403 Forbidden:        adding signatures is not enabled, or not allowed for the sender
        /// 404 Not Found:        the commit is unknown here
        /// 422 Unprocessable:    a signature is invalid, or its key is not trusted
        /// </summary>
        private HTTPResponse SubmitCommitSignatures(HTTPRequest Request)
        {

            if (!TryGetHistory(Request, out var history, out var errorResponse))
                return errorResponse;

            if (Request.ParsedURLParameters.Length < 2 ||
                !RoamingNetworkCommit_Id.TryParse(Request.ParsedURLParameters[1], out var commitId))
            {
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid commit identification!");
            }

            JObject? json = null;

            try
            {
                json = Request.HTTPBody is { Length: > 0 } body
                           ? Request.ContentType == HTTPContentType.Application.CBOR
                                 ? CBORJSON.ToJSON(CBORValue.Parse(body)) as JObject
                                 : JToken.ReadFrom(new JsonTextReader(new StringReader(Encoding.UTF8.GetString(body))) {
                                       // signedAt is signed as the text it is.
                                       DateParseHandling   = DateParseHandling.None,
                                       FloatParseHandling  = FloatParseHandling.Decimal
                                   }) as JObject
                           : null;
            }
            catch
            { }

            if (json is null)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain { signatures: [ … ] }!");

            json["commitId"] = commitId.ToString();

            if (!RoamingNetworkCommitSignature.TryParseCommitSignatures(json, out _, out var signatures, out var signaturesError))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, signaturesError);

            var result = AddCommitSignatures(Request, history, commitId, signatures);

            return result.Status switch {
                       CommitSignaturesStatus.Added      => JSONResponse (Request, HTTPStatusCode.Created,              result.ToJSON()),
                       CommitSignaturesStatus.Known      => JSONResponse (Request, HTTPStatusCode.OK,                   result.ToJSON()),
                       CommitSignaturesStatus.NotFound   => ErrorResponse(Request, HTTPStatusCode.NotFound,             result.ErrorResponse),
                       CommitSignaturesStatus.Forbidden  => ErrorResponse(Request, HTTPStatusCode.Forbidden,            result.ErrorResponse),
                       _                                 => ErrorResponse(Request, HTTPStatusCode.UnprocessableEntity,  result.ErrorResponse)
                   };

        }

        #endregion

        #region AddCommitSignatures(Request, History, CommitId, Signatures)

        /// <summary>
        /// Add the given signatures of the sender of the given request to a commit of the given history,
        /// when allowed for the sender and verified with trusted keys.
        /// </summary>
        /// <param name="Request">The HTTP request, or the WebSocket upgrade request of the sender.</param>
        /// <param name="History">The history.</param>
        /// <param name="CommitId">The identification of the commit.</param>
        /// <param name="Signatures">The signatures to add.</param>
        public CommitSignaturesResult AddCommitSignatures(HTTPRequest                                 Request,
                                                          RoamingNetworkHistory                       History,
                                                          RoamingNetworkCommit_Id                     CommitId,
                                                          IEnumerable<RoamingNetworkCommitSignature>  Signatures)
        {

            if (AuthorizeCommitSignature is null)
                return new (CommitSignaturesStatus.Forbidden, CommitId, ErrorResponse: "Adding signatures to commits is not enabled here!");

            if (!History.TryGetCommit(CommitId, out var commit))
                return new (CommitSignaturesStatus.NotFound,  CommitId, ErrorResponse: $"The commit '{CommitId}' is unknown here!");

            var signatures = Signatures.ToArray();

            foreach (var signature in signatures)
                if (!AuthorizeCommitSignature(Request, commit, signature))
                    return new (CommitSignaturesStatus.Forbidden, CommitId, commit, ErrorResponse: $"The sender may not add the signature by {signature}!");

            if (!History.TryAddSignatures(CommitId, signatures, VerifyCommitSignature, out var signedCommit, out var newSignatures, out var errorResponse))
                return new (CommitSignaturesStatus.Invalid,   CommitId, commit, ErrorResponse: errorResponse);

            return new (newSignatures.Count > 0
                            ? CommitSignaturesStatus.Added
                            : CommitSignaturesStatus.Known,
                        CommitId,
                        signedCommit,
                        newSignatures.Count);

        }

        #endregion

        #region (private) SubmitChangeSet(Request)

        /// <summary>
        /// Commit the change set in the body of the request to the version it was made for,
        /// and merge it with the other tips where it does not conflict.
        /// 201 Created:          committed now; the metadata of the commit, of the head, and the conflicts left
        /// 200 OK:               committed before, also in a rebased version; the metadata of its commit
        /// 422 Unprocessable:    refused, e.g. as it is not signed by its owners or fits no version
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

            var body = Request.HTTPBody;

            if (body is null || body.Length == 0)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain a change set!");

            RoamingNetworkChangeSet? changeSet;
            String?                  changeSetError;

            if (Request.ContentType == HTTPContentType.Application.CBOR
                    ? !RoamingNetworkChangeSet.TryParseCBOR(body,                          out changeSet, out changeSetError, ChangeSetLimits)
                    : !RoamingNetworkChangeSet.TryParse    (Encoding.UTF8.GetString(body), out changeSet, out changeSetError, ChangeSetLimits))
            {
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid change set: " + changeSetError);
            }

            if (changeSet.RoamingNetworkId != history.RoamingNetworkId)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, $"The change set is for the roaming network '{changeSet.RoamingNetworkId}', not for '{history.RoamingNetworkId}'!");

            #endregion

            if (!AuthorizeChangeSetSubmission(Request, changeSet))
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "The sender may not submit this change set!");

            var result = history.Commit(changeSet,
                                        VerifyChangeSetSignature);

            return result.Status switch {

                       RoamingNetworkCommitStatus.Committed         => JSONResponse(Request,
                                                                                    HTTPStatusCode.Created,
                                                                                    CommittedJSON(result, history)),

                       RoamingNetworkCommitStatus.AlreadyCommitted  => JSONResponse(Request,
                                                                                    HTTPStatusCode.OK,
                                                                                    CommittedJSON(result, history)),

                       _                                            => ErrorResponse(Request,
                                                                                     HTTPStatusCode.UnprocessableEntity,
                                                                                     result.ErrorResponse)

                   };

        }

        #endregion

        #region (private static) SnapshotResponse(Request, History, IncludeRuntimeStatus)

        /// <summary>
        /// A snapshot of the head of the given history, in JSON or in CBOR as the request prefers.
        /// </summary>
        private static HTTPResponse SnapshotResponse(HTTPRequest            Request,
                                                     RoamingNetworkHistory  History,
                                                     Boolean                IncludeRuntimeStatus)

            => WantsCBOR(Request)
                   ? JSONResponse(Request, HTTPStatusCode.OK, new JObject(), () => History.ToSnapshotCBOR(IncludeRuntimeStatus))
                   : JSONResponse(Request, HTTPStatusCode.OK, History.ToSnapshotJSON(IncludeRuntimeStatus));

        #endregion

        #region (internal static) CommittedJSON(Result, History)

        /// <summary>
        /// The metadata of the commit of a committed change set, of the head afterwards, and the conflicts left between the tips.
        /// </summary>
        internal static JObject CommittedJSON(RoamingNetworkCommitResult  Result,
                                              RoamingNetworkHistory       History)
        {

            var json = new RoamingNetworkCommitInfo(Result.Commit!).ToJSON();

            json.Add("status",  Result.Status.ToString());
            json.Add("head",    new RoamingNetworkCommitInfo(Result.Head.Commit).ToJSON());

            var conflicts = History.Conflicts;

            if (!conflicts.IsEmpty)
                json.Add("conflicts", new JArray(conflicts.Select(conflict => conflict.ToJSON())));

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

        #region (private static) WantsCBOR    (Request)

        /// <summary>
        /// Whether the request prefers CBOR to JSON by its Accept header field.
        /// </summary>
        private static Boolean WantsCBOR(HTTPRequest Request)

            => Request.Accept.BestMatchingContentType(HTTPContentType.Application.JSON_UTF8,
                                                      HTTPContentType.Application.CBOR) == HTTPContentType.Application.CBOR;

        #endregion

        #region (private static) JSONResponse (Request, StatusCode, JSON, CBOR = null)

        /// <summary>
        /// An answer in JSON, or in CBOR when the request prefers it, never cached.
        /// The CBOR has the keys and values of the JSON, unless given.
        /// </summary>
        private static HTTPResponse JSONResponse(HTTPRequest        Request,
                                                 HTTPStatusCode     StatusCode,
                                                 JToken             JSON,
                                                 Func<CBORValue>?   CBOR   = null)

            => WantsCBOR(Request)

                   ? new HTTPResponse.Builder(Request) {
                         HTTPStatusCode  = StatusCode,
                         ContentType     = HTTPContentType.Application.CBOR,
                         Content         = (CBOR is not null
                                                ? CBOR()
                                                : CBORJSON.ToCBOR(JSON)).ToByteArray(CBORWriterOptions.Canonical),
                         CacheControl    = "no-store",
                         Vary            = "Accept"
                     }.AsImmutable

                   : new HTTPResponse.Builder(Request) {
                         HTTPStatusCode  = StatusCode,
                         ContentType     = HTTPContentType.Application.JSON_UTF8,
                         Content         = Encoding.UTF8.GetBytes(POIJSON.ToText(JSON)),
                         CacheControl    = "no-store",
                         Vary            = "Accept"
                     }.AsImmutable;

        #endregion

        #region (private)        ErrorResponse(Request, StatusCode, Description)

        /// <summary>
        /// An error in JSON, or in CBOR when the request prefers it: { "description": "…" }.
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
