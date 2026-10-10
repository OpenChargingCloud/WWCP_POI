# Temporary validation projections for one immutable map

[Overview](../README.md) · [Rich workloads](DOMAIN-RECOVERY-WORKLOADS.md) · [Roadmap](ROADMAP.md)

## Observed allocation site

The preceding rich recovery baseline allocates 621.49–1,026.79 MiB per 64-EVSE CBOR recovery,
while the separate collected additional history is about 3.25–3.99 MiB. These are cumulative
allocations and approximate retained memory respectively; stages are not additive cost shares.

An EventPipe `gc-verbose` trace of the bound preceding v1/64-EVSE prepared-model restore
locates 53.30% of weighted allocation samples below recursive validation projection.
Parking validation repeatedly reconstructs the same station view and its ancestors. Added
subtrees, certificate consumers and merge target validation also revisit ancestors of a fixed map.
This evidence selects temporary projection reuse for this package.

The [filtered before/after profiles and trace bindings](performance/validation-projection-profiles.json)
retain commands, raw trace locations/hashes, analyzer templates/binaries and source/DLL bindings.
The analyzer keeps allocation ticks whose resolved stack contains `Measurement.Run`; setup,
warmup and subsequent controls are excluded by stack. Weights estimate allocation sites, with
overlapping inclusive callers; they are not exact per-method totals or additive shares.
The matching after trace has 16.23% of sampled weight in that traversal. Profiler
worker timings are excluded from the formal comparison. See
[Microsoft's dotnet-trace documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
and the [isolated analyzer templates](../WWCP_POI_Benchmarks/profiling/Program.cs.in).

## Implementation and lifetime

`RoamingNetworkDataSnapshot.ValidationProjection` owns one fixed immutable entity map.
Successful projections are indexed by the complete scoped entity key. Recursive ancestors and
one complete station-reference array are reused within that pass. A new map/operation receives
a new pass; no model is stored in a retained snapshot, history or ambient/persistent cache.

Passes cover ordinary single-node validation, all consumers of an updated approval document,
an added subtree and a completed merge target. Every node still runs its ordinary parser on
its first projection. Root catalog preparation, own documents, group owner reconstruction,
parking relations, reference scanning/indexing and validation order remain as before.
Groups retain full owner infrastructure reconstruction because membership parsing requires it.

Cache entries are bounded by the entity count of the current map; the station view by its
station count. Referenced model trees and dictionaries still consume memory; these entry bounds
are not constant byte or whole-process limits. All temporary models, including their local
runtime state, become collectible after the pass. Published source/recovered runtime objects
are independent and static canonical data excludes runtime as before.

Only successful projections enter the dictionary. Failed station arrays are not published.
Repeated failures use the ordinary parser and preserve exception type/message, first failure
and complete merge issue ordering. Successful ancestors can remain for the rest of a failing
pass; no failure or partially projected entity becomes a successful entry.

No public API, wire layout, signing profile or trust timing changes. Every current commit/batch
peer is checked afresh; eager/lazy suffix behavior, atomic publication, cancellation scopes and
normalization fallback remain. Full map/root/head normalization and final models still allocate.

## Tariff-group identifier correction

Coverage of all group kinds exposed a separate existing bug: `ChargingTariffGroup_Id.Parse`
accepts `*TG`, but `ToString()` emitted `*T`. Full JSON/CBOR imports of graphs containing tariff
groups therefore could not roundtrip. Emission now uses `*TG`, `Length` matches the actual emitted
operator format and `IsNotNullOrEmpty` reports the nonempty suffix correctly.

Ordering still uses normalized operator lengths: equivalent `DEABC` / `DE*ABC` forms compare
equal, as required by equality and sorted collections. The other group ID formats are unchanged.
The correction changes previously erroneous tariff-group ID text; it adds no alternate parser
or compatibility alias. The fixed 32 references and matched rich archives contain no tariff
groups and retain their exact original bytes. Focused group tests exercise the correction.

## Verification

`ValidationProjectionTests` adds **71 cases**:

- 22 entity kinds compared with the frozen preceding projection algorithm and repeated identity.
- Two complete 16/64-EVSE traversals, cache entry counts, ancestor sharing and station view identity.
- Twenty invalid node/ancestor cases, two first-failure order cases and failed station view retry.
- Independent runtime/changed-map behavior, three cultures and concurrent independent passes.
- Five tariff-group ID/format/order cases and a JSON/CBOR roundtrip containing all four group kinds.
- Eight hardcoded preceding archive/static/branch fingerprints with both original peers.
- Two atomic added-subtree cases and three complete merge issue-order comparisons.

The oracle is the exact preceding recursive projection source with a test-only namespace and
`OwnJSON` bridge. It uses the ordinary current domain parsers, including the intentional tariff
ID correction. The scoped patch reverses to exact preceding production source bytes/fingerprint.
Existing rich workload tests additionally cover bootstrap, trust failures/retry, detachment,
signed two-parent merge, all four profiles and independent local runtime.

On **2026-10-09 UTC**, all **71 new / 2,659 interoperability / 2,991 full Release cases pass**,
with zero failures and one ordinary worker skipped. Both explicit generators are excluded and
all 32 reference files are unchanged. See [execution evidence](verification/validation-projection-results.json)
and the [after source/binary binding](verification/validation-projection-after-binding.json).
Intermediate fixture corrections and the discovered production ID errors remain recorded.

## Matched measurements

The unchanged [preceding raw baseline](performance/domain-recovery-baseline.json) and
[new after series](performance/validation-projection-after.json) each contain **64 fresh workers /
192 measured calls**: eight 16/64-EVSE shapes, all four profiles, four stages and two workers /
three warmups / three samples. The same C# harness and dependency bytes run on both sides;
only production DLL/PDB bytes are replaced. All archive bytes, head/branch/peer fingerprints,
reference/value inventories and complete model/recovered results match exactly.

At 64 EVSEs, complete CBOR operation-thread allocation falls **33.66–42.18%**
to **408.86–634.18 MiB/call** across the four profiles. Prepared-model restore
allocation falls **43.53–52.75%**. Model-only decoding is a control and changes
**+0.0357–+0.2299%**. The [generated full comparison](performance/validation-projection-summary.md)
reports every shape/stage, elapsed time and collected additional history memory, including increases.

Elapsed-time medians are mixed, including increases in the 16-EVSE workloads and model-only
controls. The separate collected additional history remains about 3.26–4.01 MiB at 64 EVSEs.
Reduced temporary allocation therefore does not establish a general runtime or retained-memory gain.

Formal series run sequentially outside task builds/tests/profiling on a shared Windows desktop,
at different times without affinity control. Timings, sampled peaks and collected heap deltas
are descriptive. No production throughput/capacity, constant total-memory or latency bound follows.
The profiling run is separate and does not supply formal time/allocation samples.

## Reproduction and next work

Preserve the complete preceding Release harness before production edits. Build/test the after
library, clone the preceding harness and replace only POI DLL/PDB. Bind actual source/binary
bytes for each side. The launcher rejects changed binaries/dependencies/runtime files and
existing output paths. The comparison rejects incomplete groups and changed method, inputs,
inventories, peers, branch fingerprints or results. New builds require new actual bindings.

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~ValidationProjectionTests'
python -B WWCP_POI_Benchmarks/domain_recovery_measure.py --binary-directory path/to/matched-after --binding docs/verification/validation-projection-after-binding.json --label validation-rerun --output WWCP_POI_Benchmarks/bin/validation-rerun.json
python -B WWCP_POI_Benchmarks/validation_projection_report.py --before docs/performance/domain-recovery-baseline.json --before-binding docs/verification/domain-recovery-binding.json --after docs/performance/validation-projection-after.json --after-binding docs/verification/validation-projection-after-binding.json --output WWCP_POI_Benchmarks/bin/validation-summary-rerun.md
python -B WWCP_POI_Benchmarks/profiling/validation_projection_trace.py --binary-directory path/to/matched-after --binding docs/verification/validation-projection-after-binding.json --work-directory path/to/fresh-trace-directory --dotnet-trace path/to/dotnet-trace.exe
```

Run profiling outside formal measurement series. The launcher stages pinned TraceEvent 3.1.21
templates in an isolated directory; `.cs.in` / `.csproj.in` files leave the compiled C# recovery
harness unchanged. The measured tool is dotnet-trace 10.0.750501.

The subsequent package below completes normalized immutable property/child map reuse and
typed binding identity preparation. Larger group/catalog workloads, remaining document preparation
and reference indexes remain candidates. Individual parser cancellation, larger production catalogs and concurrency require their
own contracts and measurements. Retention still uses snapshots within the existing chain.

### Subsequent normalized immutable map preparation

[Normalized snapshot maps](SNAPSHOT-MAP-PREPARATION.md) reuse exactly equal properties, child sets,
entity/map branches and root catalogs after the full ordinary parser and normalization. Binding
reads stable typed IDs directly; all reference/current peer checks, exact errors, independent
runtime, canonical content and atomic publication remain. Removed identities/consumers are handled.
53 new cases and all 2,712 interoperability / 3,044 full Release cases pass with unchanged references.
Matched rich recovery measurements keep the exact archives, branches, peers and C# harness/dependencies.
Earlier reports remain historical. Larger group/catalog/history and concurrency workloads are next.
