# Tests and interoperability evidence

[Repository overview](../README.md) · [Architecture](../docs/ARCHITECTURE.md) · [Profile and references](../docs/INTEROPERABILITY.md)

Run the actual POI assembly with its local WWCP_CoreData, Hermod and Styx dependencies:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore
```

The latest [normalized map preparation package](../docs/SNAPSHOT-MAP-PREPARATION.md) adds **53 cases**
and passes **2,712 interoperability / 3,044 full Release cases** on 2026-10-10 UTC, with zero failures
and one ordinary worker skipped. All 22 owned kinds cover exact ordinary capture, immutable property/
child/catalog sharing, revision boundaries, ownership edits, exact invalid-input errors and retry.
All eight rich archives retain exact states, canonical content and normalized timestamp spelling;
typed binding IDs match preceding full projections. Existing runtime/trust/atomic/cancellation
contracts remain covered. The 32 references are unchanged; explicit generators are excluded.
See the [execution bindings](../docs/verification/snapshot-map-preparation-results.json).

The preceding [validation projection package](../docs/VALIDATION-PROJECTION.md) adds **71 cases**
and passes **2,659 interoperability / 2,991 full Release cases** on 2026-10-09 UTC, with zero
failures and one ordinary worker skipped. Frozen preceding projections cover all 22 entity kinds,
ancestor/station view reuse, exact failure/merge order, changed maps, atomic subtree rejection,
independent concurrent runtime, all group roundtrips and corrected tariff-group ID length/order.
Eight preceding rich archive/static/branch fingerprints and both original peers remain exact.
All 32 references are unchanged; explicit generators are excluded. See the
[execution bindings](../docs/verification/validation-projection-results.json).

The preceding [domain recovery package](../docs/DOMAIN-RECOVERY-WORKLOADS.md) adds **81 cases**
and passes **2,588 interoperability / 2,920 full Release cases** on 2026-10-09 UTC, with zero
failures and one ordinary worker skipped. Linked production-API fixtures cover shared catalogs,
meters, nested tariffs, parking relations, signed two-parent merges and all four profiles.
Exact JSON/CBOR/memory/spool roundtrips, current bootstrap trust rejection/retry, input detachment,
atomic invalid references/additional peers and independent runtime are checked. All 32 fixed
references remain unchanged; both explicit generators are excluded. See the
[execution bindings](../docs/verification/domain-recovery-results.json).

The preceding [parser-cancellation package](../docs/PARSER-CANCELLATION.md) adds **270 cases**
and passes **2,507 interoperability / 2,839 full Release cases** on 2026-10-09 UTC, with zero
failures and one ordinary worker skipped. Deterministic phase/offset cancellation checks all
four profiles through ordinary/bootstrap private-memory/mapped-spool recovery, eager/lazy trust,
source/input ownership, scope/lease release and exact retry. Frozen preceding syntax/limit readers
compare errors and budgets with default/active tokens; successful reads release the token before
later publication. Nested/concurrent readers retain no ambient parser token or observer. Both
explicit generators remain excluded; the 32 fixed references are unchanged. See the
[execution bindings](../docs/verification/parser-cancellation-results.json).

The preceding [immutable signature-copy package](../docs/SIGNATURE-COPIES.md) adds **103 cases**
and passes **2,237 interoperability / 2,569 full Release cases** on 2026-10-09, with zero failures
and one ordinary worker skipped. Full-constructor and frozen canonical oracles check all commit
kinds, peer order/duplicates/defaults, exact bytes/preimages, record equality, changed unsigned
fields, independent parse fallback, depth/errors, declared identities, alias bounds and threads.
All four profiles and duplicate store/publication/pack delivery cover fresh policy, atomic failure,
retry, original peers, retained static state and independent recovered runtime. Fixed references
remain unchanged; both explicit generators remain excluded. See the
[execution bindings](../docs/verification/signature-copies-results.json).

The preceding [root/head reconstruction package](../docs/SNAPSHOT-RECONSTRUCTION.md) adds **86 cases**
and passes **2,134 interoperability / 2,466 full Release cases** on 2026-10-09, with zero failures
and one ordinary worker skipped.
The ordinary public snapshot-to-model route verifies exact property/canonical bytes, revisions,
references, normalization fallback and error order. Tests cover every represented entity identity,
independent runtimes, concurrent readers, all four profiles through JSON/CBOR/bootstrap, original
peers/branches, cancellation and atomic rejection/retry. Fixed artifacts remain unchanged; both
explicit generators remain excluded. Execution bindings are in
[snapshot-reconstruction-results.json](../docs/verification/snapshot-reconstruction-results.json).

The preceding [canonical preparation package](../docs/CANONICAL-PREPARATION.md) adds **106 cases**
and passes **2,048 interoperability / 2,380 full Release cases** on 2026-10-09, with zero failures
and one ordinary worker skipped. Frozen preceding canonical factories/unsigned writers compare
exact identity/signing bytes, depth/validation errors, detached arrays and original peers.
Bounds, release, nesting, thread separation, fresh trust, four profiles, bootstrap, cancellation
and atomic rejection/retry are covered. Both explicit generators are excluded; fixed artifacts
remain unchanged. Execution bindings are in
[canonical-preparation-results.json](../docs/verification/canonical-preparation-results.json).

The preceding [model preparation package](../docs/MODEL-PREPARATION.md) adds **88 cases** and passes
**1,942 interoperability / 2,274 full Release cases** on 2026-10-09, with zero failures and one
ordinary worker skipped. The interoperability filter covers the entire interoperability namespace;
earlier focused counts below use their package-specific filters. Frozen preceding copying algorithms
compare exact model/import results and errors, ownership/detachment, signed additions and all four
profiles with current trust and fresh runtime. Both explicit generators are excluded; fixed artifacts
remain unchanged. Execution bindings are in
[model-preparation-results.json](../docs/verification/model-preparation-results.json).

The preceding [mapped recovery package](../docs/MAPPED-ARCHIVE-RECOVERY.md) adds **157 cases**
and passes **947 focused / 2,186 full Release cases** on 2026-10-09, with zero failures and one
ordinary worker skipped. Twelve new child-process exits cover Open, direct cold recovery and
bootstrap mapped verification across all four profiles. Fixed references remain unchanged;
two explicit reference generators are excluded. Execution bindings are recorded in
[mapped-recovery-results.json](../docs/verification/mapped-recovery-results.json).

The preceding [input-stream package](../docs/ARCHIVE-INPUT-STREAMS.md) adds **160 cases**
and passes 511 focused input/indexed/recovery/limits/maintenance/persistence/bootstrap/boundary tests.
The final Release run on **2026-10-09 passed 2,029 tests**, zero failures and one ordinary worker
skipped; two explicit reference generators remain excluded.
The preceding [indexed reader](../docs/INDEXED-ARCHIVES.md) added 78 cases and passed 257 focused /
1,869 full cases. Fixed references remain unchanged. The preceding [recovery baseline](../docs/ARCHIVE-RECOVERY-COSTS.md)
added 58 cases and passed 179 focused / 1,791 full cases without changing production.
The preceding direct-archive-ChangeSet run passed 1,733 cases.
[That verification](../docs/DIRECT-ARCHIVE-CHANGESET.md) adds
**214 batch archive cases** and passes all 71 preflight / 140 POI archive / 55 ChangeSet / 59 POI /
20 cache / 149 stream cases (708 focused). The preceding preflight package passed 1,519 cases.
The preceding [direct archive payload package](../docs/DIRECT-ARCHIVE-PAYLOAD.md) passed 1,448 cases
and added 140 tests.
The preceding [direct ChangeSet package](../docs/DIRECT-CHANGESET-CBOR.md) passed 1,308 cases
and added 55 encoder tests.
The preceding [direct POI package](../docs/DIRECT-POI-CBOR.md) passed 1,253 cases and added 59 tests.
The preceding
[bounded child-cache package](../docs/CHILD-ETAG-CACHE.md) passed 1,194 cases and added 20 tests.
The preceding [tagged-document optimization](../docs/TAGGED-DOCUMENT-OPTIMIZATION.md) passed
1,174 cases without adding tests. [Streaming verification](../docs/VERIFICATION-STREAMING.md)
records the 149 earlier new cases and crash/reference/model reruns; the
[1,025-case domain-model execution](../docs/VERIFICATION-DOMAIN-MODEL.md) remains historical.
Matching performance reports are separate evidence.
The shared-model fixture adds
**73 passing cases**. One worker runs in child processes during the crash-recovery tests. Both reference generators are
explicit and excluded from ordinary runs; they never refresh expected files during an ordinary run.
The new replication/adoption fixtures contain **61 passing cases** (34 exchange, 17 adoption,
10 persistence). Run just that package with:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~ReplicationTests|FullyQualifiedName~HeadAdoptionTests|FullyQualifiedName~ReplicationPersistenceTests'
```

