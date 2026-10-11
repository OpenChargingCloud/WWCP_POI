# Shared-reference, tariff and parking recovery workloads

[Repository overview](../README.md) · [Domain model](DOMAIN-MODEL.md) · [Scaling](SCALING.md) · [Roadmap](ROADMAP.md)

This package extends the recovery benchmark with deterministic domain data and contract tests.
The production sources, production DLL, shared dependencies, static/signing profiles and the
32 fixed interoperability references remain unchanged from [parser cancellation](PARSER-CANCELLATION.md).
The new C# fixture uses production APIs and is compiled into both the benchmark and the tests.
The [binding](verification/domain-recovery-binding.json) records that linked file separately.

## Prepared domain data

The `domainrecovery` suite has eight shapes: 16 or 64 EVSEs, each with all four history profiles.
Every station owns four EVSEs; every pool owns two stations. Their static graphs contain:

- Two shared grid operators, referenced by the pool connection points. Each point includes a
  meter, nominal voltage/frequency and contracted active/apparent power in SI quantities.
- Two pool meters, two station meters, one EVSE meter and one connection-point meter per
  applicable owner: 30/120 physical meter slots at 16/64 EVSEs, with 60/240 software assignments.
- Four shared software releases and two model/version approval documents. Each document
  distinguishes a verified release from a compatible release; repeated meter assignments resolve
  the same immutable catalog instances. Dates retain subsecond precision.
- Four tariffs, eight tariff elements, 24 price components and 16 restrictions. Energy/time/flat
  prices, decimal values, SI energy/power/duration limits, calendar/time/day restrictions and
  EVSE/connector tariff references exercise existing nested value parsers.
- One parking operator, one garage per pool, one space per EVSE, two overlapping groups and two
  products. Garage/space/group offer references and station links exercise deduplicated product
  lookup and graph reference validation. Products retain the existing duration-only model.

Tariff elements/components/restrictions remain anonymous value structures; editing `elements`
replaces that property. Identified EVSE tariff memberships and parking group memberships use
explicit `AddElement`/`RemoveElement` operations. No artificial tariff IDs or array-index edits
are introduced. Reference inventory counts come from the production reverse index: repeated
references from one owner to the same target collapse. Meter counts describe physical slots.

Every retained commit and embedded ChangeSet has two equal Ed25519 peers using distinct,
published reproducible test keys. The fixture caches those public key objects outside recovery;
each recovery still verifies every peer against the supplied current policy. These keys are test
data, not production authority.

## History shapes and edits

Each shape first applies seven signed edits: shared grid description, software vendor, document
number, connection-point grid reference, EVSE tariff membership, nested tariff values and parking
group membership. The fixture then prepares the profile-specific history:

| Profile | Prepared history |
| --- | --- |
| v1 | Complete checkpoint ancestry and the first seven edits |
| v2 | Complete ancestry, a signed snapshot link, then another seven edits |
| v3 | Explicitly authorized signed snapshot boundary, then another seven edits |
| v4 | Two reviewed archival/pruning rounds and retained receipts, then another seven edits |

Finally, every shape creates two disjoint branches from the same retained parent: a software
vendor edit and a parking duration edit. Merge preview reports availability before explicit
merge preparation, signing and publication. The two original parents, both branch states and
the signed merge remain in the archive. The branch fingerprint binds each retained commit ID
and both state ETags in ordinal order using length-prefixed UTF-8 strings.

Source EVSE/grid/parking/meter statuses are changed only after the static inventory is captured.
Recovered histories resolve their own runtime objects with fresh defaults. Static publication
within the source history preserves its local operational state.

## Measurement boundaries

| Operation | Measured work |
| --- | --- |
| `read-cbor-model` | Eager immutable model construction from an already parsed archive tree using production parsers |
| `read-restore` | Trust and replay from prepared models through the existing private production restore entry points |
| `read-cbor` | Complete normal resident CBOR recovery: syntax/limits/index, models, current trust and replay |
| `read-json` | Complete normal resident JSON recovery: syntax, models, current trust and replay |

The eager model-only control decodes every suffix model. Actual v3/v4 recovery retains its
production lazy suffix timing. These are separate workloads; their time/allocation values cannot
be added or subtracted as processing shares. Internal restore reflection stays in the harness.
The production library gains no benchmark API or additional friend assembly.

Setup constructs valid graphs/edits/retention/merge, freezes exact JSON/CBOR, verifies original
peers and checks all three reconstruction routes. Setup, warmup, post-call complete byte/branch/
shared-reference/runtime comparisons and disposal are outside measured calls. Model-only results
include the complete model/peer fingerprint; restore/full recovery checks the exact head. Every
stage and repeated worker must have the same input/inventory/branch identities.

After measured CBOR calls, a separate collection probe records the approximate heap delta of
one additional recovered history with source and input still live. This is distinct from
cumulative operation-thread allocation and sampled process memory. Neither supplies a constant
total-memory bound.

## Contract tests

`DomainRecoveryWorkloadTests` adds 81 cases:

- Eight deterministic 16/64-EVSE fixtures across all four profiles, including original peer checks.
- Sixteen JSON/CBOR/private-memory/mapped-spool roundtrips, borrowed source ownership and later
  signed publication after cancelling a completed read's token.
- Sixteen bootstrap activations across profiles and capture policies, including current trust
  rejection, retained staging, successful retry, exact installed bytes and writer lease release.
