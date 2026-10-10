# Scaling measurements and selected improvements

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [Roadmap](ROADMAP.md)

The separate [benchmark console program](../WWCP_POI_Benchmarks/README.md) supplies reproducible
Release measurements for static hashes, immutable updates, runtime capture/materialization,
signed retained archives, durable rewriting, replay, bootstrap and cold discovery. It exercises
public APIs with fixed timestamps/IDs/SI values and real Ed25519 verification. It adds no NUnit
fixture or third-party benchmark dependency.

## Evidence baseline

The committed raw reports, source digests and optimization diff measure the preceding static-v1
model. They retain their original bytes and identifiers. The later static-v2 shared catalogs and
parking relationships change the dataset/hash contract; rebuilding the current console measures
that new contract and cannot be identity-compared against the historical static-v1 reports.
A fresh matching static-v2 before/after comparison now measures streaming archive output,
durable rewriting and bootstrap export. See [streaming evidence](STREAMING-ARCHIVES.md), its raw
reports and scoped patch. The historical static-v1 measurements below remain separate.

## Reproduce the measurements

```powershell
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite scaling --operations hash-json,hash-cbor,etag-recompute,static-update,runtime-capture,runtime-materialize,archive-json,archive-cbor,replay-json,replay-cbor,snapshot-prepare,persist-rewrite,bootstrap-export,bootstrap-transfer,cold-read,catalog-create --processes 2 --samples 3 --label measurement --output WWCP_POI_Benchmarks/bin/scaling.json
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite catalog --processes 2 --samples 3 --label measurement --output WWCP_POI_Benchmarks/bin/catalog.json
```

Each workload/operation runs in two fresh sequential processes, each with one warmup and three
measured samples. Dataset preparation is excluded. One operation runs on the calling thread;
elapsed/process CPU, thread allocation, GC counts, sampled managed/working-set memory and
encoded output/written bytes are recorded. A forced compacting GC precedes each sample outside
timing. Memory samples occur every 10 ms and at both boundaries; peaks are approximations.
Raw lifetime working-set peaks include setup/warmup and must not be presented as operation-only
memory. CPU includes the sampling thread and can be quantized for short operations.

The executed environment is **Windows x64, .NET 10.0.12, Release, workstation GC, AMD Ryzen 7
3700X (8 cores / 16 logical processors), approximately 128 GiB physical RAM**. Dependency
revisions, dirty working-tree status, production source/assembly digests and every sample are
preserved in the raw reports. The baseline includes the earlier uncommitted archive packages;
its source digest identifies that exact production state rather than claiming it is the last commit.

| Workload | Graph nodes | Retained commits | Retention receipts | Purpose |
| --- | ---: | ---: | ---: | --- |
| graph-128 | 291 | 13 | 0 | 128 EVSEs, 32 stations/meters, 8 ordinary changes, 2 snapshots, 2 branches |
| graph-512 | 1155 | 13 | 0 | Same history pattern, 512 EVSEs, 128 stations/meters |
| history-64 | 39 | 77 | 0 | 16 EVSEs, 64 ordinary changes, 4 snapshots, 8 retained branches |
| pruned-4 | 39 | 42 | 4 | 16 EVSEs, four prior pruning events, then 32 ordinary changes, 4 snapshots and 4 branches |

Meter values are nested static properties rather than separate graph nodes. Each EVSE has a
connector. Branches start at the published tip and edit its first EVSE. The catalog suite freezes
16/256/4096/16384 distinct normalized local paths under one digest. Its claims are not read;
counts above the default location budget use an explicit larger local budget. Pruning receipts
retain original removed-ID evidence and therefore grow independently of retained active commits.
The workloads are synthetic, including the long-history case; they are not a two-year production trace.

## Selected measured results

The main before/after suites publish **780 samples from 260 isolated workers**. Two additional
Cold-Archive comparisons add **84 samples from 12 workers**, for **864 published samples in 272
workers**. Release builds succeed; the benchmark project has no build warnings, while the library
retains its existing 360 warnings when rebuilt. Every comparison preserves content and byte counts.

