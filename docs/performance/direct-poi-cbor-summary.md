# Direct POI CBOR measurements

Inputs: `child-etags-after.json`, `direct-poi-cbor-after.json`. Same benchmark source, runtime/dependencies, graph/history inventories, exact outputs and bounded cache statistics checked.

Cold maps/root hashes are prepared outside each call; warm calls reuse one prefilled context. Import/root hashing are excluded. Document consumption is after measurement; complete CBOR/archive calls include output SHA-256. No build or test ran alongside either benchmark report.

No dedicated host or affinity; timings and process peaks are descriptive. Retention is an approximate post-collection heap delta while the same map/root remains alive, not an isolated cache size or a global memory bound. Negative deltas reflect collectible setup objects.

## graph-128

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 27.8912 | 17.0804 | -38.8% | 70.253 | 75.382 |
| archive-cbor-stream | 27.0897 | 16.2788 | -39.9% | 68.797 | 69.657 |
| archive-json | 2.1099 | 2.1099 | +0.0% | 7.658 | 7.699 |
| archive-json-stream | 1.1887 | 1.1887 | +0.0% | 7.338 | 7.350 |
| child-etags-cbor-cold | 17.1589 | 12.4332 | -27.5% | 46.997 | 45.352 |
| child-etags-cbor-warm | 8.8600 | 5.2565 | -40.7% | 18.269 | 18.269 |
| child-etags-json-cold | 10.8896 | 9.7763 | -10.2% | 35.463 | 32.102 |
| child-etags-json-warm | 2.5908 | 2.5908 | +0.0% | 7.412 | 6.355 |
| snapshot-cbor | 8.8600 | 5.2565 | -40.7% | 19.393 | 19.989 |
| snapshot-json-document | 2.5908 | 2.5908 | +0.0% | 7.534 | 6.915 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 14.66 | 10.25 | 88.31 | 87.61 |
| snapshot-cbor | 11.99 | 8.50 | 80.98 | 80.47 |
| archive-cbor-stream | 12.46 | 13.27 | 87.74 | 86.97 |

Before: **322 pairs / 77,130 logical bytes**; collected managed delta median **124.64 KiB**, range **124.64–124.64 KiB** (8 workers).

After: **322 pairs / 77,130 logical bytes**; collected managed delta median **84.54 KiB**, range **84.54–124.64 KiB** (8 workers).

## graph-512

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 109.3138 | 66.0307 | -39.6% | 239.549 | 133.331 |
| archive-cbor-stream | 106.1826 | 63.0076 | -40.7% | 191.482 | 122.393 |
| archive-json | 7.9921 | 8.0979 | +1.3% | 15.197 | 25.999 |
| archive-json-stream | 4.4525 | 4.4525 | +0.0% | 15.552 | 15.645 |
| child-etags-cbor-cold | 68.5113 | 49.5577 | -27.7% | 80.368 | 65.842 |
| child-etags-cbor-warm | 35.2245 | 20.8682 | -40.8% | 50.147 | 47.060 |
| child-etags-json-cold | 43.5118 | 39.0182 | -10.3% | 54.523 | 62.932 |
| child-etags-json-warm | 10.2250 | 10.2602 | +0.3% | 17.078 | 29.886 |
| snapshot-cbor | 35.2245 | 20.8330 | -40.9% | 82.194 | 41.804 |
| snapshot-json-document | 10.2954 | 10.2250 | -0.7% | 23.748 | 17.160 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 36.81 | 31.11 | 154.84 | 166.44 |
| snapshot-cbor | 29.64 | 27.93 | 117.29 | 117.22 |
| archive-cbor-stream | 40.13 | 30.44 | 126.15 | 123.54 |

Before: **1282 pairs / 308,666 logical bytes**; collected managed delta median **504.38 KiB**, range **424.27–504.38 KiB** (8 workers).

After: **1282 pairs / 308,666 logical bytes**; collected managed delta median **504.38 KiB**, range **344.27–504.38 KiB** (8 workers).

## history-64

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 10.1079 | 7.7879 | -23.0% | 30.223 | 51.312 |
| archive-cbor-stream | 9.4933 | 7.1725 | -24.4% | 30.180 | 31.945 |
| archive-json | 1.4213 | 1.4213 | +0.0% | 4.004 | 5.277 |
| archive-json-stream | 0.4836 | 0.4836 | +0.0% | 3.555 | 3.658 |
| child-etags-cbor-cold | 2.2895 | 1.6924 | -26.1% | 6.443 | 7.422 |
| child-etags-cbor-warm | 1.2190 | 0.7547 | -38.1% | 3.100 | 2.961 |
| child-etags-json-cold | 1.4369 | 1.3041 | -9.2% | 4.255 | 5.547 |
| child-etags-json-warm | 0.3664 | 0.3664 | +0.0% | 1.155 | 1.118 |
| snapshot-cbor | 1.2190 | 0.7547 | -38.1% | 3.423 | 2.947 |
| snapshot-json-document | 0.3664 | 0.3664 | +0.0% | 1.373 | 1.309 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 4.00 | 3.40 | 78.86 | 79.14 |
| snapshot-cbor | 2.75 | 2.30 | 80.89 | 80.17 |
| archive-cbor-stream | 7.22 | 8.98 | 80.97 | 84.32 |

Before: **42 pairs / 9,882 logical bytes**; collected managed delta median **-2.81 KiB**, range **-2.91–17.29 KiB** (8 workers).

After: **42 pairs / 9,882 logical bytes**; collected managed delta median **7.19 KiB**, range **-13.02–17.29 KiB** (8 workers).

## pruned-4

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 8.2326 | 5.9112 | -28.2% | 25.165 | 21.902 |
| archive-cbor-stream | 7.9381 | 5.6171 | -29.2% | 24.300 | 22.204 |
| archive-json | 0.9400 | 0.9400 | +0.0% | 3.138 | 3.152 |
| archive-json-stream | 0.4044 | 0.4044 | +0.0% | 2.788 | 3.024 |
| child-etags-cbor-cold | 2.2895 | 1.6924 | -26.1% | 6.984 | 6.816 |
| child-etags-cbor-warm | 1.2190 | 0.7547 | -38.1% | 3.600 | 3.076 |
| child-etags-json-cold | 1.4369 | 1.3041 | -9.2% | 4.829 | 5.227 |
| child-etags-json-warm | 0.3664 | 0.3664 | +0.0% | 1.184 | 1.524 |
| snapshot-cbor | 1.2190 | 0.7547 | -38.1% | 3.429 | 3.035 |
| snapshot-json-document | 0.3664 | 0.3664 | +0.0% | 1.323 | 1.384 |

| Operation | Before managed peak MiB | After managed peak MiB | Before working-set peak MiB | After working-set peak MiB |
| --- | ---: | ---: | ---: | ---: |
| child-etags-cbor-cold | 4.01 | 3.45 | 80.65 | 78.41 |
| snapshot-cbor | 2.78 | 2.34 | 82.25 | 80.50 |
| archive-cbor-stream | 9.55 | 7.24 | 80.62 | 82.56 |

Before: **42 pairs / 9,882 logical bytes**; collected managed delta median **17.29 KiB**, range **-2.81–17.29 KiB** (8 workers).

After: **42 pairs / 9,882 logical bytes**; collected managed delta median **-2.81 KiB**, range **-2.81–7.19 KiB** (8 workers).

