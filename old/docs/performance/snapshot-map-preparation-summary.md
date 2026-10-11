# Normalized immutable snapshot map preparation: matched recovery comparison

Before `validation-projection-after.json` / `validation-projection-after-binding.json`; after `snapshot-map-preparation-after.json` / `snapshot-map-preparation-binding.json`.

Eight rich 16/64-EVSE shapes, all four archive profiles and four recovery stages; two fresh sequential workers, three warmups and three measured calls per worker: 64 workers / 192 measured calls per side. The preceding formal series is preserved. C# harness, shared dependencies, runtime configuration and manifest have identical bytes. Production sources and DLLs differ under explicit bindings.

All prepared archive bytes/ETags, complete graph/value/reference inventories, original signature peers, both branch states and complete recovered model/head results match. The fixed 32 reference artifacts remain unchanged.

Fresh models retain the complete domain parser, metadata completion and reference checks. Normalized exact property equality permits reuse of immutable property values, dictionaries, child sets, entity records and map branches. Removed identities and reference consumers are removed; all current references are checked again. Unchanged reverse indexes are reused. Representation binding reads stable typed identities directly instead of serializing each child subtree again. No mutable JSON/model/runtime/trust cache is added.

Stages cover different work and are not additive shares. Model-only controls eagerly decode prepared suffix models; production v3/v4 suffix recovery remains lazy. Setup, warmup, full byte/branch/runtime controls and disposal are excluded from measured calls. Current policy and every crypto/authority check remain inside recovery.

Allocations are exact operation-thread cumulative counters, not resident RAM. Time/CPU and sampled/collected process memory are descriptive on a shared Windows desktop without affinity control. Historical before and new after runs occur at different times; no task builds/tests/profiles overlap the new formal series. No production capacity, constant total-memory or cancellation-latency bound follows.

## Operation-thread allocations and elapsed time

