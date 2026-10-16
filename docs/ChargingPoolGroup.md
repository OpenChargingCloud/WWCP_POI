# ChargingPoolGroup

A charging pool group is a named set of charging pools of one charging station operator, e.g. all pools of a city,
a site partner or a motorway, optionally with a charging tariff for all of them. Groups may overlap: a pool may be
a member of several groups, or of none.

It is embedded completely in its [ChargingStationOperator](ChargingStationOperator.md) under `chargingPoolGroups`,
in the ordinal order of the identifications. It belongs to the operator its identification names, names only
pools and a tariff of that operator, and only that operator changes it, with one of its signing keys.

Its identification is the operator's identification, `*PG` and a suffix, e.g. `DE*GEF*PGJENA`, unique among the
charging pool groups of the operator.

| | |
|---|---|
| Class | `ChargingPoolGroup` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationOperator/ChargingPool/Group/ChargingPoolGroup.cs) |
| Identification | `ChargingPoolGroup_Id`, `^([A-Za-z]{2}\*?[A-Za-z0-9]{3})\*?PG([a-zA-Z0-9_][a-zA-Z0-9_\*\-\.€\$]{0,49})$`, e.g. `DE*GEF*PGJENA`; always written as `<operator>*PG<suffix>` |
| Admin status | `ChargingPoolGroupAdminStatusType`, initially `Operational` |
| Status | `ChargingPoolGroupStatusType`, initially `Available` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `chargingPoolGroups` |
| Owner | the charging station operator its identification names (a signing key) |


## Common properties

See [Common properties](README.md#common-properties).

| JSON key | Change set | Note |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | Optional, no default. |
| `description` | update/remove | |
| `dataSource` | update/remove | |
| `created` | — | Set to the change set's `createdAt` when added without one. |
| `lastChange` | — | Set to the change set's `createdAt` when added and by every change of the group. |
| `customData` | update/remove | |

The group writes no `eTags` and no `@context`.


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `chargingPoolIds` | `ChargingPoolIds` | `ImmutableArray<ChargingPool_Id>` | update/remove | The members: [charging pools](ChargingPool.md) of the same operator, in the ordinal order of their identifications, each once. Written only when not empty; a group may be empty. |
| `chargingTariffId` | `ChargingTariffId` | `ChargingTariff_Id?` | update/remove | A [charging tariff](ChargingTariff.md) of the same operator for all members, unless their own apply. |
| `priority` | `Priority` | `Int32?` | update/remove | Which of the groups of a member decides where they give it different values: the higher wins; 0 unless given. An integer, neither a text nor a fraction; see [group priority](README.md#group-priority-and-effective-charging-stations). |


## Members

The members are references, not embedded: the pools stay in the operator's `chargingPools`. A change set changes
the membership by updating `chargingPoolIds` as a whole, e.g.
`RoamingNetworkChange.UpdateProperty(group, "chargingPoolIds", oldIds, newIds)`, in ordinal order, or [element by element](README.md#element-changes). A pool can not be
removed while a group names it: remove it from the group first, in the same or an earlier change set.

The group itself is added with `RoamingNetworkChange.Add(chargingPoolGroup)`, as `ToJSON()` writes it, and removed with
`RoamingNetworkChange.Remove(chargingPoolGroupId)`; a given parent must be its operator.


## Runtime status

See [Runtime status](README.md#runtime-status). A group has an admin status and a status, no energy status; both
initially as of its creation. Its status is set manually, or calculated by a delegate from the status of its members, see [Calculated status of groups](README.md#calculated-status-of-groups).

| | Type | Initial | Predefined values |
|---|---|---|---|
| Admin status | `ChargingPoolGroupAdminStatusType` | `Operational` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` |
| Status | `ChargingPoolGroupStatusType` | `Available` | `unknown`, `unspecified`, `planned`, `inDeployment`, `offline`, `available`, `partialAvailable`, `reserved`, `charging`, `outOfService`, `error` |

Events: `OnAdminStatusChanged` (`OnChargingPoolGroupAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnChargingPoolGroupStatusChangedDelegate`), with the old and new timestamped status. In a history,
`history.RuntimeStatus` sets and reads them as entity type `ChargingPoolGroup`.


## Validation

- **Identification:** a valid `ChargingPoolGroup_Id` naming the operator that embeds it; no other charging pool
  group of the operator has it.
- **Members:** no pool twice; every member a charging pool of the operator.
- **Tariff:** `chargingTariffId`, if given, a charging tariff of the operator.
- **Change sets:** `UpdateProperty`/`RemoveProperty` only of `name`, `description`, `dataSource`, `chargingPoolIds`,
  `chargingTariffId` and `customData`. After every change the whole operator is read again, so the rules above hold
  for the result; a pool or tariff the group names can not be removed.


## Example

```json
{
  "@id": "DE*GEF*PGJENA",
  "name": { "en": "Jena", "de": "Jena" },
  "description": { "en": "All charging pools within the city of Jena" },
  "chargingPoolIds": [ "DE*GEF*P1", "DE*GEF*P2" ],
  "chargingTariffId": "DE*GEF*TAC1",
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-10-12T12:00:00.000Z"
}
```
