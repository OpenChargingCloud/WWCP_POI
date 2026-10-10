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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Transparency
{

    /// <summary>
    /// Transparency software, its documents and its legal status as plain values:
    /// references resolve through lookups instead of a roaming network.
    /// </summary>
    [TestFixture]
    public sealed class TransparencySoftwareJsonTests
    {

        private static TransparencySoftware Software(string version = "2.0") => new(TransparencySoftware_Id.Parse("verifier-" + version),
            I18NString.Parse(JObject.Parse("""{"en":"Verifier","de":"Prüfprogramm"}"""))!, TransparencySoftwareVersion.Parse(version),
            [new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"), I18NString.Parse(JObject.Parse("""{"en":"MIT license","de":"MIT-Lizenz"}"""))!,
                URL.Parse("https://example.org/license"))], "Vendor",
            URL.Parse("https://example.org/logo.svg"), URL.Parse("https://example.org/manual"),
            URL.Parse("https://example.org/software"), URL.Parse("https://example.org/source"));

        private static TransparencySoftware Release(string version, params OpenSourceLicense[] licenses)
            => new(TransparencySoftware_Id.Parse("verifier"), I18NString.Create("Verifier"), TransparencySoftwareVersion.Parse(version), licenses, "Vendor");

        private static TransparencySoftwareCertificate Certificate(string id = "certificate-1", DateTimeOffset? start = null)
            => new(TransparencySoftwareCertificate_Id.Parse(id), "issuer-a", "station-model", "1.0",
                [Software("2.0").Id, Software("3.0").Id, Software("4.0").Id],
                NotBefore: start ?? DateTimeOffset.Parse("2026-01-01T12:30:00.1234567+02:00", CultureInfo.InvariantCulture),
                NotAfter: DateTimeOffset.Parse("2027-01-01T12:30:00.7654321+02:00", CultureInfo.InvariantCulture));

        private static TransparencySoftwareStatus Status(string version = "2.0") => new(Software(version), LegalStatus.Verified, Certificate());

        private static readonly TransparencySoftware[]             Registry      = [Software("2.0"), Software("3.0"), Software("4.0")];
        private static readonly TransparencySoftwareCertificate[]  Certificates  = [Certificate(), Certificate("certificate-2")];

        private static TransparencySoftware? SoftwareLookup(TransparencySoftware_Id Id)
            => Registry.FirstOrDefault(software => software.Id == Id);

        private static TransparencySoftwareCertificate? CertificateLookup(TransparencySoftwareCertificate_Id Id)
            => Certificates.FirstOrDefault(certificate => certificate.Id == Id);

        private static TransparencySoftwareStatus ParseStatus(JObject json,
            CustomJObjectParserDelegate<TransparencySoftwareStatus>? CustomTransparencySoftwareStatusParser = null)
            => TransparencySoftwareStatus.Parse(json, SoftwareLookup, CertificateLookup, CustomTransparencySoftwareStatusParser);

        private static bool TryParseStatus(JObject json, out TransparencySoftwareStatus? value, out string? error,
            CustomJObjectParserDelegate<TransparencySoftwareStatus>? custom = null)
            => TransparencySoftwareStatus.TryParse(json, SoftwareLookup, CertificateLookup, out value, out error, custom);

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
                Assert.That(JToken.DeepEquals(parsed.ToJSON(), json), Is.True);
                Assert.That(JToken.DeepEquals(json, before), Is.True);
                Assert.That(json["openSourceLicense"], Is.Null);
                Assert.That(json["openSourceLicenses"]![0]!["@id"]!.Value<string>(), Is.EqualTo("MIT"));
                Assert.That(json["name"]!["de"]!.Value<string>(), Is.EqualTo("Prüfprogramm"));
                Assert.That(parsed.OpenSourceLicenses.Single().Description.ToJSON()["de"]!.Value<string>(), Is.EqualTo("MIT-Lizenz"));

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

        }

        [Test]
        public void Minimal_software_omits_optional_links()
        {

            var json = JObject.Parse("""{"@id":"verifier-1","name":{"en":"Verifier"},"version":"1","vendor":"Vendor","openSourceLicenses":[{"@id":"MIT"}]}""");
            var parsed = TransparencySoftware.Parse(json);

            Assert.That(parsed.Logo, Is.Null);
            Assert.That(parsed.HowToUse, Is.Null);
            Assert.That(parsed.OpenSourceLicenses.Single().URLs, Is.Empty);
            Assert.That(TransparencySoftware.Parse(parsed.ToJSON()), Is.EqualTo(parsed));

        }

        [TestCase("open_source_license")]
        [TestCase("openSourceLicense")]
        [TestCase("openSourceLicenses")]
        public void License_strings_are_rejected(string field)
        {

            var json = Software().ToJSON();

            json.Remove("openSourceLicenses");
            json[field] = OpenSourceLicense.MIT.ToString();

            Assert.That(TransparencySoftware.TryParse(json, out _, out _), Is.False);
            json[field] = "custom-license: Custom license terms";
            Assert.That(TransparencySoftware.TryParse(json, out _, out _), Is.False);

        }

        [TestCase("name")]
        [TestCase("version")]
        [TestCase("vendor")]
        [TestCase("openSourceLicenses")]
        public void Missing_software_fields_are_rejected(string field)
        {

            var json = Software().ToJSON();

            json.Remove(field);
            Assert.That(TransparencySoftware.TryParse(json, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(field));

        }

        [TestCase("name", "42")]
        [TestCase("name", "\"Verifier\"")]
        [TestCase("name", "{}")]
        [TestCase("name", "{\"en\":\" \"}")]
        [TestCase("version", "true")]
        [TestCase("version", "\" \"")]
        [TestCase("vendor", "{}")]
        [TestCase("logo", "12")]
        [TestCase("howToUse", "\"relative/path\"")]
        [TestCase("moreInformation", "[]")]
        [TestCase("sourceCodeRepository", "\"\"")]
        [TestCase("openSourceLicenses", "false")]
        [TestCase("openSourceLicenses", "[]")]
        [TestCase("openSourceLicenses", "{\"@id\":\"MIT\"}")]
        [TestCase("openSourceLicenses", "[{\"@id\":\"MIT\",\"id\":\"Apache-2.0\"}]")]
        [TestCase("openSourceLicenses", "[{\"@id\":\"MIT\",\"URLs\":[42]}]")]
        [TestCase("openSourceLicenses", "[{\"@id\":\"MIT\"},{\"@id\":\"mit\"}]")]
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

        /// <summary>
        /// A release names all its licenses; their order is no property of the release,
        /// so they are written in a canonical order, and a license must not be given twice.
        /// </summary>
        [Test]
        public void Several_licenses_are_a_set_written_in_canonical_order()
        {

            var mit     = new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"),        URL.Parse("https://example.org/mit"));
            var apache  = new OpenSourceLicense(OpenSourceLicense_Id.Parse("Apache-2.0"), URL.Parse("https://example.org/apache"));

            var first   = Release("2", mit, apache);
            var second  = Release("2", apache, mit);

            Assert.That(first,                                      Is.EqualTo(second));
            Assert.That(first.GetHashCode(),                        Is.EqualTo(second.GetHashCode()));
            Assert.That(first.ToJSON().ToString(),                  Is.EqualTo(second.ToJSON().ToString()));
            Assert.That(first.OpenSourceLicenses.Select(license => license.Id.ToString()), Is.EqualTo(new[] { "Apache-2.0", "MIT" }));
            Assert.That(TransparencySoftware.Parse(first.ToJSON()), Is.EqualTo(first));
            Assert.That(first,                                      Is.Not.EqualTo(Release("2", mit)));

            Assert.That(() => Release("2"),                                                                      Throws.ArgumentException);
            Assert.That(() => Release("2", mit, new OpenSourceLicense(OpenSourceLicense_Id.Parse("mit"))),        Throws.ArgumentException);

        }

        /// <summary>
        /// The name has a text per language, and versions compare without regard to case.
        /// </summary>
        [Test]
        public void Names_are_multilingual_and_versions_typed()
        {

            var software = Software();

            Assert.That(software.Name[Languages.de],             Is.EqualTo("Prüfprogramm"));
            Assert.That(software.Version,                         Is.EqualTo(TransparencySoftwareVersion.Parse("2.0")));
            Assert.That(Release("2.0-RC1", OpenSourceLicense.MIT), Is.EqualTo(Release("2.0-rc1", OpenSourceLicense.MIT)));
            Assert.That(Release("2.0", OpenSourceLicense.MIT),     Is.Not.EqualTo(Release("2.1", OpenSourceLicense.MIT)));

            Assert.That(() => new TransparencySoftware(TransparencySoftware_Id.Parse("verifier"), I18NString.Empty, TransparencySoftwareVersion.Parse("1"),
                                                       [OpenSourceLicense.MIT], "Vendor"),
                        Throws.ArgumentException);

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
                var parsed = ParseStatus(JObject.Parse(source.ToJSON().ToString()));

                Assert.That(parsed, Is.EqualTo(source));
                Assert.That(parsed.GetHashCode(), Is.EqualTo(source.GetHashCode()));
                Assert.That(parsed.Certificate!.NotBefore!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
                Assert.That(parsed.Certificate!.NotBefore.Value.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1234567));
                Assert.That(parsed.Certificate!.NotAfter!.Value.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(7654321));
                Assert.That(JToken.DeepEquals(parsed.ToJSON(), source.ToJSON()), Is.True);

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
            Assert.That(TryParseStatus(json, out var result, out var error), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Empty);

        }

        [Test]
        public void Optional_status_fields_can_be_absent_or_null_and_future_legal_statuses_are_preserved()
        {

            var json = new JObject(new JProperty("transparencySoftwareId", Software().Id.ToString()), new JProperty("legalStatus", "future-status"));
            var minimal = ParseStatus(json);

            Assert.That(minimal.Certificate, Is.Null);

            foreach (var field in new[] { "certificateId" })
            {
                json[field] = JValue.CreateNull();
            }

            Assert.That(ParseStatus(json), Is.EqualTo(minimal));
            Assert.That(minimal.LegalStatus.ToString(), Is.EqualTo("future-status"));

        }

        [TestCase("2026-01-01T12:30:00.1234567+02:00")]
        [TestCase("2026-01-01T10:30:00.1234567Z")]
        [TestCase("2026-01-01T10:30:00.1234567")]
        public void Validity_timestamps_require_offsets_and_normalize_to_UTC(string timestamp)
        {

            var json = Certificate().ToJSON();

            json["notBefore"] = timestamp;

            if (!timestamp.EndsWith('Z') && !timestamp.Contains('+'))
            {
                Assert.Throws<ArgumentException>(() => TransparencySoftwareCertificate.Parse(json));
                return;
            }

            var parsed = TransparencySoftwareCertificate.Parse(json);

            Assert.That(parsed.NotBefore, Is.EqualTo(DateTimeOffset.Parse("2026-01-01T10:30:00.1234567Z", CultureInfo.InvariantCulture)));
            Assert.That(parsed.NotBefore!.Value.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(parsed.ToJSON().ToString(), Is.EqualTo(Certificate().ToJSON().ToString()));

        }

        [TestCase("transparencySoftwareId")]
        [TestCase("legalStatus")]
        public void Missing_status_fields_are_rejected(string field)
        {

            var json = Status().ToJSON();

            json.Remove(field);
            Assert.That(TryParseStatus(json, out var result, out var error), Is.False);
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
            var first = Release("2", firstLicense);
            var second = Release("2", secondLicense);

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first.CompareTo(second), Is.Zero);

            var changedLicense = Release("2", new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"),
                                                I18NString.Create("Other terms"), URL.Parse("https://example.org/a")));

            Assert.That(first.CompareTo(changedLicense), Is.Not.Zero);

        }

        [Test]
        public void Equivalent_license_URL_hosts_have_equal_software_and_status_hashes()
        {

            var first = Release("2", new OpenSourceLicense(OpenSourceLicense_Id.Parse("MIT"),
                                                URL.Parse("https://EXAMPLE.org/license")));
            var second = Release("2", new OpenSourceLicense(OpenSourceLicense_Id.Parse("mit"),
                                                URL.Parse("https://example.org/license")));

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

            var plain = Release("2.0", OpenSourceLicense.MIT);
            var logo = new TransparencySoftware(TransparencySoftware_Id.Parse("verifier"), I18NString.Create("Verifier"), TransparencySoftwareVersion.Parse("2.0"),
                                                [OpenSourceLicense.MIT], "Vendor", Logo: URL.Parse("https://example.org/logo"));

            Assert.That(plain.CompareTo(logo), Is.Not.Zero);
            Assert.That(logo.CompareTo(plain), Is.EqualTo(-plain.CompareTo(logo)));
            Assert.That(new SortedSet<TransparencySoftware> { plain, logo }.Count, Is.EqualTo(2));

            var current = Status();
            var version = Status("3.0");
            var certificate = new TransparencySoftwareStatus(Software(), LegalStatus.Verified, Certificate("certificate-2"));
            var fractionalTime = new TransparencySoftwareStatus(Software(), LegalStatus.Verified,
                Certificate("certificate-3", current.Certificate!.NotBefore!.Value.AddTicks(1)));

            Assert.That(new SortedSet<TransparencySoftwareStatus> { current, version, certificate, fractionalTime }.Count, Is.EqualTo(4));
            Assert.That(current.Equals(fractionalTime), Is.False);

        }

        [Test]
        public void License_inputs_and_exposed_license_values_are_copied()
        {

            var description = I18NString.Create("Original");
            var links = new[] { URL.Parse("https://example.org/original") };
            var license = new OpenSourceLicense(OpenSourceLicense_Id.Parse("custom"), description, links);
            var software = Release("2", license);
            var before = software.ToJSON();

            links[0] = URL.Parse("https://example.org/changed");
            description.Set(Languages.en, "Changed");
            software.OpenSourceLicenses.Single().Description.Set(Languages.en, "Changed again");
            Assert.That(JToken.DeepEquals(software.ToJSON(), before), Is.True);
            Assert.That(software.Clone(), Is.EqualTo(software));
            Assert.That(Status().Clone(SoftwareLookup, CertificateLookup), Is.EqualTo(Status()));

        }

        [Test]
        public void Custom_parser_and_serializer_callbacks_propagate_and_null_results_fail()
        {

            var nestedCalls = 0;
            var statusCalls = 0;
            TransparencySoftware.Parse(Software().ToJSON(), CustomTransparencySoftwareParser: (_, value) =>
            {
                nestedCalls++;
                return value;
            });
            var parsed = ParseStatus(Status().ToJSON(), CustomTransparencySoftwareStatusParser: (_, value) =>
            {
                statusCalls++;
                return value;
            });

            Assert.That(nestedCalls, Is.EqualTo(1));
            Assert.That(statusCalls, Is.EqualTo(1));

            var json = parsed.ToJSON(CustomTransparencySoftwareStatusSerializer: (_, document) =>
                                    {

                                        document["extension"] = 1;

                                        return document;

                                    });

            Assert.That(json["extension"]!.Value<int>(), Is.EqualTo(1));
            Assert.That(TransparencySoftware.TryParse(Software().ToJSON(), out var software, out var error, (_, _) => null!), Is.False);
            Assert.That(software, Is.Null);
            Assert.That(error, Does.Contain("returned null"));
            Assert.That(TryParseStatus(Status().ToJSON(), out var status, out error, (_, _) => null!), Is.False);
            Assert.That(status, Is.Null);
            Assert.That(error, Does.Contain("returned null"));

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
            Assert.That(TryParseStatus(null!, out _, out _), Is.False);

        }
    
    }

}
