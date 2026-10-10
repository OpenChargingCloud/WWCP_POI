# Full snapshot links within the commit chain

[Repository overview](../README.md) · [History](HISTORY.md) · [Snapshot boundaries](SNAPSHOT-BOUNDARIES.md) · [Replication](REPLICATION.md) · [Bootstrap](BOOTSTRAP.md) · [Roadmap](ROADMAP.md)

Administrators can insert a complete static state into the existing chain, followed by ordinary
ChangeSet commits. A snapshot is an additional history link, with the current head as its single
parent. It does not start another chain. Its deterministic commit ID and equal peer signatures
bind the full payload, parent, timestamp, descriptions and application metadata.

## State and revision rules

`RoamingNetworkCommit.Kind` distinguishes `Checkpoint`, `ChangeSet` and `Snapshot`.
For a snapshot link, `ChangeSet` is absent and `Snapshot` is immutable
`RoamingNetworkSnapshotContent` containing `State`, `CreatedAt`, `Description` and `Metadata`.
Imported checkpoints retain their existing contract; they have no parents or invented timestamp.

| Value | Snapshot rule |
| --- | --- |
| `Parents` | Exactly one original parent ID |
| `Revision` | Parent revision plus one, checked for overflow |
| `AppliedChangeSetId` | Unchanged; no synthetic batch ID is introduced |
| `StateETags` | Same JSON/CBOR pair as the parent |
| `Snapshot.State` | Complete parent static data, with the new revision and unchanged last batch ID |
| Static POI timestamps | Unchanged; snapshot creation time belongs to the commit payload |
| Entity lifetimes | Unchanged, including embedded meters and assignments, central software/certificate catalogs and other owned objects |
| Local runtime | Preserved at publication; the derived version owns independent runtime schedules |

Static ETags exclude revision bookkeeping, so the new head has the same data identifiers and a
different commit ID. A ChangeSet created against the new head must use the **new revision**, even
though its source ETags also match the earlier static state. A batch prepared before the snapshot
cannot simply be published against it; prepare the batch against the current version.

History storage and import compare snapshot payload/header bookkeeping, both ETags and canonical
static JSON with the retained first parent. A snapshot cannot add, remove or change static data.
Its payload is validated through the same complete POI parsers as other persisted static states.

## Prepare, sign and publish

`PrepareSnapshot(expectedHead, createdAt, description, metadata)` prepares an unsigned commit
without retaining it or changing the head. The expected head must still be current at preparation.
Timestamp selection is explicit; there is no automatic time/count trigger.

```csharp
var source = history.Head;
using var metadata = JsonDocument.Parse("{\"InternalProcessId\":\"archive-2026-10\"}");

var prepared = history.PrepareSnapshot(
    source.Id,
    DateTimeOffset.UtcNow,
    ImmutableDictionary<String, String>.Empty
        .Add("de", "Vollständiger statischer Datenstand")
        .Add("en", "Complete static dataset"),
    ImmutableDictionary<String, JsonElement>.Empty
        .Add("Process", metadata.RootElement));

var signed = prepared
    .Sign(adminPrivateKey, "admin-key", COSEAlgorithm.ES256)
    .Sign(auditorPrivateKey, "audit-key", COSEAlgorithm.ES256);

if (!history.TryPublish(source.Id, signed, out var result))
    throw new InvalidOperationException(result.Error);

var head = result.Head;
// head.Commit.Kind == RoamingNetworkCommitKind.Snapshot
// head.Snapshot.Revision == source.Snapshot.Revision + 1
// head.Snapshot.ETags.SequenceEqual(source.Snapshot.ETags)
```

This example assumes an existing history configured with the application's trusted signature
verifier and authorization policy. Snapshot publication uses `verifyCommitSignature` and
`authorizeCommit`, including duplicate delivery, recovery, replication and adoption. A policy can
inspect `commit.Kind`, require administrator keys or multiple distinct accepted signers, and
restrict network IDs. Descriptions and metadata are audit information; they do not grant authority.
Without an authorization policy, unsigned snapshot links are accepted like unsigned ordinary commits.

`RoamingNetworkCommit.CreateSnapshot(parent, source, createdAt, ...)` also prepares an envelope
outside a history; it checks source/header consistency. Storage still requires a retained parent
and validates its exact static content. `TryStoreCommit` retains a validated snapshot branch;
`TryAdoptHead` previews and explicitly adopts retained descendants through the existing rules.

Metadata values are cloned, language labels and metadata keys must be nonempty, and values must
meet the signing JSON contract. Choose metadata before signing; recreating different snapshot
metadata creates a different identity. Multiple signatures remain equal peers and do not change ID.

## JSON, native CBOR and signatures

| Contract | Snapshot profile |
| --- | --- |
| Snapshot identity and wire envelope | `wwcp-poi-snapshot-commit-json-v1` |
| Snapshot commit signatures | `wwcp-poi-snapshot-commit-signature-json-v1` |
| Complete archive containing any snapshot | `wwcp-poi-history-v2` |
| Incremental page containing any snapshot | `wwcp-poi-commit-pack-v2` |
| Bootstrap manifest for a history-v2 archive | `wwcp-poi-bootstrap-manifest-v2` |

