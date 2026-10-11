# Root and head reconstruction

[Overview](../README.md) · [Architecture](ARCHITECTURE.md) · [History](HISTORY.md) · [Roadmap](ROADMAP.md)

## Reconstruction from a retained static snapshot

Complete-history recovery reconstructs the checkpoint model before trust/replay and the selected
head after all retained commits have passed their checks. Boundary recovery reconstructs its
authorized snapshot model before boundary trust, then the selected head after suffix replay.
Previously each reconstruction exported JSON, parsed the complete model, completed its static
representation and captured another immutable entity/reference map.

The internal `RoamingNetwork.ParseSnapshot` path still runs the complete domain parser in its
existing order: root properties, licenses, catalogs, operators, providers, parking children,
references, runtime defaults, timestamps and representation completion. Export and content-ETag
preparation also retain their previous position before model parsing. Public `Parse`/`TryParse`
and custom/resolver parsing continue to use ordinary capture.

Only after successful model parsing and completion may the supplied immutable snapshot be reused.
The comparison checks revision, applied ChangeSet ID, all entity properties, every owned child
and the complete visited entity set. Property JSON must match the stored raw text exactly;
numeric spellings, nested arrays, object order, SI strings and timestamp/default completion
cannot be silently folded into equality. Owned-child order is immaterial because capture stores
child identities in a set. Missing, extra, duplicate or different children disable reuse.

If any value differs, the existing `Capture` path runs with its original normalization and
diagnostics. In particular, completed missing timestamps or different escape/whitespace spellings
can require capture even when canonical content ETags agree. This conservative fallback preserves
the preceding reconstructed model's own property bytes. A structural snapshot alone never grants
permission to skip domain validation or trust.

## Ownership, runtime and publication

Successful reuse retains the already-owned immutable snapshot/entity map, reverse references and
existing lazy static ETags. The comparison borrows a private completed JSON tree only for the call;
its temporary visited set is released on return. No mutable tree, additional global cache, model
template or trust decision is retained. The bound child representations continue to store detached
strings as before.

Each separate reconstruction still creates fresh network, child and status-schedule objects.
Within an unpublished recovering history, if the final head has the very same snapshot instance
as its freshly reconstructed root, that private root model also supplies the head. This avoids
constructing a second identical root model. Separate histories never share these runtime objects;
runtime timestamps originate from the fresh root's construction.

All original signature peers, current keys, ancestry/state checks and boundary authority callbacks
still run, including repeated root verification required by the existing replay paths. Full-input
failure before trust, eager complete-history and lazy boundary suffix decoding, cancellation,
disposal and durable/atomic publication retain their existing contracts. Snapshot boundaries still
require explicit authority for omitted ancestry.

## Verification

`SnapshotReconstructionTests` adds 86 cases. The unchanged ordinary public snapshot-to-model route
is the preceding reconstruction oracle; it shares unchanged domain parsers, completion, capture,
representation binding and Styx helpers. The scoped production patch records the exact parser
factoring and conditional reuse changes separately.

Coverage includes exact static property bytes, canonical JSON/CBOR/ETags, revisions and applied IDs,
upward/catalog references, detached exports, all represented entity identities, missing metadata,
domain errors, numeric spellings, customer arrays and SI values, deep export/canonical/model errors,
normalization fallback, concurrent independent runtimes, root-only heads, all four profiles through
JSON/CBOR/bootstrap, original peers and all branches, fresh rejection/retry, cancellation and atomic
publication. Existing persistence/cold/bootstrap and eager/lazy failure suites run in the full regression.

All 2,134 interoperability / 2,466 full Release cases pass on 2026-10-09, with zero failures and
one ordinary worker skipped. See [execution and exact source/binary/reference bindings](verification/snapshot-reconstruction-results.json).
Fixed reference files are preserved; neither explicit reference generator is run.

## Matched measurements

[The four-stage summary](performance/snapshot-reconstruction-summary.md) uses the unchanged C#
harness binary/dependencies and exact seven prepared shapes/all four archive profiles. The preceding
canonical-preparation after series is the before control: 56 workers/168 samples. Another 56 fresh
workers/168 samples measure this implementation, with three warmups and three samples per worker.
All formal workers run sequentially without this task's builds/tests overlapping them.

Model-only and individual-signature probes are controls. Prepared-model restore includes root/head
construction, fresh trust, ancestry and replay; complete CBOR recovery also includes decoding.
These stages separate model decoding from replay preparation, but do not individually time each
root/head constructor or establish their shares. Stage medians cannot be added. Input bytes,
inventories, every repeated result, original peers, all branches and fresh runtime agree before/after.

Complete CBOR recovery uses 1.54–7.69% fewer cumulative operation-thread allocations in every
measured shape. At 512 EVSEs the median changes 1561.2854 -> 1494.3186 MiB (-4.29%); at
2,048 operations/four peers it changes 1524.6701 -> 1457.7567 MiB (-4.39%). Prepared-model
restore uses 2.61–12.19% fewer allocations. Model/signature controls include small changes,
including +0.38% in 128-EVSE model preparation. These controls do not establish an unchanged
allocation baseline for every stage, and the package makes no universal allocation claim.

All seven full-recovery elapsed medians are lower in this series; at 512 EVSEs they change
3308.7 -> 2632.6 ms. Prepared-model restore includes a timing increase for the 64-commit shape, 326.2 -> 361.8 ms,
despite its lower allocations. The shared desktop and synthetic shapes prevent a general speed claim.
Collected retained-memory changes are mixed: 512 EVSEs are approximately 13.4209 -> 13.4193 MiB,
while the 64-commit shape rises 1.0079 -> 1.0562 MiB. The optimization mainly avoids temporary
capture work; these collected probes demonstrate no universal decrease in retained memory.

Post-measurement collection holds one additional recovered history with source/input live.
These approximate heap deltas describe retained maps, model/runtime and other state together;
they establish no total-memory bound. The shared desktop is neither dedicated nor affinity controlled.
Elapsed times, CPU and sampled peaks remain descriptive rather than production-throughput evidence.

## Remaining work

Complete model/projection/representation trees still allocate, and mismatched property spellings
continue to take full capture. The subsequent [signature-copy package](SIGNATURE-COPIES.md)
reuses validated IDs for signature-only immutable envelope copies.
Any broader map/property reuse needs its own normalization proof. The subsequent
[parser-cancellation package](PARSER-CANCELLATION.md) adds owned-loop and model-boundary checks;
individual parser calls remain synchronous. Larger reference/tariff/parking workloads,
production concurrency and persistent indexes remain separate candidates.

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
