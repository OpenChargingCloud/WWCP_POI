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
        private static TransparencySoftware Software(string version = "2.0") => new(TransparencySoftware_Id.Parse("verifier-" + version),
            I18NString.Create("Verifier"), TransparencySoftwareVersion.Parse(version),
            [new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"), I18NString.Parse(JObject.Parse("""{"en":"MIT license","de":"MIT-Lizenz"}"""))!,
                URL.Parse("https://example.org/license"))], "Vendor",
            URL.Parse("https://example.org/logo.svg"), URL.Parse("https://example.org/manual"),
            URL.Parse("https://example.org/software"), URL.Parse("https://example.org/source"));

        private static TransparencySoftwareCertificate Certificate(string id = "certificate-1", DateTimeOffset? start = null)
            => new(TransparencySoftwareCertificate_Id.Parse(id), "issuer-a", "station-model", "1.0",
                [Software("2.0").Id, Software("3.0").Id, Software("4.0").Id],
                NotBefore: start ?? DateTimeOffset.Parse("2026-01-01T12:30:00.1234567+02:00", CultureInfo.InvariantCulture),
                NotAfter: DateTimeOffset.Parse("2027-01-01T12:30:00.7654321+02:00", CultureInfo.InvariantCulture));

        private static TransparencySoftwareStatus Status(string version = "2.0") => new(Software(version), LegalStatus.Verified, Certificate());

        private static TransparencySoftwareStatus ParseStatus(JObject json,
            CustomJObjectParserDelegate<TransparencySoftwareStatus>? CustomTransparencySoftwareStatusParser = null)
            => TransparencySoftwareStatus.Parse(json, CustomTransparencySoftwareStatusParser, Network());

        private static bool TryParseStatus(JObject json, out TransparencySoftwareStatus? value, out string? error,
            CustomJObjectParserDelegate<TransparencySoftwareStatus>? custom = null)
            => TransparencySoftwareStatus.TryParse(json, out value, out error, custom, Network());

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

            json["transparencySoftware"] = new JArray(new[] { Software("2.0"), Software("3.0"), Software("4.0") }.Select(value => value.ToJSON()));
            json["transparencySoftwareCertificates"] = new JArray(Certificate().ToJSON(), Certificate("certificate-2").ToJSON());
            json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["energyMeter"] = Meter().ToJSON();

            return RoamingNetwork.Parse(json);

        }

        private static RoamingNetworkChangeSet Set(RoamingNetwork  network,
                                                   JsonElement     oldMeter,
                                                   JsonElement     newMeter)
            => network.CreateChangeSet("transparency-change", DateTimeOffset.Parse("2026-10-06T12:00:00Z"),
                [RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "energyMeter", oldMeter, newMeter)]);

        /// <summary>
        /// Within a roaming network, documents resolve their software and manufacturer references
        /// in its registries, and the POI envelope of an exported document is no property of the value.
        /// </summary>
        [Test]
        public void Network_documents_resolve_their_references_and_drop_the_POI_envelope()
        {

            var network  = Network();

            var exported = Certificate().ToJSON();
            exported["ETags"]                            = new JArray();
            exported[POIContentProfile.PropertyName]     = POIContentProfile.Id;

            Assert.That(TransparencySoftwareCertificate.Parse(exported, network).ToJSON().ToString(), Is.EqualTo(Certificate().ToJSON().ToString()));

            var unknownSoftware = Certificate().ToJSON();
            unknownSoftware["compatibleTransparencySoftwareIds"] = new JArray("verifier-9.9");
            Assert.That(() => TransparencySoftwareCertificate.Parse(unknownSoftware, network), Throws.ArgumentException.With.Message.Contains("verifier-9.9"));

            var unknownManufacturer = Certificate().ToJSON();
            unknownManufacturer["chargingStationManufacturerId"] = "maker-x";
            Assert.That(() => TransparencySoftwareCertificate.Parse(unknownManufacturer, network), Throws.ArgumentException.With.Message.Contains("maker-x"));

            var status = Status().ToJSON();
            status["ETags"] = new JArray();
            Assert.That(ParseStatus(status), Is.EqualTo(Status()));
            Assert.That(Status().Clone(network), Is.EqualTo(Status()));

            Assert.That(TransparencySoftwareStatus.TryParse(Status().ToJSON(), out var withoutNetwork, out var error), Is.False);
            Assert.That(withoutNetwork, Is.Null);
            Assert.That(error, Does.Contain("unresolved software"));
            Assert.That(TryParseStatus(Status().ToJSON(), out var withNetwork, out _), Is.True);
            Assert.That(withNetwork, Is.EqualTo(Status()));

        }

        [Test]
        public void Energy_meter_copies_software_collection_and_keeps_distinct_versions()
        {

            var statuses = new List<TransparencySoftwareStatus> { Status("2.0"), Status("3.0") };
            var meter = new EnergyMeter(EnergyMeter_Id.Parse("meter-1"), TransparencySoftware: statuses);

            statuses.Clear();
            Assert.That(meter.TransparencySoftware.Count(), Is.EqualTo(2));

            var restored = EnergyMeter.Parse(meter.ToJSON(), Network: Network());

            Assert.That(restored.TransparencySoftware, Is.EquivalentTo(meter.TransparencySoftware));
            Assert.That(restored.LastChangeDate, Is.EqualTo(meter.LastChangeDate).Within(TimeSpan.FromMilliseconds(1)));
            Assert.Throws<ArgumentException>(() => new EnergyMeter(EnergyMeter_Id.Parse("meter-1"), TransparencySoftware: [null!]));

        }

        [Test]
        public void Invalid_nested_status_reports_the_meter_JSON_path()
        {

            var json = Meter().ToJSON();

            json["transparencySoftware"]![1]!["notAfter"] = "2020-01-01T00:00:00Z";
            Assert.That(EnergyMeter.TryParse(json, out var result, out var error, Network: Network()), Is.False);
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
            document["transparencySoftware"]![0]!["certificateId"] = "certificate-2";
            document["transparencySoftware"]![0]!["transparencySoftwareId"] = Software("4.0").Id.ToString();

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

            document["transparencySoftware"]![0]!["transparencySoftwareId"] = " ";

            var exception = Assert.Throws<RoamingNetworkChangeSetException>(() => source.ApplyChangeSet(Set(source, oldMeter,
                                                JsonSerializer.Deserialize<JsonElement>(document.ToString()))));

            Assert.That(exception!.OperationIndex, Is.EqualTo(0));
            Assert.That(exception.Message, Does.Contain("transparencySoftware[0]").And.Contain("transparencySoftwareId"));
            Assert.That(source.DataSnapshot, Is.SameAs(data));

        }}
}