| Path | Before | After | Observation |
| --- | ---: | ---: | --- |
| 512 EVSEs: runtime capture | 82.40 ms / 43.104 MiB allocated | 39.29 ms / 22.307 MiB allocated | 52.3% lower median time, 48.2% less allocation |
| 512 EVSEs: snapshot preparation | 102.55 ms / 41.303 MiB allocated | 42.66 ms / 22.671 MiB allocated | Existing static digest reuse |
| 512 EVSEs: CBOR archive export | 681.11 ms / 345.524 MiB allocated | 612.53 ms / 271.515 MiB allocated | 10.1% lower median time, 21.4% less allocation; exactly 948466 bytes in both runs |
| 512 EVSEs: checked CBOR replay | 4979.14 ms / 1872.490 MiB allocated | 4257.78 ms / 1768.730 MiB allocated | Replay remains the larger cost |
| 4096 same-digest location claims | 185.52 ms | 5.51 ms | Indexed duplicate checks |
| 16384 same-digest location claims | 2196.47 ms / 6.252 MiB allocated | 19.27 ms / 6.894 MiB allocated | About 114 times faster; extra temporary index allocation |

Allocation volume is not peak live memory. The sampled managed peak for the 512-EVSE CBOR
archive stays approximately 54 MiB in both variants. Runtime materialization's sampled peak
increases from 46.2 to 52.1 MiB despite lower allocation volume. Streaming encoding remains
motivated by complete archive construction; these digest changes do not establish universally
lower memory peaks. Raw samples and all operations, including unchanged/slower paths, are published.

### Cold replay and JIT sensitivity

The small `pruned-4/cold-read` case is slower with normal tiering and one warmup: the initial
six-sample comparison is 173.48 to 222.43 ms. A separate **three-process, seven-sample-per-process**
confirmation reproduces 182.93 to 229.95 ms (**25.7% slower**) while allocation declines from
50.849 to 48.771 MiB. This result is retained rather than described as a universal speedup.

With explicitly **`DOTNET_TieredCompilation=0`**, the same repeated comparison is 104.36 to
101.78 ms, with allocation 50.743 to 48.672 MiB. The change in behavior indicates JIT/startup
sensitivity; it does not identify the responsible method or prove a steady-state improvement.
One warmup can be insufficient after changing setup's invocation counts. Longer normal-tiering
warmup/production traces and JIT profiling remain additional measurements. The library does not
change application runtime settings. Reports from the explicit experiment record the tuning flag;
the earlier reports predate that optional metadata field.

```powershell
$taskPreviousTiering = $env:DOTNET_TieredCompilation
try {
    $env:DOTNET_TieredCompilation = '0'
    ./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite scaling --operations cold-read --processes 3 --samples 7 --label no-tiering --output WWCP_POI_Benchmarks/bin/cold-no-tiering.json
}
finally { $env:DOTNET_TieredCompilation = $taskPreviousTiering }
```

## Observed costs and implemented changes

Full CBOR archive export repeatedly produces tagged POI subtrees. In the baseline graph-512
workload, its approximately 0.95 MB encoded output caused about 345 MiB of allocation. Checked
replay cost several seconds, while direct JSON archive writing was much smaller. The explicit
catalog constructor also scanned the accumulated same-digest path list for every new claim.

The selected changes preserve all existing content, commit, signature and archive profiles:

1. **Reuse canonical JSON bytes within one digest pair.** The JSON SHA-256 and Styx metrological
   CBOR encoder now consume the same canonical JSON byte buffer. The buffer is temporary and
   is not retained in historical snapshots.
2. **Reuse the authoritative root's ETags.** `RoamingNetwork.ETags` reads its immutable snapshot's
   lazy digest pair. Tagged root JSON/CBOR exports reuse that pair instead of cloning, preparing
   and hashing the complete root hierarchy again. Runtime overlays and revision transport remain
   outside content identity; nested tagged nodes keep their original byte representation.
