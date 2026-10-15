# ParkingOperator

A parking operator runs car parks and parking spaces, e.g. those at or around charging stations. In POI 3 it is
a party of the roaming network: embedded completely in the [RoamingNetwork](RoamingNetwork.md) under
`parkingOperators`, in the order of its identification. It embeds its parking garages, parking spaces,
parking sensors, parking space groups and parking products directly; a parking space names its garage, it is
not embedded in it.

It owns its own properties and all its parking data, and signs their changes with one of its `signingKeys`;
it rotates its `signingKeys` with one of them, or with one of its `identityKeys` when none of them can sign any more,
and its `identityKeys` only with one of those; for recovery, an identity key of the roaming network operator (RNO)
changes both. The RNO adds it without its parking data, and removes it with all of it, without its signature: the RNO
decides who is a member of the roaming network. Its identification is a free text,
compared case-insensitively, e.g. `DE*PRK`, unique within the roaming network.

| | |
|---|---|
| Class | `ParkingOperator` — [source](../WWCP_POI3/RoamingNetwork/ParkingOperator/ParkingOperator.cs) |
| Identification | `ParkingOperator_Id`, e.g. `DE*PRK` |
| Admin status | `ParkingOperatorAdminStatusType`, initially `operational` |
| Status | `ParkingOperatorStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `parkingOperators` |
| Owner | itself (signing key), also for everything it embeds; its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO; added and removed by the RNO |
| HTTP | `GET ~/RNs/{RN_Id}/POs/{PO_Id}` (with everything it embeds) |


## Common properties

See [Common properties](README.md#common-properties). It writes no `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — (set on `Add` unless given) |
| `lastChange` | — (set to the change set's `createdAt` when its own properties change, and when its garages, spaces, sensors, space groups or products change) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `address` | `Address` | `Address?` | update/remove | Address of the headquarters, as Styx writes it embedded (`street`, `houseNumber`, `postalCode`, `city`, `country` as ISO 3166 alpha-3, …). |
| `homepage` | `Homepage` | `URL?` | update/remove | Homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | Hotline phone number. |
| `parkingGarages` | `ParkingGarages` | `POICollection<ParkingGarage_Id, ParkingGarage>` | add/remove | Its [parking garages](#parking-garages), ordered by identification. |
| `parkingSpaces` | `ParkingSpaces` | `POICollection<ParkingSpace_Id, ParkingSpace>` | add/remove | Its [parking spaces](#parking-spaces), ordered by identification. |
| `parkingSensors` | `ParkingSensors` | `POICollection<ParkingSensor_Id, ParkingSensor>` | add/remove | Its [parking sensors](#parking-sensors), ordered by identification. |
| `parkingSpaceGroups` | `ParkingSpaceGroups` | `POICollection<ParkingSpaceGroup_Id, ParkingSpaceGroup>` | add/remove | Its [parking space groups](#parking-space-groups), ordered by identification; they may overlap. |
| `parkingProducts` | `ParkingProducts` | `POICollection<ParkingProduct_Id, ParkingProduct>` | add/remove | Its [parking products](#parking-products), ordered by identification (ordinal, case-sensitive). |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update (signing or identity key) | Keys signing its change sets, see [Keys](#keys). |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update (identity key) | Long-term keys signing changes of its identity keys, see [Keys](#keys). |

The arrays are written only when not empty. A change set updates or removes the properties marked so by their
JSON keys (`ParkingOperatorRegistry` in
[RoamingNetwork.RegistryChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.RegistryChanges.cs)); the
embedded arrays change only by adding and removing their elements.

Derived (not serialized):

- `TryGetParkingGarage(id, …)`, `TryGetParkingSpace(id, …)`, `TryGetParkingSensor(id, …)`,
  `TryGetParkingSpaceGroup(id, …)`, `TryGetParkingProduct(id, …)`: lookups by identification.
- At the roaming network: `ParkingGarages`, `ParkingSpaces`, `ParkingSensors`, `ParkingSpaceGroups`,
  `ParkingProducts` of all parking operators, and `TryGetParkingGarageById`, `TryGetParkingSpaceById`,
  `TryGetParkingSensorById`, `TryGetParkingSpaceGroupById`, `TryGetParkingProductById`, `TryGetParkingOperatorById`.


## Embedded parking data

All parking data of an operator is changed by change sets
([RoamingNetwork.ParkingChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.ParkingChanges.cs)):

- `Add(child, parkingOperatorId)` names the parking operator as the parent; an existing element is found by
  its identification alone, as identifications are unique within the network. A parent given with any other
  change must be the actual one.
- Every change is signed by the parking operator having the element, or the one an earlier `Add` of the same
  change set named.
- A change edits the element within the operator's JSON and reads the operator again, so the operator must be
  valid as a whole afterwards (see [Validation](#validation)). The element's `lastChange` (except for products)
  becomes the change set's `createdAt`; the operator's own `lastChange` stays.
- Runtime status and internal data of the operator and of all its garages, spaces, sensors and groups go along
  to the new version, by identification.

### Parking garages

[ParkingGarage](ParkingGarage.md): a car park or parking lot, with opening times, images and products.
JSON key `parkingGarages`, ordered by identification. `RoamingNetworkChange.Add(garage, parkingOperatorId)`,
`Remove(garageId)`. A garage that a parking space of the operator names can not be removed.

### Parking spaces

[ParkingSpace](ParkingSpace.md): one parking space, optionally in a garage of the operator, watched by its
sensors. JSON key `parkingSpaces`, ordered by identification. `RoamingNetworkChange.Add(space, parkingOperatorId)`,
`Remove(spaceId)`. A space that a parking space group or a charging station (`parkingSpaceIds`) names can not be
removed.

### Parking sensors

[ParkingSensor](ParkingSensor.md): a sensor detecting whether parking spaces are occupied. JSON key
`parkingSensors`, ordered by identification. `RoamingNetworkChange.Add(sensor, parkingOperatorId)`,
`Remove(sensorId)`. A sensor that a parking space names can not be removed.

### Parking space groups

[ParkingSpaceGroup](ParkingSpaceGroup.md): parking spaces of the operator, e.g. one level or zone; groups may
overlap. JSON key `parkingSpaceGroups`, ordered by identification.
`RoamingNetworkChange.Add(group, parkingOperatorId)`, `Remove(groupId)`.

### Parking products

A parking product is what a parking operator offers for parking, e.g. short-term parking with a minimum
duration. It is no entity: no admin status, no status, no `created`/`lastChange`, no `dataSource`. Garages,
spaces and space groups of the same operator name it by `parkingProductIds`. It offers, it does not calculate
prices.

Class `ParkingProduct` — [source](../WWCP_POI3/RoamingNetwork/ParkingOperator/ParkingProduct/ParkingProduct.cs).
JSON key `parkingProducts`, ordered by identification (ordinal).

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `@id` | `Id` | `ParkingProduct_Id` | — | Mandatory, any non-empty text; compared **case-sensitively**; unique within the roaming network. |
| `name` | `Name` | `ImmutableI18NString?` | update/remove | Multi-language name. |
| `description` | `Description` | `ImmutableI18NString?` | update/remove | Multi-language description. |
| `minDuration` | `MinDuration` | `TimeSpan?` | update/remove | Minimum parking duration billed, in s (decimal); not negative. |
| `stopParkingAfterTime` | `StopParkingAfterTime` | `TimeSpan?` | update/remove | Parking duration after which parking must stop, in s (decimal); not negative, not below `minDuration`. |
| `customData` | `CustomData` | `CustomDataNew?` | update/remove | Vendor-specific extensions, written only with values. |

- `RoamingNetworkChange.Add(product, parkingOperatorId)`, `Remove(productId)`, `UpdateProperty(product, …)`,
  `RemoveProperty(product, …)`, owned by the parking operator. Its changes set no timestamps.
- A product that a garage, space or space group names can not be removed.
- `created` and `lastChange` are read (and must be timestamps) but dropped.


## Keys

`signingKeys` and `identityKeys` are arrays of public keys (`POIPublicKey`,
[source](../WWCP_POI3/DataStructures/POIPublicKey.cs)), each written only when it is not empty:

| JSON key | Type | Description |
|---|---|---|
| `@id` | `String` | SHA-256 COSE key thumbprint [RFC 9679] of `key`, 64 lowercase hex digits. A reader calculates it and refuses a key whose `@id` differs. |
| `key` | `String` | The public COSE key as canonical Base64 of its deterministic CBOR; refused with private parts. |
| `notBefore` | timestamp | Optional: valid since then. |
| `notAfter` | timestamp | Optional: valid until then; not before `notBefore`. |

- Ordered by `@id` (ordinal); a key given twice is refused, and so is a key that is both a signing and an identity key.
- A signing key signs change sets of the operator's data, including its parking data, and the rotation of its
  `signingKeys`; an identity key signs changes of `identityKeys`, and of `signingKeys` when none of them can sign
  any more. A key counts only if it is valid at the change set's `createdAt`, in the version the change set is
  applied to.
- A change replaces a whole array: `UpdateProperty(parkingOperator, "signingKeys", old, newArray)`, signed as above
  or, for recovery, by an identity key of the RNO. Removing an array, or updating it to an empty one, is refused.
- `Add` needs both arrays with at least one key. Parsing a network alone does not enforce this.
- See also [SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners).


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`ParkingOperator`; its garages, spaces, sensors and groups have their own.

| Type | Predefined values | Initial |
|---|---|---|
| `ParkingOperatorAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` since `created` |
| `ParkingOperatorStatusType` | `unknown`, `unspecified`, `offline`, `inDeployment`, `available`, `error` | `available` since `created` |

Any other non-empty text parses as well. No energy status.

Events: `OnAdminStatusChanged` (`OnParkingOperatorAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnParkingOperatorStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, ParkingOperator, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory, any non-empty text (trimmed); a parking operator must not be given twice in a network.
- Garages, spaces, sensors and space groups: no null entries, none given twice (identifications compared
  case-insensitively); products: none given twice (case-sensitively).
- The embedded arrays must contain complete objects; `parkingGarageIds`, `parkingSpaceIds`, `parkingSensorIds`,
  `parkingSpaceGroupIds` or `parkingProductIds` at the operator are refused.
- Every `parkingProductIds` of a garage, space or space group names a product of this operator.
- Every `parkingSpaceIds` of a space group names a space of this operator.
- Every `parkingGarageId` and `parkingSensorIds` of a space names a garage or sensor of this operator.
- In the network: garage, space, sensor, space group and product identifications are unique across all parking
  operators; every `chargingStationIds` of a garage, space or sensor names a charging station of the network;
  every `parkingSpaceIds` of a charging station names a parking space of the network.
- `Add` of a parking operator: with at least one signing and one identity key and without any non-empty
  `parkingGarages`, `parkingSpaces`, `parkingSensors`, `parkingSpaceGroups` or `parkingProducts`, as its parking
  data is its own to add. A change names no parent but the roaming network.


## Example

```json
{
  "@id":          "DE*PRK",
  "name":         { "en": "Example Parking" },
  "homepage":     "https://parking.example.org",
  "hotline":      "+49 3641 5550000",
  "parkingGarages": [
    { "@id": "PRK-G1", "name": { "en": "Car park Main Station" },
      "geoLocation": { "lat": 50.9271, "lng": 11.5892 },
      "parkingProductIds": [ "SHORT-STAY" ],
      "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "parkingSpaces": [
    { "@id": "PRK-G1-S001", "parkingGarageId": "PRK-G1", "parkingSensorIds": [ "PRK-SEN-001" ],
      "chargingStationIds": [ "DE*GEF*S1" ],
      "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "parkingSensors": [
    { "@id": "PRK-SEN-001", "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "parkingSpaceGroups": [
    { "@id": "PRK-G1-L1", "name": { "en": "Level 1" }, "parkingSpaceIds": [ "PRK-G1-S001" ],
      "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "parkingProducts": [
    { "@id": "SHORT-STAY", "name": { "en": "Short stay" }, "minDuration": 900, "stopParkingAfterTime": 14400 }
  ],
  "signingKeys":  [ { "@id": "a61f…0d3e", "key": "…" } ],
  "identityKeys": [ { "@id": "c0b7…92aa", "key": "…" } ],
  "created":      "2026-01-01T00:00:00.000Z",
  "lastChange":   "2026-01-01T00:00:00.000Z"
}
```

Key identifications and keys are abbreviated. The charging station `DE*GEF*S1` must exist in the network.
