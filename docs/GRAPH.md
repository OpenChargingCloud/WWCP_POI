# Owned graph nodes and references

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [Element operations](ELEMENT-OPERATIONS.md)

## Ownership

The versioned graph includes 22 independently addressed node types. Each node has one owner;
only the roaming network is a root. Owning collections use expanded objects in complete network
JSON/CBOR. Group membership, tariff assignments and parking links use identifier strings.

| Owner | Owned collection | Node type | Identity field |
| --- | --- | --- | --- |
| Network | `chargingStationOperators` | ChargingStationOperator | `@id` |
| Network | `eMobilityProviders` | EMobilityProvider | `@id` |
| Network | `chargingStationManufacturers` | ChargingStationManufacturer | `@id` |
| Network | `gridOperators` | GridOperator | `id` |
| Network | `parkingOperators` | ParkingOperator | `id` |
| Network | `transparencySoftware` | TransparencySoftware | `@id` |
| Network | `transparencySoftwareCertificates` | TransparencySoftwareCertificate | `@id` |
| ChargingStationOperator | `chargingPools` | ChargingPool | `@id` |
| ChargingStationOperator | `chargingTariffs` | ChargingTariff | `@id` |
| ChargingStationOperator | `EVSEGroups` | EVSEGroup | `@id` |
| ChargingStationOperator | `chargingStationGroups` | ChargingStationGroup | `@id` |
| ChargingStationOperator | `chargingPoolGroups` | ChargingPoolGroup | `@id` |
| ChargingStationOperator | `chargingTariffGroups` | ChargingTariffGroup | `@id` |
| ChargingPool | `chargingStations` | ChargingStation | `@id` |
| ChargingStation | `EVSEs` | EVSE | `@id` |
| EVSE | `socketOutlets` | ChargingConnector | `@id`, scoped to its EVSE |
| ParkingOperator | `parkingGarages` | ParkingGarage | `@id` |
| ParkingOperator | `parkingSpaces` | ParkingSpace | `@id` |
| ParkingOperator | `parkingSensors` | ParkingSensor | `@id` |
| ParkingOperator | `parkingSpaceGroups` | ParkingSpaceGroup | `@id` |
| ParkingOperator | `parkingProducts` | ParkingProduct | `@id` |

The root uses `@id`. All other graph identities are unique per node type within a network,
except connectors. Domain comparison rules apply: group suffixes and parking child IDs are
case-sensitive, as are software/certificate/product IDs; manufacturer and parking operator IDs are case-insensitive. A group's embedded
operator identity must match its owner. Each group type has its own wire ID prefix: `*EG` for EVSE,
`*SG` for charging station, `*PG` for charging pool and `*TG` for charging tariff groups: the entity's
letter first, then `G`. WWCP Core still writes `*GE`, `*GS` and `*GP`.

Import grid operators, manufacturers, software and certificates before infrastructure. Import
tariffs and infrastructure before groups, and charging infrastructure before parking operators. Network parsers perform this ordering themselves, regardless of JSON property order.
Materialized groups refer to infrastructure objects in that same version. Public collection access
does not expose registration or in-place static mutation APIs. Build a complete JSON document,
use the available immutable constructors, or add nodes through ChangeSets.

## References and deletion

`RoamingNetworkDataSnapshot.References` is a persistent reverse index from target keys to consumer
keys. `TariffReferences` exposes its tariff subset, including tariff groups and group tariff choices.
Both whole-property and addressed element operations maintain the index incrementally.

