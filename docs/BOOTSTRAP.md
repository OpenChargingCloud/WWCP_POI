# Bootstrapping a new replica

[Repository overview](../README.md) · [History](HISTORY.md) · [Incremental replication](REPLICATION.md) · [Trust](SIGNATURES.md)

Ordinary bootstrap supplies the checkpoint snapshot and complete retained commit DAG to a new replica.
It is the initial "clone" step before incremental exchange. An existing replica with a different
checkpoint can use the same workflow to prepare a **separate** history for an explicit application
switch. Staging and validation never replace its current history or runtime data.
`CreateSnapshotBootstrap(snapshotId, targetTip, ...)` can instead freeze an original signed snapshot
and a suffix whose every parent path terminates there. Activation requires explicit signature and
checkpoint/anchor policies. See [authorized snapshot entry](SNAPSHOT-BOUNDARIES.md).

## Frozen source and local bounds

`history.CreateBootstrap(chunkBytes, limits)` captures the deterministic static CBOR archive under
the history gate. It includes the original checkpoint, published head, every retained branch,
complete ChangeSets and both peer signature arrays. Later publication or added signatures at the
live sender cannot change this source session. Runtime statuses, measurements and forecasts are absent.
The encoder writes directly into private bounded fragments, hashing the archive and fragments as
they arrive. Raw byte/count budgets are checked during capture. The source retains the total
frozen payload across these fragments. POI snapshot payloads [preflight and emit directly](DIRECT-ARCHIVE-PAYLOAD.md),
as do [ChangeSets](DIRECT-ARCHIVE-CHANGESET.md); prepared JSON/index/schema paths remain.
`source.WriteArchive(destination)` emits the exact frozen CBOR archive to a borrowed writable,
non-seekable stream without closing/flushing it. Failed output can leave partial bytes.
See [streaming contracts](STREAMING-ARCHIVES.md).

`RoamingNetworkBootstrapSource.Manifest` fixes the archive identity and chunk layout.
`CreateChunk(index)` returns a detached immutable fragment. Fragments may split CBOR tokens;
they are reassembled before interpreting the archive. This permits a large checkpoint or single
large commit to travel through bounded messages without changing its identity or signatures.

`RoamingNetworkBootstrapLimits` has positive, receiver-controlled limits:

| Property | Default | Scope |
| --- | --- | --- |
| `MaxArchiveBytes` | 256 MiB | Complete raw CBOR archive |
| `MaxChunkBytes` | 1 MiB | Raw fragment payload |
| `MaxChunks` | 4,096 | Ordered fragment digests/receipts |
| `MaxCommits` | 100,000 | Retained commits including checkpoint or snapshot root |
| `MaxRetentionReceipts` | 4,096 | Pruning receipts in the actual archive |
| `MaxCatalogCommitIds` | 1,000,000 | All `PrunedCommits` and `ArchiveOnlyTips` entries, summed across receipts |
| `MaxWireBytes` | 2 MiB | Encoded JSON or CBOR fragment |
| `MaxManifestBytes` | 512 KiB | Encoded JSON or CBOR manifest |

The default source fragment size is 64 KiB. Source generation and typed receiver import check
both wire representations, including all envelope bytes. Parsers check raw encoded byte lengths
before building a document and count/payload limits before constructing domain envelopes. JSON
byte limits count UTF-8 bytes. The manifest cannot override receiver limits. Its commit count is
checked against the actual archive before constructing or replaying commits.
`HistoryLimits` exposes the common [archive recovery budgets](ARCHIVE-LIMITS.md). A streaming
preflight checks actual commit/receipt/ID containers before CBOR document materialization. Catalog
IDs consume the budget per occurrence, including repeated IDs across arrays and pruning events.
These are receiver settings and do not enter or change manifest identities or wire profiles.

## Wire contracts and identities

Both manifest and fragment parsers reject unknown fields, duplicate fields, unsupported profiles
and trailing input. JSON Base64 requires standard canonical padding, without embedded whitespace.
All ETags and commit IDs use the existing structured tuples; native CBOR stores binary digests.

