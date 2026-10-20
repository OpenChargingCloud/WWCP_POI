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

using System.Net;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// The archive of a history: the commits it drops when compacting, one segment per compaction,
    /// still readable and provable (SPECIFICATION.md, section 10.6).
    /// </summary>
    [TestFixture]
    public class ArchiveTests
    {

        #region Data

        private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-10-19T12:00:00Z");

        private static RoamingNetworkHistoryRetention Retention
            => new () {
                   VersionCheckpointInterval  = 1,
                   RecentVersions             = 100,
                   RetainCommits              = 2,
                   CompactAfterLogEntries     = Int64.MaxValue
               };

        #endregion


        #region (private static) NewHistory(Store, Archive)

        private static RoamingNetworkHistory NewHistory(IRoamingNetworkHistoryStore    Store,
                                                        IRoamingNetworkHistoryArchive  Archive)
        {

            var history = RoamingNetworkHistory.Start(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                                                         DataSource:  "v0",
                                                                         Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")).WithKeys(),
                                                      Store:      Store,
                                                      Retention:  Retention,
                                                      Archive:    Archive);

            history.CheckpointSigners = [ CheckpointTests.RNOSigner ];

            return history;

        }

        #endregion

        #region (private static) Commit(History, Value)

        private static RoamingNetworkCommit Commit(RoamingNetworkHistory  History,
                                                   String                 Value)
        {

            var result = History.Commit(History.Head.RoamingNetwork.CreateSignedChangeSet(
                                            [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                                            CreatedAt: T0.AddMinutes((Double) History.Head.Commit.Revision)
                                        ));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            return result.Commit!;

        }

        #endregion

        #region (private static) Checkpoint(History)

        private static RoamingNetworkCheckpoint Checkpoint(RoamingNetworkHistory History)
        {
            Assert.That(History.TryMakeCheckpoint(out var checkpoint, out var error, CreatedAt: T0.AddHours(History.Checkpoints.Count + 1)), Is.True, error);
            return checkpoint!;
        }

        #endregion

        #region (private static) Proves(History, Commit, Checkpoint)

        private static Boolean Proves(RoamingNetworkHistory     History,
                                      RoamingNetworkCommit      Commit,
                                      RoamingNetworkCheckpoint  Checkpoint)

            => History.TryGetCommitProof(Commit.Id, out var proof, out _) &&
               proof.CheckpointId == Checkpoint.Id &&
               proof.Verify(Checkpoint, out _);

        #endregion


        #region Compaction_archives_the_commits_it_drops()

        [Test]
        public void Compaction_archives_the_commits_it_drops()
        {

            var archive  = new RoamingNetworkHistoryMemoryArchive();
            var history  = NewHistory(new RoamingNetworkHistoryMemoryStore(), archive);

            var c1       = Commit(history, "v1");
            var c2       = Commit(history, "v2");
            var cp0      = Checkpoint(history);
            var c3       = Commit(history, "v3");
            var c4       = Commit(history, "v4");
            var cp1      = Checkpoint(history);
            Commit(history, "v5");
            Commit(history, "v6");
            var c7       = Commit(history, "v7");

            var genesis  = history.Root;

            Assert.That(history.Compact(out var compactError),  Is.True, compactError);

            var segment1 = archive.Load(1)!;

            // Replayed alone, the segment leads to the version of its end, the commit of the checkpoint.
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(segment1.Base, out var replayed, out var replayError),   Is.True, replayError);
            Assert.That(replayed!.TryAppend(segment1.Commits.Take(segment1.Commits.Count), out _, out var appendError),  Is.True, appendError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(history.Root.Id,                                         Is.EqualTo(c4.Id), "compacted to the commit of the checkpoint");

                Assert.That(history.ArchiveSegments,                                 Has.Count.EqualTo(1));
                Assert.That(segment1.Info.Number,                                    Is.EqualTo(1));
                Assert.That(segment1.Info.RootId,                                    Is.EqualTo(genesis.Id));
                Assert.That(segment1.Info.EndId,                                     Is.EqualTo(c4.Id));
                Assert.That(segment1.Info.FromRevision,                              Is.Zero);
                Assert.That(segment1.Info.ToRevision,                                Is.EqualTo(4));
                Assert.That(segment1.Info.CommitIds,                                 Is.EqualTo(new[] { c1.Id, c2.Id, c3.Id, c4.Id }));
                Assert.That(segment1.Base.ContainsKey("runtimeStatus"),              Is.False);

                // Dropped from the history, still in the archive.
                Assert.That(history.TryGetCommit(c1.Id, out _),                      Is.False);
                Assert.That(history.TryGetArchivedCommit(c1.Id, out var archived),   Is.True);
                Assert.That(archived!.ToJSON().ToString(),                           Is.EqualTo(c1.ToJSON().ToString()));

                // The genesis as well, which only the base of the first segment holds.
                Assert.That(history.TryGetCommit(genesis.Id, out _),                 Is.False);
                Assert.That(history.TryGetArchivedCommit(genesis.Id, out var archivedGenesis) && archivedGenesis.Id == genesis.Id,  Is.True);
                Assert.That(Proves(history, genesis, cp0),                           Is.True);

                // Provable as before: a commit of the first checkpoint, and one of the second.
                Assert.That(Proves(history, c1, cp0),                                Is.True);
                Assert.That(Proves(history, c3, cp1),                                Is.True);

                Assert.That(replayed.Head.Commit.Id,                                 Is.EqualTo(c4.Id));
                Assert.That(replayed.Head.Commit.ETags,                              Is.EqualTo(cp1.ETags));

            }

            // A second compaction continues where the first one ended.
            var cp2      = Checkpoint(history);
            Commit(history, "v8");
            Commit(history, "v9");

            Assert.That(history.Compact(out compactError),      Is.True, compactError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.ArchiveSegments.Select(info => info.Number),     Is.EqualTo(new UInt32[] { 1, 2 }));
                Assert.That(history.ArchiveSegments[1].RootId,                       Is.EqualTo(c4.Id));
                Assert.That(history.ArchiveSegments[1].EndId,                        Is.EqualTo(c7.Id));
                Assert.That(Proves(history, c7, cp2),                                Is.True);
                Assert.That(Proves(history, c1, cp0),                                Is.True, "an older segment");
            }

        }

        #endregion

        #region A_failing_archive_fails_the_compaction()

        [Test]
        public void A_failing_archive_fails_the_compaction()
        {

            var archive  = new RoamingNetworkHistoryMemoryArchive { FailAppends = true };
            var history  = NewHistory(new RoamingNetworkHistoryMemoryStore(), archive);

            var c1       = Commit(history, "v1");
            Checkpoint(history);
            Commit(history, "v2");
            Commit(history, "v3");
            Commit(history, "v4");

            var root     = history.Root.Id;
            var failed   = history.Compact(out var compactError);

            archive.FailAppends = false;

            var again    = history.Compact(out var againError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failed,                                  Is.False);
                Assert.That(compactError,                            Does.Contain("archive fails"));
                Assert.That(again,                                   Is.True, againError);
                Assert.That(history.Root.Id,                         Is.EqualTo(c1.Id));
                Assert.That(history.ArchiveSegments[0].RootId,       Is.EqualTo(root));
            }

        }

        #endregion

        #region The_file_archive_survives_a_restart()

        [Test]
        public void The_file_archive_survives_a_restart()
        {

            var directory = Path.Combine(Path.GetTempPath(), "poi-archive-" + Guid.NewGuid().ToString("N"));

            try
            {

                RoamingNetworkCommit      c1;
                RoamingNetworkCheckpoint  cp0;

                using (var store   = new RoamingNetworkHistoryFileStore(Path.Combine(directory, "store")))
                using (var archive = new RoamingNetworkHistoryFileArchive(Path.Combine(directory, "archive")))
                {

                    var history = NewHistory(store, archive);

                    c1  = Commit(history, "v1");
                    Commit(history, "v2");
                    cp0 = Checkpoint(history);
                    Commit(history, "v3");
                    Commit(history, "v4");
                    Commit(history, "v5");

                    Assert.That(history.Compact(out var compactError), Is.True, compactError);

                }

                // A line written only partly, e.g. by a crash, is dropped when opening.
                File.AppendAllText(Path.Combine(directory, "archive", RoamingNetworkHistoryFileArchive.SegmentsFileName), "{\"number\":2,\"root");

                using (var store   = new RoamingNetworkHistoryFileStore(Path.Combine(directory, "store")))
                using (var archive = new RoamingNetworkHistoryFileArchive(Path.Combine(directory, "archive")))
                {

                    Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError, Archive: archive),  Is.True, loadError);

                    var segment = archive.Load(1);

                    using (Assert.EnterMultipleScope())
                    {
                        Assert.That(archive.Segments.Select(info => info.Number),          Is.EqualTo(new UInt32[] { 1 }));
                        Assert.That(segment!.Commits.Select(commit => commit.Id),          Is.EqualTo(segment.Info.CommitIds));
                        Assert.That(loaded!.Root.Id,                                       Is.EqualTo(cp0.CommitId));
                        Assert.That(loaded.TryGetArchivedCommit(c1.Id, out _),             Is.True);
                        Assert.That(Proves(loaded, c1, cp0),                               Is.True);
                    }

                }

            }
            finally
            {
                try { Directory.Delete(directory, recursive: true); } catch { }
            }

        }

        #endregion

        #region Archived_commits_are_served_over_HTTP()

        [Test]
        public async Task Archived_commits_are_served_over_HTTP()
        {

            var history  = NewHistory(new RoamingNetworkHistoryMemoryStore(), new RoamingNetworkHistoryMemoryArchive());
            var c1       = Commit(history, "v1");
            var cp0      = Checkpoint(history);
            Commit(history, "v2");
            Commit(history, "v3");
            Commit(history, "v4");

            Assert.That(history.Compact(out var compactError), Is.True, compactError);

            await using var httpServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI      = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(history);

            using var httpClient = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/history/") };

            using var commit     = await httpClient.GetAsync($"commits/{c1.Id}");
            using var proof      = await httpClient.GetAsync($"commits/{c1.Id}/proof");
            var archiveJSON      = JObject.Parse(await httpClient.GetStringAsync("archive"));
            var segmentJSON      = JObject.Parse(await httpClient.GetStringAsync("archive/1"));
            using var missing    = await httpClient.GetAsync("archive/2");

            Assert.That(RoamingNetworkCheckpointProof.TryParse(JObject.Parse(await proof.Content.ReadAsStringAsync()), out var parsedProof, out var proofError),  Is.True, proofError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(commit.StatusCode,                                         Is.EqualTo(HttpStatusCode.OK));
                Assert.That(JObject.Parse(await commit.Content.ReadAsStringAsync())["@id"]!.Value<String>(),  Is.EqualTo(c1.Id.ToString()));
                Assert.That(parsedProof!.Verify(cp0, out var verifyError),             Is.True, verifyError);

                Assert.That(archiveJSON["segments"]!.Count(),                          Is.EqualTo(1));
                Assert.That(archiveJSON["segments"]![0]!["endId"]!.Value<String>(),    Is.EqualTo(cp0.CommitId.ToString()));
                Assert.That(archiveJSON["segments"]![0]!["commitIds"],                 Is.Null, "the list without the identifications");
                Assert.That(segmentJSON["commits"]!.Count(),                           Is.EqualTo(1), "the commit of the checkpoint, the new root");
                Assert.That(missing.StatusCode,                                        Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

    }

}
