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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate deciding whether the client of the given WebSocket upgrade request may set the given status.
    /// </summary>
    /// <param name="Request">The HTTP request that asked for the upgrade, e.g. with the credentials of the client.</param>
    /// <param name="Login">The login of the authenticated client, or null.</param>
    /// <param name="StatusUpdate">The requested status update, without old values.</param>
    public delegate Boolean AuthorizeStatusUpdateDelegate(HTTPRequest      Request,
                                                          String?          Login,
                                                          POIStatusUpdate  StatusUpdate);


    /// <summary>
    /// The histories of roaming networks over a WebSocket: the commands of the HTTP API,
    /// as OCPP-J framed requests answered asynchronously, in JSON text or in CBOR binary frames.
    /// </summary>
    /// <remarks>
    /// GET ~/RNs/{roamingNetworkId}/history/ws   upgrade to the WebSocket of the history
    ///
    /// Action            Request payload                            Response payload
    /// head              {}                                         the metadata of the head commit
    /// snapshot          { runtimeStatus? }                         the head commit, the complete roaming network and, unless
    ///                                                              runtimeStatus is false, its runtime status
    /// commitInfos       { since?, max? }                           { commitInfos: [ … ] }
    /// commits           { since?, max? }                           { commits:     [ … ] }
    /// commit            { commitId }                               the complete commit
    /// commitChangeSet   { changeSet }                              { status, …, head, conflicts? }: Committed or AlreadyCommitted with the
    ///                                                              metadata of the commit, the head and the conflicts left, or Refused
    /// tips              {}                                         { head, tips: [ … ], conflicts? }: the metadata of the tips
    /// haveIds           {}                                         { haveIds: [ … ] }: the tips and a sample of older commits held here
    /// pushCommits       { commits: [ … ] }                         { appended, head, tips, conflicts?, description? }: commits of
    ///                                                              another server, parents first, as AuthorizeCommitPush allows
    /// runtimeStatus     {}                                         { runtimeStatus: [ … ] }: the current admin status and status
    /// setStatus         { kind, entityType, entityId, value,       { result, description?, current? }: Updated, Unchanged, Outdated,
    ///                     timestamp?, dataSource? }                Scheduled or Refused, and the current status afterwards
    /// pushStatusUpdates { statusUpdates: [ … ] }                   { results: [ { result, description? }, … ] }: status updates of another
    ///                                                              server, as the topics push them, taken with their timestamps,
    ///                                                              as AuthorizeStatusPush allows
    /// signCommit       { commitId, signatures: [ … ] }            { commitId, signatures, added }: all signatures of the commit afterwards,
    ///                                                              and how many were added
    /// topics            {}                                         { topics: [ { name, action, description } ] }
    /// subscribe         { topics: [ … ] }                          { topics: [ … ] }: all topics subscribed now
    /// unsubscribe       { topics: [ … ] }                          { topics: [ … ] }
    /// Unknown commits are answered with the request error NotFound, invalid requests with FormatViolation,
    /// change sets nobody may submit with SecurityError.
    /// Every action takes a batch, { batch: [ payload, … ] }, answered with { batch: [ response or { error }, … ] }.
    ///
    /// Topic                  Push
    /// commitAnnouncements    commitAnnouncement   { timestamp, commitId, parentIds, revision, description? }
    /// commits                commit               the complete commit; one larger than MaxPushedCommitSize
    ///                                             as a commitAnnouncement, to be fetched
    /// commitSignatures       commitSignatures     { commitId, signatures: [ … ] }: signatures added to a commit later
    /// adminStatusUpdates     adminStatusUpdate    { kind, entityType, entityId, timestamp, value, oldTimestamp?, oldValue?, dataSource? }
    /// statusUpdates          statusUpdate         the same, for the status
    /// energyStatusUpdates    energyStatusUpdate   the same, for the energy status of EVSEs and charging stations,
    ///                                             its value { used, available, storedEnergy? } in W, W and Wh
    /// </remarks>
    public partial class POI_API : IAsyncDisposable
    {

        #region Data

        /// <summary>
        /// The topic of slim notices of new commits.
        /// </summary>
        public const String CommitAnnouncementsTopic  = "commitAnnouncements";

        /// <summary>
        /// The topic of complete commits.
        /// </summary>
        public const String CommitsTopic              = "commits";

        /// <summary>
        /// The topic of signatures added to commits later, e.g. by a roaming network operator.
        /// </summary>
        public const String CommitSignaturesTopic     = "commitSignatures";

        /// <summary>
        /// The default maximum size of a commit pushed completely, as JSON text: 1 MiB.
        /// </summary>
        public const Int32  DefaultMaxPushedCommitSize = 1024 * 1024;

        /// <summary>
        /// The topic of admin status updates.
        /// </summary>
        public const String AdminStatusUpdatesTopic   = "adminStatusUpdates";

        /// <summary>
        /// The topic of status updates.
        /// </summary>
        public const String StatusUpdatesTopic        = "statusUpdates";

        /// <summary>
        /// The topic of energy status updates.
        /// </summary>
        public const String EnergyStatusUpdatesTopic  = "energyStatusUpdates";

        private readonly ConcurrentDictionary<RoamingNetwork_Id, POIWebSocketServer>  historyWebSocketServers  = [];
        private readonly ConcurrentDictionary<RoamingNetwork_Id, Action>              historyDisconnects       = [];

        private AuthenticatePOIWebSocketClientDelegate? authenticateWebSocketClient;

        #endregion

        #region Properties

        /// <summary>
        /// Decides who the client opening the WebSocket of a history is; its upgrade request is refused
        /// when it returns no login. Without it, everybody may open it, as everybody may read a history over HTTP.
        /// Change sets are submitted over it as decided by AuthorizeChangeSetSubmission, given the upgrade request,
        /// and status set as decided by AuthorizeStatusUpdate.
        /// </summary>
        public AuthenticatePOIWebSocketClientDelegate? AuthenticateWebSocketClient
        {

            get
            {
                return authenticateWebSocketClient;
            }

            set
            {

                authenticateWebSocketClient = value;

                foreach (var webSocketServer in historyWebSocketServers.Values)
                    webSocketServer.AuthenticateClient = value;

            }

        }

        /// <summary>
        /// Decides who may set which status over the WebSocket of a history. Without it, setting a status is refused:
        /// a server has to decide on purpose whom it lets change the status of its roaming networks.
        /// </summary>
        public AuthorizeStatusUpdateDelegate? AuthorizeStatusUpdate { get; set; }

        /// <summary>
        /// Decides who may push which status updates, e.g. another server pushing the status set there,
        /// per status update. Without it, pushing status updates is refused.
        /// </summary>
        public AuthorizeStatusUpdateDelegate? AuthorizeStatusPush { get; set; }

        /// <summary>
        /// The maximum size of a commit pushed completely to the subscribers of "commits", as JSON text.
        /// A larger one is pushed there as a commitAnnouncement, and fetched by whoever wants it.
        /// </summary>
        public Int32 MaxPushedCommitSize { get; set; } = DefaultMaxPushedCommitSize;

        #endregion


        #region TryGetHistoryWebSocketServer(RoamingNetworkId, out WebSocketServer)

        /// <summary>
        /// Try to return the WebSocket server of the served history of the given roaming network.
        /// </summary>
        /// <param name="RoamingNetworkId">The identification of a roaming network.</param>
        /// <param name="WebSocketServer">The WebSocket server of its history.</param>
        public Boolean TryGetHistoryWebSocketServer(RoamingNetwork_Id                            RoamingNetworkId,
                                                    [NotNullWhen(true)] out POIWebSocketServer?  WebSocketServer)

            => historyWebSocketServers.TryGetValue(RoamingNetworkId, out WebSocketServer);

        #endregion


        #region (private) RegisterWebSocketURLTemplates()

        private void RegisterWebSocketURLTemplates()
        {

            #region GET ~/RNs/{roamingNetworkId}/history/ws

            AddHandler(HTTPPath.Root + "RNs/{roamingNetworkId}/history/ws",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request =>

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? Task.FromResult(errorResponse)

                               : historyWebSocketServers.TryGetValue(roamingNetworkHistory.RoamingNetworkId, out var webSocketServer)
                                     ? WebSocketUpgrade.For(webSocketServer)(request)
                                     : Task.FromResult(ErrorResponse(request, HTTPStatusCode.NotFound, $"The roaming network '{roamingNetworkHistory.RoamingNetworkId}' has no WebSocket here!"))

                       );

            #endregion

        }

        #endregion

        #region (private) CreateHistoryWebSocketServer(History)

        /// <summary>
        /// The WebSocket server of the given history, with its commands.
        /// </summary>
        private POIWebSocketServer CreateHistoryWebSocketServer(RoamingNetworkHistory History)
        {

            var webSocketServer = new POIWebSocketServer(AuthenticateClient: authenticateWebSocketClient) {

                                      // A change set with the array, the payload and the other request properties around it.
                                      MaxTextMessageSizeIn    = (UInt64) ChangeSetLimits.MaxBytes + 64 * 1024,
                                      MaxBinaryMessageSizeIn  = (UInt64) ChangeSetLimits.MaxBytes + 64 * 1024,
                                      MaxMessageDepth         = ChangeSetLimits.MaxDepth          + 4

                                  };

            #region head

            webSocketServer.AddCommand("head", request =>

                Task.FromResult(
                    request.Respond(new RoamingNetworkCommitInfo(History.Head.Commit).ToJSON())
                )

            );

            #endregion

            #region snapshot

            webSocketServer.AddCommand("snapshot", request => {

                var includeRuntimeStatus = true;

                if (request.Payload.ParseOptional("runtimeStatus",
                                                  "include runtime status",
                                                  out Boolean? runtimeStatus,
                                                  out var errorResponse))
                {

                    if (errorResponse is not null)
                        return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse));

                    includeRuntimeStatus = runtimeStatus!.Value;

                }

                return Task.FromResult(
                           request.Respond(() => History.ToSnapshotJSON(includeRuntimeStatus),
                                           () => History.ToSnapshotCBOR(includeRuntimeStatus))
                       );

            });

            #endregion

            #region commitInfos

            webSocketServer.AddCommand("commitInfos", request =>

                Task.FromResult(

                    !TryGetSince(request, History, out var since, out var max, out var errorMessage)
                        ? errorMessage

                        : History.TryGetCommitInfosSince(since, out var commitInfos, out var error, max)
                              ? request.Respond(new JObject(new JProperty("commitInfos", new JArray(commitInfos.Select(commitInfo => commitInfo.ToJSON())))))
                              : request.Error(POIWebSocketErrorCodes.NotFound, error)

                )

            );

            #endregion

            #region commits

            webSocketServer.AddCommand("commits", request => {

                if (!TryGetSince(request, History, out var since, out var max, out var errorMessage))
                    return Task.FromResult(errorMessage);

                IReadOnlyList<RoamingNetworkCommit>? commits;
                String?                              error;

                // The commits missing at a server holding the given ones, e.g. its tips and a sample of older commits.
                if (request.Payload["have"] is JToken have)
                {

                    if (!TryParseHave(have, out var haveIds, out var haveError))
                        return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, haveError));

                    History.TryGetMissingCommits(haveIds, out commits, out error, max);

                }
                else
                    History.TryGetCommitsSince(since, out commits, out error, max);

                return Task.FromResult(
                           commits is not null
                               ? request.Respond(new JObject(new JProperty("commits", new JArray(commits.Select(commit => commit.ToJSON())))))
                               : request.Error(POIWebSocketErrorCodes.NotFound, error)
                       );

            });

            #endregion

            #region tips

            webSocketServer.AddCommand("tips", request =>

                Task.FromResult(
                    request.Respond(TipsJSON(History))
                )

            );

            #endregion

            #region haveIds

            webSocketServer.AddCommand("haveIds", request =>

                Task.FromResult(
                    request.Respond(HaveIdsJSON(History))
                )

            );

            #endregion

            #region pushCommits

            webSocketServer.AddCommand("pushCommits", request => {

                if (request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The sender of the commits is unknown!"));

                if (!TryParseCommits(request.Payload, out var commits, out var commitsError))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, commitsError));

                var result = PushCommits(upgradeRequest, History, commits);

                return Task.FromResult(
                           result.Forbidden
                               ? request.Error(POIWebSocketErrorCodes.SecurityError, result.ErrorResponse!)
                               : request.Respond(result.JSON)
                       );

            });

            #endregion

            #region commit

            webSocketServer.AddCommand("commit", request => {

                if (!request.Payload.ParseMandatory("commitId",
                                                    "commit identification",
                                                    RoamingNetworkCommit_Id.TryParse,
                                                    out RoamingNetworkCommit_Id commitId,
                                                    out var errorResponse))
                {
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse));
                }

                return Task.FromResult(
                           History.TryGetCommit(commitId, out var commit)
                               ? request.Respond(commit.ToJSON())
                               : request.Error(POIWebSocketErrorCodes.NotFound, $"The commit '{commitId}' is unknown here!")
                       );

            });

            #endregion

            #region commitChangeSet

            webSocketServer.AddCommand("commitChangeSet", request =>

                Task.FromResult(
                    CommitChangeSet(request, History)
                )

            );

            #endregion

            #region signCommit

            webSocketServer.AddCommand("signCommit", request =>

                Task.FromResult(
                    SignCommit(request, History)
                )

            );

            #endregion

            #region runtimeStatus

            webSocketServer.AddCommand("runtimeStatus", request =>

                Task.FromResult(
                    request.Respond(new JObject(new JProperty("runtimeStatus", new JArray(History.RuntimeStatus.All.Select(status => status.ToJSON())))))
                )

            );

            #endregion

            #region setStatus

            webSocketServer.AddCommand("setStatus", request =>

                Task.FromResult(
                    SetStatus(request, History)
                )

            );

            #endregion

            #region pushStatusUpdates

            webSocketServer.AddCommand("pushStatusUpdates", request =>

                Task.FromResult(
                    PushStatusUpdates(request, History, webSocketServer.MaxBatchSize)
                )

            );

            #endregion

            #region Topics

            webSocketServer.AddTopic(new POIWebSocketTopic(CommitAnnouncementsTopic,
                                                           "commitAnnouncement",
                                                           "A slim notice of every new commit: timestamp, commitId, parentIds, revision and description."));

            webSocketServer.AddTopic(new POIWebSocketTopic(CommitsTopic,
                                                           "commit",
                                                           "Every new commit completely, as the command commit returns it; one larger than MaxPushedCommitSize as a commitAnnouncement."));

            webSocketServer.AddTopic(new POIWebSocketTopic(CommitSignaturesTopic,
                                                           "commitSignatures",
                                                           "The signatures added to a commit later, e.g. by a roaming network operator that checked its data."));

            webSocketServer.AddTopic(new POIWebSocketTopic(AdminStatusUpdatesTopic,
                                                           "adminStatusUpdate",
                                                           "Every change of the admin status of the roaming network and of the entities it embeds."));

            webSocketServer.AddTopic(new POIWebSocketTopic(StatusUpdatesTopic,
                                                           "statusUpdate",
                                                           "Every change of the status of the roaming network and of the entities it embeds."));

            webSocketServer.AddTopic(new POIWebSocketTopic(EnergyStatusUpdatesTopic,
                                                           "energyStatusUpdate",
                                                           "Every change of the energy status of EVSEs and charging stations: the power used and available, and the energy stored."));

            #endregion

            return webSocketServer;

        }

        #endregion

        #region (private) ConnectHistoryWebSocketServer(History, WebSocketServer)

        /// <summary>
        /// Push the commits and the status updates of the given history to the subscribers of its WebSocket server,
        /// until the API is disposed.
        /// </summary>
        private void ConnectHistoryWebSocketServer(RoamingNetworkHistory  History,
                                                   POIWebSocketServer     WebSocketServer)
        {

            OnRoamingNetworkCommitAnnouncementDelegate onCommitAnnouncement = announcement => {

                var announcementJSON = announcement.ToJSON();

                WebSocketServer.Push(CommitAnnouncementsTopic, announcementJSON);

                if (WebSocketServer.Sessions.Any(session => session.IsSubscribed(CommitsTopic)))
                {

                    var commitJSON = announcement.Commit.ToJSON();

                    if (Encoding.UTF8.GetByteCount(POIJSON.ToText(commitJSON)) <= MaxPushedCommitSize)
                        WebSocketServer.Push(CommitsTopic, commitJSON);

                    else
                        WebSocketServer.Push(CommitsTopic, announcementJSON, "commitAnnouncement");

                }

            };

            OnRoamingNetworkCommitSignaturesDelegate onCommitSignatures = (commit, newSignatures) =>
                WebSocketServer.Push(CommitSignaturesTopic,
                                     new JObject(
                                         new JProperty("commitId",    commit.Id.ToString()),
                                         new JProperty("signatures",  new JArray(newSignatures.Select(signature => signature.ToJSON())))
                                     ));

            OnPOIStatusUpdateDelegate onStatusUpdate = statusUpdate =>
                WebSocketServer.Push(statusUpdate.Kind switch {
                                         POIRuntimeStatusKind.AdminStatus   => AdminStatusUpdatesTopic,
                                         POIRuntimeStatusKind.EnergyStatus  => EnergyStatusUpdatesTopic,
                                         _                                  => StatusUpdatesTopic
                                     },
                                     statusUpdate.ToJSON());

            History.OnCommitAnnouncement          += onCommitAnnouncement;
            History.OnCommitSignatures            += onCommitSignatures;
            History.RuntimeStatus.OnStatusUpdate  += onStatusUpdate;

            historyDisconnects[History.RoamingNetworkId] = () => {
                History.OnCommitAnnouncement          -= onCommitAnnouncement;
                History.OnCommitSignatures            -= onCommitSignatures;
                History.RuntimeStatus.OnStatusUpdate  -= onStatusUpdate;
            };

        }

        #endregion

        #region (private) SignCommit(Request, History)

        /// <summary>
        /// Add the signatures of the request to a commit, as AddCommitSignatures() does for an HTTP request.
        /// </summary>
        private POIWebSocketMessage SignCommit(POIWebSocketRequest    Request,
                                               RoamingNetworkHistory  History)
        {

            if (Request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "The signer is unknown!");

            if (!RoamingNetworkCommitSignature.TryParseCommitSignatures(Request.Payload, out var commitId, out var signatures, out var errorResponse))
                return Request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse);

            var result = AddCommitSignatures(upgradeRequest, History, commitId, signatures);

            return result.Status switch {
                       CommitSignaturesStatus.Added or
                       CommitSignaturesStatus.Known      => Request.Respond(result.ToJSON()),
                       CommitSignaturesStatus.NotFound   => Request.Error(POIWebSocketErrorCodes.NotFound,       result.ErrorResponse),
                       CommitSignaturesStatus.Forbidden  => Request.Error(POIWebSocketErrorCodes.SecurityError,  result.ErrorResponse),
                       _                                 => Request.Error(POIWebSocketErrorCodes.SecurityError,  result.ErrorResponse)
                   };

        }

        #endregion

        #region (private) CommitChangeSet(Request, History)

        /// <summary>
        /// Commit the change set of the request to the version it was made for, and merge it with the other tips,
        /// as SubmitChangeSet() does for an HTTP request.
        /// </summary>
        private POIWebSocketMessage CommitChangeSet(POIWebSocketRequest    Request,
                                                    RoamingNetworkHistory  History)
        {

            if (AuthorizeChangeSetSubmission is null)
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "Submitting change sets is not enabled here!");

            if (Request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "The sender of the change set is unknown!");

            #region Parse the change set and the options

            RoamingNetworkChangeSet? changeSet       = null;
            String?                  changeSetError  = "The request must contain a change set!";

            var hasChangeSet = Request.IsBinary

                                   ? Request.Message.PayloadCBOR?.TryGetValue(CBORValue.FromText("changeSet"), out var changeSetCBOR) == true &&
                                     RoamingNetworkChangeSet.TryParse(changeSetCBOR, out changeSet, out changeSetError, ChangeSetLimits)

                                   : Request.Payload["changeSet"] is JObject changeSetJSON &&
                                     RoamingNetworkChangeSet.TryParse(changeSetJSON, out changeSet, out changeSetError, ChangeSetLimits);

            if (!hasChangeSet || changeSet is null)
                return Request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid change set: " + changeSetError);

            if (changeSet.RoamingNetworkId != History.RoamingNetworkId)
                return Request.Error(POIWebSocketErrorCodes.FormatViolation, $"The change set is for the roaming network '{changeSet.RoamingNetworkId}', not for '{History.RoamingNetworkId}'!");

            #endregion

            if (!AuthorizeChangeSetSubmission(upgradeRequest, changeSet))
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "The sender may not submit this change set!");

            var result = History.Commit(changeSet,
                                        VerifyChangeSetSignature);

            return result.IsCommitted

                       ? Request.Respond(CommittedJSON(result, History))

                       : Request.Respond(new JObject(
                             new JProperty("status",       result.Status.ToString()),
                             new JProperty("description",  result.ErrorResponse)
                         ));

        }

        #endregion

        #region (private) SetStatus(Request, History)

        /// <summary>
        /// Set the admin status or the status of an entity of the head version, as asked by the request.
        /// </summary>
        private POIWebSocketMessage SetStatus(POIWebSocketRequest    Request,
                                              RoamingNetworkHistory  History)
        {

            if (AuthorizeStatusUpdate is null)
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "Setting a status is not enabled here!");

            if (Request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "The sender of the status is unknown!");

            #region Parse the status update

            if (!POIStatusUpdate.TryParseKind(Request.Payload["kind"]?.Type == JTokenType.String ? Request.Payload["kind"]!.Value<String>() : null, out var kind))
                return Request.Error(POIWebSocketErrorCodes.FormatViolation, "The kind must be adminStatus, status or energyStatus!");

            // The value of an energy status is an object { used, available, storedEnergy? }.
            if (!Request.Payload.ParseMandatoryText("entityType", "entity type",           out String? entityType, out var errorResponse) ||
                !Request.Payload.ParseMandatoryText("entityId",   "entity identification", out String? entityId,   out errorResponse) ||
                !POIStatusUpdate.TryParseValue(kind, Request.Payload["value"], "status",  out String? value,      out errorResponse))
            {
                return Request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse);
            }

            var timestamp = TimeProvider.GetUtcNow();

            if (Request.Payload.ParseOptional("timestamp", "timestamp", out DateTimeOffset? givenTimestamp, out errorResponse))
            {

                if (errorResponse is not null)
                    return Request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse);

                timestamp = givenTimestamp!.Value;

            }

            if (Request.Payload.ParseOptional("dataSource", "data source", out String? dataSource, out errorResponse))
            {
                if (errorResponse is not null)
                    return Request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse);
            }

            #endregion

            if (!AuthorizeStatusUpdate(upgradeRequest,
                                       Request.Session.Login,
                                       new POIStatusUpdate(kind, entityType, entityId, timestamp, value, DataSource: dataSource)))
            {
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "The sender may not set this status!");
            }

            var result    = History.RuntimeStatus.TryUpdate(kind,
                                                            entityType,
                                                            entityId,
                                                            value,
                                                            timestamp,
                                                            out _,
                                                            out var description,
                                                            dataSource);

            var response  = new JObject(new JProperty("result", result.ToString()));

            if (description is not null)
                response.Add("description", description);

            if (History.RuntimeStatus.TryGet(kind, entityType, entityId, out var current))
                response.Add("current", current.ToJSON());

            return Request.Respond(response);

        }

        #endregion

        #region (private) PushStatusUpdates(Request, History, MaxStatusUpdates)

        /// <summary>
        /// Take the status updates another server pushes, e.g. the status set there, with their timestamps:
        /// the latest one of an entity wins, wherever it was set first.
        /// </summary>
        private POIWebSocketMessage PushStatusUpdates(POIWebSocketRequest    Request,
                                                      RoamingNetworkHistory  History,
                                                      Int32                  MaxStatusUpdates)
        {

            if (AuthorizeStatusPush is null)
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "Pushing status updates is not enabled here!");

            if (Request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "The sender of the status updates is unknown!");

            #region Parse the status updates

            if (Request.Payload["statusUpdates"] is not JArray statusUpdatesJSON)
                return Request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing status updates!");

            if (statusUpdatesJSON.Count > MaxStatusUpdates)
                return Request.Error(POIWebSocketErrorCodes.FormatViolation, $"At most {MaxStatusUpdates} status updates at once!");

            var statusUpdates = new List<POIStatusUpdate>();

            foreach (var statusUpdateJSON in statusUpdatesJSON)
            {

                if (statusUpdateJSON is not JObject statusUpdateObject ||
                    !POIStatusUpdate.TryParse(statusUpdateObject, out var statusUpdate, out _))
                {
                    return Request.Error(POIWebSocketErrorCodes.FormatViolation, $"Invalid status update {statusUpdates.Count}!");
                }

                statusUpdates.Add(statusUpdate);

            }

            #endregion

            var results = new JArray();

            foreach (var statusUpdate in statusUpdates)
            {

                var     result       = POIStatusUpdateResult.Refused;
                String? description  = "The sender may not set this status!";

                if (AuthorizeStatusPush(upgradeRequest, Request.Session.Login, statusUpdate))
                    result = History.RuntimeStatus.TryUpdate(statusUpdate, out description);

                var resultJSON = new JObject(new JProperty("result", result.ToString()));

                if (description is not null)
                    resultJSON.Add("description", description);

                results.Add(resultJSON);

            }

            return Request.Respond(new JObject(new JProperty("results", results)));

        }

        #endregion

        #region (private) TryGetSince(Request, History, out Since, out Max, out ErrorMessage)

        /// <summary>
        /// The "since" commit, by default the root, and the "max" number of commits of the request, or a request error.
        /// </summary>
        private static Boolean TryGetSince(POIWebSocketRequest                           Request,
                                           RoamingNetworkHistory                         History,
                                           out RoamingNetworkCommit_Id                   Since,
                                           out Int32                                     Max,
                                           [NotNullWhen(false)] out POIWebSocketMessage? ErrorMessage)
        {

            Since         = History.Root.Id;
            Max           = MaxCommitsPerRequest;
            ErrorMessage  = null;

            if (Request.Payload.ParseOptionalStruct("since",
                                                    "commit identification",
                                                    RoamingNetworkCommit_Id.TryParse,
                                                    out RoamingNetworkCommit_Id since,
                                                    out var errorResponse))
            {

                if (errorResponse is not null)
                {
                    ErrorMessage = Request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse);
                    return false;
                }

                Since = since;

            }

            if (Request.Payload.ParseOptional("max",
                                              "maximum number of commits",
                                              out Int32? max,
                                              out errorResponse))
            {

                if (errorResponse is not null || max < 1)
                {
                    ErrorMessage = Request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse ?? $"Invalid maximum number of commits '{max}'!");
                    return false;
                }

                Max = Math.Min(max!.Value, MaxCommitsPerRequest);

            }

            return true;

        }

        #endregion


        #region DisposeAsync()

        /// <summary>
        /// Dispose the WebSocket servers of the histories.
        /// </summary>
        public async ValueTask DisposeAsync()
        {

            foreach (var disconnect in historyDisconnects.Values)
                disconnect();

            historyDisconnects.Clear();

            foreach (var webSocketServer in historyWebSocketServers.Values)
                await webSocketServer.DisposeAsync();

            historyWebSocketServers.Clear();

            GC.SuppressFinalize(this);

        }

        #endregion

    }

}
