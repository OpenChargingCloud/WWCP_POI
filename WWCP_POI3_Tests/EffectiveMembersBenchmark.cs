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

using System.Diagnostics;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// What effective members cost: an operator of 500 charging pools, 2,000 charging stations and 10,000 EVSEs, with and
    /// without groups giving all of them properties, parsed and changed by commits. Run on purpose only, in Release:
    /// dotnet test -c Release --filter "FullyQualifiedName~EffectiveMembersBenchmark" --logger "console;verbosity=detailed"
    /// </summary>
    [TestFixture, Explicit]
    public class EffectiveMembersBenchmark
    {

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");

        private static RoamingNetwork Network(Boolean WithGroups, Int32 Pools = 500, Int32 Stations = 4, Int32 EVSEs = 5)
        {

            var pools = new List<ChargingPool>();

            for (var p = 1; p <= Pools; p++)
            {
                var stations = new List<ChargingStation>();
                for (var s = 1; s <= Stations; s++)
                {
                    var stationNo = (p - 1) * Stations + s;
                    var evses = new List<EVSE>();
                    for (var e = 1; e <= EVSEs; e++)
                        evses.Add(new EVSE(EVSE_Id.Parse($"DE*GEF*E{stationNo}*{e}"), Created: Created,
                                           MaxPower: WithGroups ? null : Watt.FromW(22000)));
                    stations.Add(new ChargingStation(ChargingStation_Id.Parse($"DE*GEF*S{stationNo}"), Created: Created, EVSEs: evses,
                                                     HotlinePhoneNumber: WithGroups ? null : PhoneNumber.Parse("+49 800 111111")));
                }
                pools.Add(new ChargingPool(ChargingPool_Id.Parse($"DE*GEF*P{p}"), Created: Created, ChargingStations: stations,
                                           Facilities: WithGroups ? null : [ Facility.CAFE ]));
            }

            var allEVSEs    = pools.SelectMany(pool => pool.ChargingStations).SelectMany(station => station.EVSEs).Select(evse => evse.Id).ToArray();
            var allStations = pools.SelectMany(pool => pool.ChargingStations).Select(station => station.Id).ToArray();
            var allPools    = pools.Select(pool => pool.Id).ToArray();

            return new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                       Created:                   Created,
                       ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                        Created:                Created,
                                                        ChargingPools:          pools,
                                                        EVSEGroups:             WithGroups ? [ new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EG1"), EVSEIds: allEVSEs, MaxPower: Watt.FromW(22000), Created: Created) ] : null,
                                                        ChargingStationGroups:  WithGroups ? [ new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG1"), ChargingStationIds: allStations, HotlinePhoneNumber: PhoneNumber.Parse("+49 800 111111"), Created: Created) ] : null,
                                                        ChargingPoolGroups:     WithGroups ? [ new ChargingPoolGroup(ChargingPoolGroup_Id.Parse("DE*GEF*PG1"), ChargingPoolIds: allPools, Facilities: [ Facility.CAFE ], Created: Created) ] : null) ]).WithKeys();

        }

        private static Double Time(Action Action, Int32 Runs = 3)
        {
            Action();
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < Runs; i++)
                Action();
            return stopwatch.Elapsed.TotalMilliseconds / Runs;
        }

        [Test]
        public void Effective_members_of_a_large_operator()
        {

            foreach (var withGroups in new[] { false, true })
            {

                var network   = Network(withGroups);
                var gef       = network.GetChargingStationOperatorById(GEF)!;
                var json      = gef.ToEmbeddedJSON();

                var parse     = Time(() => ChargingStationOperator.TryParse(json, out _, out _));

                var history   = RoamingNetworkHistory.Start(network);
                var minutes   = 0;

                // A small change of one EVSE.
                var commitOne = Time(() => {
                    var head = history.Head.RoamingNetwork;
                    var evse = head.GetChargingStationOperatorById(GEF)!.ChargingPools.First().EVSEs.First();
                    var r = history.Commit(head.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(evse, "maxVoltage", null, 400 + (++minutes)) ],
                                                                      CreatedAt: Created.AddMinutes(++minutes)));
                    Assert.That(r.IsCommitted, r.ErrorResponse);
                });

                var effective = Time(() => _ = history.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.EffectiveChargingPools.Count);

                TestContext.Out.WriteLine($"{(withGroups ? "with groups   " : "without groups")}: parse operator {parse,8:F0} ms, commit one EVSE change {commitOne,8:F0} ms, effective pools {effective,6:F1} ms");

                if (withGroups)
                {
                    // A change of the EVSE group, which every EVSE takes.
                    var commitGroup = Time(() => {
                        var head  = history.Head.RoamingNetwork;
                        head.TryGetEVSEGroupById(EVSEGroup_Id.Parse("DE*GEF*EG1"), out var group);
                        var r = history.Commit(head.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(group!, "maxPower", null, 11000 + (++minutes)) ],
                                                                          CreatedAt: Created.AddMinutes(++minutes)));
                        Assert.That(r.IsCommitted, r.ErrorResponse);
                    }, 2);
                    TestContext.Out.WriteLine($"with groups   : commit a change of the EVSE group of all EVSEs {commitGroup,8:F0} ms");
                }

            }

        }

    }

}
