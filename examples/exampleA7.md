# Example A7: compaction with an archive, after the checkpoints

The roaming network operator `OCC` compacts the history of `QA` twice, after its checkpoints of
[example A4](exampleA4.md) and [example A5](exampleA5.md): first to the commit of checkpoint 0, revision 6, then to the
one of checkpoint 1, revision 8. Its server then holds only the commits since revision 8, and starts again from them
after a restart. What it drops it keeps in an **archive**, one segment per compaction, so that the checkpoints still
prove what someone can show: the commits stay available, and so do their proofs.

| Revision | Kept in | After the compaction |
|---|---|---|
| 0 to 6 | segment 1 of the archive: the genesis snapshot and the commits 1 to 6 | dropped from the history, checkpoint 0 kept |
| 6 to 8 | segment 2: the snapshot of revision 6 and the commits 7 and 8 | dropped, but for revision 8, the new root; checkpoint 1 kept |
| 8, 9 | the history and its store | the root and the head; checkpoint 2 kept |

The rules (SPECIFICATION.md, section 10.6): a history that has checkpoints drops older commits only before the commit of
one of them, so that every root is the commit of a checkpoint, and keeps all checkpoints. A server should archive the
commits it drops, not delete them: a checkpoint proves only what someone can still show. It archives before it drops:
an archive failing fails the compaction. A segment alone gives every version from its root to its end again, and its
commits, with its root, prove the commits of every checkpoint between them.

The code is [exampleA7.cs](csharp/exampleA7.cs), with the helpers of [exampleA4.cs](csharp/exampleA4.cs), called by
[Program.cs](csharp/Program.cs) after example A6:

```csharp
ExampleA7.Run(a1);

ExampleA7.Validate();
```

The output is what one run printed, `…` for elided digests.


## 1. The archive and what to keep

The archive keeps its segments in `poi/QA-archive`: for each the snapshot of the root before, `segment-000001.base.json`,
and every commit dropped after it, ending with the new root, `segment-000001.commits.jsonl`, and the list of the
segments, `segments.jsonl`. Without an archive, the commits dropped would be gone.

```csharp
A1.History.Archive = new RoamingNetworkHistoryFileArchive($"poi/{A1.RoamingNetwork.Id}-archive");
```

```
Before: root revision 0, head revision 9, 3 checkpoints
```


## 2. Compacting after the checkpoints

`RoamingNetworkHistoryRetention.RetainCommits` says how many revisions behind the head are kept: the new root is the
latest commit of a checkpoint at least that far behind it, that every commit kept follows. A server of the RNO compacts
itself once its log has `CompactAfterLogEntries` entries; here it is done by hand, keeping 3 revisions, then 1.

```csharp
foreach (var retainCommits in new UInt64[] { 3, 1 })
{

    A1.History.Retention = A1.History.Retention with { RetainCommits = retainCommits };

    if (!A1.History.Compact(out var compactError))
        Helpers.Fail(compactError);

    …

}
```

Keeping 3 revisions behind the head, revision 9, allows a root up to revision 6: the commit of checkpoint 0. Keeping 1
allows revision 8: the commit of checkpoint 1. Revision 7, of no checkpoint, is never a root.

```
Compacted, keeping 3 revision(s) behind the head: root revision 6; segment 1 archives the revisions 0 to 6, 6 commit(s) after its root
Compacted, keeping 1 revision(s) behind the head: root revision 8; segment 2 archives the revisions 6 to 8, 2 commit(s) after its root
After: root revision 8, head revision 9, 3 checkpoints
```

The store is compacted with the history: its base becomes the snapshot of the new root, its log the commits since,
the checkpoints all.


## 3. What the history gives after the compaction

a) Commits since a dropped one: none; a server knowing only older commits starts again from a snapshot, e.g. the one
of the latest checkpoint. b) A dropped commit from the archive, `TryGetArchivedCommit`, and its proof,
`TryGetCommitProof`, which looks into the archive for the commits of a checkpoint the history no longer holds: the proof
of revision 3 is the one of example A4. c) After a restart, here of copies of the store and the archive beside those
still open, each of which one history only may write: the store gives the root and the commits since, the archive the
rest.

```csharp
if (!RoamingNetworkHistory.TryLoad(restartedStore, out var restarted, out error, Archive: restartedArchive))
    Helpers.Fail(error);
```

```
Commits since the genesis: The commit '85d6adbe…' is unknown here; start from a new snapshot!
The commit of revision 3 from the archive: Jena Paradies, a DC and an AC station; its proof, as before the compaction: the same
Restarted: root revision 8, head revision 9, 3 checkpoints, the proof of revision 3 the same
```


## 4. What everybody may read of the archive

The list of the segments, `published/QA-archive.json`, as `GET ~/RNs/QA/history/archive` gives it, and each segment,
`published/QA-archive-{number}.json`, as `GET ~/RNs/QA/history/archive/{number}`: `{ info, base, commits }`.