The exact snapshot commit envelope fields are `Profile`, `ContentProfile`, `Id`,
`RoamingNetworkId`, `Revision`, `Parents`, `StateETags`, `AppliedChangeSetId`, `Snapshot` and
`Signatures`. `Snapshot` contains exactly `CreatedAt`, `Description`, `Metadata` and `State`.
`Kind` and the convenience `CreatedAt` accessor are not extra top-level wire fields. There is
no `ChangeSet` field in this profile. Unknown, missing and duplicate envelope/payload fields are rejected.

`GetIdentityBytes()` uses Styx canonical JSON over the unsigned header and full snapshot payload,
excluding `Id` and commit peer envelopes. `State` includes static-v2, resulting revision, last batch
ID and derived static ETags. `CreatedAt` is written as UTC roundtrip text. Commit IDs still use
the typed JSON SHA-256 tuple in both transports, because their preimage is canonical JSON.
The signing preimage binds snapshot signing profile, algorithm, trusted key ID, encoding,
commit ID and complete unsigned commit. The ordinary commit identity/signing envelopes remain in use with the current `ContentProfile`.

`ToJSON()`/`Parse(...)` and `ToCBOR()`/`ParseCBOR(...)` use the existing commit APIs. CBOR carries
the complete state as native POI maps, SI readings with Styx metrological tags and binary ETag
digests. Application metadata uses the lossless JSON transport, preserving number spelling such
as `1.0`, `1e0` and `-0`. A metadata value containing SI text remains ordinary application text.
Runtime statuses, schedules, measurements and forecasts are never included in the static payload.

## Archives, transfer and adoption

Archives retain the original checkpoint and every original commit, including snapshots and
unpublished branches. Archive version 2 permits these snapshot envelopes; it has the same outer
fields as version 1. Recovery validates and replays every retained branch. Persistent publication
uses the existing atomic archive replacement before installing the new head. A head conflict,
invalid payload, rejected signature/authorization or persistence failure installs no candidate state.

Incremental pages still require all parents, retain original envelopes atomically and never select
a head implicitly. A snapshot counts as one commit. It is indivisible within a page: a large full
state can cause `CommitTooLarge`; increase the local page budget or use bounded archive bootstrap.
Complete-history announcements remain `wwcp-poi-replication-state-v1`, because acknowledged tips still include
**complete ancestry**. Complete-history pages without snapshots retain the version 1 contract.
Trusted-boundary announcements/pages use the separate contracts described below.

Bootstrap version 2 binds archive version 2 in `Manifest.ArchiveProfile`. Fragment envelopes retain
`wwcp-poi-bootstrap-chunk-v1`, which already transfers arbitrary frozen archive slices bound to
the manifest identity. Existing size/count limits, resume, validation and explicit activation apply.
Activation creates fresh local runtime. Snapshot-only publication and first-parent adoption within
an existing history preserve runtime; operation-history merge and secondary-parent adoption do
not treat snapshots as removals or fresh entity births.

## Snapshot entry, remaining packages and evidence

Snapshot creation retains all earlier history. A separate replica can now [start at an authorized snapshot boundary](SNAPSHOT-BOUNDARIES.md)
with unavailable earlier parents. Mandatory signature verification and an explicit chain/anchor policy
govern entry; partial archives, bootstrap and announcements use additional versioned contracts.
Boundary pages reference the retained root by ID/signatures without retransmitting its full state.
Unavailable cross-boundary dependencies require sufficient earlier history or another approved entry.
[Explicit retention plans](RETENTION.md) can now archive the complete preceding local history and
remove unneeded entries at a signed snapshot, after protecting branches and all merge dependencies.
The exact live head/runtime survives pruning; receipts identify cold history without rewriting commits.
Earlier replay remains independently available through digest-checked cold archives.
See [the roadmap](ROADMAP.md#7-admin-controlled-snapshot-commits-and-history-retention).

[SnapshotHistoryTests](../WWCP_POI_Tests/Interoperability/SnapshotHistoryTests.cs) now has 19 passing
cases for bookkeeping, sharing, exact static contents, metadata/signatures, both transports, runtime
independence, recovery, competing publication and pre-replacement failure/retry. Fixed snapshot
identity/signature/JSON/CBOR references also pass under three cultures; original v1 references remain
unchanged by that package. These executions predate static-v2; current reference artifacts
were subsequently regenerated. See [the verification report](VERIFICATION-SNAPSHOTS-RETENTION.md) for the preceding 952-test
run and limits. These are local regression references; independent peer interoperability and broader
graph/algorithm/process-crash/power-loss cases remain open.

[Snapshot/retention crash coverage](CRASH-RECOVERY.md) now adds six signed snapshot process exits:
before writing, after temporary flushing and after active replacement, each in complete and
previously pruned histories. Recovery preserves original peers/branches/receipts, rechecks current
trust and either publishes the exact candidate once or returns `AlreadyPublished`. Static ETags
and the last batch ID are preserved; fresh runtime and further signed continuation are checked.

### Subsequent exact root/head snapshot reuse

[Root/head reconstruction](SNAPSHOT-RECONSTRUCTION.md) now conditionally retains an immutable
snapshot after the complete domain parser, references, metadata and representation completion.
Every stored property byte, child and version must match; different spellings/defaults use the
existing capture path. Separate histories keep independent runtime objects; a private root-only
head can keep its freshly validated root model. Every peer/current policy, eager/lazy error timing,
cancellation and atomic publication remain intact. No persistent model or trust cache is added.
The package adds 86 cases and matched model/signature/restore/full-recovery measurements with
unchanged prepared inputs, outputs, C# harness and dependencies. Earlier results remain historical.
