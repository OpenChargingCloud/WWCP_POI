# Addressed nested POI operations

[Repository overview](../README.md) · [ChangeSets](CHANGESETS.md) · [Runtime](RUNTIME.md) · [Signatures](SIGNATURES.md)

All independently owned [graph types](GRAPH.md) support `Add`, `Remove` and `UpdateProperty`
operations. Nested owned values now have explicit operations as well. They address an existing
graph owner and a structured `ElementPath`, rather than array indices or arbitrary JSON pointers.

## Address and operations

`EntityType`/`EntityId` identify the graph owner. Each immutable `POIElementPathSegment` names an
exact schema property and optionally an `ElementId`. Arrays always require the existing element
identity. Singletons can use their owner/property slot; supplying an ID additionally asserts the
current identity. A connector owner still requires its EVSE parent scope.

```csharp
ImmutableArray<POIElementPathSegment> meterPath =
    [new("energyMeters", "grid-meter-1")];

var oldRole = network.DataSnapshot.GetElementPropertyValue(
    InfrastructureEntityType.ChargingStation, "DE*ABC*S1", meterPath, "role");

var change = RoamingNetworkChange.UpdateElementProperty(
    "ChargingStation", "DE*ABC*S1", meterPath,
    "role", oldRole, JsonSerializer.SerializeToElement("pv"));

var batch = network.CreateChangeSet("meter-role-43", DateTimeOffset.UtcNow, [change]);
var next = network.ApplyChangeSet(batch);
```

This example assumes an existing meter with a stored `role`. An absent optional property has no
old value; omit its precondition to set it. Explicit JSON null and absence remain distinct.

| Factory | Meaning | Preconditions |
| --- | --- | --- |
| `AddElement` | Add an identified array element or an absent optional singleton | Target must be absent; no old value |
| `RemoveElement` | Remove one array element or clear an optional singleton | Target must exist; optional complete old value |
| `ReplaceElement` | Replace one existing element/singleton completely | Target must exist; optional complete old value |
| `UpdateElementProperty` | Replace one static property on the selected object | Target must exist; optional previous property value |

`GetElementValue()` returns a detached `JsonElement` for complete element preconditions.
`GetElementPropertyValue()` reads an existing property. Both use the same identity/ownership rules
as application and reject absent targets. A reference string has no editable object properties.

```csharp
var addMeter = RoamingNetworkChange.AddElement(
    "ChargingPool", "DE*ABC*P1", [new("energyMeters", "pv-meter-2")],
    JsonSerializer.SerializeToElement(new { id = "pv-meter-2", role = "pv" }));

var removeMeter = RoamingNetworkChange.RemoveElement(
    "ChargingStation", "DE*ABC*S1", meterPath,
    network.DataSnapshot.GetElementValue(
        InfrastructureEntityType.ChargingStation, "DE*ABC*S1", meterPath));

var assignTariff = RoamingNetworkChange.AddElement(
    "EVSE", "DE*ABC*E1", [new("tariffIds", "DE*ABC*T1")],
    JsonSerializer.SerializeToElement("DE*ABC*T1"));
```

Tariff assignments require an existing tariff in the same operator. Ordered batches can add a
tariff before assigning it, or remove references before deleting it. The reverse reference index
is updated for targeted assignment changes just as for whole-property `tariffIds` replacement.

Group member arrays (`EVSEIds`, `chargingStationIds`, `chargingPoolIds`, `chargingTariffIds`)
and the first three groups' `allowedMemberIds` support addressed Add/Remove/Replace operations.
Parking nodes' `chargingStationIds`/`sensors`, and parking operators' local/invalid space IDs do too.
Active group members must exist under their operator and be admitted. Parking sensors/spaces
must belong to the same parking operator; station references can cross charging operators within
the network. Deletion protection is maintained by `DataSnapshot.References`; see [scopes](GRAPH.md).

## Supported ownership paths

| Property and owner | Kind | Identity |
| --- | --- | --- |
| Pool/station `energyMeters` | Array of meters | Meter `id`, with domain ID equality |
| EVSE/connection-point `energyMeter` | Optional singleton meter | Owner/property; optional meter ID assertion |
| Pool `gridConnectionPoint` | Optional singleton | Owner/property; optional connection-point `id` assertion |
| Connection-point `gridOperator` | Required singleton | Owner/property; optional operator ID assertion |
| Connector `cable` | Optional singleton value | Owner/property; no element ID |
| Operator/pool/station `brands`, EVSE `brand` | Array of expanded brands | Brand `id` |
| Tariff/EVSE-group/station-group/pool-group `brand` | Optional singleton brand | Owner/property; optional brand ID assertion |
| Network/operator/provider/pool/station/EVSE `dataLicenses` | Array of expanded licenses | License `@id` |
| Brand/grid operator `dataLicenses` | Array of expanded licenses | License `@id` |
| Network/operator/provider/pool/station/EVSE `dataLicenseIds` | Array of references | License ID string |
| EVSE/connector `tariffIds` | Array of references | Tariff ID string |
| Group active member IDs and admission IDs | Array of reference strings | Referenced domain ID |
| Parking station/sensor/space IDs | Array of reference strings | Referenced domain ID |
| Group/parking-operator `dataLicenses` | Expanded licenses | License `@id` |
| Connection-point `marketLocationIds`, `meteringLocationIds` | Array of references | Existing location identifier string |

