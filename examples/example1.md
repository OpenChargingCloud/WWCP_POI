# Example 1: a roaming network, an operator and its first charging pool

From a fresh installation of the [RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) to a roaming
network `QA` with one charging station operator and its first charging pool, in three commits, each signed by the
one whose data it changes:

| Revision | Signed by | Commit |
|---|---|---|
| 0 | the hub, as the roaming network operator (RNO) | Genesis: the roaming network `QA`, operated by the hub |
| 1 | the hub, as the RNO | `Add` of the charging station operator `DE*QAC`, with the public halves of its keys |
| 2 | `DE*QAC` | `Add` of the charging pool `DE*QAC*P1` with the charging station `DE*QAC*S1` and the EVSEs `DE*QAC*E1*1` and `DE*QAC*E1*2` |

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
15:38:51.445 [notice poi security] This hub is the roaming network operator 'RNO-103B0540' now: its signing key 784c5f9e6ae32260… and its identity key 103b0540a41fb6c0… were made in '…\roamingNetworkOperator'. Keep a copy of them where nobody else can read it: without its identity key, the keys of the roaming networks it operates can never be replaced.
…
15:38:51.662 [info poi] The points of interest are at http://127.0.0.1:2356/poi/RNs (0 roaming network(s)).
…
  RNO             this hub is the roaming network operator 'RNO-103B0540', made now, its keys in …\roamingNetworkOperator; 0 roaming network(s) operated
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
This hub is the roaming network operator 'RNO-103B0540', since 2026-10-17T13:38:51.443Z.
  signing key   784c5f9e6ae322609643dc8b9b70945eb03daf90abf83e6b2f9d9c14835e30d4  pAECIAEhWCAxcg8YFtfTUWXEXoQQIaalzXEO…
  identity key  103b0540a41fb6c0c12edfc65da0b4f9b75f4028320ad1fc35c5fdc0bb6b20d3  pAECIAMhWEIByCvzPoUTwLnJnuUfg14i0vaU…
  its private keys are kept in '…\roamingNetworkOperator'
  it operates no roaming network yet: 'roamingNetworks add <id> [<name>]' makes one
RoamingHub> roamingNetworks
This hub serves no roaming network yet: 'roamingNetworks add <id> [<name>]' makes one, operated by this hub, 'RNO-103B0540'.
```

`GET /api/v1/poi/roamingNetworkOperator` answers with `operator.json`, public as it is in every roaming network the
hub operates:

```json
{
  "@id":          "RNO-103B0540",
  "signingKeys":  [ { "@id": "784c5f9e…", "key": "pAECIAEh…" } ],
  "identityKeys": [ { "@id": "103b0540…", "key": "pAECIAMh…" } ],
  "created":      "2026-10-17T13:38:51.443Z",
  "lastChange":   "2026-10-17T13:38:51.443Z"
}
```


## 2. The first roaming network: `QA`

By an administrator (`poi:edit`), with nothing but its identification and its name - its operator is the hub:

```
RoamingHub> roamingNetworks add QA Quality assurance
The roaming network 'QA' was made, at revision 0, commit 4d99ddd0…, operated by this hub, 'RNO-103B0540'. Its parties are added now, with change sets this hub signs.
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
    "@id":          "RNO-103B0540",
    "signingKeys":  [ { "@id": "784c5f9e…", "key": "pAECIAEh…" } ],
    "identityKeys": [ { "@id": "103b0540…", "key": "pAECIAMh…" } ],
    "created":      "2026-10-17T13:38:51.443Z",
    "lastChange":   "2026-10-17T13:38:51.443Z"
  },
  "created":    "2026-10-17T13:38:52.114Z",
  "lastChange": "2026-10-17T13:38:52.114Z",
  "eTags": [ [ "json", "sha256", "hex", "…" ], [ "cbor", "sha256", "hex", "…" ] ]
}
```

Its genesis commit, `GET /poi/RNs/QA/history/commits/{id}`, is signed by the hub's signing key, in the role of the
roaming network operator - as every commit the hub makes of a roaming network it operates:

```json
{
  "@id":              "4d99ddd0…",
  "roamingNetworkId": "QA",
  "revision":         0,
  "eTags":            [ [ "json", "sha256", "hex", "248ebf99…" ], [ "cbor", "sha256", "hex", "222794eb…" ] ],
  "signatures": [
    {
      "role":      "roamingNetworkOperator",
      "signer":    "RNO-103B0540",
      "signedAt":  "2026-10-17T13:38:52.160Z",
      "algorithm": "ES256",
      "keyId":     "784c5f9e…",
      "value":     "u2X+7gWA…",
      "profile":   "wwcp-poi-commit-signature-v1",
      "encoding":  "base64"
    }
  ]
}
```


## 3. The first charging station operator: `DE*QAC`

`DE*QAC` makes its own keys and keeps them; the hub gets only their public halves. A console project with
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
using System.Text;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;

// A new private key on the given curve.
static ECPrivateKeyParameters NewKey(COSECurve Curve)
{
    var generator = new ECKeyPairGenerator();
    generator.Init(new ECKeyGenerationParameters(Curve.DomainParameters!, new SecureRandom()));
    return (ECPrivateKeyParameters) generator.GenerateKeyPair().Private;
}

// The keys of DE*QAC: kept here, and only their public halves, the Base64 of their COSE keys, given to the hub.
var qacSigningKey   = NewKey(COSECurve.P256);
var qacIdentityKey  = NewKey(COSECurve.P521);

Console.WriteLine($"signingKey  {POIPublicKey.From(qacSigningKey). ToJSON()["key"]}");
Console.WriteLine($"identityKey {POIPublicKey.From(qacIdentityKey).ToJSON()["key"]}");
```

