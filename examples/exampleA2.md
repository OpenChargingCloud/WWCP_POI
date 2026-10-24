# Example A2: the charging station operator `DE*GEF` asks to be added to `QA`

The roaming network `QA` of [example A1](exampleA1.md) gets its first party: the charging station operator `DE*GEF`,
GraphDefined Charging Solutions. It makes its own keys and keeps them, makes the change set adding it to the version of
`QA` it checked, and signs it with each of its identity keys: the proof that it holds them, and that it asks to be
added. The same change set adds the manufacturers its charging stations and their energy meters will name, which `QA`
does not know yet: `GraphDefined`, GraphDefined Charging Stations, and the energy meter manufacturers `SAALE`, Saale
Metering, of its AC meters, and `KERNBERG`, Kernberg DC Metering, of its DC meters. The roaming network operator `OCC`
decides on both, a new member and its registries of manufacturers: two of its three holders sign the change set as well,
and it is committed.

| Revision | Signed by | Commit |
|---|---|---|
| 0 | `OCC`, as the RNO | Genesis: the roaming network `QA`, see [example A1](exampleA1.md) |
| 1 | `OCC`, as the RNO; its change set by `DE*GEF`'s identity keys and two holders of `OCC` | `Add` of the charging station operator `DE*GEF`, with the public halves of its keys; of the charging station manufacturer `GraphDefined`; of the energy meter manufacturers `SAALE` and `KERNBERG` |

From then on `DE*GEF` adds its data itself, with change sets it signs: [example A3](exampleA3.md). The code is
[exampleA2.cs](csharp/exampleA2.cs), called by [Program.cs](csharp/Program.cs) with what example A1 left:

```csharp
var a2 = ExampleA2.Run(a1);

ExampleA2.Validate();
```

The output is what one run printed, `…` for elided keys, signatures and digests. Below, [with the hub](#with-the-hub):
the same at the prompt, in the web interface and over HTTP.


## 1. `DE*GEF` makes its keys

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
    { "@id": "f1945035…", "key": "pAECIAEh…", "notAfter": "2027-10-22T18:20:54.971Z", "usages": [ "infrastructure" ] },
    { "@id": "f46192f8…", "key": "pAECIAEh…", "notAfter": "2027-10-22T18:20:54.971Z", "usages": [ "party" ] },
    { "@id": "fa82f75c…", "key": "pAECIAEh…", "notAfter": "2027-10-22T18:20:54.971Z", "usages": [ "priceStatements", "tariffs" ] }
  ],
  "identityKeys": [
    { "@id": "93c7c440…", "key": "pAECIAMh…" },
    { "@id": "a6bb7043…", "key": "owEBIAch…" }
  ],
  "created":    "2026-10-22T18:20:55.084Z",
  "lastChange": "2026-10-22T18:20:55.084Z"
}
```

The private keys stay in `DE-GEF/`: `identity1-privateKey.pem` (P-521), `identity2-privateKey.pem` (Ed448) and
`signing-infrastructure-…`, `signing-tariffs-…`, `signing-party-privateKey.pem` (P-256).

Then the manufacturers its stations and the energy meters of its EVSEs will name, see
[ChargingStationManufacturer](../docs/ChargingStationManufacturer.md) and
[EnergyMeterManufacturer](../docs/EnergyMeterManufacturer.md). They are registries of the RNO, embedded in the roaming
network as `chargingStationManufacturers` and `energyMeterManufacturers`; a station or an energy meter can name only a
manufacturer of the network, so they come with `DE*GEF`:

```csharp
var manufacturers         = new[] {
                                RoamingNetworkChange.Add(new ChargingStationManufacturer(
                                                             stationMakerId,
                                                             Name:      I18NString.Create(Languages.en, "GraphDefined Charging Stations"),
                                                             Homepage:  URL.Parse("https://stations.graphdefined.example")
                                                         )),
                                RoamingNetworkChange.Add(new EnergyMeterManufacturer(
                                                             acMeterMakerId,
                                                             Name:         ImmutableI18NString.Create(Languages.en, "Saale Metering"),
                                                             Description:  ImmutableI18NString.Create(Languages.en, "Calibrated AC energy meters"),
                                                             Homepage:     URL.Parse("https://saale-metering.example")
                                                         )),
                                RoamingNetworkChange.Add(new EnergyMeterManufacturer(
                                                             dcMeterMakerId,
                                                             Name:         ImmutableI18NString.Create(Languages.en, "Kernberg DC Metering"),
                                                             Description:  ImmutableI18NString.Create(Languages.en, "Calibrated DC energy meters"),
                                                             Homepage:     URL.Parse("https://kernberg-metering.example")
                                                         ))
                            };
