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

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The price status of charging pools, charging stations and EVSEs: new prices of charging tariffs at runtime,
    /// e.g. ad hoc prices following the electricity market, like the energy status without a commit.
    /// </summary>
    [TestFixture]
    public class PriceStatusTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly ChargingTariff_Id           T1       = ChargingTariff_Id.         Parse("DE*GEF*T1");
        private static readonly ChargingTariff_Id           T2       = ChargingTariff_Id.         Parse("DE*GEF*T2");

        #endregion

        #region NewNetwork() / Tariff(Id)

        private static ChargingTariff Tariff(ChargingTariff_Id Id)

            => new (Id,
                    Currency.EUR,
                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.49m, VAT: 19, StepSize: 1) ]) ],
                    Created: Created);

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:          Created,
                                                                             ChargingTariffs:  [ Tariff(T1), Tariff(T2) ],
                                                                             ChargingPools:    [ new ChargingPool(P1,
                                                                                                                  Created:           Created,
                                                                                                                  ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                           Created:  Created,
                                                                                                                                                           EVSEs:    [ new EVSE(E1, Created: Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Prices(Tariffs)

        /// <summary>
        /// The text of a price status: one energy price per kWh for each of the given tariffs.
        /// </summary>
        private static String Prices(params (ChargingTariff_Id Tariff, Decimal PerKWh)[] Tariffs)

            => new ChargingPrices(Tariffs.Select(tariff => new ChargingTariffPrices(tariff.Tariff,
                                                                                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, tariff.PerKWh, VAT: 19, StepSize: 1) ]) ]))).Text;

        #endregion


        #region The_price_status_is_set_told_of_and_takes_the_most_specific_prices()

        [Test]
        public void The_price_status_is_set_told_of_and_takes_the_most_specific_prices()
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var updates   = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            var pool      = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, nameof(ChargingPool),    P1.ToString(), Prices((T1, 0.59m), (T2, 0.69m)), T0,                out _, out var poolError);
            var station   = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, nameof(ChargingStation), S1.ToString(), Prices((T1, 0.39m)),              T0.AddMinutes(1), out _, out _);
            var evse      = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, nameof(EVSE),            E1.ToString(), Prices((T2, 0.29m)),              T0.AddMinutes(2), out _, out _);
            var same      = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, nameof(EVSE),            E1.ToString(), Prices((T2, 0.29m)),              T0.AddMinutes(3), out _, out _);

            var head      = history.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!;
            head.TryGetChargingPool   (P1, out var p1);
            head.TryGetChargingStation(S1, out var s1);
            head.TryGetEVSE           (E1, out var e1);

            var effective = ChargingPrices.Effective(p1, s1, e1!);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(pool,                                     Is.EqualTo(POIStatusUpdateResult.Updated), poolError);
                Assert.That(station,                                  Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(evse,                                     Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(same,                                     Is.EqualTo(POIStatusUpdateResult.Unchanged));

                Assert.That(updates.Select(update => $"{update.EntityType} {update.Kind}"),
                            Is.EqualTo(new[] { "ChargingPool PriceStatus", "ChargingStation PriceStatus", "EVSE PriceStatus" }));

                // For every tariff, the most specific prices: T1 of the station, T2 of the EVSE.
                Assert.That(effective.Select(price => price.Value.ChargingTariffId),                                    Is.EqualTo(new[] { T1, T2 }));
                Assert.That(effective.Select(price => price.Value.Elements[0].PriceComponents[0].Price),               Is.EqualTo(new[] { 0.39m, 0.29m }));
                Assert.That(effective.Select(price => price.Timestamp),                                                Is.EqualTo(new[] { T0.AddMinutes(1), T0.AddMinutes(2) }));

                // On the wire, the value is an object.
                var json = updates[0].ToJSON();
                Assert.That(json["kind"]!.Value<String>(),                                                             Is.EqualTo("priceStatus"));
                Assert.That(json["value"]!["prices"]![1]!["chargingTariffId"]!.Value<String>(),                        Is.EqualTo("DE*GEF*T2"));
                Assert.That(POIStatusUpdate.TryParse(json, out var parsed, out var parseError),                       Is.True, parseError);
                Assert.That(parsed!.Value,                                                                             Is.EqualTo(updates[0].Value));

                // Kept with the head, and listed as any status.
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.PriceStatus, nameof(ChargingStation), S1.ToString(), out var stationStatus), Is.True);
                Assert.That(stationStatus!.Value,                                                                      Is.EqualTo(Prices((T1, 0.39m))));

            }

        }

        #endregion

        #region Invalid_price_statuses_are_refused()

        [Test]
        public void Invalid_price_statuses_are_refused()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());

            String? Refused(String EntityType, String EntityId, String Value)
            {
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, EntityType, EntityId, Value, T0, out _, out var error),
                            Is.EqualTo(POIStatusUpdateResult.Refused), Value);
                return error;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Refused(nameof(EVSE),            E1.ToString(), Prices((ChargingTariff_Id.Parse("DE*XYZ*T1"), 0.1m))),
                            Does.Contain("is no tariff of the charging station operator 'DE*GEF'"));
                Assert.That(Refused(nameof(ChargingStation), S1.ToString(), Prices((ChargingTariff_Id.Parse("DE*GEF*T9"), 0.1m))),
                            Does.Contain("'DE*GEF*T9' of the price status is no tariff"));
                Assert.That(Refused(nameof(ChargingStationOperator), GEF.ToString(), Prices((T1, 0.1m))),
                            Does.Contain("has no price status"));
                Assert.That(Refused(nameof(EVSE), E1.ToString(), """{"prices":[{"chargingTariffId":"DE*GEF*T1","elements":[]}]}"""),
                            Is.Not.Null);
                Assert.That(Refused(nameof(EVSE), E1.ToString(), """{"prices":[{"chargingTariffId":"DE*GEF*T1","elements":[{"priceComponents":[{"type":"ENERGY","price":0.1,"stepSize":1}]}]},{"chargingTariffId":"DE*GEF*T1","elements":[{"priceComponents":[{"type":"ENERGY","price":0.2,"stepSize":1}]}]}]}"""),
                            Is.Not.Null, "A tariff only once.");
            }

        }

        #endregion

        #region The_price_status_can_be_planned_and_survives_changes()

        [Test]
        public void The_price_status_can_be_planned_and_survives_changes()
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var planned   = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusPlanned += planned.Add;

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, nameof(EVSE), E1.ToString(), Prices((T1, 0.33m)), T0, out _, out _);

            var later     = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, nameof(EVSE), E1.ToString(), Prices((T1, 0.44m)), Timestamp.Now.AddHours(1), out _, out _);

            // A change of the EVSE keeps its price status.
            history.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.TryGetEVSE(E1, out var e1);
            history.Commit(history.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(e1!, "maxPower", null, 11000) ], CreatedAt: T0.AddMinutes(1)));
            history.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.TryGetEVSE(E1, out var changedE1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(later,                                                        Is.EqualTo(POIStatusUpdateResult.Scheduled));
                Assert.That(planned.Single().Kind,                                        Is.EqualTo(POIRuntimeStatusKind.PriceStatus));
                Assert.That(changedE1!.MaxPower?.Value,                                   Is.EqualTo(11000));
                Assert.That(changedE1.PriceStatus?.Value.Text,                            Is.EqualTo(Prices((T1, 0.33m))), "The new version shares the price status of the one before.");
            }

        }

        #endregion

    }

}
