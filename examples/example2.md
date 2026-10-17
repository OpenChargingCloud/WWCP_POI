# Example 2: two pools of a DC and an AC station, and their groups

From a fresh installation of the [RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) to the roaming
network `QA` with the charging station operator `DE*GEF`, GraphDefined GmbH: two charging pools, each with a DC
station and an AC station of two EVSEs each, then a charging station group of the DC stations and one of the AC
stations, and an EVSE group of the DC EVSEs and one of the AC EVSEs. Six commits, each signed by the one whose data it
changes:

| Revision | Signed by | Commit |
|---|---|---|
| 0 | the hub, as the roaming network operator (RNO) | Genesis: the roaming network `QA`, operated by the hub |
| 1 | the hub, as the RNO | `Add` of the charging station operator `DE*GEF`, with the public halves of its keys |
| 2 | `DE*GEF` | `Add` of the pool `DE*GEF*P1`, Jena Paradies: the DC station `DE*GEF*S1` with `DE*GEF*E1*1` and `DE*GEF*E1*2`, the AC station `DE*GEF*S2` with `DE*GEF*E2*1` and `DE*GEF*E2*2` |
| 3 | `DE*GEF` | `Add` of the pool `DE*GEF*P2`, Jena Nord: the DC station `DE*GEF*S3`, the AC station `DE*GEF*S4`, and their EVSEs |
| 4 | `DE*GEF` | `Add` of the charging station groups `DE*GEF*SG_DC` (`S1`, `S3`) and `DE*GEF*SG_AC` (`S2`, `S4`) |
| 5 | `DE*GEF` | `Add` of the EVSE groups `DE*GEF*EG_DC` (the EVSEs of `S1` and `S3`) and `DE*GEF*EG_AC` (those of `S2` and `S4`) |

An EVSE says itself what it is: its current type and its power, 150 kW DC or 22 kW AC. A group does not embed its
members, it names them, and it may give them properties: here each charging station group the power of its stations
together, 300 kW and 44 kW, and each EVSE group the connector of its EVSEs, a CCS cable or a type 2 socket. The
network keeps them in the groups; the stations and EVSEs take them at runtime, see [step 7](#7-as-they-are-at-runtime).
A group may also give a charging tariff to all its members, see [ChargingStationGroup](../docs/ChargingStationGroup.md)
and [EVSEGroup](../docs/EVSEGroup.md).

The code blocks of steps 2 to 6 are one `Program.cs`, the program of `DE*GEF`, in this order; the output is what one
run printed, `…` for elided keys, signatures and digests.


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
`COMMIT`, which `POI_API.COMMIT` names.

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

    var response  = await http.RunRequest(POI_API.COMMIT,
                                          HTTPPath.Root + "poi/RNs/QA/history/changeSets",
                                          Content:      changeSet.ToJSON().ToString().ToUTF8Bytes(),
                                          ContentType:  HTTPContentType.Application.JSON_UTF8);

    var answer    = JObject.Parse(response.HTTPBodyAsUTF8String!);

    Console.WriteLine(response.HTTPStatusCode.IsSuccessful
                          ? $"{response.HTTPStatusCode.Code} {answer["status"]}, revision {answer["revision"]}: {Description}"
                          : $"{response.HTTPStatusCode}: {answer}");

}
```


## 3. The first pool: a DC and an AC station

Both pools follow one pattern: pool `n` has the DC station `2n-1`, two EVSEs of 150 kW, and the AC station `2n`,
two EVSEs of 22 kW; the EVSEs of station `s` are `DE*GEF*E{s}*1` and `DE*GEF*E{s}*2`. Each EVSE says its current
type and its power, and nothing else: the connectors come with the EVSE groups of step 6.

```csharp
// Station s: two EVSEs, DE*GEF*E{s}*1 and DE*GEF*E{s}*2, of the given current and power.
static ChargingStation Station(Int32 S, CurrentTypes CurrentTypes, Int32 kW)

    => new (ChargingStation_Id.Parse($"DE*GEF*S{S}"),
            EVSEs: [
                new EVSE(EVSE_Id.Parse($"DE*GEF*E{S}*1"), CurrentTypes: CurrentTypes, MaxPower: Watt.FromKW(kW)),
                new EVSE(EVSE_Id.Parse($"DE*GEF*E{S}*2"), CurrentTypes: CurrentTypes, MaxPower: Watt.FromKW(kW))
            ]);

