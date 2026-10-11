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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Complex
{

    /// <summary>
    /// The energy status of an EVSE: its identification, energy information and timestamp.
    /// </summary>
    [TestFixture]
    public class EVSEEnergyStatusJsonTests
    {

        private static readonly EVSE_Id     EVSEId  = EVSE_Id.Parse("DE*GEF*E1234*1");
        private static readonly EnergyInfo  Info    = new (Watt.ParseKW("11"), Watt.ParseKW("22"));


        #region Roundtrip_keeps_id_energy_information_and_timestamp()

        [Test]
        public void Roundtrip_keeps_id_energy_information_and_timestamp()
        {

            var status  = new EVSEEnergyStatus(EVSEId, Info, DateTimeOffset.Parse("2026-10-11T14:00:00.123+02:00"));
            var json    = status.ToJSON();

            Assert.That(json["timestamp"]!.Value<String>(),            Is.EqualTo("2026-10-11T12:00:00.123Z"));
            Assert.That(json["energyInfo"]!["@context"],               Is.Null);
            Assert.That(EVSEEnergyStatus.Parse(json),                  Is.EqualTo(status));
            Assert.That(EVSEEnergyStatus.Parse(json).GetHashCode(),    Is.EqualTo(status.GetHashCode()));

        }

        #endregion

        #region Missing_energy_information_is_rejected()

        [Test]
        public void Missing_energy_information_is_rejected()
        {

            var json = JObject.Parse("""{ "id": "DE*GEF*E1234*1", "timestamp": "2026-10-11T12:00:00Z" }""");

            Assert.That(EVSEEnergyStatus.TryParse(json, out _, out var error),  Is.False);
            Assert.That(error,                                                  Does.Contain("energyInfo"));

        }

        #endregion

        #region Equality_follows_the_millisecond_of_the_instant()

        /// <summary>
        /// A status equals itself after a JSON roundtrip, which keeps milliseconds only:
        /// equality, order and hash code all follow the millisecond of the instant.
        /// </summary>
        [Test]
        public void Equality_follows_the_millisecond_of_the_instant()
        {

            var timestamp  = DateTimeOffset.Parse("2026-10-11T12:00:00.1234567Z");

            var a          = new EVSEEnergyStatus(EVSEId, Info, timestamp);
            var subMilli   = new EVSEEnergyStatus(EVSEId, Info, timestamp.AddTicks(2000));
            var offset     = new EVSEEnergyStatus(EVSEId, Info, timestamp.ToOffset(TimeSpan.FromHours(2)));
            var nextMilli  = new EVSEEnergyStatus(EVSEId, Info, timestamp.AddMilliseconds(1));
            var roundtrip  = EVSEEnergyStatus.Parse(a.ToJSON());

            foreach (var same in new[] { subMilli, offset, roundtrip })
            {
                Assert.That(same,                Is.EqualTo(a));
                Assert.That(same.CompareTo(a),   Is.Zero);
                Assert.That(same.GetHashCode(),  Is.EqualTo(a.GetHashCode()));
            }

            Assert.That(nextMilli,               Is.Not.EqualTo(a));
            Assert.That(nextMilli.CompareTo(a),  Is.Positive);

        }

        #endregion

        #region Status_types_follow_the_millisecond_of_the_instant()

        [Test]
        public void Status_types_follow_the_millisecond_of_the_instant()
        {

            var timestamp  = DateTimeOffset.Parse("2026-10-11T12:00:00.1234567Z");

            var a          = new EVSEStatus(EVSEId, EVSEStatusType.Available, timestamp);
            var b          = new EVSEStatus(EVSEId, EVSEStatusType.Available, timestamp.AddTicks(2000));

            Assert.That(b,                Is.EqualTo(a));
            Assert.That(b.GetHashCode(),  Is.EqualTo(a.GetHashCode()));

        }

        #endregion

        #region Status_lists_keep_the_newest_status_per_EVSE()

        [Test]
        public void Status_lists_keep_the_newest_status_per_EVSE()
        {

            var older  = new EVSEEnergyStatus(EVSEId, new EnergyInfo(Watt.ParseKW("1"), Watt.ParseKW("2")), DateTimeOffset.Parse("2026-10-11T12:00:00Z"));
            var newer  = new EVSEEnergyStatus(EVSEId, Info,                                                DateTimeOffset.Parse("2026-10-11T13:00:00Z"));

            foreach (var list in new[] { new[] { older, newer }, new[] { newer, older } })
            {

                var entry = list.ToJSON()[EVSEId.ToString()] as JArray;

                Assert.That(entry![0]!.Value<String>(),                     Is.EqualTo("2026-10-11T13:00:00.000Z"));
                Assert.That(EnergyInfo.Parse((JObject) entry[1]!),          Is.EqualTo(Info));

            }

        }

        #endregion

    }

}
