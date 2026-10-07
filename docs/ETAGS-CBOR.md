# Immutable POI content, ETags and CBOR

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [JSON](JSON.md) · [Signatures](SIGNATURES.md)

## Immutability boundary

The network's owned graph includes groups, manufacturers and grid/parking operators with parking
children. Their static documents participate in their owners' ETags and use independent ChangeSet
node addresses. Group IDs are references, so a member's content enters the network hash through
its owned infrastructure location. See [graph content and runtime](GRAPH.md#content-identifiers-and-runtime).


The domain contract freezes static POI data, ownership and membership. Operational/admin
statuses, their schedules, real-time measurements, forecasts, aggregation delegates and
application `InternalData` remain runtime state inside the domain objects.

The audit covers the following locally defined domain objects:

| Objects | Static data |
| --- | --- |
| Network, charging operator, provider, pool, station, EVSE, connector, tariff | Immutable properties, detached metadata and immutable membership |
| EVSE, station, pool and tariff groups | Immutable configuration, allowed IDs and member references |
| Cable, energy meter, grid connection point, grid operator, parking operator | Immutable metadata; meter/operator operational statuses remain mutable |
| Manufacturer, transparency software and transparency software status | Immutable public identities, license metadata and legal certificate values |
| Parking garage, space, sensor and space group | Immutable geometry, station references and sensor IDs |
| Brand, energy mix, charging/parking product, image, additional location | Immutable values or detached copies on access |
| Tariff element, restriction and price component | Immutable arrays, ranges and SI quantities |
| Public/ECC key, root CA and roaming partner information | Detached key bytes, custom data and multilingual text |
| Immutable multilingual text, opening hours and key description | Detached immutable value representations |
| Network data snapshot | Persistent immutable dictionaries, sets and JSON values |
| ETag | Readonly value struct with typed format/algorithm and detached immutable digest bytes |

The mutable, unused `AEMobilityEntity` base has been removed. `ParkingSpaceGroup` now uses
`AImmutableEMobilityEntity`. `Brand`, `EnergyMix`, root CA/partner information and additional
locations detach mutable text. Brand licenses are materialized and copied. Public keys detach
byte arrays on input and access and use an immutable custom-data base. Identifier and priority
classes are sealed values. Mutable dependency values such as addresses and licenses are
copied at the POI boundary.

Runtime schedules, forecasts, status reports/diffs, command results, parsers and event helpers
are not POI nodes and do not acquire a blanket immutability guarantee. Group predicates and
status aggregation delegates describe application runtime behavior and are not serialized.

## Two identifiers

`IImmutablePOI.ETags`, `BeforeETags` and `AfterETags` use `ImmutableArray<ETag>` with exactly
two entries: canonical JSON, then deterministic CBOR. Each identifier is a `readonly struct`
with `ETagFormat Format`, `ETagHashAlgorithm Algorithm` and `ImmutableArray<Byte> Digest`.
Constructor input bytes are copied. Equality and hashing compare the digest content.
`ETagDigestEncoding` selects a textual transport encoding and is not stored as part of the
identifier. A HEX tuple and a Base64 tuple containing the same bytes identify the same content.

JSON represents each identifier as `[format, algorithm, encoding, encodedDigest]`:

```json
{
  "ETags": [
    ["json", "sha256", "hex", "<64 lowercase hexadecimal digits>"],
    ["cbor", "sha256", "hex", "<64 lowercase hexadecimal digits>"]
  ]
}
```

CBOR uses a three-element array with a native 32-byte digest. Its byte-string type already
identifies the representation; it needs no textual encoding label:

```text
"ETags": [
  ["json", "sha256", h'<32 digest bytes>'],
  ["cbor", "sha256", h'<32 digest bytes>']
]
```

The first entry identifies canonical JSON content, the second deterministic CBOR content.
Their format labels describe what was hashed; both identifiers can be transported in either
JSON or CBOR. CBOR transport does not turn a JSON-content identifier into a CBOR-content identifier.

`ETag.ToString()` returns `json:sha256:hex:<hex>` or `cbor:sha256:hex:<hex>` for display/logging;
`ETag.Parse(string)` parses that explicit human-readable form. JSON and CBOR parsers require
the structured arrays and do not accept a packed string. JSON requires an explicit `hex` or
`base64` label. HEX requires 64 lowercase digits; Base64 requires the standard RFC 4648 alphabet,
canonical padding, no whitespace and a decoded length of 32 bytes. CBOR requires a byte string.
Unknown encoding labels and the previous unlabelled JSON tuples are rejected.
Only `json`/`cbor`, `sha256` and the correct digest length are
supported. The default struct value is invalid and cannot be serialized or used in a batch.
System.Text.Json and Newtonsoft.Json converters use the same JSON array contract.

```csharp
ETag tag = network.ETags[0];
ETagFormat format = tag.Format;
ETagHashAlgorithm algorithm = tag.Algorithm;
var bytes = tag.Digest; // ImmutableArray<byte>
var display = tag.ToString();
var fromJSON = ETag.Parse(tag.ToJSON());
var fromCBOR = ETag.Parse(tag.ToCBOR());

var base64JSON = tag.ToJSON(ETagDigestEncoding.Base64);
var sameTag = ETag.Parse(base64JSON); // sameTag == tag
var base64Text = tag.ToString(ETagDigestEncoding.Base64); // json:sha256:base64:...
var base64Document = network.ToJSONWithETags(DigestEncoding: ETagDigestEncoding.Base64);

// The default serializers emit HEX. Override System.Text.Json output explicitly if needed.
var options = new System.Text.Json.JsonSerializerOptions();
options.Converters.Add(new ETagJSONConverter(ETagDigestEncoding.Base64));
var batch = network.CreateChangeSet("encoding-example", DateTimeOffset.UtcNow, []);
var batchJSON = System.Text.Json.JsonSerializer.Serialize(batch, options);
```

JSON output defaults to HEX in both serializers. All JSON readers decode the declared encoding
to digest bytes, so ETag checks are independent of that encoding. Native CBOR remains binary.

This changes the text and JSON wire shape of the derived identifiers. The canonical POI digest inputs
are unchanged because derived ETag fields are excluded from both hashes.

The JSON identifier is SHA-256 of Styx `CanonicalJSON.ToUTF8Bytes`. This is Styx's deterministic
RFC 8259 JSON format, with ordinal object-property order; it is not an RFC 8785 JCS claim.
The CBOR identifier is SHA-256 of Styx CBOR using `CBORWriterOptions.Canonical`, which applies
the RFC 8949 core deterministic encoding requirements.

Both encodings use the same complete, schema-defined POI projection. Owned infrastructure
children are expanded and sorted by ordinal wire ID, including children with a runtime
`Removed` status. Ancestors and group members remain references. Tariff element, restriction
and price-component order remains content. Static timestamps, data source, custom data and
transparency-software legal/certificate metadata participate in the digest. Public-key
descriptions serialize public identities; private key material is outside this public profile.

The complete transport also preserves constructor-supplied pool time zones, languages,
facilities and services; station images, features, model identifiers and configuration flags;
EVSE photos and energy mixes; root CA/roaming partner information, certification/calibration
references and provider priorities. These values participate in both identifiers.

Pool and station `maxCurrent`, `maxPower` and `maxCapacity` also participate as typed static
electrical limits, with SI strings in JSON and metrological values in CBOR. Their operational
measurements and forecasts remain excluded.

Operational/admin statuses and histories, real-time measurements, forecasts, internal data,
revision bookkeeping and **all derived POI `ETags` arrays** are excluded. Exclusion follows
the POI schema: a customer property named `status` or `ETags` inside `customData` still
participates in the digest. Legal transparency-software status is immutable content.

Consequently direct runtime updates retain the identifiers. Static content changes, including
membership and nested meter metadata changes, change the affected content identifiers.
These are POI content validators. An export with current runtime statuses can change its
transport bytes while retaining the same POI ETags. A ChangeSet that changes static
`lastChange` metadata also changes the content identifiers. Operational status updates use the
separate runtime API and never modify that metadata.

## API

```csharp
var tags = network.ETags;

// Full static POI document, with ETags on its owned POI values.
var json = network.ToJSONWithETags();
var cbor = network.ToCBOR();
var restored = RoamingNetwork.ParseCBOR(cbor);

// Exact digest inputs, excluding derived ETags and runtime data.
var canonicalJSONBytes = network.ToCanonicalJSON();
var canonicalCBORBytes = network.ToCanonicalCBOR();

// Preserve revision bookkeeping without runtime data.
var versionCBOR = network.ToCBOR(IncludeVersionMetadata: true);

// Include current operational/admin statuses as an independent transport choice.
// The identifiers still describe static POI content.
var currentCBOR = network.ToCBOR(IncludeRuntime: true, IncludeVersionMetadata: true);

// Child parsing uses the same explicit parent context as JSON.
var restoredPool = ChargingPool.ParseCBOR(pool.ToCBOR(), chargingStationOperator);
var restoredGroup = EVSEGroup.ParseCBOR(group.ToCBOR(), chargingStationOperator);

// Resolver/custom-parser contexts remain available through the shared entry point.
var resolved = POIRepresentation.ParseCBOR(
    bytes,
    json => ChargingPool.Parse(json, chargingStationOperator, Context: resolverContext)
);

// The same content-identifier validation is available for full JSON documents.
var fromJSON = POIRepresentation.ParseJSON(json, document => RoamingNetwork.Parse(document));
```

Domain `ToJSON` views include the object's content identifiers independently of their
expansion/filter settings. Use `ToJSONWithETags` for the complete static transport contract.
Multilingual text and opening-hours values retain their existing bare JSON shapes when used
as fields; their standalone tagged export is also available through `ToJSONWithETags`.

`ParseCBOR` and shared `ParseJSON` accept documents without ETags and compute the identifiers
from the reconstructed value. When an ETag array is present it must contain exactly the two
matching identifiers. Malformed, tampered or unresolvable content is rejected. Identifiers
compare format, algorithm and digest bytes. Unknown formats/algorithms, malformed tuples,
wrong digest lengths and uninitialized values are rejected. Typed
`TryParseCBOR` entry points and the shared `POIRepresentation.TryParseCBOR` return parse errors
without throwing. Reference-only groups and parking objects require the referenced objects
in the supplied operator or station collection. Complete network imports resolve these contexts
automatically for owned groups and parking nodes; the schema visitor covers every [owned collection](GRAPH.md).

Ordinary domain JSON parsers retain their existing view/resolver semantics; they do not treat
received ETags as trusted state. Snapshot imports discard derived ETags before storage and
ChangeSet old-document comparisons ignore them. `UpdateProperty("ETags", ...)` is rejected.
Signature verification still happens before import normalization.

`DataSnapshot.ToJSON` and `WriteTo` include a root content-identifier array. Passing
`IncludeETags: false` to `WriteTo` retains direct streaming without digest generation.
CBOR transport of a data snapshot uses the same static domain projection as its network.
`IncludeVersionMetadata: true` retains root `revision` and `appliedChangeSetId` independently
of current-status transport on `ToCBOR()` and `ToJSONWithETags()`. Both flags default to false.
`IncludeRuntime: true` alone does not retain revision metadata. A `DataSnapshot` has no runtime
state and rejects that flag; export the domain network to include current statuses.

## CBOR metrology

CBOR keeps text property names and the JSON ownership structure. JSON SI strings become
Styx metrological values under tag **44252**:

```text
"250 kW"  ->  44252([250, <Watt unit reference>, 3])
```

Schema paths select electrical limits, contracted powers, voltage, frequency, cable length
and resistance, product/tariff energy and duration values, percentages and geographic altitude.
Descriptions, identifiers and arbitrary `customData` remain ordinary JSON values. Decoding
converts the metrological tags back to unit-bearing strings and then invokes the existing
typed domain parsers, including their dimension, range, ownership and reference checks.

CBOR decoding rejects trailing data, duplicate map keys and unsupported CBOR items through
Styx's strict default reader/converter. It accepts logically equivalent map ordering and
valid SI representations; identifiers are recomputed from the canonical reconstructed content.

The runtime transport option covers the operational/admin status fields supported by the
existing JSON snapshot contract. Runtime histories, forecasts, application context and
real-time electrical fields remain outside that persistence contract.

## ChangeSet state transitions

Each `RoamingNetworkChangeSet` requires `BeforeETags` and `AfterETags`. Each array contains
the canonical JSON and deterministic CBOR identifier in the order defined above. They bind
the entire source and result static POI content using this same profile.

```csharp
var batch = network.DataSnapshot.CreateChangeSet("change-44", DateTimeOffset.UtcNow, [/* operations */]);
var next = network.ApplyChangeSet(batch); // checks both source and result arrays
```

Preparation computes the expected result on unpublished immutable maps. Application checks
the network ID, base revision and source ETags first, then the signature and ordered operations,
then the result ETags after timestamp updates. A mismatch returns no successor and preserves
the original snapshot. The result includes the fixed batch timestamp; nested POI defaults also
use that timestamp instead of a replica's local clock. Immutable snapshot ETags are lazily cached.

Equal ETags identify equal canonical static data with SHA-256 collision resistance. Versions
with identical content but different revisions or histories can share them. Runtime status
preconditions, authorization, signatures and replay/history storage retain their own contracts.
See [ChangeSets](CHANGESETS.md) for required header fields, preparation and errors.

`TryMerge` checks both input transitions against their common source and compares both combined
operation orders using all stored static properties. Runtime updates are outside that merge.
Explicit [element operations](ELEMENT-OPERATIONS.md) compare their addressed elements/properties
and keep touched identity arrays in deterministic order, enabling disjoint collection edits.
It returns only a notice by default. Explicit preparation produces a new unsigned
ChangeSet with the source `BeforeETags` and freshly calculated merged `AfterETags`; it does not
combine or reuse the two branch result hashes. The fixed merge timestamp participates in the
combined result profile. See [merge semantics](CHANGESETS.md#merging-concurrent-batches).

## Signatures

Content identifiers and canonical POI bytes do not replace ChangeSet signature verification.
They do not define which key is authorized to modify a network. The existing signature
array and per-peer verification hook remain available. `Sign`/`TrySign` and `VerifySignature`/
`VerifySignatures` implement the separate `wwcp-poi-changeset-json-v2` payload profile, covering
ordered operations, header, both state identifiers, descriptions/metadata and each peer's header.
Every signature binds the same batch content; its peer signature array is excluded so signatures
can be added independently. See [signatures](SIGNATURES.md).
The v2 signing input also binds every operation's complete `ElementPath`. This changes ChangeSet
signature bytes/profile, not the static POI ETag profiles.
