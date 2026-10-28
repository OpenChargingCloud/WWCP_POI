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

using System.Net;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.WebSockets
{

    /// <summary>
    /// A follower talking to a leader over the WebSocket of a history.
    /// </summary>
    [TestFixture]
    public class HistoryWebSocketTests
    {

        #region (class) TestClient

        /// <summary>
        /// A .NET ClientWebSocket, which matches the answers to its requests by their identifications.
        /// </summary>
        internal sealed class TestClient : IAsyncDisposable
        {

            private readonly ClientWebSocket                                                    webSocket  = new ();
            private readonly ConcurrentDictionary<String, TaskCompletionSource<POIWebSocketMessage>>  pending    = new ();
            private          Task?                                                              receiver;

            /// <summary>
            /// The identifications of all answers, in the order they came in.
            /// </summary>
            public ConcurrentQueue<String>  Received     { get; } = new ();

            /// <summary>
            /// The pushes of the server, with whether they came in binary frames.
            /// </summary>
            public Channel<(POIWebSocketMessage Message, Boolean Binary)>  Pushes  { get; } = Channel.CreateUnbounded<(POIWebSocketMessage, Boolean)>();

            /// <summary>
            /// Completed when the server closed the connection, with its close status.
            /// </summary>
            public TaskCompletionSource<WebSocketCloseStatus?>  ClosedByServer  { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);

            public WebSocketState           State
                => webSocket.State;

            public static async Task<TestClient> Connect(Uri URL)
            {

                var client = new TestClient();

                await client.webSocket.ConnectAsync(URL, CancellationToken.None);

                client.receiver = client.Receive();

                return client;

            }

            private async Task Receive()
            {

                var buffer = new Byte[64 * 1024];

                try
                {
                    while (webSocket.State == WebSocketState.Open)
                    {

                        using var message = new MemoryStream();
                        WebSocketReceiveResult result;

                        do
                        {
                            result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                            message.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            ClosedByServer.TrySetResult(result.CloseStatus);
                            return;
                        }

                        var parsed = result.MessageType == WebSocketMessageType.Binary
                                         ? POIWebSocketMessage.TryParse(message.ToArray(),                       out var answer, out var error)
                                         : POIWebSocketMessage.TryParse(Encoding.UTF8.GetString(message.ToArray()), out answer,   out error);

                        var message2 = parsed ? answer! : error!;

                        if (message2.MessageType == POIWebSocketMessageType.Send)
                        {
                            Pushes.Writer.TryWrite((message2, result.MessageType == WebSocketMessageType.Binary));
                            continue;
                        }

                        Received.Enqueue(message2.MessageId);

                        if (pending.TryRemove(message2.MessageId, out var waiting))
                            waiting.TrySetResult(message2);

                    }
                }
                catch (WebSocketException)
                {
                    ClosedByServer.TrySetResult(null);
                }

            }

            public async Task<(POIWebSocketMessage Message, Boolean Binary)> NextPush()
            {

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                return await Pushes.Reader.ReadAsync(timeout.Token);

            }

            public Task<POIWebSocketMessage> Request(String      MessageId,
                                                     String      Action,
                                                     JObject?    Payload       = null,
                                                     Boolean     Binary        = false,
                                                     CBORValue?  PayloadCBOR   = null)

                => SendAndWait(POIWebSocketMessage.Request(MessageId, Action, Payload, PayloadCBOR), Binary);

            public async Task<POIWebSocketMessage> SendAndWait(POIWebSocketMessage  Message,
                                                               Boolean              Binary   = false)
            {

                var waiting = Expect(Message.MessageId);

                await Send(Message, Binary);

                return await waiting;

            }

            public Task<POIWebSocketMessage> Expect(String MessageId)
            {

                var waiting = pending.GetOrAdd(MessageId, _ => new TaskCompletionSource<POIWebSocketMessage>(TaskCreationOptions.RunContinuationsAsynchronously));

                return waiting.Task.WaitAsync(TimeSpan.FromSeconds(10));

            }

            public Task Send(POIWebSocketMessage  Message,
                             Boolean              Binary   = false)

                => Binary
                       ? webSocket.SendAsync(Message.ToCBORBytes(),                     WebSocketMessageType.Binary, true, CancellationToken.None)
                       : webSocket.SendAsync(Encoding.UTF8.GetBytes(Message.ToJSONText()), WebSocketMessageType.Text,   true, CancellationToken.None);

            public Task SendText(String Text)

                => webSocket.SendAsync(Encoding.UTF8.GetBytes(Text), WebSocketMessageType.Text, true, CancellationToken.None);

            public void Abort()
                => webSocket.Abort();

            public async ValueTask DisposeAsync()
            {

                try
                {
                    if (webSocket.State == WebSocketState.Open)
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token);
                }
                catch
                { }

                webSocket.Dispose();

                if (receiver is not null)
                    await receiver.WaitAsync(TimeSpan.FromSeconds(2)).ContinueWith(_ => { });

            }

        }

        #endregion

        #region Data

        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private RoamingNetworkHistory?  leader;
        private POIWebSocketServer?     webSocketServer;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            httpServer  = new HTTPServer(
                              IPAddress:  IPv4Address.Localhost,
                              TCPPort:    IPPort.Zero,
                              AutoStart:  true
                          );

            poiAPI      = new POI_API(httpServer);

            leader      = RoamingNetworkHistory.Start(
                              new RoamingNetwork(
                                  Id:          RoamingNetwork_Id.Parse("rn1"),
                                  Name:        I18NString.Create(Languages.en, "Roaming Network 1"),
                                  DataSource:  "v0",
                                  Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")
                              ).WithKeys()
                          );

            poiAPI.AddRoamingNetworkHistory(leader);

            poiAPI.TryGetHistoryWebSocketServer(leader.RoamingNetworkId, out webSocketServer);

        }

        [TearDown]
        public async Task TearDown()
        {

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

        }

        #endregion


        #region Connect(RoamingNetworkId = "rn1")

        private Task<TestClient> Connect(String RoamingNetworkId = "rn1")

            => TestClient.Connect(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/RNs/{RoamingNetworkId}/history/ws"));

        #endregion

        #region Publish(Value, Minute)

        private RoamingNetworkCommitResult Publish(String Value, Int32 Minute)

            => leader!.Commit(
                   leader.Head.RoamingNetwork.CreateSignedChangeSet(
                       [ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, Value) ],
                       Description:  I18NString.Create(Languages.en, "Data source " + Value),
                       CreatedAt:    DateTimeOffset.Parse("2026-10-11T12:00:00Z").AddMinutes(Minute)
                   )
               );

        #endregion

        #region ChangeSet(History, Value)

        private static RoamingNetworkChangeSet ChangeSet(RoamingNetworkHistory  History,
                                                         String                 Value)

            => History.Head.RoamingNetwork.CreateSignedChangeSet(
                   [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                   CreatedAt: DateTimeOffset.Parse("2026-10-11T13:00:00Z")
               );

        #endregion

        #region Eventually(Condition)

        private static async Task<Boolean> Eventually(Func<Boolean> Condition)
        {

            for (var i = 0; i < 100; i++)
            {

                if (Condition())
                    return true;

                await Task.Delay(50);

            }

            return Condition();

        }

        #endregion


        #region Heavy_reads_beyond_the_limit_wait_over_the_WebSocket()

        /// <summary>
        /// Over the WebSocket a heavy read beyond MaxHeavyReadsPerMinute waits for the next one its client may make,
        /// MaxHeavyReadWait at most, and is answered with GenericError where that is not enough (POI3-ISSUES.md S12).
        /// </summary>
        [Test]
        public async Task Heavy_reads_beyond_the_limit_wait_over_the_WebSocket()
        {

            Publish("v1", 1);

            // One heavy read every two seconds.
            poiAPI!.MaxHeavyReadsPerMinute  = 30;
            poiAPI.MaxHeavyReadWait         = TimeSpan.FromSeconds(5);

            await using var client = await Connect();

            var answers = new List<POIWebSocketMessage>();

            for (var i = 0; i < 30; i++)
                answers.Add(await client.Request("c" + i, "commitInfos"));

            var started  = DateTimeOffset.UtcNow;
            var waited   = await client.Request("waited", "commits");
            var took     = DateTimeOffset.UtcNow - started;

            poiAPI.MaxHeavyReadsPerMinute   = 1;
            poiAPI.MaxHeavyReadWait         = TimeSpan.Zero;

            var first    = await client.Request("first",   "snapshot");
            var refused  = await client.Request("refused", "snapshot");
            var light    = await client.Request("light",   "head");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(answers.Select(answer => answer.MessageType),  Has.All.EqualTo(POIWebSocketMessageType.Response));

                Assert.That(waited.MessageType,                            Is.EqualTo(POIWebSocketMessageType.Response), waited.ErrorDescription);
                Assert.That(took,                                          Is.GreaterThan(TimeSpan.FromMilliseconds(300)), "The heavy read did not wait.");

                Assert.That(first.MessageType,                             Is.EqualTo(POIWebSocketMessageType.Response), first.ErrorDescription);
                Assert.That(refused.ErrorCode,                             Is.EqualTo(POIWebSocketErrorCodes.GenericError));
                Assert.That(refused.ErrorDescription,                      Does.Contain("Too many heavy reads"));
                Assert.That(poiAPI.HeavyReadsRefused,                      Is.EqualTo(1));

                Assert.That(light.MessageType,                             Is.EqualTo(POIWebSocketMessageType.Response), light.ErrorDescription);

            }

        }

        #endregion

        #region A_follower_catches_up_over_the_WebSocket()

        [Test]
        public async Task A_follower_catches_up_over_the_WebSocket()
        {

            Publish("v1", 1);

            await using var client = await Connect();

            // 1. Start from a snapshot of the leader's head.
            var snapshot = await client.Request("1", "snapshot");

            Assert.That(snapshot.MessageType,  Is.EqualTo(POIWebSocketMessageType.Response), snapshot.ErrorDescription);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot.Payload!, out var follower, out var error1),  Is.True, error1);

            Publish("v2", 2);
            Publish("v3", 3);

            // 2. Look at what is new: the metadata only.
            var head    = await client.Request("2", "head");
            var infos   = await client.Request("3", "commitInfos", new JObject(new JProperty("since", follower!.Head.Commit.Id.ToString())));

            // 3. Fetch the commits completely, one page of one commit, and one commit by its identification.
            var page    = await client.Request("4", "commits", new JObject(new JProperty("since", follower.Head.Commit.Id.ToString()),
                                                                           new JProperty("max",   1)));
            var single  = await client.Request("5", "commit",  new JObject(new JProperty("commitId", leader!.Head.Commit.Id.ToString())));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(head.Payload!["revision"]!.Value<Int32>(),                                  Is.EqualTo(3));
                Assert.That(infos.Payload!["commitInfos"]!.Count(),                                     Is.EqualTo(2));
                Assert.That(infos.Payload["commitInfos"]![0]!["description"]!["en"]!.Value<String>(),  Is.EqualTo("Data source v2"));
                Assert.That(page.Payload!["commits"]!.Count(),                                          Is.EqualTo(1));
            }

            var commits = new[] {
                              RoamingNetworkCommit.Parse(page.Payload["commits"]![0]!.ToString()),
                              RoamingNetworkCommit.Parse(single.Payload!.ToString())
                          };

            Assert.That(follower.TryAppend(commits, out var appended, out var error2),  Is.True, error2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(appended,                                  Is.EqualTo(2));
                Assert.That(follower.Head.Commit.Id,                   Is.EqualTo(leader.Head.Commit.Id));
                Assert.That(follower.Head.RoamingNetwork.DataSource,   Is.EqualTo("v3"));
            }

        }

        #endregion

        #region A_follower_syncs_in_CBOR()

        [Test]
        public async Task A_follower_syncs_in_CBOR()
        {

            poiAPI!.AuthorizeChangeSetSubmission = (request, changeSet) => true;

            Publish("v1", 1);

            await using var client = await Connect();

            // The snapshot in CBOR, as the HTTP API gives it.
            var snapshot = await client.Request("1", "snapshot", Binary: true);

            Assert.That(snapshot.PayloadCBOR.HasValue,  Is.True);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot.PayloadCBOR!.Value, out var follower, out var error),  Is.True, error);

            // A change set in CBOR.
            var published = await client.Request("2", "commitChangeSet",
                                                  Binary:       true,
                                                  PayloadCBOR:  CBORValue.FromMap([
                                                                    new (CBORValue.FromText("changeSet"),     ChangeSet(follower!, "from CBOR").ToCBOR())
                                                                ]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(published.MessageType,                                     Is.EqualTo(POIWebSocketMessageType.Response), published.ErrorDescription);
                Assert.That(published.GetPayloadJSON()!["status"]!.Value<String>(),    Is.EqualTo("Committed"));
                Assert.That(leader!.Head.RoamingNetwork.DataSource,                    Is.EqualTo("from CBOR"));
            }

        }

        #endregion

        #region Requests_are_answered_asynchronously()

        [Test]
        public async Task Requests_are_answered_asynchronously()
        {

            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            webSocketServer!.AddCommand("slow", async request => {
                await gate.Task;
                return request.Respond(new JObject(new JProperty("slow", true)));
            });

            await using var client = await Connect();

            var slow  = client.Expect("s1");
            await client.Send(POIWebSocketMessage.Request("s1", "slow"));

            // Answered while the slow request is still waiting...
            var head  = await client.Request("h1", "head");

            Assert.That(slow.IsCompleted,  Is.False);

            gate.SetResult();

            // ...and the slow one afterwards.
            var slowAnswer = await slow;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(head.MessageType,                          Is.EqualTo(POIWebSocketMessageType.Response));
                Assert.That(slowAnswer.Payload!["slow"]!.Value<Boolean>(),  Is.True);
                Assert.That(client.Received.ToArray(),                 Is.EqualTo(new[] { "h1", "s1" }));
            }

        }

        #endregion

        #region Errors_are_answered_with_request_errors()

        [Test]
        public async Task Errors_are_answered_with_request_errors()
        {

            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            webSocketServer!.AddCommand("slow", async request => {
                await gate.Task;
                return request.Respond([]);
            });

            webSocketServer!.AddCommand("broken", request => throw new InvalidOperationException("secret details"));

            await using var client = await Connect();

            var garbage       = client.Expect("-1");
            await client.SendText("no JSON");

            var unknown       = await client.Request("1", "unknownAction");
            var unknownCommit = await client.Request("2", "commit",      new JObject(new JProperty("commitId", new String('0', 64))));
            var invalidSince  = await client.Request("3", "commitInfos", new JObject(new JProperty("since",    "nope")));
            var broken        = await client.Request("4", "broken");

            // The same identification while its request is still processed.
            var slow          = client.Expect("5");
            await client.Send(POIWebSocketMessage.Request("5", "slow"));
            var duplicate     = await client.Request("5", "head");

            gate.SetResult();
            await slow;

            using (Assert.EnterMultipleScope())
            {

                Assert.That((await garbage).ErrorCode,  Is.EqualTo(POIWebSocketErrorCodes.RpcFrameworkError));
                Assert.That(unknown.ErrorCode,          Is.EqualTo(POIWebSocketErrorCodes.NotImplemented));
                Assert.That(unknownCommit.ErrorCode,    Is.EqualTo(POIWebSocketErrorCodes.NotFound));
                Assert.That(invalidSince.ErrorCode,     Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));

                Assert.That(broken.ErrorCode,           Is.EqualTo(POIWebSocketErrorCodes.InternalError));
                Assert.That(broken.ErrorDescription,    Does.Not.Contain("secret"));

                Assert.That(duplicate.ErrorCode,        Is.EqualTo(POIWebSocketErrorCodes.RpcFrameworkError));

            }

        }

        #endregion

        #region Change_sets_are_committed_over_the_WebSocket()

        [Test]
        public async Task Change_sets_are_committed_over_the_WebSocket()
        {

            await using var client = await Connect();

            var head0      = leader!.Head.Commit.Id;
            var changeSet  = ChangeSet(leader, "A");
            var changeSetB = ChangeSet(leader, "B");
            var payload    = new JObject(new JProperty("changeSet", changeSet.ToJSON()));

            var refused    = await client.Request("1", "commitChangeSet", payload);

            poiAPI!.AuthorizeChangeSetSubmission = (request, changeSet) => request.Path.ToString().EndsWith("/history/ws");

            var published  = await client.Request("2", "commitChangeSet", payload);
            var again      = await client.Request("3", "commitChangeSet", payload);
            var conflict   = await client.Request("4", "commitChangeSet", new JObject(new JProperty("changeSet",     changeSetB.ToJSON())));
            var invalid    = await client.Request("5", "commitChangeSet", new JObject(new JProperty("changeSet",     "no change set")));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(refused.ErrorCode,                                    Is.EqualTo(POIWebSocketErrorCodes.SecurityError));

                Assert.That(published.Payload!["status"]!.Value<String>(),        Is.EqualTo("Committed"));
                Assert.That(leader.Tips.Select(tip => tip.Commit.Id.ToString()),  Does.Contain(published.Payload["@id"]!.Value<String>()));
                Assert.That(again.Payload!["status"]!.Value<String>(),            Is.EqualTo("AlreadyCommitted"));

                // B, made for the version A was, too, would conflict with A, the head: refused, with the conflict.
                Assert.That(conflict.Payload!["status"]!.Value<String>(),         Is.EqualTo("Conflict"));
                Assert.That(conflict.Payload["conflicts"]!.Count(),               Is.EqualTo(1));
                Assert.That(conflict.Payload["head"]!["@id"]!.Value<String>(),    Is.EqualTo(published.Payload["@id"]!.Value<String>()));
                Assert.That(leader.Tips,                                          Has.Count.EqualTo(1));
                Assert.That(leader.Head.Commit.ParentIds,                         Is.EqualTo(new[] { head0 }));

                Assert.That(invalid.ErrorCode,                                    Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));

                Assert.That(leader.Head.Commit.Revision,                          Is.EqualTo(1));

            }

        }

        #endregion

        #region A_session_ends_with_its_connection()

        [Test]
        public async Task A_session_ends_with_its_connection()
        {

            var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            webSocketServer!.AddCommand("forever", async request => {

                try
                {
                    await Task.Delay(Timeout.Infinite, request.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult();
                    throw;
                }

                return request.Respond([]);

            });

            var client = await Connect();

            await client.Send(POIWebSocketMessage.Request("1", "forever"));
            await client.Request("2", "head");

            Assert.That(webSocketServer.Sessions.Count(),  Is.EqualTo(1));

            // The connection breaks, without any close frame.
            client.Abort();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(() => !webSocketServer.Sessions.Any()),   Is.True, "The session outlived its connection.");
                Assert.That(await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5)).ContinueWith(task => task.IsCompletedSuccessfully),
                            Is.True,
                            "The request of the closed connection was not cancelled.");
            }

            await client.DisposeAsync();

        }

        #endregion

        #region Clients_learn_the_topics()

        [Test]
        public async Task Clients_learn_the_topics()
        {

            await using var client = await Connect();

            var topics = await client.Request("1", "topics");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(topics.Payload!["topics"]!.Select(topic => topic["name"]!.Value<String>()),
                            Is.EqualTo(new[] { "adminStatusUpdates", "attestations", "checkpointSignatures", "checkpoints", "commitAnnouncements", "commitSignatures", "commits", "energyStatusUpdates", "pendingChangeSets", "priceStatements", "priceStatusUpdates", "statusUpdates" }));
                Assert.That(topics.Payload["topics"]![4]!["action"]!.Value<String>(),  Is.EqualTo("commitAnnouncement"));
            }

        }

        #endregion

        #region Subscribers_hear_of_new_commits()

        [Test]
        public async Task Subscribers_hear_of_new_commits()
        {

            await using var client      = await Connect();
            await using var bystander   = await Connect();

            var subscribed  = await client.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("commitAnnouncements"))));
            await bystander.Request("1", "head");

            var parent      = leader!.Head.Commit.Id;
            Publish("v1", 1);

            var (push, binary) = await client.NextPush();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(subscribed.Payload!["topics"]!.Values<String>(),            Is.EqualTo(new[] { "commitAnnouncements" }));

                Assert.That(push.Action,                                                Is.EqualTo("commitAnnouncement"));
                Assert.That(push.MessageId,                                             Is.EqualTo("p1"));
                Assert.That(binary,                                                     Is.False);
                Assert.That(push.Payload!["commitId"]!.Value<String>(),                 Is.EqualTo(leader.Head.Commit.Id.ToString()));
                Assert.That(push.Payload["parentIds"]!.Values<String>(),                Is.EqualTo(new[] { parent.ToString() }));
                Assert.That(push.Payload["revision"]!.Value<Int32>(),                   Is.EqualTo(1));
                Assert.That(push.Payload["description"]!["en"]!.Value<String>(),        Is.EqualTo("Data source v1"));
                Assert.That(push.Payload["timestamp"],                                  Is.Not.Null);
                Assert.That(push.Payload["eTags"],                                      Is.Null);

                Assert.That(bystander.Pushes.Reader.Count,                              Is.Zero);

            }

        }

        #endregion

        #region Status_updates_are_pushed_in_CBOR_and_end_with_unsubscribe()

        [Test]
        public async Task Status_updates_are_pushed_in_CBOR_and_end_with_unsubscribe()
        {

            await using var client = await Connect();

            await client.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("statusUpdates", "adminStatusUpdates"))), Binary: true);

            leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(RoamingNetwork), "rn1", "offline",      DateTimeOffset.Parse("2026-10-11T12:00:00Z"), out _, out _);
            leader.RuntimeStatus. TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), "rn1", "outOfService", DateTimeOffset.Parse("2026-10-11T12:00:00Z"), out _, out _);

            var (status, statusBinary)  = await client.NextPush();
            var (admin,  _)             = await client.NextPush();

            var unsubscribed            = await client.Request("2", "unsubscribe", new JObject(new JProperty("topics", new JArray("statusUpdates"))));

            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(RoamingNetwork), "rn1", "available",    DateTimeOffset.Parse("2026-10-11T12:01:00Z"), out _, out _);
            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), "rn1", "operational",  DateTimeOffset.Parse("2026-10-11T12:01:00Z"), out _, out _);

            var (lastAdmin, _)          = await client.NextPush();
            var runtimeStatus           = await client.Request("3", "runtimeStatus");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(statusBinary,                                                       Is.True);
                Assert.That(status.Action,                                                      Is.EqualTo("statusUpdate"));
                Assert.That(status.GetPayloadJSON()!["value"]!.Value<String>(),                 Is.EqualTo("offline"));
                Assert.That(admin.Action,                                                       Is.EqualTo("adminStatusUpdate"));

                Assert.That(unsubscribed.GetPayloadJSON()!["topics"]!.Values<String>(),         Is.EqualTo(new[] { "adminStatusUpdates" }));

                // The status update after unsubscribing never came.
                Assert.That(lastAdmin.Action,                                                   Is.EqualTo("adminStatusUpdate"));
                Assert.That(lastAdmin.GetPayloadJSON()!["oldValue"]!.Value<String>(),           Is.EqualTo("outOfService"));
                Assert.That(client.Pushes.Reader.Count,                                         Is.Zero);

                // Of the roaming network and of its operator.
                Assert.That(runtimeStatus.Payload!["runtimeStatus"]!.Count(),                   Is.EqualTo(4));

            }

        }

        #endregion

        #region Unknown_topics_are_refused()

        [Test]
        public async Task Unknown_topics_are_refused()
        {

            await using var client = await Connect();

            var unknown  = await client.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("statusUpdates", "gossip"))));
            var invalid  = await client.Request("2", "subscribe", new JObject(new JProperty("topics", "statusUpdates")));
            var empty    = await client.Request("3", "subscribe", new JObject(new JProperty("topics", new JArray())));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unknown.ErrorCode,                                             Is.EqualTo(POIWebSocketErrorCodes.PropertyConstraintViolation));
                Assert.That(unknown.ErrorDetails!["unknownTopics"]!.Values<String>(),     Is.EqualTo(new[] { "gossip" }));
                Assert.That(invalid.ErrorCode,                                             Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));
                Assert.That(empty.ErrorCode,                                               Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));
                Assert.That(webSocketServer!.Sessions.Single().Subscriptions,              Is.Empty);
            }

        }

        #endregion

        #region Subscriptions_end_with_their_connection()

        [Test]
        public async Task Subscriptions_end_with_their_connection()
        {

            var client = await Connect();

            await client.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("commitAnnouncements"))));

            Assert.That(webSocketServer!.Push(POI_API.CommitAnnouncementsTopic, []),  Is.EqualTo(1));

            client.Abort();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(() => !webSocketServer.Sessions.Any()),      Is.True);
                Assert.That(webSocketServer.Push(POI_API.CommitAnnouncementsTopic, []),   Is.Zero);
            }

            await client.DisposeAsync();

        }

        #endregion

        #region A_client_that_does_not_keep_up_is_disconnected()

        [Test]
        public async Task A_client_that_does_not_keep_up_is_disconnected()
        {

            webSocketServer!.MaxQueuedPushes = 1;

            await using var client = await Connect();

            await client.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("commitAnnouncements"))));

            var payload = new JObject(new JProperty("filler", new String('x', 16 * 1024)));

            for (var i = 0; i < 1000 && webSocketServer.Sessions.Any(); i++)
                webSocketServer.Push(POI_API.CommitAnnouncementsTopic, payload);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(webSocketServer.Sessions,                                                   Is.Empty);
                Assert.That(await client.ClosedByServer.Task.WaitAsync(TimeSpan.FromSeconds(10)),        Is.EqualTo(WebSocketCloseStatus.PolicyViolation));
            }

        }

        #endregion

        #region SetStatus(Client, MessageId, Kind, Value, Timestamp = null, EntityId = "rn1")

        private static Task<POIWebSocketMessage> SetStatus(TestClient  Client,
                                                           String      MessageId,
                                                           String      Kind,
                                                           String      Value,
                                                           String?     Timestamp   = null,
                                                           String      EntityId    = "rn1")
        {

            var payload = new JObject(
                              new JProperty("kind",        Kind),
                              new JProperty("entityType",  nameof(RoamingNetwork)),
                              new JProperty("entityId",    EntityId),
                              new JProperty("value",       Value)
                          );

            if (Timestamp is not null)
                payload.Add("timestamp", Timestamp);

            return Client.Request(MessageId, "setStatus", payload);

        }

        #endregion

        #region Clients_set_the_status()

        [Test]
        public async Task Clients_set_the_status()
        {

            await using var operatorClient  = await Connect();
            await using var subscriber      = await Connect();

            await subscriber.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("statusUpdates"))));

            var notEnabled  = await SetStatus(operatorClient, "1", "status", "available", "2026-10-11T12:00:00Z");

            poiAPI!.AuthorizeStatusUpdate = (request, login, statusUpdate) => statusUpdate.EntityType == nameof(RoamingNetwork);

            var updated     = await SetStatus(operatorClient, "2", "status", "offline",   "2026-10-11T12:00:00Z");
            var unchanged   = await SetStatus(operatorClient, "3", "status", "offline",   "2026-10-11T12:05:00Z");
            var outdated    = await SetStatus(operatorClient, "4", "status", "available", "2026-10-11T11:00:00Z");
            var unknown     = await SetStatus(operatorClient, "5", "status", "available", "2026-10-11T12:00:00Z", EntityId: "rn2");
            var invalidKind = await SetStatus(operatorClient, "6", "energy", "available");
            var noValue     = await operatorClient.Request("7", "setStatus", new JObject(new JProperty("kind", "status"), new JProperty("entityType", nameof(RoamingNetwork)), new JProperty("entityId", "rn1")));
            var forbidden   = await operatorClient.Request("8", "setStatus", new JObject(new JProperty("kind",       "status"),
                                                                                        new JProperty("entityType", nameof(ChargingStationOperator)),
                                                                                        new JProperty("entityId",   "DE*GEF"),
                                                                                        new JProperty("value",      "available")));
            var now         = await SetStatus(operatorClient, "9", "adminStatus", "outOfService");

            var (push, _)   = await subscriber.NextPush();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(notEnabled.ErrorCode,                                    Is.EqualTo(POIWebSocketErrorCodes.SecurityError));

                Assert.That(updated.Payload!["result"]!.Value<String>(),             Is.EqualTo("Updated"));
                Assert.That(updated.Payload["current"]!["value"]!.Value<String>(),   Is.EqualTo("offline"));
                Assert.That(unchanged.Payload!["result"]!.Value<String>(),           Is.EqualTo("Unchanged"));
                Assert.That(outdated.Payload!["result"]!.Value<String>(),            Is.EqualTo("Outdated"));
                Assert.That(outdated.Payload["current"]!["value"]!.Value<String>(),  Is.EqualTo("offline"));
                Assert.That(unknown.Payload!["result"]!.Value<String>(),             Is.EqualTo("Refused"));
                Assert.That(unknown.Payload["current"],                              Is.Null);

                Assert.That(invalidKind.ErrorCode,                                   Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));
                Assert.That(noValue.ErrorCode,                                       Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));
                Assert.That(forbidden.ErrorCode,                                     Is.EqualTo(POIWebSocketErrorCodes.SecurityError));

                // Without a timestamp: now.
                Assert.That(now.Payload!["result"]!.Value<String>(),                 Is.EqualTo("Updated"));

                // One push for the one change of the status; the admin status is another topic.
                Assert.That(push.Action,                                             Is.EqualTo("statusUpdate"));
                Assert.That(push.Payload!["value"]!.Value<String>(),                 Is.EqualTo("offline"));
                Assert.That(subscriber.Pushes.Reader.Count,                          Is.Zero);

                // Of the roaming network and of its operator.
                Assert.That(leader!.RuntimeStatus.All.Count,                         Is.EqualTo(4));

            }

        }

        #endregion

        #region StatusPayload(Kind, Value, Timestamp)

        private static JObject StatusPayload(String  Kind,
                                             String  Value,
                                             String  Timestamp)

            => new (
                   new JProperty("kind",        Kind),
                   new JProperty("entityType",  nameof(RoamingNetwork)),
                   new JProperty("entityId",    "rn1"),
                   new JProperty("value",       Value),
                   new JProperty("timestamp",   Timestamp)
               );

        #endregion

        #region Requests_of_the_same_action_aggregate()

        [Test]
        public async Task Requests_of_the_same_action_aggregate()
        {

            poiAPI!.AuthorizeStatusUpdate = (request, login, statusUpdate) => true;

            await using var client = await Connect();

            var statuses  = await client.Request("1", "setStatus", new JObject(new JProperty("batch", new JArray(
                                                                       StatusPayload("status",      "offline",      "2026-10-11T12:00:00Z"),
                                                                       StatusPayload("energy",      "available",   "2026-10-11T12:00:00Z"),
                                                                       "no object",
                                                                       StatusPayload("adminStatus", "outOfService", "2026-10-11T12:00:00Z"),
                                                                       StatusPayload("status",      "offline",     "2026-10-11T11:00:00Z")
                                                                   ))));

            // Reads do not aggregate: a batch would multiply them without end.
            var heads     = await client.Request("2", "head",      new JObject(new JProperty("batch", new JArray(new JObject(), new JObject()))));
            var empty     = await client.Request("3", "setStatus", new JObject(new JProperty("batch", new JArray())));

            var inCBOR    = await client.Request("4", "setStatus",
                                                 Binary:       true,
                                                 PayloadCBOR:  CBORJSON.ToCBOR(new JObject(new JProperty("batch", new JArray(
                                                                                     StatusPayload("status", "available", "2026-10-11T12:01:00Z")
                                                                                 )))));

            var results   = statuses.Payload!["batch"]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(statuses.MessageType,                                   Is.EqualTo(POIWebSocketMessageType.Response));
                Assert.That(results.Count(),                                        Is.EqualTo(5));
                Assert.That(results[0]!["result"]!.Value<String>(),                 Is.EqualTo("Updated"));
                Assert.That(results[1]!["error"]!["code"]!.Value<String>(),         Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));
                Assert.That(results[2]!["error"]!["code"]!.Value<String>(),         Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));
                Assert.That(results[3]!["result"]!.Value<String>(),                 Is.EqualTo("Updated"));
                Assert.That(results[4]!["result"]!.Value<String>(),                 Is.EqualTo("Outdated"));

                Assert.That(heads.MessageType,                                      Is.EqualTo(POIWebSocketMessageType.RequestError));
                Assert.That(heads.ErrorCode,                                        Is.EqualTo(POIWebSocketErrorCodes.NotSupported));
                Assert.That(empty.ErrorCode,                                        Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));

                Assert.That(inCBOR.GetPayloadJSON()!["batch"]![0]!["result"]!.Value<String>(),  Is.EqualTo("Updated"));

            }

        }

        #endregion

        #region Pushes_of_the_same_action_aggregate()

        [Test]
        public async Task Pushes_of_the_same_action_aggregate()
        {

            webSocketServer!.PushCoalescingDelay = TimeSpan.FromMilliseconds(300);

            await using var everything  = await Connect();
            await using var twoAtOnce   = await Connect();

            await everything.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("statusUpdates", "adminStatusUpdates"))));
            await twoAtOnce. Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("statusUpdates")),
                                                                   new JProperty("maxBatchSize", 2)));

            var t0 = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

            leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(RoamingNetwork), "rn1", "offline",      t0,               out _, out _);
            leader.RuntimeStatus. TryUpdate(POIRuntimeStatusKind.Status,      nameof(RoamingNetwork), "rn1", "available",    t0.AddMinutes(1), out _, out _);
            leader.RuntimeStatus. TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), "rn1", "outOfService", t0.AddMinutes(1), out _, out _);
            leader.RuntimeStatus. TryUpdate(POIRuntimeStatusKind.Status,      nameof(RoamingNetwork), "rn1", "offline",      t0.AddMinutes(2), out _, out _);

            var first   = (await everything.NextPush()).Message;
            var second  = (await everything.NextPush()).Message;
            var third   = (await everything.NextPush()).Message;

            var pair    = (await twoAtOnce.NextPush()).Message;
            var single  = (await twoAtOnce.NextPush()).Message;

            using (Assert.EnterMultipleScope())
            {

                // Aggregated as long as the action stays the same, in their order.
                Assert.That(first.Action,                                                         Is.EqualTo("statusUpdate"));
                Assert.That(first.Payload!["batch"]!.Select(update => update["value"]!.Value<String>()),  Is.EqualTo(new[] { "offline", "available" }));
                Assert.That(second.Action,                                                        Is.EqualTo("adminStatusUpdate"));
                Assert.That(second.Payload!["value"]!.Value<String>(),                            Is.EqualTo("outOfService"));
                Assert.That(third.Payload!["value"]!.Value<String>(),                             Is.EqualTo("offline"));

                // At most as many as asked for.
                Assert.That(pair.Payload!["batch"]!.Count(),                                      Is.EqualTo(2));
                Assert.That(single.Payload!["value"]!.Value<String>(),                            Is.EqualTo("offline"));

                Assert.That(new[] { first.MessageId, second.MessageId, third.MessageId },        Is.EqualTo(new[] { "p1", "p2", "p3" }));

            }

        }

        #endregion

        #region Energy_statuses_are_set_in_batches_and_pushed()

        /// <summary>
        /// A charging station operator sends several energy statuses as one batch; subscribers get them aggregated,
        /// their values as objects in W, W and Wh.
        /// </summary>
        [Test]
        public async Task Energy_statuses_are_set_in_batches_and_pushed()
        {

            poiAPI!.AuthorizeStatusUpdate = (request, login, statusUpdate) => true;
            webSocketServer!.PushCoalescingDelay = TimeSpan.FromMilliseconds(300);

            // The roaming network operator adds the operator, the operator its charging pool.
            leader!.Commit(leader.Head.RoamingNetwork.CreateSignedChangeSet([
                                RoamingNetworkChange.Add(new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF")).WithKeys())
                            ], CreatedAt: DateTimeOffset.Parse("2026-10-11T10:00:00Z")));

            leader!.Commit(leader.Head.RoamingNetwork.CreateSignedChangeSet([
                                RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                          ChargingStations: [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                  EVSEs: [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1")),
                                                                                                                           new EVSE(EVSE_Id.Parse("DE*GEF*E1*2")) ]) ]))
                            ], CreatedAt: DateTimeOffset.Parse("2026-10-11T11:00:00Z")));

            await using var subscriber  = await Connect();
            await using var cso         = await Connect();

            await subscriber.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("energyStatusUpdates"))));

            static JObject EnergyPayload(String EVSEId, Decimal Used, String Timestamp)
                => new (new JProperty("kind",        "energyStatus"),
                        new JProperty("entityType",  "EVSE"),
                        new JProperty("entityId",    EVSEId),
                        new JProperty("value",       new JObject(new JProperty("used", Used), new JProperty("available", 22000))),
                        new JProperty("timestamp",   Timestamp));

            var results  = await cso.Request("1", "setStatus", new JObject(new JProperty("batch", new JArray(
                                                                   EnergyPayload("DE*GEF*E1*1", 11000, "2026-10-11T12:00:00Z"),
                                                                   EnergyPayload("DE*GEF*E1*2",  7400, "2026-10-11T12:00:00Z"),
                                                                   new JObject(new JProperty("kind",       "energyStatus"),
                                                                               new JProperty("entityType", "EVSE"),
                                                                               new JProperty("entityId",   "DE*GEF*E1*1"),
                                                                               new JProperty("value",      "11 kW"))
                                                               ))));

            var push     = (await subscriber.NextPush()).Message;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(results.Payload!["batch"]![0]!["result"]!.Value<String>(),              Is.EqualTo("Updated"));
                Assert.That(results.Payload["batch"]![1]!["result"]!.Value<String>(),               Is.EqualTo("Updated"));
                Assert.That(results.Payload["batch"]![1]!["current"]!["value"]!["used"]!.Value<Decimal>(),  Is.EqualTo(7400));
                Assert.That(results.Payload["batch"]![2]!["error"]!["code"]!.Value<String>(),       Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));

                Assert.That(push.Action,                                                             Is.EqualTo("energyStatusUpdate"));
                Assert.That(Payloads(push).Select(update => update["value"]!["used"]!.Value<Decimal>()),  Is.EqualTo(new[] { 11000m, 7400m }));

            }

        }

        #endregion

        #region The_answers_of_a_batch_are_bounded()

        /// <summary>
        /// Once the answers of a batch exceed MaxBatchResponseBytes, the payloads left are not processed, but answered with
        /// an error each (POI3-ISSUES.md S12).
        /// </summary>
        [Test]
        public async Task The_answers_of_a_batch_are_bounded()
        {

            poiAPI!.AuthorizeStatusUpdate            = (request, login, statusUpdate) => true;
            webSocketServer!.MaxBatchResponseBytes  = 1;

            await using var client = await Connect();

            var statuses  = await client.Request("1", "setStatus", new JObject(new JProperty("batch", new JArray(
                                                                       StatusPayload("status",      "offline",       "2026-10-11T12:00:00Z"),
                                                                       StatusPayload("adminStatus", "outOfService",  "2026-10-11T12:00:00Z"),
                                                                       StatusPayload("status",      "available",     "2026-10-11T12:01:00Z")
                                                                   ))));

            var results   = statuses.Payload!["batch"]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(statuses.MessageType,                                   Is.EqualTo(POIWebSocketMessageType.Response));
                Assert.That(results.Count(),                                        Is.EqualTo(3));
                Assert.That(results[0]!["result"]!.Value<String>(),                 Is.EqualTo("Updated"));
                Assert.That(results[1]!["error"]!["code"]!.Value<String>(),         Is.EqualTo(POIWebSocketErrorCodes.GenericError));
                Assert.That(results[2]!["error"]!["code"]!.Value<String>(),         Is.EqualTo(POIWebSocketErrorCodes.GenericError));

                // Not processed: the status is still the one of the first payload.
                Assert.That(leader!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(RoamingNetwork), "rn1", out var status) ? status.Value : null,
                                                                                    Is.EqualTo("offline"));

            }

        }

        #endregion

        #region A_closed_connection_cancels_the_rest_of_its_batch()

        /// <summary>
        /// A batch whose connection closes is not processed any further: nobody would get the answers (POI3-ISSUES.md S12).
        /// </summary>
        [Test]
        public async Task A_closed_connection_cancels_the_rest_of_its_batch()
        {

            var processed  = 0;
            var entered    = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var released   = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            webSocketServer!.AddCommand("slowWrite", async request => {

                                            if (Interlocked.Increment(ref processed) == 1)
                                            {
                                                entered.TrySetResult();
                                                await released.Task.WaitAsync(TimeSpan.FromSeconds(10));
                                            }

                                            return request.Respond(new JObject());

                                        },
                                        Batches: true);

            var client = await Connect();

            await client.Send(POIWebSocketMessage.Request("1", "slowWrite", new JObject(new JProperty("batch", new JArray(Enumerable.Range(0, 5).Select(_ => new JObject()))))));

            Assert.That(await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).ContinueWith(task => task.IsCompletedSuccessfully), Is.True, "The batch was not processed.");

            client.Abort();

            var closed = await Eventually(() => !webSocketServer.Sessions.Any());

            released.TrySetResult();

            await Task.Delay(300);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closed,                         Is.True, "The server did not notice the connection closing.");
                Assert.That(Volatile.Read(ref processed),   Is.EqualTo(1), "The rest of the batch was processed for a closed connection.");
            }

            await client.DisposeAsync();

        }

        #endregion

        #region The_commits_held_are_bounded()

        /// <summary>
        /// A request names at most MaxHaveIds commits held: more are refused, duplicates count once (POI3-ISSUES.md S12).
        /// </summary>
        [Test]
        public async Task The_commits_held_are_bounded()
        {

            var c1 = Publish("v1", 1).Commit!;
            var c2 = Publish("v2", 2).Commit!;

            poiAPI!.MaxHaveIds = 2;

            await using var client = await Connect();

            var tooMany  = await client.Request("1", "commits", new JObject(new JProperty("have", new JArray(leader!.Root.Id.ToString(), c1.Id.ToString(), c2.Id.ToString()))));
            var twice    = await client.Request("2", "commits", new JObject(new JProperty("have", new JArray(c1.Id.ToString(), c1.Id.ToString()))));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(tooMany.MessageType,                                            Is.EqualTo(POIWebSocketMessageType.RequestError));
                Assert.That(tooMany.ErrorCode,                                              Is.EqualTo(POIWebSocketErrorCodes.FormatViolation));
                Assert.That(tooMany.ErrorDescription,                                       Does.Contain("At most 2"));

                Assert.That(twice.MessageType,                                              Is.EqualTo(POIWebSocketMessageType.Response));
                Assert.That(twice.Payload!["commits"]!.Select(commit => commit["@id"]!.Value<String>()),
                                                                                            Is.EqualTo(new[] { c2.Id.ToString() }));

            }

        }

        #endregion

        #region Payloads(Push)

        /// <summary>
        /// The payloads of a push: one, or those of its batch.
        /// </summary>
        private static IEnumerable<JObject> Payloads(POIWebSocketMessage Push)

            => Push.GetPayloadJSON()!["batch"] is JArray batch
                   ? batch.Cast<JObject>()
                   : [ Push.GetPayloadJSON()! ];

        #endregion

        #region A_follower_follows_the_pushed_commits()

        [Test]
        public async Task A_follower_follows_the_pushed_commits()
        {

            await using var client = await Connect();

            var snapshot = await client.Request("1", "snapshot");
            RoamingNetworkHistory.TryFromSnapshot(snapshot.Payload!, out var follower, out _);

            await client.Request("2", "subscribe", new JObject(new JProperty("topics", new JArray("commits"))));

            Publish("v1", 1);
            Publish("v2", 2);

            var commits = new List<RoamingNetworkCommit>();

            while (commits.Count < 2)
            {

                var (push, _) = await client.NextPush();

                Assert.That(push.Action, Is.EqualTo("commit"));

                commits.AddRange(Payloads(push).Select(payload => RoamingNetworkCommit.Parse(payload.ToString())));

            }

            Assert.That(follower!.TryAppend(commits, out var appended, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(appended,                                 Is.EqualTo(2));
                Assert.That(follower.Head.Commit.Id,                  Is.EqualTo(leader!.Head.Commit.Id));
                Assert.That(follower.Head.RoamingNetwork.DataSource,  Is.EqualTo("v2"));
            }

        }

        #endregion

        #region Large_commits_are_only_announced()

        [Test]
        public async Task Large_commits_are_only_announced()
        {

            poiAPI!.MaxPushedCommitSize = 100;

            await using var client = await Connect();

            await client.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("commits"))));

            Publish("v1", 1);

            var (push, _) = await client.NextPush();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(push.Action,                                   Is.EqualTo("commitAnnouncement"));
                Assert.That(push.Payload!["commitId"]!.Value<String>(),    Is.EqualTo(leader!.Head.Commit.Id.ToString()));
                Assert.That(push.Payload["changeSet"],                     Is.Null);
            }

        }

        #endregion

        #region A_client_with_too_many_bytes_waiting_is_disconnected()

        [Test]
        public async Task A_client_with_too_many_bytes_waiting_is_disconnected()
        {

            webSocketServer!.MaxQueuedPushBytes   = 4096;
            webSocketServer.PushCoalescingDelay  = TimeSpan.FromMilliseconds(500);

            await using var client = await Connect();

            await client.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray("commitAnnouncements"))));

            var payload = new JObject(new JProperty("filler", new String('x', 1024)));

            var queued  = Enumerable.Range(0, 10).Select(_ => webSocketServer.Push(POI_API.CommitAnnouncementsTopic, payload)).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queued.Take(3),                                                            Is.All.EqualTo(1));
                Assert.That(queued.Last(),                                                             Is.Zero);
                Assert.That(await client.ClosedByServer.Task.WaitAsync(TimeSpan.FromSeconds(10)),      Is.EqualTo(WebSocketCloseStatus.PolicyViolation));
                Assert.That(webSocketServer.Sessions,                                                  Is.Empty);
            }

        }

        #endregion

        #region The_WebSocket_path_answers_plain_requests()

        [Test]
        public async Task The_WebSocket_path_answers_plain_requests()
        {

            using var httpClient = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpServer!.TCPPort}/RNs/") };

            using var known    = await httpClient.GetAsync("rn1/history/ws");
            using var unknown  = await httpClient.GetAsync("rn2/history/ws");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(known.StatusCode,    Is.EqualTo(HttpStatusCode.UpgradeRequired));
                Assert.That(unknown.StatusCode,  Is.EqualTo(HttpStatusCode.NotFound));
            }

        }

        #endregion

        #region Clients_can_be_required_to_authenticate()

        [Test]
        public async Task Clients_can_be_required_to_authenticate()
        {

            poiAPI!.AuthenticateWebSocketClient = (request, cancellationToken) =>
                Task.FromResult(request.Authorization is HTTPBasicAuthentication basic && basic.Username == "alice" && basic.Password == "secret"
                                    ? "alice"
                                    : null);

            await Assert.ThatAsync(async () => await Connect(), Throws.InstanceOf<WebSocketException>());

            using var alice = new ClientWebSocket();
            alice.Options.SetRequestHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("alice:secret")));
            await alice.ConnectAsync(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/RNs/rn1/history/ws"), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alice.State,                                            Is.EqualTo(WebSocketState.Open));
                Assert.That(await Eventually(() => webSocketServer!.WebSocketConnections.Any(connection => connection.Login == "alice")),  Is.True);
            }

            await alice.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);

        }

        #endregion

    }

}
