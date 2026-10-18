# ChargingPoolTemplate

A charging pool template gives [charging pools](ChargingPool.md) of one charging station operator what they share,
e.g. the hotline, the payment options and the facilities of all pools of a site partner, given once, and may name a
charging tariff for their EVSEs. It does not name its members: a pool names its templates in its
[`templateIds`](README.md#templates-and-effective-members), in an order of its own, and takes their properties at
runtime, as its [effective charging pool](#properties-of-its-members).

It is embedded completely in its [ChargingStationOperator](ChargingStationOperator.md) under
`chargingPoolTemplates`, in the ordinal order of the identifications. It belongs to the operator its identification
names, names only a tariff of that operator, and only that operator changes it, with one of its signing keys.

Its identification is the operator's identification, `*PT` and a suffix, e.g. `DE*GEF*PTSUPERMARKET`, unique among
the charging pool templates of the operator.

| | |
|---|---|
| Class | `ChargingPoolTemplate` |
| Identification | `ChargingPoolTemplate_Id`, `{operatorId}*PT{suffix}`, the suffix up to 50 characters, `[a-zA-Z0-9_]` first, then also `* - . € $`, e.g. `DE*GEF*PTSUPERMARKET` |
| Runtime status | none |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `chargingPoolTemplates` |
| Owner | the charging station operator its identification names (a signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPTs/{CPT_Id}`; embedded in its operator, and listed by `…/CPTs` |


## Common properties

See [Common properties](README.md#common-properties).

| JSON key | Change set | Note |
|---|---|---|
| `@id` | — | |
| `name` | update/remove | Optional, no default. |
| `description` | update/remove | |
| `dataSource` | update/remove | |
| `created` | — | Set to the change set's `createdAt` when added without one. |
| `lastChange` | — | Set to the change set's `createdAt` when added and by every change of the template. |
| `customData` | update/remove | |

The template writes no `eTags` and no `@context`.


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `chargingTariffId` | `ChargingTariffId` | `ChargingTariff_Id?` | update/remove | A [charging tariff](ChargingTariff.md) of the same operator for the EVSEs of its members without tariffs of their own, of their EVSE templates and of the templates of their charging stations; see [chargingTariffIds](EVSE.md#chargingtariffids). |

The properties of a charging pool it gives its members, each written and read as a
[charging pool](ChargingPool.md#properties) writes and reads it, and each updated or removed by a change set. Of
`address`, `geoLocation`, `parkingType`, `accessibility`, `hotline`, `gridConnectionPointId` (a grid connection point
of the network), `openingTimes`, `energyMix`, `timeZone`, `entranceAddress`, `entranceLocation`, `exitAddress`,
`exitLocation`, `arrivalInstructions`, `chargingWhenClosed` and `assistanceServiceDetails` its members take the value
of the first of their templates giving one, unless they have their own. Of `dataLicenses`, `brands`, `images`,
`facilities`, `paymentOptions`, `authenticationModes`, `uiFeatures`, `features`, `locationLanguages` (after their
own), `parkingRestrictions`, `relatedLocations`, `services`, `standards` and `nutsCodes` they take those of all their
templates besides their own; of data licenses and brands of the same identification, authentication modes of the same
type and images of the same URL their own, else the first template's. Absent values, empty arrays and empty texts are
left out.


## Properties of its members

A charging pool takes the properties of its templates at runtime: its **effective charging pool**, which its
operator calculates once per version, `chargingStationOperator.TryGetEffectiveChargingPool(poolId, out var pool)`,
with its effective charging stations and EVSEs. Its templates must not make it invalid. See
[templates and effective members](README.md#templates-and-effective-members).


## Adding and removing

The template is added with `RoamingNetworkChange.Add(chargingPoolTemplate)`, as `ToJSON()` writes it, and removed
with `RoamingNetworkChange.Remove(chargingPoolTemplateId)`; a given parent must be its operator. A template a pool
names can not be removed: update the pool first, in the same or an earlier change set.


## Validation

- **Identification:** a valid `ChargingPoolTemplate_Id` naming the operator that embeds it; no other charging pool
  template of the operator has it.
- **Tariff:** `chargingTariffId`, if given, a charging tariff of the operator.
- **Properties of its members:** as a charging pool checks them, e.g. no facility twice; `gridConnectionPointId` a
  grid connection point of the network; every effective charging pool naming it valid.
- **Change sets:** `UpdateProperty`/`RemoveProperty` only of `name`, `description`, `dataSource`,
  `chargingTariffId`, `customData` and the properties of its members above. After every change the whole operator is
  read again, so the rules above hold for the result; a tariff the template names can not be removed.


## Example

```json
{
  "@id": "DE*GEF*PTSUPERMARKET",
  "name": { "en": "Supermarket sites" },
  "chargingTariffId": "DE*GEF*TAC1",
  "hotline": "+49 800 4242424",
  "paymentOptions": [ "Contract", "Direct" ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-10-12T12:00:00.000Z"
}
```
