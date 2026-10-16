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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Complex
{

    /// <summary>
    /// An energy information: used and available, each with its unit.
    /// </summary>
    [TestFixture]
    public class EnergyInfoJsonTests
    {

        #region Roundtrip_keeps_both_values_in_any_culture(Culture)

        [TestCase("en-US")]
        [TestCase("de-DE")]
        public void Roundtrip_keeps_both_values_in_any_culture(String Culture)
        {

            var previous = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(Culture);

            try
            {

                var energyInfo  = EnergyInfo.Parse(JObject.Parse("""{ "used": "11.5 kW", "available": "22000 W" }"""));

                Assert.That(energyInfo.Used,                                Is.EqualTo(Watt.ParseKW("11.5")));
                Assert.That(energyInfo.Available,                           Is.EqualTo(Watt.ParseKW("22")));

                var standalone  = energyInfo.ToJSON();
                var embedded    = energyInfo.ToJSON(Embedded: true);

                Assert.That(standalone["@context"]!.Value<String>(),        Is.EqualTo(EnergyInfo.JSONLDContext));
                Assert.That(embedded  ["@context"],                         Is.Null);
                Assert.That(embedded  ["used"]!.Value<String>(),            Does.Not.Contain(","));

                Assert.That(EnergyInfo.Parse(standalone),                   Is.EqualTo(energyInfo));
                Assert.That(EnergyInfo.Parse(embedded),                     Is.EqualTo(energyInfo));

            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }

        }

        #endregion

        #region Stored_energy_is_optional_and_keeps_its_value()

        [Test]
        public void Stored_energy_is_optional_and_keeps_its_value()
        {

            var withoutBuffer  = EnergyInfo.Parse(JObject.Parse("""{ "used": "44 kW", "available": "22 kW" }"""));
            var withBuffer     = EnergyInfo.Parse(JObject.Parse("""{ "used": "44 kW", "available": "22 kW", "storedEnergy": "120 kWh" }"""));

            Assert.That(withoutBuffer.StoredEnergy,                             Is.Null);
            Assert.That(withoutBuffer.ToJSON()["storedEnergy"],                 Is.Null);

            Assert.That(withBuffer.StoredEnergy,                                Is.EqualTo(WattHour.ParseKWh("120")));
            Assert.That(withBuffer,                                             Is.Not.EqualTo(withoutBuffer));
            Assert.That(EnergyInfo.Parse(withBuffer.ToJSON()),                  Is.EqualTo(withBuffer));
            Assert.That(EnergyInfo.Parse(withBuffer.ToJSON()).GetHashCode(),    Is.EqualTo(withBuffer.GetHashCode()));

            Assert.That(EnergyInfo.TryParse(JObject.Parse("""{ "used": "44 kW", "available": "22 kW", "storedEnergy": "full" }"""),
                                            out _,
                                            out var error),                     Is.False);
            Assert.That(error,                                                  Does.Contain("stored energy"));

        }

        #endregion

        #region Current_voltage_and_remaining_charging_time_are_optional_and_keep_their_values()

        [Test]
        public void Current_voltage_and_remaining_charging_time_are_optional_and_keep_their_values()
        {

            var withoutThem  = EnergyInfo.Parse(JObject.Parse("""{ "used": "11 kW", "available": "22 kW" }"""));
            var withThem     = EnergyInfo.Parse(JObject.Parse("""{ "used": "11 kW", "available": "22 kW", "currentVoltage": "400 V", "remainingChargingTime": 1800.5 }"""));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(withoutThem.CurrentVoltage,                                Is.Null);
                Assert.That(withoutThem.RemainingChargingTime,                         Is.Null);
                Assert.That(withoutThem.HasEVSEValues,                                 Is.False);
                Assert.That(withoutThem.ToJSON()["currentVoltage"],                    Is.Null);
                Assert.That(withoutThem.ToJSON()["remainingChargingTime"],             Is.Null);

                Assert.That(withThem.CurrentVoltage,                                   Is.EqualTo(Volt.ParseV("400")));
                Assert.That(withThem.RemainingChargingTime,                            Is.EqualTo(TimeSpan.FromMilliseconds(1800500)));
                Assert.That(withThem.HasEVSEValues,                                    Is.True);
                Assert.That(withThem,                                                  Is.Not.EqualTo(withoutThem));
                Assert.That(withThem.ToJSON()["remainingChargingTime"]!.Value<Decimal>(), Is.EqualTo(1800.5m));
                Assert.That(EnergyInfo.Parse(withThem.ToJSON()),                       Is.EqualTo(withThem));
                Assert.That(EnergyInfo.Parse(withThem.ToJSON()).GetHashCode(),         Is.EqualTo(withThem.GetHashCode()));

            }

        }

        #endregion

        #region Invalid_current_voltage_or_remaining_charging_time_is_rejected(JSON, Error)

        [TestCase("""{ "used": "11 kW", "available": "22 kW", "currentVoltage": "high" }""",        "current voltage")]
        [TestCase("""{ "used": "11 kW", "available": "22 kW", "currentVoltage": "-230 V" }""",      "must not be negative")]
        [TestCase("""{ "used": "11 kW", "available": "22 kW", "remainingChargingTime": "1 h" }""",  "remaining charging time")]
        [TestCase("""{ "used": "11 kW", "available": "22 kW", "remainingChargingTime": -1 }""",     "remaining charging time")]
        public void Invalid_current_voltage_or_remaining_charging_time_is_rejected(String JSON, String Error)
        {

            Assert.That(EnergyInfo.TryParse(JObject.Parse(JSON), out _, out var error),  Is.False);
            Assert.That(error,                                                           Does.Contain(Error));

        }

        #endregion

        #region Invalid_energy_information_is_rejected(JSON, Error)

        [TestCase("""{}""",                                    "The given JSON object must not be null or empty!")]
        [TestCase("""{ "available": "22 kW" }""",              "Missing JSON property 'used'!")]
        [TestCase("""{ "used": "11 kW" }""",                   "Missing JSON property 'available'!")]
        [TestCase("""{ "used": "eleven", "available": "22 kW" }""", "used")]
        public void Invalid_energy_information_is_rejected(String JSON, String Error)
        {

            Assert.That(EnergyInfo.TryParse(JObject.Parse(JSON), out _, out var error),  Is.False);
            Assert.That(error,                                                           Does.Contain(Error));

        }

        #endregion

    }

}
