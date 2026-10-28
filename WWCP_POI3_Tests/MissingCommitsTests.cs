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
    /// The commits missing at who holds some (TryGetMissingCommits, TryGetCommitsSince): exactly those no given one is or
    /// follows, parents first, a page the start of the next larger one; and a page costs about the commits it returns,
    /// not the whole history (POI3-ISSUES.md S12).
    /// </summary>
    [TestFixture]
    public class MissingCommitsTests
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
                                  DataSource:                "v0",
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

        private static RoamingNetworkHistory Another(RoamingNetworkHistory History)
        {

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(History.ToSnapshotJSON(), out var other, out var error), Is.True, error);

            return other!;

        }

        #endregion

        #region DataSource(Version, Minutes, Value) / ExaTelephone(Version, Minutes, Telephone) / MaxPower(Version, Minutes, Watts)

        private static RoamingNetworkChangeSet DataSource(RoamingNetwork Version, Int32 Minutes, String Value)

            => Version.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(Version, "dataSource", null, Value) ], CreatedAt: T0.AddMinutes(Minutes));

        private static RoamingNetworkChangeSet ExaTelephone(RoamingNetwork Version, Int32 Minutes, String Telephone)

            => Version.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(Version.GetChargingStationOperatorById(EXA)!, "telephone", null, Telephone) ], CreatedAt: T0.AddMinutes(Minutes));

        private static RoamingNetworkChangeSet MaxPower(RoamingNetwork Version, Int32 Minutes, Int32 Watts)

            => Version.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(Version.GetChargingStationOperatorById(GEF)!.EVSEs.Single(), "maxPower", null, Watts) ], CreatedAt: T0.AddMinutes(Minutes));

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

        #region (static) Branchy(Seed)

        /// <summary>
        /// A history of a main line, branches of other servers merged into it later and tips conflicting with it,
        /// at random by the given seed; and the other servers.
        /// </summary>
        private static (RoamingNetworkHistory Server, List<RoamingNetworkHistory> Others) Branchy(Int32 Seed)
        {

            var random   = new Random(Seed);
            var server   = RoamingNetworkHistory.Start(NewNetwork());
            var others   = new List<RoamingNetworkHistory>();
            var pending  = new List<List<RoamingNetworkCommit>>();
            var minute   = 0;

            for (var step = 0; step < 40; step++)
            {

                switch (random.Next(10))
                {

                    // A branch of another server: telephones merge unless two branches set different ones,
                    // maximum powers of two branches conflict, and stay tips of their own.
                    case 6 or 7 or 8:
                        var other   = Another(server);
                        var commits = new List<RoamingNetworkCommit>();
                        for (var i = random.Next(1, 4); i > 0; i--)
                            commits.Add(Committed(other, random.Next(2) == 0
                                                             ? ExaTelephone(other.Head.RoamingNetwork, ++minute, "+49 30 " + random.Next(3))
                                                             : MaxPower    (other.Head.RoamingNetwork, ++minute, 1000 * random.Next(1, 4))));
                        others. Add(other);
                        pending.Add(commits);
                        break;

                    // The commits of a branch taken, while the main line went on meanwhile.
                    case 9 when pending.Count > 0:
                        var taken = pending[random.Next(pending.Count)];
                        pending.Remove(taken);
                        Assert.That(server.TryAppend(taken, out _, out var appendError), Is.True, appendError);
                        break;

                    default:
                        Committed(server, DataSource(server.Head.RoamingNetwork, ++minute, "v" + step));
                        break;

                }

            }

            foreach (var taken in pending)
                Assert.That(server.TryAppend(taken, out _, out var appendError), Is.True, appendError);

            return (server, others);

        }

        #endregion

        #region (static) AllCommits(History) / Followed(History, Have)

        /// <summary>
        /// All commits of the given history, reached from its tips.
        /// </summary>
        private static Dictionary<RoamingNetworkCommit_Id, RoamingNetworkCommit> AllCommits(RoamingNetworkHistory History)

            => Followed(History, History.Tips.Select(tip => tip.Commit.Id));

        /// <summary>
        /// The given commits the given history holds, and their ancestors there.
        /// </summary>
        private static Dictionary<RoamingNetworkCommit_Id, RoamingNetworkCommit> Followed(RoamingNetworkHistory                 History,
                                                                                         IEnumerable<RoamingNetworkCommit_Id>  Have)
        {

            var followed  = new Dictionary<RoamingNetworkCommit_Id, RoamingNetworkCommit>();
            var pending   = new Stack<RoamingNetworkCommit_Id>(Have);

            while (pending.TryPop(out var commitId))
                if (!followed.ContainsKey(commitId) && History.TryGetCommit(commitId, out var commit))
                {
                    followed[commitId] = commit;
                    foreach (var parentId in commit.ParentIds)
                        pending.Push(parentId);
                }

            return followed;

        }

        #endregion

        #region (static) AssertMissing(History, Have)

        /// <summary>
        /// The commits missing at who holds the given ones are those no given one is or follows, each once, parents
        /// first; a page of them is the start of all of them.
        /// </summary>
        private static void AssertMissing(RoamingNetworkHistory                History,
                                          IReadOnlyList<RoamingNetworkCommit_Id>  Have)
        {

            var all       = AllCommits(History);
            var followed  = Followed(History, Have);

            Assert.That(History.TryGetMissingCommits(Have, out var missing, out var error, Int32.MaxValue), Is.True, error);

            var ids       = missing!.Select(commit => commit.Id).ToList();
            var position  = ids.Select((commitId, index) => (commitId, index)).ToDictionary(pair => pair.commitId, pair => pair.index);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(ids,  Is.EquivalentTo(all.Keys.Where(commitId => !followed.ContainsKey(commitId))), $"have: {String.Join(", ", Have)}");
                Assert.That(ids,  Is.Unique);

                foreach (var commit in missing!)
                    foreach (var parentId in commit.ParentIds)
                        if (position.TryGetValue(parentId, out var parentPosition))
                            Assert.That(parentPosition, Is.LessThan(position[commit.Id]), "A child before its parent.");

                foreach (var max in new[] { 0, 1, 3 })
                {
                    Assert.That(History.TryGetMissingCommits(Have, out var page, out var pageError, max), Is.True, pageError);
                    Assert.That(page!.Select(commit => commit.Id), Is.EqualTo(ids.Take(max)));
                }

            }

        }

        #endregion


        #region The_missing_commits_are_those_no_given_one_follows()

        /// <summary>
        /// On histories with branches merged later and tips conflicting with the head: from every single commit, every
        /// pair, the samples other servers name of theirs, and from all tips, the commits missing are exactly those none
        /// of them is or follows, parents first.
        /// </summary>
        [Test]
        public void The_missing_commits_are_those_no_given_one_follows()
        {

            foreach (var seed in new[] { 1, 2, 3, 4, 5 })
            {

                var (server, others) = Branchy(seed);
                var all              = AllCommits(server).Keys.ToList();
                var random           = new Random(seed);

                Assert.That(server.Tips.Count + server.MergeCalculations, Is.GreaterThan(1), "No branch at all.");

                foreach (var commitId in all)
                    AssertMissing(server, [ commitId ]);

                for (var i = 0; i < 50; i++)
                    AssertMissing(server, [ all[random.Next(all.Count)], all[random.Next(all.Count)] ]);

                foreach (var other in others)
                    AssertMissing(server, other.HaveIds());

                AssertMissing(server, [ .. server.Tips.Select(tip => tip.Commit.Id) ]);

            }

        }

        #endregion

        #region The_missing_commits_are_found_after_compacting()

        /// <summary>
        /// Compacting builds the commits anew, without the ones dropped: the commits missing are found as before.
        /// </summary>
        [Test]
        public void The_missing_commits_are_found_after_compacting()
        {

            var server = RoamingNetworkHistory.Start(NewNetwork(),
                                                     Store:      new RoamingNetworkHistoryMemoryStore(),
                                                     Retention:  new RoamingNetworkHistoryRetention { RetainCommits = 5 });

            for (var i = 1; i <= 12; i++)
                Committed(server, DataSource(server.Head.RoamingNetwork, i, "v" + i));

            var other = Another(server);
            var side  = Committed(other, ExaTelephone(other.Head.RoamingNetwork, 20, "+49 30 1"));

            Committed(server, DataSource(server.Head.RoamingNetwork, 21, "v21"));
            Assert.That(server.TryAppend(side, out var appendError), Is.True, appendError);
            Committed(server, DataSource(server.Head.RoamingNetwork, 22, "v22"));

            Assert.That(server.Compact(out var compactError), Is.True, compactError);

            var all = AllCommits(server).Keys.ToList();

            Assert.That(all, Has.Count.LessThan(16), "Nothing was dropped.");

            foreach (var commitId in all)
                AssertMissing(server, [ commitId ]);

            Assert.That(server.TryGetCommitsSince(server.Root.Id, out var sinceRoot, out var error, Int32.MaxValue), Is.True, error);
            Assert.That(sinceRoot!.Select(commit => commit.Id), Is.EquivalentTo(all.Where(commitId => commitId != server.Root.Id)));

        }

        #endregion

        #region A_page_costs_what_it_returns()

        /// <summary>
        /// A server catching up from the root asks for the commits page by page, each since the last one of the page
        /// before: every page looks at about the commits it returns, not at all commits after it again.
        /// </summary>
        [Test]
        public void A_page_costs_what_it_returns()
        {

            var server = RoamingNetworkHistory.Start(NewNetwork());

            for (var i = 1; i <= 300; i++)
                Committed(server, DataSource(server.Head.RoamingNetwork, i, "v" + i));

            var received  = new List<RoamingNetworkCommit>();
            var since     = server.Root.Id;
            var before    = server.CommitsLookedAt;
            var pages     = 0;

            while (server.TryGetCommitsSince(since, out var page, out var error, 20) && page.Count > 0)
            {
                received.AddRange(page);
                since = page[^1].Id;
                pages++;
            }

            var lookedAt  = server.CommitsLookedAt - before;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(received.Select(commit => commit.Revision),  Is.EqualTo(Enumerable.Range(1, 300).Select(revision => (UInt64) revision)));
                Assert.That(pages,                                        Is.EqualTo(15));
                Assert.That(lookedAt,                                     Is.LessThanOrEqualTo(300 + pages + 1), "Every page walked the history again.");
            }

            // From the root alone as held: the first page looks at its commits.
            before = server.CommitsLookedAt;

            Assert.That(server.TryGetMissingCommits([ server.Root.Id ], out var first, out var missingError, 20), Is.True, missingError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first!.Select(commit => commit.Revision),     Is.EqualTo(Enumerable.Range(1, 20).Select(revision => (UInt64) revision)));
                Assert.That(server.CommitsLookedAt - before,              Is.LessThanOrEqualTo(21));
            }

        }

        #endregion

    }

}
