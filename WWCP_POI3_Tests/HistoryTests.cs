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

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// Commits and histories with one leading server (model a).
    /// </summary>
    [TestFixture]
    public class HistoryTests
    {

        #region Data

        private static readonly Ed25519PrivateKeyParameters Alice = new (Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(
                   Id:                        RoamingNetwork_Id.Parse("rn1"),
                   Name:                      I18NString.Create(Languages.en, "Roaming Network 1"),
                   Description:               I18NString.Create(Languages.en, "My first roaming network!"),
                   DataSource:                "test",
                   Created:                   DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                   ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                            I18NString.Create(Languages.en, "GraphDefined"),
                                                                            Created: DateTimeOffset.Parse("2026-01-01T00:00:00Z")) ]
               ).WithKeys();

        private static RoamingNetworkChangeSet DataSource(RoamingNetworkHistory History, String Value, Int32 Minute = 0)

            => History.Head.RoamingNetwork.CreateSignedChangeSet(
                   [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                   Description:  I18NString.Create(Languages.en, "Data source " + Value),
                   CreatedAt:    DateTimeOffset.Parse("2026-10-11T12:00:00Z").AddMinutes(Minute)
               );

        #endregion


        #region A_follower_catches_up_with_the_leader()

        [Test]
        public void A_follower_catches_up_with_the_leader()
        {

            var leader    = RoamingNetworkHistory.Start(NewNetwork());

            Assert.That(leader.Commit(DataSource(leader, "A", 1)).Status,  Is.EqualTo(RoamingNetworkCommitStatus.Committed));

            // The follower starts from a snapshot of the leader's head, as JSON text...
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON().ToString(), out var follower, out var error1),  Is.True, error1);

            Assert.That(leader.Commit(DataSource(leader, "B", 2)).Status,  Is.EqualTo(RoamingNetworkCommitStatus.Committed));
            Assert.That(leader.Commit(DataSource(leader, "C", 3)).Status,  Is.EqualTo(RoamingNetworkCommitStatus.Committed));

            // ...and fetches the commits since its head, also as JSON text.
            Assert.That(leader.TryGetCommitsSince(follower!.Head.Commit.Id, out var commits, out var error2),  Is.True, error2);

            var received = commits!.Select(commit => RoamingNetworkCommit.Parse(commit.ToJSON().ToString())).ToArray();

            Assert.That(follower.TryAppend(received, out var appended, out var error3),  Is.True, error3);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(appended,                                      Is.EqualTo(2));
                Assert.That(follower.Head.Commit.Id,                       Is.EqualTo(leader.Head.Commit.Id));
                Assert.That(follower.Head.Commit.Revision,                 Is.EqualTo(3));
                Assert.That(follower.Head.RoamingNetwork.Revision,         Is.EqualTo(3));
                Assert.That(follower.Head.RoamingNetwork.DataSource,       Is.EqualTo("C"));
                Assert.That(follower.Head.RoamingNetwork.CalcETags(),      Is.EqualTo(leader.Head.RoamingNetwork.CalcETags()));
                Assert.That(follower.Root.Id,                              Is.EqualTo(received[0].ParentId));
                Assert.That(follower.Count,                                Is.EqualTo(3));

                // Appending what it holds already changes nothing.
                Assert.That(follower.TryAppend(received, out _, out var error4),  Is.True, error4);
                Assert.That(follower.Count,                                       Is.EqualTo(3));

            }

        }

        #endregion

        #region Commit_infos_list_the_history_without_changes()

        [Test]
        public void Commit_infos_list_the_history_without_changes()
        {

            var leader  = RoamingNetworkHistory.Start(NewNetwork());
            var root    = leader.Head.Commit;

            for (var i = 1; i <= 5; i++)
                leader.Commit(DataSource(leader, "v" + i, i));

            Assert.That(leader.TryGetCommitInfosSince(root.Id, out var infos,     out var error1),              Is.True, error1);
            Assert.That(leader.TryGetCommitInfosSince(root.Id, out var firstTwo,  out var error2, MaxCount: 2), Is.True, error2);
            Assert.That(leader.TryGetCommitInfosSince(firstTwo![1].Id, out var rest, out var error3),           Is.True, error3);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(infos!.Select(info => info.Revision),          Is.EqualTo(new UInt64[] { 1, 2, 3, 4, 5 }));
                Assert.That(infos![0].ParentId,                             Is.EqualTo(root.Id));
                Assert.That(infos![0].Description[Languages.en],            Is.EqualTo("Data source v1"));
                Assert.That(infos![0].NumberOfChanges,                      Is.EqualTo(1));
                Assert.That(leader.TryGetCommit(infos![0].Id, out var first),  Is.True);
                Assert.That(infos![0].SignatureKeyIds,                      Is.EqualTo(first!.ChangeSet!.Signatures.Select(signature => signature.KeyId)).And.Not.Empty);
                Assert.That(infos![4].Id,                                   Is.EqualTo(leader.Head.Commit.Id));

                // Metadata only.
                Assert.That(infos![0].ToJSON().ContainsKey("changeSet"),    Is.False);
                Assert.That(infos![0].ToJSON()["changeSetId"],              Is.Not.Null);

                // Paging: from the last one returned.
                Assert.That(rest!.Select(info => info.Revision),            Is.EqualTo(new UInt64[] { 3, 4, 5 }));

                Assert.That(leader.TryGetCommitInfosSince(RoamingNetworkCommit_Id.FromDigest(new Byte[32]), out _, out var error4),  Is.False);
                Assert.That(error4,                                                                                                Does.Contain("start from a new snapshot"));

            }

        }

        #endregion

        #region Committing_branches_and_merges()

        [Test]
        public void Committing_branches_and_merges()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var root     = history.Head;

            // Another server, starting with the same genesis commit.
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(history.ToSnapshotJSON(), out var server2, out var snapshotError), Is.True, snapshotError);

            // Three change sets made for the same version, e.g. on three servers.
            var csA      = DataSource(history, "A", 1);
            var csB      = history.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.RemoveProperty(history.Head.RoamingNetwork, "description") ],
                                                                             CreatedAt: DateTimeOffset.Parse("2026-10-11T12:02:00Z"));
            var csC      = DataSource(history, "C", 3);

            var a        = history.Commit(csA);
            var again    = history.Commit(csA);

            // B fits the root, not the head: a branch, merged with A, as they change different properties.
            var b        = history.Commit(csB);
            var mergeAB  = history.Head;

            // C changes the data source, as A did: made for the root, it would conflict with the head, and is refused here...
            var refused  = history.Commit(csC);

            // ...but the other server, whose head is the root, commits it: appended here, it stays a tip of its own.
            var c        = server2!.Commit(csC);

            Assert.That(history.TryAppend(c.Commit!, out var appendError), Is.True, appendError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(a.Status,                                  Is.EqualTo(RoamingNetworkCommitStatus.Committed));
                Assert.That(again.Status,                              Is.EqualTo(RoamingNetworkCommitStatus.AlreadyCommitted));
                Assert.That(again.Commit,                              Is.SameAs(a.Commit));

                Assert.That(b.Status,                                  Is.EqualTo(RoamingNetworkCommitStatus.Committed));
                Assert.That(b.Commit!.ParentIds,                       Is.EqualTo(new[] { root.Commit.Id }));
                Assert.That(mergeAB.Commit.IsMerge,                    Is.True);
                Assert.That(mergeAB.Commit.Revision,                   Is.EqualTo(2));
                Assert.That(mergeAB.Commit.ChangeSet!.Signatures,      Is.Empty);
                Assert.That(mergeAB.RoamingNetwork.DataSource,         Is.EqualTo("A"));
                Assert.That(mergeAB.RoamingNetwork.Description.Count,  Is.Zero);

                Assert.That(refused.Status,                            Is.EqualTo(RoamingNetworkCommitStatus.Conflict));
                Assert.That(refused.IsCommitted,                       Is.False);
                Assert.That(refused.Commit,                            Is.Null);
                Assert.That(refused.Head.Commit.Id,                    Is.EqualTo(mergeAB.Commit.Id));
                Assert.That(refused.Conflicts!.Single().Kind,          Is.EqualTo(RoamingNetworkMergeConflictKind.DifferentValues));
                Assert.That(refused.Conflicts!.Single().PropertyName,  Is.EqualTo("dataSource"));
                Assert.That(refused.ErrorResponse,                     Does.Contain("conflicts with the head").And.Contain("Prepare it again for the head!"));

                Assert.That(c.Status,                                  Is.EqualTo(RoamingNetworkCommitStatus.Committed));
                Assert.That(history.Head.Commit.Id,                    Is.EqualTo(mergeAB.Commit.Id));
                Assert.That(history.Tips.Select(tip => tip.Commit.Id), Is.EqualTo(new[] { mergeAB.Commit.Id, c.Commit!.Id }));
                Assert.That(history.Conflicts.Single().Kind,           Is.EqualTo(RoamingNetworkMergeConflictKind.DifferentValues));
                Assert.That(history.Conflicts.Single().PropertyName,   Is.EqualTo("dataSource"));
                Assert.That(history.Conflicts.Single().Left?.ToString(),   Is.EqualTo("A"));
                Assert.That(history.Conflicts.Single().Right?.ToString(),  Is.EqualTo("C"));

            }

            // The owner resolves the conflict: on the branch of C, it takes the value of the other one.
            var cVersion  = history.Tips[1].RoamingNetwork;
            var resolved  = history.Commit(cVersion.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(cVersion, "dataSource", null, "A") ],
                                                                          CreatedAt: DateTimeOffset.Parse("2026-10-11T12:04:00Z")));

            // A change set made for a version this history does not have.
            var unknown   = history.Commit(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"), DataSource: "x").WithKeys().
                                               CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(history.Head.RoamingNetwork, "dataSource", null, "y") ]));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(resolved.Status,                           Is.EqualTo(RoamingNetworkCommitStatus.Committed));
                Assert.That(history.Tips,                              Has.Count.EqualTo(1));
                Assert.That(history.Conflicts,                         Is.Empty);
                Assert.That(history.Head.Commit.IsMerge,               Is.True);
                Assert.That(history.Head.Commit.ParentIds,             Does.Contain(mergeAB.Commit.Id));
                Assert.That(history.Head.RoamingNetwork.DataSource,    Is.EqualTo("A"));

                Assert.That(unknown.Status,                            Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(unknown.ErrorResponse,                     Does.Contain("fits no version"));

            }

        }

        #endregion

        #region Commits_are_checked()

        [Test]
        public void Commits_are_checked()
        {

            var leader    = RoamingNetworkHistory.Start(NewNetwork());
            RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out _);

            var commit1   = leader.Commit(DataSource(leader, "A", 1)).Commit!;
            var commit2   = leader.Commit(DataSource(leader, "B", 2)).Commit!;

            // Another content under the same identifications, of the change set and of the commit.
            var forged    = commit1.ToJSON();
            forged["changeSet"]!["description"]!["en"] = "forged";

            // Another identification than the one of its content.
            var wrongId   = commit1.ToJSON();
            wrongId["@id"] = commit2.Id.ToString();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetworkCommit.TryParse(forged,  out _, out var error1),  Is.False);
                Assert.That(error1,                                                         Does.Contain("is not identified by its content"));

                Assert.That(RoamingNetworkCommit.TryParse(wrongId, out _, out var error2),  Is.False);
                Assert.That(error2,                                                         Does.Contain("is not the one of its content"));

                // A commit skipping its predecessor.
                Assert.That(follower!.TryAppend(commit2, out var error3),                   Is.False);
                Assert.That(error3,                                                         Does.Contain("is unknown here"));

                Assert.That(follower.TryAppend(commit1, out var error4),                    Is.True, error4);
                Assert.That(follower.TryAppend(commit2, out var error5),                    Is.True, error5);

                // A snapshot whose network does not have its own ETags.
                var snapshot = leader.ToSnapshotJSON();
                snapshot["roamingNetwork"]!["dataSource"] = "forged";
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot, out _, out var error6),  Is.False);
                Assert.That(error6,                                                                  Does.Contain("do not match its content"));

                // A snapshot whose network does not have the ETags of its commit.
                ((JObject) snapshot["roamingNetwork"]!).Remove("eTags");
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot, out _, out var error7),  Is.False);
                Assert.That(error7,                                                                  Does.Contain("does not have the ETags of the commit"));

            }

        }

        #endregion

        #region Signed_change_sets_are_verified_by_leader_and_follower()

        [Test]
        public void Signed_change_sets_are_verified_by_leader_and_follower()
        {

            var leader    = RoamingNetworkHistory.Start(NewNetwork());
            RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out _);

            var stranger  = leader.Head.RoamingNetwork.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, "A") ]).
                                                       Sign(Alice, "alice", COSEAlgorithm.Ed25519);

            var refused   = leader.Commit(stranger);
            var published = leader.Commit(DataSource(leader, "A", 1));

            // The same commit, without the signatures of its change set: they are not part of its identification.
            var stripped  = published.Commit!.ToJSON();
            ((JObject) stripped["changeSet"]!).Remove("signatures");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(refused.Status,                                    Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(refused.ErrorResponse,                             Does.Contain("'alice' is no key of a party"));
                Assert.That(published.Status,                                  Is.EqualTo(RoamingNetworkCommitStatus.Committed), published.ErrorResponse);

                // The signatures travel with the commit; a follower checks them with the keys of the network.
                Assert.That(published.Commit.ChangeSet!.Signatures.Length,    Is.GreaterThan(0));
                Assert.That(RoamingNetworkCommit.TryParse(stripped, out var strippedCommit, out var parseError),  Is.True, parseError);

                Assert.That(follower!.TryAppend(strippedCommit!, out var error1),             Is.False);
                Assert.That(error1,                                                           Does.Contain("is unsigned"));
                Assert.That(follower. TryAppend(published.Commit, out var error2),           Is.True, error2);

            }

        }

        #endregion

        #region A_commit_reads_back_its_CBOR()

        [Test]
        public void A_commit_reads_back_its_CBOR()
        {

            var leader  = RoamingNetworkHistory.Start(NewNetwork());
            var commit  = leader.Commit(DataSource(leader, "A", 1)).Commit!;

            var bytes   = commit.ToCBOR().ToByteArray(CBORWriterOptions.Canonical);

            Assert.That(RoamingNetworkCommit.TryParse(CBORValue.Parse(bytes), out var parsed, out var error),  Is.True, error);
            Assert.That(parsed!.Id,                                                                           Is.EqualTo(commit.Id));
            Assert.That(parsed.ChangeSet!.Changes,                                                            Is.EqualTo(commit.ChangeSet!.Changes));

        }

        #endregion

    }

}
