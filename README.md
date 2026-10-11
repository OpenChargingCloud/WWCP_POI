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
- **Runtime:** admin status and status are not content and have no ETags,
  see `RuntimeStatus` of a history below.

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
- **Announcements:** `OnCommitAnnouncement` tells of every commit a history takes, in order:
  timestamp, commit id, parent ids, revision, description.
- **Runtime status:** `history.RuntimeStatus.TryUpdate(kind, entityType, entityId, value, timestamp, …)`
  for the network and its operators; kept across versions, dropped with its entity.
  An older timestamp is `Outdated`, the same value `Unchanged`; `OnStatusUpdate` tells of the rest.

`POI_API` serves a history in JSON, or in CBOR for `Accept: application/cbor` (also as request body):

| Request | Returns |
|---|---|
| `GET ~/roamingNetworks/{id}/history/head` | metadata of the head commit |
| `GET ~/roamingNetworks/{id}/history/snapshot` | head commit and complete network |
| `GET ~/roamingNetworks/{id}/history/commitInfos?since={commitId}&max={n}` | metadata of the commits since |
| `GET ~/roamingNetworks/{id}/history/commits?since={commitId}&max={n}` | complete commits since |
| `GET ~/roamingNetworks/{id}/history/commits/{commitId}` | one commit |
| `COMMIT ~/roamingNetworks/{id}/history/changeSets?expectedHead={commitId}&rebase=true` | 201 published, 200 already published, 409 head moved, 422 refused |

Submitting is refused unless `AuthorizeChangeSetSubmission` decides who may submit what;
signed change sets need `VerifyChangeSetSignature`.

### WebSocket

`GET ~/roamingNetworks/{id}/history/ws` upgrades to a WebSocket with OCPP-J frames,
JSON in text frames, CBOR in binary frames; every request is answered in its own format.

```
[2, "42", "commitInfos", { "since": "9f3c…", "max": 10 }]    request
[3, "42", { "commitInfos": [ … ] }]                          response
[4, "42", "NotFound", "The commit … is unknown here!", {}]   request error
```

- **Actions:** `head`, `snapshot`, `commitInfos`, `commits`, `commit {commitId}`,
  `commitChangeSet {changeSet, expectedHead?, rebase?}` → `{status, …}`, `runtimeStatus`.
- **Subscriptions:** `topics` lists them, `subscribe`/`unsubscribe {topics: […]}` change them:
  `commitAnnouncements`, `commits` (complete; above `MaxPushedCommitSize` only announced), `adminStatusUpdates`,
  `statusUpdates`. Pushes are sends, `[6, "p1", action, payload]`,
  in the format of the last `subscribe`. They end with the connection; a client that falls more than
  `MaxQueuedPushes` pushes or `MaxQueuedPushBytes` behind is disconnected and catches up after reconnecting.
- **Async:** requests are processed concurrently, up to `MaxConcurrentRequests` per connection;
  answers come when ready and are matched by their id. A connection's requests are cancelled when it closes.
- **setStatus** `{kind, entityType, entityId, value, timestamp?, dataSource?}` → `{result, description?, current?}`,
  allowed as `AuthorizeStatusUpdate` decides (refused without it); set it at the leader, followers take it from there.
- **Batches:** messages of the same action aggregate. A request `{batch: [p1, p2, …]}` runs per payload,
  in order, and is answered with `{batch: [r1, {error: {code, description, details}}, …]}`, up to `MaxBatchSize`.
  Pushes of the same action that wait together go out as one `{batch: […]}`, up to the `maxBatchSize`
  of `subscribe` (1 turns it off); `PushCoalescingDelay` lets more gather.
- **Errors:** the codes of OCPP-J plus `NotFound`; an unreadable frame is answered on id `"-1"`.
- **Access:** open like the HTTP reads, unless `AuthenticateWebSocketClient` is set;
  `commitChangeSet` asks `AuthorizeChangeSetSubmission` with the upgrade request.

## Open

- Persistence and retention of histories
- Finer subscription filters, energy status with EVSEs
- Commits signed by the leader
- Further entities: charging pools, stations, EVSEs, e-mobility providers, …

## Tests

```bash
dotnet test WWCP_POI3_Tests/WWCP_POI3_Tests.csproj
```

## Funding

This Open Source project is partially funded by the [NGI Zero Commons Fund](https://nlnet.nl/commonsfund/) as part of our [EVQI project](https://nlnet.nl/project/EVQI/).

We also appreciate any additional funding and long-term support for the Chargy family, for example via [GitHub Sponsors](https://github.com/sponsors/GraphDefined), as it helps us keep the project sustainable, independent and useful for the entire e-mobility community.

<center>
  <img src="images/NGI0_tag.svg" height="30">
</center>
