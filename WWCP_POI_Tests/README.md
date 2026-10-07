# Tests and interoperability evidence

[Repository overview](../README.md) · [Architecture](../docs/ARCHITECTURE.md) · [Profile and references](../docs/INTEROPERABILITY.md)

Run the actual POI assembly with its local WWCP_CoreData, Hermod and Styx dependencies:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore
```

The full run on 2026-10-07 passed **509 tests**, with one skipped process worker. The worker
runs in child processes during the crash-recovery tests. `GenerateReferenceVectors` is explicit
and excluded from ordinary discovery; it never refreshes expected files during an ordinary run.
The new replication/adoption fixtures contain **61 passing cases** (34 exchange, 17 adoption,
10 persistence). Run just that package with:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~ReplicationTests|FullyQualifiedName~HeadAdoptionTests|FullyQualifiedName~ReplicationPersistenceTests'
```

## Fixtures

| Fixtures | Coverage |
| --- | --- |
| `Json/*Tests` | Domain/presentation JSON, nested parsers/resolvers, parent identities, malformed input, optional/zero/false values, cultures, tariff precision and transparency software |
| `ImmutableEntitiesTests` | Original eight sealed infrastructure APIs, detached dependencies, immutable text/opening hours, rejected metadata mutation, runtime independence |
| `CopyOnWriteChangeSetTests` | Sharing, ordered operations, atomic rollback, revision/old-value checks, connector scopes, signed verification hooks, static streaming and reload |
| `RoamingNetworkChangeSetTests` | Operation shapes, detached payloads and required headers |
| `InteroperabilityTests` | Fixed bytes under multiple cultures, static/profile/ETag tuples, signed two-peer CBOR/JSON, exact signed numeric/SI spelling, optional presence, both before/after hashes, ancestry tampering, malformed input and runtime exclusion |
| `HistoryWorkflowTests` | Independent replicas, published branches, explicit merges/resolutions/audit metadata, archive bootstrap/replay/continuation, duplicate/peer delivery, trust, missing parents, conflicting batch IDs and concurrent head publication |
| `PersistenceTests` | Writer leases, persistence failures before replacement, continuation after recovery and abrupt child-process exit immediately before/after replacement |
| `ReplicationTests` | JSON/CBOR pagination, complete DAG frontiers, exact count/UTF-8/CBOR byte bounds, indivisible oversized commits, checkpoint mismatch, missing/order checks, full-page state/peer/batch-ID rollback, duplicate delivery and changed trust |
| `HeadAdoptionTests` | Signed second-parent merge adoption, preview/idempotence/ancestry checks, independent runtime schedules/measurements/forecasts, recreated/new meter and EVSE lifetimes, connection-point children, revision decreases, revoked merge parents, reentrancy, competing heads and gated runtime delivery |
| `ReplicationPersistenceTests` | One archive write per page, retention/head installation after replacement, injected failures before writing/after flushing, unchanged memory/disk/runtime, retry, original signed head recovery and continued publication |
| `BootstrapTests` | 32 cases: frozen bounded JSON/CBOR transfer, verified receipts/restart/duplicates, exact raw/wire/count limits, disk/transport corruption, write failures and leases, complete digest/profile/checkpoint/head/identity/signature/replay checks, revoked unpublished branches, explicit activation, existing archives, reentry, fresh runtime and incremental continuation |
| `StructuralMergeTests` | 34 cases: owner/descendant deletion in both branch orders, whole-subtree choices with signed recovery, meter/graph recreation and additions, all missing references and typed targets, active/admission groups, independent grid slots, owner/parking scope, connector IDs scoped to EVSE, criss-cross ancestor choices, invalid resolutions, resolver reentry/exceptions and referenced replacement with an explicit temporary reference plan (revised expectation not rerun) |

