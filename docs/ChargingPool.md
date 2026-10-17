# ChargingPool

A charging pool is a charging location: one or more charging stations at one site, with its address, opening
times, access, parking, payment and authentication, energy mix, images, and where it is connected to the grid.
It corresponds to a location in OCPI.

It is embedded completely in its [ChargingStationOperator](ChargingStationOperator.md) under `chargingPools`, in the
ordinal order of the identifications, and itself embeds its charging stations (with their EVSEs), its own energy
meters and its own brands, and names its grid connection point. It belongs to the operator its identification names, and only that operator
changes it, with one of its signing keys.

Its identification is the operator's identification, `*P` and a suffix, e.g. `DE*GEF*P1`, unique within the operator.

| | |
|---|---|
| Class | `ChargingPool` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationOperator/ChargingPool/ChargingPool.cs) |
| Identification | `ChargingPool_Id`, `^([a-zA-Z]{2}\*?[a-zA-Z0-9]{3})\*?P([a-zA-Z0-9][a-zA-Z0-9\*]{0,29})$`, e.g. `DE*GEF*P1`; always written as `<operator>*P<suffix>` |
| Admin status | `ChargingPoolAdminStatusType`, initially `Operational` |
| Status | `ChargingPoolStatusType`, initially `Available` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `chargingPools` |
| Owner | the charging station operator its identification names (a signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}`, `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/Brands` (its [brands](Brand.md), searched and paged), `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/Brands/{Brand_Id}` |

A [ChargingPoolGroup](ChargingPoolGroup.md) may give its members most of the properties below, so that a pool has
only few of its own: at runtime it takes them, as the effective charging pool of its operator, with its effective
charging stations and EVSEs, `chargingStationOperator.TryGetEffectiveChargingPool(poolId, out var pool)`. See
[properties of its members](ChargingPoolGroup.md#properties-of-its-members). POI_API serves the effective charging
pool; `?view=stored` the pool as it is given.


## Common properties

See [Common properties](README.md#common-properties).

| JSON key | Change set | Note |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | Optional, no default. |
| `description` | update/remove | |
| `dataSource` | update/remove | |
| `created` | — | Set to the change set's `createdAt` when added without one. |
| `lastChange` | — | Set to the change set's `createdAt` by every change of the pool's own properties, of its stations and their EVSEs, and when it is added. |
| `customData` | update/remove | |

The pool writes no `eTags` and no `@context`.


## Properties

All optional; absent values and empty arrays are not written.

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `dataLicenses` | `DataLicenses` | `ImmutableArray<DataLicense>` | update/remove | The licenses of the pool's data, see [Data licenses](#data-licenses). |
| `address` | `Address` | `Address?` | update/remove | Its address, see [Address and geo locations](#address-and-geo-locations). |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | Its geographical location. |
| `parkingType` | `ParkingType` | `ParkingType?` | update/remove | The type of parking, e.g. `ALONG_MOTORWAY`, `PARKING_GARAGE`, `PARKING_LOT`, `ON_DRIVEWAY`, `ON_STREET`, `UNDERGROUND_GARAGE`, `OTHER`, `UNKNOWN`. |
| `accessibility` | `Accessibility` | `AccessibilityType?` | update/remove | Who may use it, e.g. `FreePubliclyAccessible`, `RestrictedAccess`, `PayingPubliclyAccessible`, `TestStation`, `TestPool`, `Unspecified`. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | The telephone number of the hotline for this pool. |
| `gridConnectionPointId` | `GridConnectionPointId` | `GridConnectionPoint_Id?` | update/remove | Where it is connected to the public grid: a grid connection point of a grid operator of the network, see [Grid connection point](#grid-connection-point). |
| `energyMeters` | `EnergyMeters` | `ImmutableArray<EnergyMeter>` | update/remove | Its own energy meters, see [Energy meters](#energy-meters). |
| `chargingStations` | `ChargingStations` | `POICollection<ChargingStation_Id, ChargingStation>` | add/remove | Its [charging stations](#charging-stations). |
| `brands` | `Brands` | `POICollection<Brand_Id, Brand>` | update/remove | The [brands](Brand.md) it also appears under, its own, embedded completely; not those of its operator, stations or EVSEs. Ordered by `@id`, each once; changed as a whole or [brand by brand](Brand.md#change-sets). |
| `openingTimes` | `OpeningTimes` | `ImmutableOpeningTimes?` | update/remove | When it is open, see [Opening times](#opening-times); always, unless given. |
| `energyMix` | `EnergyMix` | `EnergyMix?` | update/remove | The mix of the energy it delivers, see [Energy mix](#energy-mix). |
| `images` | `Images` | `ImmutableArray<Image>` | update/remove | See [Images](#images). |
| `timeZone` | `TimeZone` | `Time_Zone?` | update/remove | Its time zone, e.g. `Europe/Berlin`. |
| `facilities` | `Facilities` | `ImmutableArray<Facility>` | update/remove | Facilities nearby, e.g. `HOTEL`, `RESTAURANT`, `CAFE`, `MALL`, `SUPERMARKET`, `SPORT`, `RECREATION_AREA`, `NATURE`, `MUSEUM`, `BIKE_SHARING`, `BUS_STOP`, `TAXI_STAND`, `TRAM_STOP`, `METRO_STATION`, `TRAIN_STATION`, `AIRPORT`, `PARKING_LOT`, `CARPOOL_PARKING`, `FUEL_STATION`, `WIFI`; ordinal order, each once. |
| `paymentOptions` | `PaymentOptions` | `ImmutableArray<PaymentOptions>` | update/remove | How charging can be paid: `Free`, `Contract`, `Direct`, `Cash`, `CreditCard`, `DebitCard`, `PrepaidCard`, `SMS`; ordinal order, each once. |
| `authenticationModes` | `AuthenticationModes` | `ImmutableArray<AuthenticationMode>` | update/remove | How drivers authenticate, see [Authentication modes](#authentication-modes). |
| `uiFeatures` | `UIFeatures` | `ImmutableArray<UIFeatures>` | update/remove | Features of its user interfaces, e.g. `Screen`, `CreditCard`, `DebitCard`, `Pinpad`, `Sound`, `SpeechRecognition`, `RFID`, `NFC`, `Bluetooth`, `BLEBeacons`, `WLAN`; ordinal order, each once. |
| `features` | `Features` | `ImmutableArray<ChargingPoolFeature>` | update/remove | Its features, e.g. `StatusInfoAvailable`, `EnergyInfoAvailable`, `GermanCalibrationLaw`, `EuropeanCalibrationLaw`, `Reservable`, `AuthenticationBeforePlugIn`, `ChargingProfilesSupported`, `ChargingPreferencesSupported`, `TokenGroupsSupported`, `CSOUnlockSupported`, `HubjectCompatible`, `START_SESSION_CONNECTOR_REQUIRED`, `Illuminated` (lit at night), `Roofed`, `IlluminatedParkingNearby`, `RoofedParkingNearby` (parking for charging nearby is); ordinal order, each once. |
| `locationLanguages` | `LocationLanguages` | `ImmutableArray<Languages>` | update/remove | The languages spoken or shown there, e.g. `de`, `en`, in their given order (preference), each once. |
| `entranceAddress` | `EntranceAddress` | `Address?` | update/remove | The address of its entrance, when it differs from its address. |
| `entranceLocation` | `EntranceLocation` | `GeoCoordinate?` | update/remove | The geographical location of its entrance. |
| `exitAddress` | `ExitAddress` | `Address?` | update/remove | The address of its exit, when it differs from its address. |
| `exitLocation` | `ExitLocation` | `GeoCoordinate?` | update/remove | The geographical location of its exit. |
| `arrivalInstructions` | `ArrivalInstructions` | `ImmutableI18NString` | update/remove | How to get there, e.g. `{"en": "Second floor of the car park"}`. |
| `chargingWhenClosed` | `ChargingWhenClosed` | `Boolean?` | update/remove | Whether a vehicle may go on charging while it is closed; absent means unknown. |
| `parkingRestrictions` | `ParkingRestrictions` | `ImmutableArray<ParkingRestrictionGroup>` | update/remove | Who may park there: `EMPLOYEES`, `EV_ONLY`, `PLUGGED`, `DISABLED`, `CUSTOMERS`, `TAXI`, `TENANTS`; ordinal order, each once. |
| `relatedLocations` | `RelatedLocations` | `ImmutableArray<AdditionalGeoLocation>` | update/remove | Further related places, see [Related locations](#related-locations). |
| `services` | `Services` | `ImmutableArray<LocationService>` | update/remove | The services offered there, as the OCPI Accessibility Extension; a set of names, each once, in their ordinal order. Predefined: `ACCESSIBLE_CHARGING`, `ACCESSIBLE_TOILETS`, `ASSISTANCE`, `CAMERA_SURVEILLANCE`, `EMERGENCY_ASSISTANCE`, `EMERGENCY_CALL`, `PERIMETER_FENCE`; any other nonempty text is accepted. |
| `assistanceServiceDetails` | `AssistanceServiceDetails` | I18N text | update/remove | How to get assistance there: how to reach it, when, and what kind of support it offers. Omitted when empty. |
| `standards` | `Standards` | `ImmutableArray<Standard>` | update/remove | The standards it conforms to, e.g. `PAS 1899:2022` for accessible charging; a set of names as `services`. |
| `nutsCodes` | `NUTSCodes` | `ImmutableArray<NUTSCode>` | update/remove | The NUTS regions of the EU it is in, e.g. `DEG` (NUTS 1, Thüringen) and `DEG03` (NUTS 3, Jena): two letters of the country and up to three letters or digits, the level being their number; read case-insensitively, written in upper case; each once, in their ordinal order. |

The values of `parkingType`, `accessibility`, `facilities`, `uiFeatures`, `features` and `parkingRestrictions`
are open sets of names: the ones listed are predefined, others are read as given. `paymentOptions` and
`locationLanguages` are enumerations: only their names are read.

Arrays of names are written in the ordinal order of the names, whatever order they are given in; a change set
should send them in that order, as it compares old values as JSON.

**Derived (not serialized)**

- `EVSEs`: the EVSEs of all its charging stations.
- `AllEnergyMeters`: its own energy meters, and those of its charging stations and their EVSEs.
- `TryGetChargingStation(id, out station)`: a lookup by identification.


## Charging stations

The chargers of the pool, `chargingStations`, each embedded completely with its EVSEs, in the ordinal order of their
identifications, e.g. `DE*GEF*S1`. A station names the same operator as its pool and is unique within the operator,
not only within the pool.

- **Add:** `RoamingNetworkChange.Add(chargingStation, chargingPoolId)`; the parent pool must exist.
- **Remove:** `RoamingNetworkChange.Remove(chargingStationId)`; removing the last one removes `chargingStations`.
  A station a charging station group names can not be removed.
- **Move** to another pool of the operator: `Remove` and `Add`.

See [ChargingStation](ChargingStation.md).


## Grid connection point

`gridConnectionPointId`: where the pool is connected to the public grid, a grid connection point of the roaming
network. The grid connection point is embedded in its [GridOperator](GridOperator.md#grid-connection-points) and
documented there; several pools may name the same one. Its maintainer, the grid operator or e.g. this pool's
charging station operator, signs its changes; the pool's operator only chooses which point the pool names.
Embedding it in the pool (`gridConnectionPoint`) is refused.

Over HTTP, `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}?expand=gridConnectionPoint` embeds it in the place of
`gridConnectionPointId`, with its `gridOperatorId`: another representation, with its own ETag.


## Energy meters

`energyMeters`: the pool's own energy meters, e.g. a meter of the PV system or battery on site, in the ordinal order
of their identifications; not those of its stations and EVSEs. The meter of its grid connection point belongs to
the grid connection point, in its grid operator.
Energy meters have admin status and status, but no change set of their own: they are changed as the pool's property
`energyMeters`, as a whole or [element by element](README.md#element-changes). Their identifications are unique within the roaming network; their manufacturers must
be energy meter manufacturers of the network. `energyMeterIds` alone are refused.
See [EnergyMeter](EnergyMeter.md).


## Authentication modes

`authenticationModes`: how drivers authenticate, with the names of WWCP; an immutable value without identification.
A type only once, in the ordinal order of the types. Source: [AuthenticationMode.cs](../WWCP_POI3/DataStructures/AuthenticationMode.cs).

| JSON key | Type | Description |
|---|---|---|
| `type` | text | Mandatory, without surrounding whitespace: `Free Charging`, `RFID`, `PINPAD`, `ISO/IEC 15118 PLC`, `ISO/IEC 15118 Over-the-Air`, `REMOTE`, `CreditCard`, `DebitCard`, `PrepaidCard`, `NFC`, `Bluetooth`, `WLAN`, `No authentication required`, `DirectPayment`, `SMS`, `PhoneCall`, or another name. |
| `cardTypes` | array of texts | RFID only: the card types it reads, `MifareClassic`, `MifareDESFire`, `Calypso`; ordinal order, each once. |
| `brands` | array of objects | RFID only: the [brands](Brand.md) whose cards it takes, its own, embedded completely; ordered by `@id`, each once. `brandIds` is refused. |
| `number` | text | Mandatory for `SMS` and `PhoneCall`, the number to send an SMS to or to call; refused for other types. |
| `stationCode` | text | `SMS` only: the code of the station to send. |

C#: `AuthenticationMode.RFID(cardTypes, brands)`, `AuthenticationMode.SMS(number, stationCode)`,
`AuthenticationMode.PhoneCall(number)`, and static instances for the others, e.g. `AuthenticationMode.CreditCard`.


## Opening times

`openingTimes`, as Styx's `OpeningTimes` write them; without regular openings it is open 24/7.

| JSON key | Type | Description |
|---|---|---|
| `24/7` | boolean | Always written: whether it is always open. |
| `regularOpenings` | array | One object per weekday, `[{ "monday": [{ "begin": "07:00", "end": "21:00" }] }]`. |
| `exceptionalOpenings` | array of texts | Opening periods besides the regular ones, `"begin -> end"`. |
| `exceptionalClosings` | array of texts | Closing periods, `"begin -> end"`. |
| `freeText` | text | A description in words. |


## Energy mix

`energyMix`: the mix of the energy the pool delivers, written embedded, without `@context`.

| JSON key | Type | Description |
|---|---|---|
| `energySources` | array | Always written: `[category, percentage]` pairs, e.g. `[["Sun", "60 %"], ["Wind", "40 %"]]`; predefined categories `unknown`, `Gas`, `Coal`, `StoneCoal`, `Oil`, `OtherFossil`, `NuclearFision`, `NuclearFusion`, `Sun`, `Wind`, `Geothermal`, `Waste`, `Biogas`, `OtherGreen`. `[]` means unknown. |
| `environmentalImpacts` | array | Always written: `[category, amount]` pairs, the amount per kWh with its unit, e.g. `[["CarbonDioxide", "350 g/kWh"]]`; predefined categories `NuclearWaste`, `CarbonDioxide`. `[]` means unknown. |
| `supplierName` | I18N text | Mandatory. |
| `productName` | I18N text | Mandatory. |
| `additionalRemarks` | I18N text | Optional. |

Each category at most once; percentages between 0 and 100, amounts not negative
([`EnvironmentalImpactAmount`](../WWCP_POI2/DataStructures/Complex/EnvironmentalImpactAmount.cs)).


## Images

`images`, in their given order, a URL at most once.

| JSON key | Type | Description |
|---|---|---|
| `url` | text | Mandatory. The URL of the image. |
| `category` | text | Mandatory. Predefined: `CHARGER`, `ENTRANCE`, `LOCATION`, `NETWORK`, `OPERATOR`, `OTHER`, `OWNER`. |
| `type` | text | Mandatory. The media type, e.g. `image/jpeg`. |
| `thumbnail` | text | The URL of a thumbnail. |
| `width` | number | Pixels, 0..65535. |
| `height` | number | Pixels, 0..65535. |


## Related locations

`relatedLocations`: further places related to the pool, e.g. a walkway or a point of interest nearby; immutable
values without identification, in their given order, none twice. Source: [AdditionalGeoLocation.cs](../WWCP_POI3/DataStructures/AdditionalGeoLocation.cs).

| JSON key | Type | Description |
|---|---|---|
| `geoLocation` | object | Mandatory. `{lat, lng, alt?}`. |
| `name` | I18N text | What is there, e.g. `{"en": "Footpath to the station"}`. |


## Address and geo locations

`address`, `entranceAddress` and `exitAddress` are written as Styx writes an address embedded: `street`,
`postalCode`, `city` (I18N text), `country` (ISO 3166-1 alpha-3, e.g. `DEU`), and optionally `houseNumber`,
`floorLevel`, `region`, `postalCodeSub`, `timeZone`, `officialLanguages`, `comment`, `customData`.
`geoLocation`, `entranceLocation` and `exitLocation` are `{lat, lng, alt?, projection?}`, `projection` only when not
WGS84. Each is updated or removed as a whole. Field tables: see
[ChargingStationOperator](ChargingStationOperator.md#address-and-geo-location).

The entrance and exit are only for pools whose access differs from their address, e.g. a car park.


## Data licenses

`dataLicenses`: the licenses of the pool's data, complete (`@id`, `description`, `URLs`), in the ordinal order of
their identifications, each at most once; when read, `dataLicenseIds` may name well-known licenses by their
identification alone. Unlike the operator, the pool writes `dataLicenses` only when it has any. Details: see
[ChargingStationOperator](ChargingStationOperator.md#data-licenses).


## Price status

Like charging stations and EVSEs, a charging pool has a price status beside its admin status and status: new prices of charging tariffs of its operator at
runtime, e.g. ad hoc prices following the electricity market, without a commit; runtime data, not content, without
ETags, shared by all versions of the charging pool. There is none until one is reported. Its prices apply to its stations and EVSEs, unless they have their own.

| JSON key | Type | Description |
|---|---|---|
| `prices` | array | Mandatory, possibly empty: `{chargingTariffId, elements}`, each charging tariff of the operator at most once. Its `elements` (as those of a [charging tariff](ChargingTariff.md#elements), at least one) replace those of the tariff; the currency is the tariff's. No prices: the tariffs apply as they are. |

Other keys are refused ([`ChargingPrices`](../WWCP_POI3/DataStructures/POIPriceStatus.cs)); so is a tariff the
operator does not have. Its text, e.g. in status updates, is this JSON, compact, the prices in the order of their
tariffs. The schedule ([`PriceStatusRuntime`](../WWCP_POI3/DataStructures/PriceStatusRuntime.cs)) keeps the last ten
entries; a later timestamp plans a price status, and `OnStatusPlanned` tells of it.

- `PriceStatus`: the current `Timestamped<ChargingPrices>?`, `null` before the first one.
- `SetPriceStatus(priceStatus, timestamp?, dataSource?)`: since the given timestamp or now; a later timestamp
  plans it. A value repeating the one in effect before it is ignored.
- `OnPriceStatusChanged`: `(timestamp, eventTrackingId, chargingPool, oldPriceStatus, newPriceStatus, dataSource)`.
- `ChargingPrices.Effective(chargingPool, chargingStation, evse)`: the prices in effect at an EVSE: for every
  charging tariff, those of the most specific price status naming it, the EVSE's before its station's before its
  pool's.

In a history, it is set and read by `history.RuntimeStatus` with the kind `priceStatus` and the entity type
`ChargingPool`, and pushed on the WebSocket topic `priceStatusUpdates`.


## Runtime status

See [Runtime status](README.md#runtime-status). A pool has an admin status and a status, no energy status; both
initially as of its creation. Its status is set, not derived from its stations.

| | Type | Initial | Predefined values |
|---|---|---|---|
| Admin status | `ChargingPoolAdminStatusType` | `Operational` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` |
| Status | `ChargingPoolStatusType` | `Available` | `unknown`, `unspecified`, `planned`, `inDeployment`, `offline`, `available`, `partialAvailable`, `reserved`, `charging`, `outOfService`, `error`, `removed` |

