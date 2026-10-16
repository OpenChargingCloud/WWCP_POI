# ParkingGarage

A parking garage is a car park or parking lot, with an address, a location, opening times and images, and
the parking products offered there. It is embedded completely in its [ParkingOperator](ParkingOperator.md)
under `parkingGarages`, in the order of its identification. It does not embed its parking spaces: they belong
to the parking operator and name the garage by `parkingGarageId`.

Its parking operator owns it and signs every change to it. Its identification is a free text, compared
case-insensitively, e.g. `PRK-G1`, unique within the roaming network (not only within its operator).

| | |
|---|---|
| Class | `ParkingGarage` — [source](../WWCP_POI3/RoamingNetwork/ParkingOperator/ParkingGarage/ParkingGarage.cs) |
| Identification | `ParkingGarage_Id`, e.g. `PRK-G1` (`NewRandom()` makes a UUIDv7) |
| Admin status | `ParkingGarageAdminStatusType`, initially `operational` |
| Status | `ParkingGarageStatusType`, initially `available` |
| Embedded in | [ParkingOperator](ParkingOperator.md) as `parkingGarages` |
| Owner | its parking operator (signing key) |
| HTTP | — (served within its parking operator, `GET ~/RNs/{RN_Id}/POs/{PO_Id}`, and listed by `GET ~/RNs/{RN_Id}/POs/{PO_Id}/PGs`) |


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
| `address` | `Address` | `Address?` | update/remove | Address, as Styx writes it embedded (`street`, `houseNumber`, `postalCode`, `city`, `country` as ISO 3166 alpha-3, …). |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | Location, `{lat, lng, alt?}`. |
| `chargingStationIds` | `ChargingStationIds` | `ImmutableArray<ChargingStation_Id>` | update/remove | Charging stations within, of any operator of the network; ordered by identification (ordinal), none twice; each must exist. |
| `openingTimes` | `OpeningTimes` | `ImmutableOpeningTimes?` | update/remove | When it is open; always, unless given. See [Opening times](#opening-times). |
| `images` | `Images` | `ImmutableArray<Image>` | update/remove | Images, in their given order; a URL only once. See [Images](#images). |
| `parkingProductIds` | `ParkingProductIds` | `ImmutableArray<ParkingProduct_Id>` | update/remove | [Parking products](ParkingOperator.md#parking-products) of its parking operator offered here; ordered by identification (ordinal), none twice; each must be one of the operator. |

The arrays are written only when not empty. A change set names the properties by their JSON keys
(`ChangeableParkingGarageProperties` in
[RoamingNetwork.ParkingChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.ParkingChanges.cs)):

```csharp
RoamingNetworkChange.Add           (garage, parkingOperatorId)
RoamingNetworkChange.UpdateProperty(garage, "chargingStationIds", null, new JArray("DE*GEF*S1", "DE*GEF*S2"))
RoamingNetworkChange.Remove        (garageId)
```

The parking spaces in it are those of its parking operator whose `parkingGarageId` names it; there is no
property for them here.


## Opening times

`openingTimes` as Styx's `OpeningTimes` write them, as for [ChargingPool](ChargingPool.md):

| JSON key | Type | Description |
|---|---|---|
| `24/7` | `Boolean` | Always written; open around the clock, i.e. without regular openings. |
| `regularOpenings` | array | `[{ "monday": [{ "begin": "07:00", "end": "21:00" }] }, …]`, one object per day. |
| `exceptionalOpenings` | array of `String` | `"begin -> end"`. |
| `exceptionalClosings` | array of `String` | `"begin -> end"`. |
| `freeText` | `String` | Optional free text. |


## Images

Each element of `images` (`Image`, [source](../WWCP_POI2/DataStructures/Complex/Image.cs)), as for
[ChargingPool](ChargingPool.md):

| JSON key | Type | Description |
|---|---|---|
| `url` | `URL` | Mandatory; where the image is fetched; unique within the garage. |
| `category` | `ImageCategory` | Mandatory, e.g. `CHARGER`, `ENTRANCE`, `LOCATION`, `NETWORK`, `OPERATOR`, `OTHER`, `OWNER`. |
| `type` | `HTTPContentType` | Mandatory media type, e.g. `image/jpeg`. |
| `width` | `UInt16` | Optional width of the full scale image. |
| `height` | `UInt16` | Optional height of the full scale image. |
| `thumbnail` | `URL` | Optional URL of a thumbnail. |


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`ParkingGarage`.

| Type | Predefined values | Initial |
|---|---|---|
| `ParkingGarageAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` since `created` |
| `ParkingGarageStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `blocked`, `available` | `available` since `created` |

Any other non-empty text parses as well. No energy status. A change of the garage or of anything else of its
parking operator keeps its runtime status.

Events: `OnAdminStatusChanged` (`OnParkingGarageAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnParkingGarageStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, ParkingGarage, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory, any non-empty text (trimmed); unique within the roaming network, compared
  case-insensitively.
- `chargingStationIds` and `parkingProductIds`: none twice; images: none with the same URL twice.
- Every `chargingStationIds` names a charging station of the network, every `parkingProductIds` a product of
  its parking operator.
- It can not be removed while a parking space of its operator names it.
- `Add` needs its parking operator as the parent, an existing one, and an object whose `@id` is the change's
  `entityId`; the garage must not exist yet.


## Example

```json
{
  "@id":                "PRK-G1",
  "name":               { "en": "Car park Main Station" },
  "address":            { "street": "Bahnhofstraße", "houseNumber": "2", "postalCode": "07743", "city": { "de": "Jena" }, "country": "DEU" },
  "geoLocation":        { "lat": 50.9271, "lng": 11.5892 },
  "chargingStationIds": [ "DE*GEF*S1" ],
  "openingTimes":       { "24/7": false, "regularOpenings": [ { "monday": [ { "begin": "06:00", "end": "23:00" } ] } ] },
  "images":             [ { "url": "https://parking.example.org/g1.jpg", "category": "LOCATION", "type": "image/jpeg", "width": 1200, "height": 800 } ],
  "parkingProductIds":  [ "SHORT-STAY" ],
  "created":            "2026-01-01T00:00:00.000Z",
  "lastChange":         "2026-10-12T12:00:00.000Z"
}
```
