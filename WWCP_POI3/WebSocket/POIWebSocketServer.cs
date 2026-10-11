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

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Decides who the client asking for a WebSocket upgrade is.
    /// </summary>
    /// <param name="Request">The HTTP request asking for the upgrade.</param>
    /// <param name="CancellationToken">A cancellation token.</param>
    /// <returns>The login of the client, or null to refuse the upgrade with 401 Unauthorized.</returns>
    public delegate Task<String?> AuthenticatePOIWebSocketClientDelegate(HTTPRequest        Request,
                                                                         CancellationToken  CancellationToken);


    /// <summary>
    /// A WebSocket server speaking OCPP-J framed messages, see POIWebSocketMessage:
    /// requests are processed asynchronously, several at a time per connection,
    /// and answered in the format they came in, JSON text or CBOR binary.
    /// </summary>
    /// <remarks>
    /// It is lent to an HTTP path via WebSocketUpgrade.For() and never started itself.
    /// While all request slots of a connection are taken, no further message is read from it.
    /// Responses, errors and sends of the client are not answered: this server sends no requests.
    ///
    /// Clients subscribe to topics: "topics" lists them, "subscribe" and "unsubscribe" take
    /// { "topics": [ ... ] } and answer with all topics subscribed now. Pushes are sends, [6, "p1", action, payload],
    /// in the format of the last subscribe request, and may arrive before its response.
    /// Subscriptions end with their connection. A client that does not keep up with its pushes,
    /// or whose push can not be sent, is disconnected: it catches up when it connects again.
    ///
    /// Messages of the same action aggregate: a request whose payload is { "batch": [ p1, p2, … ] }
    /// is processed as one request per payload, in their order, and answered with { "batch": [ r1, r2, … ] },
    /// where an error is { "error": { code, description, details } }. Pushes of the same action
    /// waiting together are sent as one push with { "batch": [ … ] }, up to the maxBatchSize of the
    /// subscribe request; PushCoalescingDelay lets more of them gather.
    /// </remarks>
    public class POIWebSocketServer : AWebSocketServer
    {

        #region Data

        /// <summary>
        /// The default maximum number of requests processed at the same time per connection.
        /// </summary>
        public const UInt16 DefaultMaxConcurrentRequests = 8;

        /// <summary>
        /// The default maximum number of pushes waiting to be sent per connection.
        /// </summary>
        public const Int32  DefaultMaxQueuedPushes       = 1000;

        /// <summary>
        /// The default maximum number of payloads of one batch.
        /// </summary>
        public const Int32  DefaultMaxBatchSize          = 1000;

        /// <summary>
        /// The default maximum size of the pushes waiting to be sent per connection, as JSON text: 64 MiB.
        /// </summary>
        public const Int64  DefaultMaxQueuedPushBytes    = 64 * 1024 * 1024;

        private readonly ConcurrentDictionary<String, POIWebSocketCommandDelegate>             commands  = new (StringComparer.Ordinal);
        private readonly ConcurrentDictionary<String, POIWebSocketTopic>                       topics    = new (StringComparer.Ordinal);
        private readonly ConcurrentDictionary<WebSocketServerConnection, POIWebSocketSession>  sessions  = [];

        private AuthenticatePOIWebSocketClientDelegate? authenticateClient;

        #endregion

        #region Properties

        /// <summary>
        /// The maximum number of requests processed at the same time per connection,
        /// for connections opened from now on.
        /// </summary>
        public UInt16                                   MaxConcurrentRequests    { get; set; } = DefaultMaxConcurrentRequests;

        /// <summary>
        /// The maximum depth of a message, including the array and the payload.
        /// </summary>
        public Int32                                    MaxMessageDepth          { get; set; } = POIWebSocketMessage.DefaultMaxDepth;

        /// <summary>
        /// The maximum number of pushes waiting to be sent per connection, for connections opened from now on.
        /// A client with more is disconnected.
        /// </summary>
        public Int32                                    MaxQueuedPushes          { get; set; } = DefaultMaxQueuedPushes;

        /// <summary>
        /// The maximum size of the pushes waiting to be sent per connection, as JSON text.
        /// A client with more is disconnected.
        /// </summary>
        public Int64                                    MaxQueuedPushBytes       { get; set; } = DefaultMaxQueuedPushBytes;

        /// <summary>
        /// The maximum number of payloads of one batch, of requests and of pushes.
        /// </summary>
        public Int32                                    MaxBatchSize             { get; set; } = DefaultMaxBatchSize;

        /// <summary>
        /// How long pushes wait for more of their kind before they are sent, to be sent as one batch.
        /// By default not at all: only what is waiting anyway is aggregated.
        /// </summary>
        public TimeSpan                                 PushCoalescingDelay      { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Decides who the client asking for an upgrade is. Setting it requires authentication.
        /// Without it, the HTTP Basic and TOTP logins of the WebSocket server are used, when authentication is required.
        /// </summary>
        public AuthenticatePOIWebSocketClientDelegate?  AuthenticateClient
        {

            get
            {
                return authenticateClient;
            }

            set
            {

                authenticateClient = value;

                if (value is not null)
                    RequireAuthentication = true;

            }

        }

        /// <summary>
        /// The actions this server knows.
        /// </summary>
        public IEnumerable<String>                      Actions
            => commands.Keys;

        /// <summary>
        /// The topics clients can subscribe to.
        /// </summary>
        public IEnumerable<POIWebSocketTopic>           Topics
            => topics.Values.OrderBy(topic => topic.Name, StringComparer.Ordinal);

        /// <summary>
        /// The sessions of the open connections.
        /// </summary>
        public IEnumerable<POIWebSocketSession>         Sessions
            => sessions.Values;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new POI WebSocket server, to be lent to an HTTP path.
        /// </summary>
        /// <param name="HTTPServiceName">An optional HTTP service name.</param>
        /// <param name="RequireAuthentication">Whether clients must authenticate; by default not.</param>
        /// <param name="AuthenticateClient">An optional delegate deciding who the client is; it requires authentication.</param>
        /// <param name="LoggerFactory">An optional logger factory.</param>
        public POIWebSocketServer(String?                                  HTTPServiceName         = null,
                                  Boolean                                  RequireAuthentication   = false,
                                  AuthenticatePOIWebSocketClientDelegate?  AuthenticateClient      = null,
                                  ILoggerFactory?                          LoggerFactory           = null)

            : base(TCPPort:                IPPort.Zero,
                   HTTPServerName:         HTTPServiceName ?? "WWCP POI WebSocket Server",
                   RequireAuthentication:  RequireAuthentication || AuthenticateClient is not null,
                   LoggerFactory:          LoggerFactory,
                   AutoStart:              false)

        {

            this.authenticateClient = AuthenticateClient;

            // However the connection ended: its session ends with it, and whatever
            // is still processed for it is cancelled.
            OnTCPConnectionClosed += (timestamp, server, connection, eventTrackingId, reason, cancellationToken) => {

                if (sessions.TryRemove(connection, out var session))
                    session.Close();

                return Task.CompletedTask;

            };

            #region topics

            AddCommand("topics", request =>

                Task.FromResult(
                    request.Respond(new JObject(new JProperty("topics", new JArray(Topics.Select(topic => topic.ToJSON())))))
                )

            );

            #endregion

            #region subscribe

            AddCommand("subscribe", request => {

                if (!TryGetTopics(request, out var topicNames, out var errorMessage))
                    return Task.FromResult(errorMessage);

                var maxPushBatchSize = MaxBatchSize;

                if (request.Payload.ParseOptional("maxBatchSize",
                                                  "maximum batch size",
                                                  out Int32? givenMaxBatchSize,
                                                  out var errorResponse))
                {

                    if (errorResponse is not null || givenMaxBatchSize < 1)
                        return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation,
                                                             errorResponse ?? $"Invalid maximum batch size '{givenMaxBatchSize}'!"));

                    maxPushBatchSize = Math.Min(givenMaxBatchSize!.Value, MaxBatchSize);

                }

                request.Session.PushBinary        = request.IsBinary;
                request.Session.MaxPushBatchSize  = Math.Max(maxPushBatchSize, 1);

                foreach (var topicName in topicNames)
                    request.Session.Subscribe(topicName);

                StartPushing(request.Session);

                return Task.FromResult(SubscriptionsResponse(request));

            });

            #endregion

            #region unsubscribe

            AddCommand("unsubscribe", request => {

                if (!TryGetTopics(request, out var topicNames, out var errorMessage))
                    return Task.FromResult(errorMessage);

                foreach (var topicName in topicNames)
                    request.Session.Unsubscribe(topicName);

                return Task.FromResult(SubscriptionsResponse(request));

            });

            #endregion

        }

        #endregion


        #region AddCommand(Action, Handler)

        /// <summary>
        /// Process requests of the given action with the given handler.
        /// </summary>
        /// <param name="Action">The action.</param>
        /// <param name="Handler">The command handler.</param>
        public void AddCommand(String                       Action,
                               POIWebSocketCommandDelegate  Handler)
        {

            if (Action.IsNullOrEmpty())
                throw new ArgumentException("An action must not be empty!", nameof(Action));

            commands[Action] = Handler;

        }

        #endregion

        #region AddTopic(Topic)

        /// <summary>
        /// Let clients subscribe to the given topic.
        /// </summary>
        /// <param name="Topic">A topic.</param>
        public void AddTopic(POIWebSocketTopic Topic)
        {

            if (Topic.Name.IsNullOrEmpty() || Topic.Action.IsNullOrEmpty())
                throw new ArgumentException("A topic needs a name and an action!", nameof(Topic));

            topics[Topic.Name] = Topic;

        }

        #endregion

        #region Push(TopicName, Payload, Action = null)

        /// <summary>
        /// Queue the given payload for every connection subscribed to the given topic, without waiting.
        /// A connection with more than MaxQueuedPushes pushes or MaxQueuedPushBytes waiting is disconnected.
        /// </summary>
        /// <param name="TopicName">The name of a topic.</param>
        /// <param name="Payload">The payload of the push.</param>
        /// <param name="Action">The action of the push, when not the one of the topic.</param>
        /// <returns>The number of connections it was queued for.</returns>
        public Int32 Push(String   TopicName,
                          JObject  Payload,
                          String?  Action   = null)
        {

            if (!topics.TryGetValue(TopicName, out var topic))
                throw new ArgumentException($"The topic '{TopicName}' is unknown!", nameof(TopicName));

            var action  = Action ?? topic.Action;
            var size    = -1L;
            var queued  = 0;

            foreach (var session in sessions.Values)
            {

                if (!session.IsSubscribed(topic.Name))
                    continue;

                // Measured once, and only when anybody listens.
                if (size < 0)
                    size = Encoding.UTF8.GetByteCount(Payload.ToString(Formatting.None));

                String? overload = null;

                if (session.AddQueuedPushBytes(size) > MaxQueuedPushBytes)
                    overload = $"More than {MaxQueuedPushBytes} bytes of pushes are waiting!";

                else if (session.Pushes.Writer.TryWrite((action, Payload, size)))
                    queued++;

                else
                    overload = $"More than {MaxQueuedPushes} pushes are waiting!";

                // Off the books at once, so that nothing more is queued for it,
                // and closed elsewhere: this may run while a history is locked.
                if (overload is not null && sessions.TryRemove(session.Connection, out _))
                    _ = Task.Run(() => Disconnect(session, overload));

            }

            return queued;

        }

        #endregion

        #region SendMessage(Session, Message, IsBinary, CancellationToken = default)

        /// <summary>
        /// Send the given message on the connection of the given session,
        /// as CBOR in a binary frame or as JSON in a text frame.
        /// </summary>
        /// <param name="Session">The session of a connection.</param>
        /// <param name="Message">The message to send.</param>
        /// <param name="IsBinary">Whether to send it as CBOR in a binary frame.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public Task<SentStatus> SendMessage(POIWebSocketSession  Session,
                                            POIWebSocketMessage  Message,
                                            Boolean              IsBinary,
                                            CancellationToken    CancellationToken   = default)

            => SendMessage(Session.Connection,
                           Message,
                           IsBinary,
                           CancellationToken);

        private Task<SentStatus> SendMessage(WebSocketServerConnection  Connection,
                                             POIWebSocketMessage        Message,
                                             Boolean                    IsBinary,
                                             CancellationToken          CancellationToken)

            => IsBinary
                   ? SendBinaryMessage(Connection, Message.ToCBORBytes(), CancellationToken: CancellationToken)
                   : SendTextMessage  (Connection, Message.ToJSONText(),  CancellationToken: CancellationToken);

        #endregion


        #region (protected override) AuthenticateAsync(Connection, Request, CancellationToken)

        /// <summary>
        /// Who the client asking for an upgrade is: by AuthenticateClient, if given.
        /// </summary>
        protected override Task<String?> AuthenticateAsync(WebSocketServerConnection  Connection,
                                                           HTTPRequest                Request,
                                                           CancellationToken          CancellationToken)

            => authenticateClient is not null
                   ? authenticateClient(Request, CancellationToken)
                   : base.AuthenticateAsync(Connection, Request, CancellationToken);

        #endregion

        #region (override) ProcessTextMessage  (Timestamp, Server, Connection, EventTrackingId, TextFrame,   TextMessage,   CancellationToken)

        /// <summary>
        /// Process a message in JSON text.
        /// </summary>
        public override Task ProcessTextMessage(DateTimeOffset             Timestamp,
                                                AWebSocketServer           Server,
                                                WebSocketServerConnection  Connection,
                                                EventTracking_Id           EventTrackingId,
                                                WebSocketFrame             TextFrame,
                                                String                     TextMessage,
                                                CancellationToken          CancellationToken)

            => POIWebSocketMessage.TryParse(TextMessage, out var message, out var errorMessage, MaxMessageDepth)
                   ? Dispatch   (GetSession(Connection), message,      IsBinary: false, CancellationToken)
                   : SendMessage(Connection,             errorMessage, IsBinary: false, CancellationToken);

        #endregion

        #region (override) ProcessBinaryMessage(Timestamp, Server, Connection, EventTrackingId, BinaryFrame, BinaryMessage, CancellationToken)

        /// <summary>
        /// Process a message in CBOR.
        /// </summary>
        public override Task ProcessBinaryMessage(DateTimeOffset             Timestamp,
                                                  AWebSocketServer           Server,
                                                  WebSocketServerConnection  Connection,
                                                  EventTracking_Id           EventTrackingId,
                                                  WebSocketFrame             BinaryFrame,
                                                  Byte[]                     BinaryMessage,
                                                  CancellationToken          CancellationToken)

            => POIWebSocketMessage.TryParse(BinaryMessage, out var message, out var errorMessage, MaxMessageDepth)
                   ? Dispatch   (GetSession(Connection), message,      IsBinary: true, CancellationToken)
                   : SendMessage(Connection,             errorMessage, IsBinary: true, CancellationToken);

        #endregion


        #region (private) GetSession(Connection)

        private POIWebSocketSession GetSession(WebSocketServerConnection Connection)

            => sessions.GetOrAdd(Connection,
                                 connection => new POIWebSocketSession(connection,
                                                                       Math.Max(MaxConcurrentRequests, (UInt16) 1),
                                                                       Math.Max(MaxQueuedPushes,       1),
                                                                       Math.Max(MaxBatchSize,          1)));

        #endregion

        #region (private) TryGetTopics(Request, out TopicNames, out ErrorMessage)

        /// <summary>
        /// The known topics named in the "topics" array of the request, or a request error.
        /// </summary>
        private Boolean TryGetTopics(POIWebSocketRequest                              Request,
                                     [NotNullWhen(true)]  out IReadOnlyList<String>?  TopicNames,
                                     [NotNullWhen(false)] out POIWebSocketMessage?    ErrorMessage)
        {

            TopicNames    = null;
            ErrorMessage  = null;

            if (Request.Payload["topics"] is not JArray topicsJSON ||
                topicsJSON.Count == 0 ||
                topicsJSON.Any(topic => topic.Type != JTokenType.String))
            {
                ErrorMessage = Request.Error(POIWebSocketErrorCodes.FormatViolation, "The topics must be a non-empty array of texts!");
                return false;
            }

            var names    = topicsJSON.Select(topic => topic.Value<String>()!).Distinct(StringComparer.Ordinal).ToArray();
            var unknown  = names.Where(name => !topics.ContainsKey(name)).ToArray();

            if (unknown.Length > 0)
            {
                ErrorMessage = Request.Error(POIWebSocketErrorCodes.PropertyConstraintViolation,
                                             $"Unknown topics: {String.Join(", ", unknown)}!",
                                             new JObject(new JProperty("unknownTopics", new JArray(unknown))));
                return false;
            }

            TopicNames = names;
            return true;

        }

        #endregion

        #region (private static) SubscriptionsResponse(Request)

        private static POIWebSocketMessage SubscriptionsResponse(POIWebSocketRequest Request)

            => Request.Respond(new JObject(new JProperty("topics", new JArray(Request.Session.Subscriptions))));

        #endregion

        #region (private) StartPushing(Session)

        /// <summary>
        /// Send the pushes of the given connection in their order, from its first subscription
        /// until it is closed: pushes of the same action waiting together as one batch.
        /// A push that can not be sent disconnects the client.
        /// </summary>
        private void StartPushing(POIWebSocketSession Session)
        {

            if (!Session.TryStartPushing())
                return;

            _ = Task.Run(async () => {

                try
                {

                    var reader = Session.Pushes.Reader;

                    while (await reader.WaitToReadAsync(Session.Closed))
                    {

                        if (PushCoalescingDelay > TimeSpan.Zero)
                            await Task.Delay(PushCoalescingDelay, Session.Closed);

                        while (reader.TryRead(out var push))
                        {

                            var payloads  = new List<JObject> { push.Payload };
                            var size      = push.Size;

                            while (payloads.Count < Session.MaxPushBatchSize &&
                                   reader.TryPeek(out var next)               &&
                                   next.Action == push.Action                 &&
                                   reader.TryRead(out next))
                            {
                                payloads.Add(next.Payload);
                                size += next.Size;
                            }

                            Session.AddQueuedPushBytes(-size);

                            var sentStatus = await SendMessage(Session,
                                                               POIWebSocketMessage.Send(Session.NextPushId(),
                                                                                        push.Action,
                                                                                        payloads.Count == 1
                                                                                            ? push.Payload
                                                                                            : new JObject(new JProperty("batch", new JArray(payloads)))),
                                                               Session.PushBinary,
                                                               Session.Closed);

                            if (sentStatus != SentStatus.Success)
                            {
                                await Disconnect(Session, $"A push could not be sent: {sentStatus}!");
                                return;
                            }

                        }
                    }

                }
                catch (OperationCanceledException) when (Session.Closed.IsCancellationRequested)
                { }
                catch (Exception e)
                {
                    Logger.LogError(e, "Exception while pushing on a POI WebSocket.");
                    await Disconnect(Session, "A push failed!");
                }

            });

        }

        #endregion

        #region (private) Disconnect(Session, Reason)

        /// <summary>
        /// End the given session at once, so that nothing more is queued for it, and close its connection.
        /// </summary>
        private async Task Disconnect(POIWebSocketSession  Session,
                                      String               Reason)
        {

            sessions.TryRemove(Session.Connection, out _);
            Session.Close();

            try
            {
                await Session.Connection.Close(WebSocketFrame.ClosingStatusCode.PolicyViolation, Reason);
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Could not close a POI WebSocket connection: {Reason}", Reason);
            }

        }

        #endregion

        #region (private) Process(Handler, Request)

        /// <summary>
        /// Process the given request with the given handler: a batch as one request per payload, in their order.
        /// </summary>
        private async Task<POIWebSocketMessage> Process(POIWebSocketCommandDelegate  Handler,
                                                        POIWebSocketRequest          Request)
        {

            if (!(Request.Payload.Count == 1 && Request.Payload["batch"] is JArray batch))
                return await Handler(Request);

            if (batch.Count == 0 || batch.Count > MaxBatchSize)
                return Request.Error(POIWebSocketErrorCodes.FormatViolation, $"A batch must have 1 to {MaxBatchSize} payloads!");

            // The CBOR payloads too, as some handlers read them, e.g. a change set.
            IReadOnlyList<CBORValue>? batchCBOR = null;

            if (Request.Message.PayloadCBOR is CBORValue payloadCBOR &&
                payloadCBOR.TryGetValue(CBORValue.FromText("batch"), out var batchValue) &&
                batchValue.Kind == CBORValueKind.Array &&
                batchValue.AsArray().Count == batch.Count)
            {
                batchCBOR = batchValue.AsArray();
            }

            var results = new JArray();

            for (var index = 0; index < batch.Count; index++)
            {

                if (batch[index] is not JObject itemJSON)
                {
                    results.Add(BatchError(POIWebSocketErrorCodes.FormatViolation, "A payload of a batch must be an object!", null));
                    continue;
                }

                var item = new POIWebSocketRequest(Request.Session,
                                                   POIWebSocketMessage.Request(Request.MessageId,
                                                                               Request.Action,
                                                                               itemJSON,
                                                                               batchCBOR?[index] is CBORValue itemCBOR && itemCBOR.Kind == CBORValueKind.Map
                                                                                   ? itemCBOR
                                                                                   : (CBORValue?) null),
                                                   Request.IsBinary,
                                                   Request.CancellationToken);

                POIWebSocketMessage answer;

                try
                {
                    answer = await Handler(item);
                }
                catch (OperationCanceledException) when (Request.CancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Exception while processing a batch of the POI WebSocket request '{Action}'.", Request.Action);
                    answer = item.Error(POIWebSocketErrorCodes.InternalError, $"The request '{Request.Action}' could not be processed!");
                }

                results.Add(answer.IsError
                                ? BatchError(answer.ErrorCode!, answer.ErrorDescription, answer.ErrorDetails)
                                : answer.GetPayloadJSON() ?? []);

            }

            return Request.Respond(new JObject(new JProperty("batch", results)));

        }

        #endregion

        #region (private static) BatchError(ErrorCode, ErrorDescription, ErrorDetails)

        /// <summary>
        /// The answer to one payload of a batch that failed: { "error": { code, description, details } }.
        /// </summary>
        private static JObject BatchError(String    ErrorCode,
                                          String?   ErrorDescription,
                                          JObject?  ErrorDetails)

            => new (
                   new JProperty("error", new JObject(
                       new JProperty("code",         ErrorCode),
                       new JProperty("description",  ErrorDescription ?? ""),
                       new JProperty("details",      ErrorDetails     ?? [])
                   ))
               );

        #endregion

        #region (private) Dispatch(Session, Message, IsBinary, CancellationToken)

        /// <summary>
        /// Start processing a request, and return once it is started:
        /// its answer is sent when it is ready, maybe after the answers to later requests.
        /// </summary>
        private async Task Dispatch(POIWebSocketSession  Session,
                                    POIWebSocketMessage  Message,
                                    Boolean              IsBinary,
                                    CancellationToken    CancellationToken)
        {

            // This server sends no requests, so there are no responses to wait for,
            // and a send is never answered.
            if (Message.MessageType != POIWebSocketMessageType.Request)
                return;

            if (!commands.TryGetValue(Message.Action!, out var handler))
            {
                await SendMessage(Session.Connection,
                                  POIWebSocketMessage.RequestError(Message.MessageId,
                                                                   POIWebSocketErrorCodes.NotImplemented,
                                                                   $"The action '{Message.Action}' is unknown here!"),
                                  IsBinary,
                                  CancellationToken);
                return;
            }

            if (!Session.TryStartRequest(Message.MessageId))
            {
                await SendMessage(Session.Connection,
                                  POIWebSocketMessage.RequestError(Message.MessageId,
                                                                   POIWebSocketErrorCodes.RpcFrameworkError,
                                                                   $"A request with the identification '{Message.MessageId}' is still being processed!"),
                                  IsBinary,
                                  CancellationToken);
                return;
            }

            try
            {
                // While all slots are taken, the connection is not read any further.
                await Session.RequestSlots.WaitAsync(CancellationToken);
            }
            catch (OperationCanceledException)
            {
                Session.EndRequest(Message.MessageId);
                return;
            }

            var request = new POIWebSocketRequest(Session,
                                                  Message,
                                                  IsBinary,
                                                  Session.Closed);

            _ = Task.Run(async () => {

                var ended = false;

                try
                {

                    POIWebSocketMessage answer;

                    try
                    {
                        answer = await Process(handler, request);
                    }
                    catch (OperationCanceledException) when (Session.Closed.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Exception while processing the POI WebSocket request '{Action}'.", Message.Action);
                        answer = request.Error(POIWebSocketErrorCodes.InternalError, $"The request '{Message.Action}' could not be processed!");
                    }

                    // Free the identification before the client has the answer,
                    // so that it can use it again as soon as it has.
                    Session.EndRequest(Message.MessageId);
                    ended = true;

                    await SendMessage(Session.Connection,
                                      answer,
                                      IsBinary,
                                      Session.Closed);

                }
                catch (OperationCanceledException) when (Session.Closed.IsCancellationRequested)
                { }
                catch (Exception e)
                {
                    Logger.LogError(e, "Exception while answering the POI WebSocket request '{Action}'.", Message.Action);
                }
                finally
                {

                    if (!ended)
                        Session.EndRequest(Message.MessageId);

                    Session.RequestSlots.Release();

                }

            });

        }

        #endregion

    }

}