Events: `OnAdminStatusChanged` (`OnChargingPoolAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnChargingPoolStatusChangedDelegate`), with the old and new timestamped status. In a history,
`history.RuntimeStatus` sets and reads them as entity type `ChargingPool`.


## Validation

- **Identification:** a valid `ChargingPool_Id`, naming the operator that embeds it; unique within the operator.
- **Charging stations:** no null entry, each of the pool's operator, none twice; stations and EVSEs unique within
  the operator. `chargingStationIds` alone are refused.
- **Energy meters:** no null entry, none twice in the pool, and none twice in the roaming network (including the
  meters of grid connection points, stations and EVSEs).
- **Grid connection point:** `gridConnectionPointId` names a grid connection point of the roaming network;
  an embedded `gridConnectionPoint` is refused.
- **Collections:** no null entry and nothing twice in `dataLicenses`, `brands`, `facilities`, `paymentOptions`,
  `uiFeatures`, `features`, `locationLanguages`, `parkingRestrictions`; `images` by URL; `authenticationModes` by
  type; `relatedLocations` by their JSON.
- **Authentication modes:** card types and brands only for `RFID`; `number` exactly for `SMS` and `PhoneCall`;
  `stationCode` only for `SMS`.
- **Brands:** given completely in `brands`, of the pool and of its RFID authentication modes; `brandIds` is refused.
- **References within the network:** what its stations, EVSEs and energy meters name must exist.
- **Removal:** a pool a charging pool group names, or whose stations or EVSEs a group names, can not be removed.
- **Change sets:** `UpdateProperty`/`RemoveProperty` only of the properties marked above; a given parent must be the
  actual one. After every change, the whole operator is read again (`ChargingStationOperator.TryParse`), so all rules
  above hold for the result.


