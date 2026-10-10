# Indexed archive recovery comparison

Before: `indexed-archive-before.json`; after: `indexed-archive-after.json`.

Before is an exact 42-worker / 210-sample subset of the preceding 98-worker decoder baseline; no new before executions are implied. After contains 42 fresh workers / 210 samples. All seven shapes/four profiles and complete CBOR/JSON recovery plus prepared-model restore match in bytes, identities, static/commit/receipt/peer inventories and repeated outputs. The C# harness, runtime tuning, dependencies and 2/5/5 process/warmup/sample method agree.

Full-CBOR recovery now indexes/validates borrowed input before decoding individual parts. Boundary suffix bytes are privately frozen before callbacks can mutate original input; no complete archive tree remains. JSON and prepared-model restore are controls; their boundary path now uses the shared synchronous cursor. Verification and retention probes remain outside measurement.

No build/test from this task overlaps the new formal series. The host is not dedicated or affinity controlled. Times and 10 ms sampled peaks are descriptive. Allocation covers the operation thread; CPU includes the process sampling thread. Collected retention deltas and sampled peaks are not capacity bounds.

| Shape | Operation | Before/after ms | Before/after CPU ms | Before/after allocated MiB | Allocation change | After min–max ms | After largest sampled managed MiB |
| --- | --- | ---: | ---: | ---: | ---: | --- | ---: |
| recover-batch-1-peers-1 | read-cbor | 16.80/15.95 | 15.62/15.62 | 5.2363/5.2000 | -0.69% | 15.38–22.18 | 6.31 |
| recover-batch-1-peers-1 | read-json | 22.54/14.47 | 46.88/15.62 | 4.8948/4.8949 | +0.00% | 14.19–17.86 | 6.01 |
| recover-batch-1-peers-1 | read-restore | 10.69/9.89 | 7.81/15.62 | 3.2599/3.2595 | -0.01% | 9.68–10.63 | 4.38 |
| recover-batch-2048-peers-4 | read-cbor | 2388.87/2376.81 | 3250.00/3273.44 | 1662.9300/1656.3946 | -0.39% | 2311.30–3458.33 | 104.66 |
| recover-batch-2048-peers-4 | read-json | 2345.27/2221.31 | 3132.81/2968.75 | 1580.9354/1580.2547 | -0.04% | 2191.15–2420.22 | 99.84 |
| recover-batch-2048-peers-4 | read-restore | 2066.03/1680.32 | 2773.44/2492.19 | 1294.6825/1294.2449 | -0.03% | 1654.35–2772.29 | 94.49 |
| recover-boundary-16 | read-cbor | 174.62/177.39 | 265.62/250.00 | 47.7422/47.9859 | +0.51% | 164.37–185.14 | 11.43 |
| recover-boundary-16 | read-json | 183.61/167.69 | 281.25/250.00 | 43.4435/43.4451 | +0.00% | 154.89–184.51 | 10.41 |
| recover-boundary-16 | read-restore | 105.61/98.27 | 156.25/140.62 | 24.2253/24.2189 | -0.03% | 84.14–102.62 | 10.36 |
| recover-graph-128 | read-cbor | 697.46/730.71 | 1218.75/1398.44 | 426.2697/425.6334 | -0.15% | 704.31–778.08 | 41.59 |
| recover-graph-128 | read-json | 618.45/706.43 | 1179.69/1210.94 | 378.3821/379.1826 | +0.21% | 604.26–731.53 | 42.74 |
| recover-graph-128 | read-restore | 339.80/301.88 | 593.75/585.94 | 170.7813/169.7172 | -0.62% | 255.32–322.49 | 30.42 |
| recover-graph-512 | read-cbor | 8494.51/3335.12 | 6656.25/4492.19 | 1687.9206/1688.5022 | +0.03% | 3174.23–3873.28 | 104.81 |
| recover-graph-512 | read-json | 3956.48/3093.54 | 5242.19/4007.81 | 1501.8238/1502.2531 | +0.03% | 2841.88–4174.51 | 98.27 |
| recover-graph-512 | read-restore | 1558.98/1210.87 | 2046.88/1679.69 | 667.0846/667.0472 | -0.01% | 1116.12–1594.06 | 90.52 |
| recover-history-64 | read-cbor | 316.00/284.20 | 578.12/554.69 | 144.8399/145.4852 | +0.45% | 200.85–337.54 | 14.54 |
| recover-history-64 | read-json | 249.14/326.96 | 406.25/625.00 | 132.3561/132.3709 | +0.01% | 293.31–370.19 | 12.13 |
| recover-history-64 | read-restore | 245.77/255.33 | 453.12/500.00 | 81.9433/82.1376 | +0.24% | 217.63–327.25 | 12.14 |
| recover-pruned-4 | read-cbor | 343.58/290.37 | 648.44/531.25 | 121.4217/122.2105 | +0.65% | 218.79–331.54 | 12.24 |
| recover-pruned-4 | read-json | 225.53/278.84 | 414.06/554.69 | 109.8996/110.0238 | +0.11% | 212.79–447.67 | 12.32 |
| recover-pruned-4 | read-restore | 264.78/207.13 | 476.56/343.75 | 60.9034/60.9901 | +0.14% | 141.82–230.14 | 12.02 |

## Collected additional recovered-history memory

One additional actual recovered history is held through collection in each full-CBOR worker, source/input kept alive on both sides. Approximate deltas include retained states/models/runtime, and other objects can become collectible. Cumulative allocation and retained memory are distinct.

| Shape | Before median MiB | After median MiB | After min–max MiB |
| --- | ---: | ---: | --- |
| recover-batch-1-peers-1 | 0.0725 | 0.0725 | 0.0725–0.0725 |
| recover-batch-2048-peers-4 | 11.5623 | 11.7439 | 11.7413–11.7464 |
| recover-boundary-16 | 0.4890 | 0.4994 | 0.4986–0.5001 |
| recover-graph-128 | 3.4028 | 3.4347 | 3.4317–3.4378 |
| recover-graph-512 | 13.4308 | 13.4421 | 13.4402–13.4440 |
| recover-history-64 | 1.0364 | 1.0202 | 0.9985–1.0419 |
| recover-pruned-4 | 0.8964 | 0.9141 | 0.9094–0.9188 |
