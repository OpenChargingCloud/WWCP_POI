# WWCP POI scaling measurements

This standalone .NET 10 console program measures public library paths and isolated encoder/reader stages
in a Release build.
It is separate from NUnit and adds no benchmark package dependency. Fixed UTC timestamps,
IDs, SI quantities and a published Ed25519 seed make state, commit and archive identities
reproducible. The seed has no production authority.

Run from the WWCP_POI repository:

```powershell
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite smoke --processes 1 --samples 1 --output WWCP_POI_Benchmarks/bin/smoke.json
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite scaling --processes 2 --samples 3 --label baseline --output WWCP_POI_Benchmarks/bin/scaling-before.json
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite catalog --processes 2 --samples 3 --label baseline --output WWCP_POI_Benchmarks/bin/catalog-before.json
```

On Linux/macOS, use `dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll`
with the same arguments. After rebuilding an optimization, use distinct `*-after.json` output
names. Existing result files are rejected so earlier evidence is preserved. Each completed
worker is written to the report with `Complete: false`; only the completed suite sets it true.

Generate a comparison, rejecting any changed state/archive/operation identities or byte counts:

```powershell
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --compare-before WWCP_POI_Benchmarks/bin/scaling-before.json --compare-after WWCP_POI_Benchmarks/bin/scaling-after.json --summary WWCP_POI_Benchmarks/bin/scaling-comparison.md
```

The [published optimization patch](../docs/performance/optimization.diff) records the four production
changes between the original measurements; reverse it in a separate checkout to reconstruct the
baseline while retaining the surrounding archive packages. The benchmark operation bodies and
measurement method are the same in both runs. Later report/environment metadata additions do
not change those measured calls.

## Workloads

| Suite / shape | EVSEs | Ordinary changes | Retained branches | Snapshot interval | Prior pruning rounds | Location claims |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| smoke | 16 | 4 | 2 | 4 | 1 | 16 |
| scaling / graph-128 | 128 | 8 | 2 | 4 | 0 | 256 |
| scaling / graph-512 | 512 | 8 | 2 | 4 | 0 | 256 |
| scaling / history-64 | 16 | 64 | 8 | 16 | 0 | 256 |
| scaling / pruned-4 | 16 | 32 | 4 | 8 | 4 | 256 |
| catalog | 1 | Not generated | Not generated | Not generated | Not generated | 16, 256, 4096, 16384 |

There are four EVSEs per station, one connector per EVSE, one grid meter per station, one pool
and one operator. Location counts apply to `catalog-create`, with distinct paths sharing one
digest to expose duplicate-check scaling; no files are read or created for those claims.
Every pruning round publishes four changes, a signed snapshot and a suffix change, then writes
a cold archive and prunes at that snapshot. After setup, the named ordinary changes, snapshots
and branches are added. Reports contain the actual retained graph/commit/receipt/catalog counts.
Changes update one EVSE's power, and branches start at the final published head. This is a
synthetic charging-network workload with real serialization/signature/replay paths.

Custom shapes and a selected set of operations support controlled comparisons:

```powershell
# Change only entity count, commit count, branch count, snapshot frequency or pruning rounds between runs.
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --evses 1024 --changes 64 --branches 8 --snapshot-every 16 --retentions 4 --locations 4096 --operations archive-cbor,replay-cbor,bootstrap-transfer,cold-read --processes 2 --samples 3 --output WWCP_POI_Benchmarks/bin/custom.json
```

## Measured boundaries

| Operation | Included in the measured call |
| --- | --- |
| hash-json / hash-cbor | Full static canonical encoding plus raw SHA-256 and digest display |
| etag-recompute | Fresh calculation of both identifiers from `RoamingNetworkDataSnapshot`; bypasses its lazy digest accessor |
| static-update | Apply a prepared unsigned batch, domain validation and mandatory result hash checks |
| runtime-capture | Apply the same batch to the materialized network, capture independent runtime and read result ETags |
| runtime-materialize | Capture plus full derived hierarchy reconstruction and runtime restoration |
| archive-json / archive-cbor | Full retained archive export, output byte count and raw digest; retains the complete string/byte output |
| archive-json-stream / archive-cbor-stream | Borrowed non-seekable output into an incremental SHA-256/counting sink; no retained archive output or disk I/O |
| snapshot-cbor | Complete tagged POI snapshot encoding with revision metadata, byte count and SHA-256 |
| changeset-cbor | Complete prepared, signed one-operation ChangeSet encoding, byte count and SHA-256 |
| snapshot-json-document | Tagged document construction, including every declared child ETag; returned document is checked after timing |
| snapshot-reading-paths | Schema visitor and metrological validation/path collection on a prebuilt tagged document |
| snapshot-json-canonical | Styx canonical UTF-8 JSON from a prebuilt tagged document |
| snapshot-cbor-tree | Canonical JSON bytes to metrological CBOR value tree, retaining textual ETags |
| snapshot-etag-tree | Convert only schema-owned ETag tuples to native digest bytes in a prebuilt tree |
| snapshot-cbor-write | Styx deterministic encoding of the prebuilt final value tree |
| changeset-json | System.Text.Json UTF-8 serialization of the prebuilt signed ChangeSet |
| changeset-validate-paths | Signing-value validation and schema-owned measurement-path collection on prebuilt inputs |
| changeset-cbor-tree | Existing lossless application/metrological transport value-tree construction |
| changeset-etag-tree / changeset-cbor-write | Native ETag conversion / final Styx deterministic encoding on prebuilt trees |
| replay-json / replay-cbor | Parse the prebuilt archive, verify original commit/batch peers and replay every retained branch |
| snapshot-prepare | Prepare an unsigned full-state snapshot and its deterministic identity |
| persist-rewrite | Publish the already published original signed tip into an opened persistent history; validates peers and rewrites/flushes the complete archive |
| bootstrap-export | Freeze complete archive, construct manifest and encode all bounded CBOR chunk envelopes |
| bootstrap-transfer | Encode/decode CBOR chunks, durably receive them, then replay and explicitly activate an in-memory history |
| cold-read | Locate exact cold bytes, verify digest/current signatures and replay the receipt-bound source history |
| catalog-create | Normalize, bound, deduplicate and freeze ordered local claims, then retrieve their candidates |

