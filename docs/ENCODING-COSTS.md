# Individual CBOR encoder and direct output costs

[Repository overview](../README.md) · [Benchmark boundaries](../WWCP_POI_Benchmarks/README.md) · [Scaling](SCALING.md) · [Streaming](STREAMING-ARCHIVES.md) · [Roadmap](ROADMAP.md)

This package measures the existing static-v2 encoders and selects the next optimization.
It extends the benchmark and documentation. Production codecs, identifiers, signature inputs,
reference vectors and NUnit sources are unchanged.

## Recorded baseline

- [Raw measurements](performance/encoding-baseline.json): **136 fresh worker processes,
  680 measured samples**, four existing shapes and 17 operations. Each worker performs five
  excluded warmups followed by five measured calls. Workers run sequentially.
- [Checked summary](performance/encoding-summary.md): all-sample medians, ranges, CPU,
  allocation volume, sampled memory, collection counts and bytes for every operation/shape.
- Environment: Release, .NET 10.0.12, Windows x64, 16 logical processors,
  workstation GC. Runtime tiering overrides are unset; see the raw environment and dependencies.
- Fixed graph/history/pruning inputs are identical to [the preceding streaming measurement](STREAMING-ARCHIVES.md).
  All graph inventories, static ETags, archive digests and the older JSON/CBOR operation outputs
  match that report. Production source and assembly SHA-256 also match.

- Production source: `882ef87016d841bbdf1f2e2684beba75d500e305b437d5083a0fe56811f66c3e`.
- Production assembly: `62201061576d4b182895c0ddf563bd5d18d1e716cc925a05c1cea51f99c59056`.
- Benchmark source: `1f7d11023960393b23e38f759795b72c5c7ce27b56f278c39a20c7b070b99e2c`.

Five warmups change the measurement conditions compared with the preceding one-warmup report.
Its timing values must not be compared as a new production speedup. The buffered/direct pairs
below are from this single fresh baseline. Earlier reports and their reconstruction patches
retain their original bytes.

## Buffered versus borrowed output

| Shape | Encoding | Buffered ms | Direct stream ms | Buffered allocation MiB | Direct stream allocation MiB | Allocation reduction | Exact output bytes |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| graph-128 | JSON | 7.096 | 7.426 | 2.1099 | 1.1887 | 43.7% | 153021 |
| graph-128 | CBOR | 176.922 | 187.888 | 62.6910 | 61.8892 | 1.3% | 249862 |
| graph-512 | JSON | 15.445 | 21.566 | 7.9921 | 4.4525 | 44.3% | 544530 |
| graph-512 | CBOR | 370.434 | 369.874 | 248.5378 | 245.5146 | 1.2% | 948637 |
| history-64 | JSON | 4.215 | 4.017 | 1.4213 | 0.4836 | 66.0% | 160077 |
| history-64 | CBOR | 49.666 | 52.979 | 17.5596 | 16.9450 | 3.5% | 144594 |
| pruned-4 | JSON | 2.917 | 2.610 | 0.9400 | 0.4044 | 57.0% | 106105 |
| pruned-4 | CBOR | 41.038 | 46.660 | 15.6842 | 15.3897 | 1.9% | 105525 |

The direct sink is writable and non-seekable. It incrementally counts/hashes every write without
retaining an archive, closing/flushing the destination or performing disk/network I/O. The
buffered JSON call includes its returned string and UTF-8 conversion for the digest; buffered
CBOR includes the returned byte array. Both paths include SHA-256, counting and digest display.
These are comparisons of the documented caller paths, including their different hash/copy costs.

All paired output byte counts and SHA-256 values agree. Direct output allocates less in every
shape; that baseline still constructed complete per-snapshot/ChangeSet CBOR values. Smaller retained
output alone does not imply lower encoder allocation or a memory bound. Elapsed values vary
across samples/processes; the full min/max ranges are in the checked summary. The evidence
supports using direct output to avoid materializing the archive; it establishes no general speedup.
For example, graph-512 JSON is 15.445 ms buffered versus 21.566 ms direct (+39.6%), despite
44.3% fewer allocated bytes. Its two buffered workers have medians of 6.291 and 24.357 ms;
the aggregate median lies between markedly different runs. CBOR direct timings are also slower
in three shapes and nearly equal in graph-512. Five warmups do not remove process/JIT/GC/system
variation. No JIT trace was captured to establish a cause.

JSON archives write the snapshot directly with its root ETags. CBOR snapshot payloads use the
existing tagged POI codec, also declaring child ETags. Thus the JSON/CBOR byte sizes compare
different existing transport declarations and encoder paths; they are not a compression ratio.

## Individual values and stages

| Shape | Tagged snapshot ms / allocation MiB | Signed ChangeSet ms / allocation MiB | Snapshot / ChangeSet CBOR bytes |
| --- | ---: | ---: | ---: |
| graph-128 | 50.290 / 20.4599 | 0.333 / 0.0342 | 78600 / 654 |
| graph-512 | 118.043 / 81.6685 | 0.260 / 0.0341 | 311525 / 654 |
| history-64 | 7.191 / 2.7093 | 0.347 / 0.0341 | 10782 / 655 |
| pruned-4 | 6.932 / 2.7093 | 0.339 / 0.0341 | 10782 / 655 |

The snapshot is the full tagged POI transport with `IncludeVersionMetadata: true`, matching the
payload used inside a CBOR archive. It differs from `hash-cbor`, which hashes the untagged static
representation. The ChangeSet is a prepared one-EVSE `maxPower` update with a real Ed25519 peer
signature. Preparation and signing are excluded. No long-batch/multi-peer scaling is measured.

