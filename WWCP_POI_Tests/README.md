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

- Cable length is measured in metres and resistance in microohms.
- Additional coordinates retain their existing string latitude/longitude fields with invariant decimal
  separators. Optional altitude is a number in metres; coordinates must be finite and within WGS84 bounds.
- Product durations are seconds, power is watts and energy is watt-hours. The legacy
  `stopChargingAfterKWh` field still carries watt-hours, matching the existing serializer.
- Authentication modes retain the existing type names and `Number`/`StationCode` casing.
- Brand license strings represent identity only. Expanded license documents also preserve metadata.
- Change sets use System.Text.Json. Omitted `OldValue` means no precondition; a present JSON null
  means the expected previous value is null. Omitted `NewValue` is absent; a present JSON null
  explicitly clears a property. Absent values are now omitted when writing change sets.
  Operations validate their required values on construction and own copies of their JSON payloads.

## Infrastructure snapshots

`RoamingNetwork.Parse(network.ToJSONSnapshot())` reads the nested operator/pool/station/EVSE/connector
hierarchy and e-mobility providers. The snapshot also carries current admin and operational statuses
with their timestamps, creation/change timestamps and custom data at every infrastructure level.
Status history, runtime/internal data and properties absent from the existing POI serializers are
not part of this snapshot contract. JSON is not yet canonical for signature verification.

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
The legacy `averageVoltage` wire name continues to represent the model's `MaxVoltage` property.
`currentType` remains an array of enum names. Charging modes are now a flat array; the parser also
accepts the previously emitted nested arrays.

Timestamp restoration uses Styx's protected `AInternalData.RestoreTimestamps` method. This additive
dependency change avoids the normal property-change mechanism replacing a persisted timestamp
with the time of deserialization.

## Copy-on-write and change sets

`CopyOnWriteChangeSetTests` exercises immutable storage sharing, atomic multi-operation batches,
old-value/revision conflicts, nested additions and cascading removal, local connector IDs,
timestamped status updates, versioned JSON reloads, signature verification hooks and concurrent
branches/projections. A 1,000-EVSE fixture checks that a single EVSE update replaces exactly five
entries (the EVSE and its four ancestors), rather than copying all entries. Tests also ensure that
editing legacy compatibility objects does not edit the authoritative snapshot or another version.

See the [ChangeSet guide](../docs/CHANGESETS.md) for the versioned API and operation rules, and
the [domain details](../WWCP_POI/ChangeSets/README.md) for tariffs and transparency software.

`ChargingTariffJsonTests` covers price precision and culture independence, legacy price strings,
all restriction fields (including dates and fractional seconds), copied collection inputs, energy
mixes, operator ownership and tariff resolvers. Snapshot and change-set tests cover tariff additions,
price updates with shared infrastructure, assignment/removal ordering, dangling references and
atomic rollback. Text parsers retain decimal precision through versioned JSON reloads.

`TransparencySoftwareJsonTests` covers full and legacy license JSON, links, custom callbacks,
certificate fields and UTC validity intervals with tick precision. Tests exercise defensive copies,
complete ordering/equality/hash semantics, malformed nested data paths, energy meter metadata and
full network snapshot reloads. Change-set tests replace an EVSE's `energyMeter` value, confirm
sharing of unrelated EVSEs and verify atomic rollback on invalid software/status data.
