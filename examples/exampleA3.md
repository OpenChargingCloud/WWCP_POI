# Example A3: two pools of a DC and an AC station, their templates and their groups

The charging station operator `DE*GEF` of [example A2](exampleA2.md) adds its data to `QA`, with change sets it signs
with its `infrastructure` signing key: the templates of a DC and an AC station, of their EVSEs and of the energy meters
of these, two charging pools, each with a DC station and an AC station of two EVSEs each naming their templates, every
EVSE with its energy meter, then a charging station group and an EVSE group for each current. Five commits:

| Revision | Signed by | Commit |
|---|---|---|
| 0 to 2 | `OCC`, as the roaming network operator; the manufacturers | `QA`, `DE*GEF`, the manufacturers and their models, see [example A1](exampleA1.md) and [example A2](exampleA2.md) |
| 3 | `DE*GEF`, its `infrastructure` key | `Add` of the charging station templates `DE*GEF*ST_DC` and `DE*GEF*ST_AC`, of the EVSE templates `DE*GEF*ET_DC` and `DE*GEF*ET_AC`, and of the energy meter templates `DE*GEF*EMT_DC` and `DE*GEF*EMT_AC` |
| 4 | `DE*GEF` | `Add` of the pool `DE*GEF*P1`, Jena Paradies: the DC station `DE*GEF*S1` with `DE*GEF*E1*1` and `DE*GEF*E1*2`, the AC station `DE*GEF*S2` with `DE*GEF*E2*1` and `DE*GEF*E2*2`, each naming its template, each EVSE with its energy meter, `DE*GEF*M1*1` to `DE*GEF*M2*2` |
| 5 | `DE*GEF` | `Add` of the pool `DE*GEF*P2`, Jena Nord: the DC station `DE*GEF*S3`, the AC station `DE*GEF*S4`, and their EVSEs |
| 6 | `DE*GEF` | `Add` of the charging station groups `DE*GEF*SG_DC`, the DC stations `S1` and `S3`, and `DE*GEF*SG_AC`, the AC stations `S2` and `S4` |
| 7 | `DE*GEF` | `Add` of the EVSE groups `DE*GEF*EG_DC`, the EVSEs of `S1` and `S3`, and `DE*GEF*EG_AC`, those of `S2` and `S4` |

