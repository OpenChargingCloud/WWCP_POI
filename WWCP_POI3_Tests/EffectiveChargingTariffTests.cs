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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The charging tariffs of an EVSE: its own, else the one of its most specific groups, of these the one with the
    /// highest priority.
    /// </summary>
    [TestFixture]
    public class EffectiveChargingTariffTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              Changed  = DateTimeOffset.Parse("2026-06-01T00:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly ChargingStation_Id          S2       = ChargingStation_Id.        Parse("DE*GEF*S2");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly EVSE_Id                     E3       = EVSE_Id.                   Parse("DE*GEF*E2*1");
        private static readonly EVSE_Id                     E4       = EVSE_Id.                   Parse("DE*GEF*E2*2");
        private static readonly ChargingTariff_Id           T1       = ChargingTariff_Id.         Parse("DE*GEF*T1");
        private static readonly ChargingTariff_Id           T2       = ChargingTariff_Id.         Parse("DE*GEF*T2");
        private static readonly ChargingTariff_Id           T3       = ChargingTariff_Id.         Parse("DE*GEF*T3A");
        private static readonly ChargingTariff_Id           T4       = ChargingTariff_Id.         Parse("DE*GEF*T4");

        #endregion

        #region NewOperator(SecondEVSEGroupPriority = 1)

        /// <summary>
        /// A pool group with T1 for the pool, a station group with T2 for the first station, an EVSE group with T3 for the
        /// second EVSE and another one with T1 for the fourth EVSE, of the given priority; the first EVSE has T4 of its own.
        /// </summary>
        private static ChargingStationOperator NewOperator(Int32?              SecondEVSEGroupPriority  = 1,
                                                           ChargingTariff_Id?  SecondEVSEGroupTariff    = null)

            => new (GEF,
                    Created:                Created,
                    ChargingTariffs:        [ .. new[] { T1, T2, T3, T4 }.Select(tariffId => new ChargingTariff(tariffId,
                                                                                                                Currency.EUR,
                                                                                                                [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                Created: Created)) ],
                    ChargingPools:          [ new ChargingPool(P1,
                                                               Created:           Created,
                                                               ChargingStations:  [ new ChargingStation(S1,
                                                                                                        Created:  Created,
                                                                                                        EVSEs:    [ new EVSE(E1, ChargingTariffIds: [ T4 ], Created: Created),
                                                                                                                    new EVSE(E2, Created: Created) ]),
                                                                                    new ChargingStation(S2,
                                                                                                        Created:  Created,
                                                                                                        EVSEs:    [ new EVSE(E3, Created: Created),
                                                                                                                    new EVSE(E4, Created: Created) ]) ]) ],
                    ChargingPoolGroups:     [ new ChargingPoolGroup   (ChargingPoolGroup_Id.   Parse("DE*GEF*PG1"), ChargingPoolIds:    [ P1 ], ChargingTariffId: T1, Created: Created, LastChange: Changed) ],
                    ChargingStationGroups:  [ new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG1"), ChargingStationIds: [ S1 ], ChargingTariffId: T2, Created: Created) ],
                    EVSEGroups:             [ new EVSEGroup           (EVSEGroup_Id.           Parse("DE*GEF*EG1"), EVSEIds:            [ E2, E4 ], ChargingTariffId: T3, Created: Created),
                                              new EVSEGroup           (EVSEGroup_Id.           Parse("DE*GEF*EG2"), EVSEIds:            [ E4 ],     ChargingTariffId: SecondEVSEGroupTariff ?? T1, Priority: SecondEVSEGroupPriority, Created: Created) ]);

        #endregion


        #region The_most_specific_groups_decide()

        [Test]
        public void The_most_specific_groups_decide()
        {

            var gef = NewOperator();

            gef.TryGetEffectiveEVSE(E1, out var e1);
            gef.TryGetEffectiveEVSE(E2, out var e2);
            gef.TryGetEffectiveEVSE(E3, out var e3);
            gef.TryGetEffectiveEVSE(E4, out var e4);

            using (Assert.EnterMultipleScope())
            {

                // Its own; its EVSE group's before its station group's; its pool group's.
                Assert.That(e1!.ChargingTariffIds,                                Is.EqualTo(new[] { T4 }));
                Assert.That(e2!.ChargingTariffIds,                                Is.EqualTo(new[] { T3 }));
                Assert.That(e3!.ChargingTariffIds,                                Is.EqualTo(new[] { T1 }));

                // Of two EVSE groups the one with the higher priority.
                Assert.That(e4!.ChargingTariffIds,                                Is.EqualTo(new[] { T1 }));
                Assert.That(NewOperator(-1).EffectiveEVSE(e4).ChargingTariffIds,  Is.EqualTo(new[] { T3 }));

                // With the last change of the group whose tariff it takes.
                Assert.That(e3.LastChangeDate,                                    Is.EqualTo(Changed));
                Assert.That(gef.EffectiveChargingPool(gef.ChargingPools.Single()).ChargingStations.Last().EVSEs.First().ChargingTariffIds,  Is.EqualTo(new[] { T1 }));

                // As given: without tariffs of their own.
                Assert.That(gef.EVSEs.Select(evse => evse.ChargingTariffIds.Length),  Is.EqualTo(new[] { 1, 0, 0, 0 }));

            }

        }

        #endregion

        #region Groups_of_the_same_priority_may_name_a_tariff_in_another_spelling()

        [Test]
        public void Groups_of_the_same_priority_may_name_a_tariff_in_another_spelling()

            => Assert.That(NewOperator(SecondEVSEGroupPriority: null, SecondEVSEGroupTariff: ChargingTariff_Id.Parse("DE*GEF*T3*A")).
                               EffectiveEVSEs.Last().ChargingTariffIds,
                           Is.EqualTo(new[] { T3 }));

        #endregion

        #region Groups_of_the_same_priority_must_not_give_different_tariffs()

        [Test]
        public void Groups_of_the_same_priority_must_not_give_different_tariffs()

            => Assert.That(() => NewOperator(SecondEVSEGroupPriority: null),
                           Throws.ArgumentException.With.Message.Contains("The groups 'DE*GEF*EG1' and 'DE*GEF*EG2' give the EVSE 'DE*GEF*E2*2' different values of 'chargingTariffId' with the same priority 0"));

        #endregion

    }

}
