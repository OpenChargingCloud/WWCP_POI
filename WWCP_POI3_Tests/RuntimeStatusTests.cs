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
    /// The runtime status of a history and the announcements of its commits.
    /// </summary>
    [TestFixture]
    public class RuntimeStatusTests
    {

        #region Data

        private const           String          Operator  = nameof(ChargingStationOperator);
        private static readonly DateTimeOffset  T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        #endregion

        #region NewHistory()

        private static RoamingNetworkHistory NewHistory()

            => RoamingNetworkHistory.Start(
                   new RoamingNetwork(
                       Id:                        RoamingNetwork_Id.Parse("rn1"),
                       Created:                   DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                       ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                                I18NString.Create(Languages.en, "GraphDefined"),
                                                                                Created: DateTimeOffset.Parse("2026-01-01T00:00:00Z")) ]
                   )
               );

        #endregion

        #region Publish(History, Changes)

        private static RoamingNetworkPublishResult Publish(RoamingNetworkHistory              History,
                                                           params RoamingNetworkChange[]      Changes)

            => History.Publish(History.Head.RoamingNetwork.CreateChangeSet(Changes,
                                                                            Description:  I18NString.Create(Languages.en, "Change " + (History.Head.Commit.Revision + 1)),
                                                                            CreatedAt:    T0.AddMinutes((Double) History.Head.Commit.Revision + 1)));

        #endregion


        #region A_status_is_updated_and_announced()

        [Test]
        public void A_status_is_updated_and_announced()
        {

            var history  = NewHistory();
            var updates  = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            var first     = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0,               out _, out _);
            var same      = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0.AddMinutes(1), out _, out _);
            var second    = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline",   T0.AddMinutes(2), out var update, out _);
            var outdated  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0.AddMinutes(1), out _, out var outdatedError);
            var admin     = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(RoamingNetwork), "rn1", "operational", T0, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(first,                   Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(same,                    Is.EqualTo(POIStatusUpdateResult.Unchanged));
                Assert.That(second,                  Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(outdated,                Is.EqualTo(POIStatusUpdateResult.Outdated));
                Assert.That(outdatedError,           Does.Contain("since"));
                Assert.That(admin,                   Is.EqualTo(POIStatusUpdateResult.Updated));

                Assert.That(updates.Count,           Is.EqualTo(3));
                Assert.That(update!.OldValue,        Is.EqualTo("available"));
                Assert.That(update.OldTimestamp,     Is.EqualTo(T0), "The same status again keeps the time since when it holds.");

                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var current),  Is.True);
                Assert.That(current!.Value,          Is.EqualTo("offline"));
                Assert.That(current.OldValue,        Is.Null);
                Assert.That(history.RuntimeStatus.All.Count,  Is.EqualTo(2));

            }

        }

        #endregion

        #region Unknown_entities_and_values_are_refused()

        [Test]
        public void Unknown_entities_and_values_are_refused()
        {

            var history = NewHistory();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator,               "DE*XYZ", "available", T0, out _, out _),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(RoamingNetwork), "rn2",    "available", T0, out _, out _),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, "ChargingPool",         "P1",     "available", T0, out _, out _),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator,               "DE*GEF", " ",         T0, out _, out _),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(history.RuntimeStatus.All,  Is.Empty);
            }

        }

        #endregion

        #region The_status_is_kept_across_versions_and_dropped_with_its_entity()

        [Test]
        public void The_status_is_kept_across_versions_and_dropped_with_its_entity()
        {

            var history = NewHistory();

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0, out _, out _);

            Publish(history, RoamingNetworkChange.UpdateProperty(history.Head.RoamingNetwork, "dataSource", null, "v1"));

            var kept = history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out _);

            Publish(history, RoamingNetworkChange.Remove(ChargingStationOperator_Id.Parse("DE*GEF")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(kept,                                                                                    Is.True);
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out _),  Is.False);
                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0.AddHours(1), out _, out _),
                            Is.EqualTo(POIStatusUpdateResult.Refused));
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

            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0,               out _, out _);
            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline",   T0.AddMinutes(1), out _, out _);

            // Over the wire, and out of order.
            var received  = updates.Select(update => { POIStatusUpdate.TryParse(update.ToJSON(), out var parsed, out _); return parsed!; }).Reverse().ToArray();

            var results   = received.Select(update => follower!.RuntimeStatus.TryUpdate(update, out _)).ToArray();

            follower!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results,          Is.EqualTo(new[] { POIStatusUpdateResult.Updated, POIStatusUpdateResult.Outdated }));
                Assert.That(status!.Value,     Is.EqualTo("offline"));
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

            var first = leader.Head.RoamingNetwork.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, "v1") ],
                                                                  Description:  I18NString.Create(Languages.en, "Change 1"),
                                                                  CreatedAt:    T0);

            leader.Publish(first);
            Publish(leader, RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, "v2"));

            // Published again: no new commit, no announcement.
            var again = leader.Publish(first);
            leader.TryGetCommitsSince(root, out var commits, out _);
            follower.TryAppend(commits!, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(again.Status,                                                  Is.EqualTo(RoamingNetworkPublishStatus.AlreadyPublished));
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
