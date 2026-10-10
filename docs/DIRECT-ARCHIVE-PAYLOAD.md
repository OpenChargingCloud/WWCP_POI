# Direct POI payloads in CBOR archives

[Repository overview](../README.md) · [Stream contracts](STREAMING-ARCHIVES.md) · [POI encoding](DIRECT-POI-CBOR.md) · [Roadmap](ROADMAP.md)

All four history profiles now emit their checkpoint and snapshot `State` POI values directly
into the borrowed CBOR archive output. The complete per-POI CBOR result buffer and final copy
are removed from this path. Public buffered POI/commit/pack APIs, exact static-v2 bytes, state
and history identifiers, signed inputs and ordered peer envelopes remain unchanged.

## Preparation, preflight and emission

`POIArchiveCBORWriter.POI` passes its remaining envelope depth to the internal
`POIRepresentation.WriteArchiveCBOR`. Preparation uses the same static tagged document,
revision metadata, schema metrology validation, canonical JSON and `JsonDocument` as buffered
POI encoding. No runtime values enter a retained snapshot or its static identity.

`POICBORPreflight` then visits that prepared document before any of its CBOR bytes are emitted:

- JSON containers retain the standalone Styx writer's depth limit, including empty containers.
- Definite empty containers consume no frame in the existing `CBORReader.SkipValue` check.
  Nonempty JSON ancestors contribute their actual depth to the remaining archive budget.
- Tags occur inside metrological/numeric leaves. Those small leaf structures and native ETag
  tuples use a conservative bound counting all ancestor tags and empty containers. If it
  fits both budgets, no leaf buffer is needed; otherwise the actual Styx canonical writer and
  `SkipValue` decide, preserving their differing tag accounting.
- Schema readings, supported exact numbers, duplicate properties and ETag pairs are checked
  before output. Customer strings and customer fields named `ETags` stay ordinary data.
- A remaining-reader-depth failure is retained until the standalone codec preflight completes,
  so it cannot hide a later standalone codec rejection. Metrology validation still precedes
  canonical JSON conversion as in the public buffered encoder.

The same prepared document is then emitted through `POICBORWriter` into the existing archive
buffer. Encoded-key ordering, definite counts, binary digests and metrological tags keep the
preceding transport bytes. Preparation and preflight do not write or flush the destination.

This replaces complete POI buffering plus the old `Encoded/SkipValue` pass. The preflight adds
a JSON traversal and scalar checks; its original per-map sets and leaf allocations were measured.
The later [bounded preflight reuse](PREFLIGHT-ALLOCATION.md) reduces these costs while retaining
these depth, validation and failure contracts.
It is not a streaming JSON parser or a fixed total-memory bound. Complete canonical JSON/index,
static graph projection, sorting arrays, child hashing on cold contexts, archive ordering and
application-metadata trees remain. That build also retained ChangeSet output buffers, now removed
by [direct ChangeSet archive emission](DIRECT-ARCHIVE-CHANGESET.md). Large individual scalars can
grow the pooled stream buffer. Public `history.ToCBOR()` still returns a complete archive.

## Stream and persistence boundaries

An invalid POI payload starts no payload output. Already emitted archive prefixes remain,
and the encoder discards its pending buffer on failure. New tests compare those prefixes
with the previous buffered-payload adapter. Stream write/resource errors may still interrupt
valid payload emission; callers discard partial output or retry to a fresh destination.
The encoder never closes, seeks or flushes the borrowed destination.

History export holds the existing gate. Failures do not change the installed head, immutable
inventory or runtime. Persistent mutations retain the writer lease, unique sibling temporary
file, durable flush, atomic replacement and state installation after replacement. No durability
or trust policy changes are introduced. Bootstrap still freezes bounded fragments during output;
final decoding/replay still materializes a complete archive and retained states.

## Verification

On **2026-10-09**, Release passes **423 focused cases**, including **140 new archive-payload
cases**, all 55 ChangeSet / 59 POI / 20 cache cases and all 149 archive/stream/bootstrap cases.
The complete suite passes **1,448 cases**, with zero failures and one ordinary skipped worker.
Both reference generators remain explicit and excluded. The
[source/assembly/TRX/report evidence](verification/direct-archive-payload-results.json) and
[scoped production patch](performance/direct-archive-payload.diff) bind the execution and paired reports.

Independent outer-envelope references retain standalone buffered commit/snapshot codecs.
The new depth comparisons use the unchanged buffered `POICBORWriter` followed by the actual
Styx `SkipValue`, without the new preflight. They exercise empty/nonempty containers, integers,
bignums, decimal tags and schema metrological tags at standalone and remaining-envelope limits.
Real snapshot payloads also compare exact successful bytes and failed prefixes, with one,
three and four enclosing maps. Malformed values cannot begin emission and support valid retry.
All four archive profiles are checked on borrowed segmented streams, with original peer
verification, fresh recovery runtime, partial write failure and exact retry. Real persistent
snapshot-state depth failures check the unchanged active file/head/runtime, exact temporary
cleanup and successful signed retry. Earlier encoding, persistence, crash and reference suites
run again; fixed signed references are not regenerated.