Setup, input generation, prebuilt batch preparation/signing and configurable warmups (default one)
are excluded. Complete operations include their correctness checks and recovered-history disposal.
Stage operations consume/encode and byte-check their actual intermediate result after the measured
window; their reported digest/byte count describes that verified intermediate representation. Runtime checks require
`charging` to survive derivation and require recovered static histories to start `available`.
Replay checks chain, anchor, head, both state ETags and retained commit/receipt counts. Every
measured output must equal warmup and every isolated worker for the same shape/operation.
Persistent duplicate rewriting isolates the archive write cost; it does not measure a new
expected-head publication end to end. Bootstrap uses local disk and 64 KiB archive slices;
it is not a network latency or throughput experiment. Output bytes are encoded output or
consumed archive bytes, as defined by the operation. Written bytes count installed archive
or staging payloads; they exclude filesystem metadata, temporary duplicate I/O and lock files.

## Measurement method and limits

- A fresh process is launched for each shape/operation/repetition. Workers run sequentially.
- `--warmups N` (default 1) precedes measured samples. A forced blocking compacting GC and finalizer drain
  precede each sample; these are excluded from elapsed/CPU values.
- `Stopwatch` records elapsed time. Process CPU includes all threads, including the sampler;
  short values can be quantized by the platform clock.
- `GC.GetAllocatedBytesForCurrentThread` counts allocation by these synchronous operations.
  The sampler runs on another thread. This is allocation volume, not live memory, and would
  omit any future work dispatched to other threads.
- Managed memory and working set are sampled every 10 ms, also at both boundaries. These
  approximate operation-window peaks can miss short-lived allocations. Raw records separately
  retain the process lifetime working-set peak, which also includes setup and warmup.
- GC collection counts, starting memory, output/written bytes and every sample are recorded.
  The comparison uses all-sample medians and the largest sampled window peak.
- Reports capture OS/runtime/architecture/GC mode, source revision plus dirty status, production
  source and assembly SHA-256 and dependency revisions/status. Source hashes describe files at
  launch; rebuild after editing sources so they describe the measured assembly.
- Optional runtime-tuning metadata records `DOTNET_TieredCompilation`, `DOTNET_TieredPGO` and
  `DOTNET_ReadyToRun`. The executed Cold-Archive follow-up shows sensitivity to one warmup and
  tiering; use more samples and explicitly record runtime overrides when investigating it.
- Results depend on hardware, JIT tiering, GC, filesystem cache and concurrent system activity.
  No CPU affinity, cold OS-cache reset, contention model, statistical confidence interval,
  production retention trace or capacity guarantee is supplied.

Fresh local scratch folders are owned by each worker. Disposal removes their exact direct files
and explicitly created staging folders without recursive deletion. An externally terminated
worker may leave its scratch folder for administrator cleanup.

See [the measured report and selected improvements](../docs/SCALING.md). A fresh static-v2
[streaming comparison](../docs/STREAMING-ARCHIVES.md) runs the unchanged `archive-json`,
`archive-cbor`, `persist-rewrite` and `bootstrap-export` operations against matching before/after
production sources, with raw reports and a scoped reconstruction patch. It checks exact identities
and byte counts; it does not replace targeted borrowed-stream/failure/crash NUnit coverage.
That [verification follow-up](../docs/VERIFICATION-STREAMING.md) now passes 149 new cases and the
complete 1,174-case suite without modifying these performance reports.


## Individual encoder and borrowed stream baseline

The [static-v2 encoding baseline](../docs/ENCODING-COSTS.md) records four shapes, 17 operations,
two fresh processes, five warmups and five measured calls per process. It preserves the earlier
history inputs and production source; the four older archive operation bodies remain unchanged.

```powershell
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite scaling --operations snapshot-cbor,snapshot-json-document,snapshot-reading-paths,snapshot-json-canonical,snapshot-cbor-tree,snapshot-etag-tree,snapshot-cbor-write,changeset-cbor,changeset-json,changeset-validate-paths,changeset-cbor-tree,changeset-etag-tree,changeset-cbor-write,archive-json,archive-json-stream,archive-cbor,archive-cbor-stream --processes 2 --samples 5 --warmups 5 --label encoding-baseline-v2 --output WWCP_POI_Benchmarks/bin/encoding-baseline.json
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --encoding-report WWCP_POI_Benchmarks/bin/encoding-baseline.json --summary WWCP_POI_Benchmarks/bin/encoding-summary.md
```

The encoding summary requires all 17 operations for every shape, complete process/sample counts,
unchanged graph/archive inventories, repeated identities, equal buffered/stream archive results
and agreement between document/final stages and the complete encoder. Every worker checks the
actual stage output byte for byte after each warmup/sample; schema path sets must also agree.
Intermediate CBOR trees still have text ETags, so their digests differ from final binary tuples.
Path-only operations output no wire bytes; their identity binds the sorted path names/count.

