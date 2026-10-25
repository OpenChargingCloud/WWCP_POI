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

using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Charging tariff offers of charging station operators and e-mobility providers: another price for a time at the EVSEs
    /// of groups or of operators, not overlapping for one owner.
    /// </summary>
    [TestFixture]
    public class ChargingTariffOfferTests
    {

        #region Data

        internal static readonly DateTimeOffset              Created   = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        internal static readonly DateTimeOffset              T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        internal static readonly ChargingStationOperator_Id  GEF       = ChargingStationOperator_Id.Parse("DE*GEF");
        internal static readonly ChargingStationOperator_Id  ION       = ChargingStationOperator_Id.Parse("DE*ION");
        internal static readonly EMobilityProvider_Id        GDF       = EMobilityProvider_Id.      Parse("DE-GDF");
        internal static readonly ChargingPool_Id             P1        = ChargingPool_Id.           Parse("DE*GEF*P1");
        internal static readonly ChargingPool_Id             P2        = ChargingPool_Id.           Parse("DE*GEF*P2");
        internal static readonly ChargingStation_Id          S1        = ChargingStation_Id.        Parse("DE*GEF*S1");
        internal static readonly ChargingStation_Id          S2        = ChargingStation_Id.        Parse("DE*GEF*S2");
        internal static readonly EVSE_Id                     E1        = EVSE_Id.                   Parse("DE*GEF*E1*1");
        internal static readonly EVSE_Id                     E2        = EVSE_Id.                   Parse("DE*GEF*E1*2");
        internal static readonly EVSE_Id                     E3        = EVSE_Id.                   Parse("DE*GEF*E2*1");
        internal static readonly EVSE_Id                     I1        = EVSE_Id.                   Parse("DE*ION*E1*1");
        internal static readonly ChargingPoolGroup_Id        A9        = ChargingPoolGroup_Id.      Parse("DE*GEF*PGA9");
        internal static readonly ChargingStationGroup_Id     Markets   = ChargingStationGroup_Id.   Parse("DE*GEF*SGMARKETS");
        internal static readonly ChargingPoolGroup_Id        Partner   = ChargingPoolGroup_Id.      Parse("DE-GDF*PGPARTNER");
        internal static readonly ChargingTariffOffer_Id      Summer    = ChargingTariffOffer_Id.    Parse("gef-summer-2026");
        internal static readonly ChargingTariffOffer_Id      Night     = ChargingTariffOffer_Id.    Parse("gef-night");
        internal static readonly ChargingTariffOffer_Id      HPC39     = ChargingTariffOffer_Id.    Parse("gdf-hpc-39");

        internal static readonly POIPartyId                  GDFParty  = new (nameof(EMobilityProvider), GDF.ToString());

        #endregion

        #region Offers

        /// <summary>
        /// The first charge of a contract at the motorway pools 30 % cheaper, this summer, on the energy.
        /// </summary>
        internal static ChargingTariffOffer SummerOffer(DateTimeOffset? NotBefore = null, DateTimeOffset? NotAfter = null, ChargingTariffOfferSessions? Sessions = ChargingTariffOfferSessions.contract)

            => new (Summer,
                    I18NString.Create(Languages.de, "Sommeraktion A9"),
                    Discount:                new ChargingTariffDiscount(30, [ ChargingDimensionTypes.ENERGY ]),
                    ChargingPoolGroupIds:    [ A9 ],
                    NotBefore:               NotBefore ?? DateTimeOffset.Parse("2026-06-01T00:00:00Z"),
                    NotAfter:                NotAfter  ?? DateTimeOffset.Parse("2026-08-31T23:59:59.999Z"),
                    Sessions:                Sessions,
                    MaxSessionsPerContract:  1,
                    Created:                 Created);

        /// <summary>
        /// The opening times of every day from the given hour to the other one.
        /// </summary>
        internal static ImmutableOpeningTimes Daily(Byte From, Byte To)
        {

            var openingTimes = new OpeningTimes();

            foreach (var dayOfWeek in Enum.GetValues<DayOfWeek>())
                openingTimes.AddRegularOpening(dayOfWeek, new HourMin(From, 0), new HourMin(To, 0));

            return new ImmutableOpeningTimes(openingTimes);

        }

        /// <summary>
        /// 20 % less at the supermarket sites, at night from 18:00 to 06:00, for ad hoc sessions.
        /// </summary>
        internal static ChargingTariffOffer NightOffer(Byte From = 18, Byte To = 6)

            => new (Night,
                    Discount:                 new ChargingTariffDiscount(20),
                    ChargingStationGroupIds:  [ Markets ],
                    OpeningTimes:             Daily(From, To),
                    Sessions:                 ChargingTariffOfferSessions.adHoc,
                    Created:                  Created);

        /// <summary>
        /// 39 ct/kWh for the customers of the provider at all EVSEs of DE*ION and at its partner sites, from October.
        /// </summary>
        internal static ChargingTariffOffer HPCOffer(ChargingTariffOffer_Id? Id = null, IEnumerable<ChargingPoolGroup_Id>? PoolGroups = null)

            => new (Id ?? HPC39,
                    ChargingTariff:              new ChargingTariffOfferPrice(Currency.EUR, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ]),
                    ChargingStationOperatorIds:  [ ION ],
                    ChargingPoolGroupIds:        PoolGroups ?? [ Partner ],
                    NotBefore:                   DateTimeOffset.Parse("2026-10-01T00:00:00Z"),
                    Created:                     Created);

        /// <summary>
        /// 10 % less at all EVSEs of DE*GEF, for every session, within the given times.
        /// </summary>
        internal static ChargingTariffOffer GEFOffer(String                  Id,
                                                     DateTimeOffset?         NotBefore      = null,
                                                     DateTimeOffset?         NotAfter       = null,
                                                     ImmutableOpeningTimes?  OpeningTimes   = null)

            => new (ChargingTariffOffer_Id.Parse(Id),
                    Discount:                    new ChargingTariffDiscount(10),
                    ChargingStationOperatorIds:  [ GEF ],
                    NotBefore:                   NotBefore,
                    NotAfter:                    NotAfter,
                    OpeningTimes:                OpeningTimes,
                    Created:                     Created);

        /// <summary>
        /// The opening times of the given day from the given hour to the other one, and what else is given.
        /// </summary>
        internal static ImmutableOpeningTimes Weekly(DayOfWeek Day, Byte From, Byte To, Action<OpeningTimes>? More = null)
        {

            var openingTimes = new OpeningTimes().AddRegularOpening(Day, new HourMin(From, 0), new HourMin(To, 0));

            More?.Invoke(openingTimes);

            return new ImmutableOpeningTimes(openingTimes);

        }

        #endregion

        #region NewNetwork(OperatorOffers = null, ProviderOffers = null)

        /// <summary>
        /// DE*GEF with two pools, the first on the A9 with two EVSEs at a supermarket, the second with one; DE*ION with one
        /// EVSE; the provider DE-GDF with its partner sites, the first pool of DE*GEF. By default the summer and the night
        /// offer of DE*GEF, and the HPC offer of DE-GDF.
        /// </summary>
        internal static RoamingNetwork NewNetwork(IEnumerable<ChargingTariffOffer>? OperatorOffers = null,
                                                  IEnumerable<ChargingTariffOffer>? ProviderOffers = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                            Created:                Created,
                                                                            ChargingPools:          [ new ChargingPool(P1, Created: Created, ChargingStations: [ new ChargingStation(S1, Created: Created, EVSEs: [ new EVSE(E1, Created: Created), new EVSE(E2, Created: Created) ]) ]),
                                                                                                      new ChargingPool(P2, Created: Created, ChargingStations: [ new ChargingStation(S2, Created: Created, EVSEs: [ new EVSE(E3, Created: Created) ]) ]) ],
                                                                            ChargingPoolGroups:     [ new ChargingPoolGroup   (A9,      ChargingPoolIds:     [ P1 ], Created: Created) ],
                                                                            ChargingStationGroups:  [ new ChargingStationGroup(Markets, ChargingStationIds:  [ S1 ], Created: Created) ],
                                                                            ChargingTariffOffers:   OperatorOffers ?? [ SummerOffer(), NightOffer() ]),
                                                new ChargingStationOperator(ION,
                                                                            Created:                Created,
                                                                            ChargingPools:          [ new ChargingPool(ChargingPool_Id.Parse("DE*ION*P1"), Created: Created, ChargingStations: [ new ChargingStation(ChargingStation_Id.Parse("DE*ION*S1"), Created: Created, EVSEs: [ new EVSE(I1, Created: Created) ]) ]) ]) ],
                   EMobilityProviders:        [ new EMobilityProvider(GDF,
                                                                      Created:               Created,
                                                                      ChargingPoolGroups:    [ new ChargingPoolGroup(Partner, ChargingPoolIds: [ P1 ], Created: Created) ],
                                                                      ChargingTariffOffers:  ProviderOffers ?? [ HPCOffer() ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Offers_survive_JSON_and_CBOR()

        [Test]
        public void Offers_survive_JSON_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var summer  = json["chargingStationOperators"]![0]!["chargingTariffOffers"]!.First(offer => offer["@id"]!.Value<String>() == "gef-summer-2026");
            var hpc     = json["eMobilityProviders"]![0]!["chargingTariffOffers"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromJSON.CalcETags(),                                            Is.EqualTo(rn1.CalcETags()));

                Assert.That(summer["@id"]!.Value<String>(),                                  Is.EqualTo("gef-summer-2026"));
                Assert.That(summer["discount"]!["percent"]!.Value<Decimal>(),                Is.EqualTo(30m));
                Assert.That(summer["discount"]!["dimensions"]!.Values<String>(),             Is.EqualTo(new[] { "ENERGY" }));
                Assert.That(summer["chargingPoolGroupIds"]!.Values<String>(),                Is.EqualTo(new[] { "DE*GEF*PGA9" }));
                Assert.That(summer["sessions"]!.Value<String>(),                             Is.EqualTo("contract"));
                Assert.That(summer["maxSessionsPerContract"]!.Value<Int32>(),                Is.EqualTo(1));
                Assert.That(summer["notAfter"]!.Value<String>(),                             Is.EqualTo("2026-08-31T23:59:59.999Z"));

                Assert.That(hpc["chargingTariff"]!["currency"]!.Value<String>(),             Is.EqualTo("EUR"));
                Assert.That(hpc["chargingStationOperatorIds"]!.Values<String>(),             Is.EqualTo(new[] { "DE*ION" }));

                // Unique in the roaming network, found without their owner.
                Assert.That(fromJSON.ChargingTariffOffers.Select(offer => offer.Id.ToString()),  Is.EquivalentTo(new[] { "gef-summer-2026", "gef-night", "gdf-hpc-39" }));
                Assert.That(fromJSON.TryGetChargingTariffOfferById(ChargingTariffOffer_Id.Parse("GDF-HPC-39"), out var found),  Is.True);
                Assert.That(found!.ChargingTariff!.Elements.Single().PriceComponents.Single().Price,             Is.EqualTo(0.39m));

            }

        }

        #endregion

        #region Invalid_offers_are_refused()

        [Test]
        public void Invalid_offers_are_refused()
        {

            var price = new ChargingTariffOfferPrice(Currency.EUR, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ]);

            using (Assert.EnterMultipleScope())
            {

                // Exactly one of a price and a discount; at least one way to name EVSEs; its times in order; a session at least.
                Assert.That(() => new ChargingTariffOffer(Summer, ChargingStationOperatorIds: [ GEF ]),
                            Throws.ArgumentException.With.Message.Contains("exactly one of a price of its own and a discount"));
                Assert.That(() => new ChargingTariffOffer(Summer, ChargingTariff: price, Discount: new ChargingTariffDiscount(10), ChargingStationOperatorIds: [ GEF ]),
                            Throws.ArgumentException.With.Message.Contains("exactly one of a price of its own and a discount"));
                Assert.That(() => new ChargingTariffOffer(Summer, Discount: new ChargingTariffDiscount(10)),
                            Throws.ArgumentException.With.Message.Contains("must name at least one"));
                Assert.That(() => new ChargingTariffOffer(Summer, Discount: new ChargingTariffDiscount(10), ChargingStationOperatorIds: [ GEF ], NotBefore: T0, NotAfter: T0.AddDays(-1)),
                            Throws.ArgumentException.With.Message.Contains("must not be before its notBefore"));
                Assert.That(() => new ChargingTariffOffer(Summer, Discount: new ChargingTariffDiscount(10), ChargingStationOperatorIds: [ GEF ], MaxSessionsPerContract: 0),
                            Throws.ArgumentException.With.Message.Contains("at least 1"));

                // A discount above 0 and at most 100 %.
                Assert.That(() => new ChargingTariffDiscount(0),    Throws.ArgumentException);
                Assert.That(() => new ChargingTariffDiscount(100),  Throws.Nothing);
                Assert.That(() => new ChargingTariffDiscount(101),  Throws.ArgumentException);

                // A price has elements, and no identification of its own.
                var json = HPCOffer().ToJSON();
                json["chargingTariff"]!["@id"] = "DE*GDF*T1";
                Assert.That(ChargingTariffOffer.TryParse(json, out _, out var priceError),                      Is.False);
                Assert.That(priceError,                                                                         Does.Contain("must not have a '@id'"));
                json["chargingTariff"] = new JObject(new JProperty("currency", "EUR"));
                Assert.That(ChargingTariffOffer.TryParse(json, out _, out var elementsError),                   Is.False);
                Assert.That(elementsError,                                                                      Does.Contain("at least one element"));

                // An operator's offer names groups of its own and no operator but itself.
                Assert.That(() => NewNetwork(OperatorOffers: [ new ChargingTariffOffer(Summer, Discount: new ChargingTariffDiscount(10), ChargingStationOperatorIds: [ ION ]) ]),
                            Throws.ArgumentException.With.Message.Contains("must not name the operator 'DE*ION'"));
                Assert.That(() => NewNetwork(OperatorOffers: [ new ChargingTariffOffer(Summer, Discount: new ChargingTariffDiscount(10), ChargingPoolGroupIds: [ Partner ]) ]),
                            Throws.ArgumentException.With.Message.Contains("The charging pool group 'DE*GDF*PGPARTNER' of the charging tariff offer 'gef-summer-2026' is unknown at the operator 'DE*GEF'"));

                // A provider's offer is for sessions with its own contracts.
                Assert.That(() => NewNetwork(ProviderOffers: [ new ChargingTariffOffer(HPC39, ChargingTariff: price, ChargingStationOperatorIds: [ ION ], Sessions: ChargingTariffOfferSessions.adHoc) ]),
                            Throws.ArgumentException.With.Message.Contains("must name neither sessions nor e-mobility providers"));

                // Unique in the roaming network.
                Assert.That(() => NewNetwork(ProviderOffers: [ HPCOffer(Summer) ]),
                            Throws.ArgumentException.With.Message.Contains("The charging tariff offer 'gef-summer-2026' must not be given twice in the roaming network"));

            }

        }

        #endregion

        #region Offers_cover_the_EVSEs_of_their_groups_and_operators()

        [Test]
        public void Offers_cover_the_EVSEs_of_their_groups_and_operators()
        {

            var rn1  = NewNetwork();
            var gef  = rn1.GetChargingStationOperatorById(GEF)!;

            using (Assert.EnterMultipleScope())
            {

                // The pools of a pool group, the stations of a station group, all EVSEs of the operators named.
                Assert.That(gef.CoveredEVSEs(SummerOffer()),                                 Is.EquivalentTo(new[] { E1, E2 }));
                Assert.That(gef.CoveredEVSEs(NightOffer()),                                  Is.EquivalentTo(new[] { E1, E2 }));
                Assert.That(rn1.OffersCovering(E3),                                          Is.Empty);

                // An EVSE: the offers of its operator, then of providers.
                Assert.That(rn1.OffersCovering(E1).Select(offer => offer.Id),                Is.EqualTo(new[] { Night, Summer, HPC39 }));
                Assert.That(rn1.OffersCovering(I1).Select(offer => offer.Id),                Is.EqualTo(new[] { HPC39 }));

            }

        }

        #endregion

        #region Offers_of_an_operator_must_not_overlap()

        [Test]
        public void Offers_of_an_operator_must_not_overlap()
        {

            // The summer offer and an ad hoc discount at the same pools at the same time do not overlap: other sessions.
            var adHoc         = new ChargingTariffOffer(ChargingTariffOffer_Id.Parse("gef-adhoc"),
                                                        Discount:              new ChargingTariffDiscount(10),
                                                        ChargingPoolGroupIds:  [ A9 ],
                                                        Sessions:              ChargingTariffOfferSessions.adHoc,
                                                        Created:               Created);

            // Of every session, it does.
            var everySession  = new ChargingTariffOffer(ChargingTariffOffer_Id.Parse("gef-every"),
                                                        Discount:              new ChargingTariffDiscount(10),
                                                        ChargingPoolGroupIds:  [ A9 ],
                                                        Created:               Created);

            // For contracts of one provider only, against an offer for contracts of another one, neither.
            ChargingTariffOffer ForProvider(String Id, String ProviderId)
                => new (ChargingTariffOffer_Id.Parse(Id),
                        Discount:              new ChargingTariffDiscount(10),
                        ChargingPoolGroupIds:  [ A9 ],
                        Sessions:              ChargingTariffOfferSessions.contract,
                        EMobilityProviderIds:  [ EMobilityProvider_Id.Parse(ProviderId) ],
                        Created:               Created);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => NewNetwork(OperatorOffers: [ SummerOffer(), adHoc ]),                              Throws.Nothing);
                Assert.That(() => NewNetwork(OperatorOffers: [ SummerOffer(), everySession ]),
                            Throws.ArgumentException.With.Message.Contains("The charging tariff offers 'gef-every' and 'gef-summer-2026' of the operator 'DE*GEF' overlap at the EVSE 'DE*GEF*E1*1'"));
                Assert.That(() => NewNetwork(OperatorOffers: [ ForProvider("gef-a", "DE-AAA"), ForProvider("gef-b", "DE-BBB") ]),  Throws.Nothing);
                Assert.That(() => NewNetwork(OperatorOffers: [ ForProvider("gef-a", "DE-AAA"), SummerOffer() ]),     Throws.ArgumentException);

                // Times: the summer before or after another summer, the nights against the days.
                Assert.That(() => NewNetwork(OperatorOffers: [ SummerOffer(), new ChargingTariffOffer(ChargingTariffOffer_Id.Parse("gef-autumn"),
                                                                                                      Discount:              new ChargingTariffDiscount(10),
                                                                                                      ChargingPoolGroupIds:  [ A9 ],
                                                                                                      NotBefore:             DateTimeOffset.Parse("2026-09-01T00:00:00Z"),
                                                                                                      Created:               Created) ]),
                            Throws.Nothing);
                Assert.That(() => NewNetwork(OperatorOffers: [ NightOffer(), new ChargingTariffOffer(ChargingTariffOffer_Id.Parse("gef-day"),
                                                                                                     Discount:                 new ChargingTariffDiscount(10),
                                                                                                     ChargingStationGroupIds:  [ Markets ],
                                                                                                     OpeningTimes:             Daily(8, 16),
                                                                                                     Created:                  Created) ]),
                            Throws.Nothing);
                Assert.That(() => NewNetwork(OperatorOffers: [ NightOffer(), new ChargingTariffOffer(ChargingTariffOffer_Id.Parse("gef-evening"),
                                                                                                     Discount:                 new ChargingTariffDiscount(10),
                                                                                                     ChargingStationGroupIds:  [ Markets ],
                                                                                                     OpeningTimes:             Daily(16, 20),
                                                                                                     Created:                  Created) ]),
                            Throws.ArgumentException.With.Message.Contains("overlap at the EVSE"));

                // A change set making them overlap is refused as well: the summer offer for ad hoc sessions, as the night offer.
                var rn1 = NewNetwork();
                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(rn1.GetChargingStationOperatorById(GEF)!.ChargingTariffOffers.First(offer => offer.Id == Summer), "sessions", "contract", "adHoc")),
                            Throws.ArgumentException.With.Message.Contains("The charging tariff offers 'gef-night' and 'gef-summer-2026' of the operator 'DE*GEF' overlap"));
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(everySession, GEF)),
                            Throws.ArgumentException.With.Message.Contains("overlap at the EVSE"));

            }

        }

        #endregion

        #region A_change_set_of_a_provider_must_not_make_its_offers_overlap()

        [Test]
        public void A_change_set_of_a_provider_must_not_make_its_offers_overlap()
        {

            var rn1     = NewNetwork();
            var single  = EVSEGroup_Id.Parse("DE-GDF*EGSINGLE");

            ChargingTariffOffer Offer(String Id, IEnumerable<ChargingPoolGroup_Id>? PoolGroups = null, IEnumerable<EVSEGroup_Id>? EVSEGroups = null)
                => new (ChargingTariffOffer_Id.Parse(Id),
                        ChargingTariff:        new ChargingTariffOfferPrice(Currency.EUR, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.35m, StepSize: 1) ]) ]),
                        ChargingPoolGroupIds:  PoolGroups,
                        EVSEGroupIds:          EVSEGroups,
                        Created:               Created);

            // Another offer of the provider at its partner sites: they overlap there.
            Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(Offer("gdf-partner", PoolGroups: [ Partner ]), GDF)),
                        Throws.ArgumentException.With.Message.Contains("The charging tariff offers 'gdf-hpc-39' and 'gdf-partner' of the e-mobility provider 'DE-GDF' must not overlap: they cover the EVSE 'DE*GEF*E1*1'"));

            // At the EVSE of the second pool of DE*GEF, they do not...
            var rn2     = Apply(rn1,
                                RoamingNetworkChange.Add(new EVSEGroup(single, EVSEIds: [ E3 ]), GDF),
                                RoamingNetworkChange.Add(Offer("gdf-single", EVSEGroups: [ single ]), GDF));

            // ...but they may come to overlap by the changes of others: the operator moves the station to its partner site.
            var rn3     = Apply(rn2, RoamingNetworkChange.Remove(nameof(ChargingStation), S2.ToString()),
                                     RoamingNetworkChange.Add   (nameof(ChargingStation), S2.ToString(), new ChargingStation(S2, Created: Created, EVSEs: [ new EVSE(E3, Created: Created) ]).ToJSON(), nameof(ChargingPool), P1.ToString()));

            // A change set of the provider leaving them as they are is not refused for it.
            rn3.TryGetEMobilityProviderById(GDF, out var gdf3);
            var rn4     = Apply(rn3, RoamingNetworkChange.UpdateProperty(gdf3!, "homepage", null, "https://gdf.example"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn2.OffersCovering(E3).Select(offer => offer.Id.ToString()),                                 Is.EqualTo(new[] { "gdf-single" }));
                Assert.That(rn3.OffersCovering(E3).Select(offer => offer.Id.ToString()),                                 Is.EqualTo(new[] { "gef-summer-2026", "gdf-hpc-39", "gdf-single" }));
                Assert.That(rn4.TryGetEMobilityProviderById(GDF, out var gdf4) && gdf4.Homepage.HasValue,               Is.True);
            }

        }

        #endregion

        #region Offers_overlap_only_at_the_same_times()

        /// <summary>
        /// Two offers overlap only at times both apply in: within their common notBefore and notAfter, by their regular
        /// openings in the time zone of the EVSE, and by their exceptional openings and closings, periods of absolute time.
        /// </summary>
        [Test]
        public void Offers_overlap_only_at_the_same_times()
        {

            var monday          = DateTimeOffset.Parse("2026-10-19T00:00:00Z");
            var berlin          = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

            var mondayTuesday   = GEFOffer("mon-tue",     NotBefore: monday, NotAfter: monday.AddDays(2).AddTicks(-1));
            var mondayToSunday  = GEFOffer("mon-sun",     NotBefore: monday, NotAfter: monday.AddDays(7).AddTicks(-1));
            var saturdays       = GEFOffer("saturdays",   OpeningTimes: Weekly(DayOfWeek.Saturday, 8, 20));
            var closed          = GEFOffer("closed",      OpeningTimes: new ImmutableOpeningTimes(new OpeningTimes().AddExceptionalClosing(monday.AddDays(-1), monday.AddDays(3))));
            var closedHalf      = GEFOffer("closed-half", OpeningTimes: new ImmutableOpeningTimes(new OpeningTimes().AddExceptionalClosing(monday.AddDays(-1), monday.AddDays(1).AddHours(12))));
            var mondays         = GEFOffer("mondays",     OpeningTimes: Weekly(DayOfWeek.Monday,  8, 10));
            var tuesdays        = GEFOffer("tuesdays",    OpeningTimes: Weekly(DayOfWeek.Tuesday, 8, 10));
            var andTuesday      = GEFOffer("and-tuesday", OpeningTimes: Weekly(DayOfWeek.Monday,  8, 10, openingTimes => openingTimes.AddExceptionalOpening(monday.AddDays(1).AddHours(9), monday.AddDays(1).AddHours(9.5))));

            // On Wednesday from 8 to 9 local time, this week; and on that Wednesday from 6 to 7 UTC, 8 to 9 in Berlin.
            var wednesday       = GEFOffer("wednesday",     NotBefore: monday, NotAfter: monday.AddDays(7).AddTicks(-1), OpeningTimes: Weekly(DayOfWeek.Wednesday, 8, 9));
            var exceptionally   = GEFOffer("exceptionally", OpeningTimes: Weekly(DayOfWeek.Monday, 8, 9, openingTimes => openingTimes.AddExceptionalOpening(monday.AddDays(2).AddHours(6), monday.AddDays(2).AddHours(7))));

            using (Assert.EnterMultipleScope())
            {

                // Two days have no Saturday, a week has.
                Assert.That(ChargingTariffOffer.TimesOverlap(mondayTuesday,  saturdays),          Is.False);
                Assert.That(ChargingTariffOffer.TimesOverlap(mondayToSunday, saturdays),          Is.True);

                // Closed during all of the other's times, or only during a part of them.
                Assert.That(ChargingTariffOffer.TimesOverlap(mondayTuesday,  closed),             Is.False);
                Assert.That(ChargingTariffOffer.TimesOverlap(mondayTuesday,  closedHalf),         Is.True);

                // Mondays and Tuesdays do not overlap, an exceptional opening on a Tuesday does.
                Assert.That(ChargingTariffOffer.TimesOverlap(mondays,        tuesdays),           Is.False);
                Assert.That(ChargingTariffOffer.TimesOverlap(andTuesday,     tuesdays),           Is.True);
                Assert.That(ChargingTariffOffer.TimesOverlap(tuesdays,       andTuesday),         Is.True);

                // Regular openings are local times, exceptional periods absolute ones.
                Assert.That(ChargingTariffOffer.TimesOverlap(wednesday,      exceptionally),          Is.False);
                Assert.That(ChargingTariffOffer.TimesOverlap(wednesday,      exceptionally, berlin),  Is.True);

                Assert.That(wednesday.IsOpenAt(monday.AddDays(2).AddHours(6.5)),          Is.False);
                Assert.That(wednesday.IsOpenAt(monday.AddDays(2).AddHours(6.5), berlin),  Is.True);
                Assert.That(closed.   IsOpenAt(monday.AddDays(2)),                        Is.False);
                Assert.That(closed.   IsOpenAt(monday.AddDays(3)),                        Is.True);

            }

        }

        #endregion

        #region Offers_of_an_operator_overlap_in_the_time_zone_of_the_EVSE()

        /// <summary>
        /// Two offers of an operator at the same EVSEs, at times that are the same only in Berlin: the time zone of the
        /// charging pool of an EVSE tells whether they overlap there, UTC without one. A change set giving the pool the
        /// time zone of Berlin is refused, one giving it that of New York is not.
        /// </summary>
        [Test]
        public void Offers_of_an_operator_overlap_in_the_time_zone_of_the_EVSE()
        {

            var monday         = DateTimeOffset.Parse("2026-10-19T00:00:00Z");
            var wednesday      = GEFOffer("wednesday",     NotBefore: monday, NotAfter: monday.AddDays(7).AddTicks(-1), OpeningTimes: Weekly(DayOfWeek.Wednesday, 8, 9));
            var exceptionally  = GEFOffer("exceptionally", OpeningTimes: Weekly(DayOfWeek.Monday, 8, 9, openingTimes => openingTimes.AddExceptionalOpening(monday.AddDays(2).AddHours(6), monday.AddDays(2).AddHours(7))));

            var rn1            = NewNetwork(OperatorOffers: [ wednesday, exceptionally ]);
            var gef1           = rn1.GetChargingStationOperatorById(GEF)!;

            gef1.TryGetChargingPool(P1, out var p1);

            var rn2            = Apply(rn1, RoamingNetworkChange.UpdateProperty(p1!, "timeZone", null, "America/New_York"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(gef1.TimeZoneOf(E1),                                                        Is.EqualTo(TimeZoneInfo.Utc));
                Assert.That(rn2.GetChargingStationOperatorById(GEF)!.TimeZoneOf(E1).Id,                 Does.Contain("New_York").Or.Contain("Eastern"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(p1!, "timeZone", null, "Europe/Berlin")),
                            Throws.ArgumentException.With.Message.Contains("The charging tariff offers 'exceptionally' and 'wednesday' of the operator 'DE*GEF' overlap at the EVSE 'DE*GEF*E1*1'"));

            }

        }

        #endregion

        #region The_applicable_offers_of_an_EVSE()

        /// <summary>
        /// At an EVSE the offers apply that cover it and are active and open then: of its operator all of them, of a
        /// provider whose offers came to overlap by the changes of others the one with the later notBefore.
        /// </summary>
        [Test]
        public void The_applicable_offers_of_an_EVSE()
        {

            var evening  = DateTimeOffset.Parse("2026-10-12T20:00:00Z");
            var noon     = DateTimeOffset.Parse("2026-10-12T12:00:00Z");
            var single   = EVSEGroup_Id.Parse("DE-GDF*EGSINGLE");

            // A second offer of the provider, from September, at the EVSE of the second pool of DE*GEF...
            var rn1      = NewNetwork();
            var rn2      = Apply(rn1,
                                 RoamingNetworkChange.Add(new EVSEGroup(single, EVSEIds: [ E3 ]), GDF),
                                 RoamingNetworkChange.Add(new ChargingTariffOffer(ChargingTariffOffer_Id.Parse("gdf-single"),
                                                                                  ChargingTariff:  new ChargingTariffOfferPrice(Currency.EUR, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.35m, StepSize: 1) ]) ]),
                                                                                  EVSEGroupIds:    [ single ],
                                                                                  NotBefore:       DateTimeOffset.Parse("2026-09-01T00:00:00Z"),
                                                                                  Created:         Created), GDF));

            // ...which the operator moves to the partner site of the provider: both offers of the provider cover it.
            var rn3      = Apply(rn2, RoamingNetworkChange.Remove(nameof(ChargingStation), S2.ToString()),
                                      RoamingNetworkChange.Add   (nameof(ChargingStation), S2.ToString(), new ChargingStation(S2, Created: Created, EVSEs: [ new EVSE(E3, Created: Created) ]).ToJSON(), nameof(ChargingPool), P1.ToString()));

            static IEnumerable<String> IdsOf(IEnumerable<ChargingTariffOffer> Offers)
                => Offers.Select(offer => offer.Id.ToString());

            using (Assert.EnterMultipleScope())
            {

                // The night offer of the operator in the evening, the HPC offer of the provider all day; the summer is over.
                Assert.That(IdsOf(rn1.ApplicableOffers(E1, evening)),                                       Is.EqualTo(new[] { "gef-night", "gdf-hpc-39" }));
                Assert.That(IdsOf(rn1.ApplicableOffers(E1, noon)),                                          Is.EqualTo(new[] { "gdf-hpc-39" }));
                Assert.That(IdsOf(rn1.ApplicableOffers(E3, evening)),                                       Is.Empty);

                // Of the two offers of the provider the one with the later notBefore, before it the other one.
                Assert.That(IdsOf(rn3.OffersCovering  (E3)),                                                Is.EqualTo(new[] { "gef-summer-2026", "gdf-hpc-39", "gdf-single" }));
                Assert.That(IdsOf(rn3.ApplicableOffers(E3, evening)),                                       Is.EqualTo(new[] { "gdf-hpc-39" }));
                Assert.That(IdsOf(rn3.ApplicableOffers(E3, DateTimeOffset.Parse("2026-09-15T12:00:00Z"))),  Is.EqualTo(new[] { "gdf-single" }));

            }

        }

        #endregion

        #region The_status_of_an_offer_is_told_when_it_begins_and_when_it_ends()

        /// <summary>
        /// Its status follows its times: the runtime status tells of it when it becomes active at its notBefore and when it
        /// expires after its notAfter, though no status was set.
        /// </summary>
        [Test]
        public async Task The_status_of_an_offer_is_told_when_it_begins_and_when_it_ends()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var told     = new List<POIStatusUpdate>();
            var expired  = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            history.RuntimeStatus.OnStatusUpdate += statusUpdate => {

                if (statusUpdate.EntityType == nameof(ChargingTariffOffer) && statusUpdate.EntityId == "gef-flash")
                {

                    lock (told)
                        told.Add(statusUpdate);

                    if (statusUpdate.Value == "expired")
                        expired.TrySetResult();

                }

            };

            var now      = Timestamp.Now;
            var begin    = DateTimeOffset.FromUnixTimeMilliseconds(now.ToUnixTimeMilliseconds() + 1000);
            var end      = begin.AddMilliseconds(500);

            Assert.That(history.Commit(history.Head.RoamingNetwork.CreateSignedChangeSet([
                            RoamingNetworkChange.Add(new ChargingTariffOffer(ChargingTariffOffer_Id.Parse("gef-flash"),
                                                                             Discount:                    new ChargingTariffDiscount(10),
                                                                             ChargingStationOperatorIds:  [ GEF ],
                                                                             NotBefore:                   begin,
                                                                             NotAfter:                    end,
                                                                             Sessions:                    ChargingTariffOfferSessions.contract,
                                                                             Created:                     Created), GEF)
                        ], CreatedAt: now)).IsCommitted);

            await expired.Task.WaitAsync(TimeSpan.FromSeconds(10));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(told.Select(statusUpdate => statusUpdate.Value),     Is.EqualTo(new[] { "active", "expired" }));
                Assert.That(told[0].Timestamp,                                   Is.EqualTo(begin));
                Assert.That(told[0].OldValue,                                    Is.EqualTo("planned"));
                Assert.That(told[1].Timestamp,                                   Is.EqualTo(end));
                Assert.That(Timestamp.Now,                                       Is.GreaterThan(end));
            }

        }

        #endregion

        #region The_status_of_an_offer_follows_its_times_unless_exhausted()

        [Test]
        public void The_status_of_an_offer_follows_its_times_unless_exhausted()
        {

            var summer   = SummerOffer();
            var history  = RoamingNetworkHistory.Start(NewNetwork());

            using (Assert.EnterMultipleScope())
            {

                Assert.That(summer.StatusAt(DateTimeOffset.Parse("2026-05-31T23:59:59Z")),      Is.EqualTo(ChargingTariffOfferStatusType.Planned));
                Assert.That(summer.StatusAt(DateTimeOffset.Parse("2026-06-01T00:00:00Z")),      Is.EqualTo(ChargingTariffOfferStatusType.Active));
                Assert.That(summer.StatusAt(DateTimeOffset.Parse("2026-08-31T23:59:59.999Z")),  Is.EqualTo(ChargingTariffOfferStatusType.Active));
                Assert.That(summer.StatusAt(DateTimeOffset.Parse("2026-09-01T00:00:00Z")),      Is.EqualTo(ChargingTariffOfferStatusType.Expired));

                // Now: the summer is over, the night offer active; the owner sets the night offer exhausted.
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(ChargingTariffOffer), Summer.ToString(), out var summerStatus),  Is.True);
                Assert.That(summerStatus!.Value,                                                                                                            Is.EqualTo("expired"));

                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(ChargingTariffOffer), Night.ToString(), "exhausted", T0, out _, out var error),
                            Is.EqualTo(POIStatusUpdateResult.Updated), error);
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(ChargingTariffOffer), Night.ToString(), out var nightStatus),   Is.True);
                Assert.That(nightStatus!.Value,                                                                                                             Is.EqualTo("exhausted"));

                history.Head.RoamingNetwork.TryGetChargingTariffOfferById(Night, out var night);
                Assert.That(night!.IsActive,                                                                                                                Is.False);

            }

        }

        #endregion

        #region Offers_are_changed_by_change_sets_of_their_owners()

        [Test]
        public void Offers_are_changed_by_change_sets_of_their_owners()
        {

            var rn1     = NewNetwork();
            rn1.TryGetChargingTariffOfferById(Summer, out var summer);
            rn1.TryGetChargingTariffOfferById(HPC39,  out var hpc);

            var rn2     = Apply(rn1,
                                RoamingNetworkChange.UpdateProperty(summer!, "notAfter", null, "2026-09-30T23:59:59.999Z"),
                                RoamingNetworkChange.UpdateProperty(hpc!,    "chargingStationOperatorIds", null, new JArray("DE*GEF", "DE*ION")));

            rn2.TryGetChargingTariffOfferById(Summer, out var changedSummer);
            rn2.TryGetChargingTariffOfferById(HPC39,  out var changedHPC);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(changedSummer!.NotAfter,                                          Is.EqualTo(DateTimeOffset.Parse("2026-09-30T23:59:59.999Z")));
                Assert.That(changedHPC!.ChargingStationOperatorIds,                           Is.EqualTo(new[] { GEF, ION }));
                Assert.That(rn2.OffersCovering(E3).Select(offer => offer.Id),                 Is.EqualTo(new[] { HPC39 }));

                // What embeds a changed offer changed with it (SPECIFICATION.md section 4): its operator, its provider.
                Assert.That(changedSummer.LastChangeDate,                                     Is.EqualTo(T0));
                Assert.That(rn2.GetChargingStationOperatorById(GEF)!.LastChangeDate,          Is.EqualTo(T0));
                Assert.That(rn2.TryGetEMobilityProviderById(GDF, out var gdf2) ? gdf2.LastChangeDate : default(DateTimeOffset?),  Is.EqualTo(T0));
                Assert.That(rn2.GetChargingStationOperatorById(ION)!.LastChangeDate,          Is.EqualTo(Created), "No offer of it changed.");

                // A group an operator's offer names can not be removed; a provider's offer naming it does not keep it.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(A9)),
                            Throws.ArgumentException.With.Message.Contains("The charging pool group 'DE*GEF*PGA9' of the charging tariff offer 'gef-summer-2026' is unknown"));

                // Switching from a discount to a price replaces the offer.
                var priced = new ChargingTariffOffer(Summer,
                                                     ChargingTariff:          new ChargingTariffOfferPrice(Currency.EUR, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.29m, StepSize: 1) ]) ]),
                                                     ChargingPoolGroupIds:    [ A9 ],
                                                     Sessions:                ChargingTariffOfferSessions.contract,
                                                     Created:                 Created);
                var rn3 = Apply(rn2, RoamingNetworkChange.Remove(Summer), RoamingNetworkChange.Add(priced, GEF));
                rn3.TryGetChargingTariffOfferById(Summer, out var pricedSummer);
                Assert.That(pricedSummer!.Discount,                                           Is.Null);
                Assert.That(pricedSummer.ChargingTariff!.Currency,                            Is.EqualTo(Currency.EUR));

                // Only the owner signs a change of its offers.
                var operatorChange  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(summer!, "maxSessionsPerContract", 1, 2) ], CreatedAt: T0);
                var providerChange  = rn1.CreateChangeSet([ RoamingNetworkChange.RemoveProperty(hpc!,    "notBefore") ],                   CreatedAt: T0);

                Assert.That(rn1.TryCheckOwnership(operatorChange.SignedBy(GDFParty),                  out var operatorError),  Is.False);
                Assert.That(operatorError,                                                                                      Does.Contain("needs a signature by"));
                Assert.That(rn1.TryCheckOwnership(operatorChange.SignedBy(TestParties.Party(GEF)),    out var operatorOK),     Is.True, operatorOK);
                Assert.That(rn1.TryCheckOwnership(providerChange.SignedBy(TestParties.Party(ION)),    out _),                  Is.False);
                Assert.That(rn1.TryCheckOwnership(providerChange.SignedBy(GDFParty),                  out var providerOK),     Is.True, providerOK);

            }

        }

        #endregion

        #region Changes_of_offers_on_two_branches_merge()

        [Test]
        public void Changes_of_offers_on_two_branches_merge()
        {

            var rn1     = NewNetwork();
            rn1.TryGetChargingTariffOfferById(Summer, out var summer);
            rn1.TryGetChargingTariffOfferById(HPC39,  out var hpc);

            var left    = Apply(rn1, RoamingNetworkChange.UpdateProperty(summer!, "maxSessionsPerContract", null, 2));
            var right   = Apply(rn1, RoamingNetworkChange.UpdateProperty(hpc!,    "name", null, I18NString.Create(Languages.en, "HPC for 39 ct").ToJSON()));

            var merged  = RoamingNetwork.TryMerge(rn1, left, right, ChangeSet_Id.Parse("merge-1"), out var merge, out _, out var conflicts);

            merge?.TryGetChargingTariffOfferById(Summer, out summer);
            merge?.TryGetChargingTariffOfferById(HPC39,  out hpc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(merged,                                                          Is.True, String.Join(" ", conflicts.Select(conflict => conflict.Description)));
                Assert.That(summer!.MaxSessionsPerContract,                                  Is.EqualTo(2));
                Assert.That(hpc!.Name.FirstText(),                                           Is.EqualTo("HPC for 39 ct"));
            }

        }

        #endregion

    }

    /// <summary>
    /// Charging tariff offers over HTTP.
    /// </summary>
    [TestFixture]
    public class ChargingTariffOfferAPITests
    {

        #region Data

        private const String GEF   = "RNs/rn1/CSOs/DE*GEF";
        private const String GDF   = "RNs/rn1/EMPs/DE-GDF";
        private const String CP1   = GEF + "/CPs/DE*GEF*P1";
        private const String EVSE  = CP1 + "/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*1";

        private RoamingNetworkHistory?  history;
        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private HttpClient?             httpClient;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            history     = RoamingNetworkHistory.Start(ChargingTariffOfferTests.NewNetwork());

            httpServer  = new HTTPServer(
                              IPAddress:  IPv4Address.Localhost,
                              TCPPort:    IPPort.Zero,
                              AutoStart:  true
                          );

            poiAPI      = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(history);

            httpClient  = new HttpClient {
                              BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/")
                          };

        }

        [TearDown]
        public async Task TearDown()
        {

            httpClient?.Dispose();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

        }

        #endregion

        #region Get(Path)

        private async Task<(HttpResponseMessage Response, JToken? JSON)> Get(String Path)
        {

            var response  = await httpClient!.GetAsync(Path);
            var content   = await response.Content.ReadAsStringAsync();

            return (response,
                    response.IsSuccessStatusCode
                        ? JToken.ReadFrom(new JsonTextReader(new StringReader(content)) { DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Decimal })
                        : null);

        }

        private static IEnumerable<String?> IdsOf(JToken? List)

            => List!.Select(entry => entry["@id"]!.Value<String>());

        #endregion


        #region Offers_are_served_by_their_owners_and_below_the_EVSEs_they_cover()

        [Test]
        public async Task Offers_are_served_by_their_owners_and_below_the_EVSEs_they_cover()
        {

            var (_,       operatorOffers)  = await Get(GEF  + "/CTOs");
            var (summer,  summerJSON)      = await Get(GEF  + "/CTOs/gef-summer-2026");
            var (_,       providerOffers)  = await Get(GDF  + "/CTOs");
            var (_,       without)         = await Get(GDF  + "?without=chargingTariffOffers");
            var (_,       ofEVSE)          = await Get(EVSE + "/CTOs");
            var (_,       active)          = await Get(EVSE + "/CTOs?status=active");
            var (_,       ofOperator)      = await Get(EVSE + "/CTOs?eMobilityProviderId=DE-XYZ");
            var (_,       ofPool)          = await Get(CP1  + "/CTOs");
            var (invalid, _)               = await Get(EVSE + "/CTOs?status=nonsense");
            var (other,   _)               = await Get(GDF  + "/CTOs/gef-summer-2026");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(IdsOf(operatorOffers),                                    Is.EqualTo(new[] { "gef-night", "gef-summer-2026" }));
                Assert.That(summer.StatusCode,                                        Is.EqualTo(HttpStatusCode.OK));
                Assert.That(summerJSON!["discount"]!["percent"]!.Value<Decimal>(),    Is.EqualTo(30m));
                Assert.That(IdsOf(providerOffers),                                    Is.EqualTo(new[] { "gdf-hpc-39" }));
                Assert.That(without!["chargingTariffOffers"],                         Is.Null);
                Assert.That(other.StatusCode,                                         Is.EqualTo(HttpStatusCode.NotFound));

                // Below an EVSE: of its operator, then of providers; the summer is over, the others are active now.
                Assert.That(IdsOf(ofEVSE),                                            Is.EqualTo(new[] { "gef-night", "gef-summer-2026", "gdf-hpc-39" }));
                Assert.That(IdsOf(active),                                            Is.EqualTo(new[] { "gef-night", "gdf-hpc-39" }));
                Assert.That(IdsOf(ofOperator),                                        Is.EqualTo(new[] { "gef-night", "gef-summer-2026" }));
                Assert.That(IdsOf(ofPool),                                            Is.EqualTo(new[] { "gef-night", "gef-summer-2026", "gdf-hpc-39" }));
                Assert.That(invalid.StatusCode,                                       Is.EqualTo(HttpStatusCode.BadRequest));

            }

        }

        #endregion

        #region Offers_are_found_by_their_identification_in_the_roaming_network()

        /// <summary>
        /// Below an EVSE the offers of its operator and of providers: their identifications name no owner, the roaming
        /// network redirects to where each is.
        /// </summary>
        [Test]
        public async Task Offers_are_found_by_their_identification_in_the_roaming_network()
        {

            var (ofOperator,  night)  = await Get("RNs/rn1/CTOs/gef-night");
            var (ofProvider,  hpc)    = await Get("RNs/rn1/CTOs/GDF-HPC-39");
            var (unknown,     _)      = await Get("RNs/rn1/CTOs/nothing-like-it");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Uri.UnescapeDataString(ofOperator.RequestMessage!.RequestUri!.AbsolutePath),  Is.EqualTo("/RNs/rn1/CSOs/DE*GEF/CTOs/gef-night"));
                Assert.That(night!["@id"]!.Value<String>(),                                                Is.EqualTo("gef-night"));
                Assert.That(Uri.UnescapeDataString(ofProvider.RequestMessage!.RequestUri!.AbsolutePath),  Is.EqualTo("/RNs/rn1/EMPs/DE-GDF/CTOs/gdf-hpc-39"));
                Assert.That(hpc!["@id"]!.Value<String>(),                                                  Is.EqualTo("gdf-hpc-39"));
                Assert.That(unknown.StatusCode,                                                            Is.EqualTo(HttpStatusCode.NotFound));
            }

        }

        #endregion

        #region The_applicable_offers_are_served_below_the_EVSEs_they_cover()

        [Test]
        public async Task The_applicable_offers_are_served_below_the_EVSEs_they_cover()
        {

            var (_,        evening)  = await Get(EVSE + "/CTOs?applicable=true&at=2026-10-12T20:00:00Z");
            var (_,        noon)     = await Get(EVSE + "/CTOs?applicable=true&at=2026-10-12T12:00:00Z");
            var (_,        ofGDF)    = await Get(EVSE + "/CTOs?applicable=true&at=2026-10-12T12:00:00Z&eMobilityProviderId=DE-XYZ");
            var (atAlone,  _)        = await Get(EVSE + "/CTOs?at=2026-10-12T20:00:00Z");
            var (notTrue,  _)        = await Get(EVSE + "/CTOs?applicable=false");
            var (noTime,   _)        = await Get(EVSE + "/CTOs?applicable=true&at=yesterday");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(IdsOf(evening),           Is.EqualTo(new[] { "gef-night", "gdf-hpc-39" }));
                Assert.That(IdsOf(noon),              Is.EqualTo(new[] { "gdf-hpc-39" }));
                Assert.That(IdsOf(ofGDF),             Is.Empty);
                Assert.That(atAlone.StatusCode,       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(notTrue.StatusCode,       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(noTime.StatusCode,        Is.EqualTo(HttpStatusCode.BadRequest));
            }

        }

        #endregion

    }

}
