# Measured comparison

Before: `direct-changeset-cbor-before.json`; after: `direct-changeset-cbor-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## batch-1-peers-1

EVSEs: 1; graph nodes: 6; retained commits: 0; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| changeset-batch-json | 0.23 | 0.23 | 0.4% | 0.019 | 0.019 | 1.0 | 1022 | 0 |
| changeset-batch-cbor | 0.40 | 0.33 | 17.5% | 0.039 | 0.034 | 1.0 | 813 | 0 |

## batch-1-peers-4

EVSEs: 1; graph nodes: 6; retained commits: 0; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| changeset-batch-json | 0.37 | 0.26 | 30.2% | 0.026 | 0.026 | 1.0 | 1634 | 0 |
| changeset-batch-cbor | 0.52 | 0.40 | 23.2% | 0.054 | 0.043 | 1.1 | 1368 | 0 |

## batch-64-peers-1

EVSEs: 64; graph nodes: 147; retained commits: 0; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| changeset-batch-json | 1.04 | 1.11 | -6.1% | 0.206 | 0.206 | 1.6 | 12113 | 0 |
| changeset-batch-cbor | 1.97 | 1.81 | 8.0% | 0.514 | 0.351 | 1.7 | 9977 | 0 |

## batch-64-peers-4

EVSEs: 64; graph nodes: 147; retained commits: 0; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| changeset-batch-json | 1.14 | 1.11 | 3.1% | 0.213 | 0.213 | 1.6 | 12725 | 0 |
| changeset-batch-cbor | 2.02 | 1.80 | 10.5% | 0.526 | 0.358 | 1.7 | 10532 | 0 |

## batch-512-peers-1

EVSEs: 512; graph nodes: 1155; retained commits: 0; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| changeset-batch-json | 8.33 | 7.71 | 7.5% | 1.661 | 1.661 | 5.8 | 91150 | 0 |
| changeset-batch-cbor | 16.67 | 13.39 | 19.7% | 3.929 | 2.610 | 6.8 | 75351 | 0 |

## batch-512-peers-4

EVSEs: 512; graph nodes: 1155; retained commits: 0; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| changeset-batch-json | 6.76 | 6.70 | 0.9% | 1.669 | 1.669 | 5.8 | 91762 | 0 |
| changeset-batch-cbor | 16.55 | 12.50 | 24.4% | 3.941 | 2.622 | 6.8 | 75906 | 0 |

## batch-2048-peers-1

EVSEs: 512; graph nodes: 1155; retained commits: 0; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| changeset-batch-json | 16.12 | 13.84 | 14.1% | 6.682 | 6.682 | 13.0 | 361930 | 0 |
| changeset-batch-cbor | 48.08 | 35.48 | 26.2% | 15.713 | 10.453 | 13.4 | 299283 | 0 |

## batch-2048-peers-4

EVSEs: 512; graph nodes: 1155; retained commits: 0; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| changeset-batch-json | 14.29 | 15.27 | -6.9% | 6.689 | 6.689 | 13.0 | 362542 | 0 |
| changeset-batch-cbor | 46.07 | 36.16 | 21.5% | 15.748 | 10.484 | 13.5 | 299838 | 0 |
