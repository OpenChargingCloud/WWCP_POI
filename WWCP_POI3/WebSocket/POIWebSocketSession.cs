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
using System.Threading.Channels;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What a POI WebSocket server keeps about one connection, for as long as it is open.
    /// </summary>
    public sealed class POIWebSocketSession
    {

        #region Data

        private readonly ConcurrentDictionary<String, Byte>  pendingRequests  = new (StringComparer.Ordinal);
        private readonly ConcurrentDictionary<String, Byte>  subscriptions    = new (StringComparer.Ordinal);
        private readonly CancellationTokenSource             closed           = new ();
        private          Int64                               pushCounter;
        private          Int64                               queuedPushBytes;
        private          Int32                               pushing;

        #endregion

        #region Properties

        /// <summary>
        /// The WebSocket connection.
        /// </summary>
        public WebSocketServerConnection  Connection           { get; }

        /// <summary>
        /// The HTTP request that asked for the upgrade, e.g. with the credentials of the client.
        /// </summary>
        public HTTPRequest?               UpgradeRequest
            => Connection.HTTPRequest;

        /// <summary>
        /// The login of the authenticated client, or null.
        /// </summary>
        public String?                    Login
            => Connection.Login;

        /// <summary>
        /// Limits the number of requests processed at the same time;
        /// while all are taken, no further message is read from the connection.
        /// </summary>
        internal SemaphoreSlim            RequestSlots         { get; }

        /// <summary>
        /// Cancelled when the connection is closed.
        /// </summary>
        public CancellationToken          Closed
            => closed.Token;

        /// <summary>
        /// The identifications of the requests still being processed.
        /// </summary>
        public IEnumerable<String>        PendingRequests
            => pendingRequests.Keys;

        /// <summary>
        /// The topics this connection subscribed to, in their ordinal order.
        /// </summary>
        public IEnumerable<String>        Subscriptions
            => subscriptions.Keys.Order(StringComparer.Ordinal);

        /// <summary>
        /// Whether pushes go out as CBOR in binary frames: as the last subscribe request came in.
        /// </summary>
        public Boolean                    PushBinary           { get; internal set; }

        /// <summary>
        /// The most pushes of the same action sent as one batch: as the last subscribe request asked,
        /// at most the MaxBatchSize of the server; 1 sends every push on its own.
        /// </summary>
        public Int32                      MaxPushBatchSize     { get; internal set; }

        /// <summary>
        /// The actions and payloads of the pushes waiting to be sent, in their order.
        /// When it is full, the client does not keep up and its connection is closed.
        /// </summary>
        internal Channel<(String Action, JObject Payload, Int64 Size)>  Pushes  { get; }

        /// <summary>
        /// The size of the pushes waiting to be sent, as JSON text.
        /// </summary>
        public Int64                      QueuedPushBytes
            => Interlocked.Read(ref queuedPushBytes);

        #endregion

        #region Constructor(s)

        internal POIWebSocketSession(WebSocketServerConnection  Connection,
                                     UInt16                     MaxConcurrentRequests,
                                     Int32                      MaxQueuedPushes,
                                     Int32                      MaxPushBatchSize)
        {

            this.Connection        = Connection;
            this.RequestSlots      = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
            this.MaxPushBatchSize  = MaxPushBatchSize;
            this.Pushes            = Channel.CreateBounded<(String, JObject, Int64)>(new BoundedChannelOptions(MaxQueuedPushes) {
                                     SingleReader  = true,
                                     FullMode      = BoundedChannelFullMode.Wait
                                 });

        }

        #endregion


        #region (internal) TryStartRequest(MessageId)

        /// <summary>
        /// Mark the request with the given identification as being processed,
        /// unless a request with this identification is processed already.
        /// </summary>
        internal Boolean TryStartRequest(String MessageId)

            => pendingRequests.TryAdd(MessageId, 0);

        #endregion

        #region (internal) EndRequest(MessageId)

        /// <summary>
        /// The request with the given identification is answered.
        /// </summary>
        internal void EndRequest(String MessageId)

            => pendingRequests.TryRemove(MessageId, out _);

        #endregion

        #region (internal) Subscribe  (Topic)

        /// <summary>
        /// Subscribe to the given topic.
        /// </summary>
        internal void Subscribe(String Topic)

            => subscriptions.TryAdd(Topic, 0);

        #endregion

        #region (internal) Unsubscribe(Topic)

        /// <summary>
        /// Unsubscribe from the given topic.
        /// </summary>
        internal void Unsubscribe(String Topic)

            => subscriptions.TryRemove(Topic, out _);

        #endregion

        #region IsSubscribed(Topic)

        /// <summary>
        /// Whether this connection subscribed to the given topic.
        /// </summary>
        public Boolean IsSubscribed(String Topic)

            => subscriptions.ContainsKey(Topic);

        #endregion

        #region (internal) NextPushId()

        /// <summary>
        /// The identification of the next push of this connection: p1, p2, …
        /// </summary>
        internal String NextPushId()

            => "p" + Interlocked.Increment(ref pushCounter);

        #endregion

        #region (internal) AddQueuedPushBytes(Size)

        /// <summary>
        /// Count the given size as waiting to be sent (or as sent, when negative), and return the new total.
        /// </summary>
        internal Int64 AddQueuedPushBytes(Int64 Size)

            => Interlocked.Add(ref queuedPushBytes, Size);

        #endregion

        #region (internal) TryStartPushing()

        /// <summary>
        /// Whether the pushes of this connection are to be started now: true only the first time.
        /// </summary>
        internal Boolean TryStartPushing()

            => Interlocked.Exchange(ref pushing, 1) == 0;

        #endregion

        #region (internal) Close()

        /// <summary>
        /// The connection is closed: its subscriptions end, its pushes are dropped,
        /// and whatever is still processed for it is cancelled.
        /// </summary>
        internal void Close()
        {
            subscriptions.Clear();
            Pushes.Writer.TryComplete();
            closed.Cancel();
        }

        #endregion

    }

}