Manifest JSON has these exact fields:

| Field | Meaning |
| --- | --- |
| `Id` | JSON SHA-256 ETag of canonical manifest JSON excluding this derived field |
| `Profile` | Manifest version 1/2 for complete histories, version 3 for a snapshot boundary, version 4 when pruning receipts are included |
| `ContentProfile` | `wwcp-poi-static-v2` |
| `ArchiveProfile` | `wwcp-poi-history-v1`/`v2` for complete history; `v3` for a boundary suffix; `v4` adds its pruning receipts |
| `Checkpoint`, `Head` | Original typed checkpoint and published head IDs |
| `Anchor` | Versions 3/4 only: original signed snapshot replay root |
| `CommitCount` | Included commits plus the checkpoint or snapshot root |
| `ArchiveBytes`, `ChunkBytes` | Complete byte length and fixed fragment size, except the final tail |
| `ArchiveETag` | CBOR SHA-256 ETag of the complete deterministic CBOR archive |
| `DigestAlgorithm` | `sha256`, for all raw fragment digests |
| `ChunkDigestEncoding` | `base64`, JSON only |
| `ChunkDigests` | Ordered array of 32-byte fragment digests, Base64 in JSON |

Native CBOR uses the same fields except `ChunkDigestEncoding`; `ChunkDigests` is an array of byte
strings. Decoding either representation computes the same manifest `Id` from its prescribed JSON
preimage and checks the declared identity. The identity preimage uses the generated JSON shape:
HEX tuples for identifiers, canonical padded Base64 fragment digests, and no `Id` field. Alternative
accepted ETag text encodings in a transport normalize to that shape before computing the identity.
Fragment hashes cover raw slices and therefore have no
`json`/`cbor` format label: a slice is not necessarily a complete representation.

Fragment JSON fields are `Profile: "wwcp-poi-bootstrap-chunk-v1"`, required `ContentProfile`, typed
`Manifest`, nonnegative `Index`, `DataEncoding: "base64"` and `Data`. Native CBOR omits `DataEncoding`
and uses a byte string for `Data`. Position, exact expected length and SHA-256 bytes are verified
against the manifest when importing.

These identities have separate meanings:

- POI state ETags describe static content and exclude runtime/revision/derived metadata.
- Commit IDs bind that content to ancestry and unsigned ChangeSet metadata/operations.
- The archive ETag also binds the captured peer envelopes and mutable archive head reference.
- The manifest ID additionally binds fragment layout, count and transfer profiles.

Bootstrap uses the current [static content profile](INTEROPERABILITY.md) inside its existing
ChangeSet/commit/signature/archive envelopes.

Full [snapshot links](SNAPSHOTS.md) select `wwcp-poi-history-v2` and
`wwcp-poi-bootstrap-manifest-v2`. `Manifest.ArchiveProfile` and `WireProfile` expose this pair;
decoding and final replay reject an inconsistent pairing. The archive still contains its original
checkpoint and complete retained ancestry, including every snapshot branch. Snapshot-only links
must reproduce their first parent's static state and advance revision while retaining its last batch ID.
Fragment envelopes retain version 1 because they already bind arbitrary archive slices to a manifest.
Activation continues to initialize fresh local runtime. Version 3 additionally supports a trusted
snapshot with omitted earlier history and binds its explicit `Anchor`. `TryActivate` requires
`authorizeSnapshotBoundary` and `verifyCommitSignature` for versions 3/4, including validation previews.
The boundary policy must authorize the chain/anchor pair and rollback constraints independently.
Version 4 binds history-v4 and also retains the [pruning receipt catalog](RETENTION.md). The complete
archive digest covers these unsigned records, whose trust depends on archive/manifest provenance.
`CommitCount` counts retained commits including the root, not recorded removed IDs. `MaxArchiveBytes`
bounds catalog input bytes; the separate receipt/aggregate-ID limits bound catalog entries before
decoding/replay. Creating a snapshot export does not record excluded branches as pruned.
SnapshotBoundaryTests and RetentionTests now exercise partial resumable transfer, fresh authorization,
v4 catalogs and exclusions without invented pruning receipts. Fixed complete/boundary/pruned archive
and manifest references are available; see [the verification report](VERIFICATION-SNAPSHOTS-RETENTION.md).

