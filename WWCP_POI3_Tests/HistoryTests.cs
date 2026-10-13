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

            Assert.That(leader.Publish(DataSource(leader, "A", 1)).Status,  Is.EqualTo(RoamingNetworkPublishStatus.Published));

            // The follower starts from a snapshot of the leader's head, as JSON text...
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON().ToString(), out var follower, out var error1),  Is.True, error1);

            Assert.That(leader.Publish(DataSource(leader, "B", 2)).Status,  Is.EqualTo(RoamingNetworkPublishStatus.Published));
            Assert.That(leader.Publish(DataSource(leader, "C", 3)).Status,  Is.EqualTo(RoamingNetworkPublishStatus.Published));

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
                leader.Publish(DataSource(leader, "v" + i, i));

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

        #region Publishing_reports_what_happened()

        [Test]
        public void Publishing_reports_what_happened()
        {

            var leader  = RoamingNetworkHistory.Start(NewNetwork());
            var head0   = leader.Head.Commit.Id;

            var csA     = DataSource(leader, "A", 1);
            var csB     = leader.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.RemoveProperty(leader.Head.RoamingNetwork, "description") ]);
            var csC     = DataSource(leader, "C", 3);

            var a       = leader.Publish(csA, ExpectedHeadId: head0);
            var again   = leader.Publish(csA);
            var stale   = leader.Publish(csB, ExpectedHeadId: head0);
            var notFit  = leader.Publish(csB);
            var unsigned = leader.Publish(csB, Rebase: true);
            var b       = leader.Publish(leader.Head.RoamingNetwork.Rebase(csB).SignedByAll(leader.Head.RoamingNetwork));
            var bAgain  = leader.Publish(csB, Rebase: true);
            var c       = leader.Publish(csC, Rebase: true);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(a.Status,        Is.EqualTo(RoamingNetworkPublishStatus.Published));
                Assert.That(again.Status,    Is.EqualTo(RoamingNetworkPublishStatus.AlreadyPublished));
                Assert.That(again.Commit,    Is.SameAs(a.Commit));

                Assert.That(stale.Status,    Is.EqualTo(RoamingNetworkPublishStatus.HeadMoved));
                Assert.That(notFit.Status,   Is.EqualTo(RoamingNetworkPublishStatus.HeadMoved));

                // A rebased change set must be signed again by its owners.
                Assert.That(unsigned.Status,         Is.EqualTo(RoamingNetworkPublishStatus.Refused));
                Assert.That(unsigned.ErrorResponse,  Does.Contain("its owners must sign it again"));

                Assert.That(b.Status,        Is.EqualTo(RoamingNetworkPublishStatus.Published));
                Assert.That(b.Commit!.ChangeSet!.Id,  Is.Not.EqualTo(csB.Id));

                // The original of a rebased change set counts as published.
                Assert.That(bAgain.Status,   Is.EqualTo(RoamingNetworkPublishStatus.AlreadyPublished));
                Assert.That(bAgain.Commit,   Is.SameAs(b.Commit));

                // C changes the data source, as A did: a conflict.
                Assert.That(c.Status,        Is.EqualTo(RoamingNetworkPublishStatus.Refused));
                Assert.That(c.ErrorResponse, Does.Contain("no longer the expected old value"));

                Assert.That(leader.Head.Commit.Revision,  Is.EqualTo(2));

            }

        }

        #endregion

        #region Commits_are_checked()

        [Test]
        public void Commits_are_checked()
        {

            var leader    = RoamingNetworkHistory.Start(NewNetwork());
            RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out _);

            var commit1   = leader.Publish(DataSource(leader, "A", 1)).Commit!;
            var commit2   = leader.Publish(DataSource(leader, "B", 2)).Commit!;

            // Another content under the same identification.
            var forged    = commit1.ToJSON();
            forged["changeSet"]!["description"]!["en"] = "forged";

            // Another identification than the one of its content.
            var wrongId   = commit1.ToJSON();
            wrongId["@id"] = commit2.Id.ToString();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetworkCommit.TryParse(forged,  out _, out var error1),  Is.False);
                Assert.That(error1,                                                         Does.Contain("is not the one of its content"));

                Assert.That(RoamingNetworkCommit.TryParse(wrongId, out _, out var error2),  Is.False);
                Assert.That(error2,                                                         Does.Contain("is not the one of its content"));

                // A commit skipping its predecessor.
                Assert.That(follower!.TryAppend(commit2, out var error3),                   Is.False);
                Assert.That(error3,                                                         Does.Contain("does not follow the head"));

                Assert.That(follower.TryAppend(commit1, out var error4),                    Is.True, error4);
                Assert.That(follower.TryAppend(commit2, out var error5),                    Is.True, error5);

                // A snapshot whose network does not have the ETags of its commit.
                var snapshot = leader.ToSnapshotJSON();
                snapshot["roamingNetwork"]!["dataSource"] = "forged";
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot, out _, out var error6),  Is.False);
                Assert.That(error6,                                                                  Does.Contain("does not have the ETags of the commit"));

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

            var refused   = leader.Publish(stranger);
            var published = leader.Publish(DataSource(leader, "A", 1));

            // The same commit, without the signatures of its change set: they are not part of its identification.
            var stripped  = published.Commit!.ToJSON();
            ((JObject) stripped["changeSet"]!).Remove("signatures");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(refused.Status,                                    Is.EqualTo(RoamingNetworkPublishStatus.Refused));
                Assert.That(refused.ErrorResponse,                             Does.Contain("'alice' is no key of a party"));
                Assert.That(published.Status,                                  Is.EqualTo(RoamingNetworkPublishStatus.Published), published.ErrorResponse);

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
            var commit  = leader.Publish(DataSource(leader, "A", 1)).Commit!;

            var bytes   = commit.ToCBOR().ToByteArray(CBORWriterOptions.Canonical);

            Assert.That(RoamingNetworkCommit.TryParse(CBORValue.Parse(bytes), out var parsed, out var error),  Is.True, error);
            Assert.That(parsed!.Id,                                                                           Is.EqualTo(commit.Id));
            Assert.That(parsed.ChangeSet!.Changes,                                                            Is.EqualTo(commit.ChangeSet!.Changes));

        }

        #endregion

    }

}
