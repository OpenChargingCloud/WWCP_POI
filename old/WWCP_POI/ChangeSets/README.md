# Versioned infrastructure and change sets

[Repository overview](../../README.md) · [Architecture](../../docs/ARCHITECTURE.md) · [ChangeSet examples](../../docs/CHANGESETS.md) · [JSON contracts](../../docs/JSON.md) · [Signatures and trust](../../docs/SIGNATURES.md)

```csharp
using System.Text.Json;

var before = roamingNetwork.DataSnapshot;
var oldPower = before.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1")
                     .Properties["maxPower"];

var changeSet = before.CreateChangeSet(
    id: "change-42",
    createdAt: DateTimeOffset.UtcNow,
    changes: [RoamingNetworkChange.UpdateProperty(
        "EVSE", "DE*ABC*E1", "maxPower", oldPower,
        JsonSerializer.SerializeToElement("150 kW"))]);

var next = roamingNetwork.ApplyChangeSet(changeSet);
// roamingNetwork is still revision 0; next is revision 1.
var document = next.ToJSONSnapshot();
var restored = RoamingNetwork.Parse(document);
```

## Storage and domain projection

`DataSnapshot` captures an immutable POI network once and becomes its authoritative versioned
data. A snapshot contains immutable entity documents and parent/child keys, backed by persistent
`ImmutableDictionary`/`ImmutableHashSet` collections. Updates replace affected entries and their
ancestors; unrelated entries and unchanged property values are shared. Operation validation
reconstructs affected nodes and their ancestors; groups/parking resolve additional reference context. Mandatory before/after ETag calculation additionally
exports the complete stored static hierarchy and both canonical encodings; it is proportional to the data
size. Immutable snapshot identifiers are computed lazily and cached. `CreateChangeSet` prepares
both arrays on unpublished immutable maps; `ApplyChangeSet` checks both before returning a result.

Static exports and hashes retain stored optional properties, including valid explicit nulls.
Network import captures supplied revision metadata or revision zero; managed timestamp defaults
are fixed once and owned graph arrays export as sorted arrays including `[]`.
`RoamingNetworkDataSnapshot.Parse` and `ParseCBOR` validate declared ETags on reload.
Complete ChangeSet CBOR exchange preserves all peer signatures and their v2 signing input;
see [binary transport](../../docs/CHANGESET-CBOR.md).

For large data sets, read `DataSnapshot.Entities`, `GetEntity()` and `GetEntityJSON()` directly.
`WriteTo(Utf8JsonWriter, IncludeETags: false)` streams the complete snapshot without creating an intermediate JObject
hierarchy. `GetEntityJSON()` returns a fresh, editable JSON document without exposing mutable storage.

The domain classes have immutable static POI data. On a newly applied network, accessing
operators/pools/stations/EVSE collections materializes the complete hierarchy lazily, with fresh
objects and parent pointers into that version. Statuses and real-time measurements remain mutable
inside the entities. Derivation captures their histories and values into independent runtime
state, visiting the source hierarchy in addition to the persistent storage update.

Pool/station `MaxCurrent`, `MaxPower` and `MaxCapacity` are readonly constructor-supplied
`Ampere`, `Watt` and `WattHour` quantities. Their `maxCurrent`, `maxPower` and `maxCapacity`
properties can be updated through ChangeSets using explicit SI strings, with equivalent units
normalized for preconditions and storage. Their operational measurements and forecasts use
the same quantity dimensions but remain runtime data outside the static content identifiers.

Static changes use ChangeSets. `ToJSONSnapshot()` exports the static version with current runtime
statuses; `DataSnapshot.WriteTo()` exports only static data and revision metadata. Runtime changes do not edit
`DataSnapshot`, POI timestamps or revision. A persisted snapshot carries its `revision` and
`appliedChangeSetId`; reloading it preserves both.

## Operations

Entity type names and property names are the case-sensitive names in the public enum and POI JSON
contract. Supported entities include the complete [owned graph](../../docs/GRAPH.md): infrastructure,
providers, groups, manufacturers, grid/parking operators and parking children. Nested brands,
licenses, cables and energy meters use owner property or addressed element operations.

