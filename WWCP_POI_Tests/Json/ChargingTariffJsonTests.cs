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

using System.Globalization;
using System.Text.Json;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using cloud.charging.open.protocols.WWCP.POI;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace WWCP_POI_Tests.Json
{
    [TestFixture]
    public class ChargingTariffJsonTests
    {
        private const string TariffDocument = """
            {
              "@id": "DE*ABC*T1",
              "name": {
                "en": "Energy tariff",
                "de": "Ladetarif"
              },
              "description": {
                "en": "Detailed tariff"
              },
              "currency": "EUR",
              "uri": "https://example.org/tariff",
              "dataSource": "tariff-feed",
              "created": "2026-01-01T00:00:00Z",
              "lastChange": "2026-02-01T00:00:00Z",
              "customData": {
                "vendorId": "example.org",
                "counter": 42
              },
              "status": {
                "value": "Operational",
                "timestamp": "2026-02-01T00:00:00Z"
              },
              "adminStatus": {
                "value": "Planned",
                "timestamp": "2026-02-01T00:00:00Z"
              },
              "elements": [
                {
                  "priceComponents": [
                    {
                      "type": "ENERGY",
                      "price": 0.456789,
                      "stepSize": "1 kWh"
                    },
                    {
                      "type": "TIME",
                      "price": 1.2345,
                      "stepSize": "300 s"
                    }
                  ],
                  "restrictions": [
                    {
                      "startTime": "08:00",
                      "endTime": "20:00",
                      "startDate": "2026-01-01T00:00:00Z",
                      "endDate": "2027-01-01T00:00:00Z",
                      "minEnergy": "0.1234567 kWh",
                      "maxEnergy": "42.9876543 kWh",
                      "minPower": "0 W",
                      "maxPower": "350.123 kW",
                      "minDuration": "0.0000001 s",
                      "maxDuration": "3600.25 s",
                      "daysOfWeek": [
                        "MONDAY",
                        "FRIDAY"
                      ]
                    }
                  ]
                }
              ],
              "energyMix": {
                "supplierName": {
                  "en": "Supplier"
                },
                "productName": {
                  "en": "Solar"
                },
                "additionalRemarks": {
                  "de": "Test"
                },
                "energySources": [
                  {
                    "source": "solar",
                    "percentage": "100 %"
                  }
                ],
                "environmentalImpacts": [
                  {
                    "impact": "CO2",
                    "percentage": "0 %"
                  }
                ]
              }
            }
            """;

        private static ChargingStationOperator Operator() => ChargingStationOperator.Parse(JObject.Parse("""
        {"@id":"DE*ABC","name":{}}
        """), new RoamingNetwork(RoamingNetwork_Id.Parse("network-a")));

        internal static RoamingNetwork Network()
        {

            var json = JObject.Parse("""
            {
              "@id": "network-a",
              "name": {},
              "chargingStationOperators": [
                {
                  "@id": "DE*ABC",
                  "name": {},
                  "chargingPools": [
                    {
                      "@id": "DE*ABC*P1",
                      "chargingStations": [
                        {
                          "@id": "DE*ABC*S1",
                          "EVSEs": [
                            {
                              "@id": "DE*ABC*E1",
                              "tariffIds": [
                                "DE*ABC*T1"
                              ],
                              "socketOutlets": [
                                {
                                  "@id": "1",
                                  "type": "CCS",
                                  "tariffIds": [
                                    "DE*ABC*T1"
                                  ]
                                }
                              ]
                            },
                            {
                              "@id": "DE*ABC*E2",
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
            """);

            ((JObject)json["chargingStationOperators"]![0]!)["chargingTariffs"] = new JArray(JObject.Parse(TariffDocument));

            return RoamingNetwork.Parse(json);

        }

        private static JsonElement Json(string text) => JsonSerializer.Deserialize<JsonElement>(text);
        private static RoamingNetworkChangeSet Set(RoamingNetwork                 network,
                                                   params RoamingNetworkChange[]  changes)
            => network.CreateChangeSet("tariff-change", DateTimeOffset.Parse("2026-10-06T12:00:00Z"), [.. changes]);

        [TestCase("de-DE")]
        [TestCase("en-US")]
        [TestCase("fr-FR")]
        public void Price_roundtrip_preserves_decimal_precision_in_every_culture(string culture)
        {

            var previous = CultureInfo.CurrentCulture;

            try
            {

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                var component = ChargingPriceComponent.Energy(0.123456789123456789m, WattHour.FromKWh(1));
                var json = component.ToJSON();

                Assert.That(json["price"]!.Type, Is.EqualTo(JTokenType.Float));

                // Newtonsoft's default reader uses double; use Decimal when reading numeric price tokens.
                using var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(json.ToString())) { FloatParseHandling = Newtonsoft.Json.FloatParseHandling.Decimal };
                var parsed = ChargingPriceComponent.Parse(JObject.Load(reader));

                Assert.That(parsed.Price, Is.EqualTo(component.Price));
                Assert.That(parsed.EnergyStep, Is.EqualTo(WattHour.FromKWh(1)));
                Assert.That(ChargingPriceComponent.Parse(JObject.Parse("""{"type":"FLAT","price":0.123456789123456789}""")).Price,
                                                                Is.EqualTo(component.Price));

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

        }

        [TestCase("{}")]
        [TestCase("{\"type\":\"7\",\"price\":1,\"stepSize\":1}")]
        [TestCase("{\"type\":\"ENERGY\",\"price\":\"0,12\",\"stepSize\":1}")]
        [TestCase("{\"type\":\"ENERGY\",\"price\":true,\"stepSize\":1}")]
        [TestCase("{\"type\":\"ENERGY\",\"price\":1,\"stepSize\":0}")]
        [TestCase("{\"type\":\"ENERGY\",\"price\":1,\"stepSize\":1.5}")]
        [TestCase("{\"type\":\"ENERGY\",\"price\":1,\"stepSize\":4294967296}")]
        public void Invalid_price_components_are_rejected(string json)
        {

            Assert.That(ChargingPriceComponent.TryParse(JObject.Parse(json), out _, out var error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }

        [Test]
        public void Billing_increment_requires_positive_typed_duration()
        {

            Assert.That(ChargingPriceComponent.ChargingTime(1m, TimeSpan.FromMinutes(5)).DurationStep, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(ChargingPriceComponent.ChargingTime(1m, TimeSpan.FromMilliseconds(500)).DurationStep, Is.EqualTo(TimeSpan.FromMilliseconds(500)));
            Assert.Throws<ArgumentException>(() => ChargingPriceComponent.ParkingTime(1m, TimeSpan.Zero));
            Assert.Throws<ArgumentException>(() => ChargingPriceComponent.ChargingTime(1m, TimeSpan.FromSeconds(-1)));

        }

        [Test]
        public void All_restrictions_roundtrip_including_dates_and_fractional_seconds()
        {

            var input = (JObject)JObject.Parse(TariffDocument)["elements"]![0]!["restrictions"]![0]!;
            var restriction = ChargingTariffRestriction.Parse(input);
            var output = restriction.ToJSON();
            var restored = ChargingTariffRestriction.Parse(JObject.Parse(output.ToString()));

            Assert.That(restored.Energy!.Value.Min, Is.EqualTo(WattHour.FromKWh(0.1234567m)));
            Assert.That(restored.Power!.Value.Max, Is.EqualTo(Watt.FromKW(350.123m)));
            Assert.That(restored.Duration!.Value.Min!.Value.Ticks, Is.EqualTo(1));
            Assert.That(restored.Duration!.Value.Max, Is.EqualTo(TimeSpan.FromMilliseconds(3600250)));
            Assert.That(restored.Date!.EndTime, Is.EqualTo(DateTimeOffset.Parse("2027-01-01T00:00:00Z")));
            Assert.That(restored.DayOfWeek, Is.EqualTo(new[] { DayOfWeek.Monday, DayOfWeek.Friday }));
            Assert.That(JToken.DeepEquals(output, restored.ToJSON()), Is.True);

        }

        [TestCase("{}")]
        [TestCase("{\"minkWh\":-1}")]
        [TestCase("{\"minPower\":20,\"maxPower\":10}")]
        [TestCase("{\"minDuration\":-1}")]
        [TestCase("{\"minDuration\":0.00000001}")]
        [TestCase("{\"endDate\":\"2027-01-01\"}")]
        [TestCase("{\"startDate\":\"2027-01-01\",\"endDate\":\"2026-01-01\"}")]
        [TestCase("{\"daysOfWeek\":[\"8\"]}")]
        [TestCase("{\"startTime\":\"invalid\"}")]
        [TestCase("{\"unknown\":1}")]
        public void Invalid_restrictions_are_rejected(string json)
        {

            Assert.That(ChargingTariffRestriction.TryParse(JObject.Parse(json), out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Empty);

        }

        [Test]
        public void Component_and_restriction_inputs_are_copied()
        {

            var components = new[] { ChargingPriceComponent.FlatRate(1m) };
            var weekdays = new[] { DayOfWeek.Monday };
            var date = new StartEndDateTime(DateTimeOffset.Parse("2026-01-01Z"), DateTimeOffset.Parse("2027-01-01Z"));
            var restriction = new ChargingTariffRestriction(Date: date, DayOfWeek: weekdays);
            var restrictions = new[] { restriction };
            var element = new ChargingTariffElement(components, restrictions);

            components[0] = ChargingPriceComponent.FlatRate(2m);
            restrictions[0] = ChargingTariffRestriction.MinEnergy(WattHour.FromKWh(99m));
            weekdays[0] = DayOfWeek.Friday;
            date.EndTime = null;
            restriction.Date!.EndTime = null;
            Assert.That(element.ChargingPriceComponents.Single().Price, Is.EqualTo(1m));
            Assert.That(element.ChargingTariffRestrictions.Single().DayOfWeek.Single(), Is.EqualTo(DayOfWeek.Monday));
            Assert.That(restriction.Date!.EndTime, Is.Not.Null);

        }

        [Test]
        public void Standalone_tariff_parser_does_not_register_with_the_parent()
        {

            var op = Operator();
            var tariff = ChargingTariff.Parse(JObject.Parse(TariffDocument), op);
            var parsed = ChargingTariff.Parse(tariff.ToJSON(), op);

            Assert.That(parsed.Currency.ISOCode, Is.EqualTo("EUR"));
            Assert.That(parsed.TariffElements.Single().ChargingPriceComponents.First().Price, Is.EqualTo(0.456789m));
            Assert.That(parsed.EnergyMix, Is.EqualTo(tariff.EnergyMix));
            Assert.That(parsed.Operator, Is.SameAs(op));
            Assert.That(op.ChargingTariffs, Is.Empty);

        }

        [TestCase("currency", "USDXXX")]
        [TestCase("@id", "DE*DEF*T1")]
        [TestCase("@context", "https://wrong.example/")]
        [TestCase("uri", "invalid")]
        public void Invalid_tariff_fields_are_rejected(string  field,
                                                       string  value)
        {

            var json = JObject.Parse(TariffDocument);

            json[field] = value;
            Assert.That(ChargingTariff.TryParse(json, Operator(), out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Empty);

        }

        [Test]
        public void Missing_or_empty_elements_and_bad_nested_data_are_rejected()
        {

            foreach (var elements in new[] { new JArray(), new JArray(new JObject()), new JArray(JObject.Parse("""{"priceComponents":[{"type":"ENERGY","price":1,"stepSize":0}]}""")) })
            {
                var json = JObject.Parse(TariffDocument);

                json["elements"] = elements;
                Assert.That(ChargingTariff.TryParse(json, Operator(), out _, out _), Is.False);
            }

        }

        [Test]
        public void Network_snapshot_preserves_tariffs_metadata_and_assignments()
        {

            var source = Network();
            var snapshot = source.ToJSONSnapshot();
            var restored = RoamingNetwork.Parse(snapshot);
            var tariff = restored.ChargingTariffs.Single();

            Assert.That(tariff.DataSource, Is.EqualTo("tariff-feed"));
            Assert.That(tariff.Created, Is.EqualTo(DateTimeOffset.Parse("2026-01-01T00:00:00Z")));
            Assert.That(tariff.LastChangeDate, Is.EqualTo(DateTimeOffset.Parse("2026-02-01T00:00:00Z")));
            Assert.That(tariff.Status.Value, Is.EqualTo(ChargingTariffStatusTypes.Operational));
            Assert.That(tariff.AdminStatus.Value, Is.EqualTo(ChargingTariffAdminStatusTypes.Planned));
            Assert.That(tariff.CustomData.ToJObject()["counter"]!.Value<int>(), Is.EqualTo(42));
            Assert.That(restored.EVSEs.Single(evse => evse.Id.ToString() == "DE*ABC*E1").ChargingTariffs.Single(), Is.SameAs(tariff));
            Assert.That(JToken.DeepEquals(snapshot, restored.ToJSONSnapshot()), Is.True);
            Assert.That(source.DataSnapshot.TariffReferences.Single().Value.Count, Is.EqualTo(2));

        }

        [Test]
        public void Tariff_price_changes_share_the_entire_station_subtree()
        {

            var source = Network();
            var data = source.DataSnapshot;
            var json = data.GetEntityJSON(InfrastructureEntityType.ChargingTariff, "DE*ABC*T1");
            var elements = json["elements"]!.DeepClone();

            elements[0]!["priceComponents"]![0]!["price"] = 0.567890123m;

            var changed = source.ApplyChangeSet(Set(source, RoamingNetworkChange.UpdateProperty("ChargingTariff", "DE*ABC*T1", "elements",
                                                Json(json["elements"]!.ToString()), Json(elements.ToString()))));

            Assert.That(changed.ChargingTariffs.Single().TariffElements.Single().ChargingPriceComponents.First().Price, Is.EqualTo(0.567890123m));
            Assert.That(source.ChargingTariffs.Single().TariffElements.Single().ChargingPriceComponents.First().Price, Is.EqualTo(0.456789m));
            Assert.That(changed.DataSnapshot.GetEntity(InfrastructureEntityType.ChargingPool, "DE*ABC*P1"), Is.SameAs(data.GetEntity(InfrastructureEntityType.ChargingPool, "DE*ABC*P1")));
            Assert.That(changed.DataSnapshot.TariffReferences, Is.SameAs(data.TariffReferences));
            Assert.That(changed.ChargingTariffs.Single().RoamingNetwork, Is.SameAs(changed));
            Assert.That(changed.EVSEs.Single(evse => evse.Id.ToString() == "DE*ABC*E1").ChargingTariffs.Single(), Is.SameAs(changed.ChargingTariffs.Single()));

        }

        [Test]
        public void Add_assign_unassign_and_remove_tariff_in_order()
        {

            var source = Network();
            var added = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.Add("ChargingTariff", "DE*ABC*T2", Json("""{"@id":"DE*ABC*T2","currency":"EUR","elements":[{"priceComponents":[{"type":"FLAT","price":2.12345}]}]}"""), "ChargingStationOperator", "DE*ABC"),
                                                RoamingNetworkChange.UpdateProperty("ChargingConnector", "1", "tariffIds", null, Json("[\"DE*ABC*T2\"]"), "EVSE", "DE*ABC*E2")));

            Assert.That(added.ChargingTariffs.Count(), Is.EqualTo(2));
            Assert.That(added.DataSnapshot.TariffReferences.Count, Is.EqualTo(2));

            var removed = added.ApplyChangeSet(Set(added,
                                                RoamingNetworkChange.UpdateProperty("ChargingConnector", "1", "tariffIds", Json("[\"DE*ABC*T2\"]"), Json("[]"), "EVSE", "DE*ABC*E2"),
                                                RoamingNetworkChange.Remove("ChargingTariff", "DE*ABC*T2")));

            Assert.That(removed.ChargingTariffs.Count(), Is.EqualTo(1));
            Assert.That(removed.DataSnapshot.TariffReferences.Count, Is.EqualTo(1));
            Assert.That(added.ChargingTariffs.Count(), Is.EqualTo(2));
            Assert.That(RoamingNetwork.Parse(removed.ToJSONSnapshot()).Revision, Is.EqualTo(2));

        }

        [Test]
        public void Referenced_tariff_cannot_be_removed_and_batch_is_atomic()
        {

            var source = Network();
            var data = source.DataSnapshot;
            var exception = Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("ChargingTariff", "DE*ABC*T1", "currency", Json("\"EUR\""), Json("\"USD\"")),
                                                RoamingNetworkChange.Remove("ChargingTariff", "DE*ABC*T1"))));

            Assert.That(exception!.OperationIndex, Is.EqualTo(1));
            Assert.That(exception.Message, Does.Contain("still referenced"));
            Assert.That(source.DataSnapshot, Is.SameAs(data));
            Assert.That(source.ChargingTariffs.Single().Currency.ISOCode, Is.EqualTo("EUR"));

        }

        [Test]
        public void Removing_operator_also_removes_assignments_and_tariffs()
        {

            var source = Network();
            var removed = source.ApplyChangeSet(Set(source, RoamingNetworkChange.Remove("ChargingStationOperator", "DE*ABC")));

            Assert.That(removed.DataSnapshot.Entities.Count, Is.EqualTo(1));
            Assert.That(removed.DataSnapshot.TariffReferences, Is.Empty);
            Assert.That(removed.ChargingTariffs, Is.Empty);
            Assert.That(source.DataSnapshot.TariffReferences.Single().Value.Count, Is.EqualTo(2));

        }

        [Test]
        public void Dangling_and_foreign_tariff_assignments_are_rejected()
        {

            var source = Network();

            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E2", "tariffIds", null, Json("[\"DE*ABC*T99\"]")))));

            var json = source.ToJSONSnapshot();

            json["chargingStationOperators"]![0]!["chargingTariffs"] = new JArray();
            Assert.That(RoamingNetwork.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("not registered"));

        }

        [Test]
        public void ID_only_tariffs_require_a_resolver_and_are_owned_by_the_new_operator()
        {

            var document = JObject.Parse("""{"@id":"DE*ABC","name":{},"chargingTariffIds":["DE*ABC*T1"]}""");
            var network = new RoamingNetwork(RoamingNetwork_Id.Parse("network-a"));

            Assert.That(ChargingStationOperator.TryParse(document, network, out _, out _), Is.False);

            var op = ChargingStationOperator.Parse(document, network, Context: new InfrastructureJsonParsingContext
            {
                ResolveChargingTariff = _ => JObject.Parse(TariffDocument)
            });

            Assert.That(op.ChargingTariffs.Single().Operator, Is.SameAs(op));
            Assert.That(ChargingStationOperator.Parse(op.ToJSON(ExpandRoamingNetworkId: InfoStatus.Hidden), network).ChargingTariffs.Count(), Is.EqualTo(1));

        }

        [Test]
        public void Decimal_text_parser_preserves_full_price_precision_through_changes_and_reload()
        {

            const decimal price = 0.123456789123456789123456789m;
            var tariff = ChargingTariff.Parse(TariffDocument.Replace("0.456789", "0.123456789123456789123456789"), Operator());

            Assert.That(tariff.TariffElements.Single().ChargingPriceComponents.First().Price, Is.EqualTo(price));

            var network = Network();
            var next = network.ApplyChangeSet(Set(network, RoamingNetworkChange.UpdateProperty("ChargingTariff", "DE*ABC*T1", "elements", null,
                                                Json("""[{"priceComponents":[{"type":"ENERGY","price":0.123456789123456789123456789,"stepSize":"1 Wh"}]}]"""))));
            var document = next.ToJSONSnapshot().ToString();

            Assert.That(document, Does.Contain("0.123456789123456789123456789"));

            var restored = RoamingNetwork.Parse(document);

            Assert.That(restored.Revision, Is.EqualTo(1));
            Assert.That(restored.ChargingTariffs.Single().TariffElements.Single().ChargingPriceComponents.First().Price, Is.EqualTo(price));
            Assert.That(RoamingNetwork.TryParse(document, out var parsed, out _), Is.True);
            Assert.That(parsed!.DataSnapshot.GetEntity(InfrastructureEntityType.ChargingTariff, "DE*ABC*T1").Properties["elements"][0].GetProperty("priceComponents")[0].GetProperty("price").GetDecimal(), Is.EqualTo(price));

        }

        [Test]
        public void Cross_operator_assignments_are_rejected_without_changing_the_index()
        {

            var source = Network();
            var added = source.ApplyChangeSet(Set(source, RoamingNetworkChange.Add("ChargingStationOperator", "DE*DEF", Json("""
            {"@id":"DE*DEF","name":{},"chargingTariffs":[{"@id":"DE*DEF*T1","currency":"EUR","elements":[{"priceComponents":[{"type":"FLAT","price":1}]}]}]}
            """))));
            var index = added.DataSnapshot.TariffReferences;
            var exception = Assert.Throws<RoamingNetworkChangeSetException>(() => added.ApplyChangeSet(Set(added,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E2", "tariffIds", null, Json("[\"DE*DEF*T1\"]")))));

            Assert.That(exception!.Message, Does.Contain("different operator"));
            Assert.That(added.DataSnapshot.TariffReferences, Is.SameAs(index));

        }

        [Test]
        public void Removing_a_referencing_EVSE_cleans_both_EVSE_and_connector_assignments()
        {

            var source = Network();
            var next = source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.Remove("EVSE", "DE*ABC*E1"),
                                                RoamingNetworkChange.Remove("ChargingTariff", "DE*ABC*T1")));

            Assert.That(next.DataSnapshot.TariffReferences, Is.Empty);
            Assert.That(next.ChargingTariffs, Is.Empty);
            Assert.That(next.EVSEs.Single().Id.ToString(), Is.EqualTo("DE*ABC*E2"));

        }

        [Test]
        public void Invalid_tariff_update_and_stale_price_precondition_are_atomic_conflicts()
        {

            var source = Network();

            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("ChargingTariff", "DE*ABC*T1", "elements", null, Json("[]")))));
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("ChargingTariff", "DE*ABC*T1", "currency", Json("\"USD\""), Json("\"EUR\"")))));
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("ChargingTariff", "DE*ABC*T1", "status", null,
                                                    Json("""{"value":"999","timestamp":"2026-10-01T00:00:00Z"}""")))));
            Assert.That(source.Revision, Is.Zero);

        }

        [Test]
        public void Energy_mix_unknown_and_full_composition_roundtrip()
        {

            var full = EnergyMix.Parse((JObject)JObject.Parse(TariffDocument)["energyMix"]!);

            Assert.That(EnergyMix.Parse(full.ToJSON()), Is.EqualTo(full));

            var unknown = EnergyMix.Parse(JObject.Parse("""{"supplierName":{"en":"Supplier"},"productName":{"en":"Unknown composition"},"energySources":[],"environmentalImpacts":[]}"""));

            Assert.That(unknown.EnergySources, Is.Empty);
            Assert.That(unknown.EnvironmentalImpacts, Is.Empty);
            Assert.That(EnergyMix.TryParse(JObject.Parse("""{"energySources":[{"source":"solar","percent":101}]}"""), out _, out _), Is.False);

        }

        [Test]
        public void Duplicate_tariffs_and_equivalent_tariff_references_are_rejected()
        {

            var source = Network();
            var json = source.ToJSONSnapshot();
            var tariffs = (JArray)json["chargingStationOperators"]![0]!["chargingTariffs"]!;
            var duplicate = (JObject)tariffs[0]!.DeepClone();

            duplicate["@id"] = "DEABCT1";
            tariffs.Add(duplicate);
            Assert.That(RoamingNetwork.TryParse(json, out _, out _), Is.False);
            Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source,
                                                RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E2", "tariffIds", null, Json("[\"DE*ABC*T1\",\"DEABCT1\"]")))));
            Assert.That(ChargingConnector.TryParse(JObject.Parse("""{"@id":"1","type":"CCS","tariffIds":["DE*ABC*T1","DEABCT1"]}"""), out _, out _), Is.False);

        }

        [Test]
        public void Equivalent_tariff_IDs_have_equal_hashes_and_sort_as_equal()
        {

            var first = ChargingTariff_Id.Parse("DE*ABC*Tab*c");
            var equivalent = ChargingTariff_Id.Parse("DEABCTABC");

            Assert.That(first, Is.EqualTo(equivalent));
            Assert.That(first.GetHashCode(), Is.EqualTo(equivalent.GetHashCode()));
            Assert.That(first.CompareTo(equivalent), Is.Zero);
            Assert.That(new HashSet<ChargingTariff_Id> { first, equivalent }.Count, Is.EqualTo(1));

        }

        [Test]
        public void Immutable_operator_import_and_lookup_use_tariff_ID_equality()
        {

            var op = Operator();
            var json = JObject.Parse(TariffDocument);

            json["@id"] = "DE*ABC*Tab*c";

            var imported = ChargingStationOperator.Parse(new JObject(
                new JProperty("@id", op.Id.ToString()),
                new JProperty("chargingTariffs", new JArray(json))), op.RoamingNetwork);
            var tariff = imported.ChargingTariffs.Single();
            Assert.That(imported.GetChargingTariff(ChargingTariff_Id.Parse("DEABCTABC")), Is.SameAs(tariff));

        }

        [Test]
        public void Operator_tariff_queries_follow_assignments_and_connector_scope()
        {

            var network = Network();
            var op = network.ChargingStationOperators.Single();
            var tariff = op.ChargingTariffs.Single();

            Assert.That(op.GetChargingTariffs().Single(), Is.SameAs(tariff));
            Assert.That(op.GetChargingTariffs(ChargingPoolId: ChargingPool_Id.Parse("DE*ABC*P1")).Single(), Is.SameAs(tariff));
            Assert.That(op.GetChargingTariffs(ChargingStationId: ChargingStation_Id.Parse("DE*ABC*S1")).Single(), Is.SameAs(tariff));
            Assert.That(op.GetChargingTariffs(EVSEId: EVSE_Id.Parse("DE*ABC*E2")), Is.Empty);
            Assert.That(op.GetChargingTariffs(EVSEId: EVSE_Id.Parse("DE*ABC*E1"), ChargingConnectorId: ChargingConnector_Id.Parse("1")).Single(), Is.SameAs(tariff));
            Assert.That(op.GetChargingTariffs(EVSEId: EVSE_Id.Parse("DE*ABC*E1"), ChargingConnectorId: ChargingConnector_Id.Parse("2")), Is.Empty);
            Assert.That(op.GetChargingTariffIds(EVSEId: EVSE_Id.Parse("DE*ABC*E1")).Single(), Is.EqualTo(tariff.Id));
            Assert.Throws<ArgumentException>(() => op.GetChargingTariffs(ChargingConnectorId: ChargingConnector_Id.Parse("1")));

        }

        [Test]
        public void Embedded_tariff_JSON_preserves_custom_data_source_and_brand()
        {

            var document = JObject.Parse(TariffDocument);

            document["brand"] = JObject.Parse("""{"id":"brand-a","name":{"en":"Brand"}}""");

            var op = Operator();
            var tariff = ChargingTariff.Parse(document, op);
            var json = tariff.ToJSON(Embedded: true, ExpandBrandIds: InfoStatus.Expanded);
            var restored = ChargingTariff.Parse(json, op);

            Assert.That(restored.DataSource, Is.EqualTo("tariff-feed"));
            Assert.That(restored.CustomData.ToJObject()["counter"]!.Value<int>(), Is.EqualTo(42));
            Assert.That(restored.Brand!.Id, Is.EqualTo(tariff.Brand!.Id));
            Assert.That(tariff.ToJSON(ExpandBrandIds: InfoStatus.Hidden)["brandId"], Is.Null);
            Assert.That(tariff.ToJSON(ExpandBrandIds: InfoStatus.Hidden)["brand"], Is.Null);

            var idOnly = tariff.ToJSON();

            Assert.That(ChargingTariff.TryParse(idOnly, op, out _, out _), Is.False);

            var resolved = ChargingTariff.Parse(idOnly, op, Context: new InfrastructureJsonParsingContext
            {
                ResolveBrand = _ => (JObject)document["brand"]!.DeepClone()
            });

            Assert.That(resolved.Brand!.Id, Is.EqualTo(tariff.Brand.Id));

            var networkDocument = Network().ToJSONSnapshot();

            networkDocument["chargingStationOperators"]![0]!["chargingTariffs"]![0]!["brand"] = document["brand"]!.DeepClone();

            var network = RoamingNetwork.Parse(networkDocument);
            var restoredNetwork = RoamingNetwork.Parse(network.ToJSONSnapshot());

            Assert.That(restoredNetwork.ChargingTariffs.Single().Brand!.Id, Is.EqualTo(tariff.Brand.Id));
            Assert.That(network.DataSnapshot.GetEntity(InfrastructureEntityType.ChargingTariff, "DE*ABC*T1").Properties.ContainsKey("brand"), Is.True);

        }
    }
}
