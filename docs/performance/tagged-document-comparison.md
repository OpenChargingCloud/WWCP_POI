# Measured comparison

Before: `encoding-baseline.json`; after: `tagged-document-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| snapshot-cbor | 50.29 | 46.39 | 7.8% | 20.460 | 17.132 | 13.3 | 78600 | 0 |
| snapshot-json-document | 38.05 | 33.53 | 11.9% | 14.191 | 10.862 | 11.9 | 116531 | 0 |
| snapshot-reading-paths | 1.08 | 1.09 | -1.3% | 0.263 | 0.263 | 5.6 | 0 | 0 |
| snapshot-json-canonical | 2.50 | 3.10 | -24.2% | 1.058 | 1.058 | 6.4 | 116531 | 0 |
| snapshot-cbor-tree | 3.48 | 3.38 | 3.0% | 2.331 | 2.331 | 8.3 | 101856 | 0 |
| snapshot-etag-tree | 2.38 | 2.90 | -22.1% | 1.237 | 1.237 | 6.8 | 78600 | 0 |
| snapshot-cbor-write | 2.16 | 2.73 | -26.6% | 1.379 | 1.379 | 6.8 | 78600 | 0 |
| changeset-cbor | 0.33 | 0.33 | -0.5% | 0.034 | 0.034 | 3.1 | 654 | 0 |
| changeset-json | 0.10 | 0.10 | -0.8% | 0.002 | 0.002 | 3.1 | 923 | 0 |
| changeset-validate-paths | 0.03 | 0.03 | -1.5% | 0.003 | 0.003 | 3.1 | 0 | 0 |
| changeset-cbor-tree | 0.13 | 0.13 | 4.1% | 0.017 | 0.017 | 3.1 | 798 | 0 |
| changeset-etag-tree | 0.07 | 0.08 | -25.3% | 0.005 | 0.005 | 3.1 | 654 | 0 |
| changeset-cbor-write | 0.05 | 0.05 | 0.4% | 0.007 | 0.007 | 3.1 | 654 | 0 |
| archive-json | 7.10 | 7.70 | -8.5% | 2.110 | 2.110 | 5.1 | 153021 | 0 |
| archive-json-stream | 7.43 | 7.90 | -6.3% | 1.189 | 1.189 | 4.3 | 153021 | 0 |
| archive-cbor | 176.92 | 189.15 | -6.9% | 62.691 | 52.706 | 14.7 | 249862 | 0 |
| archive-cbor-stream | 187.89 | 160.64 | 14.5% | 61.889 | 51.904 | 17.8 | 249862 | 0 |

## graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| snapshot-cbor | 118.04 | 138.11 | -17.0% | 81.669 | 68.386 | 30.8 | 311525 | 0 |
| snapshot-json-document | 83.05 | 71.20 | 14.3% | 56.669 | 43.386 | 32.3 | 462064 | 0 |
| snapshot-reading-paths | 1.77 | 2.57 | -45.1% | 1.052 | 1.052 | 19.4 | 0 | 0 |
| snapshot-json-canonical | 6.45 | 7.05 | -9.4% | 4.274 | 4.274 | 22.7 | 462064 | 0 |
| snapshot-cbor-tree | 12.10 | 13.36 | -10.5% | 9.292 | 9.292 | 24.0 | 403901 | 0 |
| snapshot-etag-tree | 7.66 | 7.90 | -3.1% | 4.907 | 4.907 | 24.1 | 311525 | 0 |
| snapshot-cbor-write | 2.28 | 2.27 | 0.3% | 5.475 | 5.475 | 24.1 | 311525 | 0 |
| changeset-cbor | 0.26 | 0.28 | -5.8% | 0.034 | 0.034 | 8.8 | 654 | 0 |
| changeset-json | 0.10 | 0.10 | -3.4% | 0.002 | 0.002 | 8.8 | 918 | 0 |
| changeset-validate-paths | 0.02 | 0.02 | -3.2% | 0.003 | 0.003 | 8.8 | 0 | 0 |
| changeset-cbor-tree | 0.09 | 0.11 | -18.1% | 0.017 | 0.017 | 8.8 | 798 | 0 |
| changeset-etag-tree | 0.07 | 0.06 | 7.5% | 0.005 | 0.005 | 8.8 | 654 | 0 |
| changeset-cbor-write | 0.05 | 0.04 | 19.1% | 0.007 | 0.007 | 8.8 | 654 | 0 |
| archive-json | 15.45 | 27.68 | -79.2% | 7.992 | 8.098 | 16.9 | 544530 | 0 |
| archive-json-stream | 21.57 | 15.54 | 27.9% | 4.452 | 4.452 | 13.3 | 544530 | 0 |
| archive-cbor | 370.43 | 338.94 | 8.5% | 248.538 | 208.690 | 43.8 | 948637 | 0 |
| archive-cbor-stream | 369.87 | 336.73 | 9.0% | 245.515 | 205.667 | 47.2 | 948637 | 0 |

## history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| snapshot-cbor | 7.19 | 5.97 | 17.0% | 2.709 | 2.284 | 3.8 | 10782 | 0 |
| snapshot-json-document | 6.09 | 4.55 | 25.3% | 1.857 | 1.431 | 3.1 | 15869 | 0 |
| snapshot-reading-paths | 0.21 | 0.19 | 9.3% | 0.035 | 0.035 | 1.8 | 0 | 0 |
| snapshot-json-canonical | 0.44 | 0.41 | 7.1% | 0.158 | 0.158 | 2.0 | 15869 | 0 |
| snapshot-cbor-tree | 0.56 | 0.62 | -10.0% | 0.308 | 0.308 | 2.2 | 13878 | 0 |
| snapshot-etag-tree | 0.40 | 0.46 | -13.9% | 0.167 | 0.167 | 2.0 | 10782 | 0 |
| snapshot-cbor-write | 0.32 | 0.49 | -56.1% | 0.185 | 0.185 | 2.0 | 10782 | 0 |
| changeset-cbor | 0.35 | 0.33 | 5.0% | 0.034 | 0.034 | 1.7 | 655 | 0 |
| changeset-json | 0.09 | 0.09 | 6.8% | 0.002 | 0.002 | 1.7 | 908 | 0 |
| changeset-validate-paths | 0.03 | 0.04 | -42.3% | 0.003 | 0.003 | 1.7 | 0 | 0 |
| changeset-cbor-tree | 0.13 | 0.14 | -10.9% | 0.017 | 0.017 | 1.7 | 799 | 0 |
| changeset-etag-tree | 0.06 | 0.09 | -36.1% | 0.005 | 0.005 | 1.7 | 655 | 0 |
| changeset-cbor-write | 0.05 | 0.06 | -21.9% | 0.007 | 0.007 | 1.7 | 655 | 0 |
| archive-json | 4.22 | 3.82 | 9.4% | 1.421 | 1.421 | 3.1 | 160077 | 0 |
| archive-json-stream | 4.02 | 3.51 | 12.7% | 0.484 | 0.484 | 2.1 | 160077 | 0 |
| archive-cbor | 49.67 | 45.31 | 8.8% | 17.560 | 15.432 | 9.6 | 144594 | 0 |
| archive-cbor-stream | 52.98 | 48.28 | 8.9% | 16.945 | 14.818 | 8.7 | 144594 | 0 |

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| snapshot-cbor | 6.93 | 6.48 | 6.5% | 2.709 | 2.284 | 3.8 | 10782 | 0 |
| snapshot-json-document | 6.06 | 4.60 | 24.0% | 1.857 | 1.431 | 3.2 | 15869 | 0 |
| snapshot-reading-paths | 0.21 | 0.21 | -3.5% | 0.035 | 0.035 | 1.9 | 0 | 0 |
| snapshot-json-canonical | 0.38 | 0.42 | -10.2% | 0.158 | 0.158 | 2.0 | 15869 | 0 |
| snapshot-cbor-tree | 0.54 | 0.45 | 15.5% | 0.308 | 0.308 | 2.3 | 13878 | 0 |
| snapshot-etag-tree | 0.46 | 0.47 | -3.2% | 0.167 | 0.167 | 2.1 | 10782 | 0 |
| snapshot-cbor-write | 0.50 | 0.49 | 1.6% | 0.185 | 0.185 | 2.1 | 10782 | 0 |
| changeset-cbor | 0.34 | 0.32 | 5.6% | 0.034 | 0.034 | 1.5 | 655 | 0 |
| changeset-json | 0.10 | 0.09 | 7.1% | 0.002 | 0.002 | 1.5 | 918 | 0 |
| changeset-validate-paths | 0.03 | 0.03 | 1.1% | 0.003 | 0.003 | 1.5 | 0 | 0 |
| changeset-cbor-tree | 0.14 | 0.14 | -0.5% | 0.017 | 0.017 | 1.6 | 799 | 0 |
| changeset-etag-tree | 0.07 | 0.06 | 7.7% | 0.005 | 0.005 | 1.5 | 655 | 0 |
| changeset-cbor-write | 0.05 | 0.05 | -14.5% | 0.007 | 0.007 | 1.6 | 655 | 0 |
| archive-json | 2.92 | 2.87 | 1.6% | 0.940 | 0.940 | 2.5 | 106105 | 0 |
| archive-json-stream | 2.61 | 2.90 | -11.1% | 0.404 | 0.404 | 1.9 | 106105 | 0 |
| archive-cbor | 41.04 | 39.22 | 4.4% | 15.684 | 13.557 | 8.3 | 105525 | 0 |
| archive-cbor-stream | 46.66 | 36.63 | 21.5% | 15.390 | 13.262 | 9.0 | 105525 | 0 |
