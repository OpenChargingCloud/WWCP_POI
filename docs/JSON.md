# JSON contracts and compatibility

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [ChangeSets](CHANGESETS.md)

## 1. Two JSON layers

| Layer | Library / representation | API |
| --- | --- | --- |
| POI entities and nested network snapshots | Newtonsoft.Json `JObject` / `JArray` | Entity `Parse` / `TryParse`, `ToJSON`, `ToJSONSnapshot` |
| Immutable property values and ChangeSet payloads | System.Text.Json `JsonElement` | ChangeSet constructors and `JsonSerializer` |

The entity JSON contract uses names such as `@id`, `chargingStationOperators`, `EVSEs`,
`socketOutlets` and `tariffIds`. ChangeSet JSON uses the C# property names by default:
`Id`, `BaseRevision`, `Changes`, `Kind`, `EntityType`, `NewValue`, etc.

These layers are deliberately distinct. A ChangeSet's `NewValue` can contain a POI document
without renaming the document's properties.

## 2. Network snapshots

`RoamingNetwork.ToJSONSnapshot()` writes the nested hierarchy, current timestamped operational
and admin statuses, entity creation/change timestamps, custom data and the supported POI fields.
Captured versions also carry `revision` and, when available, `appliedChangeSetId`.

```csharp
var jsonText = network.ToJSONSnapshot().ToString();
var restored = RoamingNetwork.Parse(jsonText);
```

A versioned snapshot restores its revision metadata and immutable storage. Parsing an ordinary
unversioned nested document reconstructs the mutable domain hierarchy; subsequent capture creates
revision zero.

Status history, internal/runtime data and domain properties absent from the existing serializers
are outside this persistence contract. Do not assume that every member of a legacy POI class is
persisted merely because it exists in C#.

`ToJSON()` remains the legacy serializer, with expansion controls and custom callbacks.
After capture, it serializes compatibility objects that may have been edited independently.
Use `ToJSONSnapshot()` for the authoritative version.

For export without a complete intermediate `JObject` tree:

```csharp
using System.Text.Json;

using var stream = File.Create("network-snapshot.json");
using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

network.DataSnapshot.WriteTo(writer);
```

`GetEntityJSON(type, id, parentId)` exports a fresh document for a subtree. `GetEntity()` returns
the immutable node itself. Connector lookups require their EVSE scope.

## 3. ChangeSet JSON

With default System.Text.Json options, a property update is represented as follows:

```json
{
  "Id": "change-42",
  "RoamingNetworkId": "network-a",
  "BaseRevision": 0,
  "CreatedAt": "2026-10-06T12:30:00+00:00",
  "Changes": [
    {
      "Kind": "UpdateProperty",
      "EntityType": "EVSE",
      "EntityId": "DE*ABC*E1",
      "PropertyName": "maxPower",
      "OldValue": 100000,
      "NewValue": 150000
    }
  ],
  "Signature": null
}
```

```csharp
using System.Text.Json;

var json = JsonSerializer.Serialize(changeSet);
var restored = JsonSerializer.Deserialize<RoamingNetworkChangeSet>(json)
               ?? throw new ArgumentException("Missing ChangeSet document.");
```

Required batch fields are `Id`, `RoamingNetworkId`, `BaseRevision`, `CreatedAt` and `Changes`.
Required operation fields are `Kind`, `EntityType` and `EntityId`; constructors validate the
remaining fields according to the operation kind.

Absent old/new values and parent fields are omitted when serializing. The custom
`OptionalJsonElementConverter` preserves an explicit JSON null as a defined `JsonElement`,
separately from an omitted nullable value. Other optional fields follow their configured/default
System.Text.Json behavior.

The default enum converter writes operation kinds as names. Serializer options are supplied by
the caller; the library does not globally reject all unknown ChangeSet JSON properties or prescribe
a naming policy. Agree on options at an interchange boundary. Deserialization validates the batch's
shape, not whether its operations are applicable to a particular snapshot.

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
| Electrical voltage/current/power/capacity | Volts, amperes, watts, watt-hours |
| EVSE `averageVoltage` | Legacy wire name for the model's `MaxVoltage` |
| Tariff prices and energy/power bounds | Decimal JSON numbers; supported legacy invariant strings remain readable |
| Tariff restriction durations | Seconds with TimeSpan tick precision |
| Tariff billing duration increments | Positive whole seconds |
| Transparency certificate validity | Nullable UTC-normalized DateTimeOffset, retaining tick precision |

Snapshot/entity/status timestamp handling is defined by its parser. Managed snapshot timestamps
and status updates are normalized to UTC. Transparency-software validity dates also accept
timezone-free legacy text as UTC. Creation/change timestamps are restored without firing ordinary
property mutation updates.

See the [test wire conventions](../WWCP_POI_Tests/README.md#wire-conventions) for cable,
coordinate, product and authentication details.

## 6. Compatibility and extension boundaries

- Charging modes are written as a flat array; legacy nested flag arrays remain readable.
- `currentType` remains an array of enum names.
- Tariff energy mixes now preserve composition arrays; missing old arrays mean unknown composition.
- Software `openSourceLicense` stores a full license object. Legacy license strings remain readable,
  but omitted multilingual descriptions/URLs cannot be reconstructed from those strings.
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
