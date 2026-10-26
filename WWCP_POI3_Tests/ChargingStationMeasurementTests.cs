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

using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// A charging station that signs measurements itself, as an energy meter does: the homepage of its model, its serial
    /// number, its transparency software releases, its signature methods and its public keys; and the ISO 15118
    /// certificate of an EVSE. Templates give the homepage, the releases and the signature methods, never the serial
    /// number, the public keys or the ISO 15118 certificate of the single device.
    /// </summary>
    [TestFixture]
    public class ChargingStationMeasurementTests
    {

        #region Data

        private static readonly DateTimeOffset              Created   = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0        = DateTimeOffset.Parse("2026-10-26T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF       = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1        = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1        = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1        = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly ChargingStationTemplate_Id  ST1       = ChargingStationTemplate_Id.Parse("DE*GEF*ST1");
        private static readonly TransparencySoftware_Id     Chargy10  = TransparencySoftware_Id.   Parse("chargy-1.0");
        private static readonly TransparencySoftware_Id     Chargy11  = TransparencySoftware_Id.   Parse("chargy-1.1");
        private static readonly TransparencySoftware_Id     Chargy9   = TransparencySoftware_Id.   Parse("chargy-9");

        private static readonly URL                         ModelURL  = URL.Parse("https://acme.example/wallbox-22");
        private static readonly String                      SECC      = "-----BEGIN CERTIFICATE-----\nMIIB...\n-----END CERTIFICATE-----";

        #endregion

        #region NewKey()

        /// <summary>
        /// The DER encoded SubjectPublicKeyInfo of a new key, made in the station, whose private key never leaves it.
        /// </summary>
        private static MeasurementPublicKey NewKey()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            return new MeasurementPublicKey(key.ExportSubjectPublicKeyInfo());
        }

        #endregion

        #region NewRelease(Id, Version)

        private static TransparencySoftware NewRelease(TransparencySoftware_Id Id, String Version)

            => new (Id,
                    ImmutableI18NString.Create(Languages.en, "Chargy"),
                    TransparencySoftwareVersion.Parse(Version),
                    [ OpenSourceLicense.AGPL3 ],
                    SourceCodeRepository: URL.Parse("https://github.com/OpenChargingCloud/ChargyDesktopApp"));

        #endregion

        #region NewNetwork(Station = null, Template = null)

        /// <summary>
        /// Chargy 1.0 and 1.1; DE*GEF with the station S1, its serial number, its public key and Chargy 1.0 under the
        /// German calibration law, its EVSE with an ISO 15118 certificate, and the template ST1, giving the homepage of the
        /// model, OCMF, Chargy 1.0 for information only and Chargy 1.1 for information only.
        /// </summary>
        private static RoamingNetwork NewNetwork(ChargingStation?          Station    = null,
                                                 ChargingStationTemplate?  Template   = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                            Created,
                   TransparencySoftwareManufacturers:  [ new TransparencySoftwareManufacturer(TransparencySoftwareManufacturer_Id.Parse("OpenChargingCloud"),
                                                                                              TransparencySoftware:  [ NewRelease(Chargy10, "1.0"), NewRelease(Chargy11, "1.1") ],
                                                                                              Created:               Created) ],
                   ChargingStationOperators:           [ new ChargingStationOperator(
                                                             GEF,
                                                             Created:                   Created,
                                                             ChargingPools:             [ new ChargingPool(P1,
                                                                                                           Created:           Created,
                                                                                                           ChargingStations:  [ Station ?? NewStation() ]) ],
                                                             ChargingStationTemplates:  [ Template ?? new ChargingStationTemplate(ST1,
                                                                                                                                  ModelURL:                    ModelURL,
                                                                                                                                  TransparencySoftwareStatus:  [ new TransparencySoftwareAssignment(Chargy10, LegalStatus.ForInformationOnly),
                                                                                                                                                                 new TransparencySoftwareAssignment(Chargy11, LegalStatus.ForInformationOnly) ],
                                                                                                                                  SignatureMethods:            [ SignatureMethod.OCMF ],
                                                                                                                                  Created:                     Created) ]) ]).WithKeys();

        private static ChargingStation NewStation(IEnumerable<TransparencySoftwareAssignment>? TransparencySoftwareStatus = null)

            => new (S1,
                    SerialNumber:                SerialNumber.Parse("WB22-000815"),
                    TransparencySoftwareStatus:  TransparencySoftwareStatus ?? [ new TransparencySoftwareAssignment(Chargy10, LegalStatus.GermanCalibrationLaw) ],
                    PublicKeys:                  [ NewKey() ],
                    TemplateIds:                 [ ST1 ],
                    Created:                     Created,
                    EVSEs:                       [ new EVSE(E1, ISO15118Certificate: SECC, Created: Created) ]);

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region A_charging_station_is_read_as_it_is_written()

        [Test]
        public void A_charging_station_is_read_as_it_is_written()
        {

            var key      = NewKey();
            var station  = new ChargingStation(S1,
                                               ModelURL:                    ModelURL,
                                               SerialNumber:                SerialNumber.Parse("WB22-000815"),
                                               TransparencySoftwareStatus:  [ new TransparencySoftwareAssignment(Chargy11, LegalStatus.ForInformationOnly),
                                                                              new TransparencySoftwareAssignment(Chargy10, LegalStatus.GermanCalibrationLaw) ],
                                               SignatureMethods:            [ SignatureMethod.Parse("SAFE"), SignatureMethod.OCMF ],
                                               PublicKeys:                  [ key ],
                                               Created:                     Created);

            var json     = station.ToJSON();

            JObject With(String Key, JToken Value)
            {
                var changed = (JObject) json.DeepClone();
                changed[Key] = Value;
                return changed;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json["modelURL"]?.Value<String>(),                                   Is.EqualTo(ModelURL.ToString()));
                Assert.That(json["serialNumber"]?.Value<String>(),                               Is.EqualTo("WB22-000815"));
                Assert.That(json["transparencySoftwareStatus"]!.Select(status => status["transparencySoftwareId"]!.Value<String>()),
                                                                                                 Is.EqualTo(new[] { "chargy-1.0", "chargy-1.1" }), "ordered by release");
                Assert.That(json["signatureMethods"]!.Values<String>(),                          Is.EqualTo(new[] { "OCMF", "SAFE" }), "ordered by name");
                Assert.That(json["publicKeys"]![0]!["@id"]!.Value<String>(),                      Is.EqualTo(key.Id));
                Assert.That(key.ToJSON()["@context"]!.Value<String>(),                          Is.EqualTo("https://open.charging.cloud/contexts/wwcp/roamingNetwork/measurementPublicKey/v1"));

                Assert.That(ChargingStation.TryParse(json, out var parsed, out var error),       Is.True, error);
                Assert.That(parsed!.ToJSON().ToString(),                                         Is.EqualTo(json.ToString()));

                Assert.That(new ChargingStation(S1, Created: Created).ToJSON().Properties().Select(property => property.Name).
                                Intersect([ "modelURL", "serialNumber", "transparencySoftwareStatus", "signatureMethods", "publicKeys" ]),
                            Is.Empty, "none of them when absent");

                Assert.That(ChargingStation.TryParse(With("serialNumber", " WB22-000815"), out _, out var serialError),  Is.False);
                Assert.That(serialError,                                                          Does.Contain("Invalid serial number ' WB22-000815'"));
                Assert.That(ChargingStation.TryParse(With("signatureMethods", new JArray("OCMF", "ocmf")), out _, out var twiceError),  Is.False);
                Assert.That(twiceError,                                                           Does.Contain("twice"));
                Assert.That(ChargingStation.TryParse(With("publicKeys", new JArray(json["publicKeys"]![0]!, json["publicKeys"]![0]!)), out _, out _),  Is.False, "a public key given twice");

            }

        }

        #endregion

        #region Templates_give_the_model_releases_and_signature_methods_not_the_device()

        [Test]
        public void Templates_give_the_model_releases_and_signature_methods_not_the_device()
        {

            var rn1        = NewNetwork();
            var gef        = rn1.GetChargingStationOperatorById(GEF)!;
            var station    = gef.ChargingStations.Single();

            Assert.That(gef.TryGetEffectiveChargingStation(S1, out var effective), Is.True);

            using (Assert.EnterMultipleScope())
            {

                // The template gives the homepage of the model, its signature methods and its releases besides the
                // station's own: of a release the station names too, the station's.
                Assert.That(effective!.ModelURL,                                                 Is.EqualTo(ModelURL));
                Assert.That(effective. SignatureMethods,                                          Is.EqualTo(new[] { SignatureMethod.OCMF }));
                Assert.That(effective. TransparencySoftwareStatus.Select(status => (status.TransparencySoftwareId, status.LegalStatus)),
                            Is.EqualTo(new[] { (Chargy10, LegalStatus.GermanCalibrationLaw), (Chargy11, LegalStatus.ForInformationOnly) }));

                // Its serial number, public keys and ISO 15118 certificates are its own; the stored station names
                // nothing of its template.
                Assert.That(effective. SerialNumber,                                              Is.EqualTo(SerialNumber.Parse("WB22-000815")));
                Assert.That(effective. PublicKeys,                                                Is.EqualTo(station.PublicKeys));
                Assert.That(effective. EVSEs.Single().ISO15118Certificate,                        Is.EqualTo(SECC));
                Assert.That(station.   ModelURL,                                                  Is.Null);
                Assert.That(station.   SignatureMethods,                                          Is.Empty);

                Assert.That(NewNetwork().GetChargingStationOperatorById(GEF)!.ChargingStationTemplates.Single().ToJSON().Properties().Select(property => property.Name).
                                Intersect([ "serialNumber", "publicKeys" ]),
                            Is.Empty, "no template gives them");

                // Read again, the network is the same.
                var networkJSON = rn1.ToJSON(IncludeETags: false);
                Assert.That(RoamingNetwork.TryParse(networkJSON, out var fromJSON, out var error),  Is.True, error);
                Assert.That(fromJSON!.ToJSON(IncludeETags: false).ToString(),                      Is.EqualTo(networkJSON.ToString()));

            }

        }

        #endregion

        #region Releases_of_stations_and_their_templates_are_releases_of_the_network()

        [Test]
        public void Releases_of_stations_and_their_templates_are_releases_of_the_network()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => NewNetwork(Station: NewStation([ new TransparencySoftwareAssignment(Chargy9, LegalStatus.ForInformationOnly) ])),
                            Throws.ArgumentException.With.Message.Contains("The transparency software release 'chargy-9' of the charging station 'DE*GEF*S1' is unknown in the roaming network"));

                Assert.That(() => NewNetwork(Template: new ChargingStationTemplate(ST1,
                                                                                  TransparencySoftwareStatus:  [ new TransparencySoftwareAssignment(Chargy9, LegalStatus.ForInformationOnly) ],
                                                                                  Created:                     Created)),
                            Throws.ArgumentException.With.Message.Contains("The transparency software release 'chargy-9' of the charging station template 'DE*GEF*ST1' is unknown in the roaming network"));

                Assert.That(() => new ChargingStation(S1, TransparencySoftwareStatus: [ new TransparencySoftwareAssignment(Chargy10, LegalStatus.ForInformationOnly),
                                                                                        new TransparencySoftwareAssignment(Chargy10, LegalStatus.GermanCalibrationLaw) ]),
                            Throws.ArgumentException, "a release named twice");

            }

        }

        #endregion

        #region Change_sets_change_them()

        [Test]
        public void Change_sets_change_them()
        {

            var rn1      = NewNetwork();
            var gef1     = rn1.GetChargingStationOperatorById(GEF)!;
            var station  = gef1.ChargingStations.Single();
            var evse     = gef1.EVSEs.Single();
            var key      = NewKey();

            var rn2      = Apply(rn1,
                                 RoamingNetworkChange.UpdateProperty(station, "serialNumber",               "WB22-000815", "WB22-000816"),
                                 RoamingNetworkChange.UpdateProperty(station, "modelURL",                   null,          "https://acme.example/wallbox-22-rev-b"),
                                 RoamingNetworkChange.UpdateProperty(station, "signatureMethods",           null,          new JArray("SAFE")),
                                 RoamingNetworkChange.UpdateProperty(station, "publicKeys",                 station.ToJSON()["publicKeys"],
                                                                                                             new JArray(key.ToJSON(Embedded: true))),
                                 RoamingNetworkChange.UpdateProperty(station, "transparencySoftwareStatus", station.ToJSON()["transparencySoftwareStatus"],
                                                                                                             new JArray(new TransparencySoftwareAssignment(Chargy11, LegalStatus.GermanCalibrationLaw).ToJSON(Embedded: true))),
                                 RoamingNetworkChange.UpdateProperty(evse,    "iso15118Certificate",        SECC,          "-----BEGIN CERTIFICATE-----\nMIIC...\n-----END CERTIFICATE-----"));

            var gef2     = rn2.GetChargingStationOperatorById(GEF)!;

            Assert.That(gef2.TryGetEffectiveChargingStation(S1, out var effective), Is.True);

            using (Assert.EnterMultipleScope())
            {

                // Its own values before its template's.
                Assert.That(effective!.SerialNumber,                                             Is.EqualTo(SerialNumber.Parse("WB22-000816")));
                Assert.That(effective. ModelURL,                                                 Is.EqualTo(URL.Parse("https://acme.example/wallbox-22-rev-b")));
                Assert.That(effective. SignatureMethods,                                          Is.EqualTo(new[] { SignatureMethod.OCMF, SignatureMethod.Parse("SAFE") }));
                Assert.That(effective. PublicKeys.Single().Id,                                    Is.EqualTo(key.Id));
                Assert.That(effective. TransparencySoftwareStatus.Select(status => (status.TransparencySoftwareId, status.LegalStatus)),
                            Is.EqualTo(new[] { (Chargy10, LegalStatus.ForInformationOnly), (Chargy11, LegalStatus.GermanCalibrationLaw) }));
                Assert.That(gef2.EVSEs.Single().ISO15118Certificate,                              Does.Contain("MIIC"));

                // A release unknown in the network is refused by the change set.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(gef2.ChargingStations.Single(), "transparencySoftwareStatus", null,
                                                                                 new JArray(new TransparencySoftwareAssignment(Chargy9, LegalStatus.ForInformationOnly).ToJSON(Embedded: true)))),
                            Throws.Exception.With.Message.Contains("chargy-9"));

            }

        }

        #endregion

        #region An_EVSE_is_read_as_it_is_written_with_its_ISO_15118_certificate()

        [Test]
        public void An_EVSE_is_read_as_it_is_written_with_its_ISO_15118_certificate()
        {

            var json = new EVSE(E1, ISO15118Certificate: SECC, Created: Created).ToJSON();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json["iso15118Certificate"]?.Value<String>(),                        Is.EqualTo(SECC));
                Assert.That(global::cloud.charging.open.protocols.WWCP.POI.EVSE.TryParse(json, out var parsed, out var error),  Is.True, error);
                Assert.That(parsed!.ISO15118Certificate,                                         Is.EqualTo(SECC));
                Assert.That(parsed. ToJSON().ToString(),                                         Is.EqualTo(json.ToString()));
                Assert.That(new EVSE(E1, Created: Created).ToJSON()["iso15118Certificate"],      Is.Null);
                Assert.That(new EVSETemplate(EVSETemplate_Id.Parse("DE*GEF*ET1"), Created: Created).ToJSON()["iso15118Certificate"],  Is.Null);

            }

        }

        #endregion

    }

}
