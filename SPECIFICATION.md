# WWCP POI 3: content, ETags, commits and signatures

This document specifies which bytes POI 3 hashes and signs, so that a client can check a version,
a commit or a signature without this library, and what a version tells where it gives something once
for many entities: templates, groups and offers (section 9). The examples in section 6 are checked by
`WWCP_POI3_Tests/SpecificationVectorsTests.cs`. Section 8 is a draft: proofs of single entities, e.g. of
one charging station, by a signed hash tree.

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
  stations, EVSEs, connectors, energy meters, tariffs, templates, groups and offers; manufacturers;
  e-mobility providers with their groups and offers; grid operators with their grid connection points,
  energy meter templates and groups; parking operators; energy meter manufacturers, transparency
  software releases and certificates), the brands of operators, pools,
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
- **Properties given by templates** are part of the content where they are given: an EVSE, charging
  station, charging pool, charging tariff, parking space or energy meter names its templates in
  `templateIds`, in an order that is content, and takes their properties at runtime only (the
  *effective* members, section 9.2). The content, the ETags of the network and of its entities, change
  sets and merges know them only in the template. A version whose templates make an effective member
  invalid, or whose offers overlap (section 9.4), is invalid.


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
ETag names; the HTTP `ETag` an entity is served with as it is given (POI_API: `?view=stored` for what it
serves as it is at runtime by default, with the properties its templates give it, section 9.2, telling
this ETag as `X-Stored-ETag` as well). The entity
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
  compared as the identification type of the property compares them, any other ordinally: `EVSEIds`, `chargingStationIds`, `chargingPoolIds`, `chargingTariffIds`, `energyMeterIds`, `parkingSpaceIds`, `parkingSensorIds`, `parkingProductIds`, `eMobilityProviderIds`, `chargingStationOperatorIds`, `EVSEGroupIds`, `chargingStationGroupIds`, `chargingPoolGroupIds` and `templateIds` hold identifications of their entity types, `brands`, `chargingConnectors` and `energyMeters` objects with identifications of theirs, `signatureMethods` signature methods (without case);
  every element of the array MUST have one and no two the same, else the change is refused.
  A segment on any other value MAY name its `@id`, which it then MUST have;
- `AddElement` names an element that does not exist yet, or an absent property, and its `newValue` MUST have the
  `elementId` of the segment; it goes where the ordinal order of the identifications puts it, when the array is in
  that order, otherwise at its end; to `templateIds`, whose order is content, always at its end. `ReplaceElement`
  keeps the identification. `UpdateElementProperty` and
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
| pools, stations, EVSEs and tariffs | the charging station operator their identification names |
| templates, groups and charging tariff offers | the party embedding them (the parent of an `Add`): a charging station operator, an e-mobility provider (its groups and offers), a grid operator (its energy meter templates and groups) or a parking operator (its parking space templates and groups) |
| brands | the owner of what embeds them: the charging station operator |
| parking garages, spaces, sensors and products | their parking operator (the parent of an `Add`) |
| a grid connection point | its maintainer: the party its `maintainedBy` names, else its grid operator; an `Add` also its grid operator |
| `maintainedBy` of a grid connection point | its grid operator, its maintainer, or an identity key of the RNO |

A grid operator signs its templates and groups, and "its grid operator" signs, with its signing key,
or the RNO in its place while it has none. A
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
  see above), and it is neither the keys of a party nor `templateIds`, whose order is content: then the merge is
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
arrays of identified elements (absent counting as empty), except the keys of a party and `templateIds`, is changed by
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


## 8. Draft: proofs of entities by a signed hash tree

> **Status:** draft, not implemented, without test vectors; it may still change. The other sections do
> not depend on it.

**Purpose.** An e-mobility driver's app that has found a charging station gets one small document:
everything the roaming network says about this station as it is at runtime - the station with its
EVSEs, charging connectors and energy meters with their public keys, the properties its templates give
it, its pool, its operator, the charging tariffs its EVSEs take, the manufacturers and transparency
software of its energy meters - signed by the roaming network operator. The app checks it offline,
before charging, without the rest of the network.

