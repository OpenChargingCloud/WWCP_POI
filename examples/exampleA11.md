# Example A11: new locks, same house – epoch 2

Cryptography ages. `QA` moves into **epoch 2**, of SHA-512 identifications and of the post-quantum signatures ML-DSA-65
and ML-DSA-87 only, without throwing its history away (SPECIFICATION.md, section 11). The new locks are fitted next to
the old ones first, still in epoch 1; the final checkpoint 6 closes epoch 1 and names epoch 2; the first commit of epoch
2 takes the old locks out. Its first checkpoint is signed with the old and the new keys, so a verifier trusting only
those of epoch 1 follows. Epoch 1 stays readable, and the driver's evidence of [example A10](exampleA10.md) proves what
it proved.

| Revision | Signed by | |
|---|---|---|
| 0 to 14 | see [example A1](exampleA1.md) to [example A10](exampleA10.md) | epoch 1, SHA-256, every signature algorithm |
| 15 | the quorum of `OCC`: the old identity and signing keys of `AlphaOrg` and `BetaOrg`, and their new identity keys as proofs | ML-DSA identity and signing keys next to the old ones |
| 16 | `DE*GEF`: an old identity key, and its new ones as proofs | ML-DSA keys in place of its old ones |
| checkpoint 6, final | `OCC` with the old and the new keys, both witnesses with their old keys | of revision 16, naming epoch 2 |
| genesis of epoch 2, revision 0 | `OCC`, its ML-DSA keys | the version of checkpoint 6, nothing applied |
| 1 | the quorum of `OCC`: the ML-DSA identity and signing keys of `AlphaOrg` and `BetaOrg` | the old keys out |
| 2 | `DE*GEF`, its ML-DSA `tariffs` key | the DC charging tariff lowered to 0.69 €/kWh |
| price statement, sequence 3 | `DE*GEF`, its ML-DSA `tariffs` key | 0.59 €/kWh at `DE*GEF*E1*1`, after checkpoint 6 |
| checkpoint 0 of epoch 2 | `OCC` and both witnesses, each with its old and its new key | of revision 2, naming checkpoint 6 and the statement |

The code is [exampleA11.cs](csharp/exampleA11.cs), with the helpers of [exampleA4.cs](csharp/exampleA4.cs),
[exampleA6.cs](csharp/exampleA6.cs) and [exampleA9.cs](csharp/exampleA9.cs), called by [Program.cs](csharp/Program.cs)
with what examples A1, A2 and A6 left:

```csharp
using var a11 = ExampleA11.Run(a1, a2, a6);

ExampleA11.Validate();
```

The history of epoch 2 is kept in `poi/QA-epoch-2/`, beside the one of epoch 1 in `poi/QA/`. The output is what one run
printed.


## 1. New locks next to the old ones: ML-DSA keys

`AlphaOrg` and `BetaOrg` make a new identity key, ML-DSA-87; the one of `GammaOrg` is ML-DSA-65 since
[example A1](exampleA1.md). Each organization makes a new signing key, ML-DSA-65, and certifies it with its post-quantum
identity key, alone. `DE*GEF` makes two identity keys, ML-DSA-65 and ML-DSA-87, and an ML-DSA-65 signing key for each
usage. The witnesses make ML-DSA-65 keys, trusted by configuration as their first ones were: `trusted/witnesses-mldsa.json`.


## 2. OCC states its new keys, next to the old ones

Revision 15 changes three properties of `OCC`: its identity keys, the old and the new ones; its key statement, naming all
five; and its certificates, of the old and the new signing keys. The key statement is signed by the **old** identity keys
of `AlphaOrg` and `BetaOrg`: a verifier trusting the old trust anchor trusts the new keys as well (section 8.6,
*Succession*).