3. **Share digests through snapshot-only links.** Advancing a snapshot revision shares the existing
   lazy digest pair with its exact unchanged entity/index maps. Ordinary static ChangeSets still
   calculate and check fresh result ETags.
4. **Index temporary catalog deduplication.** A per-digest `HashSet` checks normalized paths while
   the ordered lists retain first-registration order and platform path comparison. Supplied
   duplicates still consume the local enumeration budget. Construction uses extra temporary
   membership memory to avoid repeated scans; the retained immutable catalog representation is unchanged.

No canonical full-archive or per-commit byte cache is added. Such a cache could retain complete
copies alongside the shared immutable graph; this package only shares digests and temporary
conversion work. Signature checks, replay and current trust are performed on every recovery.

## Published raw evidence and comparison

- [Scaling before](performance/scaling-before.json) and [scaling after](performance/scaling-after.json)
- [Scaling comparison](performance/scaling-comparison.md)
- [Catalog before](performance/catalog-before.json) and [catalog after](performance/catalog-after.json)
- [Catalog comparison](performance/catalog-comparison.md)
- [Cold replay confirmation before](performance/cold-read-before.json),
  [after](performance/cold-read-after.json) and [comparison](performance/cold-read-comparison.md)
- [Cold replay without tiering before](performance/cold-read-no-tiering-before.json),
  [after](performance/cold-read-no-tiering-after.json) and [comparison](performance/cold-read-no-tiering-comparison.md)
- [Scoped production optimization patch](performance/optimization.diff), recording the exact four
  source changes so the baseline can be reconstructed in a separate checkout

The comparison command rejects incomplete runs, different workload sets and any changed static
state, archive digest, operation result identity, retained inventory or output/written byte count.
Warmup and measured results must also agree within and across independent workers. Derived
runtime must preserve the source `charging` status, and replay/bootstrap recovery must initialize
fresh `available` status. These are benchmark invariants for the measured workloads, not a
replacement for the separate domain/merge/crash regression suite.
The existing 952-case NUnit result belongs to the preceding archive-maintenance package; this
package was evaluated through Release builds and the explicitly requested measurement runs.

Reported time reductions are all-sample medians for this machine/run. Individual raw samples
show JIT/GC/cache/system variation. JSON export and unrelated operations also fluctuate, so
small timing changes do not establish a general improvement. Allocation-volume changes and
unchanged identities provide additional evidence. Catalog memory sampling and elapsed comparisons
include path normalization and location allocation, not only membership checks.

## What remains expensive and what to do next

- Full-state digest calculation still traverses complete static content. Updating one entity
  remains proportional to the complete encoded graph for its mandatory result hashes.
- Runtime capture still visits the source hierarchy, and first derived hierarchy access still
  constructs complete independent domain objects/schedules.
- Archive envelopes, [preflight-validated POI payloads](DIRECT-ARCHIVE-PAYLOAD.md) and
  [ChangeSet payloads](DIRECT-ARCHIVE-CHANGESET.md) now stream; prepared JSON/index/schema paths remain. JSON/CBOR replay verifies and retains every branch state. Full snapshots increase
  archive/replay costs as their count grows.
- Persistence still replaces a complete flushed archive for every successful mutation. The measured
  duplicate rewrite isolates that persistence cost; it does not measure new commit publication.
- Bootstrap source now encodes into frozen bounded fragments but retains their total payload;
  final receiver decoding/activation retains complete input bytes and recovered branch states.
  [Indexed CBOR reading](INDEXED-ARCHIVES.md) now removes the complete archive tree. Local disk receipt timing
  supplies no WAN/HTTP throughput or latency result. Cold lookup still replays matched history.
- Receipt catalogs retain all recorded IDs. Location lookup already uses a digest index; this
  package improves catalog creation, not compressed receipt storage or on-disk indexes.

