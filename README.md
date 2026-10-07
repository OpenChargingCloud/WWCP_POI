# WWCP Point-of-Interest

WWCP POI models charging infrastructure and its current status as a versioned dataset.
It is a spin-off from **WWCP-Core** and a .NET 10 library for applications that import,
maintain, exchange and synchronize charging-infrastructure data.

The starting point is a `RoamingNetwork`. Its operators own charging pools, stations,
EVSEs, connectors and tariffs. E-mobility providers belong to the same network.

Instead of repeatedly transferring a complete dataset for every update, an application
can describe changes as an ordered **ChangeSet**. Applying that batch returns a new network
version. Each ChangeSet carries canonical JSON and CBOR SHA-256 identifiers of both its
expected source and expected result. The receiver checks both before accepting the new
version. The previous version remains available, and unchanged immutable data is shared.

## Documentation

| Document | Contents |
| --- | --- |
| [Architecture and implementation](docs/ARCHITECTURE.md) | Data model, immutable storage, copy-on-write, validation, concurrency and source organization |
| [Using ChangeSets](docs/CHANGESETS.md) | Before/after ETags, operations, preconditions, revisions, errors and practical examples |
| [ETags and CBOR](docs/ETAGS-CBOR.md) | Immutability audit, canonical content identifiers, metrological CBOR and roundtrips |
| [JSON contracts](docs/JSON.md) | Network snapshots, ChangeSet JSON, reference resolution, precision and explicit units |
| [Greenfield model](docs/GREENFIELD.md) | Removed format variants, typed quantities and remaining dependency boundaries |
| [Grid connections](docs/GRIDCONNECTIONS.md) | Pool energy meters, grid connection points, operators, electrical ratings and location identifiers |
| [Signatures and trust](docs/SIGNATURES.md) | Signing/verification, equal peer signatures, commit descriptions/metadata and trust policy |
| [Domain-specific ChangeSet details](WWCP_POI/ChangeSets/README.md) | Tariffs, transparency software, energy meters and storage APIs |
| [Tests](WWCP_POI_Tests/README.md) | NUnit coverage, wire conventions and test commands |

## Charging-infrastructure hierarchy

```mermaid
flowchart TD
    RN[RoamingNetwork] --> CSO[ChargingStationOperator]
    RN --> EMP[EMobilityProvider]
    CSO --> Pool[ChargingPool]
    Pool -->|0..n| Meter[EnergyMeter]
    Pool -->|0..1| GCP[GridConnectionPoint]
    GCP -->|1| GO[GridOperator]
    GCP -->|0..1| Meter
    Pool --> Station[ChargingStation]
    Station --> EVSE
    Station -->|0..n| Meter[EnergyMeter]
    EVSE --> Connector[ChargingConnector]
    EVSE -->|0..1| Meter[EnergyMeter]
    Meter -->|0..n| SoftwareStatus[TransparencySoftwareStatus]
    SoftwareStatus --> Software[TransparencySoftware]
    CSO --> Tariff[ChargingTariff]
    EVSE -. tariffIds .-> Tariff
    Connector -. tariffIds .-> Tariff
```

An EVSE represents the individually addressable charging unit. A connector describes a
socket outlet or cable connection belonging to that EVSE. Connector IDs are local to their EVSE.

Addresses, coordinates, opening hours, brands, licenses, electrical values, energy mixes and
energy meters provide additional POI data. Pools and stations can directly own multiple energy meters,
for example with `role: "grid"` at its electricity uplink and `role: "pv"` at a photovoltaic
connection. An EVSE can additionally own one energy meter. Transparency software and certificate
status are nested under each meter. These nested values are stored as properties of their owners.

A pool can additionally have one grid connection point. It always references a grid operator
and can own one meter, independently of the pool's direct meters. Optional connection data
includes voltage level, agreed import/export powers and market/metering-location identifiers.

## What is implemented

