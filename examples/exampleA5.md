# Example A5: a correction between two checkpoints, and the data at every checkpoint

The roaming network operator `OCC` makes the next checkpoint of `QA`, of revision 8, with the AC stations of the pools
`P3` and `P4` as [example A4](exampleA4.md) recorded them: 44 kW, EVSEs of 22 kW. Then `DE*GEF` corrects them, 22 kW
each with two EVSEs of 11 kW, and `OCC` makes the checkpoint after, of revision 9. The correction changes the head, not
the past: the version of revision 8 stays what checkpoint 1 states, and a server or an app finds the AC stations at
every checkpoint as they were then.

| Revision | Signed by | Commit |
|---|---|---|
| 0 to 6 | see [example A1](exampleA1.md) to [example A3](exampleA3.md) | |
| | `OCC`, its three holders | checkpoint 0, of revision 6, see [example A4](exampleA4.md) |
| 7, 8 | `DE*GEF`, its `infrastructure` key | the pools `P3` and `P4`, see [example A4](exampleA4.md) |
| | `OCC` | **checkpoint 1**, of revision 8: the 2 commits since checkpoint 0 |
| 9 | `DE*GEF` | **the correction:** `UpdateProperty` of `maxPower` of the AC stations `DE*GEF*S6` and `DE*GEF*S8`, 22 kW, and of their four EVSEs, 11 kW |
| | `OCC` | **checkpoint 2**, of revision 9: the commit since checkpoint 1 |

The code is [exampleA5.cs](csharp/exampleA5.cs), with the helpers of [exampleA4.cs](csharp/exampleA4.cs), called by
[Program.cs](csharp/Program.cs) after example A4:

```csharp
ExampleA5.Run(a1, a2);

ExampleA5.Validate();
```

The output is what one run printed, `…` for elided digests.


## 1. The second checkpoint: before the correction

It names the first one as the one before, `previousId`, its `sequence` is 1, and its commits are those since: the
revisions 7 and 8. The proof of revision 7 is published as well.

```csharp
var checkpoint1  = ExampleA4.MakeCheckpoint(A1);

if (!A1.History.TryGetCommitProof(ExampleA4.CommitOf(A1, 7).Id, out var proof7, out var error))
    Helpers.Fail(error);

File.WriteAllText("published/QA-proof-revision-7.json", proof7.ToJSON().ToString());
```

```
Checkpoint 1 of revision 8: 2 commit(s), 3 signatures, b322217a…
```


## 2. The correction

The AC stations `S6` and `S8` get 22 kW of their own, in place of the 44 kW of their template: an effective member takes
its own value of a property before the one of its templates (section 9.2). Their EVSEs get 11 kW each. All in one change
set, so that no version has a station with more power than its EVSEs together; `CreateChangeSet` fills in the old values
it finds.

```csharp
var stations     = gef.ChargingPools.SelectMany(pool => pool.ChargingStations).
                                     Where     (station => station.Id.ToString() is "DE*GEF*S6" or "DE*GEF*S8").
                                     ToArray();

ExampleA4.Commit(A1, A2, "The AC stations of Jena Lobeda and Jena Winzerla: 22 kW, two EVSEs of 11 kW",
                 [
                     .. stations.Select      (station => RoamingNetworkChange.UpdateProperty(station, "maxPower", null,  22000)),
                     .. stations.SelectMany  (station => station.EVSEs).
                                 Select      (evse    => RoamingNetworkChange.UpdateProperty(evse,    "maxPower", 22000, 11000))
                 ]);
```

```
Committed, revision 9: The AC stations of Jena Lobeda and Jena Winzerla: 22 kW, two EVSEs of 11 kW
```

What everybody may read then: `QA.json` and `QA-commit.json` at revision 9, and the commits since checkpoint 1,
`QA-commits-since-checkpoint-1.json`.


## 3. The third checkpoint: after the correction

Its only commit is the correction; the proof of it is published as well.

```
Checkpoint 2 of revision 9: 1 commit(s), 3 signatures, 88e69122…
```


## 4. The line of checkpoints

