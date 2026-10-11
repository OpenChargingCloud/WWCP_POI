# Cooperative archive-parser cancellation measurements

Before `parser-cancellation-before.json`; after `parser-cancellation-after.json`. Six shapes/all four profiles, three operations, two fresh processes/three warmups/three samples per side: 72 workers / 216 measured calls in total.

The same new C# harness binary adds an uncancelled, cancellable token to the existing private-memory borrowed-stream recovery probe. Both sides use identical dependencies/runtime configuration and prepared archives, independently bound to `mapped-file-controls.json`. Exact output bytes, static identities, heads, every retained branch state, original peers and fresh local runtime are checked outside measured calls.

read-cbor-limits measures the existing local-budget scan with a default token. read-cbor measures ordinary span recovery with a default token. read-cbor-input-token measures complete synchronous borrowed-stream capture and recovery with a cancellable token that is never cancelled. It includes existing stream buffers/copies and token-scoped trust wrappers as well as new parser checks. The before build already contains input/trust cancellation; only the production library changes between sides. Comparisons between different operations cannot isolate parser-check costs.

Workers run sequentially; no task build/test overlaps either formal series. The shared Windows desktop is not dedicated or affinity controlled. Stage medians cannot be added or subtracted as processing shares. These successful-read measurements establish no cancellation-latency bound.

| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| recover-batch-1-peers-1 | read-cbor | 4.5212 | 4.5211 | -0.0035% | 23.375 | 15.100 |
| recover-batch-1-peers-1 | read-cbor-input-token | 4.5882 | 4.5877 | -0.0105% | 16.861 | 15.048 |
| recover-batch-1-peers-1 | read-cbor-limits | 0.0007 | 0.0007 | +0.0000% | 0.041 | 0.031 |
| recover-batch-2048-peers-4 | read-cbor | 1458.3993 | 1458.6840 | +0.0195% | 2235.221 | 5019.935 |
| recover-batch-2048-peers-4 | read-cbor-input-token | 1459.3362 | 1461.7726 | +0.1670% | 2193.985 | 3035.391 |
| recover-batch-2048-peers-4 | read-cbor-limits | 0.0007 | 0.0007 | +0.0000% | 1.071 | 1.143 |
| recover-boundary-16 | read-cbor | 40.9982 | 40.9980 | -0.0006% | 172.331 | 182.050 |
| recover-boundary-16 | read-cbor-input-token | 41.1150 | 41.1157 | +0.0017% | 170.337 | 155.290 |
| recover-boundary-16 | read-cbor-limits | 0.0007 | 0.0007 | +0.0000% | 0.129 | 0.132 |
| recover-graph-128 | read-cbor | 377.8301 | 377.1478 | -0.1806% | 754.443 | 663.854 |
| recover-graph-128 | read-cbor-input-token | 378.2761 | 377.7420 | -0.1412% | 685.881 | 677.317 |
| recover-graph-128 | read-cbor-limits | 0.0007 | 0.0007 | +0.0000% | 1.018 | 1.022 |
| recover-graph-512 | read-cbor | 1494.2783 | 1494.3465 | +0.0046% | 2658.368 | 2646.058 |
| recover-graph-512 | read-cbor-input-token | 1496.9118 | 1496.4450 | -0.0312% | 2862.085 | 2674.076 |
| recover-graph-512 | read-cbor-limits | 0.0007 | 0.0007 | +0.0000% | 4.414 | 4.315 |
| recover-pruned-4 | read-cbor | 106.5596 | 106.5441 | -0.0146% | 422.995 | 380.680 |
| recover-pruned-4 | read-cbor-input-token | 106.8086 | 106.8502 | +0.0389% | 417.372 | 369.543 |
| recover-pruned-4 | read-cbor-limits | 0.0026 | 0.0026 | +0.0000% | 0.544 | 0.423 |

Timings are mixed. In the largest batch, ordinary span recovery changes 2235.221 -> 5019.935 ms; active-token input recovery changes 2193.985 -> 3035.391 ms. The measured increases remain part of the evidence. This shared-desktop comparison does not isolate their cause or establish a universal check overhead, speedup or latency bound.

## Collected additional recovered-history memory

The existing post-measurement collection holds one additional ordinary span-recovered history with source/input live on both sides. Process-heap deltas are approximate, include model/state/runtime overhead and establish no total-memory bound. No parsing token/observer or progress context is retained in returned histories.

| Shape | Before median MiB | After median MiB |
| --- | ---: | ---: |
| recover-batch-1-peers-1 | 0.0740 | 0.0739 |
| recover-batch-2048-peers-4 | 11.7005 | 11.6331 |
| recover-boundary-16 | 0.4336 | 0.4333 |
| recover-graph-128 | 3.4741 | 3.4443 |
| recover-graph-512 | 13.4195 | 13.4220 |
| recover-pruned-4 | 0.8353 | 0.8246 |

Elapsed time, process CPU and 10 ms sampled peaks remain descriptive. Managed allocation measures the operation thread; setup, verification and disposal stay outside it. Individual Styx scalar/structured-key/SkipValue calls, immutable model construction, canonicalization, cryptography and bulk copies remain synchronous and cannot be interrupted internally by this package. Cancellation is observed around those calls and inside owned value/commit/receipt/replay loops. No hard byte, time, throughput or production-concurrency guarantee follows.
