# loadtest-ryzen7-3700x-stream-20261023-074653

```
WWCP POI 3 load test
  machine   AMD Ryzen 7 3700X 8-Core Processor, 16 logical processors, 127,93 GiB, Microsoft Windows 10.0.26300, X64
  runtime   .NET 10.0.13, server GC, Interactive
  builds    WWCP_POI3 Release WWCP_POI@646ba5e189-dirty, Styx Release Styx@e721646f19-dirty, WWCP_POI3_LoadTests Release WWCP_POI@646ba5e189-dirty
  network   1.000.000 charging stations of 200 operators (zipf 1), 2 per pool, 2 EVSEs each
  commits   5 + 50 one after another and 0 merged, 1 change(s) each; writers 0,1 for 00:00:20, readers EVSE 3, Proof 1, Commit 1, 1000 status updates/s
```

## Sizes

| stations | evses | operators | largest operator | store | entities s | network s | start s | start CPU s | managed after start MiB | managed bytes/station | peak working set MiB | first proof s | commit p50 ms | commit p90 ms | commit p99 ms | commit max ms | commit CPU p50 ms | allocated/commit MiB | gen2/commit | commits/s | merge commit p50 ms | allocated/merge MiB | managed after commits MiB | base.json MiB | compact s | reload s | reload managed MiB | reload peak working set MiB | reload first proof s |
| ---: | ---: | ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1,000,000 | 2,000,000 | 200 | 170,125 | file | 74.6 | 4.7 | 276.8 | 687.2 | 24,552 | 25,745 | 56,573 | 0.07 | 11998.0 | 13185.8 | 14876.8 | 14876.8 | 17187.5 | 1460.8 | 0.00 | 0.04 | 0.0 | 0.0 | 25,403 | 1,517 | 82.6 | 1034.7 | 25,791 | 59,649 | 0.08 |

## Concurrency

| stations | store | writers | readers evse+proof+commit | status/s target | commits/s | committed | merges | refused | commit p50 ms | commit p99 ms | prepare p50 ms | evse reads/s | evse p99 ms | proofs/s | proof p50 ms | proof p99 ms | commit reads/s | commit read max ms | commit reads >10 ms | commit read wait s | status/s | status p99 ms | CPU % | allocated MiB/s | gen2 | gc pause ms | peak working set MiB |
| ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1,000,000 | file | 0 | 3+1+1 | 1,000 | 0.00 | 0 | 0 | 0 | 0.0 | 0.0 | 0.0 | 36,797 | 0.01 | 4 | 145.41 | 1173.9 | 356,813 | 162.1 | 36 | 5.4 | 992 | 0.16 | 18.1 | 217 | 0 | 18,243 | 29,387 |
| 1,000,000 | file | 1 | 3+1+1 | 1,000 | 0.02 | 1 | 0 | 0 | 13055.2 | 13055.2 | 31266.0 | 7,992 | 0.01 | 1 | 154.09 | 2809.0 | 92,401 | 1205.6 | 23 | 6.2 | 202 | 0.17 | 10.2 | 108 | 0 | 28,986 | 27,704 |
