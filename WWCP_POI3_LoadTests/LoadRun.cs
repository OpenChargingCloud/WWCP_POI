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

using System.Diagnostics;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.loadtests
{

    /// <summary>
    /// One size of a load test with one store, in a process of its own: the roaming network made and its history started,
    /// commits one after another, then writers, readers and status updates at the same time, one level of writers after
    /// another, and, with a file store, its compaction. Nothing is timed while the memory is measured.
    /// </summary>
    internal sealed class LoadRun : IDisposable
    {

        #region Data

        private readonly LoadTestOptions                    options;
        private readonly Int32                              stations;
        private readonly LoadTestStore                      store;
        private readonly LoadNetwork                        network;
        private readonly MemorySampler                      sampler      = new ();
        private          RoamingNetworkHistory              history      = null!;
        private          RoamingNetworkHistoryFileStore?    fileStore;
        private          RoamingNetworkHistoryFileArchive?  fileArchive;
        private          Int64                              merges;
        private          Double                             mergeMs;
        private volatile Boolean                            stopRequested;

        #endregion

        #region Constructor(s)

        public LoadRun(LoadTestOptions Options)
        {

            this.options   = Options;
            this.stations  = Options.Stations[0];
            this.store     = Options.Stores[0];
            this.network   = new LoadNetwork(Options, stations);

        }

        #endregion


        #region Run(Checkpoint)

        /// <summary>
        /// All phases, the result after each given to the checkpoint: what was measured stays, should a later phase
        /// end the process, e.g. out of memory.
        /// </summary>
        public JObject Run(Action<JObject> Checkpoint)
        {

            var largest  = network.Operators[0];
            var phases   = new JObject();
            var result   = new JObject(new JProperty("stations",  stations),
                                       new JProperty("store",     Name(store)),
                                       new JProperty("network",   new JObject(new JProperty("operators",            network.Operators.Count),
                                                                              new JProperty("largestOperator",      largest.InitialStations),
                                                                              new JProperty("smallestOperator",     network.Operators[^1].InitialStations),
                                                                              new JProperty("pools",                network.Operators.Sum(op => op.InitialPools)),
                                                                              new JProperty("stations",             stations),
                                                                              new JProperty("evses",                (Int64) stations * options.EVSEsPerStation),
                                                                              new JProperty("stationsPerPool",      options.StationsPerPool),
                                                                              new JProperty("evsesPerStation",      options.EVSEsPerStation),
                                                                              new JProperty("zipf",                 options.Zipf),
                                                                              new JProperty("templates",            options.Templates))),
                                       new JProperty("phases",    phases));

            Console.WriteLine();
            Console.WriteLine($"{stations:N0} charging stations, {(Int64) stations * options.EVSEsPerStation:N0} EVSEs of {network.Operators.Count} operators (the largest {largest.InitialStations:N0}), {Name(store)} store");

            ChargingStationOperator[]? operators       = null;
            RoamingNetwork?            roamingNetwork  = null;

            phases["entities"]    = Phase(sampler, "entities made",         () => operators       = [ .. network.Operators.Select(network.BuildOperator) ]);
            phases["network"]     = Phase(sampler, "roaming network made",  () => roamingNetwork  = network.BuildNetwork(operators!));
            operators             = null;
            Checkpoint(result);

            phases["start"]       = Phase(sampler, "history started",       () => history         = Start(roamingNetwork!));
            roamingNetwork        = null;

            if (fileStore is not null)
                phases["start"]!["disk"] = Disk(fileStore.Directory);

            phases["firstProof"]  = Proofs(history, largest.StationId(1));
            Checkpoint(result);

            phases["sequential"]  = Sequential();
            Checkpoint(result);

            phases["merge"]       = Merges();
            Checkpoint(result);

            var concurrent        = new JArray();
            phases["concurrent"]  = concurrent;

            foreach (var writers in options.Writers)
            {
                concurrent.Add(Concurrent(writers));
                Checkpoint(result);
            }

            if (fileStore is not null)
                phases["compact"] = Compact();

            result["head"]                 = Head(history);
            result["memoryAtEnd"]          = MemorySnapshot.AfterFullGC().ToJSON();
            result["peakWorkingSetBytes"]  = PeakWorkingSet();

            return result;

        }

        #endregion

        #region (static) Reload(Options)

        /// <summary>
        /// Read the file store in the directory of the options again, as the hub does when it starts, in a process of its
        /// own: how long it takes, the memory afterwards, and the first proof, which makes the tree of the head.
        /// </summary>
        public static JObject Reload(LoadTestOptions Options)
        {

            using var sampler = new MemorySampler();

            var directory = Options.Reload!;
            var retention = new RoamingNetworkHistoryRetention {
                                RecentVersions             = Options.RecentVersions,
                                VersionCheckpointInterval  = Options.VersionInterval
                            };

            RoamingNetworkHistoryFileStore?    store    = null;
            RoamingNetworkHistoryFileArchive?  archive  = null;
            RoamingNetworkHistory?             history  = null;
            String?                            error    = null;

            Console.WriteLine($"  read again from {directory}");

            try
            {

                var load = Phase(sampler, "history read again", () => {
                               store    = new RoamingNetworkHistoryFileStore(directory);
                               archive  = new RoamingNetworkHistoryFileArchive(Path.Combine(directory, "archive"));
                               RoamingNetworkHistory.TryLoad(store, out history, out error, retention, Archive: archive);
                           });

                var result = new JObject(new JProperty("load", load));

                if (history is null)
                {
                    result["error"] = error;
                    return result;
                }

                var first = history.Head.RoamingNetwork.ChargingStationOperators.
                                    SelectMany(op      => op.  ChargingPools).
                                    SelectMany(pool    => pool.ChargingStations).
                                    First().Id;

                result["head"]                 = Head(history);
                result["firstProof"]           = Proofs(history, first);
                result["peakWorkingSetBytes"]  = PeakWorkingSet();

                return result;

            }
            finally
            {
                store?.  Dispose();
                archive?.Dispose();
            }

        }

        #endregion


        #region (private) Start(RoamingNetwork)

        /// <summary>
        /// The history of the given roaming network, as the hub starts it: its commits signed by the roaming network
        /// operator, every head attested, the retention of the options, and the file store with its archive.
        /// </summary>
        private RoamingNetworkHistory Start(RoamingNetwork RoamingNetwork)
        {

            if (store == LoadTestStore.File)
            {

                var directory = options.StoreDirectory ?? throw new InvalidOperationException("A file store needs --store-directory!");

                if (Directory.Exists(directory))
                    throw new InvalidOperationException($"The directory '{directory}' is there already!");

                fileStore    = new RoamingNetworkHistoryFileStore  (directory);
                fileArchive  = new RoamingNetworkHistoryFileArchive(Path.Combine(directory, "archive"));

            }

            var started = RoamingNetworkHistory.Start(RoamingNetwork,
                                                      Signers:    [ network.CommitSigner ],
                                                      Store:      fileStore,
                                                      Retention:  new RoamingNetworkHistoryRetention {
                                                                      RecentVersions             = options.RecentVersions,
                                                                      VersionCheckpointInterval  = options.VersionInterval
                                                                  },
                                                      Archive:    fileArchive);

            if (options.Attest)
            {
                started.AttestationSigners = [ network.CommitSigner ];
                started.RefreshAttestations();
            }

            started.OnCommitAnnouncement += announcement => {
                if (announcement.Commit.IsMerge)
                    Interlocked.Increment(ref merges);
            };

            return started;

        }

        #endregion

        #region (private) Sequential()

        /// <summary>
        /// Commits one after another, each of one operator picked by its size: change sets prepared for the head, signed
        /// by their owner, and committed.
        /// </summary>
        private JObject Sequential()
        {

            Console.WriteLine($"    {options.Warmup} + {options.Commits} commits of {options.ChangesPerCommit} change(s) one after another");

            var random     = new Random(options.Seed);
            var prepare    = new Latencies();
            var commit     = new Latencies();
            var cpu        = new Latencies();
            var outcomes   = new Dictionary<String, Int32>();
            var errors     = new List<String>();
            var allocated  = 0L;
            var gen2       = 0;
            var pause      = 0.0;
            var mark       = ResourceMark.Now();
            var merged     = 0L;

            for (var i = 0; i < options.Warmup + options.Commits; i++)
            {

                if (i == options.Warmup)
                {
                    sampler.Begin();
                    mark    = ResourceMark.Now();
                    merged  = Interlocked.Read(ref merges);
                }

                var op         = network.Pick(random);
                var plan       = network.Plan(op, random, options.ChangesPerCommit);

                var started    = Stopwatch.GetTimestamp();
                var changeSet  = op.Sign(history.Head.RoamingNetwork.CreateChangeSet(plan.Changes));
                var prepared   = Stopwatch.GetElapsedTime(started);

                var before     = ResourceMark.Now();
                var outcome    = history.Commit(changeSet);
                var use        = before.Elapsed();

                if (outcome.IsCommitted)
                    plan.Apply();

                if (i < options.Warmup)
                    continue;

                prepare.Add(prepared);
                commit. Add(use.WallMs);
                cpu.    Add(use.CpuMs);
                allocated += use.AllocatedBytes;
                gen2      += use.Gen2;
                pause     += use.GCPauseMs;

                outcomes[outcome.Status.ToString()] = outcomes.GetValueOrDefault(outcome.Status.ToString()) + 1;

                if (outcome.ErrorResponse is String error && errors.Count < 5 && !errors.Contains(error))
                    errors.Add(error);

            }

            var total           = mark.Elapsed();
            var (peakWS, peakM) = sampler.Peaks();
            var memory          = MemorySnapshot.AfterFullGC();

            Console.WriteLine($"      commit p50 {commit.Percentile(0.5),8:F1} ms, p99 {commit.Percentile(0.99),8:F1} ms, max {commit.Percentile(1),8:F1} ms; " +
                              $"CPU {cpu.Percentile(0.5),8:F1} ms, {Bytes(allocated / Math.Max(1, options.Commits))} allocated per commit; {Bytes(memory.ManagedBytes)} managed afterwards" +
                              (errors.Count > 0 ? $"; {outcomes.GetValueOrDefault(nameof(RoamingNetworkCommitStatus.Refused))} refused: {errors[0]}" : ""));

            return new JObject(new JProperty("commits",                  options.Commits),
                               new JProperty("changesPerCommit",         options.ChangesPerCommit),
                               new JProperty("outcomes",                 JObject.FromObject(outcomes)),
                               new JProperty("commitsPerSecond",         Math.Round(options.Commits / (total.WallMs / 1000), 3)),
                               new JProperty("merges",                   Interlocked.Read(ref merges) - merged),
                               new JProperty("prepare",                  prepare.ToJSON()),
                               new JProperty("commit",                   commit. ToJSON()),
                               new JProperty("cpuPerCommit",             cpu.    ToJSON()),
                               new JProperty("allocatedBytesPerCommit",  allocated / Math.Max(1, options.Commits)),
                               new JProperty("gen2",                     gen2),
                               new JProperty("gcPauseMs",                Math.Round(pause, 3)),
                               new JProperty("resources",                total.ToJSON()),
                               new JProperty("peakWorkingSetBytes",      peakWS),
                               new JProperty("peakManagedBytes",         peakM),
                               new JProperty("memoryAfter",              memory.ToJSON()),
                               new JProperty("errors",                   new JArray(errors)));

        }

        #endregion

        #region (private) Merges()

        /// <summary>
        /// Commits made for the version before the head: two change sets of two operators prepared for the same head,
        /// the first committed, then the second, which starts a branch and is merged with the head. What the second
        /// costs is what every commit costs whose writer was overtaken by another one.
        /// </summary>
        private JObject Merges()
        {

            Console.WriteLine($"    {options.Merges} commits for the version before the head, each merged");

            var random     = new Random(HashCode.Combine(options.Seed, "merges"));
            var commit     = new Latencies();
            var cpu        = new Latencies();
            var outcomes   = new Dictionary<String, Int32>();
            var errors     = new List<String>();
            var allocated  = 0L;
            var merged     = 0L;

            sampler.Begin();

            for (var i = 0; i < options.Merges; i++)
            {

                var first   = network.Pick(random);
                var second  = network.Pick(random);

                for (var tries = 0; second == first && tries < 100; tries++)
                    second = network.Pick(random);

                var head    = history.Head.RoamingNetwork;
                var planA   = network.Plan(first,  random, options.ChangesPerCommit);
                var planB   = network.Plan(second, random, options.ChangesPerCommit);
                var a       = first. Sign(head.CreateChangeSet(planA.Changes));
                var b       = second.Sign(head.CreateChangeSet(planB.Changes));

                if (history.Commit(a).IsCommitted)
                    planA.Apply();

                var before  = Interlocked.Read(ref merges);
                var mark    = ResourceMark.Now();
                var outcome = history.Commit(b);
                var use     = mark.Elapsed();

                if (outcome.IsCommitted)
                    planB.Apply();

                commit.Add(use.WallMs);
                cpu.   Add(use.CpuMs);
                allocated += use.AllocatedBytes;
                merged    += Interlocked.Read(ref merges) - before;

                outcomes[outcome.Status.ToString()] = outcomes.GetValueOrDefault(outcome.Status.ToString()) + 1;

                if (outcome.ErrorResponse is String error && errors.Count < 5 && !errors.Contains(error))
                    errors.Add(error);

                if (use.WallMs > 10_000)
                    Console.WriteLine($"      merge {i + 1}: {use.WallMs / 1000,8:F1} s, CPU {use.CpuMs / 1000,8:F1} s, {Bytes(use.AllocatedBytes)} allocated");

                if (use.WallMs > options.MaxMerge.TotalMilliseconds && i + 1 < options.Merges)
                {
                    Console.WriteLine($"      the other merges skipped: this one took more than --max-merge {options.MaxMerge.TotalSeconds:F0} s");
                    break;
                }

            }

            var (peakWS, peakM) = sampler.Peaks();
            var memory          = MemorySnapshot.AfterFullGC();

            mergeMs = commit.Percentile(0.5);

            if (commit.Count > 0)
                Console.WriteLine($"      commit with merge p50 {commit.Percentile(0.5),8:F1} ms, max {commit.Percentile(1),8:F1} ms; " +
                                  $"CPU {cpu.Percentile(0.5),8:F1} ms, {Bytes(allocated / commit.Count)} allocated per commit; {merged} merge commits" +
                                  (errors.Count > 0 ? $"; {errors[0]}" : ""));

            return new JObject(new JProperty("commits",                  commit.Count),
                               new JProperty("mergeCommits",             merged),
                               new JProperty("outcomes",                 JObject.FromObject(outcomes)),
                               new JProperty("commit",                   commit.ToJSON()),
                               new JProperty("cpuPerCommit",             cpu.   ToJSON()),
                               new JProperty("allocatedBytesPerCommit",  allocated / Math.Max(1, commit.Count)),
                               new JProperty("peakWorkingSetBytes",      peakWS),
                               new JProperty("peakManagedBytes",         peakM),
                               new JProperty("memoryAfter",              memory.ToJSON()),
                               new JProperty("errors",                   new JArray(errors)));

        }

        #endregion

        #region (private) Concurrent(Writers)

        /// <summary>
        /// The given number of writers, each changing the charging stations of its own operators, the readers and the
        /// status updates of the options, all at the same time for the duration of the options. Writers prepare their
        /// change sets for the head they see; one committed meanwhile makes theirs a branch, merged with the head.
        /// </summary>
        private JObject Concurrent(Int32 Writers)
        {

            var writers   = Math.Min(Writers, network.Operators.Count);
            var readers   = new JObject(options.Readers.Select(reader => new JProperty(Name(reader.Key), reader.Value)));
            var gate      = new ManualResetEventSlim(false);
            var threads   = new List<Thread>();
            var written   = new WriterResults[writers];
            var read      = new List<(LoadTestRead Kind, ReaderResults Results)>();
            var updated   = options.StatusRate > 0 ? new StatusResults() : null;

            Console.WriteLine($"    {writers} writer(s), readers {String.Join(", ", options.Readers.Select(reader => $"{Name(reader.Key)} {reader.Value}"))}, {options.StatusRate} status updates/s for {options.Duration.TotalSeconds:F0} s");

            if (writers > 1 && mergeMs > options.MaxMerge.TotalMilliseconds)
            {

                var skipped = $"skipped: a commit with a merge took {mergeMs / 1000:F1} s, more than --max-merge {options.MaxMerge.TotalSeconds:F0} s";

                Console.WriteLine($"      {skipped}");

                return new JObject(new JProperty("writers",  writers),
                                   new JProperty("readers",  readers),
                                   new JProperty("skipped",  skipped));

            }

            stopRequested = false;

            for (var w = 0; w < writers; w++)
            {
                var own      = network.Operators.Where((op, index) => index % writers == w).ToArray();
                var results  = written[w] = new WriterResults();
                var random   = new Random(HashCode.Combine(options.Seed, Writers, 1, w));
                threads.Add(new Thread(() => Write(own, LoadNetwork.CumulativeOf(own), random, results, gate)) { Name = $"writer {w}", IsBackground = true });
            }

            foreach (var (kind, count) in options.Readers)
            {
                for (var r = 0; r < count; r++)
                {
                    var results  = new ReaderResults();
                    var random   = new Random(HashCode.Combine(options.Seed, Writers, 2, kind, r));
                    read.Add((kind, results));
                    threads.Add(new Thread(() => Read(kind, random, results, gate)) { Name = $"{Name(kind)} reader {r}", IsBackground = true });
                }
            }

            if (updated is not null)
            {
                var random   = new Random(HashCode.Combine(options.Seed, Writers, 3));
                threads.Add(new Thread(() => UpdateStatus(random, updated, gate)) { Name = "status", IsBackground = true });
            }

            foreach (var thread in threads)
                thread.Start();

            var merged    = Interlocked.Read(ref merges);
            var revision  = history.Head.Commit.Revision;

            sampler.Begin();
            var mark      = ResourceMark.Now();

            gate.Set();
            Thread.Sleep(options.Duration);
            stopRequested = true;

            foreach (var thread in threads)
                thread.Join();

            var use              = mark.Elapsed();
            var (peakWS, peakM)  = sampler.Peaks();
            var memory           = MemorySnapshot.AfterFullGC();
            var seconds          = use.WallMs / 1000;

            var prepare          = new Latencies();
            var commit           = new Latencies();
            var outcomes         = new Dictionary<String, Int32>();
            var errors           = new List<String>();

            foreach (var results in written)
            {

                prepare.Add(results.Prepare);
                commit. Add(results.Commit);

                foreach (var (outcome, count) in results.Outcomes)
                    outcomes[outcome] = outcomes.GetValueOrDefault(outcome) + count;

                foreach (var error in results.Errors)
                    if (errors.Count < 5 && !errors.Contains(error))
                        errors.Add(error);

            }

            var reads = new JObject();

            foreach (var kind in Enum.GetValues<LoadTestRead>())
            {

                var latencies  = new Latencies();
                var notFound   = 0;

                foreach (var (_, results) in read.Where(reader => reader.Kind == kind))
                {
                    latencies.Add(results.Latencies);
                    notFound += results.NotFound;
                }

                foreach (var error in read.SelectMany(reader => reader.Results.Errors))
                    if (errors.Count < 5 && !errors.Contains(error))
                        errors.Add(error);

                var json = latencies.ToJSON();
                json["perSecond"]  = Math.Round(latencies.Count / seconds, 3);
                json["notFound"]   = notFound;
                reads[Name(kind)]  = json;

            }

            var committed  = outcomes.GetValueOrDefault(nameof(RoamingNetworkCommitStatus.Committed));
            var mergesNow  = Interlocked.Read(ref merges) - merged;
            var status     = updated?.Latencies.ToJSON();

            // Refused are mostly the status updates of EVSEs removed meanwhile: their errors stay with them.
            if (status is not null)
            {
                status["perSecond"] = Math.Round(updated!.Latencies.Count / seconds, 3);
                status["outcomes"]  = JObject.FromObject(updated.Outcomes);
                status["errors"]    = new JArray(updated.Errors);
            }

            Console.WriteLine($"      {committed / seconds,7:F2} commits/s ({committed} committed, {mergesNow} merges, {outcomes.GetValueOrDefault(nameof(RoamingNetworkCommitStatus.Refused))} refused), " +
                              $"commit p50 {commit.Percentile(0.5),8:F1} ms, p99 {commit.Percentile(0.99),8:F1} ms; " +
                              $"reads/s {reads.Properties().Sum(property => property.Value["perSecond"]!.Value<Double>()),9:F0}, " +
                              $"EVSE p99 {reads[Name(LoadTestRead.EVSE)]?["p99Ms"]?.Value<Double>() ?? 0,6:F2} ms, proof p99 {reads[Name(LoadTestRead.Proof)]?["p99Ms"]?.Value<Double>() ?? 0,8:F1} ms, " +
                              $"commit read max {reads[Name(LoadTestRead.Commit)]?["maxMs"]?.Value<Double>() ?? 0,8:F1} ms; " +
                              $"status/s {status?["perSecond"]?.Value<Double>() ?? 0,6:F0}; CPU {use.CpuUtilization * 100,5:F1} %" +
                              (errors.Count > 0 ? $"; {errors[0]}" : ""));

            return new JObject(new JProperty("writers",              writers),
                               new JProperty("readers",              readers),
                               new JProperty("statusRate",           options.StatusRate),
                               new JProperty("changesPerCommit",     options.ChangesPerCommit),
                               new JProperty("commits",              new JObject(new JProperty("outcomes",   JObject.FromObject(outcomes)),
                                                                                 new JProperty("perSecond",  Math.Round(committed / seconds, 3)),
                                                                                 new JProperty("merges",     mergesNow),
                                                                                 new JProperty("revisions",  history.Head.Commit.Revision - revision))),
                               new JProperty("prepare",              prepare.ToJSON()),
                               new JProperty("commit",               commit. ToJSON()),
                               new JProperty("reads",                reads),
                               new JProperty("status",               status),
                               new JProperty("resources",            use.ToJSON()),
                               new JProperty("peakWorkingSetBytes",  peakWS),
                               new JProperty("peakManagedBytes",     peakM),
                               new JProperty("memoryAfter",          memory.ToJSON()),
                               new JProperty("errors",               new JArray(errors)));

        }

        #endregion

        #region (private) Write / Read / UpdateStatus

        private sealed class WriterResults
        {
            public readonly Latencies                    Prepare   = new ();
            public readonly Latencies                    Commit    = new ();
            public readonly Dictionary<String, Int32>    Outcomes  = [];
            public readonly List<String>                 Errors    = [];
        }

        private sealed class ReaderResults
        {
            public readonly Latencies                    Latencies = new ();
            public          Int32                        NotFound;
            public readonly List<String>                 Errors    = [];
        }

        private sealed class StatusResults
        {
            public readonly Latencies                    Latencies = new ();
            public readonly Dictionary<String, Int32>    Outcomes  = [];
            public readonly List<String>                 Errors    = [];
        }

        /// <summary>
        /// A writer: change sets of its own operators, each prepared for the head it sees, signed by its owner, and
        /// committed, one after another.
        /// </summary>
        private void Write(LoadOperator[]        Operators,
                           Int64[]               Cumulative,
                           Random                Random,
                           WriterResults         Results,
                           ManualResetEventSlim  Gate)
        {

            Gate.Wait();

            try
            {
                while (!stopRequested)
                {

                    var op         = LoadNetwork.Pick(Operators, Cumulative, Random);
                    var plan       = network.Plan(op, Random, options.ChangesPerCommit);

                    var started    = Stopwatch.GetTimestamp();
                    var changeSet  = op.Sign(history.Head.RoamingNetwork.CreateChangeSet(plan.Changes));
                    var prepared   = Stopwatch.GetTimestamp();
                    var outcome    = history.Commit(changeSet);

                    Results.Prepare.Add(Stopwatch.GetElapsedTime(started, prepared));
                    Results.Commit. Add(Stopwatch.GetElapsedTime(prepared));
                    Results.Outcomes[outcome.Status.ToString()] = Results.Outcomes.GetValueOrDefault(outcome.Status.ToString()) + 1;

                    if (outcome.IsCommitted)
                        plan.Apply();

                    else if (outcome.ErrorResponse is String error && Results.Errors.Count < 5 && !Results.Errors.Contains(error))
                        Results.Errors.Add(error);

                }
            }
            catch (Exception e)
            {
                Results.Errors.Add($"{Thread.CurrentThread.Name}: {e.GetType().Name}: {e.Message}");
            }

        }

        /// <summary>
        /// A reader of one kind: an EVSE of the head or the proof of a charging station, of an operator picked by its size,
        /// both as JSON, as the API of the hub serves them. A charging station removed meanwhile is not found.
        /// </summary>
        private void Read(LoadTestRead          Kind,
                          Random                Random,
                          ReaderResults         Results,
                          ManualResetEventSlim  Gate)
        {

            Gate.Wait();

            try
            {
                while (!stopRequested)
                {

                    var op         = network.Pick(Random);
                    var stationNo  = 1 + Random.Next(op.InitialStations);
                    var started    = Stopwatch.GetTimestamp();
                    var found      = false;

                    switch (Kind)
                    {

                        case LoadTestRead.EVSE:
                            if (history.Head.RoamingNetwork.GetChargingStationOperatorById(op.Id) is ChargingStationOperator chargingStationOperator &&
                                chargingStationOperator.TryGetEVSE(op.EVSEId(stationNo, 1 + Random.Next(options.EVSEsPerStation)), out var evse))
                            {
                                _ = evse.ToJSON();
                                found = true;
                            }
                            break;

                        case LoadTestRead.Proof:
                            if (history.TryGetChargingStationProof(op.StationId(stationNo), out var proof, out _))
                            {
                                _ = proof.ToJSON();
                                found = true;
                            }
                            break;

                        case LoadTestRead.Commit:
                            found = history.TryGetCommit(history.Head.Commit.Id, out _);
                            break;

                    }

                    Results.Latencies.Add(Stopwatch.GetElapsedTime(started));

                    if (!found)
                        Results.NotFound++;

                }
            }
            catch (Exception e)
            {
                Results.Errors.Add($"{Thread.CurrentThread.Name}: {e.GetType().Name}: {e.Message}");
            }

        }

        /// <summary>
        /// The status updates of EVSEs, at the rate of the options: available and charging, by turns.
        /// </summary>
        private void UpdateStatus(Random                Random,
                                  StatusResults         Results,
                                  ManualResetEventSlim  Gate)
        {

            var interval = (Double) Stopwatch.Frequency / options.StatusRate;

            Gate.Wait();

            var start = Stopwatch.GetTimestamp();
            var count = 0L;

            try
            {
                while (!stopRequested)
                {

                    var now  = Stopwatch.GetTimestamp();
                    var due  = start + (Int64) (count * interval);

                    if (now < due)
                    {
                        if ((due - now) * 1000 / Stopwatch.Frequency >= 2)
                            Thread.Sleep(1);
                        else
                            Thread.SpinWait(100);
                        continue;
                    }

                    // More than a second behind: no burst to catch up.
                    if (now - due > Stopwatch.Frequency)
                        start = now - (Int64) (count * interval);

                    count++;

                    var op       = network.Pick(Random);
                    var evseId   = op.EVSEId(1 + Random.Next(op.InitialStations), 1 + Random.Next(options.EVSEsPerStation));
                    var started  = Stopwatch.GetTimestamp();
                    var outcome  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,
                                                                   nameof(EVSE),
                                                                   evseId.ToString(),
                                                                   count % 2 == 0 ? "available" : "charging",
                                                                   Timestamp.Now,
                                                                   out _,
                                                                   out var error);

                    Results.Latencies.Add(Stopwatch.GetElapsedTime(started));
                    Results.Outcomes[outcome.ToString()] = Results.Outcomes.GetValueOrDefault(outcome.ToString()) + 1;

                    if (outcome == POIStatusUpdateResult.Refused && error is not null && Results.Errors.Count < 5 && !Results.Errors.Contains(error))
                        Results.Errors.Add(error);

                }
            }
            catch (Exception e)
            {
                Results.Errors.Add($"{Thread.CurrentThread.Name}: {e.GetType().Name}: {e.Message}");
            }

        }

        #endregion

        #region (private) Compact()

        /// <summary>
        /// The file store compacted, as the history does once its log is long: a new generation written completely.
        /// </summary>
        private JObject Compact()
        {

            var    before  = Disk(fileStore!.Directory);
            var    ok      = false;
            String? error  = null;

            var json = Phase(sampler, "store compacted", () => ok = history.Compact(out error));

            json["ok"]          = ok;
            json["error"]       = error;
            json["diskBefore"]  = before;
            json["diskAfter"]   = Disk(fileStore.Directory);

            return json;

        }

        #endregion


        #region (private, static) Phase(Sampler, Name, Action)

        /// <summary>
        /// What the given action used, its peaks, and the memory after it, which a full collection measures.
        /// </summary>
        private static JObject Phase(MemorySampler  Sampler,
                                     String         Name,
                                     Action         Action)
        {

            Console.Write($"    {Name,-24}");

            Sampler.Begin();

            var mark                = ResourceMark.Now();
            Action();
            var use                 = mark.Elapsed();
            var (peakWS, peakM)     = Sampler.Peaks();
            var memory              = MemorySnapshot.AfterFullGC();

            Console.WriteLine($"{use.WallMs / 1000,8:F1} s, CPU {use.CpuMs / 1000,8:F1} s, {Bytes(use.AllocatedBytes)} allocated; " +
                              $"{Bytes(memory.ManagedBytes)} managed afterwards, peak working set {Bytes(peakWS)}");

            var json = use.ToJSON();
            json["peakWorkingSetBytes"]  = peakWS;
            json["peakManagedBytes"]     = peakM;
            json["memoryAfter"]          = memory.ToJSON();

            return json;

        }

        #endregion

        #region (private, static) Proofs(History, ChargingStationId)

        /// <summary>
        /// The proof of the given charging station twice: the first makes the tree of the head, the second takes it.
        /// </summary>
        private static JObject Proofs(RoamingNetworkHistory  History,
                                      ChargingStation_Id     ChargingStationId)
        {

            var json = new JObject();

            foreach (var name in new[] { "coldMs", "warmMs" })
            {

                var started  = Stopwatch.GetTimestamp();
                var ok       = History.TryGetChargingStationProof(ChargingStationId, out _, out var error);
                json[name]   = Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 3);

                if (!ok)
                    json["error"] = error;

            }

            Console.WriteLine($"    {"first proof",-24}{json["coldMs"]!.Value<Double>() / 1000,8:F1} s, then {json["warmMs"]!.Value<Double>(),8:F3} ms" +
                              (json["error"] is JValue proofError ? $": {proofError}" : ""));

            return json;

        }

        #endregion

        #region (private, static) Head(History) / Disk(Directory) / PeakWorkingSet()

        private static JObject Head(RoamingNetworkHistory History)

            => new (new JProperty("revision",  History.Head.Commit.Revision),
                    new JProperty("commitId",  History.Head.Commit.Id.ToString()));

        /// <summary>
        /// The sizes of a file store: its current generation, base and log, the lines of the log, and its archive.
        /// </summary>
        private static JObject Disk(String Directory)
        {

            var generation  = File.ReadAllText(Path.Combine(Directory, "CURRENT")).Trim();
            var baseFile    = new FileInfo(Path.Combine(Directory, generation, "base.json"));
            var logFile     = new FileInfo(Path.Combine(Directory, generation, "log.jsonl"));
            var archive     = new DirectoryInfo(Path.Combine(Directory, "archive"));
            var lines       = 0L;

            if (logFile.Exists)
            {
                using var reader = new StreamReader(new FileStream(logFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                while (reader.ReadLine() is not null)
                    lines++;
            }

            return new JObject(new JProperty("generation",    generation),
                               new JProperty("baseBytes",     baseFile.Exists ? baseFile.Length : 0),
                               new JProperty("logBytes",      logFile. Exists ? logFile. Length : 0),
                               new JProperty("logLines",      lines),
                               new JProperty("archiveBytes",  archive.Exists  ? archive.EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length) : 0),
                               new JProperty("totalBytes",    new DirectoryInfo(Directory).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length)));

        }

        private static Int64 PeakWorkingSet()
        {
            using var process = Process.GetCurrentProcess();
            return process.PeakWorkingSet64;
        }

        #endregion

        #region (internal, static) Name(...) / Bytes(...)

        internal static String Name(LoadTestStore  Store)  => Store.ToString().ToLowerInvariant();
        internal static String Name(LoadTestRead   Read)   => Read. ToString().ToLowerInvariant();

        internal static String Bytes(Int64 Bytes)

            => Bytes >= 1L << 30 ? $"{Bytes / (Double) (1L << 30),7:F2} GiB"
             : Bytes >= 1L << 20 ? $"{Bytes / (Double) (1L << 20),7:F1} MiB"
             :                     $"{Bytes / (Double) (1L << 10),7:F1} KiB";

        #endregion


        public void Dispose()
        {
            fileStore?.  Dispose();
            fileArchive?.Dispose();
            sampler.     Dispose();
        }

    }

}
