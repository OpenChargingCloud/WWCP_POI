# Archive decoder and replay baseline

Input: `archive-recovery-baseline.json`. Seven shapes/all four profiles, seven operations, two fresh processes/five warmups/five samples: 98 workers / 490 measured calls.

Every output and repeated process inventory agrees. Six existing graph/history/pruned/signed-batch CBOR inputs match the preceding export reports exactly in digest, byte count and static/history inventory. The boundary-only fixture is new. This is a fresh baseline, not a before/after performance comparison.

Input construction, signing, production-stage delegate binding and prepared stage inputs are setup. Complete recovery includes the actual production scanner, document/model decoding, trusted signature callbacks and replay. Actual restored histories remain live until verification/disposal outside measurement. Tree re-encoding, exact model fingerprints, archive-byte/peer comparison, every branch state and fresh/local runtime checks occur after each call. Prior results are released before the next measurement.

Model-only envelope composition calls the actual private field/profile/model parsers; it is benchmark code. Complete profiles decode their commits eagerly; boundary production paths decode suffix commits lazily during restore. The model-only stage makes that suffix eager. Restore-from-model calls the actual production restore method and includes its repeated trust/state/ancestry checks. Signature-only verifies every equal peer once without applying state. Stage medians cannot be added, subtracted or interpreted as shares of complete recovery.

OutputBytes is the consumed input size for complete JSON/CBOR recovery and CBOR scan, zero for object-only stages. No reader operation emits wire bytes. The result binds input CBOR, exact decoded model, peer count or recovered head as appropriate.

No build/test from this task overlaps the formal series. The host is not dedicated or affinity controlled. Elapsed times and 10 ms sampled peaks are descriptive; setup inputs/verification can affect lifetime memory. Allocation counts cover the operation thread; CPU counts cover the process including its sampling thread. Largest sampled managed/working-set peaks are not exact operation peaks or production capacity.

| Shape | Profile | CBOR bytes | JSON UTF-8 bytes | Commits | Snapshots | Commit/batch peers | Largest batch |
| --- | --- | ---: | ---: | ---: | ---: | --- | ---: |
| recover-batch-1-peers-1 | wwcp-poi-history-v1 | 4144 | 4399 | 2 | 0 | 2/1 | 1 |
| recover-batch-2048-peers-4 | wwcp-poi-history-v1 | 613720 | 540479 | 2 | 0 | 8/4 | 2048 |
| recover-boundary-16 | wwcp-poi-history-v3 | 30343 | 25339 | 8 | 2 | 8/6 | 1 |
| recover-graph-128 | wwcp-poi-history-v2 | 249862 | 153021 | 13 | 2 | 13/10 | 1 |
| recover-graph-512 | wwcp-poi-history-v2 | 948637 | 544530 | 13 | 2 | 13/10 | 1 |
| recover-history-64 | wwcp-poi-history-v2 | 144594 | 160077 | 77 | 4 | 77/72 | 1 |
| recover-pruned-4 | wwcp-poi-history-v4 | 105525 | 106105 | 42 | 5 | 42/37 | 1 |

