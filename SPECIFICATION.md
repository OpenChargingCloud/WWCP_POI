# WWCP POI 3: content, ETags, commits and signatures

This document specifies which bytes POI 3 hashes and signs, so that a client can check a version,
a commit or a signature without this library. It describes POI 3 as implemented; the examples in
section 6 are checked by `WWCP_POI3_Tests/SpecificationVectorsTests.cs`.

The words MUST, MUST NOT, SHOULD and MAY are used as in RFC 2119.


## 1. Canonical JSON and deterministic CBOR

POI uses the two byte representations of a JSON document that
[Styx specifies](https://github.com/Vanaheimr/Styx/blob/master/Styx/Illias/JSON/README.md):

- **canonical JSON**: properties ordered by UTF-16 code units, no whitespace, decimals keep their
  decimal places (`1.50` stays `1.50`), Styx's string escaping;
- **deterministic CBOR**: the CBOR of the JSON document, RFC 8949 core deterministic encoding,
  decimals with decimal places as decimal fractions (tag 4), numbers without decimal places as
  integers.

Whoever reads JSON in order to hash it again MUST read numbers with a fraction as decimals and
strings as strings, never as points in time.


## 2. The content of a version

A version of a roaming network is identified by its **content**: its JSON representation as
`RoamingNetwork.ToJSON(Embedded: false, IncludeETags: false)` writes it.

- **Included:** `@context`
  (`https://open.charging.cloud/contexts/wwcp+json/roamingNetwork`), `@id`, `name`, `description`,
  `dataSource`, `dataLicenses`, its operator `roamingNetworkOperator`, every embedded entity completely (operators with their pools,
  stations, EVSEs, connectors, energy meters, tariffs and groups; manufacturers, e-mobility
  providers, grid operators with their grid connection points, parking operators, energy meter
  manufacturers, transparency software releases and certificates), the brands of operators, pools,
  stations, EVSEs and RFID authentication modes, the `signingKeys` and `identityKeys` of its
  operator and of its parties, `created`, `lastChange` and `customData`.
- **Excluded:** `eTags`; the runtime status (admin status, status, energy status); the local
  revision counter; internal data.
- **Order:** embedded entities in the ordinal order of their identifications; the order of
  properties does not matter, as canonical JSON sorts them.
- **Timestamps:** UTC to the millisecond, `yyyy-MM-ddTHH:mm:ss.fffZ`.
- **Numbers:** physical quantities as decimals in base units (V, A, W, Hz, VA, Wh, s), prices as
  decimals.
- **Absent values** are left out. Some arrays are written also when empty, as empty means
  something there (e.g. `energySources: []`, unknown); an implementation MUST keep arrays as it
  read them.
- **Properties given by groups** are part of the content where they are given: an EVSE, charging station,
  charging pool or parking space group may give its members their properties, which they take at runtime only (the
  *effective* members; a single value is the member's own, else the one of its group with the highest
  `priority`, 0 unless given; arrays are united). The content, the ETags of the network and of its
  entities, change sets and merges know them only in the group. A version whose groups of one member give
  it different values with the same priority, where these decide, or make it invalid, is invalid.


## 3. ETags

An ETag is a digest of the content in one of two formats.

| Format | Digest over |
|---|---|
| `json` | the canonical JSON of the content (UTF-8) |
| `cbor` | the deterministic CBOR of the content |

### 3.1 Representations

- **JSON:** `["json", "sha256", "hex", "<64 lowercase hex digits>"]`; the encoding MAY also be
  `base64`, with canonical padding, except in change sets (their `beforeETags`, `afterETags` and
  preconditions) and commits: these MUST write ETags in hex, and a reader MUST refuse others, as their
  signatures and identifications are calculated over them as written.
- **CBOR:** `[format, "sha256", h'<digest>']`, e.g. `["cbor", "sha256", h'291e…']`, the digest as a byte string.
- **Text:** `json:sha256:hex:<digest>`.
- **Algorithms:** `sha256` (default), `sha384`, `sha512`.
- A roaming network lists its ETags in `eTags`, ordered by format: `json`, then `cbor`.
- A reader MUST refuse a roaming network whose `eTags` are not the ETags of its content, and one with another
  `@context`. Both MAY be left out.

### 3.2 The CBOR ETag

The deterministic CBOR follows from the canonical JSON alone: a client that knows only the JSON
document can check both ETags. A number the model holds as a decimal without decimal places,
e.g. `maxPower` 22000 W, is the integer `22000` in CBOR as in JSON; `maxVoltage` 230.5 V is the
decimal fraction `4([-1, 2305])`.


## 4. Change sets

A change set leads from the version of its `beforeETags` to the version of its `afterETags`:

```json
{ "@id": "…", "roamingNetworkId": "rn1", "createdAt": "2026-10-12T12:00:00.000Z",
  "description": { "en": "…" }, "metadata": { },
  "changes": [ { "kind": "UpdateProperty", "entityType": "EVSE", "entityId": "DE*GEF*E1*1",
                 "propertyName": "maxPower", "oldValue": 22000, "newValue": 11000 } ],
  "preconditions": [ { "entityType": "ChargingPool", "entityId": "DE*GEF*P1", "eTag": [ "json", "sha256", "hex", "…" ] } ],
  "beforeETags": [ … ], "afterETags": [ … ], "signatures": [ … ] }
```

**Preconditions** (optional, written only when there are any, signed with the change set): the ETag an
entity MUST still have for the change set to apply, or to be rebased onto another version. The ETag of an
entity is the one of its representation as the roaming network embeds it, with everything it embeds: over
its canonical JSON (section 1), or the canonical CBOR of its keys and values, with the hash algorithm the
ETag names; the HTTP `ETag` an entity is served with as it is given (POI_API: `?view=stored` for charging
pools, stations, EVSEs and parking spaces, which it serves with the properties their groups give them by
default, telling this ETag as `X-Stored-ETag` as well). The entity
is found by its type and identification, compared as the type compares it. A change set whose preconditions
do not all hold in the version it is applied to MUST be refused; a rebased change set keeps them. They hold
in a merge as well (section 5.3): a change set committed for a version older than a tip it will be merged
with MUST be refused where that tip changed or removed an entity it names since their common ancestor.

**Kinds:** `Add` and `Remove` of an entity; `UpdateProperty` and `RemoveProperty` of a property of an entity;
and the element changes `AddElement`, `RemoveElement`, `ReplaceElement`, `UpdateElementProperty` and
`RemoveElementProperty` of one element of a property, named by `elementPath`, an array of
`{ "propertyName", "elementId"? }`:

- the first segment names a property of the entity, every further one a property of the value the segment before
  names. A segment on an array MUST name an element by `elementId`: the `@id` text of an object, a text itself,
  compared as the identification type of the property compares them, any other ordinally: `EVSEIds`, `chargingStationIds`, `chargingPoolIds`, `chargingTariffIds`, `parkingSpaceIds`, `parkingSensorIds`, `parkingProductIds` and `eMobilityProviderIds` hold identifications of their entity types, `brands`, `chargingConnectors` and `energyMeters` objects with identifications of theirs;
  every element of the array MUST have one and no two the same, else the change is refused.
  A segment on any other value MAY name its `@id`, which it then MUST have;
- `AddElement` names an element that does not exist yet, or an absent property, and its `newValue` MUST have the
  `elementId` of the segment; it goes where the ordinal order of the identifications puts it, when the array is in
  that order, otherwise at its end. `ReplaceElement` keeps the identification. `UpdateElementProperty` and
  `RemoveElementProperty` change `propertyName` of the object named, never `@id`. An array left empty is removed;
- an element change MUST be checked as the `UpdateProperty`, or `RemoveProperty` when nothing is left, of the
  property its first segment names: owners (section 4.1), changeable properties, the result. The `signingKeys` and
  `identityKeys` of a party are not changed element by element;
- its `oldValue` is the element, or the property of the element, it names (JSON `null`: absent); `AddElement` has
  none.

**Old values:** a change whose `oldValue` is not the value it finds MUST be refused. Both are compared as their
canonical JSON (section 1): the number `22000` read from a text is the decimal 22000 without decimal places a
reader holds, and `22000.0` is another value.

**Timestamps:** a change set sets `lastChange` to its `createdAt`: of the roaming network always; of an entity
when it is added (and `created`, unless given) or when one of its own properties changes; not when only an entity
it embeds changes. `lastChange` therefore does not tell when anything below an entity last changed: its
representation, and so its ETag, changes with everything it embeds, and the history tells what changed.
Grid connection points, brands (embedded in operators, pools, stations, EVSEs and RFID authentication modes) and
parking products have no timestamps.

**Signatures** (profile `wwcp-poi-changeset-json-v3`) of the owners of what it changes
(section 4.1): `{ algorithm, keyId, value, profile, encoding: "base64" }`.
The signature input is the canonical JSON of

```json
{ "profile": "wwcp-poi-changeset-json-v3", "algorithm": "…", "keyId": "…", "encoding": "base64",
  "changeSet": <the change set without its signatures> }
```

Algorithms are the COSE names of Styx: `Ed25519`, `Ed448`, `EdDSA`, `ES256`, `ES384`, `ES512`,
`ES256K`, `ESP256`, `ESP384`, `ESP512`, `ESB256`, `ESB320`, `ESB384`, `ESB512`, `ML-DSA-44`,
`ML-DSA-65` and `ML-DSA-87`. ECDSA is signed deterministically (RFC 6979). A verifier MUST refuse
an unknown profile, an encoding other than `base64`, non-canonical Base64, an algorithm name that
is not canonical, and a key identification other than the one of the trusted key.

### 4.1 Keys and owners

A **key** of a party is a public COSE key in its static data:

```json
{ "@id": "<SHA-256 COSE key thumbprint [RFC 9679], 64 lowercase hex digits>",
  "key": "<Base64 of the deterministic CBOR of the public COSE key>",
  "notBefore": "…", "notAfter": "…" }
```

A reader MUST calculate the thumbprint and refuse a key whose `@id` differs, and a key with
private parts. The roaming network operator (RNO), charging station operators,
e-mobility providers, grid operators and parking operators each name 1..n `signingKeys`, e.g.
secp256r1, and 1..n `identityKeys`, e.g. secp521r1; a key is not both. A grid operator not taking
part names neither. The RNO is the `roamingNetworkOperator` the roaming network embeds, the party
`{"entityType": "RoamingNetworkOperator", "entityId": its @id}`; the roaming network names no keys
of its own, and a reader MUST refuse one that does. The `keyId` of a
signature is the `@id` of the key.

Every change has exactly one **owner**:

| Change | Owner |
|---|---|
| the RNO itself, the roaming network itself, charging station and energy meter manufacturers, transparency software releases and certificates | the RNO |
| `Add` and `Remove` of a charging station operator, e-mobility provider, grid operator or parking operator | the RNO |
| the other properties of such a party | the party; the RNO for a party without signing keys |
| pools, stations, EVSEs, tariffs and groups | the charging station operator their identification names |
| brands | the owner of what embeds them: the charging station operator |
| parking garages, spaces, sensors, space groups and products | their parking operator (the parent of an `Add`) |
| a grid connection point | its maintainer: the party its `maintainedBy` names, else its grid operator; an `Add` also its grid operator |
| `maintainedBy` of a grid connection point | its grid operator, its maintainer, or an identity key of the RNO |

"Its grid operator" signs with its signing key, or the RNO in its place while it has none. A
grid connection point's `maintainedBy` names a charging station operator or parking operator of
the network; its identification is unique within the network.

A change set MUST be refused unless, for every owner of one of its changes, it has a valid
signature with a key that owner names **in the version the change set is applied to**, valid at
its `createdAt`: a signing key for data; for the `signingKeys` of a party one of its signing keys
or one of its identity keys; for its `identityKeys` one of its identity keys. For recovery, an
identity key of the RNO also counts for both kinds of keys of every other party. Every signature
MUST be by a key of a party. A party is added with at least one key of each kind, a grid operator
also with none, and without embedded children, and keeps at least one of each once it has them. The RNO decides who is a member: removing a
party removes everything it embeds, and needs no signature of the party. The RNO itself is
neither added nor removed: a change set that does MUST be refused. A roaming network without an
RNO can not be changed where only the RNO may.

Nobody can replace the identity keys of the RNO once they are lost or expired. An RNO SHOULD name
at least two identity keys with different `notAfter`, and SHOULD replace an identity key before it
expires.


## 5. Commits

A commit is one step of the history: a change set applied to the version of its first parent.
Every server may commit; there is no leader. The history is a graph: a commit has no parents
(genesis), one, or two (merge).

### 5.1 Identification

The commit identification is the SHA-256 of the canonical JSON of

```json
{ "profile": "wwcp-poi-commit-v2", "roamingNetworkId": "rn1", "revision": 1,
  "eTags": [ <the ETags of the resulting version> ],
  "parentIds": [ "<first parent commit id>", "<second one, for a merge>" ],
  "changeSet": <the change set without its signatures> }
```

as 64 lowercase hexadecimal digits. A genesis commit has neither `parentIds` nor `changeSet`.
The `revision` is one more than the highest revision of the parents, 0 for a genesis commit.
Signatures are not part of it: neither those of the change set nor those of the commit, so that
they can be added later without changing the identification.

### 5.2 Representation

```json
{ "@id": "<commit id>", "roamingNetworkId": "rn1", "revision": 1, "eTags": [ … ],
  "parentIds": [ "…" ], "changeSet": <the change set with its signatures>,
  "signatures": [ <commit signatures> ] }
```

A reader MUST calculate the identification and refuse the commit when it differs from `@id`.
The CBOR representation has the keys and values of the JSON one.

A commit with one parent MUST be refused unless its change set fits the version of its parent,
is signed by its owners (section 4.1) and leads to its ETags.

### 5.3 Merges

The **tips** of a history are the commits no other commit follows, in their **order**: the higher
revision first, among equal revisions the lower identification (ordinal). A server merges the
first two tips that do not conflict, again and again. Their **merge commit**:

- `parentIds`: the two tips in their order; its change set leads from the version of the first one;
- the change set has the `@id` `merge-` followed by the first 32 hex digits of the SHA-256 of
  `"<first parent id>+<second parent id>"` (UTF-8), the `createdAt` of the later `lastChange` of
  both versions, the description `{"en":"Merge"}`, no metadata and no signatures;
- its changes follow from a three-way merge with the **merge base**, the best common ancestor
  (one no other common ancestor follows); several best common ancestors form a **virtual merge
  base** (below).

Two tips also conflict where a change set of the one since the merge base has a precondition
(section 4) naming an entity the other one changed or removed since the merge base: its full
representation is other than in the merge base, or it is gone. Such a change set applied only while
the entity was as it read it; the owner decides, with a change set on one of the tips.

Several best common ancestors, in the order of tips, are merged one after the other: the first
with the second, with their own merge base (calculated the same way), the result with the third,
and so on. This virtual merge base compares like the three-way merge below, but never conflicts:
where they conflict, it has the **conflict marker** `{"@conflict": {"base": …, "left": …, "right": …}}`
(absent values left out), which equals no value of a version, so that the merge of the tips conflicts
there unless both tips have the same:

- a property both changed differently has the conflict marker as its value;
- an entity one of them removed and the other one changed, or moved by both, or moved by one and
  changed by the other, has the conflict marker of its properties as its only property, under the
  parent of the first one having it;
- otherwise an entity both have under the same parent takes the merged properties, unless it is a
  conflict marker as a whole in their own merge base: then it is one again, unless both have it the
  same; one moved by one of them only, unchanged by the other, is as the one that moved it has it; one
  only one of them has is kept, unless the other one removed it unchanged.

An entity that is a conflict marker as a whole in the merge base conflicts as a whole: the tips
conflict there, unless both have it the same, and it is merged neither property by property nor
element by element, as the absent properties of the marker are no values of the common ancestors.

The three-way merge compares every entity a change set addresses (section 4.1), by its type and
identification, without its embedded children and without `lastChange`, property by property,
absent being a value of its own. Identifications are compared as their type compares them, not as
texts: `DE-ABC` and `DE*ABC` are the same e-mobility provider, most identifications are the same
without case; `@id` itself is therefore not compared, an entity keeps it as the first version writes
it, and an added entity names its parent as the second version writes it:

- a property only one version changed takes that change; both changed it alike, it is kept once;
  both changed it differently, they **conflict**, unless it is an array of identified elements (section 4)
  in all three, an absent one counting as empty (but not in an entity that is a conflict marker as a whole,
  see above), and its keys are not those of a party: then the merge is
  the first version's array with the changes of the second one against the base, element by element,
  taken in the order of the second one: an element it removed is removed, unless the first one changed it; one it
  added is added (section 4), unless the first one added it differently; one it changed is replaced, unless
  the first one changed it, too: then objects merge property by property, each as a whole, else they
  conflict; the second one changing what the first one removed conflicts. A virtual merge base merges
  arrays the same way;
- an entity only one version added is added, with what it embeds, unless the other one removed
  where it is added; one removed is removed, unless the other one changed it; both added it with
  different properties, they conflict; moved to another parent by one version only, it is
  removed and added again, unless the other one changed it.

The changes are written in this order: `Add` (of the topmost added entities, with what they
embed, in the order of the second version), the changes of properties (in the order of the first
version, properties in the order of the union of their names), removals and additions of moved
entities, `Remove` (of the topmost removed entities). A property whose old and new values are both
arrays of identified elements (absent counting as empty), except the keys of a party, is changed by
`RemoveElement` of the removed elements (in their old order), then `AddElement` of the added ones and
`UpdateElementProperty`/`RemoveElementProperty` of the changed objects, property by property in the
order of the union of their names, or `ReplaceElement` of other changed elements (in the new order),
unless these are more than the elements of the new value, or applied in this order they do not give
exactly the new value; then, as every other property, by `UpdateProperty`/`RemoveProperty`. Their
`oldValue` is the value they find. A merge commit MUST be refused unless it is the merge of its parents as calculated here,
exactly. Conflicting tips stay until an owner commits a change set to one of them that resolves
the conflict, e.g. taking the value of the other one.

### 5.4 Signatures

A commit can be signed by several parties, each on its own, in any order: the server that made
it, a roaming network operator that received the data of a charging station operator and checked
it, the charging station operator itself, or others.

```json
{ "role": "roamingNetworkOperator", "signer": "DE*RNO", "signedAt": "2026-10-12T13:00:00.000Z",
  "algorithm": "Ed25519", "keyId": "rno", "value": "<Base64>",
  "profile": "wwcp-poi-commit-signature-v1", "encoding": "base64" }
```

- **role:** `leader`, `roamingNetworkOperator`, `chargingStationOperator` or another one.
- **signer:** optional, e.g. the identification of an operator.
- **signedAt:** MUST be written as `yyyy-MM-ddTHH:mm:ss.fffZ`, as it is signed as text.

The signature input is the canonical JSON of

```json
{ "profile": "wwcp-poi-commit-signature-v1", "algorithm": "…", "keyId": "…", "encoding": "base64",
  "role": "…", "signer": "…", "signedAt": "…",
  "commit": <the commit with the signatures of its change set, without the signatures of the commit> }
```

So every signature covers the commit identification, the ETags of the resulting version, the
change set and **the signatures of the change set**: whoever signs a commit confirms the data
together with who signed it. The verification rules of section 4 apply.

A server MUST verify every signature of a commit before it takes the commit,
and MAY require signatures in certain roles. Signatures added later are sent with
`signCommit` over the WebSocket or `POST …/history/commits/{commitId}/signatures`, and pushed
on the topic `commitSignatures`.


## 6. Examples

A roaming network `rn1` named "Example", created 2026-01-01T00:00:00Z, with one operator `DE*GEF`,
one pool, one station and one EVSE `DE*GEF*E1*1` of 22000 W and 230.5 V.

**Content** (canonical JSON):

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork","@id":"rn1","chargingStationOperators":[{"@id":"DE*GEF","chargingPools":[{"@id":"DE*GEF*P1","chargingStations":[{"@id":"DE*GEF*S1","EVSEs":[{"@id":"DE*GEF*E1*1","created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z","maxPower":22000,"maxVoltage":230.5}],"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z"}],"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z"}],"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z","name":{"en":"Charging Station Operator DE*GEF"}}],"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z","name":{"en":"Example"}}
```

**ETags:**

```
json:sha256:hex:8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224
cbor:sha256:hex:291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549
```

**Genesis commit:** the SHA-256 of

```json
{"eTags":[["json","sha256","hex","8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224"],["cbor","sha256","hex","291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549"]],"profile":"wwcp-poi-commit-v2","revision":0,"roamingNetworkId":"rn1"}
```

is the commit identification `d48066e0a5e68f9019c8c2e0a911ff7d99e2ff054a79e3343e5de2ea14e33117`.

**Leader signature** of the genesis commit, with the Ed25519 key of the private seed
`101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f` (public key, Base64:
`d3bocLkzVPKgskwj8qNsxOgOIjIYwbl5Jv3QGDlqK5s=`), key identification `leader`, signer
`leader.example.org`, signed at 2026-10-12T12:00:00.000Z. Signature input:

```json
{"algorithm":"Ed25519","commit":{"@id":"d48066e0a5e68f9019c8c2e0a911ff7d99e2ff054a79e3343e5de2ea14e33117","eTags":[["json","sha256","hex","8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224"],["cbor","sha256","hex","291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549"]],"revision":0,"roamingNetworkId":"rn1"},"encoding":"base64","keyId":"leader","profile":"wwcp-poi-commit-signature-v1","role":"leader","signedAt":"2026-10-12T12:00:00.000Z","signer":"leader.example.org"}
```

Signature value:

```
uGmXUXrvziqRwcQIDE5C7ew7Rv1HPJ2vzqHwL+181Z7SWohOuOUEHvDWmrb7HaRyJxsbuFOsruTYOrTLoHFLAA==
```


## 7. Open points

None: the HTTP `ETag` of an entity is the SHA-256 of its canonical representation as the roaming network
embeds it, and change sets name it as a precondition (section 4).
