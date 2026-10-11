# Immutable model and replay preparation

[Repository overview](../README.md) · [Reader costs](ARCHIVE-RECOVERY-COSTS.md) · [Mapped input](MAPPED-ARCHIVE-RECOVERY.md)

## Private tree ownership

Recovery measurements identified model construction and replay as much larger allocation costs
than the complete file-input array. This package removes repeated deep copies of already private
JSON descendants in three existing preparation traversals. It changes neither wire profiles nor
public signatures.

| Entry point | Private traversal | Ownership and result |
| --- | --- | --- |
| `RoamingNetworkDataSnapshot.Import` | `ImportOwned` | Clone the supplied hierarchy or added subtree once; recursively import its owned children into local immutable maps |
| `POISnapshotRepresentation.CompleteImport` | `CompleteOwnedImport` | Clone the supplied source once; complete its owned children against the borrowed read-only projection |
| `POISnapshotRepresentation.Bind` | `BindOwned` | Clone the supplied document once; retain detached static strings for reconstructed immutable values |

The private methods accept only descendants of an entry-point copy. Projection fallback values
and replacements still receive their explicit clones before entering that tree. Source and
projection nodes remain borrowed and unchanged, including when a late error occurs. Returning a
completed tree or reading a retained representation never exposes a caller's original nodes.
No mutable JSON node is retained by the weak representation table; its existing string values
and weak ownership remain unchanged. This package introduces no persistent preparation cache.

## Validation, identities and runtime

All existing node-level work remains in the same order: schema and parent checks, duplicate-ID
reservations, timestamp and SI normalization, removal of runtime/transport bookkeeping,
immutable property capture, child traversal and reference validation. Recursive normalization
passes and explicit projected-child copies remain. Removing these would require separate
evidence about error order and ownership.

Exact canonical static JSON/CBOR, ETags, commit identities and original signing inputs remain
unchanged. Canonical CBOR can reorder object fields on transport; comparison of each import
uses the actual received payload. Ordinary updates still touch their owners' timestamps.
The existing raw signed numeric/SI token contracts are preserved.

Trust decisions are never reused. Every original commit and ChangeSet peer is checked against
the current recovery policies. Full-input syntax and local archive limits still precede trust;
complete v1/v2 models remain eager and v3/v4 suffix models remain lazy. Boundary authorization,
known-length diagnostics, cancellation checks, leases and atomic installation retain their
existing routes and error timing.

Runtime is reconstructed locally. A source's changing statuses and measurements do not enter
the static snapshot, hash or retained representation. Immutable state sharing and local runtime
updates retain their existing behavior.

## Regression coverage

`ModelPreparationTests` adds 88 cases. Frozen test-only copies of the preceding recursive
algorithms compare completed trees, bound strings, immutable entity properties, parent/child
links and exception types/messages. Cases cover all fixture model kinds, owned/unowned views,
missing projected children and order, input/output detachment, duplicate reservations, late
failures, metadata, SI values, customer data, runtime stripping and concurrent borrowed input.
Signed subtree additions compare the previous importer on the actual JSON/CBOR payload.
Failed additions preserve source state and signatures and allow a valid signed retry. Complete
tagged imports still verify declared ETags through `POIRepresentation.ParseJSON`; the ordinary
domain `Parse` function remains the supplied model parser. All four archive profiles verify
exact peer envelopes, retained branch states, fresh runtime and a newly rejecting trust policy.

```powershell
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --filter FullyQualifiedName~ModelPreparationTests
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --filter "FullyQualifiedName~Interoperability&FullyQualifiedName!~GenerateReferenceVectors&FullyQualifiedName!~GenerateSnapshotRetentionVectors"
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --filter "FullyQualifiedName!~GenerateReferenceVectors&FullyQualifiedName!~GenerateSnapshotRetentionVectors"
```

The two explicit reference generators are excluded from ordinary regression runs. Published
interoperability artifacts are compared without regeneration. Test-only oracles retain their
previous copying behavior and never participate in production parsing.

On 2026-10-09 all 88 new cases, all 1,942 interoperability cases and all 2,274 full Release cases
pass, with zero failures and one ordinary worker skipped in each broad run. The interoperability
filter covers that entire namespace; earlier packages' focused counts used narrower selections.
The library, test and benchmark builds succeed. Source/binary/dependency hashes, test results,
unchanged fixed artifacts and scoped reverse-patch reconstruction are recorded in
[model-preparation-results.json](verification/model-preparation-results.json).

