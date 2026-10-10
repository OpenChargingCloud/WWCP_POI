# Direct deterministic ChangeSet CBOR encoding

[Repository overview](../README.md) · [ChangeSet wire profile](CHANGESET-CBOR.md) · [Signatures](SIGNATURES.md) · [Roadmap](ROADMAP.md)

`RoamingNetworkChangeSet.ToCBOR()` now emits the complete batch through the shared deterministic
writer. It removes the complete transport value tree and the second native-header-ETag conversion
tree. The existing v2 signing profile, static-v2 identities, ordered operations, peer envelopes,
numeric/SI spellings and all transport bytes remain unchanged.

## Algorithm and retained costs

The complete serialized JSON document is validated before collecting schema reading paths and
starting CBOR emission. Duplicate signed properties and undefined values retain their existing
rejection rules. `MeasurementPaths()` still resolves entity/element kinds, whole added/removed
subtrees, property operations and optional old/new values using the existing schema.

The internal `POICBORWriter.WriteChangeSet` emits definite arrays/maps, sorts complete encoded
keys and checks uniqueness before each map. It uses the same Styx scalar/tag/depth writer and
per-call key reuse as the [direct POI encoder](DIRECT-POI-CBOR.md): at most 128 retained encoded
keys / 16,384 encoded key bytes. Sorting arrays/uncached keys still scale with open maps. Styx's
own complete-map sorting buffers are bypassed after ordering is supplied by the adapter.

Only root `BeforeETags` and `AfterETags` become validated binary digest tuples. Customer fields
with those names, or `ETags`, remain ordinary signed data. Operation, element-path and signature
arrays keep their order. Omitted payload keys and defined JSON null remain distinct.

Strings at declared reading paths and every number use the unchanged signed scalar codec.
An SI string becomes tag 44252 only when its exact text equals Styx's reconstructed text.
Canonical integer tokens use native integers/bignums; exactly recoverable decimals use tag 4.
Other spellings, including `1e0` and `-0`, keep the tag-262 embedded JSON Number wrapper.
Descriptions/metadata/customer strings never become physical readings. The scalar entry point
explicitly forbids containers, preventing an accidental return to complete CBOR trees.

Complete System.Text.Json bytes, their `JsonDocument` index, the complete output buffer and its
final byte-array copy remain. Reading paths, small numeric/metrological trees and wrapper bytes
remain per-call costs. The previous tree helpers still support application metadata and the
unchanged decoder/reference probes. This change does not remove every tree in archive/commit
code and does not add a public borrowed-stream ChangeSet API or a fixed total-memory bound.

The public signing bytes and all cryptographic primitives are unchanged. Parsing still checks
transport/model validity; signature authority and applying the declared result require the existing
explicit verifier and applier. Transport preserves JSON numeric precision; static property/domain
validators retain their existing ranges. Some transport-only payload fixtures deliberately exceed
the domain JSON Decimal range; arbitrary metadata numbers remain transportable.

## Verification

On **2026-10-09**, Release execution passes **283 focused cases** (55 new ChangeSet, 59 POI,
20 cache, 149 stream/encoding/bootstrap) and **1,308 full cases**, zero failures and one ordinary
skipped worker. Both reference generators remain explicit and excluded. The full regression
includes existing real crash exits, culture/signature/model/reference/merge/persistence cases.
The [source/assembly/TRX/report evidence](verification/direct-changeset-cbor-results.json) binds
the verified binary, paired measurements and [scoped patch](performance/direct-changeset-cbor.diff).

The previous complete-tree encoding/reference functions remain unchanged and are used independently
of the new complete `ToCBOR` path. Fixed signed reference artifacts are not regenerated.
New comparisons cover exact number tokens, SI text fallback, all nine operations, nested cable
slots beneath scoped connector entities, null/absence, customer/header ETags, Unicode/wide maps,
container/tag depth, duplicate rejection, scalar guards, zero/one/two/four peers and ordered
result application. Deep tagged batch metadata also exercises real persistent publish/store
failure, unchanged disk/head/runtime, exact temporary cleanup and successful retry in full and
pruned archive profiles. Existing POI/cache/stream/crash/merge/model suites run again.

Initial test inputs used an owned graph relation as an element path and attempted domain property
updates outside its Decimal range. They were corrected to the proper scoped connector owner and
transport-only precision checks. No production behavior was changed to accommodate those inputs.

## Measurements

The matched [before](performance/direct-changeset-cbor-before.json) and
[after](performance/direct-changeset-cbor-after.json) reports preserve every complete output byte,
digest, signed batch identity and source inventory. See the
[complete comparison](performance/direct-changeset-cbor-comparison.md) and
[checked size/peer summary](performance/direct-changeset-cbor-summary.md).