## Fixtures

**Current model:** existing JSON/runtime/merge fixtures and the new shared-model fixture were
executed against static-v2. Both vector manifests and their 29 companion artifacts are checked
by the ordinary reference tests. Commands, results and fixes are recorded in
[executed verification](../docs/VERIFICATION-DOMAIN-MODEL.md).

| Fixtures | Coverage |
| --- | --- |
| `SignatureCopyTests` | 103 cases: full-constructor/canonical oracles, all commit kinds, exact peer/preimage bytes, changed unsigned fields, parse/depth/ID errors, detached arrays, alias limits/threads and fresh atomic duplicate/pack unions |
| `ModelPreparationTests` | 88 cases: previous algorithm oracles, all fixture model kinds, exact imports/errors, private ownership, projection fallback/order, caller/output detachment, atomic signed additions/retry, concurrent input, declared ETags and four-profile fresh trust/runtime |
| `SnapshotMapPreparationTests` | 53 cases: all 22 kinds, exact normalized capture and map/property/catalog sharing, revision/ownership/error/retry controls, eight rich archive states and direct binding identities |
| `ValidationProjectionTests` | 71 cases: frozen projections for 22 entity kinds, map/cache/station bounds, exact failure/merge order, atomic subtree additions, independent runtime, all groups/ID formats and eight exact prior archives |
| `DomainRecoveryWorkloadTests` | 81 cases: shared grid/software/document instances, nested tariffs/parking, all-profile roundtrips/bootstrap retries, invalid reference/peer atomicity, detachment, explicit merge and cultures |
| `ParserCancellationTests` | 270 cases: deterministic parser/model/replay and suffix cancellation, frozen syntax/budget/canonical diagnostics, cleanup/retry, late token release, ordinary failures and independent readers |
| `MappedArchiveRecoveryTests` | 157 cases: persistent/cold/bootstrap mapped input, known-length limits, scoped handles, capture modes, cancellation, failure/atomic retry, write exclusion and twelve real process exits |
| `ArchiveInputStreamTests` | 160 cases: all four profiles and sync/async short reads, exact memory/file boundaries, mapped lifetime and private input, first-excess-byte observations, syntax/error parity, cancellations/late token lifetime, I/O/seam cleanup, required trust, concurrent reader leases and explicit orphan reviews |
| `IndexedArchiveTests` | 78 cases: exact four-profile ranges, six duplicate-key kinds before trust, late duplicates, callback mutation of ordinary/bootstrap input, deterministic bootstrap ordering and actual Styx parser/canonical-writer scalar/depth oracles |
| `ArchiveRecoveryContractTests` | 58 cases: all four profiles, truncated/trailing input before trust, noncanonical/indefinite envelopes, byte-limit precedence, invalid late models/heads, every equal peer, required boundary policies and exact valid retries |
| `DirectArchiveChangeSetTests` | 214 cases: independent warmed-scalar depth and native HEX/Base64 headers, exact four-profile long signed archive bytes/recovery, every operation/scoped slot, peer preimages, exact failed prefixes, persistent depth atomicity/cleanup/retry, duplicate/customer keys and bounded exact scalar reuse |
| `POICBORPreflightReuseTests` | 71 cases: independent cached depth and native HEX/Base64 tuples, exact/excess map/text/tree/node limits, escaped/ancestor duplicate keys, sibling/error cleanup, invalid and reader-depth retry, schema/customer separation and overflow bytes |
| `DirectArchivePOICBORTests` | 140 cases: all four profiles, exact buffered references/peers/runtime, standalone versus SkipValue depth including empty/tagged values, semantic preflight rejection, identical failed prefixes, persistent state-depth atomicity/cleanup/retry and destination failure |
| `DirectChangeSetCBORTests` | 55 cases: independent previous-tree bytes, exact numbers/SI text, all nine operations/scoped cable slots, null/absence, native header/customer ETags, wide maps/arrays, depth/tag limits, duplicate/scalar rejection, zero/one/two/four peers, ordered result application and real persistent depth failure/retry |
| `DirectPOICBORTests` | 59 cases: independent previous-tree bytes, runtime/version/HEX/Base64, all metrological schema kinds, uncertainty/prefixes, exact number grammar, scalar spelling/customer boundaries, array counts, key-cache saturation, depth/tag limits, malformed declarations, duplicate keys and failure ordering |
| `ChildETagCacheTests` | 20 cases: cold/warm runtime/version/HEX/Base64 bytes, current runtime, pure revision sharing, changed descendants, exact/excess admission budgets, kind/path keys, invalid-pair retry, parallel exports and released-context collection |
| `ArchiveStreamingTests` | 70 cases: all four JSON/CBOR profiles, independent/fixed bytes, borrowed non-seekable streams, partial failures, reentry and gated publication/runtime concurrency |
| `ArchiveStreamingFailureTests` | 25 cases: real mid-encoding atomic failures/retry, retention review/cold/active bytes and handles, definite counts, map sorting/depth and incremental hashing |
| `BootstrapStreamingTests` | 54 cases: frozen full/tail fragments, exact limits and digests, output/capture failures, sender/receipt changes/disposal and independent frozen callbacks |
| `Json/*Tests` | Domain/presentation JSON, nested parsers/resolvers, parent identities, malformed input, optional/zero/false values, cultures, tariff precision and transparency software |
| `ImmutableEntitiesTests` | Original eight sealed infrastructure APIs, detached dependencies, immutable text/opening hours, rejected metadata mutation, runtime independence |
| `CopyOnWriteChangeSetTests` | Sharing, ordered operations, atomic rollback, revision/old-value checks, connector scopes, signed verification hooks, static streaming and reload |
| `RoamingNetworkChangeSetTests` | Operation shapes, detached payloads and required headers |
| `InteroperabilityTests` | Fixed bytes under multiple cultures, static/profile/ETag tuples, signed two-peer CBOR/JSON, exact signed numeric/SI spelling, optional presence, both before/after hashes, ancestry tampering, malformed input and runtime exclusion |
| `HistoryWorkflowTests` | Independent replicas, published branches, explicit merges/resolutions/audit metadata, archive bootstrap/replay/continuation, duplicate/peer delivery, trust, missing parents, conflicting batch IDs and concurrent head publication |
| `PersistenceTests` | Writer leases, persistence failures before replacement, continuation after recovery and abrupt child-process exit immediately before/after replacement |
| `ReplicationTests` | JSON/CBOR pagination, complete DAG frontiers, exact count/UTF-8/CBOR byte bounds, indivisible oversized commits, checkpoint mismatch, missing/order checks, full-page state/peer/batch-ID rollback, duplicate delivery and changed trust |
| `HeadAdoptionTests` | Signed second-parent merge adoption, preview/idempotence/ancestry checks, independent runtime schedules/measurements/forecasts, recreated/new meter and EVSE lifetimes, connection-point children, revision decreases, revoked merge parents, reentrancy, competing heads and gated runtime delivery |
| `ReplicationPersistenceTests` | One archive write per page, retention/head installation after replacement, injected failures before writing/after flushing, unchanged memory/disk/runtime, retry, original signed head recovery and continued publication |
| `BootstrapTests` | 32 cases: frozen bounded JSON/CBOR transfer, verified receipts/restart/duplicates, exact raw/wire/count limits, disk/transport corruption, write failures and leases, complete digest/profile/checkpoint/head/identity/signature/replay checks, revoked unpublished branches, explicit activation, existing archives, reentry, fresh runtime and incremental continuation |
| `SharedDomainModelTests` | 73 cases: shared operator/software/certificate instances, all meter positions, JSON/CBOR/cultures/ETags, identifier and coverage validation, constructor rebinding, atomic errors, parking unions/scope/deletion guards, optional SI durations, signed disjoint/conflict/recreation merges, temporary reference plans and second-parent adoption with independent runtime |
| `StructuralMergeTests` | 34 cases: owner/descendant deletion in both branch orders, whole-subtree choices with signed recovery, meter/graph recreation and additions, all missing references and typed targets, active/admission groups, shared grid registry references/deletion protection, owner/parking scope, connector IDs scoped to EVSE, criss-cross ancestor choices, invalid resolutions, resolver reentry/exceptions and referenced replacement with an explicit temporary reference plan |
| `SnapshotHistoryTests` | 19 cases: immutable sharing/bookkeeping, signed JSON/CBOR, metadata spelling and UTC, runtime isolation, full recovery, expected-head races, tampering, write failure/retry and overflow |
| `SnapshotBoundaryTests` | 14 cases: fresh chain/signature/rollback authority, original root identity, revoked boundaries, large-root page bounds/local peer retention, bootstrap reopen/activation, old merge parents and suffix merges |
| `RetentionTests` | 38 cases: review/approval, protected branches/bases/parents, explicit releases, exact live head/runtime, stale inventories/peers, cold files/leases, failure/retry, repeated receipts/recovery/bootstraps, structured missing history, exclusions/reimport and concurrent delivery/execution |
| `SnapshotRetentionReferenceTests` | 5 ordinary cases: fixed references across three cultures, artifact recovery, direct SHA-256/Ed25519 verification; deliberate reference generator is explicit |
| `ArchiveLimitsTests` | 60 cases: all four profiles/both formats, exact/excess bytes and counts, repeated catalog IDs, bounded large-file reads, malformed input, released leases, dishonest manifest counts, unchanged rejected staging/archive/head/runtime and exact-budget activation |
| `SnapshotRetentionCrashTests` | 26 cases: 22 child exits across full/pruned histories and snapshot/cold/active write points; four cold-stage exception/reentry cases; exact original/new bytes, peers, root/catalog/digest recovery, fresh trust/runtime, released leases, retry without duplicate receipts and signed continuation |
| `BootstrapCrashTests` | 89 cases: 36 real manifest/chunk/activation exits across all four profiles; verified-prefix restart, duplicate acknowledgement, preserved orphans, exact destination recovery, changed peers/advanced heads/CBOR rejection, fresh trust including target replay, leases, exceptions and signed continuation |
| `ColdArchiveTests` | 98 cases: immutable normalized/bounded catalogs, moved files, multiple copies, ordered location fallback, no-replay failures, current trust/no fallback, byte/container limits, forged receipt membership/anchors, malformed/other-chain archives, unknown IDs and explicit older-receipt traversal with unchanged active history/runtime |
| `ArchiveMaintenanceTests` | 94 cases: immutable bounded review, explicit exact-name cleanup, stale installed/temporary inventories, protected/unrelated files, directory/link guards, real writer/read leases, partial failures, execution budget rechecks and five real process exits with cleanup and original retry |

