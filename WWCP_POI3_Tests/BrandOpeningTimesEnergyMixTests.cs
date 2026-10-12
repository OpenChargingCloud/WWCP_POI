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
    /// Brands of the roaming network, opening times of pools, stations and parking garages,
    /// and the energy mix of pools and tariffs.
    /// </summary>
    [TestFixture]
    public class BrandOpeningTimesEnergyMixTests
    {

        #region Data

        private static readonly DateTimeOffset              Created    = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0         = DateTimeOffset.Parse("2026-10-11T12:00:00Z");
        private static readonly DateTimeOffset              Christmas  = DateTimeOffset.Parse("2026-12-24T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF        = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1         = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1         = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1         = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly ChargingTariff_Id           T1         = ChargingTariff_Id.         Parse("DE*GEF*T1");
        private static readonly Brand_Id                    Fast       = Brand_Id.                  Parse("fast-charge");
        private static readonly Brand_Id                    Green      = Brand_Id.                  Parse("green-charge");

        #endregion

        #region NewOpeningTimes()

        private static ImmutableOpeningTimes NewOpeningTimes()

            => new (new OpeningTimes("Ask at the reception").
                        AddRegularOpening    (DayOfWeek.Monday, HourMin.Parse("07:00"), HourMin.Parse("21:00")).
                        AddRegularOpening    (DayOfWeek.Friday, HourMin.Parse("07:00"), HourMin.Parse("18:00")).
                        AddExceptionalClosing(Christmas, Christmas.AddHours(12)));

        #endregion

        #region NewEnergyMix(Product)

        private static EnergyMix NewEnergyMix(String Product)

            => new ([ new PercentageOf<EnergySourceCategory>(EnergySourceCategory.Parse("solar"), 60),
                      new PercentageOf<EnergySourceCategory>(EnergySourceCategory.Parse("wind"),  40) ],
                    // Unknown environmental impacts.
                    [],
                    new ImmutableI18NString(I18NString.Create(Languages.en, "Green Power Inc.")),
                    new ImmutableI18NString(I18NString.Create(Languages.en, Product)));

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new (RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    Brands:                    [ new Brand(Fast,  new ImmutableI18NString(I18NString.Create(Languages.en, "Fast Charge")),  Logo: URL.Parse("https://fast.example/logo.png")),
                                                 new Brand(Green, new ImmutableI18NString(I18NString.Create(Languages.en, "Green Charge"))) ],
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:          Created,
                                                                             ChargingTariffs:  [ new ChargingTariff(T1,
                                                                                                                    Currency.EUR,
                                                                                                                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                    EnergyMix:  NewEnergyMix("Tariff Green"),
                                                                                                                    Created:    Created) ],
                                                                             ChargingPools:    [ new ChargingPool(P1,
                                                                                                                  BrandIds:          [ Green, Fast ],
                                                                                                                  OpeningTimes:      NewOpeningTimes(),
                                                                                                                  EnergyMix:         NewEnergyMix("Pool Green"),
                                                                                                                  Created:           Created,
                                                                                                                  ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                           BrandIds:      [ Fast ],
                                                                                                                                                           OpeningTimes:  new ImmutableOpeningTimes(OpeningTimes.Open24Hours),
                                                                                                                                                           Created:       Created,
                                                                                                                                                           EVSEs:         [ new EVSE(E1, BrandIds: [ Fast ], Created: Created) ]) ]) ]) ],
                    ParkingOperators:          [ new ParkingOperator(ParkingOperator_Id.Parse("DE*PAR"),
                                                                     Created:         Created,
                                                                     ParkingGarages:  [ new ParkingGarage(ParkingGarage_Id.Parse("garage-1"),
                                                                                                          OpeningTimes:  NewOpeningTimes(),
                                                                                                          Created:       Created) ]) ]);

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Brands_opening_times_and_energy_mixes_survive_JSON_JSON_text_and_CBOR()

        [Test]
        public void Brands_opening_times_and_energy_mixes_survive_JSON_JSON_text_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var pool = json["chargingStationOperators"]![0]!["chargingPools"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                              Is.EqualTo(rn1.CalcETags()));

                Assert.That(json["brands"]!.Select(brand => brand["@id"]!.ToString()),          Is.EqualTo(new[] { "fast-charge", "green-charge" }));
                Assert.That(pool["brandIds"]!.Values<String>(),                                Is.EqualTo(new[] { "fast-charge", "green-charge" }));
                Assert.That(pool["openingTimes"]!["24/7"]!.Value<Boolean>(),                    Is.False);
                Assert.That(pool["openingTimes"]!["exceptionalClosings"]!.Single().ToString(),  Does.StartWith("2026-12-24T12:00:00.000Z"));
                Assert.That(pool["energyMix"]!["environmentalImpacts"]!.Count(),                Is.Zero, "Unknown impacts are kept as [].");
                Assert.That(pool["energyMix"]!["@context"],                                     Is.Null, "Embedded, without its context.");

                Assert.That(fromJSON.TryGetBrandById(Fast, out var fast),                       Is.True);
                Assert.That(fast!.Logo?.ToString(),                                             Is.EqualTo("https://fast.example/logo.png"));

                var p1 = fromCBOR.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
                Assert.That(p1.OpeningTimes!.RegularOpenings.Keys,                              Is.EquivalentTo(new[] { DayOfWeek.Monday, DayOfWeek.Friday }));
                Assert.That(p1.ChargingStations.Single().OpeningTimes!.IsOpen24Hours,           Is.True);
                Assert.That(p1.EnergyMix!.ProductName.FirstText(),                              Is.EqualTo("Pool Green"));
                Assert.That(fromCBOR.TryGetChargingTariffById(T1, out var t1),                  Is.True);
                Assert.That(t1!.EnergyMix!.ProductName.FirstText(),                             Is.EqualTo("Tariff Green"));
                Assert.That(fromCBOR.ParkingGarages.Single().OpeningTimes!.FreeText,           Is.EqualTo("Ask at the reception"));

            }

        }

        #endregion

        #region Invalid_brands_opening_times_and_energy_mixes_are_refused()

        [Test]
        public void Invalid_brands_opening_times_and_energy_mixes_are_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            JObject Pool(JObject Network)
                => (JObject) Network["chargingStationOperators"]![0]!["chargingPools"]![0]!;

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("unknown brand",     Changed(network => Pool(network)["chargingStations"]![0]!["EVSEs"]![0]!["brandIds"] = new JArray("nobody")),  "brand 'nobody' of the EVSE 'DE*GEF*E1*1'"),
                ("brand without name",Changed(network => ((JObject) network["brands"]![1]!).Remove("name")),                                     "needs its name"),
                ("24/7 and openings", Changed(network => Pool(network)["openingTimes"]!["24/7"] = true),                                          "opening times"),
                ("unknown weekday",   Changed(network => Pool(network)["openingTimes"]!["regularOpenings"]![0]!["moonday"] = new JArray()),      "opening times"),
                ("no supplier",       Changed(network => ((JObject) Pool(network)["energyMix"]!).Remove("supplierName")),                         "energy mix")
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

        #region Brands_opening_times_and_energy_mixes_are_changed_by_change_sets()

        [Test]
        public void Brands_opening_times_and_energy_mixes_are_changed_by_change_sets()
        {

            var rn1     = NewNetwork();
            var pool    = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            rn1.TryGetBrandById(Green, out var green);
            rn1.TryGetChargingTariffById(T1, out var t1);

            var blue    = Brand_Id.Parse("blue-charge");

            var rn2     = Apply(rn1,
                                RoamingNetworkChange.Add           (new Brand(blue, new ImmutableI18NString(I18NString.Create(Languages.en, "Blue Charge")))),
                                RoamingNetworkChange.UpdateProperty(green!, "homepage",      null, "https://green.example"),
                                RoamingNetworkChange.UpdateProperty(pool,   "brandIds",      null, new JArray("blue-charge")),
                                RoamingNetworkChange.UpdateProperty(pool,   "openingTimes",  null, OpeningTimes.Open24Hours.ToJSON()),
                                RoamingNetworkChange.RemoveProperty(t1!,    "energyMix"));

            var pool2   = rn2.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            rn2.TryGetChargingTariffById(T1, out var changedT1);
            rn2.TryGetBrandById(Green, out var changedGreen);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.Brands.Select(brand => brand.Id.ToString()).Order(),  Is.EqualTo(new[] { "blue-charge", "fast-charge", "green-charge" }));
                Assert.That(changedGreen!.Homepage?.ToString(),                       Is.EqualTo("https://green.example"));
                Assert.That(pool2.BrandIds,                                           Is.EqualTo(new[] { blue }));
                Assert.That(pool2.OpeningTimes!.IsOpen24Hours,                        Is.True);
                Assert.That(changedT1!.EnergyMix,                                     Is.Null);

                // A brand still named can not be removed.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(Fast)),
                            Throws.ArgumentException.With.Message.Contains("brand 'fast-charge' of the charging station 'DE*GEF*S1'"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(pool2, "openingTimes", null, new JObject(new JProperty("open", true)))),
                            Throws.ArgumentException.With.Message.Contains("opening times"));

            }

        }

        #endregion

    }

}
