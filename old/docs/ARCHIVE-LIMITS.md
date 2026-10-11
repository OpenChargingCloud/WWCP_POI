# Local archive recovery limits

[Repository overview](../README.md) · [History](HISTORY.md) · [Retention](RETENTION.md) · [Bootstrap](BOOTSTRAP.md)

`RoamingNetworkHistoryLimits` bounds incoming complete, snapshot-boundary and pruned histories.
The same checks apply to history-v1/v2/v3/v4 through JSON/CBOR parsing, persistent reopening,
cold retrieval and bootstrap final validation. Limits are selected locally and cannot be enlarged
by archive fields, receipt counts or a sender's bootstrap manifest. Wire profiles, identities and
signatures are governed by the current [static content profile](INTEROPERABILITY.md).
The executed limit cases below predate static-v2; current reference bytes were regenerated.

## Budgets and counting

All settings must be positive. A value exactly at the configured maximum is accepted.

| Property | Default | What consumes the budget |
| --- | --- | --- |
| `MaxArchiveBytes` | 256 MiB | Encoded CBOR bytes or UTF-8 JSON bytes, including every envelope and receipt |
| `MaxCommits` | 100,000 | All retained commits, including the one checkpoint or snapshot replay root |
| `MaxRetentionReceipts` | 4,096 | All entries in the archive's `RetentionReceipts` array |
| `MaxCatalogCommitIds` | 1,000,000 | The sum of entries in every receipt's `PrunedCommits` and `ArchiveOnlyTips` arrays |

Catalog IDs are counted per occurrence, rather than after deduplication. A released tip recorded
in both arrays consumes two entries. An identity appearing in another pruning event consumes
the budget again. A small retained suffix therefore cannot bypass limits through a large catalog.
Checkpoint, anchor, head and source-digest fields consume bytes but are not catalog-array entries.

`RoamingNetworkBootstrapLimits` exposes the same archive budgets through `HistoryLimits`, together
with its existing fragment, chunk-count and encoded-message limits. Its constructor also accepts
`maxRetentionReceipts` and `maxCatalogCommitIds`; these settings do not enter the manifest.
Bootstrap source generation checks retained/catalog counts before freezing archive bytes.

## Validation order

1. Direct JSON/CBOR parsing checks encoded length before allocating its document. JSON first
   measures UTF-8 byte length; the caller has already supplied a .NET string.
2. `Open`, `ReadColdArchive` and catalog cold recovery inspect the opened file's 64-bit length
   before mapping it, without a complete managed input array. Known-length rejection reports the
   full length. Cold retrieval checks the independently expected complete CBOR digest while
   holding the same file/view through replay. See [mapped file recovery](MAPPED-ARCHIVE-RECOVERY.md).
3. Streaming `Utf8JsonReader`/Styx `CBORReader` scans skip payloads and inspect the archive/receipt
   containers before a JSON/CBOR DOM, receipt identities, static reconstruction or replay is built.
   JSON and indefinite CBOR arrays stop at the first item over budget. Valid definite CBOR arrays
   can reject their declared count immediately. Impossible lengths and malformed containers remain
   decoding errors rather than allocating their claimed size.
4. Normal strict field/profile/identity/signature checks and complete retained-branch replay follow.
   Passing limits is not evidence that content is valid or authorized.

Bootstrap staging first enforces manifest and fragment budgets. Final preview and activation both
scan the actual reassembled archive with the receiver's `HistoryLimits` before materialization.
A manifest understating `CommitCount` cannot bypass the actual retained count. Bootstrap manifest
authorization may run before the scan; commit, batch and snapshot-boundary callbacks do not run
for inputs rejected by these archive limits.

## Recovery and diagnostics

```csharp
var limits = new RoamingNetworkHistoryLimits(
    maxArchiveBytes: 64 * 1024 * 1024,
    maxCommits: 10000,
    maxRetentionReceipts: 128,
    maxCatalogCommitIds: 100000);

try
{
    using var recovered = RoamingNetworkHistory.Open(
        archivePath,
        verifyBatchSignature: VerifyBatchPeer,
        verifyCommitSignature: VerifyCommitPeer,
        authorizeCommit: AcceptCommit,
        authorizeSnapshotBoundary: AcceptApprovedChainAndAnchor,
        limits: limits);
}
catch (RoamingNetworkHistoryLimitException exception)
{
    var violation = exception.Violation;
    Console.WriteLine($"{violation.Kind}: {violation.Observed} > {violation.Maximum}");
}
```

