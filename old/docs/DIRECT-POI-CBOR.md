# Direct deterministic POI CBOR encoding

[Repository overview](../README.md) · [ETags and CBOR](ETAGS-CBOR.md) · [Scaling](SCALING.md) · [Roadmap](ROADMAP.md)

The individual POI encoder writes from a prepared canonical JSON view to the Styx CBOR writer.
It no longer constructs the complete intermediate CBOR tree or a second tree for native ETags.
The static-v2 profile, canonical JSON/CBOR preimages, SI values, digests, signatures and transport
bytes remain unchanged. The public byte-array APIs and CBOR decoder retain their existing contracts.

## Encoding and memory

`POIRepresentation.Encode` first validates declared metrological string fields using the existing
schema visitor. This preserves the order of validation before canonical JSON rejects unsupported
customer values. Styx canonical JSON bytes provide the exact number/string/date/byte spelling;
a `JsonDocument` indexes that buffer without creating a complete CBOR value graph.

`POICBORWriter` emits definite containers through one Styx writer with depth 64 and preferred
scalar encodings. Each map's complete encoded text keys are sorted lexicographically and checked
for duplicates before emission. Styx's `Deterministic` flag is deliberately off here: its internal
map sorter buffers a complete map, so ordering and uniqueness are supplied by this adapter.
Integer widths, headers, scalar/tag encoding and writer depth accounting still belong to Styx.
The resulting bytes match `CBORWriterOptions.Canonical`, not the source JSON property order.

Int64/UInt64 JSON integer tokens use direct writer methods. Other numeric tokens pass individually
through Styx's exact JSON number conversion, preserving decimal scale, exponent bounds and bignums;
there is no binary-floating conversion. Declared SI strings use Styx tag 44252 and its small
metrological value representation. Any maps within those small values are sorted too.
Declared ETag pairs are validated and emitted as `[format, algorithm, digestBytes]` without a
complete conversion tree. Only immediate schema-owned object children receive that interpretation;
customer strings, nested customer arrays and customer properties named `ETags` remain ordinary data.

One encoder can retain at most **128 encoded text keys / 16,384 encoded key bytes** for reuse
during its call. Excess keys are encoded without retention. Map-field sorting arrays and keys
still scale with the currently open maps. This temporary dictionary is independent of the
snapshot's bounded immutable child-digest cache and has no global or snapshot lifetime.

Complete canonical JSON bytes, the `JsonDocument` index, the complete `ArrayBufferWriter` output
and its final byte-array copy still exist. Small scalar/metrology CBOR trees remain. This package
removes complete CBOR trees; it does not establish a fixed total-memory bound or stream a POI
payload directly into an archive. Root hash computation uses this writer, but the paired cold/warm
measurements below exclude initial root hashing and import.

## Archive and failure contracts

Archive envelopes still call the individual byte-array codec, then structurally validate that
encoded value at the remaining outer CBOR depth before copying it. This preserves the archive
reader's combined container/tag depth accounting and existing borrowed-stream partial-output rules.
Publish/store/retention/bootstrap persistence continues to flush and install its temporary file
before changing the in-memory state. No archive writer, decoder, ChangeSet codec, content profile
or fixed reference artifact changes in this package.

The new direct-writer tests compare against the previous complete-tree algorithm independently
of `Encode`. Existing archive fixtures independently assemble their envelopes and fixed reference
tests bind the historical signed bytes. The internal writer is not a public stream API.

## Measurements

The [completed cache report](performance/child-etags-after.json) is the before input; the
[direct-writer report](performance/direct-poi-cbor-after.json) is after. The
[complete checked comparison](performance/direct-poi-cbor-comparison.md) verifies all outputs
and inventories. The [paired summary](performance/direct-poi-cbor-summary.md) additionally
checks identical cold/warm cache statistics and records collected retention and sampled peaks.

For **512 EVSEs**, median synchronous allocated bytes per complete call are:

| Operation | Before MiB | After MiB | Change |
| --- | ---: | ---: | ---: |
| Cold tagged JSON document | 43.5118 | 39.0182 | −10.3% |
| Cold complete CBOR | 68.5113 | 49.5577 | −27.7% |
| Warm complete CBOR | 35.2245 | 20.8682 | −40.8% |
| Complete snapshot CBOR | 35.2245 | 20.8330 | −40.9% |
| Buffered CBOR archive | 109.3138 | 66.0307 | −39.6% |
| Streamed CBOR archive | 106.1826 | 63.0076 | −40.7% |

Cold JSON improves because calculating each new child's CBOR digest also uses the new encoder.
Warm JSON and JSON archives retain their existing implementation: small changes in allocated
bytes reflect allocation/pool variability, not a JSON algorithm change. At 128 EVSEs complete
snapshot allocation is **8.8600 → 5.2565 MiB (−40.7%)**. The smaller history/pruned fixtures
are recorded separately and retain their existing ordered signed batches/receipts.

The 512-EVSE context still retains exactly **1,282 pairs / 308,666 logical bytes**. Its median
collected managed delta remains **504.38 KiB**; the observed after range is **344.27–504.38 KiB**.
Those heap deltas include unrelated collectible setup objects and do not isolate cache object
sizes. Per-writer key reuse ends with the encoding call. No new persistent/global cache is added.

