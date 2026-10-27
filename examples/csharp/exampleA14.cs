using System.Globalization;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP;
using cloud.charging.open.protocols.WWCP.POI;


public static class ExampleA14
{

    // The papers of a meter, digital: the QI-Digital documents. SAALE declares that its meter model SM-3 conforms to the
    // Measuring Instruments Directive, the PTB, its notified body, states the EU-type examination certificate of the model
    // and the approval of the quality system of SAALE, and a calibration laboratory states the calibration of one meter of
    // DE*GEF, the SM-3 of DE*GEF*E2*1. Each signs its document with its own key of QA. The documents are no part of QA:
    // their issuers hand them out, and anybody checks them against QA, its parties and their keys, the models of the
    // manufacturers and the meter as QA names it.


    internal static readonly ConformityAssessmentBody_Id        labId              = ConformityAssessmentBody_Id.      Parse("EM-LAB");
    internal static readonly EnergyMeterModel_Id                sm3                = EnergyMeterModel_Id.              Parse("SM-3");
    internal static readonly EVSE_Id                            EVSEId             = EVSE_Id.                          Parse("DE*GEF*E2*1");
    internal static readonly DigitalCertificateOfConformity_Id  typeExaminationId  = DigitalCertificateOfConformity_Id.Parse("DE-26-M-PTB-0108");
    internal static readonly DigitalCertificateOfConformity_Id  qualitySystemId    = DigitalCertificateOfConformity_Id.Parse("DE-26-QS-PTB-0042");
    internal static readonly DigitalDeclarationOfConformity_Id  declarationId      = DigitalDeclarationOfConformity_Id.Parse("DoC-SM-3-2026-01");
    internal static readonly DigitalCalibrationCertificate_Id   calibrationId      = DigitalCalibrationCertificate_Id. Parse("DCC-2026-0815");

    /// <summary>
    /// The EU-type examination certificate of the SM-3, as the PTB hands it out.
    /// </summary>
    internal const String CertificateOfConformity  = "published/PTB-DE-26-M-PTB-0108.json";

    /// <summary>
    /// The approval of the quality system of SAALE, as the PTB hands it out.
    /// </summary>
    internal const String QualitySystemApproval    = "published/PTB-DE-26-QS-PTB-0042.json";

    /// <summary>
    /// The EU declaration of conformity of the SM-3, as SAALE hands it out.
    /// </summary>
    internal const String DeclarationOfConformity  = "published/SAALE-DoC-SM-3-2026-01.json";

    /// <summary>
    /// The calibration certificate of the meter of DE*GEF*E2*1, as the laboratory hands it out.
    /// </summary>
    internal const String CalibrationCertificate   = "published/EM-LAB-DCC-2026-0815.json";


    public static void Run(ExampleA11.Result A11)
    {

        var now      = Timestamp.Now;
        var history  = A11.History;

        #region 1. The roaming network operator adds a calibration laboratory

        // EM-LAB calibrates energy meters, accredited for it, and is no notified body; its keys are of epoch 2, ML-DSA: one
        // for its certificates, one for itself.
        var labIdentityKey      = COSEKeyPair.NewMLDsa65();
        var labCertificatesKey  = COSEKeyPair.NewMLDsa65();
        var labPartyKey         = COSEKeyPair.NewMLDsa65();

        Directory.CreateDirectory("EM-LAB");
        Helpers.SaveKey(labIdentityKey.Private,      "EM-LAB/identity-privateKey.pem");
        Helpers.SaveKey(labCertificatesKey.Private,  "EM-LAB/signing-certificates-privateKey.pem");
        Helpers.SaveKey(labPartyKey.Private,         "EM-LAB/signing-party-privateKey.pem");

        var lab           = new ConformityAssessmentBody(
                                labId,
                                I18NString.Create(Languages.en, "Jena Energy Meter Calibration Laboratory"),
                                Homepage:      URL.Parse("https://www.em-lab.example"),
                                IdentityKeys:  [ POIPublicKey.From(labIdentityKey.Private) ],
                                SigningKeys:   [ POIPublicKey.From(labCertificatesKey.Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Certificates ]),
                                                 POIPublicKey.From(labPartyKey.       Private, NotAfter: now.AddYears(1), Usages: [ POIKeyUsages.Party        ]) ]
                            );

        // Who is a member is the roaming network operator's: two of its holders; the new identity key signs as the proof
        // that the laboratory holds it.
        var addLab        = history.Head.RoamingNetwork.CreateChangeSet(
                                [ RoamingNetworkChange.Add(lab) ],
                                Description: ImmutableI18NString.Create(Languages.en, "EM-LAB, a calibration laboratory")
                            );

