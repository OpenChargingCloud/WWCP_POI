# ChargingPoolGroup

A charging pool group names [charging pools](ChargingPool.md), e.g. all pools of a city, of a site partner or along a
motorway. It gives them nothing: what a pool is, it has of its own and of its [templates](ChargingPoolTemplate.md).
Groups are for sets that do not follow from properties: for a status of their own, for searches, or to name the EVSEs
of a [charging tariff offer](ChargingTariffOffer.md). Groups may overlap: a pool may be a member of several groups, or
of none.

Two parties have charging pool groups, under `chargingPoolGroups`, in the ordinal order of the identifications:

- a [ChargingStationOperator](ChargingStationOperator.md), of its own charging pools;
- an [EMobilityProvider](EMobilityProvider.md), of charging pools of any operator, e.g. for its offers.

It belongs to the party embedding it, and only that party changes it, with one of its signing keys. Its
identification is the party's identification, `*PG` and a suffix, e.g. `DE*GEF*PGJENA`, unique in the roaming
network.

| | |
|---|---|
| Class | `ChargingPoolGroup` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationOperator/ChargingPool/Group/ChargingPoolGroup.cs) |
| Identification | `ChargingPoolGroup_Id`, `^([A-Za-z]{2}\*?[A-Za-z0-9]{3})\*?PG([a-zA-Z0-9_][a-zA-Z0-9_\*\-\.€\$]{0,49})$`, e.g. `DE*GEF*PGJENA`; always written as `<party>*PG<suffix>` |
| Admin status | `ChargingPoolGroupAdminStatusType`, initially `Operational` |
| Status | `ChargingPoolGroupStatusType`, initially `Available` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) or [EMobilityProvider](EMobilityProvider.md) as `chargingPoolGroups` |
| Owner | the party embedding it (a signing key) |


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
| `chargingPoolIds` | `ChargingPoolIds` | `ImmutableArray<ChargingPool_Id>` | update/remove | The members, in the ordinal order of their identifications, each once. Written only when not empty; a group may be empty. |


## Members

The members are references, not embedded: the pools stay in the `chargingPools` of their operator. A change set
changes the membership by updating `chargingPoolIds` as a whole, e.g.
`RoamingNetworkChange.UpdateProperty(group, "chargingPoolIds", oldIds, newIds)`, in ordinal order, or
[element by element](README.md#element-changes).

- **A group of an operator** names pools of that operator. A pool can not be removed while a group of its operator
  names it: remove it from the group first, in the same or an earlier change set.
- **A group of an e-mobility provider** names pools of any operator, by weak references: a pool that does not exist
  (any more) is left out, and its operator removes it without the provider.

The group itself is added with `RoamingNetworkChange.Add(chargingPoolGroup, partyId)`, as `ToJSON()` writes it, and
removed with `RoamingNetworkChange.Remove(chargingPoolGroupId)`. A group a [charging tariff offer](ChargingTariffOffer.md)
of the same party names can not be removed: update the offer first.


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

- **Identification:** a valid `ChargingPoolGroup_Id` naming the party that embeds it; no other charging pool group
  of the roaming network has it.
- **Members:** no pool twice; of a group of an operator, every member a charging pool of the operator.
- **Change sets:** `UpdateProperty`/`RemoveProperty` only of `name`, `description`, `dataSource`, `chargingPoolIds`
  and `customData`. After every change the whole party is read again, so the rules above hold for the result.


## Example

```json
{
  "@id": "DE*GEF*PGJENA",
  "name": { "en": "Jena", "de": "Jena" },
  "description": { "en": "All charging pools within the city of Jena" },
  "chargingPoolIds": [ "DE*GEF*P1", "DE*GEF*P2" ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-10-12T12:00:00.000Z"
}
```
