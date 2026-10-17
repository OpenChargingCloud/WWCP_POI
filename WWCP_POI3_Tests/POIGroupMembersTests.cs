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
    /// What an importer is to change of a member of groups: what a source tells as its groups give it stays in its
    /// groups, by the rules by which they give it at runtime.
    /// </summary>
    [TestFixture]
    public class POIGroupMembersTests
    {

        #region (private static) Kept(Desired, Stored, Effective, Property)

        /// <summary>
        /// The given property of the member as it is to be, after what its groups give is kept in them.
        /// </summary>
        private static JToken? Kept(String Desired, String Stored, String Effective, String Property)
        {

            var desired = JObject.Parse(Desired);

            POIGroupMembers.KeepWhatGroupsGive(desired, JObject.Parse(Stored), JObject.Parse(Effective), [ Property ]);

            return desired[Property];

        }

        #endregion


        #region What_the_groups_give_as_it_is_stays_in_them()

        [Test]
        public void What_the_groups_give_as_it_is_stays_in_them()
        {
            using (Assert.EnterMultipleScope())
            {

                // As it has it with its groups: of its own, nothing, or what it has.
                Assert.That(Kept("""{ "maxPower": 22000 }""",          "{}",                                """{ "maxPower": 22000 }""",                  "maxPower"),     Is.Null);
                Assert.That(Kept("""{ "facilities": [ "CAFE" ] }""",    """{ "facilities": [ "SUPERMARKET" ] }""",  """{ "facilities": [ "CAFE" ] }""",  "facilities")!.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("""["SUPERMARKET"]"""));

                // Another single value is its own.
                Assert.That(Kept("""{ "maxPower": 11000 }""",          "{}",                                """{ "maxPower": 22000 }""",                  "maxPower")!.Value<Int32>(),  Is.EqualTo(11000));

            }
        }

        #endregion

        #region Of_a_united_array_only_what_the_groups_do_not_give_is_its_own()

        [Test]
        public void Of_a_united_array_only_what_the_groups_do_not_give_is_its_own()
        {
            using (Assert.EnterMultipleScope())
            {

                Assert.That(Kept("""{ "facilities": [ "CAFE", "SUPERMARKET" ] }""",  "{}",  """{ "facilities": [ "CAFE" ] }""",  "facilities")!.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("""["SUPERMARKET"]"""));

                // An authentication mode by its type: a source telling an RFID mode without the card types its group gives
                // does not make it one of its own, and the card types of the group stay.
                Assert.That(Kept("""{ "authenticationModes": [ { "type": "RFID" }, { "type": "APP" } ] }""",
                                 "{}",
                                 """{ "authenticationModes": [ { "type": "RFID", "cardTypes": [ "MIFARE_CLASSIC" ] } ] }""",
                                 "authenticationModes")!.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("""[{"type":"APP"}]"""));

                // An image by its URL.
                Assert.That(Kept("""{ "images": [ { "url": "https://example.org/a.png" } ] }""",
                                 "{}",
                                 """{ "images": [ { "url": "https://example.org/a.png", "category": "LOCATION" } ] }""",
                                 "images"),
                            Is.Null);

            }
        }

        #endregion

        #region An_array_taken_whole_is_its_own_whole()

        /// <summary>
        /// The charging tariffs, current types and charging modes of an EVSE are its own, else those of its groups:
        /// told otherwise, they become its own whole, also with what its groups give.
        /// </summary>
        [Test]
        public void An_array_taken_whole_is_its_own_whole()
        {
            using (Assert.EnterMultipleScope())
            {

                Assert.That(Kept("""{ "chargingTariffIds": [ "DE*GEF*T1", "DE*GEF*T2" ] }""",  "{}",  """{ "chargingTariffIds": [ "DE*GEF*T1" ] }""",  "chargingTariffIds")!.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("""["DE*GEF*T1","DE*GEF*T2"]"""));

                Assert.That(Kept("""{ "currentTypes": [ "AC_OnePhase" ] }""",  "{}",  """{ "currentTypes": [ "AC_OnePhase", "AC_ThreePhases" ] }""",  "currentTypes")!.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("""["AC_OnePhase"]"""));

                Assert.That(Kept("""{ "chargingModes": [ "Mode_3" ] }""",  "{}",  """{ "chargingModes": [ "Mode_3" ] }""",  "chargingModes"),
                            Is.Null);

            }
        }

        #endregion

    }

}
