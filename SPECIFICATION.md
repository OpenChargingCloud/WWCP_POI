# WWCP POI 3: content, ETags, commits and signatures

This document specifies which bytes POI 3 hashes and signs, so that a client can check a version,
a commit or a signature without this library, and what a version tells where it gives something once
for many entities: templates, groups and offers (section 9). The examples in section 6 are checked by
`WWCP_POI3_Tests/SpecificationVectorsTests.cs`. Section 8 specifies proofs of single entities, e.g. of
one charging station, by a signed hash tree, checked against attestations. Section 10 specifies
checkpoints, the witnessed and lasting evidence of a history, section 11 epochs, a history going on under
new algorithms, and section 12 price statements, the signed evidence of prices at runtime.

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
strings as strings, never as points in time. A number with a fraction or an exponent that a decimal
cannot hold as written - more than 28 decimal places, as `1e-30`, or a mantissa above 96 bits -
MUST be refused, not rounded; so MUST an object that names a property twice.

### 1.1 JSON-LD contexts

Every object of POI - an entity, a template, a group, a change set and its changes, a commit, a
checkpoint, an epoch, an attestation, a key or price statement, a proof, a WebSocket message - has a
JSON-LD context, `https://open.charging.cloud/contexts/wwcp+json/<type>`, its type in lowerCamelCase,
e.g. `chargingPool`, `roamingNetworkChangeSet`, `roamingNetworkCommit`; a proof's is `poiProof`.

- **Written on its own**, e.g. as the body of an HTTP response or a WebSocket message, an object names
  its context in `@context`, after its `@id`, in JSON as in CBOR.
- **Embedded** in another object, e.g. an entity in its parent, a change set in its commit or an item of
  a list, it does not.
- **Hashed, signed or identified**, an object is written embedded: a change set, commit, checkpoint,
  attestation or statement is hashed and signed without its `@context`, and the content of a version
  (section 2) names the context of the roaming network only. The contexts change no ETag,
  identification or signature, and a history written before them stays valid.
- A reader MUST accept an object without `@context` and MUST refuse one with another.

A JSON text that is `null` is `null` in CBOR as well, never the empty text.


## 2. The content of a version

A version of a roaming network is identified by its **content**: its JSON representation as
`RoamingNetwork.ToJSON(Embedded: false, IncludeETags: false)` writes it.

- **Included:** `@context`
  (`https://open.charging.cloud/contexts/wwcp+json/roamingNetwork`), `@id`, `name`, `description`,
  `dataSource`, `dataLicenses`, its operator `operator`, every embedded entity completely (operators with their pools,
  stations, EVSEs, connectors, energy meters, tariffs, templates, groups and offers; manufacturers;
  e-mobility providers with their groups and offers; grid operators with their grid connection points,
  energy meter templates and groups; parking operators; energy meter manufacturers, transparency
  software releases and certificates), the brands of operators, pools,
  stations, EVSEs and RFID authentication modes, the `signingKeys` and `identityKeys` of its
  operator and of its parties, the key statement of its operator (section 8.6), `created`, `lastChange`
  and `customData`.
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

An ETag is a digest of the content in one of three formats.

| Format | Digest over |
|---|---|
| `json` | the canonical JSON of the content (UTF-8) |
| `cbor` | the deterministic CBOR of the content |
| `effectiveTree` | the root of the hash tree along the entities of the effective content of a version (section 8) |

### 3.1 Representations

- **JSON:** `["json", "sha256", "hex", "<64 lowercase hex digits>"]`; the encoding MAY also be
  `base64`, with canonical padding, except in change sets (their `beforeETags`, `afterETags` and
  preconditions) and commits: these MUST write ETags in hex, and a reader MUST refuse others, as their
  signatures and identifications are calculated over them as written.
- **CBOR:** `[format, "sha256", h'<digest>']`, e.g. `["cbor", "sha256", h'291e…']`, the digest as a byte string.
- **Text:** `json:sha256:hex:<digest>`.
- **Algorithms:** `sha256` (default), `sha384`, `sha512`.
- A roaming network lists its ETags in `eTags`, ordered by format: `json`, then `cbor`, then its
  `effectiveTree` ETags in the order of its tree hash algorithms (section 8.3); their content is its own
  representation without them, with its `@context`.
- Every other entity MAY list its ETags in the `eTags` of a representation of its own: `json`, then `cbor`,
  `sha256`, over its content as its parent embeds it, without `@context` and without them (section 7), the
  HTTP `ETag` of POI_API and the one a precondition names. An embedded entity has none: the ETags of what
  embeds it cover it.
- `eTags` are written in `base64` unless asked otherwise; a reader takes both encodings.
- A reader MUST refuse a roaming network or another entity whose `eTags` are not the ETags of its content,
  and one with another `@context`. Both MAY be left out.

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
  `identityKeys` of a party and the `treeHashAlgorithms` of a roaming network (section 8.3) are not changed
  element by element;
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

**What is signed** is named by a versioned JSON-LD context, `https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/<object>/v<n>`:
it tells what an object is and in which version, and every signature input and every identification
contains it as `@context`, also where the object is embedded and its JSON leaves `@context` out. A new
version of an object, or of how its signature input is made, is a new context. A verifier compares it as
text, never fetches it, and MUST refuse a context it does not know. Signature values are canonical
standard Base64; there is no `profile` and no `encoding`.

**Signatures** of the owners of what it changes (section 4.1): `{ algorithm, keyId, value }`. The
signature input is the canonical JSON of

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/changeSet/v3", "algorithm": "…", "keyId": "…",
  "changeSet": <the change set without its signatures> }
```

Algorithms are the COSE names of Styx: `Ed25519`, `Ed448`, `EdDSA`, `ES256`, `ES384`, `ES512`,
`ES256K`, `ESP256`, `ESP384`, `ESP512`, `ESB256`, `ESB320`, `ESB384`, `ESB512`, `ML-DSA-44`,
`ML-DSA-65` and `ML-DSA-87`. ECDSA is signed deterministically (RFC 6979). A verifier MUST refuse
non-canonical Base64, an algorithm name that is not canonical, and a key identification other than the
one of the trusted key.

### 4.1 Keys and owners

A **key** of a party is a public COSE key in its static data:

```json
{ "@id": "<SHA-256 COSE key thumbprint [RFC 9679], 64 lowercase hex digits>",
  "key": "<Base64 of the deterministic CBOR of the public COSE key>",
  "notBefore": "…", "notAfter": "…", "holder": "…" }
