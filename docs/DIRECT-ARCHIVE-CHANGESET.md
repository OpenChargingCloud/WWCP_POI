# Direct ChangeSet payloads in CBOR archives

[Repository overview](../README.md) · [Preflight reuse](PREFLIGHT-ALLOCATION.md) · [Roadmap](ROADMAP.md)

## Scope and algorithm

All four archive profiles now emit ChangeSet values into the archive writer after complete
preparation and preflight. The complete per-batch CBOR output buffer and its copy are removed.
POI snapshots already use this approach. Public standalone `ToCBOR()` still returns a complete
byte array and retains its preceding encoder path. No content, signing or archive profile changes.

1. Serialize the immutable batch to UTF-8 JSON and index it with `JsonDocument`.
2. Run the unchanged signing JSON validation, then prepare exact operation/scoped-element
   measurement paths. Duplicate decoded map names, including escaped aliases, retain their
   standalone exception type and priority before output.
3. Preflight the whole payload against both the standalone Styx writer's depth budget and the
   archive reader's remaining `SkipValue` budget. Root `BeforeETags`/`AfterETags` are fully parsed
   and validated as the native JSON/CBOR digest pair. Customer fields with those names remain ordinary.
4. Emit definite arrays and deterministically sorted maps directly through the shared writer.
   The original signed scalar codec preserves exact JSON number tokens and schema SI text.
   Reuse successful scalar results only inside this call, shared between preflight and emission.

The depth rules intentionally remain separate: empty definite containers occupy a writer level
without pushing a reader frame; tagged leaves have their own reader tag-run behavior. A proven
conservative bound avoids leaf buffers away from the boundary. Near a boundary, actual Styx
writer/reader checks determine acceptance. Reader-depth errors are deferred until standalone
validation finishes, preserving error ordering. Every occurrence checks depth anew, including hits.
Only small scalar/tag/digest structures can be buffered during preflight; no complete payload is.

The original codec keeps `1`, `1.0`, `1e0`, `-0` and `0` distinct as required by signing. A
noncanonical number uses the existing lossless tagged-document representation. Canonical schema
SI text becomes native metrological CBOR; noncanonical/invalid SI text stays text exactly as in
the standalone ChangeSet encoder. This transport preflight does not replace domain/application
validation. Ordinary customer strings are never interpreted as schema readings.

## Bounds and remaining costs

| Signed scalar cache admission per ChangeSet payload | Limit |
| --- | ---: |
| Successful distinct values | 64 |
| UTF-16 characters per numeric token or schema string | 256 |
| Total retained input characters | 8,192 |
| CBOR tree nodes per entry, including tags and map keys | 128 |
| Text characters or byte-string bytes per scalar node | 256 |

Keys include JSON value kind and exact numeric token/decoded string. Schema ownership is checked
before every lookup. Successful codec results may be ordinary text; cache presence never authorizes
a field or deeper occurrence. Oversized/capacity values encode normally without admission; admitted
hits remain usable. The cache stores no JSON element, path, destination, runtime or cross-call state.
Separate exports have separate contexts, which become collectible on return/failure.

The existing 64-slot map-name pool retains only maps of at most 128 names and clears references
in `finally`. The writer's encoded-key cache retains at most 128 keys/16,384 bytes per call.
These are logical admission limits, not an exact managed-heap bound. Complete POI/ChangeSet JSON,
JSON indexes, prepared measurement-path sets, sorting arrays, rejected scalar trees, graph
projection and metadata/decoder trees remain. Large scalars can grow the stream buffer.
Public archive `ToCBOR()` retains its complete result; borrowed `WriteCBOR()` does not. Full
archive rewrite, frozen bootstrap fragment totals and retained replay states remain separate costs.

## Failure and persistence contracts

Semantic preflight finishes before the first byte of that payload. An enclosing archive prefix
may already exist, matching the previous buffered-payload path. Destination failure may leave
partial bytes. Borrowed streams are neither closed nor flushed. Exact prefix comparisons and
fresh-output retries cover these rules, including failures within a long signed batch.

Persistent store/publication still writes and flushes a temporary archive before replacement
and state installation. Real archive-depth failures leave disk, head and local runtime unchanged,
remove the exact temporary file, and permit a later valid signed mutation. No runtime status
enters static hashes/signatures or survives recovery as imported remote runtime.

## Verification

