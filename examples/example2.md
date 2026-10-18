# Example 2: two pools of a DC and an AC station, their templates and their groups

From a fresh installation of the [RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) to the roaming
network `QA` with the charging station operator `DE*GEF`, GraphDefined GmbH: the templates of a DC and an AC station and
of their EVSEs, two charging pools, each with a DC station and an AC station of two EVSEs each naming their templates,
then a charging station group and an EVSE group for each current. Seven commits, each signed by the one whose data it
changes:

| Revision | Signed by | Commit |
|---|---|---|
| 0 | the hub, as the roaming network operator (RNO) | Genesis: the roaming network `QA`, operated by the hub |
| 1 | the hub, as the RNO | `Add` of the charging station operator `DE*GEF`, with the public halves of its keys |
| 2 | `DE*GEF` | `Add` of the charging station templates `DE*GEF*ST_DC` and `DE*GEF*ST_AC`, and of the EVSE templates `DE*GEF*ET_DC` and `DE*GEF*ET_AC` |
| 3 | `DE*GEF` | `Add` of the pool `DE*GEF*P1`, Jena Paradies: the DC station `DE*GEF*S1` with `DE*GEF*E1*1` and `DE*GEF*E1*2`, the AC station `DE*GEF*S2` with `DE*GEF*E2*1` and `DE*GEF*E2*2`, each naming its template |
| 4 | `DE*GEF` | `Add` of the pool `DE*GEF*P2`, Jena Nord: the DC station `DE*GEF*S3`, the AC station `DE*GEF*S4`, and their EVSEs |
| 5 | `DE*GEF` | `Add` of the charging station groups `DE*GEF*SG_DC`, the DC stations `S1` and `S3`, and `DE*GEF*SG_AC`, the AC stations `S2` and `S4` |
| 6 | `DE*GEF` | `Add` of the EVSE groups `DE*GEF*EG_DC`, the EVSEs of `S1` and `S3`, and `DE*GEF*EG_AC`, those of `S2` and `S4` |

