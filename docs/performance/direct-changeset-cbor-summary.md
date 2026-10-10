# Direct ChangeSet CBOR measurements

Inputs: `direct-changeset-cbor-before.json`, `direct-changeset-cbor-after.json`. Complete matched groups, same harness/runtime/dependencies, exact bytes/digests and static inventories checked.

Four ordered batch sizes, one/four equal Ed25519 peers, two fresh processes, five warmups and five measured samples: 32 workers / 160 samples per report. Construction, result preparation, signing, independent previous-tree byte checks, signature verification and result application are outside measurement. Complete outputs include SHA-256.

JSON is complete canonical batch JSON; ordinary serializer dictionary order varies across processes. ArchiveIdentity binds complete signed batch CBOR here; there is no retained history. The 2,048-operation fixture repeats the 512-target round four times. These are synthetic property/custom-data batches, not every domain operation or production capacity evidence.

No task build/test overlaps these reports. No host affinity/dedicated host; timings/CPU and sampled memory are descriptive. Peaks include prepared inputs, reference trees/results and the preceding result; brief peaks can be missed.

| Batch size | Peers | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms | Before managed peak MiB | After managed peak MiB |
| ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1 | 1 | changeset-batch-json | 0.0192 | 0.0192 | +0.0% | 0.229 | 0.228 | 0.98 | 0.98 |
| 1 | 1 | changeset-batch-cbor | 0.0386 | 0.0343 | -11.2% | 0.398 | 0.328 | 1.01 | 1.01 |
| 1 | 4 | changeset-batch-json | 0.0264 | 0.0264 | +0.0% | 0.374 | 0.261 | 0.99 | 0.99 |
| 1 | 4 | changeset-batch-cbor | 0.0540 | 0.0433 | -19.9% | 0.517 | 0.397 | 1.03 | 1.01 |
| 64 | 1 | changeset-batch-json | 0.2061 | 0.2061 | +0.0% | 1.043 | 1.106 | 1.56 | 1.55 |
| 64 | 1 | changeset-batch-cbor | 0.5144 | 0.3515 | -31.7% | 1.973 | 1.814 | 1.86 | 1.70 |
| 64 | 4 | changeset-batch-json | 0.2133 | 0.2133 | +0.0% | 1.143 | 1.107 | 1.57 | 1.56 |
| 64 | 4 | changeset-batch-cbor | 0.5258 | 0.3585 | -31.8% | 2.016 | 1.805 | 1.88 | 1.70 |
| 512 | 1 | changeset-batch-json | 1.6615 | 1.6615 | +0.0% | 8.334 | 7.710 | 5.84 | 5.81 |
| 512 | 1 | changeset-batch-cbor | 3.9294 | 2.6096 | -33.6% | 16.666 | 13.387 | 8.08 | 6.76 |
| 512 | 4 | changeset-batch-json | 1.6687 | 1.6687 | +0.0% | 6.761 | 6.701 | 5.83 | 5.82 |
| 512 | 4 | changeset-batch-cbor | 3.9408 | 2.6224 | -33.5% | 16.545 | 12.502 | 8.10 | 6.77 |
| 2048 | 1 | changeset-batch-json | 6.6818 | 6.6818 | +0.0% | 16.122 | 13.841 | 13.12 | 13.02 |
| 2048 | 1 | changeset-batch-cbor | 15.7134 | 10.4531 | -33.5% | 48.082 | 35.478 | 15.67 | 12.06 |
| 2048 | 4 | changeset-batch-json | 6.6890 | 6.6890 | +0.0% | 14.290 | 15.272 | 13.13 | 13.03 |
| 2048 | 4 | changeset-batch-cbor | 15.7482 | 10.4835 | -33.4% | 46.074 | 36.155 | 15.69 | 11.05 |