        foreach (var key in new[] { A11.SigningKeys["AlphaOrg"], A11.SigningKeys["BetaOrg"], labIdentityKey })
            addLab        = addLab.Sign(key.Private, POIPublicKey.From(key.Private).Id, key.Algorithm);

        Committed(history, addLab);

        var qa = history.Head.RoamingNetwork;

        #endregion

        #region 2. The PTB states the EU-type examination certificate of the SM-3 and approves the quality system of SAALE

        // Module B under the MID: the model SM-3 of SAALE in its hardware revision 1.4 with the firmware 1.0.7, as SAALE
        // stated it in example A2; with the standards applied, the characteristics confirmed and the conditions of its
        // validity, for ten years.
        var typeExamination = new DigitalCertificateOfConformity(
                                  typeExaminationId,
                                  DCoCModule.B,
                                  QIDigitalLegislation.MID,
                                  ExampleA13.ptbId,
                                  "0102",
                                  QIDigitalModel.Of(ExampleA2.acMeterMakerId, sm3, [ HardwareRevision.Parse("1.4") ], [ FirmwareVersion.Parse("1.0.7") ]),
                                  ValidFrom:        now,
                                  ValidUntil:       now.AddYears(10),
                                  Standards:        [ new QIDigitalStandard("EN 50470-1:2006"), new QIDigitalStandard("EN 50470-3:2022") ],
                                  Characteristics:  [ new QIDigitalQuantity(63,    @"\ampere", RefType: "mid_maximumCurrent"),
                                                      new QIDigitalQuantity(0.5m,  @"\ampere", RefType: "mid_transitionalCurrent"),
                                                      new QIDigitalQuantity(0.25m, @"\ampere", RefType: "mid_minimumCurrent"),
                                                      new QIDigitalQuantity(230,   @"\volt",   RefType: "mid_referenceVoltage") ],
                                  Conclusion:       ImmutableI18NString.Create(Languages.en, "The type SM-3 meets the essential requirements of the MID, Annex I and Annex V (MI-003), accuracy class B."),
                                  Conditions:       [ ImmutableI18NString.Create(Languages.en, "Sealed as the annex of this certificate shows.") ],
                                  DocumentURL:      URL.Parse("https://www.ptb.example/certificates/DE-26-M-PTB-0108.pdf")
                              );

        // Signed by the key of the PTB for itself: refused, that key signs its own properties only. The PTB signs with its
        // key for its certificates, which it keeps since example A13.
        var byPartyKey      = Signed(typeExamination, "PTB/signing-party-privateKey.pem", COSEAlgorithm.MLDsa65);

        Console.WriteLine($"Signed by the key of the PTB for itself: {(byPartyKey.TryVerify(qa, out var error, now) ? "valid" : error)}");

        typeExamination     = Signed(typeExamination, "PTB/signing-certificates-privateKey.pem", COSEAlgorithm.MLDsa65);

        if (!typeExamination.TryVerify(qa, out error, now))
            Helpers.Fail(error);

        File.WriteAllText(CertificateOfConformity, typeExamination.ToJSON().ToString());

        Console.WriteLine($"The PTB: {typeExamination.Id}, module {typeExamination.Module} under the {typeExamination.Legislation.Name}, for the {typeExamination.Model!.ModelId} of {typeExamination.Model.ManufacturerId}, signed with {typeExamination.Signature!.Algorithm}");

        // Module D under the MID: the quality system of SAALE for the production, the final product inspection and the
        // testing of its active electrical energy meters, MI-003. The approval belongs to SAALE, not to the SM-3: it names
        // the manufacturer and the scope of its quality system, no model; valid for three years, under the surveillance of
        // the PTB.
        var qualitySystem   = new DigitalCertificateOfConformity(
                                  qualitySystemId,
                                  DCoCModule.D,
                                  QIDigitalLegislation.MID,
                                  ExampleA13.ptbId,
                                  "0102",
                                  QIDigitalManufacturer.Of(ExampleA2.acMeterMakerId, [ QIDigitalManufacturer.Categories.MI003 ]),
                                  ValidFrom:    now,
                                  ValidUntil:   now.AddYears(3),
                                  Conclusion:   ImmutableI18NString.Create(Languages.en, "The quality system of SAALE for production, final product inspection and testing meets the MID, Annex II, module D, for MI-003."),
                                  Conditions:   [ ImmutableI18NString.Create(Languages.en, "Subject to the surveillance of the notified body.") ],
                                  DocumentURL:  URL.Parse("https://www.ptb.example/certificates/DE-26-QS-PTB-0042.pdf")
                              );

