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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// What a charging pool, a charging station and an EVSE tell about their location:
    /// time zone, facilities, payment and authentication, entrance and exit, parking,
    /// vehicle types, calibration, the power of a station and the reservation of an EVSE.
    /// </summary>
    [TestFixture]
    public class LocationDetailsTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-12T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly Brand_Id                    Fast     = Brand_Id.                  Parse("fast-charge");
        private static readonly ParkingSpace_Id             SP1      = ParkingSpace_Id.           Parse("space-1");
        private static readonly ParkingSpace_Id             SP2      = ParkingSpace_Id.           Parse("space-2");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(P1,
                                                                                                                TimeZone:             Time_Zone.Parse("Europe/Berlin"),
                                                                                                                Facilities:           [ Facility.RESTAURANT, Facility.HOTEL ],
                                                                                                                PaymentOptions:       [ PaymentOptions.Direct, PaymentOptions.Contract ],
                                                                                                                AuthenticationModes:  [ AuthenticationMode.SMS("+49 800 1234", "4711"),
                                                                                                                                        AuthenticationMode.RFID([ RFIDCardTypes.MifareDESFire, RFIDCardTypes.MifareClassic ], [ new Brand(Fast, ImmutableI18NString.Create(Languages.en, "Fast Charge")) ]),
                                                                                                                                        AuthenticationMode.CreditCard ],
                                                                                                                UIFeatures:           [ UIFeatures.Screen, UIFeatures.Pinpad ],
                                                                                                                Features:             [ ChargingPoolFeature.Reservable, ChargingPoolFeature.GermanCalibrationLaw ],
                                                                                                                LocationLanguages:    [ Languages.de, Languages.en ],
                                                                                                                EntranceAddress:      new Address("Hauptstraße", "07743", I18NString.Create(Languages.de, "Jena"), Country.Germany, HouseNumber: "1"),
                                                                                                                EntranceLocation:     GeoCoordinate.Parse(50.9270, 11.5890),
                                                                                                                ExitAddress:          new Address("Nebenstraße", "07743", I18NString.Create(Languages.de, "Jena"), Country.Germany, HouseNumber: "2"),
                                                                                                                ExitLocation:         GeoCoordinate.Parse(50.9275, 11.5895),
                                                                                                                ArrivalInstructions:  I18NString.Create(Languages.en, "Second floor of the car park"),
                                                                                                                ChargingWhenClosed:   false,
                                                                                                                ParkingRestrictions:  [ ParkingRestrictionGroup.EV_ONLY, ParkingRestrictionGroup.CUSTOMERS ],
                                                                                                                RelatedLocations:     [ new AdditionalGeoLocation(GeoCoordinate.Parse(50.9280, 11.5900), ImmutableI18NString.Create(Languages.en, "Walkway")),
                                                                                                                                        new AdditionalGeoLocation(GeoCoordinate.Parse(50.9260, 11.5880)) ],
                                                                                                                NUTSCodes:            [ NUTSCode.Parse("deg03"), NUTSCode.Parse("DEG") ],
                                                                                                                Created:              Created,
                                                                                                                ChargingStations:     [ new ChargingStation(S1,
                                                                                                                                                            VehicleTypes:       [ VehicleType.VAN, VehicleType.MOTORCYCLE ],
                                                                                                                                                            EUVehicleCategories: [ EUVehicleCategory.M1, EUVehicleCategory.L3 ],
                                                                                                                                                            Accessibility:      AccessibilityType.RestrictedAccess,
                                                                                                                                                            Features:           [ ChargingStationFeature.StatusInfoAvailable, ChargingStationFeature.Roofed ],
                                                                                                                                                            CalibrationInfo:    URL.Parse("https://gef.example/calibration"),
                                                                                                                                                            CertificationInfo:  URL.Parse("https://gef.example/certification"),
                                                                                                                                                            MaxPower:           Watt.FromW(44000),
                                                                                                                                                            ParkingSpaceIds:    [ SP2, SP1 ],
                                                                                                                                                            Created:            Created,
                                                                                                                                                            EVSEs:              [ new EVSE(E1,
                                                                                                                                                                                           GeoLocation:             GeoCoordinate.Parse(50.9271, 11.5892),
                                                                                                                                                                                           MaxReservationDuration:  TimeSpan.FromMinutes(30),
                                                                                                                                                                                           Created:                 Created) ]) ]) ]) ],
                    ParkingOperators:          [ new ParkingOperator(ParkingOperator_Id.Parse("DE*PAR"),
                                                                     Created:        Created,
                                                                     ParkingSpaces:  [ new ParkingSpace(SP1, Created: Created),
                                                                                       new ParkingSpace(SP2, Created: Created) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion

        #region Pool(Network) / Station(Network) / EVSE(Network)

        private static JObject Pool   (JObject Network)
            => (JObject) Network["chargingStationOperators"]![0]!["chargingPools"]![0]!;

        private static JObject Station(JObject Network)
            => (JObject) Pool(Network)["chargingStations"]![0]!;

        private static JObject EVSE   (JObject Network)
            => (JObject) Station(Network)["EVSEs"]![0]!;

        #endregion


        #region Location_details_survive_JSON_JSON_text_and_CBOR()

        [Test]
        public void Location_details_survive_JSON_JSON_text_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var pool     = Pool   (json);
            var station  = Station(json);
            var evse     = EVSE   (json);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                            Is.EqualTo(rn1.CalcETags()));
                Assert.That(fromCBOR.CalcETags(),                                            Is.EqualTo(rn1.CalcETags()));

                // Sets in the order of their names, languages in their given order.
                Assert.That(pool["timeZone"]!.Value<String>(),                               Is.EqualTo("Europe/Berlin"));
                Assert.That(pool["facilities"]!.Values<String>(),                            Is.EqualTo(new[] { "HOTEL", "RESTAURANT" }));
                Assert.That(pool["paymentOptions"]!.Values<String>(),                        Is.EqualTo(new[] { "Contract", "Direct" }));
                Assert.That(pool["authenticationModes"]!.Select(mode => mode["type"]!.Value<String>()),
                                                                                             Is.EqualTo(new[] { "CreditCard", "RFID", "SMS" }));
                Assert.That(pool["authenticationModes"]![1]!["cardTypes"]!.Values<String>(),  Is.EqualTo(new[] { "MifareClassic", "MifareDESFire" }));
                Assert.That(pool["authenticationModes"]![1]!["brands"]!.Select(brand => brand["@id"]!.ToString()),  Is.EqualTo(new[] { "fast-charge" }));
                Assert.That(pool["authenticationModes"]![2]!["stationCode"]!.Value<String>(), Is.EqualTo("4711"));
                Assert.That(pool["uiFeatures"]!.Values<String>(),                            Is.EqualTo(new[] { "Pinpad", "Screen" }));
                Assert.That(pool["features"]!.Values<String>(),                              Is.EqualTo(new[] { "GermanCalibrationLaw", "Reservable" }));
                Assert.That(pool["locationLanguages"]!.Values<String>(),                     Is.EqualTo(new[] { "de", "en" }));
                Assert.That(pool["entranceAddress"]!["street"]!.Value<String>(),             Is.EqualTo("Hauptstraße"));
                Assert.That(pool["exitLocation"],                                            Is.Not.Null);
                Assert.That(pool["arrivalInstructions"]!["en"]!.Value<String>(),             Is.EqualTo("Second floor of the car park"));
                Assert.That(pool["chargingWhenClosed"]!.Value<Boolean>(),                    Is.False, "false is written, unknown is not.");
                Assert.That(pool["parkingRestrictions"]!.Values<String>(),                   Is.EqualTo(new[] { "CUSTOMERS", "EV_ONLY" }));
                Assert.That(pool["relatedLocations"]!.Count(),                               Is.EqualTo(2));
                Assert.That(pool["relatedLocations"]![0]!["name"]!["en"]!.Value<String>(),   Is.EqualTo("Walkway"));

                Assert.That(station["vehicleTypes"]!.Values<String>(),                       Is.EqualTo(new[] { "MOTORCYCLE", "VAN" }));
                Assert.That(station["euVehicleCategories"]!.Values<String>(),                Is.EqualTo(new[] { "L3", "M1" }));
                Assert.That(station["accessibility"]!.Value<String>(),                       Is.EqualTo("RestrictedAccess"));
                Assert.That(station["features"]!.Values<String>(),                           Is.EqualTo(new[] { "Roofed", "StatusInfoAvailable" }));
                Assert.That(pool["nutsCodes"]!.Values<String>(),                             Is.EqualTo(new[] { "DEG", "DEG03" }));
                Assert.That(station["calibrationInfo"]!.Value<String>(),                     Is.EqualTo("https://gef.example/calibration"));
                Assert.That(station["maxPower"]!.Value<Decimal>(),                           Is.EqualTo(44000m));
                Assert.That(station["parkingSpaceIds"]!.Values<String>(),                    Is.EqualTo(new[] { "space-1", "space-2" }));

                Assert.That(evse["geoLocation"],                                             Is.Not.Null);
                Assert.That(evse["maxReservationDuration"]!.Value<Decimal>(),                Is.EqualTo(1800m), "In seconds.");

                var p1 = fromCBOR.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
                var s1 = p1.ChargingStations.Single();
                var e1 = s1.EVSEs.Single();

                Assert.That(p1.TimeZone,                                                     Is.EqualTo(Time_Zone.Parse("Europe/Berlin")));
                Assert.That(p1.PaymentOptions,                                               Is.EqualTo(new[] { PaymentOptions.Contract, PaymentOptions.Direct }));
                Assert.That(p1.AuthenticationModes,                                          Does.Contain(AuthenticationMode.CreditCard));
                Assert.That(p1.AuthenticationModes.Single(mode => mode.Type == "SMS").Number, Is.EqualTo("+49 800 1234"));
                Assert.That(p1.LocationLanguages,                                            Is.EqualTo(new[] { Languages.de, Languages.en }));
                Assert.That(p1.ChargingWhenClosed,                                           Is.False);
                Assert.That(p1.RelatedLocations[1].Name,                                     Is.Null);
                Assert.That(s1.MaxPower?.Value,                                              Is.EqualTo(44000m));
                Assert.That(s1.ParkingSpaceIds,                                              Is.EqualTo(new[] { SP1, SP2 }));
                Assert.That(e1.MaxReservationDuration,                                       Is.EqualTo(TimeSpan.FromMinutes(30)));
                Assert.That(e1.GeoLocation,                                                  Is.EqualTo(GeoCoordinate.Parse(50.9271, 11.5892)));

            }

        }

        #endregion

        #region Invalid_location_details_are_refused()

        [Test]
        public void Invalid_location_details_are_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("payment option twice",      Changed(network => Pool(network)["paymentOptions"]     = new JArray("Cash", "cash")),                                   "must not be given twice"),
                ("empty payment option",      Changed(network => Pool(network)["paymentOptions"]     = new JArray(" ")),                                              "payment option ' '"),
                ("unknown language",          Changed(network => Pool(network)["locationLanguages"]  = new JArray("klingon")),                                        "location language 'klingon'"),
                ("language twice",            Changed(network => Pool(network)["locationLanguages"]  = new JArray("de", "de")),                                       "must not be given twice"),
                ("authentication type twice", Changed(network => ((JArray) Pool(network)["authenticationModes"]!).Add(new JObject(new JProperty("type", "CreditCard")))), "must not be given twice"),
                ("SMS without number",        Changed(network => ((JObject) Pool(network)["authenticationModes"]![2]!).Remove("number")),                     "needs its number"),
                ("card types not for RFID",   Changed(network => Pool(network)["authenticationModes"]![0]!["cardTypes"] = new JArray("Calypso")),             "Only RFID"),
                ("RFID brand ids",            Changed(network => Pool(network)["authenticationModes"]![1]!["brandIds"]  = new JArray("fast-charge")),         "must be given completely in 'brands', not by 'brandIds'"),
                ("chargingWhenClosed text",   Changed(network => Pool(network)["chargingWhenClosed"] = "no"),                                                 "charging when closed"),
                ("entrance not an object",    Changed(network => Pool(network)["entranceLocation"]   = "here"),                                               "entrance location"),
                ("related without location",  Changed(network => ((JObject) Pool(network)["relatedLocations"]![0]!).Remove("geoLocation")),                   "related location"),
                ("EU category twice",         Changed(network => Station(network)["euVehicleCategories"] = new JArray("M1", "m1")),                           "must not be given twice"),
                ("empty EU category",         Changed(network => Station(network)["euVehicleCategories"] = new JArray(" ")),                                  "EU vehicle category ' '"),
                ("NUTS code too long",        Changed(network => Pool(network)["nutsCodes"] = new JArray("DEG0301")),                                       "NUTS code 'DEG0301'"),
                ("NUTS code without country", Changed(network => Pool(network)["nutsCodes"] = new JArray("1")),                                             "NUTS code '1'"),
                ("negative station power",    Changed(network => Station(network)["maxPower"]        = -1),                                                   "must not be negative"),
                ("unknown parking space",     Changed(network => Station(network)["parkingSpaceIds"] = new JArray("space-9")),                                "parking space 'space-9' of the charging station 'DE*GEF*S1'"),
                ("invalid calibration URL",   Changed(network => Station(network)["calibrationInfo"] = 42),                                                   "calibration information"),
                ("negative reservation",      Changed(network => EVSE(network)["maxReservationDuration"] = -60),                                              "must not be negative")
            };

            using (Assert.EnterMultipleScope())
            {
                foreach (var (name, invalid, error) in cases)
                {
                    Assert.That(RoamingNetwork.TryParse(invalid, out _, out var errorResponse),  Is.False, name);
                    Assert.That(errorResponse,                                                   Does.Contain(error), name);
                }
            }

        }

        #endregion

        #region Location_details_are_changed_by_change_sets()

        [Test]
        public void Location_details_are_changed_by_change_sets()
        {

            var rn1      = NewNetwork();
            var pool     = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            var station  = pool.ChargingStations.Single();
            var evse     = station.EVSEs.Single();

            var rn2      = Apply(rn1,
                                 RoamingNetworkChange.UpdateProperty(pool,     "timeZone",                null, "Europe/Paris"),
                                 RoamingNetworkChange.UpdateProperty(pool,     "paymentOptions",          null, new JArray("Free")),
                                 RoamingNetworkChange.UpdateProperty(pool,     "chargingWhenClosed",      null, true),
                                 RoamingNetworkChange.UpdateProperty(pool,     "nutsCodes",               null, new JArray("DEG")),
                                 RoamingNetworkChange.RemoveProperty(pool,     "relatedLocations"),
                                 RoamingNetworkChange.UpdateProperty(station,  "parkingSpaceIds",         null, new JArray("space-2")),
                                 RoamingNetworkChange.UpdateProperty(station,  "maxPower",                null, 22000),
                                 RoamingNetworkChange.UpdateProperty(station,  "euVehicleCategories",     null, new JArray("N1", "M1")),
                                 RoamingNetworkChange.UpdateProperty(evse,     "maxReservationDuration",  null, 900),
                                 RoamingNetworkChange.RemoveProperty(evse,     "geoLocation"));

            var pool2    = rn2.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            var station2 = pool2.ChargingStations.Single();
            var evse2    = station2.EVSEs.Single();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(pool2.TimeZone,                Is.EqualTo(Time_Zone.Parse("Europe/Paris")));
                Assert.That(pool2.PaymentOptions,          Is.EqualTo(new[] { PaymentOptions.Free }));
                Assert.That(pool2.ChargingWhenClosed,      Is.True);
                Assert.That(pool2.NUTSCodes,               Is.EqualTo(new[] { NUTSCode.Parse("DEG") }));
                Assert.That(pool2.NUTSCodes.Single().Level, Is.EqualTo(1));
                Assert.That(pool2.RelatedLocations,        Is.Empty);
                Assert.That(station2.ParkingSpaceIds,      Is.EqualTo(new[] { SP2 }));
                Assert.That(station2.MaxPower?.Value,      Is.EqualTo(22000m));
                Assert.That(station2.EUVehicleCategories,  Is.EqualTo(new[] { EUVehicleCategory.M1, EUVehicleCategory.N1 }));
                Assert.That(evse2.MaxReservationDuration,  Is.EqualTo(TimeSpan.FromMinutes(15)));
                Assert.That(evse2.GeoLocation,             Is.Null);

                // A parking space still named by a charging station can not be removed.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(SP2)),
                            Throws.ArgumentException.With.Message.Contains("parking space 'space-2' of the charging station 'DE*GEF*S1'"));

                // ...but one no longer named can.
                Assert.That(Apply(rn2, RoamingNetworkChange.Remove(SP1)).ParkingSpaces.Count(),
                            Is.EqualTo(1));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(pool2, "authenticationModes", null, new JArray(new JObject(new JProperty("type", "PhoneCall"))))),
                            Throws.ArgumentException.With.Message.Contains("needs its number"));

            }

        }

        #endregion

    }

}
