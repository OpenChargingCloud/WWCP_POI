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
    /// without templates giving all of them properties, parsed and changed by commits. Run on purpose only, in Release:
    /// dotnet test -c Release --filter "FullyQualifiedName~EffectiveMembersBenchmark" --logger "console;verbosity=detailed"
    /// </summary>
    [TestFixture, Explicit]
    public class EffectiveMembersBenchmark
    {

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");

        private static RoamingNetwork Network(Boolean WithTemplates, Int32 Pools = 500, Int32 Stations = 4, Int32 EVSEs = 5)
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
                                           MaxPower:     WithTemplates ? null : Watt.FromW(22000),
                                           TemplateIds:  WithTemplates ? [ EVSETemplate_Id.Parse("DE*GEF*ET1") ] : null));
                    stations.Add(new ChargingStation(ChargingStation_Id.Parse($"DE*GEF*S{stationNo}"), Created: Created, EVSEs: evses,
                                                     HotlinePhoneNumber:  WithTemplates ? null : PhoneNumber.Parse("+49 800 111111"),
                                                     TemplateIds:         WithTemplates ? [ ChargingStationTemplate_Id.Parse("DE*GEF*ST1") ] : null));
                }
                pools.Add(new ChargingPool(ChargingPool_Id.Parse($"DE*GEF*P{p}"), Created: Created, ChargingStations: stations,
                                           Facilities:   WithTemplates ? null : [ Facility.CAFE ],
                                           TemplateIds:  WithTemplates ? [ ChargingPoolTemplate_Id.Parse("DE*GEF*PT1") ] : null));
            }

            return new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                       Created:                   Created,
                       ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                        Created:                   Created,
                                                        ChargingPools:             pools,
                                                        EVSETemplates:             WithTemplates ? [ new EVSETemplate(EVSETemplate_Id.Parse("DE*GEF*ET1"), MaxPower: Watt.FromW(22000), Created: Created) ] : null,
                                                        ChargingStationTemplates:  WithTemplates ? [ new ChargingStationTemplate(ChargingStationTemplate_Id.Parse("DE*GEF*ST1"), HotlinePhoneNumber: PhoneNumber.Parse("+49 800 111111"), Created: Created) ] : null,
                                                        ChargingPoolTemplates:     WithTemplates ? [ new ChargingPoolTemplate(ChargingPoolTemplate_Id.Parse("DE*GEF*PT1"), Facilities: [ Facility.CAFE ], Created: Created) ] : null) ]).WithKeys();

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

            foreach (var withTemplates in new[] { false, true })
            {

                var network   = Network(withTemplates);
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

                TestContext.Out.WriteLine($"{(withTemplates ? "with templates   " : "without templates")}: parse operator {parse,8:F0} ms, commit one EVSE change {commitOne,8:F0} ms, effective pools {effective,6:F1} ms");

                if (withTemplates)
                {
                    // A change of the EVSE template, which every EVSE takes.
                    var commitTemplate = Time(() => {
                        var head  = history.Head.RoamingNetwork;
                        head.GetChargingStationOperatorById(GEF)!.TryGetEVSETemplate(EVSETemplate_Id.Parse("DE*GEF*ET1"), out var template);
                        var r = history.Commit(head.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(template!, "maxPower", null, 11000 + (++minutes)) ],
                                                                          CreatedAt: Created.AddMinutes(++minutes)));
                        Assert.That(r.IsCommitted, r.ErrorResponse);
                    }, 2);
                    TestContext.Out.WriteLine($"with templates   : commit a change of the EVSE template of all EVSEs {commitTemplate,8:F0} ms");
                }

            }

        }

    }

}
