# Tagged POI document optimization

[Repository overview](../README.md) · [Encoder baseline](ENCODING-COSTS.md) · [ETags](ETAGS-CBOR.md) · [Tests](../WWCP_POI_Tests/README.md) · [Roadmap](ROADMAP.md)

Static tagged POI export now hashes the already prepared subtree before attaching its declarations.
It avoids repeating `DeepClone()` and `Prepare()` for every static tagged node. Full static
content hashes, existing JSON/CBOR bytes, SI values, signatures and profiles remain identical.

## Implementation and limits

[POIRepresentation.ToJSONWithETags](../WWCP_POI/Serialization/POIRepresentation.cs) first prepares
the complete fresh document. Its schema visitor runs from parent to children. At each callback,
the current node and descendants have no newly attached ETags/profile declarations. Therefore a
static export computes both tags directly from that prepared subtree, then appends its declarations.
It never hashes the declarations just attached to an ancestor, which is outside that subtree.

Runtime-inclusive export still clones/prepares its static hash projection, so local statuses and
measurements remain transported independently and cannot enter static hashes. Version-preserving
network/snapshot roots keep their existing cached content pair. A RoamingNetwork node whose
revision bookkeeping needs exclusion retains a separate static projection. Child/hash preparation,
customer fields, nested values, digest encoding and private-key exclusions keep the existing rules.

The change adds no representation/digest cache, changes no public API and retains no additional
output across versions. It reduces a repeated pass; child digests still encode their complete static
content and the final CBOR value/output still allocate. Runtime-inclusive exports receive no claim
of this allocation saving. Concurrent access retains the existing snapshot/runtime/gate contracts.

## Matching Release measurements

- [Before](performance/encoding-baseline.json): the original static-v2 encoder baseline.
- [After](performance/tagged-document-after.json): **136 fresh processes / 680 measured samples**,
  the same 17 operations/four shapes, five excluded warmups and five samples per worker.
- [Checked comparison](performance/tagged-document-comparison.md) rejects changed static identities,
  archive digests, operation digests, inventories or byte counts for every sample/process.
- [After stage summary](performance/tagged-document-summary.md) checks complete process/sample counts,
  paired buffered/stream outputs and final/document stage agreement, and reports sample ranges/CPU/GC.

| Shape | Operation | Before ms | After ms | Before allocation MiB | After allocation MiB | Allocation reduction |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| graph-128 | snapshot-json-document | 38.053 | 33.533 | 14.1907 | 10.8623 | 23.5% |
| graph-128 | snapshot-cbor | 50.290 | 46.391 | 20.4599 | 17.1315 | 16.3% |
| graph-128 | archive-cbor | 176.922 | 189.149 | 62.6910 | 52.7057 | 15.9% |
| graph-128 | archive-cbor-stream | 187.888 | 160.644 | 61.8892 | 51.9041 | 16.1% |
| graph-512 | snapshot-json-document | 83.051 | 71.201 | 56.6690 | 43.3865 | 23.4% |
| graph-512 | snapshot-cbor | 118.043 | 138.110 | 81.6685 | 68.3861 | 16.3% |
| graph-512 | archive-cbor | 370.434 | 338.936 | 248.5378 | 208.6901 | 16.0% |
| graph-512 | archive-cbor-stream | 369.874 | 336.727 | 245.5146 | 205.6671 | 16.2% |
| history-64 | snapshot-json-document | 6.086 | 4.549 | 1.8567 | 1.4312 | 22.9% |
| history-64 | snapshot-cbor | 7.191 | 5.971 | 2.7093 | 2.2838 | 15.7% |
| history-64 | archive-cbor | 49.666 | 45.307 | 17.5596 | 15.4318 | 12.1% |
| history-64 | archive-cbor-stream | 52.979 | 48.276 | 16.9450 | 14.8181 | 12.6% |
| pruned-4 | snapshot-json-document | 6.059 | 4.602 | 1.8567 | 1.4312 | 22.9% |
| pruned-4 | snapshot-cbor | 6.932 | 6.484 | 2.7093 | 2.2838 | 15.7% |
| pruned-4 | archive-cbor | 41.038 | 39.225 | 15.6842 | 13.5569 | 13.6% |
| pruned-4 | archive-cbor-stream | 46.660 | 36.631 | 15.3897 | 13.2620 | 13.8% |

At 512 EVSEs, document construction falls from **56.67 to
43.39 MiB** of temporary allocation
(**23.4%**); complete snapshot CBOR falls from
**81.67 to 68.39 MiB**
(**16.3%**). Allocation volume counts these synchronous operations
on the calling thread; it is not live memory or a capacity bound.

Times remain mixed, including complete values and unchanged stage/JSON/ChangeSet operations.
The full comparison records increases as well as decreases; there is no general speedup claim.
Processes start separately, and tiering/setup invocation counts, GC/cache and machine activity can
affect timing despite matching warmup counts. No JIT/production trace establishes a cause. Do not add
stage medians or present them as proportions of complete calls. Stage verification follows timing;
prebuilt inputs/previous results remain live and 10 ms memory sampling misses brief peaks.

Production source: `fc9d619c60f309b129f048251eee8f7b629d0d77ec8c7bc0201902156bde0ed0`.
Production assembly: `58c252259eaea5d7e5c1ddcc1adf9d9017e3d63f582567da5bb13ec1dc61188f`.
Benchmark source: `1f7d11023960393b23e38f759795b72c5c7ce27b56f278c39a20c7b070b99e2c`.