// Pool n: the DC station 2n-1, two EVSEs of 150 kW, and the AC station 2n, two of 22 kW.
static ChargingPool Pool(Int32 N, String Name, Double Latitude, Double Longitude)

    => new (ChargingPool_Id.Parse($"DE*GEF*P{N}"),
            Name:              I18NString.Create(Languages.en, Name),
            GeoLocation:       GeoCoordinate.Parse(Latitude, Longitude),
            ChargingStations:  [
                Station(2 * N - 1, CurrentTypes.DC,             150),
                Station(2 * N,     CurrentTypes.AC_ThreePhases,  22)
            ]);

await Commit("Jena Paradies, a DC and an AC station", RoamingNetworkChange.Add(Pool(1, "Jena Paradies", 50.9271, 11.5892)));
```

```
201 Committed, revision 2: Jena Paradies, a DC and an AC station
```

One `Add` of the pool, with its stations and EVSEs embedded: the parent of a pool is the operator its identification
names, and only that operator's signature lets it in.


## 4. The second pool, after the same pattern

```csharp
await Commit("Jena Nord, a DC and an AC station", RoamingNetworkChange.Add(Pool(2, "Jena Nord", 50.9447, 11.5867)));
```

```
201 Committed, revision 3: Jena Nord, a DC and an AC station
```


## 5. The charging station groups: DC and AC

A [charging station group](../docs/ChargingStationGroup.md) names its stations by their identifications; they must be
stations of its operator, in any of its pools. Its identification is `{operatorId}*SG{suffix}`, and it is embedded in
its operator, `chargingStationGroups`. Each group here gives its members their power together, `maxPower`: a station
takes it unless it has its own, and never above the one of all its EVSEs together.

```csharp
await Commit("The DC and the AC charging stations",
             RoamingNetworkChange.Add(new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG_DC"),
                                                               Name:                I18NString.Create(Languages.en, "DC charging stations"),
                                                               ChargingStationIds:  [ ChargingStation_Id.Parse("DE*GEF*S1"), ChargingStation_Id.Parse("DE*GEF*S3") ],
                                                               MaxPower:            Watt.FromKW(300))),
             RoamingNetworkChange.Add(new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG_AC"),
                                                               Name:                I18NString.Create(Languages.en, "AC charging stations"),
                                                               ChargingStationIds:  [ ChargingStation_Id.Parse("DE*GEF*S2"), ChargingStation_Id.Parse("DE*GEF*S4") ],
                                                               MaxPower:            Watt.FromKW(44))));
```

```
201 Committed, revision 4: The DC and the AC charging stations
```

Both in one change set, so in one commit. `GET /poi/RNs/QA/CSOs/DE*GEF/CSGs` lists them as the operator embeds them:

```json
[
  {
    "@id":                "DE*GEF*SG_AC",
    "chargingStationIds": [ "DE*GEF*S2", "DE*GEF*S4" ],
    "created":            "2026-10-17T18:11:37.487Z",
    "lastChange":         "2026-10-17T18:11:37.5Z",
    "maxPower":           44000,
    "name":               { "en": "AC charging stations" }
  },
  {
    "@id":                "DE*GEF*SG_DC",
    "chargingStationIds": [ "DE*GEF*S1", "DE*GEF*S3" ],
    "created":            "2026-10-17T18:11:37.408Z",
    "lastChange":         "2026-10-17T18:11:37.5Z",
    "maxPower":           300000,
    "name":               { "en": "DC charging stations" }
  }
]
```

and the groups of one station, `GET /poi/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/CSGs`, the DC group alone.
A station may be in no group or in several; where several give it different values, the one with the higher
`priority` decides, see [group priority](../docs/README.md#group-priority-and-effective-members).


## 6. The EVSE groups: DC and AC

An [EVSE group](../docs/EVSEGroup.md) names its EVSEs the same way, `{operatorId}*EG{suffix}`, embedded in its operator
as `EVSEGroups`. Each group here gives its members their connector: a CCS cable to the DC EVSEs, a type 2 socket to
the AC EVSEs. An EVSE takes the connectors of its groups besides its own; of the same identification, its own.

```csharp
await Commit("The DC and the AC EVSEs",
             RoamingNetworkChange.Add(new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EG_DC"),
                                                    Name:                I18NString.Create(Languages.en, "DC EVSEs"),
                                                    EVSEIds:             [ .. new[] { "DE*GEF*E1*1", "DE*GEF*E1*2", "DE*GEF*E3*1", "DE*GEF*E3*2" }.Select(EVSE_Id.Parse) ],
                                                    ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2_COMBO, Format: ChargingConnectorFormat.CABLE) ])),
             RoamingNetworkChange.Add(new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EG_AC"),
                                                    Name:                I18NString.Create(Languages.en, "AC EVSEs"),
                                                    EVSEIds:             [ .. new[] { "DE*GEF*E2*1", "DE*GEF*E2*2", "DE*GEF*E4*1", "DE*GEF*E4*2" }.Select(EVSE_Id.Parse) ],
                                                    ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2,       Format: ChargingConnectorFormat.SOCKET) ])));
