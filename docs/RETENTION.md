# Explicit history archival and pruning

[Repository overview](../README.md) · [Snapshots](SNAPSHOTS.md) · [Snapshot boundaries](SNAPSHOT-BOUNDARIES.md) · [History](HISTORY.md) · [Replication](REPLICATION.md)

Administrators can reduce active history at an original signed snapshot without creating a new
chain. Snapshot creation and partial bootstrap exports still retain every source commit.
Only `TryExecuteRetention(..., prune: true)` removes entries from the active history.
Its complete preceding local archive is durably saved first. Original commit IDs, ordered parents,
snapshot payloads and equal peer signatures are never rewritten.

## Choosing a boundary

For a two-year window, first select a retained first-parent cutoff commit using an application
time policy. `GetRetentionSnapshots(cutoffCommit)` lists signed snapshots at or before that commit,
newest first in chain order. Infrequent snapshots can require keeping more than two years.
Author-supplied timestamps do not independently prove DAG ordering or elapsed time.

`TryPlanRetention(snapshotId, createdAt, out plan, out result, cutoffCommit: cutoff)` checks that
the signed snapshot lies on the current head's first-parent line and is at or before the explicit
cutoff. Every snapshot peer must pass the history's current verifier and commit policy. Planning
does not write files, change the root or delete entries. There is no automatic schedule.

The plan protects:

- The current published head, even when an unpublished child makes it no longer a frontier tip.
- Every current frontier tip, including unpublished branches.
- Every explicitly supplied `protectedCommits` identity, such as a required merge base or pending work.
- Every first and additional parent required by those commits down to the proposed snapshot.

Every protected parent path must terminate at this snapshot. Old branches and cross-boundary
merge parents produce `Blocked`, with typed `Blockers` containing the protected tip and unresolved
history landmarks. These landmarks can be an earlier replay root, rather than the first crossing
parent. A later signed snapshot after an old merge can supply a covering boundary. Otherwise choose
an earlier covering snapshot or keep the dependencies active. There is one active replay root.

`archiveOnlyTips` explicitly releases named **current frontier tips**, allowing their otherwise
unneeded ancestry to exist only in the cold archive. The head and explicit protected bases cannot
be released. A released tip still remains active if another protected tip requires it. This option
can archive unpublished work after the cutoff; it is an explicit administrative choice, not an age filter.

## Review and execution

A local immutable `RoamingNetworkRetentionPlan` contains the proposed signed `Boundary`, previous
anchor, expected head, UTC planning timestamp, optional cutoff, retained/pruned commit IDs, released
tips and protected bases. `ToJSON()` exports a review document with structured typed IDs and ETags.
Its canonical JSON SHA-256 `Id` binds the plan. `SourceArchiveETag` binds the complete deterministic
CBOR source archive, including head, unpublished branches, every peer envelope and earlier receipts.
The executable plan is produced locally; after a restart, rebuild and review it against recovered history.

Application callbacks and the explicitly supplied execution flag govern approval:

```csharp
if (!history.TryPlanRetention(snapshotId, DateTimeOffset.UtcNow,
                             out var plan, out var planning,
                             cutoffCommit: cutoffCommit,
                             protectedCommits: requiredBases))
    throw new InvalidOperationException(planning.Error);

// Present plan.ToJSON() and planning.Blockers in the administrative workflow.
if (!history.TryExecuteRetention(plan, coldArchivePath,
                                authorizeBoundary: AcceptApprovedChainAndAnchor,
                                out var execution,
                                prune: true,
                                authorizeRetention: AcceptReviewedPlan))
    throw new InvalidOperationException(execution.Error);
```

The example's approval functions belong to the application. The mandatory boundary callback must
authorize the original checkpoint/anchor pair and rollback policy; it becomes this history's boundary
policy for subsequent static mutations. Snapshot signatures alone do not prove the omitted checkpoint
association. The optional retention callback can enforce the reviewed plan identity and organizational
policy. Callbacks must not reenter mutations. Execution rechecks current root trust and all retained
commit/batch signatures before writing. `prune` defaults to false: that path revalidates the plan and
returns `Planned`, without invoking approval callbacks for the proposed new root or writing files.

The source must remain exact until execution. A changed head produces `HeadConflict`; adding a branch,
peer signature or receipt with the same head produces `InventoryChanged`. Runtime deliveries are
excluded from archive identity and can occur between review and execution. Recompute and review a
stale plan; execution never silently expands or shrinks the approved set.

| Outcome | Meaning |
| --- | --- |
| `Planned` | Review plan prepared or execution preview accepted; no deletion |
| `Pruned` | Cold archive checked and active history installed |
| `Blocked` | A protected parent path crosses the proposed snapshot boundary |
| `NothingToPrune` | All current entries remain required |
| `HeadConflict` | Published head changed after planning |
| `InventoryChanged` | Source archive or replay root changed after planning |
| `Unauthorized` | Boundary or retention callback returned false |
| `InvalidInput` | Input, signatures, recomputed requirements or a callback exception failed |
| `PersistenceFailure` | Cold backup or active archive write failed |
| `Unavailable` | Disposed history or mutation callback reentry |

