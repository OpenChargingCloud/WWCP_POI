# Example A13: who checks the meter – transparency software and the certificates of station and meter

The energy meter of a charging station signs its meter values; a **transparency software**, a product of its own
manufacturer, checks them for the driver. A **conformity assessment body** certifies a model of charging stations or
of energy meters, in hardware revisions and firmware versions, with its components: the transparency software it
verified, and of a station the meters it mounts. In POI 3 each says so itself: the transparency software manufacturer
`GraphDefined` states its release of Chargy, signed by its own key, the PTB, notified body for the Measuring
Instruments Directive (MID), states its certificates, signed by its own key. The BSI, for the Cyber Resilience Act
(CRA), is added without keys: it does not take part yet, and the roaming network operator would sign in its place.
`DE*GEF` names the certificates for its DC stations and their energy meters, and the legal status of the release for
the meters; the station proof of the driver's app gives them ([TransparencySoftwareManufacturer](../docs/TransparencySoftwareManufacturer.md),
[ConformityAssessmentBody](../docs/ConformityAssessmentBody.md)).

| Revision of epoch 2 | Signed by | |
|---|---|---|
| 0 to 3 | see [example A11](exampleA11.md) and [example A12](exampleA12.md) | |
| 4 | the quorum of `OCC`, and the new identity keys of `GraphDefined` and the PTB as proofs | the transparency software manufacturer `GraphDefined`, the conformity assessment bodies PTB, with its keys, and BSI, without |
| 5 | `GraphDefined`, its key for `software` | Chargy 1.5.0 |
| 6 | the PTB, its key for `certificates` | `PTB-2026-0042`, under the MID: the `GD-DC300` with the `KDM-500` and Chargy 1.5.0; `PTB-2026-0017`: the `KDM-500` with Chargy 1.5.0 |
| 7 | `DE*GEF`, its `infrastructure` key | its DC stations: `PTB-2026-0042`; their energy meters: `PTB-2026-0017`, Chargy 1.5.0 under the German calibration law |

The code is [exampleA13.cs](csharp/exampleA13.cs), called by [Program.cs](csharp/Program.cs) with what example A11 left:

```csharp
ExampleA13.Run(a11);

ExampleA13.Validate();
```

The private keys of `GraphDefined` are kept in `GraphDefined/`, those of the PTB in `PTB/`. All keys are ML-DSA-65, as
epoch 2 takes no others. The output is what one run printed.


## 1. The roaming network operator adds them

`GraphDefined` makes an identity key and two signing keys, one that may sign `software`, its releases, and one that
may sign `party`, its own properties. It names what the CRA asks of it: its role, `manufacturer`, where vulnerabilities
are reported, its policy on coordinated vulnerability disclosure and its security advisories. The PTB makes an
identity key and a key for `certificates`; it names its number as a notified body, `0102`, and that it is notified for
the `MID`. The BSI, notified for the `CRA` in this example, has no keys.

```csharp
var ptb = new ConformityAssessmentBody(
              ptbId,
              I18NString.Create(Languages.de, "Physikalisch-Technische Bundesanstalt"),
              Homepage:            URL.Parse("https://www.ptb.de"),
              NotifiedBodyNumber:  "0102",
              NotifiedFor:         [ ConformityAssessmentBody.Legislations.MID ],
              IdentityKeys:        [ POIPublicKey.From(ptbIdentityKey.Private) ],
              SigningKeys:         [ POIPublicKey.From(ptbCertificatesKey.Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Certificates ]),
                                     POIPublicKey.From(ptbPartyKey.       Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Party        ]) ]
          );
```

Who is a member is the roaming network operator's: two holders of `OCC` sign the change set, and the new identity keys
of `GraphDefined` and of the PTB sign it as well, as the proof that they hold them.

```
Committed, revision 4 of epoch 2: GraphDefined, transparency software manufacturer, the PTB and the BSI, conformity assessment bodies
```


