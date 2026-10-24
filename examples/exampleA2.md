# Example A2: the charging station operator `DE*GEF` asks to be added to `QA`

The roaming network `QA` of [example A1](exampleA1.md) gets its first parties: the charging station operator `DE*GEF`,
GraphDefined Charging Solutions, and the manufacturers its charging stations and their energy meters will name, which
`QA` does not know yet: `GraphDefined`, GraphDefined Charging Stations, and the energy meter manufacturers `SAALE`,
Saale Metering, of its AC meters, and `KERNBERG`, Kernberg DC Metering, of its DC meters. Each makes its own keys and
keeps them. `DE*GEF` makes the change set adding all four to the version of `QA` it checked, and each signs it with its
identity keys: the proof that it holds them, and that it asks to be added. The roaming network operator `OCC` decides
on new members: two of its three holders sign the change set as well, and it is committed. Then the manufacturers
state their models, which the stations and meters of `DE*GEF` will name, with the keys of their own.

| Revision | Signed by | Commit |
|---|---|---|
| 0 | `OCC`, as the RNO | Genesis: the roaming network `QA`, see [example A1](exampleA1.md) |
| 1 | `OCC`, as the RNO; its change set by the identity keys of `DE*GEF` and of the three manufacturers, and two holders of `OCC` | `Add` of the charging station operator `DE*GEF`, of the charging station manufacturer `GraphDefined` and of the energy meter manufacturers `SAALE` and `KERNBERG`, each with the public halves of its keys |
| 2 | `GraphDefined`, `KERNBERG` and `SAALE`, each with its `models` key | `Add` of the models `GD-DC300` and `GD-AC44` of `GraphDefined`, `KDM-500` of `KERNBERG` and `SM-3` of `SAALE` |

From then on `DE*GEF` adds its data itself, with change sets it signs: [example A3](exampleA3.md). The code is
[exampleA2.cs](csharp/exampleA2.cs), called by [Program.cs](csharp/Program.cs) with what example A1 left:

```csharp
var a2 = ExampleA2.Run(a1);

ExampleA2.Validate();
```