```

`holder`, optional, names who holds the private key, e.g. one of several companies holding the keys of the RNO
together; it is no part of the thumbprint.

A reader MUST calculate the thumbprint and refuse a key whose `@id` differs, and a key with
private parts. The roaming network operator (RNO), charging station operators,
e-mobility providers, grid operators and parking operators each name 1..n `signingKeys`, e.g.
secp256r1, and 1..n `identityKeys`, e.g. secp521r1; a key is not both. A grid operator not taking
part names neither. The RNO is the `operator` the roaming network embeds, the party
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

**Quorum.** So that no single company decides for the network, several holders MAY share the keys of the
RNO: each names its keys with its `holder`, and the RNO names its `minConsensus`, e.g. 2 of 3 holders, written
unless 1. Where the RNO must sign, signatures with keys of `minConsensus` different holders are needed, keys
naming no holder having one holder together: for the data it owns signing keys, for its keys, its
`minConsensus` and the recovery of other parties identity keys, of the kinds the rules above allow for one
signature. Holders are counted, not keys, so a holder renewing its key has one voice. `minConsensus` MUST be
more than half of the holders of each kind of keys of the RNO and at most as many, so that two quorums
always share a holder: two contradicting decisions need a holder signing both, and that holder can be named;
an odd number of holders is not needed, e.g. 3 of 4. A version that breaks this rule MUST be refused. The
quorum of the version a change set is applied to counts, as its keys do. The key statement of an RNO with a
quorum is signed by as many holders (8.6), and a verifier needs attestations of as many (8.5).

### 4.2 Co-signing

The holders of a quorum, and the owners of the changes of one change set, sign it each on its own, in any order:
signatures sign the change set without the others (4), so adding one changes nothing it says. A server MAY offer a
place where a change set **waits** for the signatures still missing, `~/history/changeSets/pending`:

- It takes a change set to wait only when it fits a version (5), every signature it has is valid by a key of that
  version valid at its `createdAt`, at least one is by an owner of one of its changes (4.1), and its changes apply
  to that version, leading to its after ETags. So nobody makes it hold change sets nobody owning anything signed.
- More signatures come with the change set again, or by themselves for its `@id`; a change set of the same `@id`
  saying something else is refused. Signatures by the same holder count once, as in 4.1.
- Once its owners signed it, by as many holders as their quorums need, the server commits it as it commits any
  change set, and it waits no longer. Also when it is committed otherwise, e.g. as a peer sends its commit.
- It waits until then, without a time limit, unless withdrawn, or once it fits no version any longer, e.g. after a
  compaction, or its commit is refused: then it can never be committed. A server MAY limit how many wait.
- A **withdrawal** is signed by a key of an owner of one of its changes, of the kind that owner signs it with,
  valid when it is withdrawn:

  ```json
  { "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/changeSetWithdrawal/v1",
    "roamingNetworkId": "rn1", "changeSetId": "…", "withdrawnAt": "2026-10-22T12:05:00.000Z",
    "algorithm": "ES256", "keyId": "<@id>", "value": "<Base64>" }
  ```

  Its signature signs the canonical JSON of `@context`, `algorithm`, `keyId`, `roamingNetworkId`,
  `changeSetId` and `withdrawnAt`, the timestamp written as here.
- The server keeps the change sets waiting in its store, `{type: "pendingChangeSet", changeSet}` with the
  signatures it has and `{type: "pendingChangeSetDropped", changeSetId}`, so that they still wait after a restart.
  They wait at this server only; a peer learns of them by their commit.

POI_API answers `{status, changeSet?, missing?, commitId?, description?}`, `status` one of `pending`,
`committed`, `alreadyCommitted`, `withdrawn`, `refused` and `notFound`, `missing` one text per change whose owners
have not signed it yet:

| Request | |
|---|---|
| `GET ~/history/changeSets/pending` | the change sets waiting, `{pendingChangeSets: [ … ]}` |
| `POST ~/history/changeSets/pending` | a change set to wait, or to be committed: 202 Accepted while it waits, 201 Created once committed |
| `GET ~/history/changeSets/pending/{changeSetId}` | one of them |
| `POST ~/history/changeSets/pending/{changeSetId}/signatures` | `{signatures: [ … ]}` of it |
| `DELETE ~/history/changeSets/pending/{changeSetId}` | its withdrawal |

and over the WebSocket of the history `pendingChangeSets`, `pendingChangeSet {changeSetId}`, `coSignChangeSet
{changeSet}`, `addPendingSignatures {changeSetId, signatures}` and `withdrawChangeSet {withdrawal}`, with the topic
`pendingChangeSets` of every change set that starts or keeps waiting, or waits no longer. Who may submit is the
server's to decide, as for `COMMIT ~/history/changeSets`.


## 5. Commits

A commit is one step of the history: a change set applied to the version of its first parent.
Every server may commit; there is no leader. The history is a graph: a commit has no parents
(genesis), one, or two (merge).

### 5.1 Identification

The commit identification is the digest, with the identification hash of its epoch (11.1), SHA-256 in the
first one, of the canonical JSON of

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commit/v3", "roamingNetworkId": "rn1", "revision": 1,
  "eTags": [ <the ETags of the resulting version> ],
  "parentIds": [ "<first parent commit id>", "<second one, for a merge>" ],
  "changeSet": <the change set without its signatures> }
```

as 64, 96 or 128 lowercase hexadecimal digits. A genesis commit has neither `parentIds` nor `changeSet`.
From the second epoch on, the content names `"epoch"`, and the genesis of an epoch its `"profiles"` (11.3).
The `eTags` include the `effectiveTree` ETags of the version (section 8.3).
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
  see above), and it is neither the keys of a party nor `templateIds` nor `treeHashAlgorithms`, whose order is content: then the merge is
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
arrays of identified elements (absent counting as empty), except the keys of a party, `templateIds` and `treeHashAlgorithms`, is changed by
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
  "algorithm": "Ed25519", "keyId": "rno", "value": "<Base64>" }
```

- **role:** `leader`, `roamingNetworkOperator`, `chargingStationOperator` or another one.
- **signer:** optional, e.g. the identification of an operator.
- **signedAt:** MUST be written as `yyyy-MM-ddTHH:mm:ss.fffZ`, as it is signed as text.

The signature input is the canonical JSON of

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commitSignature/v1", "algorithm": "…", "keyId": "…",
  "role": "…", "signer": "…", "signedAt": "…",
  "commit": <the commit with its @context, its @id and the signatures of its change set, without the signatures of the commit> }
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
effectiveTree:sha256:hex:87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221
```

**Genesis commit:** the SHA-256 of

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commit/v3","eTags":[["json","sha256","hex","8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224"],["cbor","sha256","hex","291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549"],["effectiveTree","sha256","hex","87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221"]],"revision":0,"roamingNetworkId":"rn1"}
```

is the commit identification `fb45b9c960d2ece9085219b391a1d3bda793cd834d13f590cf9b7c2bd441f5d0`.

**Leader signature** of the genesis commit, with the Ed25519 key of the private seed
`101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f` (public key, Base64:
`d3bocLkzVPKgskwj8qNsxOgOIjIYwbl5Jv3QGDlqK5s=`), key identification `leader`, signer
`leader.example.org`, signed at 2026-10-12T12:00:00.000Z. Signature input:

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commitSignature/v1","algorithm":"Ed25519","commit":{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commit/v3","@id":"fb45b9c960d2ece9085219b391a1d3bda793cd834d13f590cf9b7c2bd441f5d0","eTags":[["json","sha256","hex","8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224"],["cbor","sha256","hex","291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549"],["effectiveTree","sha256","hex","87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221"]],"revision":0,"roamingNetworkId":"rn1"},"keyId":"leader","role":"leader","signedAt":"2026-10-12T12:00:00.000Z","signer":"leader.example.org"}
```

Signature value:

```
ZdqzPpoX2rpxR/zyFw5YGG9fWVZ32guEZi1L/QSeCxHa+5T1iScOiYQSTX91kkBuSMiQZaeHCbq53N/pk/t+Cg==
```

**Effective tree** (section 8.2). The EVSE names no templates, so the effective content is the content.
Every child array has one child, whose digest, `SHA-256(0x00 || skeleton)`, is the root of the array.
Bottom-up, the skeletons and their digests:

```json
{"@id":"DE*GEF*E1*1","created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z","maxPower":22000,"maxVoltage":230.5}
```

EVSE: `f37f52daaf5db8a3db648ac8ea7f3ef9955a1afff133803d1a9ff84b99fdc443`

```json
{"@id":"DE*GEF*S1","EVSEs":{"@merkleRoot":"f37f52daaf5db8a3db648ac8ea7f3ef9955a1afff133803d1a9ff84b99fdc443","@size":1},"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z"}
```

charging station: `d111c66efd73a1ff910cf674270046cfe3b6a2e0cab85034993df80025623f40`

```json
{"@id":"DE*GEF*P1","chargingStations":{"@merkleRoot":"d111c66efd73a1ff910cf674270046cfe3b6a2e0cab85034993df80025623f40","@size":1},"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z"}
```

charging pool: `8d4f6ed9ca2ffabf1862488a9cff9c8a224a86a247a9bb48b33751d0df5042fe`

```json
{"@id":"DE*GEF","chargingPools":{"@merkleRoot":"8d4f6ed9ca2ffabf1862488a9cff9c8a224a86a247a9bb48b33751d0df5042fe","@size":1},"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z","name":{"en":"Charging Station Operator DE*GEF"}}
```

charging station operator: `b137995cb4aacc1bc4f5c1b70f9620187d427bd150c3f9bc405ea079827de2ef`

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork","@id":"rn1","chargingStationOperators":{"@merkleRoot":"b137995cb4aacc1bc4f5c1b70f9620187d427bd150c3f9bc405ea079827de2ef","@size":1},"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z","name":{"en":"Example"}}
```

roaming network, the root: `87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221`, the
`effectiveTree` ETag above.

**Attestation** (section 8.5) of the genesis commit by the roaming network operator, with the Ed25519 key
of the private seed `202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f` (public key, Base64:
`Kay64UG8yvCyLhqU000LxzYeUm0L/hLIl5S8kyKWbdc=`), key identification `rno`, signer `DE*RNO`, signed at
2026-10-17T18:00:00.000Z, valid until 2026-10-17T18:15:00.000Z. Signature input:

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/attestation/v1","algorithm":"Ed25519","commitId":"fb45b9c960d2ece9085219b391a1d3bda793cd834d13f590cf9b7c2bd441f5d0","eTag":["effectiveTree","sha256","hex","87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221"],"keyId":"rno","revision":0,"roamingNetworkId":"rn1","role":"roamingNetworkOperator","signedAt":"2026-10-17T18:00:00.000Z","signer":"DE*RNO","validUntil":"2026-10-17T18:15:00.000Z"}
```

