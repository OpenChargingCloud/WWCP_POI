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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// What a history keeps: versions calculated again when needed, older commits dropped, the status folded into the base.
    /// </summary>
    [TestFixture]
    public class RetentionTests
    {

        #region Data

        private static readonly DateTimeOffset  T0        = DateTimeOffset.Parse("2026-10-12T12:00:00Z");
        private const           String          Operator  = nameof(ChargingStationOperator);

        #endregion

        #region Linear(History, Count, From = 1)

        /// <summary>
        /// Commit the given number of changes of the data source, one after the other, and return their commits.
        /// </summary>
        private static List<RoamingNetworkCommit> Linear(RoamingNetworkHistory  History,
                                                         Int32                  Count,
                                                         Int32                  From   = 1)
        {

            var commits = new List<RoamingNetworkCommit>();

            for (var i = From; i < From + Count; i++)
            {
                var result = PersistenceTests.Change(History, i, "dataSource", "v" + (i + 1));
                Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);
                commits.Add(result.Commit!);
            }

            return commits;

        }

        #endregion


        #region Versions_not_kept_are_calculated_again()

        [Test]
        public void Versions_not_kept_are_calculated_again()
        {

            var history   = RoamingNetworkHistory.Start(PersistenceTests.NewNetwork(),
                                                        Retention: new RoamingNetworkHistoryRetention {
                                                                       VersionCheckpointInterval  = 5,
                                                                       RecentVersions             = 2
                                                                   });

            var first     = Linear(history, 3);
            var version3  = history.Head.RoamingNetwork;
            Linear(history, 9, From: 4);

            // Made for revision 3, long ago: its version is calculated again, and so is the merge base.
            var result    = PersistenceTests.Change(history, 20, "name", "Network", version3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status,                                     Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);
                Assert.That(result.Commit!.ParentIds,                          Is.EqualTo(new[] { first[2].Id }));
                Assert.That(history.Tips,                                      Has.Count.EqualTo(1));
                Assert.That(history.Head.Commit.IsMerge,                       Is.True);
                Assert.That(history.Head.RoamingNetwork.DataSource,            Is.EqualTo("v13"));
                Assert.That(history.Head.RoamingNetwork.Name[org.GraphDefined.Vanaheimr.Illias.Languages.en],  Is.EqualTo("Network"));
            }

        }

        #endregion

        #region Compacting_drops_older_commits()

        [Test]
        public void Compacting_drops_older_commits()
        {

            var store     = new RoamingNetworkHistoryMemoryStore();
            var history   = RoamingNetworkHistory.Start(PersistenceTests.NewNetwork(),
                                                        Store:      store,
                                                        Retention:  new RoamingNetworkHistoryRetention { RetainCommits = 5 });

            var root      = history.Head.RoamingNetwork;
            var commits   = Linear(history, 20);

            Assert.That(history.Compact(out var error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(history.Root.Id,                                 Is.EqualTo(commits[14].Id));
                Assert.That(history.Root.Revision,                           Is.EqualTo(15));
                Assert.That(history.Count,                                   Is.EqualTo(6));
                Assert.That(store.Load()!.Entries,                           Has.Count.EqualTo(5));

                // Loaded again, from the new root.
                Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError),  Is.True, loadError);
                Assert.That(loaded!.Head.Commit.Id,                          Is.EqualTo(history.Head.Commit.Id));
                Assert.That(loaded.Root.Id,                                  Is.EqualTo(commits[14].Id));

                // A server holding only older commits starts from a snapshot; an old change set fits no version.
                Assert.That(history.TryGetMissingCommits([ commits[3].Id ], out _, out var missingError),  Is.False);
                Assert.That(missingError,                                    Does.Contain("start from a new snapshot"));
                Assert.That(PersistenceTests.Change(history, 30, "name", "Late", root).ErrorResponse,     Does.Contain("fits no version"));

            }

        }

        #endregion

        #region Compacting_keeps_the_branches()

        [Test]
        public void Compacting_keeps_the_branches()
        {

            var history   = RoamingNetworkHistory.Start(PersistenceTests.NewNetwork(),
                                                        Retention: new RoamingNetworkHistoryRetention { RetainCommits = 1 });

            var commits   = Linear(history, 2);

            // Another server, on revision 2.
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(history.ToSnapshotJSON(), out var other, out var snapshotError), Is.True, snapshotError);

            Linear(history, 8, From: 3);

            // A branch from revision 2 of the other server, conflicting with the data source of the other tip: both stay.
            var branch    = PersistenceTests.Change(other!, 20, "dataSource", "other");
            Assert.That(branch.IsCommitted);
            Assert.That(history.TryAppend(branch.Commit!, out var appendError), Is.True, appendError);
            Assert.That(history.Tips, Has.Count.EqualTo(2));

            Assert.That(history.Compact(out var error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.Root.Id,          Is.EqualTo(commits[1].Id), "The latest commit both tips follow.");
                Assert.That(history.Tips,             Has.Count.EqualTo(2));
                Assert.That(history.Conflicts,        Has.Length.EqualTo(1));
                Assert.That(history.Count,            Is.EqualTo(10));
            }

        }

        #endregion

        #region The_status_is_folded_into_the_base()

        [Test]
        public void The_status_is_folded_into_the_base()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = RoamingNetworkHistory.Start(PersistenceTests.NewNetwork(), Store: store);

            for (var i = 0; i < 50; i++)
                history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", i % 2 == 0 ? "offline" : "available", T0.AddMinutes(i), out _, out _);

            var logged = store.Count;

            Assert.That(history.Compact(out var error), Is.True, error);
            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError), Is.True, loadError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logged,                    Is.EqualTo(50));
                Assert.That(store.Count,               Is.Zero);
                Assert.That(loaded!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status),  Is.True);
                Assert.That(status!.Value,             Is.EqualTo("available"));
                Assert.That(status.Timestamp,          Is.EqualTo(T0.AddMinutes(49)));
            }

        }

        #endregion

        #region A_long_log_is_compacted_by_itself()

        [Test]
        public async Task A_long_log_is_compacted_by_itself()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = RoamingNetworkHistory.Start(PersistenceTests.NewNetwork(),
                                                       Store:      store,
                                                       Retention:  new RoamingNetworkHistoryRetention { CompactAfterLogEntries = 10 });

            for (var i = 0; i < 15; i++)
                history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", i % 2 == 0 ? "offline" : "available", T0.AddMinutes(i), out _, out _);

            for (var i = 0; i < 200 && store.Count >= 10; i++)
                await Task.Delay(25);

            Assert.That(store.Count, Is.LessThan(10));

        }

        #endregion

    }

}
