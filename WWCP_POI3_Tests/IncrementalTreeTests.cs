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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Proofs
{

    /// <summary>
    /// The tree of a version from the digests its entities keep (SPECIFICATION.md, section 8.7): the same as the tree of
    /// its effective content, after every kind of change, and without hashing again what the version before had, also
    /// within a changed operator; and the runtime status of what a version keeps, still told of.
    /// </summary>
    [TestFixture]
    public class IncrementalTreeTests
    {

        #region Data

        private static readonly DateTimeOffset              T0   = DateTimeOffset.Parse("2026-10-20T12:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF  = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  XYZ  = ChargingStationOperator_Id.Parse("DE*XYZ");
        private static readonly ChargingStation_Id          S1   = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E11  = EVSE_Id.                   Parse("DE*GEF*E1*1");

        #endregion


        #region (static) TreeOfTheEffectiveContent(Version, HashAlgorithm)

        /// <summary>
        /// The effectiveTree ETag of the given version as section 8.2 calculates it: over its effective content as one JSON
        /// document, its child arrays replaced by their roots.
        /// </summary>
        private static ETag TreeOfTheEffectiveContent(RoamingNetwork     Version,
                                                      ETagHashAlgorithm  HashAlgorithm)
        {

            Assert.That(POIEffectiveTree.TryDigest(Version.ToEffectiveContentJSON(),
                                                   POIEffectiveTree.RoamingNetworkKind,
                                                   HashAlgorithm,
                                                   out var digest,
                                                   out var error),
                        Is.True,
                        error);

            return new ETag(ETagFormat.EffectiveTree, HashAlgorithm, digest);

        }

        #endregion

        #region (static) Commit(History, Changes)

        private static void Commit(RoamingNetworkHistory                    History,
                                   Func<RoamingNetwork, RoamingNetworkChange>  Change)
        {

            var head    = History.Head.RoamingNetwork;
            var result  = History.Commit(head.CreateSignedChangeSet([ Change(head) ], CreatedAt: T0.AddMinutes(History.Count)));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

        }

        #endregion


        #region The_tree_of_every_version_is_the_one_of_its_effective_content()

        /// <summary>
        /// Changes of the roaming network, of a charging station operator, of an EVSE, of a template its members take, of
        /// charging pools added and removed, of a grid operator, of a parking operator and of the template of a parking
        /// space: after each, the tree of the head is the one of its effective content, of both its hash algorithms, and the
        /// proof of a charging station leads to it.
        /// </summary>
        [Test]
        public void The_tree_of_every_version_is_the_one_of_its_effective_content()
        {

            var history = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());

            var changes = new (String Description, Func<RoamingNetwork, RoamingNetworkChange> Change)[] {

                ("a second tree hash algorithm",    head => RoamingNetworkChange.UpdateProperty(head, POITreeHashAlgorithms.JSONProperty, null, new JArray("sha256", "sha512"))),
                ("the roaming network",             head => RoamingNetworkChange.UpdateProperty(head, "dataSource", null, "test 2")),
                ("an EVSE",                         head => RoamingNetworkChange.UpdateProperty(nameof(EVSE),         "DE*GEF*E1*2", "maxPower", null, 11000)),
                ("the template of an EVSE",         head => RoamingNetworkChange.UpdateProperty(nameof(EVSETemplate), "DE*GEF*ET1",  "maxPower", 22000, 7400)),
                ("a charging pool added",           head => RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P3"),
                                                                                                      Created:           T0,
                                                                                                      ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S4"),
                                                                                                                                               Created:  T0,
                                                                                                                                               EVSEs:    [ new EVSE(EVSE_Id.Parse("DE*GEF*E4*1"), Created: T0) ]) ]))),
                ("a charging pool removed",         head => RoamingNetworkChange.Remove(ChargingPool_Id.Parse("DE*GEF*P2"))),
                ("another operator",                head => RoamingNetworkChange.UpdateProperty(head.GetChargingStationOperatorById(XYZ)!,  "dataSource", null, "xyz")),
                ("a grid operator",                 head => RoamingNetworkChange.UpdateProperty(head.GridOperators[0],                      "dataSource", null, "grid")),
                ("a parking operator",              head => RoamingNetworkChange.UpdateProperty(head.ParkingOperators[0],                   "dataSource", null, "parking")),
                ("the template of a parking space", head => RoamingNetworkChange.UpdateProperty(nameof(ParkingSpaceTemplate), "level-1", "roofed", true, false))

            };

            void Check(String Description)
            {

                var head = history.Head.RoamingNetwork;

                foreach (var hashAlgorithm in head.TreeHashAlgorithms)
                {

                    Assert.That(head.EffectiveTreeETag(hashAlgorithm),                        Is.EqualTo(TreeOfTheEffectiveContent(head, hashAlgorithm)),  $"{Description}: {hashAlgorithm}");
                    Assert.That(history.TryGetChargingStationProof(S1, out var proof, out var error, hashAlgorithm),  Is.True,                     $"{Description}: {error}");
                    Assert.That(proof!.Verify(head.EffectiveTreeETag(hashAlgorithm), out error),                     Is.True,                     $"{Description}: {error}");

                }

            }

            Check("the genesis");

            foreach (var (description, change) in changes)
            {
                Commit(history, change);
                Check(description);
            }

        }

        #endregion

        #region What_a_version_did_not_change_is_not_hashed_again()

        /// <summary>
        /// The nodes of the parties a commit does not change are those of the version before: they are not calculated
        /// again, nor the digests of their children.
        /// </summary>
        [Test]
        public void What_a_version_did_not_change_is_not_hashed_again()
        {

            var history  = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());
            var before   = history.Head.RoamingNetwork.EffectiveTree(ETagHashAlgorithm.SHA256).Root;

            Commit(history, head => RoamingNetworkChange.UpdateProperty(head.GetChargingStationOperatorById(XYZ)!, "dataSource", null, "xyz"));

            var after    = history.Head.RoamingNetwork.EffectiveTree(ETagHashAlgorithm.SHA256).Root;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(after.Digest,                                                  Is.Not.EqualTo(before.Digest));

                // The operator DE*GEF, with its pools, tariffs and templates, and the grid and parking operators.
                Assert.That(after.Arrays["chargingStationOperators"].Children![0],         Is.SameAs(before.Arrays["chargingStationOperators"].Children![0]));
                Assert.That(after.Arrays["gridOperators"].           Children![0],         Is.SameAs(before.Arrays["gridOperators"].           Children![0]));
                Assert.That(after.Arrays["parkingOperators"].        Children![0],         Is.SameAs(before.Arrays["parkingOperators"].        Children![0]));

                // The operator DE*XYZ changed.
                Assert.That(after.Arrays["chargingStationOperators"].Children![1],         Is.Not.SameAs(before.Arrays["chargingStationOperators"].Children![1]));
                Assert.That(after.Arrays["chargingStationOperators"].LeafHashes[0],        Is.SameAs(before.Arrays["chargingStationOperators"].LeafHashes[0]));

            }

        }

        #endregion

        #region A_change_within_an_operator_keeps_its_other_charging_pools()

        /// <summary>
        /// A change of a charging station reads again only its charging pool: the other charging pools of its operator are
        /// those of the version before, with their charging stations, EVSEs and effective members, and their digests. A
        /// change of a template reads again none: only the effective members taking it are calculated again.
        /// </summary>
        [Test]
        public void A_change_within_an_operator_keeps_its_other_charging_pools()
        {

            var history  = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());

            ChargingStationOperator GEFOf()
                => history.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!;

            ChargingPool PoolOf(ChargingStationOperator Operator, String Id)
                => Operator.TryGetChargingPool(ChargingPool_Id.Parse(Id), out var chargingPool) ? chargingPool : throw new AssertionException($"No charging pool '{Id}'!");

            EVSE EffectiveE11Of(ChargingStationOperator Operator)
                => Operator.TryGetEffectiveEVSE(E11, out var evse) ? evse : throw new AssertionException("No EVSE 'DE*GEF*E1*1'!");

            IReadOnlyList<Byte[]> PoolDigests()
                => history.Head.RoamingNetwork.EffectiveTree(ETagHashAlgorithm.SHA256).Root.Arrays["chargingStationOperators"].Children![0].Arrays["chargingPools"].LeafHashes;

            var before         = GEFOf();
            var digestsBefore  = PoolDigests();

            // A charging station of the charging pool P2.
            Commit(history, head => RoamingNetworkChange.UpdateProperty(nameof(ChargingStation), "DE*GEF*S3", "dataSource", null, "s3"));

            var after          = GEFOf();
            var digestsAfter   = PoolDigests();

            // The template of the EVSE DE*GEF*E1*1 of the charging pool P1.
            Commit(history, head => RoamingNetworkChange.UpdateProperty(nameof(EVSETemplate), "DE*GEF*ET1", "maxPower", 22000, 7400));

            var templated      = GEFOf();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(after,                                 Is.Not.SameAs(before));
                Assert.That(PoolOf(after, "DE*GEF*P1"),            Is.SameAs(PoolOf(before, "DE*GEF*P1")));
                Assert.That(PoolOf(after, "DE*GEF*P2"),            Is.Not.SameAs(PoolOf(before, "DE*GEF*P2")));
                Assert.That(PoolOf(after, "DE*GEF*P2").ChargingStations[0].DataSource,  Is.EqualTo("s3"));
                Assert.That(EffectiveE11Of(after),                 Is.SameAs(EffectiveE11Of(before)));
                Assert.That(digestsAfter[0],                       Is.SameAs(digestsBefore[0]));
                Assert.That(digestsAfter[1],                       Is.Not.EqualTo(digestsBefore[1]));

                Assert.That(PoolOf(templated, "DE*GEF*P1"),        Is.SameAs(PoolOf(before, "DE*GEF*P1")));
                Assert.That(EffectiveE11Of(templated),             Is.Not.SameAs(EffectiveE11Of(before)));
                Assert.That(EffectiveE11Of(templated).MaxPower,    Is.EqualTo(Watt.FromW(7400)));
                Assert.That(PoolDigests()[0],                      Is.Not.EqualTo(digestsBefore[0]));

                foreach (var hashAlgorithm in history.Head.RoamingNetwork.TreeHashAlgorithms)
                    Assert.That(history.Head.RoamingNetwork.EffectiveTreeETag(hashAlgorithm),  Is.EqualTo(TreeOfTheEffectiveContent(history.Head.RoamingNetwork, hashAlgorithm)));

                // As read anew from its JSON.
                Assert.That(RoamingNetwork.Parse(history.Head.RoamingNetwork.ToJSON(IncludeETags: false)).EffectiveTreeETag(ETagHashAlgorithm.SHA256),
                            Is.EqualTo(history.Head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256)));

            }

        }

        #endregion

        #region The_status_of_a_kept_charging_pool_is_told_once_after_a_commit()

        /// <summary>
        /// After a change of a charging station of the charging pool P2, the status of an EVSE of the charging pool P1 kept
        /// as it was, and the admin status of the charging station read again, are told of once each.
        /// </summary>
        [Test]
        public void The_status_of_a_kept_charging_pool_is_told_once_after_a_commit()
        {

            var history  = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());
            var updates  = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            Commit(history, head => RoamingNetworkChange.UpdateProperty(nameof(ChargingStation), "DE*GEF*S3", "dataSource", null, "s3"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(EVSE),            "DE*GEF*E1*2", "charging",     T0.AddHours(1), out _, out var evseError),     Is.EqualTo(POIStatusUpdateResult.Updated), evseError);
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(ChargingStation), "DE*GEF*S3",   "OutOfService", T0.AddHours(1), out _, out var stationError),  Is.EqualTo(POIStatusUpdateResult.Updated), stationError);

                Assert.That(updates.Select(update => $"{update.EntityId} {update.Value}"),  Is.EqualTo(new[] { "DE*GEF*E1*2 charging", "DE*GEF*S3 OutOfService" }));

            }

        }

        #endregion

        #region A_planned_status_is_due_after_a_commit_removing_the_one_before_it()

        /// <summary>
        /// A commit looks only at what its version has new for a planned status: one of an entity it no longer has lets the
        /// timer look again, in vain, and a later one of a charging pool kept as it was is still told of when it is due.
        /// </summary>
        [Test]
        public async Task A_planned_status_is_due_after_a_commit_removing_the_one_before_it()
        {

            var history  = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());
            var told     = new TaskCompletionSource<POIStatusUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);

            history.RuntimeStatus.OnStatusUpdate += statusUpdate => {
                if (statusUpdate.EntityId == "DE*GEF*E2*1")
                    told.TrySetResult(statusUpdate);
            };

            // A whole millisecond, as a status is planned at: otherwise it is due up to a millisecond before now + 900 ms.
            var now      = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.Now.ToUnixTimeMilliseconds());
            var first    = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(ChargingStation), "DE*GEF*S3",   "OutOfService", now.AddMilliseconds(300), out _, out _);
            var second   = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(EVSE),            "DE*GEF*E2*1", "charging",     now.AddMilliseconds(900), out _, out _);

            // The charging pool P2, with the charging station DE*GEF*S3.
            Commit(history, head => RoamingNetworkChange.Remove(ChargingPool_Id.Parse("DE*GEF*P2")));

            var update   = await told.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first,         Is.EqualTo(POIStatusUpdateResult.Scheduled));
                Assert.That(second,        Is.EqualTo(POIStatusUpdateResult.Scheduled));
                Assert.That(update.Value,  Is.EqualTo("charging"));
                Assert.That(Timestamp.Now, Is.GreaterThanOrEqualTo(now.AddMilliseconds(900)));
            }

        }

        #endregion

    }

}