Deterministic stream encoding and atomic streamed persistence are now implemented and measured
with matching static-v2 workloads. [Streaming contracts and evidence](STREAMING-ARCHIVES.md) record
remaining value buffers, measured allocation reductions and mixed timings. The later
[stream/failure/crash NUnit verification](VERIFICATION-STREAMING.md) passes 149 new cases and the
complete 1,174-case suite without changing those measurements. The later
[individual encoder baseline](ENCODING-COSTS.md) now separates per-value stages and direct stream
costs on that same production source. [The tagged-document follow-up](TAGGED-DOCUMENT-OPTIMIZATION.md)
now removes redundant static subtree copies/passes, records matching before/after measurements
and reruns 149 targeted plus 1,174 full regression cases against the changed production binary.
Streaming decode/replay,
an append-only journal, content-addressed subtree storage or a new Merkle identity profile need their
own design and measurements. Larger multi-operator/reference/tariff/parking graphs, long operation
batches, merged histories, concurrent writers/readers, cold OS-cache runs and independent peers
remain additional evidence. Encoded input limits establish local rejection budgets, not throughput
or million-entity capacity guarantees.

The later [bounded child ETag cache](CHILD-ETAG-CACHE.md) reuses immutable child digest pairs within
a complete static snapshot context. Pure snapshot revisions share it; changed maps start fresh.
Explicit entry/key/payload limits preserve the admitted traversal prefix. Runtime and transport
views stay fresh; managed overhead and total retained-version memory are reported separately.
Cold/warm and exact-output verification preserve static-v2 bytes and identities.

The subsequent [direct POI CBOR encoder](DIRECT-POI-CBOR.md) removes complete intermediate
CBOR/native-ETag trees, preserving canonical number/SI rules and all static-v2 output bytes.
Matched cold/warm/complete/archive reports use the same ten-operation harness with child caches
in both builds. Individual canonical JSON/output buffers and archive value-depth validation remain.
The new package passes 228 targeted and 1,253 full Release cases, including the original 149 stream cases.

The subsequent [direct ChangeSet CBOR encoder](DIRECT-CHANGESET-CBOR.md) removes complete
batch/native-header-ETag trees while retaining the exact v2 scalar/signing rules and decoder.
Matched long batches/multiple-peer results and 55 new cases accompany 283 focused / 1,308 full
Release passes. Complete per-value JSON/output buffers and archive depth validation remain costs.

The later [direct POI archive payload package](DIRECT-ARCHIVE-PAYLOAD.md) removes complete
checkpoint/snapshot CBOR output buffers after preflight preserving standalone/remaining reader
depth rules. That build retained canonical JSON/index, ChangeSet buffers, full archive rewriting
and decoding/replay costs. Its 140 new cases, 423 focused and 1,448 full Release cases pass; earlier evidence
above remains historical, and the new paired reports bind their own source/assembly fingerprints.

## Later bounded POI archive preflight reuse

[The preflight reuse package](PREFLIGHT-ALLOCATION.md) retains all pre-payload semantic/depth and
atomicity contracts while decoding names once, pooling cleared bounded name sets, proving validated
native ETag tuple shape and reusing bounded successful SI scalar trees within one payload call.
All 494 focused / 1,519 full Release cases pass, including 71 new budget/depth/context/retry cases.
Matching 80-worker/400-sample reports preserve every output and inventory; streamed 512-EVSE
allocation changes 74.87 -> 66.03 MiB (-11.8%). Earlier measurements/counts remain historical.
That build retained complete JSON/index and batch buffers, graph preparation and decoding/replay costs.

## Later direct ChangeSet archive emission

[The ChangeSet archive package](DIRECT-ARCHIVE-CHANGESET.md) removes complete per-batch CBOR
output buffers after preflight preserving original exact signed scalars, native root ETags,
both depth rules, equal peers and atomic failure/retry contracts. A bounded per-call scalar
context reuses successful codec results while rechecking every occurrence's ownership/depth.
All 708 focused / 1,733 full Release cases pass, including 214 new cases. Two matched series
each contain 48 workers/240 samples per side and retain all output counts/digests/inventories.
At 2,048 operations/four peers streamed signed-archive allocation changes
32.27 -> 31.90 MiB (-1.1%). Earlier costs/counts remain historical; complete batch CBOR
output buffers are now absent from borrowed archive output. Prepared POI/ChangeSet JSON/index,
schema paths, metadata trees, public buffered results, fragment totals and replay remain costs.

