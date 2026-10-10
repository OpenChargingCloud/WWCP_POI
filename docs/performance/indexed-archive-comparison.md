# Measured comparison

Before: `indexed-archive-before.json`; after: `indexed-archive-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## recover-graph-128

EVSEs: 128; graph nodes: 291; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| read-restore | 339.80 | 301.88 | 11.2% | 170.781 | 169.717 | 30.4 | 0 | 0 |
| read-cbor | 697.46 | 730.71 | -4.8% | 426.270 | 425.633 | 41.6 | 249862 | 0 |
| read-json | 618.45 | 706.43 | -14.2% | 378.382 | 379.183 | 42.7 | 153021 | 0 |

## recover-graph-512

EVSEs: 512; graph nodes: 1155; retained commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| read-restore | 1558.98 | 1210.87 | 22.3% | 667.085 | 667.047 | 90.5 | 0 | 0 |
| read-cbor | 8494.51 | 3335.12 | 60.7% | 1687.921 | 1688.502 | 104.8 | 948637 | 0 |
| read-json | 3956.48 | 3093.54 | 21.8% | 1501.824 | 1502.253 | 98.3 | 544530 | 0 |

## recover-history-64

EVSEs: 16; graph nodes: 39; retained commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| read-restore | 245.77 | 255.33 | -3.9% | 81.943 | 82.138 | 12.1 | 0 | 0 |
| read-cbor | 316.00 | 284.20 | 10.1% | 144.840 | 145.485 | 14.5 | 144594 | 0 |
| read-json | 249.14 | 326.96 | -31.2% | 132.356 | 132.371 | 12.1 | 160077 | 0 |

## recover-pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| read-restore | 264.78 | 207.13 | 21.8% | 60.903 | 60.990 | 12.0 | 0 | 0 |
| read-cbor | 343.58 | 290.37 | 15.5% | 121.422 | 122.211 | 12.2 | 105525 | 0 |
| read-json | 225.53 | 278.84 | -23.6% | 109.900 | 110.024 | 12.3 | 106105 | 0 |

## recover-boundary-16

EVSEs: 16; graph nodes: 39; retained commits: 8; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| read-restore | 105.61 | 98.27 | 6.9% | 24.225 | 24.219 | 10.4 | 0 | 0 |
| read-cbor | 174.62 | 177.39 | -1.6% | 47.742 | 47.986 | 11.4 | 30343 | 0 |
| read-json | 183.61 | 167.69 | 8.7% | 43.443 | 43.445 | 10.4 | 25339 | 0 |

## recover-batch-1-peers-1

EVSEs: 1; graph nodes: 6; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| read-restore | 10.69 | 9.89 | 7.5% | 3.260 | 3.260 | 4.4 | 0 | 0 |
| read-cbor | 16.80 | 15.95 | 5.0% | 5.236 | 5.200 | 6.3 | 4144 | 0 |
| read-json | 22.54 | 14.47 | 35.8% | 4.895 | 4.895 | 6.0 | 4399 | 0 |

## recover-batch-2048-peers-4

EVSEs: 512; graph nodes: 1155; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| read-restore | 2066.03 | 1680.32 | 18.7% | 1294.683 | 1294.245 | 94.5 | 0 | 0 |
| read-cbor | 2388.87 | 2376.81 | 0.5% | 1662.930 | 1656.395 | 104.7 | 613720 | 0 |
| read-json | 2345.27 | 2221.31 | 5.3% | 1580.935 | 1580.255 | 99.8 | 540479 | 0 |
