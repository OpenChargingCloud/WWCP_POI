# Bounded POI archive preflight reuse

[Repository overview](../README.md) · [Direct archive payloads](DIRECT-ARCHIVE-PAYLOAD.md) · [Roadmap](ROADMAP.md)

## Scope and algorithm

This package reduces the additional allocation introduced by direct checkpoint/snapshot POI
emission in all four CBOR archive profiles. Each complete prepared payload still passes all
semantic checks before its first output byte. Standalone Styx writer limits and the archive
reader's remaining `SkipValue` depth remain distinct. Bytes, static-v2 identities, peer
signatures, runtime lifetimes and persistent publication are unchanged.

- Decode each map property name once during preflight.
- Reuse uniqueness sets at the same JSON nesting depth after completing a map. Active parents
  occupy distinct slots. The 64-slot pool retains sets only for maps of at most 128 names;
  larger maps are validated normally and their oversized set is discarded. A `finally` clears
  name references on success and failure. A new `Validate` also resets any prior reader error.
- Parse and validate every schema ETag pair fully. Its native representation has exactly two
  array levels containing text/text/32 digest bytes. When both depth budgets fit, this proof
  avoids creating a redundant ETag CBOR tree. Near a boundary, the existing actual Styx
  writer/reader fallback still decides acceptance and failure ordering.
- Reuse successfully parsed metrological scalar trees by exact ordinal SI text within one POI
  archive payload call, shared between preflight and emission. Schema ownership is checked
  before every lookup; ordinary customer strings/arrays remain ordinary values. Each occurrence
  checks depth again. A shallow successful occurrence cannot authorize a deeper one.

The earlier measurement validation before canonical JSON remains in place, preserving invalid
SI versus unsupported customer-value error priority. ETag parsing is not skipped or cached as
an authorization proof. Decimal/bignum scalar codecs and complete standalone POI/ChangeSet
encoders retain their preceding paths. The writer's optional reading context is used only by
the archive path; no public model/API or transport profile changes.

## Bounds and lifetime

| Reading cache admission | Limit |
| --- | ---: |
| Successful distinct texts per POI payload call | 32 |
| UTF-16 characters per input text | 256 |
| Total retained input characters | 4,096 |
| CBOR tree nodes per entry, including map keys and tags | 128 |
| Text characters or byte-string bytes per scalar node | 256 |

The first admissible entries remain available when later admission is rejected. Rejected/invalid
readings are not retained; valid oversized readings still encode normally. The cache stores no
JSON element, schema path, destination, runtime or cross-call reference. Only successful scalar
codec results are reused. The private trees are never mutated by preflight/emission. The context
becomes collectible when that synchronous payload call returns or fails; separate concurrent
exports have separate contexts.

These are logical admission limits, not exact managed-heap limits. The uniqueness pool clears
all string references but retains bounded table capacities. Large active customer maps, rejected
scalar trees, complete canonical JSON/index, sorting arrays and graph preparation can still
allocate more. There is no total transient-memory or history-memory bound. Whole archive buffers
remain absent from borrowed output. That build retained complete batch buffers, now removed by
[direct ChangeSet archive emission](DIRECT-ARCHIVE-CHANGESET.md); metadata/decoder trees and public
`ToCBOR` result buffers remain.

## Verification

Release on **2026-10-09** passes **494 focused cases** and **1,519 full cases**, with zero failures
and one ordinary skipped worker. Both reference generators remain explicit and excluded.
The **71 new cases** independently compare the unchanged buffered writer followed by actual
Styx `SkipValue` against reuse at 0/1/4 enclosing levels, 59..64 JSON depths, metrological values
and native ETag tuples transported through HEX/Base64 JSON. They also check escaped/ancestor
duplicate keys, name-pool exact/excess limits and sibling/error cleanup, reader-failure retry,
schema/customer separation, cache hits/overflow/invalid retry, exact/excess input and total text
budgets, and exact/excess CBOR node/text/byte admission limits.

All 140 earlier archive-payload cases rerun, including four-profile exact bytes, original peers,
recovery/runtime, rejected payload output prefixes, real persistent state-depth failures,
temporary cleanup, unchanged disk/head/runtime and signed/destination retries. The 55 direct
ChangeSet, 59 direct POI, 20 child-context and 149 stream/bootstrap cases also pass. The full
suite reruns model/culture/merge/signature/reference and actual-process crash coverage.
Fixed references are not regenerated.

The first new schema test accidentally used `evses` instead of the declared `EVSEs` field.
Correcting that test restored its intended two-reading assertion; production required no
correction. Six explicit tree-admission cases were added before the final build and both final
runs. The [source/assembly/TRX/report evidence](verification/preflight-allocation-results.json)
and [scoped production patch](performance/preflight-allocation.diff) bind this verified binary.

## Measurements

