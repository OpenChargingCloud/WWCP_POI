# Shared domain entities and value boundaries

[Repository overview](../README.md) · [Owned graph](GRAPH.md) · [JSON](JSON.md) · [ETags and CBOR](ETAGS-CBOR.md)

## Grid operators

The network owns each `GridOperator` once in `gridOperators`. A pool's optional connection point
contains the required `gridOperatorId`. Parsing and pool construction resolve the canonical registry
instance; all points using that ID share the same description and operational/admin schedules in
that network version. Operator registration must precede point creation. Changing a point does
not implicitly register or edit an operator.

Graph operations edit the registered operator. The reverse reference index prevents deletion
while points still reference it. Runtime delivery uses `POIRuntimeTarget.GridOperator(id)`.
Derived network versions own independent operator schedules; all their points resolve the new
registry instance. Connection-point meters retain their own scoped lifetimes.

## Transparency software and certificates

A `TransparencySoftware` identifies a software release using `TransparencySoftware_Id`. Its
immutable description contains name, version, license, vendor and links. A distinct release can
have a distinct ID even if its display name is equal. IDs are opaque, case-sensitive strings;
no identity is inferred from names, versions, URLs or hashes.

`TransparencySoftwareCertificate` identifies one approval or compatibility document using
`TransparencySoftwareCertificate_Id`. It describes the station model **and its version**, rather
than an individual installed station. Its fields are:

| Property | Meaning |
| --- | --- |
| `Id` / `@id` | Stable identity of the document |
| `Issuer` | Organization/person issuing the document |
| `ChargingStationModel`, `ChargingStationModelVersion` | Model/version covered by the document |
| `ChargingStationManufacturerId` | Optional reference to a network-owned manufacturer |
| `VerifiedTransparencySoftwareIds` | Releases stated to have been verified with that model/version |
| `CompatibleTransparencySoftwareIds` | Additional releases stated to be compatible, without being in the verified list |
| `DocumentNumber`, `DocumentURL` | Optional source document reference and absolute location |
| `NotBefore`, `NotAfter` | Optional UTC-normalized validity interval with tick precision |

At least one software ID must be present. Both lists reject duplicate/empty IDs and must be
disjoint. A reversed validity interval is rejected. The network owns descriptions once in
`transparencySoftware` and `transparencySoftwareCertificates`; all software/manufacturer
references must resolve within it. These are immutable graph nodes with graph operations,
ETags and JSON/CBOR support. Tariff value structures do not acquire IDs through this change.

A meter's `TransparencySoftwareStatus` remains a nested immutable assignment with a legal label,
a software ID and optional document ID. The assignment array allows one entry per software ID.
Parsing and meter attachment resolve the shared objects in the target network, including when
constructors receive values from another version. An assigned document must cover that release
in either list.
Updating a document revalidates consuming assignments, and surviving references prevent deletion.
Legal labels remain extensible and are not inferred from certificate presence. Verified versus
compatible is explicitly recorded by the document's two lists.

Example fragment of a complete network document:

```json
{
  "@id": "network-a",
  "transparencySoftware": [
    {
      "@id": "verifier-2",
      "name": "Meter verifier",
      "version": "2.0",
      "vendor": "Example software vendor",
      "openSourceLicense": {
        "@id": "MIT",
        "description": { "en": "MIT License" },
        "URLs": ["https://opensource.org/licenses/MIT"]
      }
    }
  ],
  "transparencySoftwareCertificates": [
    {
      "@id": "approval-42",
      "issuer": "Example inspection organization",
      "chargingStationModel": "Station X",
      "chargingStationModelVersion": "3.1",
      "verifiedTransparencySoftwareIds": ["verifier-2"],
      "compatibleTransparencySoftwareIds": [],
      "documentNumber": "42/2026"
    }
  ]
}
```

A meter references it with
`{ "transparencySoftwareId": "verifier-2", "legalStatus": "verified", "certificateId": "approval-42" }`.
Standalone meter/assignment parsing needs the network context when such references are present;
complete network parsing supplies it. Certificate parsing also needs context to resolve its IDs.
Software release parsing itself needs no registry context.

The document models the described approval or compatibility evidence. It is separate from
cryptographic/X.509 certificates and ChangeSet signatures. The library validates structure,
references and software coverage. It does not authenticate the source document or automatically
match an installed station to the declared model/version; a deployment-version model and that
application policy remain separate work. Validity is stored as static evidence, so passage of
clock time does not silently change content hashes or legal labels.