An administrator adds it, and the hub signs it as the RNO: adding a party is the RNO's.

```
RoamingHub> roamingNetworks QA add chargingStationOperator DE*QAC signingKey pAECIAEhWCATKrct… identityKey pAECIAMhWEIBQLmZ… QA Charging
The ChargingStationOperator 'DE*QAC' was added to the roaming network 'QA', at revision 1, commit d403e4d9…. It adds its data now, with change sets it signs.
RoamingHub> roamingNetworks QA
QA  revision 1, commit d403e4d9…  Quality assurance
  operated by 'RNO-103B0540', this hub
  RoamingNetworkOperator RNO-103B0540  signing key(s) 784c5f9e6ae32260…, identity key(s) 103b0540a41fb6c0…
  ChargingStationOperator DE*QAC  signing key(s) f64385e056011b22…, identity key(s) c0dc20a6328a2956…
http://127.0.0.1:2356/poi/RNs/QA
```

or `POST /api/v1/poi/roamingNetworks/QA/chargingStationOperators` with the operator as POI writes it, each key the
Base64 of its COSE key or `{"key": …}`:

```json
{
  "@id":          "DE*QAC",
  "name":         { "en": "QA Charging" },
  "signingKeys":  [ "pAECIAEhWCATKrct…" ],
  "identityKeys": [ "pAECIAMhWEIBQLmZ…" ]
}
```

or the form **New party of a roaming network** below the list on the **Points of interest** page. A private key is
refused - it is no longer private once it was sent - and so is the operator with its charging pools in it: they are
its own to add. E-mobility providers, grid operators and parking operators are added the same way:
`eMobilityProvider`, `gridOperator`, `parkingOperator` at the prompt, `…/eMobilityProviders`, `…/gridOperators`,
`…/parkingOperators` over HTTP; a grid operator not taking part without keys.


## 4. `DE*QAC` commits its first charging pool

One `Add` of the pool, with its station and both EVSEs embedded, signed with the operator's signing key, made for
the version the operator read from the hub and submitted to it - by anybody, without signing in: whether a change set
may change what it changes, its signatures decide. The same program as in step 3:

```csharp
using var http      = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:2356/poi/") };

// The head of QA, as the hub serves it: checked against its ETags as it is read.
if (!RoamingNetwork.TryParse(JObject.Parse(await http.GetStringAsync("RNs/QA")), out var head, out var error))
    throw new InvalidOperationException(error);

var pool            = new ChargingPool(
                          ChargingPool_Id.Parse("DE*QAC*P1"),
                          Name:              I18NString.Create(Languages.en, "QA Pool 1"),
                          GeoLocation:       GeoCoordinate.Parse(50.9271, 11.5892),
                          ChargingStations:  [
                              new ChargingStation(
                                  ChargingStation_Id.Parse("DE*QAC*S1"),
                                  EVSEs: [
                                      new EVSE(EVSE_Id.Parse("DE*QAC*E1*1"), MaxPower: Watt.FromW(11000)),
                                      new EVSE(EVSE_Id.Parse("DE*QAC*E1*2"), MaxPower: Watt.FromW(11000))
                                  ]
                              )
                          ]
                      );

var addPool         = head.CreateChangeSet([ RoamingNetworkChange.Add(pool) ],
                                           Description: new ImmutableI18NString(I18NString.Create(Languages.en, "The first charging pool"))).
                           Sign(qacSigningKey, POIPublicKey.From(qacSigningKey).Id, COSEAlgorithm.ES256);

using var request   = new HttpRequestMessage(new HttpMethod("COMMIT"), "RNs/QA/history/changeSets") {
                          Content = new StringContent(addPool.ToJSON().ToString(), Encoding.UTF8, "application/json")
                      };

using var response  = await http.SendAsync(request);   // 201 Created
```

