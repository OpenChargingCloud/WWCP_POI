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
- Addressed nested operations use immutable ownership paths and existing element identities.
  Per-element/property preconditions, deterministic touched collection ordering and tariff-index
  maintenance enable disjoint nested edits. Built-in signing uses v2 and binds every path.
- Runtime capture detects explicit removal/reintroduction and temporary identity/slot replacement
  within a batch, so reused final IDs do not silently inherit an earlier runtime lifetime.
- The README distinguishes static versioning from live runtime values and explains CBOR
  revision transport. The historical Styx patch no longer claims a current Git metadata failure.

The library and existing test project compile with these corrections (0 errors, 378 warnings in the graph package build). Existing status/operation
fixtures were adapted to the separated APIs; no tests were added or executed for these packages.

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
batch transactions and an integrated head publication service remain application work or future
extensions; the status instruction API does not implement those features.

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
with registry references remains a contract decision. The next package is the ChangeSet CBOR
transport and consistent versioned reload semantics below.

## 3. Complete versioned snapshot transports — partly implemented

Canonical static content, version metadata and current-status export now have independent
options. Default `ToCBOR()` omits revision metadata; use `IncludeVersionMetadata: true` to
continue the numeric revision chain after reload. `IncludeRuntime: true` additionally carries
current statuses. `ToJSONWithETags()` has the same flags; `DataSnapshot` accepts only static
export. `ToJSONSnapshot()` exports the version with current statuses.

Add a complete ChangeSet CBOR codec and specify that transport choices do not alter the signed
logical batch. Define static defaults and absent/null property handling consistently across reloads.

## 4. Add history, publication and integration of divergent heads — planned

Define a stable commit content identity binding parent identities, state identifiers, ordered
operations and commit metadata. Keep peer signatures separate so appending another signature
does not change that commit identity. Persist original batches, snapshots and merge ancestry.

Add atomic head publication against an expected prior head, duplicate detection and recovery
after restart. Decide whether numeric revisions are shared chain coordinates or local indexes;
the current applier requires exact `BaseRevision` equality in addition to matching ETags.

Extend common-source `TryMerge` with integration when one branch has already been published,
using a retained common ancestor. Provide structured base/left/right conflicts and explicit
resolution. Specify compatible duplicate writes rather than relying only on whether unchanged
operation sequences commute. A prepared merge requires its own metadata and signatures.

## 5. Freeze interoperable profiles and exchange behavior — planned

Version the static projection/canonicalization profile explicitly. Publish fixed JSON, CBOR,
ETag and signature vectors, including equivalent units, custom data and timestamps.
Clarify that static creation/change metadata and wire identifier spelling are content; independently
importing equivalent facts with different defaults need not produce the same static ETags.

Specify missing-commit retrieval, bootstrap, divergent heads and application trust policy.
Signature validity alone does not specify authorized keys, required distinct signers or quorum.

## 6. Establish complete workflows and performance evidence — planned

The reference workflow should cover two replicas with different runtime statuses and equal static
ETags, signed transition exchange, before/after rejection, concurrent edits, explicit merge,
versioned JSON/CBOR reload, continued exchange and duplicate delivery.

Dedicated ETag, CBOR, cryptographic signing, multiple-signature, nested-operation and merge
coverage remains to be added. Measure full-projection hashing and runtime capture separately from persistent-map updates
before adding caches or a Merkle profile.
