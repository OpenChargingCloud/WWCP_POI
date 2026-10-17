# GridOperator

A grid operator (DSO) runs the electricity grid that charging pools are connected to. In POI 3 it is a party
of the roaming network: embedded completely in the [RoamingNetwork](RoamingNetwork.md) under `gridOperators`,
in the order of its identification, with its [grid connection points](#grid-connection-points) under
`gridConnectionPoints`, and the groups of their energy meters under `energyMeterGroups`. Charging pools name a grid
connection point by `gridConnectionPointId`.

A grid operator may have **no keys**: it does not take part (yet). Then the roaming network operator (RNO)
signs its changes in its place, and charging station operators may maintain its grid connection points
until it takes them over.

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
| Owner | itself (signing key), or the RNO while it has no signing keys; its signing keys: a signing or an identity key; its identity keys: an identity key; or, for recovery, an identity key of the RNO; added and removed by the RNO |
| Embeds | its grid connection points, `gridConnectionPoints`, each owned by its maintainer; the [energy meter groups](EnergyMeterGroup.md) of their energy meters, `energyMeterGroups`, its own |
| HTTP | `GET ~/RNs/{RN_Id}/GOs`, `GET ~/RNs/{RN_Id}/GOs/{GO_Id}` (`?without=gridConnectionPoints,energyMeterGroups`, or either of them, without them), `GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs` (them, searched and paged), `GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}`, each as it is at runtime, `?view=stored` as it embeds them; `GET ~/RNs/{RN_Id}/GOs/{GO_Id}/EMGs` and `…/EMGs/{EMG_Id}` |


## Common properties

See [Common properties](README.md#common-properties). It writes no `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — (set on `Add` unless given) |
| `lastChange` | — (set to the change set's `createdAt` by every change of the grid operator, of its grid connection points and of its energy meter groups) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `address` | `Address` | `Address?` | update/remove | Address of the headquarters, as Styx writes it embedded (`street`, `houseNumber`, `postalCode`, `city`, `country` as ISO 3166 alpha-3, …). |
| `homepage` | `Homepage` | `URL?` | update/remove | Homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | Hotline phone number. |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update (signing or identity key) | Keys signing its change sets, see [Keys](#keys). |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update (identity key) | Long-term keys signing changes of its identity keys, see [Keys](#keys). |
| `gridConnectionPoints` | `GridConnectionPoints` | `POICollection<GridConnectionPoint_Id, GridConnectionPoint>` | add/remove | Its [grid connection points](#grid-connection-points), in the ordinal order of their identifications. |
| `energyMeterGroups` | `EnergyMeterGroups` | `POICollection<EnergyMeterGroup_Id, EnergyMeterGroup>` | add/remove | [Groups](EnergyMeterGroup.md) of the energy meters of its grid connection points, whoever maintains them; owned by the grid operator, or by the RNO while it has no signing keys. |

**Derived (not serialized):** `EnergyMeters`, the meters of its grid connection points;
`TryGetGridConnectionPoint(id, out point)`; `EffectiveGridConnectionPoints` and
`TryGetEffectiveGridConnectionPoint(id, out point)`, its grid connection points as they are at runtime, their energy
meters with the properties of their groups; `TryGetEnergyMeterGroup(id, out group)` and
`EnergyMeterGroupsOf(energyMeterId)`.

A change set names the properties above by their JSON keys (`GridOperatorRegistry` in
[RoamingNetwork.RegistryChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.RegistryChanges.cs)):

```csharp
RoamingNetworkChange.Add           (gridOperator)                                           // signed by the RNO, with or without keys, without its points
RoamingNetworkChange.AddChildren   (gridOperator)                                           // its points, signed by their maintainers or the grid operator, then its energy meter groups
RoamingNetworkChange.UpdateProperty(gridOperator, "homepage", null, "https://netz.example.org")  // signed by the grid operator, or the RNO while it has no signing keys
RoamingNetworkChange.Remove        (gridOperatorId)                                         // signed by the RNO
```


## Grid connection points

A grid connection point describes where charging pools are connected to the public grid. It is no entity: it has
no status and no timestamps. It is embedded completely in its grid operator under `gridConnectionPoints`, and
charging pools name it by `gridConnectionPointId`; several pools, e.g. of one site, may name the same one.
Its electrical values are static, agreed ratings in base units; absent values are unknown. Live measurements
belong to its energy meter.

C# class `GridConnectionPoint` — [source](../WWCP_POI3/RoamingNetwork/GridOperator/GridConnectionPoint/GridConnectionPoint.cs).

| JSON key | Type | Change set | Description |
|---|---|---|---|
| `@id` | text | — | Mandatory. The identification assigned by the grid operator, any nonempty text, compared case-insensitively; unique within the roaming network. |
| `name` | I18N text | update/remove | |
| `description` | I18N text | update/remove | |
| `address` | object | update/remove | The address of the connection, as Styx writes it embedded. |
| `geoLocation` | object | update/remove | The geographical location of the connection. |
| `voltageLevel` | text | update/remove | `LowVoltage`, `MediumVoltage`, `HighVoltage` or `ExtraHighVoltage`; case-sensitive. |
| `connectionType` | text | update/remove | `Unknown`, `AC_SinglePhase`, `AC_SinglePhase_L0`, `AC_SinglePhase_L1`, `AC_SinglePhase_L2` or `AC_ThreePhases`; case-sensitive. |
| `nominalVoltage` | number | update/remove | V, line-to-line for three phases; positive. |
| `nominalFrequency` | number | update/remove | Hz; positive. |
| `contractedImportPower` | number | update/remove | W, the agreed active power taken from the grid; not negative. |
| `contractedExportPower` | number | update/remove | W, the agreed active power supplied to the grid; 0 means none. |
| `contractedImportApparentPower` | number | update/remove | VA; not negative, not less than `contractedImportPower`. |
| `contractedExportApparentPower` | number | update/remove | VA; not negative, not less than `contractedExportPower`. |
| `connectionAgreementId` | text | update/remove | The reference of the connection agreement with the grid operator; trimmed, not empty. |
| `networkLocationId` | text | update/remove | The network location, e.g. a German NeLo; trimmed, not empty. |
| `marketLocationIds` | array of texts | update/remove | Market locations, e.g. MaLos for consumption and generation; in their given order, each once, trimmed, not empty. |
| `meteringLocationIds` | array of texts | update/remove | Metering locations, e.g. MeLos; as `marketLocationIds`. |
| `energyMeter` | object | update/remove | The [energy meter](EnergyMeter.md) of the connection, 0..1; unique within the roaming network like every energy meter. |
| `maintainedBy` | `{entityType, entityId}` | update/remove (see below) | The party maintaining it: a `ChargingStationOperator` or `ParkingOperator` of the network; absent: the grid operator itself. |
| `customData` | object | update/remove | Written only when it has values. |

It has no timestamps: `created` and `lastChange` are refused; the grid operator embeds it
without naming itself, so `gridOperatorId` and `gridOperator` are refused.

### Maintainer

Exactly one party maintains a grid connection point and signs its changes: its grid operator, or the charging
station operator or parking operator `maintainedBy` names. While grid operators do not take part, charging
station operators keep publishing their grid connections this way; `maintainedBy` also tells everyone who vouches
for the data.

| Change | Signed by |
|---|---|
| `Add(point, gridOperatorId)` | the maintainer it names, or its grid operator |
| `UpdateProperty`/`RemoveProperty` of its data, `Remove(pointId)` | its maintainer |
| `UpdateProperty`/`RemoveProperty` of `maintainedBy` | its grid operator (taking it over, or handing it on), its maintainer (handing it on), or an identity key of the RNO |

"The grid operator" signs with its signing key, or the RNO in its place while it has no signing keys.
The grid operator takes a point over without the consent of its maintainer: `RemoveProperty(point, "maintainedBy")`.
The former maintainer then can no longer change it, and can not take it back. The versions it maintained stay
in the history.

```csharp
RoamingNetworkChange.Add           (new GridConnectionPoint(id, MaintainedBy: new POIPartyId("ChargingStationOperator", "DE*GEF")),
                                    gridOperatorId)                                         // signed by DE*GEF
RoamingNetworkChange.UpdateProperty(point, "contractedImportPower", null, 200000)          // signed by its maintainer
RoamingNetworkChange.RemoveProperty(point, RoamingNetwork.MaintainedByProperty)           // signed by the grid operator: taken over
```

- **Identification:** unique within the roaming network, not only within its grid operator, so that a change
  and a pool find it by its identification alone. Adding a point names its grid operator as the parent.
- **References:** every `gridConnectionPointId` of a pool must name a grid connection point of the network, when
  the network is created and after every change set: a point a pool names can not be removed, and neither can its
  grid operator. A maintainer must be a charging station operator or parking operator of the network.
- **Merges** compare it property by property, like every entity of a change set.
- **HTTP:** `GET ~/RNs/{RN_Id}/GOs/{GO_Id}/GCPs/{GCP_Id}` returns it as its grid operator embeds it;
  `GET …/CPs/{CP_Id}?expand=gridConnectionPoint` returns a pool with its point embedded, together with
  `gridOperatorId`, in the place of `gridConnectionPointId`, as another representation with its own ETag.


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
- `Add` needs both arrays with at least one key, or, for a grid operator not taking part, neither. Parsing a
  network alone does not enforce this. The RNO gives a grid operator without keys its keys with one of its
  identity keys; from then on, the grid operator signs itself.
- See also [SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners).


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`GridOperator`.

| Type | Predefined values | Initial |
|---|---|---|
| `GridOperatorAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` since `created` |
| `GridOperatorStatusType` | `unknown`, `unspecified`, `offline`, `inDeployment`, `available`, `error` | `available` since `created` |

Any other non-empty text parses as well. No energy status.

Events: `OnAdminStatusChanged` (`OnGridOperatorAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnGridOperatorStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, GridOperator, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory: two letters of a known ISO 3166 country, an optional `*`, three letters or digits; read
  case-insensitively, always written upper case with `*`.
- A grid operator must not be given twice in a roaming network.
- Grid connection points: none twice in the roaming network (case-insensitively); every `gridConnectionPointId`
  of a pool names one of them; a `maintainedBy` names a charging station operator or parking operator of the network.
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
  "gridConnectionPoints": [
    {
      "@id":                           "GCP-0815",
      "voltageLevel":                  "LowVoltage",
      "connectionType":                "AC_ThreePhases",
      "nominalVoltage":                400,
      "nominalFrequency":              50,
      "contractedImportPower":         110000,
      "contractedImportApparentPower": 120000,
      "marketLocationIds":             [ "50123456789" ],
      "maintainedBy":                  { "entityType": "ChargingStationOperator", "entityId": "DE*GEF" }
    }
  ],
  "signingKeys":  [ { "@id": "3d90…7c2b", "key": "…" } ],
  "identityKeys": [ { "@id": "e84a…15f6", "key": "…" } ],
  "created":      "2026-01-01T00:00:00.000Z",
  "lastChange":   "2026-01-01T00:00:00.000Z"
}
```

Key identifications and keys are abbreviated.
