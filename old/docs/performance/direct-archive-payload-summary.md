# Direct archive POI payload measurements

Inputs: `direct-archive-payload-before.json`, `direct-archive-payload-after.json`. Same benchmark source, runtime/dependencies, graph/history inventories, exact outputs and bounded cache statistics checked.

Cold maps/root hashes are prepared outside each call; warm calls reuse one prefilled context. Import/root hashing are excluded. Document consumption is after measurement; complete CBOR/archive calls include output SHA-256. Archive calls use prepared histories; cold/warm refers to the separate snapshot controls. Bootstrap includes bounded frozen fragment capture. 80 workers / 400 samples per report. No build or test ran alongside either benchmark report.

No dedicated host or affinity; timings and process peaks are descriptive. Retention is an approximate post-collection heap delta while the same map/root remains alive, not an isolated cache size or a global memory bound. Negative deltas reflect collectible setup objects.

## graph-128

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 17.0316 | 19.9628 | +17.2% | 67.668 | 97.017 |
| archive-cbor-stream | 16.2299 | 19.2402 | +18.5% | 69.194 | 90.156 |
| archive-json | 2.1099 | 2.1099 | +0.0% | 7.693 | 24.915 |
| archive-json-stream | 1.1887 | 1.1887 | +0.0% | 7.474 | 15.567 |
| bootstrap-export | 25.1329 | 28.1432 | +12.0% | 80.098 | 100.569 |
| child-etags-cbor-cold | 12.4445 | 12.4356 | -0.1% | 49.007 | 52.710 |
| child-etags-cbor-warm | 5.2565 | 5.2565 | +0.0% | 20.498 | 23.107 |
| child-etags-json-cold | 9.7788 | 9.7788 | +0.0% | 35.647 | 43.846 |
| child-etags-json-warm | 2.5908 | 2.5908 | +0.0% | 6.497 | 9.897 |
| snapshot-cbor | 5.2565 | 5.2565 | +0.0% | 21.044 | 25.816 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 10.24 | 10.23 | 87.20 | 90.07 |
| snapshot-cbor | 8.50 | 8.51 | 84.67 | 83.06 |
| archive-cbor-stream | 13.22 | 10.42 | 85.54 | 89.11 |

Before: **322 pairs / 77,130 logical bytes**; collected managed delta median **84.54 KiB**, range **84.54–124.64 KiB** (8 workers).

After: **322 pairs / 77,130 logical bytes**; collected managed delta median **84.54 KiB**, range **84.54–84.54 KiB** (8 workers).

## graph-512

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 65.9818 | 77.7588 | +17.8% | 132.249 | 188.869 |
| archive-cbor-stream | 62.9589 | 74.8704 | +18.9% | 135.504 | 197.545 |
| archive-json | 7.9921 | 7.8864 | -1.3% | 17.938 | 12.037 |
| archive-json-stream | 4.5582 | 4.4525 | -2.3% | 23.984 | 16.827 |
| bootstrap-export | 91.5381 | 103.4495 | +13.0% | 136.032 | 204.670 |
| child-etags-cbor-cold | 49.5675 | 49.5675 | +0.0% | 63.275 | 78.523 |
| child-etags-cbor-warm | 20.8330 | 20.8330 | +0.0% | 37.260 | 45.361 |
| child-etags-json-cold | 39.0280 | 39.0280 | +0.0% | 50.081 | 73.731 |
| child-etags-json-warm | 10.2250 | 10.2250 | +0.0% | 17.618 | 31.730 |
| snapshot-cbor | 20.8330 | 20.8683 | +0.2% | 31.037 | 59.691 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 31.13 | 31.10 | 167.44 | 167.05 |
| snapshot-cbor | 27.93 | 27.96 | 118.70 | 114.97 |
| archive-cbor-stream | 30.42 | 34.40 | 110.55 | 121.44 |

Before: **1282 pairs / 308,666 logical bytes**; collected managed delta median **504.38 KiB**, range **424.27–504.38 KiB** (8 workers).

After: **1282 pairs / 308,666 logical bytes**; collected managed delta median **504.38 KiB**, range **504.38–504.38 KiB** (8 workers).

## history-64

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 7.4352 | 8.1264 | +9.3% | 31.287 | 34.082 |
| archive-cbor-stream | 6.8206 | 7.5126 | +10.1% | 39.415 | 39.452 |
| archive-json | 1.4213 | 1.4213 | +0.0% | 4.200 | 4.758 |
| archive-json-stream | 0.4836 | 0.4836 | +0.0% | 3.948 | 4.753 |
| bootstrap-export | 12.3692 | 13.0612 | +5.6% | 29.642 | 35.636 |
| child-etags-cbor-cold | 1.6927 | 1.6927 | +0.0% | 11.070 | 7.849 |
| child-etags-cbor-warm | 0.7547 | 0.7547 | +0.0% | 3.098 | 3.093 |
| child-etags-json-cold | 1.3044 | 1.3044 | +0.0% | 5.040 | 6.096 |
| child-etags-json-warm | 0.3664 | 0.3664 | +0.0% | 1.030 | 1.125 |
| snapshot-cbor | 0.7547 | 0.7547 | +0.0% | 3.048 | 3.508 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 3.40 | 3.41 | 80.06 | 79.27 |
| snapshot-cbor | 2.32 | 2.32 | 81.96 | 82.21 |
| archive-cbor-stream | 8.61 | 9.32 | 83.52 | 84.18 |

Before: **42 pairs / 9,882 logical bytes**; collected managed delta median **12.24 KiB**, range **-13.02–17.29 KiB** (8 workers).

After: **42 pairs / 9,882 logical bytes**; collected managed delta median **2.19 KiB**, range **-2.81–17.29 KiB** (8 workers).

## pruned-4

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 5.7307 | 6.4633 | +12.8% | 21.152 | 27.355 |
| archive-cbor-stream | 5.4362 | 6.1289 | +12.7% | 20.845 | 24.378 |
| archive-json | 0.9400 | 0.9400 | +0.0% | 3.136 | 3.402 |
| archive-json-stream | 0.4044 | 0.4044 | +0.0% | 2.860 | 3.763 |
| bootstrap-export | 10.2965 | 10.9883 | +6.7% | 31.245 | 33.470 |
| child-etags-cbor-cold | 1.6927 | 1.6927 | +0.0% | 6.374 | 8.816 |
| child-etags-cbor-warm | 0.7547 | 0.7547 | +0.0% | 3.167 | 3.888 |
| child-etags-json-cold | 1.3044 | 1.3044 | +0.0% | 5.178 | 5.196 |
| child-etags-json-warm | 0.3664 | 0.3664 | +0.0% | 1.498 | 1.295 |
| snapshot-cbor | 0.7547 | 0.7547 | +0.0% | 2.892 | 3.165 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 3.45 | 3.45 | 81.52 | 81.77 |
| snapshot-cbor | 2.34 | 2.34 | 82.49 | 82.91 |
| archive-cbor-stream | 7.08 | 7.77 | 84.28 | 83.00 |

Before: **42 pairs / 9,882 logical bytes**; collected managed delta median **-2.81 KiB**, range **-2.81–17.29 KiB** (8 workers).

After: **42 pairs / 9,882 logical bytes**; collected managed delta median **-2.81 KiB**, range **-2.81–-2.81 KiB** (8 workers).

