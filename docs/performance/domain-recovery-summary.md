# Shared-reference, tariff and parking recovery baseline

Input `domain-recovery-baseline.json`; binding `domain-recovery-binding.json`. Eight shapes/all four profiles, 16/64 EVSEs, four stages, two sequential fresh processes/three warmups/three samples: 64 workers / 192 measured calls. This package makes no production change and records a new baseline.

Prepared archives contain shared grid/software/document registries, meters at pool/connection/station/EVSE slots, nested tariff components/restrictions, EVSE/connector tariff references, parking garage/space/group/product relations, targeted membership edits and one signed two-parent merge. Two original Ed25519 peers authenticate each retained commit and batch. Setup, crypto/input controls, full byte/branch/reference checks and disposal are outside measured calls; restore/full recovery still perform fresh trust.

All stages/repeated workers use exact common inputs, complete model/peer fingerprints, static ETags, heads and every branch-state identity. Recovered head models resolve one shared catalog instance per ID and fresh local runtime. Original source EVSE/grid/parking/meter statuses stay local.

Model-only eagerly decodes all suffix models from an already parsed archive tree, using production parsers. Prepared-model restore uses the existing private production replay entry points. Actual v3/v4 recovery keeps lazy suffix model timing. Complete CBOR/JSON recovery includes normal syntax/model/replay work. Stages cannot be added/subtracted as processing shares. Historical simple EVSE-power workloads remain separate controls; different data, edits and peer counts prevent a before/after performance comparison.

Formal workers run sequentially outside task builds/tests on a shared Windows desktop without affinity control. Time, CPU, sampled peaks and collected heap deltas are descriptive; no production capacity or cancellation-latency bound follows.

## Actual domain and archive inventory

| Shape | Entities | Meters / assignments | References | Commits / snapshots / merges | Receipts / catalog IDs | CBOR / JSON bytes |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| domain-v1-evses-16 | 75 | 30 / 60 | 212 | 11 / 0 / 1 | 0 / 0 | 73716 / 56455 |
| domain-v1-evses-64 | 243 | 120 / 240 | 818 | 11 / 0 / 1 | 0 / 0 | 190599 / 125175 |
| domain-v2-evses-16 | 75 | 30 / 60 | 212 | 19 / 1 / 1 | 0 / 0 | 142212 / 106038 |
| domain-v2-evses-64 | 243 | 120 / 240 | 818 | 19 / 1 / 1 | 0 / 0 | 375978 / 243353 |
| domain-v3-evses-16 | 75 | 30 / 60 | 212 | 11 / 1 / 1 | 0 / 0 | 73948 / 56847 |
| domain-v3-evses-64 | 243 | 120 / 240 | 818 | 11 / 1 / 1 | 0 / 0 | 190831 / 125487 |
| domain-v4-evses-16 | 75 | 30 / 60 | 212 | 12 / 1 / 1 | 2 / 10 | 77091 / 61612 |
| domain-v4-evses-64 | 243 | 120 / 240 | 818 | 12 / 1 / 1 | 2 / 10 | 193974 / 130277 |

Every shape contains two grid operators, four software releases, two approval documents, four tariffs/eight elements/24 price components/16 restrictions, one parking operator, two overlapping groups and two parking products. Pools/garages are EVSEs/8, stations EVSEs/4 and spaces EVSEs. All reference counts describe actual retained head edges; repeated references by the same owner collapse in the production reference index. Meter applicability counts describe physical slots.

## Operation-thread allocation and elapsed time

| Shape | Stage | Median MiB | Median ms |
| --- | --- | ---: | ---: |
| domain-v1-evses-16 | read-cbor | 186.3145 | 331.503 |
| domain-v1-evses-16 | read-cbor-model | 41.1463 | 109.393 |
| domain-v1-evses-16 | read-json | 176.7594 | 319.116 |
| domain-v1-evses-16 | read-restore | 143.6541 | 299.100 |
| domain-v1-evses-64 | read-cbor | 621.4921 | 1031.354 |
| domain-v1-evses-64 | read-cbor-model | 134.5444 | 218.516 |
| domain-v1-evses-64 | read-json | 593.0864 | 805.200 |
| domain-v1-evses-64 | read-restore | 482.3120 | 592.363 |
| domain-v2-evses-16 | read-cbor | 308.5489 | 510.075 |
| domain-v2-evses-16 | read-cbor-model | 84.6361 | 171.969 |
| domain-v2-evses-16 | read-json | 288.5730 | 466.193 |
| domain-v2-evses-16 | read-restore | 222.2053 | 337.920 |
| domain-v2-evses-64 | read-cbor | 1026.7926 | 1275.733 |
| domain-v2-evses-64 | read-cbor-model | 276.7380 | 423.926 |
| domain-v2-evses-64 | read-json | 966.6668 | 1164.418 |
| domain-v2-evses-64 | read-restore | 744.5731 | 814.928 |
| domain-v3-evses-16 | read-cbor | 190.0555 | 359.541 |
| domain-v3-evses-16 | read-cbor-model | 43.8664 | 105.074 |
| domain-v3-evses-16 | read-json | 179.4863 | 338.562 |
| domain-v3-evses-16 | read-restore | 146.0476 | 278.795 |
| domain-v3-evses-64 | read-cbor | 632.4286 | 783.239 |
| domain-v3-evses-64 | read-cbor-model | 142.5611 | 199.154 |
| domain-v3-evses-64 | read-json | 600.4803 | 727.891 |
| domain-v3-evses-64 | read-restore | 488.9403 | 552.941 |
| domain-v4-evses-16 | read-cbor | 222.0006 | 410.793 |
| domain-v4-evses-16 | read-cbor-model | 43.9891 | 94.395 |
| domain-v4-evses-16 | read-json | 211.4374 | 374.639 |
| domain-v4-evses-16 | read-restore | 177.6746 | 308.907 |
| domain-v4-evses-64 | read-cbor | 754.3716 | 864.458 |
| domain-v4-evses-64 | read-cbor-model | 142.7226 | 205.569 |
| domain-v4-evses-64 | read-json | 722.3594 | 803.537 |
| domain-v4-evses-64 | read-restore | 610.7982 | 637.272 |

## Collected additional recovered-history memory

One additional CBOR-recovered history is collected with source/input still live. Approximate process-heap deltas include model/maps/runtime/retained states; they provide no constant total-memory bound.

| Shape | Median MiB |
| --- | ---: |
| domain-v1-evses-16 | 1.0286 |
| domain-v1-evses-64 | 3.2469 |
| domain-v2-evses-16 | 1.2785 |
| domain-v2-evses-64 | 3.9949 |
| domain-v3-evses-16 | 1.0087 |
| domain-v3-evses-64 | 3.3239 |
| domain-v4-evses-16 | 0.9987 |
| domain-v4-evses-64 | 3.2565 |

Use this baseline to select and compare later model/reference/replay changes against exactly these inputs and current verification contracts. Individual trees, quantity parsing, reference resolution, fresh cryptography and retained immutable states remain costs. Production concurrency, larger catalogs and independent implementations require separate measurements.