| Shape | Operation | Median ms | Min–max ms | Median process CPU ms | Allocated MiB | Largest sampled managed MiB | Largest sampled working set MiB |
| --- | --- | ---: | --- | ---: | ---: | ---: | ---: |
| recover-batch-1-peers-1 | read-cbor | 16.802 | 15.656–22.937 | 15.625 | 5.2363 | 6.35 | 72.11 |
| recover-batch-1-peers-1 | read-cbor-limits | 0.030 | 0.029–0.034 | 0.000 | 0.0007 | 1.09 | 69.18 |
| recover-batch-1-peers-1 | read-cbor-model | 5.673 | 5.438–8.458 | 0.000 | 1.8208 | 2.94 | 69.48 |
| recover-batch-1-peers-1 | read-cbor-tree | 0.120 | 0.115–0.175 | 0.000 | 0.0507 | 1.15 | 69.11 |
| recover-batch-1-peers-1 | read-json | 22.538 | 18.670–31.526 | 46.875 | 4.8948 | 6.01 | 71.38 |
| recover-batch-1-peers-1 | read-restore | 10.693 | 9.891–12.002 | 7.812 | 3.2599 | 4.38 | 70.61 |
| recover-batch-1-peers-1 | read-signatures | 1.046 | 1.020–1.204 | 0.000 | 0.0923 | 1.21 | 69.20 |
| recover-batch-2048-peers-4 | read-cbor | 2388.873 | 2288.890–2571.543 | 3250.000 | 1662.9300 | 98.47 | 201.55 |
| recover-batch-2048-peers-4 | read-cbor-limits | 1.694 | 1.038–2.052 | 0.000 | 0.0007 | 15.76 | 274.71 |
| recover-batch-2048-peers-4 | read-cbor-model | 701.394 | 679.573–797.241 | 968.750 | 345.0791 | 95.30 | 200.93 |
| recover-batch-2048-peers-4 | read-cbor-tree | 13.537 | 10.350–22.717 | 15.625 | 9.1661 | 20.79 | 188.62 |
| recover-batch-2048-peers-4 | read-json | 2345.272 | 2211.418–3424.939 | 3132.812 | 1580.9354 | 99.91 | 200.99 |
| recover-batch-2048-peers-4 | read-restore | 2066.028 | 1889.280–3094.514 | 2773.438 | 1294.6825 | 88.02 | 198.69 |
| recover-batch-2048-peers-4 | read-signatures | 80.241 | 75.072–101.637 | 117.188 | 70.6085 | 39.37 | 136.86 |
| recover-boundary-16 | read-cbor | 174.623 | 150.027–337.086 | 265.625 | 47.7422 | 11.64 | 84.19 |
| recover-boundary-16 | read-cbor-limits | 0.158 | 0.131–0.189 | 0.000 | 0.0007 | 1.86 | 75.49 |
| recover-boundary-16 | read-cbor-model | 90.781 | 77.679–105.461 | 140.625 | 23.1454 | 10.45 | 80.84 |
| recover-boundary-16 | read-cbor-tree | 0.797 | 0.773–0.881 | 0.000 | 0.3921 | 2.25 | 77.29 |
| recover-boundary-16 | read-json | 183.613 | 173.567–201.797 | 281.250 | 43.4435 | 10.43 | 78.32 |
| recover-boundary-16 | read-restore | 105.609 | 90.511–136.334 | 156.250 | 24.2253 | 11.48 | 83.95 |
| recover-boundary-16 | read-signatures | 5.913 | 5.722–6.185 | 0.000 | 0.8145 | 2.86 | 74.02 |
| recover-graph-128 | read-cbor | 697.462 | 632.878–733.701 | 1218.750 | 426.2697 | 42.09 | 136.62 |
| recover-graph-128 | read-cbor-limits | 1.632 | 1.128–1.676 | 0.000 | 0.0007 | 4.60 | 135.31 |
| recover-graph-128 | read-cbor-model | 509.889 | 455.018–571.911 | 867.188 | 250.7012 | 47.34 | 133.69 |
| recover-graph-128 | read-cbor-tree | 8.335 | 6.523–10.034 | 0.000 | 3.3450 | 7.92 | 122.06 |
| recover-graph-128 | read-json | 618.449 | 589.012–657.313 | 1179.688 | 378.3821 | 39.03 | 135.06 |
| recover-graph-128 | read-restore | 339.801 | 296.702–375.389 | 593.750 | 170.7813 | 32.80 | 125.56 |
| recover-graph-128 | read-signatures | 12.915 | 11.384–14.871 | 15.625 | 3.3534 | 9.84 | 138.97 |
| recover-graph-512 | read-cbor | 8494.509 | 4875.283–11537.655 | 6656.250 | 1687.9206 | 126.74 | 208.07 |
| recover-graph-512 | read-cbor-limits | 4.318 | 4.239–4.608 | 0.000 | 0.0007 | 14.68 | 231.20 |
| recover-graph-512 | read-cbor-model | 3015.635 | 1879.252–6191.776 | 3476.562 | 999.3562 | 116.97 | 206.45 |
| recover-graph-512 | read-cbor-tree | 21.598 | 13.923–34.837 | 31.250 | 12.7647 | 23.20 | 282.34 |
| recover-graph-512 | read-json | 3956.478 | 3383.072–7419.603 | 5242.188 | 1501.8238 | 100.06 | 204.11 |
| recover-graph-512 | read-restore | 1558.976 | 1294.081–2902.747 | 2046.875 | 667.0846 | 97.00 | 197.06 |
| recover-graph-512 | read-signatures | 20.818 | 13.731–81.300 | 15.625 | 11.2876 | 33.50 | 255.16 |
| recover-history-64 | read-cbor | 316.004 | 255.655–389.689 | 578.125 | 144.8399 | 17.08 | 102.74 |
| recover-history-64 | read-cbor-limits | 0.753 | 0.571–1.063 | 0.000 | 0.0007 | 2.64 | 87.68 |
| recover-history-64 | read-cbor-model | 174.851 | 137.229–230.185 | 312.500 | 60.8890 | 12.97 | 93.93 |
| recover-history-64 | read-cbor-tree | 4.352 | 3.757–5.141 | 0.000 | 1.7860 | 4.45 | 87.07 |
| recover-history-64 | read-json | 249.144 | 214.784–289.258 | 406.250 | 132.3561 | 12.23 | 95.11 |
| recover-history-64 | read-restore | 245.775 | 185.348–295.318 | 453.125 | 81.9433 | 12.83 | 98.02 |
| recover-history-64 | read-signatures | 56.627 | 47.520–64.344 | 85.938 | 5.4054 | 8.66 | 88.02 |
| recover-pruned-4 | read-cbor | 343.582 | 264.312–474.423 | 648.438 | 121.4217 | 14.65 | 97.04 |
| recover-pruned-4 | read-cbor-limits | 0.430 | 0.420–0.570 | 0.000 | 0.0026 | 2.19 | 87.21 |
| recover-pruned-4 | read-cbor-model | 212.202 | 166.178–263.562 | 351.562 | 59.4540 | 11.94 | 93.12 |
| recover-pruned-4 | read-cbor-tree | 3.699 | 2.807–5.221 | 0.000 | 1.3180 | 3.53 | 84.35 |
| recover-pruned-4 | read-json | 225.525 | 186.005–272.775 | 414.062 | 109.8996 | 11.44 | 95.57 |
| recover-pruned-4 | read-restore | 264.777 | 140.940–378.882 | 476.562 | 60.9034 | 11.68 | 93.95 |
| recover-pruned-4 | read-signatures | 29.214 | 26.277–33.391 | 46.875 | 3.4115 | 6.14 | 88.27 |

