# ParkingSpaceGroup

A parking space group collects parking spaces of one parking operator, e.g. one level or zone of a garage, and
may name the parking products offered for all of them. It is embedded completely in its
[ParkingOperator](ParkingOperator.md) under `parkingSpaceGroups`, in the order of its identification. Groups may
overlap: a parking space can be a member of several groups.

It may give its members any property of a parking space but its identification, name, description and APDS reference:
e.g. the garage, the dimensions, the vehicle types and the roof of all spaces of a level, given once. The network keeps
them in the group; the parking spaces take them at runtime, as the [effective parking spaces](#properties-of-its-members)
of their parking operator.

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
| HTTP | `GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSGs/{PSG_Id}`; embedded in its parking operator, `GET ~/RNs/{RN_Id}/POs/{PO_Id}`, and listed by `GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSGs` |


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
| `parkingProductIds` | `ParkingProductIds` | `ImmutableArray<ParkingProduct_Id>` | update/remove | [Parking products](ParkingOperator.md#parking-products) of its parking operator offered at its members, besides their own; ordered by identification (ordinal), none twice. |
| `priority` | `Priority` | `Int32?` | update/remove | Which of the groups of a member decides where they give it different values: the higher wins; 0 unless given. An integer, neither a text nor a fraction; see [group priority](README.md#group-priority-and-effective-members). |

The properties of a parking space it gives its members, each written and read as a [parking space](ParkingSpace.md#properties)
writes and reads it, and each updated or removed by a change set. Of `geoLocation`, `parkingGarageId` (a garage of its
parking operator), `physicalReference`, `maxVehicleWeight`, `maxVehicleHeight`, `maxVehicleLength`, `maxVehicleWidth`,
`length`, `width`, `evsePosition`, `direction`, `surface`, `slope`, `timeLimit`, `restrictedToVehicleTypes`,
`driveThrough`, `reservationRequired`, `roofed`, `illuminated`, `refrigerationOutlet`, `dangerousGoodsAllowed` and
`protectedArea` its members take the group's value unless they have their own. Of `parkingSensorIds` (sensors of its
parking operator), `chargingStationIds` (charging stations of the network), `parkingProductIds`, `parkingRestrictions`,
`euVehicleCategories`, `vehicleTypes`, `standards` and `images` they take the group's besides their own; of images of
the same URL their own.

Arrays are written only when not empty; a group may be empty. A change set names the properties by their
JSON keys (`ChangeableParkingSpaceGroupProperties` in
[RoamingNetwork.ParkingChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.ParkingChanges.cs)):

```csharp
RoamingNetworkChange.Add           (group, parkingOperatorId)
RoamingNetworkChange.UpdateProperty(group, "parkingSpaceIds", old, new JArray("PRK-G1-S001", "PRK-G1-S002"))
RoamingNetworkChange.Remove        (groupId)
```


## Properties of its members

A parking space takes the properties its groups give it at runtime: its **effective parking space**, which its
parking operator calculates once per version, `parkingOperator.TryGetEffectiveParkingSpace(spaceId, out var space)`,
`parkingOperator.EffectiveParkingSpaces`, or in the roaming network `TryGetEffectiveParkingSpaceById` and
`EffectiveParkingSpaces`. A single value is the space's own, else the one of its group with the highest `priority`;
an array is its own united with those of all its groups. Groups of the same priority must not give it different values
that decide, nor make it invalid. See [group priority and effective members](README.md#group-priority-and-effective-members).
POI_API serves the effective parking spaces; `?view=stored` the parking spaces as they are given.


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
- `parkingSpaceIds`, `parkingProductIds` and the arrays of the properties of its members: none twice; `priority` an
  integer; vehicle limits and dimensions positive, the time limit not negative.
- Every `parkingSpaceIds` names a parking space, every `parkingProductIds` a product, `parkingGarageId` a garage and
  every `parkingSensorIds` a sensor of its parking operator; every `chargingStationIds` a charging station of the
  network. Every effective parking space must be valid, and groups of the same priority must agree.
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
  "parkingGarageId":   "PRK-G1",
  "roofed":            true,
  "length":            5,
  "width":             2.5,
  "created":           "2026-01-01T00:00:00.000Z",
  "lastChange":        "2026-01-01T00:00:00.000Z"
}
```