## Measurements

The [matched before](performance/direct-archive-payload-before.json) and
[after](performance/direct-archive-payload-after.json) reports retain every static/history
identity, output count/digest and cache inventory. See the
[complete comparison](performance/direct-archive-payload-comparison.md) and
[checked allocation/time/peak/retention summary](performance/direct-archive-payload-summary.md).

| Streamed CBOR workload | Before allocation | After allocation | Change |
| --- | ---: | ---: | ---: |
| graph-128 | 16.2299 MiB | 19.2402 MiB | +18.5% |
| graph-512 | 62.9589 MiB | 74.8704 MiB | +18.9% |
| history-64 | 6.8206 MiB | 7.5126 MiB | +10.1% |
| pruned-4 | 5.4362 MiB | 6.1289 MiB | +12.7% |

The complete per-POI output buffers were removed, but that initial preflight build increased
temporary archive allocation. The buffer-removal result is not a claim of lower total allocation
or peak memory. Canonical JSON/index and preflight coexist during emission; sampled peaks are
reported for both sides. Elapsed times also vary in the unchanged controls, so these runs do not
establish a throughput improvement. The later [bounded preflight package](PREFLIGHT-ALLOCATION.md) implements and measures reduced
repeated work before the implemented [ChangeSet archive buffer removal](DIRECT-ARCHIVE-CHANGESET.md).

Paired Release reports use the unchanged C# benchmark harness with four existing shapes:
128/512 EVSEs with snapshots, 64 changes with retained branches, and four pruning rounds.
Ten operations compare complete/buffered and borrowed JSON/CBOR archives, snapshot CBOR,
cold/warm child-context exports and frozen bootstrap output. Two fresh processes, five warmups
and five samples yield 80 workers / 400 measured samples per report. Setup is outside the
measurement window; complete outputs include SHA-256 and their byte counts/digests must agree.
The independent tests establish byte equality for all four archive profiles.

Cold/warm controls prepare maps/root hashes outside each call; only warm controls inherit a
populated child context. Archive workloads use the prepared retained histories and are not a
claim of cold archive replay. Cache counts/bounds and every static/history inventory must match.
No task build/test overlaps either report. No dedicated host or affinity is used; timings and
sampled managed/working-set peaks remain descriptive, can miss short peaks and include setup
objects. Collected cache retention is an approximate process heap delta, not a cache-size limit.

## Reproduction and next scope

```powershell
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~DirectArchivePOICBORTests|FullyQualifiedName~DirectChangeSetCBORTests|FullyQualifiedName~DirectPOICBORTests|FullyQualifiedName~ChildETagCacheTests|FullyQualifiedName~ArchiveStreamingTests|FullyQualifiedName~BootstrapStreamingTests|FullyQualifiedName~ArchiveStreamingFailureTests' --logger 'trx;LogFileName=direct-archive-payload-focused.trx' --results-directory WWCP_POI_Tests/bin/TestResults/direct-archive-payload
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=direct-archive-payload-full.trx' --results-directory WWCP_POI_Tests/bin/TestResults/direct-archive-payload
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite scaling --operations archive-json,archive-json-stream,archive-cbor,archive-cbor-stream,snapshot-cbor,child-etags-json-cold,child-etags-json-warm,child-etags-cbor-cold,child-etags-cbor-warm,bootstrap-export --processes 2 --warmups 5 --samples 5 --label direct-archive-payload-rerun --output WWCP_POI_Benchmarks/bin/direct-archive-payload-rerun.json
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --compare-before docs/performance/direct-archive-payload-before.json --compare-after docs/performance/direct-archive-payload-after.json --summary WWCP_POI_Benchmarks/bin/direct-archive-payload-comparison-rerun.md
python WWCP_POI_Benchmarks/direct_archive_payload_report.py --before docs/performance/direct-archive-payload-before.json --after docs/performance/direct-archive-payload-after.json --output WWCP_POI_Benchmarks/bin/direct-archive-payload-summary-rerun.md
```

Keep raw reports and use fresh output paths. Source/assembly/TRX/report fingerprints and a
scoped patch bind the verified binary and reconstruct the preceding production sources.
The added POI preflight work is reduced by [bounded per-call reuse](PREFLIGHT-ALLOCATION.md).
[Direct ChangeSet archive emission](DIRECT-ARCHIVE-CHANGESET.md) now removes the complete batch
CBOR buffer with exact signed scalar/header/depth and failure contracts.
Streaming decoding/replay, larger domain workloads and production concurrency remain separate.
