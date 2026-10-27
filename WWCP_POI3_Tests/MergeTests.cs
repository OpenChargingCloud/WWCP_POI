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

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// Servers without a leader: every one commits, they exchange their commits,
    /// and every one merges the tips the same way, deterministically.
    /// </summary>
    [TestFixture]
    public class MergeTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-13T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  EXA      = ChargingStationOperator_Id.Parse("DE*EXA");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                  DataSource:                "v1",
                                  Created:                   Created,
                                  ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                           Created:        Created,
                                                                                           ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                              Created:           Created,
                                                                                                                              ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                       Created:  Created,
                                                                                                                                                                       EVSEs:    [ new EVSE(E1, MaxPower: Watt.FromW(22000), Created: Created) ]) ]) ]),
                                                               new ChargingStationOperator(EXA, Created: Created) ]).WithKeys();

        #endregion

        #region Servers()

        /// <summary>
        /// Two servers, starting with the same genesis commit.
        /// </summary>
        private static (RoamingNetworkHistory Server1, RoamingNetworkHistory Server2) Servers()
        {

            var server1 = RoamingNetworkHistory.Start(NewNetwork());

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(server1.ToSnapshotJSON(), out var server2, out var error), Is.True, error);

            return (server1, server2!);

        }

        #endregion

        #region Change(History, Minutes, Changes)

        /// <summary>
        /// Commit the given changes, signed by their owners, to the head of the given history.
        /// </summary>
        private static RoamingNetworkCommit Change(RoamingNetworkHistory                                History,
                                                   Int32                                                Minutes,
                                                   Func<RoamingNetwork, RoamingNetworkChange[]>         Changes)
        {

            var head    = History.Head.RoamingNetwork;
            var result  = History.Commit(head.CreateSignedChangeSet(Changes(head), CreatedAt: T0.AddMinutes(Minutes)));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            return result.Commit!;

        }

        #endregion

        #region Exchange(From, To)

        /// <summary>
        /// Send the commits the one server misses from the other one.
        /// </summary>
        private static void Exchange(RoamingNetworkHistory  From,
                                     RoamingNetworkHistory  To)
        {

            Assert.That(From.TryGetMissingCommits(To.HaveIds(), out var missing, out var error1),  Is.True, error1);
            Assert.That(To.  TryAppend(missing!, out _, out var error2),                                                   Is.True, error2);

        }

        #endregion

        #region (static) EVSE(Network) / Pool(Network)

        private static EVSE EVSE(RoamingNetwork Network)
            => Network.GetChargingStationOperatorById(GEF)!.EVSEs.Single();

        private static ChargingPool Pool(RoamingNetwork Network)
            => Network.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();

        #endregion


        #region Concurrent_commits_merge_to_the_same_commit_everywhere()

        [Test]
        public void Concurrent_commits_merge_to_the_same_commit_everywhere()
        {

            var (server1, server2) = Servers();

            // GEF changes its EVSE on server 1, EXA its telephone on server 2, the roaming network operator its data source.
            var a  = Change(server1, 1, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 11000) ]);
            var b  = Change(server2, 2, network => [ RoamingNetworkChange.UpdateProperty(network.GetChargingStationOperatorById(EXA)!, "telephone", null, "+49 30 1") ]);
            var c  = Change(server2, 3, network => [ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "v2") ]);

            Exchange(server1, server2);
            Exchange(server2, server1);

            using (Assert.EnterMultipleScope())
            {

                // The same merge commit on both, whichever got what first.
                Assert.That(server1.Head.Commit.Id,                                           Is.EqualTo(server2.Head.Commit.Id));
                Assert.That(server1.Head.Commit.IsMerge,                                      Is.True);
                Assert.That(server1.Head.Commit.Revision,                                     Is.EqualTo(3));
                Assert.That(server1.Head.Commit.ParentIds,                                    Is.EqualTo(new[] { c.Id, a.Id }));
                Assert.That(server1.Tips,                                                     Has.Count.EqualTo(1));
                Assert.That(server1.Count,                                                    Is.EqualTo(server2.Count));

                var merged = server1.Head.RoamingNetwork;
                Assert.That(EVSE(merged).MaxPower?.Value,                                     Is.EqualTo(11000m));
                Assert.That(merged.GetChargingStationOperatorById(EXA)!.Telephone.ToString(), Is.EqualTo("+49 30 1"));
                Assert.That(merged.DataSource,                                                Is.EqualTo("v2"));
                Assert.That(merged.ETags,                                                     Is.EqualTo(server2.Head.RoamingNetwork.ETags));

                // Not signed by owners, but by nobody needed: everyone calculates it.
                Assert.That(server1.Head.Commit.ChangeSet!.Signatures,                       Is.Empty);
                Assert.That(server1.Head.Commit.ChangeSet!.Id.ToString(),                    Does.StartWith(RoamingNetworkHistory.MergeChangeSetIdPrefix));

            }

        }

        #endregion

        #region The_same_party_in_another_spelling_is_the_same_entity()

        /// <summary>
        /// An e-mobility provider both versions added, once as 'DE-ABC' and once as 'DE*ABC', is the same one, as its
        /// identification type compares them: added alike, the merge keeps it as the first version writes it;
        /// added with different properties, they conflict on them.
        /// </summary>
        [Test]
        public void The_same_party_in_another_spelling_is_the_same_entity()
        {

            var rn1       = NewNetwork();
            var provider  = new EMobilityProvider(EMobilityProvider_Id.Parse("DE-ABC"),
                                                  I18NString.Create(Languages.en, "ABC Mobility"),
                                                  Created: Created).WithKeys().ToJSON();

            RoamingNetwork Added(String Id, String Name)
            {
                var json      = (JObject) provider.DeepClone();
                json["@id"]   = Id;
                json["name"]  = new JObject(new JProperty("en", Name));
                return rn1.ApplyChangeSet(rn1.CreateChangeSet([ RoamingNetworkChange.Add(nameof(EMobilityProvider), Id, json) ], CreatedAt: T0).
                                              SignedBy(TestParties.Party(rn1)).
                                              ProvenByNewIdentityKeys());
            }

            var left      = Added("DE-ABC", "ABC Mobility");
            var right     = Added("DE*ABC", "ABC Mobility");
            var other     = Added("DEABC",  "Other Mobility");

            var merged    = RoamingNetwork.TryMerge(rn1, left, right, ChangeSet_Id.Parse("merge-1"), out var merge, out var changeSet, out var conflicts1);
            var refused   = RoamingNetwork.TryMerge(rn1, left, other, ChangeSet_Id.Parse("merge-2"), out _,         out _,             out var conflicts2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(merged,                                          Is.True, String.Join(" ", conflicts1.Select(conflict => conflict.Description)));
                Assert.That(changeSet!.Changes,                              Is.Empty);
                Assert.That(merge!.EMobilityProviders.Single().Id.ToString(), Is.EqualTo("DE-ABC"));

                Assert.That(refused,                                         Is.False);
                Assert.That(conflicts2.Single().Kind,                        Is.EqualTo(RoamingNetworkMergeConflictKind.AddAdd));
                Assert.That(conflicts2.Single().PropertyName,                Is.EqualTo("name"));

            }

        }

        #endregion

        #region Additions_on_both_sides_are_merged()

        [Test]
        public void Additions_on_both_sides_are_merged()
        {

            var (server1, server2) = Servers();

            Change(server1, 1, network => [ RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P2"))) ]);
            Change(server2, 2, network => [ RoamingNetworkChange.Add(new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S2"),
                                                                                          EVSEs: [ new EVSE(EVSE_Id.Parse("DE*GEF*E2*1")) ]),
                                                                     ChargingPool_Id.Parse("DE*GEF*P1")) ]);

            Exchange(server2, server1);
            Exchange(server1, server2);

            var gef = server1.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server1.Head.Commit.Id,                                Is.EqualTo(server2.Head.Commit.Id));
                Assert.That(gef.ChargingPools.Select(pool => pool.Id.ToString()),  Is.EqualTo(new[] { "DE*GEF*P1", "DE*GEF*P2" }));
                Assert.That(gef.EVSEs.Select(evse => evse.Id.ToString()),          Is.EqualTo(new[] { "DE*GEF*E1*1", "DE*GEF*E2*1" }));
            }

        }

        #endregion

        #region Preconditions_hold_also_for_the_head_a_change_set_is_merged_into()

        /// <summary>
        /// A change of an EVSE, prepared while its charging pool had a certain ETag, committed after the head moved on:
        /// refused as a conflict with the head when the head changed the pool, merged when it changed something else.
        /// </summary>
        [Test]
        public void Preconditions_hold_also_for_the_head_a_change_set_is_merged_into()
        {

            var (server1, _) = Servers();
            var v1           = server1.Head.RoamingNetwork;

            RoamingNetworkChangeSet Guarded(Int32 Minutes)
                => v1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(EVSE(v1), "maxPower", null, 11000) ],
                                      CreatedAt:      T0.AddMinutes(Minutes),
                                      Preconditions:  [ v1.Precondition(nameof(POI.ChargingPool), Pool(v1).Id.ToString()) ]).SignedByAll(v1);

            var forOther  = Guarded(2);
            var forPool   = Guarded(3);

            // The head changes another operator: the change set made for v1 is merged into it.
            Change(server1, 1, network => [ RoamingNetworkChange.UpdateProperty(network.GetChargingStationOperatorById(EXA)!, "telephone", null, "+49 30 1") ]);

            var merged    = server1.Commit(forOther);

            // The head changes the charging pool: a change set made for the version before is refused.
            Change(server1, 4, network => [ RoamingNetworkChange.UpdateProperty(Pool(network), "hotline", null, "+49 800 1234") ]);

            var refused   = server1.Commit(forPool);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(merged.Status,                                       Is.EqualTo(RoamingNetworkCommitStatus.Committed), merged.ErrorResponse);
                Assert.That(server1.Tips,                                        Has.Count.EqualTo(1));
                Assert.That(EVSE(server1.Head.RoamingNetwork).MaxPower?.Value,   Is.EqualTo(11000m));
                Assert.That(refused.Status,                                      Is.EqualTo(RoamingNetworkCommitStatus.Conflict));
                // Both ways: the head changed the pool, and the change set its EVSE, which the pool embeds,
                // while the change set merged before requires the pool as it was, too.
                Assert.That(refused.Conflicts!.Select(conflict => conflict.Kind),      Is.All.EqualTo(RoamingNetworkMergeConflictKind.Precondition));
                Assert.That(refused.Conflicts!.Select(conflict => conflict.EntityId),  Is.All.EqualTo(Pool(v1).Id.ToString()));
                Assert.That(refused.ErrorResponse,                               Does.Contain($"requires the ChargingPool '{Pool(v1).Id}'").And.Contain("Prepare it again for the head!"));
                Assert.That(server1.Tips,                                        Has.Count.EqualTo(1));
            }

        }

        #endregion

        #region A_peer_change_set_whose_preconditions_the_other_tip_broke_conflicts()

        /// <summary>
        /// One server changes an EVSE while its charging pool is as it read it, the other one changes the pool meanwhile:
        /// the tips are not merged, they conflict, until the owner decides.
        /// </summary>
        [Test]
        public void A_peer_change_set_whose_preconditions_the_other_tip_broke_conflicts()
        {

            var (server1, server2) = Servers();

            var v1 = server2.Head.RoamingNetwork;

            Assert.That(server2.Commit(v1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(EVSE(v1), "maxPower", null, 11000) ],
                                                          CreatedAt:      T0.AddMinutes(2),
                                                          Preconditions:  [ v1.Precondition(nameof(POI.ChargingPool), Pool(v1).Id.ToString()) ]).SignedByAll(v1)).Status,
                        Is.EqualTo(RoamingNetworkCommitStatus.Committed));

            Change(server1, 1, network => [ RoamingNetworkChange.UpdateProperty(Pool(network), "hotline", null, "+49 800 1234") ]);

            Exchange(server1, server2);
            Exchange(server2, server1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server1.Tips,                                Has.Count.EqualTo(2));
                Assert.That(server2.Tips,                                Has.Count.EqualTo(2));

                var conflict = server1.Conflicts.Single();
                Assert.That(conflict.Kind,                               Is.EqualTo(RoamingNetworkMergeConflictKind.Precondition));
                Assert.That(conflict.EntityType,                         Is.EqualTo(nameof(POI.ChargingPool)));
                Assert.That(conflict.Description,                        Does.Contain("changed it since their common ancestor"));
                Assert.That(server2.Conflicts.Single().Kind,             Is.EqualTo(RoamingNetworkMergeConflictKind.Precondition));
            }

        }

        #endregion

        #region Conflicts_stay_until_the_owner_resolves_them()

        [Test]
        public void Conflicts_stay_until_the_owner_resolves_them()
        {

            var (server1, server2) = Servers();

            // GEF changes the same property differently on both servers.
            Change(server1, 1, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 11000) ]);
            Change(server2, 2, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 7400) ]);

            Exchange(server1, server2);
            Exchange(server2, server1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(server1.Tips,                                Has.Count.EqualTo(2));
                Assert.That(server2.Tips,                                Has.Count.EqualTo(2));
                Assert.That(server1.Head.Commit.Id,                      Is.EqualTo(server2.Head.Commit.Id));

                var conflict = server1.Conflicts.Single();
                Assert.That(conflict.Kind,                               Is.EqualTo(RoamingNetworkMergeConflictKind.DifferentValues));
                Assert.That(conflict.EntityType,                         Is.EqualTo(nameof(POI.EVSE)));
                Assert.That(conflict.EntityId,                           Is.EqualTo(E1.ToString()));
                Assert.That(conflict.PropertyName,                       Is.EqualTo("maxPower"));
                Assert.That(conflict.Base!.Value<Decimal>(),             Is.EqualTo(22000m));
                Assert.That(new[] { conflict.Left!.Value<Decimal>(), conflict.Right!.Value<Decimal>() },  Is.EquivalentTo(new[] { 11000m, 7400m }));

            }

            // GEF decides for 7400 W: on the other tip, the one with 11000 W.
            var tip11000  = server1.Tips.Single(tip => EVSE(tip.RoamingNetwork).MaxPower?.Value == 11000m).RoamingNetwork;
            var resolved  = server1.Commit(tip11000.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(EVSE(tip11000), "maxPower", null, 7400) ],
                                                                          CreatedAt: T0.AddMinutes(3)));

            Exchange(server1, server2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolved.Status,                             Is.EqualTo(RoamingNetworkCommitStatus.Committed), resolved.ErrorResponse);
                Assert.That(server1.Tips,                                Has.Count.EqualTo(1));
                Assert.That(server1.Conflicts,                           Is.Empty);
                Assert.That(EVSE(server1.Head.RoamingNetwork).MaxPower?.Value,  Is.EqualTo(7400m));
                Assert.That(server2.Head.Commit.Id,                      Is.EqualTo(server1.Head.Commit.Id));
            }

        }

        #endregion

        #region A_removed_entity_changed_elsewhere_conflicts()

        [Test]
        public void A_removed_entity_changed_elsewhere_conflicts()
        {

            var (server1, server2) = Servers();

            // The roaming network operator removes EXA, while EXA changes its telephone.
            Change(server1, 1, network => [ RoamingNetworkChange.Remove(EXA) ]);
            Change(server2, 2, network => [ RoamingNetworkChange.UpdateProperty(network.GetChargingStationOperatorById(EXA)!, "telephone", null, "+49 30 1") ]);

            Exchange(server2, server1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server1.Tips,                                Has.Count.EqualTo(2));
                Assert.That(server1.Conflicts.Single().Kind,             Is.EqualTo(RoamingNetworkMergeConflictKind.DeleteModify));
                Assert.That(server1.Conflicts.Single().EntityId,         Is.EqualTo("DE*EXA"));
            }

        }

        #endregion

        #region (static) GEFSigningKeys(Network, Key)

        /// <summary>
        /// A change of the signing keys of GEF to the given one, signed by all parties of the given network.
        /// </summary>
        private static RoamingNetworkChange GEFSigningKeys(RoamingNetwork  Network,
                                                           POIPublicKey    Key)

            => RoamingNetworkChange.UpdateProperty(Network.GetChargingStationOperatorById(GEF)!,
                                                   POIPartyKeys.SigningKeysProperty,
                                                   null,
                                                   new JArray(Key.ToJSON(Embedded: true)));

        #endregion

        #region A_removed_key_signs_no_change_set_through_an_older_version()

        /// <summary>
        /// GEF replaces its signing key K: a change set signed with K, made for the version before, is refused, as it
        /// would carry K into the head, which no longer names it (docs/csharp-review.md of WWCP_POI.TS, finding 3).
        /// </summary>
        [Test]
        public void A_removed_key_signs_no_change_set_through_an_older_version()
        {

            var (server1, _)  = Servers();
            var v1            = server1.Head.RoamingNetwork;
            var withK         = v1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(EVSE(v1), "maxPower", null, 11000) ], CreatedAt: T0.AddMinutes(2));
            var next          = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF/next"), POIKeyKind.Signing).Public;

            Change(server1, 1, network => [ GEFSigningKeys(network, next) ]);

            var refused = server1.Commit(withK);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.Status,                                      Is.EqualTo(RoamingNetworkCommitStatus.Conflict));
                Assert.That(refused.Conflicts!.Single().Kind,                    Is.EqualTo(RoamingNetworkMergeConflictKind.RevokedKey));
                Assert.That(refused.ErrorResponse,                               Does.Contain($"is signed by the key '{TestParties.Key(TestParties.Party(GEF)).Public.Id}'").And.Contain("no longer names").And.Contain("Prepare it again for the head!"));
                Assert.That(server1.Tips,                                        Has.Count.EqualTo(1));
                Assert.That(EVSE(server1.Head.RoamingNetwork).MaxPower?.Value,   Is.EqualTo(22000m));
            }

        }

        #endregion

        #region A_peer_change_set_signed_by_a_key_the_other_tip_removed_conflicts()

        /// <summary>
        /// One server takes a change of GEF signed with its key K, while the other one takes the replacement of K: the
        /// commit is taken from the peer, but the tips are not merged, as it would carry K into the version that removed it.
        /// </summary>
        [Test]
        public void A_peer_change_set_signed_by_a_key_the_other_tip_removed_conflicts()
        {

            var (server1, server2) = Servers();
            var next               = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF/next"), POIKeyKind.Signing).Public;

            Change(server2, 2, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 11000) ]);
            Change(server1, 1, network => [ GEFSigningKeys(network, next) ]);

            Exchange(server2, server1);
            Exchange(server1, server2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(server1.Tips,                     Has.Count.EqualTo(2));
                Assert.That(server2.Tips,                     Has.Count.EqualTo(2));

                var conflict = server1.Conflicts.Single();
                Assert.That(conflict.Kind,                    Is.EqualTo(RoamingNetworkMergeConflictKind.RevokedKey));
                Assert.That(conflict.EntityId,                Is.EqualTo(GEF.ToString()));
                Assert.That(conflict.Description,             Does.Contain("no longer names"));
                Assert.That(server2.Conflicts.Single().Kind,  Is.EqualTo(RoamingNetworkMergeConflictKind.RevokedKey));

            }

        }

        #endregion

        #region A_key_signs_only_while_it_is_valid_when_taken()

        /// <summary>
        /// A key that expired signs nothing by a creation chosen before: a server takes a change set only when its keys are
        /// valid also when it takes it, and one created no later than then. Merged, a key that expired by the time the
        /// other tip moved on carries no change set into it.
        /// </summary>
        [Test]
        public void A_key_signs_only_while_it_is_valid_when_taken()
        {

            // GEF signs with a key valid until ten minutes after T0.
            var gef       = TestParties.Key(TestParties.Party(GEF));
            var json      = NewNetwork().ToJSON();
            var operators = (JArray) json[RoamingNetwork.PropertyKeys.ChargingStationOperators]!;
            ((JObject) operators.Single(item => item["@id"]!.Value<String>() == GEF.ToString()))[POIPartyKeys.SigningKeysProperty] =
                new JArray(POIPublicKey.From(gef.Private, NotAfter: T0.AddMinutes(10)).ToJSON(Embedded: true));

            Assert.That(RoamingNetwork.TryParse(json, out var network, out var parseError), Is.True, parseError);

            var server1 = RoamingNetworkHistory.Start(network!);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(server1.ToSnapshotJSON(), out var server2, out var snapshotError), Is.True, snapshotError);

            RoamingNetworkChangeSet SignedByGEF(RoamingNetwork Version, Int32 Minutes, Decimal MaxPower)
                => Version.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(EVSE(Version), "maxPower", null, MaxPower) ], CreatedAt: T0.AddMinutes(Minutes)).
                           Sign(gef.Private, gef.Public.Id, COSEAlgorithm.ES256);

            var v1 = server1.Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {

                // Taken after the key expired, though created before: refused; created in the future: refused.
                var expired = server1.Commit(SignedByGEF(v1, 5, 11000), At: T0.AddMinutes(30));
                Assert.That(expired.Status,         Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(expired.ErrorResponse,  Does.Contain($"is not valid at {T0.AddMinutes(30).ToISO8601()}"));

                var future  = server1.Commit(SignedByGEF(v1, 9, 11000), At: T0.AddMinutes(2));
                Assert.That(future.Status,          Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(future.ErrorResponse,   Does.Contain("later than now"));

                // Taken while it is valid, by server 2; server 1 moves on after it expired, with a change of EXA signed by EXA.
                var taken   = server2!.Commit(SignedByGEF(v1, 5, 11000), At: T0.AddMinutes(6));
                Assert.That(taken.Status,           Is.EqualTo(RoamingNetworkCommitStatus.Committed), taken.ErrorResponse);

                var movedOn = server1.Commit(v1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(v1.GetChargingStationOperatorById(EXA)!, "telephone", null, "+49 30 1") ], CreatedAt: T0.AddMinutes(20)).
                                                SignedBy(TestParties.Party(EXA)),
                                             At: T0.AddMinutes(20));
                Assert.That(movedOn.Status,         Is.EqualTo(RoamingNetworkCommitStatus.Committed), movedOn.ErrorResponse);

            }

            var ownTip = server1.Head.Commit.Id;

            Exchange(server2!, server1);

            // The tips are ordered by their ids, so server 1's own is the left or the right version by chance.
            var side   = server1.Tips[0].Commit.Id == ownTip ? "left" : "right";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server1.Tips,                         Has.Count.EqualTo(2));
                Assert.That(server1.Conflicts.Single().Kind,      Is.EqualTo(RoamingNetworkMergeConflictKind.RevokedKey));
                Assert.That(server1.Conflicts.Single().Description,  Does.Contain($"is not valid in the {side} version at {T0.AddMinutes(20).ToISO8601()}"));
            }

        }

        #endregion

        #region Merge_commits_are_checked()

        [Test]
        public void Merge_commits_are_checked()
        {

            var (server1, server2) = Servers();

            Change(server1, 1, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 11000) ]);
            Change(server2, 2, network => [ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "v2") ]);

            Exchange(server2, server1);

            var merge    = server1.Head.Commit;

            // The parents only: server 2 makes the merge itself.
            Assert.That(server1.TryGetMissingCommits(server2.HaveIds(), out var missing, out _), Is.True);
            var parents  = missing!.Where(commit => !commit.IsMerge).ToArray();

            Assert.That(server2.TryAppend(parents, out _, out var error0),  Is.True, error0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(server2.Head.Commit.Id,                          Is.EqualTo(merge.Id));
                Assert.That(server2.TryAppend(merge, out var error2),       Is.True, error2);

                // Another change set under the parents of the merge, with its own identification.
                Assert.That(server1.TryAppend(WithContent(merge, json => json["changeSet"]!["changes"]![0]!["newValue"] = "forged"), out var error3),  Is.False);
                Assert.That(error3,                                          Does.Contain("is not the merge of its parents"));

                // The parents the other way round.
                Assert.That(server1.TryAppend(WithContent(merge, json => json["parentIds"] = new JArray(merge.ParentIds.Reverse().Select(id => id.ToString()))), out var error4),  Is.False);
                Assert.That(error4,                                          Does.Contain("does not name its parents in their order"));

            }

        }

        #endregion

        #region ChangeAt(History, Commit, Minutes, Changes)

        /// <summary>
        /// Commit the given changes, signed by their owners, to the version of the given tip of the given history.
        /// </summary>
        private static RoamingNetworkCommit ChangeAt(RoamingNetworkHistory                         History,
                                                     RoamingNetworkCommit                          Commit,
                                                     Int32                                         Minutes,
                                                     Func<RoamingNetwork, RoamingNetworkChange[]>  Changes)
        {

            var version  = History.Tips.Single(tip => tip.Commit.Id == Commit.Id).RoamingNetwork;
            var result   = History.Commit(version.CreateSignedChangeSet(Changes(version), CreatedAt: T0.AddMinutes(Minutes)));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            return result.Commit!;

        }

        #endregion

        #region CrissCross(ResolveOnServer2)

        /// <summary>
        /// GEF sets the maximum power of its EVSE to 11000 W on server 1 and to 7400 W on server 2, the tips conflict.
        /// On server 1 it resolves for 7400 W, on top of the tip with 11000 W, and server 1 merges; on server 2
        /// it resolves as given, and server 2 merges. Both merges have the two conflicting commits as their
        /// best common ancestors.
        /// </summary>
        private static (RoamingNetworkHistory Server1, RoamingNetworkHistory Server2) CrissCross(Func<RoamingNetworkHistory, RoamingNetworkCommit, RoamingNetworkCommit, RoamingNetworkCommit> ResolveOnServer2)
        {

            var (server1, server2) = Servers();

            var x = Change(server1, 1, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 11000) ]);
            var y = Change(server2, 2, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 7400) ]);

            Exchange(server1, server2);
            Exchange(server2, server1);

            Assert.That(server1.Tips, Has.Count.EqualTo(2));

            ChangeAt(server1, x, 3, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 7400) ]);
            ResolveOnServer2(server2, x, y);

            Assert.That(server1.Tips, Has.Count.EqualTo(1));
            Assert.That(server2.Tips, Has.Count.EqualTo(1));

            Exchange(server1, server2);
            Exchange(server2, server1);

            return (server1, server2);

        }

        #endregion


        #region An_entity_its_common_ancestors_conflict_at_conflicts_as_a_whole()

        /// <summary>
        /// GEF removes its EVSE group on server 1, and adds an EVSE to it on server 2: they conflict. Both servers decide
        /// on the version without it, adding it again with other members, and merge. The merges have both conflicting
        /// commits as their common ancestors, whose virtual merge base has the group as a conflict marker as a whole:
        /// the merges conflict there, their members are not united.
        /// </summary>
        [Test]
        public void An_entity_its_common_ancestors_conflict_at_conflicts_as_a_whole()
        {

            var e2       = EVSE_Id.     Parse("DE*GEF*E1*2");
            var e3       = EVSE_Id.     Parse("DE*GEF*E1*3");
            var group    = EVSEGroup_Id.Parse("DE*GEF*EG1");

            var network  = new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                              Created:                   Created,
                                              ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                                       Created:        Created,
                                                                                                       ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                                          Created:           Created,
                                                                                                                                          ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                                   Created:  Created,
                                                                                                                                                                                   EVSEs:    [ new EVSE(E1, Created: Created),
                                                                                                                                                                                               new EVSE(e2, Created: Created),
                                                                                                                                                                                               new EVSE(e3, Created: Created) ]) ]) ],
                                                                                                       EVSEGroups:     [ new EVSEGroup(group, EVSEIds: [ E1 ], Created: Created) ]) ]).WithKeys();

            var server1  = RoamingNetworkHistory.Start(network);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(server1.ToSnapshotJSON(), out var server2, out var error), Is.True, error);

            EVSEGroup GroupOf(RoamingNetwork Network)
                => Network.GetChargingStationOperatorById(GEF)!.EVSEGroups.Single();

            var x = Change(server1,  1, network => [ RoamingNetworkChange.Remove(group) ]);
            var y = Change(server2!, 2, network => [ RoamingNetworkChange.UpdateProperty(GroupOf(network), "EVSEIds", null, new JArray(E1.ToString(), e2.ToString())) ]);

            Exchange(server1,  server2!);
            Exchange(server2!, server1);

            Assert.That(server1.Conflicts.Single().Kind,  Is.EqualTo(RoamingNetworkMergeConflictKind.DeleteModify));

            // Both decide on the version without it, each adding it again with other members, and merge.
            ChangeAt(server1,  x, 3, network => [ RoamingNetworkChange.Add(new EVSEGroup(group, EVSEIds: [ E1, e3 ], Created: Created)) ]);
            ChangeAt(server2!, x, 4, network => [ RoamingNetworkChange.Add(new EVSEGroup(group, EVSEIds: [ E1 ],     Created: Created)) ]);

            Assert.That(server1. Tips,  Has.Count.EqualTo(1));
            Assert.That(server2!.Tips,  Has.Count.EqualTo(1));
            Assert.That(GroupOf(server1. Head.RoamingNetwork).EVSEIds,  Is.EquivalentTo(new[] { E1, e2, e3 }));
            Assert.That(GroupOf(server2!.Head.RoamingNetwork).EVSEIds,  Is.EquivalentTo(new[] { E1, e2 }));

            Exchange(server1,  server2!);
            Exchange(server2!, server1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(server1.Tips,                                       Has.Count.EqualTo(2));
                Assert.That(server2!.Head.Commit.Id,                            Is.EqualTo(server1.Head.Commit.Id));

                var conflict = server1.Conflicts.Single();
                Assert.That(conflict.Kind,                                      Is.EqualTo(RoamingNetworkMergeConflictKind.DifferentValues));
                Assert.That(conflict.EntityType,                                Is.EqualTo(nameof(EVSEGroup)));
                Assert.That(conflict.EntityId,                                  Is.EqualTo(group.ToString()));
                Assert.That(conflict.PropertyName,                              Is.Null);
                Assert.That(conflict.Base?[RoamingNetwork.ConflictMarkerProperty],  Is.Not.Null);
                Assert.That(conflict.Description,                               Does.Contain("The common ancestors of both versions conflict"));

            }

        }

        #endregion

        #region Conflicting_common_ancestors_resolved_alike_merge()

        [Test]
        public void Conflicting_common_ancestors_resolved_alike_merge()
        {

            // Server 2 resolves for 7400 W too, in a commit of its own.
            var (server1, server2) = CrissCross((server2, x, y) => ChangeAt(server2, x, 4, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 7400) ]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server1.Tips,                                       Has.Count.EqualTo(1));
                Assert.That(server1.Conflicts,                                  Is.Empty);
                Assert.That(server1.Head.Commit.IsMerge,                        Is.True);
                Assert.That(EVSE(server1.Head.RoamingNetwork).MaxPower?.Value,  Is.EqualTo(7400m));
                Assert.That(server2.Head.Commit.Id,                             Is.EqualTo(server1.Head.Commit.Id));
                Assert.That(server2.Head.RoamingNetwork.ETags,                  Is.EqualTo(server1.Head.RoamingNetwork.ETags));
            }

        }

        #endregion

        #region Conflicting_common_ancestors_resolved_differently_conflict_until_the_owner_decides()

        [Test]
        public void Conflicting_common_ancestors_resolved_differently_conflict_until_the_owner_decides()
        {

            // Server 2 resolves for 11000 W, on top of the tip with 7400 W.
            var (server1, server2) = CrissCross((server2, x, y) => ChangeAt(server2, y, 4, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 11000) ]));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(server1.Tips,                                       Has.Count.EqualTo(2));
                Assert.That(server2.Head.Commit.Id,                             Is.EqualTo(server1.Head.Commit.Id));

                // Where the common ancestors conflict, their merge base has a conflict marker.
                var conflict = server1.Conflicts.Single();
                Assert.That(conflict.Kind,                                      Is.EqualTo(RoamingNetworkMergeConflictKind.DifferentValues));
                Assert.That(conflict.EntityId,                                  Is.EqualTo(E1.ToString()));
                Assert.That(conflict.PropertyName,                              Is.EqualTo("maxPower"));
                Assert.That(conflict.Base?[RoamingNetwork.ConflictMarkerProperty]?["base"]?.Value<Decimal>(),  Is.EqualTo(22000m));
                Assert.That(new[] { conflict.Left!.Value<Decimal>(), conflict.Right!.Value<Decimal>() },        Is.EquivalentTo(new[] { 11000m, 7400m }));

            }

            // GEF decides for 7400 W, on the tip with 11000 W.
            var tip11000 = server1.Tips.Single(tip => EVSE(tip.RoamingNetwork).MaxPower?.Value == 11000m).Commit;

            ChangeAt(server1, tip11000, 5, network => [ RoamingNetworkChange.UpdateProperty(EVSE(network), "maxPower", null, 7400) ]);

            Exchange(server1, server2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server1.Tips,                                       Has.Count.EqualTo(1));
                Assert.That(server1.Conflicts,                                  Is.Empty);
                Assert.That(EVSE(server1.Head.RoamingNetwork).MaxPower?.Value,  Is.EqualTo(7400m));
                Assert.That(server2.Head.Commit.Id,                             Is.EqualTo(server1.Head.Commit.Id));
            }

        }

        #endregion

        #region (private static) WithContent(Commit, Change)

        /// <summary>
        /// The given commit with another content, under the identification of that content, as an attacker would make it.
        /// </summary>
        private static RoamingNetworkCommit WithContent(RoamingNetworkCommit  Commit,
                                                        Action<JObject>       Change)
        {

            var json = Commit.ToJSON();
            Change(json);

            json["@id"] = new String('0', 64);
            RoamingNetworkCommit.TryParse(json, out _, out var error);

            // "The commit identification '000…' is not the one of its content, '<id>'!"
            json["@id"] = error!.Split('\'')[3];

            Assert.That(RoamingNetworkCommit.TryParse(json, out var commit, out var parseError), Is.True, parseError);

            return commit!;

        }

        #endregion

    }

}
