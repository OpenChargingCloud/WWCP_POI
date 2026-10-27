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

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// Readers of a history while a commit holds its lock, e.g. syncing to a slow disk or merging a large network: they
    /// read the state published last, without waiting for it.
    /// </summary>
    [TestFixture]
    public class ConcurrentReadTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E11      = EVSE_Id.                   Parse("DE*GEF*E1*1");

        /// <summary>
        /// How long a reader may take: far more than it needs, far less than the commit is held.
        /// </summary>
        private static readonly TimeSpan                    Patience = TimeSpan.FromSeconds(5);

        #endregion

        #region (class) BlockingStore

        /// <summary>
        /// A store in memory that holds every commit appended until it is released, as a slow disk would: the commit
        /// waits while it holds the lock of the history.
        /// </summary>
        private sealed class BlockingStore : IRoamingNetworkHistoryStore
        {

            private readonly RoamingNetworkHistoryMemoryStore  store     = new ();
            private readonly ManualResetEventSlim              released  = new (true);

            public ManualResetEventSlim                        Entered   { get; } = new (false);

            public void Block()
                => released.Reset();

            public void Release()
                => released.Set();

            public RoamingNetworkHistoryStoreContent? Load()
                => store.Load();

            public void WriteBase(JObject Snapshot)
                => store.WriteBase(Snapshot);

            public void Append(RoamingNetworkHistoryLogEntry Entry)
            {

                if (Entry.Type == RoamingNetworkHistoryLogEntryType.Commit && !released.IsSet)
                {
                    Entered.Set();
                    released.Wait();
                }

                store.Append(Entry);

            }

            public void Compact(JObject                                     Snapshot,
                                IEnumerable<RoamingNetworkHistoryLogEntry>  Entries)

                => store.Compact(Snapshot, Entries);

            public void Dispose()
            {
                released.Set();
                store.Dispose();
            }

        }

        #endregion

        #region (static) NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                  Created:                   Created,
                                  ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                   Created:        Created,
                                                                   ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                      Created:           Created,
                                                                                                      ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                               Created:  Created,
                                                                                                                                               EVSEs:    [ new EVSE(E11, Created: Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region (static) Within(What, Read)

        /// <summary>
        /// The result of the given read, which must not wait for the commit holding the lock.
        /// </summary>
        private static T Within<T>(String   What,
                                   Func<T>  Read)
        {

            var task = Task.Run(Read);

            Assert.That(task.Wait(Patience), Is.True, $"{What} waits for the commit holding the lock of the history!");

            return task.Result;

        }

        #endregion


        #region Readers_do_not_wait_for_a_commit()

        [Test]
        public void Readers_do_not_wait_for_a_commit()
        {

            using var store  = new BlockingStore();

            var history      = RoamingNetworkHistory.Start(NewNetwork(), Store: store);
            history.AttestationSigners = [ Proofs.StationProofTests.RNOSigner ];
            history.RefreshAttestations();

            var before       = history.Head;
            var changeSet    = before.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(nameof(EVSE), E11.ToString(), EVSE.PropertyKeys.PhysicalReference, null, "P-1") ],
                                                                           CreatedAt: Created.AddMinutes(1));

            store.Block();

            var commit       = Task.Run(() => history.Commit(changeSet));

            try
            {

                Assert.That(store.Entered.Wait(Patience), Is.True, "The commit did not reach the store!");

                // The commit holds the lock now; every reader reads the state before it.
                Assert.That(Within("Head",                    () => history.Head.Commit.Id),                                    Is.EqualTo(before.Commit.Id));
                Assert.That(Within("Count",                   () => history.Count),                                             Is.EqualTo(1));
                Assert.That(Within("Root",                    () => history.Root.Id),                                           Is.EqualTo(before.Commit.Id));
                Assert.That(Within("Contains",                () => history.Contains(before.Commit.Id)),                        Is.True);
                Assert.That(Within("TryGetCommit",            () => history.TryGetCommit(before.Commit.Id, out _)),             Is.True);
                Assert.That(Within("HaveIds",                 () => history.HaveIds()),                                         Is.EqualTo(new[] { before.Commit.Id }));
                Assert.That(Within("TryGetCommitsSince",      () => history.TryGetCommitsSince(before.Commit.Id, out var since, out _) ? since.Count : -1), Is.Zero);
                Assert.That(Within("TryGetMissingCommits",    () => history.TryGetMissingCommits([ before.Commit.Id ], out var missing, out _) ? missing.Count : -1), Is.Zero);
                Assert.That(Within("CurrentAttestations",     () => history.CurrentAttestations().Count),                       Is.EqualTo(1));
                Assert.That(Within("Checkpoints",             () => history.Checkpoints.Count),                                 Is.Zero);
                Assert.That(Within("LatestCheckpoint",        () => history.LatestCheckpoint),                                  Is.Null);
                Assert.That(Within("GetCheckpointsSince",     () => history.GetCheckpointsSince().Count),                       Is.Zero);

                var proof = Within("TryGetChargingStationProof", () => history.TryGetChargingStationProof(S1, out var made, out var error) ? made : throw new AssertionException(error));

                Assert.That(proof.CommitId,            Is.EqualTo(before.Commit.Id));
                Assert.That(proof.Attestations.Length, Is.EqualTo(1));

            }
            finally
            {
                store.Release();
            }

            // Released: the commit is complete, and the readers see it, attested.
            Assert.That(commit.Wait(Patience), Is.True);
            Assert.That(commit.Result.IsCommitted, Is.True, commit.Result.ErrorResponse);

            var after = history.Head;

            Assert.That(after.Commit.Id,                                 Is.Not.EqualTo(before.Commit.Id));
            Assert.That(history.Count,                                   Is.EqualTo(2));
            Assert.That(history.TryGetCommit(after.Commit.Id, out _),    Is.True);
            Assert.That(history.TryGetCommitsSince(before.Commit.Id, out var commits, out var sinceError), Is.True, sinceError);
            Assert.That(commits!.Select(commit => commit.Id),             Is.EqualTo(new[] { after.Commit.Id }));
            Assert.That(history.CurrentAttestations().Single().CommitId, Is.EqualTo(after.Commit.Id));

            Assert.That(history.TryGetChargingStationProof(S1, out var proofAfter, out var proofError), Is.True, proofError);
            Assert.That(proofAfter.CommitId, Is.EqualTo(after.Commit.Id));

        }

        #endregion

        #region Tips_waiting_change_sets_commit_proofs_and_the_archive_do_not_wait_for_a_commit()

        /// <summary>
        /// The readers that waited for the lock until POI3-ISSUES.md S12: the tips, the change sets waiting for signatures,
        /// the proofs of commits, from the commits held and from the archive, the archived commits and the snapshot of a
        /// checkpoint whose version is calculated again. They read the state before the commit holding the lock.
        /// </summary>
        [Test]
        public void Tips_waiting_change_sets_commit_proofs_and_the_archive_do_not_wait_for_a_commit()
        {

            using var store    = new BlockingStore();

            var network        = CoSigningTests.Network();
            var history        = RoamingNetworkHistory.Start(network,
                                                             Store:      store,
                                                             // Only the versions of the root, the tips and the latest commit are kept.
                                                             Retention:  new RoamingNetworkHistoryRetention {
                                                                             VersionCheckpointInterval  = 0,
                                                                             RecentVersions             = 1,
                                                                             RetainCommits              = 2,
                                                                             CompactAfterLogEntries     = Int64.MaxValue
                                                                         },
                                                             Archive:    new RoamingNetworkHistoryMemoryArchive());

            RoamingNetworkCommit Commit(String DataSource)
            {
                var result = history.Commit(CoSigningTests.SignedBy(CoSigningTests.ChangeSet(history.Head.RoamingNetwork, DataSource), network, "Alpha", "Gamma"));
                Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);
                return result.Commit!;
            }

            RoamingNetworkCheckpoint Checkpoint(RoamingNetworkCommit Of)
            {
                Assert.That(history.TryMakeCheckpoint(out var checkpoint, out var error, Of.Id, DateTimeOffset.Parse("2026-10-23T00:00:00Z").AddHours(history.Checkpoints.Count)), Is.True, error);
                return checkpoint!;
            }

            var c1       = Commit("v1");
            var c2       = Commit("v2");
            var cp0      = Checkpoint(c2);
            var c3       = Commit("v3");
            var c4       = Commit("v4");
            Checkpoint(c4);
            var c5       = Commit("v5");
            var c6       = Commit("v6");
            var cp2      = Checkpoint(c6);
            var c7       = Commit("v7");

            // Compacted to the commit of the second checkpoint: the commits before are in the archive.
            Assert.That(history.Compact(out var compactError), Is.True, compactError);
            Assert.That(history.Root.Id,                       Is.EqualTo(c4.Id));

            var waiting  = CoSigningTests.SignedBy(CoSigningTests.ChangeSet(history.Head.RoamingNetwork, "waiting"), network, "Alpha");

            Assert.That(history.CoSign(waiting).Status,        Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            store.Block();

            var commit   = Task.Run(() => history.Commit(CoSigningTests.SignedBy(CoSigningTests.ChangeSet(history.Head.RoamingNetwork, "v8"), network, "Alpha", "Gamma")));

            try
            {

                Assert.That(store.Entered.Wait(Patience), Is.True, "The commit did not reach the store!");

                var tips      = Within("Tips",                         () => history.Tips);
                var pending   = Within("PendingChangeSets",            () => history.PendingChangeSets);
                var result    = Within("TryGetPendingChangeSet",       () => history.TryGetPendingChangeSet(waiting.Id, out var read) ? read : null);
                var heldProof = Within("TryGetCommitProof (held)",     () => history.TryGetCommitProof(c5.Id, out var proof, out var error) ? proof : throw new AssertionException(error));
                var oldProof  = Within("TryGetCommitProof (archive)",  () => history.TryGetCommitProof(c1.Id, out var proof, out var error) ? proof : throw new AssertionException(error));
                var archived  = Within("TryGetArchivedCommit",         () => history.TryGetArchivedCommit(c3.Id, out var archivedCommit) ? archivedCommit : null);
                var snapshot  = Within("TryGetCheckpointSnapshotJSON", () => history.TryGetCheckpointSnapshotJSON(cp2.Id, out var json, out var error) ? json : throw new AssertionException(error));

                using (Assert.EnterMultipleScope())
                {

                    Assert.That(tips.Select(tip => tip.Commit.Id),                      Is.EqualTo(new[] { c7.Id }));
                    Assert.That(tips[0].RoamingNetwork.DataSource,                      Is.EqualTo("v7"));
                    Assert.That(pending.Select(changeSet => changeSet.Id),              Is.EqualTo(new[] { waiting.Id }));
                    Assert.That(result?.Missing,                                        Has.Count.EqualTo(1));

                    Assert.That(heldProof.CheckpointId,                                 Is.EqualTo(cp2.Id));
                    Assert.That(heldProof.Verify(cp2, out var heldError),               Is.True, heldError);
                    Assert.That(oldProof. CheckpointId,                                 Is.EqualTo(cp0.Id));
                    Assert.That(oldProof. Verify(cp0, out var oldError),                Is.True, oldError);
                    Assert.That(archived?.Id,                                           Is.EqualTo(c3.Id));

                    // The version of the commit of the checkpoint is no longer kept: calculated again, for the reader.
                    Assert.That(RoamingNetworkCommit.TryParse((JObject) snapshot[RoamingNetworkHistory.PropertyKeys.Commit]!, out var snapshotCommit, out _) ? snapshotCommit.Id : (RoamingNetworkCommit_Id?) null,
                                                                                        Is.EqualTo(c6.Id));
                    Assert.That(snapshot[RoamingNetworkHistory.PropertyKeys.RoamingNetwork]!["dataSource"]!.Value<String>(),
                                                                                        Is.EqualTo("v6"));
                    Assert.That(snapshot.ContainsKey(RoamingNetworkHistory.PropertyKeys.RuntimeStatus),  Is.False);

                }

            }
            finally
            {
                store.Release();
            }

            Assert.That(commit.Wait(Patience), Is.True);
            Assert.That(commit.Result.IsCommitted, Is.True, commit.Result.ErrorResponse);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.Tips.Select(tip => tip.Commit.Id),                  Is.EqualTo(new[] { commit.Result.Commit!.Id }));
                Assert.That(history.TryGetPendingChangeSet(waiting.Id, out var after),  Is.True);
                Assert.That(after!.Missing,                                             Has.Count.EqualTo(1));
            }

        }

        #endregion

        #region A_proof_does_not_wait_for_the_attestation_of_a_new_head()

        /// <summary>
        /// A proof asked for while a new head is shown, but not attested yet: it does not wait for the commit, which holds
        /// the lock on, merging the tips, but takes the head attested before, with its attestation.
        /// </summary>
        [Test]
        public void A_proof_does_not_wait_for_the_attestation_of_a_new_head()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());
            history.AttestationSigners = [ Proofs.StationProofTests.RNOSigner ];
            history.RefreshAttestations();

            var before     = history.Head;
            var changeSet  = before.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(nameof(EVSE), E11.ToString(), EVSE.PropertyKeys.PhysicalReference, null, "P-1") ],
                                                                         CreatedAt: Created.AddMinutes(1));

            // Announced while the commit holds the lock: the new head is shown, its attestation not made yet.
            RoamingNetworkCommit_Id?     shownHead        = null;
            Task<RoamingNetworkProof?>?  proofInTheGap    = null;
            Boolean                      proofDidNotWait  = false;

            history.OnCommitAnnouncement += announcement => {
                shownHead        = history.Head.Commit.Id;
                proofInTheGap    = Task.Run(() => history.TryGetChargingStationProof(S1, out var made, out _) ? made : null);
                proofDidNotWait  = proofInTheGap.Wait(Patience);
            };

            var result = history.Commit(changeSet);

            Assert.That(result.IsCommitted, Is.True, result.ErrorResponse);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shownHead,                                  Is.EqualTo(result.Commit!.Id), "The new head was not shown when it was announced!");
                Assert.That(proofDidNotWait,                            Is.True,                       "The proof waits for the commit holding the lock of the history!");
                Assert.That(proofInTheGap!.Result?.CommitId,            Is.EqualTo(before.Commit.Id));
                Assert.That(proofInTheGap.Result?.Attestations.Length,  Is.EqualTo(1));
            }

            // Attested: the proof of the new head.
            Assert.That(history.TryGetChargingStationProof(S1, out var proofAfter, out var proofError), Is.True, proofError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(proofAfter.CommitId,            Is.EqualTo(result.Commit!.Id));
                Assert.That(proofAfter.Attestations.Length, Is.EqualTo(1));
            }

        }

        #endregion

    }

}