- JSON and CBOR parsing/serialization of the nested network hierarchy, providers, tariffs and support values.
- Typed immutable ETags over canonical JSON and deterministic CBOR SHA-256 POI content.
- Current operational/admin statuses, timestamps and custom data in network snapshots.
- Sealed infrastructure, support and group types with immutable POI data and mutable runtime statuses.
- Immutable entity storage with persistent dictionaries and sets.
- Atomic, ordered `Add`, `Remove` and `UpdateProperty` ChangeSet operations.
- Mandatory before/after JSON and CBOR ETag checks, revision checks and optional expected-old-value checks.
- ChangeSet preparation that computes both expected states before exchange or signing.
- `TryMerge` previews compatible batches from a common source and prepares their merge only on explicit request.
- Validation of entity IDs, ownership, editable fields and tariff references.
- Lazy domain hierarchies with independent runtime schedules for each network version.
- Canonical ChangeSet signing/verification with Styx, equal peer signatures and a verifier per signature.
- Immutable multilingual commit descriptions and arbitrary JSON metadata, covered by every signature.
- NUnit coverage for JSON roundtrips, conflicts, storage sharing and schema validation.

The “git for charging data” idea now includes cryptographic POI content identities and
ChangeSets binding source and result states, signed descriptions/metadata and multiple peer
signatures. Durable history, automatic conflict resolution and a network synchronization protocol
are future work. Numeric revisions provide optimistic concurrency control; the ETags distinguish
different static data at the same revision. They describe POI content, while a commit identity
covering history, operations and signatures still needs a separate profile.

## Quick start

The following example imports one EVSE, changes its maximum power and retains the original version:

```csharp
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;

var network = RoamingNetwork.Parse("""
    {
      "@id": "network-a",
      "name": { "en": "Example network" },
      "chargingStationOperators": [
        {
          "@id": "DE*ABC",
          "name": { "en": "Example operator" },
          "chargingPools": [
            {
              "@id": "DE*ABC*P1",
              "chargingStations": [
                {
                  "@id": "DE*ABC*S1",
                  "EVSEs": [
                    {
                      "@id": "DE*ABC*E1",
                      "currentType": [ "DC" ],
                      "maxPower": "100 kW",
                      "socketOutlets": [ { "@id": "1", "type": "CCS" } ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    }
    """);

var original = network.DataSnapshot;
var oldPower = original.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1")
                       .Properties["maxPower"];

var changeSet = original.CreateChangeSet(
    id: "change-42",
    createdAt: DateTimeOffset.UtcNow,
    changes: [
        RoamingNetworkChange.UpdateProperty(
            entityType: "EVSE",
            entityId: "DE*ABC*E1",
            propertyName: "maxPower",
            oldValue: oldPower,
            newValue: JsonSerializer.SerializeToElement("150 kW"))
    ]);

var next = network.ApplyChangeSet(changeSet);

// Both sides of the transition are checked automatically by ApplyChangeSet.
Console.WriteLine(changeSet.BeforeETags[0]); // json:sha256:hex:...
Console.WriteLine(changeSet.AfterETags[0]);  // json:sha256:hex:...

Console.WriteLine(network.Revision); // 0
Console.WriteLine(next.Revision);    // 1

var json = next.ToJSONSnapshot().ToString();
var restored = RoamingNetwork.Parse(json);
```

Power values use explicit SI units. Use `ToJSONSnapshot()` for versioned persistence with
current statuses. Static POI changes use ChangeSets; constructors and parsers create the initial
immutable hierarchy. Public static setters and in-place Add/Remove/UpdateWith factory APIs have
been removed, which is a breaking API change.

`Status`, `AdminStatus`, their histories, real-time measurements and forecasts still live inside
the entities and remain mutable. Updating them directly does not change the POI revision or its
creation/change timestamps. A derived network receives independent runtime schedules captured
when `ApplyChangeSet()` runs. `DataSnapshot` remains a frozen baseline, including its captured
status values; `ToJSONSnapshot()` overlays the current runtime statuses on the static version.

`ImmutableI18NString` and `ImmutableOpeningTimes` are local immutable replacements for mutable
Illias values. Collections are detached on input; mutable nested dependency values are copied
on input and access. Energy meters expose immutable static data and their own mutable runtime
statuses. Dependency `IEntity` text access returns detached copies, and `IInternalData` mutation
of static data is rejected. `InternalData` is mutable application runtime context.

