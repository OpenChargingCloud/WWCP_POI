# ParkingSpace

A parking space is one place to park a vehicle, e.g. the bay in front of a charger. It is embedded completely
in its [ParkingOperator](ParkingOperator.md) under `parkingSpaces`, in the order of its identification. It may
be in a [ParkingGarage](ParkingGarage.md) and be watched by [ParkingSensors](ParkingSensor.md) of the same
operator, which it names; the garage does not own it. Charging stations name the spaces they charge at by
`parkingSpaceIds` (see [ChargingStation](ChargingStation.md)), parking space groups by `parkingSpaceIds`
(see [ParkingSpaceGroup](ParkingSpaceGroup.md)).

Its parking operator owns it and signs every change to it. Its identification is a free text, compared
case-insensitively, e.g. `PRK-G1-S001`, unique within the roaming network (not only within its operator).

| | |
|---|---|
| Class | `ParkingSpace` — [source](../WWCP_POI3/RoamingNetwork/ParkingOperator/ParkingSpace/ParkingSpace.cs) |
| Identification | `ParkingSpace_Id`, e.g. `PRK-G1-S001` (`NewRandom()` makes a UUIDv7) |
| Admin status | `ParkingSpaceAdminStatusType`, initially `operational` |
| Status | `ParkingSpaceStatusType`, initially `available` |
| Embedded in | [ParkingOperator](ParkingOperator.md) as `parkingSpaces` |
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
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | Location, `{lat, lng, alt?}`. |
| `parkingGarageId` | `ParkingGarageId` | `ParkingGarage_Id?` | update/remove | The [parking garage](ParkingGarage.md) it is in, one of the same parking operator. |
| `parkingSensorIds` | `ParkingSensorIds` | `ImmutableArray<ParkingSensor_Id>` | update/remove | The [parking sensors](ParkingSensor.md) watching it, of the same parking operator; ordered by identification (ordinal), none twice. |
| `chargingStationIds` | `ChargingStationIds` | `ImmutableArray<ChargingStation_Id>` | update/remove | Charging stations reachable from it, of any operator of the network; ordered by identification (ordinal), none twice; each must exist. |
| `parkingProductIds` | `ParkingProductIds` | `ImmutableArray<ParkingProduct_Id>` | update/remove | [Parking products](ParkingOperator.md#parking-products) of its parking operator offered here; ordered by identification (ordinal), none twice. |

The arrays are written only when not empty. A change set names the properties by their JSON keys
(`ChangeableParkingSpaceProperties` in
[RoamingNetwork.ParkingChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.ParkingChanges.cs)):

```csharp
RoamingNetworkChange.Add           (space, parkingOperatorId)
RoamingNetworkChange.UpdateProperty(space, "parkingSensorIds", null, new JArray("PRK-SEN-001"))
RoamingNetworkChange.Remove        (spaceId)
```

Its `chargingStationIds` and the `parkingSpaceIds` of charging stations are independent references; neither
has to mirror the other.


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`ParkingSpace`.

| Type | Predefined values | Initial |
|---|---|---|
| `ParkingSpaceAdminStatusType` | `unknown`, `unspecified`, `outOfService`, `operational`, `internalUse` | `operational` since `created` |
| `ParkingSpaceStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `blocked`, `available`, `reserved`, `inUse`, `other` | `available` since `created` |

Any other non-empty text parses as well. No energy status. A change of the space or of anything else of its
parking operator keeps its runtime status.

Events: `OnAdminStatusChanged` (`OnParkingSpaceAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnParkingSpaceStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, ParkingSpace, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory, any non-empty text (trimmed); unique within the roaming network, compared
  case-insensitively.
- `parkingSensorIds`, `chargingStationIds` and `parkingProductIds`: none twice.
- `parkingGarageId` and every `parkingSensorIds` name a garage or sensor of its parking operator, every
  `parkingProductIds` a product of its parking operator, every `chargingStationIds` a charging station of the
  network.
- It can not be removed while a parking space group of its operator or a charging station names it.
- `Add` needs its parking operator as the parent, an existing one, and an object whose `@id` is the change's
  `entityId`; the space must not exist yet.


## Example

```json
{
  "@id":                "PRK-G1-S001",
  "name":               { "en": "Bay 1, charging only" },
  "geoLocation":        { "lat": 50.92712, "lng": 11.58931 },
  "parkingGarageId":    "PRK-G1",
  "parkingSensorIds":   [ "PRK-SEN-001" ],
  "chargingStationIds": [ "DE*GEF*S1" ],
  "parkingProductIds":  [ "SHORT-STAY" ],
  "created":            "2026-01-01T00:00:00.000Z",
  "lastChange":         "2026-01-01T00:00:00.000Z"
}
```
