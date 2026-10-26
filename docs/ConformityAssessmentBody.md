# ConformityAssessmentBody

A conformity assessment body assesses products against legislation and issues certificates, e.g. a national metrology
institute examining charging stations and energy meters under the Measuring Instruments Directive. In POI 3 it is a
**party**: the roaming network operator (RNO) adds and removes it; it changes its own properties and the
**certificates** it issued, embedded in it, with its own keys. A certificate is for a model of charging stations or of
energy meters and names the components it was issued with, the transparency software among them; charging stations and
energy meters name the certificates of their models by their identifications.

It names its **number as a notified body**, as the NANDO database of the European Commission gives it, and the
**legislations it is a notified body for** (`notifiedFor`), e.g. `MID` for the Measuring Instruments Directive
2014/32/EU or `CRA` for the Cyber Resilience Act, Regulation (EU) 2024/2847. A certificate issued under one of them
names it as its `legislation`; one the body is not notified for is refused.

**What a signature says.** A certificate signed by the body is the body's statement, as the CRA properties of a
[TransparencySoftwareManufacturer](TransparencySoftwareManufacturer.md) are the manufacturer's: POI proves who says
it, not that it is true; whoever doubts it asks the body, e.g. by its `documentURL`. A body not taking part (yet) is
added **without keys**: then the RNO adds, changes and removes its certificates in its place, with a signing key that
may sign `party`, as its keys do, or `certificates`, and they are statements of the RNO, not of the body, until the RNO
gives the body keys with one of its identity keys. They say so: their `statedBy` is `roamingNetworkOperator`, by
which a reader of a proof tells them apart, also once the body has keys; the body changes one of them only by removing
it, stating it itself (SPECIFICATION.md, 4.1).

The identification is a `ConformityAssessmentBody_Id`, any nonempty text, trimmed, compared case-insensitively, e.g.
`PTB` or `BSI`. It is unique within the roaming network.