An EVSE says itself what it is: its current type and its power, 150 kW DC or 22 kW AC. What a series of stations or
EVSEs has in common comes from a template it names in its `templateIds`: here each charging station template the power
of its stations together, 300 kW and 44 kW, and each EVSE template the connector of its EVSEs, a CCS cable or a type 2
socket. The network keeps them in the templates; the stations and EVSEs take them at runtime, see
[step 8](#8-as-they-are-at-runtime). A template may also give a charging tariff, see
[ChargingStationTemplate](../docs/ChargingStationTemplate.md) and [EVSETemplate](../docs/EVSETemplate.md). A group
gives nothing: it collects members, here the stations and the EVSEs of one current, e.g. for an offer, see
[ChargingStationGroup](../docs/ChargingStationGroup.md) and [EVSEGroup](../docs/EVSEGroup.md).

The code blocks of steps 2 to 7 are one `Program.cs`, the program of `DE*GEF`, in this order; the output is what a run
prints, `…` for elided keys, signatures, digests and timestamps.


## 1. A fresh hub and the roaming network `QA`

As in [example 1](example1.md#1-the-first-start-what-is-there): the first start makes the roaming network operator
the hub is, with its keys, and no roaming network. An administrator makes `QA`, operated by the hub:

```bash
dotnet run --project RoamingHubCLI
```

```
RoamingHub> roamingNetworks add QA Quality assurance
The roaming network 'QA' was made, at revision 0, commit af14c3e4…, operated by this hub, 'RNO-47EAB7CF'. Its parties are added now, with change sets this hub signs.
```


## 2. The charging station operator `DE*GEF`

`DE*GEF` makes its own keys and keeps them; the hub gets only their public halves. A console project with WWCP_POI3,
e.g. in `examples/Example2/`:

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
signingKey  pAECIAEhWCD7CLxUDqgaZUDK8iI7oeUJ6dP2XS0h6dg9jNbbjgL1jiJY…
identityKey pAECIAMhWEIAjaUvAPAsbpk/TBPqjdqbr4qj9QMngDNQHCsu9/v5XhrL…
Add DE*GEF with these keys at the hub, then press Enter.
```

An administrator adds it, and the hub signs it as the RNO:

```
RoamingHub> roamingNetworks QA add chargingStationOperator DE*GEF signingKey pAECIAEhWCD7CLxU… identityKey pAECIAMhWEIAjaUv… GraphDefined
The ChargingStationOperator 'DE*GEF' was added to the roaming network 'QA', at revision 1, commit da9448f6…. It adds its data now, with change sets it signs.
RoamingHub> roamingNetworks QA
QA  revision 1, commit da9448f6…  Quality assurance
  operated by 'RNO-47EAB7CF', this hub
  RoamingNetworkOperator RNO-47EAB7CF  signing key(s) fcca44cda0232d72…, identity key(s) 47eab7cfa2442968…
  ChargingStationOperator DE*GEF  signing key(s) 0ddfffa0718caf64…, identity key(s) 1e5c445c965a8397…
```

Everything after Enter is the operator's own, in change sets it signs, made for the head of `QA` and submitted to the
hub - by anybody, without signing in. Two helpers do that for every step: `Head()` reads the head as the hub serves it,
checked against its ETags as it is read, and `Commit(…)` makes a change set of it, signs it with the signing key of
`DE*GEF` and submits it. Both talk to the hub with the `HTTPClient` of Hermod; a change set goes by the method
`COMMIT`, `http.COMMIT(…)`.

```csharp
await using var http  = new HTTPClient(URL.Parse(args.FirstOrDefault() ?? "http://127.0.0.1:2356"));

// The head of QA as the hub serves it, checked against its ETags as it is read.
async Task<RoamingNetwork> Head()

    => RoamingNetwork.TryParse(JObject.Parse((await http.GET(HTTPPath.Root + "poi/RNs/QA")).HTTPBodyAsUTF8String!), out var head, out var error)
           ? head
           : throw new InvalidOperationException(error);

// The given changes, as a change set of the head signed by DE*GEF, committed to the hub.
async Task Commit(String Description, params RoamingNetworkChange[] Changes)
{

    var changeSet = (await Head()).CreateChangeSet(Changes, Description: new ImmutableI18NString(I18NString.Create(Languages.en, Description))).
                                   Sign(gefSigningKey, POIPublicKey.From(gefSigningKey).Id, COSEAlgorithm.ES256);

    var response  = await http.COMMIT(HTTPPath.Root + "poi/RNs/QA/history/changeSets",
                                      Content:      changeSet.ToJSON().ToString().ToUTF8Bytes(),
                                      ContentType:  HTTPContentType.Application.JSON_UTF8);

    var answer    = JObject.Parse(response.HTTPBodyAsUTF8String!);

    Console.WriteLine(response.HTTPStatusCode.IsSuccessful
                          ? $"{response.HTTPStatusCode.Code} {answer["status"]}, revision {answer["revision"]}: {Description}"
                          : $"{response.HTTPStatusCode}: {answer}");

}
```


## 3. The templates: DC and AC

A [charging station template](../docs/ChargingStationTemplate.md) has the properties of a charging station, an
[EVSE template](../docs/EVSETemplate.md) those of an EVSE; their identifications are `{operatorId}*ST{suffix}` and
`{operatorId}*ET{suffix}`, and they are embedded in their operator, `chargingStationTemplates` and `EVSETemplates`. A
template names no members: a station or an EVSE names the templates it takes, so they come first. Each charging station
template here gives its stations their power together, `maxPower`: a station takes it unless it has its own, and never
above the one of all its EVSEs together. Each EVSE template gives its EVSEs their connector: a CCS cable to the DC
EVSEs, a type 2 socket to the AC EVSEs; an EVSE takes the connectors of its templates besides its own, of the same
identification its own.

```csharp
await Commit("The templates of the DC and the AC stations and EVSEs",
             RoamingNetworkChange.Add(new ChargingStationTemplate(ChargingStationTemplate_Id.Parse("DE*GEF*ST_DC"),
                                                                  Name:                I18NString.Create(Languages.en, "DC charging station"),
                                                                  MaxPower:            Watt.FromKW(300))),
             RoamingNetworkChange.Add(new ChargingStationTemplate(ChargingStationTemplate_Id.Parse("DE*GEF*ST_AC"),
                                                                  Name:                I18NString.Create(Languages.en, "AC charging station"),
                                                                  MaxPower:            Watt.FromKW(44))),
             RoamingNetworkChange.Add(new EVSETemplate(EVSETemplate_Id.Parse("DE*GEF*ET_DC"),
                                                       Name:                I18NString.Create(Languages.en, "DC EVSE"),
                                                       ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2_COMBO, Format: ChargingConnectorFormat.CABLE) ])),
             RoamingNetworkChange.Add(new EVSETemplate(EVSETemplate_Id.Parse("DE*GEF*ET_AC"),
                                                       Name:                I18NString.Create(Languages.en, "AC EVSE"),
                                                       ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2,       Format: ChargingConnectorFormat.SOCKET) ])));
