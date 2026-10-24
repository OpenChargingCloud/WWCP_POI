# TransparencySoftwareManufacturer

A transparency software manufacturer makes transparency software, e.g. the software with which a driver checks the
signed meter values of a charging session under the German calibration law. In POI 3 it is a **party**: the roaming
network operator (RNO) adds and removes it, with its keys; it changes its own properties and its **releases**, one
per version of a software, with its own keys. Energy meters and transparency software certificates name its releases
by their identifications.

It is the one the EU Cyber Resilience Act (CRA, Regulation (EU) 2024/2847) holds responsible for its software: it
names its role under the CRA, where vulnerabilities are reported, its policy on coordinated vulnerability disclosure
and where it publishes its security advisories; each release names the end of its support period, its EU declaration
of conformity and its software bill of materials. POI publishes what the CRA asks a manufacturer to tell users
(Annex II); it does not check that the manufacturer complies.

The identification is a `TransparencySoftwareManufacturer_Id`, any nonempty text, trimmed, compared
case-insensitively, e.g. `OpenChargingCloud`. It is unique within the roaming network.

| | |
|---|---|
| Class | `TransparencySoftwareManufacturer` — [source](../WWCP_POI3/RoamingNetwork/TransparencySoftwareManufacturer/TransparencySoftwareManufacturer.cs) |
| Identification | `TransparencySoftwareManufacturer_Id`, e.g. `OpenChargingCloud` |
| Admin status | `TransparencySoftwareManufacturerAdminStatusType`, initially `operational` |
| Status | `TransparencySoftwareManufacturerStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `transparencySoftwareManufacturers` |
| Owner | itself (signing key); its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO; added and removed by the RNO |
| Embeds | its releases, `transparencySoftware` |
| HTTP | `GET ~/RNs/{RN_Id}/TSMs/{TSM_Id}`, with its releases, listed by `GET ~/RNs/{RN_Id}/TSMs` |


## Common properties

See [README.md](README.md#common-properties). No `eTags`.

| JSON key | Change set | Notes |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | I18N text; omitted when empty |
| `description` | update/remove | I18N text; omitted when empty |
| `dataSource` | update/remove | |
| `created` | — | set to the `createdAt` of the change set when added, unless given |
| `lastChange` | — | set to the `createdAt` of the change set by every change of it and of its releases |
| `customData` | update/remove | a JSON object; omitted when empty |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `dataLicenses` | `DataLicenses` | `ImmutableArray<DataLicense>` | update/remove | The licenses of its data, as of an [energy meter manufacturer](EnergyMeterManufacturer.md). |
| `address` | `Address` | `Address?` | update/remove | Its postal address (CRA, Annex II, point 1), see [EnergyMeterManufacturer](EnergyMeterManufacturer.md#address). |
| `logos` | `Logo` | `URL?` | update/remove | Its logo: an array of at most one `{"uri": …}`. |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | The geographical location of its headquarters. |
| `telephone` | `Telephone` | `PhoneNumber?` | update/remove | The telephone number of its office. |
| `eMailAddress` | `EMailAddress` | `EMailAddress?` | update/remove | The e-mail address of its office. |
| `homepage` | `Homepage` | `URL?` | update/remove | Its homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | The telephone number of its hotline. |
| `termsAndConditions` | `TermsAndConditionsURL` | `URL?` | update/remove | The URL of its terms and conditions. |
| `craRole` | `CRARole` | `String?` | update/remove | Its role under the CRA: `manufacturer` (Article 13), or `openSourceSoftwareSteward` (Article 24), which supports free and open-source software intended for commercial activities without being its manufacturer. Any other value is refused. |
| `vulnerabilityContact` | `VulnerabilityContact` | `EMailAddress?` | update/remove | The e-mail address of its single point of contact for vulnerabilities (Article 13(17), Annex II, point 2). |
| `vulnerabilityReporting` | `VulnerabilityReporting` | `URL?` | update/remove | Where vulnerabilities of its software are reported, e.g. a form or its `security.txt` (RFC 9116). |
| `vulnerabilityDisclosurePolicy` | `VulnerabilityDisclosurePolicy` | `URL?` | update/remove | Its policy on coordinated vulnerability disclosure (Annex I, Part II, point 5). |
| `securityAdvisories` | `SecurityAdvisories` | `URL?` | update/remove | Where it publishes the vulnerabilities it fixed (Annex I, Part II, point 4). |
| `transparencySoftware` | `TransparencySoftware` | [TransparencySoftware](#transparencysoftware)[] | add/remove, each on its own | Its releases, ordered by `@id` (ordinal); omitted when none. |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update | Its signing keys, see [Keys](#keys). |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update | Its identity keys, see [Keys](#keys). |

All but the keys are optional and omitted when absent.


## transparencySoftware

`TransparencySoftware` ([source](../WWCP_POI2/RoamingNetwork/TransparencySoftware/TransparencySoftware.cs)), context
`https://open.charging.cloud/contexts/wwcp/roamingNetwork/transparencySoftware/v1`: a release of a transparency
software, one per version. Without runtime status, timestamps and custom data; its manufacturer, which embeds it,
names who made it. Read strictly: an unknown property is an error, also the `vendor` of earlier versions.

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | string | — | `TransparencySoftware_Id`: any text, compared case-insensitively, surrounding whitespace trimmed; unique within the roaming network. |
| `name` | I18N text | update | Mandatory; a nonempty text in each language. |
| `version` | string | update | Mandatory `TransparencySoftwareVersion`, e.g. `1.4.0`. |
| `openSourceLicenses` | object[] | update | Mandatory, at least one: `{@id, description?, URLs}` (Hermod `OpenSourceLicense`, embedded without `@context`); an `@id` only once (case-insensitive); in a canonical order (`@id` upper case, description, URLs). |
| `logo` | URL | update/remove | Absolute. |
| `howToUse` | URL | update/remove | Absolute; a manual. |
| `moreInformation` | URL | update/remove | Absolute. |
| `sourceCodeRepository` | URL | update/remove | Absolute. |
| `supportUntil` | timestamp | update/remove | The end of its support period, until when its manufacturer handles its vulnerabilities and provides security updates (CRA, Article 13(8), Annex II, point 7); in UTC to the millisecond. |
| `declarationOfConformity` | URL | update/remove | Absolute; its EU declaration of conformity (CRA, Article 28, Annex II, point 6). |
| `softwareBillOfMaterials` | URL | update/remove | Absolute; its software bill of materials, when its manufacturer makes it available to its users (CRA, Annex I, Part II, point 1, Annex II, point 9), e.g. CycloneDX or SPDX. |

