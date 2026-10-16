# EVSE

An EVSE (Electric Vehicle Supply Equipment, a "charging point") charges one vehicle at a time: its current types,
charging modes, electrical limits, charging connectors (plugs or sockets), its energy meter and the tariffs that
apply. It is embedded completely in a [ChargingStation](ChargingStation.md), under `EVSEs`, and so in a
[ChargingPool](ChargingPool.md) of a [ChargingStationOperator](ChargingStationOperator.md).

The EVSE belongs to the charging station operator its identification names: `DE*GEF*E1*1` belongs to `DE*GEF`,
and every change of it, and every `Add`/`Remove` of it, must be signed with a signing key of that operator.
The identification is `{operatorId}*E{suffix}`, the suffix 1–30 letters, digits and `*`, e.g. `DE*GEF*E1*1`;
it is not tied to the identification of its station. It is unique within the operator, across all its stations;
two identifications are equal when they differ only in case or in the `*` of the suffix.

| | |
|---|---|
| Class | `EVSE` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStation/EVSE/EVSE.cs) |
| Identification | `EVSE_Id`, e.g. `DE*GEF*E1*1` |
| Admin status | `EVSEAdminStatusType`, initially `Operational` |
| Status | `EVSEStatusType`, initially `Available` |
| Energy status | `EnergyInfo`, none until reported |
| Embedded in | [ChargingStation](ChargingStation.md) as `EVSEs` |
| Owner | the [ChargingStationOperator](ChargingStationOperator.md) its identification names (signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}` |


## Common properties

See [common properties](README.md#common-properties). An EVSE does not write `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — |
| `lastChange` | — (set to the change set's `createdAt`) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `physicalReference` | `PhysicalReference` | `String?` | update/remove | The reference printed on the EVSE, e.g. `"A1"`. |
| `currentTypes` | `CurrentTypes` | `CurrentTypes` (flags) | update/remove | `AC_OnePhase`, `AC_ThreePhases`, `DC`; see [currentTypes and chargingModes](#currenttypes-and-chargingmodes). |
| `chargingModes` | `ChargingModes` | `ChargingModes` (flags) | update/remove | `Mode_1`, `Mode_2`, `Mode_3`, `Mode_4`, `CHAdeMO`; see [currentTypes and chargingModes](#currenttypes-and-chargingmodes). |
| `maxVoltage` | `MaxVoltage` | `Volt?` | update/remove | The maximum voltage, in V, not negative. |
| `maxCurrent` | `MaxCurrent` | `Ampere?` | update/remove | The maximum current, in A, not negative. |
| `maxPower` | `MaxPower` | `Watt?` | update/remove | The maximum power, in W, not negative. |
| `chargingConnectors` | `ChargingConnectors` | `ImmutableArray<ChargingConnector>` | update/remove | Its plugs and sockets; see [chargingConnectors](#chargingconnectors). |
| `energyMeter` | `EnergyMeter` | `EnergyMeter?` | update/remove | Its energy meter, 0..1; see [energyMeter](#energymeter). |
| `chargingTariffIds` | `ChargingTariffIds` | `ImmutableArray<ChargingTariff_Id>` | update/remove | The tariffs of its operator that apply; see [chargingTariffIds](#chargingtariffids). |
| `brandIds` | `BrandIds` | `ImmutableArray<Brand_Id>` | update/remove | Brands of the roaming network it is offered under, each once, in the ordinal order of their identifications; every brand must exist in the network. |
| `images` | `Images` | `ImmutableArray<Image>` | update/remove | Its images, in their given order; see [images](#images). |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | `{lat, lng, alt?}`, only when it differs from the one of its station. |
| `maxReservationDuration` | `MaxReservationDuration` | `TimeSpan?` | update/remove | How long it can be reserved at most, in s (a decimal), not negative. |

Absent values are left out; empty arrays and empty flags are not written.

**Derived (not serialized)**

- `AllEnergyMeters`: its energy meter, if any.
- `EnergyStatus`: the current energy status, see [Energy status](#energy-status).


## currentTypes and chargingModes

Both are flags, written as an array of the names of the set flags, in the order of their values, and not written
when none is set (`Unspecified`). When read, each entry must be a string naming one of the flags below
(case-sensitive); `Unspecified` is refused, a name given twice counts once.

| Flags | Names, in their order |
|---|---|
| `CurrentTypes` | `AC_OnePhase`, `AC_ThreePhases`, `DC` |
| `ChargingModes` | `Mode_1`, `Mode_2`, `Mode_3`, `Mode_4` (IEC 61851-1), `CHAdeMO` |

A change set updates or removes each array as a whole; removing it leaves `Unspecified`.


## chargingConnectors

`chargingConnectors`: the plugs and sockets of the EVSE, 0..n, in the ordinal order of their identifications.
A `ChargingConnector` ([source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStation/EVSE/ChargingConnector/ChargingConnector.cs))
is not an entity: it is content of its EVSE, without status, timestamps or custom data of its own, and it is
changed only by updating or removing the EVSE's `chargingConnectors` as a whole, e.g.
`RoamingNetworkChange.UpdateProperty(evse, "chargingConnectors", old, new)`.

| JSON key | Type | Description |
|---|---|---|
| `@id` | `ChargingConnector_Id` | Mandatory; any nonempty text (trimmed), e.g. `"1"`. Unique within its EVSE, compared case-insensitively. |
| `type` | `ChargingConnectorType` | Mandatory; any nonempty text. Predefined: `CHADEMO`, `CHAOJI`, `DOMESTIC_A` … `DOMESTIC_O`, `GBT_AC`, `GBT_DC`, `IEC_60309_2_single_16`, `IEC_60309_2_three_16`, `IEC_60309_2_three_32`, `IEC_60309_2_three_64`, `IEC_62196_T1`, `IEC_62196_T1_COMBO`, `IEC_62196_T2`, `IEC_62196_T2_COMBO`, `IEC_62196_T3A`, `IEC_62196_T3C`, `NEMA_5_20`, `NEMA_6_30`, `NEMA_6_50`, `NEMA_10_30`, `NEMA_10_50`, `NEMA_14_30`, `NEMA_14_50`, `PANTOGRAPH_BOTTOM_UP`, `PANTOGRAPH_TOP_DOWN`, `TESLA_R`, `TESLA_S`. |
| `lockable` | boolean | Optional; whether the connector can be locked, if known. |

The connectors must be given completely: a JSON object naming `chargingConnectorIds` instead is refused.


## energyMeter

`energyMeter`: the [energy meter](EnergyMeter.md) of the EVSE, 0..1, embedded completely. Its identification is
unique within the roaming network, and its `manufacturerId` must name an energy meter manufacturer of the network.
It is not an entity of change sets: the EVSE's `energyMeter` is updated or removed as a whole by
`RoamingNetworkChange.UpdateProperty(evse, "energyMeter", old, new)`.


## chargingTariffIds

`chargingTariffIds`: the [charging tariffs](ChargingTariff.md) that apply, each once, in the ordinal order of
their identifications. Every one must be a tariff of the same operator (`DE*GEF*T3` for an EVSE of `DE*GEF`);
a tariff can not be removed while an EVSE names it. [EVSEGroup](EVSEGroup.md),
[ChargingStationGroup](ChargingStationGroup.md) and [ChargingPoolGroup](ChargingPoolGroup.md) may name a
`chargingTariffId` for all their members, unless their own apply.


## images

`images`: the EVSE's images, in their given order, each URL only once (ordinal).

| JSON key | Type | Description |
|---|---|---|
| `url` | URL | Where the image is fetched from. Mandatory. |
| `category` | string | What it shows: `CHARGER`, `ENTRANCE`, `LOCATION`, `NETWORK`, `OPERATOR`, `OTHER`, `OWNER`. Mandatory. |
| `type` | string | The HTTP content type, e.g. `image/png`. Mandatory. |
| `thumbnail` | URL | Where a thumbnail is fetched from. Optional. |
| `width` | integer | Width of the full scale image in pixels, 0–65535. Optional. |
| `height` | integer | Height of the full scale image in pixels, 0–65535. Optional. |

`images` is updated or removed as a whole.


## Energy status

An EVSE has an energy status beside its admin status and status: runtime data, not content, without ETags,
shared by all versions of the EVSE. There is none until one is reported.

| JSON key | Type | Description |
|---|---|---|
| `used` | decimal | The power used, in W, not negative. Mandatory. |
| `available` | decimal | The power available, in W, not negative. Mandatory. |
| `storedEnergy` | decimal | The energy stored, in Wh, not negative. Optional. |
| `currentVoltage` | decimal | The voltage it currently charges with, in V, not negative. Optional; only an EVSE has it. |
| `remainingChargingTime` | decimal | How long the vehicle will probably still charge, in s, not negative. Optional; only an EVSE has it. |

Other keys are refused ([`POIEnergyStatus`](../WWCP_POI3/DataStructures/POIEnergyStatus.cs)). Its text, e.g. in
status updates, is this JSON, compact and in this order. The schedule
([`EnergyStatusRuntime`](../WWCP_POI3/DataStructures/EnergyStatusRuntime.cs)) keeps the last ten entries.

- `EnergyStatus`: the current `Timestamped<EnergyInfo>?`, `null` before the first one.
- `SetEnergyStatus(energyStatus, timestamp?, dataSource?)`: since the given timestamp or now; a later timestamp
  plans it. A value repeating the one in effect before it is ignored.
- `OnEnergyStatusChanged`: `(timestamp, eventTrackingId, evse, oldEnergyStatus, newEnergyStatus, dataSource)`.

In a history, it is set and read by `history.RuntimeStatus` with the kind `energyStatus` and the entity type
`EVSE`, and pushed on the WebSocket topic `energyStatusUpdates`.


## Runtime status

See [runtime status](README.md#runtime-status) for the mechanism.

- **Admin status** `EVSEAdminStatusType`, initially `Operational` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Operational` (`operational`), `InternalUse` (`internalUse`), `OutOfService` (`outOfService`),
  `Blocked` (`blocked`).
- **Status** `EVSEStatusType`, initially `Available` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Offline` (`offline`), `InDeployment` (`inDeployment`),
  `Reserved` (`reserved`), `Available` (`available`), `WaitingForPlugin` (`waitingForPlugin`),
  `PluggedIn` (`pluggedIn`), `Charging` (`charging`), `Occupied` (`occupied`), `DoorNotClosed` (`doorNotClosed`),
  `Error` (`error`), `Unavailable` (`unavailable`), `Inoperative` (`inoperative`), `OutOfService` (`outOfService`),
  `OutOfOrder` (`outOfOrder`), `Blocked` (`blocked`), `Removed` (`removed`).
- **Energy status:** see [Energy status](#energy-status).
- **Events:** `OnAdminStatusChanged`, `OnStatusChanged` `(timestamp, eventTrackingId, evse, oldStatus, newStatus)`,
  `OnEnergyStatusChanged`.


## Validation

- `@id` is mandatory and a valid EVSE identification.
- `maxVoltage`, `maxCurrent`, `maxPower` and `maxReservationDuration` must not be negative.
- A charging connector must not be given twice; each needs `@id` and `type`.
- `chargingTariffIds` and `brandIds` must not name a value twice; an image URL must not be given twice.
- `currentTypes` and `chargingModes` accept only the flags above.
- In its station and operator: the EVSE belongs to the operator of its station; it is unique within the operator
  across all stations; its `chargingTariffIds` must be tariffs of the operator; its energy meter is unique in the
  roaming network.
- In its roaming network: `brandIds` must name brands of the network, the energy meter's manufacturer an energy
  meter manufacturer.
- A change set applies each change to the embedded JSON of the operator and reads the operator again, so the
  operator must be valid after every single change (e.g. remove an EVSE from its [EVSEGroups](EVSEGroup.md)
  before removing it; add a tariff before naming it); the references into the network are checked after the last change.
- `Add(evse, stationId)` needs the station, of the same operator, as its parent; the EVSE must not exist yet. It
  gets the change set's `createdAt` as `lastChange`, and as `created` unless given. `Remove(evseId)` removes it
  with its connectors and energy meter.


## Example

```json
{
  "@id":                    "DE*GEF*E1*1",
  "name":                   { "en": "Left" },
  "physicalReference":      "A1",
  "currentTypes":           [ "AC_ThreePhases" ],
  "chargingModes":          [ "Mode_3" ],
  "maxVoltage":             400,
  "maxCurrent":             32,
  "maxPower":               22000,
  "chargingConnectors":     [ { "@id": "1", "type": "IEC_62196_T2", "lockable": true } ],
  "energyMeter": {
    "@id":         "evse-meter-1",
    "role":        "evse",
    "created":     "2026-01-01T00:00:00.000Z",
    "lastChange":  "2026-01-01T00:00:00.000Z"
  },
  "chargingTariffIds":      [ "DE*GEF*T3" ],
  "brandIds":               [ "blue-charge" ],
  "images": [
    {
      "url":       "https://images.example/evse.png",
      "category":  "CHARGER",
      "type":      "image/png",
      "width":     800,
      "height":    600
    }
  ],
  "maxReservationDuration": 900,
  "created":                "2026-01-01T00:00:00.000Z",
  "lastChange":             "2026-10-11T12:00:00.000Z"
}
```
