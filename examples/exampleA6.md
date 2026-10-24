# Example A6: witnesses sign the checkpoints

Two **witnesses**, independent of the roaming network operator `OCC`, sign the three checkpoints of `QA` of
[example A4](exampleA4.md) and [example A5](exampleA5.md): a calibration authority, `calibration.example`, and an
association of operators, `association.example`. Each keeps its own copy of the history of `QA`, taken commit by commit
since the genesis and each commit checked, and signs a checkpoint only after it checked it against that copy. `OCC`
takes their signatures and publishes the checkpoints with them. A verifier then requires the signatures of `OCC` and of
both witnesses, and a proof of a commit becomes evidence of a history the witnesses saw.

| Revision | Signed by | |
|---|---|---|
| 0 to 10 | see [example A1](exampleA1.md) to [example A5](exampleA5.md) | no new commit |
| checkpoints 0, 1, 2 | `OCC`, its three holders, and now **both witnesses** | the same checkpoints, the same `@id`s |

Why witnesses (SPECIFICATION.md, section 10.2): a checkpoint signed by the roaming network operator alone proves what
the operator states. A witness signs a checkpoint only after it refused nothing of section 10.1 in its own history, and
never two different checkpoints of the same roaming network, epoch and `sequence`. So an operator showing one history to
some and another one to others, a **split view**, cannot have both witnessed, [step 5](#5-a-split-view-is-refused).
Whoever kept a proof of a commit shows months later, e.g. disputing an invoice, that the commit is part of the history the
witnesses saw, without asking the server that made it, [step 7](#7-evidence).

The code is [exampleA6.cs](csharp/exampleA6.cs), with the helpers of [exampleA4.cs](csharp/exampleA4.cs), called by
[Program.cs](csharp/Program.cs) after example A5:

```csharp
var a6 = ExampleA6.Run(a1);

ExampleA6.Validate();
```

The output is what one run printed, `…` for elided digests.


## 1. The witnesses and their keys

Each witness makes its key itself. Its public key is trusted by configuration, independently of `QA`, as the keys of
attestations are: whoever requires a witness knows its key, here from `trusted/witnesses.json`, not from anything `QA`
publishes.

```csharp
var keys = new Dictionary<String, COSEKeyPair> {
               { Witnesses[0], COSEKeyPair.NewEd25519() },
               { Witnesses[1], COSEKeyPair.NewES384()   }
           };
```

```json
[
  { "witness": "calibration.example", "key": { "@id": "bb82338b…", "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/publicKey/v1", "key": "owEBIAYh…" } },
  { "witness": "association.example", "key": { "@id": "a36cc66e…", … } }
]
```


## 2. What the roaming network operator publishes for them

A witness begins where `QA` begins: at the genesis snapshot, `published/QA-genesis.json`, which
[example A1](exampleA1.md#6-the-history-and-its-genesis-commit) published, `{ timestamp, commit, roamingNetwork }` of
revision 0. Then it takes the commits since the genesis, `published/QA-commits-since-genesis.json`, as
`GET ~/RNs/QA/history/commits?since={genesis}` gives them, and the checkpoints of examples A4 and A5.

```csharp
if (!A1.History.TryGetCommitsSince(A1.History.Root.Id, out var commits, out var error))
    Helpers.Fail(error);

File.WriteAllText("published/QA-commits-since-genesis.json", new JArray(commits.Select(commit => commit.ToJSON())).ToString());
```


## 3. Each witness checks QA and signs its checkpoints

`WitnessHistory(…)` builds the history of `QA` as a witness keeps it, from what is published only:

- the genesis: of the roaming network of the trust anchor, its key statement signed by enough holders, and the genesis
  commit signed by the quorum of `OCC`;
- every commit since, with `TryAppend`: its change set signed by its owners and leading to its ETags, the commit signed as
  the roaming network operator;
- every checkpoint, with `TryAddCheckpoint`: its commit held, with its revision and ETags, its commits those since the
  checkpoint before, with the Merkle root it signs, and signed by `OCC`. Signatures of other witnesses are not checked
  there.

Then the witness signs each checkpoint, as itself and in the role `witness`, `SignerOf(…)`. `TryWitness` signs only a checkpoint its
history holds, and it holds one per sequence:

```csharp
var signer  = SignerOf(witness, key);

foreach (var checkpoint in history.Checkpoints)
{

    if (!history.TryWitness(checkpoint.Id, signer, out _, out var signature, out error))
        Helpers.Fail(error);

    signatures.Add((checkpoint.Id, signature));

}
```

```
calibration.example: the genesis and 10 commits checked, 3 checkpoints taken
calibration.example signs checkpoint 0
calibration.example signs checkpoint 1
calibration.example signs checkpoint 2
association.example: the genesis and 10 commits checked, 3 checkpoints taken
association.example signs checkpoint 0
association.example signs checkpoint 1
association.example signs checkpoint 2
```


## 4. The roaming network operator takes their signatures

The witnesses send their signatures to the server that made the checkpoints, e.g.
`POST ~/RNs/QA/history/checkpoints/{checkpointId}/signatures` or `signCheckpoint` over the WebSocket. It takes those of
the witnesses it trusts, `TryAddCheckpointSignatures`, and publishes the checkpoints and their snapshots again. The
signatures do not change the identification of a checkpoint: its `@id` is the digest of its content without them, the
same as in examples A4 and A5.

```csharp
var verify = VerifyCheckpointSignature(A1.TrustAnchor, A1.RoamingNetwork, TrustedWitnesses());

foreach (var group in signatures.GroupBy(signature => signature.CheckpointId))
{

    if (!A1.History.TryAddCheckpointSignatures(group.Key, [.. group.Select(signature => signature.Signature)], verify, out var signed, out var newSignatures, out error))
        Helpers.Fail(error);

    …

}
```

```
Checkpoint 0, 62b7ced1…: 2 signatures more, 5 in all
Checkpoint 1, 5346668e…: 2 signatures more, 5 in all
Checkpoint 2, 2fe73391…: 2 signatures more, 5 in all
```

The signatures of checkpoint 1, as `published/QA-checkpoint-1.json` has them now:

```json
"signatures": [
  { "role": "roamingNetworkOperator", "signer": "OCC",                 "algorithm": "ES256",   "keyId": "2ec1ef50…", … },
  { "role": "roamingNetworkOperator", "signer": "OCC",                 "algorithm": "ES256",   "keyId": "b8e0268b…", … },
  { "role": "roamingNetworkOperator", "signer": "OCC",                 "algorithm": "ES256",   "keyId": "2804f9ab…", … },
  { "role": "witness",                "signer": "calibration.example", "algorithm": "Ed25519", "keyId": "bb82338b…", … },
  { "role": "witness",                "signer": "association.example", "algorithm": "ES384",   "keyId": "a36cc66e…", … }
]
```


## 5. A split view is refused

Another checkpoint of the sequence 2, of the same commit, made a minute later and signed by `OCC` as well: shown to some,
it would let the operator tell them another history than the one the witnesses saw. A witness takes no second
checkpoint of a sequence, and so never signs two:

```csharp
var other      = RoamingNetworkCheckpoint.Create(latest.RoamingNetworkId,
                                                 latest.Epoch,
                                                 latest.Sequence,
                                                 latest.PreviousId,
                                                 A1.History.Head.Commit,
                                                 [ A1.History.Head.Commit ],
                                                 latest.CreatedAt.AddMinutes(1));

other          = other.WithSignatures(A1.Signers.Select(signer => signer.Sign(other)));
```

```
Another checkpoint 2, db7225a1…, at calibration.example: This history has another checkpoint of the sequence 2: two checkpoints of one sequence are a split view!
```


## 6. Checkpoints witnessed

`ExampleA6.Validate()` checks as a verifier that requires the roaming network operator and both witnesses, from what is
published and the keys of the witnesses only. a) `QA` at its head, as everybody checks it. b) Each checkpoint: signed by
the quorum of `OCC`, as in [example A4](exampleA4.md#3-joining-from-the-checkpoint), and by both witnesses, each with the
key the verifier trusts for it:

```csharp
internal static IReadOnlyList<String> WitnessedBy(RoamingNetworkCheckpoint Checkpoint, IReadOnlyDictionary<String, POIPublicKey> Witnesses)

    => [.. Checkpoint.Signatures.Where (signature => signature.Role == RoamingNetworkCommitSignatureRoles.Witness &&
                                                     signature.Signer is String witness &&
                                                     Witnesses.TryGetValue(witness, out var key) &&
                                                     Checkpoint.VerifySignature(signature, key.Key, key.Id, out _)).
                                 Select(signature => signature.Signer!).
                                 Distinct()];
```

c) Joining from the latest checkpoint, requiring both roles, `roamingNetworkOperator` and `witness`: every signature
must be valid, by the verifier given, and of the roles required.

```
Checkpoint 0 of epoch 1: revision 7, 8 commit(s) since the genesis, signed by AlphaOrg, BetaOrg, GammaOrg
  witnessed by calibration.example, association.example
Checkpoint 1 of epoch 1: revision 9, 2 commit(s) since checkpoint 62b7ced1…, signed by AlphaOrg, BetaOrg, GammaOrg
  witnessed by calibration.example, association.example
Checkpoint 2 of epoch 1: revision 10, 1 commit(s) since checkpoint 5346668e…, signed by AlphaOrg, BetaOrg, GammaOrg
  witnessed by calibration.example, association.example

Joined from checkpoint 2, signed by the roaming network operator and its witnesses: revision 10
```


## 7. Evidence

The proof of revision 8 of [example A5](exampleA5.md), the pool "Jena Lobeda" with its AC station as example A4 recorded
it, in checkpoint 1. Kept, e.g. by an e-mobility provider disputing an invoice of a session there, it shows without
asking the server of `QA` that this commit is part of the history both witnesses saw:

```
The proof of revision 8 in checkpoint 1, witnessed by calibration.example, association.example: valid
```


## 8. What is refused

a) The snapshot of checkpoint 2 without the signatures of the witnesses: it is not signed in the role `witness`. b) A
witness signature by a key nobody trusts, here one just made, naming `calibration.example`.

```
Checkpoint 2 without its witnesses: The checkpoint of the snapshot can not be taken: The checkpoint '2fe73391…' is not signed in the role 'witness'!
A signature of calibration.example by another key: refused, not the key trusted for it
```


## With the hub

POI_API takes the signatures of witnesses as `POST ~/RNs/QA/history/checkpoints/{checkpointId}/signatures`, when
adding them is enabled and its verifier trusts the witness. The RoamingHubCLI makes no checkpoints of its own yet, see
[example A4](exampleA4.md#with-the-hub).


## Next

- The witnesses go on with their copies, `ExampleA6.Result`, and check the price statements of a checkpoint: [example A9](exampleA9.md).
- Compacting the history after the checkpoints, with an archive: [example A7](exampleA7.md).
- Witness discovery: how a client learns which witnesses there are, and their keys, is open (section 10.7).
- A witness also checks the price statements a checkpoint names before it signs (section 12.3).
