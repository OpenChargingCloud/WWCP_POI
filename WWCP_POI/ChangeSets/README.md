# Versioned infrastructure and change sets

[Repository overview](../../README.md) · [Architecture](../../docs/ARCHITECTURE.md) · [ChangeSet examples](../../docs/CHANGESETS.md) · [JSON contracts](../../docs/JSON.md) · [Signatures and trust](../../docs/SIGNATURES.md)

```csharp
using System.Text.Json;

var before = roamingNetwork.DataSnapshot;
var oldPower = before.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1")
                     .Properties["maxPower"];

var changeSet = new RoamingNetworkChangeSet(
    id: "change-42",
    roamingNetworkId: roamingNetwork.Id.ToString(),
    baseRevision: roamingNetwork.Revision,
    createdAt: DateTimeOffset.UtcNow,
    changes: [RoamingNetworkChange.UpdateProperty(
        "EVSE", "DE*ABC*E1", "maxPower", oldPower,
        JsonSerializer.SerializeToElement(150_000m))]);

var next = roamingNetwork.ApplyChangeSet(changeSet);
// roamingNetwork is still revision 0; next is revision 1.
var document = next.ToJSONSnapshot();
var restored = RoamingNetwork.Parse(document);
```

## Storage and compatibility

`DataSnapshot` captures a mutable legacy network once and becomes its authoritative versioned
data. A snapshot contains immutable entity documents and parent/child keys, backed by persistent
`ImmutableDictionary`/`ImmutableHashSet` collections. Updates replace affected entries and their
ancestors; unrelated entries and unchanged property values are shared. Applying a batch does not
serialize or reparse the entire network. Capturing the initial network and exporting its JSON are
proportional to the data size.

For large data sets, read `DataSnapshot.Entities`, `GetEntity()` and `GetEntityJSON()` directly.
`WriteTo(Utf8JsonWriter)` streams the complete snapshot without creating an intermediate JObject
hierarchy. `GetEntityJSON()` returns a fresh, editable JSON document without exposing mutable storage.

The old POI classes are still mutable **compatibility projections**, not immutable storage. On a
newly applied network, accessing the existing operators/pools/stations/EVSE collections materializes
the complete projection lazily, with fresh objects and parent pointers into that version. Editing
these projections does not edit `DataSnapshot`, `ToJSONSnapshot()` or any later version. After
capturing a snapshot, persist changes through change sets. `ToJSON()` remains the legacy projection
serializer; use `ToJSONSnapshot()` for versioned persistence. A persisted snapshot carries its
`revision` and `appliedChangeSetId`; reloading it preserves both.

## Operations

Entity type names and property names are the case-sensitive names in the public enum and POI JSON
contract. Supported entities are RoamingNetwork, ChargingStationOperator, EMobilityProvider,
ChargingPool, ChargingStation, EVSE, ChargingConnector and ChargingTariff. Nested brands, licenses, cables and
energy meters can be replaced as properties of their owning entity.

- **Add:** pass an entity JSON object with a matching `@id`. A station, pool, EVSE, connector or tariff also
  needs `ParentEntityType` and `ParentEntityId`. Operators/providers default to the current network
  parent. Nested children may be included, and are validated as part of the operation. Use expanded
  embedded POI documents, or `GetEntityJSON()` from another snapshot, rather than unresolved IDs.
- **Remove:** removes the entity and its descendants. An optional `OldValue` checks its complete
  document, including children. A missing entity is a conflict. Removing the network root is forbidden.
- **UpdateProperty:** replaces a top-level JSON property; the optional `OldValue` is checked against
  the value produced by preceding operations in the same batch. IDs, ancestry, child arrays,
  creation/change timestamps and revision fields cannot be replaced as properties. Moving a subtree
  is an explicit remove followed by add beneath its new parent.

Connector IDs are local to an EVSE. All connector operations and lookups require an explicit EVSE
parent/scope, so connector `1` on one EVSE cannot accidentally target connector `1` on another.
Snapshot key equality follows the domain's ID equality, including equivalent separator/case forms.

Omitting `OldValue` skips the precondition. A present JSON null expects a present null property;
it does not match an absent property. A new JSON null clears a nullable property, and remains
explicitly present in snapshot storage. Status/admin-status changes require an object containing
`value` and `timestamp`; their timestamps are normalized to UTC. The compatibility status schedule
treats future timestamps as scheduled values rather than current values.

Each successful batch increments the revision once, including an empty batch. Entity and ancestor
change timestamps use the batch's `CreatedAt`; existing creation timestamps remain unchanged.
New entities receive default metadata where omitted. Revision/network mismatches, invalid fields,
duplicates, wrong parents and failed preconditions abort the entire batch. `ApplyChangeSet()` throws
`RoamingNetworkChangeSetException` with the operation index; `TryApplyChangeSet()` returns an error
without a partial result. Independent branches may be built concurrently from the same snapshot.

## Signatures and boundaries

A batch with a signature envelope requires a caller-provided `VerifySignature` callback and is
rejected unless verification succeeds. No signature algorithm, key management, canonical JSON,
commit hash, merge algorithm or full revision history is implemented here. The numeric base revision
is optimistic concurrency metadata, not a cryptographic identity of a branch. `AppliedChangeSetId`
records the latest batch, not an audit log.

