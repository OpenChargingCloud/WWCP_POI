# Temporary validation projection reuse: matched recovery comparison

Before `domain-recovery-baseline.json` / `domain-recovery-binding.json`; after `validation-projection-after.json` / `validation-projection-after-binding.json`.

Each side contains eight rich 16/64-EVSE shapes across four profiles, four production recovery stages, two fresh sequential workers/three warmups/three samples: 64 workers / 192 measured calls per side. The preceding baseline remains unchanged. The complete C# harness, dependency DLLs, runtime configuration and manifest have identical bytes. Production source/DLL bytes differ under explicit bindings.

All prepared archive bytes/ETags, graph/value/reference inventories, two original peers per retained commit/batch, static heads and every branch-state fingerprint match. Complete model/peer results and recovered heads match in every group. These workloads contain no tariff groups; the separate *TG ID correction is covered by focused JSON/CBOR tests.

A pass reuses successful temporary ancestor projections and one station-reference view for one fixed immutable map. Ordinary parsers and all reference checks still run; failed projections are not cached. The pass ends before a later map/change and retains no runtime, trust or history cache.

Model-only eagerly decodes already parsed suffix models; actual v3/v4 suffix recovery remains lazy. Prepared-model restore and full CBOR/JSON include different work. Stages are not additive shares. Setup/warmup, full byte/branch/graph/runtime controls and disposal are outside calls; fresh current policy and crypto verification remain within recovery.

Operation-thread cumulative allocations are exact counters for measured synchronous calls, not resident RAM. Time/CPU/sampled peaks and collected heap deltas are descriptive measurements on a shared Windows desktop without affinity control. Historical before and new after series run at different times, outside task builds/tests. No production throughput/capacity, constant total-memory or cancellation-latency bound follows.

## Cumulative operation-thread allocation and elapsed time

