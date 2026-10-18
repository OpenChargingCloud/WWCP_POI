# EVSETemplate

An EVSE template gives [EVSEs](EVSE.md) of one charging station operator what they share, e.g. the current types,
the power and the connectors of all EVSEs of a type, given once, and may name a charging tariff for them. It does not
name its members: an EVSE names its templates in its [`templateIds`](README.md#templates-and-effective-members), in an
order of its own, and takes their properties at runtime, as its [effective EVSE](#properties-of-its-members). It is
embedded in its [ChargingStationOperator](ChargingStationOperator.md), under `EVSETemplates`.

The template belongs to the charging station operator its identification names: `DE*GEF*ETHYC300` belongs to
`DE*GEF`, and every change of it, and every `Add`/`Remove` of it, must be signed with a signing key of that operator.
The identification is `{operatorId}*ET{suffix}`, the suffix up to 50 characters, starting with a letter, digit or
`_`, followed by letters, digits and `_ * - . € $`. It is unique within the operator; two identifications are equal
when they differ only in case.

| | |
|---|---|
| Class | `EVSETemplate` |
| Identification | `EVSETemplate_Id`, e.g. `DE*GEF*ETHYC300` |
| Runtime status | none |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `EVSETemplates` |
| Owner | the [ChargingStationOperator](ChargingStationOperator.md) its identification names (signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/EVSETs/{EVSET_Id}`; embedded in its operator, and listed by `…/EVSETs` |


## Common properties

See [common properties](README.md#common-properties). An EVSE template does not write `eTags`.

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
| `chargingTariffId` | `ChargingTariffId` | `ChargingTariff_Id?` | update/remove | The [charging tariff](ChargingTariff.md) of its operator for its members without tariffs of their own: an EVSE takes the one of the first of its templates naming one, else of the first template of its charging station naming one, else of the first template of its charging pool naming one; see [chargingTariffIds](EVSE.md#chargingtariffids). It must exist at the operator. |

The properties of an EVSE it gives its members, each written and read as an [EVSE](EVSE.md#properties) writes and
reads it, and each updated or removed by a change set:

| JSON key | C# property | Its members take it |
|---|---|---|
| `physicalReference` | `PhysicalReference` | unless they have their own |
| `currentTypes` | `CurrentTypes` | unless they tell their own |
| `chargingModes` | `ChargingModes` | unless they tell their own |
| `maxVoltage` | `MaxVoltage` | unless they have their own |
| `maxCurrent` | `MaxCurrent` | unless they have their own |
| `maxPower` | `MaxPower` | unless they have their own |
| `geoLocation` | `GeoLocation` | unless they have their own |
| `maxReservationDuration` | `MaxReservationDuration` | unless they have their own |
| `reachDistance` | `ReachDistance` | unless they have their own |
| `operationTimeout` | `OperationTimeout` | unless they have their own |
| `extendedOperationTimeout` | `ExtendedOperationTimeout` | unless they tell their own |
| `chargingConnectors` | `ChargingConnectors` | besides their own; of the same identification their own |
| `brands` | `Brands` | besides their own; of the same identification their own |
| `images` | `Images` | besides their own; of the same URL their own |
| `standards` | `Standards` | besides their own |
| `floorLevel` | `FloorLevel` | unless they have their own |
| `acceptedEMobilityProviderIds` | `AcceptedEMobilityProviderIds` | besides their own |

"Unless they have their own": of the first of their templates giving one. "Besides their own": of all their templates;
of elements with the same key the first template's. Absent values are left out, nor are an empty array or unspecified
flags written.


## Properties of its members

An EVSE takes the properties of its templates at runtime: its **effective EVSE**, which its operator calculates once
per version, `chargingStationOperator.TryGetEffectiveEVSE(evseId, out var evse)`. Its templates must not make it
invalid; the effective charging station of its station has it, and is checked with it, e.g. its `maxPower` against
those of its EVSEs. See [templates and effective members](README.md#templates-and-effective-members).


## Adding and removing

An EVSE template is a child of its operator, added and removed as an entity:

```csharp
RoamingNetworkChange.Add   (evseTemplate)      // the parent is the operator its identification names
RoamingNetworkChange.Remove(evseTemplateId)
```

A template an EVSE names can not be removed: update the EVSE first, in the same change set.


## Validation

- `@id` is mandatory and a valid EVSE template identification.
- Electrical limits, durations and the reach distance must not be negative; a charging connector must be valid and
  not be given twice; `brands`, `standards` and `acceptedEMobilityProviderIds` must not name a value twice, an image
  URL must not be given twice.
- In its operator: the template belongs to the operator its identification names; a template must not be given
  twice; `chargingTariffId` must be a tariff of the operator; every effective EVSE naming it, and the effective
  charging station with it, must be valid.
- A change set applies each change to the embedded JSON of the operator and reads the operator again, so these
  rules hold after every single change.
- `Add(evseTemplate)` refuses a parent other than the operator; the template must not exist yet. It gets the change
  set's `createdAt` as `lastChange`, and as `created` unless given.


## Example

```json
{
  "@id":              "DE*GEF*ETHYC300",
  "name":             { "en": "Alpitronic HYC300" },
  "chargingTariffId": "DE*GEF*T3",
  "currentTypes":     [ "DC" ],
  "chargingModes":    [ "Mode_4" ],
  "maxPower":         300000,
  "maxVoltage":       1000,
  "created":          "2026-01-01T00:00:00.000Z",
  "lastChange":       "2026-10-11T12:00:00.000Z"
}
```
