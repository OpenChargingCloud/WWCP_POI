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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The histories of roaming networks over a WebSocket: the commands of the HTTP API,
    /// as OCPP-J framed requests answered asynchronously, in JSON text or in CBOR binary frames.
    /// </summary>
    /// <remarks>
    /// GET ~/roamingNetworks/{roamingNetworkId}/history/ws   upgrade to the WebSocket of the history
    ///
    /// Action            Request payload                            Response payload
    /// head              {}                                         the metadata of the head commit
    /// snapshot          {}                                         the head commit and the complete roaming network
    /// commitInfos       { since?, max? }                           { commitInfos: [ … ] }
    /// commits           { since?, max? }                           { commits:     [ … ] }
    /// commit            { commitId }                               the complete commit
    /// commitChangeSet   { changeSet, expectedHead?, rebase? }      { status, … }: Published or AlreadyPublished with the metadata of
    ///                                                              the commit and "rebased", HeadMoved with the head, Refused
    /// runtimeStatus     {}                                         { runtimeStatus: [ … ] }: the current admin status and status
    /// topics            {}                                         { topics: [ { name, action, description } ] }
    /// subscribe         { topics: [ … ] }                          { topics: [ … ] }: all topics subscribed now
    /// unsubscribe       { topics: [ … ] }                          { topics: [ … ] }
    /// Unknown commits are answered with the request error NotFound, invalid requests with FormatViolation,
    /// change sets nobody may submit with SecurityError.
    ///
    /// Topic                  Push
    /// commitAnnouncements    commitAnnouncement   { timestamp, commitId, parentIds, revision, description? }
    /// adminStatusUpdates     adminStatusUpdate    { kind, entityType, entityId, timestamp, value, oldTimestamp?, oldValue?, dataSource? }
    /// statusUpdates          statusUpdate         the same, for the status
    /// </remarks>
    public partial class POI_API : IAsyncDisposable
    {

        #region Data

        /// <summary>
        /// The topic of slim notices of new commits.
        /// </summary>
        public const String CommitAnnouncementsTopic  = "commitAnnouncements";

        /// <summary>
        /// The topic of admin status updates.
        /// </summary>
        public const String AdminStatusUpdatesTopic   = "adminStatusUpdates";

        /// <summary>
        /// The topic of status updates.
        /// </summary>
        public const String StatusUpdatesTopic        = "statusUpdates";

        private readonly ConcurrentDictionary<RoamingNetwork_Id, POIWebSocketServer>  historyWebSocketServers  = [];
        private readonly ConcurrentDictionary<RoamingNetwork_Id, Action>              historyDisconnects       = [];

        private AuthenticatePOIWebSocketClientDelegate? authenticateWebSocketClient;

        #endregion

        #region Properties

        /// <summary>
        /// Decides who the client opening the WebSocket of a history is; its upgrade request is refused
        /// when it returns no login. Without it, everybody may open it, as everybody may read a history over HTTP.
        /// Change sets are submitted over it as decided by AuthorizeChangeSetSubmission, given the upgrade request.
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

            #region GET ~/roamingNetworks/{roamingNetworkId}/history/ws

            AddHandler(HTTPPath.Root + "roamingNetworks/{roamingNetworkId}/history/ws",
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

            webSocketServer.AddCommand("snapshot", request =>

                Task.FromResult(
                    request.Respond(History.ToSnapshotJSON,
                                    History.ToSnapshotCBOR)
                )

            );

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

            webSocketServer.AddCommand("commits", request =>

                Task.FromResult(

                    !TryGetSince(request, History, out var since, out var max, out var errorMessage)
                        ? errorMessage

                        : History.TryGetCommitsSince(since, out var commits, out var error, max)
                              ? request.Respond(new JObject(new JProperty("commits", new JArray(commits.Select(commit => commit.ToJSON())))))
                              : request.Error(POIWebSocketErrorCodes.NotFound, error)

                )

            );

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

            #region runtimeStatus

            webSocketServer.AddCommand("runtimeStatus", request =>

                Task.FromResult(
                    request.Respond(new JObject(new JProperty("runtimeStatus", new JArray(History.RuntimeStatus.All.Select(status => status.ToJSON())))))
                )

            );

            #endregion

            #region Topics

            webSocketServer.AddTopic(new POIWebSocketTopic(CommitAnnouncementsTopic,
                                                           "commitAnnouncement",
                                                           "A slim notice of every new commit: timestamp, commitId, parentIds, revision and description."));

            webSocketServer.AddTopic(new POIWebSocketTopic(AdminStatusUpdatesTopic,
                                                           "adminStatusUpdate",
                                                           "Every change of the admin status of the roaming network and of its charging station operators."));

            webSocketServer.AddTopic(new POIWebSocketTopic(StatusUpdatesTopic,
                                                           "statusUpdate",
                                                           "Every change of the status of the roaming network and of its charging station operators."));

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

            OnRoamingNetworkCommitAnnouncementDelegate onCommitAnnouncement = announcement =>
                WebSocketServer.Push(CommitAnnouncementsTopic, announcement.ToJSON());

            OnPOIStatusUpdateDelegate onStatusUpdate = statusUpdate =>
                WebSocketServer.Push(statusUpdate.Kind == POIRuntimeStatusKind.AdminStatus
                                         ? AdminStatusUpdatesTopic
                                         : StatusUpdatesTopic,
                                     statusUpdate.ToJSON());

            History.OnCommitAnnouncement          += onCommitAnnouncement;
            History.RuntimeStatus.OnStatusUpdate  += onStatusUpdate;

            historyDisconnects[History.RoamingNetworkId] = () => {
                History.OnCommitAnnouncement          -= onCommitAnnouncement;
                History.RuntimeStatus.OnStatusUpdate  -= onStatusUpdate;
            };

        }

        #endregion

        #region (private) CommitChangeSet(Request, History)

        /// <summary>
        /// Publish the change set of the request as the new head commit,
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

            RoamingNetworkCommit_Id? expectedHead = null;

            if (Request.Payload.ParseOptionalStruct("expectedHead",
                                                    "expected head commit identification",
                                                    RoamingNetworkCommit_Id.TryParse,
                                                    out RoamingNetworkCommit_Id expectedHeadValue,
                                                    out var errorResponse))
            {

                if (errorResponse is not null)
                    return Request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse);

                expectedHead = expectedHeadValue;

            }

            if (Request.Payload.ParseOptional("rebase",
                                              "rebase",
                                              out Boolean? rebase,
                                              out errorResponse))
            {
                if (errorResponse is not null)
                    return Request.Error(POIWebSocketErrorCodes.FormatViolation, errorResponse);
            }

            #endregion

            if (!AuthorizeChangeSetSubmission(upgradeRequest, changeSet))
                return Request.Error(POIWebSocketErrorCodes.SecurityError, "The sender may not submit this change set!");

            var result = History.Publish(changeSet,
                                         expectedHead,
                                         rebase ?? false,
                                         VerifyChangeSetSignature);

            return result.Status switch {

                       RoamingNetworkPublishStatus.Published or
                       RoamingNetworkPublishStatus.AlreadyPublished  => Request.Respond(PublishedJSON(result)),

                       RoamingNetworkPublishStatus.HeadMoved         => Request.Respond(new JObject(
                                                                            new JProperty("status",       result.Status.ToString()),
                                                                            new JProperty("description",  result.ErrorResponse),
                                                                            new JProperty("head",         new RoamingNetworkCommitInfo(result.Head.Commit).ToJSON())
                                                                        )),

                       _                                             => Request.Respond(new JObject(
                                                                            new JProperty("status",       result.Status.ToString()),
                                                                            new JProperty("description",  result.ErrorResponse)
                                                                        ))

                   };

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