Signature value:

```
fPklC2Inr+ayWGWTYxcXnOXGia9fGT6NdS02iNBNwUkvjTKeB04K+UdSjYT/bt6t9qbeEjBJ+xiu4sXw2eg9AA==
```

**Key statement** (section 8.6) of the roaming network operator `DE*RNO`: the signing key of the attestation
above, `@id` `ea671729aa29c6140b5129b8d59e13fd9fbd4b96e37b3c5185fb11c3eeeac554`, stated with the Ed25519 identity
key of the private seed `303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f` (public key, Base64:
`i7BOHBuD3d8xH1vN33xQ7ePAgC9H7HluKhMc9BKY2fM=`, `@id`
`1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802`), `minConsensus` 1, signed at 2026-10-17T00:00:00.000Z,
valid until 2026-11-16T00:00:00.000Z. Signature input:

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/keyStatement/v2","algorithm":"Ed25519","identityKeys":[{"@id":"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802","key":"owEBIAYhWCCLsE4cG4Pd3zEfW83ffFDt48CAL0fseW4qExz0EpjZ8w=="}],"keyId":"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802","minConsensus":1,"roamingNetworkId":"rn1","roamingNetworkOperatorId":"DE*RNO","signedAt":"2026-10-17T00:00:00.000Z","signingKeys":[{"@id":"ea671729aa29c6140b5129b8d59e13fd9fbd4b96e37b3c5185fb11c3eeeac554","key":"owEBIAYhWCAprLrhQbzK8LIuGpTTTQvHNh5SbQv+EsiXlLyTIpZt1w=="}],"validUntil":"2026-11-16T00:00:00.000Z"}
```

Signature value:

```
+xLjrUX+ty7PBE1XHUgSmlKAljbhXdm3ccs1HjAW0tyJPAOuAuP/Dh+r/3nLWdhwt2ugx72eek5kTmKNvaP+AA==
```

**Key statement of a quorum** (sections 4.1 and 8.6): the same signing and identity keys of the holder `Alpha`
and the Ed25519 keys of the holder `Beta` of the private seeds
`606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f` (signing, `@id`
`c1924d610e8d9f1c5efdbca564a2f19991517ab8252807651f94ac5332afe3f9`) and
`707172737475767778797a7b7c7d7e7f808182838485868788898a8b8c8d8e8f` (identity, `@id`
`a287cebcd93b836a712b6393a914a86e63516d401285f3c642f734a28146f83f`), `minConsensus` 2, signed by both identity
keys. Signature input of `Alpha`:

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/keyStatement/v2","algorithm":"Ed25519","identityKeys":[{"@id":"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802","holder":"Alpha","key":"owEBIAYhWCCLsE4cG4Pd3zEfW83ffFDt48CAL0fseW4qExz0EpjZ8w=="},{"@id":"a287cebcd93b836a712b6393a914a86e63516d401285f3c642f734a28146f83f","holder":"Beta","key":"owEBIAYhWCAc5WpIyC/5kWKhS8VEYSZ05dYfuTF+ZdQFV4D9vLTcNQ=="}],"keyId":"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802","minConsensus":2,"roamingNetworkId":"rn1","roamingNetworkOperatorId":"DE*RNO","signedAt":"2026-10-17T00:00:00.000Z","signingKeys":[{"@id":"c1924d610e8d9f1c5efdbca564a2f19991517ab8252807651f94ac5332afe3f9","holder":"Beta","key":"owEBIAYhWCAXRVO0Vt3fxpCOyrHBAf5qsh4rqgYXeVt9Q6Y0gpk/1Q=="},{"@id":"ea671729aa29c6140b5129b8d59e13fd9fbd4b96e37b3c5185fb11c3eeeac554","holder":"Alpha","key":"owEBIAYhWCAprLrhQbzK8LIuGpTTTQvHNh5SbQv+EsiXlLyTIpZt1w=="}],"validUntil":"2026-11-16T00:00:00.000Z"}
```

Signature values of `Alpha` and of `Beta`, whose input has its own `keyId`:

```
EjbREsL+/wWCJh3cNLUIDIC0Jh5Pou8VAyRhSZTNU/0ll3zTJThaTaIzc721O1T7tqj/1sm0ceaw2nOSJ7DvAw==
Om7vDbZvLwtvW1qpiVguNhZ7K1zNtsaqt1bcMGDot/md+bBZWNeVHFhg2VpH5BMioseZzpP6WLvXdQqrhrqgBg==
```

**Trust anchor** (section 8.6) of the example, with the identity key of the key statement above:

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/trustAnchor/v1","roamingNetworkId":"rn1","name":{"en":"Example"},"roamingNetworkURI":"https://hub.example/poi/RNs/rn1","identityKeys":["1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802"]}
```

Its QR code, 225 bytes of CBOR:

```
WWCPPOI1:++K..DF$D4IK1$CM2D$9FQ$DTVD38DUPCG/DB$CQWEUT9MWEW9E0G7%:5I9E -DNPCGECD3D3/D+%5*VD$$EQ:5-3E7WEBAFCPES4FVPC:L5VQE$-D0LEBECZEDV2D7$CK4F-KE/:56LE QEAC8NPC04E0067B6PED1$CBWERWE-N9-FF$FGW24.+U3M0-$O1JV%LLP-T2OR-7U%J0WP7ZXAW7RI33:PKFFM*D00LEBECZEDV2D7$CK4F-KENC9XPC*-DHFES3EJ$D3/D:+9PWE04ERPD5IAG8FA9DIWENPEJ/5B9DOICB$CBECP9ESZCS9EHDDAIACPE:KE41
```

**Station proof** (section 8.4)
 of `DE*GEF*S1`: every child array on its path is partial with one leaf at
index 0 and an empty audit path; the station is complete. A verifier calculates the root above from it and
checks it against the attestation, which it embeds:

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/proof/v1",
  "hashAlgorithm": "sha256",
  "commitId": "fb45b9c960d2ece9085219b391a1d3bda793cd834d13f590cf9b7c2bd441f5d0", "revision": 0,
  "roamingNetwork": {
    "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork", "@id": "rn1",
    "name": { "en": "Example" },
    "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z",
    "chargingStationOperators": { "@size": 1, "@leaves": [ { "@index": 0, "@auditPath": [], "@entity": {
      "@id": "DE*GEF", "name": { "en": "Charging Station Operator DE*GEF" },
      "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z",
      "chargingPools": { "@size": 1, "@leaves": [ { "@index": 0, "@auditPath": [], "@entity": {
        "@id": "DE*GEF*P1", "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z",
        "chargingStations": { "@size": 1, "@leaves": [ { "@index": 0, "@auditPath": [], "@entity": {
          "@id": "DE*GEF*S1", "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z",
          "EVSEs": [ { "@id": "DE*GEF*E1*1", "maxPower": 22000, "maxVoltage": 230.5,
                       "created": "2026-01-01T00:00:00.000Z", "lastChange": "2026-01-01T00:00:00.000Z" } ]
        } } ] } } } ] } } } ] } },
  "attestations": [ <the attestation above> ] }
```

