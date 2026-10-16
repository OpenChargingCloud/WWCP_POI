# ChargingTariffGroup

A charging tariff group names a set of [ChargingTariff](ChargingTariff.md)s of one charging station operator,
e.g. all tariffs of a product line or a region. Groups may overlap; a tariff may be a member of any number of
groups. A group has no prices of its own and, unlike EVSE, charging station and charging pool groups, no
`chargingTariffId`.

A group is embedded completely in its [ChargingStationOperator](ChargingStationOperator.md) under
`chargingTariffGroups` and belongs to the operator its identification names. That operator owns it and signs
every change of it.

The identification is a `ChargingTariffGroup_Id`: the operator identification, `*TG` and a suffix of 1..50
characters (`[A-Za-z0-9_]` first, then also `*`, `-`, `.`, `€`, `$`), e.g. `DE*GEF*TG1`. When reading, the `*`
before `TG` is optional. Ids are unique within the operator and compared case-insensitively.

| | |
|---|---|
| Class | `ChargingTariffGroup` — [source](../WWCP_POI3/RoamingNetwork/ChargingTariff/Group/ChargingTariffGroup.cs) |
| Identification | `ChargingTariffGroup_Id`, e.g. `DE*GEF*TG1` |
| Admin status | `ChargingTariffGroupAdminStatusType`, initially `operational` |
| Status | `ChargingTariffGroupStatusType`, initially `active` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `chargingTariffGroups` |
| Owner | the charging station operator named by the identification (its signing keys) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTGs/{CTG_Id}`; embedded in its charging station operator, `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}`, and listed by `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTGs` |


## Common properties

See [README.md](README.md#common-properties). No `eTags`.

| JSON key | Change set | Notes |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | I18N text; omitted when empty |
| `description` | update/remove | I18N text; omitted when empty |
| `dataSource` | update/remove | |
| `created` | — | set to the `createdAt` of the change set when added, unless given |
| `lastChange` | — | set to the `createdAt` of the change set by every change |
| `customData` | update/remove | a JSON object; omitted when empty |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `chargingTariffIds` | `ChargingTariffIds` | `ChargingTariff_Id`[] | update/remove | Its members: tariffs of the same operator, ordered by identification (ordinal), each at most once. Omitted when empty; a group may have no members. |
| `priority` | `Priority` | `Int32?` | update/remove | Which of the groups of a member decides where they give it different values: the higher wins; 0 unless given. An integer, neither a text nor a fraction; see [group priority](README.md#group-priority-and-effective-charging-stations). |


## chargingTariffIds

References, not embedded tariffs: every identification must name a [ChargingTariff](ChargingTariff.md) the
operator embeds in `chargingTariffs`. A member still named by a group can not be removed; remove it from the
group first, in an earlier change of the same change set or an earlier one.

`UpdateProperty(group, "chargingTariffIds", oldValue, new JArray("DE*GEF*T1", "DE*GEF*T2"))` replaces the
members as a whole, or [element by element](README.md#element-changes); `RemoveProperty(group, "chargingTariffIds")` leaves none.


## Change sets

- `RoamingNetworkChange.Add(group)` — no parent needed, it is added to the operator its identification names.
- `RoamingNetworkChange.Remove(groupId)` — nothing references a tariff group.
- `UpdateProperty(group, …)` / `RemoveProperty(group, …)` of `name`, `description`, `dataSource`,
  `chargingTariffIds`, `customData`.
- Signed with a signing key of the operator.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `ChargingTariffGroup`. Its status is set manually,
or calculated by a delegate from the status of its members, see [Calculated status of groups](README.md#calculated-status-of-groups).

- **Admin status** `ChargingTariffGroupAdminStatusType`: `unknown`, `unspecified`, `planned`, `inDeployment`,
  `operational`, `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `ChargingTariffGroupStatusType`: `unknown`, `unspecified`, `planned`, `active`, `expired`.
  Initially `active` since `created`.
- No energy status.
- Events: `OnAdminStatusChanged` (`OnChargingTariffGroupAdminStatusChangedDelegate`) and `OnStatusChanged`
  (`OnChargingTariffGroupStatusChangedDelegate`).


## Validation

- `@id` mandatory, a valid `ChargingTariffGroup_Id`; its operator must be the embedding operator; unique within the operator.
- `chargingTariffIds`, when given, a JSON array of valid `ChargingTariff_Id` texts, none given twice.
- Every member is a tariff of the same operator.


## Example

```json
{
  "@id": "DE*GEF*TG1",
  "name": { "en": "AC tariffs" },
  "chargingTariffIds": [ "DE*GEF*T1", "DE*GEF*T2" ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-01-01T00:00:00.000Z"
}
```
