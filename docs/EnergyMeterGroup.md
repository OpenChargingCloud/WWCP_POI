# EnergyMeterGroup

An energy meter group collects [energy meters](EnergyMeter.md) of one owner, e.g. all calibration-law meters in the
EVSEs of an operator, or the grid meters of a grid operator. It gives them nothing: what an energy meter is, it has of
its own and of its [templates](EnergyMeterTemplate.md). Groups may overlap: an energy meter can be a member of several
groups. Two parties own energy meter groups:

- a [ChargingStationOperator](ChargingStationOperator.md) groups the energy meters of its charging pools, charging
  stations and EVSEs, under `energyMeterGroups`;
- a [GridOperator](GridOperator.md) groups the energy meters of its
  [grid connection points](GridOperator.md#grid-connection-points), under `energyMeterGroups`, whoever maintains them.

Its owner signs every change to it; for a grid operator without keys, not taking part, the roaming network operator.
Its identification is a free text, compared case-insensitively, e.g. `gef-evse-meters`, unique within the roaming
network (not only within its owner).

| | |
|---|---|
| Class | `EnergyMeterGroup` — [source](../WWCP_POI3/RoamingNetwork/EnergyMeterManufacturer/EnergyMeter/Group/EnergyMeterGroup.cs) |
| Identification | `EnergyMeterGroup_Id`, e.g. `gef-evse-meters` (`NewRandom()` makes a random one) |
| Admin status | `EnergyMeterGroupAdminStatusType`, initially `operational` |
| Status | `EnergyMeterGroupStatusType`, initially `available` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) or [GridOperator](GridOperator.md), as `energyMeterGroups` |
| Owner | its charging station operator, or its grid operator (signing key); for a grid operator without keys the roaming network operator |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EMGs/{EMG_Id}` and `GET ~/RNs/{RN_Id}/GOs/{GO_Id}/EMGs/{EMG_Id}`; embedded in its owner, and listed by `…/EMGs` |


## Common properties

See [Common properties](README.md#common-properties). It writes no `eTags`.

| JSON key | Change set |
|---|---|
| `@id` | — |
| `name` | update/remove |
| `description` | update/remove |
| `dataSource` | update/remove |
| `created` | — (set on `Add` unless given) |
| `lastChange` | — (set to the change set's `createdAt`) |
| `customData` | update/remove |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `energyMeterIds` | `EnergyMeterIds` | `ImmutableArray<EnergyMeter_Id>` | update/remove | Its members, energy meters of the same owner: of a charging station operator those of its charging pools, charging stations and EVSEs, of a grid operator those of its grid connection points; ordered by identification (ordinal), none twice. |

The array is written only when not empty; a group may be empty. A change set names the properties by their
JSON keys (`ChangeableEnergyMeterGroupProperties` in
[RoamingNetwork.EnergyMeterGroupChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.EnergyMeterGroupChanges.cs)):

```csharp
RoamingNetworkChange.Add           (group, chargingStationOperatorId)    // or gridOperatorId
RoamingNetworkChange.UpdateProperty(group, "energyMeterIds", old, new JArray("evse-meter-1", "evse-meter-2"))
RoamingNetworkChange.Remove        (groupId)
```


## Runtime status

Mechanism: [Runtime status](README.md#runtime-status). In a history it is addressed with the entity type
`EnergyMeterGroup`. Its status is set manually, or calculated by a delegate from the status of its members, see
[Calculated status of groups](README.md#calculated-status-of-groups); `POIGroupStatus.OfEnergyMeters` calculates
`available` when all its energy meters are, `partialAvailable` when one of them is, else `offline` when all of them are,
`error` when all of them are `offline` or `error`, `inDeployment` when all of them are, else `unknown`.

| Type | Predefined values | Initial |
|---|---|---|
| `EnergyMeterGroupAdminStatusType` | `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`, `internalUse`, `outOfService` | `operational` since `created` |
| `EnergyMeterGroupStatusType` | `unknown`, `unspecified`, `inDeployment`, `offline`, `available`, `partialAvailable`, `error` | `available` since `created` |

Any other non-empty text parses as well. No energy status. A change of the group or of anything else of its owner
keeps its runtime status.

Events: `OnAdminStatusChanged` (`OnEnergyMeterGroupAdminStatusChangedDelegate`) and `OnStatusChanged`
(`OnEnergyMeterGroupStatusChangedDelegate`), each with `(Timestamp, EventTrackingId, EnergyMeterGroup, OldStatus, NewStatus)`.


## Validation

- `@id` is mandatory, any non-empty text (trimmed); unique within the roaming network, across charging station
  operators and grid operators, compared case-insensitively.
- `energyMeterIds`: none twice, every one an energy meter of its owner. An energy meter still named by a group can not
  be removed.
- Nothing references a group, so it can always be removed.
- `Add` needs its owner as the parent, an existing `ChargingStationOperator` or `GridOperator`, and an object whose
  `@id` is the change's `entityId`; the group must not exist yet. A charging station operator or grid operator is
  added without its groups: they are its own to add (`RoamingNetworkChange.AddChildren`).


## Example

The EVSE meters of a charging station operator:

```json
{
  "@id":            "gef-evse-meters",
  "name":           { "en": "EVSE meters" },
  "energyMeterIds": [ "evse-meter-1", "evse-meter-2" ],
  "created":        "2026-01-01T00:00:00.000Z",
  "lastChange":     "2026-01-01T00:00:00.000Z"
}
```
