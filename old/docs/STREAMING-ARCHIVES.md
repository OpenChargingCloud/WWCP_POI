# Streaming archive output and atomic persistence

[Repository overview](../README.md) · [History](HISTORY.md) · [Bootstrap](BOOTSTRAP.md) · [Retention](RETENTION.md) · [Scaling](SCALING.md) · [Roadmap](ROADMAP.md)

Archive encoding now writes the existing JSON/CBOR representations directly to streams.
The content profile, archive profiles, state/commit IDs, signing inputs and peer envelopes are
unchanged. Integrated persistence, retention hashing/cold publication and bootstrap capture use
the stream encoder. These measurements describe output. Subsequent [indexed recovery](INDEXED-ARCHIVES.md)
removes the full archive tree, and [borrowed input streams](ARCHIVE-INPUT-STREAMS.md) add bounded
capture with mapped file input. Complete input availability still precedes trust and replay.

## Public APIs and stream ownership

| API | Output | Ownership and consistency |
| --- | --- | --- |
| `history.WriteJSON(destination)` | Existing compact UTF-8 JSON archive, without a BOM | Borrowed writable stream; history gate held during export |
| `history.WriteCBOR(destination)` | Existing deterministic CBOR archive | Borrowed writable stream; history gate held during export |
| `source.WriteArchive(destination)` | Exact frozen bootstrap CBOR archive | Borrowed writable stream; independent of later live history changes |
| `history.ToJSON()` / `history.ToCBOR()` | Complete string / byte array | Convenience results still allocate the complete output |

The destination may be non-seekable. Output starts at its current position; the methods neither
seek nor truncate it. They do not close it or call its `Flush()`. Encoder buffers are committed
on successful completion. Destination disposal and durable flushing belong to the caller.
Null/unwritable destinations are rejected. Write/encoding exceptions propagate and may leave a
partial caller-owned output; use a new destination or discard that output before retrying.

Live history exports reject disposed histories and reentrant gated mutations. The gate keeps
retained entries/head/peers/receipts consistent during output. Gated runtime delivery and
publication wait for the export; calls reentered from destination callbacks are rejected, as is
reentrant history disposal. A failed output does not install a new state or modify runtime.
Direct entity runtime writes still bypass history coordination, as described in [runtime](RUNTIME.md).
Frozen bootstrap output needs no live history gate and remains available after the sender changes.

```csharp
using var destination = new FileStream("history-export.cbor", FileMode.CreateNew, FileAccess.Write);
history.WriteCBOR(destination);
destination.Flush(flushToDisk: true);

// A retained bootstrap session can export its original exact archive again.
using var frozen = new FileStream("frozen-bootstrap.cbor", FileMode.CreateNew, FileAccess.Write);
source.WriteArchive(frozen);
frozen.Flush(flushToDisk: true);
```

These caller-owned exports do not provide atomic replacement. Use `CreatePersistent` / `Open`
for the integrated writer lease, temporary file, durable flush and replacement workflow.

## Encoding and preserved representations

All four archive envelopes (`wwcp-poi-history-v1` through `v4`) use the same output path.
Complete archives retain their checkpoint/commits/head; boundary archives retain the signed
snapshot root/external checkpoint claim; pruned archives additionally retain every receipt.
The static profile remains `wwcp-poi-static-v2`.

JSON preserves the existing field order, escaping, raw application values and compact formatting.
`Utf8JsonWriter` now targets a pooled `IBufferWriter<Byte>` adapter. The adapter commits segments
to the destination without calling its `Flush()` or retaining the full archive. This avoids the
stream constructor's growing document buffer and destination flush behavior in the
[.NET writer implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Text.Json/src/System/Text/Json/Writer/Utf8JsonWriter.cs).

The CBOR envelope writer emits definite map/array counts and sorts text map keys by their
encoded bytes before emitting values. Styx still encodes scalars, tags, numbers and ETag tuples.
POI snapshots now prepare canonical JSON, preflight standalone/remaining reader-depth rules,
then emit directly through the shared deterministic writer without a complete POI CBOR output
buffer. [ChangeSet payloads](DIRECT-ARCHIVE-CHANGESET.md) also preflight and emit directly, retaining
the original exact signed scalar codec and native root ETags. The combined CBOR nesting limit
remains 64; writer/reader rules stay distinct. See [direct POI payloads](DIRECT-ARCHIVE-PAYLOAD.md).
Snapshot administrator metadata keeps the existing lossless application-value codec. Standalone
commit/receipt codecs, signature preimages and incremental commit-pack codecs are unchanged.

