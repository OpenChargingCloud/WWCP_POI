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

using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Tests;

[TestFixture]
public sealed class IdParsingTests
{

    /// <summary>
    /// An unknown country code is no operator at all.
    /// </summary>
    [Test]
    public void Unknown_country_codes_are_no_operators()
    {
        Assert.That(ChargingStationOperator_Id.TryParse("QQ*ABC"),   Is.Null);
        Assert.That(EVSE_Id.                   TryParse("QQ*ABC*E1"), Is.Null);
        Assert.That(GridOperator_Id.           TryParse("QQ*ABC", out _), Is.False);
        Assert.That(() => GridOperator_Id.Parse("QQ*ABC"), Throws.ArgumentException.With.Message.Contains("'QQ*ABC'"));
    }

    /// <summary>
    /// POI identifications follow ISO 15118 / IDACS: the '*' separators are optional on input
    /// and always written; the obsolete DIN SPEC 91286 forms are no POI identifications.
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

        Assert.That(ChargingStationOperator_Id.TryParse("822"),           Is.Null);
        Assert.That(ChargingStationOperator_Id.TryParse("+49*822"),       Is.Null);
        Assert.That(EVSE_Id.                   TryParse("+49*822*12345"), Is.Null);
        Assert.That(GridOperator_Id.           TryParse("+49*822", out _), Is.False);
    }

    /// <summary>
    /// In Turkish, "i" uppers to a dotted "İ", which no operator ID pattern accepts.
    /// </summary>
    [Test]
    [SetCulture("tr-TR")]
    public void Operator_IDs_parse_in_every_culture()
    {
        Assert.That(ChargingStationOperator_Id.TryParse("it*abc")?.ToString(), Is.EqualTo("IT*ABC"));
    }

    [Test]
    public void A_brand_ID_that_cannot_be_parsed_is_null()
    {
        Assert.That(Brand_Id.TryParse(""),    Is.Null);
        Assert.That(Brand_Id.TryParse("   "), Is.Null);
    }

    /// <summary>
    /// The == operators meant "both null" to be equal; for these structs that is default.
    /// </summary>
    [Test]
    public void Default_IDs_compare_without_exceptions()
    {
        Assert.That(default(Brand_Id)                == default(Brand_Id),                Is.True);
        Assert.That(default(EVSEGroup_Id)            == default(EVSEGroup_Id),            Is.True);
        Assert.That(default(ChargingStationGroup_Id) == default(ChargingStationGroup_Id), Is.True);
        Assert.That(default(ChargingPoolGroup_Id)    == default(ChargingPoolGroup_Id),    Is.True);
        Assert.That(default(ChargingTariffGroup_Id)  == default(ChargingTariffGroup_Id),  Is.True);
        Assert.That(default(ParkingSpace_Id)         == default(ParkingSpace_Id),         Is.True);
        Assert.That(default(ParkingProduct_Id)       == default(ParkingProduct_Id),       Is.True);

        Assert.That(default(Brand_Id)     == Brand_Id.Parse("brand"),            Is.False);
        Assert.That(default(EVSEGroup_Id) == EVSEGroup_Id.Parse("DE*ABC*GE1"),   Is.False);
        Assert.That(() => default(Brand_Id).GetHashCode(),     Throws.Nothing);
        Assert.That(() => default(EVSEGroup_Id).GetHashCode(), Throws.Nothing);
    }

}
