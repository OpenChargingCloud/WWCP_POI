# WWCP POI

Points of interest of e-mobility as immutable, versioned data, synchronized between servers.
A .NET 10 library of the Open Charging Cloud.

| Directory | What it is |
|---|---|
| `WWCP_POI2` | Shared lower layer: identifications, status values, predefined strings, energy information, transparency software |
| `WWCP_POI3` | Roaming networks, operators, change sets, history and their HTTP API |
| `old` | The earlier implementation, for reference only; not developed further |

## Data

A `RoamingNetwork` holds its charging station operators and data licenses.
Charging pools, stations, EVSEs and e-mobility providers follow.

- **Content:** everything in the JSON and CBOR representations, completely, in a fixed order:
  operators and licenses embedded, `created`, `lastChange` and `customData` included,
  timestamps to the millisecond. CBOR uses the keys and values of JSON.
- **ETags:** SHA-256, SHA-384 or SHA-512 over canonical JSON or canonical CBOR.
  They identify a version; `CalcETags()` and `ValidateETags(…)`.
- **Runtime:** admin status and status are not content and have no ETags.

## Change sets

A change set is an ordered list of changes against one version, named by its before ETags,
leading to the version named by its after ETags.

```csharp
var cs  = rn1.CreateChangeSet([
              RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "v2"),
              RoamingNetworkChange.Add           (newOperator),
              RoamingNetworkChange.UpdateProperty(gef, "telephone", null, "+49 30 7654321")
          ]);

var rn2 = rn1.ApplyChangeSet(cs);   // rn1 stays as it was
```

- **Changes:** `UpdateProperty`/`RemoveProperty` of the network (`name`, `description`, `dataSource`,
  `dataLicenses`, `customData`) and of operators; `Add`/`Remove` of operators.
- **Old values:** `CreateChangeSet` gives every change the value it finds; JSON `null` means
  "expected absent". A change finding another value conflicts.
- **Timestamps:** a change set sets `lastChange` to its `CreatedAt`, so its after ETags are deterministic.
- **Rebase:** `rn2.TryRebase(cs, …)` prepares a change set of an older version again, unless it conflicts.
- **Signatures:** `Sign(…)` and `VerifySignature(…)` over canonical JSON (COSE algorithms of Styx);
  a signed change set is applied only with a verifier.
- **Wire:** JSON and CBOR; `RoamingNetworkChangeSetLimits` bound bytes, depth, changes and signatures.

## History and sync

One leading server orders all changes; the other servers follow it.

```csharp
var leader = RoamingNetworkHistory.Start(rn1);
var result = leader.Publish(cs, Rebase: true);          // Published, AlreadyPublished, HeadMoved, Refused

RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out _);
leader.TryGetCommitsSince(follower!.Head.Commit.Id, out var commits, out _);
follower.TryAppend(commits!, out _, out _);
```

- **Commits:** a change set, its parent, its revision and the ETags of the result.
  The id is the SHA-256 of the commit's canonical JSON without signatures; it is checked when read.
- **Followers** start from a snapshot (head commit and complete network) and append commits;
  each must follow the head and lead to its ETags.

`POI_API` serves a history in JSON:

| Request | Returns |
|---|---|
| `GET ~/roamingNetworks/{id}/history/head` | metadata of the head commit |
| `GET ~/roamingNetworks/{id}/history/snapshot` | head commit and complete network |
| `GET ~/roamingNetworks/{id}/history/commitInfos?since={commitId}&max={n}` | metadata of the commits since |
| `GET ~/roamingNetworks/{id}/history/commits?since={commitId}&max={n}` | complete commits since |
| `GET ~/roamingNetworks/{id}/history/commits/{commitId}` | one commit |
| `POST ~/roamingNetworks/{id}/history/changeSets?expectedHead={commitId}&rebase=true` | 201 published, 200 already published, 409 head moved, 422 refused |

Submitting is refused unless `AuthorizeChangeSetSubmission` decides who may submit what;
signed change sets need `VerifyChangeSetSignature`.

## Open

- CBOR over HTTP
- Persistence and retention of histories
- Runtime status as a channel of its own
- Commits signed by the leader
- Further entities: charging pools, stations, EVSEs, e-mobility providers, …

## Tests

```bash
dotnet test WWCP_POI3_Tests/WWCP_POI3_Tests.csproj
```

## License

Affero GPL 3.0, see `LICENSE`. Contributions under the `Contributor License Agreement.txt`.