The pooled output buffer starts at 16 KiB and can grow for a large scalar requested by a codec;
large already-encoded CBOR payloads bypass a second output copy. This is not a fixed total-memory
bound. Existing graph snapshots, canonical POI JSON/index/preflight, entry ordering,
application metadata and prepared ChangeSet JSON/index/schema paths still consume memory. `ToJSON()` / `ToCBOR()` also retain their full results.

## Integrated persistence and retention

Successful mutations stream the candidate archive into their unique sibling temporary file.
The existing writer lease, `Flush(true)`, same-directory replacement, exact temporary-file cleanup
and named diagnostic observer stages remain in place. In-memory head/maps/runtime are installed
after persistence. An encoding/write failure before replacement preserves the active archive and
in-memory state; a process interruption after replacement can require reopening the new archive.
Directory metadata is still not separately flushed, and storage durability governs power-loss
behavior. Existing active/cold/bootstrap crash cases were rerun against the stream encoder in
[the verification follow-up](VERIFICATION-STREAMING.md); this remains named-point process evidence,
with power loss and arbitrary interruption inside writes/renames outside its coverage.

Retention plans hash/count the complete reviewed CBOR archive incrementally without retaining
its bytes. Execution recomputes the review identity and streams the cold copy, checking its
length/digest before flush/publication. The matching published or preexisting cold file is checked
again and held open through active replacement. A callback streams the new boundary/receipt
catalog into the active temporary file. Exact live head/runtime and original signatures remain
unchanged. Earlier successful cold publication may survive a failed active write for the existing
explicit retry workflow. See [retention contracts](RETENTION.md).

## Frozen bootstrap fragments

Source creation streams directly into private fragment arrays. Whole-archive and per-fragment
SHA-256 digests are calculated during capture. `MaxArchiveBytes` and the capacity implied by
`chunkBytes * MaxChunks` are checked before accepting more bytes. Manifest, inventory and encoded
fragment budgets retain their existing checks, including the last full fragment and short tail.

Only the final short fragment is resized. The arrays remain private; `CreateChunk(index)` returns
a detached immutable fragment, and `WriteArchive` emits the original fragments in order. Later
head/peer/catalog changes do not alter this session or its manifest identity. All four complete/
snapshot/boundary/pruned bootstrap profiles retain their representations.

The source still retains **the entire frozen payload across fragments**. Individual encoder values
can allocate separately. Receiver final validation/activation now assembles the complete input
through [bounded capture](MAPPED-ARCHIVE-RECOVERY.md), then decodes/replays and retains its branch
states. Fragment size therefore bounds messages,
not total source/receiver memory or replay work. No durable sender service or streaming decoder
was added. See [bootstrap](BOOTSTRAP.md).

## Executed build and measurement evidence

On **2026-10-08**, the library/benchmark Release build completed with zero errors and 358 existing
library warnings. The test project also compiled with zero errors and four warnings. NUnit was
not executed in the initial encoding package. The follow-up now adds **149 passing cases** and
reruns existing crash fixtures and the complete suite: **1,174 passed**, zero failures, one ordinary
worker skipped. See [streaming verification](VERIFICATION-STREAMING.md). The earlier
[1,025 passing domain-model tests](VERIFICATION-DOMAIN-MODEL.md) remain a historical baseline.

The unchanged benchmark program measured four operations on four static-v2 shapes, with two
isolated processes, one warmup and three samples per process: **32 workers / 96 samples per
report**. The shapes are 128/512-EVSE graphs, a longer retained history and a four-receipt pruned
history. These measurements exercise archive-v2/v4 and corresponding bootstrap profiles. The
later targeted NUnit suite covers all four profiles, throwing non-seekable destinations and
encoder failures separately; it does not extend the measured performance dataset.

- [Before raw report](performance/streaming-before.json)
- [After raw report](performance/streaming-after.json)
- [Complete measured comparison](performance/streaming-comparison.md)
- [Scoped production encoding patch](performance/streaming-encoding.diff)

