# RoamingNetwork

A roaming network is the root of all POI data: one shared data set of charging station operators,
e-mobility providers, grid operators, parking operators, transparency software manufacturers and conformity
assessment bodies (the *parties*), together with the registries they share, charging station and energy meter
manufacturers.
A version of a roaming network with everything it embeds is one immutable data set, named by its
[ETags](README.md#etags); a change set leads to the next version.

The roaming network is owned by its **roaming network operator (RNO)**, the
[RoamingNetworkOperator](RoamingNetworkOperator.md) it embeds as `operator`, with its keys: the
network has none of its own. The RNO signs changes of the network's own properties, adds and removes parties, and
owns the registries (manufacturers, energy meter manufacturers).
Each party owns its own properties and data. There is no registry of brands: every charging station operator, pool,
station, EVSE and RFID authentication mode embeds its own, see [Brand](Brand.md).

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
| Owner | its roaming network operator (RNO), `operator`, by its `signingKeys` / `identityKeys` |
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
| `@context` | `JSONLDContext` (const) | string | — | `https://open.charging.cloud/contexts/wwcp/roamingNetwork/v1`, see [below](#context). |
| `dataLicenses` | `DataLicenses` | [DataLicense](#datalicenses)[] | update/remove | The licenses of the data, ordered by `@id` (ordinal); omitted when empty. |
| `operator` | `Operator` | [RoamingNetworkOperator](RoamingNetworkOperator.md)? | its properties: update/remove | Its operator, with the keys of the network; omitted for a network without one, which nobody can change where only the RNO may. Neither added nor removed. |
| `chargingStationOperators` | `ChargingStationOperators` | [ChargingStationOperator](ChargingStationOperator.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `chargingStationManufacturers` | `ChargingStationManufacturers` | [ChargingStationManufacturer](ChargingStationManufacturer.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `eMobilityProviders` | `EMobilityProviders` | [EMobilityProvider](EMobilityProvider.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `gridOperators` | `GridOperators` | [GridOperator](GridOperator.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `parkingOperators` | `ParkingOperators` | [ParkingOperator](ParkingOperator.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `energyMeterManufacturers` | `EnergyMeterManufacturers` | [EnergyMeterManufacturer](EnergyMeterManufacturer.md)[] | add/remove | Ordered by `@id` (ordinal). |
| `transparencySoftwareManufacturers` | `TransparencySoftwareManufacturers` | [TransparencySoftwareManufacturer](TransparencySoftwareManufacturer.md)[] | add/remove | Parties, each with its releases, one per version of a software; ordered by `@id` (ordinal). `TransparencySoftware` gives the releases of all of them by their identifications, unique within the network. |
| `conformityAssessmentBodies` | `ConformityAssessmentBodies` | [ConformityAssessmentBody](ConformityAssessmentBody.md)[] | add/remove | Parties, each with the transparency software certificates it issued; ordered by `@id` (ordinal). `TransparencySoftwareCertificates` gives the certificates of all of them by their identifications, unique within the network. |

Every collection is omitted when empty. Children are never written or read by their identifications alone:
`TryParse` refuses `chargingStationOperatorIds`, `chargingStationManufacturerIds`, `eMobilityProviderIds`,
`gridOperatorIds`, `parkingOperatorIds`, `energyMeterManufacturerIds`, `transparencySoftwareManufacturerIds`
and `conformityAssessmentBodyIds`. The network has no `brands`: brands are embedded in what they are a brand
of ([Brand](Brand.md)).

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
- `Parties` — the RNO (`OperatorParty`, `POIPartyId("RoamingNetworkOperator", id)`) and every operator and provider, with their keys; `TryGetParty`.
- `Revision` — a local counter, one more per applied change set; not content.
- Lookups: `TryGetChargingStationOperatorById`, `TryGetEMobilityProviderById`, `TryGetGridOperatorById`,
  `TryGetParkingOperatorById`, `TryGetChargingStationManufacturerById`, `TryGetEnergyMeterManufacturerById`,
  `TryGetTransparencySoftwareManufacturerById`, `TryGetTransparencySoftwareById`, `TransparencySoftwareManufacturerOf`,
  `TryGetConformityAssessmentBodyById`, `TryGetTransparencySoftwareCertificateById`, `ConformityAssessmentBodyOf`, ….


## @context

`https://open.charging.cloud/contexts/wwcp/roamingNetwork/v1`, written by `ToJSON()` and `ToCBOR()` unless
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

A roaming network has no keys of its own: they are those of its operator, see
[RoamingNetworkOperator](RoamingNetworkOperator.md#keys). `TryParse` refuses a network that names `signingKeys` or
`identityKeys` itself, as a network written before there were operators did; a change set changing them on the
network is refused as well.


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


## energyMeterManufacturers

The [EnergyMeterManufacturer](EnergyMeterManufacturer.md)s, named by the `manufacturerId` of energy meters and their
templates, under `energyMeterManufacturers`, ordered by `@id` (ordinal), with a runtime status as the charging
station manufacturers. Owned by the RNO: `RoamingNetworkChange.Add(energyMeterManufacturer)`,
`Remove(energyMeterManufacturerId)`, `UpdateProperty(energyMeterManufacturer, …)`. One still named can not be
removed.


## transparencySoftwareManufacturers

The [TransparencySoftwareManufacturer](TransparencySoftwareManufacturer.md)s, parties as the operators are: the RNO
adds and removes them with their keys, each changes its own properties and its releases of transparency software,
embedded in it as `transparencySoftware`, one per version of a software. Releases are named by certificates and by
the `transparencySoftwareStatus` of [EnergyMeter](EnergyMeter.md)s, by their identifications alone: these are unique
within the network, whichever manufacturer embeds them. A manufacturer whose release is still named can not be
removed.


## conformityAssessmentBodies

The [ConformityAssessmentBody](ConformityAssessmentBody.md)s, e.g. national metrology institutes, parties as the
operators are: the RNO adds and removes them, with their keys or, while one does not take part, without; each changes
its own properties and the transparency software certificates it issued, embedded in it as
`transparencySoftwareCertificates`, the RNO in the place of one without keys. Certificates are named by the
`transparencySoftwareStatus` of [EnergyMeter](EnergyMeter.md)s by their identifications alone: these are unique within
the network, whichever body embeds them. A body whose certificate is still named can not be removed.


## Change sets of the RNO

A change set must be signed by the owner of each of its changes ([../README.md](../README.md#change-sets),
[SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners)). The RNO signs:

| Change | Key of the RNO |
|---|---|
| `UpdateProperty`/`RemoveProperty` of the network's `name`, `description`, `dataSource`, `dataLicenses`, `customData` | signing key |
| `UpdateProperty`/`RemoveProperty` of the RNO's own properties | signing key |
| `UpdateProperty` of the RNO's `signingKeys` | signing key or identity key |
| `UpdateProperty` of the RNO's `identityKeys` | identity key |
| `Add`/`Remove` of a charging station operator, e-mobility provider, grid operator or parking operator | signing key |
| `UpdateProperty` of the `signingKeys`/`identityKeys` of such a party (recovery; the party's own keys also count, see its file) | identity key |
| every change of charging station manufacturers and energy meter manufacturers | signing key |
| `Add`/`Remove` of a transparency software manufacturer or a conformity assessment body, its releases or certificates with it | signing key |
| every change of a conformity assessment body without keys and of its certificates, in its place | signing key |

- A party is added with at least one `signingKeys` and one `identityKeys` entry, and without embedded children
  (`chargingPools`, `chargingTariffs`, the four group arrays; `parkingGarages`, `parkingSpaces`, `parkingSensors`,
  `parkingSpaceGroups`, `parkingProducts`): they are its own to add. Its other properties it changes itself. Each
  identity key it is added with signs the change set as well, as does every new identity key of a party later: the
  proof that the party holds it (SPECIFICATION.md, section 4.1).
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
             RoamingNetworkChange.Add           (new EnergyMeterManufacturer(EnergyMeterManufacturer_Id.Parse("EMH"),
                                                                             new ImmutableI18NString(I18NString.Create(Languages.en, "EMH metering")))),
             RoamingNetworkChange.Add           (newOperator)                       // with its keys, without children
         ]).
         Sign(rnoSigningKey, POIPublicKey.From(rnoSigningKey).Id, COSEAlgorithm.ES256);  // a signing key of rn1.Operator
```


## Runtime status

Admin status and status are kept in the status schedules shared by all versions, see
[README.md](README.md#runtime-status); in a history as `entityType` `RoamingNetwork`. No energy status.
Transparency software releases and certificates have no runtime status; their manufacturers and bodies have, see their files.

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
  e-mobility providers, grid operators, parking operators, energy meter manufacturers, transparency software
  releases and certificates.
- Keys: `@id` the thumbprint of `key`, canonical Base64, no private parts, `notBefore` ≤ `notAfter`, not twice,
  not both a signing and an identity key.
- Unique within the network, not only within their operator: energy meters; parking garages, spaces, sensors,
  space groups and products.
- References must exist in the network:
  the `manufacturerId` of a charging station (`chargingStationManufacturers`);
  the `gridOperatorId` of a grid connection point (`gridOperators`);
  the `manufacturerId` of an energy meter (`energyMeterManufacturers`);
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
  "@context":    "https://open.charging.cloud/contexts/wwcp/roamingNetwork/v1",
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
  "energyMeterManufacturers": [
    { "@id": "EMH",
      "name": { "en": "EMH metering" },
      "homepage": "https://www.emh-metering.com",
      "created":    "2026-01-01T00:00:00.000Z",
      "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "transparencySoftwareManufacturers": [
    { "@id": "GraphDefined",
      "name": { "en": "GraphDefined GmbH" },
      "craRole": "manufacturer",
      "vulnerabilityContact": "security@graphdefined.com",
      "transparencySoftware": [
        { "@id": "chargy-desktop-1.4.0",
          "name": { "en": "Chargy Transparency Software Desktop" },
          "version": "1.4.0",
          "openSourceLicenses": [
            { "@id": "AGPL-3.0", "URLs": [ "https://www.gnu.org/licenses/agpl-3.0.html" ] }
          ],
          "sourceCodeRepository": "https://github.com/OpenChargingCloud/ChargyDesktopApp",
          "supportUntil": "2031-12-31T23:59:59.999Z" }
      ],
      "signingKeys":  [ { "@id": "71aa…", "key": "pQECIAEh…" } ],
      "identityKeys": [ { "@id": "0d3e…", "key": "pQECIAMh…" } ],
      "created":    "2026-01-01T00:00:00.000Z",
      "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "conformityAssessmentBodies": [
    { "@id": "PTB",
      "name": { "de": "Physikalisch-Technische Bundesanstalt" },
      "notifiedBodyNumber": "0102",
      "notifiedFor": [ "MID" ],
      "transparencySoftwareCertificates": [
        { "@id": "PTB-2026-0042",
          "chargingStationModel": "Wallbox 22",
          "chargingStationModelVersion": "2.1",
          "verifiedTransparencySoftwareIds":   [ "chargy-desktop-1.4.0" ],
          "compatibleTransparencySoftwareIds": [],
          "notBefore": "2026-01-01T00:00:00.000Z" }
      ],
      "signingKeys":  [ { "@id": "e5c0…", "key": "pQECIAEh…" } ],
      "identityKeys": [ { "@id": "9b72…", "key": "pQECIAMh…" } ],
      "created":    "2026-01-01T00:00:00.000Z",
      "lastChange": "2026-01-01T00:00:00.000Z" }
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
