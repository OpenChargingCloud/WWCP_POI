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
    /// The energy status of EVSEs and charging stations: runtime data like the admin status and the status.
    /// </summary>
    [TestFixture]
    public class EnergyStatusTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                Created:           Created,
                                                                                                                ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                         Created:  Created,
                                                                                                                                                         EVSEs:    [ new EVSE(E1, MaxPower: Watt.FromW(22000), Created: Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Energy(Used, Available, Stored = null)

        private static String Energy(Decimal Used, Decimal Available, Decimal? Stored = null)

            => POIEnergyStatus.ToText(new EnergyInfo(Watt.FromW(Used), Watt.FromW(Available), Stored.HasValue ? WattHour.FromWh(Stored.Value) : null));

        #endregion


        #region The_energy_status_is_set_and_told_of()

        [Test]
        public void The_energy_status_is_set_and_told_of()
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var updates   = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            var none      = history.RuntimeStatus.TryGet(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE), E1.ToString(), out _);

            var evse      = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE),            E1.ToString(), Energy(11000, 22000),        T0,               out var evseUpdate, out _);
            var same      = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE),            E1.ToString(), Energy(11000, 22000),        T0.AddSeconds(30), out _, out _);
            var station   = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.EnergyStatus, nameof(ChargingStation), S1.ToString(), Energy(11000, 50000, 5000),  T0,               out _, out _);
            var older     = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE),            E1.ToString(), Energy(3700,  22000),        T0.AddMinutes(-1), out _, out _);
            var network   = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.EnergyStatus, nameof(RoamingNetwork),  "rn1",         Energy(1, 2),                T0,               out _, out var networkError);
            var invalid   = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE),            E1.ToString(), "{\"used\":-1,\"available\":2}", T0, out _, out _);

            history.RuntimeStatus.TryGet(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE), E1.ToString(), out var evseStatus);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(none,                                    Is.False, "Without a reported energy status, there is none.");
                Assert.That(evse,                                    Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(same,                                    Is.EqualTo(POIStatusUpdateResult.Unchanged));
                Assert.That(station,                                 Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(older,                                   Is.EqualTo(POIStatusUpdateResult.Outdated));
                Assert.That(network,                                 Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(networkError,                            Does.Contain("has no energy status"));
                Assert.That(invalid,                                 Is.EqualTo(POIStatusUpdateResult.Refused));

                Assert.That(evseStatus!.Value,                       Is.EqualTo(Energy(11000, 22000)));
                Assert.That(updates.Select(update => $"{update.EntityType} {update.Kind}"),
                            Is.EqualTo(new[] { "EVSE EnergyStatus", "ChargingStation EnergyStatus" }));

                // On the wire, the value is an object in W, W and Wh.
                var json = updates[1].ToJSON();
                Assert.That(json["kind"]!.Value<String>(),                   Is.EqualTo("energyStatus"));
                Assert.That(json["value"]!["available"]!.Value<Decimal>(),   Is.EqualTo(50000));
                Assert.That(json["value"]!["storedEnergy"]!.Value<Decimal>(), Is.EqualTo(5000));
                Assert.That(POIStatusUpdate.TryParse(json, out var parsed, out var parseError),  Is.True, parseError);
                Assert.That(parsed!.Value,                                   Is.EqualTo(updates[1].Value));

                Assert.That(evseUpdate!.Value,                               Is.EqualTo(Energy(11000, 22000)));

            }

        }

        #endregion

        #region The_energy_status_survives_changes_and_reaches_followers()

        [Test]
        public void The_energy_status_survives_changes_and_reaches_followers()
        {

            var leader = RoamingNetworkHistory.Start(NewNetwork());

            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE), E1.ToString(), Energy(7400, 22000), T0, out _, out _);

            // A change of the EVSE keeps its energy status.
            leader.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.TryGetEVSE(E1, out var e1);
            leader.Publish(leader.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(e1!, "maxPower", null, 11000) ], CreatedAt: T0.AddMinutes(1)));

            leader.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.TryGetEVSE(E1, out var changedE1);

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotCBOR(), out var follower, out var error),  Is.True, error);

            follower!.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.TryGetEVSE(E1, out var followerE1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changedE1!.EnergyStatus!.Value.Value.Used,         Is.EqualTo(Watt.FromW(7400)));
                Assert.That(followerE1!.EnergyStatus!.Value.Value.Available,   Is.EqualTo(Watt.FromW(22000)));
                Assert.That(followerE1.EnergyStatus!.Value.Timestamp,           Is.EqualTo(T0));
                Assert.That(leader.ToSnapshotJSON()["runtimeStatus"]!.Count(status => status["kind"]!.Value<String>() == "energyStatus"),
                            Is.EqualTo(1));
            }

        }

        #endregion

    }

}
