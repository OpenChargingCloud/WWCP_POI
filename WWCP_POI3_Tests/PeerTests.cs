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

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Sync
{

    /// <summary>
    /// A follower keeping its history in sync with its leader over the WebSocket.
    /// </summary>
    [TestFixture]
    public class PeerTests
    {

        #region Data

        private const           String          Operator  = nameof(ChargingStationOperator);
        private static readonly DateTimeOffset  T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private HTTPServer?              httpServer;
        private POI_API?                 poiAPI;
        private RoamingNetworkHistory?   leader;
        private POIWebSocketServer?      webSocketServer;
        private RoamingNetworkPeer?  follower;

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
            leader      = NewHistory().Attesting();

            poiAPI.AddRoamingNetworkHistory(leader);
            poiAPI.TryGetHistoryWebSocketServer(leader.RoamingNetworkId, out webSocketServer);

        }

        [TearDown]
        public async Task TearDown()
        {

            if (follower is not null)
                await follower.DisposeAsync();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

            follower = null;

        }

        #endregion


        #region NewHistory(DataSource = "v0")

        private static RoamingNetworkHistory NewHistory(String DataSource = "v0")

            => RoamingNetworkHistory.Start(
                   new RoamingNetwork(
                       Id:                        RoamingNetwork_Id.Parse("rn1"),
                       DataSource:                DataSource,
                       Created:                   DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                       ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                                I18NString.Create(Languages.en, "GraphDefined"),
                                                                                Created: DateTimeOffset.Parse("2026-01-01T00:00:00Z")) ]
                   ).WithKeys().WithKeyStatement()
               );

        #endregion

        #region TrustAnchor

        /// <summary>
        /// The trust anchor of the roaming network of the leader: the identity key of its operator.
        /// </summary>
        private RoamingNetworkTrustAnchor TrustAnchor
            => leader!.Head.RoamingNetwork.TrustAnchor();

        #endregion

        #region Publish(Value)

        private RoamingNetworkCommitResult Publish(String Value)

            => leader!.Commit(
                   leader.Head.RoamingNetwork.CreateSignedChangeSet(
                       [ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, Value) ],
                       Description:  I18NString.Create(Languages.en, "Data source " + Value),
                       CreatedAt:    T0.AddMinutes((Double) leader.Head.Commit.Revision + 1)
                   )
               );

        #endregion

        #region StartFollower(History = null, Options = null)

        private async Task<RoamingNetworkPeer> StartFollower(RoamingNetworkHistory?          History   = null,
                                                                 RoamingNetworkPeerOptions?  Options   = null)
        {

            follower = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/RNs/rn1/history/ws"),
                                                  History,
                                                  Options ?? new RoamingNetworkPeerOptions {
                                                                 TrustAnchor        = TrustAnchor,
                                                                 MinReconnectDelay  = TimeSpan.FromMilliseconds(50),
                                                                 MaxReconnectDelay  = TimeSpan.FromMilliseconds(200)
                                                             });

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await follower.StartAsync(timeout.Token);

            return follower;

        }

        #endregion

        #region InSync()

        private Boolean InSync()

            => follower?.History?.Head.Commit.Id == leader!.Head.Commit.Id;

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


        #region Checkpoints_reach_peers_and_witness_signatures_come_back()

        [Test]
        public async Task Checkpoints_reach_peers_and_witness_signatures_come_back()
        {

            var verify = History.CheckpointTests.Verify;

            leader!.CheckpointSigners          = [ History.CheckpointTests.RNOSigner ];
            poiAPI!.VerifyCheckpointSignature  = verify;
            poiAPI.AuthorizeCheckpointSignature = (request, checkpoint, signature) => signature.Role == RoamingNetworkCommitSignatureRoles.Witness;

            Publish("v1");

            Assert.That(leader.TryMakeCheckpoint(out var cp0, out var cp0Error, CreatedAt: T0.AddHours(1)),  Is.True, cp0Error);

            var peer      = await StartFollower(Options: new RoamingNetworkPeerOptions {
                                                             TrustAnchor                = TrustAnchor,
                                                             VerifyCheckpointSignature  = verify,
                                                             MinReconnectDelay          = TimeSpan.FromMilliseconds(50),
                                                             MaxReconnectDelay          = TimeSpan.FromMilliseconds(200)
                                                         });

            var tookFirst = await Eventually(() => peer.History?.LatestCheckpoint?.Id == cp0!.Id);

            var c2        = Publish("v2");
            Assert.That(leader.TryMakeCheckpoint(out var cp1, out var cp1Error, CreatedAt: T0.AddHours(2)),  Is.True, cp1Error);

            var tookNext  = await Eventually(() => peer.History?.LatestCheckpoint?.Id == cp1!.Id);

            // The peer is run by a witness: its signature goes back to the server that made the checkpoint.
            Assert.That(peer.History!.TryWitness(cp1!.Id, History.CheckpointTests.Witness(), out _, out _, out var witnessError),  Is.True, witnessError);

            var signedThere = await Eventually(() => leader.LatestCheckpoint!.Signatures.Any(signature => signature.Role == RoamingNetworkCommitSignatureRoles.Witness));

            using var httpClient = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpServer!.TCPPort}/RNs/rn1/history/") };

            var proofText        = await httpClient.GetStringAsync($"commits/{c2.Commit!.Id}/proof");
            var checkpointsText  = await httpClient.GetStringAsync("checkpoints?since=0");

            Assert.That(RoamingNetworkCheckpointProof.TryParse(proofText, out var proof, out var proofError),  Is.True, proofError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(tookFirst,                                         Is.True, "The peer did not take the checkpoint of the commit it started from.");
                Assert.That(tookNext,                                          Is.True, "The peer did not take the pushed checkpoint.");
                Assert.That(signedThere,                                       Is.True, "The signature of the witness did not reach the server that made the checkpoint.");

                Assert.That(proof!.CheckpointId,                               Is.EqualTo(cp1.Id));
                Assert.That(proof.Verify(leader.LatestCheckpoint!, out var verifyError),  Is.True, verifyError);
                Assert.That(JArray.Parse(checkpointsText).Select(checkpoint => checkpoint["@id"]!.Value<String>()),
                            Is.EqualTo(new[] { cp1.Id.ToString() }));

            }

        }

        #endregion

        #region Attestations_reach_peers_that_trust_their_key()

        [Test]
        public async Task Attestations_reach_peers_that_trust_their_key()
        {

            // Its operator anchors the snapshot; the peer takes the attestations of the key it trusts besides.
            leader!.AttestationSigners = [ leader.Head.RoamingNetwork.OperatorSigner(), Proofs.StationProofTests.RNOSigner ];

            Assert.That(leader.TryAttest(out var attested, out var attestError), Is.True, attestError);

            var first      = attested!.Single(attestation => attestation.KeyId == Proofs.StationProofTests.RNOSigner.KeyId);

            var peer       = await StartFollower(Options: new RoamingNetworkPeerOptions {
                                                              TrustAnchor        = TrustAnchor,
                                                              VerifyAttestation  = Proofs.StationProofTests.Trusted,
                                                              MinReconnectDelay  = TimeSpan.FromMilliseconds(50),
                                                              MaxReconnectDelay  = TimeSpan.FromMilliseconds(200)
                                                          });

            var tookFirst  = await Eventually(() => peer.History?.CurrentAttestations().Any(attestation => attestation.Value == first.Value) == true);

            // A new head is attested there, and the attestation follows the commit.
            var c2         = Publish("v2");
            var tookNext   = await Eventually(() => peer.History?.CurrentAttestations().Any(attestation => attestation.CommitId == c2.Commit!.Id) == true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tookFirst,                                            Is.True, "The peer did not take the attestation of the head it started from.");
                Assert.That(tookNext,                                             Is.True, "The peer did not take the attestation of the new head.");
                Assert.That(peer.History!.CurrentAttestations().Single().ETag,    Is.EqualTo(leader.Head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256)));
            }

        }

        #endregion

        #region A_follower_starts_from_a_snapshot_and_follows()

        [Test]
        public async Task A_follower_starts_from_a_snapshot_and_follows()
        {

            Publish("v1");

            leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0, out _, out _);

            var replaced  = 0;
            var follower  = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/RNs/rn1/history/ws"),
                                                   Options: new RoamingNetworkPeerOptions { TrustAnchor = TrustAnchor });
            this.follower = follower;

            follower.OnHistoryReplaced += (_, oldHistory, newHistory) => replaced++;

            await follower.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            var initiallyInSync  = InSync();
            var initialStatus    = follower.History!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status) ? status.Value : null;

            Publish("v2");
            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, Operator, "DE*GEF", "outOfService", T0, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(initiallyInSync,                Is.True);
                Assert.That(replaced,                       Is.EqualTo(1));
                Assert.That(initialStatus,                  Is.EqualTo("offline"));
                Assert.That(follower.IsConnected,           Is.True);

                Assert.That(await Eventually(InSync),       Is.True, "The follower did not take the pushed commit.");
                Assert.That(follower.History.Head.RoamingNetwork.DataSource,  Is.EqualTo("v2"));

                Assert.That(await Eventually(() => follower.History.RuntimeStatus.TryGet(POIRuntimeStatusKind.AdminStatus, Operator, "DE*GEF", out var adminStatus) &&
                                                   adminStatus.Value == "outOfService"),
                            Is.True,
                            "The follower did not take the pushed admin status.");

            }

        }

        #endregion

        #region Announced_and_large_commits_are_fetched()

        [TestCase(false, Description = "Subscribed to the announcements only")]
        [TestCase(true,  Description = "Subscribed to complete commits, but they are too large")]
        public async Task Announced_and_large_commits_are_fetched(Boolean PushCompleteCommits)
        {

            poiAPI!.MaxPushedCommitSize = 100;

            await StartFollower(Options: new RoamingNetworkPeerOptions {
                                             TrustAnchor          = TrustAnchor,
                                             PushCompleteCommits  = PushCompleteCommits,
                                             MinReconnectDelay    = TimeSpan.FromMilliseconds(50)
                                         });

            Publish("v1");
            Publish("v2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(InSync),                            Is.True);
                Assert.That(follower!.History!.Head.RoamingNetwork.DataSource,  Is.EqualTo("v2"));
                Assert.That(follower.Reconnects,                                 Is.Zero);
            }

        }

        #endregion

        #region A_follower_continues_its_own_history()

        [Test]
        public async Task A_follower_continues_its_own_history()
        {

            RoamingNetworkHistory.TryFromSnapshot(leader!.ToSnapshotJSON(), out var own, out _);

            Publish("v1");
            Publish("v2");
            Publish("v3");

            var follower = await StartFollower(own);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(follower.History,                Is.SameAs(own), "The follower started again instead of catching up.");
                Assert.That(InSync(),                        Is.True);
                Assert.That(own!.Head.Commit.Revision,       Is.EqualTo(3));
            }

        }

        #endregion

        #region A_follower_far_behind_catches_up_naming_few_commits()

        /// <summary>
        /// A follower hundreds of commits behind catches up page by page, naming the commits it holds as HaveIds() names
        /// them: its tips and a sample of older ones, far fewer than a server takes (POI3-ISSUES.md S12).
        /// </summary>
        [Test]
        public async Task A_follower_far_behind_catches_up_naming_few_commits()
        {

            RoamingNetworkHistory.TryFromSnapshot(leader!.ToSnapshotJSON(), out var own, out _);

            for (var i = 1; i <= 300; i++)
                Assert.That(Publish($"v{i}").IsCommitted, Is.True);

            // A server taking far fewer than the default, but as many as an honest peer names.
            poiAPI!.MaxHaveIds = 40;

            var follower = await StartFollower(own,
                                               new RoamingNetworkPeerOptions {
                                                   TrustAnchor           = TrustAnchor,
                                                   MaxCommitsPerRequest  = 25,
                                                   MinReconnectDelay     = TimeSpan.FromMilliseconds(50),
                                                   MaxReconnectDelay     = TimeSpan.FromMilliseconds(200)
                                               });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(follower.History,                Is.SameAs(own), "The follower started again instead of catching up.");
                Assert.That(InSync(),                        Is.True);
                Assert.That(own!.Head.Commit.Revision,       Is.EqualTo(300));
                Assert.That(own.HaveIds(),                   Has.Count.LessThanOrEqualTo(40));
            }

        }

        #endregion

        #region A_follower_of_a_head_unknown_to_the_leader_keeps_it_until_reset()

        /// <summary>
        /// A follower whose history the leader knows nothing of keeps it: a snapshot of the head of the leader does not prove
        /// to follow it (POI3-ISSUES.md S10). It tells why, and asks again after waiting; only its caller starts it anew.
        /// </summary>
        [Test]
        public async Task A_follower_of_a_head_unknown_to_the_leader_keeps_it_until_reset()
        {

            // The same roaming network, but another history: its head is unknown to the leader.
            var other     = NewHistory("other");
            var otherHead = other.Head.Commit.Id;
            var replaced  = new List<(RoamingNetworkHistory? Old, RoamingNetworkHistory New)>();
            var refused   = new List<(RoamingNetworkHistory History, String Reason)>();

            follower = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/RNs/rn1/history/ws"),
                                              other,
                                              new RoamingNetworkPeerOptions {
                                                  TrustAnchor        = TrustAnchor,
                                                  MinReconnectDelay  = TimeSpan.FromMilliseconds(50),
                                                  MaxReconnectDelay  = TimeSpan.FromMilliseconds(200)
                                              });

            // Slow to take it: the reset returns once it was told, not before.
            follower.OnHistoryReplaced           += (_, oldHistory, newHistory) => { Thread.Sleep(200); lock (replaced) replaced.Add((oldHistory, newHistory)); };
            follower.OnHistoryReplacementRefused += (_, history,    reason)     => { lock (refused)  refused. Add((history,    reason));     };

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                Assert.That(async () => await follower.StartAsync(timeout.Token), Throws.InstanceOf<OperationCanceledException>());

            Int32 refusals;
            lock (refused)
                refusals = refused.Count;

            var keptHistory  = follower.History;
            var reconnects   = follower.Reconnects;

            // The caller decides: anew from the leader.
            await follower.ResetHistoryAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(keptHistory,                Is.SameAs(other),      "The follower replaced its history.");
                Assert.That(other.Head.Commit.Id,       Is.EqualTo(otherHead));
                Assert.That(refusals,                   Is.GreaterThan(0));
                Assert.That(refusals,                   Is.LessThanOrEqualTo(reconnects + 1), "The follower asked again without waiting.");

                lock (refused)
                {
                    Assert.That(refused[0].History,     Is.SameAs(other));
                    Assert.That(refused[0].Reason,      Does.Contain("does not prove to follow"));
                }

                lock (replaced)
                {
                    Assert.That(replaced.Count,         Is.EqualTo(1));
                    Assert.That(replaced[0].Old,        Is.SameAs(other));
                }

                Assert.That(follower.History,           Is.Not.SameAs(other));
                Assert.That(await Eventually(InSync),   Is.True);

            }

        }

        #endregion

        #region A_follower_reconnects_and_catches_up()

        [Test]
        public async Task A_follower_reconnects_and_catches_up()
        {

            var follower = await StartFollower();

            // The leader throws everybody off...
            foreach (var session in webSocketServer!.Sessions.ToArray())
                await session.Connection.Close(WebSocketFrame.ClosingStatusCode.GoingAway);

            // ...and changes while the follower may still be away.
            Publish("v1");
            leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0, out _, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(() => follower.Reconnects > 0 && follower.IsConnected),  Is.True, "The follower did not connect again.");
                Assert.That(await Eventually(InSync),                                                 Is.True);
                Assert.That(await Eventually(() => follower.History!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status) &&
                                                   status.Value == "offline"),
                            Is.True);
            }

        }

        #endregion

        #region A_follower_submits_to_its_leader()

        [Test]
        public async Task A_follower_submits_to_its_leader()
        {

            var follower   = await StartFollower();

            var head       = follower.History!.Head;
            var changeSet  = head.RoamingNetwork.CreateSignedChangeSet(
                                 [ RoamingNetworkChange.UpdateProperty(head.RoamingNetwork, "dataSource", null, "from the follower") ],
                                 CreatedAt: T0
                             );

            POIWebSocketRequestException? refused = null;

            try
            {
                await follower.CommitChangeSetAsync(changeSet);
            }
            catch (POIWebSocketRequestException e)
            {
                refused = e;
            }

            poiAPI!.AuthorizeChangeSetSubmission  = (request, changeSet)            => true;
            poiAPI.AuthorizeStatusUpdate          = (request, login, statusUpdate)  => true;

            var published  = await follower.CommitChangeSetAsync(changeSet);
            var status     = await follower.SetStatusAsync(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(refused?.ErrorCode,                      Is.EqualTo(POIWebSocketErrorCodes.SecurityError));
                Assert.That(published["status"]!.ToString(),         Is.EqualTo("Committed"));
                Assert.That(status["result"]!.ToString(),            Is.EqualTo("Updated"));

                // Back from the leader, as for every other follower.
                Assert.That(await Eventually(InSync),                Is.True);
                Assert.That(follower.History.Head.RoamingNetwork.DataSource,  Is.EqualTo("from the follower"));
                Assert.That(await Eventually(() => follower.History.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var operatorStatus) &&
                                                   operatorStatus.Value == "offline"),
                            Is.True);

            }

        }

        #endregion

        #region A_follower_checks_commit_signatures_and_signs_commits()

        [Test]
        public async Task A_follower_checks_commit_signatures_and_signs_commits()
        {

            var verify = History.CommitSignatureTests.VerifyCommit;

            leader!.Signers = [ History.CommitSignatureTests.LeaderSigner ];

            // Only a roaming network operator may sign later, and only its signatures are told to the followers.
            poiAPI!.AuthorizeCommitSignature  = (request, commit, signature) => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator;
            poiAPI.VerifyCommitSignature      = verify;

            Publish("v1");

            var follower   = await StartFollower(Options: new RoamingNetworkPeerOptions {
                                                               TrustAnchor             = TrustAnchor,
                                                               VerifyCommitSignature   = verify,
                                                               RequiredSignatureRoles  = [ RoamingNetworkCommitSignatureRoles.Leader ],
                                                               MinReconnectDelay       = TimeSpan.FromMilliseconds(50),
                                                               MaxReconnectDelay       = TimeSpan.FromMilliseconds(200)
                                                           });

            Publish("v2");

            var inSync     = await Eventually(InSync);
            var commit     = follower.History!.Head.Commit;

            // The follower is run by the roaming network operator, which checked the data of the commit.
            var signature  = commit.CreateSignature(History.CommitSignatureTests.RNOKey,
                                                    "rno",
                                                    COSEAlgorithm.Ed25519,
                                                    RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                    "DE*RNO");

            var answer     = await follower.SignCommitAsync(commit.Id, [ signature ]);

            POIWebSocketRequestException? refused = null;

            try
            {
                await follower.SignCommitAsync(commit.Id, [ commit.CreateSignature(History.CommitSignatureTests.RNOKey,
                                                                                   "rno",
                                                                                   COSEAlgorithm.Ed25519,
                                                                                   RoamingNetworkCommitSignatureRoles.Leader) ]);
            }
            catch (POIWebSocketRequestException e)
            {
                refused = e;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(inSync,                                                   Is.True, "The follower did not take the signed commit.");
                Assert.That(commit.Signatures.Select(signature => signature.Role),    Is.EqualTo(new[] { "leader" }));

                Assert.That(answer["added"]!.Value<Int32>(),                          Is.EqualTo(1));
                Assert.That(answer["signatures"]!.Count(),                            Is.EqualTo(2));
                Assert.That(refused?.ErrorCode,                                       Is.EqualTo(POIWebSocketErrorCodes.SecurityError));

                Assert.That(leader.Head.Commit.Signatures,                            Has.Length.EqualTo(2));
                Assert.That(await Eventually(() => follower.History.Head.Commit.Signatures.Length == 2),
                            Is.True,
                            "The follower did not take the pushed commit signature.");

            }

        }

        #endregion

        #region A_follower_checks_the_head_commit_of_its_snapshot_in_the_version_of_the_snapshot()

        [Test]
        public async Task A_follower_checks_the_head_commit_of_its_snapshot_in_the_version_of_the_snapshot()
        {

            // Signed by the certified signing key of the operator, which the verifier looks up in the version it is given only:
            // no network from elsewhere, before the follower has a history of its own.
            leader!.Signers = [ leader.Head.RoamingNetwork.OperatorSigner() ];

            Publish("v1");

            var head      = leader.Head.Commit;
            var given     = new ConcurrentQueue<(RoamingNetworkCommit_Id CommitId, String? DataSource, Boolean HasItsETags)>();

            var follower  = await StartFollower(Options: new RoamingNetworkPeerOptions {
                                                              TrustAnchor             = TrustAnchor,
                                                              VerifyCommitSignature   = (commit, version, signature) => {
                                                                                            given.Enqueue((commit.Id, version.DataSource, version.MatchesCommitETags(commit.ETags)));
                                                                                            return History.CommitSignatureTests.VerifyByCertifiedKeys(commit, version, signature);
                                                                                        },
                                                              RequiredSignatureRoles  = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ],
                                                              MinReconnectDelay       = TimeSpan.FromMilliseconds(50),
                                                              MaxReconnectDelay       = TimeSpan.FromMilliseconds(200)
                                                          });

            var started   = follower.History?.Head.Commit.Id;

            // A commit pushed afterwards: in the version calculated for it.
            Publish("v2");

            var inSync    = await Eventually(InSync);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(started,                Is.EqualTo(head.Id));
                Assert.That(inSync,                 Is.True, "The follower did not take the signed commit.");
                Assert.That(given.First(),          Is.EqualTo((head.Id, (String?) "v1", true)));
                Assert.That(given,                  Does.Contain((leader.Head.Commit.Id, (String?) "v2", true)));
                Assert.That(given.All(call => call.HasItsETags),  Is.True);

            }

        }

        #endregion

        #region A_follower_sets_and_follows_energy_statuses()

        [Test]
        public async Task A_follower_sets_and_follows_energy_statuses()
        {

            var e1 = EVSE_Id.Parse("DE*GEF*E1*1");

            leader!.Commit(leader.Head.RoamingNetwork.CreateSignedChangeSet([
                                RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                          ChargingStations: [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                  EVSEs: [ new EVSE(e1) ]) ]))
                            ], CreatedAt: T0));

            poiAPI!.AuthorizeStatusUpdate = (request, login, statusUpdate) => true;

            var follower  = await StartFollower();

            var answer    = await follower.SetEnergyStatusAsync(nameof(EVSE),
                                                                e1.ToString(),
                                                                new EnergyInfo(Watt.FromW(11000), Watt.FromW(22000)),
                                                                T0.AddMinutes(1));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(answer["result"]!.ToString(),  Is.EqualTo("Updated"));

                // Back from the leader as a pushed energy status update.
                Assert.That(await Eventually(() => follower.History!.RuntimeStatus.TryGet(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE), e1.ToString(), out var energyStatus) &&
                                                   energyStatus.Value == POIEnergyStatus.ToText(new EnergyInfo(Watt.FromW(11000), Watt.FromW(22000)))),
                            Is.True);

            }

        }

        #endregion


    }

}