**Price statement** (section 12) of the EVSE `DE*GEF*E1*1` by its operator `DE*GEF`: the energy price of its
charging tariff `DE*GEF*T1` is 0.39 per kWh, valid from 2026-10-19T12:00:00.000Z until
2026-10-19T12:15:00.000Z, the first statement of the EVSE. Signed with the Ed25519 signing key of the
private seed `404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f` (public key, Base64:
`JUO5L/EJVRFHatyDadtt3JM2ZaEZeN2hQE7hBmypVZ0=`, `@id`
`8cb46c0f98440da34ad4af304431f76c7cb7462fa8fdfd5d6261dc685809220d`), signed at 2026-10-19T11:59:58.000Z.
Signature input:

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/priceStatement/v1","algorithm":"Ed25519","entityId":"DE*GEF*E1*1","entityType":"EVSE","keyId":"8cb46c0f98440da34ad4af304431f76c7cb7462fa8fdfd5d6261dc685809220d","priceStatus":{"prices":[{"chargingTariffId":"DE*GEF*T1","elements":[{"priceComponents":[{"price":0.39,"stepSize":1,"type":"ENERGY"}]}]}]},"roamingNetworkId":"rn1","role":"chargingStationOperator","sequence":0,"signedAt":"2026-10-19T11:59:58.000Z","signer":"DE*GEF","validFrom":"2026-10-19T12:00:00.000Z","validUntil":"2026-10-19T12:15:00.000Z"}
```

Signature value:

```
jj4tV4hmXg0mK2kdRCo6KF8kvAuKlBkjnXGjdMfjcpzRPhVm36KcLBGbqAAVisA5BUKEQmcI1MKWvENp2+hZDw==
```

Its digest, the SHA-256 of the signature input with `"value"` added, which the next statement of the EVSE
names as its `previous` and a checkpoint takes as a leaf:
`de4139029cd1791f6a7de0c34f77d22f5747386f166cd406128d46a8a1fb3885`.

**Genesis of a second epoch** (section 11.3) of the example, with SHA-512 identifications and the signature
algorithms Ed25519 and ML-DSA-65: the SHA-512 of

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commit/v3","eTags":[["json","sha256","hex","8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224"],["cbor","sha256","hex","291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549"],["effectiveTree","sha256","hex","87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221"]],"epoch":2,"profiles":{"canonicalization":"styx-canonical-json-v1","checkpoint":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/checkpoint/v1","commit":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commit/v3","identificationHash":"sha512","priceStatement":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/priceStatement/v1","signatureAlgorithms":["Ed25519","ML-DSA-65"]},"revision":0,"roamingNetworkId":"rn1"}
```

is its commit identification
`22dd391f9190a4a9a5a8c7fbe1c9c38b2202d1f11ad7d6552b10af28be5b5559992b56b144752c112e4f884b426f5302dce0a5ce86e909a63e33b071fb95f7cc`.


## 7. Open points

The HTTP `ETag` of an entity is the SHA-256 of its canonical representation as the roaming network embeds it,
and change sets name it as a precondition (section 4).

1. **Co-signing across servers:** a change set waits for signatures at one server (4.2); holders using other
   servers sign it there, as peers learn of it only by its commit.


## 8. Proofs of entities by a signed hash tree

**Purpose.** An e-mobility driver's app that has found a charging station gets one small document:
everything the roaming network says about this station as it is at runtime - the station with its
EVSEs, charging connectors and energy meters with their public keys, the properties its templates give
it, its pool, its operator, the charging tariffs its EVSEs take, the manufacturers and transparency
software of its energy meters - and an attestation of the roaming network operator. The app checks it
offline, before charging, without the rest of the network; months later, e.g. disputing an invoice,
with the proof of its commit in a checkpoint (10.4).

**Priorities.** With millions of charging stations, a proof grows with the logarithm of the size of
the network, and checking one costs some dozen hash calculations and one signature verification.
Writing may cost more: every version hashes its effective content.

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

The `lastChange` of an effective member follows from the version alone, without the floors POI_API keeps
at runtime (8.8):

- an EVSE, charging station or charging pool naming no templates, taking no charging tariff of a
  template and embedding only EVSEs, charging stations and energy meters that are themselves, is itself,
  with its own `lastChange`;
- every other one has the latest of its own, those of the templates it names, the one of the template
  whose `chargingTariffId` it takes, those of what it embeds as it is effective (the EVSEs of a charging
  station, the charging stations of a charging pool) and those of its energy meters taking properties of
  templates;
- an energy meter, charging tariff or parking space naming templates has the latest of its own and those
  of its templates.

So every server calculates the same tree.

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
in text `effectiveTree:sha256:hex:<digest>`, in CBOR `["effectiveTree", "sha256", h'<digest>']`.
Section 6 has an example.

### 8.3 Hash algorithms

- A roaming network names its tree hash algorithms in `treeHashAlgorithms`, an array of the
  algorithms of section 3.1 (`sha256`, `sha384`, `sha512`), at least one, none twice; `["sha256"]` when
  absent. It is a property of the roaming network, owned by the RNO (section 4.1), and part of the
  content: a change set updates and removes it as any other property of the roaming network.
- A version has one `effectiveTree` ETag per algorithm it names, in their order, after its `json`
  and `cbor` ETags. Naming two, e.g. `["sha256", "sha512"]`, lets clients move from one to the other.
- A commit lists them in its `eTags` (section 5.1) and is identified with the context
  `https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commit/v3`. A reader MUST refuse a commit without them, and one whose `effectiveTree` ETags
  are not those of its version, of every algorithm it names, in their order.
- Change sets name the `json` and `cbor` ETags of the versions before and after them; a change set MAY
  name `effectiveTree` ETags as well, which are then checked as the others. A precondition (section 4)
  names no `effectiveTree` ETag: an entity has none.

### 8.4 Proofs

A **proof** is the roaming network in its effective content, **pruned**: each child array, at any
depth, is given in one of three forms.

1. **Complete:** the array of its children, each again in any of these forms. The verifier
   calculates the `MTH` of their skeletons.
2. **Pruned:** exactly `{"@merkleRoot": "<hex>", "@size": n}`, as in the skeleton, the root of the
   length of the hash algorithm. The verifier takes it as it is; `@size` 0 only with the root `H("")`.
3. **Partial:** some children with their audit paths (RFC 9162, section 2.1.3):

   ```json
   { "@size": 200000,
     "@leaves": [ { "@index": 4711, "@auditPath": [ "<hex>", … ], "@entity": <the child, in any form> } ] }
   ```

   Exactly these two properties, at least one leaf, the leaves in ascending order of their `@index`,
   each below `@size`, with exactly these three properties. The verifier calculates the root from the
   digest of each child, its `@index` and `@size` and its audit path (RFC 9162, section 2.1.3.2). All of
   them MUST give the same root; the array then is `{"@merkleRoot": <it>, "@size": n}`.

The verifier calculates the digest of the roaming network from the proof, bottom-up, and MUST refuse
the proof unless it is the `effectiveTree` ETag of the algorithm named that an attestation it trusts
(8.5) names, or the `effectiveTree` ETag of a commit a checkpoint it trusts proves (10.4). Everything the
proof gives completely is then attested, and nothing else. It MUST read the proof with numbers with a
fraction as decimals and strings as strings (section 1).

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/proof/v1",
  "hashAlgorithm": "sha256",
  "commitId": "<commit of the proven version>", "revision": 4711,
  "roamingNetwork": <the roaming network, pruned>,
  "attestations": [ <the attestations of its root (8.5)> ] }
```

`commitId` and `revision` name the proven version, so that the verifier finds the proof of its commit in a
checkpoint (10.4); they are not part of the tree. The CBOR representation has the keys and values of the
JSON one.

A **charging station proof** gives completely:

- the roaming network's own properties, with its roaming network operator and its keys;
- the skeletons of the station's operator and pool, and the station with its EVSEs, their energy meters with their
  `signatureMethods` and `publicKeys`;
- the charging tariffs its effective EVSEs name, in the `chargingTariffs` of its operator;
- the manufacturer its `manufacturerId` names, the energy meter manufacturers the energy meters of the station and
  of its EVSEs name, and the transparency software releases and certificates their `transparencySoftwareStatus` names;
- the parking spaces it names, of its operator or of parking operators, and the grid connection point of its pool.

All other child arrays are pruned. POI_API serves it as
`GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/proof` (also `~/RNs/{RN_Id}/CSs/{CS_Id}/proof`;
`…/EVSEs/{EVSE_Id}/proof` and `~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/proof` give the proof of its station), with
`?hashAlgorithm=` one of the network's, the first one by default, and over the WebSocket as
`chargingStationProof {chargingStationId | EVSEId, hashAlgorithm?}`. It is made for the head of the history,
with the attestations of the head valid at the time; the tree of the head is kept until the head changes.

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
  "algorithm": "ES256", "keyId": "<key @id>", "value": "<Base64>" }
```

The signature input is the canonical JSON of

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/attestation/v1", "algorithm": "…", "keyId": "…",
  "role": "…", "signer": "…", "signedAt": "…", "validUntil": "…",
  "roamingNetworkId": "…", "commitId": "…", "revision": …, "eTag": [ … ], "subject": { … } }
