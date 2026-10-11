# Snapshot and retention process-crash recovery

[Repository overview](../README.md) · [History](HISTORY.md) · [Snapshots](SNAPSHOTS.md) · [Retention](RETENTION.md)

[SnapshotRetentionCrashTests](../WWCP_POI_Tests/Interoperability/SnapshotRetentionCrashTests.cs)
adds **26 passing cases**: 22 actual child-process exits and four cold-write exception/reentry cases.
The existing `PersistenceTests.ArchiveCrashWorker` dispatches ordinary ChangeSet, signed snapshot,
retention and [bootstrap](BOOTSTRAP-CRASH-RECOVERY.md) operations. It is skipped once in ordinary parent runs and executed separately by
the parent crash tests. Both explicit reference generators remain excluded from ordinary runs.

Each crash case runs against a newly allocated persistent archive. All new cases use the two
public fixture Ed25519 keys, original peer arrays, fixed timestamps and independently pinned
checkpoint/root/candidate or review-plan identities. `Environment.Exit(77)` stops the test host
without unwinding archive finally blocks, releasing managed histories or running test cleanup.
A flushed stage marker proves that the requested point was reached; a generic failed child test
alone is not accepted as a crash result. The launcher drains stdout/stderr, enforces a 45-second
timeout and kills its owned process tree on timeout.

## Signed snapshot publication

The six snapshot cases cross three write points with two initial histories: a complete original
chain and an already pruned history with its signed root and prior receipt. Both retain an
unpublished branch. Expected before/after CBOR documents are constructed independently before
launching the writer and compared byte for byte after its exit.

| Exit point | Surviving active archive | Orphan active temporary file | Re-delivery of the same signed snapshot |
| --- | --- | --- | --- |
| `BeforeTemporaryWrite` | Exact old archive | None | `Published` |
| `TemporaryFileFlushed` | Exact old archive | Exact complete candidate archive | `Published` |
| `ArchiveReplaced` | Exact new archive | None | `AlreadyPublished` |

Fresh reopening checks both peer signatures again. Revoked verification fails without rewriting
the archive; boundary histories additionally reject absent or rejecting root policies. Failed
opening releases its writer lease. Successful recovery preserves the original checkpoint, replay
root, branch envelopes and prior receipts. A snapshot advances revision once while preserving
static ETags and the last applied batch ID. Retrying never invents a second snapshot. A signed
ordinary ChangeSet can then be published and recovered through another reopening.

## Cold backup and pruning

Four cold-write points and three active-write points are each tested against complete and already
pruned histories. Two further exits test verification/flush of a matching existing cold destination.
Each plan explicitly releases an old unpublished branch and keeps its published head. Repeated
pruning includes an existing receipt and a cold archive that points to the still earlier archive.

| Exit point | Requested cold destination | Surviving active archive | Orphan temporary data |
| --- | --- | --- | --- |
| `BeforeColdTemporaryWrite` | Absent | Exact old archive | None |
| `ColdTemporaryFileFlushed` | Absent | Exact old archive | Exact complete old archive at a cold temporary path |
| `ColdArchivePublished` | Exact old archive | Exact old archive | None |
| `ColdArchiveVerified` | Exact old archive, digest checked and flushed | Exact old archive | None |
| `BeforeTemporaryWrite` | Exact old archive | Exact old archive | None |
| `TemporaryFileFlushed` | Exact old archive | Exact old archive | Exact complete pruned archive at an active temporary path |
| `ArchiveReplaced` | Exact old archive | Exact new root/suffix/catalog | None |

The cold source ETag must equal SHA-256 of the complete original CBOR bytes, including branches,
peers and any earlier receipts. Digest-checked cold recovery replays the original signed retained
graph and confirms every newly removed identity was present there. Repeated cases also recover
the earlier archive using the previous receipt's independently located source digest.