## Matched preparation and recovery measurements

[Before](performance/model-preparation-before.json) and [after](performance/model-preparation-after.json)
use the same seven prepared shapes, all four profiles and the preserved preceding C# harness
binary. Model decoding, restore from prepared models and full CBOR recovery each use two fresh
processes, three warmups and three measured calls per process: **84 workers / 252 samples** total.
The scoped source patch reconstructs the before source bytes; only the after production library
and PDB change in the two private binary folders. A freshly rebuilt harness has a separately
recorded assembly hash; the formal comparison keeps the preceding harness binary identical.
Shared dependency and runtime configuration bytes remain identical. Test completion and build
timestamps precede the sequential formal series.

Every archive/static identity, input byte count, model fingerprint, peer envelope, retained branch
state and recovered result matches. Full-CBOR operation-thread allocation decreases by 3.44–6.98%
across these seven shapes. At 512 EVSEs it changes **1688.5149 → 1571.3675 MiB (−6.94%)**;
model-only allocation changes 999.5688 → 929.1824 MiB (−7.04%). At 2,048 operations/four peers,
full recovery changes 1656.6782 → 1580.4229 MiB (−4.60%). These are cumulative managed allocations,
not peak live memory or archive size.

Elapsed times include improvements and increases, including the history-64 and pruned-4 complete
recovery groups. The host is a shared Windows desktop, without affinity control. Sampled peaks
and post-measurement collected-history deltas are descriptive and establish no total-memory bound
or production throughput. The model-only boundary probe is eager; actual v3/v4 suffix recovery
remains lazy. Setup, exact output/branch/runtime checks and disposal remain outside measurement.
Individual stage medians cannot be added or interpreted as shares of complete recovery.

[The validated summary](performance/model-preparation-summary.md) and
[reproduction commands](../WWCP_POI_Benchmarks/README.md#private-modelreplay-preparation-measurements)
retain the original reports and bind exact preceding mapped-recovery inputs. The portable stdlib
launcher verifies explicit binary/source bindings and reproduces the original worker arguments;
the summary verifies method, complete groups, dependencies, original outputs and inventories.

## Subsequent work

[Bounded canonical preparation](CANONICAL-PREPARATION.md) now reuses immutable unsigned bytes
within synchronous recovery and peer verification. Original profile/identity/signature bytes,
public detachment, every current trust callback and eager/lazy recovery remain exact. Its
matched measurements are separate from this package's historical results; neither adds a
persistent preparation cache. Root/head reconstruction, individual model/projection trees,
immutable maps, retained versions, finer cancellation and production workloads remain work.

The subsequent [parser-cancellation package](PARSER-CANCELLATION.md) adds owned-loop and
model-boundary checkpoints; individual parser calls remain synchronous. The subsequent
[domain recovery baseline](DOMAIN-RECOVERY-WORKLOADS.md) now measures shared catalogs, nested
tariffs, parking and signed merges, with separate evidence from this historical baseline.

The subsequent [root/head reconstruction package](SNAPSHOT-RECONSTRUCTION.md) conditionally
retains exact immutable snapshots after full model validation and completion. It preserves this
package's contracts and conservative capture fallback; the measurements above remain historical.

### Subsequent temporary validation projection reuse

[Validation projections](VALIDATION-PROJECTION.md) reuse successful ancestors and one station
view per fixed immutable map. Every ordinary parser/reference/trust check and failure order
remains; failed values are uncached and runtime stays private to the pass. The separate tariff
group text/length/empty-flag correction preserves ordering across equivalent operator formats.
71 new cases and all 2,659 interoperability / 2,991 full Release cases pass with unchanged
fixed references. The exact rich baseline/harness/dependencies support matched recovery
measurements; earlier performance reports remain historical. Normalized immutable map
preparation is completed below; larger catalogs and production concurrency remain work.

### Subsequent normalized immutable map preparation

[Normalized snapshot maps](SNAPSHOT-MAP-PREPARATION.md) reuse exactly equal properties, child sets,
entity/map branches and root catalogs after the full ordinary parser and normalization. Binding
reads stable typed IDs directly; all reference/current peer checks, exact errors, independent
runtime, canonical content and atomic publication remain. Removed identities/consumers are handled.
53 new cases and all 2,712 interoperability / 3,044 full Release cases pass with unchanged references.
Matched rich recovery measurements keep the exact archives, branches, peers and C# harness/dependencies.
Earlier reports remain historical. Larger group/catalog/history and concurrency workloads are next.
