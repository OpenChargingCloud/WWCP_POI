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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Grid
{

    /// <summary>
    /// Grid connection points embedded in their grid operators, each maintained by one party:
    /// its grid operator, or a charging station operator while the grid operator does not take part.
    /// </summary>
    [TestFixture]
    public class GridConnectionPointTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-14T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  EXA      = ChargingStationOperator_Id.Parse("DE*EXA");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly GridOperator_Id             NET      = GridOperator_Id.           Parse("DE*NET");   // takes part
        private static readonly GridOperator_Id             NE2      = GridOperator_Id.           Parse("DE*NE2");   // does not take part
        private static readonly GridConnectionPoint_Id      GCP1     = GridConnectionPoint_Id.    Parse("connection-1");
        private static readonly GridConnectionPoint_Id      GCP2     = GridConnectionPoint_Id.    Parse("connection-2");

        private static readonly POIPartyId                  RNO      = TestParties.Party(RoamingNetwork_Id.Parse("rn1"));

        #endregion

        #region NewNetwork()

        /// <summary>
        /// DE*NET takes part, but its connection-1 is still maintained by DE*GEF;
        /// DE*NE2 has no keys, and DE*GEF maintains its connection-2, the one of its charging pool.
        /// </summary>
        private static RoamingNetwork NewNetwork()
        {

            var json = new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                          Created:                   Created,
                                          GridOperators:             [ new GridOperator(NET,
                                                                                        GridConnectionPoints:  [ new GridConnectionPoint(GCP1, MaintainedBy: TestParties.Party(GEF)) ],
                                                                                        Created:               Created),
                                                                       new GridOperator(NE2,
                                                                                        GridConnectionPoints:  [ new GridConnectionPoint(GCP2,
                                                                                                                                         ContractedImportPower:  Watt.FromW(150000),
                                                                                                                                         MaintainedBy:           TestParties.Party(GEF)) ],
                                                                                        Created:               Created) ],
                                          ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                                   Created:        Created,
                                                                                                   ChargingPools:  [ new ChargingPool(P1,
                                                                                                                                      Created:                Created,
                                                                                                                                      GridConnectionPointId:  GCP2) ]),
                                                                       new ChargingStationOperator(EXA, Created: Created) ]).WithKeys().ToJSON();

            // DE*NE2 does not take part: it has no keys.
            var ne2 = (JObject) json["gridOperators"]!.Single(gridOperator => gridOperator["@id"]!.Value<String>() == "DE*NE2");
            ne2.Remove(POIPartyKeys.SigningKeysProperty);
            ne2.Remove(POIPartyKeys.IdentityKeysProperty);

            return RoamingNetwork.Parse(json);

        }

        #endregion

        #region Apply(Network, Signers, Changes)

        /// <summary>
        /// Apply the given changes, signed only by the given parties with their signing keys.
        /// </summary>
        private static (RoamingNetwork? Network, String? Error) Apply(RoamingNetwork                 Network,
                                                                      POIPartyId[]                   Signers,
                                                                      params RoamingNetworkChange[]  Changes)
        {

            RoamingNetworkChangeSet changeSet;

            // Creating a change set applies it already, to know its after ETags.
            try
            {
                changeSet = Network.CreateChangeSet(Changes, CreatedAt: T0);
            }
            catch (ArgumentException e)
            {
                return (null, e.Message);
            }

            foreach (var signer in Signers)
                changeSet = changeSet.SignedBy(signer);

            return Network.TryApplyChangeSet(changeSet, out var applied, out var error)
                       ? (applied, null)
                       : (null,    error);

        }

        #endregion

        #region (static) Point(Network, GridConnectionPointId)

        private static GridConnectionPoint Point(RoamingNetwork          Network,
                                                 GridConnectionPoint_Id  GridConnectionPointId)
        {
            Assert.That(Network.TryGetGridConnectionPoint(GridConnectionPointId, out _, out var gridConnectionPoint), Is.True);
            return gridConnectionPoint!;
        }

        #endregion

        #region (static) Maintainer(Party)

        private static JObject Maintainer(POIPartyId Party)
            => Party.ToJSON();

        #endregion


        #region Grid_connection_points_are_embedded_in_their_grid_operators()

        [Test]
        public void Grid_connection_points_are_embedded_in_their_grid_operators()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var error),  Is.True, error);

            var ne2   = json["gridOperators"]!.Single(gridOperator => gridOperator["@id"]!.Value<String>() == "DE*NE2");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromCBOR!.CalcETags(),                                                       Is.EqualTo(rn1.CalcETags()));
                Assert.That(ne2[POIPartyKeys.SigningKeysProperty],                                       Is.Null);
                Assert.That(ne2["gridConnectionPoints"]![0]!["maintainedBy"]!.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("""{"entityType":"ChargingStationOperator","entityId":"DE*GEF"}"""));
                Assert.That(json["chargingStationOperators"]!.Single(chargingStationOperator => chargingStationOperator["@id"]!.Value<String>() == "DE*GEF")["chargingPools"]![0]!["gridConnectionPointId"]!.Value<String>(),
                            Is.EqualTo("connection-2"));

                Assert.That(rn1.TryGetGridConnectionPoint(GridConnectionPoint_Id.Parse("CONNECTION-2"), out var gridOperator, out var point),  Is.True);
                Assert.That(gridOperator!.Id,                                                            Is.EqualTo(NE2));
                Assert.That(point!.MaintainedBy,                                                         Is.EqualTo(TestParties.Party(GEF)));
                Assert.That(rn1.GridConnectionPoints.Select(gridConnectionPoint => gridConnectionPoint.Id.ToString()).Order(),
                            Is.EqualTo(new[] { "connection-1", "connection-2" }));

            }

        }

        #endregion

        #region A_charging_station_operator_maintains_the_grid_connection_points_of_a_grid_operator_not_taking_part()

        [Test]
        public void A_charging_station_operator_maintains_the_grid_connection_points_of_a_grid_operator_not_taking_part()
        {

            var rn1        = NewNetwork();
            var gef        = TestParties.Party(GEF);
            var exa        = TestParties.Party(EXA);
            var gcp2       = Point(rn1, GCP2);
            var gcp3       = new GridConnectionPoint(GridConnectionPoint_Id.Parse("connection-3"), MaintainedBy: gef);
            var byOther    = new GridConnectionPoint(GridConnectionPoint_Id.Parse("connection-4"), MaintainedBy: gef);

            var added      = Apply(rn1, [ gef ], RoamingNetworkChange.Add(gcp3,    NE2));
            var addedByEXA = Apply(rn1, [ exa ], RoamingNetworkChange.Add(byOther, NE2));
            var changed    = Apply(rn1, [ gef ], RoamingNetworkChange.UpdateProperty(gcp2, "contractedImportPower", null, 200000));
            var byEXA      = Apply(rn1, [ exa ], RoamingNetworkChange.UpdateProperty(gcp2, "contractedImportPower", null, 200000));
            var byRNO      = Apply(rn1, [ RNO ], RoamingNetworkChange.UpdateProperty(gcp2, "contractedImportPower", null, 200000));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(added.Error,                                                  Is.Null);
                Assert.That(Point(added.Network!, gcp3.Id).MaintainedBy,                  Is.EqualTo(gef));

                Assert.That(addedByEXA.Error,                                             Does.Contain("needs a signature by"));

                Assert.That(changed.Error,                                                Is.Null);
                Assert.That(Point(changed.Network!, GCP2).ContractedImportPower,          Is.EqualTo(Watt.FromW(200000)));

                // Not by another operator, and not by the roaming network operator: the maintainer vouches for the data.
                Assert.That(byEXA.Error,                                                  Does.Contain("signing key of the ChargingStationOperator 'DE*GEF'"));
                Assert.That(byRNO.Error,                                                  Does.Contain("signing key of the ChargingStationOperator 'DE*GEF'"));

            }

        }

        #endregion

        #region A_grid_operator_takes_its_grid_connection_points_over()

        [Test]
        public void A_grid_operator_takes_its_grid_connection_points_over()
        {

            var rn1         = NewNetwork();
            var gef         = TestParties.Party(GEF);
            var exa         = TestParties.Party(EXA);
            var net         = TestParties.Party(NET);

            // While DE*GEF maintains it, DE*NET does not change its data...
            var notYet      = Apply(rn1, [ net ], RoamingNetworkChange.UpdateProperty(Point(rn1, GCP1), "nominalVoltage", null, 400));

            // ...but takes it over, without the consent of DE*GEF.
            var takenOver   = Apply(rn1, [ net ], RoamingNetworkChange.RemoveProperty(Point(rn1, GCP1), RoamingNetwork.MaintainedByProperty));
            var rn2         = takenOver.Network!;

            var byNET       = Apply(rn2, [ net ], RoamingNetworkChange.UpdateProperty(Point(rn2, GCP1), "nominalVoltage", null, 400));
            var byGEF       = Apply(rn2, [ gef ], RoamingNetworkChange.UpdateProperty(Point(rn2, GCP1), "nominalVoltage", null, 400));
            var takenBack   = Apply(rn2, [ gef ], RoamingNetworkChange.UpdateProperty(Point(rn2, GCP1), RoamingNetwork.MaintainedByProperty, null, Maintainer(gef)));
            var handedOn    = Apply(rn2, [ net ], RoamingNetworkChange.UpdateProperty(Point(rn2, GCP1), RoamingNetwork.MaintainedByProperty, null, Maintainer(exa)));
            var reassigned  = Apply(rn2, [ ],     RoamingNetworkChange.UpdateProperty(Point(rn2, GCP1), RoamingNetwork.MaintainedByProperty, null, Maintainer(gef)));

            // Handed on in a change set, a later change of the same change set is the new maintainer's (csharp-review 9).
            RoamingNetworkChange[] handOnAndChange = [ RoamingNetworkChange.UpdateProperty(Point(rn2, GCP1), RoamingNetwork.MaintainedByProperty, null, Maintainer(exa)),
                                                       RoamingNetworkChange.UpdateProperty(Point(rn2, GCP1), "nominalVoltage", null, 400) ];

            var notByNET    = Apply(rn2, [ net ],      handOnAndChange);
            var withEXA     = Apply(rn2, [ net, exa ], handOnAndChange);

            var recovery    = rn2.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(Point(rn2, GCP1), RoamingNetwork.MaintainedByProperty, null, Maintainer(gef)) ], CreatedAt: T0).
                                  SignedBy(RNO, POIKeyKind.Identity);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(notYet.Error,                                                 Does.Contain("signing key of the ChargingStationOperator 'DE*GEF'"));
                Assert.That(takenOver.Error,                                              Is.Null);
                Assert.That(Point(rn2, GCP1).MaintainedBy,                                Is.Null);

                Assert.That(byNET.Error,                                                  Is.Null);
                Assert.That(byGEF.Error,                                                  Does.Contain("signing key of the GridOperator 'DE*NET'"));

                // The former maintainer does not take it back; the grid operator hands it on.
                Assert.That(takenBack.Error,                                              Does.Contain("needs a signature by"));
                Assert.That(handedOn.Error,                                               Is.Null);
                Assert.That(Point(handedOn.Network!, GCP1).MaintainedBy,                  Is.EqualTo(exa));

                Assert.That(notByNET.Error,                                               Does.Contain("Change 1: UpdateProperty of the GridConnectionPoint 'connection-1' ('nominalVoltage') needs a signature by a signing key of the ChargingStationOperator 'DE*EXA'"));
                Assert.That(withEXA.Error,                                                Is.Null);

                Assert.That(reassigned.Error,                                             Does.Contain("unsigned"));
                Assert.That(rn2.TryApplyChangeSet(recovery, out var recovered, out var recoveryError),  Is.True, recoveryError);
                Assert.That(Point(recovered!, GCP1).MaintainedBy,                         Is.EqualTo(gef));

            }

        }

        #endregion

        #region The_roaming_network_operator_signs_for_a_grid_operator_without_keys()

        [Test]
        public void The_roaming_network_operator_signs_for_a_grid_operator_without_keys()
        {

            var rn1        = NewNetwork();
            var gef        = TestParties.Party(GEF);
            var ne2        = TestParties.Party(NE2);
            var ne2Entity  = rn1.GridOperators.Single(gridOperator => gridOperator.Id == NE2);

            var addedGO    = Apply(rn1, [ RNO ], RoamingNetworkChange.Add(new GridOperator(GridOperator_Id.Parse("DE*NE3"))));
            var addedCSO   = Apply(rn1, [ RNO ], RoamingNetworkChange.Add(new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*ZAP"))));
            var homepage   = Apply(rn1, [ RNO ], RoamingNetworkChange.UpdateProperty(ne2Entity, "homepage", null, "https://ne2.example"));
            var byGEF      = Apply(rn1, [ gef ], RoamingNetworkChange.UpdateProperty(ne2Entity, "homepage", null, "https://ne2.example"));

            // A grid connection point maintained by a grid operator without keys: the roaming network operator signs.
            var ownPoint   = Apply(rn1, [ RNO ], RoamingNetworkChange.Add(new GridConnectionPoint(GridConnectionPoint_Id.Parse("connection-5")), NE2));

            // The roaming network operator gives it keys with its identity key; then it signs itself.
            var keyed      = rn1.CreateChangeSet([
                                 RoamingNetworkChange.UpdateProperty(ne2Entity, POIPartyKeys.SigningKeysProperty,  null, new JArray(TestParties.Key(ne2, POIKeyKind.Signing). Public.ToJSON())),
                                 RoamingNetworkChange.UpdateProperty(ne2Entity, POIPartyKeys.IdentityKeysProperty, null, new JArray(TestParties.Key(ne2, POIKeyKind.Identity).Public.ToJSON()))
                             ], CreatedAt: T0).SignedBy(RNO, POIKeyKind.Identity).ProvenByNewIdentityKeys();

            Assert.That(rn1.TryApplyChangeSet(keyed, out var rn2, out var keyedError),  Is.True, keyedError);

            var ne2Keyed   = rn2!.GridOperators.Single(gridOperator => gridOperator.Id == NE2);
            var itself     = Apply(rn2, [ ne2 ], RoamingNetworkChange.UpdateProperty(ne2Keyed, "homepage", null, "https://ne2.example"));
            var notRNO     = Apply(rn2, [ RNO ], RoamingNetworkChange.UpdateProperty(ne2Keyed, "homepage", null, "https://ne2.example"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(addedGO.Error,                                                Is.Null);
                Assert.That(addedGO.Network!.GridOperators.Single(gridOperator => gridOperator.Id.ToString() == "DE*NE3").SigningKeys,  Is.Empty);
                Assert.That(addedCSO.Error,                                               Does.Contain("must be added with its signingKeys"));

                Assert.That(homepage.Error,                                               Is.Null);
                Assert.That(byGEF.Error,                                                  Does.Contain("roaming network operator"));
                Assert.That(ownPoint.Error,                                               Is.Null);

                Assert.That(itself.Error,                                                 Is.Null);
                Assert.That(notRNO.Error,                                                 Does.Contain("signing key of the GridOperator 'DE*NE2'"));

            }

        }

        #endregion

        #region Grid_connection_points_are_unique_and_named()

        [Test]
        public void Grid_connection_points_are_unique_and_named()
        {

            var rn1        = NewNetwork();
            var gef        = TestParties.Party(GEF);
            var pool       = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();

            var twice      = Apply(rn1, [ gef ], RoamingNetworkChange.Add(new GridConnectionPoint(GridConnectionPoint_Id.Parse("CONNECTION-1"), MaintainedBy: gef), NE2));
            var noParent   = Apply(rn1, [ gef ], RoamingNetworkChange.Add(nameof(GridConnectionPoint), "connection-6", new GridConnectionPoint(GridConnectionPoint_Id.Parse("connection-6"), MaintainedBy: gef).ToJSON()));
            var stillUsed  = Apply(rn1, [ gef ], RoamingNetworkChange.Remove(GCP2));
            var unknown    = Apply(rn1, [ gef ], RoamingNetworkChange.UpdateProperty(pool, "gridConnectionPointId", null, "connection-9"));
            var moved      = Apply(rn1, [ gef ], RoamingNetworkChange.UpdateProperty(pool, "gridConnectionPointId", null, "connection-1"));
            var unknownBy  = Apply(rn1, [ gef ], RoamingNetworkChange.UpdateProperty(Point(rn1, GCP2), RoamingNetwork.MaintainedByProperty, null,
                                                                                     Maintainer(TestParties.Party(ChargingStationOperator_Id.Parse("DE*XYZ")))));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(twice.Error,                                                  Does.Contain("already exists, at the grid operator 'DE*NET'"));
                Assert.That(noParent.Error,                                               Does.Contain("needs its GridOperator"));
                Assert.That(stillUsed.Error,                                              Does.Contain("grid connection point 'connection-2' of the charging pool 'DE*GEF*P1' is unknown"));
                Assert.That(unknown.Error,                                                Does.Contain("grid connection point 'connection-9' of the charging pool 'DE*GEF*P1' is unknown"));
                Assert.That(moved.Error,                                                  Is.Null);
                Assert.That(unknownBy.Error,                                              Does.Contain("is unknown in the roaming network"));

            }

        }

        #endregion

        #region Grid_connection_points_merge_property_by_property()

        [Test]
        public void Grid_connection_points_merge_property_by_property()
        {

            var rn1     = NewNetwork();
            var gef     = TestParties.Party(GEF);

            var left    = Apply(rn1, [ gef ], RoamingNetworkChange.UpdateProperty(Point(rn1, GCP2), "nominalVoltage",        null, 400)).Network!;
            var right   = Apply(rn1, [ gef ], RoamingNetworkChange.UpdateProperty(Point(rn1, GCP2), "contractedImportPower", null, 250000)).Network!;
            var other   = Apply(rn1, [ gef ], RoamingNetworkChange.UpdateProperty(Point(rn1, GCP2), "nominalVoltage",        null, 230)).Network!;

            var merged  = RoamingNetwork.TryMerge(rn1, left, right, ChangeSet_Id.Parse("merge-1"), out var merge, out _, out var conflicts1);
            var refused = RoamingNetwork.TryMerge(rn1, left, other, ChangeSet_Id.Parse("merge-2"), out _,         out _, out var conflicts2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(merged,                                                       Is.True, String.Join("; ", conflicts1.Select(conflict => conflict.Description)));
                Assert.That(Point(merge!, GCP2).NominalVoltage,                           Is.EqualTo(Volt.FromV(400)));
                Assert.That(Point(merge!, GCP2).ContractedImportPower,                    Is.EqualTo(Watt.FromW(250000)));

                Assert.That(refused,                                                      Is.False);
                Assert.That(conflicts2.Single().EntityType,                               Is.EqualTo(nameof(GridConnectionPoint)));
                Assert.That(conflicts2.Single().PropertyName,                             Is.EqualTo("nominalVoltage"));

            }

        }

        #endregion

    }

}
