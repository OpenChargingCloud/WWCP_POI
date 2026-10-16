# ChargingTariff

A charging tariff is a price list for charging, modelled as in OCPI: a currency and an ordered list of
elements, each with price components (energy, time, parking time, flat fee, current) that apply while its
optional restrictions hold (time of day, date, energy, current, power, duration, days of the week).
It may name a web page for EV drivers and the energy mix it delivers. POI describes tariffs; it does not
calculate prices.

A tariff is embedded completely in its [ChargingStationOperator](ChargingStationOperator.md) under
`chargingTariffs`, and belongs to the operator its identification names. That operator owns it and signs
every change of it. [EVSE](EVSE.md)s name tariffs of their operator in `chargingTariffIds`, groups in
`chargingTariffId`, [ChargingTariffGroup](ChargingTariffGroup.md)s in `chargingTariffIds`
([below](#how-tariffs-are-named)).

The identification is a `ChargingTariff_Id`: the operator identification, `*T` and a suffix of 1..50 characters
(`[A-Za-z0-9_]` first, then also `*`, `-`, `.`, `€`, `$`), e.g. `DE*GEF*T1`. When reading, the `*` before
`T` is optional. Ids are unique within the operator and compared case-insensitively, ignoring `*` in the suffix.

| | |
|---|---|
| Class | `ChargingTariff` — [source](../WWCP_POI3/RoamingNetwork/ChargingTariff/ChargingTariff.cs) |
| Identification | `ChargingTariff_Id`, e.g. `DE*GEF*T1` |
| Admin status | `ChargingTariffAdminStatusType`, initially `operational` |
| Status | `ChargingTariffStatusType`, initially `active` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `chargingTariffs` |
| Owner | the charging station operator named by the identification (its signing keys) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs/{CT_Id}`; embedded in its charging station operator, `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}`, and listed by `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTs` |


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
| `currency` | `Currency` | string | update | Mandatory: the ISO 4217 code of all its prices, e.g. `EUR`; an unknown code such as `XXX` is refused. Removing it is refused. |
| `tariffType` | `TariffType` | string | update/remove | For which charging sessions it is, as in OCPI: `ChargingTariffType` `AD_HOC_PAYMENT` (paid ad hoc, without a contract), `PROFILE_CHEAP`, `PROFILE_FAST`, `PROFILE_GREEN` (drivers preferring to charge cheaply, fast or green), `REGULAR` (with a contract, without a preference); upper case only. Absent: for every charging session. An EVSE may name several tariffs, one per type. |
| `tariffURL` | `TariffURL` | URL | update/remove | A web page about the tariff, e.g. for EV drivers. |
| `elements` | `Elements` | [ChargingTariffElement](#elements)[] | update | Mandatory, at least one, in their given order. Changed as a whole; removing it is refused. |
| `minPrice` | `MinPrice` | [ChargingPriceLimit](#minprice-and-maxprice) | update/remove | The least a charging session costs: one costing less costs this. Not a flat fee, which every session pays. |
| `maxPrice` | `MaxPrice` | [ChargingPriceLimit](#minprice-and-maxprice) | update/remove | The most a charging session costs: one costing more costs this. |
| `paymentMeans` | `PaymentMeans` | `ImmutableArray<PaymentMeans>` | update/remove | The means it can be paid with, each once, in the ordinal order of their names. Predefined: `CREDIT_CARD`, `DEBIT_CARD`, `VALUE_CARD` (a prepaid card), `CASH` (coins and bills), `CASH_COINS`, `CASH_BILLS`, `MOBILE_ACCOUNT`, `TOLL_TAG`, `PREPAY`, `NFC` (contactless), `EMV` (a card with a chip), `QR_CODE`, `WEBSITE`; other nonempty texts are read as well. |
| `paymentBrands` | `PaymentBrands` | `ImmutableArray<PaymentBrand>` | update/remove | The payment brands it can be paid with, each once, in the ordinal order of their names. Predefined: `AMERICAN_EXPRESS`, `APPLE_PAY`, `CIRRUS`, `DINERS_CLUB`, `DISCOVER`, `GIROCARD`, `MAESTRO`, `MASTERCARD`, `VISA`, `VPAY`; other nonempty texts, e.g. `Google Pay`, are read as well. |
| `validFrom` | `ValidFrom` | timestamp | update/remove | From when it is valid, inclusive, as OCPI's `start_date_time`; absent: always since. Written as every timestamp of the content (`yyyy-MM-ddTHH:mm:ss.fffZ`), to the millisecond. |
| `validUntil` | `ValidUntil` | timestamp | update/remove | Until when it is valid, exclusive, as OCPI's `end_date_time`; absent: for good. After `validFrom`. |
| `energyMix` | `EnergyMix` | [EnergyMix](#energymix) | update/remove | The mix of the energy it delivers. |

The tariff is changed only through its operator's change sets: `RoamingNetworkChange.Add(tariff)` (no parent
needed), `RoamingNetworkChange.Remove(tariffId)`, `UpdateProperty(tariff, "elements", oldValue, newValue)`,
`RemoveProperty(tariff, "tariffURL")`. After every change the operator is read again, so a value the
tariff does not accept (e.g. a currency `XXX`, an element without price components) refuses the change set.


## elements

`ChargingTariffElement` ([source](../WWCP_POI3/RoamingNetwork/ChargingTariff/ChargingTariffElement.cs)):
an immutable value without identification. Its price components apply while its restrictions hold;
without restrictions it always applies. A tariff's elements are changed as a whole (`elements`).

| JSON key | Type | Description |
|---|---|---|
| `priceComponents` | [ChargingPriceComponent](#pricecomponents)[] | Mandatory, at least one, in their given order. |
| `restrictions` | [ChargingTariffRestrictions](#restrictions) | Optional; when the element applies. |


## priceComponents

`ChargingPriceComponent` ([source](../WWCP_POI3/RoamingNetwork/ChargingTariff/ChargingPriceComponent.cs)):
what is billed, an immutable value.

| JSON key | Type | Description |
|---|---|---|
| `type` | string | Mandatory, `ChargingDimensionTypes`: `ENERGY`, `FLAT`, `MAX_CURRENT`, `MIN_CURRENT`, `PARKING_TIME`, `TIME`. |
| `price` | decimal | Mandatory, ≥ 0, excluding VAT, in the tariff's currency: per kWh (`ENERGY`), per A (`MAX_CURRENT`, `MIN_CURRENT`), per hour (`TIME`, `PARKING_TIME`), or the fee (`FLAT`). |
| `vat` | decimal | Optional VAT in percent, 0..100; absent means unknown. |
| `stepSize` | decimal | The billing increment in base units: Wh (`ENERGY`), A (`MAX_CURRENT`, `MIN_CURRENT`), s (`TIME`, `PARKING_TIME`). Mandatory and > 0 for every type but `FLAT`, which must not have one. |

| Type | Billed | `price` per | `stepSize` in |
|---|---|---|---|
| `ENERGY` | the charged energy | kWh | Wh |
| `FLAT` | a flat fee | — | — (none) |
| `MAX_CURRENT` | the maximum current | A | A |
| `MIN_CURRENT` | the minimum current | A | A |
| `PARKING_TIME` | the time not charging | hour | s |
| `TIME` | the time charging | hour | s |


## minPrice and maxPrice

`ChargingPriceLimit` ([source](../WWCP_POI3/RoamingNetwork/ChargingTariff/ChargingPriceLimit.cs)): an amount in
the tariff's currency, an immutable value, as OCPI's `price_limit`. Unlike price components, which give a VAT
rate, it gives the amounts as they are, so that OCPI's are taken without rounding.

| JSON key | Type | Description |
|---|---|---|
| `excludingVAT` | decimal | Mandatory, ≥ 0: the amount excluding VAT. |
| `includingVAT` | decimal | Optional, not less than `excludingVAT`: the amount including VAT; absent means unknown. |

`minPrice` must not exceed `maxPrice`, neither excluding VAT nor, where both give it, including VAT.


## restrictions

`ChargingTariffRestrictions` ([source](../WWCP_POI3/RoamingNetwork/ChargingTariff/ChargingTariffRestrictions.cs)):
when an element applies, an immutable value. All given restrictions must hold; every key is optional and
omitted when absent (an empty object is allowed). Times are local times of the location.

| JSON key | Type | Description |
|---|---|---|
| `startTime` | string | `HH:mm`, from this time of day on, inclusive. Whole minutes only. |
| `endTime` | string | `HH:mm`, until this time of day, exclusive; before `startTime` it means the next day (e.g. `22:00`–`06:30`); not equal to `startTime`, which would be ambiguous. |
| `startDate` | string | `yyyy-MM-dd`, from this date on, inclusive. |
| `endDate` | string | `yyyy-MM-dd`, until this date, exclusive; after `startDate`, so a period is at least one day. |
| `minEnergy` / `maxEnergy` | decimal | Energy charged so far, in Wh. |
| `minCurrent` / `maxCurrent` | decimal | Current, in A. |
| `minPower` / `maxPower` | decimal | Power, in W. |
| `minDuration` / `maxDuration` | decimal | Duration of the session so far, in s. |
| `daysOfWeek` | string[] | `MONDAY` … `SUNDAY`, upper case only, each at most once; written from Monday to Sunday whatever the given order. Omitted when empty. |
| `reservation` | string | `ChargingReservationRestriction`, upper case only: the element prices a reservation rather than charging; `RESERVATION` the reservation until the charging starts (e.g. `TIME` per hour reserved, or a `FLAT` fee), `RESERVATION_EXPIRES` a reservation no charging session followed. |

Every minimum and maximum is ≥ 0, and a minimum must not exceed its maximum.
`startTime`/`endTime` and `startDate`/`endDate` are also read from JSON date tokens (text read with
`DateParseHandling.DateTime`).


## energyMix

`EnergyMix` ([source](../WWCP_POI2/DataStructures/Complex/EnergyMix.cs)), written embedded, i.e. without
`@context`. Charging pools have the same structure as their `energyMix` ([ChargingPool](ChargingPool.md)).
The whole object is replaced or removed by a change set.

| JSON key | Type | Description |
|---|---|---|
| `energySources` | `[category, "percent %"]`[] | Mandatory, also when empty: `[]` means unknown. Each entry a pair of an `EnergySourceCategory` and a percentage text, e.g. `["Wind", "60 %"]`. |
| `environmentalImpacts` | `[category, "percent %"]`[] | Mandatory, also when empty: `[]` means unknown. Pairs of an `EnvironmentalImpact` and a percentage. |
| `supplierName` | I18N text | Mandatory, may be `{}`. The name or brand of the energy supplier. |
| `productName` | I18N text | Mandatory, may be `{}`. The name or brand of the energy product. |
| `additionalRemarks` | I18N text | Optional; omitted when empty. |

- Percentages are texts with a decimal point and ` %`, finite, 0..100; a category at most once per array, in
  their given order. The object must not be empty.
- Predefined `EnergySourceCategory`: `unknown`, `Gas`, `Coal`, `StoneCoal`, `Oil`, `OtherFossil`, `NuclearFision`,
  `NuclearFusion`, `Sun`, `Wind`, `Geothermal`, `Waste`, `Biogas`, `OtherGreen`; any other nonempty text is accepted.
- Predefined `EnvironmentalImpact`: `NuclearWaste`, `CarbonDioxide`; any other nonempty text is accepted.


## How tariffs are named

Tariffs are referenced by identification only, always within the same operator: the operator's constructor
refuses a reference to a tariff it does not embed, so a tariff still named can not be removed, and a
reference to a tariff of another operator is refused.

| Who | JSON key | Type | Change set |
|---|---|---|---|
| [EVSE](EVSE.md) | `chargingTariffIds` | `ChargingTariff_Id`[] | `UpdateProperty(evse, "chargingTariffIds", …)`; ordered (ordinal), each at most once, omitted when empty |
| [EVSEGroup](EVSEGroup.md) | `chargingTariffId` | `ChargingTariff_Id` | `UpdateProperty(group, "chargingTariffId", …)`, optional |
| [ChargingStationGroup](ChargingStationGroup.md) | `chargingTariffId` | `ChargingTariff_Id` | as above |
| [ChargingPoolGroup](ChargingPoolGroup.md) | `chargingTariffId` | `ChargingTariff_Id` | as above |
| [ChargingTariffGroup](ChargingTariffGroup.md) | `chargingTariffIds` | `ChargingTariff_Id`[] | its members |

The operator is read again after every single change, so within one change set a tariff must be added before
it is named, and no longer be named before it is removed.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `ChargingTariff`.

- **Admin status** `ChargingTariffAdminStatusType`: `unknown`, `unspecified`, `planned`, `inDeployment`,
  `operational`, `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `ChargingTariffStatusType`: `unknown`, `unspecified`, `planned`, `active`, `expired`.
  Initially `active` since `created`.
- No energy status.
- Events: `OnAdminStatusChanged` (`OnChargingTariffAdminStatusChangedDelegate`) and `OnStatusChanged`
  (`OnChargingTariffStatusChangedDelegate`), each with timestamp, event tracking id, the tariff, old and new value.
- A change set takes the runtime status along to the changed version of the tariff.


## Validation

- `@id` mandatory, a valid `ChargingTariff_Id`; its operator must be the embedding operator; unique within the operator.
- `currency` mandatory, a known ISO 4217 code.
- `elements` mandatory, a JSON array of objects, at least one, no null entries.
- Every element: `priceComponents` a JSON array with at least one price component; `restrictions`, when given, a JSON object.
- Every price component: `type` mandatory and known; `price` mandatory, ≥ 0; `vat` 0..100; `stepSize` > 0 for every type
  but `FLAT`, absent for `FLAT`.
- Restrictions: `HH:mm` and `yyyy-MM-dd` exactly; times in whole minutes, `startTime` ≠ `endTime`; `startDate` < `endDate`; minima and maxima ≥ 0,
  minimum ≤ maximum; `daysOfWeek` upper-case day names, each at most once.
- `paymentMeans` and `paymentBrands` must not name a value twice (case-insensitively).
- `energyMix`, when given, a valid [energy mix](#energymix).
- Network level: every `chargingTariffIds`/`chargingTariffId` of the operator's EVSEs and groups names one of its tariffs.


## Example

```json
{
  "@id": "DE*GEF*T1",
  "name": { "en": "AC standard", "de": "AC Standard" },
  "currency": "EUR",
  "tariffURL": "https://charging.example.org/tariffs/ac",
  "elements": [
    {
      "priceComponents": [
        { "type": "ENERGY", "price": 0.29, "vat": 19, "stepSize": 1 }
      ],
      "restrictions": { "startTime": "22:00", "endTime": "06:00" }
    },
    {
      "priceComponents": [
        { "type": "ENERGY",       "price": 0.39, "vat": 19, "stepSize": 1  },
        { "type": "PARKING_TIME", "price": 6.0,  "vat": 19, "stepSize": 60 }
      ],
      "restrictions": { "minDuration": 14400, "daysOfWeek": [ "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY" ] }
    },
    {
      "priceComponents": [
        { "type": "ENERGY", "price": 0.39, "vat": 19, "stepSize": 1 },
        { "type": "FLAT",   "price": 0.5,  "vat": 19 }
      ]
    }
  ],
  "paymentMeans": [ "CREDIT_CARD", "NFC", "QR_CODE" ],
  "paymentBrands": [ "MASTERCARD", "VISA" ],
  "energyMix": {
    "energySources": [ [ "Wind", "60 %" ], [ "Sun", "40 %" ] ],
    "environmentalImpacts": [],
    "supplierName": { "en": "Example Energy" },
    "productName": { "en": "Green Power" }
  },
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-03-01T12:00:00.000Z"
}
```
