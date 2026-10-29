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

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Transparency software releases, embedded in their manufacturers, and their certificates embedded in the roaming
    /// network, assigned to energy meters with their legal status.
    /// </summary>
    [TestFixture]
    public class TransparencyTests
    {

        #region Data

        private static readonly DateTimeOffset                       Created     = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset                       T0          = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id           GEF         = ChargingStationOperator_Id.         Parse("DE*GEF");
        private static readonly ChargingStationManufacturer_Id       ACME        = ChargingStationManufacturer_Id.     Parse("ACME");
        private static readonly ChargingStationModel                 Wallbox22   = new (ChargingStationModel_Id.Parse("Wallbox-22"), HardwareRevisions: [ HardwareRevision.Parse("1.0") ], FirmwareVersions: [ FirmwareVersion.Parse("1.0.0") ]);
        private static readonly TransparencySoftwareManufacturer_Id  OCC         = TransparencySoftwareManufacturer_Id.Parse("OpenChargingCloud");
        private static readonly TransparencySoftwareManufacturer_Id  Other       = TransparencySoftwareManufacturer_Id.Parse("Other");
        private static readonly TransparencySoftware_Id              Chargy10    = TransparencySoftware_Id.            Parse("chargy-1.0");
        private static readonly TransparencySoftware_Id              Chargy11    = TransparencySoftware_Id.            Parse("chargy-1.1");
        private static readonly ConformityCertificate_Id   Cert1       = ConformityCertificate_Id. Parse("cert-1");
        private static readonly ConformityAssessmentBody_Id          PTB         = ConformityAssessmentBody_Id.        Parse("PTB");
        private static readonly ConformityAssessmentBody_Id          BSI         = ConformityAssessmentBody_Id.        Parse("BSI");
        private static readonly EnergyMeter_Id                       Meter1      = EnergyMeter_Id.                     Parse("meter-1");

        #endregion

        #region NewRelease(Id, Version)

        private static TransparencySoftware NewRelease(TransparencySoftware_Id Id, String Version)

            => new (Id,
                    ImmutableI18NString.Create(Languages.en, "Chargy"),
                    TransparencySoftwareVersion.Parse(Version),
                    [ OpenSourceLicense.AGPL3 ],
                    SourceCodeRepository:     URL.Parse("https://github.com/OpenChargingCloud/ChargyDesktopApp"),
                    SupportUntil:             DateTimeOffset.Parse("2031-12-31T23:59:59.9999Z"),
                    DeclarationOfConformity:  URL.Parse("https://open.charging.cloud/chargy/" + Version + "/declarationOfConformity.pdf"),
                    SoftwareBillOfMaterials:  URL.Parse("https://open.charging.cloud/chargy/" + Version + "/sbom.cdx.json"));

        #endregion

        #region NewManufacturer(Id, Releases)

        private static TransparencySoftwareManufacturer NewManufacturer(TransparencySoftwareManufacturer_Id  Id,
                                                                        params TransparencySoftware[]        Releases)

            => new (Id,
                    I18NString.Create(Languages.en, "Open Charging Cloud"),
                    Homepage:                       URL.Parse("https://open.charging.cloud"),
                    CRARole:                        TransparencySoftwareManufacturer.CRARoles.OpenSourceSoftwareSteward,
                    VulnerabilityContact:           EMailAddress.Parse("security@open.charging.cloud"),
                    VulnerabilityReporting:         URL.Parse("https://open.charging.cloud/.well-known/security.txt"),
                    VulnerabilityDisclosurePolicy:  URL.Parse("https://open.charging.cloud/security/policy"),
                    SecurityAdvisories:             URL.Parse("https://github.com/OpenChargingCloud/ChargyDesktopApp/security/advisories"),
                    TransparencySoftware:           Releases,
                    Created:                        Created);

        #endregion

        #region NewCertificate(Id, Verified, Compatible)

        private static ConformityCertificate NewCertificate(ConformityCertificate_Id  Id,
                                                                      TransparencySoftware_Id[]           Verified,
                                                                      TransparencySoftware_Id[]           Compatible)

            => new (Id,
                    ACME,
                    Wallbox22.Id,
                    [ HardwareRevision.Parse("1.0") ],
                    [ FirmwareVersion. Parse("1.0.0") ],
                    Verified,
                    Compatible,
                    DocumentNumber:                 "DE-123",
                    NotBefore:                      Created,
                    Legislation:                    ConformityAssessmentBody.Legislations.MID);

        #endregion

        #region NewBody(Id, NotifiedFor, Certificates)

        private static ConformityAssessmentBody NewBody(ConformityAssessmentBody_Id                Id,
                                                        String[]                                   NotifiedFor,
                                                        params ConformityCertificate[]   Certificates)

            => new (Id,
                    I18NString.Create(Languages.en, Id.ToString()),
                    NotifiedBodyNumber:                Id == PTB ? "0102" : null,
                    NotifiedFor:                       NotifiedFor,
                    Certificates:  Certificates,
                    Created:                           Created);

        #endregion

        #region NewNetwork(Assignments = null)

        private static RoamingNetwork NewNetwork(TransparencySoftwareAssignment[]? Assignments = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                           Created,
                    ChargingStationManufacturers:      [ new ChargingStationManufacturer(ACME, Models: [ Wallbox22 ], Created: Created) ],
                    TransparencySoftwareManufacturers: [ NewManufacturer(OCC, NewRelease(Chargy10, "1.0"), NewRelease(Chargy11, "1.1")) ],
                    ConformityAssessmentBodies:        [ NewBody(PTB, [ ConformityAssessmentBody.Legislations.MID ], NewCertificate(Cert1, [ Chargy10 ], [ Chargy11 ])) ],
                    ChargingStationOperators:          [ new ChargingStationOperator(GEF,
                                                                                     Created:        Created,
                                                                                     ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                        Created:           Created,
                                                                                                                        ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                 ManufacturerId:    ACME,
                                                                                                                                                                 ModelId:           Wallbox22.Id,
                                                                                                                                                                 HardwareRevision:  HardwareRevision.Parse("1.0"),
                                                                                                                                                                 FirmwareVersion:   FirmwareVersion.Parse("1.0.0"),
                                                                                                                                                                 CertificateIds:    [ Cert1 ],
                                                                                                                                                                 Created:  Created,
                                                                                                                                                                 EVSEs:    [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"),
                                                                                                                                                                                      EnergyMeter:  new EnergyMeter(Meter1,
                                                                                                                                                                                                                    TransparencySoftwareStatus:  Assignments ?? [
                                                                                                                                                                                                                        new TransparencySoftwareAssignment(Chargy10, LegalStatus.GermanCalibrationLaw),
                                                                                                                                                                                                                        new TransparencySoftwareAssignment(Chargy11, LegalStatus.ForInformationOnly)
                                                                                                                                                                                                                    ],
                                                                                                                                                                                                                    Created:                     Created),
                                                                                                                                                                                      Created:      Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Certificate(Network)

        private static JObject Certificate(JObject Network)
            => (JObject) Network["conformityAssessmentBodies"]![0]!["certificates"]![0]!;

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion

        #region SignedBy(Network, Party, Changes)

        private static RoamingNetworkChangeSet SignedBy(RoamingNetwork                  Network,
                                                        POIPartyId                      Party,
                                                        params RoamingNetworkChange[]   Changes)

            => Network.CreateChangeSet(Changes, CreatedAt: T0).SignedBy(Party);

        #endregion


        #region Transparency_software_survives_JSON_JSON_text_and_CBOR()

        [Test]
        public void Transparency_software_survives_JSON_JSON_text_and_CBOR()
        {

            var rn1           = NewNetwork();
            var json          = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var meter         = json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["energyMeter"]!;
            var manufacturer  = json["transparencySoftwareManufacturers"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                          Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                          Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                          Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                                   Is.EqualTo(rn1.CalcETags()));

                Assert.That(json["transparencySoftware"],                                           Is.Null);
                Assert.That(manufacturer["@id"]!.ToString(),                                        Is.EqualTo("OpenChargingCloud"));
                Assert.That(manufacturer["craRole"]!.ToString(),                                    Is.EqualTo("openSourceSoftwareSteward"));
                Assert.That(manufacturer["vulnerabilityContact"]!.ToString(),                       Is.EqualTo("security@open.charging.cloud"));
                Assert.That(manufacturer["vulnerabilityDisclosurePolicy"]!.ToString(),              Is.EqualTo("https://open.charging.cloud/security/policy"));
                Assert.That(manufacturer["signingKeys"]!.Count(),                                   Is.EqualTo(1));
                Assert.That(manufacturer["transparencySoftware"]!.Select(release => release["@id"]!.ToString()),  Is.EqualTo(new[] { "chargy-1.0", "chargy-1.1" }));
                Assert.That(manufacturer["transparencySoftware"]![0]!["vendor"],                    Is.Null);
                Assert.That(manufacturer["transparencySoftware"]![0]!["supportUntil"]!.ToString(),  Is.EqualTo("2031-12-31T23:59:59.999Z"));
                Assert.That(manufacturer["transparencySoftware"]![0]!["softwareBillOfMaterials"]!.ToString(),  Is.EqualTo("https://open.charging.cloud/chargy/1.0/sbom.cdx.json"));
                Assert.That(json["certificates"],                               Is.Null);
                Assert.That(json["conformityAssessmentBodies"]![0]!["notifiedBodyNumber"]!.ToString(),  Is.EqualTo("0102"));
                Assert.That(json["conformityAssessmentBodies"]![0]!["notifiedFor"]![0]!.ToString(),     Is.EqualTo("MID"));
                Assert.That(Certificate(json)["@id"]!.ToString(),                                   Is.EqualTo("cert-1"));
                Assert.That(Certificate(json)["issuer"],                                            Is.Null);
                Assert.That(Certificate(json)["legislation"]!.ToString(),                           Is.EqualTo("MID"));
                Assert.That(Certificate(json)["notBefore"]!.ToString(),                             Is.EqualTo("2026-01-01T00:00:00.000Z"));
                Assert.That(fromCBOR.ConformityAssessmentBodyOf(Cert1)?.Id,                         Is.EqualTo(PTB));
                Assert.That(meter["transparencySoftwareStatus"]![0]!["certificateId"],              Is.Null);
                Assert.That(json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["certificateIds"]![0]!.ToString(),  Is.EqualTo("cert-1"));
                Assert.That(meter["transparencySoftwareStatus"]![0]!["legalStatus"]!.ToString(),    Is.EqualTo(LegalStatus.GermanCalibrationLaw.ToString()));

                Assert.That(fromCBOR.TryGetTransparencySoftwareById(Chargy11, out var chargy11),    Is.True);
                Assert.That(chargy11!.Version.ToString(),                                           Is.EqualTo("1.1"));
                Assert.That(chargy11.SupportUntil,                                                  Is.EqualTo(DateTimeOffset.Parse("2031-12-31T23:59:59.999Z")));
                Assert.That(fromCBOR.TransparencySoftwareManufacturerOf(Chargy11)?.Id,             Is.EqualTo(OCC));
                Assert.That(fromJSON.TryGetConformityCertificateById(Cert1, out var cert1),  Is.True);
                Assert.That(cert1!.Covers(Chargy11),                                                Is.True);
                Assert.That(fromJSON.TryGetEnergyMeterById(Meter1, out var meter1),                 Is.True);
                Assert.That(meter1!.TransparencySoftwareStatus.Length,                              Is.EqualTo(2));

            }

        }

        #endregion

        #region Certificates_are_valid_to_the_millisecond()

        /// <summary>
        /// The validity of a certificate is content: written as every timestamp of the content, in UTC to the millisecond.
        /// </summary>
        [Test]
        public void Certificates_are_valid_to_the_millisecond()
        {

            var certificate  = new ConformityCertificate(Cert1,
                                                                   ACME,
                                                                   Wallbox22.Id,
                                                                   [ HardwareRevision.Parse("1.0") ],
                                                                   [ FirmwareVersion. Parse("1.0.0") ],
                                                                   [ Chargy10 ],
                                                                   [],
                                                                   NotBefore:  DateTimeOffset.Parse("2026-01-01T01:00:00.1234567+01:00"),
                                                                   NotAfter:   DateTimeOffset.Parse("2031-01-01T00:00:00Z"));

            var json         = certificate.ToJSON();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(json["notBefore"]!.ToString(),                       Is.EqualTo("2026-01-01T00:00:00.123Z"));
                Assert.That(json["notAfter"]!. ToString(),                       Is.EqualTo("2031-01-01T00:00:00.000Z"));
                Assert.That(certificate.NotBefore,                               Is.EqualTo(DateTimeOffset.Parse("2026-01-01T00:00:00.123Z")));
                Assert.That(ConformityCertificate.TryParse(json, out var parsed, out var error),  Is.True, error);
                Assert.That(parsed!.ToJSON().ToString(),                         Is.EqualTo(json.ToString()));
            }

        }

        #endregion

        #region Invalid_transparency_references_are_refused()

        [Test]
        public void Invalid_transparency_references_are_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            JObject Meter(JObject Network)
                => (JObject) Network["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["energyMeter"]!;

            JObject Station(JObject Network)
                => (JObject) Network["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!;

            JArray Releases(JObject Network)
                => (JArray) Network["transparencySoftwareManufacturers"]![0]!["transparencySoftware"]!;

            var unknownRelease      = Changed(network => Certificate(network)["compatibleTransparencySoftwareIds"] = new JArray("chargy-9.9"));
            var unknownManufacturer = Changed(network => Certificate(network)["chargingStationManufacturerId"] = "NOBODY");
            var issuer              = Changed(network => Certificate(network)["issuer"] = "PTB");
            var notNotified         = Changed(network => Certificate(network)["legislation"] = "CRA");
            var bodyNumber          = Changed(network => network["conformityAssessmentBodies"]![0]!["notifiedBodyNumber"] = "102");
            // .NET's "$" also matches before a final line feed: "0102\n" would be another number than "0102".
            var bodyNumberLineFeed  = Changed(network => network["conformityAssessmentBodies"]![0]!["notifiedBodyNumber"] = "0102\n");
            var supportUntil        = Changed(network => Releases(network)[0]!["supportUntil"] = Releases(network)[0]!["supportUntil"]!.Value<String>() + "\n");
            var meterRelease        = Changed(network => Releases(network).RemoveAt(1));
            var stationCertificate  = Changed(network => Station(network)["certificateIds"] = new JArray("cert-9"));
            var statusCertificate   = Changed(network => Meter(network)["transparencySoftwareStatus"]![0]!["certificateId"] = "cert-1");
            var twice               = Changed(network => Meter(network)["transparencySoftwareStatus"]![1]!["transparencySoftwareId"] = "chargy-1.0");
            var vendor              = Changed(network => Releases(network)[0]!["vendor"] = "GraphDefined GmbH");
            var role                = Changed(network => network["transparencySoftwareManufacturers"]![0]!["craRole"] = "importer");

            // The same release at another manufacturer as well: releases are unique within the network.
            var otherManufacturer   = Changed(network => ((JArray) network["transparencySoftwareManufacturers"]!).Add(new JObject(new JProperty("@id",                  "Other"),
                                                                                                                                       new JProperty("transparencySoftware", new JArray(Releases(network)[0]!.DeepClone())),
                                                                                                                                       new JProperty("created",              "2026-01-01T00:00:00.000Z"),
                                                                                                                                       new JProperty("lastChange",           "2026-01-01T00:00:00.000Z"))));


            using (Assert.EnterMultipleScope())
            {
                Assert.That(RoamingNetwork.TryParse(unknownRelease,       out _, out var error1),  Is.False);
                Assert.That(error1,                                                                Does.Contain("release 'chargy-9.9' of the certificate 'cert-1'"));
                Assert.That(RoamingNetwork.TryParse(unknownManufacturer,  out _, out var error2),  Is.False);
                Assert.That(error2,                                                                Does.Contain("manufacturer 'NOBODY' of the certificate"));
                Assert.That(RoamingNetwork.TryParse(meterRelease,         out _, out var error3),  Is.False);
                Assert.That(error3,                                                                Does.Contain("'chargy-1.1'"));
                Assert.That(RoamingNetwork.TryParse(stationCertificate,   out _, out var error4),  Is.False);
                Assert.That(error4,                                                                Does.Contain("The certificate 'cert-9' of the charging station 'DE*GEF*S1' is unknown in the roaming network"));
                Assert.That(RoamingNetwork.TryParse(statusCertificate,    out _, out var error5),  Is.False);
                Assert.That(error5,                                                                Does.Contain("A transparency software status names no certificateId"));
                Assert.That(RoamingNetwork.TryParse(twice,                out _, out var error6),  Is.False);
                Assert.That(error6,                                                                Does.Contain("must not be given twice"));
                Assert.That(RoamingNetwork.TryParse(vendor,               out _, out var error8),  Is.False);
                Assert.That(error8,                                                                Does.Contain("vendor"));
                Assert.That(RoamingNetwork.TryParse(role,                 out _, out var error9),  Is.False);
                Assert.That(error9,                                                                Does.Contain("'importer'"));
                Assert.That(RoamingNetwork.TryParse(otherManufacturer,    out _, out var error10), Is.False);
                Assert.That(error10,                                                               Does.Contain("release 'chargy-1.0' of the manufacturer 'Other' is one of the manufacturer 'OpenChargingCloud' already"));
                Assert.That(RoamingNetwork.TryParse(issuer,               out _, out var error11), Is.False);
                Assert.That(error11,                                                               Does.Contain("issuer"));
                Assert.That(RoamingNetwork.TryParse(notNotified,          out _, out var error12), Is.False);
                Assert.That(error12,                                                               Does.Contain("legislation 'CRA', which the conformity assessment body 'PTB' is not notified for"));
                Assert.That(RoamingNetwork.TryParse(bodyNumber,           out _, out var error13), Is.False);
                Assert.That(error13,                                                               Does.Contain("must be four digits"));
                Assert.That(RoamingNetwork.TryParse(bodyNumberLineFeed,   out _, out var error14), Is.False);
                Assert.That(error14,                                                               Does.Contain("must be four digits"));
                Assert.That(RoamingNetwork.TryParse(supportUntil,         out _, out var error15), Is.False);
                Assert.That(error15,                                                               Does.Contain("supportUntil: invalid timestamp"));
            }

        }

        #endregion

        #region Transparency_software_is_changed_by_change_sets()

        [Test]
        public void Transparency_software_is_changed_by_change_sets()
        {

            var rn1      = NewNetwork();
            var chargy20 = TransparencySoftware_Id.Parse("chargy-2.0");
            var cert1    = rn1.Certificates.Values.Single();
            rn1.TryGetTransparencySoftwareById(Chargy11, out var chargy11);

            var rn2      = Apply(rn1,
                                 RoamingNetworkChange.Add           (NewRelease(chargy20, "2.0"), OCC),
                                 RoamingNetworkChange.UpdateProperty(cert1,     "compatibleTransparencySoftwareIds", null, new JArray("chargy-1.1", "chargy-2.0")),
                                 RoamingNetworkChange.UpdateProperty(chargy11!, "supportUntil",                      null, "2027-12-31T23:59:59.000Z"));

            rn2.TryGetTransparencySoftwareById(Chargy11, out var changedChargy11);
            rn2.TryGetConformityCertificateById(Cert1, out var changedCert1);
            rn2.TryGetTransparencySoftwareManufacturerById(OCC, out var manufacturer);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.TransparencySoftware.Keys.Select(releaseId => releaseId.ToString()).Order(),  Is.EqualTo(new[] { "chargy-1.0", "chargy-1.1", "chargy-2.0" }));
                Assert.That(manufacturer!.TransparencySoftware.Select(release => release.Id.ToString()),      Is.EqualTo(new[] { "chargy-1.0", "chargy-1.1", "chargy-2.0" }));
                Assert.That(changedChargy11!.SupportUntil,                                              Is.EqualTo(DateTimeOffset.Parse("2027-12-31T23:59:59Z")));
                Assert.That(changedCert1!.Covers(chargy20),                                             Is.True);
                // Releases and certificates have no timestamps; the manufacturer and the body embedding them changed with them.
                Assert.That(rn2.ToJSON()["transparencySoftwareManufacturers"]![0]!["transparencySoftware"]![2]!["created"],  Is.Null);
                Assert.That(manufacturer.LastChangeDate,                                                Is.EqualTo(T0));
                Assert.That(rn2.TryGetConformityAssessmentBodyById(PTB, out var ptb) ? ptb.LastChangeDate : default(DateTimeOffset?),  Is.EqualTo(T0));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(Chargy10)),
                            Throws.ArgumentException.With.Message.Contains("release 'chargy-1.0' of the certificate 'cert-1'"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(Cert1)),
                            Throws.ArgumentException.With.Message.Contains("certificate 'cert-1' of the charging station 'DE*GEF*S1'"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(changedChargy11, "@id", null, "x")),
                            Throws.ArgumentException.With.Message.Contains("can not be changed"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(changedChargy11, "vendor", null, "GraphDefined GmbH")),
                            Throws.ArgumentException.With.Message.Contains("can not be changed"));

                // A release to add needs its manufacturer; its releases are no property of the manufacturer.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Add("TransparencySoftware", "chargy-3.0", NewRelease(TransparencySoftware_Id.Parse("chargy-3.0"), "3.0").ToJSON(Embedded: true))),
                            Throws.ArgumentException.With.Message.Contains("needs its TransparencySoftwareManufacturer"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(manufacturer, "transparencySoftware", null, new JArray())),
                            Throws.ArgumentException.With.Message.Contains("can not be changed"));

                // A manufacturer whose release a certificate names can not be removed.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(OCC)),
                            Throws.ArgumentException.With.Message.Contains("release 'chargy-1.0' of the certificate 'cert-1'"));

            }

        }

        #endregion

        #region A_release_keeps_its_version()

        /// <summary>
        /// A new version is a new release (SPECIFICATION.md 4.1, POI3-ISSUES.md S31): no change set changes the version of
        /// a release, also not by removing it and adding it again, so that a certificate naming a release names the
        /// version that was assessed. Its other properties change as before.
        /// </summary>
        [Test]
        public void A_release_keeps_its_version()
        {

            var rn1           = NewNetwork();
            var manufacturer  = TestParties.Party(OCC);

            rn1.TryGetTransparencySoftwareById(Chargy11, out var chargy11);

            var renamed       = NewRelease(Chargy11, "1.1").ToJSON(Embedded: true);
            renamed["name"]   = new JObject(new JProperty("en", "Chargy Desktop"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => SignedBy(rn1, manufacturer, RoamingNetworkChange.UpdateProperty(chargy11!, "version", null, "1.2")),
                            Throws.ArgumentException.With.Message.Contains("Change 0: the version of the transparency software release 'chargy-1.1' can not be changed: a new version is a new release!"));

                Assert.That(() => SignedBy(rn1, manufacturer, RoamingNetworkChange.RemoveProperty(chargy11!, "version", "1.1")),
                            Throws.ArgumentException.With.Message.Contains("can not be changed: a new version is a new release!"));

                Assert.That(() => SignedBy(rn1, manufacturer, RoamingNetworkChange.Remove(Chargy11),
                                                              RoamingNetworkChange.Add   (NewRelease(Chargy11, "1.2"), OCC)),
                            Throws.ArgumentException.With.Message.Contains("The version of the transparency software release 'chargy-1.1' can not be changed from '1.1' to '1.2': a new version is a new release!"));

                // Removed and added again with its version, it may change otherwise.
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, manufacturer, RoamingNetworkChange.Remove(Chargy11),
                                                                               RoamingNetworkChange.Add   ("TransparencySoftware", "chargy-1.1", renamed, nameof(TransparencySoftwareManufacturer), OCC.ToString())),
                                                  out var readded, out var readdError),                                   Is.True, readdError);
                Assert.That(readded!.TryGetTransparencySoftwareById(Chargy11, out var renamedChargy11),                 Is.True);
                Assert.That(renamedChargy11!.Name[Languages.en],                                                        Is.EqualTo("Chargy Desktop"));

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, manufacturer, RoamingNetworkChange.UpdateProperty(chargy11!, "supportUntil", null, "2027-12-31T23:59:59.000Z")),
                                                  out _, out var supportError),                                           Is.True, supportError);

            }

        }

        #endregion

        #region A_manufacturer_signs_its_releases_itself()

        /// <summary>
        /// A transparency software manufacturer is a party: the roaming network operator adds and removes it, with its keys
        /// and without its releases; it changes its own properties and its releases with its own keys, and only it.
        /// </summary>
        [Test]
        public void A_manufacturer_signs_its_releases_itself()
        {

            var rn1           = NewNetwork();
            var manufacturer  = TestParties.Party(OCC);
            var other         = TestParties.Party(Other);
            var rno           = TestParties.Party(rn1);
            var chargy20      = TransparencySoftware_Id.Parse("chargy-2.0");
            var addRelease    = RoamingNetworkChange.Add(NewRelease(chargy20, "2.0"), OCC);

            rn1.TryGetTransparencySoftwareManufacturerById(OCC, out var occ);

            var changeRole    = RoamingNetworkChange.UpdateProperty(occ!, "craRole", null, "manufacturer");

            // Another manufacturer, added by the roaming network operator with its keys: not with releases.
            var withRelease   = NewManufacturer(Other, NewRelease(TransparencySoftware_Id.Parse("other-1.0"), "1.0")).WithKeys();
            var withoutKeys   = NewManufacturer(Other);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, manufacturer, addRelease),  out var withNew, out var error1),  Is.True,  error1);
                Assert.That(withNew!.TryGetTransparencySoftwareById(chargy20, out _),                                         Is.True);

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, rno,          addRelease),  out _, out var error2),            Is.False, "a release signed by the roaming network operator");
                Assert.That(error2,                                                                                            Does.Contain("needs a signature by a signing key of the TransparencySoftwareManufacturer 'OpenChargingCloud'"));

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, manufacturer, changeRole),  out _, out var error3),            Is.True,  error3);
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, rno,          changeRole),  out _, out var error4),            Is.False, "its properties signed by the roaming network operator");

                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(withRelease)),
                            Throws.ArgumentException.With.Message.Contains("must be added without its transparencySoftware"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(withoutKeys)),
                            Throws.ArgumentException.With.Message.Contains("must be added with its signingKeys"));

                var withOther = Apply(rn1, RoamingNetworkChange.Add(NewManufacturer(Other).WithKeys()));

                // Removed by the roaming network operator, with its releases; by itself, not.
                Assert.That(withOther.TryApplyChangeSet(SignedBy(withOther, other, RoamingNetworkChange.Remove(Other)),  out _, out var error5),  Is.False, "removed by itself");
                Assert.That(error5,                                                                                            Does.Contain("roaming network operator"));
                Assert.That(withOther.TryApplyChangeSet(SignedBy(withOther, rno,   RoamingNetworkChange.Remove(Other)),  out _, out var error8),  Is.True,  error8);

                // A release of one manufacturer, signed by another one.
                Assert.That(withOther.TryApplyChangeSet(SignedBy(withOther, other, addRelease),  out _, out var error6),     Is.False, "a release of another manufacturer");
                Assert.That(withOther.TryApplyChangeSet(SignedBy(withOther, other, RoamingNetworkChange.UpdateProperty(NewRelease(Chargy11, "1.1"), "supportUntil", null, "2027-12-31T23:59:59.000Z")),
                                                        out _, out var error7),                                               Is.False, "a release of another manufacturer");

            }

        }

        #endregion

        #region A_conformity_assessment_body_signs_its_certificates_itself()

        /// <summary>
        /// A conformity assessment body is a party: the roaming network operator adds and removes it; it changes its
        /// certificates with its own keys. One not taking part is added without keys: then the roaming network operator
        /// signs in its place.
        /// </summary>
        [Test]
        public void A_conformity_assessment_body_signs_its_certificates_itself()
        {

            var rn1           = NewNetwork();
            var ptb           = TestParties.Party(PTB);
            var rno           = TestParties.Party(rn1);
            var cert2         = ConformityCertificate_Id.Parse("cert-2");
            var addCert2      = RoamingNetworkChange.Add(NewCertificate(cert2, [ Chargy11 ], []), PTB);

            // The BSI, for the CRA, not taking part yet: added without keys.
            var withBSI       = Apply(rn1, RoamingNetworkChange.Add(NewBody(BSI, [ ConformityAssessmentBody.Legislations.CRA ])));
            var craCert       = new ConformityCertificate(ConformityCertificate_Id.Parse("cra-1"),
                                                                    ACME,
                                                                    Wallbox22.Id,
                                                                    [ HardwareRevision.Parse("1.0") ],
                                                                    [ FirmwareVersion. Parse("1.0.0") ],
                                                                    [ Chargy11 ],
                                                                    [],
                                                                    Legislation:                     ConformityAssessmentBody.Legislations.CRA,
                                                                    StatedByRoamingNetworkOperator:  true);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, ptb, addCert2),  out var withCert2, out var error1),  Is.True,  error1);
                Assert.That(withCert2!.ConformityAssessmentBodyOf(cert2)?.Id,                                    Is.EqualTo(PTB));

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, rno, addCert2),  out _, out var error2),         Is.False, "a certificate of the PTB signed by the roaming network operator");
                Assert.That(error2,                                                                               Does.Contain("needs a signature by a signing key of the ConformityAssessmentBody 'PTB'"));

                // Not notified for the CRA: the PTB can not issue under it.
                Assert.That(() => SignedBy(rn1, ptb, RoamingNetworkChange.Add(craCert, PTB)),
                            Throws.ArgumentException.With.Message.Contains("legislation 'CRA', which the conformity assessment body 'PTB' is not notified for"));

                // The BSI without keys: the roaming network operator signs its certificates in its place, the BSI can not.
                Assert.That(withBSI.TryGetConformityAssessmentBodyById(BSI, out var bsi),                       Is.True);
                Assert.That(bsi!.SigningKeys,                                                                     Is.Empty);
                Assert.That(withBSI.TryApplyChangeSet(SignedBy(withBSI, rno, RoamingNetworkChange.Add(craCert, BSI)),  out _, out var error4),  Is.True, error4);

                // A certificate to add needs its body.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add("ConformityCertificate", "cert-3", NewCertificate(ConformityCertificate_Id.Parse("cert-3"), [ Chargy10 ], []).ToJSON(Embedded: true))),
                            Throws.ArgumentException.With.Message.Contains("needs its ConformityAssessmentBody"));

                // Added with its certificates, a body is refused: they are its own to add.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(NewBody(BSI, [ ConformityAssessmentBody.Legislations.CRA ], craCert))),
                            Throws.ArgumentException.With.Message.Contains("must be added without its certificates"));

                // A body whose certificate an energy meter names can not be removed.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Remove(PTB)),
                            Throws.ArgumentException.With.Message.Contains("certificate 'cert-1' of the charging station 'DE*GEF*S1'"));

            }

        }

        #endregion

        #region A_body_names_its_legislations_in_their_ordinal_order()

        /// <summary>
        /// The legislations a body is notified for are read as the content holds them, in their ordinal order
        /// (SPECIFICATION.md 2, Written forms): written otherwise they would be written again as another text than the one
        /// hashed (D13 of WWCPPOIConformanceTests, of keys). Ordinal: "MID" before "mid"; one given twice the constructor
        /// refuses.
        /// </summary>
        [Test]
        public void A_body_names_its_legislations_in_their_ordinal_order()
        {

            var json = NewBody(PTB, [ ConformityAssessmentBody.Legislations.CRA, ConformityAssessmentBody.Legislations.MID ]).ToJSON();

            JObject With(params String[] NotifiedFor)
            {
                var copy = (JObject) json.DeepClone();
                copy["notifiedFor"] = new JArray(NotifiedFor);
                return copy;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json["notifiedFor"]!.ToObject<String[]>(),                                   Is.EqualTo(new[] { "CRA", "MID" }));
                Assert.That(ConformityAssessmentBody.TryParse(json,                   out _, out var error), Is.True, error);
                Assert.That(ConformityAssessmentBody.TryParse(With("MID", "mid"),     out _, out error),     Is.True, error);

                Assert.That(ConformityAssessmentBody.TryParse(With("MID", "CRA"),     out _, out error),     Is.False);
                Assert.That(error,                                                                       Is.EqualTo($"The legislations the conformity assessment body '{PTB}' is notified for are written in their ordinal order!"));
                Assert.That(ConformityAssessmentBody.TryParse(With("mid", "MID"),     out _, out error),     Is.False);
                Assert.That(ConformityAssessmentBody.TryParse(With("MID", ""),        out _, out error),     Is.False);
                Assert.That(error,                                                                       Does.Contain("ordinal order"));
                Assert.That(ConformityAssessmentBody.TryParse(With("MID", "MID"),     out _, out error),     Is.False);
                Assert.That(error,                                                                       Does.Contain("must not be given twice"));

            }

        }

        #endregion

        #region The_roaming_network_operator_says_what_a_body_is_notified_for()

        /// <summary>
        /// The number of a conformity assessment body as a notified body and the legislations it is notified for are the
        /// roaming network operator's, which vets it (SPECIFICATION.md 4.1, POI3-ISSUES.md S32): no body declares itself
        /// notified, also not to issue a certificate under a legislation in the same change set; that change set both
        /// sign. A number names one body. Its other properties a body changes itself.
        /// </summary>
        [Test]
        public void The_roaming_network_operator_says_what_a_body_is_notified_for()
        {

            var rn1           = NewNetwork();
            var ptb           = TestParties.Party(PTB);
            var rno           = TestParties.Party(rn1);

            rn1.TryGetConformityAssessmentBodyById(PTB, out var ptbBody);

            var notifiedForCRA  = RoamingNetworkChange.UpdateProperty(ptbBody!, "notifiedFor",        null, new JArray("CRA", "MID"));
            var otherNumber     = RoamingNetworkChange.UpdateProperty(ptbBody!, "notifiedBodyNumber", null, "0103");
            var craCert         = new ConformityCertificate(ConformityCertificate_Id.Parse("cra-1"), ACME, Wallbox22.Id, [ HardwareRevision.Parse("1.0") ], [ FirmwareVersion.Parse("1.0.0") ], [ Chargy11 ], [],
                                                            Legislation: ConformityAssessmentBody.Legislations.CRA);
            var bsiWithPTBsNumber = new ConformityAssessmentBody(BSI, I18NString.Create(Languages.en, "BSI"), NotifiedBodyNumber: "0102",
                                                                 NotifiedFor: [ ConformityAssessmentBody.Legislations.CRA ], Created: Created);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, ptb, notifiedForCRA),  out _, out var error1),          Is.False, "notified for the CRA by itself");
                Assert.That(error1,                                                                                       Does.Contain("roaming network operator"));
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, ptb, otherNumber),     out _, out var error2),          Is.False, "its number by itself");

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, rno, notifiedForCRA),  out var forCRA, out var error3), Is.True,  error3);
                Assert.That(forCRA!.TryGetConformityAssessmentBodyById(PTB, out var ptbForCRA) ? ptbForCRA.NotifiedFor : [],
                                                                                                                          Is.EqualTo(new[] { "CRA", "MID" }));
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, rno, otherNumber),     out _, out var error4),          Is.True,  error4);

                // Notified for the CRA and issuing under it in one change set: both sign.
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, ptb, notifiedForCRA, RoamingNetworkChange.Add(craCert, PTB)),
                                                  out _, out var error5),                                                 Is.False, "the certificate's legislation by the body alone");
                Assert.That(rn1.TryApplyChangeSet(rn1.CreateChangeSet([ notifiedForCRA, RoamingNetworkChange.Add(craCert, PTB) ], CreatedAt: T0).SignedBy(rno).SignedBy(ptb),
                                                  out var withCRA, out var error6),                                       Is.True,  error6);
                Assert.That(withCRA!.TryGetConformityCertificateById(craCert.Id, out _),                                  Is.True);

                // Its other properties, the body's own.
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, ptb, RoamingNetworkChange.UpdateProperty(ptbBody!, "homepage", null, "https://www.ptb.de")),
                                                  out _, out var error7),                                                 Is.True,  error7);
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, rno, RoamingNetworkChange.UpdateProperty(ptbBody!, "homepage", null, "https://www.ptb.de")),
                                                  out _, out var error8),                                                 Is.False, "the homepage of a body with keys by the roaming network operator");

                // A number names one body.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(bsiWithPTBsNumber)),
                            Throws.ArgumentException.With.Message.Contains("notified body number '0102'"));

                Assert.That(() => new RoamingNetwork(RoamingNetwork_Id.Parse("rn2"), Created: Created,
                                                     ConformityAssessmentBodies: [ NewBody(PTB, [ ConformityAssessmentBody.Legislations.MID ]), bsiWithPTBsNumber ]),
                            Throws.ArgumentException.With.Message.Contains("notified body number '0102'"));

            }

        }

        #endregion

        #region Who_states_a_certificate_its_body_or_the_roaming_network_operator_in_its_place()

        /// <summary>
        /// A certificate the roaming network operator adds or changes in place of a body without keys says so, as its
        /// statedBy, "roamingNetworkOperator"; one of a body with keys names none (SPECIFICATION.md 4.1, csharp-review 15).
        /// Once the body has keys, it changes a certificate the roaming network operator stated only by stating it itself.
        /// </summary>
        [Test]
        public void Who_states_a_certificate_its_body_or_the_roaming_network_operator_in_its_place()
        {

            var withBSI   = Apply(NewNetwork(), RoamingNetworkChange.Add(NewBody(BSI, [ ConformityAssessmentBody.Legislations.CRA ])));
            var rno       = TestParties.Party(withBSI);
            var craId     = ConformityCertificate_Id.Parse("cra-1");

            ConformityCertificate CRACertificate(Boolean StatedByRoamingNetworkOperator)
                => new (craId, ACME, Wallbox22.Id, [ HardwareRevision.Parse("1.0") ], [ FirmwareVersion.Parse("1.0.0") ], [ Chargy11 ], [],
                        Legislation: ConformityAssessmentBody.Legislations.CRA, StatedByRoamingNetworkOperator: StatedByRoamingNetworkOperator);

            var byPTB     = new ConformityCertificate(ConformityCertificate_Id.Parse("cert-2"), ACME, Wallbox22.Id, [ HardwareRevision.Parse("1.0") ], [ FirmwareVersion.Parse("1.0.0") ], [ Chargy11 ], [],
                                                      StatedByRoamingNetworkOperator: true);

            Assert.That(withBSI.TryApplyChangeSet(SignedBy(withBSI, rno, RoamingNetworkChange.Add(CRACertificate(true), BSI)), out var withCRA, out var addError),  Is.True, addError);

            var otherStatement  = CRACertificate(true).ToJSON();
            otherStatement["statedBy"] = "BSI";

            // The BSI got keys since.
            var json      = withCRA!.ToJSON(IncludeETags: false);
            var bodies    = (JArray) json["conformityAssessmentBodies"]!;
            var bsiIndex  = bodies.IndexOf(bodies.Single(body => body["@id"]?.Value<String>() == BSI.ToString()));

            Assert.That(withCRA.TryGetConformityAssessmentBodyById(BSI, out var bsi), Is.True);
            bodies[bsiIndex] = bsi!.WithKeys().ToJSON(Embedded: true);
            Assert.That(RoamingNetwork.TryParse(json, out var keyed, out var keyedError), Is.True, keyedError);
            Assert.That(keyed!.TryGetConformityCertificateById(craId, out var stated), Is.True);

            var newNumber = RoamingNetworkChange.UpdateProperty(stated!, "documentNumber", null, "BSI-2026-1");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(withCRA.TryGetConformityCertificateById(craId, out var added),                         Is.True);
                Assert.That(added!.StatedByRoamingNetworkOperator,                                                  Is.True);
                Assert.That(added.ToJSON()["statedBy"]?.Value<String>(),                                            Is.EqualTo("roamingNetworkOperator"));

                Assert.That(() => SignedBy(withBSI, rno, RoamingNetworkChange.Add(CRACertificate(false), BSI)),
                            Throws.ArgumentException.With.Message.Contains("The certificate 'cra-1' of the conformity assessment body 'BSI' without signing keys is stated by the roaming network operator in its place: its statedBy is 'roamingNetworkOperator'!"));

                Assert.That(() => SignedBy(withBSI, TestParties.Party(PTB), RoamingNetworkChange.Add(byPTB, PTB)),
                            Throws.ArgumentException.With.Message.Contains("The certificate 'cert-2' of the conformity assessment body 'PTB' is stated by the body itself, with its keys: it names no statedBy!"));

                Assert.That(ConformityCertificate.TryParse(otherStatement, out _, out var otherError),              Is.False);
                Assert.That(otherError,                                                                             Does.Contain("is stated by its conformity assessment body, or by the roaming network operator in its place, 'roamingNetworkOperator', not by 'BSI'!"));

                Assert.That(() => SignedBy(keyed, TestParties.Party(BSI), newNumber),
                            Throws.ArgumentException.With.Message.Contains("The certificate 'cra-1' of the conformity assessment body 'BSI' is stated by the body itself, with its keys: it names no statedBy!"));

                Assert.That(keyed.TryApplyChangeSet(SignedBy(keyed, TestParties.Party(BSI), newNumber, RoamingNetworkChange.RemoveProperty(stated!, "statedBy", "roamingNetworkOperator")),
                                                    out var restated, out var restateError),                       Is.True, restateError);
                Assert.That(restated!.TryGetConformityCertificateById(craId, out var itsOwn),                       Is.True);
                Assert.That(itsOwn!.StatedByRoamingNetworkOperator,                                                 Is.False);
                Assert.That(itsOwn.DocumentNumber,                                                                  Is.EqualTo("BSI-2026-1"));

            }

        }

        #endregion

        #region The_roaming_network_operator_signs_in_place_of_a_body_by_a_key_that_may_sign_party()

        /// <summary>
        /// The roaming network operator signs in place of a body without keys by a signing key that may sign "party", the
        /// usage of everything it owns, as the examples certify its keys, or by one that may sign "certificates", the usage
        /// of what it changes; not by one naming neither (csharp-review 13).
        /// </summary>
        [Test]
        public void The_roaming_network_operator_signs_in_place_of_a_body_by_a_key_that_may_sign_party()
        {

            var withBSI  = Apply(NewNetwork(), RoamingNetworkChange.Add(NewBody(BSI, [ ConformityAssessmentBody.Legislations.CRA ])));
            var rno      = TestParties.Party(withBSI);
            var craCert  = ConformityCertificate_Id.Parse("cra-1");
            var addCert  = RoamingNetworkChange.Add(new ConformityCertificate(craCert, ACME, Wallbox22.Id, [ HardwareRevision.Parse("1.0") ], [ FirmwareVersion.Parse("1.0.0") ], [ Chargy11 ], [], Legislation: ConformityAssessmentBody.Legislations.CRA, StatedByRoamingNetworkOperator: true), BSI);

            // The network with the signing key of the roaming network operator certified for the given usage alone.
            RoamingNetwork CertifiedFor(String Usage)
            {

                var json = withBSI.ToJSON(IncludeETags: false);

                json["operator"]![RoamingNetworkSigningKeyCertificate.JSONProperty] = new JArray(
                    RoamingNetworkSigningKeyCertificate.Create(TestParties.Key(rno, POIKeyKind.Identity).Private,
                                                               COSEAlgorithm.ES512,
                                                               RoamingNetworkOperator_Id.Parse(rno.EntityId),
                                                               POIPublicKey.From(TestParties.Key(rno).Private, Usages: [ Usage ]),
                                                               Created).ToJSON(Embedded: true));

                Assert.That(RoamingNetwork.TryParse(json, out var network, out var error), Is.True, error);

                return network!;

            }

            var byParty           = CertifiedFor(POIKeyUsages.Party);
            var byCertificates    = CertifiedFor(POIKeyUsages.Certificates);
            var byInfrastructure  = CertifiedFor(POIKeyUsages.Infrastructure);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(byParty.         TryApplyChangeSet(SignedBy(byParty,          rno, addCert), out var added, out var partyError),           Is.True,  partyError);
                Assert.That(added!.ConformityAssessmentBodyOf(craCert)?.Id,                                                                             Is.EqualTo(BSI));

                Assert.That(byCertificates.  TryApplyChangeSet(SignedBy(byCertificates,   rno, addCert), out _,         out var certificatesError),    Is.True,  certificatesError);

                Assert.That(byInfrastructure.TryApplyChangeSet(SignedBy(byInfrastructure, rno, addCert), out _,         out var infrastructureError),  Is.False, "a certificate by a key that may sign infrastructure");
                Assert.That(infrastructureError,                                                                                                        Does.Contain($"needs a signature by a signing key of {rno} that may sign 'certificates' or 'party'"));

            }

        }

        #endregion

        #region Changes_of_releases_and_certificates_on_two_branches_merge()

        [Test]
        public void Changes_of_releases_and_certificates_on_two_branches_merge()
        {

            var rn1          = NewNetwork();

            rn1.TryGetTransparencySoftwareById           (Chargy10, out var chargy10);
            rn1.TryGetTransparencySoftwareById           (Chargy11, out var chargy11);
            rn1.TryGetConformityCertificateById(Cert1,    out var cert1);

            var cert2        = NewCertificate(ConformityCertificate_Id.Parse("cert-2"), [ Chargy11 ], []);

            // Two releases of one manufacturer, a certificate changed and one added to the same body: no conflict.
            var left         = Apply(rn1, RoamingNetworkChange.UpdateProperty(chargy10!, "supportUntil",    null, "2032-12-31T23:59:59.999Z"),
                                          RoamingNetworkChange.UpdateProperty(cert1!,    "documentNumber",  null, "DE-124"));
            var right        = Apply(rn1, RoamingNetworkChange.UpdateProperty(chargy11!, "howToUse",        null, "https://open.charging.cloud/chargy/manual"),
                                          RoamingNetworkChange.Add(cert2, PTB));
            var other        = Apply(rn1, RoamingNetworkChange.UpdateProperty(chargy10!, "supportUntil",    null, "2033-12-31T23:59:59.999Z"));

            var merged       = RoamingNetwork.TryMerge(rn1, left, right, ChangeSet_Id.Parse("merge-1"), out var merge, out _, out var conflicts1);
            var conflict     = RoamingNetwork.TryMerge(rn1, left, other, ChangeSet_Id.Parse("merge-2"), out _,         out _, out var conflicts2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(merged,                                                                          Is.True, String.Join(" ", conflicts1.Select(conflict => conflict.Description)));
                Assert.That(merge!.TryGetTransparencySoftwareById(Chargy10, out var mergedChargy10),         Is.True);
                Assert.That(mergedChargy10!.SupportUntil,                                                    Is.EqualTo(DateTimeOffset.Parse("2032-12-31T23:59:59.999Z")));
                Assert.That(merge. TryGetTransparencySoftwareById(Chargy11, out var mergedChargy11),         Is.True);
                Assert.That(mergedChargy11!.HowToUse?.ToString(),                                            Is.EqualTo("https://open.charging.cloud/chargy/manual"));
                Assert.That(merge. TryGetConformityCertificateById(Cert1, out var mergedCert1),    Is.True);
                Assert.That(mergedCert1!.DocumentNumber,                                                     Is.EqualTo("DE-124"));
                Assert.That(merge. ConformityAssessmentBodyOf(cert2.Id)?.Id,                                 Is.EqualTo(PTB));

                Assert.That(conflict,                                                                        Is.False);
                Assert.That(conflicts2.Single().PropertyName,                                                Is.EqualTo("supportUntil"));

            }

        }

        #endregion

        #region Releases_need_a_key_that_may_sign_software()

        /// <summary>
        /// A signing key naming usages signs only what they name: releases are "software".
        /// </summary>
        [Test]
        public void Releases_need_a_key_that_may_sign_software()
        {

            var (partyKey,    partyPublic)     = TestParties.Key(new POIPartyId(nameof(TransparencySoftwareManufacturer), "OpenChargingCloud/party"));
            var (softwareKey, softwarePublic)  = TestParties.Key(new POIPartyId(nameof(TransparencySoftwareManufacturer), "OpenChargingCloud/software"));

            var json = NewNetwork().ToJSON();
            json["transparencySoftwareManufacturers"]![0]!["signingKeys"] = new JArray(POIPublicKey.From(partyKey,    Usages: [ POIKeyUsages.Party    ]).ToJSON(Embedded: true),
                                                                                         POIPublicKey.From(softwareKey, Usages: [ POIKeyUsages.Software ]).ToJSON(Embedded: true));

            Assert.That(RoamingNetwork.TryParse(json, out var rn1, out var error),  Is.True, error);

            var addRelease  = RoamingNetworkChange.Add(NewRelease(TransparencySoftware_Id.Parse("chargy-2.0"), "2.0"), OCC);
            var byParty     = rn1!.CreateChangeSet([ addRelease ], CreatedAt: T0).Sign(partyKey,    POIPublicKey.From(partyKey).Id,    COSEAlgorithm.ES256);
            var bySoftware  = rn1. CreateChangeSet([ addRelease ], CreatedAt: T0).Sign(softwareKey, POIPublicKey.From(softwareKey).Id, COSEAlgorithm.ES256);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn1.TryApplyChangeSet(byParty,    out _, out var error1),  Is.False, "a release by the key of the party");
                Assert.That(error1,                                                     Does.Contain("that may sign 'software'"));
                Assert.That(rn1.TryApplyChangeSet(bySoftware, out _, out var error2),  Is.True,  error2);
            }

        }

        #endregion

        #region Manufacturers_have_a_runtime_status()

        [Test]
        public void Manufacturers_have_a_runtime_status()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());

            var result  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(TransparencySoftwareManufacturer), "OpenChargingCloud", "outOfService", T0, out _, out var error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result,                                                                         Is.EqualTo(POIStatusUpdateResult.Updated), error);
                Assert.That(history.RuntimeStatus.All.Single(status => status.EntityType == nameof(TransparencySoftwareManufacturer) &&
                                                                       status.Kind       == POIRuntimeStatusKind.AdminStatus).Value,  Is.EqualTo("outOfService"));
            }

        }

        #endregion

    }

}
