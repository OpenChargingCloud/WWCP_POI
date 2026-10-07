# Using ChangeSets

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [JSON](JSON.md) · [Signatures](SIGNATURES.md)

## Batch contract

A `RoamingNetworkChangeSet` is an immutable, ordered batch:

| Field | Meaning |
| --- | --- |
| `Id` | Application-supplied identifier for this batch |
| `RoamingNetworkId` | Target network's domain identifier |
| `BaseRevision` | Exact revision of the source snapshot |
| `BeforeETags` | Required `ImmutableArray<ETag>` of JSON and CBOR source content identifiers |
| `AfterETags` | Required `ImmutableArray<ETag>` of JSON and CBOR result identifiers after timestamp updates |
| `CreatedAt` | Timestamp used for changed entities and their ancestors |
| `Changes` | Immutable operation array; an initialized empty array is valid |
| `Description` | Immutable language-to-text map, signed commit descriptions; `{}` when empty |
| `Metadata` | Immutable string-to-JSON-value map, signed application metadata; `{}` when empty |
| `Signatures` | Immutable array of equal peer envelopes (`Algorithm`, `KeyId`, `Value`, `Profile`, `Encoding`); `[]` when unsigned |

An operation specifies `Kind`, `EntityType`, `EntityId` and operation-specific values.
Entity type names match `InfrastructureEntityType` exactly, including case. Property names
are the JSON contract's names, not necessarily the corresponding C# property names.

The constructors check operation shape and clone JSON payloads. Entity existence, valid domain
IDs, ownership and business data are checked when the batch is prepared or applied. Both ETag
arrays contain exactly one initialized JSON `ETag` followed by one CBOR `ETag`, both SHA-256.
Each readonly value stores typed format/algorithm and immutable digest bytes. JSON uses
`["json", "sha256", "hex", "<64 lowercase hex digits>"]` and the corresponding `"cbor"` tuple;
CBOR ETags use a digest byte string. Packed text is available through `ToString()` for logging.
Missing, reordered, default or malformed entries are rejected.
JSON's fourth field contains the digest encoded according to the explicit third field: `hex`
or `base64`. HEX is the default output; Base64 uses the standard alphabet and canonical padding.
The applier compares decoded digest bytes, so changing that transport encoding preserves the
same source/result identity. Unknown encodings and unlabelled JSON tuples are rejected.

The complete batch also supports `ToCBOR`, `ParseCBOR` and `TryParseCBOR`. The deterministic
map preserves all signatures and their signing input, structured paths, operation order,
description/metadata and omitted versus explicitly null payloads. The two ETag arrays use
native digest bytes. See the [binary transport contract](CHANGESET-CBOR.md).

Read old values from the authoritative snapshot: an absent property and a stored JSON null are
different preconditions. Static JSON/CBOR reloads retain that distinction; no domain serializer
is used to replace the stored properties with constructor defaults.

## 1. Read the authoritative source

