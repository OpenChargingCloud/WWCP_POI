# Example 1: a roaming network, an operator and its first charging pool

From a fresh installation, an empty directory, to a roaming network `QA` with one charging station operator and
its first charging pool, in three commits, each signed by the one whose data it changes:

| Revision | Signed by | Commit |
|---|---|---|
| 0 | — | Genesis: the roaming network `QA` with the keys of its roaming network operator (RNO) |
| 1 | the RNO | `Add` of the charging station operator `DE*QAC`, with its keys |
| 2 | `DE*QAC` | `Add` of the charging pool `DE*QAC*P1` with the charging station `DE*QAC*S1` and the EVSEs `DE*QAC*E1*1` and `DE*QAC*E1*2` |

The properties are kept to a minimum: names, a location and the power of the EVSEs. All the others are in
[docs/](../docs/README.md). The code blocks of the setup and of steps 1 to 4, up to
[Submitting over HTTP](#submitting-over-http), are one `Program.cs`, in this order; the JSON is what it printed in
one run, `…` for elided keys, signatures and digests.


## Setup

A console project with WWCP_POI3, e.g. in `examples/Example1/`:

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
```


## 1. The RNO creates the roaming network `QA`

The RNO has no entity of its own: it is whoever holds the private halves of the keys the network names. Its
signing key (P-256) signs its change sets, its identity key (P-521) only changes of keys. Keep both private keys
safe: nobody can replace the identity keys of the RNO once they are lost, and a network for production should name
at least two of them, see [RoamingNetwork](../docs/RoamingNetwork.md#signingkeys-and-identitykeys).

```csharp
var rnoSigningKey   = NewKey(COSECurve.P256);
var rnoIdentityKey  = NewKey(COSECurve.P521);

var qa       = new RoamingNetwork(
                   RoamingNetwork_Id.Parse("QA"),
                   Name:          new ImmutableI18NString(I18NString.Create(Languages.en, "QA")),
                   SigningKeys:   [ POIPublicKey.From(rnoSigningKey)  ],
                   IdentityKeys:  [ POIPublicKey.From(rnoIdentityKey) ]
               );

// One directory per roaming network. Start only once, on an empty one:
// on a used one it replaces what is stored. Afterwards: TryLoad, see step 5.
var store    = new RoamingNetworkHistoryFileStore("poi/QA");
var history  = RoamingNetworkHistory.Start(qa, Store: store);
```

`Start` writes the genesis commit, revision 0, to `poi/QA` (`CURRENT`, `gen-000001/base.json`,
`gen-000001/log.jsonl`). The network, `GET ~/RNs/QA`:

```json
{
  "@id":          "QA",
  "@context":     "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork",
  "name":         { "en": "QA" },
  "signingKeys":  [ { "@id": "7f9d6c1b…", "key": "pAECIAEh…" } ],
  "identityKeys": [ { "@id": "d9d9c838…", "key": "pAECIAMh…" } ],
  "created":      "2026-10-17T00:25:17.835Z",
  "lastChange":   "2026-10-17T00:25:17.835Z",
  "eTags": [
    [ "json", "sha256", "hex", "9eb9cd09…" ],
    [ "cbor", "sha256", "hex", "4dde82e3…" ]
  ]
}
```


## 2. The RNO adds the charging station operator `DE*QAC`

The operator makes its own keys and keeps them; the RNO gets only their public halves, e.g. as
`POIPublicKey.ToJSON()` writes them. Adding a party is the RNO's, so the RNO signs.

```csharp
// At DE*QAC: its keys.
var qacSigningKey   = NewKey(COSECurve.P256);
var qacIdentityKey  = NewKey(COSECurve.P521);

// At the RNO: the operator with the public halves of its keys.
var qac          = new ChargingStationOperator(
                       ChargingStationOperator_Id.Parse("DE*QAC"),
                       Name:          I18NString.Create(Languages.en, "QA Charging"),
                       SigningKeys:   [ POIPublicKey.From(qacSigningKey)  ],
                       IdentityKeys:  [ POIPublicKey.From(qacIdentityKey) ]
                   );

var addOperator  = history.Head.RoamingNetwork.
                       CreateChangeSet([ RoamingNetworkChange.Add(qac) ],
                                       Description: new ImmutableI18NString(I18NString.Create(Languages.en, "Add DE*QAC"))).
                       Sign(rnoSigningKey, POIPublicKey.From(rnoSigningKey).Id, COSEAlgorithm.ES256);

var result1      = history.Commit(addOperator);   // Committed, revision 1
```

`CreateChangeSet` makes it for the version it is called on, the head: its `beforeETags` are those of revision 0,
its `afterETags` those of the result.

```json
{
  "@id":              "1ftQ88xQ9vxp6W66YEYd4GA55np97S5d",
  "roamingNetworkId": "QA",
  "createdAt":        "2026-10-17T00:25:18.649Z",
  "description":      { "en": "Add DE*QAC" },
  "changes": [
    {
      "kind":       "Add",
      "entityType": "ChargingStationOperator",
      "entityId":   "DE*QAC",
      "newValue": {
        "@id":          "DE*QAC",
        "name":         { "en": "QA Charging" },
        "signingKeys":  [ { "@id": "fc36dfc7…", "key": "pAECIAEh…" } ],
        "identityKeys": [ { "@id": "34e8eb9f…", "key": "pAECIAMh…" } ],
        "created":      "2026-10-17T00:25:18.555Z",
        "lastChange":   "2026-10-17T00:25:18.555Z"
      }
    }
  ],
  "beforeETags": [ [ "json", "sha256", "hex", "9eb9cd09…" ], [ "cbor", "sha256", "hex", "4dde82e3…" ] ],
  "afterETags":  [ [ "json", "sha256", "hex", "a5b43ff4…" ], [ "cbor", "sha256", "hex", "bfa3d692…" ] ],
  "signatures": [
    {
      "algorithm": "ES256",
      "keyId":     "7f9d6c1b…",
      "value":     "L5RCJCGU…",
      "profile":   "wwcp-poi-changeset-json-v3",
      "encoding":  "base64"
    }
  ]
}
```

The operator is added without children: its pools, tariffs and groups are its own to add.


## 3. `DE*QAC` commits its first charging pool

One `Add` of the pool, with its station and both EVSEs embedded, signed with the operator's signing key. The operator
makes it for the version it knows, here the head of the history; a remote operator reads it first, e.g. by
`GET ~/RNs/QA/history/snapshot`, and submits over HTTP, see [step 4](#submitting-over-http).

```csharp
var pool     = new ChargingPool(
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

var addPool  = history.Head.RoamingNetwork.
                   CreateChangeSet([ RoamingNetworkChange.Add(pool) ],
                                   Description: new ImmutableI18NString(I18NString.Create(Languages.en, "Add DE*QAC*P1"))).
                   Sign(qacSigningKey, POIPublicKey.From(qacSigningKey).Id, COSEAlgorithm.ES256);

var result2  = history.Commit(addPool);   // Committed, revision 2
```

```json
{
  "@id":              "Q48xz1A9MnzGv1S56SSntQ7b7U8rS1b2",
  "roamingNetworkId": "QA",
  "createdAt":        "2026-10-17T00:25:19.104Z",
  "description":      { "en": "Add DE*QAC*P1" },
  "changes": [
    {
      "kind":             "Add",
      "entityType":       "ChargingPool",
      "entityId":         "DE*QAC*P1",
      "parentEntityType": "ChargingStationOperator",
      "parentEntityId":   "DE*QAC",
      "newValue": {
        "@id":         "DE*QAC*P1",
        "name":        { "en": "QA Pool 1" },
        "geoLocation": { "lat": 50.9271, "lng": 11.5892 },
        "chargingStations": [
          {
            "@id": "DE*QAC*S1",
            "EVSEs": [
              { "@id": "DE*QAC*E1*1", "maxPower": 11000,
                "created": "2026-10-17T00:25:18.915Z", "lastChange": "2026-10-17T00:25:18.915Z" },
              { "@id": "DE*QAC*E1*2", "maxPower": 11000,
                "created": "2026-10-17T00:25:18.937Z", "lastChange": "2026-10-17T00:25:18.937Z" }
            ],
            "created":    "2026-10-17T00:25:18.942Z",
            "lastChange": "2026-10-17T00:25:18.942Z"
          }
        ],
        "created":    "2026-10-17T00:25:18.982Z",
        "lastChange": "2026-10-17T00:25:18.982Z"
      }
    }
  ],
  "beforeETags": [ [ "json", "sha256", "hex", "a5b43ff4…" ], [ "cbor", "sha256", "hex", "bfa3d692…" ] ],
  "afterETags":  [ [ "json", "sha256", "hex", "37e0b5aa…" ], [ "cbor", "sha256", "hex", "bc29af69…" ] ],
  "signatures": [
    {
      "algorithm": "ES256",
      "keyId":     "fc36dfc7…",
      "value":     "O/MK6x2Y…",
      "profile":   "wwcp-poi-changeset-json-v3",
      "encoding":  "base64"
    }
  ]
}
```

The pool is the operator's alone. The same change set signed by the RNO is refused, and nothing changes:

```
Change 0: Add of the ChargingPool 'DE*QAC*P1' needs a signature by a signing key of the ChargingStationOperator 'DE*QAC'!
```

Stations and EVSEs can also be added one by one, `RoamingNetworkChange.Add(station, poolId)` and
`Add(evse, stationId)`, e.g. a third EVSE later.


## 4. Serving it

`POI_API` serves the history over HTTP, readable by everyone:

```csharp
var httpServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Parse(8080), AutoStart: true);
var poiAPI      = new POI_API(httpServer);

poiAPI.AddRoamingNetworkHistory(history);

Console.ReadLine();

await httpServer.DisposeAsync();
await poiAPI.DisposeAsync();
store.Dispose();
```

```bash
curl http://127.0.0.1:8080/RNs/QA/CSOs/DE*QAC/CPs/DE*QAC*P1
```

| Request | Returns |
|---|---|
| `GET ~/RNs` | `[{"@id":"QA","name":{"en":"QA"}}]` |
| `GET ~/RNs/QA/CSOs` | `[{"@id":"DE*QAC","name":{"en":"QA Charging"}}]` |
| `GET ~/RNs/QA/CSOs/DE*QAC/CPs/DE*QAC*P1` | the pool, below |
| `GET ~/RNs/QA/EVSEs/DE*QAC*E1*2` | `307` to `~/RNs/QA/CSOs/DE*QAC/CPs/DE*QAC*P1/CSs/DE*QAC*S1/EVSEs/DE*QAC*E1*2` |
| `GET ~/RNs/QA/history/head` | the head commit: `revision` 2, its `parentIds`, `eTags` and `signatureKeyIds` |

The pool as served, its keys in canonical order, its `lastChange` set to the `createdAt` of its change set:

```json
{
  "@id": "DE*QAC*P1",
  "chargingStations": [
    {
      "@id": "DE*QAC*S1",
      "EVSEs": [
        { "@id": "DE*QAC*E1*1", "created": "2026-10-17T00:25:18.915Z", "lastChange": "2026-10-17T00:25:18.915Z", "maxPower": 11000 },
        { "@id": "DE*QAC*E1*2", "created": "2026-10-17T00:25:18.937Z", "lastChange": "2026-10-17T00:25:18.937Z", "maxPower": 11000 }
      ],
      "created":    "2026-10-17T00:25:18.942Z",
      "lastChange": "2026-10-17T00:25:18.942Z"
    }
  ],
  "created":     "2026-10-17T00:25:18.982Z",
  "geoLocation": { "lat": 50.9271, "lng": 11.5892 },
  "lastChange":  "2026-10-17T00:25:19.104Z",
  "name":        { "en": "QA Pool 1" }
}
```

### Submitting over HTTP

Submitting change sets is refused unless `AuthorizeChangeSetSubmission` says who may; what a change set may
change, its signatures decide. For `QA`, everybody may submit:

```csharp
var poiAPI = new POI_API(httpServer) {
                 AuthorizeChangeSetSubmission = (request, changeSet) => true
             };
```

The RNO and the operator then send their signed change sets instead of calling `history.Commit`, e.g. step 3:

```csharp
using var http      = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8080/") };
using var request   = new HttpRequestMessage(new HttpMethod("COMMIT"), "RNs/QA/history/changeSets") {
                          Content = new StringContent(CanonicalJSON.Serialize(addPool.ToJSON()), Encoding.UTF8, "application/json")
                      };
using var response  = await http.SendAsync(request);   // 201 Created
```

```bash
curl -X COMMIT -H "Content-Type: application/json" --data-binary @addPool.json http://127.0.0.1:8080/RNs/QA/history/changeSets
```

Canonical JSON, `CanonicalJSON.Serialize(…)`, is what the API and the WebSocket send themselves; `ToJSON().ToString()`
does as well, as `ToJSON()` writes `maxPower` 11000 as the integer it is, and the server reads the change set with
the signature as it was made.

`201 Created` answers with the commit, the `head` and the conflicts left, shortened:

```json
{
  "status":          "Committed",
  "@id":             "44c435fd…",
  "revision":        2,
  "parentIds":       [ "8333de5e…" ],
  "changeSetId":     "Q48xz1A9MnzGv1S56SSntQ7b7U8rS1b2",
  "createdAt":       "2026-10-17T00:25:19.104Z",
  "description":     { "en": "Add DE*QAC*P1" },
  "numberOfChanges": 1,
  "signatureKeyIds": [ "fc36dfc7…" ],
  "eTags":           [ [ "json", "sha256", "hex", "37e0b5aa…" ], [ "cbor", "sha256", "hex", "bc29af69…" ] ],
  "head":            { "@id": "44c435fd…", "revision": 2, … }
}
```

A change set not signed by its owner is `422` with the reason, e.g.
`Change 0: UpdateProperty of the ChargingStationOperator 'DE*QAC' ('telephone') needs a signature by a signing key of the ChargingStationOperator 'DE*QAC'!`.


## 5. The next start

From now on the history is loaded from its directory, not started again. `TryLoad` checks every commit again:
signatures, ETags, merges.

```csharp
using var store = new RoamingNetworkHistoryFileStore("poi/QA");

if (!RoamingNetworkHistory.TryLoad(store, out var history, out var error))
    throw new InvalidOperationException(error);

// history.Head.Commit.Revision == 2, history.Head.RoamingNetwork has DE*QAC with DE*QAC*P1
```

The file `LOCK` keeps a second store out of the directory: one program at a time.


## The same with the RoamingHub

The [RoamingHub](https://github.com/OpenChargingCloud/RoamingHub#the-points-of-interest) does steps 1, 4 and 5
itself: it makes the roaming network, stores it in `poi/QA` beside its configuration file, serves it below `/poi`
and reads it back at every start. Only the public keys of the RNO go there; the RNO and the operator keep their
private keys and sign their change sets themselves, as in steps 2 and 3.

Step 1, by an administrator (`poi:edit`), at the hub's prompt or over SSH, each key the Base64 of its COSE key as
`POIPublicKey.ToJSON()` writes it under `key`:

```
RoamingHub> roamingNetworks add QA signingKey pAECIAEh... identityKey pAECIAMh... QA
```

or `POST /api/v1/poi/roamingNetworks` with `{"@id": "QA", "name": {"en": "QA"}, "signingKeys": ["pAECIAEh..."],
"identityKeys": ["pAECIAMh..."]}`, or the form below the list on the **Points of interest** page.

Steps 2 and 3: the change sets, made against the version the RNO and the operator read, e.g. by
`GET /poi/RNs/QA/history/snapshot` and `RoamingNetworkHistory.TryFromSnapshot`, and submitted to the hub as in
[Submitting over HTTP](#submitting-over-http), below `/poi`:

```bash
curl -X COMMIT -H "Content-Type: application/json" --data-binary @addPool.json http://127.0.0.1:2356/poi/RNs/QA/history/changeSets
```

The hub takes change sets from everybody, without signing in: whether one may change what it changes, its
signatures decide.


## Next

- More of the pool, the station and the EVSEs: address, opening times, connectors, tariffs, in
  [ChargingPool](../docs/ChargingPool.md), [ChargingStation](../docs/ChargingStation.md), [EVSE](../docs/EVSE.md).
- Changing a property: `RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 22000)`, `CreateChangeSet`
  fills in the old value it finds; see [Change sets](../README.md#change-sets).
- Another server following `QA`: [Peer](../README.md#peer).
