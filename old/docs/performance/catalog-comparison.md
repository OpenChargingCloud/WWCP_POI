# Measured comparison

Before: `catalog-before.json`; after: `catalog-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## catalog-16

EVSEs: 1; graph nodes: 6; retained commits: 1; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| catalog-create | 0.13 | 0.09 | 29.4% | 0.007 | 0.008 | 0.8 | 0 | 0 |

## catalog-256

EVSEs: 1; graph nodes: 6; retained commits: 1; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| catalog-create | 3.70 | 0.43 | 88.4% | 0.099 | 0.115 | 0.9 | 0 | 0 |

## catalog-4096

EVSEs: 1; graph nodes: 6; retained commits: 1; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| catalog-create | 185.52 | 5.51 | 97.0% | 1.610 | 1.872 | 2.6 | 0 | 0 |

## catalog-16384

EVSEs: 1; graph nodes: 6; retained commits: 1; receipts: 0; catalog IDs: 0.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| catalog-create | 2196.47 | 19.27 | 99.1% | 6.252 | 6.894 | 7.7 | 0 | 0 |
