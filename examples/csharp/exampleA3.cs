using System.Security.Cryptography;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA3
{

    // The charging station operator "DE*GEF" of example A2 adds its data to "QA": the templates of a DC and an AC station,
    // of their EVSEs and of the energy meters of these, two pools of a DC and an AC station of two EVSEs each, every EVSE
    // with its energy meter, and the groups of its stations and EVSEs.


    public static void Run(ExampleA1.Result A1, ExampleA2.Result A2)
    {

        #region 1. The change sets of DE*GEF

        // Its data is its own: a change set of it is signed by a signing key of DE*GEF that may sign its infrastructure,
        // and nobody else has one. A change set is made for the head, its beforeETags those of the head.
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

        #endregion

        #region 2. The templates: DC and AC

        // A template names no members: a station, an EVSE or an energy meter names the templates it takes, so they come
        // first. Each charging station template gives its stations their power together, their manufacturer and model,
        // each EVSE template its EVSEs their connector, each energy meter template its meters their manufacturer, model and
        // how they sign their measurements.
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

        #endregion

        #region 3. The first pool: a DC and an AC station

        // Pool n has the DC station 2n-1, two EVSEs of 150 kW, and the AC station 2n, two EVSEs of 22 kW, every EVSE with its
        // energy meter: Pool(…), below.

        // Signed by its key for its tariffs, or by the roaming network operator: refused, the pool is DE*GEF's
        // infrastructure. Nothing is committed.
        var byTariffsKey          = A1.History.Commit(ChangeSet("Jena Paradies, a DC and an AC station", A2.SigningKeys[POIKeyUsages.Tariffs], RoamingNetworkChange.Add(Pool(1, "Jena Paradies", 50.9271, 11.5892))));
        var byOCC                 = A1.History.Commit(ChangeSet("Jena Paradies, a DC and an AC station", A1.SigningKeys["AlphaOrg"],           RoamingNetworkChange.Add(Pool(1, "Jena Paradies", 50.9271, 11.5892))));

        Console.WriteLine($"Signed by its tariffs key: {byTariffsKey.Status}, {byTariffsKey.ErrorResponse}");
        Console.WriteLine($"Signed by AlphaOrg of OCC: {byOCC.Status}, {byOCC.ErrorResponse}");

        Commit("Jena Paradies, a DC and an AC station", RoamingNetworkChange.Add(Pool(1, "Jena Paradies", 50.9271, 11.5892)));

        #endregion

        #region 4. The second pool, after the same pattern

        Commit("Jena Nord, a DC and an AC station",     RoamingNetworkChange.Add(Pool(2, "Jena Nord",     50.9447, 11.5867)));

        #endregion

        #region 5. The groups of the stations: DC and AC

        // A group names its members, of its operator, in any of its pools; it gives them nothing, but collects them, e.g. to
        // name them in an offer, and has a status of its own.
        Commit("The DC and the AC charging stations",
               RoamingNetworkChange.Add(new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG_DC"),
                                                                 Name:                I18NString.Create(Languages.en, "DC charging stations"),
                                                                 ChargingStationIds:  [ ChargingStation_Id.Parse("DE*GEF*S1"), ChargingStation_Id.Parse("DE*GEF*S3") ])),
               RoamingNetworkChange.Add(new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG_AC"),
                                                                 Name:                I18NString.Create(Languages.en, "AC charging stations"),
                                                                 ChargingStationIds:  [ ChargingStation_Id.Parse("DE*GEF*S2"), ChargingStation_Id.Parse("DE*GEF*S4") ])));

        #endregion

        #region 6. The groups of the EVSEs: DC and AC

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

        Console.WriteLine();

        // What everybody may read now: the roaming network with the data of DE*GEF, and the commit leading to it.
        File.WriteAllText($"published/{A1.RoamingNetwork.Id}.json",         A1.RoamingNetwork.  ToJSON(IncludeETags: true).ToString());
        File.WriteAllText($"published/{A1.RoamingNetwork.Id}-commit.json",  A1.History.Head.Commit.ToJSON().                  ToString());

        #endregion

    }


    /// <summary>
    /// Checks QA as everybody does, now at revision 7, and shows the pools of DE*GEF as they are at runtime: with what
    /// their templates give them; from what is published only.
    /// </summary>
    public static void Validate()
    {

        #region 7. As they are at runtime

        // a) QA as everybody checks it: from the QR code, the roaming network and its commit.
        ExampleA1.Validate();

        if (!RoamingNetwork.TryParse(Helpers.ReadJSON("published/QA.json"), out var qa, out var error))
            Helpers.Fail(error);

        if (!qa.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        // b) Its pools, stations and EVSEs as they are at runtime: the connectors and the station powers are the templates'.
        Console.WriteLine();

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

        // c) Its groups: the stations and the EVSEs of one current.
        Console.WriteLine();

        foreach (var group in gef.ChargingStationGroups)
            Console.WriteLine($"{group.Id}  {String.Join(", ", group.ChargingStationIds)}");

        foreach (var group in gef.EVSEGroups)
            Console.WriteLine($"{group.Id}  {String.Join(", ", group.EVSEIds)}");

        #endregion

    }


    #region Helpers, also of example A4

    // Pool n of DE*GEF: the DC station 2n-1, two EVSEs of 150 kW, and the AC station 2n, two EVSEs of the given power; the
    // EVSEs of station s are DE*GEF*E{s}*1 and DE*GEF*E{s}*2.
    internal static ChargingPool Pool(Int32 N, String Name, Double Latitude, Double Longitude, Int32 ACkW = 22)

        => new (ChargingPool_Id.Parse($"DE*GEF*P{N}"),
                Name:              I18NString.Create(Languages.en, Name),
                GeoLocation:       GeoCoordinate.Parse(Latitude, Longitude),
                ChargingStations:  [
                    Station(2 * N - 1, CurrentTypes.DC,             150,  "DC"),
                    Station(2 * N,     CurrentTypes.AC_ThreePhases, ACkW, "AC")
                ]);

    // Station s and its two EVSEs: each EVSE says its current type and its power and names its EVSE template, each station
    // its charging station template.
    private static ChargingStation Station(Int32 S, CurrentTypes CurrentTypes, Int32 kW, String Kind)

        => new (ChargingStation_Id.Parse($"DE*GEF*S{S}"),
                TemplateIds:  [ ChargingStationTemplate_Id.Parse($"DE*GEF*ST_{Kind}") ],
                EVSEs: [
                    new EVSE(EVSE_Id.Parse($"DE*GEF*E{S}*1"), CurrentTypes: CurrentTypes, MaxPower: Watt.FromKW(kW), TemplateIds: [ EVSETemplate_Id.Parse($"DE*GEF*ET_{Kind}") ], EnergyMeter: Meter(S, 1, Kind)),
                    new EVSE(EVSE_Id.Parse($"DE*GEF*E{S}*2"), CurrentTypes: CurrentTypes, MaxPower: Watt.FromKW(kW), TemplateIds: [ EVSETemplate_Id.Parse($"DE*GEF*ET_{Kind}") ], EnergyMeter: Meter(S, 2, Kind))
                ]);

    // The energy meter of EVSE e of station s, DE*GEF*M{s}*{e}: what belongs to the single device, its serial number and
    // the public key it signs its measurements with, made in the meter, whose private key never leaves it; the rest from
    // its energy meter template.
    private static EnergyMeter Meter(Int32 S, Int32 E, String Kind)
    {

        using var meterKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        return new (EnergyMeter_Id.Parse($"DE*GEF*M{S}*{E}"),
                    SerialNumber:  $"{Kind}M-2026-{S:D3}-{E}",
                    PublicKeys:    [ new EnergyMeterPublicKey(meterKey.ExportSubjectPublicKeyInfo()) ],
                    TemplateIds:   [ EnergyMeterTemplate_Id.Parse($"DE*GEF*EMT_{Kind}") ]);

    }

    #endregion

}