        qualitySystem       = Signed(qualitySystem, "PTB/signing-certificates-privateKey.pem", COSEAlgorithm.MLDsa65);

        if (!qualitySystem.TryVerify(qa, out error, now))
            Helpers.Fail(error);

        File.WriteAllText(QualitySystemApproval, qualitySystem.ToJSON().ToString());

        Console.WriteLine($"The PTB: {qualitySystem.Id}, module {qualitySystem.Module} under the {qualitySystem.Legislation.Name}, for the quality system of {qualitySystem.Manufacturer!.ManufacturerId}, scope {String.Join(", ", qualitySystem.Manufacturer.Scope)}, signed with {qualitySystem.Signature!.Algorithm}");

        #endregion

        #region 3. SAALE declares the conformity of the SM-3

        // Under its sole responsibility: the model, the legislations, the standards, the notified body with its modules and
        // its certificates, where and by whom it is signed for SAALE.
        var declaration     = new DigitalDeclarationOfConformity(
                                  declarationId,
                                  QIDigitalModel.Of(ExampleA2.acMeterMakerId, sm3, [ HardwareRevision.Parse("1.4") ], [ FirmwareVersion.Parse("1.0.7") ]),
                                  [ QIDigitalLegislation.MID, QIDigitalLegislation.EMC, QIDigitalLegislation.RoHS ],
                                  "Halle (Saale)",
                                  new QIDigitalPerson("Jane Doe", "Managing director"),
                                  Standards:              [ new QIDigitalStandard("EN 50470-1:2006"), new QIDigitalStandard("EN 50470-3:2022") ],
                                  NotifiedBodies:         [ new DDoCNotifiedBody(ExampleA13.ptbId, "0102", DCoCModule.B, typeExaminationId,
                                                                                 ImmutableI18NString.Create(Languages.en, "Performed the EU-type examination and issued the EU-type examination certificate")),
                                                            new DDoCNotifiedBody(ExampleA13.ptbId, "0102", DCoCModule.D, qualitySystemId,
                                                                                 ImmutableI18NString.Create(Languages.en, "Approved the quality system of the manufacturer and surveils it")) ],
                                  AdditionalInformation:  ImmutableI18NString.Create(Languages.en, "Accuracy class B, three-phase, 3 x 230/400 V, 0.25 to 63 A.")
                              );

        // Signed by the roaming network operator: refused, the declaration is the manufacturer's. SAALE signs with its key
        // for its models, which it keeps since example A2.
        var alphaOrg        = A11.SigningKeys["AlphaOrg"];
        var byOCC           = declaration.Sign(alphaOrg.Private, POIPublicKey.From(alphaOrg.Private).Id, alphaOrg.Algorithm);

        Console.WriteLine($"Signed by the roaming network operator: {(byOCC.TryVerify(qa, out error, now) ? "valid" : error)}");

        declaration         = Signed(declaration, "SAALE/meters-signing-models-privateKey.pem", COSEAlgorithm.ES256);

        if (!declaration.TryVerify(qa, out error, now) || !declaration.TryVerify(typeExamination, out error) || !declaration.TryVerify(qualitySystem, out error))
            Helpers.Fail(error);

        File.WriteAllText(DeclarationOfConformity, declaration.ToJSON().ToString());

        Console.WriteLine($"SAALE: {declaration.Id}, the {declaration.Model.ModelId} under the {String.Join(", ", declaration.Legislations.Select(legislation => legislation.Name))}, naming {typeExaminationId} and {qualitySystemId}, signed with {declaration.Signature!.Algorithm}");

        #endregion

        #region 4. EM-LAB calibrates the meter of DE*GEF*E2*1

