# Example 2: the charging station operator `DE*GEF`

The roaming network `QA` of [example 1](example1.md) gets its first party: the charging station operator `DE*GEF`,
GraphDefined GmbH. It makes its own keys and keeps them; the hub gets only their public halves and adds it with a
change set it signs as the roaming network operator (RNO) - adding a party is the RNO's. Four ways again - at the prompt
or over SSH, in the web interface, over HTTP with curl or C#, and in C# with POI 3 alone:

| Revision | Signed by | Commit |
|---|---|---|
| 0 | the hub, as the RNO | Genesis: the roaming network `QA`, see [example 1](example1.md) |
| 1 | the hub, as the RNO | `Add` of the charging station operator `DE*GEF`, with the public halves of its keys |

From then on `DE*GEF` adds its data itself, with change sets it signs: [example 3](example3.md). The output is what
one run printed, `…` for elided keys, signatures and digests.


## 1. `DE*GEF` makes its keys

A signing key (P-256), which signs its change sets, and an identity key (P-521), which signs a change of its keys. A
console project of the operator, e.g. in `examples/DE-GEF/`, with the same project file as the one of
[example 1](example1.md#over-http-c); [example 3](example3.md) goes on with it:

```csharp
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP.POI;

// A new private key on the given curve.
static ECPrivateKeyParameters NewKey(COSECurve Curve)
{
    var generator = new ECKeyPairGenerator();
    generator.Init(new ECKeyGenerationParameters(Curve.DomainParameters!, new SecureRandom()));
    return (ECPrivateKeyParameters) generator.GenerateKeyPair().Private;
}

// A private key in a PEM file, PKCS #8, as the hub keeps its own.
static void SaveKey(ECPrivateKeyParameters Key, String File)
    => System.IO.File.WriteAllText(File, new String(PemEncoding.Write("PRIVATE KEY", PrivateKeyInfoFactory.CreatePrivateKeyInfo(Key).GetEncoded())) + "\n");

// The keys of DE*GEF, made once and kept in DE-GEF/, where nobody else may read them.
var gefSigningKey   = NewKey(COSECurve.P256);
var gefIdentityKey  = NewKey(COSECurve.P521);

Directory.CreateDirectory("DE-GEF");
SaveKey(gefSigningKey,  "DE-GEF/signing-key.pem");
SaveKey(gefIdentityKey, "DE-GEF/identity-key.pem");

// What the hub gets: the operator as POI writes it, with the public halves of its keys, each the Base64 of its COSE key.
var gef = new JObject(
              new JProperty("@id",           "DE*GEF"),
              new JProperty("name",          new JObject(new JProperty("en", "GraphDefined GmbH"))),
              new JProperty("signingKeys",   new JArray(POIPublicKey.From(gefSigningKey). ToJSON()["key"])),
              new JProperty("identityKeys",  new JArray(POIPublicKey.From(gefIdentityKey).ToJSON()["key"]))
          );

File.WriteAllText("DE-GEF/operator.json", gef.ToString());

Console.WriteLine($"signingKey  {gef["signingKeys"]! [0]}");
Console.WriteLine($"identityKey {gef["identityKeys"]![0]}");
```

```
signingKey  pAECIAEhWCB5Vy8gs2bOr0hjZtbdc/XbBab5nzh3mkRz7A9iGuDKISJY…
identityKey pAECIAMhWEIB4zA7f9XhNrIzPv42Zza3sqVlH+RfPf3ukr4R7y5nrh0H…
```

`DE-GEF/signing-key.pem` and `DE-GEF/identity-key.pem` stay with the operator; `DE-GEF/operator.json`, with the public
halves only, goes to the administrator of the hub.


## 2. Adding it to `QA`

By an administrator (`poi:edit`), signed in as in [example 1](example1.md#3-who-may-make-a-roaming-network); the hub
signs it as the RNO.

### At the prompt or over SSH

```
RoamingHub> roamingNetworks QA add chargingStationOperator DE*GEF signingKey pAECIAEhWCB5Vy8g… identityKey pAECIAMhWEIB4zA7… GraphDefined GmbH
The ChargingStationOperator 'DE*GEF' was added to the roaming network 'QA', at revision 1, commit adcb769c…. It adds its data now, with change sets it signs.
```

Each key after the word that says which kind it is, `signingKey` or `identityKey`, as many of each as the operator has;
the words after the keys are its name.

### In the web interface

**Points of interest**, below the list of the roaming networks, the card **New party of a roaming network**: *Roaming
network* `QA`, *Party* `Charging station operator`, *Identification* `DE*GEF`, *Name* `GraphDefined GmbH`, the two
keys in *Signing keys* and *Identity keys*, one per line, then **Add the party**. Added, the page goes to the charging
station operators of `QA`.

### Over HTTP: curl

`POST /api/v1/poi/RNs/QA/CSOs` - below `/api/v1` the path `GET /poi/RNs/QA/CSOs` lists it at - with the operator as
POI writes it, each key the Base64 of its COSE key or `{"key": …}`: `operator.json` as it is.

```bash
curl -X POST http://127.0.0.1:2356/api/v1/poi/RNs/QA/CSOs \
     -H "API-Key: $ROAMINGHUB_API_KEY" -H "Content-Type: application/json" \
     --data-binary @DE-GEF/operator.json
```

`201 Created`, with the commit that added it:

```json
{
  "message":  "The ChargingStationOperator 'DE*GEF' was added to the roaming network 'QA', at revision 1, commit adcb769c…. It adds its data now, with change sets it signs.",
  "revision": 1,
  "commitId": "adcb769c…"
}
```

### Over HTTP: C#

The program of the administrator of [example 1](example1.md#over-http-c), its `Post` taking the `operator.json`
`DE*GEF` sent:

```csharp
await Post("/api/v1/poi/RNs/QA/CSOs", JObject.Parse(File.ReadAllText(args.ElementAtOrDefault(1) ?? "DE-GEF/operator.json")));
```

```
201: The ChargingStationOperator 'DE*GEF' was added to the roaming network 'QA', at revision 1, commit adcb769c…. It adds its data now, with change sets it signs.
```

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


## 3. What `QA` says now

```
RoamingHub> roamingNetworks QA
QA  revision 1, commit adcb769c…  Quality assurance
  operated by 'RNO-67BDC1FC', this hub
  RoamingNetworkOperator RNO-67BDC1FC  signing key(s) 882bbf1af577e9dd…, identity key(s) 67bdc1fcdf7c9816…
  ChargingStationOperator DE*GEF  signing key(s) a3fec322ff2c43e0…, identity key(s) 62756187531ce24f…
http://127.0.0.1:2356/poi/RNs/QA
```

`GET /poi/RNs/QA/CSOs`, for everybody:

```json
[ { "@id": "DE*GEF", "name": { "en": "GraphDefined GmbH" } } ]
```

`GET /poi/RNs/QA/CSOs/DE*GEF` the operator with the public halves of its keys, and `GET /poi/RNs/QA/history/head` the
commit, its `signatureKeyIds` the one of the hub's signing key. The web interface has it under **Points of interest →
QA → Charging station operators**.


## 4. The other parties

E-mobility providers, grid operators and parking operators are added the same way: `eMobilityProvider`,
`gridOperator`, `parkingOperator` at the prompt, *Party* in the form, `…/EMPs`, `…/GOs`, `…/POs` over HTTP. A grid
operator not taking part comes without keys; the RNO signs in its place.


## 5. Without the hub: POI 3 alone

The program of [example 1, step 5](example1.md#5-without-the-hub-poi-3-alone), in the same directory, its usings and
helpers, and `QA` loaded with `TryLoad`; `DE-GEF/` there as step 1 made it. The RNO adds `DE*GEF` with the public
halves of its keys, read from the private ones the operator keeps:

```csharp
var gefSigningKey   = LoadKey("DE-GEF/signing-key.pem");
var gefIdentityKey  = LoadKey("DE-GEF/identity-key.pem");

var gef             = new ChargingStationOperator(
                          ChargingStationOperator_Id.Parse("DE*GEF"),
                          Name:          I18NString.Create(Languages.en, "GraphDefined GmbH"),
                          SigningKeys:   [ POIPublicKey.From(gefSigningKey)  ],
                          IdentityKeys:  [ POIPublicKey.From(gefIdentityKey) ]
                      );

var addGEF          = history.Commit(history.Head.RoamingNetwork.
                                         CreateChangeSet([ RoamingNetworkChange.Add(gef) ],
                                                         Description: new ImmutableI18NString(I18NString.Create(Languages.en, "DE*GEF"))).
                                         Sign(rnoSigningKey, POIPublicKey.From(rnoSigningKey).Id, COSEAlgorithm.ES256));

Console.WriteLine($"{addGEF.Status}, revision {addGEF.Commit?.Revision}: DE*GEF");
```

```
Committed, revision 1: DE*GEF
```

Here the program has the operator's private keys only because it plays both; an RNO gets the public halves alone, e.g.
`POIPublicKey.Parse` of what `operator.json` has.


## Next

- [Example 3](example3.md): `DE*GEF` adds its templates, two charging pools of a DC and an AC station each, and their
  groups, with change sets it signs.
- Its keys replaced: a change of `signingKeys` signed by its identity key, see
  [ChargingStationOperator](../docs/ChargingStationOperator.md).
