# Measured comparison

Before: `streaming-before.json`; after: `streaming-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 7.65 | 7.89 | -3.1% | 2.180 | 2.110 | 5.2 | 153021 | 0 |
| archive-cbor | 216.89 | 186.41 | 14.1% | 68.447 | 62.691 | 14.8 | 249862 | 0 |
| persist-rewrite | 201.11 | 190.12 | 5.5% | 72.506 | 65.952 | 22.4 | 0 | 249862 |
| bootstrap-export | 202.43 | 204.89 | -1.2% | 77.049 | 70.793 | 18.2 | 250989 | 0 |

## graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 24.64 | 6.92 | 71.9% | 8.303 | 7.886 | 16.7 | 544530 | 0 |
| archive-cbor | 557.62 | 488.38 | 12.4% | 271.528 | 248.609 | 43.1 | 948637 | 0 |
| persist-rewrite | 451.28 | 411.16 | 8.9% | 287.321 | 261.423 | 74.2 | 0 | 948637 |
| bootstrap-export | 504.08 | 454.93 | 9.8% | 299.139 | 274.094 | 42.5 | 951733 | 0 |

## history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 4.24 | 3.88 | 8.4% | 1.519 | 1.421 | 3.1 | 160077 | 0 |
| archive-cbor | 58.66 | 50.82 | 13.4% | 19.308 | 17.560 | 9.7 | 144594 | 0 |
| persist-rewrite | 64.85 | 62.18 | 4.1% | 19.953 | 17.589 | 10.5 | 0 | 144594 |
| bootstrap-export | 58.21 | 53.02 | 8.9% | 24.657 | 22.494 | 9.4 | 145543 | 0 |

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 3.32 | 2.86 | 14.1% | 1.052 | 0.940 | 2.5 | 106105 | 0 |
| archive-cbor | 55.30 | 47.14 | 14.8% | 17.093 | 15.684 | 9.5 | 105525 | 0 |
| persist-rewrite | 59.34 | 52.67 | 11.2% | 17.955 | 16.256 | 10.1 | 0 | 105525 |
| bootstrap-export | 53.45 | 54.70 | -2.3% | 21.790 | 20.250 | 9.3 | 106349 | 0 |
