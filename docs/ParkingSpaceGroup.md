# ParkingSpaceGroup

A parking space group collects parking spaces of one parking operator, e.g. one level or zone of a garage, and
may name the parking products offered for all of them. It is embedded completely in its
[ParkingOperator](ParkingOperator.md) under `parkingSpaceGroups`, in the order of its identification. Groups may
overlap: a parking space can be a member of several groups.

Its parking operator owns it and signs every change to it. Its identification is a free text, compared
case-insensitively, e.g. `PRK-G1-L1`, unique within the roaming network (not only within its operator).

| | |
|---|---|
| Class | `ParkingSpaceGroup` — [source](../WWCP_POI3/RoamingNetwork/ParkingOperator/ParkingSpace/Group/ParkingSpaceGroup.cs) |
| Identification | `ParkingSpaceGroup_Id`, e.g. `PRK-G1-L1` (`NewRandom()` makes a UUIDv7) |
| Admin status | `ParkingSpaceGroupAdminStatusType`, initially `operational` |
| Status | `ParkingSpaceGroupStatusType`, initially `available` |
| Embedded in | [ParkingOperator](ParkingOperator.md) as `parkingSpaceGroups` |
| Owner | its parking operator (signing key) |
| HTTP | — (served within its parking operator, `GET ~/RNs/{RN_Id}/POs/{PO_Id}`) |


## Common properties

See [Common properties](README.md#common-properties). It writes no `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — (set on `Add` unless given) |
| `lastChange` | — (set to the change set's `createdAt`) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `parkingSpaceIds` | `ParkingSpaceIds` | `ImmutableArray<ParkingSpace_Id>` | update/remove | Its members, [parking spaces](ParkingSpace.md) of the same parking operator; ordered by identification (ordinal), none twice. |
| `parkingProductIds` | `ParkingProductIds` | `ImmutableArray<ParkingProduct_Id>` | update/remove | [Parking products](ParkingOperator.md#parking-products) of its parking operator offered here; ordered by identification (ordinal), none twice. |

Both arrays are written only when not empty; a group may be empty. A change set names the properties by their
JSON keys (`ChangeableParkingSpaceGroupProperties` in
[RoamingNetwork.ParkingChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.ParkingChanges.cs)):

```csharp
RoamingNetworkChange.Add           (group, parkingOperatorId)
RoamingNetworkChange.UpdateProperty(group, "parkingSpaceIds", old, new JArray("PRK-G1-S001", "PRK-G1-S002"))
RoamingNetworkChange.Remove        (groupId)
```


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`ParkingSpaceGroup`. Its status is set manually, or calculated by a delegate from the status of its members, see [Calculated status of groups](README.md#calculated-status-of-groups).

| Type | Predefined values | Initial |
|---|---|---|
| `ParkingSpaceGroupAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` since `created` |
| `ParkingSpaceGroupStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `blocked`, `available`, `reserved`, `inUse` | `available` since `created` |

Any other non-empty text parses as well. No energy status. A change of the group or of anything else of its
parking operator keeps its runtime status.

Events: `OnAdminStatusChanged` (`OnParkingSpaceGroupAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnParkingSpaceGroupStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, ParkingSpaceGroup, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory, any non-empty text (trimmed); unique within the roaming network, compared
  case-insensitively.
- `parkingSpaceIds` and `parkingProductIds`: none twice.
- Every `parkingSpaceIds` names a parking space, every `parkingProductIds` a product, of its parking operator.
- Nothing references a group, so it can always be removed.
- `Add` needs its parking operator as the parent, an existing one, and an object whose `@id` is the change's
  `entityId`; the group must not exist yet.


## Example

```json
{
  "@id":               "PRK-G1-L1",
  "name":              { "en": "Level 1" },
  "parkingSpaceIds":   [ "PRK-G1-S001", "PRK-G1-S002" ],
  "parkingProductIds": [ "SHORT-STAY" ],
  "created":           "2026-01-01T00:00:00.000Z",
  "lastChange":        "2026-01-01T00:00:00.000Z"
}
```