## Staging and restart

`RoamingNetworkBootstrapReceiver.Create(directory, manifest, limits)` requires a dedicated empty
directory and obtains an exclusive writer lease. Its `manifest.cbor` records the transfer.
Each accepted fragment is written to a unique temporary sibling, flushed with `Flush(true)` and
renamed without overwriting an existing receipt. Only then does `NextChunk` advance. A failed
write leaves progress unchanged and cleans up that attempt's temporary file.

A process exit after receipt installation can lose its acknowledgement. Reopening recognizes the
installed manifest/chunk after verification; a resent chunk returns `AlreadyStored`. An exit before
manifest installation leaves no resumable transfer. Preserve the orphan for review and explicitly use
a new empty staging directory, or [review and clean](ARCHIVE-MAINTENANCE.md) its temporary files before
initializing the same folder again. See [the process-crash contract and evidence](BOOTSTRAP-CRASH-RECOVERY.md).

Delivery is ordered: send `NextChunk`. Future fragments return `OutOfOrder`; a verified identical
duplicate returns `AlreadyStored` without advancing progress. There is no hidden pending queue.

Disposing releases the lease and retains the manifest/receipts. Reopen with
`Open(directory, expectedManifestId, limits)`. The independently retained expected identity must
match. Open rechecks every receipt's exact length and digest and requires an ordered prefix;
corruption and gaps are rejected. Temporary leftovers from interrupted writes do not count as
receipts and are preserved. The receiver performs no recursive directory cleanup.
Separate [archive maintenance](ARCHIVE-MAINTENANCE.md) inventories recognized temporary siblings
under the staging lease and permits explicit cleanup after checking a fixed review. Installed
manifest/chunk files are protected and verified progress remains available for normal `Open`.

A resumed request identifies **both** `Manifest.Id` and `NextChunk`. The sender must retain the
original source session or an identical frozen archive. A freshly captured archive with a changed
head, peer array or fragment layout belongs to a different manifest and is rejected by that session.

## Validation and explicit activation

`TryActivate(expectedManifestId, out history, out result)` defaults to a validation preview.
`activate: true` returns a new history only after all checks succeed. A preview returns no history
and creates no target archive, even when an `archivePath` is supplied.

Each attempt checks:

1. Expected manifest identity and optional `authorizeBootstrap` application policy.
2. Complete ordered receipts, exact lengths, fragment digests and whole archive digest.
3. Deterministic CBOR encoding and supported archive/content profiles.
4. Actual commit count, checkpoint and head against the manifest.
5. Original checkpoint/state/commit identities, every supplied batch and commit signature,
   whole-commit authorization, all parents, revision, operation preconditions and both state ETags.
6. Replay of **every** retained branch, including branches outside the selected head's ancestry.

Verifier and authorization callbacks are supplied afresh on each preview/activation. An earlier
successful preview does not authorize a later attempt after revocation. Unsigned envelopes remain
allowed unless whole-commit authorization requires signatures/quorum; supplied signatures always
need explicit verifiers. Reentrant mutation/activation and disposal during callbacks are rejected.

Hashes detect corruption; they do not authenticate a self-consistent replacement manifest.
Select/authenticate the manifest through the application's channel or expected identity.
`authorizeBootstrap` can pin its expected checkpoint/head/archive and enforce rollback policy.
Individual commit signatures do not select the archive head on the application's behalf.

An optional `archivePath` persists the validated history with the existing exclusive writer lease
and atomic archive replacement mechanism. By default it must be a **new** path; activation refuses
an existing archive, including when no writer currently holds it. Persistence failure returns no history.
The staging receipts remain available for retry. The caller owns/disposes the returned history
and explicitly chooses when to use it as its active replica.