`EncodingProbe` binds non-public schema/transport functions to typed delegates once during setup.
Changed signatures fail; there is no fallback or new production API. Snapshot path collection
mirrors the validation loop using the actual schema visitor and measurement predicate. Stage
inputs/trees and expected outputs are retained in setup. Their consuming encoders and checks
are outside the time/allocation window, but can affect later tiering/cache and lifetime memory.
Stage medians cannot be added or treated as percentages of complete encoder costs. Managed
peaks also include retained setup inputs and the preceding result until replacement; allocation
volume gives the clearer comparison of these synchronous calls. Complete calls count/hash output
inside timing; the stream sink counts/hashes incremental writes and retains no complete output.

The ChangeSet fixture updates one EVSE's power and carries one real Ed25519 peer signature.
This baseline supplies no long-batch or multi-peer scaling result. It also does not measure
network/disk throughput or a CBOR/JSON decoder.


The [tagged-document optimization](../docs/TAGGED-DOCUMENT-OPTIMIZATION.md) repeats this exact
baseline with the unchanged benchmark source and matched outputs. It records a scoped production
patch, allocation/time comparisons and reruns the existing 149 targeted/1,174 full NUnit cases.
Use its run/summary/comparison commands with fresh output paths; do not overwrite the baseline.

## Bounded child ETag cache measurements

The [child-cache package](../docs/CHILD-ETAG-CACHE.md) adds four operations:
`child-etags-json-cold`, `child-etags-json-warm`, `child-etags-cbor-cold`, `child-etags-cbor-warm`.
A fresh map and its root pair are prepared outside every cold call; warm calls reuse one prefilled
context. `PrepareSample` runs outside warmup/measurement and affects only these operations.
Every output is consumed and checked. Each worker also records exact cache statistics and a
separate approximate managed-heap delta after collecting the same live map before/after first
export. Import/root hashing and reflection are excluded. Per-context logical payload limits
exclude object/dictionary overhead; held versions can retain multiple bounded contexts.

The matched ten-operation reports have 80 fresh workers / 400 samples each. Read the
[checked cache summary](../docs/performance/child-etags-summary.md) and
[complete comparison](../docs/performance/child-etags-comparison.md). Preserve the raw reports.
Timings are descriptive; the before run partly overlapped local build/test activity.

```powershell
python WWCP_POI_Benchmarks/child_etag_report.py --before docs/performance/child-etags-before.json --after docs/performance/child-etags-after.json --output WWCP_POI_Benchmarks/bin/child-etags-summary-rerun.md
```

The stdlib report checks complete sample/process groups, matching method/runtime/dependencies,
all inventories/outputs, cold misses without inherited hits, warm reuse, cache bounds and exact
retained key/digest counts. Retained heap deltas are approximate and reported separately.
These synthetic shapes do not saturate defaults; unit cases use smaller exact/excess limits.

Small process-heap deltas can be negative because other setup objects become collectible between
readings. They do not imply a negative cache cost; exact entry/key/digest payload counts and
managed delta ranges are reported independently. See the package report for observed ranges.

## Direct POI CBOR measurements

The [direct POI encoder](../docs/DIRECT-POI-CBOR.md) uses this unchanged C# harness to compare
the completed cache build with direct output over 80 workers / 400 samples. Both sides retain
the same bounded child cache; use `direct_poi_cbor_report.py` for these paired reports.
The old `snapshot-reading-paths`, `snapshot-cbor-tree`, `snapshot-etag-tree` and `snapshot-cbor-write`
operations now measure historical reference stages, not the production POI pipeline. The later
direct ChangeSet encoder also turns its old tree stages into historical references. `EncodingProbe` continues to check complete POI
bytes against that independent previous-tree path during setup. No public production probe is added.

```powershell
python WWCP_POI_Benchmarks/direct_poi_cbor_report.py --before docs/performance/child-etags-after.json --after docs/performance/direct-poi-cbor-after.json --output WWCP_POI_Benchmarks/bin/direct-poi-cbor-summary-rerun.md
```

The stdlib report verifies complete groups, matching source/runtime/dependencies, outputs,
inventories and cold/warm cache counts in both builds. It reports allocation/time, sampled
managed/working-set peaks and approximate collected retention separately. Preserve raw inputs.

## Long signed ChangeSet batches

The [direct ChangeSet package](../docs/DIRECT-CHANGESET-CBOR.md) adds the `changesets` suite:
1/64/512/2,048 ordered property/custom-data updates on up to 512 EVSEs, with one/four real
Ed25519 envelopes using the public benchmark seed under distinct key IDs. The largest fixture
repeats that target round four times. Construction, signing, previous-tree byte checks, signature
verification and applying the exact result are setup. Complete output/SHA-256 is measured.

`changeset-batch-json` emits canonical complete JSON to avoid process-random dictionary order;
`changeset-batch-cbor` emits public `ToCBOR` output. This suite constructs no history; its
`ArchiveIdentity` field binds the signed batch CBOR. Two fresh workers/five warmups/five samples
yield 32 workers/160 samples per before/after report. The stdlib summary verifies complete groups,
matching harness/runtime/dependencies, exact outputs and inventories. Old tree microstages are
reference paths; existing application-metadata/decoder tree helpers retain their uses.

```powershell
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite changesets --processes 2 --warmups 5 --samples 5 --label batch-rerun --output WWCP_POI_Benchmarks/bin/batch-rerun.json
python WWCP_POI_Benchmarks/changeset_batch_report.py --before docs/performance/direct-changeset-cbor-before.json --after docs/performance/direct-changeset-cbor-after.json --output WWCP_POI_Benchmarks/bin/batch-summary-rerun.md
```

