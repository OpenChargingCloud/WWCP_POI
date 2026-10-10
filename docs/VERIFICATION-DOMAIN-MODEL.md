# Shared domain model verification

[Repository overview](../README.md) · [Domain decisions](DOMAIN-MODEL.md) · [Tests](../WWCP_POI_Tests/README.md) · [References](interoperability/README.md)

This execution on **2026-10-08** checks the `wwcp-poi-static-v2` model with the actual POI
assembly and local WWCP_CoreData, Hermod and Styx dependencies, using Release/.NET 10 on Windows.
It covers the shared grid/software/document catalogs and parking relationships introduced by
the domain-model package. Historical snapshot and performance reports retain their original
static-v1 counts and digests. This execution predates the later streaming archive changes.
[Streaming evidence](STREAMING-ARCHIVES.md) records their Release build and matching static-v2
measurements. Their later [verification follow-up](VERIFICATION-STREAMING.md) adds 149 passing
cases and reruns the full suite: 1,174 passed, zero failures and one skipped worker. The original
counts below remain the domain-model execution record.

## Results and commands

| Run | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| Existing full suite after isolating the reference fixture | 952 | 0 | 1 | `full-initial.trx` |
| Final dedicated shared-model fixture | 73 | 0 | 0 | `shared-domain-final.trx` |
| Full suite including the new fixture and production fixes | 1,025 | 0 | 1 | `full-final.trx` |
| Fixed references after final LF checkout normalization | 41 | 0 | 0 | `references-final.trx` |

The final full runner reports **3 minutes 37 seconds**, with **1,026 ordinary cases** total.
The dedicated fixture reports 8 seconds. These durations describe functional test execution,
not a performance workload. The .NET SDK version was **10.0.401**.

