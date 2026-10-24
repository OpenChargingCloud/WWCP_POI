# EnergyMeterManufacturer

An energy meter manufacturer builds energy meters, e.g. the calibrated meters of charging stations. In POI 3 it is a
**party**: the roaming network operator (RNO) adds and removes it, with its keys; it changes its own properties and
its **models** with its own keys. An energy meter and an energy meter template name their manufacturer
(`manufacturerId`) and their model (`modelId`). It is described by its name, its contact data and data licenses, as a
charging station operator is.

The identification is an `EnergyMeterManufacturer_Id`, any nonempty text, trimmed, compared case-insensitively,
e.g. `ACME` or `METERS`. It is unique within the roaming network.

| | |
|---|---|
| Class | `EnergyMeterManufacturer` — [source](../WWCP_POI3/RoamingNetwork/EnergyMeterManufacturer/EnergyMeterManufacturer.cs) |
| Identification | `EnergyMeterManufacturer_Id`, e.g. `ACME` |
| Admin status | `EnergyMeterManufacturerAdminStatusType`, initially `operational` |
| Status | `EnergyMeterManufacturerStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `energyMeterManufacturers` |
| Owner | itself (signing key); its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO; added and removed by the RNO |
| Embeds | its models, `energyMeterModels` |
| HTTP | `GET ~/RNs/{RN_Id}/EMMs/{EMM_Id}`, with its models (`?without=energyMeterModels` leaves them out), listed by `GET ~/RNs/{RN_Id}/EMMs`; its models `GET ~/RNs/{RN_Id}/EMMs/{EMM_Id}/models[/{Model_Id}]` |


## Common properties

See [README.md](README.md#common-properties). No `eTags`.

| JSON key | Change set | Notes |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | I18N text; omitted when empty |
| `description` | update/remove | I18N text; omitted when empty |
| `dataSource` | update/remove | |
| `created` | — | set to the `createdAt` of the change set when added, unless given |
| `lastChange` | — | set to the `createdAt` of the change set by every change of it and of its models |
| `customData` | update/remove | a JSON object; omitted when empty |


## Properties

The contact data, data licenses and keys of a [charging station manufacturer](ChargingStationManufacturer.md#properties):
`dataLicenses`, `address`, `logos`, `geoLocation`, `telephone`, `eMailAddress`, `homepage`, `hotline`,
`termsAndConditions`, `signingKeys`, `identityKeys`; and

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `energyMeterModels` | `Models` | [EnergyMeterModel](#energymetermodels)[] | add/remove, each on its own | Its models, ordered by `@id` (ordinal); omitted when none. |

All but the keys are optional and omitted when absent. `energyMeterModelIds` is refused: its models are embedded
completely.


## energyMeterModels

`EnergyMeterModel` ([source](../WWCP_POI3/RoamingNetwork/EnergyMeterManufacturer/EnergyMeterModel/EnergyMeterModel.cs)),
context `https://open.charging.cloud/contexts/wwcp/roamingNetwork/energyMeterModel/v1`: a model of energy meters, as
its manufacturer states it, e.g. `KDM-500`. Without runtime status and timestamps; its manufacturer, which embeds it,
names who made it, so it names none (`manufacturerId` is refused).

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | string | — | `EnergyMeterModel_Id`: any nonempty text, trimmed, compared case-insensitively; unique within its manufacturer. |
| `name` | I18N text | update/remove | Its name. |
| `description` | I18N text | update/remove | |
| `versions` | string[] | update/remove | Its versions, e.g. hardware or firmware versions, each once, none empty or beginning or ending with whitespace, in their given order. |
| `productURL` | URL | update/remove | Its product page. |
| `datasheetURL` | URL | update/remove | Its datasheet. |
| `images` | object[] | update/remove | Images of it; each URL once. |
| `currentTypes` | string[] | update/remove | Its current types: `AC_OnePhase`, `AC_ThreePhases`, `DC`. |
| `maxCurrent` | number | update/remove | The maximum current it measures, in A; positive. |
| `accuracyClass` | string | update/remove | The accuracy class of its type examination, e.g. `A`, `B` or `C` of the Measuring Instruments Directive (2014/32/EU, Annex V); not empty. |
| `measurands` | string[] | update/remove | What it measures, e.g. OBIS codes as `1-0:1.8.0`; each once, ordered ordinally. |
| `signatureMethods` | string[] | update/remove | How its meters sign their measurements, as a transparency software verifies them, e.g. `OCMF`; as of an [energy meter](EnergyMeter.md). |
| `customData` | object | update/remove | |