`Parse`, `ParseCBOR`, `Open` and `ReadColdArchive` append an optional `limits` argument with the
defaults above. `RoamingNetworkHistoryLimitException` derives from `ArgumentException` and exposes
an immutable `Violation` containing `Kind`, `Maximum` and `Observed`. Observed counts can be the
first excessive count; recovery does not need to enumerate the rest of a rejected input.
Malformed data, digest mismatches, invalid signatures and authorization failures retain their
ordinary error paths.

Bootstrap final rejection returns `false`, no history and `RoamingNetworkBootstrapOutcome.LimitExceeded`.
`RoamingNetworkBootstrapResult.LimitViolation` supplies the same structured diagnostic. Completed
staging receipts and `NextChunk` remain available for review/reopen under an explicitly selected
local policy. Existing malformed/trust/storage failures keep their established outcomes.

Failed direct opening releases its writer lease without rewriting the archive. Cold reading creates
no writer lease. Bootstrap limit rejection does not touch its activation destination, live history
or runtime, and preserves its manifest/fragments. No rejected archive is partially installed.

## Executed evidence and practical limits

[ArchiveLimitsTests](../WWCP_POI_Tests/Interoperability/ArchiveLimitsTests.cs) contributes **60 passing
cases**: exact and excessive bytes/commits across all four profiles and both formats; UTF-8 and
escaped keys; definite/indefinite CBOR; multiple receipts and repeated IDs; a two-commit suffix with
1,000 catalog entries; direct/cold recovery and released leases; malformed input and impossible
CBOR lengths; files larger than `Int32.MaxValue`; dishonest bootstrap counts; rejected preview and
activation with unchanged staging/archive/head/runtime; exact-budget reopen/activation and fresh
runtime; source catalog checks and invalid configuration. Two existing bootstrap cases now assert
the structured retained-count exception. The full run on **2026-10-08 passed 952 tests**, with zero
failures and one skipped child-process worker executed by its parent crash tests.

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~ArchiveLimitsTests'
```

These budgets bound encoded input and inventory counts, not all memory allocations or CPU work.
Archive output, persistence and retention now write directly to streams; bootstrap retains
its frozen fragment payload. [Indexed CBOR decoding](INDEXED-ARCHIVES.md) holds the complete
input, range indexes and individual part/key/scalar trees; boundary profiles privately freeze
pending commit bytes before trust. It no longer builds a complete archive tree. Replay retains
static states. A long replay may retain many large states while fitting all input
budgets. Settings are local recovery policies, not persisted capacity rules for later publication
or pruning; administrators must choose them for expected retained states and catalog growth.
The separate [process-crash package](CRASH-RECOVERY.md) now covers named snapshot/cold/pruning
write points. The [bootstrap crash package](BOOTSTRAP-CRASH-RECOVERY.md) covers named receipt/activation
exits and exact destination recovery under the same local limits.
The [cold catalog API](COLD-ARCHIVES.md) applies those same archive budgets to each explicitly supplied
candidate and actual matched containers. Its separate positive location budget caps catalog enumeration;
byte-limit failures appear in candidate diagnostics, and matched container failures return `LimitExceeded`.
Separate [scaling measurements](SCALING.md) record current replay/export costs; local rejection
budgets remain independent of throughput or capacity claims. Streaming replay, compressed receipt indexes and
uninstrumented write/rename interruption remain in the [roadmap](ROADMAP.md).

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
remain costs. [Borrowed CBOR streams](ARCHIVE-INPUT-STREAMS.md) now consume at most the byte
budget plus one before rejection, keep the source open, and bound private capture memory/file bytes.
Stream ArchiveBytes observations are maximum+1 instead of the known full span length. Counts/depth/
syntax still validate after capture, before trust. Mapping, individual models and retained states
remain outside a constant total-memory bound.
