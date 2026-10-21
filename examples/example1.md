# Example 1: the first start and the roaming network `QA`

From a fresh installation of the [RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) to its first roaming network, `QA`: what the first start makes, who may make a roaming network, and four ways to make it - at the prompt or over SSH, in the web interface, over HTTP with curl or C#, and in C# with POI 3 alone, without a hub. Each
makes the same: revision 0 of `QA`, its genesis commit, signed by the hub as the roaming network operator (RNO).

[Example 2](example2.md) adds the charging station operator `DE*GEF` to `QA`, [example 3](example3.md) its charging
pools; [README](README.md) says what each is about. The output is what one run printed, `…` for elided keys, paths,
signatures and digests.


## 1. The first start

```bash
dotnet run --project RoamingHubCLI -- --authorize-ssh-key root=C:\Users\you\.ssh\id_ed25519.pub
```

A first start, beside an empty `configuration.json` or none, makes up what the hub needs and says so once:

- **the account `root`**, an administrator, with a password shown once in the box at the end of the banner, and the
  SSH key given by `--authorize-ssh-key` (without it, a key pair made up, its private key shown once below the
  password);
- **the roaming network operator this hub is**: a signing key (P-256) and an identity key (P-521), in
  `roamingNetworkOperator/` beside the configuration file - `operator.json`, the operator as its roaming networks
  will embed it, with the public halves of the keys, and `signing-key.pem` and `identity-key.pem`, the private keys;
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


## 2. Who the hub is

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

`GET /api/v1/poi/roamingNetworkOperator`, for everybody, answers with `operator.json`, public as it is in every
roaming network the hub operates:

```json
{
  "@id":          "RNO-67BDC1FC",
  "signingKeys":  [ { "@id": "882bbf1a…", "key": "pAECIAEh…" } ],
  "identityKeys": [ { "@id": "67bdc1fc…", "key": "pAECIAMh…" } ],
  "created":      "2026-10-17T18:16:07.776Z",
  "lastChange":   "2026-10-17T18:16:07.776Z"
}
```


## 3. Who may make a roaming network

Making one is `poi:edit`, an administrator's, and `root` is one. Wherever the hub is asked, it signs with its own keys,
so it asks who it is:

| Where | Signed in as `root` by |
|---|---|
| the prompt of the console the hub runs in | being at it: the console may do everything |
| the same prompt over SSH, port 22356 | its SSH key: `ssh -p 22356 root@127.0.0.1` |
| the web interface, http://127.0.0.1:2356/ | its password, from the first-start box |
| a program over HTTP, below `/api/v1` | an API key of it in the header `API-Key`, or its password by HTTP Basic |

A program gets an API key of its own, one that may change things (`readWrite`; a `readOnly` one may only read). At the
console the account is named, over SSH it is the one signed in:

```
RoamingHub> apiKeys root add readWrite Example programs
'root' has a new API key, ReadWrite:
PWfD544NqMPPINzb65YAEeMzxNZRkwzX7M59wZQ8
```

It is shown whole by `apiKeys root` again, and taken out by `apiKeys root remove PWfD544N`. The programs below read it
from the environment variable `ROAMINGHUB_API_KEY`.


## 4. Making `QA`

With nothing but its identification and its name: its operator is the hub, with the keys of its first start. The
identification is the name of its directory below `poi/` as well: letters, digits, `-` and `_`, at most 64 of them.

### At the prompt or over SSH

```
RoamingHub> roamingNetworks add QA Quality assurance
The roaming network 'QA' was made, at revision 0, commit 1d728939…, operated by this hub, 'RNO-67BDC1FC'. Its parties are added now, with change sets this hub signs.
Next: 'roamingNetworks QA add chargingStationOperator <id> signingKey <key> identityKey <key> [<name>]'.
http://127.0.0.1:2356/poi/RNs/QA
```

### In the web interface

**Points of interest**, below the list of the roaming networks, the card **New roaming network**: *Identification*
`QA`, *Name* `Quality assurance`, then **Make the roaming network**. It is there only for whoever may make one; made,
the page goes to `QA`.

### Over HTTP: curl

