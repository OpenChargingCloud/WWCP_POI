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

    }

}
