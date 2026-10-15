# EVSEGroup

An EVSE group names [EVSEs](EVSE.md) of one charging station operator, e.g. all EVSEs of a fleet depot, of a
product line or of a contract, and may name one charging tariff for all of them. It does not embed its members,
it names them: groups may overlap, and an EVSE may be in no group or in several. It is embedded in its
[ChargingStationOperator](ChargingStationOperator.md), under `EVSEGroups`.

The group belongs to the charging station operator its identification names: `DE*GEF*EG2` belongs to `DE*GEF`,
and every change of it, and every `Add`/`Remove` of it, must be signed with a signing key of that operator.
The identification is `{operatorId}*EG{suffix}`, the suffix up to 50 characters, starting with a letter, digit
or `_`, followed by letters, digits and `_ * - . € $`, e.g. `DE*GEF*EG2`. It is unique within the operator;
two identifications are equal when they differ only in case.

| | |
|---|---|
| Class | `EVSEGroup` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStation/EVSE/Group/EVSEGroup.cs) |
| Identification | `EVSEGroup_Id`, e.g. `DE*GEF*EG2` |
| Admin status | `EVSEGroupAdminStatusType`, initially `Operational` |
| Status | `EVSEGroupStatusType`, initially `Available` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) as `EVSEGroups` |
| Owner | the [ChargingStationOperator](ChargingStationOperator.md) its identification names (signing key) |


## Common properties

See [common properties](README.md#common-properties). An EVSE group does not write `eTags`.

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
| `EVSEIds` | `EVSEIds` | `ImmutableArray<EVSE_Id>` | update/remove | Its members; see [EVSEIds](#evseids). |
| `chargingTariffId` | `ChargingTariffId` | `ChargingTariff_Id?` | update/remove | The [charging tariff](ChargingTariff.md) of its operator for all its members, unless their own apply; it must exist at the operator. |

Absent values are left out; an empty `EVSEIds` is not written.


## EVSEIds

`EVSEIds`: the members, each once, in the ordinal order of their identifications. Every one must be an
[EVSE](EVSE.md) of the same operator, in any of its stations; an EVSE of another operator is unknown there and
refused. The array is updated or removed as a whole, e.g.
`RoamingNetworkChange.UpdateProperty(group, "EVSEIds", old, new)`. An EVSE can not be removed while a group
names it: update the group first, in the same change set.


## Adding and removing

An EVSE group is a child of its operator, added and removed as an entity:

```csharp
RoamingNetworkChange.Add   (evseGroup)      // the parent is the operator its identification names
RoamingNetworkChange.Remove(evseGroupId)
```

Nothing else names an EVSE group, so it can always be removed.


## Runtime status

See [runtime status](README.md#runtime-status) for the mechanism. The group's status is set manually, or
calculated by a delegate from the status of its members, see [Calculated status of groups](README.md#calculated-status-of-groups).

- **Admin status** `EVSEGroupAdminStatusType`, initially `Operational` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Operational` (`operational`), `InternalUse` (`internalUse`), `OutOfService` (`outOfService`).
- **Status** `EVSEGroupStatusType`, initially `Available` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Offline` (`offline`), `Available` (`available`), `PartialAvailable` (`partialAvailable`), `Reserved` (`reserved`),
  `Charging` (`charging`), `OutOfService` (`outOfService`), `Error` (`error`).
- **Events:** `OnAdminStatusChanged`, `OnStatusChanged` `(timestamp, eventTrackingId, evseGroup, oldStatus, newStatus)`.

In a history it is set and read by `history.RuntimeStatus` with the entity type `EVSEGroup`. It has no energy status.


## Validation

- `@id` is mandatory and a valid EVSE group identification.
- `EVSEIds` must not name an EVSE twice.
- In its operator: the group belongs to the operator its identification names; a group must not be given
  twice; every member must be an EVSE of the operator; `chargingTariffId` must be a tariff of the operator.
- A change set applies each change to the embedded JSON of the operator and reads the operator again, so these
  rules hold after every single change.
- `Add(evseGroup)` refuses a parent other than the operator; the group must not exist yet. It gets the change
  set's `createdAt` as `lastChange`, and as `created` unless given.


## Example

```json
{
  "@id":              "DE*GEF*EG2",
  "name":             { "en": "Depot Jena" },
  "EVSEIds":          [ "DE*GEF*E1*1", "DE*GEF*E1*2" ],
  "chargingTariffId": "DE*GEF*T3",
  "created":          "2026-01-01T00:00:00.000Z",
  "lastChange":       "2026-10-11T12:00:00.000Z"
}
```