The 1,000-EVSE sharing fixture counts replaced immutable entries; it is not a capacity/performance
benchmark. The crash tests cover ordinary archive replacement, signed snapshot publication,
cold-backup/pruning and bootstrap receipt/activation boundaries; they do not simulate power loss, filesystem damage, interruption
inside individual writes/renames or every possible I/O failure.
The original bootstrap fixture injects receipt write failures and models interruption by dispose/reopen.
The separate 89-case crash fixture adds actual process exits and explicit activation recovery. Run both:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~BootstrapTests|FullyQualifiedName~BootstrapCrashTests'
```

## Presentation views and complete transports

Ordinary entity `ToJSON` views can omit static timestamps and content according to their expansion
options. Their view roundtrip checks compare presentation fields using `JsonViews`, excluding only
schema-owned derived ETags/profile declarations; arbitrary customer values remain content. Input
nonmutation comparisons remain exact. Complete snapshot/transport tests separately compare exact
canonical bytes and validate the original state ETags.

`DataSnapshot.WriteTo` is static and includes revision/profile metadata. Its streaming comparison
uses `DataSnapshot.ToJSON`, rather than a domain snapshot overlaid with current runtime statuses.
Text quantity imports require SI units, and the exact decimal parser accepts strings; a `JObject`
that was already parsed into Double cannot recover lost decimal digits.

## Wire conventions

- Quantities use invariant SI strings such as `"250 kW"`, `"20 kWh"`, `"400 V"`, `"50 Hz"`,
  `"5.25 m"`, `"1250.5 µΩ"` and `"300 s"`. Numeric/unitless readings are rejected.
- Coordinate latitude/longitude text uses invariant decimals; altitude requires an SI length.
- Schema timestamps normalize to UTC with tick precision; customer text is preserved.
- Charging modes/current types are flat arrays; transparency-software licenses are objects.
- IDs identify graph/collection elements; connectors require their EVSE scope. Validated inherited
  EVSE station views do not become independently editable snapshot properties.
- Missing JSON values remain distinct from defined null. `RemoveProperty`/`RemoveElementProperty`
  express absence; null remains a supplied value/precondition.
- Tagged POI transports declare `contentProfile: "wwcp-poi-static-v2"`; complete tagged parsers
  reject missing/unsupported profiles. Commit/archive headers require `ContentProfile`.
- ETag JSON uses `[format, algorithm, encoding, encodedDigest]`; HEX/Base64 identify equal bytes.
  CBOR uses `[format, algorithm, bytes]`. Commit digests remain labelled `json` in CBOR.
- Peer signatures are equal array entries. Commit identity excludes both peer arrays and binds
  the content profile, ancestry and complete unsigned batch.

## Fixed reference files

[docs/interoperability](../docs/interoperability/README.md) publishes a fixed input, exact canonical
state bytes, snapshot/ChangeSet/commit/merge/archive JSON and CBOR, identity/signing preimages,
expected hashes, public fixture keys and two-peer signatures. Ordinary assertions compare these
committed expectations and verify signatures after transport/recovery. Fixtures include W, VA,
V and Hz; multilingual text, custom decimals, exact signed exponent/negative-zero spelling,
explicit null and a custom conflict-resolution audit.

Deliberate regeneration:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~GenerateReferenceVectors'
```