An EVSE says itself what it is: its current type and its power, 150 kW DC or 22 kW AC. What a series of stations,
EVSEs or energy meters has in common comes from a template it names in its `templateIds`: here each charging station
template the power of its stations together, 300 kW and 44 kW, and their manufacturer and model, each EVSE template the
connector of its EVSEs, a CCS cable or a type 2 socket, and each energy meter template the manufacturer and model of its
meters and how they sign their measurements. What belongs to the single device, the serial number of a meter and the
public key it signs its measurements with, is its own. The network keeps them in the templates; the stations and EVSEs take them at runtime, see
[step 7](#7-as-they-are-at-runtime). A template may also give a charging tariff, see
[ChargingStationTemplate](../docs/ChargingStationTemplate.md) and [EVSETemplate](../docs/EVSETemplate.md). A group
gives nothing: it collects members, here the stations and the EVSEs of one current, e.g. for an offer, see
[ChargingStationGroup](../docs/ChargingStationGroup.md) and [EVSEGroup](../docs/EVSEGroup.md).

**Only `DE*GEF` can do this.** Its data is its own: a change of it must be signed by a signing key of `DE*GEF` that
may sign its `infrastructure`, and nobody else has one - neither `OCC` nor a hub operating `QA`, whose prompt and web
interface show a pool but add none. The operator signs a change set in its own program and commits it to the history
of `QA`; [with the hub](#with-the-hub) anybody submits it, without signing in: whether it may change what it changes,
its signatures decide.

The code is [exampleA3.cs](csharp/exampleA3.cs), called by [Program.cs](csharp/Program.cs) with what examples A1 and A2
left, the history of `QA` and the private keys of `DE*GEF`:

```csharp
ExampleA3.Run(a1, a2);

ExampleA3.Validate();
```

The output is what one run printed, `…` for elided keys, signatures, digests and timestamps.


## 1. The change sets of `DE*GEF`

Two helpers do the work of every step: `ChangeSet(…)` makes a change set of the head of `QA` and signs it with the given
key, `Commit(…)` signs it with the `infrastructure` key of `DE*GEF` and commits it to the history.

```csharp
var infrastructureKey     = A2.SigningKeys[POIKeyUsages.Infrastructure];

RoamingNetworkChangeSet ChangeSet(String Description, COSEKeyPair Key, params RoamingNetworkChange[] Changes)

    => A1.History.Head.RoamingNetwork.
           CreateChangeSet(Changes, Description: ImmutableI18NString.Create(Languages.en, Description)).
           Sign(Key.Private, POIPublicKey.From(Key.Private).Id, Key.Algorithm);

// The given changes, as a change set of the head signed by DE*GEF, committed to the history of QA.
void Commit(String Description, params RoamingNetworkChange[] Changes)
{

    var result = A1.History.Commit(ChangeSet(Description, infrastructureKey, Changes));

    if (!result.IsCommitted)
        Helpers.Fail(result.ErrorResponse ?? result.Status.ToString());

    Console.WriteLine($"{result.Status}, revision {result.Commit?.Revision}: {Description}");

}
```

`CreateChangeSet` makes the change set for the version it is called on: its `beforeETags` are those of the head, its
`afterETags` those of the result. Made for an older version, it is merged, or refused where it changes what changed
meanwhile.


## 2. The templates: DC and AC

A [charging station template](../docs/ChargingStationTemplate.md) has the properties of a charging station, an
[EVSE template](../docs/EVSETemplate.md) those of an EVSE; their identifications are `{operatorId}*ST{suffix}` and
`{operatorId}*ET{suffix}`, and they are embedded in their operator, `chargingStationTemplates` and `EVSETemplates`. A
template names no members: a station, an EVSE or an energy meter names the templates it takes, so they come first. Each
charging station template here gives its stations their power together, `maxPower`: a station takes it unless it has
its own, and never above the one of all its EVSEs together; and their manufacturer, `GraphDefined` of
[example A2](exampleA2.md), and one of its models, `GD-DC300` or `GD-AC44`: a model is named with its manufacturer,
and only one it stated. Each EVSE template gives its EVSEs their connector: a CCS cable to the DC EVSEs,
a type 2 socket to the AC EVSEs; an EVSE takes the connectors of its templates besides its own, of the same
identification its own. An [energy meter template](../docs/EnergyMeterTemplate.md) gives its meters what a series has in
common: their role, `evse`, their manufacturer, `KERNBERG` for the DC meters, `SAALE` for the AC meters, its model and
their signature method, `OCMF`; its identification is free text, here `{operatorId}*EMT{suffix}`, unique in the network,
and it is embedded in its operator as `energyMeterTemplates`, added with its operator as the parent.

```csharp
Commit("The templates of the DC and the AC stations, EVSEs and energy meters",
             RoamingNetworkChange.Add(new ChargingStationTemplate(ChargingStationTemplate_Id.Parse("DE*GEF*ST_DC"),
                                                                  Name:                I18NString.Create(Languages.en, "DC charging station"),
                                                                  ManufacturerId:      ExampleA2.stationMakerId,
                                                                  ModelId:             ChargingStationModel_Id.Parse("GD-DC300"),
                                                                  MaxPower:            Watt.FromKW(300))),
             RoamingNetworkChange.Add(new ChargingStationTemplate(ChargingStationTemplate_Id.Parse("DE*GEF*ST_AC"),
                                                                  Name:                I18NString.Create(Languages.en, "AC charging station"),
                                                                  ManufacturerId:      ExampleA2.stationMakerId,
                                                                  ModelId:             ChargingStationModel_Id.Parse("GD-AC44"),
                                                                  MaxPower:            Watt.FromKW(44))),
             RoamingNetworkChange.Add(new EVSETemplate(EVSETemplate_Id.Parse("DE*GEF*ET_DC"),
                                                       Name:                I18NString.Create(Languages.en, "DC EVSE"),
                                                       ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2_COMBO, Format: ChargingConnectorFormat.CABLE) ])),
             RoamingNetworkChange.Add(new EVSETemplate(EVSETemplate_Id.Parse("DE*GEF*ET_AC"),
                                                       Name:                I18NString.Create(Languages.en, "AC EVSE"),
                                                       ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2,       Format: ChargingConnectorFormat.SOCKET) ])),
             RoamingNetworkChange.Add(new EnergyMeterTemplate(EnergyMeterTemplate_Id.Parse("DE*GEF*EMT_DC"),
                                                              Name:                I18NString.Create(Languages.en, "DC energy meter"),
                                                              Role:                EnergyMeterRole.EVSE,
                                                              ManufacturerId:      ExampleA2.dcMeterMakerId,
                                                              ModelId:             EnergyMeterModel_Id.Parse("KDM-500"),
                                                              SignatureMethods:    [ SignatureMethod.OCMF ]),
                                      ExampleA2.gefId),
             RoamingNetworkChange.Add(new EnergyMeterTemplate(EnergyMeterTemplate_Id.Parse("DE*GEF*EMT_AC"),
                                                              Name:                I18NString.Create(Languages.en, "AC energy meter"),
                                                              Role:                EnergyMeterRole.EVSE,
                                                              ManufacturerId:      ExampleA2.acMeterMakerId,
                                                              ModelId:             EnergyMeterModel_Id.Parse("SM-3"),
                                                              SignatureMethods:    [ SignatureMethod.OCMF ]),
                                      ExampleA2.gefId));
```

```
Committed, revision 3: The templates of the DC and the AC stations, EVSEs and energy meters
```

All six in one change set, so in one commit. POI_API serves them, `GET ~/RNs/QA/CSOs/DE*GEF/CSTs` lists the charging station templates
as the operator embeds them:

```json
[
  {
    "@id":            "DE*GEF*ST_AC",
    "created":        "…",
    "lastChange":     "…",
    "manufacturerId": "GraphDefined",
    "maxPower":       44000,
    "modelId":        "GD-AC44",
    "name":           { "en": "AC charging station" }
  },
  {
    "@id":            "DE*GEF*ST_DC",
    "created":        "…",
    "lastChange":     "…",
    "manufacturerId": "GraphDefined",
    "maxPower":       300000,
    "modelId":        "GD-DC300",
    "name":           { "en": "DC charging station" }
  }
]
```

and `GET ~/RNs/QA/CSOs/DE*GEF/EVSETs` the EVSE templates, the DC one of the two:

```json
{
  "@id":                "DE*GEF*ET_DC",
  "chargingConnectors": [ { "@id": "1", "format": "CABLE", "type": "IEC_62196_T2_COMBO" } ],
  "created":            "…",
  "lastChange":         "…",
  "name":               { "en": "DC EVSE" }
}
```

and `GET ~/RNs/QA/CSOs/DE*GEF/EMTs` the energy meter templates, the DC one of the two:

```json
{
  "@id":              "DE*GEF*EMT_DC",
  "created":          "…",
  "lastChange":       "…",
  "manufacturerId":   "KERNBERG",
  "modelId":          "KDM-500",
  "name":             { "en": "DC energy meter" },
  "role":             "evse",
  "signatureMethods": [ "OCMF" ]
}
```


## 3. The first pool: a DC and an AC station

Both pools follow one pattern: pool `n` has the DC station `2n-1`, two EVSEs of 150 kW, and the AC station `2n`,
two EVSEs of 22 kW; the EVSEs of station `s` are `DE*GEF*E{s}*1` and `DE*GEF*E{s}*2`. Each EVSE says its current
type and its power, names its EVSE template, and embeds its energy meter, `DE*GEF*M{s}*{e}`; each station names its
charging station template. `Pool(…)`, `Station(…)` and `Meter(…)` are helpers of `ExampleA3`, which
[example A4](exampleA4.md) builds its pools with as well:

```csharp
// Pool n: the DC station 2n-1, two EVSEs of 150 kW, and the AC station 2n, two EVSEs of the given power.
internal static ChargingPool Pool(Int32 N, String Name, Double Latitude, Double Longitude, Int32 ACkW = 22)

    => new (ChargingPool_Id.Parse($"DE*GEF*P{N}"),
            Name:              I18NString.Create(Languages.en, Name),
            GeoLocation:       GeoCoordinate.Parse(Latitude, Longitude),
            ChargingStations:  [
                Station(2 * N - 1, CurrentTypes.DC,             150,  "DC"),
                Station(2 * N,     CurrentTypes.AC_ThreePhases, ACkW, "AC")
            ]);

// Station s: two EVSEs, DE*GEF*E{s}*1 and DE*GEF*E{s}*2, of the given current and power, and the templates of its kind.
private static ChargingStation Station(Int32 S, CurrentTypes CurrentTypes, Int32 kW, String Kind)

    => new (ChargingStation_Id.Parse($"DE*GEF*S{S}"),
            TemplateIds:  [ ChargingStationTemplate_Id.Parse($"DE*GEF*ST_{Kind}") ],
            EVSEs: [
                new EVSE(EVSE_Id.Parse($"DE*GEF*E{S}*1"), CurrentTypes: CurrentTypes, MaxPower: Watt.FromKW(kW), TemplateIds: [ EVSETemplate_Id.Parse($"DE*GEF*ET_{Kind}") ], EnergyMeter: Meter(S, 1, Kind)),
                new EVSE(EVSE_Id.Parse($"DE*GEF*E{S}*2"), CurrentTypes: CurrentTypes, MaxPower: Watt.FromKW(kW), TemplateIds: [ EVSETemplate_Id.Parse($"DE*GEF*ET_{Kind}") ], EnergyMeter: Meter(S, 2, Kind))
            ]);

// The energy meter of EVSE e of station s: its serial number and the public key it signs its measurements with, made
// in the meter, whose private key never leaves it; the rest from its energy meter template.
private static EnergyMeter Meter(Int32 S, Int32 E, String Kind)
{

    using var meterKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    return new (EnergyMeter_Id.Parse($"DE*GEF*M{S}*{E}"),
                SerialNumber:  $"{Kind}M-2026-{S:D3}-{E}",
                PublicKeys:    [ new EnergyMeterPublicKey(meterKey.ExportSubjectPublicKeyInfo()) ],
                TemplateIds:   [ EnergyMeterTemplate_Id.Parse($"DE*GEF*EMT_{Kind}") ]);

}
```

```csharp
// Signed by its key for its tariffs, or by the roaming network operator: refused, the pool is DE*GEF's infrastructure.
var byTariffsKey  = A1.History.Commit(ChangeSet("Jena Paradies, a DC and an AC station", A2.SigningKeys[POIKeyUsages.Tariffs], RoamingNetworkChange.Add(Pool(1, "Jena Paradies", 50.9271, 11.5892))));
var byOCC         = A1.History.Commit(ChangeSet("Jena Paradies, a DC and an AC station", A1.SigningKeys["AlphaOrg"],           RoamingNetworkChange.Add(Pool(1, "Jena Paradies", 50.9271, 11.5892))));

Commit("Jena Paradies, a DC and an AC station", RoamingNetworkChange.Add(Pool(1, "Jena Paradies", 50.9271, 11.5892)));
```

```
Signed by its tariffs key: Refused, Change 0: Add of the ChargingPool 'DE*GEF*P1' needs a signature by a signing key of the ChargingStationOperator 'DE*GEF' that may sign 'infrastructure'!
Signed by AlphaOrg of OCC: Refused, Change 0: Add of the ChargingPool 'DE*GEF*P1' needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'!
Committed, revision 4: Jena Paradies, a DC and an AC station
```

A key of `DE*GEF` for its tariffs signs no pools, and the roaming network operator signs none of the data of a party;
nothing is committed then.

One `Add` of the pool, with its stations, EVSEs and energy meters embedded: the parent of a pool is the operator its
identification names, and only that operator's signature, by a key that may sign its `infrastructure`, lets it in. A
template its members name must be one of that operator, a manufacturer they name one of the network, and a model they
name one of their manufacturer, as they are at runtime, with what their templates give them. An energy meter is
no entity of a change set of its own: it is embedded in its EVSE, and changes as its property `energyMeter`, see
[EnergyMeter](../docs/EnergyMeter.md). Its public key is the DER encoded SubjectPublicKeyInfo, here of a P-256 key, `@id`
its SHA-256; a transparency software checks the signed meter values of a charging session with it.


## 4. The second pool, after the same pattern

```csharp
Commit("Jena Nord, a DC and an AC station",     RoamingNetworkChange.Add(Pool(2, "Jena Nord",     50.9447, 11.5867)));
```

```
Committed, revision 5: Jena Nord, a DC and an AC station
```


## 5. The groups of the stations: DC and AC

A [charging station group](../docs/ChargingStationGroup.md) names its stations by their identifications, of its
operator, in any of its pools: `{operatorId}*SG{suffix}`, embedded in its operator as `chargingStationGroups`. It gives
them nothing: here it collects the stations of one current across both pools, e.g. to name them in an offer, and has a
status of its own.

```csharp
Commit("The DC and the AC charging stations",
             RoamingNetworkChange.Add(new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG_DC"),
                                                               Name:                I18NString.Create(Languages.en, "DC charging stations"),
                                                               ChargingStationIds:  [ ChargingStation_Id.Parse("DE*GEF*S1"), ChargingStation_Id.Parse("DE*GEF*S3") ])),
             RoamingNetworkChange.Add(new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG_AC"),
                                                               Name:                I18NString.Create(Languages.en, "AC charging stations"),
                                                               ChargingStationIds:  [ ChargingStation_Id.Parse("DE*GEF*S2"), ChargingStation_Id.Parse("DE*GEF*S4") ])));
```

```
Committed, revision 6: The DC and the AC charging stations
```

`GET ~/RNs/QA/CSOs/DE*GEF/CSGs` lists both, as the operator embeds them:

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

Commit("The DC and the AC EVSEs",
             RoamingNetworkChange.Add(new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EG_DC"),
                                                    Name:     I18NString.Create(Languages.en, "DC EVSEs"),
                                                    EVSEIds:  EVSEsOf(1, 3))),
             RoamingNetworkChange.Add(new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EG_AC"),
                                                    Name:     I18NString.Create(Languages.en, "AC EVSEs"),
                                                    EVSEIds:  EVSEsOf(2, 4))));
