# ChargingStationOperator

A charging station operator (CSO, CPO) runs charging pools, charging stations and EVSEs, though it need not own
them, and publishes their locations, characteristics and prices. In POI 3 it is a **party**: it names its own
signing and identity keys and signs every change of its data itself.

It is embedded completely in its [RoamingNetwork](RoamingNetwork.md) under `chargingStationOperators`, in the
ordinal order of the identifications, and embeds completely its charging pools (with their stations and EVSEs),
its charging tariffs, its groups and its brands. The roaming network operator (RNO) adds and removes it; everything
else of it, its properties and all it embeds, the operator changes itself.

Its identification is a country code and three characters, e.g. `DE*GEF`, unique within the roaming network.
The pools, stations, EVSEs, tariffs and groups it embeds all begin with it: `DE*GEF*P1`, `DE*GEF*S1`, `DE*GEF*TAC1`, …

| | |
|---|---|
| Class | `ChargingStationOperator` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationOperator/ChargingStationOperator.cs), [serialization](../WWCP_POI3/RoamingNetwork/ChargingStationOperator/ChargingStationOperator.Serialization.cs) |
| Identification | `ChargingStationOperator_Id`, `^([A-Z]{2})\*?([A-Z0-9]{3})$`, e.g. `DE*GEF`; always written with `*` |
| Admin status | `ChargingStationOperatorAdminStatusType`, initially `Operational` |
| Status | `ChargingStationOperatorStatusType`, initially `Available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `chargingStationOperators` |
| Owner | itself (a signing key; its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO); added and removed by the RNO |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs` (all, as `{@id, name, description}`), `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}` (`?without=chargingPools` without its charging pools, which `GET ~/RNs/{RN_Id}/CPs?operatorId={CSO_Id}` lists; `?without=` also any of `chargingTariffs`, `chargingTariffGroups`, `chargingPoolGroups`, `chargingStationGroups`, `EVSEGroups`, and `parkingSpaces`, its own parking spaces, which `…/CSs/{CS_Id}/PSs` list as its charging stations name them), `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/{CTs\|CTGs\|CPGs\|CSGs\|EVSEGs}` (each of them, searched and paged), and one of them below it, e.g. `…/CTs/{CT_Id}`, `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/Brands` (its [brands](Brand.md), searched and paged), `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/Brands/{Brand_Id}` |


## Common properties

