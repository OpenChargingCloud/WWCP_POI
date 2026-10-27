# Example A14: the papers of a meter – declaration, certificates of conformity and calibration certificate, digital

An energy meter comes with papers: the **EU declaration of conformity** of its model, which its manufacturer issues under
its sole responsibility (Decision 768/2008/EC, Annex III), the **certificates of conformity** of a notified body, e.g.
the EU-type examination certificate of the model under the Measuring Instruments Directive (MID) and the approval of the
quality system its manufacturer produces it under, and, for the single device, a **calibration certificate** of a
laboratory. The German initiative QI-Digital makes them digital: the digital calibration certificate (DCC) of the PTB,
the digital certificate of conformity (D-CoC) of the PTB and NoBoMet, and a digital EU declaration of conformity
(DDoC). POI 3 has them as a proof of concept in the JSON of the roaming network, each signed by its issuer with one of
its keys of the roaming network ([QIDigital](../WWCP_POI3/RoamingNetwork/QIDigital/)). They are no part of `QA`: their
issuers hand them out, and anybody checks them against `QA`, its parties and their keys, the manufacturers and their
models, and the meter as `QA` names it.

`SAALE` declares the conformity of its AC meter model `SM-3`, the PTB states its EU-type examination certificate and
approves the quality system of `SAALE`, and a calibration laboratory, `EM-LAB`, which `OCC` adds to `QA` first, states
the calibration of the `SM-3` of `DE*GEF*E2*1`, the AC EVSE of Jena Paradies.

| | Signed by | |
|---|---|---|
| revisions 0 to 7 of epoch 2 | see [example A11](exampleA11.md) to [example A13](exampleA13.md) | |
| revision 8 of epoch 2 | the quorum of `OCC`, and the new identity key of `EM-LAB` as the proof | the calibration laboratory `EM-LAB` |
| `DE-26-M-PTB-0108`, a D-CoC | the PTB, its key for `certificates` | module B under the MID: the `SM-3` of `SAALE`, hardware 1.4, firmware 1.0.7 |
| `DE-26-QS-PTB-0042`, a D-CoC | the PTB, its key for `certificates` | module D under the MID: the quality system of `SAALE`, scope MI-003, no model |
| `DoC-SM-3-2026-01`, a DDoC | `SAALE`, its key for `models` | the `SM-3` under the MID, the EMC and RoHS directives, with `DE-26-M-PTB-0108` and `DE-26-QS-PTB-0042` of the notified body `0102` |
| `DCC-2026-0815`, a DCC | `EM-LAB`, its key for `certificates` | the energy meter `DE*GEF*M2*1` of `DE*GEF*E2*1`, an `SM-3` |

The code is [exampleA14.cs](csharp/exampleA14.cs), called by [Program.cs](csharp/Program.cs) with what example A11 left:

```csharp
ExampleA14.Run(a11);

ExampleA14.Validate();
```

The private keys of `EM-LAB` are kept in `EM-LAB/`, ML-DSA-65 like those of the PTB. The PTB signs with its key of
[example A13](exampleA13.md), `SAALE` with its key of [example A2](exampleA2.md), each read from the PEM file its
organization keeps it in. The documents are published as `published/PTB-DE-26-M-PTB-0108.json`,
`published/PTB-DE-26-QS-PTB-0042.json`, `published/SAALE-DoC-SM-3-2026-01.json` and
`published/EM-LAB-DCC-2026-0815.json`. The output is what one run printed.


## 1. The roaming network operator adds a calibration laboratory

A calibration certificate is issued by a conformity assessment body of the network, the laboratory it names. `EM-LAB`
is accredited for calibrations, and no notified body: it names no number and no legislation. It makes an identity key,
a key for `certificates` and a key for `party`, its own properties. Two holders of `OCC` sign the change set adding it,
its new identity key signs as the proof that it holds it, as for the PTB in example A13.

```csharp
var lab = new ConformityAssessmentBody(
              labId,
              I18NString.Create(Languages.en, "Jena Energy Meter Calibration Laboratory"),
              Homepage:      URL.Parse("https://www.em-lab.example"),
              IdentityKeys:  [ POIPublicKey.From(labIdentityKey.Private) ],
              SigningKeys:   [ POIPublicKey.From(labCertificatesKey.Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Certificates ]),
                               POIPublicKey.From(labPartyKey.       Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Party        ]) ]
          );
```

```
Committed, revision 8 of epoch 2: EM-LAB, a calibration laboratory
```


## 2. The PTB states the EU-type examination certificate of the SM-3 and approves the quality system of SAALE

