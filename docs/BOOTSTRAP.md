# Bootstrapping a new replica

[Repository overview](../README.md) · [History](HISTORY.md) · [Incremental replication](REPLICATION.md) · [Trust](SIGNATURES.md)

Bootstrap supplies the checkpoint snapshot and complete retained commit DAG to a new replica.
It is the initial "clone" step before incremental exchange. An existing replica with a different
checkpoint can use the same workflow to prepare a **separate** history for an explicit application
switch. Staging and validation never replace its current history or runtime data.

## Frozen source and local bounds

`history.CreateBootstrap(chunkBytes, limits)` captures the deterministic static CBOR archive under
the history gate. It includes the original checkpoint, published head, every retained branch,
complete ChangeSets and both peer signature arrays. Later publication or added signatures at the
live sender cannot change this source session. Runtime statuses, measurements and forecasts are absent.

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
| `MaxCommits` | 100,000 | Retained commits including checkpoint |
| `MaxWireBytes` | 2 MiB | Encoded JSON or CBOR fragment |
| `MaxManifestBytes` | 512 KiB | Encoded JSON or CBOR manifest |

The default source fragment size is 64 KiB. Source generation and typed receiver import check
both wire representations, including all envelope bytes. Parsers check raw encoded byte lengths
before building a document and count/payload limits before constructing domain envelopes. JSON
byte limits count UTF-8 bytes. The manifest cannot override receiver limits. Its commit count is
checked against the actual archive before constructing or replaying commits.

## Wire contracts and identities

Both manifest and fragment parsers reject unknown fields, duplicate fields, unsupported profiles
and trailing input. JSON Base64 requires standard canonical padding, without embedded whitespace.
All ETags and commit IDs use the existing structured tuples; native CBOR stores binary digests.

Manifest JSON has these exact fields:

| Field | Meaning |
| --- | --- |
| `Id` | JSON SHA-256 ETag of canonical manifest JSON excluding this derived field |
| `Profile` | `wwcp-poi-bootstrap-manifest-v1` |
| `ContentProfile` | `wwcp-poi-static-v1` |
| `ArchiveProfile` | `wwcp-poi-history-v1` |
| `Checkpoint`, `Head` | Original typed checkpoint and published head IDs |
| `CommitCount` | Retained commits including checkpoint and unpublished branches |
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

Existing static, ChangeSet, commit, signature and archive profiles/reference bytes are unchanged.

## Staging and restart

`RoamingNetworkBootstrapReceiver.Create(directory, manifest, limits)` requires a dedicated empty
directory and obtains an exclusive writer lease. Its `manifest.cbor` records the transfer.
Each accepted fragment is written to a unique temporary sibling, flushed with `Flush(true)` and
renamed without overwriting an existing receipt. Only then does `NextChunk` advance. A failed
write leaves progress unchanged and cleans up that attempt's temporary file.

Delivery is ordered: send `NextChunk`. Future fragments return `OutOfOrder`; a verified identical
duplicate returns `AlreadyStored` without advancing progress. There is no hidden pending queue.

Disposing releases the lease and retains the manifest/receipts. Reopen with
`Open(directory, expectedManifestId, limits)`. The independently retained expected identity must
match. Open rechecks every receipt's exact length and digest and requires an ordered prefix;
corruption and gaps are rejected. Temporary leftovers from interrupted writes do not count as
receipts and are preserved. The receiver performs no recursive directory cleanup.

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
and atomic archive replacement mechanism. It must be a **new** path; activation refuses an existing
archive, including when no writer currently holds it. Persistence failure returns no history.
The staging receipts remain available for retry. The caller owns/disposes the returned history
and explicitly chooses when to use it as its active replica.

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
preview; `Activated` returns a history. Failures distinguish `Incomplete`, `OutOfOrder`,
`ManifestMismatch`, `InvalidData`, `PersistenceFailure` and `Unavailable`. No failure returns an
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
existing target archives, failed persistence paths and retry. Fixed reference vectors are unchanged.

Transfer and per-receipt validation are bounded; source export and final archive decoding/replay
still materialize a complete archive and all retained static states in memory. Replay cost and
snapshot memory are not bounded by fragment size. `MaxArchiveBytes` bounds input bytes rather than
all allocations. Staging holds accepted bytes plus manifest and transient write files; orphan files
from previous crashes need application cleanup. There is no streaming archive codec, durable sender
session service, network/key negotiation, automatic old-history switch, pruning or power-loss proof.
The tests inject failures and model transfer interruption/reopen; they do not simulate power loss
or arbitrary process exits during bootstrap writes. See the [roadmap](ROADMAP.md).
