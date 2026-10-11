# Mapped recovery measurements

44 fresh workers / 132 samples. Each group uses two processes, three warmups and three measured calls.

File controls use the same new build and seven prepared archives/all four profiles. The array control reproduces the preceding Open input algorithm (writer lease, known-length allocation, exact read/EOF probe, span replay). The mapped operation calls production Open. Both retain the writer lease and recovered history until exact branch/archive/peer/runtime verification and disposal outside measurement. Prepared disk input and source bytes are setup; file opening, capture/mapping and replay are measured. Original bytes/inventories/results also bind to stream-input-recovery.json; its old timings are not a speedup control.

The paired cold/bootstrap runs use the preceding preserved production/harness binaries and newly built binaries. Workload.cs and its measured operations are unchanged. The new harness only adds independent file operations. Prepared static/archive identities, inventories and every output result match. Bootstrap includes durable chunk transfer and default one-MiB capture, with staging cleanup outside measurement; cold retrieval includes digest/current trust/receipt verification. Source/staging payload counts do not account for total physical input-spool I/O.

No task build/test overlaps measurement. Workers run sequentially on a shared desktop without affinity control. Timings and ten-ms sampled peaks are descriptive; operation-thread managed allocations differ from mapped resident pages, OS cache, total disk use and retained branch/model memory. Neither these samples nor byte limits establish constant total memory or cancellation latency.

| Shape | Input | Median ms | CPU ms | Allocated MiB | Allocation vs array | Largest sampled managed MiB | Largest sampled working set MiB |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| recover-batch-1-peers-1 | read-cbor-file-array | 17.89 | 15.62 | 5.2091 | +0.000% | 6.32 | 69.57 |
| recover-batch-1-peers-1 | read-cbor-file-mapped | 16.94 | 15.62 | 5.2022 | -0.133% | 6.31 | 69.68 |
| recover-batch-2048-peers-4 | read-cbor-file-array | 2445.53 | 3242.19 | 1655.9660 | +0.000% | 101.88 | 201.14 |
| recover-batch-2048-peers-4 | read-cbor-file-mapped | 2402.19 | 3164.06 | 1655.8341 | -0.008% | 101.94 | 203.27 |
| recover-boundary-16 | read-cbor-file-array | 184.16 | 281.25 | 48.0195 | +0.000% | 10.13 | 78.89 |
| recover-boundary-16 | read-cbor-file-mapped | 185.65 | 296.88 | 47.9870 | -0.068% | 10.38 | 82.20 |
| recover-graph-128 | read-cbor-file-array | 699.45 | 1093.75 | 426.4279 | +0.000% | 41.46 | 131.41 |
| recover-graph-128 | read-cbor-file-mapped | 752.35 | 1437.50 | 426.5608 | +0.031% | 39.91 | 128.40 |
| recover-graph-512 | read-cbor-file-array | 3496.08 | 4476.56 | 1689.3026 | +0.000% | 104.76 | 207.16 |
| recover-graph-512 | read-cbor-file-mapped | 3480.09 | 4609.38 | 1688.6606 | -0.038% | 104.66 | 201.36 |
| recover-history-64 | read-cbor-file-array | 336.82 | 570.31 | 145.8970 | +0.000% | 13.13 | 96.23 |
| recover-history-64 | read-cbor-file-mapped | 401.06 | 843.75 | 145.8300 | -0.046% | 14.92 | 97.67 |
| recover-pruned-4 | read-cbor-file-array | 310.14 | 523.44 | 122.5524 | +0.000% | 12.80 | 96.62 |
| recover-pruned-4 | read-cbor-file-mapped | 413.48 | 773.44 | 122.6080 | +0.045% | 12.08 | 94.08 |

| Shape | Operation | Before MiB | After MiB | Allocation change | Before ms | After ms |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| integrated-128 | bootstrap-transfer | 798.9277 | 799.4715 | +0.068% | 1201.18 | 1101.17 |
| integrated-128 | cold-read | 334.9266 | 334.9145 | -0.004% | 555.61 | 517.90 |
| integrated-512 | bootstrap-transfer | 3121.0420 | 3120.8045 | -0.008% | 5184.30 | 5121.26 |
| integrated-512 | cold-read | 1326.6275 | 1325.7929 | -0.063% | 2460.77 | 2322.85 |
