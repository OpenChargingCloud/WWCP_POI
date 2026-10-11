# Measured comparison

Before: `direct-archive-changeset-batches-before.json`; after: `direct-archive-changeset-batches-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## archive-batch-1-peers-1

EVSEs: 1; graph nodes: 6; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| batch-archive-cbor | 1.09 | 1.20 | -10.1% | 0.245 | 0.252 | 1.3 | 4144 | 0 |
| batch-archive-cbor-stream | 1.11 | 1.24 | -11.0% | 0.237 | 0.244 | 1.3 | 4144 | 0 |
| batch-archive-control-json | 0.23 | 0.28 | -24.4% | 0.019 | 0.019 | 1.1 | 1022 | 0 |

## archive-batch-1-peers-4

EVSEs: 1; graph nodes: 6; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| batch-archive-cbor | 1.38 | 1.60 | -16.0% | 0.279 | 0.286 | 1.4 | 5851 | 0 |
| batch-archive-cbor-stream | 1.23 | 1.82 | -47.9% | 0.268 | 0.275 | 1.3 | 5851 | 0 |
| batch-archive-control-json | 0.27 | 0.37 | -38.0% | 0.026 | 0.026 | 1.1 | 1634 | 0 |

## archive-batch-64-peers-1

EVSEs: 64; graph nodes: 147; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| batch-archive-cbor | 11.57 | 13.04 | -12.7% | 3.331 | 3.337 | 5.7 | 51009 | 0 |
| batch-archive-cbor-stream | 11.49 | 16.42 | -42.9% | 3.174 | 3.179 | 5.5 | 51009 | 0 |
| batch-archive-control-json | 1.09 | 1.19 | -9.6% | 0.206 | 0.206 | 2.6 | 12113 | 0 |

## archive-batch-64-peers-4

EVSEs: 64; graph nodes: 147; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| batch-archive-cbor | 11.53 | 12.42 | -7.8% | 3.362 | 3.370 | 5.7 | 52716 | 0 |
| batch-archive-cbor-stream | 11.57 | 12.79 | -10.5% | 3.202 | 3.210 | 5.6 | 52716 | 0 |
| batch-archive-control-json | 1.11 | 1.12 | -0.6% | 0.213 | 0.213 | 2.6 | 12725 | 0 |

## archive-batch-512-peers-1

EVSEs: 512; graph nodes: 1155; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| batch-archive-cbor | 83.79 | 65.70 | 21.6% | 26.016 | 25.851 | 27.5 | 388081 | 0 |
| batch-archive-cbor-stream | 85.88 | 84.49 | 1.6% | 24.565 | 24.532 | 26.4 | 388081 | 0 |
| batch-archive-control-json | 4.14 | 3.87 | 6.5% | 1.661 | 1.661 | 13.3 | 91150 | 0 |

## archive-batch-512-peers-4

EVSEs: 512; graph nodes: 1155; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| batch-archive-cbor | 85.41 | 78.98 | 7.5% | 26.054 | 25.919 | 27.6 | 389788 | 0 |
| batch-archive-cbor-stream | 83.55 | 76.96 | 7.9% | 24.593 | 24.528 | 26.9 | 389788 | 0 |
| batch-archive-control-json | 4.49 | 3.33 | 25.9% | 1.669 | 1.669 | 13.4 | 91762 | 0 |

## archive-batch-2048-peers-1

EVSEs: 512; graph nodes: 1155; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| batch-archive-cbor | 53.49 | 55.87 | -4.4% | 34.897 | 34.440 | 30.2 | 612013 | 0 |
| batch-archive-cbor-stream | 55.19 | 60.87 | -10.3% | 32.281 | 32.145 | 28.8 | 612013 | 0 |
| batch-archive-control-json | 9.04 | 10.49 | -16.0% | 6.545 | 6.545 | 20.0 | 361930 | 0 |

## archive-batch-2048-peers-4

EVSEs: 512; graph nodes: 1155; retained commits: 2; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| batch-archive-cbor | 56.67 | 61.07 | -7.8% | 34.932 | 34.746 | 30.3 | 613720 | 0 |
| batch-archive-cbor-stream | 56.56 | 91.23 | -61.3% | 32.274 | 31.903 | 27.7 | 613720 | 0 |
| batch-archive-control-json | 9.95 | 10.59 | -6.4% | 6.552 | 6.552 | 20.0 | 362542 | 0 |
