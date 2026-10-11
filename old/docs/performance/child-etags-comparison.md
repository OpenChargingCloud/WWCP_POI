# Measured comparison

Before: `child-etags-before.json`; after: `child-etags-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| child-etags-json-cold | 33.79 | 35.46 | -5.0% | 10.862 | 10.890 | 12.1 | 116531 | 0 |
| child-etags-json-warm | 32.14 | 7.41 | 76.9% | 10.862 | 2.591 | 7.1 | 116531 | 0 |
| child-etags-cbor-cold | 46.53 | 47.00 | -1.0% | 17.132 | 17.159 | 14.7 | 78600 | 0 |
| child-etags-cbor-warm | 45.99 | 18.27 | 60.3% | 17.132 | 8.860 | 13.3 | 78600 | 0 |
| snapshot-json-document | 36.57 | 7.53 | 79.4% | 10.862 | 2.591 | 7.1 | 116531 | 0 |
| snapshot-cbor | 47.62 | 19.39 | 59.3% | 17.132 | 8.860 | 12.0 | 78600 | 0 |
| archive-json | 7.55 | 7.66 | -1.4% | 2.110 | 2.110 | 5.1 | 153021 | 0 |
| archive-json-stream | 7.23 | 7.34 | -1.5% | 1.189 | 1.189 | 4.3 | 153021 | 0 |
| archive-cbor | 156.37 | 70.25 | 55.1% | 52.706 | 27.891 | 12.7 | 249862 | 0 |
| archive-cbor-stream | 158.59 | 68.80 | 56.6% | 51.904 | 27.090 | 13.9 | 249862 | 0 |

## graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| child-etags-json-cold | 48.15 | 54.52 | -13.2% | 43.386 | 43.512 | 32.8 | 462064 | 0 |
| child-etags-json-warm | 56.27 | 17.08 | 69.6% | 43.386 | 10.225 | 22.1 | 462064 | 0 |
| child-etags-cbor-cold | 101.62 | 80.37 | 20.9% | 68.386 | 68.511 | 37.0 | 311525 | 0 |
| child-etags-cbor-warm | 102.50 | 50.15 | 51.1% | 68.386 | 35.224 | 34.9 | 311525 | 0 |
| snapshot-json-document | 74.85 | 23.75 | 68.3% | 43.422 | 10.295 | 23.6 | 462064 | 0 |
| snapshot-cbor | 156.01 | 82.19 | 47.3% | 68.421 | 35.224 | 29.7 | 311525 | 0 |
| archive-json | 8.44 | 15.20 | -80.0% | 7.886 | 7.992 | 16.8 | 544530 | 0 |
| archive-json-stream | 16.63 | 15.55 | 6.5% | 4.452 | 4.452 | 13.3 | 544530 | 0 |
| archive-cbor | 337.86 | 239.55 | 29.1% | 208.690 | 109.314 | 46.0 | 948637 | 0 |
| archive-cbor-stream | 437.69 | 191.48 | 56.3% | 205.667 | 106.183 | 50.4 | 948637 | 0 |

## history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| child-etags-json-cold | 4.68 | 4.26 | 9.1% | 1.431 | 1.437 | 3.2 | 15869 | 0 |
| child-etags-json-warm | 4.86 | 1.16 | 76.2% | 1.431 | 0.366 | 2.1 | 15869 | 0 |
| child-etags-cbor-cold | 6.75 | 6.44 | 4.6% | 2.284 | 2.289 | 4.0 | 10782 | 0 |
| child-etags-cbor-warm | 6.20 | 3.10 | 50.0% | 2.284 | 1.219 | 2.9 | 10782 | 0 |
| snapshot-json-document | 4.27 | 1.37 | 67.9% | 1.431 | 0.366 | 2.1 | 15869 | 0 |
| snapshot-cbor | 6.20 | 3.42 | 44.8% | 2.284 | 1.219 | 2.8 | 10782 | 0 |
| archive-json | 3.78 | 4.00 | -6.0% | 1.421 | 1.421 | 3.1 | 160077 | 0 |
| archive-json-stream | 3.86 | 3.55 | 7.8% | 0.484 | 0.484 | 2.1 | 160077 | 0 |
| archive-cbor | 48.33 | 30.22 | 37.5% | 15.433 | 10.108 | 9.1 | 144594 | 0 |
| archive-cbor-stream | 49.72 | 30.18 | 39.3% | 14.819 | 9.493 | 9.7 | 144594 | 0 |

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| child-etags-json-cold | 4.86 | 4.83 | 0.7% | 1.431 | 1.437 | 3.2 | 15869 | 0 |
| child-etags-json-warm | 4.87 | 1.18 | 75.7% | 1.431 | 0.366 | 2.1 | 15869 | 0 |
| child-etags-cbor-cold | 6.57 | 6.98 | -6.3% | 2.284 | 2.289 | 4.0 | 10782 | 0 |
| child-etags-cbor-warm | 6.41 | 3.60 | 43.8% | 2.284 | 1.219 | 3.0 | 10782 | 0 |
| snapshot-json-document | 4.45 | 1.32 | 70.3% | 1.431 | 0.366 | 2.2 | 15869 | 0 |
| snapshot-cbor | 6.52 | 3.43 | 47.4% | 2.284 | 1.219 | 2.8 | 10782 | 0 |
| archive-json | 3.09 | 3.14 | -1.5% | 0.940 | 0.940 | 2.5 | 106105 | 0 |
| archive-json-stream | 2.86 | 2.79 | 2.7% | 0.404 | 0.404 | 2.0 | 106105 | 0 |
| archive-cbor | 44.70 | 25.17 | 43.7% | 13.556 | 8.233 | 8.6 | 105525 | 0 |
| archive-cbor-stream | 48.38 | 24.30 | 49.8% | 13.262 | 7.938 | 9.6 | 105525 | 0 |
