# Canonical identity/signature preparation measurements

Before `canonical-preparation-before.json`; after `canonical-preparation-after.json`. Seven shapes/all four profiles, four operations, two fresh processes/three warmups/three samples per side: 112 workers / 336 measured calls in total.

The unchanged C# harness and assembly, runtime configuration, shared dependency bytes and method match. Every repeated output agrees; exact prepared archive identities, byte counts and inventories also bind `mapped-file-controls.json`. Model fingerprints include all original peer envelopes; complete recovery checks every retained branch state, exact archive bytes/peers and fresh local runtime outside measurement.

Before uses preserved current binaries from the model preparation package. Scoped source reconstruction binds their original bytes; after uses the newly built production binary with the same harness. Sources and binaries are recorded separately. Workers run sequentially; no task build/test overlaps the formal series. The shared Windows desktop is not dedicated or affinity controlled.

Model-only composes the envelope using production parsers and eagerly decodes suffix models. Actual v3/v4 recovery still decodes suffix commits lazily during replay. Prepared-model restore includes fresh trust, ancestry and state checks. Individual model/JSON trees, canonicalization, cryptography and retained snapshots remain. The signature-only probe calls individual peer callbacks without a preparation scope, and retains the preceding complete envelope factory as a control. Actual recovery/restore joins a bounded scope across model preparation and replay. Stage medians cannot be added or treated as full-recovery shares.

| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| recover-batch-1-peers-1 | read-cbor | 4.9340 | 4.9143 | -0.40% | 14.909 | 20.361 |
| recover-batch-1-peers-1 | read-cbor-model | 1.7321 | 1.7324 | +0.02% | 5.321 | 8.890 |
| recover-batch-1-peers-1 | read-restore | 3.0818 | 3.1041 | +0.72% | 10.080 | 12.833 |
| recover-batch-1-peers-1 | read-signatures | 0.0923 | 0.0930 | +0.72% | 1.021 | 1.175 |
| recover-batch-2048-peers-4 | read-cbor | 1581.0677 | 1524.6701 | -3.57% | 2119.726 | 2495.105 |
| recover-batch-2048-peers-4 | read-cbor-model | 321.6271 | 321.6236 | -0.00% | 516.249 | 561.468 |
| recover-batch-2048-peers-4 | read-restore | 1243.0075 | 1194.7241 | -3.88% | 1549.580 | 1640.624 |
| recover-batch-2048-peers-4 | read-signatures | 70.6085 | 70.6111 | +0.00% | 82.092 | 87.486 |
| recover-boundary-16 | read-cbor | 44.9707 | 43.2326 | -3.87% | 169.073 | 369.066 |
| recover-boundary-16 | read-cbor-model | 21.6431 | 21.6440 | +0.00% | 59.603 | 64.244 |
| recover-boundary-16 | read-restore | 22.7079 | 21.5204 | -5.23% | 69.562 | 72.443 |
| recover-boundary-16 | read-signatures | 0.8145 | 0.8176 | +0.38% | 5.734 | 6.143 |
| recover-graph-128 | read-cbor | 397.1188 | 394.0014 | -0.78% | 651.482 | 728.701 |
| recover-graph-128 | read-cbor-model | 232.9504 | 233.1510 | +0.09% | 459.266 | 534.454 |
| recover-graph-128 | read-restore | 159.2805 | 159.0573 | -0.14% | 351.042 | 395.342 |
| recover-graph-128 | read-signatures | 3.3889 | 3.3762 | -0.37% | 15.782 | 11.233 |
| recover-graph-512 | read-cbor | 1571.1970 | 1561.2854 | -0.63% | 2659.444 | 3308.712 |
| recover-graph-512 | read-cbor-model | 929.3805 | 928.6688 | -0.08% | 1584.133 | 3077.463 |
| recover-graph-512 | read-restore | 620.1177 | 620.5269 | +0.07% | 1014.573 | 1312.250 |
| recover-graph-512 | read-signatures | 11.2876 | 11.2927 | +0.05% | 11.440 | 12.427 |
| recover-history-64 | read-cbor | 140.7642 | 139.6927 | -0.76% | 444.850 | 436.576 |
| recover-history-64 | read-cbor-model | 57.1648 | 57.1821 | +0.03% | 173.095 | 267.374 |
| recover-history-64 | read-restore | 80.6867 | 81.7249 | +1.29% | 298.990 | 326.178 |
| recover-history-64 | read-signatures | 5.4054 | 5.4384 | +0.61% | 44.454 | 45.741 |
| recover-pruned-4 | read-cbor | 117.3930 | 108.7507 | -7.36% | 427.840 | 476.396 |
| recover-pruned-4 | read-cbor-model | 55.6836 | 55.6926 | +0.02% | 200.384 | 238.485 |
| recover-pruned-4 | read-restore | 59.5748 | 52.5123 | -11.85% | 236.931 | 211.416 |
| recover-pruned-4 | read-signatures | 3.4115 | 3.4290 | +0.51% | 28.380 | 28.186 |

## Collected additional recovered-history memory

The existing post-measurement collection holds one additional recovered history with source/input live on both sides. These process-heap deltas are approximate; they include model/state/runtime overhead, can be negative, and establish no total-memory bound. This package adds no persistent cache or retained mutable JSON tree.

| Shape | Before median MiB | After median MiB |
| --- | ---: | ---: |
| recover-batch-1-peers-1 | 0.0749 | 0.0750 |
| recover-batch-2048-peers-4 | 11.6051 | 11.6525 |
| recover-boundary-16 | 0.4986 | 0.4971 |
| recover-graph-128 | 3.4408 | 3.4019 |
| recover-graph-512 | 13.4218 | 13.4209 |
| recover-history-64 | 1.0341 | 1.0079 |
| recover-pruned-4 | 0.8931 | 0.8867 |

Elapsed times, process CPU and 10 ms sampled peaks in the raw reports remain descriptive. Managed allocation measures the operation thread; setup, verification and disposal stay outside it. These synthetic workloads establish neither production throughput nor cancellation latency. No universal timing or allocation improvement is inferred.