```

```
201 Committed, revision 2: The templates of the DC and the AC stations and EVSEs
```

All four in one change set, so in one commit. `GET /poi/RNs/QA/CSOs/DE*GEF/CSTs` lists the charging station templates
as the operator embeds them:

```json
[
  {
    "@id":        "DE*GEF*ST_AC",
    "created":    "…",
    "lastChange": "…",
    "maxPower":   44000,
    "name":       { "en": "AC charging station" }
  },
  {
    "@id":        "DE*GEF*ST_DC",
    "created":    "…",
    "lastChange": "…",
    "maxPower":   300000,
    "name":       { "en": "DC charging station" }
  }
]
```

and `GET /poi/RNs/QA/CSOs/DE*GEF/EVSETs` the EVSE templates, the DC one of the two:

```json
{
  "@id":                "DE*GEF*ET_DC",
  "chargingConnectors": [ { "@id": "1", "format": "CABLE", "type": "IEC_62196_T2_COMBO" } ],
  "created":            "…",
  "lastChange":         "…",
  "name":               { "en": "DC EVSE" }
}
```


## 4. The first pool: a DC and an AC station

Both pools follow one pattern: pool `n` has the DC station `2n-1`, two EVSEs of 150 kW, and the AC station `2n`,
two EVSEs of 22 kW; the EVSEs of station `s` are `DE*GEF*E{s}*1` and `DE*GEF*E{s}*2`. Each EVSE says its current
type and its power, and names its EVSE template; each station names its charging station template.

```csharp
// Station s: two EVSEs, DE*GEF*E{s}*1 and DE*GEF*E{s}*2, of the given current and power, and the templates of its kind.
static ChargingStation Station(Int32 S, CurrentTypes CurrentTypes, Int32 kW, String Kind)

    => new (ChargingStation_Id.Parse($"DE*GEF*S{S}"),
            TemplateIds:  [ ChargingStationTemplate_Id.Parse($"DE*GEF*ST_{Kind}") ],
            EVSEs: [
                new EVSE(EVSE_Id.Parse($"DE*GEF*E{S}*1"), CurrentTypes: CurrentTypes, MaxPower: Watt.FromKW(kW), TemplateIds: [ EVSETemplate_Id.Parse($"DE*GEF*ET_{Kind}") ]),
                new EVSE(EVSE_Id.Parse($"DE*GEF*E{S}*2"), CurrentTypes: CurrentTypes, MaxPower: Watt.FromKW(kW), TemplateIds: [ EVSETemplate_Id.Parse($"DE*GEF*ET_{Kind}") ])
            ]);

// Pool n: the DC station 2n-1, two EVSEs of 150 kW, and the AC station 2n, two of 22 kW.
static ChargingPool Pool(Int32 N, String Name, Double Latitude, Double Longitude)

    => new (ChargingPool_Id.Parse($"DE*GEF*P{N}"),
            Name:              I18NString.Create(Languages.en, Name),
            GeoLocation:       GeoCoordinate.Parse(Latitude, Longitude),
            ChargingStations:  [
                Station(2 * N - 1, CurrentTypes.DC,             150, "DC"),
                Station(2 * N,     CurrentTypes.AC_ThreePhases,  22, "AC")
            ]);

