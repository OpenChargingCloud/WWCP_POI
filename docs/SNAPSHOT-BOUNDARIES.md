# Starting at an authorized snapshot boundary

[Repository overview](../README.md) · [Snapshots](SNAPSHOTS.md) · [History](HISTORY.md) · [Bootstrap](BOOTSTRAP.md) · [Replication](REPLICATION.md)

A replica can start at an original signed snapshot commit without downloading its earlier parents.
The snapshot keeps its original ID, parent ID, revision, full static payload, metadata and signatures.
Later ChangeSet and snapshot commits keep their original IDs as well. The original chain checkpoint
is recorded separately from the local replay root; no replacement genesis or new chain is invented.

## Explicit trust and ancestry scope

`RoamingNetworkSnapshotBoundary` pairs an original `Checkpoint` ID with a signed `SnapshotCommit`.
Its `Anchor` is that snapshot's ID. Construction checks structure and nonempty signatures, without
establishing application trust. A boundary replica requires both:

- A trusted commit signature verifier. Every supplied anchor peer is verified through the existing
  commit verification contract, using the snapshot signing profile.
- An explicit boundary authorization callback accepting the **checkpoint/anchor pair**. It must
  enforce the application's chain selection, authorized snapshot and rollback policy.

The snapshot signature binds its immediate predecessor and complete static state. With omitted
ancestry, it does **not** independently prove that the claimed original checkpoint is its ancestor,
or replay every earlier operation. The outer checkpoint claim is therefore an application trust
decision. Pin approved checkpoint and snapshot IDs through trusted configuration or an independently
authorized bootstrap manifest; comparing a claim only with values obtained from the same untrusted
message would not supply that authorization.

Boundary recovery/activation never falls back to unsigned acceptance. Missing policies, empty
anchor signatures, rejected peers or rejected authorization fail before a history is returned.
The boundary is reauthorized for static preparation, publication, import, adoption and merge;
recovery and each bootstrap preview/activation receive current policy afresh. No successful preview
is cached as authority. Ordinary complete-history APIs retain their existing trust contract.

| History property | Meaning |
| --- | --- |
| `CheckpointId` | Original chain identity, possibly without locally retained checkpoint content |
| `AnchorId` | Local replay root, either that checkpoint or the original signed snapshot |
| `HasCompleteAncestry` | True only when replay reaches the original checkpoint |
| `SnapshotBoundary` | Current signed anchor envelope and checkpoint claim, or null for complete history |
| `Commits` | Every locally retained envelope; excluded predecessors are not invented |

Only the single authorized root may retain an external predecessor. Every later commit still
requires all parents locally and validates its first-parent source state and resulting bookkeeping.
Ancestry traversal, ordering and lifetime reconstruction stop explicitly at the root. No earlier
parent is acknowledged, treated as retained or implicitly accepted.

## Direct entry and persistence

When the full signed snapshot is already available, create a separate fresh-runtime history:

```csharp
Boolean VerifyPeer(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer)
    => trustedPublicKeys.TryGetValue(peer.KeyId, out var publicKey) &&
       commit.VerifySignature(peer, publicKey, peer.KeyId, out _);

Boolean AuthorizeBoundary(RoamingNetworkSnapshotBoundary boundary)
    => boundary.Checkpoint == expectedCheckpoint &&
       boundary.Anchor == approvedSnapshotId;

using var replica = RoamingNetworkHistory.FromSnapshot(
    expectedCheckpoint,
    signedSnapshot,
    authorizeBoundary: AuthorizeBoundary,
    verifyCommitSignature: VerifyPeer,
    verifyBatchSignature: VerifyTrustedBatchPeer,
    authorizeCommit: AuthorizeNetworkCommit);
```

`expectedCheckpoint`, `approvedSnapshotId`, the trusted keys and optional ordinary commit policy
are independently selected by the application. Add administrator/quorum restrictions in the boundary
policy as required. Accepting a timestamp alone is not a rollback rule; a newer local head requires
its own retained approval/expected-head policy.

