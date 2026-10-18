# ParkingSpaceTemplate

A parking space template gives parking spaces of one parking operator what they share, e.g. the garage, the
dimensions, the vehicle types and the roof of all spaces of a level, given once, and the parking products offered at
them. It does not name its members: a parking space names its templates in its
[`templateIds`](README.md#templates-and-effective-members), in an order of its own, and takes their properties at
runtime, as its [effective parking space](#properties-of-its-members). It is embedded completely in its
[ParkingOperator](ParkingOperator.md) under `parkingSpaceTemplates`, in the order of its identification.

Its parking operator owns it and signs every change to it. Its identification is a free text, compared
case-insensitively, e.g. `PRK-LEVEL`, unique within the roaming network (not only within its operator).

| | |
|---|---|
| Class | `ParkingSpaceTemplate` |
| Identification | `ParkingSpaceTemplate_Id`, e.g. `PRK-LEVEL` (`NewRandom()` makes a UUIDv7) |
| Runtime status | none |
| Embedded in | [ParkingOperator](ParkingOperator.md) as `parkingSpaceTemplates` |
| Owner | its parking operator (signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/POs/{PO_Id}/PSTs/{PST_Id}`; embedded in its parking operator, and listed by `…/PSTs` |


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

The properties of a parking space it gives its members, each written and read as a [parking space](ParkingSpace.md#properties)
writes and reads it, and each updated or removed by a change set. Of `geoLocation`, `parkingGarageId` (a garage of its
parking operator), `physicalReference`, `maxVehicleWeight`, `maxVehicleHeight`, `maxVehicleLength`, `maxVehicleWidth`,
`length`, `width`, `evsePosition`, `direction`, `surface`, `slope`, `timeLimit`, `restrictedToVehicleTypes`,
`driveThrough`, `reservationRequired`, `roofed`, `illuminated`, `refrigerationOutlet`, `dangerousGoodsAllowed` and
`protectedArea` its members take the value of the first of their templates giving one, unless they have their own. Of
`parkingSensorIds` (sensors of its parking operator), `chargingStationIds` (charging stations of the network),
`parkingProductIds` ([parking products](ParkingOperator.md#parking-products) of its parking operator),
`parkingRestrictions`, `euVehicleCategories`, `vehicleTypes`, `standards` and `images` they take those of all their
templates besides their own; of images of the same URL their own, else the first template's. The APDS reference
belongs to the single parking space: no template gives it.

Arrays are written only when not empty. A change set names the properties by their JSON keys:

```csharp
RoamingNetworkChange.Add           (template, parkingOperatorId)
RoamingNetworkChange.UpdateProperty(template, "roofed", null, true)
RoamingNetworkChange.Remove        (templateId)
```


## Properties of its members

A parking space takes the properties of its templates at runtime: its **effective parking space**, which its
parking operator calculates once per version, `parkingOperator.TryGetEffectiveParkingSpace(spaceId, out var space)`,
`parkingOperator.EffectiveParkingSpaces`, or in the roaming network `TryGetEffectiveParkingSpaceById` and
`EffectiveParkingSpaces`. Its templates must not make it invalid. See
[templates and effective members](README.md#templates-and-effective-members). POI_API serves the effective parking
spaces; `?view=stored` the parking spaces as they are given.


## Validation

- `@id` is mandatory, any non-empty text (trimmed); unique within the roaming network, compared
  case-insensitively.
- The arrays of the properties of its members: none twice; vehicle limits and dimensions positive, the time limit
  not negative.
- Every `parkingProductIds` names a product, `parkingGarageId` a garage and every `parkingSensorIds` a sensor of its
  parking operator; every `chargingStationIds` a charging station of the network. Every effective parking space naming
  it must be valid.
- A template a parking space names can not be removed: update the parking space first, in the same change set.
- `Add` needs its parking operator as the parent, an existing one, and an object whose `@id` is the change's
  `entityId`; the template must not exist yet.


## Example

```json
{
  "@id":               "PRK-LEVEL",
  "name":              { "en": "Garage 1, a level" },
  "parkingProductIds": [ "SHORT-STAY" ],
  "parkingGarageId":   "PRK-G1",
  "roofed":            true,
  "length":            5,
  "width":             2.5,
  "created":           "2026-01-01T00:00:00.000Z",
  "lastChange":        "2026-01-01T00:00:00.000Z"
}
```