`CreateChangeSet` makes it for the version it is called on: its `beforeETags` are those of revision 1, its
`afterETags` those of the result. `201 Created` answers with the commit and the head, shortened:

```json
{
  "status":          "Committed",
  "@id":             "e2e6fe42…",
  "revision":        2,
  "parentIds":       [ "d403e4d9…" ],
  "changeSetId":     "bjS8G6tth41fpSv1f9p2W6tKUWWj7C3b",
  "createdAt":       "2026-10-17T13:38:53.249Z",
  "description":     { "en": "The first charging pool" },
  "numberOfChanges": 1,
  "signatureKeyIds": [ "f64385e0…" ],
  "eTags":           [ [ "json", "sha256", "hex", "1f3aa569…" ], [ "cbor", "sha256", "hex", "ebf4371a…" ] ],
  "head":            { "@id": "e2e6fe42…", "revision": 2, … }
}
```

```
RoamingHub> roamingNetworks
QA  revision 2  Quality assurance  (operated by 'RNO-103B0540', this hub, 1 charging station operator(s), 1 charging pool(s))
```

The pool is the operator's alone: the same change set signed by the hub, or by any key of no party, is `422` with
the reason, e.g. `Change 0: Add of the ChargingPool 'DE*QAC*P1' needs a signature by a signing key of the
ChargingStationOperator 'DE*QAC'!`, and nothing changes. With curl, the change set written to a file:

```bash
curl -X COMMIT -H "Content-Type: application/json" --data-binary @addPool.json http://127.0.0.1:2356/poi/RNs/QA/history/changeSets
```

| Request | Returns |
|---|---|
| `GET /poi/RNs` | `[{"@id":"QA","name":{"en":"Quality assurance"}}]` |
| `GET /poi/RNs/QA/CSOs` | `[{"@id":"DE*QAC","name":{"en":"QA Charging"}}]` |
| `GET /poi/RNs/QA/CSOs/DE*QAC/CPs/DE*QAC*P1` | the pool, its keys in canonical order, its `lastChange` the `createdAt` of its change set |
| `GET /poi/RNs/QA/EVSEs/DE*QAC*E1*2` | `307` to `/poi/RNs/QA/CSOs/DE*QAC/CPs/DE*QAC*P1/CSs/DE*QAC*S1/EVSEs/DE*QAC*E1*2` |
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

// The RNO adds DE*QAC with the public halves of its keys: adding a party is the RNO's.
var qac      = new ChargingStationOperator(
                   ChargingStationOperator_Id.Parse("DE*QAC"),
                   Name:          I18NString.Create(Languages.en, "QA Charging"),
                   SigningKeys:   [ POIPublicKey.From(qacSigningKey)  ],
                   IdentityKeys:  [ POIPublicKey.From(qacIdentityKey) ]
               );

history.Commit(history.Head.RoamingNetwork.
                   CreateChangeSet([ RoamingNetworkChange.Add(qac) ]).
                   Sign(rnoSigningKey, POIPublicKey.From(rnoSigningKey).Id, COSEAlgorithm.ES256));   // revision 1

// DE*QAC adds its pool: the pool of step 4.
history.Commit(history.Head.RoamingNetwork.
                   CreateChangeSet([ RoamingNetworkChange.Add(pool) ]).
                   Sign(qacSigningKey, POIPublicKey.From(qacSigningKey).Id, COSEAlgorithm.ES256));   // revision 2
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

- More of the pool, the station and the EVSEs: address, opening times, connectors, tariffs, in
  [ChargingPool](../docs/ChargingPool.md), [ChargingStation](../docs/ChargingStation.md), [EVSE](../docs/EVSE.md).
- Changing a property: `RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 22000)`, `CreateChangeSet`
  fills in the old value it finds; see [Change sets](../README.md#change-sets).
- Changing the operator itself, e.g. its name or keys: `RoamingNetworkChange.UpdateProperty(qa.Operator!, "name", …)`,
  see [RoamingNetworkOperator](../docs/RoamingNetworkOperator.md). The hub does it in every roaming network it
  operates: `roamingNetworkOperator rotate signingKey` (or `identityKey`) replaces a key by a new one, each change
  signed by the key it replaces, and `roamingNetworkOperator name GraphDefined GmbH` renames it.
- Another server following `QA`: [Peer](../README.md#peer).
