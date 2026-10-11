# Streaming archive verification

[Repository overview](../README.md) · [Streaming contracts and measurements](STREAMING-ARCHIVES.md) · [Tests](../WWCP_POI_Tests/README.md) · [Roadmap](ROADMAP.md)

This Release/.NET 10 execution on **2026-10-08** verifies the streaming archive encoder with
the actual POI assembly and local WWCP_CoreData, Hermod and Styx dependencies on Windows.
Production source and assembly digests match the final static-v2 streaming measurement report.
This package adds tests and documentation; it required no further production correction and
does not regenerate the fixed JSON/CBOR/signature references or performance reports.

## Results

| Run | Passed | Failed | Skipped | Local ignored evidence |
| --- | ---: | ---: | ---: | --- |
| New stream/encoding/bootstrap fixtures | 149 | 0 | 0 | `streaming-focused.trx` |
| Existing persistence/snapshot/retention/bootstrap crash regression | 130 | 0 | 1 | `streaming-crash-regression.trx` |
| Complete suite including all new fixtures | 1,174 | 0 | 1 | `streaming-full.trx` |

The skipped worker is invoked separately in child processes by the crash cases. Both explicit
reference generators remain excluded from ordinary runs. The full suite increased from the
earlier **1,025 passing model cases** by the **149 new cases** below. The Release test build
completed with zero errors and four existing warnings.

The ordinary CLI summary counts 1,175 cases: 1,174 passed and one worker skipped. Raw TRX counters
also include the two explicit generators as not executed (1,177 total); the result summary records
them separately from the ordinary skipped worker.

TRX files are under `WWCP_POI_Tests/bin/TestResults/streaming/`. A portable
[result summary](verification/streaming-results.json) records exact counters/timestamps, local
TRX SHA-256 values and production/test source and assembly digests. Ignored TRX files stay local.

```powershell
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore -v quiet -clp:ErrorsOnly
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~ArchiveStreamingTests|FullyQualifiedName~BootstrapStreamingTests|FullyQualifiedName~ArchiveStreamingFailureTests' --logger 'trx;LogFileName=streaming-focused.trx' --results-directory WWCP_POI_Tests/bin/TestResults/streaming --nologo
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~PersistenceTests|FullyQualifiedName~SnapshotRetentionCrashTests|FullyQualifiedName~BootstrapCrashTests' --logger 'trx;LogFileName=streaming-crash-regression.trx' --results-directory WWCP_POI_Tests/bin/TestResults/streaming --nologo
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=streaming-full.trx' --results-directory WWCP_POI_Tests/bin/TestResults/streaming --nologo
```

## New coverage

| Fixture / cases | Executed checks |
| --- | --- |
| [ArchiveStreamingTests](../WWCP_POI_Tests/Interoperability/ArchiveStreamingTests.cs), 24 | All four profiles, JSON/CBOR, 1/7/65,536-byte sink segmentation; exact bytes against independently assembled outer envelopes and unchanged standalone codecs; prefix append, non-seekable output, no destination close/flush, signed replay and fresh runtime |
| Same fixture, 8 | Existing fixed complete/snapshot/boundary/pruned references through direct JSON/CBOR streams, including the signed merge archive |
| Same fixture, 24 | Throwing output at byte zero, halfway and the final byte; exact partial prefix, unchanged head/network/static inventory/runtime, successful retry/publication/status delivery |
| Same fixture, 8 | Destination callbacks cannot reenter publication, runtime delivery, live stream export, bootstrap capture or history disposal; existing memory exports remain readable |
| Same fixture, 4 | A blocked export holds the gate until completion; independently started publication/status delivery then proceeds |
| Same fixture, 2 | Null/read-only destinations and disposed histories reject without output |
| [ArchiveStreamingFailureTests](../WWCP_POI_Tests/Interoperability/ArchiveStreamingFailureTests.cs), 8 | Real CBOR depth failures during persistent publish/store across all four profiles; unchanged active bytes/head/runtime/inventory, exact temporary cleanup, partial nonpersistent output, retry and reopening |
| Same fixture, 4 | Incremental retention review digest matches the whole-value reference; exact cold/active bytes, observer stage order, retained cold handle, same head/runtime and temporary cleanup |
| Same fixture, 7 | Definite array counts 0/23/24/255/256/65,535/65,536 match Styx canonical bytes |
| Same fixture, 6 | Encoded map-key order/duplicate rejection, combined depth 64/65 for native/pre-encoded values, mismatched array counts/trailing values and incremental hash forwarding/borrowed ownership |
| [BootstrapStreamingTests](../WWCP_POI_Tests/Interoperability/BootstrapStreamingTests.cs), 8 | All four profiles, full final fragments and short tails, byte/chunk SHA-256, independent fragments, later sender/head/receipt changes, sender disposal and signed receiver activation |
| Same fixture, 12 | Frozen output failures at three positions, null/read-only destinations and exact session retry |
| Same fixture, 20 | Exact byte/count/commit/JSON-or-CBOR-wire/manifest budgets succeed; reducing each budget by one rejects without changing the history |
| Same fixture, 4 | Frozen destination callbacks may advance sender history/runtime and repeat the original archive independently |
| Same fixture, 10 | Capture writes cross full/tail fragments and byte/count capacity; rejected writes do not change captured length/digest, successful continuation and finalization remain exact |