Under the MID, the EU-type examination of a model (module B) goes with a module for its production: `SAALE` produces its
meters under a quality system the PTB approved and surveils (module D). The PTB states both.

`DE-26-M-PTB-0108` is a certificate of module B, the EU-type examination, under the MID, of the PTB as the notified
body `0102`, for the model `SM-3` of `SAALE` in the hardware revision 1.4 with the firmware 1.0.7, as `SAALE` stated
them in [example A2](exampleA2.md#4-the-manufacturers-state-their-models). It names the standards applied, the
characteristics it confirms in the units of the D-SI (`\ampere`, `\volt`), its conclusion, the condition of its
validity and the URL of its document; it is valid for ten years. Its date of issue is when it is signed.

```csharp
var typeExamination = new DigitalCertificateOfConformity(
                          typeExaminationId,
                          DCoCModule.B,
                          QIDigitalLegislation.MID,
                          ExampleA13.ptbId,
                          "0102",
                          QIDigitalModel.Of(ExampleA2.acMeterMakerId, sm3, [ HardwareRevision.Parse("1.4") ], [ FirmwareVersion.Parse("1.0.7") ]),
                          ValidFrom:        now,
                          ValidUntil:       now.AddYears(10),
                          Standards:        [ new QIDigitalStandard("EN 50470-1:2006"), new QIDigitalStandard("EN 50470-3:2022") ],
                          Characteristics:  [ new QIDigitalQuantity(63, @"\ampere", RefType: "mid_maximumCurrent"), … ],
                          …);
```

Signed by the key of the PTB for `party`, it is refused: a certificate needs a key that may sign `certificates`. The
PTB signs it with that one and checks it against `QA` before it hands it out.

```csharp
typeExamination = Signed(typeExamination, "PTB/signing-certificates-privateKey.pem", COSEAlgorithm.MLDsa65);
```

`DE-26-QS-PTB-0042` is a certificate of module D, the approval of the quality system of `SAALE` for production, final
product inspection and testing. The approval belongs to `SAALE`, not to the `SM-3`: it names the manufacturer, of the
kind its model has, an energy meter manufacturer, and the scope of its quality system, the category MI-003 of the MID,
the active electrical energy meters (`QIDigitalManufacturer.Categories`), and no model, hardware revision or firmware
version. A certificate of module D, D1, E, E1, H or H1 names a manufacturer, one of module B, F, F1 or G a model:
another one is refused when it is made and when it is read. It is valid for three years, subject to the surveillance of
the notified body, and signed by the same key for `certificates`.

```csharp
var qualitySystem = new DigitalCertificateOfConformity(
                        qualitySystemId,
                        DCoCModule.D,
                        QIDigitalLegislation.MID,
                        ExampleA13.ptbId,
                        "0102",
                        QIDigitalManufacturer.Of(ExampleA2.acMeterMakerId, [ QIDigitalManufacturer.Categories.MI003 ]),
                        ValidFrom:    now,
                        ValidUntil:   now.AddYears(3),
                        Conditions:   [ ImmutableI18NString.Create(Languages.en, "Subject to the surveillance of the notified body.") ],
                        …);

qualitySystem     = Signed(qualitySystem, "PTB/signing-certificates-privateKey.pem", COSEAlgorithm.MLDsa65);
```

Its JSON names the manufacturer where a certificate of a model names its model:

```json
"manufacturer": {
  "type": "EnergyMeterManufacturer",
  "manufacturerId": "SAALE",
  "scope": [
    "MI-003"
  ]
},
```

```
Signed by the key of the PTB for itself: The signing key 'e8402c9e739c2db5aaefb2c7640f10107908d89d7b575b2500470e3e138cfea3' of the conformity assessment body 'PTB' may not sign 'certificates', only 'party'!
The PTB: DE-26-M-PTB-0108, module B under the MID, for the SM-3 of SAALE, signed with ML-DSA-65
The PTB: DE-26-QS-PTB-0042, module D under the MID, for the quality system of SAALE, scope MI-003, signed with ML-DSA-65
```


## 3. SAALE declares the conformity of the SM-3

`DoC-SM-3-2026-01` names the model, the legislations it conforms to, the MID, the EMC and the RoHS directives, the
standards applied, the notified body with its number, for each of its modules the certificate it issued,
`DE-26-M-PTB-0108` of module B and `DE-26-QS-PTB-0042` of module D, and where and by whom it is signed for `SAALE`.
Signed by the roaming network operator, it is refused: the declaration is the manufacturer's. `SAALE` signs it with its
key for `models`, the ES256 key of example A2, and checks it against `QA` and against both certificates it names.

```csharp
var declaration = new DigitalDeclarationOfConformity(
                      declarationId,
                      QIDigitalModel.Of(ExampleA2.acMeterMakerId, sm3, [ HardwareRevision.Parse("1.4") ], [ FirmwareVersion.Parse("1.0.7") ]),
                      [ QIDigitalLegislation.MID, QIDigitalLegislation.EMC, QIDigitalLegislation.RoHS ],
                      "Halle (Saale)",
                      new QIDigitalPerson("Jane Doe", "Managing director"),
                      Standards:       [ new QIDigitalStandard("EN 50470-1:2006"), new QIDigitalStandard("EN 50470-3:2022") ],
                      NotifiedBodies:  [ new DDoCNotifiedBody(ExampleA13.ptbId, "0102", DCoCModule.B, typeExaminationId, …),
                                         new DDoCNotifiedBody(ExampleA13.ptbId, "0102", DCoCModule.D, qualitySystemId,   …) ],
                      …);

if (!declaration.TryVerify(qa, out error, now) || !declaration.TryVerify(typeExamination, out error) || !declaration.TryVerify(qualitySystem, out error))
    Helpers.Fail(error);
```

```
Signed by the roaming network operator: The key 'e7666bc83698261695ef3102e4e06f1045edd2004145f06e54c332955e80ef2b' of the digital declaration of conformity 'DoC-SM-3-2026-01' is no signing key of the energy meter manufacturer 'SAALE'!
SAALE: DoC-SM-3-2026-01, the SM-3 under the MID, EMC, RoHS, naming DE-26-M-PTB-0108 and DE-26-QS-PTB-0042, signed with ES256
```


## 4. EM-LAB calibrates the meter of DE*GEF*E2*1

`EM-LAB` calibrates the meter at the site of its customer, `DE*GEF`. The item of `DCC-2026-0815` is the meter as `QA`
names it at runtime, `QIDigitalItem.Of(meter)`: its own serial number, and the manufacturer, the model, the hardware
revision and the firmware version its energy meter template `DE*GEF*EMT_AC` gives it in
[example A3](exampleA3.md). Its measurement results are the error of the active energy at four currents, each with its
expanded uncertainty and the maximum permissible error of class B as its limits, under the temperature measured, by the
method of EN 50470-3; a statement of conformity, the responsible persons, the main signer among them, and when the meter
is to be calibrated again follow. It is signed by the laboratory's key for `certificates`.

```csharp
var calibration = new DigitalCalibrationCertificate(
                      calibrationId,
                      labId,
                      QIDigitalItem.Of(meter),
                      now.AddHours(-3),
                      now.AddHours(-1),
                      [ new QIDigitalPerson("Dr. Erika Mustermann", "Head of laboratory", MainSigner: true), … ],
                      [ new DCCMeasurementResult(ImmutableI18NString.Create(Languages.en, "Measurement error of the active energy"),
                                                 [ LoadPoint("Imin", 0.25m, 0.42m, 1.5m), … ], …) ],
                      PerformanceLocation:        DCCPerformanceLocation.Customer,
                      CalibrationLaboratoryCode:  "D-K-12345-01-00",
                      …,
                      RecalibrationDue:           now.AddYears(8)
                  ).Sign(labCertificatesKey.Private, POIPublicKey.From(labCertificatesKey.Private).Id, labCertificatesKey.Algorithm);
```

```
EM-LAB: DCC-2026-0815, the energy meter 'DE*GEF*M2*1', the SM-3 of SAALE of serial number ACM-2026-002-1, signed with ML-DSA-65
```

`QA` and its commit are published again, of revision 8, with `EM-LAB`.


## 5. The papers of the meter, checked

`Validate()` checks `QA` as published against the trust anchor of epoch 2, as in [example A11](exampleA11.md), and reads
each document from its file:

- the certificates of conformity: each of a conformity assessment body of `QA` with its number, notified for its
  legislation; valid now, and signed by a key of its body that may sign `certificates` and was valid when it signed.
  The model of the type examination one of its manufacturer, in hardware revisions and firmware versions it has; the
  manufacturer of the approval of the quality system a manufacturer of `QA` of the kind it names, an energy meter
  manufacturer;
- the declaration: signed by a key of the manufacturer of its model that may sign `models`; each notified body it names
  one of `QA` with its number; and each certificate it names, picked by its identification, of that body, its module,
  under one of its legislations: the type examination for its model in all its hardware revisions and firmware
  versions, the approval for the quality system of the manufacturer of its model, of its kind, with energy meters in
  its scope;
- the calibration certificate: signed by a key of its laboratory that may sign `certificates`; its item the meter of
  `QA` as it is at runtime, of the same manufacturer, model, serial number, hardware revision and firmware version: a
  meter changed since is no longer the one calibrated. Each measurement within its limits, and both certificates of
  conformity cover the meter: the type examination as one of its model, in one of its hardware revisions and firmware
  versions, the approval as an energy meter of `SAALE`, of a kind in its scope, MI-003.

```
QA, revision 8 of epoch 2: checked against the trust anchor of epoch 2
DE-26-M-PTB-0108, by the PTB, notified body 0102: valid
  module B, under the MID (Directive 2014/32/EU), for the SM-3 of SAALE, hardware 1.4, firmware 1.0.7
  The type SM-3 meets the essential requirements of the MID, Annex I and Annex V (MI-003), accuracy class B.
DE-26-QS-PTB-0042, by the PTB, notified body 0102: valid
  module D, under the MID (Directive 2014/32/EU), for the quality system of SAALE, scope MI-003, until 2029-10-27
  The quality system of SAALE for production, final product inspection and testing meets the MID, Annex II, module D, for MI-003. Subject to the surveillance of the notified body.
DoC-SM-3-2026-01, by SAALE, the manufacturer of the SM-3: valid
  under the MID (Directive 2014/32/EU), EMC (Directive 2014/30/EU), RoHS (Directive 2011/65/EU); signed in Halle (Saale) by Jane Doe, Managing director
  DE-26-M-PTB-0108 of the notified body 0102, the PTB, module B: the certificate it names
  DE-26-QS-PTB-0042 of the notified body 0102, the PTB, module D: the certificate it names
DCC-2026-0815, by EM-LAB, D-K-12345-01-00: valid
  the energy meter 'DE*GEF*M2*1', the SM-3 of SAALE of serial number ACM-2026-002-1, hardware 1.4, firmware 1.0.7: as QA names it
  Imin  0.25 A  +0.42 % ± 0.08 % (k = 2), limits ± 1.5 %: Pass
  Itr   0.5 A   -0.18 % ± 0.08 % (k = 2), limits ± 1.0 %: Pass
  Iref  5 A     +0.06 % ± 0.08 % (k = 2), limits ± 1.0 %: Pass
  Imax  63 A    -0.21 % ± 0.08 % (k = 2), limits ± 1.0 %: Pass
  passes: True; recalibration due 2034-10-27; signed by Dr. Erika Mustermann, Head of laboratory
  DE-26-M-PTB-0108 covers it: its model, hardware revision and firmware version
  DE-26-QS-PTB-0042 covers it: an energy meter of SAALE, of MI-003
```


## 6. What is refused

a) The calibration certificate with another error at Imax, changed after it was signed: its signature is not the one
of its content. b) The certificates of conformity for the meter of `DE*GEF*E1*1`, a `KDM-500` of `KERNBERG`: the type
examination covers the `SM-3` only, the approval of the quality system the meters of `SAALE` only.

