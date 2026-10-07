# Runtime statuses and static POI versions

Administrator-created [snapshot commits](SNAPSHOTS.md) advance history bookkeeping without editing
static data or runtime. Gated publication and first-parent adoption capture current local runtime
into independent schedules; snapshots do not reset entity lifetimes. Archive recovery and bootstrap
still initialize fresh local runtime, which must be restored through runtime delivery separately.

[Authorized snapshot entry](SNAPSHOT-BOUNDARIES.md) also initializes fresh runtime in a separate
history. Retained suffix adoption uses its actual operations for local continuity; omitted earlier
history supplies no birth/continuity proof. Equal static ETags do not authorize transferring runtime
from another live history or replacing its head implicitly.

[Repository overview](../README.md) · [ChangeSets](CHANGESETS.md) · [JSON](JSON.md) · [Roadmap](ROADMAP.md)

## Static boundary

Domain objects contain immutable static POI data and mutable operational state. `DataSnapshot`
stores only static data. Schema-defined operational/admin statuses, histories, measurements and
forecasts are removed during snapshot import/capture. Static ETags, old-value checks and merges
therefore do not depend on runtime state.

Static ChangeSets reject runtime fields in property updates, Add documents, complete Remove
preconditions and nested replacement values, including supplied old values. Use static
`ToJSONWithETags()` exports or `DataSnapshot.GetEntityJSON()` for these documents. This validation
follows the POI schema: `customData.status` remains customer content, and the legal/certificate
status in `TransparencySoftwareStatus` remains immutable static data.

`ToJSONSnapshot()` overlays current statuses from the domain objects on the static version.
Parsing that export restores current statuses in domain objects; it never puts them into
`DataSnapshot`. Histories and electrical runtime measurements are not persisted by this export.
`DataSnapshot.ToJSON()` and `WriteTo()` export only the static version and revision metadata.

## Apply a runtime instruction

`RoamingNetworkRuntimeUpdate` is an immutable instruction for one operational or admin-status
schedule. `ApplyRuntimeUpdate()` applies it to the existing network instance. Local domain
status setters and schedule methods remain available.

```csharp
var evse = network.GetEVSEById(EVSE_Id.Parse("DE*ABC*E1"))!;
var update = new RoamingNetworkRuntimeUpdate(
    roamingNetworkId: network.Id.ToString(),
    target: POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, evse.Id.ToString()),
    kind: POIRuntimeStatusKind.Status,
    newStatus: new POIRuntimeStatusValue("charging", DateTimeOffset.UtcNow),
    expectedStatus: POIRuntimeStatusValue.From(evse.Status),
    staticETags: network.ETags);

network.ApplyRuntimeUpdate(update);
```

Application checks the network ID, optional static ETags, existing target and domain status
parser before updating the schedule. Invalid IDs, missing targets and invalid status labels
raise `ArgumentException`; failed content/current-status preconditions raise
`InvalidOperationException`. A successful instruction leaves `DataSnapshot`, revision,
static timestamps and static ETags unchanged. It does not create a new network version.

`ExpectedStatus` is optional. When present, both the current value and exact timestamp must
match under the schedule's mutation lock. This compares the current status, not the complete
history. It can prevent a competing current-status write, but is not a replay ledger.
`StaticETags` is optional; an empty array omits the static-content precondition. When supplied,
it must be the complete JSON/CBOR pair for the target network. It binds delivery to that static
content without requiring a numeric revision. Runtime updates can differ between replicas
with identical static ETags.

### Targets and ownership

| Factory | Target |
| --- | --- |
| `POIRuntimeTarget.Entity(type, id)` | Any status-bearing graph node, including groups, grid/parking operators and parking children |
| `POIRuntimeTarget.Meter(ownerType, ownerId, meterId)` | Direct pool/station meter, or the EVSE's meter |
| `POIRuntimeTarget.ConnectionMeter(poolId, meterId)` | Meter in the pool's grid connection point |
| `POIRuntimeTarget.ConnectionGridOperator(poolId, operatorId)` | Grid operator in the pool's grid connection point |

Nested targets require both the static owner ID and the child ID in the specified ownership
slot. Meter IDs use the existing meter identity; their POI JSON spelling is `id`. A direct pool
meter and a connection-point meter are distinct targets even when their meter IDs are equal.
Connectors and manufacturers have no status schedule and are rejected. Network groups,
grid/parking operators and parking children have independent entity targets. A registered
GridOperator and a connection-point GridOperator with the same ID are distinct runtime targets;
their schedules are independent.

Connection-point targets resolve the pool's current connection point. They do not carry a
separate connection-point ID. If an owner/child ID is reused for a new static lifetime, bind
delayed messages with `StaticETags` when delivery to that new lifetime would be incorrect.

### Insert and explicit history replacement

The default `POIRuntimeUpdateMode.Insert` inserts one entry and retains the existing history,
subject to its configured capacity. Entries at the same exact UTC timestamp replace each other;
distinct ticks remain distinct. Historical/future entries are allowed and need not change the
current status. Future entries become current according to the existing schedule behavior.

`POIRuntimeUpdateMode.ReplaceHistory` explicitly clears that one status/admin-status history
and replaces it with the supplied entry. It retains the schedule capacity. The replacement
entry must be current or historical; future-only replacement is rejected. Use `Insert` for
scheduled future statuses. Replacing an operational history does not replace the admin history.

This contract currently covers status/admin-status entries. Measurements, forecasts,
`LastStatusUpdate` and application `InternalData` retain their existing runtime APIs; a status
instruction does not automatically populate those values.

## JSON contract

