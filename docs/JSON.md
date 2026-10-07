# JSON contracts

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [ChangeSets](CHANGESETS.md)

## 1. Two JSON layers

| Layer | Library / representation | API |
| --- | --- | --- |
| POI entities and nested network snapshots | Newtonsoft.Json `JObject` / `JArray` | Entity `Parse` / `TryParse`, `ToJSON`, `ToJSONSnapshot` |
| Immutable property values and ChangeSet payloads | System.Text.Json `JsonElement` | ChangeSet constructors and `JsonSerializer` |
| Scoped runtime status instructions | System.Text.Json immutable records | `RoamingNetworkRuntimeUpdate`, `JsonSerializer`, `ApplyRuntimeUpdate` |

The entity JSON contract uses names such as `@id`, `chargingStationOperators`, `EVSEs`,
`socketOutlets` and `tariffIds`. ChangeSet JSON uses the C# property names by default:
`Id`, `BaseRevision`, `Changes`, `Kind`, `EntityType`, `NewValue`, etc.

These layers are deliberately distinct. A ChangeSet's `NewValue` can contain a POI document
without renaming the document's properties.

## Profile declarations

Tagged POI documents declare `contentProfile: "wwcp-poi-static-v1"`; complete tagged parsers require
it when ETags are present. Fresh untagged input may omit it. The declaration is transport metadata,
excluded from immutable storage/digest inputs. Same-named customer fields remain content.
Commit/archive headers require `ContentProfile` with the same value; commit IDs/signatures bind it.
See [profile relationships, canonical bytes and fixed references](INTEROPERABILITY.md).

Ordinary entity `ToJSON` views can omit content/timestamps needed by their full static validators.
Their view roundtrip tests compare presentation fields independently of derived validators;
complete snapshot/transport tests validate the exact ETags and canonical bytes.

## 2. Network snapshots

Complete documents include network-owned manufacturers/grid/parking operators and operator-owned
EVSE/station/pool/tariff groups. Parking operators own `parkingGarages`, `parkingSpaces`,
`parkingSensors` and `parkingSpaceGroups`. These arrays contain expanded objects; group members
and parking links are ID strings. Grid/parking operator identities use `id`, other graph nodes
use `@id`. Import resolves infrastructure before groups and parking; see [graph contract](GRAPH.md).


`RoamingNetwork.ToJSONSnapshot()` writes the nested hierarchy, current timestamped operational
and admin statuses, entity creation/change timestamps, custom data and the supported POI fields.
Captured versions also carry `revision` and, when available, `appliedChangeSetId`.

```csharp
var jsonText = network.ToJSONSnapshot().ToString();
var restored = RoamingNetwork.Parse(jsonText);
```