Release on **2026-10-09 passes 708 focused and 1,733 full cases**, zero failures and one ordinary
skipped child worker. Both reference generators remain explicit and excluded. Fixed reference
files are not regenerated by this package. The **214 new cases** include:

- 144 independent depth comparisons: buffered standalone writer plus actual Styx `SkipValue`
  versus warmed-cache preflight/direct output at 0/1/4 enclosing levels and JSON depths 59..64.
- Native root ETags through HEX/Base64 JSON, customer lookalikes, all operation kinds and scoped slots.
- Four-profile byte equality, original multiple peer signatures, genuinely applied ordered long
  batches, recovery/signing preimages and fresh runtime.
- Destination failures inside the batch, exact successful/rejected prefixes, real persistent
  store/publication depth failures, atomic disk/head/runtime preservation, cleanup and signed retry.
- Duplicate escaped/nested keys, exact number/string cache identities, unsupported ownership,
  saturation, exact/excess individual and total text budgets, overflow codec bytes and fresh contexts.

The earlier 71 preflight-reuse, 140 direct POI archive, 55 direct ChangeSet, 59 direct POI, 20
child-cache and 149 stream/bootstrap cases all rerun. The full suite includes model/culture,
merge/reference/signature, retention/bootstrap and actual-process crash coverage.
Initial fixture compilation required correcting two helper references. The initial focused run
passed 707 tests and found duplicated numeric test data in one budget assertion; unique prefixes
corrected only the test data. Production required no correction. Final source/assembly/TRX hashes
are bound by [verification evidence](verification/direct-archive-changeset-results.json) and the
[scoped production patch](performance/direct-archive-changeset.diff).

## Matched measurements

Two new paired series use the same expanded C# harness on both production builds. Both before
series finished before production edits; test/build work finished before the after series.
Each report contains 48 fresh workers/240 measured calls: two processes/five warmups/five samples.
Together the two sides cover 192 workers/960 samples. Complete output and SHA-256 are measured;
construction, signing, original-tree comparisons, recovery and validation are setup.

### Retained signed batch archives

The new `batcharchives` suite retains a signed checkpoint and one genuinely applied
1/64/512/2,048-operation batch on up to 512 EVSEs, with one/four equal Ed25519 peers on the batch
and both commits. The largest batch repeats 512 targets four times. Buffered and borrowed-output
CBOR are measured. The third control emits canonical complete signed **batch JSON**, not JSON
archive output. Independent standalone previous-tree bytes/signing preimages and valid result
application are checked during setup. Archive recovery checks signatures, head/state and peer counts.

| Operations | Peers | Before streamed CBOR MiB | After MiB | Allocation change |
| --- | ---: | ---: | ---: | ---: |
| 1 | 1 | 0.2374 | 0.2438 | +2.7% |
| 1 | 4 | 0.2683 | 0.2752 | +2.6% |
| 64 | 1 | 3.1735 | 3.1789 | +0.2% |
| 64 | 4 | 3.2024 | 3.2103 | +0.2% |
| 512 | 1 | 24.5646 | 24.5316 | -0.1% |
| 512 | 4 | 24.5935 | 24.5277 | -0.3% |
| 2048 | 1 | 32.2807 | 32.1452 | -0.4% |
| 2048 | 4 | 32.2744 | 31.9035 | -1.1% |

At 2,048 operations/four peers, complete measured allocation changes **32.2744 -> 31.9035 MiB
(-1.1%)**. This is total allocation for the retained two-commit archive, not a microstage
percentage or retained-memory bound. Every before/after output digest, byte count and inventory agrees.
Preflight/cache work has overhead: one operation/one peer changes 0.2374 ->
0.2438 MiB (+2.7%). Removing retained output buffers
does not guarantee lower total allocation for each workload.
See [before](performance/direct-archive-changeset-batches-before.json),
[after](performance/direct-archive-changeset-batches-after.json),
[checked summary](performance/direct-archive-changeset-batches-summary.md) and
[complete comparison](performance/direct-archive-changeset-batches-comparison.md).

### Graph/history/pruned scaling controls

The six-operation scaling series reruns buffered/streamed JSON and CBOR archives, standalone
snapshot CBOR and frozen bootstrap on the four existing shapes. This package adds no cold/warm
snapshot probe; earlier child-cache measurements stay historical and its regression tests rerun.

| Shape | Before streamed CBOR MiB | After MiB | Allocation change |
| --- | ---: | ---: | ---: |
| graph-128 | 17.0270 | 17.0752 | +0.3% |
| graph-512 | 66.0325 | 66.0806 | +0.1% |
| history-64 | 7.0450 | 7.3914 | +4.9% |
| pruned-4 | 5.6612 | 5.8397 | +3.2% |