The support fixture builds real two-peer signed batches, branched/merged histories, full snapshots
and twice-pruned receipt catalogs. Large Unicode/description values force segmented output.
The CBOR reference builds a full map from the unchanged standalone codecs; it does not call the
new archive adapter. JSON uses an independently assembled envelope over the existing value writers.

The depth failure uses 60 nested metadata arrays. Its standalone snapshot commit is valid; the
archive adds containers and exceeds the combined limit of 64. Earlier retained payloads ensure
the stream has emitted bytes before the error. Persistent execution rejects before replacement;
nonpersistent storage defers this codec check until export, where caller-owned output can be partial.
These are real encoder exceptions, separate from injected failures at named file stages.

## Existing recovery and reference checks rerun

The selected crash run exercises active archive publication, signed snapshots, cold pruning and
bootstrap manifest/chunk/activation persistence, original peer arrays, writer leases, exact old/new
bytes, fresh trust/runtime and retry. It includes **60 actual child-process exits**: 22 snapshot/
retention, 36 bootstrap and two ordinary active publications. The complete suite additionally
reruns the five archive-maintenance exits and every model/merge/exchange/reference fixture.
See [snapshot/retention crash contracts](CRASH-RECOVERY.md),
[bootstrap crash contracts](BOOTSTRAP-CRASH-RECOVERY.md) and [maintenance](ARCHIVE-MAINTENANCE.md).

Fixed cryptographic/reference fixtures, culture variants, graph/reference recreation, merge,
second-parent adoption, static/runtime separation and SI transport checks all pass in the full run.
The original performance report remains separate evidence: these tests establish no new speed,
allocation, peak-memory, production-capacity or WAN result.

## Test setup corrections and practical limits

An initial stream-only run passed 64 and failed six cases because the twice-pruned fixture was
small enough to fit one encoder buffer. A retained large snapshot description now exercises
segmentation for that profile as well. The first combined run passed 139 and failed six cases:
four cold-handle checks used a reader that did not permit the existing read/write handle on Windows,
and two depth cases failed before their small prefix had been emitted. The reader now requests
read access with compatible sharing; the depth fixture retains a large earlier value. The corrected
145-case run passed, then four frozen callback cases brought the final focused result to 149.
These corrections changed test setup, not production encoding/persistence or pinned references.

This is local regression evidence for one implementation and named process-exit points. It does
not simulate power loss, filesystem damage or arbitrary interruption within writes/renames. Gate
tests exercise the public routed APIs; direct entity runtime writes still bypass that gate.
That build retained the full fragmented bootstrap payload, per-value CBOR output and complete
receiver CBOR trees. Later packages below remove those output buffers and the complete archive tree;
complete receiver input, private boundary bytes, individual part trees and retained states remain. Broader graph/peer/concurrency combinations,
independent implementations and production profiling remain open. The subsequent
[encoder baseline](ENCODING-COSTS.md) now measures individual POI/ChangeSet encoding and direct
stream costs on this same production source/assembly. It changes the benchmark/documentation
and selects redundant tagged-document copying/preparation as the next optimization candidate.
That [follow-up](TAGGED-DOCUMENT-OPTIMIZATION.md) now implements the production change and reruns
these 149 cases and the complete 1,174-case suite. The original execution above remains its
own recorded source/assembly baseline.

The later [bounded child-cache package](CHILD-ETAG-CACHE.md) adds 20 targeted cases and passes
169 combined focused / 1,194 full Release cases, with zero failures. This earlier execution and
its original source/assembly/TRX evidence remain historical. Fixed reference files are unchanged.

The subsequent [direct POI CBOR package](DIRECT-POI-CBOR.md) removes complete value/conversion
trees, adds 59 targeted cases and passes 228 focused / 1,253 full Release cases. The earlier
execution and its source/assembly evidence remain historical; fixed reference artifacts are unchanged.

The later [direct ChangeSet encoder](DIRECT-CHANGESET-CBOR.md) adds 55 cases and passes
283 focused / 1,308 full Release cases. Existing source/assembly/byte evidence remains historical;
fixed references are unchanged. Scoped batch tag-depth persistence failures have additional coverage.

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
