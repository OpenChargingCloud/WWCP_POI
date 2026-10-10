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

using System.Text.Json;
using NUnit.Framework;
using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Tests;

/// <summary>
/// Runtime status changes reach every level above them within a network version:
/// EVSE, station, pool, operator and the network itself.
/// </summary>
[TestFixture]
public sealed class EventPropagationTests
{

    private static RoamingNetwork Network() => RoamingNetwork.Parse("""
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

    private static List<String> Listen(RoamingNetwork network)
    {
        var seen    = new List<String>();
        var op      = network.ChargingStationOperators.Single();
        var pool    = op.ChargingPools.Single();
        var station = pool.ChargingStations.Single();

        station.OnEVSEStatusChanged            += (_, _, evse, status, _, _) => { seen.Add($"station: {evse.Id} {status.Value}");  return Task.CompletedTask; };
        pool.   OnEVSEStatusChanged            += (_, _, evse, status, _, _) => { seen.Add($"pool: {evse.Id} {status.Value}");     return Task.CompletedTask; };
        op.     OnEVSEStatusChanged            += (_, _, evse, status, _, _) => { seen.Add($"operator: {evse.Id} {status.Value}"); return Task.CompletedTask; };
        op.     OnChargingStationStatusChanged += (_, _, s, status, _, _)    => { seen.Add($"operator: {s.Id} {status.Value}");    return Task.CompletedTask; };
        op.     OnChargingPoolStatusChanged    += (_, _, p, status, _, _)    => { seen.Add($"operator: {p.Id} {status.Value}");    return Task.CompletedTask; };
        op.     OnStatusChanged                += (_, _, o, _, status)       => { seen.Add($"operator: {o.Id} {status.Value}");    return Task.CompletedTask; };
        network.OnStatusChanged                += (_, _, n, status, _, _)    => { seen.Add($"network: {n.Id} {status.Value}");     return Task.CompletedTask; };

        return seen;
    }

    private static void Change(RoamingNetwork network)
    {
        var op      = network.ChargingStationOperators.Single();
        var pool    = op.ChargingPools.Single();
        var station = pool.ChargingStations.Single();

        station.EVSEs.Single().SetStatus(EVSEStatusType.Charging);
        station.SetStatus(ChargingStationStatusType.Charging);
        pool.   SetStatus(ChargingPoolStatusType.Charging);
        op.     SetStatus(ChargingStationOperatorStatusTypes.Offline);
        network.SetStatus(RoamingNetworkStatusType.Offline);
    }

    private static readonly String[] Expected = [
        "station: DE*ABC*E1 charging",
        "pool: DE*ABC*E1 charging",
        "operator: DE*ABC*E1 charging",
        "operator: DE*ABC*S1 charging",
        "operator: DE*ABC*P1 charging",
        "operator: DE*ABC offline",
        "network: network-a offline"
    ];

    [Test]
    public void Status_changes_reach_every_level_above_them()
    {
        var network = Network();
        var seen    = Listen(network);

        Change(network);

        Assert.That(seen, Is.EquivalentTo(Expected));
    }

    [Test]
    public void A_derived_network_version_propagates_as_well()
    {
        var network  = Network();
        var snapshot = network.DataSnapshot;
        var next     = network.ApplyChangeSet(snapshot.CreateChangeSet(
                           id:        "power",
                           createdAt: DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                           changes:   [ RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower",
                                                                            null, JsonSerializer.SerializeToElement("150 kW")) ]));
        var seen     = Listen(next);

        Change(next);

        Assert.That(seen, Is.EquivalentTo(Expected));
    }

}
