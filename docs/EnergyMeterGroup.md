# EnergyMeterGroup

An energy meter group collects [energy meters](EnergyMeter.md) of one owner, e.g. all calibration-law meters of one
model in the EVSEs of an operator, or the grid meters of a grid operator. Groups may overlap: an energy meter can be
a member of several groups. Two parties own energy meter groups:

- a [ChargingStationOperator](ChargingStationOperator.md) groups the energy meters of its charging pools, charging
  stations and EVSEs, under `energyMeterGroups`;
- a [GridOperator](GridOperator.md) groups the energy meters of its
  [grid connection points](GridOperator.md#grid-connection-points), under `energyMeterGroups`, whoever maintains them.

It may give its members the properties of an energy meter that are not those of the single device: e.g. the
manufacturer, the model, the firmware version and the transparency software of all meters of one series, given once.
The network keeps them in the group; the energy meters take them at runtime, as the
[effective energy meters](#properties-of-its-members) of what embeds them.

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
| `priority` | `Priority` | `Int32?` | update/remove | Which of the groups of a member decides where they give it different values: the higher wins; 0 unless given. An integer, neither a text nor a fraction; see [group priority](README.md#group-priority-and-effective-members). |

The properties of an energy meter it gives its members, each written and read as an
[energy meter](EnergyMeter.md#properties) writes and reads it, and each updated or removed by a change set. Of `role`,
`manufacturerId` (an energy meter manufacturer of the network), `modelId`, `modelURL`, `hardwareVersion` and
`firmwareVersion` its members take the group's value unless they have their own. Of `transparencySoftwareStatus`
(releases and certificates of the network) and `signatureMethods` they take the group's besides their own; of the
status of the same transparency software release their own. Its `serialNumber` and its `publicKeys` belong to the
single device: no group gives them.

Arrays are written only when not empty; a group may be empty. A change set names the properties by their
JSON keys (`ChangeableEnergyMeterGroupProperties` in
[RoamingNetwork.EnergyMeterGroupChanges.cs](../WWCP_POI3/RoamingNetwork/RoamingNetwork.EnergyMeterGroupChanges.cs)):

```csharp
RoamingNetworkChange.Add           (group, chargingStationOperatorId)    // or gridOperatorId
RoamingNetworkChange.UpdateProperty(group, "firmwareVersion", "4.7.1", "4.8.0")
RoamingNetworkChange.Remove        (groupId)
```


## Properties of its members

An energy meter takes the properties its groups give it at runtime: its **effective energy meter**. It is embedded in
what embeds the energy meter, as that is at runtime:

- the effective EVSEs, charging stations and charging pools of a charging station operator embed the effective energy
  meters, `chargingStationOperator.TryGetEffectiveEVSE(evseId, out var evse)` and `EffectiveChargingPools`; an EVSE,
  charging station or charging pool whose energy meters take something of their groups is effective as well;
- the effective grid connection points of a grid operator, `gridOperator.EffectiveGridConnectionPoints` and
  `TryGetEffectiveGridConnectionPoint(gridConnectionPointId, out var gridConnectionPoint)`.

A single value is the energy meter's own, else the one of its group with the highest `priority`; an array is its own
united with those of all its groups. Groups of the same priority must not give it different values that decide, nor
make it invalid. An effective energy meter has the runtime status of the energy meter; its last change is the latest
of it and its groups, and so is the one of the EVSE, charging station or charging pool embedding it. See
[group priority and effective members](README.md#group-priority-and-effective-members). POI_API serves the effective
ones; `?view=stored` charging pools, charging stations, EVSEs and grid connection points as they are given. Its owner
embeds the energy meters as they are given, with their groups.


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
- `energyMeterIds` and the arrays of the properties of its members: none twice, at most one status per transparency
  software release; `priority` an integer.
- Every `energyMeterIds` names an energy meter of its owner. An energy meter still named by a group can not be removed.
- `manufacturerId` names an energy meter manufacturer of the network; every transparency software release and
  certificate it names exists in the network, and a certificate covers its release. Every effective energy meter must
  be valid, and groups of the same priority must agree.
- Nothing references a group, so it can always be removed.
- `Add` needs its owner as the parent, an existing `ChargingStationOperator` or `GridOperator`, and an object whose
  `@id` is the change's `entityId`; the group must not exist yet. A charging station operator or grid operator is
  added without its groups: they are its own to add (`RoamingNetworkChange.AddChildren`).


## Example

The EVSE meters of a charging station operator:

```json
{
  "@id":               "gef-evse-meters",
  "name":              { "en": "EVSE meters" },
  "energyMeterIds":    [ "evse-meter-1", "evse-meter-2" ],
  "role":              "evse",
  "manufacturerId":    "ACME",
  "modelId":           "ACME-3000",
  "firmwareVersion":   "4.7.1",
  "signatureMethods":  [ "OCMF" ],
  "created":           "2026-01-01T00:00:00.000Z",
  "lastChange":        "2026-01-01T00:00:00.000Z"
}
```
