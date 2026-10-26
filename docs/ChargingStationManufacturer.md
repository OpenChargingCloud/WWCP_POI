# ChargingStationManufacturer

A charging station manufacturer builds charging stations, e.g. wallboxes or DC chargers. In POI 3 it is a **party**:
the roaming network operator (RNO) adds and removes it, with its keys; it changes its own properties and its
**models** with its own keys. A charging station and a charging station template name their manufacturer
(`manufacturerId`), their model (`modelId`) and its hardware revision and firmware version (`hardwareRevision`,
`firmwareVersion`), a certificate of a charging station model the manufacturer, the model and the hardware revisions
and firmware versions of the model it was issued for. It is described by its name, its contact data and data licenses, as a
charging station operator is.

The identification is a `ChargingStationManufacturer_Id`, any nonempty text, trimmed, compared case-insensitively,
e.g. `GraphDefined` or `ACME`. It is unique within the roaming network.

| | |
|---|---|
| Class | `ChargingStationManufacturer` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStationManufacturer.cs) |
| Identification | `ChargingStationManufacturer_Id`, e.g. `ACME` |
| Admin status | `ChargingStationManufacturerAdminStatusType`, initially `operational` |
| Status | `ChargingStationManufacturerStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `chargingStationManufacturers` |
| Owner | itself (signing key); its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO; added and removed by the RNO |
| Embeds | its models, `chargingStationModels` |
| HTTP | `GET ~/RNs/{RN_Id}/CSMs/{CSM_Id}`, with its models (`?without=chargingStationModels` leaves them out), listed by `GET ~/RNs/{RN_Id}/CSMs`; its models `GET ~/RNs/{RN_Id}/CSMs/{CSM_Id}/models[/{Model_Id}]` |


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
| `chargingStationModels` | `Models` | [ChargingStationModel](#chargingstationmodels)[] | add/remove, each on its own | Its models, ordered by `@id` (ordinal); omitted when none. |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update | Its signing keys, see [Keys](#keys). |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update | Its identity keys, see [Keys](#keys). |

All but the keys are optional and omitted when absent. `chargingStationModelIds` is refused: its models are embedded
completely.


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


## chargingStationModels

`ChargingStationModel` ([source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStationModel/ChargingStationModel.cs)),
context `https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingStationModel/v1`: a model of charging
stations, as its manufacturer states it, e.g. `GD-DC300`. Without runtime status and timestamps; its manufacturer,
which embeds it, names who made it, so it names none (`manufacturerId` is refused). Its technical data are the
ratings of the model; a charging station of it may have less, its EVSEs say what they have.

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | string | — | `ChargingStationModel_Id`: any nonempty text, trimmed, compared case-insensitively; unique within its manufacturer, so that two manufacturers may have models of the same identification. |
| `name` | I18N text | update/remove | Its name. |
| `description` | I18N text | update/remove | |
| `hardwareRevisions` | string[] | update/remove | The hardware revisions it was built in, each once, none empty or beginning or ending with whitespace, in their given order. A station names one of them, a certificate those it covers. |
| `firmwareVersions` | string[] | update/remove | The firmware versions it ran, as `hardwareRevisions`. A station names one of them, a certificate those it covers. |
| `productURL` | URL | update/remove | Its product page. |
| `datasheetURL` | URL | update/remove | Its datasheet. |
| `images` | object[] | update/remove | Images of it, as of a charging station; each URL once. |
| `maxPower` | number | update/remove | The maximum power of a charging station of it, in W; positive. |
| `currentTypes` | string[] | update/remove | Its current types: `AC_OnePhase`, `AC_ThreePhases`, `DC`. |
| `connectorTypes` | string[] | update/remove | The types of its charging connectors, `ChargingConnectorType`s, e.g. `IEC_62196_T2_COMBO`; ordered by identification, each once. |
| `numberOfEVSEs` | integer | update/remove | The number of EVSEs of a charging station of it; at least 1. |
| `customData` | object | update/remove | |

- `RoamingNetworkChange.Add(model, manufacturerId)`, `Remove(modelId, manufacturerId)`,
  `UpdateProperty(model, manufacturerId, …)`/`RemoveProperty(model, manufacturerId, …)`: every change names its
  manufacturer as its parent, as its identification is unique within it only; one without is refused. Signed by the
  manufacturer, with a signing key that may sign `models`.
- A model still named by a charging station, a charging station template or a certificate can not be removed, nor its
  manufacturer; nor a hardware revision or firmware version of it one of them names. `versions`, one list of before,
  is no property of a model.


## Keys

Signing and identity keys as of every party (see [ChargingStationOperator](ChargingStationOperator.md#signing-and-identity-keys)):
it is added with at least one of each, every new identity key signing the change set as the proof that the
manufacturer holds it, and keeps at least one of each. A signing key naming `usages` signs its models only when it
names `models`, its own properties only when it names `party` (SPECIFICATION.md, section 4.1).


## References

| Who | JSON key | Rule |
|---|---|---|
| [ChargingStation](ChargingStation.md) | `manufacturerId`, `modelId`, `hardwareRevision`, `firmwareVersion` | `manufacturerId` names a manufacturer of the network; `modelId` a model of it, and only with its manufacturer; `hardwareRevision` one of the `hardwareRevisions` of the model, `firmwareVersion` one of its `firmwareVersions`, each only with the model: as the station is at runtime, with what its templates give it. |
| [ChargingStationTemplate](ChargingStationTemplate.md) | `manufacturerId`, `modelId`, `hardwareRevision`, `firmwareVersion` | the same, of the template; a hardware revision or firmware version without a model it gives to stations of their own model, checked at them. |
| certificate of a charging station model ([ConformityAssessmentBody](ConformityAssessmentBody.md#certificates)) | `chargingStationManufacturerId`, `chargingStationModel`, `hardwareRevisions`, `firmwareVersions` | the manufacturer, mandatory, names a manufacturer of the network, the model one of its models, each hardware revision one of its `hardwareRevisions`, each firmware version one of its `firmwareVersions`. |

The references are checked after every change set: a manufacturer, a model, a hardware revision or a firmware version
still named can not be removed, and a station, template or certificate can not name an unknown one.


## Change sets

- `RoamingNetworkChange.Add(manufacturer)` — signed by the RNO, with its keys and without its models: they are its own
  to add. `created` and `lastChange` become the change set's `createdAt` unless `created` is given.
- `RoamingNetworkChange.Remove(manufacturerId)` — signed by the RNO; its models go with it.
- `UpdateProperty(manufacturer, …)` / `RemoveProperty(manufacturer, …)` of `name`, `description`, `dataSource`,
  `dataLicenses`, `address`, `logos`, `geoLocation`, `telephone`, `eMailAddress`, `homepage`, `hotline`,
  `termsAndConditions`, `customData` — signed by the manufacturer; `signingKeys` and `identityKeys` as of every party.
  `chargingStationModels` is no property: its models are added, changed and removed on their own.
- A given parent must be the roaming network itself.
- Admin status, status and internal data go along to the changed version.


## Effective tree

A charging station manufacturer is a node of the effective tree (SPECIFICATION.md, section 8) with the child array
`chargingStationModels`; without models, its node is the one of its content alone. The proof of a charging station
gives its manufacturer with the model the station names, and prunes its other models.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `ChargingStationManufacturer`.

- **Admin status** `ChargingStationManufacturerAdminStatusType`: `unknown`, `unspecified`, `planned`, `inDeployment`,
  `operational`, `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `ChargingStationManufacturerStatusType`: `unknown`, `unspecified`, `offline`, `inDeployment`,
  `available`, `error`. Initially `available` since `created`.
