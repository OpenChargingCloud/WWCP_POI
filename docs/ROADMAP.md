# Towards distributed POI commits

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [ChangeSets](CHANGESETS.md)

This roadmap records implementation boundaries and the next development steps. Items marked
planned are not guarantees of the current API.

## Implemented corrections

- Groups, manufacturers, grid/parking operators and parking children are owned graph nodes.
  JSON/CBOR imports and lazy domain reconstruction resolve memberships and parking links.
  Static operations maintain a persistent reverse reference index and protect referenced targets
  from deletion. Runtime targets/export/history retention cover all status-bearing graph nodes.
- Pool and station `MaxCurrent`, `MaxPower` and `MaxCapacity` are readonly, constructor-supplied
  Styx `Ampere`, `Watt` and `WattHour` values. JSON uses explicit SI strings; CBOR uses
  metrological values. Static limits participate in ETags and can be updated through ChangeSets.
  Nullable limits accept zero and reject negative values and numeric/unitless JSON.
- Pool/station operational measurements and forecasts use timestamped quantities of the same
  dimensions. They remain mutable runtime values and are copied into independent derived objects.
- Status schedule enumeration returns a detached copy captured under the mutation lock.
  This protects individual history enumeration, not transactions across multiple entities.
- `DataSnapshot` stores only static data. ChangeSets reject operational runtime fields, including
  nested payloads and preconditions. Static merges compare only static properties.
- `RoamingNetworkRuntimeUpdate` provides scoped operational/admin status instructions with
  explicit timestamps and optional current-status/static-content preconditions. Static meter
  replacements preserve independent runtime histories by owner and child identity.
- Tagged JSON/CBOR exports preserve revisions through `IncludeVersionMetadata` independently
  of the `IncludeRuntime` flag.
- ChangeSet CBOR exchange preserves the complete v2 signed batch, including numeric/string
  spelling, optional/null payloads, ordered paths and all peer signatures; header ETags use bytes.
- Static hashes and complete exports read stored properties directly. Import captures revision
  zero or supplied version metadata, retains valid optional nulls, initializes timestamps once
  and canonicalizes empty owned graph arrays. Snapshot JSON/CBOR parsers validate declared ETags.
- Typed commit IDs bind ancestry, static state and complete unsigned batches; equal commit peers
  use a separate ancestry-signing profile. Retained history provides expected-head publication,
  duplicate detection, static JSON/CBOR archives and file-backed replay recovery. Revisions count
  along the first-parent chain; scoped runtime delivery shares the publication gate.
- History three-way merges compare retained ancestor/left/right static states, combine compatible
  duplicate/disjoint edits and report structured conflicts. Explicit resolutions prepare a fresh
  unsigned merge commit against the left tip with signed audit metadata. Optional property removal
  retains absence; addressed known collections use stable IDs. Multiple best ancestors require a choice.
- Addressed nested operations use immutable ownership paths and existing element identities.
  Per-element/property preconditions, deterministic touched collection ordering and tariff-index
  maintenance enable disjoint nested edits. Built-in signing uses v2 and binds every path.
- Runtime capture detects explicit removal/reintroduction and temporary identity/slot replacement
  within a batch, so reused final IDs do not silently inherit an earlier runtime lifetime.
- History merge additionally derives graph/nested owned-object lifetimes from original first-parent
  operations, including intermediate states with reused IDs/creation metadata. Structural conflicts
  expose typed checkpoint/commit/operation origins; selecting another lifetime generates Remove/Add
  and optional signed audit evidence. Dedicated regression fixtures now pass.
- Referenced graph recreation can prepare explicit temporary reference detachment/restoration
  through ordinary validators. Preview exposes complete operations and immutable transition records;
  operation indices and actual field values enter signed audit metadata. Restoration waits for pending
  removals, including consumer subtree rebuilds. The existing replacement expectation is updated;
  its targeted fixtures and the full suite now pass; broader combinations remain additional work.
- Incremental JSON/CBOR exchange announces retained DAG tips and bounds commit pages by count/bytes.
  Atomic import validates ancestry, trust and replay before retention, without selecting a head.
  Preview/explicit expected-head adoption accepts descendants through any parent and conservatively
  transfers local runtime lifetimes. Dedicated exchange/adoption/persistence fixtures add 61
  passing cases, including exact boundaries, atomic failures, concurrency and disk recovery.
- Bootstrap freezes a complete static archive and original peers into a manifest-bound sequence
  of bounded JSON/CBOR fragments. Verified disk receipts allow restart; complete hash/trust/replay
  validation precedes explicit activation into a separate new history with fresh runtime.
  Its 32 passing cases cover corruption, receipt failures, limits, activation and continuation.
- Structural merge validation collects missing/out-of-scope reference conflicts with typed consumers,
  fields and targets. Accepted whole-subtree choices are revalidated before processing stale issues;
  repeated invalid choices terminate. Its 34 passing cases cover owner/descendant deletion,
  recreation/addition/ownership, connector scope, reference/group/parking constraints, explicit
  ancestor choices in criss-cross histories and resolver reentry/exceptions. The formerly rejected
  referenced-replacement case now passes with explicit temporary detachment/restoration.
- The README distinguishes static versioning from live runtime values and explains CBOR
  revision transport. The historical Styx patch no longer claims a current Git metadata failure.

The interoperable static profile is now **`wwcp-poi-static-v2`**, declared in tagged transports and
bound into commit IDs/signatures. Fixed JSON/CBOR/state/commit/signature/merge/archive references
are published. The static-v2 domain-model run on **2026-10-08 passed 1,025 tests**, with zero failures
and one skipped worker, including **73 new model cases**. This
[model verification](VERIFICATION-DOMAIN-MODEL.md) predates streaming changes. The later
[streaming verification](VERIFICATION-STREAMING.md) adds 149 passing cases and reruns the complete
suite: **1,174 passed**, zero failures and one ordinary worker skipped. The worker executes separately in crash tests. Snapshot/boundary/retention verification
adds 76 cases and fixed local regression references; archive limits add 60 more cases and process-crash
coverage adds 26 snapshot/retention cases and 89 bootstrap cases; cold archive discovery adds 98,
and explicit orphan maintenance adds 94 cases including five further real process exits.
Existing operation-lifetime,
temporary-reference and structural merge fixtures were
rerun successfully.
Broader graph/algorithm/peer coverage remains open.
See [interoperability](INTEROPERABILITY.md).

## Current type coverage

This table describes the public POI profile and versioned graph, not every helper/result type.
All listed domain types have immutable static data; operational statuses, where present, remain
mutable. JSON/CBOR support alone does not make a type an independently editable graph node.

