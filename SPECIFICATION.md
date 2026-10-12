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
  `dataSource`, `dataLicenses`, every embedded entity completely (operators with their pools,
  stations, EVSEs, connectors, energy meters, tariffs and groups; manufacturers, e-mobility
  providers, grid operators, parking operators, brands, energy meter manufacturers, transparency
  software releases and certificates), `created`, `lastChange` and `customData`.
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


## 3. ETags

An ETag is a digest of the content in one of two formats.

| Format | Digest over |
|---|---|
| `json` | the canonical JSON of the content (UTF-8) |
| `cbor` | the deterministic CBOR of the content |

### 3.1 Representations

- **JSON:** `["json", "sha256", "hex", "<64 lowercase hex digits>"]`; the encoding MAY also be
  `base64`, with canonical padding.
- **CBOR:** `[format, "sha256", h'<digest>']`, e.g. `["cbor", "sha256", h'631d…']`, the digest as a byte string.
- **Text:** `json:sha256:hex:<digest>`.
- **Algorithms:** `sha256` (default), `sha384`, `sha512`.
- A roaming network lists its ETags in `eTags`, ordered by format: `json`, then `cbor`.

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
  "beforeETags": [ … ], "afterETags": [ … ], "signatures": [ … ] }
```

**Signatures** (profile `wwcp-poi-changeset-json-v3`), e.g. of the charging station operator
whose data it changes: `{ algorithm, keyId, value, profile, encoding: "base64" }`.
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


## 5. Commits

A commit is one step of the history: a change set applied to the version of its parent.

### 5.1 Identification

The commit identification is the SHA-256 of the canonical JSON of

```json
{ "profile": "wwcp-poi-commit-v1", "roamingNetworkId": "rn1", "revision": 1,
  "eTags": [ <the ETags of the resulting version> ],
  "parentId": "<parent commit id>", "changeSet": <the change set without its signatures> }
```

as 64 lowercase hexadecimal digits. A genesis commit has neither `parentId` nor `changeSet`.
Signatures are not part of it: neither those of the change set nor those of the commit, so that
they can be added later without changing the identification.

### 5.2 Representation

```json
{ "@id": "<commit id>", "roamingNetworkId": "rn1", "revision": 1, "eTags": [ … ],
  "parentId": "…", "changeSet": <the change set with its signatures>,
  "signatures": [ <commit signatures> ] }
```

A reader MUST calculate the identification and refuse the commit when it differs from `@id`.
The CBOR representation has the keys and values of the JSON one.

### 5.3 Signatures

A commit can be signed by several parties, each on its own, in any order: the leader that made
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

A server that follows a leader MUST verify every signature of a commit before it takes the commit,
and MAY require signatures in certain roles, e.g. `leader`. Signatures added later are sent with
`signCommit` over the WebSocket or `POST …/history/commits/{commitId}/signatures`, and pushed
on the topic `commitSignatures`.


## 6. Examples

A roaming network `rn1` named "Example", created 2026-01-01T00:00:00Z, with one operator `DE*GEF`,
one pool, one station and one EVSE `DE*GEF*E1*1` of 22000 W and 230.5 V.

**Content** (canonical JSON):

```json
{"@context":"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork","@id":"rn1","chargingStationOperators":[{"@id":"DE*GEF","chargingPools":[{"@id":"DE*GEF*P1","chargingStations":[{"@id":"DE*GEF*S1","EVSEs":[{"@id":"DE*GEF*E1*1","created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z","maxPower":22000,"maxVoltage":230.5}],"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z"}],"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z"}],"created":"2026-01-01T00:00:00.000Z","dataLicenses":[],"lastChange":"2026-01-01T00:00:00.000Z","name":{"en":"Charging Station Operator DE*GEF"}}],"created":"2026-01-01T00:00:00.000Z","lastChange":"2026-01-01T00:00:00.000Z","name":{"en":"Example"}}
```

**ETags:**

```
json:sha256:hex:a3432d3ec7622d4272a7d4c0eba47883ee562c5501804285d9b01aea0dd692de
cbor:sha256:hex:631d24121cc4ee0177f262b6703b1ddfd63552dc69109c77971ae079e679f421
```

**Genesis commit:** the SHA-256 of

```json
{"eTags":[["json","sha256","hex","a3432d3ec7622d4272a7d4c0eba47883ee562c5501804285d9b01aea0dd692de"],["cbor","sha256","hex","631d24121cc4ee0177f262b6703b1ddfd63552dc69109c77971ae079e679f421"]],"profile":"wwcp-poi-commit-v1","revision":0,"roamingNetworkId":"rn1"}
```

is the commit identification `f572ecd1b3ae708cb7eacf0e9da3a34537aae55c94d88ad7224a9bef1663c45f`.

**Leader signature** of the genesis commit, with the Ed25519 key of the private seed
`101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f` (public key, Base64:
`d3bocLkzVPKgskwj8qNsxOgOIjIYwbl5Jv3QGDlqK5s=`), key identification `leader`, signer
`leader.example.org`, signed at 2026-10-12T12:00:00.000Z. Signature input:

```json
{"algorithm":"Ed25519","commit":{"@id":"f572ecd1b3ae708cb7eacf0e9da3a34537aae55c94d88ad7224a9bef1663c45f","eTags":[["json","sha256","hex","a3432d3ec7622d4272a7d4c0eba47883ee562c5501804285d9b01aea0dd692de"],["cbor","sha256","hex","631d24121cc4ee0177f262b6703b1ddfd63552dc69109c77971ae079e679f421"]],"revision":0,"roamingNetworkId":"rn1"},"encoding":"base64","keyId":"leader","profile":"wwcp-poi-commit-signature-v1","role":"leader","signedAt":"2026-10-12T12:00:00.000Z","signer":"leader.example.org"}
```

Signature value:

```
su8SeDqPhzRxZYvEvQ89OiaIDKjVhnMShfO+iPsoGzqAAel6JMykYRKlttQGBYB5uyRIRe+P15XYrNmiDPf0CA==
```


## 7. Open points

- HTTP `ETag` and `If-None-Match` headers are not served yet.