```bash
curl -X POST http://127.0.0.1:2356/api/v1/poi/RNs \
     -H "API-Key: $ROAMINGHUB_API_KEY" -H "Content-Type: application/json" \
     -d '{"@id": "QA", "name": {"en": "Quality assurance"}}'
```

`201 Created`, with the network as the POI API lists it and its genesis commit:

```json
{
  "message":        "The roaming network 'QA' was made, at revision 0, commit 1d728939…, operated by this hub, 'RNO-67BDC1FC'. Its parties are added now, with change sets this hub signs.",
  "roamingNetwork": { "@id": "QA", "name": { "en": "Quality assurance" }, "operator": { "@id": "RNO-67BDC1FC" } },
  "revision":       0,
  "commitId":       "1d728939…"
}
```

`400` where it is no roaming network the hub makes - one with keys, an operator or a party in it among them -, `409`
where there is one of that identification already, also in another case, `401` without a key or a password, `403`
for an account that may not.

### Over HTTP: C#

A console project of the administrator, e.g. in `examples/Admin/`, with WWCP_POI3, which brings Hermod along:

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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

await using var http  = new HTTPClient(URL.Parse(args.FirstOrDefault() ?? "http://127.0.0.1:2356"));
var apiKey            = APIKey_Id.Parse(Environment.GetEnvironmentVariable("ROAMINGHUB_API_KEY")!);

// The given JSON posted below /api/v1 with the API key of the administrator, and what the hub answered.
async Task<JObject> Post(String Path, JObject JSON)
{

    var response  = await http.POST(HTTPPath.Parse(Path),
                                    Content:         JSON.ToString().ToUTF8Bytes(),
                                    ContentType:     HTTPContentType.Application.JSON_UTF8,
                                    RequestBuilder:  request => request.API_Key = apiKey);

    var answer    = JObject.Parse(response.HTTPBodyAsUTF8String!);

    Console.WriteLine($"{response.HTTPStatusCode.Code}: {answer["message"] ?? answer}");
    return answer;

}

await Post("/api/v1/poi/RNs", new JObject(
                                  new JProperty("@id",   "QA"),
                                  new JProperty("name",  new JObject(new JProperty("en", "Quality assurance")))
                              ));
