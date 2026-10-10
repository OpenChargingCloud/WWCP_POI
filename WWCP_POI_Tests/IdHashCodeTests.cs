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
using cloud.charging.open.protocols.WWCP.POI.CSM;

namespace WWCP_POI_Tests;

/// <summary>
/// Identifications that are equal must have equal hash codes,
/// or hash sets and dictionaries will not find them.
/// </summary>
[TestFixture]
public sealed class IdHashCodeTests
{

    private static void AssertOneKey<T>(T first, T second)
        where T : notnull
    {
        Assert.That(first, Is.EqualTo(second));
        Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        Assert.That(new HashSet<T> { first, second }, Has.Count.EqualTo(1));
    }

    [Test]
    public void Suffixes_ignore_case_and_stars_in_their_hash_codes()
    {
        AssertOneKey(EVSE_Id.           Parse("DE*ABC*Eab*c1"), EVSE_Id.           Parse("DE*ABC*EABC1"));
        AssertOneKey(ChargingPool_Id.   Parse("DE*ABC*Pab*c1"), ChargingPool_Id.   Parse("DE*ABC*PABC1"));
        AssertOneKey(ChargingStation_Id.Parse("DE*ABC*Sab*c1"), ChargingStation_Id.Parse("DE*ABC*SABC1"));
        AssertOneKey(ChargingProduct_Id.Parse("product"),       ChargingProduct_Id.Parse("PRODUCT"));
    }

    /// <summary>
    /// In Turkish, "I" lowers to a dotless "ı", which once split
    /// "I" and "i" into different hash codes although they are equal.
    /// </summary>
    [Test]
    [SetCulture("tr-TR")]
    public void Hash_codes_do_not_depend_on_the_current_culture()
    {
        AssertOneKey(RoamingNetwork_Id.             Parse("INDEX"),      RoamingNetwork_Id.             Parse("index"));
        AssertOneKey(ChargingConnector_Id.          Parse("I1"),         ChargingConnector_Id.          Parse("i1"));
        AssertOneKey(EnergyMeter_Id.                Parse("METER-I"),    EnergyMeter_Id.                Parse("meter-i"));
        AssertOneKey(ParkingOperator_Id.            Parse("PARKING-I"),  ParkingOperator_Id.            Parse("parking-i"));
        AssertOneKey(ChargingStationManufacturer_Id.Parse("MAKER-I"),    ChargingStationManufacturer_Id.Parse("maker-i"));
        AssertOneKey(EVSE_Id.                       Parse("DE*ABC*EI1"), EVSE_Id.                       Parse("DE*ABC*Ei1"));
    }

    [Test]
    public void An_EVSE_is_found_by_any_spelling_its_ID_equals()
    {
        var network = RoamingNetwork.Parse("""
            {
              "@id": "network-a", "name": { "en": "Network" },
              "chargingStationOperators": [{
                "@id": "DE*ABC", "name": { "en": "Operator" },
                "chargingPools": [{
                  "@id": "DE*ABC*P1",
                  "chargingStations": [{
                    "@id": "DE*ABC*S1",
                    "EVSEs": [{ "@id": "DE*ABC*EX1", "currentType": [ "DC" ] }]
                  }]
                }]
              }]
            }
            """);

        var evse = network.GetEVSEById(EVSE_Id.Parse("DE*ABC*EX1"));

        Assert.That(evse, Is.Not.Null);
        Assert.That(network.GetEVSEById(EVSE_Id.Parse("DE*ABC*Ex1")), Is.SameAs(evse));
    }

}
