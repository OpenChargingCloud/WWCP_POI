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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Ids
{

    /// <summary>
    /// All identifications are built alike: readonly structs implementing IId&lt;T&gt;,
    /// whose CompareTo, Equals and GetHashCode follow one and the same rule.
    /// </summary>
    [TestFixture]
    public class IdConsistencyTests
    {

        #region (private static) AssertSame(First, Second)

        private static void AssertSame<T>(T First, T Second)
            where T : struct, IId<T>
        {

            Assert.That(First,                              Is.EqualTo(Second));
            Assert.That(First.CompareTo(Second),            Is.Zero);
            Assert.That(First.GetHashCode(),                Is.EqualTo(Second.GetHashCode()));
            Assert.That(new HashSet<T> { First, Second },   Has.Count.EqualTo(1));

        }

        #endregion

        #region (private static) AssertDifferent(First, Second)

        private static void AssertDifferent<T>(T First, T Second)
            where T : struct, IId<T>
        {

            Assert.That(First,                              Is.Not.EqualTo(Second));
            Assert.That(First.CompareTo(Second),            Is.Not.Zero);
            Assert.That(new HashSet<T> { First, Second },   Has.Count.EqualTo(2));

        }

        #endregion


        #region Every_identification_is_a_readonly_struct_implementing_IId_of_itself()

        /// <summary>
        /// No identification is a class, and every one implements IId&lt;T&gt; of itself.
        /// </summary>
        [Test]
        public void Every_identification_is_a_readonly_struct_implementing_IId_of_itself()
        {

            var ids = typeof(EVSE_Id).Assembly.GetTypes().
                          Where (type => type.IsPublic && type.Name.EndsWith("_Id")).
                          ToArray();

            Assert.That(ids, Has.Length.GreaterThanOrEqualTo(30));

            foreach (var id in ids)
            {
                Assert.That(id.IsValueType,                                                             Is.True, id.Name);
                Assert.That(id.IsDefined(typeof(System.Runtime.CompilerServices.IsReadOnlyAttribute), false), Is.True, id.Name);
                Assert.That(typeof(IId<>).MakeGenericType(id).IsAssignableFrom(id),                     Is.True, id.Name);
            }

        }

        #endregion

        #region Groups_ignore_case_like_their_members()

        /// <summary>
        /// Group suffixes ignore case, like the suffixes of EVSEs, stations, pools and tariffs.
        /// </summary>
        [Test]
        public void Groups_ignore_case_like_their_members()
        {

            AssertSame(EVSEGroup_Id.           Parse("DE*GEF*EGabc"), EVSEGroup_Id.           Parse("DE*GEF*EGABC"));
            AssertSame(ChargingStationGroup_Id.Parse("DE*GEF*SGabc"), ChargingStationGroup_Id.Parse("DE*GEF*SGABC"));
            AssertSame(ChargingPoolGroup_Id.   Parse("DE*GEF*PGabc"), ChargingPoolGroup_Id.   Parse("DE*GEF*PGABC"));
            AssertSame(ChargingTariffGroup_Id. Parse("DE*GEF*TGabc"), ChargingTariffGroup_Id. Parse("DE*GEF*TGABC"));

            AssertDifferent(EVSEGroup_Id.Parse("DE*GEF*EGabc"), EVSEGroup_Id.Parse("DE*GEF*EGabd"));

        }

        #endregion

        #region Operators_providers_and_parking_ignore_case()

        /// <summary>
        /// Operator, provider and parking identifications ignore case as well.
        /// </summary>
        [Test]
        public void Operators_providers_and_parking_ignore_case()
        {

            AssertSame(GridOperator_Id.           Parse(Country.Germany, "grd"), GridOperator_Id.           Parse("DE*GRD"));
            AssertSame(ChargingStationOperator_Id.Parse(Country.Germany, "gef"), ChargingStationOperator_Id.Parse("DE*GEF"));
            AssertSame(EMobilityProvider_Id.      Parse("de-gdf"),               EMobilityProvider_Id.      Parse("DE-GDF"));

            AssertSame(ParkingGarage_Id.     Parse("Garage-1"), ParkingGarage_Id.     Parse("GARAGE-1"));
            AssertSame(ParkingSensor_Id.     Parse("Sensor-1"), ParkingSensor_Id.     Parse("SENSOR-1"));
            AssertSame(ParkingSensorGroup_Id.Parse("Group-1"),  ParkingSensorGroup_Id.Parse("GROUP-1"));
            AssertSame(ParkingSpace_Id.      Parse("Space-1"),  ParkingSpace_Id.      Parse("SPACE-1"));
            AssertSame(ParkingSpaceGroup_Id. Parse("Group-1"),  ParkingSpaceGroup_Id. Parse("GROUP-1"));

            AssertDifferent(GridOperator_Id.Parse("DE*GRD"), GridOperator_Id.Parse("DE*GRE"));
            AssertDifferent(ParkingSpace_Id.Parse("Space-1"), ParkingSpace_Id.Parse("Space-2"));

        }

        #endregion

        #region Stars_in_suffixes_are_ignored_by_CompareTo_as_by_Equals()

        /// <summary>
        /// Where Equals ignores stars in a suffix, CompareTo must ignore them too,
        /// or sorted sets would keep two identifications that are equal.
        /// </summary>
        [Test]
        public void Stars_in_suffixes_are_ignored_by_CompareTo_as_by_Equals()
        {

            AssertSame(EVSE_Id.           Parse("DE*GEF*Eab*c1"), EVSE_Id.           Parse("DE*GEF*EABC1"));
            AssertSame(ChargingStation_Id.Parse("DE*GEF*Sab*c1"), ChargingStation_Id.Parse("DE*GEF*SABC1"));
            AssertSame(ChargingPool_Id.   Parse("DE*GEF*Pab*c1"), ChargingPool_Id.   Parse("DE*GEF*PABC1"));
            AssertSame(ChargingTariff_Id. Parse("DE*GEF*Tab*c1"), ChargingTariff_Id. Parse("DE*GEF*TABC1"));

            Assert.That(new SortedSet<ChargingStation_Id> { ChargingStation_Id.Parse("DE*GEF*Sab*c1"),
                                                            ChargingStation_Id.Parse("DE*GEF*SABC1") },
                        Has.Count.EqualTo(1));

        }

        #endregion

        #region Identifications_sort_by_their_text_not_by_their_length()

        /// <summary>
        /// Identifications sort by operator, then by suffix, not by length first.
        /// </summary>
        [Test]
        public void Identifications_sort_by_their_text_not_by_their_length()
        {

            Assert.That(EVSEGroup_Id.    Parse("DE*GEF*EGAA").CompareTo(EVSEGroup_Id.    Parse("DE*GEF*EGB")), Is.Negative);
            Assert.That(GridOperator_Id. Parse("DE*ZZZ").     CompareTo(GridOperator_Id. Parse("FR*AAA")),     Is.Negative);
            Assert.That(ParkingSpace_Id. Parse("AA").         CompareTo(ParkingSpace_Id. Parse("B")),          Is.Negative);
            Assert.That(ParkingGarage_Id.Parse("AA").         CompareTo(ParkingGarage_Id.Parse("B")),          Is.Negative);

        }

        #endregion

        #region Software_and_certificate_identifications_compare_without_case()

        /// <summary>
        /// Transparency software and certificate identifications compare as brands do:
        /// without case, surrounding whitespace trimmed away; they keep the case they were written with.
        /// </summary>
        [Test]
        public void Software_and_certificate_identifications_compare_without_case()
        {

            AssertSame(TransparencySoftware_Id.           Parse("Release-1"),  TransparencySoftware_Id.           Parse("release-1"));
            AssertSame(ConformityCertificate_Id.Parse("PTB-1"),      ConformityCertificate_Id.Parse("ptb-1"));
            AssertSame(ConformityCertificate_Id.Parse("PTB-1"),      ConformityCertificate_Id.Parse(" PTB-1 "));

            Assert.That(ConformityCertificate_Id.Parse(" PTB-1 ").ToString(),  Is.EqualTo("PTB-1"));
            Assert.That(TransparencySoftware_Id.           Parse("Release-1").ToString(), Is.EqualTo("Release-1"));
            Assert.That(ConformityCertificate_Id.TryParse("   ",    out _),  Is.False);
            Assert.That(ConformityCertificate_Id.TryParse("",       out _),  Is.False);
            Assert.That(ConformityCertificate_Id.New,                         Is.Not.EqualTo(ConformityCertificate_Id.New));

        }

        #endregion

        #region Default_identifications_are_equal_and_empty()

        /// <summary>
        /// The default of every former class identification is an empty value, not null.
        /// </summary>
        [Test]
        public void Default_identifications_are_equal_and_empty()
        {

            Assert.That(default(ParkingGarage_Id)   == default(ParkingGarage_Id),   Is.True);
            Assert.That(default(ParkingSensor_Id).IsNullOrEmpty,                    Is.True);
            Assert.That(default(ConformityCertificate_Id).ToString(),     Is.Empty);
            Assert.That(ParkingGarage_Id.New,                                       Is.Not.EqualTo(ParkingGarage_Id.New));

        }

        #endregion

    }

}
