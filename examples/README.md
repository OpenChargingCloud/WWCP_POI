# Examples

Seven examples, each going on from the one before: a roaming network whose operator three organizations hold, a
charging station operator asking to be added to it, its pools, their templates and their groups, checkpoints of the
history, with a correction between two of them, witnesses signing them, and the history compacted after them
with an archive. They are C# with
POI 3 alone, without a hub, in one console project, [csharp/](csharp/); A2 and A3 also show the same with the
[RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI): at its prompt or over SSH, in its web interface and
over HTTP.

| | What it does | Revisions | Signed by |
|---|---|---|---|
| [Example A1](exampleA1.md) | **The creation of the roaming network `QA`, and how to check it.** The roaming network operator `OCC`, whose keys three organizations hold, two of them deciding via a 2/3 quorum, states its identity keys and quorum in a key statement, each organization certifies its own signing key, `QA` is made, and its trust anchor published as a QR code. Then `QA` is checked from the QR code and what is published alone. | 0 | `OCC`, its three holders |
| [Example A2](exampleA2.md) | **The charging station operator `DE*GEF`.** The operator makes two identity keys and a signing key for each usage, keeps them in `DE-GEF/`, makes the change set adding it and signs it with its identity keys, as the proof that it holds them; two holders of `OCC` sign it as well. With the hub: it signs as the RNO and lets the change set wait for the signatures of the party. | 1 | the change set: `DE*GEF`'s identity keys and two holders of `OCC`; the commit: `OCC` |
| [Example A3](exampleA3.md) | **Two pools, their templates and their groups.** `DE*GEF` adds the templates of a DC and an AC station and their EVSEs, two pools of a DC and an AC station of two EVSEs each, and a station group and an EVSE group for each current; then the pools as they are at runtime. Only its own program can do it: nobody else has a key of `DE*GEF`; signed by its key for its tariffs, or by `OCC`, a pool is refused. | 2 to 6 | `DE*GEF`, its `infrastructure` key |
| [Example A4](exampleA4.md) | **The first checkpoint, and joining from it.** `OCC` makes the first checkpoint of `QA`, of revision 6, a signed statement over the Merkle root of its commits; `DE*GEF` adds two pools more, their AC stations recorded with 44 kW and EVSEs of 22 kW; then `QA` is joined from the checkpoint's snapshot and the commits since, a commit is proved to be part of the checkpoint, and changed checkpoints, snapshots and proofs are refused. | checkpoint 0; 7, 8 | the checkpoint: `OCC`, its three holders; the commits: `DE*GEF` |
| [Example A5](exampleA5.md) | **A correction between two checkpoints.** `OCC` makes checkpoint 1, of revision 8, `DE*GEF` corrects the AC stations to 22 kW with EVSEs of 11 kW, `OCC` makes checkpoint 2, of revision 9; then the line of checkpoints is checked, and the AC stations at every checkpoint: commit by commit from checkpoint 0, and from each checkpoint's snapshot on its own. | checkpoints 1, 2; 9 | as in A4 |
| [Example A6](exampleA6.md) | **Witnesses sign the checkpoints.** A calibration authority and an association of operators each take the history of `QA` from its genesis snapshot, commit by commit, check the three checkpoints against it and sign them; `OCC` takes their signatures. A second checkpoint of a sequence, a split view, is refused by a witness; a verifier requires `OCC` and both witnesses, and a proof of a commit is evidence of the history they saw. | checkpoints 0 to 2 | the witnesses, with keys trusted by configuration |
| [Example A7](exampleA7.md) | **Compaction with an archive, after the checkpoints.** `OCC` compacts the history of `QA` to the commit of checkpoint 0, then of checkpoint 1, archiving what it drops, one segment each; dropped commits and their proofs come from the archive, also after a restart. Then each segment alone gives the versions from its root to its end again, its end the one its checkpoint states, its commits the Merkle root the checkpoint signs. | none new | - |

What each way needs:

| Way | Example A1 | Example A2 | Example A3 | Examples A4 to A7 |
|---|---|---|---|---|
| C#, POI 3 alone ([csharp/](csharp/)) | `RoamingNetworkKeyStatement.Create`, `RoamingNetworkSigningKeyCertificate.Create`, `RoamingNetworkHistory.Start` | `CreateChangeSet([ Add(operator) ])`, signed by `DE*GEF`'s identity keys and two holders of `OCC` | `history.Commit`, signed by `DE*GEF`'s `infrastructure` key | `history.TryMakeCheckpoint`, `TryGetCommitProof`, `RoamingNetworkHistory.TryFromCheckpointSnapshot`, `TryWitness`, `TryAddCheckpointSignatures`, `history.Compact` with an archive |
| prompt, SSH | `roamingNetworks add QA …` | `roamingNetworks QA add chargingStationOperator …` | shows: `roamingNetworks QA` | - |
| web interface | **New roaming network** | **New party of a roaming network** | shows: **Points of interest** | - |
| HTTP, as an administrator | `POST /api/v1/poi/RNs`, with an API key | `POST /api/v1/poi/RNs/QA/CSOs`, `202` | - | - |
| HTTP, by anybody | - | `POST /poi/RNs/QA/history/changeSets/pending/{id}/signatures`, the signatures of `DE*GEF`'s identity keys | `COMMIT /poi/RNs/QA/history/changeSets`, a change set `DE*GEF` signs | reads: `GET /poi/RNs/QA/history/checkpoints`, their snapshots and proofs; witnesses: `POST …/checkpoints/{checkpointId}/signatures`; `GET /poi/RNs/QA/history/archive`; the hub makes no checkpoints yet |

Run the examples in [csharp/](csharp/), where their output stays:

```
dotnet run
```

A run writes the private keys of the organizations and of `DE*GEF` into directories of their own, what is published into
`published/`, the keys of the witnesses a verifier trusts into `trusted/`, and the history of `QA` into `poi/QA/`, all
ignored by git. What POI 3 is, its entities and its change sets, is in [docs/](../docs/README.md) and the [README](../README.md) of WWCP_POI.
