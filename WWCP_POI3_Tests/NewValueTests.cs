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

using static cloud.charging.open.protocols.WWCP.POI.tests.ChargingTariffOfferTests;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// New values: what a change set gives is what the content holds (SPECIFICATION.md, section 4). Applied, a new value
    /// written otherwise than the changed entity writes it is refused; prepared by CreateChangeSet, it is written so, before
    /// the owners sign it (WWCP_POI.TS, docs/csharp-review.md, finding 10).
    /// </summary>
    [TestFixture]
    public class NewValueTests
    {

        #region Data

        private static readonly ChargingStation_Id  S9  = ChargingStation_Id.Parse("DE*GEF*S9");
        private static readonly EVSE_Id             E9  = EVSE_Id.           Parse("DE*GEF*E9*1");

        private static EVSE EVSEOf(RoamingNetwork Network, EVSE_Id Id)
            => Network.GetChargingStationOperatorById(GEF)!.ChargingStations.SelectMany(station => station.EVSEs).First(evse => evse.Id == Id);

        private static ChargingStation StationOf(RoamingNetwork Network, ChargingStation_Id Id)
            => Network.GetChargingStationOperatorById(GEF)!.ChargingStations.First(station => station.Id == Id);

        private static ChargingTariffOffer OfferOf(RoamingNetwork Network, ChargingTariffOffer_Id Id)
            => Network.TryGetChargingTariffOfferById(Id, out var offer) ? offer : throw new ArgumentException($"No offer '{Id}'!");

        /// <summary>
        /// The given entity without its lastChange: an Add gives none, the change set sets it.
        /// </summary>
        private static JObject WithoutLastChange(JObject Entity)
        {
            Entity.Remove("lastChange");
            return Entity;
        }

        /// <summary>
        /// The changes as given, each written otherwise than the content holds it, with what the content holds.
        /// </summary>
        private static (RoamingNetworkChange Change, String What, JToken Held)[] NotAsHeld(RoamingNetwork Network)
        {

            // A charging station and a brand as they write themselves on their own: with their JSON-LD context, which embedded
            // they do not name.
            var station = new ChargingStation(S9, Created: Created, EVSEs: [ new EVSE(E9, Created: Created) ]);
            var brand   = new Brand(Brand_Id.Parse("blue-charge"), ImmutableI18NString.Create(Languages.en, "Blue Charge"));

            return [
                (RoamingNetworkChange.UpdateProperty(EVSEOf(Network, E1), "name", null, JObject.Parse("""{ "en": "A9", "@context": "x" }""")),
                 "the property 'name' of the EVSE 'DE*GEF*E1*1'",
                 JObject.Parse("""{ "en": "A9" }""")),
                (RoamingNetworkChange.UpdateProperty(EVSEOf(Network, E1), "acceptedEMobilityProviderIds", null, JArray.Parse("""[ "DE-ZZZ", "de*aaa" ]""")),
                 "the property 'acceptedEMobilityProviderIds' of the EVSE 'DE*GEF*E1*1'",
                 JArray.Parse("""[ "DE*AAA", "DE-ZZZ" ]""")),
                (RoamingNetworkChange.UpdateProperty(StationOf(Network, S1), "features", null, JArray.Parse("""[ "RFIDReader", "Display" ]""")),
                 "the property 'features' of the charging station 'DE*GEF*S1'",
                 JArray.Parse("""[ "Display", "RFIDReader" ]""")),
                (RoamingNetworkChange.UpdateProperty(StationOf(Network, S1), "address", null, JObject.Parse("""{ "street": "Main", "houseNumber": "1", "postalCode": "07743", "city": { "de": "Jena" }, "country": "de" }""")),
                 "the property 'address' of the charging station 'DE*GEF*S1'",
                 JObject.Parse("""{ "street": "Main", "houseNumber": "1", "postalCode": "07743", "city": { "de": "Jena" }, "country": "DEU" }""")),
                (RoamingNetworkChange.UpdateProperty(OfferOf(Network, Summer), "notAfter", null, "2026-09-30T23:59:59+02:00"),
                 "the property 'notAfter' of the charging tariff offer 'gef-summer-2026'",
                 "2026-09-30T21:59:59.000Z"),
                (RoamingNetworkChange.AddElement(nameof(EVSE), E2.ToString(), [ new POIElementPathSegment("brands", "blue-charge") ], brand.ToJSON()),
                 "'brands['blue-charge']' of the EVSE 'DE*GEF*E1*2'",
                 brand.ToJSON(Embedded: true)),
                (RoamingNetworkChange.Add(nameof(ChargingStation), S9.ToString(), station.ToJSON(), nameof(ChargingPool), P2.ToString()),
                 "the charging station 'DE*GEF*S9' to add",
                 WithoutLastChange(station.ToJSON(Embedded: true)))
            ];

        }

        /// <summary>
        /// An unsigned change set of the given changes against the given version, as the network applies it without the
        /// signatures of its owners.
        /// </summary>
        private static RoamingNetworkChangeSet Unsigned(RoamingNetwork Network, params RoamingNetworkChange[] Changes)

            => new RoamingNetworkChangeSet(ChangeSet_Id.NewRandom(),
                                           Network.Id,
                                           T0,
                                           [ .. Changes ],
                                           Network.CalcChangeSetETags(),
                                           Network.CalcChangeSetETags()).WithContentId();

        #endregion


        #region A_new_value_not_given_as_held_is_refused()

        [Test]
        public void A_new_value_not_given_as_held_is_refused()
        {

            var rn1 = NewNetwork();

            using (Assert.EnterMultipleScope())
            {
                foreach (var (change, what, held) in NotAsHeld(rn1))
                {
                    Assert.That(rn1.TryApplyPendingChangeSet(Unsigned(rn1, change), out _, out var error),  Is.False, what);
                    Assert.That(error,  Does.Contain($"Change 0: {what} is not given as the content holds it, '{CanonicalJSON.Serialize(held)}'!"));
                }
            }

        }

        #endregion

        #region A_prepared_change_set_gives_its_new_values_as_held()

        [Test]
        public void A_prepared_change_set_gives_its_new_values_as_held()
        {

            var rn1       = NewNetwork();
            var notAsHeld = NotAsHeld(rn1);
            var prepared  = rn1.CreateSignedChangeSet(notAsHeld.Select(item => item.Change), CreatedAt: T0);

            // Read from its JSON text, as another server reads it, and applied with the signatures of its owners.
            var read      = RoamingNetworkChangeSet.Parse(prepared.ToJSON().ToString());
            var rn2       = rn1.ApplyChangeSet(read);

            using (Assert.EnterMultipleScope())
            {

                for (var index = 0; index < notAsHeld.Length; index++)
                    Assert.That(CanonicalJSON.Serialize(read.Changes[index].NewValue),  Is.EqualTo(CanonicalJSON.Serialize(notAsHeld[index].Held)), notAsHeld[index].What);

                Assert.That(EVSEOf(rn2, E1).AcceptedEMobilityProviderIds.Select(id => id.ToString()),  Is.EqualTo(new[] { "DE*AAA", "DE-ZZZ" }));
                Assert.That(StationOf(rn2, S9).ToJSON(Embedded: true)["@context"],                      Is.Null);

            }

        }

        #endregion

        #region Custom_data_is_held_as_given()

        /// <summary>
        /// Custom data is held as it is given, also a JSON-LD context in it: it is no object of POI.
        /// </summary>
        [Test]
        public void Custom_data_is_held_as_given()
        {

            var rn1         = NewNetwork();
            var customData  = JObject.Parse("""{ "a": { "@context": "c", "b": 1 } }""");
            var prepared    = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(EVSEOf(rn1, E1), "customData", null, customData) ], CreatedAt: T0);
            var rn2         = rn1.ApplyChangeSet(RoamingNetworkChangeSet.Parse(prepared.ToJSON().ToString()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CanonicalJSON.Serialize(prepared.Changes[0].NewValue),                       Is.EqualTo(CanonicalJSON.Serialize(customData)));
                Assert.That(CanonicalJSON.Serialize(EVSEOf(rn2, E1).ToJSON(Embedded: true)["customData"]),  Is.EqualTo(CanonicalJSON.Serialize(customData)));
            }

        }

        #endregion

    }

}