```

`signer`, `validUntil` and `subject` are left out where absent. The `eTag` is written in hex. The context,
algorithms and verification are as in section 4; timestamps are written and signed as in section 5.4, and
`validUntil` is not before `signedAt`. Section 6 has an example.

- **Freshness:** a server SHOULD attest its head again before `validUntil`, also without new commits.
  POI3 attests every new head with the `AttestationSigners` of the history, valid for 15 minutes, and
  `RefreshAttestations()` attests the head again where the newest attestation of a signer is older than
  5 minutes. A verifier SHOULD refuse an attestation after its `validUntil`.
- **Trust:** the verifier MUST trust the key of an attestation independently of the proof. A key the
  attested network names proves nothing, as whoever forged the network would name their own; a key
  statement (8.6) names them. Of an RNO with a quorum (section 4.1), the verifier MUST take a version only
  attested by signing keys of `minConsensus` different holders the statement names, so that no holder vouches
  for a version alone; each holder attests at its own server, and the servers exchange their attestations.
- **Audit:** whoever has the history checks that the commit `commitId` has the attested ETag. An RNO
  attesting a version it does not have is caught by it. A server takes an attestation of another server
  only for a commit it holds, whose version has the attested ETag, with a signature of a key it trusts.
- **Subject:** an attestation MAY attest the digest of one entity instead of the root:
  `"subject": {"entityType": "ChargingStationOperator", "entityId": "DE*GEF", "digest": "<hex>"}`, with
  the `eTag` still naming the root. The effective members of a charging station operator depend only
  on its own templates. So the operator can attest its own node with one of its signing keys (role
  `chargingStationOperator`), and a proof giving the operator is checked against it by the digest of the
  operator. Histories keep only attestations of the root.
- POI_API serves the current attestations, those of the head valid now, as `GET ~/RNs/{RN_Id}/attestations`
  and the WebSocket action `attestations`, and pushes every attestation the history takes on the topic
  `attestations` (action `attestation`); a proof embeds those it was made for. A peer verifying attestations
  with keys it trusts takes those of the other server, so that every server serves proofs with them.

### 8.6 Key statements and trust anchors

A verifier trusts the key of an attestation independently of the proof (8.5). So that it pins only a few
long-lived keys, the roaming network operator states its signing keys in a **key statement**, signed with identity keys of
`minConsensus` of its holders (section 4.1), one of them for an RNO without a quorum:

```json
{ "roamingNetworkId": "rn1", "roamingNetworkOperatorId": "DE*RNO",
  "signingKeys":  [ { "@id": "…", "key": "…", "holder": "Alpha" }, { "@id": "…", "key": "…", "holder": "Beta" } ],
  "identityKeys": [ { "@id": "…", "key": "…", "holder": "Alpha" }, { "@id": "…", "key": "…", "holder": "Beta" } ],
  "minConsensus": 2, "signedAt": "…", "validUntil": "…",
  "signatures": [ { "algorithm": "Ed25519", "keyId": "<@id>", "value": "<Base64>" }, … ] }
```

Its context is `https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/keyStatement/v2`; the former single signature form, with `algorithm`,
`keyId` and `value` beside the statement, is gone. The signature input of each signature is the canonical
JSON of the statement without `signatures`, with its `@context`, also where it is embedded in the operator,
its `algorithm` and its `keyId`; `identityKeys` is left out where absent.
Every holder signs the same statement on its own and adds its signature to those of the others. Keys are written as in section 4.1, in the order of their `@id`s, at least one signing key, none a
signing and an identity key; timestamps as in section 5.4, `validUntil` not before `signedAt`. Section 6
has an example.

- **Content:** the roaming network operator embeds it as `keyStatement`, a property of its own (section
  4.1). A version MUST be refused unless the statement names this roaming network and this operator, only
  signing and identity keys the operator names, its `minConsensus`, and is signed by identity keys of
  `minConsensus` holders valid at `signedAt`; every signature MUST be valid. Its validity in time is not checked there: a version keeps a statement that expired. So every
  proof of a charging station gives it with the operator, peers take it with the commits, and witnesses see
  it in the checkpoints: an operator stating other keys to some is a split view (10.2).
- **Verification:** a verifier trusting identity keys by their `@id`s checks that the statement the proof
  gives is signed by trusted ones of as many holders as the `minConsensus` it pinned and the one of the
  statement require, found among its `identityKeys` or those of the operator, each `@id` the thumbprint of its
  key (section 4.1); that it is valid at the time it checks, `signedAt` ≤ time ≤
  `validUntil`; that it names the roaming network of the proof; and that an attestation of the proof is
  signed by one of the signing keys it states, valid at the attestation's `signedAt`. The attestation is then
  trusted as 8.5 says.
- **Succession:** a verifier trusting a statement MAY trust its `identityKeys` for later statements, e.g.
  when the operator replaces an identity key before it expires.
- **Trust anchor:** what a verifier pins of a roaming network, e.g. from a QR code on the website or in the
  documents of the roaming network operator:

  ```json
  { "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/trustAnchor/v1",
    "roamingNetworkId": "rn1", "name": { "en": "…" }, "description": { "en": "…" },
    "roamingNetworkURI": "https://hub.example/poi/RNs/rn1",
    "identityKeys": [ "<@id>", "<@id>", "<@id>" ], "minConsensus": 2 }
  ```

  the roaming network, below whose URI its proofs are served (`…/CSs/{CS_Id}/proof`), the `@id`s of the identity
  keys of its operator, 64 lowercase hexadecimal digits each, and its `minConsensus`, unless 1; to show, its
  `name` and `description`. Only the `@id`s and the `minConsensus` are trusted: not one
  key, which would let its holder vouch for the network alone; the keys come with every proof. The QR code holds
  `WWCPPOI1:` and the Base45 [RFC 9285] of its deterministic CBOR, the keys and values of the JSON, the `@id`s as
  their 32 bytes, all of the alphanumeric mode of QR codes; a reader takes the URI
  `https://hub.example/poi/RNs/rn1?identityKeys=<@id>,<@id>&minConsensus=2` as well. A QR code is as trustworthy
  as the place it is found: one stuck on a charging station may be anybody's.
- POI_API serves the key statement of the operator of the head as `GET ~/RNs/{RN_Id}/keyStatement` and its
  trust anchor as `GET ~/RNs/{RN_Id}/trustAnchor`, `{roamingNetworkId, identityKeyIds, minConsensus, uri, qrCode,
  trustAnchor}`, the URIs as the request names the roaming network, with the name and description of the network.

### 8.7 Cost (informative)

- **Writing:** a version calculates its tree once, when its ETags are calculated. Entities are
  immutable and shared by the versions that did not change them, and keep their digests: a version
  hashes only the entities that are new in it and the paths from them to the root, e.g. for one changed
  EVSE of one of 50 operators of 1,000 EVSEs each some milliseconds instead of a second for all of them.
  Within its charging station operator, a change reads again only the charging pools it changes: the
  others are kept as they are, with their charging stations, EVSEs and effective members (section 9),
  so also one changed EVSE of one operator of 50,000 EVSEs costs some milliseconds. An effective member
  is calculated again where a template it takes changed: a commit changing a template 100,000 members
  name changes all of them. The `json` and `cbor` ETags are made the same way: canonical JSON and
  deterministic CBOR are made of the representations of the children, so the roaming network, its
  parties and charging pools keep their contents as the pieces between their child arrays and the
  contents of their children, a charging station its content with its EVSEs. A version writes the
  contents of the entities that are new in it and hashes the others as they are kept, e.g. for one
  changed EVSE of one operator of 50,000 EVSEs (7.7 MB of JSON, 6.6 MB of CBOR) some tens of milliseconds
  instead of 0.8 seconds, keeping some 17 MiB. What a commit then costs is mostly applying its changes.
- **Serving:** a proof is calculated from the tree of the head, made of the digests and the skeletons
  the network and its parties keep: a pool and its stations are calculated again, the audit paths from
  the digests.
- **Verifying:** some 50 to 100 hash calculations and one signature verification per attestation,
  milliseconds on a phone.

### 8.8 Open points

