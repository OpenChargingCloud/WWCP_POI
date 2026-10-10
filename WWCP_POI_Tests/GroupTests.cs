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
public sealed class GroupTests
{

    /// <summary>
    /// An admin status change of an EVSE group without subscribers must not fail.
    /// </summary>
    [Test]
    public void An_EVSE_group_admin_status_update_without_subscribers_completes()
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
                    "EVSEs": [{ "@id": "DE*ABC*E1", "currentType": [ "DC" ] }]
                  }]
                }],
                "EVSEGroups": [{ "@id": "DE*ABC*EG1", "name": { "en": "Group" }, "EVSEIds": [ "DE*ABC*E1" ] }]
              }]
            }
            """);

        var group = network.ChargingStationOperators.Single().EVSEGroups.Single();

        Assert.That(async () => await group.UpdateAdminStatus(Timestamp.Now,
                                                              EventTracking_Id.New,
                                                              new Timestamped<EVSEGroupAdminStatusTypes>(EVSEGroupAdminStatusTypes.OutOfService)),
                    Throws.Nothing);
    }

}
