# Bootstrap process-crash recovery

[Repository overview](../README.md) · [Bootstrap](BOOTSTRAP.md) · [Archive limits](ARCHIVE-LIMITS.md) · [Snapshot/retention crashes](CRASH-RECOVERY.md)

The [bootstrap crash fixture](../WWCP_POI_Tests/Interoperability/BootstrapCrashTests.cs) adds
**89 passing cases**: 36 actual child-process exits and 53 recovery, trust, lease and failure cases.
Every archive profile is exercised: full checkpoint history (v1), full history with snapshots (v2),
an authorized snapshot boundary (v3), and a pruned history with a receipt catalog (v4).

## Lost activation acknowledgement

An activation may install the destination archive and then exit before returning its history or
acknowledgement. Default activation still requires a new destination path. Applications can explicitly
request recovery of that completed write:

```csharp
using var receiver = RoamingNetworkBootstrapReceiver.Open(stagingDirectory, expectedManifest, limits);
if (!receiver.TryActivate(expectedManifest, out var replica, out var result,
    activate: true, archivePath: activationArchivePath,
    verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit,
    authorizeCommit: AuthorizeCommit, authorizeBootstrap: AuthorizeBootstrap,
    authorizeSnapshotBoundary: AcceptApprovedChainAndAnchor,
    recoverExistingArchive: true))
    throw new InvalidOperationException(result.Error);
using (replica!)
{
    // Activated: the destination was absent and has now been installed.
    // ActivationRecovered: the exact manifest-bound destination was already installed.
    // Explicitly select this replica in the application before incremental exchange.
}
```

`recoverExistingArchive: true` requires both `activate: true` and `archivePath`. A missing destination
follows normal activation. An existing destination is opened under its exclusive writer lease, read
within the receiver's `HistoryLimits`, compared byte-for-byte with the fully verified staged archive,
and replayed with current signature/commit/boundary policies. The returned persistent history keeps
that lease until disposed. Recovery never rewrites the existing archive.

The exact comparison includes the complete selected DAG, equal-peer arrays, catalog, head and CBOR
representation. Matching static ETags or commit IDs alone does not establish completed activation.
A newer valid history, changed signature array or semantically equal noncanonical CBOR is rejected as
`InvalidData`. A held destination lease or I/O failure returns `PersistenceFailure`. No failure returns
a history; staging and destination bytes remain available. A trust rejection during destination replay
also releases its newly acquired lease. Policies may run multiple times during a single attempt and
must not rely on an earlier successful preview or callback as cached authority.

Keep the expected manifest identity independently of the staging directory and authenticate its
selection. Snapshot boundaries additionally require approval of the original chain and selected
anchor, including rollback rules. Recovery does not automatically select the returned replica as the
application's live history. Dynamic statuses, measurements and forecasts start from local defaults.
If an application has already advanced the archive, use ordinary history recovery and the application's
current-head policy; this bootstrap cannot roll that archive back or acknowledge it as the frozen source.

## Receipt and activation exit points

Each table row runs once for all four profiles. Child processes flush an independent stage marker and
call `Environment.Exit(77)`; archive/receiver finally blocks, disposal and test cleanup do not run.
The shared worker is skipped once in ordinary parent runs. Parents verify both the marker and the
nonzero child exit, reopen with fresh callbacks and compare exact bytes and original signatures.

| Write | Exit point | Published state after reopening | Retry |
| --- | --- | --- | --- |
| Manifest | Before temporary write | No manifest or receipts | Create again in the empty staging directory |
| Manifest | Temporary file flushed | Only an unacknowledged manifest temporary file | Review/explicitly clean and initialize again, or choose a new empty staging folder |
| Manifest | Receipt installed | Exact `manifest.cbor`, zero chunks | Open using the independently pinned manifest ID |
| Chunk | Before temporary write | Previous verified prefix | Send the missing chunk again |
| Chunk | Temporary file flushed | Previous prefix plus an orphan temporary file | Preserve the orphan and send the missing chunk again |
| Chunk | Receipt installed | Prefix includes the unacknowledged chunk | Duplicate delivery returns `AlreadyStored` |
| Activation | Before temporary write | No destination archive | Activate from the completed verified staging |
| Activation | Temporary file flushed | No destination, exact orphan archive bytes | Preserve the orphan and activate again |
| Activation | Archive replaced | Exact manifest-bound destination | Explicit recovery returns `ActivationRecovered` |