1. **Public keys of energy meters:** an energy meter names its `signatureMethods` and its
   `publicKeys` (`{@id, key, certificateChain?}`, the DER encoded SubjectPublicKeyInfo, identified by its
   SHA-256), see [docs/EnergyMeter.md](docs/EnergyMeter.md#publickeys). Still open is whether a
   station proof also needs the certificates of the manufacturers, beyond the chains of the keys.
2. **Trust lists:** an app pins the identity keys of a roaming network operator by scanning its trust
   anchor (8.6). A signed list of operators and their identity keys, e.g. of an association or of a national
   access point, would spare scanning one per roaming network.
3. **Effective `lastChange`:** the floors are part of a version since POI3-ISSUES #45 and #46:
   snapshots keep them as `effectiveLastChanges`, a change set is compared with the version before it
   as a whole, a merge with both parents. How they are found is not specified here yet; until it is,
   the tree takes the effective `lastChange` without them (8.1), and the tree and POI_API may tell
   different values.
4. **Runtime:** admin status, status, energy status and price status are not content. A station
   proof tells neither whether a station is available nor its prices at runtime: price statements
   (section 12) give the prices, signed by the operator; signed statements of the status are open.
5. **Proofs of absence:** e.g. that an EVSE was removed. Two neighbouring leaves in the ordinal order
   of identifications prove it; their form is open.
6. **Stored content:** a tree over the content as given (section 2) would prove stored entities, e.g.
   for preconditions and owners, the same way. The station proof does not need it.
7. **CBOR skeletons:** whether the tree also hashes deterministic CBOR skeletons, for clients that read
   CBOR only.
8. **Offers:** a station proof gives no charging tariff offers (9.5, point 2).


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
floors (8.8, point 3) keep it from going back where the timestamps of a version do not grow, e.g. after
merges.

**Cost** (informative): what a member takes follows from the member and the few templates it names.
Templates have no member lists, are small and few, and a client keeps them by their ETags. A change of
a template changes every member naming it: publishers publish them again, and the effective tree (8.7)
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
  absent, at any time. Their regular openings are local times of the time zone of the EVSE's charging
  pool as it is at runtime (`timeZone`), UTC where it has none; their exceptional openings and closings
  are periods of absolute time: an exceptional closing closes, else an exceptional opening opens, else
  the regular openings apply. An offer applies to the part of a session within these times, as the
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
in, by their `notBefore`, `notAfter` and `openingTimes` in the time zone of that EVSE, exceptional
openings and closings included. A version where two offers of an operator
overlap is invalid: they and what they cover are its own data, checked per EVSE. A change set of a
provider MUST NOT make two of its offers overlap; where they come to overlap by the changes of others,
e.g. an operator adding an EVSE to a group both name, the one with the later `notBefore` applies, of the
same `notBefore` the one with the ordinal lower identification.

**Which offers apply.** At an EVSE at a time, the offers apply that cover it, are `active` then and whose
opening times are open then: of its operator all of them, which are for different sessions; of a
provider the one that applies in place of its others by the rule above. POI_API lists them below an
EVSE, charging station or charging pool with `?applicable=true`, at another time than now with `&at=`.

**References across owners are weak.** A provider's groups and offers name members, groups and
operators of others without asking them. A member, group or operator that does not exist (any more) is
left out, and its owner removes it without the provider. References within one owner are strong: a
member or group an operator's group or offer names can not be removed. A change set is checked for them
after its last change, not after each change, and so for the offers of an operator not overlapping: it
can remove a charging station and add it to another pool of its operator, its groups naming it and its
EVSEs throughout.

- **Runtime status** (not content): `planned` before its `notBefore`, `active`, `expired` after its
  `notAfter`, unless its owner sets `exhausted` before, e.g. when its budget is used up. It becomes
  `active` and `expired` without being set: a server tells of these changes at their times as of every
  other status, so that subscribers and peers learn of them; an offer new in a version is told by it.
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
6. **A tree over the stored content** (8.8, point 6): a proof could give the stored member and the
   templates it names in place of the effective member. A change of a template would then hash
   `O(log n)` in place of every member naming it, but a verifier would need the rules of 9.2.


## 10. Checkpoints

A **checkpoint** is a signed statement that a commit, and every commit it follows, is part of the
history of a roaming network. Checkpoints form one line per roaming network and epoch (section 11),
each naming the one before it, while the commits form a graph (section 5). They serve three purposes:

- **Witnessed evidence:** independent third parties, **witnesses**, sign a checkpoint only after they
  checked it against their own copy of the history. Whoever kept a proof of a charging station and
  its tariffs (8.4) for an attested commit (8.5) shows months later, e.g. disputing an invoice, that
  this commit is part of the history the witnesses saw, without asking the server that made it.
  Charging tariffs as content change slowly, so witnesses have the time between two checkpoints.
- **Starting points:** a server or client that joins starts from the version of the latest
  checkpoint and takes the commits since, instead of all commits since the genesis.
- **Compaction:** a history drops older commits only before the commit of a checkpoint (10.6).

A checkpoint is not a version of its own and changes nothing: the content, the commits and their
identifications are as without it.

### 10.1 Representation and identification

```json
{ "@id": "<checkpoint id>", "roamingNetworkId": "rn1", "epoch": 1, "sequence": 42,
  "previousId": "<id of checkpoint 41>", "commitId": "<commit id>", "revision": 4711,
  "eTags": [ <the ETags of the version of the commit> ],
  "commits": { "@merkleRoot": "<hex>", "@size": 118 },
  "priceStatements": { "@merkleRoot": "<hex>", "@size": 2400 },
  "createdAt": "2026-10-19T12:00:00.000Z",
  "signatures": [ … ] }
```

- **sequence:** 0 for the first checkpoint of an epoch, one more for every next one; `previousId` names
  the one before and is absent for sequence 0.
- **commitId, revision, eTags:** the commit, its revision and the ETags of its version, as in the commit.
- **commits:** the Merkle tree hash (8.2, RFC 9162) of the **commits of the checkpoint**: the commit
  itself and every commit it follows, that neither the commit of the previous checkpoint is nor
  follows; for sequence 0 every commit since the genesis. In ascending order of their revisions, among
  equal revisions in the ordinal order of their identifications. A leaf is the UTF-8 encoded canonical
  JSON of `{"commitId": "…", "eTags": [ … ], "revision": …}`, so that a proof of one commit (10.4) binds
  its ETags as well. `@size` is their number.
- **priceStatements:** optional, the tree of the price statements the server took since the previous
  checkpoint (section 12.3); absent when it names none.
- **final, next:** the final checkpoint of an epoch is `"final": true` and names the next epoch (11.2).
- **previous:** the first checkpoint of an epoch after the first one names the final checkpoint of the
  one before (11.3).
- **createdAt:** when it was made, written as in section 5.4.

The checkpoint identification is the digest, with the identification hash of its epoch (11.1), SHA-256 in
the first one, of the canonical JSON of the checkpoint without `@id` and without `signatures`, with its
`"@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/checkpoint/v1"`, in lowercase hexadecimal digits; so are its trees. A reader MUST calculate it and refuse the checkpoint when it differs from `@id`. The CBOR
representation has the keys and values of the JSON one; ETags are written in hex (section 3.1).

A server MUST refuse a checkpoint unless it has its commit and all commits of the checkpoint, the
commit follows the commit of the previous checkpoint (or is it, which then is no new checkpoint and is
refused), `revision` and `eTags` are those of the commit, `commits` is the tree of the commits of the
checkpoint, and `previousId` names the latest checkpoint it holds of this epoch with `sequence` one less.
The price statements a checkpoint names are checked when they are given along with it (12.3).
A server that holds no checkpoint yet, e.g. one started from a snapshot, takes as its first one a checkpoint
of a commit it holds, without the commits before its root; its signatures carry it.

### 10.2 Signatures

A checkpoint is signed as a commit is (section 5.4), with the context
`https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/checkpointSignature/v1` and the checkpoint, with its `@context` and `@id` and without its
signatures, as `"checkpoint"` in place of `"commit"`:

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/checkpointSignature/v1", "algorithm": "…", "keyId": "…",
  "role": "…", "signer": "…", "signedAt": "…",
  "checkpoint": <the checkpoint without its signatures> }
```

- **roamingNetworkOperator:** the RNO makes the checkpoints of its roaming network and signs them first,
  with one of its signing keys (section 4.1).
- **witness:** an independent third party, e.g. an association of operators, a calibration authority or
  another roaming network operator. A witness signs a checkpoint only after it refused nothing of 10.1
  in its own history, which it took commit by commit, each checked as section 5.2 says, and, when the
  checkpoint names price statements, after it checked them (12.3). A witness MUST
  NOT sign two different checkpoints of the same roaming network, epoch and `sequence`: that is what
  makes a split view, one history shown to some and another one to others, visible.
- Signatures may be added later, as to commits: `signCheckpoint` over the WebSocket or
  `POST …/history/checkpoints/{checkpointId}/signatures`; they do not change the identification.

A verifier decides which signatures it requires, e.g. the RNO's and two of the witnesses it trusts.
Witness keys are trusted independently of the network, as attestation keys are (8.5).

### 10.3 Intervals (informative)

A server of the RNO makes a checkpoint when there are new commits and the last one is older than an
interval, e.g. one hour, and SHOULD make one of a commit that all tips follow, so that a joining server
starts without open branches. Witnesses sign within the interval. Attestations (8.5) stay the fresh,
short-lived statements of the current head; checkpoints are the lasting ones.

### 10.4 Proofs of commits

A proof that a commit is part of a checkpoint is its leaf with its audit path (RFC 9162, section 2.1.3):

```json
{ "checkpointId": "<checkpoint id>",
  "leaf": { "commitId": "…", "eTags": [ … ], "revision": 4711 },
  "@index": 17, "@size": 118, "@auditPath": [ "<hex>", … ] }
