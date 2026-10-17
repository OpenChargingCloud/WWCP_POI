# Example 1: a roaming network, an operator and its first charging pool

From a fresh installation of the [RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) to a roaming
network `QA` with one charging station operator and its first charging pool, in three commits, each signed by the
one whose data it changes:

| Revision | Signed by | Commit |
|---|---|---|
| 0 | the hub, as the roaming network operator (RNO) | Genesis: the roaming network `QA`, operated by the hub |
| 1 | the hub, as the RNO | `Add` of the charging station operator `DE*GEF`, with the public halves of its keys |
| 2 | `DE*GEF` | `Add` of the charging pool `DE*GEF*P1` with the charging station `DE*GEF*S1` and the EVSEs `DE*GEF*E1*1` and `DE*GEF*E1*2` |

The hub is the [RoamingNetworkOperator](../docs/RoamingNetworkOperator.md) of every roaming network it makes: it
makes its keys at its first start, and its signing key signs the change sets that are the RNO's - the network itself,
its parties - and every commit it makes. The charging station operator keeps its own keys and signs its own data.

The properties are kept to a minimum: names, a location and the power of the EVSEs. All the others are in
[docs/](../docs/README.md). The output is what one run printed, `…` for elided keys, paths, signatures and digests.
[Without the hub](#without-the-hub-poi-3-alone) shows the same with POI 3 alone, in C#.


## 1. The first start: what is there

```bash
dotnet run --project RoamingHubCLI
```

A first start, beside an empty `configuration.json` or none, makes up what the hub needs and says so once:

- **the account `root`**, with a password shown once in the box at the end of the banner;
- **the roaming network operator this hub is**: a signing key (P-256) and an identity key (P-521), in
  `roamingNetworkOperator/` beside the configuration file - `operator.json`, the operator as its roaming networks
  will embed it, with the public halves of the keys, and `signing-key.pem` and `identity-key.pem`, the private keys,
  readable only by the account the hub runs as;
- **no roaming network**: `poi/` stays empty until one is made.

```
20:16:08.050 [notice poi security] This hub is the roaming network operator 'RNO-67BDC1FC' now: its signing key 882bbf1af577e9dd… and its identity key 67bdc1fcdf7c9816… were made in '…\roamingNetworkOperator'. Keep a copy of them where nobody else can read it: without its identity key, the keys of the roaming networks it operates can never be replaced.
…
20:16:09.353 [info poi] The points of interest are at http://127.0.0.1:2356/poi/RNs (0 roaming network(s)).
…
  RNO             this hub is the roaming network operator 'RNO-67BDC1FC', made now, its keys in …\roamingNetworkOperator; 0 roaming network(s) operated
```

Its identification is `RNO-` and the first eight hex digits of its identity key, unless the configuration file names
it **before the first start** - read then, and never again, as every roaming network it operates names it:

```json
{
  "roamingNetworkOperator": { "@id": "GraphDefined", "name": "GraphDefined GmbH" }
}
```

Keep a copy of `roamingNetworkOperator/` somewhere nobody else can read it (it is in `.gitignore`): without the
private keys the hub can not sign as the operator any more, and does not start - a new key would be none its roaming
networks know - and without the identity key, nobody can ever replace the keys of its roaming networks.

At the prompt, or over SSH, the hub says who it is, with the public halves of its keys, and what it serves:

```
RoamingHub> roamingNetworkOperator
This hub is the roaming network operator 'RNO-67BDC1FC', since 2026-10-17T18:16:07.776Z.
  signing key   882bbf1af577e9ddc8e3d34f7bc9d2a6545a0dab550fb29997341adb14696f0b  pAECIAEhWCAbOtp6vLF9O5vtI3DcT+RW+AqE…
  identity key  67bdc1fcdf7c9816854d4a0a1213879b8ef72a7bf748be99158ffa6a6cc634db  pAECIAMhWEIA6+39oKTPVUAl1v42uBNpUy6M…
  its private keys are kept in '…\roamingNetworkOperator'
  it operates no roaming network yet: 'roamingNetworks add <id> [<name>]' makes one
RoamingHub> roamingNetworks
This hub serves no roaming network yet: 'roamingNetworks add <id> [<name>]' makes one, operated by this hub, 'RNO-67BDC1FC'.
```

`GET /api/v1/poi/roamingNetworkOperator` answers with `operator.json`, public as it is in every roaming network the
hub operates:

```json
{
  "@id":          "RNO-67BDC1FC",
  "signingKeys":  [ { "@id": "882bbf1a…", "key": "pAECIAEh…" } ],
  "identityKeys": [ { "@id": "67bdc1fc…", "key": "pAECIAMh…" } ],
  "created":      "2026-10-17T18:16:07.776Z",
  "lastChange":   "2026-10-17T18:16:07.776Z"
}
```


## 2. The first roaming network: `QA`

By an administrator (`poi:edit`), with nothing but its identification and its name - its operator is the hub:

```
RoamingHub> roamingNetworks add QA Quality assurance
The roaming network 'QA' was made, at revision 0, commit 1d728939…, operated by this hub, 'RNO-67BDC1FC'. Its parties are added now, with change sets this hub signs.
Next: 'roamingNetworks QA add chargingStationOperator <id> signingKey <key> identityKey <key> [<name>]'.
http://127.0.0.1:2356/poi/RNs/QA
```

or `POST /api/v1/poi/roamingNetworks` with `{"@id": "QA", "name": {"en": "Quality assurance"}}`, or the form
**New roaming network** below the list on the **Points of interest** page. The hub stores it in `poi/QA` (`CURRENT`,
`gen-000001/base.json`, `gen-000001/log.jsonl`) and serves it below `/poi`, readable by everyone. The network embeds
its operator with its keys; it has none of its own:

```json
{
  "@context":   "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork",
  "@id":        "QA",
  "name":       { "en": "Quality assurance" },
  "roamingNetworkOperator": {
    "@id":          "RNO-67BDC1FC",
    "signingKeys":  [ { "@id": "882bbf1a…", "key": "pAECIAEh…" } ],
    "identityKeys": [ { "@id": "67bdc1fc…", "key": "pAECIAMh…" } ],
    "created":      "2026-10-17T18:16:07.776Z",
    "lastChange":   "2026-10-17T18:16:07.776Z"
  },
  "created":    "2026-10-17T18:16:11.065Z",
  "lastChange": "2026-10-17T18:16:11.065Z",
  "eTags": [ [ "json", "sha256", "hex", "…" ], [ "cbor", "sha256", "hex", "…" ] ]
}
```

Its genesis commit, `GET /poi/RNs/QA/history/commits/{id}`, is signed by the hub's signing key, in the role of the
roaming network operator - as every commit the hub makes of a roaming network it operates:

```json
{
  "@id":              "1d728939…",
  "roamingNetworkId": "QA",
  "revision":         0,
  "eTags":            [ [ "json", "sha256", "hex", "30e444f0…" ], [ "cbor", "sha256", "hex", "f8ca0e26…" ] ],
  "signatures": [
    {
      "role":      "roamingNetworkOperator",
      "signer":    "RNO-67BDC1FC",
      "signedAt":  "2026-10-17T18:16:11.230Z",
      "algorithm": "ES256",
      "keyId":     "882bbf1a…",
      "value":     "bcPUbnak…",
      "profile":   "wwcp-poi-commit-signature-v1",
      "encoding":  "base64"
    }
  ]
}
```


## 3. The first charging station operator: `DE*GEF`

`DE*GEF` makes its own keys and keeps them; the hub gets only their public halves. A console project with
WWCP_POI3, e.g. in `examples/Example1/`, the same program as in step 4:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\WWCP_POI3\WWCP_POI3.csproj" />
  </ItemGroup>

</Project>
```

```csharp
using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;

// A new private key on the given curve.
static ECPrivateKeyParameters NewKey(COSECurve Curve)
{
    var generator = new ECKeyPairGenerator();
    generator.Init(new ECKeyGenerationParameters(Curve.DomainParameters!, new SecureRandom()));
    return (ECPrivateKeyParameters) generator.GenerateKeyPair().Private;
}

// The keys of DE*GEF: kept here, and only their public halves, the Base64 of their COSE keys, given to the hub.
var gefSigningKey   = NewKey(COSECurve.P256);
var gefIdentityKey  = NewKey(COSECurve.P521);

Console.WriteLine($"signingKey  {POIPublicKey.From(gefSigningKey). ToJSON()["key"]}");
Console.WriteLine($"identityKey {POIPublicKey.From(gefIdentityKey).ToJSON()["key"]}");
Console.WriteLine("Add DE*GEF with these keys at the hub, then press Enter.");
Console.ReadLine();
```

```
signingKey  pAECIAEhWCB5Vy8gs2bOr0hjZtbdc/XbBab5nzh3mkRz7A9iGuDKISJY…
identityKey pAECIAMhWEIB4zA7f9XhNrIzPv42Zza3sqVlH+RfPf3ukr4R7y5nrh0H…
Add DE*GEF with these keys at the hub, then press Enter.
```

An administrator adds it, and the hub signs it as the RNO: adding a party is the RNO's.

```
RoamingHub> roamingNetworks QA add chargingStationOperator DE*GEF signingKey pAECIAEhWCB5Vy8g… identityKey pAECIAMhWEIB4zA7… GraphDefined
The ChargingStationOperator 'DE*GEF' was added to the roaming network 'QA', at revision 1, commit adcb769c…. It adds its data now, with change sets it signs.
RoamingHub> roamingNetworks QA
QA  revision 1, commit adcb769c…  Quality assurance
  operated by 'RNO-67BDC1FC', this hub
  RoamingNetworkOperator RNO-67BDC1FC  signing key(s) 882bbf1af577e9dd…, identity key(s) 67bdc1fcdf7c9816…
  ChargingStationOperator DE*GEF  signing key(s) a3fec322ff2c43e0…, identity key(s) 62756187531ce24f…
http://127.0.0.1:2356/poi/RNs/QA
```

or `POST /api/v1/poi/roamingNetworks/QA/chargingStationOperators` with the operator as POI writes it, each key the
Base64 of its COSE key or `{"key": …}`:

```json
{
  "@id":          "DE*GEF",
  "name":         { "en": "GraphDefined" },
  "signingKeys":  [ "pAECIAEhWCB5Vy8g…" ],
  "identityKeys": [ "pAECIAMhWEIB4zA7…" ]
}
```

or the form **New party of a roaming network** below the list on the **Points of interest** page. A private key is
refused - it is no longer private once it was sent - and so is the operator with its charging pools in it: they are
its own to add. E-mobility providers, grid operators and parking operators are added the same way:
`eMobilityProvider`, `gridOperator`, `parkingOperator` at the prompt, `…/eMobilityProviders`, `…/gridOperators`,
`…/parkingOperators` over HTTP; a grid operator not taking part without keys.


## 4. `DE*GEF` commits its first charging pool

One `Add` of the pool, with its station and both EVSEs embedded, signed with the operator's signing key, made for
the version the operator read from the hub and submitted to it - by anybody, without signing in: whether a change set
may change what it changes, its signatures decide. The same program as in step 3, after Enter, with the `HTTPClient`
of Hermod; a change set goes by the method `COMMIT`, which `POI_API.COMMIT` names:

```csharp
await using var http  = new HTTPClient(URL.Parse(args.FirstOrDefault() ?? "http://127.0.0.1:2356"));

// The head of QA, as the hub serves it: checked against its ETags as it is read.
if (!RoamingNetwork.TryParse(JObject.Parse((await http.GET(HTTPPath.Root + "poi/RNs/QA")).HTTPBodyAsUTF8String!), out var head, out var error))
    throw new InvalidOperationException(error);

var pool            = new ChargingPool(
                          ChargingPool_Id.Parse("DE*GEF*P1"),
                          Name:              I18NString.Create(Languages.en, "Jena Paradies"),
                          GeoLocation:       GeoCoordinate.Parse(50.9271, 11.5892),
                          ChargingStations:  [
                              new ChargingStation(
                                  ChargingStation_Id.Parse("DE*GEF*S1"),
                                  EVSEs: [
                                      new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"), MaxPower: Watt.FromW(11000)),
                                      new EVSE(EVSE_Id.Parse("DE*GEF*E1*2"), MaxPower: Watt.FromW(11000))
                                  ]
                              )
                          ]
                      );

var addPool         = head.CreateChangeSet([ RoamingNetworkChange.Add(pool) ],
                                           Description: new ImmutableI18NString(I18NString.Create(Languages.en, "The first charging pool"))).
                           Sign(gefSigningKey, POIPublicKey.From(gefSigningKey).Id, COSEAlgorithm.ES256);

var response        = await http.RunRequest(POI_API.COMMIT,
                                            HTTPPath.Root + "poi/RNs/QA/history/changeSets",
                                            Content:      addPool.ToJSON().ToString().ToUTF8Bytes(),
                                            ContentType:  HTTPContentType.Application.JSON_UTF8);   // 201 Created
```

`CreateChangeSet` makes it for the version it is called on: its `beforeETags` are those of revision 1, its
`afterETags` those of the result. `201 Created` answers with the commit and the head, shortened:

```json
{
  "status":          "Committed",
  "@id":             "2d2a60b9…",
  "revision":        2,
  "parentIds":       [ "adcb769c…" ],
  "changeSetId":     "MSQ5AtSUtAG8542fS12Yh6rCW7x8xv2G",
  "createdAt":       "2026-10-17T18:16:17.709Z",
  "description":     { "en": "The first charging pool" },
  "numberOfChanges": 1,
  "signatureKeyIds": [ "a3fec322…" ],
  "eTags":           [ [ "json", "sha256", "hex", "faf55b2e…" ], [ "cbor", "sha256", "hex", "23340139…" ] ],
  "head":            { "@id": "2d2a60b9…", "revision": 2, … }
}
```

```
RoamingHub> roamingNetworks
QA  revision 2  Quality assurance  (operated by 'RNO-67BDC1FC', this hub, 1 charging station operator(s), 1 charging pool(s))
```

The pool is the operator's alone: the same change set signed by the hub, or by any key of no party, is `422` with
the reason, e.g. `Change 0: Add of the ChargingPool 'DE*GEF*P1' needs a signature by a signing key of the
ChargingStationOperator 'DE*GEF'!`, and nothing changes. With curl, the change set written to a file:

```bash
curl -X COMMIT -H "Content-Type: application/json" --data-binary @addPool.json http://127.0.0.1:2356/poi/RNs/QA/history/changeSets
```

| Request | Returns |
|---|---|
| `GET /poi/RNs` | `[{"@id":"QA","name":{"en":"Quality assurance"}}]` |
| `GET /poi/RNs/QA/CSOs` | `[{"@id":"DE*GEF","name":{"en":"GraphDefined"}}]` |
| `GET /poi/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1` | the pool, its keys in canonical order, its `lastChange` the `createdAt` of its change set |
| `GET /poi/RNs/QA/EVSEs/DE*GEF*E1*2` | `307` to `/poi/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*2` |
| `GET /poi/RNs/QA/history/head` | the head commit: `revision` 2, its `parentIds`, `eTags` and `signatureKeyIds` |

Stations and EVSEs can also be added one by one, `RoamingNetworkChange.Add(station, poolId)` and
`Add(evse, stationId)`, e.g. a third EVSE later.


## 5. The next start

The hub reads its operator back from `roamingNetworkOperator/` and `QA` from `poi/QA`, checking every commit again -
signatures, ETags, merges - and signs the commits it makes of `QA` again. Without one of its private keys it does
not start: a new key would be none `QA` knows.


## Without the hub: POI 3 alone

The same in one C# program, with the usings and `NewKey` of step 3. The RNO is an entity of its own, which the
roaming network embeds:

```csharp
var rnoSigningKey   = NewKey(COSECurve.P256);
var rnoIdentityKey  = NewKey(COSECurve.P521);

// The operator of QA, who may operate other roaming networks as well.
var rno      = new RoamingNetworkOperator(
                   RoamingNetworkOperator_Id.Parse("GraphDefined"),
                   SigningKeys:   [ POIPublicKey.From(rnoSigningKey)  ],
                   IdentityKeys:  [ POIPublicKey.From(rnoIdentityKey) ]
               );

var qa       = new RoamingNetwork(
                   RoamingNetwork_Id.Parse("QA"),
                   Name:      new ImmutableI18NString(I18NString.Create(Languages.en, "Quality assurance")),
                   Operator:  rno
               );

// One directory per roaming network. Start only once, on an empty one:
// on a used one it replaces what is stored. Afterwards: TryLoad, below.
var store    = new RoamingNetworkHistoryFileStore("poi/QA");
var history  = RoamingNetworkHistory.Start(qa, Store: store);

// The RNO adds DE*GEF with the public halves of its keys: adding a party is the RNO's.
var gef      = new ChargingStationOperator(
                   ChargingStationOperator_Id.Parse("DE*GEF"),
                   Name:          I18NString.Create(Languages.en, "GraphDefined"),
                   SigningKeys:   [ POIPublicKey.From(gefSigningKey)  ],
                   IdentityKeys:  [ POIPublicKey.From(gefIdentityKey) ]
               );

history.Commit(history.Head.RoamingNetwork.
                   CreateChangeSet([ RoamingNetworkChange.Add(gef) ]).
                   Sign(rnoSigningKey, POIPublicKey.From(rnoSigningKey).Id, COSEAlgorithm.ES256));   // revision 1

// DE*GEF adds its pool: the pool of step 4.
history.Commit(history.Head.RoamingNetwork.
                   CreateChangeSet([ RoamingNetworkChange.Add(pool) ]).
                   Sign(gefSigningKey, POIPublicKey.From(gefSigningKey).Id, COSEAlgorithm.ES256));   // revision 2
```

`Start(qa, Signers: [ new RoamingNetworkCommitSigner(rnoSigningKey, …, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator) ], …)`
signs every commit as the hub does. `POI_API` serves the history over HTTP, readable by everyone; change sets are
refused unless `AuthorizeChangeSetSubmission` says who may submit them - for `QA` everybody, as their signatures
decide:

```csharp
var httpServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Parse(8080), AutoStart: true);
var poiAPI      = new POI_API(httpServer) {
                      AuthorizeChangeSetSubmission = (request, changeSet) => true
                  };