```

```
201: The roaming network 'QA' was made, at revision 0, commit 1d728939…, operated by this hub, 'RNO-67BDC1FC'. Its parties are added now, with change sets this hub signs.
```

[Example 2](example2.md#over-http-c) adds `DE*GEF` with the same `Post`.

### What `QA` is

The hub stores it in `poi/QA` (`CURRENT`, `gen-000001/base.json`, `gen-000001/log.jsonl`) and serves it below `/poi`,
readable by everyone, `GET /poi/RNs/QA`. The network embeds its operator with its keys; it has none of its own:

```json
{
  "@context":   "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork",
  "@id":        "QA",
  "name":       { "en": "Quality assurance" },
  "operator":   {
    "@id":          "RNO-67BDC1FC",
    "signingKeys":  [ { "@id": "882bbf1a…", "key": "pAECIAEh…" } ],
    "identityKeys": [ { "@id": "67bdc1fc…", "key": "pAECIAMh…" } ],
    "created":      "2026-10-17T18:16:07.776Z",
    "lastChange":   "2026-10-17T18:16:07.776Z"
  },
  "created":    "2026-10-17T18:16:11.065Z",
  "lastChange": "2026-10-17T18:16:11.065Z",
  "eTags": [ [ "json", "sha256", "base64", "…" ], [ "cbor", "sha256", "base64", "…" ] ]
}
```

Its genesis commit, `GET /poi/RNs/QA/history/commits/{id}`, is signed by the hub's signing key, in the role of the
roaming network operator - as every commit the hub makes of a roaming network it operates:

```json
{
  "@id":              "1d728939…",
  "@context":         "https://open.charging.cloud/contexts/wwcp+json/roamingNetworkCommit",
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

```
RoamingHub> roamingNetworks
QA  revision 0  Quality assurance  (operated by 'RNO-67BDC1FC', this hub, 0 charging station operator(s), 0 charging pool(s))
```


## 5. Without the hub: POI 3 alone

The same in C#, without a hub: the program is the roaming network operator itself, keeps its keys and the history of
`QA` in its directory, and may serve it over HTTP. The same project as above; [example 2](example2.md#5-without-the-hub-poi-3-alone)
and [example 3](example3.md#9-without-the-hub-poi-3-alone) go on with it, in the same directory. First the usings and
three helpers they share:

```csharp
using System.Security.Cryptography;

using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
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

// A private key in a PEM file, PKCS #8, as the hub keeps its own; and read back.
static void SaveKey(ECPrivateKeyParameters Key, String File)
    => System.IO.File.WriteAllText(File, new String(PemEncoding.Write("PRIVATE KEY", PrivateKeyInfoFactory.CreatePrivateKeyInfo(Key).GetEncoded())) + "\n");

static ECPrivateKeyParameters LoadKey(String File)
{
    var text = System.IO.File.ReadAllText(File);
    return (ECPrivateKeyParameters) PrivateKeyFactory.CreateKey(Convert.FromBase64String(text[PemEncoding.Find(text).Base64Data]));
}

// What signs every commit of QA: the signing key of its operator, in the role of the roaming network operator.
static RoamingNetworkCommitSigner OperatorSigner(ECPrivateKeyParameters SigningKey)
    => new (SigningKey, POIPublicKey.From(SigningKey).Id, COSEAlgorithm.ES256, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, "GraphDefined");
```

The roaming network operator makes its keys once, keeps them in `RNO/`, and starts the history of `QA`:

```csharp
// The keys of the roaming network operator, made once and kept in RNO/.
var rnoSigningKey   = NewKey(COSECurve.P256);
var rnoIdentityKey  = NewKey(COSECurve.P521);

Directory.CreateDirectory("RNO");
SaveKey(rnoSigningKey,  "RNO/signing-key.pem");
SaveKey(rnoIdentityKey, "RNO/identity-key.pem");

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

// One directory per roaming network. Start only once, on an empty one: on a used one it replaces what is stored.
using var store    = new RoamingNetworkHistoryFileStore("poi/QA");
var history        = RoamingNetworkHistory.Start(qa,
                                                 Signers:  [ OperatorSigner(rnoSigningKey) ],
                                                 Store:    store);

Console.WriteLine($"QA at revision {history.Head.Commit.Revision}, commit {history.Head.Commit.Id}");
```

```
QA at revision 0, commit 5a1f39d2…
```

The signer signs every commit as the hub does, here the genesis commit. From then on the history is loaded from its
directory, not started again, with the usings and helpers above; `TryLoad` checks every commit again, and the file
`LOCK` keeps a second store out of the directory - one program at a time:

```csharp
var rnoSigningKey  = LoadKey("RNO/signing-key.pem");

using var store    = new RoamingNetworkHistoryFileStore("poi/QA");

if (!RoamingNetworkHistory.TryLoad(store, out var history, out var error, Signers: [ OperatorSigner(rnoSigningKey) ]))
    throw new InvalidOperationException(error);
```

`POI_API` serves it over HTTP, readable by everyone, as the hub does below `/poi`; change sets are refused unless
`AuthorizeChangeSetSubmission` says who may submit them - for `QA` everybody, as their signatures decide:

```csharp
var httpServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Parse(8080), AutoStart: true);
var poiAPI      = new POI_API(httpServer) {
                      AuthorizeChangeSetSubmission = (request, changeSet) => true
                  };

poiAPI.AddRoamingNetworkHistory(history);
```


## 6. The next start

The hub reads its operator back from `roamingNetworkOperator/` and `QA` from `poi/QA`, checking every commit again -
signatures, ETags, merges - and signs the commits it makes of `QA` again. Without one of its private keys it does not
start: a new key would be none `QA` knows.


## Next

- [Example 2](example2.md): the charging station operator `DE*GEF` makes its keys and is added to `QA`.
- Changing the operator itself, e.g. its name or keys: `roamingNetworkOperator rotate signingKey` (or `identityKey`)
  replaces a key of the hub by a new one in every roaming network it operates, each change signed by the key it
  replaces, and `roamingNetworkOperator name GraphDefined GmbH` renames it; in C#,
  `RoamingNetworkChange.UpdateProperty(qa.Operator!, "name", …)`, see [RoamingNetworkOperator](../docs/RoamingNetworkOperator.md).
- Another server following `QA`: [Peer](../README.md#peer).