**Priorities.** With millions of charging stations, a proof grows with the logarithm of the size of
the network, and checking one costs some dozen hash calculations and one signature verification.
Writing may cost more: every commit hashes again what it changed, also every member naming a template
it changed.

The ETags of section 3 hash a version as one document; a proof of one entity against them needs the
whole version. A commit signature (section 5.4) covers the whole change set, which may be megabytes.
This section therefore adds a hash tree along the entities of a version (8.1 to 8.3), proofs as
pruned versions (8.4), and small signed statements of a tree's root (8.5).

### 8.1 The effective content

The tree is calculated over the **effective content** of a version: its content (section 2) with
every charging pool, charging station, EVSE, charging tariff, parking space and energy meter replaced
by its effective one, as section 9.2 defines them: the properties its templates give it taken in, an
effective pool embedding its effective stations, an effective station its effective EVSEs, an effective
EVSE the charging tariffs that apply to it (`chargingTariffIds`). Templates, groups, offers and every
other entity are as in the content.

The `lastChange` of an effective member is the latest of its own, the ones of the templates it names
and the ones of what it embeds, without the floors POI_API keeps at runtime (8.7). So the
effective content follows from the version alone, and every server calculates the same tree.

### 8.2 The tree

With the hash algorithm `H` (8.3), the Merkle tree hash `MTH` of a list of byte strings is the one of
RFC 9162, section 2.1.1:

- `MTH({}) = H("")`;
- `MTH({d}) = H(0x00 || d)`;
- `MTH(D[n]) = H(0x01 || MTH(D[0:k]) || MTH(D[k:n]))` for `n > 1`, `k` the largest power of two
  smaller than `n`.

**Child arrays** are the arrays of entities that change sets add and remove (`Add`, `Remove`):

| Parent | Child arrays |
|---|---|
| roaming network | `chargingStationOperators`, `chargingStationManufacturers`, `eMobilityProviders`, `gridOperators`, `parkingOperators`, `energyMeterManufacturers`, `transparencySoftware`, `transparencySoftwareCertificates` |
| charging station operator | `chargingPools`, `chargingTariffs`, `EVSETemplates`, `chargingStationTemplates`, `chargingPoolTemplates`, `chargingTariffTemplates`, `energyMeterTemplates`, `EVSEGroups`, `chargingStationGroups`, `chargingPoolGroups`, `chargingTariffGroups`, `energyMeterGroups`, `chargingTariffOffers`, `parkingSpaces` |
| charging pool | `chargingStations` |
| charging station | `EVSEs` |
| e-mobility provider | `EVSEGroups`, `chargingStationGroups`, `chargingPoolGroups`, `chargingTariffOffers` |
| grid operator | `gridConnectionPoints`, `energyMeterTemplates`, `energyMeterGroups` |
| parking operator | `parkingGarages`, `parkingSpaces`, `parkingSensors`, `parkingSpaceTemplates`, `parkingSpaceGroups`, `parkingProducts` |

Everything else is part of the entity that embeds it: its roaming network operator with its keys,
brands, charging connectors, energy meters, images, keys of parties, opening times.

The **skeleton** of an entity is its JSON as the effective content embeds it, with every child array
replaced by

```json
{ "@merkleRoot": "<MTH of its children's skeletons, lowercase hex>", "@size": <number of children> }
```

the children in the order of the content (the ordinal order of their identifications), each child
as the canonical JSON (section 1) of its own skeleton. An absent child array stays absent; an empty
one is `{"@merkleRoot": "<hex of H(\"\")>", "@size": 0}`.

The **digest** of an entity is `H(0x00 || <canonical JSON of its skeleton>)`, the `MTH` of its
skeleton alone: the leaf hash it is in the tree of its parent. The digest of the roaming network is
the root of the tree, its **`effectiveTree` ETag**: `["effectiveTree", "sha256", "hex", "<digest>"]`,
in text `effectiveTree:sha256:hex:<digest>`.

### 8.3 Hash algorithms

- A roaming network names its tree hash algorithms in `treeHashAlgorithms`, an array of the
  algorithms of section 3.1 (`sha256`, `sha384`, `sha512`, and others section 3.1 may add, e.g.
  `sha3-256`); `["sha256"]` when absent. It is a property of the roaming network, owned by the RNO
  (section 4.1), and part of the content.
