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

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Sync
{

    /// <summary>
    /// Joining from a checkpoint (SPECIFICATION.md, section 10.5): a server or client without a history starts from the
    /// version of the latest checkpoint carrying the signatures it requires, and takes the commits since from there.
    /// </summary>
    [TestFixture]
    public class JoinFromCheckpointTests
    {

        #region Data

        private const           String          Operator  = nameof(ChargingStationOperator);
        private static readonly DateTimeOffset  T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private HTTPServer?              httpServer;
        private POI_API?                 poiAPI;
        private RoamingNetworkHistory?   leader;
        private RoamingNetworkPeer?      peer;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            httpServer                         = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            poiAPI                             = new POI_API(httpServer);
            leader                             = RoamingNetworkHistory.Start(Proofs.StationProofTests.NewNetwork());

            leader.CheckpointSigners           = [ History.CheckpointTests.RNOSigner ];
            leader.PriceStatementSigners       = [ Proofs.PriceStatementTests.Signer(ChargingStationOperator_Id.Parse("DE*GEF")) ];
            poiAPI.VerifyCheckpointSignature   = History.CheckpointTests.Verify;

            poiAPI.AddRoamingNetworkHistory(leader);

        }

        [TearDown]
        public async Task TearDown()
        {

            if (peer is not null)
                await peer.DisposeAsync();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

            peer = null;

        }

        #endregion


        #region Publish(Value)

        private RoamingNetworkCommit Publish(String Value)
        {

            var result = leader!.Commit(leader.Head.RoamingNetwork.CreateSignedChangeSet(
                                            [ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, Value) ],
                                            CreatedAt: T0.AddMinutes((Double) leader.Head.Commit.Revision + 1)));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            return result.Commit!;

        }

        #endregion

        #region MakeCheckpoint()

        private RoamingNetworkCheckpoint MakeCheckpoint()
        {
            Assert.That(leader!.TryMakeCheckpoint(out var checkpoint, out var error), Is.True, error);
            return checkpoint!;
        }

        #endregion

        #region SetPrice(EntityId, Price)

        private void SetPrice(String   EntityId,
                              Decimal  Price)

            => Assert.That(leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus,
                                                           nameof(EVSE),
                                                           EntityId,
                                                           new ChargingPrices([ new ChargingTariffPrices(ChargingTariff_Id.Parse("DE*GEF*T1"),
                                                                                                         [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1) ]) ]) ]).Text,
                                                           Timestamp.Now.AddMinutes(-1),
                                                           out _,
                                                           out var error),
                           Is.EqualTo(POIStatusUpdateResult.Updated),
                           error);

        #endregion

        #region StartPeer(Options)

        private async Task<RoamingNetworkPeer> StartPeer(RoamingNetworkPeerOptions Options)
        {

            peer = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/RNs/rn1/history/ws"), null, Options);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await peer.StartAsync(timeout.Token);

            return peer;

        }

        #endregion

        #region (static) Options(Verify = true, RequiredRoles = null, Join = true)

        private static RoamingNetworkPeerOptions Options(Boolean                       Verify          = true,
                                                         IReadOnlyCollection<String>?  RequiredRoles   = null,
                                                         Boolean                       Join            = true)

            => new () {
                   VerifyCheckpointSignature         = Verify ? History.CheckpointTests.Verify : null,
                   RequiredCheckpointSignatureRoles  = RequiredRoles ?? [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ],
                   JoinFromCheckpoint                = Join,
                   MinReconnectDelay                 = TimeSpan.FromMilliseconds(50),
                   MaxReconnectDelay                 = TimeSpan.FromMilliseconds(200)
               };

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


        #region A_history_starts_from_the_snapshot_of_a_checkpoint()

        [Test]
        public void A_history_starts_from_the_snapshot_of_a_checkpoint()
        {

            var v1          = Publish("v1");
            SetPrice("DE*GEF*E1*1", 0.29m);
            var checkpoint  = MakeCheckpoint();
            Publish("v2");

            Assert.That(leader!.TryGetCheckpointSnapshotJSON(checkpoint.Id, out var snapshot, out var snapshotError),  Is.True, snapshotError);

            var text        = snapshot!.ToString();

            var joined      = RoamingNetworkHistory.TryFromCheckpointSnapshot(text,
                                                                              out var history,
                                                                              out var joinError,
                                                                              History.CheckpointTests.Verify,
                                                                              [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ],
                                                                              leader.PriceStatementsOf(checkpoint.Id));

            // The version of another commit, e.g. of the head, is not the one of the checkpoint.
            leader.TryGetCheckpointSnapshotJSON(checkpoint.Id, out var otherVersion, out _);
            var head        = leader.ToSnapshotJSON(IncludeRuntimeStatus: false);
            otherVersion!["commit"]          = head["commit"];
            otherVersion["roamingNetwork"]   = head["roamingNetwork"];
            otherVersion.Remove("effectiveLastChanges");

            if (head["effectiveLastChanges"] is JToken headFloors)
                otherVersion["effectiveLastChanges"] = headFloors;

            leader.TryGetCheckpointSnapshotJSON(checkpoint.Id, out var withoutCheckpoint, out _);
            withoutCheckpoint!.Remove("checkpoint");

            // A changed version: it no longer leads to its ETags.
            leader.TryGetCheckpointSnapshotJSON(checkpoint.Id, out var tampered, out _);
            tampered!["roamingNetwork"]!["dataSource"] = "v9";

            using (Assert.EnterMultipleScope())
            {

                Assert.That(joined,                                                  Is.True, joinError);
                Assert.That(history!.Root.Id,                                        Is.EqualTo(v1.Id));
                Assert.That(history.Head.Commit.Id,                                  Is.EqualTo(checkpoint.CommitId));
                Assert.That(history.LatestCheckpoint?.Id,                            Is.EqualTo(checkpoint.Id), "Its line of checkpoints begins with it.");
                Assert.That(history.PriceStatementsOf(checkpoint.Id),                Is.Not.Null, "With the price statements it names.");
                Assert.That(history.RuntimeStatus.All.Where(status => status.Kind == POIRuntimeStatusKind.PriceStatus), Is.Empty,
                            "No runtime status: the version is not the head.");

                Assert.That(RoamingNetworkHistory.TryFromCheckpointSnapshot(text, out _, out var noVerifier),                                                       Is.False);
                Assert.That(noVerifier,                                              Does.Contain("no verifier of checkpoint signatures"));

                Assert.That(RoamingNetworkHistory.TryFromCheckpointSnapshot(text, out _, out var noWitness, History.CheckpointTests.Verify,
                                                                            [ RoamingNetworkCommitSignatureRoles.Witness ]),                                         Is.False);
                Assert.That(noWitness,                                               Does.Contain("is not signed in the role 'witness'"));

                Assert.That(RoamingNetworkHistory.TryFromCheckpointSnapshot(otherVersion, out _, out var otherError, History.CheckpointTests.Verify),             Is.False);
                Assert.That(otherError,                                              Does.Contain("is not the version of the commit"));

                Assert.That(RoamingNetworkHistory.TryFromCheckpointSnapshot(tampered, out _, out var tamperedError, History.CheckpointTests.Verify),                  Is.False);
                Assert.That(tamperedError,                                           Does.Contain("do not match its content"));

                Assert.That(RoamingNetworkHistory.TryFromCheckpointSnapshot(withoutCheckpoint, out _, out var missingError, History.CheckpointTests.Verify),       Is.False);
                Assert.That(missingError,                                            Does.Contain("needs its checkpoint"));

            }

        }

        #endregion

        #region A_peer_joins_from_the_latest_checkpoint_and_catches_up()

        [Test]
        public async Task A_peer_joins_from_the_latest_checkpoint_and_catches_up()
        {

            Publish("v1");
            MakeCheckpoint();
            Publish("v2");
            SetPrice("DE*GEF*E1*1", 0.29m);
            var cp1      = MakeCheckpoint();
            Publish("v3");

            leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0, out _, out _);

            var follower = await StartPeer(Options());

            var root     = follower.History!.Root.Id;
            var inSync   = follower.History.Head.Commit.Id == leader.Head.Commit.Id;
            var status   = follower.History.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var operatorStatus) ? operatorStatus.Value : null;

            // Its line of checkpoints goes on.
            Publish("v4");
            var cp2      = MakeCheckpoint();

            var tookNext = await Eventually(() => follower.History?.LatestCheckpoint?.Id == cp2.Id);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(root,                                                        Is.EqualTo(cp1.CommitId), "Joined from the latest checkpoint, not from the head.");
                Assert.That(inSync,                                                      Is.True, "The commits since came with catching up.");
                Assert.That(status,                                                      Is.EqualTo("offline"), "The runtime status came from the head.");
                Assert.That(follower.History.Checkpoints.Select(checkpoint => checkpoint.Id), Does.Contain(cp1.Id));
                Assert.That(follower.History.PriceStatementsOf(cp1.Id),                  Is.Not.Null);
                Assert.That(tookNext,                                                    Is.True, "The peer did not take the next checkpoint.");

            }

        }

        #endregion

        #region A_peer_joins_from_the_latest_checkpoint_carrying_the_signatures_it_requires()

        [Test]
        public async Task A_peer_joins_from_the_latest_checkpoint_carrying_the_signatures_it_requires()
        {

            Publish("v1");
            var witnessed  = MakeCheckpoint();

            Assert.That(leader!.TryWitness(witnessed.Id, History.CheckpointTests.WitnessSigner, out _, out _, out var witnessError),  Is.True, witnessError);

            Publish("v2");
            var unwitnessed = MakeCheckpoint();
            Publish("v3");

            var follower   = await StartPeer(Options(RequiredRoles: [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                                      RoamingNetworkCommitSignatureRoles.Witness ]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(follower.History!.Root.Id,                   Is.EqualTo(witnessed.CommitId), "Not from the latest checkpoint, which no witness signed yet.");
                Assert.That(follower.History.Head.Commit.Id,             Is.EqualTo(leader.Head.Commit.Id));
                Assert.That(follower.History.LatestCheckpoint?.Id,       Is.EqualTo(witnessed.Id));
                Assert.That(unwitnessed.Signatures,                      Has.Length.EqualTo(1));
            }

        }

        #endregion

        #region Without_a_verifier_or_a_checkpoint_a_peer_starts_from_the_head()

        [TestCase(false, true,  true,  Description = "Without a verifier of checkpoint signatures")]
        [TestCase(true,  false, true,  Description = "Not joining from checkpoints")]
        [TestCase(true,  true,  false, Description = "Without a checkpoint")]
        public async Task Without_a_verifier_or_a_checkpoint_a_peer_starts_from_the_head(Boolean  Verify,
                                                                                         Boolean  Join,
                                                                                         Boolean  WithCheckpoint)
        {

            Publish("v1");

            if (WithCheckpoint)
                MakeCheckpoint();

            var head      = Publish("v2");

            var follower  = await StartPeer(Options(Verify: Verify, Join: Join));

            Assert.That(follower.History!.Root.Id, Is.EqualTo(head.Id));

        }

        #endregion

    }

}
