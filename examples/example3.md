# Example 3: two pools of a DC and an AC station, their templates and their groups

The charging station operator `DE*GEF` of [example 2](example2.md) adds its data to `QA`, with change sets it signs
with the signing key it keeps: the templates of a DC and an AC station and of their EVSEs, two charging pools, each
with a DC station and an AC station of two EVSEs each naming their templates, then a charging station group and an
EVSE group for each current. Five commits:

| Revision | Signed by | Commit |
|---|---|---|
| 0, 1 | the hub, as the roaming network operator | `QA` and `DE*GEF`, see [example 1](example1.md) and [example 2](example2.md) |
| 2 | `DE*GEF` | `Add` of the charging station templates `DE*GEF*ST_DC` and `DE*GEF*ST_AC`, and of the EVSE templates `DE*GEF*ET_DC` and `DE*GEF*ET_AC` |
| 3 | `DE*GEF` | `Add` of the pool `DE*GEF*P1`, Jena Paradies: the DC station `DE*GEF*S1` with `DE*GEF*E1*1` and `DE*GEF*E1*2`, the AC station `DE*GEF*S2` with `DE*GEF*E2*1` and `DE*GEF*E2*2`, each naming its template |
| 4 | `DE*GEF` | `Add` of the pool `DE*GEF*P2`, Jena Nord: the DC station `DE*GEF*S3`, the AC station `DE*GEF*S4`, and their EVSEs |
| 5 | `DE*GEF` | `Add` of the charging station groups `DE*GEF*SG_DC`, the DC stations `S1` and `S3`, and `DE*GEF*SG_AC`, the AC stations `S2` and `S4` |
| 6 | `DE*GEF` | `Add` of the EVSE groups `DE*GEF*EG_DC`, the EVSEs of `S1` and `S3`, and `DE*GEF*EG_AC`, those of `S2` and `S4` |

