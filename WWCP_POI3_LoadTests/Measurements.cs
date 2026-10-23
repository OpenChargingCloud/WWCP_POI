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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.loadtests
{

    /// <summary>
    /// What the process has used up to a moment: wall clock, CPU time of all its threads, bytes allocated, garbage
    /// collections and their pauses. The difference of two marks is what happened in between, in the whole process.
    /// </summary>
    internal readonly struct ResourceMark
    {

        private readonly Int64     timestamp;
        private readonly TimeSpan  cpu;
        private readonly Int64     allocated;
        private readonly Int32     gen0;
        private readonly Int32     gen1;
        private readonly Int32     gen2;
        private readonly TimeSpan  pause;

        private ResourceMark(Int64 Timestamp, TimeSpan CPU, Int64 Allocated, Int32 Gen0, Int32 Gen1, Int32 Gen2, TimeSpan Pause)
        {
            timestamp  = Timestamp;
            cpu        = CPU;
            allocated  = Allocated;
            gen0       = Gen0;
            gen1       = Gen1;
            gen2       = Gen2;
            pause      = Pause;
        }

        public static ResourceMark Now()

            => new (Stopwatch.GetTimestamp(),
                    Environment.CpuUsage.TotalTime,
                    GC.GetTotalAllocatedBytes(false),
                    GC.CollectionCount(0),
                    GC.CollectionCount(1),
                    GC.CollectionCount(2),
                    GC.GetTotalPauseDuration());

        /// <summary>
        /// What was used since this mark.
        /// </summary>
        public ResourceUse Elapsed()
        {

            var now = Now();

            return new (Stopwatch.GetElapsedTime(timestamp, now.timestamp).TotalMilliseconds,
                        (now.cpu - cpu).TotalMilliseconds,
                        now.allocated - allocated,
                        now.gen0 - gen0,
                        now.gen1 - gen1,
                        now.gen2 - gen2,
                        (now.pause - pause).TotalMilliseconds);

        }

    }


    /// <summary>
    /// What the process used between two marks. A collection of a generation counts for the younger ones as well.
    /// </summary>
    internal sealed record ResourceUse(Double  WallMs,
                                       Double  CpuMs,
                                       Int64   AllocatedBytes,
                                       Int32   Gen0,
                                       Int32   Gen1,
                                       Int32   Gen2,
                                       Double  GCPauseMs)
    {

        /// <summary>
        /// The share of all logical processors the process kept busy, 1 for all of them.
        /// </summary>
        public Double CpuUtilization
            => WallMs > 0 ? CpuMs / WallMs / Environment.ProcessorCount : 0;

        public JObject ToJSON()

            => new (new JProperty("wallMs",          Math.Round(WallMs,    3)),
                    new JProperty("cpuMs",           Math.Round(CpuMs,     3)),
                    new JProperty("cpuUtilization",  Math.Round(CpuUtilization, 4)),
                    new JProperty("allocatedBytes",  AllocatedBytes),
                    new JProperty("gen0",            Gen0),
                    new JProperty("gen1",            Gen1),
                    new JProperty("gen2",            Gen2),
                    new JProperty("gcPauseMs",       Math.Round(GCPauseMs, 3)));

    }


    /// <summary>
    /// The memory of the process: what the managed heap holds, what the GC committed, and what the process has in RAM.
    /// </summary>
    internal sealed record MemorySnapshot(Int64  ManagedBytes,
                                          Int64  HeapBytes,
                                          Int64  CommittedBytes,
                                          Int64  WorkingSetBytes,
                                          Int64  PrivateBytes)
    {

        /// <summary>
        /// The memory after a full, compacting collection: what is still reachable, without what only waits to be
        /// collected. It costs a collection of the whole heap, and is taken outside of what is timed.
        /// </summary>
        public static MemorySnapshot AfterFullGC()
        {

            GC.Collect(2, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, true, true);

            return Current();

        }

        public static MemorySnapshot Current()
        {

            using var process = Process.GetCurrentProcess();

            var info = GC.GetGCMemoryInfo();

            return new (GC.GetTotalMemory(false),
                        info.HeapSizeBytes,
                        info.TotalCommittedBytes,
                        process.WorkingSet64,
                        process.PrivateMemorySize64);

        }

        public JObject ToJSON()

            => new (new JProperty("managedBytes",     ManagedBytes),
                    new JProperty("heapBytes",        HeapBytes),
                    new JProperty("committedBytes",   CommittedBytes),
                    new JProperty("workingSetBytes",  WorkingSetBytes),
                    new JProperty("privateBytes",     PrivateBytes));

    }


    /// <summary>
    /// The highest working set and managed heap of the process within a phase, sampled every 50 ms by a thread of its
    /// own: a peak between two samples is missed.
    /// </summary>
    internal sealed class MemorySampler : IDisposable
    {

        private readonly Thread  thread;
        private volatile Boolean stopped;
        private Int64            peakWorkingSet;
        private Int64            peakManaged;

        public MemorySampler()
        {

            thread = new Thread(() => {
                         while (!stopped)
                         {
                             Sample();
                             Thread.Sleep(50);
                         }
                     }) {
                         IsBackground  = true,
                         Name          = "memory sampler",
                         Priority      = ThreadPriority.AboveNormal
                     };

            thread.Start();

        }

        private void Sample()
        {

            var workingSet  = Environment.WorkingSet;
            var managed     = GC.GetTotalMemory(false);

            InterlockedMax(ref peakWorkingSet, workingSet);
            InterlockedMax(ref peakManaged,    managed);

        }

        private static void InterlockedMax(ref Int64 Peak, Int64 Value)
        {
            var current = Interlocked.Read(ref Peak);
            while (Value > current && Interlocked.CompareExchange(ref Peak, Value, current) != current)
                current = Interlocked.Read(ref Peak);
        }

        /// <summary>
        /// Start a new phase: the peaks from now on.
        /// </summary>
        public void Begin()
        {
            Interlocked.Exchange(ref peakWorkingSet, 0);
            Interlocked.Exchange(ref peakManaged,    0);
            Sample();
        }

        /// <summary>
        /// The peaks since Begin(), sampled once more now.
        /// </summary>
        public (Int64 WorkingSet, Int64 Managed) Peaks()
        {
            Sample();
            return (Interlocked.Read(ref peakWorkingSet), Interlocked.Read(ref peakManaged));
        }

        public void Dispose()
        {
            stopped = true;
            thread.Join();
        }

    }



    /// <summary>
    /// Durations of one kind of operation, in milliseconds. One instance belongs to one thread; Add(Other) collects
    /// those of several afterwards. Count, sum, minimum, maximum and the slow ones are exact; the percentiles come from
    /// a random sample of at most 100,000 durations, so that a million fast reads a second take no memory worth
    /// mentioning from what is measured.
    /// </summary>
    internal sealed class Latencies
    {

        private const    Int32         SampleSize    = 100_000;

        private readonly List<Double>  sample        = [];
        private readonly Random        random        = new (17);
        private          Boolean       sorted;
        private          Int64         count;
        private          Double        sum;
        private          Double        min           = Double.MaxValue;
        private          Double        max;
        private          Int64         slowCount;
        private          Double        slowSum;

        public Int64 Count
            => count;

        public Double Sum
            => sum;

        public void Add(Double Milliseconds)
        {

            count++;
            sum += Milliseconds;
            min  = Math.Min(min, Milliseconds);
            max  = Math.Max(max, Milliseconds);

            if (Milliseconds > 10)
            {
                slowCount++;
                slowSum += Milliseconds;
            }

            // Reservoir sampling: every duration has the same chance to be in the sample.
            if (sample.Count < SampleSize)
                sample.Add(Milliseconds);

            else if (random.NextInt64(count) is var index && index < SampleSize)
                sample[(Int32) index] = Milliseconds;

            sorted = false;

        }

        public void Add(TimeSpan Duration)
            => Add(Duration.TotalMilliseconds);

        /// <summary>
        /// Those of another thread: the samples of both, thinned to the size of a sample by their counts.
        /// </summary>
        public void Add(Latencies Other)
        {

            if (Other.count == 0)
                return;

            var total   = count + Other.count;
            var mine    = sample.Count       == 0 ? [] : sample.      OrderBy(_ => random.Next()).Take((Int32) Math.Min(sample.Count,       Math.Ceiling(SampleSize * (Double) count       / total))).ToArray();
            var theirs  = Other.sample.Count == 0 ? [] : Other.sample.OrderBy(_ => random.Next()).Take((Int32) Math.Min(Other.sample.Count, Math.Ceiling(SampleSize * (Double) Other.count / total))).ToArray();

            sample.Clear();
            sample.AddRange(mine);
            sample.AddRange(theirs);

            count      = total;
            sum       += Other.sum;
            min        = Math.Min(min, Other.min);
            max        = Math.Max(max, Other.max);
            slowCount += Other.slowCount;
            slowSum   += Other.slowSum;
            sorted     = false;

        }

        /// <summary>
        /// The duration below which the given share of the operations took, the nearest rank in the sample; 0 is the
        /// minimum, 1 the maximum.
        /// </summary>
        public Double Percentile(Double Share)
        {

            if (count == 0)
                return 0;

            if (Share <= 0) return min;
            if (Share >= 1) return max;

            if (!sorted)
            {
                sample.Sort();
                sorted = true;
            }

            var rank = (Int32) Math.Ceiling(Share * sample.Count);

            return sample[Math.Clamp(rank - 1, 0, sample.Count - 1)];

        }

        public JObject ToJSON()

            => new (new JProperty("count",          count),
                    new JProperty("meanMs",         Math.Round(count > 0 ? sum / count : 0, 3)),
                    new JProperty("minMs",          Math.Round(Percentile(0),     3)),
                    new JProperty("p50Ms",          Math.Round(Percentile(0.50),  3)),
                    new JProperty("p90Ms",          Math.Round(Percentile(0.90),  3)),
                    new JProperty("p99Ms",          Math.Round(Percentile(0.99),  3)),
                    new JProperty("p999Ms",         Math.Round(Percentile(0.999), 3)),
                    new JProperty("maxMs",          Math.Round(Percentile(1),     3)),

                    // Few slow ones among many fast ones, e.g. reads waiting behind a commit: how often, how long in all.
                    new JProperty("over10msCount",  slowCount),
                    new JProperty("over10msSumMs",  Math.Round(slowSum, 3)));

    }

}
