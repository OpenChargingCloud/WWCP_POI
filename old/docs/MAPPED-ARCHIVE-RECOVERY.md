# Mapped file recovery and bounded bootstrap input

[Overview](../README.md) · [Borrowed inputs](ARCHIVE-INPUT-STREAMS.md) · [Limits](ARCHIVE-LIMITS.md) · [Bootstrap](BOOTSTRAP.md) · [Cold archives](COLD-ARCHIVES.md)

Persistent reopening, both cold archive APIs and bootstrap preview/activation now use the
scoped input reader. Existing archive files no longer require a complete managed input array.
Bootstrap assembles verified chunks in bounded private capture and maps the owned temporary
file once the local memory threshold is exceeded. Wire profiles, canonical bytes, IDs, ETags,
original equal peer envelopes, retained branches and fresh recovered runtime remain unchanged.

## Existing archive files

`Open`, `ReadColdArchive` and `TryReadColdArchive` open each existing file with read access
and `FileShare.Read`. The opened file's 64-bit length is checked against local archive limits
before creating a read-only mapping. Oversized files report their **known full length**, including
lengths above `Int32.MaxValue`; they do not use the maximum+1 observation of an unknown stream.
Empty files reach the ordinary empty-input parser failure without creating a mapping.

The internal `POIArchiveMappedFile` owns the existing file, mapped view and pointer acquisition.
Its span is used synchronously within the owner's scope. It rejects access after disposal;
decoded models, histories and trust callbacks retain no pointer/view. The same owner can borrow
a spool's file handle, releasing its view while leaving file deletion to the capture owner.

`Open` acquires the persistent writer lease first, replays all retained branches, then closes
the data file/view **before** attaching that lease to the returned history. Publication can
immediately replace its archive. Failure or recovery cancellation disposes the private history
and releases the lease without modifying the source file.

Cold reading uses no persistent writer lease. The independently expected full archive digest
is checked before syntax/model/trust replay, using the same opened file and mapped view throughout.
Catalog candidates retain their registration order and structured diagnostics. Missing,
unreadable, excessive or digest-mismatched candidates can be skipped. The first digest match
must pass current trust and receipt membership; verification failure never selects a later copy.
Source files are closed before returning the separate recovered history.

Read sharing excludes conflicting writers on Windows, including a write attempted from a trust
callback. Cooperating archive writers must continue to honor their archive leases. Mapping is
not an immutable disk snapshot against arbitrary external filesystem writers; these tests cover
Windows file sharing and the existing application lease protocol.

## Manifest-bound bootstrap capture

`TryActivate` still requires the expected manifest identity, current manifest authorization,
an ordered verified staging prefix, exact chunk lengths/digests and the full archive digest.
It scans staging before assembling chunks in order through `POIArchiveInputSpool`. Individual
chunk arrays remain bounded by the manifest and local chunk limits. Capture memory capacity
is bounded by `RoamingNetworkArchiveReadOptions.MemoryThresholdBytes` and the archive byte limit;
larger input spills into its own delete-on-close file under a shared maintenance reader lease.

The full captured input then undergoes indexed syntax/depth/duplicate/EOF validation,
deterministic CBOR checks, actual container budgets, manifest profile/root/head/count binding,
original peer verification and every retained state transition. No commit trust is cached from
an earlier preview. Signed snapshot boundaries still require explicit current authorization.

For a new persistent destination, all input views, spool files and temporary reader leases are
closed before durable installation starts. The existing archive writer lease, flushed sibling
temporary file and atomic replacement contract remain in force. Capture cleanup touches only
the current call's file; staged chunks, existing orphans and coordination locks remain available.

With explicit `recoverExistingArchive: true`, the existing destination is independently mapped
under its writer lease and compared **byte for byte** with the manifest-bound captured input.
It is restored again with fresh supplied trust, its data mapping closes, and only then is its
writer lease transferred to the recovered history. Its original bytes are never replaced by
recovery. The outer capture also closes before returning. Lost acknowledgement recovery and
retry therefore preserve the prior contract.

```csharp
using var history = RoamingNetworkHistory.Open(path,
    verifyBatchSignature: VerifyBatch,
    verifyCommitSignature: VerifyCommit,
    authorizeSnapshotBoundary: AuthorizeBoundary,
    cancellationToken: cancellationToken);

receiver.TryActivate(manifest.Id, out var replica, out var result,
    activate: true, archivePath: newArchivePath,
    verifyBatchSignature: VerifyBatch,
    verifyCommitSignature: VerifyCommit,
    authorizeBootstrap: AuthorizeManifest,
    authorizeSnapshotBoundary: AuthorizeBoundary,
    readOptions: new RoamingNetworkArchiveReadOptions(
        memoryThresholdBytes: 1024 * 1024,
        temporaryDirectory: existingSpoolDirectory),
    cancellationToken: cancellationToken);
```

