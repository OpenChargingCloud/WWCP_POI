# EnergyMeterManufacturer

An energy meter manufacturer builds energy meters, e.g. the calibrated meters of charging stations. POI keeps a
registry of them, so that energy meters and their templates can name their manufacturer (`manufacturerId`). It is
described by its name, its contact data and data licenses, as a charging station operator is, and may name keys.

Manufacturers are embedded completely in the [RoamingNetwork](RoamingNetwork.md) under
`energyMeterManufacturers`, ordered by identification. They are a registry of the roaming network operator
(RNO): the RNO adds, changes and removes them, signed with one of the network's signing keys.

The identification is an `EnergyMeterManufacturer_Id`, any nonempty text, trimmed, compared case-insensitively,
e.g. `ACME` or `METERS`. It is unique within the roaming network.

| | |
|---|---|
| Class | `EnergyMeterManufacturer` — [source](../WWCP_POI3/RoamingNetwork/EnergyMeterManufacturer/EnergyMeterManufacturer.cs) |
| Identification | `EnergyMeterManufacturer_Id`, e.g. `ACME` |
| Admin status | `EnergyMeterManufacturerAdminStatusType`, initially `operational` |
| Status | `EnergyMeterManufacturerStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `energyMeterManufacturers` |
| Owner | the RNO (signing keys of the roaming network) |
| HTTP | `GET ~/RNs/{RN_Id}/EMMs/{EMM_Id}`, listed by `GET ~/RNs/{RN_Id}/EMMs` |


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
| `dataLicenses` | `DataLicenses` | `ImmutableArray<DataLicense>` | update/remove | The licenses of its data, complete, ordered by identification; omitted when empty. As of a [charging station operator](ChargingStationOperator.md#data-licenses). |
| `address` | `Address` | `Address?` | update/remove | The address of its headquarters, see [address](#address). |
| `logos` | `Logo` | `URL?` | update/remove | Its logo: an array of at most one `{"uri": …}`, as of a [charging station operator](ChargingStationOperator.md#logos). |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | The geographical location of its headquarters, `{"lat", "lng"}`. |
| `telephone` | `Telephone` | `PhoneNumber?` | update/remove | The telephone number of its (sales) office. |
| `eMailAddress` | `EMailAddress` | `EMailAddress?` | update/remove | The e-mail address of its (sales) office, also with a name: `"Example Office <info@example.org>"`. |
| `homepage` | `Homepage` | `URL?` | update/remove | Its homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | The telephone number of its hotline, e.g. `+49 30 1234567`. |
| `termsAndConditions` | `TermsAndConditionsURL` | `URL?` | update/remove | The URL of its terms and conditions. |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update/remove | Its signing keys, as of a party; omitted when none. See [Keys](#keys). |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update/remove | Its identity keys, as of a party; omitted when none. See [Keys](#keys). |

All are optional and omitted when absent. 


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


## Keys

A manufacturer may name `signingKeys` and `identityKeys`, as a party does (see
[ChargingStationOperator](ChargingStationOperator.md#signing-and-identity-keys)), e.g. for what it signs outside
the roaming network. It is no party: they sign no change sets, and the RNO changes them as every property of its
registry. Every identity key a change enters anew signs the change set as well, as the proof that the manufacturer
holds it (SPECIFICATION.md, section 4.1).


## References

Energy meters and their templates name their manufacturer by its identification, `manufacturerId`. A manufacturer
still named can not be removed, and an energy meter can not name an unknown one.


## Change sets

- `RoamingNetworkChange.Add(manufacturer)` — its JSON; `created` and `lastChange` become the change set's `createdAt`
  unless `created` is given.
- `RoamingNetworkChange.Remove(manufacturerId)` — optionally with its complete previous JSON as old value.
- `UpdateProperty(manufacturer, …)` / `RemoveProperty(manufacturer, …)` of `name`, `description`, `dataSource`,
  `dataLicenses`, `address`, `logos`, `geoLocation`, `telephone`, `eMailAddress`, `homepage`, `hotline`,
  `termsAndConditions`, `signingKeys`, `identityKeys`, `customData`.
- A given parent must be the roaming network itself. Signed with a signing key of the RNO.
- Admin status, status and internal data go along to the changed version.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `EnergyMeterManufacturer`.

- **Admin status** `EnergyMeterManufacturerAdminStatusType`: `unknown`, `unspecified`, `planned`, `inDeployment`,
  `operational`, `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `EnergyMeterManufacturerStatusType`: `unknown`, `unspecified`, `offline`, `inDeployment`,
  `available`, `error`. Initially `available` since `created`.
- No energy status.
- Events: `OnAdminStatusChanged` (`OnEnergyMeterManufacturerAdminStatusChangedDelegate`) and `OnStatusChanged`
  (`OnEnergyMeterManufacturerStatusChangedDelegate`).


## Validation

- `@id` mandatory, a nonempty `EnergyMeterManufacturer_Id`; unique within the roaming network.
- `address`, when given, a JSON object with the mandatory address fields; `homepage`, `termsAndConditions` and the
  `uri` of its logo URLs; `telephone` and `hotline` phone numbers; `eMailAddress` an e-mail address; `logos` an
  array of at most one logo; `dataLicenses` licenses given completely, none twice; keys as of a party.
- Network level: every manufacturer an energy meter names is one of the network.


## Example

```json
{
  "@id": "ACME",
  "name": { "en": "ACME Meters" },
  "address": {
    "street": "Musterstraße",
    "postalCode": "07743",
    "city": { "de": "Jena" },
    "country": "DEU",
    "houseNumber": "1"
  },
  "eMailAddress": "ACME Sales <sales@meters.example.org>",
  "homepage": "https://meters.example.org",
  "hotline": "+49 3641 1234567",
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-01-01T00:00:00.000Z"
}
```
