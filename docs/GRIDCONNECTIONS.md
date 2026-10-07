# Grid connections and pool energy meters

[Repository overview](../README.md) · [Architecture](ARCHITECTURE.md) · [JSON](JSON.md) · [ChangeSets](CHANGESETS.md)

## Ownership

| Owner | Property | Cardinality |
| --- | --- | --- |
| ChargingPool | EnergyMeters | 0..n directly owned meters |
| ChargingPool | GridConnectionPoint | 0..1 connection point |
| GridConnectionPoint | GridOperator | Exactly one non-null operator reference |
| GridConnectionPoint | EnergyMeter | 0..1 directly owned meter |
| ChargingStation | EnergyMeters | 0..n directly owned meters |
| EVSE | EnergyMeter | 0..1 directly owned meter |

Direct meter collections do not aggregate their descendants' meters. Meter IDs are unique
within each collection, compared without case sensitivity. Constructors reject null entries,
empty IDs and duplicates, and detach their inputs. Membership and static fields are immutable;
the stored meter instances retain mutable operational/admin statuses. `EnergyMeter.Role` can
identify connections such as `grid`, `pv` or `battery`.

`GridConnectionPoint` is a sealed immutable description. It always references a `GridOperator`
with a valid ID. When attached to a pool, its operator must belong to the same roaming network
if the pool has one. The pool captures an independent copy of the connection point, its operator
and its meter. `ChargingPool.Clone()` and network derivation preserve independent runtime histories.
Standalone operator references are not automatically added to the network's operator registry.

## Optional connection data

