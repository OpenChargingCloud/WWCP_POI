# Normalization-aware immutable snapshot map preparation

[Overview](../README.md) · [Previous package](VALIDATION-PROJECTION.md) · [Roadmap](ROADMAP.md)

## Scope and observed cause

The preceding allocation profiles select full root/head model reconstruction as a remaining
cost after validation projections were reduced. A separate development probe shows why the
existing exact-snapshot shortcut often falls back: touched `lastChange` values contain
`\u002B00:00`, while ordinary JSON import writes `+00:00`. The string value is equivalent;
its recorded raw property spelling differs. One such change previously caused every immutable
property dictionary, entity record, child map and reverse reference catalog to be rebuilt.

This package completes the roadmap item for normalization-aware immutable map preparation.
It preserves the preceding normalized raw output. Unchanged properties and entities are reused
after normalization, including root catalogs and ownership maps. A second repeated cost is
removed: static representation binding no longer serializes a complete child subtree solely
to obtain its identity.

## Validation, ownership and reuse rules

`RoamingNetwork.ParseSnapshot` still exports a detached document and runs the complete ordinary
domain parser, all metadata/reference checks and representation completion. Exact completed
document equality still permits the existing whole-snapshot shortcut. On fallback, `Capture`
receives the prior immutable snapshot as an optional internal reuse candidate.

Every imported node still runs ordinary ID normalization, duplicate detection, parent checks,
field validation, timestamp/SI/nested normalization and owned-child detachment. The ordinary
non-reconstruction import body is retained verbatim. A change in domain-equal visible ID/scope
spelling conservatively falls back to ordinary capture, preserving key/parent/child text.
Reconstruction has a separate visited-key set, so starting with the preceding map never hides a duplicate or keeps a removed descendant.
The entry-point mutable tree is private; supplied and projected trees remain borrowed/read-only.

For each normalized own property, its compact JSON is compared with the prior property's exact
`JsonElement.GetRawText()`. Only equal raw text permits reusing that immutable value and dictionary
branch. Missing properties are removed; added/changed properties get detached values. This is
not a canonical-value equality shortcut: customer numbers, escaping, array order, nested customer
`ETags` and SI strings follow precisely the ordinary capture result.

Child membership starts from the previous immutable set and adds/removes supplied keys. Equal
key, parent, properties and children retain the same entity record; unchanged entity-map branches
and root catalogs remain shared. Changes in parent, identity, child membership or property bytes
produce the corresponding new value. Every new snapshot retains its own revision/last batch ID.
Content/child ETag contexts are shared only when the complete map and root are identical.

Old reference consumers belonging to changed or removed records are detached. Every current
entity then passes the ordinary reverse-reference and scope checks, including unchanged entities.
An equal final reverse index reuses the previous catalog, including its tariff subset. Reuse
never bypasses resolution, shape, duplicate, operator ownership or approval checks.

Representation binding reads strongly typed IDs for all 22 owned kinds and energy meters,
using the same normalized identity rules as the prior JSON projection. Tariff elements, price
components, restrictions and software assignments retain positional binding. Unknown future
types retain the preceding full-projection fallback. Stored representations remain immutable
strings in the weak table; no mutable JSON tree is retained.

Fresh domain models and runtime/status/measurement objects remain local to each reader. No
model, runtime, key or trust cache is introduced. Current signature peers and authority policies,
eager/lazy suffix behavior, parser cancellation and atomic publication retain their contracts.
Public JSON/CBOR arrays and documents remain detached. Wire layouts, signing profiles and
canonical JSON/CBOR identities are unchanged.

## Verification

`SnapshotMapPreparationTests` adds **53 cases**:

- 22 entity kinds: exact ordinary capture output and identity sharing of unchanged records,
  property values, child sets and reverse catalogs after changing an allowed property.
- Three identity-alias cases: exact visible map-key, parent and child spellings.
- One removed-property case: remaining property values and child sets stay shared.
- Three revision boundaries: exact maps/content context with independent revision bookkeeping.
- Three ownership edits: added, removed and moved children with affected maps/reference updates.
- Eight invalid imports: exact error type/message, caller/source ownership and independent retry.
- Eight rich 16/64-EVSE archives across four profiles: every retained state, timestamp normalization,
  exact canonical JSON/CBOR/ETags and sharing of all other properties/records/reference catalogs.
