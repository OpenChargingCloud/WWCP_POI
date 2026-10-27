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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// Change sets made for another version than the head: a server takes no conflict from them, and none made for a
    /// version too old (SPECIFICATION.md, section 5.3). Conflicting tips come only from the commits of other servers,
    /// and a pair of them is merged once, not again with every commit.
    /// </summary>
    [TestFixture]
    public class ForkTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-13T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  EXA      = ChargingStationOperator_Id.Parse("DE*EXA");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                  DataSource:                "v1",
                                  Created:                   Created,
                                  ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                           Created:        Created,
                                                                                           ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                              Created:           Created,
                                                                                                                              ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                       Created:  Created,
                                                                                                                                                                       EVSEs:    [ new EVSE(E1, MaxPower: Watt.FromW(22000), Created: Created) ]) ]) ]),
                                                               new ChargingStationOperator(EXA, Created: Created) ]).WithKeys();

        #endregion

        #region Another(History)

        /// <summary>
        /// Another server, starting from a snapshot of the head of the given history.
        /// </summary>
        private static RoamingNetworkHistory Another(RoamingNetworkHistory History)
        {

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(History.ToSnapshotJSON(), out var other, out var error), Is.True, error);

            return other!;

        }

        #endregion

        #region (static) EVSE(Network) / Telephone(Network)

        private static EVSE EVSE(RoamingNetwork Network)
            => Network.GetChargingStationOperatorById(GEF)!.EVSEs.Single();

        private static String? Telephone(RoamingNetwork Network)
            => Network.GetChargingStationOperatorById(EXA)!.Telephone?.ToString();

        #endregion

        #region MaxPower(Version, Minutes, Watts) / ExaTelephone(Version, Minutes, Telephone) / DataSource(Version, Minutes, Value)

        /// <summary>
        /// GEF sets the maximum power of its EVSE, in a change set made for the given version.
        /// </summary>
        private static RoamingNetworkChangeSet MaxPower(RoamingNetwork  Version,
                                                        Int32           Minutes,
                                                        Int32           Watts)

            => Version.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(EVSE(Version), "maxPower", null, Watts) ], CreatedAt: T0.AddMinutes(Minutes));

        /// <summary>
        /// EXA sets its telephone, in a change set made for the given version.
        /// </summary>
        private static RoamingNetworkChangeSet ExaTelephone(RoamingNetwork  Version,
                                                            Int32           Minutes,
                                                            String          Telephone)

            => Version.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(Version.GetChargingStationOperatorById(EXA)!, "telephone", null, Telephone) ], CreatedAt: T0.AddMinutes(Minutes));

        /// <summary>
        /// The roaming network operator sets the data source, in a change set made for the given version.
        /// </summary>
        private static RoamingNetworkChangeSet DataSource(RoamingNetwork  Version,
                                                          Int32           Minutes,
                                                          String          Value)

            => Version.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(Version, "dataSource", null, Value) ], CreatedAt: T0.AddMinutes(Minutes));

        #endregion

        #region Committed(History, ChangeSet)

        private static RoamingNetworkCommit Committed(RoamingNetworkHistory    History,
                                                     RoamingNetworkChangeSet  ChangeSet)
        {

            var result = History.Commit(ChangeSet);

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            return result.Commit!;

        }

        #endregion

        #region Append(To, Commits)

        private static void Append(RoamingNetworkHistory               To,
                                   params RoamingNetworkCommit[]       Commits)
        {
            foreach (var commit in Commits)
                Assert.That(To.TryAppend(commit, out var error), Is.True, error);
        }

        #endregion


        #region A_conflicting_fork_does_not_roll_back_the_head()

        /// <summary>
        /// GEF sets its EVSE to 11000 W, EXA and the roaming network operator change their data after it. GEF then forks
        /// from the version before all of them, with 7400 W, and extends its branch until it would be longer than the
        /// main line: the highest revision, the head. Its first change set conflicts with the head and is refused, so the
        /// others, made for the commits of its branch, fit no version: the head stays, with the commits of the others.
        /// </summary>
        [Test]
        public void A_conflicting_fork_does_not_roll_back_the_head()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var v0       = history.Head.RoamingNetwork;

            Committed(history, MaxPower(v0, 1, 11000));
            Committed(history, ExaTelephone(history.Head.RoamingNetwork, 2, "+49 30 1"));
            Committed(history, DataSource  (history.Head.RoamingNetwork, 3, "v2"));

            var head     = history.Head;
            var count    = history.Count;

            // The branch of GEF, prepared on its own: each change set made for the version the one before leads to.
            var branch   = new List<RoamingNetworkChangeSet>();
            var version  = v0;

            for (var i = 0; i < 6; i++)
            {
                var changeSet = MaxPower(version, 10 + i, 7400 - i);
                branch.Add(changeSet);
                version = version.ApplyChangeSet(changeSet);
            }

            var results  = branch.Select(changeSet => history.Commit(changeSet)).ToArray();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(results[0].Status,                                  Is.EqualTo(RoamingNetworkCommitStatus.Conflict));
                Assert.That(results[0].Commit,                                  Is.Null);
                Assert.That(results[0].Conflicts!.Single().Kind,                Is.EqualTo(RoamingNetworkMergeConflictKind.DifferentValues));
                Assert.That(results[0].Conflicts!.Single().EntityId,            Is.EqualTo(E1.ToString()));
                Assert.That(results[0].Conflicts!.Single().PropertyName,        Is.EqualTo("maxPower"));
                Assert.That(results[0].ErrorResponse,                           Does.Contain($"conflicts with the head '{head.Commit.Id}'"));

                foreach (var result in results.Skip(1))
                {
                    Assert.That(result.Status,                                  Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                    Assert.That(result.ErrorResponse,                           Does.Contain("fits no version"));
                }

                // Nothing was stored: the head, its commits and the data of the others stay.
                Assert.That(history.Head.Commit.Id,                             Is.EqualTo(head.Commit.Id));
                Assert.That(history.Tips,                                       Has.Count.EqualTo(1));
                Assert.That(history.Conflicts,                                  Is.Empty);
                Assert.That(history.Count,                                      Is.EqualTo(count));
                Assert.That(EVSE(history.Head.RoamingNetwork).MaxPower?.Value,  Is.EqualTo(11000m));
                Assert.That(Telephone(history.Head.RoamingNetwork),             Is.EqualTo("+49 30 1"));
                Assert.That(history.Head.RoamingNetwork.DataSource,             Is.EqualTo("v2"));

            }

            // GEF prepares its change again for the head: taken.
            var again = history.Commit(MaxPower(history.Head.RoamingNetwork, 20, 7400));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(again.Status,                                       Is.EqualTo(RoamingNetworkCommitStatus.Committed), again.ErrorResponse);
                Assert.That(EVSE(history.Head.RoamingNetwork).MaxPower?.Value,  Is.EqualTo(7400m));
                Assert.That(Telephone(history.Head.RoamingNetwork),             Is.EqualTo("+49 30 1"));
            }

        }

        #endregion

        #region A_fork_not_conflicting_is_merged_alike_everywhere()

        /// <summary>
        /// A change set made for an older version that does not conflict with the head is committed and merged, as
        /// before; another server appending both makes the same merge commit.
        /// </summary>
        [Test]
        public void A_fork_not_conflicting_is_merged_alike_everywhere()
        {

            var server1  = RoamingNetworkHistory.Start(NewNetwork());
            var server2  = Another(server1);
            var v0       = server1.Head.RoamingNetwork;

            var a        = Committed(server1, MaxPower(v0, 1, 11000));
            var b        = Committed(server1, DataSource(server1.Head.RoamingNetwork, 2, "v2"));

            // Made for the version before both, by EXA: merged with the head once, to see that it does not conflict,
            // and that merge is the merge commit.
            var merges   = server1.MergeCalculations;
            var c        = Committed(server1, ExaTelephone(v0, 3, "+49 30 1"));
            var merged   = server1.MergeCalculations - merges;

            Append(server2, a, b, c);

            // The merge commit, appended from server 1 as well, is checked to be the one server 2 calculated.
            Assert.That(server1.TryGetMissingCommits(server2.HaveIds(), out var missing, out var missingError), Is.True, missingError);
            Assert.That(server2.TryAppend(missing!, out _, out var appendError),                                Is.True, appendError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(c.ParentIds,                                        Is.EqualTo(new[] { server1.Root.Id }));
                Assert.That(merged,                                             Is.EqualTo(1));
                Assert.That(server1.Tips,                                       Has.Count.EqualTo(1));
                Assert.That(server1.Head.Commit.IsMerge,                        Is.True);
                Assert.That(server1.Head.Commit.ParentIds,                      Is.EqualTo(new[] { b.Id, c.Id }));
                Assert.That(server2.Head.Commit.Id,                             Is.EqualTo(server1.Head.Commit.Id));
                Assert.That(server2.Head.RoamingNetwork.ETags,                  Is.EqualTo(server1.Head.RoamingNetwork.ETags));
                Assert.That(EVSE(server1.Head.RoamingNetwork).MaxPower?.Value,  Is.EqualTo(11000m));
                Assert.That(Telephone(server1.Head.RoamingNetwork),             Is.EqualTo("+49 30 1"));
                Assert.That(server1.Head.RoamingNetwork.DataSource,             Is.EqualTo("v2"));
            }

        }

        #endregion

        #region A_change_set_on_a_side_tip_is_taken_when_it_resolves_the_conflict()

        /// <summary>
        /// GEF sets its EVSE to 11000 W on server 1 and to 7400 W on server 2; server 1 appends the commit of server 2:
        /// the tips conflict, as before. A change set on the side tip keeping the conflict is refused, one resolving it
        /// is taken, and the tips are merged.
        /// </summary>
        [Test]
        public void A_change_set_on_a_side_tip_is_taken_when_it_resolves_the_conflict()
        {

            var server1  = RoamingNetworkHistory.Start(NewNetwork());
            var server2  = Another(server1);

            Committed(server1, MaxPower(server1.Head.RoamingNetwork, 1, 11000));
            var y        = Committed(server2, MaxPower(server2.Head.RoamingNetwork, 2, 7400));

            Append(server1, y);

            Assert.That(server1.Tips,       Has.Count.EqualTo(2));
            Assert.That(server1.Conflicts,  Has.Length.EqualTo(1));

            var head     = server1.Head.Commit;
            var side     = server1.Tips.Single(tip => tip.Commit.Id != head.Id);
            var other    = server1.Tips.Single(tip => tip.Commit.Id == head.Id).RoamingNetwork;

            // EXA changes its telephone on the side tip: the conflict stays, refused.
            var keeping  = server1.Commit(ExaTelephone(side.RoamingNetwork, 3, "+49 30 1"));

            // GEF takes the value of the head on the side tip: resolved, taken and merged.
            var resolved = server1.Commit(MaxPower(side.RoamingNetwork, 4, (Int32) EVSE(other).MaxPower!.Value.Value));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(keeping.Status,                                     Is.EqualTo(RoamingNetworkCommitStatus.Conflict));
                Assert.That(keeping.Conflicts!.Single().PropertyName,           Is.EqualTo("maxPower"));

                Assert.That(resolved.Status,                                    Is.EqualTo(RoamingNetworkCommitStatus.Committed), resolved.ErrorResponse);
                Assert.That(resolved.Commit!.ParentIds,                         Is.EqualTo(new[] { side.Commit.Id }));
                Assert.That(server1.Tips,                                       Has.Count.EqualTo(1));
                Assert.That(server1.Conflicts,                                  Is.Empty);
                Assert.That(server1.Head.Commit.IsMerge,                        Is.True);
                Assert.That(EVSE(server1.Head.RoamingNetwork).MaxPower?.Value,  Is.EqualTo(EVSE(other).MaxPower!.Value.Value));

            }

        }

        #endregion

        #region A_fork_older_than_the_limit_is_refused()

        /// <summary>
        /// A change set made for a version more revisions behind the head than the history takes is refused, whether the
        /// version is kept or would be calculated again; one at the limit is taken.
        /// </summary>
        [Test]
        public void A_fork_older_than_the_limit_is_refused()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork(),
                                                       Retention: new RoamingNetworkHistoryRetention {
                                                                      VersionCheckpointInterval  = 100,
                                                                      RecentVersions             = 1
                                                                  });

            history.MaxRevisionsBehindHead = 3;

            var versions = new List<RoamingNetwork> { history.Head.RoamingNetwork };

            for (var i = 1; i <= 5; i++)
            {
                Committed(history, DataSource(history.Head.RoamingNetwork, i, "v" + (i + 1)));
                versions.Add(history.Head.RoamingNetwork);
            }

            // Not conflicting with the head: refused only for being too old.
            var root     = history.Commit(ExaTelephone(versions[0], 10, "+49 30 0"));   // the root, kept
            var first    = history.Commit(ExaTelephone(versions[1], 11, "+49 30 1"));   // not kept, not even calculated again
            var second   = history.Commit(ExaTelephone(versions[2], 12, "+49 30 2"));   // 3 revisions behind: at the limit

            using (Assert.EnterMultipleScope())
            {

                Assert.That(root.  Status,                          Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(root.  ErrorResponse,                   Does.Contain("of the revision 0, more than 3 revisions behind the head of the revision 5").And.Contain("prepare it again for the head!"));
                Assert.That(first. Status,                          Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(first. ErrorResponse,                   Does.Contain("of the revision 1, more than 3 revisions behind the head"));

                Assert.That(second.Status,                          Is.EqualTo(RoamingNetworkCommitStatus.Committed), second.ErrorResponse);
                Assert.That(history.Tips,                           Has.Count.EqualTo(1));
                Assert.That(Telephone(history.Head.RoamingNetwork), Is.EqualTo("+49 30 2"));

            }

        }

        #endregion

        #region A_fork_older_than_the_latest_checkpoint_is_refused()

        /// <summary>
        /// A change set made for a version the commit of the latest checkpoint does not lead to is refused: no branch
        /// starts before it, where compaction may drop the commits; one made for the commit of the checkpoint is taken.
        /// </summary>
        [Test]
        public void A_fork_older_than_the_latest_checkpoint_is_refused()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var versions = new List<RoamingNetwork> { history.Head.RoamingNetwork };

            for (var i = 1; i <= 3; i++)
            {
                Committed(history, DataSource(history.Head.RoamingNetwork, i, "v" + (i + 1)));
                versions.Add(history.Head.RoamingNetwork);
            }

            Assert.That(history.TryMakeCheckpoint(out var checkpoint, out var checkpointError), Is.True, checkpointError);

            Committed(history, DataSource(history.Head.RoamingNetwork, 4, "v5"));

            var before   = history.Commit(ExaTelephone(versions[2], 10, "+49 30 2"));
            var at       = history.Commit(ExaTelephone(versions[3], 11, "+49 30 3"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(checkpoint!.Revision,                   Is.EqualTo(3));

                Assert.That(before.Status,                          Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(before.ErrorResponse,                   Does.Contain($"does not follow the commit '{checkpoint.CommitId}' of the latest checkpoint"));

                Assert.That(at.Status,                              Is.EqualTo(RoamingNetworkCommitStatus.Committed), at.ErrorResponse);
                Assert.That(history.Tips,                           Has.Count.EqualTo(1));
                Assert.That(Telephone(history.Head.RoamingNetwork), Is.EqualTo("+49 30 3"));

            }

        }

        #endregion

        #region Commits_of_other_servers_are_appended_whatever_they_follow()

        /// <summary>
        /// The bounds are those of change sets: a commit of another server is appended also when it conflicts with the
        /// head, and when it follows a version a change set may no longer be made for, as the other server took it by
        /// its own head. Their tips conflict, as before.
        /// </summary>
        [Test]
        public void Commits_of_other_servers_are_appended_whatever_they_follow()
        {

            var server1  = RoamingNetworkHistory.Start(NewNetwork());
            var server2  = Another(server1);

            server1.MaxRevisionsBehindHead = 2;

            for (var i = 1; i <= 4; i++)
                Committed(server1, DataSource(server1.Head.RoamingNetwork, i, "v" + (i + 1)));

            var y = Committed(server2, DataSource(server2.Head.RoamingNetwork, 10, "other"));

            Append(server1, y);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server1.Tips,                           Has.Count.EqualTo(2));
                Assert.That(server1.Head.Commit.Revision,           Is.EqualTo(4));
                Assert.That(server1.Conflicts.Single().Kind,        Is.EqualTo(RoamingNetworkMergeConflictKind.DifferentValues));
                Assert.That(server1.Conflicts.Single().PropertyName,  Is.EqualTo("dataSource"));
            }

        }

        #endregion

        #region Conflicting_tips_are_not_merged_again_with_every_commit()

        /// <summary>
        /// Six servers set the EVSE to six different values, and one server appends all of their commits: six tips, each
        /// pair conflicting, 15 merges. A commit on the head merges only the new head with the five other tips, not every
        /// pair again; the conflicts are those a server calculating every pair anew finds, in the same order.
        /// </summary>
        [Test]
        public void Conflicting_tips_are_not_merged_again_with_every_commit()
        {

            var server   = RoamingNetworkHistory.Start(NewNetwork());
            var fresh    = Another(server);
            var others   = Enumerable.Range(1, 6).Select(_ => Another(server)).ToArray();

            var commits  = others.Select((other, i) => Committed(other, MaxPower(other.Head.RoamingNetwork, i + 1, 1000 * (i + 1)))).ToArray();

            Append(server, commits);

            var merged   = server.MergeCalculations;

            // The new head, changing EXA, conflicts with the five other tips still; their pairs are not merged again.
            Committed(server, ExaTelephone(server.Head.RoamingNetwork, 10, "+49 30 1"));

            var afterCommit = server.MergeCalculations;

            // Another server takes the same commits at once, and merges every pair anew.
            Assert.That(server.TryGetMissingCommits(fresh.HaveIds(), out var all, out var missingError), Is.True, missingError);
            Assert.That(fresh.TryAppend(all!, out _, out var appendError), Is.True, appendError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(merged,                                  Is.EqualTo(15));
                Assert.That(afterCommit - merged,                    Is.EqualTo(5));
                Assert.That(server.Tips,                             Has.Count.EqualTo(6));
                Assert.That(server.Conflicts,                        Has.Length.EqualTo(15));

                Assert.That(fresh.Tips.Select(tip => tip.Commit.Id), Is.EqualTo(server.Tips.Select(tip => tip.Commit.Id)));
                Assert.That(fresh.Conflicts.Select(conflict => conflict.ToJSON().ToString()),
                            Is.EqualTo(server.Conflicts.Select(conflict => conflict.ToJSON().ToString())));

            }

        }

        #endregion

    }

}
