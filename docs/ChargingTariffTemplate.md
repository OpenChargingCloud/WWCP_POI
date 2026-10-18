# ChargingTariffTemplate

A charging tariff template gives [charging tariffs](ChargingTariff.md) of one charging station operator what they
share, e.g. the currency, the payment means and brands, the energy mix and the web page of many tariffs, or elements
they have in common, given once. It does not name its members: a tariff names its templates in its
[`templateIds`](README.md#templates-and-effective-members), in an order of its own, and takes their properties at
runtime, as its [effective tariff](#properties-of-its-members). It is embedded completely in its
[ChargingStationOperator](ChargingStationOperator.md) under `chargingTariffTemplates` and belongs to the operator its
identification names. That operator owns it and signs every change of it.

The identification is a `ChargingTariffTemplate_Id`: the operator identification, `*TT` and a suffix of 1..50
characters (`[A-Za-z0-9_]` first, then also `*`, `-`, `.`, `€`, `$`), e.g. `DE*GEF*TTEUR`. When reading, the `*`
before `TT` is optional. Ids are unique within the operator and compared case-insensitively.

| | |
|---|---|
| Class | `ChargingTariffTemplate` |
| Identification | `ChargingTariffTemplate_Id`, e.g. `DE*GEF*TTEUR` |
| Runtime status | none |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `chargingTariffTemplates` |
| Owner | the charging station operator named by the identification (its signing keys) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTTs/{CTT_Id}`; embedded in its charging station operator, and listed by `…/CTTs` |


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

The properties of a charging tariff it gives its members, each written and read as a
[charging tariff](ChargingTariff.md#properties) writes and reads it, and each updated or removed by a change set; none
of them is mandatory in a template:

| JSON key | Its members take it |
|---|---|
| `currency` | unless they have their own |
| `tariffType` | unless they have their own |
| `tariffURL` | unless they have their own |
| `elements` | unless they have their own, as a whole |
| `minPrice` | unless they have their own |
| `maxPrice` | unless they have their own |
| `paymentMeans` | besides their own |
| `paymentBrands` | besides their own |
| `validFrom` | unless they have their own |
| `validUntil` | unless they have their own |
| `energyMix` | unless they have their own |

"Unless they have their own": of the first of their templates giving one. "Besides their own": of all their
templates. Absent values are left out; an empty array is not written.


## Properties of its members

A charging tariff takes the properties of its templates at runtime: its **effective tariff**, which its operator
calculates once per version. The effective tariff must have a `currency` and `elements`, of its own or of its
templates; the tariff as it is given need not. The effective EVSEs name the tariffs, and publishers publish the
effective ones. See [templates and effective members](README.md#templates-and-effective-members).


## Change sets

- `RoamingNetworkChange.Add(template)` — no parent needed, it is added to the operator its identification names.
- `RoamingNetworkChange.Remove(templateId)` — a template a tariff names can not be removed: update the tariff first,
  in the same change set.
- `UpdateProperty(template, …)` / `RemoveProperty(template, …)` of `name`, `description`, `dataSource`,
  `customData` and the properties above.
- Signed with a signing key of the operator.


## Validation

- `@id` mandatory, a valid `ChargingTariffTemplate_Id`; its operator must be the embedding operator; unique within the
  operator.
- Its properties are checked as a tariff checks them, e.g. a known currency, elements with price components; every
  effective tariff naming it must be valid.


## Example

```json
{
  "@id":           "DE*GEF*TTEUR",
  "name":          { "en": "Euro tariffs" },
  "currency":      "EUR",
  "paymentMeans":  [ "CREDIT_CARD", "DEBIT_CARD" ],
  "tariffURL":     "https://example.com/tariffs",
  "created":       "2026-01-01T00:00:00.000Z",
  "lastChange":    "2026-01-01T00:00:00.000Z"
}
```