poiAPI.AddRoamingNetworkHistory(history);
```

From then on the history is loaded from its directory, not started again; `TryLoad` checks every commit again, and
the file `LOCK` keeps a second store out of the directory - one program at a time:

```csharp
using var store = new RoamingNetworkHistoryFileStore("poi/QA");

if (!RoamingNetworkHistory.TryLoad(store, out var history, out var error))
    throw new InvalidOperationException(error);
```


## Next

- [Example 2](example2.md): two pools of a DC and an AC station each, and the stations and EVSEs grouped by their
  current, the groups giving them their power and their connectors.
- More of the pool, the station and the EVSEs: address, opening times, connectors, tariffs, in
  [ChargingPool](../docs/ChargingPool.md), [ChargingStation](../docs/ChargingStation.md), [EVSE](../docs/EVSE.md).
- Changing a property: `RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 22000)`, `CreateChangeSet`
  fills in the old value it finds; see [Change sets](../README.md#change-sets).
- Changing the operator itself, e.g. its name or keys: `RoamingNetworkChange.UpdateProperty(qa.Operator!, "name", …)`,
  see [RoamingNetworkOperator](../docs/RoamingNetworkOperator.md). The hub does it in every roaming network it
  operates: `roamingNetworkOperator rotate signingKey` (or `identityKey`) replaces a key by a new one, each change
  signed by the key it replaces, and `roamingNetworkOperator name GraphDefined GmbH` renames it.
- Another server following `QA`: [Peer](../README.md#peer).