Use fresh outputs. These synthetic fixtures do not establish signing performance, every operation
or production capacity; no build/test overlaps the paired reports, and timings/peaks remain descriptive.

## Direct POI archive payload measurements

[The archive payload package](../docs/DIRECT-ARCHIVE-PAYLOAD.md) uses the unchanged C# harness:
four existing graph/history/pruned shapes, complete versus streamed JSON/CBOR archives,
snapshot CBOR, four cold/warm child-context controls and frozen bootstrap output. Two fresh
processes/five warmups/five samples give 80 workers / 400 samples per paired report.
Archive workloads use prepared retained histories; cold/warm labels describe the separate
snapshot controls. The stdlib report checks complete groups, output count/digest agreement,
matching inventories/dependencies/source method, bounded cache states and collected retention.

```powershell
python WWCP_POI_Benchmarks/direct_archive_payload_report.py --before docs/performance/direct-archive-payload-before.json --after docs/performance/direct-archive-payload-after.json --output WWCP_POI_Benchmarks/bin/direct-archive-payload-summary-rerun.md
```

Use fresh outputs and preserve raw reports. Preflight adds traversal/small-leaf costs; measurements
record them alongside buffer removal. No task build/test overlaps these runs. Timings, sampled
peaks and collected heap deltas remain descriptive, not production capacity or memory bounds.

## Bounded POI archive preflight reuse measurements

[The preflight package](../docs/PREFLIGHT-ALLOCATION.md) reuses the completed direct-archive-payload
after report as its exact before baseline. The unchanged ten-operation C# harness produces a fresh
80-worker/400-sample after report. Source/assembly reconstruction, matching dependencies/method,
all output counts/digests/inventories and cold/warm child-cache budgets are checked. The new SI
scalar context exists only inside each archive payload call; the existing collected-retention
probe continues to describe the snapshot child cache. Other local .NET work was observed during
this task; no task build/test overlaps its after series. Timings/peaks remain descriptive.

```powershell
python WWCP_POI_Benchmarks/direct_archive_payload_report.py --before docs/performance/direct-archive-payload-after.json --after docs/performance/preflight-allocation-after.json --output WWCP_POI_Benchmarks/bin/preflight-allocation-summary-rerun.md
```

Preserve both raw reports and use fresh outputs. See the package's complete test/build/run commands.

## Direct ChangeSet archive payload measurements

[The archive ChangeSet package](../docs/DIRECT-ARCHIVE-CHANGESET.md) adds `--suite batcharchives`:
1/64/512/2,048 ordered operations on up to 512 EVSEs, one/four real equal Ed25519 peers on the
batch and both retained commits. Setup checks previous-tree standalone bytes, signing preimages,
actual result application and archive recovery/signatures. `batch-archive-cbor` buffers the archive;
`batch-archive-cbor-stream` counts/hashes borrowed output without retaining complete bytes.
`batch-archive-control-json` emits canonical complete signed batch JSON, not JSON archive output.
All operations bind the same history/static inventory and complete CBOR archive identity.

The expanded C# harness is identical on both sides. Fresh scaling and signed-batch paired reports
each have 48 workers/240 samples per side (two processes/five warmups/five samples), with no build/test
from this task overlapping either series. Scaling uses six operations and adds no cold/warm probe.
The stdlib report checks complete groups, method/runtime/dependencies, inventories and all output
counts/digests; timings and sampled peaks remain descriptive on this non-dedicated host.

```powershell
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite batcharchives --processes 2 --warmups 5 --samples 5 --label archive-batches-rerun --output WWCP_POI_Benchmarks/bin/archive-batches-rerun.json
python WWCP_POI_Benchmarks/archive_changeset_report.py --before docs/performance/direct-archive-changeset-batches-before.json --after docs/performance/direct-archive-changeset-batches-after.json --output WWCP_POI_Benchmarks/bin/archive-batches-summary-rerun.md
python WWCP_POI_Benchmarks/archive_changeset_report.py --before docs/performance/direct-archive-changeset-scaling-before.json --after docs/performance/direct-archive-changeset-scaling-after.json --output WWCP_POI_Benchmarks/bin/archive-scaling-summary-rerun.md
```

Preserve the raw reports and use fresh output paths. See the package for full build/test/run/compare
commands, bounded scalar reuse, source/assembly/TRX bindings and remaining JSON/path/tree/replay costs.

## Archive decoder and replay baseline

`--suite recovery` runs seven operations across seven shapes/all four archive profiles.
`read-cbor-limits` calls the actual private limit scanner; `read-cbor-tree` parses a complete Styx
tree; `read-cbor-model` composes an envelope using actual private component/profile/field parsers;
`read-signatures` checks every real fixture peer once without applying states; `read-restore`
calls actual private production restore with prepared models. `read-cbor` and `read-json` use
the complete public recovery path. Typed stage delegates are bound during setup, without new
production accessors. Model-only composition belongs to the benchmark: its v3/v4 suffix is
eager, whereas actual boundary restore decodes suffix models lazily. Prepared models are warm.

All outputs are checked outside measurement: original archive bytes/peers, head/checkpoint/anchor,
every branch state and fresh local runtime. Reader OutputBytes means consumed input size for
full reads/scanning, zero for object-only stages; no reader emits wire output.
Stage medians cannot be added or treated as shares of complete recovery.

