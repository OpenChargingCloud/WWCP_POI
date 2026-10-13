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

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// The runtime status of the entities of a history, and the announcements of its commits.
    /// </summary>
    [TestFixture]
    public class RuntimeStatusTests
    {

        #region Data

        private const           String          Operator  = nameof(ChargingStationOperator);
        private static readonly DateTimeOffset  Created   = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset  T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF  = ChargingStationOperator_Id.Parse("DE*GEF");

        #endregion

        #region NewHistory()

        private static RoamingNetworkHistory NewHistory()

            => RoamingNetworkHistory.Start(
                   new RoamingNetwork(
                       Id:                        RoamingNetwork_Id.Parse("rn1"),
                       Created:                   Created,
                       ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                I18NString.Create(Languages.en, "GraphDefined"),
                                                                                Created: Created) ]
                   ).WithKeys()
               );

        #endregion

        #region Publish(History, Changes)

        private static RoamingNetworkCommitResult Publish(RoamingNetworkHistory              History,
                                                           params RoamingNetworkChange[]      Changes)

            => History.Commit(History.Head.RoamingNetwork.CreateSignedChangeSet(Changes,
                                                                            Description:  I18NString.Create(Languages.en, "Change " + (History.Head.Commit.Revision + 1)),
                                                                            CreatedAt:    T0.AddMinutes((Double) History.Head.Commit.Revision + 1)));

        #endregion

        #region GEFOf(History)

        private static ChargingStationOperator GEFOf(RoamingNetworkHistory History)

            => History.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!;

        #endregion


        #region New_entities_are_operational_and_available_since_their_creation()

        [Test]
        public void New_entities_are_operational_and_available_since_their_creation()
        {

            var all = NewHistory().RuntimeStatus.All;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(all.Select(status => $"{status.EntityType} {status.Kind} {status.Value}"),
                            Is.EqualTo(new[] {
                                "ChargingStationOperator Status available",
                                "ChargingStationOperator AdminStatus operational",
                                "RoamingNetwork Status available",
                                "RoamingNetwork AdminStatus operational"
                            }));
                Assert.That(all.Select(status => status.Timestamp),  Is.All.EqualTo(Created));
            }

        }

        #endregion

        #region A_status_is_updated_and_announced()

        [Test]
        public void A_status_is_updated_and_announced()
        {

            var history  = NewHistory();
            var updates  = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            var first     = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline",   T0,               out var firstUpdate, out _);
            var same      = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline",   T0.AddMinutes(1), out _, out _);
            var second    = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0.AddMinutes(2), out var update, out _);
            var outdated  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline",   T0.AddMinutes(1), out _, out var outdatedError);
            var admin     = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), "rn1", "outOfService", T0, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(first,                   Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(firstUpdate!.OldValue,   Is.EqualTo("available"), "Available since its creation.");
                Assert.That(same,                    Is.EqualTo(POIStatusUpdateResult.Unchanged));
                Assert.That(second,                  Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(outdated,                Is.EqualTo(POIStatusUpdateResult.Outdated));
                Assert.That(outdatedError,           Does.Contain("since"));
                Assert.That(admin,                   Is.EqualTo(POIStatusUpdateResult.Updated));

                Assert.That(updates.Select(statusUpdate => statusUpdate.Value),  Is.EqualTo(new[] { "offline", "available", "outOfService" }));
                Assert.That(update!.OldValue,        Is.EqualTo("offline"));
                Assert.That(update.OldTimestamp,     Is.EqualTo(T0), "The same status again keeps the time since when it holds.");
                Assert.That(updates[1].OldTimestamp, Is.EqualTo(T0));

                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var current),  Is.True);
                Assert.That(current!.Value,          Is.EqualTo("available"));
                Assert.That(current.Timestamp,       Is.EqualTo(T0.AddMinutes(2)));
                Assert.That(current.OldValue,        Is.Null);

                // Typed, at the entity. The outdated one repeats the status in effect then, so its history keeps no entry for it.
                Assert.That(GEFOf(history).Status.Value,  Is.EqualTo(ChargingStationOperatorStatusType.Available));
                Assert.That(GEFOf(history).StatusSchedule().Select(status => status.Timestamp),
                            Is.EqualTo(new[] { T0.AddMinutes(2), T0, Created }));

            }

        }

        #endregion

        #region Unknown_entities_and_values_are_refused()

        [Test]
        public void Unknown_entities_and_values_are_refused()
        {

            var history = NewHistory();
            var before  = history.RuntimeStatus.All;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator,               "DE*XYZ", "offline", T0, out _, out _),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(RoamingNetwork), "rn2",    "offline", T0, out _, out _),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, "ChargingPool",         "P1",     "offline", T0, out _, out _),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator,               "DE*GEF", " ",       T0, out _, out _),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(history.RuntimeStatus.All,  Is.EqualTo(before));
            }

        }

        #endregion

        #region Status_and_internal_data_go_along_to_new_versions()

        [Test]
        public void Status_and_internal_data_go_along_to_new_versions()
        {

            var history  = NewHistory();
            var updates  = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            var oldNetwork   = history.Head.RoamingNetwork;
            var oldOperator  = GEFOf(history);

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      Operator,               "DE*GEF", "offline",      T0, out _, out _);
            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), "rn1",    "outOfService", T0, out _, out _);
            oldOperator.InternalData.Set("connection", "c1");

            // A change of the operator: it is parsed anew from its JSON.
            Publish(history, RoamingNetworkChange.UpdateProperty(oldOperator, "telephone", null, "+49 30 1234567"));

            var newOperator  = GEFOf(history);

            // Still told of after the new version, and only once.
            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0.AddMinutes(5), out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(newOperator,                                  Is.Not.SameAs(oldOperator));
                Assert.That(newOperator.Status.Value,                     Is.EqualTo(ChargingStationOperatorStatusType.Available));
                Assert.That(newOperator.StatusSchedule().Count(),         Is.EqualTo(3));
                Assert.That(newOperator.InternalData.Get("connection"),   Is.EqualTo("c1"));
                Assert.That(history.Head.RoamingNetwork.AdminStatus.Value,  Is.EqualTo(RoamingNetworkAdminStatusType.OutOfService));

                // The status belongs to the entity, not to a version: the old ones show it too.
                Assert.That(oldOperator.Status.Value,                     Is.EqualTo(ChargingStationOperatorStatusType.Available));
                Assert.That(oldNetwork.AdminStatus.Value,                 Is.EqualTo(RoamingNetworkAdminStatusType.OutOfService));

                Assert.That(updates.Select(statusUpdate => statusUpdate.Value),  Is.EqualTo(new[] { "offline", "outOfService", "available" }));

            }

        }

        #endregion

        #region The_status_is_dropped_with_its_entity()

        [Test]
        public void The_status_is_dropped_with_its_entity()
        {

            var history = NewHistory();

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0, out _, out _);

            Publish(history, RoamingNetworkChange.Remove(GEF));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out _),  Is.False);
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0.AddHours(1), out _, out _),
                            Is.EqualTo(POIStatusUpdateResult.Refused));
            }

        }

        #endregion

        #region A_status_set_at_the_entity_is_told_of_too()

        [Test]
        public void A_status_set_at_the_entity_is_told_of_too()
        {

            var history  = NewHistory();
            var updates  = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            // Typed, at the entity of the head.
            GEFOf(history).SetStatus(ChargingStationOperatorStatusType.Offline, T0);

            // A change set only prepared, never published: it shares the schedules, but must not disturb.
            history.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(GEFOf(history), "telephone", null, "+49 30 7654321") ],
                                                        CreatedAt: T0);

            history.Head.RoamingNetwork.SetAdminStatus(RoamingNetworkAdminStatusType.OutOfService, T0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updates.Select(statusUpdate => $"{statusUpdate.EntityType} {statusUpdate.Kind} {statusUpdate.Value}"),
                            Is.EqualTo(new[] { "ChargingStationOperator Status offline", "RoamingNetwork AdminStatus outOfService" }));
                Assert.That(updates[0].OldValue,  Is.EqualTo("available"));
            }

        }

        #endregion

        #region A_planned_status_is_told_of_when_it_is_due()

        [Test]
        public async Task A_planned_status_is_told_of_when_it_is_due()
        {

            var history  = NewHistory();
            var told     = new TaskCompletionSource<POIStatusUpdate>(TaskCreationOptions.RunContinuationsAsynchronously);

            history.RuntimeStatus.OnStatusUpdate += statusUpdate => told.TrySetResult(statusUpdate);

            var due      = Timestamp.Now.AddMilliseconds(300);
            var result   = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, Operator, "DE*GEF", "outOfService", due, out _, out _);

            var before   = GEFOf(history).AdminStatus.Value;
            var update   = await told.Task.WaitAsync(TimeSpan.FromSeconds(5));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result,                                  Is.EqualTo(POIStatusUpdateResult.Scheduled));
                Assert.That(before,                                  Is.EqualTo(ChargingStationOperatorAdminStatusType.Operational));
                Assert.That(update.Value,                            Is.EqualTo("outOfService"));
                Assert.That(update.Timestamp,                        Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(due.ToUnixTimeMilliseconds())));
                Assert.That(Timestamp.Now,                           Is.GreaterThanOrEqualTo(due));
                Assert.That(GEFOf(history).AdminStatus.Value,        Is.EqualTo(ChargingStationOperatorAdminStatusType.OutOfService));
            }

        }

        #endregion

        #region Describe(Statuses)

        private static String[] Describe(IEnumerable<POIStatusUpdate> Statuses)

            => Statuses.Select(status => $"{status.EntityType} {status.EntityId} {status.Kind} {status.Value} {status.Timestamp:O}").ToArray();

        #endregion

        #region A_snapshot_brings_the_runtime_status_of_its_moment()

        [Test]
        public void A_snapshot_brings_the_runtime_status_of_its_moment()
        {

            var leader = NewHistory();

            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      Operator,               "DE*GEF", "offline",      T0, out _, out _);
            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), "rn1",    "outOfService", T0, out _, out _);

            var snapshotJSON    = leader.ToSnapshotJSON();
            var withoutStatus   = leader.ToSnapshotJSON(IncludeRuntimeStatus: false);

            var fromJSON        = RoamingNetworkHistory.TryFromSnapshot(snapshotJSON.ToString(),  out var followerJSON,  out var errorJSON);
            var fromCBOR        = RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotCBOR(),  out var followerCBOR,  out var errorCBOR);
            var fromNoStatus    = RoamingNetworkHistory.TryFromSnapshot(withoutStatus,            out var followerPlain, out var errorPlain);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON,                                         Is.True, errorJSON);
                Assert.That(fromCBOR,                                         Is.True, errorCBOR);
                Assert.That(fromNoStatus,                                     Is.True, errorPlain);

                Assert.That(snapshotJSON["timestamp"],                        Is.Not.Null);
                Assert.That(snapshotJSON["runtimeStatus"]!.Count(),           Is.EqualTo(4));
                Assert.That(withoutStatus["runtimeStatus"],                   Is.Null);

                // The leader's status, with the leader's timestamps.
                Assert.That(Describe(followerJSON!.RuntimeStatus.All),        Is.EqualTo(Describe(leader.RuntimeStatus.All)));
                Assert.That(Describe(followerCBOR!.RuntimeStatus.All),        Is.EqualTo(Describe(leader.RuntimeStatus.All)));

                // Without it: as new, since the creation.
                Assert.That(followerPlain!.RuntimeStatus.All.Select(status => status.Timestamp),  Is.All.EqualTo(Created));

            }

        }

        #endregion

        #region An_invalid_runtime_status_refuses_the_snapshot()

        [Test]
        public void An_invalid_runtime_status_refuses_the_snapshot()
        {

            var leader            = NewHistory();

            var unknownEntity     = leader.ToSnapshotJSON();
            unknownEntity["runtimeStatus"]![0]!["entityId"] = "DE*XYZ";

            var invalidStatus     = leader.ToSnapshotJSON();
            invalidStatus["runtimeStatus"]![0]!["value"] = " ";

            var noArray           = leader.ToSnapshotJSON();
            noArray["runtimeStatus"] = "all fine";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(unknownEntity, out var history1, out var error1),  Is.False);
                Assert.That(error1,                                                                                Does.Contain("runtime status"));
                Assert.That(history1,                                                                              Is.Null);
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(invalidStatus, out _,            out _),           Is.False);
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(noArray,       out _,            out _),           Is.False);
            }

        }

        #endregion

        #region A_follower_takes_the_status_updates_of_the_leader()

        [Test]
        public void A_follower_takes_the_status_updates_of_the_leader()
        {

            var leader    = NewHistory();
            RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out _);

            var updates   = new List<POIStatusUpdate>();
            leader.RuntimeStatus.OnStatusUpdate += updates.Add;

            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline",   T0,               out _, out _);
            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0.AddMinutes(1), out _, out _);

            // Over the wire, and out of order.
            var received  = updates.Select(update => { POIStatusUpdate.TryParse(update.ToJSON(), out var parsed, out _); return parsed!; }).Reverse().ToArray();

            var results   = received.Select(update => follower!.RuntimeStatus.TryUpdate(update, out _)).ToArray();

            follower!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status);

            using (Assert.EnterMultipleScope())
            {
                // Available since its creation already, but now since the leader's timestamp: the older offline is out of date.
                Assert.That(results,           Is.EqualTo(new[] { POIStatusUpdateResult.Unchanged, POIStatusUpdateResult.Outdated }));
                Assert.That(status!.Value,     Is.EqualTo("available"));
                Assert.That(status.Timestamp,  Is.EqualTo(T0.AddMinutes(1)));
            }

        }

        #endregion

        #region Commits_are_announced_in_their_order()

        [Test]
        public void Commits_are_announced_in_their_order()
        {

            var leader         = NewHistory();
            RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out _);

            var announcements  = new List<RoamingNetworkCommitAnnouncement>();
            var followed       = new List<RoamingNetworkCommitAnnouncement>();

            leader.   OnCommitAnnouncement += announcements.Add;
            follower!.OnCommitAnnouncement += followed.Add;

            var root = leader.Head.Commit.Id;

            var first = leader.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, "v1") ],
                                                                  Description:  I18NString.Create(Languages.en, "Change 1"),
                                                                  CreatedAt:    T0);

            leader.Commit(first);
            Publish(leader, RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, "v2"));

            // Published again: no new commit, no announcement.
            var again = leader.Commit(first);
            leader.TryGetCommitsSince(root, out var commits, out _);
            follower.TryAppend(commits!, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(again.Status,                                                  Is.EqualTo(RoamingNetworkCommitStatus.AlreadyCommitted));
                Assert.That(announcements.Select(announcement => announcement.Revision),  Is.EqualTo(new UInt64[] { 1, 2 }));
                Assert.That(announcements[0].ParentIds,                                    Is.EqualTo(new[] { root }));
                Assert.That(announcements[1].ParentIds,                                    Is.EqualTo(new[] { announcements[0].CommitId }));
                Assert.That(announcements[0].ToJSON()["description"]!["en"]!.ToString(),  Is.EqualTo("Change 1"));
                Assert.That(announcements[0].ToJSON()["changes"],                          Is.Null);

                Assert.That(followed.Select(announcement => announcement.CommitId),        Is.EqualTo(announcements.Select(announcement => announcement.CommitId)));

            }

        }

        #endregion

    }

}
