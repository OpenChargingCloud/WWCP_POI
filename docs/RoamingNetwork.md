# RoamingNetwork

A roaming network is the root of all POI data: one shared data set of charging station operators,
e-mobility providers, grid operators and parking operators (the *parties*), together with the registries
they share, e.g. charging station and energy meter manufacturers, brands and transparency software.
A version of a roaming network with everything it embeds is one immutable data set, named by its
[ETags](README.md#etags); a change set leads to the next version.

The roaming network is owned by its **roaming network operator (RNO)**, who has no entity of its own:
the RNO is named by the network's `signingKeys` and `identityKeys`. The RNO signs changes of the network's own
properties, adds and removes parties, and owns the registries (brands, manufacturers, energy meter manufacturers,
transparency software releases and certificates). Each party owns its own properties and data.

The identification is a `RoamingNetwork_Id`, any nonempty text without surrounding whitespace, compared
case-insensitively, e.g. `rn1` or `Prod`. It names the network on a server (`~/RNs/{RN_Id}`) and in every
change set and commit (`roamingNetworkId`).

| | |
|---|---|
| Class | `RoamingNetwork` — [source](../WWCP_POI3/RoamingNetwork/RoamingNetwork.cs), partials `RoamingNetwork.*.cs` |
| Identification | `RoamingNetwork_Id`, e.g. `rn1` |
| Admin status | `RoamingNetworkAdminStatusType`, initially `operational` |
| Status | `RoamingNetworkStatusType`, initially `available` |
| Embedded in | — (root) |
| Owner | the roaming network operator (RNO), by the network's own `signingKeys` / `identityKeys` |
| HTTP | `GET ~/RNs` (list of `{@id, name, description}`), `GET ~/RNs/{RN_Id}`; history under `~/RNs/{RN_Id}/history/…`, see [../README.md](../README.md#history-and-sync) |

`GET ~/RNs/{RN_Id}` serves the complete network as canonical JSON, or as deterministic CBOR for
`Accept: application/cbor`, with its `eTags`; the HTTP ETag is the POI ETag of the format of the response.


## Common properties

See [README.md](README.md#common-properties).

| JSON key | Change set | Notes |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | always written, `{}` when empty; removing it leaves it empty |
| `description` | update/remove | omitted when empty |
| `dataSource` | update/remove | a nonempty text; omitted when empty |
| `created` | — | |
| `lastChange` | — | set to the `createdAt` of the change set |
| `customData` | update/remove | a JSON object |
| `eTags` | — | written by `ToJSON()`/`ToCBOR()` unless `IncludeETags: false`; when given, `TryParse` refuses the network unless they are its ETags |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `@context` | `JSONLDContext` (const) | string | — | `https://open.charging.cloud/contexts/wwcp+json/roamingNetwork`, see [below](#context). |
| `dataLicenses` | `DataLicenses` | [DataLicense](#datalicenses)[] | update/remove | The licenses of the data, ordered by `@id` (ordinal); omitted when empty. |
| `chargingStationOperators` | `ChargingStationOperators` | [ChargingStationOperator](ChargingStationOperator.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `chargingStationManufacturers` | `ChargingStationManufacturers` | [ChargingStationManufacturer](ChargingStationManufacturer.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `eMobilityProviders` | `EMobilityProviders` | [EMobilityProvider](EMobilityProvider.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `gridOperators` | `GridOperators` | [GridOperator](GridOperator.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `parkingOperators` | `ParkingOperators` | [ParkingOperator](ParkingOperator.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `brands` | `Brands` | [Brand](#brands)[] | add/remove | Ordered by `@id` (ordinal). |
| `energyMeterManufacturers` | `EnergyMeterManufacturers` | [EnergyMeterManufacturer](#energymetermanufacturers)[] | add/remove | Ordered by `@id` (ordinal). |
| `transparencySoftware` | `TransparencySoftware` | [TransparencySoftware](#transparencysoftware)[] | add/remove | Releases, one per version of a software, ordered by `@id` (ordinal). |
| `transparencySoftwareCertificates` | `TransparencySoftwareCertificates` | [TransparencySoftwareCertificate](#transparencysoftwarecertificates)[] | add/remove | Ordered by `@id` (ordinal). |
| `signingKeys` | `SigningKeys` | [POIPublicKey](#signingkeys-and-identitykeys)[] | update | The RNO's keys signing its data changes, ordered by `@id`. Can not be removed. |
| `identityKeys` | `IdentityKeys` | [POIPublicKey](#signingkeys-and-identitykeys)[] | update | The RNO's long-term keys signing changes of its identity keys, ordered by `@id`. Can not be removed. |

Every collection is omitted when empty. Children are never written or read by their identifications alone:
`TryParse` refuses `chargingStationOperatorIds`, `chargingStationManufacturerIds`, `eMobilityProviderIds`,
`gridOperatorIds`, `parkingOperatorIds`, `brandIds`, `energyMeterManufacturerIds`, `transparencySoftwareIds`
and `transparencySoftwareCertificateIds`.

**Views.** `ToJSON(…)` has view options: `Embedded: true` drops `@context`; `ExpandChargingStationOperatorIds`
other than `Expanded` writes `chargingStationOperatorIds` (or nothing for `Hidden`), which can not be read back;
`ExpandDataLicenses: ShowIdOnly` writes `dataLicenseIds` (or nothing for `Hidden`); the other `Expand…` options
pass through to the operators. The content, hashed into the ETags, is `ToJSON(Embedded: false, IncludeETags: false)`
with the default view options ([SPECIFICATION.md](../SPECIFICATION.md#2-the-content-of-a-version)).

**Derived (not serialized)**

- `EnergyMeters` — the energy meters of all pools, grid connection points, stations and EVSEs; `TryGetEnergyMeterById`.
- `ChargingTariffs`, `EVSEGroups`, `ChargingStationGroups`, `ChargingPoolGroups`, `ChargingTariffGroups` — those of all
  charging station operators; `TryGet…ById` looks in the operator the identification names.
- `ParkingGarages`, `ParkingSpaces`, `ParkingSensors`, `ParkingSpaceGroups`, `ParkingProducts` — those of all parking operators.
- `Parties` — the RNO (`POIPartyId("RoamingNetwork", id)`) and every operator and provider, with their keys; `TryGetParty`.
- `Revision` — a local counter, one more per applied change set; not content.
- Lookups: `TryGetChargingStationOperatorById`, `TryGetEMobilityProviderById`, `TryGetGridOperatorById`,
  `TryGetParkingOperatorById`, `TryGetChargingStationManufacturerById`, `TryGetBrandById`,
  `TryGetEnergyMeterManufacturerById`, `TryGetTransparencySoftwareById`, `TryGetTransparencySoftwareCertificateById`, ….


## @context

`https://open.charging.cloud/contexts/wwcp+json/roamingNetwork`, written by `ToJSON()` and `ToCBOR()` unless
`Embedded: true`. It is part of the content, so of the ETags. `TryParse` refuses another one; it may be left out.


## dataLicenses

The licenses under which the data of the network is offered (`Hermod.DataLicense`, written by `POIDataLicenses`).
Operators and pools have their own `dataLicenses`.

| JSON key | Type | Description |
|---|---|---|
| `@id` | string | `DataLicense_Id`, e.g. `CC BY 4.0`, `ODbL`, `dl-de/by-2-0`. Unique within the array. `id` is also read. |
| `description` | I18N text | Omitted when empty. |
| `URLs` | string[] | More information, ordered (ordinal); omitted when empty. |

- **Reading:** complete objects in `dataLicenses` and/or the identifications of well-known licenses in
  `dataLicenseIds`; an unknown identification there is an error. A license given twice, also across both, is an error.
- **Well-known:** `None`, `PDDL`, `ODC-By`, `ODbL`, `dl-de/by-2-0`, `dl-de/zero-2-0`, `GeoLizenz_V1.3`,
  `CC BY 4.0`, `CC BY-SA 4.0`, `CC BY-ND 4.0`, `CC BY-NC 4.0`, `CC BY-NC-SA 4.0`, `CC BY-NC-ND 4.0` (`POIDataLicenses.Known`).
- **Change set:** `UpdateProperty(rn, "dataLicenses", …, [ … ])` takes complete objects only (no `dataLicenseIds`);
  `RemoveProperty` leaves none.


## signingKeys and identityKeys

The public keys of the RNO (`POIPublicKey`, [source](../WWCP_POI3/DataStructures/POIPublicKey.cs); checked by
`POIPartyKeys`). Every party has the same two arrays; see [../README.md](../README.md#change-sets) and
[SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners).

| JSON key | Type | Description |
|---|---|---|
| `@id` | string | The SHA-256 COSE key thumbprint [RFC 9679] of `key`, 64 lowercase hex digits. Must match `key`. |
| `key` | string | Canonical Base64 of the deterministic CBOR of the public COSE key; private parts are refused. |
| `notBefore` | timestamp | Optional start of validity (UTC, to the millisecond). |
| `notAfter` | timestamp | Optional end of validity; not before `notBefore`. |

- **signingKeys** — short keys (e.g. secp256r1) signing change sets of RNO data: the network's other properties,
  the registries, adding and removing parties, and the rotation of the `signingKeys` themselves.
- **identityKeys** — long-term keys (e.g. secp521r1) signing changes of the RNO's `identityKeys`, of its
  `signingKeys` when none of them can sign any more, and, for recovery, of the keys of any other party.
- **Two identity keys at least.** Nobody can replace the RNO's identity keys once they are lost or expired: there is
  nobody above the RNO, and no external trust configuration. An RNO should therefore name at least two identity keys
  with different `notAfter`, one of them kept offline, and replace an identity key well before it expires. This is
  advice, not checked.
- Both arrays are ordered by `@id` (ordinal); a key must not be given twice, nor be both a signing and an identity key.
- A key counts only when valid at the `createdAt` of the change set (`IsValidAt`), and only in the version the
  change set is applied to.
- The constructor accepts none, but a change set can only replace them with a nonempty array (`UpdateProperty`);
  `RemoveProperty` is refused.


## chargingStationOperators

The [ChargingStationOperator](ChargingStationOperator.md)s, parties with their pools, stations, EVSEs, tariffs and
groups, under `chargingStationOperators`, ordered by `@id` (ordinal). Added by the RNO with
`RoamingNetworkChange.Add(chargingStationOperator)` (its embedded JSON without its children) and removed with
`RoamingNetworkChange.Remove(chargingStationOperatorId)`; see [below](#change-sets-of-the-rno). The operator then
adds its children itself, e.g. with `RoamingNetworkChange.AddChildren(chargingStationOperator)`.


## chargingStationManufacturers

The [ChargingStationManufacturer](ChargingStationManufacturer.md)s, named by the `manufacturerId` of charging
stations and the `chargingStationManufacturerId` of certificates, under `chargingStationManufacturers`, ordered by
`@id` (ordinal). Owned by the RNO: `RoamingNetworkChange.Add(manufacturer)`, `Remove(manufacturerId)`,
`UpdateProperty(manufacturer, …)`.


## eMobilityProviders

The [EMobilityProvider](EMobilityProvider.md)s, parties without children, under `eMobilityProviders`, ordered by
`@id` (ordinal). Added and removed by the RNO: `RoamingNetworkChange.Add(eMobilityProvider)`, `Remove(eMobilityProviderId)`.


## gridOperators

The [GridOperator](GridOperator.md)s, parties named by the `gridOperatorId` of grid connection points, under
`gridOperators`, ordered by `@id` (ordinal). Added and removed by the RNO: `RoamingNetworkChange.Add(gridOperator)`
(without its grid connection points, see `AddChildren(gridOperator)`), `Remove(gridOperatorId)`.


## parkingOperators

The [ParkingOperator](ParkingOperator.md)s, parties with their parking garages, spaces, sensors, space groups and
products, under `parkingOperators`, ordered by `@id` (ordinal). Added and removed by the RNO:
`RoamingNetworkChange.Add(parkingOperator)` (without its parking data, see `AddChildren(parkingOperator)`),
`Remove(parkingOperatorId)`.


## brands

Brands under which charging pools, stations and EVSEs are offered, e.g. of a franchise (`Brand`,
[source](../WWCP_POI3/RoamingNetwork/Brand/Brand.cs)). Embedded in the network, so that operators can share them;
without runtime status, without timestamps. Named by the `brandIds` of charging pools, charging stations and EVSEs,
and by the `brandIds` of the RFID `authenticationModes` of charging pools.

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | string | — | `Brand_Id`, nonempty after trimming, compared case-insensitively. |
| `name` | I18N text | update | Mandatory, at least one language. |
| `description` | I18N text | update/remove | Omitted when empty. |
| `logo` | URL | update/remove | |
| `homepage` | URL | update/remove | |
| `customData` | object | update/remove | Omitted when empty. |

It has no timestamps: `created` and `lastChange` are refused. `RoamingNetworkChange.Add(brand)`, `Remove(brandId)`,
`UpdateProperty(brand, …)`/`RemoveProperty(brand, …)`, signed by the RNO. A brand still named can not be removed.


## energyMeterManufacturers

Manufacturers of [EnergyMeter](EnergyMeter.md)s, named by their `manufacturerId` (`EnergyMeterManufacturer`,
[source](../WWCP_POI3/RoamingNetwork/EnergyMeterManufacturer/EnergyMeterManufacturer.cs)). Without runtime status.

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | string | — | `EnergyMeterManufacturer_Id`, nonempty after trimming, compared case-insensitively. |
| `name` | I18N text | update/remove | Omitted when empty. |
| `description` | I18N text | update/remove | Omitted when empty. |
| `dataSource` | string | update/remove | |
| `address` | Address | update/remove | The headquarters (Styx `Address`, embedded: `street`, `houseNumber`, `postalCode`, `city`, `country`, …). |
| `homepage` | URL | update/remove | |
| `hotline` | string | update/remove | A phone number. |
| `created` | timestamp | — | Set to the change set's `createdAt` when added, unless given. |
| `lastChange` | timestamp | — | Set to the change set's `createdAt` by every change. |
| `customData` | object | update/remove | Omitted when empty. |

`RoamingNetworkChange.Add(energyMeterManufacturer)`, `Remove(energyMeterManufacturerId)`,
`UpdateProperty(…)`/`RemoveProperty(…)`, signed by the RNO. One still named can not be removed.


## transparencySoftware

Releases of transparency software, e.g. for the German calibration law, one per version of a software
(`TransparencySoftware`, [source](../WWCP_POI2/RoamingNetwork/TransparencySoftware/TransparencySoftware.cs)).
Without runtime status, timestamps and custom data. Named by certificates and by the `transparencySoftwareStatus`
of [EnergyMeter](EnergyMeter.md)s. Read strictly: an unknown property is an error.

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | string | — | `TransparencySoftware_Id`: any text, compared case-insensitively, surrounding whitespace trimmed. |
| `name` | I18N text | update | Mandatory; a nonempty text in each language. |
| `version` | string | update | Mandatory `TransparencySoftwareVersion`, e.g. `1.4.0`. |
| `openSourceLicenses` | object[] | update | Mandatory, at least one: `{@id, description?, URLs}` (Hermod `OpenSourceLicense`, embedded without `@context`); an `@id` only once (case-insensitive); in a canonical order (`@id` upper case, description, URLs). |
| `vendor` | string | update | Mandatory, nonempty. |
| `logo` | URL | update/remove | Absolute. |
| `howToUse` | URL | update/remove | Absolute; a manual. |
| `moreInformation` | URL | update/remove | Absolute. |
| `sourceCodeRepository` | URL | update/remove | Absolute. |

`RoamingNetworkChange.Add(transparencySoftware)`, `Remove(transparencySoftwareId)`, `UpdateProperty(…)`/`RemoveProperty(…)`,
signed by the RNO. A release still named by a certificate or an energy meter can not be removed.


## transparencySoftwareCertificates

Approval or compatibility documents for a charging station model and version, naming the transparency software
releases they verify or find compatible (`TransparencySoftwareCertificate`,
[source](../WWCP_POI2/RoamingNetwork/TransparencySoftware/TransparencySoftwareCertificate.cs)). A domain document,
not a cryptographic certificate. Without runtime status, timestamps and custom data. Read strictly: an unknown property
is an error.

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | string | — | `TransparencySoftwareCertificate_Id`: any text, compared case-insensitively, surrounding whitespace trimmed. |
| `issuer` | string | update | Mandatory: the issuing organization. |
| `chargingStationModel` | string | update | Mandatory: a `ChargingStationModel_Id`, as charging stations name their model by `modelId`, compared case-insensitively. |
| `chargingStationModelVersion` | string | update | Mandatory. |
| `verifiedTransparencySoftwareIds` | string[] | update/remove | Releases officially checked with this model/version; always written, ordered (ordinal). |
| `compatibleTransparencySoftwareIds` | string[] | update/remove | Further compatible releases, without a claim of verification; always written, ordered (ordinal). |
| `chargingStationManufacturerId` | string | update/remove | Optional; a [ChargingStationManufacturer](ChargingStationManufacturer.md) of the network. |
| `documentNumber` | string | update/remove | The issuer's number, if it differs from `@id`. |
| `documentURL` | URL | update/remove | Absolute. |
| `notBefore` | timestamp | update/remove | Start of validity, in UTC to the millisecond like every timestamp of the content, e.g. `2026-01-01T00:00:00.000Z`. |
| `notAfter` | timestamp | update/remove | End of validity; not before `notBefore`. |

- At least one release in both lists together; a release not in both, not twice, not empty.
- Every release named is one of `transparencySoftware`. An energy meter's `certificateId` must be one of these and cover
  (verify or find compatible) the release it is given for.
- `RoamingNetworkChange.Add(certificate)`, `Remove(certificateId)`, `UpdateProperty(…)`/`RemoveProperty(…)`, signed by the RNO.


## Change sets of the RNO

A change set must be signed by the owner of each of its changes ([../README.md](../README.md#change-sets),
[SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners)). The RNO signs:

| Change | Key of the RNO |
|---|---|
| `UpdateProperty`/`RemoveProperty` of the network's `name`, `description`, `dataSource`, `dataLicenses`, `customData` | signing key |
| `UpdateProperty` of the network's `signingKeys` | signing key or identity key |
| `UpdateProperty` of the network's `identityKeys` | identity key |
| `Add`/`Remove` of a charging station operator, e-mobility provider, grid operator or parking operator | signing key |
| `UpdateProperty` of the `signingKeys`/`identityKeys` of such a party (recovery; the party's own keys also count, see its file) | identity key |
| every change of brands, charging station manufacturers, energy meter manufacturers, transparency software releases and certificates | signing key |

- A party is added with at least one `signingKeys` and one `identityKeys` entry, and without embedded children
  (`chargingPools`, `chargingTariffs`, the four group arrays; `parkingGarages`, `parkingSpaces`, `parkingSensors`,
  `parkingSpaceGroups`, `parkingProducts`): they are its own to add. Its other properties it changes itself.
- Removing a party removes it with everything it embeds, unless something remaining still names part of it. This
  is intended: the RNO decides who is a member of the roaming network, also without the party's consent, e.g. when
  its contract ended; its data goes with it. The history keeps the versions before.
- A change of the network itself names `entityType` `RoamingNetwork` and its `@id`, without a parent; a registry
  entry or party may name the network as its parent, no other one.
- `Add` sets `created` (unless given) and `lastChange` to the change set's `createdAt` for entities with timestamps;
  every change sets the network's `lastChange` to it.

```csharp
var cs = rn1.CreateChangeSet([
             RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "v2"),
             RoamingNetworkChange.Add           (new Brand(Brand_Id.Parse("GEF-CHARGE"),
                                                           new ImmutableI18NString(I18NString.Create(Languages.en, "GEF Charge")))),
             RoamingNetworkChange.Add           (newOperator)                       // with its keys, without children
         ]).
         Sign(rnoSigningKey, POIPublicKey.From(rnoSigningKey).Id, COSEAlgorithm.ES256);
```


## Runtime status

Admin status and status are kept in the status schedules shared by all versions, see
[README.md](README.md#runtime-status); in a history as `entityType` `RoamingNetwork`. No energy status.
The registries (brands, energy meter manufacturers, transparency software and certificates) have no runtime status.

| | Type | Predefined values | Initially |
|---|---|---|---|
| Admin status | `RoamingNetworkAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` |
| Status | `RoamingNetworkStatusType` | `unknown`, `unspecified`, `offline`, `inDeployment`, `available`, `error` | `available` |

- Both since `created`, unless the constructor is given `InitialAdminStatus`/`InitialStatus`; each schedule keeps
  `MaxAdminStatusScheduleSize`/`MaxStatusScheduleSize` entries (default 50). Other nonempty values parse as well.
- Events: `OnAdminStatusChanged` (`OnRoamingNetworkAdminStatusChangedDelegate`) and `OnStatusChanged`
  (`OnRoamingNetworkStatusChangedDelegate`), each `(Timestamp, EventTrackingId, RoamingNetwork, NewStatus, OldStatus, DataSource)`.


## Validation

Checked by the constructor (so by `TryParse`) and again on the result of every change set:

- `@id` mandatory; every other property optional.
- Within each collection an `@id` only once: data licenses, charging station operators, charging station manufacturers,
  e-mobility providers, grid operators, parking operators, brands, energy meter manufacturers, transparency software
  releases and certificates.
- Keys: `@id` the thumbprint of `key`, canonical Base64, no private parts, `notBefore` ≤ `notAfter`, not twice,
  not both a signing and an identity key.
- Unique within the network, not only within their operator: energy meters; parking garages, spaces, sensors,
  space groups and products.
- References must exist in the network:
  the `manufacturerId` of a charging station (`chargingStationManufacturers`);
  the `gridOperatorId` of a grid connection point (`gridOperators`);
  the `manufacturerId` of an energy meter (`energyMeterManufacturers`);
  the `brandIds` of pools, stations, EVSEs and RFID authentication modes (`brands`);
  the `chargingStationIds` of parking garages, spaces and sensors; the `parkingSpaceIds` of charging stations;
  the releases and `chargingStationManufacturerId` of certificates;
  the releases and `certificateId` of an energy meter's `transparencySoftwareStatus`, the certificate covering the release.
  So a referenced entity can not be removed.

Checked when a change set is applied:

- The change set is for this `@id` and signed by the owners of its changes, with keys of this version
  (`TryCheckOwnership`); its before ETags are those of this version, its after ETags those of the result.
- `name`, `description`: a JSON object; `dataSource`: a nonempty string; `dataLicenses`: complete licenses;
  `customData`: a JSON object; `signingKeys`, `identityKeys`: a nonempty array of keys.
- Only `name`, `description`, `dataSource`, `dataLicenses` and `customData` can be removed, and only when present.
- An `oldValue` must equal the current value; JSON `null` expects it absent.


## Example

`…` stands for elided keys and digests.

```json
{
  "@id":         "rn1",
  "@context":    "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork",
  "name":        { "en": "Example" },
  "description": { "en": "Public charging infrastructure in Jena" },
  "dataSource":  "v2",
  "dataLicenses": [
    { "@id": "dl-de/by-2-0",
      "description": { "de": "Datenlizenz Deutschland – Namensnennung – Version 2.0" },
      "URLs": [ "https://www.govdata.de/dl-de/by-2-0" ] }
  ],
  "chargingStationOperators": [
    { "@id": "DE*GEF",
      "name": { "en": "GraphDefined" },
      "signingKeys":  [ { "@id": "3f0c…", "key": "pQECIAEh…" } ],
      "identityKeys": [ { "@id": "a81d…", "key": "pQECIAMh…" } ],
      "created":    "2026-01-01T00:00:00.000Z",
      "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "brands": [
    { "@id": "GEF-CHARGE",
      "name": { "en": "GEF Charge" },
      "homepage": "https://charge.example.org" }
  ],
  "energyMeterManufacturers": [
    { "@id": "EMH",
      "name": { "en": "EMH metering" },
      "homepage": "https://www.emh-metering.com",
      "created":    "2026-01-01T00:00:00.000Z",
      "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "transparencySoftware": [
    { "@id": "chargy-desktop-1.4.0",
      "name": { "en": "Chargy Transparency Software Desktop" },
      "version": "1.4.0",
      "openSourceLicenses": [
        { "@id": "AGPL-3.0", "URLs": [ "https://www.gnu.org/licenses/agpl-3.0.html" ] }
      ],
      "vendor": "GraphDefined GmbH",
      "sourceCodeRepository": "https://github.com/OpenChargingCloud/ChargyDesktopApp" }
  ],
  "transparencySoftwareCertificates": [
    { "@id": "PTB-2026-0042",
      "issuer": "Physikalisch-Technische Bundesanstalt",
      "chargingStationModel": "Wallbox 22",
      "chargingStationModelVersion": "2.1",
      "verifiedTransparencySoftwareIds":   [ "chargy-desktop-1.4.0" ],
      "compatibleTransparencySoftwareIds": [],
      "notBefore": "2026-01-01T00:00:00.000Z" }
  ],
  "signingKeys":  [ { "@id": "5b9e…", "key": "pQECIAEh…" } ],
  "identityKeys": [ { "@id": "c2f4…", "key": "pQECIAMh…", "notAfter": "2036-01-01T00:00:00.000Z" } ],
  "created":    "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-10-12T12:00:00.000Z",
  "eTags": [
    [ "json", "sha256", "hex", "…" ],
    [ "cbor", "sha256", "hex", "…" ]
  ]
}
```