| Consumer | Reference | Required target scope |
| --- | --- | --- |
| EVSE/connector | `tariffIds` | Same charging station operator |
| EVSEGroup | `EVSEIds` | Same charging station operator |
| ChargingStationGroup | `chargingStationIds` | Same charging station operator |
| ChargingPoolGroup | `chargingPoolIds` | Same charging station operator |
| ChargingTariffGroup | `chargingTariffIds` | Same charging station operator |
| EVSE/station/pool group | Optional `tariffId` | Same charging station operator |
| Parking garage/space/sensor/space group | `chargingStationIds` | Existing station in the network |
| Parking space/space group | `sensors` | Sensor owned by the same parking operator |
| ParkingOperator | `localParkingSpaceIds`, `invalidParkingSpaceIds` | Space owned by that parking operator |
| ParkingSpace | Optional `parkingGarageId` | Garage owned by the same parking operator |
| ParkingSpaceGroup | `parkingSpaceIds` | Spaces owned by the same parking operator; overlapping groups allowed |
| Parking garage/space/space group | `parkingProductIds` | Products owned by the same parking operator |
| Pool connection point | Required `gridOperatorId` | Registered grid operator in the network |
| Meter legal-status assignment | `transparencySoftwareId`, optional `certificateId` | Registered software release and certificate in the network |
| TransparencySoftwareCertificate | `verifiedTransparencySoftwareIds`, `compatibleTransparencySoftwareIds` | Registered software releases in the network |
| TransparencySoftwareCertificate | Optional `chargingStationManufacturerId` | Registered manufacturer in the network |

Every indexed reference must resolve, and a reference array cannot repeat a domain identity.
A target cannot be removed while surviving consumers reference it. Removing an owned subtree
first removes references originating inside that subtree; references from outside still prevent
deletion. Ordered batches must detach surviving references before removing their targets.