See [Common properties](README.md#common-properties).

| JSON key | Change set | Note |
|---|---|---|
| `@id` | — | |
| `name` | update | Always written: without one, the name is `{"en": "Charging Station Operator <id>"}`. It can not be removed, only changed. |
| `description` | update/remove | |
| `dataSource` | update/remove | |
| `created` | — | |
| `lastChange` | — | Set to the change set's `createdAt` by every change of the operator's own properties and of everything it embeds: pools, stations, EVSEs, tariffs and groups. |
| `customData` | update/remove | |

The operator writes no `eTags`.


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `dataLicenses` | `DataLicenses` | `ImmutableArray<DataLicense>` | update/remove | The licenses of its data, see [Data licenses](#data-licenses); omitted when empty. |
| `address` | `Address` | `Address?` | update/remove | The address of its headquarters, see [Address and geo location](#address-and-geo-location). |
| `logos` | `Logo` | `URL?` | update/remove | Its logo, see [Logos](#logos). |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | Its geographical location. |
| `telephone` | `Telephone` | `PhoneNumber?` | update/remove | See [Contact](#contact). |
| `eMailAddress` | `EMailAddress` | `SimpleEMailAddress?` | update/remove | See [Contact](#contact). |
| `homepage` | `Homepage` | `URL?` | update/remove | See [Contact](#contact). |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | See [Contact](#contact). |
| `termsAndConditions` | `TermsAndConditionsURL` | `URL?` | update/remove | See [Contact](#contact). |
| `brands` | `Brands` | `POICollection<Brand_Id, Brand>` | update/remove | The [brands](Brand.md) it is a white-label solution for, its own, embedded completely; not those of its pools, stations or EVSEs. Changed as a whole or [brand by brand](Brand.md#change-sets). |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update | 1..n keys signing its change sets, see [Keys](#signing-and-identity-keys). |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update | 1..n long-term keys signing changes of its identity keys. |
| `chargingTariffs` | `ChargingTariffs` | `POICollection<ChargingTariff_Id, ChargingTariff>` | add/remove | Its [charging tariffs](#charging-tariffs). |
| `chargingPools` | `ChargingPools` | `POICollection<ChargingPool_Id, ChargingPool>` | add/remove | Its [charging pools](#charging-pools). |
| `EVSEGroups` | `EVSEGroups` | `POICollection<EVSEGroup_Id, EVSEGroup>` | add/remove | See [Groups](#groups). |
| `chargingStationGroups` | `ChargingStationGroups` | `POICollection<ChargingStationGroup_Id, ChargingStationGroup>` | add/remove | See [Groups](#groups). |
| `chargingPoolGroups` | `ChargingPoolGroups` | `POICollection<ChargingPoolGroup_Id, ChargingPoolGroup>` | add/remove | See [Groups](#groups). |
| `chargingTariffGroups` | `ChargingTariffGroups` | `POICollection<ChargingTariffGroup_Id, ChargingTariffGroup>` | add/remove | See [Groups](#groups). |
| `parkingSpaces` | `ParkingSpaces` | `POICollection<ParkingSpace_Id, ParkingSpace>` | add/remove | The [parking spaces](ParkingSpace.md) it defines itself where no parking operator does: in no parking garage, without parking sensors or parking products; identifications unique within the network, parking operators' as well. |

Children are written only when there are any; collections in the ordinal order of their identifications.

**Derived (not serialized)**

- `ChargingStations`: the charging stations of all its pools.
- `EVSEs`: the EVSEs of all its pools.
- `EnergyMeters`: the energy meters of all its pools, their grid connection points, stations and EVSEs.
- `TryGetChargingPool`, `TryGetChargingStation`, `TryGetEVSE`, `TryGetEnergyMeter`, `TryGetChargingTariff`,
  `TryGetEVSEGroup`, `TryGetChargingStationGroup`, `TryGetChargingPoolGroup`, `TryGetChargingTariffGroup`:
  lookups by identification, across all pools.
- No reference to its roaming network: versions share the operators they did not change, so it would be the
  wrong version for all but one of them. An operator is always reached through its roaming network.


### ToJSON and ToEmbeddedJSON

| Method | Writes |
|---|---|
| `ToEmbeddedJSON()` | The content, as the roaming network embeds it: no `@context`, `dataLicenses` complete. Change sets compare and edit this representation, `RoamingNetworkChange.Add(operator)` sends it without children, and `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}` serves it. |
| `ToCBOR(Embedded = false)` | The keys and values of `ToJSON(Embedded, ExpandDataLicenses: Expanded)`. |
| `ToJSON(Embedded = false, …)` | With its defaults: `@context` (`https://open.charging.cloud/contexts/wwcp+json/chargingStationOperator`) unless embedded, and `dataLicenses` complete; `ExpandDataLicenses: ShowIdOnly` writes `dataLicenseIds` instead, which reads again only for well-known licenses. |

`TryParse(JSON)` reads all of them: it refuses an `@context` other than its own, ignores `roamingNetworkId`, and reads `dataLicenseIds` only
for well-known licenses. `TryParse(CBOR)` reads the CBOR map as JSON.


## Charging pools

The charging pools of the operator, `chargingPools`, each embedded completely with its charging stations, their
EVSEs and all energy meters, in the ordinal order of their identifications. A pool belongs to the operator its
identification names: `DE*GEF*P1` is one of `DE*GEF`. Pool, station and EVSE identifications are unique within the
operator, not only within their parent.

- **Add:** `RoamingNetworkChange.Add(chargingPool)`, with its stations and EVSEs, as `ChargingPool.ToJSON()` writes
  it. Its parent, if named, must be the operator. `created` is taken from the pool or else set to the change set's
  `createdAt`, `lastChange` always to `createdAt`.
- **Remove:** `RoamingNetworkChange.Remove(chargingPoolId)` removes the pool with everything it embeds; removing the
  last one removes `chargingPools`. A pool, station or EVSE a group names can not be removed.
- **Change:** `UpdateProperty`/`RemoveProperty` of the pool itself; stations are added to a pool with
  `Add(station, poolId)`.
- Signed with a signing key of the operator.

See [ChargingPool](ChargingPool.md).


## Charging tariffs

The prices of the operator, `chargingTariffs`, as in OCPI, in the ordinal order of their identifications, e.g.
`DE*GEF*TAC1`. They belong to the operator their identification names; EVSEs (`chargingTariffIds`) and groups
(`chargingTariffId`) name only tariffs of their own operator. Added with `RoamingNetworkChange.Add(chargingTariff)`,
removed with `Remove(chargingTariffId)`; a tariff an EVSE or group names can not be removed.
See [ChargingTariff](ChargingTariff.md).


## Groups

Four kinds of groups, each a list of members of this operator, embedded completely in the ordinal order of their
identifications. Groups may overlap; members must exist at the operator. Added with
`RoamingNetworkChange.Add(group)`, removed with `Remove(groupId)`; a member can not be removed while a group
names it.

| JSON key | Type | Identification, e.g. | Members | Common tariff |
|---|---|---|---|---|
| `EVSEGroups` | [EVSEGroup](EVSEGroup.md) | `DE*GEF*EG1` | `EVSEIds` | `chargingTariffId` |
| `chargingStationGroups` | [ChargingStationGroup](ChargingStationGroup.md) | `DE*GEF*SG1` | `chargingStationIds` | `chargingTariffId` |
| `chargingPoolGroups` | [ChargingPoolGroup](ChargingPoolGroup.md) | `DE*GEF*PG1` | `chargingPoolIds` | `chargingTariffId` |
| `chargingTariffGroups` | [ChargingTariffGroup](ChargingTariffGroup.md) | `DE*GEF*TG1` | `chargingTariffIds` | — |


## Logos

`logos` is an array of at most one logo object; the C# model keeps a single `Logo` URL.

| JSON key | Type | Description |
|---|---|---|
| `uri` | `URL` | The URL of the logo image. Mandatory within a logo object. |

A change set updates `logos` as a whole, e.g. `[{"uri": "https://example.org/logo.svg"}]`. More than one logo, or a
logo without `uri`, is refused.


## Address and geo location

`address` (the headquarters) and `geoLocation` are written as Styx writes them embedded, without `@context`.

**Address**

| JSON key | Type | Description |
|---|---|---|
| `street` | text | Mandatory. |
| `postalCode` | text | Mandatory. |
| `city` | I18N text | Mandatory, e.g. `{"de": "Jena"}`. |
| `country` | text | Mandatory, ISO 3166-1 alpha-3, e.g. `DEU`. |
| `houseNumber` | text | |
| `floorLevel` | text | |
| `region` | text | |
| `postalCodeSub` | text | |
| `timeZone` | text | |
| `officialLanguages` | array of texts | |
| `comment` | I18N text | |
| `customData` | object | |

**Geo location**

| JSON key | Type | Description |
|---|---|---|
| `lat` | number | Latitude. |
| `lng` | number | Longitude. |
| `alt` | number | Altitude, optional. |
| `projection` | text | Only when not WGS84. |


## Signing and identity keys

The operator is a party (`IPOIParty`): it names the public keys its change sets are checked against.

| JSON key | Description |
|---|---|
| `signingKeys` | 1..n keys signing the change sets of its data, e.g. secp256r1. |
| `identityKeys` | 1..n long-term keys signing changes of its identity keys, and of its signing keys when none of them can sign any more, e.g. secp521r1. |

Each key:

| JSON key | Type | Description |
|---|---|---|
| `@id` | text | The SHA-256 COSE key thumbprint (RFC 9679) of the key, 64 lowercase hex digits; checked when read. |
| `key` | text | Canonical Base64 of the deterministic CBOR of the public COSE key; private parts are refused. |
| `notBefore` | timestamp | Optional: valid from. |
| `notAfter` | timestamp | Optional: valid until; not before `notBefore`. |

- Both arrays are in the ordinal order of the key identifications, a key at most once, and no key both a signing
  and an identity key. An array is written only when it is not empty.
- A key signs only a change set whose `createdAt` lies within its validity, and only in the version the change set
  is applied to.
- **Add:** the RNO adds the operator with at least one key of each kind and without charging pools, tariffs or
  groups: those are the operator's own to add. `RoamingNetworkChange.Add(operator)` leaves them out;
  `RoamingNetworkChange.AddChildren(operator)` gives the changes adding them, tariffs first and groups last, for a
  change set the operator signs once it is a member.
- **Remove:** the RNO removes the operator with its pools, stations, EVSEs, tariffs and groups, with one of its
  signing keys and without the operator's signature: the RNO decides who is a member of the roaming network.
  The history keeps the versions before.
- **Change:** `signingKeys` are rotated with a signature by one of the operator's current signing keys or, when none
  of them can sign any more, one of its identity keys; `identityKeys` only with one of its identity keys. For recovery,
  an identity key of the RNO changes both. Never removed or emptied: at least one key of each kind stays.

See [SPECIFICATION.md, 4.1](../SPECIFICATION.md#41-keys-and-owners).


## Data licenses

`dataLicenses`: the licenses of the operator's data, complete and in the ordinal order of their identifications,
each at most once.

| JSON key | Type | Description |
|---|---|---|
| `@id` | text | The identification, e.g. `CC BY 4.0`, `ODbL`, `dl-de/by-2-0`. |
| `description` | I18N text | Optional. |
| `URLs` | array of texts | Optional, in ordinal order. |

When read, `dataLicenseIds` may instead name well-known licenses by their identification alone: `None`, `PDDL`,
`ODC-By`, `ODbL`, `dl-de/by-2-0`, `dl-de/zero-2-0`, `GeoLizenz_V1.3`, `CC BY 4.0`, `CC BY-SA 4.0`, `CC BY-ND 4.0`,
`CC BY-NC 4.0`, `CC BY-NC-SA 4.0`, `CC BY-NC-ND 4.0`. Any other identification must be given completely. A change
set updates `dataLicenses` as a whole, or [element by element](README.md#element-changes); removing it, or updating it to `[]`, leaves no
`dataLicenses`.


## Contact

| JSON key | C# property | Type | Description |
|---|---|---|---|
| `telephone` | `Telephone` | `PhoneNumber?` | The telephone number of its (sales) office. |
| `eMailAddress` | `EMailAddress` | `SimpleEMailAddress?` | The e-mail address of its (sales) office. |
| `homepage` | `Homepage` | `URL?` | Its homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | The telephone number of its hotline for drivers. |
| `termsAndConditions` | `TermsAndConditionsURL` | `URL?` | The URL of its terms and conditions for charging. |

All are JSON texts; a value that is no text or can not be parsed is refused.


## Runtime status

See [Runtime status](README.md#runtime-status). The operator has an admin status and a status, no energy status;
both initially as of its creation. It does not aggregate the status of its pools.

| | Type | Initial | Predefined values |
|---|---|---|---|
| Admin status | `ChargingStationOperatorAdminStatusType` | `Operational` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` |
| Status | `ChargingStationOperatorStatusType` | `Available` | `unknown`, `unspecified`, `offline`, `inDeployment`, `available`, `error` |

Events: `OnAdminStatusChanged`, `OnStatusChanged`, with the old and new timestamped status. `OnDataChanged` exists
but is not raised: the operator is immutable.

In a history, `history.RuntimeStatus` sets and reads them as entity type `ChargingStationOperator`. A change set takes
the runtime status and internal data of the operator and of everything it embeds along (`ShareRuntimeOfAll`).


## Validation

- **Identification:** a valid `ChargingStationOperator_Id`, unique within the roaming network.
- **Keys:** no key twice, none both a signing and an identity key, each `@id` its thumbprint, no private parts,
  `notBefore` not after `notAfter`; added with at least one of each kind, and keeping at least one of each.
- **Data licenses:** no null entry, none twice; `dataLicenseIds` only of well-known licenses.
- **Logos:** an array of at most one object with `uri`.
- **Children:** given completely: `chargingPoolIds`, `chargingTariffIds`, `EVSEGroupIds`, `chargingStationGroupIds`,
  `chargingPoolGroupIds` and `chargingTariffGroupIds` are refused. No null entries.
- **Brands:** complete, each `@id` once; `brandIds` is refused. See [Brand](Brand.md#validation).
- **Belonging:** every pool, tariff and group names this operator in its identification.
- **Uniqueness within the operator:** pools, stations, EVSEs, energy meters, tariffs and each kind of group.
- **References within the operator:** every `chargingTariffIds` of an EVSE, every member and `chargingTariffId` of a group exists at it.
- **References within the network** (when the network is built or a change set applied): station manufacturers,
  grid operators of grid connection points,
  energy meter manufacturers, transparency software and certificates, parking spaces of stations; energy meters
  unique within the network.
- **Change sets:** `UpdateProperty`/`RemoveProperty` only of the properties marked above; `name` can not be removed;
  `Add` by the RNO with both kinds of keys and no children; `Remove` by the RNO. After every change the operator is
  read again with `TryParse`, so all rules above hold for the result.


## Example

`ToEmbeddedJSON()` of an operator with one brand, one pool, one tariff and one pool group; the key values are
abbreviated, real ones are 64 hex digits and the full Base64 of a COSE key.

```json
{
  "@id": "DE*GEF",
  "name": { "en": "GraphDefined", "de": "GraphDefined" },
  "description": { "en": "Charging in Jena" },
  "dataSource": "import-2026-10",
  "dataLicenses": [
    {
      "@id": "CC BY 4.0",
      "description": { "en": "Creative Commons Attribution 4.0 International" },
      "URLs": [ "http://creativecommons.org/licenses/by/4.0/", "http://creativecommons.org/licenses/by/4.0/legalcode" ]
    }
  ],
  "address": {
    "street": "Tatzendpromenade",
    "postalCode": "07745",
    "city": { "de": "Jena" },
    "country": "DEU",
    "houseNumber": "2"
  },
  "logos": [ { "uri": "https://example.org/gef/logo.svg" } ],
  "geoLocation": { "lat": 50.9175, "lng": 11.5734 },
  "telephone": "+49 3641 1234567",
  "eMailAddress": "info@example.org",
  "homepage": "https://example.org",
  "hotline": "+49 3641 7654321",
  "termsAndConditions": "https://example.org/terms",
  "brands": [
    { "@id": "white-charge", "name": { "en": "White Charge" }, "homepage": "https://white.example" }
  ],
  "chargingTariffs": [
    { "@id": "DE*GEF*TAC1", "currency": "EUR",
      "elements": [ { "priceComponents": [ { "type": "ENERGY", "price": 0.49, "stepSize": 1 } ] } ],
      "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "chargingPools": [
    { "@id": "DE*GEF*P1", "name": { "en": "Jena Paradies" },
      "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "chargingPoolGroups": [
    { "@id": "DE*GEF*PGJENA", "chargingPoolIds": [ "DE*GEF*P1" ], "chargingTariffId": "DE*GEF*TAC1",
      "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" }
  ],
  "signingKeys":  [ { "@id": "5b1c…", "key": "pQECIAEh…" } ],
  "identityKeys": [ { "@id": "e07a…", "key": "pQECIAMh…", "notAfter": "2031-01-01T00:00:00.000Z" } ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-10-12T12:00:00.000Z"
}
```