- A version has one `effectiveTree` ETag per algorithm it names, in their order, after its `json`
  and `cbor` ETags. Naming two, e.g. `["sha256", "sha512"]`, lets clients move from one to the other.
- A commit lists them in its `eTags` (section 5.1) and is identified with the profile
  `wwcp-poi-commit-v3`; a commit without them stays `wwcp-poi-commit-v2`. A reader MUST refuse a
  commit whose `effectiveTree` ETags are not those of its version, as it does with the others.

### 8.4 Proofs

A **proof** is the roaming network in its effective content, **pruned**: each child array, at any
depth, is given in one of three forms.

1. **Complete:** the array of its children, each again in any of these forms. The verifier
   calculates the `MTH` of their skeletons.
2. **Pruned:** `{"@merkleRoot": "<hex>", "@size": n}`, as in the skeleton. The verifier takes it as
   it is.
3. **Partial:** some children with their audit paths (RFC 9162, section 2.1.3):

   ```json
   { "@size": 200000,
     "@leaves": [ { "@index": 4711, "@auditPath": [ "<hex>", … ], "@entity": <the child, in any form> } ] }
   ```

   The verifier calculates the root from the digest of each child, its `@index` and `@size` and its
   audit path (RFC 9162, section 2.1.3.2). All of them MUST give the same root; the array then is
   `{"@merkleRoot": <it>, "@size": n}`.

The verifier calculates the digest of the roaming network from the proof, bottom-up, and MUST refuse
the proof unless it is the `effectiveTree` ETag of the algorithm named that an attestation it trusts
(8.5) names. Everything the proof gives completely is then attested, and nothing else.

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/poiProof",
  "hashAlgorithm": "sha256",
  "roamingNetwork": <the roaming network, pruned>,
  "attestations": [ <the attestations of its root (8.5)> ] }
```

The CBOR representation has the keys and values of the JSON one.

A **charging station proof** gives completely:

- the roaming network's own properties, with its roaming network operator and its keys;
- the skeletons of the station's operator and pool, and the station with its EVSEs, their energy meters with their
  `signatureMethods` and `publicKeys`;
- the charging tariffs its effective EVSEs name, in the `chargingTariffs` of its operator;
- the energy meter manufacturers the energy meters of the station and of its EVSEs name, and the
  transparency software releases and certificates their `transparencySoftwareStatus` names;
- optionally the parking spaces it names, and the grid connection point of its pool.

All other child arrays are pruned. POI_API serves it as
`GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/proof` (also `~/RNs/{RN_Id}/CSs/{CS_Id}/proof`;
`…/EVSEs/{EVSE_Id}/proof` gives the proof of its station), with `?hashAlgorithm=` one of the network's,
the first one by default. It is made for the newest attestation of the server's head, and stays the
same until the next one.

**Size** (informative): with 5,000 operators, one of them with 200,000 pools of 3 stations with 2 EVSEs
each and 50 tariffs, a station proof has about 13 + 18 + 2 + 2 × 6 audit path hashes, about 1.4 KB as
32-byte digests, plus the skeletons of the network, the operator and the pool and the complete station,
tariffs, manufacturer and software: some 15 to 25 KB of JSON, less in CBOR.

### 8.5 Attestations

An **attestation** states that a version is the one its signer has, at a point in time, with the
`effectiveTree` ETag of one algorithm. Unlike a commit signature, it does not depend on the size of
the change set.

```json
{ "roamingNetworkId": "rn1", "commitId": "<commit id>", "revision": 4711,
  "eTag": [ "effectiveTree", "sha256", "hex", "<digest>" ],
  "role": "roamingNetworkOperator", "signer": "DE*RNO",
  "signedAt": "2026-10-17T18:00:00.000Z", "validUntil": "2026-10-17T18:15:00.000Z",
  "algorithm": "ES256", "keyId": "<key @id>", "value": "<Base64>",
  "profile": "wwcp-poi-attestation-v1", "encoding": "base64" }
