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
    /// its effective content, after every kind of change, and without hashing again what the version before had.
    /// </summary>
    [TestFixture]
    public class IncrementalTreeTests
    {

        #region Data

        private static readonly DateTimeOffset              T0   = DateTimeOffset.Parse("2026-10-20T12:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF  = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  XYZ  = ChargingStationOperator_Id.Parse("DE*XYZ");
        private static readonly ChargingStation_Id          S1   = ChargingStation_Id.        Parse("DE*GEF*S1");

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

    }

}
