# ChargingStationGroup

A charging station group names [charging stations](ChargingStation.md) of one charging station operator, e.g.
all stations of a model, of a site type or of a contract, and may name one charging tariff for all of them. It
does not embed its members, it names them: groups may overlap, and a station may be in no group or in several.
It is embedded in its [ChargingStationOperator](ChargingStationOperator.md), under `chargingStationGroups`.

It may give its members any property of a charging station but those that are a station's own (its identification,
name, description, EVSEs and energy meters): e.g. the model, the hotline and the features of all stations of a model,
given once. The network keeps them in the group; the stations take them at runtime, as the
[effective charging stations](#properties-of-its-members) of their operator.

The group belongs to the charging station operator its identification names: `DE*GEF*SG1` belongs to `DE*GEF`,
and every change of it, and every `Add`/`Remove` of it, must be signed with a signing key of that operator.
The identification is `{operatorId}*SG{suffix}`, the suffix up to 50 characters, starting with a letter, digit
or `_`, followed by letters, digits and `_ * - . € $`, e.g. `DE*GEF*SG1`. It is unique within the operator;
two identifications are equal when they differ only in case.

| | |
|---|---|
| Class | `ChargingStationGroup` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStation/Group/ChargingStationGroup.cs) |
| Identification | `ChargingStationGroup_Id`, e.g. `DE*GEF*SG1` |
| Admin status | `ChargingStationGroupAdminStatusType`, initially `Operational` |
| Status | `ChargingStationGroupStatusType`, initially `Available` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `chargingStationGroups` |
| Owner | the [ChargingStationOperator](ChargingStationOperator.md) its identification names (signing key) |


## Common properties

