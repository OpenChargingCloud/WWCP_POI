# ChargingStationTemplate

A charging station template gives [charging stations](ChargingStation.md) of one charging station operator what they
share, e.g. the model, the hotline and the features of all stations of a model, given once, and may name a charging
tariff for their EVSEs. It does not name its members: a station names its templates in its
[`templateIds`](README.md#templates-and-effective-members), in an order of its own, and takes their properties at
runtime, as its [effective charging station](#properties-of-its-members). It is embedded in its
[ChargingStationOperator](ChargingStationOperator.md), under `chargingStationTemplates`.

The template belongs to the charging station operator its identification names: `DE*GEF*STWALLBOX22` belongs to
`DE*GEF`, and every change of it, and every `Add`/`Remove` of it, must be signed with a signing key of that operator.
The identification is `{operatorId}*ST{suffix}`, the suffix up to 50 characters, starting with a letter, digit or
`_`, followed by letters, digits and `_ * - . € $`. It is unique within the operator; two identifications are equal
when they differ only in case.

| | |
|---|---|
| Class | `ChargingStationTemplate` |
| Identification | `ChargingStationTemplate_Id`, e.g. `DE*GEF*STWALLBOX22` |
| Runtime status | none |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `chargingStationTemplates` |
| Owner | the [ChargingStationOperator](ChargingStationOperator.md) its identification names (signing key) |
| HTTP | `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CSTs/{CST_Id}`; embedded in its operator, and listed by `…/CSTs` |


## Common properties

See [common properties](README.md#common-properties). A charging station template does not write `eTags`.

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
| `chargingTariffId` | `ChargingTariffId` | `ChargingTariff_Id?` | update/remove | The [charging tariff](ChargingTariff.md) of its operator for the EVSEs of its members without tariffs of their own and of their EVSE templates: see [chargingTariffIds](EVSE.md#chargingtariffids). It must exist at the operator. |

The properties of a charging station it gives its members, each written and read as a
[charging station](ChargingStation.md#properties) writes and reads it, and each updated or removed by a change set:

| JSON key | C# property | Its members take it |
|---|---|---|
| `physicalReference` | `PhysicalReference` | unless they have their own |
| `address` | `Address` | unless they have their own |
| `geoLocation` | `GeoLocation` | unless they have their own |
| `hotline` | `HotlinePhoneNumber` | unless they have their own |
| `manufacturerId` | `ManufacturerId` | unless they have their own; a manufacturer of the network |
| `modelId` | `ModelId` | unless they have their own |
| `openingTimes` | `OpeningTimes` | unless they have their own |
| `accessibility` | `Accessibility` | unless they have their own |
| `calibrationInfo` | `CalibrationInfo` | unless they have their own |
| `certificationInfo` | `CertificationInfo` | unless they have their own |
| `maxPower` | `MaxPower` | unless they have their own; not above the one of all EVSEs of a member together |
| `brands` | `Brands` | besides their own; of the same identification their own |
| `images` | `Images` | besides their own; of the same URL their own |
| `vehicleTypes` | `VehicleTypes` | besides their own |
| `euVehicleCategories` | `EUVehicleCategories` | besides their own |
| `features` | `Features` | besides their own |
| `parkingSpaceIds` | `ParkingSpaceIds` | besides their own; parking spaces of the network |

"Unless they have their own": of the first of their templates giving one. "Besides their own": of all their templates;
of elements with the same key the first template's. Absent values are left out; an empty array is not written.


## Properties of its members

A charging station takes the properties of its templates at runtime: its **effective charging station**, which its
operator calculates once per version. Its templates must not make it invalid, e.g. by a `maxPower` above the one of
all its EVSEs together: such an operator is refused, and so is a change set making it so, already by
`CreateChangeSet`. The effective charging station has the runtime status, the effective EVSEs and the energy meters of
the station, and as `lastChange` the latest of the station, its templates and what it embeds.

```csharp
chargingStationOperator.TryGetEffectiveChargingStation(stationId, out var effectiveStation);
chargingStationOperator.EffectiveChargingStation(station);       // the station itself, when its templates give it nothing
chargingStationOperator.EffectiveChargingStations;               // all of them
chargingStationOperator.EffectiveChargingPools;                  // the pools with them
```

See [templates and effective members](README.md#templates-and-effective-members).


## Adding and removing

A charging station template is a child of its operator, added and removed as an entity:

```csharp
RoamingNetworkChange.Add   (chargingStationTemplate)      // the parent is the operator its identification names
RoamingNetworkChange.Remove(chargingStationTemplateId)
```

A template a station names can not be removed: update the station first, in the same change set.


## Validation

- `@id` is mandatory and a valid charging station template identification.
- `maxPower` must not be negative; `brands`, `vehicleTypes`, `euVehicleCategories`, `features` and `parkingSpaceIds`
  must not name a value twice, an image URL must not be given twice; `brandIds` are refused.
- In its operator: the template belongs to the operator its identification names; a template must not be given
  twice; `chargingTariffId` must be a tariff of the operator; every effective charging station naming it must be valid.
- In its roaming network: `manufacturerId` must name a manufacturer and `parkingSpaceIds` parking spaces of the network.
- A change set applies each change to the embedded JSON of the operator and reads the operator again, so these
  rules hold after every single change.
- `Add(chargingStationTemplate)` refuses a parent other than the operator; the template must not exist yet. It gets
  the change set's `createdAt` as `lastChange`, and as `created` unless given.


## Example

```json
{
  "@id":            "DE*GEF*STWALLBOX22",
  "name":           { "en": "ACME Wallbox-22" },
  "manufacturerId": "ACME",
  "modelId":        "Wallbox-22",
  "hotline":        "+49 800 4242424",
  "features":       [ "StatusInfoAvailable" ],
  "maxPower":       22000,
  "created":        "2026-01-01T00:00:00.000Z",
  "lastChange":     "2026-10-11T12:00:00.000Z"
}
```
