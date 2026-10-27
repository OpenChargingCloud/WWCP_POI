/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The QI-Digital documents: digital calibration certificates of single charging stations and energy meters, digital
    /// declarations and certificates of conformity of their models, signed by their issuers with their signing keys and
    /// checked against the roaming network.
    /// </summary>
    [TestFixture]
    public class QIDigitalTests
    {

        #region Data

        private static readonly DateTimeOffset                  Created    = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset                  T0         = DateTimeOffset.Parse("2026-10-26T09:00:00Z");

        private static readonly ChargingStationOperator_Id      GEF        = ChargingStationOperator_Id.    Parse("DE*GEF");
        private static readonly ChargingStation_Id              S1         = ChargingStation_Id.            Parse("DE*GEF*S1");
        private static readonly EnergyMeter_Id                  Meter1     = EnergyMeter_Id.                Parse("meter-1");
        private static readonly ChargingStationManufacturer_Id  ACME       = ChargingStationManufacturer_Id.Parse("ACME");
        private static readonly EnergyMeterManufacturer_Id      METERS     = EnergyMeterManufacturer_Id.    Parse("METERS");
        private static readonly EnergyMeterManufacturer_Id      OTHERS     = EnergyMeterManufacturer_Id.    Parse("OTHERS");
        private static readonly ChargingStationModel_Id         DC300      = ChargingStationModel_Id.       Parse("DC-300");
        private static readonly EnergyMeterModel_Id             M500       = EnergyMeterModel_Id.           Parse("M-500");
        private static readonly ConformityAssessmentBody_Id     PTB        = ConformityAssessmentBody_Id.   Parse("PTB");
        private static readonly ConformityAssessmentBody_Id     LAB        = ConformityAssessmentBody_Id.   Parse("EM-LAB");

        private static readonly HardwareRevision                R30        = HardwareRevision.Parse("3.0");
        private static readonly FirmwareVersion                 F17        = FirmwareVersion. Parse("1.7");
        private static readonly FirmwareVersion                 F18        = FirmwareVersion. Parse("1.8");

        private static readonly DigitalCertificateOfConformity_Id  TypeExamination  = DigitalCertificateOfConformity_Id.Parse("DE-26-M-PTB-0042");
        private static readonly EnergyMeterTemplate_Id             MeterTemplate    = EnergyMeterTemplate_Id.Parse("DE*GEF*EMT_M500");

        #endregion

        #region NewNetwork(MeterOfTemplate = false)

        /// <summary>
        /// The network of the tests; its energy meter names its manufacturer, model, hardware revision and firmware version
        /// itself, or takes them from an energy meter template of its operator.
        /// </summary>
        private static RoamingNetwork NewNetwork(Boolean MeterOfTemplate = false)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                       Created,
                    ChargingStationManufacturers:  [ new ChargingStationManufacturer(ACME,
                                                                                     Models:   [ new ChargingStationModel(DC300,
                                                                                                                          HardwareRevisions: [ HardwareRevision.Parse("1.1") ],
                                                                                                                          FirmwareVersions:  [ FirmwareVersion. Parse("4.3") ]) ],
                                                                                     Created:  Created) ],
                    EnergyMeterManufacturers:      [ new EnergyMeterManufacturer(METERS,
                                                                                 Models:   [ new EnergyMeterModel(M500,
                                                                                                                  HardwareRevisions: [ R30 ],
                                                                                                                  FirmwareVersions:  [ F17, F18 ],
                                                                                                                  AccuracyClass:     "B") ],
                                                                                 Created:  Created),
                                                     new EnergyMeterManufacturer(OTHERS, Created: Created) ],
                    ConformityAssessmentBodies:    [ new ConformityAssessmentBody(PTB,
                                                                                  I18NString.Create(Languages.en, "PTB"),
                                                                                  NotifiedBodyNumber:  "0102",
                                                                                  NotifiedFor:         [ ConformityAssessmentBody.Legislations.MID ],
                                                                                  Created:             Created),
                                                     new ConformityAssessmentBody(LAB,
                                                                                  I18NString.Create(Languages.en, "EM test laboratory"),
                                                                                  Created:             Created) ],
                    ChargingStationOperators:      [ new ChargingStationOperator(GEF,
                                                                                 Created:               Created,
                                                                                 EnergyMeterTemplates:  MeterOfTemplate
                                                                                                            ? [ new EnergyMeterTemplate(MeterTemplate,
                                                                                                                                        ManufacturerId:    METERS,
                                                                                                                                        ModelId:           M500,
                                                                                                                                        HardwareRevision:  R30,
                                                                                                                                        FirmwareVersion:   F17,
                                                                                                                                        Created:           Created) ]
                                                                                                            : null,
                                                                                 ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                    Created:           Created,
                                                                                                                    ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                             ManufacturerId:    ACME,
                                                                                                                                                             ModelId:           DC300,
                                                                                                                                                             HardwareRevision:  HardwareRevision.Parse("1.1"),
                                                                                                                                                             FirmwareVersion:   FirmwareVersion. Parse("4.3"),
                                                                                                                                                             Created:           Created,
                                                                                                                                                             EVSEs:             [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"),
                                                                                                                                                                                           EnergyMeter:  MeterOfTemplate
                                                                                                                                                                                                             ? new EnergyMeter(Meter1,
                                                                                                                                                                                                                               SerialNumber:      SerialNumber.Parse("M500-000815"),
                                                                                                                                                                                                                               TemplateIds:       [ MeterTemplate ],
                                                                                                                                                                                                                               Created:           Created)
                                                                                                                                                                                                             : new EnergyMeter(Meter1,
                                                                                                                                                                                                                               ManufacturerId:    METERS,
                                                                                                                                                                                                                               ModelId:           M500,
                                                                                                                                                                                                                               SerialNumber:      SerialNumber.Parse("M500-000815"),
                                                                                                                                                                                                                               HardwareRevision:  R30,
                                                                                                                                                                                                                               FirmwareVersion:   F17,
                                                                                                                                                                                                                               Created:           Created),
                                                                                                                                                                                           Created:      Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Signed(Document, Party) helpers

        private static DigitalCalibrationCertificate   SignedBy(DigitalCalibrationCertificate   Certificate, POIPartyId Party)
        {
            var (privateKey, publicKey) = TestParties.Key(Party);
            return Certificate.Sign(privateKey, publicKey.Id, COSEAlgorithm.ES256, T0);
        }

        private static DigitalDeclarationOfConformity  SignedBy(DigitalDeclarationOfConformity  Declaration, POIPartyId Party)
        {
            var (privateKey, publicKey) = TestParties.Key(Party);
            return Declaration.Sign(privateKey, publicKey.Id, COSEAlgorithm.ES256, T0);
        }

        private static DigitalCertificateOfConformity  SignedBy(DigitalCertificateOfConformity  Certificate, POIPartyId Party)
        {
            var (privateKey, publicKey) = TestParties.Key(Party);
            return Certificate.Sign(privateKey, publicKey.Id, COSEAlgorithm.ES256, T0);
        }

        #endregion

        #region LoadPoint(Name, Current, Error)

        /// <summary>
        /// The measurement error of the meter at the given current, with the maximum permissible errors of class B.
        /// </summary>
        private static DCCResult LoadPoint(String   Name,
                                           Decimal  Current,
                                           Decimal  Error)

            => new (ImmutableI18NString.Create(Languages.en, Name),
                    [ new QIDigitalQuantity(Current, @"\ampere",  RefType: "basic_nominalValue"),
                      new QIDigitalQuantity(Error,   @"\percent", RefType: "basic_measurementError",
                                            ExpandedUncertainty:  0.08m,
                                            CoverageFactor:       2,
                                            CoverageProbability:  0.95m,
                                            ToleranceLower:       -1.0m,
                                            ToleranceUpper:        1.0m,
                                            Conformity:           Math.Abs(Error) + 0.08m <= 1.0m ? QIDigitalConformity.Pass : QIDigitalConformity.Fail) ],
                    RefType: "basic_calibrationPoint");

        #endregion

        #region NewDCC(Network, Item = null, Error = -0.12)

        private static DigitalCalibrationCertificate NewDCC(RoamingNetwork  Network,
                                                            QIDigitalItem?  Item    = null,
                                                            Decimal         Error   = -0.12m)
        {

            Network.TryGetEnergyMeterById(Meter1, out var meter);

            return new DigitalCalibrationCertificate(
                       DigitalCalibrationCertificate_Id.Parse("DCC-2026-4711"),
                       LAB,
                       Item ?? QIDigitalItem.Of(meter!),
                       DateTimeOffset.Parse("2026-10-20T08:00:00Z"),
                       DateTimeOffset.Parse("2026-10-20T11:30:00Z"),
                       [ new QIDigitalPerson("Dr. Erika Mustermann", "Head of laboratory", MainSigner: true),
                         new QIDigitalPerson("Max Mustermann",       "Test engineer") ],
                       [ new DCCMeasurementResult(ImmutableI18NString.Create(Languages.en, "Measurement error of the active energy"),
                                                  [ LoadPoint("Imin",  5,   0.31m),
                                                    LoadPoint("Itr",  25,  -0.04m),
                                                    LoadPoint("Imax", 500, Error) ],
                                                  UsedMethods:          [ new QIDigitalStandard("EN 50470-3:2022", ImmutableI18NString.Create(Languages.en, "Accuracy test of static meters")) ],
                                                  InfluenceConditions:  [ new QIDigitalQuantity(23.1m, @"\degreecelsius", RefType: "basic_temperature", ExpandedUncertainty: 0.5m, CoverageFactor: 2) ]) ],
                       CountryCode:                "DE",
                       PerformanceLocation:        DCCPerformanceLocation.Customer,
                       CalibrationLaboratoryCode:  "D-K-15071-01-00",
                       Customer:                   "GraphDefined Charging",
                       OrderNumber:                "PO-2026-0042",
                       Statements:                 [ new DCCStatement(ImmutableI18NString.Create(Languages.en, "The meter conforms to its accuracy class B."),
                                                                      RefType:     "basic_conformity",
                                                                      Norm:        "EN 50470-3:2022",
                                                                      Conformity:  QIDigitalConformity.Pass) ],
                       RecalibrationDue:           DateTimeOffset.Parse("2034-12-31T23:59:59Z")
                   );

        }

        #endregion

        #region NewDCoC(Module = B, FirmwareVersions = 1.7, 1.8)

        private static DigitalCertificateOfConformity NewDCoC(FirmwareVersion[]?  FirmwareVersions   = null,
                                                              String              NotifiedBodyNumber = "0102",
                                                              QIDigitalLegislation? Legislation      = null)

            => new (TypeExamination,
                    DCoCModule.B,
                    Legislation ?? QIDigitalLegislation.MID,
                    PTB,
                    NotifiedBodyNumber,
                    QIDigitalModel.Of(METERS, M500, [ R30 ], FirmwareVersions ?? [ F17, F18 ]),
                    ValidFrom:        DateTimeOffset.Parse("2026-03-01T00:00:00Z"),
                    ValidUntil:       DateTimeOffset.Parse("2036-02-28T23:59:59Z"),
                    Revision:         1,
                    Standards:        [ new QIDigitalStandard("EN 50470-3:2022"), new QIDigitalStandard("PTB-A 50.7") ],
                    Characteristics:  [ new QIDigitalQuantity(500, @"\ampere", RefType: "mid_maximumCurrent"),
                                        new QIDigitalQuantity(5,   @"\ampere", RefType: "mid_minimumCurrent"),
                                        new QIDigitalQuantity(920, @"\volt",   RefType: "mid_ratedVoltage") ],
                    Conclusion:       ImmutableI18NString.Create(Languages.en, "The type M-500 meets the essential requirements of the MID, accuracy class B."),
                    Conditions:       [ ImmutableI18NString.Create(Languages.en, "Only with a transparency software of PTB-A 50.7.") ]);

        #endregion

        #region NewDDoC(NotifiedBodies = PTB)

        private static DigitalDeclarationOfConformity NewDDoC(FirmwareVersion[]? FirmwareVersions = null)

            => new (DigitalDeclarationOfConformity_Id.Parse("DoC-M500-2026-01"),
                    QIDigitalModel.Of(METERS, M500, [ R30 ], FirmwareVersions ?? [ F17 ]),
                    [ QIDigitalLegislation.MID, QIDigitalLegislation.EMC, QIDigitalLegislation.RoHS ],
                    "Jena",
                    new QIDigitalPerson("Jane Doe", "Managing director"),
                    Standards:              [ new QIDigitalStandard("EN 50470-1:2022"), new QIDigitalStandard("EN 50470-3:2022") ],
                    NotifiedBodies:         [ new DDoCNotifiedBody(PTB, "0102", DCoCModule.B, TypeExamination,
                                                                   ImmutableI18NString.Create(Languages.en, "EU-type examination")) ],
                    AdditionalInformation:  ImmutableI18NString.Create(Languages.en, "Accuracy class B."));

        #endregion


        #region A_DCC_is_signed_by_its_calibration_laboratory_and_matches_its_meter()

        [Test]
        public void A_DCC_is_signed_by_its_calibration_laboratory_and_matches_its_meter()
        {

            var network  = NewNetwork();
            var dcc      = SignedBy(NewDCC(network), TestParties.Party(LAB));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dcc.TryVerify(network, out var error, T0),  Is.True, error);
                Assert.That(dcc.Passes,                                 Is.True);
                Assert.That(dcc.MainSigner?.Name,                       Is.EqualTo("Dr. Erika Mustermann"));
                Assert.That(dcc.IsDueAt(T0),                            Is.False);
                Assert.That(dcc.IsDueAt(dcc.RecalibrationDue!.Value),   Is.True);
                Assert.That(dcc.Item.SerialNumber,                      Is.EqualTo(SerialNumber.Parse("M500-000815")));
                Assert.That(dcc.Signature!.SignedAt,                    Is.EqualTo(T0));
                Assert.That(dcc.Digest,                                 Does.Match("^[0-9a-f]{64}$"));
            }

        }

        #endregion

        #region A_DCC_reads_back_what_it_writes_and_its_signature_with_it()

        [Test]
        public void A_DCC_reads_back_what_it_writes_and_its_signature_with_it()
        {

            var network  = NewNetwork();
            var dcc      = SignedBy(NewDCC(network), TestParties.Party(LAB));
            var text     = dcc.ToJSON().ToString(Formatting.Indented);

            Assert.That(DigitalCalibrationCertificate.TryParse(text, out var parsed, out var parseError), Is.True, parseError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(JToken.DeepEquals(parsed!.ToJSON(), dcc.ToJSON()),  Is.True);
                Assert.That(parsed.Digest,                                      Is.EqualTo(dcc.Digest));
                Assert.That(parsed.TryVerify(network, out var error, T0),       Is.True, error);
                Assert.That(text,                                               Does.Contain("\"@context\": \"https://open.charging.cloud/contexts/wwcp/qiDigital/digitalCalibrationCertificate/v1\""));
                Assert.That(text,                                               Does.Contain("\"value\": -0.12"));
                Assert.That(text,                                               Does.Contain("\"coverageProbability\": 0.95"));
            }

        }

        #endregion

        #region A_changed_or_foreign_signed_DCC_is_refused()

        [Test]
        public void A_changed_or_foreign_signed_DCC_is_refused()
        {

            var network  = NewNetwork();
            var dcc      = SignedBy(NewDCC(network), TestParties.Party(LAB));

            // The measurement error at Imax changed after signing.
            var json     = dcc.ToJSON();
            json["measurementResults"]![0]!["results"]![2]!["quantities"]![1]!["value"] = 0.12m;

            Assert.That(DigitalCalibrationCertificate.TryParse(json, out var changed, out var parseError), Is.True, parseError);

            var byPTB    = SignedBy(NewDCC(network), TestParties.Party(PTB));
            var unsigned = NewDCC(network);
            var (_, labKey) = TestParties.Key(TestParties.Party(LAB));
            var infraKey = new POIPublicKey(labKey.Key, Usages: [ POIKeyUsages.Infrastructure ]);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(changed!.TryVerify(network, out var error1),       Is.False);
                Assert.That(error1,                                            Does.Contain("The signature of the digital calibration certificate 'DCC-2026-4711' is invalid"));

                Assert.That(byPTB.TryVerify(network, out var error2),          Is.False);
                Assert.That(error2,                                            Does.Contain("is no signing key of the calibration laboratory 'EM-LAB'"));

                Assert.That(unsigned.TryVerify(network, out var error3),       Is.False);
                Assert.That(error3,                                            Does.Contain("is not signed"));

                Assert.That(dcc.TryVerify([ infraKey ], out var error4),       Is.False);
                Assert.That(error4,                                            Does.Contain("may not sign 'certificates'"));

            }

        }

        #endregion

        #region A_DCC_of_a_meter_changed_since_is_refused()

        [Test]
        public void A_DCC_of_a_meter_changed_since_is_refused()
        {

            var network      = NewNetwork();
            network.TryGetEnergyMeterById(Meter1, out var meter);

            var otherMeter   = new EnergyMeter(Meter1, ManufacturerId: METERS, ModelId: M500, SerialNumber: SerialNumber.Parse("M500-000815"), HardwareRevision: R30, FirmwareVersion: F18, Created: Created);
            var newFirmware  = SignedBy(NewDCC(network, QIDigitalItem.Of(otherMeter)), TestParties.Party(LAB));
            var unknown      = SignedBy(NewDCC(network, QIDigitalItem.Of(new EnergyMeter(EnergyMeter_Id.Parse("meter-9"), Created: Created))), TestParties.Party(LAB));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(newFirmware.TryVerify(network, out var error1),  Is.False);
                Assert.That(error1,                                          Is.EqualTo("The energy meter 'meter-1' runs the firmware version '1.7', not '1.8'!"));

                Assert.That(unknown.TryVerify(network, out var error2),      Is.False);
                Assert.That(error2,                                          Does.Contain("The energy meter 'meter-9' is none of the roaming network 'rn1'"));

                Assert.That(NewDCC(network, Error: -0.95m).Passes,           Is.False);

            }

        }

        #endregion

        #region A_DCC_of_a_charging_station_matches_it_as_it_is_at_runtime()

        [Test]
        public void A_DCC_of_a_charging_station_matches_it_as_it_is_at_runtime()
        {

            var network = NewNetwork();
            network.TryGetChargingStationOperatorById(GEF, out var gef);
            gef!.TryGetEffectiveChargingStation(S1, out var station);

            var dcc     = SignedBy(NewDCC(network, QIDigitalItem.Of(station!, SerialNumber: SerialNumber.Parse("DC300-0001"))), TestParties.Party(LAB));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dcc.TryVerify(network, out var error, T0),  Is.True, error);
                Assert.That(dcc.Item.Type,                              Is.EqualTo(nameof(ChargingStation)));
                Assert.That(dcc.Item.FirmwareVersion,                   Is.EqualTo(FirmwareVersion.Parse("4.3")));
            }

        }

        #endregion

        #region A_DCC_of_an_energy_meter_matches_it_as_it_is_at_runtime()

        [Test]
        public void A_DCC_of_an_energy_meter_matches_it_as_it_is_at_runtime()
        {

            // The meter names only its serial number and its template; its manufacturer, model, hardware revision and
            // firmware version are the template's.
            var network = NewNetwork(MeterOfTemplate: true);
            network.TryGetChargingStationOperatorById(GEF, out var gef);
            gef!.TryGetEffectiveEVSE(EVSE_Id.Parse("DE*GEF*E1*1"), out var evse);

            var dcc     = SignedBy(NewDCC(network, QIDigitalItem.Of(evse!.EnergyMeter!)), TestParties.Party(LAB));
            var dcoc    = SignedBy(NewDCoC(), TestParties.Party(PTB));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dcc.TryVerify(network, out var error1, T0),  Is.True, error1);
                Assert.That(dcc.Item.ModelId,                            Is.EqualTo(M500.ToString()));
                Assert.That(dcc.Item.FirmwareVersion,                    Is.EqualTo(F17));
                Assert.That(dcoc.TryCover(dcc.Item, out var error2),     Is.True, error2);
            }

        }

        #endregion

        #region A_DCoC_of_a_notified_body_covers_its_model_and_the_meters_of_it()

        [Test]
        public void A_DCoC_of_a_notified_body_covers_its_model_and_the_meters_of_it()
        {

            var network  = NewNetwork();
            network.TryGetEnergyMeterById(Meter1, out var meter);

            var dcoc     = SignedBy(NewDCoC(), TestParties.Party(PTB));

            Assert.That(DigitalCertificateOfConformity.TryParse(dcoc.ToJSON().ToString(), out var parsed, out var parseError), Is.True, parseError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dcoc.  TryVerify(network, out var error1, T0),                       Is.True, error1);
                Assert.That(parsed!.TryVerify(network, out var error2, T0),                      Is.True, error2);
                Assert.That(parsed.Digest,                                                       Is.EqualTo(dcoc.Digest));
                Assert.That(dcoc.  TryCover(QIDigitalItem.Of(meter!), out var error3),           Is.True, error3);
                Assert.That(dcoc.  IsValidAt(DateTimeOffset.Parse("2036-03-01T00:00:00Z")),      Is.False);
                Assert.That(dcoc.  DocumentText,                                                 Is.EqualTo("the digital certificate of conformity 'DE-26-M-PTB-0042' revision 1"));
            }

        }

        #endregion

        #region A_DCoC_is_refused_where_its_body_does_not_fit()

        [Test]
        public void A_DCoC_is_refused_where_its_body_does_not_fit()
        {

            var network       = NewNetwork();

            var byLab         = SignedBy(NewDCoC(), TestParties.Party(LAB));
            var wrongNumber   = SignedBy(NewDCoC(NotifiedBodyNumber: "0103"), TestParties.Party(PTB));
            var underCRA      = SignedBy(NewDCoC(Legislation: QIDigitalLegislation.CRA), TestParties.Party(PTB));
            var newFirmware   = SignedBy(NewDCoC([ F17, FirmwareVersion.Parse("2.0") ]), TestParties.Party(PTB));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(byLab.      TryVerify(network, out var error1),                                    Is.False);
                Assert.That(error1,                                                                            Does.Contain("is no signing key of the conformity assessment body 'PTB'"));

                Assert.That(wrongNumber.TryVerify(network, out var error2),                                    Is.False);
                Assert.That(error2,                                                                            Does.Contain("is the notified body '0102', not '0103'"));

                Assert.That(underCRA.   TryVerify(network, out var error3),                                    Is.False);
                Assert.That(error3,                                                                            Does.Contain("is not notified for the CRA"));

                Assert.That(newFirmware.TryVerify(network, out var error4),                                    Is.False);
                Assert.That(error4,                                                                            Is.EqualTo("The firmware version '2.0' is none of the model 'M-500' of 'METERS'!"));

                Assert.That(SignedBy(NewDCoC(), TestParties.Party(PTB)).TryVerify(network, out var error5, DateTimeOffset.Parse("2036-03-01T00:00:00Z")), Is.False);
                Assert.That(error5,                                                                            Does.Contain("is not valid at 2036-03-01T00:00:00.000Z"));

            }

        }

        #endregion

        #region A_DDoC_of_its_manufacturer_names_the_DCoC_of_its_notified_body()

        [Test]
        public void A_DDoC_of_its_manufacturer_names_the_DCoC_of_its_notified_body()
        {

            var network  = NewNetwork();
            var ddoc     = SignedBy(NewDDoC(), TestParties.Party(METERS));
            var dcoc     = SignedBy(NewDCoC(), TestParties.Party(PTB));

            Assert.That(DigitalDeclarationOfConformity.TryParse(ddoc.ToJSON().ToString(), out var parsed, out var parseError), Is.True, parseError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ddoc.   TryVerify(network, out var error1, T0),  Is.True, error1);
                Assert.That(parsed!.TryVerify(network, out var error2, T0),  Is.True, error2);
                Assert.That(parsed. Digest,                                  Is.EqualTo(ddoc.Digest));
                Assert.That(ddoc.   TryVerify(dcoc, out var error3),         Is.True, error3);
                Assert.That(ddoc.   Legislations.Select(legislation => legislation.Name),
                                                                             Is.EqualTo(new[] { "MID", "EMC", "RoHS" }));
            }

        }

        #endregion

        #region A_DDoC_is_refused_where_it_does_not_fit()

        [Test]
        public void A_DDoC_is_refused_where_it_does_not_fit()
        {

            var network     = NewNetwork();
            var byOthers    = SignedBy(NewDDoC(), TestParties.Party(OTHERS));
            var newFirmware = SignedBy(NewDDoC([ F17, F18 ]), TestParties.Party(METERS));
            var narrowDCoC  = SignedBy(NewDCoC([ F17 ]),      TestParties.Party(PTB));
            var otherDCoC   = new DigitalCertificateOfConformity(DigitalCertificateOfConformity_Id.Parse("DE-26-M-PTB-0043"), DCoCModule.B, QIDigitalLegislation.MID, PTB, "0102",
                                                                 QIDigitalModel.Of(METERS, M500, [ R30 ], [ F17 ]), Created);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(byOthers.   TryVerify(network, out var error1),     Is.False);
                Assert.That(error1,                                             Does.Contain("is no signing key of the energy meter manufacturer 'METERS'"));

                Assert.That(newFirmware.TryVerify(network, out var error2),     Is.True, error2);
                Assert.That(newFirmware.TryVerify(narrowDCoC, out var error3),  Is.False);
                Assert.That(error3,                                             Does.Contain("does not cover the firmware version '1.8'"));

                Assert.That(newFirmware.TryVerify(otherDCoC, out var error4),   Is.False);
                Assert.That(error4,                                             Does.Contain("names no certificate 'DE-26-M-PTB-0043'"));

            }

        }

        #endregion

        #region Invalid_documents_are_refused()

        [Test]
        public void Invalid_documents_are_refused()
        {

            var network  = NewNetwork();
            var dcc      = NewDCC(network).ToJSON();

            JObject With(JObject JSON, String Property, JToken? Value)
            {
                var copy = (JObject) JSON.DeepClone();
                if (Value is null) copy.Remove(Property); else copy[Property] = Value;
                return copy;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(DigitalCalibrationCertificate.TryParse(With(dcc, "unsigned", "x"), out _, out var error1),                                  Is.False);
                Assert.That(error1,                                                                                                                      Is.EqualTo("Unknown property 'unsigned' of a digital calibration certificate!"));

                Assert.That(DigitalCalibrationCertificate.TryParse(With(dcc, "@context", DigitalCertificateOfConformity.JSONLDContext), out _, out var error2), Is.False);
                Assert.That(error2,                                                                                                                      Does.Contain("must be 'https://open.charging.cloud/contexts/wwcp/qiDigital/digitalCalibrationCertificate/v1'"));

                Assert.That(DigitalCalibrationCertificate.TryParse(With(dcc, "measurementResults", new JArray()), out _, out var error3),                Is.False);
                Assert.That(error3,                                                                                                                      Does.Contain("requires at least one measurement result"));

                Assert.That(DigitalCalibrationCertificate.TryParse(With(dcc, "beginPerformanceDate", "2026-10-20"), out _, out var error4),             Is.False);
                Assert.That(error4,                                                                                                                      Does.Contain("must be written as yyyy-MM-ddTHH:mm:ss.fffZ"));

                Assert.That(DigitalCalibrationCertificate.TryParse(With(dcc, "countryCode", "de"), out _, out _),                                       Is.False);

                Assert.That(() => new QIDigitalQuantity(1, @"\percent", CoverageFactor: 2),                                                             Throws.ArgumentException.With.Message.Contains("is one of an expanded uncertainty"));
                Assert.That(() => new QIDigitalQuantity(1, @"\percent", ExpandedUncertainty: 0.1m, CoverageProbability: 1.5m),                          Throws.ArgumentException.With.Message.Contains("at most 1"));
                Assert.That(() => QIDigitalModel.Of(METERS, M500, [ R30 ], []),                                                                         Throws.ArgumentException.With.Message.Contains("names at least one firmware version"));
                Assert.That(() => new DDoCNotifiedBody(PTB, "102"),                                                                                     Throws.ArgumentException.With.Message.Contains("four digits"));

                Assert.That(QIDigitalModel.TryParse(JObject.Parse("""{ "type": "EnergyMeterModel", "manufacturerId": "METERS", "modelId": "M-500", "hardwareRevisions": [ "3.0" ], "firmwareVersions": [ " 1.7" ] }"""), out _, out var error5), Is.False);
                Assert.That(error5,                                                                                                                      Is.EqualTo("Invalid firmware version ' 1.7'!"));

            }

        }

        #endregion

    }

}
