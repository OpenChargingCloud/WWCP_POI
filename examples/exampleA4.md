# Example A4: the first checkpoint, two pools more, and joining from the checkpoint

The roaming network operator `OCC` makes the first **checkpoint** of `QA`, of its revision 7: a statement, signed by
its quorum, that this commit and every commit before it are the history of `QA`. Then `DE*GEF` of
[example A3](exampleA3.md) adds two pools more, after the same pattern. Their AC stations are recorded as those of A3,
with the AC template, 44 kW, and EVSEs of 22 kW, although they have two EVSEs of 11 kW only;
[example A5](exampleA5.md) corrects them. Finally a server or an app that has no history of `QA` joins it from the
checkpoint, instead of taking every commit since the genesis.

| Revision | Signed by | Commit |
|---|---|---|
| 0 to 7 | see [example A1](exampleA1.md) to [example A3](exampleA3.md) | `QA`, `DE*GEF`, its templates, the pools `P1` and `P2` and the groups |
| | `OCC`, its three holders | **checkpoint 0**, of revision 7: the 8 commits since the genesis |
| 8 | `DE*GEF`, its `infrastructure` key | `Add` of the pool `DE*GEF*P3`, Jena Lobeda: the DC station `DE*GEF*S5`, the AC station `DE*GEF*S6`, two EVSEs each |
| 9 | `DE*GEF` | `Add` of the pool `DE*GEF*P4`, Jena Winzerla: the DC station `DE*GEF*S7`, the AC station `DE*GEF*S8` |

A checkpoint is not a version of its own and changes nothing (SPECIFICATION.md, section 10): it names a commit, its
revision and the ETags of its version, and the root of a Merkle tree over the **commits of the checkpoint**, the commit
and every commit it follows that the checkpoint before does not include. Checkpoints form one line, each naming the one
before. They serve three purposes:

- **Starting points:** a server or an app joins from the version of the latest checkpoint and the commits since,
  [step 3](#3-joining-from-the-checkpoint).
- **Evidence:** a proof shows that a commit is one of the commits of a checkpoint, months later, without asking the
  server that made it, [step 4](#4-the-proof-of-a-commit).
- **Compaction:** a history drops older commits only before the commit of a checkpoint.

The code is [exampleA4.cs](csharp/exampleA4.cs), called by [Program.cs](csharp/Program.cs) with what examples A1 and A2
left; its helpers are also those of example A5:

```csharp
ExampleA4.Run(a1, a2);

ExampleA4.Validate();
```

The output is what one run printed, `…` for elided digests and timestamps.


## 1. The first checkpoint

The roaming network operator signs its checkpoints as it signs its commits, with the signing keys of `AlphaOrg`,
`BetaOrg` and `GammaOrg`. A server of the RNO makes one when there are new commits and the last one is older than an
interval, e.g. one hour (section 10.3); here it is made by hand, of the latest commit, revision 7.

```csharp
A1.History.CheckpointSigners = [.. A1.Signers];

var checkpoint0 = MakeCheckpoint(A1);
```

`MakeCheckpoint` calls `TryMakeCheckpoint` and publishes the checkpoint and its **snapshot**, the version of its commit:

```csharp
if (!A1.History.TryMakeCheckpoint(out var checkpoint, out var error))
    Helpers.Fail(error);

if (!A1.History.TryGetCheckpointSnapshotJSON(checkpoint.Id, out var snapshot, out error))
    Helpers.Fail(error);

File.WriteAllText($"published/QA-checkpoint-{checkpoint.Sequence}.json",           checkpoint.ToJSON().ToString());
File.WriteAllText($"published/QA-checkpoint-{checkpoint.Sequence}-snapshot.json",  snapshot.          ToString());
```

The server keeps a proof for every commit of a checkpoint. Here it is the proof of the commit of revision 4, the one
adding the pool "Jena Paradies":

```csharp
var revision4   = CommitOf(A1, 4);

if (!A1.History.TryGetCommitProof(revision4.Id, out var proof, out var error))
    Helpers.Fail(error);

File.WriteAllText("published/QA-proof-revision-4.json", proof.ToJSON().ToString());
```

```
Checkpoint 0 of revision 7: 8 commit(s), 3 signatures, c11fec63…
The commit of revision 4 is the commit 4 of the 8 of checkpoint 0
```

The checkpoint, as `published/QA-checkpoint-0.json` has it; its `@id` is the SHA-256 of its canonical JSON without
`@id` and `signatures`. The first one of an epoch has the `sequence` 0 and names no checkpoint before:

```json
{
  "@id": "c11fec63…",
  "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/checkpoint/v1",
  "roamingNetworkId": "QA",
  "epoch": 1,
  "sequence": 0,
  "commitId": "…",
  "revision": 6,
  "eTags": [ [ "json", "sha256", "hex", "…" ], [ "cbor", "sha256", "hex", "…" ], [ "effectiveTree", "sha256", "hex", "…" ] ],
  "commits": { "@merkleRoot": "…", "@size": 7 },
  "createdAt": "…",
  "signatures": [
    { "role": "roamingNetworkOperator", "signer": "OCC", "signedAt": "…", "algorithm": "ES256", "keyId": "…", "value": "…" },
    …
  ]
}
```


## 2. Two pools more, after the pattern of A3

As in A3, pool n has the DC station 2n-1 and the AC station 2n, two EVSEs each, every EVSE with its energy meter.
`ExampleA3.Pool(…)` is the helper of A3, here with the power of the AC EVSEs as a parameter, and `Commit(…)` the one of
A3 as a helper of `ExampleA4`. The AC stations take the AC template, 44 kW, and their
EVSEs 22 kW each: wrong, they have two EVSEs of 11 kW, 22 kW together.

```csharp
Commit(A1, A2, "Jena Lobeda, a DC and an AC station",   RoamingNetworkChange.Add(ExampleA3.Pool(3, "Jena Lobeda",   50.8858, 11.6157, ACkW: 22)));
Commit(A1, A2, "Jena Winzerla, a DC and an AC station", RoamingNetworkChange.Add(ExampleA3.Pool(4, "Jena Winzerla", 50.9105, 11.5555, ACkW: 22)));
```

The data must be wrong in a valid way: two EVSEs of 11 kW with the 44 kW of the AC template would be refused, as a
version is invalid where a charging station has more power than its effective EVSEs together (section 9.2). A commit
after the latest checkpoint is part of none yet, so there is no proof of it:

```
Committed, revision 8: Jena Lobeda, a DC and an AC station
Committed, revision 9: Jena Winzerla, a DC and an AC station
The proof of revision 9: No checkpoint here includes the commit '7947f051…' yet!
```

What everybody may read then: `QA.json` and `QA-commit.json` at revision 9, and the commits since the checkpoint,
`QA-commits-since-checkpoint-0.json`, as `GET ~/RNs/QA/history/commits?since=…` gives them.


## 3. Joining from the checkpoint

`ExampleA4.Validate()` joins `QA` as a server or an app does that has no history of it, from what is published only.

a) `QA` at its head, as everybody checks it ([example A1](exampleA1.md#8-checking-qa-as-a-charging-station-operator-or-an-app)):
the trust anchor, the roaming network, its key statement and certificates, and the commit of revision 9.

b) The checkpoint: `TryParse` calculates its `@id` from its content and refuses it when it differs. It must be of `QA`
and signed as the roaming network operator by signing keys of as many holders as the quorum requires, each certified
by a trusted identity key of its holder, as A1 checks the signatures of a commit (`SigningKeyOf`):

```csharp
foreach (var signature in checkpoint.Signatures.Where(signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator))
{

    var key = SigningKeyOf(Anchor, Network, signature);

    if (key is null)
        Helpers.Fail($"the key '{signature.KeyId}' is no signing key a trusted identity key of its holder certifies");

    if (!checkpoint.VerifySignature(signature, key.Key, key.Id, out error))
        Helpers.Fail(error);

    signedBy.Add(HolderOf(Anchor, Network, signature)!);

}

if (signedBy.Count < Math.Max(Anchor.MinConsensus, Network.Operator!.KeyStatement!.MinConsensus))
    Helpers.Fail($"the checkpoint is signed by {signedBy.Count} holder(s) of the roaming network operator only");
```

c) The version of its commit, from the snapshot of the checkpoint, `{ commit, roamingNetwork, checkpoint }`: it must
lead to the ETags of the checkpoint, whose signatures are checked again, by the verifier given. The new history starts
its line of checkpoints with it (section 10.5):

```csharp
if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(File.ReadAllText("published/QA-checkpoint-0-snapshot.json"),
                                                     out var joined,
                                                     out var error,
                                                     VerifyCheckpointSignature(anchor, network),
                                                     [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ]))
    Helpers.Fail(error);
```

d) The commits since, each checked as every commit is: its change set signed by its owners and leading to its ETags,
the commit signed as the roaming network operator:

```csharp
foreach (var commit in CommitsSince(0))
    Append(joined, commit, anchor, network);
```

e) The head reached is the published one:

```
Checkpoint 0 of epoch 1: revision 7, 8 commit(s) since the genesis, signed by AlphaOrg, BetaOrg, GammaOrg
Joined at revision 7, the checkpoint's: the same; checkpoints: 1
Appended revision 8: Jena Lobeda, a DC and an AC station
Appended revision 9: Jena Winzerla, a DC and an AC station
The head of the joined history and the published commit: the same
```


## 4. The proof of a commit

The proof of the commit of revision 4 is its leaf, its commit id, revision and ETags, with the audit path to the root
of the commits the checkpoint signs (section 10.4, RFC 9162). The joined history does not hold this commit; whoever
kept it, or a proof of a charging station of its version (section 8.4), shows with this proof that it is part of the
history the checkpoint states.

```csharp
if (!RoamingNetworkCheckpointProof.TryParse(Helpers.ReadJSON("published/QA-proof-revision-4.json"), out var proof, out error))
    Helpers.Fail(error);

Console.WriteLine($"The proof of the commit of revision {proof.Revision} in checkpoint {checkpoint0.Sequence}: {(proof.Verify(checkpoint0, out error) ? "valid" : error)}");
```

```
The proof of the commit of revision 4 in checkpoint 0: valid
```


## 5. What is refused

a) A checkpoint of another revision: its `@id` is no longer the digest of its content. b) A snapshot whose version is
changed, here the name of the pool `P1`: it does not lead to the ETags of the checkpoint. c) A proof of another
position: it leads to another root.

```
A checkpoint of another revision: The checkpoint identification 'c11fec63…' is not the one of its content, 'e154a7a8…'!
A version of the checkpoint changed: Invalid snapshot of the checkpoint: Invalid roaming network of the snapshot: The ETags of the roaming network 'QA' do not match its content: The JSON ETag (SHA256) differs!
A proof of another position: The proof does not lead to the commits of the checkpoint!
```


## 6. The new pools, as they are at runtime

As recorded: the AC stations with the 44 kW of their template, their EVSEs with 22 kW each.

```
DE*GEF*P3  Jena Lobeda
  DE*GEF*S5  300 kW: DE*GEF*E5*1 DC 150 kW, DE*GEF*E5*2 DC 150 kW
  DE*GEF*S6  44 kW: DE*GEF*E6*1 AC_ThreePhases 22 kW, DE*GEF*E6*2 AC_ThreePhases 22 kW
DE*GEF*P4  Jena Winzerla
  DE*GEF*S7  300 kW: DE*GEF*E7*1 DC 150 kW, DE*GEF*E7*2 DC 150 kW
  DE*GEF*S8  44 kW: DE*GEF*E8*1 AC_ThreePhases 22 kW, DE*GEF*E8*2 AC_ThreePhases 22 kW
```


## With the hub

POI_API serves what this example publishes as files: `GET ~/RNs/QA/history/checkpoints`, a checkpoint as
`GET ~/RNs/QA/history/checkpoints/{checkpointId}`, its snapshot as `…/checkpoints/{checkpointId}/snapshot` and the
proof of a commit as `…/checkpoints/{checkpointId}/commits/{commitId}/proof` or `GET ~/RNs/QA/history/commits/{commitId}/proof`.
The RoamingHubCLI makes no checkpoints of its own yet: it has no checkpoint signers and no interval.


## Next

- Witnesses: independent third parties sign a checkpoint after they checked it against their own copy of the history,
  `TryWitness`, and the RNO takes their signatures, `TryAddCheckpointSignatures`; a verifier then requires the
  `witness` role as well (section 10.2): [example A6](exampleA6.md).
- The next checkpoints, the correction of the AC stations between them, and the data at every checkpoint:
  [example A5](exampleA5.md).