```csharp
var bridgeStatement  = RoamingNetworkKeyStatement.Create(A1.IdentityKeys["AlphaOrg"],
                                                         ExampleA1.minConsensus,
                                                         A1.RoamingNetwork.Id,
                                                         occ.Id,
                                                         IdentityKeys:  allIdentityKeys,
                                                         SignedAt:      now,
                                                         ValidUntil:    now.AddYears(1)).
                           WithSignature(A1.IdentityKeys["BetaOrg"]);
```

The change set is signed by the quorum, two of the three holders: with their identity keys for the identity keys and
the certificates, with their signing keys for the key statement, which is data of `OCC`; and by each new identity key, as
the proof that its holder holds it (section 4.1). From now on `OCC` signs commits and attestations with its ML-DSA
signing keys, and the final checkpoint with the old ones as well:

```csharp
A1.History.Signers             = [ .. signers ];
A1.History.AttestationSigners  = [ .. signers ];
A1.History.CheckpointSigners   = [ .. A1.Signers, .. signers ];
```


## 3. DE*GEF replaces its keys

Its keys are its own: revision 16 replaces its identity keys and its signing keys, signed by one of its old identity keys
and by each new identity key as its proof. Epoch 1 accepts every algorithm, so its new keys sign at once; its old ones
are not needed any more.

```
Committed, revision 15 of epoch 1: OCC: ML-DSA keys next to the old ones
Committed, revision 16 of epoch 1: DE*GEF: ML-DSA keys in place of its old ones
```


## 4. The final checkpoint closes epoch 1

`TryCloseEpoch` makes checkpoint 6 of the head, its only tip: `"final": true` with the next epoch and its profiles.

```csharp
internal static readonly RoamingNetworkEpochProfiles PostQuantum = new (ETagHashAlgorithm.SHA512, [ "ML-DSA-65", "ML-DSA-87" ]);

if (!A1.History.TryCloseEpoch(PostQuantum, out var final, out var error))
    Helpers.Fail(error);
```

Epoch 1 takes no commit after it. The witnesses take revisions 15 and 16, each checked in the version before, and
checkpoint 6, and sign it with their old keys, as every checkpoint of epoch 1.

```
Checkpoint 6, final, of revision 16: 2 commit(s), names epoch 2, sha512, ML-DSA-65, ML-DSA-87
Epoch 1: Closing; a commit now: The epoch 1 of the roaming network 'QA' is closed by its final checkpoint 'ad9ede39f9de6951c5126ecc3fb20944877a0fc958e9c265af9fe51b4456d666': what follows belongs to the epoch 2!
Checkpoint 6, ad9ede39…: 2 signatures more, 8 in all
```


## 5. Epoch 2 starts from the version of the final checkpoint

`TryStartNextEpoch` applies nothing: the genesis of epoch 2 has the version of checkpoint 6, revision 0, and is
identified with SHA-512, 128 hexadecimal digits. Its ETags are those of checkpoint 6. Of the current price statements,
those signed with an algorithm of epoch 2 are taken along (section 11.4): none, as `DE*GEF` signed those of examples A8
and A10 with ES256; their chains go on in epoch 2 all the same (7). Every server starting epoch 2 from checkpoint 6 makes
the same genesis: the witnesses, from their own copies, make it as well. A server serves it in place of epoch 1 at the same URL (`TryServeNextEpoch` of POI_API); epoch 1 stays
readable and still proves its commits, e.g. the one of the driver's station proof.

```csharp
var store = new RoamingNetworkHistoryFileStore("poi/QA-epoch-2");

if (!A1.History.TryStartNextEpoch(out var epoch2, out error, Signers: signers, Store: store))
    Helpers.Fail(error);
```

```
Epoch 2, genesis 128 hexadecimal digits: 8cc0eff56abca778da1eef00f94ae525b5621b7fa064f6177661462c6781486790a38407d6d997abab5d6434834703e8d1e9e4657deec7c7238d683baa502c95
  revision 0, the ETags of checkpoint 6: the same; epoch 1: Closed
  price statements taken along: 0 of 2; not those signed with ES256
  calibration.example: genesis 8cc0eff5…, the same
  association.example: genesis 8cc0eff5…, the same
Epoch 1, revision 13: proved in checkpoint 4, as the driver keeps it
```


