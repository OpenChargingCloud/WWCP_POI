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
    /// The priority of groups, and charging stations taking the properties their groups give them at runtime.
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
        private static readonly ChargingStationGroup_Id     All      = ChargingStationGroup_Id.   Parse("DE*GEF*SG1");
        private static readonly ChargingStationGroup_Id     Premium  = ChargingStationGroup_Id.   Parse("DE*GEF*SG2");
        private static readonly Brand_Id                    Blue     = Brand_Id.                  Parse("blue-charge");

        private static readonly PhoneNumber                 Hotline1 = PhoneNumber.Parse("+49 800 111111");
        private static readonly PhoneNumber                 Hotline2 = PhoneNumber.Parse("+49 800 222222");

        #endregion

        #region NewNetwork(PremiumPriority = 5)

        /// <summary>
        /// Two charging stations in a group of all of them, which gives them a hotline, an address, a feature, a brand and a
        /// maximum power, and the first one in a premium group of a higher priority, which gives it another hotline, the same
        /// brand by another name and a vehicle type. The first one has an accessibility and a feature of its own.
        /// </summary>
        private static RoamingNetwork NewNetwork(Int32? PremiumPriority = 5)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    GEF,
                                                    Created:                Created,
                                                    ChargingPools:          [ new ChargingPool(P1,
                                                                                               Created:           Created,
                                                                                               ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                        Accessibility:  AccessibilityType.FreePubliclyAccessible,
                                                                                                                                        Features:       [ ChargingStationFeature.Roofed ],
                                                                                                                                        Created:        Created,
                                                                                                                                        EVSEs:          [ new EVSE(E1, MaxPower: Watt.FromW(22000), Created: Created),
                                                                                                                                                          new EVSE(E2, MaxPower: Watt.FromW(22000), Created: Created) ]),
                                                                                                                    new ChargingStation(S2,
                                                                                                                                        Created:        Created,
                                                                                                                                        EVSEs:          [ new EVSE(E3, Created: Created) ]) ]) ],
                                                    ChargingStationGroups:  [ new ChargingStationGroup(All,
                                                                                                       ChargingStationIds:  [ S1, S2 ],
                                                                                                       Address:             new Address("Hauptstraße", "07743", I18NString.Create(Languages.de, "Jena"), Country.Germany, HouseNumber: "1"),
                                                                                                       HotlinePhoneNumber:  Hotline1,
                                                                                                       Accessibility:       AccessibilityType.PayingPubliclyAccessible,
                                                                                                       Features:            [ ChargingStationFeature.StatusInfoAvailable ],
                                                                                                       Brands:              [ new Brand(Blue, new ImmutableI18NString(I18NString.Create(Languages.en, "Blue Charge"))) ],
                                                                                                       MaxPower:            Watt.FromW(30000),
                                                                                                       Created:             Created),
                                                                              new ChargingStationGroup(Premium,
                                                                                                       ChargingStationIds:  [ S1 ],
                                                                                                       Priority:            PremiumPriority,
                                                                                                       HotlinePhoneNumber:  Hotline2,
                                                                                                       VehicleTypes:        [ VehicleType.MOTORCYCLE ],
                                                                                                       Brands:              [ new Brand(Blue, new ImmutableI18NString(I18NString.Create(Languages.en, "Blue Charge Premium"))) ],
                                                                                                       Created:             Created,
                                                                                                       LastChange:          Changed) ]) ]).WithKeys();

        #endregion

        #region (static) GEFOf(Network)

        private static ChargingStationOperator GEFOf(RoamingNetwork Network)

            => Network.GetChargingStationOperatorById(GEF)!;

        #endregion


        #region Every_group_has_a_priority()

        [Test]
        public void Every_group_has_a_priority()
        {

            var groups = new (String Name, JObject JSON, Func<JObject, Int32?> PriorityOf)[] {
                ("EVSE group",             new EVSEGroup           (EVSEGroup_Id.           Parse("DE*GEF*EG1"), Priority: 3,  Created: Created).ToJSON(), json => EVSEGroup.           TryParse(json, out var group, out _) ? group!.Priority : -99),
                ("charging station group", new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG1"), Priority: -2, Created: Created).ToJSON(), json => ChargingStationGroup.TryParse(json, out var group, out _) ? group!.Priority : -99),
                ("charging pool group",    new ChargingPoolGroup   (ChargingPoolGroup_Id.   Parse("DE*GEF*PG1"), Priority: 0,  Created: Created).ToJSON(), json => ChargingPoolGroup.   TryParse(json, out var group, out _) ? group!.Priority : -99),
                ("charging tariff group",  new ChargingTariffGroup (ChargingTariffGroup_Id. Parse("DE*GEF*TG1"), Priority: 7,  Created: Created).ToJSON(), json => ChargingTariffGroup. TryParse(json, out var group, out _) ? group!.Priority : -99),
                ("parking space group",    new ParkingSpaceGroup   (ParkingSpaceGroup_Id.   Parse("level-1"),    Priority: 1,  Created: Created).ToJSON(), json => ParkingSpaceGroup.   TryParse(json, out var group, out _) ? group!.Priority : -99)
            };

            using (Assert.EnterMultipleScope())
            {
                foreach (var (name, json, priorityOf) in groups)
                {

                    Assert.That(json["priority"]!.Type,                                          Is.EqualTo(JTokenType.Integer), name);
                    Assert.That(priorityOf(json),                                                Is.EqualTo(json["priority"]!.Value<Int32>()), name);

                    // Only an integer: neither a text, nor a fraction, nor beyond 32 bits.
                    foreach (var invalid in new JToken[] { "3", 1.5, 1.0, 4294967296L })
                    {
                        var changed = (JObject) json.DeepClone();
                        changed["priority"] = invalid;
                        Assert.That(priorityOf(changed),                                         Is.EqualTo(-99), $"{name}: {invalid}");
                    }

                    var without = (JObject) json.DeepClone();
                    without.Remove("priority");
                    Assert.That(priorityOf(without),                                             Is.Null, name);

                }
            }

        }

        #endregion

        #region A_charging_station_group_has_the_properties_of_a_charging_station()

        [Test]
        public void A_charging_station_group_has_the_properties_of_a_charging_station()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var all = json["chargingStationOperators"]![0]!["chargingStationGroups"]![0]!;

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
                Assert.That(all["priority"],                                    Is.Null);

                // The network stores them where they are given: the stations do not have them.
                var s1 = json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!;
                Assert.That(s1["hotline"],                                      Is.Null);
                Assert.That(s1["address"],                                      Is.Null);

            }

        }

        #endregion

        #region Charging_stations_take_the_properties_of_their_groups()

        [Test]
        public void Charging_stations_take_the_properties_of_their_groups()
        {

            var gef = GEFOf(NewNetwork());

            Assert.That(gef.TryGetChargingStation         (S1, out var s1),           Is.True);
            Assert.That(gef.TryGetEffectiveChargingStation(S1, out var effectiveS1),  Is.True);
            Assert.That(gef.TryGetEffectiveChargingStation(S2, out var effectiveS2),  Is.True);
            Assert.That(gef.TryGetEffectiveChargingPool   (P1, out var effectiveP1),  Is.True);

            using (Assert.EnterMultipleScope())
            {

                // A single value: its own, else the one of the group with the highest priority.
                Assert.That(effectiveS1!.Accessibility,                                    Is.EqualTo(AccessibilityType.FreePubliclyAccessible));
                Assert.That(effectiveS1.HotlinePhoneNumber,                                Is.EqualTo(Hotline2));
                Assert.That(effectiveS2!.HotlinePhoneNumber,                               Is.EqualTo(Hotline1));
                Assert.That(effectiveS2.Accessibility,                                     Is.EqualTo(AccessibilityType.PayingPubliclyAccessible));
                Assert.That(effectiveS1.Address!.City.FirstText(),                         Is.EqualTo("Jena"));
                Assert.That(effectiveS1.MaxPower,                                          Is.EqualTo(Watt.FromW(30000)));

                // An array: its own united with those of all its groups; of the same brand the one of the higher priority.
                Assert.That(effectiveS1.Features,                                          Is.EquivalentTo(new[] { ChargingStationFeature.Roofed, ChargingStationFeature.StatusInfoAvailable }));
                Assert.That(effectiveS1.VehicleTypes,                                      Is.EqualTo(new[] { VehicleType.MOTORCYCLE }));
                Assert.That(effectiveS2.VehicleTypes,                                      Is.Empty);
                Assert.That(effectiveS1.Brands.Single().Name.FirstText(),                  Is.EqualTo("Blue Charge Premium"));
                Assert.That(effectiveS2.Brands.Single().Name.FirstText(),                  Is.EqualTo("Blue Charge"));

                // Its very EVSEs, and the latest change of it and its groups.
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
                Assert.That(gef.ChargingStationGroupsOf(S1).Select(group => group.Id),     Is.EqualTo(new[] { All, Premium }));

            }

        }

        #endregion

        #region Without_groups_giving_properties_charging_stations_are_their_own()

        [Test]
        public void Without_groups_giving_properties_charging_stations_are_their_own()
        {

            var gef = new ChargingStationOperator(GEF,
                                                  Created:                Created,
                                                  ChargingPools:          [ new ChargingPool(P1,
                                                                                             Created:           Created,
                                                                                             ChargingStations:  [ new ChargingStation(S1, Created: Created) ]) ],
                                                  ChargingStationGroups:  [ new ChargingStationGroup(All, ChargingStationIds: [ S1 ], Priority: 3, Created: Created) ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gef.EffectiveChargingPools,                                    Is.SameAs(gef.ChargingPools));
                Assert.That(gef.EffectiveChargingStations.Single(),                        Is.SameAs(gef.ChargingStations.Single()));
            }

        }

        #endregion

        #region Groups_of_the_same_priority_must_not_disagree()

        [Test]
        public void Groups_of_the_same_priority_must_not_disagree()
        {

            JObject WithPremium(Action<JToken> Change)
            {
                var json = NewNetwork().ToJSON();
                Change(json["chargingStationOperators"]![0]!["chargingStationGroups"]![1]!);
                return json;
            }

            // The same priority: the hotline, and then the brand, is given differently.
            var hotline = WithPremium(premium => ((JObject) premium).Remove("priority"));
            var brand   = WithPremium(premium => { premium["priority"] = 0; ((JObject) premium).Remove("hotline"); });

            Assert.That(RoamingNetwork.TryParse(hotline, out _, out var hotlineError),  Is.False);
            Assert.That(RoamingNetwork.TryParse(brand,   out _, out var brandError),    Is.False);

            // So is a charging station operator given so.
            Assert.That(() => NewNetwork(PremiumPriority: null),                       Throws.ArgumentException.With.Message.Contains("their priorities must tell"));

            // A lower priority decides as a higher one.
            var lower   = GEFOf(NewNetwork(PremiumPriority: -1));
            lower.TryGetEffectiveChargingStation(S1, out var effectiveS1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(hotlineError,                                                  Does.Contain("The groups 'DE*GEF*SG1' and 'DE*GEF*SG2' give the charging station 'DE*GEF*S1' different values of 'hotline' with the same priority 0"));
                Assert.That(hotlineError,                                                  Does.Contain("their priorities must tell which one it takes"));
                Assert.That(brandError,                                                    Does.Contain("different values of brand 'blue-charge'"));
                Assert.That(effectiveS1!.HotlinePhoneNumber,                               Is.EqualTo(Hotline1));
                Assert.That(effectiveS1.Brands.Single().Name.FirstText(),                  Is.EqualTo("Blue Charge"));
            }

        }

        #endregion

        #region Groups_must_not_make_their_members_invalid()

        [Test]
        public void Groups_must_not_make_their_members_invalid()
        {

            var json = NewNetwork().ToJSON();

            JObject With(String PropertyName, JToken Value)
            {
                var changed = (JObject) json.DeepClone();
                changed["chargingStationOperators"]![0]!["chargingStationGroups"]![0]![PropertyName] = Value;
                return changed;
            }

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("too much power",         With("maxPower",        50000),                      "The charging station 'DE*GEF*S1' with the properties of its groups 'DE*GEF*SG2', 'DE*GEF*SG1' is invalid"),
                ("negative power",         With("maxPower",        -1),                         "must not be negative"),
                ("unknown manufacturer",   With("manufacturerId",  "unknown"),                  "manufacturer 'unknown' of the charging station group 'DE*GEF*SG1'"),
                ("unknown parking space",  With("parkingSpaceIds", new JArray("space-9")),      "parking space 'space-9' of the charging station group 'DE*GEF*SG1'"),
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
            rn1.TryGetChargingStationGroupById(All,     out var all);
            rn1.TryGetChargingStationGroupById(Premium, out var premium);

            var rn2 = rn1.ApplyChangeSet(rn1.CreateSignedChangeSet([
                          RoamingNetworkChange.UpdateProperty(premium!, "priority",  null, -1),
                          RoamingNetworkChange.UpdateProperty(all!,     "features",  null, new JArray("StatusInfoAvailable", "Roofed")),
                          RoamingNetworkChange.RemoveProperty(all!,     "maxPower")
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
                Assert.That(refused!.Message,                                              Does.Contain("The charging station 'DE*GEF*S1' with the properties of its groups"));
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

            rn1.TryGetChargingStationGroupById(All, out var all);
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