```

The verifier calculates the root from the leaf, `@index`, `@size` and the audit path, and MUST refuse
the proof unless it is the `commits` root of the checkpoint, and that checkpoint carries the signatures
it requires. With the proof of a charging station (8.4) against an attestation of `commitId`, whose
`eTag` MUST be one of the leaf's `eTags`, the station, its EVSEs and the tariffs they take are then
witnessed as part of the history. Without an attestation, the root of the station proof MUST be the leaf's `effectiveTree` ETag
of the hash algorithm of the proof. POI_API serves it as
`GET ~/RNs/{RN_Id}/history/checkpoints/{checkpointId}/commits/{commitId}/proof`, and
`GET ~/RNs/{RN_Id}/history/commits/{commitId}/proof` for the first checkpoint whose commits include it.

### 10.5 Joining from a checkpoint

A server or client that has no history takes the latest checkpoint carrying the signatures it requires,
the version of its commit (`GET ~/RNs/{RN_Id}/history/checkpoints/{checkpointId}/snapshot`), checks the
version against the checkpoint's `eTags`, and then the commits since, as from a snapshot of the head.

- **The snapshot of a checkpoint** is `{commit, roamingNetwork, effectiveLastChanges?, checkpoint}`, also
  `checkpointSnapshot {checkpointId}` over the WebSocket, without runtime status: the version is not the
  head. A joining server MUST refuse it unless every signature of the checkpoint is valid and of the roles
  it requires, `commit` is the commit of the checkpoint with its `revision` and `eTags`, and the version
  leads to them (section 3).
- **Which checkpoint:** the latest one, or, when it does not carry the signatures required yet, e.g. of
  witnesses, the latest one before that does. Without one, the server starts from the head.
- **Afterwards:** the checkpoint is the first of its line (10.1), the price statements it names are checked
  when they come along with it (12.3), the commits since are taken as they come (5.2), and the runtime
  status is the one of the head of the other server.

### 10.6 Compaction and archives

A history that has checkpoints drops older commits only before the commit of one of them, so that
every root is the commit of a checkpoint, and keeps all checkpoints. A server SHOULD archive the commits
it drops, not delete them: a checkpoint proves only what someone can still show.

The **archive** of a history has one **segment** per compaction, numbered from 1, never changed once
written: the snapshot of the root before the compaction (`commit`, `roamingNetwork`, `effectiveLastChanges`,
without runtime status) and every commit dropped after it, in their order, ending with the new root.
A segment alone gives every version from its root to its end again, and its commits, with the root, prove
the commits of every checkpoint between them (10.4). Segments follow each other: the root of one is the end
of the one before. What a segment holds:

```json
{ "number": 1, "rootId": "<root before>", "endId": "<new root>", "fromRevision": 0, "toRevision": 4711,
  "size": 4711, "commitIds": [ "…" ], "archivedAt": "2026-10-19T12:00:00.000Z" }
```

A server archives before it drops: an archive failing fails the compaction. It serves archived commits as
it serves the commits it holds (`GET ~/RNs/{RN_Id}/history/commits/{commitId}`, and their proofs), the list
of the segments as `GET ~/RNs/{RN_Id}/history/archive` and a segment as
`GET ~/RNs/{RN_Id}/history/archive/{number}` (`{info, base, commits}`).

Personal data that must be erased (GDPR) is removed from the archive, its commits and versions keep their
identifications and ETags, and a proof of what was erased is no longer possible.

### 10.7 Open points

1. **Archives:** how long they are kept; how personal data is removed from a segment, keeping the
   identifications of its commits.
2. **Witness discovery:** how a client learns which witnesses there are, and their keys.


## 11. Epochs

An **epoch** is one history of a roaming network under one set of **profiles**: the algorithms and
formats its identifications, trees and signatures are calculated with. A new epoch begins only when
these must change, not periodically: when an algorithm becomes weak (crypto agility, e.g. from SHA-256
to SHA-512 identifications, or from ECDSA to ML-DSA signatures), when the canonicalization changes, or
after the RNO lost control of its keys. Checkpoints make periodic restarts unnecessary (section 10).
Every history begins in epoch 1, whose commits and checkpoints do not name it.

### 11.1 Profiles

```json
{ "epoch": 2,
  "profiles": { "canonicalization": "styx-canonical-json-v1", "identificationHash": "sha512",
                "commit": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commit/v3",
                "checkpoint": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/checkpoint/v1",
                "priceStatement": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/priceStatement/v1",
                "signatureAlgorithms": [ "Ed25519", "ML-DSA-65" ] } }
```

- **canonicalization:** of everything hashed and signed; `styx-canonical-json-v1` (section 1).
- **identificationHash:** `sha256`, `sha384` or `sha512`: the hash of the identifications of commits
  (5.1) and checkpoints (10.1), and of the trees of checkpoints. An identification is the digest in
  lowercase hexadecimal digits, 64, 96 or 128 of them: its length tells the hash. The first epoch has
  `sha256`.
- **commit, checkpoint, priceStatement:** the contexts of their contents and signatures (section 4).
- **signatureAlgorithms:** the COSE names of the signature algorithms the epoch accepts, for the
  signatures of change sets, commits, checkpoints, attestations, key statements and price statements;
  every one the implementation signs with, when empty, as in the first epoch.

A server MUST refuse profiles it can not calculate. The `effectiveTree` ETags follow the
`treeHashAlgorithms` of the roaming network (8.3), which its operator changes within an epoch; the
`json` and `cbor` ETags stay SHA-256, as the `effectiveTree` ETags bind the content with the stronger
hashes.

### 11.2 The end of an epoch

The last checkpoint of an epoch is **final**: `"final": true` with `"next": {"epoch": …, "profiles": …}`,
the next epoch, made by the RNO, signed with the algorithms of the ending epoch, and signed by the
witnesses. Its commit is the head, when it is the only tip: open branches are merged first. It may name
the commit of the checkpoint before it, its commits then being that commit alone. The epoch takes no
commit and no checkpoint after it.

### 11.3 The start of the next one

The **genesis** of the next epoch has the version of the commit of the final checkpoint: nothing is
applied, its `revision` is 0, and it names the epoch and its profiles:

```json
{ "@context": "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commit/v3", "roamingNetworkId": "rn1", "revision": 0, "epoch": 2,
  "eTags": [ <the ETags of the final checkpoint> ], "profiles": { … } }
```

identified with its `identificationHash`. A server MUST refuse it unless its version leads to the
`eTags` of the final checkpoint. Every commit after it names `"epoch"` as well, so that no commit of
one epoch is one of another, and commits of different epochs are never merged.

The first checkpoint of the epoch (sequence 0) names the final checkpoint of the one before,
`"previous": {"epoch": 1, "checkpointId": "…", "eTags": [ … ]}`, and is signed with the algorithms of
both epochs, so that a verifier trusting only the old ones follows into the new epoch. Its signatures
are accepted with the algorithms of either epoch, every later one only with those of its own.

### 11.4 Moving on

A server applies nothing: it starts the next epoch from the version it has and checks the new genesis
against the final checkpoint. Every server doing so makes the same genesis, and takes the commits of
the epoch from there. The runtime status and the current price statements are taken along.

- **The server of the RNO** closes the epoch, starts the next one, and serves it in place of the old
  one (`TryServeNextEpoch`): the WebSocket of the old epoch closes, the one of the new epoch answers at
  the same URL, and the old epoch stays readable.
- **A peer** asks the other server for its epoch (`epoch`) before it catches up. When the other server
  is in the next epoch, a history holding the final checkpoint starts the next epoch itself; one without
  it starts from the other server, as from its start (10.5). A history in an epoch the other server has
  not reached yet waits for it, and asks again later.
- **Status of an epoch:** `active`, `closing` (final checkpoint there, witnesses still signing, the next
  epoch not started here), `closed` (the next epoch started here).

### 11.5 Old epochs

Old epochs stay readable, and their checkpoints keep proving their commits (10.4):

```
GET ~/RNs/{RN_Id}/history/epoch                                     the current epoch: { epoch, profiles, status,
                                                                    previous?, finalCheckpointId? }
