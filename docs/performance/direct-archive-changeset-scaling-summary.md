# Direct archive ChangeSet measurements

Inputs: `direct-archive-changeset-scaling-before.json`, `direct-archive-changeset-scaling-after.json`. Exact workload groups, same harness/runtime/dependencies, all outputs/counts/digests and static/history inventories checked.

Each report has 48 fresh workers / 240 samples, with two processes/five warmups/five samples. Complete output/SHA-256 is measured. Construction, signing, recovery and validation are setup. The borrowed digest sink retains no complete output.

Scaling uses prepared graph/history/pruned fixtures and six operations, including buffered/stream JSON controls, snapshot CBOR and frozen bootstrap. This series adds no cold/warm snapshot probe.

Batch archives retain a signed checkpoint and one genuinely applied 1/64/512/2,048-operation batch with one/four equal Ed25519 peers on the batch and both commits. The original standalone batch probe checks previous-tree bytes, signing preimages and result application in setup. Archive recovery checks head/state and peer counts. The canonical JSON control measures the complete signed batch, not JSON archive output. The largest fixture repeats 512 targets four times.

No build/test from this benchmark task overlaps either series. The host is not dedicated; timings and sampled peaks remain descriptive, and setup/preceding results can affect memory. These synthetic shapes do not establish every operation, cold replay or production capacity.

| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms | Before managed peak MiB | After managed peak MiB |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| graph-128 | archive-cbor | 17.7494 | 17.7976 | +0.3% | 83.159 | 83.779 | 14.73 | 14.77 |
| graph-128 | archive-cbor-stream | 17.0270 | 17.0752 | +0.3% | 88.851 | 80.363 | 14.02 | 14.07 |
| graph-128 | archive-json | 2.1099 | 2.1099 | +0.0% | 10.572 | 8.329 | 5.10 | 5.10 |
| graph-128 | archive-json-stream | 1.1887 | 1.1887 | +0.0% | 9.836 | 9.373 | 4.18 | 4.18 |
| graph-128 | bootstrap-export | 25.9299 | 25.9783 | +0.2% | 83.161 | 90.158 | 11.87 | 12.42 |
| graph-128 | snapshot-cbor | 5.2565 | 5.2565 | +0.0% | 24.899 | 19.608 | 8.51 | 8.50 |
| graph-512 | archive-cbor | 68.9211 | 68.9691 | +0.1% | 166.587 | 201.708 | 34.01 | 32.25 |
| graph-512 | archive-cbor-stream | 66.0325 | 66.0806 | +0.1% | 178.133 | 155.249 | 32.04 | 29.86 |
| graph-512 | archive-json | 7.8864 | 8.0979 | +2.7% | 7.781 | 26.332 | 16.66 | 16.92 |
| graph-512 | archive-json-stream | 4.5582 | 4.4525 | -2.3% | 32.177 | 16.390 | 13.34 | 13.23 |
| graph-512 | bootstrap-export | 94.6116 | 94.6598 | +0.1% | 186.743 | 215.887 | 31.50 | 34.95 |
| graph-512 | snapshot-cbor | 20.8682 | 20.8330 | -0.2% | 54.587 | 34.407 | 27.95 | 27.94 |
| history-64 | archive-cbor | 7.6587 | 8.0144 | +4.6% | 32.940 | 36.311 | 9.44 | 9.81 |
| history-64 | archive-cbor-stream | 7.0450 | 7.3914 | +4.9% | 29.595 | 30.710 | 8.84 | 9.20 |
| history-64 | archive-json | 1.4213 | 1.4213 | +0.0% | 4.009 | 4.249 | 3.06 | 3.07 |
| history-64 | archive-json-stream | 0.4836 | 0.4836 | +0.0% | 3.873 | 4.201 | 2.14 | 2.14 |
| history-64 | bootstrap-export | 12.5935 | 12.9408 | +2.8% | 35.667 | 29.869 | 6.63 | 7.03 |
| history-64 | snapshot-cbor | 0.7547 | 0.7548 | +0.0% | 3.099 | 2.993 | 2.31 | 2.31 |
| pruned-4 | archive-cbor | 5.9956 | 6.1741 | +3.0% | 27.711 | 24.151 | 7.62 | 7.79 |
| pruned-4 | archive-cbor-stream | 5.6612 | 5.8397 | +3.2% | 28.759 | 26.041 | 7.29 | 7.46 |
| pruned-4 | archive-json | 0.9400 | 0.9400 | +0.0% | 3.999 | 3.917 | 2.49 | 2.47 |
| pruned-4 | archive-json-stream | 0.4044 | 0.4044 | +0.0% | 3.163 | 3.542 | 1.95 | 1.95 |
| pruned-4 | bootstrap-export | 10.5206 | 10.6995 | +1.7% | 28.638 | 28.424 | 4.93 | 4.91 |
| pruned-4 | snapshot-cbor | 0.7547 | 0.7548 | +0.0% | 3.951 | 3.000 | 2.33 | 2.33 |
