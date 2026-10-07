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

using cloud.charging.open.protocols.WWCP.POI;

#endregion

namespace WWCP_POI_Tests.Json
{
    [TestFixture]
    public sealed class EVSEJsonTests
    {
        internal static ChargingStation Station() => new(ChargingStation_Id.Parse("DE*ABC*S1"));

        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        public void Embedded_EVSE_roundtrip_preserves_hardware_and_decimal_limits(string culture)
        {

            var previous = CultureInfo.CurrentCulture;

            try
            {

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);

                var station = Station();
                var source = new EVSE(EVSE_Id.Parse("DE*ABC*E1"), station,
                                                                Name: I18NString.Create("Socket 1"), PhysicalReference: "Bay 7",
                                                                CurrentType: CurrentTypes.DC,
                                                                ChargingModes: [ChargingModes.Mode_3 | ChargingModes.Mode_4],
                                                                MaxVoltage: Volt.Parse("800.125"), MaxCurrent: Ampere.Parse("125.625"),
                                                                MaxPower: Watt.Parse("100000.125"), MaxCapacity: WattHour.Parse("0"),
                                                                IsFreeOfCharge: true, DataSource: "inventory",
                                                                ChargingConnectors: [new ChargingConnector(ChargingConnector_Id.Parse(1), ChargingConnectorType.Parse("CCS"),
                    new ChargingCable(Meter.From_m(5.25m)), false)],
                                                                CustomData: CustomDataNew.ParseJSON("{\"vendor\":{\"serial\":\"A1\"}}"));
                var json = source.ToJSON(Embedded: true, IncludeCustomData: true)!;

                Assert.That(json["exception"], Is.Null, json.ToString());

                var before = json.DeepClone();
                var parsed = EVSE.Parse(JObject.Parse(json.ToString()), station);

                Assert.That(parsed.ChargingStation, Is.SameAs(station));
                Assert.That(parsed.ChargingConnectors.Single().EVSE, Is.SameAs(parsed));
                Assert.That(station.EVSEs, Is.Empty, "Parsing does not mutate the supplied station.");
                Assert.That(parsed.MaxPower, Is.EqualTo(source.MaxPower));
                Assert.That(parsed.MaxVoltage, Is.EqualTo(source.MaxVoltage));
                Assert.That(parsed.MaxCapacity, Is.EqualTo(source.MaxCapacity));
                Assert.That(JToken.DeepEquals(parsed.ToJSON(Embedded: true, IncludeCustomData: true), json), Is.True);
                Assert.That(JToken.DeepEquals(json, before), Is.True);

            }
            finally
            {

                CultureInfo.CurrentCulture = previous;

            }

        }

        [Test]
        public void Nested_mode_arrays_are_rejected()
        {

            var json = JObject.Parse("{\"@id\":\"DE*ABC*E1\",\"currentType\":[\"AC_OnePhase\",\"DC\"],\"chargingModes\":[[\"Mode_3\",\"Mode_4\"]]}");
            Assert.That(EVSE.TryParse(json, Station(), out _, out _), Is.False);

        }

        [TestCase("{}")]
        [TestCase("{\"@id\":\"invalid\"}")]
        [TestCase("{\"@id\":\"DE*ABC*E1\",\"currentType\":[\"future\"]}")]
        [TestCase("{\"@id\":\"DE*ABC*E1\",\"maxPower\":-1}")]
        [TestCase("{\"@id\":\"DE*ABC*E1\",\"maxCurrent\":true}")]
        [TestCase("{\"@id\":\"DE*ABC*E1\",\"chargingModes\":[42]}")]
        [TestCase("{\"@id\":\"DE*ABC*E1\",\"chargingStationId\":\"DE*ABC*S2\"}")]
        [TestCase("{\"@id\":\"DE*ABC*E1\",\"socketOutlets\":[{}]}")]
        [TestCase("{\"@id\":\"DE*ABC*E1\",\"isFreeOfCharge\":\"true\"}")]
        public void Invalid_EVSE_fails_atomically(string text)
        {

            var station = Station();

            Assert.That(EVSE.TryParse(JObject.Parse(text), station, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Empty);
            Assert.That(station.EVSEs, Is.Empty);

        }

        [Test]
        public void Duplicate_connector_ids_are_rejected()
        {

            var connector = new ChargingConnector(ChargingConnector_Id.Parse(1), ChargingConnectorType.Parse("CCS")).ToJSON(true)!;
            var json = new JObject(new JProperty("@id", "DE*ABC*E1"), new JProperty("socketOutlets", new JArray(connector, connector.DeepClone())));

            Assert.That(EVSE.TryParse(json, Station(), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("socketOutlets").And.Contain("duplicate"));

        }

        [TestCase("DE*ABC*E1")]
        [TestCase("DE*ABC*E123")]
        public void Parse_and_TryParse_agree_on_the_default_ID_mode(string text)
        {

            Assert.That(EVSE_Id.TryParse(text, out var id), Is.True);
            Assert.That(id, Is.EqualTo(EVSE_Id.Parse(text)));

        }

        [Test]
        public void Brand_reference_resolution_preserves_metadata()
        {

            var json = JObject.Parse("{\"@id\":\"DE*ABC*E1\",\"brandId\":[\"brand-a\"]}");
            var document = JObject.Parse("{\"id\":\"brand-a\",\"name\":{\"en\":\"Brand A\"}}");
            var context = new InfrastructureJsonParsingContext { ResolveBrand = _ => document };
            var parsed = EVSE.Parse(json, Station(), Context: context);

            Assert.That(parsed.Brands.Single().Name.ToJSON()["en"]!.Value<string>(), Is.EqualTo("Brand A"));
            Assert.That(EVSE.TryParse(json, Station(), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("brandId[0]"));

        }

        [Test]
        public void Different_operator_ID_is_rejected_even_without_explicit_parent_ID()
        {

            Assert.That(EVSE.TryParse(JObject.Parse("{\"@id\":\"DE*DEF*E1\"}"), Station(), out _, out var error), Is.False);
            Assert.That(error, Does.Contain("operator"));

        }
    }
}