GET ~/RNs/{RN_Id}/history/epochs                                    every epoch served, in their order
GET ~/RNs/{RN_Id}/history/epochs/{epoch}/checkpoints[/{checkpointId}]  its checkpoints
GET ~/RNs/{RN_Id}/history/epochs/{epoch}/commits/{commitId}[/proof]    a commit, and its proof in a checkpoint
```

### 11.6 Open points

1. **Evidence records:** before an algorithm of an old epoch becomes weak, its checkpoints are to be
   signed again, or their identifications hashed and signed, under the new profiles (as in RFC 4998), so
   that what they prove stays provable.
2. **Lost keys:** an RNO that lost control of its keys can not sign the final checkpoint of its epoch;
   who starts the next one then, e.g. the witnesses, is open.
3. **Price statement digests** stay SHA-256 (`https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/priceStatement/v1`); a later context changes them.
4. **Archived epochs:** when a server drops an old epoch, and where it is kept then.

## 12. Prices at runtime

Charging tariffs are content: they change slowly and are proved by checkpoints (section 10). Ad hoc
prices that change by the minute, like status messages in DATEX II, are the **price status** of
charging pools, charging stations and EVSEs: runtime data without versions,
`{"prices": [{"chargingTariffId": …, "elements": [ … ]}, …]}`, the elements replacing those of charging
tariffs of their operator. Their evidence differs: a **price statement**, signed by the charging station
operator with one of its signing keys (section 4.1) when it sets the price.

```json
{ "roamingNetworkId": "rn1", "entityType": "EVSE", "entityId": "DE*GEF*E1*1",
  "priceStatus": { "prices": [ … ] },
  "validFrom": "2026-10-19T12:00:00.000Z", "validUntil": "2026-10-19T12:15:00.000Z",
  "sequence": 1017, "previous": "<digest of statement 1016 of this entity>",
  "after": "<id of the latest checkpoint the operator knew>",
  "role": "chargingStationOperator", "signer": "DE*GEF", "signedAt": "2026-10-19T11:59:58.000Z",
  "algorithm": "ES256", "keyId": "<key @id>", "value": "<Base64>" }
```

### 12.1 Representation and signature

- **entityType, entityId:** `ChargingPool`, `ChargingStation` or `EVSE`, and its identification. Its
  operator is the one of the identification: `signer` names it, `role` is `chargingStationOperator`.
- **priceStatus:** the price status, signed as it is written. A reader keeps it as given, numbers with a
  fraction as decimals, and checks that it is one.
- **validFrom, validUntil:** when it is valid, written as in section 5.4. `validFrom` is not before
  `signedAt`; without `validUntil`, it is valid until the next statement of its entity.
- **Chain per entity:** `sequence` is 0 for the first statement of an entity and one more for every next
  one; `previous` is the digest of the statement before, absent for sequence 0. The **digest** of a
  statement is the SHA-256 of its canonical JSON with its `@context`, signature value included, as 64
  lowercase hexadecimal digits. Statements are neither reordered nor withdrawn unnoticed.
- **Not before:** `after`, if given, names the latest checkpoint the operator knew: the statement was made
  after it.

The signature input is the canonical JSON of the statement without `value`, its `@context`
(`https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/priceStatement/v1`), `algorithm` and `keyId` included, as for an attestation (8.5). The key MUST be a signing key of the
operator of the entity, valid at `signedAt`. The CBOR representation has the keys and values of the JSON
one.

### 12.2 Taking a statement

A server takes a price statement only when it is one of its roaming network, its entity is one of the
head, it is signed by a signing key of the operator of the entity in the head, valid at `signedAt`, every
charging tariff it names is one of that operator, and it is not over (`validUntil`). Against the current
statement of its entity, if there is one:

- the same statement again is known already;
- one of a lower sequence is out of date;
- another one of the same sequence is refused: two statements of one sequence are a split view;
- the next one MUST name the digest of the current one;
- a later one is taken: the statements in between did not reach this server.

Taken, it is the current statement of its entity and sets its price status as of `validFrom`. A server
holding a signing key of an operator states every change of the price status of the entities of that
operator, following their current statements; when its statements have a `validUntil`, it states the
price status anew before they end. Servers keep the statements they take in their store, so that a chain
goes on after a restart.

### 12.3 Checkpoints

A checkpoint (section 10.1) MAY carry `"priceStatements": {"@merkleRoot": …, "@size": …}`: the Merkle tree
hash (8.2, RFC 9162) of the price statements its server took since the previous checkpoint and that were
signed before it (`signedAt` not after `createdAt`), in the ordinal order of their entity types and
identifications, then of their sequences; a leaf is the 32 bytes of the digest of a statement. It is part
of the content the checkpoint identification is calculated over. A checkpoint naming no statements has
no `priceStatements` and is identified as before.

A server checks the statements a checkpoint names when it gets them along with it
(`GET ~/RNs/{RN_Id}/history/checkpoints/{checkpointId}/priceStatements`, or `priceStatements` of
`pushCheckpoints`): their tree is the one of the checkpoint, and every one was signed before it by a
signing key of its operator in the version of the commit of the checkpoint or of the checkpoint before,
valid at `signedAt`. A server that does not get them takes the checkpoint all the same; a witness signs a
checkpoint naming price statements only after it checked them (10.2).

A proof that a statement is named by a checkpoint is the statement with its index and audit path:

```json
{ "checkpointId": "<checkpoint id>", "priceStatement": <the statement>,
  "@index": 17, "@size": 118, "@auditPath": [ "<hex>", … ] }
```

The verifier calculates the root from the digest of the statement, `@index`, `@size` and the audit path,
and MUST refuse the proof unless it is the `priceStatements` root of the checkpoint, and that checkpoint
carries the signatures it requires: then the statement was made before the checkpoint.

### 12.4 Evidence

A driver's app gets the proof of a charging station (8.4) and the current statements of its pool, of the
station and of its EVSEs, and checks them offline: the proof against the identity keys of the roaming
network operator it pinned (8.6), each statement against what the proof gives: its entity below its
operator, the signing key of the operator it is signed with, and that it is valid when charging begins.
Its prices apply to the charging tariffs the proof gives, those of the EVSE. The app keeps the
statement, the proof of the station and later the proof of the statement in a checkpoint: it was made
after the checkpoint `after` names, and before the one naming it, which witnesses signed.

### 12.5 Over HTTP and the WebSocket

```
GET  ~/RNs/{RN_Id}/CSs/{CS_Id}/priceStatements        the statements of the pool, the station and its EVSEs
GET  ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/priceStatements    those of the pool, the station and the EVSE
GET  ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/priceStatements, …/EVSEs/{EVSE_Id}/priceStatements
GET  ~/RNs/{RN_Id}/history/priceStatements            the current statement of every entity
POST ~/RNs/{RN_Id}/history/priceStatements            { priceStatements: [ … ] }: taken in their order;
                                                      { taken, known, outdated, refused, refusals },
                                                      422 when one was refused
GET  ~/RNs/{RN_Id}/history/checkpoints/{checkpointId}/priceStatements                  those a checkpoint names
GET  ~/RNs/{RN_Id}/history/checkpoints/{checkpointId}/priceStatements/{digest}/proof   the proof of one
```

The WebSocket of a history has the actions `priceStatements` (`{chargingStationId}`, `{EVSEId}` or all),
`addPriceStatements`, `checkpointPriceStatements` and `priceStatementProof`, and the topic
`priceStatements` (action `priceStatement`). A statement needs no authorization: it is signed by the
operator of its entity. A peer takes the statements of the other server and pushes those taken here, and
gets the statements of a checkpoint along with it.

### 12.6 Open points

1. **Retention:** a server keeps the statements of its checkpoints as long as the checkpoints, in its store
   and in memory; how long, and whether the archive (10.6) takes them, is open.
2. **A server without a store** starts the chains of its operators at 0 again after a restart, unless it
   takes the current statements of another server first, as a peer does when it connects; the others
   refuse what it states before as out of date.
3. **Status:** the availability of a station is not stated yet (8.8, point 4).
