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
    /// The last change of an effective member never goes back: not when a change set moves its charging station to
    /// another charging pool, whose group gives it another tariff, and not after a restart or in a history started from
    /// a snapshot, which keep the floors of the last changes.
    /// </summary>
    [TestFixture]
    public class EffectiveLastChangeTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-18T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingPool_Id             P2       = ChargingPool_Id.           Parse("DE*GEF*P2");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly ChargingStation_Id          S2       = ChargingStation_Id.        Parse("DE*GEF*S2");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E2*1");
        private static readonly ChargingTariff_Id           T1       = ChargingTariff_Id.         Parse("DE*GEF*T1");
        private static readonly ChargingTariff_Id           T2       = ChargingTariff_Id.         Parse("DE*GEF*T2");
        private static readonly EVSEGroup_Id                EG1      = EVSEGroup_Id.              Parse("DE*GEF*EG1");

        #endregion

        #region NewNetwork()

        /// <summary>
        /// Two charging pools, each in a group giving its EVSEs a tariff of its own, T1 and T2; a station with one EVSE in
        /// each; the given EVSEs in a group giving them 22 kW. Everything of the creation, the groups as well.
        /// </summary>
        private static RoamingNetwork NewNetwork(params EVSE_Id[] Of22kW)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    GEF,
                                                    Created:             Created,
                                                    ChargingTariffs:     [ .. new[] { T1, T2 }.Select(tariffId => new ChargingTariff(tariffId,
                                                                                                                                     Currency.EUR,
                                                                                                                                     [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                                     Created: Created)) ],
                                                    ChargingPools:       [ new ChargingPool(P1,
                                                                                            Created:           Created,
                                                                                            ChargingStations:  [ new ChargingStation(S1, Created: Created, EVSEs: [ new EVSE(E1, Created: Created) ]) ]),
                                                                           new ChargingPool(P2,
                                                                                            Created:           Created,
                                                                                            ChargingStations:  [ new ChargingStation(S2, Created: Created, EVSEs: [ new EVSE(E2, Created: Created) ]) ]) ],
                                                    EVSEGroups:          [ new EVSEGroup(EG1, EVSEIds: Of22kW, MaxPower: Watt.FromW(22000), Created: Created) ],
                                                    ChargingPoolGroups:  [ new ChargingPoolGroup(ChargingPoolGroup_Id.Parse("DE*GEF*PG1"), ChargingPoolIds: [ P1 ], ChargingTariffId: T1, Created: Created),
                                                                           new ChargingPoolGroup(ChargingPoolGroup_Id.Parse("DE*GEF*PG2"), ChargingPoolIds: [ P2 ], ChargingTariffId: T2, Created: Created) ]) ]).WithKeys();

        private static ChargingStationOperator GEFOf(RoamingNetwork Network)

            => Network.GetChargingStationOperatorById(GEF)!;

        private static EVSE EffectiveE1Of(RoamingNetworkHistory History)
        {
            Assert.That(GEFOf(History.Head.RoamingNetwork).TryGetEffectiveEVSE(E1, out var e1), Is.True);
            return e1!;
        }

        #endregion

        #region (static) LeaveTheGroup(History)

        /// <summary>
        /// The first EVSE leaves the group of 22 kW EVSEs: it is as it is given, but changed now, though it did not.
        /// </summary>
        private static void LeaveTheGroup(RoamingNetworkHistory History)
        {

            var rn1 = History.Head.RoamingNetwork;

            rn1.TryGetEVSEGroupById(EG1, out var eg1);

            Assert.That(History.Commit(rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(eg1!, "EVSEIds", null, new JArray(E2.ToString())) ],
                                                                 CreatedAt: T0)).IsCommitted);

            Assert.That(EffectiveE1Of(History).LastChangeDate, Is.EqualTo(T0));

        }

        #endregion


        #region A_station_moved_to_another_pool_changes_the_last_change_of_its_EVSEs_and_keeps_their_status()

        /// <summary>
        /// Removed from the first pool and added to the second one in one change set: its EVSE takes the tariff of the
        /// group of the second pool, though neither it nor a group changed, so it changed now. It and its station keep
        /// their status: between the removal and the addition they did not exist.
        /// </summary>
        [Test]
        public void A_station_moved_to_another_pool_changes_the_last_change_of_its_EVSEs_and_keeps_their_status()
        {

            // Its EVSE in no group: a group naming it would name what does not exist between the removal and the addition.
            var history  = RoamingNetworkHistory.Start(NewNetwork(E2));
            var rn1      = history.Head.RoamingNetwork;

            GEFOf(rn1).TryGetEVSE           (E1, out var e1);
            GEFOf(rn1).TryGetChargingStation(S1, out var s1);
            e1!.SetStatus(EVSEStatusType.Charging);
            s1!.SetStatus(ChargingStationStatusType.Error);

            var s1JSON   = (JObject) rn1.ToJSON().SelectToken($"$.chargingStationOperators[0].chargingPools[0].chargingStations[0]")!.DeepClone();

            Assert.That(history.Commit(rn1.CreateSignedChangeSet([
                            RoamingNetworkChange.Remove(nameof(ChargingStation), S1.ToString()),
                            RoamingNetworkChange.Add   (nameof(ChargingStation), S1.ToString(), s1JSON, nameof(ChargingPool), P2.ToString())
                        ], CreatedAt: T0)).IsCommitted);

            var gef2 = GEFOf(history.Head.RoamingNetwork);

            gef2.TryGetEVSE           (E1, out var movedE1);
            gef2.TryGetChargingStation(S1, out var movedS1);
            gef2.TryGetEffectiveChargingPool(P2, out var effectiveP2);

            var effectiveE1 = EffectiveE1Of(history);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(effectiveP2!.ChargingStations.Select(chargingStation => chargingStation.Id),  Is.EquivalentTo(new[] { S1, S2 }));

                Assert.That(effectiveE1.ChargingTariffIds,                                               Is.EqualTo(new[] { T2 }));
                Assert.That(movedE1!.LastChangeDate,                                                     Is.EqualTo(Created));
                Assert.That(effectiveE1.LastChangeDate,                                                  Is.EqualTo(T0));

                Assert.That(movedE1.Status.Value,                                                        Is.EqualTo(EVSEStatusType.Charging));
                Assert.That(effectiveE1.Status.Value,                                                    Is.EqualTo(EVSEStatusType.Charging));
                Assert.That(movedS1!.Status.Value,                                                       Is.EqualTo(ChargingStationStatusType.Error));

            }

        }

        #endregion

        #region A_snapshot_keeps_the_last_changes_of_effective_members()

        /// <summary>
        /// A history started from a snapshot, as JSON, JSON text or CBOR, tells the last change the member left its group:
        /// the snapshot keeps it as an effective last change, as it is neither content nor runtime status.
        /// </summary>
        [Test]
        public void A_snapshot_keeps_the_last_changes_of_effective_members()
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork(E1, E2));

            LeaveTheGroup(history);

            var snapshot  = history.ToSnapshotJSON();

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot,                     out var fromJSON, out var errorJSON), Is.True, errorJSON);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot.ToString(),          out var fromText, out var errorText), Is.True, errorText);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(history.ToSnapshotCBOR(),     out var fromCBOR, out var errorCBOR), Is.True, errorCBOR);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(snapshot["effectiveLastChanges"],                Is.EqualTo(new JArray(new JObject(new JProperty("entityType",  nameof(EVSE)),
                                                                                                               new JProperty("entityId",    E1.ToString()),
                                                                                                               new JProperty("lastChange",  T0.ToISO8601())))));

                Assert.That(EffectiveE1Of(fromJSON!).LastChangeDate,         Is.EqualTo(T0));
                Assert.That(EffectiveE1Of(fromText!).LastChangeDate,         Is.EqualTo(T0));
                Assert.That(EffectiveE1Of(fromCBOR!).LastChangeDate,         Is.EqualTo(T0));

                // Content it is not: the same ETags.
                Assert.That(fromJSON!.Head.RoamingNetwork.ETags,             Is.EqualTo(history.Head.RoamingNetwork.ETags));

            }

        }

        #endregion

        #region A_compacted_store_keeps_the_last_changes_of_effective_members()

        /// <summary>
        /// The commit in which the member left its group is compacted into the base of the store: loaded again, the
        /// history still tells its last change, instead of the one of the member itself.
        /// </summary>
        [Test]
        public void A_compacted_store_keeps_the_last_changes_of_effective_members()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = RoamingNetworkHistory.Start(NewNetwork(E1, E2), Store: store);

            history.Retention = new RoamingNetworkHistoryRetention { RetainCommits = 0, RecentVersions = 1, VersionCheckpointInterval = 1 };

            LeaveTheGroup(history);

            Assert.That(history.Compact(out var compactError), Is.True, compactError);

            history.DetachStore();

            Assert.That(store.Load()!.Base["commit"]?["revision"]?.Value<Int32>(),      Is.EqualTo(1), "The commit was not compacted into the base.");
            Assert.That(store.Load()!.Base["effectiveLastChanges"],                      Is.Not.Null);

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var error), Is.True, error);
            Assert.That(EffectiveE1Of(loaded!).LastChangeDate,                          Is.EqualTo(T0));

        }

        #endregion

    }

}