Installed receipts are authoritative only after their exact lengths/digests and ordered prefix have
been rechecked. Orphans do not advance `NextChunk` and are never promoted or removed automatically.
An unpublished manifest has no resumable transfer. `Create` retains its dedicated-empty-folder
requirement; a folder containing an orphan manifest is preserved for application review.
[Explicit maintenance](ARCHIVE-MAINTENANCE.md) can now review and remove that temporary file under
the staging lease, allowing `Create` in the same folder. Installed receipts remain protected.

After each successful activation/recovery, tests verify original checkpoint/anchor/head identities,
retained branches, two original Ed25519 peers, v4 receipts and fresh runtime. They publish a signed
continuation, reopen again with current trust and verify its exact bytes. Failed attempts leave an
independent live history and its runtime unchanged.

## Additional recovery cases

| Cases | Count | Evidence |
| --- | --- | --- |
| Existing destinations | 20 | Different bytes, advanced valid head, changed peers, noncanonical equal-content CBOR, held writer lease; exact disk/staging/progress preserved |
| Current policy revocation | 18 | Batch signatures, commit signatures, whole commits, manifest selection and partial-history boundary policies |
| Revocation during destination replay | 4 | Source validation succeeds, target lease is acquired, fresh target authorization rejects, lease is released, later recovery succeeds |
| Activation write exceptions | 8 | Failure before writing/after flush; own temporary cleanup, reentry/disposal guards, released lease, retry from unchanged staging |
| Invalid recovery options | 3 | Explicit activation and destination path required before any destination write/lease |

## Executed evidence and practical limits

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~BootstrapCrashTests'
```

The complete run on **2026-10-08 passed 952 tests**, with zero failures and one parent-skipped child
worker. Both explicit reference generators remain excluded from ordinary runs. Local ignored TRX
evidence is under `WWCP_POI_Tests/bin/TestResults/full-suite.trx`. Existing fixed JSON/CBOR/signature
artifacts and wire profiles were unchanged by that package. These executed cases predate
static-v2; current reference artifacts were subsequently regenerated.

This is process-interruption evidence at named points on the local filesystem. It does not simulate
power loss, filesystem damage or termination inside a write/rename. Directory metadata is not
separately flushed. [Explicit orphan inventory/cleanup](ARCHIVE-MAINTENANCE.md) is implemented separately;
the fixtures delete only their own exact leaf files after verification. Later source encoding
now freezes bounded fragments directly; it still retains their total payload. POI payloads now
[preflight and emit directly](DIRECT-ARCHIVE-PAYLOAD.md), as do [ChangeSets](DIRECT-ARCHIVE-CHANGESET.md).
Prepared JSON/index/schema paths, complete receiver input and retained replay states remain.
[Indexed CBOR reading](INDEXED-ARCHIVES.md) now uses individual part trees and private boundary
suffix bytes instead of a complete archive tree.
[Streaming evidence](STREAMING-ARCHIVES.md) records the new build/measurement package. These crash
fixtures now pass again against that encoder in [the verification follow-up](VERIFICATION-STREAMING.md),
whose complete Release suite passes 1,174 tests, zero failures and one skipped worker. [Explicit local cold archive discovery](COLD-ARCHIVES.md) is
implemented separately. Streaming replay, automatic directory discovery, catalog indexes and
larger production measurements remain in the [roadmap](ROADMAP.md). Separate
[scaling evidence](SCALING.md) covers local export, durable receipt transfer and activation replay.

## Decoder baseline and incremental reader contracts

The subsequent [reader measurements](ARCHIVE-RECOVERY-COSTS.md) isolate scanning, CBOR trees,
immutable models, trust and branch replay with exact results for all four profiles. Its 58 new
contracts and 179 focused / 1,791 full passing cases preserve full-input errors before trust,
late callback behavior, every peer and fresh local runtime. That baseline did not change production.

The subsequent [indexed reader](INDEXED-ARCHIVES.md) removes the complete archive CBOR tree,
with full syntax/duplicate/depth/EOF validation before trust and preserved eager/lazy model timing.
Bootstrap checks canonical encoding without re-encoding the archive. Callback mutation of the
original input cannot change pending suffix models. All 257 focused / 1,869 full cases pass,
including 78 new cases. Resident input, private boundary bytes, part trees and retained states
remain costs. The subsequent [borrowed-input API](ARCHIVE-INPUT-STREAMS.md) adds general-history
sync/async non-seekable imports with bounded private capture, mapped recovery and cancellation.
This API leaves the source open and does not install an archive, check a cold receipt or activate a
bootstrap manifest. [Mapped recovery integration](MAPPED-ARCHIVE-RECOVERY.md) now updates
those separate routes while preserving their digest/manifest/lease contracts.
