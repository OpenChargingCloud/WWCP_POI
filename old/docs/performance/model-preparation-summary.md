# Immutable model/replay preparation measurements

Before `model-preparation-before.json`; after `model-preparation-after.json`. Seven shapes/all four profiles, three operations, two fresh processes/three warmups/three samples per side: 84 workers / 252 measured calls in total.

The unchanged C# harness and assembly, runtime configuration, shared dependency bytes and method match. Every repeated output agrees; exact prepared archive identities, byte counts and inventories also bind `mapped-file-controls.json`. Model fingerprints include all original peer envelopes; complete recovery checks every retained branch state, exact archive bytes/peers and fresh local runtime outside measurement.

Before uses preserved binaries from the mapped recovery package. Scoped source reconstruction binds their original bytes; after uses the newly built production binary with the same harness. Sources and binaries are recorded separately. Workers run sequentially; no task build/test overlaps the formal series. The shared Windows desktop is not dedicated or affinity controlled.

Model-only composes the envelope using production parsers and eagerly decodes suffix models. Actual v3/v4 recovery still decodes suffix commits lazily during replay. Prepared-model restore includes fresh trust, ancestry and state checks. Individual model/JSON trees, canonicalization, cryptography and retained snapshots remain. Stage medians cannot be added or treated as full-recovery shares.

| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| recover-batch-1-peers-1 | read-cbor | 5.1997 | 4.9342 | -5.10% | 17.613 | 14.564 |
| recover-batch-1-peers-1 | read-cbor-model | 1.8209 | 1.7320 | -4.88% | 5.584 | 5.494 |
| recover-batch-1-peers-1 | read-restore | 3.2598 | 3.0817 | -5.46% | 10.017 | 10.107 |
| recover-batch-2048-peers-4 | read-cbor | 1656.6782 | 1580.4229 | -4.60% | 2365.334 | 2124.481 |
| recover-batch-2048-peers-4 | read-cbor-model | 345.2029 | 321.8505 | -6.76% | 743.382 | 539.127 |
| recover-batch-2048-peers-4 | read-restore | 1294.2025 | 1242.1901 | -4.02% | 1803.918 | 1583.114 |
| recover-boundary-16 | read-cbor | 47.9859 | 44.9804 | -6.26% | 177.814 | 170.228 |
| recover-boundary-16 | read-cbor-model | 23.1442 | 21.6452 | -6.48% | 69.001 | 61.008 |
| recover-boundary-16 | read-restore | 24.2186 | 22.7093 | -6.23% | 97.251 | 68.614 |
| recover-graph-128 | read-cbor | 427.0072 | 397.1897 | -6.98% | 894.969 | 667.479 |
| recover-graph-128 | read-cbor-model | 251.1631 | 233.9554 | -6.85% | 504.797 | 446.193 |
| recover-graph-128 | read-restore | 170.5367 | 159.0292 | -6.75% | 374.638 | 334.348 |
| recover-graph-512 | read-cbor | 1688.5149 | 1571.3675 | -6.94% | 3255.161 | 2752.237 |
| recover-graph-512 | read-cbor-model | 999.5688 | 929.1824 | -7.04% | 2187.997 | 1615.156 |
| recover-graph-512 | read-restore | 667.1094 | 620.1519 | -7.04% | 1268.755 | 1045.456 |
| recover-history-64 | read-cbor | 145.7441 | 140.7331 | -3.44% | 376.817 | 433.503 |
| recover-history-64 | read-cbor-model | 60.9201 | 57.1802 | -6.14% | 201.563 | 211.370 |
| recover-history-64 | read-restore | 82.1519 | 80.6466 | -1.83% | 326.847 | 314.224 |
| recover-pruned-4 | read-cbor | 122.4869 | 117.4037 | -4.15% | 395.011 | 405.298 |
| recover-pruned-4 | read-cbor-model | 59.4543 | 55.6860 | -6.34% | 262.672 | 189.466 |
| recover-pruned-4 | read-restore | 61.0728 | 59.5819 | -2.44% | 258.968 | 233.875 |

## Collected additional recovered-history memory

The existing post-measurement collection holds one additional recovered history with source/input live on both sides. These process-heap deltas are approximate; they include model/state/runtime overhead, can be negative, and establish no total-memory bound. This package adds no persistent cache or retained mutable JSON tree.

| Shape | Before median MiB | After median MiB |
| --- | ---: | ---: |
| recover-batch-1-peers-1 | 0.0750 | 0.0750 |
| recover-batch-2048-peers-4 | 11.7272 | 11.6309 |
| recover-boundary-16 | 0.4984 | 0.4998 |
| recover-graph-128 | 3.4381 | 3.4730 |
| recover-graph-512 | 13.4271 | 13.4202 |
| recover-history-64 | 1.0076 | 1.0309 |
| recover-pruned-4 | 0.8908 | 0.8997 |

Elapsed times, process CPU and 10 ms sampled peaks in the raw reports remain descriptive. Managed allocation measures the operation thread; setup, verification and disposal stay outside it. These synthetic workloads establish neither production throughput nor cancellation latency. No universal timing or allocation improvement is inferred.
