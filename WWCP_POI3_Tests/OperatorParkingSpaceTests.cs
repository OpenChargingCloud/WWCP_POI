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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Parking spaces a charging station operator defines itself, where no parking operator does, and what a parking
    /// space tells as an OCPI parking: its vehicles, measures, position, layout and equipment.
    /// </summary>
    [TestFixture]
    public class OperatorParkingSpaceTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-16T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ParkingOperator_Id          PAR      = ParkingOperator_Id.        Parse("DE*PAR");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");

        private static readonly ParkingSpace_Id             GS1      = ParkingSpace_Id.Parse("GEF-1");
        private static readonly ParkingSpace_Id             GS2      = ParkingSpace_Id.Parse("GEF-2");
        private static readonly ParkingSpace_Id             PS1      = ParkingSpace_Id.Parse("PAR-1");

        #endregion

        #region ASpace(Id)

        private static ParkingSpace ASpace(ParkingSpace_Id Id)

            => new (Id,
                    PhysicalReference:         "17",
                    VehicleTypes:              [ VehicleType.VAN, VehicleType.PERSONAL_VEHICLE ],
                    MaxVehicleWeight:          Kilogram.FromKG(3500),
                    MaxVehicleHeight:          Meter.   From_m(2.8m),
                    MaxVehicleLength:          Meter.   From_m(6),
                    MaxVehicleWidth:           Meter.   From_m(2.2m),
                    Length:                    Meter.   From_m(6.5m),
                    Width:                     Meter.   From_m(3.5m),
                    EVSEPosition:              EVSEPosition.LEFT,
                    Direction:                 ParkingDirection.PERPENDICULAR,
                    Surface:                   ParkingSurface.ASPHALT,
                    Slope:                     ParkingSlope.FLAT,
                    TimeLimit:                 TimeSpan.FromHours(4),
                    RestrictedToVehicleTypes:  false,
                    DriveThrough:              false,
                    ReservationRequired:       false,
                    Roofed:                    true,
                    Illuminated:               true,
                    RefrigerationOutlet:       false,
                    DangerousGoodsAllowed:     false,
                    ProtectedArea:             true,
                    APDSReference:             "APDS-4711",
                    Standards:                 [ Standard.PAS1899_2022 ],
                    Images:                    [ new Image(URL.Parse("https://gef.example/space.jpg"), HTTPContentType.Image.JPEG, ImageCategory.LOCATION) ],
                    Created:                   Created);

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:        Created,
                                                                             ParkingSpaces:  [ ASpace(GS1) ],
                                                                             ChargingPools:  [ new ChargingPool(P1,
                                                                                                                Created:           Created,
                                                                                                                ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                         ParkingSpaceIds:  [ GS1, PS1 ],
                                                                                                                                                         Created:          Created) ]) ]) ],
                    ParkingOperators:          [ new ParkingOperator(PAR,
                                                                     Created:        Created,
                                                                     ParkingSpaces:  [ new ParkingSpace(PS1, Created: Created) ]) ]).WithKeys();

        #endregion

        #region Change(Network, Changes) / Apply(Network, ChangeSet)

        private static RoamingNetworkChangeSet Change(RoamingNetwork                Network,
                                                      params RoamingNetworkChange[] Changes)

            => Network.CreateChangeSet(Changes, CreatedAt: T0);

        private static (Boolean Applied, String? Error, RoamingNetwork? Network) Apply(RoamingNetwork           Network,
                                                                                       RoamingNetworkChangeSet  ChangeSet)

            => (Network.TryApplyChangeSet(ChangeSet, out var changed, out var error), error, changed);

        #endregion


        #region Operator_parking_spaces_survive_JSON_JSON_text_and_CBOR()

        [Test]
        public void Operator_parking_spaces_survive_JSON_JSON_text_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var space = (JObject) json["chargingStationOperators"]![0]!["parkingSpaces"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                         Is.EqualTo(rn1.CalcETags()));

                Assert.That(space["@id"]!.Value<String>(),                                Is.EqualTo("GEF-1"));
                Assert.That(space["vehicleTypes"]!.Values<String>(),                      Is.EqualTo(new[] { "PERSONAL_VEHICLE", "VAN" }));
                Assert.That(space["maxVehicleWeight"]!.Value<Decimal>(),                  Is.EqualTo(3500m), "In kilograms.");
                Assert.That(space["length"]!.Value<Decimal>(),                            Is.EqualTo(6.5m),  "In meters.");
                Assert.That(space["timeLimit"]!.Value<Decimal>(),                         Is.EqualTo(14400m), "In seconds.");
                Assert.That(space["evsePosition"]!.Value<String>(),                       Is.EqualTo("LEFT"));
                Assert.That(space["direction"]!.Value<String>(),                          Is.EqualTo("PERPENDICULAR"));
                Assert.That(space["surface"]!.Value<String>(),                            Is.EqualTo("ASPHALT"));
                Assert.That(space["slope"]!.Value<String>(),                              Is.EqualTo("FLAT"));
                Assert.That(space["roofed"]!.Value<Boolean>(),                            Is.True);
                Assert.That(space["driveThrough"]!.Value<Boolean>(),                      Is.False, "false is written, unknown is not.");
                Assert.That(space["apdsReference"]!.Value<String>(),                      Is.EqualTo("APDS-4711"));

                // Found by the network as every parking space, the one of the parking operator as well.
                Assert.That(fromCBOR.TryGetParkingSpaceById(GS1, out var gs1),            Is.True);
                Assert.That(gs1!.MaxVehicleHeight?.m,                                     Is.EqualTo(2.8m));
                Assert.That(gs1.Images.Single().URL,                                      Is.EqualTo(URL.Parse("https://gef.example/space.jpg")));
                Assert.That(fromCBOR.TryGetParkingSpaceById(PS1, out _),                  Is.True);
                Assert.That(fromCBOR.GetChargingStationOperatorById(GEF)!.ParkingSpaces.Single().Id,  Is.EqualTo(GS1));

            }

        }

        #endregion

        #region Invalid_parking_spaces_are_refused()

        [Test]
        public void Invalid_parking_spaces_are_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            JObject Space(JObject Network)
                => (JObject) Network["chargingStationOperators"]![0]!["parkingSpaces"]![0]!;

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("in a garage",            Changed(network => Space(network)["parkingGarageId"]  = "garage-1"),                       "can be in no parking garage"),
                ("with a sensor",          Changed(network => Space(network)["parkingSensorIds"] = new JArray("sensor-1")),           "can be in no parking garage"),
                ("twice in the network",   Changed(network => Space(network)["@id"]              = "PAR-1"),                          "parking space 'PAR-1' must not be given twice"),
                ("zero length",            Changed(network => Space(network)["length"]           = 0),                                "length of the parking space 'GEF-1' must be positive"),
                ("negative weight",        Changed(network => Space(network)["maxVehicleWeight"] = -1),                               "maximum vehicle weight must not be negative"),
                ("negative time limit",    Changed(network => Space(network)["timeLimit"]        = -60),                              "time limit must not be negative"),
                ("lower case direction",   Changed(network => Space(network)["direction"]        = "parallel"),                       "direction"),
                ("roofed as text",         Changed(network => Space(network)["roofed"]           = "yes"),                            "roofed"),
                ("unknown station",        Changed(network => Space(network)["chargingStationIds"] = new JArray("DE*GEF*S9")),        "charging station 'DE*GEF*S9' of the parking space 'GEF-1'")
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

        #region A_charging_station_operator_changes_its_own_parking_spaces()

        [Test]
        public void A_charging_station_operator_changes_its_own_parking_spaces()
        {

            var rn1     = NewNetwork();
            var gs1     = rn1.GetChargingStationOperatorById(GEF)!.ParkingSpaces.Single();
            var ps1     = rn1.ParkingOperators.Single().ParkingSpaces.Single();

            var add     = Change(rn1, RoamingNetworkChange.Add(ASpace(GS2), GEF));
            var update  = Change(rn1, RoamingNetworkChange.UpdateProperty(gs1, "timeLimit", null, 7200));

            using (Assert.EnterMultipleScope())
            {

                // The charging station operator signs its own parking spaces...
                var (addedByGEF, addError, rn2) = Apply(rn1, add.SignedBy(TestParties.Party(GEF)));

                Assert.That(addedByGEF,                                                                    Is.True, addError);
                Assert.That(rn2!.GetChargingStationOperatorById(GEF)!.ParkingSpaces.Select(space => space.Id), Is.EqualTo(new[] { GS1, GS2 }));
                Assert.That(rn2.TryGetParkingSpaceById(GS2, out _),                                         Is.True);

                var (updatedByGEF, updateError, rn3) = Apply(rn1, update.SignedBy(TestParties.Party(GEF)));

                Assert.That(updatedByGEF,                                                                  Is.True, updateError);
                Assert.That(rn3!.GetChargingStationOperatorById(GEF)!.ParkingSpaces.Single().TimeLimit,    Is.EqualTo(TimeSpan.FromHours(2)));

                // ...a parking operator does not.
                Assert.That(Apply(rn1, add.   SignedBy(TestParties.Party(PAR))).Error,                    Does.Contain("needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'"));
                Assert.That(Apply(rn1, update.SignedBy(TestParties.Party(PAR))).Error,                    Does.Contain("needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'"));

                // An identification of a parking space of a parking operator is taken.
                Assert.That(() => Change(rn1, RoamingNetworkChange.Add(ASpace(PS1), GEF)),
                            Throws.ArgumentException.With.Message.Contains("the parking space 'PAR-1' already exists"));

                // A charging station operator's parking space is in no garage.
                Assert.That(() => Change(rn1, RoamingNetworkChange.UpdateProperty(gs1, "parkingGarageId", null, "garage-1")),
                            Throws.ArgumentException.With.Message.Contains("can be in no parking garage"));

                // A parking space named by a charging station is not removed.
                Assert.That(() => Change(rn1, RoamingNetworkChange.Remove(GS1)),
                            Throws.ArgumentException.With.Message.Contains("parking space 'GEF-1' of the charging station 'DE*GEF*S1'"));

            }

        }

        #endregion

        #region A_parking_operator_takes_over_a_parking_space()

        [Test]
        public void A_parking_operator_takes_over_a_parking_space()
        {

            var rn1        = NewNetwork();
            var gs1        = rn1.GetChargingStationOperatorById(GEF)!.ParkingSpaces.Single();

            // Removed at the charging station operator and added at the parking operator, with all it says: both sign.
            var takeover   = Change(rn1, RoamingNetworkChange.Remove(GS1),
                                         RoamingNetworkChange.Add   (gs1, PAR));

            var (onlyByPAR, onlyByPARError, _)  = Apply(rn1, takeover.SignedBy(TestParties.Party(PAR)));
            var (byBoth,    byBothError,    rn2) = Apply(rn1, takeover.SignedBy(TestParties.Party(GEF)).
                                                                       SignedBy(TestParties.Party(PAR)));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(onlyByPAR,                                                                     Is.False);
                Assert.That(onlyByPARError,                                                                Does.Contain("needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'"));

                Assert.That(byBoth,                                                                        Is.True, byBothError);
                Assert.That(rn2!.GetChargingStationOperatorById(GEF)!.ParkingSpaces,                        Is.Empty);
                Assert.That(rn2.ParkingOperators.Single().TryGetParkingSpace(GS1, out var taken),          Is.True);
                Assert.That(taken!.APDSReference,                                                          Is.EqualTo("APDS-4711"));
                Assert.That(rn2.GetChargingStationOperatorById(GEF)!.ChargingStations.Single().ParkingSpaceIds,
                            Is.EqualTo(new[] { GS1, PS1 }), "The charging station still names it.");

            }

        }

        #endregion

        #region Merges_and_runtime_status_reach_operator_parking_spaces()

        [Test]
        public void Merges_and_runtime_status_reach_operator_parking_spaces()
        {

            var rn1      = NewNetwork();
            var gs1      = rn1.GetChargingStationOperatorById(GEF)!.ParkingSpaces.Single();

            var right    = rn1.ApplyChangeSet(rn1.CreateSignedChangeSet([ RoamingNetworkChange.Add(ASpace(GS2), GEF),
                                                                          RoamingNetworkChange.UpdateProperty(gs1, "roofed", null, false) ],
                                                                        CreatedAt: T0));

            var merged   = RoamingNetwork.TryMerge(rn1, rn1, right, ChangeSet_Id.NewRandom(), out var merge, out var changeSet, out var conflicts);

            var history  = RoamingNetworkHistory.Start(rn1);
            var updated  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(ParkingSpace), GS1.ToString(), "inUse", Created.AddDays(1), out _, out var statusError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(merged,                                                                        Is.True, String.Join(" ", conflicts.Select(conflict => conflict.Description)));
                Assert.That(changeSet!.Changes.Select(change => (change.Kind, change.ParentEntityType ?? change.PropertyName)),
                            Is.EquivalentTo(new[] { (RoamingNetworkChangeKind.Add,            nameof(ChargingStationOperator)),
                                                    (RoamingNetworkChangeKind.UpdateProperty, "roofed") }));
                Assert.That(merge!.GetChargingStationOperatorById(GEF)!.ParkingSpaces.Select(space => space.Id),
                            Is.EqualTo(new[] { GS1, GS2 }));

                // Refused for a parking space unknown in the network.
                Assert.That(updated,                                                                       Is.EqualTo(POIStatusUpdateResult.Updated), statusError);
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(ParkingSpace), GS1.ToString(), out var status),  Is.True);
                Assert.That(status!.Value,                                                                 Is.EqualTo("inUse"));

            }

        }

        #endregion

    }

}
