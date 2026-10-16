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

using System.Collections.Immutable;

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
        private static readonly Brand_Id                    White      = Brand_Id.                  Parse("white-charge");

        #endregion

        #region FastBrand(), GreenBrand(), WhiteBrand()

        // A brand is embedded in what it is a brand of: every level has its own, each a copy of its own.

        private static Brand FastBrand()

            => new (Fast,  new ImmutableI18NString(I18NString.Create(Languages.en, "Fast Charge")), Logo: URL.Parse("https://fast.example/logo.png"));

        private static Brand GreenBrand()

            => new (Green, new ImmutableI18NString(I18NString.Create(Languages.en, "Green Charge")));

        private static Brand WhiteBrand()

            => new (White, new ImmutableI18NString(I18NString.Create(Languages.en, "White Charge")));

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

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:          Created,
                                                                             // A white label: the operator charges under this brand.
                                                                             Brands:           [ WhiteBrand() ],
                                                                             ChargingTariffs:  [ new ChargingTariff(T1,
                                                                                                                    Currency.EUR,
                                                                                                                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                    EnergyMix:  NewEnergyMix("Tariff Green"),
                                                                                                                    Created:    Created) ],
                                                                             ChargingPools:    [ new ChargingPool(P1,
                                                                                                                  Brands:            [ GreenBrand(), FastBrand() ],
                                                                                                                  OpeningTimes:      NewOpeningTimes(),
                                                                                                                  EnergyMix:         NewEnergyMix("Pool Green"),
                                                                                                                  Created:           Created,
                                                                                                                  ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                           Brands:        [ FastBrand() ],
                                                                                                                                                           OpeningTimes:  new ImmutableOpeningTimes(OpeningTimes.Open24Hours),
                                                                                                                                                           Created:       Created,
                                                                                                                                                           EVSEs:         [ new EVSE(E1, Brands: [ FastBrand() ], Created: Created) ]) ]) ]) ],
                    ParkingOperators:          [ new ParkingOperator(ParkingOperator_Id.Parse("DE*PAR"),
                                                                     Created:         Created,
                                                                     ParkingGarages:  [ new ParkingGarage(ParkingGarage_Id.Parse("garage-1"),
                                                                                                          OpeningTimes:  NewOpeningTimes(),
                                                                                                          Created:       Created) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

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

            var cso  = json["chargingStationOperators"]![0]!;
            var pool = cso["chargingPools"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                              Is.EqualTo(rn1.CalcETags()));

                // Every level its own brands, embedded, in the order of their identifications; none of the network.
                Assert.That(json["brands"],                                                     Is.Null);
                Assert.That(cso ["brands"]!.Select(brand => brand["@id"]!.ToString()),          Is.EqualTo(new[] { "white-charge" }));
                Assert.That(pool["brands"]!.Select(brand => brand["@id"]!.ToString()),          Is.EqualTo(new[] { "fast-charge", "green-charge" }));
                Assert.That(pool["brandIds"],                                                   Is.Null);
                Assert.That(pool["openingTimes"]!["24/7"]!.Value<Boolean>(),                    Is.False);
                Assert.That(pool["openingTimes"]!["exceptionalClosings"]!.Single().ToString(),  Does.StartWith("2026-12-24T12:00:00.000Z"));
                Assert.That(pool["energyMix"]!["environmentalImpacts"]!.Count(),                Is.Zero, "Unknown impacts are kept as [].");
                Assert.That(pool["energyMix"]!["@context"],                                     Is.Null, "Embedded, without its context.");

                var station = fromJSON.GetChargingStationOperatorById(GEF)!.ChargingPools.Single().ChargingStations.Single();
                Assert.That(station.Brands.TryGet(Fast, out var fast),                          Is.True);
                Assert.That(fast!.Logo?.ToString(),                                             Is.EqualTo("https://fast.example/logo.png"));
                Assert.That(fromCBOR.GetChargingStationOperatorById(GEF)!.Brands.Single().Id,  Is.EqualTo(White));

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
                ("brand ids",         Changed(network => Pool(network)["chargingStations"]![0]!["EVSEs"]![0]!["brandIds"] = new JArray("fast-charge")), "must be given completely in 'brands', not by 'brandIds'"),
                ("brand without name",Changed(network => ((JObject) Pool(network)["brands"]![1]!).Remove("name")),                               "needs its name"),
                ("brand twice",       Changed(network => ((JArray) Pool(network)["brands"]!).Add(Pool(network)["brands"]![0]!.DeepClone())),      "The brand 'fast-charge' of the charging pool 'DE*GEF*P1' must not be given twice"),
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
            rn1.TryGetChargingTariffById(T1, out var t1);

            var blue    = new Brand(Brand_Id.Parse("blue-charge"), new ImmutableI18NString(I18NString.Create(Languages.en, "Blue Charge")));

            ImmutableArray<POIElementPathSegment> BrandPath(String BrandId)
                => [ new POIElementPathSegment("brands", BrandId) ];

            // A brand of the pool added, one of its brands changed, and the operator charging under one more brand.
            var rn2     = Apply(rn1,
                                RoamingNetworkChange.AddElement           (nameof(ChargingPool),            P1. ToString(), BrandPath("blue-charge"), blue.ToJSON()),
                                RoamingNetworkChange.UpdateElementProperty(nameof(ChargingPool),            P1. ToString(), BrandPath("fast-charge"), "homepage", null, "https://fast.example"),
                                RoamingNetworkChange.AddElement           (nameof(ChargingStationOperator), GEF.ToString(), BrandPath("blue-charge"), blue.ToJSON()),
                                RoamingNetworkChange.UpdateProperty       (pool,   "openingTimes",  null, OpeningTimes.Open24Hours.ToJSON()),
                                RoamingNetworkChange.RemoveProperty       (t1!,    "energyMix"));

            var cso2    = rn2.GetChargingStationOperatorById(GEF)!;
            var pool2   = cso2.ChargingPools.Single();
            var station = pool2.ChargingStations.Single();
            rn2.TryGetChargingTariffById(T1, out var changedT1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(pool2.Brands.Select(brand => brand.Id.ToString()),        Is.EqualTo(new[] { "blue-charge", "fast-charge", "green-charge" }));
                Assert.That(pool2.Brands.TryGet(Fast, out var poolFast),              Is.True);
                Assert.That(poolFast!.Homepage?.ToString(),                           Is.EqualTo("https://fast.example"));
                Assert.That(pool2.OpeningTimes!.IsOpen24Hours,                        Is.True);
                Assert.That(changedT1!.EnergyMix,                                     Is.Null);

                Assert.That(cso2.Brands.Select(brand => brand.Id.ToString()),         Is.EqualTo(new[] { "blue-charge", "white-charge" }));

                // The levels are not mixed: the brand of the same name of its station is the station's own, unchanged.
                Assert.That(station.Brands.TryGet(Fast, out var stationFast),        Is.True);
                Assert.That(stationFast!.Homepage,                                    Is.Null);
                Assert.That(station.Brands.Count,                                     Is.EqualTo(1));

                // A brand of the pool removed is no brand of the pool, and still one of its station.
                var rn3     = Apply(rn2, RoamingNetworkChange.RemoveElement(nameof(ChargingPool), P1.ToString(), BrandPath("fast-charge")));
                var pool3   = rn3.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
                Assert.That(pool3.Brands.Select(brand => brand.Id.ToString()),        Is.EqualTo(new[] { "blue-charge", "green-charge" }));
                Assert.That(pool3.ChargingStations.Single().Brands.Single().Id,       Is.EqualTo(Fast));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(pool2, "openingTimes", null, new JObject(new JProperty("open", true)))),
                            Throws.ArgumentException.With.Message.Contains("opening times"));

            }

        }

        #endregion

        #region Concurrent_brand_changes_of_a_level_are_merged_brand_by_brand()

        /// <summary>
        /// Two change sets prepared against the same version, each adding another brand to the same charging pool,
        /// and one adding a brand to its charging station: merged brand by brand, the levels kept apart.
        /// </summary>
        [Test]
        public void Concurrent_brand_changes_of_a_level_are_merged_brand_by_brand()
        {

            var rn1       = NewNetwork();

            Brand Named(String Id)
                => new (Brand_Id.Parse(Id), new ImmutableI18NString(I18NString.Create(Languages.en, Id)));

            ImmutableArray<POIElementPathSegment> BrandPath(String BrandId)
                => [ new POIElementPathSegment("brands", BrandId) ];

            var blueOfPool     = rn1.CreateSignedChangeSet([ RoamingNetworkChange.AddElement(nameof(ChargingPool),    P1.ToString(), BrandPath("blue-charge"), Named("blue-charge").ToJSON()) ], CreatedAt: T0);
            var redOfPool      = rn1.CreateSignedChangeSet([ RoamingNetworkChange.AddElement(nameof(ChargingPool),    P1.ToString(), BrandPath("red-charge"),  Named("red-charge"). ToJSON()) ], CreatedAt: T0);
            var blueOfStation  = rn1.CreateSignedChangeSet([ RoamingNetworkChange.AddElement(nameof(ChargingStation), S1.ToString(), BrandPath("blue-charge"), Named("blue-charge").ToJSON()) ], CreatedAt: T0);

            var rn2       = rn1.ApplyChangeSet(blueOfPool);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.TryRebase(redOfPool,     out var rebasedRed,     out var redError),      Is.True, redError);
                var rn3 = rn2.ApplyChangeSet(rebasedRed!.SignedByAll(rn2));

                Assert.That(rn3.TryRebase(blueOfStation, out var rebasedStation, out var stationError),  Is.True, stationError);
                var rn4 = rn3.ApplyChangeSet(rebasedStation!.SignedByAll(rn3));

                var pool = rn4.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();

                Assert.That(pool.Brands.Select(brand => brand.Id.ToString()),                          Is.EqualTo(new[] { "blue-charge", "fast-charge", "green-charge", "red-charge" }));
                Assert.That(pool.ChargingStations.Single().Brands.Select(brand => brand.Id.ToString()), Is.EqualTo(new[] { "blue-charge", "fast-charge" }));

                // The same brand added twice to the same level does conflict.
                Assert.That(rn3.TryRebase(blueOfPool, out _, out var twiceError),  Is.False);
                Assert.That(twiceError,                                            Does.Contain("'brands['blue-charge']' already exists"));

            }

        }

        #endregion


    }

}