| Ordered operations | Peer signatures | Before CBOR allocation | After CBOR allocation | Allocation reduction |
| ---: | ---: | ---: | ---: | ---: |
| 1 | 4 | 0.0540 MiB | 0.0433 MiB | 19.9% |
| 64 | 4 | 0.5258 MiB | 0.3585 MiB | 31.8% |
| 512 | 4 | 3.9408 MiB | 2.6224 MiB | 33.5% |
| 2,048 | 4 | 15.7482 MiB | 10.4835 MiB | 33.4% |

With one peer, the 2,048-operation batch falls from 15.7134 to 10.4531 MiB (33.5%).
The canonical JSON control has unchanged allocation in every matched shape. In the four-peer
2,048-operation fixture, median CBOR elapsed time is 46.074 -> 36.155 ms; these timings and
sampled process-memory peaks describe this run and do not establish production throughput or
capacity. Complete JSON/index/output buffering still grows with batch size.

The new `changesets` suite has batches of **1, 64, 512 and 2,048 operations**, with **one or
four real Ed25519 peer signatures**, using the published benchmark seed under distinct key IDs. Each operation is a static power/current/name/custom-data
update on up to 512 EVSEs. The largest fixture repeats that target round four times; this is
synthetic ordered bulk-update evidence. Schema additions, very large tariffs/parking payloads,
network/disk throughput, production concurrency and every signature algorithm are separate work.

Construction, immutable result preparation, signing, decoding, independent old-tree byte checks,
signature verification and result application happen in setup. Each complete measured output
includes its SHA-256 calculation and is checked byte for byte after every warmup/sample.
There are eight size/peer shapes, two operations, two fresh workers, five warmups and five samples:
**32 workers / 160 samples per report**, using the same C# harness before/after.

The JSON control emits canonical complete batch JSON; ordinary dictionary serializer order varies
across processes. It preserves numeric spellings. The existing `ArchiveIdentity` report field
binds complete signed batch CBOR in this suite; no retained history is constructed.
An initial raw-JSON control correctly failed cross-process digest comparison and its incomplete
report was preserved outside the checked-in evidence before fixing the control.

No task build/test overlaps either measurement series. There is no dedicated host or affinity;
times/CPU and sampled process memory remain descriptive. Peaks include live setup/reference inputs
and prior results, and short peaks can be missed. No new permanent cache is introduced.

## Reproduction and next scope

```powershell
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~DirectChangeSetCBORTests|FullyQualifiedName~DirectPOICBORTests|FullyQualifiedName~ChildETagCacheTests|FullyQualifiedName~ArchiveStreamingTests|FullyQualifiedName~BootstrapStreamingTests|FullyQualifiedName~ArchiveStreamingFailureTests' --logger 'trx;LogFileName=direct-changeset-cbor-focused.trx' --results-directory WWCP_POI_Tests/bin/TestResults/direct-changeset-cbor
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=direct-changeset-cbor-full.trx' --results-directory WWCP_POI_Tests/bin/TestResults/direct-changeset-cbor
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite changesets --processes 2 --warmups 5 --samples 5 --label direct-changeset-cbor-rerun --output WWCP_POI_Benchmarks/bin/direct-changeset-cbor-rerun.json
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --compare-before docs/performance/direct-changeset-cbor-before.json --compare-after docs/performance/direct-changeset-cbor-after.json --summary WWCP_POI_Benchmarks/bin/direct-changeset-cbor-comparison-rerun.md
python WWCP_POI_Benchmarks/changeset_batch_report.py --before docs/performance/direct-changeset-cbor-before.json --after docs/performance/direct-changeset-cbor-after.json --output WWCP_POI_Benchmarks/bin/direct-changeset-cbor-summary-rerun.md
```

Use fresh report paths; retain the checked-in raw reports. Before production source can be
reconstructed from the scoped patch and its recorded fingerprint; source/assembly/TRX/report
bindings are recorded with the results. Old ChangeSet tree microstages are now reference stages
alongside old POI stages, rather than the current complete transport pipeline.

The later [direct POI archive payload package](DIRECT-ARCHIVE-PAYLOAD.md) preserves combined
reader/writer depth, borrowed-stream failure and durable temporary-file installation. The subsequent
[direct ChangeSet archive package](DIRECT-ARCHIVE-CHANGESET.md) removes per-batch CBOR output buffers.
Prepared JSON/index/schema paths, buffered public APIs and decoding/replay remain separate costs.
