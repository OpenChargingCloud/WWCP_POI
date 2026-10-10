# Measured comparison

Before: `scaling-before.json`; after: `scaling-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| hash-json | 7.65 | 9.01 | -17.8% | 2.197 | 2.197 | 5.1 | 42131 | 0 |
| hash-cbor | 12.84 | 12.44 | 3.1% | 4.148 | 4.148 | 7.0 | 34900 | 0 |
| etag-recompute | 19.03 | 15.93 | 16.3% | 4.677 | 4.149 | 7.0 | 0 | 0 |
| static-update | 19.26 | 19.58 | -1.7% | 5.005 | 4.477 | 7.4 | 0 | 0 |
| runtime-capture | 38.27 | 18.15 | 52.6% | 11.156 | 5.951 | 8.8 | 0 | 0 |
| runtime-materialize | 173.72 | 171.87 | 1.1% | 42.087 | 36.875 | 16.9 | 0 | 0 |
| archive-json | 9.38 | 9.75 | -3.9% | 2.176 | 2.176 | 5.2 | 152854 | 0 |
| archive-cbor | 320.88 | 303.50 | 5.4% | 86.854 | 68.434 | 20.9 | 249691 | 0 |
| replay-json | 1369.81 | 1192.67 | 12.9% | 416.748 | 397.957 | 39.6 | 152854 | 0 |
| replay-cbor | 1466.84 | 1294.30 | 11.8% | 474.546 | 449.029 | 42.4 | 249691 | 0 |
| snapshot-prepare | 35.91 | 21.56 | 40.0% | 10.388 | 5.711 | 8.6 | 0 | 0 |
| persist-rewrite | 290.85 | 269.97 | 7.2% | 90.858 | 72.488 | 23.6 | 0 | 249691 |
| bootstrap-export | 307.63 | 270.95 | 11.9% | 95.452 | 77.032 | 19.0 | 250818 | 0 |
| bootstrap-transfer | 1564.29 | 1397.62 | 10.7% | 489.797 | 464.137 | 48.2 | 249691 | 250240 |
| catalog-create | 3.59 | 0.45 | 87.5% | 0.099 | 0.115 | 2.7 | 0 | 0 |

## graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| hash-json | 11.63 | 10.02 | 13.8% | 8.724 | 8.724 | 17.4 | 166864 | 0 |
| hash-cbor | 23.94 | 22.31 | 6.8% | 16.471 | 16.471 | 20.6 | 138225 | 0 |
| etag-recompute | 34.69 | 26.00 | 25.1% | 18.634 | 16.472 | 20.6 | 0 | 0 |
| static-update | 30.40 | 27.35 | 10.0% | 18.961 | 16.800 | 20.9 | 0 | 0 |
| runtime-capture | 82.40 | 39.29 | 52.3% | 43.104 | 22.307 | 18.3 | 0 | 0 |
| runtime-materialize | 387.78 | 340.99 | 12.1% | 165.255 | 144.393 | 52.1 | 0 | 0 |
| archive-json | 10.41 | 15.42 | -48.1% | 8.088 | 8.088 | 16.9 | 544338 | 0 |
| archive-cbor | 681.11 | 612.53 | 10.1% | 345.524 | 271.515 | 54.3 | 948466 | 0 |
| replay-json | 4493.77 | 3923.09 | 12.7% | 1642.155 | 1566.021 | 107.2 | 544338 | 0 |
| replay-cbor | 4979.14 | 4257.78 | 14.5% | 1872.490 | 1768.730 | 126.1 | 948466 | 0 |
| snapshot-prepare | 102.55 | 42.66 | 58.4% | 41.303 | 22.671 | 21.9 | 0 | 0 |
| persist-rewrite | 1135.45 | 550.20 | 51.5% | 361.053 | 287.260 | 80.6 | 0 | 948466 |
| bootstrap-export | 791.06 | 616.37 | 22.1% | 373.132 | 299.123 | 44.2 | 951562 | 0 |
| bootstrap-transfer | 4282.73 | 4326.70 | -1.0% | 1932.350 | 1828.934 | 133.8 | 948466 | 949389 |
| catalog-create | 3.70 | 0.45 | 87.9% | 0.099 | 0.115 | 8.2 | 0 | 0 |

## history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| hash-json | 1.11 | 1.17 | -5.2% | 0.318 | 0.318 | 1.8 | 5868 | 0 |
| hash-cbor | 1.81 | 1.81 | 0.2% | 0.580 | 0.580 | 2.1 | 4880 | 0 |
| etag-recompute | 2.44 | 1.95 | 19.8% | 0.656 | 0.581 | 2.1 | 0 | 0 |
| static-update | 3.73 | 3.56 | 4.8% | 0.984 | 0.908 | 2.4 | 0 | 0 |
| runtime-capture | 6.26 | 4.33 | 30.8% | 1.934 | 1.204 | 2.7 | 0 | 0 |
| runtime-materialize | 24.95 | 14.00 | 43.9% | 5.988 | 5.257 | 6.8 | 0 | 0 |
| archive-json | 4.73 | 4.69 | 0.9% | 1.509 | 1.509 | 3.2 | 159662 | 0 |
| archive-cbor | 75.11 | 70.66 | 5.9% | 23.434 | 19.288 | 8.6 | 144309 | 0 |
| replay-json | 635.32 | 667.49 | -5.1% | 149.437 | 140.408 | 11.9 | 159662 | 0 |
| replay-cbor | 730.37 | 655.72 | 10.2% | 164.325 | 153.876 | 17.8 | 144309 | 0 |
| snapshot-prepare | 4.66 | 3.00 | 35.7% | 1.493 | 0.838 | 2.3 | 0 | 0 |
| persist-rewrite | 85.22 | 79.10 | 7.2% | 24.072 | 19.926 | 9.6 | 0 | 144309 |
| bootstrap-export | 83.72 | 75.90 | 9.3% | 28.776 | 24.630 | 9.1 | 145258 | 0 |
| bootstrap-transfer | 794.82 | 647.16 | 18.6% | 173.218 | 162.729 | 18.0 | 144309 | 144825 |
| catalog-create | 3.41 | 0.43 | 87.4% | 0.099 | 0.115 | 1.1 | 0 | 0 |

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| hash-json | 1.27 | 1.33 | -4.6% | 0.318 | 0.318 | 1.8 | 5868 | 0 |
| hash-cbor | 1.93 | 2.42 | -25.3% | 0.580 | 0.580 | 2.1 | 4880 | 0 |
| etag-recompute | 2.51 | 2.44 | 3.0% | 0.656 | 0.581 | 2.1 | 0 | 0 |
| static-update | 3.86 | 3.39 | 12.3% | 0.984 | 0.908 | 2.4 | 0 | 0 |
| runtime-capture | 8.52 | 5.12 | 40.0% | 1.934 | 1.203 | 2.7 | 0 | 0 |
| runtime-materialize | 19.70 | 20.46 | -3.8% | 5.988 | 5.257 | 6.8 | 0 | 0 |
| archive-json | 3.50 | 4.06 | -16.0% | 1.044 | 1.044 | 2.5 | 105815 | 0 |
| archive-cbor | 70.98 | 65.17 | 8.2% | 21.219 | 17.072 | 9.0 | 105240 | 0 |
| replay-json | 662.06 | 540.11 | 18.4% | 121.006 | 114.678 | 12.2 | 105815 | 0 |
| replay-cbor | 631.64 | 568.57 | 10.0% | 134.651 | 126.899 | 12.4 | 105240 | 0 |
| snapshot-prepare | 5.88 | 4.23 | 28.0% | 1.710 | 1.055 | 2.6 | 0 | 0 |
| persist-rewrite | 95.56 | 73.64 | 22.9% | 22.073 | 17.927 | 9.9 | 0 | 105240 |
| bootstrap-export | 78.81 | 61.30 | 22.2% | 25.909 | 21.762 | 8.6 | 106064 | 0 |
| bootstrap-transfer | 690.20 | 570.13 | 17.4% | 140.902 | 133.074 | 19.6 | 105240 | 105776 |
| cold-read | 173.48 | 222.43 | -28.2% | 50.848 | 48.772 | 10.6 | 32611 | 0 |
| catalog-create | 3.39 | 0.43 | 87.2% | 0.099 | 0.115 | 1.1 | 0 | 0 |
