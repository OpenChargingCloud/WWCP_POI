# Individual encoding and borrowed stream measurements

Input: `encoding-baseline.json`. 2 isolated processes per operation, 5 measured samples and 5 excluded warmups per process.

All samples and inventories are checked. Buffered/stream archive bytes and digests must agree. Snapshot JSON document/canonical stages and each final CBOR stage must agree with their complete encoders. Intermediate tree stages retain textual ETags and have their own verified digests.

Stage inputs are prebuilt; consuming/encoding and byte verification occur after the allocation/time window. Complete encoder and archive operations include counting and SHA-256. Stage medians must not be added or presented as fractions of complete calls. Managed peaks include retained setup inputs and post-checks from earlier calls; 10 ms sampling misses short-lived peaks. Lifetime working set includes setup and validation.

## graph-128

EVSEs: 128; graph nodes: 291; commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| snapshot-cbor | 50.290 | 47.963–58.887 | 54.69 | 20.4599 | 10.2 | 78600 | 2/1/0 |
| snapshot-json-document | 38.053 | 35.434–40.740 | 46.88 | 14.1907 | 12.2 | 116531 | 1/0/0 |
| snapshot-reading-paths | 1.077 | 1.040–1.251 | 0.00 | 0.2632 | 5.6 | 0 | 0/0/0 |
| snapshot-json-canonical | 2.496 | 2.425–3.654 | 0.00 | 1.0579 | 6.4 | 116531 | 0/0/0 |
| snapshot-cbor-tree | 3.482 | 3.259–4.331 | 0.00 | 2.3313 | 8.3 | 101856 | 0/0/0 |
| snapshot-etag-tree | 2.377 | 2.182–2.537 | 0.00 | 1.2371 | 6.8 | 78600 | 0/0/0 |
| snapshot-cbor-write | 2.156 | 2.004–2.311 | 0.00 | 1.3794 | 6.7 | 78600 | 0/0/0 |
| changeset-cbor | 0.333 | 0.320–0.373 | 0.00 | 0.0342 | 3.1 | 654 | 0/0/0 |
| changeset-json | 0.098 | 0.091–0.117 | 0.00 | 0.0019 | 3.1 | 923 | 0/0/0 |
| changeset-validate-paths | 0.034 | 0.031–0.038 | 0.00 | 0.0027 | 3.1 | 0 | 0/0/0 |
| changeset-cbor-tree | 0.134 | 0.129–0.169 | 0.00 | 0.0171 | 3.1 | 798 | 0/0/0 |
| changeset-etag-tree | 0.066 | 0.059–0.071 | 0.00 | 0.0050 | 3.1 | 654 | 0/0/0 |
| changeset-cbor-write | 0.053 | 0.045–0.067 | 0.00 | 0.0070 | 3.1 | 654 | 0/0/0 |
| archive-json | 7.096 | 6.979–7.533 | 0.00 | 2.1099 | 5.1 | 153021 | 0/0/0 |
| archive-json-stream | 7.426 | 6.971–9.778 | 0.00 | 1.1887 | 4.3 | 153021 | 0/0/0 |
| archive-cbor | 176.922 | 104.952–186.346 | 250.00 | 62.6910 | 14.5 | 249862 | 8/3/1 |
| archive-cbor-stream | 187.888 | 176.750–210.631 | 296.88 | 61.8892 | 18.6 | 249862 | 7/3/1 |

## graph-512

EVSEs: 512; graph nodes: 1155; commits: 13; receipts: 0; catalog IDs: 0.

| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| snapshot-cbor | 118.043 | 101.467–125.004 | 242.19 | 81.6685 | 35.9 | 311525 | 9/4/1 |
| snapshot-json-document | 83.051 | 74.342–90.359 | 109.38 | 56.6690 | 33.9 | 462064 | 6/1/0 |
| snapshot-reading-paths | 1.773 | 1.757–1.849 | 0.00 | 1.0519 | 19.5 | 0 | 0/0/0 |
| snapshot-json-canonical | 6.446 | 6.315–6.882 | 0.00 | 4.2737 | 22.7 | 462064 | 0/0/0 |
| snapshot-cbor-tree | 12.096 | 10.665–14.801 | 15.62 | 9.2924 | 24.0 | 403901 | 1/0/0 |
| snapshot-etag-tree | 7.659 | 7.033–8.020 | 15.62 | 4.9065 | 24.1 | 311525 | 0/0/0 |
| snapshot-cbor-write | 2.277 | 2.258–2.704 | 0.00 | 5.4748 | 24.1 | 311525 | 0/0/0 |
| changeset-cbor | 0.260 | 0.253–0.445 | 0.00 | 0.0341 | 8.8 | 654 | 0/0/0 |
| changeset-json | 0.097 | 0.086–0.123 | 0.00 | 0.0019 | 8.8 | 918 | 0/0/0 |
| changeset-validate-paths | 0.024 | 0.023–0.028 | 0.00 | 0.0027 | 8.8 | 0 | 0/0/0 |
| changeset-cbor-tree | 0.089 | 0.085–0.108 | 0.00 | 0.0171 | 8.8 | 798 | 0/0/0 |
| changeset-etag-tree | 0.068 | 0.056–0.205 | 0.00 | 0.0050 | 8.8 | 654 | 0/0/0 |
| changeset-cbor-write | 0.048 | 0.045–0.055 | 0.00 | 0.0070 | 8.8 | 654 | 0/0/0 |
| archive-json | 15.445 | 6.124–25.326 | 15.62 | 7.9921 | 16.8 | 544530 | 0/0/0 |
| archive-json-stream | 21.566 | 12.285–26.792 | 15.62 | 4.4525 | 13.3 | 544530 | 0/0/0 |
| archive-cbor | 370.434 | 361.700–404.024 | 656.25 | 248.5378 | 35.9 | 948637 | 31/12/5 |
| archive-cbor-stream | 369.874 | 352.912–431.808 | 617.19 | 245.5146 | 41.6 | 948637 | 30/12/4 |

