# EnergyMeter

An energy meter measures the energy of what embeds it: a whole charging pool, a grid connection point
(e.g. the grid meter), a charging station, or a single EVSE (e.g. the calibration-law meter of a charge point).
It is described by its role, its manufacturer and model, serial number and versions, by how it signs its
measurements and the public keys it signs them with, and by the transparency software releases that can verify its
signed measurements, with their legal status.

An energy meter is an entity with admin status and status, but it is **not addressed by change sets on its own**:
it is embedded completely in what it measures and changes as a property of that ([below](#where-energy-meters-live)).
Pools, stations and EVSEs belong to a [ChargingStationOperator](ChargingStationOperator.md), which therefore owns
their meters and signs their changes; the meter of a grid connection point belongs to the maintainer of the point
([GridOperator](GridOperator.md#maintainer)).

The identification is an `EnergyMeter_Id`, any nonempty text, trimmed, compared case-insensitively, e.g.
`grid-meter` or `1ESY1161229886`. It is unique within the whole roaming network, not only within its embedder.

| | |
|---|---|
| Class | `EnergyMeter` — [source](../WWCP_POI3/RoamingNetwork/EnergyMeterManufacturer/EnergyMeter/EnergyMeter.cs) |
| Identification | `EnergyMeter_Id`, e.g. `grid-meter` |
| Admin status | `EnergyMeterAdminStatusType`, initially `operational` |
| Status | `EnergyMeterStatusType`, initially `available` |
| Embedded in | [ChargingPool](ChargingPool.md) as `energyMeters`, a [grid connection point](GridOperator.md#grid-connection-points) as `energyMeter`, [ChargingStation](ChargingStation.md) as `energyMeters`, [EVSE](EVSE.md) as `energyMeter` |
| Owner | the charging station operator owning the embedding pool, station or EVSE; the maintainer of the embedding grid connection point |
| HTTP | no route of its own; embedded in `GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}`, `…/CSs/{CS_Id}` and `…/EVSEs/{EVSE_Id}` |


## Common properties

See [README.md](README.md#common-properties). No `eTags`.

"via parent" means: the property changes only when the embedding property (`energyMeters`, `energyMeter`)
is replaced or removed as a whole ([below](#change-sets)).

| JSON key | Change set | Notes |
|---|---|---|
| `@id` | via parent | |
| `name` | via parent | I18N text; omitted when empty |
| `description` | via parent | I18N text; omitted when empty |
| `dataSource` | via parent | |
| `created` | via parent | taken as given in the new value; a change set does not set it |
| `lastChange` | via parent | taken as given in the new value; a change set sets only the embedder's `lastChange` |
| `customData` | via parent | a JSON object; omitted when empty |


## Properties

| JSON key | C# property | Type | Change set | Description |
|---|---|---|---|---|
| `role` | `Role` | `EnergyMeterRole` | via parent | What it measures; predefined `grid`, `gridConnectionPoint`, `pool`, `station`, `evse`, `pv`, `wind`, `battery`, read in any case and written so, any other nonempty text accepted. |
| `manufacturerId` | `ManufacturerId` | `EnergyMeterManufacturer_Id` | via parent | Its manufacturer, which must be an energy meter manufacturer of the roaming network ([below](#manufacturerid)). |
| `modelId` | `ModelId` | `EnergyMeterModel_Id` | via parent | Its model, as its manufacturer names it; nonempty text, not checked against anything. |
| `modelURL` | `ModelURL` | URL | via parent | The homepage of its model, e.g. with its data sheet. |
| `serialNumber` | `SerialNumber` | string | via parent | |
| `hardwareVersion` | `HardwareVersion` | string | via parent | |
| `firmwareVersion` | `FirmwareVersion` | string | via parent | |
| `transparencySoftwareStatus` | `TransparencySoftwareStatus` | [TransparencySoftwareAssignment](#transparencysoftwarestatus)[] | via parent | The transparency software releases for its measurements, with their legal status; ordered by `transparencySoftwareId` (ordinal), at most one per release. Omitted when empty. |
| `signatureMethods` | `SignatureMethods` | [SignatureMethod](#signaturemethods)[] | via parent | How it signs its measurements, e.g. `OCMF`; ordered by name (ordinal), each once. Omitted when empty. |
| `publicKeys` | `PublicKeys` | [EnergyMeterPublicKey](#publickeys)[] | via parent | The public keys it signs its measurements with, with their certificate chains; ordered by `@id` (ordinal), each once. Omitted when empty. |
| `templateIds` | `TemplateIds` | `ImmutableArray<EnergyMeterTemplate_Id>` | via parent | The [energy meter templates](EnergyMeterTemplate.md) it takes, each once, in its order, the first deciding; see [templates and groups](#templates-and-groups). |

All are optional and omitted when absent.

**Derived (not serialized)** — the embedders collect their meters: `ChargingPool.AllEnergyMeters` (its own, its
stations' and EVSEs'), `ChargingStation.AllEnergyMeters`, `EVSE.AllEnergyMeters`, `ChargingStationOperator.EnergyMeters`,
`GridOperator.EnergyMeters` (those of its grid connection points), `RoamingNetwork.EnergyMeters` with `TryGetEnergyMeterById`.


## Where energy meters live

| Embedder | JSON key | Cardinality | Changed by |
|---|---|---|---|
| [ChargingPool](ChargingPool.md) | `energyMeters` | 0..n, ordered by `@id` (ordinal) | `UpdateProperty(pool, "energyMeters", old, new JArray(…))`, `RemoveProperty(pool, "energyMeters")` |
| [grid connection point](GridOperator.md#grid-connection-points) of a grid operator | `energyMeter` | 0..1 | `UpdateProperty(point, "energyMeter", old, meter.ToJSON())`, `RemoveProperty(point, "energyMeter")`, signed by its maintainer |
| [ChargingStation](ChargingStation.md) | `energyMeters` | 0..n, ordered by `@id` (ordinal) | `UpdateProperty(station, "energyMeters", …)`, `RemoveProperty(station, "energyMeters")` |
| [EVSE](EVSE.md) | `energyMeter` | 0..1 | `UpdateProperty(evse, "energyMeter", old, meter.ToJSON())`, `RemoveProperty(evse, "energyMeter")` |

- A pool's `energyMeters` are its own, not those of its stations, and not that of the grid connection point it names.
- The arrays are written only when not empty, and are read only completely: `energyMeterIds` is refused.
- There is no `RoamingNetworkChange.Add`/`Remove` of an energy meter, and no entity type `EnergyMeter` in change sets.
  Adding, changing or removing a meter replaces the embedding property; a three-way merge compares that property
  as a whole.


## Templates and groups

An energy meter names [energy meter templates](EnergyMeterTemplate.md) in `templateIds`: of the charging station
operator of its pool, station or EVSE, or, of a grid connection point, of its grid operator. A template may give it
`role`, `manufacturerId`, `modelId`, `modelURL`, `hardwareVersion`, `firmwareVersion`, `transparencySoftwareStatus`
and `signatureMethods`, but never `serialNumber` or `publicKeys`: a value of its own wins, else the first template's;
arrays are united. The meter keeps only its own; it takes the others at runtime, as the effective energy meter the
effective EVSEs, charging stations, charging pools and grid connection points embed.

[Energy meter groups](EnergyMeterGroup.md) of the same owners collect energy meters and give them nothing. A meter
still named by a group after the last change of a change set can not be removed: one of a charging station moved to
another pool stays in its groups.


## Change sets

A change of a meter is a change of its embedder, owned and signed by that embedder's charging station operator:

```csharp
var meter = new EnergyMeter(EnergyMeter_Id.Parse("evse-meter-2"),
                            Role:            EnergyMeterRole.EVSE,
                            ManufacturerId:  EnergyMeterManufacturer_Id.Parse("ACME"),
                            SerialNumber:    "1ESY1161229886");

var cs = rn1.CreateChangeSet([
             RoamingNetworkChange.UpdateProperty(evse, "energyMeter", null, meter.ToJSON())
         ]).
         Sign(gefSigningKey, POIPublicKey.From(gefSigningKey).Id, COSEAlgorithm.ES256);
```

Give the new value as `ToJSON()` writes it, with `created` and `lastChange`: the change set does not set them for
embedded meters. The runtime status of a meter is kept by its identification and goes along when its embedder
changes.


## manufacturerId

A reference to an `EnergyMeterManufacturer` of the roaming network (its `energyMeterManufacturers`, owned by the
RNO; documented in [RoamingNetwork](RoamingNetwork.md#energymetermanufacturers)). The manufacturer must exist in the
version the meter is in; an energy meter manufacturer still named by a meter can not be removed.


## transparencySoftwareStatus

`TransparencySoftwareAssignment` ([source](../WWCP_POI3/RoamingNetwork/TransparencySoftware/TransparencySoftwareAssignment.cs)):
the legal status of one transparency software release for this meter, optionally proven by a certificate. The
release is embedded in its [TransparencySoftwareManufacturer](TransparencySoftwareManufacturer.md#transparencysoftware),
the certificate in the [ConformityAssessmentBody](ConformityAssessmentBody.md#transparencysoftwarecertificates) that
issued it.

| JSON key | Type | Description |
|---|---|---|
| `transparencySoftwareId` | string | Mandatory: a `TransparencySoftware_Id` of the network, compared case-insensitively. At most once per meter. |
| `legalStatus` | string | Mandatory: a `LegalStatus`; predefined `forInformationOnly`, `verified`, `germanCalibrationLaw`, read in any case and written so, any other nonempty text accepted. |
| `certificateId` | string | Optional: a `TransparencySoftwareCertificate_Id` of the network. The certificate must cover the release, i.e. name it among its verified or compatible releases. |

The array is ordered by `transparencySoftwareId` (ordinal) whatever the given order.


## signatureMethods

`SignatureMethod` ([source](../WWCP_POI2/RoamingNetwork/EnergyMeterManufacturer/EnergyMeter/SignatureMethod.cs)):
how the meter signs its measurements, the format of its signed meter values and how they are signed, as a
transparency software verifies them. The algorithm and the curve are those of its [public keys](#publickeys). A
meter may sign in several ways, e.g. OCMF and a format of its own.

Predefined are the energy meter signature formats [Chargy](https://github.com/OpenChargingCloud/ChargyCore.NET)
verifies: `OCMF`, `EDL40`, `AlfenCrypt01`, `AlfenCrypt03`, `ChargePointCrypt01`, `EMHCrypt01`, `MennekesCrypt01`
and `PCDF`; read in any case and written so, any other nonempty text accepted, e.g. a format no transparency software
knows yet. Two that differ in case only are the same.


## publicKeys

`EnergyMeterPublicKey` ([source](../WWCP_POI3/RoamingNetwork/EnergyMeterManufacturer/EnergyMeter/EnergyMeterPublicKey.cs)):
a public key the meter signs with. Unlike the keys of the parties, it is not a COSE key: energy meters sign with
curves COSE does not know, e.g. secp192r1 or secp224k1. It is the DER encoded SubjectPublicKeyInfo [RFC 5280],
which names its algorithm and curve itself; an app or a transparency software shows it as the meter shows it, e.g.
as hexadecimal coordinates.

| JSON key | Type | Description |
|---|---|---|
| `@id` | string | Mandatory: the SHA-256 of the DER encoded SubjectPublicKeyInfo, 64 lowercase hexadecimal digits; a reader calculates it and refuses another one. |
| `key` | string | Mandatory: the DER encoded SubjectPublicKeyInfo as canonical Base64. |
| `certificateChain` | string[] | Optional: the certificate chain of the key, each certificate DER encoded as canonical Base64: first the one of the key, then each one followed by the one that issued it, e.g. by the manufacturer of the meter. Omitted when empty. |

`Algorithm` (not serialized) names the curve for people, as meters and transparency software name it, e.g.
`secp256r1`, `secp192r1`, `brainpoolP256r1`, else `Ed25519`, `Ed448`, `RSA` or the object identifier of the
algorithm.


## Runtime status

See [README.md](README.md#runtime-status). Runtime status entity type: `EnergyMeter`, addressed by its identification
(e.g. `history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, "EnergyMeter", "grid-meter", "offline", …)`).

- **Admin status** `EnergyMeterAdminStatusType`: `unknown`, `unspecified`, `planned`, `inDeployment`, `operational`,
  `internalUse`, `outOfService`. Initially `operational` since `created`.
- **Status** `EnergyMeterStatusType`: `unknown`, `unspecified`, `inDeployment`, `offline`, `available`, `error`.
  Initially `available` since `created`.
- No energy status.
- Events: `OnAdminStatusChanged` (`OnEnergyMeterAdminStatusChangedDelegate`) and `OnStatusChanged`
  (`OnEnergyMeterStatusChangedDelegate`).


## Validation

- `@id` mandatory, a nonempty `EnergyMeter_Id`.
- Within one pool's or one station's `energyMeters`: no null entries, no identification twice.
- Network level: an identification at most once in the whole roaming network, across all pools, grid connection
  points, stations and EVSEs of all operators.
- `manufacturerId`, when given, names an energy meter manufacturer of the network.
- `transparencySoftwareStatus`: a JSON array of objects, each with `transparencySoftwareId` and `legalStatus`;
  a release at most once; every release and certificate exists in the network; a given certificate covers its release.
- `signatureMethods`: an array of nonempty texts, none twice, in any case.
- `publicKeys`: an array of objects, none twice; a key a valid public key of a known algorithm, DER encoded, with
  its SHA-256 as `@id`; the first certificate of a chain the one of the key, each one issued by the next one, all DER
  encoded; Base64 canonical.
- `modelURL` a valid URL; texts are texts.


## Example

An EVSE's meter, as `energyMeter`:

```json
{
  "@id": "1ESY1161229886",
  "role": "evse",
  "manufacturerId": "ACME",
  "modelId": "ACME-3000",
  "modelURL": "https://meters.example.org/acme-3000",
  "serialNumber": "1ESY1161229886",
  "hardwareVersion": "1.2",
  "firmwareVersion": "4.7.1",
  "transparencySoftwareStatus": [
    { "transparencySoftwareId": "chargy-1.5.0", "legalStatus": "germanCalibrationLaw", "certificateId": "PTB-2026-001" }
  ],
  "signatureMethods": [ "OCMF" ],
  "publicKeys": [
    { "@id": "b19e015397ed17785f274eee8706d2be6474a2ebc50c91d322a6f89cc894a72b",
      "key": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAENtBxZdTUSRc5xrOyDhLLD43S2ku36d+UsniiIMiba63u9X2602GxJ+tnOVYQJ8pmA83ya2qKu3oBa+hwYFTINA==" }
  ],
  "created": "2026-01-01T00:00:00.000Z",
  "lastChange": "2026-01-01T00:00:00.000Z"
}
```
