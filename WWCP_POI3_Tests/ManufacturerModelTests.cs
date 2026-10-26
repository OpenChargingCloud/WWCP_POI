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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Charging station and energy meter manufacturers, parties of their roaming network, and their models, which
    /// charging stations, energy meters and certificates name with their manufacturer.
    /// </summary>
    [TestFixture]
    public class ManufacturerModelTests
    {

        #region Data

        private static readonly DateTimeOffset                  Created    = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset                  T0         = DateTimeOffset.Parse("2026-10-24T12:00:00Z");

        private static readonly ChargingStationOperator_Id      GEF        = ChargingStationOperator_Id.    Parse("DE*GEF");
        private static readonly ChargingStation_Id              S1         = ChargingStation_Id.            Parse("DE*GEF*S1");
        private static readonly EnergyMeter_Id                  Meter1     = EnergyMeter_Id.                Parse("meter-1");
        private static readonly ChargingStationManufacturer_Id  ACME       = ChargingStationManufacturer_Id.Parse("ACME");
        private static readonly ChargingStationManufacturer_Id  ZAP        = ChargingStationManufacturer_Id.Parse("ZAP");
        private static readonly EnergyMeterManufacturer_Id      METERS     = EnergyMeterManufacturer_Id.    Parse("METERS");
        private static readonly ChargingStationModel_Id         DC300      = ChargingStationModel_Id.       Parse("DC-300");
        private static readonly ChargingStationModel_Id         AC22       = ChargingStationModel_Id.       Parse("AC-22");
        private static readonly EnergyMeterModel_Id             M500       = EnergyMeterModel_Id.           Parse("M-500");
        private static readonly TransparencySoftware_Id         Chargy10   = TransparencySoftware_Id.       Parse("chargy-1.0");

        #endregion

        #region NewDC300() / NewM500()

        private static ChargingStationModel NewDC300()

            => new (DC300,
                    ImmutableI18NString.Create(Languages.en, "DC 300 kW"),
                    HardwareRevisions:  [ "1.0", "1.1" ],
                    FirmwareVersions:   [ "4.2", "4.3" ],
                    ProductURL:         URL.Parse("https://acme.example/dc-300"),
                    DatasheetURL:       URL.Parse("https://acme.example/dc-300/datasheet.pdf"),
                    MaxPower:           Watt.FromW(300000),
                    CurrentTypes:       CurrentTypes.DC,
                    ConnectorTypes:     [ ChargingConnectorType.IEC_62196_T2_COMBO ],
                    NumberOfEVSEs:      2);

        private static EnergyMeterModel NewM500()

            => new (M500,
                    ImmutableI18NString.Create(Languages.en, "DC meter"),
                    HardwareRevisions:  [ "3.0" ],
                    FirmwareVersions:   [ "1.7" ],
                    CurrentTypes:       CurrentTypes.DC,
                    MaxCurrent:         Ampere.FromA(500),
                    AccuracyClass:      "B",
                    Measurands:         [ "1-0:1.8.0", "1-0:2.8.0" ],
                    SignatureMethods:   [ SignatureMethod.OCMF ]);

        #endregion

        #region NewNetwork(StationModel = DC300, MeterModel = M500, StationFirmware = "4.3", MeterRevision = "3.0")

        private static RoamingNetwork NewNetwork(ChargingStationModel_Id?  StationModel      = null,
                                                 EnergyMeterModel_Id?      MeterModel        = null,
                                                 String                    StationFirmware   = "4.3",
                                                 String                    MeterRevision     = "3.0")

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                       Created,
                    ChargingStationManufacturers:  [ new ChargingStationManufacturer(ACME, Models: [ NewDC300(), new ChargingStationModel(AC22) ], Created: Created) ],
                    EnergyMeterManufacturers:      [ new EnergyMeterManufacturer(METERS, Models: [ NewM500() ], Created: Created) ],
                    ChargingStationOperators:      [ new ChargingStationOperator(GEF,
                                                                                 Created:        Created,
                                                                                 ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                    Created:           Created,
                                                                                                                    ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                             ManufacturerId:    ACME,
                                                                                                                                                             ModelId:           StationModel ?? DC300,
                                                                                                                                                             HardwareRevision:  StationModel is null ? "1.1"           : null,
                                                                                                                                                             FirmwareVersion:   StationModel is null ? StationFirmware : null,
                                                                                                                                                             Created:           Created,
                                                                                                                                                             EVSEs:             [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"),
                                                                                                                                                                                           EnergyMeter:  new EnergyMeter(Meter1,
                                                                                                                                                                                                                         ManufacturerId:    METERS,
                                                                                                                                                                                                                         ModelId:           MeterModel ?? M500,
                                                                                                                                                                                                                         HardwareRevision:  MeterModel is null ? MeterRevision : null,
                                                                                                                                                                                                                         FirmwareVersion:   MeterModel is null ? "1.7"         : null,
                                                                                                                                                                                                                         Created:           Created),
                                                                                                                                                                                           Created:      Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes) / SignedBy(Network, Party, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        private static RoamingNetworkChangeSet SignedBy(RoamingNetwork                  Network,
                                                        POIPartyId                      Party,
                                                        params RoamingNetworkChange[]   Changes)

            => Network.CreateChangeSet(Changes, CreatedAt: T0).SignedBy(Party);

        #endregion

        #region ErrorOf(Network, Party, Change)

        /// <summary>
        /// Why the given change, signed by the given party, is refused, or null: a change of an invalid content already
        /// when its change set is created.
        /// </summary>
        private static String? ErrorOf(RoamingNetwork        Network,
                                       POIPartyId            Party,
                                       RoamingNetworkChange  Change)
        {
            try
            {
                return Network.TryApplyChangeSet(SignedBy(Network, Party, Change), out _, out var error)
                           ? null
                           : error;
            }
            catch (ArgumentException exception)
            {
                return exception.Message;
            }
        }

        #endregion


        #region Models_survive_JSON_JSON_text_and_CBOR()

        [Test]
        public void Models_survive_JSON_JSON_text_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var manufacturer  = json["chargingStationManufacturers"]![0]!;
            var dc300         = manufacturer["chargingStationModels"]![1]!;
            var m500          = json["energyMeterManufacturers"]![0]!["energyMeterModels"]![0]!;

            fromCBOR!.TryGetChargingStationManufacturerById(ACME, out var acme);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR. ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                            Is.EqualTo(rn1.CalcETags()));

                // Ordered by their identifications, embedded without their own context and their manufacturer.
                Assert.That(manufacturer["chargingStationModels"]!.Select(model => model["@id"]!.ToString()),  Is.EqualTo(new[] { "AC-22", "DC-300" }));
                Assert.That(manufacturer["signingKeys"]!.Count(),                            Is.EqualTo(1));
                Assert.That(dc300["@context"],                                               Is.Null);
                Assert.That(dc300["hardwareRevisions"]!.Select(revision => revision.ToString()),  Is.EqualTo(new[] { "1.0", "1.1" }));
                Assert.That(dc300["firmwareVersions"]!. Select(version  => version. ToString()),  Is.EqualTo(new[] { "4.2", "4.3" }));
                Assert.That(dc300["versions"],                                               Is.Null);
                Assert.That(dc300["maxPower"]!.Value<Decimal>(),                             Is.EqualTo(300000m));
                Assert.That(dc300["currentTypes"]![0]!.ToString(),                           Is.EqualTo("DC"));
                Assert.That(dc300["connectorTypes"]!.Count(),                                Is.EqualTo(1));
                Assert.That(dc300["numberOfEVSEs"]!.Value<Int32>(),                          Is.EqualTo(2));
                Assert.That(m500["maxCurrent"]!.Value<Decimal>(),                            Is.EqualTo(500m));
                Assert.That(m500["accuracyClass"]!.ToString(),                               Is.EqualTo("B"));
                Assert.That(m500["measurands"]!.Select(measurand => measurand.ToString()),   Is.EqualTo(new[] { "1-0:1.8.0", "1-0:2.8.0" }));
                Assert.That(m500["signatureMethods"]![0]!.ToString(),                        Is.EqualTo("OCMF"));

                Assert.That(acme!.TryGetModel(DC300, out var model),                         Is.True);
                Assert.That(model!.HasHardwareRevision("1.1"),                               Is.True);
                Assert.That(model. HasHardwareRevision("4.3"),                               Is.False);
                Assert.That(model. HasFirmwareVersion ("4.3"),                               Is.True);
                Assert.That(model. HasFirmwareVersion ("1.1"),                               Is.False);
                Assert.That(model.ToJSON()["@context"]!.ToString(),                          Is.EqualTo(ChargingStationModel.JSONLDContext));

            }

        }

        #endregion

        #region Invalid_models_are_refused()

        [Test]
        public void Invalid_models_are_refused()
        {

            JObject Model(params JProperty[] Properties)
                => new (new JProperty("@id", "X-1"), Properties);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(ChargingStationModel.TryParse(Model(new JProperty("hardwareRevisions", new JArray("1.0", "1.0"))),  out _, out var error1),  Is.False);
                Assert.That(error1,                                                                                                 Does.Contain("A hardware revision of the charging station model 'X-1' must not be given twice"));
                Assert.That(ChargingStationModel.TryParse(Model(new JProperty("firmwareVersions",  new JArray(" 1.0"))),        out _, out var error2),  Is.False);
                Assert.That(error2,                                                                                                 Does.Contain("A firmware version of the charging station model 'X-1' must not begin or end with whitespace"));
                Assert.That(EnergyMeterModel.    TryParse(Model(new JProperty("firmwareVersions",  new JArray(""))),            out _, out var error6),  Is.False);
                Assert.That(error6,                                                                                                 Does.Contain("A firmware version of the energy meter model 'X-1' must not be empty"));
                Assert.That(ChargingStationModel.TryParse(Model(new JProperty("maxPower",       0)),                         out _, out var error3),  Is.False);
                Assert.That(error3,                                                                                                 Does.Contain("must be positive"));
                Assert.That(ChargingStationModel.TryParse(Model(new JProperty("numberOfEVSEs",  0)),                         out _, out _),           Is.False);
                Assert.That(ChargingStationModel.TryParse(Model(new JProperty("manufacturerId", "ACME")),                    out _, out var error5),  Is.False);
                Assert.That(error5,                                                                                                 Does.Contain("embedded in its manufacturer"));
                Assert.That(EnergyMeterModel.    TryParse(Model(new JProperty("measurands",     new JArray("A", "A"))),      out _, out _),           Is.False);
                Assert.That(EnergyMeterModel.    TryParse(Model(new JProperty("maxCurrent",     -1)),                        out _, out _),           Is.False);
                Assert.That(EnergyMeterModel.    TryParse(Model(new JProperty("accuracyClass",  "")),                        out _, out _),           Is.False);

                Assert.That(() => new ChargingStationManufacturer(ACME, Models: [ new ChargingStationModel(DC300), new ChargingStationModel(DC300) ]),
                            Throws.ArgumentException.With.Message.Contains("must not be given twice"));

            }

        }

        #endregion

        #region A_manufacturer_states_its_models_itself()

        [Test]
        public void A_manufacturer_states_its_models_itself()
        {

            var rn1         = NewNetwork();
            var acme        = TestParties.Party(ACME);
            var meters      = TestParties.Party(METERS);
            var rno         = TestParties.Party(rn1);
            var dc150       = new ChargingStationModel(ChargingStationModel_Id.Parse("DC-150"), HardwareRevisions: [ "1.0" ]);
            var addModel    = RoamingNetworkChange.Add(dc150, ACME);
            var addMeter    = RoamingNetworkChange.Add(new EnergyMeterModel(EnergyMeterModel_Id.Parse("M-300")), METERS);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, acme,   addModel),  out var withModel, out var error1),  Is.True,  error1);
                Assert.That(withModel!.ChargingStationManufacturers.Single().Models.Ids.Select(id => id.ToString()),  Is.EqualTo(new[] { "AC-22", "DC-150", "DC-300" }));
                Assert.That(withModel. ChargingStationManufacturers.Single().LastChangeDate,                            Is.EqualTo(T0));

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, rno,    addModel),  out _, out var error2),              Is.False, "a model signed by the roaming network operator");
                Assert.That(error2,                                                                                       Does.Contain("needs a signature by a signing key of the ChargingStationManufacturer 'ACME'"));

                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, meters, addModel),  out _, out var error3),              Is.False, "a model of another manufacturer");
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, meters, addMeter),  out _, out var error4),              Is.True,  error4);

                // Its own properties it changes itself; the roaming network operator adds and removes it.
                Assert.That(rn1.TryApplyChangeSet(SignedBy(rn1, acme,   RoamingNetworkChange.UpdateProperty(rn1.ChargingStationManufacturers.Single(), "homepage", null, "https://acme.example")),
                                                  out _, out var error5),                                                Is.True,  error5);
                // Added with its keys and without its models, which are its own to add.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(new ChargingStationManufacturer(ZAP))),
                            Throws.ArgumentException.With.Message.Contains("must be added with its signingKeys"));
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(new ChargingStationManufacturer(ZAP, Models: [ dc150 ]).WithKeys())),
                            Throws.ArgumentException.With.Message.Contains("must be added without its chargingStationModels"));
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(new EnergyMeterManufacturer(EnergyMeterManufacturer_Id.Parse("NEW")))),
                            Throws.ArgumentException.With.Message.Contains("must be added with its signingKeys"));

                var withZAP = Apply(rn1, RoamingNetworkChange.Add(new ChargingStationManufacturer(ZAP).WithKeys()));
                Assert.That(withZAP.Parties.Select(party => party.Id.ToString()),                                         Does.Contain("the ChargingStationManufacturer 'ZAP'"));

                Assert.That(ErrorOf(withZAP, TestParties.Party(ZAP), RoamingNetworkChange.Remove(ZAP)),                   Does.Contain("roaming network operator"), "removed by itself");
                Assert.That(ErrorOf(withZAP, rno,                    RoamingNetworkChange.Remove(ZAP)),                   Is.Null);

            }

        }

        #endregion

        #region A_model_is_named_with_its_manufacturer()

        [Test]
        public void A_model_is_named_with_its_manufacturer()
        {

            var rn1      = Apply(NewNetwork(), RoamingNetworkChange.Add(new ChargingStationManufacturer(ZAP).WithKeys()));
            var rn2      = rn1.ApplyChangeSet(SignedBy(rn1, TestParties.Party(ZAP), RoamingNetworkChange.Add(new ChargingStationModel(DC300, HardwareRevisions: [ "7" ]), ZAP)));

            rn2.TryGetChargingStationManufacturerById(ACME, out var acme);
            rn2.TryGetChargingStationManufacturerById(ZAP,  out var zap);

            acme!.TryGetModel(DC300, out var acmeDC300);

            var update   = RoamingNetworkChange.UpdateProperty(acmeDC300!, ACME, "maxPower", 300000, 350000);
            var rn3      = rn2.ApplyChangeSet(SignedBy(rn2, TestParties.Party(ACME), update));

            rn3.TryGetChargingStationManufacturerById(ACME, out var acme3);
            rn3.TryGetChargingStationManufacturerById(ZAP,  out var zap3);

            using (Assert.EnterMultipleScope())
            {

                // The same identification at another manufacturer is another model.
                Assert.That(zap!.TryGetModel(DC300, out var zapDC300),                                         Is.True);
                Assert.That(zapDC300!.HardwareRevisions,                                                       Is.EqualTo(new[] { "7" }));

                Assert.That(acme3!.Models.Index[DC300].MaxPower,                                              Is.EqualTo(Watt.FromW(350000)));
                Assert.That(zap3! .Models.Index[DC300].MaxPower,                                              Is.Null);

                // Without its manufacturer, a change of a model is refused.
                Assert.That(ErrorOf(rn2, TestParties.Party(ACME), RoamingNetworkChange.UpdateProperty(nameof(ChargingStationModel), "DC-300", "maxPower", null, 1)),
                            Does.Contain("needs its ChargingStationManufacturer"));

                Assert.That(ErrorOf(rn2, TestParties.Party(ACME), RoamingNetworkChange.UpdateProperty(acmeDC300!, ACME, "manufacturerId", null, "ZAP")),
                            Does.Contain("can not be changed"));

                Assert.That(ErrorOf(rn2, TestParties.Party(ACME), RoamingNetworkChange.Add(new ChargingStationModel(AC22), ACME)),
                            Does.Contain("already exists"));

                Assert.That(ErrorOf(rn2, TestParties.Party(ACME), RoamingNetworkChange.Remove(ChargingStationModel_Id.Parse("DC-999"), ACME)),
                            Does.Contain("does not exist"));

                // A model the charging station names stays.
                Assert.That(ErrorOf(rn2, TestParties.Party(ACME), RoamingNetworkChange.Remove(DC300, ACME)),
                            Does.Contain("The model 'DC-300' of the charging station 'DE*GEF*S1' is unknown at its manufacturer 'ACME'"));

                Assert.That(rn2.TryApplyChangeSet(SignedBy(rn2, TestParties.Party(ACME), RoamingNetworkChange.Remove(AC22, ACME)),
                                                  out var rn4, out var error6),                               Is.True, error6);
                Assert.That(rn4!.ChargingStationManufacturers.Index[ACME].Models.Ids.Select(id => id.ToString()),  Is.EqualTo(new[] { "DC-300" }));

            }

        }

        #endregion

        #region Stations_and_meters_name_models_of_their_manufacturers()

        [Test]
        public void Stations_and_meters_name_models_of_their_manufacturers()
        {

            var rn1      = NewNetwork();
            var station  = rn1.ChargingStationOperators.Single().ChargingStations.Single();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => NewNetwork(StationModel: ChargingStationModel_Id.Parse("DC-999")),
                            Throws.ArgumentException.With.Message.Contains("The model 'DC-999' of the charging station 'DE*GEF*S1' is unknown at its manufacturer 'ACME'"));

                Assert.That(() => NewNetwork(MeterModel: EnergyMeterModel_Id.Parse("M-999")),
                            Throws.ArgumentException.With.Message.Contains("The energy meter 'meter-1' names the model 'M-999', unknown at its manufacturer 'METERS'"));

                // A model without its manufacturer.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.RemoveProperty(station, "manufacturerId")),
                            Throws.ArgumentException.With.Message.Contains("The model 'DC-300' of the charging station 'DE*GEF*S1' is named without its manufacturer"));

                // Another model of its manufacturer, by a change of its operator: with a revision and a firmware of it.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(station, "modelId", "DC-300", "AC-22")),
                            Throws.ArgumentException.With.Message.Contains("The hardware revision '1.1' of the charging station 'DE*GEF*S1' is no hardware revision of its model 'AC-22' of 'ACME'"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(station, "modelId",          "DC-300", "AC-22"),
                                             RoamingNetworkChange.RemoveProperty(station, "hardwareRevision"),
                                             RoamingNetworkChange.RemoveProperty(station, "firmwareVersion")),
                            Throws.Nothing);

                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(station, "modelId", "DC-300", "DC-999")),
                            Throws.ArgumentException.With.Message.Contains("is unknown at its manufacturer"));

            }

        }

        #endregion

        #region Stations_and_meters_name_a_hardware_revision_and_a_firmware_version_of_their_model()

        [Test]
        public void Stations_and_meters_name_a_hardware_revision_and_a_firmware_version_of_their_model()
        {

            var rn1      = NewNetwork();
            var station  = rn1.ChargingStationOperators.Single().ChargingStations.Single();
            var meter    = station.EVSEs.Single().EnergyMeter!;

            rn1.TryGetChargingStationManufacturerById(ACME, out var acme);
            acme!.TryGetModel(DC300, out var dc300);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(station.HardwareRevision,                                        Is.EqualTo("1.1"));
                Assert.That(station.FirmwareVersion,                                         Is.EqualTo("4.3"));
                Assert.That(station.ToJSON()["hardwareRevision"]!.ToString(),                Is.EqualTo("1.1"));
                Assert.That(meter.  ToJSON()["hardwareRevision"]!.ToString(),                Is.EqualTo("3.0"));
                Assert.That(meter.  ToJSON()["hardwareVersion"],                             Is.Null);

                Assert.That(() => NewNetwork(StationFirmware: "4.4"),
                            Throws.ArgumentException.With.Message.Contains("The firmware version '4.4' of the charging station 'DE*GEF*S1' is no firmware version of its model 'DC-300' of 'ACME'"));

                Assert.That(() => NewNetwork(MeterRevision: "2.0"),
                            Throws.ArgumentException.With.Message.Contains("The energy meter 'meter-1' names the hardware revision '2.0', no hardware revision of its model 'M-500' of 'METERS'"));

                // Another revision of its model, by a change of its operator.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(station, "hardwareRevision", "1.1", "1.0")),
                            Throws.Nothing);

                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(station, "hardwareRevision", "1.1", "4.2")),
                            Throws.ArgumentException.With.Message.Contains("The hardware revision '4.2' of the charging station 'DE*GEF*S1' is no hardware revision of its model"));

                // A revision without its model.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.RemoveProperty(station, "modelId")),
                            Throws.ArgumentException.With.Message.Contains("The hardware revision '1.1' of the charging station 'DE*GEF*S1' is named without its model"));

                // A firmware version the station runs stays one of its model.
                Assert.That(ErrorOf(rn1, TestParties.Party(ACME), RoamingNetworkChange.UpdateProperty(dc300!, ACME, "firmwareVersions", new JArray("4.2", "4.3"), new JArray("4.2"))),
                            Does.Contain("The firmware version '4.3' of the charging station 'DE*GEF*S1' is no firmware version of its model 'DC-300' of 'ACME'"));

                Assert.That(ErrorOf(rn1, TestParties.Party(ACME), RoamingNetworkChange.UpdateProperty(dc300!, ACME, "firmwareVersions", new JArray("4.2", "4.3"), new JArray("4.2", "4.3", "4.4"))),
                            Is.Null);

            }

        }

        #endregion

        #region Certificates_name_a_model_and_hardware_revisions_and_firmware_versions_of_it()

        [Test]
        public void Certificates_name_a_model_and_hardware_revisions_and_firmware_versions_of_it()
        {

            TransparencySoftwareCertificate Certificate(ChargingStationManufacturer_Id  ManufacturerId,
                                                        ChargingStationModel_Id         ModelId,
                                                        String                          HardwareRevision,
                                                        String                          FirmwareVersion   = "4.3")

                => new (TransparencySoftwareCertificate_Id.Parse("cert-1"), ManufacturerId, ModelId, [ HardwareRevision ], [ FirmwareVersion ], [ Chargy10 ], []);

            RoamingNetwork With(TransparencySoftwareCertificate Certificate)

                => new (RoamingNetwork_Id.Parse("rn1"),
                        Created:                            Created,
                        ChargingStationManufacturers:       [ new ChargingStationManufacturer(ACME, Models: [ NewDC300() ], Created: Created) ],
                        TransparencySoftwareManufacturers:  [ new TransparencySoftwareManufacturer(TransparencySoftwareManufacturer_Id.Parse("OCC"),
                                                                                                   TransparencySoftware:  [ new TransparencySoftware(Chargy10,
                                                                                                                                                     ImmutableI18NString.Create(Languages.en, "Chargy"),
                                                                                                                                                     TransparencySoftwareVersion.Parse("1.0"),
                                                                                                                                                     [ OpenSourceLicense.AGPL3 ]) ],
                                                                                                   Created:               Created) ],
                        ConformityAssessmentBodies:         [ new ConformityAssessmentBody(ConformityAssessmentBody_Id.Parse("PTB"),
                                                                                           TransparencySoftwareCertificates:  [ Certificate ],
                                                                                           Created:                           Created) ]);

            var json = Certificate(ACME, DC300, "1.1").ToJSON();
            json.Remove("chargingStationManufacturerId");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => With(Certificate(ACME, DC300, "1.1")),                       Throws.Nothing);

                Assert.That(() => With(Certificate(ZAP,  DC300, "1.1")),                       Throws.ArgumentException.With.Message.Contains("The manufacturer 'ZAP' of the certificate 'cert-1' is unknown"));
                Assert.That(() => With(Certificate(ACME, AC22,  "1.1")),                       Throws.ArgumentException.With.Message.Contains("The charging station model 'AC-22' of the certificate 'cert-1' is unknown at its manufacturer 'ACME'"));
                Assert.That(() => With(Certificate(ACME, DC300, "2.0")),                       Throws.ArgumentException.With.Message.Contains("The hardware revision '2.0' of the certificate 'cert-1' is no hardware revision of the charging station model 'DC-300' of 'ACME'"));
                Assert.That(() => With(Certificate(ACME, DC300, "1.1", "1.1")),                Throws.ArgumentException.With.Message.Contains("The firmware version '1.1' of the certificate 'cert-1' is no firmware version of the charging station model 'DC-300' of 'ACME'"));

                Assert.That(json["hardwareRevisions"]!.Select(revision => revision.ToString()),   Is.EqualTo(new[] { "1.1" }));
                Assert.That(json["firmwareVersions"]!. Select(version  => version. ToString()),   Is.EqualTo(new[] { "4.3" }));

                // At least one of each, and no longer the one version of before.
                Assert.That(() => new TransparencySoftwareCertificate(TransparencySoftwareCertificate_Id.Parse("cert-2"), ACME, DC300, [ "1.1" ], [], [ Chargy10 ], []),
                            Throws.ArgumentException.With.Message.Contains("The certificate 'cert-2' must name at least one firmware version of its model"));

                var withVersion = Certificate(ACME, DC300, "1.1").ToJSON();
                withVersion.Add("chargingStationModelVersion", "1.1");
                Assert.That(TransparencySoftwareCertificate.TryParse(withVersion, out _, out var versionError),  Is.False);
                Assert.That(versionError,                                                                         Does.Contain("chargingStationModelVersion"));

                // The manufacturer of the model is no longer optional.
                Assert.That(TransparencySoftwareCertificate.TryParse(json, out _, out var error),  Is.False);
                Assert.That(error,                                                                 Does.Contain("chargingStationManufacturerId"));

            }

        }

        #endregion

        #region Models_need_a_key_that_may_sign_models()

        [Test]
        public void Models_need_a_key_that_may_sign_models()
        {

            var (partyKey,  _)  = TestParties.Key(new POIPartyId(nameof(ChargingStationManufacturer), "ACME/party"));
            var (modelsKey, _)  = TestParties.Key(new POIPartyId(nameof(ChargingStationManufacturer), "ACME/models"));

            var json = NewNetwork().ToJSON();
            json["chargingStationManufacturers"]![0]!["signingKeys"] = new JArray(POIPublicKey.From(partyKey,  Usages: [ POIKeyUsages.Party  ]).ToJSON(Embedded: true),
                                                                                    POIPublicKey.From(modelsKey, Usages: [ POIKeyUsages.Models ]).ToJSON(Embedded: true));

            Assert.That(RoamingNetwork.TryParse(json, out var rn1, out var error),  Is.True, error);

            var addModel  = RoamingNetworkChange.Add(new ChargingStationModel(ChargingStationModel_Id.Parse("DC-150")), ACME);
            var byParty   = rn1!.CreateChangeSet([ addModel ], CreatedAt: T0).Sign(partyKey,  POIPublicKey.From(partyKey). Id, COSEAlgorithm.ES256);
            var byModels  = rn1. CreateChangeSet([ addModel ], CreatedAt: T0).Sign(modelsKey, POIPublicKey.From(modelsKey).Id, COSEAlgorithm.ES256);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn1.TryApplyChangeSet(byParty,  out _, out var error1),  Is.False, "a model by the key of the party");
                Assert.That(error1,                                                   Does.Contain("that may sign 'models'"));
                Assert.That(rn1.TryApplyChangeSet(byModels, out _, out var error2),  Is.True,  error2);
            }

        }

        #endregion

        #region Changes_of_models_on_two_branches_merge()

        [Test]
        public void Changes_of_models_on_two_branches_merge()
        {

            var rn1       = Apply(NewNetwork(), RoamingNetworkChange.Add(new ChargingStationManufacturer(ZAP).WithKeys()));

            rn1.TryGetChargingStationManufacturerById(ACME, out var acme);
            acme!.TryGetModel(DC300, out var dc300);
            acme. TryGetModel(AC22,  out var ac22);

            // The same identification at two manufacturers, changed on both branches.
            var left      = Apply(rn1, RoamingNetworkChange.UpdateProperty(dc300!, ACME, "maxPower", 300000, 350000),
                                       RoamingNetworkChange.Add(new ChargingStationModel(DC300, HardwareRevisions: [ "7" ]), ZAP));
            var right     = Apply(rn1, RoamingNetworkChange.UpdateProperty(ac22!,  ACME, "numberOfEVSEs", null, 1),
                                       RoamingNetworkChange.UpdateProperty(dc300!, ACME, "datasheetURL", "https://acme.example/dc-300/datasheet.pdf", "https://acme.example/dc-300/v2.pdf"));
            var other     = Apply(rn1, RoamingNetworkChange.UpdateProperty(dc300!, ACME, "maxPower", 300000, 400000));

            var merged    = RoamingNetwork.TryMerge(rn1, left,  right, ChangeSet_Id.Parse("merge-1"), out var merge, out _, out var conflicts1);
            var conflict  = RoamingNetwork.TryMerge(rn1, left,  other, ChangeSet_Id.Parse("merge-2"), out _,         out _, out var conflicts2);
            var removed   = RoamingNetwork.TryMerge(rn1, right, Apply(rn1, RoamingNetworkChange.Remove(AC22, ACME)), ChangeSet_Id.Parse("merge-3"), out _, out _, out var conflicts3);

            merge!.TryGetChargingStationManufacturerById(ACME, out acme);
            merge. TryGetChargingStationManufacturerById(ZAP,  out var zap);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(merged,                                                              Is.True, String.Join(" ", conflicts1.Select(conflict => conflict.Description)));
                Assert.That(acme!.Models.Index[DC300].MaxPower,                                 Is.EqualTo(Watt.FromW(350000)));
                Assert.That(acme. Models.Index[DC300].DatasheetURL?.ToString(),                 Is.EqualTo("https://acme.example/dc-300/v2.pdf"));
                Assert.That(acme. Models.Index[AC22]. NumberOfEVSEs,                            Is.EqualTo(1));
                Assert.That(zap!. Models.Index[DC300].HardwareRevisions,                        Is.EqualTo(new[] { "7" }));

                Assert.That(conflict,                                                            Is.False);
                Assert.That(conflicts2.Single().PropertyName,                                    Is.EqualTo("maxPower"));

                // Changed on the one, removed on the other branch.
                Assert.That(removed,                                                             Is.False);
                Assert.That(conflicts3.Single().Kind,                                            Is.EqualTo(RoamingNetworkMergeConflictKind.DeleteModify));

            }

        }

        #endregion

    }

}