The sampled managed peak for complete snapshot CBOR is **29.64 → 27.93 MiB**, and streamed
archive **40.13 → 30.44 MiB**. Cold CBOR working-set peak rises **154.84 → 166.44 MiB** even as
allocation falls. Peaks include setup/live expected results and the sampler misses short peaks;
these results establish neither a fixed memory cap nor uniformly lower process memory.
Timings remain mixed: graph-512 complete CBOR median improves, while cold JSON and some history
archive medians rise. The allocation reduction is the supported optimization result.

Before is the completed cache build, not the earlier build without a cache. Both reports use
four shapes, ten operations, two fresh workers per operation, five warmups and five measured
samples: **80 workers / 400 samples each**. The C# harness, host/runtime/dependencies, inputs,
identities and all output byte counts/digests match. No build or test ran alongside either report.
The host is not dedicated and no affinity is set; timings/CPU/peaks remain descriptive.

The unchanged `EncodingProbe` still retains the former tree stages as reference microbenchmarks.
Those isolated stages no longer describe the production POI pipeline. The subsequent
[direct ChangeSet encoder](DIRECT-CHANGESET-CBOR.md) also turns the old ChangeSet tree stages
into historical references. The selected complete snapshot/archive operations and cold/warm operations measure the
new production code. Stage medians must not be added to infer complete-call costs.

## Verification and reproduction

On **2026-10-09**, Release verification passes **228 focused cases** (59 new direct-encoder,
20 child-cache, 149 archive/stream/bootstrap) and **1,253 full cases**, zero failures and one
ordinary skipped crash worker. Both explicit reference generators remain excluded. The full run
includes the existing real process-exit, persistence, retention, bootstrap, culture, signature,
merge, reference and domain-model fixtures; reference artifacts were not regenerated.

The new cases cover eight runtime/version/digest-display variants, every metrological schema
kind, prefix/uncertainty values, exact decimal/exponent/bignum grammar, canonical scalar spelling,
Unicode key order, schema/customer boundaries, definite counts, key-cache saturation, container/
tag depth, malformed declarations, duplicate keys, exponent bounds and validation ordering.
The [source/assembly/TRX/report evidence](verification/direct-poi-cbor-results.json) binds this
binary to the measurements and [scoped production patch](performance/direct-poi-cbor.diff).
The before source can be reconstructed by excluding the new writer and restoring the recorded
pre-change encoder; its fingerprint and saved assembly match the reused baseline report.

The initial test setup compared a tagged/versioned view with an untagged canonical view, flattened
an intended nested `JArray`, and used a compound-unit string that the current Styx text parser
does not accept. Correcting those test inputs required no production change. The final reference
comparison uses the existing prepared content projection and a previous-tree encoder.

```powershell
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~DirectPOICBORTests|FullyQualifiedName~ChildETagCacheTests|FullyQualifiedName~ArchiveStreamingTests|FullyQualifiedName~BootstrapStreamingTests|FullyQualifiedName~ArchiveStreamingFailureTests' --logger 'trx;LogFileName=direct-poi-cbor-focused.trx' --results-directory WWCP_POI_Tests/bin/TestResults/direct-poi-cbor
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=direct-poi-cbor-full.trx' --results-directory WWCP_POI_Tests/bin/TestResults/direct-poi-cbor
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite scaling --operations child-etags-json-cold,child-etags-json-warm,child-etags-cbor-cold,child-etags-cbor-warm,snapshot-json-document,snapshot-cbor,archive-json,archive-json-stream,archive-cbor,archive-cbor-stream --processes 2 --warmups 5 --samples 5 --label direct-poi-cbor-after --output WWCP_POI_Benchmarks/bin/direct-poi-cbor-rerun.json
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --compare-before docs/performance/child-etags-after.json --compare-after docs/performance/direct-poi-cbor-after.json --summary WWCP_POI_Benchmarks/bin/direct-poi-cbor-comparison-rerun.md
python WWCP_POI_Benchmarks/direct_poi_cbor_report.py --before docs/performance/child-etags-after.json --after docs/performance/direct-poi-cbor-after.json --output WWCP_POI_Benchmarks/bin/direct-poi-cbor-summary-rerun.md
```

Use fresh output paths; preserve the checked-in raw reports and byte/signature references.
The focused scope includes all cache/stream cases. Larger saturated, merged, multi-operator,
tariff/parking workloads, long signed batches, concurrent production traces and an independent
implementation remain additional evidence. Later [direct archive payload emission](DIRECT-ARCHIVE-PAYLOAD.md)
preserves combined-depth and partial-failure contracts. Individual ChangeSet encoding is also
[implemented separately](DIRECT-CHANGESET-CBOR.md); its archive output buffer remained in that
build. [Direct ChangeSet archive payloads](DIRECT-ARCHIVE-CHANGESET.md) subsequently removed it.
[Indexed recovery](INDEXED-ARCHIVES.md) now removes the complete archive CBOR decoding tree;
input bytes, individual trees and retained replay states remain.