The electrical concepts follow the separation of connection voltage level, agreed active power
and agreed apparent power for import/export used in the [VDE FNN connection rules](https://www.vde.com/de/fnn/themen/tar/tar-mittelspannung-vde-ar-n-4110).
The field names below are WWCP's mapping of these concepts.

| C# property | JSON field | Meaning / unit |
| --- | --- | --- |
| Id | id | Connection-point identifier assigned by the utility |
| Name / Description | name / description | Immutable multilingual descriptions |
| Address / GeoLocation | address / geoLocation | Physical connection location |
| VoltageLevel | voltageLevel | LowVoltage, MediumVoltage, HighVoltage, ExtraHighVoltage |
| ConnectionType | connectionType | Existing GridConnectionTypes, e.g. AC_ThreePhases |
| NominalVoltage (Volt?) | nominalVoltage | V; line-to-line for three-phase connections |
| NominalFrequency (Hertz?) | nominalFrequency | Hz |
| ContractedImportPower (Watt?) | contractedImportPower | Agreed active power taken from the grid, W |
| ContractedExportPower (Watt?) | contractedExportPower | Agreed active power supplied to the grid, W |
| ContractedImportApparentPower (VoltAmpere?) | contractedImportApparentPower | Agreed apparent power taken from the grid, VA |
| ContractedExportApparentPower (VoltAmpere?) | contractedExportApparentPower | Agreed apparent power supplied to the grid, VA |
| ConnectionAgreementId | connectionAgreementId | Utility's connection agreement reference |
| NetworkLocationId | networkLocationId | Associated network-location identifier (e.g. NeLo) |
| MarketLocationIds | marketLocationIds | Associated market locations (e.g. MaLo for consumption/generation) |
| MeteringLocationIds | meteringLocationIds | Associated metering locations (e.g. MeLo / Zaehlpunktbezeichnung) |

A market location identifies where energy is produced or consumed, with its identifier assigned
by the grid operator ([Bundesnetzagentur: Marktlokation](https://www.bundesnetzagentur.de/SharedDocs/A_Z_Glossar/M/Marktlokation.html)).
Network, market and metering locations are separate concepts and can have multiple relationships
([Bundesnetzagentur: location bundle structures](https://www.bundesnetzagentur.de/DE/Beschlusskammern/BK06/BK6_83_Zug_Mess/835_mitteilungen_datenformate/Mitteilung_32/Anlagen/EDIFACT/Codeliste-Lokationsbuendelstrukturen_1_0.pdf?__blob=publicationFile&v=1)).
Consequently, market/metering identifiers are lists; they do not identify the physical meter's serial
number. Country-specific identifier syntax/checksums are not enforced by these optional strings.

Missing values mean unknown. Voltage/frequency must be positive when provided; power values
must be non-negative. Zero export power explicitly represents no agreed export capacity. If
both active and apparent ratings are supplied for one direction, active power must not exceed
apparent power. These are static agreed ratings; live measurements remain in the meter/runtime
model. No default voltage, frequency or rating is assumed. Identifier strings are trimmed,
must be non-empty, and lists reject nulls and duplicates.

Electrical properties and constructor parameters use Styx's immutable, decimal-based metrology
types (`Volt`, `Hertz`, `Watt`, `VoltAmpere`), available in `org.GraphDefined.Vanaheimr.Illias`.
Factories such as `Watt.FromKW(250m)` handle scale conversion explicitly. JSON writes unit-bearing
strings such as `"400 V"`, `"50 Hz"`, `"250 kW"` or `"300 kVA"`, with invariant decimal separators
and lossless precision. Numeric inputs and strings without units are rejected. Cloning preserves
these value types without conversion.

## Construction

```csharp
var network = new RoamingNetwork(RoamingNetwork_Id.Parse("network-a"));
var cpo = new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*ABC"), network);
var gridOperator = new GridOperator(GridOperator_Id.Parse("DE*NET"), network);

var point = new GridConnectionPoint(
    gridOperator,
    EnergyMeter: new EnergyMeter(EnergyMeter_Id.Parse("grid-meter"), Role: "grid"),
    Id: "connection-42",
    VoltageLevel: GridVoltageLevel.LowVoltage,
    ConnectionType: GridConnectionTypes.AC_ThreePhases,
    NominalVoltage: Volt.FromV(400m),
    NominalFrequency: Hertz.FromHz(50m),
    ContractedImportPower: Watt.FromKW(250m),
    ContractedExportPower: Watt.FromKW(100m));

var pool = new ChargingPool(
    ChargingPool_Id.Parse("DE*ABC*P1"), cpo,
    EnergyMeters: [new EnergyMeter(EnergyMeter_Id.Parse("pv-meter"), Role: "pv")],
    GridConnectionPoint: point);

var restored = ChargingPool.Parse(pool.ToJSON(), cpo);
```

## JSON, ChangeSets and runtime state

The pool serializes `energyMeters` as embedded meter objects and `gridConnectionPoint` as one
embedded object. The latter contains a required `gridOperator` document, including its `id`,
network reference, static data, metadata and current statuses; its `energyMeter` is optional.
`GridConnectionPoint.Parse/TryParse` and `GridOperator.Parse/TryParse` restore these documents.
Standalone parsing recreates the operator's network reference from `roamingNetworkId`; parsing
with a supplied network checks that ID and binds the operator to the supplied version.

Use pool `UpdateProperty` operations on `energyMeters` or `gridConnectionPoint`. Replace the whole
array/object; `[]` clears direct meters and JSON null removes the optional connection point.
Nested values remain pool properties in the existing eight-node ChangeSet graph. Domain validation
checks all supplied nested data before publishing a derived snapshot.

Prepare these operations with `pool.Operator.RoamingNetwork.CreateChangeSet(...)` or the source
snapshot's `CreateChangeSet(...)`. Each batch carries required before/after JSON and CBOR ETags
of the whole static network as `ImmutableArray<ETag>` values; both are checked when applied.
Each ETag is a readonly struct; JSON entries use format/algorithm/encoding/digest arrays and CBOR entries
use format/algorithm/digest-byte arrays. Meter/operator metadata participates
in the content profile independently of JSON's explicit `hex` or `base64` digest encoding. Both
encodings decode to the same bytes. Mutable operational/admin statuses are excluded. Missing static
timestamps in introduced nested nodes are initialized from the fixed batch timestamp.
See [ChangeSets](CHANGESETS.md#before-and-after-content-checks).

`ToJSONSnapshot()` overlays current statuses of direct meters and connection-point operator/meter
instances without changing POI revision or timestamps. `DataSnapshot.ToJSON()` exports the frozen
baseline. Unrelated ChangeSets capture independent copies of current histories and their capacities;
explicit replacement of a nested property keeps its supplied statuses.
