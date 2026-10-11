# Child ETag cache measurements

Inputs: `child-etags-before.json`, `child-etags-after.json`. Matching benchmark source, runtime/dependencies, graph/history inventories, and exact output byte counts/digests checked.

Cold maps and root digests are prepared outside every sample. Warm calls share one prefilled context. Import and root hashing are excluded. Document consumption/byte verification occurs after measurement; complete CBOR and archive calls include output SHA-256.

Before timings partly overlap local build/test work; no exclusive host or affinity. Time medians are descriptive. Retained memory is a separate approximate process-managed heap delta after full collection, not allocation or a total-memory cap. Logical payload excludes object headers, digest-pair arrays, dictionary capacity and locks.

## graph-128

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 52.7057 | 27.8912 | -47.1% | 156.370 | 70.253 |
| archive-cbor-stream | 51.9041 | 27.0897 | -47.8% | 158.588 | 68.797 |
| archive-json | 2.1099 | 2.1099 | +0.0% | 7.552 | 7.658 |
| archive-json-stream | 1.1887 | 1.1887 | +0.0% | 7.232 | 7.338 |
| child-etags-cbor-cold | 17.1315 | 17.1589 | +0.2% | 46.527 | 46.997 |
| child-etags-cbor-warm | 17.1315 | 8.8600 | -48.3% | 45.994 | 18.269 |
| child-etags-json-cold | 10.8623 | 10.8896 | +0.3% | 33.789 | 35.463 |
| child-etags-json-warm | 10.8623 | 2.5908 | -76.1% | 32.137 | 7.412 |
| snapshot-cbor | 17.1315 | 8.8600 | -48.3% | 47.616 | 19.393 |
| snapshot-json-document | 10.8623 | 2.5908 | -76.1% | 36.575 | 7.534 |

Retained pairs: **322**; logical payload: **77,130 bytes**. Post-collection managed delta over 8 workers: median **124.64 KiB**, range **124.64–124.64 KiB**. No default-bound saturation.

## graph-512

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 208.6902 | 109.3138 | -47.6% | 337.859 | 239.549 |
| archive-cbor-stream | 205.6671 | 106.1826 | -48.4% | 437.688 | 191.482 |
| archive-json | 7.8864 | 7.9921 | +1.3% | 8.442 | 15.197 |
| archive-json-stream | 4.4525 | 4.4525 | +0.0% | 16.633 | 15.552 |
| child-etags-cbor-cold | 68.3860 | 68.5113 | +0.2% | 101.623 | 80.368 |
| child-etags-cbor-warm | 68.3860 | 35.2245 | -48.5% | 102.499 | 50.147 |
| child-etags-json-cold | 43.3865 | 43.5118 | +0.3% | 48.153 | 54.523 |
| child-etags-json-warm | 43.3865 | 10.2250 | -76.4% | 56.269 | 17.078 |
| snapshot-cbor | 68.4213 | 35.2245 | -48.5% | 156.012 | 82.194 |
| snapshot-json-document | 43.4217 | 10.2954 | -76.3% | 74.847 | 23.748 |

Retained pairs: **1282**; logical payload: **308,666 bytes**. Post-collection managed delta over 8 workers: median **504.38 KiB**, range **424.27–504.38 KiB**. No default-bound saturation.

## history-64

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 15.4335 | 10.1079 | -34.5% | 48.330 | 30.223 |
| archive-cbor-stream | 14.8189 | 9.4933 | -35.9% | 49.718 | 30.180 |
| archive-json | 1.4213 | 1.4213 | +0.0% | 3.778 | 4.004 |
| archive-json-stream | 0.4836 | 0.4836 | +0.0% | 3.856 | 3.555 |
| child-etags-cbor-cold | 2.2838 | 2.2895 | +0.2% | 6.755 | 6.443 |
| child-etags-cbor-warm | 2.2838 | 1.2190 | -46.6% | 6.198 | 3.100 |
| child-etags-json-cold | 1.4312 | 1.4369 | +0.4% | 4.679 | 4.255 |
| child-etags-json-warm | 1.4312 | 0.3664 | -74.4% | 4.860 | 1.155 |
| snapshot-cbor | 2.2838 | 1.2190 | -46.6% | 6.203 | 3.423 |
| snapshot-json-document | 1.4312 | 0.3664 | -74.4% | 4.273 | 1.373 |

Retained pairs: **42**; logical payload: **9,882 bytes**. Post-collection managed delta over 8 workers: median **-2.81 KiB**, range **-2.91–17.29 KiB**. No default-bound saturation.

## pruned-4

| Operation | Before MiB allocated | After MiB allocated | Change | Before median ms | After median ms |
| --- | ---: | ---: | ---: | ---: | ---: |
| archive-cbor | 13.5565 | 8.2326 | -39.3% | 44.704 | 25.165 |
| archive-cbor-stream | 13.2620 | 7.9381 | -40.1% | 48.384 | 24.300 |
| archive-json | 0.9400 | 0.9400 | +0.0% | 3.092 | 3.138 |
| archive-json-stream | 0.4044 | 0.4044 | +0.0% | 2.864 | 2.788 |
| child-etags-cbor-cold | 2.2838 | 2.2895 | +0.2% | 6.571 | 6.984 |
| child-etags-cbor-warm | 2.2838 | 1.2190 | -46.6% | 6.405 | 3.600 |
| child-etags-json-cold | 1.4312 | 1.4369 | +0.4% | 4.862 | 4.829 |
| child-etags-json-warm | 1.4312 | 0.3664 | -74.4% | 4.873 | 1.184 |
| snapshot-cbor | 2.2838 | 1.2190 | -46.6% | 6.524 | 3.429 |
| snapshot-json-document | 1.4312 | 0.3664 | -74.4% | 4.447 | 1.323 |

Retained pairs: **42**; logical payload: **9,882 bytes**. Post-collection managed delta over 8 workers: median **17.29 KiB**, range **-2.81–17.29 KiB**. No default-bound saturation.