`ChargingCable`, `EnergyMeter`, `GridOperator`, `ParkingOperator`, `TransparencySoftware` and its
certificate status follow the same static-data boundary. The local `ChargingStationManufacturer`
fork stores immutable multilingual values and `ImmutableCryptoKeyInfo` documents. Parking children
are immutable as well. EVSE, station, pool and tariff groups freeze membership; `WithMembers`,
`WithMember` and `WithoutMember` return new groups. `ChargingPoolGroup` now groups actual pools.
The ChangeSet graph still has the eight node types shown above; support values and standalone
groups do not gain separate ChangeSet operations through this API conversion.

## Merge concurrent ChangeSets

Two batches prepared against the same snapshot can be checked together. Both must match its
network, revision and `BeforeETags`; their individual `AfterETags` and any signatures are checked
as well. `TryMerge` defaults to a preview: success returns a notice and no merged batch.

```csharp
// left and right were both prepared against original.
if (original.TryMerge(left, right, out _, out var preview))
    Console.WriteLine(preview.Message); // Compatible; explicit merge preparation is required.

// Call this after the application/user explicitly chooses to merge.
if (original.TryMerge(left, right, out var merged, out var report,
                      merge: true, mergedChangeSetId: "merge-43"))
{
    var combined = network.ApplyChangeSet(merged!);
    // combined.Revision == original.Revision + 1
    // merged.BeforeETags identify original; merged.AfterETags identify combined.
}
else
{
    foreach (var issue in report.Issues)
        Console.WriteLine(issue.Message);
}
```

The check executes both batch orders on unpublished immutable maps, preserving the operations
and their old-value checks. Both orders must be valid and yield the same complete stored data,
including frozen runtime status values. Different properties of one entity can merge;
competing replacements, subtree deletion versus modification and failed reference/old-value
checks are reported. Nested arrays/objects are whole-property replacements. The check is
conservative: it does not rewrite preconditions, deduplicate operations or choose a winning value.