Reopening always installs one consistent active root, retained set and receipt catalog. The
published head ID, revision, last batch ID and static ETags remain unchanged by pruning. Original
snapshot envelopes are compared directly. Both commit and batch peer signatures are reverified.
Recorded newly removed identities become `Archived` and resolve to the exact expected receipt.

## Retry after a missing acknowledgement

The surviving archive determines what completed. A returned in-memory result from the terminated
writer is unavailable and is not evidence of success or failure.

| Recovered state | Explicit handling |
| --- | --- |
| Old active root/inventory | Re-execute the unchanged reviewed plan; a published matching cold file is reused after digest verification and flush |
| New active root with the expected receipt `PlanId` | Identify the completed operation from that receipt; the stale original plan returns `InventoryChanged` and cannot add another receipt |
| New snapshot head | Re-delivery of its original signed envelope returns `AlreadyPublished` |

Tests compare exact receipt CBOR and plan IDs, not only receipt counts. Retry from the old state
produces the same expected new bytes. Stale-plan rejection preserves memory and disk. A retry
within a recovered live history preserves the exact current `Head`/`Head.Network` instances and
newly delivered local statuses, measurements and forecasts. Every recovered chain also accepts
and reopens a further signed ordinary commit.

Runtime values from either terminated or previous history are absent after recovery. The parent
sets `charging`; the writer sets `reserved`, a real-time power value and a forecast. Fresh reopening
has neither live status and no previous measurement/forecast. Runtime is locally initialized and
does not enter the snapshot, archive identity or pruning receipt.

## Exception and lease evidence

The four cold-stage exception cases assert the exact injected I/O error, unchanged active disk,
complete history and original head/runtime objects. Each own temporary file is removed during
ordinary exception unwinding. A completed cold destination is preserved for retry. Cold file and
writer lease handles can be reopened exclusively after failure. Reentrant pruning and disposal
are rejected while the operation is active. Removing the observer permits the same plan to finish.

The new `RetentionWriteObserver`/`RetentionWriteStage` are internal test seams. All four points
precede active replacement and allow exception injection. Existing active `ArchiveReplaced`
observers may terminate the process but must not throw. No observer is installed in ordinary use;
archive profiles, signing envelopes and public retention outcomes were unchanged by that package.
These executed cases predate static-v2; current reference bytes were subsequently regenerated.

## Running and limits

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~SnapshotRetentionCrashTests|FullyQualifiedName~PersistenceTests'
```

The focused run passed **41 tests**, with zero failures and one parent-skipped child worker,
including existing archive and replication persistence cases. Local ignored TRX evidence is under
`WWCP_POI_Tests/bin/TestResults/snapshot-retention-crashes.trx` and `full-suite.trx`.
The latest full NUnit run on **2026-10-08 passed 952 tests**, with zero failures and one skipped child worker.
The library build completed with zero errors and the existing 360 warnings.

This demonstrates process interruption at the named points on the current local filesystem.
It does not simulate power loss, filesystem damage, abrupt termination within a write/rename,
independent remote implementations or all possible I/O failures. Directory metadata is not
separately flushed. Orphan files from terminated writes are ignored rather than automatically
promoted or cleaned; the test fixture deletes only its own exact leaf files after verification.
[Explicit archive maintenance](ARCHIVE-MAINTENANCE.md) now reviews and removes recognized temporary
siblings under the writer lease, with stale-review rejection and partial failure diagnostics.
Its five real exit cases verify original archive/cold/bootstrap retry after cleanup.
The [bootstrap crash package](BOOTSTRAP-CRASH-RECOVERY.md) adds 36 actual receipt/activation exits
and explicit recovery of exact already installed destinations, with 89 passing cases overall.
[Explicit local cold archive discovery](COLD-ARCHIVES.md) is implemented separately. Streaming replay,
compressed receipt catalogs, automatic directory discovery and broader performance work remain in the
[roadmap](ROADMAP.md). Separate [Release measurements](SCALING.md) cover full archive rewriting/replay.
