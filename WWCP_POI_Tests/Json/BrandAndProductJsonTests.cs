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

using cloud.charging.open.protocols.WWCP.POI;

#endregion

namespace WWCP_POI_Tests.Json
{
    [TestFixture]
    public sealed class BrandAndProductJsonTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Brand_roundtrip_preserves_names_links_and_license_representation(bool  embedded,
                                                                                     bool  expanded)
        {

            var source = new Brand(Brand_Id.Parse("brand-a"),
                                                I18NString.Parse(JObject.Parse("{\"de\":\"Marke\",\"en\":\"Brand\"}"))!,
                                                I18NString.Parse(JObject.Parse("{\"en\":\"Description\"}")),
                                                URL.Parse("https://example.org/logo.png"), URL.Parse("https://example.org/"),
                                                [new DataLicense(DataLicense_Id.Parse("license-a"),
                             I18NString.Parse(JObject.Parse("{\"en\":\"License terms\"}"))!,
                             URL.Parse("https://example.org/license"))]);
            var expansion = expanded ? InfoStatus.Expanded : InfoStatus.ShowIdOnly;
            var json = source.ToJSON(embedded, expansion);
            var before = json.DeepClone();
            var parsed = Brand.Parse(json);

            Assert.That(JToken.DeepEquals(json, before), Is.True, "Parsing must not mutate the input JSON.");
            Assert.That(JToken.DeepEquals(parsed.ToJSON(embedded, expansion), json), Is.True);
            Assert.That(parsed.Id, Is.EqualTo(source.Id));
            Assert.That(parsed.DataLicenses.Single().Id, Is.EqualTo(source.DataLicenses.Single().Id));

            if (expanded)
            {
                Assert.That(parsed.DataLicenses.Single().URLs, Is.EqualTo(source.DataLicenses.Single().URLs));
                Assert.That(JToken.DeepEquals(parsed.DataLicenses.Single().Description.ToJSON(),
                                                                                         source.DataLicenses.Single().Description.ToJSON()), Is.True);
            }

        }

        [Test]
        public void Minimal_brand_roundtrip_omits_optional_fields()
        {

            var json = JObject.Parse("{\"id\":\"brand-a\",\"name\":{\"en\":\"Brand\"}}");
            var parsed = Brand.Parse(json);

            Assert.That(parsed.DataLicenses, Is.Empty);
            Assert.That(parsed.Homepage, Is.Null);
            Assert.That(parsed.Logo, Is.Null);
            Assert.That(JToken.DeepEquals(parsed.ToJSON(Embedded: true), json), Is.True);

        }

        [TestCase("{}")]
        [TestCase("{\"id\":\"brand-a\"}")]
        [TestCase("{\"id\":\"brand-a\",\"name\":{}}")]
        [TestCase("{\"id\":\"brand-a\",\"name\":\"Brand\"}")]
        [TestCase("{\"id\":\"brand-a\",\"name\":{\"en\":\"Brand\"},\"dataLicenses\":{}}")]
        [TestCase("{\"id\":\"brand-a\",\"name\":{\"en\":\"Brand\"},\"dataLicenses\":[false]}")]
        [TestCase("{\"id\":\"brand-a\",\"name\":{\"en\":\"Brand\"},\"dataLicenses\":[{}]}")]
        public void Brand_rejects_invalid_json(string text)
        {

            Assert.That(Brand.TryParse(JObject.Parse(text), out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }

        [TestCase("de-DE", false)]
        [TestCase("de-DE", true)]
        [TestCase("en-US", false)]
        [TestCase("en-US", true)]
        public void Product_roundtrip_preserves_all_limits_and_units(string  culture,
                                                                     bool    intermediateCDRs)
        {

            var previous = CultureInfo.CurrentCulture;

            try
            {

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                var source = new ChargingProduct(ChargingProduct_Id.Parse("product-a"),
                                                                TimeSpan.FromSeconds(10.125), TimeSpan.FromSeconds(120.5),
                                                                Watt.Parse("3500.5"), Watt.Parse("22000.25"),
                                                                WattHour.Parse("1500.25"), WattHour.Parse("30000.5"),
                                                                12.3456789m, intermediateCDRs);
                var json = source.ToJSON();
                var parsed = ChargingProduct.Parse(JObject.Parse(json.ToString()));

                Assert.Multiple(() =>
                                                            {

                                                                Assert.That(parsed.Id, Is.EqualTo(source.Id));
                                                                Assert.That(parsed.MinDuration, Is.EqualTo(source.MinDuration));
                                                                Assert.That(parsed.StopChargingAfterTime, Is.EqualTo(source.StopChargingAfterTime));
                                                                Assert.That(parsed.MinPower, Is.EqualTo(source.MinPower));
                                                                Assert.That(parsed.MaxPower, Is.EqualTo(source.MaxPower));
                                                                Assert.That(parsed.MinEnergy, Is.EqualTo(source.MinEnergy));
                                                                Assert.That(parsed.StopChargingAfterKWh, Is.EqualTo(source.StopChargingAfterKWh));
                                                                Assert.That(parsed.MaxB2BServiceCosts, Is.EqualTo(source.MaxB2BServiceCosts));
                                                                Assert.That(parsed.IntermediateCDRs, Is.EqualTo(intermediateCDRs));

                                                            });
                Assert.That(JToken.DeepEquals(parsed.ToJSON(), json), Is.True);

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

        }

        [TestCase(false)]
        [TestCase(true)]
        public void Minimal_product_roundtrip_omits_optional_values(bool embedded)
        {

            var source = new ChargingProduct(ChargingProduct_Id.Parse("product-a"));
            var json = source.ToJSON(embedded);

            Assert.That(JToken.DeepEquals(ChargingProduct.Parse(json).ToJSON(embedded), json), Is.True);

        }

        [TestCase("minDuration")]
        [TestCase("stopChargingAfterTime")]
        [TestCase("minPower")]
        [TestCase("maxPower")]
        [TestCase("minEnergy")]
        [TestCase("stopChargingAfterKWh")]
        [TestCase("maxB2BServiceCosts")]
        [TestCase("intermediateCDRs")]
        public void Product_rejects_invalid_optional_value(string field)
        {

            var json = new JObject(new JProperty("@id", "product-a"), new JProperty(field, new JObject()));

            Assert.That(ChargingProduct.TryParse(json, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }

        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("1e300")]
        public void Product_rejects_nonfinite_or_overflowing_duration(string seconds)
        {

            var json = new JObject(new JProperty("@id", "product-a"), new JProperty("minDuration", seconds));

            Assert.That(ChargingProduct.TryParse(json, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }
    }
}