- One complete binding traversal: direct IDs equal preceding full projection for every owned kind,
  meters and nested values.
- Four numeric/customer-data cases: raw number spelling, escaped strings, arrays and SI strings.

On **2026-10-10 UTC**, the full Release run passes **3,044 tests**, including **2,712 interoperability
cases and all 53 new cases**, with zero failures and one ordinary worker skipped. Both explicit
generators are excluded. Existing tests additionally cover normalization/default/depth failures,
independent concurrent runtime, current peer trust, merge/error order, bootstrap, cancellation
and atomic rejection/retry. All 32 reference artifacts remain unchanged.

The [execution evidence](verification/snapshot-map-preparation-results.json) and
[source/binary binding](verification/snapshot-map-preparation-binding.json) record exact source,
DLL, fixture, dependency and reference fingerprints. The scoped patch reverses to the exact
preceding production/test source bytes; ordinary import preservation is checked directly.
Initial test-fixture corrections are retained in the evidence.

## Matched measurements

The preserved [preceding formal series](performance/validation-projection-after.json) and
[new formal series](performance/snapshot-map-preparation-after.json) each contain **64 fresh
workers / 192 measured calls**: eight shapes, four production recovery stages, two workers per
stage, three warmups and three samples. The same C# harness and dependency bytes run on both
sides; only the production DLL/PDB is replaced. Archive bytes, complete graph/value/reference
inventories, original peers, both branch states and complete model/head results match exactly.

At 64 EVSEs, complete CBOR operation-thread allocation falls a further
**16.63–19.38%**, to **329.63–528.71 MiB/call** across the
four profiles. Prepared-model restore allocation falls **15.25–19.79%**.
Collected additional recovered-history heap is approximately **2.50–3.33 MiB**.
The [generated comparison](performance/snapshot-map-preparation-summary.md) reports every stage,
elapsed-time change and collected retained-memory delta, including increases.

These are exact cumulative operation-thread allocation counters and descriptive process-heap
measurements respectively. Stages are not additive shares; retained memory is not temporary
allocation. Setup, warmup, full controls and disposal are excluded from measured calls; fresh
policy/crypto verification remains inside recovery. Formal workers run sequentially outside
task builds/tests/profiling on a shared desktop, at different times from the preceding series.
Elapsed time and approximate heap deltas give no production capacity, constant-memory or
cancellation-latency bound.

## Reproduction and remaining work

Preserve the preceding complete Release harness, build/test the new library, clone the harness
and replace only POI DLL/PDB. New builds need new actual source/binary bindings. The launcher
rejects changed dependencies/binaries/configuration and existing output paths. The comparison
checks complete inventory, method, inputs, peers, branch states and results; negative controls
reject 37 altered reports and five altered bound files.

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~SnapshotMapPreparationTests'
python -B WWCP_POI_Benchmarks/domain_recovery_measure.py --binary-directory path/to/matched-after --binding docs/verification/snapshot-map-preparation-binding.json --label map-preparation-rerun --output WWCP_POI_Benchmarks/bin/map-preparation-rerun.json
python -B WWCP_POI_Benchmarks/snapshot_map_preparation_report.py --before docs/performance/validation-projection-after.json --before-binding docs/verification/validation-projection-after-binding.json --after docs/performance/snapshot-map-preparation-after.json --after-binding docs/verification/snapshot-map-preparation-binding.json --output WWCP_POI_Benchmarks/bin/map-preparation-summary-rerun.md
```

Ordinary domain parsing, detached documents, normalized property comparison, cryptography and
fresh runtime still allocate. The completed map-reuse package does not imply zero-allocation
recovery. Larger group/catalog workloads, longer histories, production concurrency and controlled
JIT/warmup measurements are the next scaling package. Independent peer interoperability, service
and policy integration and durability testing remain separate work. Retention uses snapshots
within the existing chain.
