# Direct archive POI payload measurements

Inputs: `direct-archive-payload-after.json`, `preflight-allocation-after.json`. Same benchmark source, runtime/dependencies, graph/history inventories, exact outputs and bounded cache statistics checked.

Cold maps/root hashes are prepared outside each call; warm calls reuse one prefilled context. Import/root hashing are excluded. Document consumption is after measurement; complete CBOR/archive calls include output SHA-256. Archive calls use prepared histories; cold/warm refers to the separate snapshot controls. Bootstrap includes bounded frozen fragment capture. 80 workers / 400 samples per report. No benchmark-task build or test ran alongside either report. Other local .NET work was observed during this task.

No dedicated host or affinity; timings and process peaks are descriptive. Retention is an approximate post-collection heap delta while the same map/root remains alive, not an isolated cache size or a global memory bound. Negative deltas reflect collectible setup objects.

## graph-128

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 19.9628 | 17.7494 | -11.1% | 97.017 | 80.238 |
| archive-cbor-stream | 19.2402 | 17.0269 | -11.5% | 90.156 | 85.447 |
| archive-json | 2.1099 | 2.1099 | +0.0% | 24.915 | 9.755 |
| archive-json-stream | 1.1887 | 1.1887 | +0.0% | 15.567 | 10.107 |
| bootstrap-export | 28.1432 | 25.9299 | -7.9% | 100.569 | 96.891 |
| child-etags-cbor-cold | 12.4356 | 12.4470 | +0.1% | 52.710 | 55.010 |
| child-etags-cbor-warm | 5.2565 | 5.2565 | +0.0% | 23.107 | 21.256 |
| child-etags-json-cold | 9.7788 | 9.7812 | +0.0% | 43.846 | 43.355 |
| child-etags-json-warm | 2.5908 | 2.5908 | +0.0% | 9.897 | 6.829 |
| snapshot-cbor | 5.2565 | 5.2565 | +0.0% | 25.816 | 22.156 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 10.23 | 10.26 | 90.07 | 90.33 |
| snapshot-cbor | 8.51 | 8.50 | 83.06 | 86.44 |
| archive-cbor-stream | 10.42 | 14.03 | 89.11 | 86.88 |

Before: **322 pairs / 77,130 logical bytes**; collected managed delta median **84.54 KiB**, range **84.54–84.54 KiB** (8 workers).

After: **322 pairs / 77,130 logical bytes**; collected managed delta median **100.57 KiB**, range **84.54–124.64 KiB** (8 workers).

## graph-512

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 77.7588 | 68.9209 | -11.4% | 188.869 | 173.752 |
| archive-cbor-stream | 74.8704 | 66.0324 | -11.8% | 197.545 | 211.393 |
| archive-json | 7.8864 | 7.8864 | +0.0% | 12.037 | 10.039 |
| archive-json-stream | 4.4525 | 4.3467 | -2.4% | 16.827 | 7.319 |
| bootstrap-export | 103.4495 | 94.6116 | -8.5% | 204.670 | 178.269 |
| child-etags-cbor-cold | 49.5675 | 49.5773 | +0.0% | 78.523 | 65.954 |
| child-etags-cbor-warm | 20.8330 | 20.8330 | +0.0% | 45.361 | 33.990 |
| child-etags-json-cold | 39.0280 | 39.0378 | +0.0% | 73.731 | 55.391 |
| child-etags-json-warm | 10.2250 | 10.2250 | +0.0% | 31.730 | 21.813 |
| snapshot-cbor | 20.8683 | 20.8330 | -0.2% | 59.691 | 43.799 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 31.10 | 31.18 | 167.05 | 164.17 |
| snapshot-cbor | 27.96 | 27.93 | 114.97 | 110.51 |
| archive-cbor-stream | 34.40 | 28.25 | 121.44 | 120.05 |

Before: **1282 pairs / 308,666 logical bytes**; collected managed delta median **504.38 KiB**, range **504.38–504.38 KiB** (8 workers).

After: **1282 pairs / 308,666 logical bytes**; collected managed delta median **504.38 KiB**, range **424.27–504.38 KiB** (8 workers).

## history-64

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 8.1264 | 7.6587 | -5.8% | 34.082 | 34.766 |
| archive-cbor-stream | 7.5126 | 7.0450 | -6.2% | 39.452 | 34.263 |
| archive-json | 1.4213 | 1.4213 | +0.0% | 4.758 | 4.544 |
| archive-json-stream | 0.4836 | 0.4836 | +0.0% | 4.753 | 4.214 |
| bootstrap-export | 13.0612 | 12.5944 | -3.6% | 35.636 | 35.307 |
| child-etags-cbor-cold | 1.6927 | 1.6931 | +0.0% | 7.849 | 9.225 |
| child-etags-cbor-warm | 0.7547 | 0.7547 | +0.0% | 3.093 | 3.995 |
| child-etags-json-cold | 1.3044 | 1.3047 | +0.0% | 6.096 | 6.530 |
| child-etags-json-warm | 0.3664 | 0.3664 | +0.0% | 1.125 | 1.409 |
| snapshot-cbor | 0.7547 | 0.7547 | +0.0% | 3.508 | 3.237 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 3.41 | 3.41 | 79.27 | 79.58 |
| snapshot-cbor | 2.32 | 2.33 | 82.21 | 79.45 |
| archive-cbor-stream | 9.32 | 8.84 | 84.18 | 83.52 |

Before: **42 pairs / 9,882 logical bytes**; collected managed delta median **2.19 KiB**, range **-2.81–17.29 KiB** (8 workers).

After: **42 pairs / 9,882 logical bytes**; collected managed delta median **2.19 KiB**, range **-13.02–17.29 KiB** (8 workers).

## pruned-4

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 6.4633 | 5.9956 | -7.2% | 27.355 | 26.905 |
| archive-cbor-stream | 6.1289 | 5.6612 | -7.6% | 24.378 | 26.035 |
| archive-json | 0.9400 | 0.9400 | +0.0% | 3.402 | 4.222 |
| archive-json-stream | 0.4044 | 0.4044 | +0.0% | 3.763 | 3.744 |
| bootstrap-export | 10.9883 | 10.5206 | -4.3% | 33.470 | 26.662 |
| child-etags-cbor-cold | 1.6927 | 1.6931 | +0.0% | 8.816 | 8.174 |
| child-etags-cbor-warm | 0.7547 | 0.7547 | +0.0% | 3.888 | 3.219 |
| child-etags-json-cold | 1.3044 | 1.3047 | +0.0% | 5.196 | 6.763 |
| child-etags-json-warm | 0.3664 | 0.3664 | +0.0% | 1.295 | 1.442 |
| snapshot-cbor | 0.7547 | 0.7547 | +0.0% | 3.165 | 3.720 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 3.45 | 3.45 | 81.77 | 79.59 |
| snapshot-cbor | 2.34 | 2.35 | 82.91 | 82.73 |
| archive-cbor-stream | 7.77 | 7.28 | 83.00 | 84.31 |

Before: **42 pairs / 9,882 logical bytes**; collected managed delta median **-2.81 KiB**, range **-2.81–-2.81 KiB** (8 workers).

After: **42 pairs / 9,882 logical bytes**; collected managed delta median **-2.91 KiB**, range **-13.02–17.29 KiB** (8 workers).