The output is what one run printed, `…` for elided keys, signatures and digests. Below, [with the hub](#with-the-hub):
the same at the prompt, in the web interface and over HTTP.


## 1. `DE*GEF` and its manufacturers make their keys

Two identity keys of two kinds, the long-term keys which sign a change of its keys: should one kind of key be broken one
day, the other still signs. And a signing key for each usage (`POIKeyUsages`, section 4.1): its charging pools,
stations and EVSEs; its tariffs and the prices it signs at runtime; and the party itself, its own properties. A key
stolen from the system publishing the prices then signs no charging pools.

```csharp
var now                   = Timestamp.Now;
var identityKeys          = new[] {
                                COSEKeyPair.NewES512(),
                                COSEKeyPair.NewEd448()
                            };
var signingKeys           = new Dictionary<String, COSEKeyPair> {
                                { POIKeyUsages.Infrastructure, COSEKeyPair.NewES256() },
                                { POIKeyUsages.Tariffs,        COSEKeyPair.NewES256() },
                                { POIKeyUsages.Party,          COSEKeyPair.NewES256() }
                            };

// The private keys stay with DE*GEF, where nobody else may read them.
Directory.CreateDirectory("DE-GEF");
for (var number = 1; number <= identityKeys.Length; number++)
    Helpers.SaveKey(identityKeys[number - 1].Private, $"DE-GEF/identity{number}-privateKey.pem");

foreach (var usage in signingKeys.Keys)
    Helpers.SaveKey(signingKeys[usage].Private, $"DE-GEF/signing-{usage}-privateKey.pem");

// What the roaming network operator gets: the operator as POI writes it, with the public halves of its keys.
var gef                   = new ChargingStationOperator(
                                gefId,
                                Name:          I18NString.Create(Languages.en, "GraphDefined Charging Solutions"),
                                Description:   I18NString.Create(Languages.en, "GraphDefined the first Charging Station Operator on the Open Charging Cloud"),
                                SigningKeys:   [
                                                   POIPublicKey.From(signingKeys[POIKeyUsages.Infrastructure].Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Infrastructure ]),
                                                   POIPublicKey.From(signingKeys[POIKeyUsages.Tariffs].       Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Tariffs, POIKeyUsages.PriceStatements ]),
                                                   POIPublicKey.From(signingKeys[POIKeyUsages.Party].         Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Party ])
                                               ],
                                IdentityKeys:  [ .. identityKeys.Select(identityKey => POIPublicKey.From(identityKey.Private)) ]
                            );

File.WriteAllText("DE-GEF/operator.json", gef.ToJSON().ToString());
```

`DE-GEF/operator.json`, with the public halves only:

```json
{
  "@id":         "DE*GEF",
  "@context":    "https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingStationOperator/v1",
  "name":        { "en": "GraphDefined Charging Solutions" },
  "description": { "en": "GraphDefined the first Charging Station Operator on the Open Charging Cloud" },
  "signingKeys": [
    { "@id": "1040f738…", "key": "pAECIAEh…", "notAfter": "2027-10-24T15:12:04.800Z", "usages": [ "party" ] },
    { "@id": "613f0ffa…", "key": "pAECIAEh…", "notAfter": "2027-10-24T15:12:04.800Z", "usages": [ "priceStatements", "tariffs" ] },
    { "@id": "be516d16…", "key": "pAECIAEh…", "notAfter": "2027-10-24T15:12:04.800Z", "usages": [ "infrastructure" ] }
  ],
  "identityKeys": [
    { "@id": "1922a7fb…", "key": "pAECIAMh…" },
    { "@id": "5fc456af…", "key": "owEBIAch…" }
  ],
  "created":    "2026-10-24T15:12:04.909Z",
  "lastChange": "2026-10-24T15:12:04.909Z"
}
```

The private keys stay in `DE-GEF/`: `identity1-privateKey.pem` (P-521), `identity2-privateKey.pem` (Ed448) and
`signing-infrastructure-…`, `signing-tariffs-…`, `signing-party-privateKey.pem` (P-256).

Then the manufacturers its stations and the energy meters of its EVSEs will name, see
[ChargingStationManufacturer](../docs/ChargingStationManufacturer.md) and
[EnergyMeterManufacturer](../docs/EnergyMeterManufacturer.md). Each is a party of its own, embedded in the roaming
network as `chargingStationManufacturers` and `energyMeterManufacturers`, and states its models itself; a station or an
energy meter can name only a manufacturer of the network and one of its models, so they come with `DE*GEF`. Each makes
an identity key and two signing keys, one for its own properties, one for its models, the private halves in a folder
of its own, `GraphDefined/stations-…`, `SAALE/meters-…`, `KERNBERG/meters-…` (`MakerKeys.New`, in
[exampleA2.cs](csharp/exampleA2.cs)):

```csharp
var stationMakerKeys      = MakerKeys.New("GraphDefined", "stations");
var acMeterMakerKeys      = MakerKeys.New("SAALE",        "meters");
var dcMeterMakerKeys      = MakerKeys.New("KERNBERG",     "meters");

var manufacturers         = new[] {
                                RoamingNetworkChange.Add(new ChargingStationManufacturer(
                                                             stationMakerId,
                                                             Name:          I18NString.Create(Languages.en, "GraphDefined Charging Stations"),
                                                             Homepage:      URL.Parse("https://stations.graphdefined.example"),
                                                             SigningKeys:   stationMakerKeys.SigningKeys(now),
                                                             IdentityKeys:  stationMakerKeys.IdentityKeys
                                                         )),
                                RoamingNetworkChange.Add(new EnergyMeterManufacturer(
                                                             acMeterMakerId,
                                                             Name:          ImmutableI18NString.Create(Languages.en, "Saale Metering"),
                                                             Description:   ImmutableI18NString.Create(Languages.en, "Calibrated AC energy meters"),
                                                             Homepage:      URL.Parse("https://saale-metering.example"),
                                                             SigningKeys:   acMeterMakerKeys.SigningKeys(now),
                                                             IdentityKeys:  acMeterMakerKeys.IdentityKeys
                                                         )),
                                RoamingNetworkChange.Add(new EnergyMeterManufacturer(
                                                             dcMeterMakerId,
                                                             Name:          ImmutableI18NString.Create(Languages.en, "Kernberg DC Metering"),
                                                             Description:   ImmutableI18NString.Create(Languages.en, "Calibrated DC energy meters"),
                                                             Homepage:      URL.Parse("https://kernberg-metering.example"),
                                                             SigningKeys:   dcMeterMakerKeys.SigningKeys(now),
                                                             IdentityKeys:  dcMeterMakerKeys.IdentityKeys
                                                         ))
                            };
```

`stationMakerId`, `acMeterMakerId` and `dcMeterMakerId` are `GraphDefined`, `SAALE` and `KERNBERG`, fields of
`ExampleA2` as `gefId` is, for the examples building on this one. The `GraphDefined` making charging stations is a
party of its own, beside the `GraphDefined` making transparency software of [example A13](exampleA13.md), with keys of
its own.


## 2. `DE*GEF` asks to be added

`DE*GEF` makes the change set adding it and the manufacturers, for the version of `QA` it checked with
`ExampleA1.Validate()`, and signs it with each of its identity keys; each manufacturer with its own. Every identity key
a change enters anew must sign its change set (SPECIFICATION.md, section 4.1): without, `OCC` could add `DE*GEF`
without it knowing, or anybody the keys of another one.

```csharp
if (!RoamingNetwork.TryParse(Helpers.ReadJSON("published/QA.json"), out var checkedQA, out var error))
    Helpers.Fail(error);

var addGEF                = checkedQA.CreateChangeSet(
                                [ RoamingNetworkChange.Add(gef), .. manufacturers ],
                                Description:  ImmutableI18NString.Create(Languages.en, "DE*GEF, GraphDefined Charging Solutions, and the manufacturers of its charging stations and energy meters")
                            );

foreach (var identityKey in identityKeys.Concat([ stationMakerKeys.Identity, acMeterMakerKeys.Identity, dcMeterMakerKeys.Identity ]))
    addGEF                = addGEF.Sign(identityKey.Private,
                                        POIPublicKey.From(identityKey.Private).Id,
                                        identityKey.Algorithm);

File.WriteAllText("DE-GEF/addGEF.json", addGEF.ToJSON().ToString());
```

`DE-GEF/addGEF.json`, what `DE*GEF` hands to `OCC`:

```json
{
  "@id":              "6hnMA1C992E7AS1fQrSQYK9ntz8MQUKW",
  "@context":         "https://open.charging.cloud/contexts/wwcp/roamingNetwork/changeSet/v3",
  "roamingNetworkId": "QA",
  "createdAt":        "2026-10-24T15:12:05.357Z",
  "description":      { "en": "DE*GEF, GraphDefined Charging Solutions, and the manufacturers of its charging stations and energy meters" },
  "signatures": [
    { "algorithm": "ES512", "keyId": "1922a7fb…", "value": "AbKu5SDL…" },
    { "algorithm": "Ed448", "keyId": "5fc456af…", "value": "zWYa8Ylp…" },
    { "algorithm": "ES512", "keyId": "9f26e981…", "value": "Abw8Yys7…" },
    { "algorithm": "ES512", "keyId": "07643329…", "value": "Af+oEoEa…" },
    { "algorithm": "ES512", "keyId": "a0779cfa…", "value": "AWXg2xhu…" }
  ],
  "changes": [
    { "kind": "Add", "entityType": "ChargingStationOperator", "entityId": "DE*GEF", "newValue": { "@id": "DE*GEF", "name": …, "signingKeys": […], "identityKeys": […], … } },
    { "kind": "Add", "entityType": "ChargingStationManufacturer", "entityId": "GraphDefined", "newValue": { "@id": "GraphDefined", "name": { "en": "GraphDefined Charging Stations" }, "homepage": "https://stations.graphdefined.example", "signingKeys": [ { …, "usages": [ "models" ] }, { …, "usages": [ "party" ] } ], "identityKeys": [ { "@id": "9f26e981…", … } ], … } },
    { "kind": "Add", "entityType": "EnergyMeterManufacturer", "entityId": "SAALE", "newValue": { "@id": "SAALE", "name": { "en": "Saale Metering" }, …, "identityKeys": [ { "@id": "07643329…", … } ], … } },
    { "kind": "Add", "entityType": "EnergyMeterManufacturer", "entityId": "KERNBERG", "newValue": { "@id": "KERNBERG", "name": { "en": "Kernberg DC Metering" }, …, "identityKeys": [ { "@id": "a0779cfa…", … } ], … } }
  ],
  "beforeETags": [ [ "json", "sha256", "hex", "a332cc95…" ], [ "cbor", "sha256", "hex", "71dd244d…" ] ],
  "afterETags":  [ [ "json", "sha256", "hex", "936ed300…" ], [ "cbor", "sha256", "hex", "4381d326…" ] ]
}
```

The `beforeETags` are those of the version `DE*GEF` checked: the change set applies to it alone. Its five signatures
are no signatures of an owner: adding a party is the RNO's.


## 3. `OCC` adds `DE*GEF` and the manufacturers

`OCC` reads what `DE*GEF` sent - the public halves of the keys only, a private key would be refused - and sees what is
missing (`TryCheckCoSigning`, section 4.2). Who is a member is the RNO's, decided by its quorum: two of its three
holders. Each change of the change set needs the quorum.

```csharp
if (!RoamingNetworkChangeSet.TryParse(Helpers.ReadJSON("DE-GEF/addGEF.json"), out var addGEFSent, out error))
    Helpers.Fail(error);

if (!A1.RoamingNetwork.TryCheckCoSigning(addGEFSent, out var missing, out _, out error))
    Helpers.Fail(error);

// The change set signed by the signing key of the given organization.
RoamingNetworkChangeSet SignedBy(RoamingNetworkChangeSet ChangeSet, String Organization)

    => ChangeSet.Sign(A1.SigningKeys[Organization].Private,
                      POIPublicKey.From(A1.SigningKeys[Organization].Private).Id,
                      A1.SigningKeys[Organization].Algorithm);

// AlphaOrg alone: refused, nothing is committed.
var byAlphaOrg            = SignedBy(addGEFSent, "AlphaOrg");
var refused               = A1.History.Commit(byAlphaOrg);

// BetaOrg signs it as well: 2/3 quorum reached!
var byAlphaAndBetaOrg     = SignedBy(byAlphaOrg, "BetaOrg");
var added                 = A1.History.Commit(byAlphaAndBetaOrg);
```

```
Sent by DE*GEF, signed by its 2 identity keys and the ones of its manufacturers; missing: Change 0: Add of the ChargingStationOperator 'DE*GEF' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'! Change 1: Add of the ChargingStationManufacturer 'GraphDefined' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'! Change 2: Add of the EnergyMeterManufacturer 'SAALE' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'! Change 3: Add of the EnergyMeterManufacturer 'KERNBERG' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'!
Signed by AlphaOrg alone: Refused, Change 0: Add of the ChargingStationOperator 'DE*GEF' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'!
Signed by AlphaOrg and BetaOrg: Committed, revision 1, commit 1fa68005fcfd0ad35b2a44e9d1a6a932862e03c4f44bcd3449b7b99f1466e8a1
```

Had `DE*GEF` or a manufacturer not signed with one of its identity keys, the missing signatures would name it as well:
`… needs a signature by the new identity key '…' of the ChargingStationOperator 'DE*GEF', as the proof that it holds
it!`. The commit, revision 1, names the genesis commit in its `parentIds` and carries the change set with its seven
signatures, by the two identity keys of `DE*GEF`, the identity keys of the three manufacturers and the signing keys of
`AlphaOrg` and `BetaOrg`; the commit itself is signed by the signers of the history, the three holders of `OCC`.


## 4. The manufacturers state their models

The models the stations and meters of `DE*GEF` are, each stated by its manufacturer
([ChargingStationManufacturer](../docs/ChargingStationManufacturer.md#chargingstationmodels),
[EnergyMeterManufacturer](../docs/EnergyMeterManufacturer.md#energymetermodels)): a station or a meter names a model
of its manufacturer, which knows its versions and its ratings; a certificate of a conformity assessment body names one
of its versions, see [example A13](exampleA13.md). A model is unique within its manufacturer only, so every change of
it names its manufacturer. One change set with the models of all three, signed by each with its key for its models; the
roaming network operator signs none of them.

```csharp
var models                = new[] {
                                RoamingNetworkChange.Add(new ChargingStationModel(ChargingStationModel_Id.Parse("GD-DC300"),
                                                         ImmutableI18NString.Create(Languages.en, "GraphDefined DC 300"),
                                                         Versions:        [ "1.0", "1.1" ],
                                                         DatasheetURL:    URL.Parse("https://stations.graphdefined.example/gd-dc300/datasheet.pdf"),
                                                         MaxPower:        Watt.FromKW(300),
                                                         CurrentTypes:    CurrentTypes.DC,
                                                         ConnectorTypes:  [ ChargingConnectorType.IEC_62196_T2_COMBO ],
                                                         NumberOfEVSEs:   2),
                                stationMakerId),
                                …                                                 // GD-AC44 of GraphDefined
                                RoamingNetworkChange.Add(new EnergyMeterModel(EnergyMeterModel_Id.Parse("KDM-500"),
                                                         ImmutableI18NString.Create(Languages.en, "Kernberg DC meter 500 A"),
                                                         Versions:          [ "2.1" ],
                                                         CurrentTypes:      CurrentTypes.DC,
                                                         MaxCurrent:        Ampere.FromA(500),
                                                         AccuracyClass:     "B",
                                                         Measurands:        [ "1-0:1.8.0", "1-0:2.8.0" ],
                                                         SignatureMethods:  [ SignatureMethod.OCMF ]),
                                dcMeterMakerId),
                                …                                                 // SM-3 of SAALE
                            };

var addModels             = A1.History.Head.RoamingNetwork.CreateChangeSet(models, Description: ImmutableI18NString.Create(Languages.en, "The models of GraphDefined, KERNBERG and SAALE"));

// Signed by the roaming network operator: refused, the models are their manufacturers'.
var byOCCOnly             = A1.History.Commit(SignedBy(SignedBy(addModels, "AlphaOrg"), "BetaOrg"));

foreach (var modelsKey in new[] { stationMakerKeys.Models, dcMeterMakerKeys.Models, acMeterMakerKeys.Models })
    addModels             = addModels.Sign(modelsKey.Private, POIPublicKey.From(modelsKey.Private).Id, modelsKey.Algorithm);

var modelsAdded           = A1.History.Commit(addModels);

// What everybody may read now: the roaming network with DE*GEF and its manufacturers, and the commit leading to it.
File.WriteAllText($"published/{A1.RoamingNetwork.Id}.json",         A1.RoamingNetwork.  ToJSON(IncludeETags: true).ToString());
File.WriteAllText($"published/{A1.RoamingNetwork.Id}-commit.json",  A1.History.Head.Commit.ToJSON().                  ToString());
```

```
The models signed by the roaming network operator: Refused, Change 0: Add of the ChargingStationModel 'GD-DC300' needs a signature by a signing key of the ChargingStationManufacturer 'GraphDefined'!
Signed by the three manufacturers: Committed, revision 2
```

`GraphDefined` in `published/QA.json` then, its models embedded:

```json
{
  "@id": "GraphDefined",
  "name": { "en": "GraphDefined Charging Stations" },
  "homepage": "https://stations.graphdefined.example",
  "chargingStationModels": [
    { "@id": "GD-AC44", "name": { "en": "GraphDefined AC 44" }, "versions": [ "1.0" ], "datasheetURL": "https://stations.graphdefined.example/gd-ac44/datasheet.pdf",
      "maxPower": 44000, "currentTypes": [ "AC_ThreePhases" ], "connectorTypes": [ "IEC_62196_T2" ], "numberOfEVSEs": 2 },
    { "@id": "GD-DC300", "name": { "en": "GraphDefined DC 300" }, "versions": [ "1.0", "1.1" ], "datasheetURL": "https://stations.graphdefined.example/gd-dc300/datasheet.pdf",
      "maxPower": 300000, "currentTypes": [ "DC" ], "connectorTypes": [ "IEC_62196_T2_COMBO" ], "numberOfEVSEs": 2 }
  ],
  "signingKeys": [
    { "@id": "7b140b74…", "key": "pAECIAEh…", "notAfter": "2027-10-24T15:12:04.800Z", "usages": [ "models" ] },
    { "@id": "8ebe0819…", "key": "pAECIAEh…", "notAfter": "2027-10-24T15:12:04.800Z", "usages": [ "party" ] }
  ],
  "identityKeys": [ { "@id": "9f26e981…", "key": "pAECIAMh…" } ],
  "created":    "2026-10-24T15:12:05.142Z",
  "lastChange": "2026-10-24T15:12:05.964Z"
}
```

Its `lastChange` is the one of the change set adding its models: a change of a model changes its manufacturer.


## 5. Checking, as `DE*GEF`, that `QA` names it with its keys

`ExampleA2.Validate()` checks `QA` as everybody does, `ExampleA1.Validate()` now at revision 2, then that it names
`DE*GEF` with exactly the keys it sent, from what is published and its own `operator.json` only, and shows the
manufacturers with their models:

```csharp
ExampleA1.Validate();

var network  = Helpers.ReadJSON("published/QA.json");
var sent     = Helpers.ReadJSON("DE-GEF/operator.json");

if (!RoamingNetwork.TryParse(network, out var qa, out var error))
    Helpers.Fail(error);

if (!ChargingStationOperator.TryParse(sent, out var gefSent, out error))
    Helpers.Fail(error);

if (!qa.TryGetChargingStationOperatorById(gefId, out var gef))
    Helpers.Fail($"QA has no charging station operator '{gefId}'");

static String KeyIds(IEnumerable<POIPublicKey> Keys)
    => String.Join(", ", Keys.Select(key => key.Id).Order());

if (KeyIds(gef.SigningKeys)  != KeyIds(gefSent.SigningKeys) ||
    KeyIds(gef.IdentityKeys) != KeyIds(gefSent.IdentityKeys))
    Helpers.Fail($"QA names '{gefId}' with other keys than it sent");
```

```
QA is checked: revision 2, commit 39e1bbe68021679daf954f91127682ff8463ed766aa0d7ec706e8a10e2d3ca82, signed by AlphaOrg and BetaOrg and GammaOrg
beforeETags of the next change set: json:sha256:hex:f1df53cf33d81396eea7220725bdb72d1bbd3c5eb45452723347b152deaefbc3, cbor:sha256:hex:cf2e9d2a5559fb4b3d981a18881c3faa656c9f5c279b73b0e816f76e735f1306

DE*GEF is a member of QA with its keys:
  signing key  1040f738490ecae5c72b962c58b12bcf164465a92a0d04d6fc4d246f5181cb35 for party
  signing key  613f0ffa6c2db5f5dc2cf87095759b3bf5db43fb8608c5c94ca40c6326056546 for priceStatements, tariffs
  signing key  be516d1694d0a0087162cdc7610febef0e74a1a3b0aa9858ef17b46a1cef862f for infrastructure
  identity key 1922a7fb7e73a4f45962ec1c35e3647476e6b1b16f9ceb47439dbf9b2a1ca1df
  identity key 5fc456af8ade7749b0e7049a5f2920c4db48eca829c74e414367cdd45f720590

Charging station manufacturer GraphDefined: GraphDefined Charging Stations, models GD-AC44 (44 kW, versions 1.0), GD-DC300 (300 kW, versions 1.0/1.1)
Energy meter manufacturer     KERNBERG: Kernberg DC Metering, models KDM-500 (class B, OCMF)
Energy meter manufacturer     SAALE: Saale Metering, models SM-3 (class B, OCMF)
```

`ExampleA2.Run(a1)` returns the private keys of `DE*GEF`, its identity keys and its signing keys by usage, for the
examples building on this one. The manufacturers keep theirs in their folders.


## With the hub

A [RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) operating `QA` is its RNO with keys of a single
holder, so it signs alone. An administrator (`poi:edit`) asks it to add the party with its public keys; the hub signs
the change set and lets it wait (section 4.2) until the party signed it with each of its identity keys. Then it is
committed.

### At the prompt or over SSH

```
RoamingHub> roamingNetworks QA add chargingStationOperator DE*GEF signingKey pAECIAEh... identityKey pAECIAMh... identityKey owEBIAch... GraphDefined Charging Solutions
The ChargingStationOperator 'DE*GEF' is added to the roaming network 'QA' once it signed the change set '…' with each of its identity keys, as the proof that it holds them: POST /poi/RNs/QA/history/changeSets/pending/…/signatures. Signed by this hub, it waits until then.
Change 0: Add of the ChargingStationOperator 'DE*GEF' needs a signature by the new identity key '1922a7fb…' of the ChargingStationOperator 'DE*GEF', as the proof that it holds it!
```

Each key after the word that says which kind it is, `signingKey` or `identityKey`, as many of each as the operator has;
the words after the keys are its name.

### In the web interface

**Points of interest**, below the list of the roaming networks, the card **New party of a roaming network**: *Roaming
network* `QA`, *Party* `Charging station operator`, *Identification* `DE*GEF`, *Name*, the keys in *Signing keys* and
*Identity keys*, one per line, then **Add the party**. The card then says that the party is added once it signed the
change set with its identity keys.

### Over HTTP

`POST /api/v1/poi/RNs/QA/CSOs` with the operator as POI writes it, each key the Base64 of its COSE key or as POI writes
it: `operator.json` as it is.

```bash
curl -X POST http://127.0.0.1:2356/api/v1/poi/RNs/QA/CSOs \
     -H "API-Key: $ROAMINGHUB_API_KEY" -H "Content-Type: application/json" \
     --data-binary @DE-GEF/operator.json
```

`202 Accepted`, with the change set waiting:

```json
{
  "message":     "The ChargingStationOperator 'DE*GEF' is added to the roaming network 'QA' once it signed the change set '…' with each of its identity keys, …",
  "status":      "pending",
  "changeSetId": "…",
  "changeSet":   { …, "signatures": [ { "algorithm": "ES256", "keyId": "…", "value": "…" } ] },
  "missing":     [ "Change 0: Add of the ChargingStationOperator 'DE*GEF' needs a signature by the new identity key '1922a7fb…' …" ]
}
```

`DE*GEF` signs that change set with each of its identity keys, as in step 2 - `GET
/poi/RNs/QA/history/changeSets/pending/{changeSetId}` gives it again - and sends the signatures, by anybody, without
signing in: whether they count, the keys decide.

```bash
curl -X POST http://127.0.0.1:2356/poi/RNs/QA/history/changeSets/pending/{changeSetId}/signatures \
     -H "Content-Type: application/json" \
     --data-binary '{ "signatures": [ { "algorithm": "ES512", "keyId": "1922a7fb…", "value": "…" }, { "algorithm": "Ed448", "keyId": "5fc456af…", "value": "…" } ] }'
```

`{ "status": "committed", "commitId": "…", … }`: `DE*GEF` is a member. A grid operator not taking part comes without
keys; it is committed at once, `201` with the commit.

### The manufacturers

The hub adds each manufacturer as it adds `DE*GEF`, on its own, before or after it: a change set it signs as the RNO,
waiting for the signatures of the identity key of the manufacturer; so on a hub they are commits of their own, and the
revisions of the examples after this one are three higher. A charging station or energy meter manufacturer takes a
`homepage` after its keys at the prompt:

```
RoamingHub> roamingNetworks QA add chargingStationManufacturer GraphDefined signingKey pAECIAEh... signingKey pAECIAEh... identityKey pAECIAMh... homepage https://stations.graphdefined.example GraphDefined Charging Stations
The ChargingStationManufacturer 'GraphDefined' is added to the roaming network 'QA' once it signed the change set '…' with each of its identity keys, as the proof that it holds them: POST /poi/RNs/QA/history/changeSets/pending/…/signatures. Signed by this hub, it waits until then.
RoamingHub> roamingNetworks QA add energyMeterManufacturer SAALE signingKey pAECIAEh... signingKey pAECIAEh... identityKey pAECIAMh... homepage https://saale-metering.example Saale Metering
RoamingHub> roamingNetworks QA add energyMeterManufacturer KERNBERG signingKey pAECIAEh... signingKey pAECIAEh... identityKey pAECIAMh... homepage https://kernberg-metering.example Kernberg DC Metering
```

In the web interface: the card **New party of a roaming network**, *Party* `Charging station manufacturer` or `Energy
meter manufacturer`. Over HTTP, `POST /api/v1/poi/RNs/QA/CSMs` and `…/EMMs` with the manufacturer and its keys, `202`
as for `DE*GEF`; the same one again is `409`. Its models it commits itself, as in step 4, with
`COMMIT /poi/RNs/QA/history/changeSets`, signed by its `models` key: the hub signs none of them. The POI API lists the
manufacturers at `GET /poi/RNs/QA/CSMs` and `GET /poi/RNs/QA/EMMs`, their models at
`GET /poi/RNs/QA/CSMs/GraphDefined/models` and `…/EMMs/KERNBERG/models`; the web interface shows them below each
manufacturer.

### What is refused

Nothing is committed then, and the answer says why:

| Asked | Answer |
|---|---|
| a private key among the keys - it is no longer private once it was sent | `400` |
| no signing key, or no identity key, also of a manufacturer | `400` |
| the operator with its charging pools, or anything else of its own, in it, e.g. a manufacturer with its models: they are its own to add | `422` |
| `DE*GEF` once more | `409`, `The roaming network 'QA' has a ChargingStationOperator 'DE*GEF' already.` |
| a roaming network the hub does not serve, or one another RNO operates | `404`, `403` |
| a list of no party, e.g. `…/RNs/QA/CPs` | `404`, naming `CSOs \| EMPs \| GOs \| POs \| CSMs \| EMMs \| TSMs \| CABs` |
| signatures by a key that is neither one of a party nor a new identity key of the change set | refused |

E-mobility providers, grid operators and parking operators are added the same way: `eMobilityProvider`,
`gridOperator`, `parkingOperator` at the prompt, *Party* in the form, `…/EMPs`, `…/GOs`, `…/POs` over HTTP.


## Next

- [Example A3](exampleA3.md): `DE*GEF` adds its templates, naming the models of its manufacturers, two charging pools
  of a DC and an AC station each, every EVSE with its energy meter, and their groups, with change sets it signs with its
  `infrastructure` key.
- Its keys replaced: a change of `signingKeys` signed by one of its signing keys that may sign for the `party`, or by an
  identity key; a new identity key signs its change as well, see
  [ChargingStationOperator](../docs/ChargingStationOperator.md).
