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
    /// EVSEs and charging pools taking the properties their groups give them at runtime, and charging stations taking
    /// their effective EVSEs, charging pools their effective charging stations.
    /// </summary>
    [TestFixture]
    public class EffectiveEVSEAndChargingPoolTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              Changed  = DateTimeOffset.Parse("2026-06-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingPool_Id             P2       = ChargingPool_Id.           Parse("DE*GEF*P2");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly ChargingStation_Id          S2       = ChargingStation_Id.        Parse("DE*GEF*S2");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly EVSE_Id                     E3       = EVSE_Id.                   Parse("DE*GEF*E2*1");
        private static readonly EVSEGroup_Id                AC22     = EVSEGroup_Id.              Parse("DE*GEF*EG1");
        private static readonly ChargingPoolGroup_Id        Sites    = ChargingPoolGroup_Id.      Parse("DE*GEF*PG1");

        private static readonly ChargingConnector_Id        C1       = ChargingConnector_Id.      Parse("1");
        private static readonly PhoneNumber                 Hotline  = PhoneNumber.Parse("+49 800 111111");

        #endregion

        #region NewNetwork()

        /// <summary>
        /// Two charging pools in a group, which gives them a hotline, a parking type and a facility; three EVSEs in a group
        /// of 22 kW EVSEs with a cable, the first one with 11 kW of its own. The first pool has a facility of its own.
        /// </summary>
        private static RoamingNetwork NewNetwork(Int32? StationMaxPower = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    GEF,
                                                    Created:             Created,
                                                    ChargingPools:       [ new ChargingPool(P1,
                                                                                            Facilities:        [ Facility.CAFE ],
                                                                                            Created:           Created,
                                                                                            ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                     MaxPower:  StationMaxPower.HasValue ? Watt.FromW(StationMaxPower.Value) : null,
                                                                                                                                     Created:   Created,
                                                                                                                                     EVSEs:     [ new EVSE(E1, MaxPower: Watt.FromW(11000), Created: Created),
                                                                                                                                                  new EVSE(E2, Created: Created) ]) ]),
                                                                           new ChargingPool(P2,
                                                                                            Created:           Created,
                                                                                            ChargingStations:  [ new ChargingStation(S2,
                                                                                                                                     Created:   Created,
                                                                                                                                     EVSEs:     [ new EVSE(E3, Created: Created) ]) ]) ],
                                                    EVSEGroups:          [ new EVSEGroup(AC22,
                                                                                         EVSEIds:             [ E1, E2, E3 ],
                                                                                         CurrentTypes:        CurrentTypes.AC_ThreePhases,
                                                                                         MaxPower:            Watt.FromW(22000),
                                                                                         ChargingConnectors:  [ new ChargingConnector(C1, ChargingConnectorType.IEC_62196_T2, Format: ChargingConnectorFormat.CABLE) ],
                                                                                         Standards:           [ Standard.PAS1899_2022 ],
                                                                                         Created:             Created,
                                                                                         LastChange:          Changed) ],
                                                    ChargingPoolGroups:  [ new ChargingPoolGroup(Sites,
                                                                                                 ChargingPoolIds:     [ P1, P2 ],
                                                                                                 HotlinePhoneNumber:  Hotline,
                                                                                                 ParkingType:         ParkingType.PARKING_LOT,
                                                                                                 Facilities:          [ Facility.SUPERMARKET ],
                                                                                                 ArrivalInstructions: I18NString.Create(Languages.en, "Behind the house"),
                                                                                                 Created:             Created) ]) ]).WithKeys();

        #endregion

        #region (static) GEFOf(Network)

        private static ChargingStationOperator GEFOf(RoamingNetwork Network)

            => Network.GetChargingStationOperatorById(GEF)!;

        #endregion


        #region EVSE_and_charging_pool_groups_have_the_properties_of_their_members()

        [Test]
        public void EVSE_and_charging_pool_groups_have_the_properties_of_their_members()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var gef        = json["chargingStationOperators"]![0]!;
            var evseGroup  = gef["EVSEGroups"]![0]!;
            var poolGroup  = gef["chargingPoolGroups"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));

                // Named as their members name them.
                Assert.That(evseGroup["maxPower"]!.Value<Int32>(),                           Is.EqualTo(22000));
                Assert.That(evseGroup["currentTypes"],                                       Is.Not.Null);
                Assert.That(evseGroup["chargingConnectors"]![0]!["@id"]!.Value<String>(),    Is.EqualTo("1"));
                Assert.That(poolGroup["hotline"]!.Value<String>(),                           Is.EqualTo(Hotline.ToString()));
                Assert.That(poolGroup["parkingType"]!.Value<String>(),                       Is.EqualTo("PARKING_LOT"));
                Assert.That(poolGroup["arrivalInstructions"],                                Is.Not.Null);

                // Kept where they are given.
                Assert.That(gef["chargingPools"]![0]!["hotline"],                            Is.Null);
                Assert.That(gef["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![1]!["maxPower"],  Is.Null);

            }

        }

        #endregion

        #region EVSEs_and_charging_pools_take_the_properties_of_their_groups()

        [Test]
        public void EVSEs_and_charging_pools_take_the_properties_of_their_groups()
        {

            var gef = GEFOf(NewNetwork());

            Assert.That(gef.TryGetEVSE                 (E1, out var e1),             Is.True);
            Assert.That(gef.TryGetEffectiveEVSE        (E1, out var effectiveE1),    Is.True);
            Assert.That(gef.TryGetEffectiveEVSE        (E2, out var effectiveE2),    Is.True);
            Assert.That(gef.TryGetEffectiveChargingPool(P1, out var effectiveP1),    Is.True);
            Assert.That(gef.TryGetEffectiveChargingPool(P2, out var effectiveP2),    Is.True);

            var effectiveS1 = effectiveP1!.ChargingStations.Single();

            using (Assert.EnterMultipleScope())
            {

                // EVSEs: their own maximum power wins, the others of the group they take.
                Assert.That(effectiveE1!.MaxPower,                                         Is.EqualTo(Watt.FromW(11000)));
                Assert.That(effectiveE2!.MaxPower,                                         Is.EqualTo(Watt.FromW(22000)));
                Assert.That(effectiveE2.CurrentTypes,                                      Is.EqualTo(CurrentTypes.AC_ThreePhases));
                Assert.That(effectiveE2.ChargingConnectors.Single().Id,                    Is.EqualTo(C1));
                Assert.That(effectiveE2.Standards,                                         Is.EqualTo(new[] { Standard.PAS1899_2022 }));
                Assert.That(effectiveE2.LastChangeDate,                                    Is.EqualTo(Changed));
                Assert.That(e1!.MaxPower,                                                  Is.EqualTo(Watt.FromW(11000)));
                Assert.That(e1.ChargingConnectors,                                         Is.Empty);
                Assert.That(gef.EffectiveEVSE(e1),                                         Is.SameAs(effectiveE1));

                // Charging pools: a single value of their group, arrays united.
                Assert.That(effectiveP1.HotlinePhoneNumber,                                Is.EqualTo(Hotline));
                Assert.That(effectiveP2!.ParkingType,                                      Is.EqualTo(ParkingType.PARKING_LOT));
                Assert.That(effectiveP1.Facilities,                                        Is.EquivalentTo(new[] { Facility.CAFE, Facility.SUPERMARKET }));
                Assert.That(effectiveP2.Facilities,                                        Is.EqualTo(new[] { Facility.SUPERMARKET }));
                Assert.That(effectiveP1.ArrivalInstructions.FirstText(),                   Is.EqualTo("Behind the house"));

                // Each with what it embeds as it is at runtime, and its latest change.
                Assert.That(effectiveS1.EVSEs.First(),                                     Is.SameAs(effectiveE1));
                Assert.That(gef.EffectiveChargingStation(effectiveS1),                     Is.SameAs(effectiveS1));
                Assert.That(effectiveS1.LastChangeDate,                                    Is.EqualTo(Changed));
                Assert.That(effectiveP1.LastChangeDate,                                    Is.EqualTo(Changed));
                Assert.That(gef.EffectiveChargingPools.First(),                            Is.SameAs(effectiveP1));
                Assert.That(gef.EffectiveEVSEs.Count(),                                    Is.EqualTo(3));
                Assert.That(gef.EVSEGroupsOf(E3).Single().Id,                              Is.EqualTo(AC22));
                Assert.That(gef.ChargingPoolGroupsOf(P2).Single().Id,                      Is.EqualTo(Sites));

                // The charging pools themselves are as they are given.
                Assert.That(gef.ChargingPools.First().HotlinePhoneNumber,                  Is.Null);

            }

        }

        #endregion

        #region A_charging_station_is_checked_with_its_effective_EVSEs()

        [Test]
        public void A_charging_station_is_checked_with_its_effective_EVSEs()
        {

            // 11 kW of its own and 22 kW of its group: 33 kW together.
            Assert.That(() => NewNetwork(StationMaxPower: 33000), Throws.Nothing);
            Assert.That(() => NewNetwork(StationMaxPower: 40000), Throws.ArgumentException.With.Message.Contains("must not exceed the 33000 W of all its EVSEs together"));

        }

        #endregion

        #region Invalid_EVSE_and_charging_pool_groups_are_refused()

        [Test]
        public void Invalid_EVSE_and_charging_pool_groups_are_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject With(String Groups, String PropertyName, JToken Value, Action<JObject>? More = null)
            {
                var changed  = (JObject) json.DeepClone();
                var gef      = (JObject) changed["chargingStationOperators"]![0]!;
                gef[Groups]![0]![PropertyName] = Value;
                More?.Invoke(gef);
                return changed;
            }

            // A second group of the same priority, giving another value.
            void SecondEVSEGroup(JObject GEF)
                => ((JArray) GEF["EVSEGroups"]!).Add(new JObject(new JProperty("@id",       "DE*GEF*EG2"),
                                                                 new JProperty("EVSEIds",   new JArray("DE*GEF*E1*2")),
                                                                 new JProperty("maxPower",  11000),
                                                                 new JProperty("created",   "2026-01-01T00:00:00.000Z")));

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("negative voltage",       With("EVSEGroups",          "maxVoltage",             -1),                         "must not be negative"),
                ("twice a connector",      With("EVSEGroups",          "chargingConnectors",     new JArray(json["chargingStationOperators"]![0]!["EVSEGroups"]![0]!["chargingConnectors"]![0]!,
                                                                                                            json["chargingStationOperators"]![0]!["EVSEGroups"]![0]!["chargingConnectors"]![0]!)),  "must not be given twice"),
                ("same priority",          With("EVSEGroups",          "priority",               0, SecondEVSEGroup),         "give the EVSE 'DE*GEF*E1*2' different values of 'maxPower' with the same priority 0"),
                ("unknown grid point",     With("chargingPoolGroups",  "gridConnectionPointId",  "GCP-9"),                    "grid connection point 'GCP-9' of the charging pool group 'DE*GEF*PG1'"),
                ("twice a facility",       With("chargingPoolGroups",  "facilities",             new JArray("CAFE", "CAFE")), "must not be given twice"),
                ("a text as priority",     With("chargingPoolGroups",  "priority",               "1"),                        "priority of the charging pool group must be an integer")
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

        #region A_member_leaving_its_group_changes_its_last_change()

        [Test]
        public void A_member_leaving_its_group_changes_its_last_change()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());
            var rn1     = history.Head.RoamingNetwork;

            rn1.TryGetEVSEGroupById(AC22, out var ac22);

            // The second EVSE leaves the group of 22 kW EVSEs: it is as it is given, but changed now.
            Assert.That(history.Commit(rn1.CreateSignedChangeSet([
                            RoamingNetworkChange.UpdateProperty(ac22!, "EVSEIds",    null, new JArray(E1.ToString(), E3.ToString())),
                            // A further change of the same change set: the operator is read again, the last change stays.
                            RoamingNetworkChange.UpdateProperty(ac22!, "maxVoltage", null, 400)
                        ], CreatedAt: T0)).IsCommitted);

            var gef2 = GEFOf(history.Head.RoamingNetwork);

            gef2.TryGetEVSE                 (E2, out var e2);
            gef2.TryGetEffectiveEVSE        (E2, out var effectiveE2);
            gef2.TryGetEffectiveEVSE        (E3, out var effectiveE3);
            gef2.TryGetEffectiveChargingPool(P1, out var effectiveP1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(effectiveE2!.MaxPower,                                         Is.Null);
                Assert.That(effectiveE2.LastChangeDate,                                    Is.EqualTo(T0));
                Assert.That(e2!.LastChangeDate,                                            Is.EqualTo(Created));
                Assert.That(effectiveP1!.ChargingStations.Single().LastChangeDate,         Is.EqualTo(T0));
                Assert.That(effectiveP1.LastChangeDate,                                    Is.EqualTo(T0));

                // The members staying have the last change of their changed group.
                Assert.That(effectiveE3!.LastChangeDate,                                   Is.EqualTo(T0));
            }

        }

        #endregion

        #region Change_sets_change_what_EVSEs_and_charging_pools_take()

        [Test]
        public void Change_sets_change_what_EVSEs_and_charging_pools_take()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());
            var rn1     = history.Head.RoamingNetwork;

            GEFOf(rn1).TryGetEVSE(E2, out var e2);
            e2!.SetStatus(EVSEStatusType.Charging);

            rn1.TryGetEVSEGroupById        (AC22,  out var ac22);
            rn1.TryGetChargingPoolGroupById(Sites, out var sites);

            Assert.That(history.Commit(rn1.CreateSignedChangeSet([
                            RoamingNetworkChange.UpdateProperty(ac22!,  "maxPower",    null, 11000),
                            RoamingNetworkChange.UpdateProperty(sites!, "facilities",  null, new JArray("CAFE", "SUPERMARKET")),
                            RoamingNetworkChange.RemoveProperty(sites!, "hotline")
                        ], CreatedAt: T0)).IsCommitted);

            var gef2 = GEFOf(history.Head.RoamingNetwork);

            gef2.TryGetEffectiveEVSE        (E2, out var effectiveE2);
            gef2.TryGetEffectiveChargingPool(P2, out var effectiveP2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(effectiveE2!.MaxPower,                                         Is.EqualTo(Watt.FromW(11000)));
                Assert.That(effectiveE2.LastChangeDate,                                    Is.EqualTo(T0));
                Assert.That(effectiveE2.Status.Value,                                      Is.EqualTo(EVSEStatusType.Charging));
                Assert.That(effectiveP2!.HotlinePhoneNumber,                               Is.Null);
                Assert.That(effectiveP2.Facilities,                                        Is.EquivalentTo(new[] { Facility.CAFE, Facility.SUPERMARKET }));
                Assert.That(effectiveP2.LastChangeDate,                                    Is.EqualTo(T0));
            }

        }

        #endregion

    }

}
