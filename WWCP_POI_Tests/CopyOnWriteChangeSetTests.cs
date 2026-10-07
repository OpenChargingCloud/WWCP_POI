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

using System.Text.Json;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using cloud.charging.open.protocols.WWCP.POI;

#endregion

namespace WWCP_POI_Tests
{
    [TestFixture]
    public sealed class CopyOnWriteChangeSetTests
    {
        private static readonly DateTimeOffset CommitTime = DateTimeOffset.Parse("2026-10-06T12:30:00Z");
        private static JsonElement Json(string value)
        {

            using var document = JsonDocument.Parse(value);

            return document.RootElement.Clone();

        }

        private static RoamingNetwork Network() => RoamingNetwork.Parse(JObject.Parse("""
            {
              "@id": "network-a",
              "name": {
                "en": "Network A"
              },
              "chargingStationOperators": [
                {
                  "@id": "DE*ABC",
                  "name": {
                    "en": "Operator A"
                  },
                  "chargingPools": [
                    {
                      "@id": "DE*ABC*P1",
                      "name": {
                        "en": "Pool A"
                      },
                      "chargingStations": [
                        {
                          "@id": "DE*ABC*S1",
                          "name": {
                            "en": "Station A"
                          },
                          "EVSEs": [
                            {
                              "@id": "DE*ABC*E1",
                              "currentType": [
                                "DC"
                              ],
                              "maxPower": "100000 W",
                              "socketOutlets": [
                                {
                                  "@id": "1",
                                  "type": "CCS",
                                  "lockable": false
                                }
                              ]
                            },
                            {
                              "@id": "DE*ABC*E2",
                              "currentType": [
                                "AC_ThreePhases"
                              ],
                              "maxPower": "22000 W",
                              "socketOutlets": [
                                {
                                  "@id": "1",
                                  "type": "Type2"
                                }
                              ]
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
            """));

        private static RoamingNetworkChangeSet Set(RoamingNetwork                 network,
                                                   params RoamingNetworkChange[]  changes)
            => network.CreateChangeSet("change-a", CommitTime, [.. changes]);