## Persistence ordering and runtime

Execution runs under the history gate. All candidate maps, batch inventory, receipt catalog and
active archive bytes are prepared before installation:

1. Write the **entire preceding local CBOR archive** to a unique cold temporary file and `Flush(true)`.
2. Publish its requested destination without overwriting an existing file. An existing destination
   is accepted for retry only when its length and SHA-256 match the exact reviewed source archive.
3. Hold its writer lease and a file handle excluding other writers/deletion, verify the digest and
   flush it (also for a matching existing destination), then replace the active archive
   through the existing flushed temporary-file/atomic-rename mechanism when persistence is configured.
4. Install retained entries, rebuilt batch-ID inventory, new root, policy and receipts together.

In-memory histories also require a durable cold archive before entries are removed. Cold and active
destinations and writer leases must differ; linked directories are resolved for this check, and cold
file/lease symlinks are rejected. Destinations belong to the administrator's trusted filesystem.
A pre-replacement failure leaves the active archive and all in-memory state unchanged. A completed
cold archive can remain after an active write failure and supports retry of the unchanged plan.
It is not automatically deleted. Reopening after a successful active replacement recovers the
pruned archive and its catalog. Cold archive locations must be independently inventoried by digest.
Execution requires write access for the explicit durable flush, including a matching retry destination.
Subsequent `ReadColdArchive` retrieval requires only read access.

The **exact existing `Head` and `Head.Network` instances remain unchanged** during pruning. No static
revision, commit ID, state ETag, runtime schedule, status, measurement or forecast is modified.
Recovery and cold reads create fresh local runtime. Later lifetime comparisons stop at the new root
and use an opaque baseline there; pruning does not prove entity births before that boundary.
Previously returned objects can remain alive in application memory after removal from active history.

Batch-ID uniqueness covers retained suffix batches and the new anchor's last applied batch ID.
Older omitted batch IDs are no longer inventoried; applications continue issuing globally fresh IDs.

This is backup-before-replacement ordering, not a transaction across two independent filesystems.
It uses the existing file flush/rename guarantees; directory metadata is not separately flushed,
and power-loss behavior has not been demonstrated. Existing post-replacement diagnostics must not throw.

## Receipts, lookup and cold retrieval

`RetentionReceipts` preserves immutable `wwcp-poi-retention-receipt-v1` records. Each has its own
canonical JSON identity, the reviewed `PlanId`, original checkpoint, before/after anchors, unchanged
head, explicit planning timestamp, source CBOR archive ETag, removed commit IDs and released tips.
JSON uses structured IDs/digest tuples; CBOR uses native maps and binary digest tuples. Local paths
are excluded. Parse/ParseCBOR validate fields and recompute receipt identity.

The catalog retains commit IDs rather than removed payloads. `LookupCommit(id)` distinguishes
`Retained`, `Archived` and `Unknown`. Retained content takes precedence if an archived branch is
later reimported with all required suffix parents. For an archived identity, the lookup supplies a
receipt with its source archive ETag and a proposed signed boundary. Replication requests for known
archived tips return `SnapshotRequired` with `MissingCommits`, `RetentionReceipt` and `ProposedBoundary`;
arbitrary unknown tips remain `UnknownTip`. Imports needing recorded cold dependencies return
`HistoryRequired`. A response never silently changes the receiver's root or head.

The receipt is **unsigned bookkeeping**, not administrative authority or an independently signed
proof that a particular commit belonged to the omitted chain. Archive storage/transport and its
catalog must be trusted independently. The source archive digest enables checking the actual prior
archive; its original commit signatures, identities, parents and replay provide the historical evidence.
Root signatures do not bind the receipt catalog. Bootstrap manifests bind it by archive digest,
but those manifests themselves require independently retained/trusted identities.

```csharp
var lookup = history.LookupCommit(oldCommitId);
if (lookup.Receipt is { } receipt)
{
    using var oldHistory = RoamingNetworkHistory.ReadColdArchive(
        locatedColdArchive, receipt.SourceArchiveETag,
        verifyBatchSignature: VerifyBatchPeer,
        verifyCommitSignature: VerifyCommitPeer,
        authorizeCommit: AcceptCommit,
        authorizeSnapshotBoundary: AcceptApprovedChainAndAnchor,
        limits: new RoamingNetworkHistoryLimits(maxArchiveBytes: 64 * 1024 * 1024,
            maxCommits: 10000, maxRetentionReceipts: 128, maxCatalogCommitIds: 100000));
    var oldState = oldHistory.GetSnapshot(oldCommitId);
}
```

