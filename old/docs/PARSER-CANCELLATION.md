# Cooperative cancellation while parsing archives

[Repository](../README.md) · [Input streams](ARCHIVE-INPUT-STREAMS.md) · [Mapped recovery](MAPPED-ARCHIVE-RECOVERY.md)

Existing recovery tokens now reach the CBOR limit scan, full syntax index, bootstrap canonical
validation, commit/receipt model loading, private suffix capture and root/replay/head preparation.
The JSON and borrowed-span APIs retain their existing signatures and default uncancellable behavior.
Stream/async-stream, persistent/cold file recovery and bootstrap use the same internal parser paths.
The wire profiles, static ETags, commit IDs, metadata and all peer signatures remain exact.

## Checkpoints and lifetime

`POIArchiveReadProgress` is an internal readonly per-call value. It carries a token and an optional
internal test observer through explicit method arguments and the temporary suffix cursor. There is
no ambient token and no retained history field. The observer is unavailable through public options.
Production reads use no observer; an uncancellable/default context returns immediately at each check.

| Processing | Cooperative checks |
| --- | --- |
| Local CBOR budgets | Before scanning, between archive/receipt fields and counted items, around the existing Styx `SkipValue` call, after the scan |
| Syntax index and bootstrap canonical validation | At each owned recursive value boundary, between map fields, before structured-key reads and after scalar/container reads |
| Immutable models | Before and after each complete commit/receipt/root-state parse |
| Boundary suffix | Before and after freezing private suffix bytes, then around each lazily decoded commit |
| Private history | Around root reconstruction, between replayed commits and around head reconstruction/receipt indexing |
| Current trust | Existing checks before and after every supplied batch/commit/boundary policy callback remain |

Checks throw `OperationCanceledException` with the read token. The existing outer input adapter
also normalizes wrapped failures when that token is cancelled. Without cancellation, preceding
diagnostics and their ordering remain exact: the reader uses the same Styx primitives, duplicate
equality, depth/count/EOF checks and eager or lazy model timing. The bootstrap canonical reader
retains its original deterministic-encoding rules and error wrapper.

Complete v1/v2 archives still build suffix models before trust. Boundary v3/v4 archives still
freeze private suffix bytes before authorization and decode suffix models during replay after
root authorization. Adding checks does not permit trust before full syntax validation.

Failure disposes the private history, input views, owned capture file and leases. Every outer
canonical-preparation scope releases its entries. Input bytes and unrelated temporary files are
preserved. Existing application callback side effects remain the application's responsibility.
The returned history's trust delegates have the read token cleared; later cancellation cannot
poison a subsequent publication. Bootstrap still checks immediately before installation; once
durable installation begins, its existing atomic publication contract completes.

## Cooperative limits

Individual Styx `SkipValue`, scalar and structured-key readers, immutable model parsing,
canonicalization, state application, cryptography, synchronous disk calls and bulk copies remain
synchronous. This package checks around them; it cannot interrupt their internals. The owned
index traversal checks within nested arrays/maps/tags, but one large scalar or structured key
can still delay cancellation. Async capture does not make parsing/replay asynchronous.

These checkpoints establish no maximum bytes or milliseconds between cancellation and return.
More granular model or Styx cancellation would require separate parser work and exact diagnostic
comparisons. Local archive byte/count/depth limits continue to bound rejection budgets; they
provide no constant total-memory or production-throughput guarantee.

## Verification

`ParserCancellationTests` adds **270 cases**:

- 116 deterministic phase cancellations across four profiles, ordinary/bootstrap recovery and
  private-memory/mapped-spool input; exact token, bytes, trust order, cleanup and successful retry.
- 16 cancellations after loading a suffix commit, preserving eager/lazy trust timing.
- Eight successful active-token reads followed by late cancellation and a fresh signed publication.
- Four comparisons of indexed ranges, values, canonical encoding and limits with frozen prior readers.
- 102 syntax/depth/duplicate/UTF-8/EOF/canonical diagnostic comparisons with default and active tokens.
- 12 exact budget comparisons including definite/indefinite receipt/catalog containers and exact limits.
- Eight ordinary observer failures checking unchanged exceptions, private-state/input release and retry.
- Three pre-cancelled scans and one nested/concurrent-read test proving no ambient token or observer.

The observer cancels synchronously at a selected stage/offset. Tests exercise the real internal
restore/input-spool adapter; they add no public instrumentation and make no timing-dependent
cancellation claim. Existing full-suite stream/async/file/cold/bootstrap tests cover public API
integration, candidate outcomes, durable activation, crash recovery and source/staging ownership.
Frozen preceding syntax/limit algorithms compare exact errors; actual model/trust contracts and
the **32 unchanged fixed reference files** remain covered by the full suites.

