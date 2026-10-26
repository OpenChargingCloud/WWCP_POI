using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA13
{

    // Who checks the meter? The signed meter values of a charging session are checked by a transparency software, a
    // product of its own manufacturer, and a conformity assessment body certifies a model of charging stations or of
    // energy meters with its components: the transparency software it verified, and of a station the meters it mounts.
    // Each says so itself: the transparency software manufacturer GraphDefined signs its release, the PTB, notified body
    // for the Measuring Instruments Directive, signs its certificates; the BSI, for the Cyber Resilience Act, does not
    // take part yet. DE*GEF names the certificates for its DC stations and their meters, and the station proof of the
    // driver's app gives them.


    internal static readonly TransparencySoftwareManufacturer_Id  softwareMakerId  = TransparencySoftwareManufacturer_Id.Parse("GraphDefined");
    internal static readonly ConformityAssessmentBody_Id          ptbId            = ConformityAssessmentBody_Id.Parse("PTB");
    internal static readonly ConformityAssessmentBody_Id          bsiId            = ConformityAssessmentBody_Id.Parse("BSI");
    internal static readonly TransparencySoftware_Id              chargyId         = TransparencySoftware_Id.Parse("chargy-1.5.0");
    internal static readonly ConformityCertificate_Id             certificateId       = ConformityCertificate_Id.Parse("PTB-2026-0042");
    internal static readonly ConformityCertificate_Id             meterCertificateId  = ConformityCertificate_Id.Parse("PTB-2026-0017");
    internal static readonly EnergyMeterModel_Id                  kdm500              = EnergyMeterModel_Id.Parse("KDM-500");

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

        #region 3. The PTB certifies the DC stations of GraphDefined and their meters, with Chargy

        // The DC station in its hardware revision 1.0 with the firmware 2.5.0, with its components: Chargy 1.5.0, verified,
        // and the KDM-500 of KERNBERG it mounts, in its hardware revision 2.1 with the firmware 3.2.
        ConformityCertificate CertificateUnder(String Legislation)

            => new (certificateId,
                    ExampleA2.stationMakerId,
                    ChargingStationModel_Id.Parse("GD-DC300"),
                    [ HardwareRevision.Parse("1.0") ],
                    [ FirmwareVersion. Parse("2.5.0") ],
                    [ chargyId ],
                    [],
                    DocumentURL:   URL.Parse("https://www.ptb.example/certificates/PTB-2026-0042.pdf"),
                    NotBefore:     now,
                    NotAfter:      now.AddYears(10),
                    Legislation:   Legislation,
                    EnergyMeters:  [ new CertifiedEnergyMeter(ExampleA2.dcMeterMakerId, kdm500, [ HardwareRevision.Parse("2.1") ], [ FirmwareVersion.Parse("3.2") ]) ]);

        // The meter on its own, the KDM-500 in the same revision and firmware, with Chargy 1.5.0 as well.
        var meterCertificate = new ConformityCertificate(meterCertificateId,
                                                         ExampleA2.dcMeterMakerId,
                                                         kdm500,
                                                         [ HardwareRevision.Parse("2.1") ],
                                                         [ FirmwareVersion.Parse("3.2") ],
                                                         [ chargyId ],
                                                         [],
                                                         DocumentURL:  URL.Parse("https://www.ptb.example/certificates/PTB-2026-0017.pdf"),
                                                         NotBefore:    now,
                                                         NotAfter:     now.AddYears(10));

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
                                 [ RoamingNetworkChange.Add(CertificateUnder(ConformityAssessmentBody.Legislations.MID), ptbId),
                                   RoamingNetworkChange.Add(meterCertificate,                                             ptbId) ],
                                 Description: ImmutableI18NString.Create(Languages.en, "PTB-2026-0042 for the GD-DC300 with the KDM-500, PTB-2026-0017 for the KDM-500, each with Chargy 1.5.0")
                             );

        Committed(history, addCertificate.Sign(ptbCertificatesKey.Private, POIPublicKey.From(ptbCertificatesKey.Private).Id, ptbCertificatesKey.Algorithm));

        #endregion

        #region 4. DE*GEF names them for its DC stations and their energy meters

        if (!history.Head.RoamingNetwork.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        var infrastructureKey = A11.GEF.SigningKeys[POIKeyUsages.Infrastructure];
        var dcStations        = gef.ChargingStationTemplates.Single(template => template.Id == ChargingStationTemplate_Id.Parse("DE*GEF*ST_DC"));
        var dcMeters          = gef.EnergyMeterTemplates.    Single(template => template.Id == EnergyMeterTemplate_Id.    Parse("DE*GEF*EMT_DC"));

        // The templates give them: the stations their certificate, the meters theirs and the legal status of Chargy, which
        // the certificates naming it among their components prove.
        var nameChargy        = history.Head.RoamingNetwork.CreateChangeSet(
                                    [ RoamingNetworkChange.UpdateProperty(dcStations, "certificateIds",             null, new JArray(certificateId.ToString())),
                                      RoamingNetworkChange.UpdateProperty(dcMeters,   "certificateIds",             null, new JArray(meterCertificateId.ToString())),
                                      RoamingNetworkChange.UpdateProperty(dcMeters,   "transparencySoftwareStatus", null, new JArray(new TransparencySoftwareAssignment(chargyId, LegalStatus.GermanCalibrationLaw).ToJSON(Embedded: true))) ],
                                    Description: ImmutableI18NString.Create(Languages.en, "The DC stations: PTB-2026-0042; their energy meters: PTB-2026-0017, Chargy 1.5.0 under the German calibration law")
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
    /// The app checks the station proof against the trust anchor of epoch 2, and shows who checks its meter: the release
    /// and its manufacturer, the certificates of the station and of the meter and their body, each compared with what it
    /// is for.
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

        var bodies   = proof.Given(RoamingNetwork.PropertyKeys.ConformityAssessmentBodies);

        // A certificate the proof gives, with the body that issued it.
        (JObject Body, JObject Certificate) CertificateOf(String? Id)
        {
            var body = bodies.Single(json => proof.GivenBelow(json, ConformityAssessmentBody.PropertyKeys.Certificates).Any(certificate => certificate["@id"]?.Value<String>() == Id));
            return (body, proof.GivenBelow(body, ConformityAssessmentBody.PropertyKeys.Certificates).Single(json => json["@id"]?.Value<String>() == Id));
        }

        var certified = new List<String>();

        // The station and its meter, each a model of its manufacturer as the proof gives it, with the certificates they name.
        foreach (var (named, kind, makers, models, makerKey, modelKey) in new[] {
                     (station, "charging station", RoamingNetwork.PropertyKeys.ChargingStationManufacturers, ChargingStationManufacturer.PropertyKeys.Models, "chargingStationManufacturerId", "chargingStationModel"),
                     (meter,   "energy meter",     RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,     EnergyMeterManufacturer.    PropertyKeys.Models, "energyMeterManufacturerId",     "energyMeterModel") })
        {

            var maker     = proof.Given(makers).Single(json => json["@id"]?.Value<String>() == named["manufacturerId"]?.Value<String>());
            var model     = proof.GivenBelow(maker, models).Single(json => json["@id"]?.Value<String>() == named["modelId"]?.Value<String>());
            var revision  = named["hardwareRevision"]?.Value<String>();
            var firmware  = named["firmwareVersion"]?. Value<String>();

            Console.WriteLine($"The {kind} {named["@id"]}: a {model["@id"]} of {maker["@id"]}, {model["name"]?["en"]}, hardware {revision}, firmware {firmware}");

            foreach (var id in named["certificateIds"]?.Values<String>() ?? [])
            {

                var (body, cert) = CertificateOf(id);
                var components   = cert["verifiedTransparencySoftwareIds"]!.Values<String>().Select(release => $"{release} verified").
                                       Concat(cert["compatibleTransparencySoftwareIds"]!.Values<String>().Select(release => $"{release} compatible")).
                                       Concat((cert["energyMeters"] as JArray ?? []).Select(energyMeter => $"the {energyMeter["modelId"]} of {energyMeter["manufacturerId"]}, hardware {String.Join("/", energyMeter["hardwareRevisions"]!.Values<String>())}, firmware {String.Join("/", energyMeter["firmwareVersions"]!.Values<String>())}"));

                certified.Add(cert["@id"]!.ToString());

                Console.WriteLine($"  {cert["@id"]}{(cert["legislation"] is JToken legislation ? $", under the {legislation}" : "")}, by the {body["@id"]}, notified body {body["notifiedBodyNumber"]} for {String.Join(", ", body["notifiedFor"]!.Values<String>())}; signed by itself: {(body["signingKeys"] is JArray { Count: > 0 } ? "yes, it has keys of its own" : "no, the roaming network operator in its place")}");
                Console.WriteLine($"    for the {cert[modelKey]} of {cert[makerKey]}, hardware {String.Join("/", cert["hardwareRevisions"]!.Values<String>())}, firmware {String.Join("/", cert["firmwareVersions"]!.Values<String>())}: " +
                                  (cert[modelKey]?.Value<String>() == model["@id"]?.Value<String>() && cert[makerKey]?.Value<String>() == maker["@id"]?.Value<String>() &&
                                   cert["hardwareRevisions"]!.Values<String>().Contains(revision) && cert["firmwareVersions"]!.Values<String>().Contains(firmware)
                                       ? $"the model, hardware revision and firmware version of the {kind}"
                                       : "another model, hardware revision or firmware version"));
                Console.WriteLine($"    with {String.Join(", ", components)}");

            }

        }

        // The releases of the meter, with their legal status, which the certificates naming them among their components prove.
        foreach (var assignment in meter["transparencySoftwareStatus"]!.Cast<JObject>())
        {

            var releaseId  = assignment["transparencySoftwareId"]?.Value<String>();
            var makers     = proof.Given(RoamingNetwork.PropertyKeys.TransparencySoftwareManufacturers);
            var maker      = makers.Single(json => proof.GivenBelow(json, TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware).Any(release => release["@id"]?.Value<String>() == releaseId));
            var release    = proof.GivenBelow(maker, TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware).Single(json => json["@id"]?.Value<String>() == releaseId);
            var provenBy   = certified.Where(id => CertificateOf(id).Certificate is var cert &&
                                                   cert["verifiedTransparencySoftwareIds"]!.Values<String>().Concat(cert["compatibleTransparencySoftwareIds"]!.Values<String>()).Contains(releaseId));

            Console.WriteLine($"{meter["@id"]}, the energy meter of {ExampleA10.EVSEId}: {release["name"]!["en"]} {release["version"]}, {assignment["legalStatus"]}, as {String.Join(" and ", provenBy)} name it");
            Console.WriteLine($"  by {maker["name"]!["en"]}, {maker["craRole"]}, supported until {release["supportUntil"]}, vulnerabilities to {maker["vulnerabilityContact"]}, bill of materials {release["softwareBillOfMaterials"]}");

        }

        // The BSI is not given: neither the station nor its meter names a certificate of it.
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
