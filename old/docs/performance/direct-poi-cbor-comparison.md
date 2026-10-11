# Measured comparison

Before: `child-etags-after.json`; after: `direct-poi-cbor-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| child-etags-json-cold | 35.46 | 32.10 | 9.5% | 10.890 | 9.776 | 11.0 | 116531 | 0 |
| child-etags-json-warm | 7.41 | 6.35 | 14.3% | 2.591 | 2.591 | 7.1 | 116531 | 0 |
| child-etags-cbor-cold | 47.00 | 45.35 | 3.5% | 17.159 | 12.433 | 12.0 | 78600 | 0 |
| child-etags-cbor-warm | 18.27 | 18.27 | 0.0% | 8.860 | 5.257 | 9.8 | 78600 | 0 |
| snapshot-json-document | 7.53 | 6.92 | 8.2% | 2.591 | 2.591 | 7.2 | 116531 | 0 |
| snapshot-cbor | 19.39 | 19.99 | -3.1% | 8.860 | 5.257 | 8.5 | 78600 | 0 |
| archive-json | 7.66 | 7.70 | -0.5% | 2.110 | 2.110 | 5.1 | 153021 | 0 |
| archive-json-stream | 7.34 | 7.35 | -0.2% | 1.189 | 1.189 | 4.2 | 153021 | 0 |
| archive-cbor | 70.25 | 75.38 | -7.3% | 27.891 | 17.080 | 12.5 | 249862 | 0 |
| archive-cbor-stream | 68.80 | 69.66 | -1.3% | 27.090 | 16.279 | 13.3 | 249862 | 0 |

## graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| child-etags-json-cold | 54.52 | 62.93 | -15.4% | 43.512 | 39.018 | 29.7 | 462064 | 0 |
| child-etags-json-warm | 17.08 | 29.89 | -75.0% | 10.225 | 10.260 | 23.1 | 462064 | 0 |
| child-etags-cbor-cold | 80.37 | 65.84 | 18.1% | 68.511 | 49.558 | 34.5 | 311525 | 0 |
| child-etags-cbor-warm | 50.15 | 47.06 | 6.2% | 35.224 | 20.868 | 33.2 | 311525 | 0 |
| snapshot-json-document | 23.75 | 17.16 | 27.7% | 10.295 | 10.225 | 22.8 | 462064 | 0 |
| snapshot-cbor | 82.19 | 41.80 | 49.1% | 35.224 | 20.833 | 27.9 | 311525 | 0 |
| archive-json | 15.20 | 26.00 | -71.1% | 7.992 | 8.098 | 16.9 | 544530 | 0 |
| archive-json-stream | 15.55 | 15.64 | -0.6% | 4.452 | 4.452 | 13.4 | 544530 | 0 |
| archive-cbor | 239.55 | 133.33 | 44.3% | 109.314 | 66.031 | 34.9 | 948637 | 0 |
| archive-cbor-stream | 191.48 | 122.39 | 36.1% | 106.183 | 63.008 | 30.5 | 948637 | 0 |

## history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| child-etags-json-cold | 4.26 | 5.55 | -30.4% | 1.437 | 1.304 | 3.0 | 15869 | 0 |
| child-etags-json-warm | 1.16 | 1.12 | 3.2% | 0.366 | 0.366 | 2.1 | 15869 | 0 |
| child-etags-cbor-cold | 6.44 | 7.42 | -15.2% | 2.289 | 1.692 | 3.4 | 10782 | 0 |
| child-etags-cbor-warm | 3.10 | 2.96 | 4.5% | 1.219 | 0.755 | 2.5 | 10782 | 0 |
| snapshot-json-document | 1.37 | 1.31 | 4.7% | 0.366 | 0.366 | 2.1 | 15869 | 0 |
| snapshot-cbor | 3.42 | 2.95 | 13.9% | 1.219 | 0.755 | 2.3 | 10782 | 0 |
| archive-json | 4.00 | 5.28 | -31.8% | 1.421 | 1.421 | 3.1 | 160077 | 0 |
| archive-json-stream | 3.55 | 3.66 | -2.9% | 0.484 | 0.484 | 2.2 | 160077 | 0 |
| archive-cbor | 30.22 | 51.31 | -69.8% | 10.108 | 7.788 | 9.6 | 144594 | 0 |
| archive-cbor-stream | 30.18 | 31.94 | -5.8% | 9.493 | 7.172 | 9.0 | 144594 | 0 |

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| child-etags-json-cold | 4.83 | 5.23 | -8.2% | 1.437 | 1.304 | 3.1 | 15869 | 0 |
| child-etags-json-warm | 1.18 | 1.52 | -28.6% | 0.366 | 0.366 | 2.1 | 15869 | 0 |
| child-etags-cbor-cold | 6.98 | 6.82 | 2.4% | 2.289 | 1.692 | 3.5 | 10782 | 0 |
| child-etags-cbor-warm | 3.60 | 3.08 | 14.5% | 1.219 | 0.755 | 2.5 | 10782 | 0 |
| snapshot-json-document | 1.32 | 1.38 | -4.6% | 0.366 | 0.366 | 2.2 | 15869 | 0 |
| snapshot-cbor | 3.43 | 3.04 | 11.5% | 1.219 | 0.755 | 2.4 | 10782 | 0 |
| archive-json | 3.14 | 3.15 | -0.4% | 0.940 | 0.940 | 2.5 | 106105 | 0 |
| archive-json-stream | 2.79 | 3.02 | -8.5% | 0.404 | 0.404 | 2.0 | 106105 | 0 |
| archive-cbor | 25.17 | 21.90 | 13.0% | 8.233 | 5.911 | 7.6 | 105525 | 0 |
| archive-cbor-stream | 24.30 | 22.20 | 8.6% | 7.938 | 5.617 | 7.3 | 105525 | 0 |
