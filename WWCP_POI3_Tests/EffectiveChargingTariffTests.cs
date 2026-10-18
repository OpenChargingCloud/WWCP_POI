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
    /// The charging tariffs of an EVSE: its own; without any, the one of the first of its EVSE templates naming one, else
    /// of the first template of its charging station naming one, else of the first template of its charging pool naming one.
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
        private static readonly EVSETemplate_Id             ET1      = EVSETemplate_Id.           Parse("DE*GEF*ET1");
        private static readonly EVSETemplate_Id             ET2      = EVSETemplate_Id.           Parse("DE*GEF*ET2");
        private static readonly EVSETemplate_Id             ET3      = EVSETemplate_Id.           Parse("DE*GEF*ET3");

        #endregion

        #region NewOperator(SecondFirst = true)

        /// <summary>
        /// A pool template with T1 for the pool, a station template with T2 for the first station, an EVSE template with T3
        /// for the second and the fourth EVSE, another one with T1 for the fourth EVSE, by default named before the one with
        /// T3, and one without a tariff for the third EVSE; the first EVSE has T4 of its own.
        /// </summary>
        private static ChargingStationOperator NewOperator(Boolean SecondFirst = true)

            => new (GEF,
                    Created:                   Created,
                    ChargingTariffs:           [ .. new[] { T1, T2, T3, T4 }.Select(tariffId => new ChargingTariff(tariffId,
                                                                                                                   Currency.EUR,
                                                                                                                   [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                   Created: Created)) ],
                    ChargingPoolTemplates:     [ new ChargingPoolTemplate   (ChargingPoolTemplate_Id.   Parse("DE*GEF*PT1"), ChargingTariffId: T1, Created: Created, LastChange: Changed) ],
                    ChargingStationTemplates:  [ new ChargingStationTemplate(ChargingStationTemplate_Id.Parse("DE*GEF*ST1"), ChargingTariffId: T2, Created: Created) ],
                    EVSETemplates:             [ new EVSETemplate           (ET1,                                            ChargingTariffId: T3, Created: Created),
                                                 new EVSETemplate           (ET2,                                            ChargingTariffId: T1, Created: Created),
                                                 new EVSETemplate           (ET3,                                            MaxPower: Watt.FromW(11000), Created: Created) ],
                    ChargingPools:             [ new ChargingPool(P1,
                                                                  TemplateIds:       [ ChargingPoolTemplate_Id.Parse("DE*GEF*PT1") ],
                                                                  Created:           Created,
                                                                  ChargingStations:  [ new ChargingStation(S1,
                                                                                                           TemplateIds:  [ ChargingStationTemplate_Id.Parse("DE*GEF*ST1") ],
                                                                                                           Created:      Created,
                                                                                                           EVSEs:        [ new EVSE(E1, ChargingTariffIds: [ T4 ], TemplateIds: [ ET1 ], Created: Created),
                                                                                                                           new EVSE(E2,                              TemplateIds: [ ET1 ], Created: Created) ]),
                                                                                       new ChargingStation(S2,
                                                                                                           Created:      Created,
                                                                                                           EVSEs:        [ new EVSE(E3, TemplateIds: [ ET3 ], Created: Created),
                                                                                                                           new EVSE(E4, TemplateIds: SecondFirst ? [ ET2, ET1 ] : [ ET1, ET2 ], Created: Created) ]) ]) ]);

        #endregion


        #region The_most_specific_templates_decide()

        [Test]
        public void The_most_specific_templates_decide()
        {

            var gef = NewOperator();

            gef.TryGetEffectiveEVSE(E1, out var e1);
            gef.TryGetEffectiveEVSE(E2, out var e2);
            gef.TryGetEffectiveEVSE(E3, out var e3);
            gef.TryGetEffectiveEVSE(E4, out var e4);

            using (Assert.EnterMultipleScope())
            {

                // Its own; its EVSE template's before its station template's; its pool template's, when its EVSE
                // template names none.
                Assert.That(e1!.ChargingTariffIds,                                       Is.EqualTo(new[] { T4 }));
                Assert.That(e2!.ChargingTariffIds,                                       Is.EqualTo(new[] { T3 }));
                Assert.That(e3!.ChargingTariffIds,                                       Is.EqualTo(new[] { T1 }));
                Assert.That(e3.MaxPower,                                                 Is.EqualTo(Watt.FromW(11000)));

                // Of two EVSE templates the first one.
                Assert.That(e4!.ChargingTariffIds,                                       Is.EqualTo(new[] { T1 }));
                Assert.That(NewOperator(SecondFirst: false).EffectiveEVSE(e4).ChargingTariffIds,  Is.EqualTo(new[] { T3 }));

                // With the last change of the template whose tariff it takes.
                Assert.That(e3.LastChangeDate,                                           Is.EqualTo(Changed));
                Assert.That(gef.EffectiveChargingPool(gef.ChargingPools.Single()).ChargingStations.Last().EVSEs.First().ChargingTariffIds,  Is.EqualTo(new[] { T1 }));

                // As given: without tariffs of their own.
                Assert.That(gef.EVSEs.Select(evse => evse.ChargingTariffIds.Length),     Is.EqualTo(new[] { 1, 0, 0, 0 }));

            }

        }

        #endregion

    }

}
