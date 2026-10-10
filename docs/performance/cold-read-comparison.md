# Measured comparison

Before: `cold-read-before.json`; after: `cold-read-after.json`.

Medians include every measured sample across isolated processes. Peak columns are the largest sampled operation-window values; lifetime peaks include setup.

All static identities, archive digests, operation identities and byte counts must match before this report is written. A smaller elapsed value is an observation for this run, not a performance guarantee.

## pruned-4

EVSEs: 16; graph nodes: 39; retained commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Before ms | After ms | Time reduction | Before allocation MiB | After allocation MiB | After sampled managed peak MiB | Output bytes | Written bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| cold-read | 182.93 | 229.95 | -25.7% | 50.849 | 48.771 | 12.3 | 32611 | 0 |
