# Measured comparison

Before: `direct-archive-changeset-scaling-before.json`; after: `direct-archive-changeset-scaling-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 10.57 | 8.33 | 21.2% | 2.110 | 2.110 | 5.1 | 153021 | 0 |
| archive-json-stream | 9.84 | 9.37 | 4.7% | 1.189 | 1.189 | 4.2 | 153021 | 0 |
| archive-cbor | 83.16 | 83.78 | -0.7% | 17.749 | 17.798 | 14.8 | 249862 | 0 |
| archive-cbor-stream | 88.85 | 80.36 | 9.6% | 17.027 | 17.075 | 14.1 | 249862 | 0 |
| snapshot-cbor | 24.90 | 19.61 | 21.2% | 5.257 | 5.257 | 8.5 | 78600 | 0 |
| bootstrap-export | 83.16 | 90.16 | -8.4% | 25.930 | 25.978 | 14.4 | 250989 | 0 |

## graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 7.78 | 26.33 | -238.4% | 7.886 | 8.098 | 16.9 | 544530 | 0 |
| archive-json-stream | 32.18 | 16.39 | 49.1% | 4.558 | 4.452 | 13.3 | 544530 | 0 |
| archive-cbor | 166.59 | 201.71 | -21.1% | 68.921 | 68.969 | 35.7 | 948637 | 0 |
| archive-cbor-stream | 178.13 | 155.25 | 12.8% | 66.032 | 66.081 | 38.0 | 948637 | 0 |
| snapshot-cbor | 54.59 | 34.41 | 37.0% | 20.868 | 20.833 | 27.9 | 311525 | 0 |
| bootstrap-export | 186.74 | 215.89 | -15.6% | 94.612 | 94.660 | 38.6 | 951733 | 0 |

## history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 4.01 | 4.25 | -6.0% | 1.421 | 1.421 | 3.1 | 160077 | 0 |
| archive-json-stream | 3.87 | 4.20 | -8.5% | 0.484 | 0.484 | 2.2 | 160077 | 0 |
| archive-cbor | 32.94 | 36.31 | -10.2% | 7.659 | 8.014 | 9.8 | 144594 | 0 |
| archive-cbor-stream | 29.59 | 30.71 | -3.8% | 7.045 | 7.391 | 9.2 | 144594 | 0 |
| snapshot-cbor | 3.10 | 2.99 | 3.4% | 0.755 | 0.755 | 2.4 | 10782 | 0 |
| bootstrap-export | 35.67 | 29.87 | 16.3% | 12.594 | 12.941 | 8.5 | 145543 | 0 |

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 4.00 | 3.92 | 2.0% | 0.940 | 0.940 | 2.5 | 106105 | 0 |
| archive-json-stream | 3.16 | 3.54 | -12.0% | 0.404 | 0.404 | 2.0 | 106105 | 0 |
| archive-cbor | 27.71 | 24.15 | 12.8% | 5.996 | 6.174 | 7.8 | 105525 | 0 |
| archive-cbor-stream | 28.76 | 26.04 | 9.5% | 5.661 | 5.840 | 7.5 | 105525 | 0 |
| snapshot-cbor | 3.95 | 3.00 | 24.1% | 0.755 | 0.755 | 2.4 | 10782 | 0 |
| bootstrap-export | 28.64 | 28.42 | 0.7% | 10.521 | 10.700 | 7.3 | 106349 | 0 |