`CreatePersistentFromSnapshot(path, ...)` also acquires an exclusive writer lease and creates a
new archive without overwriting one. `Parse`, `ParseCBOR` and `Open` add optional
`authorizeSnapshotBoundary`; this callback and `verifyCommitSignature` are mandatory for a boundary
archive. No trust callback or public key is persisted as future authority. Archive head bookkeeping
is outside individual commit signatures, so applications retain expected-head/rollback constraints
independently, as for complete archives.

The static state begins at the snapshot's resulting revision and last applied batch ID. Source
and result ETags remain unchanged. Runtime starts with domain defaults. Creating or activating this
separate history never moves another live history's head or transfers its current runtime implicitly.

## Bounded snapshot bootstrap

`CreateSnapshotBootstrap(snapshotId, targetTip, chunkBytes, limits)` freezes the selected signed
snapshot plus the requested tip's complete retained suffix, including all required merge parents.
`targetTip` defaults to the published head. Unrelated branches are not included in this export;
the source history remains intact. Every target parent path must terminate at the selected snapshot.
Otherwise export rejects the selection and identifies unresolved dependency landmarks.

```csharp
var source = sender.CreateSnapshotBootstrap(approvedSnapshotId);
var manifest = RoamingNetworkBootstrapManifest.ParseCBOR(source.Manifest.ToCBOR());
using var receiver = RoamingNetworkBootstrapReceiver.Create(stagingDirectory, manifest);

while (receiver.NextChunk < manifest.ChunkCount)
{
    var fragment = source.CreateChunk(receiver.NextChunk);
    if (!receiver.TryAcceptChunk(fragment, out var receipt))
        throw new InvalidOperationException(receipt.Error);
}

if (!receiver.TryActivate(manifest.Id, out var replica, out var activation,
    activate: true,
    verifyBatchSignature: VerifyTrustedBatchPeer,
    verifyCommitSignature: VerifyPeer,
    authorizeCommit: AuthorizeNetworkCommit,
    authorizeBootstrap: AuthorizeExpectedManifest,
    authorizeSnapshotBoundary: AuthorizeBoundary))
    throw new InvalidOperationException(activation.Error);
```

The example assumes the application has independently approved the manifest and chain/anchor pair.
For a validation preview omit `activate: true`; preview returns no usable history. Restart, exact
byte/count limits, corruption checks and optional activation to a new archive path use the existing
bootstrap workflow. The archive includes the complete snapshot only once; fragments may split CBOR
tokens. An existing boundary history's `CreateBootstrap()` includes all its retained suffix branches.

## Boundary wire profiles

| Contract | Profile | Additional boundary fields |
| --- | --- | --- |
| Partial archive | `wwcp-poi-history-v3` | `CheckpointId`, complete `SnapshotCommit` |
| Pruned partial archive | `wwcp-poi-history-v4` | Boundary fields plus nonempty `RetentionReceipts` |
| Bootstrap manifest | `wwcp-poi-bootstrap-manifest-v3` | Original `Checkpoint`, explicit `Anchor`, history-v3 `ArchiveProfile` |
| Pruned bootstrap manifest | `wwcp-poi-bootstrap-manifest-v4` | Explicit `Anchor` and history-v4 archive/catalog digest |
| Retained-tip announcement | `wwcp-poi-replication-state-v2` | Original `Checkpoint`, explicit `Anchor` |
| Incremental page | `wwcp-poi-commit-pack-v3` | `CheckpointId`, `AnchorId`, `AnchorSignatures` |

Version 3 archive fields are exactly `Profile`, `ContentProfile`, `CheckpointId`, `SnapshotCommit`,
`Commits` and `Head`. `Commits` excludes the root and contains parent-before-child original
envelopes. Manifest count includes the root, and its identity binds checkpoint, anchor, head,
archive identity, fragment digests and layout. Profile/anchor/archive/count/head disagreements fail.
Fragment envelopes remain `wwcp-poi-bootstrap-chunk-v1`. Static-v1, snapshot identity/signatures
and complete-history version 1/2 contracts retain their existing preimages.
Version 4 additionally preserves [explicit pruning receipts](RETENTION.md). They distinguish recorded
archived identities from merely unknown IDs, without authenticating omitted history by themselves.

