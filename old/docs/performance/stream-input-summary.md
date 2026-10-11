# Borrowed CBOR input measurements

Raw report: `stream-input-recovery.json`. Exact preceding input control: `indexed-archive-after.json` (SHA-256 `63594df8cb8f5faf1a7d900e338ce4676cdaa7223cee9159077a336dceec71aa`).

42 fresh workers / 210 samples, seven shapes/all four profiles, two processes/five warmups/five calls. All three operations use one new production/harness build and identical prepared input bytes, state/commit/receipt/peer inventories and recovered heads/results. Span control is freshly measured; the preceding report only binds exact inputs/results, not a before/after speedup.

Memory capture allows the entire archive under its byte budget. File capture forces a temporary spool at zero threshold, shared maintenance lease, async-independent synchronous reads, flush and read-only mapping. Both sources are borrowed non-seekable wrappers returning at most 16 KiB per read. Each source wrapper is created inside measurement; prepared bytes/options are setup. File capture includes its actual I/O/map/handle cleanup costs. Exact archive/branch/peer/runtime verification is outside measurement.

No task build/test overlaps the formal run. Host is shared and not affinity controlled. Operation-thread managed allocation differs from 10 ms sampled managed/working-set peaks and file-backed resident pages. The fixture retains its original input bytes in all operations. These are local source/disk recovery costs, not WAN, cancellation latency or total-memory bounds.

| Shape | Operation | Median ms | Process CPU ms | Allocated MiB | Allocation vs span | Min–max ms | Largest sampled managed MiB | Largest sampled working set MiB |
| --- | --- | ---: | ---: | ---: | ---: | --- | ---: | ---: |
| recover-batch-1-peers-1 | read-cbor | 15.14 | 15.62 | 5.2005 | +0.00% | 14.95–15.51 | 6.31 | 69.41 |
| recover-batch-1-peers-1 | read-cbor-input-memory | 16.33 | 23.44 | 5.2675 | +1.29% | 14.93–27.84 | 6.39 | 72.05 |
| recover-batch-1-peers-1 | read-cbor-input-spool | 16.94 | 15.62 | 5.3287 | +2.47% | 16.22–26.03 | 6.44 | 71.60 |
| recover-batch-2048-peers-4 | read-cbor | 2279.68 | 3078.12 | 1656.9736 | +0.00% | 2251.49–2322.45 | 96.91 | 207.04 |
| recover-batch-2048-peers-4 | read-cbor-input-memory | 2285.13 | 3203.12 | 1659.6032 | +0.16% | 2191.76–2353.09 | 96.77 | 197.14 |
| recover-batch-2048-peers-4 | read-cbor-input-spool | 2299.45 | 2773.44 | 1657.7068 | +0.04% | 2271.75–2358.39 | 101.18 | 198.11 |
| recover-boundary-16 | read-cbor | 194.61 | 281.25 | 47.9845 | +0.00% | 167.23–218.67 | 11.32 | 83.89 |
| recover-boundary-16 | read-cbor-input-memory | 203.64 | 328.12 | 48.0950 | +0.23% | 188.93–216.23 | 10.45 | 86.13 |
| recover-boundary-16 | read-cbor-input-spool | 205.62 | 312.50 | 48.1163 | +0.27% | 148.93–223.50 | 11.13 | 84.41 |
| recover-graph-128 | read-cbor | 718.67 | 1273.44 | 425.6019 | +0.00% | 668.94–773.29 | 37.29 | 131.13 |
| recover-graph-128 | read-cbor-input-memory | 686.12 | 1226.56 | 426.1131 | +0.12% | 648.42–704.54 | 40.34 | 131.88 |
| recover-graph-128 | read-cbor-input-spool | 703.47 | 1296.88 | 426.5675 | +0.23% | 645.25–760.20 | 40.12 | 131.74 |
| recover-graph-512 | read-cbor | 3026.59 | 4117.19 | 1687.9580 | +0.00% | 2951.31–3325.40 | 107.92 | 200.65 |
| recover-graph-512 | read-cbor-input-memory | 2952.12 | 3937.50 | 1690.6336 | +0.16% | 2780.43–3055.27 | 101.75 | 197.35 |
| recover-graph-512 | read-cbor-input-spool | 2920.13 | 3835.94 | 1688.7723 | +0.05% | 2807.78–3047.46 | 106.64 | 202.81 |
| recover-history-64 | read-cbor | 321.37 | 671.88 | 145.7841 | +0.00% | 230.74–401.56 | 12.58 | 95.81 |
| recover-history-64 | read-cbor-input-memory | 391.20 | 789.06 | 146.3657 | +0.40% | 282.30–441.27 | 12.86 | 98.73 |
| recover-history-64 | read-cbor-input-spool | 291.40 | 539.06 | 145.8476 | +0.04% | 249.28–314.10 | 12.08 | 94.03 |
| recover-pruned-4 | read-cbor | 299.09 | 632.81 | 122.4238 | +0.00% | 208.52–411.95 | 12.13 | 96.49 |
| recover-pruned-4 | read-cbor-input-memory | 330.61 | 703.12 | 122.6491 | +0.18% | 252.48–508.37 | 12.59 | 95.73 |
| recover-pruned-4 | read-cbor-input-spool | 316.96 | 593.75 | 122.5095 | +0.07% | 237.29–460.40 | 11.77 | 95.56 |
