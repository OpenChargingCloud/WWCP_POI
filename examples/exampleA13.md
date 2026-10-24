# Example A13: who checks the meter – transparency software and its certificate

The energy meter of a charging station signs its meter values; a **transparency software** checks them for the driver,
and a **conformity assessment body** certifies which software may do so for which station model. In POI 3 each says
so itself: the transparency software manufacturer `GraphDefined` states its release of Chargy, signed by its own key,
the PTB, notified body for the Measuring Instruments Directive (MID), states its certificate, signed by its own key.
The BSI, for the Cyber Resilience Act (CRA), is added without keys: it does not take part yet, and the roaming network
operator would sign in its place. `DE*GEF` names release and certificate for the energy meters of its DC stations, and
the station proof of the driver's app gives them ([TransparencySoftwareManufacturer](../docs/TransparencySoftwareManufacturer.md),
[ConformityAssessmentBody](../docs/ConformityAssessmentBody.md)).

| Revision of epoch 2 | Signed by | |
|---|---|---|
| 0 to 3 | see [example A11](exampleA11.md) and [example A12](exampleA12.md) | |
| 4 | the quorum of `OCC`, and the new identity keys of `GraphDefined` and the PTB as proofs | the transparency software manufacturer `GraphDefined`, the conformity assessment bodies PTB, with its keys, and BSI, without |
| 5 | `GraphDefined`, its key for `software` | Chargy 1.5.0 |
| 6 | the PTB, its key for `certificates` | `PTB-2026-0042`, under the MID: Chargy 1.5.0 verified for the `GD-DC300` |
| 7 | `DE*GEF`, its `infrastructure` key | its DC energy meters: Chargy 1.5.0 under the German calibration law, with `PTB-2026-0042` |

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


## 3. The PTB certifies Chargy for the DC stations of GraphDefined

The certificate `PTB-2026-0042` verifies Chargy 1.5.0 for the charging station model `GD-DC300`, version 1.0, of the
charging station manufacturer `GraphDefined`, under the `MID`, with the URL of its document. It names the manufacturer,
the model and the version: the network checks that `GraphDefined` stated the model with that version, as it did in
[example A2](exampleA2.md#4-the-manufacturers-state-their-models); a certificate for a model or a version its
manufacturer does not have is refused. Under the `CRA`, which the PTB is not notified for, it is refused before
anybody signs it.

```
A certificate of the PTB under the CRA: Change 0: The given JSON representation of a conformity assessment body is invalid: The transparency software certificate 'PTB-2026-0042' is issued under the legislation 'CRA', which the conformity assessment body 'PTB' is not notified for!
Committed, revision 6 of epoch 2: PTB-2026-0042: Chargy 1.5.0 for the GD-DC300
```


## 4. DE*GEF names them for its DC energy meters

`DE*GEF` changes its energy meter template `DE*GEF*EMT_DC`: every DC energy meter naming it is checked by Chargy 1.5.0
under the German calibration law, as `PTB-2026-0042` certifies. Its `infrastructure` key of epoch 2 signs it.

```csharp
RoamingNetworkChange.UpdateProperty(dcMeters, "transparencySoftwareStatus", null,
                                    new JArray(new TransparencySoftwareAssignment(chargyId, LegalStatus.GermanCalibrationLaw, certificateId).ToJSON(Embedded: true)))
```

```
Committed, revision 7 of epoch 2: The DC energy meters: Chargy 1.5.0 under the German calibration law, PTB-2026-0042
```


## 5. The station proof the app gets

The server of `QA` attests revision 7, and the app gets the station proof of `DE*GEF*E1*1`, as in
[example A10](exampleA10.md), published as `published/QA-DE-GEF-E1-1-proof.json`. It is large: the keys and
attestations of epoch 2 are ML-DSA.

```
The station proof of DE*GEF*E1*1, revision 7: 139 KiB
```


## 6. Who checks the meter

`Validate()` checks the proof against the trust anchor of epoch 2 and reads, for the energy meter of `DE*GEF*E1*1`, the
release and its manufacturer, the certificate and its body, from the proof alone. The proof gives the manufacturer
of the station with its model, as `GraphDefined` stated it, and none of its other models: the certificate is for that
model, in one of its versions. The PTB has keys of its own, so it signed its certificate itself; the BSI is not given, as no meter of the
station names a certificate of it. A certificate for another model changes the root of the proof: its attestations no
longer attest it.

```
The station proof, revision 7, by its attestations: valid
DE*GEF*M1*1, the energy meter of DE*GEF*E1*1: germanCalibrationLaw
  Chargy Transparency Software 1.5.0, by GraphDefined GmbH, manufacturer, supported until 2031-10-24T13:37:35.335Z, vulnerabilities to security@graphdefined.example, bill of materials https://www.graphdefined.example/chargy/1.5.0/sbom.cdx.json
  PTB-2026-0042 for the GD-DC300 1.0, under the MID, by the PTB, notified body 0102 for MID; signed by itself: yes, it has keys of its own
  the station DE*GEF*S1 is a GD-DC300 of GraphDefined, GraphDefined DC 300, versions 1.0/1.1: the model the certificate is for, in one of its versions
Conformity assessment bodies the proof gives: PTB
The certificate for another model: Valid attestations of trusted keys of 0 holders attest the root effectiveTree:sha256:hex:b96df761… of the proof, not of 2!
```


## Limits

- POI proves who states something, not that it is true: the certificate is the PTB's statement, the CRA properties are
  those of `GraphDefined`. Whoever doubts them asks them, e.g. by the URL of the document.
- The names, numbers and URLs of the PTB, the BSI and `GraphDefined` are examples, not those of real bodies, documents
  and contacts. Which bodies are notified under the CRA is up to the member states.
- A certificate is one for a charging station model; one under the CRA concerns the software itself, which the
  certificate does not cover yet. So the BSI issues none here.
- Whether the PTB has keys tells whether it signed its certificate itself; which key of it signed which commit, the
  commits tell, not the station proof.
- The transparency software checking the meter values the meter signs is not part of the example.