```

`stationMakerId`, `acMeterMakerId` and `dcMeterMakerId` are `GraphDefined`, `SAALE` and `KERNBERG`, fields of
`ExampleA2` as `gefId` is, for the examples building on this one.


## 2. `DE*GEF` asks to be added

`DE*GEF` makes the change set adding it and the manufacturers, for the version of `QA` it checked with
`ExampleA1.Validate()`, and signs it with each of its identity keys. Every identity key a change enters anew must sign its change set (SPECIFICATION.md,
section 4.1): without, `OCC` could add `DE*GEF` without it knowing, or anybody the keys of another one.

```csharp
if (!RoamingNetwork.TryParse(Helpers.ReadJSON("published/QA.json"), out var checkedQA, out var error))
    Helpers.Fail(error);

var addGEF                = checkedQA.CreateChangeSet(
                                [ RoamingNetworkChange.Add(gef), .. manufacturers ],
                                Description:  ImmutableI18NString.Create(Languages.en, "DE*GEF, GraphDefined Charging Solutions, and the manufacturers of its charging stations and energy meters")
                            );

foreach (var identityKey in identityKeys)
    addGEF                = addGEF.Sign(identityKey.Private,
                                        POIPublicKey.From(identityKey.Private).Id,
                                        identityKey.Algorithm);

File.WriteAllText("DE-GEF/addGEF.json", addGEF.ToJSON().ToString());
```

`DE-GEF/addGEF.json`, what `DE*GEF` hands to `OCC`:

```json
{
  "@id":              "US9WdKESMh9t1Ex79fpW8Q6hbp412rj1",
  "@context":         "https://open.charging.cloud/contexts/wwcp/roamingNetwork/changeSet/v3",
  "roamingNetworkId": "QA",
  "createdAt":        "2026-10-22T18:20:55.243Z",
  "description":      { "en": "DE*GEF, GraphDefined Charging Solutions, and the manufacturers of its charging stations and energy meters" },
  "signatures": [
    { "algorithm": "ES512", "keyId": "93c7c440…", "value": "AVzksr72…" },
    { "algorithm": "Ed448", "keyId": "a6bb7043…", "value": "Zqi+zQ+N…" }
  ],
  "changes": [
    { "kind": "Add", "entityType": "ChargingStationOperator", "entityId": "DE*GEF", "newValue": { "@id": "DE*GEF", "name": …, "signingKeys": […], "identityKeys": […], … } },
    { "kind": "Add", "entityType": "ChargingStationManufacturer", "entityId": "GraphDefined", "newValue": { "@id": "GraphDefined", "name": { "en": "GraphDefined Charging Stations" }, "homepage": "https://stations.graphdefined.example", … } },
    { "kind": "Add", "entityType": "EnergyMeterManufacturer", "entityId": "SAALE", "newValue": { "@id": "SAALE", "name": { "en": "Saale Metering" }, … } },
    { "kind": "Add", "entityType": "EnergyMeterManufacturer", "entityId": "KERNBERG", "newValue": { "@id": "KERNBERG", "name": { "en": "Kernberg DC Metering" }, … } }
  ],
  "beforeETags": [ [ "json", "sha256", "hex", "a19a65c7…" ], [ "cbor", "sha256", "hex", "86b2dd1a…" ] ],
  "afterETags":  [ [ "json", "sha256", "hex", "a8f48589…" ], [ "cbor", "sha256", "hex", "a42bde54…" ] ]
}
```

The `beforeETags` are those of the version `DE*GEF` checked: the change set applies to it alone. Its own signatures are
no signatures of an owner: adding a party is the RNO's, and so are its registries of manufacturers.


## 3. `OCC` adds `DE*GEF`

`OCC` reads what `DE*GEF` sent - the public halves of its keys only, a private key would be refused - and sees what is
missing (`TryCheckCoSigning`, section 4.2). Who is a member is the RNO's, decided by its quorum: two of its three
holders. So are the manufacturers: each change of the change set needs the quorum.

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

// What everybody may read now: the roaming network with DE*GEF, and the commit leading to it.
File.WriteAllText($"published/{A1.RoamingNetwork.Id}.json",         A1.RoamingNetwork.  ToJSON(IncludeETags: true).ToString());
File.WriteAllText($"published/{A1.RoamingNetwork.Id}-commit.json",  A1.History.Head.Commit.ToJSON().                  ToString());
```