For a lost activation acknowledgement, explicitly set `recoverExistingArchive: true`, together with
`activate: true` and `archivePath`. An absent destination is installed normally. An existing destination
must match the entire manifest-bound archive byte-for-byte and pass replay with fresh current policies
under its exclusive writer lease. Success returns `ActivationRecovered` and a persistent history;
no existing bytes are rewritten. Newer valid heads, changed peers and alternate CBOR encodings fail as
`InvalidData`; unavailable leases fail as `PersistenceFailure`. Matching state ETags alone is insufficient.
See [the recovery example, exit stages and practical limits](BOOTSTRAP-CRASH-RECOVERY.md).

Original commit IDs, revisions, applied batch IDs, ordered parents and equal peer arrays survive
activation. Runtime starts from local domain defaults, without foreign schedules, measurements or
forecasts. After application selection, use incremental packs and expected-head adoption normally.

## Example

The application has selected the sender/manifest and configured current signature/trust callbacks:

```csharp
var limits = new RoamingNetworkBootstrapLimits();
var source = sender.CreateBootstrap(chunkBytes: 64 * 1024, limits: limits);
var manifest = RoamingNetworkBootstrapManifest.ParseCBOR(source.Manifest.ToCBOR(), limits);

// Persist this identity independently for resumed requests and activation.
var expectedManifest = manifest.Id;
using var receiver = RoamingNetworkBootstrapReceiver.Create(stagingDirectory, manifest, limits);
while (receiver.NextChunk < manifest.ChunkCount)
{
    var chunk = RoamingNetworkBootstrapChunk.ParseCBOR(source.CreateChunk(receiver.NextChunk).ToCBOR(), limits);
    if (!receiver.TryAcceptChunk(chunk, out var receipt))
        throw new InvalidOperationException(receipt.Error);
}

if (!receiver.TryActivate(expectedManifest, out _, out var preview,
    verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit,
    authorizeCommit: AuthorizeCommit, authorizeBootstrap: AuthorizeBootstrap))
    throw new InvalidOperationException(preview.Error);

if (!receiver.TryActivate(expectedManifest, out var replica, out var activation,
    activate: true, archivePath: newArchivePath,
    verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit,
    authorizeCommit: AuthorizeCommit, authorizeBootstrap: AuthorizeBootstrap))
    throw new InvalidOperationException(activation.Error);
// The application explicitly selects replica, and disposes it when finished.
```

After interruption, use `Open(stagingDirectory, expectedManifest, limits)` and request its next
fragment from the same source session. JSON codecs are interchangeable with the CBOR codecs.

## Outcomes and evidence

`Accepted`/`AlreadyStored` acknowledge fragment receipts. `ActivationAvailable` is a validated
preview; `Activated` returns a newly activated history, and `ActivationRecovered` returns an explicitly
reopened exact destination. Failures distinguish `Incomplete`, `OutOfOrder`,
`ManifestMismatch`, `InvalidData`, `PersistenceFailure`, `Unavailable`, `LimitExceeded` and recovery
`Cancelled`. Final archive
limit rejection supplies structured `LimitViolation` with `Kind`, `Maximum` and `Observed`; completed
staging and progress remain unchanged. No failure returns an
activated history or mutates a preexisting replica.

Sources: [manifest/limits](../WWCP_POI/History/RoamingNetworkBootstrapManifest.cs),
[fragments/source](../WWCP_POI/History/RoamingNetworkBootstrapChunk.cs),
[receiver](../WWCP_POI/History/RoamingNetworkBootstrapReceiver.cs),
[history integration](../WWCP_POI/History/RoamingNetworkHistory.Bootstrap.cs).

