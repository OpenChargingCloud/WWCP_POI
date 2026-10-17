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

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Numbers without decimal places, as the model holds most of them - 22000 W, 400 V, 32 A - written as text by
    /// ToString() and read back: change sets with their signatures, commits with their identifications, snapshots and
    /// roaming networks with their ETags. And old values compared as their content, the canonical JSON: the integer
    /// 22000 is the decimal 22000, and 22000.0 is another value.
    /// </summary>
    [TestFixture]
    public class WholeNumberTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly EVSE_Id                     E3       = EVSE_Id.                   Parse("DE*GEF*E1*3");

        #endregion

        #region NewEVSE(Id)

        private static EVSE NewEVSE(EVSE_Id Id)

            => new (Id,
                    MaxVoltage:  Volt.  FromV(400),
                    MaxCurrent:  Ampere.FromA(32.5m),
                    MaxPower:    Watt.  FromW(22000),
                    Created:     Created);

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(P1,
                                                                                                                GeoLocation:       GeoCoordinate.Parse(50.92, 11.58),
                                                                                                                Created:           Created,
                                                                                                                ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                         EVSEs:    [ NewEVSE(E1), NewEVSE(E2) ],
                                                                                                                                                         Created:  Created) ]) ]) ]).WithKeys();

        #endregion

        #region (helper) EVSEOf(Network, Id)

        private static EVSE EVSEOf(RoamingNetwork Network, EVSE_Id Id)

            => Network.GetChargingStationOperatorById(GEF)!.TryGetEVSE(Id, out var evse)
                   ? evse!
                   : throw new ArgumentException($"There is no EVSE '{Id}'!");

        #endregion

        #region (helper) WholeNumbers(Network)

        /// <summary>
        /// A change set full of numbers without decimal places: a new EVSE, as its JSON writes it, and the power of
        /// another one, whose old value the change set finds.
        /// </summary>
        private static RoamingNetworkChangeSet WholeNumbers(RoamingNetwork Network)

            => Network.CreateSignedChangeSet([
                   RoamingNetworkChange.Add           (NewEVSE(E3), S1),
                   RoamingNetworkChange.UpdateProperty(EVSEOf(Network, E1), "maxPower", null, 11000)
               ],
               CreatedAt: T0);

        #endregion


        #region Whole_numbers_are_integers_in_the_JSON_and_numbers_with_places_keep_them()

        /// <summary>
        /// Every ToJSON() writes a decimal without decimal places as the integer it is, as its canonical JSON does: of an
        /// EVSE, of the roaming network embedding it, of a change set, of a snapshot; a decimal with places stays one,
        /// with its places. And the JSON of an entity is the same JSON as that of the entity read from it as text.
        /// </summary>
        [Test]
        public void Whole_numbers_are_integers_in_the_JSON_and_numbers_with_places_keep_them()
        {

            var rn1        = NewNetwork();
            var changeSet  = WholeNumbers(rn1);
            var history    = RoamingNetworkHistory.Start(rn1);

            var own        = EVSEOf(rn1, E1).ToJSON();
            var embedded   = rn1.ToJSON()["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;
            var added      = changeSet.ToJSON()["changes"]![0]!["newValue"]!;
            var oldPower   = changeSet.ToJSON()["changes"]![1]!["oldValue"]!;
            var snapshot   = history.ToSnapshotJSON()["roamingNetwork"]!["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                foreach (var (evse, what) in new[] { (own, "its own"), (embedded, "embedded"), (added, "added"), (snapshot, "in a snapshot") })
                {
                    Assert.That(evse["maxPower"]!.Type,                                       Is.EqualTo(JTokenType.Integer), what);
                    Assert.That(evse["maxVoltage"]!.Type,                                     Is.EqualTo(JTokenType.Integer), what);
                    Assert.That(evse["maxCurrent"]!.Type,                                     Is.EqualTo(JTokenType.Float),   what);
                    Assert.That(evse["maxCurrent"]!.ToString(Newtonsoft.Json.Formatting.None), Is.EqualTo("32.5"),            what);
                }

                Assert.That(oldPower.Type,                                                    Is.EqualTo(JTokenType.Integer));
                Assert.That(changeSet.ToJSON().ToString(),                                    Does.Contain("\"maxPower\": 22000,").And.Not.Contain("22000.0"));

                Assert.That(JToken.DeepEquals(own, ReadAsText(own.ToString())),               Is.True, "the JSON of an EVSE is not the same JSON read again from its text");

            }

        }

        #endregion

        #region A_roaming_network_written_by_ToString_keeps_its_ETags()

        /// <summary>
        /// Read with its decimals as decimals, as the POI JSON is: its eTags, written in it, are those of its content.
        /// </summary>
        [Test]
        public void A_roaming_network_written_by_ToString_keeps_its_ETags()
        {

            var rn1   = NewNetwork();
            var text  = rn1.ToJSON().ToString();

            Assert.That(RoamingNetwork.TryParse((JObject) ReadAsText(text), out var read, out var error), Is.True, error);
            Assert.That(read!.CalcETags(), Is.EqualTo(rn1.CalcETags()));

        }

        #endregion

        #region (helper) ReadAsText(Text)

        /// <summary>
        /// JSON read from a text as the POI JSON is: numbers with a fraction as decimals, keeping their decimal
        /// places, and texts as texts, never as points in time (SPECIFICATION section 1).
        /// </summary>
        private static JToken ReadAsText(String Text)
        {

            using var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(Text)) {
                                   FloatParseHandling  = Newtonsoft.Json.FloatParseHandling.Decimal,
                                   DateParseHandling   = Newtonsoft.Json.DateParseHandling.None
                               };

            return JToken.ReadFrom(reader);

        }

        #endregion

        #region A_change_set_written_by_ToString_keeps_its_signatures()

        [Test]
        public void A_change_set_written_by_ToString_keeps_its_signatures()
        {

            var rn1        = NewNetwork();
            var changeSet  = WholeNumbers(rn1);

            Assert.That(RoamingNetworkChangeSet.TryParse(changeSet.ToJSON().ToString(), out var read, out var error), Is.True, error);

            var result     = RoamingNetworkHistory.Start(rn1).Commit(read!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status,                                                Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);
                Assert.That(EVSEOf(result.Head.RoamingNetwork, E1).MaxPower,             Is.EqualTo(Watt.FromW(11000)));
                Assert.That(EVSEOf(result.Head.RoamingNetwork, E3).MaxCurrent,           Is.EqualTo(Ampere.FromA(32.5m)));
            }

        }

        #endregion

        #region A_change_set_written_by_ToString_is_committed_over_HTTP()

        /// <summary>
        /// The trap as a client of the POI API met it: the body of COMMIT .../history/changeSets written by
        /// ToString(), not as canonical JSON.
        /// </summary>
        [Test]
        public async Task A_change_set_written_by_ToString_is_committed_over_HTTP()
        {

            var rn1             = NewNetwork();
            var history         = RoamingNetworkHistory.Start(rn1);

            await using var httpServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI      = new POI_API(httpServer) {
                                              AuthorizeChangeSetSubmission = (request, changeSet) => true
                                          };

            poiAPI.AddRoamingNetworkHistory(history);

            using var http      = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/history/") };
            using var request   = new HttpRequestMessage(new HttpMethod("COMMIT"), "changeSets") {
                                      Content = new StringContent(WholeNumbers(rn1).ToJSON().ToString(), System.Text.Encoding.UTF8, "application/json")
                                  };
            using var response  = await http.SendAsync(request);

            var text            = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode,                                  Is.EqualTo(HttpStatusCode.Created), text);
                Assert.That(EVSEOf(history.Head.RoamingNetwork, E1).MaxPower,     Is.EqualTo(Watt.FromW(11000)));
            }

        }

        #endregion

        #region A_commit_written_by_ToString_keeps_its_identification()

        [Test]
        public void A_commit_written_by_ToString_keeps_its_identification()
        {

            var rn1     = NewNetwork();
            var commit  = RoamingNetworkHistory.Start(rn1).Commit(WholeNumbers(rn1)).Commit!;

            Assert.That(RoamingNetworkCommit.TryParse(commit.ToJSON().ToString(), out var read, out var error), Is.True, error);
            Assert.That(read!.Id, Is.EqualTo(commit.Id));

        }

        #endregion

        #region A_snapshot_written_by_ToString_keeps_its_ETags()

        [Test]
        public void A_snapshot_written_by_ToString_keeps_its_ETags()
        {

            var rn1      = NewNetwork();
            var history  = RoamingNetworkHistory.Start(rn1);

            history.Commit(WholeNumbers(rn1));

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(history.ToSnapshotJSON().ToString(), out var follower, out var error), Is.True, error);
            Assert.That(follower!.Head.Commit.Id, Is.EqualTo(history.Head.Commit.Id));

        }

        #endregion

        #region A_merge_commit_read_as_text_is_the_merge_of_its_parents()

        /// <summary>
        /// Two servers change the power of two EVSEs at once and merge; the merge commit of the one, read as text as
        /// the WebSocket and HTTP send it, is the merge the other one calculates of the same parents.
        /// </summary>
        [Test]
        public void A_merge_commit_read_as_text_is_the_merge_of_its_parents()
        {

            var rn1      = NewNetwork();
            var server1  = RoamingNetworkHistory.Start(rn1);

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(server1.ToSnapshotJSON(), out var server2, out var snapshotError), Is.True, snapshotError);

            server1.Commit(rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(EVSEOf(rn1, E1), "maxPower", null, 11000) ], CreatedAt: T0));
            server2!.Commit(rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(EVSEOf(rn1, E2), "maxPower", null, 7400) ], CreatedAt: T0.AddSeconds(1)));

            // Server 1 takes the commit of server 2 and merges.
            Assert.That(server2.TryGetMissingCommits(server1.HaveIds(), out var fromServer2, out var missingError2), Is.True, missingError2);
            Assert.That(server1.TryAppend(fromServer2!, out _, out var appendError1),                              Is.True, appendError1);
            Assert.That(server1.Head.Commit.ParentIds.Length,                                                       Is.EqualTo(2), "server 1 did not merge");

            // Server 2 takes the commits of server 1, its merge commit among them, as text.
            Assert.That(server1.TryGetMissingCommits(server2.HaveIds(), out var fromServer1, out var missingError1), Is.True, missingError1);

            var asText   = fromServer1!.Select(commit => RoamingNetworkCommit.Parse(CanonicalJSON.Serialize(commit.ToJSON()))).ToArray();

            Assert.That(server2.TryAppend(asText, out _, out var appendError2), Is.True, appendError2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(server2.Head.Commit.Id,                                Is.EqualTo(server1.Head.Commit.Id));
                Assert.That(EVSEOf(server2.Head.RoamingNetwork, E1).MaxPower,      Is.EqualTo(Watt.FromW(11000)));
                Assert.That(EVSEOf(server2.Head.RoamingNetwork, E2).MaxPower,      Is.EqualTo(Watt.FromW(7400)));
            }

        }

        #endregion


        #region An_old_value_is_compared_as_its_canonical_JSON()

        /// <summary>
        /// The integer 22000 is the old value 22000 of the model's decimal; 22000.0 is not, as its content differs, and
        /// the refusal says the value as its content is.
        /// </summary>
        [Test]
        public void An_old_value_is_compared_as_its_canonical_JSON()
        {

            var rn1      = NewNetwork();
            var e1       = EVSEOf(rn1, E1);

            var asInteger  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(e1, "maxPower", 22000, 11000) ], CreatedAt: T0);

            var refused    = Assert.Throws<ArgumentException>(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(e1, "maxPower", new JValue(22000.0m), 11000) ], CreatedAt: T0))!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(asInteger.Changes.Single().OldValue?.ToString(),  Is.EqualTo("22000"));
                Assert.That(refused.Message,                                  Does.Contain("is no longer the expected old value, but '22000'!"));
            }

        }

        #endregion

    }

}
