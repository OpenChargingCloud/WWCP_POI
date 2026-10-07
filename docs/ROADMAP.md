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
  ancestor choices in criss-cross histories, resolver reentry/exceptions and unschedulable replacement.
- The README distinguishes static versioning from live runtime values and explains CBOR
  revision transport. The historical Styx patch no longer claims a current Git metadata failure.

The interoperable static profile is now **`wwcp-poi-static-v1`**, declared in tagged transports and
bound into commit IDs/signatures. Fixed JSON/CBOR/state/commit/signature/merge/archive references
are published. The full NUnit run on 2026-10-07 passed **509 tests**, with one skipped child
worker that is executed separately by the process-crash tests. See [interoperability](INTEROPERABILITY.md).

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
| GridOperator | Network child; independent embedded description in connection points | Registry and embedded content participate in their owners | Independent network node; nested singleton/property edits for embedded slot |
| ChargingCable | Connector property | Included in connector | Singleton/property edits by connector owner; no invented ID |
| TransparencySoftwareStatus, TransparencySoftware | Nested under meters | Included in meter, including legal/certificate metadata | Replace the selected meter's software array or whole meter; software values have no IDs |
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

## 2. Complete addressing and collection operations — partly implemented

`AddElement`, `RemoveElement`, `ReplaceElement` and `UpdateElementProperty` address nested owned
values through typed property/ID paths. Existing IDs define collection membership; singletons can
use their owner/property slot. Duplicate scope, quantity/old-value validation, deterministic
ordering, metadata propagation, runtime lifetimes and tariff reference maintenance are implemented
for the [supported relations](ELEMENT-OPERATIONS.md#supported-ownership-paths). Whole-property
replacement remains an explicit operation.

[Graph integration](GRAPH.md) imports the groups, manufacturers and grid/parking operators,
including parking children. Ownership scopes, independent node addresses, reference resolution,
reverse index maintenance, deletion protection and runtime retention are implemented. Group
admission lists may refer to future IDs; active members must resolve. Embedded connection-point
operators remain independent descriptions rather than aliases of network registry entries.

Value collections without IDs still need an explicit identity decision if finer operations are
required; array indices are not stable identities. Parking space groups do not yet have space
membership, and parking products have no graph owner. Unifying embedded GridOperator descriptions
with registry references remains a contract decision. History, atomic publication and explicit
three-way integration of already-published divergent heads are available below.

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
The fixed static-v1 profile and references now cover signed two-peer roundtrips, optional presence,
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
archive scalability and cached-snapshot performance remain to be established.

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
ancestor choices and resolver reentry/exception rollback. Operation-history lifetime proofs for
reused creation metadata and exhaustive graph/reference combinations remain additional work.

## 5. Freeze interoperable profiles and exchange behavior — partly implemented

The [static-v1 profile](INTEROPERABILITY.md) fixes stored-property projection, canonicalization and
normalization; transport declarations reject unknown profiles and commits explicitly bind it.
Fixed reference files cover JSON/CBOR state bytes, ETags, unsigned identity/signing bytes,
two-peer signatures, custom data, timestamps, optional presence and disjoint/resolved merge archives.

[Incremental replication](REPLICATION.md) now provides bounded missing-ancestry pages, atomic
retention, explicit expected-head adoption and divergence notices. Secondary-parent adoption
selects the original merge and uses conservative local lifetime proofs. Different checkpoints
require explicit [bootstrap](BOOTSTRAP.md), now available as bounded resumable archive fragments
with preview/activation. Source export and final replay still materialize a complete archive;
a streaming archive codec remains planned. Transport/key
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

Broader nested/graph/ownership combinations, operation-history lifetime proofs, recursive merge bases
and performance remain additional work. There is no power-loss simulation or independently
implemented remote peer yet. Measure full static hashing, import/materialization, archive rewrite/replay
and runtime capture before adding caches or a Merkle profile.