Paths can traverse several supported relations:

```csharp
ImmutableArray<POIElementPathSegment> uplinkMeter =
    [new("gridConnectionPoint", "uplink-1"), new("energyMeter", "grid-meter-1")];

var editSerial = RoamingNetworkChange.UpdateElementProperty(
    "ChargingPool", "DE*ABC*P1", uplinkMeter, "serialNumber", null,
    JsonSerializer.SerializeToElement("SN-2026-17"));

var editPower = RoamingNetworkChange.UpdateElementProperty(
    "ChargingPool", "DE*ABC*P1", [new("gridConnectionPoint", "uplink-1")],
    "contractedImportPower", JsonSerializer.SerializeToElement("250000 W"),
    JsonSerializer.SerializeToElement("300 kW"));
```

Only declared relations are traversable. `customData`, addresses and other arbitrary objects are
not traversal roots. Graph child collections such as `EVSEs` still use graph operations. Expanded
object elements must contain their correct identity and supported fields. Duplicate identities,
wrong scopes, malformed elements and unknown element fields are rejected before publication.

Tariff elements/price components/restrictions and transparency software/certificate values have
no independent IDs in the current model. Their arrays remain explicit whole-property values.
For example, `UpdateElementProperty` can update a selected meter's `transparencySoftware` array
while preserving its other fields. Groups, manufacturers and network grid/parking operators,
including parking children, have independent graph addresses. Their owned child arrays use graph
operations; identified membership/reference arrays use element operations.

## Validation, metadata and runtime

Element operations participate in the same atomic ordered ChangeSet engine, before/after ETag
checks, signatures and domain validation. They copy the owner's changed JSON property and replace
the owner/ancestor entries in the persistent maps. Nested JSON collections are materialized/copied;
this is not a persistent element map or a constant-cost update for arbitrarily large arrays.

Property edits cannot change element IDs, parent/network references, contexts, creation/change
timestamps or derived ETags. Array replacements must retain the addressed element identity.
An unqualified singleton slot can explicitly replace its child with a different identity.

Meter/grid-operator `lastChange` and those of their affected nested ancestors use the fixed batch
timestamp. Graph owner/ancestor timestamps are updated normally. `ReplaceElement` of the same
identified meter/operator preserves its creation timestamp and rejects a supplied different one.
Missing introduced metadata uses deterministic batch defaults. Quantities use explicit SI strings;
equivalent units are normalized for old-value checks and storage. Zero/null constraints remain
those of the domain parser.
Meter roles are trimmed/lowercase in stored edits and role preconditions, matching the domain contract.

All operational runtime fields are rejected, including in complete old values and nested payloads.
Legal transparency status remains static. Existing child histories are copied independently by
owner and identity. Explicit removal/readdition, or temporarily clearing/replacing the runtime
ownership slot in the same batch, starts a new runtime lifetime even if the final ID is reused.
Unrelated children retain their histories. See [runtime retention](RUNTIME.md#runtime-retention-across-static-changes).

## Merge behavior

Touched identity collections are stored in ordinal order of their wire IDs. Independent adds,
removes and edits therefore have deterministic results in both operation orders. Two element
property edits can merge even within one meter when they change different fields and their
preconditions and domain constraints remain valid. Whole-property replacement remains available
and retains its whole-value precondition.

`TryMerge` keeps its two-order check and explicit preparation step. Same-element deletion versus
editing, duplicate additions, incompatible replacements, failed preconditions or broken references
are conflicts. It does not rewrite preconditions or automatically choose a value. An original failed
element operation is reported with its graph owner, `ElementPath`, property and operation index.
Whole-result differences can still be reported at the enclosing owner property.

## JSON and signing

The additional operation contract is System.Text.Json with immutable path records:

```json
{
  "Kind": "UpdateElementProperty",
  "EntityType": "ChargingStation",
  "EntityId": "DE*ABC*S1",
  "ElementPath": [{ "PropertyName": "energyMeters", "ElementId": "grid-meter-1" }],
  "PropertyName": "role",
  "OldValue": "grid",
  "NewValue": "pv"
}
```

Element operations require a nonempty path without null segments. Original graph operations use
an empty path; their JSON can omit it on input. Unknown operation/path fields are rejected.
`AddElement` needs a new value; `RemoveElement` cannot contain one. JSON null cannot add/replace
an element; use removal for optional slots. Property updates can use null where the parser allows it.

**Signing profile change:** built-in signing/verification uses `wwcp-poi-changeset-json-v2`.
Every signed operation includes `ElementPath`, including `[]` on original graph operations, with
the exact ordered property names and supplied IDs. The previous signing profile is unsupported
by built-in verification; existing batches need new signatures for v2. Static POI ETag profiles
are unchanged. Descriptions, metadata and equal peer-signature rules remain part of the profile.
