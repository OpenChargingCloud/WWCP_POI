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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// What a commit of one changed EVSE costs in a network of 50,000 EVSEs: of one charging station operator, with and
    /// without templates, and of 50 operators. The change set is signed by the operator only. Run on purpose only, in
    /// Release:
    /// dotnet test -c Release --filter "FullyQualifiedName~CommitCostBenchmark" --logger "console;verbosity=detailed"
    /// </summary>
    /// <remarks>
    /// The tree of a version after a change is calculated from the digests the entities keep (POIEffectiveDigests); only
    /// a changed operator is hashed again, as a change reads it again completely. The JSON and CBOR ETags hash the whole
    /// version, once per version.
    /// </remarks>
    [TestFixture, Explicit]
    public class CommitCostBenchmark
    {

        #region Data

        private static readonly DateTimeOffset  Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        #endregion


        #region (static) Network(Operators, Pools, WithTemplates)

        /// <summary>
        /// A network of the given number of operators of the given number of charging pools, each with 4 charging stations
        /// of 5 EVSEs: with templates giving them their properties, or with their own.
        /// </summary>
        private static RoamingNetwork Network(Int32    Operators,
                                              Int32    Pools,
                                              Boolean  WithTemplates)
        {

            var operators = new List<ChargingStationOperator>();

            for (var o = 1; o <= Operators; o++)
            {

                var operatorId  = ChargingStationOperator_Id.Parse($"DE*O{o:D2}");
                var pools       = new List<ChargingPool>();

                for (var p = 1; p <= Pools; p++)
                {

                    var stations = new List<ChargingStation>();

                    for (var s = 1; s <= 4; s++)
                    {

                        var stationNo  = (p - 1) * 4 + s;
                        var evses      = new List<EVSE>();

                        for (var e = 1; e <= 5; e++)
                            evses.Add(new EVSE(EVSE_Id.Parse($"{operatorId}*E{stationNo}*{e}"),
                                               Created:      Created,
                                               MaxPower:     WithTemplates ? null : Watt.FromW(22000),
                                               TemplateIds:  WithTemplates ? [ EVSETemplate_Id.Parse($"{operatorId}*ET1") ] : null));

                        stations.Add(new ChargingStation(ChargingStation_Id.Parse($"{operatorId}*S{stationNo}"),
                                                         Created:             Created,
                                                         EVSEs:               evses,
                                                         HotlinePhoneNumber:  WithTemplates ? null : PhoneNumber.Parse("+49 800 111111"),
                                                         TemplateIds:         WithTemplates ? [ ChargingStationTemplate_Id.Parse($"{operatorId}*ST1") ] : null));

                    }

                    pools.Add(new ChargingPool(ChargingPool_Id.Parse($"{operatorId}*P{p}"),
                                               Created:           Created,
                                               ChargingStations:  stations,
                                               Facilities:        WithTemplates ? null : [ Facility.CAFE ],
                                               TemplateIds:       WithTemplates ? [ ChargingPoolTemplate_Id.Parse($"{operatorId}*PT1") ] : null));

                }

                operators.Add(new ChargingStationOperator(operatorId,
                                                          Created:                   Created,
                                                          ChargingPools:             pools,
                                                          EVSETemplates:             WithTemplates ? [ new EVSETemplate           (EVSETemplate_Id.           Parse($"{operatorId}*ET1"), MaxPower:           Watt.FromW(22000),                Created: Created) ] : null,
                                                          ChargingStationTemplates:  WithTemplates ? [ new ChargingStationTemplate(ChargingStationTemplate_Id.Parse($"{operatorId}*ST1"), HotlinePhoneNumber: PhoneNumber.Parse("+49 800 111111"), Created: Created) ] : null,
                                                          ChargingPoolTemplates:     WithTemplates ? [ new ChargingPoolTemplate   (ChargingPoolTemplate_Id.   Parse($"{operatorId}*PT1"), Facilities:         [ Facility.CAFE ],                Created: Created) ] : null));

            }

            return new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                      Created:                   Created,
                                      ChargingStationOperators:  operators).WithKeys();

        }

        #endregion

        #region (static) Ms(Action)

        private static Double Ms(Action Action)
        {
            var stopwatch = Stopwatch.StartNew();
            Action();
            return stopwatch.Elapsed.TotalMilliseconds;
        }

        #endregion


        #region A_commit_of_one_changed_EVSE(Operators, Pools, WithTemplates)

        [TestCase( 1, 2500, false)]
        [TestCase( 1, 2500, true)]
        [TestCase(50,   50, false)]
        public void A_commit_of_one_changed_EVSE(Int32 Operators, Int32 Pools, Boolean WithTemplates)
        {

            var history  = RoamingNetworkHistory.Start(Network(Operators, Pools, WithTemplates));
            var minutes  = 0;

            for (var run = 0; run < 4; run++)
            {

                var head             = history.Head.RoamingNetwork;
                var chargingStation  = head.ChargingStationOperators[run % Operators];
                var evse             = chargingStation.ChargingPools.First().EVSEs.First();

                RoamingNetworkChangeSet? changeSet = null;
                RoamingNetwork?          version   = null;

                var create    = Ms(() => changeSet = head.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(evse, "maxVoltage", null, 400 + (++minutes)) ],
                                                                          CreatedAt: Created.AddMinutes(++minutes)).
                                                          SignedBy(TestParties.Party(chargingStation.Id)));

                var apply     = Ms(() => Assert.That(head.TryApplyChangeSet(changeSet!, out version, out var error), Is.True, error));

                // The tree of a version read anew, whose entities keep no digests yet.
                var cold      = RoamingNetwork.Parse(version!.ToJSON(IncludeETags: false));
                var coldTree  = Ms(() => _ = cold.EffectiveTreeETag(ETagHashAlgorithm.SHA256));

                var commit    = Ms(() => Assert.That(history.Commit(changeSet!).IsCommitted));

                TestContext.Out.WriteLine($"{Operators,2} operator(s) of {Operators * Pools * 20:N0} EVSEs{(WithTemplates ? " with templates   " : "")}, run {run}: create the change set {create,6:F0} ms, apply it {apply,6:F0} ms, tree of a version read anew {coldTree,6:F0} ms, commit {commit,6:F0} ms");

            }

        }

        #endregion

    }

}