## 6. The first commit of epoch 2 takes the old locks out

Revision 1 of epoch 2 leaves `OCC` its ML-DSA identity keys only, a key statement of them signed by the ML-DSA identity
keys of `AlphaOrg` and `BetaOrg`, and the certificates of its ML-DSA signing keys. Signed by the old keys, as it would
have been in epoch 1, it is refused: epoch 2 accepts its own algorithms only. A version of epoch 2 is refused as well
while its key statement or a certificate is signed otherwise: so the old locks go with its first commit.

The trust anchor changes with the identity keys: `published/QA-trustAnchor-mldsa.txt` names the ML-DSA ones, the QR code
on the website of `OCC` is replaced. It is as long as before, 511 characters: it names the `@id`s of the keys, not the
keys.

```
Signed by the old identity keys: The signature algorithm 'ES512' of a signature of the change set is not one of the epoch 2, ML-DSA-65, ML-DSA-87!
Committed, revision 1 of epoch 2: OCC: the old keys out
The new trust anchor: 3 keys, minConsensus 2, QR code of 511 characters
```


## 7. DE*GEF in epoch 2

`DE*GEF` lowers its DC charging tariff with its ML-DSA `tariffs` key, revision 2, and states 0.59 €/kWh at
`DE*GEF*E1*1`: the chain of price statements goes on. Epoch 2 did not take the statement of example A10 along, signed with
ES256, but sequence 3 names it all the same, the last one of epoch 1, and, as the latest checkpoint `DE*GEF` knows,
checkpoint 6. No server checks it against that one: none holds it as the current one of epoch 2. A system of `DE*GEF`
still signing with its old ES256 key is refused, for its commits and its statements alike.

```csharp
var previous = A1.History.PriceStatementsOf(ExampleA10.EVSEId).Single(statement => statement.IsOf(nameof(EVSE), ExampleA10.EVSEId.ToString()));
```

```
Signed by its old tariffs key: The signature algorithm 'ES256' of a signature of the change set is not one of the epoch 2, ML-DSA-65, ML-DSA-87!
Committed, revision 2 of epoch 2: The DC charging tariff: 0.69 €/kWh
A price statement by its old tariffs key: The signature algorithm 'ES256' of the price statement is not one of the epoch 2, ML-DSA-65, ML-DSA-87!
DE*GEF*E1*1, 0.59 €/kWh, sequence 3, ML-DSA-65: Taken
```


## 8. The first checkpoint of epoch 2, with the old and the new locks

Checkpoint 0 of epoch 2 names the final checkpoint of epoch 1, `"previous": {"epoch": 1, "checkpointId": …, "eTags": …}`,
and is signed with the algorithms of both epochs: by the old signing keys of `OCC`, as the genesis certifies them, and by
its new ones; the witnesses check the commits of epoch 2 and the price statement and sign it with their old and their
new keys. Its signatures are accepted with the algorithms of either epoch, every later checkpoint's only with those of
epoch 2.

```csharp
epoch2.CheckpointSigners = [ .. signers, .. A1.Signers ];

if (!epoch2.TryMakeCheckpoint(out var first, out error))
    Helpers.Fail(error);
```

```
Checkpoint 0 of epoch 2, of revision 2: 3 commit(s), 1 price statement(s), after checkpoint 6 of epoch 1, signed with ML-DSA-65, ES256
calibration.example: revision 2 of epoch 2, signs checkpoint 0 with Ed25519 and ML-DSA-65
association.example: revision 2 of epoch 2, signs checkpoint 0 with ES384 and ML-DSA-65
Checkpoint 0 of epoch 2: 10 signatures
```

The new locks are larger: an ML-DSA-65 signature has 3,309 bytes, an ES256 one 64. Checkpoint 0 of epoch 2, with its 10
signatures, is 27 KiB as JSON; checkpoint 6, of 8 signatures, 17 KiB.


