# Archive orphan inventory and explicit cleanup

[Repository overview](../README.md) · [History](HISTORY.md) · [Bootstrap](BOOTSTRAP.md) · [Retention](RETENTION.md)

Interrupted archive and bootstrap writes can leave unacknowledged temporary siblings.
`RoamingNetworkArchiveMaintenance` reviews their exact local inventory and permits explicit cleanup
after rechecking that review under the corresponding writer lease. Installed data stays protected.

## Review and execution

```csharp
if (!RoamingNetworkArchiveMaintenance.TryInspectArchive(archivePath, out var plan, out var inspection))
    throw new InvalidOperationException(inspection.Error);

Console.WriteLine(plan!.ToJSON()); // Local administrative review, including paths and raw digests.

if (!RoamingNetworkArchiveMaintenance.TryExecute(plan, out var preview))
    throw new InvalidOperationException(preview.Error);
// CleanupAvailable: inventory still matches; the default performs no deletion.

// Call after the application/admin has selected this exact review for cleanup.
if (!RoamingNetworkArchiveMaintenance.TryExecute(plan, out var cleanup, cleanup: true))
{
    Console.WriteLine(cleanup.RemovedFiles); // Exact partial progress, if deletion had already begun.
    throw new InvalidOperationException(cleanup.Error);
}
```

For bootstrap staging, use `TryInspectBootstrap(stagingDirectory, out plan, out result, limits)`.
Both APIs require an existing directory. The archive or initial manifest may be absent after an
interrupted write. Inspection and execution acquire the normal writer lease: `<archive>.lock` or
`bootstrap.lock`. This coordination file may be created and is retained. Live histories/receivers
must release their writer lease before maintenance; competing writers cause `PersistenceFailure`.
Each call releases its maintenance lease on success and failure.

The immutable plan binds scope, absolute selected target, local budgets and the complete recognized
file inventory in ordinal filename order. For each file it records kind, byte length, UTC creation
and last-write timestamps, file attributes and an immutable SHA-256 digest of the raw bytes.
`Id` is a typed canonical-JSON SHA-256 ETag over that local review. `ToJSON()` exports it for review;
plans are produced by inspection and have no public deserialization or arbitrary-path constructor.
This local identity is independent of POI content, commit/signature identities and transport profiles.

## Recognized namespaces

| Scope | Protected installed files | Eligible temporary siblings |
| --- | --- | --- |
| Selected archive path | The exact selected archive filename | `<selected filename>.tmp-<32 lowercase GUID hex digits>` |
| Selected bootstrap directory | `manifest.cbor`, canonical nonnegative Int32 chunk names formatted as `D8` plus `.chunk` | Each recognized manifest/chunk name followed by `.tmp-<32 lowercase GUID hex digits>` |

Chunk indices larger than eight digits retain their canonical `D8` representation. Signs, overflow,
extra zero padding, malformed/uppercase GUID suffixes, added filename extensions and another archive's
temporary files are ignored. Installed namespace matching follows case-insensitive Windows paths
and ordinal comparison on other platforms. A selected archive whose own filename looks temporary
is still protected as installed data; only its further temporary siblings are eligible.

Inspection enumerates direct entries only. Unrelated files and nested directories are preserved;
their contents are not hashed or traversed. A recognized directory, symbolic file, linked lease or
directory alias in the selected path is rejected as `UnsupportedFile`. No temporary file is decoded
or promoted to installed data. Raw hashing and regular-file checks can review interrupted or corrupt
writes without granting them archive/signature authority.

Applications select a storage namespace they administer. Filename recognition identifies the
writer's convention; it does not independently prove who created a file. Cleanup authorization
comes from explicitly choosing the reviewed namespace and setting `cleanup: true`.

## Stale reviews and partial progress

Execution reacquires the lease and rescans with the plan's fixed budgets before any deletion.
Changes to recognized names, bytes, lengths, timestamps, attributes or installed data return
`InventoryChanged`. Newly installed archives/manifests also invalidate a review of an unpublished
write. Unrelated file changes remain outside the recognized review, while still consuming the
directory-entry budget.

Explicit cleanup removes only reviewed temporary leaf files, in ordinal order. Each is checked
again immediately before deletion; the installed inventory and absence of new temporary candidates
are checked afterward. Changed files are preserved. I/O errors stop further deletion and return
`PersistenceFailure` with `RemovedFiles` and `AffectedPath`. Unexpected mid-cleanup changes return
`InventoryChanged` and also report any partial removals. Deletion has no filesystem rollback.

After partial progress, inspect again before retrying: the original review cannot match the remaining
inventory. A failure before any change can retry an unchanged review. Installed archive, manifest,
chunk receipt and lease files are never deletion candidates. Read-only attributes are respected;
cleanup does not clear them or override filesystem permissions.

After cleaning an unpublished orphan manifest, `Create` can initialize the same now-empty staging
directory with its retained `bootstrap.lock`. With installed receipts, reopen using the independently
pinned manifest identity and continue from verified `NextChunk`. Cleaned activation/cold-archive
temporary files leave the final path absent, so the original activation/retention can be explicitly
retried. Actual archive replay and current trust checks remain part of the normal recovery APIs.

