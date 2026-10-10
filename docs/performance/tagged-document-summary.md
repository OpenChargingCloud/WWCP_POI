# Individual encoding and borrowed stream measurements

Input: `tagged-document-after.json`. 2 isolated processes per operation, 5 measured samples and 5 excluded warmups per process.

All samples and inventories are checked. Buffered/stream archive bytes and digests must agree. Snapshot JSON document/canonical stages and each final CBOR stage must agree with their complete encoders. Intermediate tree stages retain textual ETags and have their own verified digests.

Stage inputs are prebuilt; consuming/encoding and byte verification occur after the allocation/time window. Complete encoder and archive operations include counting and SHA-256. Stage medians must not be added or presented as fractions of complete calls. Managed peaks include retained setup inputs and post-checks from earlier calls; 10 ms sampling misses short-lived peaks. Lifetime working set includes setup and validation.

## graph-128

EVSEs: 128; graph nodes: 291; commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| snapshot-cbor | 46.391 | 41.663–56.395 | 46.88 | 17.1315 | 13.3 | 78600 | 1/0/0 |
| snapshot-json-document | 33.533 | 31.099–46.577 | 54.69 | 10.8623 | 11.9 | 116531 | 1/0/0 |
| snapshot-reading-paths | 1.091 | 1.059–1.235 | 0.00 | 0.2632 | 5.6 | 0 | 0/0/0 |
| snapshot-json-canonical | 3.100 | 2.547–3.798 | 0.00 | 1.0579 | 6.4 | 116531 | 0/0/0 |
| snapshot-cbor-tree | 3.380 | 3.196–4.748 | 0.00 | 2.3313 | 8.3 | 101856 | 0/0/0 |
| snapshot-etag-tree | 2.902 | 2.842–3.027 | 0.00 | 1.2371 | 6.8 | 78600 | 0/0/0 |
| snapshot-cbor-write | 2.730 | 2.129–3.087 | 0.00 | 1.3794 | 6.8 | 78600 | 0/0/0 |
| changeset-cbor | 0.334 | 0.309–0.466 | 0.00 | 0.0342 | 3.1 | 654 | 0/0/0 |
| changeset-json | 0.098 | 0.087–0.113 | 0.00 | 0.0019 | 3.1 | 923 | 0/0/0 |
| changeset-validate-paths | 0.034 | 0.032–0.039 | 0.00 | 0.0027 | 3.1 | 0 | 0/0/0 |
| changeset-cbor-tree | 0.128 | 0.121–0.142 | 0.00 | 0.0171 | 3.1 | 798 | 0/0/0 |
| changeset-etag-tree | 0.082 | 0.074–0.093 | 0.00 | 0.0050 | 3.1 | 654 | 0/0/0 |
| changeset-cbor-write | 0.053 | 0.043–0.064 | 0.00 | 0.0070 | 3.1 | 654 | 0/0/0 |
| archive-json | 7.701 | 7.217–10.730 | 0.00 | 2.1099 | 5.1 | 153021 | 0/0/0 |
| archive-json-stream | 7.896 | 7.184–13.624 | 7.81 | 1.1887 | 4.3 | 153021 | 0/0/0 |
| archive-cbor | 189.149 | 148.749–279.980 | 296.88 | 52.7057 | 14.7 | 249862 | 7/3/1 |
| archive-cbor-stream | 160.644 | 134.129–298.720 | 242.19 | 51.9041 | 17.8 | 249862 | 6/3/1 |

## graph-512

EVSEs: 512; graph nodes: 1155; commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| snapshot-cbor | 138.110 | 96.319–170.403 | 234.38 | 68.3861 | 30.8 | 311525 | 10/6/3 |
| snapshot-json-document | 71.201 | 53.085–109.756 | 140.62 | 43.3865 | 32.3 | 462064 | 4/1/0 |
| snapshot-reading-paths | 2.573 | 1.942–2.968 | 0.00 | 1.0519 | 19.4 | 0 | 0/0/0 |
| snapshot-json-canonical | 7.052 | 6.962–8.820 | 7.81 | 4.2737 | 22.7 | 462064 | 0/0/0 |
| snapshot-cbor-tree | 13.363 | 7.748–14.471 | 15.62 | 9.2924 | 24.0 | 403901 | 1/0/0 |
| snapshot-etag-tree | 7.897 | 7.652–8.714 | 15.62 | 4.9065 | 24.1 | 311525 | 0/0/0 |
| snapshot-cbor-write | 2.270 | 2.160–2.414 | 0.00 | 5.4748 | 24.1 | 311525 | 0/0/0 |
| changeset-cbor | 0.275 | 0.232–0.342 | 0.00 | 0.0341 | 8.8 | 654 | 0/0/0 |
| changeset-json | 0.101 | 0.082–0.183 | 0.00 | 0.0019 | 8.8 | 918 | 0/0/0 |
| changeset-validate-paths | 0.025 | 0.021–0.033 | 0.00 | 0.0027 | 8.8 | 0 | 0/0/0 |
| changeset-cbor-tree | 0.105 | 0.092–0.119 | 0.00 | 0.0171 | 8.8 | 798 | 0/0/0 |
| changeset-etag-tree | 0.063 | 0.054–0.068 | 0.00 | 0.0050 | 8.8 | 654 | 0/0/0 |
| changeset-cbor-write | 0.039 | 0.027–0.049 | 0.00 | 0.0070 | 8.8 | 654 | 0/0/0 |
| archive-json | 27.675 | 23.778–39.703 | 39.06 | 8.0979 | 16.9 | 544530 | 0/0/0 |
| archive-json-stream | 15.541 | 5.794–32.875 | 23.44 | 4.4525 | 13.3 | 544530 | 0/0/0 |
| archive-cbor | 338.936 | 301.638–355.681 | 710.94 | 208.6901 | 43.8 | 948637 | 26/12/5 |
| archive-cbor-stream | 336.727 | 293.214–420.493 | 640.62 | 205.6671 | 47.2 | 948637 | 26/11/5 |