| | |
|---|---|
| Class | `ConformityAssessmentBody` — [source](../WWCP_POI3/RoamingNetwork/ConformityAssessmentBody/ConformityAssessmentBody.cs) |
| Identification | `ConformityAssessmentBody_Id`, e.g. `PTB` |
| Admin status | `ConformityAssessmentBodyAdminStatusType`, initially `operational` |
| Status | `ConformityAssessmentBodyStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `conformityAssessmentBodies` |
| Owner | itself (signing key), or the RNO while it has no signing keys; its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO; added and removed by the RNO |
| Embeds | its certificates, `certificates` |
| HTTP | `GET ~/RNs/{RN_Id}/CABs/{CAB_Id}`, with its certificates (`?without=certificates` leaves them out), listed by `GET ~/RNs/{RN_Id}/CABs`; its certificates `GET ~/RNs/{RN_Id}/CABs/{CAB_Id}/certificates[/{Certificate_Id}]` |


## Common properties

See [README.md](README.md#common-properties). No `eTags`.

| JSON key | Change set | Notes |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | I18N text; omitted when empty |
| `description` | update/remove | I18N text; omitted when empty |
| `dataSource` | update/remove | |
| `created` | — | set to the `createdAt` of the change set when added, unless given |
| `lastChange` | — | set to the `createdAt` of the change set by every change of it and of its certificates |
| `customData` | update/remove | a JSON object; omitted when empty |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `dataLicenses` | `DataLicenses` | `ImmutableArray<DataLicense>` | update/remove | The licenses of its data, as of an [energy meter manufacturer](EnergyMeterManufacturer.md). |
| `address` | `Address` | `Address?` | update/remove | Its address, see [EnergyMeterManufacturer](EnergyMeterManufacturer.md#address). |
| `logos` | `Logo` | `URL?` | update/remove | Its logo: an array of at most one `{"uri": …}`. |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | The geographical location of its headquarters. |
| `telephone` | `Telephone` | `PhoneNumber?` | update/remove | The telephone number of its office. |
| `eMailAddress` | `EMailAddress` | `EMailAddress?` | update/remove | The e-mail address of its office. |
| `homepage` | `Homepage` | `URL?` | update/remove | Its homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | The telephone number of its hotline. |
| `termsAndConditions` | `TermsAndConditionsURL` | `URL?` | update/remove | The URL of its terms and conditions. |
| `notifiedBodyNumber` | `NotifiedBodyNumber` | `String?` | update/remove | Its number as a notified body: four digits, e.g. `0102`. |
| `notifiedFor` | `NotifiedFor` | `ImmutableArray<String>` | update/remove | The legislations it is a notified body for, each once, in their ordinal order; omitted when none. `MID` and `CRA` (`ConformityAssessmentBody.Legislations`) are well known, any other nonempty text names another one. |
| `certificates` | `Certificates` | [ConformityCertificate](#certificates)[] | add/remove, each on its own | The certificates it issued, ordered by `@id` (ordinal); omitted when none. |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update | Its signing keys, see [Keys](#keys); none while it does not take part. |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update | Its identity keys, see [Keys](#keys); none while it does not take part. |

All are optional and omitted when absent.


## certificates

`ConformityCertificate` ([source](../WWCP_POI2/RoamingNetwork/ConformityAssessmentBody/ConformityCertificate.cs)),
context `https://open.charging.cloud/contexts/wwcp/roamingNetwork/certificate/v1`, change sets name it as the entity
type `ConformityCertificate`: a certificate of conformity for **one model**, of charging stations or of energy meters,
in the hardware revisions and firmware versions it covers, naming the **components** it was issued with. The
transparency software is one, a product of its own manufacturer, which the certification of a station needs verified
as that of a meter does; of a charging station model, the energy meters it mounts are others. A domain document, not a
cryptographic certificate. Without runtime status, timestamps and custom data; the body embedding it is its issuer.
Read strictly: an unknown property is an error, also the `issuer` and the `chargingStationModelVersion` of earlier
versions.

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | string | — | `ConformityCertificate_Id`: any text, compared case-insensitively, surrounding whitespace trimmed; unique within the roaming network. |
| `legislation` | string | update/remove | The legislation it is issued under, e.g. `MID`: one its body is `notifiedFor`. |
| `chargingStationManufacturerId` | string | update | Of a certificate for a charging station model: a [ChargingStationManufacturer](ChargingStationManufacturer.md) of the network, the manufacturer of the model. |
| `chargingStationModel` | string | update | Of a certificate for a charging station model: a `ChargingStationModel_Id`, one of the [models](ChargingStationManufacturer.md#chargingstationmodels) of the manufacturer, as charging stations name their model by `modelId`, compared case-insensitively. |
| `energyMeterManufacturerId` | string | update | Of a certificate for an energy meter model: an [EnergyMeterManufacturer](EnergyMeterManufacturer.md) of the network. |
| `energyMeterModel` | string | update | Of a certificate for an energy meter model: an `EnergyMeterModel_Id`, one of the [models](EnergyMeterManufacturer.md#energymetermodels) of the manufacturer, as energy meters name their model by `modelId`. |
| `hardwareRevisions` | string[] | update | Mandatory, at least one: the hardware revisions of the model it covers, each one of the `hardwareRevisions` of the model, compared ordinally; each once, in their given order. |
| `firmwareVersions` | string[] | update | Mandatory, at least one: the firmware versions of the model it covers, each one of its `firmwareVersions`, as `hardwareRevisions`; an amendment adds one by an update of its body. |
| `verifiedTransparencySoftwareIds` | string[] | update/remove | Components: the releases officially checked with this model; always written, ordered (ordinal). |
| `compatibleTransparencySoftwareIds` | string[] | update/remove | Components: further compatible releases, without a claim of verification; always written, ordered (ordinal). |
| `energyMeters` | object[] | update/remove | Components of a charging station model: the energy meters it was issued with, each `{ manufacturerId, modelId, hardwareRevisions, firmwareVersions }`, a model of an energy meter manufacturer of the network in at least one of its hardware revisions and firmware versions; each model once, in their given order; omitted when none. A certificate for an energy meter model names none. |
| `documentNumber` | string | update/remove | The body's number of the document, if it differs from `@id`. |
| `documentURL` | URL | update/remove | Absolute; where the body publishes the document. |
| `notBefore` | timestamp | update/remove | Start of validity, in UTC to the millisecond like every timestamp of the content, e.g. `2026-01-01T00:00:00.000Z`. |
| `notAfter` | timestamp | update/remove | End of validity; not before `notBefore`. |
| `statedBy` | string | update/remove | `roamingNetworkOperator` where the RNO adds or changes it in place of the body without signing keys; omitted where the body signs it (`StatedByRoamingNetworkOperator`). A change set adding or changing a certificate is refused unless it says who signs it. |

- For exactly one model: `chargingStationManufacturerId` and `chargingStationModel`, or `energyMeterManufacturerId` and
  `energyMeterModel`, each with its manufacturer.
- At least one release in both lists together; a release not in both, not twice, not empty. Every release named is one
  of a [TransparencySoftwareManufacturer](TransparencySoftwareManufacturer.md#transparencysoftware) of the network.
- A [charging station](ChargingStation.md) names the certificates of its model in its `certificateIds`, an
  [energy meter](EnergyMeter.md) those of its model in its; each covers the hardware revision and the firmware version
  of what names it. Where a certificate of a charging station model names `energyMeters`, every energy meter of a
  station naming it is one of them, of its model, revision and firmware. The `transparencySoftwareStatus` of a meter
  names no certificate: a certificate of the meter or of its station naming the release among its components proves
  its legal status.
- `RoamingNetworkChange.Add(certificate, bodyId)`: the parent of the `Add` is its body, which signs it; the RNO in place
  of a body without signing keys, the certificate made with `StatedByRoamingNetworkOperator: true`.
- `RoamingNetworkChange.Remove(certificateId)`, `UpdateProperty(certificate, …)`/`RemoveProperty(certificate, …)`:
  signed by the body embedding it, or the RNO in its place; a change naming another parent is refused.
- A certificate still named by a charging station, an energy meter or one of their templates can not be removed, nor
  its body. Two bodies can not have certificates of the same identification.


## Keys

Signing and identity keys as of every party (see [ChargingStationOperator](ChargingStationOperator.md#signing-and-identity-keys)),
or none at all while it does not take part, as a [GridOperator](GridOperator.md) may. With keys, every new identity key
signs the change set as the proof that the body holds it, and it keeps at least one of each. A signing key naming
`usages` signs its certificates only when it names `certificates`, its own properties only when it names `party`
(SPECIFICATION.md, section 4.1).


## Change sets

- `RoamingNetworkChange.Add(body)` — signed by the RNO, with its keys or without any, and without its certificates:
  they are its own to add. `created` and `lastChange` become the change set's `createdAt` unless `created` is given.
- `RoamingNetworkChange.Remove(bodyId)` — signed by the RNO; its certificates go with it.
- `UpdateProperty(body, …)` / `RemoveProperty(body, …)` of `name`, `description`, `dataSource`, `dataLicenses`,
  `address`, `logos`, `geoLocation`, `telephone`, `eMailAddress`, `homepage`, `hotline`, `termsAndConditions`,
  `notifiedBodyNumber`, `notifiedFor`, `customData` — signed by the body, or the RNO in its place; `signingKeys` and
  `identityKeys` as of every party, or by an identity key of the RNO, which gives a body not taking part its keys.
  `certificates` is no property: its certificates are added, changed and removed on their own.
- Admin status, status and internal data go along to the changed version.


## Effective tree

A conformity assessment body is a node of the effective tree (SPECIFICATION.md, section 8) with the child array
`certificates`. The proof of a charging station gives the certificates the station and its energy meters name, in the
skeletons of their bodies, and prunes the other certificates.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `ConformityAssessmentBody`.

- **Admin status** `ConformityAssessmentBodyAdminStatusType`: `unknown`, `unspecified`, `planned`, `inDeployment`,
  `operational`, `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `ConformityAssessmentBodyStatusType`: `unknown`, `unspecified`, `offline`, `inDeployment`, `available`,
  `error`. Initially `available` since `created`.
- Certificates have none.


## Example

```json
{
  "@id": "PTB",
  "name": { "de": "Physikalisch-Technische Bundesanstalt", "en": "National Metrology Institute of Germany" },
  "homepage": "https://www.ptb.de",
  "notifiedBodyNumber": "0102",
  "notifiedFor": [ "MID" ],
  "certificates": [
    {
      "@id": "PTB-2026-0017",
      "energyMeterManufacturerId": "ACME",
      "energyMeterModel": "ACME-3000",
      "hardwareRevisions": [ "1.2" ],
      "firmwareVersions": [ "4.7.1" ],
      "verifiedTransparencySoftwareIds":   [ "chargy-1.5.0" ],
      "compatibleTransparencySoftwareIds": [],
      "notBefore": "2026-01-01T00:00:00.000Z"
    },
    {
      "@id": "PTB-2026-0042",
      "legislation": "MID",
      "chargingStationManufacturerId": "ACME",
      "chargingStationModel": "Wallbox 22",
      "hardwareRevisions": [ "2.1" ],
      "firmwareVersions": [ "4.7.1" ],
      "verifiedTransparencySoftwareIds":   [ "chargy-1.5.0" ],
      "compatibleTransparencySoftwareIds": [],
      "energyMeters": [
        { "manufacturerId": "ACME", "modelId": "ACME-3000", "hardwareRevisions": [ "1.2" ], "firmwareVersions": [ "4.7.1" ] }
      ],
      "notBefore": "2026-01-01T00:00:00.000Z"
    }
  ],
  "signingKeys":  [ { "@id": "…", "key": "…" } ],
  "identityKeys": [ { "@id": "…", "key": "…" } ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-01-01T00:00:00.000Z"
}
```

The numbers and identifications are examples, not those of real bodies and documents.