- `RoamingNetworkChange.Add(model, manufacturerId)`, `Remove(modelId, manufacturerId)`,
  `UpdateProperty(model, manufacturerId, …)`/`RemoveProperty(model, manufacturerId, …)`: every change names its
  manufacturer as its parent, as its identification is unique within it only; one without is refused. Signed by the
  manufacturer, with a signing key that may sign `models`.
- A model still named by an energy meter or an energy meter template can not be removed, nor its manufacturer.


## Keys

As of a [charging station manufacturer](ChargingStationManufacturer.md#keys): added with at least one signing and one
identity key, every new identity key signing the change set; a signing key naming `usages` signs its models only when
it names `models`.


## References

| Who | JSON key | Rule |
|---|---|---|
| [EnergyMeter](EnergyMeter.md) | `manufacturerId`, `modelId` | `manufacturerId` names a manufacturer of the network; `modelId` a model of it, and only with its manufacturer: as the meter is at runtime, with what its templates give it. |
| [EnergyMeterTemplate](EnergyMeterTemplate.md) | `manufacturerId`, `modelId` | the same, of the template. |

The references are checked after every change set: a manufacturer or a model still named can not be removed, and a
meter or template can not name an unknown one.


## Change sets

As of a [charging station manufacturer](ChargingStationManufacturer.md#change-sets): added and removed by the RNO, with
its keys and without its models; its own properties changed by itself; `energyMeterModels` is no property, its models
are added, changed and removed on their own. Admin status, status and internal data go along to the changed version.


## Effective tree

An energy meter manufacturer is a node of the effective tree (SPECIFICATION.md, section 8) with the child array
`energyMeterModels`. The proof of a charging station gives the manufacturers of its energy meters with the models they
name, and prunes their other models.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `EnergyMeterManufacturer`.

- **Admin status** `EnergyMeterManufacturerAdminStatusType`: `unknown`, `unspecified`, `planned`, `inDeployment`,
  `operational`, `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `EnergyMeterManufacturerStatusType`: `unknown`, `unspecified`, `offline`, `inDeployment`,
  `available`, `error`. Initially `available` since `created`.
- No energy status. Models have none.
- Events: `OnAdminStatusChanged` (`OnEnergyMeterManufacturerAdminStatusChangedDelegate`) and `OnStatusChanged`
  (`OnEnergyMeterManufacturerStatusChangedDelegate`).


## Validation

- `@id` mandatory, a nonempty `EnergyMeterManufacturer_Id`; unique within the roaming network.
- The contact data as of a charging station manufacturer; keys as of a party; a model `@id` only once.
- Network level: every manufacturer and model an energy meter or an energy meter template names is one of the network,
  see [References](#references).


## Example

```json
{
  "@id": "KERNBERG",
  "name": { "en": "Kernberg DC Metering" },
  "description": { "en": "Calibrated DC energy meters" },
  "homepage": "https://kernberg-metering.example",
  "energyMeterModels": [
    {
      "@id": "KDM-500",
      "name": { "en": "Kernberg DC meter 500 A" },
      "versions": [ "2.1" ],
      "currentTypes": [ "DC" ],
      "maxCurrent": 500,
      "accuracyClass": "B",
      "measurands": [ "1-0:1.8.0", "1-0:2.8.0" ],
      "signatureMethods": [ "OCMF" ]
    }
  ],
  "signingKeys":  [ { "@id": "…", "key": "…", "usages": [ "models" ] }, { "@id": "…", "key": "…", "usages": [ "party" ] } ],
  "identityKeys": [ { "@id": "…", "key": "…" } ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-01-01T00:00:00.000Z"
}
```