        // The meter as QA names it at runtime, with what its template gives it: its manufacturer, model, hardware revision
        // and firmware version, and its own serial number.
        if (!qa.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        if (!gef.TryGetEffectiveEVSE(EVSEId, out var evse) || evse.EnergyMeter is null)
            Helpers.Fail($"QA has no energy meter of '{EVSEId}'");

        var meter = evse.EnergyMeter;

        // The error of the active energy at four currents, each with its expanded uncertainty and the maximum permissible
        // error of class B (MID, Annex V): 1.5 % below the transitional current, 1 % from it on.
        static DCCResult LoadPoint(String Name, Decimal Current, Decimal Error, Decimal MaxPermissibleError)

            => new (ImmutableI18NString.Create(Languages.en, Name),
                    [ new QIDigitalQuantity(Current, @"\ampere",  RefType: "basic_nominalValue"),
                      new QIDigitalQuantity(Error,   @"\percent", RefType: "basic_measurementError",
                                            ExpandedUncertainty:  0.08m,
                                            CoverageFactor:       2,
                                            CoverageProbability:  0.95m,
                                            ToleranceLower:       -MaxPermissibleError,
                                            ToleranceUpper:        MaxPermissibleError,
                                            Conformity:           Math.Abs(Error) + 0.08m <= MaxPermissibleError ? QIDigitalConformity.Pass : QIDigitalConformity.Fail) ],
                    RefType: "basic_calibrationPoint");

        var calibration     = new DigitalCalibrationCertificate(
                                  calibrationId,
                                  labId,
                                  QIDigitalItem.Of(meter),
                                  now.AddHours(-3),
                                  now.AddHours(-1),
                                  [ new QIDigitalPerson("Dr. Erika Mustermann", "Head of laboratory", MainSigner: true),
                                    new QIDigitalPerson("Max Mustermann",       "Calibration engineer") ],
                                  [ new DCCMeasurementResult(ImmutableI18NString.Create(Languages.en, "Measurement error of the active energy"),
                                                             [ LoadPoint("Imin",  0.25m,  0.42m, 1.5m),
                                                               LoadPoint("Itr",   0.5m,  -0.18m, 1.0m),
                                                               LoadPoint("Iref",  5,      0.06m, 1.0m),
                                                               LoadPoint("Imax",  63,    -0.21m, 1.0m) ],
                                                             UsedMethods:          [ new QIDigitalStandard("EN 50470-3:2022", ImmutableI18NString.Create(Languages.en, "Accuracy test of static meters")) ],
                                                             InfluenceConditions:  [ new QIDigitalQuantity(23.1m, @"\degreecelsius", RefType: "basic_temperature", ExpandedUncertainty: 0.5m, CoverageFactor: 2) ]) ],
                                  PerformanceLocation:        DCCPerformanceLocation.Customer,
                                  CalibrationLaboratoryCode:  "D-K-12345-01-00",
                                  Customer:                   "GraphDefined Charging Solutions",
                                  OrderNumber:                "PO-2026-0815",
                                  Statements:                 [ new DCCStatement(ImmutableI18NString.Create(Languages.en, "The meter conforms to its accuracy class B."),
                                                                                 RefType:     "basic_conformity",
                                                                                 Norm:        "EN 50470-3:2022",
                                                                                 Conformity:  QIDigitalConformity.Pass) ],
                                  RecalibrationDue:           now.AddYears(8)
                              ).Sign(labCertificatesKey.Private, POIPublicKey.From(labCertificatesKey.Private).Id, labCertificatesKey.Algorithm);

        if (!calibration.TryVerify(qa, out error, now))
            Helpers.Fail(error);

        File.WriteAllText(CalibrationCertificate, calibration.ToJSON().ToString());

        Console.WriteLine($"EM-LAB: {calibration.Id}, the {calibration.Item.Text}, the {calibration.Item.ModelId} of {calibration.Item.ManufacturerId} of serial number {calibration.Item.SerialNumber}, signed with {calibration.Signature!.Algorithm}");
        Console.WriteLine();

        // What everybody may read now: QA with the laboratory, and the commit leading to it.
        File.WriteAllText($"published/{history.RoamingNetworkId}.json",        qa.ToJSON(IncludeETags: true).ToString());
        File.WriteAllText($"published/{history.RoamingNetworkId}-commit.json", history.Head.Commit.ToJSON().ToString());

        #endregion

    }


