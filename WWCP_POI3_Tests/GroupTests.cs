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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Groups of EVSEs, charging stations, charging pools, charging tariffs and parking spaces.
    /// </summary>
    [TestFixture]
    public class GroupTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly ChargingTariff_Id           T1       = ChargingTariff_Id.         Parse("DE*GEF*T1");
        private static readonly EVSEGroup_Id                EG1      = EVSEGroup_Id.              Parse("DE*GEF*EG1");
        private static readonly ChargingStationGroup_Id     SG1      = ChargingStationGroup_Id.   Parse("DE*GEF*SG1");
        private static readonly ChargingPoolGroup_Id        PG1      = ChargingPoolGroup_Id.      Parse("DE*GEF*PG1");
        private static readonly ChargingTariffGroup_Id      TG1      = ChargingTariffGroup_Id.    Parse("DE*GEF*TG1");
        private static readonly ParkingOperator_Id          PAR      = ParkingOperator_Id.        Parse("DE*PAR");
        private static readonly ParkingSpaceGroup_Id        Level1   = ParkingSpaceGroup_Id.      Parse("level-1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:                Created,
                                                                             ChargingTariffs:        [ new ChargingTariff(T1,
                                                                                                                          Currency.EUR,
                                                                                                                          [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                          Created: Created) ],
                                                                             ChargingPools:          [ new ChargingPool(P1,
                                                                                                                        Created:           Created,
                                                                                                                        ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                                 Created:  Created,
                                                                                                                                                                 EVSEs:    [ new EVSE(E1, Created: Created),
                                                                                                                                                                             new EVSE(E2, Created: Created) ]) ]) ],
                                                                             EVSEGroups:             [ new EVSEGroup           (EG1, I18NString.Create(Languages.en, "Fast"), EVSEIds:            [ E2, E1 ], ChargingTariffId: T1, Created: Created) ],
                                                                             ChargingStationGroups:  [ new ChargingStationGroup(SG1,                                          ChargingStationIds: [ S1 ],                             Created: Created) ],
                                                                             ChargingPoolGroups:     [ new ChargingPoolGroup   (PG1,                                          ChargingPoolIds:    [ P1 ],                             Created: Created) ],
                                                                             ChargingTariffGroups:   [ new ChargingTariffGroup (TG1,                                          ChargingTariffIds:  [ T1 ],                             Created: Created) ]) ],
                    ParkingOperators:          [ new ParkingOperator(PAR,
                                                                     Created:             Created,
                                                                     ParkingSpaces:       [ new ParkingSpace(ParkingSpace_Id.Parse("space-1"), Created: Created),
                                                                                            new ParkingSpace(ParkingSpace_Id.Parse("space-2"), Created: Created) ],
                                                                     ParkingSpaceGroups:  [ new ParkingSpaceGroup(Level1,
                                                                                                                  ParkingSpaceIds:  [ ParkingSpace_Id.Parse("space-1"), ParkingSpace_Id.Parse("space-2") ],
                                                                                                                  Created:          Created) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Groups_survive_JSON_and_CBOR()

        [Test]
        public void Groups_survive_JSON_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var gef = json["chargingStationOperators"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromJSON.CalcETags(),                                              Is.EqualTo(rn1.CalcETags()));

                Assert.That(gef["EVSEGroups"]![0]!["EVSEIds"]!.Values<String>(),               Is.EqualTo(new[] { "DE*GEF*E1*1", "DE*GEF*E1*2" }));
                Assert.That(gef["EVSEGroups"]![0]!["chargingTariffId"]!.ToString(),           Is.EqualTo("DE*GEF*T1"));
                Assert.That(gef["chargingStationGroups"]![0]!["chargingStationIds"]!.Values<String>(),  Is.EqualTo(new[] { "DE*GEF*S1" }));
                Assert.That(gef["chargingPoolGroups"]![0]!["chargingPoolIds"]!.Values<String>(),        Is.EqualTo(new[] { "DE*GEF*P1" }));
                Assert.That(gef["chargingTariffGroups"]![0]!["chargingTariffIds"]!.Values<String>(),    Is.EqualTo(new[] { "DE*GEF*T1" }));
                Assert.That(json["parkingOperators"]![0]!["parkingSpaceGroups"]![0]!["parkingSpaceIds"]!.Values<String>(),  Is.EqualTo(new[] { "space-1", "space-2" }));

                Assert.That(fromJSON.TryGetEVSEGroupById(EG1, out var eg1),                    Is.True);
                Assert.That(eg1!.ChargingTariffId,                                             Is.EqualTo(T1));
                Assert.That(fromJSON.TryGetChargingStationGroupById(SG1, out _),               Is.True);
                Assert.That(fromJSON.TryGetChargingPoolGroupById(PG1, out _),                  Is.True);
                Assert.That(fromJSON.TryGetChargingTariffGroupById(TG1, out _),                Is.True);
                Assert.That(fromJSON.TryGetParkingSpaceGroupById(Level1, out _),               Is.True);

            }

        }

        #endregion

        #region Invalid_groups_are_refused()

        [Test]
        public void Invalid_groups_are_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            JObject GEFJSON(JObject Network)
                => (JObject) Network["chargingStationOperators"]![0]!;

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("unknown EVSE",           Changed(network => GEFJSON(network)["EVSEGroups"]![0]!["EVSEIds"] = new JArray("DE*GEF*E9*1")),           "member 'DE*GEF*E9*1' of the group 'DE*GEF*EG1'"),
                ("unknown tariff",         Changed(network => GEFJSON(network)["EVSEGroups"]![0]!["chargingTariffId"] = "DE*GEF*T9"),              "charging tariff 'DE*GEF*T9' of the group"),
                ("foreign group",          Changed(network => GEFJSON(network)["chargingPoolGroups"]![0]!["@id"] = "DE*XYZ*PG1"),                    "does not belong to the operator"),
                ("unknown station",        Changed(network => GEFJSON(network)["chargingStationGroups"]![0]!["chargingStationIds"] = new JArray("DE*GEF*S9")), "member 'DE*GEF*S9'"),
                ("unknown parking space",  Changed(network => network["parkingOperators"]![0]!["parkingSpaceGroups"]![0]!["parkingSpaceIds"] = new JArray("space-9")), "parking space 'space-9' of the parking space group 'level-1'")
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

        #region Groups_are_changed_by_change_sets()

        [Test]
        public void Groups_are_changed_by_change_sets()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var rn1      = history.Head.RoamingNetwork;
            rn1.TryGetEVSEGroupById(EG1, out var eg1);
            rn1.TryGetParkingSpaceGroupById(Level1, out var level1);

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(EVSEGroup), EG1.ToString(), "charging", T0, out _, out _);

            var eg2      = EVSEGroup_Id.Parse("DE*GEF*EG2");

            history.Publish(rn1.CreateSignedChangeSet([
                                RoamingNetworkChange.Add           (new EVSEGroup(eg2, EVSEIds: [ E1 ])),
                                RoamingNetworkChange.UpdateProperty(eg1!,    "EVSEIds",          null, new JArray(E1.ToString())),
                                RoamingNetworkChange.RemoveProperty(eg1!,    "chargingTariffId"),
                                RoamingNetworkChange.UpdateProperty(level1!, "parkingSpaceIds",  null, new JArray("space-2")),
                                RoamingNetworkChange.Remove        (SG1)
                            ], CreatedAt: T0.AddMinutes(1)));

            var rn2      = history.Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.EVSEGroups.Select(group => group.Id.ToString()),               Is.EqualTo(new[] { "DE*GEF*EG1", "DE*GEF*EG2" }));
                Assert.That(rn2.TryGetEVSEGroupById(EG1, out var changedEG1),                  Is.True);
                Assert.That(changedEG1!.EVSEIds,                                               Is.EqualTo(new[] { E1 }));
                Assert.That(changedEG1.ChargingTariffId,                                       Is.Null);
                Assert.That(rn2.TryGetChargingStationGroupById(SG1, out _),                    Is.False);
                Assert.That(rn2.TryGetParkingSpaceGroupById(Level1, out var changedLevel1),    Is.True);
                Assert.That(changedLevel1!.ParkingSpaceIds.Select(id => id.ToString()),        Is.EqualTo(new[] { "space-2" }));

                // The status of the group survived its change.
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(EVSEGroup), EG1.ToString(), out var groupStatus),  Is.True);
                Assert.That(groupStatus!.Value,                                                                                                 Is.EqualTo("charging"));

                // A member of a group can not be removed, nor a tariff of a tariff group.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(E1)),
                            Throws.ArgumentException.With.Message.Contains("member 'DE*GEF*E1*1' of the group"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(T1)),
                            Throws.ArgumentException.With.Message.Contains("member 'DE*GEF*T1' of the group 'DE*GEF*TG1'"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(ParkingSpace_Id.Parse("space-2"))),
                            Throws.ArgumentException.With.Message.Contains("parking space 'space-2' of the parking space group 'level-1'"));

            }

        }

        #endregion

        #region The_runtime_status_reaches_groups()

        [Test]
        public void The_runtime_status_reaches_groups()
        {

            var leader   = RoamingNetworkHistory.Start(NewNetwork());
            var updates  = new List<POIStatusUpdate>();

            leader.RuntimeStatus.OnStatusUpdate += updates.Add;

            var results  = new[] {
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(EVSEGroup),            EG1.ToString(),    "charging",     T0, out _, out _),
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ChargingStationGroup), SG1.ToString(),    "faulted",      T0, out _, out _),
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(ChargingPoolGroup),    PG1.ToString(),    "outOfService", T0, out _, out _),
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ChargingTariffGroup),  TG1.ToString(),    "expired",      T0, out _, out _),
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ParkingSpaceGroup),    Level1.ToString(), "inUse",        T0, out _, out _)
                           };

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotCBOR(), out var follower, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(results,  Is.All.EqualTo(POIStatusUpdateResult.Updated));

                Assert.That(updates.Select(update => $"{update.EntityType} {update.Value}"),
                            Is.EqualTo(new[] {
                                "EVSEGroup charging",
                                "ChargingStationGroup faulted",
                                "ChargingPoolGroup outOfService",
                                "ChargingTariffGroup expired",
                                "ParkingSpaceGroup inUse"
                            }));

                Assert.That(follower!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(ParkingSpaceGroup), Level1.ToString(), out var groupStatus),  Is.True);
                Assert.That(groupStatus!.Value,                                                                                                              Is.EqualTo("inUse"));

            }

        }

        #endregion

    }

}
