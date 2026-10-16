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
    /// Charging tariffs embedded in their operators, named by its EVSEs.
    /// </summary>
    [TestFixture]
    public class TariffTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly ChargingTariff_Id           T1       = ChargingTariff_Id.         Parse("DE*GEF*T1");
        private static readonly ChargingTariff_Id           T2       = ChargingTariff_Id.         Parse("DE*GEF*T2");

        #endregion

        #region NewTariff(Id, Price = 0.39, WithTypeAndLimits = false)

        private static ChargingTariff NewTariff(ChargingTariff_Id  Id,
                                                Decimal            Price               = 0.39m,
                                                Boolean            WithTypeAndLimits   = false)

            => new (Id,
                    Currency.NOK,
                    [
                        new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, VAT: 25, StepSize: 1) ]),
                        new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.PARKING_TIME, 6, StepSize: 60),
                                                    new ChargingPriceComponent(ChargingDimensionTypes.FLAT,         1) ],
                                                  new ChargingTariffRestrictions(StartTime:   new TimeOnly(22, 0),
                                                                                 EndTime:     new TimeOnly(6, 30),
                                                                                 StartDate:   new DateOnly(2026, 1, 1),
                                                                                 EndDate:     new DateOnly(2027, 1, 1),
                                                                                 MinPower:    3700,
                                                                                 MaxPower:    22000,
                                                                                 MaxDuration: 3600,
                                                                                 DaysOfWeek:  [ DayOfWeek.Sunday, DayOfWeek.Monday ]))
                    ],
                    I18NString.Create(Languages.en, "Standard"),
                    TariffType:  WithTypeAndLimits ? ChargingTariffType.REGULAR             : null,
                    MinPrice:    WithTypeAndLimits ? new ChargingPriceLimit(2.5m, 2.98m)   : null,
                    MaxPrice:    WithTypeAndLimits ? new ChargingPriceLimit(50)             : null,
                    PaymentMeans:   WithTypeAndLimits ? [ PaymentMeans.QR_CODE, PaymentMeans.CREDIT_CARD ]      : null,
                    PaymentBrands:  WithTypeAndLimits ? [ PaymentBrand.VISA, PaymentBrand.Parse("Google Pay") ] : null,
                    Created:     Created);

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:          Created,
                                                                             ChargingTariffs:  [ NewTariff(T1, WithTypeAndLimits: true), NewTariff(T2, 0.49m) ],
                                                                             ChargingPools:    [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                  Created:           Created,
                                                                                                                  ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                           Created:  Created,
                                                                                                                                                           EVSEs:    [ new EVSE(E1,
                                                                                                                                                                                ChargingTariffIds:  [ T2, T1 ],
                                                                                                                                                                                Created:            Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Tariffs_survive_JSON_JSON_text_and_CBOR()

        [Test]
        public void Tariffs_survive_JSON_JSON_text_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                             out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            // Read as text, dates become date tokens.
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),   out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                     out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var tariff       = json["chargingStationOperators"]![0]!["chargingTariffs"]![0]!;
            var restrictions = tariff["elements"]![1]!["restrictions"]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                         Is.EqualTo(rn1.CalcETags()));

                Assert.That(tariff["currency"]!.ToString(),                               Is.EqualTo("NOK"));
                Assert.That(tariff["tariffType"]!.ToString(),                             Is.EqualTo("REGULAR"));
                Assert.That(tariff["minPrice"]!.ToString(Newtonsoft.Json.Formatting.None), Is.EqualTo("""{"excludingVAT":2.5,"includingVAT":2.98}"""));
                Assert.That(tariff["maxPrice"]!.ToString(Newtonsoft.Json.Formatting.None), Is.EqualTo("""{"excludingVAT":50.0}"""));
                Assert.That(json["chargingStationOperators"]![0]!["chargingTariffs"]![1]!["tariffType"], Is.Null);
                Assert.That(tariff["paymentMeans"]!.Values<String>(),                     Is.EqualTo(new[] { "CREDIT_CARD", "QR_CODE" }));
                Assert.That(tariff["paymentBrands"]!.Values<String>(),                    Is.EqualTo(new[] { "Google Pay", "VISA" }));
                Assert.That(json["chargingStationOperators"]![0]!["chargingTariffs"]![1]!["paymentMeans"], Is.Null);
                Assert.That(tariff["elements"]![0]!["priceComponents"]![0]!["type"]!.ToString(),  Is.EqualTo("ENERGY"));
                Assert.That(restrictions["startTime"]!.ToString(),                        Is.EqualTo("22:00"));
                Assert.That(restrictions["endTime"]!.ToString(),                          Is.EqualTo("06:30"));
                Assert.That(restrictions["daysOfWeek"]!.Values<String>(),                 Is.EqualTo(new[] { "MONDAY", "SUNDAY" }));

                Assert.That(fromCBOR.TryGetChargingTariffById(T1, out var t1),            Is.True);
                Assert.That(t1!.TariffType,                                               Is.EqualTo(ChargingTariffType.REGULAR));
                Assert.That(t1.MinPrice,                                                  Is.EqualTo(new ChargingPriceLimit(2.5m, 2.98m)));
                Assert.That(t1.MaxPrice,                                                  Is.EqualTo(new ChargingPriceLimit(50)));
                Assert.That(t1.PaymentMeans,                                              Is.EqualTo(new[] { PaymentMeans.CREDIT_CARD, PaymentMeans.QR_CODE }));
                Assert.That(t1.PaymentBrands,                                             Is.EqualTo(new[] { PaymentBrand.Parse("google pay"), PaymentBrand.VISA }));

                Assert.That(fromJSON.TryGetChargingTariffById(T2, out var t2),            Is.True);
                Assert.That(t2!.Elements[0].PriceComponents[0].Price,                     Is.EqualTo(0.49m));
                Assert.That(fromJSON.GetChargingStationOperatorById(GEF)!.EVSEs.Single().ChargingTariffIds,
                            Is.EqualTo(new[] { T1, T2 }));

            }

        }

        #endregion

        #region Invalid_tariffs_are_refused()

        [Test]
        public void Invalid_tariffs_are_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            JObject Tariff(JObject Network)
                => (JObject) Network["chargingStationOperators"]![0]!["chargingTariffs"]![0]!;

            JObject Restrictions(JObject Network)
                => (JObject) Tariff(Network)["elements"]![1]!["restrictions"]!;

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("unknown currency",     Changed(network => Tariff(network)["currency"] = "XXX"),                                                  "ISO 4217"),
                ("no elements",          Changed(network => Tariff(network)["elements"] = new JArray()),                                           "at least one element"),
                ("flat with step",       Changed(network => Tariff(network)["elements"]![1]!["priceComponents"]![1]!["stepSize"] = 1),             "FLAT price component has no step size"),
                ("energy without step",  Changed(network => ((JObject) Tariff(network)["elements"]![0]!["priceComponents"]![0]!).Remove("stepSize")), "needs a positive step size"),
                ("min above max",        Changed(network => Restrictions(network)["minPower"] = 50000),                                           "must not exceed its maximum"),
                ("invalid time",         Changed(network => Restrictions(network)["startTime"] = "25:00"),                                         "expected HH:mm"),
                ("lower case day",       Changed(network => Restrictions(network)["daysOfWeek"] = new JArray("monday")),                          "day of the week"),
                ("equal times",          Changed(network => { Restrictions(network)["startTime"] = "08:00"; Restrictions(network)["endTime"] = "08:00"; }),
                                                                                                                                                   "start and end times of a charging tariff restriction must differ"),
                ("empty date range",     Changed(network => { Restrictions(network)["startDate"] = "2026-06-01"; Restrictions(network)["endDate"] = "2026-06-01"; }),
                                                                                                                                                   "start date of a charging tariff restriction must be before its end date"),
                ("unknown type",         Changed(network => Tariff(network)["tariffType"] = "regular"),                                            "charging tariff type"),
                ("negative limit",       Changed(network => Tariff(network)["minPrice"]!["excludingVAT"] = -1),                                   "must not be negative"),
                ("gross below net",      Changed(network => Tariff(network)["minPrice"]!["includingVAT"] = 2),                                    "must not be less than the one excluding VAT"),
                ("limit without net",    Changed(network => ((JObject) Tariff(network)["maxPrice"]!).Remove("excludingVAT")),                     "maximum price"),
                ("minimum above max",    Changed(network => Tariff(network)["maxPrice"]!["excludingVAT"] = 1),                                    "must not exceed its maximum price"),
                ("minimum above max VAT",Changed(network => Tariff(network)["maxPrice"] = new ChargingPriceLimit(2.5m, 2.97m).ToJSON()),          "must not exceed its maximum price"),
                ("payment means twice",  Changed(network => Tariff(network)["paymentMeans"]  = new JArray("NFC", "nfc")),                                 "must not be given twice"),
                ("payment brand no text",Changed(network => Tariff(network)["paymentBrands"] = new JArray(42)),                                           "payment brand '42'"),
                ("foreign tariff",       Changed(network => Tariff(network)["@id"] = "DE*XYZ*T1"),                                                 "does not belong to the operator"),
                ("unknown tariff",       Changed(network => network["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["chargingTariffIds"] = new JArray("DE*GEF*T9")),
                                                                                                                                                   "charging tariff 'DE*GEF*T9' of the EVSE")
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

        #region Tariffs_are_changed_by_change_sets()

        [Test]
        public void Tariffs_are_changed_by_change_sets()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var rn1      = history.Head.RoamingNetwork;
            var gef      = rn1.GetChargingStationOperatorById(GEF)!;
            var e1       = gef.EVSEs.Single();
            rn1.TryGetChargingTariffById(T1, out var t1);

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(ChargingTariff), T1.ToString(), "expired", T0, out _, out _);

            var t3       = ChargingTariff_Id.Parse("DE*GEF*T3");
            var elements = new JArray(new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.59m, StepSize: 1000) ]).ToJSON());

            var cs       = rn1.CreateSignedChangeSet([
                               RoamingNetworkChange.Add           (NewTariff(t3, 0.29m)),
                               RoamingNetworkChange.UpdateProperty(t1!, "elements",          null, elements),
                               RoamingNetworkChange.UpdateProperty(t1!, "tariffType",        "REGULAR", "AD_HOC_PAYMENT"),
                               RoamingNetworkChange.RemoveProperty(t1!, "maxPrice"),
                               RoamingNetworkChange.UpdateProperty(t1!, "paymentMeans",      null, new JArray("EMV", "WEBSITE")),
                               RoamingNetworkChange.RemoveProperty(t1!, "paymentBrands"),
                               RoamingNetworkChange.UpdateProperty(e1,  "chargingTariffIds", null, new JArray(t3.ToString())),
                               RoamingNetworkChange.Remove        (T2)
                           ], CreatedAt: T0.AddMinutes(1));

            history.Commit(cs);

            var rn2      = history.Head.RoamingNetwork;
            var gef2     = rn2.GetChargingStationOperatorById(GEF)!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(gef2.ChargingTariffs.Select(tariff => tariff.Id.ToString()),          Is.EqualTo(new[] { "DE*GEF*T1", "DE*GEF*T3" }));
                Assert.That(rn2.TryGetChargingTariffById(T1, out var changedT1),                  Is.True);
                Assert.That(changedT1!.Elements.Single().PriceComponents.Single().StepSize,      Is.EqualTo(1000));
                Assert.That(changedT1.LastChangeDate,                                            Is.EqualTo(T0.AddMinutes(1)));
                Assert.That(gef2.EVSEs.Single().ChargingTariffIds,                                Is.EqualTo(new[] { t3 }));

                // Old values for a later check.
                Assert.That(cs.Changes[7].OldValue!["@id"]!.ToString(),                           Is.EqualTo("DE*GEF*T2"));

                // The type and the price limits.
                Assert.That(changedT1.TariffType,                                                Is.EqualTo(ChargingTariffType.AD_HOC_PAYMENT));
                Assert.That(changedT1.MinPrice,                                                  Is.EqualTo(new ChargingPriceLimit(2.5m, 2.98m)));
                Assert.That(changedT1.MaxPrice,                                                  Is.Null);
                Assert.That(changedT1.PaymentMeans,                                              Is.EqualTo(new[] { PaymentMeans.EMV, PaymentMeans.WEBSITE }));
                Assert.That(changedT1.PaymentBrands,                                             Is.Empty);

                // The status of the tariff survived its change.
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(ChargingTariff), T1.ToString(), out var tariffStatus),  Is.True);
                Assert.That(tariffStatus!.Value,                                                                                                      Is.EqualTo("expired"));

                // A tariff an EVSE names can not be removed, nor can an invalid one be set.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(t3)),
                            Throws.ArgumentException.With.Message.Contains("charging tariff 'DE*GEF*T3' of the EVSE"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(changedT1, "currency", null, "XXX")),
                            Throws.ArgumentException.With.Message.Contains("ISO 4217"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(changedT1, "maxPrice", null, new ChargingPriceLimit(1).ToJSON())),
                            Throws.ArgumentException.With.Message.Contains("must not exceed its maximum price"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.Add(NewTariff(t3))),
                            Throws.ArgumentException.With.Message.Contains("already exists"));

                // What a tariff needs can be changed, but not removed.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.RemoveProperty(changedT1, "currency")),
                            Throws.ArgumentException.With.Message.Contains("the property 'currency' of a charging tariff can not be removed, only changed"));

                Assert.That(() => Apply(rn2, RoamingNetworkChange.RemoveProperty(changedT1, "elements")),
                            Throws.ArgumentException.With.Message.Contains("the property 'elements' of a charging tariff can not be removed, only changed"));

            }

        }

        #endregion

        #region Validity_and_reservation_survive_and_are_checked()

        [Test]
        public void Validity_and_reservation_survive_and_are_checked()
        {

            var reserving = new ChargingTariff(T1,
                                               Currency.EUR,
                                               [
                                                   new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]),
                                                   new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.TIME,   2,     StepSize: 60) ],
                                                                             new ChargingTariffRestrictions(Reservation: ChargingReservationRestriction.RESERVATION)),
                                                   new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.FLAT,   5) ],
                                                                             new ChargingTariffRestrictions(Reservation: ChargingReservationRestriction.RESERVATION_EXPIRES))
                                               ],
                                               ValidFrom:   DateTimeOffset.Parse("2026-11-01T00:00:00.1234Z"),
                                               ValidUntil:  DateTimeOffset.Parse("2027-01-01T00:00:00Z"),
                                               Created:     Created);

            var rn1   = new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                           Created:                   Created,
                                           ChargingStationOperators:  [ new ChargingStationOperator(GEF, Created: Created, ChargingTariffs: [ reserving ]) ]).WithKeys();

            var json  = rn1.ToJSON();
            var t1    = (JObject) json["chargingStationOperators"]![0]!["chargingTariffs"]![0]!;

            Assert.That(RoamingNetwork.TryParse(json,                             out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),   out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                     out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change((JObject) changed["chargingStationOperators"]![0]!["chargingTariffs"]![0]!);
                return changed;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                               Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                               Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                               Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                        Is.EqualTo(rn1.CalcETags()));

                // To the millisecond, as every timestamp of the content.
                Assert.That(t1["validFrom"]!.Value<String>(),                            Is.EqualTo("2026-11-01T00:00:00.123Z"));
                Assert.That(t1["validUntil"]!.Value<String>(),                           Is.EqualTo("2027-01-01T00:00:00.000Z"));
                Assert.That(t1["elements"]![1]!["restrictions"]!["reservation"]!.Value<String>(),  Is.EqualTo("RESERVATION"));
                Assert.That(t1["elements"]![2]!["restrictions"]!["reservation"]!.Value<String>(),  Is.EqualTo("RESERVATION_EXPIRES"));
                Assert.That(t1["elements"]![0]!["restrictions"],                          Is.Null);

                Assert.That(fromCBOR.TryGetChargingTariffById(T1, out var parsed),       Is.True);
                Assert.That(parsed!.ValidUntil,                                          Is.EqualTo(DateTimeOffset.Parse("2027-01-01T00:00:00Z")));
                Assert.That(parsed.Elements[2].Restrictions?.Reservation,                Is.EqualTo(ChargingReservationRestriction.RESERVATION_EXPIRES));

                // Invalid ones are refused.
                Assert.That(RoamingNetwork.TryParse(Changed(tariff => tariff["validUntil"] = "2026-11-01T00:00:00.123Z"), out _, out var equalError),     Is.False);
                Assert.That(equalError,                                                  Does.Contain("must be valid from before it is valid until"));
                Assert.That(RoamingNetwork.TryParse(Changed(tariff => tariff["validFrom"]  = "soon"),                     out _, out var textError),      Is.False);
                Assert.That(textError,                                                   Does.Contain("start of the validity"));
                Assert.That(RoamingNetwork.TryParse(Changed(tariff => tariff["elements"]![1]!["restrictions"]!["reservation"] = "reservation"), out _, out var caseError), Is.False);
                Assert.That(caseError,                                                   Does.Contain("reservation restriction"));

                // ...and change sets change them.
                var rn2 = Apply(rn1, RoamingNetworkChange.RemoveProperty(parsed, "validUntil"),
                                     RoamingNetworkChange.UpdateProperty(parsed, "validFrom", null, "2026-12-01T00:00:00.000Z"));

                Assert.That(rn2.TryGetChargingTariffById(T1, out var changed),           Is.True);
                Assert.That(changed!.ValidUntil,                                         Is.Null);
                Assert.That(changed.ValidFrom,                                           Is.EqualTo(DateTimeOffset.Parse("2026-12-01T00:00:00Z")));

            }

        }

        #endregion

        #region The_runtime_status_reaches_tariffs()

        [Test]
        public void The_runtime_status_reaches_tariffs()
        {

            var leader   = RoamingNetworkHistory.Start(NewNetwork());
            var updates  = new List<POIStatusUpdate>();

            leader.RuntimeStatus.OnStatusUpdate += updates.Add;

            var admin    = leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(ChargingTariff), T1.ToString(),  "outOfService", T0, out _, out _);
            var status   = leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ChargingTariff), T2.ToString(),  "planned",      T0, out _, out _);
            var unknown  = leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ChargingTariff), "DE*GEF*T9",    "planned",      T0, out _, out _);

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotCBOR(), out var follower, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(new[] { admin, status },  Is.All.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(unknown,                  Is.EqualTo(POIStatusUpdateResult.Refused));

                Assert.That(updates.Select(update => $"{update.EntityType} {update.Value}"),
                            Is.EqualTo(new[] { "ChargingTariff outOfService", "ChargingTariff planned" }));

                Assert.That(follower!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(ChargingTariff), T2.ToString(), out var tariffStatus),  Is.True);
                Assert.That(tariffStatus!.Value,                                                                                                        Is.EqualTo("planned"));

            }

        }

        #endregion

    }

}