An EVSE says itself what it is: its current type and its power, 150 kW DC or 22 kW AC. What a series of stations or
EVSEs has in common comes from a template it names in its `templateIds`: here each charging station template the power
of its stations together, 300 kW and 44 kW, and each EVSE template the connector of its EVSEs, a CCS cable or a type 2
socket. The network keeps them in the templates; the stations and EVSEs take them at runtime, see
[step 7](#7-as-they-are-at-runtime). A template may also give a charging tariff, see
[ChargingStationTemplate](../docs/ChargingStationTemplate.md) and [EVSETemplate](../docs/EVSETemplate.md). A group
gives nothing: it collects members, here the stations and the EVSEs of one current, e.g. for an offer, see
[ChargingStationGroup](../docs/ChargingStationGroup.md) and [EVSEGroup](../docs/EVSEGroup.md).

**Only `DE*GEF` can do this.** Its data is its own: a change of it must be signed by a signing key of `DE*GEF`, and
the hub has none - so neither the prompt nor the web interface adds a pool, they show it (see
[step 7](#7-as-they-are-at-runtime)). The operator signs a change set in its own program and submits it to the
history of `QA`, by anybody, without signing in: whether it may change what it changes, its signatures decide. In C#
over HTTP below, with curl, or without the hub, with POI 3 alone.

The code blocks of steps 1 to 6 are one `Program.cs`, the program of `DE*GEF`, in this order, in the directory where
[example 2](example2.md#1-degef-makes-its-keys) left `DE-GEF/`; the output is what a run prints, `…` for elided keys,
signatures, digests and timestamps.


## 1. The program of `DE*GEF`

The project of example 2, with `LoadKey` of [example 1](example1.md#5-without-the-hub-poi-3-alone) to read its key back. Two
helpers do the rest for every step: `Head()` reads the head of `QA` as the hub serves it, checked against its ETags as
it is read, and `Commit(…)` makes a change set of it, signs it with the signing key of `DE*GEF` and submits it. Both
talk to the hub with the `HTTPClient` of Hermod; a change set goes by the method `COMMIT`, `http.COMMIT(…)`.

```csharp
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;

// A private key read from its PEM file, PKCS #8.
static ECPrivateKeyParameters LoadKey(String File)
{
    var text = System.IO.File.ReadAllText(File);
    return (ECPrivateKeyParameters) PrivateKeyFactory.CreateKey(Convert.FromBase64String(text[PemEncoding.Find(text).Base64Data]));
}

// The signing key DE*GEF made in example 2.
var gefSigningKey     = LoadKey("DE-GEF/signing-key.pem");

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

`CreateChangeSet` makes the change set for the version it is called on: its `beforeETags` are those of the head, its
`afterETags` those of the result. Made for an older version, it is merged, or refused where it changes what changed
meanwhile.


## 2. The templates: DC and AC

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


## 3. The first pool: a DC and an AC station

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


## 4. The second pool, after the same pattern

```csharp
await Commit("Jena Nord, a DC and an AC station", RoamingNetworkChange.Add(Pool(2, "Jena Nord", 50.9447, 11.5867)));
```

```
201 Committed, revision 4: Jena Nord, a DC and an AC station
```


## 5. The groups of the stations: DC and AC

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


## 6. The groups of the EVSEs: DC and AC

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


## 7. As they are at runtime

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

The web interface shows them under **Points of interest → QA → Charging station operators → DE\*GEF**: its pools,
stations and EVSEs as they are at runtime, with the template each value comes from, its templates and its groups.
Over HTTP, for everybody:

| Request | Returns |
|---|---|
| `GET /poi/RNs/QA/CPs?operatorId=DE*GEF` | the pools of `DE*GEF` |
| `GET /poi/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1` | the pool at runtime, its keys in canonical order; `?view=stored` as it is stored |
| `GET /poi/RNs/QA/EVSEs/DE*GEF*E1*2` | `307` to `/poi/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*2` |
| `GET /poi/RNs/QA/CSOs/DE*GEF/{CSTs \| EVSETs \| CSGs \| EVSEGs}` | its templates and its groups |
| `GET /poi/RNs/QA/history/head` | the head commit: `revision` 6, its `parentIds`, `eTags` and `signatureKeyIds`, the one of `DE*GEF` |


## 8. What the hub answers

`201 Created` answers a change set with its commit and the new head, shortened:

```json
{
  "status":          "Committed",
  "@id":             "2d2a60b9…",
  "revision":        3,
  "parentIds":       [ "adcb769c…" ],
  "changeSetId":     "MSQ5AtSUtAG8542fS12Yh6rCW7x8xv2G",
  "createdAt":       "2026-10-17T18:16:17.709Z",
  "description":     { "en": "Jena Paradies, a DC and an AC station" },
  "numberOfChanges": 1,
  "signatureKeyIds": [ "a3fec322…" ],
  "eTags":           [ [ "json", "sha256", "hex", "faf55b2e…" ], [ "cbor", "sha256", "hex", "23340139…" ] ],
  "head":            { "@id": "2d2a60b9…", "revision": 3, … }
}
```

The pools are the operator's alone: the same change set signed by the hub is `422` with the reason,
`Change 0: Add of the ChargingPool 'DE*GEF*P1' needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'!`,
and by a key of no party `422` with `Signature 0: the key '…' is no key of a party of the roaming network 'QA'!`;
nothing changes.

With curl, a change set the program wrote to a file, `File.WriteAllText("pool.json", changeSet.ToJSON().ToString())`:

```bash
curl -X COMMIT -H "Content-Type: application/json" --data-binary @pool.json http://127.0.0.1:2356/poi/RNs/QA/history/changeSets
```


## 9. Without the hub: POI 3 alone

The program of [example 1, step 5](example1.md#5-without-the-hub-poi-3-alone), in the same directory, its usings and
helpers, and `QA` loaded with `TryLoad`; [example 2](example2.md#5-without-the-hub-poi-3-alone) added `DE*GEF` to it.
Only `Commit` is another: it commits to the history itself, and steps 2 to 6 then run as they are.

```csharp
var gefSigningKey  = LoadKey("DE-GEF/signing-key.pem");

// The given changes, as a change set of the head signed by DE*GEF, committed to the history.
Task Commit(String Description, params RoamingNetworkChange[] Changes)
{

    var result = history.Commit(history.Head.RoamingNetwork.
                                    CreateChangeSet(Changes, Description: new ImmutableI18NString(I18NString.Create(Languages.en, Description))).
                                    Sign(gefSigningKey, POIPublicKey.From(gefSigningKey).Id, COSEAlgorithm.ES256));

    Console.WriteLine($"{result.Status}, revision {result.Commit?.Revision}: {Description}");
    return Task.CompletedTask;

}
```

```
Committed, revision 2: The templates of the DC and the AC stations and EVSEs
Committed, revision 3: Jena Paradies, a DC and an AC station
Committed, revision 4: Jena Nord, a DC and an AC station
Committed, revision 5: The DC and the AC charging stations
Committed, revision 6: The DC and the AC EVSEs
```

The operator of a version gives the pools as they are at runtime: `history.Head.RoamingNetwork.
GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!.EffectiveChargingPools`.


## Next

- A charging tariff for all members of a template: `chargingTariffId` of the template, a tariff of its operator, see
  [ChargingTariff](../docs/ChargingTariff.md); an EVSE takes the one of the first of its EVSE templates naming one, else
  of the templates of its station, else of the templates of its pool.
- Stations and EVSEs added one by one, `RoamingNetworkChange.Add(station, poolId)` and `Add(evse, stationId)`, e.g. a
  third EVSE later.
- Changing a property: `RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 22000)`, `CreateChangeSet` fills
  in the old value it finds; see [Change sets](../README.md#change-sets).
- A station added later takes its template by naming it, `templateIds`; an [element change](../docs/README.md#element-changes)
  adds a template at the end of its list.
- An offer at the members of a group, e.g. 20 % less at night at the DC stations: a
  [charging tariff offer](../docs/ChargingTariffOffer.md) naming `DE*GEF*SG_DC` in its `chargingStationGroupIds`, or
  at the AC EVSEs `DE*GEF*EG_AC` in its `EVSEGroupIds`.
- The status of a group, calculated from the status of its members: [Calculated status of
  groups](../docs/README.md#calculated-status-of-groups).
