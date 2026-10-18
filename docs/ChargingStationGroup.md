# ChargingStationGroup

A charging station group names [charging stations](ChargingStation.md), e.g. all stations of a site type, of a
contract or of a partner network. It gives them nothing: what a station is, it has of its own and of its
[templates](ChargingStationTemplate.md). Groups are for sets that do not follow from properties: for a status of their
own, for searches, or to name the EVSEs of a [charging tariff offer](ChargingTariffOffer.md). A group does not embed
its members, it names them: groups may overlap, and a station may be in no group or in several.

Two parties have charging station groups, under `chargingStationGroups`:

- a [ChargingStationOperator](ChargingStationOperator.md), of its own charging stations;
- an [EMobilityProvider](EMobilityProvider.md), of charging stations of any operator, e.g. for its offers.

The group belongs to the party embedding it: every change of it, and every `Add`/`Remove` of it, must be signed
with a signing key of that party. The identification is `{partyId}*SG{suffix}`, the identification of that party
followed by `*SG` and a suffix of up to 50 characters, starting with a letter, digit or `_`, followed by letters,
digits and `_ * - . € $`, e.g. `DE*GEF*SG1`. It is unique within the roaming network; two identifications are equal
when they differ only in case.

| | |
|---|---|
| Class | `ChargingStationGroup` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStation/Group/ChargingStationGroup.cs) |
| Identification | `ChargingStationGroup_Id`, e.g. `DE*GEF*SG1` |
| Admin status | `ChargingStationGroupAdminStatusType`, initially `Operational` |
| Status | `ChargingStationGroupStatusType`, initially `Available` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) or [EMobilityProvider](EMobilityProvider.md) as `chargingStationGroups` |
| Owner | the party embedding it (signing key) |


## Common properties

See [common properties](README.md#common-properties). A charging station group does not write `eTags`.

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
| `chargingStationIds` | `ChargingStationIds` | `ImmutableArray<ChargingStation_Id>` | update/remove | Its members; see [chargingStationIds](#chargingstationids). |

An empty `chargingStationIds` is not written.


## chargingStationIds

`chargingStationIds`: the members, each once, in the ordinal order of their identifications. The array is updated or
removed as a whole, e.g. `RoamingNetworkChange.UpdateProperty(group, "chargingStationIds", old, new)`, or
[element by element](README.md#element-changes).

- **A group of an operator** names charging stations of that operator, in any of its pools; a station of another
  operator is unknown there and refused. A station can not be removed while a group of its operator names it: update
  the group first, in the same change set.
- **A group of an e-mobility provider** names charging stations of any operator, by weak references: a station that
  does not exist (any more) is left out, and its operator removes it without the provider.


## Adding and removing

A charging station group is a child of the party embedding it, added and removed as an entity:

```csharp
RoamingNetworkChange.Add   (chargingStationGroup, partyId)      // a charging station operator or an e-mobility provider
RoamingNetworkChange.Remove(chargingStationGroupId)
```

A group a [charging tariff offer](ChargingTariffOffer.md) of the same party names can not be removed: update the offer
in the same change set. An offer of an e-mobility provider naming a group of an operator does not keep it.


## Runtime status

See [runtime status](README.md#runtime-status) for the mechanism. The group's status is set manually, or
calculated by a delegate from the status of its members, see [Calculated status of groups](README.md#calculated-status-of-groups).

- **Admin status** `ChargingStationGroupAdminStatusType`, initially `Operational` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Operational` (`operational`), `InternalUse` (`internalUse`), `OutOfService` (`outOfService`).
- **Status** `ChargingStationGroupStatusType`, initially `Available` since its creation:
  `Unknown` (`unknown`), `Unspecified` (`unspecified`), `Planned` (`planned`), `InDeployment` (`inDeployment`),
  `Offline` (`offline`), `Available` (`available`), `PartialAvailable` (`partialAvailable`), `Reserved` (`reserved`),
  `Charging` (`charging`), `OutOfService` (`outOfService`), `Error` (`error`).
- **Events:** `OnAdminStatusChanged`, `OnStatusChanged` `(timestamp, eventTrackingId, chargingStationGroup, oldStatus, newStatus)`.

In a history it is set and read by `history.RuntimeStatus` with the entity type `ChargingStationGroup`. It has
no energy status.


## Validation

- `@id` is mandatory, a valid charging station group identification naming the party embedding it, and unique in
  the roaming network.
- `chargingStationIds` must not name a station twice; a group of an operator names only stations of the operator.
- A change set applies each change to the embedded JSON of the party and reads it again, so these rules hold after
  every single change; that a group of an operator names only charging stations of the operator is checked after the last
  change, so a charging station moved by a `Remove` and an `Add` stays in its groups.
- `Add(chargingStationGroup, partyId)` refuses a parent other than a charging station operator or an e-mobility
  provider whose identification the group's names; the group must not exist yet. It gets the change set's
  `createdAt` as `lastChange`, and as `created` unless given.


## Example

```json
{
  "@id":                "DE*GEF*SG1",
  "name":               { "en": "Supermarket sites" },
  "chargingStationIds": [ "DE*GEF*S1", "DE*GEF*S2" ],
  "created":            "2026-01-01T00:00:00.000Z",
  "lastChange":         "2026-10-11T12:00:00.000Z"
}
```
