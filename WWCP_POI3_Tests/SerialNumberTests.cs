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

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The serial number of an energy meter, and of a charging station or an energy meter a QI-Digital document names, is
    /// a SerialNumber: read as it is given, and refused where it is read when it is empty or begins or ends with whitespace.
    /// </summary>
    [TestFixture]
    public class SerialNumberTests
    {

        private static readonly DateTimeOffset Created = DateTimeOffset.Parse("2026-01-01T00:00:00.000Z");

        #region Energy_meters_read_their_serial_number_as_it_is_given()

        [Test]
        public void Energy_meters_read_their_serial_number_as_it_is_given()
        {

            var meter = new EnergyMeter(EnergyMeter_Id.Parse("meter-1"), SerialNumber: SerialNumber.Parse("m500-000815"), Created: Created);

            JObject WithSerialNumber(String Text)
            {
                var json = meter.ToJSON();
                json["serialNumber"] = Text;
                return json;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(meter.ToJSON()["serialNumber"]?.Value<String>(),                     Is.EqualTo("m500-000815"));
                Assert.That(EnergyMeter.TryParse(meter.ToJSON(), out var parsed, out _),         Is.True);
                Assert.That(parsed!.SerialNumber,                                                Is.EqualTo(SerialNumber.Parse("m500-000815")));
                Assert.That(parsed. SerialNumber,                                                Is.Not.EqualTo(SerialNumber.Parse("M500-000815")));

                Assert.That(EnergyMeter.TryParse(WithSerialNumber(" M-1"), out _, out var error1), Is.False);
                Assert.That(error1,                                                              Does.Contain("Invalid serial number ' M-1'"));
                Assert.That(EnergyMeter.TryParse(WithSerialNumber(""),     out _, out var error2), Is.False);
                Assert.That(error2,                                                              Does.Contain("Invalid serial number ''"));

            }

        }

        #endregion

        #region QIDigital_items_read_their_serial_number_as_it_is_given()

        [Test]
        public void QIDigital_items_read_their_serial_number_as_it_is_given()
        {

            var item = QIDigitalItem.Of(new EnergyMeter(EnergyMeter_Id.Parse("meter-1"), SerialNumber: SerialNumber.Parse("M500-000815"), Created: Created));

            JObject WithSerialNumber(String Text)
            {
                var json = item.ToJSON();
                json["serialNumber"] = Text;
                return json;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(item.ToJSON()["serialNumber"]?.Value<String>(),                      Is.EqualTo("M500-000815"));
                Assert.That(QIDigitalItem.TryParse(item.ToJSON(), out var parsed, out _),        Is.True);
                Assert.That(parsed!.SerialNumber,                                                Is.EqualTo(SerialNumber.Parse("M500-000815")));

                Assert.That(QIDigitalItem.TryParse(WithSerialNumber("M500-000815 "), out _, out var error), Is.False);
                Assert.That(error,                                                               Does.Contain("Invalid serial number 'M500-000815 '"));

            }

        }

        #endregion

    }

}
