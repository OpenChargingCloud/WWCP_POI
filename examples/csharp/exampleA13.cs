using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA13
{

    // Who checks the meter? The signed meter values of a charging session are checked by a transparency software, and
    // the software is approved for a charging station model by a conformity assessment body. Each says so itself: the
    // transparency software manufacturer GraphDefined signs its release, the PTB, notified body for the Measuring
    // Instruments Directive, signs its certificate; the BSI, for the Cyber Resilience Act, does not take part yet. DE*GEF
    // names both for the energy meters of its DC stations, and the station proof of the driver's app gives them.


    internal static readonly TransparencySoftwareManufacturer_Id  softwareMakerId  = TransparencySoftwareManufacturer_Id.Parse("GraphDefined");
    internal static readonly ConformityAssessmentBody_Id          ptbId            = ConformityAssessmentBody_Id.Parse("PTB");
    internal static readonly ConformityAssessmentBody_Id          bsiId            = ConformityAssessmentBody_Id.Parse("BSI");
    internal static readonly TransparencySoftware_Id              chargyId         = TransparencySoftware_Id.Parse("chargy-1.5.0");
    internal static readonly TransparencySoftwareCertificate_Id   certificateId    = TransparencySoftwareCertificate_Id.Parse("PTB-2026-0042");

    /// <summary>
    /// The station proof of DE*GEF*E1*1 the app gets.
    /// </summary>
    internal const String Proof = "published/QA-DE-GEF-E1-1-proof.json";


    public static void Run(ExampleA11.Result A11)
    {

        var now      = Timestamp.Now;
        var history  = A11.History;

        #region 1. The roaming network operator adds a transparency software manufacturer and two conformity assessment bodies

        // GraphDefined makes Chargy; its keys are of epoch 2, ML-DSA: one for its releases, one for itself.
        var softwareIdentityKey  = COSEKeyPair.NewMLDsa65();
        var softwareKey          = COSEKeyPair.NewMLDsa65();
        var softwarePartyKey     = COSEKeyPair.NewMLDsa65();

        // The PTB, notified body 0102 for the MID: one key for its certificates, one for itself.
        var ptbIdentityKey       = COSEKeyPair.NewMLDsa65();
        var ptbCertificatesKey   = COSEKeyPair.NewMLDsa65();
        var ptbPartyKey          = COSEKeyPair.NewMLDsa65();

        Directory.CreateDirectory("GraphDefined");
        Helpers.SaveKey(softwareIdentityKey.Private,  "GraphDefined/identity-privateKey.pem");
        Helpers.SaveKey(softwareKey.Private,          "GraphDefined/signing-software-privateKey.pem");
        Helpers.SaveKey(softwarePartyKey.Private,     "GraphDefined/signing-party-privateKey.pem");

        Directory.CreateDirectory("PTB");
        Helpers.SaveKey(ptbIdentityKey.Private,       "PTB/identity-privateKey.pem");
        Helpers.SaveKey(ptbCertificatesKey.Private,   "PTB/signing-certificates-privateKey.pem");
        Helpers.SaveKey(ptbPartyKey.Private,          "PTB/signing-party-privateKey.pem");

        var softwareMaker = new TransparencySoftwareManufacturer(
                                softwareMakerId,
                                I18NString.Create(Languages.en, "GraphDefined GmbH"),
                                Homepage:                       URL.Parse("https://www.graphdefined.com"),
                                CRARole:                        TransparencySoftwareManufacturer.CRARoles.Manufacturer,
                                VulnerabilityContact:           EMailAddress.Parse("security@graphdefined.example"),
                                VulnerabilityReporting:         URL.Parse("https://www.graphdefined.example/.well-known/security.txt"),
                                VulnerabilityDisclosurePolicy:  URL.Parse("https://www.graphdefined.example/security/policy"),
                                SecurityAdvisories:             URL.Parse("https://github.com/OpenChargingCloud/ChargyDesktopApp/security/advisories"),
                                IdentityKeys:                   [ POIPublicKey.From(softwareIdentityKey.Private) ],
                                SigningKeys:                    [ POIPublicKey.From(softwareKey.     Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Software ]),
                                                                  POIPublicKey.From(softwarePartyKey.Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Party    ]) ]
                            );

        var ptb           = new ConformityAssessmentBody(
                                ptbId,
                                I18NString.Create(Languages.de, "Physikalisch-Technische Bundesanstalt"),
                                Homepage:            URL.Parse("https://www.ptb.de"),
                                NotifiedBodyNumber:  "0102",
                                NotifiedFor:         [ ConformityAssessmentBody.Legislations.MID ],
                                IdentityKeys:        [ POIPublicKey.From(ptbIdentityKey.Private) ],
                                SigningKeys:         [ POIPublicKey.From(ptbCertificatesKey.Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Certificates ]),
                                                       POIPublicKey.From(ptbPartyKey.       Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Party        ]) ]
                            );

        // The BSI, for the CRA, does not take part yet: added without keys, the roaming network operator signs in its place.
        var bsi           = new ConformityAssessmentBody(
                                bsiId,
                                I18NString.Create(Languages.de, "Bundesamt für Sicherheit in der Informationstechnik"),
                                Homepage:     URL.Parse("https://www.bsi.bund.de"),
                                NotifiedFor:  [ ConformityAssessmentBody.Legislations.CRA ]
                            );

        // Who is a member is the roaming network operator's: two of its holders, with their signing keys of epoch 2; the new
        // identity keys sign as the proof that their parties hold them.
        var addParties    = history.Head.RoamingNetwork.CreateChangeSet(
                                [ RoamingNetworkChange.Add(softwareMaker), RoamingNetworkChange.Add(ptb), RoamingNetworkChange.Add(bsi) ],
                                Description: ImmutableI18NString.Create(Languages.en, "GraphDefined, transparency software manufacturer, the PTB and the BSI, conformity assessment bodies")
                            );

        foreach (var key in new[] { A11.SigningKeys["AlphaOrg"], A11.SigningKeys["BetaOrg"], softwareIdentityKey, ptbIdentityKey })
            addParties    = addParties.Sign(key.Private, POIPublicKey.From(key.Private).Id, key.Algorithm);

        Committed(history, addParties);

        #endregion

        #region 2. GraphDefined states its release of Chargy

        var chargy        = new TransparencySoftware(chargyId,
                                                     ImmutableI18NString.Create(Languages.en, "Chargy Transparency Software"),
                                                     TransparencySoftwareVersion.Parse("1.5.0"),
                                                     [ OpenSourceLicense.AGPL3 ],
                                                     SourceCodeRepository:     URL.Parse("https://github.com/OpenChargingCloud/ChargyDesktopApp"),
                                                     SupportUntil:             now.AddYears(5),
                                                     DeclarationOfConformity:  URL.Parse("https://www.graphdefined.example/chargy/1.5.0/declarationOfConformity.pdf"),
                                                     SoftwareBillOfMaterials:  URL.Parse("https://www.graphdefined.example/chargy/1.5.0/sbom.cdx.json"));

        var addChargy     = history.Head.RoamingNetwork.CreateChangeSet(
                                [ RoamingNetworkChange.Add(chargy, softwareMakerId) ],
                                Description: ImmutableI18NString.Create(Languages.en, "Chargy 1.5.0")
                            );

        // Signed by the roaming network operator: refused, the release is its manufacturer's.
        Console.WriteLine($"Signed by the roaming network operator: {history.Commit(addChargy.Sign(A11.SigningKeys["AlphaOrg"].Private, POIPublicKey.From(A11.SigningKeys["AlphaOrg"].Private).Id, A11.SigningKeys["AlphaOrg"].Algorithm).Sign(A11.SigningKeys["BetaOrg"].Private, POIPublicKey.From(A11.SigningKeys["BetaOrg"].Private).Id, A11.SigningKeys["BetaOrg"].Algorithm)).ErrorResponse}");

        Committed(history, addChargy.Sign(softwareKey.Private, POIPublicKey.From(softwareKey.Private).Id, softwareKey.Algorithm));

        #endregion

        #region 3. The PTB certifies Chargy for the DC stations of GraphDefined

        TransparencySoftwareCertificate CertificateUnder(String Legislation)

            => new (certificateId,
                    ExampleA2.stationMakerId,
                    ChargingStationModel_Id.Parse("GD-DC300"),
                    [ "1.0" ],
                    [ "2.5.0" ],
                    [ chargyId ],
                    [],
                    DocumentURL:                    URL.Parse("https://www.ptb.example/certificates/PTB-2026-0042.pdf"),
                    NotBefore:                      now,
                    NotAfter:                       now.AddYears(10),
                    Legislation:                    Legislation);

        // Under the CRA, which the PTB is not notified for: refused.
        try
        {
            history.Head.RoamingNetwork.CreateChangeSet([ RoamingNetworkChange.Add(CertificateUnder(ConformityAssessmentBody.Legislations.CRA), ptbId) ]);
        }
        catch (ArgumentException e)
        {
            Console.WriteLine($"A certificate of the PTB under the CRA: {e.Message.Split(" (Parameter")[0]}");
        }

        var addCertificate = history.Head.RoamingNetwork.CreateChangeSet(
                                 [ RoamingNetworkChange.Add(CertificateUnder(ConformityAssessmentBody.Legislations.MID), ptbId) ],
                                 Description: ImmutableI18NString.Create(Languages.en, "PTB-2026-0042: Chargy 1.5.0 for the GD-DC300")
                             );

        Committed(history, addCertificate.Sign(ptbCertificatesKey.Private, POIPublicKey.From(ptbCertificatesKey.Private).Id, ptbCertificatesKey.Algorithm));

        #endregion

        #region 4. DE*GEF names them for the energy meters of its DC stations

        if (!history.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        var infrastructureKey = A11.GEF.SigningKeys[POIKeyUsages.Infrastructure];
        var dcMeters          = gef.EnergyMeterTemplates.Single(template => template.Id == EnergyMeterTemplate_Id.Parse("DE*GEF*EMT_DC"));

        var nameChargy        = history.Head.RoamingNetwork.CreateChangeSet(
                                    [ RoamingNetworkChange.UpdateProperty(dcMeters, "transparencySoftwareStatus", null, new JArray(new TransparencySoftwareAssignment(chargyId, LegalStatus.GermanCalibrationLaw, certificateId).ToJSON(Embedded: true))) ],
                                    Description: ImmutableI18NString.Create(Languages.en, "The DC energy meters: Chargy 1.5.0 under the German calibration law, PTB-2026-0042")
                                );

        Committed(history, nameChargy.Sign(infrastructureKey.Private, POIPublicKey.From(infrastructureKey.Private).Id, infrastructureKey.Algorithm));
        Console.WriteLine();

        #endregion

        #region 5. The station proof the app gets

        history.AttestationSigners = [ .. A11.Signers ];

        if (!history.TryAttest(out _, out var error))
            Helpers.Fail(error);

        if (!history.TryGetChargingStationProof(ExampleA10.EVSEId, out var proof, out error))
            Helpers.Fail(error);

        File.WriteAllText(Proof, proof.ToJSON().ToString());
        File.WriteAllText($"published/{history.RoamingNetworkId}.json",        history.Head.RoamingNetwork.ToJSON(IncludeETags: true).ToString());
        File.WriteAllText($"published/{history.RoamingNetworkId}-commit.json", history.Head.Commit.ToJSON().ToString());

        Console.WriteLine($"The station proof of {ExampleA10.EVSEId}, revision {proof.Revision}: {new FileInfo(Proof).Length / 1024} KiB");
        Console.WriteLine();

        #endregion

    }


    /// <summary>
    /// The app checks the station proof against the trust anchor of epoch 2, and shows who checks its meter: the release,
    /// its manufacturer and the certificate, its body.
    /// </summary>
    public static void Validate()
    {

        #region 6. Who checks the meter

        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-mldsa.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        if (!RoamingNetworkProof.TryParse(Helpers.ReadJSON(Proof), out var proof, out error))
            Helpers.Fail(error);

        Console.WriteLine($"The station proof, revision {proof.Revision}, by its attestations: {(proof.Verify(anchor, out _, out error) ? "valid" : error)}");

        var gef      = proof.Given(RoamingNetwork.PropertyKeys.ChargingStationOperators).Single(json => json["@id"]?.Value<String>() == ExampleA2.gefId.ToString());
        var station  = proof.GivenBelow(gef, ChargingStationOperator.PropertyKeys.ChargingPools, ChargingPool.PropertyKeys.ChargingStations).Single(json => json[ChargingStation.PropertyKeys.EVSEs]?.Any(evse => evse["@id"]?.Value<String>() == ExampleA10.EVSEId.ToString()) == true);
        var meter    = (JObject) station[ChargingStation.PropertyKeys.EVSEs]!.Single(evse => evse["@id"]?.Value<String>() == ExampleA10.EVSEId.ToString())["energyMeter"]!;

        foreach (var assignment in meter["transparencySoftwareStatus"]!.Cast<JObject>())
        {

            var makers   = proof.Given(RoamingNetwork.PropertyKeys.TransparencySoftwareManufacturers);
            var maker    = makers.Single(json => proof.GivenBelow(json, TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware).Any(release => release["@id"]?.Value<String>() == assignment["transparencySoftwareId"]?.Value<String>()));
            var release  = proof.GivenBelow(maker, TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware).Single(json => json["@id"]?.Value<String>() == assignment["transparencySoftwareId"]?.Value<String>());

            var bodies   = proof.Given(RoamingNetwork.PropertyKeys.ConformityAssessmentBodies);
            var body     = bodies.Single(json => proof.GivenBelow(json, ConformityAssessmentBody.PropertyKeys.TransparencySoftwareCertificates).Any(certificate => certificate["@id"]?.Value<String>() == assignment["certificateId"]?.Value<String>()));
            var cert     = proof.GivenBelow(body, ConformityAssessmentBody.PropertyKeys.TransparencySoftwareCertificates).Single(json => json["@id"]?.Value<String>() == assignment["certificateId"]?.Value<String>());

            Console.WriteLine($"{meter["@id"]}, the energy meter of {ExampleA10.EVSEId}: {assignment["legalStatus"]}");
            Console.WriteLine($"  {release["name"]!["en"]} {release["version"]}, by {maker["name"]!["en"]}, {maker["craRole"]}, supported until {release["supportUntil"]}, vulnerabilities to {maker["vulnerabilityContact"]}, bill of materials {release["softwareBillOfMaterials"]}");
            Console.WriteLine($"  {cert["@id"]} for the {cert["chargingStationModel"]} {String.Join("/", cert["hardwareRevisions"]!.Values<String>())}, firmware {String.Join("/", cert["firmwareVersions"]!.Values<String>())}, under the {cert["legislation"]}, by the {body["@id"]}, notified body {body["notifiedBodyNumber"]} for {String.Join(", ", body["notifiedFor"]!.Values<String>())}; signed by itself: {(body["signingKeys"] is JArray { Count: > 0 } ? "yes, it has keys of its own" : "no, the roaming network operator in its place")}");
            // The model of the station, as its manufacturer states it, given by the proof with its manufacturer.
            var stationMaker  = proof.Given(RoamingNetwork.PropertyKeys.ChargingStationManufacturers).Single(json => json["@id"]?.Value<String>() == station["manufacturerId"]?.Value<String>());
            var model         = proof.GivenBelow(stationMaker, ChargingStationManufacturer.PropertyKeys.Models).Single(json => json["@id"]?.Value<String>() == station["modelId"]?.Value<String>());
            var revision      = station["hardwareRevision"]?.Value<String>();
            var firmware      = station["firmwareVersion"]?. Value<String>();

            Console.WriteLine($"  the station {station["@id"]} is a {model["@id"]} of {stationMaker["@id"]}, {model["name"]?["en"]}, hardware {revision}, firmware {firmware}: " +
                              (cert["chargingStationModel"]?.Value<String>() == model["@id"]?.Value<String>() && cert["chargingStationManufacturerId"]?.Value<String>() == stationMaker["@id"]?.Value<String>() &&
                               cert["hardwareRevisions"]!.Values<String>().Contains(revision) && cert["firmwareVersions"]!.Values<String>().Contains(firmware)
                                   ? "the model, hardware revision and firmware version the certificate is for"
                                   : "another model, hardware revision or firmware version"));

        }

        // The BSI is not given: no energy meter of the station names a certificate of it.
        Console.WriteLine($"Conformity assessment bodies the proof gives: {String.Join(", ", proof.Given(RoamingNetwork.PropertyKeys.ConformityAssessmentBodies).Select(json => json["@id"]))}");

        // A certificate changed on the way: the proof is no longer one of the version the attestations name.
        var changed = Helpers.ReadJSON(Proof);
        var changedCertificate = changed.Descendants().OfType<JObject>().Single(json => json["@id"]?.Type == JTokenType.String && json["@id"]!.Value<String>() == certificateId.ToString());
        changedCertificate["chargingStationModel"] = "GD-AC44";

        Console.WriteLine($"The certificate for another model: {(RoamingNetworkProof.TryParse(changed, out var changedProof, out error) && changedProof.Verify(anchor, out _, out error) ? "valid" : error)}");

        #endregion

    }


    #region Helpers

    // The given change set committed to the given history.
    private static void Committed(RoamingNetworkHistory History, RoamingNetworkChangeSet ChangeSet)
    {

        var result = History.Commit(ChangeSet);

        if (!result.IsCommitted)
            Helpers.Fail(result.ErrorResponse ?? result.Status.ToString());

        Console.WriteLine($"{result.Status}, revision {result.Commit?.Revision} of epoch {History.Epoch.Number}: {ChangeSet.Description.FirstText()}");

    }

    #endregion

}
