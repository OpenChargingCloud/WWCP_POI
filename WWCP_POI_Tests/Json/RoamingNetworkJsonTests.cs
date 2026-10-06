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

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP.POI;

#endregion

namespace WWCP_POI_Tests.Json
{
    [TestFixture]
    public sealed class RoamingNetworkJsonTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Metadata_roundtrip_preserves_multilingual_text_and_source(bool embedded)
        {

            var source = new RoamingNetwork(RoamingNetwork_Id.Parse("network-a"),
                                                I18NString.Parse(JObject.Parse("{\"de\":\"Verbund\",\"en\":\"Network\"}")),
                                                I18NString.Parse(JObject.Parse("{\"de\":\"Beschreibung\"}")),
                                                DataSource: "source-a");
            var json = source.ToJSON(Embedded: embedded);
            var before = json.DeepClone();
            var parsed = RoamingNetwork.Parse(json);

            Assert.That(parsed.Id, Is.EqualTo(source.Id));
            Assert.That(JToken.DeepEquals(parsed.ToJSON(Embedded: embedded), json), Is.True);
            Assert.That(JToken.DeepEquals(json, before), Is.True);

        }

        [Test]
        public void Unnamed_network_roundtrips()
        {

            var json = new RoamingNetwork(RoamingNetwork_Id.Parse("network-a")).ToJSON();

            Assert.That(JToken.DeepEquals(RoamingNetwork.Parse(json).ToJSON(), json), Is.True);

        }

        [Test]
        public void License_identifiers_roundtrip()
        {

            var json = JObject.Parse("{\"@id\":\"network-a\",\"name\":{},\"dataLicenseIds\":[\"license-a\"]}");
            var parsed = RoamingNetwork.Parse(json);

            Assert.That(parsed.DataLicenses.Single().Id.ToString(), Is.EqualTo("license-a"));
            Assert.That(JToken.DeepEquals(parsed.ToJSON(Embedded: true), json), Is.True);

        }

        [TestCase("{}")]
        [TestCase("{\"@id\":\"network-a\"}")]
        [TestCase("{\"@id\":\"network-a\",\"name\":\"invalid\"}")]
        [TestCase("{\"@id\":\"network-a\",\"name\":{},\"@context\":\"wrong\"}")]
        [TestCase("{\"@id\":\"network-a\",\"name\":{},\"dataSource\":42}")]
        [TestCase("{\"@id\":\"network-a\",\"name\":{},\"dataLicenseIds\":[false]}")]
        [TestCase("{\"@id\":\"network-a\",\"name\":{},\"dataLicenseIds\":[\"a\",\"a\"]}")]
        [TestCase("{\"@id\":\"network-a\",\"name\":{},\"chargingStations\":[{}]}")]
        [TestCase("{\"@id\":\"network-a\",\"name\":{},\"EVSEIds\":{}}")]
        public void Invalid_or_unresolved_data_fails_without_a_partial_result(string text)
        {

            var json = JObject.Parse(text);

            Assert.That(RoamingNetwork.TryParse(json, out var network, out var error), Is.False);
            Assert.That(network, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.Throws<ArgumentException>(() => RoamingNetwork.Parse(json));

        }

        [Test]
        public void Null_input_and_null_custom_result_fail()
        {

            Assert.That(RoamingNetwork.TryParse(null!, out _, out var error), Is.False);
            Assert.That(error, Is.Not.Empty);

            var json = JObject.Parse("{\"@id\":\"network-a\",\"name\":{}}");

            Assert.That(RoamingNetwork.TryParse(json, out var result, out error, (_, _) => null!), Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Not.Empty);

        }

        [Test]
        public void Custom_parser_receives_the_parsed_network()
        {

            var json = JObject.Parse("{\"@id\":\"network-a\",\"name\":{}}");
            var called = false;
            var parsed = RoamingNetwork.Parse(json, (document, network) =>
                                            {

                                                called = true;
                                                Assert.That(document, Is.SameAs(json));
                                                Assert.That(network.Id.ToString(), Is.EqualTo("network-a"));

                                                return network;

                                            });

            Assert.That(called, Is.True);
            Assert.That(parsed, Is.Not.Null);

        }
    }
}