## history-64

EVSEs: 16; graph nodes: 39; commits: 77; receipts: 0; catalog IDs: 0.

| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| snapshot-cbor | 7.191 | 6.511–8.170 | 0.00 | 2.7093 | 4.3 | 10782 | 0/0/0 |
| snapshot-json-document | 6.086 | 5.803–6.630 | 0.00 | 1.8567 | 3.6 | 15869 | 0/0/0 |
| snapshot-reading-paths | 0.210 | 0.187–0.250 | 0.00 | 0.0348 | 1.9 | 0 | 0/0/0 |
| snapshot-json-canonical | 0.441 | 0.417–0.455 | 0.00 | 0.1582 | 2.0 | 15869 | 0/0/0 |
| snapshot-cbor-tree | 0.561 | 0.450–0.634 | 0.00 | 0.3076 | 2.2 | 13878 | 0/0/0 |
| snapshot-etag-tree | 0.403 | 0.365–0.458 | 0.00 | 0.1668 | 2.0 | 10782 | 0/0/0 |
| snapshot-cbor-write | 0.316 | 0.308–0.469 | 0.00 | 0.1848 | 2.0 | 10782 | 0/0/0 |
| changeset-cbor | 0.347 | 0.327–0.431 | 0.00 | 0.0341 | 1.7 | 655 | 0/0/0 |
| changeset-json | 0.094 | 0.088–0.234 | 0.00 | 0.0019 | 1.7 | 908 | 0/0/0 |
| changeset-validate-paths | 0.030 | 0.027–0.039 | 0.00 | 0.0027 | 1.7 | 0 | 0/0/0 |
| changeset-cbor-tree | 0.126 | 0.116–0.144 | 0.00 | 0.0171 | 1.7 | 799 | 0/0/0 |
| changeset-etag-tree | 0.064 | 0.061–0.068 | 0.00 | 0.0050 | 1.7 | 655 | 0/0/0 |
| changeset-cbor-write | 0.046 | 0.044–0.058 | 0.00 | 0.0070 | 1.7 | 655 | 0/0/0 |
| archive-json | 4.215 | 4.064–6.488 | 7.81 | 1.4213 | 3.1 | 160077 | 0/0/0 |
| archive-json-stream | 4.017 | 3.808–4.070 | 0.00 | 0.4836 | 2.1 | 160077 | 0/0/0 |
| archive-cbor | 49.666 | 44.833–61.778 | 62.50 | 17.5596 | 9.0 | 144594 | 2/0/0 |
| archive-cbor-stream | 52.979 | 44.615–57.497 | 62.50 | 16.9450 | 9.6 | 144594 | 2/0/0 |

## pruned-4

EVSEs: 16; graph nodes: 39; commits: 42; receipts: 4; catalog IDs: 23.

| Operation | Median ms | Min–max ms | Median CPU ms | Median allocation MiB | Largest sampled managed peak MiB | Output bytes | Max Gen0/1/2 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| snapshot-cbor | 6.932 | 5.900–8.484 | 15.62 | 2.7093 | 4.3 | 10782 | 0/0/0 |
| snapshot-json-document | 6.059 | 5.897–6.525 | 0.00 | 1.8567 | 3.6 | 15869 | 0/0/0 |
| snapshot-reading-paths | 0.205 | 0.179–0.217 | 0.00 | 0.0348 | 1.9 | 0 | 0/0/0 |
| snapshot-json-canonical | 0.383 | 0.373–0.475 | 0.00 | 0.1582 | 2.0 | 15869 | 0/0/0 |
| snapshot-cbor-tree | 0.536 | 0.492–0.745 | 0.00 | 0.3076 | 2.3 | 13878 | 0/0/0 |
| snapshot-etag-tree | 0.459 | 0.376–0.511 | 0.00 | 0.1668 | 2.1 | 10782 | 0/0/0 |
| snapshot-cbor-write | 0.499 | 0.482–0.641 | 0.00 | 0.1848 | 2.1 | 10782 | 0/0/0 |
| changeset-cbor | 0.339 | 0.315–0.391 | 0.00 | 0.0341 | 1.6 | 655 | 0/0/0 |
| changeset-json | 0.097 | 0.089–0.117 | 0.00 | 0.0019 | 1.5 | 918 | 0/0/0 |
| changeset-validate-paths | 0.028 | 0.027–0.031 | 0.00 | 0.0027 | 1.5 | 0 | 0/0/0 |
| changeset-cbor-tree | 0.139 | 0.129–0.148 | 0.00 | 0.0171 | 1.6 | 799 | 0/0/0 |
| changeset-etag-tree | 0.066 | 0.056–0.083 | 0.00 | 0.0050 | 1.6 | 655 | 0/0/0 |
| changeset-cbor-write | 0.046 | 0.032–0.068 | 0.00 | 0.0070 | 1.5 | 655 | 0/0/0 |
| archive-json | 2.917 | 2.878–3.234 | 0.00 | 0.9400 | 2.5 | 106105 | 0/0/0 |
| archive-json-stream | 2.610 | 2.417–2.751 | 0.00 | 0.4044 | 1.9 | 106105 | 0/0/0 |
| archive-cbor | 41.038 | 38.270–42.750 | 46.88 | 15.6842 | 9.5 | 105525 | 1/0/0 |
| archive-cbor-stream | 46.660 | 38.111–48.320 | 46.88 | 15.3897 | 9.4 | 105525 | 1/0/0 |