Read options and cancellation are local policy; they are not part of manifests, history bytes,
signatures or static ETags. Temporary-directory rules and explicit orphan maintenance follow
[the borrowed-input contract](ARCHIVE-INPUT-STREAMS.md).

## Cancellation and publication

The file APIs add an optional trailing `CancellationToken`. `TryActivate` adds optional
`readOptions` and `cancellationToken`. Existing source calls keep their defaults. These additions
change CLR method signatures; applications using precompiled callers must rebuild those callers. `Open` and
`ReadColdArchive` throw `OperationCanceledException` with the read token. Catalog cold recovery
and bootstrap return `Cancelled`, no history and their available candidate/progress diagnostics.

Checks occur before recovery, after manifest/receipt authorization, between scanned/captured
chunks, before hashing/replay handoff, between commits and around commit/batch/boundary callbacks.
The subsequent [parser-cancellation package](PARSER-CANCELLATION.md) checks inside owned syntax/
limit loops and around individual commit/receipt/model calls and root/replay/head preparation.
Individual Styx scalar/structured-key/skip, model/crypto/state calls, bulk copies and synchronous
disk operations remain synchronous; this is not a bound on cancellation latency. A returned
history retains the original trust policies with the recovery token cleared, so a later token
cancellation does not reject new commits.

Bootstrap checks cancellation immediately before installation. Once durable installation starts,
its existing publication contract completes; a token cancelled during the write does not hide a
successfully activated archive behind a cancellation result. I/O failure and process interruption
continue to use the existing error/lost-acknowledgement recovery rules.

## Verification and remaining costs

`MappedArchiveRecoveryTests` covers all four archive profiles, exact known-length limits, malformed
and empty input, original peers, fresh runtime, handle release before subsequent publication,
memory/file capture modes, preview/new activation/existing recovery, cancellation and retry,
capture I/O failure, untouched staging/orphans, Windows sharing and named child-process exits.
Existing limit, cold-candidate, canonical bootstrap, atomic publication and crash fixtures are rerun.
The 157 new cases, all 947 focused cases and all 2,186 full cases pass on 2026-10-09, with zero
failures and one ordinary child worker skipped. Both explicit reference generators are excluded;
fixed JSON/CBOR/signature references are unchanged. Twelve child exits cover the three routes
across all four profiles; existing receipt/activation/publication exit fixtures pass again.
Execution results and file/assembly bindings are recorded in
[mapped-recovery-results.json](verification/mapped-recovery-results.json).

Mapping removes complete managed input copies for existing files. Bootstrap may still retain a
small complete input within its selected memory threshold. Neither route removes mapped pages,
OS cache, individual parser trees/scalars, frozen boundary suffix bytes, retained branch states,
runtime reconstruction or repeated model/replay preparation. Input byte limits do not bound total
memory use. The completed 44-worker / 132-sample series binds every input and result: same-build
array/mapped-file controls across seven shapes/all four profiles, plus paired preserved-before/new
cold and bootstrap workloads at 128/512 EVSEs. The large bootstrap input is 1,609,250 bytes and
exercises default file spill; the 444,625-byte smaller input stays within the one-MiB threshold.
At 512 EVSEs file-control allocation changes 1689.30 -> 1688.66 MiB (-0.038%); graph-128 rises
0.031% and pruned-4 rises 0.045%. Bootstrap changes +0.068% / -0.008% at 128/512 EVSEs; cold
changes -0.004% / -0.063%. Model/replay costs dominate and no universal improvement follows.
[Measurements](performance/mapped-recovery-summary.md) distinguish operation-thread
managed allocations from sampled managed/process peaks and complete input disk usage.

The subsequent [model preparation package](MODEL-PREPARATION.md) removes repeated copies of
already private descendants in model import, completion and representation binding, using the
existing recovery stages and exact static/signature references. Identity/signature preparation
and root/head reconstruction were further candidates at that stage. Subsequent packages cover them
and [owned parser-loop cancellation](PARSER-CANCELLATION.md). Finer checks within individual parsers,
larger reference/tariff/parking workloads and production concurrency remain separate follow-ups.

[Scoped canonical preparation](CANONICAL-PREPARATION.md) now reuses unsigned bytes during
synchronous recovery/peer loops with bounded admission and complete release at return/failure.
Original identity/signature profiles and every key/verifier/authority callback remain exact;
no trust result or key is cached. Direct individual signing-byte calls use their preceding
pipeline. Public arrays stay detached, and runtime remains separate from signed static content.