| Shape | Stage | Before MiB | After MiB | Allocation change | Before ms | After ms | Time change |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| domain-v1-evses-16 | read-cbor | 134.1869 | 111.9383 | -16.58% | 423.670 | 356.790 | -15.79% |
| domain-v1-evses-16 | read-cbor-model | 41.1559 | 34.0571 | -17.25% | 126.837 | 132.033 | +4.10% |
| domain-v1-evses-16 | read-json | 124.6337 | 102.3987 | -17.84% | 401.064 | 394.095 | -1.74% |
| domain-v1-evses-16 | read-restore | 91.3228 | 76.2142 | -16.54% | 310.740 | 292.618 | -5.83% |
| domain-v1-evses-64 | read-cbor | 408.8645 | 329.6300 | -19.38% | 721.836 | 643.795 | -10.81% |
| domain-v1-evses-64 | read-cbor-model | 134.8057 | 109.1705 | -19.02% | 300.058 | 263.640 | -12.14% |
| domain-v1-evses-64 | read-json | 380.1962 | 301.0694 | -20.81% | 645.773 | 627.936 | -2.76% |
| domain-v1-evses-64 | read-restore | 269.7631 | 216.3868 | -19.79% | 546.304 | 479.231 | -12.28% |
| domain-v2-evses-16 | read-cbor | 212.1644 | 182.5200 | -13.97% | 539.163 | 459.257 | -14.82% |
| domain-v2-evses-16 | read-cbor-model | 84.7604 | 70.5505 | -16.76% | 275.208 | 292.658 | +6.34% |
| domain-v2-evses-16 | read-json | 192.3289 | 162.6074 | -15.45% | 505.217 | 435.507 | -13.80% |
| domain-v2-evses-16 | read-restore | 125.7198 | 110.0594 | -12.46% | 372.218 | 333.830 | -10.31% |
| domain-v2-evses-64 | read-cbor | 634.1758 | 528.7055 | -16.63% | 1012.668 | 836.506 | -17.40% |
| domain-v2-evses-64 | read-cbor-model | 276.8369 | 225.6499 | -18.49% | 459.416 | 388.472 | -15.44% |
| domain-v2-evses-64 | read-json | 573.9042 | 468.9227 | -18.29% | 892.932 | 763.355 | -14.51% |
| domain-v2-evses-64 | read-restore | 351.7913 | 298.1499 | -15.25% | 529.965 | 445.477 | -15.94% |
| domain-v3-evses-16 | read-cbor | 137.9797 | 115.7418 | -16.12% | 446.307 | 354.023 | -20.68% |
| domain-v3-evses-16 | read-cbor-model | 43.8596 | 36.7565 | -16.19% | 150.678 | 156.859 | +4.10% |
| domain-v3-evses-16 | read-json | 127.3317 | 105.1266 | -17.44% | 444.964 | 356.688 | -19.84% |
| domain-v3-evses-16 | read-restore | 93.7881 | 78.6595 | -16.13% | 288.477 | 262.922 | -8.86% |
| domain-v3-evses-64 | read-cbor | 419.5366 | 340.5612 | -18.82% | 687.250 | 599.435 | -12.78% |
| domain-v3-evses-64 | read-cbor-model | 142.8888 | 116.9994 | -18.12% | 294.887 | 245.466 | -16.76% |
| domain-v3-evses-64 | read-json | 387.5325 | 308.1788 | -20.48% | 645.819 | 563.626 | -12.73% |
| domain-v3-evses-64 | read-restore | 276.1063 | 222.9376 | -19.26% | 494.740 | 457.393 | -7.55% |
| domain-v4-evses-16 | read-cbor | 144.4191 | 122.0925 | -15.46% | 477.326 | 418.404 | -12.34% |
| domain-v4-evses-16 | read-cbor-model | 43.9976 | 36.8976 | -16.14% | 134.801 | 139.939 | +3.81% |
| domain-v4-evses-16 | read-json | 133.6943 | 111.3968 | -16.68% | 475.189 | 454.248 | -4.41% |
| domain-v4-evses-16 | read-restore | 100.0146 | 84.8202 | -15.19% | 318.042 | 311.862 | -1.94% |
| domain-v4-evses-64 | read-cbor | 436.1634 | 356.8346 | -18.19% | 732.062 | 571.308 | -21.96% |
| domain-v4-evses-64 | read-cbor-model | 142.9408 | 117.1678 | -18.03% | 269.529 | 231.406 | -14.14% |
| domain-v4-evses-64 | read-json | 404.1894 | 325.2726 | -19.52% | 672.257 | 522.634 | -22.26% |
| domain-v4-evses-64 | read-restore | 292.6777 | 239.0896 | -18.31% | 509.253 | 488.672 | -4.04% |

## Collected additional recovered-history memory

One additional recovered history is collected while prepared inputs/source history stay live. These approximate process-heap deltas include models, shared maps, runtime and retained states. Sharing normalized immutable entities can reduce this retained graph; temporary allocations and retained memory remain distinct measurements.

| Shape | Before MiB | After MiB | Change |
| --- | ---: | ---: | ---: |
| domain-v1-evses-16 | 0.9898 | 0.8292 | -16.22% |
| domain-v1-evses-64 | 3.2560 | 2.5022 | -23.15% |
| domain-v2-evses-16 | 1.2392 | 1.0854 | -12.41% |
| domain-v2-evses-64 | 4.0089 | 3.3323 | -16.88% |
| domain-v3-evses-16 | 1.0297 | 0.7902 | -23.26% |
| domain-v3-evses-64 | 3.2554 | 2.5852 | -20.59% |
| domain-v4-evses-16 | 1.0361 | 0.8190 | -20.96% |
| domain-v4-evses-64 | 3.2609 | 2.5901 | -20.57% |

Ordinary domain parsing, detached JSON preparation, property comparison, metrological normalization, crypto and fresh local runtime still allocate. Group-heavy and larger catalogs, longer histories and production concurrency need separate workloads.
