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
    /// Parking operators with their parking garages, parking spaces and parking sensors.
    /// </summary>
    [TestFixture]
    public class ParkingTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly ParkingOperator_Id          PAR      = ParkingOperator_Id.        Parse("DE*PAR");
        private static readonly ParkingGarage_Id            G1       = ParkingGarage_Id.          Parse("garage-1");
        private static readonly ParkingSpace_Id             SP1      = ParkingSpace_Id.           Parse("space-1");
        private static readonly ParkingSpace_Id             SP2      = ParkingSpace_Id.           Parse("space-2");
        private static readonly ParkingSensor_Id            SN1      = ParkingSensor_Id.          Parse("sensor-1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                Created:           Created,
                                                                                                                ChargingStations:  [ new ChargingStation(S1, Created: Created) ]) ]) ],
                    ParkingOperators:          [ new ParkingOperator(PAR,
                                                                     I18NString.Create(Languages.en, "Example Parking"),
                                                                     Created:         Created,
                                                                     ParkingGarages:  [ new ParkingGarage(G1,
                                                                                                          I18NString.Create(Languages.en, "Garage 1"),
                                                                                                          GeoLocation:         GeoCoordinate.Parse(50.92, 11.58),
                                                                                                          ChargingStationIds:  [ S1 ],
                                                                                                          Created:             Created) ],
                                                                     ParkingSpaces:   [ new ParkingSpace(SP1,
                                                                                                         ParkingGarageId:     G1,
                                                                                                         ParkingSensorIds:    [ SN1 ],
                                                                                                         ChargingStationIds:  [ S1 ],
                                                                                                         Created:             Created),
                                                                                        new ParkingSpace(SP2,
                                                                                                         ParkingGarageId:     G1,
                                                                                                         Created:             Created) ],
                                                                     ParkingSensors:  [ new ParkingSensor(SN1, Created: Created) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Parking_survives_JSON_and_CBOR()

        [Test]
        public void Parking_survives_JSON_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var parking = json["parkingOperators"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                  Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                  Is.EqualTo(json.ToString()));
                Assert.That(fromJSON.CalcETags(),                                           Is.EqualTo(rn1.CalcETags()));

                Assert.That(parking["parkingGarages"]![0]!["chargingStationIds"]!.Values<String>(),  Is.EqualTo(new[] { "DE*GEF*S1" }));
                Assert.That(parking["parkingSpaces"]!.Select(space => space["@id"]!.ToString()),      Is.EqualTo(new[] { "space-1", "space-2" }));
                Assert.That(parking["parkingSpaces"]![0]!["parkingGarageId"]!.ToString(),            Is.EqualTo("garage-1"));
                Assert.That(parking["parkingSpaces"]![0]!["parkingSensorIds"]!.Values<String>(),     Is.EqualTo(new[] { "sensor-1" }));

                Assert.That(fromJSON.TryGetParkingOperatorById(PAR, out _),                 Is.True);
                Assert.That(fromJSON.TryGetParkingSpaceById(SP2, out var sp2),              Is.True);
                Assert.That(sp2!.ParkingGarageId,                                           Is.EqualTo(G1));
                Assert.That(fromJSON.TryGetParkingSensorById(SN1, out _),                   Is.True);
                Assert.That(fromJSON.TryGetParkingGarageById(G1, out _),                    Is.True);

            }

        }

        #endregion

        #region Invalid_parking_is_refused()

        [Test]
        public void Invalid_parking_is_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            JObject Parking(JObject Network)
                => (JObject) Network["parkingOperators"]![0]!;

            var unknownGarage   = Changed(network => Parking(network)["parkingSpaces"]![0]!["parkingGarageId"] = "garage-9");
            var unknownSensor   = Changed(network => Parking(network)["parkingSpaces"]![0]!["parkingSensorIds"] = new JArray("sensor-9"));
            var unknownStation  = Changed(network => Parking(network)["parkingGarages"]![0]!["chargingStationIds"] = new JArray("DE*GEF*S9"));
            var spaceTwice      = Changed(network => Parking(network)["parkingSpaces"]![1]!["@id"] = "SPACE-1");

            // The same sensor at two parking operators.
            var otherOperator   = Changed(network => ((JArray) network["parkingOperators"]!).Add(
                                                         new JObject(new JProperty("@id",             "DE*OTH"),
                                                                     new JProperty("parkingSensors",  new JArray(new JObject(new JProperty("@id", "sensor-1")))))));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RoamingNetwork.TryParse(unknownGarage,   out _, out var error1),  Is.False);
                Assert.That(error1,                                                           Does.Contain("parking garage 'garage-9'"));
                Assert.That(RoamingNetwork.TryParse(unknownSensor,   out _, out var error2),  Is.False);
                Assert.That(error2,                                                           Does.Contain("parking sensor 'sensor-9'"));
                Assert.That(RoamingNetwork.TryParse(unknownStation,  out _, out var error3),  Is.False);
                Assert.That(error3,                                                           Does.Contain("charging station 'DE*GEF*S9'"));
                Assert.That(RoamingNetwork.TryParse(spaceTwice,      out _, out _),           Is.False);
                Assert.That(RoamingNetwork.TryParse(otherOperator,   out _, out var error5),  Is.False);
                Assert.That(error5,                                                           Does.Contain("'sensor-1' must not be given twice in the roaming network"));
            }

        }

        #endregion

        #region Parking_is_changed_by_change_sets()

        [Test]
        public void Parking_is_changed_by_change_sets()
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var rn1       = history.Head.RoamingNetwork;
            var parking   = rn1.ParkingOperators.Single();
            rn1.TryGetParkingSpaceById(SP1, out var sp1);

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(ParkingSpace), "space-1", "inUse", T0, out _, out _);

            var sn2       = ParkingSensor_Id.Parse("sensor-2");

            var cs        = rn1.CreateSignedChangeSet([
                                RoamingNetworkChange.Add           (new ParkingSensor(sn2), PAR),
                                RoamingNetworkChange.UpdateProperty(sp1!,     "parkingSensorIds", null, new JArray("sensor-1", "sensor-2")),
                                RoamingNetworkChange.Remove        (SP2),
                                RoamingNetworkChange.UpdateProperty(parking,  "homepage",         null, "https://parking.example")
                            ], CreatedAt: T0.AddMinutes(1));

            history.Commit(cs);

            var rn2       = history.Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.TryGetParkingSpaceById(SP1, out var changedSP1),                           Is.True);
                Assert.That(changedSP1!.ParkingSensorIds.Select(id => id.ToString()),                     Is.EqualTo(new[] { "sensor-1", "sensor-2" }));
                Assert.That(changedSP1.LastChangeDate,                                                    Is.EqualTo(T0.AddMinutes(1)));
                Assert.That(rn2.TryGetParkingSpaceById(SP2, out _),                                        Is.False);
                Assert.That(rn2.TryGetParkingSensorById(sn2, out _),                                       Is.True);
                Assert.That(rn2.ParkingOperators.Single().Homepage?.ToString(),                            Is.EqualTo("https://parking.example"));

                // Old values for a later check.
                Assert.That(cs.Changes[2].OldValue!["@id"]!.ToString(),                                    Is.EqualTo("space-2"));

                // The status of the space survived its change, and that of its operator.
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(ParkingSpace), "space-1", out var spaceStatus),  Is.True);
                Assert.That(spaceStatus!.Value,                                                                                             Is.EqualTo("inUse"));

                // Invalid changes.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(G1)),
                            Throws.ArgumentException.With.Message.Contains("parking garage 'garage-1' of the parking space 'space-1'"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Add("ParkingSensor", "sensor-9", new ParkingSensor(ParkingSensor_Id.Parse("sensor-9")).ToJSON())),
                            Throws.ArgumentException.With.Message.Contains("needs its ParkingOperator"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Add(new ParkingSensor(SN1), PAR)),
                            Throws.ArgumentException.With.Message.Contains("already exists"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(changedSP1, "@id", null, "space-9")),
                            Throws.ArgumentException.With.Message.Contains("can not be changed"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(S1)),
                            Throws.ArgumentException.With.Message.Contains("charging station 'DE*GEF*S1' of the parking garage 'garage-1'"));

            }

        }

        #endregion

        #region The_runtime_status_reaches_parking()

        [Test]
        public void The_runtime_status_reaches_parking()
        {

            var leader   = RoamingNetworkHistory.Start(NewNetwork());
            var updates  = new List<POIStatusUpdate>();

            leader.RuntimeStatus.OnStatusUpdate += updates.Add;

            var results  = new[] {
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(ParkingOperator), "DE*PAR",   "outOfService", T0, out _, out _),
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ParkingGarage),   "garage-1", "blocked",      T0, out _, out _),
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ParkingSpace),    "space-2",  "reserved",     T0, out _, out _),
                               leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ParkingSensor),   "sensor-1", "blocked",      T0, out _, out _)
                           };

            var unknown  = leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(ParkingSpace), "space-9", "reserved", T0, out _, out _);

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotCBOR(), out var follower, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(results,  Is.All.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(unknown,  Is.EqualTo(POIStatusUpdateResult.Refused));

                Assert.That(updates.Select(update => $"{update.EntityType} {update.Value}"),
                            Is.EqualTo(new[] {
                                "ParkingOperator outOfService",
                                "ParkingGarage blocked",
                                "ParkingSpace reserved",
                                "ParkingSensor blocked"
                            }));

                Assert.That(follower!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(ParkingSpace), "space-2", out var spaceStatus),  Is.True);
                Assert.That(spaceStatus!.Value,                                                                                                 Is.EqualTo("reserved"));

            }

        }

        #endregion

    }

}
