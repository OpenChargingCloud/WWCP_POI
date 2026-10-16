# Brand

A brand is a name, with a logo and a homepage, that something of a charging station operator is known by. It means
something of its own on every level it is embedded in, and the levels are never mixed:

| Embedded in | JSON key | Meaning |
|---|---|---|
| [ChargingStationOperator](ChargingStationOperator.md) | `brands` | The operator is a white-label solution for the brand. |
| [ChargingPool](ChargingPool.md), [ChargingStation](ChargingStation.md), [EVSE](EVSE.md) | `brands` | It also appears under the brand. |
| an RFID [authentication mode](ChargingPool.md#authentication-modes) of a pool | `brands` | It takes the cards of the brand. |

A brand of an operator is no brand of its pools, a brand of a pool none of its stations, nor the other way round.
Each of them embeds its own brands completely; nothing names a brand by its identification, and the roaming network
has no registry of brands. Two levels may each embed a brand with the same `@id`: they are two brands, each changed
on its own.

| | |
|---|---|
| Class | `Brand` — [source](../WWCP_POI3/RoamingNetwork/Brand/Brand.cs) |
| Identification | `Brand_Id`, e.g. `blue-charge`; unique within the `brands` it is in |
| Admin status | — (no runtime status) |
| Status | — |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md), [ChargingPool](ChargingPool.md), [ChargingStation](ChargingStation.md), [EVSE](EVSE.md) and RFID [authentication modes](ChargingPool.md#authentication-modes), each as `brands` |
| Owner | the owner of what embeds it: the charging station operator (a signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/Brands` and `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/Brands/{Brand_Id}`, the same below a pool `…/CPs/{CP_Id}/Brands[/{Brand_Id}]`, a station `…/CSs/{CS_Id}/Brands[/{Brand_Id}]` and an EVSE `…/EVSEs/{EVSE_Id}/Brands[/{Brand_Id}]`; see [HTTP](#http) |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `@id` | `Id` | `Brand_Id` | — | Mandatory, nonempty after trimming, compared case-insensitively. |
| `name` | `Name` | `ImmutableI18NString` | update | Mandatory, at least one language. |
| `description` | `Description` | `ImmutableI18NString?` | update/remove | Omitted when empty. |
| `logo` | `Logo` | `URL?` | update/remove | |
| `homepage` | `Homepage` | `URL?` | update/remove | |
| `customData` | `CustomData` | `CustomDataNew?` | update/remove | Omitted when empty. |

A brand is no entity: it has no timestamps (`created` and `lastChange` are refused), no runtime status and no
`eTags`. Two brands are equal, and ordered, by their `@id` alone. `brands` is written only when it is not empty,
in the ordinal order of the identifications; `brandIds` is refused: brands must be given completely in `brands`.


## Change sets

- **As a whole:** `brands` is a property of the operator, pool, station and EVSE:
  `UpdateProperty(pool, "brands", …, [ … ])` with complete brands, `RemoveProperty(pool, "brands")` leaves none.
- **Brand by brand:** with [element changes](README.md#element-changes), the element path naming the brand by its
  `@id` in `brands`; the change of a property of a brand, by the column *Change set* above:

```csharp
ImmutableArray<POIElementPathSegment> BrandPath(String BrandId) => [ new POIElementPathSegment("brands", BrandId) ];

RoamingNetworkChange.AddElement           (nameof(ChargingPool),            "DE*GEF*P1", BrandPath("blue-charge"), blueCharge.ToJSON());
RoamingNetworkChange.UpdateElementProperty(nameof(ChargingPool),            "DE*GEF*P1", BrandPath("fast-charge"), "homepage", null, "https://fast.example");
RoamingNetworkChange.RemoveElement        (nameof(ChargingPool),            "DE*GEF*P1", BrandPath("fast-charge"));
RoamingNetworkChange.AddElement           (nameof(ChargingStationOperator), "DE*GEF",    BrandPath("white-charge"), whiteCharge.ToJSON());
```

- **RFID authentication modes:** their brands are changed with the pool's `authenticationModes`, as a whole:
  an authentication mode has no `@id`.
- **Owner:** every change is signed with a signing key of the charging station operator: its own brands are one of
  its properties, those of its pools, stations, EVSEs and RFID authentication modes part of them. The RNO has no say.
- **Merges:** concurrent changes of different brands of the same level merge brand by brand; the same brand added
  twice to the same level conflicts. The brands of different levels are different properties and never conflict.


## HTTP

`GET …/Brands` lists the brands of the operator, pool, station or EVSE, each as it embeds them, searched and paged as
every list ([../README.md](../README.md#query)): `match`, `offset`, `limit`, `X-Total-Count`, `X-Filtered-Count`.
`GET …/Brands/{Brand_Id}` is one of them, with an ETag of its own, and 404 for a brand of another level. With an
`HTMLView`, both are answered by it for a browser, as every list and entity.


## Validation

- `@id` mandatory; `name` mandatory with at least one language ("needs its name"); `logo` and `homepage` URLs.
- Within one `brands` an `@id` only once ("must not be given twice").
- `created` and `lastChange` refused; `brandIds` refused ("must be given completely in 'brands', not by 'brandIds'").
- Brands of an authentication mode only for `RFID`.
- Nothing names a brand: a brand can be removed at any time.
- After every change the operator is read again (`ChargingStationOperator.TryParse`), so these rules hold for the
  result.


## Example

`brands` of a charging pool.

```json
"brands": [
  { "@id": "blue-charge",
    "name": { "en": "Blue Charge" },
    "logo": "https://blue.example/logo.svg",
    "homepage": "https://blue.example" },
  { "@id": "fast-charge",
    "name": { "en": "Fast Charge" } }
]
```