The history with many small batches changes 7.0450 -> 7.3914 MiB
(+4.9%). The unchanged JSON/snapshot code controls also show
some allocation/timing variation across processes. No general speedup or peak-memory reduction
is inferred; further reducing repeated ChangeSet JSON/path/preflight work remains an improvement item.

See [before](performance/direct-archive-changeset-scaling-before.json),
[after](performance/direct-archive-changeset-scaling-after.json),
[checked summary](performance/direct-archive-changeset-scaling-summary.md) and
[complete comparison](performance/direct-archive-changeset-scaling-comparison.md).
Method/runtime/dependencies and C# harness fingerprints match within and across these pairs.
Historical reports are preserved. No build/test from this task overlaps either measurement series.
Other local .NET activity was observed; the host is not dedicated or affinity controlled.
Elapsed times and sampled managed/working-set peaks remain descriptive. Setup inputs/previous
results can affect memory. These shapes establish no production capacity, concurrency or cold replay bound.

## Reproduction and next package

```powershell
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~DirectArchiveChangeSetTests|FullyQualifiedName~POICBORPreflightReuseTests|FullyQualifiedName~DirectArchivePOICBORTests|FullyQualifiedName~DirectChangeSetCBORTests|FullyQualifiedName~DirectPOICBORTests|FullyQualifiedName~ChildETagCacheTests|FullyQualifiedName~ArchiveStreamingTests|FullyQualifiedName~BootstrapStreamingTests|FullyQualifiedName~ArchiveStreamingFailureTests' --logger 'trx;LogFileName=direct-archive-changeset-focused-rerun.trx' --results-directory WWCP_POI_Tests/bin/TestResults/direct-archive-changeset-rerun
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=direct-archive-changeset-full-rerun.trx' --results-directory WWCP_POI_Tests/bin/TestResults/direct-archive-changeset-rerun
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite batcharchives --processes 2 --warmups 5 --samples 5 --label archive-batches-rerun --output WWCP_POI_Benchmarks/bin/archive-batches-rerun.json
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --suite scaling --operations archive-json,archive-json-stream,archive-cbor,archive-cbor-stream,snapshot-cbor,bootstrap-export --processes 2 --warmups 5 --samples 5 --label archive-scaling-rerun --output WWCP_POI_Benchmarks/bin/archive-scaling-rerun.json
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --compare-before docs/performance/direct-archive-changeset-batches-before.json --compare-after docs/performance/direct-archive-changeset-batches-after.json --summary WWCP_POI_Benchmarks/bin/archive-batches-comparison-rerun.md
python WWCP_POI_Benchmarks/archive_changeset_report.py --before docs/performance/direct-archive-changeset-batches-before.json --after docs/performance/direct-archive-changeset-batches-after.json --output WWCP_POI_Benchmarks/bin/archive-batches-summary-rerun.md
dotnet WWCP_POI_Benchmarks/bin/Release/net10.0/WWCP_POI_Benchmarks.dll --compare-before docs/performance/direct-archive-changeset-scaling-before.json --compare-after docs/performance/direct-archive-changeset-scaling-after.json --summary WWCP_POI_Benchmarks/bin/archive-scaling-comparison-rerun.md
python WWCP_POI_Benchmarks/archive_changeset_report.py --before docs/performance/direct-archive-changeset-scaling-before.json --after docs/performance/direct-archive-changeset-scaling-after.json --output WWCP_POI_Benchmarks/bin/archive-scaling-summary-rerun.md
```

Keep raw reports and use fresh outputs. The subsequent [recovery baseline](ARCHIVE-RECOVERY-COSTS.md)
now measures seven reader stages across all four profiles and defines incremental-reader contracts.
The subsequent [indexed reader](INDEXED-ARCHIVES.md) removes the complete archive CBOR tree,
preserving full-input validation and callback/model timing. Resident input, individual parts,
private boundary suffix bytes and retained branch states remain. [Borrowed input streams](ARCHIVE-INPUT-STREAMS.md)
now add bounded capture, mapping, cancellation and explicit orphan maintenance. Compact catalog indexes, larger domain/concurrency workloads,
automatic schedules and independent implementations remain separate roadmap items.
Reducing the measured small-batch preflight overhead is also retained as a follow-up.