| Shape | Stage | Before MiB | After MiB | Allocation change | Before ms | After ms | Time change |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| domain-v1-evses-16 | read-cbor | 186.3145 | 134.1869 | -27.98% | 331.503 | 423.670 | +27.80% |
| domain-v1-evses-16 | read-cbor-model | 41.1463 | 41.1559 | +0.02% | 109.393 | 126.837 | +15.95% |
| domain-v1-evses-16 | read-json | 176.7594 | 124.6337 | -29.49% | 319.116 | 401.064 | +25.68% |
| domain-v1-evses-16 | read-restore | 143.6541 | 91.3228 | -36.43% | 299.100 | 310.740 | +3.89% |
| domain-v1-evses-64 | read-cbor | 621.4921 | 408.8645 | -34.21% | 1031.354 | 721.836 | -30.01% |
| domain-v1-evses-64 | read-cbor-model | 134.5444 | 134.8057 | +0.19% | 218.516 | 300.058 | +37.32% |
| domain-v1-evses-64 | read-json | 593.0864 | 380.1962 | -35.90% | 805.200 | 645.773 | -19.80% |
| domain-v1-evses-64 | read-restore | 482.3120 | 269.7631 | -44.07% | 592.363 | 546.304 | -7.78% |
| domain-v2-evses-16 | read-cbor | 308.5489 | 212.1644 | -31.24% | 510.075 | 539.163 | +5.70% |
| domain-v2-evses-16 | read-cbor-model | 84.6361 | 84.7604 | +0.15% | 171.969 | 275.208 | +60.03% |
| domain-v2-evses-16 | read-json | 288.5730 | 192.3289 | -33.35% | 466.193 | 505.217 | +8.37% |
| domain-v2-evses-16 | read-restore | 222.2053 | 125.7198 | -43.42% | 337.920 | 372.218 | +10.15% |
| domain-v2-evses-64 | read-cbor | 1026.7926 | 634.1758 | -38.24% | 1275.733 | 1012.668 | -20.62% |
| domain-v2-evses-64 | read-cbor-model | 276.7380 | 276.8369 | +0.04% | 423.926 | 459.416 | +8.37% |
| domain-v2-evses-64 | read-json | 966.6668 | 573.9042 | -40.63% | 1164.418 | 892.932 | -23.32% |
| domain-v2-evses-64 | read-restore | 744.5731 | 351.7913 | -52.75% | 814.928 | 529.965 | -34.97% |
| domain-v3-evses-16 | read-cbor | 190.0555 | 137.9797 | -27.40% | 359.541 | 446.307 | +24.13% |
| domain-v3-evses-16 | read-cbor-model | 43.8664 | 43.8596 | -0.02% | 105.074 | 150.678 | +43.40% |
| domain-v3-evses-16 | read-json | 179.4863 | 127.3317 | -29.06% | 338.562 | 444.964 | +31.43% |
| domain-v3-evses-16 | read-restore | 146.0476 | 93.7881 | -35.78% | 278.795 | 288.477 | +3.47% |
| domain-v3-evses-64 | read-cbor | 632.4286 | 419.5366 | -33.66% | 783.239 | 687.250 | -12.26% |
| domain-v3-evses-64 | read-cbor-model | 142.5611 | 142.8888 | +0.23% | 199.154 | 294.887 | +48.07% |
| domain-v3-evses-64 | read-json | 600.4803 | 387.5325 | -35.46% | 727.891 | 645.819 | -11.28% |
| domain-v3-evses-64 | read-restore | 488.9403 | 276.1063 | -43.53% | 552.941 | 494.740 | -10.53% |
| domain-v4-evses-16 | read-cbor | 222.0006 | 144.4191 | -34.95% | 410.793 | 477.326 | +16.20% |
| domain-v4-evses-16 | read-cbor-model | 43.9891 | 43.9976 | +0.02% | 94.395 | 134.801 | +42.80% |
| domain-v4-evses-16 | read-json | 211.4374 | 133.6943 | -36.77% | 374.639 | 475.189 | +26.84% |
| domain-v4-evses-16 | read-restore | 177.6746 | 100.0146 | -43.71% | 308.907 | 318.042 | +2.96% |
| domain-v4-evses-64 | read-cbor | 754.3716 | 436.1634 | -42.18% | 864.458 | 732.062 | -15.32% |
| domain-v4-evses-64 | read-cbor-model | 142.7226 | 142.9408 | +0.15% | 205.569 | 269.529 | +31.11% |
| domain-v4-evses-64 | read-json | 722.3594 | 404.1894 | -44.05% | 803.537 | 672.257 | -16.34% |
| domain-v4-evses-64 | read-restore | 610.7982 | 292.6777 | -52.08% | 637.272 | 509.253 | -20.09% |

## Collected additional recovered-history memory

One additional history is collected after full CBOR recovery while prepared source/input stay live. These approximate process-heap deltas include model/maps/runtime/retained states; private validation pass projections are not retained. Heap decreases and increases are reported without a capacity claim.

| Shape | Before MiB | After MiB | Change |
| --- | ---: | ---: | ---: |
| domain-v1-evses-16 | 1.0286 | 0.9898 | -3.78% |
| domain-v1-evses-64 | 3.2469 | 3.2560 | +0.28% |
| domain-v2-evses-16 | 1.2785 | 1.2392 | -3.07% |
| domain-v2-evses-64 | 3.9949 | 4.0089 | +0.35% |
| domain-v3-evses-16 | 1.0087 | 1.0297 | +2.09% |
| domain-v3-evses-64 | 3.3239 | 3.2554 | -2.06% |
| domain-v4-evses-16 | 0.9987 | 1.0361 | +3.75% |
| domain-v4-evses-64 | 3.2565 | 3.2609 | +0.14% |

The model-only controls quantify unchanged decoding work. Remaining parsing, own-document preparation, immutable map rebuilding, crypto and final root/head model reconstruction still allocate. Larger registries, more parking/group consumers and production concurrency need separate workloads.