## Collected recovery retention

One post-measurement probe per read-cbor worker collects before/after holding one additional recovered history, input/source held on both sides. It checks every branch state, inventory and fresh head runtime without re-exporting an archive. GC.KeepAlive preserves the recovered/source histories through collection. These approximate process-heap deltas include retained model/state/runtime objects and overhead; they are not logical cache payload sizes or total-memory bounds. Other objects can become collectible, including negative deltas on small inputs.

| Shape | Median additional managed MiB | Min–max MiB |
| --- | ---: | --- |
| recover-batch-1-peers-1 | 0.0725 | 0.0724–0.0725 |
| recover-batch-2048-peers-4 | 11.5623 | 11.4093–11.7153 |
| recover-boundary-16 | 0.4890 | 0.4786–0.4995 |
| recover-graph-128 | 3.4028 | 3.3592–3.4464 |
| recover-graph-512 | 13.4308 | 13.4303–13.4314 |
| recover-history-64 | 1.0364 | 1.0347–1.0380 |
| recover-pruned-4 | 0.8964 | 0.8921–0.9008 |

## Exact preceding input bindings

| Current shape | Preceding export shape |
| --- | --- |
| recover-batch-1-peers-1 | archive-batch-1-peers-1 |
| recover-batch-2048-peers-4 | archive-batch-2048-peers-4 |
| recover-graph-128 | graph-128 |
| recover-graph-512 | graph-512 |
| recover-history-64 | history-64 |
| recover-pruned-4 | pruned-4 |