## 2. GraphDefined states its release of Chargy

The release names the end of its support period, its EU declaration of conformity and its software bill of materials.
Signed by the roaming network operator, it is refused: the release is its manufacturer's. Signed by the `software` key of
`GraphDefined`, it is committed.

```csharp
var addChargy = history.Head.RoamingNetwork.CreateChangeSet([ RoamingNetworkChange.Add(chargy, softwareMakerId) ], …);
```

```
Signed by the roaming network operator: Change 0: Add of the TransparencySoftware 'chargy-1.5.0' needs a signature by a signing key of the TransparencySoftwareManufacturer 'GraphDefined'!
Committed, revision 5 of epoch 2: Chargy 1.5.0
```


## 3. The PTB certifies the DC stations of GraphDefined and their meters, with Chargy

The certificate `PTB-2026-0042` is for the charging station model `GD-DC300` of `GraphDefined`, in its hardware
revision 1.0 with the firmware version 2.5.0, under the `MID`, with the URL of its document. It names the components
it was issued with: Chargy 1.5.0, verified, and the energy meter `KDM-500` of `KERNBERG` the station mounts, in its
hardware revision 2.1 with the firmware 3.2. The certificate `PTB-2026-0017` is for the energy meter model `KDM-500`
itself, in the same revision and firmware, with Chargy 1.5.0 as its component, without a legislation. The network
checks that the manufacturers stated the models with these revisions and firmware versions, as they did in
[example A2](exampleA2.md#4-the-manufacturers-state-their-models); a certificate for a model, a revision or a firmware
version its manufacturer does not have is refused. Under the `CRA`, which the PTB is not notified for, the certificate
of the station is refused before anybody signs it.

```csharp
ConformityCertificate CertificateUnder(String Legislation)
    => new (certificateId, ExampleA2.stationMakerId, ChargingStationModel_Id.Parse("GD-DC300"), [ "1.0" ], [ "2.5.0" ], [ chargyId ], [],
            …,
            Legislation:   Legislation,
            EnergyMeters:  [ new CertifiedEnergyMeter(ExampleA2.dcMeterMakerId, kdm500, [ "2.1" ], [ "3.2" ]) ]);

var meterCertificate = new ConformityCertificate(meterCertificateId, ExampleA2.dcMeterMakerId, kdm500, [ "2.1" ], [ "3.2" ], [ chargyId ], [], …);
```

```
A certificate of the PTB under the CRA: Change 0: The given JSON representation of a conformity assessment body is invalid: The certificate 'PTB-2026-0042' is issued under the legislation 'CRA', which the conformity assessment body 'PTB' is not notified for!
Committed, revision 6 of epoch 2: PTB-2026-0042 for the GD-DC300 with the KDM-500, PTB-2026-0017 for the KDM-500, each with Chargy 1.5.0
```


## 4. DE*GEF names them for its DC stations and their energy meters

`DE*GEF` changes its charging station template `DE*GEF*ST_DC`: every DC station naming it names `PTB-2026-0042`, and
its energy meter template `DE*GEF*EMT_DC`: every DC energy meter naming it names `PTB-2026-0017` and is checked by
Chargy 1.5.0 under the German calibration law. The legal status names no certificate: the certificates naming the
release among their components prove it. The network checks each certificate against what names it, as it is with
what its templates give it: the model and manufacturer, the hardware revision and the firmware version, and that the
meters of a station are among the energy meters its certificate names. Its `infrastructure` key of epoch 2 signs it.

```csharp
RoamingNetworkChange.UpdateProperty(dcStations, "certificateIds",             null, new JArray(certificateId.ToString())),
RoamingNetworkChange.UpdateProperty(dcMeters,   "certificateIds",             null, new JArray(meterCertificateId.ToString())),
RoamingNetworkChange.UpdateProperty(dcMeters,   "transparencySoftwareStatus", null, new JArray(new TransparencySoftwareAssignment(chargyId, LegalStatus.GermanCalibrationLaw).ToJSON(Embedded: true)))
```

```
Committed, revision 7 of epoch 2: The DC stations: PTB-2026-0042; their energy meters: PTB-2026-0017, Chargy 1.5.0 under the German calibration law
```


## 5. The station proof the app gets

The server of `QA` attests revision 7, and the app gets the station proof of `DE*GEF*E1*1`, as in
[example A10](exampleA10.md), published as `published/QA-DE-GEF-E1-1-proof.json`. It is large: the keys and
attestations of epoch 2 are ML-DSA.

```
The station proof of DE*GEF*E1*1, revision 7: 142 KiB
```


## 6. Who checks the meter

`Validate()` checks the proof against the trust anchor of epoch 2 and reads, from the proof alone, the station of
`DE*GEF*E1*1` and its energy meter, each with its model as its manufacturer stated it and none of its other models,
the certificates they name with their body, and the release of the meter with its manufacturer. Each certificate is for
the model, the hardware revision and the firmware version of what names it, and names its components; the legal status
of Chargy is proven by both certificates, which name it. The PTB has keys of its own and states its certificates
itself, so they name no `statedBy`; the BSI is not given, as neither the station nor its meter names a certificate of it. A certificate for another
model changes the leaves of the proof: they no longer lead to the root its attestations attest.

```
The station proof, revision 7, by its attestations: valid
The charging station DE*GEF*S1: a GD-DC300 of GraphDefined, GraphDefined DC 300, hardware 1.0, firmware 2.5.0
  PTB-2026-0042, under the MID, by the PTB, notified body 0102 for MID; stated by itself
    for the GD-DC300 of GraphDefined, hardware 1.0, firmware 2.5.0: the model, hardware revision and firmware version of the charging station
    with chargy-1.5.0 verified, the KDM-500 of KERNBERG, hardware 2.1, firmware 3.2
The energy meter DE*GEF*M1*1: a KDM-500 of KERNBERG, Kernberg DC meter 500 A, hardware 2.1, firmware 3.2
  PTB-2026-0017, by the PTB, notified body 0102 for MID; stated by itself
    for the KDM-500 of KERNBERG, hardware 2.1, firmware 3.2: the model, hardware revision and firmware version of the energy meter
    with chargy-1.5.0 verified
DE*GEF*M1*1, the energy meter of DE*GEF*E1*1: Chargy Transparency Software 1.5.0, germanCalibrationLaw, as PTB-2026-0042 and PTB-2026-0017 name it
  by GraphDefined GmbH, manufacturer, supported until 2031-10-26T00:44:35.984Z, vulnerabilities to security@graphdefined.example, bill of materials https://www.graphdefined.example/chargy/1.5.0/sbom.cdx.json
Conformity assessment bodies the proof gives: PTB
The certificate for another model: roamingNetwork.conformityAssessmentBodies.@leaves[0].@entity.certificates.@leaves[1]: it leads to another root than the leaves before!
```


## Limits

- POI proves who states something, not that it is true: the certificate is the PTB's statement, the CRA properties are
  those of `GraphDefined`. Whoever doubts them asks them, e.g. by the URL of the document.
- The names, numbers and URLs of the PTB, the BSI and `GraphDefined` are examples, not those of real bodies, documents
  and contacts. Which bodies are notified under the CRA is up to the member states.
- A certificate is one for a model of charging stations or of energy meters; one under the CRA concerns the software
  itself, which no certificate is for. So the BSI issues none here.
- `PTB-2026-0017` names no legislation: a DC meter is no measuring instrument of the MID, and the legislation it is
  certified under is not one the PTB names in `notifiedFor` in this example.
- A certificate tells who states it: one the roaming network operator states in place of a body without keys names
  `statedBy`, also once the body has keys; which key signed which commit, the commits tell, not the station proof.
- The transparency software checking the meter values the meter signs is not part of the example.