The merged batch contains the left operations followed by the right operations. Its timestamp
defaults to the later input timestamp, or uses an explicitly supplied `createdAt`. That single
timestamp drives changed ancestors and omitted metadata defaults. Original signatures are
verified via `verifySignature` and are not copied; sign the new batch separately if required.
`network.TryMerge(...)` delegates to its frozen `DataSnapshot`. Applying to the common source
produces one successor. Applications coordinate publication of their current head and retain
branch successors as needed. See [merge details](docs/CHANGESETS.md#merging-concurrent-batches).

## Sign ChangeSets with descriptions and metadata

```csharp
using org.GraphDefined.Vanaheimr.Illias;

var signed = changeSet
    .WithDescription("de", "Maximalleistung angepasst")
    .WithDescription("en", "Updated maximum power")
    .WithMetadata("acme:ticketId", "INC-4711")
    .WithMetadata("acme:approval", new { department = "operations", approved = true })
    .Sign(alicePrivateKey, "acme:alice", COSEAlgorithm.Ed25519)
    .Sign(bobPrivateKey,   "acme:bob",   COSEAlgorithm.Ed25519);

// trustedKeys maps authorized application key IDs to their public keys.
var next = network.ApplyChangeSet(signed, VerifySignature: (batch, signature) =>
    trustedKeys.TryGetValue(signature.KeyId, out var key) &&
    batch.VerifySignature(signature, key, signature.KeyId, out _));
```

`Signatures` is an immutable array of equal peers. `Sign` appends; `TrySign` returns an error
instead of throwing. `VerifySignature` checks one peer and `VerifySignatures` checks all peers
with an application key resolver. Signing accepts Bouncy Castle keys or Styx `COSEKey` objects.
Every supplied signature must pass the Apply/Merge callback. Applications resolve trusted keys
and enforce their signer policy; unsigned batches remain accepted by the core applier.

The versioned `wwcp-poi-changeset-json-v1` profile signs the header, operations, both ETag arrays,
descriptions and metadata using Styx canonical JSON. It also binds each peer's profile, algorithm,
key ID and Base64 encoding. Signatures themselves are excluded so further peers can independently
sign the same content. Descriptions and metadata do not change POI state ETags. Editing either
returns an unsigned copy; existing signatures cannot be retained over changed content.
See [the complete signing profile](docs/SIGNATURES.md).

## Build and tests

Install the .NET 10 SDK. The current project references sibling source repositories:

```text
parent/
  WWCP_POI/
    WWCP_POI/WWCP_POI.csproj
    WWCP_POI_Tests/WWCP_POI_Tests.csproj
  WWCP_Core/
    WWCP_CoreData/WWCP_CoreData.csproj
  Hermod/
    Hermod/Hermod.csproj
  Styx/
    Styx/Styx.csproj
```

Their contents must be compatible with the project references; this checkout is not a
standalone NuGet-only build. Timestamp restoration uses the local immutable metadata base;
canonical JSON, deterministic CBOR and metrological tag support use Styx. The retained
[dependency patch](patches/README.md) documents the earlier external timestamp helper.

Run from this repository's root:

```powershell
dotnet build WWCP_POI/WWCP_POI.csproj
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj
```

The library targets `net10.0`, with nullable reference types and implicit usings enabled.
POI documents use Newtonsoft.Json; immutable storage and ChangeSet payloads use System.Text.Json.
The tests use NUnit.

## License

The source files carry the GNU Affero General Public License, version 3.0 notice.

## POI content identifiers and CBOR

`network.ETags` is an `ImmutableArray<ETag>`, containing JSON then CBOR identifiers. Each
`readonly struct ETag` holds typed `Format`, `Algorithm` and immutable `Digest` bytes.
`ToString()` displays `json:sha256:hex:<hex>` or `cbor:sha256:hex:<hex>`. The digests use
Styx canonical JSON and deterministic CBOR with metrological extensions. Both describe the full
static POI hierarchy, including static timestamps and owned children. Dynamic statuses, measurements,
revision metadata and derived ETags are excluded. Servers using the same profile can compare
the corresponding identifiers to check content agreement.

JSON writes each ETag as `["json", "sha256", "hex", "<64 lowercase hex digits>"]` or the corresponding
`"cbor"` tuple. CBOR writes `["json", "sha256", h'<32 digest bytes>']`, using a native byte string.
System.Text.Json and Newtonsoft.Json use the same structured JSON contract. Packed ETag strings
are a display form; wire parsers require the tuple arrays.
The explicit textual encoding supports `hex` (the default) and standard padded `base64`.
`tag.ToJSON(ETagDigestEncoding.Base64)` or
`network.ToJSONWithETags(DigestEncoding: ETagDigestEncoding.Base64)` selects Base64 output.
Both decode to the same immutable digest bytes and compare equally. Encoding is a transport
choice, so neither ETag equality nor the canonical state hashes change. Native CBOR stays binary.

```csharp
var bytes = network.ToCBOR();
var restored = RoamingNetwork.ParseCBOR(bytes);

var batch = network.CreateChangeSet("change-43", DateTimeOffset.UtcNow, [/* operations */]);
// Transfer JsonSerializer.Serialize(batch) to a replica of this source version.
var next = replica.ApplyChangeSet(batch);
```

`BeforeETags` and `AfterETags` are required `ImmutableArray<ETag>` values containing both identifiers.
The applier checks the target, revision and source ETags, verifies every peer signature, applies all
operations locally, updates timestamps and then checks the result ETags. Any mismatch rejects
the whole batch and leaves the source unchanged. `CreateChangeSet` validates and computes the
result on immutable maps without publishing it. `Sign` appends a cryptographic peer signature
after preparation and commit-metadata edits; `WithSignature` appends an external envelope.
The canonical signing profile binds both ETag arrays, operations, descriptions and metadata.

**Breaking contract change:** constructing or deserializing a ChangeSet requires both typed arrays
and their structured wire entries. Default/uninitialized ETags are rejected.
Use `CreateChangeSet` for local preparation or pass trusted expected identifiers to its constructor.
Direct runtime updates retain POI ETags; status ChangeSets can change them through managed
`lastChange` timestamps. Empty batches advance revision and touch the root; unchanged content
can still have the same ETags when the timestamp is also unchanged.

Hash verification traverses the complete canonical POI projection. Snapshot identifiers are
computed lazily and cached; the immutable storage update continues to share unchanged entries.
See [ETags and CBOR](docs/ETAGS-CBOR.md) for the complete profile and
[ChangeSets](docs/CHANGESETS.md) for preparation, validation order and errors.
