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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP.POI;

#endregion

namespace WWCP_POI_Tests.Json
{
    [TestFixture]
    public sealed class LocationAndImageJsonTests
    {
        [TestCase("de-DE")]
        [TestCase("en-US")]
        [TestCase("fr-FR")]
        public void Location_roundtrip_preserves_altitude_and_multilingual_name_across_cultures(string culture)
        {

            var previous = CultureInfo.CurrentCulture;

            try
            {

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                var source = new AdditionalGeoLocation(Latitude.Parse(52.52), Longitude.Parse(13.405),
                                                                Altitude.Parse(42.75), I18NString.Parse(JObject.Parse("{\"de\":\"Einfahrt\",\"en\":\"Entrance\"}")));
                var json = source.ToJSON();

                Assert.That(json["latitude"]!.Value<string>(), Is.EqualTo("52.52"));
                Assert.That(json["longitude"]!.Value<string>(), Is.EqualTo("13.405"));

                var parsed = AdditionalGeoLocation.Parse(JObject.Parse(json.ToString()));

                Assert.That(parsed.GeoLocation, Is.EqualTo(source.GeoLocation));
                Assert.That(parsed.GeoLocation.Altitude, Is.EqualTo(source.GeoLocation.Altitude));
                Assert.That(JsonViews.EqualViews(parsed.Name!.ToJSON(), source.Name!.ToJSON()), Is.True);

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

        }

        [Test]
        public void Minimal_location_accepts_numeric_coordinates_and_omits_optional_fields()
        {

            var parsed = AdditionalGeoLocation.Parse(JObject.Parse("{\"latitude\":0,\"longitude\":0}"));

            Assert.That(parsed.GeoLocation.Altitude, Is.Null);
            Assert.That(parsed.Name, Is.Null);
            Assert.That(parsed.ToJSON().ContainsKey("altitude"), Is.False);

        }

        [TestCase("{}")]
        [TestCase("{\"latitude\":1}")]
        [TestCase("{\"latitude\":91,\"longitude\":0}")]
        [TestCase("{\"latitude\":0,\"longitude\":181}")]
        [TestCase("{\"latitude\":0,\"longitude\":0,\"altitude\":\"invalid\"}")]
        [TestCase("{\"latitude\":\"NaN\",\"longitude\":0}")]
        [TestCase("{\"latitude\":0,\"longitude\":\"Infinity\"}")]
        [TestCase("{\"latitude\":0,\"longitude\":0,\"altitude\":\"NaN\"}")]
        [TestCase("{\"latitude\":\"0,005\",\"longitude\":0}")]
        [TestCase("{\"latitude\":0,\"longitude\":\"0,005\"}")]
        public void Location_rejects_invalid_input(string text)
        {

            Assert.That(AdditionalGeoLocation.TryParse(JObject.Parse(text), out _, out var error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }

        [TestCase(false)]
        [TestCase(true)]
        public void Image_roundtrip_preserves_all_fields(bool optional)
        {

            var json = JObject.Parse("{\"url\":\"https://example.org/image.png\",\"category\":\"station\",\"type\":\"image/png\"}");

            if (optional)
            {
                json["width"] = 640;
                json["height"] = 480;
                json["thumbnail"] = "https://example.org/thumb.png";
            }

            var source = Image.Parse(json);
            var parsed = Image.Parse(JObject.Parse(source.ToJSON().ToString()));

            Assert.That(parsed, Is.EqualTo(source));
            Assert.That(JsonViews.EqualViews(parsed.ToJSON(), json), Is.True);

        }

        [TestCase("url")]
        [TestCase("type")]
        [TestCase("category")]
        public void Image_rejects_missing_mandatory_field(string field)
        {

            var json = JObject.Parse("{\"url\":\"https://example.org/image.png\",\"category\":\"station\",\"type\":\"image/png\"}");

            json.Remove(field);
            Assert.That(Image.TryParse(json, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }

        [TestCase("width", -1)]
        [TestCase("height", 65536)]
        public void Image_rejects_out_of_range_dimensions(string  field,
                                                          int     value)
        {

            var json = JObject.Parse("{\"url\":\"https://example.org/image.png\",\"category\":\"station\",\"type\":\"image/png\"}");

            json[field] = value;
            Assert.That(Image.TryParse(json, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }

        [TestCase("width")]
        [TestCase("height")]
        public void Image_rejects_fractional_dimensions(string field)
        {

            var json = JObject.Parse("{\"url\":\"https://example.org/image.png\",\"category\":\"station\",\"type\":\"image/png\"}");

            json[field] = 1.5;
            Assert.That(Image.TryParse(json, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }
    }
}