Comparison completed successfully: both static ETags, CBOR archive digests, JSON/CBOR operation
digests, frozen manifest identities, inventories and output/written byte counts match. Existing
benchmark setup uses signed histories, real Ed25519 verification and replay invariants. The
measured persistence operation rewrites a retained peer envelope; it does not measure new-commit
publication. Public caller-owned stream outputs have no separate benchmark operation yet.

Measured allocation reductions across the four shapes are **3.2–10.6% for JSON**, **8.2–9.1% for
CBOR**, **9.0–11.8% for durable rewriting** and **7.1–8.8% for bootstrap export**. For graph-512:

| Operation | Before allocation MiB | After allocation MiB | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: |
| JSON archive | 8.303 | 7.886 | 24.64 | 6.92 |
| CBOR archive | 271.528 | 248.609 | 557.62 | 488.38 |
| Durable rewrite | 287.321 | 261.423 | 451.28 | 411.16 |
| Bootstrap export | 299.139 | 274.094 | 504.08 | 454.93 |

Timing is mixed: graph-128 JSON is 7.65 → 7.89 ms; graph-128 bootstrap is 202.43 → 204.89 ms and
pruned-4 bootstrap is 53.45 → 54.70 ms. These small isolated workloads have JIT/GC/system
variation and only one warmup. No universal speedup, reduced peak-memory guarantee, production
capacity or WAN throughput claim follows. Raw reports include CPU, GC, sampled memory peaks,
environment, source/assembly digests and every sample. Individual POI snapshot encoding remains
a substantial allocation cost, alongside the retained graph and full receiver replay.

### Reproduction

```powershell
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite scaling --operations archive-json,archive-cbor,persist-rewrite,bootstrap-export --processes 2 --samples 3 --label streaming-after-v2 --output WWCP_POI_Benchmarks/bin/streaming-after.json
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --compare-before docs/performance/streaming-before.json --compare-after WWCP_POI_Benchmarks/bin/streaming-after.json --summary WWCP_POI_Benchmarks/bin/streaming-comparison.md
```

Use new output paths: the console refuses to overwrite reports. Before/after used identical
commands apart from label/output against their respective production sources. Both benchmark
source digests are `cb1111f16fc5e7c7feeff5f27b8e3367c5b8fe7eb8e5da1b194d4659e64f5051`.
The scoped patch contains only this package's five modified and five new production files; reverse
application reconstructs the matching static-v2 baseline, including earlier uncommitted domain
changes. Its reverse check succeeds, and reconstructing the source fingerprint from saved input
bytes matches `d5f9a4b8769e0adba4f930055bc3042352cf47889aa81f65025732c09ee8d230`.
The measured final source fingerprint is
`882ef87016d841bbdf1f2e2684beba75d500e305b437d5083a0fe56811f66c3e`.
Historical static-v1 measurements in [scaling](SCALING.md) remain unchanged and separate.

## Verification and remaining work

[The verification follow-up](VERIFICATION-STREAMING.md) now covers all four profiles against
standalone codecs and fixed references, non-seekable streams, no close/flush, exact partial
prefixes, reentrant gated operations, blocked publication/runtime delivery, real encoder failures,
retention review/cold/active output and frozen full/tail/bootstrap budgets. Its 149 new cases,
existing crash reruns and complete 1,174-case run pass.

The [individual encoder baseline](ENCODING-COSTS.md) now measures tagged snapshots, signed
ChangeSets, encoder stages and direct versus buffered streams using five warmups/five samples
in each fresh process. It uses the same verified production source and preserves this historical
comparison. [The tagged-document follow-up](TAGGED-DOCUMENT-OPTIMIZATION.md) now removes redundant
static subtree copies/preparation with matched before/after reports and 149/1,174-case reruns.
The later [direct archive payload package](DIRECT-ARCHIVE-PAYLOAD.md) removes POI output buffers;
that build retained canonical JSON/index, ChangeSet buffers and complete decoding/replay.
Larger merged/multi-operator/reference/tariff/parking workloads and production profiling remain
separate evidence. Append-only journals or a new Merkle profile need
their own contracts. See [the roadmap](ROADMAP.md).

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