Review every changed existing digest/byte/signature as a contract change. The next ordinary
build copies reference artifacts to the test output. JSON artifacts have fixed LF checkout
policy and CBOR artifacts are marked binary in `.gitattributes`.

## Snapshot and retention evidence

The four snapshot/retention fixtures contributed **76 passing ordinary cases** to the historical
952-test baseline and are rerun in the current static-v2 suite.
The [verification report](../docs/VERIFICATION-SNAPSHOTS-RETENTION.md) maps assertions, commands,
fixed local reference artifacts and limits. Existing lifetime/temporary-reference fixtures now pass
with the revised semantics, including explicit replacement choice before recreated-EVSE adoption.
Ordinary tests compare exact references without updating them. Local TRX output is under
`bin/TestResults`; both reference generators require deliberate explicit invocation.

## Archive recovery limits

The [archive-limit fixture](Interoperability/ArchiveLimitsTests.cs) adds **60 passing cases** to the
historical 952-test baseline and is rerun in the current static-v2 suite. It checks all history profiles and both formats, exact and excessive byte/commit
counts, repeated receipt/catalog entries, large file lengths before allocation, malformed input,
released leases, dishonest bootstrap counts, unchanged staging/head/runtime and exact-budget
activation. Two existing bootstrap cases now assert the structured retained-count exception.
See [limits and evidence](../docs/ARCHIVE-LIMITS.md).

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~ArchiveLimitsTests'
```

## Snapshot and retention process-crash evidence

The [snapshot/retention crash fixture](Interoperability/SnapshotRetentionCrashTests.cs) adds **26 passing
cases**, reusing the existing child worker. Its six snapshot and sixteen pruning exits run against
complete and previously pruned histories; four additional exception cases check cold cleanup,
handles, reentry and retry. Exact bytes and two-peer signatures are checked after fresh opening.
Old-root recovery retries the reviewed plan; a persisted matching receipt identifies completed
pruning, and the stale plan cannot duplicate it. Signed continuation is reopened again.
The historical baseline passed 952 tests; [current static-v2 results](../docs/VERIFICATION-DOMAIN-MODEL.md)
include this fixture. See
[crash stages and evidence](../docs/CRASH-RECOVERY.md).

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~SnapshotRetentionCrashTests|FullyQualifiedName~PersistenceTests'
```

