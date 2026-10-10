# Cold archive discovery and verified retrieval

[Repository overview](../README.md) · [Retention](RETENTION.md) · [Archive limits](ARCHIVE-LIMITS.md) · [History](HISTORY.md)

Pruning receipts already identify their complete preceding archive by `SourceArchiveETag` and
contain no file paths. `RoamingNetworkColdArchiveCatalog` supplies immutable local location claims
for those exact CBOR digests. Moving a file requires a new catalog entry; receipt identities,
signatures, static state, active history and wire contracts remain unchanged.

## Local catalog

```csharp
var catalog = new RoamingNetworkColdArchiveCatalog([
    new(receipt.SourceArchiveETag, previousArchivePath),
    new(receipt.SourceArchiveETag, movedArchivePath)
], maxLocations: 4096);
```

`RoamingNetworkColdArchiveLocation` requires a valid typed CBOR ETag and captures the absolute
local path at construction. Catalog construction freezes the supplied enumerable without reading
files, scanning directories or creating leases. `Locations` and `GetCandidates(digest)` return
initialized immutable arrays. Unknown valid digests return an empty candidate array.

Candidates retain registration order; duplicate digest/path pairs are removed. Path comparison
uses ordinal case-insensitive comparison on Windows and ordinal comparison elsewhere. The positive
`maxLocations` budget counts all supplied entries, including duplicates, and stops enumeration at
the first excess claim. The default is 4,096 supplied locations. This local configuration is separate
from archive byte/container budgets. Build a new catalog when locations change.

## Verified retrieval

```csharp
var lookup = activeHistory.LookupCommit(oldCommitId);
if (lookup.Receipt is { } receipt)
{
    var catalog = new RoamingNetworkColdArchiveCatalog([
        new(receipt.SourceArchiveETag, movedArchivePath)
    ]);
    if (!RoamingNetworkHistory.TryReadColdArchive(receipt, catalog,
        out var oldHistory, out var result,
        verifyBatchSignature: VerifyBatchPeer,
        verifyCommitSignature: VerifyCommitPeer,
        authorizeCommit: AcceptCommit,
        authorizeSnapshotBoundary: AcceptApprovedChainAndAnchor,
        limits: new RoamingNetworkHistoryLimits(maxArchiveBytes: 64 * 1024 * 1024,
            maxCommits: 10000, maxRetentionReceipts: 128, maxCatalogCommitIds: 100000),
        requestedCommit: oldCommitId,
        authorizeReceipt: AcceptReceiptFromTrustedArchive))
        throw new InvalidOperationException(result.Error);
    using (oldHistory!)
    {
        var oldState = oldHistory!.GetSnapshot(oldCommitId);
        // This history is separate from activeHistory and can be inspected or exchanged explicitly.
    }
}
```

Each attempt:

1. Checks inputs and the optional current `authorizeReceipt` callback before accessing candidates.
2. Maps candidates in registration order after checking their known full file length against byte limits.
3. Records missing files, I/O failures, excess lengths and exact SHA-256 digest mismatches. These
   location failures allow trying the next candidate.
4. Selects the first matching digest, applies actual container limits before document materialization,
   and replays all retained branches with freshly supplied signature/commit/boundary policies.
5. Checks the receipt's original chain, prior anchor and head against the recovered archive. Its
   new anchor must be a retained signed snapshot, and all recorded pruned IDs and archive-only tips
   must actually be retained in this source archive.
6. If `requestedCommit` is supplied, requires that commit to be retained in the recovered archive.

A digest-matched archive's verification, trust or receipt failure ends the attempt. Multiple matching
copies resolve to the first registered readable copy; a newer valid archive has different bytes and
cannot replace the frozen source selected by the receipt. A later call always checks current trust
again. Callback exceptions produce structured failure and no returned history.

Success returns a separate in-memory history with the original commits, peers, roots, branches and
catalog. Runtime starts from local defaults, without foreign status schedules, measurements or
forecasts. The caller owns/disposes this history. It has no archive writer lease; subsequent local
publication in this in-memory history does not write to any catalog file. The active history's
ancestry, head, runtime and pruning receipts are preserved.

Receipt provenance and administrative approval remain application policies. Receipts and location
claims are unsigned bookkeeping. Use an independently trusted receipt/archive selection and current
key/quorum rules. Snapshot-rooted source archives additionally require explicit chain/anchor and
rollback authorization. Successful source replay verifies historical content and membership.

Mapped input stays open through digest/trust checks and closes before return. Both cold APIs
accept an optional trailing `CancellationToken`; direct reading throws cancellation, while catalog
recovery returns `Cancelled` with no history and the candidate diagnostics already collected.
The token is cleared from retained trust delegates after success. See
[mapped recovery and cancellation](MAPPED-ARCHIVE-RECOVERY.md).