```

```
201 Committed, revision 5: The DC and the AC EVSEs
```

`GET /poi/RNs/QA/CSOs/DE*GEF/EVSEGs`, the DC group of the two:

```json
{
  "@id":                "DE*GEF*EG_DC",
  "EVSEIds":            [ "DE*GEF*E1*1", "DE*GEF*E1*2", "DE*GEF*E3*1", "DE*GEF*E3*2" ],
  "chargingConnectors": [ { "@id": "1", "format": "CABLE", "type": "IEC_62196_T2_COMBO" } ],
  "created":            "2026-10-17T18:11:38.319Z",
  "lastChange":         "2026-10-17T18:11:38.439Z",
  "name":               { "en": "DC EVSEs" }
}
```

The groups of one EVSE: `GET /poi/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*1/EVSEGs`.


## 7. As they are at runtime

The POI API serves a pool, its stations and its EVSEs as they are at runtime, with what their groups give them:
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
          "created":            "2026-10-17T18:11:31.097Z",
          "currentTypes":       [ "DC" ],
          "lastChange":         "2026-10-17T18:11:38.439Z",
          "maxPower":           150000
        },
        …
      ],
      "created":    "2026-10-17T18:11:31.455Z",
      "lastChange": "2026-10-17T18:11:38.439Z",
      "maxPower":   300000
    },
    {
      "@id": "DE*GEF*S2",
      "EVSEs": [
        {
          "@id":                "DE*GEF*E2*1",
          "chargingConnectors": [ { "@id": "1", "format": "SOCKET", "type": "IEC_62196_T2" } ],
          "created":            "2026-10-17T18:11:31.751Z",
          "currentTypes":       [ "AC_ThreePhases" ],
          "lastChange":         "2026-10-17T18:11:38.439Z",
          "maxPower":           22000
        },
        …
      ],
      "created":    "2026-10-17T18:11:31.752Z",
      "lastChange": "2026-10-17T18:11:38.439Z",
      "maxPower":   44000
    }
  ],
  "created":     "2026-10-17T18:11:31.771Z",
  "geoLocation": { "lat": 50.9271, "lng": 11.5892 },
  "lastChange":  "2026-10-17T18:11:38.439Z",
  "name":        { "en": "Jena Paradies" }
}
```

The connectors and the station powers are the groups'; the `lastChange` of each is the latest of its own and of its
groups giving it something, here the commit of the EVSE groups. `?view=stored` serves the pool as its operator embeds
it - with the ETag a [precondition](../docs/README.md#etags) of a change set names - without them:

```json
{
  "@id": "DE*GEF*P1",
  "chargingStations": [
    {
      "@id": "DE*GEF*S1",
      "EVSEs": [
        { "@id": "DE*GEF*E1*1", "created": "2026-10-17T18:11:31.097Z", "currentTypes": [ "DC" ], "lastChange": "2026-10-17T18:11:31.097Z", "maxPower": 150000 },
        …
      ],
      "created":    "2026-10-17T18:11:31.455Z",
      "lastChange": "2026-10-17T18:11:31.455Z"
    },
    …
  ],
  …
}
```

The OCPI and DATEX II publishers publish the stations as they are at runtime as well, with their EVSEs. In C#, the operator
of a version gives them: `TryGetEffectiveEVSE(evseId, out var evse)`, `EffectiveChargingPools`, and
`ChargingStationGroupsOf(stationId)`, see [ChargingStationGroup](../docs/ChargingStationGroup.md#properties-of-its-members).

```
RoamingHub> roamingNetworks
QA  revision 5  Quality assurance  (operated by 'RNO-47EAB7CF', this hub, 1 charging station operator(s), 2 charging pool(s))
```


## Next

- A charging tariff for all members of a group: `chargingTariffId` of the group, a tariff of its operator, see
  [ChargingTariff](../docs/ChargingTariff.md); an EVSE takes the one of its EVSE groups, else of the groups of its
  station, else of the groups of its pool.
- A station added later joins its group by an [element change](../docs/README.md#element-changes) of
  `chargingStationIds`, without the whole array; an EVSE the same way, `EVSEIds`.
- The status of a group, calculated from the status of its members: [Calculated status of
  groups](../docs/README.md#calculated-status-of-groups).