## Local budgets and outcomes

`RoamingNetworkArchiveCleanupLimits` has positive defaults:

| Budget | Default | Counts |
| --- | --- | --- |
| `MaxDirectoryEntries` | 100,000 | All direct entries, including ignored entries and coordination leases |
| `MaxFiles` | 8,192 | Recognized installed and temporary files together |
| `MaxFileBytes` | 1 GiB | One recognized file's raw length |
| `MaxTotalBytes` | 4 GiB | Aggregate recognized file lengths |

Exact budgets and zero-length files are accepted. Hashing streams raw files, with byte limits checked
before hashing; it does not allocate or decode a complete archive. Execution reapplies the same
budgets. `LimitExceeded` supplies `LimitViolation.Kind`, `Maximum` and `Observed` and preserves any
reported partial progress.

| Outcome | Meaning |
| --- | --- |
| `Inspected` | Immutable review returned; no data file changed |
| `CleanupAvailable` | Execution preview verified the unchanged review; no deletion |
| `Cleaned` | All reviewed temporary files removed; protected inventory still matches |
| `NothingToClean` | Explicit execution found no reviewed temporary files |
| `InventoryChanged` | Stale or unexpectedly changed recognized inventory; partial removals, if any, are reported |
| `PersistenceFailure` | Writer/read/delete access or other I/O failed; handles are released and partial progress is explicit |
| `UnsupportedFile` | Recognized nonregular file or linked selected directory/lease; `AffectedPath` identifies it |
| `LimitExceeded` | A fixed local inventory/hash budget was exceeded |
| `InvalidInput` | Invalid target, null plan or other invalid input |

Missing directories produce `PersistenceFailure` and are never created. Failed inspection returns
no plan. `RemovedFiles` is always initialized, including before-deletion failures.

## Executed evidence and limits

[ArchiveMaintenanceTests](../WWCP_POI_Tests/Interoperability/ArchiveMaintenanceTests.cs) adds
**94 passing cases**, including five actual child-process exits:

| Cases | Count |
| --- | --- |
| Review, default preview, explicit cleanup and empty/idempotent inventories | 6 |
| Changed recognized inventory, including equal-length/timestamp-preserving byte edits | 18 |
| Newly installed files and an archive filename resembling a temporary file | 3 |
| Held writer/read handles and lease release/retry | 6 |
| Recognized directories and symbolic files/leases/directory aliases | 6 |
| Injected partial failures/changes and real mid-cleanup file locks | 10 |
| Unexpected mid-cleanup installed/new temporary files | 4 |
| Exact/excess review and execution budgets | 16 |
| Invalid limits, targets and missing directories | 10 |
| Unrelated changes, platform filename case and canonical large chunk indices | 5 |
| Real live history/receiver writers, preserved runtime/progress and continuation | 2 |
| Process exits during archive, cold, manifest, chunk and activation writes, cleanup and original retry | 5 |
| Immutable/culture-independent review JSON and identity | 3 |

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~ArchiveMaintenanceTests'
```

The full run on **2026-10-08 passed 952 tests**, with zero failures and one parent-skipped crash
worker. Both explicit reference generators remain excluded from ordinary runs. The two link cases
were executed successfully on this filesystem; they skip on platforms lacking link-creation rights.
Ignored local TRX evidence is under `WWCP_POI_Tests/bin/TestResults/archive-maintenance.trx` and
`full-suite.trx`. The executed maintenance cases predate static-v2. Current fixed byte/signature artifacts were
subsequently regenerated for the new content profile.

Coordination requires writers to honor the same lease protocol. External filesystem changes are
detected at the scans and per-file checks; this is not a transaction against arbitrary processes
that bypass leases. The final read-handle close and path deletion are separate filesystem operations.
Power loss, directory-metadata durability and interruption inside deletion remain outside these tests.
Interrupted cleanup is reviewed again from the remaining inventory. Automatic cleanup schedules,
recursive directory cleanup and large-storage performance measurements are outside this package.

Sources: [maintenance](../WWCP_POI/History/RoamingNetworkArchiveMaintenance.cs),
[immutable plans, limits and outcomes](../WWCP_POI/History/RoamingNetworkArchiveCleanupPlan.cs).

## Temporary CBOR input capture

[Borrowed CBOR inputs](ARCHIVE-INPUT-STREAMS.md) use the existing archive temporary-file convention
under `RoamingNetworkArchiveReadOptions.TemporaryArchivePath`, the selected directory's
`wwcp-poi-input.cbor` namespace. Recovery never installs that archive and deletes only its own
`CreateNew`/`DeleteOnClose` file. Shared read leases let multiple captures coexist and exclude
this maintenance API's writer lease while any capture is live. The `.lock` file is retained.
Existing orphans are not swept by recovery. After readers finish, `TryInspectArchive` with that
exact path produces the usual immutable review; `TryExecute(..., cleanup: true)` explicitly
removes only unchanged reviewed temporary siblings. Installed namespace data and unrelated files
remain protected. Cancellation/ordinary failure, concurrent leases and an explicit orphan
review/default-preview/cleanup are included in the new 160-case input fixture.
