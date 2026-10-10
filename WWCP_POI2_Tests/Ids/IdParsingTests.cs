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
    /// Identifications parsed from text and compared as default values.
    /// </summary>
    [TestFixture]
    public class IdParsingTests
    {

        #region Unknown_country_codes_are_no_operators()

        /// <summary>
        /// An unknown country code is no operator at all, and neither is the
        /// obsolete DIN SPEC 91286 form ("822", "+49*822").
        /// </summary>
        [Test]
        public void Unknown_country_codes_are_no_operators()
        {

            Assert.That(ChargingStationOperator_Id.TryParse("QQ*ABC"),          Is.Null);
            Assert.That(ChargingStationOperator_Id.TryParse("+12345*123"),      Is.Null);
            Assert.That(EVSE_Id.                   TryParse("QQ*ABC*E1"),       Is.Null);
            Assert.That(GridOperator_Id.           TryParse("QQ*ABC", out _),   Is.False);
            Assert.That(() => GridOperator_Id.Parse("QQ*ABC"),                  Throws.ArgumentException.With.Message.Contains("'QQ*ABC'"));

            Assert.That(ChargingStationOperator_Id.TryParse("822"),             Is.Null);
            Assert.That(ChargingStationOperator_Id.TryParse("+49*822"),         Is.Null);
            Assert.That(GridOperator_Id.           TryParse("822", out _),      Is.False);

        }

        #endregion

        #region Identifications_are_ISO_with_optional_separators_and_written_with_them()

        /// <summary>
        /// Identifications follow ISO 15118 / IDACS: the '*' separators are optional on input
        /// and always written; the obsolete DIN SPEC 91286 forms are no identifications.
        /// </summary>
        [Test]
        public void Identifications_are_ISO_with_optional_separators_and_written_with_them()
        {

            Assert.That(ChargingStationOperator_Id.Parse("DEABC").     ToString(), Is.EqualTo("DE*ABC"));
            Assert.That(EVSE_Id.                   Parse("DEABCE1").   ToString(), Is.EqualTo("DE*ABC*E1"));
            Assert.That(ChargingStation_Id.        Parse("DEABCS1").   ToString(), Is.EqualTo("DE*ABC*S1"));
            Assert.That(ChargingPool_Id.           Parse("DEABCP1").   ToString(), Is.EqualTo("DE*ABC*P1"));
            Assert.That(GridOperator_Id.           Parse("DEGRD").     ToString(), Is.EqualTo("DE*GRD"));
            Assert.That(EVSE_Id.                   Parse("DEABCE1"),               Is.EqualTo(EVSE_Id.Parse("DE*ABC*E1")));

            Assert.That(ChargingStationOperator_Id.TryParse("822"),            Is.Null);
            Assert.That(ChargingStationOperator_Id.TryParse("+49*822"),        Is.Null);
            Assert.That(EVSE_Id.                   TryParse("+49*822*12345"),  Is.Null);
            Assert.That(GridOperator_Id.           TryParse("+49*822", out _), Is.False);

        }

        #endregion

        #region Suffixes_have_their_specified_lengths()

        /// <summary>
        /// IDACS: after "E", "P" or "S" come 1 to 30 characters; WWCP's own suffixes take 1 to 50.
        /// </summary>
        [Test]
        public void Suffixes_have_their_specified_lengths()
        {

            var max30 = new String('A', 30);
            var max50 = new String('A', 50);

            Assert.That(EVSE_Id.           TryParse("DE*ABC*E"  + max30),              Is.Not.Null);
            Assert.That(EVSE_Id.           TryParse("DE*ABC*E"  + max30 + "A"),        Is.Null);
            Assert.That(EVSE_Id.           TryParse("DE*ABC*E"),                       Is.Null);
            Assert.That(ChargingStation_Id.TryParse("DE*ABC*S"  + max30),              Is.Not.Null);
            Assert.That(ChargingStation_Id.TryParse("DE*ABC*S"  + max30 + "A"),        Is.Null);
            Assert.That(ChargingPool_Id.   TryParse("DE*ABC*P"  + max30),              Is.Not.Null);
            Assert.That(ChargingPool_Id.   TryParse("DE*ABC*P"  + max30 + "A"),        Is.Null);
            Assert.That(ChargingTariff_Id. TryParse("DE*ABC*T"  + max50),              Is.Not.Null);
            Assert.That(ChargingTariff_Id. TryParse("DE*ABC*T"  + max50 + "A"),        Is.Null);
            Assert.That(EVSEGroup_Id.      TryParse("DE*ABC*EG" + max50,       out _), Is.True);
            Assert.That(EVSEGroup_Id.      TryParse("DE*ABC*EG" + max50 + "A", out _), Is.False);

        }

        #endregion

        #region Group_IDs_put_the_entity_letter_first()

        /// <summary>
        /// Groups write the entity's letter first: EG, SG, PG, TG; the operator part ignores case.
        /// </summary>
        [Test]
        public void Group_IDs_put_the_entity_letter_first()
        {

            Assert.That(EVSEGroup_Id.           Parse("de*abc*EG1").ToString(), Is.EqualTo("DE*ABC*EG1"));
            Assert.That(ChargingStationGroup_Id.Parse("de*abc*SG1").ToString(), Is.EqualTo("DE*ABC*SG1"));
            Assert.That(ChargingPoolGroup_Id.   Parse("de*abc*PG1").ToString(), Is.EqualTo("DE*ABC*PG1"));
            Assert.That(ChargingTariffGroup_Id. Parse("de*abc*TG1").ToString(), Is.EqualTo("DE*ABC*TG1"));

            Assert.That(EVSEGroup_Id.           TryParse("DE*ABC*GE1", out _), Is.False);
            Assert.That(ChargingStationGroup_Id.TryParse("DE*ABC*GS1", out _), Is.False);
            Assert.That(ChargingPoolGroup_Id.   TryParse("DE*ABC*GP1", out _), Is.False);

        }

        #endregion

        #region Charging_pool_group_IDs_use_their_own_prefix()

        /// <summary>
        /// Charging pool groups are *PG; *SG belongs to charging station groups.
        /// </summary>
        [Test]
        public void Charging_pool_group_IDs_use_their_own_prefix()
        {

            var poolGroupId = ChargingPoolGroup_Id.Parse("DE*ABC*PG1");

            Assert.That(poolGroupId.ToString(),                                                    Is.EqualTo("DE*ABC*PG1"));
            Assert.That(ChargingPoolGroup_Id.Parse(ChargingStationOperator_Id.Parse("DE*ABC"), "1"), Is.EqualTo(poolGroupId));

            Assert.That(ChargingPoolGroup_Id.   TryParse("DE*ABC*SG1", out _), Is.False);
            Assert.That(ChargingStationGroup_Id.TryParse("DE*ABC*PG1", out _), Is.False);
            Assert.That(ChargingStationGroup_Id.TryParse("DE*ABC*SG1", out _), Is.True);

        }

        #endregion

        #region EMAIDs_take_an_optional_check_digit_and_no_pipes()

        /// <summary>
        /// The EMAID check digit is optional, and '|' is no character of any e-mobility ID.
        /// </summary>
        [Test]
        public void EMAIDs_take_an_optional_check_digit_and_no_pipes()
        {

            Assert.That(EMobilityAccount_Id.TryParse("DE-GDF-C12022187-X")?.CheckDigit, Is.EqualTo('X'));
            Assert.That(EMobilityAccount_Id.TryParse("DEGDFC12022187"),                 Is.Not.Null);
            Assert.That(EMobilityAccount_Id.TryParse("DEGDFC12022187")?.CheckDigit,     Is.Null);

            Assert.That(EMobilityAccount_Id.TryParse("DE-GDF-C12022187-|"),             Is.Null);
            Assert.That(EMobilityAccount_Id.TryParse("DE|GDF|0010LY|3"),                Is.Null);
            Assert.That(EMobilityAccount_Id.TryParse("DE*GDF*0010LY*|"),                Is.Null);

        }

        #endregion

        #region Operator_IDs_parse_in_every_culture()

        /// <summary>
        /// In Turkish, "i" uppers to a dotted "İ", which no operator ID pattern accepts.
        /// </summary>
        [Test]
        [SetCulture("tr-TR")]
        public void Operator_IDs_parse_in_every_culture()
        {

            Assert.That(ChargingStationOperator_Id.TryParse("it*abc")?.ToString(), Is.EqualTo("IT*ABC"));

        }

        #endregion

        #region Default_IDs_compare_without_exceptions()

        /// <summary>
        /// The == operators meant "both null" to be equal; for these structs that is default.
        /// </summary>
        [Test]
        public void Default_IDs_compare_without_exceptions()
        {

            Assert.That(default(EVSEGroup_Id)            == default(EVSEGroup_Id),            Is.True);
            Assert.That(default(ChargingStationGroup_Id) == default(ChargingStationGroup_Id), Is.True);
            Assert.That(default(ChargingPoolGroup_Id)    == default(ChargingPoolGroup_Id),    Is.True);
            Assert.That(default(ChargingTariffGroup_Id)  == default(ChargingTariffGroup_Id),  Is.True);
            Assert.That(default(ParkingSpace_Id)         == default(ParkingSpace_Id),         Is.True);

            Assert.That(default(EVSEGroup_Id) == EVSEGroup_Id.Parse("DE*ABC*EG1"),  Is.False);
            Assert.That(() => default(EVSEGroup_Id).GetHashCode(),                  Throws.Nothing);

        }

        #endregion

    }

}