## Incremental exchange after entry

An announcement acknowledges only ancestry down to `Anchor`, including all retained merge parents.
It never acknowledges the anchor's excluded predecessor. Complete-history receivers retain the
version 1 announcement; boundary receivers use version 2. A full sender can therefore select a
boundary-compatible suffix without falsely assuming that the receiver holds earlier commits.

Version 3 pages include the root **ID and peer envelopes**, using the receiver's already retained
snapshot as signature input. They do not retransmit its full state. The root remains outside the
ordinary transition-count limit, while all its identifiers/signatures count toward the wire byte limit.
Parse JSON/CBOR with an explicit local resolver:

```csharp
var page = RoamingNetworkCommitPack.ParseCBOR(bytes, resolveAnchor: replica.GetCommit);
if (!replica.TryImportCommitPack(page, out var imported))
    throw new InvalidOperationException(imported.Error);
```

`Parse` has the same resolver parameter. It must return the exact local signed snapshot ID; unknown
roots fail rather than fetching data or accepting a new boundary. Parsing checks structure/identity;
import rechecks current trust and atomically unions peer envelopes. Import never selects a head.
The incoming root peer array may be empty when the sender has no additional envelopes. The local
anchor keeps its previously accepted signatures; its combined peers and boundary authorization
are rechecked. This does not permit unsigned boundary creation, recovery or activation.
Use the existing preview/explicit `TryAdoptHead` after complete retention, with expected-head checks.

| Outcome | Meaning |
| --- | --- |
| Replication `SnapshotRequired` | A compatible root is not available/acknowledged; `ProposedBoundary` may suggest an original signed snapshot |
| Replication `HistoryRequired` | The requested tip needs parent paths outside the selected boundary |
| Merge `HistoryRequired` | Requested tips or an explicit base are not locally retained within the boundary |

`MissingCommits` provides typed unresolved IDs. An absent ID does not prove prior retention or
compaction. `ProposedBoundary` is a proposal requiring fresh approval, never authority to replace
the active history. If an announcement's root is unavailable, do not silently switch to the proposed
root. A full receiver fetching from a partial sender must already acknowledge that sender's anchor;
otherwise it needs sufficient original ancestry or a separate explicitly authorized bootstrap.

## Merge dependencies and runtime continuity

All known parent paths must remain inside the chosen suffix. A later merge referencing an old
pre-boundary branch requires an earlier covering snapshot or full history; it is not rewritten to
drop the additional parent. A newly approved snapshot after that merge can provide another entry
point. There is currently one boundary per history, with no automatic multi-anchor import.

Known suffix branches can merge using retained bases. An explicit unavailable earlier base returns
`HistoryRequired`, not an invented root comparison. Lifetime origin `OperationIndex == -1` at the
trusted snapshot is a local baseline, without proof of pre-boundary object births. Later original
operations provide lifetime evidence normally. First-parent adoption preserves local runtime;
secondary-parent adoption conservatively transfers it using retained suffix operations. Runtime
from a different live history is not transferable solely from equal final IDs or static ETags.

Batch-ID uniqueness covers retained suffix batches and the anchor's last applied batch ID.
Excluded older batch IDs cannot be independently inventoried. Applications continue to issue
globally fresh batch IDs; full-history receivers enforce their complete retained inventory.

## Evidence and next work

The library builds; no boundary-specific tests have been added or executed in this package.
Dedicated JSON/CBOR/signature vectors, fresh-policy rejection, rollback decisions, resumable activation,
large-anchor incremental limits, atomic import/recovery, runtime continuity, excluded branches and
cross-boundary merge coverage remain pending. These new profiles are not yet frozen reference vectors.

Snapshot exports and separate boundary creation leave the source intact. [Explicit retention](RETENTION.md)
can additionally archive and prune an existing history under a reviewed plan, preserving its exact
head/runtime and installing a new signed root. History-v4/manifest-v4 preserve its pruning receipts.
Only recorded archived IDs establish the local `Archived` lookup classification; absent IDs alone
do not. Retention schedules and automatic boundary switching remain application work.