Local TRX files and build/run logs are under `WWCP_POI_Tests/TestResults/model-v2/`, which is
ignored by Git. These commands run from the subrepository root:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~SharedDomainModelTests' --logger 'trx;LogFileName=shared-domain-final.trx' --results-directory WWCP_POI_Tests/TestResults/model-v2 -v quiet
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=full-final.trx' --results-directory WWCP_POI_Tests/TestResults/model-v2 -v quiet
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~InteroperabilityTests|FullyQualifiedName~SnapshotRetentionReferenceTests' --logger 'trx;LogFileName=references-final.trx' --results-directory WWCP_POI_Tests/TestResults/model-v2 -v quiet
```

The skipped ordinary test is `ArchiveCrashWorker`, invoked separately by crash-recovery
fixtures. Both deliberate reference generators remain explicit and excluded from ordinary runs.
TRX lists those two generators as additional `NotExecuted` entries (1,028 listed cases); the
runner summary counts 1,026 ordinary cases, including its one skipped worker.
Ordinary tests compare the fixed manifests and all 29 companion files; they do not regenerate them.
The existing nullable/identifier compiler warnings remain; this package does not clear that backlog.
A final documentation check found no missing targets among 569 local links, and `git diff --check`
reported no whitespace errors.

## Dedicated coverage

[SharedDomainModelTests](../WWCP_POI_Tests/Interoperability/SharedDomainModelTests.cs) contains
**73 passing cases**:

| Cases | Assertions |
| ---: | --- |
| 1 | One shared registry instance per ID across two pools and six meter slots: direct pool/station/EVSE meters and connection-point meters; complete reverse consumer index |
| 12 | Complete JSON/CBOR roundtrips under `en-US`, `de-DE` and `ar-EG`, with/without runtime; exact static bytes/ETags/revision, tick-precision validity, parking union and canonical registry aliases |
| 6 | Standalone software/document/meter transports, declared ETag tampering rejection and required network context |
| 14 | Duplicate catalogs/assignments/products/members, missing references, invalid document coverage and unresolved parking references; unchanged rejected input |
| 5 | Opaque ID whitespace rejection, ordinal case sensitivity and defensive copying of certificate inputs |
| 1 | Registry edits rebind meter/point constructors to the new descriptions, preserve reference ETags and copy grid runtime into independent schedules |
| 6 | A valid first operation followed by invalid deletion/coverage/runtime edits rejects at operation index 1; identical source snapshot/reference index/runtime |
| 1 | Ordered software/document Add, edit and Remove with no invented managed timestamps |
| 3 | Stateless software/document/product nodes reject operational runtime targets |
| 1 | Overlapping parking groups offer a sorted deduplicated union; targeted membership/product changes and optional garage removal preserve the original version |
| 3 | Garage/product/space references cannot cross parking-operator scope |
| 1 | A parking-to-station reference blocks target deletion until explicitly removed |
| 3 | Negative or inverted parking duration ranges are rejected |
| 4 | Absent/minimum-only/cutoff-only/both parking durations retain exact presence and SI values through JSON/CBOR |
| 1 | Disjoint software/document/parking edits prepare only on explicit merge request, retain unchanged history until publication and recover original signed identities |
| 6 | Referenced grid/software/document recreation in both branch orders; explicit temporary reference records/operation order, signed replay, shared aliases and runtime lifetime rules |
| 2 | JSON/CBOR exchange and preview/explicit adoption over the second parent; shared registries, preserved local status, independent old runtime and fresh runtime on archive recovery |
| 3 | Deletion versus newly added operator/software/product references produces typed conflicts and accepts an explicit signed resolution |

## Corrections found during execution

The first selected existing run passed 133 cases and failed eight structural merge cases. The
extended shared reference input added a parking-space link to the station those cases deliberately
delete. Reference protection correctly rejected that setup. The general interoperability input
now keeps parking garage/group/product relationships without that station link. The dedicated
parking-link case independently verifies both the deletion guard and ordered detachment. Both
fixed vector manifests and their companion files were regenerated for that input.

A new product deletion/reference merge exposed serialization of absent optional durations.
The adapter now adds each optional JSON property only when supplied, preserving absence. Four
direct transport cases cover every optional-duration combination, alongside signed history replay.

The added recreation cases exposed that temporary merge detachment only covered top-level
reference strings and arrays. It now traverses the schema-owned meter assignment fields at each
position. Software recreation temporarily removes the affected assignment; document recreation
temporarily removes its optional certificate ID. Recreating a grid operator clears the optional
connection-point slot because its operator reference is required. Every intermediate operation
passes the ordinary validators, and transition values/positions remain in the signed merge audit.
Restoration replaces pending edits anywhere within the affected owner field, including deeper
element paths, and waits for the removal barrier.

Meter assignment detachment preserves meter identities/runtime. Explicitly clearing and restoring
a point starts a new lifetime for its meter; grid-operator recreation likewise resets that registry
runtime. The tests check these resets and the surviving independent pool/station histories.
Preview/preparation never publishes a head or mutates its runtime.

## Limits and next work

These are local regression tests against one implementation. They do not establish independent
peer interoperability, exhaustive reference/recreation combinations or production capacity.
Document structure and release coverage do not authenticate the real approval document or infer
an installed station model/version. Runtime authentication and business parking prices remain
application decisions, as described in the domain model.

The later streaming package now implements deterministic archive output with the existing atomic
persistence flow and has separate [static-v2 measurements](STREAMING-ARCHIVES.md). This model
execution did not cover those later changes. Their subsequent
[verification](VERIFICATION-STREAMING.md) now passes the targeted stream/failure/crash cases and
complete suite. The subsequent [encoder baseline](ENCODING-COSTS.md) now separates per-value
CBOR stages and direct stream costs on that same unchanged production source/assembly. The later
[tagged-document optimization](TAGGED-DOCUMENT-OPTIMIZATION.md) changes the static export and
passes the 149 targeted and 1,174 full regression cases again.

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