## history-64

EVSEs: 16; graph nodes: 39; commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| snapshot-cbor | 5.971 | 5.615–6.253 | 0.00 | 2.2838 | 3.8 | 10782 | 0/0/0 |
| snapshot-json-document | 4.549 | 3.894–5.089 | 0.00 | 1.4312 | 3.1 | 15869 | 0/0/0 |
| snapshot-reading-paths | 0.190 | 0.180–0.199 | 0.00 | 0.0348 | 1.8 | 0 | 0/0/0 |
| snapshot-json-canonical | 0.409 | 0.376–0.445 | 0.00 | 0.1582 | 2.0 | 15869 | 0/0/0 |
| snapshot-cbor-tree | 0.618 | 0.591–0.871 | 0.00 | 0.3076 | 2.2 | 13878 | 0/0/0 |
| snapshot-etag-tree | 0.460 | 0.446–0.627 | 0.00 | 0.1668 | 2.0 | 10782 | 0/0/0 |
| snapshot-cbor-write | 0.494 | 0.306–0.703 | 0.00 | 0.1848 | 2.0 | 10782 | 0/0/0 |
| changeset-cbor | 0.330 | 0.307–0.428 | 0.00 | 0.0341 | 1.7 | 655 | 0/0/0 |
| changeset-json | 0.088 | 0.085–0.094 | 0.00 | 0.0019 | 1.7 | 908 | 0/0/0 |
| changeset-validate-paths | 0.043 | 0.040–0.318 | 0.00 | 0.0027 | 1.7 | 0 | 0/0/0 |
| changeset-cbor-tree | 0.140 | 0.127–0.203 | 0.00 | 0.0171 | 1.7 | 799 | 0/0/0 |
| changeset-etag-tree | 0.087 | 0.071–0.247 | 0.00 | 0.0050 | 1.7 | 655 | 0/0/0 |
| changeset-cbor-write | 0.056 | 0.048–0.080 | 0.00 | 0.0070 | 1.7 | 655 | 0/0/0 |
| archive-json | 3.821 | 3.698–4.024 | 0.00 | 1.4213 | 3.1 | 160077 | 0/0/0 |
| archive-json-stream | 3.506 | 3.357–4.195 | 0.00 | 0.4836 | 2.1 | 160077 | 0/0/0 |
| archive-cbor | 45.307 | 43.780–56.089 | 46.88 | 15.4318 | 9.6 | 144594 | 1/0/0 |
| archive-cbor-stream | 48.276 | 40.674–65.874 | 54.69 | 14.8181 | 8.7 | 144594 | 1/0/0 |

## pruned-4

EVSEs: 16; graph nodes: 39; commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| snapshot-cbor | 6.484 | 6.377–6.884 | 0.00 | 2.2838 | 3.8 | 10782 | 0/0/0 |
| snapshot-json-document | 4.602 | 4.250–5.124 | 0.00 | 1.4312 | 3.2 | 15869 | 0/0/0 |
| snapshot-reading-paths | 0.212 | 0.173–0.248 | 0.00 | 0.0348 | 1.9 | 0 | 0/0/0 |
| snapshot-json-canonical | 0.422 | 0.383–0.560 | 0.00 | 0.1582 | 2.0 | 15869 | 0/0/0 |
| snapshot-cbor-tree | 0.453 | 0.414–0.642 | 0.00 | 0.3076 | 2.3 | 13878 | 0/0/0 |
| snapshot-etag-tree | 0.474 | 0.440–0.525 | 0.00 | 0.1668 | 2.1 | 10782 | 0/0/0 |
| snapshot-cbor-write | 0.491 | 0.479–0.501 | 0.00 | 0.1848 | 2.1 | 10782 | 0/0/0 |
| changeset-cbor | 0.320 | 0.312–0.387 | 0.00 | 0.0341 | 1.5 | 655 | 0/0/0 |
| changeset-json | 0.090 | 0.081–0.101 | 0.00 | 0.0019 | 1.5 | 918 | 0/0/0 |
| changeset-validate-paths | 0.028 | 0.025–0.033 | 0.00 | 0.0027 | 1.5 | 0 | 0/0/0 |
| changeset-cbor-tree | 0.140 | 0.128–0.143 | 0.00 | 0.0171 | 1.6 | 799 | 0/0/0 |
| changeset-etag-tree | 0.060 | 0.058–0.123 | 0.00 | 0.0050 | 1.5 | 655 | 0/0/0 |
| changeset-cbor-write | 0.052 | 0.046–0.074 | 0.00 | 0.0070 | 1.6 | 655 | 0/0/0 |
| archive-json | 2.870 | 2.752–3.165 | 0.00 | 0.9400 | 2.5 | 106105 | 0/0/0 |
| archive-json-stream | 2.901 | 2.778–3.755 | 0.00 | 0.4044 | 1.9 | 106105 | 0/0/0 |
| archive-cbor | 39.225 | 37.300–43.842 | 62.50 | 13.5569 | 8.3 | 105525 | 1/0/0 |
| archive-cbor-stream | 36.631 | 31.046–56.561 | 54.69 | 13.2620 | 9.0 | 105525 | 1/0/0 |
