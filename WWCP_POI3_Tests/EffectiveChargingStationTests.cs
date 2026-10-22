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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The order of the templates of a member, and charging stations taking the properties their templates give them at
    /// runtime.
    /// </summary>
    [TestFixture]
    public class EffectiveChargingStationTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              Changed  = DateTimeOffset.Parse("2026-06-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly ChargingStation_Id          S2       = ChargingStation_Id.        Parse("DE*GEF*S2");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly EVSE_Id                     E3       = EVSE_Id.                   Parse("DE*GEF*E2*1");
        private static readonly ChargingStationTemplate_Id  All      = ChargingStationTemplate_Id.Parse("DE*GEF*ST1");
        private static readonly ChargingStationTemplate_Id  Premium  = ChargingStationTemplate_Id.Parse("DE*GEF*ST2");
        private static readonly Brand_Id                    Blue     = Brand_Id.                  Parse("blue-charge");

        private static readonly PhoneNumber                 Hotline1 = PhoneNumber.Parse("+49 800 111111");
        private static readonly PhoneNumber                 Hotline2 = PhoneNumber.Parse("+49 800 222222");

        #endregion

        #region NewNetwork(PremiumFirst = true)

        /// <summary>
        /// Two charging stations taking a template of all of them, which gives them a hotline, an address, a feature, a
        /// brand and a maximum power, and the first one also a premium template, by default before the other one, which
        /// gives it another hotline, the same brand by another name and a vehicle type. The first one has an accessibility
        /// and a feature of its own.
        /// </summary>
        private static RoamingNetwork NewNetwork(Boolean PremiumFirst = true)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    GEF,
                                                    Created:                   Created,
                                                    ChargingPools:             [ new ChargingPool(P1,
                                                                                                  Created:           Created,
                                                                                                  ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                           Accessibility:  AccessibilityType.FreePubliclyAccessible,
                                                                                                                                           Features:       [ ChargingStationFeature.Roofed ],
                                                                                                                                           TemplateIds:    PremiumFirst ? [ Premium, All ] : [ All, Premium ],
                                                                                                                                           Created:        Created,
                                                                                                                                           EVSEs:          [ new EVSE(E1, MaxPower: Watt.FromW(22000), Created: Created),
                                                                                                                                                             new EVSE(E2, MaxPower: Watt.FromW(22000), Created: Created) ]),
                                                                                                                       new ChargingStation(S2,
                                                                                                                                           TemplateIds:    [ All ],
                                                                                                                                           Created:        Created,
                                                                                                                                           EVSEs:          [ new EVSE(E3, Created: Created) ]) ]) ],
                                                    ChargingStationTemplates:  [ new ChargingStationTemplate(All,
                                                                                                             Address:             new Address("Hauptstraße", "07743", I18NString.Create(Languages.de, "Jena"), Country.Germany, HouseNumber: "1"),
                                                                                                             HotlinePhoneNumber:  Hotline1,
                                                                                                             Accessibility:       AccessibilityType.PayingPubliclyAccessible,
                                                                                                             Features:            [ ChargingStationFeature.StatusInfoAvailable ],
                                                                                                             Brands:              [ new Brand(Blue, ImmutableI18NString.Create(Languages.en, "Blue Charge")) ],
                                                                                                             MaxPower:            Watt.FromW(30000),
                                                                                                             Created:             Created),
                                                                                 new ChargingStationTemplate(Premium,
                                                                                                             HotlinePhoneNumber:  Hotline2,
                                                                                                             VehicleTypes:        [ VehicleType.MOTORCYCLE ],
                                                                                                             Brands:              [ new Brand(Blue, ImmutableI18NString.Create(Languages.en, "Blue Charge Premium")) ],
                                                                                                             Created:             Created,
                                                                                                             LastChange:          Changed) ]) ]).WithKeys();

        #endregion

        #region (static) GEFOf(Network)

        private static ChargingStationOperator GEFOf(RoamingNetwork Network)

            => Network.GetChargingStationOperatorById(GEF)!;

        #endregion


        #region Neither_templates_nor_groups_have_a_priority()

        [Test]
        public void Neither_templates_nor_groups_have_a_priority()
        {

            var entities = new (String Name, JObject JSON)[] {
                ("EVSE template",              new EVSETemplate           (EVSETemplate_Id.           Parse("DE*GEF*ET1"), Created: Created).ToJSON()),
                ("charging station template",  new ChargingStationTemplate(ChargingStationTemplate_Id.Parse("DE*GEF*ST1"), Created: Created).ToJSON()),
                ("charging pool template",     new ChargingPoolTemplate   (ChargingPoolTemplate_Id.   Parse("DE*GEF*PT1"), Created: Created).ToJSON()),
                ("charging tariff template",   new ChargingTariffTemplate (ChargingTariffTemplate_Id. Parse("DE*GEF*TT1"), Created: Created).ToJSON()),
                ("parking space template",     new ParkingSpaceTemplate   (ParkingSpaceTemplate_Id.   Parse("covered"),    Created: Created).ToJSON()),
                ("EVSE group",                 new EVSEGroup              (EVSEGroup_Id.              Parse("DE*GEF*EG1"), Created: Created).ToJSON()),
                ("charging tariff group",      new ChargingTariffGroup    (ChargingTariffGroup_Id.    Parse("DE*GEF*TG1"), Created: Created).ToJSON()),
                ("parking space group",        new ParkingSpaceGroup      (ParkingSpaceGroup_Id.      Parse("level-1"),    Created: Created).ToJSON())
            };

            using (Assert.EnterMultipleScope())
            {
                foreach (var (name, json) in entities)
                {
                    Assert.That(json["priority"],   Is.Null, name);
                    Assert.That(json["@id"],        Is.Not.Null, name);
                }
            }

        }

        #endregion

        #region A_charging_station_template_has_the_properties_of_a_charging_station()

        [Test]
        public void A_charging_station_template_has_the_properties_of_a_charging_station()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var all = json["chargingStationOperators"]![0]!["chargingStationTemplates"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                      Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                      Is.EqualTo(json.ToString()));

                // Named as a charging station names them.
                Assert.That(all["hotline"]!.Value<String>(),                    Is.EqualTo(Hotline1.ToString()));
                Assert.That(all["address"]!["city"],                            Is.Not.Null);
                Assert.That(all["accessibility"]!.Value<String>(),              Is.EqualTo("PayingPubliclyAccessible"));
                Assert.That(all["features"]!.Values<String>(),                  Is.EqualTo(new[] { "StatusInfoAvailable" }));
                Assert.That(all["brands"]![0]!["@id"]!.Value<String>(),         Is.EqualTo("blue-charge"));
                Assert.That(all["maxPower"]!.Value<Int32>(),                    Is.EqualTo(30000));
                Assert.That(all["chargingStationIds"],                          Is.Null);

                // The network stores them where they are given: the stations do not have them, they name their templates.
                var s1 = json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!;
                Assert.That(s1["hotline"],                                      Is.Null);
                Assert.That(s1["address"],                                      Is.Null);
                Assert.That(s1["templateIds"]!.Values<String>(),                Is.EqualTo(new[] { "DE*GEF*ST2", "DE*GEF*ST1" }));

            }

        }

        #endregion

        #region Charging_stations_take_the_properties_of_their_templates()

        [Test]
        public void Charging_stations_take_the_properties_of_their_templates()
        {

            var gef = GEFOf(NewNetwork());

            Assert.That(gef.TryGetChargingStation         (S1, out var s1),           Is.True);
            Assert.That(gef.TryGetEffectiveChargingStation(S1, out var effectiveS1),  Is.True);
            Assert.That(gef.TryGetEffectiveChargingStation(S2, out var effectiveS2),  Is.True);
            Assert.That(gef.TryGetEffectiveChargingPool   (P1, out var effectiveP1),  Is.True);

            using (Assert.EnterMultipleScope())
            {

                // A single value: its own, else the one of the first of its templates giving one.
                Assert.That(effectiveS1!.Accessibility,                                    Is.EqualTo(AccessibilityType.FreePubliclyAccessible));
                Assert.That(effectiveS1.HotlinePhoneNumber,                                Is.EqualTo(Hotline2));
                Assert.That(effectiveS2!.HotlinePhoneNumber,                               Is.EqualTo(Hotline1));
                Assert.That(effectiveS2.Accessibility,                                     Is.EqualTo(AccessibilityType.PayingPubliclyAccessible));
                Assert.That(effectiveS1.Address!.City.FirstText(),                         Is.EqualTo("Jena"));
                Assert.That(effectiveS1.MaxPower,                                          Is.EqualTo(Watt.FromW(30000)));

                // An array: its own united with those of all its templates; of the same brand the one of the first template.
                Assert.That(effectiveS1.Features,                                          Is.EquivalentTo(new[] { ChargingStationFeature.Roofed, ChargingStationFeature.StatusInfoAvailable }));
                Assert.That(effectiveS1.VehicleTypes,                                      Is.EqualTo(new[] { VehicleType.MOTORCYCLE }));
                Assert.That(effectiveS2.VehicleTypes,                                      Is.Empty);
                Assert.That(effectiveS1.Brands.Single().Name.FirstText(),                  Is.EqualTo("Blue Charge Premium"));
                Assert.That(effectiveS2.Brands.Single().Name.FirstText(),                  Is.EqualTo("Blue Charge"));

                // Its very EVSEs, and the latest change of it and its templates.
                Assert.That(effectiveS1.EVSEs.First(),                                     Is.SameAs(s1!.EVSEs.First()));
                Assert.That(effectiveS1.LastChangeDate,                                    Is.EqualTo(Changed));
                Assert.That(effectiveS2.LastChangeDate,                                    Is.EqualTo(Created));

                // The charging station itself is as it is given.
                Assert.That(s1.HotlinePhoneNumber,                                         Is.Null);
                Assert.That(s1.Features,                                                   Is.EqualTo(new[] { ChargingStationFeature.Roofed }));
                Assert.That(gef.EffectiveChargingStation(s1),                              Is.SameAs(effectiveS1));
                Assert.That(gef.EffectiveChargingStations.Count(),                         Is.EqualTo(2));

                // The charging pool with them.
                Assert.That(effectiveP1!.ChargingStations.First(),                         Is.SameAs(effectiveS1));
                Assert.That(effectiveP1.LastChangeDate,                                    Is.EqualTo(Changed));
                Assert.That(gef.EffectiveChargingPools.Single(),                           Is.SameAs(effectiveP1));
                Assert.That(gef.TemplatesOf(s1).Select(template => template.Id),           Is.EqualTo(new[] { Premium, All }));

            }

        }

        #endregion

        #region Without_templates_charging_stations_are_their_own()

        [Test]
        public void Without_templates_charging_stations_are_their_own()
        {

            var gef = new ChargingStationOperator(GEF,
                                                  Created:                   Created,
                                                  ChargingPools:             [ new ChargingPool(P1,
                                                                                                Created:           Created,
                                                                                                ChargingStations:  [ new ChargingStation(S1, Created: Created) ]) ],
                                                  ChargingStationTemplates:  [ new ChargingStationTemplate(All, HotlinePhoneNumber: Hotline1, Created: Created) ],
                                                  ChargingStationGroups:     [ new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG1"), ChargingStationIds: [ S1 ], Created: Created) ]);

            using (Assert.EnterMultipleScope())
            {
                // Neither a template it does not name, nor a group it is in, gives it anything.
                Assert.That(gef.EffectiveChargingPools,                                    Is.SameAs(gef.ChargingPools));
                Assert.That(gef.EffectiveChargingStations.Single(),                        Is.SameAs(gef.ChargingStations.Single()));
            }

        }

        #endregion

        #region The_order_of_the_templates_decides()

        [Test]
        public void The_order_of_the_templates_decides()
        {

            // Templates never disagree: of two giving a value, the one named first wins.
            var allFirst = GEFOf(NewNetwork(PremiumFirst: false));
            allFirst.TryGetEffectiveChargingStation(S1, out var effectiveS1);

            // Twice the same template is refused.
            var json = NewNetwork().ToJSON();
            json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["templateIds"] = new JArray("DE*GEF*ST1", "DE*GEF*ST1");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(effectiveS1!.HotlinePhoneNumber,                               Is.EqualTo(Hotline1));
                Assert.That(effectiveS1.Brands.Single().Name.FirstText(),                  Is.EqualTo("Blue Charge"));
                Assert.That(effectiveS1.VehicleTypes,                                      Is.EqualTo(new[] { VehicleType.MOTORCYCLE }));
                Assert.That(RoamingNetwork.TryParse(json, out _, out var twiceError),      Is.False);
                Assert.That(twiceError,                                                    Does.Contain("must not be given twice"));
            }

        }

        #endregion

        #region Templates_must_not_make_their_members_invalid()

        [Test]
        public void Templates_must_not_make_their_members_invalid()
        {

            var json = NewNetwork().ToJSON();

            JObject With(String PropertyName, JToken Value)
            {
                var changed = (JObject) json.DeepClone();
                changed["chargingStationOperators"]![0]!["chargingStationTemplates"]![0]![PropertyName] = Value;
                return changed;
            }

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("too much power",         With("maxPower",        50000),                      "The charging station 'DE*GEF*S1' with the properties of its templates 'DE*GEF*ST2', 'DE*GEF*ST1' is invalid"),
                ("negative power",         With("maxPower",        -1),                         "must not be negative"),
                ("unknown manufacturer",   With("manufacturerId",  "unknown"),                  "manufacturer 'unknown' of the charging station template 'DE*GEF*ST1'"),
                ("unknown parking space",  With("parkingSpaceIds", new JArray("space-9")),      "parking space 'space-9' of the charging station template 'DE*GEF*ST1'"),
                ("twice the same feature", With("features",        new JArray("Roofed", "Roofed")), "must not be given twice")
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

        #region Change_sets_change_what_charging_stations_take()

        [Test]
        public void Change_sets_change_what_charging_stations_take()
        {

            var rn1 = NewNetwork();
            GEFOf(rn1).TryGetChargingStationTemplate(All, out var all);
            GEFOf(rn1).TryGetChargingStation        (S1,  out var s1);

            var rn2 = rn1.ApplyChangeSet(rn1.CreateSignedChangeSet([
                          RoamingNetworkChange.UpdateProperty(s1!,  "templateIds", null, new JArray(All.ToString(), Premium.ToString())),
                          RoamingNetworkChange.UpdateProperty(all!, "features",    null, new JArray("StatusInfoAvailable", "Roofed")),
                          RoamingNetworkChange.RemoveProperty(all!, "maxPower")
                      ], CreatedAt: T0));

            GEFOf(rn2).TryGetEffectiveChargingStation(S1, out var effectiveS1);

            // Making a member invalid is refused: already when the change set is created.
            var refused = Assert.Throws<ArgumentException>(() => rn2.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(all!, "maxPower", null, 50000) ], CreatedAt: T0.AddMinutes(1)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(effectiveS1!.HotlinePhoneNumber,                               Is.EqualTo(Hotline1));
                Assert.That(effectiveS1.MaxPower,                                          Is.Null);
                Assert.That(effectiveS1.Features,                                          Is.EquivalentTo(new[] { ChargingStationFeature.Roofed, ChargingStationFeature.StatusInfoAvailable }));
                Assert.That(effectiveS1.LastChangeDate,                                    Is.EqualTo(T0));
                Assert.That(refused!.Message,                                              Does.Contain("The charging station 'DE*GEF*S1' with the properties of its templates"));
            }

        }

        #endregion

        #region Effective_charging_stations_share_the_runtime_status()

        [Test]
        public void Effective_charging_stations_share_the_runtime_status()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());
            var rn1     = history.Head.RoamingNetwork;

            GEFOf(rn1).TryGetChargingStation(S1, out var s1);
            s1!.SetStatus(ChargingStationStatusType.Offline);

            GEFOf(rn1).TryGetChargingStationTemplate(All, out var all);
            history.Commit(rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(all!, "hotline", null, "+49 800 333333") ], CreatedAt: T0));

            var gef2 = GEFOf(history.Head.RoamingNetwork);
            gef2.TryGetEffectiveChargingStation(S2, out var effectiveS2);
            gef2.TryGetEffectiveChargingStation(S1, out var effectiveS1);
            gef2.TryGetEffectiveChargingPool   (P1, out var effectiveP1);
            gef2.TryGetChargingPool            (P1, out var p1);

            p1!.SetStatus(ChargingPoolStatusType.Offline);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(effectiveS2!.HotlinePhoneNumber,                               Is.EqualTo(PhoneNumber.Parse("+49 800 333333")));
                Assert.That(effectiveS1!.Status.Value,                                     Is.EqualTo(ChargingStationStatusType.Offline));
                Assert.That(effectiveP1!.Status.Value,                                     Is.EqualTo(ChargingPoolStatusType.Offline));
                Assert.That(effectiveP1.ChargingStations.First().Status.Value,             Is.EqualTo(ChargingStationStatusType.Offline));
            }

        }

        #endregion

    }

}