### Subsequent archive reader measurements

The [recovery baseline](ARCHIVE-RECOVERY-COSTS.md) separates seven reader stages across all four
profiles, with 98 workers / 490 calls and exact recovered heads, branch states, peers and runtime.
It adds 58 contract cases; all 179 focused / 1,791 full Release cases pass without production or
reference changes. Full-input syntax/duplicate/EOF checks precede trust; v3/v4 suffix models
retain their existing lazy callback timing. That build preceded the subsequent indexed reader. Prepared models and retained branch states still consume memory;
stage medians and collected process-heap deltas establish no total-memory or throughput bound.

### Indexed CBOR archive recovery

The [indexed reader](INDEXED-ARCHIVES.md) validates the complete resident input before trust,
then reads envelope/model slices without constructing a complete archive CBOR tree. Boundary
suffix bytes are privately frozen before callbacks; eager v1/v2 and lazy v3/v4 model timing,
all equal peers, deterministic bootstrap and fresh runtime remain unchanged. The package adds
78 cases; 257 focused / 1,869 full cases pass. Matched complete-CBOR, JSON and prepared-model
restore reports use the exact 210-sample baseline subset and 210 fresh after samples with the
unchanged C# harness. Resident input, private suffix bytes, individual trees and retained states
remain. The subsequent [input-stream package](ARCHIVE-INPUT-STREAMS.md) implements borrowed
sync/async non-seekable capture with bounded memory/file bytes, mapped recovery and cancellation.
Shared reader leases permit concurrency and exclude explicit orphan maintenance. Earlier
measurements remain historical; the new package compares fresh same-build span/memory/file inputs.

The subsequent [mapped recovery package](MAPPED-ARCHIVE-RECOVERY.md) applies scoped file input
to persistent opening and both cold APIs, and bounded capture to bootstrap preview/activation.
Input views close before new installation and persistent return. Exact digest/manifest bytes,
current trust, atomic retry, known-length diagnostics and recovery cancellation are covered;
model/replay costs, mapped pages and retained states remain. Earlier results are historical.

### Subsequent private model/replay preparation

The [model preparation package](MODEL-PREPARATION.md) removes recursive deep copies of descendants
already owned by a private import/completion/binding traversal. Entry-point and projected-value
copies remain; all normalization/validation order, exact bytes/peers, current trust, eager/lazy
recovery and fresh runtime are preserved. No persistent cache is added. Frozen prior algorithms
and atomic failures/retries are covered by 88 new cases; all 1,942 interoperability / 2,274 full
Release cases pass. Matched preparation/restore/full-CBOR reports bind the unchanged harness binary,
dependencies, exact inputs and results. Earlier measurements remain historical. Canonicalization,
cryptography, individual trees, root/head reconstruction and retained branch states remain costs.

### Subsequent bounded canonical identity/signature preparation

The [canonical preparation package](CANONICAL-PREPARATION.md) reuses successful immutable unsigned
bytes within synchronous recovery/peer verification, with 64-entry/1-MiB-value/4-MiB-byte admission
bounds. Every outer scope releases its entries. Original numeric/SI/identity/signature bytes,
public array ownership, eager/lazy errors, all current peer/policy checks, runtime and atomic
publication remain exact. It adds 106 cases; all 2,048 interoperability / 2,380 full Release cases
pass with unchanged fixed artifacts. Four-stage matched reports bind the unchanged harness,
dependencies, inputs and results across 112 workers / 336 samples. Individual signature-only
calls retain the preceding pipeline as a control; actual recovery joins scoped preparation.
Earlier measurements remain historical. Root/head reconstruction, cryptography, individual
model trees, retained versions and wider production/concurrency measurements remain work.

### Subsequent exact root/head snapshot reuse