The 1,000-EVSE sharing fixture counts replaced immutable entries; it is not a capacity/performance
benchmark. The crash tests establish behavior for process interruption at two known stages;
they do not simulate power loss, filesystem damage or every possible I/O failure.
The bootstrap fixture injects receipt write failures and models interruption by dispose/reopen;
it does not simulate abrupt process exits or power loss during bootstrap writes. Run its 32 cases:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~BootstrapTests'
```

## Presentation views and complete transports

Ordinary entity `ToJSON` views can omit static timestamps and content according to their expansion
options. Their view roundtrip checks compare presentation fields using `JsonViews`, excluding only
schema-owned derived ETags/profile declarations; arbitrary customer values remain content. Input
nonmutation comparisons remain exact. Complete snapshot/transport tests separately compare exact
canonical bytes and validate the original state ETags.

`DataSnapshot.WriteTo` is static and includes revision/profile metadata. Its streaming comparison
uses `DataSnapshot.ToJSON`, rather than a domain snapshot overlaid with current runtime statuses.
Text quantity imports require SI units, and the exact decimal parser accepts strings; a `JObject`
that was already parsed into Double cannot recover lost decimal digits.

## Wire conventions

- Quantities use invariant SI strings such as `"250 kW"`, `"20 kWh"`, `"400 V"`, `"50 Hz"`,
  `"5.25 m"`, `"1250.5 µΩ"` and `"300 s"`. Numeric/unitless readings are rejected.
- Coordinate latitude/longitude text uses invariant decimals; altitude requires an SI length.
- Schema timestamps normalize to UTC with tick precision; customer text is preserved.
- Charging modes/current types are flat arrays; transparency-software licenses are objects.
- IDs identify graph/collection elements; connectors require their EVSE scope. Validated inherited
  EVSE station views do not become independently editable snapshot properties.
- Missing JSON values remain distinct from defined null. `RemoveProperty`/`RemoveElementProperty`
  express absence; null remains a supplied value/precondition.
- Tagged POI transports declare `contentProfile: "wwcp-poi-static-v1"`; complete tagged parsers
  reject missing/unsupported profiles. Commit/archive headers require `ContentProfile`.
- ETag JSON uses `[format, algorithm, encoding, encodedDigest]`; HEX/Base64 identify equal bytes.
  CBOR uses `[format, algorithm, bytes]`. Commit digests remain labelled `json` in CBOR.
- Peer signatures are equal array entries. Commit identity excludes both peer arrays and binds
  the content profile, ancestry and complete unsigned batch.

## Fixed reference files

[docs/interoperability](../docs/interoperability/README.md) publishes a fixed input, exact canonical
state bytes, snapshot/ChangeSet/commit/merge/archive JSON and CBOR, identity/signing preimages,
expected hashes, public fixture keys and two-peer signatures. Ordinary assertions compare these
committed expectations and verify signatures after transport/recovery. Fixtures include W, VA,
V and Hz; multilingual text, custom decimals, exact signed exponent/negative-zero spelling,
explicit null and a custom conflict-resolution audit.

Deliberate regeneration:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~GenerateReferenceVectors'
```

Review every changed existing digest/byte/signature as a contract change. The next ordinary
build copies reference artifacts to the test output. JSON artifacts have fixed LF checkout
policy and CBOR artifacts are marked binary in `.gitattributes`.

## Remaining coverage

Full [snapshot commits](../docs/SNAPSHOTS.md) are implemented and the library builds. No dedicated
snapshot tests have been added or run. Versioned JSON/CBOR/signature roundtrips and reference vectors,
revision/last-batch bookkeeping, unchanged static timestamps/ETags, runtime and lifetime continuity,
head races, atomic failure/recovery, bounded transfer and merges across snapshots remain to verify.
The existing executed evidence and version 1 vectors predate the snapshot implementation.

[Authorized snapshot boundaries](../docs/SNAPSHOT-BOUNDARIES.md) now have a library build, but no
dedicated added/executed tests. Pending cases include mandatory fresh trust and rollback decisions,
JSON/CBOR profiles/reference vectors, bounded partial bootstrap/restart/activation, large-anchor
incremental limits, atomic failures/recovery, excluded ancestry acknowledgments, unknown roots,
cross-boundary merge dependencies and retained-suffix runtime continuity. Existing executed
complete-history evidence does not verify these new paths.

[Explicit retention](../docs/RETENTION.md) now has a successful library build, without added or
executed retention tests. Required cases include protected branches/bases and merge dependencies,
released unpublished tips, stale head/inventory/peer additions, approval reentry/rejection, cold archive
mismatch/leases, failure and retry before active replacement, repeated pruning/cold reads, catalog
JSON/CBOR/bootstrap recovery, archived/unknown replication responses and exact live head/runtime identity.
Snapshot/boundary/retention profiles still await frozen independent reference vectors.

Operation-history lifetime handling and explicit temporary reference scheduling are implemented;
their dedicated broader coverage and a fresh test run remain planned. Exhaustive groups/parking/
reference/ownership combinations, recursive virtual merge bases, power-loss simulation, broader size limits
and performance remain additional work. There is no independent remote implementation under test.
The original archive-bootstrap workflow remains covered alongside bounded incremental
exchange/adoption and [resumable bootstrap](../docs/BOOTSTRAP.md) workflows. Failure assertions compare the original head/network references,
full JSON/CBOR history and runtime views; persistence cases also compare disk bytes and temporary
file cleanup. Runtime identity checks cover first-parent replay and second-parent branch proofs,
including equal newly added meter IDs and reused identities. Broader owner/reference combinations,
recursive merge bases, independent implementations and performance remain in the
[roadmap](../docs/ROADMAP.md). See [replication evidence](../docs/REPLICATION.md#implementation-and-evidence).

The [structural merge package](../docs/MERGING.md#structural-merge-evidence) had 34 passing cases in the
last full run. That run predates the new lifetime rules and revised temporary-reference expectation:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj --no-restore --filter 'FullyQualifiedName~StructuralMergeTests'
```

Preview, rejected preparation and resolver exceptions compare the unchanged complete history,
original head/network references and runtime. Reference resolution tests assert exact typed
targets, absence preservation, dropping stale failures and bounded resolver retries. Signed
resolution recovery and explicit criss-cross ancestor choices preserve the existing byte profiles.