Use System.Text.Json for runtime instructions:

```csharp
var json = JsonSerializer.Serialize(update);
var received = JsonSerializer.Deserialize<RoamingNetworkRuntimeUpdate>(json)!;
network.ApplyRuntimeUpdate(received);
```

Default property names follow the C# names, independently of nested POI document spelling:

```json
{
  "RoamingNetworkId": "network-a",
  "Target": {
    "Kind": "Entity",
    "OwnerEntityType": "EVSE",
    "OwnerEntityId": "DE*ABC*E1"
  },
  "Kind": "Status",
  "NewStatus": {
    "Value": "charging",
    "Timestamp": "2026-10-07T12:30:00.0000000+00:00"
  },
  "StaticETags": [],
  "Mode": "Insert"
}
```

Network, target, kind and new status are required. Nested status values require both value
and timestamp; nested targets require kind, owner type and owner ID. `ChildId` is required for
meter/operator slots. `ExpectedStatus`, `StaticETags` and `Mode` are optional. Unknown properties
are rejected on instruction, target and status records. Enum fields use string names and reject
numeric JSON. Timestamp strings require an explicit offset or `Z` and are normalized to UTC.
There is no inferred local-clock timestamp. ETags use the existing typed tuple converters.

There is no runtime CBOR codec, signature envelope, batch transaction, sequence number,
automatic deduplication or freshness policy in this API. Static ChangeSet signatures do not
authenticate runtime instructions. The application supplies authentication, authorization,
delivery ordering and any replay policy on its runtime channel.

## Runtime retention across static changes

`ApplyChangeSet()` captures the source version's runtime state when deriving the successor,
even if the successor's domain hierarchy is materialized later. Schedules and measurements
are copied independently: updates on either version do not subsequently edit the other.

- Surviving graph nodes retain their runtime state unless their subtree was removed/readded.
- Existing nested meters retain histories and capacities when owner and meter ID match,
  including complete static `energyMeter`/`energyMeters` replacements.
- A connection-point meter/operator additionally requires matching connection-point ID.
  Two absent IDs identify the same optional owner slot.
- New child IDs, changed connection-point IDs and removed/recreated owner subtrees start
  with domain runtime defaults. Static payloads cannot inject runtime histories or statuses.
- An addressed child removed/readded in the same batch starts a new lifetime even if its final
  ID is reused. Temporarily clearing/replacing its runtime ownership slot also resets that
  lifetime. Metadata/property edits retaining the slot and identity preserve the histories.

Thus editing a meter's role, serial number or transparency metadata does not reset its
operational lifetime. Replacing its runtime history is a separate explicit runtime instruction.
See [element operations](ELEMENT-OPERATIONS.md) for individual static meter edits and removal.

## Runtime when adopting a retained head

`RoamingNetworkHistory.TryAdoptHead` previews by default and requires `adopt: true` to select a
retained descendant against an expected head ID. It uses the same runtime/publication gate.
First-parent fast-forward replays original batches, preserving the existing lifetime rules.
Secondary-parent merge adoption derives the validated target snapshot and captures current local
runtime only where both first-parent branches prove uninterrupted identity/ownership lifetimes.
Removal, recreation, owner changes or temporary nested-slot replacement invalidate that proof.
Objects introduced since the shared ancestor can therefore start with domain defaults even when
the merge retains their receiver-branch static data. Foreign runtime is never imported.

Runtime schedules/measurements/forecasts are copied independently before persistence and head
publication. A failed adoption leaves the old head/network in place. Commit pages retain static
history without selecting a head or changing local runtime. See [replication and lifetime proofs](REPLICATION.md).

## Publication and concurrency

Target resolution uses the network's lock; expected-current-status validation and schedule
mutation share the schedule lock. Direct domain setters use that schedule lock as well.
History enumeration returns a detached copy. These locks do not create a transaction across
entities or make a separate application head store atomic.

`RoamingNetworkHistory` routes scoped runtime delivery and static head publication through its
shared gate, so arriving status instructions are applied to the published network:

```csharp
var source = history.Head;
var commit = history.PrepareCommit(source.Id, batch);
if (!history.TryPublish(source.Id, commit, out var result))
    throw new InvalidOperationException(result.Error);

history.ApplyRuntimeUpdate(receivedRuntimeUpdate);
```

The history's configured verifiers authenticate supplied batch/commit peers. Runtime status
delivery does not change or persist the static commit head. Applications using `ApplyChangeSet`
directly must supply their own publication gate. Writing through an entity reference
retained from an older version bypasses this publication discipline. Coherent runtime exports
and direct measurements/forecasts also require application coordination. Static archive recovery
starts fresh runtime schedules; see [history and atomic heads](HISTORY.md).
The same rule applies to [bootstrap activation](BOOTSTRAP.md): transfer contains only static
history, the returned replica starts fresh schedules/measurements/forecasts, and the application's
existing history/runtime is untouched. Apply local runtime updates after explicitly selecting it.
[Explicit retention](RETENTION.md) differs from recovery: pruning an existing history preserves the
exact current head/network instances and every local runtime value. No runtime reconstruction occurs.
Subsequent lifetime proofs stop at the new opaque snapshot baseline; omitted history cannot establish
earlier births. Digest-checked cold reads return a separate history with fresh runtime.

Runtime notifications follow the existing schedule events. A synchronous notification handler
can throw after the schedule has been changed; runtime application does not provide the static
ChangeSet engine's rollback guarantee. Async notification completion is not awaited. Applications
should isolate failing subscribers and must not treat a notification exception as proof that
no runtime write occurred.
