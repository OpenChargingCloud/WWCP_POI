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

using System.Text;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// A history kept in a store: written before it is shown, loaded again after a restart, also after a crash.
    /// </summary>
    [TestFixture]
    public class PersistenceTests
    {

        #region Data

        private static readonly DateTimeOffset              Created   = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0        = DateTimeOffset.Parse("2026-10-12T12:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF       = ChargingStationOperator_Id.Parse("DE*GEF");
        private const           String                      Operator  = nameof(ChargingStationOperator);

        private String? directory;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "poi3-history-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (directory is not null && Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
            catch
            { }
        }

        #endregion

        #region NewNetwork()

        internal static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                  DataSource:                "v1",
                                  Created:                   Created,
                                  ChargingStationOperators:  [ new ChargingStationOperator(GEF, Created: Created) ]).WithKeys();

        #endregion

        #region Change(History, Minutes, Property, Value, Version = null)

        /// <summary>
        /// Commit a change of a property of the roaming network, signed by its owners, to the given version or the head.
        /// </summary>
        internal static RoamingNetworkCommitResult Change(RoamingNetworkHistory  History,
                                                          Int32                  Minutes,
                                                          String                 Property,
                                                          String                 Value,
                                                          RoamingNetwork?        Version   = null)
        {

            var version = Version ?? History.Head.RoamingNetwork;

            return History.Commit(version.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(version, Property, null,
                                                                                                      Property is "name" or "description"
                                                                                                          ? new Newtonsoft.Json.Linq.JObject(new Newtonsoft.Json.Linq.JProperty("en", Value))
                                                                                                          : Value) ],
                                                                 CreatedAt: T0.AddMinutes(Minutes)));

        }

        #endregion

        #region Fill(History)

        /// <summary>
        /// Commits, a merge, a conflict, signatures added later and status updates.
        /// </summary>
        private static void Fill(RoamingNetworkHistory History)
        {

            var root = History.Head.RoamingNetwork;

            Assert.That(Change(History, 1, "dataSource",  "v2").IsCommitted);
            Assert.That(Change(History, 2, "name",        "Network", root).IsCommitted);   // a branch, merged
            Assert.That(Change(History, 3, "dataSource",  "v3", root).IsCommitted);        // a branch, conflicting

            Assert.That(History.TryAddSignatures(History.Root.Id,
                                                 [ History.Root.CreateSignature(CommitSignatureTests.RNOKey, "rno", COSEAlgorithm.Ed25519, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, SignedAt: T0) ],
                                                 CommitSignatureTests.VerifyCommit,
                                                 out _, out _, out var signatureError),
                        Is.True, signatureError);

            History.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline",   T0.AddMinutes(4), out _, out _);
            History.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "available", T0.AddMinutes(5), out _, out _);

        }

        #endregion

        #region AssertSame(Loaded, Original)

        private static void AssertSame(RoamingNetworkHistory  Loaded,
                                       RoamingNetworkHistory  Original)
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Loaded.Head.Commit.Id,                               Is.EqualTo(Original.Head.Commit.Id));
                Assert.That(Loaded.Tips.Select(tip => tip.Commit.Id),            Is.EqualTo(Original.Tips.Select(tip => tip.Commit.Id)));
                Assert.That(Loaded.Conflicts.Length,                             Is.EqualTo(Original.Conflicts.Length));
                Assert.That(Loaded.Count,                                        Is.EqualTo(Original.Count));
                Assert.That(Loaded.Root.Signatures,                              Is.EqualTo(Original.Root.Signatures));
                Assert.That(Loaded.Head.RoamingNetwork.ETags,                    Is.EqualTo(Original.Head.RoamingNetwork.ETags));

                Assert.That(Loaded.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status),  Is.True);
                Assert.That(status!.Value,                                       Is.EqualTo("available"));
                Assert.That(status.Timestamp,                                    Is.EqualTo(T0.AddMinutes(5)));

            }

        }

        #endregion


        #region A_history_is_loaded_again_from_its_files()

        [Test]
        public void A_history_is_loaded_again_from_its_files()
        {

            RoamingNetworkHistory original;

            using (var store = new RoamingNetworkHistoryFileStore(directory!))
            {
                original = RoamingNetworkHistory.Start(NewNetwork(), Store: store);
                Fill(original);
                original.DetachStore();
            }

            using var store2 = new RoamingNetworkHistoryFileStore(directory!);

            Assert.That(RoamingNetworkHistory.TryLoad(store2, out var loaded, out var error), Is.True, error);

            AssertSame(loaded!, original);

            // It goes on in the store.
            Assert.That(Change(loaded!, 9, "description", "Go on").Status,  Is.EqualTo(RoamingNetworkCommitStatus.Committed));
            Assert.That(store2.Load()!.Entries.Count(entry => entry.Type == RoamingNetworkHistoryLogEntryType.Commit),  Is.EqualTo(loaded!.Count - 1));

        }

        #endregion

        #region A_change_of_a_property_the_network_had_not_is_loaded_again()

        /// <summary>
        /// A change set made without an old value expects the value it found: of a property the roaming network has not,
        /// JSON null. Read again from the store, it must still fit the version it was made for.
        /// </summary>
        [Test]
        public void A_change_of_a_property_the_network_had_not_is_loaded_again()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = RoamingNetworkHistory.Start(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                                                          Created:                   Created,
                                                                          ChargingStationOperators:  [ new ChargingStationOperator(GEF, Created: Created) ]).WithKeys(),
                                                       Store: store);

            Assert.That(Change(history, 1, "dataSource", "v2").IsCommitted);

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var error),  Is.True, error);
            Assert.That(loaded!.Head.RoamingNetwork.DataSource,                                Is.EqualTo("v2"));

        }

        #endregion

        #region A_line_written_only_partly_is_dropped()

        [Test]
        public void A_line_written_only_partly_is_dropped()
        {

            RoamingNetworkHistory original;

            using (var store = new RoamingNetworkHistoryFileStore(directory!))
            {
                original = RoamingNetworkHistory.Start(NewNetwork(), Store: store);
                Fill(original);
                original.DetachStore();
            }

            var log     = Path.Combine(directory!, File.ReadAllText(Path.Combine(directory!, RoamingNetworkHistoryFileStore.CurrentFileName)).Trim(), RoamingNetworkHistoryFileStore.LogFileName);
            var length  = new FileInfo(log).Length;

            // A crash in the middle of a line.
            File.AppendAllText(log, "{\"type\":\"commit\",\"commit\":{\"@id\":\"12", Encoding.UTF8);

            using var store2 = new RoamingNetworkHistoryFileStore(directory!);

            Assert.That(RoamingNetworkHistory.TryLoad(store2, out var loaded, out var error), Is.True, error);

            AssertSame(loaded!, original);
            Assert.That(new FileInfo(log).Length, Is.EqualTo(length), "The part is cut off.");

        }

        #endregion

        #region A_log_of_lines_longer_than_its_read_buffer_is_read_again()

        /// <summary>
        /// The log is read line by line, in pieces of 64 KiB: commits of more, lines across the pieces, and a line
        /// written only partly at its end.
        /// </summary>
        [Test]
        public void A_log_of_lines_longer_than_its_read_buffer_is_read_again()
        {

            RoamingNetworkHistory original;

            using (var store = new RoamingNetworkHistoryFileStore(directory!))
            {

                original = RoamingNetworkHistory.Start(NewNetwork(), Store: store);

                for (var minutes = 1; minutes <= 5; minutes++)
                    Assert.That(Change(original, minutes, "dataSource", new String((Char) ('a' + minutes), 30000 * minutes)).IsCommitted);

                Fill(original);
                original.DetachStore();

            }

            var log     = Path.Combine(directory!, File.ReadAllText(Path.Combine(directory!, RoamingNetworkHistoryFileStore.CurrentFileName)).Trim(), RoamingNetworkHistoryFileStore.LogFileName);
            var length  = new FileInfo(log).Length;

            Assert.That(length, Is.GreaterThan(3 * 64 * 1024));

            File.AppendAllText(log, "{\"type\":\"commit\",\"commit\":{\"@id\":\"" + new String('1', 100000), Encoding.UTF8);

            using var store2 = new RoamingNetworkHistoryFileStore(directory!);

            Assert.That(RoamingNetworkHistory.TryLoad(store2, out var loaded, out var error), Is.True, error);

            AssertSame(loaded!, original);
            Assert.That(new FileInfo(log).Length, Is.EqualTo(length), "The part is cut off.");

        }

        #endregion

        #region An_invalid_line_before_the_last_one_is_an_error()

        [Test]
        public void An_invalid_line_before_the_last_one_is_an_error()
        {

            using (var store = new RoamingNetworkHistoryFileStore(directory!))
            {
                var history = RoamingNetworkHistory.Start(NewNetwork(), Store: store);
                Fill(history);
                history.DetachStore();
            }

            var log    = Path.Combine(directory!, File.ReadAllText(Path.Combine(directory!, RoamingNetworkHistoryFileStore.CurrentFileName)).Trim(), RoamingNetworkHistoryFileStore.LogFileName);
            var lines  = File.ReadAllLines(log);
            lines[1]   = "{ broken";
            File.WriteAllLines(log, lines);

            using var store2 = new RoamingNetworkHistoryFileStore(directory!);

            Assert.That(RoamingNetworkHistory.TryLoad(store2, out _, out var error),  Is.False);
            Assert.That(error,                                                       Does.Contain("line 2"));

        }

        #endregion

        #region A_directory_is_used_by_one_store_at_a_time()

        [Test]
        public void A_directory_is_used_by_one_store_at_a_time()
        {

            using var store = new RoamingNetworkHistoryFileStore(directory!);

            Assert.That(() => new RoamingNetworkHistoryFileStore(directory!),
                        Throws.InvalidOperationException.With.Message.Contains("used by another store"));

            Assert.That(() => RoamingNetworkHistory.Start(NewNetwork(), Store: store).DetachStore(),  Throws.Nothing);
            Assert.That(() => store.WriteBase(new Newtonsoft.Json.Linq.JObject()),
                        Throws.InvalidOperationException.With.Message.Contains("is not empty"));

        }

        #endregion

        #region A_commit_the_store_can_not_keep_is_not_taken()

        [Test]
        public void A_commit_the_store_can_not_keep_is_not_taken()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = RoamingNetworkHistory.Start(NewNetwork(), Store: store);
            var head     = history.Head.Commit.Id;

            store.BeforeWrite = type => throw new IOException("Disk full");

            var result   = Change(history, 1, "dataSource", "v2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status,            Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(result.ErrorResponse,     Does.Contain("can not be stored").And.Contain("Disk full"));
                Assert.That(history.Head.Commit.Id,   Is.EqualTo(head));
                Assert.That(history.Count,            Is.EqualTo(1));
            }

        }

        #endregion

        #region A_compacted_store_keeps_the_status_of_entities_added_after_its_root()

        /// <summary>
        /// The base of a compacted store is the snapshot of its new root with the runtime status of the head, also of
        /// entities added by the commits after the root: loaded again, it is taken all the same.
        /// </summary>
        [Test]
        public void A_compacted_store_keeps_the_status_of_entities_added_after_its_root()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = RoamingNetworkHistory.Start(NewNetwork(),
                                                       Store:      store,
                                                       Retention:  new RoamingNetworkHistoryRetention {
                                                                       RetainCommits              = 1,
                                                                       CompactAfterLogEntries     = Int64.MaxValue,
                                                                       VersionCheckpointInterval  = 1,
                                                                       RecentVersions             = 1
                                                                   });

            Assert.That(Change(history, 1, "dataSource", "v2").IsCommitted);
            Assert.That(Change(history, 2, "dataSource", "v3").IsCommitted);

            // After the root the compaction finds: one revision behind the head.
            var added = history.Commit(history.Head.RoamingNetwork.CreateSignedChangeSet(
                                           [ RoamingNetworkChange.Add(new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*NEW"), Created: Created).WithKeys()) ],
                                           CreatedAt: T0.AddMinutes(3)));

            Assert.That(added.IsCommitted, Is.True, added.ErrorResponse);

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*NEW", "offline", Created.AddDays(1), out _, out _);
            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", Created.AddDays(1), out _, out _);

            Assert.That(history.Compact(out var compactError),                                   Is.True, compactError);
            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError),  Is.True, loadError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.Root.Revision,                                                                                  Is.EqualTo(2));
                Assert.That(loaded!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*NEW", out var newStatus) ? newStatus.Value : null,  Is.EqualTo("offline"));
                Assert.That(loaded.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var known)    ? known.Value  : null,  Is.EqualTo("offline"));
            }

        }

        #endregion

        #region A_merge_lost_by_a_crash_is_made_again()

        [Test]
        public void A_merge_lost_by_a_crash_is_made_again()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = RoamingNetworkHistory.Start(NewNetwork(), Store: store);
            var root     = history.Head.RoamingNetwork;
            var errors   = new List<String>();

            history.OnStoreError += (history, description, exception) => errors.Add(description);

            Assert.That(Change(history, 1, "dataSource", "v2").IsCommitted);

            // The second commit is stored, its merge commit is not: the crash came in between.
            var commits = 0;
            store.BeforeWrite = type => { if (type == "Commit" && ++commits == 2) throw new IOException("Crash"); };

            Assert.That(Change(history, 2, "name", "Network", root).IsCommitted);

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(errors.Single(),               Does.Contain("A merge commit can not be stored"));
                Assert.That(history.Tips,                  Has.Count.EqualTo(2));
                Assert.That(loaded!.Tips,                  Has.Count.EqualTo(1));
                Assert.That(loaded.Head.Commit.IsMerge,    Is.True);
                Assert.That(store.Load()!.Entries.Last().Commit!.Id,  Is.EqualTo(loaded.Head.Commit.Id), "Made again, and stored.");
            }

        }

        #endregion

        #region A_generation_written_only_partly_is_not_used()

        [Test]
        public void A_generation_written_only_partly_is_not_used()
        {

            RoamingNetworkHistory original;

            using (var store = new RoamingNetworkHistoryFileStore(directory!))
            {
                original = RoamingNetworkHistory.Start(NewNetwork(), Store: store);
                Fill(original);
                original.DetachStore();
            }

            // A crash while compacting: the new generation is there only partly, CURRENT still names the old one.
            Directory.CreateDirectory(Path.Combine(directory!, "gen-000002"));
            File.WriteAllText(Path.Combine(directory!, "gen-000002", RoamingNetworkHistoryFileStore.BaseFileName), "{ partly");

            using var store2 = new RoamingNetworkHistoryFileStore(directory!);

            Assert.That(RoamingNetworkHistory.TryLoad(store2, out var loaded, out var error), Is.True, error);
            AssertSame(loaded!, original);

            // Compacting writes that generation again, completely.
            Assert.That(loaded!.Compact(out var compactError), Is.True, compactError);
            Assert.That(File.ReadAllText(Path.Combine(directory!, RoamingNetworkHistoryFileStore.CurrentFileName)).Trim(),  Is.EqualTo("gen-000002"));
            Assert.That(Directory.Exists(Path.Combine(directory!, "gen-000001")),                                            Is.False);

            loaded.DetachStore();
            store2.Dispose();

            using var store3 = new RoamingNetworkHistoryFileStore(directory!);
            Assert.That(RoamingNetworkHistory.TryLoad(store3, out var again, out var againError), Is.True, againError);
            AssertSame(again!, original);

        }

        #endregion

    }

}