The snapshot retains the existing POI snapshot contract: current timestamped statuses, entity
timestamps and custom data, but no status history or properties absent from the POI serializers.

## Charging tariffs

Operators own `chargingTariffs`; EVSEs and connectors reference them through `tariffIds`. Tariffs
are independent immutable entity nodes, so changing a price shares the entire station subtree.
`elements`, `currency`, `brand`, `URI` and `energy_mix` can be updated as tariff properties.
Price components and restrictions are nested tariff values; replace `elements` to edit them.

`TariffReferences` is a persistent reverse index of tariff keys to EVSE/connector keys. It is built
once during capture and updated only for affected assignments/subtrees. Assignments must refer to
an existing tariff owned by the same operator. A referenced tariff cannot be removed: first clear
or replace its assignments, then remove it in the same batch. Removing an operator removes both
its infrastructure assignments and its tariffs atomically. Operations observe preceding operations
in order; add a tariff before assigning it.

Legacy `GetChargingTariffs()`/`GetChargingTariffIds()` now return registered tariffs without filters,
or assigned tariffs for the supplied pool/station/EVSE/connector. Connector queries require an EVSE
scope and include the EVSE's direct assignments. Provider-specific tariff agreements are not modeled;
passing an EMobilityProvider filter raises `NotSupportedException`.

Tariff JSON retains the existing field names. Prices and energy/power bounds are now decimal JSON
numbers without cent rounding; old invariant decimal strings remain readable. Date restrictions
use `startDate`/`endDate` as ISO timestamps; duration restrictions use `minDuration`/`maxDuration`
in seconds with TimeSpan tick precision. Billing increments require positive whole seconds.
Energy mixes include their `energySources` and `environmentalImpacts` arrays. Missing arrays in old
JSON mean unknown composition; the original serializer did not store those values.

Use `RoamingNetwork.Parse(string)` or `ChargingTariff.Parse(string, operator)` to load numeric prices
without converting them to binary floating point first. When supplying a JObject, load it with
`JsonTextReader.FloatParseHandling = FloatParseHandling.Decimal`. Precision already lost in a
JObject created with the default double reader cannot be recovered by a parser.

## Transparency software and certificate status

`TransparencySoftware` and `TransparencySoftwareStatus` are nested values under
`EVSE.energyMeter.transparencySoftware`. They have no independent entity IDs. Change them by
replacing the owning EVSE's `energyMeter` property with an optional `OldValue` precondition:

```csharp
var oldMeter = roamingNetwork.DataSnapshot
    .GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["energyMeter"];
var meter = System.Text.Json.Nodes.JsonNode.Parse(oldMeter.GetRawText())!;
meter["transparencySoftware"]![0]!["legalStatus"] = "verified";
meter["transparencySoftware"]![0]!["certificate"] = "certificate-2";

var change = RoamingNetworkChange.UpdateProperty(
    "EVSE", "DE*ABC*E1", "energyMeter", oldMeter,
    JsonSerializer.SerializeToElement(meter));
```

The replacement is validated through the energy meter and software/status parsers before commit.
Invalid software, non-string certificate fields and reversed validity intervals abort the batch. Other
EVSEs and their data are shared unchanged. The meter's own `lastChange` is supplied in the property
document; the change set updates the owning EVSE and its ancestor timestamps automatically.

The current software JSON uses `openSourceLicense` as a complete license object (`@id`,
`description`, `URLs`). The parser also accepts `id`, legacy `open_source_license` strings and
license strings under `openSourceLicense`. Known legacy license IDs resolve to the predefined
license; unknown strings retain their ID and available description text. Old strings cannot restore
original URLs or multilingual metadata omitted by the old serializer.

`TransparencySoftwareStatus.NotBefore`/`NotAfter` and their constructor parameters are now nullable
`DateTimeOffset` values normalized to UTC. JSON retains all seven fractional second digits.
Timezone-free legacy timestamps are read as UTC. Legal status values remain extensible; parsing
checks their representation, not certificate authenticity or legal approval. Software links and
license URLs must be absolute. Software/status comparisons cover all serialized fields, and nested
license metadata and the meter's software collection are copied defensively.

Energy meter JSON now retains creation/change timestamps, current timestamped statuses,
data source and custom data in both regular output and network snapshots.

## Source organization

The public change set, operation, signature, entity key, entity type and exception each live in
separate files. The snapshot implementation is grouped by responsibility:

| File | Responsibility |
| --- | --- |
| `RoamingNetworkDataSnapshot.cs` | Immutable data, lookup and initial capture |
| `RoamingNetworkDataSnapshot.Changes.cs` | Batch validation and ordered add/remove/property operations |
| `RoamingNetworkDataSnapshot.Import.cs` | Hierarchy import, metadata normalization and subtree traversal |
| `RoamingNetworkDataSnapshot.Validation.cs` | Domain validation through small ancestor projections |
| `RoamingNetworkDataSnapshot.TariffReferences.cs` | Incremental reverse tariff index |
| `RoamingNetworkDataSnapshot.Json.cs` | Entity JSON and direct nested export |
| `RoamingNetwork.CopyOnWrite.cs` | Roaming network API and lazy legacy hierarchy projection |

All partial snapshot files operate on the same immutable storage. This source organization does
not introduce extra copies of entities or JSON trees.