    /// <summary>
    /// Anybody checks the papers of the meter of DE*GEF*E2*1 against QA as published: each document read from its file,
    /// signed by a key of its issuer that may sign it; the declaration naming the certificates of conformity, each of which
    /// covers the meter the calibration certificate is for.
    /// </summary>
    public static void Validate()
    {

        #region 5. The papers of the meter, checked

        // a) QA as published: its key statement signed by the quorum the trust anchor of epoch 2 names, and the commit
        //    leading to it.
        if (!RoamingNetworkTrustAnchor.TryParse(File.ReadAllText("published/QA-trustAnchor-mldsa.txt"), out var anchor, out var error))
            Helpers.Fail(error);

        if (!RoamingNetwork.TryParse(Helpers.ReadJSON("published/QA.json"), out var qa, out error))
            Helpers.Fail(error);

        if (!RoamingNetworkCommit.TryParse(Helpers.ReadJSON("published/QA-commit.json"), out var commit, out error))
            Helpers.Fail(error);

        if (!qa.Operator!.KeyStatement!.TryVerify(anchor.Holders, qa.Operator.IdentityKeys, out _, out error, anchor.MinConsensus, Timestamp.Now))
            Helpers.Fail(error);

        if (qa.Id != anchor.Id || commit.RoamingNetworkId != qa.Id || !qa.MatchesCommitETags(commit.ETags))
            Helpers.Fail("the commit does not lead to the roaming network of the trust anchor");

        Console.WriteLine($"QA, revision {commit.Revision} of epoch {commit.Epoch}: checked against the trust anchor of epoch 2");

        // b) The certificates of conformity: each of a conformity assessment body of QA with its number, notified for its
        //    legislation, and valid now; the type examination for a model of its manufacturer, in revisions and versions
        //    it has; the approval of the quality system for a manufacturer of QA.
        var now = Timestamp.Now;

        if (!DigitalCertificateOfConformity.TryParse(File.ReadAllText(CertificateOfConformity), out var typeExamination, out error))
            Helpers.Fail(error);

        Console.WriteLine($"{typeExamination.Id}, by the {typeExamination.ConformityAssessmentBodyId}, notified body {typeExamination.NotifiedBodyNumber}: {(typeExamination.TryVerify(qa, out error, now) ? "valid" : error)}");
        Console.WriteLine($"  module {typeExamination.Module}, under the {typeExamination.Legislation}, for the {typeExamination.Model!.ModelId} of {typeExamination.Model.ManufacturerId}, hardware {String.Join("/", typeExamination.Model.HardwareRevisions)}, firmware {String.Join("/", typeExamination.Model.FirmwareVersions)}");
        Console.WriteLine($"  {typeExamination.Conclusion?.FirstText()}");

        if (!DigitalCertificateOfConformity.TryParse(File.ReadAllText(QualitySystemApproval), out var qualitySystem, out error))
            Helpers.Fail(error);

        Console.WriteLine($"{qualitySystem.Id}, by the {qualitySystem.ConformityAssessmentBodyId}, notified body {qualitySystem.NotifiedBodyNumber}: {(qualitySystem.TryVerify(qa, out error, now) ? "valid" : error)}");
        Console.WriteLine($"  module {qualitySystem.Module}, under the {qualitySystem.Legislation}, for the quality system of {qualitySystem.Manufacturer!.ManufacturerId}, scope {String.Join(", ", qualitySystem.Manufacturer.Scope)}, until {qualitySystem.ValidUntil?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"  {qualitySystem.Conclusion?.FirstText()} {qualitySystem.Conditions.FirstOrDefault()?.FirstText()}");

        // c) The declaration of conformity: of the manufacturer of its model, signed by its key for its models; and each
        //    certificate it names, picked by its identification, of the notified body it names with its module: the type
        //    examination for its model in all its revisions and versions, the approval for the quality system of its
        //    manufacturer.
        if (!DigitalDeclarationOfConformity.TryParse(File.ReadAllText(DeclarationOfConformity), out var declaration, out error))
            Helpers.Fail(error);

        Console.WriteLine($"{declaration.Id}, by {declaration.Model.ManufacturerId}, the manufacturer of the {declaration.Model.ModelId}: {(declaration.TryVerify(qa, out error, now) ? "valid" : error)}");
        Console.WriteLine($"  under the {String.Join(", ", declaration.Legislations)}; signed in {declaration.PlaceOfIssue} by {declaration.Signatory}");

        var certificates = new[] { typeExamination, qualitySystem }.ToDictionary(certificate => certificate.Id);

        foreach (var notifiedBody in declaration.NotifiedBodies)
        {

            var named = notifiedBody.CertificateId is DigitalCertificateOfConformity_Id certificateId && certificates.TryGetValue(certificateId, out var certificate)
                            ? declaration.TryVerify(certificate, out error) ? "the certificate it names" : error
                            : "no certificate at hand";

            Console.WriteLine($"  {notifiedBody.CertificateId} of the notified body {notifiedBody.NotifiedBodyNumber}, the {notifiedBody.ConformityAssessmentBodyId}, module {notifiedBody.Module}: {named}");

        }

        // d) The calibration certificate: of a laboratory of QA, signed by its key for certificates; its item the meter as QA
        //    names it at runtime, its measurement results within their limits, and its meter one both certificates of
        //    conformity cover.
        if (!DigitalCalibrationCertificate.TryParse(File.ReadAllText(CalibrationCertificate), out var calibration, out error))
            Helpers.Fail(error);

        Console.WriteLine($"{calibration.Id}, by {calibration.CalibrationLaboratoryId}, {calibration.CalibrationLaboratoryCode}: {(calibration.TryVerify(qa, out error, now) ? "valid" : error)}");
        Console.WriteLine($"  the {calibration.Item.Text}, the {calibration.Item.ModelId} of {calibration.Item.ManufacturerId} of serial number {calibration.Item.SerialNumber}, hardware {calibration.Item.HardwareRevision}, firmware {calibration.Item.FirmwareVersion}: as QA names it");

        foreach (var measurement in calibration.MeasurementResults)
            foreach (var result in measurement.Results)
            {

                var current  = result.Quantities.Single(quantity => quantity.RefType == "basic_nominalValue");
                var measured = result.Quantities.Single(quantity => quantity.RefType == "basic_measurementError");

                Console.WriteLine($"  {result.Name.FirstText(),-4}  {Number(current.Value) + " A",-6}  {Number(measured.Value, "+0.00;-0.00")} % ± {Number(measured.ExpandedUncertainty!.Value, "0.00")} % (k = {measured.CoverageFactor}), limits ± {Number(measured.ToleranceUpper!.Value, "0.0")} %: {measured.Conformity}");

            }

        Console.WriteLine($"  passes: {calibration.Passes}; recalibration due {calibration.RecalibrationDue?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}; signed by {calibration.MainSigner}");
        Console.WriteLine($"  {typeExamination.Id} covers it: {(typeExamination.TryCover(calibration.Item, out error) ? "its model, hardware revision and firmware version" : error)}");
        Console.WriteLine($"  {qualitySystem.Id} covers it: {(qualitySystem.TryCover(calibration.Item, out error) ? $"an energy meter of {qualitySystem.Manufacturer.ManufacturerId}, of {String.Join(", ", qualitySystem.Manufacturer.Scope)}" : error)}");

        #endregion

        #region 6. What is refused

        // a) The calibration certificate with another error at Imax, changed after it was signed.
        var changed = Helpers.ReadJSON(CalibrationCertificate);
        changed["measurementResults"]![0]!["results"]![3]!["quantities"]![1]!["value"] = -0.12m;

        Console.WriteLine($"Another error at Imax: {(DigitalCalibrationCertificate.TryParse(changed, out var changedCalibration, out error) && changedCalibration.TryVerify(qa, out error, now) ? "valid" : error)}");

        // b) The certificates of conformity for the DC meter of DE*GEF*E1*1, a KDM-500 of KERNBERG.
        if (!qa.TryGetChargingStationOperatorById(ExampleA2.gefId, out var gef))
            Helpers.Fail($"QA has no charging station operator '{ExampleA2.gefId}'");

        if (!gef.TryGetEffectiveEVSE(ExampleA10.EVSEId, out var evse) || evse.EnergyMeter is null)
            Helpers.Fail($"QA has no energy meter of '{ExampleA10.EVSEId}'");

        Console.WriteLine($"{typeExamination.Id} for the meter of {ExampleA10.EVSEId}: {(typeExamination.TryCover(QIDigitalItem.Of(evse.EnergyMeter), out error) ? "covered" : error)}");
        Console.WriteLine($"{qualitySystem.Id} for the meter of {ExampleA10.EVSEId}: {(qualitySystem.  TryCover(QIDigitalItem.Of(evse.EnergyMeter), out error) ? "covered" : error)}");

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

    // The given document signed by its issuer with the private key it keeps in the given file.
    private static TDocument Signed<TDocument>(AQIDigitalDocument<TDocument> Document, String KeyFile, COSEAlgorithm Algorithm)

        where TDocument : AQIDigitalDocument<TDocument>

    {
        var key = Helpers.ReadKey(KeyFile);
        return Document.Sign(key, POIPublicKey.From(key).Id, Algorithm);
    }

    // A number as the documents write it, whatever the culture of the machine.
    private static String Number(Decimal Value, String Format = "0.####")
        => Value.ToString(Format, CultureInfo.InvariantCulture);

    #endregion

}
