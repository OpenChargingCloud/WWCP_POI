# EnergyMeterTemplate

An energy meter template gives [energy meters](EnergyMeter.md) what a series of meters shares, e.g. the manufacturer,
the model, the firmware version and the transparency software of all meters of one series, given once. It does not
name its members: an energy meter names its templates in its [`templateIds`](README.md#templates-and-effective-members),
in an order of its own, and takes their properties at runtime, as its
[effective energy meter](#properties-of-its-members). Two parties have energy meter templates:

- a [ChargingStationOperator](ChargingStationOperator.md), for the energy meters of its charging pools, charging
  stations and EVSEs, under `energyMeterTemplates`;
- a [GridOperator](GridOperator.md), for the energy meters of its
  [grid connection points](GridOperator.md#grid-connection-points), under `energyMeterTemplates`, whoever maintains
  them.

Its owner signs every change to it; for a grid operator without keys, not taking part, the roaming network operator.
Its identification is a free text, compared case-insensitively, e.g. `acme-3000`, unique within the roaming network
(not only within its owner).

| | |
|---|---|
| Class | `EnergyMeterTemplate` |
| Identification | `EnergyMeterTemplate_Id`, e.g. `acme-3000` (`NewRandom()` makes a random one) |
| Runtime status | none |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) or [GridOperator](GridOperator.md), as `energyMeterTemplates` |
| Owner | its charging station operator, or its grid operator (signing key); for a grid operator without keys the roaming network operator |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EMTs/{EMT_Id}` and `GET ~/RNs/{RN_Id}/GOs/{GO_Id}/EMTs/{EMT_Id}`; embedded in its owner, and listed by `…/EMTs` |


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

The properties of an energy meter it gives its members, each written and read as an
[energy meter](EnergyMeter.md#properties) writes and reads it, and each updated or removed by a change set. Of `role`,
`manufacturerId` (an energy meter manufacturer of the network), `modelId` (one of its models), `modelURL`, `hardwareVersion` and
`firmwareVersion` its members take the value of the first of their templates giving one, unless they have their own.
Of `transparencySoftwareStatus` (releases and certificates of the network) and `signatureMethods` they take those of
all their templates besides their own; of the status of the same transparency software release their own, else the
first template's. Its `serialNumber` and its `publicKeys` belong to the single device: no template gives them.

Arrays are written only when not empty. A change set names the properties by their JSON keys:

```csharp
RoamingNetworkChange.Add           (template, chargingStationOperatorId)    // or gridOperatorId
RoamingNetworkChange.UpdateProperty(template, "firmwareVersion", "4.7.1", "4.8.0")
RoamingNetworkChange.Remove        (templateId)
```


## Properties of its members

An energy meter takes the properties of its templates at runtime: its **effective energy meter**. It is embedded in
what embeds the energy meter, as that is at runtime:

- the effective EVSEs, charging stations and charging pools of a charging station operator embed the effective energy
  meters, `chargingStationOperator.TryGetEffectiveEVSE(evseId, out var evse)` and `EffectiveChargingPools`; an EVSE,
  charging station or charging pool whose energy meters take something of their templates is effective as well;
- the effective grid connection points of a grid operator, `gridOperator.EffectiveGridConnectionPoints` and
  `TryGetEffectiveGridConnectionPoint(gridConnectionPointId, out var gridConnectionPoint)`.

Its templates must not make it invalid. An effective energy meter has the runtime status of the energy meter; its last
change is the latest of it and its templates, and so is the one of the EVSE, charging station or charging pool
embedding it. See [templates and effective members](README.md#templates-and-effective-members). POI_API serves the
effective ones; `?view=stored` charging pools, charging stations, EVSEs and grid connection points as they are given.


## Validation

- `@id` is mandatory, any non-empty text (trimmed); unique within the roaming network, across charging station
  operators and grid operators, compared case-insensitively.
- The arrays of the properties of its members: none twice, at most one status per transparency software release.
- `manufacturerId` names an energy meter manufacturer of the network, `modelId` one of its models, and only with it;
  every transparency software release and
  certificate it names exists in the network, and a certificate covers its release. Every effective energy meter
  naming it must be valid.
- A template an energy meter names can not be removed: update the energy meter first, in the same change set.
- `Add` needs its owner as the parent, an existing `ChargingStationOperator` or `GridOperator`, and an object whose
  `@id` is the change's `entityId`; the template must not exist yet. A charging station operator or grid operator is
  added without its templates: they are its own to add (`RoamingNetworkChange.AddChildren`).


## Example

The meters of one series in the EVSEs of a charging station operator:

```json
{
  "@id":              "acme-3000",
  "name":             { "en": "ACME-3000 EVSE meters" },
  "role":             "evse",
  "manufacturerId":   "ACME",
  "modelId":          "ACME-3000",
  "firmwareVersion":  "4.7.1",
  "signatureMethods": [ "OCMF" ],
  "created":          "2026-01-01T00:00:00.000Z",
  "lastChange":       "2026-01-01T00:00:00.000Z"
}
```