`ReadColdArchive` opens the file only for reading, checks its exact ETag and returns a separate
in-memory history after fresh verification/replay. It preserves the active history. An earlier cold
archive can itself start at a previous snapshot and contain receipts pointing to still older archives.
These archives must remain retrievable; this package does not delete them or automatically hydrate
older ancestry into an active boundary history.

[Local archive limits](ARCHIVE-LIMITS.md) check the opened file's known byte length before read-only mapping,
then retained commits, receipt count and the aggregate of both receipt ID arrays before document
materialization/replay. Direct `Parse`/`ParseCBOR`/`Open` and bootstrap final validation apply the same
budgets. Repeated IDs across arrays/events consume the catalog budget again. A rejected cold read
never creates a writer lease or changes the active graph; structured exceptions identify the budget.

For moved files or multiple local copies, [cold archive discovery](COLD-ARCHIVES.md) supplies an
immutable `RoamingNetworkColdArchiveCatalog` keyed by the receipt's `SourceArchiveETag`.
`TryReadColdArchive(receipt, catalog, out history, out result, ...)` diagnoses candidates in registration
order and returns a separate verified history only after current trust, replay and receipt/source
membership checks. Optional `requestedCommit` rejects unknown or still archived IDs; the latter can
return an earlier receipt for an explicit further lookup. No paths are added to historical receipts,
no candidate files are changed and active ancestry remains unchanged.

## Wire profiles and evidence

`wwcp-poi-history-v4` adds a required nonempty `RetentionReceipts` array to the snapshot-boundary
archive fields. `wwcp-poi-bootstrap-manifest-v4` binds that exact archive profile and its explicit
anchor. Full/snapshot/bootstrap recovery preserves the catalog. Excluding unrelated branches from
a snapshot bootstrap does not create pruning receipts: the source still retains them. Existing
history-v1/v2/v3, commit identities, snapshot signing inputs, page contracts and chunk-v1 bytes
remain unchanged when the new catalog is absent.

[RetentionTests](../WWCP_POI_Tests/Interoperability/RetentionTests.cs) now has 38 passing cases for
protected branches/bases and merge parents, stale inventories/peers, explicit releases, cold archives,
callback rejection/reentry, pre-replacement failure/retry, destination/lease guards, repeated catalog
recovery, bootstrap exclusions and v4 transfer, cold reads, archived/unknown lookups, missing cold
dependencies, reimport and concurrent execution/runtime delivery. Fixed plan/receipt/archive/manifest
regression references passed at that baseline. Before static-v2, the full run had 952 passing tests;
current artifacts were subsequently regenerated. See [verification and limits](VERIFICATION-SNAPSHOTS-RETENTION.md).
Independent implementation interoperability and process-crash/power-loss proof for every retention
stage remain broader work; the named write boundaries now have actual process-crash coverage below.
These local regression files do not authenticate unsigned catalog bookkeeping.

[SnapshotRetentionCrashTests](CRASH-RECOVERY.md) adds sixteen pruning process exits around four
cold and three active archive points, including previously pruned history and matching existing
cold destinations. Recovery rechecks original signatures, exact roots/catalogs/source bytes and
cold digests; repeated cases retrieve the still older archive through its earlier receipt. Four
additional cold-stage exceptions assert unchanged live state, own-temp cleanup, released handles,
reentry rejection and successful retry. The six signed snapshot exits in the same fixture bring
this package to 26 passing cases.

After a missing acknowledgement, reopen using independently approved chain/root policies. An old
active inventory can retry the same reviewed plan against a verified matching cold destination.
A new active root with the expected receipt `PlanId` identifies a completed operation; the stale
plan returns `InventoryChanged` without adding a second receipt. Retry preserves the recovered
live head/runtime, and both paths accept further signed publication. These tests establish named
process-interruption points; power loss and interruption inside individual writes/renames remain open.

[Explicit archive maintenance](ARCHIVE-MAINTENANCE.md) can review and remove orphan temporary
siblings of active or cold archives under their respective writer leases. Installed cold data and
active files are protected; stale inventory and partial deletions are reported. A real cold-flush
process exit now exercises cleanup and retry of the original reviewed retention operation.

Planning hashes/counts the reviewed CBOR archive incrementally. Cold publication and active
replacement stream directly into temporary files before durable flush/rename; the cold write
checks the reviewed length/digest before publication and retains the verified handle through active
replacement. POI payloads [preflight and emit directly](DIRECT-ARCHIVE-PAYLOAD.md), as do
[ChangeSets](DIRECT-ARCHIVE-CHANGESET.md); prepared JSON/index/schema paths and retained graph
closures remain in memory. See [streaming contracts and evidence](STREAMING-ARCHIVES.md). The catalog
uses space proportional to recorded removed IDs, with repeated events retaining their own receipts.
Hash-only catalogs still grow; compressed indexes, streaming replay, automatic archive discovery,
independent implementations and larger production measurements remain future work. Separate
[scaling evidence](SCALING.md) covers repeated pruning catalogs and retained archive/replay costs.