## Bootstrap process-crash and activation recovery evidence

The [bootstrap crash fixture](Interoperability/BootstrapCrashTests.cs) adds **89 passing cases**.
Its 36 child exits cover three manifest, three chunk and three activation write points across
full checkpoint histories, complete snapshot histories, snapshot boundaries and pruning catalogs.
Parents pin the original manifest independently, verify staging prefixes/orphans and both original
signature peers, retry with fresh trust/runtime and reopen a signed continuation.
An installed destination is recovered only by explicit `recoverExistingArchive`, exact byte equality
and fresh replay under its writer lease. Changed/advanced archives, peers and noncanonical CBOR fail
without writes. Further cases cover current policy revocation, revocation during target replay,
held/released leases, cleanup after write exceptions, reentry and invalid recovery options.
The historical baseline passed 952 tests; [current static-v2 results](../docs/VERIFICATION-DOMAIN-MODEL.md)
include this fixture. See
[bootstrap stages, API and limits](../docs/BOOTSTRAP-CRASH-RECOVERY.md). Local ignored TRX evidence is
under `bin/TestResults/full-suite.trx`; both reference generators remain explicit.

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~BootstrapCrashTests'
```

## Cold archive discovery evidence

[ColdArchiveTests](Interoperability/ColdArchiveTests.cs) adds **98 passing cases** for explicit immutable
local catalogs, moved/duplicate files, deterministic candidate order, typed location failures, exact
digests, fresh receipt/signature/commit/boundary policies and all local archive budgets.
Forged receipt fields cannot invent chain/roots/head or membership in a verified source; new anchors
must be signed snapshots. Repeated receipt traversal retrieves a moved earlier archive explicitly.
Every result preserves active head/runtime/receipts and candidate bytes; no source writer leases
are created. See [API, detailed coverage and limits](../docs/COLD-ARCHIVES.md). Ignored local evidence
is under `bin/TestResults/cold-archive-tests.trx` and `full-suite.trx`.

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~ColdArchiveTests'
```

