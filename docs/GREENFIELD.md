# Greenfield model and JSON contract

[Repository overview](../README.md) · [JSON](JSON.md) · [Architecture](ARCHITECTURE.md)

WWCP_POI defines its own contract. It does not import previous WWCP document formats,
infer physical units from property names, or maintain alternate input representations.

## Implemented cleanup

| Area | Current contract |
| --- | --- |
| Styx electrical/energy/length/resistance quantities | JSON strings with explicit SI units/prefixes; numbers and unitless strings are rejected |
| Product and tariff durations | JSON seconds with explicit `s`, preserving TimeSpan ticks |
| Geographic altitude | `alt`/`altitude` strings with explicit `m`, preserving the Aegir value's floating-point precision |
| Tariff restriction bounds | `Range<WattHour?>? Energy` and `Range<Watt?>? Power`; typed factories such as `MinEnergy(WattHour)` |
| Billing increments | Typed `EnergyStep`, `CurrentStep`, `DurationStep`; dimension-specific factories; positive fractional quantities supported |
| Flat fees | No physical increment or `stepSize` property |
| Product energy cutoff | `StopChargingAfterEnergy` in C# and `stopChargingAfterEnergy` in JSON |
| EVSE voltage limit | `MaxVoltage` in C# and `maxVoltage` in JSON |
| Energy composition | Explicit source/impact arrays and `percentage` strings containing `%` |
| Enum flags | Flat arrays of declared names; nested arrays and alternate current-type names rejected |
| Prices | Decimal JSON numbers; string prices rejected |
| Timestamps | Explicit offset or `Z`; no inference of UTC for timezone-free text |
| Software licenses | Object at `openSourceLicense`, with `@id`; no string licenses, ID alias or alternative license key |
| Data licenses | Local parser for the emitted `@id` contract; no rewrites to satisfy a different dependency parser |
| Authentication | Removed obsolete `DirectPayment` subclass; typed modes use `number`/`stationCode` |
| Property spelling | `energyMix`, `daysOfWeek`, `uri`, `howToUse`, `moreInformation`, `sourceCodeRepository`; no spelling aliases |
| Snapshots and ChangeSets | Same quantity contract as entity parsers; normalization compares equivalent explicit units without inventing a scale |
| POI identities | Readonly typed `ETag` values over canonical JSON/deterministic metrological CBOR; runtime values are excluded |
| ChangeSet state binding | Required `BeforeETags` and `AfterETags`, each JSON then CBOR; no revision-only or unchecked application path |

```csharp
var restriction = new ChargingTariffRestriction(
    Energy: new Range<WattHour?>(WattHour.FromKWh(20), null),
    Power: new Range<Watt?>(null, Watt.FromKW(250)));

var energyPrice = ChargingPriceComponent.Energy(0.45m, WattHour.FromWh(100));
var timePrice = ChargingPriceComponent.ChargingTime(1m, TimeSpan.FromMilliseconds(500));
```

The deliberate breaking changes apply to constructors, properties, JSON documents and ChangeSet
payloads. Existing examples and test fixtures use this contract. Quantity normalization occurs
after signature verification and does not rewrite signed ChangeSet payloads.

ChangeSet construction/deserialization requires both `ImmutableArray<ETag>` properties. JSON
entries are `[format, algorithm, encoding, encodedDigest]`; CBOR ETags use a native 32-byte digest
inside a three-element array. Explicit JSON encodings are `hex` and canonical standard `base64`;
readers decode both to equal byte-valued identities. The previous unlabelled JSON tuple is
rejected. Packed strings include the encoding in their display/text-parser form and are not a wire
alias. Both JSON serializers use converters for this contract. Use `CreateChangeSet` on the
source snapshot/network to compute a validated batch;
receivers preserve its declarations and check source/result content. Nested POI timestamp defaults
use the fixed batch `CreatedAt`. See [ChangeSets](CHANGESETS.md) and [ETags/CBOR](ETAGS-CBOR.md).

ChangeSets use an immutable `Signatures` array of equal peers; the singular JSON field is rejected.
Multilingual descriptions and arbitrary JSON key/value metadata are part of their canonical signing
profile. `Sign`/`TrySign` append signatures using Styx; metadata edits clear the array on the new copy.
See [signatures](SIGNATURES.md) for the fixed v1 byte contract and trusted-key verification.

## Further structural opportunities

These are dependency boundaries and separate model changes, rather than alternate JSON formats.

| Area | Current dependency | Possible POI design |
| --- | --- | --- |
| Entity metadata/text | Illias `IEntity`/`IInternalData` expose mutable members; POI rejects writes and returns detached text | Local read-only entity interface; adapt only where a dependency requires its interface |
| Nested address/license/brand values | Some dependency objects are mutable and copied defensively | Local immutable value types with direct POI parsing and serialization |
| Geographic values | Aegir coordinate types store floating-point latitude, longitude and altitude | A local immutable location type with Styx length for altitude and one documented coordinate representation |
| Serializer error handling | Some entity serializers catch failures and return an `exception` document | A consistent throwing serializer and explicit `TryToJSON` API, so an error cannot look like POI data |
| Entity IDs | Domain comparison compensates for dependency hashing inconsistencies | Local immutable IDs whose equality, normalization and hash codes agree |

Current ID-only/expanded representations and resolver APIs are explicit export choices. Custom
parser callbacks remain extension points. Mutable operational status histories and runtime
measurements remain within their entities, independent of the immutable POI data.
