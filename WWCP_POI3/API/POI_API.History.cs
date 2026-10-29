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

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/commitSignaturesResult/v1";

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context     = "@context";

            /// <summary>
            /// The property of the identification of the commit.
            /// </summary>
            public const String  CommitId    = RoamingNetworkCommitSignature.PropertyKeys.CommitId;

            /// <summary>
            /// The property of all signatures of the commit.
            /// </summary>
            public const String  Signatures  = RoamingNetworkCommitSignature.PropertyKeys.Signatures;

            /// <summary>
            /// The property of the number of signatures added.
            /// </summary>
            public const String  Added       = "added";

        }

        #endregion

        /// <summary>
        /// { commitId, signatures, added }: all signatures of the commit, and how many were added.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomCommitSignaturesResultSerializer">An optional custom serializer of results of adding signatures.</param>
        public JObject ToJSON(Boolean                                                   Embedded                                 = false,
                              CustomJObjectSerializerDelegate<CommitSignaturesResult>?  CustomCommitSignaturesResultSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,     JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.CommitId,    CommitId.ToString()),
                                 new JProperty(PropertyKeys.Signatures,  new JArray(Commit?.Signatures.Select(signature => signature.ToJSON(Embedded: true)) ?? [])),
                                 new JProperty(PropertyKeys.Added,       Added)

                       );

            return CustomCommitSignaturesResultSerializer is not null
                       ? CustomCommitSignaturesResultSerializer(this, json)
                       : json;

        }

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
    /// GET    ~/RNs/{roamingNetworkId}/history/commits?have={commitId,…}&amp;max={n}     the complete commits missing at who holds these,
    ///                                                                             at most MaxHaveIds of them, also over
    ///                                                                             several have=…
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

            AddReadPreflight(history + "/head");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/snapshot

            AddHandler(history + "/snapshot",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  HeavyRead(request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : request.QueryString.GetString("runtimeStatus") is String runtimeStatusText &&
                                 runtimeStatusText is not ("true" or "false")
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid runtimeStatus option '{runtimeStatusText}'; use true or false!")

                                     : SnapshotResponse(request, roamingNetworkHistory, request.QueryString.GetString("runtimeStatus") != "false")

                       )));

            AddReadPreflight(history + "/snapshot");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/commitInfos?since={commitId}&max={n}

            AddHandler(history + "/commitInfos",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  HeavyRead(request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : !TryGetSince(request, roamingNetworkHistory, out var since, out var max, out errorResponse)
                                     ? errorResponse

                                     : roamingNetworkHistory.TryGetCommitInfosSince(since, out var commitInfos, out var error, max)
                                           ? JSONResponse(request, HTTPStatusCode.OK, new JArray(commitInfos.Select(commitInfo => commitInfo.ToJSON(Embedded: true))))
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, error)

                       )));

            AddReadPreflight(history + "/commitInfos");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/commits?since={commitId}&have={commitId,…}&max={n}

            AddHandler(history + "/commits",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  HeavyRead(request => {

                           if (!TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse) ||
                               !TryGetSince  (request, roamingNetworkHistory, out var since, out var max, out errorResponse))
                           {
                               return Task.FromResult(errorResponse);
                           }

                           IReadOnlyList<RoamingNetworkCommit>? commits;
                           String?                              error;

                           // The commits missing at a server holding the given ones, e.g. its tips and a sample of older commits:
                           // all of them, also of "have" given more than once, have=a&have=b as have=a,b.
                           if (request.QueryString.TryGetString("have", out _))
                           {

                               if (!TryParseHave(new JArray(request.QueryString.GetStrings("have")), out var haveIds, out var haveError))
                                   return Task.FromResult(ErrorResponse(request, HTTPStatusCode.BadRequest, haveError));

                               roamingNetworkHistory.TryGetMissingCommits(haveIds, out commits, out error, max);

                           }
                           else
                               roamingNetworkHistory.TryGetCommitsSince(since, out commits, out error, max);

                           return Task.FromResult(
                                      commits is not null
                                          ? JSONResponse(request, HTTPStatusCode.OK, new JArray(commits.Select(commit => commit.ToJSON(Embedded: true))))
                                          : ErrorResponse(request, HTTPStatusCode.NotFound, error)
                                  );

                       }));

            AddReadPreflight(history + "/commits", HTTPMethod.POST);

            #endregion

            #region COMMIT ~/RNs/{roamingNetworkId}/history/changeSets

            AddHandler(history + "/changeSets",
                       HTTPMethod:    COMMIT,
                       HTTPDelegate:  Changes(SubmitChangeSet));

            #endregion

            #region POST ~/RNs/{roamingNetworkId}/history/commits/{commitId}/signatures

            AddHandler(history + "/commits/{commitId}/signatures",
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  Changes(SubmitCommitSignatures));

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

                                     : roamingNetworkHistory.TryGetCommit(commitId, out var commit) || roamingNetworkHistory.TryGetArchivedCommit(commitId, out commit)
                                           ? JSONResponse(request, HTTPStatusCode.OK, commit.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, $"The commit '{commitId}' is unknown here!")

                       ));

            AddReadPreflight(history + "/commits/{commitId}");

            #endregion

            RegisterCheckpointURLTemplates(history);
            RegisterEpochURLTemplates(history);

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
                           ? HasCBORBody(Request)
                                 ? CBORJSON.ToJSON(CBORValue.Parse(body)) as JObject
                                 // signedAt is signed as the text it is.
                                 : POIJSON.Read(Encoding.UTF8.GetString(body)) as JObject
                           : null;
            }
            catch
            { }

            if (json is null)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain { signatures: [ … ] }!");

            json[RoamingNetworkCommitSignature.PropertyKeys.CommitId] = commitId.ToString();

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
        /// 409 Conflict:         made for another version than the head, its commit would conflict with the head;
        ///                       the conflicts and the metadata of the head: prepare it again for the head
        /// 422 Unprocessable:    refused, e.g. as it is not signed by its owners, fits no version or one too old
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

            if (HasCBORBody(Request)
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

                       RoamingNetworkCommitStatus.Conflict          => JSONResponse(Request,
                                                                                    HTTPStatusCode.Conflict,
                                                                                    ConflictJSON(result)),

                       _                                            => ErrorResponse(Request,
                                                                                     HTTPStatusCode.UnprocessableEntity,
                                                                                     result.ErrorResponse)

                   };

        }

        #endregion

        #region (private static) SnapshotResponse(Request, History, IncludeRuntimeStatus)

        /// <summary>
        /// A snapshot of the head of the given history, in JSON or in CBOR as the request prefers, with the attestations of
        /// its head: whoever starts from it checks it against its trust anchor.
        /// </summary>
        private static HTTPResponse SnapshotResponse(HTTPRequest            Request,
                                                     RoamingNetworkHistory  History,
                                                     Boolean                IncludeRuntimeStatus)

            => WantsCBOR(Request)
                   ? JSONResponse(Request, HTTPStatusCode.OK, new JObject(), () => History.ToSnapshotCBOR(IncludeRuntimeStatus, IncludeAttestations: true))
                   : JSONResponse(Request, HTTPStatusCode.OK, History.ToSnapshotJSON(IncludeRuntimeStatus, IncludeAttestations: true));

        #endregion

        #region (internal static) CommittedJSON(Result, History)

        /// <summary>
        /// The metadata of the commit of a committed change set, of the head afterwards, and the conflicts left between the tips.
        /// </summary>
        internal static JObject CommittedJSON(RoamingNetworkCommitResult  Result,
                                              RoamingNetworkHistory       History)
        {

            var json = new RoamingNetworkCommitInfo(Result.Commit!).ToJSON();

            json.Add(PropertyKeys.Status,  Result.Status.ToString());
            json.Add(PropertyKeys.Head,    new RoamingNetworkCommitInfo(Result.Head.Commit).ToJSON(Embedded: true));

            var conflicts = History.Conflicts;

            if (!conflicts.IsEmpty)
                json.Add(PropertyKeys.Conflicts, new JArray(conflicts.Select(conflict => conflict.ToJSON(Embedded: true))));

            return json;

        }

        #endregion

        #region (internal static) ConflictJSON(Result)

        /// <summary>
        /// Why a change set made for another version than the head was refused: the conflicts its commit would have with
        /// the head, and the metadata of the head to prepare it again for.
        /// </summary>
        internal static JObject ConflictJSON(RoamingNetworkCommitResult Result)

            => new (
                   new JProperty(PropertyKeys.Status,       Result.Status.ToString()),
                   new JProperty(PropertyKeys.Description,  Result.ErrorResponse),
                   new JProperty(PropertyKeys.Head,         new RoamingNetworkCommitInfo(Result.Head.Commit).ToJSON(Embedded: true)),
                   new JProperty(PropertyKeys.Conflicts,    new JArray((Result.Conflicts ?? []).Select(conflict => conflict.ToJSON(Embedded: true))))
               );

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
        /// An answer in JSON, or in CBOR when the request prefers it, never cached; the answer to a read readable from
        /// every origin, as the entities are, the answer to a change not.
        /// The CBOR has the keys and values of the JSON, unless given.
        /// </summary>
        private static HTTPResponse JSONResponse(HTTPRequest        Request,
                                                 HTTPStatusCode     StatusCode,
                                                 JToken             JSON,
                                                 Func<CBORValue>?   CBOR   = null)

            => WantsCBOR(Request)

                   ? new HTTPResponse.Builder(Request) {
                         HTTPStatusCode            = StatusCode,
                         ContentType               = HTTPContentType.Application.CBOR,
                         Content                   = (CBOR is not null
                                                          ? CBOR()
                                                          : CBORJSON.ToCBOR(JSON)).ToByteArray(CBORWriterOptions.Canonical),
                         CacheControl              = "no-store",
                         Vary                      = "Accept",
                         AccessControlAllowOrigin  = IsRead(Request) ? "*" : null
                     }.AsImmutable

                   : new HTTPResponse.Builder(Request) {
                         HTTPStatusCode            = StatusCode,
                         ContentType               = HTTPContentType.Application.JSON_UTF8,
                         Content                   = Encoding.UTF8.GetBytes(POIJSON.ToText(JSON)),
                         CacheControl              = "no-store",
                         Vary                      = "Accept",
                         AccessControlAllowOrigin  = IsRead(Request) ? "*" : null
                     }.AsImmutable;

        #endregion

        #region (private static) IsRead       (Request)

        /// <summary>
        /// Whether the request reads, GET or HEAD, rather than changes something.
        /// </summary>
        private static Boolean IsRead(HTTPRequest Request)

            => Request.HTTPMethod == HTTPMethod.GET ||
               Request.HTTPMethod == HTTPMethod.HEAD;

        #endregion

        #region (private)        AddReadPreflight(Path, params Changes)

        /// <summary>
        /// Answer OPTIONS of a read of a history with the CORS permissions of every origin, as for the entities: GET and
        /// HEAD, so that a page of another site may read what is served to everyone. Not the given methods of the same
        /// path that change something: Allow names them, Access-Control-Allow-Methods does not, and a browser asked by
        /// such a page sends none of them (POI3-ISSUES.md S23).
        /// </summary>
        private void AddReadPreflight(HTTPPath             Path,
                                      params HTTPMethod[]  Changes)

            => AddHandler(Path,
                          HTTPMethod:    HTTPMethod.OPTIONS,
                          HTTPDelegate:  request => Task.FromResult(
                                             new HTTPResponse.Builder(request) {
                                                 HTTPStatusCode             = HTTPStatusCode.NoContent,
                                                 Allow                      = [ .. EntityMethods, .. Changes ],
                                                 AccessControlAllowOrigin   = "*",
                                                 AccessControlAllowMethods  = EntityMethods,
                                                 AccessControlAllowHeaders  = EntityRequestHeaders,
                                                 AccessControlMaxAge        = 86400
                                             }.AsImmutable
                                         ));

        #endregion

        #region (private static) HasCBORBody  (Request)

        /// <summary>
        /// Whether the body of the request is CBOR by its Content-Type, whatever the case it is written in.
        /// </summary>
        private static Boolean HasCBORBody(HTTPRequest Request)

            => String.Equals(Request.ContentType?.MediaType, HTTPContentType.Application.CBOR.MediaType, StringComparison.OrdinalIgnoreCase);

        #endregion

        #region (private static) HasJSONBody  (Request)

        /// <summary>
        /// Whether the body of the request is JSON by its Content-Type, whatever the case it is written in.
        /// </summary>
        private static Boolean HasJSONBody(HTTPRequest Request)

            => String.Equals(Request.ContentType?.MediaType, HTTPContentType.Application.JSON_UTF8.MediaType, StringComparison.OrdinalIgnoreCase);

        #endregion

        #region (private)        Changes      (Handler)

        /// <summary>
        /// The given handler of a request that changes something, behind a 415 Unsupported Media Type for every request
        /// whose Content-Type is neither application/json nor application/cbor - parameters such as a charset aside.
        /// </summary>
        /// <remarks>
        /// A handler read every body that did not say CBOR as JSON. So "text/plain", which a page of another site may make
        /// a browser send without asking first, reached it (POI3-ISSUES.md S23). What a change says is signed by its owners,
        /// but a server may also let in by a cookie, Basic auth, a client certificate or an address, which go with such a
        /// request. Neither of the two media types a browser sends for another site's page without a CORS preflight first.
        /// </remarks>
        private HTTPDelegate Changes(Func<HTTPRequest, HTTPResponse> Handler)

            => request => Task.FromResult(
                              HasJSONBody(request) || HasCBORBody(request)
                                  ? Handler(request)
                                  : ErrorResponse(request,
                                                  HTTPStatusCode.UnsupportedMediaType,
                                                  "The request body must be application/json or application/cbor!")
                          );

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
                            new JObject(new JProperty(PropertyKeys.Description, Description ?? StatusCode.ToString())));

        #endregion

    }

}