```json
[
  { "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/archiveSegmentInfo/v1",
    "number": 1, "rootId": "85d6adbe…", "endId": "ab3566ae…", "fromRevision": 0, "toRevision": 6, "size": 6,
    "archivedAt": "…", "commitIds": [ "a68f3046…", "7eb8009b…", "cb9a30dc…", "335647c8…", "de1c9ad9…", "ab3566ae…" ] },
  { "@context": "https://open.charging.cloud/contexts/wwcp/roamingNetwork/archiveSegmentInfo/v1",
    "number": 2, "rootId": "ab3566ae…", "endId": "1d0ec2b0…", "fromRevision": 6, "toRevision": 8, "size": 2,
    "archivedAt": "…", "commitIds": [ "a236fa8e…", "1d0ec2b0…" ] }
]
```

A segment is never changed once written. Personal data that must be erased (GDPR) is removed from it, its commits and
versions keeping their identifications and ETags; a proof of what was erased is then no longer possible.


## 5. The segments

`ExampleA7.Validate()` checks the archive from what is published only. a) `QA` at its head, as everybody checks it, and
its three checkpoints, signed by the quorum of `OCC`. b) The segments follow each other: the first one begins at the
genesis of `published/QA-genesis.json`, each other one at the end of the one before, and each ends with the commit of a
checkpoint:

```
Segments: 1: revisions 0 to 6, 2: revisions 6 to 8, one after the other
```


## 6. Each segment on its own

Its commits are the ones its information names, in its order, and its base is of its root (`Check`). Its base, the
snapshot of its root, must lead to the ETags of its commit; its commits are taken each checked, as every commit is. The
version at its end is the one the checkpoint of its end states, and its commits, with its root for the first
checkpoint, are the commits of that checkpoint: their Merkle root is the one it signs.

```csharp
var ofCheckpoint = RoamingNetworkCheckpoint.InOrder(checkpoint.Sequence == 0 ? [ history.Root, .. segment.Commits ] : segment.Commits);

Console.WriteLine($"  its end and checkpoint {checkpoint.Sequence}: ETags {(checkpoint.ETags.SequenceEqual(history.Head.Commit.ETags) ? "the same" : "others")}, the root of its {ofCheckpoint.Count} commits {(RoamingNetworkCheckpoint.CommitsRootOf(ofCheckpoint, checkpoint.IdentificationHash) == checkpoint.CommitsRoot ? "the same" : "another")}");
```

```
Segment 1, from revision 0:
Appended revision 1: DE*GEF, GraphDefined Charging Solutions
Appended revision 2: The templates of the DC and the AC stations and EVSEs
Appended revision 3: Jena Paradies, a DC and an AC station
Appended revision 4: Jena Nord, a DC and an AC station
Appended revision 5: The DC and the AC charging stations
Appended revision 6: The DC and the AC EVSEs
  its end and checkpoint 0: ETags the same, the root of its 7 commits the same
Segment 2, from revision 6:
Appended revision 7: Jena Lobeda, a DC and an AC station
Appended revision 8: Jena Winzerla, a DC and an AC station
  its end and checkpoint 1: ETags the same, the root of its 2 commits the same
DE*GEF*P3  Jena Lobeda
  DE*GEF*S5  300 kW: DE*GEF*E5*1 DC 150 kW, DE*GEF*E5*2 DC 150 kW
  DE*GEF*S6  44 kW: DE*GEF*E6*1 AC_ThreePhases 22 kW, DE*GEF*E6*2 AC_ThreePhases 22 kW
DE*GEF*P4  Jena Winzerla
  DE*GEF*S7  300 kW: DE*GEF*E7*1 DC 150 kW, DE*GEF*E7*2 DC 150 kW
  DE*GEF*S8  44 kW: DE*GEF*E8*1 AC_ThreePhases 22 kW, DE*GEF*E8*2 AC_ThreePhases 22 kW
```

So the AC stations as example A4 recorded them, before the correction of example A5, are still shown from the archive,
after the history dropped them.


## 7. Proofs after the compaction, and what is refused

a) The proof of revision 3, given from the archive, is valid in checkpoint 0. b) A segment with a commit changed: its
`@id` is no longer the digest of its content. c) A segment with another base, here the name of the pool `P1` changed:
it does not lead to the ETags of its root.

```
The proof of revision 3, from the archive, in checkpoint 0: valid
A segment with a commit changed: The commit identification '…' is not the one of its content, '…'!
A segment with another base: Invalid roaming network of the snapshot: The ETags of the roaming network 'QA' do not match its content: The JSON ETag (SHA256) differs!
```


## With the hub

The RoamingHubCLI configures the retention of its histories in its `poi` section, `retainCommits` and
`compactAfterLogEntries`, and keeps an archive beside each store, `archive/`, unless `"archive": false`;
POI_API serves archived commits as it serves the commits it holds, `GET ~/RNs/QA/history/commits/{commitId}` and their
proofs, the list of the segments as `GET ~/RNs/QA/history/archive` and a segment as `GET ~/RNs/QA/history/archive/{number}`.
As the hub makes no checkpoints yet ([example A4](exampleA4.md#with-the-hub)), it compacts only where it took checkpoints
of another server, or where it has none at all.


## Next

- Archives: how long they are kept, and how personal data is removed from a segment, keeping the identifications of its
  commits, are open (section 10.7).
