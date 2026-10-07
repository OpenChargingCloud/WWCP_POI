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

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

using cloud.charging.open.protocols.WWCP.POI;

#endregion

namespace WWCP_POI_Tests.Json
{
    [TestFixture]
    public sealed class InfrastructureHierarchyJsonTests
    {
        private static JObject Snapshot() => JObject.Parse("""
            {
              "@id": "network-a",
              "name": {
                "en": "Test network"
              },
              "chargingStationOperators": [
                {
                  "@id": "DE*ABC",
                  "name": {
                    "en": "Operator ABC"
                  },
                  "chargingPools": [
                    {
                      "@id": "DE*ABC*P1",
                      "name": {
                        "en": "Pool 1"
                      },
                      "chargingStations": [
                        {
                          "@id": "DE*ABC*S1",
                          "name": {
                            "en": "Station 1"
                          },
                          "EVSEs": [
                            {
                              "@id": "DE*ABC*E1",
                              "currentType": [
                                "DC"
                              ],
                              "maxPower": "150000.125 W",
                              "socketOutlets": [
                                {
                                  "@id": "1",
                                  "type": "CCS"
                                }
                              ]
                            },
                            {
                              "@id": "DE*ABC*E2",
                              "currentType": [
                                "AC_ThreePhases"
                              ]
                            }
                          ]
                        },
                        {
                          "@id": "DE*ABC*S2",
                          "EVSEs": [
                            {
                              "@id": "DE*ABC*E3",
                              "currentType": [
                                "DC"
                              ]
                            }
                          ]
                        }
                      ]
                    }
                  ]
                },
                {
                  "@id": "DE*DEF",
                  "name": {
                    "en": "Operator DEF"
                  },
                  "chargingPools": [
                    {
                      "@id": "DE*DEF*P1",
                      "chargingStations": [
                        {
                          "@id": "DE*DEF*S1",
                          "EVSEs": [
                            {
                              "@id": "DE*DEF*E1",
                              "currentType": [
                                "DC"
                              ]
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
              ],
              "eMobilityProviders": [
                {
                  "@id": "DE-GHI",
                  "name": {
                    "en": "Provider GHI"
                  },
                  "dataSource": "provider-data"
                }
              ]
            }
            """);

        private static JObject Expanded(RoamingNetwork network) => network.ToJSON(
            ExpandChargingStationOperatorIds: InfoStatus.Expanded,
            ExpandChargingPoolIds: InfoStatus.Expanded, ExpandChargingStationIds: InfoStatus.Expanded,
            ExpandEVSEIds: InfoStatus.Expanded, ExpandBrandIds: InfoStatus.Expanded,
            ExpandDataLicenses: InfoStatus.Expanded, ExpandEMobilityProviderId: InfoStatus.Expanded);

        [Test]
        public void Entire_network_roundtrip_preserves_children_and_parent_identity()
        {

            var input = Snapshot();
            var before = input.DeepClone();
            var network = RoamingNetwork.Parse(input);

            Assert.That(network.ChargingStationOperators.Count(), Is.EqualTo(2));
            Assert.That(network.ChargingPools.Count(), Is.EqualTo(2));
            Assert.That(network.ChargingStations.Count(), Is.EqualTo(3));
            Assert.That(network.EVSEs.Count(), Is.EqualTo(4));

            var json = Expanded(network);

            Assert.That(json.ToString(), Does.Not.Contain("exception"));

            var parsed = RoamingNetwork.Parse(JObject.Parse(json.ToString()));

            Assert.That(parsed.EVSEs.Count(), Is.EqualTo(4));

            foreach (var evse in parsed.EVSEs)
            {
                Assert.That(evse.RoamingNetwork, Is.SameAs(parsed));
                Assert.That(evse.ChargingStation!.EVSEs, Does.Contain(evse));
                Assert.That(evse.ChargingPool!.ChargingStations, Does.Contain(evse.ChargingStation));
            }

            Assert.That(parsed.EMobilityProviders.Single().RoamingNetwork, Is.SameAs(parsed));
            Assert.That(parsed.EMobilityProviders.Single().DataSource, Is.EqualTo("provider-data"));
            Assert.That(JToken.DeepEquals(Expanded(parsed), json), Is.True, json.ToString());
            Assert.That(JToken.DeepEquals(input, before), Is.True);

        }

