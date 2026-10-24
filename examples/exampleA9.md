# Example A9: witnesses check the price statements

The two witnesses of [example A6](exampleA6.md), `calibration.example` and `association.example`, go on with their
copies of the history of `QA`: they take the commits of [example A8](exampleA8.md), the checkpoints 3 and 4, and sign
them. Checkpoint 4 names price statements: a witness signs it only after it checked them. So the proof of a price
statement in checkpoint 4 becomes witnessed evidence: the statement was made between the checkpoints 3 and 4, and two
independent parties checked it.

| Revision | Signed by | |
|---|---|---|
| 0 to 13 | see [example A1](exampleA1.md) to [example A8](exampleA8.md) | no new commit |
| checkpoint 3 | `OCC`, and now both witnesses | of revision 12, naming no price statements |
| checkpoint 4 | `OCC`, and now both witnesses, after they checked its 3 price statements | of revision 13 |

What a witness checks of the price statements a checkpoint names (SPECIFICATION.md, sections 10.2 and 12.3): their tree
is the one of the checkpoint, and every one was signed before it by a signing key of its charging station operator in
the version of the commit of the checkpoint or of the checkpoint before, valid when it was signed. A server that does not
get the statements takes the checkpoint all the same; a witness does not sign it then.

The code is [exampleA9.cs](csharp/exampleA9.cs), with the helpers of [exampleA4.cs](csharp/exampleA4.cs) and
[exampleA6.cs](csharp/exampleA6.cs), called by [Program.cs](csharp/Program.cs) with what examples A1 and A6 left, the
history of `QA` and the keys and histories of the witnesses:

```csharp
ExampleA9.Run(a1, a6);

ExampleA9.Validate();
```

`ExampleA6.Run` returns them as its `Result`: a witness keeps its history, as the roaming network operator does. The
output is what one run printed, `…` for elided digests.


## 1. What the roaming network operator publishes for them

The witnesses hold the history up to revision 10, the commit of checkpoint 2. The roaming network operator publishes the
commits since, `published/QA-commits-since-checkpoint-2.json`, as `GET ~/RNs/QA/history/commits?since={commit}` gives
them. Its history was compacted in [example A7](exampleA7.md), to revision 9: it gives commits since every commit it
holds. The checkpoints 3 and 4 and the price statements of checkpoint 4 are those example A8 published.

```csharp
ExampleA4.PublishCommitsSince(A1, A1.History.Checkpoints[2]);
```


## 2. Each witness checks and signs

a) The commits of example A8, each checked as every commit is, `AppendCommits`, and checkpoint 3, which names no price
statements: taken after it is checked against the history of the witness, `TryTakeCheckpoint`, and signed.

```csharp
ExampleA6.AppendCommits(history, "published/QA-commits-since-checkpoint-2.json");

if (!ExampleA6.TryTakeCheckpoint(history, 3, out var checkpoint3, out var error))
    Helpers.Fail(error);

if (!history.TryWitness(checkpoint3.Id, signer, out _, out var signature3, out error))
    Helpers.Fail(error);
```

b) Checkpoint 4, first without its price statements: the history of the witness takes it, but `TryWitness` refuses to
sign it. c) Its price statements given afterwards, `TryAddCheckpointPriceStatements`: one of them missing, or one changed
by a forger, a statement of 0.09 €/kWh signed by another key naming the key of `DE*GEF`, and the tree of those given is
not the one the checkpoint signs. d) Its price statements as published: checked, and then the checkpoint is signed.

```csharp
if (!history.TryAddCheckpointPriceStatements(checkpoint4.Id, statements, out error))
    Helpers.Fail(error);

if (!history.TryWitness(checkpoint4.Id, signer, out _, out var signature4, out error))
    Helpers.Fail(error);
```

```
calibration.example, checkpoint 4 without its price statements: The price statements of the checkpoint '29f98e22…' are not checked here; a witness signs only checkpoints it checked!
calibration.example, a price statement missing: The price statements are not those the checkpoint '29f98e22…' names!
calibration.example, a price statement changed: The price statements are not those the checkpoint '29f98e22…' names!
calibration.example: revision 13, signs checkpoint 3, and checkpoint 4 with its 3 price statements checked

association.example, checkpoint 4 without its price statements: The price statements of the checkpoint '29f98e22…' are not checked here; a witness signs only checkpoints it checked!
association.example, a price statement missing: The price statements are not those the checkpoint '29f98e22…' names!
association.example, a price statement changed: The price statements are not those the checkpoint '29f98e22…' names!
association.example: revision 13, signs checkpoint 3, and checkpoint 4 with its 3 price statements checked
```

A witness could also get the statements along with the checkpoint, `TryAddCheckpoint(…, PriceStatements)`, as
`GET ~/RNs/QA/history/checkpoints/{checkpointId}/priceStatements` or `priceStatements` of `pushCheckpoints` gives them.


## 3. The roaming network operator takes their signatures

As in [example A6](exampleA6.md#4-the-roaming-network-operator-takes-their-signatures): `TryAddCheckpointSignatures`
with the witnesses it trusts, and the checkpoints and their snapshots published again, their `@id`s unchanged.

```
Checkpoint 3, a068d94d…: 2 signatures more, 5 in all
Checkpoint 4, 29f98e22…: 2 signatures more, 5 in all
```


## 4. Price statements witnessed

`ExampleA9.Validate()` checks as a verifier that requires the roaming network operator and both witnesses, from what is
published and the keys of the witnesses only. a) `QA` at its head. b) The checkpoints 3 and 4: signed by the quorum of
`OCC` and by both witnesses. c) The proof of the happy hour at `DE*GEF*E1*1` of example A8, now in a witnessed
checkpoint. d) Joining from checkpoint 4 with its price statements, requiring both roles.

```
Checkpoint 3 of epoch 1: revision 12, 2 commit(s) since checkpoint c0841d4d…, signed by AlphaOrg, BetaOrg, GammaOrg
  witnessed by calibration.example, association.example
Checkpoint 4 of epoch 1: revision 13, 1 commit(s) since checkpoint a068d94d…, signed by AlphaOrg, BetaOrg, GammaOrg
  witnessed by calibration.example, association.example, its 3 price statements checked

The proof of DE*GEF*E1*1, sequence 0, 0.59 €/kWh, in checkpoint 4, witnessed by calibration.example, association.example: valid
Joined from checkpoint 4, signed by the roaming network operator and its witnesses: revision 13, 3 price statements
```

The statement was made after checkpoint 3, which it names, and before checkpoint 4, which names it, and both witnesses
checked it with checkpoint 4: kept by a driver or an e-mobility provider, it is evidence of the price at
`DE*GEF*E1*1` without asking `DE*GEF` or the server of `QA`.


## Next

- What a driver's app keeps of a charging session, to prove later that exactly this tariff applied at exactly this
  charging station when it charged: the proof of the station, the price statement valid then and the witnessed
  checkpoints with the proofs of both: [example A10](exampleA10.md).
