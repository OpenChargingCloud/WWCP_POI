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

using NUnit.Framework;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Ids
{

    /// <summary>
    /// Identifications that are equal must have equal hash codes,
    /// or hash sets and dictionaries will not find them.
    /// </summary>
    [TestFixture]
    public class IdHashCodeTests
    {

        #region (private static) AssertOneKey(First, Second)

        private static void AssertOneKey<T>(T First, T Second)
            where T : notnull
        {

            Assert.That(First,                              Is.EqualTo(Second));
            Assert.That(First.GetHashCode(),                Is.EqualTo(Second.GetHashCode()));
            Assert.That(new HashSet<T> { First, Second },   Has.Count.EqualTo(1));

        }

        #endregion


        #region Suffixes_ignore_case_and_stars_in_their_hash_codes()

        /// <summary>
        /// Suffixes that differ only in case or in stars are equal and must hash equal.
        /// </summary>
        [Test]
        public void Suffixes_ignore_case_and_stars_in_their_hash_codes()
        {

            AssertOneKey(EVSE_Id.           Parse("DE*GEF*Eab*c1"), EVSE_Id.           Parse("DE*GEF*EABC1"));
            AssertOneKey(ChargingPool_Id.   Parse("DE*GEF*Pab*c1"), ChargingPool_Id.   Parse("DE*GEF*PABC1"));
            AssertOneKey(ChargingStation_Id.Parse("DE*GEF*Sab*c1"), ChargingStation_Id.Parse("DE*GEF*SABC1"));
            AssertOneKey(ChargingTariff_Id. Parse("DE*GEF*Tab*c1"), ChargingTariff_Id. Parse("DE*GEF*TABC1"));

        }

        #endregion

        #region Hash_codes_do_not_depend_on_the_current_culture()

        /// <summary>
        /// In Turkish, "I" lowers to a dotless "ı", which once split
        /// "I" and "i" into different hash codes although they are equal.
        /// </summary>
        [Test]
        [SetCulture("tr-TR")]
        public void Hash_codes_do_not_depend_on_the_current_culture()
        {

            AssertOneKey(RoamingNetwork_Id.             Parse("INDEX"),       RoamingNetwork_Id.             Parse("index"));
            AssertOneKey(ChargingConnector_Id.          Parse("I1"),          ChargingConnector_Id.          Parse("i1"));
            AssertOneKey(EnergyMeter_Id.                Parse("METER-I"),     EnergyMeter_Id.                Parse("meter-i"));
            AssertOneKey(ParkingOperator_Id.            Parse("PARKING-I"),   ParkingOperator_Id.            Parse("parking-i"));
            AssertOneKey(ChargingStationManufacturer_Id.Parse("MAKER-I"),     ChargingStationManufacturer_Id.Parse("maker-i"));
            AssertOneKey(EVSE_Id.                       Parse("DE*GEF*EI1"),  EVSE_Id.                       Parse("DE*GEF*Ei1"));

        }

        #endregion

    }

}