## 9. A verifier trusting only the old locks

`ExampleA11.Validate()` checks the move from what is published only. The first verifier trusts what it trusted in epoch
1: the trust anchor `QA-trustAnchor.txt` it scanned once, and the old keys of the witnesses, `trusted/witnesses.json`.

a) It holds `QA` as of checkpoint 5, joined from its snapshot. b) It takes revisions 15 and 16, each checked in the
version before: revision 15 by the old quorum, so it trusts the new keys of `OCC` as well; and the final checkpoint 6.
c) It starts epoch 2 itself and gets the genesis the server published. d) It takes the commits of epoch 2 and checkpoint
0, checking the signatures it can, of `OCC` and of the witnesses it trusts; those of the new witness keys are no business
of this verifier. With the old keys alone, checkpoint 0 is signed by the quorum of `OCC` and by both witnesses: the
history goes on in epoch 2 under the trust of epoch 1. e) The head of epoch 2 with the old trust anchor alone is refused;
the new trust anchor names the identity keys and the quorum of the key statement the verifier followed, so it takes that
one from now on.

```
Old trust: revision 16, checkpoint 6, final, witnessed by calibration.example, association.example; next: epoch 2, sha512, ML-DSA-65, ML-DSA-87
Old trust: epoch 2 started, genesis 8cc0eff5…, the published one
Old trust: checkpoint 0 of epoch 2, after checkpoint 6 of epoch 1: the final one it took; with the old keys signed by AlphaOrg, BetaOrg, GammaOrg, witnessed by calibration.example, association.example
The head with the old trust anchor alone: The key statement is signed by trusted identity keys of 0 holders, not of 2!
The new trust anchor: the identity keys and the quorum of the key statement it followed
```


## 10. A verifier trusting only the new locks

The second verifier trusts the new trust anchor and the ML-DSA keys of the witnesses only. It joins epoch 2 from the
snapshot of checkpoint 0, checking only the signatures of the algorithms of epoch 2, and its price statement. It refuses
a commit of epoch 1 given as one of epoch 2, and a genesis of another version. The evidence of the driver of example A10,
in epoch 1, is as valid as before.

```
New trust: joined epoch 2 from checkpoint 0, revision 2, 128 hexadecimal digits, signed with ML-DSA-65, witnessed by calibration.example, association.example
DE*GEF*E1*1, sequence 3, 0.59 €/kWh, ML-DSA-65, after checkpoint 6 of epoch 1: the final one
A commit of epoch 1: Commit 0: The commit 'd9debbc19995ca411f1c480d0aeb53a0b4c3b9b2ca5752bee1256968ad639a5a' is one of the epoch 1, but this history is of the epoch 2!
A genesis of another version: Invalid roaming network of the snapshot: The ETags of the roaming network 'QA' do not match its content: The JSON ETag (SHA256) differs!
The driver's evidence of example A10, revision 13 in checkpoint 4 of epoch 1: valid
```


## Limits

- Price statement digests and the `json` and `cbor` ETags stay SHA-256 in epoch 2 (sections 11.1 and 11.6); the
  `effectiveTree` ETags follow the `treeHashAlgorithms` of `QA`, which its operator changes within an epoch.
- The checkpoints of epoch 1 stay signed by ES256 and the other algorithms of epoch 1. Before one of them becomes weak,
  they are to be signed again, or their identifications hashed and signed, under the profiles of epoch 2: evidence
  records, as in RFC 4998, are open (section 11.6). So is who starts the next epoch when the roaming network operator
  lost its keys.
- The witnesses are trusted by configuration, their new keys as well; a signed list of them is open, as in
  [example A10](exampleA10.md).
- The hub makes no checkpoints yet, and so closes no epoch.


## Next

- The atlas and the traffic report: `QA` of epoch 2 served over its WebSocket, a mirror following it, a driver's app
  told live of the status of `DE*GEF*E1*1`, and of a commit: [example A12](exampleA12.md).
