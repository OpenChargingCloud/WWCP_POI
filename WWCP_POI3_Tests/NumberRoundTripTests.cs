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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// Numbers as they were: a roaming network written as JSON text and read again has the ETags it was written
    /// with - in a file store, in a memory store, and at a follower over the WebSocket.
    /// </summary>
    /// <remarks>
    /// Decimals are read as decimals, keeping their decimal places, and the canonical JSON and CBOR the ETags are
    /// calculated over keep them as well. Written by Newtonsoft, 150000 W became "150000.0" and was read back with
    /// one decimal place; and a decimal cast to a double is not always the double its text is: 51.050599999999996
    /// became 51.0506. Either way the roaming network read back was not the one written, and a store with an EVSE of
    /// 150 kW could not be loaded at all: "The roaming network does not have the ETags of the commit!"
    /// </remarks>
    [TestFixture]
    public class NumberRoundTripTests
    {

        #region Data

        private static readonly DateTimeOffset  Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private String? directory;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "poi3-numbers-" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (directory is not null && Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }
            catch
            { }
        }

        #endregion


        #region NewNetwork()

        /// <summary>
        /// Whole numbers as decimals, a decimal with a decimal place of its own, and a coordinate that is no
        /// short decimal at all, as the sum of two doubles is.
        /// </summary>
        internal static RoamingNetwork NewNetwork()
        {

            var gridConnectionPoint = new GridConnectionPoint(GridConnectionPoint_Id.Parse("GCP1"),
                                                              VoltageLevel:           GridVoltageLevel.LowVoltage,
                                                              NominalVoltage:         Volt.  FromV (400),
                                                              NominalFrequency:       Hertz. FromHz(50),
                                                              ContractedImportPower:  Watt.  FromW (300000));

            return new RoamingNetwork(
                       RoamingNetwork_Id.Parse("rn1"),
                       Created:                   Created,
                       GridOperators:             [ new GridOperator(GridOperator_Id.Parse("DE*NET"),
                                                                     GridConnectionPoints:  [ gridConnectionPoint ],
                                                                     Created:               Created) ],
                       ChargingStationOperators:  [ new ChargingStationOperator(
                                                        ChargingStationOperator_Id.Parse("DE*GEF"),
                                                        Created:        Created,
                                                        ChargingPools:  [ new ChargingPool(
                                                                              ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                              GeoLocation:            GeoCoordinate.Parse(51.0496 + 0.001, 13.7381),
                                                                              GridConnectionPointId:  gridConnectionPoint.Id,
                                                                              Created:                Created,
                                                                              ChargingStations:       [ new ChargingStation(
                                                                                                            ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                            Created:  Created,
                                                                                                            EVSEs:    [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"),
                                                                                                                                 MaxPower:    Watt.  FromW (150000),
                                                                                                                                 MaxCurrent:  Ampere.FromA (375),
                                                                                                                                 MaxVoltage:  Volt.  FromV (1000),
                                                                                                                                 Created:     Created),
                                                                                                                        new EVSE(EVSE_Id.Parse("DE*GEF*E1*2"),
                                                                                                                                 MaxPower:    Watt.  FromKW(22.0m),
                                                                                                                                 Created:     Created) ]) ]) ]) ]
                   ).WithKeys();

        }

        #endregion


        #region A_file_store_gives_back_the_numbers_it_was_given()

        [Test]
        public void A_file_store_gives_back_the_numbers_it_was_given()
        {

            RoamingNetworkHistory original;

            using (var store = new RoamingNetworkHistoryFileStore(directory!))
            {
                original = RoamingNetworkHistory.Start(NewNetwork(), Store: store);
                original.DetachStore();
            }

            using var store2 = new RoamingNetworkHistoryFileStore(directory!);

            Assert.That(RoamingNetworkHistory.TryLoad(store2, out var loaded, out var error), Is.True, error);
            Assert.That(loaded!.Head.RoamingNetwork.ETags, Is.EqualTo(original.Head.RoamingNetwork.ETags));

        }

        #endregion

        #region A_memory_store_gives_back_the_numbers_it_was_given()

        [Test]
        public void A_memory_store_gives_back_the_numbers_it_was_given()
        {

            var store     = new RoamingNetworkHistoryMemoryStore();
            var original  = RoamingNetworkHistory.Start(NewNetwork(), Store: store);

            original.DetachStore();

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var error), Is.True, error);
            Assert.That(loaded!.Head.RoamingNetwork.ETags, Is.EqualTo(original.Head.RoamingNetwork.ETags));

        }

        #endregion

        #region A_follower_gets_the_numbers_of_its_leader()

        [Test]
        public async Task A_follower_gets_the_numbers_of_its_leader()
        {

            await using var httpServer  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI      = new POI_API(httpServer);

            var leader = RoamingNetworkHistory.Start(NewNetwork().WithKeyStatement()).Attesting();
            poiAPI.AddRoamingNetworkHistory(leader);

            await using var follower = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/history/ws"),
                                                              Options: new RoamingNetworkPeerOptions { TrustAnchor = leader.Head.RoamingNetwork.TrustAnchor() });

            await follower.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(follower.History,                                Is.Not.Null, "The follower did not take the snapshot of its leader.");
                Assert.That(follower.History?.Head.Commit.Id,                Is.EqualTo(leader.Head.Commit.Id));
                Assert.That(follower.History?.Head.RoamingNetwork.ETags,     Is.EqualTo(leader.Head.RoamingNetwork.ETags));
            }

        }

        #endregion

    }

}
