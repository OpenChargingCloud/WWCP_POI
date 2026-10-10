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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Complex
{

    [TestFixture]
    public sealed class ImageJsonTests
    {

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
            Assert.That(JToken.DeepEquals(parsed.ToJSON(), json), Is.True);

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