| Type | Location in a network document | Static ETag coverage | ChangeSet addressing |
| --- | --- | --- | --- |
| RoamingNetwork | Root | Complete owned static hierarchy | Independent node |
| ChargingStationOperator, EMobilityProvider | Network children | Owned content | Independent nodes |
| ChargingPool, ChargingStation, EVSE, ChargingConnector | Owned infrastructure hierarchy | Owned content | Independent nodes; connector ID is scoped to its EVSE |
| ChargingTariff | Operator child; referenced by EVSEs/connectors | Owned tariff content | Independent node |
| EnergyMeter | Pool/station array or EVSE/connection-point singleton | Included in owner; also has its own ETags | Addressed element Add/Remove/Replace/property edit, or whole owner property |
| GridConnectionPoint | Optional pool property | Included in pool; also has its own ETags | Addressed singleton/property edits, or whole pool property |
| GridOperator | Network registry; point `gridOperatorId` references | Registry document and owned point references | Independent node; shared runtime within a version |
| ChargingCable | Connector property | Included in connector | Singleton/property edits by connector owner; no invented ID |
| TransparencySoftware, TransparencySoftwareCertificate | Network catalogs; meter assignments reference IDs | Release and model/version evidence owned once | Independent nodes; addressed certificate reference arrays |
| TransparencySoftwareStatus | Nested legal assignment under meter | Software/certificate IDs and legal label | Whole assignment array on selected meter |
| ParkingProduct | Parking operator child; garage/space/group references | Product document and offer references | Independent node; addressed product reference arrays |
| Tariff element, price component, restriction | Tariff value arrays | Complete value content | Whole-property replacement; deliberately no IDs |
| Brands, data licenses and tariff/location references | Supported owner properties | Included in owner | Addressed collection elements; references use existing ID strings |
| EVSEGroup, ChargingStationGroup, ChargingPoolGroup, ChargingTariffGroup | Operator children | Configuration and member IDs; infrastructure separately owned | Independent nodes and addressed reference-array elements |
| ChargingStationManufacturer | Network child | Public manufacturer/key documents | Independent node; cryptoKeys whole-property replacement |
| ParkingOperator and parking children | Network operator with directly owned garage/space/sensor/space-group children | Owned documents and station/sensor references | Independent nodes and addressed reference-array elements |

## 1. Separate static transitions and runtime updates — implemented

Runtime stays within entity objects and is absent from immutable snapshot storage. Static
ChangeSets reject operational fields. The distinct `RoamingNetworkRuntimeUpdate` contract uses
existing owner/child identities, an explicit timestamp and optional current-status/static ETag
preconditions. It does not touch static timestamps or advance the static commit chain.

Static edits preserve surviving meter/operator histories by owner and identity; explicit
`ReplaceHistory` replaces one operational/admin schedule. New identities start with domain
runtime defaults. [Runtime updates](RUNTIME.md) documents target slots, JSON, notification
limitations and the application gate needed to serialize delivery with head publication.

Measurements/forecasts retain their direct runtime APIs. Runtime authentication, replay policy,
and batch transactions remain application work or future extensions. The history package now
routes scoped status delivery and static publication through one gate; the status instruction
API itself does not implement those features.

## 2. Complete addressing and collection operations — implemented for the defined model

