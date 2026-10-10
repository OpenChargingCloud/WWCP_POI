# Root/head snapshot reconstruction measurements

Before `canonical-preparation-after.json`; after `snapshot-reconstruction-after.json`. Seven shapes/all four profiles, four operations, two fresh processes/three warmups/three samples per side: 112 workers / 336 measured calls in total.

The unchanged C# harness and assembly, runtime configuration, shared dependency bytes and method match. Every repeated output agrees; exact prepared archive identities, byte counts and inventories also bind `mapped-file-controls.json`. Model fingerprints include all original peer envelopes; complete recovery checks every retained branch state, exact archive bytes/peers and fresh local runtime outside measurement.

Before uses preserved current binaries from the bounded canonical preparation package. Scoped source reconstruction binds their original bytes; after uses the newly built production binary with the same harness. Sources and binaries are recorded separately. Workers run sequentially; no task build/test overlaps the formal series. The shared Windows desktop is not dedicated or affinity controlled.

Model-only composes the envelope using production parsers and eagerly decodes suffix models. Actual v3/v4 recovery still decodes suffix commits lazily during replay. Prepared-model restore includes fresh trust, ancestry and state checks. Individual model/JSON trees, canonicalization, cryptography and retained snapshots remain. The signature-only probe calls individual peer callbacks without a preparation scope, and retains the preceding complete envelope factory as a control. Actual recovery/restore also uses fresh model/runtime objects and conditionally retains exact immutable snapshots; it joins a bounded scope across model preparation and replay. Stage medians cannot be added or treated as full-recovery shares.

| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| recover-batch-1-peers-1 | read-cbor | 4.9143 | 4.5364 | -7.69% | 20.361 | 14.527 |
| recover-batch-1-peers-1 | read-cbor-model | 1.7324 | 1.7324 | +0.00% | 8.890 | 5.490 |
| recover-batch-1-peers-1 | read-restore | 3.1041 | 2.7259 | -12.19% | 12.833 | 8.938 |
| recover-batch-1-peers-1 | read-signatures | 0.0930 | 0.0930 | +0.00% | 1.175 | 1.172 |
| recover-batch-2048-peers-4 | read-cbor | 1524.6701 | 1457.7567 | -4.39% | 2495.105 | 1978.530 |
| recover-batch-2048-peers-4 | read-cbor-model | 321.6236 | 321.5557 | -0.02% | 561.468 | 524.098 |
| recover-batch-2048-peers-4 | read-restore | 1194.7241 | 1128.5096 | -5.54% | 1640.624 | 1371.717 |
| recover-batch-2048-peers-4 | read-signatures | 70.6111 | 70.6111 | +0.00% | 87.486 | 84.902 |
| recover-boundary-16 | read-cbor | 43.2326 | 40.9993 | -5.17% | 369.066 | 162.736 |
| recover-boundary-16 | read-cbor-model | 21.6440 | 21.6467 | +0.01% | 64.244 | 66.534 |
| recover-boundary-16 | read-restore | 21.5204 | 19.2926 | -10.35% | 72.443 | 64.467 |
| recover-boundary-16 | read-signatures | 0.8176 | 0.8176 | +0.00% | 6.143 | 6.110 |
| recover-graph-128 | read-cbor | 394.0014 | 377.2827 | -4.24% | 728.701 | 685.525 |
| recover-graph-128 | read-cbor-model | 233.1510 | 234.0359 | +0.38% | 534.454 | 470.906 |
| recover-graph-128 | read-restore | 159.0573 | 142.8487 | -10.19% | 395.342 | 360.150 |
| recover-graph-128 | read-signatures | 3.3762 | 3.3762 | +0.00% | 11.233 | 14.430 |
| recover-graph-512 | read-cbor | 1561.2854 | 1494.3186 | -4.29% | 3308.712 | 2632.591 |
| recover-graph-512 | read-cbor-model | 928.6688 | 929.1434 | +0.05% | 3077.463 | 1625.637 |
| recover-graph-512 | read-restore | 620.5269 | 553.6775 | -10.77% | 1312.250 | 960.291 |
| recover-graph-512 | read-signatures | 11.2927 | 11.2927 | +0.00% | 12.427 | 11.891 |
| recover-history-64 | read-cbor | 139.6927 | 137.5388 | -1.54% | 436.576 | 382.038 |
| recover-history-64 | read-cbor-model | 57.1821 | 57.1724 | -0.02% | 267.374 | 234.437 |
| recover-history-64 | read-restore | 81.7249 | 79.5921 | -2.61% | 326.178 | 361.843 |
| recover-history-64 | read-signatures | 5.4384 | 5.4384 | +0.00% | 45.741 | 46.517 |
| recover-pruned-4 | read-cbor | 108.7507 | 106.5761 | -2.00% | 476.396 | 377.887 |
| recover-pruned-4 | read-cbor-model | 55.6926 | 55.6930 | +0.00% | 238.485 | 210.216 |
| recover-pruned-4 | read-restore | 52.5123 | 50.2701 | -4.27% | 211.416 | 201.025 |
| recover-pruned-4 | read-signatures | 3.4290 | 3.4290 | -0.00% | 28.186 | 26.223 |

## Collected additional recovered-history memory

The existing post-measurement collection holds one additional recovered history with source/input live on both sides. These process-heap deltas are approximate; they include model/state/runtime overhead, can be negative, and establish no total-memory bound. This package adds no persistent cache or retained mutable JSON tree.

| Shape | Before median MiB | After median MiB |
| --- | ---: | ---: |
| recover-batch-1-peers-1 | 0.0750 | 0.0740 |
| recover-batch-2048-peers-4 | 11.6525 | 11.5776 |
| recover-boundary-16 | 0.4971 | 0.4349 |
| recover-graph-128 | 3.4019 | 3.4336 |
| recover-graph-512 | 13.4209 | 13.4193 |
| recover-history-64 | 1.0079 | 1.0562 |
| recover-pruned-4 | 0.8867 | 0.8468 |

Elapsed times, process CPU and 10 ms sampled peaks in the raw reports remain descriptive. Managed allocation measures the operation thread; setup, verification and disposal stay outside it. These synthetic workloads establish neither production throughput nor cancellation latency. No universal timing or allocation improvement is inferred.