## Archive orphan maintenance evidence

[ArchiveMaintenanceTests](Interoperability/ArchiveMaintenanceTests.cs) adds **94 passing cases**,
including five actual archive/cold/manifest/chunk/activation process exits followed by explicit
cleanup and original-operation retry. Review/default execution preserve data; explicit deletion
checks exact names, raw digests, timestamps, attributes and installed files under the normal lease.
Changed inventories, exact/excess budgets, recognized directories/links, real competing writers,
file locks and partial failures are exercised. Remaining inventories require a fresh review after
partial progress. The two link cases passed here and skip only without platform link privileges.
See [contracts, case counts and limits](../docs/ARCHIVE-MAINTENANCE.md). Ignored evidence is under
`bin/TestResults/archive-maintenance.trx` and `full-suite.trx`.

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~ArchiveMaintenanceTests'
```

## Remaining coverage

Exhaustive groups/parking/reference/ownership combinations, broader operation-lifetime/reference
transition cases, recursive virtual merge bases, power-loss simulation,
uninstrumented write/rename interruption, streaming replay, compressed receipt catalogs and broader
performance work remain additional work. The separate [Release benchmark](../WWCP_POI_Benchmarks/README.md)
now records [scaling evidence](../docs/SCALING.md), without changing NUnit suite counts. Fixed local
regression references do not establish interoperability with an independent remote implementation.
The original archive-bootstrap workflow remains covered alongside bounded incremental
exchange/adoption and [resumable bootstrap](../docs/BOOTSTRAP.md) workflows. Failure assertions compare the original head/network references,
full JSON/CBOR history and runtime views; persistence cases also compare disk bytes and temporary
file cleanup. Runtime identity checks cover first-parent replay and second-parent branch proofs,
including equal newly added meter IDs and reused identities. Broader owner/reference combinations,
recursive merge bases, independent implementations and broader performance work remain in the
[roadmap](../docs/ROADMAP.md). See [replication evidence](../docs/REPLICATION.md#implementation-and-evidence).

The [structural merge package](../docs/MERGING.md#structural-merge-evidence) has 34 passing cases in the
latest full run, including the new lifetime rules and revised temporary-reference expectation:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~StructuralMergeTests'
```

Preview, rejected preparation and resolver exceptions compare the unchanged complete history,
original head/network references and runtime. Reference resolution tests assert exact typed
targets, absence preservation, dropping stale failures and bounded resolver retries. Signed
resolution recovery and explicit criss-cross ancestor choices preserve the existing byte profiles.