The benchmark source, dependencies, runtime/OS/GC configuration and measurement counts match the
before report. [The scoped one-file patch](performance/tagged-document-optimization.diff) records
only this production change; its reverse check succeeds. Reconstructing the previous file from
saved exact input bytes recovers source fingerprint
`882ef87016d841bbdf1f2e2684beba75d500e305b437d5083a0fe56811f66c3e` including the earlier uncommitted model/archive
packages. Historical reports/patches/reference vectors retain their original bytes.

## NUnit verification

| Run | Passed | Failed | Ordinary skipped | Local ignored TRX |
| --- | ---: | ---: | ---: | --- |
| Existing stream/encoding/bootstrap cases | 149 | 0 | 0 | `tagged-document-focused.trx` |
| Complete regression suite | 1,174 | 0 | 1 | `tagged-document-full.trx` |

The Release reruns on **2026-10-08** execute the optimized production binary with local Styx,
Hermod and WWCP_CoreData. No new test case or reference regeneration was needed. Test source
fingerprint remains `469fb273f771ad94017ba234e87ba8d18181ad51851d7f4f0f6b154035c1cfc4`. The full run also repeats fixed byte/crypto/culture
references, JSON/CBOR shared catalogs and parking, static/runtime/version options, merge/adoption,
depth failures, atomic persistence/retention/bootstrap recovery and the 65 actual process exits.
The existing [149-case coverage description](VERIFICATION-STREAMING.md#new-coverage) remains valid.
The ordinary skipped worker executes inside the crash cases; two explicit reference generators
are also not executed in raw TRX (1,177 total / 1,174 executed), separately from that skipped worker.

[Portable results](verification/tagged-document-results.json) bind counters/timestamps, local TRX
SHA-256, production/test source and assembly hashes and the optimization patch. Both test and
benchmark executions use the same optimized production assembly. TRX files stay in
`WWCP_POI_Tests/bin/TestResults/tagged-document/`. Release builds have zero errors; recompiling the
production library exposes its 358 existing warnings, and the test build has four existing warnings.
These local tests simulate named process exits, not arbitrary power loss or filesystem corruption.

## Reproduction

```powershell
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore -v quiet -clp:ErrorsOnly
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite scaling --operations snapshot-cbor,snapshot-json-document,snapshot-reading-paths,snapshot-json-canonical,snapshot-cbor-tree,snapshot-etag-tree,snapshot-cbor-write,changeset-cbor,changeset-json,changeset-validate-paths,changeset-cbor-tree,changeset-etag-tree,changeset-cbor-write,archive-json,archive-json-stream,archive-cbor,archive-cbor-stream --processes 2 --samples 5 --warmups 5 --label tagged-document-after-v2 --output WWCP_POI_Benchmarks/bin/tagged-document-after.json
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --encoding-report WWCP_POI_Benchmarks/bin/tagged-document-after.json --summary WWCP_POI_Benchmarks/bin/tagged-document-summary.md
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --compare-before docs/performance/encoding-baseline.json --compare-after WWCP_POI_Benchmarks/bin/tagged-document-after.json --summary WWCP_POI_Benchmarks/bin/tagged-document-comparison.md

dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore -v quiet -clp:ErrorsOnly
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~ArchiveStreamingTests|FullyQualifiedName~BootstrapStreamingTests|FullyQualifiedName~ArchiveStreamingFailureTests' --logger 'trx;LogFileName=tagged-document-focused.trx' --results-directory WWCP_POI_Tests/bin/TestResults/tagged-document --nologo
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=tagged-document-full.trx' --results-directory WWCP_POI_Tests/bin/TestResults/tagged-document --nologo
```

Use new report/TRX destinations to preserve executed evidence. Reversing the scoped patch in a
separate checkout restores the matched production baseline while keeping the same benchmark.
Do not run test/build workloads concurrently with performance workers.

## Completed follow-up: bounded immutable child-digest reuse

Tagged document construction remains the largest isolated snapshot allocation stage after this
change. Each export still computes every declared child's full JSON/CBOR pair again. Investigate
reusing those small pairs within the lifetime of an immutable snapshot context, with an explicit
entry/memory bound and independently measured first versus repeated export costs.

A key must bind the complete immutable entity map, root/schema path and fixed content profile.
An entity ID or own-node reference alone is insufficient: descendant changes can keep the owner's
own record while changing its subtree. Share only truly unchanged snapshot-only revisions; changed
maps must not inherit stale child digests. Include runtime/version/digest-text variants, derived
snapshots, concurrent exports, weak lifetime/eviction and retained-memory behavior in verification.
Cache immutable digest pairs rather than complete transport buffers or runtime views. This remains
a planned investigation; it introduces no new Merkle profile or mutable public state.

The [bounded child-cache package](CHILD-ETAG-CACHE.md) now implements this reuse with explicit
per-context admission/lifetime bounds, cold/warm/retained-memory evidence and targeted verification.
The selection above records the original plan; these measurements remain the preceding binary.

The subsequent [direct POI CBOR package](DIRECT-POI-CBOR.md) removes complete value/conversion
trees, adds 59 targeted cases and passes 228 focused / 1,253 full Release cases. The earlier
execution and its source/assembly evidence remain historical; fixed reference artifacts are unchanged.
