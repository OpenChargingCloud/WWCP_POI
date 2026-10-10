# Snapshot and retention verification

This executed evidence predates the static-v2 shared-catalog/parking model change. Its static-v1
profile, counts and digests describe that historical run. Current reference artifacts have been
regenerated for static-v2 and checked again in the ordinary suite.
See [current executed model verification](VERIFICATION-DOMAIN-MODEL.md),
[current profile](INTEROPERABILITY.md) and [domain model](DOMAIN-MODEL.md).

[Repository overview](../README.md) · [Snapshots](SNAPSHOTS.md) · [Boundaries](SNAPSHOT-BOUNDARIES.md) · [Retention](RETENTION.md) · [References](interoperability/README.md)

On **2026-10-08**, the complete NUnit run passed **585 tests**, with zero failures and one skipped
process worker. The worker executes separately in the existing archive crash tests. Both reference
generators are explicit and excluded from ordinary runs. This package adds **76 passing ordinary
cases**; the complete run also reruns the existing lifetime, temporary-reference and merge fixtures.

## Added evidence

| Fixture | Ordinary cases | Assertions |
| --- | ---: | --- |
| [SnapshotHistoryTests](../WWCP_POI_Tests/Interoperability/SnapshotHistoryTests.cs) | 19 | Shared immutable maps/indexes; revision and last-batch rules; unchanged static bytes/ETags; detached metadata and UTC offsets; native CBOR; exact metadata number spelling; peer signature verification and identity independence; rejected tampering; independent runtime schedules/measurements/forecasts; complete signed branch recovery; expected-head races; write failure/retry and overflow |
| [SnapshotBoundaryTests](../WWCP_POI_Tests/Interoperability/SnapshotBoundaryTests.cs) | 14 | Original checkpoint/root/parent identities; fresh runtime; mandatory current signature, chain and rollback policies; recovery rejection; revoked static access with continuing local runtime; bounded pages despite a large root; empty incoming root peers retaining accepted local peers; bootstrap staging/reopen/preview/activation; old merge parent rejection and later covering snapshot; retained suffix merges and unavailable bases |
| [RetentionTests](../WWCP_POI_Tests/Interoperability/RetentionTests.cs) | 38 | Review/default preview without writes; protected old/unpublished branches and bases; crossing merge parents; invalid cutoffs/releases; explicit cold-only work; exact live head/network/runtime; stale heads/branches/peer-only envelopes; approval rejection/exception/reentry; active write failure/retry; cold mismatch/leases/destination guards; repeated receipt/catalog recovery and cold retrieval; v4 bootstrap; archived versus unknown IDs; reimported suffix branches; rejected digest/signature/receipt tampering; missing cold import dependencies; exclusions without invented receipts; competing pruning and gated runtime delivery |
| [SnapshotRetentionReferenceTests](../WWCP_POI_Tests/Interoperability/SnapshotRetentionReferenceTests.cs) | 5 | Fixed identity/signature/JSON/CBOR/archive/page/plan/receipt/manifest bytes under `en-US`, `de-DE` and `ar-EG`; original history recovery from artifacts; SHA-256 calculated directly from published preimages; Ed25519 verification directly with BouncyCastle and the public fixture keys |

The first baseline run found one outdated second-parent adoption expectation. Recreating an EVSE
under the same ID while another branch edits its former lifetime requires `ReplaceModify` resolution.
That existing test now asserts the conflict and unchanged history, explicitly selects the recreated
right subtree, signs the resulting batch/commit, and still verifies EVSE runtime reset plus preserved
unrelated meter runtime. The test now follows the operation lifetime conflict semantics.

## Commands and results

The package was compiled/run against the current built library and local dependency assemblies:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --verbosity quiet -consoleLoggerParameters:ErrorsOnly --logger 'console;verbosity=minimal' --logger 'trx;LogFileName=full-suite.trx' --results-directory WWCP_POI_Tests/bin/TestResults
```

To rerun only the 76 added ordinary cases:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore -p:BuildProjectReferences=false --filter 'FullyQualifiedName~SnapshotHistoryTests|FullyQualifiedName~SnapshotBoundaryTests|FullyQualifiedName~RetentionTests|FullyQualifiedName~SnapshotRetentionReferenceTests'
```

Local TRX results are under `WWCP_POI_Tests/bin/TestResults` and are ignored generated outputs.
Ordinary tests never refresh expectations. The reviewed reference generator was run deliberately
once while adding the new files; the subsequent ordinary/full runs compared them without writing.

## Fixed regression references

[snapshot-retention.vectors.json](interoperability/snapshot-retention.vectors.json) contains 31 named
fields, including identity/signing preimages, both Ed25519 peer signatures and complete wire documents.
Fifteen companion JSON/CBOR artifacts cover the full snapshot, complete and boundary histories,
snapshot-rooted page, review plan, receipt, pruned history and v4 bootstrap manifest. Review plans
have a JSON artifact; their executable local objects are not decoded from a wire document.

Files use the existing public reproducibility keys and fixed input hierarchy/timestamps. Cold paths
are deliberately absent from identities. Every ordinary test independently creates a new temporary
archive, then compares exact expected bytes and hashes. Original version 1 references remain unchanged.
These are fixed **local regression references** produced by this implementation. They do not establish
interoperability with another POI implementation; root signatures also do not authenticate unsigned
receipt bookkeeping or omitted checkpoint association.

## Practical limits and next work

The tests inject exceptions before active writing and after its temporary-file flush, exercise clean
reopen/retry, and assert exact disk, history, head/network and runtime invariants. The initial verification
package reused ordinary archive crash tests. The subsequent [process-crash package](CRASH-RECOVERY.md)
now adds actual signed snapshot/cold/pruning exits at named write points, using the same child worker.
Power loss and arbitrary filesystem behavior remain outside this evidence.

The scenarios use representative small DAGs and a deliberately large snapshot description. They do
not exhaust every graph/ownership combination or cryptographic algorithm, benchmark large two-year
histories, or validate an independent network peer. Broader lifetime/reference scheduling cases,
streaming archive replay and compressed catalog indexes remain additional work.

The subsequent [archive-limit package](ARCHIVE-LIMITS.md) now checks local bytes, retained commits,
receipts and aggregate catalog IDs before document materialization/replay, with structured rejection
and unchanged active state. Its 60 new cases brought the suite to 645 passing tests. The subsequent
26 snapshot/retention process-crash/exception cases brought the suite to 671. The later
[bootstrap crash/recovery package](BOOTSTRAP-CRASH-RECOVERY.md) adds 89 cases, including 36 actual exits,
and brought the suite to 760. [Cold archive discovery](COLD-ARCHIVES.md) added 98 cases and brought the
suite to 858. [Explicit archive maintenance](ARCHIVE-MAINTENANCE.md) adds 94 cases, including five
further real process exits, and brings the latest full run to 952 passing tests, zero failures and
one parent-skipped child worker.
The 585-test run above records this verification package's original baseline.
Complete serialization and retained states still scale with history;
uninstrumented write/rename interruption, streaming replay and compact catalogs remain additional work.