- `RoamingNetworkChange.Add(release, manufacturerId)`: the parent of the `Add` is its manufacturer, which signs it.
- `RoamingNetworkChange.Remove(releaseId)`, `UpdateProperty(release, …)`/`RemoveProperty(release, …)`: signed by
  the manufacturer embedding it; a change naming another parent is refused.
- A release still named by a certificate or an energy meter can not be removed, nor its manufacturer.
- Two manufacturers can not have releases of the same identification.


## Keys

Signing and identity keys as of every party (see [ChargingStationOperator](ChargingStationOperator.md#signing-and-identity-keys)):
it is added with at least one of each, every new identity key signing the change set as the proof that the
manufacturer holds it, and keeps at least one of each. A signing key naming `usages` signs its releases only when it
names `software`, its own properties only when it names `party` (SPECIFICATION.md, section 4.1): e.g. one key in its
build pipeline for its releases, one in its office for its address.


## Change sets

- `RoamingNetworkChange.Add(manufacturer)` — signed by the RNO, with its keys and without its releases: they are its
  own to add. `created` and `lastChange` become the change set's `createdAt` unless `created` is given.
- `RoamingNetworkChange.Remove(manufacturerId)` — signed by the RNO; its releases go with it.
- `UpdateProperty(manufacturer, …)` / `RemoveProperty(manufacturer, …)` of `name`, `description`, `dataSource`,
  `dataLicenses`, `address`, `logos`, `geoLocation`, `telephone`, `eMailAddress`, `homepage`, `hotline`,
  `termsAndConditions`, `craRole`, `vulnerabilityContact`, `vulnerabilityReporting`, `vulnerabilityDisclosurePolicy`,
  `securityAdvisories`, `customData` — signed by the manufacturer; `signingKeys` and `identityKeys` as of every party.
  `transparencySoftware` is no property: its releases are added, changed and removed on their own.
- Admin status, status and internal data go along to the changed version.


## Effective tree

A transparency software manufacturer is a node of the effective tree (SPECIFICATION.md, section 8) with the child
array `transparencySoftware`. The proof of a charging station gives the releases its energy meters name, in the
skeletons of their manufacturers, and prunes the other releases.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `TransparencySoftwareManufacturer`.

- **Admin status** `TransparencySoftwareManufacturerAdminStatusType`: `unknown`, `unspecified`, `planned`,
  `inDeployment`, `operational`, `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `TransparencySoftwareManufacturerStatusType`: `unknown`, `unspecified`, `offline`, `inDeployment`,
  `available`, `error`. Initially `available` since `created`.
- Releases have none.


## Example

```json
{
  "@id": "OpenChargingCloud",
  "name": { "en": "Open Charging Cloud" },
  "homepage": "https://open.charging.cloud",
  "craRole": "openSourceSoftwareSteward",
  "vulnerabilityContact": "security@open.charging.cloud",
  "vulnerabilityReporting": "https://open.charging.cloud/.well-known/security.txt",
  "vulnerabilityDisclosurePolicy": "https://open.charging.cloud/security/policy",
  "securityAdvisories": "https://github.com/OpenChargingCloud/ChargyDesktopApp/security/advisories",
  "transparencySoftware": [
    {
      "@id": "chargy-1.5.0",
      "name": { "en": "Chargy" },
      "version": "1.5.0",
      "openSourceLicenses": [ { "@id": "AGPL-3.0", "URLs": [ "https://www.gnu.org/licenses/agpl-3.0.html" ] } ],
      "sourceCodeRepository": "https://github.com/OpenChargingCloud/ChargyDesktopApp",
      "supportUntil": "2031-12-31T23:59:59.999Z",
      "declarationOfConformity": "https://open.charging.cloud/chargy/1.5.0/declarationOfConformity.pdf",
      "softwareBillOfMaterials": "https://open.charging.cloud/chargy/1.5.0/sbom.cdx.json"
    }
  ],
  "signingKeys":  [ { "@id": "…", "key": "…" } ],
  "identityKeys": [ { "@id": "…", "key": "…" } ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-01-01T00:00:00.000Z"
}
```
