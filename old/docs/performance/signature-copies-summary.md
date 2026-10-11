# Immutable signature-copy measurements

Before `signature-copies-before.json`; after `signature-copies-after.json`. Four shapes/five operations, two fresh processes/three warmups/three samples per side: 80 workers / 240 measured calls.

Each measured call creates sixteen independent copies of one immutable source. Construction, transport, full-constructor oracle comparison and current cryptographic verification are outside measurement. The commit-sign operation includes real Ed25519 signing and its unsigned signing preimage. No canonical preparation scope is active. WithChangeSet receives a prepared batch peer copy; creation of that batch and cloning its metadata are excluded.

The exact same new C# harness binary, runtime configuration and shared dependency bytes are used on both sides. Every input, complete signed output digest, commit ID and repeated result agrees. Workers run sequentially; no task build/test overlaps these formal series. The Windows desktop is shared and is not affinity controlled.

| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| copy-1-peers-1 | checkpoint-copy-signatures | 0.2439 | 0.0015 | -99.3839% | 0.561 | 0.007 |
| copy-1-peers-1 | commit-copy-batch-peers | 0.4441 | 0.0052 | -98.8369% | 2.046 | 0.076 |
| copy-1-peers-1 | commit-copy-signatures | 0.4441 | 0.0015 | -99.6615% | 1.746 | 0.007 |
| copy-1-peers-1 | commit-sign | 0.9881 | 0.5464 | -44.7076% | 7.176 | 5.129 |
| copy-1-peers-1 | snapshot-copy-signatures | 0.9285 | 0.0015 | -99.8381% | 4.383 | 0.008 |
| copy-2048-peers-4 | checkpoint-copy-signatures | 0.2439 | 0.0015 | -99.3839% | 0.307 | 0.008 |
| copy-2048-peers-4 | commit-copy-batch-peers | 137.6590 | 0.0052 | -99.9962% | 263.951 | 0.051 |
| copy-2048-peers-4 | commit-copy-signatures | 137.6678 | 0.0015 | -99.9989% | 254.775 | 0.008 |
| copy-2048-peers-4 | commit-sign | 277.0092 | 143.3433 | -48.2532% | 672.119 | 370.325 |
| copy-2048-peers-4 | snapshot-copy-signatures | 84.7055 | 0.0015 | -99.9982% | 177.759 | 0.008 |
| copy-512-peers-4 | checkpoint-copy-signatures | 0.2439 | 0.0015 | -99.3839% | 0.647 | 0.008 |
| copy-512-peers-4 | commit-copy-batch-peers | 34.0185 | 0.0052 | -99.9848% | 119.113 | 0.082 |
| copy-512-peers-4 | commit-copy-signatures | 34.0192 | 0.0015 | -99.9956% | 110.079 | 0.007 |
| copy-512-peers-4 | commit-sign | 69.5426 | 35.5140 | -48.9320% | 240.732 | 167.871 |
| copy-512-peers-4 | snapshot-copy-signatures | 84.7071 | 0.0015 | -99.9982% | 246.434 | 0.008 |
| copy-64-peers-4 | checkpoint-copy-signatures | 0.2439 | 0.0015 | -99.3839% | 0.583 | 0.007 |
| copy-64-peers-4 | commit-copy-batch-peers | 4.0846 | 0.0052 | -99.8735% | 30.520 | 0.074 |
| copy-64-peers-4 | commit-copy-signatures | 4.0846 | 0.0015 | -99.9632% | 22.007 | 0.007 |
| copy-64-peers-4 | commit-sign | 8.4396 | 4.3565 | -48.3798% | 59.355 | 25.351 |
| copy-64-peers-4 | snapshot-copy-signatures | 10.5758 | 0.0015 | -99.9858% | 55.885 | 0.008 |

Elapsed time, process CPU and 10 ms sampled peaks are descriptive. Managed allocation measures the operation thread. These copy probes establish no end-to-end recovery, throughput, retained-memory or production-concurrency improvement. Signature generation, independent parsing, changed unsigned content and exact metadata comparison retain their costs.