[Root/head reconstruction](SNAPSHOT-RECONSTRUCTION.md) now conditionally retains an immutable
snapshot after the complete domain parser, references, metadata and representation completion.
Every stored property byte, child and version must match; different spellings/defaults use the
existing capture path. Separate histories keep independent runtime objects; a private root-only
head can keep its freshly validated root model. Every peer/current policy, eager/lazy error timing,
cancellation and atomic publication remain intact. No persistent model or trust cache is added.
The package adds 86 cases and matched model/signature/restore/full-recovery measurements with
unchanged prepared inputs, outputs, C# harness and dependencies. Earlier results remain historical.

### Subsequent immutable signature-envelope copies

[Signature copies](SIGNATURE-COPIES.md) retain the already validated commit identity when only
peer arrays change. Embedded batch replacements need exact unsigned-content proof; changed
content and independent parses retain the complete constructor. Existing scoped canonical bytes
can be shared within the same conservative bounds, with detached public arrays and full release.
Every current peer policy, atomic duplicate/pack union and independent runtime remains intact.
The package adds 103 cases and direct matched copy/signing measurements; recovery measurements
above remain historical and establish no copy-package end-to-end speedup.

### Subsequent cooperative parser cancellation

[Parser cancellation](PARSER-CANCELLATION.md) passes the existing recovery token explicitly through
owned syntax/budget/canonical loops and around each commit/receipt/model/root/replay/head step.
There is no ambient token or retained parser context. Exact preceding diagnostics, eager/lazy
trust timing, input ownership, scope release, late token clearing and atomic installation remain.
Individual Styx/model/crypto calls and copies remain synchronous; no cancellation latency bound
is claimed. It adds 270 cases; all 2,507 interoperability / 2,839 full Release tests pass with
unchanged fixed references. Matched default/active-token successful reads use 72 workers/216 samples,
the same new harness/dependencies and exact prepared inputs/results. Earlier measurements remain
historical. Broader shared-reference/tariff/parking workloads are measured in the subsequent package below.

### Subsequent domain recovery baseline

[Shared-reference, tariff and parking workloads](DOMAIN-RECOVERY-WORKLOADS.md) add eight
deterministic 16/64-EVSE shapes with all four profiles, shared catalogs, meters, nested values,
targeted membership edits and one signed two-parent merge. Model/restore/full CBOR/JSON stages
use 64 fresh workers/192 samples with exact input/head/branch/peer and independent-runtime checks.
Production/dependency/reference bytes remain unchanged; six old simple-workload controls still
match. The new baseline measures richer data and is separate from historical performance reports.
81 new cases and all 2,588 interoperability / 2,920 full Release tests pass, including current
bootstrap trust rejection/retry and atomic invalid reference/value/additional-peer edits.

### Subsequent temporary validation projection reuse

[Validation projections](VALIDATION-PROJECTION.md) reuse successful ancestors and one station
view per fixed immutable map. Every ordinary parser/reference/trust check and failure order
remains; failed values are uncached and runtime stays private to the pass. The separate tariff
group text/length/empty-flag correction preserves ordering across equivalent operator formats.
71 new cases and all 2,659 interoperability / 2,991 full Release cases pass with unchanged
fixed references. The exact rich baseline/harness/dependencies support matched recovery
measurements; earlier performance reports remain historical. Normalized immutable map
preparation is completed below; larger catalogs and production concurrency remain work.

### Subsequent normalized immutable map preparation

[Normalized snapshot maps](SNAPSHOT-MAP-PREPARATION.md) reuse exactly equal properties, child sets,
entity/map branches and root catalogs after the full ordinary parser and normalization. Binding
reads stable typed IDs directly; all reference/current peer checks, exact errors, independent
runtime, canonical content and atomic publication remain. Removed identities/consumers are handled.
53 new cases and all 2,712 interoperability / 3,044 full Release cases pass with unchanged references.
Matched rich recovery measurements keep the exact archives, branches, peers and C# harness/dependencies.
Earlier reports remain historical. Larger group/catalog/history and concurrency workloads are next.