Shapes reuse graph-128/512, history-64 and pruned-4, add a signed snapshot-boundary suffix, and
reuse the exact one-operation/one-peer and 2,048-operation/four-peer batch archives. Six CBOR
inputs bind to preceding export reports by digest/bytes/static/history inventory. The new
98-worker/490-sample report is a decoder baseline with unchanged production, not a speedup comparison.
Each full-CBOR worker separately collects the approximate additional managed memory retained
by one recovered history after measurements, keeping source/input live on both sides.
Retained models/states/runtime, largest commits/scalars and sampled peaks are not capacity bounds.

```powershell
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite recovery --processes 2 --warmups 5 --samples 5 --label recovery-rerun --output WWCP_POI_Benchmarks/bin/recovery-rerun.json
python -B WWCP_POI_Benchmarks/archive_recovery_report.py --input WWCP_POI_Benchmarks/bin/recovery-rerun.json --scaling-control docs/performance/direct-archive-changeset-scaling-after.json --batch-control docs/performance/direct-archive-changeset-batches-after.json --output WWCP_POI_Benchmarks/bin/recovery-summary-rerun.md
```

Finish builds/tests before the formal series. See [measured recovery costs and reader requirements](../docs/ARCHIVE-RECOVERY-COSTS.md)
and the [raw report](../docs/performance/archive-recovery-baseline.json).

## Indexed archive recovery comparison

The [indexed-reader package](../docs/INDEXED-ARCHIVES.md) compares complete `read-cbor` with
`read-json` and prepared-model `read-restore` controls across the same seven shapes/four profiles.
The before report is an exact 42-worker / 210-sample subset of the original 98-worker baseline;
those before samples were not rerun. The after report contains 42 fresh workers / 210 samples.
The C# harness is unchanged; the shared boundary restore uses its new synchronous cursor.
No task builds/tests overlap the formal after series. Static/history/peer/receipt inventories,
complete inputs/results and recovered heads match. Source/binary/test/reference/report bindings
and remaining input/part/private-suffix/state memory costs are recorded in the package.

`indexed_archive_report.py` checks the exact subset against the original raw baseline, complete
groups, runtime/dependencies/method and every paired inventory/result. It reports allocation,
wall/process CPU, sampled peaks and separate approximate collected recovered-history deltas.
Earlier isolated decoder stage probes remain historical; their medians are not additive.

```powershell
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite recovery --operations read-cbor,read-json,read-restore --processes 2 --warmups 5 --samples 5 --label indexed-reader-rerun --output WWCP_POI_Benchmarks/bin/indexed-reader-rerun.json
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --compare-before docs/performance/indexed-archive-before.json --compare-after WWCP_POI_Benchmarks/bin/indexed-reader-rerun.json --summary WWCP_POI_Benchmarks/bin/indexed-reader-comparison-rerun.md
python -B WWCP_POI_Benchmarks/indexed_archive_report.py --before docs/performance/indexed-archive-before.json --after WWCP_POI_Benchmarks/bin/indexed-reader-rerun.json --original docs/performance/archive-recovery-baseline.json --output WWCP_POI_Benchmarks/bin/indexed-reader-summary-rerun.md
```

Use fresh output names and finish builds/tests first. No total-memory, concurrency or throughput
guarantee follows from operation-thread allocation or process-heap deltas on this shared host.

## Borrowed CBOR input controls

Explicit recovery operations `read-cbor-input-memory` and `read-cbor-input-spool` compare private
capture with a fresh `read-cbor` span control in the same build. The historical seven-stage
recovery default remains unchanged. Sources are non-seekable, return at most 16 KiB per read,
and are created inside measurement over the same prepared input. Memory capture permits the
whole archive under its byte budget; forced file capture includes real I/O, a shared maintenance
lease, flush, read-only mapping and cleanup. Options/prepared bytes are setup; exact output/
branch/peer/runtime checks are post-measurement. The fixture retains its source bytes throughout.

The [package](../docs/ARCHIVE-INPUT-STREAMS.md) and `stream_input_report.py` bind all seven shapes/
four profiles and preceding inputs/results. This is a fresh same-build operation comparison,
not a before/after speedup claim with the changed C# harness. Managed allocation does not count
all mapped/file-cache pages; sampled peaks and local disk times establish no WAN or capacity bound.

```powershell
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite recovery --operations read-cbor,read-cbor-input-memory,read-cbor-input-spool --processes 2 --warmups 5 --samples 5 --label stream-input-rerun --output WWCP_POI_Benchmarks/bin/stream-input-rerun.json
python -B WWCP_POI_Benchmarks/stream_input_report.py --input WWCP_POI_Benchmarks/bin/stream-input-rerun.json --previous docs/performance/indexed-archive-after.json --output WWCP_POI_Benchmarks/bin/stream-input-summary-rerun.md
```

Finish builds/tests before measuring and use fresh output names.

## Existing-file recovery controls

Explicit `read-cbor-file-array` and `read-cbor-file-mapped` operations compare the preceding
known-length input algorithm with production Open in the same build. Input files are prepared
before measurement; both writer leases and restored histories live through exact output/branch/
peer/runtime verification outside measurement. The historical seven-stage default is unchanged.
The [integration package](../docs/MAPPED-ARCHIVE-RECOVERY.md) also reruns the unchanged cold-read
and bootstrap-transfer workloads against preserved preceding and newly built binaries.

```powershell
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite recovery --operations read-cbor-file-array,read-cbor-file-mapped --processes 2 --warmups 3 --samples 3 --label mapped-files-rerun --output WWCP_POI_Benchmarks/bin/mapped-files-rerun.json
python -B WWCP_POI_Benchmarks/mapped_recovery_report.py --files docs/performance/mapped-file-controls.json --previous docs/performance/stream-input-recovery.json --before docs/performance/mapped-integration-before.json --after docs/performance/mapped-integration-after.json --output WWCP_POI_Benchmarks/bin/mapped-summary-rerun.md
```