        [Test]
        public void Deep_error_reports_the_path_and_returns_no_network()
        {

            var input = Snapshot();

            input["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["maxPower"] = "invalid";
            Assert.That(RoamingNetwork.TryParse(input, out var network, out var error), Is.False);
            Assert.That(network, Is.Null);
            Assert.That(error, Does.Contain("chargingStationOperators[0]").And.Contain("chargingPools[0]")
                                                                  .And.Contain("chargingStations[0]").And.Contain("EVSEs[0]").And.Contain("maxPower"));

        }

        [Test]
        public void Standalone_station_without_location_serializes_and_parses()
        {

            var json = EVSEJsonTests.Station().ToJSON();

            Assert.That(json["exception"], Is.Null, json.ToString());
            Assert.That(JToken.DeepEquals(ChargingStation.Parse(json).ToJSON(), json), Is.True);

        }

        [Test]
        public void Snapshot_preserves_statuses_timestamps_custom_data_and_license_metadata()
        {

            var input = Snapshot();

            input["created"] = "2020-01-02T03:04:05+00:00";
            input["lastChange"] = "2026-01-02T03:04:05+00:00";
            input["customData"] = JObject.Parse("{\"source\":{\"revision\":12}}");

            var evse = (JObject)input["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;

            evse["status"] = JObject.Parse("{\"value\":\"charging\",\"timestamp\":\"2024-05-06T07:08:09+00:00\"}");
            evse["adminStatus"] = JObject.Parse("{\"value\":\"operational\",\"timestamp\":\"2024-05-01T00:00:00+00:00\"}");
            evse["customData"] = JObject.Parse("{\"serial\":\"A1\"}");
            evse["dataLicenses"] = JArray.Parse("[{\"@id\":\"license-a\",\"description\":{\"en\":\"License terms\"},\"URLs\":[\"https://example.org/license\"]}]");

            var network = RoamingNetwork.Parse(input);
            var json = network.ToJSONSnapshot();

            Assert.That(json.ToString(), Does.Not.Contain("exception"));

            var parsed = RoamingNetwork.Parse(JObject.Parse(json.ToString()));

            Assert.That(JToken.DeepEquals(parsed.ToJSONSnapshot(), json), Is.True, parsed.ToJSONSnapshot().ToString());

            var parsedEVSE = parsed.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1");

            Assert.That(parsedEVSE.Status.Value, Is.EqualTo(EVSEStatusType.Parse("charging")));
            Assert.That(parsedEVSE.Status.Timestamp.Year, Is.EqualTo(2024));
            Assert.That(parsedEVSE.DataLicenses.Single().URLs.Count(), Is.EqualTo(1));
            Assert.That(parsed.Created.Year, Is.EqualTo(2020));

        }

        [Test]
        public void Id_only_network_can_be_resolved_into_a_fresh_hierarchy()
        {

            var source = RoamingNetwork.Parse(Snapshot());
            var snapshot = source.ToJSONSnapshot();
            var documents = snapshot.Descendants().OfType<JObject>()
                                                .Where(document => document["@id"] is not null)
                                                .ToDictionary(document => document["@id"]!.Value<string>()!);
            var before = snapshot.DeepClone();
            var context = new InfrastructureJsonParsingContext
            {
                ResolveChargingStationOperator = id => documents[id.ToString()],
                ResolveEMobilityProvider = id => documents[id.ToString()]
            };
            var parsed = RoamingNetwork.Parse(source.ToJSON(), Context: context);

            Assert.That(parsed.EVSEs.Count(), Is.EqualTo(4));
            Assert.That(parsed.EVSEs.First(), Is.Not.SameAs(source.EVSEs.First()));
            Assert.That(parsed.EVSEs.All(evse => ReferenceEquals(evse.RoamingNetwork, parsed)), Is.True);
            Assert.That(JToken.DeepEquals(snapshot, before), Is.True, "Resolver documents must remain unchanged.");

        }

        [Test]
        public void References_at_every_child_level_are_resolved()
        {

            var input = Snapshot();
            var documents = input.Descendants().OfType<JObject>().Where(document => document["@id"] is not null)
                                                .ToDictionary(document => document["@id"]!.Value<string>()!, document => (JObject)document.DeepClone());

            foreach (var document in documents.Values)
            {

                foreach (var pair in new[] { ("chargingPools", "chargingPoolIds"), ("chargingStations", "chargingStationIds"), ("EVSEs", "EVSEIds") })
                {

                    if (document[pair.Item1] is JArray children)
                    {
                        document[pair.Item2] = new JArray(children.Select(child => child["@id"]!.Value<string>()));
                        document.Remove(pair.Item1);
                    }

                }

            }

            input.Remove("chargingStationOperators");
            input["chargingStationOperatorIds"] = new JArray("DE*ABC", "DE*DEF");

            var context = new InfrastructureJsonParsingContext
            {
                ResolveChargingStationOperator = id => documents[id.ToString()],
                ResolveChargingPool = id => documents[id.ToString()],
                ResolveChargingStation = id => documents[id.ToString()],
                ResolveEVSE = id => documents[id.ToString()]
            };

            Assert.That(RoamingNetwork.Parse(input, Context: context).EVSEs.Count(), Is.EqualTo(4));

        }

        [TestCase("DE*ABC")]
        [TestCase("DE*DEF")]
        public void Missing_or_mismatched_resolver_document_fails(string returnedId)
        {

            var input = JObject.Parse("{\"@id\":\"network-a\",\"name\":{},\"chargingStationOperatorIds\":[\"DE*ABC\"]}");
            var context = new InfrastructureJsonParsingContext
            {
                ResolveChargingStationOperator = _ => returnedId == "DE*ABC" ? null : new JObject(new JProperty("@id", returnedId))
            };

            Assert.That(RoamingNetwork.TryParse(input, out var result, out var error, null, context), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain("chargingStationOperatorIds[0]"));

        }

        [Test]
        public void Duplicate_EVSE_across_stations_is_rejected()
        {

            var input = Snapshot();

            input["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![1]!["EVSEs"]![0]!["@id"] = "DE*ABC*E1";
            Assert.That(RoamingNetwork.TryParse(input, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain("duplicate identifier"));

        }

        [Test]
        public void Nonembedded_EVSE_accepts_matching_inherited_station_data()
        {

            var station = EVSEJsonTests.Station();
            var evse = new EVSE(EVSE_Id.Parse("DE*ABC*E1"), station);
            var json = evse.ToJSON()!;

            Assert.That(EVSE.TryParse(json, station, out var parsed, out var error), Is.True, error);
            Assert.That(JToken.DeepEquals(parsed!.ToJSON(), json), Is.True);
            json["openingTimes"] = new JObject(new JProperty("24/7", false));
            Assert.That(EVSE.TryParse(json, station, out _, out error), Is.False);
            Assert.That(error, Does.Contain("openingTimes"));

        }

        [Test]
        public void Regular_and_exceptional_opening_times_roundtrip()
        {

            var input = Snapshot();
            var pool = (JObject)input["chargingStationOperators"]![0]!["chargingPools"]![0]!;

            pool["openingTimes"] = JObject.Parse("""
            {
              "24/7": false,
              "regularOpenings": [
                {
                  "monday": [
                    {
                      "begin": "08:00",
                      "end": "18:00"
                    }
                  ]
                }
              ],
              "exceptionalOpenings": [
                "2026-12-24T08:00:00Z -> 2026-12-24T12:00:00Z"
              ],
              "exceptionalClosings": [
                "2026-12-25T00:00:00Z -> 2026-12-26T00:00:00Z"
              ],
              "freeText": "Holiday schedule"
            }
            """);

            var json = Expanded(RoamingNetwork.Parse(input));

            Assert.That(JToken.DeepEquals(Expanded(RoamingNetwork.Parse(json)), json), Is.True);

        }

        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        public void Coordinates_and_altitude_roundtrip_at_all_levels(string culture)
        {

            var previous = CultureInfo.CurrentCulture;

            try
            {

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                var input = Snapshot();
                var pool = (JObject)input["chargingStationOperators"]![0]!["chargingPools"]![0]!;
                var station = (JObject)pool["chargingStations"]![0]!;
                var evse = (JObject)station["EVSEs"]![0]!;

                pool["geoLocation"] = JObject.Parse("{\"lat\":52.125,\"lng\":13.625,\"alt\":\"25.125 m\"}");
                station["geoLocation"] = JObject.Parse("{\"lat\":52.126,\"lng\":13.626,\"alt\":\"26.125 m\"}");
                evse["geoLocation"] = JObject.Parse("{\"lat\":52.127,\"lng\":13.627,\"alt\":\"27.125 m\"}");

                var json = Expanded(RoamingNetwork.Parse(input));
                var parsed = RoamingNetwork.Parse(JObject.Parse(json.ToString()));

                Assert.That(JToken.DeepEquals(Expanded(parsed), json), Is.True);
                Assert.That(parsed.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1").GeoLocation!.Value.Altitude!.Value.Value,
                                                                        Is.EqualTo(27.125));

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

        }

        [TestCase("chargingStationOperatorIds")]
        [TestCase("chargingPoolIds")]
        [TestCase("chargingStationIds")]
        [TestCase("EVSEIds")]
        public void Unknown_top_level_references_are_rejected(string field)
        {

            var input = Snapshot();

            input[field] = new JArray("missing");
            Assert.That(RoamingNetwork.TryParse(input, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(field));

        }

        [TestCase("maxPower", "[]")]
        [TestCase("geoLocation", "{\"lat\":91,\"lng\":13}")]
        [TestCase("geoLocation", "{\"lat\":52,\"lng\":181}")]
        [TestCase("geoLocation", "{\"lat\":\"NaN\",\"lng\":13}")]
        [TestCase("geoLocation", "{\"lat\":52}")]
        [TestCase("status", "{\"value\":\"charging\"}")]
        [TestCase("status", "{\"value\":42,\"timestamp\":\"2024-01-01T00:00:00Z\"}")]
        [TestCase("customData", "42")]
        public void Malformed_deep_properties_report_their_field(string  field,
                                                                 string  value)
        {

            var input = Snapshot();

            input["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]![field] = JToken.Parse(value);
            Assert.That(RoamingNetwork.TryParse(input, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(field));

        }

        [Test]
        public void Energy_meter_with_description_roundtrips_inside_an_EVSE()
        {

            var input = Snapshot();
            var meter = new EnergyMeter(EnergyMeter_Id.Parse("meter-a"),
                                                Name: I18NString.Create("Meter A"), Description: I18NString.Create("Calibrated meter"),
                                                Manufacturer: "Vendor", SerialNumber: "123");

            input["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["energyMeter"] = meter.ToJSON(true);

            var json = Expanded(RoamingNetwork.Parse(input));

            Assert.That(JToken.DeepEquals(Expanded(RoamingNetwork.Parse(json)), json), Is.True);

        }

        [Test]
        public void Equal_latitude_and_longitude_do_not_hide_different_altitudes()
        {

            var input = Snapshot();
            var pool = (JObject)input["chargingStationOperators"]![0]!["chargingPools"]![0]!;
            var station = (JObject)pool["chargingStations"]![0]!;
            var evse = (JObject)station["EVSEs"]![0]!;

            pool["geoLocation"] = JObject.Parse("{\"lat\":52,\"lng\":13,\"alt\":\"20 m\"}");
            station["geoLocation"] = JObject.Parse("{\"lat\":52,\"lng\":13,\"alt\":\"21 m\"}");
            evse["geoLocation"] = JObject.Parse("{\"lat\":52,\"lng\":13,\"alt\":\"22 m\"}");

            var parsed = RoamingNetwork.Parse(Expanded(RoamingNetwork.Parse(input)));
            var parsedStation = parsed.ChargingStations.Single(item => item.Id.ToString() == "DE*ABC*S1");
            var parsedEVSE = parsed.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1");

            Assert.That(parsedStation.GeoLocation!.Value.Altitude!.Value.Value, Is.EqualTo(21));
            Assert.That(parsedEVSE.GeoLocation!.Value.Altitude!.Value.Value, Is.EqualTo(22));

        }

        [Test]
        public void Snapshot_retains_explicit_data_sources_equal_to_the_parent_source()
        {

            var input = Snapshot();
            var op = (JObject)input["chargingStationOperators"]![0]!;
            var pool = (JObject)op["chargingPools"]![0]!;
            var station = (JObject)pool["chargingStations"]![0]!;

            foreach (var entity in new[] { input, op, pool, station })
            {
                entity["dataSource"] = "inventory";
            }

            var parsed = RoamingNetwork.Parse(RoamingNetwork.Parse(input).ToJSONSnapshot());

            Assert.That(parsed.ChargingPools.Single(item => item.Id.ToString() == "DE*ABC*P1").DataSource, Is.EqualTo("inventory"));
            Assert.That(parsed.ChargingStations.Single(item => item.Id.ToString() == "DE*ABC*S1").DataSource, Is.EqualTo("inventory"));

        }

        [Test]
        public void Snapshot_preserves_explicit_location_and_openings_equal_to_the_parent()
        {

            var input = Snapshot();
            var pool = (JObject)input["chargingStationOperators"]![0]!["chargingPools"]![0]!;
            var station = (JObject)pool["chargingStations"]![0]!;
            var evse = (JObject)station["EVSEs"]![0]!;

            foreach (var entity in new[] { pool, station, evse })
            {
                entity["geoLocation"] = JObject.Parse("{\"lat\":52,\"lng\":13,\"alt\":\"20 m\"}");
            }

            foreach (var entity in new[] { pool, station })
            {
                entity["openingTimes"] = JObject.Parse("{\"24/7\":false,\"regularOpenings\":[{\"monday\":[{\"begin\":\"08:00\",\"end\":\"18:00\"}]}]}");
            }

            var parsed = RoamingNetwork.Parse(RoamingNetwork.Parse(input).ToJSONSnapshot());

            Assert.That(parsed.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1").GeoLocation.HasValue, Is.True);
            Assert.That(parsed.ChargingStations.Single(item => item.Id.ToString() == "DE*ABC*S1").OpeningTimes.IsOpen24Hours, Is.False);

        }
    }
}