| Snapshot operation | graph-128 ms | graph-128 allocation MiB | graph-512 ms | graph-512 allocation MiB |
| --- | ---: | ---: | ---: | ---: |
| snapshot-cbor | 50.290 | 20.4599 | 118.043 | 81.6685 |
| snapshot-json-document | 38.053 | 14.1907 | 83.051 | 56.6690 |
| snapshot-reading-paths | 1.077 | 0.2632 | 1.773 | 1.0519 |
| snapshot-json-canonical | 2.496 | 1.0579 | 6.446 | 4.2737 |
| snapshot-cbor-tree | 3.482 | 2.3313 | 12.096 | 9.2924 |
| snapshot-etag-tree | 2.377 | 1.2371 | 7.659 | 4.9065 |
| snapshot-cbor-write | 2.156 | 1.3794 | 2.277 | 5.4748 |

The document stage builds the tagged JSON hierarchy and computes every declared child ETag.
The path stage validates schema-owned SI readings. Canonical JSON then feeds a metrological
CBOR value tree, whose schema-owned ETags are converted to binary tuples before deterministic
Styx encoding. The final writer stage includes deterministic map scratch/output allocation.

Each stage receives prebuilt inputs and retains its actual result. After every warmup/sample,
its consuming encoder checks exact expected bytes outside timing/allocation measurement. The
complete POI/ChangeSet calls include encoding/counting/SHA-256; their actual bytes are also
checked after timing. Intermediate tree digests refer to textual ETags and therefore differ
from the final binary-tuple digests. Path-only results bind sorted schema path names/counts.
Stage medians must not be added or treated as percentages of complete call costs: validation,
prebuilt inputs, digest work, warmup/cache/tiering and output lifetimes differ.

`EncodingProbe` binds the existing non-public schema and transport methods to typed delegates
once during setup, without changing the library API. Changed signatures fail. The snapshot
path loop mirrors the production loop using its real visitor/measurement predicate; this is an
isolated stage probe, not a heap object-type trace or a production profiler.

## Selected next package: reduce tagged snapshot document work

This selection is now implemented and verified in [the tagged-document follow-up](TAGGED-DOCUMENT-OPTIMIZATION.md).
The baseline tables/source hashes below retain their original measurement inputs.

The tagged document stage has the largest measured allocation volume among the isolated snapshot
stages: **56.67 MiB** at 512 EVSEs, versus
**81.67 MiB** for the complete tagged snapshot call. Code inspection of
[POIRepresentation](../WWCP_POI/Serialization/POIRepresentation.cs) explains a concrete candidate:
the whole document is prepared first, then each child subtree is deep-cloned and prepared again
before computing its two ETags. This is evidence for investigating that repeated work, not a
measurement of how many bytes any individual clone allocates or a promised improvement.

1. Remove redundant cloning/preparation where a static subtree is already prepared, before
   its own or descendant ETags are attached. Keep runtime-export projections, root revision
   metadata, nested tagged values, private-key exclusions and customer fields under the exact
   existing rules. Child hashes continue to describe their full static content.
2. Preserve byte-for-byte JSON/CBOR output, binary ETag tuples, SI tags, canonical numbers,
   signature inputs and combined depth failures. Reuse the 149 stream cases, fixed references
   and complete regression suite for the production change.
3. Rerun this exact 17-operation baseline with matching warmup/sample/process counts; use the
   existing comparison command to reject changed identities or sizes and record mixed timings.
4. Consider deeper digest reuse or direct per-value encoding only after measuring this smaller
   change. Any cache must bind the complete immutable owned subtree, bound its lifetime/memory,
   and preserve runtime/revision exclusions; it must not introduce a new Merkle hash profile.

No production optimization is implemented in this measurement package. ChangeSet encoding is
small for the single-operation fixture; longer batches need independent evidence before being
prioritized. Replay/streaming decode, append-only storage, larger shared-reference/tariff/parking
graphs, merged histories and concurrent/production traces remain separate work.

## Reproduction and verification scope

The [benchmark README](../WWCP_POI_Benchmarks/README.md#individual-encoder-and-borrowed-stream-baseline)
contains the exact Release build/run/summary commands. Use a new output path; reports are never
overwritten. Stage inputs, expected output and the preceding result remain live during setup or
until replacement. The 10 ms memory sampler misses brief peaks; reported managed/working-set
peaks and lifetime peaks must not be read as isolated encoder working memory. Thread allocation
counts synchronous operation work; CPU includes the sampler and can be quantized for short calls.
No confidence interval, affinity control, cold OS-cache reset or production capacity claim is supplied.

The Release benchmark build completed with zero warnings/errors. A 17-operation smoke run and
the full baseline completed; worker output checks and the generated summary checks passed.
The library source/assembly hashes match [the prior NUnit verification](VERIFICATION-STREAMING.md)
with 149 targeted cases and 1,174 full-suite passes. NUnit was not rerun for this benchmark-only
package; the existing result remains evidence for that unchanged production binary.

The subsequent [direct POI CBOR encoder](DIRECT-POI-CBOR.md) removes the complete POI
CBOR/native-ETag trees. Snapshot path/tree/write microstages above remain historical references,
not stages of that new production pipeline. Complete snapshot/archive operations and the paired
cold/warm reports measure that production output. The subsequent
[direct ChangeSet encoder](DIRECT-CHANGESET-CBOR.md) now also removes its complete transport trees;
all old tree microstages remain historical references. Long batches/multiple peers have separate evidence.

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