```

```
Committed, revision 7: The DC and the AC EVSEs
```

`GET ~/RNs/QA/CSOs/DE*GEF/EVSEGs/DE*GEF*EG_DC`:

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
`GET ~/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/CSTs`: the DC template alone; its groups, `…/CSGs`: the DC
station group; the groups of one of its EVSEs, `…/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*1/EVSEGs`: the DC EVSE group. A station
may name several templates: of two giving a value, the one it names first gives it, see
[templates](../docs/README.md#templates-and-effective-members).


## 7. As they are at runtime

`ExampleA3.Validate()` checks `QA` as everybody does, now at revision 7, and shows the pools of `DE*GEF` as they are at
runtime, with what their templates give them, every EVSE with its energy meter, from what is published only:

```csharp
ExampleA1.Validate();

if (!RoamingNetwork.TryParse(Helpers.ReadJSON("published/QA.json"), out var qa, out var error))
    Helpers.Fail(error);

if (!qa.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
    Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

foreach (var pool in gef.EffectiveChargingPools)
{

    Console.WriteLine($"{pool.Id}  {pool.Name.FirstText()}");

    foreach (var station in pool.ChargingStations)
    {

        Console.WriteLine($"  {station.Id}  {station.MaxPower?.kW} kW  {station.ManufacturerId} {station.ModelId}  (templates {String.Join(", ", station.TemplateIds)})");

        foreach (var evse in station.EVSEs)
        {

            Console.WriteLine($"    {evse.Id}  {evse.CurrentTypes} {evse.MaxPower?.kW} kW  " +
                              String.Join(", ", evse.ChargingConnectors.Select(connector => $"{connector.Type} {connector.Format}")) +
                              $"  (templates {String.Join(", ", evse.TemplateIds)})");

            if (evse.EnergyMeter is EnergyMeter meter)
                Console.WriteLine($"      {meter.Id}  {meter.ManufacturerId} {meter.ModelId}, serial {meter.SerialNumber}, {String.Join(", ", meter.SignatureMethods)}, " +
                                  String.Join(", ", meter.PublicKeys.Select(publicKey => $"{publicKey.Algorithm} key {publicKey.Id[..8]}…")) +
                                  $"  (templates {String.Join(", ", meter.TemplateIds)})");

        }

    }

}
```

```
QA is checked: revision 7, commit 6b4309ffd15468b8cb160bdaa7293e03475e1e281d35ede3238846acd8b538cc, signed by AlphaOrg and BetaOrg and GammaOrg

DE*GEF*P1  Jena Paradies
  DE*GEF*S1  300 kW  GraphDefined GD-DC300  (templates DE*GEF*ST_DC)
    DE*GEF*E1*1  DC 150 kW  IEC_62196_T2_COMBO CABLE  (templates DE*GEF*ET_DC)
      DE*GEF*M1*1  KERNBERG KDM-500, serial DCM-2026-001-1, OCMF, secp256r1 key 0cc14973…  (templates DE*GEF*EMT_DC)
    DE*GEF*E1*2  DC 150 kW  IEC_62196_T2_COMBO CABLE  (templates DE*GEF*ET_DC)
      DE*GEF*M1*2  KERNBERG KDM-500, serial DCM-2026-001-2, OCMF, secp256r1 key 4a134d5c…  (templates DE*GEF*EMT_DC)
  DE*GEF*S2  44 kW  GraphDefined GD-AC44  (templates DE*GEF*ST_AC)
    DE*GEF*E2*1  AC_ThreePhases 22 kW  IEC_62196_T2 SOCKET  (templates DE*GEF*ET_AC)
      DE*GEF*M2*1  SAALE SM-3, serial ACM-2026-002-1, OCMF, secp256r1 key 860bdc5a…  (templates DE*GEF*EMT_AC)
    DE*GEF*E2*2  AC_ThreePhases 22 kW  IEC_62196_T2 SOCKET  (templates DE*GEF*ET_AC)
      DE*GEF*M2*2  SAALE SM-3, serial ACM-2026-002-2, OCMF, secp256r1 key 801812ee…  (templates DE*GEF*EMT_AC)
DE*GEF*P2  Jena Nord
  DE*GEF*S3  300 kW  GraphDefined GD-DC300  (templates DE*GEF*ST_DC)
    …
  DE*GEF*S4  44 kW  GraphDefined GD-AC44  (templates DE*GEF*ST_AC)
    …

DE*GEF*SG_AC  DE*GEF*S2, DE*GEF*S4
DE*GEF*SG_DC  DE*GEF*S1, DE*GEF*S3
DE*GEF*EG_AC  DE*GEF*E2*1, DE*GEF*E2*2, DE*GEF*E4*1, DE*GEF*E4*2
DE*GEF*EG_DC  DE*GEF*E1*1, DE*GEF*E1*2, DE*GEF*E3*1, DE*GEF*E3*2
```

The power, the manufacturer and the model of the stations, the connectors of the EVSEs, and the manufacturer, model and
signature method of the energy meters are the templates'; the stations, EVSEs and energy meters as they are stored name
their templates only, and a meter keeps its serial number and its public key.

POI_API serves a pool, its stations and its EVSEs as they are at runtime as well, with what their templates give them:
`GET ~/RNs/QA/CSOs/DE*GEF/CPs/DE*GEF*P1`, shortened to the first EVSE of each station:

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
          "energyMeter":        {
            "@id":              "DE*GEF*M1*1",
            "created":          "…",
            "lastChange":       "…",
            "manufacturerId":   "KERNBERG",
            "modelId":          "KDM-500",
            "publicKeys":       [ { "@id": "0cc14973…", "key": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE…" } ],
            "role":             "evse",
            "serialNumber":     "DCM-2026-001-1",
            "signatureMethods": [ "OCMF" ],
            "templateIds":      [ "DE*GEF*EMT_DC" ]
          },
          "lastChange":         "…",
          "maxPower":           150000,
          "templateIds":        [ "DE*GEF*ET_DC" ]
        },
        …
      ],
      "created":        "…",
      "lastChange":     "…",
      "manufacturerId": "GraphDefined",
      "maxPower":       300000,
      "modelId":        "GD-DC300",
      "templateIds":    [ "DE*GEF*ST_DC" ]
    },
    {
      "@id": "DE*GEF*S2",
      "EVSEs": [
        {
          "@id":                "DE*GEF*E2*1",
          "chargingConnectors": [ { "@id": "1", "format": "SOCKET", "type": "IEC_62196_T2" } ],
          "created":            "…",
          "currentTypes":       [ "AC_ThreePhases" ],
          "energyMeter":        { "@id": "DE*GEF*M2*1", …, "manufacturerId": "SAALE", "modelId": "SM-3", … },
          "lastChange":         "…",
          "maxPower":           22000,
          "templateIds":        [ "DE*GEF*ET_AC" ]
        },
        …
      ],
      "created":        "…",
      "lastChange":     "…",
      "manufacturerId": "GraphDefined",
      "maxPower":       44000,
      "modelId":        "GD-AC44",
      "templateIds":    [ "DE*GEF*ST_AC" ]
    }
  ],
  "created":     "…",
  "geoLocation": { "lat": 50.9271, "lng": 11.5892 },
  "lastChange":  "…",
  "name":        { "en": "Jena Paradies" }
}
```

The connectors, the station powers and manufacturers and what the energy meters share are the templates'; the `lastChange` of each is the latest of its own and of its
templates, here its own, as the templates are older. `?view=stored` serves the pool as its operator embeds it - with the
ETag a [precondition](../docs/README.md#etags) of a change set names - naming its templates, without what they give:

```json
{
  "@id": "DE*GEF*P1",
  "chargingStations": [
    {
      "@id": "DE*GEF*S1",
      "EVSEs": [
        { "@id": "DE*GEF*E1*1", "created": "…", "currentTypes": [ "DC" ],
          "energyMeter": { "@id": "DE*GEF*M1*1", "created": "…", "lastChange": "…", "publicKeys": [ … ], "serialNumber": "DCM-2026-001-1", "templateIds": [ "DE*GEF*EMT_DC" ] },
          "lastChange": "…", "maxPower": 150000, "templateIds": [ "DE*GEF*ET_DC" ] },
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


## With the hub

A [RoamingHubCLI](https://github.com/OpenChargingCloud/RoamingHubCLI) serving `QA` takes the change sets of `DE*GEF`
over HTTP, `COMMIT /poi/RNs/QA/history/changeSets`, from anybody, and serves the pools as above. The templates,
stations and energy meters name the manufacturers of [example A2](exampleA2.md) and their models, which the hub added
and they stated [before](exampleA2.md#the-manufacturers); its revisions are three higher than above.

### The program of `DE*GEF`, over HTTP

A console project of `DE*GEF` of its own, talking to the hub, with `LoadKey` to read its key back. Two helpers do the
rest for every step, in the place of those of [step 1](#1-the-change-sets-of-degef): `Head()` reads the head of `QA`
as the hub serves it, checked against its ETags as it is read, and `Commit(…)` makes a change set of it, signs it with
the signing key of `DE*GEF` and submits it. Both talk to the hub with the `HTTPClient` of Hermod; a change set goes by
the method `COMMIT`, `http.COMMIT(…)`.

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

// The signing key for its infrastructure DE*GEF made in example A2.
var gefSigningKey     = LoadKey("DE-GEF/signing-infrastructure-privateKey.pem");

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

Steps 2 to 6 then run with `await Commit(…)`; each answers `201 Committed, revision …`.


### What the hub answers

`201 Created` answers a change set with its commit and the new head, shortened:

```json
{
  "status":          "Committed",
  "@id":             "2d2a60b9…",
  "revision":        7,
  "parentIds":       [ "adcb769c…" ],
  "changeSetId":     "MSQ5AtSUtAG8542fS12Yh6rCW7x8xv2G",
  "createdAt":       "2026-10-17T18:16:17.709Z",
  "description":     { "en": "Jena Paradies, a DC and an AC station" },
  "numberOfChanges": 1,
  "signatureKeyIds": [ "a3fec322…" ],
  "eTags":           [ [ "json", "sha256", "hex", "faf55b2e…" ], [ "cbor", "sha256", "hex", "23340139…" ] ],
  "head":            { "@id": "2d2a60b9…", "revision": 7, … }
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


### Where the hub shows them

```
RoamingHub> roamingNetworks
QA  revision 10  Quality assurance  (operated by 'RNO-47EAB7CF', this hub, 1 charging station operator(s), 2 charging pool(s))
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
| `GET /poi/RNs/QA/history/head` | the head commit: `revision` 9, its `parentIds`, `eTags` and `signatureKeyIds`, the one of `DE*GEF` |


## Next

- The first checkpoint of `QA`, two pools more, and joining `QA` from the checkpoint: [example A4](exampleA4.md).
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
