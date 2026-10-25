# EMobilityProvider

An e-mobility provider (EMSP, eMSP) offers charging to EV drivers, e.g. by their contracts and authentication
tokens. In POI 3 it is a party of the roaming network: embedded completely in the
[RoamingNetwork](RoamingNetwork.md) under `eMobilityProviders`, in the order of its identification, with its
[groups and charging tariff offers](#groups-and-offers): the price its customers pay at the EVSEs of many operators.

It owns its own properties and signs their changes with one of its `signingKeys`; it rotates its
`signingKeys` with one of them, or with one of its `identityKeys` when none of them can sign any more, and its
`identityKeys` only with one of those; for recovery, an identity key of the roaming network operator (RNO) changes both. The RNO adds and
removes it. Its identification is an EMSP id, a country code and three letters or digits, e.g. `DE-GDF`
(also `DEGDF` or `DE*GDF`), unique within the roaming network.

| | |
|---|---|
| Class | `EMobilityProvider` — [source](../WWCP_POI3/RoamingNetwork/EMobilityProvider/EMobilityProvider.cs) |
| Identification | `EMobilityProvider_Id`, e.g. `DE-GDF` |
| Admin status | `EMobilityProviderAdminStatusType`, initially `operational` |
| Status | `EMobilityProviderStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `eMobilityProviders` |
| Owner | itself (signing key); its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO; added and removed by the RNO |
| HTTP | `GET ~/RNs/{RN_Id}/EMPs/{EMP_Id}` (`?without=` any of `EVSEGroups`, `chargingStationGroups`, `chargingPoolGroups`, `chargingTariffOffers`), `GET ~/RNs/{RN_Id}/EMPs/{EMP_Id}/{EVSEGs\|CSGs\|CPGs\|CTOs}` (each of them, searched and paged), and one of them below it |


## Common properties

See [Common properties](README.md#common-properties). It writes no `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — (set on `Add` unless given) |
| `lastChange` | — (set to the change set's `createdAt` by every change of the provider and of its groups and offers) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `dataLicenses` | `DataLicenses` | `ImmutableArray<DataLicense>` | update/remove | The licenses of its data, complete, ordered by identification; omitted when empty. As of a [charging station operator](ChargingStationOperator.md#data-licenses). |
| `address` | `Address` | `Address?` | update/remove | The address of its headquarters, as Styx writes it embedded (`street`, `houseNumber`, `postalCode`, `city`, `country` as ISO 3166 alpha-3, …). |
| `logos` | `Logo` | `URL?` | update/remove | Its logo: an array of at most one `{"uri": …}`, as of a [charging station operator](ChargingStationOperator.md#logos). |
| `geoLocation` | `GeoLocation` | `GeoCoordinate?` | update/remove | The geographical location of its headquarters, `{"lat", "lng"}`. |
| `telephone` | `Telephone` | `PhoneNumber?` | update/remove | The telephone number of its (sales) office. |
| `eMailAddress` | `EMailAddress` | `EMailAddress?` | update/remove | The e-mail address of its (sales) office, also with a name: `"Example Office <info@example.org>"`. |
| `homepage` | `Homepage` | `URL?` | update/remove | Its homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | The telephone number of its hotline, e.g. `+49 30 1234567`. |
| `termsAndConditions` | `TermsAndConditionsURL` | `URL?` | update/remove | The URL of its terms and conditions. |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update (signing or identity key) | Keys signing its change sets, see [Keys](#keys). |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update (identity key) | Long-term keys signing changes of its identity keys, see [Keys](#keys). |

| `EVSEGroups` | `EVSEGroups` | `POICollection<EVSEGroup_Id, EVSEGroup>` | add/remove | See [Groups and offers](#groups-and-offers). |
| `chargingStationGroups` | `ChargingStationGroups` | `POICollection<ChargingStationGroup_Id, ChargingStationGroup>` | add/remove | See [Groups and offers](#groups-and-offers). |
| `chargingPoolGroups` | `ChargingPoolGroups` | `POICollection<ChargingPoolGroup_Id, ChargingPoolGroup>` | add/remove | See [Groups and offers](#groups-and-offers). |
| `chargingTariffOffers` | `ChargingTariffOffers` | `POICollection<ChargingTariffOffer_Id, ChargingTariffOffer>` | add/remove | See [Groups and offers](#groups-and-offers). |

Children are written only when there are any; collections in the ordinal order of their identifications.

A change set names the properties above by their JSON keys (`EMobilityProviderRegistry` in
[RoamingNetwork.RegistryChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.RegistryChanges.cs)):

```csharp
RoamingNetworkChange.Add           (provider)                                          // signed by the RNO
RoamingNetworkChange.UpdateProperty(provider, "hotline", null, "+49 3641 1234567")     // signed by the provider
RoamingNetworkChange.Remove        (providerId)                                        // signed by the RNO
```


## Groups and offers

An e-mobility provider announces the price its customers pay at the EVSEs of many operators by
[charging tariff offers](ChargingTariffOffer.md), `chargingTariffOffers`, e.g. 39 ct/kWh at all EVSEs of one operator
and at its partner sites. Its offers name groups, of its own or of any operator, and whole operators. Its own groups,
[EVSE groups](EVSEGroup.md), [charging station groups](ChargingStationGroup.md) and
[charging pool groups](ChargingPoolGroup.md), name members of any operator; their identifications start with its
identification, e.g. `DE*GDF*PGPARTNER`, and are unique in the roaming network.

- **Owner:** it signs its groups and offers itself; they change nothing of an operator's, so no operator signs them.
  Within a change set, a group is the provider's while it has it: once an earlier change removed it, alone or with
  the provider, its identification names the group of the charging station operator of the same prefix again
  (`DE-GDF` and `DE*GDF`), which that operator signs, see [SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners).
- **Weak references:** a member, group or operator its groups and offers name that does not exist (any more) is left
  out, and its owner removes it without the provider. A group an offer of its own names can not be removed.
- **No overlap:** a change set of the provider must not make two of its offers overlap; offers coming to overlap by
  the changes of others are decided by their `notBefore`, see [no overlap](ChargingTariffOffer.md#no-overlap).
- **Add:** the RNO adds the provider without groups and offers: they are its own to add, with
  `RoamingNetworkChange.Add(group, providerId)` and `Add(offer, providerId)`, removed with `Remove(id)`.


## Keys

`signingKeys` and `identityKeys` are arrays of public keys (`POIPublicKey`,
[source](../WWCP_POI3/DataStructures/POIPublicKey.cs)), each written only when it is not empty:

| JSON key | Type | Description |
|---|---|---|
| `@id` | `String` | SHA-256 COSE key thumbprint [RFC 9679] of `key`, 64 lowercase hex digits. A reader calculates it and refuses a key whose `@id` differs. |
| `key` | `String` | The public COSE key as canonical Base64 of its deterministic CBOR; refused with private parts. |
| `notBefore` | timestamp | Optional: valid since then. |
| `notAfter` | timestamp | Optional: valid until then; not before `notBefore`. |

- Ordered by `@id` (ordinal); a key given twice is refused, and so is a key that is both a signing and an identity key.
- A signing key signs change sets of the provider's data and the rotation of its `signingKeys`; an identity key
  signs changes of `identityKeys`, and of `signingKeys` when none of them can sign any more. A key counts only if it
  is valid at the change set's `createdAt`, in the version the change set is applied to.
- A change replaces a whole array: `UpdateProperty(provider, "signingKeys", old, newArray)`, signed as above or, for
  recovery, by an identity key of the RNO. Removing an array, or updating it to an empty one, is refused.
- `Add` needs both arrays with at least one key. Parsing a network alone does not enforce this.
- See also [SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners).


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`EMobilityProvider`.

| Type | Predefined values | Initial |
|---|---|---|
| `EMobilityProviderAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` since `created` |
| `EMobilityProviderStatusType` | `unknown`, `unspecified`, `offline`, `inDeployment`, `available`, `error` | `available` since `created` |

Any other non-empty text parses as well. No energy status.

Events: `OnAdminStatusChanged` (`OnEMobilityProviderAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnEMobilityProviderStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, EMobilityProvider, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory: two letters of a known ISO 3166 country, an optional `-` or `*`, three letters or digits;
  read case-insensitively and written in upper case in the separator style it was read with. Two
  identifications differing only in the separator are the same provider.
- An e-mobility provider must not be given twice in a roaming network.
- `name`/`description` must be multi-language objects, `homepage`, `termsAndConditions` and the `uri` of its logo
  URLs, `telephone` and `hotline` phone numbers, `eMailAddress` an e-mail address, `address` and `geoLocation`
  objects, `logos` an array of at most one logo, `dataLicenses` licenses given completely, none twice.
- Keys: see [Keys](#keys).
- Groups and offers: given completely, none twice; group identifications naming this provider, unique in the roaming
  network, as offer identifications are; see [Groups and offers](#groups-and-offers).
- Change sets: only the properties listed above can be updated or removed; a change names no parent but the
  roaming network; the provider of an `Add` must not exist yet and must have the change's `entityId`.


## Example

```json
{
  "@id":          "DE-GDF",
  "name":         { "en": "GraphDefined Mobility" },
  "dataSource":   "registry import",
  "address":      { "street": "Musterstraße", "houseNumber": "1", "postalCode": "07743", "city": { "de": "Jena" }, "country": "DEU" },
  "logos":        [ { "uri": "https://mobility.example.org/logo.svg" } ],
  "telephone":    "+49 3641 7654321",
  "eMailAddress": "GraphDefined Mobility <service@mobility.example.org>",
  "homepage":     "https://mobility.example.org",
  "hotline":      "+49 3641 1234567",
  "termsAndConditions": "https://mobility.example.org/terms",
  "signingKeys":  [ { "@id": "0c5e…9a1f", "key": "…" } ],
  "identityKeys": [ { "@id": "7b21…e04d", "key": "…", "notAfter": "2031-01-01T00:00:00.000Z" } ],
  "created":      "2026-01-01T00:00:00.000Z",
  "lastChange":   "2026-10-12T12:00:00.000Z"
}
```

Key identifications and keys are abbreviated.