```
Sent by DE*GEF, signed by its 2 identity keys; missing: Change 0: Add of the ChargingStationOperator 'DE*GEF' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'! Change 1: Add of the ChargingStationManufacturer 'GraphDefined' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'! Change 2: Add of the EnergyMeterManufacturer 'SAALE' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'! Change 3: Add of the EnergyMeterManufacturer 'KERNBERG' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'!
Signed by AlphaOrg alone: Refused, Change 0: Add of the ChargingStationOperator 'DE*GEF' needs signatures by signing keys of 2 holders of the roaming network operator 'OCC'!
Signed by AlphaOrg and BetaOrg: Committed, revision 1, commit 65207f5c029f52d47d991d55fecdc794bd9809fde54acbaa554fe5e74d7db9bf
```

Had `DE*GEF` not signed with one of its identity keys, the missing signatures would name it as well:
`… needs a signature by the new identity key '…' of the ChargingStationOperator 'DE*GEF', as the proof that it holds
it!`. The commit, revision 1, names the genesis commit in its `parentIds` and carries the change set with its four
signatures, by the two identity keys of `DE*GEF` (`93c7c440…`, `a6bb7043…`) and the signing keys of `AlphaOrg` and
`BetaOrg`; the commit itself is signed by the signers of the history, the three holders of `OCC`.


## 4. Checking, as `DE*GEF`, that `QA` names it with its keys

`ExampleA2.Validate()` checks `QA` as everybody does, `ExampleA1.Validate()` now at revision 1, then that it names
`DE*GEF` with exactly the keys it sent, from what is published and its own `operator.json` only, and shows the
manufacturers:

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
QA is checked: revision 1, commit 65207f5c029f52d47d991d55fecdc794bd9809fde54acbaa554fe5e74d7db9bf, signed by AlphaOrg and BetaOrg and GammaOrg
beforeETags of the next change set: json:sha256:hex:a8f485894c10c42dce2f2d6f70600114c97c40b7fed4ca2d6091d74e45a4a2d7, cbor:sha256:hex:a42bde54a2c66cdee21d257f64d97051e031b5c9ab0b2cdfafeea8b57ac757a8

DE*GEF is a member of QA with its keys:
  signing key  f19450356e435189c2ef365e46853f4c0f47f7e9d2e13419597f8dab39552efa for infrastructure
  signing key  f46192f83588c50e91a57600949652fd331c1a479d6a1d21a6636fa9195e5598 for party
  signing key  fa82f75c5b73b6acffc9c25d7292644f755af866031ab5ac6e13ca21f8427c75 for priceStatements, tariffs
  identity key 93c7c4407a0a96f7dcb6cfc500b2628a0cc92c05029cf845cba603de83754f58
  identity key a6bb704342b3615bc54e8cd377483b22befef538bfe242e7e8d1ee75e7a2ae85

