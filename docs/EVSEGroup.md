# EVSEGroup

An EVSE group names [EVSEs](EVSE.md), e.g. all EVSEs of a fleet depot, of a contract or of a partner network. It
gives them nothing: what an EVSE is, it has of its own and of its [templates](EVSETemplate.md). Groups are for sets
that do not follow from properties: for a status of their own, for searches, or to name the EVSEs of a
[charging tariff offer](ChargingTariffOffer.md). A group does not embed its members, it names them: groups may
overlap, and an EVSE may be in no group or in several.

Two parties have EVSE groups, under `EVSEGroups`:

- a [ChargingStationOperator](ChargingStationOperator.md), of its own EVSEs;
- an [EMobilityProvider](EMobilityProvider.md), of EVSEs of any operator, e.g. for its offers.

The group belongs to the party embedding it: every change of it, and every `Add`/`Remove` of it, must be signed
with a signing key of that party. The identification is `{partyId}*EG{suffix}`, the identification of that party
followed by `*EG` and a suffix of up to 50 characters, starting with a letter, digit or `_`, followed by letters,
digits and `_ * - . € $`, e.g. `DE*GEF*EG2`. It is unique within the roaming network, so that a group of an operator
and a group of a provider with the same identification can not both exist; two identifications are equal when they
differ only in case.

| | |
|---|---|
| Class | `EVSEGroup` — [source](../WWCP_POI3/RoamingNetwork/ChargingStationManufacturer/ChargingStation/EVSE/Group/EVSEGroup.cs) |
| Identification | `EVSEGroup_Id`, e.g. `DE*GEF*EG2` |
| Admin status | `EVSEGroupAdminStatusType`, initially `Operational` |
| Status | `EVSEGroupStatusType`, initially `Available` |
| Embedded in | [ChargingStationOperator](ChargingStationOperator.md) or [EMobilityProvider](EMobilityProvider.md) as `EVSEGroups` |
| Owner | the party embedding it (signing key) |


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

An empty `EVSEIds` is not written.


## EVSEIds

`EVSEIds`: the members, each once, in the ordinal order of their identifications. The array is updated or removed as
a whole, e.g. `RoamingNetworkChange.UpdateProperty(group, "EVSEIds", old, new)`, or
[element by element](README.md#element-changes), e.g. `AddElement("EVSEGroup", groupId, [ new ("EVSEIds", evseId) ], evseId)`.

- **A group of an operator** names EVSEs of that operator, in any of its stations; an EVSE of another operator is
  unknown there and refused. An EVSE can not be removed while a group of its operator names it: update the group
  first, in the same change set.
- **A group of an e-mobility provider** names EVSEs of any operator, by weak references: an EVSE that does not exist
  (any more) is left out, and its operator removes it without the provider.


## Adding and removing

An EVSE group is a child of the party embedding it, added and removed as an entity:

```csharp
RoamingNetworkChange.Add   (evseGroup, partyId)      // a charging station operator or an e-mobility provider
RoamingNetworkChange.Remove(evseGroupId)
```

A group a [charging tariff offer](ChargingTariffOffer.md) of the same party names can not be removed: update the offer
first, in the same change set. An offer of an e-mobility provider naming a group of an operator does not keep it.


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

- `@id` is mandatory, a valid EVSE group identification naming the party embedding it, and unique in the roaming
  network.
- `EVSEIds` must not name an EVSE twice; a group of an operator names only EVSEs of the operator.
- A change set applies each change to the embedded JSON of the party and reads it again, so these rules hold after
  every single change.
- `Add(evseGroup, partyId)` refuses a parent other than a charging station operator or an e-mobility provider whose
  identification the group's names; the group must not exist yet. It gets the change set's `createdAt` as
  `lastChange`, and as `created` unless given.


## Example

```json
{
  "@id":        "DE*GEF*EG2",
  "name":       { "en": "Depot Jena" },
  "EVSEIds":    [ "DE*GEF*E1*1", "DE*GEF*E1*2" ],
  "created":    "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-10-11T12:00:00.000Z"
}
```
