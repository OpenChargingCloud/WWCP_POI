# Measured comparison

Before: `direct-archive-payload-after.json`; after: `preflight-allocation-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 24.92 | 9.75 | 60.8% | 2.110 | 2.110 | 5.1 | 153021 | 0 |
| archive-json-stream | 15.57 | 10.11 | 35.1% | 1.189 | 1.189 | 4.2 | 153021 | 0 |
| archive-cbor | 97.02 | 80.24 | 17.3% | 19.963 | 17.749 | 14.7 | 249862 | 0 |
| archive-cbor-stream | 90.16 | 85.45 | 5.2% | 19.240 | 17.027 | 14.0 | 249862 | 0 |
| snapshot-cbor | 25.82 | 22.16 | 14.2% | 5.257 | 5.257 | 8.5 | 78600 | 0 |
| child-etags-json-cold | 43.85 | 43.36 | 1.1% | 9.779 | 9.781 | 11.6 | 116531 | 0 |
| child-etags-json-warm | 9.90 | 6.83 | 31.0% | 2.591 | 2.591 | 7.1 | 116531 | 0 |
| child-etags-cbor-cold | 52.71 | 55.01 | -4.4% | 12.436 | 12.447 | 10.9 | 78600 | 0 |
| child-etags-cbor-warm | 23.11 | 21.26 | 8.0% | 5.257 | 5.257 | 9.8 | 78600 | 0 |
| bootstrap-export | 100.57 | 96.89 | 3.7% | 28.143 | 25.930 | 14.1 | 250989 | 0 |

## graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 12.04 | 10.04 | 16.6% | 7.886 | 7.886 | 16.7 | 544530 | 0 |
| archive-json-stream | 16.83 | 7.32 | 56.5% | 4.452 | 4.347 | 13.1 | 544530 | 0 |
| archive-cbor | 188.87 | 173.75 | 8.0% | 77.759 | 68.921 | 45.0 | 948637 | 0 |
| archive-cbor-stream | 197.55 | 211.39 | -7.0% | 74.870 | 66.032 | 33.6 | 948637 | 0 |
| snapshot-cbor | 59.69 | 43.80 | 26.6% | 20.868 | 20.833 | 27.9 | 311525 | 0 |
| child-etags-json-cold | 73.73 | 55.39 | 24.9% | 39.028 | 39.038 | 28.9 | 462064 | 0 |
| child-etags-json-warm | 31.73 | 21.81 | 31.3% | 10.225 | 10.225 | 22.9 | 462064 | 0 |
| child-etags-cbor-cold | 78.52 | 65.95 | 16.0% | 49.568 | 49.577 | 36.7 | 311525 | 0 |
| child-etags-cbor-warm | 45.36 | 33.99 | 25.1% | 20.833 | 20.833 | 33.2 | 311525 | 0 |
| bootstrap-export | 204.67 | 178.27 | 12.9% | 103.450 | 94.612 | 39.0 | 951733 | 0 |

## history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 4.76 | 4.54 | 4.5% | 1.421 | 1.421 | 3.1 | 160077 | 0 |
| archive-json-stream | 4.75 | 4.21 | 11.3% | 0.484 | 0.484 | 2.2 | 160077 | 0 |
| archive-cbor | 34.08 | 34.77 | -2.0% | 8.126 | 7.659 | 9.5 | 144594 | 0 |
| archive-cbor-stream | 39.45 | 34.26 | 13.2% | 7.513 | 7.045 | 8.9 | 144594 | 0 |
| snapshot-cbor | 3.51 | 3.24 | 7.7% | 0.755 | 0.755 | 2.4 | 10782 | 0 |
| child-etags-json-cold | 6.10 | 6.53 | -7.1% | 1.304 | 1.305 | 3.0 | 15869 | 0 |
| child-etags-json-warm | 1.12 | 1.41 | -25.3% | 0.366 | 0.366 | 2.1 | 15869 | 0 |
| child-etags-cbor-cold | 7.85 | 9.22 | -17.5% | 1.693 | 1.693 | 3.4 | 10782 | 0 |
| child-etags-cbor-warm | 3.09 | 3.99 | -29.1% | 0.755 | 0.755 | 2.5 | 10782 | 0 |
| bootstrap-export | 35.64 | 35.31 | 0.9% | 13.061 | 12.594 | 6.8 | 145543 | 0 |

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 3.40 | 4.22 | -24.1% | 0.940 | 0.940 | 2.5 | 106105 | 0 |
| archive-json-stream | 3.76 | 3.74 | 0.5% | 0.404 | 0.404 | 2.0 | 106105 | 0 |
| archive-cbor | 27.35 | 26.90 | 1.6% | 6.463 | 5.996 | 7.7 | 105525 | 0 |
| archive-cbor-stream | 24.38 | 26.03 | -6.8% | 6.129 | 5.661 | 7.3 | 105525 | 0 |
| snapshot-cbor | 3.17 | 3.72 | -17.5% | 0.755 | 0.755 | 2.4 | 10782 | 0 |
| child-etags-json-cold | 5.20 | 6.76 | -30.2% | 1.304 | 1.305 | 3.1 | 15869 | 0 |
| child-etags-json-warm | 1.30 | 1.44 | -11.3% | 0.366 | 0.366 | 2.1 | 15869 | 0 |
| child-etags-cbor-cold | 8.82 | 8.17 | 7.3% | 1.693 | 1.693 | 3.5 | 10782 | 0 |
| child-etags-cbor-warm | 3.89 | 3.22 | 17.2% | 0.755 | 0.755 | 2.5 | 10782 | 0 |
| bootstrap-export | 33.47 | 26.66 | 20.3% | 10.988 | 10.521 | 7.3 | 106349 | 0 |
