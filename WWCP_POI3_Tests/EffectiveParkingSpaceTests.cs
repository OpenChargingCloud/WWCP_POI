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
    /// Parking spaces taking the properties their parking space templates give them at runtime.
    /// </summary>
    [TestFixture]
    public class EffectiveParkingSpaceTests
    {

        #region Data

        private static readonly DateTimeOffset        Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset        Changed  = DateTimeOffset.Parse("2026-06-01T00:00:00Z");
        private static readonly DateTimeOffset        T0       = DateTimeOffset.Parse("2026-10-17T12:00:00Z");

        private static readonly ParkingOperator_Id    PAR      = ParkingOperator_Id.  Parse("DE*PAR");
        private static readonly ParkingGarage_Id      Garage   = ParkingGarage_Id.    Parse("garage-1");
        private static readonly ParkingSensor_Id      Sensor   = ParkingSensor_Id.    Parse("sensor-1");
        private static readonly ParkingSpace_Id       Space1   = ParkingSpace_Id.     Parse("space-1");
        private static readonly ParkingSpace_Id       Space2   = ParkingSpace_Id.     Parse("space-2");
        private static readonly ParkingSpaceTemplate_Id  Level1   = ParkingSpaceTemplate_Id.Parse("level-1");
        private static readonly ParkingSpaceTemplate_Id  Wide     = ParkingSpaceTemplate_Id.Parse("wide");

        #endregion

        #region NewNetwork()

        /// <summary>
        /// Two parking spaces on level 1 of a garage, roofed, 5 m long and 2.5 m wide, for passenger cars; the first one is
        /// wide, 3.5 m, by a template it names first, and has a length of its own.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:           Created,
                   ParkingOperators:  [ new ParkingOperator(PAR,
                                                            Created:             Created,
                                                            ParkingGarages:      [ new ParkingGarage(Garage, Created: Created) ],
                                                            ParkingSensors:      [ new ParkingSensor(Sensor, Created: Created) ],
                                                            ParkingSpaces:       [ new ParkingSpace(Space1, Length: Meter.From_m(6), TemplateIds: [ Wide, Level1 ], Created: Created),
                                                                                   new ParkingSpace(Space2,                          TemplateIds: [ Level1 ],       Created: Created) ],
                                                            ParkingSpaceTemplates:  [ new ParkingSpaceTemplate(Level1,
                                                                                                         ParkingGarageId:   Garage,
                                                                                                         ParkingSensorIds:  [ Sensor ],
                                                                                                         VehicleTypes:      [ VehicleType.PERSONAL_VEHICLE ],
                                                                                                         Length:            Meter.From_m(5),
                                                                                                         Width:             Meter.From_m(2.5m),
                                                                                                         Roofed:            true,
                                                                                                         Created:           Created),
                                                                                   new ParkingSpaceTemplate(Wide,
                                                                                                         Width:             Meter.From_m(3.5m),
                                                                                                         VehicleTypes:      [ VehicleType.VAN ],
                                                                                                         Created:           Created,
                                                                                                         LastChange:        Changed) ]) ]).WithKeys();

        #endregion

        #region (static) PAROf(Network)

        private static ParkingOperator PAROf(RoamingNetwork Network)
        {
            Network.TryGetParkingOperatorById(PAR, out var parkingOperator);
            return parkingOperator!;
        }

        #endregion


        #region Parking_space_templates_have_the_properties_of_parking_spaces()

        [Test]
        public void Parking_space_templates_have_the_properties_of_parking_spaces()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var level1 = json["parkingOperators"]![0]!["parkingSpaceTemplates"]![0]!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromJSON!.ToJSON().ToString(),                       Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                       Is.EqualTo(json.ToString()));
                Assert.That(level1["parkingGarageId"]!.Value<String>(),          Is.EqualTo("garage-1"));
                Assert.That(level1["width"]!.Value<Decimal>(),                   Is.EqualTo(2.5m));
                Assert.That(level1["roofed"]!.Value<Boolean>(),                  Is.True);
                Assert.That(json["parkingOperators"]![0]!["parkingSpaces"]![1]!["roofed"],  Is.Null);
            }

        }

        #endregion

        #region Parking_spaces_take_the_properties_of_their_templates()

        [Test]
        public void Parking_spaces_take_the_properties_of_their_templates()
        {

            var rn1  = NewNetwork();
            var par  = PAROf(rn1);

            Assert.That(par.TryGetEffectiveParkingSpace(Space1, out var space1), Is.True);
            Assert.That(par.TryGetEffectiveParkingSpace(Space2, out var space2), Is.True);

            using (Assert.EnterMultipleScope())
            {

                // Own values win, then the first template; arrays are united.
                Assert.That(space1!.Length,                                      Is.EqualTo(Meter.From_m(6)));
                Assert.That(space1.Width,                                        Is.EqualTo(Meter.From_m(3.5m)));
                Assert.That(space2!.Width,                                       Is.EqualTo(Meter.From_m(2.5m)));
                Assert.That(space2.Length,                                       Is.EqualTo(Meter.From_m(5)));
                Assert.That(space1.Roofed,                                       Is.True);
                Assert.That(space1.ParkingGarageId,                              Is.EqualTo(Garage));
                Assert.That(space1.ParkingSensorIds,                             Is.EqualTo(new[] { Sensor }));
                Assert.That(space1.VehicleTypes,                                 Is.EquivalentTo(new[] { VehicleType.PERSONAL_VEHICLE, VehicleType.VAN }));
                Assert.That(space2.VehicleTypes,                                 Is.EqualTo(new[] { VehicleType.PERSONAL_VEHICLE }));
                Assert.That(space1.LastChangeDate,                               Is.EqualTo(Changed));

                // As given, and in the roaming network.
                Assert.That(par.ParkingSpaces.First().Roofed,                    Is.Null);
                Assert.That(par.EffectiveParkingSpaces.First(),                  Is.SameAs(space1));
                Assert.That(par.TemplatesOf(par.ParkingSpaces.First()).Select(template => template.Id),  Is.EqualTo(new[] { Wide, Level1 }));
                Assert.That(rn1.TryGetEffectiveParkingSpaceById(Space2, out var fromNetwork),  Is.True);
                Assert.That(fromNetwork,                                         Is.SameAs(space2));
                Assert.That(rn1.EffectiveParkingSpaces.Count(space => space.Roofed == true),  Is.EqualTo(2));

            }

        }

        #endregion

        #region Invalid_parking_space_templates_are_refused()

        [Test]
        public void Invalid_parking_space_templates_are_refused()
        {

            var json = NewNetwork().ToJSON(IncludeETags: false);

            JObject With(Int32 Template, String PropertyName, JToken Value)
            {
                var changed = (JObject) json.DeepClone();
                changed["parkingOperators"]![0]!["parkingSpaceTemplates"]![Template]![PropertyName] = Value;
                return changed;
            }

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("unknown garage",      With(0, "parkingGarageId",     "garage-9"),                "parking garage 'garage-9' of the parking space template 'level-1'"),
                ("unknown sensor",      With(0, "parkingSensorIds",    new JArray("sensor-9")),    "parking sensor 'sensor-9' of the parking space template 'level-1'"),
                ("unknown station",     With(0, "chargingStationIds",  new JArray("DE*GEF*S9")),   "charging station 'DE*GEF*S9' of the parking space template 'level-1'"),
                ("no width",            With(0, "width",               0),                         "must be positive")
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

        #region Change_sets_change_what_parking_spaces_take()

        [Test]
        public void Change_sets_change_what_parking_spaces_take()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());
            var rn1     = history.Head.RoamingNetwork;

            PAROf(rn1).TryGetParkingSpace(Space2, out var space2);
            space2!.SetStatus(ParkingSpaceStatusType.InUse);

            PAROf(rn1).TryGetParkingSpaceTemplate(Level1, out var level1);

            // Space 2 leaves level 1: as it is given, changed now.
            Assert.That(history.Commit(rn1.CreateSignedChangeSet([
                            RoamingNetworkChange.RemoveProperty (space2,  "templateIds"),
                            RoamingNetworkChange.UpdateProperty(level1!, "roofed",          null, false)
                        ], CreatedAt: T0)).IsCommitted);

            var par2 = PAROf(history.Head.RoamingNetwork);

            par2.TryGetEffectiveParkingSpace(Space1, out var effective1);
            par2.TryGetEffectiveParkingSpace(Space2, out var effective2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(effective1!.Roofed,                                  Is.False);
                Assert.That(effective2!.Width,                                   Is.Null);
                Assert.That(effective2.LastChangeDate,                           Is.EqualTo(T0));
                Assert.That(effective2.Status.Value,                             Is.EqualTo(ParkingSpaceStatusType.InUse));
            }

        }

        #endregion

    }

}
