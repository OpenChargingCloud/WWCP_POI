# Bounded child ETag reuse

[Repository overview](../README.md) · [ETags and CBOR](ETAGS-CBOR.md) · [Scaling](SCALING.md) · [Roadmap](ROADMAP.md)

Repeated tagged JSON/CBOR exports of the same immutable static snapshot now reuse the declared
children's JSON/CBOR SHA-256 pairs. The root already had its own immutable digest pair. This
optimization changes neither the static-v2 content profile nor any canonical preimage, digest,
SI unit, signature or transport byte. Standalone entity/value exports continue to compute their
child declarations using the existing value codecs.

## Context, bounds and lifetime

Each `RoamingNetworkDataSnapshot` owns a lazily initialized internal context. Its constructor
binds the complete immutable entity map, root and fixed content profile. Within that context,
the key is the schema kind and complete ordinal JSON pointer used by the deterministic export.
Stable owner IDs or direct child identities alone do not bind changed descendant content.
The private snapshot-only revision path shares the context with its unchanged map/root; every
ordinary changed snapshot starts a fresh context. There is no global cache of snapshots or IDs.

The context retains at most **4,096 pairs**, **1,048,576 logical payload bytes**, and **1,024
UTF-16 characters per combined kind/path key**. Payload accounting is exactly two 32-byte digests
plus twice the retained key character count. Managed object headers, ETag arrays, dictionary
capacity and the lock are additional; the payload budget is not a 1 MiB total managed-heap cap.
The entry and key limits separately bound that overhead. The benchmark records an approximate
post-collection managed-heap delta as well as exact logical counts.

Admission keeps the first schema traversal entries, which include large ancestor subtrees.
Once a budget is reached, remaining pairs are computed normally without being retained. Existing
entries are not evicted during a live context, avoiding a full sequential-export eviction cycle.
There is no timer, TTL, background work or application-facing cache configuration. When its last
snapshot/network/history owner is released, the entire context can be collected. Snapshot-only
revisions keep their shared context alive. Total cache memory scales with the number of distinct
retained static versions; these are per-context bounds, not a global history memory guarantee.

Only immutable `ETag` pairs are retained. Returned JSON objects, CBOR buffers, static canonical
buffers, callbacks and runtime overlays are not cached. Locking covers lookup, admission and
statistics; hashing occurs outside the lock. Simultaneous cold misses can repeat work, but retain
only one successful pair per key. A failed computation cannot install an entry or poison retries.

## Runtime and representation options

Runtime statuses/readings and revision metadata never enter cached static child pairs. Each
export still constructs its current transport view; cache hits attach newly constructed ETag
arrays using the requested HEX or Base64 display. A cold runtime-inclusive export hashes a fresh
static projection; subsequent exports reuse that static result while reflecting new runtime
values. A `DataSnapshot` still rejects runtime-inclusive export because it owns no runtime state.
Changing a returned transport object cannot affect future exports or cached pairs.

The visitor still performs deterministic per-document schema traversal, metrological validation,
canonicalization and native ETag emission when required by the selected codec. The later
[direct POI encoder](DIRECT-POI-CBOR.md) removes the complete CBOR/conversion trees; the reports
below record the preceding cache package's tree codec. A hit skips
only the child's repeated static JSON/CBOR hashing. Whole-state result hashes, immutable updates,
individual output buffers, archive rewrite/replay and runtime materialization retain their existing
costs. Reuse is across unchanged versions; a descendant change does not copy caches from older maps.

## Measurements

The [before](performance/child-etags-before.json) and [after](performance/child-etags-after.json)
reports use the same benchmark source and deterministic graph/history inputs: four shapes, ten
operations, two fresh processes, five warmups and five measured samples per process. Each report
contains **80 workers / 400 samples**. The [checked comparison](performance/child-etags-comparison.md)
verifies matching bytes/digests and inventories for every operation. The
[cache summary](performance/child-etags-summary.md) records cold/warm allocation, timing and retention.

For 512 EVSEs, repeated tagged document allocation falls from **43.3865 to 10.2250 MiB
(−76.4%)** and repeated complete snapshot CBOR from **68.3860 to 35.2245 MiB (−48.5%)**.
Cold allocation rises slightly: **43.5118 MiB (+0.3%)** for the document and **68.5113 MiB
(+0.2%)** for complete CBOR. This separates reuse benefits from first-admission costs.
The 512-EVSE context retains **1,282 pairs / 308,666 logical payload bytes**; its separate
post-collection managed-heap delta is about **504 KiB**, including overhead. At 128 EVSEs
it retains **322 pairs / 77,130 logical bytes**, with about **125 KiB** of managed delta.
These are measurements of these prepared immutable snapshots, not a global history bound.