- **Add:** pass an entity document with a matching identity (`id` for grid/parking operators,
  `@id` otherwise). Nodes below the network root require `ParentEntityType` and `ParentEntityId`.
  Network-owned nodes default to the current network parent. Nested children may be included, and are validated as part of the operation. Use expanded
  embedded POI documents, or `GetEntityJSON()` from another snapshot, rather than unresolved IDs.
- **Remove:** removes the entity and its descendants. An optional `OldValue` checks its complete
  document, including children. A missing entity is a conflict. Removing the network root is forbidden.
- **UpdateProperty:** replaces a top-level JSON property; the optional `OldValue` is checked against
  the value produced by preceding operations in the same batch. IDs, ancestry, child arrays,
  creation/change timestamps and revision fields cannot be replaced as properties. Moving a subtree
  is an explicit remove followed by add beneath its new parent.
- **RemoveProperty:** removes an existing editable optional key, has no `NewValue` and permits
  an `OldValue` precondition. Remaining domain content/references are validated; absence differs from null.
- **AddElement/RemoveElement/ReplaceElement/UpdateElementProperty/RemoveElementProperty:** target one nested value via
  an immutable schema-property/ID `ElementPath`. Individual preconditions and deterministic ID
  ordering enable disjoint nested edits. See [supported paths and examples](../../docs/ELEMENT-OPERATIONS.md).

Connector IDs are local to an EVSE. All connector operations and lookups require an explicit EVSE
parent/scope, so connector `1` on one EVSE cannot accidentally target connector `1` on another.
Snapshot key equality follows the domain's ID equality, including equivalent separator/case forms.

Omitting `OldValue` skips the precondition. A present JSON null expects a present null property;
it does not match an absent property. A new JSON null clears a nullable property, and remains
explicitly present in snapshot storage. Static ChangeSets reject operational status/admin-status,
history, measurement and forecast fields, including in nested payloads and old-value preconditions.
Use static `ToJSONWithETags()` or `GetEntityJSON()` operation documents. The separate
[runtime API](../../docs/RUNTIME.md) supplies timestamped instructions and optional current-status
and static-content preconditions without advancing the static version.

Each successful batch increments the revision once, including an empty batch. Metadata-bearing entity and ancestor
change timestamps use the batch's `CreatedAt`; existing creation timestamps remain unchanged.
New entities and nested POI replacements receive deterministic metadata defaults from `CreatedAt`
where omitted. Required `BeforeETags` and `AfterETags` each contain JSON and CBOR SHA-256
identifiers as `ImmutableArray<ETag>`. Each readonly value contains typed format/algorithm and
immutable digest bytes. JSON uses `[format, algorithm, encoding, encodedDigest]`; CBOR ETag arrays use native digest
bytes. Both JSON serializers have matching type converters. The readable `ToString()` form is
`format:algorithm:encoding:digest`, with HEX by default. JSON supports explicit `hex` and canonical
standard `base64`; both decode to the same byte-valued identifier. Transport encoding is outside
ETag equality. The readable form supports logging and explicit text parsing. Both source
identifiers are checked before operations, both result identifiers after
timestamp updates. Content mismatches fail at batch level with no operation index.
Revision/network mismatches, invalid fields,
duplicates, wrong parents and failed preconditions abort the entire batch. `ApplyChangeSet()` throws
`RoamingNetworkChangeSetException` with the operation index; `TryApplyChangeSet()` returns an error
without a partial result. Independent branches may be built concurrently from the same snapshot.

## Explicit merging

`snapshot.TryMerge(left, right, out merged, out report)` validates both batches against the common
source, including source/result ETags and optional signatures, then checks both execution orders.
They must be valid and yield equal entity structure and complete stored static properties.
Runtime updates are outside the merge. A compatible preview returns true and `MergeAvailable`, but `merged` is null.

