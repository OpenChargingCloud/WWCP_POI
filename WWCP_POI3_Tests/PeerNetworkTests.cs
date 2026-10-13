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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Sync
{

    /// <summary>
    /// Three servers without a leader: two peers of one server commit at the same time,
    /// push their commits, take the ones of the others, and all end with the same head.
    /// </summary>
    [TestFixture]
    public class PeerNetworkTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-13T12:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");

        // Status timestamps in the past: a later one is planned only.
        private static readonly DateTimeOffset              Past     = DateTimeOffset.Parse("2026-10-12T12:00:00Z");
        private const           String                      Operator = nameof(ChargingStationOperator);

        private HTTPServer?              httpServer;
        private POI_API?                 poiAPI;
        private RoamingNetworkHistory?   serverA;
        private readonly List<RoamingNetworkPeer> peers = [];

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            httpServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            poiAPI      = new POI_API(httpServer) {
                              // Commits are checked as every commit is: signed by their owners, or the merge of their parents.
                              AuthorizeCommitPush = (request, commits) => true
                          };

            serverA     = RoamingNetworkHistory.Start(
                              new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                                 DataSource:                "v1",
                                                 Created:                   Created,
                                                 ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                                          Created:        Created,
                                                                                                          ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                                             Created:  Created) ]) ]).WithKeys()
                          );

            poiAPI.AddRoamingNetworkHistory(serverA);

        }

        [TearDown]
        public async Task TearDown()
        {

            foreach (var peer in peers)
                await peer.DisposeAsync();

            peers.Clear();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

        }

        #endregion

        #region StartPeer(Options = null)

        /// <summary>
        /// A server starting from a snapshot of server A, and keeping in sync with it.
        /// </summary>
        private async Task<RoamingNetworkPeer> StartPeer(RoamingNetworkPeerOptions?  Options   = null,
                                                         RoamingNetworkHistory?      History   = null)
        {

            var peer = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/roamingNetworks/rn1/history/ws"),
                                              History,
                                              Options ?? new RoamingNetworkPeerOptions {
                                                                      MinReconnectDelay  = TimeSpan.FromMilliseconds(50),
                                                                      MaxReconnectDelay  = TimeSpan.FromMilliseconds(200)
                                                                  });

            peers.Add(peer);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await peer.StartAsync(timeout.Token);

            return peer;

        }

        #endregion

        #region Eventually(Condition)

        private static async Task<Boolean> Eventually(Func<Boolean> Condition)
        {

            for (var i = 0; i < 200; i++)
            {
                if (Condition())
                    return true;
                await Task.Delay(25);
            }

            return Condition();

        }

        #endregion


        #region Peers_commit_at_the_same_time_and_end_with_the_same_head()

        [Test]
        public async Task Peers_commit_at_the_same_time_and_end_with_the_same_head()
        {

            var peerB     = await StartPeer();
            var peerC     = await StartPeer();

            var historyB  = peerB.History!;
            var historyC  = peerC.History!;

            // Made at the same time, on server B by the operator, on server C by the roaming network operator.
            var poolB     = historyB.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            var changeB   = historyB.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(poolB, "hotline", null, "+49 800 1234") ],
                                                                               CreatedAt: T0.AddMinutes(1));
            var changeC   = historyC.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(historyC.Head.RoamingNetwork, "dataSource", null, "v2") ],
                                                                               CreatedAt: T0.AddMinutes(2));

            var resultB   = historyB.Commit(changeB);
            var resultC   = historyC.Commit(changeC);

            Boolean Converged()
            {

                // One head of server A for all comparisons: it may move on in between.
                var tips  = serverA!.Tips;
                var head  = tips[0];

                return tips.Count == 1 &&
                       historyB.Head.Commit.Id == head.Commit.Id &&
                       historyC.Head.Commit.Id == head.Commit.Id &&
                       head.RoamingNetwork.DataSource == "v2" &&
                       head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.ChargingPools.Single().HotlinePhoneNumber?.ToString() == "+49 800 1234";

            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(resultB.Status,                     Is.EqualTo(RoamingNetworkCommitStatus.Committed), resultB.ErrorResponse);
                Assert.That(resultC.Status,                     Is.EqualTo(RoamingNetworkCommitStatus.Committed), resultC.ErrorResponse);

                Assert.That(await Eventually(Converged),        Is.True,
                            $"A: {serverA!.Head.Commit.Id} ({serverA.Tips.Count} tips), B: {historyB.Head.Commit.Id}, C: {historyC.Head.Commit.Id}");

                Assert.That(serverA.Head.Commit.IsMerge,        Is.True);
                Assert.That(historyB.Head.RoamingNetwork.ETags, Is.EqualTo(serverA.Head.RoamingNetwork.ETags));
                Assert.That(historyC.Head.RoamingNetwork.ETags, Is.EqualTo(serverA.Head.RoamingNetwork.ETags));

            }

        }

        #endregion

        #region A_peer_keeps_its_history_in_a_store()

        [Test]
        public async Task A_peer_keeps_its_history_in_a_store()
        {

            var directory = Path.Combine(Path.GetTempPath(), "poi3-peer-" + Guid.NewGuid().ToString("N"));

            try
            {

                RoamingNetworkCommit_Id head;

                using (var store = new RoamingNetworkHistoryFileStore(directory))
                {

                    var peerB     = await StartPeer(new RoamingNetworkPeerOptions { Store = store });
                    var historyB  = peerB.History!;

                    Assert.That(historyB.Commit(historyB.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(historyB.Head.RoamingNetwork, "dataSource", null, "v2") ],
                                                                                                   CreatedAt: T0.AddMinutes(1))).IsCommitted);

                    Assert.That(await Eventually(() => serverA!.Head.Commit.Id == historyB.Head.Commit.Id), Is.True);

                    head = historyB.Head.Commit.Id;

                    await peerB.StopAsync();
                    historyB.DetachStore();

                }

                using var reopened = new RoamingNetworkHistoryFileStore(directory);

                Assert.That(RoamingNetworkHistory.TryLoad(reopened, out var loaded, out var error), Is.True, error);
                Assert.That(loaded!.Head.Commit.Id,                                                Is.EqualTo(head));
                Assert.That(loaded.Head.RoamingNetwork.DataSource,                                 Is.EqualTo("v2"));

            }
            finally
            {
                try { Directory.Delete(directory, recursive: true); } catch { }
            }

        }

        #endregion

        #region A_server_refusing_pushes_keeps_its_peer_from_pushing()

        [Test]
        public async Task A_server_refusing_pushes_keeps_its_peer_from_pushing()
        {

            poiAPI!.AuthorizeCommitPush = null;

            var errors  = new List<String>();
            var peerB   = await StartPeer();

            peerB.OnError += (peer, description, exception) => { lock (errors) errors.Add(description); };

            var historyB  = peerB.History!;
            historyB.Commit(historyB.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(historyB.Head.RoamingNetwork, "dataSource", null, "v2") ],
                                                                               CreatedAt: T0.AddMinutes(1)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(() => { lock (errors) return errors.Any(error => error.Contains("takes no commits")); }),  Is.True);
                Assert.That(serverA!.Head.RoamingNetwork.DataSource,  Is.EqualTo("v1"));
            }

        }

        #endregion

        #region A_snapshot_of_another_roaming_network_operator_is_not_trusted()

        [Test]
        public async Task A_snapshot_of_another_roaming_network_operator_is_not_trusted()
        {

            var trusted  = TestParties.Key(TestParties.Party(RoamingNetwork_Id.Parse("rn1")), POIKeyKind.Identity).Public.Id;

            // The right key: the peer starts.
            var peerB    = await StartPeer(new RoamingNetworkPeerOptions { TrustedRoamingNetworkOperatorKeyIds = [ trusted ] });

            // Another key: the peer never starts.
            var errors   = new List<String>();
            var peerC    = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/roamingNetworks/rn1/history/ws"),
                                                  Options: new RoamingNetworkPeerOptions {
                                                               TrustedRoamingNetworkOperatorKeyIds  = [ new String('0', 64) ],
                                                               MinReconnectDelay                    = TimeSpan.FromMilliseconds(50),
                                                               MaxReconnectDelay                    = TimeSpan.FromMilliseconds(200)
                                                           });

            peers.Add(peerC);
            peerC.OnError += (peer, description, exception) => { lock (errors) errors.Add(description); };

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(peerB.History,                                    Is.Not.Null);
                Assert.That(async () => await peerC.StartAsync(timeout.Token), Throws.InstanceOf<OperationCanceledException>());
                Assert.That(peerC.History,                                    Is.Null);
                lock (errors)
                    Assert.That(errors.Any(error => error.Contains("none of the trusted keys")),  Is.True);
            }

        }

        #endregion


        #region Status(History, Kind = Status, EntityType = Operator, EntityId = "DE*GEF")

        private static (String? Value, DateTimeOffset? Timestamp) Status(RoamingNetworkHistory  History,
                                                                         POIRuntimeStatusKind   Kind         = POIRuntimeStatusKind.Status,
                                                                         String                 EntityType   = Operator,
                                                                         String                 EntityId     = "DE*GEF")

            => History.RuntimeStatus.TryGet(Kind, EntityType, EntityId, out var status)
                   ? (status.Value, status.Timestamp)
                   : (null, null);

        #endregion


        #region A_peer_pushes_the_status_set_there_on_to_all_servers()

        [Test]
        public async Task A_peer_pushes_the_status_set_there_on_to_all_servers()
        {

            poiAPI!.AuthorizeStatusPush = (request, login, statusUpdate) => true;

            var peerB    = await StartPeer();
            var peerC    = await StartPeer();

            var updates  = 0;
            serverA!.RuntimeStatus.OnStatusUpdate += statusUpdate => Interlocked.Increment(ref updates);

            // Set at server B, e.g. by the operator's backend.
            var result   = peerB.History!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", Past, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(result,                                                         Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(await Eventually(() => Status(serverA).Value == "offline"),     Is.True, "Not at server A.");
                Assert.That(await Eventually(() => Status(peerC.History!).Value == "offline"), Is.True, "Not at server C.");

                // With the timestamp of server B, and once: nothing goes back and forth.
                await Task.Delay(300);

                Assert.That(Status(serverA).Timestamp,                                     Is.EqualTo(Past));
                Assert.That(Status(peerC.History!).Timestamp,                              Is.EqualTo(Past));
                Assert.That(Status(peerB.History!).Timestamp,                              Is.EqualTo(Past));
                Assert.That(Volatile.Read(ref updates),                                     Is.EqualTo(1));

            }

        }

        #endregion

        #region A_peer_pushes_the_status_set_while_not_connected()

        [Test]
        public async Task A_peer_pushes_the_status_set_while_not_connected()
        {

            poiAPI!.AuthorizeStatusPush = (request, login, statusUpdate) => true;

            // A history of server B, e.g. loaded from its store, with a status set while it was not connected.
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(serverA!.ToSnapshotJSON(), out var historyB, out var error), Is.True, error);

            historyB!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, Operator, "DE*GEF", "outOfService", Past, out _, out _);

            // A later status of server A wins at server B.
            serverA.RuntimeStatus.TryUpdate  (POIRuntimeStatusKind.Status,      Operator, "DE*GEF", "offline",      Past, out _, out _);
            historyB.RuntimeStatus.TryUpdate (POIRuntimeStatusKind.Status,      Operator, "DE*GEF", "available",    Past.AddMinutes(-1), out _, out _);

            await StartPeer(History: historyB);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(() => Status(serverA, POIRuntimeStatusKind.AdminStatus).Value == "outOfService"),  Is.True);
                Assert.That(await Eventually(() => Status(historyB).Value == "offline"),                                         Is.True);
                Assert.That(Status(serverA).Value,                                                                                Is.EqualTo("offline"));
            }

        }

        #endregion

        #region A_new_entity_is_pushed_before_its_status()

        [Test]
        public async Task A_new_entity_is_pushed_before_its_status()
        {

            poiAPI!.AuthorizeStatusPush = (request, login, statusUpdate) => true;

            var errors    = new List<String>();
            var peerB     = await StartPeer();
            var historyB  = peerB.History!;

            peerB.OnError += (peer, description, exception) => { lock (errors) errors.Add(description); };

            var committed = historyB.Commit(historyB.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P2"), Created: Created)) ],
                                                                                               CreatedAt: T0.AddMinutes(1)));

            var updated   = historyB.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(ChargingPool), "DE*GEF*P2", "offline", Past, out _, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(committed.IsCommitted,                                                                                    Is.True, committed.ErrorResponse);
                Assert.That(updated,                                                                                                  Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(await Eventually(() => Status(serverA!, EntityType: nameof(ChargingPool), EntityId: "DE*GEF*P2").Value == "offline"), Is.True);
                lock (errors)
                    Assert.That(errors,                                                                                               Is.Empty);
            }

        }

        #endregion

        #region A_server_refusing_status_pushes_still_takes_commits()

        [Test]
        public async Task A_server_refusing_status_pushes_still_takes_commits()
        {

            var errors    = new List<String>();
            var peerB     = await StartPeer();
            var historyB  = peerB.History!;

            peerB.OnError += (peer, description, exception) => { lock (errors) errors.Add(description); };

            historyB.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", Past, out _, out _);

            historyB.Commit(historyB.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(historyB.Head.RoamingNetwork, "dataSource", null, "v2") ],
                                                                               CreatedAt: T0.AddMinutes(1)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(() => { lock (errors) return errors.Any(error => error.Contains("takes no status updates")); }),  Is.True);
                Assert.That(await Eventually(() => serverA!.Head.RoamingNetwork.DataSource == "v2"),                                          Is.True);
                Assert.That(Status(serverA!).Value,                                                                                           Is.Not.EqualTo("offline"));
            }

        }

        #endregion

        #region A_server_decides_which_status_a_peer_may_push()

        [Test]
        public async Task A_server_decides_which_status_a_peer_may_push()
        {

            // E.g. only the status, not the admin status.
            poiAPI!.AuthorizeStatusPush = (request, login, statusUpdate) => statusUpdate.Kind == POIRuntimeStatusKind.Status;

            var errors    = new List<String>();
            var peerB     = await StartPeer();
            var historyB  = peerB.History!;

            peerB.OnError += (peer, description, exception) => { lock (errors) errors.Add(description); };

            historyB.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, Operator, "DE*GEF", "outOfService", Past, out _, out _);
            historyB.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      Operator, "DE*GEF", "offline",      Past, out _, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(() => Status(serverA!).Value == "offline"),                                               Is.True);
                Assert.That(await Eventually(() => { lock (errors) return errors.Any(error => error.Contains("refused 1 of")); }),      Is.True);
                Assert.That(Status(serverA!, POIRuntimeStatusKind.AdminStatus).Value,                                                   Is.Not.EqualTo("outOfService"));
            }

        }

        #endregion

    }

}
