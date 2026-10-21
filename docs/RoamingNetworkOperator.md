# RoamingNetworkOperator

The roaming network operator (RNO) runs a roaming network: it owns the network itself and its registries, and it
decides who is a member. In POI 3 it is a party of the roaming network: embedded completely in the
[RoamingNetwork](RoamingNetwork.md) as `operator`, a single object, with the public halves of its
keys. A roaming network has no keys of its own: they are the keys of its operator.

One operator may operate many roaming networks, e.g. `Prod` and `QA`: each of them embeds it, so that each can be
verified from its own history alone. A roaming network without an operator can not be changed where only the
operator may change it: nobody can sign for it.

It owns its own properties, the roaming network itself, the registries (charging station manufacturers, energy
meter manufacturers, transparency software releases and certificates), and adds and removes the other parties. It
signs in the place of a grid operator without keys, and its identity keys recover the keys of every other party.
It never changes the data of another party. Its identification is any nonempty text without surrounding
whitespace, compared case-insensitively, e.g. `GraphDefined`.

| | |
|---|---|
| Class | `RoamingNetworkOperator` — [source](../WWCP_POI3/RoamingNetwork/RoamingNetworkOperator/RoamingNetworkOperator.cs) |
| Identification | `RoamingNetworkOperator_Id` (WWCP_POI2), e.g. `GraphDefined` |
| Admin status | `RoamingNetworkOperatorAdminStatusType`, initially `operational` |
| Status | `RoamingNetworkOperatorStatusType`, initially `available` |
| Embedded in | [RoamingNetwork](RoamingNetwork.md) as `operator` |
| Owner | itself: its data and signing keys by a signing key, its signing keys also by an identity key, its identity keys by an identity key; neither added nor removed |
| Embeds | nothing |
| HTTP | in `GET ~/RNs/{RN_Id}`, as `operator` |


## Common properties