History merge can combine independently valid branches into an invalid reference graph: for example,
one deletes a target while another adds a consumer. It now reports `Reference` with consumer `Entity`,
`PropertyName` and typed `RelatedEntity`, collecting all current missing/out-of-scope targets before
domain projection. Whole-consumer subtree choices are validated again; keeping an invalid reference
does not resurrect the target or waive operator scope. When target recreation blocks scheduling,
the planner can explicitly detach indexed references and restore their selected final values.
The complete proposed steps and their operation indices are visible in the merge report and signed
audit metadata. Every intermediate operation retains normal validation. See [reference conflicts](MERGING.md#reference-conflicts)
and [temporary reference transitions](MERGING.md#explicit-temporary-reference-transitions).

For EVSE/station/pool groups, `allowedMemberIds` is an admission list, not active membership.
It can contain future IDs from the same operator; these IDs do not require target nodes and do
not protect deletion. Every active member must be allowed. Omitting the list defaults it to the
active IDs; an explicitly empty list permits no active members. Parsers reject membership that
would otherwise be silently filtered by constructor predicates. Executable inclusion predicates
are application configuration and are not persisted.

Garages, spaces, sensors, space groups and products remain direct children of the parking
operator. A space optionally references a garage; groups reference spaces and can overlap.
`GetAvailableParkingProducts(spaceId)` returns the deduplicated union of direct space, garage and
containing-group offers. It does not combine prices or choose precedence. See [domain decisions](DOMAIN-MODEL.md).

Nested point/meter references are indexed against their graph owner and containing property
(`gridConnectionPoint`, `energyMeters` or `energyMeter`). Merge reports and temporary detachment
therefore identify that property. Certificate edits revalidate consuming assignments, including
whether the document still covers the selected software release.

## Create and edit a group

This example requires an existing operator `DE*ABC` and EVSE `DE*ABC*E1`.

```csharp
using System.Text.Json;

var opId = ChargingStationOperator_Id.Parse("DE*ABC");
var groupId = EVSEGroup_Id.Parse(opId, "fast").ToString();
var groupDocument = JsonSerializer.SerializeToElement(new Dictionary<String, Object> {
    ["@id"] = groupId,
    ["name"] = new { en = "Fast chargers" },
    ["EVSEIds"] = new[] { "DE*ABC*E1" },
    ["allowedMemberIds"] = new[] { "DE*ABC*E1" }
});
var addGroup = RoamingNetworkChange.Add(
    "EVSEGroup", groupId, groupDocument, "ChargingStationOperator", opId.ToString());
var batch = network.CreateChangeSet("group-1", DateTimeOffset.UtcNow, [addGroup]);
var next = network.ApplyChangeSet(batch);

var detachMember = RoamingNetworkChange.RemoveElement(
    "EVSEGroup", groupId, [new("EVSEIds", "DE*ABC*E1")]);
```

Use graph `Add`, `Remove` and `UpdateProperty` for the new nodes. Use `AddElement`/`RemoveElement`
for the identified reference arrays, including admission lists. Add an admission ID before adding
active membership, and remove active membership before removing its admission ID. A selected
reference string has no object properties to edit. Manufacturer `cryptoKeys` is a whole-property
value; private keys are excluded from snapshot import and rejected in static ChangeSet payloads.

## Editable fields

The schema allowlist follows the persisted domain contract:

| Node | Editable static fields |
| --- | --- |
| EVSE/station/pool group | `name`, `description`, member IDs, `allowedMemberIds`, `brand`, `priority`, `tariffId`, `dataLicenses` |
| Tariff group | `description`, `chargingTariffIds` |
| Manufacturer | `name`, `description`, `cryptoKeys` |
| GridOperator | `name`, `description`, `dataSource`, `customData`, `logos`, `address`, `geoLocation`, `telephone`, `eMailAddress`, `homepage`, `hotline`, `priority`, `dataLicenses` |
| ParkingOperator | `name`, `description`, `dataSource`, `customData`, `logos`, `address`, `geoLocation`, `telephone`, `eMailAddress`, `homepage`, `hotlinePhoneNumber`, `dataLicenses`, local/invalid parking space IDs |
| Parking garage/space/sensor/space group | `name`, `description`, `osmWayId`, `geometry`, `chargingStationIds`; spaces/space groups additionally `sensors`; garages/spaces/groups `parkingProductIds`; space `parkingGarageId`; group `parkingSpaceIds` |
| ParkingProduct | `minDuration`, `stopParkingAfterTime` |
| TransparencySoftware | `name`, `version`, `openSourceLicenses`, `vendor`, `logo`, `howToUse`, `moreInformation`, `sourceCodeRepository` |
| TransparencySoftwareCertificate | `issuer`, `chargingStationModel`, `chargingStationModelVersion`, optional `chargingStationManufacturerId`, `documentNumber`, `documentURL`, validity and verified/compatible software IDs |

Identity, parent, managed timestamp and owned child fields cannot be edited as properties.
Manufacturers, software releases, certificate documents and parking products have no managed
node timestamps; their ancestors still receive the batch timestamp.
Other added graph nodes receive deterministic missing timestamps from the batch's `CreatedAt`.

## Content identifiers and runtime

Network/operator ETags include the owned node documents. Group ETags include group configuration
and member IDs, while each owned member's content is hashed through its infrastructure location.
A change to a member can leave the group's ETag unchanged and still change the network ETags.
Empty owned graph collections export as `[]`. Complete network hashes use stored static properties,
including valid optional nulls, without a domain serialization roundtrip. Graph expansion and the
stored-property profile change earlier content identifiers; recalculate them and prepare/sign new
batches against the current profile. See [snapshot defaults](ETAGS-CBOR.md#versioned-property-presence-and-defaults).

A point stores only `gridOperatorId` and resolves the registry object in its network version.
Many points share one description and one operational schedule. Updating that registry node
changes its own and the network's ETags; a point's ETag can remain unchanged because it contains
an ID reference. Software/certificate references follow the same rule: descriptions are hashed
once at their owned network location. Complete network ETags cover all referenced catalog content.

The `wwcp-poi-static-v2` ownership/reference contract changes state digests and downstream commit
identities. Tagged v1 exports/archives are unsupported; current reference artifacts use v2.

Groups, grid/parking operators and parking children retain mutable operational/admin statuses.
Use `POIRuntimeTarget.Entity(type, id)` for them. Static derivation captures their histories and
capacities into independent objects. Removing and reintroducing a node starts a new lifetime.
Connectors, manufacturers, software, certificates and parking products have no status schedules
and reject runtime targeting.

`ToJSONSnapshot()` and `ToCBOR(IncludeRuntime: true)` can carry their current statuses;
`DataSnapshot` stores none. Standalone group/parking parsers still need explicit membership
context; complete network imports resolve that context automatically. Group validation resolves
the owning operator's infrastructure, and parking validation resolves station context; their
validation work can therefore exceed the cost of a scalar infrastructure-node update.
