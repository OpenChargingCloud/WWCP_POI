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
    /// Images of pools, stations, EVSEs and parking garages, and parking products of parking operators.
    /// </summary>
    [TestFixture]
    public class ImageAndParkingProductTests
    {

        #region Data

        private static readonly DateTimeOffset              Created   = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF       = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1        = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly EVSE_Id                     E1        = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly ParkingOperator_Id          PAR       = ParkingOperator_Id.        Parse("DE*PAR");
        private static readonly ParkingProduct_Id           Hourly    = ParkingProduct_Id.         Parse("Hourly");
        private static readonly ParkingProduct_Id           Daily     = ParkingProduct_Id.         Parse("Daily");
        private static readonly ParkingSpace_Id             SP1       = ParkingSpace_Id.           Parse("space-1");

        #endregion

        #region NewImage(Path, Category)

        private static Image NewImage(String Path, ImageCategory Category)

            => new (URL.Parse("https://images.example/" + Path),
                    HTTPContentType.Image.PNG,
                    Category,
                    Width:      800,
                    Height:     600,
                    Thumbnail:  URL.Parse("https://images.example/thumbs/" + Path));

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new (RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(P1,
                                                                                                                Images:            [ NewImage("location.png", ImageCategory.LOCATION), NewImage("entrance.png", ImageCategory.ENTRANCE) ],
                                                                                                                Created:           Created,
                                                                                                                ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                         Images:   [ NewImage("station.png", ImageCategory.CHARGER) ],
                                                                                                                                                         Created:  Created,
                                                                                                                                                         EVSEs:    [ new EVSE(E1, Images: [ NewImage("evse.png", ImageCategory.CHARGER) ], Created: Created) ]) ]) ]) ],
                    ParkingOperators:          [ new ParkingOperator(PAR,
                                                                     Created:             Created,
                                                                     ParkingProducts:     [ new ParkingProduct(Hourly, MinDuration: TimeSpan.FromMinutes(30), StopParkingAfterTime: TimeSpan.FromHours(4)),
                                                                                            new ParkingProduct(Daily,  StopParkingAfterTime: TimeSpan.FromHours(24)) ],
                                                                     ParkingGarages:      [ new ParkingGarage(ParkingGarage_Id.Parse("garage-1"),
                                                                                                              Images:             [ NewImage("garage.png", ImageCategory.ENTRANCE) ],
                                                                                                              ParkingProductIds:  [ Daily ],
                                                                                                              Created:            Created) ],
                                                                     ParkingSpaces:       [ new ParkingSpace(SP1,
                                                                                                             ParkingGarageId:    ParkingGarage_Id.Parse("garage-1"),
                                                                                                             ParkingProductIds:  [ Hourly, Daily ],
                                                                                                             Created:            Created) ],
                                                                     ParkingSpaceGroups:  [ new ParkingSpaceGroup(ParkingSpaceGroup_Id.Parse("level-1"),
                                                                                                                  ParkingSpaceIds:    [ SP1 ],
                                                                                                                  ParkingProductIds:  [ Hourly ],
                                                                                                                  Created:            Created) ]) ]);

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Images_and_parking_products_survive_JSON_JSON_text_and_CBOR()

        [Test]
        public void Images_and_parking_products_survive_JSON_JSON_text_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var pool     = json["chargingStationOperators"]![0]!["chargingPools"]![0]!;
            var parking  = json["parkingOperators"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                           Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                           Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                           Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                                    Is.EqualTo(rn1.CalcETags()));

                // Images keep their given order.
                Assert.That(pool["images"]!.Select(image => image["category"]!.ToString()),           Is.EqualTo(new[] { "LOCATION", "ENTRANCE" }));
                Assert.That(pool["images"]![0]!["width"]!.Value<Int32>(),                            Is.EqualTo(800));

                Assert.That(parking["parkingProducts"]!.Select(product => product["@id"]!.ToString()),  Is.EqualTo(new[] { "Daily", "Hourly" }));
                Assert.That(parking["parkingProducts"]![1]!["minDuration"]!.Value<Decimal>(),         Is.EqualTo(1800));
                Assert.That(parking["parkingSpaces"]![0]!["parkingProductIds"]!.Values<String>(),     Is.EqualTo(new[] { "Daily", "Hourly" }));

                Assert.That(fromCBOR.TryGetParkingProductById(Hourly, out var hourly),               Is.True);
                Assert.That(hourly!.StopParkingAfterTime,                                            Is.EqualTo(TimeSpan.FromHours(4)));
                Assert.That(fromCBOR.TryGetParkingProductById(ParkingProduct_Id.Parse("hourly"), out _),  Is.False, "Compared case-sensitively.");
                Assert.That(fromCBOR.GetChargingStationOperatorById(GEF)!.EVSEs.Single().Images.Single().Category,
                            Is.EqualTo(ImageCategory.CHARGER));

            }

        }

        #endregion

        #region Invalid_images_and_parking_products_are_refused()

        [Test]
        public void Invalid_images_and_parking_products_are_refused()
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

            JObject Pool(JObject Network)
                => (JObject) Network["chargingStationOperators"]![0]!["chargingPools"]![0]!;

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("unknown product",     Changed(network => Parking(network)["parkingSpaces"]![0]!["parkingProductIds"] = new JArray("Weekly")),   "parking product 'Weekly' of the parking space 'space-1'"),
                ("product twice",       Changed(network => Parking(network)["parkingProducts"]![0]!["@id"] = "Hourly"),                            "must not be given twice"),
                ("min above stop",      Changed(network => Parking(network)["parkingProducts"]![1]!["minDuration"] = 86400),                       "must not exceed"),
                ("image twice",         Changed(network => Pool(network)["images"]![1]!["url"] = "https://images.example/location.png"),         "must not be given twice"),
                ("image without type",  Changed(network => ((JObject) Pool(network)["images"]![0]!).Remove("type")),                               "Invalid images")
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

        #region Images_and_parking_products_are_changed_by_change_sets()

        [Test]
        public void Images_and_parking_products_are_changed_by_change_sets()
        {

            var rn1      = NewNetwork();
            var pool     = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            rn1.TryGetParkingProductById(Hourly, out var hourly);
            rn1.TryGetParkingSpaceById(SP1, out var sp1);

            var weekly   = ParkingProduct_Id.Parse("Weekly");

            var cs       = rn1.CreateChangeSet([
                               RoamingNetworkChange.Add           (new ParkingProduct(weekly, StopParkingAfterTime: TimeSpan.FromDays(7)), PAR),
                               RoamingNetworkChange.UpdateProperty(hourly!, "minDuration",        null, 900),
                               RoamingNetworkChange.UpdateProperty(sp1!,    "parkingProductIds",  null, new JArray("Weekly")),
                               RoamingNetworkChange.UpdateProperty(pool,    "images",             null, new JArray(NewImage("new.png", ImageCategory.OTHER).ToJSON()))
                           ], CreatedAt: T0);

            var rn2      = rn1.ApplyChangeSet(cs);
            rn2.TryGetParkingProductById(Hourly, out var changedHourly);
            rn2.TryGetParkingSpaceById(SP1, out var changedSP1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.ParkingProducts.Select(product => product.Id.ToString()),           Is.EqualTo(new[] { "Daily", "Hourly", "Weekly" }));
                Assert.That(changedHourly!.MinDuration,                                              Is.EqualTo(TimeSpan.FromMinutes(15)));
                Assert.That(changedSP1!.ParkingProductIds,                                           Is.EqualTo(new[] { weekly }));
                Assert.That(rn2.GetChargingStationOperatorById(GEF)!.ChargingPools.Single().Images.Single().Category,
                            Is.EqualTo(ImageCategory.OTHER));

                // Parking products have no timestamps.
                Assert.That(rn2.ToJSON()["parkingOperators"]![0]!["parkingProducts"]![2]!["created"],  Is.Null);

                // A parking product still named can not be removed.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(Daily)),
                            Throws.ArgumentException.With.Message.Contains("parking product 'Daily' of the parking garage 'garage-1'"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Add(new ParkingProduct(weekly), PAR)),
                            Throws.ArgumentException.With.Message.Contains("already exists"));

            }

        }

        #endregion

    }

}