await Commit("Jena Paradies, a DC and an AC station", RoamingNetworkChange.Add(Pool(1, "Jena Paradies", 50.9271, 11.5892)));
```

```
201 Committed, revision 3: Jena Paradies, a DC and an AC station
```

One `Add` of the pool, with its stations and EVSEs embedded: the parent of a pool is the operator its identification
names, and only that operator's signature lets it in. A template its members name must be one of that operator.


## 5. The second pool, after the same pattern

```csharp
await Commit("Jena Nord, a DC and an AC station", RoamingNetworkChange.Add(Pool(2, "Jena Nord", 50.9447, 11.5867)));
```

```
201 Committed, revision 4: Jena Nord, a DC and an AC station
```


## 6. The groups of the stations: DC and AC

A [charging station group](../docs/ChargingStationGroup.md) names its stations by their identifications, of its
operator, in any of its pools: `{operatorId}*SG{suffix}`, embedded in its operator as `chargingStationGroups`. It gives
them nothing: here it collects the stations of one current across both pools, e.g. to name them in an offer, and has a
status of its own.

```csharp
await Commit("The DC and the AC charging stations",
             RoamingNetworkChange.Add(new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG_DC"),
                                                               Name:                I18NString.Create(Languages.en, "DC charging stations"),
                                                               ChargingStationIds:  [ ChargingStation_Id.Parse("DE*GEF*S1"), ChargingStation_Id.Parse("DE*GEF*S3") ])),
             RoamingNetworkChange.Add(new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG_AC"),
                                                               Name:                I18NString.Create(Languages.en, "AC charging stations"),
                                                               ChargingStationIds:  [ ChargingStation_Id.Parse("DE*GEF*S2"), ChargingStation_Id.Parse("DE*GEF*S4") ])));
```

```
201 Committed, revision 5: The DC and the AC charging stations
```

`GET /poi/RNs/QA/CSOs/DE*GEF/CSGs` lists both, as the operator embeds them:

```json
[
  {
    "@id":                "DE*GEF*SG_AC",
    "chargingStationIds": [ "DE*GEF*S2", "DE*GEF*S4" ],
    "created":            "…",
    "lastChange":         "…",
    "name":               { "en": "AC charging stations" }
  },
  {
    "@id":                "DE*GEF*SG_DC",
    "chargingStationIds": [ "DE*GEF*S1", "DE*GEF*S3" ],
    "created":            "…",
    "lastChange":         "…",
    "name":               { "en": "DC charging stations" }
  }
]
```


## 7. The groups of the EVSEs: DC and AC

The same for the EVSEs: an [EVSE group](../docs/EVSEGroup.md) names its EVSEs, `{operatorId}*EG{suffix}`, embedded in
its operator as `EVSEGroups`. The DC group has the four EVSEs of `S1` and `S3`, the AC group those of `S2` and `S4`.

```csharp
// The EVSEs of the given stations: DE*GEF*E{s}*1 and DE*GEF*E{s}*2 of each station s.
static IEnumerable<EVSE_Id> EVSEsOf(params Int32[] Stations)

    => Stations.SelectMany(s => new[] { EVSE_Id.Parse($"DE*GEF*E{s}*1"), EVSE_Id.Parse($"DE*GEF*E{s}*2") });

await Commit("The DC and the AC EVSEs",
             RoamingNetworkChange.Add(new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EG_DC"),
                                                    Name:     I18NString.Create(Languages.en, "DC EVSEs"),
                                                    EVSEIds:  EVSEsOf(1, 3))),
             RoamingNetworkChange.Add(new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EG_AC"),
                                                    Name:     I18NString.Create(Languages.en, "AC EVSEs"),
                                                    EVSEIds:  EVSEsOf(2, 4))));