`AddElement`, `RemoveElement`, `ReplaceElement` and `UpdateElementProperty` address nested owned
values through typed property/ID paths. Existing IDs define collection membership; singletons can
use their owner/property slot. Duplicate scope, quantity/old-value validation, deterministic
ordering, metadata propagation, runtime lifetimes and tariff reference maintenance are implemented
for the [supported relations](ELEMENT-OPERATIONS.md#supported-ownership-paths). Whole-property
replacement remains an explicit operation.

[Graph integration](GRAPH.md) imports the groups, manufacturers and grid/parking operators,
including parking children. Ownership scopes, independent node addresses, reference resolution,
reverse index maintenance, deletion protection and runtime retention are implemented. Group
admission lists may refer to future IDs; active members must resolve. Connection points reference
deduplicated registry operators. Network-owned software releases and model/version approval or
compatibility documents have typed IDs and are referenced from immutable meter assignments.
Tariff elements, components and restrictions deliberately remain values without IDs.

Parking operators own products; groups reference spaces and may overlap. A space optionally
references a same-operator garage. Garage/space/group offers form a deduplicated union without
inferred price combination or priority. All new references participate in deletion/merge checks.
Certificate coverage is revalidated after edits. See [domain decisions](DOMAIN-MODEL.md).

These changes define static-v2. Fixed JSON/CBOR/state/commit/signature/snapshot/retention references
were regenerated and the dedicated **73-case model fixture** passes. It covers references,
mutation isolation, complete transports, atomic rejection, parking scope, shared operator runtime,
recreation transitions and second-parent adoption. The full suite is recorded in
[executed verification](VERIFICATION-DOMAIN-MODEL.md). History, publication and three-way
integration of already-published divergent heads remain available below.

## 3. Complete versioned snapshot transports — implemented

Canonical static content, version metadata and current-status export now have independent
options. Default `ToCBOR()` omits revision metadata; use `IncludeVersionMetadata: true` to
continue the numeric revision chain after reload. `IncludeRuntime: true` additionally carries
current statuses. `ToJSONWithETags()` has the same flags; `DataSnapshot` accepts only static
export. `ToJSONSnapshot()` exports the version with current statuses.

The [ChangeSet CBOR codec](CHANGESET-CBOR.md) transfers all peer envelopes without changing
the v2 signing input. Native ETags and lossless schema-defined metrological values coexist with
exact JSON number/string spelling and optional payload presence.

The authoritative [stored-property profile](ETAGS-CBOR.md#versioned-property-presence-and-defaults)
retains valid optional absence/null distinctions and defines initial timestamp and empty owned-array
defaults. Versioned JSON/CBOR imports retain revision metadata and static preconditions. The hash
input changed from domain reconstruction to stored properties; re-export and reprepare batches.
The fixed static-v2 profile and references now cover signed two-peer roundtrips, optional presence,
raw signed number/SI spelling and continued exchange after JSON/CBOR reload. No alternate old-input
hash profile exists.

## 4. Add history, publication and integration of divergent heads — partly implemented

The [history package](HISTORY.md) adds a typed canonical JSON SHA-256 commit identity binding
ordered parents, state identifiers, revision bookkeeping and complete unsigned batch content.
Both peer arrays are excluded; an independent commit signing profile authenticates ancestry.
Retained branches expose original batches, descriptions/metadata and cached static snapshots.

Expected-head publication, first-parent duplicate detection, conflicting batch-ID rejection,
JSON/CBOR archives and file-backed replay recovery are implemented. A shared gate routes scoped
runtime status delivery alongside publication. File persistence uses a writer lease, flushed
sibling temporary archive and rename before head installation. Numeric revisions count along
the first-parent chain; branches can share revisions. The applier still requires exact
`BaseRevision` equality and both static ETags. Executed tests now cover identity/signature vectors,
peer/duplicate delivery, concurrent expected-head publication and signed JSON/CBOR archive recovery.

Additional parents record explicit ancestry without automatically integrating their data.
Tests inject failures before archive replacement and terminate a child process immediately before/after
replacement, proving old/new archive recovery for those stages. Power-loss/filesystem-failure simulation,
broader archive scalability remains additional work. [Release measurements](SCALING.md) now
record retained archive costs and snapshot digest reuse for the published synthetic workloads.

[Three-way integration](MERGING.md) now supports already-published branches, compatible identical
writes/removals, disjoint ID-based collection edits, structured base/left/right conflicts and
explicit choices. It prepares an unsigned commit against the left tip, records ancestor/tips/
decisions in batch metadata, validates the combined graph/references and schedules a legal operation
sequence. Candidate failure or missing resolution creates no commit. Multiple best common ancestors
require explicit selection. Executed tests cover disjoint property/meter edits, structured conflicts,
custom SI resolutions, absence/null choices and signed merge/archive exchange.

Recursive virtual merge bases, rebase and multi-tip integration APIs remain planned. Validation
cycles or operation dependencies that cannot be scheduled are reported rather than weakening
domain constraints. Fixed disjoint/resolved merge references and workflow tests are published.
The 34 [structural merge cases](MERGING.md#structural-merge-evidence) now exercise deletion/recreation,
ownership, missing and out-of-scope references, scoped connectors, criss-cross ambiguity, explicit
ancestor choices and resolver reentry/exception rollback. Operation-history lifetime handling for
reused creation metadata is now implemented; dedicated coverage, exhaustive graph/reference
combinations and broader temporary reference-transition coverage remain additional work.

## 5. Freeze interoperable profiles and exchange behavior — partly implemented

The [static-v2 profile](INTEROPERABILITY.md) fixes stored-property projection, canonicalization and
normalization; transport declarations reject unknown profiles and commits explicitly bind it.
Fixed reference files cover JSON/CBOR state bytes, ETags, unsigned identity/signing bytes,
two-peer signatures, custom data, timestamps, optional presence and disjoint/resolved merge archives.

[Incremental replication](REPLICATION.md) now provides bounded missing-ancestry pages, atomic
retention, explicit expected-head adoption and divergence notices. Secondary-parent adoption
selects the original merge and uses conservative local lifetime proofs. Different checkpoints
require explicit [bootstrap](BOOTSTRAP.md), now available as bounded resumable archive fragments
with preview/activation. Source export now streams into frozen bounded fragments; final decoding/
replay still materializes a complete archive and retained states. Individual CBOR value encoders
and the total frozen source payload still allocate. See [streaming archives](STREAMING-ARCHIVES.md).
Transport/key
negotiation, authorized keys/distinct signers/quorum and external archive rollback policy remain
application work. Executed dedicated fixtures cover exact page bounds, atomic rejection,
original signed merge adoption, runtime lifetimes, changed trust, concurrent heads and recovery.

## 6. Establish complete workflows and performance evidence — partly implemented

Executed workflows cover independent replicas with different runtime statuses and equal static IDs,
signed transition exchange, both before/after digest rejections, published concurrent edits, explicit
merge, signed JSON/CBOR archive reload, continued exchange and duplicate delivery. Tests also cover
malformed transports, unchanged signature identity after added peers, ancestry/signature tampering,
required signer counts, writer leases, persistence failures and abrupt process exit around replacement.

Executed replication evidence now covers bounded multi-page JSON/CBOR transfer, exact UTF-8/CBOR
byte and count limits, indivisible oversized commits, atomic rejection of a later commit, checkpoint
mismatch, missing/out-of-order parents, repeated delivery, changed trust/additional peers, adoption
races, gated runtime delivery, second-parent lifetime resets and persistent retry/recovery. Existing
archive-bootstrap workflows remain covered as well. The 32 new [bootstrap cases](BOOTSTRAP.md)
cover interrupted transfer/reopen, exact limits, corruption, complete trust/replay validation,
explicit activation, fresh runtime and continued incremental exchange. See also
[replication fixture details](REPLICATION.md#implementation-and-evidence).

Broader nested/graph/ownership combinations, dedicated operation-history lifetime coverage, recursive merge bases
and broader production performance remain additional work. There is no power-loss simulation or
independently implemented remote peer yet. [Release measurements](SCALING.md) now cover full static
hashing, runtime materialization/capture, archive rewrite/replay, bootstrap and cold lookup. Initial
improvements reuse existing digests/canonical conversion work and index location deduplication;
larger representation caches or a Merkle profile require separate evidence and contracts.

## 7. Admin-controlled snapshot commits and history retention

Full-state snapshot links now exist inside the original chain. Administrators explicitly prepare,
sign and publish them; ordinary ChangeSet commits follow at the new revision. Operation-history
lifetime handling and temporary reference scheduling remain integrated. Snapshot creation keeps
all original ancestry. See [snapshot contracts and usage](SNAPSHOTS.md). Automatic time/change-count
policies remain application work. Explicit trusted snapshot entry, suffix exchange and independently
approved archival/pruning are now implemented as described below.

### Implemented snapshot links and complete-history exchange

- Explicit snapshot commit kind with the current head as its single parent, a complete static
  snapshot, both state ETags, content profile, creation timestamp, descriptions and metadata.
  Existing commit IDs and signatures are preserved; subsequent commits build on the snapshot.
- Separate deterministic identity and signing profiles bind parent, full state and administrator
  metadata. Equal-peer signatures use the existing commit verification and authorization callbacks.
- Snapshot-only commits advance revision once, preserve the last applied batch ID and reproduce
  exact parent static content and both ETags. POI timestamps and entity lifetimes do not change.
- Publication uses the expected-head gate and atomic persistence. Derived local runtime remains
  independent and preserves statuses, schedules, measurements and forecasts.
- JSON/CBOR full-history archives, resumable bootstrap and incremental pages have explicit profiles
  for snapshot envelopes. Complete first-parent replay and every original merge parent remain required.

SnapshotHistoryTests now has 19 passing cases for roundtrips/signatures, bookkeeping, runtime,
publication races, atomic failure/recovery and complete archives. Fixed byte/signature references
also pass across three cultures. See [the verification report](VERIFICATION-SNAPSHOTS-RETENTION.md).

### Implemented authorized snapshot boundaries

- `FromSnapshot` and `CreatePersistentFromSnapshot` start a separate fresh-runtime replica at an
  original signed snapshot, preserving its ID, external parent and original chain checkpoint claim.
- Signature verification and explicit checkpoint/anchor authorization are mandatory. Boundary
  policy must independently authorize chain association and rollback constraints; omitted ancestry
  cannot prove the original checkpoint claim. Static mutations and recovery recheck current trust.
- Partial history-v3 archives and manifest-v3 bootstrap include the full root once. The selected
  tip's suffix must include all parent paths. The source archive remains intact.
- Replication-state-v2 explicitly acknowledges only ancestry down to its anchor. Commit-pack-v3
  references the local root by ID and peer envelopes, keeping incremental pages independent of its
  full payload size. That package retained the then-current complete-history identities/profiles; current references
  were subsequently regenerated for static-v2.
- `SnapshotRequired` and `HistoryRequired` report boundary/dependency failures with typed IDs.
  Proposals require fresh approval; unknown IDs are not described as proven compacted history.
- Known suffix branches can merge and preserve local runtime through retained operation evidence.
  Missing requested bases return `HistoryRequired`. Snapshot entry starts fresh runtime and cannot
  prove pre-boundary object lifetimes. Full-history replay remains independently available.

See [boundary contracts and examples](SNAPSHOT-BOUNDARIES.md). SnapshotBoundaryTests now has 14 passing
cases for fresh policies/rollback, recovery, large-root page limits, original local peers, partial
bootstrap, runtime and cross-boundary merges. Fixed boundary/page regression references are available.

### Implemented explicit retention and pruning

- `GetRetentionSnapshots` binds administrator time policy to a retained first-parent cutoff.
  Immutable plans protect head, all unpublished frontier tips, explicit bases and every merge parent.
  Cross-boundary dependencies block pruning; only named frontier tips can be released to cold storage.
- Review identities bind the full source archive, including peers and earlier receipts. Execution
  rejects changed heads or inventories and requires explicit pruning plus boundary authorization.
- A complete preceding local CBOR archive is flushed, published without overwrite and digest-checked
  before active replacement. In-memory histories require this durable backup as well.
- Exact live head/network/runtime remain unchanged. Root, retained maps, batch inventory and receipts
  install together after persistence. Existing original commit identities/signatures remain intact.
- History-v4/manifest-v4 persist native JSON/CBOR pruning receipts. Known archived IDs return
  structured lookup/replication outcomes and cold archive digests; arbitrary unknown IDs stay distinct.
  `ReadColdArchive` verifies an independently located archive into a separate fresh-runtime history.

See [retention contracts and examples](RETENTION.md). RetentionTests now has 38 passing cases covering
branch/base protections, stale plans/peers, cold files/leases, failure/retry, catalog/repeated recovery,
bootstrap exclusions, reimport, structured missing history and concurrent runtime/execution. The catalog
remains unsigned bookkeeping; original signed cold replay supplies historical evidence.

### Implemented snapshot and retention verification

- Initial verification run: 76 new ordinary cases plus all existing tests; 585 passed, zero failures,
  one skipped process worker. The initial archive-limit run passed 645 tests; process-crash coverage
  brought the suite to 671; bootstrap crash/recovery brought it to 760, cold archive discovery to 858,
  and explicit orphan maintenance to 952.
- Fixed snapshot/complete/boundary/page/plan/receipt/pruned-archive/manifest regression artifacts,
  with exact identity/signature inputs, three cultures, direct SHA-256 and Ed25519 checks.
- Existing EVSE recreation adoption now requires explicit ReplaceModify resolution and still verifies
  lifetime reset plus preserved unrelated runtime. The current reference files were subsequently regenerated for static-v2.
- These local regression references do not establish independent peer interoperability or exhaustive
  graph/algorithm/process-crash/power-loss behavior. See [verification limits](VERIFICATION-SNAPSHOTS-RETENTION.md).

### Implemented: bounded archive and cold-history recovery

- `RoamingNetworkHistoryLimits` caps bytes, retained commits including the root, pruning receipts and
  aggregate entries in both catalog-ID arrays. JSON measures UTF-8 bytes; file length is checked
  before allocation. Streaming JSON/Styx CBOR scans precede document materialization and replay.
- All four archive profiles apply the same local budgets during parsing, reopening, cold retrieval
  and bootstrap final preview/activation. Bootstrap source checks catalog counts before freezing.
- Typed exceptions and final bootstrap `LimitExceeded`/`LimitViolation` preserve archive, staging,
  active head/network and runtime on rejection, and failed reopening releases its writer lease.
- 60 new ordinary cases cover exact/excess limits, aggregate/repeated catalog IDs, malformed and
  impossible lengths, bounded large-file reads, dishonest manifest counts, unchanged failure state
  and exact-budget resumed activation. Initial package run: 645 passed, zero failures, one skipped worker.
  See [archive limits and evidence](ARCHIVE-LIMITS.md).

### Implemented: process-crash recovery at snapshot and retention boundaries

- Reuse the existing child worker for signed snapshots and retention, with independently pinned
  checkpoint/root/candidate/plan identities and stage markers proving actual exits without unwinding.
- Six snapshot exits cover three active write points in complete and previously pruned histories;
  sixteen pruning exits cover four cold/seven total write points, repeated catalogs and matching
  existing cold files. Four additional cold-stage exceptions cover cleanup/handles/reentry/retry.
- Reopen with fresh signature/boundary policies and runtime; verify exact original/new archive bytes,
  head/root/catalog, both original peers, cold source digest and nested older cold retrieval.
- Old state explicitly retries the same candidate/plan; completed snapshots return `AlreadyPublished`.
  Completed pruning is recognized by its receipt `PlanId`; stale replay returns `InventoryChanged`
  without another receipt. Recovered live runtime survives retry, and signed continuation reopens.
- Initial package full suite: 671 passed, zero failures, one skipped child worker; the focused persistence/crash run
  passed 41 cases. See [crash recovery and practical limits](CRASH-RECOVERY.md). This is named-point
  process-interruption evidence, with power loss and interruption within writes/renames still open.

### Implemented: bootstrap process-crash recovery

- Reuse the child-worker framework for manifest/chunk receipts and explicit activation persistence.
- 36 actual exits cover three manifest, three chunk and three activation points across all four archive
  profiles, with independently retained manifest/chain/root policies and original equal-peer signatures.
- Reopen the verified prefix, retry missing chunks, acknowledge installed duplicates and preserve orphans.
  An unpublished orphan manifest requires a new empty staging folder or the later explicit
  maintenance workflow before initializing the same folder again.
- Explicit `recoverExistingArchive` reopens only exact frozen archive bytes under the writer lease,
  with fresh replay/authority and local runtime. `ActivationRecovered` identifies a completed write;
  newer heads, changed peers/CBOR bytes, revoked policy and held leases are rejected without overwrite.
- 53 additional cases cover mismatches, all trust callbacks, revocation during target replay, released
  leases, activation write exceptions, reentry and invalid options. Signed continuation reopens again.
- Initial package full suite: 760 passed, zero failures, one skipped child worker. See
  [bootstrap crash stages, recovery API and limits](BOOTSTRAP-CRASH-RECOVERY.md).

### Implemented: cold archive discovery independent of local storage paths

- Immutable typed locations and `RoamingNetworkColdArchiveCatalog` freeze local digest/path claims
  in explicit registration order, with normalized paths, deduplication and a bounded input count.
  Receipts already contain no paths and retain their exact original identities when files move.
- `TryReadColdArchive` diagnoses missing/read/digest/byte failures before trying another location.
  The first digest match must pass fresh replay/authority and recorded chain/roots/head/membership;
  a trust failure ends the attempt. Matching copies use registration order.
- Optional requested commits produce structured retained/archived/unknown results. Earlier receipts
  can be resolved explicitly through the same catalog without hydrating live ancestry.
- Separate recovered histories start fresh runtime, hold no persistent source writer lease, and leave
  active heads/runtime/catalog and candidate files unchanged. Local archive budgets remain mandatory.
- 98 targeted cases cover moved files, repeated chains, multiple/newer copies, location failures,
  current trust/revocation/exceptions, exact/excess budgets, forged receipt membership and unknown IDs.
  Initial package full suite: 858 passed, zero failures, one skipped worker. See [cold archive discovery](COLD-ARCHIVES.md).

### Implemented: archive orphan inventory and explicit cleanup

- Immutable local plans bind selected scope/path, budgets and recognized filenames, raw SHA-256,
  lengths, timestamps and attributes. Installed archive/manifest/chunk states are protected.
- Inspection/default execution hold the normal writer lease and hash regular direct files without
  decoding archives. Explicit `cleanup: true` rechecks the complete recognized inventory first,
  removes only reviewed GUID temporary siblings and reports exact partial progress on failure.
- New installed data, changed inventories, held writers/read handles, directories/links and exceeded
  budgets reject work. Unrelated files and child directories are preserved; no recursive cleanup.
- Five real process exits cover active/cold/manifest/chunk/activation writes. After cleanup, the
  original commit, retention, transfer or activation can retry; initial manifest staging can reuse
  the same cleaned folder. Cooperation through writer leases and nontransactional deletion are documented.
- 94 focused cases pass, including actual link guards on this filesystem. Its original full-suite
  baseline passed 952 tests, zero failures, one skipped worker. The current static-v2 suite reruns
  them; see [model verification](VERIFICATION-DOMAIN-MODEL.md) and [archive maintenance](ARCHIVE-MAINTENANCE.md).

### Implemented: reproducible scaling measurements and digest/catalog improvements

- Separate dependency-free Release benchmark processes use fixed IDs/timestamps/SI values and
  real Ed25519 verification. Published workloads vary graph size, retained branches/commits/snapshots,
  repeated pruning receipts and same-digest local location count.
- Full hashes, immutable changes, runtime capture/materialization, JSON/CBOR archives, durable
  rewrite, verified replay, local bootstrap and cold replay record elapsed/CPU/thread allocation,
  GC counts, approximate managed/working-set peaks and output/written bytes.
- Root exports reuse existing snapshot digests; snapshot-only links share them. Fresh pairs reuse
  canonical JSON bytes within CBOR conversion. Local catalog construction indexes path membership
  while retaining registration order, platform comparison and enumeration budgets.
- Raw before/after reports and commands bind the measured environment and production state.
  Comparison rejects changed static/archive/operation identities, inventories and byte counts.
  Timing is local synthetic evidence, with JIT/GC/system variation and no capacity guarantees.
  The Cold-Archive follow-up preserves a slower normal-tiering result and documents a separate
  no-tiering comparison; additional warmup/JIT/production measurements remain open.
  See [scaling report and remaining costs](SCALING.md).

### Implemented: shared catalog and parking verification

- **73 passing cases** cover shared catalog instances at all six meter slots in a two-pool graph,
  JSON/CBOR under three cultures, standalone transports/ETag rejection and typed identifiers.
- Ordered edits and failed second operations check static/reference/runtime isolation. Parking
  coverage includes garage scope, overlapping groups, offer unions, membership and station deletion guards.
- Signed history tests cover disjoint merges, typed deletion/reference conflicts, reference recreation
  in both branch orders and second-parent adoption with independent local runtime.
- Fixed references and the full ordinary suite were rerun against static-v2.
  See [commands, results, corrections and limits](VERIFICATION-DOMAIN-MODEL.md).

### Implemented: streaming deterministic archive encoding and atomic persistence

- `WriteJSON(Stream)` and `WriteCBOR(Stream)` emit the existing archive representations to borrowed,
  writable, non-seekable destinations without closing/flushing them. Reentrant gated mutations reject.
- JSON uses the existing field order over a pooled `IBufferWriter`; CBOR emits sorted definite maps,
  commits and receipts incrementally while keeping Styx leaf and POI/ChangeSet codecs unchanged.
- Integrated persistence streams into the same unique temporary file before durable flush/rename.
  Retention hashes/counts the reviewed source incrementally and streams cold/active replacements.
  Writer leases, named observer stages and installation after persistence remain in place.
- Bootstrap freezes private bounded fragments and incremental whole/chunk hashes during output;
  it still retains the full payload and final decoding/replay still materializes the archive.
- Matching static-v2 before/after Release workloads preserve exact identities, digests, inventories
  and byte counts. Allocation/time/CPU/peak measurements and a reproducible scoped patch are recorded
  in [streaming evidence](STREAMING-ARCHIVES.md). Timings vary; there is no general speedup claim.
- That initial streaming build still allocated complete individual CBOR payloads, now removed by
  [direct POI](DIRECT-ARCHIVE-PAYLOAD.md) and [ChangeSet](DIRECT-ARCHIVE-CHANGESET.md) archive emission.
  No append-only journal, new content profile or streaming decoder is introduced.

### Implemented: stream contracts and atomic encoding verification

- **149 new passing cases** cover all four archive profiles and both encodings against independent
  outer envelopes/unchanged value codecs and fixed references, including peers, merges and receipts.
- Non-seekable destinations, no close/flush, varied boundaries, exact partial failures, guard reset,
  reentrant gated actions and blocked concurrent publication/status delivery are exercised.
- Real codec-depth failures during publish/store preserve active files, head/inventory/runtime and
  clean exact temporaries; valid retry/reopening succeeds. Incremental retention review/cold/active
  bytes match whole-value references, with the verified cold handle held through replacement.
- Frozen bootstrap covers full/tail fragments, byte/count/commit/wire/manifest budgets, failed
  capture/output, later sender/receipt changes, sender disposal and allowed frozen callbacks.
- Existing persistence/snapshot/retention/bootstrap crash regression passes **130 cases**, with
  60 actual child exits. The complete suite passes **1,174 cases**, zero failures, one skipped worker,
  also rerunning the five maintenance exits and all culture/cryptographic/model/merge references.
- No production correction or reference/performance regeneration was needed. See
  [commands, results, test setup corrections and limits](VERIFICATION-STREAMING.md).

### Implemented: individual CBOR value and direct stream measurements

- 17 benchmark operations isolate tagged snapshots, signed one-operation ChangeSets, schema-path
  validation, canonical JSON, value trees, native ETag conversion and deterministic map/output work.
- Direct non-seekable JSON/CBOR hash/count sinks are compared with buffered APIs. Exact output
  byte counts/digests and graph/history inventories agree, including the preceding static-v2 report.
- 136 fresh workers and 680 measured samples use five warmups/five samples per process. Stage
  products are consumed and checked byte for byte after measurement; complete calls include SHA-256.
- The tagged document stage was the largest isolated snapshot allocation cost. That measurement
  package left production sources, assemblies, profiles/vectors and tests unchanged; the existing
  149/1,174-case verification described that binary. See [measurements and interpretation](ENCODING-COSTS.md).

### Implemented: reduce tagged snapshot document work

- Static tagged export hashes the prepared subtree before adding declarations. It avoids each
  redundant deep clone/prepare pass while keeping runtime-inclusive static projections separate.
- Matching 17-operation before/after Release runs use unchanged benchmark code, dependencies,
  counts and exact outputs: 136 fresh workers / 680 samples per report. At 512 EVSEs tagged
  document allocation falls 23.4%, complete snapshot CBOR 16.3%.
- All identities, JSON/CBOR bytes, SI/signature contracts and depth failures remain exact. The
  existing 149 stream cases and complete 1,174-case regression pass again; no new test/reference
  regeneration is needed. A scoped patch and source/assembly/TRX evidence bind the executions.
- Times remain mixed. See [change, measured costs, verification and limits](TAGGED-DOCUMENT-OPTIMIZATION.md).

### Implemented: bounded immutable child-digest reuse

- Complete static snapshot contexts reuse declared children's immutable JSON/CBOR pairs. Only
  snapshot-only revisions share the context; changed maps start fresh. Kind and full schema path
  bind entries within that complete map/root/profile context.
- Admission preserves the first traversal entries under explicit pair-count, logical key/digest
  payload and individual key limits. Whole contexts are collected with their last owner; runtime
  overlays, output documents/buffers and failed computations are not retained.
- At 512 EVSEs repeated tagged-document allocation falls 76.4%, complete snapshot CBOR 48.5%.
  Cold allocation rises about 0.3%/0.2%; the measured context adds about 504 KiB of managed
  retained memory. Counts/payload are bounded per context, not across the complete history.
- 20 new cache cases and all 149 stream cases pass; the complete suite passes 1,194 cases.
- Cold and warm exports, per-context retention and the existing archive outputs have matched
  before/after measurements. Runtime/version/HEX/Base64 variants, changed descendants, exact
  limits, concurrency and released context lifetimes have targeted coverage. See
  [cache design, measured costs, verification and limits](CHILD-ETAG-CACHE.md).

### Implemented: direct deterministic encoding of individual POI CBOR values

- Prepared canonical JSON is indexed once; sorted unique maps and definite containers are emitted
  through Styx without complete intermediate CBOR/native-ETag conversion trees. Exact numeric
  spelling, metrological tag 44252, binary digest tuples and all static-v2 bytes remain unchanged.
- A per-call encoded-key cache has entry/byte bounds. Canonical JSON/index and complete value
  output buffers remain; decoding, ChangeSet encoding and archive value-depth validation are unchanged.
- 59 independent previous-tree cases cover scalar/schema/customer/depth/count/declaration boundaries.
  The full suite passes 1,253 cases; all 20 cache and 149 archive cases pass (228 focused).
- Matched cold/warm, complete snapshot and archive outputs use unchanged benchmark code with
  child caches in both builds. At 512 EVSEs complete snapshot allocation falls a further 40.9%,
  streamed CBOR archives 40.7%; timings/peaks remain mixed. See
  [algorithm, costs, source/assembly bindings and limits](DIRECT-POI-CBOR.md).

### Implemented: direct deterministic ChangeSet CBOR encoding

- Complete batch/header-conversion trees are removed. Definite sorted emission shares the POI
  writer and its bounded per-call key reuse; the unchanged scalar codec preserves exact signed
  numbers, lossless SI text, native before/after digests, operations and all equal peer envelopes.
- Complete input validation and schema reading paths precede emission. JSON/index/output buffers,
  scalar trees and existing application-metadata/decoder trees remain. No signing/profile change.
- 55 new cases cover independent old-tree bytes, operations/scoped slots, optional/null/customer
  values, scalar/depth guards, peers, ordered application and persistent tag-depth failure/retry.
  All 283 focused cases and the full 1,308-case suite pass; fixed references remain unchanged.
- Paired 1/64/512/2,048-operation fixtures with one/four real Ed25519 envelopes use identical
  benchmark code, complete outputs and valid result application; signing/setup are excluded.
  At 2,048 operations/four peers, complete CBOR allocation falls 33.4% (15.75 -> 10.48 MiB),
  with byte-identical outputs and an unchanged canonical JSON allocation control. See
  [design, costs, source/assembly bindings and limits](DIRECT-CHANGESET-CBOR.md).

### Implemented: direct POI payload emission into CBOR archive envelopes

- All four archive profiles emit checkpoint/snapshot POI values into borrowed output after
  preparation and preflight. Complete per-POI CBOR output buffers/copies are removed.
- Standalone writer and remaining SkipValue rules stay distinct, including empty containers and
  tagged leaves. A conservative leaf-depth bound avoids buffering away from the boundary; actual
  Styx writer/reader checks settle boundary cases before any payload byte is emitted.
- 140 new cases verify exact bytes, original peers/fresh recovery runtime, identical failed
  prefixes, real persistent state-depth atomicity/temporary cleanup/retry and destination errors.
  All 423 focused and 1,448 full cases pass. Fixed references remain unchanged.
- The unchanged C# harness measures paired complete/stream archives, snapshot cold/warm controls
  and frozen bootstrap across 80 workers / 400 samples per report. That buffer-removal build added
  preflight allocation: streamed 512-EVSE archives rise 62.96 -> 74.87 MiB
  (+18.9%). See [design, costs, source/assembly bindings and limits](DIRECT-ARCHIVE-PAYLOAD.md).

### Implemented: bounded POI archive preflight reuse

- Each property name is decoded once; cleared uniqueness sets are reused by nesting depth and
  oversized sets discarded. Fully validated native ETag pairs use their two-array shape proof
  away from a boundary; actual Styx fallback still checks boundary cases.
- A 32-entry/4,096-character per-payload SI scalar cache has individual text/tree/node bounds.
  Every occurrence checks schema ownership and depth anew. No runtime/cross-call state is retained.
- 71 new cases cover exact/excess budgets, independent depth/HEX/Base64 bytes, customer separation,
  duplicate/sibling/error cleanup and retry. All 494 focused and 1,519 full Release cases pass.
- Matched 80-worker/400-sample archive/bootstrap and cold/warm controls retain every output and
  inventory. Streamed 512-EVSE allocation changes 74.87 -> 66.03 MiB (-11.8%);
  the earlier buffering build used 62.96 MiB. See [algorithm, evidence and limits](PREFLIGHT-ALLOCATION.md).

### Implemented: direct ChangeSet payload emission into CBOR archive envelopes

- All four profiles preflight and emit each ChangeSet directly, removing its complete CBOR output
  buffer/copy. Exact signed number/SI text, root native digest pairs and every equal peer remain.
- A 64-entry/8,192-character per-payload scalar cache retains only successful bounded codec trees;
  exact tokens/value kind remain distinct and each occurrence rechecks ownership/depth.
- 214 new cases compare independent bytes, actual writer/SkipValue budgets, original peers,
  failed prefixes, persistent depth atomicity/cleanup and signed retries. All 708 focused and
  1,733 full Release cases pass. Fixed references and runtime separation remain intact.
- Two fresh matched suites each use 48 workers/240 samples per side. At 2,048 operations/four
  peers streamed signed-archive allocation changes 32.27 -> 31.90 MiB (-1.1%).
  JSON/index/path preparation, metadata trees, fragment totals and replay remain costs. See
  [design, exact source/binary evidence and measured limits](DIRECT-ARCHIVE-CHANGESET.md).
- Preflight adds overhead for small batches: streamed history-64 allocation changes
  7.04 -> 7.39 MiB (+4.9%).
  Reduce repeated JSON/path/scalar preparation in a follow-up; no universal allocation/time win.

### Implemented: archive decoding, trust and replay baseline

- Seven actual reader/stage operations cover seven shapes and all four profiles: 98 fresh
  workers / 490 measured calls, with exact heads, branch states, peers, runtime and input bindings.
- Separate post-measurement collection records approximate additional recovered-history memory.
  Resident inputs, prepared models and retained states remain distinct from reader-tree buffers.
- 58 new recovery contracts preserve full-input failure-before-trust, existing eager/lazy model
  behavior, equal peer verification, required boundary authority and exact successful retries.
  All 179 focused / 1,791 full Release cases pass; production binary/references stay unchanged.
- [Results and incremental-reader requirements](ARCHIVE-RECOVERY-COSTS.md) define byte/depth/count,
  EOF/duplicate/UTF-8, arbitrary field order, borrowed source and private recovery contracts.

### Implemented: validated archive ranges before model replay

- Complete resident CBOR syntax/UTF-8/duplicate/depth/EOF validation precedes trust; root/commit/
  receipt slices feed unchanged model parsers without a complete archive tree. Actual Styx key
  equality and deterministic bootstrap rules preserve the preceding parser/writer contracts.
- Boundary suffix bytes are privately frozen before callbacks can change the original input.
  Complete v1/v2 models remain eager, boundary v3/v4 models lazy; original peers, all branches,
  exact static identities and fresh local runtime remain intact.
- 78 new cases compare actual Styx parser/canonical-writer oracles, structured duplicates,
  global depth, exact four-profile slices and ordinary/bootstrap input mutation. All 257 focused /
  1,869 full Release cases pass with unchanged fixed references.
- Complete CBOR recovery and JSON/prepared-model controls use an exact 42-worker / 210-sample
  subset of the preceding baseline and 42 fresh workers / 210 samples after. The C# harness,
  dependencies, inputs and recovered results match. See [design, costs and evidence](INDEXED-ARCHIVES.md).

### Implemented: borrowed non-seekable CBOR input

- Sync/async general-history input leaves the source open, starts at its current position and
  supports short reads without Length/Position/Seek/Flush. Early byte rejection consumes only
  maximum+1; complete-span diagnostics retain their known full length.
- Private capture capacity is bounded by a local memory threshold and archive bytes; larger
  inputs spill to owned delete-on-close files. A read-only mapping supplies Styx without a new
  complete managed input copy. Individual values, private suffix bytes and retained states remain.
- Full syntax-before-trust, eager/lazy model timing, all peers and fresh runtime stay exact.
  Cancellation checks reads/commits/callbacks and is cleared from retained trust delegates after
  recovery. Async capture does not preempt synchronous scalar/model/state processing.
- Shared capture leases permit simultaneous readers and block maintenance. Existing temporary
  siblings use explicit immutable orphan review/default preview/cleanup; recovery deletes only
  its own file and retains the coordination lock.
- 160 new cases and all 511 focused recovery/limits/maintenance/persistence/bootstrap/boundary
  cases pass. All 2,029 full Release cases pass with unchanged fixed references.
- [Design, tests, measurements and evidence](ARCHIVE-INPUT-STREAMS.md) compare fresh same-build
  span/private-memory/mapped-file recovery against exact common prepared archives.

### Implemented: mapped persistent/cold input and bounded bootstrap recovery

- Existing files now check their known full length before scoped read-only mapping. Open transfers
  its writer lease only after input release; cold digest and replay use the same file/view.
- Bootstrap captures verified ordered chunks under local memory/file policy, retains full digest,
  canonical CBOR and manifest/trust checks, and closes capture before durable new installation.
  Exact existing-destination recovery retains byte equality and fresh verification under its lease.
- Optional cancellation preserves source/staging/orphans, disposes private histories and clears
  the read token from retained trust. Once durable activation starts, atomic publication completes.
- [Contracts, tests and measurements](MAPPED-ARCHIVE-RECOVERY.md) include handle release, retry,
  structured candidate outcomes and child-process exits during mapped verification.

### Implemented: private immutable model preparation

- Import, source/projection completion and representation binding copy the supplied tree once at
  their entry points and traverse private descendants. Explicit projected-value copies remain.
- All node checks/normalization/error order and original numeric/SI/static/signature content stay
  exact. Current trust, eager/lazy recovery, local runtime and atomic publication are unchanged.
- No persistent preparation cache or mutable JSON tree is retained. 88 new cases compare frozen
  preceding algorithms, owned/borrowed trees, failure/retry and all four archive profiles.
  All 1,942 interoperability / 2,274 full Release cases pass; fixed artifacts remain unchanged.
- [Design, matched model/restore/full-recovery measurements and bindings](MODEL-PREPARATION.md)
  preserve the same prepared archives, C# harness binary and dependency bytes on both sides.

### Implemented: bounded canonical identity/signature preparation

- Reuse validated immutable unsigned bytes by reference during synchronous recovery/peer loops;
  canonical key/profile envelopes retain exact Styx ordering, escaping, numeric/SI spelling and IDs.
- Admission is bounded by 64 entries, 1 MiB/value and 4 MiB of canonical bytes. Oversized/saturated
  contexts prepare fresh content; every outer scope releases all entries on success/failure.
- Public arrays remain detached; original peers, fresh keys/policies, eager/lazy model errors,
  runtime and atomic publication stay exact. No key/trust/persistent preparation cache is added.
- Frozen preceding factories/writers and all ownership/bounds/depth/retry contracts add 106 cases;
  all 2,048 interoperability / 2,380 full Release cases pass with unchanged fixed references.
- [Design, four-stage matched measurements and bindings](CANONICAL-PREPARATION.md) retain the
  same prepared inputs, results, C# harness binary and dependency bytes across 112 workers/336 samples.

### Implemented: exact root/head snapshot reuse after full model validation

- Fresh root/head models run the complete preceding domain parser, metadata/reference checks and
  representation completion. Exact property/child/version equality permits sharing the retained
  immutable snapshot; mismatched spellings/defaults use the original capture/normalization path.
- A private root-only head retains its freshly validated root model. Independent histories/readers
  own separate runtime objects. Every peer/current authority check, eager/lazy errors, cancellation
  and atomic publication stay intact; no global model/preparation/trust cache is added.
- 86 cases compare the ordinary preceding reconstruction route, all represented identities, exact
  property/canonical bytes, fallback/errors, root-only heads, concurrent runtimes, four profiles,
  bootstrap, cancellation and atomic rejection/retry. All 2,134 interoperability / 2,466 full
  Release cases pass with zero failures; fixed references remain unchanged.
- [Design, four-stage matched measurements and bindings](SNAPSHOT-RECONSTRUCTION.md) preserve the
  existing seven prepared archives/results and C# harness/dependencies. Broader model/projection
  reconstruction and conservative lexical fallback remain costs. Complete CBOR allocation falls
  1.54–7.69%; prepared-model restore falls 2.61–12.19% in the 336-sample matched comparison.
  Controls and collected retained memory include increases; no general capacity/memory bound follows.

### Implemented: immutable signature-envelope copies

- Signature-only commit copies retain their validated unsigned identity. Embedded batch replacement
  requires shared immutable operation/ETag arrays and exact unsigned headers/description/raw metadata;
  independent arrays or changed content retain full construction, validation and declared-ID checks.
- Existing scoped canonical entries can be shared within unchanged conservative admission bounds;
  public bytes stay detached, scopes release all aliases, and no persistent payload/key/trust cache is added.
- 103 cases compare full preceding construction/frozen canonical bytes, all kinds/peers, changed fields,
  errors, equality, alias bounds/threads, four profiles and fresh atomic duplicate/pack union/retry.
  All 2,237 interoperability / 2,569 full Release cases pass; fixed references remain unchanged.
- [Design, direct matched measurements and bindings](SIGNATURE-COPIES.md) cover 80 workers/240 calls
  with the same new harness binary/dependencies. Real signing retains crypto/preimage costs; prepared
  batch metadata copies, transport and policy verification are excluded from the measured copy probes.
  Sixteen 512-EVSE snapshot signature copies allocate 84.7071 -> 0.0015 MiB; real signing falls
  44.71–48.93% across the four shapes. No end-to-end recovery/capacity improvement is inferred.

### Implemented: finer cooperative archive-parsing cancellation

- Existing tokens reach owned CBOR limit/index/canonical loops and individual commit/receipt/model
  boundaries, suffix capture and root/replay/head preparation through an explicit per-call context.
- Exact Styx diagnostics, budgets/depth/EOF/duplicate equality, eager/lazy trust timing, source
  ownership, private-history/scope cleanup, late token release and atomic installation remain.
- 270 cases cover deterministic stage/offset cancellation, original token, failure/retry, frozen
  preceding diagnostics/budgets, successful late publication and nested/concurrent readers.
  All 2,507 interoperability / 2,839 full Release cases pass with unchanged fixed references.
- [Contracts, matched overhead measurements and bindings](PARSER-CANCELLATION.md) compare six
  shapes/all four profiles with 72 workers/216 samples and the same new harness/dependencies.
  Individual Styx/model/crypto/state calls and bulk copies remain synchronous; no latency bound follows.

### Implemented: richer shared-reference/tariff/parking recovery workloads

- Shared grid/software/document registries, pool/point/station/EVSE meters, nested tariffs and
  parking relationships cover eight deterministic 16/64-EVSE shapes across all four profiles.
- Seven reference/value/membership edits, repeated after snapshots/boundaries, and an explicitly
  signed two-parent merge retain original peers, both branch states and independent local runtime.
- Four production recovery stages record actual inventories and costs in 64 fresh workers /
  192 calls. Exact byte/head/branch/reference/peer controls run outside measurements. Production,
  dependencies and all 32 references remain unchanged; six old simple-workload controls still match.
- 81 cases cover graphs, transports, spool/lease release, bootstrap trust failure/retry, invalid
  reference/value/extra-peer atomicity, detachment, merge and cultures. All 2,588 interoperability /
  2,920 full Release cases pass. See [design, baseline and bindings](DOMAIN-RECOVERY-WORKLOADS.md).

### Implemented: temporary validation projections for a fixed immutable map

- Allocation stacks select repeated recursive projection/station resolution. One private pass
  reuses successful ancestors and a station view; failed projections remain uncached. Entry
  counts follow the current map; no model/runtime/history/trust cache survives the pass.
- Added subtrees, approval consumers and merge targets retain ordinary parser/reference order,
  full group owner reconstruction, exact failures, atomic rejection and independent runtime.
- All 22 entity kinds and groups are covered. A discovered tariff-group *T text bug is corrected
  to *TG, with matching length/empty flags and equal ordering across equivalent operator formats.
- 71 cases add frozen projection comparisons, bounds/identity/runtime/culture checks, exact old
  archive/static/branch hashes and atomic subtree/merge cases. All 2,659 interoperability /
  2,991 full Release tests pass with unchanged 32 references.
- [Design, stack profiles and matched rich recovery measurements](VALIDATION-PROJECTION.md)
  retain exact archives/branches/peers and the same C# harness/dependencies. At 64 EVSEs, CBOR
  cumulative allocation falls 33.66–42.18%; no capacity/constant-memory bound follows.

### Implemented: normalization-aware immutable snapshot map preparation

- Complete ordinary parsers, normalization and reference checks precede reuse of exactly equal
  immutable properties, child sets, entity/map branches and root catalogs. Removed keys/consumers
  are detached; revision bookkeeping stays independent. Changed maps receive fresh ETag contexts.
- Binding reads stable typed identities directly. Runtime/current trust/error order, canonical
  bytes, eager/lazy recovery and atomic publication remain; no mutable JSON/model/trust cache is added.
- 53 cases cover all 22 kinds, exact ordinary capture, map/catalog sharing, ownership/revision/error/
  retry controls, customer numeric/escaped data, binding IDs and all retained rich archive states.
  All 2,712 interoperability / 3,044 full Release cases pass with unchanged 32 references.
- [Design, matched rich recovery measurements and bindings](SNAPSHOT-MAP-PREPARATION.md) use the
  same archives, branches, original peers and C# harness/dependencies. At 64 EVSEs, CBOR cumulative
  allocation falls a further 16.63–19.38%; temporary allocation and retained memory stay distinct.

### Next package: larger group/catalog and history scaling workloads

- Extend the bound fixtures with larger shared registries, all four group kinds, more reference
  consumers, longer histories and branches. Preserve exact content, original peers and runtime.
- Measure controlled warmup/JIT and production-like concurrency separately from the current
  sequential desktop series. Keep cumulative allocations, collected retained memory and elapsed
  time distinct; derive targeted next changes from those workloads.
- Independent peer implementations, service/client/current-policy integration, durability and
  individual parser cancellation remain separate work.

Streaming replay, compact catalog indexes, automatic schedules, multiple anchors,
independent implementations and larger production/concurrency measurements remain additional work.

Announced chain/epoch transitions remain a possible later extension. The implemented retention workflow
uses snapshot boundaries within the existing chain and independently configured archive availability.