A versioned snapshot restores its revision metadata and immutable storage. An ordinary
unversioned nested document is also captured during import and starts at revision zero.
Valid optional property presence and explicit nulls remain in storage. Managed timestamps receive
initial defaults once; owned graph arrays have the canonical empty value `[]` when empty.
Hashes and complete transports read stored static properties directly, preserving old-value
preconditions across reloads. See [property defaults](ETAGS-CBOR.md#versioned-property-presence-and-defaults).

`RoamingNetworkDataSnapshot.Parse(string/JObject)` validates declared ETags and returns only static
storage. For a domain network use the shared `POIRepresentation.ParseJSON` to validate ETags;
ordinary entity `Parse` methods perform domain parsing and do not themselves check those declarations.

Status history, internal/runtime data and domain properties absent from the existing serializers
are outside this persistence contract. Do not assume that every member of a POI class is
persisted merely because it exists in C#.

`ToJSON()` retains expansion controls and custom callbacks. Use `ToJSONSnapshot()` for the
authoritative static version with current runtime statuses, including the operational/admin
statuses of directly owned pool/station energy meters, EVSE meters and grid-connection-point
meters, plus embedded and registered grid operators, groups and parking entities. They remain in their instances and their current values
are overlaid by `ToJSONSnapshot()`. Direct runtime changes do not increase `revision` or change
entity timestamps. Entities whose runtime status is `Removed` remain in
the snapshot; removing POI nodes requires a ChangeSet Remove operation.

`DataSnapshot.ToJSON()`, `GetEntityJSON()` and `WriteTo()` export only static data. Import/capture
removes schema-defined runtime fields before storing entity documents. Reimporting a
`ToJSONSnapshot()` document restores its current statuses in domain objects and its revision;
its `DataSnapshot` remains strictly static. Customer fields inside `customData` are preserved.

For static version export without a complete intermediate `JObject` tree:

```csharp
using System.Text.Json;

using var stream = File.Create("network-snapshot.json");
using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

network.DataSnapshot.WriteTo(writer);
```

`GetEntityJSON(type, id, parentId)` exports a fresh document for a subtree. `GetEntity()` returns
the immutable node itself. Connector lookups require their EVSE scope.

### Pool and station electrical limits

`maxCurrent`, `maxPower` and `maxCapacity` are optional SI strings mapped to Styx `Ampere`,
`Watt` and `WattHour`, for example `"125 A"`, `"250 kW"` and `"20 kWh"`. Zero is retained;
negative values, numeric JSON and strings without units are rejected. Absent or null values
represent no local limit. Serialization writes canonical unit-bearing strings and CBOR uses
the corresponding metrological values. These static values are included in both content ETags.
ChangeSet old-value checks normalize equivalent explicit units, such as `"250 kW"` and `"250000 W"`.

Real-time limits and prognoses remain mutable timestamped quantities within the entity. They
are excluded from the static profile and are not added to the snapshot persistence contract.

### Pool and station energy meters

`ChargingPool.energyMeters` and `ChargingStation.energyMeters` are optional arrays of directly owned, embedded energy-meter
objects. An absent property or an empty array represents zero meters; JSON null and non-array
values are rejected. Every entry must be an object with a valid `id` and `lastChange` timestamp.
Meter IDs must be unique within their owner, compared without case sensitivity. These meters
are independent of the optional `EVSE.energyMeter` and `GridConnectionPoint.energyMeter` objects.

`EnergyMeter.role` is an optional non-empty string describing the measurement connection,
for example `"grid"`, `"pv"` or `"battery"`. The domain constructor trims it and normalizes it
to lowercase. The vocabulary is open; additional roles are allowed, and several meters may
have the same role. The role is immutable POI data, separate from the mutable operational status.

```csharp
var station = new ChargingStation(
    ChargingStation_Id.Parse("DE*ABC*S1"),
    EnergyMeters: [
        new EnergyMeter(EnergyMeter_Id.Parse("grid-meter"), Role: "grid"),
        new EnergyMeter(EnergyMeter_Id.Parse("pv-meter"), Role: "pv")
    ]);

var json = station.ToJSON(); // energyMeters contains both complete meter objects
var restoredStation = ChargingStation.Parse(json);
```

The constructor detaches the input meters, including their runtime schedules. The
`EnergyMeters` collection exposes those stored instances so their statuses can still change.
Replace the complete `energyMeters` property through a pool/station `UpdateProperty` ChangeSet;
use `[]` to clear it. Meters remain nested owner properties in the versioned graph.
For individual membership/metadata changes use addressed [element operations](ELEMENT-OPERATIONS.md).

### Grid connection points

`ChargingPool.gridConnectionPoint` is an optional object (zero or one). Its `gridOperator` is
a required embedded operator document with an `id`; `energyMeter` is optional. The embedded
operator carries a `roamingNetworkId`, its static data and timestamped current statuses.
Parsing into a supplied pool/network validates that network reference; standalone parsing
reconstructs a network reference from that ID. The operator is not automatically registered
as an independent graph node. An absent or null connection point means no grid connection point.

Replace the complete `gridConnectionPoint` property to edit it; JSON null removes it. Static
replacement payloads reject runtime fields. Operator/meter histories are independently preserved
when owner, connection-point identity and child ID match. Electrical properties use Styx `Volt`, `Hertz`, `Watt` and
`VoltAmpere` value types. JSON writes invariant unit-bearing strings such as `"400 V"`,
`"50 Hz"`, `"250 kW"` and `"300 kVA"`. Numbers and unitless strings are rejected.
See [Grid connections](GRIDCONNECTIONS.md)
for fields and units.
Connection-point and child properties can also be edited via structured element paths; whole
property replacement remains available.

## 3. ChangeSet JSON

Nested operations additionally carry an immutable `ElementPath` array of `PropertyName`/optional
`ElementId` objects. Original graph operations use `[]` and can omit that field on input. New
element operations require a nonempty path. Unknown operation/path fields are rejected. The
[element guide](ELEMENT-OPERATIONS.md#json-and-signing) lists operation shapes and examples.

`RemoveProperty` uses an empty path; `RemoveElementProperty` requires an existing addressed
object. Both require `PropertyName`, forbid `NewValue` and optionally carry `OldValue`, including
explicit JSON null. Application removes the key and validates the remaining data. This differs
from `UpdateProperty`/`UpdateElementProperty` writing a present null value.

History three-way merge metadata uses the reserved `Metadata.wwcpPOIMerge` object with its profile,
typed ancestor/left/right commit tuples and sequential explicit resolution records. These remain
ordinary JSON values in ChangeSet CBOR so their v2 signing representation is preserved. See
[merge audit metadata](MERGING.md#audit-metadata-and-signatures).

With default System.Text.Json options, a property update is represented as follows:

```json
{
  "Id": "change-42",
  "RoamingNetworkId": "network-a",
  "BaseRevision": 0,
  "BeforeETags": [
    ["json", "sha256", "hex", "<source canonical JSON digest>"],
    ["cbor", "sha256", "hex", "<source deterministic CBOR digest>"]
  ],
  "AfterETags": [
    ["json", "sha256", "hex", "<result canonical JSON digest>"],
    ["cbor", "sha256", "hex", "<result deterministic CBOR digest>"]
  ],
  "CreatedAt": "2026-10-06T12:30:00+00:00",
  "Changes": [
    {
      "Kind": "UpdateProperty",
      "EntityType": "EVSE",
      "EntityId": "DE*ABC*E1",
      "ElementPath": [],
      "PropertyName": "maxPower",
      "OldValue": "100 kW",
      "NewValue": "150 kW"
    }
  ],
  "Description": {
    "de": "Maximalleistung angepasst",
    "en": "Updated maximum power"
  },
  "Metadata": { "acme:ticketId": "INC-4711" },
  "Signatures": []
}
```

```csharp
using System.Text.Json;

var json = JsonSerializer.Serialize(changeSet);
var restored = JsonSerializer.Deserialize<RoamingNetworkChangeSet>(json)
               ?? throw new ArgumentException("Missing ChangeSet document.");
```

Digest placeholders above must be replaced with 64 lowercase hexadecimal digits; use
`snapshot.CreateChangeSet(id, createdAt, changes)` to generate actual values.
Required batch fields are `Id`, `RoamingNetworkId`, `BaseRevision`, `BeforeETags`, `AfterETags`,
`CreatedAt`, `Changes`, `Description`, `Metadata` and `Signatures`. Description/metadata are objects;
signatures form an array of equal peer envelopes, empty for unsigned batches. Both ETag properties are `ImmutableArray<ETag>` in C#, containing exactly
JSON then CBOR SHA-256 ETags. Each JSON entry is the four-element `[format, algorithm, encoding, encodedDigest]`
array shown above, not a packed string or a serialized CLR object. Both JSON serializers have
type converters implementing the same contract. Digest length is 32 bytes: `hex` uses 64 lowercase
digits, `base64` uses standard RFC 4648 Base64 with canonical padding and no whitespace.
The default output is HEX. Both readers decode the declared encoding to bytes and compare by
ETag value; HEX and Base64 for the same digest are interchangeable. The encoding is separate
from the C# ETag identity. Unknown labels and unlabelled three-element JSON tuples are rejected.
Missing, null/default, malformed, duplicate or reordered digest entries are rejected. Both arrays
survive System.Text.Json serialization/deserialization and cannot be edited in place.
Required operation fields are `Kind`, `EntityType` and `EntityId`; constructors validate the
remaining fields according to the operation kind.

Absent old/new values and parent fields are omitted when serializing. The custom
`OptionalJsonElementConverter` preserves an explicit JSON null as a defined `JsonElement`,
separately from an omitted nullable value. Other optional fields follow their configured/default
System.Text.Json behavior.

The default enum converter writes operation kinds as names. Serializer options are supplied by
the caller; top-level ChangeSet and signature-envelope objects reject unknown members. Operation
objects retain their existing unknown-member behavior. Extension key/value data belongs in `Metadata`.
The singular `Signature` property is rejected. Agree on naming options at an interchange boundary. Deserialization validates the batch's
shape, not whether its operations are applicable to a particular snapshot.

Applying the batch checks both declared source identifiers before operations and both declared
result identifiers after managed timestamp updates. See [ChangeSets](CHANGESETS.md#before-and-after-content-checks).
Payload ETags are derived metadata and are removed according to the POI schema before storage;
customer fields named `ETags` inside `customData` remain content. The original operation payloads
and the before/after arrays remain intact in the batch supplied to a signature verifier.

An explicitly prepared `TryMerge` result is an ordinary unsigned ChangeSet with the same JSON
contract: common-source `BeforeETags`, combined ordered operations and freshly calculated
`AfterETags`. Source signatures and commit metadata are not copied. Add the merge's own description
and metadata before signing it. Preview and conflict reports do not create a batch
for interchange. `RoamingNetworkCommit` supplies ordered ancestry and a typed deterministic ID;
`RoamingNetworkHistory` retains and publishes these envelopes. See
[merging](CHANGESETS.md#merging-concurrent-batches) and [history](HISTORY.md).

Each signed array entry has this shape (the Base64 placeholder is illustrative):

```json
{
  "Algorithm": "Ed25519",
  "KeyId": "acme:alice",
  "Value": "<canonical Base64 signature bytes>",
  "Profile": "wwcp-poi-changeset-json-v2",
  "Encoding": "base64"
}
```

All five fields are required. `Sign` appends entries. The built-in canonical signing profile binds
the complete batch including multilingual descriptions/JSON metadata and each peer's header;
the peer array is excluded. Transport options do not change signing field names or ETag encoding.
See [signatures](SIGNATURES.md) for the exact byte contract, key resolution and verification.

## 4. Resolving ID-only hierarchy references

Standalone entity exports may contain child IDs rather than expanded child objects. Parsing them
requires `InfrastructureJsonParsingContext` resolvers returning matching JSON documents:

```csharp
using Newtonsoft.Json.Linq;
using cloud.charging.open.protocols.WWCP.POI;

// operatorDocuments is an application-supplied dictionary of operator JSON documents.
var context = new InfrastructureJsonParsingContext
{
    ResolveChargingStationOperator = id =>
        operatorDocuments.TryGetValue(id.ToString(), out var document) ? document : null
};

var network = RoamingNetwork.Parse(networkJsonText, Context: context);
```

Resolvers exist for brands, tariffs, EVSEs, stations, pools, operators and providers. The parser
checks the returned document's ID, copies resolver-owned documents and creates objects attached
to the new hierarchy. It does not attach mutable objects from another network.

Unresolved IDs, duplicate IDs, inconsistent parent/operator IDs, expanded ancestor objects and
unsupported flattened expanded hierarchy lists are rejected. Supplementary flat ID lists are
checked against the reconstructed hierarchy.

The ChangeSet applier has no resolver parameter. Its added subtrees must contain supported
expanded embedded documents; do not pass unresolved child references expecting a remote lookup.

## 5. Numbers, units and dates

Text parsers use a `JsonTextReader` configured with:

- `FloatParseHandling.Decimal`, retaining tariff decimal precision.
- `DateParseHandling.None`, leaving timestamp interpretation to the domain parsers.

For a caller-created `JObject`, use those settings when reading input. Default double parsing
can lose decimal precision before the domain parser sees it; that loss cannot be recovered later.

| Field / value | Convention |
| --- | --- |
| Electrical voltage/current/power/capacity | Unit-bearing strings, e.g. `"400 V"`, `"32 A"`, `"250 kW"`, `"20 kWh"` |
| EVSE `maxVoltage` | The model's typed `MaxVoltage` quantity |
| Tariff prices | Decimal JSON numbers; string prices are rejected |
| Tariff energy/power bounds | Unit-bearing strings in `minEnergy`/`maxEnergy` and `minPower`/`maxPower` |
| Tariff energy/current billing increments | Positive unit-bearing `stepSize`, e.g. `"1 kWh"` or `"0.5 A"` |
| Cable length/resistance | Unit-bearing strings, e.g. `"5.25 m"`, `"1250.5 µΩ"` |
| Geographic `alt`/`altitude` | Unit-bearing metre strings such as `"25.125 m"`; coordinates retain their geographic representation |
| Energy-source/environmental-impact shares | Unit-bearing `percentage`, e.g. `"25 %"` |
| Product/tariff durations | Unit-bearing strings such as `"300 s"`, with TimeSpan tick precision |
| Tariff billing duration increments | Positive unit-bearing strings such as `"0.5 s"` |
| Flat tariff fees | No `stepSize` property |
| Transparency certificate validity | Nullable UTC-normalized DateTimeOffset, retaining tick precision |

Formatting uses invariant decimal separators and selects SI prefixes while preserving the full
stored decimal precision. Property names identify quantities, independent of their chosen unit:
product `stopChargingAfterEnergy`, tariff restriction `minEnergy`/`maxEnergy` and energy-mix
`percentage`. There are no field aliases or implicit scales. Each quantity must be a JSON
string containing its numerical value and a supported SI unit or SI prefix. Numeric JSON values,
unitless strings and quantities with the wrong dimension are rejected, including in ChangeSets.

Tariff restriction C# APIs use `Range<WattHour?>? Energy` and `Range<Watt?>? Power`.
Billing components are created through typed factories (`Energy`, `MaximumCurrent`,
`MinimumCurrent`, `ChargingTime`, `ParkingTime`) and expose `EnergyStep`, `CurrentStep` or
`DurationStep`. The `FlatRate` factory has no physical increment.

Import, snapshot capture and applied property updates normalize supported quantities to the new
format. ChangeSet preconditions normalize these fields too, so `"250000 W"` and `"250 kW"`
compare equally for an EVSE power field. Remove preconditions normalize
quantities recursively. Raw ChangeSet payloads retain the caller's representation for signature
verification; use unit strings when preparing new batches.

Snapshot/entity/status timestamp handling is defined by its parser. Managed snapshot timestamps
and separate runtime status instructions are normalized to UTC. Timestamp strings require an explicit offset or `Z`.
Creation/change timestamps are restored without firing ordinary
property mutation updates.

Runtime instructions use their own [JSON contract](RUNTIME.md#json-contract). Static ChangeSets
reject operational statuses, including in nested replacement documents and Remove preconditions.
`IncludeVersionMetadata` on `ToJSONWithETags()` and `ToCBOR()` exports revision information
independently of `IncludeRuntime`; see [snapshot transport](ETAGS-CBOR.md#api).

See the [test wire conventions](../WWCP_POI_Tests/README.md#wire-conventions) for cable,
coordinate, product and authentication details.

## 6. Schema and extension boundaries

- Charging modes require a flat array of enum names; nested arrays are rejected.
- `currentType` remains an array of enum names.
- Energy mixes require `energySources` and `environmentalImpacts` arrays; use `[]` for unknown composition.
- Software `openSourceLicense` requires a license object with `@id`; string licenses and alternative
  field names are rejected. Optional descriptions and URLs belong to that object.
- Custom parsers and serializers support application extensions at the domain API.
  Snapshot capture and ChangeSet validation still use the explicit schema; extension fields should
  use supported `customData` or receive corresponding schema/parser support.
- Null, zero, false and empty collections are different values. Parsers and tests preserve them
  according to each field's contract.

Roundtrip tests compare serialized fields and documents where domain equality only compares
identity or selected fields. This helps detect information loss that a simple object equality
assertion would miss.

Ordinary JSON output is not canonical signing data. See [signatures](SIGNATURES.md) before
using serialized documents in a cryptographic interchange contract.

## POI ETags and CBOR

ChangeSets have a separate `ToCBOR` / `ParseCBOR` / `TryParseCBOR` transport retaining signatures,
ordered operations, metadata and optional/null payloads. Its numeric wrapper preserves the v2
signing profile's JSON number spelling. See [ChangeSet CBOR](CHANGESET-CBOR.md).

`IImmutablePOI.ETags` provides canonical JSON and deterministic CBOR SHA-256 identifiers.
The properties use typed immutable `ETag` values. JSON uses `[format, algorithm, encoding, encodedDigest]`;
CBOR uses `[format, algorithm, digestBytes]`, with a native 32-byte byte string. Textual
`json:sha256:hex:...` is the display/explicit text-parser form. Wire parsers reject packed strings,
unknown format/algorithm labels, wrong tuple shapes and wrong digest lengths.
Use `ToJSONWithETags(DigestEncoding: ETagDigestEncoding.Base64)` for a complete POI document
with Base64 ETags. The default `ETagJSONConverter` writes HEX; configuring
`new ETagJSONConverter(ETagDigestEncoding.Base64)` in System.Text.Json options selects Base64
for typed tags in serialized ChangeSets. Newtonsoft.Json's attribute converter writes HEX;
explicit `ETag.ToJSON(encoding)` supports both encodings. All import paths honor the declared label.
Use `ToJSONWithETags()` or `ToCBOR()` for complete static POI exports, and `ParseCBOR` for
the second import format. See [ETags and CBOR](ETAGS-CBOR.md) for the exact content boundary,
parent contexts and ETag verification.

## Incremental history messages

`RoamingNetworkReplicationState` declares `wwcp-poi-replication-state-v1`, the required
`ContentProfile`, typed checkpoint/head IDs and retained `KnownTips`. `RoamingNetworkCommitPack`
declares `wwcp-poi-commit-pack-v1`, the required `ContentProfile`, the original `CheckpointCommit`,
typed `Tip`, Boolean `Complete` and ordered original `Commits`. Both contracts have exact JSON
and native CBOR codecs. All commit IDs use structured `json` SHA-256 tuples in either transport.
The checkpoint envelope has peer signatures but no static checkpoint snapshot or runtime data.

Input parsers impose configurable raw byte/count limits before commit construction. Atomic import
verifies trust, parents, replay and resulting state before retaining the entire page. The message's
routing/completion declarations never select a head. See [replication](REPLICATION.md) for exact
field tables, bounds, missing-parent outcomes and explicit adoption.

## Bootstrap messages

`RoamingNetworkBootstrapManifest` declares `wwcp-poi-bootstrap-manifest-v1`, required content/archive
profiles, typed manifest/checkpoint/head/archive identifiers, archive/chunk/commit counts and ordered
SHA-256 fragment digests. JSON explicitly labels their Base64 encoding; CBOR carries byte strings.
`RoamingNetworkBootstrapChunk` declares `wwcp-poi-bootstrap-chunk-v1`, required `ContentProfile`,
typed `Manifest`, zero-based `Index` and binary `Data` (labelled Base64 in JSON, native bytes in CBOR).
Fragments may split any CBOR token; they are verified/staged before the complete deterministic archive
is decoded. Exact parsers impose local byte/count bounds and recompute the manifest identity.
These messages contain no runtime and do not select an existing head. Complete field tables,
canonical identity, resume and preview/activation rules are in [bootstrap](BOOTSTRAP.md).
