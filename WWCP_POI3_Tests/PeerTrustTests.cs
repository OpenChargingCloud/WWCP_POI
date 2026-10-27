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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Sync
{

    /// <summary>
    /// A peer takes a history of another server only anchored in the trust anchor of the roaming network, and never
    /// replaces the one it holds by one not proven to follow it, nor by one of an epoch before (POI3-ISSUES.md S10;
    /// SPECIFICATION.md, sections 10.5 and 11.4).
    /// </summary>
    [TestFixture]
    public class PeerTrustTests
    {

        #region Data

        private static readonly DateTimeOffset  Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset  T0       = DateTimeOffset.Parse("2026-10-20T12:00:00Z");

        private readonly List<(HTTPServer HTTPServer, POI_API API)>  servers  = [];
        private readonly List<RoamingNetworkPeer>                     peers    = [];

        #endregion

        #region TearDown

        [TearDown]
        public async Task TearDown()
        {

            foreach (var peer in peers)
                await peer.DisposeAsync();

            foreach (var (httpServer, api) in servers)
            {
                await httpServer.DisposeAsync();
                await api.DisposeAsync();
            }

            peers.  Clear();
            servers.Clear();

        }

        #endregion


        #region (static) NewHistory(DataSource = "v0", Retention = null, Archive = null)

        /// <summary>
        /// A history of the roaming network rn1, its operator stating its identity key, its checkpoints signed by its operator.
        /// </summary>
        private static RoamingNetworkHistory NewHistory(String                           DataSource   = "v0",
                                                        RoamingNetworkHistoryRetention?  Retention    = null,
                                                        IRoamingNetworkHistoryArchive?   Archive      = null)
        {

            var history = RoamingNetworkHistory.Start(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                                                         DataSource:                DataSource,
                                                                         Created:                   Created,
                                                                         ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                                                                                  Created: Created) ]).WithKeys().WithKeyStatement(),
                                                      Store:      Retention is not null ? new RoamingNetworkHistoryMemoryStore() : null,
                                                      Retention:  Retention,
                                                      Archive:    Archive);

            history.CheckpointSigners = [ history.Head.RoamingNetwork.OperatorSigner() ];

            return history;

        }

        #endregion

        #region (static) Publish(History, Value)

        private static RoamingNetworkCommit Publish(RoamingNetworkHistory  History,
                                                    String                 Value)
        {

            var result = History.Commit(History.Head.RoamingNetwork.CreateSignedChangeSet(
                                            [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                                            CreatedAt: T0.AddMinutes((Double) History.Head.Commit.Revision + 1)));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            return result.Commit!;

        }

        #endregion

        #region (static) MakeCheckpoint(History)

        private static RoamingNetworkCheckpoint MakeCheckpoint(RoamingNetworkHistory History)
        {
            Assert.That(History.TryMakeCheckpoint(out var checkpoint, out var error, CreatedAt: T0.AddHours(History.Checkpoints.Count + 1)), Is.True, error);
            return checkpoint!;
        }

        #endregion

        #region Serve(History)

        /// <summary>
        /// Serve the given history; the URL of its WebSocket and its API.
        /// </summary>
        private (Uri URL, POI_API API) Serve(RoamingNetworkHistory History)
        {

            var httpServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            var api         = new POI_API(httpServer) {
                                  VerifyCheckpointSignature  = Verify(History),
                                  AuthorizeCommitPush        = (request, commits) => true
                              };

            api.AddRoamingNetworkHistory(History);

            servers.Add((httpServer, api));

            return (new Uri($"ws://127.0.0.1:{httpServer.TCPPort}/RNs/{History.RoamingNetworkId}/history/ws"), api);

        }

        #endregion

        #region (static) Verify(History)

        /// <summary>
        /// Verifies the checkpoint signatures of the operator of the roaming network of the given history.
        /// </summary>
        private static VerifyCheckpointSignatureDelegate Verify(RoamingNetworkHistory History)

            => TestParties.VerifyOperatorCheckpointSignature(History.Head.RoamingNetwork);

        #endregion

        #region NewPeer(URL, TrustAnchor, out Refusals, out Errors, History = null, Verify = null, FollowCheckpoints = true)

        /// <summary>
        /// A peer of the given server trusting the given trust anchor, continuing the given history, if any; its refusals
        /// and its errors are collected.
        /// </summary>
        private RoamingNetworkPeer NewPeer(Uri                                 URL,
                                           RoamingNetworkTrustAnchor?          TrustAnchor,
                                           out List<String>                    Refusals,
                                           out List<String>                    Errors,
                                           RoamingNetworkHistory?              History             = null,
                                           VerifyCheckpointSignatureDelegate?  Verify              = null,
                                           Boolean                             FollowCheckpoints   = true)
        {

            var refusals  = new List<String>();
            var errors    = new List<String>();

            var peer      = new RoamingNetworkPeer(URL,
                                                   History,
                                                   new RoamingNetworkPeerOptions {
                                                       TrustAnchor                = TrustAnchor,
                                                       VerifyCheckpointSignature  = Verify,
                                                       FollowCheckpoints          = FollowCheckpoints,
                                                       MinReconnectDelay          = TimeSpan.FromMilliseconds(50),
                                                       MaxReconnectDelay          = TimeSpan.FromMilliseconds(200)
                                                   });

            peer.OnHistoryReplacementRefused += (_, history, reason)       => { lock (refusals) refusals.Add(reason);    };
            peer.OnError                     += (_, description, exception) => { lock (errors)   errors.  Add(description); };

            peers.Add(peer);

            Refusals  = refusals;
            Errors    = errors;

            return peer;

        }

        #endregion

        #region (static) StartsWithin(Peer, Seconds)

        /// <summary>
        /// Whether the given peer synchronizes for the first time within the given seconds.
        /// </summary>
        private static async Task<Boolean> StartsWithin(RoamingNetworkPeer  Peer,
                                                        Double              Seconds)
        {

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Seconds));

            try
            {
                await Peer.StartAsync(timeout.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }

        }

        #endregion

        #region (static) Eventually(Condition)

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

        #region (static) Any(List, Text)

        private static Boolean Any(List<String> List, String Text)
        {
            lock (List)
                return List.Any(entry => entry.Contains(Text));
        }

        #endregion


        #region A_fabricated_snapshot_naming_the_keys_of_the_operator_is_not_taken()

        /// <summary>
        /// A server making up a roaming network names the keys of its operator, which are public, and may attest it with
        /// keys of its own: a snapshot is taken only attested by signing keys the identity keys of the trust anchor certify.
        /// </summary>
        [TestCase(false, Description = "Not attested")]
        [TestCase(true,  Description = "Attested in the role of the roaming network operator by a key not certified")]
        public async Task A_fabricated_snapshot_naming_the_keys_of_the_operator_is_not_taken(Boolean Attested)
        {

            var fabricated  = NewHistory("fabricated");

            if (Attested)
            {
                fabricated.AttestationSigners = [ Proofs.StationProofTests.RNOSigner ];
                Assert.That(fabricated.TryAttest(out _, out var attestError), Is.True, attestError);
            }

            var (url, _)    = Serve(fabricated);
            var anchor      = fabricated.Head.RoamingNetwork.TrustAnchor();
            var peer        = NewPeer(url, anchor, out _, out var errors);

            var started     = await StartsWithin(peer, 1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fabricated.Head.RoamingNetwork.Operator!.IdentityKeys.Select(key => key.Id),  Is.EquivalentTo(anchor.Keys), "It names the trusted keys.");
                Assert.That(fabricated.ToSnapshotJSON(IncludeAttestations: true)["attestations"]?.Count() ?? 0,  Is.EqualTo(Attested ? 1 : 0));

                Assert.That(started,                                         Is.False);
                Assert.That(peer.History,                                    Is.Null);
                Assert.That(Any(errors, "is not anchored in the trust anchor of the roaming network 'rn1'"),  Is.True);

            }

        }

        #endregion

        #region An_attested_snapshot_is_taken()

        /// <summary>
        /// A snapshot carries the attestations of its head: attested by the operator, as the trust anchor requires, it is
        /// taken, and the peer follows.
        /// </summary>
        [Test]
        public async Task An_attested_snapshot_is_taken()
        {

            var server    = NewHistory().Attesting();
            var (url, _)  = Serve(server);
            var peer      = NewPeer(url, server.Head.RoamingNetwork.TrustAnchor(), out var refusals, out _);

            var started   = await StartsWithin(peer, 10);

            Publish(server, "v1");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(started,                                                                   Is.True);
                Assert.That(server.VerifyHead(server.Head.RoamingNetwork.TrustAnchor(), out var error),  Is.True, error);
                Assert.That(await Eventually(() => peer.History?.Head.Commit.Id == server.Head.Commit.Id),  Is.True);
                Assert.That(refusals,                                                                  Is.Empty);

            }

        }

        #endregion

        #region Without_a_trust_anchor_a_peer_starts_no_history()

        /// <summary>
        /// Fail closed: without a trust anchor a peer takes no history of the other server; one given to it, it continues.
        /// </summary>
        [Test]
        public async Task Without_a_trust_anchor_a_peer_starts_no_history()
        {

            var server              = NewHistory().Attesting();
            var (url, _)            = Serve(server);

            var peer                = NewPeer(url, null, out _, out var errors);
            var started             = await StartsWithin(peer, 1);

            // One given its own history, e.g. persisted before, catches up without.
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(server.ToSnapshotJSON(), out var own, out var snapshotError),  Is.True, snapshotError);

            Publish(server, "v1");

            var continuing          = NewPeer(url, null, out _, out _, own);
            var continued           = await StartsWithin(continuing, 10);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(started,                                           Is.False);
                Assert.That(peer.History,                                      Is.Null);
                Assert.That(Any(errors, "Without the trust anchor of the roaming network"),  Is.True);

                Assert.That(continued,                                         Is.True);
                Assert.That(continuing.History,                                Is.SameAs(own));
                Assert.That(own!.Head.Commit.Id,                               Is.EqualTo(server.Head.Commit.Id));

            }

        }

        #endregion

        #region A_rolled_back_server_does_not_roll_back_a_peer()

        /// <summary>
        /// A server knowing none of the commits of a peer, e.g. restored from an old backup, serves a snapshot attested by
        /// the operator, but not following the head of the peer: the peer keeps its history, with its commit not pushed yet,
        /// tells why, and asks again after waiting, until its caller resets it.
        /// </summary>
        [TestCase(false, Description = "From a snapshot of the head")]
        [TestCase(true,  Description = "From a checkpoint of the other server, which does not prove the commits of the peer")]
        public async Task A_rolled_back_server_does_not_roll_back_a_peer(Boolean JoinFromCheckpoint)
        {

            // The history of the peer: from the server as it was, with a commit of its own, not pushed yet.
            var before        = NewHistory().Attesting();
            Publish(before, "v1");
            Publish(before, "v2");

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(before.ToSnapshotJSON(), out var own, out var snapshotError),  Is.True, snapshotError);

            var local         = Publish(own!, "local");

            // The server rolled back, to a version before: attested by the operator all the same.
            var rolledBack    = NewHistory().Attesting();

            if (JoinFromCheckpoint)
            {
                Publish(rolledBack, "v1 again");
                MakeCheckpoint(rolledBack);
            }

            var rolledBackHead  = rolledBack.Head.Commit.Id;

            var (url, _)      = Serve(rolledBack);
            var peer          = NewPeer(url, rolledBack.Head.RoamingNetwork.TrustAnchor(), out var refusals, out _, own, Verify(rolledBack));

            var started       = await StartsWithin(peer, 1);

            Int32 refusalCount;
            lock (refusals)
                refusalCount  = refusals.Count;

            var reconnects    = peer.Reconnects;
            var kept          = peer.History;

            // Its caller decides: anew from the other server, discarding the commit not pushed.
            await peer.ResetHistoryAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(started,                                 Is.False);
                Assert.That(kept,                                    Is.SameAs(own));
                Assert.That(own.Head.Commit.Id,                      Is.EqualTo(local.Id), "The commit not pushed is still the head.");
                Assert.That(own.Contains(local.Id),                  Is.True);

                Assert.That(refusalCount,                            Is.GreaterThan(0));
                Assert.That(refusalCount,                            Is.LessThanOrEqualTo(reconnects + 1), "The peer asked again without waiting.");

                Assert.That(Any(refusals, JoinFromCheckpoint
                                              ? $"The other server proves not that the commit '{local.Id}' held here is one of its checkpoints"
                                              : "does not prove to follow the head"),  Is.True);

                Assert.That(rolledBack.Head.Commit.Id,               Is.EqualTo(rolledBackHead), "Nothing was pushed to it.");

                // After the reset.
                Assert.That(peer.History,                            Is.Not.SameAs(own));
                Assert.That(peer.History!.Contains(local.Id),        Is.False);
                Assert.That(await Eventually(() => peer.History.Head.Commit.Id == rolledBack.Head.Commit.Id),  Is.True);
                Assert.That(own.Contains(local.Id),                  Is.True, "The history before stays as it is.");

            }

        }

        #endregion

        #region A_peer_unknown_to_a_compacted_server_rejoins_from_a_checkpoint_following_it()

        /// <summary>
        /// A server that dropped the commits a peer holds, compacting, knows none of them: the peer takes the latest
        /// checkpoint, as it proves the head of the peer to be one of the commits of a checkpoint before it in its line.
        /// </summary>
        [Test]
        public async Task A_peer_unknown_to_a_compacted_server_rejoins_from_a_checkpoint_following_it()
        {

            var server    = NewHistory(Retention: new RoamingNetworkHistoryRetention {
                                                      VersionCheckpointInterval  = 1,
                                                      RecentVersions             = 100,
                                                      RetainCommits              = 2,
                                                      CompactAfterLogEntries     = Int64.MaxValue
                                                  },
                                       Archive:   new RoamingNetworkHistoryMemoryArchive()).Attesting();

            var c1        = Publish(server, "v1");

            // The peer, stopped at the first commit.
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(server.ToSnapshotJSON(), out var own, out var snapshotError),  Is.True, snapshotError);

            var cp0       = MakeCheckpoint(server);
            Publish(server, "v2");
            var c3        = Publish(server, "v3");
            var cp1       = MakeCheckpoint(server);
            Publish(server, "v4");
            Publish(server, "v5");
            Publish(server, "v6");

            Assert.That(server.Compact(out var compactError),  Is.True, compactError);

            var (url, _)  = Serve(server);
            var replaced  = new List<(RoamingNetworkHistory? Old, RoamingNetworkHistory New)>();
            var peer      = NewPeer(url, server.Head.RoamingNetwork.TrustAnchor(), out var refusals, out _, own, Verify(server));

            peer.OnHistoryReplaced += (_, oldHistory, newHistory) => { lock (replaced) replaced.Add((oldHistory, newHistory)); };

            var started   = await StartsWithin(peer, 10);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(server.Contains(c1.Id),                    Is.False, "Dropped there.");
                Assert.That(server.TryGetCommitProof(c1.Id, out var proof, out _) && proof.CheckpointId == cp0.Id,  Is.True);

                Assert.That(started,                                   Is.True);
                Assert.That(refusals,                                  Is.Empty);

                lock (replaced)
                {
                    Assert.That(replaced,                              Has.Count.EqualTo(1));
                    Assert.That(replaced[0].Old,                       Is.SameAs(own));
                }

                Assert.That(peer.History!.Root.Id,                     Is.EqualTo(c3.Id), "From the checkpoint following its head.");
                Assert.That(peer.History.LatestCheckpoint?.Id,         Is.EqualTo(cp1.Id));
                Assert.That(peer.History.Head.Commit.Id,               Is.EqualTo(server.Head.Commit.Id));

            }

        }

        #endregion

        #region A_peer_moves_to_the_next_epoch_only_by_an_anchored_snapshot()

        /// <summary>
        /// A peer without the final checkpoint of its epoch takes the next epoch from a snapshot of the other server only
        /// attested by the operator of the roaming network; it keeps its epoch until then.
        /// </summary>
        [Test]
        public async Task A_peer_moves_to_the_next_epoch_only_by_an_anchored_snapshot()
        {

            var server    = NewHistory().Attesting();
            var (url, api) = Serve(server);

            // Not following checkpoints: it never holds the final one.
            var peer      = NewPeer(url, server.Head.RoamingNetwork.TrustAnchor(), out var refusals, out _, FollowCheckpoints: false);

            Assert.That(await StartsWithin(peer, 10),  Is.True);

            var epoch1    = peer.History!;

            Publish(server, "v1");
            MakeCheckpoint(server);

            Assert.That(server.TryCloseEpoch(new RoamingNetworkEpochProfiles(ETagHashAlgorithm.SHA512), out _, out var closeError, T0.AddHours(5)),  Is.True, closeError);
            Assert.That(server.TryStartNextEpoch(out var next, out var startError),                                                                 Is.True, startError);

            // Served, but not attested yet.
            Assert.That(api.TryServeNextEpoch(next!, out var serveError),  Is.True, serveError);

            var refused   = await Eventually(() => Any(refusals, "is not anchored in the trust anchor"));
            var kept      = peer.History;

            next!.Attesting();

            var moved     = await Eventually(() => peer.History?.Epoch.Number == 2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(refused,                         Is.True, "The peer took an unanchored snapshot of the next epoch.");
                Assert.That(kept,                            Is.SameAs(epoch1));
                Assert.That(moved,                           Is.True, "The peer did not move to the next epoch.");
                Assert.That(peer.History!.Head.Commit.Id,    Is.EqualTo(next.Head.Commit.Id));

            }

        }

        #endregion

        #region A_reset_never_goes_back_to_an_epoch_before()

        /// <summary>
        /// Not even its caller moves a peer back to an epoch before: a reset waits for a history of its epoch or a later one.
        /// </summary>
        [Test]
        public async Task A_reset_never_goes_back_to_an_epoch_before()
        {

            var server    = NewHistory().Attesting();

            Publish(server, "v1");
            MakeCheckpoint(server);

            Assert.That(server.TryCloseEpoch(new RoamingNetworkEpochProfiles(ETagHashAlgorithm.SHA512), out _, out var closeError, T0.AddHours(5)),  Is.True, closeError);
            Assert.That(server.TryStartNextEpoch(out var next, out var startError),                                                                 Is.True, startError);

            // The other server serves the epoch before.
            var (url, _)  = Serve(server);
            var peer      = NewPeer(url, server.Head.RoamingNetwork.TrustAnchor(), out var refusals, out _, next);

            _             = peer.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token).ContinueWith(_ => { });

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(async () => await peer.ResetHistoryAsync(timeout.Token),  Throws.InstanceOf<OperationCanceledException>());
                Assert.That(peer.History,                                            Is.SameAs(next));
                Assert.That(Any(refusals, "of the epoch 1, this one is of the epoch 2"),  Is.True);

            }

        }

        #endregion

    }

}