`ExampleA5.Validate()` checks from what is published only. a) `QA` at its head, as everybody checks it. b) The three
checkpoints, each as in [example A4](exampleA4.md#3-joining-from-the-checkpoint): its `@id` the digest of its content,
signed by the quorum of `OCC`. Each one names the one before, its `sequence` is one more, and its epoch the same:

```csharp
for (var i = 1; i < checkpoints.Length; i++)
    if (checkpoints[i].PreviousId != checkpoints[i - 1].Id || checkpoints[i].Sequence != checkpoints[i - 1].Sequence + 1 || checkpoints[i].Epoch != checkpoints[i - 1].Epoch)
        Helpers.Fail($"the checkpoint {checkpoints[i].Sequence} does not follow the one before");
```

```
Checkpoint 0 of epoch 1: revision 6, 7 commit(s) since the genesis, signed by AlphaOrg, BetaOrg, GammaOrg
Checkpoint 1 of epoch 1: revision 8, 2 commit(s) since checkpoint c11fec63…, signed by AlphaOrg, BetaOrg, GammaOrg
Checkpoint 2 of epoch 1: revision 9, 1 commit(s) since checkpoint b322217a…, signed by AlphaOrg, BetaOrg, GammaOrg
One line: 0 (revision 6) <- 1 (revision 8) <- 2 (revision 9)
```


## 5. From the first checkpoint, commit by commit

A history joined from checkpoint 0 takes each commit as it comes, and each checkpoint where its commit is reached. It
refuses a checkpoint unless its commit is that one, with its revision and the ETags of the version reached, its
commits are the ones since the checkpoint before, with the Merkle root it signs, and it names the checkpoint before
(section 10.1). So the version at each checkpoint is the one it states, and between them every revision is shown:

```csharp
foreach (var commit in ExampleA4.CommitsSince(0).Concat(ExampleA4.CommitsSince(1)))
{

    ExampleA4.Append(stepwise, commit, anchor, network);

    foreach (var checkpoint in checkpoints.Where(checkpoint => checkpoint.Revision == commit.Revision))
    {

        if (!stepwise.TryAddCheckpoint(checkpoint, out error, verify, roles))
            Helpers.Fail(error);

        Console.WriteLine($"Revision {commit.Revision}, checkpoint {checkpoint.Sequence}, its ETags and those of the version reached: {(checkpoint.ETags.SequenceEqual(stepwise.Head.Commit.ETags) ? "the same" : "others")}");

    }

    ExampleA4.ShowPools(stepwise.Head.RoamingNetwork, 3, 4);

}
```

```
Revision 6, checkpoint 0:
no pool 3 or 4 yet
Appended revision 7: Jena Lobeda, a DC and an AC station
DE*GEF*P3  Jena Lobeda
  DE*GEF*S5  300 kW: DE*GEF*E5*1 DC 150 kW, DE*GEF*E5*2 DC 150 kW
  DE*GEF*S6  44 kW: DE*GEF*E6*1 AC_ThreePhases 22 kW, DE*GEF*E6*2 AC_ThreePhases 22 kW
Appended revision 8: Jena Winzerla, a DC and an AC station
Revision 8, checkpoint 1, its ETags and those of the version reached: the same
DE*GEF*P3  Jena Lobeda
  DE*GEF*S5  300 kW: DE*GEF*E5*1 DC 150 kW, DE*GEF*E5*2 DC 150 kW
  DE*GEF*S6  44 kW: DE*GEF*E6*1 AC_ThreePhases 22 kW, DE*GEF*E6*2 AC_ThreePhases 22 kW
DE*GEF*P4  Jena Winzerla
  DE*GEF*S7  300 kW: DE*GEF*E7*1 DC 150 kW, DE*GEF*E7*2 DC 150 kW
  DE*GEF*S8  44 kW: DE*GEF*E8*1 AC_ThreePhases 22 kW, DE*GEF*E8*2 AC_ThreePhases 22 kW
Appended revision 9: The AC stations of Jena Lobeda and Jena Winzerla: 22 kW, two EVSEs of 11 kW
Revision 9, checkpoint 2, its ETags and those of the version reached: the same
DE*GEF*P3  Jena Lobeda
  DE*GEF*S5  300 kW: DE*GEF*E5*1 DC 150 kW, DE*GEF*E5*2 DC 150 kW
  DE*GEF*S6  22 kW: DE*GEF*E6*1 AC_ThreePhases 11 kW, DE*GEF*E6*2 AC_ThreePhases 11 kW
DE*GEF*P4  Jena Winzerla
  DE*GEF*S7  300 kW: DE*GEF*E7*1 DC 150 kW, DE*GEF*E7*2 DC 150 kW
  DE*GEF*S8  22 kW: DE*GEF*E8*1 AC_ThreePhases 11 kW, DE*GEF*E8*2 AC_ThreePhases 11 kW
Checkpoints taken: 3
```


## 6. From each checkpoint on its own

A server joining later starts from the latest checkpoint; one asking about a past time, e.g. disputing an invoice of a
session at `S6` before the correction, starts from the checkpoint of that time. The version of each snapshot is the one
reached commit by commit, with the AC stations as they were then:

```csharp
foreach (var checkpoint in checkpoints.Skip(1))
{

    if (!RoamingNetworkHistory.TryFromCheckpointSnapshot(File.ReadAllText($"published/QA-checkpoint-{checkpoint.Sequence}-snapshot.json"), out var joined, out error, verify, roles))
        Helpers.Fail(error);

    if (!stepwise.TryGetCheckpoint(checkpoint.Id, out var reached) || !reached.ETags.SequenceEqual(joined.Head.Commit.ETags))
        Helpers.Fail($"the snapshot of checkpoint {checkpoint.Sequence} is another version than the one reached");

    …

}
```

```
Joined from checkpoint 1, revision 8, and the version reached commit by commit: the same
DE*GEF*P3  Jena Lobeda
  DE*GEF*S5  300 kW: DE*GEF*E5*1 DC 150 kW, DE*GEF*E5*2 DC 150 kW
  DE*GEF*S6  44 kW: DE*GEF*E6*1 AC_ThreePhases 22 kW, DE*GEF*E6*2 AC_ThreePhases 22 kW
DE*GEF*P4  Jena Winzerla
  DE*GEF*S7  300 kW: DE*GEF*E7*1 DC 150 kW, DE*GEF*E7*2 DC 150 kW
  DE*GEF*S8  44 kW: DE*GEF*E8*1 AC_ThreePhases 22 kW, DE*GEF*E8*2 AC_ThreePhases 22 kW
Joined from checkpoint 2, revision 9, and the version reached commit by commit: the same
DE*GEF*P3  Jena Lobeda
  DE*GEF*S5  300 kW: DE*GEF*E5*1 DC 150 kW, DE*GEF*E5*2 DC 150 kW
  DE*GEF*S6  22 kW: DE*GEF*E6*1 AC_ThreePhases 11 kW, DE*GEF*E6*2 AC_ThreePhases 11 kW
DE*GEF*P4  Jena Winzerla
  DE*GEF*S7  300 kW: DE*GEF*E7*1 DC 150 kW, DE*GEF*E7*2 DC 150 kW
  DE*GEF*S8  22 kW: DE*GEF*E8*1 AC_ThreePhases 11 kW, DE*GEF*E8*2 AC_ThreePhases 11 kW
```


## 7. The proofs, and what is refused

a) The commits of revisions 3, 7 and 9, each in the checkpoint whose commits include it. b) The proof of the correction
against checkpoint 1: the correction is no commit of it. c) Checkpoint 2 without checkpoint 1: a history joined from
checkpoint 0 and at revision 9 refuses it, as it does not name the latest checkpoint the history holds.

```
The proof of revision 3 in checkpoint 0: valid
The proof of revision 7 in checkpoint 1: valid
The proof of revision 9 in checkpoint 2: valid
The proof of revision 9 in checkpoint 1: The proof is for the checkpoint '88e69122…', not for 'b322217a…'!
Checkpoint 2 without checkpoint 1: The checkpoint does not follow the latest checkpoint 'c11fec63…' of the sequence 0 here!
```


## Next

- Witnesses sign the three checkpoints: [example A6](exampleA6.md).
- Compaction: with checkpoints, a history drops older commits only before the commit of one of them and archives them,
  `RoamingNetworkHistoryRetention` and an archive, so that the proofs above stay possible (section 10.6).
- Proofs of a single charging station with its EVSEs and tariffs against the commit of a checkpoint (sections 8.4, 10.4).
