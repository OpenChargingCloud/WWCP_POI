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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Templates of EVSEs, charging stations, charging pools, charging tariffs, energy meters and parking spaces: a member
    /// names them in its templateIds, in an order that is content, and takes their properties at runtime, the first
    /// template giving a value winning.
    /// </summary>
    [TestFixture]
    public class TemplateTests
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
        private static readonly ChargingTariff_Id           T2       = ChargingTariff_Id.         Parse("DE*GEF*T2");
        private static readonly EVSETemplate_Id             AC22     = EVSETemplate_Id.           Parse("DE*GEF*ETAC22");
        private static readonly EVSETemplate_Id             AC11     = EVSETemplate_Id.           Parse("DE*GEF*ETAC11");
        private static readonly ChargingStationTemplate_Id  Wallbox  = ChargingStationTemplate_Id.Parse("DE*GEF*STWALLBOX");
        private static readonly ChargingPoolTemplate_Id     Sites    = ChargingPoolTemplate_Id.   Parse("DE*GEF*PTSITES");
        private static readonly ChargingTariffTemplate_Id   Euro     = ChargingTariffTemplate_Id. Parse("DE*GEF*TTEURO");
        private static readonly ParkingOperator_Id          PAR      = ParkingOperator_Id.        Parse("DE*PAR");
        private static readonly ParkingSpaceTemplate_Id     Covered  = ParkingSpaceTemplate_Id.   Parse("covered");

        #endregion

        #region NewNetwork()

        /// <summary>
        /// Two EVSEs, the first naming the 11 kW template before the 22 kW one, the second only the 22 kW one; a station,
        /// a pool and a tariff taking their templates, the tariff all its prices; a parking space taking a template.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:                   Created,
                                                                             ChargingTariffTemplates:   [ new ChargingTariffTemplate(Euro,
                                                                                                                                     Currency:  Currency.EUR,
                                                                                                                                     Elements:  [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                                     Created:   Created) ],
                                                                             ChargingTariffs:           [ new ChargingTariff(T1, Currency.EUR, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.49m, StepSize: 1) ]) ], Created: Created),
                                                                                                          new ChargingTariff(T2, null, null, TemplateIds: [ Euro ], Created: Created) ],
                                                                             EVSETemplates:             [ new EVSETemplate(AC22, MaxPower: Watt.FromW(22000), ChargingTariffId: T1, Created: Created),
                                                                                                          new EVSETemplate(AC11, MaxPower: Watt.FromW(11000),                       Created: Created) ],
                                                                             ChargingStationTemplates:  [ new ChargingStationTemplate(Wallbox, HotlinePhoneNumber: PhoneNumber.Parse("+49 30 1234567"), Created: Created) ],
                                                                             ChargingPoolTemplates:     [ new ChargingPoolTemplate(Sites, ParkingType: ParkingType.PARKING_LOT, Created: Created) ],
                                                                             ChargingPools:             [ new ChargingPool(P1,
                                                                                                                           TemplateIds:       [ Sites ],
                                                                                                                           Created:           Created,
                                                                                                                           ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                                    TemplateIds:  [ Wallbox ],
                                                                                                                                                                    Created:      Created,
                                                                                                                                                                    EVSEs:        [ new EVSE(E1, TemplateIds: [ AC11, AC22 ], Created: Created),
                                                                                                                                                                                    new EVSE(E2, TemplateIds: [ AC22 ],       Created: Created) ]) ]) ]) ],
                    ParkingOperators:          [ new ParkingOperator(PAR,
                                                                     Created:                Created,
                                                                     ParkingSpaceTemplates:  [ new ParkingSpaceTemplate(Covered, Roofed: true, Created: Created) ],
                                                                     ParkingSpaces:          [ new ParkingSpace(ParkingSpace_Id.Parse("space-1"), TemplateIds: [ Covered ], Created: Created) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Templates_survive_JSON_and_CBOR()

        [Test]
        public void Templates_survive_JSON_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var gef   = json["chargingStationOperators"]![0]!;
            var evse  = gef["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                          Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                          Is.EqualTo(json.ToString()));
                Assert.That(fromJSON.CalcETags(),                                                   Is.EqualTo(rn1.CalcETags()));

                // The order of the templates of a member is content: not sorted.
                Assert.That(evse["templateIds"]!.Values<String>(),                                  Is.EqualTo(new[] { "DE*GEF*ETAC11", "DE*GEF*ETAC22" }));
                Assert.That(gef["EVSETemplates"]!.Select(template => template["@id"]!.Value<String>()),  Is.EqualTo(new[] { "DE*GEF*ETAC11", "DE*GEF*ETAC22" }));
                Assert.That(gef["chargingTariffTemplates"]![0]!["currency"]!.Value<String>(),       Is.EqualTo("EUR"));
                Assert.That(gef["chargingTariffs"]![1]!["currency"],                                Is.Null);
                Assert.That(json["parkingOperators"]![0]!["parkingSpaceTemplates"]![0]!["roofed"]!.Value<Boolean>(),  Is.True);

                // Templates have no runtime status.
                Assert.That(gef["EVSETemplates"]![0]!["status"],                                    Is.Null);

            }

        }

        #endregion

        #region The_first_template_giving_a_value_wins()

        [Test]
        public void The_first_template_giving_a_value_wins()
        {

            var rn1  = NewNetwork();
            var gef  = rn1.GetChargingStationOperatorById(GEF)!;

            Assert.That(gef.TryGetEffectiveEVSE           (E1, out var effectiveE1),  Is.True);
            Assert.That(gef.TryGetEffectiveEVSE           (E2, out var effectiveE2),  Is.True);
            Assert.That(gef.TryGetEffectiveChargingStation(S1, out var effectiveS1),  Is.True);
            Assert.That(gef.TryGetEffectiveChargingPool   (P1, out var effectiveP1),  Is.True);
            Assert.That(gef.TryGetEffectiveChargingTariff (T2, out var effectiveT2),  Is.True);

            rn1.TryGetParkingOperatorById(PAR, out var par);
            par!.TryGetParkingSpace(ParkingSpace_Id.Parse("space-1"), out var space1);

            using (Assert.EnterMultipleScope())
            {

                // E1 names the 11 kW template first; the 22 kW one still gives it its charging tariff.
                Assert.That(effectiveE1!.MaxPower,                                         Is.EqualTo(Watt.FromW(11000)));
                Assert.That(effectiveE1.ChargingTariffIds,                                 Is.EqualTo(new[] { T1 }));
                Assert.That(effectiveE1.TemplateIds,                                       Is.EqualTo(new[] { AC11, AC22 }));
                Assert.That(effectiveE2!.MaxPower,                                         Is.EqualTo(Watt.FromW(22000)));

                Assert.That(effectiveS1!.HotlinePhoneNumber?.ToString(),                   Is.EqualTo("+49 30 1234567"));
                Assert.That(effectiveP1!.ParkingType,                                      Is.EqualTo(ParkingType.PARKING_LOT));

                // A tariff without prices of its own takes all of them from its templates.
                Assert.That(effectiveT2!.Currency,                                         Is.EqualTo(Currency.EUR));
                Assert.That(effectiveT2.Elements.Single().PriceComponents.Single().Price,  Is.EqualTo(0.39m));
                Assert.That(gef.EffectiveChargingTariffs.Select(tariff => tariff.Id),      Is.EqualTo(new[] { T1, T2 }));

                Assert.That(par.EffectiveParkingSpace(space1!).Roofed,                     Is.True);

                // The templates of a member, in its order.
                Assert.That(gef.TemplatesOf(effectiveE1).Select(template => template.Id),  Is.EqualTo(new[] { AC11, AC22 }));

            }

        }

        #endregion

        #region Invalid_templates_are_refused()

        [Test]
        public void Invalid_templates_are_refused()
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

            JObject E1JSON(JObject Network)
                => (JObject) GEFJSON(Network)["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("unknown template",           Changed(network => E1JSON(network)["templateIds"] = new JArray("DE*GEF*ETAC50")),                         "template 'DE*GEF*ETAC50' of the EVSE 'DE*GEF*E1*1' is unknown"),
                ("foreign template",           Changed(network => GEFJSON(network)["EVSETemplates"]![0]!["@id"] = "DE*XYZ*ETAC11"),                       "does not belong to the operator"),
                ("unknown tariff",             Changed(network => GEFJSON(network)["EVSETemplates"]![1]!["chargingTariffId"] = "DE*GEF*T9"),              "charging tariff 'DE*GEF*T9' of the template"),
                ("tariff without prices",      Changed(network => ((JObject) GEFJSON(network)["chargingTariffs"]![1]!).Remove("templateIds")),           "DE*GEF*T2"),
                ("unknown parking template",   Changed(network => network["parkingOperators"]![0]!["parkingSpaces"]![0]!["templateIds"] = new JArray("open")),  "template 'open' of the parking space 'space-1'")
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

        #region Templates_are_changed_by_change_sets()

        [Test]
        public void Templates_are_changed_by_change_sets()
        {

            var rn1   = NewNetwork();
            var gef   = rn1.GetChargingStationOperatorById(GEF)!;
            gef.TryGetEVSETemplate(AC11, out var ac11);
            gef.TryGetEVSE        (E2,   out var e2);

            var ac50  = EVSETemplate_Id.Parse("DE*GEF*ETAC50");

            var rn2   = Apply(rn1,
                              RoamingNetworkChange.Add           (new EVSETemplate(ac50, MaxPower: Watt.FromW(50000))),
                              RoamingNetworkChange.UpdateProperty(ac11!, "maxPower", null, 7400),
                              // Added to templateIds, whose order is content: always at its end, here after AC22.
                              RoamingNetworkChange.AddElement    (nameof(EVSE), E2.ToString(), [ new POIElementPathSegment("templateIds", "DE*GEF*ETAC11") ], "DE*GEF*ETAC11"));

            var gef2  = rn2.GetChargingStationOperatorById(GEF)!;

            gef2.TryGetEVSE(E2, out var changedE2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(gef2.EVSETemplates.Select(template => template.Id),       Is.EqualTo(new[] { AC11, AC22, ac50 }));
                Assert.That(changedE2!.TemplateIds,                                   Is.EqualTo(new[] { AC22, AC11 }));

                Assert.That(gef2.TryGetEffectiveEVSE(E1, out var effectiveE1),        Is.True);
                Assert.That(effectiveE1!.MaxPower,                                    Is.EqualTo(Watt.FromW(7400)));
                Assert.That(gef2.TryGetEffectiveEVSE(E2, out var effectiveE2),        Is.True);
                Assert.That(effectiveE2!.MaxPower,                                    Is.EqualTo(Watt.FromW(22000)));

                // A template a member names can not be removed.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(AC11)),
                            Throws.ArgumentException.With.Message.Contains("template 'DE*GEF*ETAC11' of the EVSE"));

                // A member names only templates of its operator.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(changedE2, "templateIds", null, new JArray("DE*GEF*ETAC99"))),
                            Throws.ArgumentException.With.Message.Contains("template 'DE*GEF*ETAC99' of the EVSE 'DE*GEF*E1*2' is unknown"));

                // A template has no members of its own: membership is the member's.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(ac11!, "EVSEIds", null, new JArray(E1.ToString()))),
                            Throws.ArgumentException.With.Message.Contains("the property 'EVSEIds' of a"));

            }

        }

        #endregion

        #region Templates_of_a_member_changed_differently_conflict()

        /// <summary>
        /// The templates of a member are merged as a whole, as their order is content: both versions changing them
        /// differently conflict, also when each only adds one.
        /// </summary>
        [Test]
        public void Templates_of_a_member_changed_differently_conflict()
        {

            var rn1     = NewNetwork();
            var e2      = rn1.GetChargingStationOperatorById(GEF)!.EVSEs.First(evse => evse.Id == E2);

            var left    = Apply(rn1, RoamingNetworkChange.UpdateProperty(e2, "templateIds", null, new JArray("DE*GEF*ETAC22", "DE*GEF*ETAC11")));
            var right   = Apply(rn1, RoamingNetworkChange.UpdateProperty(e2, "templateIds", null, new JArray("DE*GEF*ETAC11", "DE*GEF*ETAC22")));
            var alike   = Apply(rn1, RoamingNetworkChange.UpdateProperty(e2, "templateIds", null, new JArray("DE*GEF*ETAC22", "DE*GEF*ETAC11")));

            var refused = RoamingNetwork.TryMerge(rn1, left, right, ChangeSet_Id.Parse("merge-1"), out _,         out _, out var conflicts);
            var merged  = RoamingNetwork.TryMerge(rn1, left, alike, ChangeSet_Id.Parse("merge-2"), out var merge, out _, out var none);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(refused,                                 Is.False);
                Assert.That(conflicts.Single().Kind,                 Is.EqualTo(RoamingNetworkMergeConflictKind.DifferentValues));
                Assert.That(conflicts.Single().PropertyName,         Is.EqualTo("templateIds"));

                Assert.That(merged,                                  Is.True, String.Join(" ", none.Select(conflict => conflict.Description)));
                Assert.That(merge!.GetChargingStationOperatorById(GEF)!.EVSEs.First(evse => evse.Id == E2).TemplateIds,
                            Is.EqualTo(new[] { AC22, AC11 }));

            }

        }

        #endregion

    }

}
