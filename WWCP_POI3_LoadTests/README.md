# WWCP POI 3 load tests

A console program measuring WWCP POI 3 histories the way the hub keeps them, at sizes of real roaming networks: 10,000 to
1,000,000 charging stations, spread over charging station operators of very different sizes. It runs from a Release
build on any machine .NET 10 runs on, and writes what it measured as JSON, Markdown and CSV, together with the machine,
the runtime, its garbage collector and the commits it was built from, so that runs on different hardware can be put
side by side.

It is not an NUnit fixture: the two benchmarks in `WWCP_POI3_Tests` (`CommitCostBenchmark`, `EffectiveMembersBenchmark`)
time single commits of up to 10,000 charging stations; this program also measures memory, CPU, the file store, restarts
and concurrency, each size in a fresh process.


## Build and run

From the repository (the projects next to it, Hermod and Styx, as the RoamingHubCLI superproject checks them out):

```bash
dotnet build WWCP_POI3_LoadTests/WWCP_POI3_LoadTests.csproj -c Release
```

```bash
WWCP_POI3_LoadTests/bin/Release/net10.0/WWCP_POI3_LoadTests --suite full --label my-machine
```

For another machine without an SDK, publish it self-contained and copy the directory over:

```bash
dotnet publish WWCP_POI3_LoadTests/WWCP_POI3_LoadTests.csproj -c Release -r linux-x64 --self-contained -o publish/linux-x64
```

Other runtimes: `win-x64`, `win-arm64`, `linux-arm64`, `osx-arm64`. The program refuses nothing in a Debug build, but
its report then carries a warning, as Debug numbers are meaningless here.

`--suite quick` is 10k charging stations (a few minutes), `standard` 10k, 50k and 100k, `full` adds 250k and 1M. A full
run takes hours and, at 1M charging stations, tens of GiB of RAM; `--stations 10k,1M` picks any sizes, `-h` lists all
options.


## What it does

The parent process starts one child process for every size and store (`--stores memory,file`), so that no size inherits
the heap of another, and one more for every file store, which reads it again as a hub does when it starts. Each child
writes its result after every phase: should one end, e.g. out of memory, what it measured before stays in the report.

**The network.** `--operators 200` charging station operators; the i-th has a share of 1/i^`--zipf` (1.0) of all
charging stations, so a few large operators and many small ones: of 1M charging stations, the largest has about 170,000.
Every charging pool has `--stations-per-pool 2` charging stations with a name, an address and a location in Germany,
every charging station `--evses-per-station 2` EVSEs with one connector each; every fifth charging station is a DC one.
With `--templates`, EVSEs, charging stations and pools take properties from templates of their operator. Every operator
has its own signing and identity keys and signs its own change sets.

**The history** is started as the hub starts it: every commit signed by the roaming network operator, every new head
attested (`--no-attest` leaves that out), the retention of the hub (`--recent-versions`, `--version-interval`), and for
the file store a `RoamingNetworkHistoryFileStore` with its archive, which syncs every commit to the disk.

**Phases** of each child:

| Phase | What is measured |
| --- | --- |
| entities, network, start | making the entities, the roaming network, and the genesis commit with all its ETags; memory afterwards; for the file store the size of `base.json` |
| first proof | the proof of a charging station twice: the first builds the tree of the head |
| sequential | `--warmup` + `--commits` commits one after another, each of `--changes-per-commit` changes of one operator picked by its size, by `--mix` (EVSE property, new charging station, removed charging station, new charging pool): latencies of preparing and committing, CPU and allocations per commit, gen-2 collections, memory afterwards |
| merge | `--merges` commits made for the version before the head: two change sets of two operators prepared for the same head, the second committed after the first, so it is merged. Every commit whose writer was overtaken by another one costs this |
| concurrent | one level per `--writers` (0,1,2,4,8,16) for `--duration`: writers, each changing the charging stations of its own operators and preparing its change sets for the head it sees; at the same time reader threads (`--readers evse=3,proof=1,commit=1`) and `--status-rate` EVSE status updates per second. Levels of several writers are skipped where one merge took longer than `--max-merge` |
| compact | the file store compacted, and its size before and after |
| reload | the file store read again in a fresh process: time, memory, first proof |

The readers: **evse** reads an EVSE of the head as JSON, without a lock. **proof** makes the proof of a charging station,
which takes the attestations of the head under the lock of the history. **commit** gets the commit of the head with
`TryGetCommit`, also under that lock and nothing else, so it measures how long a reader waits behind a commit, which
holds the lock for all of its time.

**Numbers.** CPU, allocations and GC counts are those of the whole process between two moments
(`Environment.CpuUsage`, `GC.GetTotalAllocatedBytes`, `GC.GetTotalPauseDuration`); in the concurrent phase they are those
of all threads. Memory after a phase is measured after a full compacting collection, outside of what is timed; peaks
are sampled every 50 ms. Percentiles come from a random sample of at most 100,000 durations per kind; count, mean, min,
max and the durations above 10 ms are exact. CPU % is the share of all logical processors. The server GC is on, as in
the hub; `DOTNET_gcServer=0` measures the workstation GC, `DOTNET_GCHeapCount` limits its heaps, and the report lists
every `DOTNET_` variable set.