`mapped_recovery_report.py` validates complete groups, method, exact preceding file inputs and
paired input/output identities. File controls and before/after integration costs are separate
comparisons. No constant total-memory or universal elapsed-time improvement follows.

## Private model/replay preparation measurements

[The preparation package](../docs/MODEL-PREPARATION.md) compares seven existing shapes/all four
profiles for `read-cbor-model`, `read-restore` and `read-cbor`, with two fresh processes/three
warmups/three samples on each side: 84 workers / 252 samples total. The preserved preceding C#
harness binary, runtime/dependency files and source are unchanged. Two private binary folders
hold the before library and the newly built after library respectively; the public/private
bindings used by this harness do not change in this package. Rebuilding the harness against
the new library produces a separately recorded assembly hash; the matched series uses the
preserved harness on both sides. No task build/test overlaps the formal sequential workers.

`model_preparation_measure.py` reproduces that fixed worker subset against explicitly bound
binaries. It verifies library/harness/dependency hashes first; source hashes come from the
specified verification binding. It invokes the same C# worker arguments and preserves raw
reports. Keep the original before build, make an after copy of its binaries, replace only the
after POI library/PDB after building, and provide the corresponding binding for each folder.
Use fresh outputs and run the two invocations sequentially without overlapping builds/tests.

```powershell
python -B WWCP_POI_Benchmarks/model_preparation_measure.py --binary-directory path/to/preserved-before --binding docs/verification/mapped-recovery-results.json --label model-before-rerun --output WWCP_POI_Benchmarks/bin/model-before-rerun.json
python -B WWCP_POI_Benchmarks/model_preparation_measure.py --binary-directory path/to/matched-after --binding docs/verification/model-preparation-results.json --label model-after-rerun --output WWCP_POI_Benchmarks/bin/model-after-rerun.json
python -B WWCP_POI_Benchmarks/model_preparation_report.py --before docs/performance/model-preparation-before.json --after docs/performance/model-preparation-after.json --inputs docs/performance/mapped-file-controls.json --output WWCP_POI_Benchmarks/bin/model-summary-rerun.md
```

The stdlib summary verifies complete groups/method, exact preceding inputs, per-stage results,
every repeated inventory, harness/dependency bytes and collected retention inventories. Model-only
decoding is eager for boundary suffixes; production v3/v4 stays lazy. Setup, byte/peer/branch/runtime
checks and disposal are outside measurement. Stage medians cannot be added; approximate collection
deltas and sampled peaks establish no total-memory bound or production throughput. Current trust
is checked on every recovery; this package adds no persistent cache.

## Canonical identity/signature preparation measurements

[The canonical preparation package](../docs/CANONICAL-PREPARATION.md) adds `read-signatures`
to the same seven-shape preparation/restore/full-CBOR subset: 112 workers / 336 samples across
both sides. The preserved C# harness binary and source, runtime/dependency files, inputs and
outputs match. Before uses the model-preparation library; after replaces only the POI library/PDB
in a separate private copy. Complete before workers finish before builds/tests; complete after
workers start after tests/builds finish. Use fresh outputs and sequential workers.

```powershell
python -B WWCP_POI_Benchmarks/canonical_preparation_measure.py --binary-directory path/to/preserved-before --binding docs/verification/model-preparation-results.json --label canonical-before-rerun --output WWCP_POI_Benchmarks/bin/canonical-before-rerun.json
python -B WWCP_POI_Benchmarks/canonical_preparation_measure.py --binary-directory path/to/matched-after --binding docs/verification/canonical-preparation-results.json --label canonical-after-rerun --output WWCP_POI_Benchmarks/bin/canonical-after-rerun.json
python -B WWCP_POI_Benchmarks/canonical_preparation_report.py --before docs/performance/canonical-preparation-before.json --after docs/performance/canonical-preparation-after.json --inputs docs/performance/mapped-file-controls.json --output WWCP_POI_Benchmarks/bin/canonical-summary-rerun.md
```

The stdlib launcher checks explicit source/binary/dependency bindings. The summary validates
complete four-stage groups/method, exact preceding input inventories, original peers, all
repeated outputs and collected history inventories. `read-signatures` invokes individual peer
callbacks without a preparation scope and retains their original factory as a control; actual
restore/recovery uses bounded scoped reuse. Boundary model-only preparation remains eager;
production v3/v4 stays lazy. Stage medians, shared-desktop timings, sampled peaks and approximate
post-collection deltas establish no total-memory bound or production throughput.

## Root/head snapshot reconstruction measurements

[This package](../docs/SNAPSHOT-RECONSTRUCTION.md) uses the same seven-shape/four-stage worker
subset and preserved C# harness/dependencies as canonical preparation. Its after report is the
exact before control; new sequential after workers provide another 56 workers/168 samples. Sources
and library/harness bytes are bound separately. Model/signature probes remain controls; restore
includes root/head construction, trust and replay rather than isolated constructor timing.

`snapshot_reconstruction_measure.py` verifies explicitly bound binary/dependency bytes and refuses
to overwrite evidence. Preserve both builds as described above and use fresh output names:

```powershell
python -B WWCP_POI_Benchmarks/snapshot_reconstruction_measure.py --binary-directory path/to/preserved-before --binding docs/verification/canonical-preparation-results.json --label reconstruction-before-rerun --output WWCP_POI_Benchmarks/bin/reconstruction-before-rerun.json
python -B WWCP_POI_Benchmarks/snapshot_reconstruction_measure.py --binary-directory path/to/matched-after --binding docs/verification/snapshot-reconstruction-measurement-binding.json --label reconstruction-after-rerun --output WWCP_POI_Benchmarks/bin/reconstruction-after-rerun.json
python -B WWCP_POI_Benchmarks/snapshot_reconstruction_report.py --before docs/performance/canonical-preparation-after.json --after docs/performance/snapshot-reconstruction-after.json --inputs docs/performance/mapped-file-controls.json --output WWCP_POI_Benchmarks/bin/reconstruction-summary-rerun.md
```

Keep each formal series sequential without task builds/tests. The report validates complete groups,
all profiles, inventories, input bytes/identities, repeated results, preserved harness/dependencies
and collected retention inventories. No production capacity or total-memory bound follows.

## Immutable signature-copy measurements

`SignatureCopyProbe` adds five worker operations for checkpoint/ChangeSet/snapshot peer copies,
embedded batch peer replacement and real Ed25519 commit signing. Four shapes and sixteen copies
per call cover one/four peers and up to 2,048 operations. Construction, complete byte/oracle/trust
checks and cloning replacement batch metadata are setup; individual signing has no scoped cache.

Build the new harness against the preceding library before production edits; preserve that complete
directory. After building/testing, clone it and replace only the after POI library/PDB. Bind exact
source/binary/dependency bytes for both folders. The same harness runs sequentially with two fresh
workers/three warmups/three samples per group/side. Keep task builds/tests outside both formal series.
The launcher refuses to overwrite outputs; the summary validates every fixed group, inventory,
result, harness/dependency/runtime byte and method. These probes establish no recovery/throughput bound.

```powershell
python -B WWCP_POI_Benchmarks/signature_copies_measure.py --binary-directory path/to/preserved-before --binding docs/verification/signature-copies-before-binding.json --label copies-before-rerun --output WWCP_POI_Benchmarks/bin/copies-before-rerun.json
python -B WWCP_POI_Benchmarks/signature_copies_measure.py --binary-directory path/to/matched-after --binding docs/verification/signature-copies-after-binding.json --label copies-after-rerun --output WWCP_POI_Benchmarks/bin/copies-after-rerun.json
python -B WWCP_POI_Benchmarks/signature_copies_report.py --before docs/performance/signature-copies-before.json --after docs/performance/signature-copies-after.json --output WWCP_POI_Benchmarks/bin/copies-summary-rerun.md
```

See [copy contracts, measured costs and evidence](../docs/SIGNATURE-COPIES.md).

## Cooperative parser-cancellation measurements

`read-cbor-input-token` adds a never-cancelled cancellable token to the existing private-memory
borrowed-input operation. Default-token `read-cbor-limits` and span `read-cbor` remain controls.
Six shapes/all four profiles use two fresh workers/three warmups/three samples per group/side:
72 workers/216 measured calls. Capture and existing trust wrappers remain within input recovery;
the comparison measures successful-read overhead, not cancellation latency or an isolated check.

Build/preserve the new harness against preceding production before editing production; clone that
folder after building/testing and replace only the after POI library/PDB. Both explicit bindings
must match exact source/binary/dependency bytes. Keep task builds/tests outside formal series.

```powershell
python -B WWCP_POI_Benchmarks/parser_cancellation_measure.py --binary-directory path/to/preserved-before --binding docs/verification/parser-cancellation-before-binding.json --label parser-before-rerun --output WWCP_POI_Benchmarks/bin/parser-before-rerun.json
python -B WWCP_POI_Benchmarks/parser_cancellation_measure.py --binary-directory path/to/matched-after --binding docs/verification/parser-cancellation-after-binding.json --label parser-after-rerun --output WWCP_POI_Benchmarks/bin/parser-after-rerun.json
python -B WWCP_POI_Benchmarks/parser_cancellation_report.py --before docs/performance/parser-cancellation-before.json --after docs/performance/parser-cancellation-after.json --inputs docs/performance/mapped-file-controls.json --output WWCP_POI_Benchmarks/bin/parser-summary-rerun.md
```

Launchers refuse output overwrite; the summary rejects incomplete groups, changed input/output
inventories/identities, shape parameters, peers/head results, method/harness/dependencies and
retention counts. See [contracts, tests and evidence](../docs/PARSER-CANCELLATION.md).

## Shared-reference, tariff and parking recovery baseline

`--suite domainrecovery` prepares eight 16/64-EVSE shapes across all four history profiles.
Shared grid/software/document catalogs, meters at every supported hierarchy slot, nested tariff
values, EVSE/connector tariff references and parking garage/space/group/product links use existing
production parsers. Signed reference/value/membership edits culminate in an explicit two-parent
merge. Each retained commit/batch carries two equal peers with distinct reproducible Ed25519 keys.

The default operations are `read-cbor-model`, `read-restore`, `read-cbor` and `read-json`.
Model-only eagerly decodes all prepared suffix parts; actual v3/v4 recovery retains lazy suffix
timing. Setup and full output/branch/graph/runtime checks are outside calls. `DomainInventory`
records actual graph/value/reference counts and the complete branch-state fingerprint;
`RecoveryInventory` binds bytes, peers and snapshots. Separate collected memory remains approximate.
The fixture is linked into contract tests, without changing the production API or binary.