```

```
201 Committed, revision 6: The DC and the AC EVSEs
```

`GET /poi/RNs/QA/CSOs/DE*GEF/EVSEGs/DE*GEF*EG_DC`:

```json
{
  "@id":        "DE*GEF*EG_DC",
  "EVSEIds":    [ "DE*GEF*E1*1", "DE*GEF*E1*2", "DE*GEF*E3*1", "DE*GEF*E3*2" ],
  "created":    "…",
  "lastChange": "…",
  "name":       { "en": "DC EVSEs" }
}
```

Each member also lists its templates and its groups below itself. The templates of one station, in its order,
`GET /poi/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/CSTs`: the DC template alone; its groups, `…/CSGs`: the DC
station group; the groups of one of its EVSEs, `…/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*1/EVSEGs`: the DC EVSE group. A station
may name several templates: of two giving a value, the one it names first gives it, see
[templates](../docs/README.md#templates-and-effective-members).


## 8. As they are at runtime

The POI API serves a pool, its stations and its EVSEs as they are at runtime, with what their templates give them:
`GET /poi/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1`, shortened to the first EVSE of each station:

```json
{
  "@id": "DE*GEF*P1",
  "chargingStations": [
    {
      "@id": "DE*GEF*S1",
      "EVSEs": [
        {
          "@id":                "DE*GEF*E1*1",
          "chargingConnectors": [ { "@id": "1", "format": "CABLE", "type": "IEC_62196_T2_COMBO" } ],
          "created":            "…",
          "currentTypes":       [ "DC" ],
          "lastChange":         "…",
          "maxPower":           150000,
          "templateIds":        [ "DE*GEF*ET_DC" ]
        },
        …
      ],
      "created":     "…",
      "lastChange":  "…",
      "maxPower":    300000,
      "templateIds": [ "DE*GEF*ST_DC" ]
    },
    {
      "@id": "DE*GEF*S2",
      "EVSEs": [
        {
          "@id":                "DE*GEF*E2*1",
          "chargingConnectors": [ { "@id": "1", "format": "SOCKET", "type": "IEC_62196_T2" } ],
          "created":            "…",
          "currentTypes":       [ "AC_ThreePhases" ],
          "lastChange":         "…",
          "maxPower":           22000,
          "templateIds":        [ "DE*GEF*ET_AC" ]
        },
        …
      ],
      "created":     "…",
      "lastChange":  "…",
      "maxPower":    44000,
      "templateIds": [ "DE*GEF*ST_AC" ]
    }
  ],
  "created":     "…",
  "geoLocation": { "lat": 50.9271, "lng": 11.5892 },
  "lastChange":  "…",
  "name":        { "en": "Jena Paradies" }
}
```

The connectors and the station powers are the templates'; the `lastChange` of each is the latest of its own and of its
templates, here its own, as the templates are older. `?view=stored` serves the pool as its operator embeds it - with the
ETag a [precondition](../docs/README.md#etags) of a change set names - naming its templates, without what they give:

```json
{
  "@id": "DE*GEF*P1",
  "chargingStations": [
    {
      "@id": "DE*GEF*S1",
      "EVSEs": [
        { "@id": "DE*GEF*E1*1", "created": "…", "currentTypes": [ "DC" ], "lastChange": "…", "maxPower": 150000, "templateIds": [ "DE*GEF*ET_DC" ] },
        …
      ],
      "created":     "…",
      "lastChange":  "…",
      "templateIds": [ "DE*GEF*ST_DC" ]
    },
    …
  ],
  …
}
```

The OCPI and DATEX II publishers publish the stations as they are at runtime as well, with their EVSEs. In C#, the operator
of a version gives them: `TryGetEffectiveEVSE(evseId, out var evse)`, `EffectiveChargingPools`, and
`TemplatesOf(station)`, see [ChargingStationTemplate](../docs/ChargingStationTemplate.md).

```
RoamingHub> roamingNetworks
QA  revision 6  Quality assurance  (operated by 'RNO-47EAB7CF', this hub, 1 charging station operator(s), 2 charging pool(s))
```


## Next

- A charging tariff for all members of a template: `chargingTariffId` of the template, a tariff of its operator, see
  [ChargingTariff](../docs/ChargingTariff.md); an EVSE takes the one of the first of its EVSE templates naming one, else
  of the templates of its station, else of the templates of its pool.
- A station added later takes its template by naming it, `templateIds`; an [element change](../docs/README.md#element-changes)
  adds a template at the end of its list.
- An offer at the members of a group, e.g. 20 % less at night at the DC stations: a
  [charging tariff offer](../docs/ChargingTariffOffer.md) naming `DE*GEF*SG_DC` in its `chargingStationGroupIds`, or
  at the AC EVSEs `DE*GEF*EG_AC` in its `EVSEGroupIds`.
- The status of a group, calculated from the status of its members: [Calculated status of
  groups](../docs/README.md#calculated-status-of-groups).