Charging station manufacturer GraphDefined: GraphDefined Charging Stations
Energy meter manufacturer     KERNBERG: Kernberg DC Metering
Energy meter manufacturer     SAALE: Saale Metering
```

`ExampleA2.Run(a1)` returns the private keys of `DE*GEF`, its identity keys and its signing keys by usage, for the
examples building on this one.


## With the hub

A [RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) operating `QA` is its RNO with keys of a single
holder, so it signs alone. An administrator (`poi:edit`) asks it to add the party with its public keys; the hub signs
the change set and lets it wait (section 4.2) until the party signed it with each of its identity keys. Then it is
committed.

### At the prompt or over SSH

```
RoamingHub> roamingNetworks QA add chargingStationOperator DE*GEF signingKey pAECIAEh... identityKey pAECIAMh... identityKey owEBIAch... GraphDefined Charging Solutions
The ChargingStationOperator 'DE*GEF' is added to the roaming network 'QA' once it signed the change set '…' with each of its identity keys, as the proof that it holds them: POST /poi/RNs/QA/history/changeSets/pending/…/signatures. Signed by this hub, it waits until then.
Change 0: Add of the ChargingStationOperator 'DE*GEF' needs a signature by the new identity key '93c7c440…' of the ChargingStationOperator 'DE*GEF', as the proof that it holds it!
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
  "missing":     [ "Change 0: Add of the ChargingStationOperator 'DE*GEF' needs a signature by the new identity key '93c7c440…' …" ]
}
```

`DE*GEF` signs that change set with each of its identity keys, as in step 2 - `GET
/poi/RNs/QA/history/changeSets/pending/{changeSetId}` gives it again - and sends the signatures, by anybody, without
signing in: whether they count, the keys decide.

```bash
curl -X POST http://127.0.0.1:2356/poi/RNs/QA/history/changeSets/pending/{changeSetId}/signatures \
     -H "Content-Type: application/json" \
     --data-binary '{ "signatures": [ { "algorithm": "ES512", "keyId": "93c7c440…", "value": "…" }, { "algorithm": "Ed448", "keyId": "a6bb7043…", "value": "…" } ] }'
```

`{ "status": "committed", "commitId": "…", … }`: `DE*GEF` is a member. A grid operator not taking part comes without
keys; it is committed at once, `201` with the commit.

### The manufacturers

The hub adds the party alone, and the manufacturers on their own, before or after it, each with a change set it signs as
the RNO, committed at once: so on a hub they are commits of their own, and the revisions of the examples after this one
are three higher. At the prompt or over SSH:

```
RoamingHub> roamingNetworks QA add chargingStationManufacturer GraphDefined homepage https://stations.graphdefined.example GraphDefined Charging Stations
The ChargingStationManufacturer 'GraphDefined' was added to the roaming network 'QA', at revision 1, commit a77d56ee…. Charging stations and energy meters may name it now.
RoamingHub> roamingNetworks QA add energyMeterManufacturer SAALE homepage https://saale-metering.example Saale Metering
RoamingHub> roamingNetworks QA add energyMeterManufacturer KERNBERG homepage https://kernberg-metering.example Kernberg DC Metering
```

In the web interface: **Points of interest**, the card **New manufacturer of a roaming network**, *Manufacturer of*
`Charging station manufacturer` or `Energy meter manufacturer`, then **Add the manufacturer**. Over HTTP, `POST
/api/v1/poi/RNs/QA/CSMs` and `…/EMMs` with the manufacturer as POI writes it, `201` with the commit; the same one again
is `409`. The POI API lists them at `GET /poi/RNs/QA/CSMs` and `GET /poi/RNs/QA/EMMs`.

### What is refused

Nothing is committed then, and the answer says why:

| Asked | Answer |
|---|---|
| a private key among the keys - it is no longer private once it was sent | `400` |
| no signing key, or no identity key | `400` |
| the operator with its charging pools, or anything else of its own, in it: they are its own to add | `422` |
| `DE*GEF` once more | `409`, `The roaming network 'QA' has a ChargingStationOperator 'DE*GEF' already.` |
| a roaming network the hub does not serve, or one another RNO operates | `404`, `403` |
| a list of no party, e.g. `…/RNs/QA/CPs` | `404`, naming `CSOs \| EMPs \| GOs \| POs` |
| signatures by a key that is neither one of a party nor a new identity key of the change set | refused |

E-mobility providers, grid operators and parking operators are added the same way: `eMobilityProvider`,
`gridOperator`, `parkingOperator` at the prompt, *Party* in the form, `…/EMPs`, `…/GOs`, `…/POs` over HTTP.


## Next

- [Example A3](exampleA3.md): `DE*GEF` adds its templates, two charging pools of a DC and an AC station each, every EVSE
  with its energy meter, and their groups, with change sets it signs with its `infrastructure` key.
- Its keys replaced: a change of `signingKeys` signed by one of its signing keys that may sign for the `party`, or by an
  identity key; a new identity key signs its change as well, see
  [ChargingStationOperator](../docs/ChargingStationOperator.md).