The formal baseline uses two sequential fresh workers/three warmups/three samples per group:
64 workers/192 measured calls. All task builds/tests and historical input controls finish first.
The launcher binds DLLs, runtime configuration and dependency manifest, and refuses overwrite.
The report rejects incomplete groups, changed inputs/domain/branches/results/peers and bindings.
The six preceding simple-data controls are correctness checks, not a performance comparison.

```powershell
dotnet run --project WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-build -- --suite domainrecovery --processes 2 --warmups 3 --samples 3 --output WWCP_POI_Benchmarks/bin/domain-unbound-rerun.json
python -B WWCP_POI_Benchmarks/domain_recovery_controls.py --binary-directory path/to/preserved-domain-build --binding docs/verification/domain-recovery-binding.json --preceding docs/performance/parser-cancellation-after.json --output WWCP_POI_Benchmarks/bin/domain-controls-rerun.json
python -B WWCP_POI_Benchmarks/domain_recovery_measure.py --binary-directory path/to/preserved-domain-build --binding docs/verification/domain-recovery-binding.json --label domain-baseline-rerun --output WWCP_POI_Benchmarks/bin/domain-baseline-rerun.json
python -B WWCP_POI_Benchmarks/domain_recovery_report.py --input docs/performance/domain-recovery-baseline.json --binding docs/verification/domain-recovery-binding.json --output WWCP_POI_Benchmarks/bin/domain-summary-rerun.md
```

Use the Python launcher for the checked-in bound protocol. A new build needs a binding to its
actual bytes; the generic suite does not record that binding. See [design/tests/evidence](../docs/DOMAIN-RECOVERY-WORKLOADS.md)
and the [raw baseline/summary](../docs/performance/domain-recovery-summary.md). Larger catalogs,
production concurrency and later optimizations need separate matched evidence.

## Matched temporary validation projection measurements

The [validation projection package](../docs/VALIDATION-PROJECTION.md) compares the exact rich
baseline with the new library using the preserved C# harness/dependencies. Build/test the after
library before a formal series; clone the complete before directory and replace only POI DLL/PDB.
Bind actual bytes. The existing domain launcher runs 64 fresh workers / 192 calls per side.
Model decoding is a control; eager model-only suffix work and actual lazy v3/v4 recovery differ.
Inputs, peers, inventories, branches and recovered/model results must match across all stages.

```powershell
python -B WWCP_POI_Benchmarks/domain_recovery_measure.py --binary-directory path/to/matched-after --binding docs/verification/validation-projection-after-binding.json --label validation-rerun --output WWCP_POI_Benchmarks/bin/validation-rerun.json
python -B WWCP_POI_Benchmarks/validation_projection_report.py --before docs/performance/domain-recovery-baseline.json --before-binding docs/verification/domain-recovery-binding.json --after docs/performance/validation-projection-after.json --after-binding docs/verification/validation-projection-after-binding.json --output WWCP_POI_Benchmarks/bin/validation-summary-rerun.md
```

Run stack profiling separately with a task-local dotnet-trace 10.0.750501 tool. The launcher
stages pinned TraceEvent 3.1.21 `.cs.in` / `.csproj.in` templates in an isolated work directory;
these files do not change the compiled C# recovery harness. It filters weighted allocation ticks
by resolved `Measurement.Run` stack and records trace/tool/analyzer/source bindings. Inclusive
sites overlap and are sampling estimates. Profiler timings are excluded from formal evidence.

```powershell
python -B WWCP_POI_Benchmarks/profiling/validation_projection_trace.py --binary-directory path/to/preserved-before --binding docs/verification/domain-recovery-binding.json --work-directory path/to/fresh-before-trace --dotnet-trace path/to/dotnet-trace.exe
python -B WWCP_POI_Benchmarks/profiling/validation_projection_trace.py --binary-directory path/to/matched-after --binding docs/verification/validation-projection-after-binding.json --work-directory path/to/fresh-after-trace --dotnet-trace path/to/dotnet-trace.exe
```

The launcher refuses existing outputs and changed DLL/runtime/dependency bytes. The comparison
rejects incomplete groups, changed method/harness/dependencies, inputs/branches/peers/results.
Task builds/tests/profiling finish before formal series. Time and collected memory remain
descriptive; no production capacity, total-memory or latency bound follows.

## Matched normalized immutable snapshot map preparation

The [map preparation package](../docs/SNAPSHOT-MAP-PREPARATION.md) compares the preserved previous
[raw series](../docs/performance/validation-projection-after.json) with the
[new after series](../docs/performance/snapshot-map-preparation-after.json): 64 fresh sequential
workers / 192 measured calls per side across eight rich shapes, four profiles and four stages.
The same C# harness/dependency bytes run on both sides, under explicit actual source/binary bindings.
Archive bytes, original peers, both branches, complete graph/reference/value inventories and
recovered model/head results match. All 32 fixed references remain unchanged.

The [generated comparison](../docs/performance/snapshot-map-preparation-summary.md) reports each
stage, elapsed time and collected additional history memory. At 64 EVSEs, CBOR cumulative
allocation falls a further 16.63–19.38%, to 329.63–528.71 MiB/call.
All new formal workers run outside task builds/tests/profiling. Desktop timing and heap deltas
are descriptive; larger group/catalog/history and concurrent workloads remain separate.

```powershell
python -B WWCP_POI_Benchmarks/snapshot_map_preparation_report.py --before docs/performance/validation-projection-after.json --before-binding docs/verification/validation-projection-after-binding.json --after docs/performance/snapshot-map-preparation-after.json --after-binding docs/verification/snapshot-map-preparation-binding.json --output WWCP_POI_Benchmarks/bin/map-preparation-summary-rerun.md
```
