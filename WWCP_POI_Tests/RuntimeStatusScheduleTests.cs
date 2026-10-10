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
public sealed class RuntimeStatusScheduleTests
{

    /// <summary>
    /// A scheduled return to the current value must survive: out of service
    /// in one hour and available again in two is three entries, not two.
    /// </summary>
    [Test]
    public void A_future_value_equal_to_the_current_one_is_scheduled()
    {
        var now = Timestamp.Now;
        var schedule = new RuntimeStatusSchedule<String>(new Timestamped<String>(now, "available"));

        schedule.Insert("outOfService", now + TimeSpan.FromHours(1));
        schedule.Insert("available",    now + TimeSpan.FromHours(2));

        Assert.That(schedule.Select(status => status.Value), Is.EqualTo(new[] { "available", "outOfService", "available" }));
        Assert.That(schedule.CurrentValue, Is.EqualTo("available"));
        Assert.That(schedule.NextStatus?.Value, Is.EqualTo("outOfService"));
    }

    /// <summary>
    /// A value repeating the one in effect right before it changes nothing.
    /// </summary>
    [Test]
    public void A_value_repeating_the_previous_one_is_ignored()
    {
        var now = Timestamp.Now;
        var schedule = new RuntimeStatusSchedule<String>(new Timestamped<String>(now - TimeSpan.FromHours(1), "available"));

        schedule.Insert("available", now);
        schedule.Insert("available", now + TimeSpan.FromHours(1));

        Assert.That(schedule.Select(status => status.Value), Is.EqualTo(new[] { "available" }));
    }

    /// <summary>
    /// The status setter must not drop a scheduled return to the current status either.
    /// </summary>
    [Test]
    public void The_status_setter_schedules_a_return_to_the_current_status()
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
                }]
              }]
            }
            """);

        var evse    = network.GetEVSEById(EVSE_Id.Parse("DE*ABC*E1"))!;
        var current = evse.Status.Value;
        var other   = current == EVSEStatusType.OutOfService ? EVSEStatusType.Available : EVSEStatusType.OutOfService;
        var now     = Timestamp.Now;

        evse.Status = new Timestamped<EVSEStatusType>(now + TimeSpan.FromHours(1), other);
        evse.Status = new Timestamped<EVSEStatusType>(now + TimeSpan.FromHours(2), current);

        Assert.That(evse.StatusSchedule().Take(2).Select(status => status.Value), Is.EqualTo(new[] { current, other }));
    }

}