[BootstrapTests](../WWCP_POI_Tests/Interoperability/BootstrapTests.cs) has 32 passing cases covering
both wire formats, frozen multi-page transfer/reopen/duplicate delivery, another checkpoint,
preview/activation, original signed branches, fresh runtime and incremental continuation. Further
cases check exact raw/wire/count limits, failed writes before/after flushing, orphan writes, leases,
disk corruption/gaps, whole archive/checkpoint/head/count/profile/identity/signature failures,
replay preconditions/result ETags/batch-ID reuse, revoked unpublished branches, callback reentry,
existing target archives, failed persistence paths and retry. Those executions predate static-v2; current reference vectors were subsequently regenerated.
The [archive-limit fixture](ARCHIVE-LIMITS.md#executed-evidence-and-practical-limits) adds 60 passing
cases, including actual counts exceeding manifest claims, bounded catalogs, exact-budget resumed
activation and unchanged staging/destination/runtime on rejection. The [bootstrap crash fixture](BOOTSTRAP-CRASH-RECOVERY.md)
adds 89 cases, including 36 real process exits across all four profiles and explicit existing-destination
recovery with fresh trust. The domain-model full run passed 1,025 tests before the streaming
changes; see [the recorded execution](VERIFICATION-DOMAIN-MODEL.md). The later
[streaming verification](VERIFICATION-STREAMING.md) adds 54 bootstrap and 95 archive/encoding cases
and reruns the full suite: 1,174 passed, zero failures, one skipped worker. Existing bootstrap crash
fixtures pass against the new encoder. Performance measurements remain separate evidence.

Transfer and per-receipt validation are bounded; source encoding now writes directly into frozen
fragments without a complete contiguous archive buffer. Their total payload is still retained.
Prepared POI/ChangeSet JSON/index/schema paths, final archive decoding/replay and retained
static states still allocate in memory. Total allocation, replay cost and snapshot memory are not bounded by fragment
size. `MaxArchiveBytes` bounds input bytes rather than all allocations. Staging holds accepted
bytes plus manifest and transient write files; orphan files from previous crashes can be
[reviewed and explicitly cleaned](ARCHIVE-MAINTENANCE.md). The general-history
[borrowed input API](ARCHIVE-INPUT-STREAMS.md) maps captured files.
[Bootstrap now uses bounded capture](MAPPED-ARCHIVE-RECOVERY.md) for manifest-bound validation/activation,
closing input views before durable installation and supporting explicit recovery cancellation. A durable sender session service,
network/key negotiation, automatic old-history switch and power-loss proof remain future work. See [streaming contracts](STREAMING-ARCHIVES.md) and [executed verification](VERIFICATION-STREAMING.md).
Separate [explicit retention](RETENTION.md) now archives and prunes existing histories; bootstrap
export itself continues to retain the source.
The tests inject failures and terminate child processes at named receipt/activation write points.
Power loss and interruption inside individual writes/renames remain outside the evidence.
See [process-crash recovery](BOOTSTRAP-CRASH-RECOVERY.md) and the [roadmap](ROADMAP.md).

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

### Subsequent cooperative parser cancellation

[Parser cancellation](PARSER-CANCELLATION.md) passes the existing recovery token explicitly through
owned syntax/budget/canonical loops and around each commit/receipt/model/root/replay/head step.
There is no ambient token or retained parser context. Exact preceding diagnostics, eager/lazy
trust timing, input ownership, scope release, late token clearing and atomic installation remain.
Individual Styx/model/crypto calls and copies remain synchronous; no cancellation latency bound
is claimed. It adds 270 cases; all 2,507 interoperability / 2,839 full Release tests pass with
unchanged fixed references. Matched default/active-token successful reads use 72 workers/216 samples,
the same new harness/dependencies and exact prepared inputs/results. Earlier measurements remain
historical. Broader shared-reference/tariff/parking workloads are measured in the subsequent package below.

### Subsequent domain recovery baseline

[Shared-reference, tariff and parking workloads](DOMAIN-RECOVERY-WORKLOADS.md) add eight
deterministic 16/64-EVSE shapes with all four profiles, shared catalogs, meters, nested values,
targeted membership edits and one signed two-parent merge. Model/restore/full CBOR/JSON stages
use 64 fresh workers/192 samples with exact input/head/branch/peer and independent-runtime checks.
Production/dependency/reference bytes remain unchanged; six old simple-workload controls still
match. The new baseline measures richer data and is separate from historical performance reports.
81 new cases and all 2,588 interoperability / 2,920 full Release tests pass, including current
bootstrap trust rejection/retry and atomic invalid reference/value/additional-peer edits.