The examples below assume the network from the [quick start](../README.md#quick-start).
Each operation example is an independent batch against that network.

```csharp
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;

var snapshot = network.DataSnapshot;
var evse = snapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1");
var oldPower = evse.Properties["maxPower"];
```

Capture the source snapshot once when preparing a batch. Derive its old values and
`BaseRevision` and `BeforeETags` from that same snapshot. `snapshot.CreateChangeSet` does this
automatically and computes `AfterETags` from a locally derived, validated result.

The same node operations address the owned groups, manufacturers, grid/parking operators and
parking children. The [graph contract](GRAPH.md) lists parent types, JSON fields, identity fields
and reference scopes. Referenced members must be detached before deletion; group and parking
reference arrays support element operations.

## 2. Update a property

```csharp
var update = RoamingNetworkChange.UpdateProperty(
    entityType: "EVSE",
    entityId: "DE*ABC*E1",
    propertyName: "maxPower",
    oldValue: oldPower,
    newValue: JsonSerializer.SerializeToElement("150 kW"));

var batch = snapshot.CreateChangeSet(
    id: "power-update-1",
    createdAt: DateTimeOffset.UtcNow,
    changes: [update]);

var next = network.ApplyChangeSet(batch);
```

Preparation returns an unsigned, content-bound batch without publishing a network version.
It validates all operations and supplies the target ID, base revision and both ETag arrays.
`network.CreateChangeSet(...)` is the convenience entry point using `network.DataSnapshot`.
Invalid operations fail during preparation with the same operation context as application.

To receive a batch, deserialize it and call `ApplyChangeSet`; do not recompute or overwrite its
declared ETags on the receiving server. To sign a prepared batch, add descriptions/metadata with
`batch.WithDescription(...)`/`WithMetadata(...)`, then use `Sign` or `TrySign`. Every sign call appends
an equal peer signature over the full batch, commit metadata and its own signing header.
`WithSignature(envelope)` can append an external signature. See [the signing profile](SIGNATURES.md).

The constructor remains available when expected states are already known:

```csharp
var received = new RoamingNetworkChangeSet(
    batch.Id, batch.RoamingNetworkId, batch.BaseRevision, batch.CreatedAt,
    batch.Changes, batch.BeforeETags, batch.AfterETags,
    batch.Signatures, batch.Description, batch.Metadata);
```

Every constructed batch requires both ETag arrays. JSON additionally requires `Description`,
`Metadata` and `Signatures` properties, which can be empty. Signature arrays cannot contain null.
Unknown top-level ChangeSet/signature-envelope JSON fields are rejected by System.Text.Json;
application extension fields belong in `Metadata`. The singular `Signature` field is not supported.

`OldValue` is optional. Passing C# `null` skips its precondition. When supplied, the applier
checks the currently stored static JSON value using `JsonElement.DeepEquals`; supported
metrological quantities are first normalized. Equivalent units such as `"150000 W"` and `"150 kW"`
therefore compare equally. Quantities require unit-bearing strings; numbers and unitless strings
are rejected. Quantities in complete Remove preconditions are normalized recursively.

`UpdateProperty` replaces a complete top-level property. For individual nested edits use
`AddElement`, `RemoveElement`, `ReplaceElement`, `UpdateElementProperty` and `RemoveElementProperty` with a structured
`ElementPath`; see [nested element operations](ELEMENT-OPERATIONS.md). `GetElementValue()` and
`GetElementPropertyValue()` provide detached element/property preconditions.

`UpdateProperty` is not a JSON Pointer/path operation.
For example, update a tariff's `elements` array to change one price component, or replace an EVSE's
`energyMeter` object to change its transparency-software status. For a station, replace its
complete `energyMeters` array to add/remove meters, edit their roles or change their transparency
information. Pools support the same property. Use an empty array to remove all directly owned meters. This array is an
owner property; meters do not introduce separate ChangeSet entity types. Replacement payloads
must contain only static meter data. Existing meters retain independent copies of their current
status histories when both owner and meter ID remain the same, including array replacements.

A pool's optional `gridConnectionPoint` is also replaced as a complete property. Its grid
operator is mandatory; its meter is optional. JSON null removes the connection point.
Replacement payloads must exclude operator/meter runtime fields. Matching child identities retain
their histories when the pool and connection-point identity remain the same. A changed connection
point ID or child ID starts a new runtime lifetime; see [runtime updates](RUNTIME.md).

### Missing values and explicit null

| Input | Meaning |
| --- | --- |
| `oldValue: null` / omitted `OldValue` | No old-value check |
| `oldValue: JsonSerializer.SerializeToElement<object?>(null)` | Expect an existing property whose JSON value is null |
| Omitted `NewValue` | Invalid for `Add` / `UpdateProperty` |
| `newValue: JsonSerializer.SerializeToElement<object?>(null)` | Replace with explicit JSON null, if the property accepts null |
| Empty JSON array | Replace an array-valued property with an empty collection |

An absent property does not match an expected explicit JSON null. Property updates do not delete
the property key. Use `RemoveProperty` or addressed `RemoveElementProperty` to remove an existing
optional property completely. Removal has no `NewValue`, permits an `OldValue` precondition and
validates the remaining object/relationships. Managed fields and required domain data stay protected.
Use explicit null only for nullable fields; mandatory fields and collection parsers may reject it.

## 3. Add a nested station

```csharp
var stationDocument = JsonSerializer.Deserialize<JsonElement>("""
    {
      "@id": "DE*ABC*S2",
      "name": { "en": "New station" },
      "EVSEs": [
        {
          "@id": "DE*ABC*E2",
          "currentType": [ "DC" ],
          "maxPower": "200 kW",
          "socketOutlets": [ { "@id": "1", "type": "CCS" } ]
        }
      ]
    }
    """);

var add = RoamingNetworkChange.Add(
    entityType: "ChargingStation",
    entityId: "DE*ABC*S2",
    entity: stationDocument,
    parentEntityType: "ChargingPool",
    parentEntityId: "DE*ABC*P1");

var addition = snapshot.CreateChangeSet("station-add-1", DateTimeOffset.UtcNow, [add]);

var extended = network.ApplyChangeSet(addition);
```

The supplied `@id` must match `EntityId` according to its domain identity. The payload is an
embedded, expanded entity document. Nested descendants are imported and validated in order.
Unresolved child-ID references are not resolved by the ChangeSet applier.

Operators and providers default to the source network as their parent. Pools, stations, EVSEs,
connectors and tariffs require explicit parent type/ID when added. The root network cannot be added.

## 4. Remove a subtree

```csharp
var remove = RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S1");

var removal = snapshot.CreateChangeSet("station-remove-1", DateTimeOffset.UtcNow, [remove]);

var reduced = network.ApplyChangeSet(removal);
```

Removal cascades to all descendants. It fails if the target does not exist, or if removing a tariff
would leave consumers outside the removed subtree. The root network cannot be removed.

To protect against changes anywhere inside the subtree, supply its complete expected document:

```csharp
var previousStation = snapshot.GetEntityJSON(
    InfrastructureEntityType.ChargingStation, "DE*ABC*S1");

var checkedRemoval = RoamingNetworkChange.Remove(
    "ChargingStation", "DE*ABC*S1",
    JsonSerializer.Deserialize<JsonElement>(
        previousStation.ToString(Newtonsoft.Json.Formatting.None)));
```

Moving a subtree requires explicit remove/add operations and IDs that remain valid for the new
ownership. If domain IDs encode operator ownership, changing operators can also require new IDs.

## 5. Scoped connector operations

Every connector operation requires its EVSE parent because connector IDs are local:

```csharp
var connectorUpdate = RoamingNetworkChange.UpdateProperty(
    entityType: "ChargingConnector",
    entityId: "1",
    propertyName: "lockable",
    oldValue: null,
    newValue: JsonSerializer.SerializeToElement(true),
    parentEntityType: "EVSE",
    parentEntityId: "DE*ABC*E1");

var connector = snapshot.GetEntity(
    InfrastructureEntityType.ChargingConnector, "1", parentId: "DE*ABC*E1");
```

For other existing node types, the parent can be omitted on update/removal. If supplied,
it must match the stored parent.

## 6. Static and runtime boundaries

`status`, `adminStatus`, operational histories, measurements and forecasts are not editable
ChangeSet fields. Add payloads, complete Remove preconditions and nested property replacements
also reject embedded runtime fields rather than silently discarding them. Use
`DataSnapshot.GetEntityJSON()` or the static `ToJSONWithETags()` export for operation documents.
Customer-defined `customData.status` remains static; the legal/certificate status of
`TransparencySoftwareStatus` is static as well.

`RoamingNetwork.ApplyChangeSet()` captures current runtime histories and measurements into
independent objects in the new version, even if its hierarchy is materialized later. Existing
nested meters and grid operators retain histories by owner and child identity, also across static
property replacements. New identities and removed/readded owner subtrees start with domain
runtime defaults. No runtime values are stored in `DataSnapshot` or compared by static old-value
checks or `TryMerge`.

Use `RoamingNetworkRuntimeUpdate` and `ApplyRuntimeUpdate()` for explicit status/admin-status
instructions; local domain setters and schedule methods remain available. Runtime updates do
not advance revision or alter static timestamps/ETags. See [runtime updates](RUNTIME.md) for
current-status preconditions, history replacement and coordinating updates with head publication.

## Ordering, conflicts and errors

Operations observe preceding operations in the same batch. This makes a two-step property update,
a tariff-add followed by assignment, or assignment clearing followed by tariff removal possible.

Every successful batch creates exactly one new revision. Applying a batch against another revision
fails before its operations are applied. A failed operation rejects the complete batch; the source
snapshot and all previously published versions remain unchanged.

Administrator-created [full snapshot links](SNAPSHOTS.md) also advance the history revision once,
preserving the static ETags and last applied batch ID. Prepare a subsequent batch against that
new head; matching older ETags alone does not satisfy the base-revision/publication checks.

```csharp
if (network.TryApplyChangeSet(batch, out var result, out var error))
{
    // Publish result as the application's new version.
}
else
{
    Console.WriteLine(error);
}
```

Use `ApplyChangeSet()` and catch `RoamingNetworkChangeSetException` when structured error details
are needed. `ChangeSetId` identifies the rejected batch; `OperationIndex` identifies the zero-based
operation, or is null for target/revision/signature/content-identifier failures.

The library does not keep a global current head, deduplicate ChangeSet IDs or automatically retry
stale batches. Replaying the same batch on the same immutable source can derive another successor.
Replaying it on its successor normally fails the base-revision check.

Applications should coordinate their current-head update, for example within a lock or a database
transaction, and decide how to rebuild or reject stale operations. Two branches can have equal
revision numbers; they are not interchangeable solely because those numbers match.

### Before and after content checks

Application checks the target, exact revision and `BeforeETags` before invoking a supplied
signature verifier or applying any operations. Every signature is passed to the per-peer verifier;
any failure aborts the batch. It then derives the candidate on immutable maps,
updates managed timestamps/revision and checks **both** `AfterETags` before returning it.
A mismatch raises a batch-level `RoamingNetworkChangeSetException` with `OperationIndex == null`
and an error naming `BeforeETags` or `AfterETags`, with the expected and actual identifier.
`TryApplyChangeSet` returns false and no successor. There is no public unchecked apply mode.

The identifiers describe the [static POI profile](ETAGS-CBOR.md), including owned children and
static metadata. Runtime statuses and revision bookkeeping are excluded. Direct runtime updates
therefore retain these identifiers. Static ChangeSets reject operational status fields.
Empty batches still advance the revision and touch the root; if content and its
timestamp remain identical, the ETags can remain identical as well.

Missing timestamps on newly added nodes and nested POI replacements are filled deterministically
using `CreatedAt`, preserving supplied timestamps; a supplied creation/change timestamp supplies
its missing counterpart for nested values. No replica's local clock supplies hashed defaults.
Receiver-side old-value checks compare only static data. Runtime current-status preconditions
belong to the separate runtime API. Content hashes establish data agreement; signatures and
authorization establish which sender may request that transition. History/replay persistence is
the application's responsibility.

Preparation and application hash the complete stored static hierarchy; this is an additional
whole-hierarchy cost alongside the persistent map updates. Snapshot ETags are computed once
on first access and cached safely for concurrent readers.

## Merging concurrent batches

Use `source.TryMerge(left, right, out mergedChangeSet, out result, ...)` on the **common source**
snapshot. The network convenience method delegates to `network.DataSnapshot`. The method returns
true when the requested check/preparation succeeds. In the default preview mode, a true return
still leaves `mergedChangeSet == null`: inspect `result.Status`, `RequiresExplicitMerge` and
`Message`, then let the application/user explicitly choose whether to prepare the merge.

```csharp
var left = snapshot.CreateChangeSet("branch-a", timestampA,
    [RoamingNetworkChange.UpdateProperty(
        "EVSE", "DE*ABC*E1", "maxPower", oldPower,
        JsonSerializer.SerializeToElement("150 kW"))]);
var right = snapshot.CreateChangeSet("branch-b", timestampB,
    [RoamingNetworkChange.UpdateProperty(
        "EVSE", "DE*ABC*E1", "physicalReference", null,
        JsonSerializer.SerializeToElement("Bay 7"))]);

if (snapshot.TryMerge(left, right, out _, out var preview))
    Console.WriteLine(preview.Message);

// Explicit preparation after the application/user accepts that notice:
if (snapshot.TryMerge(left, right, out var merged, out var report,
                      merge: true, mergedChangeSetId: "merge-a-b"))
{
    var combined = snapshot.ApplyChangeSet(merged!);
    // Revision increases once. BeforeETags = common source; AfterETags = combined result.
}
else
{
    foreach (var issue in report.Issues)
        Console.WriteLine($"{issue.ChangeSetId} / {issue.OperationIndex}: {issue.Message}");
}
```

### Checks and result

1. Apply each incoming batch independently against the source on unpublished immutable maps.
   Network/revision, both source/result ETag pairs, signatures, operations and domain constraints
   must pass. An invalid batch gives `InvalidInput`. Signed inputs require `verifySignature(batch, signature)`
   for every peer; one failure rejects the merge.
2. Execute left then right, and right then left, preserving each batch's operation order and
   original old-value preconditions. Both local candidates use one fixed merge timestamp.
3. Compare entity membership, ancestry, children and **all** stored properties using
   `JsonElement.DeepEquals`. Storage contains only static data; runtime updates are outside this merge.
   Failed operations or different outcomes give `Conflicts`.
4. Preview returns `MergeAvailable`, `RequiresExplicitMerge == true` and no batch. Explicit
   preparation (`merge: true`) additionally requires a nonempty new `mergedChangeSetId`, distinct
   from both input IDs. It returns `Merged` and a new unsigned, content-bound ChangeSet.

`result.Issues` is immutable. Failed operations carry the original `ChangeSetId` and zero-based
`OperationIndex`, plus the entity/property when available; their message identifies the attempted
order. Differing results identify the entity and affected property or structural relation.
No failure returns a partial batch or edits the source. `CanMerge` describes success for the
requested mode; an invalid requested ID gives `InvalidInput` even if the operations are compatible.

### Conflict granularity and timestamps

Updates of distinct properties on the same entity can merge. Differing unconditional writes to
the same property produce different outcomes and are rejected. A deletion versus a descendant
update/addition, duplicate additions, wrong ancestry, stale old values or broken tariff references
fail operation validation. Connector scope uses domain key equality, so two local connector IDs
under different EVSEs are independent.

Explicit element operations use owner paths and stable IDs, allowing independent collection edits
and distinct properties within one nested object to commute. Identity arrays touched by those
operations use deterministic ordinal wire-ID order. Failed original operations include their
`ElementPath` in the merge report.

Whole-value replacements of `energyMeters`, `gridConnectionPoint`, `elements`, `customData` or a
multilingual `name` are still not recursively combined. This is a
conservative merge of unchanged operations: no precondition rebasing, operation deduplication,
winning-writer policy or automatic conflict resolution occurs. Even two identical conditional
writes can conflict when the second still expects the original old value. A combination requiring
one particular order is reported rather than silently reordered.

The timestamp defaults deterministically to the later input `CreatedAt`; pass `createdAt` to
choose another fixed timestamp. Preview and preparation should use the same timestamp. Changed
entities/ancestors use it, and omitted metadata on additions/nested replacements defaults to it.
Explicitly supplied creation timestamps are preserved. A precondition depending on an input
batch's timestamp can therefore fail under a different merge timestamp and is reported.

The output concatenates left operations then right operations, keeps the common source revision
and `BeforeETags`, and calculates fresh JSON/CBOR `AfterETags` from the combined candidate. It has
no signature: verified source signatures authenticate the source batches, not this new header and
result. The merged batch starts with empty descriptions/metadata and signatures; add its own commit
metadata and append new signatures using `Sign` when required. Original commit metadata remains
on the input batches retained by the application. Swapping the arguments yields
the same checked content at the same merge timestamp, although the operation order and batch
identity can differ.

Apply the merged batch to the common source, yielding revision `source.Revision + 1`. It is not
a patch for either already-applied branch successor, and there is no implicit current-head update.
`RoamingNetworkHistory` retains both original batches and explicit additional-parent relationships
and coordinates expected-head publication. The output batch itself has no ancestry graph; its
commit envelope supplies that history. A common-source merge cannot be published on an already
advanced branch head; integration must prepare a new batch against that head. See
[history and ancestry](HISTORY.md#revision-and-ancestry). `RoamingNetworkHistory.TryMerge` now
prepares that new batch using retained ancestor/left/right static states and explicit structured
resolutions; its commit has `[leftId, rightId]` parents and signed merge audit metadata. See
[three-way integration](MERGING.md). Direct runtime
updates after snapshot capture remain outside this check. Network application carries those live
histories into the new version using its existing runtime-state rules.

## Editable fields and persistence

The complete allowlist is in
[`InfrastructureChangeSchema`](../WWCP_POI/ChangeSets/InfrastructureChangeSchema.cs).
Common editable fields include name, description, source and custom data.
Additional fields depend on the entity type.

IDs, parent links, child collections, `created`, `lastChange`, `revision`, `appliedChangeSetId`
and derived `ETags` are managed through operations and cannot be changed as top-level properties.
Nested owner properties are validated as complete replacements.


### Current editable-property reference

All node types except connectors accept the shared fields:
`name`, `description`, `dataSource`, `customData`.
The following fields are additional; connectors use only their listed fields.

| Entity | Additional editable JSON properties |
| --- | --- |
| RoamingNetwork | `dataLicenses`, `dataLicenseIds` |
| ChargingStationOperator | `address`, `logos`, `homepage`, `hotline`, `brands`, `dataLicenses`, `dataLicenseIds` |
| EMobilityProvider | `address`, `logos`, `homepage`, `hotline`, `priority`, `dataLicenses`, `dataLicenseIds` |
| ChargingPool | `address`, `geoLocation`, `locationType`, `accessibility`, `authenticationModes`, `hotlinePhoneNumber`, `openingTimes`, `timeZone`, `chargingWhenClosed`, `locationLanguages`, `facilities`, `services`, `relatedLocations`, `mobilityRootCAs`, `evRoamingPartners`, `brands`, `dataLicenses`, `dataLicenseIds`, `energyMeters`, `gridConnectionPoint`, `maxCurrent`, `maxPower`, `maxCapacity` |
| ChargingStation | `address`, `geoLocation`, `authenticationModes`, `hotlinePhoneNumber`, `openingTimes`, `isFreeOfCharge`, `chargingWhenClosed`, `accessibility`, `locationLanguage`, `physicalReference`, `paymentOptions`, `features`, `vehicleTypes`, `images`, `serviceIdentification`, `modelCode`, `published`, `disabled`, `mobilityRootCAs`, `evRoamingPartners`, `certificationInfo`, `calibrationInfo`, `brands`, `dataLicenses`, `dataLicenseIds`, `energyMeters`, `maxCurrent`, `maxPower`, `maxCapacity` |
| EVSE | `physicalReference`, `geoLocation`, `brand`, `isFreeOfCharge`, `chargingModes`, `currentType`, `maxVoltage`, `maxCurrent`, `maxPower`, `maxCapacity`, `energyMeter`, `photoURLs`, `mobilityRootCAs`, `energyMix`, `calibrationInfo`, `dataLicenses`, `dataLicenseIds`, `tariffIds` |
| ChargingTariff | `elements`, `currency`, `brand`, `uri`, `energyMix` |
| ChargingConnector | `type`, `cable`, `lockable`, `tariffIds`, `termsAndConditions` |

The allowlist controls which fields may be addressed; the entity's parser still controls accepted
values. Being listed does not make a mandatory field nullable or permit an invalid nested value.

Pool/station electrical limits use `Ampere`, `Watt` and `WattHour` in the domain and explicit
SI strings in operation values. Equivalent unit strings are normalized for preconditions and
storage; zero is preserved and null clears an optional limit. Negative limits and numeric or
unitless values fail validation. Real-time limits/prognoses remain outside the editable allowlist.

Persist the static version and current statuses with `ToJSONSnapshot()`, or only the static version
with `DataSnapshot.WriteTo()`. The snapshot stores only the latest `AppliedChangeSetId`; it does
not retain the batches that created earlier versions. Use `RoamingNetworkHistory` for retained
commits, branch snapshots, duplicate detection and expected-head publication. Its static JSON/CBOR
archives retain the original batches and all peers; `CreatePersistent`/`Open` provide integrated
file persistence and replay recovery. [History contracts](HISTORY.md) define first-parent revisions
and the separation of static state tags, commit identity and peer signatures.

See [JSON](JSON.md) for ChangeSet serialization, [signatures](SIGNATURES.md) for verified batches
and [domain details](../WWCP_POI/ChangeSets/README.md) for tariffs and transparency software.
The [element guide](ELEMENT-OPERATIONS.md) defines nested relations, metadata and runtime lifetimes.
