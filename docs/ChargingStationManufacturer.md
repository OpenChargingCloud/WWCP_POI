# ChargingStationManufacturer

A charging station manufacturer builds charging stations, e.g. wallboxes or DC chargers. POI keeps a registry of
them, so that charging stations of every operator can name their manufacturer (`manufacturerId`, with a `modelId`)
and transparency software certificates the manufacturer they were issued for. It is described by its name,
headquarters address, homepage and hotline.

Manufacturers are embedded completely in the [RoamingNetwork](RoamingNetwork.md) under
`chargingStationManufacturers`, ordered by identification. They are a registry of the roaming network operator
(RNO): the RNO adds, changes and removes them, signed with one of the network's signing keys.

The identification is a `ChargingStationManufacturer_Id`, any nonempty text, trimmed, compared case-insensitively,
e.g. `GraphDefined` or `ACME`. It is unique within the roaming network.

| | |
|---|---|
| Class | `ChargingStationManufacturer` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStationManufacturer.cs) |
| Identification | `ChargingStationManufacturer_Id`, e.g. `ACME` |
| Admin status | `ChargingStationManufacturerAdminStatusType`, initially `operational` |
| Status | `ChargingStationManufacturerStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `chargingStationManufacturers` |
| Owner | the RNO (signing keys of the roaming network) |
| HTTP | `GET ~/RNs/{RN_Id}/CSMs/{CSM_Id}` |


## Common properties

See [README.md](README.md#common-properties). No `eTags`.

| JSON key | Change set | Notes |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | I18N text; omitted when empty |
| `description` | update/remove | I18N text; omitted when empty |
| `dataSource` | update/remove | |
| `created` | — | set to the `createdAt` of the change set when added, unless given |
| `lastChange` | — | set to the `createdAt` of the change set by every change |
| `customData` | update/remove | a JSON object; omitted when empty |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `address` | `Address` | [Address](#address) | update/remove | The address of the headquarters. |
| `homepage` | `Homepage` | URL | update/remove | |
| `hotline` | `HotlinePhoneNumber` | string | update/remove | The hotline phone number, e.g. `+49 30 1234567`. |

All are optional and omitted when absent. A manufacturer has no list of models: the `modelId` of a charging
station is not checked against anything.


## address

Styx's `Address`, written embedded (without `@context`):

| JSON key | Type | Description |
|---|---|---|
| `street` | string | Mandatory. |
| `postalCode` | string | Mandatory. |
| `city` | I18N text | Mandatory. |
| `country` | string | Mandatory, ISO 3166-1 alpha-3, e.g. `DEU`. |
| `houseNumber` | string | Optional. |
| `floorLevel` | string | Optional. |
| `region` | string | Optional. |
| `postalCodeSub` | string | Optional. |
| `timeZone` | string | Optional. Written as `timeZone`, but Styx reads `TimeZone`: avoid it, it does not survive a round trip. |
| `officialLanguages` | string[] | Optional. |
| `comment` | I18N text | Optional. |
| `customData` | object | Optional. |

The address is replaced or removed as a whole: `UpdateProperty(manufacturer, "address", old, newAddress)`.


## References

| Who | JSON key | Rule |
|---|---|---|
| [ChargingStation](ChargingStation.md) | `manufacturerId`, `modelId` | `manufacturerId` must name a manufacturer of the network; `modelId` (`ChargingStationModel_Id`) is free text. |
| transparency software certificate ([RoamingNetwork](RoamingNetwork.md#transparencysoftwarecertificates)) | `chargingStationManufacturerId` | must name a manufacturer of the network. |

The references are checked after every change set: a manufacturer still named can not be removed, and a station
or certificate can not name an unknown one.


## Change sets

- `RoamingNetworkChange.Add(manufacturer)` — its JSON; `created` and `lastChange` become the change set's `createdAt`
  unless `created` is given.
- `RoamingNetworkChange.Remove(manufacturerId)` — optionally with its complete previous JSON as old value.
- `UpdateProperty(manufacturer, …)` / `RemoveProperty(manufacturer, …)` of `name`, `description`, `dataSource`,
  `address`, `homepage`, `hotline`, `customData`.
- A given parent must be the roaming network itself. Signed with a signing key of the RNO.
- Admin status, status and internal data go along to the changed version.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `ChargingStationManufacturer`.

- **Admin status** `ChargingStationManufacturerAdminStatusType`: `unknown`, `unspecified`, `planned`, `inDeployment`,
  `operational`, `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `ChargingStationManufacturerStatusType`: `unknown`, `unspecified`, `offline`, `inDeployment`,
  `available`, `error`. Initially `available` since `created`.
- No energy status.
- Events: `OnAdminStatusChanged` (`OnChargingStationManufacturerAdminStatusChangedDelegate`) and `OnStatusChanged`
  (`OnChargingStationManufacturerStatusChangedDelegate`).


## Validation

- `@id` mandatory, a nonempty `ChargingStationManufacturer_Id`; unique within the roaming network.
- `address`, when given, a JSON object with the mandatory address fields; `homepage` a URL; `hotline` a phone number.
- Network level: every `manufacturerId` of a charging station and every `chargingStationManufacturerId` of a
  certificate names a manufacturer of the network.


## Example

```json
{
  "@id": "ACME",
  "name": { "en": "ACME Charging" },
  "address": {
    "street": "Musterstraße",
    "postalCode": "07743",
    "city": { "de": "Jena" },
    "country": "DEU",
    "houseNumber": "1"
  },
  "homepage": "https://charging.example.org",
  "hotline": "+49 3641 1234567",
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-01-01T00:00:00.000Z"
}
```
