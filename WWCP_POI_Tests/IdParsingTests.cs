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
using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Tests;

/// <summary>
/// The identifications of WWCP_POI2 are tested in WWCP_POI2_Tests.
/// </summary>
[TestFixture]
public sealed class IdParsingTests
{

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
        Assert.That(default(Brand_Id)          == default(Brand_Id),          Is.True);
        Assert.That(default(ParkingProduct_Id) == default(ParkingProduct_Id), Is.True);

        Assert.That(default(Brand_Id) == Brand_Id.Parse("brand"),  Is.False);
        Assert.That(() => default(Brand_Id).GetHashCode(),        Throws.Nothing);
    }

}