        [Test]
        public void Property_update_creates_a_new_version_and_shares_unchanged_entities()
        {

            var source = Network();
            var oldData = source.DataSnapshot;
            var before = source.ToJSONSnapshot();
            var untouched = oldData.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E2");
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Json("\"100000 W\""), Json("\"150000.125 W\""))));

            Assert.That(changed, Is.Not.SameAs(source));
            Assert.That(changed.Revision, Is.EqualTo(1));
            Assert.That(source.Revision, Is.Zero);
            Assert.That(changed.AppliedChangeSetId, Is.EqualTo("change-a"));
            Assert.That(changed.DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E2"), Is.SameAs(untouched));
            Assert.That(changed.DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
                                                        Is.Not.SameAs(oldData.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1")));
            Assert.That(changed.DataSnapshot.GetEntity(InfrastructureEntityType.ChargingStation, "DE*ABC*S1"),
                                                        Is.Not.SameAs(oldData.GetEntity(InfrastructureEntityType.ChargingStation, "DE*ABC*S1")));
            Assert.That(JToken.DeepEquals(source.ToJSONSnapshot(), before), Is.True);
            Assert.That(changed.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1").MaxPower!.Value.Value, Is.EqualTo(150000.125m));
            Assert.That(changed.EVSEs.All(item => ReferenceEquals(item.RoamingNetwork, changed)), Is.True);

        }

        [Test]
        public void Add_station_with_nested_EVSEs_and_remove_another_subtree()
        {

            var source = Network();
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.Add("ChargingStation", "DE*ABC*S2", Json("""
            {"@id":"DE*ABC*S2","name":{"en":"New station"},"EVSEs":[
              {"@id":"DE*ABC*E3","currentType":["DC"],"socketOutlets":[{"@id":"1","type":"CCS"}]}]}
            """), "ChargingPool", "DE*ABC*P1"),
                                                RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S1")));

            Assert.That(changed.ChargingStations.Single().Id.ToString(), Is.EqualTo("DE*ABC*S2"));
            Assert.That(changed.EVSEs.Single().Id.ToString(), Is.EqualTo("DE*ABC*E3"));
            Assert.That(source.EVSEs.Count(), Is.EqualTo(2));
            Assert.That(changed.DataSnapshot.Entities.ContainsKey(new(InfrastructureEntityType.EVSE, "DE*ABC*E1")), Is.False);
            Assert.That(changed.DataSnapshot.Entities.ContainsKey(new(InfrastructureEntityType.ChargingConnector, "1", "DE*ABC*E1")), Is.False);

        }

        [Test]
        public void Changes_in_a_batch_observe_previous_operations_in_order()
        {

            var source = Network();
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Json("\"100000 W\""), Json("\"120000 W\"")),
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Json("\"120000 W\""), Json("\"150000 W\""))));

            Assert.That(changed.DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["maxPower"].GetString(), Is.EqualTo("150 kW"));

        }

        [Test]
        public void Failed_later_operation_rolls_back_the_whole_batch()
        {

            var source = Network();
            var snapshot = source.DataSnapshot;
            var set = new RoamingNetworkChangeSet("change-a", source.Id.ToString(), snapshot.Revision, CommitTime, [
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Json("\"100000 W\""), Json("\"120000 W\"")),
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E2", "maxPower", Json("\"999 W\""), Json("\"150000 W\""))], snapshot.ETags, snapshot.ETags);
            var exception = Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(set));

            Assert.That(exception!.OperationIndex, Is.EqualTo(1));
            Assert.That(source.DataSnapshot, Is.SameAs(snapshot));
            Assert.That(source.DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["maxPower"].GetString(), Is.EqualTo("100 kW"));
            Assert.That(source.TryApplyChangeSet(set, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain("Changes[1]"));

        }

        [Test]
        public void Versioned_snapshot_JSON_roundtrip_preserves_revision_and_can_accept_the_next_commit()
        {

            var source = Network();
            var changed = source.ApplyChangeSet(Set(source, RoamingNetworkChange.UpdateProperty("RoamingNetwork", "network-a", "name", null, Json("{\"en\":\"Renamed\"}"))));
            var json = changed.ToJSONSnapshot();
            var restored = RoamingNetwork.Parse(JObject.Parse(json.ToString()));

            Assert.That(restored.Revision, Is.EqualTo(1));
            Assert.That(JToken.DeepEquals(restored.ToJSONSnapshot(), json), Is.True);
            Assert.That(restored.ApplyChangeSet(Set(restored)).Revision, Is.EqualTo(2));

        }

        [Test]
        public void Connector_changes_are_scoped_to_the_explicit_EVSE()
        {

            var source = Network();
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("ChargingConnector", "1", "lockable", Json("false"), Json("true"), "EVSE", "DE*ABC*E1")));

            Assert.That(changed.DataSnapshot.GetEntity(InfrastructureEntityType.ChargingConnector, "1", "DE*ABC*E1").Properties["lockable"].GetBoolean(), Is.True);
            Assert.That(changed.DataSnapshot.GetEntity(InfrastructureEntityType.ChargingConnector, "1", "DE*ABC*E2"),
                                                        Is.SameAs(source.DataSnapshot.GetEntity(InfrastructureEntityType.ChargingConnector, "1", "DE*ABC*E2")));

        }

        [Test]
        public void Detached_dependency_texts_do_not_modify_immutable_entity_data()
        {

            var source = Network();
            var next = source.ApplyChangeSet(Set(source));
            var before = next.ToJSONSnapshot();

            var detachedName = next.Name.ToMutable();
            detachedName.Set(org.GraphDefined.Vanaheimr.Illias.I18NString.Create("Local name"));
            Assert.That(JToken.DeepEquals(next.ToJSONSnapshot(), before), Is.True);
            Assert.That(source.EVSEs.First().PhysicalReference, Is.Null);

            var nextAgain = next.ApplyChangeSet(Set(next));

            Assert.That(nextAgain.EVSEs.All(item => item.PhysicalReference is null), Is.True);

        }

        [Test]
        public void Explicit_JSON_null_can_be_checked_and_cleared_repeatedly()
        {

            var source = Network();
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "physicalReference", null, Json("null")),
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "physicalReference", Json("null"), Json("\"Bay 1\""))));

            Assert.That(changed.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1").PhysicalReference, Is.EqualTo("Bay 1"));
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "physicalReference", Json("null"), Json("\"Bay 1\"")))));

        }

        [TestCase("@id")]
        [TestCase("revision")]
        [TestCase("EVSEs")]
        [TestCase("chargingPoolId")]
        [TestCase("missing")]
        public void Identity_topology_and_unknown_properties_cannot_be_updated(string property)
        {

            var source = Network();

            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("ChargingStation", "DE*ABC*S1", property, null, Json("null")))));

        }

        [Test]
        public void Wrong_revision_wrong_network_and_invalid_property_values_are_rejected()
        {

            var source = Network();

            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(new("c", "network-a", 9, CommitTime, [], source.ETags, source.ETags)));
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(new("c", "another-network", 0, CommitTime, [], source.ETags, source.ETags)));
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", null, Json("-1")))));
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "status", null, Json("\"charging\"")))));

        }

        [Test]
        public void Signed_changes_require_a_successful_verifier()
        {

            var source = Network();
            var set = source.CreateChangeSet("signed", CommitTime, []).WithSignature(new("Ed25519", "key-a", "signature"));

            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(set));
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(set, (_, _) => false));
            Assert.That(source.ApplyChangeSet(set, (_, _) => true).Revision, Is.EqualTo(1));

        }

        [Test]
        public void Streaming_export_matches_the_snapshot_JSON()
        {

            var source = Network();
            using var stream = new MemoryStream();

            using (var writer = new Utf8JsonWriter(stream))
            {
                source.DataSnapshot.WriteTo(writer);
            }

            Assert.That(JsonElement.DeepEquals(Json(System.Text.Encoding.UTF8.GetString(stream.ToArray())), Json(source.ToJSONSnapshot().ToString())), Is.True);

        }

        [Test]
        public void ChangeSet_JSON_roundtrip_preserves_parent_addresses_and_applies()
        {

            var source = Network();
            var set = Set(source, RoamingNetworkChange.Add("EVSE", "DE*ABC*E3",
                                                Json("{\"@id\":\"DE*ABC*E3\",\"currentType\":[\"DC\"]}"), "ChargingStation", "DE*ABC*S1"));
            var restored = JsonSerializer.Deserialize<RoamingNetworkChangeSet>(JsonSerializer.Serialize(set))!;

            Assert.That(restored.Changes[0].ParentEntityType, Is.EqualTo("ChargingStation"));
            Assert.That(source.ApplyChangeSet(restored).EVSEs.Count(), Is.EqualTo(3));

        }

        [Test]
        public void Duplicate_and_mismatched_IDs_missing_parents_and_wrong_parent_types_fail()
        {

            var source = Network();
            var invalid = new[]
                                            {
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E1", Json("{\"@id\":\"DE*ABC*E1\"}"), "ChargingStation", "DE*ABC*S1"),
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E3", Json("{\"@id\":\"DE*ABC*E4\"}"), "ChargingStation", "DE*ABC*S1"),
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E3", Json("{\"@id\":\"DE*ABC*E3\"}"), "ChargingStation", "DE*ABC*S9"),
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E3", Json("{\"@id\":\"DE*ABC*E3\"}"), "ChargingPool", "DE*ABC*P1"),
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E3", Json("{\"@id\":\"DE*ABC*E3\"}")),
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E3", Json("{\"@id\":\"DE*ABC*E3\",\"ignored\":true}"), "ChargingStation", "DE*ABC*S1"),
            RoamingNetworkChange.Add("EVSE", "DE*DEF*E3", Json("{\"@id\":\"DE*DEF*E3\"}"), "ChargingStation", "DE*ABC*S1"),
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E3", Json("{\"@id\":\"DE*ABC*E3\",\"chargingStationId\":\"DE*ABC*S9\"}"), "ChargingStation", "DE*ABC*S1")
        };

            foreach (var change in invalid)
            {
                Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source, change)), change.ToString());
            }

            Assert.That(source.Revision, Is.Zero);

        }

        [Test]
        public void Root_identity_and_existence_are_protected()
        {

            var source = Network();

            foreach (var change in new[] {
            RoamingNetworkChange.Remove("RoamingNetwork", "network-a"),
            RoamingNetworkChange.Add("RoamingNetwork", "network-a", Json("{\"@id\":\"network-a\",\"name\":{}}")),
            RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S9"),
            RoamingNetworkChange.Remove("Unknown", "id")
        })
            {
                Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source, change)));
            }

        }

        [Test]
        public void Remove_precondition_checks_the_entire_subtree()
        {

            var source = Network();
            var previous = Json(source.DataSnapshot.GetEntityJSON(InfrastructureEntityType.ChargingStation, "DE*ABC*S1").ToString());
            var result = source.ApplyChangeSet(Set(source, RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S1", previous)));

            Assert.That(result.EVSEs, Is.Empty);
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S1", Json("{}")))));

        }

        [Test]
        public void A_station_can_be_moved_by_remove_and_add()
        {

            var source = Network();
            var document = Json(source.DataSnapshot.GetEntityJSON(InfrastructureEntityType.ChargingStation, "DE*ABC*S1").ToString());
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.Add("ChargingPool", "DE*ABC*P2", Json("{\"@id\":\"DE*ABC*P2\"}"), "ChargingStationOperator", "DE*ABC"),
                                                RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S1"),
                                                RoamingNetworkChange.Add("ChargingStation", "DE*ABC*S1", document, "ChargingPool", "DE*ABC*P2")));

            Assert.That(changed.ChargingStations.Single().ChargingPool!.Id.ToString(), Is.EqualTo("DE*ABC*P2"));
            Assert.That(changed.EVSEs.Count(), Is.EqualTo(2));
            Assert.That(source.ChargingStations.Single().ChargingPool!.Id.ToString(), Is.EqualTo("DE*ABC*P1"));

        }

        [Test]
        public void Equivalent_domain_ID_spellings_address_the_same_snapshot_entity()
        {

            var source = Network();
            var key = new InfrastructureEntityKey(InfrastructureEntityType.EVSE, "deabcE1");
            var canonical = new InfrastructureEntityKey(InfrastructureEntityType.EVSE, "DE*ABC*E1");

            Assert.That(key, Is.EqualTo(canonical));
            Assert.That(key.GetHashCode(), Is.EqualTo(canonical.GetHashCode()));

            var change = RoamingNetworkChange.UpdateProperty("EVSE", "deabcE1", "maxPower", Json("\"100000 W\""), Json("\"120000 W\""));
            var prepared = source.CreateChangeSet("c", CommitTime, [change]);
            var set = new RoamingNetworkChangeSet("c", "NETWORK-A", 0, CommitTime, [change], prepared.BeforeETags, prepared.AfterETags);

            Assert.That(source.ApplyChangeSet(set).DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1")
                                                               .Properties["maxPower"].GetString(), Is.EqualTo("120 kW"));

        }

        [Test]
        public void Parallel_branches_share_the_base_and_remain_independent()
        {

            var source = Network();
            var data = source.DataSnapshot;
            var versions = new RoamingNetwork[12];

            Parallel.For(0, versions.Length, index =>
                                            {

                                                versions[index] = source.ApplyChangeSet(Set(source,
                                                                                        RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Json("\"100000 W\""), Json(JsonSerializer.Serialize((120000 + index).ToString(System.Globalization.CultureInfo.InvariantCulture) + " W")))));

                                            });

            for (var index = 0; index < versions.Length; index++)
            {
                Assert.That(versions[index].Revision, Is.EqualTo(1));
                Assert.That(versions[index].DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["maxPower"].GetString(),
                            Is.EqualTo(((120000m + index) / 1000m).ToString(System.Globalization.CultureInfo.InvariantCulture) + " kW"));
                Assert.That(versions[index].DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E2"),
                                                                        Is.SameAs(data.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E2")));
            }

        }

        [Test]
        public void Runtime_status_updates_keep_their_timestamp_without_editing_static_data()
        {

            var source = Network();
            var data = source.DataSnapshot;
            var eTags = source.ETags;
            var evse = source.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1");
            var update = new RoamingNetworkRuntimeUpdate(
                             source.Id.ToString(),
                             POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, evse.Id.ToString()),
                             POIRuntimeStatusKind.Status,
                             new POIRuntimeStatusValue("charging", CommitTime.AddDays(-1)),
                             POIRuntimeStatusValue.From(evse.Status),
                             eTags,
                             POIRuntimeUpdateMode.ReplaceHistory);

            source.ApplyRuntimeUpdate(JsonSerializer.Deserialize<RoamingNetworkRuntimeUpdate>(JsonSerializer.Serialize(update))!);

            Assert.That(evse.Status.Value, Is.EqualTo(EVSEStatusType.Parse("charging")));
            Assert.That(evse.Status.Timestamp, Is.EqualTo(CommitTime.AddDays(-1)));
            Assert.That(source.DataSnapshot, Is.SameAs(data));
            Assert.That(data.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties.ContainsKey("status"), Is.False);
            Assert.That(source.Revision, Is.Zero);
            Assert.That(source.ETags, Is.EqualTo(eTags));
            Assert.That(JToken.DeepEquals(RoamingNetwork.Parse(JObject.Parse(source.ToJSONSnapshot().ToString())).ToJSONSnapshot(), source.ToJSONSnapshot()), Is.True);

        }

        [Test]
        public void One_update_in_a_large_station_replaces_only_the_entity_and_four_ancestors()
        {

            var json = new JObject(new JProperty("@id", "network-a"), new JProperty("name", new JObject()));
            var evses = new JArray(Enumerable.Range(1, 1000).Select(index => new JObject(
                                                new JProperty("@id", $"DE*ABC*E{index}"), new JProperty("currentType", new JArray("DC")), new JProperty("maxPower", "100 kW"))));
            var station = new JObject(new JProperty("@id", "DE*ABC*S1"), new JProperty("EVSEs", evses));
            var pool = new JObject(new JProperty("@id", "DE*ABC*P1"), new JProperty("chargingStations", new JArray(station)));
            var op = new JObject(new JProperty("@id", "DE*ABC"), new JProperty("chargingPools", new JArray(pool)));

            json["chargingStationOperators"] = new JArray(op);

            var source = RoamingNetwork.Parse(json);
            var previous = source.DataSnapshot;
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E500", "maxPower", Json("\"100000 W\""), Json("\"120000 W\""))));
            var replacementCount = previous.Entities.Count(entry => !ReferenceEquals(entry.Value, changed.DataSnapshot.Entities[entry.Key]));

            Assert.That(previous.Entities.Count, Is.EqualTo(1004));
            Assert.That(replacementCount, Is.EqualTo(5));

        }

        [Test]
        public void Materializing_one_new_version_concurrently_builds_one_projection_with_correct_links()
        {

            var source = Network();
            var next = source.ApplyChangeSet(Set(source));
            var entities = new EVSE[12];

            Parallel.For(0, entities.Length, index => entities[index] = next.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1"));
            Assert.That(entities.All(entity => ReferenceEquals(entity, entities[0])), Is.True);
            Assert.That(entities[0].RoamingNetwork, Is.SameAs(next));

        }

        [Test]
        public void Adding_and_removing_operators_and_providers_uses_the_network_parent()
        {

            var source = Network();
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.Add("ChargingStationOperator", "DE*DEF", Json("{\"@id\":\"DE*DEF\",\"name\":{\"en\":\"Operator B\"}}")),
                                                RoamingNetworkChange.Add("EMobilityProvider", "DE-GHI", Json("{\"@id\":\"DE-GHI\",\"name\":{\"en\":\"Provider G\"}}")),
                                                RoamingNetworkChange.Remove("ChargingStationOperator", "DE*ABC")));

            Assert.That(changed.ChargingStationOperators.Single().Id.ToString(), Is.EqualTo("DE*DEF"));
            Assert.That(changed.EMobilityProviders.Single().RoamingNetwork, Is.SameAs(changed));
            Assert.That(changed.EVSEs, Is.Empty);

        }

        [Test]
        public void Exported_documents_are_editable_copies_and_update_payloads_outlive_the_input_document()
        {

            var source = Network();
            var export = source.DataSnapshot.GetEntityJSON(InfrastructureEntityType.EVSE, "DE*ABC*E1");

            export["maxPower"] = 1;
            Assert.That(source.DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["maxPower"].GetString(), Is.EqualTo("100 kW"));

            RoamingNetworkChange operation;

            using (var document = JsonDocument.Parse("{\"serial\":\"A1\"}"))
            {
                operation = RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null, document.RootElement);
            }

            var result = source.ApplyChangeSet(Set(source, operation));

            Assert.That(result.DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["customData"].GetProperty("serial").GetString(), Is.EqualTo("A1"));

        }

        [Test]
        public void Connector_add_remove_and_scope_validation_work()
        {

            var source = Network();
            var changed = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.Add("ChargingConnector", "2", Json("{\"@id\":\"2\",\"type\":\"CCS\"}"), "EVSE", "DE*ABC*E1"),
                                                RoamingNetworkChange.Remove("ChargingConnector", "1", null, "EVSE", "DE*ABC*E1")));

            Assert.That(changed.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1").ChargingConnectors.Single().Id.ToString(), Is.EqualTo("2"));
            Assert.That(changed.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E2").ChargingConnectors.Single().Id.ToString(), Is.EqualTo("1"));
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source, RoamingNetworkChange.Remove("ChargingConnector", "1"))));

        }

        [Test]
        public void Stale_change_sets_cannot_be_replayed_and_revision_overflow_is_rejected()
        {

            var source = Network();
            var set = Set(source);
            var changed = source.ApplyChangeSet(set);

            Assert.Throws<RoamingNetworkChangeSetException>(() => changed.ApplyChangeSet(set));

            var json = source.ToJSONSnapshot();

            json["revision"] = long.MaxValue;

            var maximum = RoamingNetwork.Parse(json);

            Assert.Throws<RoamingNetworkChangeSetException>(() => maximum.ApplyChangeSet(Set(maximum)));

        }

        [Test]
        public void Parent_fields_are_validated_during_change_set_JSON_deserialization()
        {

            var operation = """
        {"Kind":"Add","EntityType":"ChargingStation","EntityId":"DE*ABC*S2",
         "NewValue":{"@id":"DE*ABC*S2"},"ParentEntityType":"ChargingPool"}
        """;

            Assert.Throws<ArgumentException>(() => JsonSerializer.Deserialize<RoamingNetworkChange>(operation));

        }
    }
}