```
Another error at Imax: The signature of the digital calibration certificate 'DCC-2026-0815' is invalid: The signature is invalid!
DE-26-M-PTB-0108 for the meter of DE*GEF*E1*1: The energy meter 'DE*GEF*M1*1' is no 'SM-3' of 'SAALE'!
DE-26-QS-PTB-0042 for the meter of DE*GEF*E1*1: The energy meter 'DE*GEF*M1*1' is none of 'SAALE'!
```


## Limits

- The documents are no part of `QA`, of its change sets or of the station proof: who has a meter's papers, gets them
  from its issuers or its operator, not from `QA`. A meter, a model or a conformity certificate of `QA`
  ([example A13](exampleA13.md)) naming them, and their digests, is open.
- They are checked against `QA` as it is now. A calibration certificate of a meter whose firmware was updated since is
  refused, as it should be; one of a revision checked later, e.g. at a checkpoint, needs that version of `QA`.
- `SAALE` signs with its ES256 key of example A2: the documents are no commits, and the algorithms of epoch 2 do not bind
  them. Whether they should, is open.
- The structure follows the PTB DCC 3.x and the D-CoC of NoBoMet where the network needs them, in JSON, not their XML,
  and the signature is one of POI, not XAdES. The documents do not replace the ones the law asks for.
- The scope of a quality system is checked as far as its categories are known: MI-003 takes energy meters, another
  category any kind. Whether a meter was produced under the quality system, and while it was approved, the documents do
  not tell.
- The names, numbers, persons, URLs and measurement results of `EM-LAB`, the PTB and `SAALE` are examples, not those of
  real bodies, documents and people.
