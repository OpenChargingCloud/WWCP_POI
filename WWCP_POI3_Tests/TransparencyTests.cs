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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Transparency software releases and their certificates embedded in the roaming network,
    /// assigned to energy meters with their legal status.
    /// </summary>
    [TestFixture]
    public class TransparencyTests
    {

        #region Data

        private static readonly DateTimeOffset                      Created     = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset                      T0          = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id          GEF         = ChargingStationOperator_Id.        Parse("DE*GEF");
        private static readonly ChargingStationManufacturer_Id      ACME        = ChargingStationManufacturer_Id.    Parse("ACME");
        private static readonly TransparencySoftware_Id             Chargy10    = TransparencySoftware_Id.           Parse("chargy-1.0");
        private static readonly TransparencySoftware_Id             Chargy11    = TransparencySoftware_Id.           Parse("chargy-1.1");
        private static readonly TransparencySoftwareCertificate_Id  Cert1       = TransparencySoftwareCertificate_Id.Parse("cert-1");
        private static readonly EnergyMeter_Id                      Meter1      = EnergyMeter_Id.                    Parse("meter-1");

        #endregion

        #region NewRelease(Id, Version)

        private static TransparencySoftware NewRelease(TransparencySoftware_Id Id, String Version)

            => new (Id,
                    new ImmutableI18NString(I18NString.Create(Languages.en, "Chargy")),
                    TransparencySoftwareVersion.Parse(Version),
                    [ OpenSourceLicense.AGPL3 ],
                    "GraphDefined GmbH",
                    SourceCodeRepository: URL.Parse("https://github.com/OpenChargingCloud/ChargyDesktopApp"));

        #endregion

        #region NewCertificate(Id, Verified, Compatible)

        private static TransparencySoftwareCertificate NewCertificate(TransparencySoftwareCertificate_Id  Id,
                                                                      TransparencySoftware_Id[]           Verified,
                                                                      TransparencySoftware_Id[]           Compatible)

            => new (Id,
                    "PTB",
                    "Wallbox-22",
                    "1.0",
                    Verified,
                    Compatible,
                    ChargingStationManufacturerId:  ACME,
                    DocumentNumber:                 "DE-123",
                    NotBefore:                      Created);

        #endregion

        #region NewNetwork(Assignments = null)

        private static RoamingNetwork NewNetwork(TransparencySoftwareAssignment[]? Assignments = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                           Created,
                    ChargingStationManufacturers:      [ new ChargingStationManufacturer(ACME, Created: Created) ],
                    TransparencySoftware:              [ NewRelease(Chargy10, "1.0"), NewRelease(Chargy11, "1.1") ],
                    TransparencySoftwareCertificates:  [ NewCertificate(Cert1, [ Chargy10 ], [ Chargy11 ]) ],
                    ChargingStationOperators:          [ new ChargingStationOperator(GEF,
                                                                                     Created:        Created,
                                                                                     ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                        Created:           Created,
                                                                                                                        ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                 Created:  Created,
                                                                                                                                                                 EVSEs:    [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"),
                                                                                                                                                                                      EnergyMeter:  new EnergyMeter(Meter1,
                                                                                                                                                                                                                    TransparencySoftwareStatus:  Assignments ?? [
                                                                                                                                                                                                                        new TransparencySoftwareAssignment(Chargy10, LegalStatus.GermanCalibrationLaw, Cert1),
                                                                                                                                                                                                                        new TransparencySoftwareAssignment(Chargy11, LegalStatus.ForInformationOnly)
                                                                                                                                                                                                                    ],
                                                                                                                                                                                                                    Created:                     Created),
                                                                                                                                                                                      Created:      Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Transparency_software_survives_JSON_JSON_text_and_CBOR()

        [Test]
        public void Transparency_software_survives_JSON_JSON_text_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var meter = json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["energyMeter"]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                          Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                          Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                          Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                                   Is.EqualTo(rn1.CalcETags()));

                Assert.That(json["transparencySoftware"]!.Select(release => release["@id"]!.ToString()),  Is.EqualTo(new[] { "chargy-1.0", "chargy-1.1" }));
                Assert.That(json["transparencySoftwareCertificates"]![0]!["@id"]!.ToString(),       Is.EqualTo("cert-1"));
                Assert.That(meter["transparencySoftwareStatus"]![0]!["certificateId"]!.ToString(),  Is.EqualTo("cert-1"));
                Assert.That(meter["transparencySoftwareStatus"]![0]!["legalStatus"]!.ToString(),    Is.EqualTo(LegalStatus.GermanCalibrationLaw.ToString()));

                Assert.That(fromJSON.TryGetTransparencySoftwareById(Chargy11, out var chargy11),    Is.True);
                Assert.That(chargy11!.Version.ToString(),                                           Is.EqualTo("1.1"));
                Assert.That(fromJSON.TryGetTransparencySoftwareCertificateById(Cert1, out var cert1),  Is.True);
                Assert.That(cert1!.Covers(Chargy11),                                                Is.True);
                Assert.That(fromJSON.TryGetEnergyMeterById(Meter1, out var meter1),                 Is.True);
                Assert.That(meter1!.TransparencySoftwareStatus.Length,                              Is.EqualTo(2));

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

            var unknownRelease      = Changed(network => network["transparencySoftwareCertificates"]![0]!["compatibleTransparencySoftwareIds"] = new JArray("chargy-9.9"));
            var unknownManufacturer = Changed(network => network["transparencySoftwareCertificates"]![0]!["chargingStationManufacturerId"] = "NOBODY");
            var meterRelease        = Changed(network => ((JArray) network["transparencySoftware"]!).RemoveAt(1));
            var meterCertificate    = Changed(network => Meter(network)["transparencySoftwareStatus"]![0]!["certificateId"] = "cert-9");
            var twice               = Changed(network => Meter(network)["transparencySoftwareStatus"]![1]!["transparencySoftwareId"] = "chargy-1.0");

            // Chargy 1.1 is only compatible with the certificate, but covered by it: fine.
            var covered             = Changed(network => Meter(network)["transparencySoftwareStatus"]![1]!["certificateId"] = "cert-1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RoamingNetwork.TryParse(unknownRelease,       out _, out var error1),  Is.False);
                Assert.That(error1,                                                                Does.Contain("release 'chargy-9.9' of the certificate 'cert-1'"));
                Assert.That(RoamingNetwork.TryParse(unknownManufacturer,  out _, out var error2),  Is.False);
                Assert.That(error2,                                                                Does.Contain("manufacturer 'NOBODY' of the certificate"));
                Assert.That(RoamingNetwork.TryParse(meterRelease,         out _, out var error3),  Is.False);
                Assert.That(error3,                                                                Does.Contain("'chargy-1.1'"));
                Assert.That(RoamingNetwork.TryParse(meterCertificate,     out _, out var error4),  Is.False);
                Assert.That(error4,                                                                Does.Contain("certificate 'cert-9' of the energy meter 'meter-1'"));
                Assert.That(RoamingNetwork.TryParse(twice,                out _, out var error6),  Is.False);
                Assert.That(error6,                                                                Does.Contain("must not be given twice"));
                Assert.That(RoamingNetwork.TryParse(covered,              out _, out var error5),  Is.True, error5);
            }

            // Not covered: the certificate verifies and makes compatible other releases only.
            var other = NewNetwork().ToJSON();
            ((JObject) other["transparencySoftwareCertificates"]![0]!)["compatibleTransparencySoftwareIds"] = new JArray();
            Meter(other)["transparencySoftwareStatus"]![1]!["certificateId"] = "cert-1";

            Assert.That(RoamingNetwork.TryParse(other, out _, out var error7),  Is.False);
            Assert.That(error7,                                                 Does.Contain("does not cover the transparency software release 'chargy-1.1'"));

        }

        #endregion

        #region Transparency_software_is_changed_by_change_sets()

        [Test]
        public void Transparency_software_is_changed_by_change_sets()
        {

            var rn1      = NewNetwork();
            var chargy20 = TransparencySoftware_Id.Parse("chargy-2.0");
            var cert1    = rn1.TransparencySoftwareCertificates.Single();
            rn1.TryGetTransparencySoftwareById(Chargy11, out var chargy11);

            var rn2      = Apply(rn1,
                                 RoamingNetworkChange.Add           (NewRelease(chargy20, "2.0")),
                                 RoamingNetworkChange.UpdateProperty(cert1,     "compatibleTransparencySoftwareIds", null, new JArray("chargy-1.1", "chargy-2.0")),
                                 RoamingNetworkChange.UpdateProperty(chargy11!, "vendor",                            null, "Open Charging Cloud"));

            rn2.TryGetTransparencySoftwareById(Chargy11, out var changedChargy11);
            rn2.TryGetTransparencySoftwareCertificateById(Cert1, out var changedCert1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.TransparencySoftware.Select(release => release.Id.ToString()).Order(),  Is.EqualTo(new[] { "chargy-1.0", "chargy-1.1", "chargy-2.0" }));
                Assert.That(changedChargy11!.Vendor,                                                    Is.EqualTo("Open Charging Cloud"));
                Assert.That(changedCert1!.Covers(chargy20),                                             Is.True);
                // Releases and certificates have no timestamps.
                Assert.That(rn2.ToJSON()["transparencySoftware"]![2]!["created"],                        Is.Null);

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(Chargy10)),
                            Throws.ArgumentException.With.Message.Contains("release 'chargy-1.0' of the certificate 'cert-1'"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(Cert1)),
                            Throws.ArgumentException.With.Message.Contains("certificate 'cert-1' of the energy meter 'meter-1'"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(changedChargy11, "@id", null, "x")),
                            Throws.ArgumentException.With.Message.Contains("can not be changed"));

            }

        }

        #endregion

    }

}
