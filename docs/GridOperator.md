# GridOperator

A grid operator (DSO) runs the electricity grid that charging pools are connected to. In POI 3 it is a party
of the roaming network: embedded completely in the [RoamingNetwork](RoamingNetwork.md) under `gridOperators`,
in the order of its identification, without children of its own. Grid connection points of charging pools
name it by `gridOperatorId`.

It owns its own properties and signs their changes with one of its `signingKeys`; it rotates its
`signingKeys` with one of them, or with one of its `identityKeys` when none of them can sign any more, and its
`identityKeys` only with one of those; for recovery, an identity key of the roaming network operator (RNO) changes both. The RNO adds and
removes it. Its identification is a country code and three letters or digits, written with `*`, e.g. `DE*SWJ`,
unique within the roaming network.

| | |
|---|---|
| Class | `GridOperator` — [source](../WWCP_POI3/RoamingNetwork/GridOperator/GridOperator.cs) |
| Identification | `GridOperator_Id`, e.g. `DE*SWJ` |
| Admin status | `GridOperatorAdminStatusType`, initially `operational` |
| Status | `GridOperatorStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `gridOperators` |
| Owner | itself (signing key); its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO; added and removed by the RNO |
| HTTP | `GET ~/RNs/{RN_Id}/GOs/{GO_Id}`, `GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}` |


## Common properties

See [Common properties](README.md#common-properties). It writes no `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — (set on `Add` unless given) |
| `lastChange` | — (set to the change set's `createdAt`) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `address` | `Address` | `Address?` | update/remove | Address of the headquarters, as Styx writes it embedded (`street`, `houseNumber`, `postalCode`, `city`, `country` as ISO 3166 alpha-3, …). |
| `homepage` | `Homepage` | `URL?` | update/remove | Homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | Hotline phone number. |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update (signing or identity key) | Keys signing its change sets, see [Keys](#keys). |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update (identity key) | Long-term keys signing changes of its identity keys, see [Keys](#keys). |

A change set names the properties above by their JSON keys (`GridOperatorRegistry` in
[RoamingNetwork.RegistryChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.RegistryChanges.cs)):

```csharp
RoamingNetworkChange.Add           (gridOperator)                                           // signed by the RNO
RoamingNetworkChange.UpdateProperty(gridOperator, "homepage", null, "https://netz.example.org")  // signed by the grid operator
RoamingNetworkChange.Remove        (gridOperatorId)                                         // signed by the RNO
```


## Grid connection points

A grid connection point describes where a charging pool is connected to the public grid. It is no entity and
not part of the grid operator: it is embedded in its [ChargingPool](ChargingPool.md) as `gridConnectionPoint`
(0..1) and fully documented there, see [GridConnectionPoint](ChargingPool.md#grid-connection-point).

- It names its grid operator by `gridOperatorId`, which must be a grid operator of the roaming network, when
  the network is created and after every change set. A grid operator that a grid connection point names can
  therefore not be removed.
- It is a property of the pool: it changes with `UpdateProperty(pool, "gridConnectionPoint", …)`, signed by the
  charging station operator of the pool, not by the grid operator.
- `GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}` returns the grid connection point with that identification
  naming that grid operator, searching the pools of the charging station operators in the order of their
  identifications; the first one found is returned, 404 if none.


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
- A signing key signs change sets of the grid operator's data and the rotation of its `signingKeys`; an identity
  key signs changes of `identityKeys`, and of `signingKeys` when none of them can sign any more. A key counts only
  if it is valid at the change set's `createdAt`, in the version the change set is applied to.
- A change replaces a whole array: `UpdateProperty(gridOperator, "signingKeys", old, newArray)`, signed as above or,
  for recovery, by an identity key of the RNO. Removing an array, or updating it to an empty one, is refused.
- `Add` needs both arrays with at least one key. Parsing a network alone does not enforce this.
- See also [SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners).


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`GridOperator`.

| Type | Predefined values | Initial |
|---|---|---|
| `GridOperatorAdminStatusType` | `unknown`, `unspecified`, `outOfService`, `operational`, `internalUse` | `operational` since `created` |
| `GridOperatorStatusType` | `unknown`, `unspecified`, `offline`, `inDeployment`, `available`, `error` | `available` since `created` |

Any other non-empty text parses as well. No energy status.

Events: `OnAdminStatusChanged` (`OnGridOperatorAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnGridOperatorStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, GridOperator, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory: two letters of a known ISO 3166 country, an optional `*`, three letters or digits; read
  case-insensitively, always written upper case with `*`.
- A grid operator must not be given twice in a roaming network.
- Every `gridOperatorId` of a grid connection point must name a grid operator of the network.
- `name`/`description` must be multi-language objects, `homepage` a URL, `hotline` a phone number,
  `address` an object.
- Keys: see [Keys](#keys).
- Change sets: only the properties listed above can be updated or removed; a change names no parent but the
  roaming network; the grid operator of an `Add` must not exist yet and must have the change's `entityId`.


## Example

```json
{
  "@id":          "DE*SWJ",
  "name":         { "de": "Netze Beispielstadt" },
  "address":      { "street": "Netzweg", "houseNumber": "5", "postalCode": "07745", "city": { "de": "Jena" }, "country": "DEU" },
  "homepage":     "https://netz.example.org",
  "hotline":      "+49 3641 7654321",
  "signingKeys":  [ { "@id": "3d90…7c2b", "key": "…" } ],
  "identityKeys": [ { "@id": "e84a…15f6", "key": "…" } ],
  "created":      "2026-01-01T00:00:00.000Z",
  "lastChange":   "2026-01-01T00:00:00.000Z"
}
```

Key identifications and keys are abbreviated.
