# ChargingStation

A charging station (EVCS, "charger") is one device at a charging location: it has a manufacturer and a model,
its own energy meters and the EVSEs that charge one vehicle each. It is embedded completely in a
[ChargingPool](ChargingPool.md) of a [ChargingStationOperator](ChargingStationOperator.md), under `chargingStations`.
Its source lives under `RoamingNetwork/ChargingStationManufacturer/` for historical reasons only: a station
belongs to its operator, it is not embedded in its [ChargingStationManufacturer](ChargingStationManufacturer.md),
it names it by `manufacturerId`.

The station belongs to the charging station operator its identification names: `DE*GEF*S1` belongs to `DE*GEF`,
and every change of it, of its EVSEs, and every `Add`/`Remove` of it must be signed with a signing key of that
operator. The identification is `{operatorId}*S{suffix}`, the suffix 1–30 letters, digits and `*`, e.g.
`DE*GEF*S1`. It is unique within the operator, across all its pools; two identifications are equal when they
differ only in case or in the `*` of the suffix. The station's address, geo location and accessibility are
those of its pool unless given.

| | |
|---|---|
| Class | `ChargingStation` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStation/ChargingStation.cs) |
| Identification | `ChargingStation_Id`, e.g. `DE*GEF*S1` |
| Admin status | `ChargingStationAdminStatusType`, initially `Operational` |
| Status | `ChargingStationStatusType`, initially `Available` |
| Energy status | `EnergyInfo`, none until reported |
| Embedded in | [ChargingPool](ChargingPool.md) as `chargingStations` |
| Owner | the [ChargingStationOperator](ChargingStationOperator.md) its identification names (signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}`; the parking spaces it names, of parking operators or of its operator, `…/CSs/{CS_Id}/PSs[/{PS_Id}]`; `GET …/CSs/{CS_Id}/Brands` (its [brands](Brand.md), searched and paged), `GET …/CSs/{CS_Id}/Brands/{Brand_Id}` |


## Common properties

See [common properties](README.md#common-properties). A charging station does not write `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — |
| `lastChange` | — (set to the change set's `createdAt` by every change of the station and of its EVSEs) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `physicalReference` | `PhysicalReference` | `String?` | update/remove | The reference printed on the station, e.g. `"Station 2"`. |
| `address` | `Address` | `Address?` | update/remove | The address, only when it differs from the one of its pool. |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | `{lat, lng, alt?}`, only when it differs from the one of its pool. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | The hotline phone number, e.g. `"+49 3641 123456"`. |
| `manufacturerId` | `ManufacturerId` | `ChargingStationManufacturer_Id?` | update/remove | A [ChargingStationManufacturer](ChargingStationManufacturer.md) of the roaming network; see [Manufacturer and model](#manufacturer-and-model). |
| `modelId` | `ModelId` | `ChargingStationModel_Id?` | update/remove | The model as its manufacturer names it, e.g. `"Wallbox-22"`. |
| `energyMeters` | `EnergyMeters` | `ImmutableArray<EnergyMeter>` | update/remove | Its own energy meters, not those of its EVSEs; see [energyMeters](#energymeters). |
| `EVSEs` | `EVSEs` | `POICollection<EVSE_Id, EVSE>` | add/remove | Its EVSEs, embedded completely; see [EVSEs](#evses). |
| `brands` | `Brands` | `POICollection<Brand_Id, Brand>` | update/remove | Brands it also appears under; see [brands](#brands). |
| `openingTimes` | `OpeningTimes` | `ImmutableOpeningTimes?` | update/remove | When it is open; always, unless given. See [openingTimes](#openingtimes). |
| `images` | `Images` | `ImmutableArray<Image>` | update/remove | Its images, in their given order; see [images](#images). |
| `vehicleTypes` | `VehicleTypes` | `ImmutableArray<VehicleType>` | update/remove | The types of vehicles it charges, each once, in the ordinal order of their names. Predefined: `MOTORCYCLE`, `PERSONAL_VEHICLE`, `PERSONAL_VEHICLE_WITH_TRAILER`, `VAN`, `SEMI_TRACTOR`, `RIGID`, `TRUCK_WITH_TRAILER`, `BUS`, `DISABLED`; other nonempty texts are read as well. |
| `euVehicleCategories` | `EUVehicleCategories` | `ImmutableArray<EUVehicleCategory>` | update/remove | The EU vehicle categories it charges, as in the type-approval regulations (EU) 2018/858, 167/2013 and 168/2013, each once, in the ordinal order of their names. Predefined: `L`, `L1`…`L7` (category L1e…L7e), `M`, `M1`…`M3`, `N`, `N1`…`N3`, `O`, `O1`…`O4`, `R`, `R1`…`R4`, `T`, `T1`…`T4`; other nonempty texts are read as well. |
| `accessibility` | `Accessibility` | `AccessibilityType?` | update/remove | Who may use it, only when it differs from its pool. Predefined: `Unspecified`, `FreePubliclyAccessible`, `RestrictedAccess`, `PayingPubliclyAccessible`, `TestStation`, `TestPool`. |
| `features` | `Features` | `ImmutableArray<ChargingStationFeature>` | update/remove | Each once, in the ordinal order of their names. Predefined: `StatusInfoAvailable`, `EnergyInfoAvailable`, `GermanCalibrationLaw`, `EuropeanCalibrationLaw`, `Reservable`, `AuthenticationBeforePlugIn`, `ChargingProfilesSupported`, `ChargingPreferencesSupported`, `TokenGroupsSupported`, `CSOUnlockSupported`, `HubjectCompatible`, `START_SESSION_CONNECTOR_REQUIRED`, `Illuminated` (lit at night), `Roofed`, `IlluminatedParkingNearby`, `RoofedParkingNearby` (parking for charging nearby is); other nonempty texts are read as well. |
| `calibrationInfo` | `CalibrationInfo` | `URL?` | update/remove | Where to find information on the calibration of its energy meters; see [Calibration and certification](#calibration-and-certification). |
| `certificationInfo` | `CertificationInfo` | `URL?` | update/remove | Where to find information on its certification, e.g. its conformity with calibration law. |
| `maxPower` | `MaxPower` | `Watt?` | update/remove | The power it can deliver at once, in W, not negative: with load management often less than the sum of its EVSEs, never more. It is given, not calculated. |
| `parkingSpaceIds` | `ParkingSpaceIds` | `ImmutableArray<ParkingSpace_Id>` | update/remove | The parking spaces it charges at; see [parkingSpaceIds](#parkingspaceids). |

Absent values are left out; empty arrays are not written.

**Derived (not serialized)**

- `AllEnergyMeters`: its own energy meters and those of its EVSEs.
- `TryGetEVSE(EVSEId, out EVSE)`: an EVSE of this station by its identification.
- `EnergyStatus`: the current energy status, see [Energy status](#energy-status).


## EVSEs

`EVSEs`: the [EVSEs](EVSE.md) of the station, 0..n, embedded completely, in the ordinal order of their
identifications. An EVSE must belong to the same operator as the station (`DE*GEF*E1*1` to `DE*GEF`); its
identification is not otherwise tied to the station's. EVSE identifications are unique within the operator,
across all its stations. They are read only completely: a JSON object naming `EVSEIds` instead is refused.

An EVSE is added and removed as an entity, not as a property of the station:

```csharp
RoamingNetworkChange.Add   (evse, chargingStationId)   // the EVSE with everything it embeds
RoamingNetworkChange.Remove(evseId)
```

Moving an EVSE to another station is a `Remove` and an `Add`. An EVSE named by an
[EVSEGroup](EVSEGroup.md) can not be removed while the group names it.


## energyMeters

`energyMeters`: the station's own [energy meters](EnergyMeter.md), 0..n, in the ordinal order of their
identifications, each once; the meters of its EVSEs are in their `energyMeter`. Energy meter identifications are
unique within the roaming network. Energy meters are not entities of change sets: the station's `energyMeters`
is updated or removed as a whole by `RoamingNetworkChange.UpdateProperty(station, "energyMeters", old, new)`, or
[element by element](README.md#element-changes).
A meter's `manufacturerId` must name an energy meter manufacturer of the network. A JSON object naming
`energyMeterIds` instead is refused.


## Energy status

Like EVSEs, a charging station has an energy status beside its admin status and status: runtime data, not
content, without ETags, shared by all versions of the station. There is none until one is reported.

| JSON key | Type | Description |
|---|---|---|
| `used` | decimal | The power used, in W, not negative. Mandatory. |
| `available` | decimal | The power available, in W, not negative. Mandatory. |
| `storedEnergy` | decimal | The energy stored, in Wh, not negative. Optional. |

The `currentVoltage` and `remainingChargingTime` of an [EVSE](EVSE.md#energy-status)'s energy status are refused:
`SetEnergyStatus` throws, a status update is refused. Other keys are refused ([`POIEnergyStatus`](../WWCP_POI3/DataStructures/POIEnergyStatus.cs)). Its text, e.g. in
status updates, is this JSON, compact and in this order. The schedule
([`EnergyStatusRuntime`](../WWCP_POI3/DataStructures/EnergyStatusRuntime.cs)) keeps the last ten entries.

- `EnergyStatus`: the current `Timestamped<EnergyInfo>?`, `null` before the first one.
- `SetEnergyStatus(energyStatus, timestamp?, dataSource?)`: since the given timestamp or now; a later timestamp
  plans it. A value repeating the one in effect before it is ignored.
- `OnEnergyStatusChanged`: `(timestamp, eventTrackingId, chargingStation, oldEnergyStatus, newEnergyStatus, dataSource)`.

In a history, it is set and read by `history.RuntimeStatus` with the kind `energyStatus` and the entity type
`ChargingStation`, and pushed on the WebSocket topic `energyStatusUpdates`.


## Manufacturer and model

`manufacturerId` names a [ChargingStationManufacturer](ChargingStationManufacturer.md) embedded in the roaming
network; the network refuses a station naming an unknown one, and a manufacturer can not be removed while a
station names it. `modelId` is any nonempty text, as the manufacturer names the model; it is not checked
against the manufacturer.


## parkingSpaceIds

`parkingSpaceIds`: the [parking spaces](ParkingSpace.md) the station charges at, each once, in the ordinal
order of their identifications. They may be spaces of any [ParkingOperator](ParkingOperator.md) of the
network, but must exist there. In the other direction, parking garages, spaces and sensors name the stations
by `chargingStationIds`; a station they name can not be removed.


## openingTimes

`openingTimes`: when the station is open, as Styx's `OpeningTimes` write it; without it, the station is always
open. Without regular openings it is 24/7.

| JSON key | Type | Description |
|---|---|---|
| `24/7` | boolean | Always written: `true` without regular openings, `false` with them; another value is refused. |
| `regularOpenings` | array | `[{ "monday": [{ "begin": "07:00", "end": "21:00" }] }, …]`: per weekday its periods, `HH:mm`, in their given order. |
| `exceptionalOpenings` | array of strings | Periods open in addition, each `"begin -> end"` as ISO 8601 timestamps. |
| `exceptionalClosings` | array of strings | Periods closed, as `exceptionalOpenings`. |
| `freeText` | string | A free text, e.g. "Access through the hotel reception". |

`openingTimes` is updated or removed as a whole.


## images

`images`: the station's images, in their given order, each URL only once (ordinal).

| JSON key | Type | Description |
|---|---|---|
| `url` | URL | Where the image is fetched from. Mandatory. |
| `category` | string | What it shows: `CHARGER`, `ENTRANCE`, `LOCATION`, `NETWORK`, `OPERATOR`, `OTHER`, `OWNER`. Mandatory. |
| `type` | string | The HTTP content type, e.g. `image/png`. Mandatory. |
| `thumbnail` | URL | Where a thumbnail is fetched from. Optional. |
| `width` | integer | Width of the full scale image in pixels, 0–65535. Optional. |
| `height` | integer | Height of the full scale image in pixels, 0–65535. Optional. |

`images` is updated or removed as a whole.


## brands

`brands`: the [brands](Brand.md) the station also appears under, its own, embedded completely, each once, in the
ordinal order of their identifications. They are not those of its pool, its operator or its EVSEs: every level has
brands of its own. Changed as a whole, or [brand by brand](Brand.md#change-sets), e.g.
`AddElement(nameof(ChargingStation), stationId, [ new ("brands", "blue-charge") ], blueCharge.ToJSON())`;
`brandIds` is refused.


## Calibration and certification

`calibrationInfo` and `certificationInfo` are URLs, e.g. of the documents on the calibration of the station's
energy meters and on its conformity with calibration law. They are not checked beyond being URLs. Features such as
`GermanCalibrationLaw` or `EuropeanCalibrationLaw` say the same in short; the transparency software of the
meters is part of the [energy meters](EnergyMeter.md).


## Runtime status

See [runtime status](README.md#runtime-status) for the mechanism.

- **Admin status** `ChargingStationAdminStatusType`, initially `Operational` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Operational` (`operational`), `InternalUse` (`internalUse`), `OutOfService` (`outOfService`).
- **Status** `ChargingStationStatusType`, initially `Available` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Offline` (`offline`), `Available` (`available`), `PartialAvailable` (`partialAvailable`), `Reserved` (`reserved`),
  `Charging` (`charging`), `OutOfService` (`outOfService`), `Error` (`error`), `Removed` (`removed`).
- **Energy status:** see [Energy status](#energy-status).
- **Events:** `OnAdminStatusChanged`, `OnStatusChanged` `(timestamp, eventTrackingId, chargingStation, oldStatus, newStatus)`,
  `OnEnergyStatusChanged`.

The station's status is set on its own; it is not calculated from the status of its EVSEs.


## Validation

- `@id` is mandatory and a valid charging station identification.
- `maxPower` must not be negative, and not exceed the `maxPower` of all its EVSEs together, where all of them have one.
- Every EVSE must belong to the operator of the station; an EVSE must not be given twice.
- An energy meter must not be given twice; energy meter identifications are unique in the roaming network.
- `brands`, `vehicleTypes`, `euVehicleCategories`, `features` and `parkingSpaceIds` must not name a value twice; an image URL must
  not be given twice.
- `EVSEs`, `energyMeters` and `brands` must be given completely; `EVSEIds`, `energyMeterIds` and `brandIds` are refused.
- In its operator: the station belongs to the operator of its pool, and is unique within the operator across all
  pools; its EVSEs' `chargingTariffIds` must be tariffs of the operator; a [ChargingStationGroup](ChargingStationGroup.md)
  may only name stations of the operator.
- In its roaming network: `manufacturerId` must name a manufacturer and `parkingSpaceIds` parking spaces of the
  network.
- A change set applies each change to the embedded JSON of the operator and reads the operator again, so the
  operator must be valid after every single change (e.g. remove a station from its groups before removing it);
  the references into the network are checked after the last change.
- `Add(station, poolId)` needs the pool, of the same operator, as its parent; the station must not exist yet. It
  gets the change set's `createdAt` as `lastChange`, and as `created` unless given.


## Example

```json
{
  "@id":               "DE*GEF*S1",
  "name":              { "en": "Station 1" },
  "physicalReference": "Station 1",
  "hotline":           "+49 3641 123456",
  "manufacturerId":    "ACME",
  "modelId":           "Wallbox-22",
  "EVSEs": [
    {
      "@id":                "DE*GEF*E1*1",
      "physicalReference":  "1",
      "currentTypes":       [ "AC_ThreePhases" ],
      "chargingModes":      [ "Mode_3" ],
      "maxVoltage":         400,
      "maxCurrent":         32,
      "maxPower":           22000,
      "chargingConnectors": [ { "@id": "1", "type": "IEC_62196_T2", "lockable": true } ],
      "created":            "2026-01-01T00:00:00.000Z",
      "lastChange":         "2026-01-01T00:00:00.000Z"
    },
    {
      "@id":                "DE*GEF*E1*2",
      "physicalReference":  "2",
      "currentTypes":       [ "AC_ThreePhases" ],
      "chargingModes":      [ "Mode_3" ],
      "maxVoltage":         400,
      "maxCurrent":         32,
      "maxPower":           22000,
      "chargingConnectors": [ { "@id": "1", "type": "IEC_62196_T2", "lockable": true } ],
      "created":            "2026-01-01T00:00:00.000Z",
      "lastChange":         "2026-01-01T00:00:00.000Z"
    }
  ],
  "brands":            [ { "@id": "blue-charge", "name": { "en": "Blue Charge" } } ],
  "images": [
    {
      "url":       "https://images.example/station.png",
      "category":  "CHARGER",
      "type":      "image/png",
      "thumbnail": "https://images.example/thumbs/station.png",
      "width":     800,
      "height":    600
    }
  ],
  "vehicleTypes":      [ "MOTORCYCLE", "PERSONAL_VEHICLE" ],
  "euVehicleCategories": [ "L3", "M1" ],
  "features":          [ "EuropeanCalibrationLaw", "Reservable" ],
  "calibrationInfo":   "https://charging.example/calibration/S1",
  "maxPower":          44000,
  "parkingSpaceIds":   [ "space-1" ],
  "created":           "2026-01-01T00:00:00.000Z",
  "lastChange":        "2026-10-11T12:00:00.000Z"
}
```
