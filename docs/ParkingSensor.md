# ParkingSensor

A parking sensor detects whether parking spaces are occupied, e.g. a ground sensor or a camera. It is embedded
completely in its [ParkingOperator](ParkingOperator.md) under `parkingSensors`, in the order of its
identification. It does not name the spaces it watches: [ParkingSpaces](ParkingSpace.md) of the same operator
name it by `parkingSensorIds`.

Its parking operator owns it and signs every change to it. Its identification is a free text, compared
case-insensitively, e.g. `PRK-SEN-001`, unique within the roaming network (not only within its operator).

| | |
|---|---|
| Class | `ParkingSensor` — [source](../WWCP_POI3/RoamingNetwork/ParkingOperator/ParkingSensor/ParkingSensor.cs) |
| Identification | `ParkingSensor_Id`, e.g. `PRK-SEN-001` (`NewRandom()` makes a UUIDv7) |
| Admin status | `ParkingSensorAdminStatusType`, initially `operational` |
| Status | `ParkingSensorStatusType`, initially `available` |
| Embedded in | [ParkingOperator](ParkingOperator.md) as `parkingSensors` |
| Owner | its parking operator (signing key) |
| HTTP | — (served within its parking operator, `GET ~/RNs/{RN_Id}/POs/{PO_Id}`, and listed by `GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSNs`) |


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
| `chargingStationIds` | `ChargingStationIds` | `ImmutableArray<ChargingStation_Id>` | update/remove | Charging stations it watches, of any operator of the network; ordered by identification (ordinal), none twice; each must exist. Written only when not empty. |

A change set names the properties by their JSON keys (`ChangeableParkingSensorProperties` in
[RoamingNetwork.ParkingChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.ParkingChanges.cs)):

```csharp
RoamingNetworkChange.Add           (sensor, parkingOperatorId)
RoamingNetworkChange.UpdateProperty(sensor, "geoLocation", null, new JObject(new JProperty("lat", 50.92712), new JProperty("lng", 11.58931)))
RoamingNetworkChange.Remove        (sensorId)
```


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`ParkingSensor`.

| Type | Predefined values | Initial |
|---|---|---|
| `ParkingSensorAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` since `created` |
| `ParkingSensorStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `offline`, `available`, `blocked`, `error` | `available` since `created` |

Any other non-empty text parses as well. No energy status. A change of the sensor or of anything else of its
parking operator keeps its runtime status.

Events: `OnAdminStatusChanged` (`OnParkingSensorAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnParkingSensorStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, ParkingSensor, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory, any non-empty text (trimmed); unique within the roaming network, compared
  case-insensitively.
- `chargingStationIds`: none twice, each a charging station of the network.
- It can not be removed while a parking space of its operator names it.
- `Add` needs its parking operator as the parent, an existing one, and an object whose `@id` is the change's
  `entityId`; the sensor must not exist yet.


## Example

```json
{
  "@id":                "PRK-SEN-001",
  "name":               { "en": "Ground sensor bay 1" },
  "geoLocation":        { "lat": 50.92712, "lng": 11.58931 },
  "chargingStationIds": [ "DE*GEF*S1" ],
  "created":            "2026-01-01T00:00:00.000Z",
  "lastChange":         "2026-01-01T00:00:00.000Z"
}
```