The checked-in [previous completed after report](performance/direct-archive-payload-after.json)
is reused as the before measurement, with its exact production source/assembly reconstructed
and checked. The [new after report](performance/preflight-allocation-after.json) uses the unchanged
C# harness, dependencies, runtime and method. Each report contains 80 fresh workers and 400
samples: four existing shapes, ten operations, two processes/five warmups/five samples.
Every output byte count/digest, static/history inventory and bounded child-cache state agrees.
See the [complete comparison](performance/preflight-allocation-comparison.md) and
[checked allocation/time/peak/retention summary](performance/preflight-allocation-summary.md).

| Streamed CBOR workload | Before allocation | After allocation | Change |
| --- | ---: | ---: | ---: |
| graph-128 | 19.2402 MiB | 17.0269 MiB | -11.5% |
| graph-512 | 74.8704 MiB | 66.0324 MiB | -11.8% |
| history-64 | 7.5126 MiB | 7.0450 MiB | -6.2% |
| pruned-4 | 6.1289 MiB | 5.6612 MiB | -7.6% |

At 512 EVSEs the preceding snapshot-buffering baseline was **62.9589 MiB**; direct archive
payloads first raised this to **74.8704 MiB**. This package changes it to **66.0324 MiB**
(-11.8% against direct payloads, +4.9% against the earlier buffering build).
The improvement concerns complete measured allocation, not an inferred stage percentage.
Canonical JSON/index and preparation remain substantial costs.

Allocation varies in unchanged controls too: streamed JSON at 512 EVSEs changes 4.4525 ->
4.3467 MiB (-2.4%) and standalone snapshot CBOR 20.8683 -> 20.8330 MiB (-0.2%). Interpret the
archive medians alongside these controls. Sampled managed peaks also move in both directions:
graph-128 streamed CBOR rises 10.42 -> 14.03 MiB, while graph-512 falls 34.40 -> 28.25 MiB.
The allocation result does not establish a general peak-memory reduction.

Complete/streamed JSON archives, standalone snapshot CBOR and cold/warm child-context exports
provide unchanged-code controls. Archive/bootstrapping workloads use prepared retained histories;
the cold labels describe the separate snapshot controls, not cold archive replay. Complete output
and SHA-256 are inside the timed/allocation window; fixture construction, signing and verification
are setup. The independent tests compare actual bytes as well as the recorded output digests.

No task build/test overlaps the new after series. Other local .NET work was observed during this
task; this is not a dedicated host or affinity-controlled run. Elapsed times, sampled managed and
working-set peaks and approximate collected cache deltas remain descriptive, not a throughput,
peak-memory or production-capacity guarantee. Cache retention measurements describe the unchanged
snapshot child cache; the new scalar context ends with each payload call.

## Reproduction and next package

```powershell
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~POICBORPreflightReuseTests|FullyQualifiedName~DirectArchivePOICBORTests|FullyQualifiedName~DirectChangeSetCBORTests|FullyQualifiedName~DirectPOICBORTests|FullyQualifiedName~ChildETagCacheTests|FullyQualifiedName~ArchiveStreamingTests|FullyQualifiedName~BootstrapStreamingTests|FullyQualifiedName~ArchiveStreamingFailureTests' --logger 'trx;LogFileName=preflight-allocation-focused-rerun.trx' --results-directory WWCP_POI_Tests/bin/TestResults/preflight-allocation-rerun
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=preflight-allocation-full-rerun.trx' --results-directory WWCP_POI_Tests/bin/TestResults/preflight-allocation-rerun
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite scaling --operations archive-json,archive-json-stream,archive-cbor,archive-cbor-stream,snapshot-cbor,child-etags-json-cold,child-etags-json-warm,child-etags-cbor-cold,child-etags-cbor-warm,bootstrap-export --processes 2 --warmups 5 --samples 5 --label preflight-allocation-rerun --output WWCP_POI_Benchmarks/bin/preflight-allocation-rerun.json
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --compare-before docs/performance/direct-archive-payload-after.json --compare-after docs/performance/preflight-allocation-after.json --summary WWCP_POI_Benchmarks/bin/preflight-allocation-comparison-rerun.md
python WWCP_POI_Benchmarks/direct_archive_payload_report.py --before docs/performance/direct-archive-payload-after.json --after docs/performance/preflight-allocation-after.json --output WWCP_POI_Benchmarks/bin/preflight-allocation-summary-rerun.md
```

Preserve raw reports and choose fresh outputs. [Direct ChangeSet archive emission](DIRECT-ARCHIVE-CHANGESET.md)
now removes per-batch buffers with exact signed scalar/native-header/depth and failure contracts.
The subsequent [recovery baseline](ARCHIVE-RECOVERY-COSTS.md) measures decoding/replay and defines
incremental-reader contracts. The subsequent [indexed reader](INDEXED-ARCHIVES.md) implements
validated ranges without the complete archive tree. [Borrowed input streams](ARCHIVE-INPUT-STREAMS.md)
now implement bounded capture/mapping/cancellation; larger domain
and production/concurrency workloads remain separate packages.
