# Direct archive ChangeSet measurements

Inputs: `direct-archive-changeset-batches-before.json`, `direct-archive-changeset-batches-after.json`. Exact workload groups, same harness/runtime/dependencies, all outputs/counts/digests and static/history inventories checked.

Each report has 48 fresh workers / 240 samples, with two processes/five warmups/five samples. Complete output/SHA-256 is measured. Construction, signing, recovery and validation are setup. The borrowed digest sink retains no complete output.

Scaling uses prepared graph/history/pruned fixtures and six operations, including buffered/stream JSON controls, snapshot CBOR and frozen bootstrap. This series adds no cold/warm snapshot probe.

Batch archives retain a signed checkpoint and one genuinely applied 1/64/512/2,048-operation batch with one/four equal Ed25519 peers on the batch and both commits. The original standalone batch probe checks previous-tree bytes, signing preimages and result application in setup. Archive recovery checks head/state and peer counts. The canonical JSON control measures the complete signed batch, not JSON archive output. The largest fixture repeats 512 targets four times.

No build/test from this benchmark task overlaps either series. The host is not dedicated; timings and sampled peaks remain descriptive, and setup/preceding results can affect memory. These synthetic shapes do not establish every operation, cold replay or production capacity.

| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms | Before managed peak MiB | After managed peak MiB |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| archive-batch-1-peers-1 | batch-archive-cbor | 0.2451 | 0.2515 | +2.6% | 1.088 | 1.199 | 1.32 | 1.32 |
| archive-batch-1-peers-1 | batch-archive-cbor-stream | 0.2374 | 0.2438 | +2.7% | 1.114 | 1.236 | 1.31 | 1.32 |
| archive-batch-1-peers-1 | batch-archive-control-json | 0.0192 | 0.0192 | +0.0% | 0.225 | 0.280 | 1.09 | 1.09 |
| archive-batch-1-peers-4 | batch-archive-cbor | 0.2793 | 0.2862 | +2.5% | 1.375 | 1.596 | 1.36 | 1.37 |
| archive-batch-1-peers-4 | batch-archive-cbor-stream | 0.2683 | 0.2752 | +2.6% | 1.229 | 1.817 | 1.34 | 1.35 |
| archive-batch-1-peers-4 | batch-archive-control-json | 0.0264 | 0.0264 | +0.0% | 0.270 | 0.373 | 1.10 | 1.10 |
| archive-batch-2048-peers-1 | batch-archive-cbor | 34.8974 | 34.4400 | -1.3% | 53.494 | 55.871 | 30.09 | 29.79 |
| archive-batch-2048-peers-1 | batch-archive-cbor-stream | 32.2807 | 32.1452 | -0.4% | 55.194 | 60.870 | 28.50 | 27.52 |
| archive-batch-2048-peers-1 | batch-archive-control-json | 6.5449 | 6.5449 | +0.0% | 9.044 | 10.492 | 19.94 | 19.93 |
| archive-batch-2048-peers-4 | batch-archive-cbor | 34.9316 | 34.7459 | -0.5% | 56.666 | 61.071 | 30.23 | 29.84 |
| archive-batch-2048-peers-4 | batch-archive-cbor-stream | 32.2744 | 31.9035 | -1.1% | 56.559 | 91.232 | 27.53 | 27.21 |
| archive-batch-2048-peers-4 | batch-archive-control-json | 6.5520 | 6.5520 | +0.0% | 9.948 | 10.586 | 19.96 | 19.96 |
| archive-batch-512-peers-1 | batch-archive-cbor | 26.0159 | 25.8507 | -0.6% | 83.794 | 65.699 | 27.41 | 27.49 |
| archive-batch-512-peers-1 | batch-archive-cbor-stream | 24.5646 | 24.5316 | -0.1% | 85.884 | 84.488 | 25.96 | 25.74 |
| archive-batch-512-peers-1 | batch-archive-control-json | 1.6615 | 1.6615 | +0.0% | 4.142 | 3.871 | 13.24 | 13.25 |
| archive-batch-512-peers-4 | batch-archive-cbor | 26.0544 | 25.9189 | -0.5% | 85.412 | 78.978 | 27.42 | 27.24 |
| archive-batch-512-peers-4 | batch-archive-cbor-stream | 24.5935 | 24.5277 | -0.3% | 83.545 | 76.956 | 26.19 | 25.92 |
| archive-batch-512-peers-4 | batch-archive-control-json | 1.6687 | 1.6687 | +0.0% | 4.488 | 3.327 | 13.31 | 13.27 |
| archive-batch-64-peers-1 | batch-archive-cbor | 3.3314 | 3.3368 | +0.2% | 11.571 | 13.045 | 5.68 | 5.68 |
| archive-batch-64-peers-1 | batch-archive-cbor-stream | 3.1735 | 3.1789 | +0.2% | 11.492 | 16.424 | 5.52 | 5.53 |
| archive-batch-64-peers-1 | batch-archive-control-json | 0.2061 | 0.2061 | +0.0% | 1.088 | 1.192 | 2.55 | 2.54 |
| archive-batch-64-peers-4 | batch-archive-cbor | 3.3619 | 3.3698 | +0.2% | 11.525 | 12.420 | 5.71 | 5.72 |
| archive-batch-64-peers-4 | batch-archive-cbor-stream | 3.2024 | 3.2103 | +0.2% | 11.567 | 12.787 | 5.55 | 5.56 |
| archive-batch-64-peers-4 | batch-archive-control-json | 0.2133 | 0.2133 | +0.0% | 1.111 | 1.118 | 2.55 | 2.56 |
