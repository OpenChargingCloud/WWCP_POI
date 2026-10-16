# ParkingSpace

A parking space is one place to park a vehicle, e.g. the bay in front of a charger. It is embedded completely
in its [ParkingOperator](ParkingOperator.md) under `parkingSpaces`, in the order of its identification. It may
be in a [ParkingGarage](ParkingGarage.md) and be watched by [ParkingSensors](ParkingSensor.md) of the same
operator, which it names; the garage does not own it. Charging stations name the spaces they charge at by
`parkingSpaceIds` (see [ChargingStation](ChargingStation.md)), parking space groups by `parkingSpaceIds`
(see [ParkingSpaceGroup](ParkingSpaceGroup.md)).

Its parking operator owns it and signs every change to it. Its identification is a free text, compared
case-insensitively, e.g. `PRK-G1-S001`, unique within the roaming network (not only within its operator).

Where no parking operator defines the parking spaces of a charging station, its
[ChargingStationOperator](ChargingStationOperator.md) can: it embeds them under `parkingSpaces` as well, owns them
and signs every change to them. Such a space is in no parking garage, and has no parking sensors or parking products,
which are those of parking operators; identifications are unique across both. A parking operator takes one over with
one change set, signed by both: `Remove(spaceId)` at the charging station operator and `Add(space, parkingOperatorId)`
with all it says; the charging stations naming it go on naming it.

| | |
|---|---|
| Class | `ParkingSpace` — [source](../WWCP_POI3/RoamingNetwork/ParkingOperator/ParkingSpace/ParkingSpace.cs) |
| Identification | `ParkingSpace_Id`, e.g. `PRK-G1-S001` (`NewRandom()` makes a UUIDv7) |
| Admin status | `ParkingSpaceAdminStatusType`, initially `operational` |
| Status | `ParkingSpaceStatusType`, initially `available` |
| Embedded in | [ParkingOperator](ParkingOperator.md) as `parkingSpaces`, or [ChargingStationOperator](ChargingStationOperator.md) as `parkingSpaces` where no parking operator defines it |
| Owner | the operator embedding it (signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSs/{PS_Id}`; embedded in its parking operator, `GET ~/RNs/{RN_Id}/POs/{PO_Id}`, and listed by `GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSs`; one of a charging station operator embedded in it, `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}`. Either below the charging stations naming it, `GET …/CSs/{CS_Id}/PSs[/{PS_Id}]` |


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
| `parkingRestrictions` | `ParkingRestrictions` | `ImmutableArray<ParkingRestrictionGroup>` | update/remove | Who may park there, as for a [charging pool](ChargingPool.md): `EMPLOYEES`, `EV_ONLY`, `PLUGGED`, `DISABLED` (persons with disabilities), `CUSTOMERS`, `TAXI`, `TENANTS`; ordinal order, each once. |
| `euVehicleCategories` | `EUVehicleCategories` | `ImmutableArray<EUVehicleCategory>` | update/remove | The EU vehicle categories it is for, as for a [charging station](ChargingStation.md), e.g. `M1`; ordinal order, each once. |
| `physicalReference` | `PhysicalReference` | `String?` | update/remove | Its reference visible on site, e.g. a number painted on the ground. |
| `vehicleTypes` | `VehicleTypes` | `ImmutableArray<VehicleType>` | update/remove | The vehicles it is meant for, as for a [charging station](ChargingStation.md), e.g. `VAN`; ordinal order, each once. |
| `restrictedToVehicleTypes` | `RestrictedToVehicleTypes` | `Boolean?` | update/remove | Whether only its `vehicleTypes` may park there, if known. |
| `maxVehicleWeight` | `MaxVehicleWeight` | `Kilogram?` | update/remove | The maximum weight of a vehicle parking there, in kg, positive. |
| `maxVehicleHeight` / `maxVehicleLength` / `maxVehicleWidth` | `MaxVehicle…` | `Meter?` | update/remove | The maximum measures of a vehicle parking there, in m, positive. |
| `length` / `width` | `Length` / `Width` | `Meter?` | update/remove | Its own measures, in m, positive. |
| `evsePosition` | `EVSEPosition` | `EVSEPosition?` | update/remove | Where the charging station is, seen from a vehicle parked there: `LEFT`, `RIGHT`, `CENTER`. |
| `direction` | `Direction` | `ParkingDirection?` | update/remove | How it lies to the road or aisle: `PARALLEL`, `PERPENDICULAR`, `ANGLE`. |
| `surface` | `Surface` | `ParkingSurface?` | update/remove | `ASPHALT`, `CONCRETE`, `TILES`, `GRAVEL`, `GRASS`, `SOIL`. |
| `slope` | `Slope` | `ParkingSlope?` | update/remove | `FLAT`, `SLOPED`, `STEEPLY_SLOPED`. |
| `timeLimit` | `TimeLimit` | `TimeSpan?` | update/remove | How long a vehicle may park there at most, in s (a decimal), not negative. |
| `driveThrough` / `reservationRequired` / `roofed` / `illuminated` / `refrigerationOutlet` / `dangerousGoodsAllowed` / `protectedArea` | `DriveThrough` … `ProtectedArea` | `Boolean?` | update/remove | Whether one can drive through it, it must be reserved, is roofed, lit at night, has an outlet for refrigerated trailers, takes vehicles with dangerous goods, is in an area protected against theft and vandalism; `false` is written, unknown is not. |
| `apdsReference` | `APDSReference` | `String?` | update/remove | Its reference in an automated parking data system, e.g. of a truck parking. |
| `standards` | `Standards` | `ImmutableArray<Standard>` | update/remove | The standards it conforms to, e.g. `PAS 1899:2022`; ordinal order, each once. |
| `images` | `Images` | `Image[]` | update/remove | Images of it, each URL once, in their given order. |

Enumerations (`evsePosition`, `direction`, `surface`, `slope`) are read in upper case only; these properties are those
of an OCPI 2.3 `Parking`.

The arrays are written only when not empty. A change set names the properties by their JSON keys
(`ChangeableParkingSpaceProperties` in
[RoamingNetwork.ParkingChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.ParkingChanges.cs)):

```csharp
RoamingNetworkChange.Add           (space, parkingOperatorId)
RoamingNetworkChange.Add           (space, chargingStationOperatorId)    // where no parking operator defines it
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
| `ParkingSpaceAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` since `created` |
| `ParkingSpaceStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `blocked`, `available`, `reserved`, `inUse` | `available` since `created` |

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