## Parking

A parking operator directly owns garages, spaces, sensors, space groups and parking products.
A space can reference one `ParkingGarageId`; the garage does not become its owner. Groups list
`ParkingSpaceIds` and may overlap. Garage/space/group `ParkingProductIds` select available offers.
Every garage/space/sensor/product reference must stay within the same parking operator; station
links can reference charging stations elsewhere in the network.

`GetAvailableParkingProducts(spaceId)` returns the deduplicated union of directly assigned offers,
offers of its optional garage and offers of all containing groups, sorted by product ID. An unknown
space is rejected. The result does not add prices, pick a winning offer or define precedence.
`ParkingProduct` currently contains an ID and optional minimum/cutoff durations in SI seconds.
Durations cannot be negative, and the minimum cannot exceed the cutoff when both are supplied.
It does not introduce a parking price calculation engine.

These graph nodes use Add/Remove/property operations. Identified membership/product arrays use
addressed element operations, and optional scalar references use property operations. References
are indexed for deletion protection and merge conflict detection. Remove surviving references
before deleting a target in an ordered batch.

## Values without independent IDs

Tariff elements, price components and restrictions remain immutable value substructures. Their
arrays use whole-property replacement, without array-index addressing or invented IDs. A meter's
legal-status assignment is also a nested value, although its referenced software and document
have independent graph identities. Its array is replaced as a value on the selected meter.

## Content profile and verification state

These ownership/reference changes define **`wwcp-poi-static-v2`**. Complete network ETags hash each
catalog description through its owned location and references through the consumer's data. Thus
an operator/software/document edit changes network ETags even when a point/meter reference ETag
remains equal. Runtime statuses remain outside static content.

Earlier tagged static-v1 exports and archives are rejected. Recreate exports, batches and
signatures against v2; there is no mixed-profile conversion. Current state/commit/signature/
snapshot/retention JSON and CBOR reference artifacts have been regenerated from fixed inputs.
Release library, test and benchmark projects compile. Dedicated coverage now passes **73 cases**
for shared references, transports, validation, atomic failures, parking, merges and runtime continuity.
The full static-v2 suite passes **1,025 tests**, with zero failures and one ordinary worker skipped.
See [executed model verification](VERIFICATION-DOMAIN-MODEL.md) for commands, results and fixes.
That run predates streaming archive changes; those have separate
[build/measurement evidence](STREAMING-ARCHIVES.md). The later
[streaming verification](VERIFICATION-STREAMING.md) reruns this model fixture and the full suite:
1,174 passed, zero failures and one ordinary worker skipped. Earlier
performance reports retain their original static-v1 baseline.

The later [domain recovery workload package](DOMAIN-RECOVERY-WORKLOADS.md) composes these
shared catalogs, meter slots, nested tariffs and parking relations with signed edits, explicit
branch merge, all four archive profiles and independent local runtime. Its 81 new cases and
2,920-case full Release run preserve the same 32 references and production binary. Four measured
recovery stages establish a rich synthetic baseline; pricing decisions and production capacity
remain outside this fixture.

### Subsequent temporary validation projection reuse

[Validation projections](VALIDATION-PROJECTION.md) reuse successful ancestors and one station
view per fixed immutable map. Every ordinary parser/reference/trust check and failure order
remains; failed values are uncached and runtime stays private to the pass. The separate tariff
group text/length/empty-flag correction preserves ordering across equivalent operator formats.
71 new cases and all 2,659 interoperability / 2,991 full Release cases pass with unchanged
fixed references. The exact rich baseline/harness/dependencies support matched recovery
measurements; earlier performance reports remain historical. Normalized immutable map
preparation is completed below; larger catalogs and production concurrency remain work.

### Subsequent normalized immutable map preparation

[Normalized snapshot maps](SNAPSHOT-MAP-PREPARATION.md) reuse exactly equal properties, child sets,
entity/map branches and root catalogs after the full ordinary parser and normalization. Binding
reads stable typed IDs directly; all reference/current peer checks, exact errors, independent
runtime, canonical content and atomic publication remain. Removed identities/consumers are handled.
53 new cases and all 2,712 interoperability / 3,044 full Release cases pass with unchanged references.
Matched rich recovery measurements keep the exact archives, branches, peers and C# harness/dependencies.
Earlier reports remain historical. Larger group/catalog/history and concurrency workloads are next.