Use `merge: true, mergedChangeSetId: "new-id"` only after an explicit decision to combine the
batches. The result is unsigned, preserves left-then-right operations and old-value checks, and
has fresh result ETags. A single merge timestamp defaults to the later input timestamp; optional
`createdAt` overrides it. Apply the result separately to the common source for one new revision.
This API does not rewrite preconditions or deduplicate operations. Explicit element operations
can merge independent edits; whole-property replacements retain their whole-value semantics.
Structured issues include the element path of failed original operations and report invalid input batches
or order-dependent result fields. See [merge examples and boundaries](../../docs/CHANGESETS.md#merging-concurrent-batches).

## Signatures and boundaries

A batch's `Signatures` array contains equal peer envelopes. Signed batches require a caller-provided
`VerifySignature(batch, signature)` callback for every peer; one failure rejects the whole batch.
`Sign`/`TrySign` append signatures using Styx `COSEAlgorithm` and asymmetric keys, while
`VerifySignature`/`VerifySignatures` supply cryptographic checks with application-trusted public keys.
`WithSignature` appends an externally prepared envelope. The `wwcp-poi-changeset-json-v2` profile
binds the before/after arrays, header, complete operations, multilingual `Description` and arbitrary
JSON `Metadata`, together with the peer's algorithm/key ID/profile/encoding. Description/metadata
edits return an unsigned copy; the peer array is excluded to permit independent additional signers.
Canonical JSON and deterministic CBOR SHA-256 POI identities bind the source and result data.
`RoamingNetworkHistory` retains original batches and static snapshots, supplies a typed ancestry-bound
commit identity and publishes/persists heads against an expected prior commit. Independent commit
signatures authenticate ordered parents. `AppliedChangeSetId` alone records only the latest batch;
use the [history archive](../../docs/HISTORY.md) for retained ancestry and replay recovery.
`RoamingNetworkHistory.TryMerge` now integrates retained/published branches via three-way static
comparison and structured explicit resolution. It prepares a fresh commit against the left tip
with both parents and signed ancestor/resolution metadata. See [branch integration](../../docs/MERGING.md).
Key management, recursive virtual bases and rebase remain application work or roadmap extensions.

The snapshot retains the existing POI snapshot contract: current timestamped statuses, entity
timestamps and custom data, but no status history or properties absent from the POI serializers.

## Charging tariffs

Operators own `chargingTariffs`; EVSEs and connectors reference them through `tariffIds`. Tariffs
are independent immutable entity nodes, so changing a price shares the entire station subtree.
`elements`, `currency`, `brand`, `uri` and `energyMix` can be updated as tariff properties.
Price components and restrictions are nested tariff values; replace `elements` to edit them.

`References` is a persistent reverse index from target entities to consumers. It covers tariffs,
group members and parking links, validates ownership scopes and protects deletion. `TariffReferences`
exposes its tariff subset. It is built during capture and incrementally maintained for both node
and addressed element operations. See [graph integration](../../docs/GRAPH.md).

`GetChargingTariffs()`/`GetChargingTariffIds()` now return registered tariffs without filters,
or assigned tariffs for the supplied pool/station/EVSE/connector. Connector queries require an EVSE
scope and include the EVSE's direct assignments. Provider-specific tariff agreements are not modeled;
passing an EMobilityProvider filter raises `NotSupportedException`.

Tariff prices are decimal JSON numbers without cent rounding. Energy/power bounds and energy/current
billing steps use unit-bearing strings. Restrictions write `minEnergy`/`maxEnergy` and expose
typed `Range<WattHour?>` and `Range<Watt?>` bounds in C#. Date restrictions
use `startDate`/`endDate` as ISO timestamps; duration restrictions use `minDuration`/`maxDuration`
with unit-bearing seconds and TimeSpan tick precision. Billing increments require positive quantities.
Energy mixes include `energySources` and `environmentalImpacts` arrays with unit-bearing `percentage`
strings. Both arrays are required; use `[]` for unknown composition.

Use `RoamingNetwork.Parse(string)` or `ChargingTariff.Parse(string, operator)` to load numeric prices
without converting them to binary floating point first. When supplying a JObject, load it with
`JsonTextReader.FloatParseHandling = FloatParseHandling.Decimal`. Precision already lost in a
JObject created with the default double reader cannot be recovered by a parser.

## Transparency software, certificate documents and meters

The network owns identified `TransparencySoftware` releases and identified
`TransparencySoftwareCertificate` approval/compatibility documents. Add, remove and edit these
with graph operations. A document describes a station model/version, its issuer and software IDs
classified as verified or compatible. Optional validity belongs to the document. IDs protect
referenced entries from deletion; changing a document revalidates its consuming assignments.
See [domain contract](../../docs/DOMAIN-MODEL.md#transparency-software-and-certificates).

`TransparencySoftwareStatus` is a nested immutable assignment in a meter's `transparencySoftware`
array. It contains `transparencySoftwareId`, `legalStatus` and optional `certificateId`. The array
has one assignment per software ID. Assignments resolve the shared network entries rather than
embedding descriptions. Change the array on an addressed meter, or replace the owning meter
property with an optional old-value precondition. The following requires an existing document
`certificate-2` covering the selected software:

```csharp
var oldMeter = roamingNetwork.DataSnapshot
    .GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["energyMeter"];
var meter = System.Text.Json.Nodes.JsonNode.Parse(oldMeter.GetRawText())!;
meter["transparencySoftware"]![0]!["legalStatus"] = "verified";
meter["transparencySoftware"]![0]!["certificateId"] = "certificate-2";

var change = RoamingNetworkChange.UpdateProperty(
    "EVSE", "DE*ABC*E1", "energyMeter", oldMeter,
    JsonSerializer.SerializeToElement(meter));
```

Pools and stations directly own zero or more `energyMeters`; an EVSE or point owns an optional
single `energyMeter`. Meter roles such as `grid`, `pv` and `battery` are immutable. Use element
Add/Remove/property operations for meters, or whole-property replacement (`[]` clears an array).
Static edits preserve independent runtime by owner/meter identity. Removal/reintroduction starts
a new runtime lifetime even if the final ID is reused; runtime fields are rejected in static payloads.

A pool's optional `gridConnectionPoint` has a required `gridOperatorId` resolving the network
registry and an optional owned meter. Edit its properties or replace it; JSON null removes it.
Changing its identity resets the point meter, while the shared operator retains its schedule.
See [Grid connections](../../docs/GRIDCONNECTIONS.md).

Software JSON requires a multi-language `name` and a nonempty `openSourceLicenses` array of license
objects (`@id`, `description`, `URLs`), never a string license. Software links, license URLs and document URLs must be absolute. Certificate
`NotBefore`/`NotAfter` are nullable UTC-normalized `DateTimeOffset` values with tick precision;
JSON timestamps require `Z` or an explicit offset. Reversed intervals, duplicate/disjoint-list
violations and dangling references fail validation. At least one verified or compatible software
ID is required. Legal labels remain extensible. The document is domain evidence, with authenticity
and station deployment matching left to the application; see [signatures](../../docs/SIGNATURES.md).

Energy meter JSON retains creation/change timestamps, current timestamped statuses, data source
and custom data. Runtime status stays outside static hashes. Tariff elements, price components
and restrictions remain value substructures without IDs.

## Source organization

The public change set, operation, signature, entity key, entity type and exception each live in
separate files. The snapshot implementation is grouped by responsibility:

| File | Responsibility |
| --- | --- |
| `RoamingNetworkDataSnapshot.cs` | Immutable data, lookup and initial capture |
| `RoamingNetworkChangeSet.CBOR.cs` | Deterministic batch transport with native ETags and exact signed-value preservation |
| `RoamingNetworkDataSnapshot.Changes.cs` | Batch validation and ordered add/remove/property operations |
| `RoamingNetworkDataSnapshot.Merge.cs` | Explicit merge preparation, input checks and comparison of both execution orders |
| `RoamingNetworkChangeSetMergeResult.cs` | Immutable merge status, notices and structured issues |
| `RoamingNetworkChangeSet.Signing.cs` | Canonical signature input, Styx signing and peer verification |
| `RoamingNetworkDataSnapshot.Import.cs` | Hierarchy import, metadata normalization and subtree traversal |
| `RoamingNetworkDataSnapshot.Validation.cs` | Ancestor projections plus group/parking reference context |
| `RoamingNetworkDataSnapshot.References.cs` | Incremental reverse graph reference index |
| `RoamingNetworkDataSnapshot.Json.cs` | Entity JSON and direct nested export |
| `RoamingNetwork.CopyOnWrite.cs` | Roaming network API and lazy immutable hierarchy projection |
| `RoamingNetwork.RuntimeState.cs` | Independent runtime histories and measurements for derived versions |

All partial snapshot files operate on the same immutable storage. This source organization does
not introduce extra copies of entities or JSON trees.

## Interoperability evidence

The fixed [static-v2 profile](../../docs/INTEROPERABILITY.md) supplies exact JSON/CBOR/digest/signature
references and executed replica, resolution, expected-head race and archive-recovery workflows.
Commit IDs/signatures bind the profile explicitly; static ETag inputs exclude its transport declaration.