## Example

`ToJSON()` of a pool with one station, as its operator embeds it.

```json
{
  "@id": "DE*GEF*P1",
  "name": { "en": "Jena Paradies" },
  "dataSource": "import-2026-10",
  "address": {
    "street": "Am Paradiesbahnhof",
    "postalCode": "07743",
    "city": { "de": "Jena" },
    "country": "DEU",
    "houseNumber": "1"
  },
  "geoLocation": { "lat": 50.9241, "lng": 11.5859 },
  "parkingType": "PARKING_LOT",
  "accessibility": "FreePubliclyAccessible",
  "hotline": "+49 3641 7654321",
  "gridConnectionPointId": "GCP-0815",
  "chargingStations": [
    {
      "@id": "DE*GEF*S1",
      "EVSEs": [
        { "@id": "DE*GEF*E1*1", "maxPower": 22000,
          "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" }
      ],
      "created": "2026-01-01T00:00:00.000Z",
      "lastChange": "2026-01-01T00:00:00.000Z"
    }
  ],
  "openingTimes": { "24/7": true },
  "energyMix": {
    "energySources": [ [ "Sun", "60 %" ], [ "Wind", "40 %" ] ],
    "environmentalImpacts": [],
    "supplierName": { "de": "Stadtwerke Jena" },
    "productName": { "de": "Ökostrom" }
  },
  "images": [
    { "url": "https://example.org/gef/p1.jpg", "category": "LOCATION", "type": "image/jpeg", "width": 800, "height": 600 }
  ],
  "timeZone": "Europe/Berlin",
  "facilities": [ "CAFE", "TRAIN_STATION" ],
  "paymentOptions": [ "Contract", "CreditCard" ],
  "authenticationModes": [
    { "type": "CreditCard" },
    { "type": "RFID", "cardTypes": [ "MifareClassic", "MifareDESFire" ] }
  ],
  "features": [ "Reservable", "StatusInfoAvailable" ],
  "locationLanguages": [ "de", "en" ],
  "arrivalInstructions": { "en": "Behind the station building" },
  "chargingWhenClosed": true,
  "relatedLocations": [
    { "geoLocation": { "lat": 50.9238, "lng": 11.5866 }, "name": { "en": "Footpath to the platforms" } }
  ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-10-12T12:00:00.000Z"
}
```