See [common properties](README.md#common-properties). A charging station group does not write `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — |
| `lastChange` | — (set to the change set's `createdAt`) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `chargingStationIds` | `ChargingStationIds` | `ImmutableArray<ChargingStation_Id>` | update/remove | Its members; see [chargingStationIds](#chargingstationids). |
| `chargingTariffId` | `ChargingTariffId` | `ChargingTariff_Id?` | update/remove | The [charging tariff](ChargingTariff.md) of its operator for all its members, unless their own apply; it must exist at the operator. |
| `priority` | `Priority` | `Int32?` | update/remove | Which of the groups of a member decides where they give it different values: the higher wins; 0 unless given. An integer, neither a text nor a fraction; see [group priority](README.md#group-priority-and-effective-charging-stations). |

The properties of a charging station it gives its members, each written and read as a
[charging station](ChargingStation.md#properties) writes and reads it, and each updated or removed by a change set:

| JSON key | C# property | Its members take it |
|---|---|---|
| `physicalReference` | `PhysicalReference` | unless they have their own |
| `address` | `Address` | unless they have their own |
| `geoLocation` | `GeoLocation` | unless they have their own |
| `hotline` | `HotlinePhoneNumber` | unless they have their own |
| `manufacturerId` | `ManufacturerId` | unless they have their own; a manufacturer of the network |
| `modelId` | `ModelId` | unless they have their own |
| `openingTimes` | `OpeningTimes` | unless they have their own |
| `accessibility` | `Accessibility` | unless they have their own |
| `calibrationInfo` | `CalibrationInfo` | unless they have their own |
| `certificationInfo` | `CertificationInfo` | unless they have their own |
| `maxPower` | `MaxPower` | unless they have their own; not above the one of all EVSEs of a member together |
| `brands` | `Brands` | besides their own; of the same identification their own |
| `images` | `Images` | besides their own; of the same URL their own |
| `vehicleTypes` | `VehicleTypes` | besides their own |
| `euVehicleCategories` | `EUVehicleCategories` | besides their own |
| `features` | `Features` | besides their own |
| `parkingSpaceIds` | `ParkingSpaceIds` | besides their own; parking spaces of the network |

Absent values are left out; an empty `chargingStationIds` is not written, nor an empty array of the properties above.


## chargingStationIds

`chargingStationIds`: the members, each once, in the ordinal order of their identifications. Every one must be
a [charging station](ChargingStation.md) of the same operator, in any of its pools; a station of another
operator is unknown there and refused. The array is updated or removed as a whole, e.g.
`RoamingNetworkChange.UpdateProperty(group, "chargingStationIds", old, new)`, or [element by element](README.md#element-changes). A station can not be removed
while a group names it: update the group first, in the same change set.


## Properties of its members

A charging station takes the properties its groups give it at runtime: its **effective charging station**, which
its operator calculates once per version. Of every property above:

- a single value is the station's own, else the one of its group with the highest `priority` giving one,
- an array is the station's own united with those of all its groups; of brands with the same identification and
  images with the same URL the station's own wins, else the one of the group with the higher priority.

Groups of a station with the same priority must not give it different values where these decide what it takes,
e.g. two hotlines while it has none of its own, or two brands of the same identification; nor must they make it
invalid, e.g. by a `maxPower` above the one of all its EVSEs together. Such an operator is refused, and so is a
change set making it so, already by `CreateChangeSet`. The effective charging station has the runtime status, the
EVSEs and the energy meters of the station, and as `lastChange` the latest of the station and its groups giving
properties.

```csharp
chargingStationOperator.TryGetEffectiveChargingStation(stationId, out var effectiveStation);
chargingStationOperator.EffectiveChargingStation(station);       // the station itself, when its groups give it nothing
chargingStationOperator.EffectiveChargingStations;               // all of them
chargingStationOperator.EffectiveChargingPools;                  // the pools with them
chargingStationOperator.ChargingStationGroupsOf(stationId);      // its groups
```

POI_API serves a charging station, and a charging pool with its stations, as they are at runtime; `?view=stored`
serves them as the operator embeds them, with the ETag a [precondition](README.md#etags) names. The OCPI and DATEX II
publishers publish the effective charging stations. The roaming network, the operator and their ETags, change sets
and merges keep the properties where they are given.


## Adding and removing

A charging station group is a child of its operator, added and removed as an entity:

```csharp
RoamingNetworkChange.Add   (chargingStationGroup)      // the parent is the operator its identification names
RoamingNetworkChange.Remove(chargingStationGroupId)
```

Nothing else names a charging station group, so it can always be removed.


## Runtime status

See [runtime status](README.md#runtime-status) for the mechanism. The group's status is set manually, or
calculated by a delegate from the status of its members, see [Calculated status of groups](README.md#calculated-status-of-groups).

- **Admin status** `ChargingStationGroupAdminStatusType`, initially `Operational` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Operational` (`operational`), `InternalUse` (`internalUse`), `OutOfService` (`outOfService`).
- **Status** `ChargingStationGroupStatusType`, initially `Available` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Offline` (`offline`), `Available` (`available`), `PartialAvailable` (`partialAvailable`), `Reserved` (`reserved`),
  `Charging` (`charging`), `OutOfService` (`outOfService`), `Error` (`error`).
- **Events:** `OnAdminStatusChanged`, `OnStatusChanged` `(timestamp, eventTrackingId, chargingStationGroup, oldStatus, newStatus)`.

In a history it is set and read by `history.RuntimeStatus` with the entity type `ChargingStationGroup`. It has
no energy status.


## Validation

- `@id` is mandatory and a valid charging station group identification.
- `chargingStationIds` must not name a station twice.
- `priority` must be an integer; `maxPower` must not be negative; `brands`, `vehicleTypes`, `euVehicleCategories`,
  `features` and `parkingSpaceIds` must not name a value twice, an image URL must not be given twice; `brandIds`
  are refused.
- In its operator: the group belongs to the operator its identification names; a group must not be given
  twice; every member must be a charging station of the operator; `chargingTariffId` must be a tariff of the operator;
  every effective charging station must be valid, and its groups of the same priority must agree, see
  [properties of its members](#properties-of-its-members).
- In its roaming network: `manufacturerId` must name a manufacturer and `parkingSpaceIds` parking spaces of the network.
- A change set applies each change to the embedded JSON of the operator and reads the operator again, so these
  rules hold after every single change.
- `Add(chargingStationGroup)` refuses a parent other than the operator; the group must not exist yet. It gets the
  change set's `createdAt` as `lastChange`, and as `created` unless given.


## Example

```json
{
  "@id":                "DE*GEF*SG1",
  "name":               { "en": "ACME Wallbox-22" },
  "chargingStationIds": [ "DE*GEF*S1", "DE*GEF*S2" ],
  "chargingTariffId":   "DE*GEF*T3",
  "priority":           10,
  "manufacturerId":     "ACME",
  "modelId":            "Wallbox-22",
  "hotline":            "+49 800 4242424",
  "features":           [ "StatusInfoAvailable" ],
  "maxPower":           22000,
  "created":            "2026-01-01T00:00:00.000Z",
  "lastChange":         "2026-10-11T12:00:00.000Z"
}
```