On **2026-10-09 UTC**, all **2,507 interoperability / 2,839 full Release cases pass**, with zero
failures and one ordinary process worker skipped. Both explicit reference generators are excluded.
[Execution evidence](verification/parser-cancellation-results.json) records source/assembly hashes,
TRX results, references, scoped reverse-patch reconstruction and measurement bindings.

## Successful-read overhead measurements

[The comparison](performance/parser-cancellation-summary.md) contains **72 fresh workers / 216
measured calls**, six fixed shapes and all four profiles. Each group/side has two sequential
processes, three warmups and three samples. The same new C# harness binary and all shared dependency
bytes run against preserved preceding and newly built POI libraries. No task build/test overlaps
the formal series. Prepared inputs also match [preceding controls](performance/mapped-file-controls.json).

`read-cbor-limits` and ordinary `read-cbor` measure default-token controls. The new
`read-cbor-input-token` measures successful complete borrowed-stream recovery with a cancellable
token that is never cancelled. Before already implements stream/trust token checks; after adds
owned parser/model/replay checkpoints. The operation includes capture and existing trust wrappers.
It does not isolate a single check or measure cancellation latency. Every input/output byte,
head, branch state, original peer and fresh local runtime is checked outside the measured call.

Raw [before](performance/parser-cancellation-before.json) and [after](performance/parser-cancellation-after.json)
reports retain allocation, time, CPU and sampled peaks. Shared-desktop times and collected additional
history memory are descriptive. Individual parser/model/replay costs and retained snapshots remain.
Successful active-token recovery allocation changes between -0.1412% and +0.1670% across these
six shapes. Timings are mixed: the largest batch's ordinary span median rises 2,235.221 ->
5,019.935 ms; active-token input rises 2,193.985 -> 3,035.391 ms. These increases remain in
the evidence; the comparison does not isolate their cause or guarantee a universal check overhead.
The [production patch](performance/parser-cancellation.diff) and separate
[before](verification/parser-cancellation-before-binding.json) /
[after](verification/parser-cancellation-after-binding.json) bindings reconstruct the compared source
without discarding surrounding changes. Reverse the patch only in a separate copy of the recorded
source state; build/preserve the new harness against that preceding library, then replace only the
POI library/PDB in its matched after copy. Historical later builds need their own new bindings.

## Reproduction

Run from the repository; preserve original evidence and use fresh result names:

```powershell
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~ParserCancellationTests' --logger 'trx;LogFileName=parser-cancellation-rerun.trx' --results-directory WWCP_POI_Tests/bin/TestResults/parser-cancellation-rerun
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName!~GenerateReferenceVectors&FullyQualifiedName!~GenerateSnapshotRetentionVectors' --logger 'trx;LogFileName=parser-cancellation-full-rerun.trx' --results-directory WWCP_POI_Tests/bin/TestResults/parser-cancellation-rerun
python -B WWCP_POI_Benchmarks/parser_cancellation_measure.py --binary-directory path/to/preserved-before --binding docs/verification/parser-cancellation-before-binding.json --label parser-before-rerun --output WWCP_POI_Benchmarks/bin/parser-before-rerun.json
python -B WWCP_POI_Benchmarks/parser_cancellation_measure.py --binary-directory path/to/matched-after --binding docs/verification/parser-cancellation-after-binding.json --label parser-after-rerun --output WWCP_POI_Benchmarks/bin/parser-after-rerun.json
python -B WWCP_POI_Benchmarks/parser_cancellation_report.py --before docs/performance/parser-cancellation-before.json --after docs/performance/parser-cancellation-after.json --inputs docs/performance/mapped-file-controls.json --output WWCP_POI_Benchmarks/bin/parser-summary-rerun.md
```

Finish tests/builds before each formal measurement series. The launcher checks bound binaries and
refuses output overwrite. The summary rejects incomplete groups, changed shape parameters,
inventories, repeated results, input/head identities, dependencies/harness/method and retention counts.

## Next work

The subsequent [domain recovery package](DOMAIN-RECOVERY-WORKLOADS.md) now measures shared
grid/software/document references, nested tariffs, parking relations and signed merges across all
four profiles. Production remains unchanged. That richer baseline guides later model/reference/
replay improvements; normalized map reuse is completed below. Finer individual-parser cancellation,
production concurrency and persistent indexes remain separate candidates.

### Subsequent normalized immutable map preparation

[Normalized snapshot maps](SNAPSHOT-MAP-PREPARATION.md) reuse exactly equal properties, child sets,
entity/map branches and root catalogs after the full ordinary parser and normalization. Binding
reads stable typed IDs directly; all reference/current peer checks, exact errors, independent
runtime, canonical content and atomic publication remain. Removed identities/consumers are handled.
53 new cases and all 2,712 interoperability / 3,044 full Release cases pass with unchanged references.
Matched rich recovery measurements keep the exact archives, branches, peers and C# harness/dependencies.
Earlier reports remain historical. Larger group/catalog/history and concurrency workloads are next.