```

The signature input is the canonical JSON of

```json
{ "profile": "wwcp-poi-attestation-v1", "algorithm": "…", "keyId": "…", "encoding": "base64",
  "role": "…", "signer": "…", "signedAt": "…", "validUntil": "…",
  "roamingNetworkId": "…", "commitId": "…", "revision": …, "eTag": [ … ], "subject": { … } }
```

`signer`, `validUntil` and `subject` are left out where absent. Algorithms, encoding and verification
are as in section 4; timestamps are written as in section 5.4.

- **Freshness:** a server SHOULD attest its head again before `validUntil`, also without new commits,
  e.g. every 5 minutes with a `validUntil` 15 minutes later. A verifier SHOULD refuse an attestation
  after its `validUntil`.
- **Trust:** the verifier MUST trust the key of an attestation independently of the proof. A key the
  attested network names proves nothing, as whoever forged the network would name their own (8.7).
- **Audit:** whoever has the history checks that the commit `commitId` has the attested ETag. An RNO
  attesting a version it does not have is caught by it.
- **Subject:** an attestation MAY attest the digest of one entity instead of the root:
  `"subject": {"entityType": "ChargingStationOperator", "entityId": "DE*GEF", "digest": "<hex>"}`, with
  the `eTag` still naming the root. The effective members of a charging station operator depend only
  on its own templates. So the operator can attest its own node with one of its signing keys (role
  `chargingStationOperator`), and a proof checked against it needs only the paths below the operator.
- POI_API serves the current attestations as `GET ~/RNs/{RN_Id}/attestations` and pushes new ones on
  the WebSocket topic `attestations`; a proof embeds those it was made for.

### 8.6 Cost (informative)

- **Writing:** entities are immutable and shared by the versions that did not change them, so a
  digest is calculated once per entity object and kept with it. A child array keeps the inner nodes
  of its tree, and an entity added, changed or removed updates `O(log n)` of them. A commit changing
  one EVSE hashes its station, the path to its pool, the pool, the path to its operator, the operator,
  the path to the network and the network: some 50 hash calculations. A commit changing a template
  100,000 members name hashes all of them again: some 200,000 hash calculations, below a second.
- **Serving:** a proof is calculated per attested version and kept until the next attestation.
- **Verifying:** some 50 to 100 hash calculations and one signature verification per attestation,
  milliseconds on a phone.

### 8.7 Open points

1. **Public keys of energy meters:** done. An energy meter names its `signatureMethods` and its
   `publicKeys` (`{@id, key, certificateChain?}`, the DER encoded SubjectPublicKeyInfo, identified by its
   SHA-256), see [docs/EnergyMeter.md](docs/EnergyMeter.md#publickeys). Still open is whether a
   station proof also needs the certificates of the manufacturers, beyond the chains of the keys.
2. **Trust anchor:** how an app knows the signing keys of an RNO. Proposal: a key statement signed by
   an identity key of the RNO, `{roamingNetworkId, signingKeys, signedAt, validUntil}`, so that an app
   pins only the identity keys of the RNOs it trusts.
3. **Effective `lastChange`:** the floors are part of a version since POI3-ISSUES #45 and #46:
   snapshots keep them as `effectiveLastChanges`, a change set is compared with the version before it
   as a whole, a merge with both parents. How they are found is not specified here yet; until it is,
   the tree takes the effective `lastChange` without them, and the tree and POI_API may tell different
   values.
4. **Runtime:** admin status, status, energy status and price status are not content. A station
   proof tells neither whether a station is available nor its prices at runtime. Signed runtime
   statements, e.g. a price status the operator signs for some minutes, would complete it.
5. **Rules of effective members:** done, section 9.2.
6. **Proofs of absence:** e.g. that an EVSE was removed. Two neighbouring leaves in the ordinal order
   of identifications prove it; their form is open.
7. **Stored content:** a tree over the content as given (section 2) would prove stored entities, e.g.
   for preconditions and owners, the same way. The station proof does not need it.
8. **CBOR skeletons:** whether the tree also hashes deterministic CBOR skeletons, for clients that read
   CBOR only.


## 9. Templates, groups and offers

Many entities share what they are, e.g. the current types, the power and the connectors of a thousand
EVSEs of one type: **templates** give it once, and the members naming them take it at runtime. Some sets
of entities follow from no property, e.g. the charging pools along a motorway: **groups** name them and
give them nothing. **Charging tariff offers** announce prices for a time at the members of groups.

### 9.1 Templates

A template is a set of properties for the members of one level. There is a kind of template for every
kind of group, and a type of template identification for every type of group identification:

| Template | Embedded in, as | Identification | Named by |
|---|---|---|---|
| EVSE template | charging station operator, `EVSETemplates` | `EVSETemplate_Id`, `{operatorId}*ET{suffix}` | EVSEs of the operator |
| charging station template | charging station operator, `chargingStationTemplates` | `ChargingStationTemplate_Id`, `{operatorId}*ST{suffix}` | charging stations of the operator |
| charging pool template | charging station operator, `chargingPoolTemplates` | `ChargingPoolTemplate_Id`, `{operatorId}*PT{suffix}` | charging pools of the operator |
| charging tariff template | charging station operator, `chargingTariffTemplates` | `ChargingTariffTemplate_Id`, `{operatorId}*TT{suffix}` | charging tariffs of the operator |
| parking space template | parking operator, `parkingSpaceTemplates` | `ParkingSpaceTemplate_Id`, free text, unique in the roaming network | parking spaces of the parking operator |
| energy meter template | charging station operator or grid operator, `energyMeterTemplates` | `EnergyMeterTemplate_Id`, free text, unique in the roaming network | energy meters of the operator's pools, stations and EVSEs, or of the grid operator's grid connection points |

A suffix is up to 50 characters, a letter, digit or `_` first, then also `* - . € $`. Identifications
are compared without case.

- **Content:** `@id`, `name`, `description`, `dataSource`, `created`, `lastChange`, `customData`, and
  any property of its members but their identification, name, description, data source, custom data,
  `templateIds`, what they embed (charging stations, EVSEs) and their energy meters. Neither the
  charging tariffs of an EVSE, in whose place EVSE, charging station and charging pool templates name a
  `chargingTariffId` of their own (9.2), nor the APDS reference of a parking space, nor what belongs to
  the single energy meter, its serial number and its public keys. A charging tariff template gives any
  property of a charging tariff but those above. A template names no members and has no runtime status.
- **Owner** (section 4.1): the party embedding it.
- **`templateIds`** of a member: the templates it takes, of its level, embedded in its owner; for an
  energy meter in the owner of what embeds it, for an energy meter of a grid connection point in its
  grid operator. Each at most once, **in the order the member gives**: unlike every other array of
  identifications, its order is content, and an implementation MUST keep it. A member MUST NOT name a
  template that does not exist; a template a member names can not be removed.

### 9.2 Effective members

An effective member takes, of every property a template may give:

- a single value: its own, else the one of the first of its templates giving one;
- an array: its own united with those of all its templates; of elements with the same key (brands,
  charging connectors and data licenses of the same identification, authentication modes of the same
  type, images of the same URL) its own, else the one of the first template giving it. The current
  types and charging modes of an EVSE are taken as single values;
- the `elements` of a charging tariff: its own, else those of the first of its templates giving
  them, as a whole. A charging tariff MUST have a `currency` and `elements` as it is at runtime, not
  necessarily as it is given;
- the charging tariffs of an EVSE: its own; without any, the `chargingTariffId` of the first of its
  templates giving one, else of the first template of its charging station giving one, else of the
  first template of its charging pool giving one.

An effective charging station has the effective EVSEs, an effective charging pool the effective
charging stations, and every effective member its effective energy meters. Charging tariffs are
effective as well: publishers publish them so, and proofs (8.4) give them so. An effective member keeps
its `templateIds`, so that it tells where its values may come from.

Templates never disagree: their order decides. A version is invalid where a member names a template
that does not exist, or where its templates make an effective member invalid, e.g. a charging station
with a `maxPower` above the one of its effective EVSEs together. Both are checked per member; a change
of a template for the members naming it.

The **`lastChange`** of an effective member is the latest of its own, those of the templates it names
and those of what it embeds. Naming a template more or less is a change of the member itself; the
floors (8.7, point 3) keep it from going back where the timestamps of a version do not grow, e.g. after
merges.

**Cost** (informative): what a member takes follows from the member and the few templates it names.
Templates have no member lists, are small and few, and a client keeps them by their ETags. A change of
a template changes every member naming it: publishers publish them again, and the effective tree (8.6)
hashes them again. Giving 10,000 EVSEs a template changes 10,000 EVSEs.

### 9.3 Groups

A group names members and gives them nothing: no effective member depends on a group, and a change of
a group changes neither a member, nor its ETag, nor its effective content. Besides `@id`, `name`,
`description`, `dataSource`, `created`, `lastChange` and `customData` it has its members, and a runtime
status, set manually or calculated from the status of its members. Groups may overlap.

| Group | Embedded in, as | Identification | Members |
|---|---|---|---|
| EVSE group | charging station operator or e-mobility provider, `EVSEGroups` | `EVSEGroup_Id`, `{partyId}*EG{suffix}` | `EVSEIds` |
| charging station group | charging station operator or e-mobility provider, `chargingStationGroups` | `ChargingStationGroup_Id`, `{partyId}*SG{suffix}` | `chargingStationIds` |
| charging pool group | charging station operator or e-mobility provider, `chargingPoolGroups` | `ChargingPoolGroup_Id`, `{partyId}*PG{suffix}` | `chargingPoolIds` |
| charging tariff group | charging station operator, `chargingTariffGroups` | `ChargingTariffGroup_Id`, `{operatorId}*TG{suffix}` | `chargingTariffIds` |
| parking space group | parking operator, `parkingSpaceGroups` | `ParkingSpaceGroup_Id`, free text | `parkingSpaceIds` |
| energy meter group | charging station operator or grid operator, `energyMeterGroups` | `EnergyMeterGroup_Id`, free text | `energyMeterIds` |

`{partyId}` is the identification of the party embedding it. The identification of a group is unique in
the roaming network, so that a provider's group and an operator's group of the same identification can
not both exist; a group belongs to the party embedding it. A group of an operator names members of its
own, of a parking operator its parking spaces, of a grid operator the energy meters of its grid
connection points; a group of an e-mobility provider names members of any operator of the network, e.g.
for its offers, by weak references (9.4).

Groups are for sets that do not follow from properties, e.g. the charging pools along a motorway, of a
funding programme or of a partner network, or those of an offer. A selection by properties, e.g. EVSEs
with direct current and at least 150 kW, is a query (POI_API), not a group.

### 9.4 Charging tariff offers

A **charging tariff offer** announces another price for a time at the EVSEs of groups or of operators:
e.g. a promotion of a charging station operator at its EVSEs, or the price an e-mobility provider gives
its customers at the EVSEs of many operators. Which EVSEs, is a business decision that does not follow
from their properties: groups name them, and the offer names the groups. Neither the groups nor the
EVSEs change; no effective member takes an offer.

| | |
|---|---|
| Embedded in, as | a charging station operator or an e-mobility provider, `chargingTariffOffers` |
| Identification | `ChargingTariffOffer_Id`, free text, compared without case, unique in the roaming network |
| Owner | the party embedding it (section 4.1), the parent of an `Add` |

Its content is `@id`, `name`, `description`, `dataSource`, `created`, `lastChange`, `customData`, and:

- **What**, exactly one of: `chargingTariff`, a price of its own, as a charging tariff writes it,
  without its identification, `tariffType`, validity and timestamps; or `discount`,
  `{"percent": <decimal above 0, at most 100>, "dimensions": [ … ]}`, which reduces a price by the
  percentage: the price components of the dimensions named (`ChargingDimensionTypes`), or, without
  `dimensions`, all of them and its `minPrice` and `maxPrice`.
- **Where**, at least one of: `EVSEGroupIds`, `chargingStationGroupIds` and `chargingPoolGroupIds`;
  `chargingStationOperatorIds`, all EVSEs of these operators. An operator's offer names groups of its own
  and no operator but itself; a provider's offer groups of its own or of any operator, and any operator.
  An EVSE is **covered** where it, its charging station or its charging pool is a member of one of these
  groups, or where its operator is named.
- **When:** `notBefore` and `notAfter`, both inclusive, as of keys (section 4.1); absent, always since
  and for good. `openingTimes`, as a charging pool writes them, e.g. every day from 18:00 to 06:00;
  absent, at any time. An offer applies to the part of a session within these times, as the
  restrictions of a tariff element do: a session from 17:30 to 19:00 takes an offer opening at 18:00
  from 18:00 on.
- **For which sessions:** `maxSessionsPerContract`, how often one contract, or ad hoc one means of
  payment, takes it; of an operator's offer also `sessions`, `adHoc` (paid without a contract) or
  `contract`, both when absent, and `eMobilityProviderIds`, only for contracts of these providers. A
  provider's offer is for sessions with its own contracts.

**What it changes.** An operator's offer changes the operator's price: at a covered EVSE, the regular
tariffs (its effective `chargingTariffIds`, 9.2) for the sessions it names (of the `tariffType`
`AD_HOC_PAYMENT` for ad hoc sessions, of any other type for sessions with a contract, without a type for
both) are replaced by its tariff, or reduced by its discount. A provider's offer changes the price its
customers pay at a covered EVSE: its tariff, or their usual price reduced, which the provider knows and
the roaming network does not. It needs no signature of the operator, as it changes nothing of the
operator's. An operator's offer and a provider's offer at the same EVSE are prices of two parties and
do not overlap. An offer applies while it is `active` (below) and within its times. Whether a session
meets its conditions, e.g. is the first of its contract, its owner decides when the session starts, and
the charge detail record tells the price: the roaming network announces offers, it does not enforce
them.

**No overlap.** Two offers of one owner overlap where they cover the same EVSE, for sessions both apply
to (ad hoc, or with a contract of a provider both name, or one of them names none), at times both apply
in, by their `notBefore`, `notAfter` and `openingTimes`. A version where two offers of an operator
overlap is invalid: they and what they cover are its own data, checked per EVSE. A change set of a
provider MUST NOT make two of its offers overlap; where they come to overlap by the changes of others,
e.g. an operator adding an EVSE to a group both name, the one with the later `notBefore` applies, of the
same `notBefore` the one with the ordinal lower identification.

**References across owners are weak.** A provider's groups and offers name members, groups and
operators of others without asking them. A member, group or operator that does not exist (any more) is
left out, and its owner removes it without the provider. References within one owner are strong: a
member or group an operator's group or offer names can not be removed.

- **Runtime status** (not content): `planned` before its `notBefore`, `active`, `expired` after its
  `notAfter`, unless its owner sets `exhausted` before, e.g. when its budget is used up.
- **Effective content:** offers are content, child arrays of their owners (8.2), but they change no
  effective member, and a station proof (8.4) gives none (9.5).
- **Publishers** (informative): OCPI knows no offers. An operator's active offer without
  `maxSessionsPerContract` can be published as tariffs taking the place of the regular ones while it is
  valid (to the providers it names, where it names some), a discount as the regular tariff with the
  reduced prices, and its regular opening times as restrictions of their elements; one with
  `maxSessionsPerContract` only as text, as a tariff would show its price also to drivers who no longer
  get it. A provider's offers are no tariffs of an operator; POI_API serves them.

### 9.5 Open points

1. **Templates of others:** e.g. the templates of a charging station manufacturer for its models,
   named by charging stations of many operators. A member takes them by naming them, but their owner
   would then change what the member takes, unless the member names them with their ETag.
2. **Proofs of offers:** a station proof could give the offers covering it only together with a proof
   that no other does, e.g. by a tree over the member lists of groups.
3. **Groups of other parties:** grid operators, parking operators or a funding authority, which is no
   party of the network yet, might collect EVSEs of operators as providers do (9.3).
4. **Removing what a template gives:** arrays are united; a member can not drop a connector or a brand
   its templates give, only stop naming the template.
5. **Templates naming templates:** not proposed. The list of a member is flat, so the member tells
   everything it takes; an order of several templates many members share is repeated in each.
6. **A tree over the stored content** (8.7, point 7): a proof could give the stored member and the
   templates it names in place of the effective member. A change of a template would then hash
   `O(log n)` in place of every member naming it, but a verifier would need the rules of 9.2.
