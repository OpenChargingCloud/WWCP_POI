# ChargingTariffOffer

A charging tariff offer announces another price for a time at the EVSEs of groups or of operators: e.g. a promotion
of a charging station operator ("the first charge of a contract at our motorway pools 30 % cheaper this summer"), or
the price an e-mobility provider gives its customers at the EVSEs of many operators ("39 ct/kWh at all EVSEs of
operator X and at our partner sites"). Which EVSEs, is a business decision that does not follow from their
properties: groups name them, and the offer names the groups. Neither the groups nor the EVSEs change; no effective
member takes an offer. The [specification](../SPECIFICATION.md#94-charging-tariff-offers) defines it.

Two parties have offers:

- a [ChargingStationOperator](ChargingStationOperator.md), for its own EVSEs: it changes its own price;
- an [EMobilityProvider](EMobilityProvider.md), for the EVSEs of any operator: it changes the price its customers
  pay, and needs no signature of the operators, as it changes nothing of theirs.

| | |
|---|---|
| Class | `ChargingTariffOffer` |
| Identification | `ChargingTariffOffer_Id`, free text, compared without case, unique in the roaming network, e.g. `gef-summer-2026` |
| Admin status | `ChargingTariffOfferAdminStatusType`, initially `operational` |
| Status | `ChargingTariffOfferStatusType`: `planned`, `active`, `exhausted`, `expired`; see [runtime status](#runtime-status) |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) or [EMobilityProvider](EMobilityProvider.md), as `chargingTariffOffers` |
| Owner | the party embedding it (signing key), the parent of an `Add` |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CTOs/{CTO_Id}` and `GET ~/RNs/{RN_Id}/EMPs/{EMP_Id}/CTOs/{CTO_Id}`; embedded in its owner, and listed by `…/CTOs` |


## Properties

Besides the [common properties](README.md#common-properties), each updated or removed by a change set:

| JSON key | Type | Description |
|---|---|---|
| `chargingTariff` | object | A price of its own, as a [charging tariff](ChargingTariff.md#properties) writes it, without its identification, `tariffType`, validity and timestamps: `currency`, `elements`, and optionally `minPrice`, `maxPrice`, `tariffURL`, `paymentMeans`, `paymentBrands`, `energyMix`. |
| `discount` | object | `{"percent": <decimal above 0, at most 100>, "dimensions": [ … ]}`: reduces a price by the percentage, the price components of the dimensions named (`ENERGY`, `FLAT`, `MAX_CURRENT`, `MIN_CURRENT`, `PARKING_TIME`, `TIME`), or, without `dimensions`, all of them and its `minPrice` and `maxPrice`. |
| `EVSEGroupIds` | `EVSEGroup_Id`[] | [EVSE groups](EVSEGroup.md) whose EVSEs it covers. |
| `chargingStationGroupIds` | `ChargingStationGroup_Id`[] | [Charging station groups](ChargingStationGroup.md) whose charging stations' EVSEs it covers. |
| `chargingPoolGroupIds` | `ChargingPoolGroup_Id`[] | [Charging pool groups](ChargingPoolGroup.md) whose charging pools' EVSEs it covers. |
| `chargingStationOperatorIds` | `ChargingStationOperator_Id`[] | Operators all of whose EVSEs it covers. |
| `notBefore` | timestamp | From when it applies, inclusive; absent: always since. |
| `notAfter` | timestamp | Until when it applies, inclusive; absent: for good. Not before `notBefore`. |
| `openingTimes` | `OpeningTimes` | When it applies within them, as a [charging pool](ChargingPool.md#opening-times) writes its opening times; absent: at any time. |
| `sessions` | string | Of an operator's offer only: `adHoc` (paid without a contract) or `contract`; absent: both. |
| `eMobilityProviderIds` | `EMobilityProvider_Id`[] | Of an operator's offer only: for contracts of these providers only. |
| `maxSessionsPerContract` | integer | How often one contract, or ad hoc one means of payment, takes it, at least 1; absent: every time. |

- Exactly one of `chargingTariff` and `discount`.
- At least one of the four ways to name EVSEs. An operator's offer names groups of its own and no operator but
  itself; a provider's offer groups of its own or of any operator, and any operator.
- The arrays are ordered by identification, each value once, as other arrays of identifications.


## Covered EVSEs

An EVSE is **covered** by an offer where it, its charging station or its charging pool is a member of one of the
groups the offer names, or where its operator is named. Groups name their members and give them nothing. An
e-mobility provider has groups of its own for its offers, naming members of any operator; e.g. its partner sites:

```json
"chargingPoolGroups": [
  { "@id": "DE*GDF*PGPARTNER", "name": { "en": "Partner sites" },
    "chargingPoolIds": [ "DE*ABC*P7", "DE*GEF*P1" ] }
]
```


## When it applies

From `notBefore` until `notAfter`, both inclusive, as the validity of a key, and while its `openingTimes` are open:
e.g. regular openings every day from 18:00 to 06:00, a period ending before it begins ending the next day. An offer
applies to the part of a charging session within these times, as the restrictions of the elements of a tariff do: a
session from 17:30 to 19:00 takes an offer opening at 18:00 from 18:00 on. It applies only while it is `active`
([runtime status](#runtime-status)).


## What it changes

- **An operator's offer** changes the operator's price at a covered EVSE: the regular tariffs the EVSE takes (its
  effective `chargingTariffIds`) for the sessions the offer names are replaced by its `chargingTariff`, or reduced by
  its `discount`. A regular tariff of the `tariffType` `AD_HOC_PAYMENT` is the one of ad hoc sessions, one of any
  other type of sessions with a contract, one without a type of both.
- **A provider's offer** changes the price its customers pay at a covered EVSE: its `chargingTariff`, or their usual
  price reduced by its `discount`. The provider knows that price, the roaming network does not.
- An operator's offer and a provider's offer at the same EVSE are prices of two parties and do not overlap. Whether
  a customer of the provider pays less when the operator's price is lower is between the two.
- Whether a session meets its conditions, e.g. is the first of its contract, the owner of the offer decides when the
  session starts, and the charge detail record tells the price: the roaming network announces offers, it does not
  enforce them.


## No overlap

Two offers of one owner **overlap** where they cover the same EVSE, for sessions both apply to (ad hoc, or with a
contract of a provider both name, or one of them names none), at times both apply in, by their `notBefore`,
`notAfter` and `openingTimes`.

- **Operators:** a version where two offers of an operator overlap is invalid, and so is a change set making them
  overlap. The offers, their groups and the EVSEs are all the operator's data; it is checked per EVSE.
- **Providers:** a change set of a provider must not make two of its offers overlap. They may still come to overlap
  by the changes of others, e.g. when an operator adds an EVSE to a group both name; then the one with the later
  `notBefore` applies, of the same `notBefore` the one with the ordinal lower identification. A version is never
  invalid for the data of two owners together.


## References across owners

A provider's groups and offers name members, groups and operators of others without asking them. These references
are weak: a member, group or operator that does not exist (any more) is left out, and its owner removes it without
the provider, e.g. an operator an EVSE a provider's group still names. References within one owner are strong: a
member or group an operator's group or offer names can not be removed, update the group or offer first, in the same
change set.


## Runtime status

See [runtime status](README.md#runtime-status) for the mechanism.

- **Status** `ChargingTariffOfferStatusType`: `planned` before its `notBefore`, `active` from then on, `expired`
  after its `notAfter`, calculated; unless its owner sets `exhausted` before, e.g. when its budget is used up or it
  ends early. Only an `active` offer applies.
- **Admin status** `ChargingTariffOfferAdminStatusType`, set manually, as of every entity.

The status is not content: a budget used up changes no version, and is sent to peers as every status is.


## POI_API

- Offers are served under the paths above, embedded in their owner, and listed by it.
- `…/EVSEs/{EVSE_Id}/CTOs` lists the offers covering an EVSE, of its operator and of providers; `…/CSs/{CS_Id}/CTOs`
  and `…/CPs/{CP_Id}/CTOs` those covering one of its EVSEs. `?status=active` leaves out all others,
  `?eMobilityProviderId=` all but those of the operator and of that provider.
- An EVSE, charging station or charging pool itself does not change: the offers are not part of what it is at runtime.


## Publishers

OCPI knows no offers. An operator's active offer without `maxSessionsPerContract` can be published as tariffs taking
the place of the regular ones while it is valid, to the providers it names where it names some; a discount as the
regular tariff with the reduced prices, and its regular opening times as restrictions of their elements. One with
`maxSessionsPerContract` is published only as text, as a tariff would show its price also to drivers who no longer get
it. A provider's offers are no tariffs of an operator: POI_API serves them, e.g. to the provider's app or to route
planners.


## Examples

The first charge of a contract at the motorway pools of `DE*GEF` 30 % cheaper, this summer, on the energy:

```json
{
  "@id":                    "gef-summer-2026",
  "name":                   { "de": "Sommeraktion A9: erste Ladung 30 % günstiger" },
  "discount":               { "percent": 30, "dimensions": [ "ENERGY" ] },
  "chargingPoolGroupIds":   [ "DE*GEF*PGA9" ],
  "notBefore":              "2026-06-01T00:00:00.000Z",
  "notAfter":               "2026-08-31T23:59:59.999Z",
  "sessions":               "contract",
  "maxSessionsPerContract": 1,
  "created":                "2026-05-15T00:00:00.000Z",
  "lastChange":             "2026-05-15T00:00:00.000Z"
}
```

20 % less at night, from 18:00 to 06:00, at the supermarket sites of `DE*GEF`, for every session:

```json
{
  "@id":                     "gef-night",
  "name":                    { "de": "Nachtrabatt" },
  "discount":                { "percent": 20 },
  "chargingStationGroupIds": [ "DE*GEF*SG1" ],
  "openingTimes":            { "24/7": false,
                               "regularOpenings": [ { "monday":    [ { "begin": "18:00", "end": "06:00" } ] },
                                                    { "tuesday":   [ { "begin": "18:00", "end": "06:00" } ] },
                                                    { "wednesday": [ { "begin": "18:00", "end": "06:00" } ] },
                                                    { "thursday":  [ { "begin": "18:00", "end": "06:00" } ] },
                                                    { "friday":    [ { "begin": "18:00", "end": "06:00" } ] },
                                                    { "saturday":  [ { "begin": "18:00", "end": "06:00" } ] },
                                                    { "sunday":    [ { "begin": "18:00", "end": "06:00" } ] } ] },
  "created":                 "2026-10-01T00:00:00.000Z",
  "lastChange":              "2026-10-01T00:00:00.000Z"
}
```

39 ct/kWh, excluding VAT, for the customers of the provider `DE-GDF` at all EVSEs of `DE*ION` and at its partner
sites, from October:

```json
{
  "@id":                        "gdf-hpc-39",
  "name":                       { "de": "HPC für 39 ct/kWh" },
  "chargingTariff":             { "currency": "EUR",
                                  "elements": [ { "priceComponents": [ { "type": "ENERGY", "price": 0.39, "vat": 19, "stepSize": 1 } ] } ] },
  "chargingStationOperatorIds": [ "DE*ION" ],
  "chargingPoolGroupIds":       [ "DE*GDF*PGPARTNER" ],
  "notBefore":                  "2026-10-01T00:00:00.000Z",
  "created":                    "2026-09-20T00:00:00.000Z",
  "lastChange":                 "2026-09-20T00:00:00.000Z"
}
```


## Open points

Proofs of offers, and groups of other parties than providers: see
[SPECIFICATION §9.5](../SPECIFICATION.md#95-open-points).
