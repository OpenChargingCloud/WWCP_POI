# JSON tests

[Repository overview](../README.md) · [Architecture and validation](../docs/ARCHITECTURE.md) · [JSON contracts](../docs/JSON.md)

Run the NUnit test project from the repository root:

```powershell
dotnet test WWCP_POI_Tests/WWCP_POI_Tests.csproj
```

The tests build the actual POI assembly and its local WWCP_CoreData, Hermod and Styx dependencies.

Coverage includes cable/connector payloads, images, additional coordinates, authentication variants,
brands with ID-only and expanded licenses, charging products, and change sets. Assertions compare
individual fields and JSON documents where entity equality only checks identity or selected fields.
Tests exercise embedded representations, omitted optional values, zero and false values, invalid
input, nested parser callbacks and decimal values under different cultures.

## Wire conventions

- Metrological values require invariant unit-bearing strings, e.g. `"250 kW"`, `"20 kWh"`,
  `"5.25 m"` and `"1250.5 µΩ"`. Numbers and unitless strings are rejected.
- Additional coordinates retain their string latitude/longitude fields with invariant decimal
  separators. Optional altitude requires an SI string such as `"25.125 m"`; coordinates must be finite and within WGS84 bounds.
- Product and tariff durations use explicit SI seconds, e.g. `"300 s"`, retaining tick precision.
- Product energy uses `stopChargingAfterEnergy`; tariff restrictions use `minEnergy`/`maxEnergy`.
  C# tariff bounds and physical billing increments use typed quantities, without implicit scales.
- Energy-mix shares require `percentage` strings such as `"25 %"` and explicit composition arrays.
- Authentication modes use `number`/`stationCode` for SMS and phone details.
- Brand license strings represent identity only. Expanded license documents also preserve metadata.
- Change sets use System.Text.Json. Omitted `OldValue` means no precondition; a present JSON null
  means the expected previous value is null. Omitted `NewValue` is absent; a present JSON null
  explicitly clears a property. Absent values are now omitted when writing change sets.
  Operations validate their required values on construction and own copies of their JSON payloads.
- ChangeSet headers require `BeforeETags` and `AfterETags`, each containing JSON then CBOR
  SHA-256 identifiers as `ImmutableArray<ETag>`. Each JSON identifier uses
  `[format, algorithm, encoding, encodedDigest]`, with converters for both serializers.
  JSON readers accept explicit `hex` or canonical padded `base64` and compare decoded bytes.
  CBOR ETags use a native 32-byte digest in the third position. Applicable batches in existing fixtures use `CreateChangeSet` to compute
  expected states. Deliberately invalid incoming batches may use the explicit constructor.

## Infrastructure snapshots

`RoamingNetwork.Parse(network.ToJSONSnapshot())` reads the nested operator/pool/station/EVSE/connector
hierarchy and e-mobility providers. The snapshot also carries current admin and operational statuses
with their timestamps, creation/change timestamps and custom data at every infrastructure level.
Status history, runtime/internal data and properties absent from the existing POI serializers are
not part of this snapshot contract. Canonical static POI bytes and ETags are available through
`POIRepresentation`. The separate canonical ChangeSet signing profile is documented in
[Signatures](../docs/SIGNATURES.md). Existing signature fixtures cover envelope roundtrips and the
verification hook, and have been adapted to the peer array/callback contract. Dedicated cryptographic
signing, metadata and multiple-signer tests have not been added or run in this implementation step.

ID-only children require an explicit `InfrastructureJsonParsingContext` with resolvers returning
JSON documents. Those documents are copied and parsed into new entities with correct parent links;
mutable source entities are never attached directly. Unresolved references, duplicate IDs and
contradictory parent/operator IDs fail without returning a partial hierarchy. Supplementary flat
ID lists are validated against the reconstructed hierarchy. Flat expanded object lists and expanded
ancestor objects are currently rejected; use a nested snapshot instead.

Tests cover complete network roundtrips, reference resolution at each level, deep error paths,
nonmutation of input and resolver documents, current statuses with historical timestamps, custom
data, license metadata, opening hours and coordinates (including altitude) across cultures.

EVSE electrical values use volts, amperes, watts and watt-hours without rounding; zero is retained.
The `maxVoltage` wire name represents the model's typed `MaxVoltage` property.
`currentType` and charging modes use flat arrays of declared names; nested flag arrays are rejected.

Timestamp restoration uses the local `AImmutableInternalData.RestoreTimestamps` method.
This avoids replacing persisted timestamps with the time of deserialization.

## Copy-on-write and change sets

The applier now checks both source and result ETag arrays around the existing operation engine.
Snapshot hashes are lazily cached; missing nested timestamp defaults use `CreatedAt`.
The fixtures were adapted to the required constructor/preparation API and typed identifiers.
Dedicated ETag mismatch, value/converter, cross-replica and CBOR transition tests have not been
added or executed as part of these changes.

`CopyOnWriteChangeSetTests` exercises immutable storage sharing, atomic multi-operation batches,
old-value/revision conflicts, nested additions and cascading removal, local connector IDs,
timestamped status updates, versioned JSON reloads, signature verification hooks and concurrent
branches/projections. A 1,000-EVSE fixture checks that a single EVSE update replaces exactly five
entries (the EVSE and its four ancestors), rather than copying all entries. Tests also ensure that
editing detached dependency text copies does not edit the authoritative snapshot or another version.

`ImmutableEntitiesTests` covers the eight sealed entity APIs, detached constructor inputs,
immutable multilingual text/opening hours, rejected dependency metadata mutation, direct runtime
status updates without POI revision changes, and isolated runtime histories in derived versions.

See the [ChangeSet guide](../docs/CHANGESETS.md) for the versioned API and operation rules, and
the [domain details](../WWCP_POI/ChangeSets/README.md) for tariffs and transparency software.

`ChargingTariffJsonTests` covers price precision and culture independence, numeric prices,
all restriction fields (including dates and fractional seconds), copied collection inputs, energy
mixes, operator ownership and tariff resolvers. Snapshot and change-set tests cover tariff additions,
price updates with shared infrastructure, assignment/removal ordering, dangling references and
atomic rollback. Text parsers retain decimal precision through versioned JSON reloads.

`TransparencySoftwareJsonTests` covers object license JSON and rejected string licenses, links, custom callbacks,
certificate fields and UTC validity intervals with tick precision. Tests exercise defensive copies,
complete ordering/equality/hash semantics, malformed nested data paths, energy meter metadata and
full network snapshot reloads. Change-set tests replace an EVSE's `energyMeter` value, confirm
sharing of unrelated EVSEs and verify atomic rollback on invalid software/status data.
