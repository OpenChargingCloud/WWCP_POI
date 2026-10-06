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
    public sealed class ChargingHardwareJsonTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void Cable_roundtrip_preserves_units_and_compensation(bool embedded)
        {

            var source = new ChargingCable(Meter.From_m(5.25m), Ohm.Parse_µΩ("1250.5"), "Calibration A", long.MaxValue);
            var json = source.ToJSON(embedded)!;

            Assert.That(json["resistance"]!.Value<decimal>(), Is.EqualTo(1250.5m));
            Assert.That(json.ContainsKey("@context"), Is.EqualTo(!embedded));

            var parsed = ChargingCable.Parse(JObject.Parse(json.ToString()));

            Assert.Multiple(() =>
                                            {

                                                Assert.That(parsed.Length, Is.EqualTo(source.Length));
                                                Assert.That(parsed.Resistance, Is.EqualTo(source.Resistance));
                                                Assert.That(parsed.LossCompensationName, Is.EqualTo(source.LossCompensationName));
                                                Assert.That(parsed.LossCompensationIdentification, Is.EqualTo(long.MaxValue));

                                            });

        }

        [TestCase(false)]
        [TestCase(true)]
        public void Empty_cable_roundtrip_is_valid(bool embedded)
        {

            var json = new ChargingCable().ToJSON(embedded)!;

            Assert.That(ChargingCable.TryParse(json, out var parsed, out var error), Is.True, error);
            Assert.That(parsed!.Length, Is.Null);
            Assert.That(parsed.Resistance, Is.Null);

        }

        [TestCase("{\"length\":\"invalid\"}")]
        [TestCase("{\"resistance\":[]}")]
        [TestCase("{\"lossCompensationIdentification\":\"invalid\"}")]
        [TestCase("{\"lossCompensationName\":{}}")]
        [TestCase("{\"lossCompensationName\":42}")]
        [TestCase("{\"lossCompensationIdentification\":1.5}")]
        public void Cable_rejects_malformed_fields(string text)
        {

            Assert.That(ChargingCable.TryParse(JObject.Parse(text), out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }

        [Test]
        public void Cable_zero_values_are_preserved()
        {

            var source = new ChargingCable(Meter.From_m(0), Ohm.Parse_µΩ("0"), LossCompensationIdentification: 0);
            var parsed = ChargingCable.Parse(source.ToJSON()!);

            Assert.That(parsed.Length, Is.EqualTo(source.Length));
            Assert.That(parsed.Resistance, Is.EqualTo(source.Resistance));
            Assert.That(parsed.LossCompensationIdentification, Is.Zero);

        }

        [Test]
        public void Null_cable_input_is_rejected_and_parse_reports_the_error()
        {

            Assert.That(ChargingCable.TryParse(null!, out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
            Assert.That(() => ChargingCable.Parse(null!), Throws.ArgumentException);

        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Connector_roundtrip_preserves_cable_and_lockable(bool  embedded,
                                                                     bool  lockable)
        {

            var source = new ChargingConnector(ChargingConnector_Id.Parse(2), ChargingConnectorType.Parse("CCS"),
                                                                               new ChargingCable(Meter.From_m(3.5m), Ohm.Parse_µΩ("750")), lockable);
            var json = source.ToJSON(embedded)!;

            Assert.That(json["lockable"]!.Value<bool>(), Is.EqualTo(lockable));
            Assert.That(json.ContainsKey("@context"), Is.EqualTo(!embedded));
            Assert.That(((JObject)json["cable"]!).ContainsKey("@context"), Is.False);

            var parsed = ChargingConnector.Parse(JObject.Parse(json.ToString()));

            Assert.Multiple(() =>
                                            {

                                                Assert.That(parsed.Id, Is.EqualTo(source.Id));
                                                Assert.That(parsed.Type, Is.EqualTo(source.Type));
                                                Assert.That(parsed.Lockable, Is.EqualTo(lockable));
                                                Assert.That(parsed.ChargingCable!.Length, Is.EqualTo(source.ChargingCable!.Length));
                                                Assert.That(parsed.ChargingCable.Resistance, Is.EqualTo(source.ChargingCable.Resistance));

                                            });

        }

        [Test]
        public void Minimal_connector_omits_optional_fields()
        {

            var source = new ChargingConnector(ChargingConnectorType.Parse("Type2"));
            var json = source.ToJSON()!;

            Assert.That(json.ContainsKey("lockable"), Is.False);
            Assert.That(json.ContainsKey("cable"), Is.False);
            Assert.That(ChargingConnector.Parse(json).Lockable, Is.Null);

        }

        [Test]
        public void Connector_forwards_custom_nested_cable_parser()
        {

            var json = new ChargingConnector(ChargingConnectorType.Parse("Type2"), new ChargingCable(Meter.From_m(1))).ToJSON()!;
            var calls = 0;
            var parsed = ChargingConnector.Parse(json, CustomChargingCableParser: (_, cable) =>
                                            {

                                                calls++;

                                                return new ChargingCable(cable.Length, LossCompensationName: "custom");

                                            });

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(parsed.ChargingCable!.LossCompensationName, Is.EqualTo("custom"));

        }

        [Test]
        public void Connector_roundtrip_preserves_tariff_references_and_terms_url()
        {

            var json = JObject.Parse("{\"@id\":\"1\",\"type\":\"Type2\",\"tariffIds\":[\"DE*ABC*Ttariff-a\",\"DE*ABC*Ttariff-b\"],\"termsAndConditions\":\"https://example.org/terms\"}");
            var source = ChargingConnector.Parse(json);
            var restored = ChargingConnector.Parse(JObject.Parse(source.ToJSON()!.ToString()));

            Assert.That(restored.TariffIds, Is.EquivalentTo(source.TariffIds));
            Assert.That(restored.TermsAndConditions, Is.EqualTo(source.TermsAndConditions));

        }

        [TestCase("{}")]
        [TestCase("{\"@id\":\"1\"}")]
        [TestCase("{\"type\":\"Type2\"}")]
        [TestCase("{\"@id\":\" \",\"type\":\"Type2\"}")]
        [TestCase("{\"@id\":\"1\",\"type\":\"Type2\",\"cable\":{\"length\":\"invalid\"}}")]
        public void Connector_rejects_invalid_input(string text)
        {

            Assert.That(ChargingConnector.TryParse(JObject.Parse(text), out var parsed, out var error), Is.False);
            Assert.That(parsed, Is.Null);
            Assert.That(error, Is.Not.Null.And.Not.Empty);

        }
    }
}
