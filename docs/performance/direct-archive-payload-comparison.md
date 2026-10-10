# Measured comparison

Before: `direct-archive-payload-before.json`; after: `direct-archive-payload-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 7.69 | 24.92 | -223.9% | 2.110 | 2.110 | 5.1 | 153021 | 0 |
| archive-json-stream | 7.47 | 15.57 | -108.3% | 1.189 | 1.189 | 4.2 | 153021 | 0 |
| archive-cbor | 67.67 | 97.02 | -43.4% | 17.032 | 19.963 | 15.0 | 249862 | 0 |
| archive-cbor-stream | 69.19 | 90.16 | -30.3% | 16.230 | 19.240 | 14.7 | 249862 | 0 |
| snapshot-cbor | 21.04 | 25.82 | -22.7% | 5.257 | 5.257 | 8.5 | 78600 | 0 |
| child-etags-json-cold | 35.65 | 43.85 | -23.0% | 9.779 | 9.779 | 11.7 | 116531 | 0 |
| child-etags-json-warm | 6.50 | 9.90 | -52.3% | 2.591 | 2.591 | 7.1 | 116531 | 0 |
| child-etags-cbor-cold | 49.01 | 52.71 | -7.6% | 12.444 | 12.436 | 12.0 | 78600 | 0 |
| child-etags-cbor-warm | 20.50 | 23.11 | -12.7% | 5.257 | 5.257 | 9.8 | 78600 | 0 |
| bootstrap-export | 80.10 | 100.57 | -25.6% | 25.133 | 28.143 | 14.6 | 250989 | 0 |

## graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 17.94 | 12.04 | 32.9% | 7.992 | 7.886 | 16.7 | 544530 | 0 |
| archive-json-stream | 23.98 | 16.83 | 29.8% | 4.558 | 4.452 | 13.3 | 544530 | 0 |
| archive-cbor | 132.25 | 188.87 | -42.8% | 65.982 | 77.759 | 36.9 | 948637 | 0 |
| archive-cbor-stream | 135.50 | 197.55 | -45.8% | 62.959 | 74.870 | 36.4 | 948637 | 0 |
| snapshot-cbor | 31.04 | 59.69 | -92.3% | 20.833 | 20.868 | 28.0 | 311525 | 0 |
| child-etags-json-cold | 50.08 | 73.73 | -47.2% | 39.028 | 39.028 | 29.9 | 462064 | 0 |
| child-etags-json-warm | 17.62 | 31.73 | -80.1% | 10.225 | 10.225 | 23.4 | 462064 | 0 |
| child-etags-cbor-cold | 63.27 | 78.52 | -24.1% | 49.568 | 49.568 | 32.3 | 311525 | 0 |
| child-etags-cbor-warm | 37.26 | 45.36 | -21.7% | 20.833 | 20.833 | 33.2 | 311525 | 0 |
| bootstrap-export | 136.03 | 204.67 | -50.5% | 91.538 | 103.450 | 36.8 | 951733 | 0 |

## history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 4.20 | 4.76 | -13.3% | 1.421 | 1.421 | 3.1 | 160077 | 0 |
| archive-json-stream | 3.95 | 4.75 | -20.4% | 0.484 | 0.484 | 2.2 | 160077 | 0 |
| archive-cbor | 31.29 | 34.08 | -8.9% | 7.435 | 8.126 | 10.0 | 144594 | 0 |
| archive-cbor-stream | 39.41 | 39.45 | -0.1% | 6.821 | 7.513 | 9.4 | 144594 | 0 |
| snapshot-cbor | 3.05 | 3.51 | -15.1% | 0.755 | 0.755 | 2.3 | 10782 | 0 |
| child-etags-json-cold | 5.04 | 6.10 | -21.0% | 1.304 | 1.304 | 3.0 | 15869 | 0 |
| child-etags-json-warm | 1.03 | 1.12 | -9.2% | 0.366 | 0.366 | 2.1 | 15869 | 0 |
| child-etags-cbor-cold | 11.07 | 7.85 | 29.1% | 1.693 | 1.693 | 3.4 | 10782 | 0 |
| child-etags-cbor-warm | 3.10 | 3.09 | 0.2% | 0.755 | 0.755 | 2.5 | 10782 | 0 |
| bootstrap-export | 29.64 | 35.64 | -20.2% | 12.369 | 13.061 | 7.9 | 145543 | 0 |

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-json | 3.14 | 3.40 | -8.5% | 0.940 | 0.940 | 2.5 | 106105 | 0 |
| archive-json-stream | 2.86 | 3.76 | -31.6% | 0.404 | 0.404 | 2.0 | 106105 | 0 |
| archive-cbor | 21.15 | 27.35 | -29.3% | 5.731 | 6.463 | 8.1 | 105525 | 0 |
| archive-cbor-stream | 20.85 | 24.38 | -16.9% | 5.436 | 6.129 | 7.8 | 105525 | 0 |
| snapshot-cbor | 2.89 | 3.17 | -9.4% | 0.755 | 0.755 | 2.4 | 10782 | 0 |
| child-etags-json-cold | 5.18 | 5.20 | -0.3% | 1.304 | 1.304 | 3.1 | 15869 | 0 |
| child-etags-json-warm | 1.50 | 1.30 | 13.5% | 0.366 | 0.366 | 2.1 | 15869 | 0 |
| child-etags-cbor-cold | 6.37 | 8.82 | -38.3% | 1.693 | 1.693 | 3.5 | 10782 | 0 |
| child-etags-cbor-warm | 3.17 | 3.89 | -22.8% | 0.755 | 0.755 | 2.5 | 10782 | 0 |
| bootstrap-export | 31.25 | 33.47 | -7.1% | 10.297 | 10.988 | 7.3 | 106349 | 0 |