- Two input/export detachment cases for shared catalogs and nested tariff/parking values.
- Twenty-four invalid reference/value edits that leave the head, archive, inventory and runtime
  unchanged. Referenced grid/software/document/tariff/parking targets cannot be deleted.
- Eight additional unknown peer rejections at commit/batch level with atomic state and valid retry.
- Four explicit merge cases checking both branches and resulting software/parking values.
- Three culture checks for exact quantities, prices, signatures, archives and branch fingerprints.

On 2026-10-09 UTC, all **81 new / 2,588 interoperability / 2,920 full Release cases pass**,
with zero failures and one ordinary worker skipped. Both explicit reference generators are
excluded. See [execution evidence](verification/domain-recovery-results.json) for full-suite counts,
source/DLL/linked-fixture bindings, preserved build logs and fixed reference hashes.

## Results and reproduction

The [raw baseline](performance/domain-recovery-baseline.json) and generated
[summary](performance/domain-recovery-summary.md) contain 64 sequential fresh workers / 192
measured calls: two workers, three warmups and three samples per shape/operation. All task
builds/tests and the six [historical input controls](performance/domain-recovery-basic-controls.json)
finish before the formal series. The shared Windows desktop is not an isolated production host.
Runtime configuration, dependency manifest and dependency DLL bytes are bound explicitly.

The old simple EVSE-power workloads retain exact inputs, identities, peers and output controls
with the new harness. Those six calls are correctness checks. Richer archives, different edits
and two distinct keys define a new baseline; historical performance numbers are not comparable.
This package makes no production optimization or throughput/capacity/cancellation-latency claim.

Preserve a complete Release benchmark directory and bind its actual bytes. A rebuilt DLL can
have different bytes; give a new build a new binding rather than relabelling the checked-in run.
Launchers reject changed binaries/dependencies/runtime files and existing output paths. The
summary rejects missing groups/workers/samples, changed shapes/inventories/branch/results/peers,
method/binding/dependencies and retention counts.

```powershell
dotnet build WWCP_POI_Benchmarks/WWCP_POI_Benchmarks.csproj -c Release --no-restore
dotnet build WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-restore
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~DomainRecoveryWorkloadTests' --logger 'trx;LogFileName=domain-recovery-rerun.trx' --results-directory WWCP_POI_Tests/bin/TestResults/domain-recovery-rerun
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName!~GenerateReferenceVectors&FullyQualifiedName!~GenerateSnapshotRetentionVectors' --logger 'trx;LogFileName=domain-recovery-full-rerun.trx' --results-directory WWCP_POI_Tests/bin/TestResults/domain-recovery-rerun
python -B WWCP_POI_Benchmarks/domain_recovery_controls.py --binary-directory path/to/preserved-domain-build --binding docs/verification/domain-recovery-binding.json --preceding docs/performance/parser-cancellation-after.json --output WWCP_POI_Benchmarks/bin/domain-controls-rerun.json
python -B WWCP_POI_Benchmarks/domain_recovery_measure.py --binary-directory path/to/preserved-domain-build --binding docs/verification/domain-recovery-binding.json --label domain-baseline-rerun --output WWCP_POI_Benchmarks/bin/domain-baseline-rerun.json
python -B WWCP_POI_Benchmarks/domain_recovery_report.py --input docs/performance/domain-recovery-baseline.json --binding docs/verification/domain-recovery-binding.json --output WWCP_POI_Benchmarks/bin/domain-summary-rerun.md
```

## Measured next-step evidence

At 64 EVSEs, complete CBOR recovery allocates **621.49–1,026.79 MiB per call** across the
four prepared profiles. The separate prepared-model restore controls allocate **482.31–744.57 MiB**;
model-only controls allocate **134.54–276.74 MiB**. These are cumulative operation-thread
allocations, not resident RAM or additive stage shares. The collected additional recovered-history
heap deltas are approximately **3.25–3.99 MiB**. See the complete table for actual archives,
retained commits, timings and repeated samples.

Inference: the difference between cumulative allocation and collected retained memory makes
temporary model/reference/replay preparation a useful next investigation. The controls do not
identify an individual allocation site or prove that map reuse will improve end-to-end recovery.
Profile v2 retains more ancestry than v3/v4; their different archive/commit counts are not a matched
retention optimization experiment.

## Subsequent work

The [validation projection package](VALIDATION-PROJECTION.md) now uses this exact baseline and
allocation stack evidence to reuse temporary ancestors/station views per immutable map. Its
matched results retain these archive bytes, branch/peer fingerprints and the same harness.
The baseline above remains unchanged. Normalized map preparation is completed below; larger catalogs,
production concurrency, individual parser cancellation and persistent indexes remain further
work; synthetic 16/64-EVSE results do not establish their production costs.

### Subsequent normalized immutable map preparation

[Normalized snapshot maps](SNAPSHOT-MAP-PREPARATION.md) reuse exactly equal properties, child sets,
entity/map branches and root catalogs after the full ordinary parser and normalization. Binding
reads stable typed IDs directly; all reference/current peer checks, exact errors, independent
runtime, canonical content and atomic publication remain. Removed identities/consumers are handled.
53 new cases and all 2,712 interoperability / 3,044 full Release cases pass with unchanged references.
Matched rich recovery measurements keep the exact archives, branches, peers and C# harness/dependencies.
Earlier reports remain historical. Larger group/catalog/history and concurrency workloads are next.
