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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace WWCP_POI_Tests.Json
{
    [TestFixture]
    public sealed class TransparencySoftwareJsonTests
    {
        private static TransparencySoftware Software(string version = "2.0") => new("Verifier", version,
            new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"), I18NString.Parse(JObject.Parse("""{"en":"MIT license","de":"MIT-Lizenz"}"""))!,
                URL.Parse("https://example.org/license")), "Vendor",
            URL.Parse("https://example.org/logo.svg"), URL.Parse("https://example.org/manual"),
            URL.Parse("https://example.org/software"), URL.Parse("https://example.org/source"));

        private static TransparencySoftwareStatus Status(string version = "2.0") => new(Software(version), LegalStatus.Verified, "certificate-1", "issuer-a",
            DateTimeOffset.Parse("2026-01-01T12:30:00.1234567+02:00", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2027-01-01T12:30:00.7654321+02:00", CultureInfo.InvariantCulture));

        private static EnergyMeter Meter() => new(EnergyMeter_Id.Parse("meter-1"), I18NString.Create("Meter"),
            TransparencySoftware: [Status("2.0"), Status("3.0")],
            DataSource: "meter-feed", Created: DateTimeOffset.Parse("2025-01-01T00:00:00Z"),
            LastChange: DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            CustomData: CustomDataNew.ParseJSON("""{"vendorId":"example.org","serial":123}"""));

        private static RoamingNetwork Network()
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
                              "@id": "DE*ABC*E1"
                            },
                            {
                              "@id": "DE*ABC*E2"
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

            json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["energyMeter"] = Meter().ToJSON();

            return RoamingNetwork.Parse(json);

        }

        private static RoamingNetworkChangeSet Set(RoamingNetwork  network,
                                                   JsonElement     oldMeter,
                                                   JsonElement     newMeter)
            => network.CreateChangeSet("transparency-change", DateTimeOffset.Parse("2026-10-06T12:00:00Z"),
                [RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "energyMeter", oldMeter, newMeter)]);

        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("tr-TR")]
        public void Software_roundtrip_preserves_complete_license_and_all_links(string culture)
        {

            var previous = CultureInfo.CurrentCulture;

            try
            {

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                var source = Software();
                var json = source.ToJSON();
                var before = json.DeepClone();
                var parsed = TransparencySoftware.Parse(JObject.Parse(json.ToString()));

                Assert.That(parsed, Is.EqualTo(source));
                Assert.That(parsed.GetHashCode(), Is.EqualTo(source.GetHashCode()));
                Assert.That(parsed.CompareTo(source), Is.Zero);
                Assert.That(JsonViews.EqualViews(parsed.ToJSON(), json), Is.True);
                Assert.That(JToken.DeepEquals(json, before), Is.True);
                Assert.That(json["open_source_license"], Is.Null);
                Assert.That(json["openSourceLicense"]!["@id"]!.Value<string>(), Is.EqualTo("MIT"));
                Assert.That(parsed.OpenSourceLicense.Description.ToJSON()["de"]!.Value<string>(), Is.EqualTo("MIT-Lizenz"));

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

        }

        [Test]
        public void Minimal_software_omits_optional_links()
        {

            var json = JObject.Parse("""{"name":"Verifier","version":"1","vendor":"Vendor","openSourceLicense":{"@id":"MIT"}}""");
            var parsed = TransparencySoftware.Parse(json);

            Assert.That(parsed.Logo, Is.Null);
            Assert.That(parsed.HowToUse, Is.Null);
            Assert.That(parsed.OpenSourceLicense.URLs, Is.Empty);
            Assert.That(TransparencySoftware.Parse(parsed.ToJSON()), Is.EqualTo(parsed));

        }

        [TestCase("open_source_license")]
        [TestCase("openSourceLicense")]
        public void License_strings_are_rejected(string field)
        {

            var json = Software().ToJSON();

            json.Remove("openSourceLicense");
            json[field] = OpenSourceLicense.MIT.ToString();

            Assert.That(TransparencySoftware.TryParse(json, out _, out _), Is.False);
            json[field] = "custom-license: Custom license terms";
            Assert.That(TransparencySoftware.TryParse(json, out _, out _), Is.False);

        }

        [TestCase("name")]
        [TestCase("version")]
        [TestCase("vendor")]
        [TestCase("openSourceLicense")]
        public void Missing_software_fields_are_rejected(string field)
        {

            var json = Software().ToJSON();

            json.Remove(field);
            Assert.That(TransparencySoftware.TryParse(json, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(field));

        }

        [TestCase("name", "42")]
        [TestCase("version", "true")]
        [TestCase("vendor", "{}")]
        [TestCase("logo", "12")]
        [TestCase("howToUse", "\"relative/path\"")]
        [TestCase("moreInformation", "[]")]
        [TestCase("sourceCodeRepository", "\"\"")]
        [TestCase("openSourceLicense", "false")]
        [TestCase("openSourceLicense", "{\"@id\":\"MIT\",\"id\":\"Apache-2.0\"}")]
        [TestCase("openSourceLicense", "{\"@id\":\"MIT\",\"URLs\":[42]}")]
        public void Invalid_software_fields_are_rejected(string  field,
                                                         string  value)
        {

            var json = Software().ToJSON();

            json[field] = JToken.Parse(value);
            Assert.That(TransparencySoftware.TryParse(json, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Empty);

        }

        [Test]
        public void Unknown_license_property_is_rejected()
        {

            var json = Software().ToJSON();

            json["open_source_license"] = "Apache-2.0";
            Assert.That(TransparencySoftware.TryParse(json, out _, out var error), Is.False);
            Assert.That(error, Does.Contain("open_source_license"));

        }

        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("tr-TR")]
        public void Status_roundtrip_preserves_certificates_UTC_instants_and_ticks(string culture)
        {

            var previous = CultureInfo.CurrentCulture;

            try
            {

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                var source = Status();
                var parsed = TransparencySoftwareStatus.Parse(JObject.Parse(source.ToJSON().ToString()));

                Assert.That(parsed, Is.EqualTo(source));
                Assert.That(parsed.GetHashCode(), Is.EqualTo(source.GetHashCode()));
                Assert.That(parsed.NotBefore!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
                Assert.That(parsed.NotBefore.Value.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1234567));
                Assert.That(parsed.NotAfter!.Value.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(7654321));
                Assert.That(JsonViews.EqualViews(parsed.ToJSON(), source.ToJSON()), Is.True);

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

        }

        [TestCase("certificate", "12")]
        [TestCase("certificateIssuer", "false")]
        [TestCase("notBefore", "\"invalid\"")]
        [TestCase("notAfter", "true")]
        [TestCase("notAfter", "\"2025-01-01T00:00:00Z\"")]
        [TestCase("legalStatus", "\"  \"")]
        [TestCase("legalStatus", "42")]
        [TestCase("transparencySoftware", "{}")]
        public void Invalid_status_fields_are_rejected(string  field,
                                                       string  value)
        {

            var json = Status().ToJSON();

            json[field] = JToken.Parse(value);
            Assert.That(TransparencySoftwareStatus.TryParse(json, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Empty);

        }

        [Test]
        public void Optional_status_fields_can_be_absent_or_null_and_future_legal_statuses_are_preserved()
        {

            var json = new JObject(new JProperty("transparencySoftware", Software().ToJSON()), new JProperty("legalStatus", "future-status"));
            var minimal = TransparencySoftwareStatus.Parse(json);

            Assert.That(minimal.Certificate, Is.Null);
            Assert.That(minimal.NotBefore, Is.Null);

            foreach (var field in new[] { "certificate", "certificateIssuer", "notBefore", "notAfter" })
            {
                json[field] = JValue.CreateNull();
            }

            Assert.That(TransparencySoftwareStatus.Parse(json), Is.EqualTo(minimal));
            Assert.That(minimal.LegalStatus.ToString(), Is.EqualTo("future-status"));

        }

        [TestCase("2026-01-01T12:30:00.1234567+02:00")]
        [TestCase("2026-01-01T10:30:00.1234567Z")]
        [TestCase("2026-01-01T10:30:00.1234567")]
        public void Validity_timestamps_require_offsets_and_normalize_to_UTC(string timestamp)
        {

            var json = Status().ToJSON();

            json["notBefore"] = timestamp;

            if (!timestamp.EndsWith('Z') && !timestamp.Contains('+'))
            {
                Assert.That(TransparencySoftwareStatus.TryParse(json, out _, out _), Is.False);
                return;
            }

            var parsed = TransparencySoftwareStatus.Parse(json);

            Assert.That(parsed.NotBefore, Is.EqualTo(DateTimeOffset.Parse("2026-01-01T10:30:00.1234567Z", CultureInfo.InvariantCulture)));
            Assert.That(parsed.NotBefore!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(parsed.GetHashCode(), Is.EqualTo(Status().GetHashCode()));

        }

        [TestCase("transparencySoftware")]
        [TestCase("legalStatus")]
        public void Missing_status_fields_are_rejected(string field)
        {

            var json = Status().ToJSON();

            json.Remove(field);
            Assert.That(TransparencySoftwareStatus.TryParse(json, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(field));

        }

        [Test]
        public void License_order_does_not_affect_software_equality_or_hashes()
        {

            var firstLicense = new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"),
                                                I18NString.Parse(JObject.Parse("""{"en":"License","de":"Lizenz"}"""))!,
                                                URL.Parse("https://example.org/a"), URL.Parse("https://example.org/b"));
            var secondLicense = new OpenSourceLicense(OpenSourceLicense_Id.Parse("mit"),
                                                I18NString.Parse(JObject.Parse("""{"de":"Lizenz","en":"License"}"""))!,
                                                URL.Parse("https://example.org/b"), URL.Parse("https://example.org/a"));
            var first = new TransparencySoftware("Verifier", "2", firstLicense, "Vendor");
            var second = new TransparencySoftware("Verifier", "2", secondLicense, "Vendor");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first.CompareTo(second), Is.Zero);

            var changedLicense = new TransparencySoftware("Verifier", "2", new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"),
                                                I18NString.Create("Other terms"), URL.Parse("https://example.org/a")), "Vendor");

            Assert.That(first.CompareTo(changedLicense), Is.Not.Zero);

        }

        [Test]
        public void Equivalent_license_URL_hosts_have_equal_software_and_status_hashes()
        {

            var first = new TransparencySoftware("Verifier", "2", new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"),
                                                URL.Parse("https://EXAMPLE.org/license")), "Vendor");
            var second = new TransparencySoftware("Verifier", "2", new OpenSourceLicense(OpenSourceLicense_Id.Parse("mit"),
                                                URL.Parse("https://example.org/license")), "Vendor");

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first.CompareTo(second), Is.Zero);

            var firstStatus = new TransparencySoftwareStatus(first, LegalStatus.Verified);
            var secondStatus = new TransparencySoftwareStatus(second, LegalStatus.Parse("VERIFIED"));

            Assert.That(firstStatus, Is.EqualTo(secondStatus));
            Assert.That(firstStatus.GetHashCode(), Is.EqualTo(secondStatus.GetHashCode()));
            Assert.That(new HashSet<TransparencySoftwareStatus> { firstStatus, secondStatus }.Count, Is.EqualTo(1));

        }

        [Test]
        public void Software_and_status_comparisons_distinguish_all_serialized_fields()
        {

            var plain = new TransparencySoftware("Verifier", "2.0", OpenSourceLicense.MIT, "Vendor");
            var logo = new TransparencySoftware("Verifier", "2.0", OpenSourceLicense.MIT, "Vendor", Logo: URL.Parse("https://example.org/logo"));

            Assert.That(plain.CompareTo(logo), Is.Not.Zero);
            Assert.That(logo.CompareTo(plain), Is.EqualTo(-plain.CompareTo(logo)));
            Assert.That(new SortedSet<TransparencySoftware> { plain, logo }.Count, Is.EqualTo(2));

            var current = Status();
            var version = Status("3.0");
            var certificate = new TransparencySoftwareStatus(Software(), LegalStatus.Verified, "certificate-2", "issuer-a", current.NotBefore, current.NotAfter);
            var fractionalTime = new TransparencySoftwareStatus(Software(), LegalStatus.Verified, current.Certificate, current.CertificateIssuer,
                                                current.NotBefore!.Value.AddTicks(1), current.NotAfter);

            Assert.That(new SortedSet<TransparencySoftwareStatus> { current, version, certificate, fractionalTime }.Count, Is.EqualTo(4));
            Assert.That(current.Equals(fractionalTime), Is.False);

        }

        [Test]
        public void License_inputs_and_exposed_license_values_are_copied()
        {

            var description = I18NString.Create("Original");
            var links = new[] { URL.Parse("https://example.org/original") };
            var license = new OpenSourceLicense(OpenSourceLicense_Id.Parse("custom"), description, links);
            var software = new TransparencySoftware("Verifier", "2", license, "Vendor");
            var before = software.ToJSON();

            links[0] = URL.Parse("https://example.org/changed");
            description.Set(Languages.en, "Changed");
            software.OpenSourceLicense.Description.Set(Languages.en, "Changed again");
            Assert.That(JsonViews.EqualViews(software.ToJSON(), before), Is.True);
            Assert.That(software.Clone(), Is.EqualTo(software));
            Assert.That(Status().Clone(), Is.EqualTo(Status()));

        }

        [Test]
        public void Custom_parser_and_serializer_callbacks_propagate_and_null_results_fail()
        {

            var nestedCalls = 0;
            var statusCalls = 0;
            var parsed = TransparencySoftwareStatus.Parse(Status().ToJSON(),
                                                CustomTransparencySoftwareStatusParser: (_, value) =>
                                                {

                                                    statusCalls++;

                                                    return value;

                                                },
                                                CustomTransparencySoftwareParser: (_, value) =>
                                                {

                                                    nestedCalls++;

                                                    return value;

                                                });

            Assert.That(nestedCalls, Is.EqualTo(1));
            Assert.That(statusCalls, Is.EqualTo(1));

            var json = parsed.ToJSON(CustomTransparencySoftwareSerializer: (_, document) =>
                                    {

                                        document["extension"] = 1;

                                        return document;

                                    });

            Assert.That(json["transparencySoftware"]!["extension"]!.Value<int>(), Is.EqualTo(1));
            Assert.That(TransparencySoftware.TryParse(Software().ToJSON(), out var software, out var error, (_, _) => null!), Is.False);
            Assert.That(software, Is.Null);
            Assert.That(error, Does.Contain("returned null"));
            Assert.That(TransparencySoftwareStatus.TryParse(Status().ToJSON(), out var status, out error, (_, _) => null!), Is.False);
            Assert.That(status, Is.Null);
            Assert.That(error, Does.Contain("returned null"));

        }

        [Test]
        public void Energy_meter_copies_software_collection_and_keeps_distinct_versions()
        {

            var statuses = new List<TransparencySoftwareStatus> { Status("2.0"), Status("3.0") };
            var meter = new EnergyMeter(EnergyMeter_Id.Parse("meter-1"), TransparencySoftware: statuses);

            statuses.Clear();
            Assert.That(meter.TransparencySoftware.Count(), Is.EqualTo(2));

            var restored = EnergyMeter.Parse(meter.ToJSON());

            Assert.That(restored.TransparencySoftware, Is.EquivalentTo(meter.TransparencySoftware));
            Assert.That(restored.LastChangeDate, Is.EqualTo(meter.LastChangeDate).Within(TimeSpan.FromMilliseconds(1)));
            Assert.Throws<ArgumentException>(() => new EnergyMeter(EnergyMeter_Id.Parse("meter-1"), TransparencySoftware: [null!]));

        }

        [Test]
        public void Invalid_nested_status_reports_the_meter_JSON_path()
        {

            var json = Meter().ToJSON();

            json["transparencySoftware"]![1]!["notAfter"] = "2020-01-01T00:00:00Z";
            Assert.That(EnergyMeter.TryParse(json, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain("transparencySoftware[1]"));

        }

        [Test]
        public void Snapshot_roundtrip_keeps_meter_metadata_and_transparency_software()
        {

            var source = Network();
            var json = source.ToJSONSnapshot();
            var restored = RoamingNetwork.Parse(json.ToString());
            var meter = restored.EVSEs.Single(evse => evse.Id.ToString() == "DE*ABC*E1").EnergyMeter!;

            Assert.That(meter.TransparencySoftware, Is.EquivalentTo(Meter().TransparencySoftware));
            Assert.That(meter.DataSource, Is.EqualTo("meter-feed"));
            Assert.That(meter.CustomData.ToJObject()["serial"]!.Value<int>(), Is.EqualTo(123));
            Assert.That(meter.LastChangeDate, Is.EqualTo(DateTimeOffset.Parse("2026-01-01T00:00:00Z")));
            Assert.That(meter.Created, Is.EqualTo(DateTimeOffset.Parse("2025-01-01T00:00:00Z")));
            Assert.That(JsonViews.EqualViews(json, restored.ToJSONSnapshot()), Is.True);

        }

        [Test]
        public void ChangeSet_updates_nested_software_and_preserves_the_source_and_unrelated_EVSE()
        {

            var source = Network();
            var data = source.DataSnapshot;
            var entity = data.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1");
            var oldMeter = entity.Properties["energyMeter"];
            var document = JObject.Parse(oldMeter.GetRawText());

            document["transparencySoftware"]![0]!["legalStatus"] = "GermanCalibrationLaw";
            document["transparencySoftware"]![0]!["certificate"] = "certificate-2";
            document["transparencySoftware"]![0]!["transparencySoftware"]!["version"] = "4.0";

            var next = source.ApplyChangeSet(Set(source, oldMeter, JsonSerializer.Deserialize<JsonElement>(document.ToString())));

            Assert.That(next.Revision, Is.EqualTo(1));
            Assert.That(next.DataSnapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E2"), Is.SameAs(data.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E2")));

            var updated = next.EVSEs.Single(evse => evse.Id.ToString() == "DE*ABC*E1").EnergyMeter!.TransparencySoftware.First();

            Assert.That(updated.TransparencySoftware.Version, Is.EqualTo("4.0"));
            Assert.That(updated.LegalStatus, Is.EqualTo(LegalStatus.GermanCalibrationLaw));
            Assert.That(source.EVSEs.Single(evse => evse.Id.ToString() == "DE*ABC*E1").EnergyMeter!.TransparencySoftware.First().TransparencySoftware.Version, Is.EqualTo("2.0"));

            var restored = RoamingNetwork.Parse(next.ToJSONSnapshot().ToString());

            Assert.That(restored.Revision, Is.EqualTo(1));
            Assert.That(restored.EVSEs.Single(evse => evse.Id.ToString() == "DE*ABC*E1").EnergyMeter!.TransparencySoftware.First(), Is.EqualTo(updated));

        }

        [Test]
        public void Invalid_nested_software_change_is_an_atomic_conflict()
        {

            var source = Network();
            var data = source.DataSnapshot;
            var oldMeter = data.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["energyMeter"];
            var document = JObject.Parse(oldMeter.GetRawText());

            document["transparencySoftware"]![0]!["transparencySoftware"]!["version"] = " ";

            var exception = Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source, oldMeter,
                                                JsonSerializer.Deserialize<JsonElement>(document.ToString()))));

            Assert.That(exception!.OperationIndex, Is.EqualTo(0));
            Assert.That(exception.Message, Does.Contain("transparencySoftware[0]").And.Contain("version"));
            Assert.That(source.DataSnapshot, Is.SameAs(data));

        }

        [Test]
        public void Legal_status_hashes_are_culture_independent_and_null_parse_is_safe()
        {

            var first = LegalStatus.Parse("INFORMATION");
            var second = LegalStatus.Parse("information");
            var previous = CultureInfo.CurrentCulture;

            try
            {

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
                Assert.That(new HashSet<LegalStatus> { first, second }.Count, Is.EqualTo(1));
                Assert.That(LegalStatus.TryParse(null!, out _), Is.False);

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

            Assert.That(TransparencySoftware.TryParse(null!, out _, out _), Is.False);
            Assert.That(TransparencySoftwareStatus.TryParse(null!, out _, out _), Is.False);

        }
    }
}
