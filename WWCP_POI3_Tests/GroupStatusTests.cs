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
    /// The status of a group: set manually, or calculated by a delegate from the status of its members.
    /// </summary>
    [TestFixture]
    public class GroupStatusTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly ChargingTariff_Id           T1       = ChargingTariff_Id.         Parse("DE*GEF*T1");
        private static readonly EVSEGroup_Id                EG1      = EVSEGroup_Id.              Parse("DE*GEF*EG1");
        private static readonly ChargingTariffGroup_Id      TG1      = ChargingTariffGroup_Id.    Parse("DE*GEF*TG1");
        private static readonly ParkingOperator_Id          PAR      = ParkingOperator_Id.        Parse("DE*PAR");
        private static readonly ParkingSpace_Id             Space1   = ParkingSpace_Id.           Parse("space-1");
        private static readonly ParkingSpace_Id             Space2   = ParkingSpace_Id.           Parse("space-2");
        private static readonly ParkingSpaceGroup_Id        Level1   = ParkingSpaceGroup_Id.      Parse("level-1");

        #endregion

        #region NewHistory()

        private static RoamingNetworkHistory NewHistory()

            => RoamingNetworkHistory.Start(
                   new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                       Created:                   Created,
                       ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                Created:               Created,
                                                                                ChargingTariffs:       [ new ChargingTariff(T1,
                                                                                                                            Currency.EUR,
                                                                                                                            [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                            Created: Created) ],
                                                                                ChargingPools:         [ new ChargingPool(P1,
                                                                                                                          Created:           Created,
                                                                                                                          ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                                   Created:  Created,
                                                                                                                                                                   EVSEs:    [ new EVSE(E1, Created: Created),
                                                                                                                                                                               new EVSE(E2, Created: Created) ]) ]) ],
                                                                                EVSEGroups:            [ new EVSEGroup          (EG1, EVSEIds:           [ E1, E2 ], Created: Created) ],
                                                                                ChargingTariffGroups:  [ new ChargingTariffGroup(TG1, ChargingTariffIds: [ T1 ],     Created: Created) ]) ],
                       ParkingOperators:          [ new ParkingOperator(PAR,
                                                                        Created:             Created,
                                                                        ParkingSpaces:       [ new ParkingSpace(Space1, Created: Created),
                                                                                               new ParkingSpace(Space2, Created: Created) ],
                                                                        ParkingSpaceGroups:  [ new ParkingSpaceGroup(Level1, ParkingSpaceIds: [ Space1, Space2 ], Created: Created) ]) ]).WithKeys());

        #endregion

        #region SetStatus(History, EntityType, EntityId, Value, Timestamp)

        private static POIStatusUpdateResult SetStatus(RoamingNetworkHistory  History,
                                                       String                 EntityType,
                                                       Object                 EntityId,
                                                       String                 Value,
                                                       DateTimeOffset         Timestamp)

            => History.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, EntityType, EntityId.ToString()!, Value, Timestamp, out _, out _);

        private static String? StatusOf(RoamingNetworkHistory History, String EntityType, Object EntityId)

            => History.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, EntityType, EntityId.ToString()!, out var status)
                   ? status.Value
                   : null;

        #endregion


        #region An_EVSE_group_has_the_status_its_delegate_calculates()

        [Test]
        public void An_EVSE_group_has_the_status_its_delegate_calculates()
        {

            var history  = NewHistory();
            var updates  = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;
            history.RuntimeStatus.CalculateStatus(EG1, POIGroupStatus.OfEVSEs);

            using (Assert.EnterMultipleScope())
            {

                // Both EVSEs are available since their creation, and so is the group.
                Assert.That(StatusOf(history, nameof(EVSEGroup), EG1),                                       Is.EqualTo("available"));

                Assert.That(SetStatus(history, nameof(EVSE), E1, "charging", T0),                            Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(history, nameof(EVSEGroup), EG1),                                       Is.EqualTo("partialAvailable"));

                Assert.That(SetStatus(history, nameof(EVSE), E2, "outOfOrder", T0.AddMinutes(1)),            Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(history, nameof(EVSEGroup), EG1),                                       Is.EqualTo("charging"));

                Assert.That(SetStatus(history, nameof(EVSE), E1, "offline", T0.AddMinutes(2)),               Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(history, nameof(EVSEGroup), EG1),                                       Is.EqualTo("error"));

                // Every calculated status is a status update of its own, after the one that caused it, since then.
                var groupUpdates = updates.Where(update => update.EntityType == nameof(EVSEGroup)).ToArray();
                Assert.That(groupUpdates.Select(update => update.Value),                                     Is.EqualTo(new[] { "partialAvailable", "charging", "error" }));
                Assert.That(groupUpdates.Select(update => update.Timestamp),                                 Is.EqualTo(new[] { T0, T0.AddMinutes(1), T0.AddMinutes(2) }));
                Assert.That(updates.Select(update => update.EntityType),                                     Is.EqualTo(new[] { nameof(EVSE), nameof(EVSEGroup), nameof(EVSE), nameof(EVSEGroup), nameof(EVSE), nameof(EVSEGroup) }));

                // A calculated status names its source.
                Assert.That(groupUpdates.Select(update => update.DataSource),                                Is.All.EqualTo("calculated"));
                Assert.That(updates.Where(update => update.EntityType == nameof(EVSE)).Select(update => update.DataSource),
                                                                                                             Is.All.Null);

            }

        }

        #endregion

        #region Status_updates_name_their_source()

        /// <summary>
        /// The source a status is set with goes along with its status update, for every kind of entity.
        /// </summary>
        [Test]
        public void Status_updates_name_their_source()
        {

            var history  = NewHistory();
            var updates  = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            POIStatusUpdateResult Set(POIRuntimeStatusKind Kind, String EntityType, Object EntityId, String Value, String DataSource)
                => history.RuntimeStatus.TryUpdate(Kind, EntityType, EntityId.ToString()!, Value, T0, out _, out _, DataSource);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Set(POIRuntimeStatusKind.Status,      nameof(EVSE),                    E1,     "charging",     "ocpi"),      Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(Set(POIRuntimeStatusKind.Status,      nameof(ChargingStationOperator), GEF,    "offline",      "backend"),   Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(Set(POIRuntimeStatusKind.AdminStatus, nameof(EVSE),                    E2,     "outOfService", "operator"),  Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(Set(POIRuntimeStatusKind.Status,      nameof(ParkingSpace),            Space1, "inUse",        "sensor"),    Is.EqualTo(POIStatusUpdateResult.Updated));

                Assert.That(updates.Select(update => (update.EntityType, update.DataSource)),
                            Is.EqualTo(new[] {
                                (nameof(EVSE),                    "ocpi"),
                                (nameof(ChargingStationOperator), "backend"),
                                (nameof(EVSE),                    "operator"),
                                (nameof(ParkingSpace),            "sensor")
                            }));

            }

        }

        #endregion

        #region A_calculated_status_has_the_say()

        [Test]
        public void A_calculated_status_has_the_say()
        {

            var history = NewHistory();

            history.RuntimeStatus.CalculateStatus(EG1, POIGroupStatus.OfEVSEs);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(EVSEGroup), EG1.ToString(), "reserved", T0, out _, out var error),
                            Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(error,                                                                           Does.Contain("is calculated by a delegate, which has the say here"));

                // Also a status of another server.
                Assert.That(history.RuntimeStatus.TryUpdate(new POIStatusUpdate(POIRuntimeStatusKind.Status, nameof(EVSEGroup), EG1.ToString(), T0, "reserved", null, null, null), out _),
                            Is.EqualTo(POIStatusUpdateResult.Refused));

                // Not the admin status: it is always set manually.
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(EVSEGroup), EG1.ToString(), "outOfService", T0, out _, out _),
                            Is.EqualTo(POIStatusUpdateResult.Updated));

                // Set manually again.
                history.RuntimeStatus.SetStatusManually(EG1);
                Assert.That(SetStatus(history, nameof(EVSEGroup), EG1, "reserved", T0),                      Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(SetStatus(history, nameof(EVSE), E1, "charging", T0.AddMinutes(1)),              Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(history, nameof(EVSEGroup), EG1),                                       Is.EqualTo("reserved"));

            }

        }

        #endregion

        #region All_groups_of_a_kind_are_calculated_but_those_set_manually()

        [Test]
        public void All_groups_of_a_kind_are_calculated_but_those_set_manually()
        {

            var history = NewHistory();

            history.RuntimeStatus.CalculateStatusOfAllChargingTariffGroups(POIGroupStatus.OfChargingTariffs);
            history.RuntimeStatus.CalculateStatusOfAllParkingSpaceGroups  (POIGroupStatus.OfParkingSpaces);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(SetStatus(history, nameof(ChargingTariff), T1, "expired", T0),                   Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(history, nameof(ChargingTariffGroup), TG1),                             Is.EqualTo("expired"));

                Assert.That(SetStatus(history, nameof(ParkingSpace), Space1, "inUse", T0),                   Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(history, nameof(ParkingSpaceGroup), Level1),                            Is.EqualTo("available"));
                Assert.That(SetStatus(history, nameof(ParkingSpace), Space2, "reserved", T0),                Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(history, nameof(ParkingSpaceGroup), Level1),                            Is.EqualTo("inUse"));

                // One group set manually, also while all others are calculated.
                history.RuntimeStatus.SetStatusManually(Level1);
                Assert.That(SetStatus(history, nameof(ParkingSpaceGroup), Level1, "blocked", T0.AddMinutes(1)),  Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(history.RuntimeStatus.IsCalculated(nameof(ChargingTariffGroup), TG1.ToString()),     Is.True);
                Assert.That(history.RuntimeStatus.IsCalculated(nameof(ParkingSpaceGroup),   Level1.ToString()),  Is.False);

                // And all set manually again.
                history.RuntimeStatus.CalculateStatusOfAllChargingTariffGroups(null);
                Assert.That(SetStatus(history, nameof(ChargingTariffGroup), TG1, "active", T0.AddMinutes(1)),    Is.EqualTo(POIStatusUpdateResult.Updated));

            }

        }

        #endregion

        #region A_new_version_may_change_the_members()

        [Test]
        public void A_new_version_may_change_the_members()
        {

            var history = NewHistory();

            history.RuntimeStatus.CalculateStatus(EG1, POIGroupStatus.OfEVSEs);
            SetStatus(history, nameof(EVSE), E1, "charging", T0);

            Assert.That(StatusOf(history, nameof(EVSEGroup), EG1), Is.EqualTo("partialAvailable"));

            var rn1    = history.Head.RoamingNetwork;
            rn1.TryGetEVSEGroupById(EG1, out var group);

            history.Commit(rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(group!, "EVSEIds", null, new JArray(E1.ToString())) ],
                                                     CreatedAt: T0.AddMinutes(5)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StatusOf(history, nameof(EVSEGroup), EG1),                                       Is.EqualTo("charging"));
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(EVSEGroup), EG1.ToString(), out var status) ? status.Timestamp : default,
                                                                                                             Is.EqualTo(T0.AddMinutes(5)));
            }

        }

        #endregion

        #region A_status_set_at_the_entity_is_calculated_as_well()

        [Test]
        public void A_status_set_at_the_entity_is_calculated_as_well()
        {

            var history = NewHistory();

            history.RuntimeStatus.CalculateStatus(EG1, POIGroupStatus.OfEVSEs);

            history.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.TryGetEVSE(E2, out var e2);
            e2!.SetStatus(EVSEStatusType.Reserved, T0);

            // Calculated on another thread, as the status schedule of the EVSE was locked.
            Assert.That(() => StatusOf(history, nameof(EVSEGroup), EG1), Is.EqualTo("partialAvailable").After(5000, 20));

        }

        #endregion

        #region The_default_calculation_of_charging_groups()

        [TestCase(new String[0],                                       "unknown")]
        [TestCase(new[] { "available",  "waitingForPlugin" },          "available")]
        [TestCase(new[] { "available",  "charging" },                  "partialAvailable")]
        [TestCase(new[] { "charging",   "reserved",   "offline" },     "charging")]
        [TestCase(new[] { "reserved",   "offline" },                   "reserved")]
        [TestCase(new[] { "offline",    "offline" },                   "offline")]
        [TestCase(new[] { "outOfService", "outOfService" },            "outOfService")]
        [TestCase(new[] { "offline",    "outOfOrder" },                "error")]
        [TestCase(new[] { "planned" },                                 "planned")]
        [TestCase(new[] { "planned",    "inDeployment" },              "inDeployment")]
        [TestCase(new[] { "offline",    "planned" },                   "unknown")]
        [TestCase(new[] { "removed",    "unknown",    "available" },   "available")]
        [TestCase(new[] { "some future status" },                      "unknown")]
        public void The_default_calculation_of_charging_groups(String[] Members, String Expected)
        {

            var members = Members.Select((status, index) => (Id: EVSE_Id.Parse($"DE*GEF*E9*{index + 1}"), Status: status)).
                                  ToDictionary(member => member.Id, member => new Timestamped<EVSEStatusType>(T0, EVSEStatusType.Parse(member.Status)));

            Assert.That(POIGroupStatus.OfEVSEs(new EVSEGroup(EG1, Created: Created), members).ToString(), Is.EqualTo(Expected));

        }

        #endregion

    }

}
