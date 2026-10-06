# Using ChangeSets

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [JSON](JSON.md) · [Signatures](SIGNATURES.md)

## Batch contract

A `RoamingNetworkChangeSet` is an immutable, ordered batch:

| Field | Meaning |
| --- | --- |
| `Id` | Application-supplied identifier for this batch |
| `RoamingNetworkId` | Target network's domain identifier |
| `BaseRevision` | Exact revision of the source snapshot |
| `CreatedAt` | Timestamp used for changed entities and their ancestors |
| `Changes` | Immutable operation array; an initialized empty array is valid |
| `Signature` | Optional `Algorithm`, `KeyId`, `Value` envelope |

An operation specifies `Kind`, `EntityType`, `EntityId` and operation-specific values.
Entity type names match `InfrastructureEntityType` exactly, including case. Property names
are the JSON contract's names, not necessarily the corresponding C# property names.

The constructors check operation shape and clone JSON payloads. Entity existence, valid domain
IDs, ownership and business data are checked when the batch is applied.

## 1. Read the authoritative source

The examples below assume the network from the [quick start](../README.md#quick-start).
Each operation example is an independent batch against that network.

```csharp
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;

var snapshot = network.DataSnapshot;
var evse = snapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1");
var oldPower = evse.Properties["maxPower"];
```

Capture the source snapshot once when preparing a batch. Derive its old values and
`BaseRevision` from that same snapshot.

## 2. Update a property

```csharp
var update = RoamingNetworkChange.UpdateProperty(
    entityType: "EVSE",
    entityId: "DE*ABC*E1",
    propertyName: "maxPower",
    oldValue: oldPower,
    newValue: JsonSerializer.SerializeToElement(150_000m));

var batch = new RoamingNetworkChangeSet(
    id: "power-update-1",
    roamingNetworkId: network.Id.ToString(),
    baseRevision: snapshot.Revision,
    createdAt: DateTimeOffset.UtcNow,
    changes: [update]);

var next = network.ApplyChangeSet(batch);
```

`OldValue` is optional. Passing C# `null` skips its precondition. When supplied, the applier
checks the currently stored JSON value using `JsonElement.DeepEquals`; status values are first
normalized by the status-property reader.

`UpdateProperty` replaces a complete top-level property. It is not a JSON Pointer/path operation.
For example, update a tariff's `elements` array to change one price component, or replace an EVSE's
`energyMeter` object to change its transparency-software status.

### Missing values and explicit null

| Input | Meaning |
| --- | --- |
| `oldValue: null` / omitted `OldValue` | No old-value check |
| `oldValue: JsonSerializer.SerializeToElement<object?>(null)` | Expect an existing property whose JSON value is null |
| Omitted `NewValue` | Invalid for `Add` / `UpdateProperty` |
| `newValue: JsonSerializer.SerializeToElement<object?>(null)` | Replace with explicit JSON null, if the property accepts null |
| Empty JSON array | Replace an array-valued property with an empty collection |

An absent property does not match an expected explicit JSON null. Property updates do not delete
the property key. Use explicit null only for nullable fields; mandatory fields and collection
parsers may reject it.

## 3. Add a nested station

```csharp
var stationDocument = JsonSerializer.Deserialize<JsonElement>("""
    {
      "@id": "DE*ABC*S2",
      "name": { "en": "New station" },
      "EVSEs": [
        {
          "@id": "DE*ABC*E2",
          "currentType": [ "DC" ],
          "maxPower": 200000,
          "socketOutlets": [ { "@id": "1", "type": "CCS" } ]
        }
      ]
    }
    """);

var add = RoamingNetworkChange.Add(
    entityType: "ChargingStation",
    entityId: "DE*ABC*S2",
    entity: stationDocument,
    parentEntityType: "ChargingPool",
    parentEntityId: "DE*ABC*P1");

var addition = new RoamingNetworkChangeSet(
    "station-add-1", network.Id.ToString(), snapshot.Revision,
    DateTimeOffset.UtcNow, [add]);

var extended = network.ApplyChangeSet(addition);
```

The supplied `@id` must match `EntityId` according to its domain identity. The payload is an
embedded, expanded entity document. Nested descendants are imported and validated in order.
Unresolved child-ID references are not resolved by the ChangeSet applier.

Operators and providers default to the source network as their parent. Pools, stations, EVSEs,
connectors and tariffs require explicit parent type/ID when added. The root network cannot be added.

## 4. Remove a subtree

```csharp
var remove = RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S1");

var removal = new RoamingNetworkChangeSet(
    "station-remove-1", network.Id.ToString(), snapshot.Revision,
    DateTimeOffset.UtcNow, [remove]);

var reduced = network.ApplyChangeSet(removal);
```

Removal cascades to all descendants. It fails if the target does not exist, or if removing a tariff
would leave consumers outside the removed subtree. The root network cannot be removed.

To protect against changes anywhere inside the subtree, supply its complete expected document:

```csharp
var previousStation = snapshot.GetEntityJSON(
    InfrastructureEntityType.ChargingStation, "DE*ABC*S1");

var checkedRemoval = RoamingNetworkChange.Remove(
    "ChargingStation", "DE*ABC*S1",
    JsonSerializer.Deserialize<JsonElement>(
        previousStation.ToString(Newtonsoft.Json.Formatting.None)));
```

Moving a subtree requires explicit remove/add operations and IDs that remain valid for the new
ownership. If domain IDs encode operator ownership, changing operators can also require new IDs.

## 5. Scoped connector operations

Every connector operation requires its EVSE parent because connector IDs are local:

```csharp
var connectorUpdate = RoamingNetworkChange.UpdateProperty(
    entityType: "ChargingConnector",
    entityId: "1",
    propertyName: "lockable",
    oldValue: null,
    newValue: JsonSerializer.SerializeToElement(true),
    parentEntityType: "EVSE",
    parentEntityId: "DE*ABC*E1");

var connector = snapshot.GetEntity(
    InfrastructureEntityType.ChargingConnector, "1", parentId: "DE*ABC*E1");
```

For other existing node types, the parent can be omitted on update/removal. If supplied,
it must match the stored parent.

## 6. Status updates

```csharp
var statusValue = JsonSerializer.SerializeToElement(new
{
    value = "charging",
    timestamp = DateTimeOffset.UtcNow.ToString("O")
});

var statusUpdate = RoamingNetworkChange.UpdateProperty(
    "EVSE", "DE*ABC*E1", "status",
    oldValue: evse.Properties["status"],
    newValue: statusValue);
```

Use the status vocabulary accepted by the target entity's parser. `status` and `adminStatus`
require an object containing `value` and `timestamp`. The status timestamp describes when the
status takes effect; the batch's `CreatedAt` describes the change's entity metadata.

## Ordering, conflicts and errors

Operations observe preceding operations in the same batch. This makes a two-step property update,
a tariff-add followed by assignment, or assignment clearing followed by tariff removal possible.

Every successful batch creates exactly one new revision. Applying a batch against another revision
fails before its operations are applied. A failed operation rejects the complete batch; the source
snapshot and all previously published versions remain unchanged.

```csharp
if (network.TryApplyChangeSet(batch, out var result, out var error))
{
    // Publish result as the application's new version.
}
else
{
    Console.WriteLine(error);
}
```

Use `ApplyChangeSet()` and catch `RoamingNetworkChangeSetException` when structured error details
are needed. `ChangeSetId` identifies the rejected batch; `OperationIndex` identifies the zero-based
operation, or is null for target/revision/signature failures.

The library does not keep a global current head, deduplicate ChangeSet IDs or automatically retry
stale batches. Replaying the same batch on the same immutable source can derive another successor.
Replaying it on its successor normally fails the base-revision check.

Applications should coordinate their current-head update, for example within a lock or a database
transaction, and decide how to rebuild or reject stale operations. Two branches can have equal
revision numbers; they are not interchangeable solely because those numbers match.

## Editable fields and persistence

The complete allowlist is in
[`InfrastructureChangeSchema`](../WWCP_POI/ChangeSets/InfrastructureChangeSchema.cs).
Common editable fields include name, description, source, custom data and current statuses.
Additional fields depend on the entity type.

IDs, parent links, child collections, `created`, `lastChange`, `revision` and
`appliedChangeSetId` are managed through operations and cannot be changed as top-level properties.
Nested owner properties are validated as complete replacements.


### Current editable-property reference

All node types except connectors accept the shared fields:
`name`, `description`, `dataSource`, `customData`, `status`, `adminStatus`.
The following fields are additional; connectors use only their listed fields.

| Entity | Additional editable JSON properties |
| --- | --- |
| RoamingNetwork | `dataLicenses`, `dataLicenseIds` |
| ChargingStationOperator | `address`, `logos`, `homepage`, `hotline`, `brands`, `dataLicenses`, `dataLicenseIds` |
| EMobilityProvider | `address`, `logos`, `homepage`, `hotline`, `dataLicenses`, `dataLicenseIds` |
| ChargingPool | `address`, `geoLocation`, `locationType`, `accessibility`, `authenticationModes`, `hotlinePhoneNumber`, `openingTimes`, `brands`, `dataLicenses`, `dataLicenseIds` |
| ChargingStation | `address`, `geoLocation`, `authenticationModes`, `hotlinePhoneNumber`, `openingTimes`, `isFreeOfCharge`, `brands`, `dataLicenses`, `dataLicenseIds` |
| EVSE | `physicalReference`, `geoLocation`, `brand`, `isFreeOfCharge`, `chargingModes`, `currentType`, `averageVoltage`, `maxCurrent`, `maxPower`, `maxCapacity`, `energyMeter`, `dataLicenses`, `dataLicenseIds`, `tariffIds` |
| ChargingTariff | `elements`, `currency`, `brand`, `URI`, `energy_mix` |
| ChargingConnector | `type`, `cable`, `lockable`, `tariffIds`, `termsAndConditions` |

The allowlist controls which fields may be addressed; the entity's parser still controls accepted
values. Being listed does not make a mandatory field nullable or permit an invalid nested value.

Persist the version with `ToJSONSnapshot()` or `DataSnapshot.WriteTo()`. Persist ChangeSets
separately if the application needs a history. The snapshot stores only the latest
`AppliedChangeSetId`; it does not retain the batches that created earlier versions.

See [JSON](JSON.md) for ChangeSet serialization, [signatures](SIGNATURES.md) for verified batches
and [domain details](../WWCP_POI/ChangeSets/README.md) for tariffs and transparency software.