## Reports

`--output results` gets `loadtest-<label>-<time>.json` (everything), `.md` (two tables: one row per size and store, one
per level of writers), `-series.csv` and `-concurrency.csv`. They are written again after every child, so a run
stopped halfway leaves what it measured.


## Results

The complete reports are in [results/](results/). The first run, `loadtest-ryzen7-3700x-20261023-001737`, measured
WWCP_POI 6661d2d on an AMD Ryzen 7 3700X (8 cores, 16 logical processors, 128 GiB) with Windows 11, .NET 10.0.13 and the
server GC, `--suite full` with the defaults: 200 operators, 2 charging stations per pool, 2 EVSEs per charging station.

| Charging stations | Largest operator | Managed after start | Commit p50 | Allocated per commit | Commit with merge | Allocated per merge | `base.json` | Read again |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10,000 | 1,702 | 254 MiB | 74 ms | 23 MiB | 2.8 s | 2.2 GiB | 15 MiB | 164 s |
| 50,000 | 8,507 | 1.2 GiB | 411 ms | 81 MiB | 14 s | 11 GiB | 75 MiB | 550 s |
| 100,000 | 17,013 | 2.4 GiB | 774 ms | 154 MiB | 32 s | 21 GiB | 151 MiB | 1,042 s |
| 250,000 | 42,532 | 6.0 GiB | 2.0 s | 371 MiB | 78 s | 53 GiB | 378 MiB | 272 s |
| 1,000,000 | 170,125 | 24.1 GiB | 10.5 s | 1.4 GiB | 666 s | 214 GiB | 1.5 GiB¹ | 1,035 s¹ |

¹ Of the run `loadtest-ryzen7-3700x-stream-20261023-074653` (`--stations 1M --stores file --merges 0 --writers 0,1`), after
the base is written and read as a stream; the first run could not write it.

What it showed:

- **Memory** is linear, about 26 KB managed per charging station with its 2 EVSEs; the working set of the server GC peaks
  at 2 to 3.5 times that, 87 GiB at 1M charging stations, during the merge.
- **A commit costs about 10 µs per charging station of the whole network**, whichever operator it changes: the JSON and
  CBOR ETags hash the content of the whole version (SPECIFICATION.md 8.7). Preparing a change set costs as much again,
  as `CreateChangeSet` applies its changes to calculate the after ETags, which the commit then does once more.
- **Commits are one after another**: `Commit` holds the lock of the history for all of its time. One writer reaches 1/commit
  time; more writers reach less, as every writer overtaken by another one makes a branch, merged with the head.
- **A merge is O(network)** and costs 30 to 60 commits: `RoamingNetwork.TryMerge` flattens the base and both versions
  into JSON, deep-cloning the JSON of every entity with its children. With 2 to 16 writers, nearly every commit is merged
  (10k: 4.9 commits/s with one writer, 0.36 with two or more); from 250k charging stations a merge takes longer than
  `--max-merge`, and the levels of several writers are skipped.
- **Reading a file store again replays every commit, merges included**: the 100k store of 63 commits, 30 of them merged,
  took 17 minutes.
- **A file store of 1M charging stations could not be written**: `WriteBase` serialized the snapshot into one string, longer
  than .NET strings may be (`OutOfMemoryException` with 26 GiB of 128 in use), and `Load` read `base.json` as one. Both
  stream it now (`CanonicalJSON.Write` of Styx, `POIJSON.Write` and `POIJSON.Read`): the base of 1.59 billion bytes is
  written at the start and again by the compaction (83 s), and read again with the same head after 17 minutes, most of
  them replaying its 56 commits.
- **Readers waiting behind commits**: plain reads of the head never waited (EVSE p99 0.01 ms throughout), but every reader
  of the commits, attestations and checkpoints took the lock of the history, so a proof or a `TryGetCommit` waited as long
  as a commit with its merge: up to 47 s at 50k charging stations with 4 writers. Since readers take an immutable view of
  them (`RoamingNetworkHistory.ReadView.cs`), the second run, `loadtest-ryzen7-3700x-mvcc-20261023-030214`, measured:

| Charging stations, writers | Commit read max, before | after | Waiting in all, before | after | Proof p99, before | after |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10k, 2 | 3,424 ms | 167 ms | 22.0 s | 2.3 s | 3,327 ms | 1.4 ms |
| 10k, 4 | 10,101 ms | 176 ms | 20.0 s | 2.5 s | 9,250 ms | 1.3 ms |
| 50k, 2 | 17,432 ms | 380 ms | 31.5 s | 2.0 s | 33.6 ms | 19.6 ms |
| 50k, 4 | 46,824 ms | 407 ms | 47.3 s | 2.5 s | 12.5 ms | 5.7 ms |

  What a reader still waits is the garbage collector, behind merges allocating gigabytes.