The four new operations prepare a fresh parsed immutable map and its root digests outside each
cold warmup/sample. Warm operations prepare one map and fill its child cache before measurement.
Cold and warm samples both exclude import and initial root hashing. The document stage consumes
canonical bytes after measurement; complete CBOR calls count/hash their output within it. Every
warmup and sample compares actual output bytes with the original source's expected representation.

Retention measurement holds the same map/root pair alive, discards its first tagged export,
and compares fully collected process-managed memory before/after that export. It initializes
reflection and the empty context before the first collection. This approximate delta is separate
from allocation volume and exact retained key/digest payload; it is not an isolated object-size
measurement, RSS guarantee or a bound on the complete history. The small `history-64` fixture
has a negative median heap delta despite 42 retained pairs: unrelated setup objects can become
collectible between readings. This is measurement noise, not a negative cache memory cost. The
512-EVSE delta ranges about 424–504 KiB; the 504 KiB headline is its median, not an exact object size. Existing snapshot operations use
their prebuilt stage setup; archives export all retained versions and may share snapshot contexts.

Runs use workstation GC on this Windows/.NET host, with no process affinity, exclusive machine
reservation, confidence intervals or production contention model. The before run overlapped local
builds and focused verification in parts. Timings are descriptive, not evidence of a general speed
guarantee. Allocation and exact outputs provide the clearer controlled comparison. The synthetic
performance shapes do not saturate the default context bounds; small-budget unit cases exercise
admission limits and preservation. Larger saturated/merged/multi-operator/reference/tariff/parking
workloads, long batches and production concurrency traces remain additional evidence.

## Verification and source binding

The new `ChildETagCacheTests` contain **20 passing cases**: eight cold/warm runtime/version/text
variants against the pre-cache projection algorithm; fresh runtime delivery; shared snapshot-only
revisions versus changed descendants; the unchanged runtime rejection contract; five exact/excess
entry/payload/key budgets; schema-kind/path separation and duplicate admission; invalid pair retry;
eight simultaneous exporters with four calls each; and complete context collection after release.
The reference exporter always hashes fresh static projections using the unchanged canonical/value
codecs; it never looks up the child cache. Fixed JSON/CBOR/signature/history references remain
unchanged and are rerun with the broader regression.

The combined focused run passes **169 cases** (20 cache + 149 stream/encoding/bootstrap),
with zero failures/skips. The complete Release run passes **1,194 cases**, zero failures,
one skipped child worker and two additionally excluded explicit generators (raw TRX total 1,197).
It reruns the existing real process-exit, fixed-byte, domain, culture, signature, merge and runtime
fixtures. The latest incremental test build has zero errors/four existing warnings; recompiling
the production library reports its 358 existing warnings. No reference regeneration was needed.

Initial test setup assumed ancestor record objects remained identical after a property update.
The existing applier touches ancestor timestamps and constructs new records. The corrected assertion
checks that owner/direct child identities stay the same while descendant content and its cache
context change. The production cache needed no correction from that failed assertion.

The [verification record](verification/child-etags-results.json) binds source, assembly, scoped
[production patch](performance/child-etags-cache.diff) and original TRX hashes. The patch reconstructs
the preceding tagged-document production source without reverting earlier domain/streaming work.
Original TRX files stay in the ignored `WWCP_POI_Tests/bin/TestResults/child-etags/` directory.
Reference generators remain explicit and excluded from ordinary runs; no vectors were regenerated.

```powershell
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --suite scaling --operations child-etags-json-cold,child-etags-json-warm,child-etags-cbor-cold,child-etags-cbor-warm,snapshot-json-document,snapshot-cbor,archive-json,archive-json-stream,archive-cbor,archive-cbor-stream --processes 2 --warmups 5 --samples 5 --label child-etags-rerun --output WWCP_POI_Benchmarks/bin/child-etags-rerun.json
./WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.exe --compare-before docs/performance/child-etags-before.json --compare-after WWCP_POI_Benchmarks/bin/child-etags-rerun.json --summary WWCP_POI_Benchmarks/bin/child-etags-comparison.md
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~ChildETagCacheTests|FullyQualifiedName~ArchiveStreamingTests|FullyQualifiedName~BootstrapStreamingTests|FullyQualifiedName~ArchiveStreamingFailureTests' --logger 'trx;LogFileName=child-etags-focused.trx' --results-directory WWCP_POI_Tests/bin/TestResults/child-etags --nologo
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=child-etags-full.trx' --results-directory WWCP_POI_Tests/bin/TestResults/child-etags --nologo
```

Choose fresh report/TRX destinations for reruns. The saved before executable was built while its
production source was still the preceding version, then retained in a separate ignored output
directory; current binaries cannot reproduce a historical before run without restoring that source
in a separate checkout. Preserve recorded historical results rather than overwriting them.
