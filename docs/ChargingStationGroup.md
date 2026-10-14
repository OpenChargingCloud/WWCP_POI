# ChargingStationGroup

A charging station group names [charging stations](ChargingStation.md) of one charging station operator, e.g.
all stations of a model, of a site type or of a contract, and may name one charging tariff for all of them. It
does not embed its members, it names them: groups may overlap, and a station may be in no group or in several.
It is embedded in its [ChargingStationOperator](ChargingStationOperator.md), under `chargingStationGroups`.

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

Absent values are left out; an empty `chargingStationIds` is not written.


## chargingStationIds

`chargingStationIds`: the members, each once, in the ordinal order of their identifications. Every one must be
a [charging station](ChargingStation.md) of the same operator, in any of its pools; a station of another
operator is unknown there and refused. The array is updated or removed as a whole, e.g.
`RoamingNetworkChange.UpdateProperty(group, "chargingStationIds", old, new)`. A station can not be removed
while a group names it: update the group first, in the same change set.


## Adding and removing

A charging station group is a child of its operator, added and removed as an entity:

```csharp
RoamingNetworkChange.Add   (chargingStationGroup)      // the parent is the operator its identification names
RoamingNetworkChange.Remove(chargingStationGroupId)
```

Nothing else names a charging station group, so it can always be removed.


## Runtime status

See [runtime status](README.md#runtime-status) for the mechanism. The group's status is set on its own; it is
not calculated from the status of its members.

- **Admin status** `ChargingStationGroupAdminStatusType`, initially `Operational` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `OutOfService` (`outOfService`),
  `Operational` (`operational`), `InternalUse` (`internalUse`).
- **Status** `ChargingStationGroupStatusType`, initially `Available` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `OutOfService` (`outOfService`), `Offline` (`offline`), `Available` (`available`),
  `PartialAvailable` (`partialAvailable`), `Reserved` (`reserved`), `Charging` (`charging`),
  `Faulted` (`faulted`), `Other` (`other`).
- **Events:** `OnAdminStatusChanged`, `OnStatusChanged` `(timestamp, eventTrackingId, chargingStationGroup, oldStatus, newStatus)`.

In a history it is set and read by `history.RuntimeStatus` with the entity type `ChargingStationGroup`. It has
no energy status.


## Validation

- `@id` is mandatory and a valid charging station group identification.
- `chargingStationIds` must not name a station twice.
- In its operator: the group belongs to the operator its identification names; a group must not be given
  twice; every member must be a charging station of the operator; `chargingTariffId` must be a tariff of the operator.
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
  "created":            "2026-01-01T00:00:00.000Z",
  "lastChange":         "2026-10-11T12:00:00.000Z"
}
```