See [Common properties](README.md#common-properties). It writes no `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — |
| `lastChange` | — (set to the change set's `createdAt` by every change of it) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `address` | `Address` | `Address?` | update/remove | Address of the headquarters, as Styx writes it embedded. |
| `homepage` | `Homepage` | `URL?` | update/remove | Homepage. |
| `hotline` | `HotlinePhoneNumber` | `PhoneNumber?` | update/remove | Hotline phone number. |
| `signingKeys` | `SigningKeys` | `ImmutableArray<POIPublicKey>` | update (signing or identity key) | Keys signing its change sets, see [Keys](#keys). Can not be removed. |
| `identityKeys` | `IdentityKeys` | `ImmutableArray<POIPublicKey>` | update (identity key) | Long-term keys signing changes of keys, see [Keys](#keys). Can not be removed. |
| `minConsensus` | `MinConsensus` | `Byte`, 1 by default, written unless 1 | update (identity keys) | How many holders of its keys of one kind sign what it owns, see [Quorum](#quorum). |
| `keyStatement` | `KeyStatement` | `RoamingNetworkKeyStatement?` | update/remove | Its signing keys stated by its identity keys, for apps ([SPECIFICATION.md](../SPECIFICATION.md#86-key-statements-and-trust-anchors), section 8.6). |

A change set names the properties above by their JSON keys (`RoamingNetworkOperatorRegistry` in
[RoamingNetwork.RegistryChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.RegistryChanges.cs)), with
`entityType` `RoamingNetworkOperator` and its `@id`. `Add` and `Remove` of it are refused: it comes with the
roaming network it operates.

```csharp
RoamingNetworkChange.UpdateProperty(rn.Operator!, "homepage",    null, "https://open.charging.cloud")  // signed by its signing key
RoamingNetworkChange.UpdateProperty(rn.Operator!, "signingKeys", null, new JArray(newKey.ToJSON()))   // signed by a signing or an identity key
```

The party of its signatures is `POIPartyId("RoamingNetworkOperator", id)`, `RoamingNetwork.OperatorParty`; it is
the first of `RoamingNetwork.Parties`.


## Keys

The public keys of the RNO (`POIPublicKey`, [source](../WWCP_POI3/DataStructures/POIPublicKey.cs); checked by
`POIPartyKeys`). Every party has the same two arrays; see [../README.md](../README.md#change-sets) and
[SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners).

| JSON key | Type | Description |
|---|---|---|
| `@id` | string | The SHA-256 COSE key thumbprint [RFC 9679] of `key`, 64 lowercase hex digits. Must match `key`. |
| `key` | string | Canonical Base64 of the deterministic CBOR of the public COSE key; private parts are refused. |
| `notBefore` | timestamp | Optional start of validity (UTC, to the millisecond). |
| `notAfter` | timestamp | Optional end of validity; not before `notBefore`. |
| `holder` | string | Optional: who holds the private key, e.g. one company of a [quorum](#quorum). No part of `@id`. |

- **signingKeys** — short keys (e.g. secp256r1) signing change sets of RNO data: its own properties, the network's
  properties, the registries, adding and removing parties, and the rotation of the `signingKeys` themselves. A
  server operating the network signs its commits with one as well, in the role `roamingNetworkOperator`.
- **identityKeys** — long-term keys (e.g. secp521r1) signing changes of the RNO's `identityKeys`, of its
  `signingKeys` when none of them can sign any more, and, for recovery, of the keys of any other party.
- **Two identity keys at least.** Nobody can replace the RNO's identity keys once they are lost or expired: there is
  nobody above the RNO, and no external trust configuration. An RNO should therefore name at least two identity keys
  with different `notAfter`, one of them kept offline, and replace an identity key well before it expires. This is
  advice, not checked.
- Both arrays are ordered by `@id` (ordinal); a key must not be given twice, nor be both a signing and an identity key.
- A key counts only when valid at the `createdAt` of the change set (`IsValidAt`), and only in the version the
  change set is applied to.
- A change set can only replace them with a nonempty array (`UpdateProperty`); `RemoveProperty` is refused.


## Quorum

Several holders, e.g. companies, may share the keys of the RNO, so that none of them changes the network alone
([SPECIFICATION.md](../SPECIFICATION.md#41-keys-and-owners), section 4.1): each names its keys with its `holder`,
and the RNO names its `minConsensus`, e.g. 2 of 3.

- **What needs it:** everything the RNO signs needs signatures with keys of `minConsensus` different holders: its
  data and that of the network with signing keys; its keys, its `minConsensus` and the recovery of the keys of
  other parties with identity keys; its key statement (`wwcp-poi-key-statement-v2`, signed by identity keys of as
  many holders). An app takes a version only when signing keys of as many holders attest it.
- **Holders, not keys:** a holder renewing its key signs with two keys, but has one voice. Keys naming no holder
  have one holder together: an RNO without a quorum is one holder with `minConsensus` 1.
- **More than half:** `minConsensus` is more than half of the holders of each kind of keys and at most as many, so
  that two quorums always share a holder (`POIPartyKeys.CheckQuorum`): 2 of 3, 3 of 4, 3 of 5. An odd number of
  holders is not needed.
- **Signing on one's own:** each holder signs a change set or key statement with its own key and adds its
  signature to the others (`changeSet.Sign(…)`, `keyStatement.WithSignature(…)`); one of them commits it.

```csharp
var statement = RoamingNetworkKeyStatement.Create([ (alphaIdentityKey, COSEAlgorithm.ES512) ], 2,
                                                  rn.Id, rn.Operator!.Id, rn.Operator.SigningKeys, rn.Operator.IdentityKeys,
                                                  now, now.AddDays(30)).
                    WithSignature(betaIdentityKey, COSEAlgorithm.ES512);   // at the place of Beta
```


## Runtime status

Admin status and status are kept in the status schedules shared by all versions, see
[README.md](README.md#runtime-status); in a history as `entityType` `RoamingNetworkOperator`. No energy status.

| | Type | Predefined values | Initially |
|---|---|---|---|
| Admin status | `RoamingNetworkOperatorAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` |
| Status | `RoamingNetworkOperatorStatusType` | `unknown`, `unspecified`, `offline`, `inDeployment`, `available`, `error` | `available` |