- No energy status. Models have none.
- Events: `OnAdminStatusChanged` (`OnChargingStationManufacturerAdminStatusChangedDelegate`) and `OnStatusChanged`
  (`OnChargingStationManufacturerStatusChangedDelegate`).


## Validation

- `@id` mandatory, a nonempty `ChargingStationManufacturer_Id`; unique within the roaming network.
- `address`, when given, a JSON object with the mandatory address fields; `homepage`, `termsAndConditions` and the
  `uri` of its logo URLs; `telephone` and `hotline` phone numbers; `eMailAddress` an e-mail address; `logos` an
  array of at most one logo; `dataLicenses` licenses given completely, none twice; keys as of a party; a model `@id`
  only once.
- Network level: every manufacturer, model, hardware revision and firmware version a charging station, a charging
  station template or a certificate names is one of the network, see [References](#references).


## Example

```json
{
  "@id": "GraphDefined",
  "name": { "en": "GraphDefined Charging Stations" },
  "homepage": "https://stations.graphdefined.example",
  "chargingStationModels": [
    {
      "@id": "GD-DC300",
      "name": { "en": "GraphDefined DC 300" },
      "hardwareRevisions": [ "1.0", "1.1" ],
      "firmwareVersions": [ "2.4.0", "2.5.0" ],
      "datasheetURL": "https://stations.graphdefined.example/gd-dc300/datasheet.pdf",
      "maxPower": 300000,
      "currentTypes": [ "DC" ],
      "connectorTypes": [ "IEC_62196_T2_COMBO" ],
      "numberOfEVSEs": 2
    }
  ],
  "signingKeys":  [ { "@id": "…", "key": "…", "usages": [ "models" ] }, { "@id": "…", "key": "…", "usages": [ "party" ] } ],
  "identityKeys": [ { "@id": "…", "key": "…" } ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-01-01T00:00:00.000Z"
}
```