## Structured outcomes

| Outcome | Meaning |
| --- | --- |
| `Cancelled` | Recovery was cancelled before handing out a history; source files remain unchanged |
| `Recovered` | A matching archive passed current trust/replay/receipt checks; requested commit, if supplied, is retained |
| `ArchiveNotFound` | No registered candidates, or every attempted location is missing |
| `CandidatesRejected` | No digest match and at least one candidate had a byte limit, digest or read failure |
| `Unauthorized` | Current receipt policy returned false before file access |
| `VerificationFailed` | Matching bytes failed decoding, profiles, signatures, current authority or replay; also reports callback exceptions |
| `ReceiptMismatch` | Verified archive disagrees with recorded chain/roots/head or commit membership |
| `CommitNotRetained` | Archive is verified but the requested commit is archived there or unknown; `CommitLookup` distinguishes these cases |
| `LimitExceeded` | Digest-matched archive exceeds actual commit/receipt/catalog bounds; `LimitViolation` gives kind, maximum and observed count |
| `InvalidInput` | Missing receipt/catalog or invalid requested commit ID; no candidate is accessed |

`Candidates` records each attempted path, its typed outcome, the observed digest when hashed, and
any read error or byte-limit violation. `ResolvedPath` identifies a selected digest match, including
when later verification fails. Every failure returns `history == null`. Read handles are released
on success and failure; candidate files and local catalog are preserved and no lock files are created.

## Older receipt chains

An archive from repeated pruning can itself contain receipts for earlier archives. If an optional
requested commit is archived inside that verified source, `CommitNotRetained` includes
`CommitLookup.Receipt` and its proposed boundary. The application can explicitly make another
`TryReadColdArchive` call for that earlier receipt using the same catalog and current policies.
Unknown commits have no invented receipt. Calls perform one archive lookup at a time; applications
choose traversal depth and track visited receipt/digest identities.

Direct `ReadColdArchive(path, expectedArchiveETag, ...)` remains available for an already located
archive and reports errors as exceptions. The catalog API adds structured location diagnostics and
receipt/source membership checks. Older ancestry is retrieved into a separate history; importing or
selecting it in a live replica requires the existing explicit replication/adoption APIs.

## Executed evidence and limits

[ColdArchiveTests](../WWCP_POI_Tests/Interoperability/ColdArchiveTests.cs) adds **98 passing cases**:

| Cases | Count |
| --- | --- |
| Immutable normalized catalog, invalid configuration and bounded duplicate enumeration | 11 |
| Moved archives with JSON/CBOR receipts, original peers/branches, fresh runtime and local continuation | 6 |
| Ordered fallback after missing, digest-mismatched, unreadable and excessive files | 12 |
| No usable candidates with structured diagnostics and no replay | 18 |
| Multiple matching copies and newer valid archives | 3 |
| Fresh policy revocation/exceptions, missing verifiers/boundary authority and no trust fallback | 25 |
| Actual commit/receipt/catalog limits, exact budgets and released handles | 5 |
| Forged receipt membership/roots, non-snapshot and unsigned anchors | 8 |
| Matching malformed/unsupported archives and another valid chain | 3 |
| Unknown requested commits | 3 |
| Explicit traversal to a moved earlier archive | 1 |
| Invalid recovery inputs before file access | 3 |

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~ColdArchiveTests'
```

The complete run on **2026-10-08 passed 952 tests**, with zero failures and one parent-skipped crash
worker. Both explicit reference generators remain excluded from ordinary runs. Local ignored TRX
evidence is under `WWCP_POI_Tests/bin/TestResults/cold-archive-tests.trx` and `full-suite.trx`.
The executed cases predate static-v2; current fixed JSON/CBOR/signature artifacts were
subsequently regenerated for the shared-catalog model.

The catalog is explicitly supplied and held in memory. Directory discovery, network/archive services,
catalog persistence and automatic file registration are application work. Limits bound encoded bytes
and inventory counts; complete archive parsing/replay still retains full static states. This package
does not establish independent peer interoperability. Separate [scaling measurements](SCALING.md)
cover cold replay and catalog creation with up to 16384 location claims; the constructor now uses
temporary indexed membership while preserving candidate order, path comparison and duplicate budgets.
Larger production histories remain in the [roadmap](ROADMAP.md).
[Separate archive maintenance](ARCHIVE-MAINTENANCE.md) handles explicit review/cleanup of interrupted
temporary writes; cold retrieval itself continues to preserve every candidate file.

Sources: [local catalog](../WWCP_POI/History/RoamingNetworkColdArchiveCatalog.cs),
[retrieval](../WWCP_POI/History/RoamingNetworkHistory.ColdArchives.cs),
[structured outcomes](../WWCP_POI/History/RoamingNetworkColdArchiveResult.cs).
