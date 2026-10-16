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

using System.Collections.Immutable;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Element changes address one element of a property of an entity: a member of a group, a connector of an EVSE,
    /// a property of a connector. Concurrent changes of other elements of the same property do not conflict.
    /// </summary>
    [TestFixture]
    public class ElementChangeTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-16T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  EXA      = ChargingStationOperator_Id.Parse("DE*EXA");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly EVSE_Id                     E3       = EVSE_Id.                   Parse("DE*GEF*E1*3");
        private static readonly EVSEGroup_Id                EG1      = EVSEGroup_Id.              Parse("DE*GEF*EG1");
        private static readonly ChargingTariff_Id           T1       = ChargingTariff_Id.         Parse("DE*GEF*T1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                  Created:                   Created,
                                  ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                           Created:          Created,
                                                                                           ChargingTariffs:  [ new ChargingTariff(T1,
                                                                                                                                  Currency.EUR,
                                                                                                                                  [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                                  Created: Created) ],
                                                                                           ChargingPools:    [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                                Created:           Created,
                                                                                                                                ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                         Created:  Created,
                                                                                                                                                                         EVSEs:    [ new EVSE(E1, Created: Created,
                                                                                                                                                                                          ChargingConnectors: [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2),
                                                                                                                                                                                                                new ChargingConnector(ChargingConnector_Id.Parse("2"), ChargingConnectorType.IEC_62196_T2_COMBO) ]),
                                                                                                                                                                                     new EVSE(E2, Created: Created),
                                                                                                                                                                                     new EVSE(E3, Created: Created) ]) ]) ],
                                                                                           EVSEGroups:       [ new EVSEGroup(EG1, EVSEIds: [ E1 ], Created: Created) ]),
                                                               new ChargingStationOperator(EXA, Created: Created) ]).WithKeys();

        #endregion

        #region Path(PropertyName, ElementId = null, ...)

        private static ImmutableArray<POIElementPathSegment> Path(String   PropertyName,
                                                                  String?  ElementId = null)

            => [ new POIElementPathSegment(PropertyName, ElementId) ];

        #endregion

        #region Members(Network) / Connector(Network, Id)

        private static String[] Members(RoamingNetwork Network)

            => [ .. Network.GetChargingStationOperatorById(GEF)!.EVSEGroups.Single().EVSEIds.Select(id => id.ToString()) ];

        private static JObject Connector(RoamingNetwork Network, String Id)

            => (JObject) ((JArray) Network.GetChargingStationOperatorById(GEF)!.ChargingPools.Single().EVSEs.First(evse => evse.Id == E1).ToJSON()["chargingConnectors"]!).
                             Single(connector => connector["@id"]!.Value<String>() == Id);

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork Network, params RoamingNetworkChange[] Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Members_of_a_group_are_added_and_removed_one_by_one()

        [Test]
        public void Members_of_a_group_are_added_and_removed_one_by_one()
        {

            var rn1     = NewNetwork();

            var add     = rn1.CreateSignedChangeSet([ RoamingNetworkChange.AddElement(nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E3.ToString()), E3.ToString()),
                                                      RoamingNetworkChange.AddElement(nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E2.ToString()), E2.ToString()) ], CreatedAt: T0);
            var rn2     = rn1.ApplyChangeSet(add);

            var remove  = rn2.CreateSignedChangeSet([ RoamingNetworkChange.RemoveElement(nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E1.ToString())) ], CreatedAt: T0);
            var rn3     = rn2.ApplyChangeSet(remove);

            // Read again from JSON, as a peer does.
            Assert.That(RoamingNetworkChangeSet.TryParse(remove.ToJSON(), out var parsed, out var parseError),  Is.True, parseError);

            using (Assert.EnterMultipleScope())
            {

                // In the order of the identifications, as the group keeps them.
                Assert.That(Members(rn2),  Is.EqualTo(new[] { E1.ToString(), E2.ToString(), E3.ToString() }));
                Assert.That(Members(rn3),  Is.EqualTo(new[] { E2.ToString(), E3.ToString() }));

                // AddElement has no old value, every other change the one it found: the element.
                Assert.That(add.Changes[0].OldValue,                     Is.Null);
                Assert.That(remove.Changes[0].OldValue?.Value<String>(), Is.EqualTo(E1.ToString()));
                Assert.That(remove.ToJSON()["changes"]![0]!["elementPath"]!.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo($$"""[{"propertyName":"EVSEIds","elementId":"{{E1}}"}]"""));

                Assert.That(parsed!.Changes[0],                          Is.EqualTo(remove.Changes[0]));
                Assert.That(rn2.TryApplyChangeSet(parsed, out var rn3b, out var applyError),  Is.True, applyError);
                Assert.That(rn3b!.CalcETags(),                           Is.EqualTo(rn3.CalcETags()));

            }

        }

        #endregion

        #region Concurrent_changes_of_other_elements_do_not_conflict()

        /// <summary>
        /// Two change sets prepared against the same version, each changing another member of the same group:
        /// the second is rebased onto the first one. Changed as a whole, the property would conflict.
        /// </summary>
        [Test]
        public void Concurrent_changes_of_other_elements_do_not_conflict()
        {

            var rn1        = NewNetwork();

            var addE2      = rn1.CreateSignedChangeSet([ RoamingNetworkChange.AddElement   (nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E2.ToString()), E2.ToString()) ], CreatedAt: T0);
            var addE3      = rn1.CreateSignedChangeSet([ RoamingNetworkChange.AddElement   (nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E3.ToString()), E3.ToString()) ], CreatedAt: T0);
            var removeE1   = rn1.CreateSignedChangeSet([ RoamingNetworkChange.RemoveElement(nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E1.ToString())) ],               CreatedAt: T0);
            var asAWhole   = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(nameof(EVSEGroup), EG1.ToString(), "EVSEIds", null, new JArray(E1.ToString(), E3.ToString())) ], CreatedAt: T0);

            var rn2        = rn1.ApplyChangeSet(addE2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.TryRebase(addE3,    out var rebased1, out var error1),  Is.True, error1);
                Assert.That(rn2.TryRebase(removeE1, out var rebased2, out var error2),  Is.True, error2);

                var rn3 = rn2.ApplyChangeSet(rebased1!.SignedByAll(rn2));
                var rn4 = rn3.ApplyChangeSet(rn3.Rebase(rebased2!).SignedByAll(rn3));

                Assert.That(Members(rn3),  Is.EqualTo(new[] { E1.ToString(), E2.ToString(), E3.ToString() }));
                Assert.That(Members(rn4),  Is.EqualTo(new[] { E2.ToString(), E3.ToString() }));

                // The same member added twice, or one removed twice, does conflict.
                Assert.That(rn3.TryRebase(addE3,  out _, out var error3),  Is.False);
                Assert.That(error3,                                        Does.Contain($"'EVSEIds['{E3}']' already exists"));

                Assert.That(rn2.TryRebase(asAWhole, out _, out var error4),  Is.False);
                Assert.That(error4,                                          Does.Contain("is no longer the expected old value"));

            }

        }

        #endregion

        #region The_properties_of_an_element_are_changed_on_their_own()

        [Test]
        public void The_properties_of_an_element_are_changed_on_their_own()
        {

            var rn1  = NewNetwork();

            var rn2  = Apply(rn1, RoamingNetworkChange.UpdateElementProperty(nameof(EVSE), E1.ToString(), Path("chargingConnectors", "1"), "lockable", null, true),
                                  RoamingNetworkChange.ReplaceElement       (nameof(EVSE), E1.ToString(), Path("chargingConnectors", "2"), null,
                                                                             new ChargingConnector(ChargingConnector_Id.Parse("2"), ChargingConnectorType.IEC_62196_T2, Format: ChargingConnectorFormat.SOCKET).ToJSON()));

            var rn3  = Apply(rn2, RoamingNetworkChange.RemoveElementProperty(nameof(EVSE), E1.ToString(), Path("chargingConnectors", "1"), "lockable"),
                                  RoamingNetworkChange.AddElement           (nameof(EVSE), E1.ToString(), Path("chargingConnectors", "3"),
                                                                             new ChargingConnector(ChargingConnector_Id.Parse("3"), ChargingConnectorType.IEC_62196_T2).ToJSON()));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Connector(rn2, "1")["lockable"]?.Value<Boolean>(),  Is.True);
                Assert.That(Connector(rn2, "1")["type"]?.Value<String>(),       Is.EqualTo(ChargingConnectorType.IEC_62196_T2.ToString()));
                Assert.That(Connector(rn2, "2")["format"]?.Value<String>(),     Is.EqualTo("SOCKET"));

                Assert.That(Connector(rn3, "1")["lockable"],                    Is.Null);
                Assert.That(Connector(rn3, "3")["type"]?.Value<String>(),       Is.EqualTo(ChargingConnectorType.IEC_62196_T2.ToString()));

            }

        }

        #endregion

        #region Invalid_element_changes_are_refused()

        [Test]
        public void Invalid_element_changes_are_refused()
        {

            var rn1 = NewNetwork();

            String Refused(params RoamingNetworkChange[] Changes)
            {
                Assert.That(() => rn1.CreateChangeSet(Changes, CreatedAt: T0), Throws.ArgumentException);
                try
                {
                    rn1.CreateChangeSet(Changes, CreatedAt: T0);
                    return "";
                }
                catch (ArgumentException e)
                {
                    return e.Message;
                }
            }

            var group = EG1.ToString();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Refused(RoamingNetworkChange.RemoveElement(nameof(EVSEGroup), group, Path("EVSEIds", "DE*GEF*E1*9"))),
                            Does.Contain("'EVSEIds['DE*GEF*E1*9']' does not exist"));

                Assert.That(Refused(RoamingNetworkChange.AddElement(nameof(EVSEGroup), group, Path("EVSEIds", E1.ToString()), E1.ToString())),
                            Does.Contain($"'EVSEIds['{E1}']' already exists"));

                Assert.That(Refused(RoamingNetworkChange.AddElement(nameof(EVSEGroup), group, Path("EVSEIds", E2.ToString()), E3.ToString())),
                            Does.Contain($"must have the identification '{E2}'"));

                Assert.That(Refused(RoamingNetworkChange.AddElement(nameof(EVSEGroup), group, Path("EVSEIds", "DE*GEF*E1*9"), "DE*GEF*E1*9")),
                            Does.Contain("DE*GEF*E1*9"));

                Assert.That(Refused(RoamingNetworkChange.RemoveElement(nameof(EVSEGroup), group, Path("EVSEIds", E1.ToString()), E2.ToString())),
                            Does.Contain($"'EVSEIds['{E1}']' of the EVSE group '{group}' is no longer the expected old value, but '\"{E1}\"'"));

                Assert.That(Refused(RoamingNetworkChange.UpdateElementProperty(nameof(EVSE), E1.ToString(), Path("chargingConnectors", "1"), "@id", null, "9")),
                            Does.Contain("The identification of 'chargingConnectors['1']' can not be changed"));

                Assert.That(Refused(RoamingNetworkChange.ReplaceElement(nameof(EVSE), E1.ToString(), Path("chargingConnectors", "1"), null,
                                                                        new ChargingConnector(ChargingConnector_Id.Parse("9"), ChargingConnectorType.IEC_62196_T2).ToJSON())),
                            Does.Contain("must keep its identification"));

                Assert.That(Refused(RoamingNetworkChange.UpdateElementProperty(nameof(ChargingTariff), T1.ToString(), Path("elements", "1"), "priceComponents", null, new JArray())),
                            Does.Contain("The elements of 'elements' have no identifications of their own: it is changed as a whole"));

                Assert.That(Refused(RoamingNetworkChange.AddElement(nameof(ChargingStationOperator), GEF.ToString(), Path("signingKeys", "key"), new JObject(new JProperty("@id", "key")))),
                            Does.Contain("are changed as a whole, not element by element"));

                // Children are entities of their own.
                Assert.That(Refused(RoamingNetworkChange.AddElement(nameof(ChargingPool), "DE*GEF*P1", Path("chargingStations", "DE*GEF*S9"), new JObject(new JProperty("@id", "DE*GEF*S9")))),
                            Does.Contain("the property 'chargingStations' of a charging pool can not be changed"));

                Assert.That(Refused(RoamingNetworkChange.UpdateElementProperty(nameof(EVSE), "DE*GEF*E1*9", Path("chargingConnectors", "1"), "lockable", null, true)),
                            Does.Contain("does not exist"));

            }

            // Owned as the property they change.
            var byEXA = rn1.CreateChangeSet([ RoamingNetworkChange.AddElement(nameof(EVSEGroup), group, Path("EVSEIds", E2.ToString()), E2.ToString()) ], CreatedAt: T0).
                            SignedBy(TestParties.Party(EXA));

            Assert.That(rn1.TryApplyChangeSet(byEXA, out _, out var ownerError),  Is.False);
            Assert.That(ownerError,                                               Does.Contain($"AddElement of the EVSEGroup '{group}' ('EVSEIds') needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'"));

        }

        #endregion

        #region Merges_take_the_changes_of_different_elements_of_the_same_property()

        [Test]
        public void Merges_take_the_changes_of_different_elements_of_the_same_property()
        {

            var rn1     = NewNetwork();

            var left    = Apply(rn1, RoamingNetworkChange.AddElement           (nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E2.ToString()), E2.ToString()),
                                     RoamingNetworkChange.UpdateElementProperty(nameof(EVSE),      E1.ToString(),  Path("chargingConnectors", "1"), "lockable", null, true));

            var right   = Apply(rn1, RoamingNetworkChange.AddElement           (nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E3.ToString()), E3.ToString()),
                                     RoamingNetworkChange.UpdateElementProperty(nameof(EVSE),      E1.ToString(),  Path("chargingConnectors", "1"), "format", null, "CABLE"));

            var other   = Apply(rn1, RoamingNetworkChange.UpdateElementProperty(nameof(EVSE),      E1.ToString(),  Path("chargingConnectors", "1"), "lockable", null, false));

            var merged  = RoamingNetwork.TryMerge(rn1, left, right, ChangeSet_Id.Parse("merge-1"), out var merge, out var changeSet, out var conflicts1);
            var refused = RoamingNetwork.TryMerge(rn1, left, other, ChangeSet_Id.Parse("merge-2"), out _,         out _,             out var conflicts2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(merged,                                       Is.True, String.Join(" ", conflicts1.Select(conflict => conflict.Description)));
                Assert.That(Members(merge!),                              Is.EqualTo(new[] { E1.ToString(), E2.ToString(), E3.ToString() }));
                Assert.That(Connector(merge!, "1")["lockable"]?.Value<Boolean>(),  Is.True);
                Assert.That(Connector(merge!, "1")["format"]?.Value<String>(),     Is.EqualTo("CABLE"));

                // Written element by element, with the old values they find.
                Assert.That(changeSet!.Changes.Select(change => (change.Kind, change.EntityType, POIElements.PathText(change.ElementPath, change.PropertyName))),
                            Is.EquivalentTo(new[] {
                                (RoamingNetworkChangeKind.AddElement,            nameof(EVSEGroup), $"EVSEIds['{E3}']"),
                                (RoamingNetworkChangeKind.UpdateElementProperty, nameof(EVSE),      "chargingConnectors['1'].format")
                            }));
                Assert.That(changeSet.Changes.Single(change => change.EntityType == nameof(EVSE)).OldValue?.Type,  Is.EqualTo(JTokenType.Null));

                // The same property of the same element changed differently does conflict.
                Assert.That(refused,                                      Is.False);
                Assert.That(conflicts2.Single().PropertyName,             Is.EqualTo("chargingConnectors"));

            }

        }

        #endregion

        #region Servers_merge_concurrent_changes_of_the_members_of_a_group()

        /// <summary>
        /// Two servers add different members to the same group at the same time: both merge their tips to the same
        /// merge commit, element by element, also when they changed the members as a whole.
        /// </summary>
        [Test]
        public void Servers_merge_concurrent_changes_of_the_members_of_a_group()
        {

            static (RoamingNetworkHistory, RoamingNetworkHistory) Servers()
            {
                var server1 = RoamingNetworkHistory.Start(NewNetwork());
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(server1.ToSnapshotJSON(), out var server2, out var error), Is.True, error);
                return (server1, server2!);
            }

            static void Commit(RoamingNetworkHistory History, Int32 Minutes, RoamingNetworkChange Change)
            {
                var head   = History.Head.RoamingNetwork;
                var result = History.Commit(head.CreateSignedChangeSet([ Change ], CreatedAt: T0.AddMinutes(Minutes)));
                Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);
            }

            static void Exchange(RoamingNetworkHistory From, RoamingNetworkHistory To)
            {
                Assert.That(From.TryGetMissingCommits(To.HaveIds(), out var missing, out var error1),  Is.True, error1);
                Assert.That(To.  TryAppend(missing!, out _, out var error2),                          Is.True, error2);
            }

            var (server1, server2) = Servers();

            Commit(server1, 1, RoamingNetworkChange.AddElement(nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E2.ToString()), E2.ToString()));
            Commit(server2, 2, RoamingNetworkChange.AddElement(nameof(EVSEGroup), EG1.ToString(), Path("EVSEIds", E3.ToString()), E3.ToString()));

            Exchange(server1, server2);
            Exchange(server2, server1);

            var (whole1, whole2) = Servers();

            Commit(whole1, 1, RoamingNetworkChange.UpdateProperty(nameof(EVSEGroup), EG1.ToString(), "EVSEIds", null, new JArray(E1.ToString(), E2.ToString())));
            Commit(whole2, 2, RoamingNetworkChange.UpdateProperty(nameof(EVSEGroup), EG1.ToString(), "EVSEIds", null, new JArray(E1.ToString(), E3.ToString())));

            Exchange(whole1, whole2);
            Exchange(whole2, whole1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(server1.Head.Commit.IsMerge,                Is.True);
                Assert.That(server1.Head.Commit.Id,                     Is.EqualTo(server2.Head.Commit.Id));
                Assert.That(server1.Tips,                               Has.Count.EqualTo(1));
                Assert.That(Members(server1.Head.RoamingNetwork),       Is.EqualTo(new[] { E1.ToString(), E2.ToString(), E3.ToString() }));

                // Changed as a whole by both, the members are still merged member by member.
                Assert.That(whole1.Head.Commit.Id,                      Is.EqualTo(whole2.Head.Commit.Id));
                Assert.That(whole2.Tips,                                Has.Count.EqualTo(1));
                Assert.That(Members(whole2.Head.RoamingNetwork),        Is.EqualTo(new[] { E1.ToString(), E2.ToString(), E3.ToString() }));
                Assert.That(whole2.Head.Commit.ChangeSet!.Changes.Single().Kind,  Is.EqualTo(RoamingNetworkChangeKind.AddElement));

            }

        }

        #endregion

        #region The_difference_of_two_versions_is_written_element_by_element()

        /// <summary>
        /// The difference an importer commits, a merge whose left version is its base, names the elements it changes;
        /// a property changed throughout is written as a whole.
        /// </summary>
        [Test]
        public void The_difference_of_two_versions_is_written_element_by_element()
        {

            var rn1       = NewNetwork();
            var desired   = Apply(rn1, RoamingNetworkChange.UpdateProperty(nameof(EVSEGroup), EG1.ToString(), "EVSEIds", null, new JArray(E1.ToString(), E3.ToString())));

            Assert.That(RoamingNetwork.TryMerge(rn1, rn1, desired, ChangeSet_Id.NewRandom(), out _, out var difference, out var conflicts),
                        Is.True, String.Join(" ", conflicts.Select(conflict => conflict.Description)));

            var element   = POIElements.Diff(nameof(EVSEGroup), EG1.ToString(), "EVSEIds", new JArray("A", "B", "C"), new JArray("A", "C", "D"));
            var whole     = POIElements.Diff(nameof(EVSEGroup), EG1.ToString(), "EVSEIds", new JArray("A", "B"),      new JArray("C", "D"));
            var order     = POIElements.Diff(nameof(EVSEGroup), EG1.ToString(), "EVSEIds", new JArray("B", "A"),      new JArray("A", "B"));
            var none      = POIElements.Diff(nameof(EVSEGroup), EG1.ToString(), "EVSEIds", new JArray("A"),           new JArray("A"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(difference!.Changes.Single().Kind,                 Is.EqualTo(RoamingNetworkChangeKind.AddElement));
                Assert.That(difference.Changes.Single().ElementPath.Single(),  Is.EqualTo(new POIElementPathSegment("EVSEIds", E3.ToString())));

                Assert.That(element.Select(change => change.Kind),  Is.EqualTo(new[] { RoamingNetworkChangeKind.RemoveElement, RoamingNetworkChangeKind.AddElement }));
                Assert.That(element[0].OldValue?.Value<String>(),    Is.EqualTo("B"));

                // More changes than elements, or another order: as a whole.
                Assert.That(whole.Single().Kind,                     Is.EqualTo(RoamingNetworkChangeKind.UpdateProperty));
                Assert.That(order.Single().Kind,                     Is.EqualTo(RoamingNetworkChangeKind.UpdateProperty));
                Assert.That(none,                                    Is.Empty);

            }

        }

        #endregion

    }

}
